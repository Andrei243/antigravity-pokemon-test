using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

/// <summary>
/// The battles that play by rules of their own (the original's battle types beyond trainer, wild and double;
/// plan 06 · R9).
/// </summary>
public enum BattleKind
{
    /// <summary>An ordinary battle.</summary>
    Normal,
    /// <summary>A roaming Pokémon: it runs on its first chance unless it is held.</summary>
    Roamer,
    /// <summary>The assistant's catching lesson on Route 202: the assistant's Pokémon uses a move by itself, then a ball is thrown that can't fail.</summary>
    CatchingLesson,
    /// <summary>The Great Marsh: Safari Balls, bait and mud, no Pokémon of the player's, a Pokémon that may run any turn.</summary>
    Safari,
    /// <summary>Pal Park's catching show: Park Balls that can't fail, no Pokémon of the player's.</summary>
    PalPark
}

public enum BattleFormat
{
    /// <summary>One Pokémon per side.</summary>
    Single,
    /// <summary>Two Pokémon per side (twins, Galactic pairs, two trainers who spot the player together).</summary>
    Double,
    /// <summary>
    /// A horde (plan 14 · B3, X and Y's): one of the player's Pokémon against up to five wild ones at once. Fought only
    /// under rules that have it (<see cref="Data.Ruleset.Hordes"/>); anywhere else it falls back (<see cref="BattleFormats.FallBack"/>).
    /// </summary>
    Horde,
    /// <summary>
    /// An SOS battle (plan 14 · B5, Sun and Moon's): one wild Pokémon against the player's one, with a second place on the
    /// wild side left empty for an ally it calls for help. Fought only under rules that have it
    /// (<see cref="Data.Ruleset.SosCalls"/>); anywhere else it is the single it began as (<see cref="BattleFormats.FallBack"/>).
    /// </summary>
    Sos,
    /// <summary>
    /// A triple battle (plan 14 · B6, Black and White's): three places a side, and a move aimed at one reaches only the
    /// places beside its user and across from it (<see cref="Sim.BattleCore.InReach"/>). Fought only under rules that have
    /// it (<see cref="Data.Ruleset.Triples"/>) with three able to fight on each side; anywhere else it is the double, or the
    /// single, the sides can field (<see cref="BattleFormats.FallBack"/>).
    /// </summary>
    Triple
}

/// <summary>What each format is made of (plan 14 · B3): how many places a side has, and what a battle asked for becomes.</summary>
public static class BattleFormats
{
    /// <summary>
    /// The most places a side of a battle in this format has: one each in a single, two in a double, and in a horde
    /// one for the player and five for the wild Pokémon (a horde of fewer is fought with as many as it has).
    /// </summary>
    public static int PlacesOf(this BattleFormat format, BattleSide side) => format switch
    {
        BattleFormat.Double => 2,
        BattleFormat.Horde => side == BattleSide.Enemy ? 5 : 1,
        // The caller's place and the one an ally fills (plan 14 · B5)
        BattleFormat.Sos => side == BattleSide.Enemy ? 2 : 1,
        // Three a side (plan 14 · B6)
        BattleFormat.Triple => 3,
        _ => 1
    };

    /// <summary>
    /// The format a battle asked for is fought in, by who can stand on each side. A double needs two able to fight on
    /// each side (a partner brings the player's second; a partner with nobody able is left out, <paramref name="partnerStays"/>),
    /// and is never the Great Marsh's or Pal Park's; a horde needs rules that have it, at least two wild Pokémon, a
    /// player alone with one able to fight and an ordinary battle, and falls back to a single, or to the double a
    /// partner makes. Everything else is a single.
    /// </summary>
    public static BattleFormat FallBack(BattleFormat asked, int ownAble, int partnerAble, bool partner, int trainers, int foesAble, int wild,
        BattleKind kind, Data.Ruleset rules, out bool partnerStays)
    {
        bool foesForTwo = trainers == 0 ? wild >= 2 : trainers >= 2 || foesAble >= 2;
        bool canDouble = partner ? ownAble > 0 && partnerAble > 0 && foesForTwo : ownAble >= 2 && foesForTwo;
        partnerStays = partner && canDouble;
        bool special = kind is BattleKind.Safari or BattleKind.PalPark;
        if (asked == BattleFormat.Horde && !partnerStays && rules.Hordes && trainers == 0 && wild >= 2 && ownAble > 0 && kind == BattleKind.Normal)
            return BattleFormat.Horde;
        // An SOS battle (plan 14 · B5): one wild Pokémon met by the player alone, under rules that have calls for help
        if (asked == BattleFormat.Sos && !partner && rules.SosCalls && trainers == 0 && wild == 1 && ownAble > 0 && kind == BattleKind.Normal)
            return BattleFormat.Sos;
        // A triple (plan 14 · B6): the player alone with three able to fight, against one trainer's three or three wild
        // Pokémon, under rules that have it; what can't be one is asked for as a double
        if (asked == BattleFormat.Triple)
        {
            if (!partner && rules.Triples && ownAble >= 3 && (trainers == 0 ? wild >= 3 : trainers == 1 && foesAble >= 3) && kind == BattleKind.Normal)
                return BattleFormat.Triple;
            asked = BattleFormat.Double;
        }
        return (asked == BattleFormat.Double || partnerStays) && canDouble && !special ? BattleFormat.Double : BattleFormat.Single;
    }
}

