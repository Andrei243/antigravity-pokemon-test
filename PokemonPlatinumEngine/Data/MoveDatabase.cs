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

    public static Move Create(string name)
    {
        return new Move(Get(name));
    }
}
