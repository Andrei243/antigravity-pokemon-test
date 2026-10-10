using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.PoketchApps;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The Pokétch's apps about Pokémon (plan 06 · R14b): the Friendship Checker, the Day-Care Checker, the Pokémon
/// History, the Move Tester and the Matchup Checker, each held to the original's rules
/// (<c>src/applications/poketch/&lt;app&gt;/main.c</c>), touched with <see cref="PoketchAppState.Press"/> and run with
/// <see cref="PoketchAppState.Update"/>.
/// </summary>
public class PoketchPokemonTests
{
    private const float Frame = 1f / 60f;

    private static Pokemon Mon(string species, Gender gender = Gender.Male, int friendship = 70, int level = 10, TrainerMark? trainer = null)
    {
        var p = new Pokemon(PokemonDatabase.Get(species)!, level, gender, Nature.Hardy, false)
        {
            Friendship = friendship,
            OriginalTrainer = trainer
        };
        return p;
    }

    private sealed class Rig
    {
        public readonly Poketch Poketch = new() { Enabled = true };
        public readonly List<string> Sounds = new();
        public readonly List<string> Cries = new();
        public readonly PoketchContext Context;

        public Rig(params Pokemon[] team)
        {
            var party = new Party();
            foreach (var p in team) party.Add(p);
            Context = new PoketchContext
            {
                Poketch = Poketch,
                Party = party,
                Rng = new Random(1),
                Sound = Sounds.Add,
                Cry = p => Cries.Add(p.ModelName)
            };
        }

        public T Open<T>(PoketchApp app) where T : PoketchAppState
        {
            Poketch.Register(app);
            return (T)Poketch.State!;
        }

        public void Run(PoketchAppState app, float seconds) => Frames(app, (int)MathF.Round(seconds * 60));

        public void Frames(PoketchAppState app, int frames)
        {
            for (int i = 0; i < frames; i++) app.Update(Frame, Context);
        }
    }

    // ---- Friendship Checker ----

