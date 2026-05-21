# Phase 1 Data Model: PRO Feature Parity

Entities are grouped by where they live: **config** (Migration.Common/Config), **intermediate JSON contract** (Migration.WIContract), **sidecar files** (workspace), and **import settings**. All additions are additive and optional (no-regression).

## Config entities (Migration.Common/Config)

### Field (extended)
Existing: `Source`, `Target`, `SourceType`, `For`, `NotFor`, `Type`, `Mapper`, `Mapping`. New optional members:
| Field | Type | Purpose |
|---|---|---|
| `composite-sources` | `List<CompositeSource>` | Source fields to consolidate into one target (US7) |
| `composite-template` | `string?` | Optional `string.Format`-style layout; default = separator join |
| `composite-separator` | `string` (default `" "`) | Separator when no template; empties skipped |
| `property-path` | `string?` | JSONPath into an object/array field value (US8) |
| `version-target` | enum `Tags`\|`Field` (default `Tags`) | How Fix/Affects versions land (US1) |

### CompositeSource (new)
| Field | Type | Notes |
|---|---|---|
| `source` | `string` | Jira field name/key |
| `source-type` | `string` = `id`\|`name` | Same semantics as `Field.SourceType` |
| `mapper` | `string?` | Optional per-source mapper applied before joining |

### ConfigJson (extended) — feature toggles
| Key | Type | Default | Purpose |
|---|---|---|---|
| `build-inventory` | bool | `false` | Build pre-run inventory/index (FR-020) |
| `include-remote-links` | bool | `false` | Fetch & migrate remote/web links (US5) |
| `include-branch-links` | bool | `false` | Migrate branch dev-links (US6); requires `repository-map` |
| `correct-embedded-links` | bool | `false` | Rewrite embedded issue links (US4) |
| `version-target` | enum | `Tags` | Global default for version representation (US1) |
| `state-date-map` | `List<StateDate>` | `[]` | Per-state → target date-field overrides (US3) |

### StateDate (new)
| Field | Type | Notes |
|---|---|---|
| `state` | `string` | Target ADO state name (post type/state mapping) |
| `date-field` | `string` | Reference name of the date field to set (e.g. `Microsoft.VSTS.Common.ActivatedDate`) |

## Intermediate JSON contract (Migration.WIContract)

### WiLink (extended) / WiRemoteLink (new)
Add remote/web-link support without breaking existing issue links:
| Field | Type | Notes |
|---|---|---|
| `IsRemoteLink` | bool (default false) | Distinguishes a web hyperlink from a work-item link |
| `Url` | `string?` | Target URL (remote links) |
| `Title` | `string?` | Hyperlink display name |

(Existing `Change`, `TargetOriginId`, `TargetWiId`, `WiType` unchanged.)

### WiDevelopmentLink (extended)
- `Type` now accepts `"Branch"` in addition to `"Commit"`. Existing `Id`, `Repository` fields carry the branch id and repo name.

### WiRevision / WiField (unchanged shape)
Version assignments flow through the existing `Fields` list as either `System.Tags` values (default) or a custom field reference name — no schema change. Per-revision transition timestamps already exist via `WiRevision.Time`.

## Sidecar files (workspace) — see contracts/sidecar-files.md
- **`release-metadata.json`** (new): map of version name → `{ Description, StartDate, ReleaseDate, Status, Archived }`.
- **`inventory-index.json`** (new): catalog of in-scope projects, issue keys, labels, versions.
- **`sprint-metadata.json`** (existing): map of sprint name → `{ State, StartDate, EndDate }`.
- **`ItemsJournal.txt`** (existing): origin issue key → work-item id; reused for embedded-link resolution.

## Import settings (WorkItemImport/Settings.cs, extended)
| Member | Type | Purpose |
|---|---|---|
| `ReleaseDates` | `Dictionary<string, ReleaseInfo>` | Loaded from `release-metadata.json` (parallels existing `SprintDates`) |
| `VersionTarget` | enum `Tags`\|`Field` | Mirrors config `version-target` |
| `StateDateMap` | `Dictionary<string,string>` | state → date-field overrides |
| `Inventory` | index handle | Loaded from `inventory-index.json` for link correction |

### ReleaseInfo (new, parallels SprintDateInfo)
`Name`, `Description`, `StartDate?`, `ReleaseDate?`, `Status`, `Archived`.

## Key relationships & rules
- A **Version** is referenced by issues via Fix/Affects associations; representation governed by `version-target` (R1).
- **Embedded reference** resolution: validated against `inventory-index.json`; linked via journal; unresolved → plain text (FR-011).
- **State transition** → date field: category-inferred by default, overridable via `state-date-map` (FR-008); unresolved → warn + continue (FR-009).
- **Validation**: malformed/absent dates (sprint or release) must not abort the run (Edge Cases); unmapped repository for a branch link → skip + warn (FR-013).
