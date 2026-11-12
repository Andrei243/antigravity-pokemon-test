using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 14 · B2 and B13 in the world (<c>docs/mechanics/rulings.md</c>, "Sky battles" and "The Battle Royal"): a sky
/// trainer of our own on the Vista Lighthouse's gallery after the Hall of Fame, who fights one Sky Battle a day with a
/// team that may fly, and the Battle Royal's corner in the Battle Frontier's annex, whose three fight the player each for
/// themselves. Neither carries a team on the map, so nothing is drawn for them as the world loads.
/// </summary>
public class SkyAndRoyalPlacesTests
{
    private const string Lighthouse = "VistaLighthouse", SkyTeam = "sky_trainer_aster";
    private const string DaysBattle = "FLAG_DAILY_VISTA_LIGHTHOUSE_SKY_BATTLE", Explained = "FLAG_VISTA_LIGHTHOUSE_SKY_BATTLE_EXPLAINED";
    private const string Annex = "BattleFrontierAnnex";
    private static readonly string[] RoyalTeams = { "royal_brisa", "royal_corin", "royal_dalla" };

    private static OpeningTests.Game OnTheGallery(string lead, BattleOutcome fight = BattleOutcome.Won, bool champion = true)
    {
        var game = new OpeningTests.Game(0, fight);
        game.Party.Add(new Pokemon(PokemonDatabase.Get(lead)!, 60));
        if (champion) game.Story.Set("FLAG_GAME_COMPLETED");
        game.Arrive(Lighthouse, 3, 5);
        game.Facing = Direction.Up;
        return game;
    }

    private static HeadlessScriptHost Talk(OpeningTests.Game game, string key, string place, params int[] answers)
    {
        foreach (int answer in answers) game.Answers.Enqueue(answer);
        var npc = game.Present(key, place) ?? throw new InvalidOperationException($"{key} isn't there");
        return game.Play(ScriptLibrary.Default.Find(npc.Script!, place)!, npc);
    }

    private static Trainer TrainerOf(string id)
    {
        var record = TrainerDatabase.Get(id)!;
        var t = new Trainer { Id = record.Id };
        TrainerDatabase.Fill(t, record);
        return t;
    }

    [Fact]
    public void TheSkyTrainerStandsOnTheGalleryOnceTheHallOfFameIsEntered()
    {
        var before = OnTheGallery("Staraptor", champion: false);
        Assert.Null(before.Present("sky_trainer", Lighthouse));
        var game = OnTheGallery("Staraptor");
        var her = game.Present("sky_trainer", Lighthouse);
        Assert.NotNull(her);
        Assert.Equal((3, 4, "SkyBattle", "FLAG_GAME_COMPLETED"), (her!.GridX, her.GridY, her.Script, her.ShownBy));
        // No team on the map: the script battles her by our record
        Assert.Null(her.TrainerData);
        Assert.False(game.Map.IsSolid(3, 5));
        // Clear of Volkner's walks round the gallery (VistaLighthouse.txt): east of him, down the east side and back
        Assert.True(her.GridX < 6);
    }

    [Fact]
    public void HerTeamFliesAndKnowsNoMoveTheSkyRefuses()
    {
        var record = TrainerDatabase.Get(SkyTeam)!;
        Assert.Contains(TrainerDatabase.Ours, t => t.Id == SkyTeam);
        Assert.Equal(("Aster", "Sky Trainer", "sky"), (record.Name, record.Class, record.Format));
        var trainer = TrainerOf(SkyTeam);
        Assert.True(trainer.InTheAir);
        Assert.Equal("Sky Trainer", Trainer.ClassName(trainer.TrainerClass));
        Assert.All(trainer.Party.Members, p =>
        {
            Assert.True(SkyBattles.CanFly(p), $"{p.Species.Name} can't fly");
            Assert.DoesNotContain(p.Moves, m => SkyBattles.Grounded(m.Data));
        });
    }

    [Fact]
    public void SheOffersOneSkyBattleADayToATeamThatFlies()
    {
        var game = OnTheGallery("Staraptor");
        var talk = Talk(game, "sky_trainer", Lighthouse, 0);
        Assert.Contains(talk.Transcript, l => l.Text.Contains("Sky Battle"));
        Assert.Single(talk.Asked);
        Assert.Contains($"battle {SkyTeam} Won", talk.Log);
        Assert.True(game.Story.Has(DaysBattle));
        Assert.True(game.Story.Has(Explained));

        // The same day: no question and no battle
        talk = Talk(game, "sky_trainer", Lighthouse);
        Assert.Empty(talk.Asked);
        Assert.DoesNotContain(talk.Log, l => l.StartsWith("battle "));

        // The next, the offer without the explanation
        game.Story.ClearDaily();
        talk = Talk(game, "sky_trainer", Lighthouse, 1);
        Assert.Single(talk.Asked);
        Assert.DoesNotContain(talk.Transcript, l => l.Text.Contains("Earthquake"));
        Assert.DoesNotContain(talk.Log, l => l.StartsWith("battle "));
        Assert.False(game.Story.Has(DaysBattle));
    }

    [Fact]
    public void ATeamWithNobodyThatFliesIsTurnedAwayBeforeTheOffer()
    {
        var game = OnTheGallery("Bidoof");
        // (OpeningTests.Game's team has the starter too: no flier there either)
        Assert.DoesNotContain(game.Party.Members, SkyBattles.CanFly);
        var talk = Talk(game, "sky_trainer", Lighthouse);
        Assert.Empty(talk.Asked);
        Assert.DoesNotContain(talk.Log, l => l.StartsWith("battle "));
        Assert.Contains("fly or float", talk.Transcript[^1].Text);
        Assert.False(game.Story.Has(DaysBattle));
    }

