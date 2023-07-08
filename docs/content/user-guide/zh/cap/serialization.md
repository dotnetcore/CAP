# 序列化

CAP 提供了 `ISerializer` 接口来支持对消息进行序列化，默认情况下我们使用 json 来对消息进行序列化处理并存储到数据库中。

## 自定义序列化

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

然后将你的实现注册到容器中:

```

//注册你的自定义实现
services.AddSingleton<ISerializer, YourSerializer>();

// ---
services.AddCap 

```