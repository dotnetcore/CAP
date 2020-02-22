# Serialization

We provide the `ISerializer` interface to support serialization of messages. By default, we use json to serialize messages and store them in the database.

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

Then register your implementation in the container:

```

services.AddSingleton<ISerializer, YourSerializer>();

// ---
services.AddCap 

```

## Message Adapter (removed in v3.0)

In heterogeneous systems, sometimes you need to communicate with other systems, but other systems use message objects that may be different from CAP's [**Wrapper Object**](../persistent/general.md#_7). This time maybe you need to customize the message wapper.

The CAP provides the `IMessagePacker` interface for customizing the [**Wrapper Object**](../persistent/general.md#_7). The custom MessagePacker usually packs and unpacks the `CapMessage` In this process you can add your own business objects.

Usage :

```csharp

class MyMessagePacker : IMessagePacker
{
    private readonly IContentSerializer _serializer;

    public DefaultMessagePacker(IContentSerializer serializer)
    {
        _serializer = serializer;
    }

    public string Pack(CapMessage obj)
    {
        var myStructure = new
        {
            Id = obj.Id,
            Body = obj.Content,
            Date = obj.Timestamp,
            Callback = obj.CallbackName
        };
        return _serializer.Serialize(myStructure);
    }

    public CapMessage UnPack(string packingMessage)
    {
        var myStructure = _serializer.DeSerialize<MyStructure>(packingMessage);

        return new CapMessageDto
        {
            Id = myStructure.Id,
            Timestamp = myStructure.Date,
            Content = myStructure.Body,
            CallbackName = myStructure.Callback
        };
    }
}
```

Next, configure the custom `MyMessagePacker` to the service.

```csharp

services.AddCap(x =>{  }).AddMessagePacker<MyMessagePacker>();

```