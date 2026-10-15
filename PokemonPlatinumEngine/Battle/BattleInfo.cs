using PokemonPlatinumEngine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

/// <summary>
/// The abilities and held items the battle's lines have named (plan 12 · Q11), by the screen's own Pokémon: what the
/// player has been told, and so all the information panel may say of a Pokémon that isn't the player's. Filled by
/// the log's <see cref="AbilityShown"/> and <see cref="ItemShown"/> as their lines appear, never by reading the lines.
/// </summary>
public sealed class Revealed
{
    private readonly Dictionary<Pokemon, string> abilities = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<Pokemon, string> items = new(ReferenceEqualityComparer.Instance);

    public void Ability(Pokemon pokemon, string ability) => abilities[pokemon] = ability;
    public void Item(Pokemon pokemon, string item) => items[pokemon] = item;

    /// <summary>The ability a line last named for this Pokémon, null while none has.</summary>
    public string? AbilityOf(Pokemon pokemon) => abilities.GetValueOrDefault(pokemon);

    /// <summary>The item a line last named for this Pokémon, null while none has.</summary>
    public string? ItemOf(Pokemon pokemon) => items.GetValueOrDefault(pokemon);
}

/// <summary>How a line of the panel bears on whoever it is about: a help, a harm, or neither.</summary>
public enum InfoTone { Plain, Good, Bad }

/// <summary>One state of the field, a side or a Pokémon: its name and, where the player can know it, a count ("4 turns", "×2", "+2").</summary>
public sealed record InfoLine(string Name, string Detail = "", InfoTone Tone = InfoTone.Plain)
{
    public string Text => Detail.Length == 0 ? Name : $"{Name} {Detail}";
}

/// <summary>
/// A Pokémon on the field as the panel tells of it. Its ability and item are null where the player hasn't been
/// told them; an item the player saw go and that nothing has replaced is "None".
/// </summary>
public sealed record PokemonInfo(Battler Place, Pokemon Pokemon, string? Ability, string? Item,
    IReadOnlyList<InfoLine> Stages, IReadOnlyList<InfoLine> Conditions);

/// <summary>A side of the field: what it has up and lying about, and its Pokémon.</summary>
public sealed record SideInfo(BattleSide Side, IReadOnlyList<InfoLine> Lines, IReadOnlyList<PokemonInfo> Pokemon);

/// <summary>
/// What the information panel shows (plan 12 · Q11; style guide, "Battle panels"): the field's weather, Trick
/// Room and Gravity, each side's screens, veils and hazards, and each Pokémon's ability, item, stat stages and
/// passing conditions. GPU-free, made from the screen's battlers whenever a menu is open (they are the rules' then).
/// <para>
/// It never says what the player hasn't been told: a Pokémon that isn't the player's own shows the ability and the
/// item a line has named (<see cref="Revealed"/>) and nothing else; and a count is given only where nothing hidden
/// lengthens it. A held rock lengthens the weather and Light Clay the screens, so the weather has no count and
/// the screens over the foe's side none; a count the rules draw at random (confusion, Encore, a bind) is never
/// given either.
/// </para>
/// </summary>
public sealed class BattleInfo
{
    /// <summary>The weather, Trick Room and Gravity: the strip under the foe's box.</summary>
    public IReadOnlyList<InfoLine> Field { get; }

    /// <summary>The player's side and the foe's (indexed by <see cref="BattleSide"/>).</summary>
    public IReadOnlyList<SideInfo> Sides { get; }

    private BattleInfo(IReadOnlyList<InfoLine> field, IReadOnlyList<SideInfo> sides)
    {
        Field = field;
        Sides = sides;
    }

    public SideInfo Side(BattleSide side) => Sides[(int)side];

    /// <summary>Every line of the panel, in order: the field's, then each side's with its Pokémon's.</summary>
    public IEnumerable<string> Lines =>
        Field.Select(l => l.Text).Concat(Sides.SelectMany(s => s.Lines.Select(l => l.Text)
            .Concat(s.Pokemon.SelectMany(p => p.Stages.Concat(p.Conditions).Select(l => $"{p.Pokemon.DisplayName}: {l.Text}")))));

