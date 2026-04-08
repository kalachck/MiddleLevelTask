namespace DataProcessor.Domain.Entities.Abstract;

public interface IBaseEntity
{
    public Guid Id { get; }
    public DateTime CreatedAt { get; }
}

public abstract record BaseEntity : IBaseEntity
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
}
