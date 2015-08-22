using System;
using Akka.Actor;

namespace ActorBinTreeApp
{
    internal static class Program
    {
        private static void Main()
        {
            var system = ActorSystem.Create("ActorBinTree");
            var client = system.ActorOf(Props.Create(typeof(Client)), "Client");
            Console.ReadLine();
        }
    }
}