    [Theory]
    // GetFriendshipLevel's tiers: 1, 35, 70, 150, 200, 255
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(34, 1)]
    [InlineData(35, 2)]
    [InlineData(69, 2)]
    [InlineData(70, 3)]
    [InlineData(149, 3)]
    [InlineData(150, 4)]
    [InlineData(199, 4)]
    [InlineData(200, 5)]
    [InlineData(254, 5)]
    [InlineData(255, 6)]
    public void FriendshipFallsIntoTheOriginalsTiers(int friendship, int level) =>
        Assert.Equal(level, FriendshipCheckerApp.Level(friendship));

    [Theory]
    // Levels 0 to 2 dislike with 3 to 1, level 3 neither, levels 4 to 6 like with 1 to 3 (Init)
    [InlineData(0, FriendshipCheckerApp.Feeling.Dislikes, 3)]
    [InlineData(20, FriendshipCheckerApp.Feeling.Dislikes, 2)]
    [InlineData(50, FriendshipCheckerApp.Feeling.Dislikes, 1)]
    [InlineData(100, FriendshipCheckerApp.Feeling.Neutral, 0)]
    [InlineData(160, FriendshipCheckerApp.Feeling.Likes, 1)]
    [InlineData(220, FriendshipCheckerApp.Feeling.Likes, 2)]
    [InlineData(255, FriendshipCheckerApp.Feeling.Likes, 3)]
    public void AFriendshipShowsAsAFeelingAndHowStrong(int friendship, FriendshipCheckerApp.Feeling feeling, int intensity) =>
        Assert.Equal((feeling, intensity), FriendshipCheckerApp.FeelingOf(friendship));

    [Fact]
    public void TheTeamWandersAboutTheScreen()
    {
        var rig = new Rig(Mon("Bidoof"), Mon("Shinx"), Mon("Starly"));
        var app = rig.Open<FriendshipCheckerApp>(PoketchApp.FriendshipChecker);
        rig.Run(app, 0.05f);
        Assert.Equal(3, app.Team.Count);
        var before = app.Team.Select(w => w.Position).ToList();
        rig.Run(app, 1f);
        Assert.Contains(Enumerable.Range(0, 3), i => Vector2.Distance(app.Team[i].Position, before[i]) > 10);
        Assert.All(app.Team, w => Assert.Equal(FriendshipCheckerApp.Action.Wander, w.Action));
    }

    [Fact]
    public void APokemonThatLikesThePlayerShowsItsHeartsWhenTouched()
    {
        var rig = new Rig(Mon("Bidoof", friendship: 255));
        var app = rig.Open<FriendshipCheckerApp>(PoketchApp.FriendshipChecker);
        app.Press(0, rig.Context);
        rig.Run(app, 0.2f);

        var w = app.Team[0];
        Assert.Equal(FriendshipCheckerApp.Action.ShowLike, w.Action);
        Assert.Equal(3, w.Hearts);
        Assert.Equal(Vector2.Zero, w.Velocity);
        Assert.Equal(new[] { "Bidoof" }, rig.Cries);

        // Once the stylus is lifted it wanders off again
        rig.Run(app, FriendshipCheckerApp.HoldSeconds);
        Assert.Equal(FriendshipCheckerApp.Action.Wander, w.Action);
        Assert.Equal(0, w.Hearts);
    }

    [Fact]
    public void APokemonThatDislikesThePlayerKeepsStillAndShowsNoHeart()
    {
        var rig = new Rig(Mon("Bidoof", friendship: 0));
        var app = rig.Open<FriendshipCheckerApp>(PoketchApp.FriendshipChecker);
        app.Press(0, rig.Context);
        rig.Run(app, 0.2f);

        var w = app.Team[0];
        Assert.Equal(FriendshipCheckerApp.Action.ShowDislike, w.Action);
        Assert.Equal(0, w.Hearts);
        Assert.Equal(Vector2.Zero, w.Velocity);
        Assert.Single(rig.Cries);
    }

    [Fact]
    public void ThoseThatLikeThePlayerGatherToTheStylusAndTheOthersRunFromIt()
    {
        // The ground's second spot is at the top middle; each Pokémon is put 78 units under it, inside the
        // original's 48 pixels of reach (90 units) and outside its touch (30)
        foreach (var (friendship, gathers) in new[] { (255, true), (0, false) })
        {
            var rig = new Rig(Mon("Bidoof", friendship: friendship));
            var app = rig.Open<FriendshipCheckerApp>(PoketchApp.FriendshipChecker);
            app.Update(0f, rig.Context);
            var spot = app.Buttons(rig.Context).First(b => b.Id == FriendshipCheckerApp.GroundId + 1);
            var stylus = new Vector2(spot.CentreX * 8, spot.CentreY * 8);
            var w = app.Team[0];
            w.Position = stylus + new Vector2(0, 78);
            w.Velocity = Vector2.Zero;

            app.Press(spot.Id, rig.Context);
            rig.Run(app, 0.15f);
            float distance = Vector2.Distance(w.Position, stylus);
            if (gathers)
            {
                Assert.True(distance < 78, $"came no nearer: {distance}");
                rig.Run(app, 0.6f);
                Assert.Equal(FriendshipCheckerApp.Action.ShowLike, w.Action);
            }
            else
            {
                Assert.Equal(FriendshipCheckerApp.Action.RunAway, w.Action);
                Assert.True(distance > 78, $"didn't run: {distance}");
            }
        }
    }

    [Fact]
    public void TwoQuickTouchesOfTheGroundMakeEveryoneJump()
    {
        var rig = new Rig(Mon("Bidoof"), Mon("Shinx"));
        var app = rig.Open<FriendshipCheckerApp>(PoketchApp.FriendshipChecker);
        int spot = FriendshipCheckerApp.GroundId + 4;
        app.Press(spot, rig.Context);
        rig.Run(app, 0.1f);
        app.Press(spot, rig.Context);
        app.Update(Frame, rig.Context);

        Assert.All(app.Team, w => Assert.Equal(FriendshipCheckerApp.Action.Jump, w.Action));
        Assert.Contains("poketch_count", rig.Sounds);
        rig.Run(app, 0.15f);
        Assert.All(app.Team, w => Assert.True(w.Lift > 0));

        // HandleJumpAction: 8 degrees a frame to 180, so down again in 23 frames
        rig.Run(app, 0.4f);
        Assert.All(app.Team, w =>
        {
            Assert.Equal(FriendshipCheckerApp.Action.Wander, w.Action);
            Assert.Equal(0f, w.Lift);
        });
    }

    // ---- Day-Care Checker ----

    [Fact]
    public void TheDayCareCheckerIsEmptyWhileTheDayCareIs()
    {
        var rig = new Rig(Mon("Bidoof"));
        var app = rig.Open<DayCareCheckerApp>(PoketchApp.DayCareChecker);
        app.Update(Frame, rig.Context);
        Assert.Empty(app.Shown);
        Assert.False(app.HasEgg);
    }

    [Fact]
    public void TouchingTheDayCareCheckerReadsTheDayCareAgainThroughAMosaic()
    {
        var rig = new Rig(Mon("Bidoof"));
        var app = rig.Open<DayCareCheckerApp>(PoketchApp.DayCareChecker);
        app.Update(Frame, rig.Context);

        rig.Context.DayCare = new[] { Mon("Shinx", Gender.Male, level: 7), Mon("Shinx", Gender.Female, level: 100), Mon("Bidoof") };
        app.Press(DayCareCheckerApp.Screen.Id, rig.Context);
        Assert.Equal(2, app.Shown.Count);
        Assert.Equal(Gender.Male, app.Shown[0].Gender);
        Assert.Equal(Gender.Female, app.Shown[1].Gender);
        Assert.Equal(DayCareCheckerApp.MosaicStart, app.Mosaic);
        Assert.Single(rig.Sounds);

        // Ten steps of four frames
        rig.Frames(app, 39);
        Assert.Equal(1, app.Mosaic);
        rig.Frames(app, 1);
        Assert.Equal(0, app.Mosaic);
    }

    [Theory]
    // SetLevelSprites: no hundreds under 100, no tens under 10
    [InlineData(5, "5")]
    [InlineData(42, "42")]
    [InlineData(100, "100")]
    public void ALevelIsShownWithoutLeadingNoughts(int level, string figures) =>
        Assert.Equal(figures, DayCareCheckerApp.Figures(level));

    // ---- Pokémon History ----

    [Fact]
    public void TheHistoryKeepsTheLastTwelveOldestFirst()
    {
        var rig = new Rig();
        string[] species = { "Bidoof", "Shinx", "Starly", "Turtwig", "Buizel", "Pichu", "Magnemite", "Gastly", "Ditto", "Bibarel", "Bidoof", "Shinx", "Starly", "Turtwig" };
        foreach (var s in species) rig.Poketch.Remember(Mon(s));

        Assert.Equal(Poketch.HistoryLength, rig.Poketch.History.Count);
        Assert.Equal(species.Skip(2), rig.Poketch.History);

        var app = rig.Open<PokemonHistoryApp>(PoketchApp.PokemonHistory);
        var buttons = app.Buttons(rig.Context);
        Assert.Equal(12, buttons.Count);
        // Read like a page: the oldest at the top left, the newest at the bottom right
        Assert.True(buttons[0].X < buttons[3].X && buttons[0].Y == buttons[3].Y);
        Assert.True(buttons[11].Y > buttons[0].Y && buttons[11].X == buttons[3].X);

        app.Press(0, rig.Context);
        app.Press(11, rig.Context);
        Assert.Equal(new[] { "Starly", "Turtwig" }, rig.Cries);
    }

    [Fact]
    public void AnEmptyHistoryHasNothingToTouch()
    {
        var rig = new Rig();
        var app = rig.Open<PokemonHistoryApp>(PoketchApp.PokemonHistory);
        Assert.Empty(app.Buttons(rig.Context));
    }

    // ---- Move Tester ----

    [Theory]
    // GetExclamationCount: three, one more for each type weak to the move and one fewer for each that resists it
    [InlineData(PokemonType.Normal, PokemonType.Normal, null, 3)]
    [InlineData(PokemonType.Fire, PokemonType.Grass, null, 4)]
    [InlineData(PokemonType.Fire, PokemonType.Grass, PokemonType.Bug, 5)]
    [InlineData(PokemonType.Fire, PokemonType.Water, null, 2)]
    [InlineData(PokemonType.Fire, PokemonType.Water, PokemonType.Rock, 1)]
    [InlineData(PokemonType.Normal, PokemonType.Ghost, null, 0)]
    [InlineData(PokemonType.Electric, PokemonType.Water, PokemonType.Ground, 0)]
    [InlineData(PokemonType.Fire, PokemonType.Grass, PokemonType.Water, 3)]
    // A second type the same as the first counts once
    [InlineData(PokemonType.Fire, PokemonType.Grass, PokemonType.Grass, 4)]
    public void TheMoveTesterMarksHowWellAMoveWorks(PokemonType attack, PokemonType first, PokemonType? second, int marks) =>
        Assert.Equal(marks, MoveTesterApp.MarksFor(attack, first, second, Ruleset.Platinum));

    [Fact]
    public void TheMoveTesterReadsTheGamesOwnChart()
    {
        // Steel resists Ghost in Platinum, and not by the modern rules
        Assert.Equal(2, MoveTesterApp.MarksFor(PokemonType.Ghost, PokemonType.Steel, null, Ruleset.Platinum));
        Assert.Equal(3, MoveTesterApp.MarksFor(PokemonType.Ghost, PokemonType.Steel, null, Ruleset.Modern));
    }

    [Fact]
    public void TheArrowsGoThroughTheTypesInTheOriginalsOrder()
    {
        var rig = new Rig();
        var app = rig.Open<MoveTesterApp>(PoketchApp.MoveTester);
        app.Rules = Ruleset.Platinum;
        Assert.Equal((PokemonType.Normal, PokemonType.Normal, (PokemonType?)null), (app.Attack, app.First, app.Second));
        Assert.Equal(3, app.Marks);

        app.Press(MoveTesterApp.AttackUp, rig.Context);
        Assert.Equal(PokemonType.Fire, app.Attack);
        for (int i = 0; i < 4; i++) app.Press(MoveTesterApp.FirstUp, rig.Context);
        Assert.Equal(PokemonType.Grass, app.First);
        Assert.Equal(4, app.Marks);

        // The move's type goes round; the second type goes to none past either end
        app.Press(MoveTesterApp.AttackDown, rig.Context);
        app.Press(MoveTesterApp.AttackDown, rig.Context);
        Assert.Equal(PokemonType.Steel, app.Attack);
        app.Press(MoveTesterApp.SecondDown, rig.Context);
        Assert.Equal(PokemonType.Steel, app.Second);
        app.Press(MoveTesterApp.SecondUp, rig.Context);
        Assert.Null(app.Second);
        app.Press(MoveTesterApp.SecondUp, rig.Context);
        Assert.Equal(PokemonType.Normal, app.Second);
        Assert.Equal(10, rig.Sounds.Count(s => s == "poketch"));
        Assert.Equal(PokemonType.Normal, MoveTesterApp.Shift(null, 1, true));
        Assert.Equal(PokemonType.Steel, MoveTesterApp.Shift(null, -1, true));
    }

    [Fact]
    public void TheMoveTesterKeepsWhatWasChosenForNextTime()
    {
        var rig = new Rig();
        var app = rig.Open<MoveTesterApp>(PoketchApp.MoveTester);
        app.Press(MoveTesterApp.AttackUp, rig.Context);
        app.Press(MoveTesterApp.SecondDown, rig.Context);

        rig.Poketch.Close();
        var again = (MoveTesterApp)rig.Poketch.State!;
        Assert.NotSame(app, again);
        Assert.Equal(PokemonType.Fire, again.Attack);
        Assert.Equal(PokemonType.Normal, again.First);
        Assert.Equal(PokemonType.Steel, again.Second);
    }

    // ---- Matchup Checker ----

    private static readonly TrainerMark Someone = new("Someone", 54321, PlayerLook.Boy);

    [Fact]
    public void TheMatchupIsTheDayCaresOwn()
    {
        // BoxMon_GetPairDaycareCompatibilityScore, as levels: 0 the best, 3 none
        Assert.Equal(MatchupCheckerApp.Best, MatchupCheckerApp.Level(Mon("Bidoof"), Mon("Bidoof", Gender.Female, trainer: Someone)));
        Assert.Equal(MatchupCheckerApp.Good, MatchupCheckerApp.Level(Mon("Bidoof"), Mon("Bidoof", Gender.Female)));
        Assert.Equal(MatchupCheckerApp.Good, MatchupCheckerApp.Level(Mon("Bidoof"), Mon("Shinx", Gender.Female, trainer: Someone)));
        Assert.Equal(MatchupCheckerApp.Poor, MatchupCheckerApp.Level(Mon("Bidoof"), Mon("Shinx", Gender.Female)));
        // The same gender, no egg group shared, the Undiscovered group
        Assert.Equal(MatchupCheckerApp.None, MatchupCheckerApp.Level(Mon("Bidoof"), Mon("Shinx")));
        Assert.Equal(MatchupCheckerApp.None, MatchupCheckerApp.Level(Mon("Bidoof"), Mon("Starly", Gender.Female)));
        Assert.Equal(MatchupCheckerApp.None, MatchupCheckerApp.Level(Mon("Pichu"), Mon("Pichu", Gender.Female, trainer: Someone)));
        // Ditto: with anyone else, even one with no gender, by the trainers alone; never with another Ditto
        Assert.Equal(MatchupCheckerApp.Poor, MatchupCheckerApp.Level(Mon("Ditto", Gender.Genderless), Mon("Magnemite", Gender.Genderless)));
        Assert.Equal(MatchupCheckerApp.Good, MatchupCheckerApp.Level(Mon("Ditto", Gender.Genderless, trainer: Someone), Mon("Bidoof")));
        Assert.Equal(MatchupCheckerApp.None, MatchupCheckerApp.Level(Mon("Ditto", Gender.Genderless), Mon("Ditto", Gender.Genderless, trainer: Someone)));
        // No Ditto and no gender
        Assert.Equal(MatchupCheckerApp.None, MatchupCheckerApp.Level(Mon("Magnemite", Gender.Genderless), Mon("Magnemite", Gender.Genderless, trainer: Someone)));
    }

    [Fact]
    public void TheBestMatchSwimsThreeStepsAndKisses()
    {
        var rig = new Rig(Mon("Bidoof"), Mon("Bidoof", Gender.Female, trainer: Someone));
        var app = rig.Open<MatchupCheckerApp>(PoketchApp.MatchupChecker);
        app.Press(MatchupCheckerApp.Check, rig.Context);
        Assert.Equal(MatchupCheckerApp.Best, app.Result);
        Assert.Equal(3, app.Hearts);
        Assert.True(app.Busy);

        // A touch while they swim does nothing
        app.Press(MatchupCheckerApp.ChangeLeft, rig.Context);
        Assert.Equal(0, app.Left);

        rig.Run(app, 1.5f);
        Assert.False(app.Busy);
        Assert.Equal(48f, app.Offset);
        Assert.True(app.Kissing);
        Assert.False(app.TurnedAway);
        Assert.Equal(new[] { "poketch_count", "poketch", "poketch", "poketch", "dowsing_ping" }, rig.Sounds);
    }

    [Fact]
    public void NoMatchTurnsTheFishAway()
    {
        var rig = new Rig(Mon("Bidoof"), Mon("Shinx"));
        var app = rig.Open<MatchupCheckerApp>(PoketchApp.MatchupChecker);
        app.Press(MatchupCheckerApp.Check, rig.Context);
        Assert.Equal(MatchupCheckerApp.None, app.Result);
        Assert.Equal(0, app.Hearts);
        rig.Run(app, 1.5f);
        Assert.True(app.TurnedAway);
        Assert.Equal(0f, app.Offset);
        Assert.False(app.Kissing);
    }

    [Fact]
    public void EachSideGoesThroughTheTeamPastTheOther()
    {
        var rig = new Rig(Mon("Bidoof"), Mon("Shinx", Gender.Female), Mon("Starly"), Mon("Turtwig"));
        var app = rig.Open<MatchupCheckerApp>(PoketchApp.MatchupChecker);
        Assert.Equal((0, 1), (app.Left, app.Right));

        app.Press(MatchupCheckerApp.ChangeLeft, rig.Context);
        Assert.Equal(2, app.Left);
        app.Press(MatchupCheckerApp.ChangeRight, rig.Context);
        Assert.Equal(3, app.Right);
        app.Press(MatchupCheckerApp.ChangeRight, rig.Context);
        Assert.Equal(0, app.Right);
        Assert.Equal(new[] { "Starly", "Turtwig", "Bidoof" }, rig.Cries);
    }

    [Fact]
    public void WithTooFewPokemonTheMatchupCheckerCannotChangeOrCheck()
    {
        var two = new Rig(Mon("Bidoof"), Mon("Shinx", Gender.Female));
        var app = two.Open<MatchupCheckerApp>(PoketchApp.MatchupChecker);
        app.Press(MatchupCheckerApp.ChangeLeft, two.Context);
        Assert.Equal(0, app.Left);
        Assert.Empty(two.Cries);

        var one = new Rig(Mon("Bidoof"));
        var lone = one.Open<MatchupCheckerApp>(PoketchApp.MatchupChecker);
        lone.Press(MatchupCheckerApp.Check, one.Context);
        Assert.Null(lone.Result);
        Assert.Equal(new[] { "poketch_beep" }, one.Sounds);
        Assert.Null(lone.RightPokemon(one.Context));
    }
}
