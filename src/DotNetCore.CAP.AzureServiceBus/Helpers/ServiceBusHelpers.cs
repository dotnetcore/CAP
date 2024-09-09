using System;
using DotNetCore.CAP.Transport;

namespace DotNetCore.CAP.AzureServiceBus.Helpers;

public static class ServiceBusHelpers
{
    public static BrokerAddress GetBrokerAddress(string? connectionString, string? @namespace)
    {
        var host = (@namespace, connectionString) switch
        {
            _ when string.IsNullOrWhiteSpace(@namespace) && string.IsNullOrWhiteSpace(connectionString)
                => throw new ArgumentException("Either connection string or namespace are required."),
            _ when string.IsNullOrWhiteSpace(connectionString)
                   || (!string.IsNullOrWhiteSpace(@namespace) && !string.IsNullOrWhiteSpace(connectionString))
                => @namespace!,
            _ when string.IsNullOrWhiteSpace(@namespace)
                => TryGetEndpointFromConnectionString(connectionString, out var extractedValue)
                    ? extractedValue!
                    : throw new InvalidOperationException("Unable to extract namespace from connection string.")
        };

        return new BrokerAddress("AzureServiceBus", host);
    }


    private static bool TryGetEndpointFromConnectionString(string? connectionString, out string? @namespace)
    {
        @namespace = string.Empty;

        if (string.IsNullOrWhiteSpace(connectionString))
            return false;

        var keyValuePairs = connectionString.Split(';');

        foreach (var kvp in keyValuePairs)
        {
            if (!kvp.StartsWith("Endpoint", StringComparison.InvariantCultureIgnoreCase)) continue;
            
            var endpointParts = kvp.Split('=');

            if (endpointParts.Length != 2) continue;

            var uri = new Uri(endpointParts[1]);
            
            // Namespace is the host part without the .servicebus.windows.net
            @namespace = uri.ToString();

            return true;
        }

        return false;
    }
}