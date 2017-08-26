using System.Collections.Generic;

namespace DotNetCore.CAP
{
    public class StateData
    {
        public string Name { get; set; }

        public string Reason { get; set; }

        public IDictionary<string, string> Data { get; set; } 
    }
}