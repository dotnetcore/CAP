# 序列化

CAP 目前还不支持消息本身的序列化，在将消息发送到消息队列之前 CAP 使用 json 对消息对象进行序列化。

## 内容序列化

CAP 支持对消息的 Content 字段进行序列化，你可以自定义 `IContentSerializer` 接口来做到这一点。

目前由于消息对象需要进行数据库存储，所以只支持 string 的序列化和反序例化。

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

## 消息适配器

在异构系统中，有时候需要和其他系统进行通讯，但是其他系统使用的消息对象可能和 CAP 的[**包装器对象**](../persistent/general.md#_7)不一样，这个时候就需要对消息进行自定义适配。

CAP 提供了 `IMessagePacker` 接口用于对 [**包装器对象**](../persistent/general.md#_7) 进行自定义，自定义的 MessagePacker 通常是将 `CapMessage` 进行打包和解包操作，在这个过程中可以添加自己的业务对象。

使用方法：

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

接下来，配置自定义的 `MyMessagePacker` 到服务中。

```csharp

services.AddCap(x =>{  }).AddMessagePacker<MyMessagePacker>();

```