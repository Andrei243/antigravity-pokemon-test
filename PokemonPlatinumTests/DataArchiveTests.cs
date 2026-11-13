using System;
using System.IO;
using PokemonPlatinumEngine.Data;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The archive of the small data files (<see cref="DataArchive"/>): what it holds is the files' own text, it is made
/// again when a file changes, and it has no say over what it doesn't cover. Each test has a data folder and a cache
/// folder of its own, and each asks its own archive: which one the game reads through is the whole process's, and the tests run side
/// by side.
/// </summary>
public class DataArchiveTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "pp-archive-" + Guid.NewGuid().ToString("N"));
    private string Data => Path.Combine(root, "Data");
    private string Cache => Path.Combine(root, "cache");

    public DataArchiveTests()
    {
        Write("maps/Room.json", "{ \"name\": \"Room\" }");
        Write("scripts/common.txt", "script Hello\n  say \"Héllo, {player}!\"\nend\n");
        Write("world/sinnoh/chunks/001.json", "{ \"id\": 1 }");
        Write("world/sinnoh/overlays/twinleaf_town.json", "﻿{ \"music\": \"sinnoh/twinleaf\" }");
        Write("species.json", "[]");
    }

    public void Dispose()
    {
        try { Directory.Delete(root, recursive: true); } catch (IOException) { }
    }

    private void Write(string key, string text)
    {
        string path = Path.Combine(Data, key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }

    private DataArchive Open()
    {
        var archive = DataArchive.Open(Data, Cache);
        archive.Written?.Wait();
        return archive;
    }

    [Fact]
    public void TheArchiveGivesEachFilesOwnText()
    {
        var archive = Open();
        Assert.False(archive.FromCache);
        Assert.Equal(4, archive.Count);
        foreach (string key in new[] { "maps/Room.json", "scripts/common.txt", "world/sinnoh/chunks/001.json", "world/sinnoh/overlays/twinleaf_town.json" })
        {
            string path = Path.Combine(Data, key.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(archive.Has(path), key);
            // A byte order mark is passed over as File.ReadAllText passes over it
            Assert.Equal(File.ReadAllText(path), archive.TextOf(path));
        }
    }

    [Fact]
    public void ASecondStartReadsTheArchiveFromTheCache()
    {
        string made = Open().Fingerprint;
        var again = Open();
        Assert.True(again.FromCache);
        Assert.Equal(made, again.Fingerprint);
        string path = Path.Combine(Data, "scripts", "common.txt");
        Assert.Equal(File.ReadAllText(path), again.TextOf(path));
    }

    [Fact]
    public void AFileChangedAddedOrTakenAwayMakesTheArchiveAgain()
    {
        string first = Open().Fingerprint;

        // Changed, to the same length: only the time of writing tells
        string script = Path.Combine(Data, "scripts", "common.txt");
        File.WriteAllText(script, File.ReadAllText(script).Replace("Hello", "Hallo"));
        File.SetLastWriteTimeUtc(script, File.GetLastWriteTimeUtc(script).AddSeconds(2));
        var changed = Open();
        Assert.False(changed.FromCache);
        Assert.NotEqual(first, changed.Fingerprint);
        Assert.Contains("Hallo", changed.TextOf(script));

        Write("maps/Other.json", "{ \"name\": \"Other\" }");
        var added = Open();
        Assert.False(added.FromCache);
        Assert.True(added.Has(Path.Combine(Data, "maps", "Other.json")));

        File.Delete(Path.Combine(Data, "maps", "Other.json"));
        var removed = Open();
        Assert.Equal(changed.Fingerprint, removed.Fingerprint);
        Assert.False(removed.Has(Path.Combine(Data, "maps", "Other.json")));

        // The older archives were taken away as each new one was written
        Assert.Single(Directory.GetFiles(Cache, "*.pak"));
    }

    [Fact]
    public void WhatTheArchiveDoesNotCoverIsReadFromItsFile()
    {
        var archive = Open();
        // A file of the data outside the folders it holds
        string species = Path.Combine(Data, "species.json");
        Assert.True(archive.Has(species));
        Assert.Equal("[]", archive.TextOf(species));
        // A file of another folder altogether (a pack's, a tool's)
        string elsewhere = Path.Combine(root, "pack", "maps", "Room.json");
        Directory.CreateDirectory(Path.GetDirectoryName(elsewhere)!);
        File.WriteAllText(elsewhere, "pack");
        Assert.Equal("pack", archive.TextOf(elsewhere));
        // And a file the archive should hold and doesn't is not there, whatever was written since
        Assert.False(archive.Has(Path.Combine(Data, "maps", "Missing.json")));
    }

    [Fact]
    public void ADamagedArchiveIsMadeAgain()
    {
        Open();
        string file = Assert.Single(Directory.GetFiles(Cache, "*.pak"));
        File.WriteAllBytes(file, new byte[] { 1, 2, 3 });
        var again = Open();
        Assert.False(again.FromCache);
        string path = Path.Combine(Data, "maps", "Room.json");
        Assert.Equal(File.ReadAllText(path), again.TextOf(path));
    }
}
