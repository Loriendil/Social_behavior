namespace RelationshipCore.Edges;

/// <summary>Стандартная реализация IEdge: направленное ребро с изменяемым атрибутом Relationship.</summary>
public class Edge : IEdge
{
    public Edge(INode from, INode to, IRelationship relationship)
    {
        From = from;
        To = to;
        Relationship = relationship;
    }

    public INode From { get; }

    public INode To { get; }

    public IRelationship Relationship { get; set; }
}
