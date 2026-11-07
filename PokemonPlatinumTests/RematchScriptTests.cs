using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 41 · EG5: every Gym of Kanto and Unova fights its Leader again once a day after the region's Hall of Fame, and
/// Johto's Elite Four and Lance fight their second round in their rooms, each played headless on every way through its
/// script: the team of the rematch is fought only after the Hall of Fame, only once a day, and the day is told by
/// <c>FLAG_DAILY_…</c>, which <see cref="PokemonPlatinumEngine.Models.DailyEvents"/> clears with every day's other flags.
/// </summary>
public class RematchScriptTests
{
    private static readonly ScriptWalk Walk = new();

    /// <summary>The Gym's file, its Leader's script, the region, the first team, the rematch team, the daily flag and the flag of the Leader's TM.</summary>
    public static readonly (string File, string Script, string Region, string First, string Again, string Daily, string Tm)[] Gyms =
    {
        ("pewter_city_gym", "Brock", "Kanto", "kanto_leader_brock", "kanto_leader_brock_rematch", "FLAG_DAILY_KANTO_BROCK_REMATCH", "FLAG_GOT_TM39_FROM_BROCK"),
        ("cerulean_city_gym", "Misty", "Kanto", "kanto_leader_misty", "kanto_leader_misty_rematch", "FLAG_DAILY_KANTO_MISTY_REMATCH", "FLAG_GOT_TM03_FROM_MISTY"),
        ("vermilion_city_gym", "LtSurge", "Kanto", "kanto_leader_lt_surge", "kanto_leader_lt_surge_rematch", "FLAG_DAILY_KANTO_LT_SURGE_REMATCH", "FLAG_GOT_TM34_FROM_SURGE"),
        ("celadon_city_gym", "Erika", "Kanto", "kanto_leader_erika", "kanto_leader_erika_rematch", "FLAG_DAILY_KANTO_ERIKA_REMATCH", "FLAG_GOT_TM19_FROM_ERIKA"),
        ("fuchsia_city_gym", "Koga", "Kanto", "kanto_leader_koga", "kanto_leader_koga_rematch", "FLAG_DAILY_KANTO_KOGA_REMATCH", "FLAG_GOT_TM06_FROM_KOGA"),
        ("saffron_city_gym", "Sabrina", "Kanto", "kanto_leader_sabrina", "kanto_leader_sabrina_rematch", "FLAG_DAILY_KANTO_SABRINA_REMATCH", "FLAG_GOT_TM04_FROM_SABRINA"),
        ("cinnabar_island_gym", "Blaine", "Kanto", "kanto_leader_blaine", "kanto_leader_blaine_rematch", "FLAG_DAILY_KANTO_BLAINE_REMATCH", "FLAG_GOT_TM38_FROM_BLAINE"),
        ("aspertia_city_gym", "Cheren", "Unova", "unova_leader_cheren", "unova_leader_cheren_rematch", "FLAG_DAILY_UNOVA_CHEREN_REMATCH", "FLAG_UNOVA_GOT_TM83"),
        ("virbank_city_gym_2", "Roxie", "Unova", "unova_leader_roxie", "unova_leader_roxie_rematch", "FLAG_DAILY_UNOVA_ROXIE_REMATCH", "FLAG_UNOVA_GOT_TM09"),
        ("castelia_city_gym", "Burgh", "Unova", "unova_leader_burgh", "unova_leader_burgh_rematch", "FLAG_DAILY_UNOVA_BURGH_REMATCH", "FLAG_UNOVA_GOT_TM76"),
        ("nimbasa_city_gym", "Elesa", "Unova", "unova_leader_elesa", "unova_leader_elesa_rematch", "FLAG_DAILY_UNOVA_ELESA_REMATCH", "FLAG_UNOVA_GOT_TM72"),
        ("DriftveilCity_Gym", "Clay", "Unova", "unova_leader_clay", "unova_leader_clay_rematch", "FLAG_DAILY_UNOVA_CLAY_REMATCH", "FLAG_UNOVA_GOT_TM78"),
        ("mistralton_city_gym", "Skyla", "Unova", "unova_leader_skyla", "unova_leader_skyla_rematch", "FLAG_DAILY_UNOVA_SKYLA_REMATCH", "FLAG_UNOVA_GOT_TM62"),
        ("opelucid_city_gym", "Drayden", "Unova", "unova_leader_drayden", "unova_leader_drayden_rematch", "FLAG_DAILY_UNOVA_DRAYDEN_REMATCH", "FLAG_UNOVA_GOT_TM82"),
        ("humilau_city_gym", "Marlon", "Unova", "unova_leader_marlon", "unova_leader_marlon_rematch", "FLAG_DAILY_UNOVA_MARLON_REMATCH", "FLAG_UNOVA_GOT_TM55"),
    };

