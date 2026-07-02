using RelationshipCore.Edges;

namespace RelationshipCore.Graphs;

/// <summary>
/// Граф с семантикой прямых и косвенных рёбер (раздел 3.1 диссертации O'Connor).
/// Прямое ребро хранится у узла edge.From — это собственное отношение узла.
/// Косвенное ребро хранится у узла, отличного от edge.From, — это предположение
/// узла-владельца об отношении двух других сущностей (слухи, обман, неполное знание).
/// </summary>
public class DeepGraph : Graph
{
    /// <summary>Есть ли прямое ребро from -&gt; to.</summary>
    public bool HasEdge(INode from, INode to) => GetEdge(from, to) is not null;

    /// <summary>Есть ли у owner (прямое или косвенное) ребро from -&gt; to.</summary>
    public bool NodeHasEdge(INode owner, INode from, INode to) => FindEdge(owner, from, to) is not null;

    /// <summary>
    /// Добавляет ребро в список owner. Если у owner уже есть ребро с тем же From/To,
    /// обновляет его Relationship вместо добавления дубликата.
    /// </summary>
    public void AddEdge(INode owner, IEdge edge)
    {
        var existing = FindEdge(owner, edge.From, edge.To);
        if (existing is not null)
        {
            existing.Relationship = edge.Relationship;
        }
        else
        {
            MutableEdgesOf(owner).Add(edge);
        }
    }

    /// <summary>Добавляет прямое ребро — сохраняет его в списке узла edge.From.</summary>
    public void AddDirectEdge(IEdge edge) => AddEdge(edge.From, edge);

    /// <summary>
    /// Добавляет пару встречных прямых рёбер from-&gt;to и to-&gt;from. Используется, когда связь
    /// по своей природе взаимна, но каждая сторона хранит и может независимо менять свою копию
    /// отношения (Relationship несимметричен, см. SocialRelation в модели Ochs).
    /// </summary>
    public void AddCommonEdge(INode from, INode to, IRelationship relationship, IRelationship reverseRelationship)
    {
        AddDirectEdge(new Edge(from, to, relationship));
        AddDirectEdge(new Edge(to, from, reverseRelationship));
    }

    /// <summary>Все рёбра, хранящиеся у узла (и прямые, и косвенные).</summary>
    public IReadOnlyList<IEdge> GetEdges(INode node) => EdgesOf(node);

    /// <summary>Прямые рёбра узла — те, где сам узел является источником (From).</summary>
    public IReadOnlyList<IEdge> GetDirectEdges(INode node) =>
        EdgesOf(node).Where(e => e.From.EntityId == node.EntityId).ToList();

    /// <summary>
    /// Косвенные рёбра узла — его предположения об отношениях между двумя другими сущностями;
    /// не обязательно совпадают с реальным прямым ребром между ними.
    /// </summary>
    public IReadOnlyList<IEdge> GetIndirectEdges(INode node) =>
        EdgesOf(node).Where(e => e.From.EntityId != node.EntityId).ToList();

    /// <summary>Прямое ребро from -&gt; to, если оно есть.</summary>
    public IEdge? GetEdge(INode from, INode to) => FindEdge(from, from, to);

    /// <summary>Ребро from -&gt; to так, как его хранит owner (может быть косвенным, если owner != from).</summary>
    public IEdge? GetNodeEdge(INode owner, INode from, INode to) => FindEdge(owner, from, to);

    public bool RemoveEdge(INode owner, IEdge edge) => MutableEdgesOf(owner).Remove(edge);

    public bool RemoveDirectEdge(IEdge edge) => RemoveEdge(edge.From, edge);

    public bool ClearEdges(INode node)
    {
        if (!IsGraphed(node))
        {
            return false;
        }

        MutableEdgesOf(node).Clear();
        return true;
    }

    /// <summary>Узлы, к которым node имеет прямое ребро с отношением, совпадающим (Matches) с заданным.</summary>
    public IReadOnlyList<INode> WithRelationship(INode node, IRelationship relationship) =>
        GetDirectEdges(node)
            .Where(e => e.Relationship.Matches(relationship))
            .Select(e => e.To)
            .ToList();

    /// <summary>Узлы, чьи прямые рёбра указывают на node с отношением, совпадающим (Matches) с заданным.</summary>
    public IReadOnlyList<INode> WithRelationshipTo(INode node, IRelationship relationship) =>
        Nodes
            .SelectMany(GetDirectEdges)
            .Where(e => e.To.EntityId == node.EntityId && e.Relationship.Matches(relationship))
            .Select(e => e.From)
            .ToList();

    private IEdge? FindEdge(INode owner, INode from, INode to)
    {
        foreach (var edge in EdgesOf(owner))
        {
            if (edge.From.EntityId == from.EntityId && edge.To.EntityId == to.EntityId)
            {
                return edge;
            }
        }

        return null;
    }
}
