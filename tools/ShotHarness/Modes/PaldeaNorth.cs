using System;
using System.Linq;
using System.Threading;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Core.Autoplay;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.OpenWorld;

partial class Harness
{
    /// <summary>
    /// The North Province and the last three gyms (plan 34 · PD6, its second session; the <c>paldea</c> mode,
    /// <c>SHOTS_FILTER=pdn</c>): the North Province's first area, Glaseado's foot and its snowslope from below and from the
    /// top, the gym and a gate of the run, Montenevera and its gym, the East Province's third area, Lurking Steel and
    /// Casseroya Lake (the last two Titans moved), Alfornada Cavern, Alfornada, its gym and its ruins (<c>pdn00</c> to
    /// <c>pdn16</c>, Montenevera after dark <c>pdn08n</c>), the nine rooms (<c>pdn20</c> to <c>pdn28</c>), the three leaders
    /// at the far end of their halls (<c>pdn30</c> to <c>pdn32</c>), and last the ride (<c>pdn90</c>): the playthrough bot on
    /// the mount from Cabo Poco's lane to Glaseado's top (<see cref="Itineraries.PaldeaToGlaseadosTop"/>), drawn every
    /// frame at sixty a second, printing every chunk the frame had to wait for, the streaming's sums and the share of the
    /// ride ridden. Its milliseconds want the Windows machine, quiet; the waits are counted here.
    /// </summary>
    void PaldeaNorth(OpenMap paldea)
    {
        ShootAt(paldea, "pdn00_north_province", "north_one_south");
        ShootAt(paldea, "pdn01_north_province_north", "north_one_north");
        // The snowslope from its foot, up to the top; the top; the gym from the terrace; down the slope from the start
        ShootToward(paldea, "pdn02_glaseado_from_the_foot", "glaseado_foot", 40, -1080, 160);
        ShootAt(paldea, "pdn03_glaseado_top", "glaseado_top");
        ShootToward(paldea, "pdn04_glaseado_gym", "glaseado_top", 24, -1104, 22);
        ShootToward(paldea, "pdn05_the_run_from_the_top", "glaseado_run_start", 40, -920, 145);
        ShootToward(paldea, "pdn06_a_gate_of_the_run", "glaseado_run_start", 10, -990, 26);
        ShootAt(paldea, "pdn07_montenevera", "montenevera_west");
        ShootToward(paldea, "pdn08_montenevera_gym", "montenevera_square", 322, -986, 24);
        ShootAt(paldea, "pdn09_east_province_three", "east_three_south");
        ShootAt(paldea, "pdn10_lurking_steel", "lurking_steel_mouth");
        ShootAt(paldea, "pdn11_casseroya_lake", "false_dragon_mouth");
        ShootAt(paldea, "pdn12_alfornada_cavern", "alfornada_cavern_middle");
        ShootAt(paldea, "pdn13_alfornada", "alfornada_east");
        ShootToward(paldea, "pdn14_alfornada_gym", "alfornada_square", -640, -368, 22);
        ShootToward(paldea, "pdn15_alfornada_ruins", "alfornada_ruins", -660, -298, 16);
        // Montenevera's square after dark
        if (Wanted("pdn08n_montenevera_gym"))
        {
            var hour = engine.Settings.TimeOfDay;
            engine.Settings.TimeOfDay = TimeOfDay.Night;
            engine.ApplySettings(window: false);
            ShootToward(paldea, "pdn08n_montenevera_gym", "montenevera_square", 322, -986, 24);
            engine.Settings.TimeOfDay = hour;
            engine.ApplySettings(window: false);
        }
        foreach (var (shot, room) in new[]
        {
            ("pdn20_montenevera_center", "Montenevera_PokemonCenter"),
            ("pdn21_montenevera_gym", "Montenevera_Gym"),
            ("pdn22_montenevera_hall", "Montenevera_GymTest"),
            ("pdn23_alfornada_center", "Alfornada_PokemonCenter"),
            ("pdn24_alfornada_gym", "Alfornada_Gym"),
            ("pdn25_alfornada_studio", "Alfornada_GymTest"),
            ("pdn26_glaseado_center", "Glaseado_PokemonCenter"),
            ("pdn27_glaseado_gym", "Glaseado_Gym"),
            ("pdn28_glaseado_hall", "Glaseado_GymTest"),
        })
            ShootRoom(shot, room);
        foreach (var (shot, room) in new[] { ("pdn30_ryme", "Montenevera_GymTest"), ("pdn31_tulip", "Alfornada_GymTest"), ("pdn32_grusha", "Glaseado_GymTest") })
        {
            if (!Wanted(shot)) continue;
            var map = MapDatabase.Get(room);
            game.Place(map, 6, 6, Direction.Up);
            game.LocationSign.Hide();
            Frames(40);
            Shot(shot);
            Console.WriteLine($"  {shot}: {room} at 6,6, {map.NPCs.Count} people: {string.Join(", ", map.NPCs.Select(n => n.Name))}");
        }
        PaldeaRide(paldea);
    }

