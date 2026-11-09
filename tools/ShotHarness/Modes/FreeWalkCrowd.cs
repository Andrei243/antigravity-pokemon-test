using System.Diagnostics;
using PokemonPlatinumEngine.Graphics;

partial class Harness
{
    // Plan 30 · FM3: the crowd of thirty on Aquacorde Town's promenade, the busiest Kalos place FM0 built, with a region's
    // kit laid as grown (engine.RegionKitOverride: no region's file says so yet)
    const int CrowdX = 261, CrowdZ = 661;

    static readonly string[] GridCrowdLooks =
    {
        "LASS", "YOUNGSTER", "CLERK", "BLACKBELT", "OLD_MAN", "NURSE", "DAWN", "RIVAL", "CYNTHIA", "ROARK"
    };

    readonly List<NPC> crowd = new();

    /// <summary>
    /// Thirty people stood about the promenade round the player, on open tiles a step or more apart, each a look of the
    /// table facing one of the four ways, the eleventh with a "!" over their head; put on the map's own list, so the
    /// field gathers them as it gathers anyone.
    /// </summary>
    void LayTheCrowd()
    {
        var map = game.Map;
        crowd.Clear();
        int n = 0;
        var ways = new[] { Direction.Down, Direction.Left, Direction.Right, Direction.Up };
        for (int dz = -5; dz <= 5 && n < 30; dz += 2)
            for (int dx = -11; dx <= 11 && n < 30; dx += 2)
            {
                int x = CrowdX + dx + (dz / 2 % 2 == 0 ? 0 : 1), z = CrowdZ + dz;
                if ((x == CrowdX && z == CrowdZ) || !map.IsWalkable(x, z) || map.NPCs.At(x, z) != null) continue;
                var npc = new NPC { Name = $"fw50_crowd_{n}", NpcType = GridCrowdLooks[n % GridCrowdLooks.Length], GridX = x, GridY = z, Facing = ways[(n * 3 + dz + 12) & 3] };
                map.NPCs.Add(npc);
                crowd.Add(npc);
                n++;
            }
        crowd.ElementAtOrDefault(10)?.ShowBubble(EmoteBubble.Exclaim, 600f);
        Console.WriteLine($"  the crowd: {crowd.Count} people round ({CrowdX}, {CrowdZ})");
    }

    void LiftTheCrowd()
    {
        foreach (var npc in crowd) game.Map.NPCs.Remove(npc);
        crowd.Clear();
    }

    /// <summary>Frames until every grown look of the crowd is made and every impostor baked (one look a frame), a minute at most.</summary>
    void WaitForTheCrowd()
    {
        var rigs = game.World.LiveRigs;
        var watch = Stopwatch.StartNew();
        int settled = 0;
        while (watch.Elapsed.TotalSeconds < 90 && settled < 20)
        {
            Frames(1);
            settled = rigs.Waiting == 0 ? settled + 1 : 0;
        }
        Frames(12);
        if (rigs.Waiting > 0) Console.WriteLine($"  !! {rigs.Waiting} grown looks still not made after {watch.Elapsed.TotalSeconds:0} s");
    }

    void TellTheCrowd(string name)
    {
        var rigs = game.World.LiveRigs;
        Console.WriteLine($"  {name}: {rigs.Live} live rigs, {rigs.Impostors} impostors, {rigs.Sprites} sprites standing in ({rigs.Waiting} looks being made), "
            + $"{rigs.LooksBaked} looks' impostors baked; {rigs.Triangles} triangles of people in the colour pass, {rigs.ShadowTriangles} in the shadow pass");
    }

    /// <summary>
    /// fw50 (plan 30 · FM3): the crowd of thirty as the game's own path draws a grown region's people, by day, close up
    /// at the screen's own resolution and after dark, beside the same crowd as today's chibi sprites; prints the live,
    /// impostor and sprite counts, the triangles and one profiled frame's passes (the Characters lap among them), which
    /// are read, never judged, on the harness's shared card.
    /// </summary>
    void FreeWalkCrowd()
    {
        if (!Wanted("fw50")) return;
        engine.NewGameRegion = "Kalos";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        At("Kalos", CrowdX, CrowdZ, Direction.Down);
        Frames(20);
        LayTheCrowd();
        Frames(10);
        Shot("fw50_crowd_chibi");

        engine.RegionKitOverride = PokemonPlatinumEngine.Data.CharacterKit.Grown;
        WaitForTheCrowd();
        Shot("fw50_crowd");
        TellTheCrowd("fw50_crowd");
        ShotCrop("fw50_crowd_close", 560, 300, 800, 450, 2);

        FrameProfiler.Enabled = true;
        FrameProfiler.Reset();
        Frames(30);
        Console.WriteLine($"  fw50_crowd profiled (read, never judged here): {FrameProfiler.Report()}");
        FrameProfiler.Enabled = false;

        var hour = engine.Settings.TimeOfDay;
        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        Frames(20);
        Shot("fw50_crowd_night");
        TellTheCrowd("fw50_crowd_night");
        engine.Settings.TimeOfDay = hour;
        engine.ApplySettings(window: false);

        Board("fw50_board", new List<(string, string)> { ("fw50_crowd_chibi", "today: chibi sprites"), ("fw50_crowd", "grown: live rigs and impostors"), ("fw50_crowd_night", "grown, after dark") },
            new Rectangle(0, 0, 1920, 1080), 1);
        engine.RegionKitOverride = null;
        LiftTheCrowd();
        Frames(10);
        game.DeleteSave();
    }

    /// <summary>
    /// `profile`'s freewalk_crowd scene (plan 30 · FM3's done-when): the crowd of thirty on the promenade, grown, by day,
    /// timed against the same 8 ms; laid in and taken away again, the player put back where they stood.
    /// </summary>
    void FreeWalkCrowdProfile()
    {
        const string label = "freewalk_crowd";
        var only = (Environment.GetEnvironmentVariable("SHOTS_ONLY") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
        bool look = Environment.GetEnvironmentVariable("SHOTS_LOOK") == "1";
        if (only.Length > 0 && !only.Any(word => label.Contains(word, StringComparison.OrdinalIgnoreCase)) && !look) return;
        if (only.Length == 0 && !look) return;
        var (stood, x, y, facing) = (game.Map, game.Player.GridX, game.Player.GridY, game.Player.Facing);
        At("Kalos", CrowdX, CrowdZ, Direction.Down);
        Frames(20);
        LayTheCrowd();
        engine.RegionKitOverride = PokemonPlatinumEngine.Data.CharacterKit.Grown;
        WaitForTheCrowd();
        TellTheCrowd(label);
        Profile("freewalk_crowd: thirty grown people on Aquacorde's promenade, by day");
        engine.RegionKitOverride = null;
        LiftTheCrowd();
        game.Place(stood, x, y, facing);
    }
}
