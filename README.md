# Fynly - AI-Powered CFO Application

A modern, multi-tenant SaaS platform for financial operations built with .NET 10 and advanced AI capabilities.

## 🎯 Overview

Fynly is an intelligent financial operations platform that combines robust accounting features with AI-driven analytics, bank reconciliation, and predictive insights. Designed for modern financial teams, it provides real-time visibility into financial health, automates reconciliation processes, and uses machine learning for anomaly detection and financial predictions.

### Key Capabilities
- **Multi-Tenant Architecture**: Secure data isolation for multiple organizations
- **Ledger Management**: Chart of accounts, journal entries, trial balances
- **Bank Integration**: OAuth2 integration with Flutterwave for automatic transaction sync
- **Reconciliation**: Intelligent matching and reconciliation of bank transactions
- **AI Analytics**: Anomaly detection, financial predictions, health scoring
- **Background Jobs**: Hangfire-powered recurring jobs for batch processing
- **Real-time Alerts**: Anomaly and predictive alerts with configurable thresholds
- **RESTful API**: Scalar/Swagger documentation with comprehensive endpoints

## 🛠️ Tech Stack

### Backend
- **.NET 10** with C# 14.0
- **ASP.NET Core** - Web framework
- **Entity Framework Core 10** - ORM with SQL Server/PostgreSQL support
- **MediatR** - CQRS command/query pattern
- **JWT Authentication** - Secure token-based auth
- **Hangfire** - Background job processing

### Data & Caching
- **SQL Server** (Primary) or **PostgreSQL** (Alternative)
- **Redis** - Distributed caching
- **Npgsql/SQL Server Providers** - Database adapters

### Integration
- **Flutterwave OAuth2** - Bank connection provider
- **HttpClient** - External API communication

### DevOps
- **Docker** - Containerization
- **Docker Compose** - Multi-service orchestration
- **Serilog** - Structured logging

## 📋 Prerequisites

