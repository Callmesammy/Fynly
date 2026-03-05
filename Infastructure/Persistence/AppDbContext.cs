namespace AiCFO.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core DbContext for AI CFO application.
/// Implements multi-tenancy through global query filters on the Entity base class.
/// </summary>
public class AppDbContext : DbContext
{
    private readonly ITenantContext? _tenantContext;

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    // Core entities
    public DbSet<User> Users { get; set; } = null!;

    // Ledger entities
    public DbSet<ChartOfAccounts> ChartsOfAccounts { get; set; } = null!;
    public DbSet<ChartAccountEntry> ChartAccountEntries { get; set; } = null!;
    public DbSet<JournalEntry> JournalEntries { get; set; } = null!;
    public DbSet<JournalLine> JournalLines { get; set; } = null!;
    public DbSet<AccountBalance> AccountBalances { get; set; } = null!;

    // Bank entities
    public DbSet<BankConnection> BankConnections { get; set; } = null!;
    public DbSet<BankAccount> BankAccounts { get; set; } = null!;
    public DbSet<BankTransaction> BankTransactions { get; set; } = null!;

    // Reconciliation entities
    public DbSet<ReconciliationMatch> ReconciliationMatches { get; set; } = null!;
    public DbSet<ReconciliationAuditLog> ReconciliationAuditLogs { get; set; } = null!;
    public DbSet<ReconciliationSession> ReconciliationSessions { get; set; } = null!;
    public DbSet<UnmatchedBankTransaction> UnmatchedBankTransactions { get; set; } = null!;
    public DbSet<UnmatchedJournalEntry> UnmatchedJournalEntries { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply global query filter for multi-tenancy
        // Automatically filters all queries by current tenant's TenantId
        ApplyGlobalQueryFilters(modelBuilder);

        // Register ledger configurations
        modelBuilder.ApplyConfiguration(new ChartOfAccountsConfiguration());
        modelBuilder.ApplyConfiguration(new ChartAccountEntryConfiguration());
        modelBuilder.ApplyConfiguration(new JournalEntryConfiguration());
        modelBuilder.ApplyConfiguration(new JournalLineConfiguration());
        modelBuilder.ApplyConfiguration(new AccountBalanceConfiguration());

        // Register bank configurations
        modelBuilder.ApplyConfiguration(new BankConnectionConfiguration());
        modelBuilder.ApplyConfiguration(new BankAccountConfiguration());
        modelBuilder.ApplyConfiguration(new BankTransactionConfiguration());

        // Register reconciliation configurations
        modelBuilder.ApplyConfiguration(new ReconciliationMatchConfiguration());
        modelBuilder.ApplyConfiguration(new ReconciliationAuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new ReconciliationSessionConfiguration());
        modelBuilder.ApplyConfiguration(new UnmatchedBankTransactionConfiguration());
        modelBuilder.ApplyConfiguration(new UnmatchedJournalEntryConfiguration());

        // Configure entity mappings and relationships
        ConfigureEntities(modelBuilder);
    }

    /// <summary>
    /// Applies global query filters to enforce multi-tenancy data isolation.
    /// Ensures queries only return data for the current tenant.
    /// </summary>
    private void ApplyGlobalQueryFilters(ModelBuilder modelBuilder)
    {
        // Get all entity types that inherit from Entity
        var entityTypes = modelBuilder.Model.GetEntityTypes()
            .Where(t => t.ClrType.IsAssignableTo(typeof(Entity)))
            .ToList();

        foreach (var entityType in entityTypes)
        {
            // Apply filter: e => e.TenantId == currentTenantId
            // This automatically filters all queries to current tenant
            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var tenantIdProperty = Expression.Property(parameter, "TenantId");
            var currentTenantId = Expression.Constant(_tenantContext?.TenantId ?? Guid.Empty);
            var comparison = Expression.Equal(tenantIdProperty, currentTenantId);
            var lambda = Expression.Lambda(comparison, parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }

    /// <summary>
    /// Configures entity mappings, relationships, and database constraints.
    /// </summary>
    private void ConfigureEntities(ModelBuilder modelBuilder)
    {
        // Configure soft delete behavior
        var entityTypes = modelBuilder.Model.GetEntityTypes()
            .Where(t => t.ClrType.IsAssignableTo(typeof(Entity)))
            .ToList();

        foreach (var entityType in entityTypes)
        {
            // Table naming convention (e.g., User → users)
            var tableName = entityType.ClrType.Name.ToLower();
            modelBuilder.Entity(entityType.ClrType).ToTable(tableName + "s");

            // Add soft delete query filter
            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var isDeletedProperty = Expression.Property(parameter, "IsDeleted");
            var notDeleted = Expression.Equal(isDeletedProperty, Expression.Constant(false));
            var lambda = Expression.Lambda(notDeleted, parameter);

            // Combine with existing filters (though only if not already applied)
            // This will be improved with a dedicated query filter configuration
        }
    }

    /// <summary>
    /// Override SaveChanges to automatically set audit fields (CreatedAt, UpdatedAt, etc.)
    /// </summary>
    public override int SaveChanges()
    {
        SetAuditFields();
        return base.SaveChanges();
    }

    /// <summary>
    /// Override SaveChangesAsync to automatically set audit fields (CreatedAt, UpdatedAt, etc.)
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetAuditFields();
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Automatically sets audit fields on tracked entities.
    /// </summary>
    private void SetAuditFields()
    {
        var entries = ChangeTracker.Entries<Entity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                // Set creation audit fields
                entry.Property("CreatedAt").CurrentValue = DateTime.UtcNow;
                if (_tenantContext is not null)
                {
                    entry.Property("CreatedBy").CurrentValue = _tenantContext.UserId;
                    entry.Property("TenantId").CurrentValue = _tenantContext.TenantId;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                // Set update audit fields
                entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
                if (_tenantContext is not null)
                {
                    entry.Property("UpdatedBy").CurrentValue = _tenantContext.UserId;
                }
            }
            else if (entry.State == EntityState.Deleted)
            {
                // Convert delete to soft delete
                entry.State = EntityState.Modified;
                entry.Property("IsDeleted").CurrentValue = true;
                entry.Property("DeletedAt").CurrentValue = DateTime.UtcNow;
                if (_tenantContext is not null)
                {
                    entry.Property("DeletedBy").CurrentValue = _tenantContext.UserId;
                }
            }
        }
    }
}
