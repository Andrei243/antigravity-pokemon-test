using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumTests.Golden;

/// <summary>
/// Plan 24 · X4: the JSON schemas of the files a person writes, in <c>docs/schemas/</c>, written from the records the
/// game reads them into by .NET's <see cref="JsonSchemaExporter"/> over <see cref="GameDataFiles.Strict"/>, so an
/// editor completes and checks their keys (<c>.vscode/settings.json</c> maps each folder's files to its schema). The
/// schemas are a convenience; the check is the strict load. Each file is held to what the code gives as a golden file
/// is: <c>GOLDEN=accept dotnet test PokemonPlatinumTests --filter Golden</c> rewrites them.
/// </summary>
public class SchemaTests
{
    public static string Folder => Path.Combine(Repository.Root, "docs", "schemas");

    /// <summary>Each schema by its file's name, with the record it describes.</summary>
    internal static readonly (string File, Type Type, string Title)[] Schemas =
    {
        ("world.schema.json", typeof(WorldIndexFile), "A region's world.json (Data/world/<region>/world.json)"),
        ("overlay.schema.json", typeof(WorldOverlayFile), "An area's overlay (Data/world/<region>/overlays/<key>.json)"),
        ("map.schema.json", typeof(MapFile), "A map file, a room (Data/maps/<Name>.json)"),
        ("sound-map.schema.json", typeof(SoundMapFile), "The sound map (Data/audio/sound-map.json)"),
        ("model-options.schema.json", typeof(ImportOptions), "A model file's options (overrides/models/<model>.json)"),
        ("materials.schema.json", typeof(MaterialsFile), "The crafting kit's materials (Data/materials.json)"),
        ("recipes.schema.json", typeof(RecipesFile), "The crafting kit's recipes (Data/recipes.json)"),
        ("training.schema.json", typeof(TrainingFile), "The training club's tables: the drills' prizes (Data/training.json)"),
        ("facility-trainers.schema.json", typeof(PokemonPlatinumEngine.Models.Facilities.FacilityTrainersFile), "The later battle places' trainers: classes, looks and names of ours (Data/facility-trainers.json)"),
        ("facility-prizes.schema.json", typeof(PokemonPlatinumEngine.Models.Facilities.FacilityPrizesFile), "The later battle places' prize counters, a stock to each place (Data/facility-prizes.json)"),
        // An open region's descriptions (plan 32 · OW2), read strictly by tools/OpenWorldBake
        ("open-world.schema.json", typeof(OpenWorldBake.OpenWorldFile), "An open region's world.json (Data/world3d/<region>/world.json)"),
        ("open-terrain.schema.json", typeof(OpenWorldBake.TerrainFile), "An open region's land: control points, brushes and paint (Data/world3d/<region>/terrain/*.json)"),
        ("open-carve.schema.json", typeof(OpenWorldBake.CarveFile), "What is cut out of an open region's land and added to it (Data/world3d/<region>/carve/*.json)"),
        ("open-water.schema.json", typeof(OpenWorldBake.WaterFile), "An open region's sea, lakes and rivers (Data/world3d/<region>/water.json)"),
        ("open-roads.schema.json", typeof(OpenWorldBake.RoadsFile), "An open region's roads (Data/world3d/<region>/roads.json)"),
        // Plan 40 · GE5: which charm a Pokédex filled earns
        ("charms.schema.json", typeof(CharmsFile), "The charms a Pokédex filled earns (Data/charms.json)")
    };

    /// <summary>The importer's patches (tools/DataImporter/Overrides): an object of entries by name, each the entry's fields, none required, and <c>new</c>.</summary>
    internal static readonly (string File, Type Type, string Title)[] Patches =
    {
        ("species-patch.schema.json", typeof(PokemonSpecies), "The importer's patch of species.json (tools/DataImporter/Overrides/species.json)"),
        ("items-patch.schema.json", typeof(ItemData), "The importer's patch of items.json (tools/DataImporter/Overrides/items.json)"),
        ("moves-patch.schema.json", typeof(MoveData), "The importer's patch of moves.json (tools/DataImporter/Overrides/moves.json)"),
        ("facility-sets-patch.schema.json", typeof(FrontierSetRecord), "The importer's patch of the later sets, by set id (tools/DataImporter/Overrides/facility-sets.json)")
    };

    [Fact]
    public void TheSchemasAreWhatTheCodeGives()
    {
        var moved = new List<string>();
        foreach (var (file, type, title) in Schemas)
            if (GoldenFile.Compare(Folder, file, Text(Of(type), title), GoldenFile.Accepting) is { } why) moved.Add(why);
        foreach (var (file, type, title) in Patches)
            if (GoldenFile.Compare(Folder, file, Text(PatchOf(type), title), GoldenFile.Accepting) is { } why) moved.Add(why);
        Assert.True(moved.Count == 0, string.Join("\n", moved));
    }

    [Fact]
    public void ASchemaRefusesWhatTheStrictReadingRefuses()
    {
        // The overlay's schema shuts its records: a person has no hidenBy, a door no mapp (a record written on one line)
        var overlay = Of(typeof(WorldOverlayFile));
        Assert.Equal(false, (bool?)overlay["additionalProperties"]);
        var person = Resolve(overlay, overlay["properties"]!["people"]!["additionalProperties"]!);
        Assert.NotNull(person["properties"]!["hiddenBy"]);
        Assert.Null(person["properties"]!["hidenBy"]);
        var door = Resolve(overlay, overlay["properties"]!["doors"]!["items"]!);
        Assert.NotNull(door["properties"]!["map"]);
        Assert.Equal(false, (bool?)door["additionalProperties"]);
        // A map file's required fields are the strict reading's
        var map = Of(typeof(MapFile));
        var required = map["required"]!.AsArray().Select(n => (string)n!).ToList();
        Assert.Contains("name", required);
        Assert.Contains("width", required);
    }

