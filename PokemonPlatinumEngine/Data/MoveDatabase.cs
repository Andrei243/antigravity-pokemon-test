using System.Collections.Generic;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Data;

/// <summary>Every move, read from <c>Data/moves.json</c> the first time it is used.</summary>
public static class MoveDatabase
{
    public const string FileName = "moves.json";

    private static readonly Dictionary<string, MoveData> Moves = new(System.StringComparer.OrdinalIgnoreCase);

    public static void Initialize() { }

    static MoveDatabase()
    {
        foreach (var move in GameDataFiles.Load<List<MoveData>>(FileName))
        {
            Register(move);
        }
    }

    private static void Register(MoveData move)
    {
        Moves[move.Name] = move;
    }

    public static MoveData Get(string name)
    {
        if (Moves.TryGetValue(name, out var move))
            return move;
        return Moves["Tackle"];
    }

    public static IEnumerable<MoveData> GetAll() => Moves.Values;

    /// <summary>
    /// Whether a move does anything in a battle yet: every move but those whose effect no session has written and
    /// that have no power to hit with (<see cref="MoveEffectSupport.None"/>). A Pokémon learns only such moves by
    /// level (plan 06 · R19: the newest learnsets name later moves R24 to R26 give their effects); a name the data
    /// doesn't know is passed as it was.
    /// </summary>
    public static bool Runs(string name) => !Moves.TryGetValue(name, out var move) || move.Support != MoveEffectSupport.None;

    /// <summary>
    /// Gives every move the values these rules say: Platinum's own, or the newest games' where they differ
    /// (<see cref="MoveData.Modern"/>). Called through <see cref="Ruleset.Use"/> as a game begins or is loaded.
    /// </summary>
    internal static void UseRules(Ruleset rules)
    {
        foreach (var move in Moves.Values) move.UseRules(rules);
    }

    public static Move Create(string name)
    {
        return new Move(Get(name));
    }
}
