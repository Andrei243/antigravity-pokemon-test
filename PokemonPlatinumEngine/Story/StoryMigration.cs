using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// What a new game's story begins with, and how the story of a save written by an older game is brought up to
/// date (plan 02 · S1). Every chapter that changes what an existing save has to know (a flag that must already be
/// set, people who must already have left) raises <see cref="StoryState.CurrentVersion"/> and adds its step here,
/// so a save is taken through each step it hasn't had, oldest first.
/// </summary>
public static class StoryMigration
{
    /// <summary>The three starters and what each grows into: how a save from before the choice was recorded is told which was taken.</summary>
    private static readonly string[][] StarterLines =
    {
        new[] { "Turtwig", "Grotle", "Torterra" },
        new[] { "Chimchar", "Monferno", "Infernape" },
        new[] { "Piplup", "Prinplup", "Empoleon" }
    };

    /// <summary>
    /// Sets what every game starts with: the script <see cref="ScriptLibrary.NewGame"/>, which may change flags and
    /// variables and nothing else (people hidden until their scene comes are hidden by flags set here).
    /// </summary>
    public static void BeginNewGame(StoryState story, ScriptLibrary scripts)
    {
        if (scripts.Find(ScriptLibrary.NewGame) is not { } script) return;
        var runner = new ScriptRunner(scripts, new HeadlessScriptHost(story) { ShowsNothing = true });
        runner.Start(script);
        runner.RunToEnd();
        // And each region's own (plan 19 · K3: common.KantoNewGame), whose flags are the region's original's
        foreach (string regional in RegionalNewGames) Run(story, scripts, regional);
    }

    /// <summary>The scripts of what a game starts with in each region beside Sinnoh's: <c>common.&lt;Region&gt;NewGame</c>.</summary>
    public static IEnumerable<string> RegionalNewGames =>
        RegionDatabase.All.Where(r => r.Id != RegionDatabase.Sinnoh).Select(r => $"common.{r.Id}NewGame");

