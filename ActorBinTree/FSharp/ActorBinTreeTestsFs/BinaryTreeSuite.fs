module ActorBinTreeTestsFs

open Akka.FSharp
open Akka.TestKit.NUnit
open ActorBinTreeFs

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
