namespace CollabEditor.Domain.Common;

public abstract class Entity<TId> : IEquatable<Entity<TId>>
{
    public TId Id { get; protected set; }
    
    protected Entity(TId id)
    {
        Id = id;
    }
    
    public bool Equals(Entity<TId>? other)
    {
        if (other is null)
        {
            return false;
        }
        
        return ReferenceEquals(this, other) || EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }
    
    public override bool Equals(object? obj)
    {
        return obj is Entity<TId> entity && Equals(entity);
    }
    
    public override int GetHashCode() => Id?.GetHashCode() ?? 0;
    
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        return Equals(left, right);
    }
    
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !Equals(left, right);
    }
}