namespace DotNetCore.CAP.Dashboard.GatewayProxy
{
    public class DownstreamUrl
    {
        public DownstreamUrl(string value)
        {
            Value = value;
        }

        public string Value { get; }
    }
}