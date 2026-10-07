// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Xunit;

namespace DotNetCore.CAP.NATS.Test;

public sealed class DockerFactAttribute : FactAttribute
{
    public DockerFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("CAP_NATS_DOCKER_TEST") != "1")
            Skip = "Set CAP_NATS_DOCKER_TEST=1 to test server restarts in a separate disposable Docker container.";
    }
}

public class DockerRestartTests
{
    [DockerFact]
    public async Task ServerRestartSignalsCapAndNewClientResumesExistingConsumer()
    {
        var name = "cap-nats-test-" + Guid.NewGuid().ToString("N");
        using var reservation = new TcpListener(IPAddress.Loopback, 0);
        reservation.Start();
        var port = ((IPEndPoint)reservation.LocalEndpoint).Port;
        reservation.Stop();
        // Docker reassigns an automatically allocated host port on restart; use a fixed mapping.
        await DockerAsync("run", "--detach", "--name", name, "--publish", $"127.0.0.1:{port}:4222",
            Environment.GetEnvironmentVariable("CAP_NATS_DOCKER_IMAGE") ?? "nats:latest", "--jetstream", "--store_dir", "/data");
        try
        {
            await using var scope = new NatsTestScope("nats://127.0.0.1:" + port);
            await NatsTestScope.UntilAsync(async () =>
            {
                try { await scope.Connection.ConnectAsync(); return true; }
                catch { return false; }
            });
            scope.Options.StreamOptions = config => config.Storage = global::NATS.Client.JetStream.Models.StreamConfigStorage.File;
            scope.Options.ConsumerOptions = opts => opts with { AckWait = TimeSpan.FromMilliseconds(300) };
            var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var first = await scope.StartAsync("group", (_, _, _) => { pending.TrySetResult(); return Task.CompletedTask; });
            await scope.PublishAsync("pending");
            await pending.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var durable = Helper.Normalized("group-" + scope.Topic);
            var before = await scope.JetStream.GetPushConsumerAsync(scope.Stream, durable);
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            var listening = first.ListeningAsync(TimeSpan.FromSeconds(1), cancellation.Token);
            await DockerAsync("restart", name);
            await Assert.ThrowsAsync<BrokerConnectionException>(() => listening);
            await first.DisposeAsync();
            Assert.Contains(scope.Logs, log => log.LogType == MqLogType.ConnectError);
            await NatsTestScope.UntilAsync(async () =>
            {
                try { await scope.Connection.PingAsync(cancellation.Token); return true; }
                catch { return false; }
            });
            var restored = await scope.JetStream.GetPushConsumerAsync(scope.Stream, durable);
            Assert.Equal(before.Info.Config.DeliverSubject, restored.Info.Config.DeliverSubject);
            Assert.Equal(1, restored.Info.NumAckPending);
            var received = new ConcurrentBag<string>();
            await scope.StartAsync("group", async (client, message, sender) =>
            {
                received.Add(message.GetId());
                await client.CommitAsync(sender);
            });
            await scope.PublishAsync("after-restart");
            await NatsTestScope.UntilAsync(() => received.Contains("pending") && received.Contains("after-restart"));
            var after = await scope.JetStream.GetPushConsumerAsync(scope.Stream, durable);
            // Server 2.10 stores a slightly later metadata timestamp than the initial API response.
            // Compare against the restored consumer to verify CAP does not recreate it.
            Assert.Equal(restored.Info.Created, after.Info.Created);
            Assert.Equal(before.Info.Config.DeliverSubject, after.Info.Config.DeliverSubject);
        }
        finally
        {
            await DockerAsync("rm", "--force", name);
        }
    }

    private static async Task<string> DockerAsync(params string[] arguments)
    {
        var info = new ProcessStartInfo("docker") { RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        foreach (var argument in arguments) info.ArgumentList.Add(argument);
        using var process = Process.Start(info)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        await process.WaitForExitAsync(timeout.Token);
        Assert.True(process.ExitCode == 0, await error);
        return await output;
    }
}
