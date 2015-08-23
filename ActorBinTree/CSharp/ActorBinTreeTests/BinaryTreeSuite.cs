﻿using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Akka.Actor;
using ActorBinTree;
using Akka.TestKit;
using Akka.TestKit.NUnit;

namespace ActorBinTreeTests
{
    [TestFixture]
    public class BinaryTreeSuite : TestKit
    {
        private const string ConfigString = @"
            akka {
              loglevel = INFO
              test {
                testkit {
                  debug = true
                }
              }
            }";

        public BinaryTreeSuite() : base(ConfigString)
        {
        }

        [Test]
        public void ProperInsertsAndLookups()
        {
            var topNode = Sys.ActorOf(Props.Create<BinaryTreeSet>());

            topNode.Tell(new BinaryTreeSetMessages.Contains(TestActor, 1, 1));
            ExpectMsg(new BinaryTreeSetMessages.ContainsResult(1, false), ContainsResultComparerMethod);

            topNode.Tell(new BinaryTreeSetMessages.Insert(TestActor, 2, 1));
            topNode.Tell(new BinaryTreeSetMessages.Contains(TestActor, 3, 1));

            ExpectMsg(new BinaryTreeSetMessages.OperationFinished(2), OperationFinishedComparerMethod);
            ExpectMsg(new BinaryTreeSetMessages.ContainsResult(3, true), ContainsResultComparerMethod);
        }

        [Test]
        public void InstructionExample()
        {
            var requester = CreateTestProbe();
            var requesterRef = requester.Ref;

            var ops = new List<BinaryTreeSetMessages.Operation>
            {
                new BinaryTreeSetMessages.Insert(requesterRef, 100, 1),
                new BinaryTreeSetMessages.Contains(requesterRef, 50, 2),
                new BinaryTreeSetMessages.Remove(requesterRef, 10, 1),
                new BinaryTreeSetMessages.Insert(requesterRef, 20, 2),
                new BinaryTreeSetMessages.Contains(requesterRef, 80, 1),
                new BinaryTreeSetMessages.Contains(requesterRef, 70, 2)
            };

            var expectedReplies = new List<BinaryTreeSetMessages.OperationReply>
            {
                new BinaryTreeSetMessages.OperationFinished(10),
                new BinaryTreeSetMessages.OperationFinished(20),
                new BinaryTreeSetMessages.ContainsResult(50, false),
                new BinaryTreeSetMessages.ContainsResult(70, true),
                new BinaryTreeSetMessages.ContainsResult(80, false),
                new BinaryTreeSetMessages.OperationFinished(100)
            };

            Verify(requester, ops, expectedReplies);
        }

        [Test]
        public void GcTest()
        {
            Within(TimeSpan.FromSeconds(5), () =>
            {
                var topNode = Sys.ActorOf(Props.Create<BinaryTreeSet>());
                topNode.Tell(new BinaryTreeSetMessages.Insert(TestActor, 1, 1));
                topNode.Tell(new BinaryTreeSetMessages.Insert(TestActor, 2, 2));
                topNode.Tell(new BinaryTreeSetMessages.Insert(TestActor, 3, 3));
                ExpectMsg(new BinaryTreeSetMessages.OperationFinished(1), OperationFinishedComparerMethod);
                ExpectMsg(new BinaryTreeSetMessages.OperationFinished(2), OperationFinishedComparerMethod);
                ExpectMsg(new BinaryTreeSetMessages.OperationFinished(3), OperationFinishedComparerMethod);
                topNode.Tell(new BinaryTreeSetMessages.Gc());
                topNode.Tell(new BinaryTreeSetMessages.Insert(TestActor, 4, 4));
                topNode.Tell(new BinaryTreeSetMessages.Insert(TestActor, 5, 5));
                System.Threading.Thread.Sleep(2000);
                topNode.Tell(new BinaryTreeSetMessages.Insert(TestActor, 6, 6));
                topNode.Tell(new BinaryTreeSetMessages.Insert(TestActor, 7, 7));
                ExpectMsg(new BinaryTreeSetMessages.OperationFinished(4), OperationFinishedComparerMethod);
                ExpectMsg(new BinaryTreeSetMessages.OperationFinished(5), OperationFinishedComparerMethod);
                ExpectMsg(new BinaryTreeSetMessages.OperationFinished(6), OperationFinishedComparerMethod);
                ExpectMsg(new BinaryTreeSetMessages.OperationFinished(7), OperationFinishedComparerMethod);
            });
        }