/// <summary>
/// What a script or a venue asks of a battle beyond who fights it and where (plan 14): the formats of the later
/// games that are a flag on the battle. The game lays them over the place's own conditions as the battle is made
/// (<see cref="Sim.BattleConditions"/>), never into a battle that is running. B1: an inverse battle, whose type
/// chart is turned round (<see cref="Sim.BattleConditions.Inverse"/>). B14: a Master Trainer's battle, in which nothing
/// may be used from the bag by either side (<see cref="NoBattleItems"/>) and the player's side is one Pokémon of the
/// team alone (<see cref="Alone"/>): the team's own, not a copy, so what the battle does to it stays.
/// </summary>
public sealed record BattleTerms(bool Inverse = false, bool NoBattleItems = false, Pokemon? Alone = null);

/// <summary>Everything a battle starts from.</summary>
public sealed class BattleSetup
{
    public required Party PlayerParty { get; init; }
    public required Inventory Inventory { get; init; }
    public required Pokedex Pokedex { get; init; }

    /// <summary>Where caught Pokémon go when the party is full.</summary>
    public PcBoxes? PcStorage { get; init; }

    /// <summary>The name of the place it is fought in: where a Pokémon caught in it was met (plan 06 · R12).</summary>
    public string? Place { get; init; }

    public BattleFormat Format { get; init; } = BattleFormat.Single;

    /// <summary>The wild Pokémon met (one, or two in a wild double battle). Empty in trainer battles.</summary>
    public List<Pokemon> WildPokemon { get; init; } = new();

    /// <summary>
    /// The opposing trainers: one, or two in a double battle where each sends one Pokémon at a time (two trainers
    /// at once). A single trainer in a double battle sends two at a time.
    /// </summary>
    public List<Trainer> Trainers { get; init; } = new();

    /// <summary>
    /// Random numbers for every roll in the battle (fixed seeds make tests repeatable). A
    /// <see cref="Sim.BattleRandom"/> also lets a test fix the rolls of one kind.
    /// </summary>
    public Random? Random { get; init; }

    /// <summary>The rules the battle is fought by; left out, those of the game in progress.</summary>
    public Data.Ruleset? Rules { get; init; }

    /// <summary>
    /// What the rules ask of the world outside the battle (water or a cave underfoot, night, which species the
    /// player has caught); left out, a battle on land by day, with the Pokédex answering for what was caught.
    /// </summary>
    public Sim.BattleConditions? Conditions { get; init; }

    /// <summary>What kind of battle it is (a roamer, the catching lesson, the Great Marsh, Pal Park).</summary>
    public BattleKind Kind { get; init; } = BattleKind.Normal;

    /// <summary>
    /// The trainer fighting beside the player in a tag battle (Cheryl in Eterna Forest, Dawn in Jubilife): their
    /// Pokémon stand in the player's second place and their AI chooses for them. The battle is a double one.
    /// </summary>
    public Trainer? Partner { get; init; }

    /// <summary>The player can't run (a legendary the story brings, the battles the original won't let anyone leave).</summary>
    public bool CannotFlee { get; init; }

    /// <summary>The game's first battle (the rival's on Route 201, <c>BATTLE_STATUS_FIRST_BATTLE</c>): no critical hits.</summary>
    public bool FirstBattle { get; init; }

