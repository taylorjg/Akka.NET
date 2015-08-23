using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Event;
using Akka.Util.Internal;

namespace ActorBinTree
{
    public class BinaryTreeSet : ReceiveActor
    {
        private IActorRef _root = CreateRoot();
        private readonly Queue<BinaryTreeSetMessages.Operation> _pendingQueue = new Queue<BinaryTreeSetMessages.Operation>();
        private readonly ILoggingAdapter _loggingAdapter = Context.GetLogger();

        public BinaryTreeSet()
        {
            Become(Normal);
        }

        private void Normal()
        {
            Receive<BinaryTreeSetMessages.Operation>(op =>
            {
                _loggingAdapter.Info($"Forwarding operation: {op.Id}");
                _root.Forward(op);
            });

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
                SetReceiveTimeout(TimeSpan.FromSeconds(5));

                Receive<BinaryTreeSetMessages.Operation>(op =>
                {
                    _loggingAdapter.Info($"Enqueuing operation whilst garbage collecting: {op.Id}");
                    _pendingQueue.Enqueue(op);
                });

                Receive<BinaryTreeSetMessages.Gc>(_ => { /* ignore message */ });

                Receive<BinaryTreeNodeMessages.CopyFinished>(_ =>
                {
                    _loggingAdapter.Info("Received CopyFinished - garbage collection complete");
                    _root.Tell(PoisonPill.Instance);
                    _root = newRoot;
                    _pendingQueue.ForEach(_root.Tell);
                    _pendingQueue.Clear();
                    Become(Normal);
                });
            };
        }

        private static int _rootNumber = 1;

        private static IActorRef CreateRoot()
        {
            return Context.ActorOf(BinaryTreeNode.Props(0, true), $"Root{_rootNumber++}");
        }
    }
}
