using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// The later games' key items that change a Pokémon's form outside a battle (plan 06 · R29; docs/mechanics/rulings.md,
/// "Forms changed outside a battle"), each told by the field use the importer gives it (<see cref="FieldUse"/>) and never
/// used up: the DNA Splicers, the N-Solarizer and N-Lunarizer and the Reins of Unity fuse two Pokémon of the team into
/// one and split them again, the Reveal Glass turns a Forces of Nature Pokémon between its Incarnate and Therian Formes,
/// the Prison Bottle unbinds Hoopa and binds it again, the Zygarde Cube turns Zygarde between its 10% and 50% Formes, and
/// the Rotom Catalog sends Rotom into the next appliance in it. A form's own move comes and goes with it as the newest
/// game has it (Pokémon Showdown has no bag, so the rules are the games' as published). Like <see cref="LaterItemUse"/>,
/// <see cref="WouldHelp"/> is the bag's ABLE and <see cref="Apply"/> does it. No drawing or input.
/// </summary>
public static class FormChanges
{
    public const string FieldUse = "FormChange";

    /// <summary>What a Pokémon is left knowing when splitting from its partner would leave it with no move (Sword and Shield's).</summary>
    public const string Fallback = "Confusion";

    /// <summary>Whether the item is one of these.</summary>
    public static bool Handles(ItemData item) => item.FieldUse == FieldUse;

    /// <summary>
    /// A fusion: the item, the Pokémon that takes the other in, its partner, the form the two make, the moves the
    /// form changes (each known one into the other, and back on splitting) and the move the form learns (forgotten on
    /// splitting).
    /// </summary>
    public sealed record Fusion(string Item, string Holder, string Partner, string Form, (string Own, string Fused)[] Swaps, string? Learns);

    /// <summary>Every fusion of the data, in the National Pokédex's order.</summary>
    public static IReadOnlyList<Fusion> Fusions { get; } = new[]
    {
        // Black 2 and White 2's: Scary Face and Glaciate become the partner's two
        new Fusion("DNA Splicers", "Kyurem", "Zekrom", "Kyurem-Black", new[] { ("Scary Face", "Fusion Bolt"), ("Glaciate", "Freeze Shock") }, null),
        new Fusion("DNA Splicers", "Kyurem", "Reshiram", "Kyurem-White", new[] { ("Scary Face", "Fusion Flare"), ("Glaciate", "Ice Burn") }, null),
        // Ultra Sun and Ultra Moon's, as Sword and Shield keep them: the partner's own move is learned
        new Fusion("N-Solarizer", "Necrozma", "Solgaleo", "Necrozma-Dusk", Array.Empty<(string, string)>(), "Sunsteel Strike"),
        new Fusion("N-Lunarizer", "Necrozma", "Lunala", "Necrozma-Dawn", Array.Empty<(string, string)>(), "Moongeist Beam"),
        // The Crown Tundra's
        new Fusion("Reins of Unity", "Calyrex", "Glastrier", "Calyrex-Ice", Array.Empty<(string, string)>(), "Glacial Lance"),
        new Fusion("Reins of Unity", "Calyrex", "Spectrier", "Calyrex-Shadow", Array.Empty<(string, string)>(), "Astral Barrage"),
    };

    /// <summary>The species the Reveal Glass turns, each between its own form and its Therian Forme.</summary>
    private static readonly HashSet<string> ForcesOfNature = new() { "Tornadus", "Thundurus", "Landorus", "Enamorus" };

    /// <summary>
    /// The Rotom Catalog's order, a page each use (our stand-in for the games' list to choose from): its own form,
    /// then the five appliances, each with the move it learns there.
    /// </summary>
    public static IReadOnlyList<(string? Form, string? Move)> RotomCatalog { get; } = new (string?, string?)[]
    {
        (null, null), ("Rotom-Heat", "Overheat"), ("Rotom-Wash", "Hydro Pump"), ("Rotom-Frost", "Blizzard"),
        ("Rotom-Fan", "Air Slash"), ("Rotom-Mow", "Leaf Storm")
    };

    /// <summary>What a plain Rotom is left knowing when its appliance's move was its only one (Thunder Shock, as the games have it).</summary>
    public const string RotomFallback = "Thunder Shock";

    /// <summary>The form the Zygarde Cube turns a Zygarde into: its 10% and 50% Formes, each with the ability it has.</summary>
    public static string? ZygardeOther(string? form) => form switch
    {
        null => "Zygarde-10",
        "Zygarde-10" => null,
        "Zygarde-10-Power-Construct" => "Zygarde-50-Power-Construct",
        "Zygarde-50-Power-Construct" => "Zygarde-10-Power-Construct",
        _ => form
    };

    /// <summary>The fusion this item would make of this Pokémon with someone of the team, or null.</summary>
    public static Fusion? FusionFor(ItemData item, Pokemon p, Party? party) =>
        p.FusedWith != null || p.Form != null ? null
        : Fusions.FirstOrDefault(f => f.Item == item.Name && f.Holder == p.Species.Name && PartnerIn(f, p, party) != null);

