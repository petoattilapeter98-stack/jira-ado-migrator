# Feature Specification: PRO Feature Parity for the Jira → Azure DevOps Migrator

**Feature Branch**: `001-pro-feature-parity`
**Created**: 2026-05-21
**Status**: Draft
**Input**: User description: "implement missing features to match pro"

## Overview

The free Community edition of the migrator covers core work-item migration (fields, history, users, links, attachments, commit links, sprint *names*). The commercial PRO edition adds eight capabilities on top. This feature brings those eight capabilities into the freely-available tool so that a migration loses as little Jira fidelity as possible without requiring a paid license.

The eight target capabilities (the parity scope) are:

1. Migrate **Releases** and the **Fix Version** / **Affects Version** issue fields, including each release's date, start date, status, and description.
2. Migrate **Sprint dates** (start/end/completion) onto the target iterations, not just sprint names.
3. Preserve **state-transition dates** (e.g. activated, resolved, closed) for **custom** workflow states, not only the three default states.
4. Correct **embedded links to Jira issues** inside text fields (description, repro steps, comments) so they point to the migrated work item instead of the old Jira issue.
5. Migrate **remote/web links** as work-item hyperlinks.
6. Migrate **branch** development links (in addition to the commit links already supported).
7. Provide a **composite field mapper** that consolidates several Jira fields into a single target field.
8. Allow selecting **any property of object- and array-typed Jira fields** when mapping (e.g. picking a specific property of Fix Version, Affects Version, or Components).

## Clarifications

### Session 2026-05-21

- Q: For embedded-link correction, what should the tool detect and rewrite in text fields? → A: Both explicit hyperlinks and bare issue keys (e.g. `ABC-123`), but bare-key candidates are validated against an inventory of in-scope projects/issues; rewrite only when the candidate resolves to a migrated work item, otherwise leave it as plain text.
- Q: How far should the reference-resolution inventory reach and when is it built? → A: Build a comprehensive pre-run inventory (a discovery/indexing pass) cataloging all in-scope Jira projects, issue keys, labels, and versions; index it and cross-check every embedded reference against it. The index governs whether a reference is rewritten; the actual work-item link is supplied by the origin→work-item cross-reference (journal), with not-yet-migrated targets resolved in a finalization pass.
- Q: For custom workflow states, how is the target transition-date field chosen? → A: Auto-infer the standard date field (ActivatedDate/ResolvedDate/ClosedDate) from the state's category by default, and allow per-state overrides in configuration.
- Q: How does the composite mapper combine multiple Jira fields into one ADO field? → A: Join the source values in configured order with a chosen separator, skipping empty sources so no stray separators appear. (Preferred alternative where feasible: add a dedicated ADO custom field and map 1:1; the composite mapper is the fallback when that is not possible.)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Migrate Releases and Version fields (Priority: P1)

A migration engineer is moving a Jira project that uses Releases (Versions). They need each issue's **Fix Version** and **Affects Version** assignments preserved in Azure DevOps, and they need the release definitions themselves — name, description, start date, release date, and released/unreleased status — to survive the migration so the team can still see what shipped in which release.

**Why this priority**: Release/version data is the single largest gap between Community and PRO and is frequently load-bearing for release planning and audit. Without it, the team loses the ability to answer "which work items belong to release X" after the migration.

**Independent Test**: Run a migration of a Jira project containing at least two releases (one released, one unreleased) with issues assigned to each via Fix Version and Affects Version. Verify in Azure DevOps that every issue's version assignment is queryable and that each release's metadata (dates, status, description) is represented.

**Acceptance Scenarios**:

1. **Given** a Jira issue assigned to Fix Version "2.3.0", **When** the issue is migrated, **Then** the migrated work item is associated with "2.3.0" in a way that is filterable/queryable in Azure DevOps.
2. **Given** a Jira issue with both a Fix Version and an Affects Version, **When** the issue is migrated, **Then** both assignments are preserved and distinguishable from one another.
3. **Given** a Jira release with a name, description, start date, release date, and "released" status, **When** the migration runs, **Then** that release's metadata is represented in the target without manual re-entry.
4. **Given** an issue whose version assignment changed over its history, **When** the issue is migrated, **Then** the version assignment reflects the correct value per revision consistent with how other historical fields are migrated.

