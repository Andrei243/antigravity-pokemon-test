using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;

namespace PokemonPlatinumEngine.Data;

/// <summary>The groups a phrase's words are chosen from (plan 08 · P9), in the order the picker shows them.</summary>
public enum PhraseGroup
{
    /// <summary>The Pokémon the player has seen, in National Pokédex order.</summary>
    Pokemon,
    /// <summary>The moves the player's team knows.</summary>
    Moves,
    /// <summary>The eighteen types.</summary>
    Types,
    Feelings,
    People,
    Places,
    Time,
    /// <summary>Small words that join the others: and, very, not, together…</summary>
    Small
}

/// <summary>
/// The words a phrase is made of (plan 08 · P9; Platinum's easy chat, with lists of our own and never the original's):
/// eight groups, three of them the game's own names (the Pokémon seen, the team's moves, the types) and five our own
/// words, whose ids are here and whose words are <c>ui.json</c>'s <c>phrases.words.&lt;group&gt;.&lt;id&gt;</c>, so
/// every language says them in its own. A word is kept as its key, <c>group:id</c> (<c>feelings:happy</c>,
/// <c>pokemon:Turtwig</c>, <c>types:Fire</c>), the same in every language, which the save keeps
/// (<see cref="Story.StoryState.Phrases"/>) and <see cref="Shown(string)"/> says in the language chosen. GPU-free.
/// </summary>
public sealed class PhraseGroups
{
    /// <summary>How many words a phrase has.</summary>
    public const int Length = 2;

    /// <summary>Every group, in the picker's order.</summary>
    public static IReadOnlyList<PhraseGroup> All => Enum.GetValues<PhraseGroup>();

    private readonly IReadOnlyList<string>[] words;

    /// <summary>The groups for a player who has seen <paramref name="pokemon"/> and whose team knows <paramref name="moves"/> (the data's names; unknown ones are passed over, repeats kept once).</summary>
    public PhraseGroups(IEnumerable<string> pokemon, IEnumerable<string> moves)
    {
        words = new IReadOnlyList<string>[All.Count];
        foreach (var group in All)
            words[(int)group] = group switch
            {
                PhraseGroup.Pokemon => pokemon.Where(s => PokemonDatabase.Get(s)?.Name == s).Distinct().Select(s => Key(group, s)).ToList(),
                PhraseGroup.Moves => moves.Where(m => MoveDatabase.Get(m).Name == m).Distinct().Select(m => Key(group, m)).ToList(),
                PhraseGroup.Types => Enum.GetValues<PokemonType>().Select(t => Key(group, t.ToString())).ToList(),
                _ => OwnWords(group).Select(w => Key(group, w)).ToList()
            };
    }

    /// <summary>The groups with no Pokémon seen and no moves known: only the types and our own words.</summary>
    public static PhraseGroups Plain() => new(Array.Empty<string>(), Array.Empty<string>());

    /// <summary>The words of a group, as keys.</summary>
    public IReadOnlyList<string> WordsOf(PhraseGroup group) => words[(int)group];

    /// <summary>The group's name as the picker shows it.</summary>
    public static string GroupName(PhraseGroup group)
    {
        string tag = Tag(group);
        return Text.Of($"phrases.group.{tag}");
    }

    /// <summary>A group's word in its key: <c>tag:id</c>.</summary>
    public static string Key(PhraseGroup group, string id) => Tag(group) + ":" + id;

    /// <summary>The group's name in a word's key (<c>feelings</c>); a property, so it is no word of any language.</summary>
    public static string Tag(PhraseGroup group) => group switch
    {
        PhraseGroup.Pokemon => "pokemon",
        PhraseGroup.Moves => "moves",
        PhraseGroup.Types => "types",
        PhraseGroup.Feelings => "feelings",
        PhraseGroup.People => "people",
        PhraseGroup.Places => "places",
        PhraseGroup.Time => "time",
        _ => "small"
    };