    /// <summary>The fusion this Pokémon is in that this item splits, or null.</summary>
    public static Fusion? FusionOf(ItemData item, Pokemon p) =>
        p.FusedWith is { } partner ? Fusions.FirstOrDefault(f => f.Item == item.Name && f.Holder == p.Species.Name && f.Form == p.Form && f.Partner == partner.Species.Name) : null;

    private static Pokemon? PartnerIn(Fusion f, Pokemon p, Party? party) =>
        party?.Members.FirstOrDefault(m => m != p && !m.IsEgg && m.Species.Name == f.Partner && m.FusedWith == null && p.Species.Form(f.Form) != null);

    /// <summary>Whether using it on this Pokémon would do anything.</summary>
    public static bool WouldHelp(ItemData item, Pokemon p, Party? party)
    {
        if (p.IsEgg || !Handles(item)) return false;
        string species = p.Species.Name;
        return item.Name switch
        {
            "DNA Splicers" or "N-Solarizer" or "N-Lunarizer" or "Reins of Unity" =>
                FusionOf(item, p) != null ? party != null && !party.IsFull : FusionFor(item, p, party) != null,
            "Reveal Glass" => ForcesOfNature.Contains(species) && p.Species.Form(species + "-Therian") != null,
            "Prison Bottle" => species == "Hoopa" && p.Species.Form("Hoopa-Unbound") != null,
            // Since R29b it teaches its five moves too, so any Zygarde
            "Zygarde Cube" => species == "Zygarde",
            "Rotom Catalog" => species == "Rotom",
            _ => false
        };
    }

    /// <summary>
    /// Uses it on the Pokémon and says what happened; nothing is changed when it would have no effect. A move the new
    /// form brings that doesn't fit is <see cref="ItemUseResult.Wanted"/>, for the bag to ask about as it asks of a level's.
    /// </summary>
    public static ItemUseResult Apply(ItemData item, Pokemon p, Party? party)
    {
        if (!WouldHelp(item, p, party)) return ItemUseResult.Nothing;
        string name = p.DisplayName;
        switch (item.Name)
        {
            case "DNA Splicers" or "N-Solarizer" or "N-Lunarizer" or "Reins of Unity":
                if (FusionOf(item, p) is { } fused) return Split(fused, p, party!);
                return Fuse(FusionFor(item, p, party)!, p, party!);

            case "Reveal Glass":
                p.ChangeForm(p.Form == null ? p.Species.Name + "-Therian" : null);
                return Changed(name);

            case "Prison Bottle":
            {
                bool unbinding = p.Form == null;
                p.ChangeForm(unbinding ? "Hoopa-Unbound" : null);
                Swap(p, unbinding ? "Hyperspace Hole" : "Hyperspace Fury", unbinding ? "Hyperspace Fury" : "Hyperspace Hole");
                return Changed(name);
            }

            case "Zygarde Cube":
                if (ZygardeOther(p.Form) == p.Form) return ItemUseResult.Nothing;
                p.ChangeForm(ZygardeOther(p.Form));
                return Changed(name);

            case "Rotom Catalog":
            {
                int at = Math.Max(0, CatalogPage(p));
                return ToAppliance(p, (at + 1) % RotomCatalog.Count);
            }
        }
        return ItemUseResult.Nothing;
    }

    // ---------------------------------------------------------------- the lists to choose from (plan 06 · R29b)

    /// <summary>The page of the Rotom Catalog a Rotom is on now: 0 its own form, 1 to 5 an appliance.</summary>
    public static int CatalogPage(Pokemon p) => RotomCatalog.ToList().FindIndex(c => c.Form == p.Form);

    /// <summary>
    /// The Rotom Catalog's list as the bag shows it (Sun and Moon's): the five appliances in the catalogue's order and
    /// last the light bulb, its own form. The <paramref name="option"/> of <see cref="Choose"/> is a row of it.
    /// </summary>
    public static IReadOnlyList<string> CatalogRows => new[]
    {
        Text.Of("bag.catalog.heat"), Text.Of("bag.catalog.wash"), Text.Of("bag.catalog.frost"),
        Text.Of("bag.catalog.fan"), Text.Of("bag.catalog.mow"), Text.Of("bag.catalog.bulb")
    };

    /// <summary>
    /// The moves the Zygarde Cube teaches (Sun and Moon's, by its cores): all five from the first, since this game has
    /// no cores to gather (our ruling, docs/mechanics/rulings.md, "What R29 left").
    /// </summary>
    public static IReadOnlyList<string> CubeMoves { get; } = new[] { "Thousand Arrows", "Thousand Waves", "Core Enforcer", "Extreme Speed", "Dragon Dance" };

    /// <summary>
    /// What the bag lists when this item is used on this Pokémon, a row to an answer (plan 06 · R29b): the Rotom
    /// Catalog's appliances, and the Zygarde Cube's CHANGE FORME (where its form can change) with the five moves it
    /// teaches. Null for an item that does its one thing at once.
    /// </summary>
    public static IReadOnlyList<string>? OptionsFor(ItemData item, Pokemon p)
    {
        if (!Handles(item) || p.IsEgg) return null;
        return item.Name switch
        {
            "Rotom Catalog" when p.Species.Name == "Rotom" => CatalogRows,
            "Zygarde Cube" when p.Species.Name == "Zygarde" =>
                (ZygardeOther(p.Form) != p.Form ? new[] { Text.Of("bag.cube.changeForme") } : Array.Empty<string>())
                    .Concat(CubeMoves.Select(LocalNames.Move)).ToArray(),
            _ => null
        };
    }