    [Fact]
    public void ALossIsNoWhiteOut()
    {
        var game = OnTheGallery("Staraptor", BattleOutcome.Lost);
        var talk = Talk(game, "sky_trainer", Lighthouse, 0);
        Assert.Contains($"battle {SkyTeam} Lost", talk.Log);
        Assert.Contains("hard place to learn", talk.Transcript[^1].Text);
        Assert.Equal(Lighthouse, game.Map.Name);
    }

    [Fact]
    public void HerSkyBattleIsFoughtToItsEndInTheAir()
    {
        var mine = new Party();
        foreach (string species in new[] { "Staraptor", "Bidoof", "Gyarados" }) mine.Add(new Pokemon(PokemonDatabase.Get(species)!, 60, new Random(7)));
        var fliers = SkyBattles.Fliers(mine);
        Assert.Equal(new[] { "Staraptor", "Gyarados" }, fliers.Members.Select(p => p.Species.Name));
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = fliers, Trainers = new List<Trainer> { TrainerOf(SkyTeam) }, Random = new BattleRandom(41), Rules = Ruleset.Modern,
            Conditions = new BattleConditions { InTheAir = true }, PlayerController = TrainerAi.Instance, PlayerName = "Lucas"
        });
        core.Start();
        Assert.Null(core.Request);
        Assert.True(core.Result is BattleResult.PlayerVictory or BattleResult.PlayerDefeat);
        Assert.Contains(core.Log.OfType<Said>(), l => l.Text == BattleCore.SkyLine);
    }

    [Fact]
    public void TheRoyalCornerStandsInTheAnnexWithItsThree()
    {
        var game = new OpeningTests.Game();
        game.Arrive(Annex, 2, 8);
        Assert.NotNull(game.Present("royal_receptionist", Annex));
        foreach (string key in RoyalTeams)
        {
            var person = game.Present(key, Annex);
            Assert.NotNull(person);
            Assert.Null(person!.TrainerData);
            var record = TrainerDatabase.Get(key)!;
            Assert.Equal(3, record.Party.Count);
            Assert.Equal(0, record.PrizeMoney);
            Assert.Null(record.Format);
        }
        // The corner keeps the way from the door to the desks open
        foreach (var (x, y) in new[] { (7, 9), (7, 8), (2, 8), (2, 7) }) Assert.False(game.Map.IsSolid(x, y), $"({x}, {y}) is shut");
    }

    [Fact]
    public void TheDeskTellsTheRulesAndStartsARoyalAgainstTheThree()
    {
        var game = new OpeningTests.Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Garchomp")!, 60));
        game.Arrive(Annex, 2, 8);
        game.Facing = Direction.Left;

        var rules = Talk(game, "royal_receptionist", Annex, 1);
        Assert.Contains(rules.Transcript, l => l.Text.Contains("knocked out"));
        Assert.DoesNotContain(rules.Log, l => l.StartsWith("royal "));

        var talk = Talk(game, "royal_receptionist", Annex, 0);
        Assert.Contains($"royal {string.Join(" ", RoyalTeams)} Won", talk.Log);
        Assert.Contains(talk.Transcript, l => l.Text.Contains("First place"));
        // First place earns two Battle Points at the desk, as Alola's Great Rank (rulings, "The Battle Royal")
        Assert.Equal(2, talk.Frontier.BattlePoints);
        // The battle itself gives nothing: nobody beaten, no money
        Assert.All(RoyalTeams, id => Assert.False(game.Story.HasDefeated(id)));

        // Offered again as often as the player likes
        talk = Talk(game, "royal_receptionist", Annex, 0);
        Assert.Contains(talk.Log, l => l.StartsWith("royal "));
    }

    [Fact]
    public void APlacingOtherThanFirstIsNeitherWonNorLost()
    {
        var game = new OpeningTests.Game(0, BattleOutcome.None);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Garchomp")!, 60));
        game.Arrive(Annex, 2, 8);
        var talk = Talk(game, "royal_receptionist", Annex, 0);
        Assert.Contains(talk.Log, l => l.StartsWith("royal ") && l.EndsWith(" None"));
        Assert.Contains("hard-fought placing", talk.Transcript[^1].Text);
        Assert.Equal(Annex, game.Map.Name);
    }

    [Fact]
    public void TheCornersRoyalIsFoughtToAPlacing()
    {
        var mine = new Party();
        foreach (string species in new[] { "Garchomp", "Staraptor", "Infernape" }) mine.Add(new Pokemon(PokemonDatabase.Get(species)!, 58, new Random(3)));
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = mine, Trainers = RoyalTeams.Select(TrainerOf).ToList(), Format = BattleFormat.Royal, Random = new BattleRandom(17),
            Rules = Ruleset.Modern, PlayerController = TrainerAi.Instance, NoRewards = true, NoBattleItems = true, PlayerName = "Lucas"
        });
        Assert.Equal(BattleFormat.Royal, core.Format);
        core.Start();
        Assert.Null(core.Request);
        Assert.True(core.Result is BattleResult.PlayerVictory or BattleResult.Placed, $"ended as {core.Result}");
        Assert.Single(core.Log.OfType<RoyalPlaced>());
    }
}
