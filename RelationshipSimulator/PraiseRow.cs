using System.ComponentModel;
using System.Runtime.CompilerServices;
using RelationshipCore.Dynamics;
using RelationshipCore.Simulation;

namespace RelationshipSimulator;

/// <summary>
/// Строка DataGrid для одной записи praise(владелец, действие) — задача 2 (praiseworthiness
/// действий). Praise субъективен (Appraisal.GetPraise на NpcState владельца), а не часть
/// ActionDictionary — см. CLAUDE.md, «Архитектура объединения слоёв», п.4.
/// </summary>
public sealed class PraiseRow : INotifyPropertyChanged
{
    private readonly SocialDynamicsEngine _engine;
    private int _ownerId;
    private int _actionId;

    public PraiseRow(SocialDynamicsEngine engine, int ownerId, int actionId)
    {
        _engine = engine;
        _ownerId = ownerId;
        _actionId = actionId;
    }

    public int OwnerId
    {
        get => _ownerId;
        set
        {
            _ownerId = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Praise));
        }
    }

    public int ActionId
    {
        get => _actionId;
        set
        {
            _actionId = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Praise));
        }
    }

    public float Praise
    {
        get => TryGetOwner(out var owner) ? owner.Appraisal.GetPraise(new ActionId(_actionId)) : 0f;
        set
        {
            if (TryGetOwner(out var owner))
            {
                owner.Appraisal.SetPraise(new ActionId(_actionId), value);
            }

            OnPropertyChanged();
        }
    }

    private bool TryGetOwner(out NpcState owner)
    {
        try
        {
            owner = _engine.GetState(_ownerId);
            return true;
        }
        catch (InvalidOperationException)
        {
            owner = null!;
            return false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
