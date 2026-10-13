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
- [ ] Clicking **Load business rules** while disconnected opens the connection dialog
- [ ] With a development connection: rules load; count matches the maker portal (category 2 / type 1)
- [ ] UI stays responsive while loading (progress overlay shown)
- [ ] **Export to Excel** writes a file that opens in Excel without a repair prompt
- [ ] Resizing/maximizing XrmToolBox resizes grid and toolbar correctly
- [ ] The Save dialog suggests `<environment>_Business_Rules.xlsx` for the connected environment; the folder and name can still be changed and the overwrite prompt still appears
- [ ] The Logic column (grid and Excel) shows logical names such as `int_interrelateprogram` and matches the Variable Mapping column
- [ ] Export with nothing loaded shows the "Load business rules before exporting" message
