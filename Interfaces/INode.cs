namespace RelationshipCore;

/// <summary>Узел графа: персонаж, группа, предмет или локация.</summary>
public interface INode
{
    int EntityId { get; }

    void HandleMessage(IMessage message);
}
