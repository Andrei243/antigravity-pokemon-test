using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 30 · FM10: Kalos's cast in the grown kit. Every look a person of Kalos stands as (its open areas' people and its
/// rooms', the heroes, the rival, the professor) and every look its trainers' classes stand on the platform in has a look
/// of the grown kit: a twin of the same name in <c>characters.json</c>'s <c>grown</c> block, asked for by
/// <see cref="CharacterStyles.Get(string, CharacterKit)"/>. None of them is the plain default in either kit, and nothing of it
/// reaches the game of today: Kalos's file still says the chibi kit (the gate is the user's), so every look stays chibi
/// where it is asked for plainly.
/// </summary>
public class KalosCastGrownTests
{
    private readonly ITestOutputHelper output;
    public KalosCastGrownTests(ITestOutputHelper output) => this.output = output;

    private static CharacterStyles Table => CharacterStyles.Default;

    private static readonly Lazy<World> Kalos = new(() => World.LoadAll().Single(w => w.Index.Region == "Kalos"));
    private static readonly Lazy<Dictionary<string, Map>> Built = new(() => Kalos.Value.BuildMaps().ToDictionary(m => m.Name));
    private static readonly Lazy<Func<string, Map>> Rooms = new(MapDatabase.Fresh);

    /// <summary>Every look Kalos's people and classes stand as, with where each was found.</summary>
    internal static SortedDictionary<string, string> KalosLooks()
    {
        var looks = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        void Note(string look, string where) { if (!looks.ContainsKey(look)) looks[look] = where; }

        var kalos = RegionDatabase.Get("Kalos")!;
        foreach (string name in kalos.Maps)
        {
            var map = Built.Value.TryGetValue(name, out var built) ? built : Rooms.Value(name);
            foreach (var n in map.Everyone)
            {
                if (n.IsThing || n.IsPokemon || CharacterModels.KindOf(n.NpcType) != RigKind.Humanoid) continue;
                // The player, the assistant and the rival who is the hero the player isn't: both ways round
                foreach (var look in new[] { PlayerLook.Boy, PlayerLook.Girl })
                    Note(PlayerIdentity.CharacterFor(n.NpcType, look), $"{name}: {n.Name}");
            }
        }
        Note(kalos.Heroes.Boy, "the boy hero");
        Note(kalos.Heroes.Girl, "the girl hero");
        if (kalos.Professor is { } professor) Note(professor.Look, "the professor");
        foreach (var record in TrainerDatabase.OfRegion("Kalos"))
        {
            if (record.Class is "No Data" || TrainerDatabase.IsRival(record.Class)) continue;
            if (!Table.Classes.TryGetValue(record.Class, out var named)) { Note(record.Class, $"the class {record.Class}"); continue; }
            foreach (string look in named ?? Array.Empty<string>()) Note(look, $"the class {record.Class}");
        }
        return looks;
    }

    [Fact]
    public void EveryLookOfKalosIsALookOfTheTable()
    {
        var plain = KalosLooks().Where(l => !Table.Has(l.Key)).Select(l => $"{l.Key} ({l.Value})").ToList();
        Assert.True(plain.Count == 0, "Kalos's people standing in the plain default: " + string.Join(", ", plain));
    }

    [Fact]
    public void EveryLookOfKalosHasAGrownLookOfItsOwn()
    {
        var looks = KalosLooks();
        output.WriteLine($"{looks.Count} looks of Kalos: {string.Join(", ", looks.Keys)}");
        var missing = new List<string>();
        foreach (var (look, where) in looks)
        {
            if (!Table.Has(look)) continue;
            var grown = Table.Get(look, CharacterKit.Grown);
            // A twin of the grown block (or a look made grown already), in a build of the grown kit
            if (grown.Kit != CharacterKit.Grown) { missing.Add($"{look} ({where})"); continue; }
            Assert.Equal(CharacterKit.Grown, CharacterStyle.KitOf(grown.Build));
        }
        Assert.True(missing.Count == 0, "Kalos's looks with no grown look: " + string.Join(", ", missing));
    }