### Local Development
- **.NET 10 SDK** - [Download](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- **SQL Server 2019+** or **PostgreSQL 16+**
- **Redis 7+**
- **Visual Studio 2022** (Community or better)

### Docker Setup
- **Docker Desktop** - [Download](https://www.docker.com/products/docker-desktop)
- **Docker Compose** - Included with Docker Desktop

### Optional
- **SQL Server Management Studio (SSMS)** - For database management
- **Postman/Insomnia** - API testing

## 🚀 Quick Start

### 1. Clone Repository
```bash
git clone https://github.com/Callmesammy/Fynly.git
cd Fynly
```

### 2. Local Development Setup

#### Option A: SQL Server (Recommended)
```bash
# Create databases
sqlcmd -S localhost -E -Q "CREATE DATABASE aicfo;"
sqlcmd -S localhost -E -Q "CREATE DATABASE aicfo_hangfire;"

# Apply migrations
dotnet ef database update --project Infrastructure --startup-project Fynly

# Run application
dotnet run --project Fynly
```

#### Option B: PostgreSQL
Update `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=aicfo;Username=postgres;Password=postgres;",
    "HangfireConnection": "Host=localhost;Port=5432;Database=aicfo_hangfire;Username=postgres;Password=postgres;"
  }
}
```

Then:
```bash
# Create databases (PostgreSQL CLI)
createdb -U postgres aicfo
createdb -U postgres aicfo_hangfire

# Apply migrations
dotnet ef database update --project Infrastructure --startup-project Fynly

# Run application
dotnet run --project Fynly
```

### 3. Docker Setup (Recommended)
```bash
# Start all services (PostgreSQL, Redis, API)
docker-compose up -d

# Verify services
docker ps

# Check API health
curl http://localhost:5000/health
```

API will be available at: **http://localhost:5000**

## ⚙️ Configuration

### Environment Files

#### appsettings.Development.json
```json
{
  "Jwt": {
    "Secret": "your-super-secret-key-min-32-characters-long!!",
    "Issuer": "ai-cfo",
    "Audience": "ai-cfo-api"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=aicfo;Trusted_Connection=true;Encrypt=false;",
    "HangfireConnection": "Server=localhost;Database=aicfo_hangfire;Trusted_Connection=true;Encrypt=false;"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}
```

### Key Configuration Variables
| Variable | Purpose | Example |
|----------|---------|---------|
| `Jwt:Secret` | JWT signing key (min 32 chars) | `your-super-secret-key...` |
| `Jwt:Issuer` | Token issuer | `ai-cfo` |
| `Jwt:Audience` | Token audience | `ai-cfo-api` |
| `ConnectionStrings:DefaultConnection` | Main database | `Server=localhost;...` |
| `Redis:ConnectionString` | Cache connection | `localhost:6379` |

## 📚 API Documentation

### Access Swagger/Scalar
- **Swagger UI**: http://localhost:5000/swagger
- **Scalar Docs**: http://localhost:5000/scalar/v1

### Core Endpoints

#### Authentication
```bash
# Register
POST /api/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "firstName": "John",
  "lastName": "Doe"
}

# Login
POST /api/auth/login
{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

#### Ledger
```bash
# Create Chart of Accounts
POST /api/ledger/chart-of-accounts
Authorization: Bearer {token}

# Record Journal Entry
POST /api/ledger/journal-entries
Authorization: Bearer {token}

# Get Trial Balance
GET /api/ledger/trial-balance
Authorization: Bearer {token}
```

#### Bank Integration
```bash
# Initiate Bank Connection
POST /api/bank/connect
Authorization: Bearer {token}

# Sync Bank Transactions
POST /api/bank/sync
Authorization: Bearer {token}
```

#### Reconciliation
```bash
# Start Reconciliation Session
POST /api/reconciliation/start
Authorization: Bearer {token}

# Get Unmatched Items
GET /api/reconciliation/unmatched
Authorization: Bearer {token}
```

#### AI Analytics
```bash
# Get Anomalies
GET /api/analytics/anomalies
Authorization: Bearer {token}

# Get Predictions
GET /api/analytics/predictions
Authorization: Bearer {token}

# Get Health Score
GET /api/analytics/health-score
Authorization: Bearer {token}
```

## 📁 Project Structure

```
Fynly/
├── Domain/                          # Core business logic
│   ├── Entities/                   # Entity models (User, JournalEntry, etc.)
│   ├── ValueObjects/               # Value objects (Currency, Money, etc.)
│   ├── Rules/                      # Business rules (AccountingRulesEngine)
│   └── Abstractions/               # Base classes (Entity)
│
├── Application/                     # Use cases & business logic
│   ├── Features/                   # CQRS commands and queries
│   │   ├── Auth/                   # Authentication features
│   │   ├── Ledger/                 # Accounting features
│   │   ├── Bank/                   # Bank integration features
│   │   ├── Reconciliation/         # Reconciliation features
│   │   ├── AI/                     # Analytics features
│   │   └── Alerts/                 # Alert management
│   ├── Common/                     # Service interfaces
│   ├── Dtos/                       # Data transfer objects
│   └── Behaviors/                  # MediatR behaviors (validation, logging)
│
├── Infrastructure/                  # Data access & external services
│   ├── Persistence/                # EF Core DbContext & configurations
│   │   ├── Configurations/         # Entity type configurations
│   │   └── Migrations/             # Database migrations
│   ├── Services/                   # Service implementations
│   │   ├── AuthService.cs          # Authentication logic
│   │   ├── LedgerService.cs        # Ledger operations
│   │   ├── BankService.cs          # Bank integration
│   │   ├── ReconciliationService.cs# Reconciliation logic
│   │   ├── TenantContext.cs        # Multi-tenancy context
│   │   └── AI Services/            # ML-powered services
│   ├── BankIntegration/            # External bank providers
│   ├── Jobs/                       # Hangfire background jobs
│   └── GlobalUsings.cs             # Global namespace declarations
│
├── Fynly/                          # ASP.NET Core API
│   ├── Controllers/                # API endpoints
│   ├── Middleware/                 # Custom middleware
│   ├── Program.cs                  # Service registration & configuration
│   ├── appsettings.json           # Configuration
│   ├── appsettings.Development.json # Development secrets
│   └── Properties/launchSettings.json
│
├── TestFynly/                      # Unit & integration tests
│   ├── Features/                   # Feature tests
│   ├── Services/                   # Service tests
│   ├── Entities/                   # Entity tests
│   └── ValueObjects/               # Value object tests
│
├── docker-compose.yml              # Multi-service orchestration
├── Dockerfile                      # API container image
└── README.md                       # This file
```

## 🗄️ Database Schema

### Multi-Tenancy
All entities inherit from `Entity` base class with:
- `Id`: Unique identifier (GUID)
- `TenantId`: Multi-tenant isolation key
- `CreatedAt`, `UpdatedAt`: Audit timestamps
- `IsDeleted`: Soft delete support

### Core Entities
- **Users**: Authentication & authorization
- **ChartOfAccounts**: Account hierarchy
- **JournalEntries**: Transaction records
- **AccountBalances**: Aggregated balances
- **BankConnections**: OAuth connections
- **BankTransactions**: Synced transactions
- **ReconciliationMatches**: Matched items
- **Alerts**: Anomaly & predictive alerts
- **HealthReports**: Financial health snapshots

## 🔄 EF Core Migrations

### Create New Migration
```bash
dotnet ef migrations add MigrationName --project Infrastructure --startup-project Fynly
```

### Apply Migrations
```bash
dotnet ef database update --project Infrastructure --startup-project Fynly
```

### Revert Last Migration
```bash
dotnet ef migrations remove --project Infrastructure --startup-project Fynly
```

### Generate SQL Script
```bash
dotnet ef migrations script --project Infrastructure --startup-project Fynly
```

## 🧪 Testing

### Run All Tests
```bash
dotnet test
```

### Run Specific Test Project
```bash
dotnet test TestFynly/TestFynly.csproj
```

### Run with Coverage
```bash
dotnet test /p:CollectCoverage=true
```

## 🐳 Docker Commands

### Start Services
```bash
# Start all services in background
docker-compose up -d

# Start with logs
docker-compose up

# Start specific service
docker-compose up -d postgres
```

### Stop Services
```bash
docker-compose down

# Remove volumes (WARNING: deletes data)
docker-compose down -v
```

### View Logs
```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f api
docker-compose logs -f postgres
docker-compose logs -f redis
```

### Access Container Shell
```bash
docker exec -it aicfo-postgres psql -U postgres -d aicfo
docker exec -it aicfo-redis redis-cli
docker exec -it fynly bash
```

## 🔐 Security Features

- **JWT Authentication**: Token-based authentication with configurable expiry
- **Multi-Tenancy**: Automatic data isolation per tenant
- **Role-Based Access Control**: Authorization policies
- **CORS Configuration**: Configurable allowed origins
- **Secure Secrets Management**: Environment-based configuration
- **Global Query Filters**: Automatic tenant filtering on all queries
- **Soft Delete**: Logical deletion with data recovery
- **Audit Trails**: CreatedAt/UpdatedAt timestamp tracking

## 🚢 Deployment

### Environment Variables (Production)
```bash
ASPNETCORE_ENVIRONMENT=Production
Jwt__Secret=your-production-secret-key
ConnectionStrings__DefaultConnection=your-prod-connection-string
Redis__ConnectionString=your-prod-redis
```

### Docker Deployment
```bash
# Build image
docker build -t fynly:latest .

# Push to registry
docker push your-registry/fynly:latest

# Deploy
docker run -d \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e Jwt__Secret=$JWT_SECRET \
  -e ConnectionStrings__DefaultConnection=$DB_CONNECTION \
  -p 8080:8080 \
  your-registry/fynly:latest
```

## 📊 Performance Optimization

- **Entity Framework Core Lazy Loading**: Deferred query execution
- **Global Query Filters**: Efficient multi-tenancy enforcement
- **Redis Caching**: Distributed cache for frequently accessed data
- **Connection Pooling**: SQL Server connection pool management
- **Hangfire Background Jobs**: Non-blocking long-running operations
- **Retry Policies**: Automatic retry with exponential backoff

## 🐛 Troubleshooting

### Connection Issues
```bash
# Test SQL Server
sqlcmd -S localhost -E -Q "SELECT 1"

# Test PostgreSQL
psql -h localhost -U postgres -d postgres -c "SELECT 1"

# Test Redis
redis-cli ping
```

### Migration Issues
```bash
# Check migration status
dotnet ef migrations list --project Infrastructure --startup-project Fynly

# Reset database (DESTRUCTIVE)
dotnet ef database drop -f --project Infrastructure --startup-project Fynly
dotnet ef database update --project Infrastructure --startup-project Fynly
```

### Port Conflicts
- API: 5000/5001 (local), 8080/8081 (Docker)
- PostgreSQL: 5432
- Redis: 6379
- SQL Server: 1433

## 📝 Contributing

1. Fork the repository
2. Create feature branch: `git checkout -b feature/amazing-feature`
3. Commit changes: `git commit -m 'Add amazing feature'`
4. Push to branch: `git push origin feature/amazing-feature`
5. Open Pull Request

## 📄 License

This project is licensed under the MIT License - see LICENSE file for details.

## 📞 Support

For issues, questions, or suggestions:
- **GitHub Issues**: [Open an issue](https://github.com/Callmesammy/Fynly/issues)
- **Documentation**: Check the `/docs` folder for detailed guides
- **Email**: [Your contact info]

## 🎓 Learn More

- [.NET 10 Documentation](https://learn.microsoft.com/en-us/dotnet/)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [MediatR Documentation](https://github.com/jbogard/MediatR)
- [Hangfire Documentation](https://www.hangfire.io/)
- [Docker Documentation](https://docs.docker.com/)

---

**Happy coding! 🚀**

Last Updated: 2024
