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
    Double
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
    /// The options' Battle Scene (plan 08 · P1): on unless given, the moves and the turn's animations play; off, the
    /// face shows none of them and a line's hits land at once (<see cref="BattleEngine.Scene"/>). Presentation only,
    /// never read by the rules.
    /// </summary>
    public bool Scene { get; init; } = true;

    /// <summary>The trainer's first Pokémon when it isn't the first in their party (the old constructor's foe argument).</summary>
    internal Pokemon? FirstTrainerPokemon { get; init; }
}
