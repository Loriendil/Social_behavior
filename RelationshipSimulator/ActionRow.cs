using System.ComponentModel;
using System.Runtime.CompilerServices;
using RelationshipCore.Dynamics;

namespace RelationshipSimulator;

/// <summary>Строка DataGrid для одного действия в словаре игры — тонкая обёртка над ActionDictionary.</summary>
public sealed class ActionRow : INotifyPropertyChanged
{
    private readonly ActionDictionary _actions;
    private readonly ActionId _actionId;

    public ActionRow(ActionDictionary actions, int actionId)
    {
        _actions = actions;
        _actionId = new ActionId(actionId);
    }

    public int ActionId => _actionId.Value;

    public float Effect
    {
        get => _actions.GetEffect(_actionId);
        set
        {
            _actions.SetEffect(_actionId, value);
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
