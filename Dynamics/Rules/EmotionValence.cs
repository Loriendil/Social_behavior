using RelationshipCore.Dynamics;

namespace RelationshipCore.Dynamics.Rules;

/// <summary>
/// Классификация эмоций по знаку (позитивные/негативные) и по влиянию на доминантность
/// выражающего — вспомогательные данные для SocialRelationRules (рис. 4-6 статьи Ochs).
/// </summary>
internal static class EmotionValence
{
    public static readonly EmotionKind[] Positive =
    {
        EmotionKind.Joy, EmotionKind.Hope, EmotionKind.Relief, EmotionKind.Pride, EmotionKind.Admiration,
    };

    public static readonly EmotionKind[] Negative =
    {
        EmotionKind.Distress, EmotionKind.Fear, EmotionKind.Disappointment, EmotionKind.Shame, EmotionKind.Anger,
    };

    /// <summary>Pride/anger — "напористые" эмоции, поднимающие доминантность того, кто их испытывает.</summary>
    public static readonly EmotionKind[] DominanceRaising = { EmotionKind.Pride, EmotionKind.Anger };

    /// <summary>Fear/distress/admiration/shame — "подчинённые" эмоции, снижающие доминантность испытывающего.</summary>
    public static readonly EmotionKind[] DominanceLowering =
    {
        EmotionKind.Fear, EmotionKind.Distress, EmotionKind.Admiration, EmotionKind.Shame,
    };

    public static readonly EmotionKind[] All = (EmotionKind[])Enum.GetValues(typeof(EmotionKind));

    public static float Sum(EmotionVector emotions, EmotionKind[] kinds)
    {
        float total = 0f;
        foreach (var kind in kinds)
        {
            total += emotions[kind];
        }

        return total;
    }

    /// <summary>Сумма позитивных минус сумма негативных — знак и сила общей желательности испытанного.</summary>
    public static float NetValence(EmotionVector emotions) => Sum(emotions, Positive) - Sum(emotions, Negative);

    /// <summary>Сумма "напористых" минус сумма "подчинённых" эмоций — сдвиг собственной доминантности.</summary>
    public static float NetDominanceShift(EmotionVector emotions) =>
        Sum(emotions, DominanceRaising) - Sum(emotions, DominanceLowering);
}
