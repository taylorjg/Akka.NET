open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open ActorBinTree

[<EntryPoint>]
let main _ = 

    let config = ConfigurationFactory.Default()
    let system = System.create "ActorBinTree" config

    let bts = spawn system "BinaryTreeSet" binaryTreeSet

    bts <! Insert (bts, 1, 1)
    bts <! Contains (bts, 2, 1)
    bts <! Remove (bts, 3, 1)
    bts <! GC
    bts <! Insert (bts, 4, 2)
    bts <! Insert (bts, 5, 3)
    bts <! Insert (bts, 6, 4)

    System.Threading.Thread.Sleep(500)

    system.Shutdown()
    0
