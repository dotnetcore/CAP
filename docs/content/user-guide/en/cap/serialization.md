# Serialization

CAP does not currently support serialization for transport messages, and CAP uses json to serialize message objects before sending them to the transport.

## Content Serialization

The CAP supports serializing the Message's Content field, which you can do by customizing the `IContentSerializer` interface.

Currently, since the message object needs to be stored in the database, only the serialization and reverse ordering of `string` are supported.

```csharp

class MyContentSerializer : IContentSerializer 
{
    public T DeSerialize<T>(string messageObjStr)
    {
    }

    public object DeSerialize(string content, Type type)
    {
    }

    public string Serialize<T>(T messageObj)
    {
    }
}
```

Configure the custom `MyContentSerializer` to the service.

```csharp

services.AddCap(x =>{  }).AddContentSerializer<MyContentSerializer>();

```

## Message Adapter

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