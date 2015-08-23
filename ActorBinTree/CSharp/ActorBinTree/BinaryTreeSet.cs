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
            Console.WriteLine("BinaryTreeSet Normal");

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
            Console.WriteLine("BinaryTreeSet GarbageCollecting");

            return () =>
            {
                Console.WriteLine("BinaryTreeSet GarbageCollecting action");

                SetReceiveTimeout(TimeSpan.FromSeconds(5));

                Receive<BinaryTreeSetMessages.Operation>(op =>
                {
                    Console.WriteLine($"Enqueuing operation whilst garbage collecting: {op.Id}");
                    _pendingQueue.Enqueue(op);
                });

                Receive<BinaryTreeSetMessages.Gc>(_ => {});

                //Receive<BinaryTreeNodeMessages.CopyFinished>(_ =>
                //{
                //    _root.Tell(PoisonPill.Instance);
                //    _root = newRoot;
                //    _pendingQueue.ForEach(_root.Tell);
                //    _pendingQueue.Clear();
                //    Become(Normal);
                //});

                Receive<ReceiveTimeout>(_ =>
                {
                    SetReceiveTimeout(null);
                    _pendingQueue.ForEach(_root.Tell);
                    _pendingQueue.Clear();
                    Become(Normal);
                });
            };
        }

        private static IActorRef CreateRoot()
        {
            return Context.ActorOf(BinaryTreeNode.Props(0, true));
        }
    }
}