    /// <summary>
    /// Brings a loaded save's story up to today's. <paramref name="savedVersion"/> is the save's
    /// <c>StoryVersion</c>; <paramref name="owned"/> is every Pokémon the player has, on the team and in the PC, and
    /// <paramref name="bag"/> the player's bag, for what a step hands over.
    /// </summary>
    public static void Upgrade(StoryState story, int savedVersion, IEnumerable<Pokemon> owned, ScriptLibrary scripts, Inventory? bag = null)
    {
        if (savedVersion < 1) FromBeforeTheStory(story, owned, scripts);
        if (savedVersion < 2) PastTheOpening(story, scripts);
        if (savedVersion < 3) Run(story, scripts, ScriptLibrary.ChapterTwo);
        else if (savedVersion < 4) Run(story, scripts, ScriptLibrary.ChapterThree);
        if (savedVersion < 4) RenameTrainers(story);
        if (savedVersion < 5) HandOverRockSmash(story, bag);
        if (savedVersion < 6) RenameTrainers(story, SouthWestTrainers);
        // Version 7: the people who come and go with the clock, and the Windworks' Drifloon (plan 06 · R14a)
        if (savedVersion < 7) Run(story, scripts, ScriptLibrary.TimedPeople);
        // Version 8: the Pokétch campaign's clowns gone after the north gate's battle, the third from Jubilife TV's door
        if (savedVersion < 8) Run(story, scripts, ScriptLibrary.CampaignOver);
        // Version 9: the fourth chapter's people kept out of sight until their scenes (plan 02 · S7)
        if (savedVersion < 9) Run(story, scripts, ScriptLibrary.ChapterFour);
        // Version 10: the fifth chapter's (plan 02 · S8), and the gate to Route 212 opened for a save that has been to
        // Pastoria City, whose arrival lifts its television crew's blockade
        if (savedVersion < 10) ChapterFive(story, scripts);
        // Version 11: the sixth chapter's (plan 02 · S9), the Old Charm for a save that cured the Psyduck before
        // Cynthia came up the road after them, Route 218's blockade gone for a save that has been to Canalave City,
        // and the Vs. Seeker's first level of rematches for a save that has the Vs. Seeker
        if (savedVersion < 11) ChapterSix(story, scripts, bag);
        // Version 12: the seventh chapter's (plan 02 · S10), and Mt. Coronet's northern hall clear of Team Galactic's
        // grunts, with Maylene past on Route 217, for a save that has already been to Snowpoint City
        if (savedVersion < 12) ChapterSeven(story, scripts);
        // Version 13: the eighth chapter's (plan 02 · S11, part a), and the rival gone from the head of Acuity
        // Lakefront's rock face for a save that has already been past it to Snowpoint City
        if (savedVersion < 13) ChapterEight(story, scripts);
        // Version 14: the people of Team Galactic's headquarters (plan 02 · S11, part b), and the grunt with the Storage
        // Key shown again for a save that has already seen Commander Jupiter at Lake Acuity
        if (savedVersion < 14) Headquarters(story, scripts);
        // Version 15: the people of Spear Pillar and the Distortion World (plan 02 · S12), and Cynthia gone from before
        // Turnback Cave's mouth for a save that has already entered the Hall of Fame
        if (savedVersion < 15) ChapterNine(story, scripts);
        // Version 16: the people of the tenth chapter (plan 02 · S13), a guardian of the lakes already the player's
        // counted as caught, and Sunyshore City as its scenes leave it for a save that has already been there or holds
        // its Beacon Badge
        if (savedVersion < 16) ChapterTen(story, scripts, owned);
        // Version 17: the people of the eleventh chapter (plan 02 · S14), and everything the Hall of Fame's scene sets
        // for a save that entered it before that scene was written
        if (savedVersion < 17) ChapterEleven(story, scripts);
        // Version 18: the people of the time after the Hall of Fame (plan 02 · S15, part a), and the Fight Area's and
        // Route 227's first scenes counted as had, with the roads out of the town open, for a save that has already
        // been to the Battle Zone
        if (savedVersion < 18) ChapterTwelve(story, scripts);
        // Version 19: the badges by region (plan 18 · W2). A save from before kept one mask of Sinnoh's eight; it is
        // given to the region the save was made in as the story is read (Core.SaveData.RegionalBadges), before the first
        // step here, since the steps above ask for the Coal, Icicle and Beacon Badges. Nothing is left to do here.
        // Version 20: Kanto's first chapter (plan 19 · K3), and a game begun in Kanto before it was written taken past it
        if (savedVersion < 20) KantoChapterOne(story, scripts, owned);
        // Version 21: Johto's first chapter (plan 20 · J4), and a game begun in Johto before it was written taken past it
        if (savedVersion < 21) JohtoChapterOne(story, scripts, owned);
        // Version 22: the Pokégear's phone (plan 20 · J3): a game of Johto that has the Pokégear has Mom's number in
        // it, and Professor Elm's once his Pokémon is had, as the scenes that give them now register them
        if (savedVersion < 22) Run(story, scripts, "common.JohtoPokegear");
        // Version 23: Hoenn's first chapter (plan 21 · H2), and a game begun in Hoenn before it was written taken past it
        if (savedVersion < 23) HoennChapterOne(story, scripts, owned);
        // Version 24: Hoenn's second stretch (plan 21 · H2, part b): the people of Route 104 to Rusturf Tunnel are hidden
        // until their scenes by common.HoennChapterTwo, which a new game runs too
        if (savedVersion < 24) Run(story, scripts, "common.HoennChapterTwo");
        // Version 25: Johto's second chapter (plan 20 · J7): its people are hidden until their scenes by
        // common.JohtoChapterTwo, which a new game runs too (the strange tree on Route 36 stands again in the way it kept)
        if (savedVersion < 25) Run(story, scripts, "common.JohtoChapterTwo");
        // Version 26: Hoenn's third stretch (plan 21 · H3): the people of Dewford Town to Slateport City are hidden until
        // their scenes by common.HoennChapterThree, which a new game runs too
        if (savedVersion < 26) Run(story, scripts, "common.HoennChapterThree");
    }

    /// <summary>
    /// Version 21, a save from before Johto's first chapter was written (plan 20 · J4): its Johto people are hidden until
    /// their scenes by <c>common.JohtoNewGame</c>, which a new game runs too. A game begun in Johto before then began in
    /// front of the player's house with Elm's first Pokémon and no choice recorded: it has its opening played by
    /// <c>common.JohtoOpeningDone</c>, and its starter is the first of Elm's three it owns.
    /// </summary>
    private static void JohtoChapterOne(StoryState story, ScriptLibrary scripts, IEnumerable<Pokemon> owned)
    {
        Run(story, scripts, "common.JohtoNewGame");
        var mine = owned.ToList();
        if (story.PlayerStarter != null || StarterAmong(mine) != null) return;
        var species = mine.Select(p => p.Species.Name).ToHashSet(StringComparer.Ordinal);
        if (JohtoStarterLines.FirstOrDefault(line => line.Any(species.Contains))?[0] is not { } starter) return;
        Run(story, scripts, "common.JohtoOpeningDone");
        story.ChooseStarter(starter);
    }

