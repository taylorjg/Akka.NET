using Akka.Actor;

namespace ActorBinTree
{
    internal static class BinaryTreeNodeMessages
    {
        public class CopyTo
        {
            public CopyTo(IActorRef treeNode)
            {
                TreeNode = treeNode;
            }

            public IActorRef TreeNode { get; }
        }

        public class CopyFinished
        {
        }
    }
}
