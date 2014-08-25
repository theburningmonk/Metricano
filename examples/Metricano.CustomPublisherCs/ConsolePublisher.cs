using System;
using System.Linq;
using System.Threading.Tasks;

namespace Metricano.CustomPublisherCs
{
    public class ConsolePublisher : IMetricsPublisher
    {
        public async Task Publish(Metric[] metrics)
        {
            foreach (var metric in metrics)
            {
                Publish(metric);
            }
        }

        public void Dispose()
        {
            Console.WriteLine("Bye bye world...");
        }

        private static TimeSpan GetNinetyFivePercentile(TimeSpan[] timeSpans)
        {
            var skipCount = timeSpans.Length * 0.05;
            return timeSpans
                    .OrderByDescending(ts => ts.TotalMilliseconds)
                     .Skip((int)skipCount)
                     .FirstOrDefault();
        }

        private static void Publish(Metric metric)
        {
            if (metric.IsCount)
            {
                var countMetric = (Metric.Count)metric;
                Console.WriteLine(
                    @"Count metric ({0}):
Max             : {1}
Min             : {2}
Average         : {3}
Total           : {4}
Sample Size     : {5}",
                    countMetric.Name,
                    countMetric.Max,
                    countMetric.Min,
                    countMetric.Average,
                    countMetric.Sum,
                    countMetric.SampleCount);
            }
            else
            {
                var timeMetric = (Metric.TimeSpan)metric;
                var rawTimes = timeMetric.Item.Raw;
                Console.WriteLine(
                    @"TimeSpan metric ({0}):
Max             : {1}ms
Min             : {2}ms
Average         : {3}ms
Total           : {4}ms
Sample Size     : {5}
95 percentile   : {6}ms",
                    timeMetric.Name,
                    timeMetric.Max,
                    timeMetric.Min,
                    timeMetric.Average,
                    timeMetric.Sum,
                    timeMetric.SampleCount,
                    GetNinetyFivePercentile(rawTimes).TotalMilliseconds);

            }
        }
    }
}
