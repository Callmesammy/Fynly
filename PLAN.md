# PLAN.md — AI CFO Backend Development Plan

## Vision
Build a production-grade, multi-tenant, AI-powered financial intelligence API that serves as the backend brain for the AI CFO platform. The API must be secure, scalable, observable, and deeply integrated with Claude AI for financial reasoning.

---

## Development Phases

---

## Phase 1 — Foundation (Weeks 1–3)

**Goal:** Solid base that everything else builds on. No shortcuts here.

### 1.1 Solution Setup
- [ ] Create solution with Clean Architecture structure
  ```
  AiCFO.API, AiCFO.Application, AiCFO.Domain, AiCFO.Infrastructure, AiCFO.Shared
  ```
- [ ] Configure `.editorconfig`, nullable reference types, global usings
- [ ] Setup `docker-compose.yml` with PostgreSQL, Redis, Seq, Hangfire
- [ ] Configure Serilog with structured logging to Seq
- [ ] Setup Scalar for API documentation
- [ ] Configure environment-based appsettings (Development, Staging, Production)

### 1.2 Domain Layer — Core Entities
- [ ] `Tenant` — root aggregate for multi-tenancy
- [ ] `User` — with roles (Owner, Admin, Accountant, Viewer)
- [ ] `BusinessProfile` — industry, country, currency, fiscal year settings
- [ ] `Account` — Chart of Accounts with account types (Asset, Liability, Equity, Revenue, Expense)
- [ ] `JournalEntry` + `JournalEntryLine` — immutable double-entry records
- [ ] `Transaction` — bank/payment transactions linked to accounts
- [ ] `Invoice` + `InvoiceLineItem` — AR/AP management
- [ ] `Contact` — customers and suppliers
- [ ] Value Objects: `Money`, `Currency`, `DateRange`, `TaxRate`, `Percentage`
- [ ] Domain Events: `TransactionCreated`, `InvoiceOverdue`, `AnomalyDetected`

### 1.3 Multi-Tenancy Infrastructure
- [ ] `TenantMiddleware` — resolves TenantId from JWT on every request
- [ ] `ITenantContext` — injectable current tenant service
- [ ] EF Core Global Query Filters on all tenant-scoped entities
- [ ] Tenant isolation tests — ensure tenant A cannot access tenant B data

### 1.4 Authentication & Authorization
- [ ] ASP.NET Identity configured with PostgreSQL
- [ ] JWT access token generation (15 min expiry)
- [ ] Refresh token rotation (7 day expiry, hashed in DB)
- [ ] Role-based authorization policies
- [ ] TOTP / 2FA support
- [ ] Endpoints: Register, Login, Refresh, Logout, ForgotPassword, ResetPassword, Enable2FA

### 1.5 CQRS Pipeline Setup
- [ ] MediatR configured with pipeline behaviors:
  - `ValidationBehavior` — FluentValidation on all commands/queries
  - `LoggingBehavior` — structured logging of every request
  - `CachingBehavior` — Redis cache for expensive queries
  - `AuthorizationBehavior` — tenant + role checks
- [ ] Global exception handling middleware
- [ ] `Result<T>` pattern implemented across all handlers

### 1.6 Audit Log
- [ ] `AuditLog` entity — TenantId, UserId, Action, EntityType, EntityId, OldValues, NewValues, Timestamp, IPAddress
- [ ] EF Core SaveChanges interceptor — auto-log all write operations
- [ ] Query endpoint for audit history (admin only)

---

## Phase 2 — Core Accounting Engine (Weeks 4–6)

**Goal:** The financial heart of the product. This must be bulletproof.

### 2.1 Chart of Accounts
- [ ] Seed default CoA templates per business type (Retail, Service, Manufacturing)
- [ ] CRUD for accounts with validation (no deletion if transactions exist)
- [ ] Account hierarchy (parent/child relationships)
- [ ] Account balance calculation with date range support

### 2.2 Double-Entry Engine
- [ ] Journal entry creation with automatic balance validation (debits = credits)
- [ ] Reversal entries — reverse a journal entry (never delete)
- [ ] Recurring journal entries — auto-post on schedule via Hangfire
- [ ] Period closing — lock entries for closed fiscal periods
- [ ] Trial balance calculation

