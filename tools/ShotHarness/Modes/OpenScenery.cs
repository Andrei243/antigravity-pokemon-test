using PokemonPlatinumEngine.Graphics.OpenWorld;
using PokemonPlatinumEngine.OpenWorld;

partial class Harness
{
    // ---------------------------------------------------------------- plan 32 · OW9, the open world's renderer II

    /// <summary>What a shot of the open world drew of plan 32 · OW9's, printed under <see cref="TellOpenShot"/>'s lines.</summary>
    void TellOpenScenery(string name)
    {
        var renderer = game.OpenRenderer!;
        var f = renderer.Foliage;
        var b = renderer.Buildings;
        var p = renderer.Figures;
        Console.WriteLine($"    OW9: buildings {b.Buildings} and blocks {b.Blocks} ({b.Triangles} triangles); trees of the map {f.MapTrees}, "
            + $"{f.NearTrees} near as meshes ({f.NearTreeTriangles} triangles), {f.FarTrees} as impostors; {f.Tufts} tufts; "
            + $"figures {game.OpenFigures.Count}: {p.Live} live, {p.Impostors} impostors, {p.Waiting} waiting for their rigs, {p.Pending} for their bake, "
            + $"{p.Exclaiming} with the \"!\"; looks baked {p.LooksBaked}; the land beyond {renderer.HorizonTriangles} triangles and {renderer.HorizonTrees} far trees");
    }

    /// <summary>A shot of the open world as <see cref="OpenLabShot"/> takes it (which prints OW9's line too).</summary>
    void OpenSceneryShot(string name) => OpenLabShot(name);

    /// <summary>The frames an impostor's bake takes, a look a frame, until every far figure's look is baked (at most forty).</summary>
    void BakeTheFiguresImpostors()
    {
        for (int i = 0; i < 40 && game.OpenRenderer is { } r && (r.Figures.Pending > 0 || r.Figures.Waiting > 0); i++) Frames(1);
    }


    // The crowd's looks: ten, three of each, so every far one's look stands in the impostors' atlas at once
    static readonly string[] CrowdLooks = { "lass", "youngster", "hiker", "picnicker", "gentleman", "old_woman", "jogger", "school_kid_f", "worker", "backpacker_m" };

    // The field Pokémon the crowd stands with, until OW7's spawner stands them in the zones
    static readonly string[] FieldSpecies = { "Wooloo", "Skwovet", "Rookidee", "Yamper", "Nickit", "Gossifleur", "Bunnelby", "Pikachu", "Eevee", "Lillipup", "Zigzagoon", "Wooloo" };

    /// <summary>
    /// The village of plan 32 · OW9's done-when: two more houses beside the lab's own north of the hill (solids laid
    /// on the harness's own copy of the map, built as the kit builds the first), thirty people and twelve Pokémon stood
    /// about the meadow before them by hand (<c>Driver.OpenFiguresByHand</c>), facing ways of their own. Their places and
    /// looks are fixed numbers, never the game's chance, so the picture is the same every run and at every commit.
    /// </summary>
    void LayTheVillage(OpenMap map)
    {
        if (map.Buildings.Count < 3)
        {
            foreach (var (name, x0, z0, x1, z1, town) in new[]
            {
                ("the west house", -33.5, -48.0, -27.5, -42.0, Architecture.Plaster),
                ("the east house", -8.0, -50.0, -1.0, -44.0, Architecture.Townhouse)
            })
            {
                var box = OpenBox.Metres(name, x0, -1, z0, x1, 4, z1);
                map.Add(box);
                map.Add(new OpenBuilding(box, BuildingKind.House, town, 0, Direction.Down));
            }
        }
        var figures = new List<OpenFigure>();
        for (int i = 0; i < 30; i++)
        {
            int col = i % 6, row = i / 6;
            double x = -44 + col * 9.5 + (row % 2) * 3.5, z = -38 + row * 6.5 - (col % 3) * 1.2;
            var at = OnLabGround(map, x, z, 20, -5);
            float yaw = (i * 137 % 360) * MathF.PI / 180f;
            figures.Add(new OpenFigure(OpenFigureKind.Person, $"crowd:{i}", CharacterStyle.InKit(CrowdLooks[i % CrowdLooks.Length], CharacterKit.Grown),
                new Vector3((float)at.MetresX, (float)at.MetresY, (float)at.MetresZ), yaw));
        }
        for (int i = 0; i < FieldSpecies.Length; i++)
        {
            double x = -40 + (i % 6) * 11 + 4, z = -32 + (i / 6) * 13 + (i % 2) * 2;
            var at = OnLabGround(map, x, z, 20, -5);
            float yaw = (i * 211 % 360) * MathF.PI / 180f;
            float height = PokemonDatabase.Get(FieldSpecies[i])?.Height ?? 0.6f;
            figures.Add(new OpenFigure(OpenFigureKind.Pokemon, $"field:{i}", FieldSpecies[i], new Vector3((float)at.MetresX, (float)at.MetresY, (float)at.MetresZ), yaw, Height: height));
        }
        game.OpenFiguresByHand = figures;
    }

