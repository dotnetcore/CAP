using System.Diagnostics;

namespace SkyWalking.Core.Tests.Diagnostics
{
    public class FakeDiagnosticListener : DiagnosticListener
    {
        public const string ListenerName = "SkyWalking.Core.Tests.Diagnostics";

        public const string Executing = "Executing";

        public const string Executed = "Executed";
        
        public FakeDiagnosticListener() : base(ListenerName)
        {
        }
    }
}