### 2.3 Transaction Management
- [ ] Manual transaction entry with account assignment
- [ ] Bulk import via CSV upload
- [ ] Transaction categorization rules engine
- [ ] Bank reconciliation matching logic
- [ ] Transaction search with full-text search and filters

### 2.4 Invoice Management
- [ ] Invoice CRUD with line items, tax, discount support
- [ ] Invoice status lifecycle: Draft → Sent → Partial → Paid → Overdue → Void
- [ ] Credit notes and refunds
- [ ] Recurring invoices
- [ ] PDF generation with company branding (QuestPDF)
- [ ] Email delivery via Resend

### 2.5 Financial Reports Engine
- [ ] Profit & Loss Statement (with comparison periods)
- [ ] Balance Sheet (assets = liabilities + equity validation)
- [ ] Cash Flow Statement (direct and indirect method)
- [ ] Accounts Receivable Aging Report
- [ ] Accounts Payable Aging Report
- [ ] Custom date range support for all reports
- [ ] PDF and CSV export for all reports

---

## Phase 3 — Bank Integration Layer (Weeks 7–8)

**Goal:** Connect real bank data to power AI insights.

### 3.1 Mono Integration (Nigeria/Africa)
- [ ] Mono Connect widget integration flow
- [ ] Bank account linking and token storage (encrypted)
- [ ] Transaction sync — pull latest transactions on schedule
- [ ] Account balance real-time lookup
- [ ] Webhook handler for real-time transaction events
- [ ] Support for: GTB, Access, Zenith, UBA, First Bank, Stanbic

### 3.2 Plaid Integration (Global)
- [ ] Plaid Link flow for US, UK, EU, CA markets
- [ ] Transaction sync with category mapping
- [ ] Balance updates
- [ ] Webhook handler

### 3.3 Bank Feed Processing Pipeline
- [ ] Deduplication — prevent duplicate transaction imports
- [ ] Auto-categorization using rules + ML
- [ ] Unmatched transaction queue for manual review
- [ ] Sync status tracking per bank connection

---

## Phase 4 — AI Brain (Weeks 9–11)

**Goal:** This is the product's soul. The AI layer that makes everything else valuable.

### 4.1 Financial Context Builder
- [ ] `FinancialContextService` — assembles rich financial snapshot for every AI call
  - Current balances, recent transactions, open invoices
  - Cash position, burn rate, runway days
  - Top expense categories, top revenue sources
  - Overdue AR/AP summary
- [ ] Context compression for token efficiency
- [ ] Business profile context (industry, country, currency, fiscal year)

### 4.2 Claude Function Calling Tools
Implement all tools that Claude can call during a conversation:
- [ ] `get_account_balances(account_type?, as_of_date?)`
- [ ] `get_transactions(from_date, to_date, category?, min_amount?, max_amount?)`
- [ ] `get_cash_flow_forecast(days: 30|60|90)`
- [ ] `get_profit_and_loss(from_date, to_date, compare_previous?)`
- [ ] `get_accounts_receivable(overdue_only?)`
- [ ] `get_accounts_payable(upcoming_days?)`
- [ ] `get_expense_breakdown(from_date, to_date, group_by: category|vendor|month)`
- [ ] `get_profitability(group_by: product|client|branch)`
- [ ] `run_scenario(type: hire|price_change|new_branch, parameters)`
- [ ] `get_business_metrics(metric: runway|burn_rate|gross_margin)`

### 4.3 AI Chat API
- [ ] Chat session management (create, continue, list, delete)
- [ ] Message persistence — full conversation history per session
- [ ] Streaming endpoint using SSE
- [ ] Context injection — financial snapshot prepended to every session
- [ ] Tool execution pipeline — Claude calls tool → backend executes → result returned to Claude
- [ ] Message moderation — filter non-finance queries gracefully
- [ ] Token usage tracking per tenant for billing

### 4.4 Daily Briefing Engine
- [ ] Hangfire job runs daily at 6 AM per tenant timezone
- [ ] Collects previous day transactions, balance changes, overdue invoices
- [ ] Builds structured data payload → sends to Claude for narration
- [ ] Saves generated briefing to DB
- [ ] Pushes via SignalR on next user login
- [ ] Email delivery option

