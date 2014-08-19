namespace Metricano

open System
open System.Collections.Generic
open System.Threading

/// Type alias for F# mailbox processor type
type Agent<'T> = MailboxProcessor<'T>

[<AutoOpen>]
module AgentExt =
    type MailboxProcessor<'T> with
        static member StartSupervised (body               : MailboxProcessor<_> -> Async<unit>, 
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

type MetricType =
    | TimeSpan = 1uy
    | Count    = 2uy

type Metric = 
    {
        Type                : MetricType
        Name                : string
        TimeStamp           : DateTime
        Unit                : string
        Namespace           : string
        mutable Average     : double
        mutable Sum         : double
        mutable Max         : double
        mutable Min         : double
        mutable Count       : double
    }

    static member Create ns metricType name timestamp unit = 
        {
            Type        = metricType
            Name        = name
            Unit        = unit
            Namespace   = ns + "/" + metricType.ToString()
            TimeStamp   = timestamp
            Average     = 0.0
            Sum         = 0.0
            Max         = 0.0
            Min         = 0.0
            Count       = 0.0
        }

    /// Operator for track a timespan to a TimeSpan metric
    static member (+=) (metric : Metric, timespan : TimeSpan) =
        metric.Sum        <- metric.Sum + timespan.TotalMilliseconds
        metric.Count      <- metric.Count + 1.0
        metric.Average    <- metric.Sum / metric.Count        

        if metric.Count = 1.0 then 
            // if this is the first sample, then it defines the min and max
            metric.Max    <- timespan.TotalMilliseconds
            metric.Min    <- timespan.TotalMilliseconds
        else 
            metric.Max    <- max timespan.TotalMilliseconds metric.Max
            metric.Min    <- min timespan.TotalMilliseconds metric.Min

    /// Operator for setting the count for a Count metric
    static member (+=) (metric : Metric, n) =
        metric.Sum        <- n
        metric.Count      <- 1.0
        metric.Average    <- n
        metric.Min        <- n
        metric.Max        <- n

    /// Operator for incrementing the count for a Count metric
    static member (++) (metric : Metric, n) = 
        metric.Sum        <- metric.Sum + n

        match metric.Count with
        | 0.0 -> metric.Max <- n
                 metric.Min <- n
        | _   -> metric.Max <- max metric.Max n
                 metric.Min <- min metric.Min n

        metric.Count      <- metric.Count + 1.0
        metric.Average    <- metric.Sum / metric.Count
        

type Message = | TimeSpan   of DateTime * string * TimeSpan
               | IncrCount  of DateTime * string * int64
               | SetCount   of DateTime * string * int64
               | Flush      of AsyncReplyChannel<Metric[]>

type MetricsAgent (ns : string) =
    static let getPeriodId (dt : DateTime) = uint64 <| dt.ToString("yyyyMMddHHmm")

    // the main message processing agent
    let agent = Agent<Message>.StartSupervised(fun inbox ->
        let metricsData = new Dictionary<uint64 * MetricType * string, Metric>()
        
        // registers a TimeSpan metric
        let regTimeSpanMetric timestamp metricName timespan =
            let key = getPeriodId timestamp, MetricType.TimeSpan, metricName
            match metricsData.TryGetValue key with
            | true, metric -> metric += timespan
            | false, _ -> 
                let metric = Metric.Create ns MetricType.TimeSpan metricName timestamp "Milliseconds"
                metric += timespan
                metricsData.[key] <- metric

        // registers a Count metric
        let regCountMetric timestamp metricName count update = 
            let key = getPeriodId timestamp, MetricType.Count, metricName
            match metricsData.TryGetValue key with
            | true, metric -> update metric (float count)
            | false, _ -> 
                let metric = Metric.Create ns MetricType.Count metricName timestamp "Count"
                metric ++ float count
                metricsData.[key] <- metric

        let rec loop () = async {
            let! msg = inbox.Receive()

            match msg with
            | TimeSpan(timestamp, metricName, timespan) ->
                // register the timespan metric
                regTimeSpanMetric timestamp metricName timespan
                return! loop()
            | IncrCount(timestamp, metricName, count) ->
                // register and increment the count metric
                regCountMetric timestamp metricName count (++)
                return! loop()
            | SetCount(timestamp, metricName, count) ->
                // register and set the count metric
                regCountMetric timestamp metricName count (+=)
                return! loop()
            | Flush(reply) ->
                // replies with all the current metrics, and clear the metrics dictionary
                reply.Reply(metricsData.Values |> Seq.toArray)
                metricsData.Clear()
                return! loop()
        }

        loop())

    member this.RecordTimeSpanMetric (metricName, timespan) = 
        TimeSpan(DateTime.UtcNow, metricName, timespan) |> agent.Post

    member this.IncrementCountMetric (metricName) =
        IncrCount(DateTime.UtcNow, metricName, 1L) |> agent.Post

    member this.IncrementCountMetricBy (metricName, n) =
        IncrCount(DateTime.UtcNow, metricName, n) |> agent.Post

    member this.SetCountMetric (metricName, n) =
        SetCount(DateTime.UtcNow, metricName, n) |> agent.Post

    member this.Flush () = agent.PostAndAsyncReply(fun reply -> Flush(reply)) |> Async.StartAsTask