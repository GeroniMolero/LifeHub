# Frontend Test Structure

This frontend keeps tests outside `src` under `test/`.

## Folders

- `test/unit`: fast isolated tests for components/services/helpers.
- `test/integration`: feature-level tests combining multiple parts.
- `test/e2e`: end-to-end UI flows.

## Current rules

- Unit test files use `*.spec.ts`.
- Karma discovers specs from `test/**/*.spec.ts` via `src/test.ts`.
- TypeScript test config includes `test/**/*.spec.ts` in `tsconfig.spec.json`.

## Naming

Use feature-oriented folders, for example:

- `test/unit/spaces/space-workspace.component.spec.ts`
- `test/integration/spaces/space-workspace.flow.spec.ts`
- `test/e2e/spaces/space-workspace.e2e.spec.ts`