    public static BattleInfo Of(FieldState field, IReadOnlyList<Battler> player, IReadOnlyList<Battler> enemy, Revealed revealed)
    {
        var sides = new[] { player, enemy }
            .Select((slots, side) => new SideInfo((BattleSide)side, SideLines(field, (BattleSide)side),
                slots.Where(b => b.IsActive).Select(b => Card(b, field, revealed)).ToList()))
            .ToList();
        return new BattleInfo(FieldLines(field), sides);
    }

    private static string Turns(int turns) => Text.Of("battleInfo.turns", ("n", turns));

    private static List<InfoLine> FieldLines(FieldState field)
    {
        var lines = new List<InfoLine>();
        if (WeatherName(field.Weather) is { } weather) lines.Add(new(weather));
        if (field.TrickRoom) lines.Add(new(Text.Of("battleInfo.trickRoom"), Turns(field.TrickRoomTurns)));
        if (field.Gravity) lines.Add(new(Text.Of("battleInfo.gravity"), Turns(field.GravityTurns)));
        return lines;
    }

    public static string? WeatherName(BattleWeather weather) => weather switch
    {
        BattleWeather.Rain => Text.Of("battleInfo.rain"),
        BattleWeather.Sandstorm => Text.Of("battleInfo.sandstorm"),
        BattleWeather.Sun => Text.Of("battleInfo.sun"),
        BattleWeather.Hail => Text.Of("battleInfo.hail"),
        BattleWeather.Fog => Text.Of("battleInfo.fog"),
        _ => null
    };

    private static List<InfoLine> SideLines(FieldState field, BattleSide side)
    {
        var s = field.Side(side);
        bool ours = side == BattleSide.Player;
        var lines = new List<InfoLine>();
        // Light Clay lengthens a screen, so the foe's are named without their count
        if (s.Reflect) lines.Add(new(Text.Of("battleInfo.reflect"), ours ? Turns(s.ReflectTurns) : "", InfoTone.Good));
        if (s.LightScreen) lines.Add(new(Text.Of("battleInfo.lightScreen"), ours ? Turns(s.LightScreenTurns) : "", InfoTone.Good));
        if (s.Mist) lines.Add(new(Text.Of("battleInfo.mist"), Turns(s.MistTurns), InfoTone.Good));
        if (s.Safeguard) lines.Add(new(Text.Of("battleInfo.safeguard"), Turns(s.SafeguardTurns), InfoTone.Good));
        if (s.Tailwind) lines.Add(new(Text.Of("battleInfo.tailwind"), Turns(s.TailwindTurns), InfoTone.Good));
        if (s.LuckyChant) lines.Add(new(Text.Of("battleInfo.luckyChant"), Turns(s.LuckyChantTurns), InfoTone.Good));
        if (s.Spikes > 0) lines.Add(new(Text.Of("battleInfo.spikes"), Text.Of("battleInfo.times", ("n", s.Spikes)), InfoTone.Bad));
        if (s.ToxicSpikes > 0) lines.Add(new(Text.Of("battleInfo.toxicSpikes"), Text.Of("battleInfo.times", ("n", s.ToxicSpikes)), InfoTone.Bad));
        if (s.StealthRock) lines.Add(new(Text.Of("battleInfo.stealthRock"), "", InfoTone.Bad));
        return lines;
    }

    /// <summary>The stats in the order the summary lists them, as a stage is told: "Attack +2".</summary>
    private static readonly StatType[] StageOrder =
        { StatType.Attack, StatType.Defense, StatType.SpAttack, StatType.SpDefense, StatType.Speed, StatType.Accuracy, StatType.Evasion };

    public static string StageName(StatType stat) => stat switch
    {
        StatType.Accuracy => Text.Of("battleInfo.accuracy"),
        StatType.Evasion => Text.Of("battleInfo.evasion"),
        _ => BattleText.StatName(stat)
    };

    private static PokemonInfo Card(Battler b, FieldState field, Revealed revealed)
    {
        var p = b.Pokemon!;
        // The player knows their own team; a partner's and the foes' are known only as far as the lines have named them
        bool own = b.IsPlayerSide && b.Trainer == null;
        string ability = b.Ability?.Name ?? p.AbilityName ?? "";
        string? item = p.HeldItem?.Name;
        string? shownAbility = own || revealed.AbilityOf(p) == ability ? ability : null;
        string? shownItem = own ? item ?? Text.Of("battleInfo.none")
            : revealed.ItemOf(p) is not { } seen ? null
            : item == null ? Text.Of("battleInfo.none")
            : seen == item ? item : null;

        var stages = new List<InfoLine>();
        foreach (var stat in StageOrder)
        {
            int stage = p.StatStages.GetValueOrDefault(stat);
            if (stage != 0) stages.Add(new(StageName(stat), stage > 0 ? $"+{stage}" : $"-{-stage}", stage > 0 ? InfoTone.Good : InfoTone.Bad));
        }
        return new PokemonInfo(b, p, shownAbility, shownItem, stages, Conditions(b, field.At(b.Place)));
    }

