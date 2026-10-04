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
