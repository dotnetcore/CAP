# Serialization

We provide the `ISerializer` interface to support message serialization. By default, JSON is used to serialize messages and store them in the database.

## Custom Serialization

The default serializer uses `CapOptions.JsonSerializerOptions` (`System.Text.Json`). Configure converters and other JSON behavior without replacing the serializer:

```csharp
using System.Text.Json.Serialization;

services.AddCap(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
```

If you replace `ISerializer`, implement all members in its interface. The asynchronous methods return `ValueTask`, and the interface also requires synchronous serialization, deserialization, and `IsJsonType` members:

| Member | Signature |
| :--- | :--- |
| `Serialize` | `string Serialize(Message message)` |
| `SerializeAsync` | `ValueTask<TransportMessage> SerializeAsync(Message message)` |
| `Deserialize` | `Message? Deserialize(string json)` |
| `DeserializeAsync` | `ValueTask<Message> DeserializeAsync(TransportMessage transportMessage, Type? valueType)` |
| `Deserialize` | `object? Deserialize(object value, Type valueType)` |
| `IsJsonType` | `bool IsJsonType(object jsonObject)` |

Then register your serializer implementation in the container:

```C#
services.AddSingleton<ISerializer, YourSerializer>();

services.AddCap( /* ... */ );
```
