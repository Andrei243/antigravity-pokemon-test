using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Data;

/// <summary>What a file read strictly is (plan 24 · X4), which says the type it is read as.</summary>
public enum StrictKind
{
    // Written by hand: the game reads these strictly
    World,
    Overlay,
    Map,
    SoundMap,
    // Generated: read strictly by the tests and the content check alone
    Species,
    Moves,
    Abilities,
    Items,
    Trainers,
    Chunk,
    Area,
    Matrix,
    Habitats,
    Calendar,
    Encounters,
    // Plan 37 · FC1: the later sets (generated) and the later places' trainers (written by hand)
    FacilitySets,
    FacilityTrainers,
    // The later places' prize counters (written by hand: plan 23, Kiloude City's Battle Maison)
    FacilityPrizes,
    // Plan 37 · FC3: the partners' tree's famous trainers (written by hand)
    TreeCast
}

/// <summary>A data file to read strictly: where it is, its name under <c>Data</c>, and its kind.</summary>
public sealed record StrictFile(string Path, string Relative, StrictKind Kind);

/// <summary>
/// The data files read strictly (plan 24 · X4, <see cref="GameDataFiles.Strict"/>), found under a <c>Data</c> folder:
/// the hand-written ones the game itself reads so (each region's <c>world.json</c> and overlays, the map files of
/// <c>maps</c>, <c>audio/sound-map.json</c>) and the generated ones the tests and the content check read so (the five
/// tables, the trainers of our own and the masters', and each region's chunks, areas, matrices, habitats, calendar and
/// encounters). Scripts are read by <c>ScriptParser</c>, which already names a mistake by its file and line.
/// </summary>
public static class StrictFiles
{
    public static IEnumerable<StrictFile> HandWrittenFiles(string data)
    {
        string world = Path.Combine(data, World.Folder);
        if (Directory.Exists(world))
            foreach (string region in Directory.GetDirectories(world).OrderBy(d => d, StringComparer.Ordinal))
            {
                string index = Path.Combine(region, World.IndexFile);
                if (File.Exists(index)) yield return Make(data, index, StrictKind.World);
                foreach (string file in Files(Path.Combine(region, "overlays"))) yield return Make(data, file, StrictKind.Overlay);
            }
        foreach (string file in Files(Path.Combine(data, "maps"))) yield return Make(data, file, StrictKind.Map);
        string sound = Path.Combine(data, SoundMap.FileName);
        if (File.Exists(sound)) yield return Make(data, sound, StrictKind.SoundMap);
        string facilityTrainers = Path.Combine(data, Models.Facilities.FacilityTrainers.FileName);
        if (File.Exists(facilityTrainers)) yield return Make(data, facilityTrainers, StrictKind.FacilityTrainers);
        string facilityPrizes = Path.Combine(data, Models.Facilities.FacilityPrizes.FileName);
        if (File.Exists(facilityPrizes)) yield return Make(data, facilityPrizes, StrictKind.FacilityPrizes);
        string treeCast = Path.Combine(data, Models.Facilities.PartnersTree.FileName);
        if (File.Exists(treeCast)) yield return Make(data, treeCast, StrictKind.TreeCast);
    }

    public static IEnumerable<StrictFile> GeneratedFiles(string data)
    {
        (string name, StrictKind kind)[] tables =
        {
            ("species.json", StrictKind.Species), ("moves.json", StrictKind.Moves), ("abilities.json", StrictKind.Abilities),
            ("items.json", StrictKind.Items), (TrainerDatabase.FileName, StrictKind.Trainers),
            (TrainerDatabase.OursFileName, StrictKind.Trainers), (TrainerDatabase.MastersFileName, StrictKind.Trainers)
        };
        foreach (var (name, kind) in tables)
            if (File.Exists(Path.Combine(data, name))) yield return Make(data, Path.Combine(data, name), kind);
        string facilitySets = Path.Combine(data, FacilitySetDatabase.FileName);
        if (File.Exists(facilitySets)) yield return Make(data, facilitySets, StrictKind.FacilitySets);
        string world = Path.Combine(data, World.Folder);
        if (!Directory.Exists(world)) yield break;
        foreach (string region in Directory.GetDirectories(world).OrderBy(d => d, StringComparer.Ordinal))
        {
            foreach (string file in Files(Path.Combine(region, "chunks"))) yield return Make(data, file, StrictKind.Chunk);
            foreach (string file in Files(Path.Combine(region, "areas"))) yield return Make(data, file, StrictKind.Area);
            foreach (string file in Files(Path.Combine(region, "matrices"))) yield return Make(data, file, StrictKind.Matrix);
            (string name, StrictKind kind)[] single =
            {
                (WorldHabitatsFile.FileName, StrictKind.Habitats), (WorldCalendarFile.FileName, StrictKind.Calendar),
                (WorldEncountersFile.FileName, StrictKind.Encounters)
            };
            foreach (var (name, kind) in single)
                if (File.Exists(Path.Combine(region, name))) yield return Make(data, Path.Combine(region, name), kind);
        }
    }

