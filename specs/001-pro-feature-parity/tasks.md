# Tasks: PRO Feature Parity for the Jira → Azure DevOps Migrator

**Input**: Design documents from `/specs/001-pro-feature-parity/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: INCLUDED — the spec's success criteria require them (SC-007 no-regression, SC-008 acceptance tests) and the codebase uses NUnit per component.

**Organization**: Grouped by user story (US1–US8) in priority order so each is independently implementable and testable.

## Path Conventions

All paths are under the vendored .NET solution root:
`jira-azuredevops-migrator/src/WorkItemMigrator/` (abbreviated below as `…/`).
Tests live in `…/tests/Migration.Jira-Export.Tests/`, `…/tests/Migration.Wi-Import.Tests/`, `…/tests/Migration.Common.Tests/`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish a clean, verifiable starting point before changing shared code.

- [X] T001 Verify baseline builds and tests are green: `dotnet build` and `dotnet test` on `…/WorkItemMigrator.sln` — DONE: .NET 10.0.300 installed locally; build 0 errors; tests 168/168 after fixing a pre-existing .NET 10 regex bug in `WitClient/WitClientUtils.cs` `CorrectImagePath` (unescaped filename in `!{...}!` pattern → `Regex.Escape`)
- [ ] T002 [P] Capture a no-regression golden baseline by exporting+importing the existing test fixtures and saving the produced work-item JSON under `…/tests/Migration.Wi-Import.Tests/Fixtures/baseline/` (used for SC-007 comparison) — PARTIAL: baseline established as 168 green tests (pre-change); literal export/import golden-JSON capture deferred to T052 (needs sample data)
- [X] T003 [P] Add a shared sample config exercising the new opt-in keys at `…/tests/Migration.Common.Tests/Fixtures/config-pro-parity.json`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared config + reporting plumbing every story builds on.

**⚠️ CRITICAL**: No user-story work begins until this phase is complete.

- [X] T004 Add global feature-toggle keys (`build-inventory`, `version-target`, `include-remote-links`, `include-branch-links`, `correct-embedded-links`, `state-date-map`) with backward-compatible defaults in `…/Migration.Common/Config/ConfigJson.cs`
- [X] T005 [P] Extend the field config with optional members (`composite-sources`, `composite-template`, `composite-separator`, `property-path`, `version-target`) in `…/Migration.Common/Config/Field.cs`
- [X] T006 [P] Add new config classes `CompositeSource` and `StateDate` in `…/Migration.Common/Config/`
- [X] T007 Add per-capability run-summary counters (migrated/skipped-with-reason, FR-019) — created reusable `CapabilitySummary` in `…/Migration.Common/CapabilitySummary.cs` (the shared primitive, unit-tested); export/import summary *wiring* lands within each feature's tasks
- [X] T008 [P] Config-parsing tests proving new keys load and legacy configs are unchanged (no-regression) in `…/tests/Migration.Common.Tests/ConfigAndSummaryTests.cs` (6 tests)

**Checkpoint**: Config + reporting foundation ready — user stories can begin.

---

## Phase 3: User Story 1 - Releases & Fix/Affects Version (Priority: P1) 🎯 MVP

**Goal**: Preserve each issue's Fix/Affects Version assignments (as tags by default, or a custom field) and capture release metadata in `release-metadata.json` + a report.

**Independent Test**: Migrate a project with ≥2 releases (one released, one not) and issues assigned via Fix/Affects Version; verify assignments are queryable in ADO and release metadata is captured (SC-001).

### Tests for User Story 1

- [X] T009 [P] [US1] Tests for `MapFixVersions`/`MapAffectsVersions` (tag mode, trim/skip-empty, multi-value) in `…/tests/Migration.Jira-Export.Tests/RevisionUtils/VersionMapperUtilsTests.cs` (5 tests)
- [ ] T010 [P] [US1] Import test asserting version tags / custom field applied to the work item in `…/tests/Migration.Wi-Import.Tests/WitClient/WitClientUtilsTests.cs`

### Implementation for User Story 1

- [X] T011 [US1] Extract `fixVersions` and `versions` arrays in `…/JiraExport/JiraItem.cs` (`ExtractFields`) — names already land as `;`-joined strings via the generic array handler; confirmed
- [X] T012 [US1] Add a `ReleaseMetadata` cache (mirroring `_sprintMetadataCache`) in `…/JiraExport/JiraItem.cs` — extracts description/start/release dates/released/archived from version objects
- [X] T013 [P] [US1] Implement `MapFixVersions`/`MapAffectsVersions` (prefixed `fix:`/`affects:` tags, multi-value, skip-empty) in `…/JiraExport/RevisionUtils/FieldMapperUtils.cs`
- [X] T014 [US1] Dispatch the new mappers in `…/JiraExport/JiraMapper.cs`
- [X] T015 [US1] Write `release-metadata.json` sidecar in `…/JiraExport/JiraCommandLine.cs` (same pattern as sprint metadata)
- [ ] T016 [US1] Add `ReleaseInfo`/`VersionTarget`/`ReleaseDates` and load `release-metadata.json` in `…/WorkItemImport/Settings.cs` and `…/WorkItemImport/ImportCommandLine.cs`
- [ ] T017 [US1] Apply version tags (default) or the configured custom field in `…/WorkItemImport/WitClient/WitClientUtils.cs`
- [ ] T018 [US1] Emit the release report + summary counts (FR-019) in `…/WorkItemImport/ImportCommandLine.cs`

**Checkpoint**: US1 fully functional and independently testable (MVP).

---

## Phase 4: User Story 2 - Sprint dates (Priority: P2)

**Goal**: Iterations carry start/finish dates from Jira sprints (largely already implemented — verify, harden, test).

**Independent Test**: Migrate a project with a dated closed sprint and an active sprint; confirm matching iteration dates; an undated sprint must not fail (SC-002).

### Tests for User Story 2

- [ ] T019 [P] [US2] Tests asserting iteration node start/finish dates set from metadata, and undated/malformed sprint dates don't abort, in `…/tests/Migration.Wi-Import.Tests/AgentTests.cs`

### Implementation for User Story 2

- [ ] T020 [US2] Verify/enable the sprint date+state cache in `…/JiraExport/JiraItem.cs` (≈ lines 530–547); gate behind config if needed
- [ ] T021 [US2] Harden iteration-node date setting in `…/WorkItemImport/Agent.cs` (`EnsureClasification`, ≈ lines 501–510): handle undated/malformed dates gracefully (Edge Cases)
- [ ] T022 [US2] Confirm `sprint-metadata.json` load path into `Settings.SprintDates` in `…/WorkItemImport/ImportCommandLine.cs`

**Checkpoint**: US1 + US2 both work independently.

---

## Phase 5: User Story 3 - Custom-state transition dates (Priority: P3)

**Goal**: Auto-infer `Activated/Resolved/ClosedDate` by state category, with per-state config overrides; warn+continue when unresolved.

**Independent Test**: Migrate issues through custom states with known timestamps; verify the date fields match Jira history; default-state behavior unchanged (SC-003, FR-009).

### Tests for User Story 3

- [ ] T023 [P] [US3] Tests for category-inferred dates, config overrides, and warn-on-unresolved in `…/tests/Migration.Wi-Import.Tests/WitClient/WitClientUtilsTests.cs`

### Implementation for User Story 3

- [ ] T024 [US3] Add `StateDateMap` to `…/WorkItemImport/Settings.cs` and load it from config in `…/WorkItemImport/ImportCommandLine.cs`
- [ ] T025 [US3] Add category-inference + override date-field logic in `…/WorkItemImport/WitClient/WitClientUtils.cs` (`EnsureFieldsOnStateChange`)
- [ ] T026 [US3] Warn + continue on unresolved state→date and record summary counts (FR-009/FR-019) in `…/WorkItemImport/WitClient/WitClientUtils.cs`

**Checkpoint**: US1–US3 independently functional.

---

## Phase 6: User Story 4 - Embedded Jira link correction (Priority: P3)

**Goal**: Build a pre-run inventory; rewrite in-text hyperlinks and validated bare issue keys to ADO work-item links; leave unresolved references as plain text.

**Independent Test**: Migrate cross-referencing issues; in-text links resolve to migrated work items; a bare key matching no in-scope issue stays plain text (SC-004, FR-011).

### Tests for User Story 4

- [ ] T027 [P] [US4] Inventory-build tests in `…/tests/Migration.Jira-Export.Tests/InventoryIndexTests.cs`
- [ ] T028 [P] [US4] Link-correction tests (hyperlink + validated bare key + plain-text fallback) in `…/tests/Migration.Wi-Import.Tests/WitClient/WitClientUtilsTests.cs`

### Implementation for User Story 4

- [ ] T029 [US4] Build the pre-run inventory (projects/issue keys/labels/versions) in `…/JiraExport/JiraProvider.cs` and write `inventory-index.json` in `…/JiraExport/JiraCommandLine.cs` (FR-020)
- [ ] T030 [US4] Load `inventory-index.json` into `Settings.Inventory` in `…/WorkItemImport/ImportCommandLine.cs` and `…/WorkItemImport/Settings.cs`
- [ ] T031 [US4] Implement `CorrectIssueLinkReferences` (detect hyperlinks + bare keys, validate against inventory) beside `CorrectComment`/`CorrectImagePath` in `…/WorkItemImport/WitClient/WitClientUtils.cs`
- [ ] T032 [US4] Add a finalization pass resolving not-yet-migrated targets via the journal in `…/WorkItemImport/Agent.cs`
- [ ] T033 [US4] Plain-text fallback + unresolved-reference counts (FR-011/FR-019) in `…/WorkItemImport/WitClient/WitClientUtils.cs`

**Checkpoint**: US1–US4 independently functional.

---

## Phase 7: User Story 5 - Remote/web links (Priority: P4)

**Goal**: Migrate Jira remote/web links as ADO `Hyperlink` relations.

**Independent Test**: Migrate issues with web links; each appears as a hyperlink on the work item, no duplicates (SC-005).

### Tests for User Story 5

- [ ] T034 [P] [US5] Tests for `CreateHyperlinkPatchOp` and de-duplicated apply in `…/tests/Migration.Wi-Import.Tests/WitClient/JsonPatchDocUtilsTests.cs`

### Implementation for User Story 5

- [ ] T035 [US5] Fetch remote links (`/issue/{key}/remotelink`) in `…/JiraExport/JiraServiceWrapper.cs` and `…/JiraExport/JiraProvider.cs`
- [ ] T036 [P] [US5] Add `JiraRemoteLink` model + `ExtractRemoteLinks` in `…/JiraExport/JiraItem.cs`
- [ ] T037 [P] [US5] Extend `WiLink` with `IsRemoteLink`/`Url`/`Title` in `…/Migration.WIContract/WiLink.cs`
- [ ] T038 [US5] Add `CreateHyperlinkPatchOp` in `…/WorkItemImport/WitClient/JsonPatchDocUtils.cs`
- [ ] T039 [US5] Apply hyperlinks (skip duplicates) in `…/WorkItemImport/Agent.cs` (`ApplyAndSaveLinks`)

**Checkpoint**: US1–US5 independently functional.

---

## Phase 8: User Story 6 - Branch development links (Priority: P4)

**Goal**: Migrate branch dev-links (alongside existing commit links) when repositories are mapped.

**Independent Test**: With a repository mapping, migrate issues referencing branches; branch links appear on the work items; unmapped repos skipped with a warning (SC-005, FR-013).

### Tests for User Story 6

- [ ] T040 [P] [US6] Tests for the Branch artifact-link patch (`vstfs:///Git/Ref/...`) and unmapped-repo skip in `…/tests/Migration.Wi-Import.Tests/WitClient/JsonPatchDocUtilsTests.cs`

