using System;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 18 · W4b-2, stages, with plan 03 · D15-A, grown as they are met (approved 2026-10-26: design C, named trainers
/// grow too, every evolution counting with its condition taken as met, wild one in four by personality, Masters follow
/// the player, Challenge by share in a crossed region): <see cref="OursGrowth"/>, <see cref="Trainer.Relevelled"/> and
/// <see cref="DifficultyRules"/>.
/// </summary>
public class OursGrowthTests
{
    private static Region RegionOf(string id) => RegionDatabase.Get(id)!;

    private static TrainerRecord Record(string region, string id) =>
        TrainerDatabase.OfRegion(RegionOf(region).Folder).First(t => t.Id == id);

    /// <summary>A trainer of a region's own file as a game begun in Sinnoh meets it there.</summary>
    private static Trainer Met(string region, string id) =>
        new Trainer { Id = id, Party = TrainerDatabase.PartyOf(Record(region, id)) }.Relevelled(RegionOf(region).Levels!.Map, grow: true);

    private static Pokemon Make(string species, int level, uint personality = 0x12340000) =>
        new(PokemonDatabase.Get(species)!, level, personality, new Random(1));

    /// <summary>The level of a species' evolution of ours into <paramref name="ours"/>.</summary>
    private static int LevelInto(string species, string ours) =>
        PokemonDatabase.Get(species)!.Evolutions!.First(e => e.TargetSpecies == ours).Level;

    [Fact]
    public void WhitneysMiltankIsMetAsItsSpeciesOfOursInAGameThatCrossedIntoJohto()
    {
        var own = Record(RegionDatabase.Johto, "johto_leader_whitney").Party.First(m => m.Species == "Miltank");
        int met = RegionOf(RegionDatabase.Johto).Levels!.Map(own.Level);
        Assert.True(met >= LevelInto("Miltank", "Pastorine"), $"Whitney's Miltank is met at {met}");
        var whitney = Met(RegionDatabase.Johto, "johto_leader_whitney");
        Assert.Contains(whitney.Party.Members, p => p.Species.Name == "Pastorine" && p.Level == met);
        Assert.DoesNotContain(whitney.Party.Members, p => p.Species.Name == "Miltank");
        // The trainer of the data keeps her own team at her game's level
        Assert.Contains(TrainerDatabase.PartyOf(Record(RegionDatabase.Johto, "johto_leader_whitney")).Members, p => p.Species.Name == "Miltank" && p.Level == own.Level);
    }

    [Fact]
    public void ARattataOfLevelFourIsMetAsRaticateAndThenAsOurs()
    {
        var rattata = Make("Rattata", 105);
        Assert.Equal("Raticate", OursGrowth.GamesStageAt(rattata)!.TargetSpecies);
        OursGrowth.GrowTrainers(rattata, Ruleset.Platinum.LevelCap);
        int ratchisel = LevelInto("Raticate", "Ratchisel");
        Assert.Equal(105 >= ratchisel ? "Ratchisel" : "Raticate", rattata.Species.Name);
        // Below Raticate's own level it is the Rattata it was
        var young = Make("Rattata", 19);
        Assert.False(OursGrowth.GrowTrainers(young));
        Assert.Equal("Rattata", young.Species.Name);
    }

    [Fact]
    public void ALineOfOneWayAndNoLevelGrowsAtThirtyOrSixteenAndOneOfSeveralWaysStays()
    {
        // Pikachu's Thunder Stone (Alolan Raichu's way asks for a region and is passed over)
        Assert.Null(OursGrowth.GamesStageAt(Make("Pikachu", 29)));
        Assert.Equal("Raichu", OursGrowth.GamesStageAt(Make("Pikachu", 30))!.TargetSpecies);
        // Pichu by friendship, at sixteen
        Assert.Equal("Pikachu", OursGrowth.GamesStageAt(Make("Pichu", 16))!.TargetSpecies);
        // Eevee and Gloom have several ways and no level: the games' stage stays
        Assert.Null(OursGrowth.GamesStageAt(Make("Eevee", 200)));
        Assert.Null(OursGrowth.GamesStageAt(Make("Gloom", 200)));
    }

    [Fact]
    public void AWayDecidedByThePokemonItselfIsTakenOnlyWhereItHolds()
    {
        // Wurmple by its personality: the high half of the personality's upper word makes a Silcoon, the low a Cascoon
        var silcoon = Make("Wurmple", 50, 0x00010000);
        var cascoon = Make("Wurmple", 50, 0x00070000);
        Assert.Equal("Silcoon", OursGrowth.GamesStageAt(silcoon)!.TargetSpecies);
        Assert.Equal("Cascoon", OursGrowth.GamesStageAt(cascoon)!.TargetSpecies);
    }

    [Fact]
    public void NoPokemonIsEverGrownIntoALegendarySetsEnd()
    {
        foreach (var species in PokemonDatabase.Everyone().Where(s => s.Evolutions?.Any(e => PokemonDatabase.Get(e.TargetSpecies) is { IsOurs: true }) == true))
        {
            var p = Make(species.Name, Ruleset.HighestLevel);
            OursGrowth.GrowTrainers(p, Ruleset.Platinum.LevelCap);
            Assert.False(p.Species.IsOurs && (p.Species.Legendary || p.Species.Mythical), $"{species.Name} grew into {p.Species.Name}");
        }
    }

    [Fact]
    public void MovesItsLevelTaughtAreTheNewSpeciesAndMovesChosenAreKept()
    {
        var byLevel = Make("Rattata", 4);
        byLevel.Relevel(150, Ruleset.Platinum.LevelCap);
        OursGrowth.GrowTrainers(byLevel, Ruleset.Platinum.LevelCap);
        var fresh = Make(byLevel.Species.Name, Ruleset.Platinum.LevelCap);
        Assert.Equal(fresh.Moves.Select(m => m.Name), byLevel.Moves.Select(m => m.Name));

        var chosen = Make("Rattata", 4);
        chosen.Moves.Clear();
        chosen.Moves.Add(MoveDatabase.Create("Tackle"));
        chosen.Moves.Add(MoveDatabase.Create("Bite"));
        chosen.Relevel(150, Ruleset.Platinum.LevelCap);
        OursGrowth.GrowTrainers(chosen, Ruleset.Platinum.LevelCap);
        Assert.Equal(new[] { "Tackle", "Bite" }, chosen.Moves.Select(m => m.Name));
    }

    [Fact]
    public void AWildPokemonGrowsIntoOursOneTimeInFourByItsPersonalityAndNeverTakesTheGamesStage()
    {
        int at = LevelInto("Raticate", "Ratchisel");
        var chosen = Make("Raticate", at, 0x10000000u);
        var passed = Make("Raticate", at, 0x10000001u);
        Assert.True(OursGrowth.GrowWild(chosen));
        Assert.Equal("Ratchisel", chosen.Species.Name);
        Assert.False(OursGrowth.GrowWild(passed));
        Assert.Equal("Raticate", passed.Species.Name);
        // A Rattata of the same level is still a Rattata: a wild Pokémon keeps the games' stage its table gives it
        var rattata = Make("Rattata", at, 0x10000000u);
        Assert.False(OursGrowth.GrowWild(rattata));
        Assert.Equal("Rattata", rattata.Species.Name);
        // and nothing was drawn: the personality is the one it was met with
        Assert.Equal(0x10000000u, chosen.Personality);
    }

    [Fact]
    public void WithoutGrowingATrainerKeepsTheSpeciesTheBattleSubwayAndTheFrontierMeet()
    {
        var record = Record(RegionDatabase.Johto, "johto_leader_whitney");
        var kept = new Trainer { Party = TrainerDatabase.PartyOf(record) }.Relevelled(RegionOf(RegionDatabase.Johto).Levels!.Map);
        Assert.Equal(record.Party.Select(m => m.Species), kept.Party.Members.Select(p => p.Species.Name));
    }

    [Fact]
    public void AMasterTrainerFollowsThePlayerAboveItsOwnLevel()
    {
        var master = new Trainer { Id = "master_bidoof", Party = TrainerDatabase.PartyOf(TrainerDatabase.Get(TrainerDatabase.MasterId("Bidoof"))!) };
        int own = master.Party.Members.Max(p => p.Level);
        Assert.Same(master, DifficultyRules.FollowThePlayer(master, own - 10));
        var followed = DifficultyRules.FollowThePlayer(master, 90);
        Assert.NotSame(master, followed);
        Assert.Equal(90, followed.Party.Members.Max(p => p.Level));
        Assert.Equal(own, master.Party.Members.Max(p => p.Level));
        // Its species stay: a Master is never grown
        Assert.Equal(master.Party.Members.Select(p => p.Species.Name), followed.Party.Members.Select(p => p.Species.Name));
    }

    [Fact]
    public void ChallengeRaisesByShareInACrossedRegionAndByLevelsInSinnoh()
    {
        Assert.Equal(4, DifficultyRules.LevelsOf(DifficultyRules.Tier.Chief, 62, crossed: false));
        Assert.Equal(12, DifficultyRules.LevelsOf(DifficultyRules.Tier.Chief, 185, crossed: true));
        Assert.Equal(4, DifficultyRules.LevelsOf(DifficultyRules.Tier.Chief, 62, crossed: true));
        Assert.Equal(9, DifficultyRules.LevelsOf(DifficultyRules.Tier.Boss, 185, crossed: true));
        Assert.Equal(4, DifficultyRules.LevelsOf(DifficultyRules.Tier.Everyone, 185, crossed: true));
        Assert.Equal(1, DifficultyRules.LevelsOf(DifficultyRules.Tier.Everyone, 30, crossed: true));

        // A Champion's ace of 90 (under the rules a test is fought by, whose cap is 100): +6 crossed, +4 at home
        var champion = new Trainer { Id = "test_champion", TrainerClass = "Champion" };
        champion.Party.Add(Make("Garchomp", 90));
        var challenge = RunRules.Normal with { Difficulty = Difficulty.Challenge };
        Assert.Equal(96, DifficultyRules.Prepare(champion, challenge, crossed: true).Party.Members.Max(p => p.Level));
        Assert.Equal(94, DifficultyRules.Prepare(champion, challenge).Party.Members.Max(p => p.Level));
        Assert.Equal(90, champion.Party.Members.Max(p => p.Level));
    }
}