    private static List<InfoLine> Conditions(Battler b, PlaceState place)
    {
        var v = b.Volatile;
        var lines = new List<InfoLine>();
        void Bad(string name, string detail = "") => lines.Add(new(name, detail, InfoTone.Bad));
        void Good(string name, string detail = "") => lines.Add(new(name, detail, InfoTone.Good));

        if (b.IsConfused) Bad(Text.Of("battleInfo.confused"));
        if (v.InLoveWith != null) Bad(Text.Of("battleInfo.infatuated"));
        if (v.SeededBy != null) Bad(Text.Of("battleInfo.seeded"));
        if (v.Cursed) Bad(Text.Of("battleInfo.cursed"));
        if (v.Nightmare) Bad(Text.Of("battleInfo.nightmare"));
        if (v.YawnTurns > 0) Bad(Text.Of("battleInfo.drowsy"));
        if (v.PerishCount >= 0) Bad(Text.Of("battleInfo.perishCount"), v.PerishCount.ToString());
        if (v.TauntTurns > 0) Bad(Text.Of("battleInfo.taunted"));
        if (v.Tormented) Bad(Text.Of("battleInfo.tormented"));
        if (v.Encored != null) Bad(Text.Of("battleInfo.encore", ("move", v.Encored.Name)));
        if (v.Disabled != null) Bad(Text.Of("battleInfo.disabled", ("move", v.Disabled.Name)));
        if (v.HealBlockTurns > 0) Bad(Text.Of("battleInfo.healBlock"), Turns(v.HealBlockTurns));
        if (v.EmbargoTurns > 0) Bad(Text.Of("battleInfo.embargo"), Turns(v.EmbargoTurns));
        if (v.BindTurns > 0 && v.BindingMove.Length > 0) Bad(Text.Of("battleInfo.bound", ("move", v.BindingMove)));
        if (v.TrappedBy != null) Bad(Text.Of("battleInfo.cantEscape"));
        if (v.Identified || v.MiracleEye) Bad(Text.Of("battleInfo.identified"));
        if (v.LockedOnBy != null) Bad(Text.Of("battleInfo.takenAimAt"));
        if (v.AbilitySuppressed) Bad(Text.Of("battleInfo.abilitySuppressed"));

        if (b.HasSubstitute) Good(Text.Of("battleInfo.substitute"));
        if (v.FocusEnergy) Good(Text.Of("battleInfo.focusEnergy"));
        if (v.Ingrained) Good(Text.Of("battleInfo.ingrained"));
        if (v.AquaRing) Good(Text.Of("battleInfo.aquaRing"));
        if (v.MagnetRiseTurns > 0) Good(Text.Of("battleInfo.magnetRise"), Turns(v.MagnetRiseTurns));
        if (v.ChargeTurns > 0) Good(Text.Of("battleInfo.charged"));
        if (b.FlashFire) Good(Text.Of("battleInfo.flashFire"));
        if (v.Stockpile > 0) Good(Text.Of("battleInfo.stockpile"), Text.Of("battleInfo.times", ("n", v.Stockpile)));
        if (v.DefenseCurl) Good(Text.Of("battleInfo.defenseCurl"));
        if (v.Minimized) Good(Text.Of("battleInfo.minimized"));
        if (v.Rage) Good(Text.Of("battleInfo.rage"));
        if (v.Transformed) Good(Text.Of("battleInfo.transformed"));
        if (v.PowerTrick) Good(Text.Of("battleInfo.powerTrick"));

        // What waits for the place itself, whoever stands there when it comes
        if (place.WishTurns > 0) Good(Text.Of("battleInfo.wish"), Turns(place.WishTurns));
        if (place.DoomTurns > 0 && place.DoomMove != null) Bad(place.DoomMove.Name, Turns(place.DoomTurns));
        return lines;
    }
}
