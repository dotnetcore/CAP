namespace DotNetCore.CAP
{
    /// <summary>
    /// Consumer client factory to create consumer client instance.
    /// </summary>
    public interface IConsumerClientFactory
    {
        /// <summary>
        /// Create a new instance of <see cref="IConsumerClient" />.
        /// </summary>
        /// <param name="groupId">message group number</param>
        IConsumerClient Create(string groupId);
    }
}