---

### User Story 2 - Migrate sprint dates onto iterations (Priority: P2)

A migration engineer migrates a Jira project that runs sprints. Today the sprint *names* become Azure DevOps iteration paths, but the iterations have no dates, so capacity, burndown, and "current sprint" views do not work. They need each iteration to carry the correct start and end (and completion) dates from the corresponding Jira sprint.

**Why this priority**: Sprint names already migrate; adding dates is a high-value, comparatively contained increment that makes the migrated iterations actually usable for Agile tooling in Azure DevOps.

**Independent Test**: Migrate a project with at least one closed sprint and one active sprint that have defined dates, then confirm the corresponding Azure DevOps iterations show matching start and end dates and that date-driven views behave correctly.

**Acceptance Scenarios**:

1. **Given** a Jira sprint with a start date and end date, **When** the project is migrated, **Then** the matching Azure DevOps iteration node has the same start and end dates.
2. **Given** a completed Jira sprint, **When** it is migrated, **Then** the iteration reflects the completion consistently with its dates.
3. **Given** a sprint with no dates set in Jira, **When** it is migrated, **Then** the iteration is created without dates and the migration does not fail.

---

### User Story 3 - Preserve state-transition dates for custom workflows (Priority: P3)

A migration engineer migrates a project with a customized workflow that has states beyond the three defaults. They need the dates on which work items entered key states (e.g. activated, resolved, closed) to be preserved so reporting on cycle time and lead time remains accurate.

**Why this priority**: History accuracy is a core promise of the tool; without transition dates for custom states, time-in-state reporting in Azure DevOps is wrong for any non-default workflow.

**Independent Test**: Migrate issues that passed through custom states with known transition timestamps and verify the corresponding transition-date fields on the migrated work items match the Jira history.

**Acceptance Scenarios**:

1. **Given** an issue that moved into an "active" equivalent state on a known date, **When** it is migrated, **Then** the corresponding activated-date field is set to that date.
2. **Given** a workflow with a custom state mapped by the engineer to a target state and date field, **When** issues in that state are migrated, **Then** the configured transition date is populated.
3. **Given** an issue using only default states, **When** it is migrated, **Then** behavior is unchanged from today (no regression).

---

### User Story 4 - Correct embedded Jira issue links in text (Priority: P3)

A migration engineer migrates issues whose descriptions, repro steps, and comments contain links to other Jira issues. After migration those links should point to the corresponding Azure DevOps work items, not back to the decommissioned Jira instance.

**Why this priority**: Dangling links to a retired Jira instance degrade the migrated content and undermine the "consolidate off Jira" goal; correcting them preserves navigability.

**Independent Test**: Migrate a set of cross-referencing issues and confirm that in-text links between them resolve to the migrated work items, with no remaining links to the original Jira issues for in-scope items.

**Acceptance Scenarios**:

1. **Given** an issue description containing a link to another in-scope Jira issue, **When** both are migrated, **Then** the link in the migrated description points to the other issue's work item.
2. **Given** a comment that links to an issue that is **not** in scope for the migration, **When** the issue is migrated, **Then** the migration handles the unresolved reference gracefully (clearly retained or flagged) rather than producing a broken silent link.

---

### User Story 5 - Migrate remote/web links as hyperlinks (Priority: P4)

A migration engineer needs Jira "remote links" (web links to external pages, documents, dashboards) carried over as hyperlinks on the migrated work items.

**Why this priority**: Useful context preservation, but lower frequency and lower impact than version/sprint/history fidelity.

**Independent Test**: Migrate issues that have remote/web links and verify each appears as a hyperlink on the corresponding work item.

**Acceptance Scenarios**:

1. **Given** a Jira issue with a remote web link, **When** it is migrated, **Then** the work item has a hyperlink to the same URL.
2. **Given** an issue with multiple remote links, **When** it is migrated, **Then** all links are preserved without duplication.