    /// <summary>
    /// Plan 32 · OW9's shots, the last of the <c>open</c> mode (<c>open50</c> to <c>open72</c>): the lab's house from the
    /// front, behind and after dark; the trees near the west road, the grove south of the river and the far ones from the
    /// hill's top; the grass's tufts parting round the player as they run; the meadow's people, a trainer's "!", a
    /// Pokémon following; the rings to the horizon by day and after dark; and the village with its crowd near and far,
    /// with the impostors' atlases written beside them.
    /// </summary>
    void OpenSceneryShots(OpenMap map)
    {
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);
        game.OpenWeather = null;
        game.OpenView = null;

        // The house: its front from the south-east, its back from the north, after dark with its windows lit
        PutInLab(map, OnLabGround(map, -11, -35), 220, 220);
        OpenSceneryShot("open50_house_front_day");
        PutInLab(map, OnLabGround(map, -20, -58), 0, 0);
        OpenSceneryShot("open51_house_back_day");
        PutInLab(map, OnLabGround(map, -32, -36), 130, 130);
        OpenSceneryShot("open52_house_west_day");

        // Trees: near the west road, the grove south of the river, and from the hill's top the far ones
        PutInLab(map, OnLabGround(map, -50, 25), 10, 10);
        OpenSceneryShot("open53_trees_near_day");
        PutInLab(map, OnLabGround(map, -6, 31), 0, 0);
        OpenSceneryShot("open54_grove_south_day");
        PutInLab(map, map.Marker("hill_top")!.Pos, 45, 45);
        OpenSceneryShot("open55_from_the_top_day");

        // The grass: the player run east across the south-west meadow, then shot at once as the tufts part round them
        PutInLab(map, OnLabGround(map, -40, 16), 90, 90);
        var run = new ActionFrame(ActionFrame.Stick(127, 0).Down | ActionFrame.Press(GameAction.Run).Down, 0) { MoveX = 127, MoveY = 0 };
        for (int f = 0; f < 70; f++) { InputManager.Feed(run); Frames(1); }
        var walk = new ActionFrame(ActionFrame.Stick(127, 0).Down, 0) { MoveX = 127, MoveY = 0 };
        for (int f = 0; f < 12; f++) { InputManager.Feed(walk); Frames(1); }
        if (Wanted("open56_grass_parting"))
        {
            Shot("open56_grass_parting");
            TellOpenShot("open56_grass_parting");
            TellOpenScenery("open56_grass_parting");
            var at = game.OpenRenderer!.PlayerOnPicture;
            int cx = Math.Clamp((int)(at.X * 1920) - 160, 0, 1920 - 320), cy = Math.Clamp((int)(at.Y * 1080) + 60, 0, 1080 - 180);
            ShotCrop("open56b_grass_parting_close", cx, cy, 320, 180, 4);
        }
        InputManager.Feed(null);

