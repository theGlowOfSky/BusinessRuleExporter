# Business Rule Exporter

A custom XrmToolBox utility for retrieving Microsoft Dataverse business rules, interpreting available client-side logic, and exporting rule details to Excel.

## Features

- Uses the active XrmToolBox connection to read business rules from the Dataverse `workflow` table.
- Interprets the JavaScript stored in each rule's `clientdata` into readable IF / THEN / ELSE text (best effort).
- Shows rules in a grid and exports them to an `.xlsx` workbook.

## Install

Open XrmToolBox, go to **Tool Library**, search for **Business Rule Exporter**, and install it.

## Notes

Option-set values are shown as integers because labels are not stored on the business rule record. Logic interpretation is best effort; review complex rules in the Power Apps designer.

Author: Gagandeep Singh
