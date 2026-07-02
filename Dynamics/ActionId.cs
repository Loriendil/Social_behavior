namespace RelationshipCore.Dynamics;

/// <summary>Идентификатор действия из словаря игры (аналог EntityId, но для действий, а не сущностей).</summary>
public readonly struct ActionId : IEquatable<ActionId>
{
    public ActionId(int value)
    {
        Value = value;
    }

    public int Value { get; }

    public bool Equals(ActionId other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is ActionId other && Equals(other);

    public override int GetHashCode() => Value;

    public static bool operator ==(ActionId left, ActionId right) => left.Equals(right);

    public static bool operator !=(ActionId left, ActionId right) => !left.Equals(right);
}
