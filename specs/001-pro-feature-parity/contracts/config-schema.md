# Contract: Config Schema Additions

The config file is the tool's user-facing contract. These additions are **backward-compatible**: every new key is optional and defaults to current behavior.

## New top-level keys (ConfigJson)

```jsonc
{
  // ... existing keys ...
  "build-inventory": false,          // FR-020: build pre-run project/issue/label/version index
  "version-target": "tags",          // US1: "tags" (default) | "field"
  "include-remote-links": false,     // US5
  "include-branch-links": false,     // US6 (requires repository-map + include-development-links)
  "correct-embedded-links": false,   // US4 (implies build-inventory)
  "state-date-map": [                 // US3 overrides (optional)
    { "state": "Active",   "date-field": "Microsoft.VSTS.Common.ActivatedDate" },
    { "state": "Resolved", "date-field": "Microsoft.VSTS.Common.ResolvedDate" }
  ]
}
```

## New field-map mappers (dispatched in JiraMapper)

| Mapper name | Capability | Behavior |
|---|---|---|
| `MapFixVersions` | US1 | Emits fix-version names as tags (`fix:<name>`) or a custom field per `version-target` |
| `MapAffectsVersions` | US1 | Emits affects-version names as tags (`affects:<name>`) or a custom field |
| `MapComposite` | US7 | Joins `composite-sources` with separator/template; skips empties |

### Composite mapper example (US7)
```jsonc
{
  "target": "Custom.OriginContext",
  "mapper": "MapComposite",
  "composite-separator": " / ",
  "composite-sources": [
    { "source": "components", "source-type": "name" },
    { "source": "environment", "source-type": "name" }
  ]
}
// Component="Payments", Environment="Prod"  ->  "Payments / Prod"
// Component="Payments", Environment=""       ->  "Payments"   (no stray separator)
```

### Object/array property selection example (US8)
```jsonc
{
  "source": "customfield_10100",   // an object/array-typed field
  "target": "Custom.PrimaryComponentLead",
  "property-path": "$[0].lead.displayName"
}
// unmatched path -> field treated as empty (no error)
```

### Fix/Affects version example (US1)
```jsonc
{ "source": "fixVersions",     "target": "System.Tags", "mapper": "MapFixVersions" },
{ "source": "versions",        "target": "System.Tags", "mapper": "MapAffectsVersions" }
// with "version-target": "field", target a custom field instead of System.Tags
```

## Validation contract
- Unknown/legacy configs that omit all new keys behave exactly as today (FR-017).
- `include-branch-links` without a matching `repository-map` entry → that link is skipped with a warning (FR-013).
- `correct-embedded-links: true` forces `build-inventory: true`.
- Invalid `state-date-map` date-field reference → warn + continue (FR-009).
