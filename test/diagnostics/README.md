# Diagnostics Tests

This folder stores useful ad-hoc verification scripts used during refactors.

## Scripts

- `diag_permissions.ps1`
  - End-to-end permission check against local backend (`http://localhost:5000`).
  - Creates a space and document, shares with viewer user, and verifies access behavior.

- `diag_login_connectivity.ps1`
  - Quick diagnosis for login issues from browser.
  - Validates backend container visibility, Swagger availability, CORS preflight, and real login.
  - Helps distinguish between backend not ready, CORS mismatch, and invalid credentials.

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

# Run login/connectivity diagnostic from host (PowerShell)
.\test\diagnostics\diag_login_connectivity.ps1

# Optionally use another account
.\test\diagnostics\diag_login_connectivity.ps1 -Email "juan@lifehub.com" -Password "Test123!"
```
