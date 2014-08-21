namespace Metricano.Extensions

open System
open System.Threading

/// Type alias for F# mailbox processor type
type Agent<'T> = MailboxProcessor<'T>

[<AutoOpen>]
module AgentExt =
    type MailboxProcessor<'T> with
        static member StartSupervised : body : (MailboxProcessor<'T> -> Async<unit>) * ?cancellationToken : CancellationToken * ?onRestart : (Exception -> unit) -> MailboxProcessor<'T>