# Contract: Workspace Sidecar Files

`jira-export` writes these alongside the per-issue JSON in the workspace; `wi-import` reads them. Schemas are the integration contract between the two phases.

## release-metadata.json (NEW, US1)
Map of Jira version name → metadata. Written by `JiraCommandLine` (same pattern as `sprint-metadata.json`).

```json
{
  "2.3.0": {
    "Description": "GA release",
    "StartDate": "2026-05-01T00:00:00Z",
    "ReleaseDate": "2026-05-20T00:00:00Z",
    "Status": "Released",
    "Archived": false
  },
  "2.4.0": {
    "Description": "next",
    "StartDate": "2026-05-21T00:00:00Z",
    "ReleaseDate": null,
    "Status": "Unreleased",
    "Archived": false
  }
}
```
Rules: dates are ISO-8601 or `null`; a release with no issues is still listed; malformed dates do not abort the run (logged).

## inventory-index.json (NEW, FR-020 / US4)
Pre-run catalog used to validate embedded references. Built before the export loop.

```json
{
  "generatedUtc": "2026-05-21T10:00:00Z",
  "projects": ["ALPHA", "BETA"],
  "issueKeys": ["ALPHA-1", "ALPHA-2", "BETA-1"],
  "labels": ["backend", "urgent"],
  "versions": ["2.3.0", "2.4.0"],
  "counts": { "projects": 2, "issues": 3, "labels": 2, "versions": 2 }
}
```
Rules: `issueKeys` is the authority for bare-key validation; for very large instances the in-memory form may be a hash set while the file is the durable summary. A human-readable summary is also logged.

## sprint-metadata.json (EXISTING, US2)
Already produced today; consumed by `Agent` to set iteration `startDate`/`finishDate`.
```json
{ "Sprint 5": { "State": "closed", "StartDate": "2026-01-01T00:00:00Z", "EndDate": "2026-01-15T00:00:00Z" } }
```

## ItemsJournal.txt (EXISTING, reused for US4)
Records origin issue key → created work-item id. The embedded-link finalization pass reads this to turn validated references into ADO work-item links; entries missing at first pass are resolved after their target is migrated.

## Run-summary additions (FR-019)
The end-of-run log MUST report, per enabled capability, counts migrated and skipped-with-reason — e.g. `embedded-links: 142 rewritten, 7 left as text (out-of-scope)`, `remote-links: 33 added`, `branch-links: 12 added, 2 skipped (repo unmapped)`.