    /// <summary>
    /// Plan 34 · PD6's done-when, drawn: the bot rides from Cabo Poco's lane to Glaseado's top, mounted again whenever
    /// something put it down, every frame drawn and paced at sixty a second so the stream's worker runs as in a game.
    /// </summary>
    void PaldeaRide(OpenMap paldea)
    {
        if (!Wanted("pdn90_the_ride")) return;
        var renderer = game.OpenRenderer;
        game.Story.SetVar("VAR_PALDEA_STORY", 7);
        game.Story.SetVar(MountRules.MountVariable, MountRules.Koraidon);
        game.Story.Set("FLAG_PALDEA_MOUNT_DASH");
        foreach (var (species, level) in new[] { ("Pawmot", 75), ("Skeledirge", 75), ("Garganacl", 75) })
            if (game.Party.Count < 6) game.Party.Add(new Pokemon(PokemonDatabase.Get(species)!, level));
        game.Flow.EncounterAids.RepelSteps = 1_000_000;
        ShootAt(paldea, "pdn90_the_ride_start", "cabo_poco_lane");
        renderer = game.OpenRenderer;
        if (renderer == null) { Console.WriteLine("!! no open renderer for the ride"); return; }
        var bot = new Autoplayer(engine, seed: 1) { Itinerary = new Itinerary(Itineraries.PaldeaToGlaseadosTop) };
        int startWaits = renderer.Stream.Waits, frames = 0, ridden = 0, waited = 0;
        double total = 0, worst = 0;
        string worstAt = "";
        var watch = new System.Diagnostics.Stopwatch();
        for (int f = 0; f < 60 * 60 * 20 && !bot.Itinerary.Finished && !bot.Stopped; f++)
        {
            if (engine.InOpenField && game.State == GameState.Overworld && !game.OpenMounted && !engine.ScriptRunning && game.WhyNotMount == MountRefusal.None)
                game.ToggleMount();
            watch.Restart();
            FrameClock.Fixed = ++tick / 60.0;
            int waits = renderer.Stream.Waits;
            bot.Frame(1f / 60f);
            engine.Draw();
            if (renderer.Stream.Waits > waits)
            {
                waited += renderer.Stream.Waits - waits;
                Console.WriteLine($"!! a chunk waited for at {game.OpenPlayer?.Pos} ({paldea.AreaAt(game.OpenPlayer!.Pos)?.Key ?? "no area"}), leg {bot.Itinerary.Current}");
            }
            double ms = watch.Elapsed.TotalMilliseconds;
            total += ms; frames++;
            if (game.OpenMounted) ridden++;
            if (ms > worst) { worst = ms; worstAt = $"{game.OpenPlayer?.Pos} ({paldea.AreaAt(game.OpenPlayer!.Pos)?.Key ?? "no area"})"; }
            while (watch.Elapsed.TotalMilliseconds < 1000.0 / 60.0) Thread.Sleep(1);
        }
        bot.Release();
        game.LocationSign.Hide();
        Frames(20);
        Shot("pdn90_the_ride");
        Console.WriteLine($"  pdn90_the_ride: legs {string.Join(", ", bot.Report.Legs)}{(bot.Report.LegsGivenUp.Count > 0 ? $"; !! given up {string.Join(", ", bot.Report.LegsGivenUp)}" : "")}");
        Console.WriteLine($"  the ride: {frames} frames ({frames / 60.0:0} s), ridden {100.0 * ridden / Math.Max(1, frames):0}% of them, ended at {game.OpenPlayer?.Pos} "
            + $"({paldea.AreaAt(game.OpenPlayer!.Pos)?.Key ?? "no area"}), {waited} chunks waited for ({renderer.Stream.Waits - startWaits} by the stream's count), "
            + $"{total / Math.Max(1, frames):F2} ms/frame, worst {worst:F1} ms at {worstAt} (milliseconds owed to the quiet Windows machine)");
        Console.WriteLine($"  streaming: {renderer.Stream.Chunks} chunks made ready, worst upload frame {renderer.Stream.WorstFrameMs:F1} ms");
    }
}
