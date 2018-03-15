namespace DotNetCore.CAP.Infrastructure
{
    /// <summary>
    /// The message status name.
    /// </summary>
    public struct StatusName
    {
        public const string Scheduled = nameof(Scheduled);
        public const string Succeeded = nameof(Succeeded);
        public const string Failed = nameof(Failed);
    }
}