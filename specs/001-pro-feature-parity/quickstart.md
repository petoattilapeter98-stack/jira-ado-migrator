# Quickstart: Building, Running & Verifying PRO-Parity Features

## Prerequisites
- .NET 10 SDK (`dotnet --version`)
- A test Jira instance (Cloud or Server) + a throwaway Azure DevOps project
- Jira credentials (URL + user + API token); ADO PAT with Work Items (Read/write/manage), **Bypass rules**, and **Create child nodes**

## Build & test
```bash
cd jira-azuredevops-migrator/src/WorkItemMigrator
dotnet build WorkItemMigrator.sln
dotnet test                       # runs all NUnit suites (existing + new feature tests)
```

## Run the pipeline (with new features opt-in)
```bash
# 1. Export (now also builds inventory + release sidecar when enabled)
dotnet run --project JiraExport -- \
  -u <user> -p <api-token> --url https://your.atlassian.net \
  --config config.json --force

# 2. Import
dotnet run --project WorkItemImport -- \
  --token <PAT> --url https://dev.azure.com/yourorg \
  --config config.json
```

## Enable each capability (config.json)
```jsonc
{
  "build-inventory": true,
  "correct-embedded-links": true,
  "include-remote-links": true,
  "include-branch-links": true,
  "version-target": "tags",
  "state-date-map": [
    { "state": "Active", "date-field": "Microsoft.VSTS.Common.ActivatedDate" }
  ],
  "field-map": { "field": [
    { "source": "fixVersions", "target": "System.Tags", "mapper": "MapFixVersions" },
    { "source": "versions",    "target": "System.Tags", "mapper": "MapAffectsVersions" }
  ]}
}
```

## Verify (acceptance smoke checks → spec Success Criteria)
| Check | Expectation | SC |
|---|---|---|
| Query `Tags Contains 'fix:2.3.0'` in ADO | returns the issues assigned that fix version | SC-001 |
| Open a migrated iteration | shows start/end dates from the Jira sprint | SC-002 |
| Open a custom-workflow item's history | activated/resolved/closed dates match Jira | SC-003 |
| Open a description that linked another in-scope issue | link points to the ADO work item, not Jira | SC-004 |
| Check `release-metadata.json` + run log | every version + its dates/status present | US1 |
| Inspect a work item with web links / branches | hyperlinks and branch links present | SC-005 |
| Run with all toggles **off** | output equivalent to current Community edition | SC-007 |

## No-regression guard
Run a representative project once with all new toggles disabled and diff the produced work-item JSON against a baseline export — they must match (SC-007). Then enable features and re-run.
