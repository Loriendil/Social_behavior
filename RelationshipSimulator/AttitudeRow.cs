using System.ComponentModel;
using System.Runtime.CompilerServices;
using RelationshipCore.Dynamics;
using RelationshipCore.Simulation;

namespace RelationshipSimulator;

/// <summary>
/// Строка DataGrid для одной записи attitude(владелец, целевая сущность) — задача 1
/// (attitudes к произвольным сущностям, не только к себе). OwnerId и TargetEntityId
/// редактируемы, поэтому NpcState владельца ищется через движок в момент чтения/записи
/// Attitude, а не хранится захваченной ссылкой.
/// </summary>
public sealed class AttitudeRow : INotifyPropertyChanged
{
    private readonly SocialDynamicsEngine _engine;
    private int _ownerId;
    private int _targetEntityId;

    public AttitudeRow(SocialDynamicsEngine engine, int ownerId, int targetEntityId)
    {
        _engine = engine;
        _ownerId = ownerId;
        _targetEntityId = targetEntityId;
    }

    public int OwnerId
    {
        get => _ownerId;
        set
        {
            _ownerId = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Attitude));
        }
    }

    public int TargetEntityId
    {
        get => _targetEntityId;
        set
        {
            _targetEntityId = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Attitude));
        }
    }

    public float Attitude
    {
        get => TryGetOwner(out var owner) ? owner.Appraisal.GetAttitude(_targetEntityId) : 0f;
        set
        {
            if (TryGetOwner(out var owner))
            {
                owner.Appraisal.SetAttitude(_targetEntityId, value);
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
