namespace DotNetCore.CAP.Infrastructure
{
    /// <summary>
    /// The message status name.
    /// </summary>
    public struct StatusName
    {
        public const string Scheduled = nameof(Scheduled);
        public const string Enqueued = nameof(Enqueued);
        public const string Processing = nameof(Processing);
        public const string Succeeded = nameof(Succeeded);
        public const string Failed = nameof(Failed);
    }
}