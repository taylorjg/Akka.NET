module ActorBinTree

open Akka.Actor
open Akka.Configuration
open Akka.FSharp

let config = ConfigurationFactory.Default()
let system = System.create "my-system" config

type Message =
    | Insert of IActorRef * int * int
    | Contains of IActorRef * int * int
    | Remove of IActorRef * int * int
    | GC
    | ContainsResult of int * bool
    | OperationFinished of int
    | CopyTo of IActorRef
    | CopyFinished

// type Actor<'Message> =
//    inherit Akka.Actor.IActorRefFactory
//    inherit Akka.Actor.ICanWatch
//    abstract member Context : IActorContext
//    abstract member Defer : (unit -> unit) -> unit
//    abstract member Log : System.Lazy<Event.ILoggingAdapter>
//    abstract member Receive : unit -> IO<'Message>
//    abstract member Self : IActorRef
//    abstract member Sender : unit -> IActorRef
//    abstract member Stash : unit -> unit
//    abstract member Unhandled : 'Message -> unit
//    abstract member Unstash : unit -> unit
//    abstract member UnstashAll : unit -> unit

// Gives access to the next message throu let! binding in actor computation expression.
//
// type Cont<'In, 'Out> =
//    | Func of 'In -> Cont<'In,'Out>
//    | Return of 'Out

// type ActorBuilder =
//    new : unit -> ActorBuilder
//    /// Binds the result of another actor computation expression.
//    member Bind : x:Cont<'In,'Out1> * f:('Out1 -> Cont<'In,'Out2>) -> Cont<'In,'Out2>
//    /// Binds the next message.
//    member Bind : m:IO<'In> * f:('In -> Cont<'In,'T839767>) -> Cont<'In,'T839767>
//    member Combine : f:Cont<'In,'T839817> * g:Cont<'In,'Out> -> Cont<'In,'Out>
//    member Combine : f:(unit -> Cont<'In,'T839813>) * g:Cont<'In,'Out> -> Cont<'In,'Out>
//    member Combine : f:Cont<'In,'T839809> * g:(unit -> Cont<'In,'Out>) -> Cont<'In,'Out>
//    member Combine : f:(unit -> Cont<'In,'T839805>) * g:(unit -> Cont<'In,'Out>) -> Cont<'In,'Out>
//    member Delay : f:(unit -> Cont<'T839795,'T839796>) -> unit -> Cont<'T839795,'T839796>
//    member For : source:seq<'Iter> * f:('Iter -> Cont<'In,unit>) -> Cont<'In,unit>
//    member Return : x:'T839775 -> Cont<'T839776,'T839775>
//    member ReturnFrom : x:'T839773 -> 'T839773
//    member Run : f:Cont<'T839801,'T839802> -> Cont<'T839801,'T839802>
//    member Run : f:(unit -> Cont<'T839798,'T839799>) -> Cont<'T839798,'T839799>
//    member TryFinally : f:(unit -> Cont<'In,'Out>) * fnl:(unit -> unit) -> Cont<'In,'Out>
//    member TryWith : f:(unit -> Cont<'In,'Out>) * c:(exn -> Cont<'In,'Out>) -> Cont<'In,'Out>
//    member Using : d:'T839786 * f:('T839786 -> Cont<'In,'Out>) -> Cont<'In,'Out> when 'T839786 :> System.IDisposable and 'T839786 : equality and 'T839786 : null
//    member While : condition:(unit -> bool) * f:(unit -> Cont<'In,unit>) -> Cont<'In,unit>
//    member Zero : unit -> Cont<'T839778,unit>

// Spawns an actor using specified actor computation expression.  The actor can only be used locally.
// actorFactory: Either actor system or parent actor
// name: Name of spawned child actor
// f: Used by actor for handling response for incoming request
//
// val spawn :
//  actorFactory:IActorRefFactory ->
//  name:string ->
//  f:(Actor<'Message> -> Cont<'Message,'Returned>) ->
//  IActorRef

// Wraps provided function with actor behavior. It will be invoked each time, an actor will receive a message.
//
// val actorOf : fn:('Message -> unit) -> mailbox:Actor<'Message> -> Cont<'Message,'Returned>

// Wraps provided function with actor behavior. It will be invoked each time, an actor will receive a message.
//
// val actorOf2 : fn:(Actor<'Message> -> 'Message -> unit) -> mailbox:Actor<'Message> -> Cont<'Message,'Returned>

let binaryTreeNode (mailbox: Actor<Message>) =
    let rec normal () =
        actor {
            let! msg = mailbox.Receive ()
            match msg with
            | Insert (r, id, e) -> printfn "Insert %d" e
            | Contains (r, id, e) -> printfn "Contains %d" e
            | Remove (r, id, e) -> printfn "Remove %d" e
            | _ as m -> printfn "Unexpected message: %A" m
            return! normal ()
        }
    normal ()

let binaryTreeSet (mailbox: Actor<Message>) =
    let rec normal (root: IActorRef) =
        actor {
            let! msg = mailbox.Receive ()
            match msg with
            | Insert (r, id, e) -> root.Forward msg
            | Contains (r, id, e) -> root.Forward msg
            | Remove (r, id, e) -> root.Forward msg
            | GC -> printfn "GC"
            | _ as m -> printfn "Unexpected message: %A" m
            return! normal root
        }
    and garbageCollecting (pending: Message list) (newRoot: IActorRef) =
        actor {
            let! msg = mailbox.Receive ()
            match msg with
            | Insert (r, id, e) -> return! garbageCollecting (msg::pending) newRoot
            | Contains (r, id, e) -> return! garbageCollecting (msg::pending) newRoot
            | Remove (r, id, e) -> return! garbageCollecting (msg::pending) newRoot
            | GC -> printfn "Ignoring GC whilst already garbage collecting"
            | CopyFinished -> return! normal newRoot
            | _ as m -> printfn "Unexpected message: %A" m
            return! garbageCollecting pending newRoot
        }
    normal <| spawn mailbox.Context.System "root" binaryTreeNode

//let binaryTreeSetHandler (mailbox: Actor<Message>) msg = 
//    match msg with
//    | Insert (r, id, e) -> r <! msg
//    | Contains (r, id, e) -> r <! msg
//    | Remove (r, id, e) -> r <! msg
//    | GC -> printfn "GC"
//    | CopyFinished -> printfn "CopyFinished"
//    | _ as m -> printfn "Unexpected message: %A" m
//
//let binaryTreeNodeHandler (mailbox: Actor<Message>) msg = 
//    match msg with
//    | Insert (r, id, e) -> printfn "Insert %d" e
//    | Contains (r, id, e) -> printfn "Contains %d" e
//    | Remove (r, id, e) -> printfn "Remove %d" e
//    | CopyTo treeNode -> ()
//    | _ as m -> printfn "Unexpected message: %A" m
