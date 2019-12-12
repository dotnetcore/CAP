namespace DotNetCore.CAP.Test.FakeInMemoryQueue
{
    public static class CapOptionsExtensions
    {
        public static CapOptions UseFakeTransport(this CapOptions options)
        {
            options.RegisterExtension(new FakeQueueOptionsExtension());
            return options;
        }
    }
}