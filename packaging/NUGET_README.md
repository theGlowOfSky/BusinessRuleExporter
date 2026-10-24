# Business Rule Exporter

**Version:** 1.2026.10.3

Version format: `Major.Year.Month.ReleaseNumberInMonth`. This is release 3 for October 2026.

Business Rule Exporter is an XrmToolBox tool developed by Gagan Deep Singh to retrieve Dataverse business rules, interpret supported logic, and export the results to Excel.

## Workflow

The tool uses one screen with a scrollable table selector on the left (about 25% of the width) and a wider business-rule preview on the right (about 75%).

1. Connect to your Dataverse environment through XrmToolBox.
2. Click **Load environment tables** to retrieve table metadata only; business rules are not loaded at this stage. The table list appears in the left pane.
3. Search the table list and select the tables you want to audit. **Select All**, **Clear Selection**, and the live selection count help manage table selection; searching does not alter selections.
4. Click **Load selected business rules** to retrieve business rules associated with the selected tables. The button is enabled only when environment tables are loaded and at least one table is selected.
5. Review results in the right-hand preview and click **Export to Excel** to export the current audit. Changing the selection clears the preview and disables export until you load business rules again. Both panes show instructions before their data is loaded.

## Notes

- Rule retrieval targets Dataverse `workflow` records with `category = 2` and `type = 1`, associated with selected tables.
- State is displayed as Activated or Draft for the standard Dataverse state values. Logic interpretation is best effort; review complex rules in the Power Apps designer.
- A new audit or a table-selection change clears stale results so exports use the current audit.
- The preview and Excel export use the same six columns in this order: **Table Logical Name**, **Business Rule Name**, **Business Rule ID**, **State**, **Referenced Fields**, and **Business Rule Logic**.
- Business rules are sorted by Table Logical Name, then Business Rule Name, then State (case-insensitively). Status Code, Scope, and Variable Mapping are not displayed or exported.

Author: [Gagan Deep Singh on LinkedIn](https://www.linkedin.com/in/glowofsky/)