### Implementation for User Story 6

- [ ] T041 [US6] Add `Branch` to `DevelopmentLinkType` in `…/JiraExport/JiraDevelopmentLink.cs`
- [ ] T042 [US6] Fetch branches (parallel to commit fetch) in `…/JiraExport/JiraItem.cs`
- [ ] T043 [US6] Add the `Branch` case (Git ref artifact URI) in `…/WorkItemImport/WitClient/JsonPatchDocUtils.cs` (`CreateJsonArtifactLinkPatchOp`)

**Checkpoint**: US1–US6 independently functional.

---

## Phase 9: User Story 7 - Composite field mapper (Priority: P5)

**Goal**: Join multiple Jira fields into one ADO field with a configurable separator, skipping empties.

**Independent Test**: Configure a 2-source composite mapping; verify combined output and that an empty source yields no stray separator (SC-006, FR-014).

### Tests for User Story 7

- [ ] T044 [P] [US7] Tests for `MapComposite` (ordered join, skip empties, optional template) in `…/tests/Migration.Jira-Export.Tests/RevisionUtils/FieldMapperUtilsTests.cs`

### Implementation for User Story 7

- [ ] T045 [US7] Implement `MapComposite` in `…/JiraExport/RevisionUtils/FieldMapperUtils.cs`
- [ ] T046 [US7] Dispatch `MapComposite` in `…/JiraExport/JiraMapper.cs`

