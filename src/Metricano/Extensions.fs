namespace Metricano.Extensions

open System
open System.Threading

type Agent<'T> = MailboxProcessor<'T>

[<AutoOpen>]
module AgentExt =
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
