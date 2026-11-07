using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim.Link;

/// <summary>
/// A battle's log, questions, answers and the link's messages as plain JSON (plan 28 · I1): what a server sends two
/// players and what they send back. Every event, request, choice and message is written as an object of its fields in
/// the order they are declared, named in camel case, a type of several kinds with its wire name in <c>"wire"</c>, and
/// read back into the same records: a Pokémon as the <see cref="PokemonRef"/> a <see cref="BattleRoster"/> gives it, a
/// species by its National Pokédex number, an enum by its name. Everything goes in an envelope with the protocol's
/// <see cref="Version"/>, which a reader refuses when it is another.
/// <para>
/// A type is written only if it is named in <see cref="EventTypes"/>, <see cref="RequestTypes"/> or
/// <see cref="MessageTypes"/>, so a new event of the log that nobody gave a wire name fails the round trip of the golden
/// battles (<c>BattleWireTests</c>) rather than going out as something else. Nothing here rolls or reads a clock: the same
/// values write the same text on any machine. GPU-free.
/// </para>
/// </summary>
public static class BattleWire
{
    /// <summary>The protocol's version: both ends of a link must write the same.</summary>
    public const int Version = 1;

    /// <summary>Every event of the log, under its wire name (its type's name).</summary>
    public static readonly IReadOnlyList<Type> EventTypes = new[]
    {
        typeof(Said), typeof(TrainersStand), typeof(Seen), typeof(Entered), typeof(Recalled), typeof(Left), typeof(Lunged),
        typeof(MoveShown), typeof(Struck), typeof(HitSounded), typeof(Confused), typeof(HpChanged), typeof(StageChanged),
        typeof(StatusChanged), typeof(Fainted), typeof(GotAway), typeof(Vanished), typeof(Reappeared), typeof(Reshaped),
        typeof(SubstituteChanged), typeof(AbilityShown), typeof(ItemShown), typeof(WeatherChanged), typeof(WeatherContinues),
        typeof(SideConditionChanged), typeof(FieldConditionChanged), typeof(PlaceConditionChanged), typeof(ExpGained),
        typeof(LevelRose), typeof(MoveWanted), typeof(BallThrown), typeof(Caught), typeof(TrainerReacts), typeof(Won),
        typeof(Ended), typeof(MomentShown), typeof(AheadLanded), typeof(VolatileChanged), typeof(CameInWith), typeof(Charging),
        typeof(MegaEvolved), typeof(ZPowerUsed), typeof(UltraBursted), typeof(PlacesSwapped), typeof(Dynamaxed),
        typeof(DynamaxEnded), typeof(MaxMoveUsed), typeof(Terastallized), typeof(MarkChanged), typeof(FormHpChanged),
        typeof(CalledForHelp), typeof(AllyJoined), typeof(Rotated), typeof(Judged), typeof(RoyalPlaced)
    };

    /// <summary>Every question the core asks.</summary>
    public static readonly IReadOnlyList<Type> RequestTypes = new[] { typeof(ActionRequest), typeof(ReplacementRequest), typeof(SwitchOfferRequest) };

    /// <summary>Every message of a link (<see cref="LinkMessage"/>).</summary>
    public static readonly IReadOnlyList<Type> MessageTypes = new[]
    {
        typeof(LinkHello), typeof(LinkPick), typeof(LinkTurn), typeof(LinkAsk), typeof(LinkAnswer), typeof(LinkForfeit),
        typeof(LinkRefused), typeof(LinkOver)
    };

    private static readonly Dictionary<string, Type> ByName =
        EventTypes.Concat(RequestTypes).Concat(MessageTypes).ToDictionary(t => t.Name, StringComparer.Ordinal);

    private static readonly HashSet<Type> Named = ByName.Values.ToHashSet();

    /// <summary>The kinds whose values are written with their wire name, since a field of one may hold any of its types.</summary>
    private static readonly Type[] Families = { typeof(BattleEvent), typeof(BattleRequest), typeof(LinkMessage) };

    /// <summary>The field an object of several kinds gives its wire name in.</summary>
    public const string TypeField = "wire";

    private static readonly JsonWriterOptions WriterOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    // ------------------------------------------------------------------ the envelopes

