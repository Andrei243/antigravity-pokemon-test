using System.Numerics;
using PokemonPlatinumEngine.OpenWorld;

partial class Harness
{
    // ---------------------------------------------------------------- plan 34 · PD2, the mount's yard in the open test world

    /// <summary>
    /// PD2's shots (`SHOTS_FILTER=fwy`; plan 34, "PD2 as built"), last in `freewalk`: the lab as its baker paints it now,
    /// with the mount's yard on the west meadow (a block of rock whose east face is painted <c>ClimbableRock</c>, a hop step
    /// and a high step), Koraidon ridden with every power along the links its navigation mesh bakes: half way up the
    /// painted face (fwy0), standing on the block's top (fwy1), gliding off it (fwy2), the high jump in the air and on the
    /// high step (fwy3, fwy4), the hop in the air (fwy5), and the yard from over the meadow with the mount on its top
    /// (fwy6). Each shot prints the open world's line, the mount's form and the camera's ride, and what the stance was.
    /// </summary>
    void FreeWalkYardShots()
    {
        if (filter.Length > 0 && !filter.Split(',').Any(f => f.StartsWith("fwy"))) return;
        engine.StartNewGame();
        PastTheOpening();
        game.SaveSlot = 1;
        var map = openLab ??= BuildOpenLab();
        engine.RegionMovementOverride = new RegionMovement { Directions = MovementDirections.Free, Speed = MovementSpeed.Analog, Modes = new() { RegionMode.Mount } };
        foreach (var (_, flag) in MountRules.Powers) game.Story.Set(flag);
        game.Story.SetVar(MountRules.MountVariable, MountRules.Koraidon);
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);
        game.OpenWeather = null;
        foreach (string form in RideForms) PokemonModels.Get(form);
        var mesh = map.Land?.Nav;
        if (mesh != null)
        {
            int Count(OpenNavFlags f) => mesh.Links.Count(l => (l.Flags & f) != 0);
            Console.WriteLine($"  the lab's mesh: {Count(OpenNavFlags.Jump)} hops, {Count(OpenNavFlags.HighJump)} high jumps, {Count(OpenNavFlags.Climb)} climbs, {Count(OpenNavFlags.Glide)} glides");
        }
        else Console.WriteLine("  !! the lab has no navigation mesh");

        static ActionFrame Steer(int x, int y, bool jump = false)
        {
            var held = ActionFrame.Stick((sbyte)x, (sbyte)y);
            uint pressed = jump ? ActionFrame.Press(GameAction.Confirm).Pressed : 0u;
            return new ActionFrame(held.Down | (jump ? ActionFrame.Press(GameAction.Confirm).Down : 0u), pressed) { MoveX = (sbyte)x, MoveY = (sbyte)y };
        }
        void Hold(ActionFrame frame, int frames)
        {
            for (int f = 0; f < frames; f++)
            {
                InputManager.Feed(frame);
                Frames(1);
            }
            InputManager.Feed(null);
        }
        OpenBody Body() => game.OpenField!.Player.Body;
        void Tell(string name)
        {
            if (!Wanted(name)) return;
            Shot(name);
            TellOpenShot(name);
            var ride = game.OpenCamera.RideNow;
            Console.WriteLine($"    the stance {Body().Stance}; the mount: {game.MountTold ?? "!! none drawn"}");
            Console.WriteLine($"    the camera's ride: share {ride.Share:0.00}, {ride.Above:0.00} m over, arm {ride.Arm:0.00} m, pitch {ride.Pitch:0.0}°");
        }
        void GetOn()
        {
            if (!game.ToggleMount()) Console.WriteLine("  !! the mount refused: " + game.WhyNotMount);
            Frames(45);
        }

        // Up the painted face from its foot, half way, and then on its top
        PutInLab(map, OnLabGround(map, -53, -20), 270, 270);
        GetOn();
        for (int f = 0; f < 240 && Body().Stance != OpenStance.Climbing; f++) Hold(Steer(-127, 0), 1);
        if (Body().Stance != OpenStance.Climbing) Console.WriteLine("  !! never clung to the painted face");
        Hold(Steer(-127, 0), 100);
        Tell("fwy0_yard_climb");
        for (int f = 0; f < 400 && Body().Stance == OpenStance.Climbing; f++) Hold(Steer(-127, 0), 1);
        Hold(Steer(-127, 0), 30);
        Hold(ActionFrame.None, 20);
        Tell("fwy1_yard_block_top");

        // Off its top eastward: a press once falling, and the glide
        PutInLab(map, OnLabGround(map, -58.5, -18, 10, 3), 90, 90);
        GetOn();
        for (int f = 0; f < 120 && Body().Stance != OpenStance.Falling; f++) Hold(Steer(127, 0), 1);
        Hold(Steer(127, 0, jump: true), 1);
        Hold(Steer(127, 0), 40);
        Tell("fwy2_yard_glide");

        // The high jump onto the high step from the east, in the air and landed
        PutInLab(map, OnLabGround(map, -44.6, -23.5), 270, 270);
        GetOn();
        Hold(Steer(-127, 0, jump: true), 1);
        Hold(Steer(-127, 0), 16);
        Tell("fwy3_yard_high_jump");
        for (int f = 0; f < 120 && Body().Stance != OpenStance.Standing; f++) Hold(Steer(-127, 0), 1);
        Hold(ActionFrame.None, 30);
        Tell("fwy4_yard_high_step");

        // The hop onto the hop step, in the air
        PutInLab(map, OnLabGround(map, -45.7, -15), 270, 270);
        GetOn();
        Hold(Steer(-127, 0, jump: true), 1);
        Hold(Steer(-127, 0), 14);
        Tell("fwy5_yard_hop");

        // The yard from over the meadow, the mount on the block's top
        PutInLab(map, OnLabGround(map, -59, -20, 10, 3), 90, 90);
        GetOn();
        var feet = new Vector3(-55f, 3f, -20f);
        game.OpenView = (feet + new Vector3(16f, 10f, 14f), feet);
        Frames(2);
        Tell("fwy6_yard_overview");
        game.OpenView = null;

        game.Story.SetVar(MountRules.MountVariable, 0);
        engine.RegionMovementOverride = null;
    }
}
