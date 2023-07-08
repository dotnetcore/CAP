# Serialization

We provide the `ISerializer` interface to support serialization of messages. By default, json is used to serialize messages and store them in the database.

## Custom Serialization

```C#
public class YourSerializer: ISerializer
{
    Task<TransportMessage> SerializeAsync(Message message)
    {

    }
 
    Task<Message> DeserializeAsync(TransportMessage transportMessage, Type valueType)
    {

    }
}
```

Then register your implemented serializer in the container:

```

services.AddSingleton<ISerializer, YourSerializer>();

// ---
services.AddCap 

```
