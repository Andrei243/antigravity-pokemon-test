using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The wild Pokémon beyond the tables (plan 06 · R13): the moment's slots, swarms, the dailies, honey trees, the Poké
/// Radar, roamers, Feebas and poison in the field. The rules are GPU-free in <c>Overworld/</c> and
/// <c>Models/SpecialEncounters.cs</c>; this is where the game hands them its state and hears what they did.
/// </summary>
public partial class GameEngine
{
    /// <summary>What the game remembers of its wild Pokémon (saved as <see cref="SaveData.Encounters"/>).</summary>
    private SpecialEncounters encounters = new();

    /// <summary>The Poké Radar's chain, which the original doesn't save either.</summary>
    private readonly RadarChain radar = new();

    // Whether the battle under way was met in the field's grass, water or on a rod (not a script's, nor a honey
    // tree's): only then do the roamers and the Poké Radar hear how it ended (FieldTask_WildEncounter)
    private bool fieldEncounter;

    // Whether the battle under way was hooked on a rod, and so is fought on the water (plan 06 · R13)
    private bool hooked;

    // The moment, as the player's steps ask for it
    private Func<EncounterMoment?>? momentOf;

    /// <summary>The trainer's whole number: the card's and its hidden half (the original's 32 bits).</summary>
    internal uint TrainerNumber => ((uint)encounters.SecretId << 16) | (uint)(trainerId & 0xffff);

    /// <summary>What the moment brings to the wild Pokémon met where the player stands (<see cref="EncounterMoment"/>).</summary>
    private EncounterMoment EncounterMomentNow()
    {
        var garden = SpecialEncounterTables.Sinnoh.TrophyGarden;
        return new EncounterMoment
        {
            Time = GameClock.Now,
            Today = GameClock.Today,
            SwarmArea = Swarms.Today(encounters),
            NationalDex = playerPokedex.NationalUnlocked,
            TrophyFirst = TrophyGardenRules.SpeciesIn(encounters.TrophyFirst, garden),
            TrophySecond = TrophyGardenRules.SpeciesIn(encounters.TrophySecond, garden),
            MarshDaily = safari.Active ? encounters.MarshDaily : null,
            Marsh = SpecialEncounterTables.Sinnoh.GreatMarsh,
            Partner = partner != null,
            Radar = radar.Active ? radar : null,
            Height = player.HeightOn(currentMap),
            State = encounters
        };
    }

    /// <summary>The place the player is in, as the roamers know places: its area's key, or a hand-made map's name.</summary>
    private string PlaceKey() => currentMap.AreaAt(player.GridX, player.GridY)?.Key ?? currentMap.Name;

    /// <summary>The Poké Radar's chain is over, its patches still.</summary>
    private void EndRadarChain() => radar.Clear();

    /// <summary>
    /// A step's part in the wild Pokémon (<c>Field_ProcessStep</c>): poison bites every fourth step, the Poké Radar
    /// charges in the bag, and its patches out of sight stop shaking. True when a script took over (a Pokémon came
    /// through the poison).
    /// </summary>
    private bool EncountersStep()
    {
        if (playerInventory.GetQuantity(ItemDatabase.Get("Poké Radar")!) > 0) RadarChain.Charge(encounters);
        radar.KeepInView(player.GridX, player.GridY);
        switch (FieldPoison.Step(encounters, playerParty, Ruleset.Current))
        {
            case PoisonStep.Hurt:
                AudioManager.PlaySound("status_poison");
                poisonFlash = PoisonFlashSeconds;
                break;
            case PoisonStep.Survived:
                AudioManager.PlaySound("status_poison");
                poisonFlash = PoisonFlashSeconds;
                return StartScript(FieldScripts.PoisonSurvived);
        }
        return false;
    }

    /// <summary>How long the field flashes purple when poison bites (<c>Field_DoPoisonEffect</c>), and what is left of it.</summary>
    private const float PoisonFlashSeconds = 0.4f;
    private float poisonFlash;

    // How high a honey tree's leaves are shaken from: the foot of its crown, in texels of its card (Landmarks' honey
    // tree), which the field stretches upright like every card
    private const float CrownTexels = 56f;

    // How often the Poké Radar's patches and a honey tree that Pokémon have come to stir, by the field's own clock
    private const double StirEvery = 0.6;
    private double nextStir;

