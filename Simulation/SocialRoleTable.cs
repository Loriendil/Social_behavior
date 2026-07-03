using RelationshipCore.Dynamics;
using RelationshipCore.Graphs;

namespace RelationshipCore.Simulation;

/// <summary>
/// Начальные значения SocialRelation по паре социальных ролей (cop/gangster и т.п., раздел IV-B
/// статьи Ochs). Роль — генератор стартового состояния, не часть текущего SocialRelation и не
/// хранится на ребре; при нескольких общих ролях между двумя сущностями подходящие пары усредняются.
/// </summary>
public sealed class SocialRoleTable
{
    private readonly Dictionary<(string From, string To), SocialRelation> _baselines = new();

    /// <summary>Задаёт стартовое SocialRelation "с точки зрения" fromRole по отношению к toRole.</summary>
    public void Set(string fromRole, string toRole, SocialRelation relation) =>
        _baselines[(fromRole, toRole)] = relation;

    /// <summary>
    /// Усредняет SocialRelation по всем известным парам (одна роль из fromRoles) × (одна роль из
    /// toRoles). Возвращает SocialRelation.Neutral, если ни одна пара ролей не задана в таблице.
    /// </summary>
    public SocialRelation Resolve(IEnumerable<string> fromRoles, IEnumerable<string> toRoles)
    {
        float liking = 0f, dominance = 0f, familiarity = 0f, solidarity = 0f;
        int count = 0;

        foreach (var fromRole in fromRoles)
        {
            foreach (var toRole in toRoles)
            {
                if (!_baselines.TryGetValue((fromRole, toRole), out var relation))
                {
                    continue;
                }

                liking += relation.Liking;
                dominance += relation.Dominance;
                familiarity += relation.Familiarity;
                solidarity += relation.Solidarity;
                count++;
            }
        }

        return count == 0
            ? SocialRelation.Neutral
            : new SocialRelation(liking / count, dominance / count, familiarity / count, solidarity / count);
    }

    /// <summary>
    /// Инициализирует пару встречных прямых рёбер from&lt;-&gt;to значениями, усреднёнными из
    /// таблицы ролей независимо для каждого направления (SocialRelation несимметричен), через
    /// DeepGraph.AddCommonEdge.
    /// </summary>
    public void ApplyTo(
        DeepGraph graph, INode from, INode to, IEnumerable<string> fromRoles, IEnumerable<string> toRoles)
    {
        var forward = Resolve(fromRoles, toRoles);
        var backward = Resolve(toRoles, fromRoles);
        graph.AddCommonEdge(from, to, forward, backward);
    }
}
