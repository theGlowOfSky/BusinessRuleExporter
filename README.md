# Business Rule Exporter

**Business Rule Exporter** is an [XrmToolBox](https://www.xrmtoolbox.com/) tool developed by **Gagan Deep Singh** to retrieve, interpret, and export Microsoft Dataverse business rules into a readable Excel format.

Dataverse business rules are stored as `workflow` records. This tool targets records where `category = 2` and `type = 1`, interprets the JavaScript stored in `clientdata` into human-readable business logic, and exports the resulting information to Excel.

The goal is to make business rules easier to review, document, analyze, and maintain without manually inspecting the underlying Dataverse metadata.

## Features

- **Retrieve business rules:** Query Dataverse for business rules matching the configured entity filters.
- **Interpret business logic:** Convert supported JavaScript expressions in `clientdata` into more readable business logic.
- **Export to Excel:** Produce an Excel workbook containing the interpreted business rule information.
- **XrmToolBox integration:** Run the tool within the XrmToolBox environment using its existing Dataverse connection infrastructure.
- **Automated tests:** Include unit tests for validating supported functionality and reducing regression risk.

> Interpretation depends on the JavaScript structures supported by the implementation. Complex or unsupported expressions should be verified against the original Dataverse business rule.

## Repository Structure

```text
BusinessRuleExporter.sln
├── BusinessRuleExporter/          # XrmToolBox plugin source
├── BusinessRuleExporter.Tests/    # xUnit unit tests
├── packaging/                     # NuGet packaging configuration
│   ├── BusinessRuleExporter.nuspec
│   ├── README.md                  # NuGet package README
│   └── pack.ps1                   # Package creation script
├── assets/                        # Original tool icons (32, 80, 128 px)
├── docs/
│   ├── BUILD_AND_TEST.md          # Build and test instructions
│   └── RELEASE_RUNBOOK.md         # Release and publishing workflow
└── BusinessRuleExporter.sln
```

## How It Works

1. **Connect to Dataverse:** Use the connection configured through XrmToolBox.
2. **Retrieve business rules:** Query the Dataverse `workflow` table using the supported business rule filters:
   - `category = 2`
   - `type = 1`
3. **Read rule definitions:** Access the rule's `clientdata` and process its supported JavaScript structures.
4. **Interpret the logic:** Translate supported expressions into a more readable representation of the business rule.
5. **Export results:** Write the processed information into an Excel workbook for further review and documentation.

The exact exported columns, interpretation coverage, and handling of unsupported expressions are defined by the implementation.

## Technology Stack

| Component | Technology |
|---|---|
| Platform | Microsoft Dataverse |
| Host application | XrmToolBox |
| Framework | .NET Framework 4.8 |
| Language | C# |
| Project format | SDK-style `.csproj` |
| Test framework | xUnit |
| Packaging | NuGet |

## Dependencies

The plugin uses the following development-time packages:

| Package | Version | Purpose |
|---|---|---|
| `XrmToolBoxPackage` | `1.2025.10.74` | XrmToolBox development SDK |
| `MscrmTools.Xrm.Connection` | `1.2025.9.64` | Dataverse connection integration |

Both packages are configured with `PrivateAssets=all` and `ExcludeAssets=runtime`. Their assemblies are supplied by the XrmToolBox host at runtime.

The published NuGet package declares its XrmToolBox dependency through `packaging/BusinessRuleExporter.nuspec`. The plugin does not require additional third-party runtime libraries, according to the current project configuration.

## Build and Test

Prerequisites:

- Windows development environment.
- Visual Studio with .NET desktop development tools.
- .NET Framework 4.8 targeting pack.
- NuGet package restore access.

### Build

1. Open `BusinessRuleExporter.sln` in Visual Studio.
2. Restore the NuGet dependencies.
3. Select the `Release` configuration.
4. Build the solution.
5. Review the build output for errors and warnings.

### Run Tests

Run the unit tests in `BusinessRuleExporter.Tests` using Visual Studio Test Explorer or the command line:

```powershell
dotnet test BusinessRuleExporter.sln --configuration Release
```

See [docs/BUILD_AND_TEST.md](docs/BUILD_AND_TEST.md) for the repository's detailed build and test instructions.

## Packaging and Release

The repository includes a NuGet specification and PowerShell packaging script.

Before creating a release:

1. Build and test the solution in Release configuration.
2. Verify the plugin assembly and required assets.
3. Review `packaging/BusinessRuleExporter.nuspec` for package metadata and dependency declarations.
4. Generate the NuGet package using the documented packaging workflow.
5. Inspect the generated `.nupkg` contents and validate the package version.
6. Follow [docs/RELEASE_RUNBOOK.md](docs/RELEASE_RUNBOOK.md) for publishing and release verification.

Do not overwrite or republish an existing package version. Increment the package version for each new release.

## Security and Data Handling

- Never commit exported `.xlsx` or `.csv` files containing real business data.
- Never commit connection profiles, credentials, API keys, access tokens, or other secrets.
- Use synthetic or sanitized business rule definitions in tests and examples.
- Review generated exports before sharing them, as they may contain internal entity names, field names, or business logic.
- Keep credentials and environment-specific configuration outside source control.

## Limitations

- The tool is designed for the business rule records selected by its Dataverse query.
- The readability of the exported logic depends on the structures supported by the `clientdata` interpreter.
- Exported interpretations should be treated as documentation aids, not as replacements for the original Dataverse business rule definitions.
- The original business rule should be checked whenever an expression cannot be fully interpreted or the output is ambiguous.

## Contributing

Contributions that improve interpretation accuracy, export quality, test coverage, and compatibility with XrmToolBox are welcome.

When contributing:

1. Keep changes compatible with .NET Framework 4.8.
2. Add or update unit tests for changes to business rule interpretation.
3. Use synthetic or sanitized test data.
4. Verify the Release build before submitting changes.
5. Update the documentation when behavior, dependencies, or packaging changes.

## License

Refer to the license included in this repository, if applicable, for the terms governing use, modification, and distribution.

---

**Maintainer:** Gagan Deep Singh  
**Project:** Business Rule Exporter for XrmToolBox  
**Target framework:** .NET Framework 4.8
