# Evidências sugeridas

As evidências devem ser capturadas pelo autor depois de executar cada etapa. Não inclua URLs, recursos ou telas que não existam.

## Execução local

- terminal mostrando as nove Functions carregadas;
- `GET /api/v1/health` com HTTP 200;
- upload de uma capa com HTTP 201;
- criação de um item com HTTP 201;
- pesquisa retornando o item cadastrado;
- saída de `dotnet test` com todos os testes aprovados.

## GitHub

- página principal do repositório com o README;
- execução verde do workflow `CI`;
- etapas de build, testes, cobertura, publish e Bicep aprovadas.

## Azure — somente se houver implantação real

- recursos do grupo criado;
- Function App em execução;
- resposta pública do endpoint de saúde;
- execução manual aprovada do workflow de deploy;
- exclusão do grupo ao término, quando aplicável.

## Transparência desta entrega

Durante a elaboração, a Function App e o Azurite foram executados localmente. O Bicep e os workflows foram validados estaticamente. Não foi utilizada uma assinatura Azure, portanto não existem URL pública, captura do portal ou execução remota a apresentar.
