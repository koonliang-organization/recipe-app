# Backend Tests and Coverage

This folder contains unit tests for the backend (.NET 8). Tests use xUnit, FluentAssertions, Moq, and collect coverage via coverlet.

## Prerequisites

- .NET SDK 8.x installed (`dotnet --version` shows 8.*)
- Optional: ReportGenerator tool for HTML/text coverage reports
  - Install once: `dotnet tool install -g dotnet-reportgenerator-globaltool`
  - Ensure `~/.dotnet/tools` is on your PATH (Linux/macOS): `export PATH="$PATH:$HOME/.dotnet/tools"`

## Projects

- `BuildingBlocks.Common.Tests`
- `Core.Domain.Tests`
- `Core.Application.Tests`

## Run Tests (all)

```
# Restore solution
dotnet restore backend/RecipeAppApi.sln

# Run all tests + collect coverage (Cobertura) into backend/TestResults
dotnet test backend/RecipeAppApi.sln \
  --collect:"XPlat Code Coverage" \
  --results-directory backend/TestResults \
  --verbosity minimal
```

## Run Tests (single project / filter)

```
# Single test project
dotnet test backend/tests/Core.Application.Tests/Core.Application.Tests.csproj \
  --collect:"XPlat Code Coverage" \
  --results-directory backend/TestResults

# Filter by namespace/class/name (example)
dotnet test backend/tests/Core.Application.Tests/Core.Application.Tests.csproj \
  --filter "FullyQualifiedName~LoginCommandHandler" \
  --collect:"XPlat Code Coverage" \
  --results-directory backend/TestResults
```

## Generate Coverage Report (HTML + text)

After running tests with `--collect:"XPlat Code Coverage"`:

```
# Generate reports for Core.Domain and Core.Application only (for local insight)
reportgenerator \
  -reports:"backend/TestResults/**/coverage.cobertura.xml" \
  -targetdir:"backend/TestResults/CoverageReport" \
  -reporttypes:"Html;TextSummary" \
  -classfilters:"+*;-*.Migrations.*;-Program" \
  -assemblyfilters:"+Core.Domain;+Core.Application" \
  -historydir:"backend/TestResults/CoverageReport/History"

# View outputs
cat backend/TestResults/CoverageReport/Summary.txt
# Open HTML report in your browser
# Linux:   xdg-open backend/TestResults/CoverageReport/index.html
# macOS:   open backend/TestResults/CoverageReport/index.html
# Windows: start backend/TestResults/CoverageReport/index.html
```

# Generate full report including Infrastructure and Lambdas (threshold enforced in CI)
reportgenerator \
  -reports:"backend/TestResults/**/coverage.cobertura.xml" \
  -targetdir:"backend/TestResults/CoverageReportAll" \
  -reporttypes:"Html;TextSummary" \
  -classfilters:"+*;-*.Migrations.*;-Program" \
  -assemblyfilters:"+Core.Domain;+Core.Application;+Infrastructure.Persistence;+Recipe;+User" \
  -historydir:"backend/TestResults/CoverageReportAll/History"

To include all assemblies in the report, remove `-assemblyfilters` or use `-assemblyfilters:"+*"`.

## Clean Previous Results

```
rm -rf backend/TestResults
```

## Troubleshooting

- Missing .NET 8 runtime: Install .NET 8 SDK and rerun `dotnet restore`/`dotnet test`.
- `reportgenerator` not found: Ensure it’s installed and on PATH (see prerequisites). On Windows, the tool is in `%USERPROFILE%\.dotnet\tools`.

## CI Coverage

The GitHub Actions workflow generates two artifacts and enforces overall line coverage:
- `backend-coverage-report` (Domain + Application) — informational.
- `backend-coverage-report-all` (Domain + Application + Infrastructure + Lambdas) — pipeline enforces >= 70% line coverage on this report.

## Optional: Enforce Coverage Threshold in CI

If you want to fail the build when coverage is below a threshold, you can switch to coverlet.msbuild in test projects:

1) Add to each test `.csproj`:
```
<ItemGroup>
  <PackageReference Include="coverlet.msbuild" Version="6.0.0" PrivateAssets="All" />
</ItemGroup>
```

2) Run with:
```
dotnet test backend/RecipeAppApi.sln \
  /p:CollectCoverage=true \
  /p:CoverletOutputFormat=cobertura \
  /p:Threshold=80
```

(We currently use the DataCollector approach; the above is an alternative if you need thresholds.)
