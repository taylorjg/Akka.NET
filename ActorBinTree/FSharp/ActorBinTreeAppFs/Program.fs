open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open ActorBinTree

[<EntryPoint>]
let main _ = 

    let config = ConfigurationFactory.Default()
    let system = System.create "ActorBinTree" config

    let bts = spawn system "BinaryTreeSet" binaryTreeSet

    bts <! Insert (bts, 1, 20)
    bts <! Contains (bts, 2, 20)
    bts <! Remove (bts, 3, 0)
    bts <! GC
    bts <! Insert (bts, 4, 10)
    bts <! Insert (bts, 5, 30)
    bts <! Insert (bts, 6, 5)
    bts <! Insert (bts, 7, 40)

    System.Threading.Thread.Sleep(500)

    system.Shutdown()
    0
