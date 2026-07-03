namespace RelationshipCore.Nodes;

/// <summary>Минимальная реализация INode. Базовый класс для персонажей, групп, предметов и локаций.</summary>
public class Node : INode
{
    public Node(int entityId)
    {
        EntityId = entityId;
    }

    public int EntityId { get; }

    /// <summary>Базовая реализация ничего не делает; наследники переопределяют обработку конкретных сообщений.</summary>
    public virtual void HandleMessage(IMessage message)
    {
    }
}