    /// <summary>Elm's three and what each grows into (plan 20 · J4).</summary>
    private static readonly string[][] JohtoStarterLines =
    {
        new[] { "Chikorita", "Bayleef", "Meganium" },
        new[] { "Cyndaquil", "Quilava", "Typhlosion" },
        new[] { "Totodile", "Croconaw", "Feraligatr" }
    };

    /// <summary>
    /// Version 20, a save from before Kanto's first chapter was written (plan 19 · K3): its Kanto people are hidden until
    /// their scenes by <c>common.KantoNewGame</c>, which a new game runs too. A game begun in Kanto before then began in
    /// front of the player's house with a Pokémon of its own and no choice of starter recorded, which no game of Sinnoh
    /// has (Sinnoh's begins with none and records the briefcase's): it has its opening played by
    /// <c>common.KantoOpeningDone</c>, and its starter is the first of Oak's three it owns.
    /// </summary>
    private static void KantoChapterOne(StoryState story, ScriptLibrary scripts, IEnumerable<Pokemon> owned)
    {
        Run(story, scripts, "common.KantoNewGame");
        var mine = owned.ToList();
        // A game of Kanto's stand-in start owned one of Oak's three and none of the briefcase's (a game of Sinnoh made
        // by a tool may have its Turtwig with no choice recorded)
        if (story.PlayerStarter != null || StarterAmong(mine) != null) return;
        var species = mine.Select(p => p.Species.Name).ToHashSet(StringComparer.Ordinal);
        if (KantoStarterLines.FirstOrDefault(line => line.Any(species.Contains))?[0] is not { } starter) return;
        Run(story, scripts, "common.KantoOpeningDone");
        story.ChooseStarter(starter);
    }

    /// <summary>
    /// Version 23, a save from before Hoenn's first chapter was written (plan 21 · H2): its Hoenn people are hidden until
    /// their scenes by <c>common.HoennNewGame</c>, which a new game runs too. A game begun in Hoenn before then began in
    /// front of the family's house with Birch's first Pokémon and no choice recorded: it has its opening played by
    /// <c>common.HoennOpeningDone</c>, and its starter is the first of Birch's three it owns.
    /// </summary>
    private static void HoennChapterOne(StoryState story, ScriptLibrary scripts, IEnumerable<Pokemon> owned)
    {
        Run(story, scripts, "common.HoennNewGame");
        var mine = owned.ToList();
        if (story.PlayerStarter != null || StarterAmong(mine) != null) return;
        var species = mine.Select(p => p.Species.Name).ToHashSet(StringComparer.Ordinal);
        if (HoennStarterLines.FirstOrDefault(line => line.Any(species.Contains))?[0] is not { } starter) return;
        Run(story, scripts, "common.HoennOpeningDone");
        story.ChooseStarter(starter);
    }

    /// <summary>Birch's three and what each grows into (plan 21 · H2).</summary>
    private static readonly string[][] HoennStarterLines =
    {
        new[] { "Treecko", "Grovyle", "Sceptile" },
        new[] { "Torchic", "Combusken", "Blaziken" },
        new[] { "Mudkip", "Marshtomp", "Swampert" }
    };

    /// <summary>Oak's three and what each grows into (plan 19 · K3).</summary>
    private static readonly string[][] KantoStarterLines =
    {
        new[] { "Bulbasaur", "Ivysaur", "Venusaur" },
        new[] { "Charmander", "Charmeleon", "Charizard" },
        new[] { "Squirtle", "Wartortle", "Blastoise" }
    };

    /// <summary>The flags that first arriving in the Battle Zone's three towns sets (<see cref="Data.SpawnLocation.ArrivalFlag"/>).</summary>
    public static readonly string[] ArrivedInTheBattleZone =
    {
        "FLAG_FIRST_ARRIVAL_FIGHT_AREA", "FLAG_FIRST_ARRIVAL_SURVIVAL_AREA", "FLAG_FIRST_ARRIVAL_RESORT_AREA"
    };

