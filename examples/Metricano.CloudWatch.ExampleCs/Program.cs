using System;
using System.Threading;

using Amazon;

using Metricano.Publisher;

namespace Metricano.CloudWatch.ExampleCs
{
    /// <summary>
    /// Let this run for couple of minutes to generate data points in CloudWatch.
    /// Press Ctrl+C or close the console window to stop it.
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            Publish.With(new CloudWatchPublisher(
                "MetricanoDemo", 
                "YOUR_AWS_KEY_HERE", 
                "YOUR_AWS_SECRET_HERE", 
                RegionEndpoint.USEast1));

            const string CountMetric = "CountMetric";
            const string TimeMetric = "TimeMetric";
            var rand = new Random((int)DateTime.UtcNow.Ticks);

            while (true)
            {
                MetricsAgent.IncrementCountMetric(CountMetric);
                MetricsAgent.RecordTimeSpanMetric(TimeMetric, TimeSpan.FromSeconds(rand.Next(60)));
                Thread.Sleep(10);
            }
        }
    }
}