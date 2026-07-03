using RelationshipCore.Dynamics;

namespace RelationshipCore.Learning;

/// <summary>Готовые функции веса для ConnectionLearning.Learn — насколько выражено/уверенно отношение, независимо от знака.</summary>
public static class RelationshipWeights
{
    /// <summary>
    /// Вес SocialRelation — сумма модулей всех четырёх измерений. Чем сильнее выражено мнение
    /// (в любую сторону) и чем выше знакомство/солидарность, тем больше вес — такое отношение
    /// вытесняет более слабое/неопределённое при конфликте знаний о паре.
    /// </summary>
    public static float SocialRelation(IRelationship relationship) =>
        relationship is SocialRelation sr
            ? MathF.Abs(sr.Liking) + MathF.Abs(sr.Dominance) + sr.Familiarity + sr.Solidarity
            : 0f;
}
