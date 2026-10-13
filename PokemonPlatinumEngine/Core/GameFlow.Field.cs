using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The game's objects (plan 16 · T11b): the place and the player in it, what the player carries and has seen, the
/// story with the script under way, and the battle being fought. The flow holds them; the engine draws them.
/// </summary>
public sealed partial class GameFlow
{
    /// <summary>The map the player is on. Not valid before a game has begun or been continued.</summary>
    public Map Map { get; set; } = null!;

    /// <summary>The player on <see cref="Map"/>. Not valid before a game has begun or been continued.</summary>
    public Player Player { get; set; } = null!;

    /// <summary>The team.</summary>
    public Party Party { get; } = new();

    /// <summary>The bag.</summary>
    public Inventory Bag { get; } = new();

    /// <summary>The Pokédex.</summary>
    public Pokedex Pokedex { get; } = new();

    /// <summary>The PC's boxes; a new game or a loaded save gives them afresh.</summary>
    public PcBoxes Boxes { get; set; } = new();

    /// <summary>The player's money, by reference: the shops' screens change it where it is kept.</summary>
    public ref int Money => ref money;
    private int money = 3000;

    /// <summary>What the story remembers: flags, variables, trainers beaten, items taken, the badges, the starters (plan 02 · S1).</summary>
    public StoryState Story { get; } = new();

    /// <summary>The script library the story's scripts come from, read as the engine starts.</summary>
    public ScriptLibrary Scripts { get; set; } = null!;

    /// <summary>The runner of the script under way, made as the engine starts with the game's host.</summary>
    public ScriptRunner Runner { get; set; } = null!;

    /// <summary>The battle being fought, or the last one until another begins; null before the first.</summary>
    public BattleEngine? Battle { get; set; }
}
