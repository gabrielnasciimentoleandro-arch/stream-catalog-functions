# Security Policy

## Supported version

Security fixes are applied to the latest revision of the `main` branch.

## Reporting a vulnerability

Do not open a public issue containing credentials, tokens, personal data or exploit details. Contact the repository owner privately and include:

- affected component;
- reproduction steps;
- expected and observed behavior;
- possible impact;
- suggested mitigation, when available.

## Security decisions

- Azure write endpoints use Function-level authorization.
- Blob containers are private and files are downloaded through the API.
- Azure resources require HTTPS and TLS 1.2 or newer.
- The Function App uses managed identity for Blob Storage and Cosmos DB data access.
- The deployment workflow authenticates with Azure OpenID Connect and does not use publish profiles.
- Request payloads, media types, file names and file sizes are validated.
- JSON properties and numeric enum values not defined by the contract are rejected.
- Local settings, environment files, build outputs and certificates are ignored by Git.

## Secrets

Never commit `local.settings.json`, `.env`, connection strings, account keys, publish profiles or Azure credentials. Rotate a credential immediately if it is exposed.
