# Kubernetes

[Kubernetes](https://kubernetes.io)，也称为 K8s，是一个开源系统，用于自动部署、扩展和管理容器化应用程序。

## Dashboard 中的 Kubernetes

我们的 Dashboard 从 7.2.0 版本开始支持 Kubernetes 作为服务发现。你可以切换到Node节点页面，然后选择命名空间，CAP会列出该命名空间下的所有Services，点击 *切换* 按钮后Dashboard将检测该节点的CAP服务是否可用，如果可用则会代理到切换的节点进行数据查看。

以下是一个配置示例

```cs
services.AddCap(x =>
{
    // ...
    x.UseDashboard();
    x.UseK8sDiscovery();
});

```

## 使用K8sDiscovery配置

此配置选项用于配置仪表板/节点以默认列出每个 K8s `service` 。如果将此设置为 `true`，则只会列出带有`dotnetcore.cap.visibility: show` 标签的服务。有关标签的更多信息可以在 **Kubernetes 标签配置** 部分找到。

* ShowOnlyExplicitVisibleNodes

> 默认值：false


```cs
services.AddCap(x =>
{
    // ...
    x.UseK8sDiscovery(opt=>{
      opt.ShowOnlyExplicitVisibleNodes = true;
    });
});
```

组件将会自动检测是否处于集群内部，如果处于集群内部在需要赋予Pod Kubernetes Api 的权限。参考下一章节。

## 分配 Pod 访问  Kubernetes Api 

如果你的Deployment关联的ServiceAccount没有K8s Api访问权限的话，则需要赋予 `namespaces`, `services` 资源的 `get`, `list` 权限。

这是一个实例yaml，首先创建一个 ServiceAccount 和 ClusterRole 并设置相关权限，然后使用 ClusterRoleBinding 进行绑定。最后在Deployment中使用 `serviceAccountName: api-access` 继续指定。

```
apiVersion: v1
kind: ServiceAccount
metadata:
  name: api-access

---
apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRole
metadata:
  name: ns-svc-reader
rules:
- apiGroups: [""]
  resources: ["namespaces", "services"]
  verbs: ["get", "watch", "list"]

---
apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRoleBinding
metadata:
  name: read-pods
subjects:
- kind: ServiceAccount
  name: api-access
  namespace: default
roleRef:
  kind: ClusterRole
  name: ns-svc-reader
  apiGroup: rbac.authorization.k8s.io
  
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: api-access-deployment
spec:
  replicas: 1
  selector:
    matchLabels:
      app: api-access-app
  template:
    metadata:
      labels:
        app: api-access-app
    spec:
      serviceAccountName: api-access
      containers:
      - name: api-access-container
        image: your_image
        
---
apiVersion: v1
kind: Service
metadata:
  name: api-access-service
spec:
  selector:
    app: api-access-app
  ports:
    - protocol: TCP
      port: 80
      targetPort: 80
```

从版本 `8.3.0` 及更高版本，您可以使用 `Role` 而不是 `ClusterRole`，以允许仅在仪表板运行的命名空间内发现服务。 Kubernetes 角色在命名空间内拥有有限的管辖权。在上面的示例中，只需删除 ClusterRole 和 ClusterRoleBinding 并改为使用以下内容

```
apiVersion: rbac.authorization.k8s.io/v1
kind: Role
metadata:
  name: ns-svc-reader
rules:
- apiGroups: [""]
  resources: ["services"]
  verbs: ["get", "watch", "list"]

---
apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRoleBinding
metadata:
  name: read-pods
subjects:
- kind: ServiceAccount
  name: api-access
  namespace: default
roleRef:
  kind: ClusterRole
  name: ns-svc-reader
  apiGroup: rbac.authorization.k8s.io

```

## Kubernetes 标签配置

可以通过向 kubernetes 服务添加标签来控制仪表板中显示的节点列表。


- `dotnetcore.cap.visibility` 标签用于显示或隐藏列表中的服务。

    > 可能的值: show | hide

    > 示例: `dotnetcore.cap.visibility: show` or `dotnetcore.cap.visibility: hide`

默认情况下，每个 k8s 服务都会列出该服务中找到的第一个端口。但是，如果服务上存在更多端口，您可以使用以下标签选择所需的端口：

- `dotnetcore.cap.portName` 标签用于过滤需要的服务端口。

    > 可能的值: string

    > 示例: `dotnetcore.cap.portName: grpc` or `dotnetcore.cap.portName: http`

If not found any port with the given name, it will try to match the next label portIndex

- `dotnetcore.cap.portIndex` 标签用于过滤需要的服务端口。 仅当未设置标签 portName 或设置不匹配的 portName 时，才会考虑此过滤器。

    > 可能的值: 数字表示为字符串 ex: '2' or '14'

    > 示例: `dotnetcore.cap.portIndex: '1'` or `dotnetcore.cap.portIndex: '3'`

  如果提供的索引超出范围，那么它将回退到第一个端口（索引：0）



## 独立使用 Dashboard 

你可以独立使用 Dashboard 而不需要配置CAP，此时相当于 Dashboard 可作为单独的 Pod 部署到 Kubernetes 集群中仅用作查看数据，待查看的服务不再需要配置 `cap.UseK8sDiscovery()` 配置项。

```
services.AddCapDashboardStandalone();
```

同样，你需要为此Pod配置 ServiceAccount 的访问权限。