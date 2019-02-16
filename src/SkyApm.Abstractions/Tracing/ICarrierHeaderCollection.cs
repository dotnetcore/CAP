using System.Collections.Generic;

namespace SkyApm.Tracing
{
    public interface ICarrierHeaderCollection : IEnumerable<KeyValuePair<string, string>>
    {
        void Add(string key, string value);
    }
}