    /// <summary>
    /// Version 18, a save from before the time after the Hall of Fame was written (plan 02 · S15, part a): its people
    /// are hidden until their scenes by <see cref="ScriptLibrary.ChapterTwelve"/>, which a new game runs too (called by
    /// the chapter before).
    /// <para>The rival waits on the Fight Area's pier for whoever the ship first brings, takes them to the Battle
    /// Frontier's gate, and the battle fought there beside him against Flint and Volkner is what opens the roads out
    /// of the town, with the National Pokédex. A save that has already been to the Battle Zone came when nobody stood
    /// on the pier, before the gate or across those roads, and may have saved anywhere beyond them: for it that scene
    /// has been played (everyone it sends away is gone, and the two pairs who keep the roads with them, whatever its
    /// Pokédex), and so has the rival's lesson with Crasher Wake at the foot of Route 227, which that battle leads to.
    /// Buck stays on Route 227, where the way into Stark Mountain begins with him.</para>
    /// </summary>
    private static void ChapterTwelve(StoryState story, ScriptLibrary scripts)
    {
        Run(story, scripts, ScriptLibrary.ChapterTwelve);
        if (!ArrivedInTheBattleZone.Any(story.Has)) return;
        foreach (string flag in new[]
                 {
                     "FLAG_HIDE_FIGHT_AREA_RIVAL", "FLAG_HIDE_FIGHT_AREA_BUCK", "FLAG_HIDE_FIGHT_AREA_BATTLERS_AND_SPECTATORS",
                     "FLAG_HIDE_FIGHT_AREA_BLOCKADE", "FLAG_HIDE_ROUTE_227_RIVAL", "FLAG_HIDE_ROUTE_227_CRASHER_WAKE"
                 })
            story.Set(flag);
        if (story.Var("VAR_FIGHT_AREA_STATE") < 2) story.SetVar("VAR_FIGHT_AREA_STATE", 2);
        if (story.Var("VAR_ROUTE_227_WAKE_RIVAL_STATE") == 0) story.SetVar("VAR_ROUTE_227_WAKE_RIVAL_STATE", 1);
    }

    /// <summary>The flag that says the League has been won (scripts/PokemonLeagueHallOfFame.txt; the original's system flag).</summary>
    public const string GameCompletedFlag = "FLAG_GAME_COMPLETED";

    /// <summary>The variable of the rival's battle before the League's door (scripts/PokemonLeagueNorthPokemonCenter.txt): 1 once he has been beaten there.</summary>
    public const string RivalAtTheLeague = "VAR_RIVAL_BEAT_SUNYSHORE_GYM";

    /// <summary>
    /// Version 17, a save from before the eleventh chapter was written (plan 02 · S14): its people are hidden until
    /// their scenes by <see cref="ScriptLibrary.ChapterEleven"/>, which a new game runs too (called by the chapter
    /// before): the rival in the League's Pokémon Center, and the Day-Care couple's fan unless a Pokémon has been left
    /// with them already.
    /// <para>A save that has entered the Hall of Fame went through a room whose scene ended in a stand-in: the record,
    /// the region done and home. It is given what the scene sets now, by the scene's own script of what a victory sets
    /// (<see cref="ScriptLibrary.HallOfFameVictory"/>, which needs no screen): the Day-Care couple's fan, the assistant
    /// in Sandgem Town, Lucian among Canalave's shelves, Mom's news at home, and the Pokémon of the lakes and of Turnback
    /// Cave back where they weren't caught, as every victory brings them back. With it the Vs. Seeker's fourth level of
    /// rematches, which the scene sets beside it. And the rival, who battles whoever first walks up to the League's door,
    /// has had his battle: a Champion is not asked which of the two is ready for the Elite Four.</para>
    /// </summary>
    private static void ChapterEleven(StoryState story, ScriptLibrary scripts)
    {
        Run(story, scripts, ScriptLibrary.ChapterEleven);
        if (!story.Has(GameCompletedFlag)) return;
        Run(story, scripts, ScriptLibrary.HallOfFameVictory);
        story.Set(Overworld.VsSeeker.LevelFlag(4));
        if (story.Var(RivalAtTheLeague) == 0) story.SetVar(RivalAtTheLeague, 1);
    }

    /// <summary>The flag that first arriving in Sunyshore City sets (<see cref="Data.SpawnLocation.ArrivalFlag"/>).</summary>
    public const string ArrivedInSunyshoreFlag = "FLAG_FIRST_ARRIVAL_SUNYSHORE_CITY";

    /// <summary>The flag that says Volkner has left the Vista Lighthouse for his Gym, on which Flint leaves its door (scripts/VistaLighthouse.txt).</summary>
    public const string VolknerReturnedFlag = "FLAG_VOLKNER_RETURNED_TO_GYM";

