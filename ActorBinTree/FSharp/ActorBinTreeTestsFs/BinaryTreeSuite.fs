module ActorBinTreeTestsFs

open Akka.FSharp
open Akka.TestKit.NUnit
open ActorBinTreeFs
open System

[<NUnit.Framework.TestFixture>]
type BinaryTreeSuiteFs () =
    inherit TestKit ()

    [<NUnit.Framework.Test>]
    member this.ProperInsertsAndLookups ()  =
        let topNode = spawn this.Sys "BinaryTreeSet" binaryTreeSet

        topNode <! Contains (this.TestActor, 1, 1)
        this.ExpectMsg (ContainsResult (1, false)) |> ignore

        topNode <! Insert (this.TestActor, 2, 1)
        topNode <! Contains (this.TestActor, 3, 1)

        this.ExpectMsg (OperationFinished 2) |> ignore
        this.ExpectMsg (ContainsResult (3, true)) |> ignore

    [<NUnit.Framework.Test>]
    member this.InstructionExample ()  =
        let probe = this.CreateTestProbe ()
        let requesterRef = probe.Ref

        let ops = [
            Insert (requesterRef, 100, 1);
            Contains (requesterRef, 50, 2);
            Remove (requesterRef, 10, 1);
            Insert (requesterRef, 20, 2);
            Contains (requesterRef, 80, 1);
            Contains (requesterRef, 70, 2)
        ]

        let expectedReplies = [
            OperationFinished (10);
            OperationFinished (20);
            ContainsResult(50, false);
            ContainsResult(70, true);
            ContainsResult(80, false);
            OperationFinished (100);
        ]

        this.Verify probe ops expectedReplies

    member private this.Verify probe ops expectedReplies =
        let topNode = spawn this.Sys "BinaryTreeSet" binaryTreeSet
        List.map (fun op -> topNode <! op) ops |> ignore
        this.CheckExpectedReplies probe ops expectedReplies

    member private this.CheckExpectedReplies probe ops expectedReplies =
        let max = TimeSpan.FromSeconds (float 5)
        let action () =
            let repliesUnsorted = probe.ReceiveN (List.length ops)
            let replies = this.SortReplies repliesUnsorted
            match replies <> expectedReplies with
            | true -> NUnit.Framework.Assert.Fail "replies <> expectedReplies"
            | false -> ()
        probe.Within (max, action) 

    member private this.SortReplies repliesUnsorted =
        let isOperationReply (msg: obj) =
            match msg with
            | :? OperationReply -> true
            | _ -> false
        let idFromMsg (msg: obj) =
            match msg with
            | :? OperationReply as opr ->
                match opr with
                | OperationFinished id -> id
                | ContainsResult (id, _) -> id
            | _ -> 0
        let replies: list<OperationReply> = Seq.toList <| Seq.cast (query {
            for msg in repliesUnsorted do
            where (isOperationReply msg)
            sortBy (idFromMsg msg)
            select msg
        })
        replies