    /// <summary>Events of a battle's log, their Pokémon named by <paramref name="roster"/>.</summary>
    public static string WriteEvents(IEnumerable<BattleEvent> events, BattleRoster roster) =>
        Envelope("events", events.ToList(), typeof(List<BattleEvent>), roster);

    public static List<BattleEvent> ReadEvents(string json, BattleRoster roster) =>
        (List<BattleEvent>)Open(json, "events", typeof(List<BattleEvent>), roster)!;

    public static string WriteRequest(BattleRequest request, BattleRoster roster) => Envelope("request", request, typeof(BattleRequest), roster);

    public static BattleRequest ReadRequest(string json, BattleRoster roster) => (BattleRequest)Open(json, "request", typeof(BattleRequest), roster)!;

    /// <summary>Answers, which name places and positions and never a Pokémon.</summary>
    public static string WriteChoices(IEnumerable<BattleChoice> choices) => Envelope("choices", choices.ToList(), typeof(List<BattleChoice>), null);

    public static List<BattleChoice> ReadChoices(string json) => (List<BattleChoice>)Open(json, "choices", typeof(List<BattleChoice>), null)!;

    /// <summary>A link's message; the roster names the Pokémon a turn's events or a question carry.</summary>
    public static string WriteMessage(LinkMessage message, BattleRoster? roster) => Envelope("message", message, typeof(LinkMessage), roster);

    public static LinkMessage ReadMessage(string json, BattleRoster? roster) => (LinkMessage)Open(json, "message", typeof(LinkMessage), roster)!;

