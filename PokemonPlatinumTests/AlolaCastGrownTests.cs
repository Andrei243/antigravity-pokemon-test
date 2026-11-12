using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

/// <summary>
/// Alola's cast in the grown kit (the user's switch of 2026-11-12, on plan 30 · FM10's pattern for Kalos): every look a
/// person of Alola stands as (its islands' people and its rooms', the heroes, the rival, the professor) and every look its
/// trainers' classes stand on the platform in has a twin of the same name in <c>characters.json</c>'s <c>grown</c> block,
/// asked for by <see cref="CharacterStyles.Get(string, CharacterKit)"/>; Alola's file says the grown kit, and every look
/// asked for plainly stays the chibi look, so nothing of Alola's grown cast reaches a chibi region.
/// </summary>
public class AlolaCastGrownTests
{
    private readonly ITestOutputHelper output;
    public AlolaCastGrownTests(ITestOutputHelper output) => this.output = output;

    private static CharacterStyles Table => CharacterStyles.Default;

    private static readonly Lazy<World> Alola = new(() => World.LoadAll().Single(w => w.Index.Region == "Alola"));
    private static readonly Lazy<Dictionary<string, Map>> Built = new(() => Alola.Value.BuildMaps().ToDictionary(m => m.Name));
    private static readonly Lazy<Func<string, Map>> Rooms = new(MapDatabase.Fresh);

    /// <summary>Every look Alola's people and classes stand as, with where each was found.</summary>
    internal static SortedDictionary<string, string> AlolaLooks()
    {
        var looks = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        void Note(string look, string where) { if (!looks.ContainsKey(look)) looks[look] = where; }

        var alola = RegionDatabase.Get("Alola")!;
        foreach (string name in alola.Maps)
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
        Note(alola.Heroes.Boy, "the boy hero");
        Note(alola.Heroes.Girl, "the girl hero");
        if (alola.Professor is { } professor) Note(professor.Look, "the professor");
        foreach (var record in TrainerDatabase.OfRegion("Alola"))
        {
            if (record.Class is "No Data" || TrainerDatabase.IsRival(record.Class)) continue;
            if (!Table.Classes.TryGetValue(record.Class, out var named)) { Note(record.Class, $"the class {record.Class}"); continue; }
            foreach (string look in named ?? Array.Empty<string>()) Note(look, $"the class {record.Class}");
        }
        return looks;
    }

    [Fact]
    public void EveryLookOfAlolaIsALookOfTheTable()
    {
        var plain = AlolaLooks().Where(l => !Table.Has(l.Key)).Select(l => $"{l.Key} ({l.Value})").ToList();
        Assert.True(plain.Count == 0, "Alola's people standing in the plain default: " + string.Join(", ", plain));
    }

    [Fact]
    public void EveryLookOfAlolaHasAGrownLookOfItsOwn()
    {
        var looks = AlolaLooks();
        output.WriteLine($"{looks.Count} looks of Alola: {string.Join(", ", looks.Keys)}");
        var missing = new List<string>();
        foreach (var (look, where) in looks)
        {
            if (!Table.Has(look)) continue;
            var grown = Table.Get(look, CharacterKit.Grown);
            // A twin of the grown block (or a look made grown already), in a build of the grown kit
            if (grown.Kit != CharacterKit.Grown) { missing.Add($"{look} ({where})"); continue; }
            Assert.Equal(CharacterKit.Grown, CharacterStyle.KitOf(grown.Build));
        }
        Assert.True(missing.Count == 0, "Alola's looks with no grown look: " + string.Join(", ", missing));
    }

    [Fact]
    public void AlolaIsGrownThroughItsKitAndItsLooksStayTwins()
    {
        // The gate (plan 30 · FM10, "Done when"), opened by the user on 2026-11-12: Alola's file says the grown kit, and
        // every look of Alola asked for plainly is still the chibi look it was (a twin is reached through the region's
        // kit alone, `CharacterStyle.InKit`), so the grown cast shows only where the place's kit is grown
        Assert.Equal(CharacterKit.Grown, RegionDatabase.Get("Alola")!.Kit);
        foreach (string look in AlolaLooks().Keys.Where(Table.Has))
        {
            Assert.Equal(CharacterKit.Chibi, Table.Get(look).Kit);
            Assert.Equal(CharacterKit.Chibi, CharacterStyle.For(look).Kit);
            // ...and the grown kit's name for it is the twin's build
            string grownName = CharacterStyle.InKit(look, CharacterKit.Grown);
            Assert.Equal(Table.Get(look, CharacterKit.Grown).Build, CharacterStyle.For(grownName).Build);
        }
    }

    [Fact]
    public void TheCastIsBuiltForItsAge()
    {
        // The rival, Lillie, Gladion and the captains are teenagers; the professor, Lusamine, Guzma and the trades adults;
        // Hala broad; the old scholar and the Battle Tree's keeper elders (style guide, "Grown characters", "Alola's cast")
        foreach (string teen in new[] { "hau", "lillie", "gladion", "ilima", "mallow", "lana", "kiawe", "acerola", "sophocles", "hapu" })
            Assert.Equal(BodyBuild.Teen, Table.Get(teen, CharacterKit.Grown).Build);
        foreach (string adult in new[] { "kukui", "olivia", "nanu", "lusamine", "guzma", "wicke", "veyra", "swimmer_m", "teacher" })
            Assert.Equal(BodyBuild.Grown, Table.Get(adult, CharacterKit.Grown).Build);
        Assert.Equal(BodyBuild.GrownBroad, Table.Get("hala", CharacterKit.Grown).Build);
        foreach (string old in new[] { "expert_m", "tree_ione" })
            Assert.Equal(BodyBuild.GrownElder, Table.Get(old, CharacterKit.Grown).Build);
        Assert.True(CharacterStyle.For(CharacterStyle.InKit("olivia", CharacterKit.Grown)).CurvedFigure());
        Assert.False(CharacterStyle.For(CharacterStyle.InKit("kukui", CharacterKit.Grown)).CurvedFigure());
    }
}
