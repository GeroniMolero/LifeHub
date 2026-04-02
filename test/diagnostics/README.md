# Diagnostics Tests

This folder stores useful ad-hoc verification scripts used during refactors.

## Scripts

- `diag_permissions.ps1`
  - End-to-end permission check against local backend (`http://localhost:5000`).
  - Creates a space and document, shares with viewer user, and verifies access behavior.

- `smoke_backend_in_container.sh`
  - Smoke test intended to run inside the backend container.
  - Validates login, space/document creation, snapshot creation, and restore flow.

## Typical usage

From repository root:

```powershell
# Run backend smoke inside container
Docker compose -f docker-compose.dev.yml exec backend sh /app/smoke_backend_in_container.sh

# Run permissions diagnostic from host (PowerShell)
.\test\diagnostics\diag_permissions.ps1
```
