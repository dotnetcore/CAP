// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace DotNetCore.CAP.Diagnostics;

/// <summary>
/// Extension methods on the DiagnosticListener class to log CAP data
/// </summary>
public static class CapDiagnosticListenerNames
{
    private const string CapPrefix = "DotNetCore.CAP.";

    //Tracing
    public const string DiagnosticListenerName = "CapDiagnosticListener";

    public const string BeforePublishMessageStore = CapPrefix + "WritePublishMessageStoreBefore";
    public const string AfterPublishMessageStore = CapPrefix + "WritePublishMessageStoreAfter";
    public const string ErrorPublishMessageStore = CapPrefix + "WritePublishMessageStoreError";

    public const string BeforePublish = CapPrefix + "WritePublishBefore";
    public const string AfterPublish = CapPrefix + "WritePublishAfter";
    public const string ErrorPublish = CapPrefix + "WritePublishError";

    public const string BeforeConsume = CapPrefix + "WriteConsumeBefore";
    public const string AfterConsume = CapPrefix + "WriteConsumeAfter";
    public const string ErrorConsume = CapPrefix + "WriteConsumeError";

    public const string BeforeSubscriberInvoke = CapPrefix + "WriteSubscriberInvokeBefore";
    public const string AfterSubscriberInvoke = CapPrefix + "WriteSubscriberInvokeAfter";
    public const string ErrorSubscriberInvoke = CapPrefix + "WriteSubscriberInvokeError";

    //Metrics
    public const string MetricListenerName = CapPrefix + "EventCounter";
    public const string PublishedPerSec = "published-per-second";
    public const string ConsumePerSec = "consume-per-second";
    public const string InvokeSubscriberPerSec = "invoke-subscriber-per-second";
    public const string InvokeSubscriberElapsedMs = "invoke-subscriber-elapsed-ms";
}