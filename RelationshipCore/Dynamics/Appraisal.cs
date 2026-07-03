namespace RelationshipCore.Dynamics;

/// <summary>
/// Субъективная оценка одного NPC (раздел IV статьи Ochs): attitude к сущностям графа
/// (персонажам, предметам, локациям — все они адресуются через EntityId) и praise к действиям.
/// В отличие от ActionDictionary.GetEffect, эти значения свои у каждого NPC.
/// </summary>
public sealed class Appraisal
{
    private readonly Dictionary<int, float> _attitudes = new();
    private readonly Dictionary<int, float> _praises = new();

    /// <summary>Отношение к сущности с данным EntityId ∈ [-1,1]; 0 (безразличие), если не задано.</summary>
    public float GetAttitude(int targetEntityId) =>
        _attitudes.TryGetValue(targetEntityId, out var value) ? value : 0f;

    public void SetAttitude(int targetEntityId, float value) =>
        _attitudes[targetEntityId] = Math.Clamp(value, -1f, 1f);

    /// <summary>Оценка действия ∈ [-1,1]; 0 (безразлично), если не задано.</summary>
    public float GetPraise(ActionId action) =>
        _praises.TryGetValue(action.Value, out var value) ? value : 0f;

    public void SetPraise(ActionId action, float value) =>
        _praises[action.Value] = Math.Clamp(value, -1f, 1f);
}
