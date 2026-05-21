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
- [X] T010 [P] [US1] Import test asserting the release report renders correctly in `…/tests/Migration.Wi-Import.Tests/ReleaseReportTests.cs` (3 tests). (Tag *application* itself reuses existing System.Tags handling, already covered.)

### Implementation for User Story 1

- [X] T011 [US1] Extract `fixVersions` and `versions` arrays in `…/JiraExport/JiraItem.cs` (`ExtractFields`) — names already land as `;`-joined strings via the generic array handler; confirmed
- [X] T012 [US1] Add a `ReleaseMetadata` cache (mirroring `_sprintMetadataCache`) in `…/JiraExport/JiraItem.cs` — extracts description/start/release dates/released/archived from version objects
- [X] T013 [P] [US1] Implement `MapFixVersions`/`MapAffectsVersions` (prefixed `fix:`/`affects:` tags, multi-value, skip-empty) in `…/JiraExport/RevisionUtils/FieldMapperUtils.cs`
- [X] T014 [US1] Dispatch the new mappers in `…/JiraExport/JiraMapper.cs`
- [X] T015 [US1] Write `release-metadata.json` sidecar in `…/JiraExport/JiraCommandLine.cs` (same pattern as sprint metadata)
- [X] T016 [US1] Add `ReleaseInfo`/`ReleaseDates` to `…/WorkItemImport/Settings.cs` and load `release-metadata.json` in `…/WorkItemImport/ImportCommandLine.cs`
- [X] T017 [US1] Apply version tags — DEFAULT (tags) works end-to-end via the existing `System.Tags` field application (mapper emits to `System.Tags`); the optional `version-target: field` (custom-field, unprefixed) refinement is deferred
- [X] T018 [US1] Emit the release report in `…/WorkItemImport/ReleaseReport.cs`, logged from `…/WorkItemImport/ImportCommandLine.cs` (per-tag association counts deferred to the import-loop wiring)

**Checkpoint**: US1 fully functional and independently testable (MVP).

---

## Phase 4: User Story 2 - Sprint dates (Priority: P2)

**Goal**: Iterations carry start/finish dates from Jira sprints (largely already implemented — verify, harden, test).

**Independent Test**: Migrate a project with a dated closed sprint and an active sprint; confirm matching iteration dates; an undated sprint must not fail (SC-002).

### Tests for User Story 2

- [X] T019 [P] [US2] Tests for iteration-date attributes (both/start-only/undated/null) in `…/tests/Migration.Wi-Import.Tests/IterationDatesTests.cs` (4 tests)

### Implementation for User Story 2

- [X] T020 [US2] Verified the sprint date+state cache in `…/JiraExport/JiraItem.cs` is already populated unconditionally during export; no config gate needed (sidecar only written when non-empty)
- [X] T021 [US2] Hardened iteration-node date setting in `…/WorkItemImport/Agent.cs` — extracted to `IterationDates.Build` (returns null for undated → dateless iteration, no failure); malformed dates still caught by the existing try/catch
- [X] T022 [US2] Confirmed `sprint-metadata.json` loads into `Settings.SprintDates` in `…/WorkItemImport/ImportCommandLine.cs`

**Checkpoint**: US1 + US2 both work independently.

---

## Phase 5: User Story 3 - Custom-state transition dates (Priority: P3)

**Goal**: Auto-infer `Activated/Resolved/ClosedDate` by state category, with per-state config overrides; warn+continue when unresolved.

**Independent Test**: Migrate issues through custom states with known timestamps; verify the date fields match Jira history; default-state behavior unchanged (SC-003, FR-009).

### Tests for User Story 3

- [X] T023 [P] [US3] Tests for state→date overrides (match, case-insensitive, unmatched, warn-on-missing-field, no-overwrite) in `…/tests/Migration.Wi-Import.Tests/StateTransitionDatesTests.cs` (5 tests); category-inferred dates already covered by existing WitClientUtils tests

### Implementation for User Story 3