    /// <summary>
    /// Version 16, a save from before the tenth chapter was written (plan 02 · S13): its people are hidden until their
    /// scenes by <see cref="ScriptLibrary.ChapterTen"/>, which a new game runs too (called by the chapter before).
    /// <para>The professor's lab brings the three Pokémon of the lakes back to their caverns, each unless it has been
    /// caught already: a save that has one of them (met some other way, before the caverns were open) counts it as
    /// caught, so that it isn't found a second time: Azelf and Uxie by their flags, Mesprit by the state its roaming
    /// keeps.</para>
    /// <para>In Sunyshore City, Flint meets whoever first comes in by the gate and then keeps the Gym's door until
    /// Volkner has been fetched from the lighthouse. A save that has already been to Sunyshore City, or holds its
    /// Beacon Badge, walked in when nobody stood there and found the Gym open: for it Flint has said his piece and
    /// gone, Volkner is back in his Gym, and the road from Valor Lakefront, which the professor's scene opens, is open,
    /// so that nobody is shut in or out. With the Badge, the rival's scene on the beach waits (the Gym's script left
    /// the city's state at 2, and an older Gym's is made so), which is where Jasmine gives HM07.</para>
    /// </summary>
    private static void ChapterTen(StoryState story, ScriptLibrary scripts, IEnumerable<Pokemon> owned)
    {
        Run(story, scripts, ScriptLibrary.ChapterTen);

        var mine = owned.Select(p => p.Species.Name).ToHashSet(StringComparer.Ordinal);
        if (mine.Contains("Azelf")) story.Set("FLAG_CAUGHT_AZELF");
        if (mine.Contains("Uxie")) story.Set("FLAG_CAUGHT_UXIE");
        string mesprit = Overworld.Roamers.StateVariable("Mesprit");
        if (mine.Contains("Mesprit") && story.Var(mesprit) == Overworld.Roamers.Roaming) story.SetVar(mesprit, Overworld.Roamers.Captured);

        bool badge = story.HasBadge(RegionDatabase.Sinnoh, "Beacon");
        if (!badge && !story.Has(ArrivedInSunyshoreFlag)) return;
        story.Set("FLAG_HIDE_VALOR_LAKEFRONT_COLLECTOR");
        if (story.Var("VAR_VALOR_LAKEFRONT_BLOCK_SUNYSHORE_STATE") == 0) story.SetVar("VAR_VALOR_LAKEFRONT_BLOCK_SUNYSHORE_STATE", 1);
        story.Set(VolknerReturnedFlag);
        story.Set("FLAG_HIDE_VISTA_LIGHTHOUSE_VOLKNER");
        story.Set("FLAG_HIDE_SUNYSHORE_CITY_FLINT");
        if (story.Var("VAR_SUNYSHORE_CITY_STATE") < (badge ? 2 : 1)) story.SetVar("VAR_SUNYSHORE_CITY_STATE", badge ? 2 : 1);
    }

    /// <summary>The flag that takes Cynthia from before Turnback Cave's mouth at Sendoff Spring (scripts/PokemonLeagueHallOfFame.txt).</summary>
    public const string SendoffCynthiaGoneFlag = "FLAG_HIDE_SENDOFF_SPRING_CYNTHIA";

    /// <summary>
    /// Version 15, a save from before the ninth chapter was written (plan 02 · S12): its people are hidden until their
    /// scenes by <see cref="ScriptLibrary.ChapterNine"/>, which a new game runs too (called by the chapter before). That
    /// script also keeps what such a save has already done: the shards of Mt. Coronet's cave painting stay on show
    /// where the button of Team Galactic's control room has been pressed, and Giratina, which a game of ours hid in
    /// Turnback Cave until the Hall of Fame, stands there again unless it has been caught. Cynthia now stands before
    /// the cave's mouth at Sendoff Spring until the Hall of Fame, which is what keeps everyone out of it until then: a
    /// save that has entered the Hall of Fame went through before she stood there, and for it she has gone.
    /// </summary>
    private static void ChapterNine(StoryState story, ScriptLibrary scripts)
    {
        Run(story, scripts, ScriptLibrary.ChapterNine);
        if (story.Has("FLAG_GAME_COMPLETED")) story.Set(SendoffCynthiaGoneFlag);
    }

    /// <summary>The flag that keeps the grunt with the Storage Key off Veilstone City's terrace (scripts/veilstone_city.txt).</summary>
    public const string StorageKeyGruntFlag = "FLAG_HIDE_VEILSTONE_CITY_GRUNT_M_STORAGE_KEY";

