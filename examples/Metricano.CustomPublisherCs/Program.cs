using System;
using System.Threading;

using Metricano.PostSharpAspects;

namespace Metricano.CustomPublisherCs
{
    /// <summary>
    /// Let this run for couple of minutes to generate data points in CloudWatch.
    /// Press Ctrl+C or close the console window to stop it.
    /// </summary>
    [CountExecution]    // multi-cast to all methods, public & private
    [LogExecutionTime]  // multi-cast to all methods, public & private
    public class Program
    {
        private static readonly Random Rand = new Random((int)DateTime.UtcNow.Ticks);

        public static void Main(string[] args)
        {
            Publish.With(new ConsolePublisher());

            while (true)
            {
                TakesRandomTimeToExecute();
            }
        }

        private static void TakesRandomTimeToExecute()
        {
            Thread.Sleep(Rand.Next(42));
        }
    }
}
