using System.Threading;
using System.Threading.Tasks;

using Amazon;

using Metricano.Publisher;

namespace Metricano.PostSharpAspects.ExampleCs
{
    /// <summary>
    /// Let this run for couple of minutes to generate data points in CloudWatch.
    /// Press Ctrl+C or close the console window to stop it.
    /// </summary>
    [CountExecution]    // multi-cast to all methods, public & private
    [LogExecutionTime]  // multi-cast to all methods, public & private
    public class Program
    {
        public static void Main(string[] args)
        {
            var prog = new Program();
            Publish.With(new CloudWatchPublisher(
                "MetricanoDemo",
                "YOUR_AWS_KEY_HERE",
                "YOUR_AWS_SECRET_HERE", 
                RegionEndpoint.USEast1));

            while (true)
            {
                PublicStaticMethod();
                prog.PublicMethod();
                prog.PublicAsyncMethod().Wait();
                var _ = prog.PublicAsyncMethodWithReturnValue().Result;

                PrivateStaticMethod();
                prog.PrivateMethod();
                prog.PrivateAsyncMethod().Wait();
                var __ = prog.PrivateAsyncMethodWithReturnValue().Result;
            }
        }

        public static void PublicStaticMethod()
        {
            Thread.Sleep(1);
        }

        public void PublicMethod()
        {
            Thread.Sleep(1);
        }

        public async Task PublicAsyncMethod()
        {
            await Task.Delay(1);
        }

        public async Task<int> PublicAsyncMethodWithReturnValue()
        {
            await Task.Delay(1);
            return await Task.FromResult(42);
        }

        private static void PrivateStaticMethod()
        {
            Thread.Sleep(1);
        }

        private void PrivateMethod()
        {
            Thread.Sleep(1);
        }

        private async Task PrivateAsyncMethod()
        {
            await Task.Delay(1);
        }

        private async Task<int> PrivateAsyncMethodWithReturnValue()
        {
            await Task.Delay(1);
            return await Task.FromResult(42);
        }
    }
}