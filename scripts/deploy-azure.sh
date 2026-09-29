#!/usr/bin/env bash
set -euo pipefail

require_command() {
  command -v "$1" >/dev/null 2>&1 || {
    printf 'Required command not found: %s\n' "$1" >&2
    exit 1
  }
}

require_command az
require_command dotnet
require_command zip

ROOT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
RESOURCE_GROUP=${RESOURCE_GROUP:-rg-stream-catalog-dev}
LOCATION=${LOCATION:-brazilsouth}
PROJECT_NAME=${PROJECT_NAME:-streamcatalog}
ENVIRONMENT_NAME=${ENVIRONMENT_NAME:-dev}
ARTIFACTS_DIR="$ROOT_DIR/artifacts"
PUBLISH_DIR="$ARTIFACTS_DIR/function"
PACKAGE_PATH="$ARTIFACTS_DIR/function.zip"

az account show --output none
az group create \
  --name "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --output none

FUNCTION_APP_NAME=$(az deployment group create \
  --resource-group "$RESOURCE_GROUP" \
  --template-file "$ROOT_DIR/infra/main.bicep" \
  --parameters \
    projectName="$PROJECT_NAME" \
    environmentName="$ENVIRONMENT_NAME" \
    location="$LOCATION" \
  --query 'properties.outputs.functionAppName.value' \
  --output tsv)

rm -rf "$ARTIFACTS_DIR"
mkdir -p "$PUBLISH_DIR"
dotnet restore "$ROOT_DIR/StreamCatalog.sln" --locked-mode
dotnet publish \
  "$ROOT_DIR/src/StreamCatalog.Functions/StreamCatalog.Functions.csproj" \
  --configuration Release \
  --no-restore \
  --output "$PUBLISH_DIR"
(
  cd "$PUBLISH_DIR"
  zip -q -r "$PACKAGE_PATH" .
)

az functionapp deployment source config-zip \
  --resource-group "$RESOURCE_GROUP" \
  --name "$FUNCTION_APP_NAME" \
  --src "$PACKAGE_PATH" \
  --output none

printf 'Function App: https://%s.azurewebsites.net\n' "$FUNCTION_APP_NAME"
printf 'Health: https://%s.azurewebsites.net/api/v1/health\n' "$FUNCTION_APP_NAME"
