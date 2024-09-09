using System;
using DotNetCore.CAP.AzureServiceBus.Helpers;
using Xunit;

namespace DotNetCore.CAP.AzureServiceBus.Test.Helpers;

public class ServiceBusHelpersTests
{
    [Fact]
    public void GetBrokerAddress_ShouldThrowArgumentException_WhenBothInputsAreNull()
    {
        // Arrange
        string? connectionString = null;
        string? @namespace = null;

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => ServiceBusHelpers.GetBrokerAddress(connectionString, @namespace));
        Assert.Equal("Either connection string or namespace are required.", ex.Message);
    }

    [Fact]
    public void GetBrokerAddress_ShouldReturnNamespace_WhenConnectionStringIsNull()
    {
        // Arrange
        string? connectionString = null;
        string? @namespace = "sb://mynamespace.servicebus.windows.net/";

        // Act
        var result = ServiceBusHelpers.GetBrokerAddress(connectionString, @namespace);

        // Assert
        Assert.Equal("AzureServiceBus", result.Name);
        Assert.Equal("sb://mynamespace.servicebus.windows.net/", result.Endpoint);
    }

    [Fact]
    public void GetBrokerAddress_ShouldReturnExtractedNamespace_WhenNamespaceIsNull()
    {
        // Arrange
        string? connectionString = "Endpoint=sb://mynamespace.servicebus.windows.net/;SharedAccessKeyName=myPolicy;SharedAccessKey=myKey";
        string? @namespace = null;

        // Act
        var result = ServiceBusHelpers.GetBrokerAddress(connectionString, @namespace);

        // Assert
        Assert.Equal("AzureServiceBus", result.Name);
        Assert.Equal("sb://mynamespace.servicebus.windows.net/", result.Endpoint);
    }

    [Fact]
    public void GetBrokerAddress_ShouldThrowInvalidOperationException_WhenNamespaceExtractionFails()
    {
        // Arrange
        string? connectionString = "InvalidConnectionString";
        string? @namespace = null;

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => ServiceBusHelpers.GetBrokerAddress(connectionString, @namespace));
        Assert.Equal("Unable to extract namespace from connection string.", ex.Message);
    }

    [Fact]
    public void GetBrokerAddress_ShouldReturnNamespace_WhenBothNamespaceAndConnectionStringAreProvided()
    {
        // Arrange
        string? connectionString = "Endpoint=sb://mynamespace.servicebus.windows.net/;SharedAccessKeyName=myPolicy;SharedAccessKey=myKey";
        string? @namespace = "anothernamespace";

        // Act
        var result = ServiceBusHelpers.GetBrokerAddress(connectionString, @namespace);

        // Assert
        Assert.Equal("AzureServiceBus", result.Name);
        Assert.Equal("anothernamespace", result.Endpoint);
    }

    [Fact]
    public void GetBrokerAddress_ShouldReturnExtractedNamespace_WhenConnectionStringIsValidAndNamespaceIsEmpty()
    {
        // Arrange
        string? connectionString = "Endpoint=sb://mynamespace.servicebus.windows.net/;SharedAccessKeyName=myPolicy;SharedAccessKey=myKey";
        string? @namespace = "";

        // Act
        var result = ServiceBusHelpers.GetBrokerAddress(connectionString, @namespace);

        // Assert
        Assert.Equal("AzureServiceBus", result.Name);
        Assert.Equal("sb://mynamespace.servicebus.windows.net/", result.Endpoint);
    }

    [Fact]
    public void GetBrokerAddress_ShouldReturnNamespace_WhenConnectionStringIsEmpty()
    {
        // Arrange
        string? connectionString = "";
        string? @namespace = "sb://mynamespace.servicebus.windows.net/";

        // Act
        var result = ServiceBusHelpers.GetBrokerAddress(connectionString, @namespace);

        // Assert
        Assert.Equal("AzureServiceBus", result.Name);
        Assert.Equal("sb://mynamespace.servicebus.windows.net/", result.Endpoint);
    }
}