    /// <summary>Johto's rooms of the League: file, script, first team, second round.</summary>
    public static readonly (string File, string Script, string First, string Again)[] Rooms =
    {
        ("pokemon_league_will_room", "Will", "johto_elite_four_will_will", "johto_elite_four_will_will_2"),
        ("pokemon_league_koga_room", "Koga", "johto_elite_four_koga_koga", "johto_elite_four_koga_koga_2"),
        ("pokemon_league_bruno_room", "Bruno", "johto_elite_four_bruno_bruno", "johto_elite_four_bruno_bruno_2"),
        ("pokemon_league_karen_room", "Karen", "johto_elite_four_karen_karen", "johto_elite_four_karen_karen_2"),
        ("pokemon_league_lance_room", "Lance", "johto_champion_lance", "johto_champion_lance_2"),
    };

    private static List<ScriptWalk.Way> Ways(string file, string script, Action<HeadlessScriptHost> before)
    {
        var starter = Walk.Starters().First(s => s.Script == script && s.File == file);
        return Walk.EveryWayThrough(ScriptWalk.Scripts.Find(script, file)!, starter.Map, starter.Person, before: before);
    }

    private static bool Fought(ScriptWalk.Way way, string team) => way.Host.Log.Any(l => l.StartsWith("battle " + team + " ", StringComparison.Ordinal));

    public static IEnumerable<object[]> GymRows() => Gyms.Select(g => new object[] { g.File, g.Script });
    public static IEnumerable<object[]> RoomRows() => Rooms.Select(r => new object[] { r.File, r.Script });

    [Theory]
    [MemberData(nameof(GymRows))]
    public void AGymFightsItsLeaderAgainOnceADayAfterTheHallOfFame(string file, string script)
    {
        var g = Gyms.Single(x => x.File == file && x.Script == script);
        void Done(HeadlessScriptHost h, bool hallOfFame, bool today)
        {
            h.Story.SetBadges(g.Region, 0xFF);
            h.Story.Set(g.Tm);
            if (hallOfFame) h.Story.Set(g.Region + "HallOfFame");
            if (today) h.Story.Set(g.Daily);
        }

        // Before the Hall of Fame, and after it with the day's battle already fought: never the second team
        foreach (var (hall, today) in new[] { (false, false), (true, true) })
            foreach (var way in Ways(file, script, h => Done(h, hall, today)))
                Assert.False(Fought(way, g.Again), $"{script}: the rematch team fought with hallOfFame={hall}, today={today}");

        // After it, on a fresh day: asked, and the second team is fought on the way that says yes, once the day is marked
        var fresh = Ways(file, script, h => Done(h, true, false));
        var yes = fresh.Where(w => Fought(w, g.Again)).ToList();
        Assert.NotEmpty(yes);
        Assert.All(yes, w => Assert.True(w.Host.Story.Has(g.Daily) || w.Host.Outcome != BattleOutcome.Won, $"{script}: the day is not marked after a win"));
        Assert.Contains(yes, w => w.Host.Story.Has(g.Daily));
        // ... and the first team never, because the Badge is already held
        Assert.DoesNotContain(fresh, w => Fought(w, g.First));
        // A way that says no fights nobody
        Assert.Contains(fresh, w => !Fought(w, g.Again));
    }

    [Theory]
    [MemberData(nameof(RoomRows))]
    public void JohtosLeagueFightsItsSecondRoundOnceTheHallOfFameIsEntered(string file, string script)
    {
        var r = Rooms.Single(x => x.File == file && x.Script == script);
        var first = Ways(file, script, h => h.Story.SetBadges("Johto", 0xFF));
        Assert.Contains(first, w => Fought(w, r.First));
        Assert.DoesNotContain(first, w => Fought(w, r.Again));
        var again = Ways(file, script, h => { h.Story.SetBadges("Johto", 0xFF); h.Story.Set("JohtoHallOfFame"); });
        Assert.Contains(again, w => Fought(w, r.Again));
        Assert.DoesNotContain(again, w => Fought(w, r.First));
    }
}
