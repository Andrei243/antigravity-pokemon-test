using System.Numerics;
using PokemonPlatinumEngine.OpenWorld;

partial class Harness
{
    // ---------------------------------------------------------------- plan 30 · FM13, the mount as a rig

    /// <summary>The eight ride forms, made before the first shot so no frame draws a rider over nothing.</summary>
    static readonly string[] RideForms =
    {
        "Koraidon-Limited-Build", "Koraidon-Sprinting-Build", "Koraidon-Swimming-Build", "Koraidon-Gliding-Build",
        "Miraidon-Low-Power-Mode", "Miraidon-Drive-Mode", "Miraidon-Aquatic-Mode", "Miraidon-Glide-Mode"
    };

    /// <summary>
    /// FM13's shots (`SHOTS_FILTER=fw7,fw8`; plan 30, "FM13 as built"), at the end of `freewalk`: in the open test
    /// world (the lab, with a climbable block of the harness's own laid on the meadow by the ledge, laid before the lab
    /// painted one of its own: plan 34 · PD2's yard is FreeWalkYard's) with a test region's movement that lists the mount, Koraidon and then Miraidon
    /// ridden with every power: getting on half way, standing, the trot, the sprint with the camera's lead, a swim in
    /// the river, a glide off the hill's top and its change of form half way, a climb half way up the block, getting off
    /// half way; and both mounts standing from the side close up (the rider on the seat), and Koraidon at night by the
    /// pond (its shadow and its reflection). Each shot prints the form, the seat, the rider's place and the camera's ride.
    /// </summary>
    void FreeWalkMountShots()
    {
        if (filter.Length > 0 && !filter.Split(',').Any(f => f.StartsWith("fw7") || f.StartsWith("fw8"))) return;
        engine.StartNewGame();
        PastTheOpening();
        game.SaveSlot = 1;
        var map = openLab ??= BuildOpenLab();
        // A climbable box of the harness's own on the meadow under the ledge, 7 m tall (FM13's shots, kept as they were
        // shot; the lab's own painted face, plan 34 · PD2, is FreeWalkYard's)
        // (its bottom 2 cm over the highest ground under it, as MountRigTests' MountShots has it: a block sunk into the
        // ground has the meadow inside it, which the climb's step onto the top takes for open ground)
        double wallFoot = double.MinValue;
        for (double x = -32; x <= -26; x += 0.5)
            for (double z = -34; z <= -26; z += 0.5)
                wallFoot = Math.Max(wallFoot, map.Ground.Footing(x, z, 20, -20)?.Y ?? 0);
        wallFoot += 0.02;
        if (!map.Solids.Any(s => s.Name == "the climbing block"))
            map.Add(OpenBox.Metres("the climbing block", -32, wallFoot, -34, -26, wallFoot + 7, -26) with { Climbable = true });
        engine.RegionMovementOverride = new RegionMovement { Directions = MovementDirections.Free, Speed = MovementSpeed.Analog, Modes = new() { RegionMode.Mount } };
        foreach (var (_, flag) in MountRules.Powers) game.Story.Set(flag);
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);
        game.OpenWeather = null;
        foreach (string form in RideForms) PokemonModels.Get(form);
        Console.WriteLine($"  the ride forms made; the climbing block from {wallFoot:0.00} m to {wallFoot + 7:0.00} m");

