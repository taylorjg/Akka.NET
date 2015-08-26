module ActorBinTreeFs.BinaryTreeSet

open Akka.Actor
open Akka.FSharp

let config = Akka.FSharp.Configuration.defaultConfig()
let system = System.create "my-system" config

type BinaryTreeSetMessage =
    | Insert of IActorRef * int * int
    | Contains of IActorRef * int * int
    | Remove of IActorRef * int * int
    | GC

type BinaryTreeSetMessageReply =
    | ContainsResult of int * bool
    | OperationFinished of int

let handleMessage (mailbox: Actor<BinaryTreeSetMessage>) msg = 
    match msg with
    | Insert (r, id, e) -> printfn "Insert %d" e
    | Contains (r, id, e) -> printfn "Contains %d" e
    | Remove (r, id, e) -> printfn "Remove %d" e
    | GC -> printfn "GC"

let aref = spawn system "my-actor" (actorOf2 handleMessage)
let aref2 = spawn system "black-hole" (actorOf (fun _ -> ()))
