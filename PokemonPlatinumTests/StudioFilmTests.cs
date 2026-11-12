using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Studio;
using PokemonPlatinumEngine.UI;
using PokemonPlatinumEngine.UI.Kit;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>The film studio's films (plan 42 · PS4): their battles, beats, endings and records.</summary>
[Collection("Shared clocks and dice")]
public class StudioFilmTests
{
    [Fact]
    public void TheFirstTwoFilmsAreTheGhostStoryAndTheHeist()
    {
        Assert.Equal(new[] { "emptySeat", "gildedHour" }, StudioFilms.All.Take(2).Select(f => f.Id));
        Assert.Equal("ghost", StudioFilms.All[0].Genre);
        Assert.Equal("heist", StudioFilms.All[1].Genre);
    }

    [Fact]
    public void EveryFilmCanBeShotAsWritten()
    {
        foreach (var film in StudioFilms.All)
        {
            Assert.Empty(StudioFilms.Problems(film));
            Assert.True(Text.Has(film.TitleKey), film.TitleKey);
            Assert.True(Text.Has(film.GenreKey), film.GenreKey);
        }
    }

    [Fact]
    public void EveryEndingOfEveryFilmIsReachedByItsTake()
    {
        foreach (var film in StudioFilms.All)
            foreach (var script in film.Takes)
            {
                var take = StudioFilms.Shoot(film, script);
                Assert.True(script.Ending == take.Ending, $"{film.Id}: the take written for {script.Ending} came to {take.Ending} (met {string.Join(",", take.Met)})");
                Assert.Equal(film.Endings.Single(e => e.Id == script.Ending).Stars, take.Stars);
                Assert.Equal(StudioFilms.Shoot(film, script).Met, take.Met);
            }
    }

    [Fact]
    public void ABeatComesOnceAtTheFirstMenuItHolds()
    {
        var film = StudioFilms.Get("emptySeat")!;
        var core = new BattleCore(Puzzles.Setup(film.Battle));
        core.Start();
        var director = new StudioDirector(film);
        var first = director.Next(core);
        Assert.Equal("seatThirteen", first!.Id);
        Assert.Null(director.Next(core));
        director.Choose(first, "encore");
        Assert.Equal(new[] { "encore" }, director.Chosen);
        // The foe's first Pokémon down: its beat comes at the next menu, and not again
        core.Submit(Puzzles.Answer(core, new PuzzleAnswer { Move = "Dark Pulse" })!);
        Assert.Equal("firstDown", director.Next(core)?.Id);
        Assert.Null(director.Next(core));
        Assert.Equal(new[] { "seatThirteen", "firstDown" }, director.Met);
    }

    [Fact]
    public void AnIdTheBeatDoesNotOfferIsItsFirstLine()
    {
        var film = StudioFilms.Get("gildedHour")!;
        var beat = film.Beats.First(b => b.Choices.Count > 0);
        Assert.Equal(beat.Choices[0].Id, new StudioDirector(film).Choose(beat, "nothing").Id);
    }

    [Fact]
    public void AFilmsBattleIsTheSameWithItsBeatsAsWithout()
    {
        foreach (var film in StudioFilms.All)
        {
            var script = film.Takes[0];
            var plain = Puzzles.Play(film.Battle, script.Answers);
            var core = new BattleCore(Puzzles.Setup(film.Battle));
            core.Start();
            var director = new StudioDirector(film);
            foreach (var answer in script.Answers)
            {
                while (director.Next(core) is { } beat) if (beat.Choices.Count > 0) director.Choose(beat, null);
                if (core.Request == null || core.Result != BattleResult.None) break;
                if (Puzzles.Answer(core, answer) is not { } choice || core.WhyNot(choice) != null) break;
                core.Submit(choice);
            }
            Assert.Equal(Golden.BattleText.Written(plain), Golden.BattleText.Written(core));
        }
    }

    [Fact]
    public void TheRecordsKeepEachFilmsBestAndRoundTripASave()
    {
        var records = new StudioRecords();
        Assert.Null(records.ToSave());
        Assert.True(records.Keep(new StudioTake { Film = "emptySeat", Ending = "lost", Stars = 1 }));
        Assert.True(records.Keep(new StudioTake { Film = "emptySeat", Ending = "takesABow", Stars = 5, Chosen = { "encore" } }));
        Assert.False(records.Keep(new StudioTake { Film = "emptySeat", Ending = "movedOn", Stars = 4 }));
        Assert.Equal(5, records.BestOf("emptySeat"));
        Assert.Equal(3, records.ShotOf("emptySeat"));
        Assert.Equal(5, records.Rank);
        var back = new StudioRecords();
        back.Load(System.Text.Json.JsonSerializer.Deserialize<StudioSave>(System.Text.Json.JsonSerializer.Serialize(records.ToSave(), GameDataFiles.Json), GameDataFiles.Json));
        Assert.Equal(5, back.BestOf("emptySeat"));
        Assert.Equal("takesABow", back.BestTakeOf("emptySeat")!.Ending);
        Assert.Equal(new[] { "encore" }, back.BestTakeOf("emptySeat")!.Chosen);
        // An older save reads as no films shot
        var none = new StudioRecords();
        none.Load(null);
        Assert.Equal(0, none.Rank);
        Assert.All(StudioFilms.All.Where(f => f.Rank == 0), f => Assert.True(none.Opens(f)));
    }

