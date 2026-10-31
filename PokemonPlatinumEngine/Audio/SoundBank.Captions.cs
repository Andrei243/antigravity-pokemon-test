using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Audio;

// Captions (plan 12 · Q9; style guide, "Field menus and notices", "Captions"): what each sound and each cry is called on
// screen, in a few words of our own from ui.json's captions section. A sound of a family (a move's own, a type's launch,
// a type's Z-Move or Max Move, a G-Max Move's signature) is named by its family's line with the move or the type put in;
// every other sound has a line of its own under captions.sound.
public static partial class SoundBank
{
    private static Dictionary<string, string>? ownMoveByKey;

    /// <summary>The caption of a sound of the bank, in the language chosen; the bank's own name for a name it doesn't know.</summary>
    public static string CaptionOf(string name)
    {
        string key = name.ToLowerInvariant();
        if (key.StartsWith("use_", StringComparison.Ordinal))
        {
            ownMoveByKey ??= MovesWithOwnSounds.GroupBy(MoveKey).ToDictionary(g => g.Key, g => g.First());
            if (ownMoveByKey.TryGetValue(key[4..], out var move)) return Text.Of("captions.family.move", ("move", LocalNames.Move(move)));
        }
        if (key.StartsWith("gmax_", StringComparison.Ordinal))
            foreach (var (gmax, _) in GMaxSounds)
                if (GMaxSound(gmax) == key) return Text.Of("captions.family.gmax", ("move", LocalNames.Move(gmax)));
        foreach (var type in Enum.GetValues<PokemonType>())
        {
            if (key == MoveSound(type)) return Text.Of("captions.family.moveType", ("type", Words.Type(type)));
            if (key == ZMoveSound(type)) return Text.Of("captions.family.zMove", ("type", Words.Type(type)));
            if (key == MaxMoveSound(type)) return Text.Of("captions.family.maxMove", ("type", Words.Type(type)));
        }
        string own = CaptionKey(key);
        return Text.Has($"captions.sound.{own}") ? Text.Of($"captions.sound.{own}") : name;
    }

    /// <summary>The key a sound's own caption goes by under <c>captions.sound</c>: its name with its words run together ("door_open" is doorOpen).</summary>
    public static string CaptionKey(string name)
    {
        var parts = name.ToLowerInvariant().Split('_', StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Select((p, i) => i == 0 ? p : char.ToUpperInvariant(p[0]) + p[1..]));
    }

    /// <summary>The caption of a cry: the species named, in the language chosen ("Starly cries").</summary>
    public static string CryCaption(PokemonSpecies species, string? form = null) =>
        Text.Of("captions.cry", ("name", species.DisplayName));
}
