using System;
using Xunit;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

/// <summary>Trainers spotting the player, walking up, and what a win or a loss leaves behind.</summary>
public class TrainerEncounterTests
{
    private static (Map Map, NPC Trainer) RouteWithTrainer()
    {
        var map = new Map(10, 10);
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Starly")!, 4));
        var trainer = new NPC
        {
            Name = "Tristan",
            GridX = 5,
            GridY = 2,
            Facing = Direction.Down,
            IsTrainer = true,
            TrainerData = new Trainer { Name = "Tristan", TrainerClass = "Youngster", SightRange = 3, Party = party }
        };
        map.NPCs.Add(trainer);
        return (map, trainer);
    }

    private static void Tick(TrainerApproach approach, Player player, float seconds)
    {
        for (float t = 0f; t < seconds - 1e-4f; t += 1f / 60f) approach.Update(1f / 60f, player);
    }

    [Fact]
    public void TestTrainersSeeStraightAheadAsFarAsTheirSightRange()
    {
        var (map, trainer) = RouteWithTrainer();

        Assert.True(TrainerApproach.CanSee(map, trainer, 5, 3));
        Assert.True(TrainerApproach.CanSee(map, trainer, 5, 5));
        Assert.False(TrainerApproach.CanSee(map, trainer, 5, 6), "four tiles away is out of range");
        Assert.False(TrainerApproach.CanSee(map, trainer, 4, 3), "not looking sideways");
        Assert.False(TrainerApproach.CanSee(map, trainer, 5, 1), "not looking behind");

        Assert.Same(trainer, TrainerApproach.FindSpotter(map, 5, 4));
        Assert.Null(TrainerApproach.FindSpotter(map, 4, 4));
    }

    [Fact]
    public void TestTrainersCannotSeePastObstacles()
    {
        var (map, trainer) = RouteWithTrainer();
        map.SetSolid(5, 4, true);
        Assert.True(TrainerApproach.CanSee(map, trainer, 5, 3));
        Assert.False(TrainerApproach.CanSee(map, trainer, 5, 5));

        var (map2, trainer2) = RouteWithTrainer();
        map2.NPCs.Add(new NPC { Name = "Bystander", GridX = 5, GridY = 3 });
        Assert.False(TrainerApproach.CanSee(map2, trainer2, 5, 4));
    }

    [Fact]
    public void TestASpottingTrainerWalksUpAndThePlayerTurnsToFaceThem()
    {
        var (map, trainer) = RouteWithTrainer();
        var player = new Player(5, 5) { Facing = Direction.Left };
        var approach = new TrainerApproach(TrainerApproach.FindSpotter(map, player.GridX, player.GridY)!, player);

        // The "!" shows first, with the trainer still at their post
        Tick(approach, player, 0.5f);
        Assert.True(trainer.HasSpottedPlayer && trainer.ExclamationTimer > 0f);
        Assert.Equal((5, 2), (trainer.GridX, trainer.GridY));

        // Then they walk over, step by step
        bool seenMidStep = false;
        for (int i = 0; i < 600 && !approach.IsDone; i++)
        {
            approach.Update(1f / 60f, player);
            seenMidStep |= trainer.DrawY > 2.2f && trainer.DrawY < 3.8f && trainer.WalkBlend > 0.3f;
        }

        Assert.True(approach.IsDone);
        Assert.True(seenMidStep);
        Assert.False(trainer.HasSpottedPlayer);
        Assert.Equal((5, 4), (trainer.GridX, trainer.GridY));
        Assert.Equal(4f, trainer.DrawY);
        Assert.Equal(0f, trainer.WalkBlend);
        Assert.Equal(Direction.Up, player.Facing);
        Assert.Equal(MathF.Cos(Player.YawOf(Direction.Up)), MathF.Cos(player.Yaw), 2);
    }

    [Fact]
    public void TestATrainerRightNextToThePlayerStaysPut()
    {
        var (map, trainer) = RouteWithTrainer();
        var player = new Player(5, 3);
        var approach = new TrainerApproach(trainer, player);

        Tick(approach, player, 2f);

        Assert.True(approach.IsDone);
        Assert.Equal((5, 2), (trainer.GridX, trainer.GridY));
        Assert.Equal(Direction.Up, player.Facing);
    }

    [Fact]
    public void TestABeatenTrainerStaysAndDoesNotChallengeAgain()
    {
        var (map, trainer) = RouteWithTrainer();
        var player = new Player(5, 5);
        Tick(new TrainerApproach(trainer, player), player, 3f);

        trainer.FinishBattle(playerWon: true);

        Assert.True(trainer.HasBattled);
        Assert.Equal((5, 4), (trainer.GridX, trainer.GridY));
        Assert.Null(TrainerApproach.FindSpotter(map, 5, 5));
    }

    [Fact]
    public void TestATrainerWhoWonGoesBackAndCanBeChallengedAgain()
    {
        var (map, trainer) = RouteWithTrainer();
        var player = new Player(5, 5);
        Tick(new TrainerApproach(trainer, player), player, 3f);
        var starly = trainer.TrainerData!.Party.Members[0];
        starly.CurrentHP = 1;
        starly.Status = StatusCondition.Poison;

        trainer.FinishBattle(playerWon: false);

        Assert.False(trainer.HasBattled);
        Assert.Equal((5, 2, Direction.Down), (trainer.GridX, trainer.GridY, trainer.Facing));
        Assert.Equal(starly.MaxHP, starly.CurrentHP);
        Assert.Equal(StatusCondition.None, starly.Status);
        Assert.Same(trainer, TrainerApproach.FindSpotter(map, 5, 5));
    }
}
