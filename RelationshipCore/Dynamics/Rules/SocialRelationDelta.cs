namespace RelationshipCore.Dynamics.Rules;

/// <summary>
/// Вектор изменения социального отношения — выход функции f_sr статьи Ochs (раздел IV-D-4),
/// ещё не применённый к текущему отношению. Несколько источников изменения за одно
/// взаимодействие (своя эмоция, эмоция собеседника, совпадение эмоций) складываются через `+`
/// и применяются к SocialRelation ОДИН раз через SocialRelationRules.Apply (функция g_sr) —
/// так и в статье: φ_sr = g_sr(social_relation, f_sr(...)), а не g_sr, вызванная несколько раз подряд.
/// </summary>
public readonly struct SocialRelationDelta
{
    public SocialRelationDelta(float liking = 0f, float dominance = 0f, float solidarity = 0f)
    {
        Liking = liking;
        Dominance = dominance;
        Solidarity = solidarity;
    }

    public float Liking { get; }

    public float Dominance { get; }

    public float Solidarity { get; }

    public static readonly SocialRelationDelta Zero = default;

    public static SocialRelationDelta operator +(SocialRelationDelta a, SocialRelationDelta b) =>
        new(a.Liking + b.Liking, a.Dominance + b.Dominance, a.Solidarity + b.Solidarity);
}
