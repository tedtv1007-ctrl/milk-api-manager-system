# Memory

## Development Flow & Stability (穩定開發流程)
- **Mandatory Verification**: BEFORE any `git push` or concluding a task, you MUST run `./scripts/verify-all.sh` (Linux) or `./scripts/verify-all.ps1` (Windows).
- **Zero-Failure Policy**: Never commit or push if any test in the verification suite fails.
- **Reporting**: Always check `E2E_TEST_REPORT.md` after running the verification script to confirm all components are green.
- **Code Style**: Adhere to existing .NET naming conventions and ensure PII masking logic is covered by at least one E2E test.

## Multi-VPS & Human Collaboration (人機多節點協作規範)
- **Central Repository**: All collaborators must use `tedtv1007-ctrl/milk-api-manager-system` as the main remote `origin`.
- **Concurrency Locking**:
    1. START: Run `git pull origin main` and check `HEARTBEAT.md`.
    2. LOCK: If `USER_ACTIVE` is not `None`, do NOT modify code. Only perform research or wait.
    3. CLAIM: If clear, update `HEARTBEAT.md` with your ID before starting.
- **Verification Hierarchy**:
    - **Local/Human**: Must pass `./scripts/verify-all.ps1` before pushing.
    - **VPS**: Must pass `dotnet test`, then push and monitor GitHub CI.

## Security Notes
- **Secrets**: All passwords and API keys are managed via `.env` file (gitignored). See `.env.example` for template.
- **API Auth**: All `/api/*` endpoints require `X-API-KEY` header. key is set via `API_AUTH_KEY` env var.
