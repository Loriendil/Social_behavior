using RelationshipCore.Edges;

namespace RelationshipCore.Graphs;

/// <summary>
/// Граф с семантикой прямых и косвенных рёбер (раздел 3.1 диссертации O'Connor).
/// Прямое ребро хранится у узла edge.From — это собственное отношение узла.
/// Косвенное ребро хранится у узла, отличного от edge.From, — это предположение
/// узла-владельца об отношении двух других сущностей (слухи, обман, неполное знание).
///
/// Поддерживает несколько параллельных Relationship РАЗНЫХ типов между одной и той же парой
/// узлов ("WideEdge", дисс. О'Коннора §6.2.6, Future Work — сам автор такое не реализовал, только
/// предложил): AddEdge перезаписывает существующее ребро только если у него ТОТ ЖЕ рантайм-тип
/// Relationship; ребро другого типа между той же парой добавляется отдельно. Нужно, когда на одной
/// паре узлов одновременно живут структурное отношение слоя O'Connor (например, MEMBER) и
/// динамическое SocialRelation слоя Ochs — они не должны конфликтовать за один слот.
/// </summary>
public class DeepGraph : Graph
{
    /// <summary>Есть ли прямое ребро from -&gt; to.</summary>
    public bool HasEdge(INode from, INode to) => GetEdge(from, to) is not null;

    /// <summary>Есть ли у owner (прямое или косвенное) ребро from -&gt; to.</summary>
    public bool NodeHasEdge(INode owner, INode from, INode to) => FindEdge(owner, from, to) is not null;

    /// <summary>
    /// Добавляет ребро в список owner. Если у owner уже есть ребро с тем же From/To И ТЕМ ЖЕ
    /// рантайм-типом Relationship, обновляет его Relationship вместо добавления дубликата.
    /// Ребро с ДРУГИМ типом Relationship между той же парой (from,to) добавляется отдельно, не
    /// перезаписывая существующее — см. WideEdge, дисс. О'Коннора §6.2.6: одна пара узлов может
    /// одновременно нести несколько параллельных Relationship разных типов (например, структурное
    /// MEMBER слоя O'Connor и динамический SocialRelation слоя Ochs), и они не должны конфликтовать
    /// за один слот.
    /// </summary>
    public void AddEdge(INode owner, IEdge edge)
    {
        var existing = FindEdge(owner, edge.From, edge.To, edge.Relationship.GetType());
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

    /// <summary>
    /// Прямое ребро from -&gt; to, если оно есть. Если между этой парой узлов хранится несколько
    /// Relationship разных типов (WideEdge, см. AddEdge), возвращает ПЕРВОЕ найденное — для
    /// однозначного результата, когда типов несколько, используйте <see cref="GetEdge{TRelationship}"/>.
    /// </summary>
    public IEdge? GetEdge(INode from, INode to) => FindEdge(from, from, to);

    /// <summary>Прямое ребро from -&gt; to с Relationship ровно типа TRelationship — однозначный доступ при нескольких параллельных типах между одной парой узлов (WideEdge).</summary>
    public IEdge? GetEdge<TRelationship>(INode from, INode to) where TRelationship : IRelationship =>
        FindEdge(from, from, to, typeof(TRelationship));

    /// <summary>Ребро from -&gt; to так, как его хранит owner (может быть косвенным, если owner != from).</summary>
    public IEdge? GetNodeEdge(INode owner, INode from, INode to) => FindEdge(owner, from, to);

    /// <summary>То же самое, но с уточнением типа Relationship — см. GetEdge&lt;TRelationship&gt;.</summary>
    public IEdge? GetNodeEdge<TRelationship>(INode owner, INode from, INode to) where TRelationship : IRelationship =>
        FindEdge(owner, from, to, typeof(TRelationship));

    /// <summary>
    /// То же самое, но тип Relationship передаётся как рантайм-значение — нужно, когда конкретный
    /// тип известен только во время выполнения (например, тип присланного слуха в ConnectionLearning).
    /// </summary>
    public IEdge? GetNodeEdge(INode owner, INode from, INode to, Type relationshipType) =>
        FindEdge(owner, from, to, relationshipType);

    /// <summary>
    /// Все рёбра, которые owner хранит для конкретной пары from-&gt;to, независимо от типа
    /// Relationship (WideEdge "как есть", без комбинирующей функции — дисс. О'Коннора §6.2.6).
    /// </summary>
    public IReadOnlyList<IEdge> GetEdges(INode owner, INode from, INode to) =>
        EdgesOf(owner)
            .Where(e => e.From.EntityId == from.EntityId && e.To.EntityId == to.EntityId)
            .ToList();

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

    /// <summary>
    /// Ищет ребро owner'а по паре (from,to); если relationshipType задан, дополнительно требует
    /// точного совпадения рантайм-типа Relationship — так несколько параллельных Relationship
    /// разных типов между одной парой узлов (WideEdge) не путаются друг с другом.
    /// </summary>
    private IEdge? FindEdge(INode owner, INode from, INode to, Type? relationshipType = null)
    {
        foreach (var edge in EdgesOf(owner))
        {
            if (edge.From.EntityId == from.EntityId && edge.To.EntityId == to.EntityId &&
                (relationshipType is null || edge.Relationship.GetType() == relationshipType))
            {
                return edge;
            }
        }

        return null;
    }
}
