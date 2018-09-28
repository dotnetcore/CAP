namespace SkyWalking.Transport.Grpc
{
    internal static class ExceptionHelpers
    {
        public static readonly string RegisterApplicationError = "Register application fail.";
        public static readonly string RegisterApplicationInstanceError = "Register application instance fail.";
        public static readonly string HeartbeatError = "Heartbeat fail.";
        public static readonly string CollectError = "Send trace segment fail.";
    }
}