---

### User Story 6 - Migrate branch development links (Priority: P4)

A migration engineer who has already moved their Bitbucket repositories to Azure DevOps needs **branch** development links (not just commit links) migrated onto the corresponding work items.

**Why this priority**: Extends an existing capability (commit links) and is only relevant when repositories have already been migrated, so it benefits a narrower audience.

**Independent Test**: With a repository mapping configured, migrate issues that reference branches and confirm branch links appear on the work items alongside commit links.

**Acceptance Scenarios**:

1. **Given** a configured repository mapping and an issue with a branch development link, **When** it is migrated, **Then** the work item has the corresponding branch link in Azure DevOps.
2. **Given** an issue with both commit and branch development links, **When** it is migrated, **Then** both types are migrated.

---

### User Story 7 - Composite field mapper (Priority: P5)

A migration engineer needs to combine several Jira fields into a single Azure DevOps field (for example, concatenating multiple Jira fields into one description or custom field) because the target process model has fewer or differently-shaped fields.

**Why this priority**: A convenience/flexibility capability that unblocks specific schema mismatches but is not required by every migration.

**Independent Test**: Configure a mapping that consolidates two or more Jira fields into one target field, migrate sample issues, and verify the target field contains the combined values as configured.

**Acceptance Scenarios**:

1. **Given** a configured composite mapping of two Jira fields into one target field, **When** issues are migrated, **Then** the target field contains both source values combined per the configuration.
2. **Given** one of the source fields is empty, **When** the issue is migrated, **Then** the composite result is produced without error and without stray separators.

---

### User Story 8 - Select properties of object/array fields (Priority: P5)

A migration engineer needs to map a specific property of a complex (object- or array-typed) Jira field — for example, a particular attribute of Fix Version, Affects Version, or Components — rather than only the default representation.

**Why this priority**: An enabler that increases mapping precision (and underpins richer version/component mapping), but most common fields work without it.

**Independent Test**: Configure a mapping that selects a named property of an object/array field, migrate sample issues, and verify the chosen property's value lands in the target field.

**Acceptance Scenarios**:

1. **Given** a Jira field that is an array of objects and a configured property selector, **When** issues are migrated, **Then** the selected property of each element is mapped to the target.
2. **Given** a property selector that matches no property on some issues, **When** those issues are migrated, **Then** they are migrated without error and the unmatched field is treated as empty.

---

### Edge Cases

- A release exists in Jira but has **no issues assigned** to it — its metadata should still be representable, or be safely skipped, without failing the run.
- An issue references a **Fix Version that was deleted/merged** in Jira — migration must not crash and should record the situation.
- A sprint or release has a **start date after its end/release date**, or malformed dates — handled gracefully without aborting the migration.
- A custom state has **no corresponding target transition-date field** configured — the engineer is warned and migration continues.
- An embedded Jira link points to an issue **outside the migration scope** or to a non-issue Jira URL — left intact or flagged, never silently broken.
- A remote link URL is **malformed or unreachable** — migrated as-is (the tool does not validate external reachability).
- A branch development link references a **repository not present in the repository map** — skipped with a warning, consistent with commit-link behavior.
- All new capabilities are **disabled by configuration** — output is byte-for-byte equivalent to the current Community edition (no regression).

## Requirements *(mandatory)*

### Functional Requirements

**Releases & version fields**

- **FR-001**: The system MUST migrate each issue's Fix Version assignment(s) to the target work item such that they are queryable/filterable in Azure DevOps.
- **FR-002**: The system MUST migrate each issue's Affects Version assignment(s) and keep them distinguishable from Fix Version assignments.
- **FR-003**: The system MUST preserve release definitions (name, description, start date, release date, released/unreleased status) so they remain accessible after migration. Because Azure DevOps work tracking has no native release/version entity, the **default representation** MUST require no Azure DevOps process customization: per-issue version associations are written as work-item **tags** (with a prefix distinguishing fix vs affects), and release **metadata** is emitted as a generated **release report** artifact in the migration output. The system MUST also allow the engineer to opt, via configuration, to map version associations to a **custom work-item field** instead of (or in addition to) tags. The precise representation is finalized during planning (`/speckit.plan`).
- **FR-004**: Version assignments MUST follow the same per-revision history treatment as other migrated fields.

