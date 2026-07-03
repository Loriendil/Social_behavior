namespace RelationshipCore.Dynamics;

/// <summary>
/// Вектор интенсивностей десяти эмоций, каждая в [0,1] (раздел III статьи Ochs).
/// Immutable: любое изменение возвращает новый вектор через <see cref="With"/>.
/// </summary>
public readonly struct EmotionVector
{
    private const int Count = 10;

    private readonly float[]? _values;

    private EmotionVector(float[] values)
    {
        _values = values;
    }

    public static readonly EmotionVector Zero = default;

    /// <summary>Вектор с одной ненулевой эмоцией — типичный результат правила триггера (рис. 2 статьи).</summary>
    public static EmotionVector Single(EmotionKind kind, float intensity)
    {
        var values = new float[Count];
        values[(int)kind] = Clamp(intensity);
        return new EmotionVector(values);
    }

    public float this[EmotionKind kind] => _values is null ? 0f : _values[(int)kind];

    /// <summary>Возвращает новый вектор с изменённой интенсивностью одной эмоции, остальные без изменений.</summary>
    public EmotionVector With(EmotionKind kind, float intensity)
    {
        var values = new float[Count];
        if (_values is not null)
        {
            Array.Copy(_values, values, Count);
        }

        values[(int)kind] = Clamp(intensity);
        return new EmotionVector(values);
    }

    private static float Clamp(float v) => Math.Clamp(v, 0f, 1f);
}
