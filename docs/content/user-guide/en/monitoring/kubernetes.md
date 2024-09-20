# Kubernetes

[Kubernetes](https://kubernetes.io), also known as K8s, is an open-source system for automating deployment, scaling, and management of containerized applications.

## Kubernetes in the Dashboard

Our Dashboard has supported Kubernetes as a service discovery from version 7.2.0 onwards. You can switch to the *Node* page, then select a k8s namespace, and CAP will list all Services under that namespace. After clicking the *Switch* button, the Dashboard will detect whether the CAP service of that node is available. If it is available, it will proxy to the switched node for data viewing.

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

This configuration option is used to configure the Dashboard/Nodes to list every K8s `service` by default. If this is set to `True` then only services with the `dotnetcore.cap.visibility: show` label will be listed. More information on labels will be found on the **Kubernetes Labels Configuration** section.

* ShowOnlyExplicitVisibleNodes 

> Default ：false


```cs
services.AddCap(x =>
{
    // ...
    x.UseK8sDiscovery(opt=>{
      opt.ShowOnlyExplicitVisibleNodes = true;
    });
});
```

The component will automatically detect whether it is inside the cluster. If it is inside the cluster, the Pod must be granted Kubernetes Api permissions. Refer to the next section.

## Assign Pod Access to Kubernetes Api 

If the ServiceAccount associated with your Deployment does not have access to the K8s Api, you need to grant the `namespaces`, `services` resources the `get`, `list` permissions.

Here is an example yaml. First create a ServiceAccount and ClusterRole and set the related permissions, then bind them using ClusterRoleBinding. Finally, use `serviceAccountName: api-access` to specify in Deployment.

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