# Deploy no Azure

> Estes passos criam recursos cobrados. Revise preços, políticas e permissões antes de executar. Nenhum deploy Azure foi realizado durante a elaboração deste repositório.

## Recursos provisionados

O Bicep cria um Storage Account, container privado de capas, Cosmos DB serverless, plano Consumption, Function App Linux, Log Analytics, Application Insights, identidade gerenciada e atribuições de acesso a dados.

## Opção 1 — script local

Pré-requisitos:

- assinatura Azure ativa;
- Azure CLI autenticada com uma identidade capaz de criar recursos e atribuições de função;
- SDK .NET 8;
- `zip` disponível no terminal.

Execute:

```bash
az login
az account set --subscription "ID-OU-NOME-DA-ASSINATURA"

RESOURCE_GROUP=rg-stream-catalog-dev \
LOCATION=brazilsouth \
PROJECT_NAME=streamcatalog \
ENVIRONMENT_NAME=dev \
bash scripts/deploy-azure.sh
```

O script cria o grupo, implanta `infra/main.bicep`, publica a Function App e mostra as URLs. Ele não imprime chaves de Functions ou credenciais.

## Opção 2 — GitHub Actions com OIDC

### 1. Criar o grupo de recursos

Faça esta etapa com sua identidade administrativa:

```bash
az group create \
  --name rg-stream-catalog-dev \
  --location brazilsouth
```

### 2. Criar a aplicação e o service principal

```bash
APP_ID=$(az ad app create \
  --display-name github-stream-catalog \
  --query appId \
  --output tsv)

SP_OBJECT_ID=$(az ad sp create \
  --id "$APP_ID" \
  --query id \
  --output tsv)

TENANT_ID=$(az account show --query tenantId --output tsv)
SUBSCRIPTION_ID=$(az account show --query id --output tsv)
RESOURCE_GROUP_ID=$(az group show \
  --name rg-stream-catalog-dev \
  --query id \
  --output tsv)
```

### 3. Conceder permissões no grupo

`Contributor` permite provisionar e publicar. `User Access Administrator` é necessário porque o Bicep atribui à identidade gerenciada o papel de acesso aos blobs.

```bash
az role assignment create \
  --assignee-object-id "$SP_OBJECT_ID" \
  --assignee-principal-type ServicePrincipal \
  --role Contributor \
  --scope "$RESOURCE_GROUP_ID"

az role assignment create \
  --assignee-object-id "$SP_OBJECT_ID" \
  --assignee-principal-type ServicePrincipal \
  --role "User Access Administrator" \
  --scope "$RESOURCE_GROUP_ID"
```

Aplique privilégio mínimo conforme as políticas da sua organização. Remova as atribuições quando não forem mais necessárias.

### 4. Criar a credencial federada

Substitua `SEU-USUARIO` pelo proprietário real do repositório:

```bash
APP_OBJECT_ID=$(az ad app show --id "$APP_ID" --query id --output tsv)

cat > federated-credential.json <<EOF
{
  "name": "github-stream-catalog-dev",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:SEU-USUARIO/stream-catalog-functions:environment:dev",
  "description": "GitHub Actions environment dev",
  "audiences": ["api://AzureADTokenExchange"]
}
EOF

az ad app federated-credential create \
  --id "$APP_OBJECT_ID" \
  --parameters federated-credential.json

rm federated-credential.json
```

O `subject` precisa coincidir exatamente com proprietário, repositório e environment usados pelo workflow.

### 5. Configurar secrets do GitHub

Em **Settings → Secrets and variables → Actions**, crie:

- `AZURE_CLIENT_ID`: valor de `APP_ID`;
- `AZURE_TENANT_ID`: valor de `TENANT_ID`;
- `AZURE_SUBSCRIPTION_ID`: valor de `SUBSCRIPTION_ID`.

Esses identificadores não são senhas, mas são mantidos como secrets para centralizar a configuração.

### 6. Executar o workflow

Abra **Actions → Deploy to Azure → Run workflow**, escolha `dev` e a região. O workflow é manual e não executa em `push`.

## Chave para endpoints de gravação

Depois do deploy, obtenha uma chave apenas quando necessário:

```bash
az functionapp keys list \
  --resource-group rg-stream-catalog-dev \
  --name NOME-DA-FUNCTION-APP \
  --query functionKeys.default \
  --output tsv
```

Não publique a chave. Envie-a no cabeçalho `x-functions-key` somente nas operações `POST`.

## Validar

```bash
curl "https://NOME-DA-FUNCTION-APP.azurewebsites.net/api/v1/health"
```

## Excluir e interromper cobranças

```bash
bash scripts/delete-azure-resources.sh rg-stream-catalog-dev
```

O script exige a repetição exata do nome e recusa grupos fora do padrão `rg-stream-catalog-*`.
