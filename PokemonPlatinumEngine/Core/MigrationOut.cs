using System;
using System.IO;
using System.Linq;
using System.Text;
using PokemonPlatinumEngine.Foreign;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The way out of the migration (plan 08 · P22): a Pokémon of a save written as a <c>.pk4</c> file or as Pokémon
/// Showdown's text into <c>migrate/out</c>, as a copy: nothing leaves the save and no file of the player's is touched.
/// This is the one place the game writes in the <c>migrate</c> folder, and it never overwrites: a file of the name is
/// kept, and the new one takes the next free number.
/// </summary>
public static class MigrationOut
{
    public const string FolderName = "out";

    /// <summary>The folder the files go in, inside the migrate folder.</summary>
    public static string FolderIn(string migrate) => Path.Combine(migrate, FolderName);

    /// <summary>
    /// Writes a Pokémon of <paramref name="from"/>'s out; returns the file's name, or null where nothing could be written (a
    /// species a <c>.pk4</c> can't hold, a folder that can't be made, a disk that won't take it).
    /// </summary>
    public static string? Write(string migrate, SaveData from, SavedPokemonData saved, WriteFormat format)
    {
        try
        {
            var pokemon = saved.ToPokemon();
            // A species of our own (plan 03 · D14) is in no other game, and Showdown wouldn't know its name
            if (pokemon.Species.IsOurs) return null;
            byte[]? bytes;
            string extension;
            if (format == WriteFormat.Pk4)
            {
                bytes = DsPokemon.Write(pokemon, unchecked((uint)from.TrainerId), from.PlayerName, from.Look == PlayerLook.Girl);
                extension = ".pk4";
            }
            else
            {
                bytes = Encoding.UTF8.GetBytes(ShowdownText.Of(pokemon) + "\n");
                extension = ".txt";
            }
            if (bytes == null) return null;

            string folder = FolderIn(migrate);
            Directory.CreateDirectory(folder);
            string stem = new(($"{pokemon.Form ?? pokemon.Species.Name}-{pokemon.Personality:X8}")
                .Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '-' : c).ToArray());
            for (int n = 1; n < 1000; n++)
            {
                string name = n == 1 ? stem + extension : $"{stem}-{n}{extension}";
                try
                {
                    // CreateNew: a file that is there is never written over
                    using var file = new FileStream(Path.Combine(folder, name), FileMode.CreateNew, FileAccess.Write, FileShare.None);
                    file.Write(bytes);
                    return name;
                }
                catch (IOException) when (File.Exists(Path.Combine(folder, name))) { }
            }
            return null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return null;
        }
    }
}