        static ActionFrame Steer(int x, int y, bool run = false)
        {
            uint down = ActionFrame.Stick((sbyte)x, (sbyte)y).Down | (run ? 1u << (int)GameAction.Run : 0u);
            return new ActionFrame(down, 0) { MoveX = (sbyte)x, MoveY = (sbyte)y };
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
        void Press(GameAction action)
        {
            InputManager.Feed(ActionFrame.Press(action));
            Frames(1);
            InputManager.Feed(null);
        }
        void Tell(string name)
        {
            if (!Wanted(name)) return;
            Shot(name);
            TellOpenShot(name);
            var ride = game.OpenCamera.RideNow;
            Console.WriteLine($"    the mount: {game.MountTold ?? "!! none drawn"}; the renderer drew {game.OpenRenderer?.MountDrawn ?? "no mount"}");
            Console.WriteLine($"    the camera's ride: share {ride.Share:0.00}, {ride.Above:0.00} m over, arm {ride.Arm:0.00} m, pitch {ride.Pitch:0.0}°, lens {ride.Lens:0.0}°, lead {ride.Lead:0.00} m");
        }
        void GetOn()
        {
            Assert(game.ToggleMount(), "the mount refused: " + game.WhyNotMount);
            Frames(45);
        }
        static void Assert(bool holds, string what)
        {
            if (!holds) Console.WriteLine($"  !! {what}");
        }

        foreach (var (mount, name, number) in new[] { (MountRules.Koraidon, "koraidon", 70), (MountRules.Miraidon, "miraidon", 80) })
        {
            game.Story.SetVar(MountRules.MountVariable, mount);
            var west = map.Marker("west_road")!;

            // Getting on, half way: the mount out of its ball, the rider in the air
            PutInLab(map, west.Pos, 90, 60);
            Assert(game.ToggleMount(), "the mount refused: " + game.WhyNotMount);
            Frames(14);
            Tell($"fw{number}_{name}_getting_on");
            Frames(40);
            Tell($"fw{number + 1}_{name}_standing");

            // The trot east along the road, and the sprint with the camera's lead
            Hold(Steer(127, 0), 70);
            Tell($"fw{number + 2}_{name}_trot");
            Hold(Steer(127, 0, run: true), 90);
            Tell($"fw{number + 3}_{name}_sprint");

            // Into the pond: a swim (the river is waded)
            PutInLab(map, OnLabGround(map, 40, -27), 180, 180);
            GetOn();
            for (int f = 0; f < 240 && game.OpenField!.Player.Body.Stance != OpenStance.Swimming; f++) Hold(Steer(0, -127), 1);
            Hold(Steer(0, -127), 50);
            Tell($"fw{number + 4}_{name}_swim");

            // Off the hill's top: the high jump, a glide caught half way through its change of form, and the glide
            var top = map.Marker("hill_top")!;
            PutInLab(map, top.Pos, 270, 270);
            GetOn();
            Hold(Steer(-127, 0), 20);
            Press(GameAction.Confirm);
            Hold(Steer(-127, 0), 24);
            Press(GameAction.Confirm);
            Assert(game.OpenField!.Player.Body.Stance == OpenStance.Gliding, "not gliding: " + game.OpenField!.Player.Body.Stance);
            Hold(Steer(-127, 0), 8);
            Tell($"fw{number + 5}_{name}_glide_changing");
            Hold(Steer(-127, 0), 40);
            Tell($"fw{number + 6}_{name}_glide");

            // The climbing block from its east side, half way up
            PutInLab(map, OnLabGround(map, -23, -30), 270, 270);
            GetOn();
            for (int f = 0; f < 240 && game.OpenField!.Player.Body.Stance != OpenStance.Climbing; f++) Hold(Steer(-127, 0), 1);
            Assert(game.OpenField!.Player.Body.Stance == OpenStance.Climbing, "never clung to the block");
            Hold(Steer(-127, 0), 100);
            Tell($"fw{number + 7}_{name}_climb");

            // Getting off on the road, half way
            PutInLab(map, west.Pos, 90, 60);
            GetOn();
            Press(GameAction.Mount);
            Frames(9);
            Tell($"fw{number + 8}_{name}_getting_off");
            Frames(30);
        }

        // Both standing from the side, close: the rider on the seat
        foreach (var (mount, name) in new[] { (MountRules.Koraidon, "koraidon"), (MountRules.Miraidon, "miraidon") })
        {
            game.Story.SetVar(MountRules.MountVariable, mount);
            var at = map.Marker("west_road")!.Pos;
            PutInLab(map, at, 90, 90);
            GetOn();
            var feet = new Vector3(at.X / 1000f, at.Y / 1000f, at.Z / 1000f);
            game.OpenView = (feet + new Vector3(0.6f, 1.5f, 5.2f), feet + new Vector3(0, 1.1f, 0));
            Frames(2);
            Tell($"fw89_{name}_side");
            game.OpenView = (feet + new Vector3(-4.2f, 2.0f, 3.0f), feet + new Vector3(0, 1.1f, 0));
            Frames(2);
            Tell($"fw89_{name}_behind");
            game.OpenView = null;
            Press(GameAction.Mount);
            Frames(30);
        }

        // After dark by the pond: its shadow under the lamp and its reflection
        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        game.Story.SetVar(MountRules.MountVariable, MountRules.Koraidon);
        PutInLab(map, OnLabGround(map, 30, -28), 130, 130);
        GetOn();
        Tell("fw79_koraidon_pond_night");
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);
        game.Story.SetVar(MountRules.MountVariable, 0);
        engine.RegionMovementOverride = null;
    }
}
