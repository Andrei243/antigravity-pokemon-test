using System.Collections.Generic;
using System.Linq;
using DataImporter;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
using Xunit;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 03 · D14, batch 7 (Alola: docs/plans/D14-proposals-alola.md, all 28 designs approved by the user with the
/// defaults of questions 1 to 5 and 7). What is Alola's own beside the rules <see cref="SpeciesPastHundredTests"/> holds
/// every batch to: the band of 209 to 221 within Alola's climb, the island guardians' ring at 225 to 660, the two pairs
/// of version exclusives, Salazzle's court, Lycanroc's three shapes at dusk, and the conditions that ask for more than a
/// level. Question 6 (the Ultra Beasts as a set) is D14-proposals-alola-supplement.md's, held by SpeciesPastHundredAlolaSupplementTests.
/// </summary>
public class SpeciesPastHundredAlolaTests
{
    private static readonly System.Lazy<List<SpeciesDesign>> designs = new(() =>
        SpeciesPastHundred.Load(SpeciesPastHundred.PathIn(Repository.Root))
            .Where(d => PokemonDatabase.Get(d.From)!.Generation == 7 && d.Legendary == null).ToList()); // the supplement's Ultra Beasts are SpeciesPastHundredAlolaSupplementTests'

    private static List<SpeciesDesign> Alola => designs.Value;

    private static PokemonSpecies Species(string name) => PokemonDatabase.Get(name)!;

    private static int Total(PokemonSpecies s)
    {
        var x = SpeciesPastHundred.StatsOf(s);
        return x.HP + x.Attack + x.Defense + x.SpAttack + x.SpDefense + x.Speed;
    }

    private static string? LevelUp(Pokemon p, EvolutionContext context) => Evolution.Find(p, EvolutionTrigger.LevelUp, context)?.TargetSpecies;

    private static Party PartyOf(params Pokemon[] members)
    {
        var party = new Party();
        foreach (var m in members) party.Add(m);
        return party;
    }

    [Fact]
    public void AlolasTwentyEightEvolveWithinAlolasClimb()
    {
        // Question 1: Alola is met at about 205 to 228, so its own evolve at 209 to 221 and its guardians at 225
        Assert.Equal(28, Alola.Count);
        Assert.Equal(Enumerable.Range(0, 28).Select(i => Alola[i].Name), Alola.OrderBy(d => Species(d.From).DexNumber).Select(d => d.Name));
        Assert.All(Alola.Where(d => !Species(d.From).Legendary), d => Assert.InRange(d.Evolution.Level, 209, 221));
        Assert.Equal(new[] { "Kokonui", "Lelenui", "Bulunui", "Fininui" }, Alola.Where(d => Species(d.From).Legendary).Select(d => d.Name));
        Assert.All(Alola.Where(d => Species(d.From).Legendary), d => Assert.Equal(225, d.Evolution.Level));
        Assert.All(Alola, d => Assert.InRange(Species(d.Name).DexNumber, 2735, 2788));

        // Nothing that isn't legendary passes 600; Shogunpod rises only to it
        Assert.Equal(600, Total(Species("Shogunpod")));
        Assert.All(Alola.Where(d => !Species(d.From).Legendary), d => Assert.True(Total(Species(d.Name)) <= 600, d.Name));
    }

    [Fact]
    public void ALevelAloneIsNotEnoughAtTheirOwnLevel()
    {
        // At its own level and above, with nothing else, no design of Alola's but a plain level evolves
        foreach (var d in Alola)
        {
            var p = Mon(d.From, d.Evolution.Level + 5);
            p.Friendship = 0;
            Assert.True(LevelUp(p, new EvolutionContext { IsNight = d.Evolution.Method == EvolutionMethod.LevelDay }) != d.Name, $"{d.From} becomes {d.Name} with nothing but its level");
        }
    }

    [Fact]
    public void TheIslandGuardiansCloseARing()
    {
        // Question 2: each at 225 with the next in the party (Koko with Lele, Lele with Bulu, Bulu with Fini, Fini with
        // Koko); the one before it, or another of its own kind, is not enough. To 660 (+90), banned from the Frontier.
        foreach (var (who, next, before, into) in new[]
        {
            ("Tapu Koko", "Tapu Lele", "Tapu Fini", "Kokonui"), ("Tapu Lele", "Tapu Bulu", "Tapu Koko", "Lelenui"),
            ("Tapu Bulu", "Tapu Fini", "Tapu Lele", "Bulunui"), ("Tapu Fini", "Tapu Koko", "Tapu Bulu", "Fininui")
        })
        {
            var p = Mon(who, 225);
            Assert.Null(LevelUp(p, new EvolutionContext()));
            Assert.Null(LevelUp(p, new EvolutionContext { Party = PartyOf(p, Mon(before, 50), Mon(who, 50)) }));
            Assert.Equal(into, LevelUp(p, new EvolutionContext { Party = PartyOf(p, Mon(next, 50)) }));
            Assert.Null(LevelUp(Mon(who, 224), new EvolutionContext { Party = PartyOf(Mon(next, 50)) }));
            Assert.Equal(660, Total(Species(into)));
            Assert.Equal(90, Total(Species(into)) - Total(Species(who)));
            Assert.True(Species(into).Legendary);
            Assert.Equal(3, Species(into).CatchRate);
            Assert.False(FrontierRules.MayEnter(Mon(into, 225)));
        }
    }

    [Fact]
    public void TheVersionPairsAndSalazzlesCourtGoFurtherOnlyWithTheOther()
    {
        // Question 5: Oranguru and Passimian, Turtonator and Drampa, each with the other; question 7: Salazzle with a
        // Salandit. One of its own kind beside it is not its partner.
        foreach (var (who, partner, into, level) in new[]
        {
            ("Oranguru", "Passimian", "Arborsage", 215), ("Passimian", "Oranguru", "Scrummian", 215),
            ("Turtonator", "Drampa", "Caldertoise", 220), ("Drampa", "Turtonator", "Elderdrake", 220),
            ("Salazzle", "Salandit", "Smolderess", 215)
        })
        {
            var p = Mon(who, level);
            Assert.Null(LevelUp(p, new EvolutionContext()));
            Assert.Null(LevelUp(p, new EvolutionContext { Party = PartyOf(p, Mon(who, level)) }));
            Assert.Equal(into, LevelUp(p, new EvolutionContext { Party = PartyOf(p, Mon(partner, 5)) }));
            Assert.Null(LevelUp(Mon(who, level - 1), new EvolutionContext { Party = PartyOf(Mon(partner, 5)) }));
            Assert.True(FrontierRules.MayEnter(Mon(into, level)));
        }

        // Smolderess is female only, as Salazzle
        Assert.Equal(Species("Salazzle").GenderRatio, Species("Smolderess").GenderRatio);
    }

    [Fact]
    public void AnyOfLycanrocsThreeShapesBecomesLupinacleAtDusk()
    {
        // Question 4: one species of ours from all three, at the hour where they meet (Rockruff's own LevelDusk)
        foreach (string? form in new[] { null, "Lycanroc-Midnight", "Lycanroc-Dusk" })
        {
            var p = Mon("Lycanroc", 216);
            if (form != null) p.ChangeForm(form);
            Assert.Null(LevelUp(p, new EvolutionContext { IsNight = false }));
            Assert.Null(LevelUp(p, new EvolutionContext { IsNight = true }));
            var found = Evolution.Find(p, EvolutionTrigger.LevelUp, new EvolutionContext { IsDusk = true });
            Assert.Equal("Lupinacle", found?.TargetSpecies);
            Evolution.Evolve(p, found!, new EvolutionContext { IsDusk = true });
            Assert.Equal(("Lupinacle", (string?)null), (p.Species.Name, p.Form));
        }
        var early = Mon("Lycanroc", 215);
        Assert.Null(LevelUp(early, new EvolutionContext { IsDusk = true }));
    }

    [Fact]
    public void MasqueradollSewsItsCostumeAtNightAndCursesWhoStrikesIt()
    {
        // Question 3: Disguise works for Mimikyu by name alone, so Masqueradoll takes Cursed Body in its one slot
        Assert.Equal(new[] { "Cursed Body" }, Species("Masqueradoll").Abilities);
        Assert.Null(Species("Masqueradoll").HiddenAbility);

        var mimikyu = Mon("Mimikyu", 217);
        mimikyu.HeldItem = ItemDatabase.Get("Reaper Cloth");
        Assert.Null(LevelUp(mimikyu, new EvolutionContext { IsNight = false }));
        var found = Evolution.Find(mimikyu, EvolutionTrigger.LevelUp, new EvolutionContext { IsNight = true });
        Assert.Equal("Masqueradoll", found?.TargetSpecies);
        Evolution.Evolve(mimikyu, found!, new EvolutionContext { IsNight = true });
        Assert.Null(mimikyu.HeldItem);
    }

    [Fact]
    public void TheOtherConditionsOfAlolaAskForWhatTheySay()
    {
        // Crabominable takes the Ice Stone from the bag at 218 and not before; held, it does nothing
        var context = new EvolutionContext { Item = ItemDatabase.Get("Ice Stone") };
        Assert.Null(Evolution.Find(Mon("Crabominable", 217), EvolutionTrigger.UseItem, context));
        Assert.Equal("Glacipincer", Evolution.Find(Mon("Crabominable", 218), EvolutionTrigger.UseItem, context)?.TargetSpecies);
        var holding = Mon("Crabominable", 218);
        holding.HeldItem = ItemDatabase.Get("Ice Stone");
        Assert.Null(LevelUp(holding, new EvolutionContext()));

        // Mudsdale in the rain
        Assert.Null(LevelUp(Mon("Mudsdale", 214), new EvolutionContext()));
        Assert.Equal("Claysteed", LevelUp(Mon("Mudsdale", 214), new EvolutionContext { IsRaining = true }));

        // Bewear fond of its trainer
        var bewear = Mon("Bewear", 216);
        bewear.Friendship = FriendshipRules.EvolveAt - 1;
        Assert.Null(LevelUp(bewear, new EvolutionContext()));
        bewear.Friendship = FriendshipRules.EvolveAt;
        Assert.Equal("Huggrizzle", LevelUp(bewear, new EvolutionContext()));

        // Gumshoos and Lurantis by day, Shiinotic and Komala at night
        foreach (var (who, into, level, night) in new[]
        {
            ("Gumshoos", "Inspectoose", 209, false), ("Lurantis", "Orchimantis", 213, false),
            ("Shiinotic", "Lumishroom", 211, true), ("Komala", "Somnikoala", 216, true)
        })
        {
            Assert.Null(LevelUp(Mon(who, level), new EvolutionContext { IsNight = !night }));
            Assert.Equal(into, LevelUp(Mon(who, level), new EvolutionContext { IsNight = night }));
        }

        // Toxapex and Palossand holding their items, which are used up
        foreach (var (who, item, into, level) in new[] { ("Toxapex", "Black Sludge", "Venomreef", 220), ("Palossand", "Soft Sand", "Sandcitadel", 213) })
        {
            var p = Mon(who, level);
            Assert.Null(LevelUp(p, new EvolutionContext()));
            p.HeldItem = ItemDatabase.Get(item);
            var found = Evolution.Find(p, EvolutionTrigger.LevelUp, new EvolutionContext());
            Assert.Equal(into, found?.TargetSpecies);
            Evolution.Evolve(p, found!, new EvolutionContext());
            Assert.Null(p.HeldItem);
        }

        // The eight that must know a move first
        foreach (var (who, move, into, level) in new[]
        {
            ("Ribombee", "Pollen Puff", "Pollenoble", 210), ("Araquanid", "Liquidation", "Diveweaver", 212),
            ("Comfey", "Floral Healing", "Wreathfey", 214), ("Golisopod", "First Impression", "Shogunpod", 218),
            ("Pyukumuku", "Counter", "Cucumbrawl", 210), ("Togedemaru", "Zing Zap", "Rollocupine", 212),
            ("Bruxish", "Psychic Fangs", "Chromagnash", 219), ("Dhelmise", "Anchor Shot", "Galleonghast", 221)
        })
        {
            Assert.Null(LevelUp(Mon(who, level, "Tackle"), new EvolutionContext()));
            Assert.Equal(into, LevelUp(Mon(who, level, move), new EvolutionContext()));
            Assert.Null(LevelUp(Mon(who, level - 1, move), new EvolutionContext()));
        }
    }

    [Fact]
    public void AnAlolaSpeciesOfOursIsMetInTheGameAsAnyOther()
    {
        // Past the 1,025, in no regional Pokédex, its moves past 100 by R31's rule, and a battle with one of them
        Assert.All(Alola, d => Assert.DoesNotContain(PokemonDatabase.GetAll(), s => s.Name == d.Name));
        Assert.All(Alola, d => Assert.Contains(PokemonDatabase.Everyone(), s => s.Name == d.Name));
        var lupinacle = Mon("Lupinacle", 220, "Rock Slide");
        var battle = Battle(lupinacle, Mon("Gumshoos", 50), Steady(), Ruleset.Modern);
        for (int turn = 0; turn < 10 && !battle.IsBattleOver; turn++) Turn(battle);
        Assert.True(battle.IsBattleOver);
        Assert.Contains(Species("Lupinacle").Learnset, l => l.Level > 100);
    }
}
