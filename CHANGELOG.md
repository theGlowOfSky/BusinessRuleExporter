# Changelog

All notable changes to this project are documented here. Versions follow the assembly/package version.

## [1.0.0.0] - Unreleased

### Added
- Plugin metadata: author Gagandeep Singh, original 32 px and 80 px tool icons, product description.
- `Properties/AssemblyInfo.cs` as the single source of assembly metadata and version.
- Solution file, `.gitignore`, xUnit test project (interpreter, variable mapping, xlsx writer).
- NuGet packaging files under `packaging/` (nuspec, package README, pack script).

### Changed (since initial draft)
- Export to Excel now suggests `[EnvironmentName]_Business_Rules.xlsx` in the Save dialog, built from the connected organization's friendly name (falls back to unique name, connection name, URL name, then `Dataverse`). Invalid file-name characters are replaced, whitespace runs become `_`, and the suffix is never duplicated. The user can still change the folder and file name.
- The Logic column (grid and Excel) now uses the exact Dataverse logical names from the variable mapping instead of prettified display names. Variables with no mapping are shown unchanged (for example `v9`) rather than guessed.

### Fixed
- `BusinessRuleInterpreter.MapVariables`: a comparison such as `v2 == 1` followed on the same line by an `attributes.get(...)` call was read as an assignment, mapping `v2` to the wrong field.
- `XlsxWriter`: characters that are illegal in XML 1.0 (for example control characters in `clientdata`) are now removed so the generated workbook stays valid.

### Changed
- Project file: removed placeholder `Authors`/`Company`/`Version`; runtime assets of XrmToolBox development packages are excluded so only `BusinessRuleExporter.dll` is produced.