    /// <summary>
    /// A frame of the field's: the poison's flash fades, and what the Poké Radar set shaking and the honey trees
    /// with Pokémon at them stir every so often (<see cref="FieldLife.Rustle"/>, <see cref="FieldLife.ShakeTree"/>).
    /// </summary>
    private void TickEncounters(float dt)
    {
        if (poisonFlash > 0f) poisonFlash = Math.Max(0f, poisonFlash - dt);
        var life = world.Life;
        world.Stirring.Clear();
        foreach (var patch in radar.Patches.Where(p => p.Active))
            world.Stirring.Add((patch.X, patch.Y, patch.Shake == PatchShake.Hard));
        if (life.Now < nextStir) return;
        nextStir = life.Now + StirEvery;
        foreach (var patch in radar.Patches.Where(p => p.Active))
            life.Rustle(currentMap, patch.X, patch.Y, patch.Shake == PatchShake.Hard, patch.Shiny);
        int px = player.GridX, py = player.GridY;
        foreach (var tree in currentMap.Props)
        {
            if (tree.Type != PropType.HoneyTree || Math.Abs(tree.X - px) > 12 || Math.Abs(tree.Y - py) > 9) continue;
            int x = tree.X + tree.Width / 2, y = tree.Y + tree.Depth - 1;
            if (HoneyTrees.IdOf(currentMap.AreaAt(x, y)?.Key) is { } id && HoneyTrees.Shaking(encounters.Trees[id]))
                life.ShakeTree(currentMap, x, y, CrownTexels / 32f * Graphics.MapScene.VerticalScaleOf(currentMap), encounters.Trees[id].Shakes);
        }
    }

    /// <summary>The field's purple flash as poison bites, in two flat steps.</summary>
    private void DrawPoisonFlash()
    {
        if (poisonFlash <= 0f) return;
        byte alpha = poisonFlash > PoisonFlashSeconds / 2f ? (byte)110 : (byte)55;
        Raylib_cs.Raylib.DrawRectangle(0, 0, VirtualWidth, VirtualHeight, new Raylib_cs.Color((byte)150, (byte)60, (byte)190, alpha));
    }

    /// <summary>The roamer of a slot, met in the field: as the save keeps it, in a battle it runs from (plan 06 · R13).</summary>
    private Pokemon RoamerToBattle(int slot)
    {
        EndRadarChain();
        return Roamers.ToBattle(encounters.Roamers[slot]);
    }

    /// <summary>
    /// What wild Pokémon hold (<c>AddWildMonToParty</c>, <c>Pokemon_GiveHeldItem</c>): every wild Pokémon met in the
    /// field or put in the way by a script, but a roamer, the catching lesson's and Pal Park's.
    /// </summary>
    private void GiveWildItems(IEnumerable<Pokemon> wilds, BattleKind kind)
    {
        if (kind is not (BattleKind.Normal or BattleKind.Safari)) return;
        var lead = WildLead.Of(playerParty);
        foreach (var wild in wilds)
            if (wild.HeldItem == null) wild.HeldItem = WildEncounterRules.HeldItem(wild.Species, lead, fieldRandom);
    }

    /// <summary>
    /// The end of a wild battle met in the field, unless it was lost (<c>FieldTask_WildEncounter</c>): the roamers hear
    /// of it (<c>RoamerAfterBattle_UpdateRoamers</c>) and the Poké Radar's chain goes on or ends. Any other battle has
    /// ended the chain as it began.
    /// </summary>
    private void EncountersAfterBattle(BattleEngine fought, bool defeat)
    {
        bool field = fieldEncounter;
        fieldEncounter = hooked = false;
        if (!field) return;
        bool won = fought.Result == BattleResult.PlayerVictory, caught = fought.Result == BattleResult.EnemyCaught;
        if (!defeat && fought.EnemyPokemon is { } foe && fought.Kind is BattleKind.Normal or BattleKind.Roamer)
            Roamers.AfterBattle(encounters, foe, won, caught, PlaceKey(), story, fieldRandom);
        radar.AfterBattle(won && !defeat, caught, fieldRandom);
    }

    /// <summary>
    /// The clock's part (<c>FieldSystem_HandleDailyEvents</c> and the minutes after it): each new day brings the day's
    /// events (<see cref="DailyEvents"/>: the daily flags, the day's number, the hidden items that come back), and the
    /// honey trees count the minutes.
    /// </summary>
    private void KeepTheEncounterClock(int daysPassed)
    {
        if (daysPassed > 0) DailyEvents.DaysPass(daysPassed, story, encounters, PlaceKey(), fieldRandom);
        encounters.ClockTo(GameClock.Moment);
        berries.ClockTo(GameClock.Moment);
    }
}
