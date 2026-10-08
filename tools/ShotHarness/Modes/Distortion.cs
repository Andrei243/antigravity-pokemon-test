partial class Harness
{
    // Not part of `all` but the end of `world`: the Distortion World's floors, islands over nothing, from where the rift
    // at Spear Pillar puts the player down to Giratina's room (its light is the same at every hour), and the models that
    // stood in for the north's last landmarks: Sunyshore's sea stack, Snowpoint's harbour storehouse and its drifts, and
    // Spear Pillar's rifts (which stand on the torn Spear Pillar of plan 02 · S12's scene, put on the pillar for the picture)
    public void DistortionMode()
    {
        (string Name, string Map, int X, int Y, Direction Facing)[] distorted =
        {
            ("wh0_distortion_1f", "DistortionWorld1F", 34, 30, Direction.Down), ("wh1_distortion_1f_slab", "DistortionWorld1F", 20, 42, Direction.Down),
            ("wh2_distortion_b1f", "DistortionWorldB1F", 24, 12, Direction.Right), ("wh3_distortion_b2f_stones", "DistortionWorldB2F", 18, 25, Direction.Up),
            ("wh4_distortion_b2f_upper", "DistortionWorldB2F", 34, 38, Direction.Up), ("wh5_distortion_b3f", "DistortionWorldB3F", 18, 22, Direction.Down),
            ("wh6_distortion_b4f", "DistortionWorldB4F", 12, 12, Direction.Down), ("wh7_distortion_b5f", "DistortionWorldB5F", 30, 33, Direction.Up),
            ("wh8_distortion_b6f", "DistortionWorldB6F", 25, 28, Direction.Right), ("wh9_distortion_b7f", "DistortionWorldB7F", 11, 44, Direction.Up),
            ("whg_distortion_giratina_room", "DistortionWorldGiratinaRoom", 15, 20, Direction.Up),
            ("wht_distortion_turnback_room", "DistortionWorldTurnbackCaveRoom", 46, 45, Direction.Down)
        };
        foreach (var (name, map, x, y, facing) in distorted)
        {
            At(map, x, y, facing); Frames(2); Shot(name);
        }
        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        At("DistortionWorld1F", 34, 30, Direction.Down); Frames(2); Shot("whn_distortion_1f_night");
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);

        // The north's landmarks that had stand-ins until now
        // (seen from the water south of it, surfing)
        At("Sinnoh", 882, 765, Direction.Up);
        game.Player.SetMode(TravelMode.Surfing);
        Frames(2); Shot("wi0_sunyshore_sea_stack");
        game.Player.SetMode(TravelMode.OnFoot);
        At("Sinnoh", 375, 246, Direction.Up); Frames(2); Shot("wi1_snowpoint_storehouse");
        var pillar = MapDatabase.Get("SpearPillar");
        var rifts = new[]
        {
            new Prop { Type = PropType.RiftShadow, X = 28, Y = 20, Width = 7, Depth = 7, Height = 0.5f, Model = "d5_ana_pl" },
            new Prop { Type = PropType.Rift, X = 29, Y = 23, Width = 1, Depth = 1, Height = 5.6f, Model = "d5_ana_d" },
            new Prop { Type = PropType.Rift, X = 33, Y = 23, Width = 1, Depth = 1, Height = 5.6f, Model = "d5_ana_p" }
        };
        var renderer = game.World;
        pillar.Props.AddRange(rifts);
        renderer.Forget(pillar, 0, 0, pillar.Width, pillar.Height);
        At("SpearPillar", 31, 28, Direction.Up); Frames(2); Shot("wi2_spear_pillar_rifts");
        foreach (var rift in rifts) pillar.Props.Remove(rift);
        renderer.Forget(pillar, 0, 0, pillar.Width, pillar.Height);
    }
}