    /// <summary>
    /// Version 14, a save from before Team Galactic's headquarters were written (plan 02 · S11, part b): its people are
    /// hidden until their scenes by <see cref="ScriptLibrary.Headquarters"/>, which a new game runs too (called by the
    /// chapter's script). The grunt who lets the Storage Key fall is one of them, and the scene at Lake Acuity is what
    /// brings him to Veilstone City: a save that has played it (<c>VAR_LAKE_ACUITY_STATE</c> 2) had his flag cleared
    /// when it meant nothing yet, so it is cleared again, or nobody would ever open the warehouse's door.
    /// </summary>
    private static void Headquarters(StoryState story, ScriptLibrary scripts)
    {
        Run(story, scripts, ScriptLibrary.Headquarters);
        if (story.Var("VAR_LAKE_ACUITY_STATE") >= 2) story.Unset(StorageKeyGruntFlag);
    }

    /// <summary>The flag that takes the rival from the head of Acuity Lakefront's rock face (scripts/acuity_lakefront.txt).</summary>
    public const string LakefrontRivalGoneFlag = "FLAG_HIDE_ACUITY_LAKEFRONT_RIVAL";

    /// <summary>
    /// Version 13, a save from before the eighth chapter's first half was written (plan 02 · S11, part a): its people
    /// are hidden until their scenes by <see cref="ScriptLibrary.ChapterEight"/>, which a new game runs too. The rival
    /// stands at the head of Acuity Lakefront's rock face until the player first comes up the road from Route 217,
    /// which nobody reaches Snowpoint City without doing. A save that has already been to Snowpoint City, or holds its
    /// Icicle Badge, went by when nobody stood there: for that save he has called down and gone on to the lake, or it
    /// would come back from the east with Rock Climb and find him standing where the climb ends.
    /// </summary>
    private static void ChapterEight(StoryState story, ScriptLibrary scripts)
    {
        Run(story, scripts, ScriptLibrary.ChapterEight);
        if (!story.Has(ArrivedInSnowpointFlag) && !story.HasBadge(RegionDatabase.Sinnoh, "Icicle")) return;
        story.Set(LakefrontRivalGoneFlag);
        if (story.Var("VAR_ACUITY_LAKEFRONT_STATE") == 0) story.SetVar("VAR_ACUITY_LAKEFRONT_STATE", 1);
    }

    /// <summary>The flag that first arriving in Snowpoint City sets (<see cref="Data.SpawnLocation.ArrivalFlag"/>).</summary>
    public const string ArrivedInSnowpointFlag = "FLAG_FIRST_ARRIVAL_SNOWPOINT_CITY";

    /// <summary>The flag that takes Team Galactic's two grunts out of Mt. Coronet's northern hall (scripts/lake_verity.txt).</summary>
    public const string CoronetGruntsGoneFlag = "FLAG_HIDE_MT_CORONET_1F_NORTH_ROOM_1_GRUNTS_M";

    /// <summary>
    /// Version 12, a save from before the seventh chapter was written (plan 02 · S10): its people are hidden until
    /// their scenes by <see cref="ScriptLibrary.ChapterSeven"/>, which a new game runs too. A save that has Byron's Badge
    /// keeps the library's people as his Gym's script left them.
    /// </summary>
    private static void ChapterSeven(StoryState story, ScriptLibrary scripts)
    {
        Run(story, scripts, ScriptLibrary.ChapterSeven);
        BeyondTheGrunts(story);
    }

    /// <summary>
    /// Version 12 again: the two grunts the chapter stands across Mt. Coronet's northern hall, on the way to Route 216,
    /// stay until Commander Mars is beaten at Lake Verity. A save that has already been to Snowpoint City, or holds
    /// its Icicle Badge, walked that hall when nobody stood in it: for that save they have gone, so that it isn't shut
    /// in the north or out of it, and Maylene, who catches the player up on Route 217 on her way to Snowpoint, has
    /// long since gone by.
    /// </summary>
    private static void BeyondTheGrunts(StoryState story)
    {
        if (!story.Has(ArrivedInSnowpointFlag) && !story.HasBadge(RegionDatabase.Sinnoh, "Icicle")) return;
        story.Set(CoronetGruntsGoneFlag);
        if (story.Var("VAR_ROUTE_217_STATE") == 0) story.SetVar("VAR_ROUTE_217_STATE", 1);
    }

    /// <summary>The flag that first arriving in Canalave City sets (<see cref="Data.SpawnLocation.ArrivalFlag"/>).</summary>
    public const string ArrivedInCanalaveFlag = "FLAG_FIRST_ARRIVAL_CANALAVE_CITY";

