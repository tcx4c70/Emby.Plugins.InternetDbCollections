# Repository Guidelines

## Project Structure & Module Organization
- `src/` contains the Emby plugin code.
- `src/Collector/` holds datasource adapters, `src/Common/` has library update logic, `src/ScheduledTasks/` contains Emby jobs, and `src/Models/` plus `src/Utils/` hold shared DTOs and helpers.
- `src/Configuration/` includes the embedded settings page and plugin config objects.
- `tests/` contains the xUnit project; keep test folders aligned with `src/` (for example `tests/Collector/` and `tests/Models/Trakt/`).

## Build, Test, and Development Commands
- `dotnet build src/Emby.Plugins.InternetDbCollections.csproj` builds the plugin.
- `dotnet build src/Emby.Plugins.InternetDbCollections.csproj -c Release` produces the release DLL and runs ILRepack.
- `dotnet test Emby.Plugins.InternetDbCollections.sln -m:1 -nodeReuse:false` runs the full test suite reliably in this repo.
- `dotnet test Emby.Plugins.InternetDbCollections.sln --collect:"XPlat Code Coverage"` generates coverage output.

## Coding Style & Naming Conventions
- Follow `.editorconfig`: 4-space indentation, UTF-8, LF endings, file-scoped namespaces, braces on new lines, and no trailing whitespace.
- Prefer explicit types over `var` when practical.
- Use `PascalCase` for public types and members, `_camelCase` for private/internal fields, and `s_` for static fields.
- Keep lines under 120 characters where practical.

## Testing Guidelines
- Use xUnit (`[Fact]`, `[Theory]`) and keep tests focused on one behavior.
- Name test files `*Tests.cs` and mirror the source folder structure.
- Prefer pure unit tests for collectors, mappings, progress helpers, and configuration logic; avoid live network calls.

## Commit & Pull Request Guidelines
- Commit messages follow Conventional Commit style used in history: `test:`, `fix(scope):`, `ci:`, `refactor(test):`.
- PRs should explain behavior changes, list commands run, and mention any config or compatibility impact.
- Add screenshots only for UI/config page changes.

## Security & Configuration Tips
- Do not commit Emby plugin secrets. MDBList and Trakt credentials belong in plugin configuration, not source files.
- If you change collector parsing, check the matching cleanup and external-ID logic together.

## Third-Party DLLs & Notices
- Runtime dependencies are bundled into the release DLL with ILRepack. Keep `src/ILRepack.targets` and `NOTICE` in sync with `src/Emby.Plugins.InternetDbCollections.csproj`.
- When adding a new third-party dependency, first add a `PackageReference` in `src/Emby.Plugins.InternetDbCollections.csproj`.
- If the package ships a runtime DLL, add that DLL to `src/ILRepack.targets` so it is merged into the release artifact.
- Add a short entry to `NOTICE` with the package name, version, license, and project URL, for example `Polly 8.0.0 - BSD-3-Clause - https://github.com/App-vNext/Polly`.
- Rebuild with `dotnet build src/Emby.Plugins.InternetDbCollections.csproj -c Release` and rerun `dotnet test Emby.Plugins.InternetDbCollections.sln -m:1 -nodeReuse:false` after dependency changes.
- Test-only packages should stay in `tests/Emby.Plugins.InternetDbCollections.Tests.csproj` and do not need ILRepack or NOTICE updates.
