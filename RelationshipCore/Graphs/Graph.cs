namespace RelationshipCore.Graphs;

/// <summary>
/// Хранилище графа: узлы и списки рёбер, индексированные по EntityId (Dictionary, не линейный поиск).
/// Не знает про семантику прямых/косвенных рёбер — см. <see cref="DeepGraph"/>.
/// </summary>
public class Graph
{
    private readonly Dictionary<int, INode> _nodes = new();
    private readonly Dictionary<int, List<IEdge>> _edges = new();

    public int Count => _nodes.Count;

    public IReadOnlyCollection<INode> Nodes => _nodes.Values;

    public bool IsGraphed(INode node) => _nodes.ContainsKey(node.EntityId);

    public bool HasEdges(INode node) => _edges.TryGetValue(node.EntityId, out var edges) && edges.Count > 0;

    public void AddNode(INode node)
    {
        if (_nodes.TryAdd(node.EntityId, node))
        {
            _edges.Add(node.EntityId, new List<IEdge>());
        }
    }

    public bool RemoveNode(INode node)
    {
        _edges.Remove(node.EntityId);
        return _nodes.Remove(node.EntityId);
    }

    public INode? GetNode(int entityId) => _nodes.TryGetValue(entityId, out var node) ? node : null;

    /// <summary>Рёбра, хранящиеся "у" данного узла — могут быть как прямыми, так и косвенными.</summary>
    protected IReadOnlyList<IEdge> EdgesOf(INode node) =>
        _edges.TryGetValue(node.EntityId, out var edges) ? edges : Array.Empty<IEdge>();

    /// <summary>То же самое, но регистрирует узел в графе, если его там ещё нет.</summary>
    protected List<IEdge> MutableEdgesOf(INode node)
    {
        if (!_edges.TryGetValue(node.EntityId, out var edges))
        {
            AddNode(node);
            edges = _edges[node.EntityId];
        }

        return edges;
    }
}