    private static string Envelope(string field, object value, Type declared, BattleRoster? roster)
    {
        using var stream = new MemoryStream();
        using (var w = new Utf8JsonWriter(stream, WriterOptions))
        {
            w.WriteStartObject();
            w.WriteNumber("v", Version);
            w.WritePropertyName(field);
            Write(w, value, declared, roster);
            w.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static object? Open(string json, string field, Type declared, BattleRoster? roster)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException e)
        {
            throw new BattleWireException($"Not the wire's JSON: {e.Message}");
        }
        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("v", out var v) || v.ValueKind != JsonValueKind.Number)
                throw new BattleWireException("No protocol version");
            if (v.GetInt32() != Version) throw new BattleWireException($"Protocol {v.GetInt32()}, where this game speaks {Version}");
            if (!root.TryGetProperty(field, out var body)) throw new BattleWireException($"No {field} in the envelope");
            return Read(body, declared, roster);
        }
    }

    // ------------------------------------------------------------------ turning a view round

    /// <summary>
    /// Events as a screen that stands on the other side would play them (plan 28 · I3): every side named turned round
    /// (a place, a side's conditions), a victory a defeat and a defeat a victory, and the <see cref="Won"/> that tells the
    /// player's side it won moved to the side that did. A Pokémon stays the one it is: only the places it stands in
    /// turn. What a link battle's side B is sent names its own places by the core's other side; its screen, like every
    /// battle screen, stands on the player's side, so it plays its view turned round. Turned twice, events are what they were.
    /// </summary>
    public static List<BattleEvent> Turned(IReadOnlyList<BattleEvent> events, BattleRoster roster)
    {
        var turned = (List<BattleEvent>)Turned(events.ToList(), typeof(List<BattleEvent>), roster);
        // The player's side is told it won: turned round, the other side's win is the one the player's side is told of
        bool hadWon = turned.Any(e => e is Won);
        turned.RemoveAll(e => e is Won);
        int ended = turned.FindIndex(e => e is Ended { Result: BattleResult.PlayerVictory });
        if (ended >= 0 && !hadWon)
        {
            // After the last faint, as the core says it before its own win's lines; a match given up has none, and no Won
            // either way (the core's forfeit says none)
            int lastLine = turned.FindLastIndex(ended, e => e is Fainted || e is Said s && s.Shows.Concat(s.OnImpact).Any(i => i is Fainted));
            if (lastLine >= 0) turned.Insert(lastLine + 1, new Won());
        }
        return turned;
    }

    /// <summary>A question turned round (<see cref="Turned(IReadOnlyList{BattleEvent}, BattleRoster)"/>).</summary>
    public static BattleRequest Turned(BattleRequest request, BattleRoster roster) => (BattleRequest)Turned(request, typeof(BattleRequest), roster);

    /// <summary>What a place may answer, turned round.</summary>
    public static PlaceOptions Turned(PlaceOptions options) => options with { Place = Turned(options.Place) };

    /// <summary>A choice turned round: its place and its target.</summary>
    public static BattleChoice Turned(BattleChoice choice) => (BattleChoice)Turned(choice, typeof(BattleChoice), null);

    /// <summary>A place turned round.</summary>
    public static Place Turned(Place place) => new(place.Side == BattleSide.Player ? BattleSide.Enemy : BattleSide.Player, place.Slot);

    private static object Turned(object value, Type declared, BattleRoster? roster)
    {
        using var stream = new MemoryStream();
        using (var w = new Utf8JsonWriter(stream, WriterOptions)) Write(w, value, declared, roster);
        using var document = JsonDocument.Parse(stream.ToArray());
        return Read(document.RootElement, declared, roster, turn: true)!;
    }

    private static object Turn(object value, bool turn)
    {
        if (!turn) return value;
        return value switch
        {
            BattleSide side => side == BattleSide.Player ? BattleSide.Enemy : BattleSide.Player,
            BattleResult.PlayerVictory => BattleResult.PlayerDefeat,
            BattleResult.PlayerDefeat => BattleResult.PlayerVictory,
            _ => value
        };
    }

    // ------------------------------------------------------------------ writing

    private static void Write(Utf8JsonWriter w, object? value, Type declared, BattleRoster? roster)
    {
        if (value == null)
        {
            w.WriteNullValue();
            return;
        }
        var type = Nullable.GetUnderlyingType(declared) ?? declared;
        switch (value)
        {
            case string s: w.WriteStringValue(s); return;
            case bool b: w.WriteBooleanValue(b); return;
            case int i: w.WriteNumberValue(i); return;
            case uint u: w.WriteNumberValue(u); return;
            case long l: w.WriteNumberValue(l); return;
            case Enum e: w.WriteStringValue(e.ToString()); return;
            case Pokemon p:
                if (roster == null) throw new BattleWireException("A Pokémon can only be named with a roster");
                Write(w, roster.RefOf(p), typeof(PokemonRef), roster);
                return;
            case PokemonSpecies species: w.WriteNumberValue(species.DexNumber); return;
            case float or double or decimal:
                // A fraction is written differently by different machines: nothing on the wire is one
                throw new BattleWireException($"{type.Name} has a fraction, which the wire never carries");
        }
        if (value is IEnumerable many && value is not string)
        {
            var element = ElementOf(type) ?? typeof(object);
            w.WriteStartArray();
            foreach (var item in many) Write(w, item, element, roster);
            w.WriteEndArray();
            return;
        }

        var actual = value.GetType();
        bool family = Families.Any(f => f.IsAssignableFrom(actual));
        if (family && !Named.Contains(actual)) throw new BattleWireException($"{actual.Name} has no wire name");
        w.WriteStartObject();
        if (family) w.WriteString(TypeField, actual.Name);
        foreach (var field in FieldsOf(actual))
        {
            w.WritePropertyName(field.Json);
            Write(w, field.Property.GetValue(value), field.Property.PropertyType, roster);
        }
        w.WriteEndObject();
    }

    // ------------------------------------------------------------------ reading

    private static object? Read(JsonElement e, Type declared, BattleRoster? roster, bool turn = false)
    {
        if (e.ValueKind == JsonValueKind.Null) return null;
        var type = Nullable.GetUnderlyingType(declared) ?? declared;
        try
        {
            if (type == typeof(string)) return e.GetString();
            if (type == typeof(bool)) return e.GetBoolean();
            if (type == typeof(int)) return e.GetInt32();
            if (type == typeof(uint)) return e.GetUInt32();
            if (type == typeof(long)) return e.GetInt64();
            if (type.IsEnum) return Turn(Enum.Parse(type, e.GetString() ?? "", ignoreCase: false), turn);
            if (type == typeof(Pokemon))
            {
                if (roster == null) throw new BattleWireException("A Pokémon can only be named with a roster");
                return roster.Resolve((PokemonRef)Read(e, typeof(PokemonRef), roster, turn: false)!);
            }
            if (type == typeof(PokemonSpecies))
                return PokemonDatabase.GetByDex(e.GetInt32()) ?? throw new BattleWireException($"No species is number {e.GetInt32()}");
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException or ArgumentException)
        {
            throw new BattleWireException($"Not a {type.Name}: {e}");
        }

        if (ElementOf(type) is { } element && type != typeof(string))
        {
            if (e.ValueKind != JsonValueKind.Array) throw new BattleWireException($"Not a list of {element.Name}");
            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(element))!;
            foreach (var item in e.EnumerateArray()) list.Add(Read(item, element, roster, turn));
            return list;
        }

        if (e.ValueKind != JsonValueKind.Object) throw new BattleWireException($"Not a {type.Name}: {e}");
        var actual = type;
        if (Families.Any(f => f.IsAssignableFrom(type)))
        {
            if (!e.TryGetProperty(TypeField, out var named) || named.GetString() is not { } name || !ByName.TryGetValue(name, out var found)
                || !type.IsAssignableFrom(found))
                throw new BattleWireException($"No {type.Name} of that wire name: {e}");
            actual = found;
        }
        return Build(e, actual, roster, turn && actual != typeof(PokemonRef));
    }

    private static object Build(JsonElement e, Type type, BattleRoster? roster, bool turn)
    {
        var fields = FieldsOf(type);
        var ctor = ConstructorOf(type);
        var parameters = ctor?.GetParameters() ?? Array.Empty<ParameterInfo>();
        var args = parameters.Select(p =>
        {
            if (e.TryGetProperty(Camel(p.Name!), out var given)) return Read(given, p.ParameterType, roster, turn);
            if (p.HasDefaultValue) return p.DefaultValue;
            return p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null;
        }).ToArray();
        object made = ctor != null ? ctor.Invoke(args) : Activator.CreateInstance(type)!;
        var inCtor = parameters.Select(p => p.Name!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var field in fields)
        {
            if (inCtor.Contains(field.Property.Name) || !e.TryGetProperty(field.Json, out var given)) continue;
            if (field.Property.SetMethod is { IsPublic: true })
                field.Property.SetValue(made, Read(given, field.Property.PropertyType, roster, turn));
            else if (field.Property.GetValue(made) is IList into && Read(given, field.Property.PropertyType, roster, turn) is IList items)
                foreach (var item in items) into.Add(item);
        }
        return made;
    }

    // ------------------------------------------------------------------ a type's fields

    private sealed record Field(PropertyInfo Property, string Json);

    private static readonly ConcurrentDictionary<Type, Field[]> Fields = new();
    private static readonly ConcurrentDictionary<Type, ConstructorInfo?> Constructors = new();

    /// <summary>
    /// What is written of a type: its public properties in the order declared that a constructor takes, that can be set,
    /// or that are a list filled in place (a line's events); a property worked out from others (a place's number) is not.
    /// </summary>
    private static Field[] FieldsOf(Type type) => Fields.GetOrAdd(type, t =>
    {
        var ctor = ConstructorOf(t);
        var taken = (ctor?.GetParameters() ?? Array.Empty<ParameterInfo>()).Select(p => p.Name!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .Where(p => taken.Contains(p.Name) || p.SetMethod is { IsPublic: true } || typeof(IList).IsAssignableFrom(p.PropertyType))
            .OrderBy(p => p.MetadataToken)
            .Select(p => new Field(p, Camel(p.Name)))
            .ToArray();
    });

    /// <summary>The public constructor that takes the most of a type's properties (a record's own), or none for one made empty and filled.</summary>
    private static ConstructorInfo? ConstructorOf(Type type) => Constructors.GetOrAdd(type, t =>
    {
        var names = t.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return t.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Where(c => c.GetParameters().Length > 0 && c.GetParameters().All(p => names.Contains(p.Name!) && p.ParameterType != t))
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault();
    });

    private static Type? ElementOf(Type type)
    {
        if (type == typeof(string)) return null;
        if (type.IsArray) return type.GetElementType();
        var enumerable = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>) ? type
            : type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        return enumerable?.GetGenericArguments()[0];
    }

    private static string Camel(string name) => name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name[1..];
}
