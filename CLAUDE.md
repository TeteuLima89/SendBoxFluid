# CLAUDE.md — SendBox Fluid

Este arquivo serve como referencia completa do projeto SendBox Fluid para manter contexto entre conversas.

## O que e este projeto

**SendBox Fluid** e um sandbox/mock de ERPs (SAP Business One e Sankhya) para que equipes de QA possam testar fluxos de integracao do Fluid sem precisar de um ambiente ERP real.

Funciona assim:
1. QA aponta o `urlSap` ou `urlSankhya` do scheduler Fluid para o SendBox
2. O fluxo Fluid roda normalmente (busca dados no Narwal real, processa, monta payload)
3. Em vez de enviar pro ERP real, envia pro SendBox
4. O SendBox aceita qualquer payload, retorna sucesso, e registra tudo
5. QA visualiza no painel web o que foi enviado, o resultado, e pode baixar os JSONs

## Stack tecnica

- .NET 8, ASP.NET Core MVC
- Entity Framework Core 8 + Npgsql (PostgreSQL)
- Fallback para in-memory quando sem banco
- Bootstrap 5 + jQuery DataTables + highlight.js
- Deploy: Render (Docker) + Neon (Postgres free sem expiracao)

## Arquitetura (padrao Comex3)

Estrutura de pastas numeradas 0-4, Clean Architecture:

```
SendBoxFluid/
├── 0 - Apresentacao/          # Controllers, ViewModels, Views (Razor)
│   ├── Controladores/
│   │   ├── B1sController.cs         # Mock SAP B1 Service Layer (/b1s/v1/*)
│   │   ├── SankhyaController.cs     # Mock Sankhya (/mge/service.sbr, /sankhya/*)
│   │   ├── PainelController.cs      # Frontend QA (painel web)
│   │   └── SandboxController.cs     # Utilitarios (seed, reset, saude)
│   ├── ViewModels/
│   │   ├── SessaoListaViewModel.cs
│   │   └── SessaoDetalheViewModel.cs
│   └── Views/
│       ├── Painel/Index.cshtml      # Listagem de sessoes (DataTable)
│       ├── Painel/Detalhe.cshtml    # Detalhes: envio ERP + relatorio
│       ├── Shared/_Layout.cshtml    # Layout com sidebar admin
│       ├── _ViewImports.cshtml
│       └── _ViewStart.cshtml
│
├── 2 - Aplicacao/             # Servicos de orquestracao
│   ├── Interfaces/
│   │   ├── IServicoAplicacaoDocumento.cs
│   │   └── IServicoAplicacaoSessao.cs
│   └── Servicos/
│       ├── ServicoAplicacaoDocumento.cs   # CRUD docs + auto-generate
│       └── ServicoAplicacaoSessao.cs      # CRUD sessoes + relatorio
│
├── 3 - Dominio/               # Entidades, enums, interfaces, servicos de dominio
│   ├── Entidades/
│   │   ├── DocumentoSap.cs           # Documento SAP generico (JsonObject wrapper)
│   │   ├── SessaoIntegracao.cs       # UMA execucao de fluxo (1 NF, 1 despesa, etc)
│   │   ├── RegistroRequisicao.cs     # UMA requisicao HTTP capturada
│   │   └── RelatorioIntegracao.cs    # Formato do retornointegracao do Fluid
│   ├── Enumeradores/
│   │   ├── ResultadoIntegracaoEnum.cs  # EmAndamento, Sucesso, Erro, Incompleto
│   │   ├── TipoAcaoEnum.cs            # 14 tipos: NF Entrada Draft, Transito, Saida, Despesa, etc
│   │   └── TipoErpEnum.cs             # Desconhecido, SapB1, Sankhya, SapS4
│   ├── Interfaces/Repositorios/
│   │   ├── IRepositorioDocumento.cs
│   │   └── IRepositorioSessao.cs
│   └── Servicos/
│       ├── ServicoFiltroOData.cs          # $filter, $select, $orderby, $top
│       ├── ServicoGeradorDocumento.cs     # Auto-gera docs fake quando GET retorna vazio
│       ├── ServicoIdentificadorAcao.cs    # Infere tipo de acao pelo endpoint/body
│       ├── ServicoExtratorIdentificador.cs # Extrai NfeId, ProcessoId, etc do payload
│       ├── ServicoConstrutorRelatorio.cs  # Monta JSON igual ao retornointegracao
│       ├── ServicoFormatadorJson.cs       # Formata JSON (trata string-encoded, \r\n, etc)
│       └── ServicoFusoHorario.cs          # UTC -> America/Sao_Paulo
│
├── 4 - Infraestrutura/        # Implementacoes concretas
│   ├── Middlewares/
│   │   └── MiddlewareCapturaRequisicao.cs  # Intercepta /b1s, /mge, /sankhya
│   ├── Persistencia/
│   │   └── SendBoxDbContext.cs             # EF Core (sessoes, requisicoes, documentos, contadores)
│   ├── Repositorios/
│   │   ├── RepositorioDocumentoEmMemoria.cs
│   │   ├── RepositorioDocumentoPostgres.cs
│   │   ├── RepositorioSessaoEmMemoria.cs
│   │   └── RepositorioSessaoPostgres.cs
│   └── ServicosFundo/
│       └── ServicoMantenedorAtivo.cs       # Auto-ping a cada 10min (evita idle Render)
│
├── Program.cs                 # DI, config, auto-migrate, fallback in-memory vs Postgres
├── Dockerfile                 # Multi-stage .NET 8
└── SendBoxFluid.csproj        # net8.0, EF Core + Npgsql
```

