namespace RelationshipCore.Dynamics;

/// <summary>
/// Событие: кортеж ⟨agent, action, patient, dc⟩ (раздел IV статьи Ochs).
/// dc ∈ [0,1] — степень уверенности воспринимающего (1 — очевидец, 0 — ожидаемое не произошло).
/// </summary>
public readonly struct GameEvent
{
    public GameEvent(int agentId, ActionId action, int patientId, float dc)
    {
        AgentId = agentId;
        Action = action;
        PatientId = patientId;
        Dc = Math.Clamp(dc, 0f, 1f);
    }

    public int AgentId { get; }

    public ActionId Action { get; }

    public int PatientId { get; }

    public float Dc { get; }
}