    /// <summary>Every finding of the hand-written files, a line each.</summary>
    public static IEnumerable<string> HandWritten(string data) => HandWrittenFiles(data).Select(Check).OfType<string>();

    /// <summary>Every finding of the generated files, a line each.</summary>
    public static IEnumerable<string> Generated(string data) =>
        GeneratedFiles(data).AsParallel().AsOrdered().Select(Check).OfType<string>();

    /// <summary>The file read strictly as its kind: null when it reads, otherwise the finding.</summary>
    public static string? Check(StrictFile file)
    {
        try
        {
            string text = File.ReadAllText(file.Path);
            switch (file.Kind)
            {
                case StrictKind.World: GameDataFiles.ReadStrict<WorldIndexFile>(text, file.Relative); break;
                case StrictKind.Overlay: GameDataFiles.ReadStrict<WorldOverlayFile>(text, file.Relative); break;
                case StrictKind.Map: GameDataFiles.ReadStrict<MapFile>(text, file.Relative); break;
                case StrictKind.SoundMap: GameDataFiles.ReadStrict<SoundMapFile>(text, file.Relative); break;
                case StrictKind.Species: GameDataFiles.ReadStrict<List<PokemonSpecies>>(text, file.Relative); break;
                case StrictKind.Moves: GameDataFiles.ReadStrict<List<MoveData>>(text, file.Relative); break;
                case StrictKind.Abilities: GameDataFiles.ReadStrict<List<AbilityDatabase.AbilityRecord>>(text, file.Relative); break;
                case StrictKind.Items: GameDataFiles.ReadStrict<List<ItemData>>(text, file.Relative); break;
                case StrictKind.Trainers: GameDataFiles.ReadStrict<List<TrainerRecord>>(text, file.Relative); break;
                case StrictKind.Chunk: GameDataFiles.ReadStrict<WorldChunkFile>(text, file.Relative); break;
                case StrictKind.Area: GameDataFiles.ReadStrict<WorldAreaFile>(text, file.Relative); break;
                case StrictKind.Matrix: GameDataFiles.ReadStrict<WorldMatrixFile>(text, file.Relative); break;
                case StrictKind.Habitats: GameDataFiles.ReadStrict<WorldHabitatsFile>(text, file.Relative); break;
                case StrictKind.Calendar: GameDataFiles.ReadStrict<WorldCalendarFile>(text, file.Relative); break;
                case StrictKind.Encounters: GameDataFiles.ReadStrict<WorldEncountersFile>(text, file.Relative); break;
                case StrictKind.FacilitySets: GameDataFiles.ReadStrict<FacilitySetData>(text, file.Relative); break;
                case StrictKind.FacilityTrainers: GameDataFiles.ReadStrict<Models.Facilities.FacilityTrainersFile>(text, file.Relative); break;
                case StrictKind.FacilityPrizes: GameDataFiles.ReadStrict<Models.Facilities.FacilityPrizesFile>(text, file.Relative); break;
                case StrictKind.TreeCast: GameDataFiles.ReadStrict<Models.Facilities.TreeCastFile>(text, file.Relative); break;
            }
            return null;
        }
        catch (InvalidDataException e)
        {
            return e.Message;
        }
    }

    private static IEnumerable<string> Files(string folder) =>
        Directory.Exists(folder)
            ? Directory.GetFiles(folder, "*.json").OrderBy(f => f, StringComparer.Ordinal)
            : Enumerable.Empty<string>();

    private static StrictFile Make(string data, string path, StrictKind kind) =>
        new(path, Path.GetRelativePath(data, path).Replace('\\', '/'), kind);
}
