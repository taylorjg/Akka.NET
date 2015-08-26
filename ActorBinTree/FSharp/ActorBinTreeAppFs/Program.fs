open Akka.Actor
open Akka.FSharp
open ActorBinTreeFs.BinaryTreeSet

[<EntryPoint>]
let main _ = 

    aref <! Insert (aref, 1, 1)
    aref <! Contains (aref, 1, 1)
    aref <! Remove (aref, 1, 1)
    aref <! GC

    System.Threading.Thread.Sleep(500)

    system.Shutdown()
    0
