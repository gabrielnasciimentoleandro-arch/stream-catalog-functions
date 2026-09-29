#!/usr/bin/env bash
set -euo pipefail

command -v az >/dev/null 2>&1 || {
  printf 'Required command not found: az\n' >&2
  exit 1
}

RESOURCE_GROUP=${1:-${RESOURCE_GROUP:-}}
if [[ -z "$RESOURCE_GROUP" ]]; then
  printf 'Usage: %s <resource-group>\n' "$0" >&2
  exit 1
fi

if [[ ! "$RESOURCE_GROUP" =~ ^rg-stream-catalog-[a-z0-9-]+$ ]]; then
  printf 'Refusing to delete an unexpected resource group: %s\n' "$RESOURCE_GROUP" >&2
  exit 1
fi

printf 'Type the resource-group name to confirm deletion: '
read -r confirmation
if [[ "$confirmation" != "$RESOURCE_GROUP" ]]; then
  printf 'Confirmation did not match. Nothing was deleted.\n' >&2
  exit 1
fi

az group delete \
  --name "$RESOURCE_GROUP" \
  --yes \
  --no-wait

printf 'Deletion requested for %s.\n' "$RESOURCE_GROUP"
