using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 24 Â· X4: the data files read strictly. A key no field has, a value of the wrong type or a missing required field
/// is a finding naming the file, the JSON path and the key; the hand-written files are read so by the game itself, the
/// generated ones only here, so a field renamed without running the importer fails in the tests and never in a game.
/// </summary>
public class DataFileStrictTests
{
    private static string Data => Path.Combine(Repository.Root, "PokemonPlatinumEngine", "Data");

    [Fact]
    public void EveryHandWrittenFileReadsStrictly()
    {
        var findings = StrictFiles.HandWritten(Data).ToList();
        Assert.True(findings.Count == 0, string.Join("\n", findings));
    }

    [Fact]
    public void EveryGeneratedFileReadsStrictly()
    {
        var findings = StrictFiles.Generated(Data).ToList();
        Assert.True(findings.Count == 0, string.Join("\n", findings));
    }

    [Fact]
    public void TheHandWrittenFilesAreManyAndAllRead()
    {
        // The walk finds every kind: an empty list would pass the test above for nothing
        var files = StrictFiles.HandWrittenFiles(Data).ToList();
        Assert.Contains(files, f => f.Kind == StrictKind.World);
        Assert.Contains(files, f => f.Kind == StrictKind.Overlay);
        Assert.Contains(files, f => f.Kind == StrictKind.Map);
        Assert.Contains(files, f => f.Kind == StrictKind.SoundMap);
        Assert.True(files.Count > 300, $"only {files.Count} hand-written files found");
        var generated = StrictFiles.GeneratedFiles(Data).ToList();
        foreach (StrictKind kind in new[] { StrictKind.Species, StrictKind.Moves, StrictKind.Abilities, StrictKind.Items,
                     StrictKind.Trainers, StrictKind.Chunk, StrictKind.Area, StrictKind.Matrix, StrictKind.Habitats })
            Assert.Contains(generated, f => f.Kind == kind);
        Assert.True(generated.Count > 3000, $"only {generated.Count} generated files found");
    }

    [Fact]
    public void AGeneratedFileWithAFieldTheCodeLacksFailsTheTest()
    {
        // As a field renamed in the code without the importer run again would leave it
        string message = Refusal<List<MoveData>>("""[ { "id": 1, "name": "Pound", "basePowr": 40 } ]""", "moves.json");
        Assert.Equal("moves.json: $[0].basePowr: no such field", message);
    }

    private static string Refusal<T>(string text, string file)
    {
        var e = Assert.Throws<InvalidDataException>(() => GameDataFiles.ReadStrict<T>(text, file));
        return e.Message;
    }

    [Fact]
    public void AMistypedKeyOfAPersonIsRefusedWithItsPath()
    {
        string message = Refusal<WorldOverlayFile>("""
            { "area": "route_204_south", "people": { "lass_sarah": { "hidenBy": "FLAG_X" } } }
            """, "world/sinnoh/overlays/route_204_south.json");
        Assert.Equal("world/sinnoh/overlays/route_204_south.json: $.people.lass_sarah.hidenBy: no such field", message);
    }

    [Fact]
    public void AMistypedKeyOfADoorIsRefusedWithItsPath()
    {
        // A door is a record written on one line (OneLine), which reads keys of its own
        string message = Refusal<WorldOverlayFile>("""
            { "area": "twinleaf_town", "doors": [ { "warp": 0, "map": "PlayerHouse", "x": 1, "y": 2 }, { "warp": 1, "mapp": "RivalHouse" } ] }
            """, "world/sinnoh/overlays/twinleaf_town.json");
        Assert.StartsWith("world/sinnoh/overlays/twinleaf_town.json: $.doors[1]", message);
        Assert.EndsWith(".mapp: no such field", message);
    }

    [Fact]
    public void AMapFileWithAMisspeltKeyIsRefused()
    {
        string message = Refusal<MapFile>(MapText("\"bgmTrak\": \"route\""), "maps/Probe.json");
        Assert.Equal("maps/Probe.json: $.bgmTrak: no such field", message);
    }

    [Fact]
    public void CaseCountsSoAKeyInCSharpsCaseIsRefusedNotRead()
    {
        string message = Refusal<MapFile>(MapText("\"BgmTrack\": \"route\""), "maps/Probe.json");
        Assert.Equal("maps/Probe.json: $.BgmTrack: no such field", message);
    }

    [Fact]
    public void AMissingRequiredFieldIsAFinding()
    {
        string message = Refusal<MapFile>("""{ "name": "Probe", "height": 1, "ground": ["."] }""", "maps/Probe.json");
        Assert.StartsWith("maps/Probe.json: $: missing", message);
        Assert.Contains("width", message);
    }

    [Fact]
    public void AValueOfTheWrongTypeIsAFindingWithItsPath()
    {
        string message = Refusal<MapFile>(MapText("\"encounterRate\": \"often\""), "maps/Probe.json");
        Assert.StartsWith("maps/Probe.json: $.encounterRate: ", message);
        string inLine = Refusal<WorldOverlayFile>("""{ "area": "a", "doors": [ { "warp": "first" } ] }""", "o.json");
        Assert.StartsWith("o.json: $.doors[0]: 'warp': ", inLine);
    }

    [Fact]
    public void TheLenientReadingIsUnchanged()
    {
        // A save, a tool or a generated file still reads what it knows and passes the rest by
        var map = GameDataFiles.Deserialize<MapFile>(MapText("\"bgmTrak\": \"route\""));
        Assert.Equal("Probe", map.Name);
        var overlay = GameDataFiles.Deserialize<WorldOverlayFile>("""{ "area": "a", "doors": [ { "warp": 3, "mapp": "X" } ] }""");
        Assert.Equal(3, overlay.Doors![0].Warp);
        // And a required field is asked for by the strict reading alone
        Assert.Equal(0, GameDataFiles.Deserialize<MapFile>("""{ "name": "Probe" }""").Width);
    }

    [Fact]
    public void AStrictReadOfAGoodFileIsTheLenientRead()
    {
        foreach (string name in new[] { "PlayerHouse", "OreburghGym", "PokemonLeagueHallOfFame" })
        {
            string text = File.ReadAllText(Path.Combine(Data, "maps", name + ".json"));
            Assert.Equal(GameDataFiles.Serialize(GameDataFiles.Deserialize<MapFile>(text)),
                GameDataFiles.Serialize(GameDataFiles.ReadStrict<MapFile>(text, name)));
        }
        string overlay = File.ReadAllText(Path.Combine(Data, "world", "sinnoh", "overlays", "twinleaf_town.json"));
        Assert.Equal(GameDataFiles.Serialize(GameDataFiles.Deserialize<WorldOverlayFile>(overlay)),
            GameDataFiles.Serialize(GameDataFiles.ReadStrict<WorldOverlayFile>(overlay, "twinleaf_town")));
    }

    private static string MapText(string extra) =>
        $$"""{ "name": "Probe", "width": 1, "height": 1, "ground": ["."], "solid": ["."], {{extra}} }""";
}
