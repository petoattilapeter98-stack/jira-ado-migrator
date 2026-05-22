# Features Added — PRO Feature Parity

This fork extends the open-source **Community** edition of the Jira → Azure DevOps work-item
migrator with the capabilities that previously required the commercial **PRO** edition. Every
addition is **opt-in via the config file** and defaults to current behavior (no regression).

Full reference: [`jira-azuredevops-migrator/docs/pro-feature-parity.md`](jira-azuredevops-migrator/docs/pro-feature-parity.md).
Spec & plan: [`specs/001-pro-feature-parity/`](specs/001-pro-feature-parity/).

## The 8 capabilities

| # | Feature | What it does | Key config |
|---|---|---|---|
| US1 | **Releases & Fix/Affects Version** | Fix-version → ADO tags (`fix:2.0.0`); release dates/status/description captured to `release-metadata.json` + a release report | `MapFixVersions` / `MapAffectsVersions`, `version-target` |
| US2 | **Sprint dates** | Stamps start/finish dates on ADO iteration nodes (not just sprint names) | `MapSprint` (+ `sprint-metadata.json`) |
| US3 | **Custom-state transition dates** | Preserves Activated/Resolved/Closed dates for custom workflow states (auto-infer by category + per-state overrides) | `state-date-map` |
| US4 | **Embedded-link correction** | Rewrites in-text Jira references (hyperlinks **and** validated bare keys) to migrated ADO work items; includes a forward-reference finalization pass | `correct-embedded-links`, `build-inventory` |
| US5 | **Remote/web links** | Migrates Jira remote links as ADO work-item hyperlinks | `include-remote-links` |
| US6 | **Branch dev-links** | Migrates branch (alongside commit) development links | `include-branch-links` |
| US7 | **Composite field mapper** | Consolidates multiple Jira fields into one ADO field | `MapComposite` (`composite-sources`/`composite-template`) |
| US8 | **Object/array property selection** | Maps a chosen property of complex fields via JSONPath | `property-path` |

## Supporting / cross-cutting

- **Pre-run inventory index** (`inventory-index.json`) — catalogs in-scope projects, issue keys,
  labels, and versions; powers embedded-link validation.
- **Per-capability run summary** — counts migrated / skipped (with reasons).
- **Bulk ADO user-provisioning script** (`scripts/provision-ado-users.sh`) — provisions org users
  from a `users.txt` mapping before import.
- **No-regression by design** — all toggles default off; an existing config behaves exactly as before.
- Built on **.NET 10**, NUnit-tested, delivered via the Spec Kit workflow (spec → plan → tasks → implement).

## Status

All 8 features are implemented, tested, and merged to `main`.

**Live-validated end-to-end** (real Jira project `DD` → an ADO Agile project): US1, US3, US4, and
user-mapping — work items landed with correct types, states, fix-version tags, transition dates,
rewritten cross-links, and assignees.
