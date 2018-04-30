namespace SkyWalking.Context.Trace
{
    public class NoopEntrySpan:NoopSpan
    {
        public override bool IsEntry { get; } = true;
    }
}