using System;
using System.Numerics;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>Photo mode in battle (plan 12 · Q17): a held shot stops the director, the free camera's bounds, a stepped pose.</summary>
public class BattlePhotoModeTests
{
    private static readonly FxPlaces Places = new();

    private static BattleAnimator Settled()
    {
        var anim = new BattleAnimator();
        anim.Update(2f, null, null);
        return anim;
    }

    private static BattleAnimator Critical()
    {
        var anim = Settled();
        anim.Cue(new EffectCue { Move = "Slash", Category = MoveCategory.Physical, FromSide = BattleSide.Player, ToSide = BattleSide.Enemy, Critical = true });
        for (float t = 0f; t < BattleAnimator.ImpactTime + 0.05f; t += 1f / 60f) anim.Update(1f / 60f, null, null);
        return anim;
    }

    [Fact]
    public void HoldingAShotStopsTheDirector()
    {
        var director = new BattleCamera();
        var anim = Critical();
        // The director would punch in on the critical hit and shake the lens
        Assert.Equal(ShotKind.Critical, BattleCamera.Direct(anim, Places).Kind);
        var held = BattleCamera.Overview(0f) with { Position = new Vector3(-1f, 4f, 24f) };
        director.Hold(held);
        for (int i = 0; i < 30; i++)
        {
            var camera = director.Update(1f / 60f, anim, Places, 2f);
            Assert.Equal(held.Position, camera.Position);
            Assert.Equal(held.Target, camera.Target);
            Assert.Equal(held.Fov, camera.FovY);
            Assert.Equal(ShotKind.Held, director.Kind);
        }
        // Let go, it eases from the held shot toward what it wants rather than jumping there (from this side of the
        // player's platform; a held shot from beyond it is cut from, as any move across the platform is)
        director.LetGo();
        var after = director.Update(1f / 60f, Settled(), Places, 0f);
        Assert.NotEqual(BattleCamera.Overview(2f).Position, after.Position);
        Assert.NotEqual(ShotKind.Held, director.Kind);
    }

    [Fact]
    public void APhotoBeginsOnTheShotItWasGivenAndLooksWhereItLooked()
    {
        var start = BattleCamera.Overview(0f);
        var p = new BattlePhotoView(start);
        // The overview looks under the ground 20 units ahead: it is turned round the point of its line nearest the field's middle
        Assert.True(Vector3.Distance(p.Pivot, BattlePhotoView.FieldMiddle) < BattlePhotoView.NearTheMiddle);
        var along = Vector3.Normalize(start.Target - start.Position);
        Assert.True(Vector3.Dot(Vector3.Normalize(p.Shot.Target - p.Shot.Position), along) > 0.9999f);
        Assert.Equal(start.Fov, p.Shot.Fov);
        Assert.True(Vector3.Distance(start.Position, p.Shot.Position) < 1e-3f);
        // A shot of a Pokémon on its platform is turned round what it looks at
        var close = BattleCamera.Frame(BattleStage.EnemySpot + Vector3.UnitY, 3f);
        Assert.Equal(close.Target, new BattlePhotoView(close).Pivot);
        // Turned and brought closer, it still looks at the same point with the same lens
        p.Turn(new Vector2(1f, -1f), 0.7f);
        p.ZoomBy(2);
        Assert.Equal(p.Pivot, p.Shot.Target);
        Assert.Equal(start.Fov, p.Shot.Fov);
        Assert.NotEqual(start.Position, p.Shot.Position);
    }

    [Fact]
    public void TheFreeCameraGoesRoundButNeverUnderTheGroundNorOverTheTop()
    {
        var p = new BattlePhotoView(BattleCamera.Overview(0f));
        // All the way round the vertical, behind the player's platform included
        for (int i = 0; i < 60; i++) p.Turn(new Vector2(1f, 0f), 0.1f);
        Assert.InRange(p.Yaw, -MathF.PI, MathF.PI);
        for (int i = 0; i < 100; i++) p.Turn(new Vector2(0f, 1f), 0.5f);
        Assert.Equal(BattlePhotoView.LowestPitch, p.Pitch, 4);
        Assert.True(p.Shot.Position.Y >= BattlePhotoView.LowestHeight);
        for (int i = 0; i < 100; i++) p.Turn(new Vector2(0f, -1f), 0.5f);
        Assert.Equal(BattlePhotoView.HighestPitch, p.Pitch, 4);
        // And straight back down again from the top, with nothing lost at the bound
        p.Turn(new Vector2(0f, 1f), 0.1f);
        Assert.True(p.Pitch < BattlePhotoView.HighestPitch);
    }