    // A "$ref" to a definition of the same schema, followed
    private static JsonNode Resolve(JsonNode root, JsonNode node)
    {
        if (node is JsonObject o && o["$ref"] is JsonValue r && ((string)r!).StartsWith("#/", StringComparison.Ordinal))
        {
            JsonNode current = root;
            foreach (string part in ((string)r!)[2..].Split('/')) current = current[part]!;
            return current;
        }
        return node;
    }

    private static string Text(JsonNode schema, string title)
    {
        var top = new JsonObject
        {
            ["$schema"] = "https://json-schema.org/draft/2020-12/schema",
            ["title"] = title,
            ["description"] = "Written by SchemaTests (plan 24 · X4) from the records the game reads the file into; don't edit by hand. The game reads the file strictly: a key no field has stops it."
        };
        foreach (var (key, value) in schema.AsObject().ToList())
        {
            schema.AsObject().Remove(key);
            top[key] = value;
        }
        return top.ToJsonString(new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }) + "\n";
    }

    /// <summary>A record's schema as the strict reading reads it.</summary>
    internal static JsonNode Of(Type type) =>
        JsonSchemaExporter.GetJsonSchemaAsNode(GameDataFiles.Strict, type, new JsonSchemaExporterOptions
        {
            TreatNullObliviousAsNonNullable = false,
            TransformSchemaNode = Transform
        });

    /// <summary>A patch file's schema: entries by name, each the record's fields, none required, any of them null (which takes the field away), and <c>new</c>.</summary>
    internal static JsonNode PatchOf(Type type)
    {
        var entry = Of(type).AsObject();
        entry.Remove("required");
        if (entry["properties"] is JsonObject properties)
        {
            foreach (var (name, value) in properties.ToList())
                properties[name] = new JsonObject { ["anyOf"] = new JsonArray(value!.DeepClone(), new JsonObject { ["type"] = "null" }) };
            properties["new"] = new JsonObject
            {
                ["type"] = "boolean",
                ["description"] = "An entry the import has none of by this name: added only when it says so, so a misspelt name stops the importer."
            };
        }
        var defs = entry["$defs"];
        entry.Remove("$defs");
        var top = new JsonObject { ["type"] = "object", ["additionalProperties"] = entry };
        if (defs != null) top["$defs"] = defs;
        return top;
    }

    /// <summary>
    /// A record written on one line (<see cref="OneLine{T}"/>) has a converter of its own, which the exporter can't see
    /// into: it is described from its properties here, its keys shut as the strict reading shuts them.
    /// </summary>
    private static JsonNode Transform(JsonSchemaExporterContext context, JsonNode schema)
    {
        var type = context.TypeInfo.Type;
        // The exporter leaves out a list's items where they are records of their own converter: put them in
        if (schema is JsonObject list && list["items"] == null && ElementOf(type) is { } element && IsOneLine(element))
        {
            list["items"] = OneLineSchema(element);
            return list;
        }
        if (!IsOneLine(Nullable.GetUnderlyingType(type) ?? type)) return schema;
        return OneLineSchema(type);
    }

    private static Type? ElementOf(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>) ? type.GetGenericArguments()[0] : null;

    private static bool IsOneLine(Type type) =>
        type.GetCustomAttribute<JsonConverterAttribute>()?.ConverterType is { IsGenericType: true } converter
        && converter.GetGenericTypeDefinition() == typeof(OneLine<>);

    private static JsonNode OneLineSchema(Type type)
    {
        var properties = new JsonObject();
        foreach (var property in type.GetProperties().Where(p => p.CanWrite))
            properties[JsonNamingPolicy.CamelCase.ConvertName(property.Name)] = Plain(property.PropertyType);
        return new JsonObject { ["type"] = "object", ["properties"] = properties, ["additionalProperties"] = false };
    }

    // What a field of a record written on one line holds: the plain kinds those records are made of
    private static JsonNode Plain(Type type)
    {
        var inner = Nullable.GetUnderlyingType(type);
        bool nullable = inner != null || !type.IsValueType;
        type = inner ?? type;
        JsonNode node;
        if (type == typeof(string)) node = Kind("string", nullable);
        else if (type == typeof(bool)) node = Kind("boolean", nullable);
        else if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte)) node = Kind("integer", nullable);
        else if (type == typeof(float) || type == typeof(double)) node = Kind("number", nullable);
        else if (type.IsEnum)
        {
            var values = new JsonArray(Enum.GetNames(type).Select(n => (JsonNode?)JsonValue.Create(n)).ToArray());
            if (nullable) values.Add(null);
            node = new JsonObject { ["enum"] = values };
        }
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            node = new JsonObject { ["type"] = nullable ? new JsonArray("array", "null") : "array", ["items"] = Plain(type.GetGenericArguments()[0]) };
        else node = new JsonObject();
        return node;
    }

    private static JsonNode Kind(string kind, bool nullable) =>
        new JsonObject { ["type"] = nullable ? new JsonArray(kind, "null") : kind };
}
