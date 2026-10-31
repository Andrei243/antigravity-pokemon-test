using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    /// <summary>
    /// Mirage Island (plan 21 · H12, part h), after part g's shots at the end of the <c>hoenn</c> mode
    /// (<c>HOENN_FROM=postgame</c>): Pacifidlog's old man on a day the island isn't there (<c>h2d0</c>), the sea of Route
    /// 130 without it (<c>h2d1</c>), the day's number made the lead's so the island rises (<c>h2d2</c>, from its shore,
    /// with its berry tree), and the old man that day (<c>h2d3</c>). It prints the day's number, the lead's personality
    /// and the flag at each. Written, not run, by the session that built it.
    /// </summary>
    void HoennMirageIslandShots()
    {
        if (!HoennReady("Mirage Island")) return;
        DialogueManager Box() => game.Dialogue;
        void Settle()
        {
            for (int i = 0; i < 1200 && (engine.ScriptRunning || Box().IsActive); i++)
            {
                if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
                else Frames(1);
            }
            Frames(10);
        }
        void Report(string when) => Console.WriteLine(
            $"  {when}: day's number {MirageIsland.NumberOf(game.Encounters):X4}, lead {(game.Party.Members.FirstOrDefault()?.Personality ?? 0) & 0xFFFF:X4}, island {game.Story.Has(MirageIsland.Flag)}");
        void OldMan(string shot)
        {
            At("PacifidlogTown_House5", 9, 5, Direction.Up);
            game.ArriveOnMap(); Frames(10);
            Settle();
            var man = game.Map.NPCs.FirstOrDefault(n => n.Key == "expert_m");
            if (man == null) { Console.WriteLine("  !! the old man is not in his house"); return; }
            At("PacifidlogTown_House5", man.GridX + 1, man.GridY, Direction.Left);
            game.Interact(); Frames(3);
            for (int i = 0; i < 600 && !(Box().IsActive && Box().IsCurrentLineComplete); i++) Frames(1);
            Shot(shot);
            Settle();
        }

        game.State = GameState.Overworld;
        Settle();
        game.LocationSign.Hide();
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        var lead = game.Party.Members.FirstOrDefault();
        if (lead == null) { Console.WriteLine("  !! nobody in the team"); return; }
        // A day whose number matches nobody's
        game.Encounters.DailyNumber = ((lead.Personality + 1) & 0xFFFF) << 16;
        OldMan("h2d0_pacifidlog_old_man_not_today");
        At("Hoenn", 612, 280, Direction.Up);
        game.EnterArea(); Settle();
        Report("an ordinary day");
        Shot("h2d1_route130_no_island");

        // The day's number made the lead's: the island rises as the route is come to
        game.Encounters.DailyNumber = (lead.Personality & 0xFFFF) << 16;
        At("Hoenn", 640, 280, Direction.Left);
        game.EnterArea(); Settle();
        At("Hoenn", 612, 280, Direction.Up);
        game.EnterArea(); Settle();
        Report("the lead's day");
        // stand on the island's shore nearest the berry tree, facing it
        var shore = Enumerable.Range(0, 12).SelectMany(r => Enumerable.Range(-r, 2 * r + 1).Select(dx => (X: 612 + dx, Y: 269 + r)))
            .FirstOrDefault(t => t.Y > 269 && game.Map.IsWalkable(t.X, t.Y));
        if (shore != default) At("Hoenn", shore.X, shore.Y, Direction.Up);
        else Console.WriteLine("  !! no land south of the berry tree");
        Frames(20);
        Shot("h2d2_route130_mirage_island");
        OldMan("h2d3_pacifidlog_old_man_island_risen");
    }
}
