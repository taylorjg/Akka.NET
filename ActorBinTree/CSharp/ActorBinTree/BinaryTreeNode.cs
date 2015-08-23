using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.Event;

namespace ActorBinTree
{
    internal class BinaryTreeNode : ReceiveActor
    {
        private readonly int _elem;
        private bool _removed;
        private readonly Dictionary<Position, IActorRef> _subtrees = new Dictionary<Position, IActorRef>();
        private readonly ILoggingAdapter _loggingAdapter = Context.GetLogger();

        // ReSharper disable once MemberCanBePrivate.Global
        public BinaryTreeNode(int elem, bool initiallyRemoved)
        {
            _elem = elem;
            _removed = initiallyRemoved;
            Become(Normal);
        }

        private void HandleOperation<T>(
            T op,
            bool createMissingNodes,
            Action<T> onElemEqual,
            Action<T> onOperationFinished)
            where T: BinaryTreeSetMessages.Operation
        {
            if (op.Elem == _elem)
            {
                onElemEqual(op);
                onOperationFinished(op);
                return;
            }

            Action<Position> handleSubtrees = position =>
            {
                IActorRef n;
                switch (_subtrees.TryGetValue(position, out n))
                {
                    case true:
                        n.Tell(op);
                        return;
                    default:
                        if (createMissingNodes)
                        {
                            n = Context.ActorOf(Props(op.Elem, false), position.ToString());
                            _subtrees[position] = n;
                        }
                        onOperationFinished(op);
                        break;
                }
            };

            handleSubtrees(op.Elem < _elem ? Position.Left : Position.Right);
        }

        private void Normal()
        {
            _loggingAdapter.Info("Becoming Normal");

            Receive<BinaryTreeSetMessages.Insert>(op =>
            {
                HandleOperation(
                    op,
                    true,
                    _ => _removed = false,
                    _ => op.Requester.Tell(new BinaryTreeSetMessages.OperationFinished(op.Id)));
            });

            Receive<BinaryTreeSetMessages.Contains>(op =>
            {
                var result = false;
                HandleOperation(
                    op,
                    false,
                    _ => result = !_removed,
                    _ => op.Requester.Tell(new BinaryTreeSetMessages.ContainsResult(op.Id, result)));
            });

            Receive<BinaryTreeSetMessages.Remove>(op =>
            {
                HandleOperation(
                    op,
                    false,
                    _ => _removed = true,
                    _ => op.Requester.Tell(new BinaryTreeSetMessages.OperationFinished(op.Id)));
            });

            Receive<BinaryTreeNodeMessages.CopyTo>(msg =>
            {
                _loggingAdapter.Info("CopyTo");

                var expected = Context.GetChildren().ToList();

                if (_removed && !expected.Any())
                {
                    _loggingAdapter.Info("Sending CopyFinished because _removed && !expected.Any()");
                    Context.Parent.Tell(new BinaryTreeNodeMessages.CopyFinished());
                    return;
                }

                if (!_removed) msg.TreeNode.Tell(new BinaryTreeSetMessages.Insert(Self, _elem, _elem));
                // TODO: could we just forward msg ?
                expected.ForEach(n => n.Tell(new BinaryTreeNodeMessages.CopyTo(msg.TreeNode)));
                Become(Copying(expected, _removed));
            });
        }

        private Action Copying(List<IActorRef> expected, bool insertConfirmed)
        {
            _loggingAdapter.Info($"Becoming Copying - expected.Count: {expected.Count}; insertConfirmed: {insertConfirmed}");

            //Action checkForCopyFinished = () =>
            //{
            //    if (!expected.Any() && insertConfirmed)
            //    {
            //        Context.Parent.Tell(new BinaryTreeNodeMessages.CopyFinished());
            //        Become(Normal);
            //    }
            //};

            return () =>
            {
                _loggingAdapter.Info($"Inside Copying become action method - expected.Count: {expected.Count}; insertConfirmed: {insertConfirmed}");

                Receive<BinaryTreeSetMessages.OperationFinished>(msg =>
                {
                    //insertConfirmed = true;
                    //checkForCopyFinished();
                    if (!expected.Any())
                    {
                        _loggingAdapter.Info("Sending CopyFinished because !expected.Any()");
                        Context.Parent.Tell(new BinaryTreeNodeMessages.CopyFinished());
                        Become(Normal);
                    }
                    else
                    {
                        Become(Copying(expected, true));
                    }
                });

                Receive<BinaryTreeNodeMessages.CopyFinished>(msg =>
                {
                    expected.Remove(Sender);
                    //checkForCopyFinished();
                    if (!expected.Any() && insertConfirmed)
                    {
                        _loggingAdapter.Info("Sending CopyFinished because !expected.Any() && insertConfirmed");
                        Context.Parent.Tell(new BinaryTreeNodeMessages.CopyFinished());
                        Become(Normal);
                    }
                    else
                    {
                        Become(Copying(expected, insertConfirmed));
                    }
                });
            };
        }

        private enum Position
        {
            Left,
            Right
        }

        public static Props Props(int elem, bool initiallyRemoved)
        {
            return Akka.Actor.Props.Create(() => new BinaryTreeNode(elem, initiallyRemoved));
        }
    }
}