### 4.5 Proactive Alerts Engine
- [ ] Alert rule definitions:
  - Cash runway below 30 days
  - Unusual transaction amount (3x average for category)
  - Invoice overdue by 7/14/30 days
  - Duplicate transaction detected
  - Expense category exceeds budget
  - Revenue down >20% month-over-month
  - New payee with large first payment
- [ ] Alert severity levels: Info, Warning, Critical
- [ ] Claude generates human-readable alert message for each
- [ ] Alert delivery: in-app (SignalR), email, SMS (Termii), WhatsApp
- [ ] User alert preferences management

### 4.6 Cash Flow Forecasting Engine
- [ ] Historical pattern analysis (12 months lookback)
- [ ] Recurring revenue detection (subscriptions, retainers)
- [ ] Recurring expense detection (rent, payroll, subscriptions)
- [ ] Seasonal trend adjustment
- [ ] Outstanding invoice probability weighting (older = lower probability)
- [ ] 30/60/90 day projections with confidence intervals
- [ ] "Cash gap" detection with specific dates

### 4.7 Anomaly Detection
- [ ] ML.NET Isolation Forest model for transaction anomaly scoring
- [ ] Features: amount z-score, time-of-day, day-of-week, vendor frequency, category mismatch
- [ ] Threshold-based flagging with configurable sensitivity
- [ ] False positive feedback loop — users mark alerts as resolved/dismissed
- [ ] Model retraining job (weekly) incorporating feedback

### 4.8 Scenario Planning Engine
- [ ] Scenario types:
  - `HireScenario` — add N employees at salary X, forecast impact
  - `PriceChangeScenario` — adjust revenue by X%, forecast impact
  - `NewExpenseScenario` — add recurring expense, forecast impact
  - `PaymentDelayScenario` — what if client pays 30/60 days late
  - `LoanScenario` — take loan of X at Y% rate, forecast impact
- [ ] Each scenario returns updated 90-day cash flow projection
- [ ] Claude narrates the scenario outcome in plain English
- [ ] Save and compare multiple scenarios

---

## Phase 5 — Profitability Intelligence (Week 12)

### 5.1 Multi-Dimensional Profitability
- [ ] Revenue and cost assignment to dimensions:
  - Product / service line
  - Client / customer
  - Project
  - Branch / location
  - Team / department
- [ ] Gross margin calculation per dimension
- [ ] Profitability trend over time per dimension
- [ ] "Best client", "Most profitable product" rankings
- [ ] Underperforming dimension alerts

---

## Phase 6 — Integrations & Payments (Week 13)

### 6.1 Payment Processing
- [ ] Paystack integration (Nigeria/Africa subscription billing)
- [ ] Stripe integration (Global subscription billing)
- [ ] Subscription plan management (Free, Starter, Growth, Business, Enterprise)
- [ ] Usage metering for AI queries (token counting)
- [ ] Invoice payment via payment link
- [ ] Webhook handlers for payment events

### 6.2 OCR / Document Processing
- [ ] Receipt upload endpoint (image/PDF)
- [ ] Azure Document Intelligence extraction pipeline
- [ ] Extracted fields: vendor, amount, date, tax, line items
- [ ] Auto-create transaction from extracted data with user confirmation
- [ ] Invoice upload and parsing

---

## Phase 7 — API Hardening & DevOps (Week 14)

### 7.1 Performance
- [ ] Redis caching for all expensive report queries (5 min TTL)
- [ ] Pagination on all list endpoints (cursor-based for large datasets)
- [ ] Database query optimization — review all EF queries with logging
- [ ] Background processing for heavy operations (reports, AI calls)
- [ ] Response compression (Brotli/GZip)

### 7.2 Security Hardening
- [ ] Rate limiting per tenant per endpoint
- [ ] IP allowlist for enterprise tenants
- [ ] Security headers middleware (HSTS, CSP, X-Frame-Options)
- [ ] Input sanitization middleware
- [ ] Penetration test checklist review

