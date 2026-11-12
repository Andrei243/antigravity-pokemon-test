using System;
using System.Linq;
using System.Numerics;
using Xunit;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 35 · BN2, the care close-up: strokes on the liked spot make a pat sooner, the brush takes dust and the cloth
/// doesn't, the medicine cures once for each battle, a full Pokémon refuses, the tray, the camp's faces at nought leave
/// a pose as it was, the stage's spots, and CAMP under the TEAM CARE row.
/// </summary>
[Collection("Shared clocks and dice")]
public class CareScreenTests
{
    private const int Today = 9800;

    private static (CareScreen Screen, Pokemon Pokemon, CareBook Book) Open(string species = "Turtwig")
    {
        var p = Mon(species, 10);
        p.Friendship = 70;
        var book = new CareBook();
        var screen = new CareScreen();
        Assert.True(screen.Open(p, book, Today, night: false));
        screen.Layout = CareStage.Fallback(BodyPlan.Quadruped);
        return (screen, p, book);
    }

    // Strokes back and forth about a point until the screen's count passes on, returning how far the hand went
    private static float StrokeAt(CareScreen screen, Vector2 at, Func<bool> done)
    {
        screen.MoveHand(at - screen.Hand);
        screen.Hold();
        float went = 0f;
        for (int i = 0; i < 400 && !done(); i++)
        {
            var step = new Vector2(i % 2 == 0 ? 0.02f : -0.02f, 0f);
            screen.MoveHand(step);
            went += step.Length();
        }
        screen.Release();
        return went;
    }

    [Fact]
    public void StrokesOnTheLikedSpotMakeAPatSoonerThanStrokesElsewhere()
    {
        var (liked, p1, _) = Open();
        int before = p1.Friendship;
        float onLiked = StrokeAt(liked, liked.Layout.Liked, () => p1.Friendship > before);
        var (elsewhere, p2, _) = Open();
        // The head of a quadruped is on its body but no liked spot
        float onHead = StrokeAt(elsewhere, elsewhere.Layout.Head, () => p2.Friendship > before);
        Assert.True(p1.Friendship > before && p2.Friendship > before);
        Assert.True(onLiked < onHead, $"{onLiked} on the liked spot, {onHead} elsewhere");
    }

    [Fact]
    public void TheBrushTakesDustOffAndTheClothDoesNot()
    {
        var (screen, p, book) = Open();
        CareRules.Soil(book.Of(p), SoilMark.Dust);
        screen.ChooseTool(CareTool.Cloth);
        StrokeAt(screen, screen.Layout.Mark(0), () => false);
        Assert.Equal(new[] { SoilMark.Dust }, book.Of(p).Marks);
        Assert.Equal(Text.Of("care.note.wrongTool.Cloth"), screen.Note);
        screen.ChooseTool(CareTool.Brush);
        float went = StrokeAt(screen, screen.Layout.Mark(0), () => book.Of(p).Marks.Count == 0);
        Assert.Empty(book.Of(p).Marks);
        Assert.InRange(went, CareScreen.MarkWear, CareScreen.MarkWear + 0.05f);
    }

    [Fact]
    public void TheMedicineCuresOnceForEachBattle()
    {
        var (screen, p, book) = Open();
        p.Status = StatusCondition.Poison;
        screen.ChooseTool(CareTool.Medicine);
        screen.MoveHand(screen.Layout.Body - screen.Hand);
        screen.Hold();
        Assert.Equal(StatusCondition.None, p.Status);
        p.Status = StatusCondition.Poison;
        screen.Hold();
        Assert.Equal(StatusCondition.Poison, p.Status);
        CareRules.AfterBattle(book, new CareRules.BattleOutcome(new[] { p }, true, false, Array.Empty<Pokemon>(), PokemonPlatinumEngine.Battle.Sim.BattleTerrain.Plain, PokemonPlatinumEngine.Battle.Sim.BattleWeather.None));
        screen.Hold();
        Assert.Equal(StatusCondition.None, p.Status);
    }

    [Fact]
    public void AFullPokemonRefusesAndTheTreatToolAsksTheBag()
    {
        var (screen, p, book) = Open();
        var bag = new Inventory();
        var berry = ItemDatabase.Get("Oran Berry")!;
        bag.AddItem(berry, 9);
        screen.ChooseTool(CareTool.Treat);
        screen.Hold();
        Assert.Equal(CareRequest.Treat, screen.TakeRequest());
        for (int i = 0; i < CareRules.Fullness; i++) Assert.True(screen.Feed(berry, bag));
        Assert.False(screen.Feed(berry, bag));
        Assert.Equal(4, bag.GetQuantity(berry));
        screen.Hold();
        Assert.Equal(CareRequest.None, screen.TakeRequest());
        Assert.Equal(Text.Of("camp.note.full", ("name", p.DisplayName)), screen.Note);
    }

    [Fact]
    public void TheTrayGoesRoundAndAnEggIsNoCloseUps()
    {
        var (screen, _, _) = Open();
        Assert.Equal(CareTool.Hand, screen.Tool);
        screen.NextTool(-1);
        Assert.Equal(CareTool.Medicine, screen.Tool);
        screen.NextTool(1);
        screen.NextTool(1);
        Assert.Equal(CareTool.Treat, screen.Tool);
        var egg = Mon("Turtwig", 1);
        egg.IsEgg = true;
        Assert.False(new CareScreen().Open(egg, new CareBook(), Today, false));
        // The tray's chips stand apart in a row along the foot
        for (int i = 1; i < Enum.GetValues<CareTool>().Length; i++)
            Assert.True(ModernUi.CareToolChip(i).X > ModernUi.CareToolChip(i - 1).X + ModernUi.CareToolChip(i - 1).Width);
    }

