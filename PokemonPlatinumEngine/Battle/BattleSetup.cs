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

/// <summary>Everything a battle starts from.</summary>
public sealed class BattleSetup
{
    public required Party PlayerParty { get; init; }
    public required Inventory Inventory { get; init; }
    public required Pokedex Pokedex { get; init; }

    /// <summary>Where caught Pokémon go when the party is full.</summary>
    public List<Pokemon>? PcStorage { get; init; }

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

    /// <summary>The trainer's first Pokémon when it isn't the first in their party (the old constructor's foe argument).</summary>
    internal Pokemon? FirstTrainerPokemon { get; init; }
}
