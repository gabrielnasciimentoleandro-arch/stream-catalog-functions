# Textos para publicação

## Nome sugerido do repositório

stream-catalog-functions

## Descrição curta para o GitHub

API serverless em .NET 8 com Azure Functions, Blob Storage, Cosmos DB, Bicep, testes automatizados e CI/CD com OIDC.

## Título para a DIO

Stream Catalog Functions — Catálogo Serverless com Azure Functions

## Descrição para a entrega

Desenvolvi a Stream Catalog Functions, uma API serverless original em .NET 8 para gerenciar um catálogo de filmes e séries. A solução permite enviar capas, cadastrar metadados, listar, paginar, pesquisar por título, gênero, tipo ou ano e consultar detalhes. Também inclui endpoint de saúde e contrato OpenAPI.

A arquitetura utiliza Azure Functions no modelo isolated worker, Azure Blob Storage com container privado para as capas e Azure Cosmos DB for NoSQL para persistência do catálogo. A infraestrutura está declarada em Bicep e configura uma identidade gerenciada com acesso aos serviços sem armazenar chaves de aplicação.

O repositório contém 80 testes automatizados, cobertura medida de 100% de linhas e branches no escopo unitário, validação de JSON e uploads, respostas Problem Details, exemplos HTTP, documentação de segurança e workflows de CI/CD. O host real foi validado localmente com Azurite em 23 grupos de verificações, incluindo upload e download de blobs, cadastro, filtros, erros e concorrência.

Como não possuo uma assinatura Azure, não realizei nem simulei uma implantação remota. O deploy manual por GitHub Actions utiliza autenticação OpenID Connect e está documentado para execução futura em uma assinatura válida.