**Checkpoint**: US1–US7 independently functional. (Config support from T005/T006.)

---

## Phase 10: User Story 8 - Object/array property selection (Priority: P5)

**Goal**: Map a named property of an object/array-typed Jira field; unmatched path → empty, no error.

**Independent Test**: Configure a `property-path` mapping; verify the selected property lands in the target and an unmatched path is treated as empty (SC-006, FR-015).

### Tests for User Story 8

- [ ] T047 [P] [US8] Tests for `ExtractPropertyValue` / `SelectToken` (matched + unmatched→empty) in `…/tests/Migration.Jira-Export.Tests/RevisionUtils/FieldMapperUtilsTests.cs`

### Implementation for User Story 8

- [ ] T048 [US8] Implement `ExtractPropertyValue` using `JToken.SelectToken` in `…/JiraExport/RevisionUtils/FieldMapperUtils.cs`
- [ ] T049 [US8] Wire `property-path` into extraction/`IfChanged` in `…/JiraExport/JiraMapper.cs` (and `JiraItem.ExtractFields` if needed)

**Checkpoint**: All 8 user stories independently functional.

---

## Phase 11: Polish & Cross-Cutting Concerns

- [ ] T050 [P] Document new config keys, mappers, and sidecar files in `jira-azuredevops-migrator/docs/config.md` and `jira-azuredevops-migrator/docs/faq.md`
- [ ] T051 [P] Add opt-in examples to `jira-azuredevops-migrator/docs/Samples/config-*.json`
- [ ] T052 No-regression validation: run with all new toggles OFF and diff produced JSON against the T002 baseline (SC-007)
- [ ] T053 Run `quickstart.md` acceptance smoke checks across SC-001…SC-005, SC-008
- [ ] T054 [P] Update `readme.md` to note the now-supported parity features

