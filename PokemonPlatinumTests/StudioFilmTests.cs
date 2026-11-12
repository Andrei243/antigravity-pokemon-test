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
        }
        Assert.True(tooLong.Count == 0, string.Join("\n", tooLong));
    }
}
