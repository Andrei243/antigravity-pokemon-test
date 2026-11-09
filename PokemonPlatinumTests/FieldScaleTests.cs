using System.Numerics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;
using Stance = PokemonPlatinumEngine.Overworld.Stance;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 30 · FM6: scale in a region whose people are grown (style guide, "Scale in a grown region"): Pokémon in the
/// field at a grown region's size and their cards, the region's own camera, the grown sitter's seat, the motion's
/// velocity, a grown trainer in battle and a grown bust; and that none of it reaches a chibi region.
/// </summary>
public class FieldScaleTests
{
    // The grown kit's teen, the player's build: a crown of 1.485 model units, about 1.5 tiles with the hair
    private const float GrownTeen = 1.485f;

    [Fact]
    public void PokemonKeepTheirOrderOfSizesAtAGrownRegionsScale()
    {
        // A Machop stands under the grown player, a Steelix far over, and every species' order by height is the same
        // at either scale (both rise with the metres; the clamps only tie)
        Assert.True(FieldSprites.GrownHeight(FieldSprites.MetresOf("Machop")) < GrownTeen * 0.6f);
        Assert.True(FieldSprites.GrownHeight(FieldSprites.MetresOf("Steelix")) > GrownTeen * 2f);
        Assert.Equal(3.5f, FieldSprites.GrownHeight(FieldSprites.MetresOf("Wailord")));
        var species = new[] { "Pichu", "Machop", "Pikachu", "Bidoof", "Machoke", "Machamp", "Onix", "Steelix", "Gyarados", "Wailord" }
            .OrderBy(FieldSprites.MetresOf).ToList();
        for (int i = 1; i < species.Count; i++)
        {
            Assert.True(FieldSprites.FieldHeight(FieldSprites.MetresOf(species[i])) >= FieldSprites.FieldHeight(FieldSprites.MetresOf(species[i - 1])));
            Assert.True(FieldSprites.GrownHeight(FieldSprites.MetresOf(species[i])) >= FieldSprites.GrownHeight(FieldSprites.MetresOf(species[i - 1])));
        }
        Assert.Equal(0.35f, FieldSprites.GrownHeight(0.1f));
        Assert.Equal(0.85f * 1.2f, FieldSprites.GrownHeight(1.2f), 4);
    }

    [Fact]
    public void AChibiHeightTakesTheCardItAlwaysHadAndAGrownOneACardThatHoldsIt()
    {
        for (float h = 0.4f; h <= 1.9f; h += 0.01f)
        {
            int was = h < 0.7f ? FieldSprites.Small : h <= 1.6f ? FieldSprites.Medium : FieldSprites.Large;
            Assert.Equal(was, FieldSprites.ClassOf(h));
        }
        for (float m = 0f; m <= 20f; m += 0.05f)
        {
            float h = FieldSprites.GrownHeight(m);
            int size = FieldSprites.GrownClassOf(h);
            // The card less its foot and outline holds the height
            Assert.True((size - FieldSprites.FootRows - 1f) / CharacterSprites.TexelsPerUnit >= h - 0.02f, $"{m} m: {h} tiles on {size}");
        }
        Assert.Equal(FieldSprites.Huge, FieldSprites.ClassOf(2.5f));
        Assert.Equal(FieldSprites.Giant, FieldSprites.ClassOf(3.5f));
        Assert.Equal(FieldSprites.Small, FieldSprites.GrownClassOf(0.5f));
        Assert.Equal(FieldSprites.Large, FieldSprites.GrownClassOf(1.5f));
        Assert.Equal(FieldSprites.Giant, FieldSprites.GrownClassOf(3.5f));
    }

    [Fact]
    public void AGrownNameIsTheSpeciesMarkedAndMeasuredAtTheGrownScale()
    {
        string grown = FieldSprites.GrownName("Machop");
        Assert.True(FieldSprites.IsGrown(grown));
        Assert.False(FieldSprites.IsGrown("Machop"));
        Assert.Equal(grown, FieldSprites.GrownName(grown));
        Assert.Equal("Machop", FieldSprites.ModelOf(grown));
        Assert.Equal("Machop", FieldSprites.ModelOf("Machop"));
        Assert.Equal(FieldSprites.MetresOf("Machop"), FieldSprites.MetresOf(grown));
        Assert.Equal(FieldSprites.GrownHeight(0.8f), FieldSprites.HeightOf(grown));
        Assert.Equal(FieldSprites.FieldHeight(0.8f), FieldSprites.HeightOf("Machop"));
        // (a shiny or female look too)
        Assert.Equal("Pikachu+f+s", FieldSprites.ModelOf(FieldSprites.GrownName("Pikachu+f+s")));
    }