    [Fact]
    public void TheZoomStaysWithinItsBoundsAndStepsBack()
    {
        var p = new BattlePhotoView(BattleCamera.Overview(0f));
        for (int i = 0; i < 20; i++) p.ZoomBy(1);
        Assert.Equal(BattlePhotoView.TightestZoom, p.Zoom);
        for (int i = 0; i < 20; i++) p.ZoomBy(-1);
        Assert.Equal(BattlePhotoView.WidestZoom, p.Zoom);
        var q = new BattlePhotoView(BattleCamera.Overview(0f));
        q.ZoomBy(3);
        q.ZoomBy(-3);
        Assert.Equal(1f, q.Zoom);
    }

    [Fact]
    public void ASteppedPoseIsAFunctionOfItsTime()
    {
        var p = new BattlePhotoView(BattleCamera.Overview(0f));
        while (p.Row != BattlePhotoRow.Pose) p.MoveRow(1);
        Assert.Equal(0f, p.PoseStep);
        Assert.Equal(5f + 0.37f, p.PoseTime(5f, 0.37f));
        p.Change(3);
        Assert.Equal(3, p.Pose);
        Assert.Equal(0.3f, p.PoseStep, 5);
        Assert.Equal(5f + 0.37f + 0.3f, p.PoseTime(5f, 0.37f), 5);
        // The same step at the same time is the same pose, however it was come to
        var q = new BattlePhotoView(BattleCamera.Overview(0f));
        while (q.Row != BattlePhotoRow.Pose) q.MoveRow(1);
        q.Change(-(BattlePhotoView.PoseSteps - 3));
        Assert.Equal(p.PoseTime(5f, 0.37f), q.PoseTime(5f, 0.37f));
        // And round again after the last
        for (int i = 0; i < BattlePhotoView.PoseSteps; i++) q.Change(1);
        Assert.Equal(3, q.Pose);
    }

    [Fact]
    public void ABorrowedHourIsTheFieldsOwn()
    {
        var p = new BattlePhotoView(BattleCamera.Overview(0f));
        while (p.Row != BattlePhotoRow.Time) p.MoveRow(1);
        Assert.Null(p.Hour);
        foreach (var period in new[] { TimeOfDay.Morning, TimeOfDay.Day, TimeOfDay.Twilight, TimeOfDay.Night, TimeOfDay.LateNight })
        {
            p.Change(1);
            Assert.Equal(period, p.Time);
            Assert.Equal(PhotoView.HourOf(period), p.Hour);
            Assert.Equal(period, GameClock.ForHour((int)p.Hour!.Value));
        }
        p.Change(1);
        Assert.Null(p.Time);
    }

    [Fact]
    public void ThePanelSaysEveryRowInEnglishAndRomanian()
    {
        var p = new BattlePhotoView(BattleCamera.Overview(0f));
        foreach (var row in Enum.GetValues<BattlePhotoRow>())
        {
            Assert.False(string.IsNullOrWhiteSpace(ModernUi.BattlePhotoValue(p, row)));
            Assert.DoesNotContain("photo.", ModernUi.BattlePhotoValue(p, row));
        }
        while (p.Row != BattlePhotoRow.Pose) p.MoveRow(1);
        p.Change(4);
        Assert.Equal("+0.4 s", ModernUi.BattlePhotoValue(p, BattlePhotoRow.Pose));
    }

    [Fact]
    public void APhotoOfABattleTouchesNothingOfTheRules()
    {
        // The engine's photo in battle reads keys and holds the camera; it never reaches the battle's own members
        string source = System.IO.File.ReadAllText(System.IO.Path.Combine(Repository.Root, "PokemonPlatinumEngine", "Core", "GameEngine.BattlePhoto.cs"));
        foreach (string reach in new[] { "Battle.Update", "Battle.Core", "Submit", "ConfirmMessage", "Select", "Anim." })
            Assert.DoesNotContain("." + reach.TrimStart('.'), source.Replace("flow.Battle == null", "").Replace("flow.Battle, VirtualWidth", ""));
    }
}
