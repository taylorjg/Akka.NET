using Akka.Actor;

namespace ActorBinTree
{
    public static class BinaryTreeSetMessages
    {
        public abstract class Operation
        {
            protected Operation(IActorRef requester, int id, int elem)
            {
                Requester = requester;
                Id = id;
                Elem = elem;
            }

            public IActorRef Requester { get; }
            public int Id { get; }
            public int Elem { get; }
        }

        public abstract class OperationReply
        {
            protected OperationReply(int id)
            {
                Id = id;
            }

            public int Id { get; }
        }

        public class Insert : Operation
        {
            public Insert(IActorRef requester, int id, int elem) : base(requester, id, elem)
            {
            }
        }

        public class Contains : Operation
        {
            public Contains(IActorRef requester, int id, int elem) : base(requester, id, elem)
            {
            }
        }

        public class Remove : Operation
        {
            public Remove(IActorRef requester, int id, int elem) : base(requester, id, elem)
            {
            }
        }

        public class Gc
        {
        }

        public class ContainsResult : OperationReply
        {
            public ContainsResult(int id, bool result) : base(id)
            {
                Result = result;
            }

            public bool Result { get; }
        }

        public class OperationFinished : OperationReply
        {
            public OperationFinished(int id) : base(id)
            {
            }
        }
    }
}
