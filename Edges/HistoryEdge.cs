namespace RelationshipCore.Edges;

/// <summary>
/// Ребро, хранящее полную историю значений Relationship, а не только текущее (дисс. О'Коннора,
/// разделы 3.2.1 и 4.2.4 — "HistoryEdge"/"DeepEdge"). Каждое присваивание Relationship ДОПИСЫВАЕТ
/// значение в список вместо замены — старые значения не теряются.
///
/// Совместимо с DeepGraph.AddEdge без каких-либо изменений вызывающего кода: AddEdge вызывает
/// Relationship-сеттер УЖЕ НАЙДЕННОГО существующего ребра (см. DeepGraph), поэтому если это ребро —
/// HistoryEdge, а не обычный Edge, то апдейт через throwaway `new Edge(from, to, updated)` (как это
/// уже делает SocialDynamicsEngine.UpdateRelation) автоматически становится записью в историю.
/// Чтобы включить историю для конкретной пары узлов, достаточно один раз создать ребро как
/// HistoryEdge вместо Edge — остальной код (SocialDynamicsEngine, SocialRoleTable) не меняется.
/// </summary>
public sealed class HistoryEdge : IEdge
{
    private readonly List<IRelationship> _history = new();

    public HistoryEdge(INode from, INode to, IRelationship relationship)
    {
        From = from;
        To = to;
        Relationship = relationship;
    }

    public INode From { get; }

    public INode To { get; }

    /// <summary>Текущее (последнее записанное) значение. Присваивание ДОПИСЫВАЕТ в историю, не заменяет.</summary>
    public IRelationship Relationship
    {
        get => _history[^1];
        set => _history.Add(value);
    }

    /// <summary>Полная история значений от самого старого к текущему; последний элемент == Relationship.</summary>
    public IReadOnlyList<IRelationship> History => _history;

    /// <summary>Сколько раз было установлено значение Relationship (включая текущее).</summary>
    public int Count => _history.Count;

    /// <summary>
    /// Значение Relationship stepsBack шагов назад от текущего (1 — предыдущее значение, 2 — то,
    /// что было перед ним, и т.д.); null, если истории на столько шагов назад ещё нет.
    /// </summary>
    public IRelationship? PreviousRelationship(int stepsBack = 1)
    {
        int index = _history.Count - 1 - stepsBack;
        return index >= 0 ? _history[index] : null;
    }
}
