using RelationshipCore.Dynamics;

namespace RelationshipCore.Dynamics.Rules;

/// <summary>
/// Точные наборы эмоций по измерениям SocialRelation — рис. 4 (liking), рис. 5 (dominance) и
/// рис. 6 (solidarity) статьи Ochs. Наборы для разных измерений НЕ совпадают (например,
/// relief/disappointment не входят в набор liking, admiration не входит в набор solidarity) —
/// поэтому это отдельные списки, а не один общий "позитив/негатив".
/// </summary>
internal static class EmotionValence
{
    /// <summary>Рис. 4: "emotions of i caused by j" → +liking.</summary>
    public static readonly EmotionKind[] LikingPositive =
    {
        EmotionKind.Joy, EmotionKind.Hope, EmotionKind.Admiration, EmotionKind.Pride,
    };

    /// <summary>Рис. 4: "emotions of i caused by j" → -liking.</summary>
    public static readonly EmotionKind[] LikingNegative =
    {
        EmotionKind.Distress, EmotionKind.Fear, EmotionKind.Anger, EmotionKind.Shame,
    };

    /// <summary>Рис. 5: "emotions of i caused by j" → +dominance.</summary>
    public static readonly EmotionKind[] DominancePositive = { EmotionKind.Pride, EmotionKind.Anger };

    /// <summary>
    /// Рис. 5: "emotions of i caused by j" → -dominance. Текст статьи (раздел IV-D-2) явно
    /// перечисляет 4 эмоции ("fear, distress, admiration, or shame"), хотя на самой диаграмме
    /// shame визуально отсутствует (вероятно, опечатка в оригинале) — доверяем тексту.
    /// </summary>
    public static readonly EmotionKind[] DominanceNegative =
    {
        EmotionKind.Fear, EmotionKind.Distress, EmotionKind.Admiration, EmotionKind.Shame,
    };

    /// <summary>Рис. 5: "emotions expressed by j" → +dominance у наблюдателя i. Только эти две.</summary>
    public static readonly EmotionKind[] DominanceExpressedPositive = { EmotionKind.Fear, EmotionKind.Distress };

    /// <summary>Рис. 6: "emotions of i caused by j" → -solidarity.</summary>
    public static readonly EmotionKind[] SolidarityNegative =
    {
        EmotionKind.Distress, EmotionKind.Fear, EmotionKind.Disappointment, EmotionKind.Shame, EmotionKind.Anger,
    };

    /// <summary>Рис. 6: типы эмоций, участвующие в сравнении "совпадение/несовпадение" для solidarity.</summary>
    public static readonly EmotionKind[] CoincidenceKinds =
    {
        EmotionKind.Joy, EmotionKind.Hope, EmotionKind.Distress, EmotionKind.Fear,
    };

    /// <summary>
    /// Рис. 6: несовпадающие (противоположные по желательности) пары (i, j) → -solidarity.
    /// Например, joy у i и distress у j одновременно — несовпадение.
    /// </summary>
    public static readonly (EmotionKind Own, EmotionKind Other)[] IncongruentPairs =
    {
        (EmotionKind.Joy, EmotionKind.Distress),
        (EmotionKind.Hope, EmotionKind.Fear),
        (EmotionKind.Distress, EmotionKind.Joy),
        (EmotionKind.Fear, EmotionKind.Hope),
    };

    public static float Sum(EmotionVector emotions, EmotionKind[] kinds)
    {
        float total = 0f;
        foreach (var kind in kinds)
        {
            total += emotions[kind];
        }

        return total;
    }
}
