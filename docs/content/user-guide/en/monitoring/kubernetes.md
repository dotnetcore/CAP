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

## Using Dashboard Standalone

You can use the Dashboard standalone without configuring CAP, in this case, the Dashboard can be deployed as a separate Pod in the Kubernetes cluster just for data viewing. The service to be viewed no longer needs to configure the `cap.UseK8sDiscovery()` option.

```
services.AddCapDashboardStandalone();
```

Similarly, you need to configure the access for the ServiceAccount for this Pod.