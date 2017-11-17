using RabbitMQ.Client;

namespace DotNetCore.CAP.RabbitMQ
{
    public interface IConnectionChannelPool
    {
        IConnection GetConnection();

        IModel Rent();

        bool Return(IModel context);
    }
}