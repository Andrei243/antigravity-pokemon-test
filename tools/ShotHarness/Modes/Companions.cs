partial class Harness
{
    // ---------------------------------------------------------------- companions (plan 10 · F3 to F5)

    // The lead out of its ball behind the player with the options' FOLLOWER row on: standing, walking on its walk
    // strip, through a door into a room, over a ledge, back in its ball on a Bicycle, spoken to and with something
    // found; then wild Pokémon in sight on Route 201 with the WILD POKéMON row at VISIBLE, by day and after dark, and
    // one touched as the way into its battle flashes (cp*; not part of "all"). It prints who walks and where, and who
    // stands in sight with their species, level and tile.
    public void CompanionsMode()
    {
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        var team = game.Party;
        var sinnoh = MapDatabase.Get("Sinnoh");
        DialogueManager Box() => game.Dialogue;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void Walk(Direction way, int tiles = 1)
        {
            for (int i = 0; i < tiles && !engine.ScriptRunning && game.State == GameState.Overworld; i++)
            {
                engine.Steering = (way, false);
                for (int f = 0; f < 30 && !game.Player.IsMoving && game.State == GameState.Overworld; f++) Frames(1);
                engine.Steering = null;
                for (int f = 0; f < 90 && game.Player.IsMoving; f++) Frames(1);
                Frames(1);
            }
        }
        void Settle() { Until(() => game.Lead?.Moving != true, "the lead to catch up", 240); Frames(12); }
        string Tell() => game.Lead is { } lead
            ? $"walking {lead.Pokemon?.DisplayName} at {lead.Who.GridX},{lead.Who.GridY} (gap {lead.Gap}), player at {game.Player.GridX},{game.Player.GridY} on {game.Map.Name}"
            : $"nobody walks; player at {game.Player.GridX},{game.Player.GridY} on {game.Map.Name}, travelling {game.Player.Mode}";

        team.Clear();
        team.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 18));
        team.Add(new Pokemon(PokemonDatabase.Get("Starly")!, 15));
        engine.Settings.Follower = true;

        // ---- The lead behind the player in Twinleaf Town, standing and walking
        At("Sinnoh", 106, 887, Direction.Right);
        Walk(Direction.Right);
        Settle();
        Console.WriteLine($"cp01: {Tell()}");
        Shot("cp01_follower_behind");
        engine.Steering = (Direction.Right, false);
        for (int f = 0; f < 50; f++) Frames(1);
        Console.WriteLine($"cp02: {Tell()}, its walk blend {game.Lead?.Who.WalkBlend:0.00}");
        Shot("cp02_follower_walking");
        engine.Steering = null;
        Settle();
        Walk(Direction.Up, 1);
        Settle();
        Console.WriteLine($"cp03: {Tell()}");
        Shot("cp03_follower_turned");

        // ---- Through a door: Sandgem Town's Pokémon Center
        var door = sinnoh.Warps.First(w => w.TargetMap == "PokemonCenter");
        At("Sinnoh", door.SourceX, door.SourceY + 3, Direction.Up);
        Frames(20);
        for (int i = 0; i < 8 && game.Map.Name != "PokemonCenter"; i++) Walk(Direction.Up);
        Until(() => game.State == GameState.Overworld && game.Map.Name == "PokemonCenter", "the room");
        Frames(30);
        Settle();
        Console.WriteLine($"cp04: {Tell()}");
        Shot("cp04_follower_door");

        // ---- Over a ledge: the nearest south-facing ledge to Route 201 with open ground on both sides
        (int X, int Y)? ledge = null;
        for (int r = 0; r < 120 && ledge == null; r++)
            for (int dy = -r; dy <= r && ledge == null; dy++)
                for (int dx = -r; dx <= r && ledge == null; dx++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r) continue;
                    int lx = 116 + dx, ly = 870 + dy;
                    if (sinnoh.BehaviourAt(lx, ly) == TileBehavior.LedgeSouth && sinnoh.IsWalkable(lx, ly - 1) && sinnoh.IsWalkable(lx, ly - 2)
                        && sinnoh.IsWalkable(lx, ly + 1) && sinnoh.IsWalkable(lx, ly + 2) && !sinnoh.IsTallGrass(lx, ly + 1))
                        ledge = (lx, ly);
                }
        if (ledge is { } l)
        {
            At("Sinnoh", l.X, l.Y - 2, Direction.Down);
            Frames(20);
            Walk(Direction.Down);
            Walk(Direction.Down);
            // The player has hopped; a step on, and the lead takes the ledge after them
            engine.Steering = (Direction.Down, false);
            for (int f = 0; f < 120 && game.Lead?.Hopping != true; f++) Frames(1);
            engine.Steering = null;
            Frames(3);
            Console.WriteLine($"cp05: ledge at {l.X},{l.Y}; {Tell()}, hopping {game.Lead?.Hopping}, height {game.Lead?.Who.Hop:0.00}");
            Shot("cp05_follower_ledge");
            Settle();
        }
        else Console.WriteLine("  !! no ledge near Route 201");

        // ---- On a Bicycle it is back in its ball
        At("Sinnoh", 116, 888, Direction.Down);
        Frames(10);
        game.Player.SetCycling(true);
        Frames(20);
        Console.WriteLine($"cp06: {Tell()}");
        Shot("cp06_follower_cycling_recalled");
        game.Player.SetCycling(false);
        Frames(20);

        // ---- Spoken to: its band's line, a bubble and its cry; then something it found. A step aside first, so that it
        // stands beside the player and not behind their head
        Settle();
        Walk(Direction.Down);
        Walk(Direction.Right);
        Settle();
        if (game.Lead is { } walker)
        {
            game.Player.Facing = ScriptWay((game.Player.GridX, game.Player.GridY), (walker.Who.GridX, walker.Who.GridY));
            walker.Pokemon!.Friendship = 160;
            Frames(2);
            game.TalkToTheLead();
            Until(() => Box().IsActive, "the lead's line");
            Box().FinishLine();
            Frames(6);
            Console.WriteLine($"cp07: friendship {walker.Pokemon.Friendship}, band {LeadFollower.BandOf(walker.Pokemon.Friendship)}");
            Shot("cp07_follower_turns");
            for (int i = 0; i < 60 && (Box().IsActive || engine.ScriptRunning || game.State != GameState.Overworld); i++) { if (Box().IsActive) { Box().FinishLine(); Box().Advance(); } Frames(6); }
            engine.StartScript(FieldScripts.FollowerFound, walker.Who, new[] { Text.Of("followerTalk.found", ("name", walker.Pokemon.DisplayName)) }, item: ("Oran Berry", 1));
            Until(() => Box().IsActive, "the find's line");
            Box().FinishLine();
            Frames(6);
            Shot("cp08_follower_found_item");
            for (int i = 0; i < 60 && (Box().IsActive || engine.ScriptRunning || game.State != GameState.Overworld); i++) { if (Box().IsActive) { Box().FinishLine(); Box().Advance(); } Frames(6); }
            Console.WriteLine($"cp08: Oran Berries in the bag {game.Bag.GetQuantity(ItemDatabase.Get("Oran Berry")!)}");
        }
        engine.Settings.Follower = false;
        Frames(4);

        // ---- Wild Pokémon in sight on Route 201
        engine.Settings.WildInSight = true;
        (int X, int Y) spot = (116, 870);
        for (int r = 0; r < 60; r++)
        {
            bool found = false;
            for (int dy = -r; dy <= r && !found; dy++)
                for (int dx = -r; dx <= r && !found; dx++)
                {
                    int gx = 116 + dx, gy = 872 + dy;
                    if (sinnoh.IsWalkable(gx, gy) && !sinnoh.IsTallGrass(gx, gy) && sinnoh.IsTallGrass(gx, gy - 3) && sinnoh.IsTallGrass(gx + 2, gy - 2))
                    {
                        spot = (gx, gy);
                        found = true;
                    }
                }
            if (found) break;
        }
        At("Sinnoh", spot.X, spot.Y, Direction.Up);
        // Stood still, so that nothing is walked into, while they come out
        for (int f = 0; f < 60 * 12 && game.WildInSight.All.Count < FieldPokemon.MostInView; f++) { game.KeepWildInSight(1f / 60f); Frames(1); }
        Frames(30);
        foreach (var w in game.WildInSight.All)
            Console.WriteLine($"cp10: {w.Pokemon.DisplayName} Lv.{w.Pokemon.Level} at {w.Body.GridX},{w.Body.GridY}{(w.Water ? " on the water" : "")}, the table's {w.Entry.SpeciesName} {w.Entry.MinLevel}-{w.Entry.MaxLevel}");
        Shot("cp10_wild_in_the_grass");
        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        Frames(30);
        Shot("cp11_wild_in_the_grass_at_night");
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);
        Frames(10);
        if (game.WildInSight.All.FirstOrDefault() is { } met)
        {
            Console.WriteLine($"cp12: met {met.Pokemon.DisplayName} Lv.{met.Pokemon.Level}, from behind {FieldPokemon.FromBehind(game.Player.Facing, met.Body.Facing)}");
            game.MeetSeen(met);
            Frames(8);
            Shot("cp12_wild_touched");
            Until(() => game.State == GameState.Battle, "the battle", 600);
            Console.WriteLine($"cp12: the battle's foe is {game.Battle?.EnemyPokemon.DisplayName} Lv.{game.Battle?.EnemyPokemon.Level}, the one seen {(ReferenceEquals(game.Battle?.EnemyPokemon, met.Pokemon) ? "itself" : "!! another")}");
            if (game.Battle is { } battle) ToMainMenu(battle);
            Shot("cp13_wild_touched_battle");
        }
        engine.Settings.WildInSight = false;
    }

    static Direction ScriptWay((int X, int Y) from, (int X, int Y) to) =>
        to.Y < from.Y ? Direction.Up : to.Y > from.Y ? Direction.Down : to.X < from.X ? Direction.Left : Direction.Right;
}
