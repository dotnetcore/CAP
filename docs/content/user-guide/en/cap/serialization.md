# Serialization

We provide the `ISerializer` interface to support message serialization. By default, JSON is used to serialize messages and store them in the database.

## Custom Serialization

```C#
public class YourSerializer : ISerializer
{
    Task<TransportMessage> SerializeAsync(Message message)
    {

    }
 
    Task<Message> DeserializeAsync(TransportMessage transportMessage, Type valueType)
    {

    }
}
```

Then register your serializer implementation in the container:

```C#
services.AddSingleton<ISerializer, YourSerializer>();

services.AddCap( /* ... */ );
```
