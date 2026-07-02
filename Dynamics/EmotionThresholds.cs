namespace RelationshipCore.Dynamics;

/// <summary>
/// Пороги активации и насыщения эмоций. Ниже порога активации эмоция не влияет на поведение;
/// выше порога насыщения — отключает рациональное принятие решений (см. общее описание модели Ochs).
/// Статья не фиксирует конкретные числа — это настраиваемые параметры игры.
/// </summary>
public sealed class EmotionThresholds
{
    public EmotionThresholds(float activation = 0.1f, float saturation = 0.9f)
    {
        Activation = Math.Clamp(activation, 0f, 1f);
        Saturation = Math.Clamp(saturation, 0f, 1f);
    }

    public float Activation { get; }

    public float Saturation { get; }

    public bool IsActive(EmotionVector emotions, EmotionKind kind) => emotions[kind] >= Activation;

    public bool IsSaturated(EmotionVector emotions, EmotionKind kind) => emotions[kind] >= Saturation;
}