    /// <summary>The flag the Psyduck on Route 210 leave by, once the Secret Potion has cured them (scripts/route_210_south.txt).</summary>
    public const string PsyduckCuredFlag = "FLAG_HIDE_ROUTE_210_SOUTH_PSYDUCK";

    /// <summary>
    /// Version 11, a save from before the sixth chapter was written (plan 02 · S9): its people are hidden until their
    /// scenes by <see cref="ScriptLibrary.ChapterSix"/>, which a new game runs too. A save whose Psyduck have gone
    /// cured them before Cynthia came up the road after them with the Old Charm: it has her errand now, as if she had
    /// (the charm in the bag and <c>FLAG_USED_SECRETPOTION</c>). Route 218's blockade before the Canalave gate, which
    /// Cynthia's scene in Celestic Town lifts, is gone for a save that has already been to Canalave City, so it isn't
    /// shut in. And the Vs. Seeker the assistant gave on Route 207 unlocks the first level of rematches, which that
    /// scene didn't set until this chapter.
    /// </summary>
    private static void ChapterSix(StoryState story, ScriptLibrary scripts, Inventory? bag)
    {
        Run(story, scripts, ScriptLibrary.ChapterSix);
        if (story.Has(PsyduckCuredFlag) && !story.Has("FLAG_USED_SECRETPOTION") && !story.Has("FLAG_DELIVERED_OLD_CHARM"))
        {
            if (ItemDatabase.Get("Old Charm") is { } charm && bag != null && bag.GetQuantity(charm) == 0) bag.AddItem(charm);
            story.Set("FLAG_USED_SECRETPOTION");
        }
        if (story.Has(ArrivedInCanalaveFlag)) story.Set("FLAG_HIDE_ROUTE_218_BLOCKADE");
        if (ItemDatabase.Get(Overworld.VsSeeker.Item) is { } seeker && bag != null && bag.GetQuantity(seeker) > 0)
            story.Set(Overworld.VsSeeker.LevelFlag(1));
    }

    /// <summary>The flag that first arriving in Pastoria City sets (<see cref="Data.SpawnLocation.ArrivalFlag"/>).</summary>
    public const string ArrivedInPastoriaFlag = "FLAG_FIRST_ARRIVAL_PASTORIA_CITY";

    /// <summary>
    /// Version 10, a save from before the fifth chapter was written (plan 02 · S8): its people are hidden until their
    /// scenes by <see cref="ScriptLibrary.ChapterFive"/>, which a new game runs too. The television crew who keep the
    /// gate between Hearthome City and Route 212 shut until the player has come to Pastoria City from the other side
    /// have gone for a save that has already been there.
    /// </summary>
    private static void ChapterFive(StoryState story, ScriptLibrary scripts)
    {
        Run(story, scripts, ScriptLibrary.ChapterFive);
        if (story.Has(ArrivedInPastoriaFlag)) story.Set("FLAG_HIDE_ROUTE_212_BLOCKADE");
    }

    /// <summary>The flag set as Oreburgh Gate's hiker hands over HM06 (scripts/oreburgh_gate_1f.txt).</summary>
    public const string ReceivedRockSmashFlag = "FLAG_RECEIVED_HM06";

    /// <summary>
    /// Version 4, a save that won the Coal Badge before Oreburgh Gate's hiker gave out HM06 (plan 02 · S5): it went
    /// through the Gate when nobody stood there, so it has no Rock Smash, and the Ravaged Path's cracked rocks shut
    /// the way north to Floaroma Town and Eterna City. The Badge says it has been past him, so it has the HM now.
    /// </summary>
    private static void HandOverRockSmash(StoryState story, Inventory? bag)
    {
        if (!story.HasBadge(RegionDatabase.Sinnoh, "Coal") || story.Has(ReceivedRockSmashFlag)) return;
        if (ItemDatabase.Get("HM06") is { } hm && bag != null && bag.GetQuantity(hm) == 0) bag.AddItem(hm);
        story.Set(ReceivedRockSmashFlag);
        story.SetVar("VAR_OREBURGH_GATE_1F_HIKER_STATE", 2);
    }

