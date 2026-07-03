namespace RelationshipSimulator;

/// <summary>
/// Строка DataGrid для одного шага сценария — прямое соответствие вызову
/// SocialDynamicsEngine.Perceive(perceiverId, new GameEvent(agentId, action, patientId, dc), time).
/// Обычный POCO: значения читаются только в момент запуска сценария, живой реактивности не нужно.
/// </summary>
public sealed class EventRow
{
    public int PerceiverId { get; set; }

    public int AgentId { get; set; }

    public int ActionId { get; set; }

    public int PatientId { get; set; }

    public float Dc { get; set; } = 1f;

    public float Time { get; set; }
}