**Sprint dates**

- **FR-005**: The system MUST set start and end dates on target iterations to match the corresponding Jira sprint dates.
- **FR-006**: The system MUST handle sprints without dates by creating the iteration without dates and without failing.

**State-transition dates**

- **FR-007**: The system MUST preserve transition dates for the default states (equivalent of new/active/resolved/closed) as it does today.
- **FR-008**: For custom workflow states, the system MUST by default infer the appropriate standard transition-date field (`ActivatedDate`/`ResolvedDate`/`ClosedDate`) from the state's category, and MUST allow the engineer to override the target date field per state via configuration.
- **FR-009**: When a custom state's transition-date target cannot be determined (no category match and no configured override), the system MUST warn and continue without setting a transition date.

**Embedded link correction**

- **FR-010**: The system MUST detect references to Jira issues embedded in text fields (description, repro steps, comments) — both explicit hyperlinks (e.g. `…/browse/KEY-123`, smart links) and bare issue keys in free text (e.g. `ABC-123`) — and rewrite them to point to the corresponding migrated work item. To prevent false positives, bare-key candidates MUST be validated against the pre-run inventory/index (see FR-020), and rewritten only if they resolve to a migrated work item. Resolving a validated reference to its work-item link uses the origin→work-item cross-reference; targets not yet migrated are resolved in a finalization pass.
- **FR-011**: Embedded references that do not resolve to a migrated work item (out-of-scope or deleted issues, or text that merely resembles an issue key) MUST be left as their original plain text and MUST NOT be rewritten; the count of such unresolved references SHOULD appear in the run summary.

**Remote / web links**

- **FR-012**: The system MUST migrate Jira remote/web links as hyperlinks on the corresponding work item, without duplication.

**Branch development links**

- **FR-013**: The system MUST migrate branch development links (in addition to commit links) when a repository mapping is configured, and MUST skip links whose repository is unmapped, with a warning.

**Composite field mapper**

- **FR-014**: The system MUST allow a single target field to be populated from multiple Jira source fields joined in a configured order with a configurable separator, skipping empty sources so no stray separators appear. This is a fallback for targets where adding a dedicated 1:1 field is not feasible.

**Object/array property selection**

- **FR-015**: The system MUST allow a mapping to select a named property of an object- or array-typed Jira field, and treat an unmatched property as empty without error.

**Cross-cutting**

- **FR-016**: Every new capability MUST be controllable via the existing configuration file (opt-in), with documented defaults.
- **FR-017**: With all new capabilities disabled, migration output MUST be equivalent to the current Community edition (no regression).
- **FR-018**: All new capabilities MUST respect the tool's existing resumability/journal behavior so that interrupted migrations can resume without duplicating or losing the new data.
- **FR-019**: The system MUST log a summary of what each new capability migrated (counts) and what it skipped (with reasons) for verification.
- **FR-020**: Before migrating work items, the system MUST build a **pre-run inventory/index** cataloging all in-scope Jira content — projects, issue keys, labels, and versions — and emit a summary of it. This index MUST be used to validate and resolve cross-references (notably embedded issue links per FR-010) during migration, independent of the order in which projects are migrated.

### Key Entities *(include if feature involves data)*

