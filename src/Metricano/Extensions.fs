namespace Metricano.Extensions

open System
open System.Threading
open System.Threading.Tasks

type Agent<'T> = MailboxProcessor<'T>

[<AutoOpen>]
module Extensions =
    type MailboxProcessor<'T> with
        static member StartSupervised (body               : MailboxProcessor<'T> -> Async<unit>, 
                                       ?cancellationToken : CancellationToken,
                                       ?onRestart         : Exception -> unit) = 
            let watchdog f x = async {
                while true do
                    let! result = Async.Catch(f x)
                    match result, onRestart with
                    | Choice2Of2 exn, Some g -> g(exn)
                    | _ -> ()                    
            }

            Agent.Start((fun inbox -> watchdog body inbox), ?cancellationToken = cancellationToken)

    type Async with
        static member AwaitPlainTask (task: Task) = 
            let continuation (t : Task) : unit =
                match t.IsFaulted with
                | true -> raise t.Exception
                | _    -> ()
            task.ContinueWith continuation |> Async.AwaitTask

        static member StartAsPlainTask (work : Async<unit>) = 
            Task.Factory.StartNew(fun () -> work |> Async.RunSynchronously)