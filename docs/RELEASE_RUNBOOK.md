# Release runbook

Run these steps yourself with your own authenticated accounts. Never paste keys into chats or commit them.

## 0. Preconditions
1. Replace `YOUR_GITHUB_USERNAME` in `packaging/BusinessRuleExporter.nuspec`.
2. Confirm `AssemblyVersion`/`AssemblyFileVersion` in `Properties/AssemblyInfo.cs` equal `<version>` in the nuspec.
3. Complete the manual checklist in `docs/BUILD_AND_TEST.md`.
4. Check the ID is free: open `https://www.nuget.org/packages/GagandeepSingh.BusinessRuleExporter` — it must return "not found".
5. Confirm `<minimum XrmToolBox version>` (nuspec dependency `XrmToolBox` 1.2025.10.74) exists at `https://www.nuget.org/packages/XrmToolBox`.

## 1. Pack
```powershell
powershell -ExecutionPolicy Bypass -File packaging\pack.ps1
```
Open `artifacts\GagandeepSingh.BusinessRuleExporter.1.0.0.nupkg` in NuGet Package Explorer. Expected contents: `lib/net48/Plugins/BusinessRuleExporter.dll`, `icon.png`, `README.md`, and a single dependency on `XrmToolBox`. No `.cs`, `.pdb`, or host DLLs. NuGet normalizes `1.0.0.0` to `1.0.0`.

## 2. NuGet.org account and API key
1. Sign in at https://www.nuget.org with your Microsoft account (or create one), verify the e-mail.
2. Account menu → **API Keys** → **Create**: key name `BusinessRuleExporter-publish`, **Push new packages and package versions**, glob pattern `GagandeepSingh.BusinessRuleExporter*`, short expiry.
3. Store it in an environment variable for the session only:
```powershell
$env:NUGET_API_KEY = Read-Host "NuGet API key"   # typed locally, never shared
```

## 3. Publish (after final review)
```powershell
dotnet nuget push artifacts\GagandeepSingh.BusinessRuleExporter.1.0.0.nupkg --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json
Remove-Item Env:NUGET_API_KEY
```

## 4. Verify
- `https://www.nuget.org/packages/GagandeepSingh.BusinessRuleExporter` shows version, author, icon, README, dependency `XrmToolBox`, owner = your account (package page → **Manage package**).
- Wait for validation and indexing (can take from several minutes to longer) before registering.

## 5. Register in the XrmToolBox Tool Library
1. Sign in at https://www.xrmtoolbox.com and open https://www.xrmtoolbox.com/plugins/new/.
2. Enter the exact package ID `GagandeepSingh.BusinessRuleExporter`; the portal reads the package metadata.
3. Submit. An administrator reviews it (can take days). Do not announce it as listed until it appears in the Tool Library.

## 6. Updating later
Bump the version in `AssemblyInfo.cs` (all three attributes), `CHANGELOG.md`, nuspec `<version>` and `<releaseNotes>` together, repeat steps 1, 3, 4. A published version number can never be reused; unlist (not delete) bad versions.

## 7. Git remotes
```bash
# Private source (this folder)
git init -b main
git add . && git status          # review the file list before committing
git commit -m "Initial commit: Business Rule Exporter 1.0.0.0"
git remote add origin https://github.com/YOUR_GITHUB_USERNAME/BusinessRuleExporter-Source.git
git push -u origin main

# Public showcase (the separate BusinessRuleExporter-Showcase folder: documentation only)
cd ../BusinessRuleExporter-Showcase
git init -b main
git add . && git status
git commit -m "Add project showcase documentation"
git remote add origin https://github.com/YOUR_GITHUB_USERNAME/BusinessRuleExporter.git
git push -u origin main
```
The public repository must serve `assets/icon-128.png` at the `iconUrl` in the nuspec (raw.githubusercontent.com works for public repos).
