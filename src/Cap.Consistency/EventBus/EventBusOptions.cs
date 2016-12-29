namespace Cap.Consistency.EventBus
{
    public class EventBusOptions
    {
        public long MaxPendingEventNumber { get; set; }

        public int MaxPendingEventNumber32 {
            get {
                if (this.MaxPendingEventNumber < int.MaxValue) {
                    return (int)this.MaxPendingEventNumber;
                }
                return int.MaxValue;
            }
        }

        public EventBusOptions() {
            this.MaxPendingEventNumber = EventBusBase.DefaultMaxPendingEventNumber;
        }
    }
}