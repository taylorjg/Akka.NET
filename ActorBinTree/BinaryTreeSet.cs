using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Util.Internal;

namespace ActorBinTree
{
    public class BinaryTreeSet : ReceiveActor
    {
        private IActorRef _root = CreateRoot();
        private readonly Queue<BinaryTreeSetMessages.Operation> _pendingQueue = new Queue<BinaryTreeSetMessages.Operation>();

        public BinaryTreeSet()
        {
            Become(Normal);
        }

        private void Normal()
        {
            Receive<BinaryTreeSetMessages.Operation>(op => _root.Forward(op));

            Receive<BinaryTreeSetMessages.Gc>(_ =>
            {
                var newRoot = CreateRoot();
                _root.Tell(new BinaryTreeNodeMessages.CopyTo(newRoot));
                Become(GarbageCollecting(newRoot));
            });
        }

        private Action GarbageCollecting(IActorRef newRoot)
        {
            return () =>
            {
                Receive<BinaryTreeSetMessages.Operation>(op => _pendingQueue.Enqueue(op));

                Receive<BinaryTreeSetMessages.Gc>(_ =>
                {
                });

                Receive<BinaryTreeNodeMessages.CopyFinished>(_ =>
                {
                    _root.Tell(PoisonPill.Instance);
                    _root = newRoot;
                    _pendingQueue.ForEach(_root.Tell);
                    _pendingQueue.Clear();
                    Become(Normal);
                });
            };
        }

        private static IActorRef CreateRoot()
        {
            return Context.ActorOf(BinaryTreeNode.Props(0, false));
        }
    }
}