    /// <summary>The Safari Balls or Park Balls the player has, in the Great Marsh and Pal Park.</summary>
    public int SpecialBalls { get; init; }

    /// <summary>The name the battle's lines give the player's side; left out, the player's (the catching lesson's is the assistant's).</summary>
    public string? PlayerName { get; init; }

    /// <summary>
    /// The look the figure on the player's platform is drawn in; left out, the player's own (the catching lesson's is
    /// whoever shows it: the assistant, or plan 21 · H2's Wally).
    /// </summary>
    public string? PlayerLook { get; init; }

    /// <summary>
    /// The move cards say how a damaging move will do against each foe already seen (plan 12 · Q10): presentation
    /// only, never read by the rules.
    /// </summary>
    public bool MoveHints { get; init; }

    /// <summary>
    /// The trainers across the field speak at the moments the rules mark (plan 11 · C12; <see cref="Sim.TrainerReacts"/>,
    /// <see cref="Data.TrainerLines"/>): presentation only, never read by the rules.
    /// </summary>
    public bool TrainerLines { get; init; }

    /// <summary>
    /// A battle of the Battle Frontier (plan 06 · R18a; the original's <c>BATTLE_TYPE_FRONTIER</c>): nothing from the
    /// bag, no EXP, no money either way, RUN is a forfeit, and the Pokédex is told nothing (<see cref="Sim.CoreSetup.Frontier"/>).
    /// The team it is handed is the battle's own copy (<see cref="Models.Frontier.FrontierRules.TeamFor"/>).
    /// </summary>
    public bool Frontier { get; init; }

    /// <summary>
    /// Nothing may be used from the bag, by either side (plan 14 · B14: a Master Trainer's battle; plan 13 · V5's run
    /// rule sets the same): the BAG command is greyed and says why (<see cref="Sim.CoreSetup.NoBattleItems"/>).
    /// </summary>
    public bool NoBattleItems { get; init; }

    /// <summary>
    /// Battle Style Shift (plan 08 · P1; the options' Battle Style): when a trainer's Pokémon faints with another to
    /// come, the player is asked whether to switch first (<see cref="Sim.CoreSetup.OffersSwitch"/>). Off unless given,
    /// which is Set: the game turns it on from its settings, and a battle a test or a tool builds itself asks nothing.
    /// </summary>
    public bool OffersSwitch { get; init; }

    /// <summary>
    /// The difficulty chosen at NEW GAME (plan 13 · V2; <see cref="Sim.CoreSetup.Difficulty"/>): Easy gives more EXP. The
    /// game hands it over from the run's rules; a battle a test or a tool builds itself is Normal unless it asks.
    /// </summary>
    public Difficulty Difficulty { get; init; } = Difficulty.Normal;

    /// <summary>
    /// The level the run's cap holds the player's Pokémon to (plan 13 · V3; <see cref="Sim.CoreSetup.RunLevelCap"/>), 0 for
    /// none. The game hands it over from <see cref="Models.LevelCaps.For"/>; a battle a test or a tool builds is uncapped.
    /// </summary>
    public int RunLevelCap { get; init; }

    /// <summary>
    /// The Nuzlocke's terms (plan 13 · V4; <see cref="Sim.CoreSetup.Nuzlocke"/>): no revive, and whether the wild Pokémon
    /// may be caught. The game hands them over under a Nuzlocke run; null for none, which changes nothing.
    /// </summary>
    public NuzlockeTerms? Nuzlocke { get; init; }

    /// <summary>
    /// The run's restrictions (plan 13 · V5; <see cref="Sim.CoreSetup.Restrictions"/>): no catching, no running, a Monotype.
    /// The game hands them over under a run that has any; null for none, which changes nothing.
    /// </summary>
    public RunTerms? Restrictions { get; init; }

    /// <summary>
    /// The options' Battle Scene (plan 08 · P1): on unless given, the moves and the turn's animations play; off, the
    /// face shows none of them and a line's hits land at once (<see cref="BattleEngine.Scene"/>). Presentation only,
    /// never read by the rules.
    /// </summary>
    public bool Scene { get; init; } = true;

    /// <summary>The trainer's first Pokémon when it isn't the first in their party (the old constructor's foe argument).</summary>
    internal Pokemon? FirstTrainerPokemon { get; init; }
}
