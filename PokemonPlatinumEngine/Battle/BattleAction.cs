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

    /// <summary>The place acting, and the Pokémon that stood there when the action was chosen.</summary>
    public Battler? User { get; set; }
    public Pokemon? Actor { get; set; }

    /// <summary>The place a single-target move was aimed at (null = the engine picks a foe).</summary>
    public Battler? Target { get; set; }

    // Filled in when the turn's order is worked out
    internal int Speed;
    internal bool QuickClaw;
    internal int Tiebreak;

    public int Priority => Type switch
    {
        ActionType.Run => 99,
        ActionType.UseItem => 80,
        ActionType.Switch => 70,
        ActionType.Fight => Move?.Priority ?? 0,
        _ => 0
    };
}
