namespace RelationshipCore.Dynamics;

/// <summary>
/// Запрос-паттерн для DeepGraph.WithRelationship/WithRelationshipTo: описывает диапазоны
/// измерений SocialRelation вместо конкретных значений (null — "любое значение"). Позволяет
/// выражать категории вроде "друзья"/"враги" как производную от непрерывных измерений,
/// не заводя отдельный enum-тип отношения.
/// </summary>
public sealed class SocialRelationPattern : IRelationship
{
    public FloatRange? Liking { get; init; }

    public FloatRange? Dominance { get; init; }

    public FloatRange? Familiarity { get; init; }

    public FloatRange? Solidarity { get; init; }

    public bool Matches(IRelationship other)
    {
        if (other is not SocialRelation relation)
        {
            return false;
        }

        return (Liking is not { } liking || liking.Contains(relation.Liking)) &&
               (Dominance is not { } dominance || dominance.Contains(relation.Dominance)) &&
               (Familiarity is not { } familiarity || familiarity.Contains(relation.Familiarity)) &&
               (Solidarity is not { } solidarity || solidarity.Contains(relation.Solidarity));
    }
}
