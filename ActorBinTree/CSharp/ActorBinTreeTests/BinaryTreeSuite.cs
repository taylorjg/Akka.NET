using NUnit.Framework;
using Akka.Actor;
using ActorBinTree;
using Akka.TestKit.NUnit;

namespace ActorBinTreeTests
{
    [TestFixture]
    public class BinaryTreeSuite : TestKit
    {
        [Test]
        public void ProperInsertsAndLookups()
        {
            var topNode = Sys.ActorOf(Props.Create<BinaryTreeSet>());

            topNode.Tell(new BinaryTreeSetMessages.Contains(TestActor, 1, 1));
            ExpectMsg(new BinaryTreeSetMessages.ContainsResult(1, false), ContainsResultComparer);

            topNode.Tell(new BinaryTreeSetMessages.Insert(TestActor, 2, 1));
            topNode.Tell(new BinaryTreeSetMessages.Contains(TestActor, 3, 1));

            ExpectMsg(new BinaryTreeSetMessages.OperationFinished(2), OperationFinishedComparer);
            ExpectMsg(new BinaryTreeSetMessages.ContainsResult(3, true), ContainsResultComparer);
        }

        //private void ReceiveN(
        //    TestProbe probe,
        //    List<BinaryTreeSetMessages.Operation> ops,
        //    List<BinaryTreeSetMessages.OperationReply> expectedReplies)
        //{
        //}

        //private void Verify(
        //    TestProbe probe,
        //    List<BinaryTreeSetMessages.Operation> ops,
        //    List<BinaryTreeSetMessages.OperationReply> expectedReplies)
        //{
        //    var topNode = Sys.ActorOf(Props.Create<BinaryTreeSet>());
        //    ops.ForEach(topNode.Tell);
        //    ReceiveN(probe, ops, expectedReplies);
        //}

        private static bool OperationFinishedComparer(
            BinaryTreeSetMessages.OperationFinished msg1,
            BinaryTreeSetMessages.OperationFinished msg2)
        {
            return msg1.Id == msg2.Id;
        }

        private static bool ContainsResultComparer(
            BinaryTreeSetMessages.ContainsResult msg1,
            BinaryTreeSetMessages.ContainsResult msg2)
        {
            return
                msg1.Id == msg2.Id &&
                msg1.Result == msg2.Result;
        }
    }
}
