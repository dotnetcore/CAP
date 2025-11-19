// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace DotNetCore.CAP;

/// <summary>
/// A marker interface that identifies a class as containing CAP subscriber methods.
/// Classes implementing this interface can use CAP subscriber attributes (e.g., <c>[Topic(...)]</c>) on their methods
/// to subscribe to published messages.
/// </summary>
/// <remarks>
/// This interface serves purely as a marker and does not define any members.
/// Its purpose is to enable automatic discovery and registration of subscriber classes during application startup.
/// 
/// Usage example:
/// <code>
/// public class OrderSubscriber : ICapSubscribe
/// {
///     [CapSubscribe("order.created")]
///     public async Task OnOrderCreated(OrderCreatedMessage message)
///     {
///         // Handle the message
///         await ProcessOrder(message);
///     }
/// }
/// </code>
/// </remarks>
public interface ICapSubscribe
{
}