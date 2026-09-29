using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

public enum ActionType
{
    Fight,
    Switch,
    UseItem,
    Run
}

public class BattleAction
{
    public ActionType Type { get; set; }
    public bool IsPlayer { get; set; }
    public Move? Move { get; set; }
    public int SwitchToIndex { get; set; } = -1;
    public ItemData? Item { get; set; }
    public int TargetPartyIndex { get; set; } = 0;

    public int Priority => Type switch
    {
        ActionType.Run => 99,
        ActionType.UseItem => 80,
        ActionType.Switch => 70,
        ActionType.Fight => Move?.Priority ?? 0,
        _ => 0
    };
}
