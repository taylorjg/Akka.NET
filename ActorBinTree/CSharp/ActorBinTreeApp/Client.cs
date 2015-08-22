using System;
using ActorBinTree;
using Akka.Actor;

namespace ActorBinTreeApp
{
    internal class Client : ReceiveActor
    {
        public Client()
        {
            var binaryTreeSet = Context.ActorOf(Props.Create(typeof(BinaryTreeSet)), "BinaryTreeSet");

            binaryTreeSet.Tell(new BinaryTreeSetMessages.Insert(Self, 1, 10));
            binaryTreeSet.Tell(new BinaryTreeSetMessages.Insert(Self, 2, 20));
            binaryTreeSet.Tell(new BinaryTreeSetMessages.Insert(Self, 3, 30));

            binaryTreeSet.Tell(new BinaryTreeSetMessages.Gc());

            binaryTreeSet.Tell(new BinaryTreeSetMessages.Insert(Self, 4, 40));
            binaryTreeSet.Tell(new BinaryTreeSetMessages.Insert(Self, 5, 50));

            binaryTreeSet.Tell(new BinaryTreeSetMessages.Contains(Self, 6, 20));
            binaryTreeSet.Tell(new BinaryTreeSetMessages.Contains(Self, 7, 50));
            binaryTreeSet.Tell(new BinaryTreeSetMessages.Contains(Self, 8, 100));

            Receive<BinaryTreeSetMessages.OperationFinished>(msg => Console.WriteLine($"OperationFinished: {msg.Id}"));
            Receive<BinaryTreeSetMessages.ContainsResult>(msg => Console.WriteLine($"ContainsResult: {msg.Id} {msg.Result}"));
        }
    }
}
