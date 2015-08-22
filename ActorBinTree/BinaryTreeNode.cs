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

        private BinaryTreeNode(int elem, bool initiallyRemoved)
        {
            _elem = elem;
            _removed = initiallyRemoved;
            Become(Normal);
        }

        private void Normal()
        {
            Receive<BinaryTreeSetMessages.Insert>(op =>
            {
                if (_elem == op.Elem)
                {
                    _removed = false;
                    op.Requester.Tell(new BinaryTreeSetMessages.OperationFinished(op.Id));
                    return;
                }

                if (op.Elem < _elem)
                {
                    IActorRef n;
                    switch (_subtrees.TryGetValue(Position.Left, out n))
                    {
                        case true:
                            n.Tell(op);
                            break;
                        default:
                            var a = Context.ActorOf(BinaryTreeNode.Props(op.Elem, false), "Left");
                            _subtrees[Position.Left] = a;
                            op.Requester.Tell(new BinaryTreeSetMessages.OperationFinished(op.Id));
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
                            break;
                        default:
                            var a = Context.ActorOf(BinaryTreeNode.Props(op.Elem, false), "Right");
                            _subtrees[Position.Right] = a;
                            op.Requester.Tell(new BinaryTreeSetMessages.OperationFinished(op.Id));
                            break;
                    }
                }
            });

            Receive<BinaryTreeSetMessages.Contains>(op =>
            {
                if (_elem == op.Elem)
                {
                    op.Requester.Tell(new BinaryTreeSetMessages.ContainsResult(op.Id, !_removed));
                    return;
                }

                if (op.Elem < _elem)
                {
                    IActorRef n;
                    switch (_subtrees.TryGetValue(Position.Left, out n))
                    {
                        case true:
                            n.Tell(op);
                            break;
                        default:
                            op.Requester.Tell(new BinaryTreeSetMessages.ContainsResult(op.Id, false));
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
                            break;
                        default:
                            op.Requester.Tell(new BinaryTreeSetMessages.ContainsResult(op.Id, false));
                            break;
                    }
                }
            });

            Receive<BinaryTreeSetMessages.Remove>(op =>
            {
                if (_elem == op.Elem)
                {
                    _removed = true;
                    op.Requester.Tell(new BinaryTreeSetMessages.OperationFinished(op.Id));
                    return;
                }

                if (op.Elem < _elem)
                {
                    IActorRef n;
                    switch (_subtrees.TryGetValue(Position.Left, out n))
                    {
                        case true:
                            n.Tell(op);
                            break;
                        default:
                            op.Requester.Tell(new BinaryTreeSetMessages.OperationFinished(op.Id));
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
                            break;
                        default:
                            op.Requester.Tell(new BinaryTreeSetMessages.OperationFinished(op.Id));
                            break;
                    }
                }
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
