# Arquitetura

## Visão geral

A solução separa o protocolo HTTP, as regras de negócio e os adaptadores Azure. As Azure Functions dependem de interfaces de aplicação; os serviços dependem de interfaces de repositório; e a composição dos provedores acontece no ponto de entrada.

```text
HTTP Functions
    ↓
Application services and validation
    ↓
Repository abstractions
    ├── In-memory adapters for tests and local catalog data
    ├── Azure Blob Storage adapter for private covers
    └── Azure Cosmos DB adapter for persistent catalog data
```

## Componentes

### Functions

- `CreateCatalogItemFunction`: cadastra filmes e séries;
- `ListCatalogItemsFunction`: lista com paginação;
- `SearchCatalogItemsFunction`: filtra por título, gênero, tipo e ano;
- `GetCatalogItemFunction`: consulta detalhes;
- `UploadCoverFunction`: valida e envia capas;
- `GetCoverFunction`: serve conteúdo do container privado;
- `RootFunction`, `HealthFunction` e `OpenApiFunction`: operação e documentação.

### Application

`CatalogService` e `CoverService` concentram validação e normalização. As Functions apenas convertem HTTP em comandos e resultados em respostas padronizadas.

### Persistência

`ICatalogRepository` possui implementações em memória e Cosmos DB. `ICoverStorage` possui implementações em memória e Blob Storage. A seleção é feita pelas configurações `Catalog__Provider` e `CoverStorage__Provider`.

### Segurança

- gravações usam `AuthorizationLevel.Function` no Azure;
- blobs não têm acesso público;
- nomes de arquivo são gerados no servidor;
- extensões devem coincidir com o tipo de mídia;
- o tamanho máximo é 5 MiB;
- JSON desconhecido, malformado e enum numérico são rejeitados;
- consultas Cosmos utilizam parâmetros;
- a identidade gerenciada acessa Blob e Cosmos sem chaves de aplicação;
- o workflow Azure usa OIDC em vez de segredo de cliente ou publish profile.

## Cosmos DB

O container `items` usa `/type` como chave de partição. O campo contém `movie` ou `series`, seguindo a mesma representação JSON da API. Para o escopo educacional, essa divisão é previsível e permite consultas entre os dois tipos.

A listagem utiliza `OFFSET` e `LIMIT`, adequados ao volume demonstrativo. Em uma solução de grande escala, o recomendado seria expor tokens de continuação do Cosmos DB.

## Observabilidade

A aplicação usa OpenTelemetry. Quando `APPLICATIONINSIGHTS_CONNECTION_STRING` está presente, os sinais são exportados para o Application Insights. A configuração é condicional para permitir execução local sem credenciais de monitoramento.

## Ambientes

| Ambiente | Catálogo | Capas | Host storage |
| --- | --- | --- | --- |
| testes | memória | memória | não se aplica |
| local padrão | memória | Azurite | Azurite |
| Azure | Cosmos DB | Blob Storage | Storage Account |

## Limitações intencionais

- não há frontend;
- não há edição ou exclusão de itens, pois não fazem parte do fluxo principal do laboratório;
- não há upload multipart: a imagem é enviada diretamente no corpo HTTP;
- a infraestrutura mantém acesso de rede público com autenticação; private endpoints aumentariam custo e complexidade;
- nenhum recurso Azure foi criado durante o desenvolvimento sem uma assinatura válida.
