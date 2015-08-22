using System;
using System.Collections.Generic;
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
            Action<T, Position> onElemNotEqual,
            Action<T> onOperationFinished)
            where T: BinaryTreeSetMessages.Operation
        {
            if (op.Elem == _elem)
            {
                onElemEqual(op);
                onOperationFinished(op);
                return;
            }

            if (op.Elem < _elem)
            {
                IActorRef n;
                switch (_subtrees.TryGetValue(Position.Left, out n))
                {
                    case true:
                        n.Tell(op);
                        return;
                    default:
                        if (createMissingNodes)
                        {
                            n = Context.ActorOf(Props(op.Elem, false), "Left");
                            _subtrees[Position.Left] = n;
                        }
                        onElemNotEqual(op, Position.Left);
                        onOperationFinished(op);
                        break;
                }
            }
            else
            {
                IActorRef n;
                switch (_subtrees.TryGetValue(Position.Right, out n))
                {
                    case true:
                        n.Tell(op);
                        return;
                    default:
                        if (createMissingNodes)
                        {
                            n = Context.ActorOf(Props(op.Elem, false), "Right");
                            _subtrees[Position.Right] = n;
                        }
                        onElemNotEqual(op, Position.Right);
                        onOperationFinished(op);
                        break;
                }
            }
        }

        private void Normal()
        {
            Receive<BinaryTreeSetMessages.Insert>(op =>
            {
                HandleOperation(
                    op,
                    true,
                    _ => _removed = false,
                    (_, __) => { },
                    _ => op.Requester.Tell(new BinaryTreeSetMessages.OperationFinished(op.Id)));
            });

            Receive<BinaryTreeSetMessages.Contains>(op =>
            {
                HandleOperation(
                    op,
                    false,
                    _ => op.Requester.Tell(new BinaryTreeSetMessages.ContainsResult(op.Id, !_removed)),
                    (_, __) => op.Requester.Tell(new BinaryTreeSetMessages.ContainsResult(op.Id, false)),
                    _ => { });
            });

            Receive<BinaryTreeSetMessages.Remove>(op =>
            {
                HandleOperation(
                    op,
                    false,
                    _ => _removed = true,
                    (_, __) => { },
                    _ => op.Requester.Tell(new BinaryTreeSetMessages.OperationFinished(op.Id)));
            });

            Receive<BinaryTreeNodeMessages.CopyTo>(msg =>
            {
            });
        }

        private Action Copying(IList<IActorRef> expected, bool insertConfirmed)
        {
            return () =>
            {
                Receive<BinaryTreeSetMessages.OperationFinished>(msg =>
                {
                });

                Receive<BinaryTreeNodeMessages.CopyFinished>(msg =>
                {
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