## Endpoints mockados

### SAP Business One (/b1s/v1/*)

| Metodo | Endpoint | Comportamento |
|---|---|---|
| POST | /b1s/v1/Login | Retorna SessionId (GUID), SessionTimeout=30 |
| POST | /b1s/v1/Logout | 204 |
| POST | /b1s/v1/{entidade} | Aceita qualquer JSON, retorna DocEntry (ou JdtNum pra JournalEntries) + campo error vazio |
| POST | /b1s/v1/{entidade}({id})/Cancel | 204 |
| PATCH | /b1s/v1/{entidade}({id}) | 204 |
| PATCH | /b1s/v2/{entidade}({id}) | 204 |
| GET | /b1s/v1/{entidade}?$filter&$select&$orderby&$top | Busca no store + auto-gera se vazio |
| GET | /b1s/v1/{entidade}({id}) | Busca por DocEntry |

### Sankhya (/mge/*)

| Metodo | Endpoint | Comportamento |
|---|---|---|
| POST/GET | /mge/service.sbr?serviceName=MobileLoginSP.login | Retorna jsessionid |
| POST | /mge/service.sbr?serviceName=CRUDServiceProvider.saveRecord | Retorna NUFIN |
| POST | /mge/service.sbr?serviceName=CACSP.incluirNota | Retorna NunotaSalva |
| POST | /mge/service.sbr?serviceName=* | Sucesso generico |
| POST | /sankhya/{path} | Gateway (login ou sucesso) |

### Painel e utilitarios

| Metodo | Endpoint | Funcao |
|---|---|---|
| GET | /painel/{erp} | Dashboard (todos, sapb1, sankhya) |
| GET | /painel/sessao/{id} | Detalhes de uma integracao |
| GET | /painel/sessao/{id}/download/relatorio | Baixar JSON do relatorio |
| GET | /painel/sessao/{id}/download/payload | Baixar JSON do payload enviado |
| POST | /sandbox/seed | Pre-popular documentos |
| DELETE | /sandbox/reset | Limpar tudo |
| GET | /saude | Health check (usado pelo auto-ping) |
| GET | /sandbox/store | Debug: ver documentos em memoria |

## Fluxos SAP B1 suportados

Testados e funcionando com os 5 fluxos:

| Fluxo | Status | Observacoes |
|---|---|---|
| nota-fiscal-entrada-skill-sapb1 | Funcionando | GET PO auto-gera, POST Drafts aceita tudo |
| nota-fiscal-saida-sapb1 | Funcionando | GET Orders auto-gera, POST Drafts aceita |
| despesas-importacao-sapb1 | Funcionando | GET PI auto-gera, POST LandedCosts aceita |
| despesas-importacao-sapb1-v2 | Funcionando | GET PI (serial/orderby), POST PI/VendorPayments |
| contas-pagar-sapb1 | Funcionando | GET PO, POST JournalEntries (JdtNum), PATCH, Cancel |

Os 2 fluxos Invent (nota-fiscal-invent-sapb1 e nota-fiscal-invent-sapb1-odata) NAO chamam SAP B1 — enviam pro Invent diretamente.

## Mecanismos importantes

### Auto-generate de documentos

Quando um GET com $filter nao acha nada no store, o SendBox gera automaticamente um documento fake. Extrai valores do filtro (DocNum, U_ACT_NfeId, etc) e preenche campos padrao. Gera 1000 DocumentLines (LineNum 0-999) pra cobrir qualquer referencia de pedidoCompraItem. QA nao precisa fazer seed manual.

### Captura de sessoes (middleware)

O MiddlewareCapturaRequisicao intercepta toda requisicao /b1s, /mge, /sankhya. Bufferiza Login e GETs por cookie B1SESSION. Quando chega um POST principal (Draft, LandedCost, etc), cria nova SessaoIntegracao com GUID proprio, move o buffer, extrai identificador de negocio e tipo de acao. Cada POST principal = 1 item no painel.

