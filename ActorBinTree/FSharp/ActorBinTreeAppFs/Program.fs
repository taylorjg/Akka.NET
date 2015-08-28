module ActorBinTreeAppFs

open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open ActorBinTreeFs

let client (bts: IActorRef) (mailbox: Actor<OperationReply>) =
    let self = mailbox.Context.Self
    bts <! Contains (self, 1, 20)
    bts <! Insert (self, 2, 20)
    bts <! Contains (self, 3, 20)
    bts <! Remove (self, 4, 20)
    bts <! Contains (self, 5, 20)
    bts <! GC
    bts <! Insert (self, 6, 10)
    bts <! Insert (self, 7, 30)
    bts <! Insert (self, 8, 5)
    bts <! Insert (self, 9, 40)
    let rec loop () =
        actor {
            let! msg = mailbox.Receive ()
            match msg with
            | OperationFinished id -> mailbox.Log.Value.Info("OperationFinished {0}", id)
            | ContainsResult (id, result) -> mailbox.Log.Value.Info("ContainsResult {0} {1}", id, result)
            return! loop ()
        }
    loop ()

[<EntryPoint>]
let main _ = 

    let config = ConfigurationFactory.Default()
    let system = System.create "ActorBinTree" config

    let bts = spawn system "BinaryTreeSet" binaryTreeSet
    let client = spawn system "Client" (client bts)

    System.Threading.Thread.Sleep(500)

    system.Shutdown()
    0
