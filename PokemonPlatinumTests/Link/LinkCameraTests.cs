using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Graphics;
using Xunit;

namespace PokemonPlatinumTests.Link;

/// <summary>
/// Plan 45 · BC7: link battles are filmed on each screen by that player's own options, from their own side, at the same
/// turn of the same log. The camera is the face's alone, so one screen may film in Galar's manner and the other as
/// Classic and what they see is the same battle.
/// </summary>
[Collection("Shared clocks and dice")]
public class LinkCameraTests
{
    private static (LinkBench Bench, BattleEngine A, BattleEngine B) ScreensAtTheCommands(uint seed)
    {
        var bench = new LinkBench(seed, autoAnswerB: false);
        bench.A.AnswersByItself = false;
        bench.Settle();
        var a = BattleEngine.ForLink(bench.A);
        var b = BattleEngine.ForLink(bench.B);
        for (int i = 0; i < 4000 && !(a.HUD.MenuState == BattleMenuState.Main && b.HUD.MenuState == BattleMenuState.Main); i++)
        {
            foreach (var face in new[] { a, b })
            {
                face.Update(0.05f);
                if (face.IsWaitingForConfirm && !face.IsLearningMove) face.ConfirmMessage();
            }
            bench.Server.Pump();
        }
        Assert.Equal(BattleMenuState.Main, a.HUD.MenuState);
        Assert.Equal(BattleMenuState.Main, b.HUD.MenuState);
        return (bench, a, b);
    }

    private static FxPlaces PlacesOf(BattleAnimator anim)
    {
        var places = new FxPlaces();
        foreach (var side in new[] { BattleSide.Player, BattleSide.Enemy })
            for (int slot = 0; slot < anim.SlotsOf(side); slot++)
                places.Set(side, slot, BattlePlaces.Spot(side, slot, anim.SlotsOf(side), anim.Spread(side)) + Vector3.UnitY * BattleStage.PlatformHeight,
                    1.2f * BattlePlaces.Fit(anim.SlotsOf(side)));
        return places;
    }

    [Fact]
    public void TwoLinkPlayersFilmTheirOwnSidesAtTheSameTurnEachByTheirOwnOptions()
    {
        var galar = CameraManners.Find("galar")!;
        var (bench, a, b) = ScreensAtTheCommands(7);
        // The same turn of the same log: both commands open, both clocks stepped in lockstep from here
        // Each screen stands on its own side: its own Pokémon are the player's on its platform
        Assert.Contains(a.PlayerPokemon, bench.A.Own);
        Assert.Contains(b.PlayerPokemon, bench.B.Own);
        Assert.DoesNotContain(a.PlayerPokemon, bench.B.Own);

        var placesA = PlacesOf(a.Anim);
        var placesB = PlacesOf(b.Anim);
        // Five seconds into the menu: A's player films in Galar's manner and is in its cycle, B's as Classic and is on the overview
        for (int i = 0; i < 400 && (a.Anim.MenuAge < 5f || b.Anim.MenuAge < 5f); i++)
        {
            a.Update(0.05f);
            b.Update(0.05f);
            bench.Server.Pump();
        }
        Assert.Equal(BattleMenuState.Main, a.HUD.MenuState);
        Assert.Equal(BattleMenuState.Main, b.HUD.MenuState);
        Assert.True(a.Anim.MenuAge >= 5f && b.Anim.MenuAge >= 5f, $"{a.Anim.MenuAge} {b.Anim.MenuAge}");
        var (shotA, kindA, _) = BattleCamera.Direct(a.Anim, placesA, true, galar);
        var (shotB, kindB, _) = BattleCamera.Direct(b.Anim, placesB, true, CameraManners.Classic);
        Assert.Equal(ShotKind.Idle, kindA);
        Assert.Equal(ShotKind.Overview, kindB);
        Assert.Equal(BattleCamera.Overview(b.Anim.Time), shotB);
        Assert.NotEqual(BattleCamera.Overview(a.Anim.Time).Position, shotA.Position);

        // The other way round, the options being each player's own: the same turn, the manners swapped
        var (swapA, swapKindA, _) = BattleCamera.Direct(a.Anim, placesA, true, CameraManners.Classic);
        var (swapB, swapKindB, _) = BattleCamera.Direct(b.Anim, placesB, true, galar);
        Assert.Equal(ShotKind.Overview, swapKindA);
        Assert.Equal(ShotKind.Idle, swapKindB);
        // Each one's idle shot frames its own foe wholly in the picture's free part
        var panels = IdleFrame.Panels(1, 1);
        Assert.True(IdleFrame.BoxOf(shotA, placesA, BattleSide.Enemy, 0) is { } boxA && IdleFrame.Holds(boxA, panels));
        Assert.True(IdleFrame.BoxOf(swapB, placesB, BattleSide.Enemy, 0) is { } boxB && IdleFrame.Holds(boxB, panels));
        Assert.Equal(BattleCamera.Overview(a.Anim.Time), swapA);
    }

    [Fact]
    public void ALinkBattlesSplitIsOnTheScreenOfTheOneWhoseMannerSplitsAndTheOthersIsUntouched()
    {
        var galar = CameraManners.Find("galar")!;
        var (bench, a, b) = ScreensAtTheCommands(11);
        for (int i = 0; i < 80; i++)
        {
            a.Update(0.05f);
            b.Update(0.05f);
            bench.Server.Pump();
        }
        // Both screens noted the leads' moment (a link battle is a trainer's battle) whatever their manner
        Assert.True(a.Anim.SplitMoments.Count(m => m.Reason == SplitReason.Leads) == 1, $"A at {a.Anim.Time}: leads may divide {a.Anim.LeadsMayDivide}, {a.Anim.SplitMoments.Count} moments");
        Assert.True(b.Anim.SplitMoments.Count(m => m.Reason == SplitReason.Leads) == 1, $"B at {b.Anim.Time}: leads may divide {b.Anim.LeadsMayDivide}, {b.Anim.SplitMoments.Count} moments");
        var moment = a.Anim.SplitMoments[0];
        var places = PlacesOf(a.Anim);
        // ...filmed by the one with Galar's manner and by nobody under Classic
        Assert.True(BattleCamera.SplitsAt(galar, moment));
        Assert.False(BattleCamera.SplitsAt(CameraManners.Classic, moment));
        // and each half frames the screen's own sides: the foe's half the foe, the player's the screen's own Pokémon
        var split = BattleCamera.SplitOf(a.Anim, places, galar, 1f);
        Assert.False(split.Royal);
        var panels = IdleFrame.Panels(1, 1);
        Assert.True(IdleFrame.Holds(IdleFrame.BoxOf(split.Foe, places, BattleSide.Enemy, 0)!.Value, panels));
        Assert.True(IdleFrame.Holds(IdleFrame.BoxOf(split.Own, places, BattleSide.Player, 0)!.Value, panels));    }
}
