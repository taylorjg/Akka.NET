module ActorBinTree

open Akka.Actor
open Akka.Configuration
open Akka.FSharp

type Message =
    | Insert of IActorRef * int * int
    | Contains of IActorRef * int * int
    | Remove of IActorRef * int * int
    | GC
    | ContainsResult of int * bool
    | OperationFinished of int
    | CopyTo of IActorRef
    | CopyFinished

type private Position =
    | Left
    |Right

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

let rec private binaryTreeNode elem removed (mailbox: Actor<Message>) =
    let rec normal elem removed (subtrees: Map<Position, IActorRef>) =
        actor {
            let! msg = mailbox.Receive ()
            match msg with
            | Insert (r, id, e) ->
                mailbox.Log.Value.Info("Insert {0}", e)
                if e = elem
                    then
                        r <! OperationFinished id
                        return! normal elem false subtrees
                    else
                        if e < elem
                            then
                                match Map.tryFind Left subtrees with
                                    | Some n -> n <! msg
                                    | None ->
                                        let n = spawn mailbox "Left" (binaryTreeNode e false)
                                        let subtrees' = Map.add Left n subtrees
                                        r <! OperationFinished id
                                        return! normal elem removed subtrees'
                            else
                                match Map.tryFind Right subtrees with
                                    | Some n -> n <! msg
                                    | None ->
                                        let n = spawn mailbox "Right" (binaryTreeNode e false)
                                        let subtrees' = Map.add Right n subtrees
                                        r <! OperationFinished id
                                        return! normal elem removed subtrees'
            | Contains (r, id, e) ->
                mailbox.Log.Value.Info("Contains {0}", e)
                if e = elem
                    then r <! ContainsResult (id, not removed)
                    else if e < elem
                        then
                            match Map.tryFind Left subtrees with
                                | Some n -> n <! msg
                                | None -> r <! ContainsResult (id, false)
                        else
                            match Map.tryFind Right subtrees with
                                | Some n -> n <! msg
                                | None -> r <! ContainsResult (id, false)
            | Remove (r, id, e) ->
                mailbox.Log.Value.Info("Remove {0}", e)
            | CopyTo newRoot ->
                mailbox.Log.Value.Info "CopyTo"
                mailbox.Context.Parent <! CopyFinished
            | _ as m -> mailbox.Log.Value.Info("Unexpected message: {0}", m)
            return! normal elem removed subtrees
        }
    normal elem removed Map.empty

let binaryTreeSet (mailbox: Actor<Message>) =
    let rec normal (root: IActorRef) =
        mailbox.Log.Value.Info "Becoming normal"
        actor {
            let! msg = mailbox.Receive ()
            match msg with
            | Insert _ -> root.Forward msg
            | Contains _ -> root.Forward msg
            | Remove _ -> root.Forward msg
            | GC ->
                mailbox.Log.Value.Info "GC in binaryTreeSet"
                root <! CopyTo root
                return! garbageCollecting [] root
            | _ as m -> mailbox.Log.Value.Info("Unexpected message: {0}", m)
            return! normal root
        }
    and garbageCollecting (pending: Message list) (newRoot: IActorRef) =
        mailbox.Log.Value.Info "Becoming garbageCollecting"
        actor {
            let! msg = mailbox.Receive ()
            match msg with
            | Insert _ ->
                mailbox.Log.Value.Info "Enqueuing Insert"
                return! garbageCollecting (msg::pending) newRoot
            | Contains _ ->
                mailbox.Log.Value.Info "Enqueuing Contains"
                return! garbageCollecting (msg::pending) newRoot
            | Remove _ ->
                mailbox.Log.Value.Info "Enqueuing Remove"
                return! garbageCollecting (msg::pending) newRoot
            | GC -> mailbox.Log.Value.Info "Ignoring GC whilst already garbage collecting"
            | CopyFinished ->
                (List.map (fun msg -> newRoot <! msg) <| List.rev pending) |> ignore
                return! normal newRoot
            | _ as m -> mailbox.Log.Value.Info("Unexpected message: {0}", m)
            return! garbageCollecting pending newRoot
        }
    normal <| spawn mailbox "root" (binaryTreeNode 0 true)
