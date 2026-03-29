# Repository Guidelines

## Project Structure & Module Organization
Core application code lives in `MatrixEase.App/` (Electron-hosted desktop app), `MatrixEase.Web/` (ASP.NET Core web app), and `MatrixEase.Manga/` (shared matrix-processing library). `web_blaster/` is a supporting project, Excel parsing comes from the `ExcelDataReader` NuGet package, and the CLI test harness lives in `MatrixEase.Tester/` with unit tests in `MatrixEase.Tester.Tests/`. Static marketing and documentation sites are under `www.matrixease.com/`, `docs.matrixease.com/`, and `static.matrixease.wwwroot/`. Deployment scripts and container configs are in `docker/`, `config/`, and `docs/`.

## Build, Test, and Development Commands
Use scoped `dotnet` commands from the repo root.

- `dotnet build MatrixEase.App/MatrixEase.App.csproj --configuration Debug`: build the desktop app.
- `dotnet build MatrixEase.Web/MatrixEase.Web.csproj --configuration Debug`: build the web app.
- `dotnet build MatrixEase.Manga/MatrixEase.Manga.csproj --configuration Debug`: build the shared library.
- `dotnet run --project MatrixEase.Web/MatrixEase.Web.csproj`: run the web app locally.
- `dotnet run --project MatrixEase.App/MatrixEase.App.csproj`: run the desktop app host.
- `docker-compose -f docker/matrixease_dotnet/docker-compose.yml up -d`: start the packaged .NET deployment stack.

## Coding Style & Naming Conventions
Follow existing C# conventions: 4-space indentation, braces on new lines, `PascalCase` for types and public members, `camelCase` for locals/parameters, and leading-underscore private fields such as `_data`. Keep controllers in `Controllers/`, reusable helpers in `Utility/` or `Common/`, and match filenames to primary class names. Preserve existing static-site naming and folder layout when editing HTML/CSS/JS assets.

## Testing Guidelines
`MatrixEase.Tester.Tests/` contains xUnit coverage for the CLI parser and spec storage. Use `dotnet test MatrixEase.Tester.Tests/MatrixEase.Tester.Tests.csproj` for those tests, and use `MatrixEase.Tester/` for CLI-based integration checks. When touching upload, parsing, or deployment paths, note the sample deployment files referenced in `docs/build_and_deploy.txt`.

## Commit & Pull Request Guidelines
Recent history uses short, imperative, lower-case commit subjects such as `fix encryption` and `Create README.md`. Keep commits focused and descriptive. PRs should include a concise summary, affected projects, manual test notes, configuration or deployment impact, and screenshots for UI or static-site changes.

## Configuration & Security Tips
Use `MatrixEase.App/appsettings_sample.json` as the template for local settings; do not commit secrets or user-specific values. Treat Google auth, SendGrid, and deployment credentials as external configuration.

## Agent-Specific Instructions
In Codex CLI, any command or prompt line prefixed with `>>>` should be treated as an instruction being added to the session, not as a shell command to execute. Preserve the literal text after `>>>` and apply it as guidance for subsequent work.