    [Fact]
    public void TheHeadTurnsTowardTheHandAndALeanFollowsAPat()
    {
        var (screen, p, _) = Open();
        screen.MoveHand(screen.Layout.Head + new Vector2(0.3f, 0f) - screen.Hand);
        var pose = screen.Pose();
        Assert.True(pose.LookAt.X < 0f, "the hand on the picture's right is on the Pokémon's left");
        StrokeAt(screen, screen.Layout.Liked, () => p.Friendship > 70);
        screen.Advance(0.5f);
        Assert.True(screen.Pose().Happy > 0.5f);
        Assert.Equal(EyeState.Smile, PokemonAnimation.EyesOf(screen.Pose()));
        var night = new CareScreen();
        night.Open(p, new CareBook(), Today, night: true);
        Assert.True(night.Pose().Sleepy > 0.5f);
    }

    [Fact]
    public void TheCampsFacesAtNoughtLeaveAPoseAsItWas()
    {
        var model = PokemonModels.Get("Turtwig");
        var plain = new SkeletonPose(model.Skeleton.Count);
        var faced = new SkeletonPose(model.Skeleton.Count);
        var pose = new PokePose { Time = 1.37f, Blink = 0f };
        PokemonAnimation.Apply(model, pose, plain);
        PokemonAnimation.Apply(model, pose with { Happy = 0f, Sleepy = 0f, Eating = 0f, LookAt = Vector2.Zero }, faced);
        for (int b = 0; b < plain.Count; b++)
        {
            Assert.Equal(plain.Rotation[b], faced.Rotation[b]);
            Assert.Equal(plain.Offset[b], faced.Offset[b]);
            Assert.Equal(plain.Scale[b], faced.Scale[b]);
        }
        // ...and a look turns the head
        PokemonAnimation.Apply(model, pose with { LookAt = new Vector2(1f, 0f) }, faced);
        int head = Enumerable.Range(0, model.Bones.Count).First(b => model.Bones[b].Role == PokeRole.Head);
        Assert.NotEqual(plain.Rotation[head], faced.Rotation[head]);
        Assert.Equal(EyeState.Open, PokemonAnimation.EyesOf(pose));
    }

    [Fact]
    public void TheStagesSpotsLieOnThePictureAndALikedSpotByTheBodyPlan()
    {
        foreach (string species in new[] { "Turtwig", "Starly", "Chimchar" })
        {
            var model = PokemonModels.Get(species);
            var layout = CareStage.Of(model);
            foreach (var spot in new[] { layout.Head, layout.Body, layout.Liked })
            {
                Assert.InRange(spot.X, 0.05f, 0.95f);
                Assert.InRange(spot.Y, 0.05f, 0.95f);
            }
            Assert.True(layout.Head.Y < layout.Body.Y + 0.05f, $"{species}: the head stands over the body");
            Assert.True(layout.OnBody(layout.Liked), $"{species}: the liked spot is on it");
        }
        Assert.Equal(PokeRole.Head, CareStage.LikedRole(BodyPlan.Biped));
        Assert.Equal(PokeRole.Body, CareStage.LikedRole(BodyPlan.Quadruped));
        Assert.Equal(PokeRole.Fin, CareStage.LikedRole(BodyPlan.Fish));
    }

    [Fact]
    public void CampIsInTheStartMenuUnderTeamCareWithFollowerOffAndTheCloseUpOpensThroughTheGame()
    {
        var paths = SavePaths.Current;
        try
        {
            Dice.Seed(41);
            var engine = new GameEngine(new GameSettings { Muted = true, Follower = false });
            engine.InitializeHeadless();
            var game = engine.Drive;
            engine.NewGameRegion = "Sinnoh";
            engine.StartNewGame();
            StoryMigration.PastTheOpening(game.Story, ScriptLibrary.Default);
            game.Party.Clear();
            game.Party.Add(new Pokemon(PokemonDatabase.Get("Starly")!, 12));
            game.State = GameState.Overworld;
            Assert.True(game.HasCamp);
            game.ChooseFromStartMenu(StartMenuChoice.Camp);
            Assert.Equal(GameState.Camp, game.State);
            game.Camp.AskToCare();
            InputManager.Feed(ActionFrame.None);
            engine.Update(1f / 60f);
            Assert.Equal(GameState.Care, game.State);
            Assert.Same(game.Party.Members[0], game.CareCloseUp.Pokemon);
            game.CareCloseUp.Cancel();
            engine.Update(1f / 60f);
            Assert.Equal(GameState.Camp, game.State);
            // TEAM CARE off and FOLLOWER off: no camp
            engine.Settings.Care = RulesDefault.Off;
            Assert.False(game.HasCamp);
        }
        finally
        {
            InputManager.Feed(null);
            Dice.Seed(null);
            Ruleset.Use(RulesPreset.Platinum);
            SavePaths.Current = paths;
        }
    }

    [Fact]
    public void TheStartMenuHoldsElevenEntriesOnTheScreen()
    {
        var rows = ModernUi.StartMenuRows(GameEngine.VirtualWidth, 12);
        Assert.True(rows[^1].Y + rows[^1].Height < GameEngine.VirtualHeight - 160);
        Assert.Equal(74f, ModernUi.StartMenuRowHeight(10));
    }
}
