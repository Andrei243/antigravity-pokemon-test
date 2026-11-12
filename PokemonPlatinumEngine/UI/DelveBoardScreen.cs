using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models.Delve;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The camp's board (plan 44 · SD9; style guide, "The delve", "The camp's board"): the day's three requests with which
/// are done, and the camp's rank. GPU-free: <see cref="Move"/>, <see cref="Close"/>.
/// </summary>
public sealed class DelveBoardScreen
{
    public bool IsActive { get; private set; }
    public IReadOnlyList<DelveRequest> Requests { get; private set; } = new List<DelveRequest>();
    public IReadOnlyList<bool> Done { get; private set; } = new List<bool>();
    public int Rank { get; private set; }
    public int Cursor { get; private set; }

    public void Open(IReadOnlyList<DelveRequest> requests, IReadOnlyList<bool> done, int rank)
    {
        Requests = requests;
        Done = done;
        Rank = rank;
        Cursor = 0;
        IsActive = true;
    }

    public void Move(int by)
    {
        if (Requests.Count == 0) return;
        Cursor = ((Cursor + by) % Requests.Count + Requests.Count) % Requests.Count;
    }

    public void Close() => IsActive = false;

    /// <summary>A hollow's name in words.</summary>
    public static string HollowName(string key) => Text.Of($"delve.hollow.{key}");

    /// <summary>A request in words (<c>delve.request.*</c>, brisk and kind: the user's answer 62a).</summary>
    public static string LineOf(DelveRequest r) => Text.Of($"delve.request.{r.Kind}",
        ("hollow", HollowName(r.Hollow)), ("floor", r.Floor.ToString()),
        ("species", r.Species != null ? LocalNames.Species(r.Species) : ""), ("item", r.Item != null ? LocalNames.Item(r.Item) : ""));

    /// <summary>What a request pays, in words.</summary>
    public static string PrizeOf(DelveRequest r) =>
        string.Join(", ", DelveHollows.Prizes.Where(p => p.Kind == r.Kind).Select(p => p.Count > 1 ? $"{LocalNames.Item(p.Item)} ×{p.Count}" : LocalNames.Item(p.Item)));
}
