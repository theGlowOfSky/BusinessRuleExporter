# Business Rule Exporter

**Version:** 1.2026.10.3

Version format: `Major.Year.Month.ReleaseNumberInMonth`. This is release 3 for October 2026.

**Business Rule Exporter** is an [XrmToolBox](https://www.xrmtoolbox.com/) tool developed by **Gagan Deep Singh** to retrieve, interpret, and export Microsoft Dataverse business rules into a readable Excel format.

Dataverse business rules are stored as `workflow` records. This tool targets records where `category = 2` and `type = 1`, interprets the JavaScript stored in `clientdata` into human-readable business logic, and exports the resulting information to Excel.

The goal is to make business rules easier to review, document, analyze, and maintain without manually inspecting the underlying Dataverse metadata.

## Features

- **Single-screen workflow:** Keep the table selector and business-rule preview visible side by side, with a scrollable table list on the left and a wider results area on the right.
- **Select tables before auditing:** Load environment table metadata first, then choose which tables to audit.
- **Retrieve selected business rules:** Load business rules only for the selected tables.
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
2. **Load environment tables:** Click **Load environment tables** to retrieve table/entity metadata only; business rules are not loaded at this stage.
3. **Select tables:** Search or filter the table list and select individual tables. **Select All** and **Clear Selection** apply to the full list, including tables hidden by the current filter. The live selected-table count updates as selections change, and searching does not clear selections.
4. **Load business rules in the same screen:** The interface keeps table selection on the left (about 25% of the width) and the business-rule preview on the right (about 75%). Click **Load selected business rules** to retrieve rules associated with the currently selected tables. The button is enabled only when tables are loaded for the active environment and at least one table is selected. The query filters rule records by `category = 2`, `type = 1`, and the selected table association in `workflow.primaryentity`.
5. **Review and export:** Results appear in the right-hand preview. Click **Export to Excel** to export the current audit results. Changing the table selection invalidates the preview and disables export until business rules are loaded again; a new audit also replaces prior results. The table list is independently scrollable, and both panes show an instruction message until their data is loaded.
6. **Interpret business logic:** The tool reads supported JavaScript structures in each rule's `clientdata` and translates them into a more readable representation.

The preview and Excel export use the same six columns in this order: **Table Logical Name**, **Business Rule Name**, **Business Rule ID**, **State**, **Referenced Fields**, and **Business Rule Logic**. Business rules are sorted by Table Logical Name, then Business Rule Name, then State (case-insensitively). Status Code, Scope, and Variable Mapping are not displayed or exported. Unsupported or complex expressions should be checked against the original Dataverse business rule.

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
**LinkedIn:** [Gagan Deep Singh](https://www.linkedin.com/in/glowofsky/)
**Target framework:** .NET Framework 4.8
