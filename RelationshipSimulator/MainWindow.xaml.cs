using System.Collections.ObjectModel;
using System.Windows;
using RelationshipCore.Dynamics;
using RelationshipCore.Graphs;
using RelationshipCore.Simulation;

namespace RelationshipSimulator;

/// <summary>
/// Редактор персонажей и словаря действий (Этап 6, первый подпункт). Держит единственный
/// экземпляр DeepGraph/ActionDictionary/SocialDynamicsEngine на всё приложение — остальные
/// части UI (сценарии, графики, визуализация графа) будут работать с этим же движком.
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

    public MainWindow()
    {
        _engine = new SocialDynamicsEngine(_graph, _actions);
        InitializeComponent();
        DataContext = this;
    }

    private void AddNpc_Click(object sender, RoutedEventArgs e)
    {
        var state = _engine.RegisterNpc(_nextEntityId++);
        Npcs.Add(new NpcRow(state));
    }

    private void AddAction_Click(object sender, RoutedEventArgs e)
    {
        int actionId = _nextActionId++;
        _actions.SetEffect(new ActionId(actionId), 0f);
        Actions.Add(new ActionRow(_actions, actionId));
    }
}
