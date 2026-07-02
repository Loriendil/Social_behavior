using RelationshipCore.Dynamics;

namespace RelationshipCore.Dynamics.Rules;

/// <summary>
/// Модуляция интенсивности эмоций личностью (раздел IV-A статьи Ochs): экстраверсия усиливает
/// joy/hope/pride/relief, нейротизм — distress/fear/shame/disappointment. Admiration и anger
/// личностью не модулируются — в статье они не связаны напрямую с этими двумя чертами.
/// Нейтральная личность (0,0) — множитель 1.0; максимальная черта (±1) — множитель 1.5/0.5.
/// </summary>
public static class PersonalityRules
{
    private static readonly EmotionKind[] ExtraversionScaled =
    {
        EmotionKind.Joy, EmotionKind.Hope, EmotionKind.Pride, EmotionKind.Relief,
    };

    private static readonly EmotionKind[] NeuroticismScaled =
    {
        EmotionKind.Distress, EmotionKind.Fear, EmotionKind.Shame, EmotionKind.Disappointment,
    };

    public static EmotionVector Modulate(Personality personality, EmotionVector stimulus)
    {
        var result = stimulus;

        foreach (var kind in ExtraversionScaled)
        {
            result = ScaleBy(result, kind, personality.Extraversion);
        }

        foreach (var kind in NeuroticismScaled)
        {
            result = ScaleBy(result, kind, personality.Neuroticism);
        }

        return result;
    }

    private static EmotionVector ScaleBy(EmotionVector vector, EmotionKind kind, float trait)
    {
        float factor = 1f + (0.5f * trait);
        return vector.With(kind, vector[kind] * factor);
    }
}
