# CLAUDE.md — AI CFO Backend (.NET)

## Project Overview
AI CFO is a global AI-powered financial intelligence platform. It acts as an always-on Chief Financial Officer for businesses of all sizes — providing cash flow forecasting, proactive alerts, natural language financial querying, profitability intelligence, and scenario planning. Built with ASP.NET Core, Clean Architecture, CQRS, and integrated with Claude AI.

---

## Technology Stack

| Layer              | Technology                          |
|--------------------|-------------------------------------|
| Framework          | ASP.NET Core 8 Web API              |
| Architecture       | Clean Architecture + CQRS (MediatR) |
| ORM                | Entity Framework Core 8             |
| Database           | PostgreSQL 16                       |
| Cache              | Redis (StackExchange.Redis)         |
| Auth               | ASP.NET Identity + JWT Bearer       |
| AI                 | Anthropic Claude API (claude-sonnet-4-20250514) |
| Background Jobs    | Hangfire                            |
| Real-time          | SignalR                             |
| Validation         | FluentValidation                    |
| Mapping            | AutoMapper                          |
| Logging            | Serilog + Seq                       |
| Testing            | xUnit + Moq + Testcontainers        |
| Documentation      | Scalar (OpenAPI)                    |
| Containerization   | Docker + Docker Compose             |

---

## Solution Structure

```
AiCFO.sln
├── src/
│   ├── AiCFO.API/                    ← Entry point, controllers, middleware
│   ├── AiCFO.Application/            ← CQRS handlers, DTOs, interfaces
│   ├── AiCFO.Domain/                 ← Entities, value objects, domain events
│   ├── AiCFO.Infrastructure/         ← EF Core, external services, jobs
│   └── AiCFO.Shared/                 ← Shared kernel, constants, helpers
└── tests/
    ├── AiCFO.UnitTests/
    ├── AiCFO.IntegrationTests/
    └── AiCFO.ArchitectureTests/
```

---

## Architecture Rules

### Clean Architecture Dependency Flow
```
API → Application → Domain
Infrastructure → Application (implements interfaces)
NEVER: Domain → Application, Domain → Infrastructure
```

### CQRS Pattern (MediatR)
- Every operation is either a **Command** (writes) or a **Query** (reads)
- Commands return `Result<T>` — never throw exceptions for business logic
- Queries return read-optimized DTOs — never return domain entities
- Use pipeline behaviors for: validation, logging, caching, auth

### Domain Rules
- Use **Value Objects** for Money, Currency, Percentage, DateRange
- All monetary values stored as `decimal` — NEVER `float` or `double`
- Domain entities raise **Domain Events** on state changes
- **Aggregate roots** control invariants — no direct child manipulation
- Journal entries are **immutable** — reverse, never delete

### Multi-Tenancy
- Every table has `TenantId` (Guid) as a non-nullable column
- EF Core **Global Query Filters** automatically scope every query
- Tenant resolved from JWT claim on every request
- `ITenantContext` injected via middleware — never pass TenantId manually

---

## Key Conventions

### Naming
```csharp
// Commands
CreateInvoiceCommand, UpdateTransactionCommand, DeleteAccountCommand

// Queries  
GetCashFlowForecastQuery, ListTransactionsQuery, GetDailyBriefingQuery

// Handlers
CreateInvoiceCommandHandler, GetCashFlowForecastQueryHandler

// DTOs
InvoiceDto, TransactionSummaryDto, CashFlowForecastDto

// Validators
CreateInvoiceCommandValidator, UpdateTransactionCommandValidator
```

### Result Pattern — Use Everywhere
```csharp
// Never throw for business logic — always return Result
public async Task<Result<InvoiceDto>> Handle(CreateInvoiceCommand command, CancellationToken ct)
{
    if (amount <= 0)
        return Result.Failure<InvoiceDto>("Amount must be greater than zero");

    // ... logic

    return Result.Success(invoiceDto);
}
```

### Money Value Object — Always Use for Currency
```csharp
// Always use Money value object — never raw decimal for amounts
var amount = Money.Create(1500.00m, Currency.NGN);
var total = amount.Add(Money.Create(500.00m, Currency.NGN));
```

### API Response Envelope
```json
{
  "success": true,
  "data": { },
  "error": null,
  "meta": {
    "timestamp": "2025-01-01T00:00:00Z",
    "requestId": "abc-123"
  }
}
```

---

## AI Integration (Claude API)

### Claude is Used For
1. **Daily Briefing generation** — personalized morning financial summary
2. **Natural language chat** — answering questions about the user's finances
3. **Insight generation** — extracting anomalies and observations from data
4. **Scenario narration** — explaining what-if analysis in plain English
5. **Alert messaging** — writing human-readable alert descriptions

### Function Calling Tools Exposed to Claude
```
get_account_balances          → Returns all account balances
get_transactions              → Filtered transaction list
get_cash_flow_forecast        → 30/60/90 day forecast
get_profit_and_loss           → P&L for date range
get_accounts_receivable       → Outstanding invoices + aging
get_accounts_payable          → Upcoming payments
get_expense_breakdown         → Expenses by category
run_scenario                  → What-if simulation
get_business_context          → Company info, industry, preferences
```

