partial class Harness
{
    // ---------------------------------------------------------------- the first chapter (plan 02 · S4)

    // The opening's scenes (op*), played by the game's own scripts in a new game of their own: the bedroom and the
    // television's special, the rival bursting in, the rival running out of his door, the professor on Route 201 and
    // his briefcase, the first battle, the man in grey at Lake Verity, the Pokédex in the lab and the assistant's tour
    // of Sandgem Town. The scenes after the briefcase are started one by one where they play (the test project plays
    // the chapter through in order, OpeningTests). Not part of "all".
    public void OpeningMode()
    {
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        var story = game.Story;
        var sinnoh = MapDatabase.Get("Sinnoh");
        // Where the lab's people stand before any scene has walked them about: the scene skipped at the mode's end is
        // told from there each time (plan 12 · Q20)
        var labAtFirst = MapDatabase.Get("RowanLab").Everyone.Select(n => (Who: n, n.GridX, n.GridY, n.Facing)).ToList();

        DialogueManager Box() => game.Dialogue;
        GameState State() => game.State;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void Whole() { Until(() => Box().IsActive, "text on the screen"); Box().FinishLine(); Frames(2); }
        // Reads on, saying yes to every question, until a line says something (null: to the script's end)
        void ReadTo(string? text, int most = 2400)
        {
            for (int i = 0; i < most; i++)
            {
                if (text != null && Box().IsActive && Translated.Saying(Box().CurrentLine, text)) { Whole(); return; }
                if (text == null && !engine.ScriptRunning && !Box().IsActive) return;
                if (engine.Choice.IsOpen) { Frames(10); engine.Choice.Confirm(); Frames(4); }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
                else Frames(1);
            }
            Console.WriteLine($"  !! never said: {text ?? "the script's end"}");
        }
        void Scene(string file)
        {
            engine.StartScript(ScriptLibrary.Default.In(file, ScriptLibrary.OnEnter)!);
        }

        // ---- the bedroom: the special's last lines, then the room
        Until(() => Box().IsActive, "the television");
        Whole(); Shot("op01_the_television");
        ReadTo(null);
        Frames(30); Shot("op02_the_bedroom");

        // ---- a step off the starting tile: the rival comes up the stairs
        engine.Steering = (Direction.Down, false);
        Until(() => engine.ScriptRunning, "the rival's entrance", 120);
        engine.Steering = null;
        ReadTo("Did you see it?"); Frames(10); Shot("op03_the_rival_bursts_in");
        ReadTo("tons of them"); Frames(4); Shot("op03b_the_rival_runs_up");
        ReadTo("new computer"); Frames(4); Shot("op03c_the_rival_at_the_computer");
        ReadTo("ten-million fine"); Frames(4); Shot("op03d_the_rival_comes_back");
        ReadTo(null);
        Console.WriteLine($"bedroom: rival state {story.Var("VAR_PLAYER_HOUSE_RIVAL_STATE")}, rival on the map {game.Map.NPCs.Any(n => n.Key == "rival")}");

        // ---- Twinleaf Town: the rival runs out of his door into the player
        At("Sinnoh", 106, 876, Direction.Left);
        engine.Steering = (Direction.Left, false);
        Until(() => engine.ScriptRunning, "the rival's door", 120);
        engine.Steering = null;
        ReadTo("Who's standing right outside my door?"); Frames(10); Shot("op04_the_rival_runs_out");
        ReadTo(null);

        // ---- Route 201: the professor stops the two of them at the grass
        story.SetVar("VAR_TWINLEAF_TOWN_GUITARIST_TRIGGER_STATE", 2);
        At("Sinnoh", 111, 858, Direction.Up);
        engine.Steering = (Direction.Up, false);
        Until(() => engine.ScriptRunning, "the professor's scene", 120);
        engine.Steering = null;
        ReadTo("Way too slow!"); Shot("op05_route201_the_rival_waits");
        ReadTo("What do you think you are doing?"); Frames(10); Shot("op06_route201_the_professor");
        ReadTo("Do you love Pokémon?");
        Until(() => engine.Choice.IsOpen, "the professor's question"); Frames(20); Shot("op07_route201_do_you_love_pokemon");
        ReadTo("Open it and choose"); Frames(10); Shot("op08_route201_the_briefcase");
        ReadTo(null);
        // The rival has stepped aside, and the briefcase waits in view; and it is still there, in view, for a player who
        // went off through a door and came back (the game's own arrival on a map, as a warp brings one)
        Frames(30); Shot("op08b_route201_the_briefcase_waits");
        var road = game.Player.GridX;
        At("Sinnoh", road, 857, Direction.Up);
        game.ArriveOnMap();
        Frames(30); Shot("op08c_route201_the_briefcase_after_a_door");
        Console.WriteLine($"route 201: briefcase on the map {sinnoh.NPCs.Any(n => n.Key == "briefcase")}, rival at {sinnoh.NPCs.Where(n => n.Key == "rival" && n.GridY > 840 && n.GridY < 860).Select(n => (n.GridX, n.GridY)).FirstOrDefault()}");

        // ---- the briefcase: the three Pokémon
        var briefcase = sinnoh.NPCs.First(n => n.Key == "briefcase");
        At("Sinnoh", briefcase.GridX, briefcase.GridY + 1, Direction.Up);
        game.Interact();
        Until(() => State() == GameState.StarterSelect, "the briefcase opening", 600);
        Frames(40); Shot("op09_the_briefcase");
        game.StarterSelect.Close();

        // ---- the first battle: the rival with Platinum's team for a Turtwig
        var rivalTeam = new Trainer { Id = "rival_route_201_turtwig" };
        TrainerDatabase.Fill(rivalTeam, TrainerDatabase.Get(rivalTeam.Id)!);
        engine.StartScript(ScriptLibrary.Default.In("lake_verity_low_water", ScriptLibrary.OnEnter)!);
        PastTheOpening();
        story.SetVar("VAR_VISITED_LAKE_VERITY_WITH_RIVAL", 0);
        story.Unset("FLAG_HIDE_LAKE_VERITY_LOW_WATER_CYRUS");
        var first = StartBattle("", 5, rivalTeam, "Route201");
        Skip(1.0); Shot("op10_the_first_battle_begins");
        ToMainMenu(first); Shot("op11_the_first_battle");

        // ---- Lake Verity: the man in grey
        At("LakeVerity", 46, 53, Direction.Up);
        ReadTo("Who's that"); Frames(10); Shot("op12_lake_who_is_that");
        ReadTo("My name is Cyrus."); Frames(30); Shot("op13_lake_cyrus");
        ReadTo("Step aside."); Frames(10); Shot("op14_lake_step_aside");
        ReadTo("Did you hear that?"); Frames(10); Shot("op15_lake_did_you_hear_that");
        ReadTo(null);

        // ---- the lab: the Pokédex
        story.SetVar("VAR_SANDGEM_TOWN_STATE", 1);
        story.SetVar("VAR_SANDGEM_TOWN_LAB_STATE", 0);
        story.Unset("FLAG_HIDE_SANDGEM_TOWN_LAB_COUNTERPART");
        Scene("RowanLab");
        At("RowanLab", 7, 15, Direction.Up);
        ReadTo("Let me have a look"); Frames(10); Shot("op16_lab_the_professor");
        ReadTo("Will you take this Pokédex");
        Until(() => engine.Choice.IsOpen, "the professor's request"); Frames(20); Shot("op17_lab_the_pokedex");
        ReadTo("My first Pokémon is"); Frames(10); Shot("op18_lab_the_assistant");
        ReadTo(null);

        // ---- Sandgem Town: the professor's TM and the assistant's tour
        story.SetVar("VAR_SANDGEM_TOWN_STATE", 1);
        story.Unset("FLAG_HIDE_SANDGEM_TOWN_PROF_ROWAN");
        story.Unset("FLAG_HIDE_SANDGEM_TOWN_COUNTERPART");
        Scene("sandgem_town");
        At("Sinnoh", 168, 843, Direction.Down);
        ReadTo("One more thing."); Frames(10); Shot("op19_sandgem_one_more_thing");
        ReadTo("That's the Pokémon Center"); Frames(50); Shot("op20_sandgem_the_center");
        ReadTo("And that's the Poké Mart"); Frames(50); Shot("op21_sandgem_the_mart");
        ReadTo(null);
        Console.WriteLine($"opening: Pokédex {story.Has(StoryState.PokedexFlag)}, TM27 {inventory.GetQuantity(ItemDatabase.Get("TM27")!)}, sandgem state {story.Var("VAR_SANDGEM_TOWN_STATE")}");

        // ---- home, on the original's plan (plan 01 · M11, the last step): Mom's three scenes, each as its moment of the
        // chapter finds the house (they come last here, so the shots above are the ones they were)
        var house = MapDatabase.Get("PlayerHouse");
        NPC Mom() => house.Everyone.First(n => n.Key == "mom");
        string Where(NPC who) => $"{who.GridX},{who.GridY} facing {who.Facing}";
        // The first morning: down the stairs, and she comes round to their foot
        story.SetVar("VAR_PLAYER_HOUSE_STATE", 0);
        At("PlayerHouse", 11, 3, Direction.Right);
        game.ArriveOnMap();
        ReadTo("was just here"); Frames(4); Shot("op22_home_mom_at_the_foot_of_the_stairs");
        Console.WriteLine($"home, the first morning: Mom at {Where(Mom())}, the player at {game.Player.GridX},{game.Player.GridY}");
        ReadTo(null);
        Frames(20); Shot("op23_home_mom_back_at_her_television");
        // The mat, the first time out
        engine.Steering = (Direction.Down, false);
        At("PlayerHouse", 6, 9, Direction.Down);
        Until(() => engine.ScriptRunning, "the warning at the door", 120);
        engine.Steering = null;
        ReadTo("tall grass"); Frames(4); Shot("op24_home_the_warning_at_the_door");
        ReadTo(null);
        // Back from the first battle: in before the kitchen counter, and round to the cushions after her
        story.SetVar("VAR_PLAYER_HOUSE_STATE", 3);
        At("PlayerHouse", 2, 6, Direction.Up);
        game.ArriveOnMap();
        Frames(30); Shot("op25_home_mom_at_the_sink");
        Until(() => Mom().GridX >= 5, "Mom leading the way", 600);
        Frames(6); Shot("op26_home_following_mom");
        ReadTo("Welcome home"); Frames(4); Shot("op27_home_on_the_cushions");
        ReadTo("Running Shoes!"); Frames(4); Shot("op28_home_the_running_shoes");
        ReadTo(null);
        Console.WriteLine($"home, the Running Shoes: Mom at {Where(Mom())}, the player at {game.Player.GridX},{game.Player.GridY}, shoes {story.Has(StoryState.RunningShoesFlag)}, state {story.Var("VAR_PLAYER_HOUSE_STATE")}");
        // The Journal and the parcel: spoken to from her east, the rival's mother comes in and over
        story.Unset("FLAG_RECEIVED_PARCEL");
        At("PlayerHouse", 8, 8, Direction.Left);
        game.Interact();
        ReadTo("gone already"); Frames(4); Shot("op29_home_the_rivals_mother_comes_in");
        ReadTo("Would you really"); Frames(4); Shot("op30_home_the_parcel");
        ReadTo(null);
        Frames(20); Shot("op31_home_afterwards");
        Console.WriteLine($"home, the parcel: Journal {inventory.GetQuantity(ItemDatabase.Get("Journal")!)}, Parcel {inventory.GetQuantity(ItemDatabase.Get("Parcel")!)}, the rival's mother on the map {house.NPCs.Any(n => n.Key == "rival_mom")}, state {story.Var("VAR_PLAYER_HOUSE_STATE")}");

        // ---- a scene skipped (plan 12 · Q20): the lab's scene played to its end, and then skipped by the hold
        // The scene as the chapter comes to it, in the lab's door: the story put back before it each time
        var lab = MapDatabase.Get("RowanLab");
        void BeforeTheLab()
        {
            story.SetVar("VAR_SANDGEM_TOWN_STATE", 1);
            story.SetVar("VAR_SANDGEM_TOWN_LAB_STATE", 0);
            story.Unset(StoryState.PokedexFlag);
            story.Unset("FLAG_HIDE_SANDGEM_TOWN_LAB_COUNTERPART");
            story.Set("FLAG_HIDE_SANDGEM_TOWN_COUNTERPART");
            // (whoever a scene walks about a room stays where it left them: everyone back where the room has them)
            foreach (var (who, x, y, facing) in labAtFirst) (who.GridX, who.GridY, who.Facing) = (x, y, facing);
            game.Place(lab, 7, 15, Direction.Up);
            // (as a door brings the player: everyone back where the flags have them, and the lab's own scene started)
            game.ArriveOnMap();
            Until(() => engine.ScriptRunning, "the lab's scene", 120);
        }
        // Under "Seen" a scene this game hasn't played to its end, and this machine neither, can't be skipped: the
        // button held for a second and a half does nothing, and no ring is drawn
        engine.Settings.SkipScenes = SkipScenes.Seen;
        engine.Settings.ScenesSeen.Clear();
        story.RestoreScenes(Array.Empty<string>());
        int skippedBefore = game.ScenesSkipped;
        BeforeTheLab();
        ReadTo("Let me have a look");
        game.SkipHeld = true;
        Frames(90);
        Console.WriteLine($"skip, under Seen, before the scene was ever played: scene {game.Scene}, may be skipped {game.SceneMayBeSkipped}, the ring's fill {game.SkipFill:0.00}, skipped {game.ScenesSkipped - skippedBefore}");
        game.SkipHeld = false;
        ReadTo(null);
        Frames(40);
        // (what the scene left: where everyone stands, what the story holds, the team and the bag: Modes/Skips.cs)
        var played = WhatASceneLeft();
        Console.WriteLine($"skip: played to its end, the game has seen [{string.Join(", ", story.ScenesSeen.Order(StringComparer.Ordinal))}], this machine [{string.Join(", ", engine.Settings.ScenesSeen)}]");

        // Seen once, it may be skipped: the button held for a second closes the picture, and the scene is hurried
        // through to its question, which is asked at its own pace; answered, the button held again skips on to the end
        BeforeTheLab();
        ReadTo("Let me have a look");
        Frames(20);
        game.SkipHeld = true;
        Frames(33);
        Console.WriteLine($"skip, seen: scene {game.Scene}, may be skipped {game.SceneMayBeSkipped}, the ring's fill {game.SkipFill:0.00}");
        Shot("op40_skip_held");
        ShotCrop("op40b_skip_held_close", 1920 - 64 - 220, 0, 284, 124, 4);
        Until(() => game.Skipping, "the skip's dark closing", 120);
        Until(() => !game.Skipping, "the skip's dark opening", 240);
        game.SkipHeld = false;
        Until(() => engine.Choice.IsOpen, "the professor's question after the skip", 240);
        Console.WriteLine($"skip: the hurry stopped at the question \"{Box().CurrentLine}\", skipped {game.ScenesSkipped - skippedBefore}, scene {game.Scene}, Pokédex yet {story.Has(StoryState.PokedexFlag)}");
        Frames(10);
        engine.Choice.Confirm();
        Frames(20);
        game.SkipHeld = true;
        Until(() => game.Skipping, "the second skip's dark closing", 180);
        Until(() => !game.Skipping, "the second skip's dark opening", 240);
        game.SkipHeld = false;
        ReadTo(null);
        Frames(40);
        Shot("op41_skip_done");
        var skipped = WhatASceneLeft();
        Console.WriteLine($"skip: skipped {game.ScenesSkipped - skippedBefore} times in all");

        // Side by side: a line begins with !! where the skipped scene left something else than the played one
        TellBothWays("the lab", played, skipped);

        // The options as they were: nothing is skipped unless a mode says so
        engine.Settings.SkipScenes = SkipScenes.Off;
        engine.Settings.ScenesSeen.Clear();
    }
}
