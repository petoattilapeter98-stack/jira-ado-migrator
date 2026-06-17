#!/usr/bin/env bash
#
# provision-ado-users.sh
# ----------------------------------------------------------------------------
# Bulk-add Azure DevOps (Services / cloud) users so they exist BEFORE you run
# `wi-import`. The migrator maps Jira users -> ADO users via a user-mapping file
# (users.txt); for AssignedTo and history authorship to land on the right person,
# those ADO identities must already be in the organization.
#
# This reads the *target* identities from the mapping file (the part after "="),
# or you can pass --emails directly. Idempotent: users that already exist are
# skipped. Use --dry-run first to preview.
#
# Prerequisites:
#   az extension add --name azure-devops
#   export AZURE_DEVOPS_EXT_PAT=<PAT with "Member Entitlement Management (read & write)">
#     (or run `az devops login` / `az login`)
#
# Usage:
#   ./provision-ado-users.sh --org https://dev.azure.com/MYORG --mapping path/to/users.txt
#   ./provision-ado-users.sh --org https://dev.azure.com/MYORG --emails a@x.com,b@y.com --license express
#   ./provision-ado-users.sh --org https://dev.azure.com/MYORG --mapping users.txt --dry-run
#
# License types (Azure DevOps):
#   stakeholder  -> free, limited (good default for low-activity / reporter-only users)
#   express      -> Basic (full work-tracking; paid beyond the first 5 free)
#   advanced     -> Basic + Test Plans
# ----------------------------------------------------------------------------

set -euo pipefail

ORG=""
MAPPING=""
EMAILS=""
LICENSE="stakeholder"
DRY_RUN=false

usage() { sed -n '2,40p' "$0"; exit "${1:-0}"; }

while [ $# -gt 0 ]; do
  case "$1" in
    --org)      ORG="$2"; shift 2 ;;
    --mapping)  MAPPING="$2"; shift 2 ;;
    --emails)   EMAILS="$2"; shift 2 ;;
    --license)  LICENSE="$2"; shift 2 ;;
    --dry-run)  DRY_RUN=true; shift ;;
    -h|--help)  usage 0 ;;
    *) echo "Unknown argument: $1" >&2; usage 1 ;;
  esac
done

# --- validation ---
[ -n "$ORG" ] || { echo "ERROR: --org is required (e.g. https://dev.azure.com/MYORG)" >&2; exit 1; }
[ -n "$MAPPING" ] || [ -n "$EMAILS" ] || { echo "ERROR: provide --mapping <file> or --emails <a,b>" >&2; exit 1; }
command -v az >/dev/null || { echo "ERROR: Azure CLI ('az') not found. Install it, then: az extension add --name azure-devops" >&2; exit 1; }
az extension show --name azure-devops >/dev/null 2>&1 || { echo "ERROR: azure-devops extension missing. Run: az extension add --name azure-devops" >&2; exit 1; }

# --- collect target identities ---
declare -A seen=()
targets=()

add_target() {
  local e
  e="$(echo "$1" | xargs)"                 # trim whitespace
  [ -z "$e" ] && return
  [ "$e" = "default username" ] && return  # from the "*=default username" fallback line
  case "$e" in \#*) return ;; esac          # comment
  [[ "$e" == *@* ]] || return               # must look like an email/identity
  if [ -z "${seen[$e]:-}" ]; then seen[$e]=1; targets+=("$e"); fi
}

if [ -n "$MAPPING" ]; then
  [ -f "$MAPPING" ] || { echo "ERROR: mapping file not found: $MAPPING" >&2; exit 1; }
  # Each line: <jira-source>=<ado-target>. The wildcard line "*=default username" is skipped.
  while IFS= read -r line || [ -n "$line" ]; do
    case "$line" in ""|\#*) continue ;; esac
    [[ "$line" == \** ]] && continue        # skip "*=..." default mapping
    add_target "${line#*=}"                 # everything after the first "="
  done < "$MAPPING"
fi

if [ -n "$EMAILS" ]; then
  IFS=',' read -ra arr <<< "$EMAILS"
  for e in "${arr[@]}"; do add_target "$e"; done
fi

[ "${#targets[@]}" -gt 0 ] || { echo "No target identities found to provision." >&2; exit 0; }

echo "Org:      $ORG"
echo "License:  $LICENSE"
echo "Targets:  ${#targets[@]}"
$DRY_RUN && echo "(dry run — no changes will be made)"
echo "----------------------------------------"

added=0 skipped=0 failed=0 planned=0
for email in "${targets[@]}"; do
  if az devops user show --user "$email" --org "$ORG" >/dev/null 2>&1; then
    echo "skip (already exists): $email"; ((skipped++)); continue
  fi
  if $DRY_RUN; then
    echo "would add: $email  [$LICENSE]"; ((planned++)); continue
  fi
  if az devops user add --email-id "$email" --license-type "$LICENSE" --org "$ORG" --send-email-invite false >/dev/null 2>&1; then
    echo "added: $email  [$LICENSE]"; ((added++))
  else
    echo "FAILED: $email (check PAT scope / license availability / identity exists in Entra/MSA)"; ((failed++))
  fi
done

echo "----------------------------------------"
if $DRY_RUN; then
  echo "Dry run: $planned would be added, $skipped already exist."
else
  echo "Done: $added added, $skipped skipped (existing), $failed failed."
fi
[ "$failed" -eq 0 ]
