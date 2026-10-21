using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Foreign;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// A source of the migration (plan 08 · P21): a save in the <c>migrate</c> folder, or the folder's loose Pokémon files
/// taken together as one, or a file the readers refuse (<see cref="Refusal"/>), kept in the list so that the player is told
/// why it isn't offered.
/// </summary>
public sealed record MigrationSource(string Name, ForeignSave? Save, ForeignRefusal Refusal, int Files = 1, int? Slot = null)
{
    public bool Readable => Save != null;

    /// <summary>What the list says of the file: a save's game and trainer, a folder of Pokémon, or why it can't be read.</summary>
    public string Kind => Save?.Kind switch
    {
        ForeignKind.GbaSave => Text.Of("migrate.kind.gba"),
        ForeignKind.PlatinumSave => Text.Of("migrate.kind.platinum"),
        ForeignKind.Files => Text.Of("migrate.kind.files", ("n", Files)),
        ForeignKind.OwnSave => Text.Of("migrate.kind.slot", ("slot", Slot ?? 0)),
        ForeignKind.GbaPokemon or ForeignKind.DsPokemon => Text.Of("migrate.kind.file"),
        _ => Text.Of("migrate.kind.unknown")
    };

    /// <summary>Why a file can't be read, in the player's words.</summary>
    public string Notice => Refusal switch
    {
        ForeignRefusal.UnknownSize => Text.Of("migrate.refused.size"),
        ForeignRefusal.DamagedSave => Text.Of("migrate.refused.save"),
        ForeignRefusal.DamagedPokemon => Text.Of("migrate.refused.pokemon"),
        ForeignRefusal.Unreadable => Text.Of("migrate.refused.unreadable"),
        _ => ""
    };
}

/// <summary>
/// The folder a player's own files are dropped in (<see cref="SavePaths.Migrate"/>; plan 08 · P21). It is read and never
/// written: each file is opened by <see cref="ForeignFile.Open"/> (read-only), nothing is created, moved or renamed, and
/// a folder that isn't there has no sources and is not made.
/// </summary>
public static class MigrationFolder
{
    /// <summary>The most files looked at, so that a folder of something else is not read whole.</summary>
    public const int MostFiles = 300;

    /// <summary>
    /// The folder's sources, saves first in the order of their names, then the loose Pokémon files as one, then the files
    /// that couldn't be read; files of a size none of the readers knows (a picture, a note) are not listed at all.
    /// </summary>
    public static List<MigrationSource> Read(string folder)
    {
        var sources = new List<MigrationSource>();
        if (!Directory.Exists(folder)) return sources;
        var saves = new List<MigrationSource>();
        var loose = new List<ForeignSave>();
        var refused = new List<MigrationSource>();
        foreach (string path in Directory.EnumerateFiles(folder).OrderBy(p => p, StringComparer.OrdinalIgnoreCase).Take(MostFiles))
        {
            var (save, refusal) = ForeignFile.Open(path);
            string name = Path.GetFileName(path);
            if (save == null)
            {
                // A file of any other size isn't one of ours to speak of
                if (refusal != ForeignRefusal.UnknownSize) refused.Add(new MigrationSource(name, null, refusal));
            }
            else if (save.Kind is ForeignKind.GbaPokemon or ForeignKind.DsPokemon) loose.Add(save);
            else saves.Add(new MigrationSource(name, save, ForeignRefusal.None));
        }
        sources.AddRange(saves);
        if (loose.Count > 0) sources.Add(new MigrationSource(Text.Of("migrate.files.name"), Migration.Combine(loose), ForeignRefusal.None, loose.Count));
        sources.AddRange(refused);
        return sources;
    }

    /// <summary>Whether the folder holds something the readers know.</summary>
    public static bool Offers(IEnumerable<MigrationSource> sources) => sources.Any(s => s.Readable);

    /// <summary>
    /// The saves of this game that can be moved from (plan 08 · P22): each slot that reads, as a source, with its player's
    /// name. The title offers MIGRATE when there is a file in the folder or two saves, one to move from and one to move to.
    /// </summary>
    public static List<MigrationSource> Slots(IEnumerable<(int Slot, SaveData Save)> saves) =>
        saves.Select(s => new MigrationSource(s.Save.PlayerName, Migration.FromSave(s.Save), ForeignRefusal.None, 1, s.Slot)).ToList();
}
