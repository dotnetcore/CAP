# NATS integration tests

Requires .NET 10 SDK and a running NATS Server with JetStream enabled. Tests use uniquely named `cap_test_<guid>` streams and delete only those streams during cleanup. The old `NATS.Client 1.1.8` dependency is used exclusively to verify rolling upgrades, pending-message takeover and rollback compatibility.

```powershell
# Defaults to nats://127.0.0.1:4222. Override for a different server:
$env:CAP_NATS_TEST_URL = 'nats://127.0.0.1:4222'
dotnet test test/DotNetCore.CAP.NATS.Test/DotNetCore.CAP.NATS.Test.csproj
```

The server-restart test is opt-in. It creates, restarts and removes a separate container with a fixed random host-port mapping and file-backed JetStream storage. It never restarts the server configured above.

```powershell
$env:CAP_NATS_DOCKER_TEST = '1'
# Optional: use a particular server image; defaults to nats:latest.
$env:CAP_NATS_DOCKER_IMAGE = 'nats:2.10.22'
dotnet test test/DotNetCore.CAP.NATS.Test/DotNetCore.CAP.NATS.Test.csproj
```

Coverage includes raw bytes and empty payloads, headers, publish deduplication and failures, same-group competing consumers, cross-group delivery, concurrent startup, ACK/NAK and timeout redelivery, new-message delivery policy, durable reuse and ACK progress, old/new client coexistence and rollback, callback failures, bounded group concurrency, cancellation and in-flight draining, stream configuration preservation, disabled stream management, connection pooling and server-restart recovery.
