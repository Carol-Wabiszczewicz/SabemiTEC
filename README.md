# Sabemi Tec — Webhooks de Pagamento

Serviço que recebe notificações (webhooks) de um banco parceiro confirmando a liquidação de seguros/parcelas de empréstimo, garante idempotência, processa a regra de negócio em background e expõe um painel administrativo para acompanhar os eventos e o status de cada contrato

## Arquitetura

```
Banco parceiro
      │  POST /webhooks/pagamento (X-Api-Key)
      ▼
┌─────────────────────────────┐
│   ASP.NET Core Web API      │
│                              │
│  1. valida ApiKey            │
│  2. valida payload           │
│  3. verifica idempotência    │──► PostgreSQL
│     (id_transacao único)     │     ├─ EventosLog        (Log de Eventos Brutos)
│  4. grava evento (Pendente)  │     └─ StatusContratos    (Status do Contrato)
│  5. responde rápido (202)    │
│  6. enfileira (Channel)      │
└──────────────┬───────────────┘
               │
               ▼
   BackgroundService (worker)
   - Task.Delay(2s) simulando regra de negócio pesada
   - valida regra de negócio
   - atualiza EventosLog (Processado/Erro)
   - upsert em StatusContratos

Dashboard React  ──GET /pagamentos?status=&idContrato=──►  API  ──►  PostgreSQL
                 ──GET /pagamentos/contratos──────────────►
```

**Por que essa arquitetura:**
- O endpoint do webhook só faz uma leitura de idempotência + um insert (rápido) e devolve `202 Accepted` imediatamente dai o banco parceiro não fica esperando o processamento pesado
- A idempotência é garantida em duas camadas: (1) checagem prévia por `id_transacao` e (2) um índice único no banco, que protege contra a corrida de duas notificações concorrentes com o mesmo `id_transacao` (testado)
- O processamento pesado roda num `BackgroundService` consumindo uma fila em memória (`System.Threading.Channels`), fora do ciclo request/response.

## Stack

- **Backend:** ASP.NET Core 8 (Web API), Entity Framework Core 8, Npgsql (PostgreSQL)
- **Banco:** PostgreSQL 16 (via Docker)
- **Frontend:** React 19 + TypeScript + Vite
- **Infra local:** Docker Compose (apenas o Postgres; API e frontend rodam nativamente)

## Estrutura de pastas

```
Avaliacao/
├── backend/
│   └── SabemiTec.Api/
│       ├── Controllers/       (WebhooksController, PagamentosController)
│       ├── Data/               (AppDbContext, Migrations)
│       ├── Dtos/                (WebhookPagamentoRequest)
│       ├── Models/              (EventoLog, StatusContrato)
│       └── Services/            (fila em memória + worker de processamento)
├── frontend/
│   └── src/                   (App.tsx, api.ts, types.ts)
├── docker-compose.yml          (PostgreSQL)
└── README.md
```

## Como rodar

### 1. Banco de dados

```bash
docker compose up -d postgres
```

### 2. Backend

```bash
cd backend/SabemiTec.Api
dotnet ef database update   # aqui se aplica as migrations 
dotnet run --urls http://localhost:5080
```

A API sobe em `http://localhost:5080` (Swagger em `http://localhost:5080/swagger`).

ApiKey de desenvolvimento (configurada em `appsettings.json`, seção `Webhook:ApiKey`):
```
sabemitec-dev-apikey-2026
```

### 3. Frontend

```bash
cd frontend
npm install
npm run dev
```

Dashboard em `http://localhost:5173`.

## Endpoints

### `POST /webhooks/pagamento`

Header obrigatório: `X-Api-Key: sabemitec-dev-apikey-2026`

```json
{
  "id_transacao": "TX-001",
  "id_contrato": "CT-100",
  "valor": 1500.50,
  "data_pagamento": "2026-08-17T10:00:00Z",
  "status": "Sucesso"
}
```

| Cenário | Resposta |
|---|---|
| ApiKey ausente/inválida | `401` |
| JSON malformado / campo `id_transacao` ausente | `400` |
| `id_transacao` já recebido antes | `200` (não reprocessa, não duplica) |
| Evento novo e válido | `202 Accepted` (processamento segue em background) |

### `GET /pagamentos?status=&idContrato=`

