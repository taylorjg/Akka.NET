open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open ActorBinTree

[<EntryPoint>]
let main _ = 

    let config = ConfigurationFactory.Default()
    let system = System.create "ActorBinTree" config

    let a = spawn system "BinaryTreeSet" binaryTreeSet

    a <! Insert (a, 1, 1)
    a <! Contains (a, 1, 1)
    a <! Remove (a, 1, 1)
    a <! GC

    System.Threading.Thread.Sleep(500)

    system.Shutdown()
    0