---

## Dependencies & Execution Order

### Phase Dependencies
- **Setup (P1)**: no dependencies.
- **Foundational (P2)**: depends on Setup; **blocks all user stories** (config + summary plumbing).
- **User Stories (P3–P10)**: each depends only on Foundational; otherwise independent and can run in parallel.
- **Polish (P11)**: depends on the targeted stories being complete.

### User Story Dependencies
- US1–US8 are mutually independent (different mappers/handlers/files). US4 additionally requires the pre-run inventory built within its own phase (T029).
- Shared files create ordering *within* a story (e.g. US1 T011→T012 both edit `JiraItem.cs`; T013→T014 dispatch).

### Parallel Opportunities
- Setup: T002, T003 in parallel.
- Foundational: T005, T006, T008 in parallel (T004 then T007 touch shared files).
- Cross-story: with capacity, US1–US8 can be developed concurrently after Phase 2 (they touch mostly disjoint methods; coordinate on `JiraMapper.cs` dispatch and `WitClientUtils.cs`).
- Within a story: all `[P]` test tasks run together first.

---

## Parallel Example: User Story 1

```bash
# Tests first (parallel):
Task: "T009 FieldMapperUtils tests for MapFixVersions/MapAffectsVersions"
Task: "T010 Import test asserting version tags/custom field applied"

# Then the independent mapper file while extraction lands in JiraItem:
Task: "T013 Implement MapFixVersions/MapAffectsVersions in FieldMapperUtils.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 only)
1. Phase 1 Setup → 2. Phase 2 Foundational → 3. Phase 3 US1 → 4. **STOP & VALIDATE** (SC-001) → 5. Demo.

### Incremental Delivery
Foundation → US1 (MVP) → US2 → US3 → US4 → US5 → US6 → US7 → US8, validating each independently and keeping all toggles opt-in so earlier work never regresses.

### Parallel Team Strategy
After Phase 2: assign P1/P2/P3 stories to different developers; serialize edits to the two shared hotspots (`JiraMapper.cs` mapper dispatch, `WitClientUtils.cs`).

---

## Notes
- `[P]` = different files, no incomplete dependencies.
- `[USx]` maps each task to its spec user story for traceability.
- Verify each story against its Independent Test before moving on.
- Keep every capability opt-in (FR-016/FR-017); run T052 no-regression before merging.
- Commit after each task or logical group.

## Summary
- **Total tasks**: 54 (T001–T054)
- **Per story**: US1=10, US2=4, US3=4, US4=7, US5=6, US6=4, US7=3, US8=3; Setup=3, Foundational=5, Polish=5
- **Tests**: included per story (NUnit) + no-regression + quickstart validation
- **MVP**: Phases 1–3 (Setup + Foundational + US1)
