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
- **Commits**: follow the **Angular convention** (`feat`, `fix`, `chore`,
  `build`, `ci`, `docs`, `refactor`, `test`, ..., with optional scope like
  `feat(Collector/Letterboxd):`).
  - **Write commit titles and messages in English.**
  - **Follow the readable, multi-line format of the existing Git history.**
    Keep the Angular subject on one line, followed by a blank line. Explain the
    problem or motivation and the chosen approach in separate, short paragraphs;
    include relevant tradeoffs instead of only listing changed files.
  - **Hard-wrap the body at roughly 72 columns (at most 80)**, except for
    indivisible URLs, identifiers and trailers. Do not rely on terminal soft
    wrapping or put the whole body on one long physical line.
  - For substantial changes, follow the rationale with a concise bullet list of
    the affected behavior/surfaces and relevant regression coverage. Indent
    wrapped bullet continuations by two spaces. Separate paragraphs and the
    final trailer block with blank lines; keep trailers contiguous.
  - Use `git commit -F <message-file>` or a genuinely multi-line `-m` argument
    to preserve line breaks. Before finishing, inspect `git log -1 --format=%B`
    and check the actual stored message, not just how the shell command looks.
- **Committing is allowed by default** (unless the user explicitly says not to),
  but **split commits by _functionality_, as vertical feature slices - NOT by
  layer/module.**
  - **One commit = one end-to-end user-facing capability.** If a feature needs
    backend + frontend + migration + tests, they go in the **same** commit. Do
    **not** make a "backend changes" commit and a separate "frontend changes"
    commit for the same feature.
  - **Never** finish a large piece of work with one giant catch-all commit
    (e.g. thousands of lines). Build and commit incrementally, one capability
    at a time, each commit independently building and passing the lint/format
    gate.
- **Plans (`plan.md`)** must also be organized as **vertical feature slices**,
  not as a "backend tasks" / "frontend tasks" split.
- **`Fixes:` trailer (suggested, not required)**: for a `fix` commit, when you
  can identify the commit that introduced the bug, add a Linux-kernel-style
  trailer as the last line(s), with no blank line between it and any other
  trailers (e.g. `Co-authored-by:`):
  ```text
  Fixes: <12-char-commit-id> ("<commit-title>")
  ```
  Find the offending commit with e.g.
  `git log -S "<buggy snippet>" -- <file>` or `git blame`. Omit the trailer when
  the introducing commit can't be found or is hard to pin down - it's a
  convenience, not a hard rule.
- **NEVER `git push` without the user's explicit permission.**
- **NEVER commit `plan.md`** - it is gitignored and must stay untracked.
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
