# 和 Castle DynamicProxy 集成

Castle DynamicProxy 是一个用于在运行时动态生成轻量级.NET代理的库。代理对象允许在不修改类代码的情况下截取对对象成员的调用。可以代理类和接口，但是只能拦截虚拟成员。

Castle.DynamicProxy 可以帮助你方便的创建代理对象，代理对象可以帮助构建灵活的应用程序体系结构，因为它允许将功能透明地添加到代码中，而无需对其进行修改。例如，可以代理一个类来添加日志记录或安全检查，而无需使代码知道已添加此功能。

下面可以看到如何在 CAP 中集成使用 Castle.DynamicProxy。


## 1、安装 NuGet 包

在 集成了 CAP 的项目中安装包，有关如何集成 CAP 的文档请看[这里](https://cap.dotnetcore.xyz/)。

注意，`Castle.DynamicProxy` 这个包已经被废弃，请使用最新的 `Castle.Core` 包。

```xml
<PackageReference Include="Castle.Core" Version="4.4.1" />
```

## 2、创建一个 Castle 切面拦截器

可以在这里 [dynamicproxy.md](https://github.com/castleproject/Core/blob/master/docs/dynamicproxy.md) 找到相关的文档。

下面为示例代码，继承 Castle 提供的 `IInterceptor` 接口即可：

```
[Serializable]
public class MyInterceptor : IInterceptor
{
    public void Intercept(IInvocation invocation)
    {
        Console.WriteLine("Before target call");
        try
        {
            invocation.Proceed();
        }
        catch (Exception)
        {
            Console.WriteLine("Target threw an exception!");
            throw;
        }
        finally
        {
            Console.WriteLine("After target call");
        }
    }
}

```

拦截器此处命名为 `MyInterceptor`，你可以在其中处理你的业务逻辑，比如添加日志或其他的一些行为。

## 3、创建 IServiceCollection 的扩展类

为 `IServiceCollection` 创建扩展，方面后续调用。

```csharp
using Castle.DynamicProxy;

public static class ServicesExtensions
{
    public static void AddProxiedSingleton<TImplementation>(this IServiceCollection services)
        where TImplementation : class
    {
        services.AddSingleton(serviceProvider =>
        {
            var proxyGenerator = serviceProvider.GetRequiredService<ProxyGenerator>();
            var interceptors = serviceProvider.GetServices<IInterceptor>().ToArray();
            return proxyGenerator.CreateClassProxy<TImplementation>(interceptors);
        });
    }
}
```

此处我创建了一个 Singleton 声明周期的扩展方法，建议所有 CAP 的订阅者都创建为 Singleton 即可，因为在 CAP 内部实际执行的时候也会创建一个 scope 来执行，所以无需担心资源释放问题。


## 4、创建 CAP 订阅服务

创建一个 CAP 订阅类，注意不能放在 Controller 中了。

**注意：方法需要为虚方法 virtual，才能被 Castle 重写，别搞忘了加！！！**  

```cs
public class CapSubscribeService: ICapSubscribe
{
    [CapSubscribe("sample.rabbitmq.mysql")]
    public virtual void Subscriber(DateTime p)
    {
        Console.WriteLine($@"{DateTime.Now} Subscriber invoked, Info: {p}");
    }
}
```

## 5、在 Startup 中集成

```cs
public void ConfigureServices(IServiceCollection services)
{
    // 添加 Castle 的代理生成器
    services.AddSingleton(new ProxyGenerator());
    
    // 添加第2步的自定义的拦截类，声明周期为
    services.AddSingleton<IInterceptor, MyInterceptor>();
    
    // 此处为上面的扩展方法， 添加 CAP 订阅 Service
    services.AddProxiedSingleton<CapSubscribeService>();
    
    services.AddCap(x =>
    {
        x.UseMySql("");
        x.UseRabbitMQ("");
        x.UseDashboard();
    });
    
    // ...
}
```

以上就完成了所有的集成工作，可以开始进行测试了，有问题欢迎到 [Github issue](https://github.com/dotnetcore/CAP/issues) 反馈。


**注意： CAP 需要使用 5.0 + 版本，目前(2021年1月6日)只有 preview 版本。**
