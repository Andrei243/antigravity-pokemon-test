using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

public class MoveData
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public PokemonType Type { get; set; }
    public MoveCategory Category { get; set; }
    public int Power { get; set; }
    public int Accuracy { get; set; } // 1-100, or 0 for never-miss
    public int MaxPP { get; set; }
    public int Priority { get; set; } = 0;
    public string Description { get; set; } = string.Empty;

    // Secondary effect definitions
    public StatusCondition InflictStatus { get; set; } = StatusCondition.None;
    public int StatusChancePercent { get; set; } = 0;
    public StatType? TargetStatChange { get; set; }
    public int StatStageAmount { get; set; } = 0; // +1, -1, etc.
    public bool StatChangeTargetSelf { get; set; } = false;
    public int StatChangeChancePercent { get; set; } = 0;
    public int RecoilPercent { get; set; } = 0;
    public int DrainPercent { get; set; } = 0;
    public int CritStage { get; set; } = 0;

    /// <summary>More stats changed by the same amount and on the same Pokémon as <see cref="TargetStatChange"/> (Close Combat).</summary>
    public StatType[] AlsoChangesStats { get; set; } = System.Array.Empty<StatType>();
    public int FlinchChancePercent { get; set; } = 0;
    public int ConfuseChancePercent { get; set; } = 0;
    public MoveTarget Target { get; set; } = MoveTarget.Selected;
    public MoveFlags Flags { get; set; } = MoveFlags.None;

    /// <summary>Thaws a frozen user before it attacks (Flame Wheel).</summary>
    public bool ThawsUser { get; set; }

    /// <summary>Restores this share of the user's max HP (Synthesis, Recover).</summary>
    public int HealPercent { get; set; } = 0;

    public bool MakesContact => (Flags & MoveFlags.Contact) != 0;
    public bool HitsSeveral => Target is MoveTarget.AllFoes or MoveTarget.AllOthers;
}

public class Move
{
    public MoveData Data { get; }
    public int CurrentPP { get; set; }
    public int MaxPP => Data.MaxPP;

    public string Name => Data.Name;
    public PokemonType Type => Data.Type;
    public MoveCategory Category => Data.Category;
    public int Power => Data.Power;
    public int Accuracy => Data.Accuracy;
    public int Priority => Data.Priority;
    public string Description => Data.Description;
    public MoveTarget Target => Data.Target;

    public Move(MoveData data)
    {
        Data = data;
        CurrentPP = data.MaxPP;
    }

    public Move(MoveData data, int currentPP)
    {
        Data = data;
        CurrentPP = currentPP;
    }

    public void RestorePP()
    {
        CurrentPP = MaxPP;
    }
}
