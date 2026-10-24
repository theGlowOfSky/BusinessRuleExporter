# Changelog

All notable changes to this project are documented here. Versions follow the assembly/package version.

## [Unreleased]

## [1.2026.10.3] - 2026-10-23

### Changed
- Reordered the preview and Excel columns to: Table Logical Name, Business Rule Name, Business Rule ID, State, Referenced Fields, and Business Rule Logic.
- Sorted business-rule results by Table Logical Name, then Business Rule Name, then State.
- Removed Status Code, Scope, and Variable Mapping from the visible preview and Excel export.
- Applied version format `Major.Year.Month.ReleaseNumberInMonth`; this is the third release in October 2026.

## [1.0.2] - 2026-10-21

### Added
- Single-screen audit workflow with a scrollable table-selection pane on the left (about 25%) and the business-rule preview on the right (about 75%).
- Searchable table selection with Select All, Clear Selection, and a live selected-table count; the preview must be reloaded whenever the selection changes.
- README and NuGet package guidance for the side-by-side workflow and maintainer LinkedIn link.

### Changed
- Tool-list background metadata is set to white with dark text.
- Package metadata and assembly version updated to 1.0.2.

## [1.0.1] - Package metadata correction
- Corrected the NuGet package author metadata.

## [1.0.0] - Initial implementation

### Added
- Plugin metadata: author Gagan Deep Singh, original 32 px and 80 px tool icons, product description.
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