- [X] T024 [US3] Added `StateDateMap` to `…/WorkItemImport/Settings.cs` and load it from config in `…/WorkItemImport/ImportCommandLine.cs`
- [X] T025 [US3] Category-inference already exists (`WitClientUtils` Correct*Date handlers); added override logic in new `…/WorkItemImport/StateTransitionDates.cs`, wired into `Agent.ImportRevision` (which has `Settings`, unlike `EnsureFieldsOnStateChange`)
- [X] T026 [US3] Warn + continue on invalid override (mapped state with empty date-field) — warnings surfaced from `StateTransitionDates.Apply` and logged in `Agent.ImportRevision` (FR-009)

**Checkpoint**: US1–US3 independently functional.

---

## Phase 6: User Story 4 - Embedded Jira link correction (Priority: P3)

**Goal**: Build a pre-run inventory; rewrite in-text hyperlinks and validated bare issue keys to ADO work-item links; leave unresolved references as plain text.

**Independent Test**: Migrate cross-referencing issues; in-text links resolve to migrated work items; a bare key matching no in-scope issue stays plain text (SC-004, FR-011).

### Tests for User Story 4

- [X] T027 [P] [US4] Inventory model tests in `…/tests/Migration.Common.Tests/InventoryIndexTests.cs` (4 tests)
- [X] T028 [P] [US4] Link-correction tests (browse URL + validated bare key + out-of-scope no-op + unresolved + mixed) in `…/tests/Migration.Wi-Import.Tests/EmbeddedLinkCorrectorTests.cs` (6 tests)

### Implementation for User Story 4

- [X] T029 [US4] Build the pre-run inventory (issue keys + derived projects) from the exported items and write `inventory-index.json` in `…/JiraExport/JiraCommandLine.cs`, gated on `build-inventory`/`correct-embedded-links` (FR-020). NOTE: built from the exported set (post-mapping, pre-import) rather than a separate `JiraProvider` pass — simpler and captures the same in-scope keys
- [X] T030 [US4] Added `Settings.Inventory` and load `inventory-index.json` in `…/WorkItemImport/ImportCommandLine.cs`
- [X] T031 [US4] Rewriter (`EmbeddedLinkCorrector`) now invoked live during import via new `WitClientUtils.CorrectEmbeddedIssueLinks` (rewrites Description/ReproSteps/History/AcceptanceCriteria), wired into `Agent.ImportRevision` before `SaveWorkItemFields`, gated on `correct-embedded-links` + inventory; resolves ids via `Journal.GetMigratedId` (+4 tests)
- [ ] T032 [US4] DEFERRED: dedicated finalization pass for forward references (issues migrated *after* their referrer). Backward references already resolve in-loop (time-ordered import); only forward refs remain as plain text
- [X] T033 [US4] Plain-text fallback + rewritten/unresolved counts implemented in `EmbeddedLinkCorrector.Rewrite` (FR-011); end-of-run aggregation lands with the live wiring

**Checkpoint**: US1–US4 independently functional.

---

## Phase 7: User Story 5 - Remote/web links (Priority: P4)

**Goal**: Migrate Jira remote/web links as ADO `Hyperlink` relations.

**Independent Test**: Migrate issues with web links; each appears as a hyperlink on the work item, no duplicates (SC-005).

### Tests for User Story 5

- [X] T034 [P] [US5] Tests for `CreateHyperlinkPatchOp` (correct op + throws on null url) in `…/tests/Migration.Wi-Import.Tests/WitClient/JsonPatchDocUtilsTests.cs` (2 tests)

### Implementation for User Story 5

- [ ] T035 [US5] DEFERRED: fetch remote links (`/issue/{key}/remotelink`) in `…/JiraExport/JiraServiceWrapper.cs`/`JiraProvider.cs` — Jira REST call, not verifiable offline; needs a live instance
- [ ] T036 [P] [US5] DEFERRED: `JiraRemoteLink` model + `ExtractRemoteLinks` in `…/JiraExport/JiraItem.cs` (depends on T035)
- [ ] T037 [P] [US5] DEFERRED: extend `WiLink` with `IsRemoteLink`/`Url`/`Title` (inert until export/apply exist)
- [X] T038 [US5] Added `CreateHyperlinkPatchOp` in `…/WorkItemImport/WitClient/JsonPatchDocUtils.cs` — import is now hyperlink-capable
- [ ] T039 [US5] DEFERRED: apply hyperlinks in `…/WorkItemImport/Agent.cs` (`ApplyAndSaveLinks`) (depends on T035/T036)

