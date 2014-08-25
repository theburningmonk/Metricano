namespace Metricano

open System
open System.Collections.Generic
open System.Threading.Tasks

open Metricano.Extensions

type MetricType =
    | TimeSpan = 1uy
    | Count    = 2uy

type CountMetric =
    {
        Name        : string
        TimeStamp   : DateTime
        Sum         : double
        Average     : double
        Max         : double
        Min         : double
        Count       : int64
    }

type TimeSpanMetric =
    {
        Name        : string
        TimeStamp   : DateTime
        Sum         : TimeSpan
        Average     : TimeSpan
        Max         : TimeSpan
        Min         : TimeSpan
        Raw         : TimeSpan[]
    }

type Metric = 
    | Count     of CountMetric
    | TimeSpan  of TimeSpanMetric

    member this.Name        = match this with | Count { Name = name } | TimeSpan { Name = name } -> name    
    member this.TimeStamp   = match this with | Count { TimeStamp = ts } | TimeSpan { TimeStamp = ts } -> ts
    member this.Type        = match this with | Count _ -> MetricType.Count | TimeSpan _ -> MetricType.TimeSpan
    member this.Unit        = match this with | Count _ -> "Count" | TimeSpan _ -> "Milliseconds"
    member this.Sum         = match this with | Count { Sum = sum } -> sum | TimeSpan { Sum = sum } -> sum.TotalMilliseconds
    member this.Average     = match this with | Count { Average = avg } -> avg | TimeSpan { Average = avg } -> avg.TotalMilliseconds
    member this.Max         = match this with | Count { Max = max } -> max | TimeSpan { Max = max } -> max.TotalMilliseconds
    member this.Min         = match this with | Count { Min = min } -> min | TimeSpan { Min = min } -> min.TotalMilliseconds
    member this.SampleCount = match this with 
                              | Count { Count = count } -> count
                              | TimeSpan { Raw = raw }  -> raw.LongLength

type CountMetricInternal =
    {
        Name            : string
        TimeStamp       : DateTime
        mutable Sum     : double
        mutable Average : double
        mutable Max     : double
        mutable Min     : double
        mutable Count   : int64
    }

    static member Create name timeStamp =
        {
            Name        = name
            TimeStamp   = timeStamp
            Average     = 0.0
            Sum         = 0.0
            Max         = 0.0
            Min         = 0.0
            Count       = 0L
        }

    /// Resets the count metric
    static member (?=) (metric : CountMetricInternal, n) =
        metric.Sum        <- n
        metric.Count      <- 1L
        metric.Average    <- n
        metric.Min        <- n
        metric.Max        <- n

    /// Add data point to the count metric
    static member (++) (metric : CountMetricInternal, n) = 
        metric.Sum         <- metric.Sum + n

        match metric.Count with
        | 0L -> metric.Max <- n
                metric.Min <- n
        | _  -> metric.Max <- max metric.Max n
                metric.Min <- min metric.Min n

        metric.Count       <- metric.Count + 1L
        metric.Average     <- metric.Sum / double metric.Count

type TimeSpanMetricInternal =
    {
        Name        : string
        TimeStamp   : DateTime
        Raw         : List<TimeSpan>
    }

    static member Create name timeStamp =
        {
            Name        = name
            TimeStamp   = timeStamp
            Raw         = new List<TimeSpan>()
        }

type MetricInternal =
    | Count     of CountMetricInternal
    | TimeSpan  of TimeSpanMetricInternal

type IMetricsAgent =
    abstract member RecordTimeSpanMetric   : string * TimeSpan -> unit
    abstract member IncrementCountMetric   : string -> unit
    abstract member IncrementCountMetricBy : string * int64 -> unit
    abstract member SetCountMetric         : string * int64 -> unit
    abstract member Flush                  : unit -> Task<Metric[]>
            
type Message = | AddTimeSpan  of DateTime * string * TimeSpan
               | IncrCount    of DateTime * string * int64
               | SetCount     of DateTime * string * int64
               | Flush        of AsyncReplyChannel<Metric[]>

