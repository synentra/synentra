# Synentra v2 API Contract

## Scope

The `/v2` surface is the stable HTTP contract for invocation governance and execution. JSON request and response fields use camelCase. Resource identifiers are opaque strings. Clients must treat undocumented fields as additive.

## Correlation and diagnostics

Every response includes `X-Correlation-Id`. Clients may provide a UUID in that header; otherwise the server generates one. Errors use `application/problem+json` and include:

- `status`, `title`, and `type`
- `errorCode` (stable machine-readable code)
- `correlationId` and `traceId`
- `errors` for validation failures, keyed by request field

Clients should log `correlationId` and return it with support requests. Retries are safe only when the operation contract explicitly supports them.

## Mutations and idempotency

All state-changing `/v2` requests require `Idempotency-Key`; the header is echoed in the response. The key is scoped to the authenticated agent, security scope, and operation. Reusing a key with the same canonical request returns the original result; reusing it with different input returns `409` with `IDEMPOTENCY_CONFLICT`. Keys must be retained by clients when retrying a request after a timeout. Read-only `POST /v2/simulate` and `POST /v2/manifests/validate` are exempt.

A successful mutation returns the resource `Location` when a resource is created. `202 Accepted` means processing remains asynchronous and clients should poll the returned resource.

## Pagination

List endpoints use `pageNumber` (one-based, default `1`) and `pageSize` (default `20`, maximum `200`). Responses expose the requested page, page size, total count, total pages, and next/previous-page indicators where the feature returns a paginated result. Invalid values are normalized only where the endpoint explicitly documents clamping; otherwise they produce `400`.

## Lifecycle and compatibility

Invocation states progress through submission, normalization, evaluation, approval (when required), admission, and execution. Execution states are immutable once dispatch has started except through the reconciliation contract. Cancellation is only valid before dispatch. Approval decisions are valid only while pending and before expiry.

API, manifest, and persistence schema versions are independent compatibility boundaries. A reader must accept the current version and preserve unknown additive fields. Breaking changes require a new `/vN` route or an explicit schema-version migration; persistence migrations must be backward-readable during rollout.

The current compatibility values are API `v2`, manifest `synentra.io/v2alpha2`, and persistence schema `1`. Database startup records and validates the persistence schema marker; an unsupported marker prevents startup rather than allowing potentially incompatible writes.

## Core resources

- `POST /v2/invocations` — submit and govern an invocation.
- `GET /v2/invocations/{id}` — retrieve invocation and execution result.
- `POST /v2/invocations/{id}/resume` — resume an invocation after approval or external pause.
- `GET /v2/executions/{id}` — retrieve execution state and evidence.
- `POST /v2/executions/{id}/cancel` — cancel before dispatch.
- `POST /v2/executions/{id}/reconcile` — resolve an uncertain execution using configured verification.
- Approval, manifest, audit, capability, and task-grant routes are exposed under `/v2` and follow the same diagnostic, correlation, authorization, and pagination conventions.
