using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// One thing a trainer says in battle (plan 11 · C12): its words, which may hold <c>{player}</c> and <c>{rival}</c>,
/// and the face and gesture it is said with, by C11's names (<see cref="Overworld.Mood"/>, <see cref="Overworld.Gesture"/>),
/// which the trainer shows as they step back onto their platform to say it (plan 11 · C13).
/// </summary>
public sealed class TrainerLine
{
    public string Text { get; set; } = "";
    public string? Mood { get; set; }
    public string? Act { get; set; }

    /// <summary>The face the line is said with: its <see cref="Mood"/> by C11's name, neutral where it gives none.</summary>
    public Overworld.Mood Face => Mood != null && Enum.TryParse<Overworld.Mood>(Mood, true, out var mood) ? mood : Overworld.Mood.Neutral;

    /// <summary>The gesture the line is said with, by C11's name; null where it gives none.</summary>
    public Overworld.Gesture? Gesture => Act != null && Enum.TryParse<Overworld.Gesture>(Act, true, out var gesture) ? gesture : null;
}

/// <summary>A trainer's lines in battle: up to one for each moment (<see cref="TrainerCue"/>).</summary>
public sealed class TrainerLineSet
{
    public TrainerLine? LastPokemon { get; set; }
    public TrainerLine? FirstDown { get; set; }
    public TrainerLine? LowHp { get; set; }

    /// <summary>
    /// What they say as their Pokémon grows into a giant (plan 25 · YD6: each Champion's Mega Max team): no moment of
    /// the rules (no <see cref="TrainerCue"/>, no event), said by the screen once the growth it saw is over.
    /// </summary>
    public TrainerLine? Giant { get; set; }

    public TrainerLine? For(TrainerCue cue) => cue switch
    {
        TrainerCue.LastPokemon => LastPokemon,
        TrainerCue.FirstDown => FirstDown,
        _ => LowHp
    };

    /// <summary>Every line of the set, the rules' three moments and the screen's giant.</summary>
    public IEnumerable<TrainerLine> Lines => new[] { LastPokemon, FirstDown, LowHp, Giant }.Where(l => l is { Text.Length: > 0 }).Select(l => l!);
}

/// <summary>
/// What trainers say in battle (plan 11 · C12; <c>Data/trainer-lines.json</c>, hand-written, our own words): a set of
/// lines for each trainer, keyed by <c>trainers.json</c>'s id (<c>leader_roark</c>) or by a stem ending in <c>*</c>
/// (<c>rival_route_203_*</c>, the rival's three teams there). An id of its own wins over a stem, and a longer stem
/// over a shorter one. GPU-free; <see cref="Battle.BattleEngine"/> reads it as a trainer reacts.
/// </summary>
public static class TrainerLines
{
    public const string FileName = "trainer-lines.json";

    private static readonly Lazy<Dictionary<string, TrainerLineSet>> File = new(() =>
        new Dictionary<string, TrainerLineSet>(GameDataFiles.Load<Dictionary<string, TrainerLineSet>>(FileName), StringComparer.OrdinalIgnoreCase));

    /// <summary>Every key of the file, ids and stems alike.</summary>
    public static IEnumerable<string> Keys => File.Value.Keys;

    /// <summary>Every set of lines with its key.</summary>
    public static IEnumerable<KeyValuePair<string, TrainerLineSet>> All => File.Value;

    /// <summary>Whether a key is a stem (it ends in <c>*</c>) rather than an id.</summary>
    public static bool IsStem(string key) => key.EndsWith('*');

    /// <summary>The set of lines of a trainer by their id: their own, or the longest stem their id begins with; null for none.</summary>
    public static TrainerLineSet? SetOf(string? id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (File.Value.TryGetValue(id, out var own)) return own;
        return File.Value.Where(e => IsStem(e.Key) && id.StartsWith(e.Key[..^1], StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(e => e.Key.Length).Select(e => e.Value).FirstOrDefault();
    }

    /// <summary>What a trainer says at a moment, or null when they have nothing to say then.</summary>
    public static TrainerLine? For(string? id, TrainerCue cue) => SetOf(id)?.For(cue) is { Text.Length: > 0 } line ? line : null;

    /// <summary>What a trainer says as their Pokémon grows into a giant (plan 25 · YD6), or null.</summary>
    public static TrainerLine? GiantFor(string? id) => SetOf(id)?.Giant is { Text.Length: > 0 } line ? line : null;
}
