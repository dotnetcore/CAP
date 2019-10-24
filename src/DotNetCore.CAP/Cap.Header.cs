using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DotNetCore.CAP
{
    public class CapHeader : ReadOnlyDictionary<string, string>
    {
        public CapHeader(IDictionary<string, string> dictionary) : base(dictionary)
        {

        }
    }
}