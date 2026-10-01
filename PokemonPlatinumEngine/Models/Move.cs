using System.Text.Json.Serialization;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

public class MoveData
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>The generation that introduced the move (0 for a move made for this game).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Generation { get; set; }
    public PokemonType Type { get; set; }
    public MoveCategory Category { get; set; }
    public int Power { get; set; }
    public int Accuracy { get; set; } // 1-100, or 0 for never-miss
    public int MaxPP { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Priority { get; set; } = 0;
    public string Description { get; set; } = string.Empty;

    // Secondary effect definitions
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public StatusCondition InflictStatus { get; set; } = StatusCondition.None;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int StatusChancePercent { get; set; } = 0;
    public StatType? TargetStatChange { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int StatStageAmount { get; set; } = 0; // +1, -1, etc.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool StatChangeTargetSelf { get; set; } = false;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int StatChangeChancePercent { get; set; } = 0;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int RecoilPercent { get; set; } = 0;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int DrainPercent { get; set; } = 0;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int CritStage { get; set; } = 0;

    /// <summary>More stats changed by the same amount and on the same Pokémon as <see cref="TargetStatChange"/> (Close Combat).</summary>
    public StatType[]? AlsoChangesStats { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int FlinchChancePercent { get; set; } = 0;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int ConfuseChancePercent { get; set; } = 0;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public MoveTarget Target { get; set; } = MoveTarget.Selected;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public MoveFlags Flags { get; set; } = MoveFlags.None;

    /// <summary>Thaws a frozen user before it attacks (Flame Wheel).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool ThawsUser { get; set; }

    /// <summary>Restores this share of the user's max HP (Synthesis, Recover).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int HealPercent { get; set; } = 0;

    /// <summary>
    /// What the move does beyond its data fields (<c>MultiHit</c>, <c>Protect</c>, <c>RechargeAfter</c>), named after the
    /// decompilation's battle effect or, for later moves, the first move that has it. Null when the fields say it all.
    /// </summary>
    public string? Effect { get; set; }

    /// <summary>The chance of <see cref="Effect"/>, when it has one (Tri Attack's 20%).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int EffectChance { get; set; }

    /// <summary>How much of the move the engine runs; anything but Full has an <see cref="Effect"/> still to write.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public MoveEffectSupport Support { get; set; } = MoveEffectSupport.Full;

    [JsonIgnore]
    public bool MakesContact => (Flags & MoveFlags.Contact) != 0;
    [JsonIgnore]
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
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
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
