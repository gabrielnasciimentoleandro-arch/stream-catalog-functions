# Relatório de validação

Data da revisão: **29 de setembro de 2026**.

## Código .NET

| Verificação | Resultado |
| --- | --- |
| Restore bloqueado | aprovado |
| `dotnet format --verify-no-changes` | aprovado |
| Build Release | 0 erros e 0 avisos |
| Testes automatizados | 80 aprovados, 0 falhas |
| Cobertura no escopo unitário | 100% de linhas e 100% de branches |
| Pacotes vulneráveis | nenhum identificado |
| Pacotes obsoletos | nenhum identificado |

A cobertura usa `coverage.runsettings`. O escopo exclui o ponto de entrada, código gerado pelo SDK e adaptadores que exigem serviços Azure reais. Permanecem cobertos serviços, validações, funções HTTP, contratos, serialização, configuração de dependências e adaptadores em memória.

## Smoke test real

A Function App foi iniciada com Azure Functions Core Tools 4.15.1 e Azurite 3.37.0. Foram aprovados **23 de 23 grupos de verificações HTTP**:

- metadados e cabeçalhos de segurança;
- endpoint de saúde e provedores configurados;
- contrato OpenAPI embutido;
- listagem vazia;
- exigência de filtro na pesquisa;
- upload real de PNG no Azurite;
- cabeçalho `Location` do upload;
- download do mesmo blob e preservação dos bytes;
- cache imutável da capa;
- criação de item;
- cabeçalho `Location` do catálogo;
- consulta de detalhes;
- paginação;
- combinação de quatro filtros;
- rejeição de enum numérico;
- rejeição de propriedade JSON desconhecida;
- rejeição de JSON malformado;
- rejeição de mídia não suportada;
- rejeição de arquivo vazio;
- HTTP 404 para item inexistente;
- HTTP 404 para capa inexistente;
- 30 de 30 health checks concorrentes;
- validação do limite de 5 MiB.

## Artefatos estáticos

| Artefato | Ferramenta | Resultado |
| --- | --- | --- |
| `openapi.yaml` | OpenAPI Spec Validator | aprovado |
| `infra/main.bicep` | Bicep CLI 0.47.16 | compilado sem erros ou avisos |
| workflows | actionlint | aprovado |
| scripts Bash | ShellCheck | aprovado |
| scripts Python | Ruff e `py_compile` | aprovado |
| código-fonte | Gitleaks 8.30.1 | nenhum segredo identificado |
| `compose.yaml` | schema da Compose Specification | aprovado |

## Limites da validação

- não havia Docker ou Podman no ambiente, então o Compose foi validado estaticamente e o Azurite foi executado diretamente por Node.js;
- o Cosmos DB real não foi acessado; o adaptador foi compilado, revisado e a lógica foi testada por abstrações e repositório em memória;
- nenhuma assinatura Azure foi utilizada;
- nenhum recurso, URL pública ou captura remota foi produzido;
- o workflow de deploy permanece manual e não foi acionado.
