using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 45 · BC4: a manner's push in on the user and pull out past the target, and its reaction to a super-effective or
/// critical blow; every shot clear of the player's platform, and no line read a moment later for any of it.
/// </summary>
public class BattleCameraPushTests
{
    private static CameraManner Galar => CameraManners.Find("galar")!;

    private static readonly Vector3 Platform = BattleStage.PlayerSpot + new Vector3(0, 1.2f, 0);

    private static (BattleAnimator Anim, FxPlaces Places) Battle(int slots, float foeHeight, float ownHeight)
    {
        var anim = new BattleAnimator();
        if (slots > 1) anim.SetSlots(slots, slots);
        var places = new FxPlaces();
        for (int slot = 0; slot < slots; slot++)
        {
            places.Set(BattleSide.Enemy, slot, BattlePlaces.Spot(BattleSide.Enemy, slot, slots) + Vector3.UnitY * BattleStage.PlatformHeight, foeHeight * BattlePlaces.Fit(slots));
            places.Set(BattleSide.Player, slot, BattlePlaces.Spot(BattleSide.Player, slot, slots) + Vector3.UnitY * BattleStage.PlatformHeight, ownHeight * BattlePlaces.Fit(slots));
            anim[BattleSide.Enemy, slot].Present = true;
            anim[BattleSide.Player, slot].Present = true;
        }
        Advance(anim, 2f);
        return (anim, places);
    }

    private static void Advance(BattleAnimator anim, float seconds)
    {
        for (float t = 0f; t < seconds - 1e-4f; t += 1f / 60f) anim.Update(1f / 60f, null, null);
    }

    [Fact]
    public void AMannerThatDoesNotPushFilmsAMoveAsClassicDoes()
    {
        var (anim, places) = Battle(1, 1.4f, 1.2f);
        anim.Cue(new EffectCue { Move = "Tackle", FromSide = BattleSide.Player, ToSide = BattleSide.Enemy, SuperEffective = true });
        for (int i = 0; i < 100; i++)
        {
            var classic = BattleCamera.Direct(anim, places, false, CameraManners.Classic);
            Assert.Equal(classic, BattleCamera.Direct(anim, places, false, CameraManners.Sinnoh));
            Assert.NotEqual(ShotKind.React, classic.Kind);
            anim.Update(1f / 60f, null, null);
        }
    }

    [Fact]
    public void GalarPushesInOnTheUserAndPullsOutPastTheTarget()
    {
        var (anim, places) = Battle(1, 1.4f, 1.2f);
        var cue = anim.Cue(new EffectCue { Move = "Tackle", FromSide = BattleSide.Enemy, ToSide = BattleSide.Player });
        var user = places.Body(BattleSide.Enemy, 0);
        float early = Vector3.Distance(BattleCamera.Direct(anim, places, false, Galar).Shot.Position, user);
        Advance(anim, 0.2f);
        var (wound, kind, _) = BattleCamera.Direct(anim, places, false, Galar);
        Assert.Equal(ShotKind.Attacker, kind);
        Assert.True(Vector3.Distance(wound.Position, user) < early - 0.3f, "the attacker's shot came in over the wind-up");

        while (cue.Age < BattleAnimator.ImpactTime + 0.02f) anim.Update(1f / 60f, null, null);
        var target = places.Body(BattleSide.Player, 0);
        float tight = Vector3.Distance(BattleCamera.Direct(anim, places, false, Galar).Shot.Position, target);
        while (cue.Age < BattleCamera.MoveShotEnd - 0.05f) anim.Update(1f / 60f, null, null);
        var (late, lateKind, _) = BattleCamera.Direct(anim, places, false, Galar);
        Assert.Equal(ShotKind.Target, lateKind);
        Assert.True(Vector3.Distance(late.Position, target) > tight + 0.3f, "the target's shot pulled out by the move's end");
    }

    public static IEnumerable<object[]> Shapes()
    {
        foreach (int slots in new[] { 1, 2 })
            foreach (var (foeH, ownH) in new[] { (0.4f, 0.4f), (0.8f, 0.8f), (1.4f, 1.2f), (2.4f, 2.0f) })
                yield return new object[] { slots, foeH, ownH };
    }

    [Theory]
    [MemberData(nameof(Shapes))]
    public void EveryPushedShotAndReactionKeepsClearOfThePlayersPlatform(int slots, float foeH, float ownH)
    {
        foreach (var (from, to) in new[] { (BattleSide.Player, BattleSide.Enemy), (BattleSide.Enemy, BattleSide.Player) })
            foreach (var category in new[] { MoveCategory.Physical, MoveCategory.Special, MoveCategory.Status })
                for (int slot = 0; slot < slots; slot++)
                {
                    var (anim, places) = Battle(slots, foeH, ownH);
                    anim.Cue(new EffectCue { Move = "Tackle", Category = category, FromSide = from, FromSlot = slot, ToSide = to, ToSlot = slots - 1 - slot, Critical = true });
                    var kinds = new HashSet<ShotKind>();
                    for (int i = 0; i < 110; i++)
                    {
                        var (shot, kind, _) = BattleCamera.Direct(anim, places, false, Galar);
                        kinds.Add(kind);
                        if (kind is ShotKind.Attacker or ShotKind.Target or ShotKind.React)
                            Assert.True(Vector3.Distance(shot.Position, Platform) > BattleCamera.Clearance,
                                $"{category} {from} {slot} of {slots} ({foeH}, {ownH}): {kind} at {anim.Time:F2} within {Vector3.Distance(shot.Position, Platform):F2} of the platform");
                        anim.Update(1f / 60f, null, null);
                    }
                    if (category != MoveCategory.Status) Assert.Contains(ShotKind.React, kinds);
                }
    }

