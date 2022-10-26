using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Messages;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    public static class CapPublisherExtensions
    {
        /// <summary>
        /// Schedule a message to be published at the feature time with callback name.
        /// <para>SHOULD BE ENABLE RabbitMQ <b>rabbitmq_delayed_message_exchange</b>  PLUGINS.</para>
        /// </summary>
        /// <typeparam name="T">content object</typeparam>
        /// <param name="delayTime">The delay for message to published.</param>
        /// <param name="name">the topic name or exchange router key.</param>
        /// <param name="contentObj">message body content, that will be serialized. (can be null)</param>
        /// <param name="callbackName">callback subscriber name.</param>
        /// <param name="cancellationToken"></param>
        /// <param name="publisher"></param>
        public static async Task PublishDelayAsync<T>(this ICapPublisher publisher, TimeSpan delayTime,
            string name, T? contentObj, string? callbackName = null, CancellationToken cancellationToken = default)
        {
            var dic = new Dictionary<string, string?>
            {
                {Headers.CallbackName, callbackName},
                {"x-delay", delayTime.TotalMilliseconds.ToString(CultureInfo.InvariantCulture)}
            };

            await publisher.PublishAsync(name, contentObj, dic, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Schedule a message to be published at the feature time with custom headers.
        /// <para>SHOULD BE ENABLE RabbitMQ <b>rabbitmq_delayed_message_exchange</b>  PLUGINS.</para>
        /// </summary>
        /// <typeparam name="T">content object</typeparam>
        /// <param name="publisher"></param>
        /// <param name="delayTime">The delay for message to published.</param>
        /// <param name="name">the topic name or exchange router key.</param>
        /// <param name="contentObj">message body content, that will be serialized. (can be null)</param>
        /// <param name="headers">message additional headers.</param>
        /// <param name="cancellationToken"></param>
        public static async Task PublishDelayAsync<T>(this ICapPublisher publisher, TimeSpan delayTime,
            string name, T? contentObj, Dictionary<string, string?> headers, CancellationToken cancellationToken = default)
        {
            headers.Add("x-delay", delayTime.TotalMilliseconds.ToString(CultureInfo.InvariantCulture));

            await publisher.PublishAsync(name, contentObj, headers, cancellationToken).ConfigureAwait(false);
        }
    }
}
