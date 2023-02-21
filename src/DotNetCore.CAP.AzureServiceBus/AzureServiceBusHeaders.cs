namespace DotNetCore.CAP.AzureServiceBus
{
    public static class AzureServiceBusHeaders
    {
        public const string SessionId = "cap-session-id";

        /// <summary>
        /// The scheduled enqueue time as DateTimeOffset. This value is for delayed message sending.
        /// It is utilized to delay messages sending to a specific time in the future.
        /// </summary>
        /// <remarks>
        /// See https://docs.microsoft.com/en-us/azure/service-bus-messaging/message-sequencing#scheduled-messages for details.
        /// </remarks>
        public const string ScheduledEnqueueTimeUtc = "cap-scheduled-enqueue-time-utc";
    }
}