    /// <summary>
    /// Route 205's trainers were placed under ids of our own before Platinum's data filled them (plan 02 · S6): a
    /// save that beat one under its old id has beaten the data's.
    /// </summary>
    private static readonly Dictionary<string, string> RenamedTrainers = new()
    {
        ["trainer_jacob"] = "camper_jacob", ["trainer_daniel"] = "hiker_daniel", ["trainer_elizabeth"] = "aroma_lady_elizabeth",
        ["trainer_zackary"] = "camper_zackary", ["trainer_siena"] = "picnicker_siena", ["trainer_nicholas"] = "hiker_nicholas",
        ["trainer_kelsey"] = "battle_girl_kelsey", ["trainer_karina"] = "picnicker_karina"
    };

    /// <summary>
    /// Version 6: the south-west's trainers (Routes 202 to 204, Oreburgh Gate and the mine) had ids of our own too,
    /// until their overlays gave their lines alone and the table everything else, its ids included (plan 08 · P7).
    /// </summary>
    private static readonly Dictionary<string, string> SouthWestTrainers = new()
    {
        ["trainer_tristan"] = "youngster_tristan", ["trainer_natalie"] = "lass_natalie", ["trainer_logan"] = "youngster_logan",
        ["trainer_michael"] = "youngster_michael", ["trainer_dallas"] = "youngster_dallas", ["trainer_sebastian"] = "youngster_sebastian",
        ["trainer_madeline"] = "lass_madeline", ["trainer_kaitlin"] = "lass_kaitlin", ["trainer_taylor"] = "aroma_lady_taylor",
        ["trainer_brandon"] = "bug_catcher_brandon", ["trainer_liv_and_liz"] = "twins_liv_and_liz", ["trainer_sarah"] = "lass_sarah",
        ["trainer_tyler"] = "youngster_tyler", ["trainer_samantha"] = "lass_samantha", ["trainer_curtis"] = "camper_curtis",
        ["trainer_diana"] = "picnicker_diana", ["trainer_grant"] = "veteran_grant", ["trainer_colin"] = "worker_colin",
        ["trainer_mason"] = "worker_mason"
    };

    private static void RenameTrainers(StoryState story, IReadOnlyDictionary<string, string>? renamed = null)
    {
        foreach (var (old, now) in renamed ?? RenamedTrainers)
            if (story.HasDefeated(old)) story.Defeat(now);
    }

    /// <summary>
    /// Version 1, a save from before the first chapter was written (plan 02 · S4): its game began with a Turtwig, a
    /// Pokédex and the Running Shoes, out in front of the house. It is taken to have played the chapter already,
    /// by the script <see cref="ScriptLibrary.OpeningDone"/>, which sets the flags and variables the chapter would
    /// have left behind.
    /// </summary>
    public static void PastTheOpening(StoryState story, ScriptLibrary scripts) => Run(story, scripts, ScriptLibrary.OpeningDone);

    /// <summary>
    /// Version 2, a save from before the second chapter was written (plan 02 · S5): the people its scenes bring on
    /// (the assistant and Looker in Jubilife City, the president, the professor and Team Galactic at the north gate)
    /// stood about with their old lines. They are taken off the map until their scenes, by
    /// <see cref="ScriptLibrary.ChapterTwo"/>, which a new game runs too.
    /// </summary>
    private static void Run(StoryState story, ScriptLibrary scripts, string name)
    {
        if (scripts.Find(name) is not { } script) return;
        var runner = new ScriptRunner(scripts, new HeadlessScriptHost(story) { ShowsNothing = true });
        runner.Start(script);
        runner.RunToEnd();
    }

    /// <summary>
    /// Version 0, a save from before there was a story to remember: it has its Hall of Fame flags, its badges and
    /// the trainers it beat, and nothing else. It is given what a new game starts with, which leaves whatever it
    /// has already set as it is, and its starter is read off its Pokémon.
    /// </summary>
    private static void FromBeforeTheStory(StoryState story, IEnumerable<Pokemon> owned, ScriptLibrary scripts)
    {
        var had = story.Snapshot();
        BeginNewGame(story, scripts);
        foreach (string flag in had.Flags) story.Set(flag);
        foreach (var (name, value) in had.Variables) story.SetVar(name, value);

        if (story.PlayerStarter == null) story.ChooseStarter(StarterAmong(owned) ?? StoryState.Starters[0]);
    }

    /// <summary>The starter someone with these Pokémon took: the first of the three they have, grown or not. Null with none of them.</summary>
    public static string? StarterAmong(IEnumerable<Pokemon> owned)
    {
        var species = owned.Select(p => p.Species.Name).ToHashSet(StringComparer.Ordinal);
        return StarterLines.FirstOrDefault(line => line.Any(species.Contains))?[0];
    }
}