    [Fact]
    public void KalosIsGrownThroughItsKitAndItsLooksStayTwins()
    {
        // The gate (plan 30 · FM10, "Done when"), opened by the user on 2026-11-12: Kalos's file says the grown kit, and
        // every look of Kalos asked for plainly is still the chibi look it was (a twin is reached through the region's
        // kit alone, `CharacterStyle.InKit`), so the grown cast shows only where the place's kit is grown
        Assert.Equal(CharacterKit.Grown, RegionDatabase.Get("Kalos")!.Kit);
        foreach (string look in KalosLooks().Keys.Where(Table.Has))
        {
            Assert.Equal(CharacterKit.Chibi, Table.Get(look).Kit);
            Assert.Equal(CharacterKit.Chibi, CharacterStyle.For(look).Kit);
            // ...and the grown kit's name for it is the twin's build
            string grownName = CharacterStyle.InKit(look, CharacterKit.Grown);
            Assert.Equal(Table.Get(look, CharacterKit.Grown).Build, CharacterStyle.For(grownName).Build);
        }
    }

    [Fact]
    public void TheBoutiquesGarmentsAreWornInTheGrownKit()
    {
        // Plan 11 · C10's outfit is the look's garments put on whatever kit it is made in: the hero dressed at a boutique
        // is the grown twin dressed the same in Kalos's kit
        const string Dressed = "DAWN@red_cap.-.denim_jeans.-.no_bag";
        var chibi = CharacterStyle.For(Dressed);
        var grown = CharacterStyle.For(CharacterStyle.InKit(Dressed, CharacterKit.Grown));
        Assert.Equal(CharacterKit.Grown, grown.Kit);
        Assert.Equal(BodyBuild.Teen, grown.Build);
        Assert.Equal(Headwear.Cap, grown.Hat);
        Assert.Equal(chibi.HatColor, grown.HatColor);
        Assert.Equal(chibi.Bottom, grown.Bottom);
        Assert.Equal(chibi.Bag, grown.Bag);
        Assert.Equal(CharacterKit.Chibi, chibi.Kit);
    }

    [Fact]
    public void TheCastIsBuiltForItsAge()
    {
        // The heroes, the rival and the four friends are teenagers; the children are children; the old are old; the
        // broad trades are broad (style guide, "Grown characters", the builds' "For")
        foreach (string teen in new[] { "player", "dawn", "serena", "calem", "shauna", "tierno", "trevor", "emma" })
            Assert.Equal(BodyBuild.Teen, Table.Get(teen, CharacterKit.Grown).Build);
        foreach (string child in new[] { "youngster", "lass", "preschooler_m", "preschooler_f", "school_kid_m", "school_kid_f", "bug_catcher" })
            Assert.Equal(BodyBuild.Child, Table.Get(child, CharacterKit.Grown).Build);
        foreach (string old in new[] { "old_man", "old_woman", "ramos", "drasna", "gurkinn" })
            Assert.Equal(BodyBuild.GrownElder, Table.Get(old, CharacterKit.Grown).Build);
        foreach (string broad in new[] { "hiker", "black_belt", "wulfric", "worker" })
            Assert.Equal(BodyBuild.GrownBroad, Table.Get(broad, CharacterKit.Grown).Build);
        // A woman of the grown cast has her figure, a girl and a man none
        Assert.True(CharacterStyle.For(CharacterStyle.InKit("diantha", CharacterKit.Grown)).CurvedFigure());
        Assert.False(CharacterStyle.For(CharacterStyle.InKit("lass", CharacterKit.Grown)).CurvedFigure());
        Assert.False(CharacterStyle.For(CharacterStyle.InKit("sycamore", CharacterKit.Grown)).CurvedFigure());
    }
}