        [Test]
        public void BehaveIdenticallyToBuiltInSet_IncludesGc()
        {
            var rnd = new Random();
            Func<IActorRef, int, List<BinaryTreeSetMessages.Operation>> randomOperations =
                (requester, count) =>
                {
                    Func<int> randomElement = () => rnd.Next(100);
                    Func<int, BinaryTreeSetMessages.Operation> randomOperation =
                        id =>
                        {
                            switch (rnd.Next(4))
                            {
                                case 0:
                                case 1:
                                    return new BinaryTreeSetMessages.Insert(requester, id, randomElement());
                                case 2:
                                    return new BinaryTreeSetMessages.Contains(requester, id, randomElement());
                                default:
                                    return new BinaryTreeSetMessages.Remove(requester, id, randomElement());
                            }
                        };
                    return Enumerable.Range(0, count).Select(randomOperation).ToList();
                };
            Func<IEnumerable<BinaryTreeSetMessages.Operation>, List<BinaryTreeSetMessages.OperationReply>> referenceReplies =
                operations =>
                {
                    var referenceSet = new HashSet<int>();
                    Func<BinaryTreeSetMessages.Operation, BinaryTreeSetMessages.OperationReply> replyFor =
                        operation =>
                        {
                            var insert = operation as BinaryTreeSetMessages.Insert;
                            if (insert != null)
                            {
                                referenceSet.Add(insert.Elem);
                                return new BinaryTreeSetMessages.OperationFinished(insert.Id);
                            }

                            var remove = operation as BinaryTreeSetMessages.Remove;
                            if (remove != null)
                            {
                                referenceSet.Remove(remove.Elem);
                                return new BinaryTreeSetMessages.OperationFinished(remove.Id);
                            }

                            var contains = operation as BinaryTreeSetMessages.Contains;
                            if (contains != null)
                            {
                                return new BinaryTreeSetMessages.ContainsResult(contains.Id, referenceSet.Contains(contains.Elem));
                            }

                            throw new ArgumentException("Unrecognised operation", nameof(operation));
                        };
                    return operations.Select(replyFor).ToList();
                };

            var probe = CreateTestProbe();
            var requesterRef = probe.Ref;

            var ops = randomOperations(requesterRef, 100);
            var expectedReplies = referenceReplies(ops);

            var topNode = Sys.ActorOf(Props.Create<BinaryTreeSet>());
            ops.ForEach(op =>
            {
                topNode.Tell(op);
                System.Threading.Thread.Sleep(5);
                if (rnd.NextDouble() < 0.1) topNode.Tell(new BinaryTreeSetMessages.Gc());
            });
            ReceiveN(probe, ops, expectedReplies);
        }

        private void ReceiveN(
            TestProbe probe,
            List<BinaryTreeSetMessages.Operation> ops,
            List<BinaryTreeSetMessages.OperationReply> expectedReplies)
        {
            probe.Within(TimeSpan.FromSeconds(5), () =>
            {
                var repliesUnsorted = probe.ReceiveN(ops.Count).OfType<BinaryTreeSetMessages.OperationReply>();
                var replies = repliesUnsorted.OrderBy(msg => msg.Id);
                if (!replies.SequenceEqual(expectedReplies, new OperationReplyEqualityComparer()))
                {
                    Assert.Fail("replies != expectedReplies");
                }
            });
        }

        private void Verify(
            TestProbe probe,
            List<BinaryTreeSetMessages.Operation> ops,
            List<BinaryTreeSetMessages.OperationReply> expectedReplies)
        {
            var topNode = Sys.ActorOf(Props.Create<BinaryTreeSet>());
            ops.ForEach(topNode.Tell);
            ReceiveN(probe, ops, expectedReplies);
        }

        private static bool OperationFinishedComparerMethod(
            BinaryTreeSetMessages.OperationFinished msg1,
            BinaryTreeSetMessages.OperationFinished msg2)
        {
            return msg1.Id == msg2.Id;
        }

        private static bool ContainsResultComparerMethod(
            BinaryTreeSetMessages.ContainsResult msg1,
            BinaryTreeSetMessages.ContainsResult msg2)
        {
            return
                msg1.Id == msg2.Id &&
                msg1.Result == msg2.Result;
        }

        private class OperationReplyEqualityComparer : IEqualityComparer<BinaryTreeSetMessages.OperationReply>
        {
            public bool Equals(
                BinaryTreeSetMessages.OperationReply msg1,
                BinaryTreeSetMessages.OperationReply msg2)
            {
                var of1 = msg1 as BinaryTreeSetMessages.OperationFinished;
                var of2 = msg2 as BinaryTreeSetMessages.OperationFinished;
                if (of1 != null && of2 != null) return OperationFinishedComparerMethod(of1, of2);

                var cr1 = msg1 as BinaryTreeSetMessages.ContainsResult;
                var cr2 = msg2 as BinaryTreeSetMessages.ContainsResult;
                if (cr1 != null && cr2 != null) return ContainsResultComparerMethod(cr1, cr2);

                return false;
            }

            public int GetHashCode(BinaryTreeSetMessages.OperationReply msg)
            {
                return msg.GetHashCode();
            }
        }
    }
}
