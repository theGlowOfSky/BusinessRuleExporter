# Build and test

Requirements: Windows, Visual Studio 2022 (or .NET SDK), .NET Framework 4.8 targeting pack, internet access to nuget.org for restore.

```bat
dotnet restore BusinessRuleExporter.sln
dotnet build BusinessRuleExporter.sln -c Release --no-restore
dotnet test BusinessRuleExporter.Tests\BusinessRuleExporter.Tests.csproj -c Release --no-build
```

Expected plugin output: `BusinessRuleExporter\bin\Release\BusinessRuleExporter.dll`. If other DLLs appear next to it, a package reference is missing `ExcludeAssets="runtime"`.

## Clean-clone check

```bat
git clone <private-repo-url> C:\temp\bre-check
cd C:\temp\bre-check
dotnet restore BusinessRuleExporter.sln && dotnet build BusinessRuleExporter.sln -c Release
```

## Manual test checklist (XrmToolBox on Windows)

Copy `BusinessRuleExporter.dll` to `%AppData%\MscrmTools\XrmToolBox\Plugins`, restart XrmToolBox, then verify:

- [ ] Tool appears with its small and large icon and the correct description/author/version
- [ ] Opens with no organization connected, no errors
- [ ] The plugin opens as one screen with a scrollable table selector on the left (about 25% width) and a wider business-rule preview on the right (about 75% width); no Previous/Next buttons are shown
- [ ] Before loading, the left and right panes show instructions for loading tables and business rules respectively
- [ ] Clicking **Load environment tables** while disconnected reports that a Dataverse connection is required
- [ ] With a development connection, **Load environment tables** loads metadata only; it does not automatically retrieve business rules
- [ ] Search preserves checked tables; **Select All** includes filtered-out tables; **Clear Selection** clears all selections; selected-table count updates live
- [ ] **Load selected business rules** remains disabled until tables are loaded and at least one table is selected
- [ ] With selected tables: rules load in the right preview; count matches the maker portal for the selected tables (category 2 / type 1)
- [ ] Changing the selected tables clears the old preview and disables **Export to Excel** until the rules are loaded again
- [ ] If the audit returns no rules, the right pane displays a no-results message instead of a blank grid
- [ ] UI stays responsive while loading (progress overlay shown)
- [ ] **Export to Excel** writes a file that opens in Excel without a repair prompt
- [ ] Resizing/maximizing XrmToolBox resizes grid and toolbar correctly
- [ ] The Save dialog suggests `<environment>_Business_Rules.xlsx` for the connected environment; the folder and name can still be changed and the overwrite prompt still appears
- [ ] The preview and Excel contain exactly these columns in order: `Table Logical Name`, `Business Rule Name`, `Business Rule ID`, `State`, `Referenced Fields`, `Business Rule Logic`
- [ ] Results are sorted by `Table Logical Name`, then `Business Rule Name`, then `State`
- [ ] `Status Code`, `Scope`, and `Variable Mapping` are absent from both the preview and Excel export
- [ ] The `Business Rule Logic` column shows logical field names, for example `int_interrelateprogram`, where supported by the interpreter
- [ ] Export is disabled until a current audit contains rows; the current audit is the only content exported
