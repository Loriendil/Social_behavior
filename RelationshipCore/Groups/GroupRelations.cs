using RelationshipCore.Graphs;

namespace RelationshipCore.Groups;

/// <summary>
/// Групповая семантика поверх DeepGraph (дисс. О'Коннора, раздел 3.4.1, рис. 3.3-3.4): членство
/// через MemberRelationship, наследование отношений группы её членами ("враг группы — враг каждого
/// члена") и broadcast-сообщений членам группы. Группа — обычный INode; ничто структурно не
/// отличает его от любого другого узла, кроме входящих MEMBER-рёбер от участников.
/// </summary>
public static class GroupRelations
{
    /// <summary>Узлы, являющиеся прямыми MEMBER группы group (у кого прямое ребро member-&gt;group имеет Relationship MEMBER).</summary>
    public static IReadOnlyList<INode> MembersOf(DeepGraph graph, INode group) =>
        graph.WithRelationshipTo(group, MemberRelationship.Instance);

    /// <summary>Группы, MEMBER'ом которых является member.</summary>
    public static IReadOnlyList<INode> GroupsOf(DeepGraph graph, INode member) =>
        graph.WithRelationship(member, MemberRelationship.Instance);

    /// <summary>
    /// Есть ли у member отношение к target, совпадающее с pattern — напрямую, ИЛИ унаследованное
    /// от любой группы, MEMBER которой является member ("враг группы — враг каждого члена", рис. 3.3
    /// дисс. О'Коннора). Прямое отношение member-&gt;target всегда имеет приоритет над групповым:
    /// метод возвращает true, как только находит первое совпадение любого из источников.
    /// </summary>
    public static bool HasInheritedRelationship(DeepGraph graph, INode member, INode target, IRelationship pattern)
    {
        var direct = graph.GetEdge(member, target);
        if (direct is not null && direct.Relationship.Matches(pattern))
        {
            return true;
        }

        foreach (var group in GroupsOf(graph, member))
        {
            var groupEdge = graph.GetEdge(group, target);
            if (groupEdge is not null && groupEdge.Relationship.Matches(pattern))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Рассылает message всем прямым MEMBER'ам group (дисс. О'Коннора, раздел 3.4.1: "A message is
    /// sent from Entity 'O' to the Group Entity 'G'. This in turn 'Broadcasts' the message to each
    /// of its members"). Самой group сообщение не доставляется — только участникам.
    /// </summary>
    public static void Broadcast(DeepGraph graph, INode group, IMessage message)
    {
        foreach (var member in MembersOf(graph, group))
        {
            member.HandleMessage(message);
        }
    }
}
