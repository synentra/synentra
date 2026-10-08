# Synentra Helm deployment sample

A complete, minimal chart for the [Helm Chart Deployment tutorial](http://synentra.io/docs/tutorials/helm-chart-deployment).

The chart files are extracted from the supplied tutorial text. Consult the linked tutorial for the full explanation and production adaptations. This is a tutorial sample, not an official production chart.

## Included files

```text
synentra-helm-sample/
├── README.md
├── .gitignore
└── synentra-chart/
    ├── .helmignore
    ├── Chart.yaml
    ├── values.yaml
    └── templates/
        ├── _helpers.tpl
        ├── deployment.yaml
        ├── service.yaml
        └── pvc.yaml
```

The baseline uses one Synentra replica, SQLite at `/data/synentra.db`, memory cache, a 1 GiB PersistentVolumeClaim, a ClusterIP Service on port 7080, and startup/readiness/liveness probes at `/health`. The `Recreate` strategy avoids overlapping pods during upgrades, with brief unavailability. Keep `replicaCount: 1` for this SQLite sample.

## Prerequisites

- Docker and kind for a local cluster, or an existing disposable Kubernetes cluster.
- kubectl, Helm 3 or 4, and curl.
- A default StorageClass capable of provisioning the requested volume, or a suitable explicit storage class.
- Cluster permissions to create namespaces, Deployments, Services, and PersistentVolumeClaims.
- Cluster network access to pull `ghcr.io/synentra/synentra:latest`.

Commands below use Bash-compatible syntax. Run them from this sample folder unless stated otherwise.

```bash
docker version
kubectl version --client
helm version
kind version
```

Docker and kind are only needed when creating the local cluster.

## 1. Select or create a cluster

Skip cluster creation if you already have a disposable cluster selected.

```bash
kind create cluster --name synentra
kubectl cluster-info --context kind-synentra
kubectl get nodes
```

Verify the current context before making changes:

```bash
kubectl config current-context
kubectl get storageclass
```

## 2. Validate the chart

```bash
helm lint ./synentra-chart
helm template synentra ./synentra-chart --namespace synentra > rendered.yaml
```

For server-side validation, create the namespace first and explicitly target it. Rendering with `--namespace` supplies Helm's release namespace; these templates do not set `metadata.namespace` themselves.

```bash
kubectl create namespace synentra --dry-run=client -o yaml | kubectl apply -f -
kubectl apply --dry-run=server --namespace synentra -f rendered.yaml
```

Expected lint result: `1 chart(s) linted, 0 chart(s) failed`. Server-side validation requires a running cluster. `rendered.yaml` is an inspection artifact; Helm performs the actual installation. It is excluded from Git and chart packaging.

## 3. Install

```bash
helm upgrade --install synentra ./synentra-chart \
  --namespace synentra \
  --create-namespace \
  --wait \
  --timeout 5m

kubectl get deploy,pod,service,pvc -n synentra
kubectl rollout status deployment/synentra-synentra -n synentra --timeout=5m
```

Expected: one ready pod, an available Deployment, a ClusterIP Service on port 7080, and a Bound PVC. Resource names in this README assume the release name `synentra`.

If your cluster has no default StorageClass, add `--set persistence.storageClassName=YOUR_STORAGE_CLASS` to installation and subsequent upgrade commands, replacing the placeholder with an actual class name.

## 4. Check health

In one terminal:

```bash
kubectl port-forward -n synentra service/synentra-synentra 7080:7080
```

In another terminal:

```bash
curl --fail --silent --show-error http://localhost:7080/health
```

The tutorial shows a JSON response with `status: Healthy` and a variable `healthCheckDuration`. Stop forwarding with Ctrl+C. If local port 7080 is busy, forward `17080:7080` and call `http://localhost:17080/health`.

Inspect logs and probe events:

```bash
kubectl logs -n synentra deployment/synentra-synentra --tail=100
kubectl describe pod -n synentra -l app.kubernetes.io/instance=synentra
```

## 5. Upgrade and inspect history

Edit `synentra-chart/Chart.yaml`: change `version: 0.1.0` to `version: 0.1.1`, leaving `appVersion` unchanged.

```bash
helm lint ./synentra-chart
helm upgrade synentra ./synentra-chart \
  --namespace synentra \
  --wait \
  --timeout 5m
helm history synentra -n synentra
```

This records a new release revision. With these templates, a chart-version-only change updates resource metadata but does not change the Deployment's pod template, so it does not itself trigger a pod restart. Changes to the pod template use the `Recreate` strategy. Verify health and confirm that the PVC remains Bound after upgrades.

## 6. Roll back the lab release

If revision 1 is the initial installation from this lab:

```bash
helm rollback synentra 1 -n synentra --wait --timeout 5m
helm history synentra -n synentra
```

This exercise changes chart metadata only. For application upgrades, check database migration compatibility before rollback; Helm does not restore the database.

## Configuration

Edit `synentra-chart/values.yaml` or use a separate override file with `-f` on install and upgrade.

| Value | Default | Purpose |
| --- | --- | --- |
| `replicaCount` | `1` | Keep one replica for this SQLite lab. |
| `image.repository` | `ghcr.io/synentra/synentra` | Container repository. |
| `image.tag` | `latest` | Use a reviewed, published tag for controlled deployments. |
| `image.pullPolicy` | `IfNotPresent` | Cached images may be reused with a mutable tag. |
| `service.port` | `7080` | Service port; the container still listens on 7080. |
| `persistence.enabled` | `true` | If false, uses ephemeral `emptyDir`; data is lost when the pod is removed. |
| `persistence.size` | `1Gi` | Requested PVC capacity. |
| `persistence.storageClassName` | `""` | Omits the field and uses the cluster default StorageClass. |
| `resources` | `{}` | Set requests/limits after measuring the workload. |
| `podAnnotations` | `{}` | Additional pod annotations. |
| `podSecurityContext` | `{}` | Volume/user settings, validated against the image. |
| `containerSecurityContext.allowPrivilegeEscalation` | `false` | Disables privilege escalation. |
| `probes` | See values file | Startup, readiness, and liveness timing. |

The image template uses `repository:tag`; digest selection is not implemented in this minimal chart. StorageClass changes on an existing PVC generally require storage migration rather than an in-place Helm change.

## Troubleshooting

**PVC Pending:** inspect storage provisioning and select a valid StorageClass.

```bash
kubectl get storageclass
kubectl describe pvc synentra-synentra -n synentra
```

**ImagePullBackOff or failed probes:** inspect events and logs. Verify GHCR access, image tag availability, startup time, and the port configuration.

```bash
kubectl describe pod -n synentra -l app.kubernetes.io/instance=synentra
kubectl logs -n synentra deployment/synentra-synentra --tail=100
kubectl get events -n synentra --sort-by=.lastTimestamp
```

**SQLite write permission errors:** inspect `/data` ownership and the image's runtime user. Configure a tested pod security context or storage provisioner fix.

```bash
kubectl exec -n synentra deployment/synentra-synentra -- id
kubectl exec -n synentra deployment/synentra-synentra -- ls -ld /data
```

**Template errors or stuck upgrades:**

```bash
helm template synentra ./synentra-chart --namespace synentra --debug
helm status synentra -n synentra
kubectl describe deployment synentra-synentra -n synentra
```

## Cleanup

These commands remove the lab deployment and its storage claim. The chart has no PVC retention annotation: uninstall deletes its PVC, and the storage reclaim policy determines whether the backing volume is deleted. Back up any data you want to keep first.

```bash
helm uninstall synentra -n synentra
kubectl delete namespace synentra
```

Only if you created the dedicated kind cluster for this lab:

```bash
kind delete cluster --name synentra
```

## Scope and validation

This sample does not configure ingress, TLS, external Secrets, PostgreSQL, Redis, or high availability. See the [tutorial's production adaptations](http://synentra.io/docs/tutorials/helm-chart-deployment) before extending it beyond a local lab.

The six chart source files were checked against the supplied tutorial text, and plain YAML files were parsed during packaging. Helm, kubectl, and a Kubernetes cluster were unavailable in the packaging environment, so Helm lint/render and live installation were not executed. Run the validation commands above in your environment.
