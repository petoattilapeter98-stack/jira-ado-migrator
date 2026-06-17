# Developer Guide

This guide is for engineers working on the **jiratoazure** project — a fork of the open-source
[Solidify/jira-azuredevops-migrator](https://github.com/solidify/jira-azuredevops-migrator)
(Community edition) extended toward **PRO feature parity**. It covers the architecture, how to
build and test, how to run a migration end-to-end, and where each added feature lives in the code.

For *what* the added features do, see [`FEATURES.md`](FEATURES.md) and
[`jira-azuredevops-migrator/docs/pro-feature-parity.md`](jira-azuredevops-migrator/docs/pro-feature-parity.md).
For the spec-driven workflow that produced them, see [`specs/`](specs/).

---

## 1. Architecture at a glance

The tool is a **two-stage pipeline of two separate executables**. Stage 1 reads Jira and writes
files to disk; stage 2 reads those files and writes work items to Azure DevOps / TFS. The disk
boundary is intentional — you can inspect and validate an exported batch before anything is written
to ADO, and you can migrate in batches.

```
                 jira-export                         wi-import
   ┌────────┐   (JiraExport)    ┌──────────────┐   (WorkItemImport)   ┌──────────────┐
   │  Jira  │ ────────────────> │  files on    │ ──────────────────>  │ Azure DevOps │
   │  REST  │   issues + meta   │  disk        │   work items + links │   / TFS      │
   └────────┘                   │  + journal   │                      └──────────────┘
                                └──────────────┘
                                   ^ inspect / validate here
```

**Stage 1 — `jira-export`** queries Jira (JQL), and for each issue writes:
- one `<KEY>.json` migration item per work item (all revisions/history),
- attachments under `Attachments/`,
- a journal (`itemsJournal.txt`) tracking progress so runs are resumable,
- PRO-parity side-car metadata (`inventory-index.json`, `release-metadata.json`,
  `sprint-metadata.json`).

**Stage 2 — `wi-import`** reads those files, builds an execution plan, and creates/updates ADO work
items, links, and attachments — applying the field mappings and PRO-parity passes from `config.json`.

---

## 2. Repository layout

```text
jiratoazure/
├── README.md                       # Project overview & provenance
├── FEATURES.md                     # The 8 PRO-parity capabilities (what + key config)
├── DEVELOPERS.md                   # ← this file
├── CLAUDE.md                       # Agent/dev guidelines (tech stack, conventions)
├── specs/001-pro-feature-parity/   # Spec Kit feature spec, plan, tasks
├── .specify/                       # Spec Kit config, templates, extensions
├── scripts/
│   └── provision-ado-users.sh      # Bulk ADO user provisioning from users.txt
├── migration-poc/                  # GITIGNORED scratch: real exported data + logs (see §7)
└── jira-azuredevops-migrator/      # The vendored .NET tool — where the code lives
    ├── config.json                 # Sample/working migration config
    ├── readme.md                   # Upstream readme
    ├── docs/                        # Upstream + pro-feature-parity docs
    └── src/WorkItemMigrator/
        └── WorkItemMigrator.sln     # ← build this
```

### Solution projects (`src/WorkItemMigrator/WorkItemMigrator.sln`)

| Project | Output | Responsibility |
|---|---|---|
| **JiraExport** | `jira-export.exe` | Stage 1 CLI. Talks to Jira REST v2/v3, maps issues → migration items. |
| **WorkItemImport** | `wi-import.exe` | Stage 2 CLI. Talks to ADO Work Item Tracking, applies the execution plan. |
| **Migration.Common** | library | Shared core: config model, field mapping, user mapping, inventory, context. |
| **Migration.Common.Log** | library | Logging abstraction (file + Application Insights). |
| **Migration.WIContract** | library | The on-disk work-item contract (the JSON schema the two stages share). |
| `tests/*` | NUnit | One test project per library/CLI (see §4). |

---

## 3. Prerequisites & building

- **.NET 10 SDK** (`net10.0` — confirm with `dotnet --list-sdks`). Note the legacy
  `azure-pipelines.yml` still pins `6.0.x`; local development targets net10.
- The codebase still carries some net6-era patterns and **Newtonsoft.Json 13.x**; match the
  surrounding style rather than modernizing opportunistically.

```bash
cd jira-azuredevops-migrator/src/WorkItemMigrator
dotnet restore WorkItemMigrator.sln
dotnet build   WorkItemMigrator.sln -c Release
```

The two CLI executables land under each project's `bin/Release/net10.0/`.

---

## 4. Testing

NUnit, one project per area. Run the whole suite from the solution folder:

```bash
cd jira-azuredevops-migrator/src/WorkItemMigrator
dotnet test WorkItemMigrator.sln
```

| Test project | Covers |
|---|---|
| `Migration.Jira-Export.Tests` | Jira mapping, revisions, links, export-side logic |
| `Migration.Wi-Import.Tests` | Import agent, execution plan, embedded-link/state-date passes |
| `Migration.Common.Tests` | Config, field/user mapping, inventory index |
| `Migration.WIContract.Tests` | The shared on-disk contract |

Run a single project, or filter by name:

```bash
dotnet test tests/Migration.Wi-Import.Tests/Migration.Wi-Import.Tests.csproj
dotnet test WorkItemMigrator.sln --filter "FullyQualifiedName~EmbeddedLink"
```

Every PRO-parity addition ships with tests — keep that invariant when extending.

---

## 5. Running a migration end-to-end

All behavior is driven by a **`config.json`** (see `jira-azuredevops-migrator/config.json` for a
working sample, and `docs/config.md` for the full key reference).

```bash
# Stage 1 — export Jira issues to ./export (or wherever config points)
jira-export --config config.json --user <jira-user> --password <api-token> --force

# inspect the exported <KEY>.json files, inventory-index.json, etc.

# Stage 2 — import the exported files into Azure DevOps
wi-import   --config config.json --token <ado-pat> --force
```

`--force` re-runs items the journal already marked done. Logs are written per run as
`jira-export-log-*.txt` / `wi-import-log-*.txt`.

**User provisioning:** before import, ADO accounts referenced by the user map must exist.
`scripts/provision-ado-users.sh` bulk-provisions them from a `users.txt` mapping.

> ⚠️ **Never commit credentials.** Jira tokens, ADO PATs, and `users.txt` live under `.secrets/`
> or `migration-poc/`, both gitignored. The root `.gitignore` also blocks `*.secret*` and
> `*.local.env`.

---

## 6. Where the PRO-parity features live

Each capability is **opt-in via config** and defaults to current behavior (no regression). Map of
feature → primary code → config toggle (full detail in [`FEATURES.md`](FEATURES.md)):

| # | Feature | Primary code | Config key(s) |
|---|---|---|---|
| US1 | Releases & Fix/Affects Version | `WorkItemImport/ReleaseReport.cs`, `JiraExport/JiraMapper.cs` | `MapFixVersions`, `MapAffectsVersions`, `version-target` |
| US2 | Sprint dates | `WorkItemImport/IterationDates.cs` | `MapSprint` |
| US3 | Custom-state transition dates | `WorkItemImport/StateTransitionDates.cs` | `state-date-map` |
| US4 | Embedded-link correction | `WorkItemImport/EmbeddedLinkCorrector.cs`, `Migration.Common/InventoryIndex.cs` | `correct-embedded-links`, `build-inventory` |
| US5 | Remote/web links | `JiraExport/JiraRemoteLink.cs`, `JiraExport/JiraLink.cs` | `include-remote-links` |
| US6 | Branch dev-links | `JiraExport/JiraDevelopmentLink.cs` | `include-branch-links` |
| US7 | Composite field mapper | `Migration.Common/Config`, `BaseMapper.cs` | `MapComposite` (`composite-sources`/`composite-template`) |
| US8 | Object/array property selection | field-mapping path in `Migration.Common` | `property-path` |

Cross-cutting: `Migration.Common/CapabilitySummary.cs` (per-capability run counts) and
`Migration.Common/InventoryIndex.cs` (pre-run index powering link validation).

**Design rule for new work:** add a config toggle that defaults to the old behavior; wire it through
`Migration.Common/Config`; implement the pass in the relevant CLI project; add NUnit tests; update
`FEATURES.md` and `docs/pro-feature-parity.md`.

---

## 7. The `migration-poc/` workspace

`migration-poc/` is **gitignored scratch** holding a real end-to-end run of the Jira `DD` project →
an ADO Agile project: exported `DD-*.json` items, `Attachments/`, `inventory-index.json`,
`release-metadata.json`, the journal, `users.txt`, and per-run logs. It is the de-facto manual
integration harness for validating changes against real data.

Do **not** commit it (it contains exported customer-ish data and secrets) and do not rely on it in
automated tests — treat it as a local sandbox.

---

## 8. Spec-driven workflow (Spec Kit)

Features are developed via **[Spec Kit](https://github.com/github/spec-kit)**: `spec → plan →
tasks → implement`. The PRO-parity effort lives in `specs/001-pro-feature-parity/`. When adding a
capability, start from a spec rather than ad-hoc code so the eight-feature documentation set stays
coherent. `.specify/` holds the templates and configuration that drive the workflow.

---

## 9. Conventions

- **No regression:** every new behavior is opt-in and defaults off; an existing `config.json` must
  behave exactly as before.
- **Match the local style:** standard C# conventions; the tree mixes net10 and net6-era patterns
  plus Newtonsoft.Json — follow the file you're editing.
- **Test what you add:** NUnit coverage per capability.
- **Keep docs in sync:** `FEATURES.md` (summary) + `docs/pro-feature-parity.md` (reference).
- **Provenance:** `jira-azuredevops-migrator/` is vendored from upstream Community edition; respect
  its `LICENSE.md`.
</content>
</invoke>
