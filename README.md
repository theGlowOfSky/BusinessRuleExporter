# BusinessRuleExporter-Source
Source code for **Business Rule Exporter**, an XrmToolBox tool by Gagandeep Singh that retrieves Dataverse business rules (`workflow` where `category = 2` and `type = 1`), interprets the JavaScript in `clientdata` into readable logic, and exports the result to Excel.

> Keep this repository **private**. The public showcase lives in the separate `BusinessRuleExporter` repository and contains documentation only.

## Layout

```text
BusinessRuleExporter.sln
BusinessRuleExporter/          plugin (net48, SDK-style csproj, PackageReference)
BusinessRuleExporter.Tests/    xUnit tests (net48)
packaging/                     nuspec, NuGet README, pack.ps1
assets/                        original tool icons (32, 80, 128 px)
docs/                          build/test and release runbooks
```

## Dependencies

| Package | Version | Role |
|---|---|---|
| `XrmToolBoxPackage` | 1.2025.10.74 | Development-time SDK. Not a runtime dependency of the published package. |
| `MscrmTools.Xrm.Connection` | 1.2025.9.64 | Development-time reference, pinned directly because `XrmToolBoxPackage` pulls 1.2025.7.63. |

Both are `PrivateAssets=all` and `ExcludeAssets=runtime`: XrmToolBox supplies these assemblies at runtime. The NuGet package declares a dependency on `XrmToolBox` instead (see `packaging/BusinessRuleExporter.nuspec`). The plugin uses no third-party runtime libraries.

## Build and test

See [docs/BUILD_AND_TEST.md](docs/BUILD_AND_TEST.md). Release workflow: [docs/RELEASE_RUNBOOK.md](docs/RELEASE_RUNBOOK.md).

## Rules for this repository

- Never commit exports (`*.xlsx`, `*.csv`), connection files, API keys, or real client/employer business-rule data.
- Test data must be synthetic or sanitized.
