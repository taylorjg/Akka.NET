using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;

namespace ActorBinTree
{
    internal class BinaryTreeNode : ReceiveActor
    {
        private readonly int _elem;
        private bool _removed;
        private readonly Dictionary<Position, IActorRef> _subtrees = new Dictionary<Position, IActorRef>();

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
                var expected = Context.GetChildren().ToList();

                if (!expected.Any() && _removed)
                {
                    Context.Parent.Tell(new BinaryTreeNodeMessages.CopyFinished());
                    return;
                }

                if (!_removed) msg.TreeNode.Tell(new BinaryTreeSetMessages.Insert(Self, _elem, _elem));
                expected.ForEach(n => n.Forward(msg));
                Become(Copying(expected, _removed));
            });
        }

        private Action Copying(List<IActorRef> expected, bool insertConfirmed)
        {
            // ReSharper disable once ImplicitlyCapturedClosure
            Action checkForCopyFinished = () =>
            {
                if (!expected.Any() && insertConfirmed)
                {
                    Context.Parent.Tell(new BinaryTreeNodeMessages.CopyFinished());
                    Become(Normal);
                }
            };

            return () =>
            {
                Receive<BinaryTreeSetMessages.OperationFinished>(msg =>
                {
                    insertConfirmed = true;
                    checkForCopyFinished();
                });

                Receive<BinaryTreeNodeMessages.CopyFinished>(msg =>
                {
                    expected.Remove(Sender);
                    checkForCopyFinished();
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
