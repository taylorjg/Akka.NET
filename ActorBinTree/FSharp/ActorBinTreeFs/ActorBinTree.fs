module ActorBinTreeFs

open Akka.Actor
open Akka.Configuration
open Akka.FSharp

type Operation =
    | Insert of IActorRef * int * int
    | Contains of IActorRef * int * int
    | Remove of IActorRef * int * int

type OperationReply =
    | ContainsResult of int * bool
    | OperationFinished of int

type GC =
    | GC

type private InternalMessage =
    | CopyTo of IActorRef
    | CopyFinished

type private Position =
    | Left
    | Right

let rec private binaryTreeNode elem removed (mailbox: Actor<obj>) =
    let rec normal elem removed (subtrees: Map<Position, IActorRef>) =
        actor {
            let! msg = mailbox.Receive ()
            match msg with
            | :? Operation as op ->
                match op with
                | Insert (r, id, e) ->
                    mailbox.Log.Value.Info("Insert {0}", e)
                    if e = elem
                        then
                            r <! OperationFinished id
                            return! normal elem false subtrees
                        else if e < elem
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
                    if e = elem
                        then
                            r <! OperationFinished id
                            return! normal elem true subtrees
                        else if e < elem
                            then
                                match Map.tryFind Left subtrees with
                                    | Some n -> n <! msg
                                    | None -> r <! OperationFinished id
                            else
                                match Map.tryFind Right subtrees with
                                    | Some n -> n <! msg
                                    | None -> r <! OperationFinished id
            | :? InternalMessage as imsg ->
                match imsg with
                | CopyTo newRoot ->
                    mailbox.Log.Value.Info "CopyTo"
                    mailbox.Context.Parent <! CopyFinished
                | _ -> mailbox.Log.Value.Info("Unexpected internal message: {0}", msg)
            | _ -> mailbox.Log.Value.Info("Unexpected message: {0}", msg)
            return! normal elem removed subtrees
        }
    normal elem removed Map.empty

let binaryTreeSet (mailbox: Actor<obj>) =
    let rec normal (root: IActorRef) =
        mailbox.Log.Value.Info "Becoming normal"
        actor {
            let! msg = mailbox.Receive ()
            match msg with
            | :? Operation as op ->
                mailbox.Log.Value.Info ("Forwarding {0}", op)
                root.Forward op
            | :? GC ->
                mailbox.Log.Value.Info "GC in binaryTreeSet"
                root <! CopyTo root
                return! garbageCollecting [] root
            | _ -> mailbox.Log.Value.Info("Unexpected message: {0}", msg)
            return! normal root
        }
    and garbageCollecting (pending: Operation list) (newRoot: IActorRef) =
        mailbox.Log.Value.Info "Becoming garbageCollecting"
        actor {
            let! msg = mailbox.Receive ()
            match msg with
            | :? Operation as op ->
                mailbox.Log.Value.Info ("Enqueuing {0}", op)
                return! garbageCollecting (op::pending) newRoot
            | :? GC -> mailbox.Log.Value.Info "Ignoring GC whilst already garbage collecting"
            | :? InternalMessage as imsg ->
                match imsg with
                | CopyFinished ->
                    (List.map (fun op -> newRoot <! op) <| List.rev pending) |> ignore
                    return! normal newRoot
                | _ -> mailbox.Log.Value.Info("Unexpected internal message: {0}", msg)
            | _ -> mailbox.Log.Value.Info("Unexpected message: {0}", msg)
            return! garbageCollecting pending newRoot
        }
    normal <| spawn mailbox "root" (binaryTreeNode 0 true)
