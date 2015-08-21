using Akka.Actor;

namespace ActorBinTree
{
    public class BinaryTreeSetMessages
    {
        public abstract class Operation
        {
            private readonly IActorRef _requester;
            private readonly int _id;
            private readonly int _elem;

            public Operation(IActorRef requester, int id, int elem)
            {
                _requester = requester;
                _id = id;
                _elem = elem;
            }

            public IActorRef Requester
            {
                get { return _requester; }
            }

            public int Id
            {
                get { return _id; }
            }

            public int Elem
            {
                get { return _elem; }
            }
        }

        public abstract class OperationReply
        {
            private readonly int _id;

            public OperationReply(int id)
            {
                _id = id;
            }

            public int Id
            {
                get { return _id; }
            }
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
            private readonly int _id;
            private readonly bool _result;

            public ContainsResult(int id, bool result) : base(id)
            {
                _id = id;
                _result = result;
            }

            public int Id
            {
                get { return _id; }
            }

            public bool Result
            {
                get { return _result; }
            }
        }

        public class OperationFinished : OperationReply
        {
            public OperationFinished(int id) : base(id)
            {
            }
        }
    }
}
