using Akka.Actor;

namespace ActorBinTree
{
    internal class BinaryTreeNodeMessages
    {
        public class CopyTo
        {
            private readonly IActorRef _treeNode;

            public CopyTo(IActorRef treeNode)
            {
                _treeNode = treeNode;
            }

            public IActorRef TreeNode
            {
                get { return _treeNode; }
            }
        }

        public class CopyFinished
        {
        }
    }
}
