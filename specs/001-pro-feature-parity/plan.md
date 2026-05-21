# Implementation Plan: PRO Feature Parity for the Jira → Azure DevOps Migrator

**Branch**: `001-pro-feature-parity` | **Date**: 2026-05-21 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-pro-feature-parity/spec.md`

## Summary

Bring the free Community edition of the .NET `jira-azuredevops-migrator` to parity with the PRO edition by adding eight capabilities (Releases & Fix/Affects Version, sprint dates, custom-state transition dates, embedded-link correction, remote/web links, branch dev-links, composite field mapper, object/array property selection). The approach extends the existing two-phase pipeline in place — `jira-export` (Jira → intermediate JSON) and `wi-import` (JSON → Azure DevOps) — reusing established patterns: named field mappers, sidecar metadata files (like `sprint-metadata.json`), the resumable journal, and JSON-Patch work-item updates. All new behavior is opt-in via the config file and defaults to current behavior (no regression).

## Technical Context

**Language/Version**: C# on .NET 10 (`net10.0`); existing source also retains net6-era patterns
**Primary Dependencies**: Newtonsoft.Json 13.x; Microsoft.Extensions.CommandLineUtils; Microsoft Azure DevOps / TFS Work Item Tracking client (`Microsoft.TeamFoundation.WorkItemTracking.WebApi`); Atlassian Jira REST API v2/v3
**Storage**: Filesystem only — intermediate JSON per work item, attachments, the journal (`ItemsJournal.txt`), and JSON sidecar metadata files in the workspace. No database.
**Testing**: NUnit (with AutoFixture + NSubstitute), per existing `tests/Migration.*.Tests` projects
**Target Platform**: Windows x64 (official) + cross-platform via .NET; targets Azure DevOps Services & Server/TFS 2018+, Jira Cloud & Server 7–9
**Project Type**: Multi-project .NET solution — two CLI executables (`jira-export`, `wi-import`) plus shared libraries
**Performance Goals**: Handle enterprise instances (10k+ issues, many projects) via batched export and resumable import; the new pre-run inventory index (FR-020) must scale to large instances without exhausting memory
**Constraints**: No regression when features are disabled (FR-017); opt-in via config (FR-016); must respect journal-based resumability (FR-018); intermediate-JSON contract changes must stay backward-compatible
**Scale/Scope**: 8 capabilities across ~12 existing files + a handful of new classes; no new projects

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The project constitution (`.specify/memory/constitution.md`) is the **unpopulated Spec Kit template** — no principles have been ratified. There are therefore no formal constitutional gates to enforce. In their absence, this plan self-imposes the spec's own non-negotiables as gates:

- **No-regression gate**: with all features disabled, output is equivalent to today's Community edition (FR-017, SC-007). ✅ Satisfied by opt-in design.
- **Opt-in/config gate**: every capability is controllable from the config file (FR-016). ✅ Satisfied.
- **Resumability gate**: new data participates in journal-based resume (FR-018). ✅ Designed in (Phase 1).
- **Test gate**: each capability ships with NUnit tests in the existing test projects. ✅ Planned.

**Result**: PASS (no violations; Complexity Tracking not required).

*Post-Phase 1 re-check*: PASS — design adds no new projects, introduces no parallel data stores, and reuses existing extension points (mapper dispatch, sidecar files, JSON-Patch utils). See Phase 1 artifacts.

## Project Structure

### Documentation (this feature)

```text
specs/001-pro-feature-parity/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (config schema + sidecar file formats)
│   ├── config-schema.md
│   └── sidecar-files.md
├── checklists/
│   └── requirements.md  # from /speckit.specify
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created here)
```

### Source Code (repository root)

This feature extends the existing vendored solution in place. Real directories that change:

```text
jira-azuredevops-migrator/src/WorkItemMigrator/
├── JiraExport/                      # jira-export CLI (Jira → JSON)
│   ├── JiraProvider.cs              # + fetch remote links; drive pre-run inventory
│   ├── JiraItem.cs                  # + extract fixVersions/versions, branches, remote links; release-metadata cache
│   ├── JiraMapper.cs                # + dispatch MapFixVersions / MapComposite; property-path extraction
│   ├── JiraCommandLine.cs           # + write release-metadata.json + inventory-index.json sidecars
│   ├── JiraDevelopmentLink.cs       # + Branch enum member
│   ├── JiraLink.cs / new JiraRemoteLink
│   └── RevisionUtils/FieldMapperUtils.cs   # + MapFixVersions, MapComposite, ExtractPropertyValue, link-correction helpers
├── WorkItemImport/                  # wi-import CLI (JSON → ADO)
│   ├── ImportCommandLine.cs         # + load release-metadata.json & inventory-index.json
│   ├── Settings.cs                  # + ReleaseDates, version-mapping mode, state-date overrides
│   ├── Agent.cs                     # + apply hyperlinks; finalization pass for embedded-link rewrite
│   └── WitClient/
│       ├── WitClientUtils.cs        # + custom-state date handlers; CorrectIssueLinkReferences
│       └── JsonPatchDocUtils.cs     # + CreateHyperlinkPatchOp; Branch artifact-link case
├── Migration.WIContract/            # shared intermediate-JSON models
│   ├── WiLink.cs                    # + hyperlink/remote fields (or new WiRemoteLink)
│   └── WiRevision.cs / WiField.cs   # (version values flow through existing fields)
├── Migration.Common/Config/         # config schema
│   ├── Field.cs                     # + composite-sources, composite-template, property-path, version-target
│   ├── CompositeSource.cs           # NEW
│   └── ConfigJson.cs                # + feature toggles (version-target, build-inventory, etc.)
└── tests/                           # NUnit
    ├── Migration.Jira-Export.Tests/ # mapper/extraction/inventory tests
    ├── Migration.Wi-Import.Tests/   # state-date, hyperlink, link-correction tests
    └── Migration.Common.Tests/      # config + JSON-path tests
```

**Structure Decision**: Extend the existing solution in place. No new projects are introduced; new classes live beside the components they extend, and tests go in the matching existing test project. This preserves the build, the no-regression guarantee, and the established two-phase architecture.

## Implementation phasing (by spec priority)

The eight capabilities map to the spec's prioritized user stories and are independently shippable. Recommended order:

1. **P1 — Releases & Fix/Affects Version** (US1): version extraction on export, `release-metadata.json` sidecar, tag/custom-field application on import. Default representation = tags + report (see research.md).
2. **P2 — Sprint dates** (US2): mostly verify/enable existing infra (`JiraItem` cache + `Agent` node dates); add tests and config toggle.
3. **P3 — Custom-state transition dates** (US3): new state→date handlers in `WitClientUtils.EnsureFieldsOnStateChange`, with config overrides.
4. **P3 — Embedded-link correction** (US4): pre-run inventory (FR-020) + import-time/finalization rewrite using the journal.
5. **P4 — Remote/web links** (US5): export fetch + `Hyperlink` relation patch op.
6. **P4 — Branch dev-links** (US6): `Branch` enum + artifact-link URI case.
7. **P5 — Composite mapper** (US7): `Field` config + `MapComposite` dispatch.
8. **P5 — Object/array property selection** (US8): `property-path` config + `SelectToken` extraction.

Detailed touch-points per capability are in [research.md](./research.md); data shapes in [data-model.md](./data-model.md); config/sidecar contracts in [contracts/](./contracts/).

## Complexity Tracking

No constitutional violations; no new projects or patterns requiring justification. Section intentionally empty.