    [Fact]
    public void TheKalosCameraIsItsOwnRowAndEveryOtherCameraIsAsItWas()
    {
        var map = new Map(9, 9) { Name = "Lab" };
        Assert.Equal(MapScene.OutdoorPitchDeg, MapScene.ViewOf(map).PitchDeg);
        Assert.Equal(new FieldView(MapScene.OutdoorPitchDeg, MapScene.OutdoorFovYDeg, MapScene.OutdoorDistance), MapScene.ViewOf(map));
        map.Camera = FieldCamera.Kalos;
        Assert.Equal(new FieldView(50f, 20f, 520f / 16f), MapScene.ViewOf(map));
        // The straightening holds at any pitch: the vertical scale follows it
        Assert.Equal(1f / MathF.Cos(50f * MathF.PI / 180f), MapScene.VerticalScaleOf(map), 5);
        Assert.Equal(FieldCamera.Kalos, WorldMapBuilder.CameraOf("Kalos"));
    }

    [Fact]
    public void OnlyARegionWhoseKitIsGrownTakesTheKalosCameraAndOnlyOutdoorsWhereNoneIsNamed()
    {
        foreach (var region in RegionDatabase.All)
        {
            var camera = WorldMapBuilder.GrownCamera(FieldCamera.Default, region.Id, outdoors: true);
            Assert.Equal(region.Kit == CharacterKit.Grown ? FieldCamera.Kalos : FieldCamera.Default, camera);
            // A room, a cave and a camera of the place's own keep theirs, grown or not
            Assert.Equal(FieldCamera.Default, WorldMapBuilder.GrownCamera(FieldCamera.Default, region.Id, outdoors: false));
            Assert.Equal(FieldCamera.Cave, WorldMapBuilder.GrownCamera(FieldCamera.Cave, region.Id, outdoors: true));
            Assert.Equal(FieldCamera.ZoomedIn, WorldMapBuilder.GrownCamera(FieldCamera.ZoomedIn, region.Id, outdoors: true));
        }
        // The chibi regions of today all keep Platinum's
        foreach (string chibi in new[] { "Sinnoh", "Kanto", "Johto", "Hoenn", "Unova", "Kalos", "Alola" })
            Assert.Equal(FieldCamera.Default, WorldMapBuilder.GrownCamera(FieldCamera.Default, chibi, outdoors: true));
    }

    [Fact]
    public void AGrownSitterSitsWithTheFeetOnTheFloorBeforeTheSeat()
    {
        var stood = new Vector3(5.5f, 1f, 7.5f);
        // The teen's own seat at a scale of one: 0.45 of the hip joints' height
        float seat = GrownAnimation.SeatShare * 0.76f;
        var onChair = FieldRigChoice.GrownSeated(stood, Stance.Sit, Direction.Down, PropType.Chair, raised: false, seat);
        Assert.Equal(stood.X, onChair.X, 4);
        Assert.Equal(stood.Z + FieldRigChoice.GrownForwardOn(PropType.Chair), onChair.Z, 4);
        // Lifted only by what the chair stands over the grown sit's own seat (a hair), never sunk
        Assert.Equal(Math.Max(0f, Seats.GrownHeightOf(PropType.Chair) - seat), onChair.Y - stood.Y, 4);
        Assert.True(onChair.Y - stood.Y < 0.06f);
        // A sofa takes a deeper seat; a bench's top is the sit's to a hair
        var onBench = FieldRigChoice.GrownSeated(stood, Stance.Sit, Direction.Left, PropType.Bench, raised: false, seat);
        Assert.InRange(onBench.Y - stood.Y, 0f, 0.01f);
        Assert.Equal(stood.X - FieldRigChoice.GrownForwardOn(PropType.Bench), onBench.X, 4);
        Assert.True(FieldRigChoice.GrownForwardOn(PropType.Sofa) > FieldRigChoice.GrownForwardOn(PropType.Chair));
        // On a cushion, on its top; whoever stands at a height of their own is lifted no more
        var onCushion = FieldRigChoice.GrownSeated(stood, Stance.SitGround, Direction.Up, PropType.Cushion, raised: false, seat);
        Assert.Equal(stood.Y + Seats.GrownHeightOf(PropType.Cushion), onCushion.Y, 4);
        Assert.Equal(stood.Y, FieldRigChoice.GrownSeated(stood, Stance.SitGround, Direction.Up, PropType.Cushion, raised: true, seat).Y);
        // Live in the stances the grown kit is placed in, a sprite in the rest
        Assert.True(FieldRigChoice.LiveIn(Stance.Stand) && FieldRigChoice.LiveIn(Stance.Sit) && FieldRigChoice.LiveIn(Stance.SitGround));
        Assert.False(FieldRigChoice.LiveIn(Stance.Lie) || FieldRigChoice.LiveIn(Stance.Swim) || FieldRigChoice.LiveIn(Stance.Fish) || FieldRigChoice.LiveIn(Stance.Tug));
    }