### 7.3 Observability
- [ ] Structured logging for all key events
- [ ] Health check endpoints (`/health`, `/health/ready`, `/health/live`)
- [ ] Sentry error tracking with tenant context
- [ ] Performance metrics (request duration, DB query time, AI latency)
- [ ] Hangfire job failure alerting

### 7.4 DevOps
- [ ] Dockerfile (multi-stage build)
- [ ] docker-compose for local full-stack development
- [ ] GitHub Actions CI pipeline (build, test, lint)
- [ ] GitHub Actions CD pipeline (staging deploy on merge to main)
- [ ] Database migration automation in deployment pipeline
- [ ] Environment secrets management (GitHub Secrets / Azure Key Vault)

---

## Database Schema — Key Tables

```sql
-- Core tenant isolation
tenants (id, name, plan, is_active, created_at)
users (id, tenant_id, email, role, is_2fa_enabled)
business_profiles (id, tenant_id, name, industry, country, currency, fiscal_year_start)

-- Chart of accounts
accounts (id, tenant_id, code, name, type, parent_id, is_active)

-- Double-entry ledger
journal_entries (id, tenant_id, reference, date, description, is_reversed, created_by)
journal_entry_lines (id, journal_entry_id, account_id, debit, credit, description)

-- Transactions
transactions (id, tenant_id, date, description, amount, currency, category, account_id, 
              bank_connection_id, is_reconciled, anomaly_score)

-- Invoices
invoices (id, tenant_id, number, contact_id, status, issue_date, due_date, subtotal, 
          tax_total, total, currency, notes)
invoice_lines (id, invoice_id, description, quantity, unit_price, tax_rate, amount)

-- Bank connections
bank_connections (id, tenant_id, provider, institution_name, account_name, 
                  access_token_encrypted, last_synced_at, is_active)

-- AI features
ai_chat_sessions (id, tenant_id, user_id, title, created_at)
ai_chat_messages (id, session_id, role, content, tool_calls, created_at)
daily_briefings (id, tenant_id, date, content, was_read)
anomaly_alerts (id, tenant_id, transaction_id, severity, message, is_resolved, created_at)
cash_flow_forecasts (id, tenant_id, generated_at, forecast_data_json, horizon_days)

-- Billing
subscriptions (id, tenant_id, plan, status, provider, provider_subscription_id, 
               current_period_end, ai_query_count)

-- System
audit_logs (id, tenant_id, user_id, action, entity_type, entity_id, 
            old_values, new_values, ip_address, created_at)
```

---

## API Endpoint Summary

```
POST   /api/auth/register
POST   /api/auth/login
POST   /api/auth/refresh
POST   /api/auth/logout

GET    /api/accounts
POST   /api/accounts
PUT    /api/accounts/{id}

GET    /api/transactions
POST   /api/transactions
GET    /api/transactions/{id}
POST   /api/transactions/import

GET    /api/invoices
POST   /api/invoices
PUT    /api/invoices/{id}
POST   /api/invoices/{id}/send
POST   /api/invoices/{id}/pay

GET    /api/reports/profit-loss
GET    /api/reports/balance-sheet
GET    /api/reports/cash-flow
GET    /api/reports/ar-aging
GET    /api/reports/ap-aging

GET    /api/ai/briefing/today
GET    /api/ai/chat/sessions
POST   /api/ai/chat/sessions
POST   /api/ai/chat/sessions/{id}/messages  ← SSE streaming
GET    /api/ai/forecast?horizon=30
POST   /api/ai/scenario
GET    /api/ai/alerts
PATCH  /api/ai/alerts/{id}/resolve

POST   /api/bank/connect/mono
POST   /api/bank/connect/plaid
POST   /api/bank/sync/{connectionId}

POST   /api/documents/receipts
POST   /api/documents/invoices
```

---

## Definition of Done (Every Feature)

- [ ] Unit tests written and passing
- [ ] Integration test covering the happy path
- [ ] Input validation with meaningful error messages
- [ ] Audit log entry created on write operations
- [ ] Endpoint documented in Scalar
- [ ] Error cases return proper HTTP status codes
- [ ] Multi-tenancy isolation verified
- [ ] No EF N+1 queries (use `.Include()` or projections)
