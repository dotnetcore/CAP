using System.Linq;
using JetBrains.Annotations;

namespace DotNetCore.CAP.Transport
{
    public struct BrokerAddress
    {
        public BrokerAddress([NotNull]string address)
        {
            if (address.Contains("$"))
            {
                var parts = address.Split('$');

                Name = parts[0];
                Endpoint = string.Join(string.Empty, parts.Skip(1));
            }
            else
            {
                Name = string.Empty;
                Endpoint = address;
            }
        }

        public BrokerAddress([NotNull]string name, [CanBeNull]string endpoint)
        {
            Name = name;
            Endpoint = endpoint;
        }

        public string Name { get; set; }

        public string Endpoint { get; set; }

        public override string ToString()
        {
            return Name + "$" + Endpoint;
        }
    }
}