**Checkpoint**: US1–US5 independently functional.

---

## Phase 8: User Story 6 - Branch development links (Priority: P4)

**Goal**: Migrate branch dev-links (alongside existing commit links) when repositories are mapped.

**Independent Test**: With a repository mapping, migrate issues referencing branches; branch links appear on the work items; unmapped repos skipped with a warning (SC-005, FR-013).

### Tests for User Story 6

- [X] T040 [P] [US6] Test for the Branch artifact-link patch (`vstfs:///Git/Ref/...GB<branch>`) in `…/tests/Migration.Wi-Import.Tests/WitClient/JsonPatchDocUtilsTests.cs` (1 test)

### Implementation for User Story 6

- [X] T041 [US6] `Branch` already present in `DevelopmentLinkType` (`…/JiraExport/JiraDevelopmentLink.cs`) — verified
- [ ] T042 [US6] DEFERRED: fetch branches (parallel to commit fetch) in `…/JiraExport/JiraItem.cs` — Jira dev-status REST call, not verifiable offline
- [X] T043 [US6] Added the `Branch` case (Git ref artifact URI) in `…/WorkItemImport/WitClient/JsonPatchDocUtils.cs` — import is now branch-capable

**Checkpoint**: US1–US6 independently functional.

---

## Phase 9: User Story 7 - Composite field mapper (Priority: P5)

**Goal**: Join multiple Jira fields into one ADO field with a configurable separator, skipping empties.

**Independent Test**: Configure a 2-source composite mapping; verify combined output and that an empty source yields no stray separator (SC-006, FR-014).

### Tests for User Story 7

- [X] T044 [P] [US7] Tests for `MapComposite` (ordered join, skip empties, optional template, no-sources cases) in `…/tests/Migration.Jira-Export.Tests/RevisionUtils/CompositeMapperTests.cs` (5 tests)

### Implementation for User Story 7

- [X] T045 [US7] Implemented `MapComposite` in `…/JiraExport/RevisionUtils/FieldMapperUtils.cs` (separator join skipping empties, or `composite-template` format)
- [X] T046 [US7] Dispatched `MapComposite` in `…/JiraExport/JiraMapper.cs`

**Checkpoint**: US1–US7 independently functional. (Config support from T005/T006.)

---

## Phase 10: User Story 8 - Object/array property selection (Priority: P5)

**Goal**: Map a named property of an object/array-typed Jira field; unmatched path → empty, no error.

**Independent Test**: Configure a `property-path` mapping; verify the selected property lands in the target and an unmatched path is treated as empty (SC-006, FR-015).

### Tests for User Story 8

- [X] T047 [P] [US8] Tests for `ExtractPropertyValue` (object/array select, primitive type preserved, unmatched→null, null token, malformed path) in `…/tests/Migration.Jira-Export.Tests/RevisionUtils/PropertyPathTests.cs` (6 tests)

### Implementation for User Story 8

- [X] T048 [US8] Implemented `ExtractPropertyValue` using `JToken.SelectToken` in `…/JiraExport/RevisionUtils/FieldMapperUtils.cs`
- [X] T049 [US8] Wired `property-path` as a top-precedence branch in `…/JiraExport/JiraMapper.cs` that reads the raw field token from `JiraItem.RemoteIssue` (no `ExtractFields` change needed)

**Checkpoint**: All 8 user stories independently functional.

---

## Phase 11: Polish & Cross-Cutting Concerns

- [X] T050 [P] Documented new config keys, mappers, and sidecar files in new `jira-azuredevops-migrator/docs/pro-feature-parity.md`
- [X] T051 [P] Added opt-in example `jira-azuredevops-migrator/docs/Samples/config-pro-parity.json`
- [X] T052 No-regression: validated by the test suite — all original 168 tests still pass and `Legacy_config_without_new_keys_uses_safe_defaults` proves untouched behavior. (A full live export/import golden diff still needs sample data + live services.)
- [ ] T053 DEFERRED: `quickstart.md` acceptance smoke checks (SC-001…SC-008) require live Jira + Azure DevOps instances
- [X] T054 [P] Updated `readme.md` with a "Community parity additions" section linking to the parity doc

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
