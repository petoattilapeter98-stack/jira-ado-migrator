# Phase 0 Research: PRO Feature Parity

All Technical Context unknowns and per-capability approach decisions, resolved. Format: Decision / Rationale / Alternatives.

## Cross-cutting decisions

### R1. Release/Version representation in Azure DevOps
- **Decision**: Default = per-issue Fix/Affects associations as **work-item tags** (prefixed, e.g. `fix:2.3.0` / `affects:2.2.0`) + a generated **`release-report`** for release metadata; **opt-in** to a custom field via config (`version-target`).
- **Rationale**: ADO Boards has no native version entity; tags need zero process customization and are immediately queryable; metadata (dates/status/description) has no live home, so a report artifact preserves it. Matches spec FR-003 and the clarification session.
- **Alternatives**: Custom field only (requires inherited-process customization before import); reuse iteration nodes (collides with sprints, semantically wrong). Both retained as non-default options.

### R2. Pre-run inventory/index (FR-020)
- **Decision**: Add a discovery pass in `jira-export` that catalogs all in-scope projects, issue keys, labels, and versions into an in-memory index, persisted as `inventory-index.json` in the workspace and emitted as a human summary. Built by `JiraProvider` before the per-issue export loop.
- **Rationale**: Enables bare-key validation for embedded-link correction independent of per-project run order; reuses the existing JQL/paging machinery in `JiraServiceWrapper`. Streamed/paged to bound memory on large instances.
- **Alternatives**: Resolve only within a single run (misses cross-project links); query Jira live during text correction (slow, fragile).

### R3. Embedded-link resolution = two-phase (validate vs. link)
- **Decision**: The pre-run index decides **whether** a reference (`…/browse/KEY-123` or bare `KEY-123`) is a real in-scope issue and should be rewritten. The actual ADO work-item link is supplied by the origin→work-item map in the **journal** at import time; references whose target isn't migrated yet are resolved in a **finalization pass** after import.
- **Rationale**: Work-item IDs only exist post-import; the journal already records origin→WiId. Mirrors the tool's existing `CorrectImagePath`/`CorrectComment` post-processing pattern in `WitClientUtils`.
- **Alternatives**: Rewrite at export (impossible — no WiIds yet); skip forward references (loses links).

### R4. Backward compatibility of the intermediate JSON
- **Decision**: Extend WIContract additively (new optional fields / link kinds default to absent); never change existing field names or defaults.
- **Rationale**: Preserves no-regression (FR-017) and lets old workspaces still import.

## Per-capability decisions

### R5. Releases & Fix/Affects Version (US1)
- **Decision**: Export — read `fixVersions` and `versions` arrays in `JiraItem.ExtractFields()`; cache release metadata like the existing `_sprintMetadataCache`; write `release-metadata.json` from `JiraCommandLine` (same pattern as `sprint-metadata.json`). Add `MapFixVersions` / `MapAffectsVersions` mappers (dispatched in `JiraMapper`). Import — load metadata in `ImportCommandLine`, store on `Settings`, apply tags (default) or a custom field in `WitClientUtils.UpdateWIFields`.
- **Rationale**: Reuses the proven sprint-metadata sidecar mechanism and the named-mapper dispatch. Tags via existing `System.Tags` handling.
- **Alternatives**: see R1.

### R6. Sprint dates (US2)
- **Decision**: Largely **already implemented** — `JiraItem.cs:530-547` caches `startDate`/`endDate`/`state`; `Agent.cs:501-510` sets `startDate`/`finishDate` on iteration classification nodes from `Settings.SprintDates`. Work = add a config toggle, fill gaps (e.g. completion handling, undated sprints), and add explicit tests.
- **Rationale**: Avoid rebuilding existing infrastructure; verify and harden instead.
- **Alternatives**: Reimplement (wasteful).

### R7. Custom-state transition dates (US3)
- **Decision**: Default — auto-infer `ActivatedDate`/`ResolvedDate`/`ClosedDate` from each state's category (the existing `Correct*ByAnd*Date` handlers in `WitClientUtils.EnsureFieldsOnStateChange`). Add config overrides mapping a specific state → a target date field. Warn + continue when neither category nor override applies (FR-009).
- **Rationale**: Revisions already carry per-transition timestamps (`rev.Time`); only the target-field selection logic is missing.
- **Alternatives**: Explicit-only config (too much setup); standard-only (fails custom workflows).

### R8. Embedded-link correction (US4)
- **Decision**: New `CorrectIssueLinkReferences` alongside `CorrectComment`/`CorrectImagePath` in `WitClientUtils`; detect hyperlinks + bare keys, validate against the pre-run index (R2), resolve via journal/finalization (R3), else leave plain text and count it in the run summary (FR-011/FR-019).
- **Rationale**: Reuses the existing text post-processing seam.

### R9. Remote/web links (US5)
- **Decision**: Export — fetch `/issue/{key}/remotelink` in `JiraProvider`, model as `JiraRemoteLink`, attach to revisions. Import — `JsonPatchDocUtils.CreateHyperlinkPatchOp` adding a `Hyperlink` relation (`/relations/-`), applied in `Agent.ApplyAndSaveLinks`.
- **Rationale**: ADO represents web links as `Hyperlink` relations; matches existing patch-op patterns.
- **Alternatives**: Store as text in a field (loses link semantics).

### R10. Branch development links (US6)
- **Decision**: Add `Branch` to `DevelopmentLinkType`; fetch branches in `JiraItem` like commits; on import add a `Branch` case in `JsonPatchDocUtils.CreateJsonArtifactLinkPatchOp` using the ADO Git ref artifact URI (`vstfs:///Git/Ref/...`). Reuse the existing `repository-map` and `include-development-links` gating.
- **Rationale**: Symmetric with the existing commit-link path; only the artifact-URI shape differs.

### R11. Composite field mapper (US7)
- **Decision**: Add `composite-sources` (+ optional `composite-template`) to `Field`; new `CompositeSource` config class; `MapComposite` mapper that joins source values in order with a configurable separator, **skipping empties** (no stray separators). Document that adding a dedicated 1:1 ADO field is preferred where feasible.
- **Rationale**: Matches spec FR-014 + the clarification (separator join, skip empties). Template form deferred as an optional extension.
- **Alternatives**: Template/format string as default (harder to skip empties cleanly).

### R12. Object/array property selection (US8)
- **Decision**: Add `property-path` to `Field`; extract via Newtonsoft `JToken.SelectToken(...)` over the field's JSON value (helper `ExtractPropertyValue` in `FieldMapperUtils`). Unmatched path → treated as empty, no error (FR-015).
- **Rationale**: The Jira REST values are already `JToken`s; `SelectToken` is the idiomatic Newtonsoft selector and consistent with existing `JsonExtensions`.

## Resolved Technical Context unknowns
- **.NET version**: confirmed `net10.0` from the `.csproj`/`obj` targets.
- **Test stack**: NUnit + AutoFixture + NSubstitute (existing test projects).
- **No NEEDS CLARIFICATION remain** — the single spec-level clarification (release target, FR-003) was resolved in the clarify session and refined here (R1).
