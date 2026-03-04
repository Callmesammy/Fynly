namespace AiCFO.Domain.Abstractions;

/// <summary>
/// Base class for all domain entities.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected init; }
    public Guid TenantId { get; protected init; }
    public DateTime CreatedAt { get; protected init; }
    public Guid CreatedBy { get; protected init; }
    public DateTime? UpdatedAt { get; protected set; }
    public Guid? UpdatedBy { get; protected set; }
    public bool IsDeleted { get; protected set; }
    public DateTime? DeletedAt { get; protected set; }
    public Guid? DeletedBy { get; protected set; }

    private readonly List<IDomainEvent> _domainEvents = [];

    protected Entity() { }

    protected Entity(Guid id, Guid tenantId, Guid createdBy)
    {
        Id = id;
        TenantId = tenantId;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
    }

    public IReadOnlyList<IDomainEvent> GetDomainEvents() => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}

/// <summary>
/// Base class for aggregate roots (entities that own other entities).
/// </summary>
public abstract class AggregateRoot : Entity
{
    protected AggregateRoot() { }

    protected AggregateRoot(Guid id, Guid tenantId, Guid createdBy)
        : base(id, tenantId, createdBy) { }
}

/// <summary>
/// Marker interface for domain events.
/// </summary>
public interface IDomainEvent
{
    Guid AggregateId { get; }
    Guid TenantId { get; }
    DateTime OccurredAt { get; }
}

/// <summary>
/// Base class for domain events.
/// </summary>
public abstract class DomainEvent : IDomainEvent
{
    public Guid AggregateId { get; protected set; }
    public Guid TenantId { get; protected set; }
    public DateTime OccurredAt { get; protected init; } = DateTime.UtcNow;

    protected DomainEvent(Guid aggregateId, Guid tenantId)
    {
        AggregateId = aggregateId;
        TenantId = tenantId;
    }
}