### Fallback query string

O Fluid codifica $filter=valor como %24filter%3Dvalor (o = vira %3D). ASP.NET nao reconhece como query parameter. O B1sController tem ResolverParametroQuery() que faz fallback pra raw query string via Uri.UnescapeDataString.

### Campo error vazio no sucesso

O POST principal retorna DocEntry + campo error vazio (`{ code: 0, message: { value: "" } }`). Isso evita "index of nil pointer" no template Go do passo retornoerro do Fluid, que assume que error.message.value sempre existe.

### Formatacao de JSON

O Fluid manda body como string-encoded JSON (com aspas envolvendo, \r\n escapado). ServicoFormatadorJson e ServicoExtratorIdentificador fazem: detectar string-encoded, desempacotar, converter escapes, achar inicio do JSON real, parsear.

## Infraestrutura

### Deploy

- **Render** (free tier) com Docker
- Env var `PORT` configurada pelo Render
- Env var `CONNECTION_STRING_POSTGRES` para persistencia

### Banco de dados

- **Neon** (Postgres free, sem expiracao, 500MB)
- Connection string: `postgresql://...neon.tech/...?sslmode=require`
- Program.cs normaliza URL postgres:// pra formato Npgsql
- Auto-migrate via `EnsureCreated()` no startup
- Fallback automatico pra in-memory se env var nao definida

### Tabelas PostgreSQL

| Tabela | Descricao |
|---|---|
| sessoes | SessaoIntegracao (PK=CodigoSessao, enums como int) |
| requisicoes | RegistroRequisicao (FK=CodigoSessao, cascade delete) |
| documentos | DocumentoArmazenado (Entidade + DadosJson text) |
| contadores | ContadorEntidade (DocEntry, JdtNum) |

### Anti-sleep

ServicoMantenedorAtivo (BackgroundService) pinga GET /saude a cada 10min pra evitar que o Render free tier durma (idle = 15min sem requisicao).

## Frontend

### Painel web

- Layout admin com sidebar fixa escura
- Sidebar: Todas as Sessoes, SAP Business One, Sankhya (com contadores)
- Listagem: DataTable com colunas Data/Hora, ERP, Acao, Identificador, Resultado, Mensagem, Reqs, Acoes
- Detalhes: Envio ao ERP (POST/PATCH/responses em accordion) + Resultado do Evento de Integracao (badge + mensagem + JSON)
- Syntax highlighting: highlight.js (atom-one-dark) em todos os JSONs
- Timezone: todas as datas em America/Sao_Paulo (UTC-3)

### Identificadores de negocio

Extraidos automaticamente do payload POST. SAP B1: U_ACT_NfeId, U_ACT_ComexId, U_ACT_Invoice, U_ChaveAcesso, Reference2, Reference1, NumAtCard, VendorCode, CardCode. Sankhya: NUNOTA, NUFIN, NUMNOTA, AD_NUMNFI, CHAVENFE.

## Como usar (QA)

### SAP B1

Scheduler Fluid — troca so o `urlSap`:
```json
{
    "env": { "fluxo": "nota-fiscal-entrada-skill-sapb1" },
    "parametros": {
        "urlSap": "sendboxfluid.onrender.com",
        "urlNarwal": "https://CLIENTE_API.narwalsistemas.com.br",
        ...demais parametros mantidos...
    }
}
```

### Sankhya

Troca `urlSankhya` e/ou `urlGatewaySankhya`:
```json
{
    "parametros": {
        "urlSankhya": "sendboxfluid.onrender.com",
        "urlGatewaySankhya": "sendboxfluid.onrender.com/sankhya",
        ...
    }
}
```

### Visualizar resultados

Acessa `https://sendboxfluid.onrender.com/painel`

### Limpar dados

```bash
curl -X DELETE https://sendboxfluid.onrender.com/sandbox/reset
```

## Nomes em pt-BR

Todo o codigo interno (metodos, variaveis, propriedades, entidades) esta em portugues brasileiro conforme padrao da empresa. Sufixos de framework (Controller, ViewModel, DbContext) ficam em ingles por convencao ASP.NET.

## Repositorio

GitHub: `TeteuLima89/SendBoxFluid`
Branch principal: `master`

## Limitacoes conhecidas

1. Sankhya mock basico — nao testado com fluxos reais ainda
2. Auto-generate de docs nao valida regras de negocio (aceita tudo)
3. Neon free: 500MB storage (suficiente pra sandbox)
4. Render free: precisa auto-ping pra nao dormir
5. Sessoes antigas (antes do fix de identificador) podem nao ter identificador