    [Fact]
    public void TheMotionIsHandedTheFeetsSpeedOverTheGroundAndNothingForAJump()
    {
        var a = new Vector3(1f, 0f, 1f);
        Assert.Equal(Vector2.Zero, FieldRigChoice.VelocityOf(a, a + new Vector3(0.1f, 0, 0), 0f, 1f));
        Assert.Equal(Vector2.Zero, FieldRigChoice.VelocityOf(a, a + new Vector3(3f, 0, 0), 1f / 60f, 1f));
        var v = FieldRigChoice.VelocityOf(a, a + new Vector3(0.1f, 5f, -0.05f), 0.05f, 0.5f);
        Assert.Equal(4f, v.X, 4);
        Assert.Equal(-2f, v.Y, 4);

        // A grid walker a tile every quarter of a second, east: the motion's yaw comes round east, the walk blends in and
        // the cycle goes on by the distance, as the field hands it frame by frame
        var motion = new GrownMotion(0.72f, yaw: 0f);
        var feet = new Vector3(0, 0, 0);
        for (int frame = 0; frame < 60; frame++)
        {
            var next = feet + new Vector3(4f / 60f, 0, 0);
            motion.Step(1f / 60f, FieldRigChoice.VelocityOf(feet, next, 1f / 60f, 1f), face: 0f);
            feet = next;
        }
        Assert.Equal(MathF.PI / 2f, motion.Yaw, 1);
        Assert.True(motion.WalkBlend > 0.95f);
        Assert.True(motion.RunBlend > 0.9f, "four tiles a second is a run for a leg of 0.72");
    }

    [Fact]
    public void AGrownTrainerStandsAsTallOnThePlatformAsAChibiOne()
    {
        // 1.6 model units at three quarters is about the chibi body's 1.2
        Assert.InRange(1.6f * BattleRenderer.GrownBattleShare, 1.15f, 1.25f);
        var chibi = new CharacterRig();
        var root = Matrix4x4.CreateRotationY(0.3f) * Matrix4x4.CreateTranslation(2, 1, 3);
        Assert.Equal(root, BattleRenderer.InItsKit(chibi, root));
        var grown = new CharacterRig();
        grown.Style.Kit = CharacterKit.Grown;
        var scaled = BattleRenderer.InItsKit(grown, root);
        // About the feet: the origin stays where it stood
        Assert.Equal(Vector3.Transform(Vector3.Zero, root), Vector3.Transform(Vector3.Zero, scaled));
        Assert.Equal(BattleRenderer.GrownBattleShare, Vector3.Transform(Vector3.UnitY, scaled).Y - 1f, 4);
    }

    [Fact]
    public void AChibiBustIsAsItWasAndAGrownOneIsAHeadAndShoulders()
    {
        Assert.Equal((1.0f + CharacterStudio.BustLift, CharacterStudio.BustHeight), CharacterStudio.BustOf(CharacterKit.Chibi, BodyBuild.Kid, false, 1.0f));
        foreach (var build in new[] { BodyBuild.Teen, BodyBuild.Grown, BodyBuild.GrownBroad, BodyBuild.Child, BodyBuild.GrownElder })
        {
            var f = CharacterModels.GrownFrameOf(build);
            var (middle, height) = CharacterStudio.BustOf(CharacterKit.Grown, build, false, f.HeadC.Y);
            float top = middle + height / 2f, bottom = middle - height / 2f;
            // Over the crown with room for the hair and a hat, under the shoulders, and well above the waist
            Assert.True(top > f.Crown + 0.5f * f.HeadR, $"{build}: top {top} crown {f.Crown}");
            Assert.True(bottom < f.ShoulderY, $"{build}: bottom {bottom} shoulders {f.ShoulderY}");
            Assert.True(bottom > f.WaistY, $"{build}: bottom {bottom} waist {f.WaistY}");
        }
    }
}
