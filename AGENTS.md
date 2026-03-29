# Repository Guidelines

## Project Structure & Module Organization
`MatrixEase.sln` is the top-level solution. Core application code lives in `MatrixEase.App/` (Electron-hosted desktop app), `MatrixEase.Web/` (ASP.NET Core web app), and `MatrixEase.Manga/` (shared matrix-processing library). `ExcelReader/` and `web_blaster/` are supporting projects. Manual test utilities live in `MatrixEase.Tester/`. Static marketing and documentation sites are under `www.matrixease.com/`, `docs.matrixease.com/`, and `static.matrixease.wwwroot/`. Deployment scripts and container configs are in `docker/`, `config/`, and `docs/`.

## Build, Test, and Development Commands
Use scoped `dotnet` commands instead of building the whole solution with the modern SDK.

- `dotnet build MatrixEase.App/MatrixEase.App.csproj --configuration Debug`: build the desktop app.
- `dotnet build MatrixEase.Web/MatrixEase.Web.csproj --configuration Debug`: build the web app.
- `dotnet build MatrixEase.Manga/MatrixEase.Manga.csproj --configuration Debug`: build the shared library.
- `dotnet run --project MatrixEase.Web/MatrixEase.Web.csproj`: run the web app locally.
- `dotnet run --project MatrixEase.App/MatrixEase.App.csproj`: run the desktop app host.
- `docker-compose -f docker/matrixease_dotnet/docker-compose.yml up -d`: start the packaged .NET deployment stack.

`dotnet build MatrixEase.sln` currently fails on the legacy website projects (`docs.matrixease.com`, `www.matrixease.com`) because they require the .NET Framework ASP.NET compiler.

## Coding Style & Naming Conventions
Follow existing C# conventions: 4-space indentation, braces on new lines, `PascalCase` for types and public members, `camelCase` for locals/parameters, and leading-underscore private fields such as `_data`. Keep controllers in `Controllers/`, reusable helpers in `Utility/` or `Common/`, and match filenames to primary class names. Preserve existing static-site naming and folder layout when editing HTML/CSS/JS assets.

## Testing Guidelines
There is no formal xUnit/NUnit test suite in this repo. Use `MatrixEase.Tester/` for manual verification and run targeted smoke tests against the app or web project after changes. When touching upload, parsing, or deployment paths, note the sample deployment files referenced in `docs/build_and_deploy.txt`.

## Commit & Pull Request Guidelines
Recent history uses short, imperative, lower-case commit subjects such as `fix encryption` and `Create README.md`. Keep commits focused and descriptive. PRs should include a concise summary, affected projects, manual test notes, configuration or deployment impact, and screenshots for UI or static-site changes.

## Configuration & Security Tips
Use `MatrixEase.App/appsettings_sample.json` as the template for local settings; do not commit secrets or user-specific values. Treat Google auth, SendGrid, and deployment credentials as external configuration.

## Agent-Specific Instructions
In Codex CLI, any command or prompt line prefixed with `>>>` should be treated as an instruction being added to the session, not as a shell command to execute. Preserve the literal text after `>>>` and apply it as guidance for subsequent work.
