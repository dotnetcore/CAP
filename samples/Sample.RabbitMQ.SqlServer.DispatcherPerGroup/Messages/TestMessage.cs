namespace Sample.RabbitMQ.SqlServer.DispatcherPerGroup.Messages
{
    public class TestMessage
    {
        public static TestMessage Create(string text) => new()
        {
            Text = text
        };

        public string Text { get; private init; }
    }
}