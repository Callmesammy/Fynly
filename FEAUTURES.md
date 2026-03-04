# FEATURES.md — AI CFO Backend Feature Specifications

## Feature Index

1. [Authentication & Identity](#1-authentication--identity)
2. [Multi-Tenancy](#2-multi-tenancy)
3. [Chart of Accounts](#3-chart-of-accounts)
4. [Double-Entry Engine](#4-double-entry-engine)
5. [Transaction Management](#5-transaction-management)
6. [Invoice Management](#6-invoice-management)
7. [Bank Feed Integration](#7-bank-feed-integration)
8. [AI Chat Interface](#8-ai-chat-interface)
9. [Daily Briefing Engine](#9-daily-briefing-engine)
10. [Cash Flow Forecasting](#10-cash-flow-forecasting)
11. [Anomaly Detection & Alerts](#11-anomaly-detection--alerts)
12. [Scenario Planning](#12-scenario-planning)
13. [Profitability Intelligence](#13-profitability-intelligence)
14. [Financial Reports Engine](#14-financial-reports-engine)
15. [OCR & Document Processing](#15-ocr--document-processing)
16. [Subscription & Billing](#16-subscription--billing)
17. [Notifications](#17-notifications)
18. [Audit & Compliance](#18-audit--compliance)

---

## 1. Authentication & Identity

### Description
Secure, multi-factor authentication system supporting JWT tokens with refresh rotation, social login, and 2FA. Every session is tenant-aware.

### Endpoints
| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/auth/register` | Create new account + tenant |
| POST | `/api/auth/login` | Email/password login |
| POST | `/api/auth/refresh` | Rotate refresh token |
| POST | `/api/auth/logout` | Revoke refresh token |
| POST | `/api/auth/forgot-password` | Send reset email |
| POST | `/api/auth/reset-password` | Complete password reset |
| POST | `/api/auth/2fa/enable` | Generate TOTP setup |
| POST | `/api/auth/2fa/verify` | Confirm and activate 2FA |
| GET  | `/api/auth/me` | Current user profile |

### Business Rules
- Password: min 8 chars, 1 uppercase, 1 number, 1 special character
- Access token expires in 15 minutes
- Refresh token expires in 7 days, rotated on every use
- After 5 failed login attempts, account locked for 30 minutes
- Registration creates both User and Tenant in a single transaction
- Email verification required before first login
- 2FA is optional but encouraged; enterprise plans can enforce it

### Security
- Passwords hashed with BCrypt (cost factor 12)
- Refresh tokens stored as SHA-256 hash in DB
- All auth events logged in AuditLog

---

## 2. Multi-Tenancy

### Description
Complete data isolation between tenants using a shared database, discriminator-based approach with EF Core Global Query Filters. Every piece of data belongs to exactly one tenant.

### Rules
- TenantId resolved from `tenant_id` JWT claim on every request
- Missing or invalid TenantId returns 401
- EF Core global filter ensures zero cross-tenant data leakage
- Tenant can have multiple users with different roles
- Roles: `Owner`, `Admin`, `Accountant`, `Viewer`

### Role Permissions Matrix
| Feature | Owner | Admin | Accountant | Viewer |
|---------|-------|-------|------------|--------|
| Manage users | ✅ | ✅ | ❌ | ❌ |
| Delete transactions | ✅ | ❌ | ❌ | ❌ |
| Create invoices | ✅ | ✅ | ✅ | ❌ |
| View reports | ✅ | ✅ | ✅ | ✅ |
| AI chat | ✅ | ✅ | ✅ | ✅ |
| Bank connections | ✅ | ✅ | ❌ | ❌ |
| Billing settings | ✅ | ❌ | ❌ | ❌ |

---

## 3. Chart of Accounts

### Description
Hierarchical account structure that forms the backbone of the double-entry system. Every financial transaction maps to one or more accounts.

### Account Types
```
ASSET          (Debit increases, Credit decreases)
  ├── Current Assets
  │     ├── Cash and Bank
  │     ├── Accounts Receivable
  │     └── Inventory
  └── Fixed Assets
        ├── Equipment
        └── Property

LIABILITY      (Credit increases, Debit decreases)
  ├── Current Liabilities
  │     ├── Accounts Payable
  │     └── Short-term Loans
  └── Long-term Liabilities

EQUITY         (Credit increases, Debit decreases)
  ├── Owner's Capital
  └── Retained Earnings

REVENUE        (Credit increases, Debit decreases)
  ├── Sales Revenue
  └── Service Revenue

EXPENSE        (Debit increases, Credit decreases)
  ├── Cost of Goods Sold
  ├── Salaries
  ├── Rent
  └── Utilities
```

### Endpoints
| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/accounts` | List all accounts with balances |
| POST | `/api/accounts` | Create account |
| PUT | `/api/accounts/{id}` | Update account |
| DELETE | `/api/accounts/{id}` | Deactivate (soft) if no transactions |
| GET | `/api/accounts/{id}/transactions` | Transactions for account |
| GET | `/api/accounts/templates` | Default CoA templates by industry |

### Business Rules
- Account code must be unique per tenant
- Accounts with posted transactions cannot be deleted, only deactivated
- Parent account balance = sum of all child account balances
- System accounts (AR, AP, Cash) cannot be deleted
- On tenant creation, default CoA is seeded based on business type

---

## 4. Double-Entry Engine

### Description
The immutable financial ledger. Every financial event produces a balanced journal entry where total debits always equal total credits. This is the source of truth for all financial data.

### Journal Entry Rules
- Every entry must balance: `Σ debits = Σ credits`
- Minimum 2 lines per entry (one debit, one credit)
- Entries are immutable once posted — reverse to correct errors
- Each entry has a unique auto-generated reference number
- Entries can be in Draft status before posting
- Posted entries lock the relevant fiscal period line

### Reversal Mechanics
```
Original Entry (Posted):
  DR  Office Supplies Expense    5,000
  CR  Cash                               5,000

Reversal Entry (Auto-generated):
  DR  Cash                       5,000
  CR  Office Supplies Expense            5,000
  [Reference: REV-2025-001234, Reverses: JE-2025-005678]
```

### Endpoints
| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/journal-entries` | List entries with filters |
| POST | `/api/journal-entries` | Create draft entry |
| POST | `/api/journal-entries/{id}/post` | Post draft entry |
| POST | `/api/journal-entries/{id}/reverse` | Create reversal |
| GET | `/api/journal-entries/{id}` | Entry detail with lines |
| GET | `/api/ledger/trial-balance` | Trial balance report |
| GET | `/api/ledger/general-ledger` | Full general ledger |

---

## 5. Transaction Management

### Description
The day-to-day financial activity layer. Transactions come from bank feeds, manual entry, or imports. They are categorized, assigned to accounts, and form the basis for all AI analysis.

### Transaction Lifecycle
```
Imported/Created → Categorized → Reviewed → Reconciled → Archived
```

### Fields
- Date, Description, Amount, Currency
- Category (auto-assigned or manual)
- Account assignment
- Contact (vendor/customer)
- Bank connection source
- Tags (for custom grouping)
- Attachments (receipts)
- Reconciliation status
- Anomaly score (0-1)
- Notes

### Endpoints
| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/transactions` | Paginated list with filters |
| POST | `/api/transactions` | Manual transaction entry |
| PUT | `/api/transactions/{id}` | Update category/account |
| POST | `/api/transactions/import` | CSV bulk import |
| POST | `/api/transactions/{id}/split` | Split into multiple |
| POST | `/api/transactions/{id}/match` | Match to invoice |
| GET | `/api/transactions/unmatched` | Unreconciled transactions |

### Auto-Categorization Rules Engine
- Rule structure: `IF description CONTAINS "x" AND amount BETWEEN a AND b THEN category = "y"`
- Rules evaluated in priority order
- Learning: user overrides feed back into rules
- ML fallback when no rule matches (trained on historical data)

---

## 6. Invoice Management

### Description
Full accounts receivable and payable lifecycle — from creation to collection. Supports multi-currency, tax, discounts, and payment tracking.

### Invoice Status Flow
```
DRAFT → SENT → VIEWED → PARTIAL → PAID
                              ↘ OVERDUE → WRITTEN_OFF
DRAFT → VOID
```

### Key Fields
- Invoice number (auto-generated: INV-2025-0001)
- Contact (customer/supplier)
- Issue date, due date, payment terms
- Line items: description, quantity, unit price, tax rate
- Subtotal, tax total, discount, grand total
- Currency + exchange rate if multi-currency
- Payment records (partial payments supported)
- Notes and terms

### Endpoints
| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/invoices` | List with status filter |
| POST | `/api/invoices` | Create invoice |
| PUT | `/api/invoices/{id}` | Update draft invoice |
| POST | `/api/invoices/{id}/send` | Send via email |
| POST | `/api/invoices/{id}/pay` | Record payment |
| POST | `/api/invoices/{id}/void` | Void invoice |
| POST | `/api/invoices/{id}/duplicate` | Clone invoice |
| GET | `/api/invoices/{id}/pdf` | Download PDF |
| POST | `/api/invoices/{id}/reminder` | Send payment reminder |
| POST | `/api/invoices/recurring` | Create recurring template |

### Automation
- Auto-send reminder at 7 days before due, on due date, 7 days after
- Auto-status change to OVERDUE at midnight on due date
- Auto-create journal entry on payment recording
- Recurring invoices generated by Hangfire job

---

## 7. Bank Feed Integration

### Description
Automated connection to user's bank accounts for real-time transaction data. Supports Nigerian banks via Mono and international banks via Plaid.

### Supported Providers
| Provider | Markets | Banks |
|----------|---------|-------|
| Mono | Nigeria, Ghana, Kenya | GTB, Access, Zenith, UBA, First Bank, Stanbic, + 50 more |
| Plaid | US, UK, EU, CA | 12,000+ institutions |

### Connection Flow
```
1. User clicks "Connect Bank"
2. Frontend opens Mono Connect / Plaid Link widget
3. User authenticates with their bank
4. Provider returns access token to our backend
5. Token encrypted and stored
6. Initial sync: pull last 90 days of transactions
7. Ongoing sync: every 4 hours via Hangfire
8. Webhooks: real-time updates when available
```

### Endpoints
| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/bank/connect/mono/exchange` | Exchange Mono code for token |
| POST | `/api/bank/connect/plaid/exchange` | Exchange Plaid token |
| GET | `/api/bank/connections` | List connected accounts |
| DELETE | `/api/bank/connections/{id}` | Disconnect account |
| POST | `/api/bank/sync/{connectionId}` | Manual sync trigger |
| GET | `/api/bank/connections/{id}/status` | Sync status + last synced |
| POST | `/api/bank/webhooks/mono` | Mono webhook receiver |
| POST | `/api/bank/webhooks/plaid` | Plaid webhook receiver |

### Sync Rules
- Deduplication by provider transaction ID
- Duplicate detection by (amount + date + description) hash as fallback
- Failed syncs retry 3 times with exponential backoff
- Sync errors surfaced as tenant notifications

---

## 8. AI Chat Interface

### Description
Natural language interface powered by Claude. Users ask questions about their finances and get intelligent, data-backed answers in plain English. The AI has access to all financial data via structured tools.

### Chat Architecture
```
User Message
    ↓
Context Builder (assembles financial snapshot)
    ↓
Claude API (with function calling tools)
    ↓
Tool Execution (if Claude calls a tool)
    ↓
Claude Response (with tool results)
    ↓
Stream to User (SSE)
    ↓
Persist to DB
```

### Example Interactions
```
User: "Can I afford to hire someone at ₦200,000/month?"
AI:   "Based on your current cash position of ₦4.2M and monthly burn rate of
       ₦1.8M, adding a ₦200,000 salary would reduce your runway from 47 days
       to 39 days. I'd recommend waiting until you collect the ₦890,000 
       outstanding from 3 clients first. Want me to send them reminders?"

User: "Which of my clients makes me the most money?"
AI:   "Your most profitable client is TechBridge Ltd — they generated 
       ₦1.4M in revenue last quarter with only ₦180,000 in service costs,
       giving you a 87% margin. Your least profitable is QuickFix Motors 
       at 23% margin due to high delivery costs. Want a full breakdown?"
```

### Endpoints
| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/ai/chat/sessions` | List sessions |
| POST | `/api/ai/chat/sessions` | Create session |
| DELETE | `/api/ai/chat/sessions/{id}` | Delete session |
| GET | `/api/ai/chat/sessions/{id}/messages` | Message history |
| POST | `/api/ai/chat/sessions/{id}/messages` | Send message (SSE stream) |

### Rate Limits by Plan
| Plan | AI Queries/Month |
|------|-----------------|
| Free | 20 |
| Starter | 200 |
| Growth | Unlimited |
| Business | Unlimited |
| Enterprise | Unlimited |

---

## 9. Daily Briefing Engine

### Description
Every morning, every user gets a personalized AI-generated financial summary — no dashboards to interpret, no reports to run. Just a clear, conversational briefing written by AI.

### Briefing Structure
```
📊 Good morning, [Name]. Here's your financial picture for [Date]:

CASH POSITION
Your balance across 3 accounts is ₦6.2M — up ₦340K from yesterday.

ACTION NEEDED
• 3 invoices totalling ₦1.2M are overdue. Longest outstanding: Sunshine 
  Bakery at 34 days.
• Your June rent of ₦450,000 is due in 4 days.

YESTERDAY'S ACTIVITY  
• 12 transactions totalling ₦287,000 in expenses
• 2 payments received: ₦500,000 from Zenith Traders

OUTLOOK
At current burn rate, you have 52 days of runway. This improved by 5 days 
compared to last week.

ONE THING TO DO TODAY
Chase payment from Sunshine Bakery — they've received 2 reminders with 
no response. Consider a direct call.
```

### Endpoints
| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/ai/briefing/today` | Today's briefing |
| GET | `/api/ai/briefing/history` | Past 30 briefings |
| PATCH | `/api/ai/briefing/{id}/read` | Mark as read |

---

## 10. Cash Flow Forecasting

### Description
Predicts the business's cash position over the next 30, 60, and 90 days using historical patterns, outstanding invoices, and known upcoming expenses. Proactively identifies cash gaps before they happen.

### Forecasting Model Inputs
- Bank account balances (current)
- Outstanding invoices (with probability weights by age)
- Recurring expenses (detected automatically)
- Recurring revenue (detected automatically)
- Known upcoming payments (loan repayments, tax deadlines)
- Seasonal patterns (e.g., always slow in January)

### Output Format
```json
{
  "currentBalance": 6200000,
  "currency": "NGN",
  "forecastDays": 90,
  "projections": [
    { "date": "2025-02-01", "balance": 5800000, "confidence": 0.92 },
    { "date": "2025-02-15", "balance": 4200000, "confidence": 0.85 },
    ...
  ],
  "cashGaps": [
    {
      "date": "2025-03-08",
      "projectedBalance": -340000,
      "severity": "critical",
      "suggestion": "Collect ₦890K from 3 overdue clients before this date"
    }
  ],
  "assumptions": ["Recurring salary on 25th", "3 invoices at 60% collection probability"],
  "narrative": "Your cash position looks healthy for the next 45 days..."
}
```

### Endpoints
| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/ai/forecast` | Get forecast (horizon=30\|60\|90) |
| POST | `/api/ai/forecast/refresh` | Force recalculate |
| GET | `/api/ai/forecast/assumptions` | View forecast assumptions |

---

## 11. Anomaly Detection & Alerts

### Description
AI-powered monitoring that watches every transaction and financial pattern, flagging unusual activity proactively. The system learns what "normal" looks like for each business.

### Alert Types
| Alert | Trigger | Severity |
|-------|---------|---------|
| Unusual large transaction | Amount > 3x category average | Warning |
| Duplicate transaction | Same amount + vendor within 48h | Warning |
| New payee large payment | First time vendor, amount > threshold | Warning |
| Cash runway critical | Runway < 30 days | Critical |
| Invoice severely overdue | > 60 days past due | Warning |
| Revenue drop | MoM decline > 20% | Warning |
| Expense spike | Category up > 40% MoM | Info |
| Potential fraud | ML score > 0.85 | Critical |
| Budget exceeded | Expense > budget by 10%+ | Warning |
| Tax deadline approaching | 14 days to filing | Info |

### Endpoints
| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/ai/alerts` | List alerts (unresolved first) |
| GET | `/api/ai/alerts/{id}` | Alert detail |
| PATCH | `/api/ai/alerts/{id}/resolve` | Mark resolved |
| PATCH | `/api/ai/alerts/{id}/dismiss` | Dismiss (false positive) |
| GET | `/api/ai/alerts/settings` | Alert preferences |
| PUT | `/api/ai/alerts/settings` | Update preferences |

---

## 12. Scenario Planning

### Description
What-if financial modeling in plain English. Users describe a business decision and the AI calculates the financial impact and gives a recommendation.

### Scenario Types

**Hiring Scenario**
- Input: Number of hires, monthly salary per person, start date
- Output: Updated 90-day cash flow, impact on runway, break-even point

**Price Change Scenario**
- Input: Revenue category, % increase or decrease
- Output: Projected revenue impact, effect on margins, risk assessment

**Loan Scenario**
- Input: Loan amount, interest rate, repayment period
- Output: Monthly repayment, total cost, cash flow with loan vs without

**New Expense Scenario**
- Input: Expense name, amount, frequency, start date
- Output: Cumulative cost over 12 months, runway impact

**Payment Delay Scenario**
- Input: Client name or amount, expected delay in days
- Output: Cash gap risk, suggested mitigation

### Endpoints
| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/ai/scenarios` | List saved scenarios |
| POST | `/api/ai/scenarios` | Run new scenario |
| GET | `/api/ai/scenarios/{id}` | Scenario detail + results |
| DELETE | `/api/ai/scenarios/{id}` | Delete scenario |
| POST | `/api/ai/scenarios/compare` | Compare two scenarios |

---

## 13. Profitability Intelligence

### Description
Breaks down financial performance by multiple dimensions — not just overall profit, but profit per client, per product/service, per project, per branch. Reveals where money is really made and lost.

### Dimensions
- **Client/Customer** — who is your most/least profitable customer?
- **Product/Service** — which offering has the best margin?
- **Project** — are your projects profitable after all costs?
- **Branch/Location** — which location performs best?
- **Time** — is profitability improving or declining?

### Endpoints
| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/analytics/profitability` | Overall profitability |
| GET | `/api/analytics/profitability/by-client` | Per-client breakdown |
| GET | `/api/analytics/profitability/by-product` | Per-product breakdown |
| GET | `/api/analytics/profitability/by-project` | Per-project breakdown |
| GET | `/api/analytics/profitability/trends` | Monthly profitability trend |
| GET | `/api/analytics/kpis` | Key financial KPIs dashboard data |

---

## 14. Financial Reports Engine

### Description
Standard financial statements generated on-demand for any date range, with comparison periods, PDF export, and audit-ready formatting.

### Reports
| Report | Description |
|--------|-------------|
| Profit & Loss | Revenue, expenses, net profit for a period |
| Balance Sheet | Assets, liabilities, equity at a point in time |
| Cash Flow Statement | Operating, investing, financing activities |
| AR Aging | Outstanding invoices grouped by age |
| AP Aging | Outstanding payables grouped by age |
| General Ledger | Complete transaction history per account |
| Trial Balance | All account balances at a point in time |
| Tax Summary | VAT/GST collected and paid |

### Endpoints
| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/reports/profit-loss` | P&L statement |
| GET | `/api/reports/balance-sheet` | Balance sheet |
| GET | `/api/reports/cash-flow` | Cash flow statement |
| GET | `/api/reports/ar-aging` | AR aging |
| GET | `/api/reports/ap-aging` | AP aging |
| GET | `/api/reports/general-ledger` | General ledger |
| GET | `/api/reports/trial-balance` | Trial balance |
| GET | `/api/reports/tax-summary` | Tax report |
| POST | `/api/reports/export` | Export to PDF/CSV/Excel |

---

## 15. OCR & Document Processing

### Description
Upload a receipt or invoice photo, and the system automatically extracts the data, creates a transaction or invoice, and awaits user confirmation.

### Pipeline
```
Upload image/PDF
    ↓
Azure Document Intelligence (extract fields)
    ↓
Confidence scoring
    ↓
Map to transaction/invoice schema
    ↓
Return draft for user confirmation
    ↓
User confirms → posted to ledger
```

### Endpoints
| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/documents/receipts` | Upload receipt for OCR |
| POST | `/api/documents/invoices` | Upload invoice for OCR |
| GET | `/api/documents/{id}` | Extracted data + confidence |
| POST | `/api/documents/{id}/confirm` | Confirm and create transaction |
| POST | `/api/documents/{id}/reject` | Discard extraction |

---

## 16. Subscription & Billing

### Description
SaaS subscription management with support for multiple payment providers, plan upgrades/downgrades, usage metering, and revenue recognition.

### Plans
| Plan | Price | AI Queries | Bank Connections | Users |
|------|-------|-----------|-----------------|-------|
| Free | $0 | 20/month | 1 | 1 |
| Starter | $19/month | 200/month | 2 | 2 |
| Growth | $49/month | Unlimited | 5 | 5 |
| Business | $149/month | Unlimited | Unlimited | 15 |
| Enterprise | Custom | Unlimited | Unlimited | Unlimited |

### Endpoints
| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/billing/plans` | Available plans |
| GET | `/api/billing/subscription` | Current subscription |
| POST | `/api/billing/subscribe` | Start subscription |
| POST | `/api/billing/upgrade` | Change plan |
| POST | `/api/billing/cancel` | Cancel subscription |
| GET | `/api/billing/invoices` | Billing history |
| POST | `/api/billing/webhooks/paystack` | Paystack webhook |
| POST | `/api/billing/webhooks/stripe` | Stripe webhook |

---

## 17. Notifications

### Description
Multi-channel notification system for alerts, briefings, invoice reminders, and system events. Respects user preferences per notification type.

### Channels
- **In-app** — SignalR real-time push to dashboard
- **Email** — Resend (transactional templates)
- **SMS** — Termii (Nigeria/Africa), Twilio (Global)
- **WhatsApp** — WhatsApp Business API via Twilio

### Notification Types
| Type | Default Channel | Configurable |
|------|----------------|-------------|
| Daily briefing | Email + In-app | Yes |
| Critical alert | All channels | Yes |
| Invoice overdue | Email | Yes |
| Payment received | In-app | Yes |
| Cash gap warning | Email + SMS | Yes |
| Bank sync failure | Email | Yes |

### Endpoints
| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/notifications` | Unread notifications |
| PATCH | `/api/notifications/{id}/read` | Mark read |
| PATCH | `/api/notifications/read-all` | Mark all read |
| GET | `/api/notifications/preferences` | Notification settings |
| PUT | `/api/notifications/preferences` | Update settings |

---

## 18. Audit & Compliance

### Description
Complete, tamper-evident audit trail of every action performed in the system. Required for regulatory compliance and fraud investigation.

### What Is Logged
- Every create, update, delete operation with before/after values
- Every login, logout, failed login attempt
- Every AI chat message and tool call
- Every bank sync and data import
- Every export and report generation
- Every permission change

### Audit Log Fields
```
id, tenant_id, user_id, user_email, action, entity_type, entity_id,
old_values (JSON), new_values (JSON), ip_address, user_agent,
request_id, created_at
```

### Endpoints
| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/audit/logs` | Paginated audit logs (admin) |
| GET | `/api/audit/logs/{entityType}/{entityId}` | Entity history |
| GET | `/api/audit/export` | Export audit log CSV |

### Compliance Notes
- Audit logs are append-only — no update or delete endpoints
- Logs retained for minimum 7 years (financial record requirement)
- GDPR: user data deletion anonymizes logs but retains financial records
- Nigerian NDPR compliance: data residency options available
