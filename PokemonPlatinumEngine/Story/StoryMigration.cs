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
    }

    /// <summary>
    /// Brings a loaded save's story up to today's. <paramref name="savedVersion"/> is the save's
    /// <c>StoryVersion</c>; <paramref name="owned"/> is every Pokémon the player has, on the team and in the PC, and
    /// <paramref name="bag"/> the player's bag, for what a step hands over.
    /// </summary>
    /// <param name="chance">Where a step's draws come from (the day's events' first numbers); the game's own chance when left out.</param>
    public static void Upgrade(StoryState story, int savedVersion, IEnumerable<Pokemon> owned, ScriptLibrary scripts, Inventory? bag = null, Random? chance = null)
    {
        if (savedVersion < 1) FromBeforeTheStory(story, owned, scripts);
        if (savedVersion < 2) PastTheOpening(story, scripts);
        if (savedVersion < 3) Run(story, scripts, ScriptLibrary.ChapterTwo);
        else if (savedVersion < 4) Run(story, scripts, ScriptLibrary.ChapterThree);
        if (savedVersion < 4) RenameTrainers(story);
        if (savedVersion < 5) HandOverRockSmash(story, bag);
        if (savedVersion < 6) RenameTrainers(story, SouthWestTrainers);
        if (savedVersion < 7) TheDaysBegin(story, chance ?? Core.Dice.Shared);
    }

    /// <summary>
    /// Version 7, plan 06 · R14a ("the day's events"): a save from before the day's events has no number for Jubilife
    /// TV's lottery and no level of the day, which a new game's script draws (<see cref="DailyEvents.NewGame"/>); it is
    /// given them as a new game is, unless something has set them already. The Valley Windworks' Drifloon of Fridays,
    /// which stood nowhere before, is hidden as a new game hides it until the place's own script says it is there.
    /// </summary>
    private static void TheDaysBegin(StoryState story, Random chance)
    {
        story.Set(FridayDrifloonFlag);
        if (story.Var(Lottery.NumberVar) != 0 || story.Var(DailyEvents.DailyLevelVar) != 0) return;
        DailyEvents.NewGame(story, chance);
    }

    /// <summary>The flag that hides the Valley Windworks' Drifloon (scripts/valley_windworks_outside.txt).</summary>
    public const string FridayDrifloonFlag = "FLAG_HIDE_VALLEY_WINDWORKS_OUTSIDE_DRIFLOON";

    /// <summary>The flag set as Oreburgh Gate's hiker hands over HM06 (scripts/oreburgh_gate_1f.txt).</summary>
    public const string ReceivedRockSmashFlag = "FLAG_RECEIVED_HM06";

    /// <summary>
    /// Version 4, a save that won the Coal Badge before Oreburgh Gate's hiker gave out HM06 (plan 02 · S5): it went
    /// through the Gate when nobody stood there, so it has no Rock Smash, and the Ravaged Path's cracked rocks shut
    /// the way north to Floaroma Town and Eterna City. The Badge says it has been past him, so it has the HM now.
    /// </summary>
    private static void HandOverRockSmash(StoryState story, Inventory? bag)
    {
        if (!story.HasBadge(Badge.Coal) || story.Has(ReceivedRockSmashFlag)) return;
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
