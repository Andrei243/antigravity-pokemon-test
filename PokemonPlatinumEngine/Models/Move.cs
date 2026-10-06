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

    /// <summary>
    /// The decompilation's own battle effect of one of Platinum's moves (<c>AtkUp2</c> for Swords Dance, <c>Hit</c> for
    /// Tackle), whatever the fields say: what the trainer AI asks of a move (plan 06 · R9). Null for the later moves.
    /// </summary>
    public string? BattleEffect
    {
        get => battleEffect;
        set { battleEffect = value; effectId = null; }
    }

    private string? battleEffect;
    private Battle.Sim.Ai.MoveEffectId? effectId;

    /// <summary><see cref="BattleEffect"/> as the AI's enum; <see cref="Battle.Sim.Ai.MoveEffectId.Hit"/> for a move Platinum doesn't have.</summary>
    [JsonIgnore]
    public Battle.Sim.Ai.MoveEffectId EffectId =>
        effectId ??= battleEffect != null && Enum.TryParse(battleEffect.Length > 0 && char.IsDigit(battleEffect[0]) ? "N" + battleEffect : battleEffect, out Battle.Sim.Ai.MoveEffectId id)
            ? id : Battle.Sim.Ai.MoveEffectId.Hit;

    /// <summary>How much of the move the engine runs; anything but Full has an <see cref="Effect"/> still to write.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public MoveEffectSupport Support { get; set; } = MoveEffectSupport.Full;

    /// <summary>An ordinary move, or one a later mechanic makes out of another (a Z-Move, a Max Move).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public MoveKind Kind { get; set; }

    /// <summary>
    /// The power of the Z-Move a type's Z-Crystal turns this move into, and of the Max Move it becomes under
    /// Dynamax (damaging moves; 0 for the rest). From the newest games' values, whatever rules the game is
    /// played by, since the mechanics are theirs.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int ZPower { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int MaxPower { get; set; }

    /// <summary>What a status move gains when it is used through a Z-Crystal, before its own effect.</summary>
    public ZBonus? ZBonus { get; set; }

    /// <summary>The species whose Gigantamax form uses this G-Max Move.</summary>
    public string? GigantamaxOf { get; set; }

    /// <summary>
    /// What the newest games give the move where that differs from Platinum's own values above (Tackle: 40 power,
    /// 100% accurate). Null when nothing differs, and for every move that came after Platinum. The values above
    /// are the ones in force: <see cref="Ruleset.Use"/> swaps these in for a game played by the modern rules.
    /// </summary>
    public MoveValues? Modern { get; set; }

    [JsonIgnore]
    public bool MakesContact => (Flags & MoveFlags.Contact) != 0;
    [JsonIgnore]
    public bool HitsSeveral => Target is MoveTarget.AllFoes or MoveTarget.AllOthers;

    // Platinum's values, kept from the first time the rules replaced them
    private MoveValues? platinum;

    /// <summary>The move as these rules have it, leaving this one as it is: for tests and tools, which never change
    /// the rules of the game in progress.</summary>
    public MoveData Under(Ruleset rules)
    {
        if (Modern == null) return this;
        var copy = (MoveData)MemberwiseClone();
        copy.platinum = platinum ?? Values();
        copy.Take(rules.ModernMoveValues ? Modern : null);
        return copy;
    }

    /// <summary>Gives the move the values these rules say, in place, so every Pokémon that knows it follows.</summary>
    internal void UseRules(Ruleset rules)
    {
        if (Modern == null) return;
        platinum ??= Values();
        Take(rules.ModernMoveValues ? Modern : null);
    }

    /// <summary>The move with another type for one use (Weather Ball under a sky, Hidden Power), leaving this one as it is.</summary>
    public MoveData OfType(PokemonType type)
    {
        if (type == Type) return this;
        var copy = (MoveData)MemberwiseClone();
        copy.Type = type;
        return copy;
    }

    private MoveValues Values() =>
        new() { Power = Power, Accuracy = Accuracy, MaxPP = MaxPP, Priority = Priority, Type = Type, Category = Category };

    private void Take(MoveValues? values)
    {
        Power = values?.Power ?? platinum!.Power!.Value;
        Accuracy = values?.Accuracy ?? platinum!.Accuracy!.Value;
        MaxPP = values?.MaxPP ?? platinum!.MaxPP!.Value;
        Priority = values?.Priority ?? platinum!.Priority!.Value;
        Type = values?.Type ?? platinum!.Type!.Value;
        Category = values?.Category ?? platinum!.Category!.Value;
    }
}

/// <summary>What sort of move an entry of <c>moves.json</c> is. Only <see cref="Standard"/> moves are learned.</summary>
public enum MoveKind
{
    Standard,
    /// <summary>What a Z-Crystal turns a move into: one for each type, and those of single species.</summary>
    ZMove,
    /// <summary>What a move becomes under Dynamax: one for each type, and Max Guard for status moves.</summary>
    MaxMove,
    /// <summary>The Max Move of one species' Gigantamax form (<see cref="MoveData.GigantamaxOf"/>).</summary>
    GMaxMove
}

/// <summary>
/// Z-Power on a status move: an effect by name (<c>ClearNegativeBoost</c>, <c>Heal</c>, <c>HealReplacement</c>,
/// <c>Crit2</c>, <c>Redirect</c>, <c>Curse</c>), or these stats raised by this many stages.
/// </summary>
public class ZBonus
{
    public string? Effect { get; set; }
    public StatType[]? Stats { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Stages { get; set; }
}

/// <summary>The values of a move that a later generation changed; only the ones that differ are given.</summary>
public class MoveValues
{
    public int? Power { get; set; }
    public int? Accuracy { get; set; }
    public int? MaxPP { get; set; }
    public int? Priority { get; set; }
    public PokemonType? Type { get; set; }
    public MoveCategory? Category { get; set; }
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
