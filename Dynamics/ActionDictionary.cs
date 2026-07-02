namespace RelationshipCore.Dynamics;

/// <summary>
/// Объективный эффект действий — общий для всех NPC словарь игры (раздел IV статьи Ochs).
/// В отличие от Appraisal (attitude/praise), effect не зависит от того, кто его воспринимает.
/// </summary>
public sealed class ActionDictionary
{
    private readonly Dictionary<int, float> _effects = new();

    public void SetEffect(ActionId action, float effect) => _effects[action.Value] = Math.Clamp(effect, -1f, 1f);

    /// <summary>Объективный эффект действия ∈ [-1,1]; 0, если действие не описано в словаре.</summary>
    public float GetEffect(ActionId action) => _effects.TryGetValue(action.Value, out var effect) ? effect : 0f;
}
