namespace Metricano.Publisher

open System.Threading.Tasks
open Metricano

type CloudWatchPublisher () =    
    interface IMetricsPublisher with
        member this.Publish (metrics) = Task.Delay(1)