        // The meadow's people: the walker near, the camper by the meadow's middle, the picnicker past the live rigs' reach
        PutInLab(map, OnLabGround(map, 4, -44), 150, 135);
        OpenSceneryShot("open57_people_day");
        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        PutInLab(map, OnLabGround(map, -11, -35), 220, 220);
        OpenSceneryShot("open58_house_night");
        PutInLab(map, OnLabGround(map, 4, -44), 150, 135);
        OpenSceneryShot("open59_people_night");
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);

        // A trainer's "!": the player put down between the two trainers, who both see them at once (their impostors baked
        // already, so the shot is taken within the "!"'s 84 ticks)
        PutInLab(map, map.Marker("twins_gap")!.Pos, 0, 300, bake: false);
        for (int f = 0; f < 8 && game.OpenApproach == null; f++) Frames(1);
        Frames(6);
        if (Wanted("open60_exclaim"))
        {
            Shot("open60_exclaim");
            TellOpenShot("open60_exclaim");
            TellOpenScenery("open60_exclaim");
            Console.WriteLine($"    the walk up: {(game.OpenApproach is { } a ? $"{a.Trainer.Key}{(a.Exclaiming ? " exclaiming" : "")}" : "!! nobody")}"
                + $"{(game.OpenPairApproach is { } b ? $" and {b.Trainer.Key}{(b.Exclaiming ? " exclaiming" : "")}" : "")}");
        }

        // A Pokémon following: the lead, behind the player along the west road
        PutInLab(map, map.Marker("west_road")!.Pos, 90, 160);
        var lead = game.Party.Members[0];
        game.OpenField!.Follower = new OpenFollower(lead.LookName, game.OpenField.Player.Pos, game.OpenField.Player.Yaw);
        PokemonModels.Get(lead.LookName);
        var east = new ActionFrame(ActionFrame.Stick(127, 0).Down, 0) { MoveX = 127, MoveY = 0 };
        for (int f = 0; f < 150; f++) { InputManager.Feed(east); Frames(1); }
        InputManager.Feed(null);
        Frames(30);
        // Turned to look across the two from the south, the follower beside the player rather than behind them
        game.OpenCamera.Face(150 * Math.PI / 180);
        Frames(2);
        OpenSceneryShot("open61_follower_day");
        game.OpenField.Follower = null;

        // The rings to the horizon: from over the lab's west edge looking out west, by day and after dark, and over all of it
        PutInLab(map, OnLabGround(map, -60, -10), 270, 270);
        game.OpenView = (new Vector3(-58, 26, -6), new Vector3(-400, 8, -40));
        OpenSceneryShot("open62_horizon_west_day");
        game.OpenView = (new Vector3(30, 40, 50), new Vector3(600, 0, 600));
        OpenSceneryShot("open63_horizon_south_east_day");
        game.OpenView = (new Vector3(-260, 180, 260), new Vector3(0, 0, 0));
        OpenSceneryShot("open64_horizon_over_all_day");
        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        game.OpenView = (new Vector3(-58, 26, -6), new Vector3(-400, 8, -40));
        OpenSceneryShot("open65_horizon_west_night");
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);
        game.OpenView = null;

        // The village with its crowd: thirty people and twelve Pokémon, near (twelve live, the rest impostors) and from far off
        LayTheVillage(map);
        PutInLab(map, OnLabGround(map, -14, -24), 180, 200);
        OpenSceneryShot("open66_village_crowd_day");
        PutInLab(map, OnLabGround(map, 30, -20), 270, 285);
        OpenSceneryShot("open67_village_crowd_from_the_east");
        game.OpenView = (new Vector3(-16, 30, 25), new Vector3(-16, 0, -40));
        OpenSceneryShot("open68_village_from_over_the_hill");
        game.OpenView = null;
        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        PutInLab(map, OnLabGround(map, -14, -24), 180, 200);
        OpenSceneryShot("open69_village_crowd_night");
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);

        // The impostors' atlases as baked: the trees' (once) and the figures' (as the crowd left it)
        WriteOpenAtlases();
        game.OpenFiguresByHand = null;
    }

    /// <summary>The trees' impostor atlas and the figures', written beside the shots (<c>open70_trees_atlas</c>, <c>open71_figures_atlas</c>).</summary>
    void WriteOpenAtlases()
    {
        if (game.OpenRenderer is not { } renderer) return;
        if (Wanted("open70_trees_atlas") && renderer.Foliage.Atlas is { } trees)
        {
            var image = trees.ToImage();
            Save(image, "open70_trees_atlas");
        }
        if (Wanted("open71_figures_atlas") && renderer.Figures.AtlasTexture.Id != 0)
        {
            var image = Raylib.LoadImageFromTexture(renderer.Figures.AtlasTexture);
            Save(image, "open71_figures_atlas");
        }
    }
}
