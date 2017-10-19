
using Confluent.Kafka;

namespace DotNetCore.CAP.Kafka
{
    public interface IConnectionPool
    {
        Producer Rent();

        bool Return(Producer context);
    }
}