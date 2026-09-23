# 序列化

CAP 提供了 `ISerializer` 接口来支持对消息进行序列化，默认情况下我们使用 json 来对消息进行序列化处理并存储到数据库中。

## 自定义序列化

默认序列化器使用 `CapOptions.JsonSerializerOptions`（`System.Text.Json`）。如需添加转换器或调整 JSON 行为，无需替换序列化器：

```csharp
using System.Text.Json.Serialization;

services.AddCap(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
```

如果要替换 `ISerializer`，需要实现接口中的全部成员。异步方法返回 `ValueTask`，接口还要求实现同步序列化、反序列化和 `IsJsonType`：

| 成员 | 签名 |
| :--- | :--- |
| `Serialize` | `string Serialize(Message message)` |
| `SerializeAsync` | `ValueTask<TransportMessage> SerializeAsync(Message message)` |
| `Deserialize` | `Message? Deserialize(string json)` |
| `DeserializeAsync` | `ValueTask<Message> DeserializeAsync(TransportMessage transportMessage, Type? valueType)` |
| `Deserialize` | `object? Deserialize(object value, Type valueType)` |
| `IsJsonType` | `bool IsJsonType(object jsonObject)` |

然后将你的实现注册到容器中:

```

//注册你的自定义实现
services.AddSingleton<ISerializer, YourSerializer>();

// ---
services.AddCap 

```
