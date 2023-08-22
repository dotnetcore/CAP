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

## 独立使用 Dashboard

你可以独立使用 Dashboard 而不需要配置CAP，此时相当于Dashboard可作为单独的Pod部署到Kubernetes集群中仅用作查看数据，待查看的服务不再需要配置 `cap.UseK8sDiscovery()` 配置项。

```
services.AddCapDashboardStandalone();
```

同样，你需要为此Pod配置 ServiceAccount 的访问权限。