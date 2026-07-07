using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Msagl.WpfGraphControl;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using RelationshipCore.Dynamics;
using RelationshipCore.Graphs;
using RelationshipCore.Nodes;
using RelationshipCore.Simulation;

namespace RelationshipSimulator;

/// <summary>
/// Редактор персонажей/словаря действий, редактор сценария, графики эмоций и визуализация графа
/// (Этап 6, все четыре подпункта). Держит единственный экземпляр
/// DeepGraph/ActionDictionary/SocialDynamicsEngine на всё приложение — все вкладки работают с
/// одним и тем же движком, а не создают свои копии.
///
/// Типы MSAGL (Microsoft.Msagl.Drawing.Graph/Node) везде пишутся полным именем — короткие имена
/// Graph/Node уже заняты RelationshipCore.Graphs.Graph/RelationshipCore.Nodes.Node через using выше.
/// </summary>
public partial class MainWindow : Window
{
    private readonly DeepGraph _graph = new();
    private readonly ActionDictionary _actions = new();
    private readonly SocialDynamicsEngine _engine;
    private readonly List<(int NpcId, float Time, EmotionVector Emotions)> _emotionSamples = new();
    private readonly GraphViewer _graphViewer = new();
    private bool _graphViewerBound;
    private int _nextEntityId = 1;
    private int _nextActionId = 1;

    public ObservableCollection<NpcRow> Npcs { get; } = new();

    public ObservableCollection<ActionRow> Actions { get; } = new();

    public ObservableCollection<EventRow> Events { get; } = new();

    public ObservableCollection<AttitudeRow> Attitudes { get; } = new();

    public ObservableCollection<PraiseRow> Praises { get; } = new();

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

