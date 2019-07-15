# Diagnostics

Diagnostics provides a set of features that make it easy for us to document the critical operations that occur during the application's operation, their execution time, etc., allowing administrators to find the root cause of problems, especially in production environments.

## Diagnostics events

The CAP provides support for `DiagnosticSource` with a listener name of `CapDiagnosticListener`.

Diagnostics provides external event information as follows:

* Before the message is persisted
* After the message is persisted
* Message persistence exception
* Before the message is sent to MQ
* After the message is sent to MQ
* The message sends an exception to MQ.
* Messages saved from MQ consumption before saving
* After the message is saved from MQ consumption
* Before the subscriber method is executed
* After the subscriber method is executed
* Subscriber method execution exception

Related objects, you can find at the `DotNetCore.CAP.Diagnostics` namespace.