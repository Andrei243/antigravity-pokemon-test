using System;
using System.Collections.Generic;
using System.Linq;
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
    /// <c>StoryVersion</c>; <paramref name="owned"/> is every Pokémon the player has, on the team and in the PC.
    /// </summary>
    public static void Upgrade(StoryState story, int savedVersion, IEnumerable<Pokemon> owned, ScriptLibrary scripts)
    {
        if (savedVersion < 1) FromBeforeTheStory(story, owned, scripts);
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
