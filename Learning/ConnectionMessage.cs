namespace RelationshipCore.Learning;

/// <summary>
/// Сообщение-слух: отправитель делится своим знанием о ребре (from, to, relationship) с
/// получателем (дисс. О'Коннора, рис. 3.4, "ConnectionMessage"). Чистый носитель данных — сама
/// логика принятия/отклонения не здесь, а в <see cref="ConnectionLearning"/>: сообщения-адаптеры
/// знают про граф, граф про них не знает (см. CLAUDE.md, «Архитектура объединения слоёв»).
/// </summary>
public sealed class ConnectionMessage : IMessage
{
    public ConnectionMessage(IEdge connection)
    {
        Connection = connection;
    }

    public IEdge Connection { get; }
}
