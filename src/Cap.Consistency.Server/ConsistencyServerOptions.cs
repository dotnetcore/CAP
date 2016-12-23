using System;

namespace Cap.Consistency.Server
{
    public class ConsistencyServerOptions
    {
        /// <summary>
        /// Enables the Consistency Server options callback to resolve and use services registered by the application during startup.
        /// Typically initialized by <see cref="Cap.Consistency.UseConsistency(Action{ConsistencyServerOptions})"/>.
        /// </summary>
        public IServiceProvider ApplicationServices { get; set; }

        /// <summary>
        /// The amount of time after the server begins shutting down before connections will be forcefully closed.
        /// Kestrel will wait for the duration of the timeout for any ongoing request processing to complete before
        /// terminating the connection. No new connections or requests will be accepted during this time.
        /// </summary>
        /// <remarks>
        /// Defaults to 5 seconds.
        /// </remarks>
        public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// The number of libuv I/O threads used to process requests.
        /// </summary>
        /// <remarks>
        /// Defaults to half of <see cref="Environment.ProcessorCount" /> rounded down and clamped between 1 and 16.
        /// </remarks>
        public int ThreadCount { get; set; } = ProcessorThreadCount;

        private static int ProcessorThreadCount {
            get {
                // Actual core count would be a better number
                // rather than logical cores which includes hyper-threaded cores.
                // Divide by 2 for hyper-threading, and good defaults (still need threads to do webserving).
                var threadCount = Environment.ProcessorCount >> 1;

                if (threadCount < 1) {
                    // Ensure shifted value is at least one
                    return 1;
                }

                if (threadCount > 16) {
                    // Receive Side Scaling RSS Processor count currently maxes out at 16
                    // would be better to check the NIC's current hardware queues; but xplat...
                    return 16;
                }

                return threadCount;
            }
        }
    }
}