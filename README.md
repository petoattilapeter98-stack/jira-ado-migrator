# jira-ado-migrator

A working repository for extending the **Jira → Azure DevOps work-item migrator** toward feature parity with the commercial PRO edition.

## Contents

- **`jira-azuredevops-migrator/`** — the .NET migration tool, vendored from the open-source
  [Solidify/jira-azuredevops-migrator](https://github.com/solidify/jira-azuredevops-migrator)
  (Community edition). This is the codebase we build upon. See its `LICENSE.md` for upstream terms.
- **`specs/`** — [Spec Kit](https://github.com/github/spec-kit) feature specifications.
  - `specs/001-pro-feature-parity/` — specification for implementing the eight PRO-only
    capabilities (Releases & Fix/Affects Version, sprint dates, custom-state transition dates,
    embedded-link correction, remote links, branch dev-links, composite mapper, object/array
    property selection) in the freely-available tool.
- **`.specify/`** — Spec Kit configuration, templates, and extensions driving the spec workflow.

## Status

Specification phase. The PRO-parity spec is drafted and clarified; next step is `/speckit.plan`.

## Provenance

The vendored `jira-azuredevops-migrator/` was imported from the upstream Community edition and
flattened into this repository (its original git history remains available upstream).