    /// <summary>
    /// WPF DataGrid держит незавершённое редактирование строки открытым, пока пользователь не
    /// перейдёт на другую строку/элемент управления В ТОЙ ЖЕ вкладке (это вызывает CommitEdit).
    /// Переключение TabItem само по себе НЕ гарантирует commit — обнаружено вручную: если
    /// отредактировать 2+ ячейки одной строки (например, TargetEntityId и Attitude) и сразу
    /// переключиться на другую вкладку, WPF может отменить (CancelEdit), а не зафиксировать
    /// последнее изменение. Форсируем commit на всех гридах при каждом переключении вкладки.
    /// </summary>
    private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
    }

    private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
    }

    /// <summary>
    /// AttitudeRow/PraiseRow имеют "перекрёстные" ячейки: Attitude/Praise читают значение по
    /// ключу, собранному из OwnerId+TargetEntityId (или OwnerId+ActionId) ДРУГИХ ячеек той же
    /// строки. Обнаружено вручную (см. TASK_fixes_round1.md, задача 1): если отредактировать
    /// TargetEntityId, а затем сразу Attitude на той же строке и уйти со вкладки, WPF DataGrid
    /// может не закрыть транзакцию редактирования строки полностью, и второе значение теряется
    /// (не долетает до модели) — воспроизводится и настоящим кликом мыши, это не баг автоматизации.
    /// Форсируем CommitEdit строки сразу после КАЖДОЙ ячейки (не дожидаясь ухода со строки/вкладки).
    /// Dispatcher.BeginInvoke — чтобы не вызывать CommitEdit реентрантно изнутри CellEditEnding.
    /// </summary>
    private void CrossFieldGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction != DataGridEditAction.Commit)
        {
            return;
        }

        var grid = (DataGrid)sender;
        Dispatcher.BeginInvoke(new Action(() => grid.CommitEdit(DataGridEditingUnit.Row, true)));
    }

    private void CommitAllGridEdits()
    {
        foreach (var grid in new[] { NpcGrid, ActionGrid, AttitudeGrid, PraiseGrid, EventGrid })
        {
            grid.CommitEdit(DataGridEditingUnit.Cell, true);
            grid.CommitEdit(DataGridEditingUnit.Row, true);
        }
    }

    /// <summary>
    /// Задача 3 (раунд правок 2): единственное хранилище attitude — эта таблица (owner==target —
    /// это attitude "к себе"). Дубли по (OwnerId, TargetEntityId) запрещены при РЕДАКТИРОВАНИИ
    /// (см. AttitudeRow) — попытка увести строку на уже занятую пару откатывается.
    /// </summary>
    private bool AttitudeWouldDuplicate(AttitudeRow row, int ownerId, int targetId) =>
        Attitudes.Any(r => !ReferenceEquals(r, row) && r.OwnerId == ownerId && r.TargetEntityId == targetId);

    /// <summary>
    /// Новая строка получает заведомо свободный (никогда не встречавшийся) TargetEntityId —
    /// иначе фиксированный дефолт (owner=target=Npcs[0]) сталкивался бы сам с собой при втором
    /// клике "Добавить attitude" до того, как пользователь успеет отредактировать первую строку
    /// (обнаружено при живой проверке UI, задача 3). Пара с уникальным (заведомо неиспользуемым)
    /// target не может дублировать ни одну существующую строку, поэтому AttitudeWouldDuplicate
    /// тут заведомо не сработает.
    /// </summary>
    private void AddAttitude_Click(object sender, RoutedEventArgs e)
    {
        int ownerId = Npcs.Count > 0 ? Npcs[0].EntityId : 0;
        int placeholderTarget = Attitudes.Count == 0 ? -1 : Math.Min(-1, Attitudes.Min(r => r.TargetEntityId) - 1);
        Attitudes.Add(new AttitudeRow(_engine, ownerId, placeholderTarget, AttitudeWouldDuplicate));
    }

    private void AddPraise_Click(object sender, RoutedEventArgs e)
    {
        int ownerId = Npcs.Count > 0 ? Npcs[0].EntityId : 0;
        int actionId = Actions.Count > 0 ? Actions[0].ActionId : 0;
        Praises.Add(new PraiseRow(_engine, ownerId, actionId));
    }

    private void RunScenario_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            CommitAllGridEdits();
            _emotionSamples.Clear();
            foreach (var row in Events.OrderBy(r => r.Time))
            {
                _engine.Perceive(
                    row.PerceiverId,
                    new GameEvent(row.AgentId, new ActionId(row.ActionId), row.PatientId, row.Dc),
                    row.Time);

                _emotionSamples.Add((row.PerceiverId, row.Time, _engine.GetState(row.PerceiverId).Emotions));
            }

            ResultsText.Text = BuildResultsSummary();

            if (ChartNpcSelector.SelectedItem is null && Npcs.Count > 0)
            {
                ChartNpcSelector.SelectedIndex = 0; // это само вызовет ChartNpcSelector_SelectionChanged
            }
            else
            {
                RebuildChart();
            }
        }
        catch (InvalidOperationException ex)
        {
            ResultsText.Text = $"Ошибка: {ex.Message}\n\n(проверьте, что все perceiverId/agentId/patientId — это EntityId уже добавленных персонажей)";
        }
    }

    private void ChartNpcSelector_SelectionChanged(object sender, SelectionChangedEventArgs e) => RebuildChart();

    /// <summary>
    /// Строит график эмоций выбранного персонажа во времени (рис. 9 статьи Ochs) по накопленным
    /// в RunScenario_Click снимкам EmotionVector. Показывает только те эмоции, что хоть раз были
    /// заметно ненулевыми у этого персонажа — десять плоских нулевых линий только мешали бы читать график.
    /// </summary>
    private void RebuildChart()
    {
        if (ChartNpcSelector.SelectedItem is not NpcRow selected)
        {
            EmotionPlot.Model = new PlotModel { Title = "Сначала добавьте персонажа и запустите сценарий" };
            return;
        }

        var samples = _emotionSamples
            .Where(s => s.NpcId == selected.EntityId)
            .OrderBy(s => s.Time)
            .ToList();

        var model = new PlotModel { Title = $"Эмоции персонажа {selected.EntityId} во времени" };
        model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Время" });
        model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Интенсивность", Minimum = 0, Maximum = 1 });

        foreach (var kind in Enum.GetValues<EmotionKind>())
        {
            if (!samples.Any(s => s.Emotions[kind] > 0.01f))
            {
                continue;
            }

            var series = new LineSeries { Title = kind.ToString(), MarkerType = MarkerType.Circle };
            foreach (var sample in samples)
            {
                series.Points.Add(new DataPoint(sample.Time, sample.Emotions[kind]));
            }

            model.Series.Add(series);
        }

        EmotionPlot.Model = model;
    }

    /// <summary>Строит MSAGL-граф из добавленных персонажей и прямых SocialRelation-рёбер между ними.</summary>
    private void RefreshGraph_Click(object sender, RoutedEventArgs e)
    {
        if (!_graphViewerBound)
        {
            _graphViewer.BindToPanel(GraphHost);
            _graphViewerBound = true;
        }

        var graph = new Microsoft.Msagl.Drawing.Graph("relationships");

        foreach (var npc in Npcs)
        {
            graph.AddNode(npc.EntityId.ToString());
        }

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
                    var edge = graph.AddEdge(from.EntityId.ToString(), to.EntityId.ToString());
                    edge.LabelText = $"liking={relation.Liking:F2}";
                    edge.Attr.Color = relation.Liking >= 0
                        ? Microsoft.Msagl.Drawing.Color.Green
                        : Microsoft.Msagl.Drawing.Color.Red;
                }
            }
        }

        _graphViewer.Graph = graph;
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
