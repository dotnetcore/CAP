namespace Cap.Consistency.EventBus
{
    public class BrokeredMessage
    {
        public byte[] Body { get; set; }

        public string Type { get; set; }
    }
}