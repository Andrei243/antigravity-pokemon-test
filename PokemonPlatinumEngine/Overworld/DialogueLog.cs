using System.Collections.Generic;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// The backlog (plan 09 · L3): the last lines the text box wrote, with who said them, in a ring of fifty. Plain text,
/// markup stripped. GPU-free.
/// </summary>
public sealed class DialogueLog
{
    public const int Capacity = 50;
    private readonly List<(string Speaker, string Text)> entries = new();

    public int Count => entries.Count;
    public IReadOnlyList<(string Speaker, string Text)> Entries => entries;

    /// <summary>Keeps a line; the oldest goes once there are fifty.</summary>
    public void Add(string speaker, string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        entries.Add((speaker, text));
        if (entries.Count > Capacity) entries.RemoveAt(0);
    }

    public void Clear() => entries.Clear();
}
