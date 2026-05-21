# Community Edition — PRO Feature Parity Additions

This build adds several capabilities toward parity with the PRO edition. **All are opt-in**: with the new keys absent, migration behaves exactly as before (no regression).

See the full specification under `specs/001-pro-feature-parity/`.

## New config keys (top level)

| Key | Default | Purpose |
|---|---|---|
| `build-inventory` | `false` | Build a pre-run `inventory-index.json` (projects/issue keys) used to validate embedded links |
| `version-target` | `"tags"` | How Fix/Affects versions land: `"tags"` or `"field"` |
| `include-remote-links` | `false` | (import-capable) migrate remote/web links as ADO Hyperlinks |
| `include-branch-links` | `false` | (import-capable) migrate branch dev-links |
| `correct-embedded-links` | `false` | Rewrite in-text Jira issue links to migrated work items (implies `build-inventory`) |
| `state-date-map` | `[]` | Per-state → transition-date-field overrides, e.g. `[{ "state": "Active", "date-field": "Microsoft.VSTS.Common.ActivatedDate" }]` |

## New field-map options (per field)

| Key | Purpose |
|---|---|
| `composite-sources` | List of `{ source, source-type }` to consolidate into one target field |
| `composite-template` | Optional `string.Format` layout (e.g. `"{0} - {1}"`); default is separator-join |
| `composite-separator` | Separator for composite join (default `" "`); empty sources skipped |
| `property-path` | JSONPath into an object/array field value, e.g. `"$[0].lead.displayName"` |

## New mappers

| Mapper | Effect |
|---|---|
| `MapFixVersions` | Fix Versions → `fix:<name>` tags (semicolon-separated) |
| `MapAffectsVersions` | Affects Versions → `affects:<name>` tags |
| `MapComposite` | Consolidate `composite-sources` into one field (see above) |

## Sidecar files (written to the workspace by jira-export)

- `release-metadata.json` — version name → `{ Description, StartDate, ReleaseDate, Released, Archived }`; the importer logs a **release report** from it.
- `inventory-index.json` — projects + issue keys; used for embedded-link validation.
- `sprint-metadata.json` *(already existed)* — sprint dates, applied to ADO iteration nodes.

## Capability status

| Capability | Status |
|---|---|
| Fix/Affects Version → tags + release report | ✅ end-to-end |
| Sprint dates on iterations | ✅ end-to-end |
| Custom-state transition dates (auto + overrides) | ✅ end-to-end |
| Embedded Jira-link correction (backward refs) | ✅ live during import |
| Composite field mapper | ✅ end-to-end |
| Object/array property selection | ✅ end-to-end |
| Remote/web links, branch dev-links | ◑ import-side capable; **export-side fetch pending** (needs Jira REST work) |
| Embedded-link forward-reference finalization pass | ◑ pending (backward refs already work) |

## Example

See `docs/Samples/config-pro-parity.json` for a config exercising these options.
