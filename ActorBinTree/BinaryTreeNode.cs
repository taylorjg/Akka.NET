using Akka.Actor;

namespace ActorBinTree
{
    internal class BinaryTreeNode : ReceiveActor
    {
        private BinaryTreeNode(int elem, bool initiallyRemoved)
        {
            
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
