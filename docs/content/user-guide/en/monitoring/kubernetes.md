# Kubernetes

[Kubernetes](https://kubernetes.io), also known as K8s, is an open-source system for automating deployment, scaling, and management of containerized applications.

## Kubernetes in the Dashboard

The Dashboard has supported Kubernetes as a service discovery mechanism since version 7.2.0. You can navigate to the Nodes page, select a Kubernetes namespace, and CAP will list all Services within that namespace. After clicking the Switch button, the Dashboard will check if the CAP service of that node is available. If it is, the Dashboard will proxy to the switched node to display data.

Here is a configuration example:

```cs
services.AddCap(x =>
{
    // ...
    x.UseDashboard();
    x.UseK8sDiscovery();
});
```

## UseK8sDiscovery Configuration

This configuration option controls whether the Dashboard/Nodes page lists every K8s `Service` by default. If set to `True`, only services with the `dotnetcore.cap.visibility: show` label will be listed. See the **Kubernetes Labels Configuration** section for more information about labels.

* **ShowOnlyExplicitVisibleNodes** 

> Default: false

```cs
services.AddCap(x =>
{
    // ...
    x.UseK8sDiscovery(opt =>
    {
        opt.ShowOnlyExplicitVisibleNodes = true;
    });
});
```

The component automatically detects whether it is running inside a Kubernetes cluster. If it is, the Pod must be granted Kubernetes API permissions. Refer to the next section.

## Assigning Pod Access to Kubernetes API 

If the ServiceAccount associated with your Deployment does not have access to the Kubernetes API, you must grant `namespaces` and `services` resources with `get` and `list` permissions.

Here is an example YAML. First, create a ServiceAccount and ClusterRole with the appropriate permissions, then bind them using ClusterRoleBinding. Finally, use `serviceAccountName: api-access` in your Deployment.

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

From version `8.3.0` and onwards you can use a `Role` instead of `ClusterRole` to allow discovery of services only inside the namespace that the dashboard is running. Kubernetes Roles has limited jurisdiction inside the namespace. In the above example just remove ClusterRole and ClusterRoleBinding and instead use the following: 

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

## Kubernetes Labels Configuration

The list of Nodes showed in the dashboard can be controlled by adding labels to the to your kubernetes services. 


- `dotnetcore.cap.visibility` label is used to show or hide a service from the list. 

    > Allowed Values: show | hide

    > Examples: `dotnetcore.cap.visibility: show` or `dotnetcore.cap.visibility: hide`

By default every k8s service is listed with the first port found in the service. However if more ports are present on the service you can select the wanted by using the following labels: 

- `dotnetcore.cap.portName` label is used to filter the wanted port of the service. 

    > Allowed Values: string

    > Examples: `dotnetcore.cap.portName: grpc` or `dotnetcore.cap.portName: http`

If not found any port with the given name, it will try to match the next label portIndex

- `dotnetcore.cap.portIndex` label is used to filter the wanted port of the service. This filter is taken into consideration only if no label portName is set or a non matching portName is set.

    > Allowed Values: number represented as string ex: '2' or '14'

    > Examples: `dotnetcore.cap.portIndex: '1'` or `dotnetcore.cap.portIndex: '3'`

  If the provided index is outside of bounds then it will fallback to the first port (index:0)





## Using Dashboard Standalone

You can use the Dashboard standalone without configuring CAP, in this case, the Dashboard can be deployed as a separate Pod in the Kubernetes cluster just for data viewing. The service to be viewed no longer needs to configure the `cap.UseK8sDiscovery()` option.

```
services.AddCapDashboardStandalone();
```

Similarly, you need to configure the access for the ServiceAccount for this Pod.