    /// <summary>A film's take made as the game makes it (<c>GameEngine.StartStudioFilm</c>), with no screen.</summary>
    private static BattleEngine Take(StudioFilm film) => new(new BattleSetup
    {
        PlayerParty = Puzzles.PlayerParty(film.Battle),
        Inventory = Puzzles.Bag(film.Battle),
        Pokedex = new Pokedex(),
        Trainers = new List<Trainer> { Puzzles.FoeTrainer(film.Battle)! },
        Random = Puzzles.Random(film.Battle),
        Rules = film.Battle.RulesSet,
        Conditions = Puzzles.Conditions(film.Battle),
        EnemyController = new PuzzleFoe(film.Battle.FoeMoves),
        NoBattleItems = true,
        NoRewards = true,
        Frontier = true,
        StudioFilm = new StudioDirector(film),
        StudioTakeNumber = 2,
        PlayerName = "Player"
    });

    /// <summary>Reads the battle on to its next menu, noting each line with its speaker and choosing lines by <paramref name="pick"/>.</summary>
    private static void ReadOn(BattleEngine battle, List<(string Line, string? Speaker)> seen, int pick)
    {
        for (int i = 0; i < 400; i++)
        {
            if (battle.IsLearningMove && battle.LearnStep == LearnStep.Line)
            {
                battle.ChooseLearn(pick);
                battle.Update(1f / 60f);
                continue;
            }
            if (battle.HUD.MenuState is not (BattleMenuState.Message or BattleMenuState.LearnMove)) return;
            if (battle.IsWaitingForConfirm) seen.Add((battle.CurrentMessage, battle.CurrentSpeaker));
            battle.ConfirmMessage();
            battle.Update(1f / 60f);
        }
    }

    [Fact]
    public void ATakeOnTheBattlesFaceSaysItsBeatsAndEndsOnItsStars()
    {
        var film = StudioFilms.Get("emptySeat")!;
        var script = film.Takes.Single(t => t.Ending == "takesABow");
        var battle = Take(film);
        var seen = new List<(string Line, string? Speaker)>();
        ReadOn(battle, seen, pick: 1);
        foreach (var answer in script.Answers)
        {
            if (battle.IsBattleOver || battle.HUD.MenuState != BattleMenuState.Main) break;
            battle.SelectMove(battle.Core.PlayerParty.Members[0].Moves.FindIndex(m => m.Name == answer.Move));
            ReadOn(battle, seen, pick: 1);
        }
        Assert.True(battle.IsBattleOver);
        battle.WrapUpStudio();
        ReadOn(battle, seen, pick: 1);
        Assert.True(battle.StudioDone);
        Assert.Equal("takesABow", battle.StudioTake!.Ending);
        Assert.Equal(5, battle.StudioTake.Stars);
        Assert.Equal(new[] { "encore" }, battle.StudioTake.Chosen);

        Assert.Contains(seen, s => s.Line.Contains("The Empty Seat") && s.Line.Contains("take 2"));
        Assert.Contains(seen, s => s.Speaker == "Captain" && s.Line.Contains("seat thirteen"));
        Assert.Contains(seen, s => s.Speaker == PlayerIdentity.Name && s.Line == "Stay for an encore, then!");
        Assert.Contains(seen, s => s.Speaker == "Usher" && s.Line.Contains("remain seated"));
        Assert.Contains(seen, s => s.Line.Contains("taking a bow"));
        Assert.Equal(Text.Of("studio.stars", ("stars", 5)), seen[^1].Line);
        // The title card comes first of the film's own lines, before any beat
        Assert.True(seen.FindIndex(s => s.Line.Contains("take 2")) < seen.FindIndex(s => s.Line.Contains("seat thirteen")));
        // The rules fought the battle the puzzle's way, beats or none
        Assert.Equal(Golden.BattleText.Written(Puzzles.Play(film.Battle, script.Answers)), Golden.BattleText.Written(battle.Core));
    }

    [Fact]
    public void ReadingOnPicksTheScriptsLine()
    {
        var film = StudioFilms.Get("gildedHour")!;
        var battle = Take(film);
        for (int i = 0; i < 400 && !(battle.IsLearningMove && battle.LearnStep == LearnStep.Line); i++)
        {
            battle.ConfirmMessage();
            battle.Update(1f / 60f);
        }
        Assert.Equal(new[] { "In and out. Nobody hears a thing.", "Let's give them a show first!" }, battle.LearnAnswers);
        battle.ConfirmMessage();
        Assert.Equal(new[] { "quietly" }, battle.Studio!.Chosen);
    }

    [Fact]
    public void EveryLineOfEveryFilmFitsTheBattlesTwoRows()
    {
        var tooLong = new List<string>();
        foreach (var film in StudioFilms.All)
        {
            var lines = film.Beats.SelectMany(b => b.Lines.Concat(b.Choices.Select(c => c.Text))).Concat(film.Endings.SelectMany(e => e.Lines));
            foreach (string line in lines)
            {
                var rows = TextMetrics.Wrap(PlayerIdentity.Fill(line), ModernUi.MessageBoxWidth - ModernUi.MessageMargin, ModernUi.MessageSize, UiWeight.ExtraBold);
                if (rows.Count > 2) tooLong.Add($"{film.Id}: {line}");
            }
            // The title card at a take of two digits, and the stars, in every language with a table
            foreach (string language in new[] { "en", "ro" })
            {
                Text.Use(language);
                try
                {
                    foreach (string line in new[]
                    {
                        Text.Of("studio.titleCard", ("title", Text.Of(film.TitleKey)), ("genre", Text.Of(film.GenreKey)), ("take", 99)),
                        Text.Of("studio.stars", ("stars", 5))
                    })
                        if (TextMetrics.Wrap(line, ModernUi.MessageBoxWidth - ModernUi.MessageMargin, ModernUi.MessageSize, UiWeight.ExtraBold).Count > 2)
                            tooLong.Add($"{language}: {line}");
                }
                finally { Text.Use("en"); }
            }
        }
        Assert.True(tooLong.Count == 0, string.Join("\n", tooLong));
    }
}
