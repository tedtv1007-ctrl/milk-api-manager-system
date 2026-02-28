# Architecture Remediation Checklist

## Scope
This checklist tracks executable architecture hardening work for `milk-api-manager-system`.

## Status Legend
- `[x]` Completed in current iteration
- `[ ]` Pending

## P0 (Security Baseline)
- [x] Enforce global authenticated-by-default policy (fallback authorization policy)
- [x] Keep explicit anonymous access only for health endpoints
- [x] Require `JWT_SECRET` in non-test/non-demo environments (fail-fast)
- [x] Require `API_AUTH_KEY` in non-test/non-demo environments (fail-fast)
- [x] Add explicit `[Authorize]` on core management controllers:
  - [x] `ApiController`
  - [x] `RouteController`
  - [x] `KeysController`
  - [x] `ConsumerController`
  - [x] `ConsumerGroupController`
  - [x] `AnalyticsController`
  - [x] `AuditLogsController`

## P1 (Consistency & Reliability)
- [x] Introduce DB outbox for APISIX synchronization events (v1 for Blacklist sync, config-gated)
- [x] Add APISIX reconcile job (DB as source of truth)
- [x] Implement drift report endpoint (DB vs APISIX)
- [x] Replace fire-and-forget audit shipping with durable queue/retry

## Rollout Note
- [x] Add EF migration for `SyncOutboxEntries` before enabling `Sync:Blacklist:UseOutbox=true` in non-test environments.

## P2 (Governance)
- [x] Define role matrix per endpoint (Admin/Operator/Viewer) and enforce via attributes/policies
- [x] Add environment profile guardrail checks (deny insecure production startup)
- [x] Add SLO dashboard for control-plane success, sync latency, and drift count

## Governance Artifacts
- RBAC endpoint matrix: `docs/security/rbac_endpoint_matrix.md`
- Startup guardrails: `backend/MilkShared/Services/ProductionStartupGuardrails.cs`

## Validation Evidence (Current Iteration)
- Unit tests: `101 passed`
- E2E tests: `67 passed`