Lista o Log de Eventos Brutos. `status` aceita `Pendente`, `Processado`, `Erro`
ou `Duplicado`. `idContrato` filtra por substring.

### `GET /pagamentos/contratos`

Lista o Status do Contrato (visão consolidada por `id_contrato`).

## Erros encontrados durante o desenvolvimento (e como foram resolvidos)

Documentando de forma transparente os problemas reais que apareceram no caminho e não só o resultado final:

1. **`dotnet` não era encontrado apesar do SDK estar instalado.**
   tinha um `dotnet.exe` "stub" vazio em `Program Files (x86)\dotnet`, que
   aparecia primeiro no `PATH` e mascarava o SDK real instalado em
   `Program Files\dotnet` (sem nenhuma pasta `sdk`). Resolvi apontando
   explicitamente para o SDK correto ao invocar os comandos `dotnet`

2. **Pacotes do EF Core incompatíveis com o projeto.**
   `dotnet add package Microsoft.EntityFrameworkCore.Design` e
   `Npgsql.EntityFrameworkCore.PostgreSQL` instalaram por padrão a versão
   `10.x`, que exige `net10.0`  mas o projeto foi criado como `net8.0` dai o
   `dotnet build` falhou com `NU1202` (pacote incompatível com o framework).
   Resolvido fixando a versão explicitamente para `8.0.11` (compatível com o
   .NET 8 LTS usado no projeto)

3. **Filtro por status quebrava com erro de tradução do LINQ.**
   O endpoint `GET /pagamentos?status=Erro` lançava `System.InvalidOperationException: ... Translation of method 'object.ToString' failed`. A causa: o filtro comparava `e.StatusProcessamento.ToString() == status` dentro de um `Where`, e o EF Core não consegue traduzir `ToString()` de um enum para SQL nesse contexto (mesmo com o enum mapeado como string).
   Descobri testando a API diretamente via `curl` antes de validar no dashboard. Corrigi fazendo `Enum.TryParse<StatusProcessamento>` do
   parâmetro recebido e comparando o enum diretamente (`e.StatusProcessamento == statusEnum`),que o EF traduz sem problemas e retornando `400` com mensagem clara caso o valor de status enviado seja inválido

4. **`dotnet build` falhando com o arquivo `.exe` travado.**
   Ao corrigir o bug do filtro e rodar `dotnet build` de novo com a  API ainda em execução (`dotnet run` de uma sessão anterior), o build
   falhou com `MSB3027`/`MSB3021`: "não foi possível copiar apphost.exe... o arquivo está bloqueado pelo processo". O processo antigo ainda seguravao `.exe` gerado. Dai resolvi finalizando o processo do `dotnet run` anterior antes de recompilar  lição prática de que hot-reload/rebuild em .NET ;) 
   exige parar a instância rodando quando não se usa `dotnet watch`


## Testes realizados (evidência via curl)

Todos os cenários abaixo foram executados manualmente contra a API local:

- ✅ Requisição sem `X-Api-Key` → `401`
- ✅ Requisição com `X-Api-Key` inválida → `401`
- ✅ JSON malformado → `400`
- ✅ Payload sem `id_transacao` → `400`
- ✅ Evento novo e válido → `202`, processado ~2s depois (background)
- ✅ Reenvio do **mesmo** `id_transacao` → `200`, sem duplicar o log
- ✅ **Duas requisições concorrentes simultâneas** com o mesmo `id_transacao`
  (teste de corrida real, via `curl` em paralelo) → apenas 1 registro
  persistido, graças ao índice único + tratamento de `DbUpdateException`
- ✅ Evento com `status: "Erro"` vindo do banco → marcado como `Erro` no log,
  contrato não é atualizado
- ✅ Filtro por `status` e por `idContrato` no dashboard e via API


## Limitações conhecidas / próximos passos

- Autenticação via `ApiKey` simples no header em produção, o ideal seria validar uma assinatura HMAC do payload (ex: header `X-Signature`) usando uma secret compartilhado com o banco
- Não há tratamento de exceções global (middleware) com isso erros inesperados retornariam a stack trace completa em ambiente de desenvolvimento
- Sem testes automatizados (unitários/integração) os testes descritos acima foram executados manualmente via `curl`
- Sem paginação real no `GET /pagamentos` (limitado a 200 registros mais recentes)