    /// <summary>A word's group and id; false for a key of no group.</summary>
    public static bool TryParse(string? key, out PhraseGroup group, out string id)
    {
        group = PhraseGroup.Small;
        id = "";
        if (string.IsNullOrEmpty(key)) return false;
        int colon = key.IndexOf(':');
        if (colon <= 0 || colon == key.Length - 1) return false;
        string tag = key[..colon];
        foreach (var g in All)
            if (Tag(g) == tag)
            {
                group = g;
                id = key[(colon + 1)..];
                return true;
            }
        return false;
    }

    /// <summary>Whether a key names a word that can be shown: one of our own lists, a species or move of the data, a type.</summary>
    public static bool IsWord(string? key)
    {
        if (!TryParse(key, out var group, out string id)) return false;
        return group switch
        {
            PhraseGroup.Pokemon => PokemonDatabase.Get(id)?.Name == id,
            PhraseGroup.Moves => MoveDatabase.Get(id).Name == id,
            PhraseGroup.Types => Enum.TryParse<PokemonType>(id, out var type) && type.ToString() == id,
            _ => OwnWords(group).Contains(id)
        };
    }

    /// <summary>A word as the chosen language says it; a key that names no word is shown as nothing.</summary>
    public static string Shown(string? key)
    {
        if (!IsWord(key) || !TryParse(key, out var group, out string id)) return "";
        switch (group)
        {
            case PhraseGroup.Pokemon: return LocalNames.Species(id);
            case PhraseGroup.Moves: return LocalNames.Move(id);
            case PhraseGroup.Types: return Words.Type(Enum.Parse<PokemonType>(id));
        }
        string tag = Tag(group);
        return Text.Of($"phrases.words.{tag}.{id}");
    }

    /// <summary>A whole phrase as it is said: its words one after another, those that name nothing left out.</summary>
    public static string Shown(IEnumerable<string?> phrase) => string.Join(" ", phrase.Select(Shown).Where(w => w.Length > 0));

    /// <summary>Whether a phrase is whole: <see cref="Length"/> words, each one a word.</summary>
    public static bool IsWhole(IReadOnlyList<string?>? phrase) => phrase != null && phrase.Count == Length && phrase.All(IsWord);

    /// <summary>The ids of a group of our own words, in the picker's order (none for the three groups of names).</summary>
    public static IReadOnlyList<string> OwnWords(PhraseGroup group) => group switch
    {
        PhraseGroup.Feelings => new[]
        {
            "happy", "glad", "excited", "proud", "brave", "calm", "cool", "cute", "strong", "lucky", "amazing", "wonderful",
            "fun", "great", "tough", "gentle", "shy", "nervous", "tired", "sleepy", "hungry", "lonely", "sad", "angry",
            "scared", "surprised", "grateful", "determined"
        },
        PhraseGroup.People => new[]
        {
            "me", "you", "friend", "friends", "rival", "mom", "dad", "family", "professor", "trainer", "trainers", "champion",
            "leader", "team", "partner", "everyone", "kids", "grandpa", "grandma", "teacher", "hero", "nobody"
        },
        PhraseGroup.Places => new[]
        {
            "home", "town", "city", "route", "grass", "forest", "cave", "lake", "sea", "beach", "mountain", "snow", "sky",
            "gym", "league", "school", "tower", "garden", "road", "world"
        },
        PhraseGroup.Time => new[]
        {
            "morning", "noon", "evening", "night", "today", "tomorrow", "yesterday", "now", "soon", "always", "never",
            "sometimes", "someday", "forever", "weekend", "everyDay", "again", "first", "last"
        },
        PhraseGroup.Small => new[]
        {
            "and", "or", "but", "so", "very", "really", "too", "not", "yes", "no", "my", "your", "our", "with", "for", "all",
            "more", "together", "best", "number1", "please", "thanks", "hello", "goodbye"
        },
        _ => Array.Empty<string>()
    };
}
