using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 22 · U12: Unova's people and classes on looks of their own. Every person of Unova's open areas is a look of the
/// table, none of them the plain default nor a look of another class standing in; every trainer class of Unova's records
/// stands on the platform as a look (or, for a class that names nobody in particular, as the person); and every class the
/// overlays place has its eye theme in the sound map.
/// </summary>
public class UnovaCastTests
{
    private static readonly Lazy<World> Unova = new(() => World.LoadAll().Single(w => w.Index.Region == "Unova"));

    private static CharacterStyles Table => CharacterStyles.Default;

    /// <summary>What an overlay's person can be that is no look of the table.</summary>
    private static readonly HashSet<string> NotPeople = new(StringComparer.OrdinalIgnoreCase)
    {
        "Pokemon", "Rubble", "ItemBall", "GrottoHollow", "Player", "Assistant", "StrengthBoulder", "CutTree"
    };

    /// <summary>The looks of Unova's classes, each a design of its own (none a fold onto another class's look).</summary>
    private static readonly string[] ClassLooks =
    {
        "preschooler_m", "preschooler_f", "nursery_aide", "janitor", "physician", "baker", "depot_agent", "harlequin", "street_dancer",
        "musician", "backers_m", "backers_f", "smasher", "hoopster", "striker", "linebacker", "infielder", "backpacker_m",
        "backpacker_f", "plasma_grunt", "plasma_grunt_f", "plasma_sage", "plasma_former_m", "plasma_former_f", "veteran_f", "psychic_f"
    };

    [Fact]
    public void EveryPersonOfUnovaIsALookOfItsOwn()
    {
        var unova = Unova.Value;
        var problems = new List<string>();
        foreach (string key in unova.Index.Areas)
        {
            var overlay = unova.Overlay(key);
            if (overlay == null) continue;
            var file = unova.Area(key);
            foreach (var (id, person) in overlay.People ?? new())
            {
                var obj = file?.Objects.FirstOrDefault(o => o.Id == id);
                if (person.NpcType != null)
                {
                    if (NotPeople.Contains(person.NpcType) || person.NpcType.StartsWith("Rival:", StringComparison.Ordinal)) continue;
                    if (!Table.Has(person.NpcType)) problems.Add($"{key}: {id} is of type '{person.NpcType}', which is no look");
                    // Galactic's grunt is Sinnoh's: Team Plasma's people are Team Plasma's
                    if (person.NpcType.Equals("Grunt", StringComparison.OrdinalIgnoreCase)) problems.Add($"{key}: {id} wears Team Galactic's uniform");
                    continue;
                }
                if (obj == null || WorldMapBuilder.ObstacleFor(obj) != null || obj.Item != null || obj.Patch != null) continue;
                if (person.Trainer == null && WorldMapBuilder.SpeciesFor(obj.Looks) != null) continue;
                // Black 2's own name for the look, or a name the table gives the look of the same class: never the plain default
                if (!Table.Has(obj.Looks) && !Table.Fallbacks.ContainsKey(obj.Looks))
                    problems.Add($"{key}: {id} ({obj.Looks}) falls back to the plain default");
            }
        }
        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public void UnovasClassesAreLooksOfTheirOwnInTheFieldAndInBattle()
    {
        foreach (string look in ClassLooks)
        {
            Assert.True(Table.Has(look), $"{look} is no look");
            Assert.False(Table.Fallbacks.ContainsKey(look), $"{look} still falls back");
        }
        // Black 2's own names for its classes' looks find them by themselves, and the names that differ the look of the same class
        foreach (var (gfx, look) in new Dictionary<string, string>
        {
            ["backers_m"] = "backers_m", ["nursery_aide"] = "nursery_aide", ["janitor"] = "janitor",
            ["depot_agent"] = "depot_agent", ["backpacker_m"] = "backpacker_m", ["smasher"] = "smasher", ["hoopster"] = "hoopster",
            ["harlequin_233"] = "harlequin", ["team_plasma_grunt_f"] = "plasma_grunt_f", ["pkmn_ranger_m"] = "ranger_m",
            ["pkmn_breeder_f"] = "pokemon_breeder_f", ["veteran_f"] = "veteran_f", ["psychic_f"] = "psychic_f"
        })
            Assert.Equal(look, Table.CharacterFor(gfx));

        // In battle each class is its own look; Team Plasma's women in theirs; Hugh as himself, not Sinnoh's rival
        Assert.Equal("preschooler_f", Table.InBattle("Preschooler", "preschooler_f"));
        Assert.Equal("harlequin", Table.InBattle("Harlequin", null));
        Assert.Equal("depot_agent", Table.InBattle("Depot Agent", null));
        Assert.Equal("smasher", Table.InBattle("Smasher", null));
        Assert.Equal("plasma_grunt_f", Table.InBattle("Team Plasma Grunt", "plasma_grunt_f"));
        Assert.Equal("plasma_grunt", Table.InBattle("Team Plasma Grunt", null));
        Assert.Equal("hugh", Table.InBattle(TrainerDatabase.RivalClass, "hugh"));
        Assert.Equal("bianca", Table.InBattle("Pokémon Trainer Bianca", null));
        Assert.Equal("cheren", Table.InBattle("Leader Cheren", null));
        // and the last named people on looks of their own (our own designs for the roles), never the person's stand-in
        foreach (var (cls, look) in new[]
        {
            ("Leader Drayden", "opelucid_leader"), ("Leader Marlon", "humilau_leader"), ("Boss Trainer Benga", "tower_boss"),
            ("Pokémon Trainer Cilan", "cafe_brother_a"), ("Pokémon Trainer Cress", "cafe_brother_b"), ("Pokémon Trainer Chili", "cafe_brother_c")
        })
        {
            Assert.True(Table.Has(look), $"{look} is no look");
            Assert.Equal(look, Table.InBattle(cls, "swimmer_m"));
        }

        // Every class of Unova's records stands as a look, or as the person
        var plain = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var record in TrainerDatabase.OfRegion("Unova"))
        {
            if (record.Class is "No Data" || TrainerDatabase.IsRival(record.Class)) continue;
            if (!Table.Classes.TryGetValue(record.Class, out var looks)) { plain.Add(record.Class); continue; }
            foreach (string look in looks ?? Array.Empty<string>()) Assert.True(Table.Has(look), $"{record.Class} is drawn as '{look}', which is no look");
        }
        Assert.True(plain.Count == 0, "Unova's classes without a look: " + string.Join(", ", plain));
    }

    [Fact]
    public void TheSoundMapNamesAThemeForEveryClassTheOverlaysPlace()
    {
        var unova = Unova.Value;
        var missing = new SortedSet<string>(StringComparer.Ordinal);
        foreach (string key in unova.Index.Areas)
        {
            var overlay = unova.Overlay(key);
            var file = unova.Area(key);
            if (overlay?.People == null || file == null) continue;
            foreach (var (id, person) in overlay.People)
            {
                if (person.Trainer is not { } lines) continue;
                var obj = file.Objects.FirstOrDefault(o => o.Id == id);
                var record = TrainerDatabase.Get(obj?.Script, "Unova") ?? TrainerDatabase.Get(lines.Id, "Unova");
                if (record == null || TrainerDatabase.IsRival(record.Class)) continue;
                if (!SoundMap.EyeThemes.ContainsKey(record.Class)) missing.Add(record.Class);
            }
        }
        Assert.True(missing.Count == 0, "the sound map has no eye theme for " + string.Join(", ", missing));
    }
}