- **Release / Version**: A named Jira version with description, start date, release date, released status, and archived status. Issues reference it via Fix Version and Affects Version. Has no native equivalent in Azure DevOps work tracking; preserved per FR-003 (default: tags for associations + a release report for metadata; optionally a custom field).
- **Version assignment**: The association between an issue and a release, qualified as "fix" or "affects", potentially changing across the issue's history.
- **Sprint / Iteration**: A time-boxed period with start, end, and completion dates; maps to an Azure DevOps iteration node which must carry those dates.
- **State transition**: A change of an issue's workflow state at a point in time; maps to a target state plus, optionally, a transition-date field.
- **Embedded reference**: A hyperlink or bare issue key referencing another Jira issue inside a text field. Validated against the pre-run inventory; only references resolvable to a migrated work item are rewritten.
- **Pre-run inventory / index**: A catalog built before migration of all in-scope Jira content — projects, issue keys, labels, and versions — plus an emitted summary. Used as the source of truth for validating cross-references (e.g. embedded links) regardless of per-project migration order.
- **Remote link**: A web link (URL + title) attached to a Jira issue, migrated as a work-item hyperlink.
- **Development link**: A reference from an issue to source-control activity (commit or branch), resolved through a repository mapping.
- **Field mapping rule**: The engineer-defined rule governing how a source field maps to a target field, extended to support composite (many-to-one) mapping and property selection on complex fields.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of in-scope issues' Fix Version and Affects Version assignments are queryable in Azure DevOps after migration.
- **SC-002**: 100% of migrated iterations that correspond to dated Jira sprints show start and end dates matching the source.
- **SC-003**: For projects with custom workflows, time-in-state reporting derived from migrated data matches the source within the granularity of the migrated history (no missing transition dates for configured states).
- **SC-004**: 0 embedded links to in-scope Jira issues remain pointing at the original Jira instance after migration; all resolve to the corresponding work items.
- **SC-005**: 100% of remote/web links and branch development links present on in-scope issues appear on the corresponding work items (given repositories are mapped).
- **SC-006**: A migration engineer can enable and configure every parity capability through the configuration file with no source-code changes.
- **SC-007**: With all new capabilities disabled, a migration of a representative project produces output equivalent to the current Community edition (verified by comparison).
- **SC-008**: All eight PRO capabilities listed in the Overview are available in the free edition and demonstrably exercised by acceptance tests.
- **SC-009**: An interrupted migration that used the new capabilities resumes without duplicating or losing any of the newly-migrated data.

## Assumptions

- **Release/version representation defaults to tags + a generated release report**, with an opt-in custom field, chosen because tags require no Azure DevOps process customization and are immediately queryable while release metadata has no native home. The exact mechanics are a planning-phase decision.
- **Prefer dedicated 1:1 fields over composite mapping.** Where the target Azure DevOps process can be extended with a custom field (via an inherited process), mapping a Jira field to a dedicated ADO field 1:1 is preferred; the composite mapper (FR-014) is a fallback for non-customizable targets or deliberate field consolidation.
- **Scope = PRO edition only.** The separately-sold Suite capabilities (test-management migration such as Xray/Zephyr/QMetry, and Confluence-to-Wiki migration) are explicitly out of scope; "match pro" refers to the PRO edition feature set.
- The tool remains a **one-time, one-way** migrator; ongoing Jira↔Azure DevOps synchronization is out of scope.
- The target may be **Azure DevOps Services or Server/TFS**, consistent with the versions the tool already supports; new capabilities should not reduce that support.
- New capabilities are **opt-in** via configuration and default to off (or to current behavior) to guarantee no regression for existing users.
- Branch and commit development links assume the referenced **repositories have already been migrated** to Azure DevOps and are declared in the repository map, mirroring today's commit-link prerequisite.
- Embedded-link correction depends on a **complete mapping of source issue → migrated work item**; references to issues outside the migration scope cannot be rewritten and are handled per FR-011.
- "Equivalent output" for the no-regression criterion (SC-007/FR-017) is judged on migrated work-item data and history, not on incidental log text or timing.
- Functional parity (the same outcomes) is the goal; exact replication of the PRO edition's internal configuration schema is **not** required, though configuration should stay consistent with the tool's existing conventions.

## Out of Scope

- Test-management migration (Xray, Zephyr, QMetry → Azure DevOps Test Plans).
- Confluence → Azure DevOps Wikis migration.
- Ongoing/incremental synchronization after the initial migration.
- Automated migration of Jira boards, dashboards, automation rules, saved filters, and reports (these are rebuilt natively in Azure DevOps).
