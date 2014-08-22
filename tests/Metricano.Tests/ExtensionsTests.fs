namespace Metricano.Tests

open System

open FsUnit
open NUnit.Framework

open Metricano
open Metricano.Extensions

type Message = 
    | Throw
    | Return    of AsyncReplyChannel<int>

[<TestFixture>]
type ``Extensions tests`` () =
    [<Test>]
    member test.``StartSupervised should auto-restart agents after unhandled exception`` () =
        let agent = Agent.StartSupervised(fun inbox ->             
            let n = ref 0

            async {

                while true do
                    let! msg = inbox.Receive()
                    match msg with
                    | Throw        -> failwith "boom"
                    | Return reply -> reply.Reply !n; n := !n + 1
            })

        agent.PostAndReply (fun reply -> Return reply) |> should equal 0
        agent.PostAndReply (fun reply -> Return reply) |> should equal 1

        agent.Post Throw

        // after the agent except it should be restarted and able to serve answers again
        agent.PostAndReply (fun reply -> Return reply) |> should equal 0