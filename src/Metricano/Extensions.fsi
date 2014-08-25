namespace Metricano.Extensions

open System
open System.Threading
open System.Threading.Tasks

/// Type alias for F# mailbox processor type
type Agent<'T> = MailboxProcessor<'T>

[<AutoOpen>]
module Extensions =
    type MailboxProcessor<'T> with
        static member StartSupervised : body : (MailboxProcessor<'T> -> Async<unit>) * ?cancellationToken : CancellationToken * ?onRestart : (Exception -> unit) -> MailboxProcessor<'T>

    type Async with
        static member AwaitPlainTask    : Task -> Async<unit>
        static member StartAsPlainTask  : Async<unit> -> Task