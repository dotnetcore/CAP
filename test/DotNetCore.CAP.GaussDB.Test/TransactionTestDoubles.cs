using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Persistence;
using DotNetCore.CAP.Transport;

namespace DotNetCore.CAP.GaussDB.Test;

internal sealed class TransactionTestPublisher : ICapPublisher
{
    public TransactionTestPublisher(IServiceProvider serviceProvider) => ServiceProvider = serviceProvider;

    public IServiceProvider ServiceProvider { get; }
    public ICapTransaction Transaction { get; set; }
    public Task PublishAsync<T>(string name, T contentObj, string callbackName = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task PublishAsync<T>(string name, T contentObj, IDictionary<string, string> headers, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public void Publish<T>(string name, T contentObj, string callbackName = null) => throw new NotSupportedException();
    public void Publish<T>(string name, T contentObj, IDictionary<string, string> headers) => throw new NotSupportedException();
    public Task PublishDelayAsync<T>(TimeSpan delayTime, string name, T contentObj, IDictionary<string, string> headers, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task PublishDelayAsync<T>(TimeSpan delayTime, string name, T contentObj, string callbackName = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public void PublishDelay<T>(TimeSpan delayTime, string name, T contentObj, IDictionary<string, string> headers) => throw new NotSupportedException();
    public void PublishDelay<T>(TimeSpan delayTime, string name, T contentObj, string callbackName = null) => throw new NotSupportedException();
}

internal sealed class TransactionTestDispatcher : IDispatcher
{
    public ValueTask EnqueueToPublish(MediumMessage message) => ValueTask.CompletedTask;
    public ValueTask EnqueueToExecute(MediumMessage message, ConsumerExecutorDescriptor descriptor = null) => ValueTask.CompletedTask;
    public Task EnqueueToScheduler(MediumMessage message, DateTime publishTime, object transaction = null) => Task.CompletedTask;
    public ValueTask StartAsync(CancellationToken stoppingToken) => ValueTask.CompletedTask;
    public void Dispose() { }
}
