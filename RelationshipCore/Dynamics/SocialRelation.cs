namespace RelationshipCore.Dynamics;

/// <summary>
/// Социальное отношение по модели Ochs: четыре несимметричных измерения (раздел IV-D статьи
/// Ochs). Liking и dominance ∈ [-1,1]; familiarity и solidarity ∈ [0,1] — диапазоны РАЗНЫЕ,
/// это явно указано в определении квадруплета social_relation в статье.
/// Immutable: обновление ребра графа выполняется присваиванием нового значения в
/// IEdge.Relationship, а не мутацией полей (важно для будущего HistoryEdge — Этап 4).
/// </summary>
public sealed class SocialRelation : IRelationship
{
    public SocialRelation(float liking, float dominance, float familiarity, float solidarity)
    {
        Liking = ClampSigned(liking);
        Dominance = ClampSigned(dominance);
        Familiarity = ClampUnit(familiarity);
        Solidarity = ClampUnit(solidarity);
    }

    public float Liking { get; }

    public float Dominance { get; }

    public float Familiarity { get; }

    public float Solidarity { get; }

    public static readonly SocialRelation Neutral = new(0f, 0f, 0f, 0f);

    /// <summary>
    /// SocialRelationPattern сопоставляется по диапазонам измерений; другой SocialRelation —
    /// по приблизительному числовому совпадению (эпсилон нужен, т.к. значения — результат
    /// вещественной арифметики затухания/обновления).
    /// </summary>
    public bool Matches(IRelationship other) => other switch
    {
        SocialRelationPattern pattern => pattern.Matches(this),
        SocialRelation relation => IsCloseTo(relation),
        _ => false,
    };

    private bool IsCloseTo(SocialRelation other, float epsilon = 0.001f) =>
        MathF.Abs(Liking - other.Liking) < epsilon &&
        MathF.Abs(Dominance - other.Dominance) < epsilon &&
        MathF.Abs(Familiarity - other.Familiarity) < epsilon &&
        MathF.Abs(Solidarity - other.Solidarity) < epsilon;

    private static float ClampSigned(float v) => Math.Clamp(v, -1f, 1f);

    private static float ClampUnit(float v) => Math.Clamp(v, 0f, 1f);
}
