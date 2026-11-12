using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models.StageShow;

/// <summary>Where a prop is worn on a Pokémon's own model (plan 42 · PS1; <c>Graphics/PropAnchors.cs</c> finds each).</summary>
public enum PropSlot { Head, Face, Neck, Back, Hand, Tail }

/// <summary>A prop's mood, ours (none of them a contest's quality): a show asks for one, and pieces of it draw the crowd's eyes further.</summary>
public enum PropMood { Bright, Dashing, Gentle, Eerie }

/// <summary>What a prop does in a performer's cue, played by its age on the stage.</summary>
public enum PropTrick { Spin, Wave, Toss, Shine, Bounce, Ring }

/// <summary>
/// One prop of <c>Data/show-props.json</c> (ours; <c>docs/data-files.md</c>, "Props"): its id (its words are
/// <c>stageShow.prop.&lt;id in camel case&gt;</c>), its slot, mood and trick, its two colours (body and trim, <c>#RRGGBB</c>)
/// and the shape <c>Graphics/ShowPropModels.cs</c> sculpts.
/// </summary>
public sealed class ShowProp
{
    [DataRequired] public string Id { get; set; } = "";
    [DataRequired] public PropSlot Slot { get; set; }
    [DataRequired] public PropMood Mood { get; set; }
    [DataRequired] public PropTrick Trick { get; set; }
    [DataRequired] public List<string> Colors { get; set; } = new();
    [DataRequired] public string Shape { get; set; } = "";

    /// <summary>The key of the piece's name in <c>ui.json</c>.</summary>
    public string NameKey => "stageShow.prop." + ShowPropDatabase.KeyOf(Id);
}

public sealed class ShowPropsFile
{
    [DataRequired] public List<ShowProp> Props { get; set; } = new();
}

/// <summary>The props' table (<c>Data/show-props.json</c>, read strictly through <see cref="GameDataFiles"/>).</summary>
public static class ShowPropDatabase
{
    public const string FileName = "show-props.json";

    private static readonly Lazy<List<ShowProp>> all = new(() => GameDataFiles.Load<ShowPropsFile>(FileName, strict: true).Props);

    /// <summary>Every prop in the table's order.</summary>
    public static IReadOnlyList<ShowProp> All => all.Value;

    public static ShowProp? Get(string id) => All.FirstOrDefault(p => p.Id == id);

    /// <summary>The props of a slot, in the table's order.</summary>
    public static IEnumerable<ShowProp> Of(PropSlot slot) => All.Where(p => p.Slot == slot);

    /// <summary>An id in camel case: <c>top_hat_black</c> is <c>topHatBlack</c>, a key's segment being letters and digits.</summary>
    public static string KeyOf(string id)
    {
        var parts = id.Split('_', StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Select((p, k) => k == 0 ? p : char.ToUpperInvariant(p[0]) + p[1..]));
    }

    /// <summary>A colour of the table (<c>#RRGGBB</c>) as its three bytes.</summary>
    public static (byte R, byte G, byte B) ColourOf(string hex)
    {
        string h = hex.TrimStart('#');
        if (h.Length != 6) throw new FormatException($"'{hex}' is no colour of #RRGGBB");
        int v = Convert.ToInt32(h, 16);
        return ((byte)(v >> 16), (byte)(v >> 8), (byte)v);
    }
}

/// <summary>How many of each prop the player has (plan 42 · PS1): <see cref="Most"/> of a piece at most.</summary>
public sealed class PropTrunk
{
    /// <summary>The most of one piece the trunk holds.</summary>
    public const int Most = 9;

    private readonly Dictionary<string, int> counts = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, int> Counts => counts;

    public int CountOf(string id) => counts.TryGetValue(id, out int n) ? n : 0;

    public bool Has(string id) => CountOf(id) > 0;

    /// <summary>The kinds of piece held.</summary>
    public int Kinds => counts.Count(c => c.Value > 0);

    /// <summary>Adds up to <paramref name="count"/> of a piece of the table; returns how many went in (none past <see cref="Most"/>).</summary>
    public int Add(string id, int count = 1)
    {
        if (count <= 0 || ShowPropDatabase.Get(id) == null) return 0;
        int have = CountOf(id);
        int put = Math.Min(count, Most - have);
        if (put <= 0) return 0;
        counts[id] = have + put;
        return put;
    }

    /// <summary>The pieces held of a slot, in the table's order.</summary>
    public IEnumerable<ShowProp> Of(PropSlot slot) => ShowPropDatabase.Of(slot).Where(p => Has(p.Id));

    public void Clear() => counts.Clear();
}

/// <summary>Why a piece can't be put on.</summary>
public enum DressRefusal { None, NotInTrunk, SlotTaken, Full, NoSuchSlot }

/// <summary>
/// A Pokémon's pieces (plan 42 · PS1): one to a slot and <see cref="Most"/> at most. Worn only on the stage and in the
/// dressing room; nothing of it changes the Pokémon.
/// </summary>
public sealed class Dressing
{
    public const int Most = 4;

    private readonly Dictionary<PropSlot, string> pieces = new();

    public IReadOnlyDictionary<PropSlot, string> Pieces => pieces;

    public int Count => pieces.Count;

    public string? In(PropSlot slot) => pieces.TryGetValue(slot, out var id) ? id : null;

    /// <summary>The pieces worn as props of the table, in the slots' order.</summary>
    public IEnumerable<ShowProp> Props =>
        Enum.GetValues<PropSlot>().Select(In).Where(id => id != null).Select(id => ShowPropDatabase.Get(id!)).Where(p => p != null)!;

    /// <summary>Why <paramref name="prop"/> can't be worn now, or <see cref="DressRefusal.None"/>; <paramref name="slots"/> the slots the body has.</summary>
    public DressRefusal WhyNot(ShowProp prop, IReadOnlyCollection<PropSlot>? slots = null)
    {
        if (slots != null && !slots.Contains(prop.Slot)) return DressRefusal.NoSuchSlot;
        if (pieces.ContainsKey(prop.Slot)) return DressRefusal.SlotTaken;
        if (pieces.Count >= Most) return DressRefusal.Full;
        return DressRefusal.None;
    }

    /// <summary>Puts a piece on; false (and nothing changed) where <see cref="WhyNot"/> refuses.</summary>
    public bool Wear(ShowProp prop, IReadOnlyCollection<PropSlot>? slots = null)
    {
        if (WhyNot(prop, slots) != DressRefusal.None) return false;
        pieces[prop.Slot] = prop.Id;
        return true;
    }

    public bool Remove(PropSlot slot) => pieces.Remove(slot);

    public void Clear() => pieces.Clear();

    /// <summary>How many pieces are of <paramref name="mood"/>.</summary>
    public int Matching(PropMood mood) => Props.Count(p => p.Mood == mood);

    public Dressing Copy()
    {
        var d = new Dressing();
        foreach (var (slot, id) in pieces) d.pieces[slot] = id;
        return d;
    }

    /// <summary>The ids worn, in the slots' order (what a save keeps).</summary>
    public List<string> Ids() => Enum.GetValues<PropSlot>().Select(In).Where(id => id != null).Select(id => id!).ToList();

    /// <summary>A dressing of these ids, the unknown and a second of a slot passed over.</summary>
    public static Dressing Of(IEnumerable<string> ids)
    {
        var d = new Dressing();
        foreach (var id in ids)
            if (ShowPropDatabase.Get(id) is { } p) d.Wear(p);
        return d;
    }
}
