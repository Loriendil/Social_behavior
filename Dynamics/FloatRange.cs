namespace RelationshipCore.Dynamics;

/// <summary>Диапазон [Min, Max] для запросов к измерениям SocialRelation через SocialRelationPattern.</summary>
public readonly struct FloatRange
{
    public FloatRange(float min, float max)
    {
        Min = min;
        Max = max;
    }

    public float Min { get; }

    public float Max { get; }

    public bool Contains(float value) => value >= Min && value <= Max;

    public static FloatRange AtLeast(float min) => new(min, 1f);

    public static FloatRange AtMost(float max) => new(-1f, max);
}
