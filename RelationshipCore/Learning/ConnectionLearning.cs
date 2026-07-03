using RelationshipCore.Graphs;

namespace RelationshipCore.Learning;

/// <summary>
/// Простое обучение новым отношениям через полученные Connection (дисс. О'Коннора, разделы 3.4.3 и
/// 4.4.2, рис. 3.4): получатель сравнивает присланное ребро (from, to, relationship) со своим уже
/// известным знанием о той же паре (которое может быть косвенным — предположением о чужих
/// отношениях) и либо принимает его, либо отвергает.
///
/// Правило принятия — по весу (дисс., раздел 4.4.2, "Relationship Evaluation"/"Adopting new
/// Relationships"): если знания о паре ещё нет — присланное принимается как есть (в прототипе
/// автора это тоже сделано "для простоты"); если знание уже есть и конфликтует — остаётся то,
/// у которого больше вес. Что такое "вес" — не часть модели графа (IRelationship нарочно ничего
/// не знает о своём "весе"), поэтому вызывающая сторона передаёт функцию извлечения веса явно.
/// </summary>
public static class ConnectionLearning
{
    /// <summary>
    /// Оценивает присланное ребро received с точки зрения learner. Если у learner ещё нет
    /// знания о паре (received.From, received.To), либо есть, но с меньшим весом, чем у
    /// received, — записывает received в граф от имени learner (прямое ребро, если learner —
    /// сам received.From, иначе косвенное — предположение о чужих отношениях) и возвращает true.
    /// Иначе оставляет существующее знание нетронутым и возвращает false.
    /// </summary>
    public static bool Learn(DeepGraph graph, INode learner, IEdge received, Func<IRelationship, float> weight)
    {
        var existing = graph.GetNodeEdge(learner, received.From, received.To, received.Relationship.GetType());

        if (existing is not null && weight(existing.Relationship) >= weight(received.Relationship))
        {
            return false;
        }

        graph.AddEdge(learner, received);
        return true;
    }

    /// <summary>Удобная перегрузка — принимает уже упакованное в ConnectionMessage ребро.</summary>
    public static bool Learn(DeepGraph graph, INode learner, ConnectionMessage message, Func<IRelationship, float> weight) =>
        Learn(graph, learner, message.Connection, weight);
}
