# RBAC Endpoint Matrix

## Policy Definitions
- `ViewerOrAbove`: `Viewer`, `Operator`, `Admin`
- `OperatorOrAbove`: `Operator`, `Admin`
- `AdminOnly`: `Admin`

## Endpoint Matrix
- `AuthController`
  - `POST /api/auth/login`: `AllowAnonymous`
  - `GET /api/auth/me`: `ViewerOrAbove`
- `ApiController`
  - `GET /api/Api`, `GET /api/Api/{id}`: `ViewerOrAbove`
  - `POST/PUT/DELETE /api/Api...`: `OperatorOrAbove`
- `RouteController`
  - `GET /api/Route`, `GET /api/Route/{id}`: `ViewerOrAbove`
  - `POST/PUT/DELETE /api/Route...`: `OperatorOrAbove`
- `ConsumerController`
  - `GET /api/Consumer...`: `ViewerOrAbove`
  - `POST/DELETE /api/Consumer...`: `OperatorOrAbove`
- `ConsumerGroupController`
  - `GET /api/ConsumerGroup`: `ViewerOrAbove`
  - `PUT/DELETE /api/ConsumerGroup...`: `OperatorOrAbove`
- `KeysController`
  - `GET /api/Keys...`: `OperatorOrAbove`
  - `POST/DELETE /api/Keys...`: `AdminOnly`
- `BlacklistController`
  - all endpoints: `AdminOnly`
- `WhitelistController`
  - all endpoints: `OperatorOrAbove`
- `PiiMaskingController`
  - all endpoints: `OperatorOrAbove`
- `AuditLogsController`
  - all endpoints: `OperatorOrAbove`
- `AnalyticsController`
  - all endpoints: `ViewerOrAbove`
- `SyncStatusController`
  - `GET /api/SyncStatus`: `ViewerOrAbove`
  - `GET /api/SyncStatus/blacklist-drift`: `OperatorOrAbove`
  - `POST /api/SyncStatus/reconcile-blacklist`: `AdminOnly`
- `ApiCatalogController`
  - `GET /api/ApiCatalog`: `ViewerOrAbove`
  - `POST /api/ApiCatalog/register`: `OperatorOrAbove`
- `AlertRulesController`
  - `GET /api/AlertRules`: `ViewerOrAbove`
  - `POST/DELETE/PUT(toggle) /api/AlertRules...`: `OperatorOrAbove`
- `MockController`
  - `GET /api/Mock`: `ViewerOrAbove`
  - `POST/PUT/DELETE /api/Mock...`: `OperatorOrAbove`
- `TestExecutionController`
  - `GET /api/TestExecution/scenarios/{serviceId}`: `ViewerOrAbove`
  - `POST /api/TestExecution/scenarios`, `POST /api/TestExecution/run/{id}`: `OperatorOrAbove`
- `LoadTestController`
  - `POST /api/LoadTest/run`: `OperatorOrAbove`
- `AccessRequestController`
  - all endpoints: `AdminOnly`

## Enforcement Notes
- Global fallback policy remains enabled, so all endpoints are authenticated by default.
- Endpoint-level policy attributes are explicit to avoid accidental privilege broadening during future refactors.
