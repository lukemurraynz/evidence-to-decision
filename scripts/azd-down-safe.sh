#!/usr/bin/env bash

set -euo pipefail

if [[ $# -ne 1 ]]; then
  printf 'Usage: %s <environment-name>\n' "$0" >&2
  exit 64
fi

environment_name=$1
active_environment=${AZURE_ENV_NAME:-}

if [[ -z "$active_environment" || "$environment_name" != "$active_environment" ]]; then
  printf 'Refusing deletion: requested environment does not match AZURE_ENV_NAME.\n' >&2
  exit 65
fi

if [[ ! "$environment_name" =~ ^[a-z0-9-]{3,20}$ ]]; then
  printf 'Refusing deletion: invalid environment name.\n' >&2
  exit 65
fi

printf 'This permanently purges Azure environment %s.\n' "$environment_name" >&2
printf 'Type DELETE %s to continue: ' "$environment_name" >&2
IFS= read -r confirmation

if [[ "$confirmation" != "DELETE $environment_name" ]]; then
  printf 'Confirmation did not match. No resources were changed.\n' >&2
  exit 66
fi

azd down --environment "$environment_name" --purge
