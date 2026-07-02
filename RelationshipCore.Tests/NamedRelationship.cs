namespace RelationshipCore.Tests;

/// <summary>Простейшая IRelationship для тестов графа — сравнение по строковому типу.</summary>
internal sealed class NamedRelationship : IRelationship
{
    public NamedRelationship(string type)
    {
        Type = type;
    }

    public string Type { get; }

    public bool Matches(IRelationship other) => other is NamedRelationship r && r.Type == Type;

    public override string ToString() => Type;
}
