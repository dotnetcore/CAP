namespace Cap.Consistency.EventBus
{
    public interface IBackgroundWorker
    {
        bool IsRunning { get; }

        /// <summary>
        /// Start the background worker digest loop.
        /// </summary>
        void Start();

        /// <summary>
        /// Stop the background worker digest loop.
        /// </summary>
        /// <param name="timeout"></param>
        void Stop(int timeout = 2000);
    }
}