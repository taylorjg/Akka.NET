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
    | Right

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