    /// <summary>
    /// The row of <see cref="OptionsFor"/> chosen: an appliance (or the bulb) for Rotom, or for Zygarde its other form
    /// or a move to teach. A move that doesn't fit is <see cref="ItemUseResult.Wanted"/>; one known already, or the
    /// appliance Rotom is in, changes nothing and says why.
    /// </summary>
    public static ItemUseResult Choose(ItemData item, Pokemon p, Party? party, int option)
    {
        if (OptionsFor(item, p) is not { } rows || option < 0 || option >= rows.Count) return ItemUseResult.Nothing;
        if (item.Name == "Rotom Catalog")
        {
            // Rows 0 to 4 are pages 1 to 5, the bulb page 0
            int page = option == RotomCatalog.Count - 1 ? 0 : option + 1;
            if (page == Math.Max(0, CatalogPage(p))) return new ItemUseResult(false, Text.Of("bag.noEffect"));
            return ToAppliance(p, page);
        }
        bool changes = ZygardeOther(p.Form) != p.Form;
        if (changes && option == 0)
        {
            p.ChangeForm(ZygardeOther(p.Form));
            return Changed(p.DisplayName);
        }
        string move = CubeMoves[option - (changes ? 1 : 0)];
        if (p.Knows(move)) return new ItemUseResult(false, Text.Of("bag.knowsAlready", ("name", p.DisplayName), ("move", LocalNames.Move(move))));
        if (p.TryLearn(move)) return new ItemUseResult(true, Text.Of("bag.learned", ("name", p.DisplayName), ("move", LocalNames.Move(move))));
        return new ItemUseResult(true, "") { Wanted = new[] { move } };
    }

    /// <summary>Sends Rotom into a page of the catalogue, its appliance's move with it (R29's rule).</summary>
    private static ItemUseResult ToAppliance(Pokemon p, int page)
    {
        var (_, oldMove) = RotomCatalog[Math.Max(0, CatalogPage(p))];
        var (form, move) = RotomCatalog[page];
        p.ChangeForm(form);
        var wanted = new List<string>();
        // The appliance's move takes the place of the last one's; none to take, it is learned where there is room
        int old = oldMove == null ? -1 : p.Moves.FindIndex(m => m.Name == oldMove);
        if (move == null)
        {
            if (old >= 0) p.Moves.RemoveAt(old);
            if (p.Moves.Count == 0) p.TryLearn(RotomFallback);
        }
        else if (old >= 0 && !p.Knows(move)) p.ReplaceMove(old, move);
        else if (!p.TryLearn(move) && !p.Knows(move)) wanted.Add(move);
        return Changed(p.DisplayName) with { Wanted = wanted };
    }

    private static ItemUseResult Changed(string name) => new(true, Text.Of("bag.changedForm", ("name", name)));

    private static ItemUseResult Fuse(Fusion f, Pokemon p, Party party)
    {
        var partner = PartnerIn(f, p, party)!;
        party.Remove(partner);
        p.FusedWith = partner;
        p.ChangeForm(f.Form);
        foreach (var (own, into) in f.Swaps) Swap(p, own, into);
        var wanted = new List<string>();
        if (f.Learns is { } move && !p.TryLearn(move) && !p.Knows(move)) wanted.Add(move);
        return new ItemUseResult(true, Text.Of("bag.fused", ("name", p.DisplayName), ("partner", partner.DisplayName))) { Wanted = wanted };
    }

    private static ItemUseResult Split(Fusion f, Pokemon p, Party party)
    {
        var partner = p.FusedWith!;
        p.FusedWith = null;
        p.ChangeForm(null);
        foreach (var (own, into) in f.Swaps) Swap(p, into, own);
        if (f.Learns is { } move && p.Moves.FindIndex(m => m.Name == move) is >= 0 and var at)
        {
            p.Moves.RemoveAt(at);
            if (p.Moves.Count == 0) p.TryLearn(Fallback);
        }
        // The partner comes back beside it
        party.Insert(party.Members.IndexOf(p) + 1, partner);
        return new ItemUseResult(true, Text.Of("bag.split", ("name", p.DisplayName), ("partner", partner.DisplayName)));
    }

    /// <summary>A known move turned into another in its place, keeping its PP Ups (none where it isn't known, or the other is).</summary>
    private static void Swap(Pokemon p, string from, string to)
    {
        int at = p.Moves.FindIndex(m => m.Name == from);
        if (at < 0 || p.Knows(to)) return;
        int ups = p.Moves[at].PPUps;
        p.ReplaceMove(at, to);
        p.Moves[at].PPUps = ups;
        p.Moves[at].CurrentPP = p.Moves[at].MaxPP;
    }
}