    [Fact]
    public void AReactionHoldsTheOneHitOnceTheMovesShotsAreOverAndGivesWayToTheNextMove()
    {
        var (anim, places) = Battle(1, 1.4f, 1.2f);
        var cue = anim.Cue(new EffectCue { Move = "Tackle", FromSide = BattleSide.Player, ToSide = BattleSide.Enemy, SuperEffective = true });
        float start = anim.Time;
        while (anim.Time < start + BattleCamera.MoveShotEnd + 0.1f) anim.Update(1f / 60f, null, null);
        Assert.Equal(ShotKind.React, BattleCamera.Direct(anim, places, false, Galar).Kind);
        Assert.Equal(ShotKind.Overview, BattleCamera.Direct(anim, places, false, CameraManners.Classic).Kind);
        // Over once its time is up
        while (anim.Time < start + BattleCamera.MoveShotEnd + BattleCamera.ReactTime + 0.02f) anim.Update(1f / 60f, null, null);
        Assert.NotEqual(ShotKind.React, BattleCamera.Direct(anim, places, false, Galar).Kind);

        // A plain hit has none
        var (plain, plainPlaces) = Battle(1, 1.4f, 1.2f);
        plain.Cue(new EffectCue { Move = "Tackle", FromSide = BattleSide.Player, ToSide = BattleSide.Enemy });
        Advance(plain, BattleCamera.MoveShotEnd + 0.1f);
        Assert.NotEqual(ShotKind.React, BattleCamera.Direct(plain, plainPlaces, false, Galar).Kind);

        // Another move begun has the camera at once
        var (next, nextPlaces) = Battle(1, 1.4f, 1.2f);
        next.Cue(new EffectCue { Move = "Tackle", FromSide = BattleSide.Player, ToSide = BattleSide.Enemy, Critical = true });
        Advance(next, BattleCamera.MoveShotEnd + 0.1f);
        Assert.Equal(ShotKind.React, BattleCamera.Direct(next, nextPlaces, false, Galar).Kind);
        next.Cue(new EffectCue { Move = "Tackle", FromSide = BattleSide.Enemy, ToSide = BattleSide.Player });
        Assert.Equal(ShotKind.Attacker, BattleCamera.Direct(next, nextPlaces, false, Galar).Kind);

        // And the menu is the menu's
        var (menu, menuPlaces) = Battle(1, 1.4f, 1.2f);
        menu.Cue(new EffectCue { Move = "Tackle", FromSide = BattleSide.Player, ToSide = BattleSide.Enemy, SuperEffective = true });
        Advance(menu, BattleCamera.MoveShotEnd + 0.1f);
        menu.NoteMenu(true);
        Assert.NotEqual(ShotKind.React, BattleCamera.Direct(menu, menuPlaces, true, Galar).Kind);
    }

    /// <summary>A battle read on as a player would (a line every half second, the first move at each menu), with or without the camera asked every frame.</summary>
    private static List<(int Frame, string Line)> Read(bool filmed)
    {
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Chimchar")!, 30, new Random(3)));
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(),
            WildPokemon = new List<Pokemon> { new Pokemon(PokemonDatabase.Get("Turtwig")!, 12, new Random(4)) }, Random = new Random(7)
        });
        var places = new FxPlaces();
        for (int side = 0; side < 2; side++)
            places.Set((BattleSide)side, 0, (side == 0 ? BattleStage.PlayerSpot : BattleStage.EnemySpot) + Vector3.UnitY * BattleStage.PlatformHeight, 1.2f);
        var lines = new List<(int, string)>();
        string last = "";
        int shown = 0;
        for (int frame = 0; frame < 60 * 40 && !battle.IsBattleOver; frame++)
        {
            battle.Update(1f / 60f);
            if (filmed)
            {
                bool choosing = battle.HUD.MenuState != BattleMenuState.Message;
                BattleCamera.Direct(battle.Anim, places, choosing, Galar);
                BattleCamera.SplitNow(battle.Anim, places, Galar);
            }
            if (battle.CurrentMessage != last) { last = battle.CurrentMessage; lines.Add((frame, last)); shown = 0; }
            shown++;
            if (battle.HUD.MenuState == BattleMenuState.Main && shown > 30) { battle.SelectMove(0); shown = 0; }
            else if (battle.HUD.MenuState == BattleMenuState.Message && shown > 30) { battle.ConfirmMessage(); shown = 0; }
        }
        return lines;
    }

    [Fact]
    public void NoLineIsReadALaterMomentForTheCamera()
    {
        var plain = Read(false);
        var filmed = Read(true);
        Assert.True(plain.Count > 8, "the battle was read through a turn or more");
        Assert.Equal(plain, filmed);
    }
}
