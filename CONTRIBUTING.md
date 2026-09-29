# Contributing

1. Create a branch from `main`.
2. Do not commit `local.settings.json`, `.env`, credentials or generated outputs.
3. Run restore, formatting, build and tests before opening a pull request.
4. Keep public contracts documented in `openapi.yaml`.
5. Add or update tests for behavior changes.
6. Explain security and infrastructure impacts in the pull request.

```bash
dotnet restore --locked-mode
dotnet format --verify-no-changes --no-restore
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build
```