### System Prompt Strategy
Every Claude request is prefixed with a rich system prompt containing:
- Business name, industry, country, currency
- Current date and fiscal year info
- Key financial metrics snapshot
- User's preferred communication style
- Instructions to always recommend action, not just report

### Streaming
- All chat responses use **Server-Sent Events (SSE)** for real-time streaming
- Frontend receives token-by-token for live chat feel
- Use `IAsyncEnumerable<string>` for streaming pipeline

---

## Database Guidelines

### Migration Strategy
```bash
# Always name migrations descriptively
dotnet ef migrations add AddCashFlowForecastTable --project AiCFO.Infrastructure
dotnet ef migrations add AddAnomalyAlertsWithSeverityLevel --project AiCFO.Infrastructure
```

### Indexing Rules
- Every foreign key must have an index
- Add composite index on `(TenantId, CreatedAt)` for all core tables
- Add index on `(TenantId, Status)` for invoice and transaction tables
- Use partial indexes for soft-deleted records

### Soft Delete
- All entities inherit `ISoftDeletable` with `IsDeleted`, `DeletedAt`, `DeletedBy`
- EF Global Query Filter excludes soft-deleted records automatically
- Hard delete is NEVER used on financial data

---

## Security Rules

- JWT tokens expire in 15 minutes — refresh tokens last 7 days
- Refresh tokens stored hashed in DB — never in plain text
- All bank credentials / API keys encrypted at rest (AES-256)
- Rate limiting applied per tenant on AI endpoints (60 req/hour free tier)
- All user actions written to immutable `AuditLog` table
- PII fields (email, phone, tax ID) encrypted at column level
- CORS strictly configured — no wildcard origins in production

---

## Background Jobs (Hangfire)

| Job                          | Schedule          | Description                              |
|------------------------------|-------------------|------------------------------------------|
| GenerateDailyBriefings       | Daily 6:00 AM     | AI morning summary for all tenants       |
| RefreshBankFeeds             | Every 4 hours     | Pull latest transactions from Mono/Plaid |
| RunAnomalyDetection          | Every 2 hours     | ML scan for unusual transactions         |
| UpdateCashFlowForecasts      | Daily midnight    | Recalculate 90-day forecasts             |
| SendOverdueInvoiceAlerts     | Daily 9:00 AM     | Flag and notify overdue AR               |
| ProcessRecurringTransactions | Daily midnight    | Auto-post recurring journal entries      |
| CleanupExpiredTokens         | Daily 2:00 AM     | Remove expired refresh tokens            |

---

## Environment Variables

```env
# Database
DATABASE_URL=postgresql://user:pass@localhost:5432/aicfo
REDIS_URL=redis://localhost:6379

# Auth
JWT_SECRET=<256-bit-secret>
JWT_ISSUER=aicfo.com
JWT_AUDIENCE=aicfo-api

# AI
ANTHROPIC_API_KEY=<key>
ANTHROPIC_MODEL=claude-sonnet-4-20250514

# Bank Integrations
MONO_SECRET_KEY=<key>
MONO_APP_ID=<id>
PLAID_CLIENT_ID=<id>
PLAID_SECRET=<key>
PLAID_ENV=sandbox|development|production

# Payments
PAYSTACK_SECRET_KEY=<key>
STRIPE_SECRET_KEY=<key>

# Notifications
TERMII_API_KEY=<key>
RESEND_API_KEY=<key>

# Storage
CLOUDFLARE_R2_ACCOUNT_ID=<id>
CLOUDFLARE_R2_ACCESS_KEY=<key>
CLOUDFLARE_R2_SECRET_KEY=<key>
CLOUDFLARE_R2_BUCKET=aicfo-storage

# OCR
AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT=<url>
AZURE_DOCUMENT_INTELLIGENCE_KEY=<key>

# Monitoring
SENTRY_DSN=<dsn>
SEQ_SERVER_URL=http://localhost:5341
```

---

## Testing Strategy

- **Unit tests** — Domain logic, value objects, calculation engines
- **Integration tests** — API endpoints with real PostgreSQL (Testcontainers)
- **Architecture tests** — Enforce Clean Architecture dependency rules (NetArchTest)
- **AI tests** — Snapshot tests for prompt construction; mock Claude responses
- Aim for 80%+ coverage on Domain and Application layers
- Use `WebApplicationFactory<Program>` for integration test setup

---

## Code Quality

- Enable **nullable reference types** globally
- Use **file-scoped namespaces**
- **No magic strings** — use constants or enums
- All public APIs must have **XML documentation**
- Run `dotnet format` before every commit
- Enforce with `.editorconfig` and Roslyn analyzers

---

## Getting Started

```bash
# Clone and restore
git clone https://github.com/your-org/aicfo-backend
cd aicfo-backend
dotnet restore

# Start dependencies
docker-compose up -d postgres redis hangfire-dashboard

# Apply migrations
dotnet ef database update --project AiCFO.Infrastructure --startup-project AiCFO.API

# Run
dotnet run --project AiCFO.API

# API available at
https://localhost:7001
https://localhost:7001/scalar  ← API docs
https://localhost:7001/hangfire ← Job dashboard
```
