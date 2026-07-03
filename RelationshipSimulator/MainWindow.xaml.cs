using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using RelationshipCore.Dynamics;
using RelationshipCore.Graphs;
using RelationshipCore.Nodes;
using RelationshipCore.Simulation;

namespace RelationshipSimulator;

/// <summary>
/// Редактор персонажей/словаря действий и редактор сценария (Этап 6, подпункты 1-2). Держит
/// единственный экземпляр DeepGraph/ActionDictionary/SocialDynamicsEngine на всё приложение —
/// остальные части UI (графики, визуализация графа) будут работать с этим же движком.
/// </summary>
public partial class MainWindow : Window
{
    private readonly DeepGraph _graph = new();
    private readonly ActionDictionary _actions = new();
    private readonly SocialDynamicsEngine _engine;
    private int _nextEntityId = 1;
    private int _nextActionId = 1;

    public ObservableCollection<NpcRow> Npcs { get; } = new();

    public ObservableCollection<ActionRow> Actions { get; } = new();

    public ObservableCollection<EventRow> Events { get; } = new();

    public MainWindow()
    {
        _engine = new SocialDynamicsEngine(_graph, _actions);
        InitializeComponent();
        DataContext = this;
    }

    private void AddNpc_Click(object sender, RoutedEventArgs e)
    {
        int entityId = _nextEntityId++;
        var state = _engine.RegisterNpc(entityId);
        _graph.AddNode(new Node(entityId)); // без этого Perceive не найдёт узел и бросит исключение
        Npcs.Add(new NpcRow(state));
    }

    private void AddAction_Click(object sender, RoutedEventArgs e)
    {
        int actionId = _nextActionId++;
        _actions.SetEffect(new ActionId(actionId), 0f);
        Actions.Add(new ActionRow(_actions, actionId));
    }

    private void AddEvent_Click(object sender, RoutedEventArgs e) => Events.Add(new EventRow());

    private void RunScenario_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            foreach (var row in Events.OrderBy(r => r.Time))
            {
                _engine.Perceive(
                    row.PerceiverId,
                    new GameEvent(row.AgentId, new ActionId(row.ActionId), row.PatientId, row.Dc),
                    row.Time);
            }

            ResultsText.Text = BuildResultsSummary();
        }
        catch (InvalidOperationException ex)
        {
            ResultsText.Text = $"Ошибка: {ex.Message}\n\n(проверьте, что все perceiverId/agentId/patientId — это EntityId уже добавленных персонажей)";
        }
    }

    private string BuildResultsSummary()
    {
        var sb = new StringBuilder();

        sb.AppendLine("Эмоции:");
        foreach (var npc in Npcs)
        {
            var state = _engine.GetState(npc.EntityId);
            var active = Enum.GetValues<EmotionKind>()
                .Where(kind => state.Emotions[kind] > 0.001f)
                .Select(kind => $"{kind}={state.Emotions[kind]:F2}");
            sb.AppendLine($"  {npc.EntityId}: {string.Join(", ", active)}");
        }

        sb.AppendLine();
        sb.AppendLine("Отношения (SocialRelation по прямым рёбрам между добавленными персонажами):");
        foreach (var from in Npcs)
        {
            foreach (var to in Npcs)
            {
                if (from.EntityId == to.EntityId)
                {
                    continue;
                }

                var fromNode = _graph.GetNode(from.EntityId);
                var toNode = _graph.GetNode(to.EntityId);
                if (fromNode is null || toNode is null)
                {
                    continue;
                }

                if (_graph.GetEdge<SocialRelation>(fromNode, toNode)?.Relationship is SocialRelation relation)
                {
                    sb.AppendLine(
                        $"  {from.EntityId}->{to.EntityId}: liking={relation.Liking:F2} dominance={relation.Dominance:F2} " +
                        $"familiarity={relation.Familiarity:F2} solidarity={relation.Solidarity:F2}");
                }
            }
        }

        return sb.ToString();
    }
}
