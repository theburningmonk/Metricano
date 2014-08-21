namespace Metricano.CloudWatch.Tests

open System
open System.Threading
open System.Threading.Tasks

open Amazon.CloudWatch.Model
open Foq
open FsUnit
open NUnit.Framework
open Metricano
open Metricano.Publisher

[<TestFixture>]
type ``CloudWatchPublisher tests`` = 
    [<Test>]
    member test.``metrics published across multiple publish intervals are aggregated into per minute metrics`` () =
        let req : PutMetricDataRequest ref = ref null
        let res = PutMetricDataResponse()

        let cloudWatch =
            Mock<Amazon.CloudWatch.IAmazonCloudWatch>()
                .Setup(fun x -> <@ x.PutMetricDataAsync(is(fun req' -> req := req'; true)) @>)
                .Returns(Task.FromResult res)
                .Create()

        ()