namespace RelationshipCore.Dynamics;

/// <summary>Личность NPC: пара черт (extraversion, neuroticism) ∈ [-1,1]² (раздел IV-A статьи Ochs).</summary>
public readonly struct Personality
{
    public Personality(float extraversion, float neuroticism)
    {
        Extraversion = Math.Clamp(extraversion, -1f, 1f);
        Neuroticism = Math.Clamp(neuroticism, -1f, 1f);
    }

    public float Extraversion { get; }

    public float Neuroticism { get; }

    public static readonly Personality Neutral = new(0f, 0f);
}
