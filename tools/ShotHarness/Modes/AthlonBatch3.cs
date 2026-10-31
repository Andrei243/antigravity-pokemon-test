using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    /// <summary>
    /// The Pokéathlon's third batch (plan 15 · E8), at the end of the <c>athlon</c> mode: the Stamina Course from the
    /// clerk (Relay Run shot, <c>at12_relay_run</c>), the Power Course (Circle Push and Goal Roll, <c>at13_</c>,
    /// <c>at14_</c>) to its scoreboard with its medal (<c>at14c_power_scoreboard</c>), the records desk's board
    /// (<c>at15_records</c>) and the Athlete Shop's stall (<c>at16_shop</c>, a stone bought, <c>at16b_shop_bought</c>). It
    /// prints each event's points, the medals and the points spent.
    /// </summary>
    private void AthlonBatch3(AthlonScreen screen, System.Action<System.Func<bool>, string, int> until, System.Action<string, string> readOn)
    {
        void Course(string key)
        {
            At("PokeathlonDome_1F", 23, 10, Direction.Up);
            Frames(10);
            game.Interact();
            Frames(3);
            readOn(null, null);
            until(() => game.State == GameState.Athlon, "the Pokéathlon's screen", 300);
            Frames(10);
            int index = AthlonCourses.All.ToList().FindIndex(c => c.Key == key);
            for (int i = 0; i < index; i++) screen.Move(0, 1);
            Frames(20);
            if (key == "power") Shot("at11z_course_choice_power");
            screen.Confirm();
            Frames(10);
            screen.Confirm();          // Quilava
            screen.Move(0, 1);
            screen.Confirm();          // Pikachu
            screen.Move(0, 1);
            screen.Move(0, 1);
            screen.Confirm();          // Machop
            Frames(20);
        }
        void Event(string shot, int frames)
        {
            screen.Confirm();
            Frames(40);
            screen.Play(frames, ev => ev.Autopilot());
            Frames(1);
            if (shot != null) Shot(shot);
            screen.Play(int.MaxValue, ev => ev.Autopilot());
            Frames(30);
            int e = screen.Run!.EventIndex - 1;
            Console.WriteLine($"  {screen.Run.Course.Events[e]}: " + string.Join(", ",
                Enumerable.Range(0, AthlonRun.TeamCount).Select(t => $"{(t == 0 ? "player" : screen.Run.Teams[t].Name)} {screen.Run.PointsOf(e, t)}")));
            screen.Confirm();
            Frames(20);
        }
        void Finish(string shot)
        {
            if (shot != null) Shot(shot);
            Console.WriteLine($"  {screen.Run!.Course.Key}: earned {screen.Run.Earned}, place {screen.Outcome}, medal {screen.Medal}");
            screen.Confirm();
            until(() => game.State is GameState.Overworld or GameState.Dialogue, "the field again", 120);
            readOn(null, null);
        }

        // ---- The Stamina Course: Ring Drop, Relay Run, Block Smash
        Course("stamina");
        Event(null, 0);
        Shot("at12a_briefing_relay_run");
        Event("at12_relay_run", 600);
        Event(null, 0);
        Finish(null);

        // ---- The Power Course: Block Smash, Circle Push, Goal Roll, and its medal
        Course("power");
        Event(null, 0);
        Shot("at13a_briefing_circle_push");
        Event("at13_circle_push", 420);
        Shot("at14a_briefing_goal_roll");
        Event("at14_goal_roll", 900);
        Finish("at14c_power_scoreboard");

        // ---- The records desk, east of the course desk
        var desk = game.Map.Everyone.First(n => n.Script == "AthlonRecords");
        At("PokeathlonDome_1F", desk.GridX, desk.GridY + 2, Direction.Up);
        Frames(10);
        game.Interact();
        Frames(3);
        readOn(null, null);
        until(() => game.State == GameState.Athlon, "the records board", 300);
        Frames(30);
        Shot("at15_records");
        Console.WriteLine("  medals: " + string.Join(", ", AthlonCourses.All.Select(c => $"{c.Key} {game.Athlon.BestOf(c.Key)} {game.Athlon.MedalOf(c.Key)}")));
        screen.Confirm();
        until(() => game.State is GameState.Overworld or GameState.Dialogue, "the field again", 120);
        readOn(null, null);

        // ---- The Athlete Shop's stall on the square, a stone bought with the points
        game.Athlon.Earn(3000);
        At("PokeathlonDome", 22, 25, Direction.Up);
        Frames(20);
        game.Interact();
        Frames(3);
        readOn(null, null);
        until(() => game.State == GameState.Shop, "the Athlete Shop", 300);
        var shop = game.Shop;
        int stone = shop.Stock.ToList().FindIndex(i => i.Name == "Fire Stone");
        for (int i = 0; i < stone; i++) shop.Move(0, 1, game.Athlon.Points);
        Frames(30);
        Shot("at16_shop");
        int before = game.Athlon.Points;
        shop.Confirm(game.Bag, game.Athlon.Points, _ => { });
        Frames(20);
        Shot("at16a_shop_how_many");
        game.Athlon.Points += shop.Confirm(game.Bag, game.Athlon.Points, _ => { });
        Frames(20);
        Shot("at16b_shop_bought");
        Console.WriteLine($"  shop: {before} points, {game.Athlon.Points} after, Fire Stones {game.Bag.GetQuantity(PokemonPlatinumEngine.Data.ItemDatabase.Get("Fire Stone")!)}");
        shop.Cancel();
        until(() => game.State is GameState.Overworld or GameState.Dialogue, "the field again", 120);
        readOn(null, null);
    }
}
