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
                if (text != null && Box().IsActive && Box().CurrentLine.Contains(text)) { Whole(); return; }
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
        At("RowanLab", 5, 8, Direction.Up);
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
    }
}
