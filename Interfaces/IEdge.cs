namespace RelationshipCore;

/// <summary>Ребро: направленная связь от одной сущности к другой.</summary>
public interface IEdge
{
    INode From { get; }

    INode To { get; }

    IRelationship Relationship { get; set; }
}