[<Sealed>]
type MetricsAgent private () =
    static let defaultAgent = MetricsAgent() :> IMetricsAgent

    static let getPeriodId (dt : DateTime) = uint64 <| dt.ToString("yyyyMMddHHmmss")
    static let maxBacklog = 600 // keep a maximum of 10 mins worth of data in the backlog

    static let toMetric = function
        | TimeSpan timeMetric -> 
            let raw = timeMetric.Raw.ToArray()
            Metric.TimeSpan {
                                Name        = timeMetric.Name
                                TimeStamp   = timeMetric.TimeStamp
                                Sum         = raw |> Array.sumBy (fun ts -> ts.Ticks) |> TimeSpan.FromTicks
                                Average     = raw |> Array.averageBy (fun ts -> ts.TotalMilliseconds) |> TimeSpan.FromMilliseconds
                                Max         = raw |> Array.max
                                Min         = raw |> Array.min
                                Raw         = raw
                            }
        | Count countMetric ->
            Metric.Count {
                                Name        = countMetric.Name
                                TimeStamp   = countMetric.TimeStamp
                                Sum         = countMetric.Sum
                                Average     = countMetric.Average
                                Max         = countMetric.Max
                                Min         = countMetric.Min
                                Count       = countMetric.Count
                         }

    // the main message processing agent
    let agent = Agent<Message>.StartSupervised(fun inbox ->
        let metricsData = new Dictionary<uint64 * MetricType * string, MetricInternal>()
        
        // registers a TimeSpan metric
        let regTimeSpanMetric timestamp metricName timeSpan =
            let key = getPeriodId timestamp, MetricType.TimeSpan, metricName
            match metricsData.TryGetValue key with
            | true, TimeSpan metric -> metric.Raw.Add timeSpan
            | false, _ -> 
                let metric = TimeSpanMetricInternal.Create metricName timestamp
                metric.Raw.Add timeSpan
                metricsData.[key] <- TimeSpan metric

        // registers a Count metric
        let regCountMetric timestamp metricName count update = 
            let key = getPeriodId timestamp, MetricType.Count, metricName
            match metricsData.TryGetValue key with
            | true, Count metric -> update metric (float count)
            | true, _  -> () // ignore this case as it should never happen
            | false, _ -> 
                let metric = CountMetricInternal.Create metricName timestamp
                metric ++ double count
                metricsData.[key] <- Count metric

        let rec loop () = async {
            if metricsData.Count > maxBacklog then
                let toDelete = metricsData.Keys |> Seq.sort |> Seq.skip maxBacklog |> Seq.toArray
                toDelete |> Array.iter (metricsData.Remove >> ignore)

            let! msg = inbox.Receive()

            match msg with
            | AddTimeSpan(timestamp, metricName, timeSpan) ->
                // register the timespan metric
                regTimeSpanMetric timestamp metricName timeSpan
                return! loop()
            | IncrCount(timestamp, metricName, count) ->
                // register and increment the count metric
                regCountMetric timestamp metricName count (++)
                return! loop()
            | SetCount(timestamp, metricName, count) ->
                // register and set the count metric
                regCountMetric timestamp metricName count (?=)
                return! loop()
            | Flush(reply) ->
                // replies with all the current metrics, and clear the metrics dictionary
                reply.Reply(metricsData.Values |> Seq.map toMetric |> Seq.toArray)
                metricsData.Clear()
                return! loop()
        }

        loop())

    interface IMetricsAgent with
        member this.RecordTimeSpanMetric (metricName, timespan) = agent.Post <| AddTimeSpan(DateTime.UtcNow, metricName, timespan)
        member this.IncrementCountMetric (metricName)           = agent.Post <| IncrCount(DateTime.UtcNow, metricName, 1L)
        member this.IncrementCountMetricBy (metricName, n)      = agent.Post <| IncrCount(DateTime.UtcNow, metricName, n)
        member this.SetCountMetric (metricName, n)              = agent.Post <| SetCount(DateTime.UtcNow, metricName, n)
        member this.Flush ()                                    = agent.PostAndAsyncReply(fun reply -> Flush(reply)) |> Async.StartAsTask

    static member Default   = defaultAgent
    static member Create () = MetricsAgent() :> IMetricsAgent

type IMetricsPublisher =
    inherit IDisposable

    abstract member Publish : Metric[] -> Task