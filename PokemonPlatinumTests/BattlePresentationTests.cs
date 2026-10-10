using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>Battle presentation (plan 04 · G8): effect cues, the effects themselves, the camera, the Poké Ball and the arenas.</summary>
public class BattlePresentationTests
{
    // ------------------------------------------------------------------ cues from the battle

    private static BattleEngine Battle(string mine, int level, string foe, int foeLevel, int seed = 1)
    {
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get(mine)!, level, new Random(seed)));
        var battle = new BattleEngine(party, new Pokemon(PokemonDatabase.Get(foe)!, foeLevel, new Random(seed + 1)), new Inventory(), new Pokedex());
        for (int i = 0; i < 10 && battle.HUD.MenuState == BattleMenuState.Message; i++) battle.ConfirmMessage();
        return battle;
    }

    private static void Tick(BattleEngine battle, float seconds)
    {
        for (float t = 0f; t < seconds - 1e-4f; t += 1f / 60f) battle.Update(1f / 60f);
    }

    // ------------------------------------------------------------------ the HP boxes

    [Fact]
    public void AWildFoeShowsTheCaughtMarkOnceItsSpeciesIsCaughtAndATrainersNever()
    {
        // The original's health box (HealthBox_DrawInfo): the Poké Ball for a caught species on an enemy box, never
        // in a trainer's battle (BATTLE_TYPE_TRAINER), read from the Pokédex (BattleSystem_HasCaughtSpecies)
        var dex = new Pokedex();
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Piplup")!, 12, new Random(1)));
        var bidoof = new Pokemon(PokemonDatabase.Get("Bidoof")!, 8, new Random(2));

        var wild = new BattleEngine(party, bidoof, new Inventory(), dex);
        Assert.False(wild.ShowsCaughtMark(wild.EnemyPokemon));
        dex.RegisterSeen(bidoof.Species.DexNumber);
        Assert.False(wild.ShowsCaughtMark(wild.EnemyPokemon));
        dex.RegisterCaught(bidoof.Species.DexNumber);
        Assert.True(wild.ShowsCaughtMark(wild.EnemyPokemon));

        var trainerParty = new Party();
        trainerParty.Add(new Pokemon(PokemonDatabase.Get("Bidoof")!, 8, new Random(3)));
        var youngster = new Trainer { Name = "Tester", TrainerClass = "Youngster", Party = trainerParty };
        var trainers = new BattleEngine(party, trainerParty.Members[0], new Inventory(), dex, youngster);
        Assert.False(trainers.ShowsCaughtMark(trainers.EnemyPokemon));
    }

    [Fact]
    public void ADamagingMoveCuesItsEffectFromTheUserToTheTarget()
    {
        var battle = Battle("Piplup", 12, "Bidoof", 8);
        int hp = battle.EnemyPokemon.CurrentHP;
        var move = battle.PlayerPokemon.Moves.First(m => m.Category != MoveCategory.Status && m.Power > 0);
        battle.SelectMove(battle.PlayerPokemon.Moves.IndexOf(move));

        var cue = Assert.Single(battle.Anim.Cues, c => c.Kind == CueKind.Move);
        Assert.Equal(move.Name, cue.Move);
        Assert.Equal(move.Type, cue.Type);
        Assert.Equal(move.Category, cue.Category);
        Assert.Equal(BattleSide.Player, cue.FromSide);
        Assert.Equal(BattleSide.Enemy, cue.ToSide);
        Assert.False(cue.OnSelf);

        // It lands with the damage, or flies past on a miss
        Tick(battle, BattleAnimator.ImpactTime + 0.05f);
        Assert.Equal(battle.EnemyPokemon.CurrentHP < hp, cue.Landed);
        Assert.Equal(!cue.Landed, cue.Missed);

        // And it is gone once it has played
        Tick(battle, BattleAnimator.EffectTime);
        Assert.DoesNotContain(cue, battle.Anim.Cues);
    }

    [Fact]
    public void AStatusMoveOnItselfPlaysOnTheUserAndShowsTheStatRising()
    {
        var battle = Battle("Turtwig", 10, "Starly", 2);
        int withdraw = battle.PlayerPokemon.Moves.FindIndex(m => m.Name == "Withdraw");
        Assert.True(withdraw >= 0, "Turtwig knows Withdraw at level 10");
        battle.SelectMove(withdraw);

        var cue = Assert.Single(battle.Anim.Cues, c => c.Kind == CueKind.Move);
        Assert.True(cue.OnSelf);
        Assert.True(MoveFx.HasOwnEffect("Withdraw"));

        // The stat change has its own effect when its message shows
        for (int i = 0; i < 6 && !battle.Anim.Cues.Any(c => c.Kind == CueKind.StatUp); i++)
        {
            battle.ConfirmMessage();
            Tick(battle, 0.05f);
        }
        var up = Assert.Single(battle.Anim.Cues, c => c.Kind == CueKind.StatUp);
        Assert.Equal(BattleSide.Player, up.ToSide);
    }

    [Fact]
    public void AStatLoweringMoveShowsTheFoesStatFalling()
    {
        var battle = Battle("Piplup", 12, "Bidoof", 3);
        int growl = battle.PlayerPokemon.Moves.FindIndex(m => m.Name == "Growl");
        Assert.True(growl >= 0, "Piplup knows Growl at level 12");
        battle.SelectMove(growl);
        Assert.False(Assert.Single(battle.Anim.Cues, c => c.Kind == CueKind.Move).OnSelf);
        for (int i = 0; i < 6 && !battle.Anim.Cues.Any(c => c.Kind == CueKind.StatDown); i++)
        {
            battle.ConfirmMessage();
            Tick(battle, 0.05f);
        }
        Assert.Equal(BattleSide.Enemy, Assert.Single(battle.Anim.Cues, c => c.Kind == CueKind.StatDown).ToSide);
    }

    [Fact]
    public void EachUseOfAMoveGetsItsOwnSeed()
    {
        var anim = new BattleAnimator();
        var a = anim.Cue(new EffectCue { Move = "Ember" });
        var b = anim.Cue(new EffectCue { Move = "Ember" });
        Assert.NotEqual(a.Seed, b.Seed);
    }

    // ------------------------------------------------------------------ the effects

    private static readonly FxPlaces Places = new();

    private static FxList Draw(EffectCue cue, float age)
    {
        cue.Age = age;
        var fx = new FxList();
        MoveFx.Play(cue, Places, fx);
        return fx;
    }

    private static EffectCue Cue(string move, PokemonType type, MoveCategory category, bool missed = false, bool critical = false, bool self = false) =>
        new BattleAnimator().Cue(new EffectCue
        {
            Move = move, Type = type, Category = category, FromSide = BattleSide.Player, ToSide = self ? BattleSide.Player : BattleSide.Enemy,
            Missed = missed, Critical = critical
        });

    public static IEnumerable<object[]> TypesAndCategories =>
        from type in Enum.GetValues<PokemonType>()
        from category in Enum.GetValues<MoveCategory>()
        select new object[] { type, category };

    [Theory]
    [MemberData(nameof(TypesAndCategories))]
    public void EveryTypeAndCategoryHasATemplateThatDrawsSomething(PokemonType type, MoveCategory category)
    {
        var cue = Cue("", type, category);
        int drawn = 0;
        foreach (float age in new[] { 0.1f, 0.3f, BattleAnimator.ImpactTime + 0.05f, 0.6f }) drawn += Draw(cue, age).Count;
        Assert.True(drawn > 0, $"{type} {category} draws nothing");
        // Nothing is left on screen once the cue is over
        Assert.Equal(0, Draw(cue, BattleAnimator.EffectTime + 0.05f).Count);
    }

    [Fact]
    public void MovesWithTheirOwnEffectsAreRealMoves()
    {
        MoveDatabase.Initialize();
        foreach (var move in MoveFx.MovesWithOwnEffects) Assert.True(MoveDatabase.Get(move) != null, move);
    }

    [Fact]
    public void AMissLandsNothingOnTheTarget()
    {
        var target = Places.Body(BattleSide.Enemy, 0);
        float h = Places.Height(BattleSide.Enemy, 0);
        int NearTarget(FxList fx) => fx.Quads.Count(q => Vector3.Distance(q.Center, target) < h * 0.6f);

        float after = BattleAnimator.ImpactTime + 0.08f;
        Assert.True(NearTarget(Draw(Cue("Tackle", PokemonType.Normal, MoveCategory.Physical), after)) > 0);
        Assert.Equal(0, NearTarget(Draw(Cue("Tackle", PokemonType.Normal, MoveCategory.Physical, missed: true), after)));
    }

    [Fact]
    public void ACriticalHitFlashesTheScreenWithinTheLimit()
    {
        float at = BattleAnimator.ImpactTime + 0.04f;
        var normal = Draw(Cue("Tackle", PokemonType.Normal, MoveCategory.Physical), at);
        var critical = Draw(Cue("Tackle", PokemonType.Normal, MoveCategory.Physical, critical: true), at);
        Assert.Equal(0f, normal.Flash);
        Assert.InRange(critical.Flash, 0.05f, 0.5f);
    }

    [Fact]
    public void NoEffectFlashesHarderThanTheStyleGuideAllows()
    {
        foreach (var move in MoveFx.MovesWithOwnEffects)
        {
            var data = MoveDatabase.Get(move)!;
            var cue = Cue(move, data.Type, data.Category, critical: true);
            for (float age = 0f; age < BattleAnimator.EffectTime; age += 0.02f)
                Assert.InRange(Draw(cue, age).Flash, 0f, 0.5f);
        }
    }

    [Theory]
    [InlineData(StatusCondition.Burn)]
    [InlineData(StatusCondition.Poison)]
    [InlineData(StatusCondition.Toxic)]
    [InlineData(StatusCondition.Paralyze)]
    [InlineData(StatusCondition.Sleep)]
    [InlineData(StatusCondition.Freeze)]
    public void EveryStatusConditionHasAnEffect(StatusCondition status)
    {
        var cue = new BattleAnimator().StatusGiven(BattleSide.Enemy, 0, status);
        Assert.True(new[] { 0.1f, 0.3f, 0.5f }.Sum(t => Draw(cue, t).Count) > 0);
    }

    [Fact]
    public void StatChangesAndHealsHaveEffects()
    {
        var anim = new BattleAnimator();
        foreach (var cue in new[] { anim.StatChange(BattleSide.Player, 0, true), anim.StatChange(BattleSide.Enemy, 0, false), anim.Heal(BattleSide.Player, 0) })
            Assert.True(Draw(cue, 0.4f).Count > 0, cue.Kind.ToString());
    }

    [Fact]
    public void EffectShapesStayInsideTheirCells()
    {
        // Mipmaps would bleed a shape into its neighbours if it reached the cell's edge (the beam is only ever
        // sampled down its middle)
        foreach (var shape in Enum.GetValues<FxShape>().Where(s => s != FxShape.Beam))
        {
            float total = 0f, edge = 0f;
            for (int y = -16; y <= 16; y++)
                for (int x = -16; x <= 16; x++)
                {
                    var p = new Vector2(x, y) / 16f * 1.14f;
                    float c = FxTextures.Coverage(shape, p);
                    if (MathF.Max(MathF.Abs(p.X), MathF.Abs(p.Y)) > 1.1f) edge = MathF.Max(edge, c);
                    else total += c;
                }
            Assert.True(total > 8f, $"{shape} is almost empty");
            Assert.True(edge < 0.03f, $"{shape} reaches the edge of its cell ({edge})");
        }
    }

    // ------------------------------------------------------------------ the camera

    private static float Reach(CameraShot s) => Vector3.Distance(s.Position, s.Target);

    /// <summary>How much of the world the shot fits in the frame's height at what it looks at.</summary>
    private static float Framed(CameraShot s) => 2f * Reach(s) * MathF.Tan(s.Fov * MathF.PI / 360f);

    [Fact]
    public void TheCameraSweepsInAndSettlesOnTheOverview()
    {
        var anim = new BattleAnimator();
        var (start, kind, _) = BattleCamera.Direct(anim, Places);
        Assert.Equal(ShotKind.Intro, kind);
        Assert.True(Reach(start) > Reach(BattleCamera.Overview(0f)) * 1.2f, "the sweep starts further out");

        anim.Update(2f, null, null);
        Assert.Equal(ShotKind.Overview, BattleCamera.Direct(anim, Places).Kind);
    }

    private static BattleAnimator Settled()
    {
        var anim = new BattleAnimator();
        anim.Update(2f, null, null);
        return anim;
    }

    private static void Advance(BattleAnimator anim, float seconds)
    {
        for (float t = 0f; t < seconds - 1e-4f; t += 1f / 60f) anim.Update(1f / 60f, null, null);
    }

    [Fact]
    public void AMoveFollowsTheAttackerThenCutsToTheTarget()
    {
        var anim = Settled();
        anim.Cue(new EffectCue { Move = "Tackle", Category = MoveCategory.Physical, FromSide = BattleSide.Player, ToSide = BattleSide.Enemy });
        var attacker = Places.Body(BattleSide.Player, 0);
        var target = Places.Body(BattleSide.Enemy, 0);

        Advance(anim, 0.1f);
        var (shot, kind, _) = BattleCamera.Direct(anim, Places);
        Assert.Equal(ShotKind.Attacker, kind);
        Assert.True(Vector3.Distance(shot.Target, attacker) < 0.2f);
        Assert.True(Reach(shot) < Reach(BattleCamera.Overview(anim.Time)), "closer than the overview");

        Advance(anim, BattleCamera.CutTime);
        (shot, kind, _) = BattleCamera.Direct(anim, Places);
        Assert.Equal(ShotKind.Target, kind);
        Assert.True(Vector3.Distance(shot.Target, target) < 0.2f);

        Advance(anim, BattleCamera.MoveShotEnd);
        Assert.Equal(ShotKind.Overview, BattleCamera.Direct(anim, Places).Kind);
    }

    [Fact]
    public void TheCameraCutsToTheTargetButEasesEverywhereElse()
    {
        var anim = Settled();
        var camera = new BattleCamera();
        camera.Update(1f / 60f, anim, Places, 0f);
        anim.Cue(new EffectCue { Move = "Ember", Category = MoveCategory.Special, FromSide = BattleSide.Player, ToSide = BattleSide.Enemy });
        Advance(anim, 0.05f);

        // Easing toward the attacker: one frame moves only part of the way
        camera.Update(1f / 60f, anim, Places, 0f);
        var wanted = BattleCamera.Direct(anim, Places).Shot;
        Assert.True(Vector3.Distance(camera.Current.Position, wanted.Position) > 0.5f);

        // The cut to the target is immediate
        Advance(anim, BattleCamera.CutTime);
        camera.Update(1f / 60f, anim, Places, 0f);
        Assert.Equal(ShotKind.Target, camera.Kind);
        Assert.True(Vector3.Distance(camera.Current.Position, BattleCamera.Direct(anim, Places).Shot.Position) < 1e-3f);
    }

    [Fact]
    public void TheCameraNeverEasesLowAcrossThePlayersSide()
    {
        // A move from a shot of the foe back to the overview passes high over the player's platform, or cuts
        var anim = Settled();
        var camera = new BattleCamera();
        anim.SendOut(BattleSide.Enemy, new Pokemon(PokemonDatabase.Get("Shinx")!, 5));
        Advance(anim, 0.3f);
        camera.Update(1f / 60f, anim, Places, 0f);
        Assert.Equal(ShotKind.SendOut, camera.Kind);
        var player = BattleStage.PlayerSpot + new Vector3(0, 1.2f, 0);
        for (int i = 0; i < 90; i++)
        {
            anim.Update(1f / 60f, null, null);
            camera.Update(1f / 60f, anim, Places, 0f);
            Assert.True(Vector3.Distance(camera.Current.Position, player) > 2.5f, $"frame {i}: the camera came within {Vector3.Distance(camera.Current.Position, player):F2} of the player's side");
        }
        Assert.Equal(ShotKind.Overview, camera.Kind);
        // The rule itself: a straight line through the player's platform is caught, the overview's drift isn't
        Assert.True(BattleCamera.CrossesPlayerSide(BattleStage.PlayerSpot + new Vector3(-4, 1, 0), BattleStage.PlayerSpot + new Vector3(4, 1, 0)));
        Assert.False(BattleCamera.CrossesPlayerSide(BattleCamera.Overview(0f).Position, BattleCamera.Overview(5f).Position));
    }

    [Theory]
    [InlineData(0.8f)]
    [InlineData(1.5f)]
    [InlineData(2.2f)]
    public void TheCameraGivesThePlayersTrainerRoom(float foeHeight)
    {
        // While the player's trainer still stands on their platform, the camera never skims their head on its way
        // to the foe being sent out: it cuts where easing would carry it past them
        var places = new FxPlaces();
        places.Set(BattleSide.Enemy, 0, BattleStage.EnemySpot + new Vector3(0, BattleStage.PlatformHeight, 0), foeHeight);
        var anim = Settled();
        var camera = new BattleCamera();
        camera.Update(1f / 60f, anim, places, 0f);
        anim.SendOut(BattleSide.Enemy, new Pokemon(PokemonDatabase.Get("Torterra")!, 50));
        var trainer = BattleStage.PlayerSpot + new Vector3(0, 1.2f, 0);
        for (int i = 0; i < 50; i++)
        {
            anim.Update(1f / 60f, null, null);
            camera.Update(1f / 60f, anim, places, 0f);
            float distance = Vector3.Distance(camera.Current.Position, trainer);
            Assert.True(distance > 3.75f, $"frame {i}: the camera came within {distance:F2} of the player's trainer");
        }
        Assert.Equal(ShotKind.SendOut, camera.Kind);
    }

    [Theory]
    [InlineData(1.0f)]
    [InlineData(2.2f)]
    [InlineData(3.5f)]
    public void TheFoesShotsStandInFrontOfThePlayersPlatform(float foeHeight)
    {
        // A big foe is framed with a wider lens, never from among the player's trainer and Pokémon
        var places = new FxPlaces();
        places.Set(BattleSide.Enemy, 0, BattleStage.EnemySpot + new Vector3(0, BattleStage.PlatformHeight, 0), foeHeight);
        foreach (float scale in new[] { 2.1f, 3.0f, 3.4f, 3.6f })
        {
            var shot = BattleCamera.Close(places, BattleSide.Enemy, 0, scale);
            Assert.True(shot.Position.Z < BattleStage.PlayerSpot.Z - 2f, $"×{scale}: the shot stands at z {shot.Position.Z:F2}");
            Assert.InRange(shot.Fov, BattleCamera.OverviewFov, BattleCamera.WidestFov);
            // The framing asked for, as long as the lens can widen enough
            if (shot.Fov < BattleCamera.WidestFov - 0.01f) Assert.Equal(foeHeight * scale, Framed(shot), 2);
        }
    }

    [Fact]
    public void TheCameraCutsAcrossThePlayersPlatform()
    {
        // A move on the nearer of two foes, its target shot and the way back to the overview: whenever the camera
        // goes from behind the player's platform to in front of it (or back), it cuts rather than flying past the
        // player's Pokémon
        var places = new FxPlaces();
        places.Set(BattleSide.Enemy, 0, BattleStage.EnemySpot + new Vector3(-1.2f, BattleStage.PlatformHeight, 0), 1.0f);
        var anim = Settled();
        var camera = new BattleCamera();
        camera.Update(1f / 60f, anim, places, 0f);
        anim.Cue(new EffectCue { Move = "Tackle", Category = MoveCategory.Physical, FromSide = BattleSide.Player, ToSide = BattleSide.Enemy });
        float plane = BattleStage.PlayerSpot.Z;
        var last = camera.Current.Position;
        int crossings = 0;
        for (int i = 0; i < 120; i++)
        {
            anim.Update(1f / 60f, null, null);
            camera.Update(1f / 60f, anim, places, 0f);
            var now = camera.Current.Position;
            if ((last.Z - plane) * (now.Z - plane) < 0f)
            {
                crossings++;
                Assert.Equal(BattleCamera.Direct(anim, places).Shot.Position, now);
            }
            last = now;
        }
        Assert.Equal(2, crossings);
        Assert.Equal(ShotKind.Overview, camera.Kind);
    }

    [Fact]
    public void ACriticalHitPunchesIn()
    {
        var anim = Settled();
        anim.Cue(new EffectCue { Move = "Slash", Category = MoveCategory.Physical, FromSide = BattleSide.Player, ToSide = BattleSide.Enemy, Critical = true });
        Advance(anim, BattleAnimator.ImpactTime - 0.05f);
        var (target, kind, _) = BattleCamera.Direct(anim, Places);
        Assert.Equal(ShotKind.Target, kind);
        Advance(anim, 0.1f);
        var (punch, punchKind, _) = BattleCamera.Direct(anim, Places);
        Assert.Equal(ShotKind.Critical, punchKind);
        Assert.True(Framed(punch) < Framed(target) * 0.8f);
    }

    [Fact]
    public void ASendOutIsShownCloseUp()
    {
        var anim = Settled();
        anim.SendOut(BattleSide.Enemy, new Pokemon(PokemonDatabase.Get("Shinx")!, 5));
        Advance(anim, 0.1f);
        var (shot, kind, _) = BattleCamera.Direct(anim, Places);
        Assert.Equal(ShotKind.SendOut, kind);
        Assert.True(Vector3.Distance(shot.Target, Places.Body(BattleSide.Enemy, 0)) < 0.2f);
    }

    [Fact]
    public void TheCameraFollowsAThrownBallAndClosesInWhileItWobbles()
    {
        var anim = Settled();
        anim.Appear(BattleSide.Enemy, new Pokemon(PokemonDatabase.Get("Starly")!, 3));
        anim.ThrowBall("Poké Ball", 0, 4);
        var seen = new List<ShotKind>();
        for (float t = 0f; t < BattleAnimator.BallThrowTime(4); t += 0.05f)
        {
            var kind = BattleCamera.Direct(anim, Places).Kind;
            if (seen.Count == 0 || seen[^1] != kind) seen.Add(kind);
            Advance(anim, 0.05f);
        }
        Assert.Equal(new[] { ShotKind.Throw, ShotKind.Ball, ShotKind.BallClose }, seen.Take(3));
        // After the click the caught ball stays, and the camera goes back to the overview
        Advance(anim, 0.5f);
        Assert.Equal(ShotKind.Overview, BattleCamera.Direct(anim, Places).Kind);
    }

    [Theory]
    [InlineData(0.8f)]
    [InlineData(2.2f)]
    [InlineData(3.5f)]
    public void TheCameraKeepsAThrownBallInSight(float foeHeight)
    {
        // While the ball flies, the camera stays behind the player's platform and keeps the ball in the frame
        var places = new FxPlaces();
        places.Set(BattleSide.Enemy, 0, BattleStage.EnemySpot + new Vector3(0, BattleStage.PlatformHeight, 0), foeHeight);
        var anim = Settled();
        anim.Appear(BattleSide.Enemy, new Pokemon(PokemonDatabase.Get("Starly")!, 3));
        var camera = new BattleCamera();
        camera.Update(1f / 60f, anim, places, 0f);
        anim.ThrowBall("Poké Ball", 0, 3);
        var to = BattleBall.CaptureOpen(places, 0);
        int seen = 0;
        while (anim.Ball!.Age < BattleAnimator.BallArriveTime)
        {
            anim.Update(1f / 60f, null, null);
            camera.Update(1f / 60f, anim, places, 0f);
            var (phase, p, _) = BattleAnimator.BallStage(anim.Ball.Age, 3);
            if (phase != BallPhase.Flight) continue;
            var shot = camera.Current;
            Assert.True(shot.Position.Z > BattleStage.PlayerSpot.Z, "the camera stays behind the player's platform");
            // Where the ball is on the 16:9 screen, -1..1 across and up
            var ball = BattleBall.Arc(BattleBall.CaptureHand, to, BattleBall.CaptureArc, p) - shot.Position;
            var forward = Vector3.Normalize(shot.Target - shot.Position);
            var right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));
            var up = Vector3.Cross(right, forward);
            float depth = Vector3.Dot(ball, forward), half = MathF.Tan(shot.Fov * MathF.PI / 360f);
            float x = Vector3.Dot(ball, right) / depth / (half * 16f / 9f), y = Vector3.Dot(ball, up) / depth / half;
            Assert.True(depth > 0f && MathF.Abs(x) < 0.95f && MathF.Abs(y) < 0.95f, $"at {p:F2} the ball is at ({x:F2}, {y:F2}) on the screen");
            seen++;
        }
        Assert.True(seen > 30);
    }

    // ------------------------------------------------------------------ the Poké Ball

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(4)]
    public void ABallsThrowGoesThroughItsStepsInOrder(int shakes)
    {
        var order = new List<BallPhase>();
        int wobbles = 0, lastWobble = -1;
        for (float t = 0f; t < BattleAnimator.BallThrowTime(shakes) + 0.3f; t += 0.01f)
        {
            var (phase, progress, wobble) = BattleAnimator.BallStage(t, shakes);
            Assert.InRange(progress, 0f, 1f);
            if (order.Count == 0 || order[^1] != phase) order.Add(phase);
            if (phase == BallPhase.Wobble && wobble != lastWobble) { wobbles++; lastWobble = wobble; }
        }
        Assert.Equal(Math.Min(shakes, 3), wobbles);
        Assert.Equal(new[] { BallPhase.RunIn, BallPhase.Flight, BallPhase.Open, BallPhase.Drop }, order.Take(4));
        Assert.Equal(shakes >= 4 ? BallPhase.Rest : BallPhase.Burst, order[^1]);
        if (shakes >= 4) Assert.Contains(BallPhase.Click, order);
    }

    [Fact]
    public void TheFoeIsDrawnInWhenTheThrownBallArrives()
    {
        var anim = new BattleAnimator();
        var foe = new Pokemon(PokemonDatabase.Get("Starly")!, 3);
        anim.Appear(BattleSide.Enemy, foe);
        anim.ThrowBall("Great Ball", 0, 2);
        Assert.Equal("Great Ball", anim.Ball!.Ball);
        Advance(anim, BattleAnimator.BallArriveTime - 0.05f);
        Assert.True(anim.Enemy.Present);
        Advance(anim, 0.05f + BattleAnimator.CaptureTime + 0.05f);
        Assert.False(anim.Enemy.Present);
        // A ball the Pokémon broke out of is gone once it has played out
        Advance(anim, BattleAnimator.BallThrowTime(2));
        Assert.Null(anim.Ball);
    }

    [Fact]
    public void TheEngineThrowsTheBallFromTheBag()
    {
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 5));
        var inventory = new Inventory();
        inventory.AddItem(ItemDatabase.Get("Poké Ball")!, 1);
        var battle = new BattleEngine(party, new Pokemon(PokemonDatabase.Get("Starly")!, 3) { CurrentHP = 1 }, inventory, new Pokedex(), null, new PcBoxes());
        for (int i = 0; i < 10 && battle.HUD.MenuState == BattleMenuState.Message; i++) battle.ConfirmMessage();
        battle.UseBagItem("Poké Ball");
        battle.ConfirmMessage();
        Assert.NotNull(battle.Anim.Ball);
        Assert.Equal("Poké Ball", battle.Anim.Ball!.Ball);
    }

    [Fact]
    public void BallsLookLikeTheirKind()
    {
        var looks = new[] { "Poké Ball", "Great Ball", "Ultra Ball", "Master Ball", "Premier Ball" }.Select(BattleBall.LookOf).ToList();
        Assert.Equal(looks.Count, looks.Distinct().Count());
        Assert.Equal(BattleBall.LookOf("Poké Ball"), BattleBall.LookOf("Some Ball Nobody Made"));
        Assert.Equal(BallMark.Domes, BattleBall.LookOf("Master Ball").Mark);
    }

    [Fact]
    public void TheBallIsTwoHalvesMeetingAtTheSeamWithTheButtonInFront()
    {
        var look = BattleBall.LookOf("Poké Ball");
        var top = SdfMesher.Mesh(BattleBall.TopModel(look), 1f / 48f);
        var bottom = SdfMesher.Mesh(BattleBall.BottomModel(look), 1f / 48f);
        Assert.True(top.VertexCount > 500 && bottom.VertexCount > 500);
        Assert.All(top.Positions, p => Assert.True(p.Y > -0.02f, $"top half below the seam at {p}"));
        // The bottom stays below the seam except for its button, at the front
        Assert.All(bottom.Positions.Where(p => p.Y > 0.03f), p => Assert.True(p.Z > 0.3f, $"bottom half above the seam at {p}"));
        Assert.Contains(bottom.Positions, p => p.Z > BattleBall.Radius);
        float radius = top.Positions.Concat(bottom.Positions).Where(p => p.Z <= BattleBall.Radius).Max(p => p.Length());
        Assert.InRange(radius, BattleBall.Radius * 0.95f, BattleBall.Radius * 1.12f);
    }

    [Fact]
    public void AThrowArcsAboveBothEnds()
    {
        var from = new Vector3(-8, 1.5f, 16);
        var to = new Vector3(0, 1.5f, 0);
        Assert.Equal(from, BattleBall.Arc(from, to, 2f, 0f));
        Assert.Equal(to, BattleBall.Arc(from, to, 2f, 1f));
        Assert.True(BattleBall.Arc(from, to, 2f, 0.5f).Y > 3f);
    }

    // ------------------------------------------------------------------ arenas

    private static Map Field(TileType under, InteriorStyle interior = InteriorStyle.None)
    {
        var map = new Map(5, 5) { Interior = interior };
        map.SetGroundTile(2, 2, under);
        return map;
    }

    [Theory]
    [InlineData(TileType.Grass, BattleArena.Grass)]
    [InlineData(TileType.TallGrass, BattleArena.Grass)]
    [InlineData(TileType.Water, BattleArena.Water)]
    [InlineData(TileType.Sand, BattleArena.Sand)]
    [InlineData(TileType.Snow, BattleArena.Snow)]
    [InlineData(TileType.CaveFloor, BattleArena.Cave)]
    public void TheGroundUnderThePlayerPicksTheArena(TileType under, BattleArena arena) =>
        Assert.Equal(arena, Field(under).ArenaAt(2, 2));

    [Fact]
    public void MapsCanNameTheirArena()
    {
        Assert.Equal(BattleArena.Indoors, Field(TileType.Floor, InteriorStyle.House).ArenaAt(2, 2));

        var forest = Field(TileType.TallGrass);
        forest.Arena = BattleArena.Forest;
        Assert.Equal(BattleArena.Forest, forest.ArenaAt(2, 2));
        // A pond in the forest is still water
        forest.SetGroundTile(1, 1, TileType.Water);
        Assert.Equal(BattleArena.Water, forest.ArenaAt(1, 1));

        var gym = Field(TileType.Floor, InteriorStyle.House);
        gym.Arena = BattleArena.Gym;
        gym.ArenaType = PokemonType.Rock;
        var spec = ArenaSpec.For(gym, 2, 2);
        Assert.Equal(new ArenaSpec(BattleArena.Gym, PokemonType.Rock, TreeStyle.Round, false), spec);
        Assert.True(spec.Hall);
        Assert.False(spec.Outdoors);
    }

    [Fact]
    public void OnlyGrassArenasGetTheLakeAndOnlyHallsATheme()
    {
        MapDatabase.Initialize();
        var verity = Fixtures.Map("LakeVerity");
        Assert.True(ArenaSpec.For(verity, -1, -1).Lakeside);
        verity.Arena = BattleArena.Forest;
        verity.ArenaType = PokemonType.Grass;
        var forest = ArenaSpec.For(verity, -1, -1);
        Assert.False(forest.Lakeside);
        Assert.Null(forest.Theme);
    }

    [Fact]
    public void MapFilesKeepTheirArena()
    {
        var map = Field(TileType.Floor, InteriorStyle.House);
        map.Arena = BattleArena.League;
        map.ArenaType = PokemonType.Fire;
        var back = MapFile.FromMap(map).ToMap();
        Assert.Equal(BattleArena.League, back.Arena);
        Assert.Equal(PokemonType.Fire, back.ArenaType);

        // Maps that leave it to the ground don't write it
        var plain = MapFile.FromMap(Field(TileType.Grass));
        Assert.Null(plain.BattleArena);
        Assert.Null(plain.ArenaType);
    }

    [Fact]
    public void CavesAndHallsIgnoreTheClockButTheOpenAirFollowsIt()
    {
        foreach (var kind in new[] { BattleArena.Cave, BattleArena.Gym, BattleArena.League })
            Assert.Equal(ArtLook.ArenaRig(new ArenaSpec(kind, PokemonType.Fire), 3f), ArtLook.ArenaRig(new ArenaSpec(kind, PokemonType.Fire), 14f));
        foreach (var kind in new[] { BattleArena.Grass, BattleArena.Forest, BattleArena.Water, BattleArena.Snow, BattleArena.Sand })
            Assert.NotEqual(ArtLook.ArenaRig(new ArenaSpec(kind), 3f), ArtLook.ArenaRig(new ArenaSpec(kind), 14f));
    }

    [Fact]
    public void EachArenaHasItsOwnLight()
    {
        var grass = ArtLook.ArenaRig(new ArenaSpec(BattleArena.Grass), 14f);
        var forest = ArtLook.ArenaRig(new ArenaSpec(BattleArena.Forest), 14f);
        var snow = ArtLook.ArenaRig(new ArenaSpec(BattleArena.Snow), 14f);
        var sand = ArtLook.ArenaRig(new ArenaSpec(BattleArena.Sand), 14f);
        var cave = ArtLook.ArenaRig(new ArenaSpec(BattleArena.Cave), 14f);

        // Under the trees: less sun, more vignette; on snow: light bounced up off the ground; in sand: a warmer sun
        Assert.True(forest.Light.SunColor.X < grass.Light.SunColor.X);
        Assert.True(forest.Post.Vignette > grass.Post.Vignette);
        Assert.True(snow.Light.GroundAmbient.Z > grass.Light.GroundAmbient.Z);
        Assert.True(sand.Light.SunColor.X / sand.Light.SunColor.Z > grass.Light.SunColor.X / grass.Light.SunColor.Z);
        // A cave is darker than any day and its lamps (crystals) are lit
        Assert.True(cave.Light.SkyAmbient.Length() < grass.Light.SkyAmbient.Length());
        Assert.Equal(1f, cave.LampGlow);

        // Each gym's light leans toward its type
        var fire = ArtLook.ArenaRig(new ArenaSpec(BattleArena.Gym, PokemonType.Fire), 14f);
        var water = ArtLook.ArenaRig(new ArenaSpec(BattleArena.Gym, PokemonType.Water), 14f);
        Assert.True(fire.Light.SkyAmbient.X - fire.Light.SkyAmbient.Z > water.Light.SkyAmbient.X - water.Light.SkyAmbient.Z);
        // The League's rooms are darker and more dramatic than a gym
        var league = ArtLook.ArenaRig(new ArenaSpec(BattleArena.League, PokemonType.Fire), 14f);
        Assert.True(league.Post.Vignette > fire.Post.Vignette);
        Assert.True(league.Light.SkyAmbient.Length() < fire.Light.SkyAmbient.Length());
    }

    [Fact]
    public void ArenasHaveSomethingInTheAir()
    {
        foreach (var spec in new[]
        {
            new ArenaSpec(BattleArena.Snow), new ArenaSpec(BattleArena.Sand), new ArenaSpec(BattleArena.Forest), new ArenaSpec(BattleArena.Cave),
            new ArenaSpec(BattleArena.Water), new ArenaSpec(BattleArena.Gym, PokemonType.Fire), new ArenaSpec(BattleArena.League)
        })
        {
            var fx = new FxList();
            ArenaFx.Emit(spec, 3.7f, night: false, fx);
            Assert.True(fx.Count > 0, spec.ToString());
            Assert.All(fx.Quads, q => Assert.True(q.Depth, $"{spec}: ambient particles sit in the scene, behind what stands in front of them"));
        }
    }

    // ------------------------------------------------------------------ the low-HP music (plan 05 · A2)

    [Fact]
    public void ThePlayerIsInDangerWhileTheBarShowsRed()
    {
        var battle = Battle("Piplup", 12, "Bidoof", 8);
        Tick(battle, 4f);
        var piplup = battle.Anim.Player.Shown!;
        Assert.False(battle.PlayerInDanger);

        // The bar drains toward the new HP; the music turns as it reaches the red, not before
        piplup.CurrentHP = (int)(piplup.MaxHP * BattleAnimator.LowHpRatio) - 1;
        Assert.False(battle.PlayerInDanger);
        Tick(battle, 3f);
        Assert.True(battle.PlayerInDanger);

        // Healed, the bar fills and the danger passes
        piplup.CurrentHP = piplup.MaxHP;
        Tick(battle, 3f);
        Assert.False(battle.PlayerInDanger);

        // Fainted is not in danger: there is nothing left to save
        piplup.CurrentHP = 0;
        Tick(battle, 3f);
        Assert.False(battle.PlayerInDanger);
    }
    // ------------------------------------------------------------------ move hints (plan 12 · Q10)

    /// <summary>A wild battle with the hints on or off, its foe seen before it began or not, played to the first choice.</summary>
    private static BattleEngine Hinted(Pokemon mine, Pokemon foe, bool seen = true, bool hints = true)
    {
        var party = new Party();
        party.Add(mine);
        var dex = new Pokedex();
        if (seen) dex.RegisterSeen(foe.Species.DexNumber);
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = dex, WildPokemon = new List<Pokemon> { foe },
            Random = Scenario.Steady(), Rules = Ruleset.Platinum, MoveHints = hints
        });
        Scenario.Settle(battle);
        return battle;
    }

    [Fact]
    public void TheHintIsTheTypeChartsAnswerForEveryPairOfTypes()
    {
        var types = Enum.GetValues<PokemonType>();
        var moves = types.ToDictionary(t => t, t => new Move(MoveDatabase.GetAll().First(m => m.Type == t && m.Category != MoveCategory.Status)));
        foreach (var defending in types)
        {
            var species = PokemonDatabase.GetAll().First(s => s.PrimaryType == defending && s.SecondaryType == null);
            var battle = Hinted(Scenario.Mon("Piplup", 20, "Pound"), Scenario.Mon(species.Name, 20));
            foreach (var attacking in types)
            {
                var expected = MoveHints.Of(TypeChart.GetEffectiveness(attacking, defending, null, Ruleset.Platinum));
                Assert.True(expected == battle.HintFor(moves[attacking], battle.EnemySlots[0]), $"{attacking} against {species.Name}");
            }
        }
        // Two types multiply: Water against Rock and Ground is four times, and still says super effective
        var geodude = Hinted(Scenario.Mon("Piplup", 20, "Bubble"), Scenario.Mon("Geodude", 20));
        Assert.Equal(MoveHint.SuperEffective, geodude.HintFor(geodude.PlayerPokemon.Moves[0], geodude.EnemySlots[0]));
    }

    [Theory]
    [InlineData("Geodude", "Bubble", MoveHint.SuperEffective)]
    [InlineData("Turtwig", "Bubble", MoveHint.NotVeryEffective)]
    [InlineData("Bidoof", "Bubble", MoveHint.Neutral)]
    public void TheHintSaysWhatTheHitThenSounds(string foe, string move, MoveHint hint)
    {
        var battle = Hinted(Scenario.Mon("Piplup", 20, move), Scenario.Mon(foe, 20));
        Assert.Equal(hint, battle.HintFor(battle.PlayerPokemon.Moves[0], battle.EnemySlots[0]));

        int before = battle.Core.Log.Count;
        Scenario.Turn(battle);
        var sounded = battle.Core.Log.Skip(before).SelectMany(e => e is Said said ? said.Shows.Concat(said.OnImpact).Prepend(e) : new[] { e })
            .OfType<HitSounded>().First();
        Assert.Equal(hint == MoveHint.SuperEffective, sounded.SuperEffective);
        Assert.Equal(hint == MoveHint.NotVeryEffective, sounded.NotVeryEffective);
    }

    [Fact]
    public void AStatusMoveANewSpeciesAndHintsTurnedOffShowNothing()
    {
        var battle = Hinted(Scenario.Mon("Piplup", 20, "Bubble", "Growl"), Scenario.Mon("Geodude", 20));
        Assert.Equal(MoveHint.SuperEffective, battle.HintFor(battle.PlayerPokemon.Moves[0], battle.EnemySlots[0]));
        Assert.Equal(MoveHint.None, battle.HintFor(battle.PlayerPokemon.Moves[1], battle.EnemySlots[0]));

        // Met for the first time: the battle registers it as seen, but it was new when the battle began
        var unseen = Hinted(Scenario.Mon("Piplup", 20, "Bubble"), Scenario.Mon("Geodude", 20), seen: false);
        Assert.True(unseen.Pokedex.IsSeen(PokemonDatabase.Get("Geodude")!.DexNumber));
        Assert.Equal(MoveHint.None, unseen.HintFor(unseen.PlayerPokemon.Moves[0], unseen.EnemySlots[0]));

        var off = Hinted(Scenario.Mon("Piplup", 20, "Bubble"), Scenario.Mon("Geodude", 20), hints: false);
        Assert.False(off.ShowsMoveHints);
        Assert.Equal(MoveHint.None, off.HintFor(off.PlayerPokemon.Moves[0], off.EnemySlots[0]));
    }

    [Fact]
    public void UnderRulesAPlatinumGameShowsNoHintsAndAModernOneDoes()
    {
        Assert.Equal(RulesDefault.Rules, new GameSettings().MoveHints);
        Assert.False(RulesDefault.Rules.Holds(RulesPreset.Platinum));
        Assert.True(RulesDefault.Rules.Holds(RulesPreset.Modern));
        Assert.False(RulesDefault.Off.Holds(RulesPreset.Modern));
        Assert.True(RulesDefault.On.Holds(RulesPreset.Platinum));

        // The row goes round its three values
        var settings = new GameSettings();
        OptionsScreen.Change(settings, OptionRow.MoveHints, 1);
        Assert.Equal(RulesDefault.On, settings.MoveHints);
        OptionsScreen.Change(settings, OptionRow.MoveHints, 1);
        Assert.Equal(RulesDefault.Off, settings.MoveHints);
    }

    [Fact]
    public void TheHelpPagesOpenFromTheOptionsAndWalkTheChart()
    {
        var options = new OptionsScreen();
        options.Open();
        while (Enum.GetValues<OptionRow>()[options.SelectedIndex] != OptionRow.Help) options.Move(1);
        options.Confirm();
        var help = options.Help;
        Assert.True(help.IsActive);
        Assert.Equal(HelpPage.Types, help.Page);
        Assert.Equal("Super effective", help.Describe().Headline);

        // Fire against Water, then up to the top row and onto the tabs, across to the controls and back down
        help.Move(-1, 0);
        Assert.Equal("Not very effective", help.Describe().Headline);
        for (int i = 0; i < 2; i++) help.Move(0, -1);
        Assert.True(help.OnTabs);
        help.Move(1, 0);
        Assert.Equal(HelpPage.Controls, help.Page);
        help.Move(-1, 0);
        help.Move(0, 1);
        Assert.False(help.OnTabs);
        Assert.Equal(PokemonType.Normal, HelpScreen.Types[help.Attack]);

        // Every cell is the chart's answer, Normal against Ghost has no effect
        Assert.Equal(MoveHint.NoEffect, HelpScreen.Hint(PokemonType.Normal, PokemonType.Ghost));
        help.Cancel();
        Assert.False(help.IsActive);
        Assert.True(options.IsActive);
    }

    [Fact]
    public void TheControlsPageListsEveryActionsBinding()
    {
        var actions = Enum.GetValues<GameAction>();
        Assert.Equal(actions.Length, InputManager.Bindings.Length);
        for (int i = 0; i < actions.Length; i++)
        {
            Assert.Equal(actions[i], InputManager.Bindings[i].Action);
            Assert.NotEmpty(InputManager.Bindings[i].Keys);
        }
    }
}
