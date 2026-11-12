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
    // The moment, as the player's steps ask for it
    private Func<EncounterMoment?>? momentOf;

    /// <summary>The trainer's whole number: the card's and its hidden half (the original's 32 bits).</summary>
    internal uint TrainerNumber => ((uint)flow.Encounters.SecretId << 16) | (uint)(flow.TrainerId & 0xffff);

    /// <summary>What the moment brings to the wild Pokémon met where the player stands (<see cref="EncounterMoment"/>).</summary>
    private EncounterMoment EncounterMomentNow()
    {
        var garden = SpecialEncounterTables.Sinnoh.TrophyGarden;
        // The day's swarm is the region's own (plan 20 · J13, part c): Johto's twenty places by HeartGold's table, every other region's Sinnoh's
        var swarm = Swarms.PlaceToday(flow.Encounters, Here.Id);
        return new EncounterMoment
        {
            Time = GameClock.Now,
            Today = GameClock.Today,
            SwarmArea = swarm?.Area,
            SwarmWay = swarm?.Way switch { "surf" => SwarmWay.Surf, "fish" => SwarmWay.Fish, _ => SwarmWay.Grass },
            NationalDex = flow.Pokedex.NationalUnlocked,
            SlotTwo = Settings.SlotTwo,
            TrophyFirst = TrophyGardenRules.SpeciesIn(flow.Encounters.TrophyFirst, garden),
            TrophySecond = TrophyGardenRules.SpeciesIn(flow.Encounters.TrophySecond, garden),
            MarshDaily = flow.Safari.Active ? flow.Encounters.MarshDaily : null,
            Marsh = SpecialEncounterTables.Sinnoh.GreatMarsh,
            Partner = partner != null,
            Radar = flow.Radar.Active ? flow.Radar : null,
            Height = flow.Player.HeightOn(flow.Map),
            State = flow.Encounters,
            // The song the Pokégear's radio plays over the field (plan 20 · J3)
            Radio = radioSong,
            // Unova's season, whose tables an area may have (plan 22 · U4); null in every other region
            Season = SeasonHere,
            // The hordes of the region's tall grass, once its Hall of Fame is entered (plan 14 · B4)
            Hordes = Hordes.Open(Here, flow.Story, Ruleset.Current),
            // Our own species past level 100 in the rarest slots, once the region's Hall of Fame is entered (plan 03 · D15)
            OursOpen = OursSlots.Open(Here, flow.Story),
            // Alphas of the post-game zone (plan 10 · F10)
            AlphasOpen = Alphas.Open(Here, flow.Story),
            // Which of a place's grass tables is met where it has several: Altering Cave's (plan 21 · H12, part k)
            LandSet = flow.Story.Var(EncounterMoment.LandSetVariable),
            // The day's outbreaks of one species, once the region's Hall of Fame is entered (plan 38 · EX1)
            Outbreaks = OutbreaksNow(),
            // The powers in force that a step reads (plan 39 · CR6's dishes): the encounters' or stealth's, and a type's
            PowerRate = flow.Powers.Strength(PowerKind.Encounter) + flow.Powers.Strength(PowerKind.Stealth),
            PowerType = flow.Powers.TypeOf(PowerKind.TypeEncounter),
            PowerSlots = flow.Powers.Strength(PowerKind.TypeEncounter),
            // The visitor a scan called up, for its hour (plan 38 · EX2)
            Visitor = VisitorNow(),
            // The hour's rift's table within its reach (plan 38 · EX5)
            Rift = RiftHere()
        };
    }

    /// <summary>The place the player is in, as the roamers know places: its area's key, or a hand-made map's name.</summary>
    private string PlaceKey() => flow.Map.AreaAt(flow.Player.GridX, flow.Player.GridY)?.Key ?? flow.Map.Name;

    /// <summary>The Poké Radar's chain is over, its patches still.</summary>
    private void EndRadarChain() => flow.Radar.Clear();

    /// <summary>
    /// A step's part in the wild Pokémon (<c>Field_ProcessStep</c>): poison bites every fourth step, the Poké Radar
    /// charges in the bag, and its patches out of sight stop shaking. True when a script took over (a Pokémon came
    /// through the poison).
    /// </summary>
    private bool EncountersStep()
    {
        if (flow.Bag.GetQuantity(ItemDatabase.Get("Poké Radar")!) > 0) RadarChain.Charge(flow.Encounters);
        flow.Radar.KeepInView(flow.Player.GridX, flow.Player.GridY);
        switch (FieldPoison.Step(flow.Encounters, flow.Party, Ruleset.Current))
        {
            case PoisonStep.Hurt:
                CarePoisonBite();
                AudioManager.PlaySound("status_poison");
                poisonFlash = PoisonFlashSeconds;
                break;
            case PoisonStep.Survived:
                CarePoisonBite();
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

    // Who hides in the grass within this many tiles of the player stirs it, and the list they are gathered in
    private const int HiddenStirReach = 16;
    private readonly List<Overworld.NPC> hidden = new();
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
        foreach (var patch in flow.Radar.Patches.Where(p => p.Active))
            world.Stirring.Add((patch.X, patch.Y, patch.Shake == PatchShake.Hard));
        // Someone the map keeps out of sight in tall grass (plan 21 · part q: Faraway Island's Mew, until it is found)
        // stirs the grass it hides in, as the radar's patches do, so the player has something to walk up to
        hidden.Clear();
        flow.Map.NPCs.Near(flow.Player.GridX, flow.Player.GridY, HiddenStirReach, hidden);
        hidden.RemoveAll(n => !n.OutOfSight || !flow.Map.IsTallGrass(n.GridX, n.GridY));
        foreach (var npc in hidden) world.Stirring.Add((npc.GridX, npc.GridY, false));
        // The DexNav's find hiding in the grass (plan 10 · F12; style guide, "Life", "A rustle") parts it as a hard shake
        // does, and every 0.6 s throws up leaf bits and its shiver marks. With nothing found nothing is added here
        var hiding = DexNavHere ? rustle : null;
        if (hiding != null) world.Stirring.Add((hiding.X, hiding.Y, true));
        if (life.Now < nextStir) return;
        nextStir = life.Now + StirEvery;
        if (hiding != null) life.Hiding(flow.Map, hiding.X, hiding.Y);
        foreach (var patch in flow.Radar.Patches.Where(p => p.Active))
            life.Rustle(flow.Map, patch.X, patch.Y, patch.Shake == PatchShake.Hard, patch.Shiny);
        foreach (var npc in hidden) life.Rustle(flow.Map, npc.GridX, npc.GridY, false, false);
        int px = flow.Player.GridX, py = flow.Player.GridY;
        foreach (var tree in flow.Map.Props)
        {
            if (tree.Type != PropType.HoneyTree || Math.Abs(tree.X - px) > 12 || Math.Abs(tree.Y - py) > 9) continue;
            int x = tree.X + tree.Width / 2, y = tree.Y + tree.Depth - 1;
            if (HoneyTrees.IdOf(flow.Map.AreaAt(x, y)?.Key) is { } id && HoneyTrees.Shaking(flow.Encounters.Trees[id]))
                life.ShakeTree(flow.Map, x, y, CrownTexels / 32f * Graphics.MapScene.VerticalScaleOf(flow.Map), flow.Encounters.Trees[id].Shakes);
        }
        RustleTheGrottoes(life);
    }

    // When a grotto near the player last rustled, by the field's own clock (plan 22 · U11)
    private double nextGrottoRustle;

    /// <summary>
    /// A grotto near the player that may hold something rustles (plan 22 · U11; <see cref="HiddenGrottoes.Rustles"/>):
    /// every <see cref="HiddenGrottoes.RustleEvery"/> seconds of the field's clock while one is within reach, leaves stir
    /// on the tile before its hollow and <c>grotto_rustle</c> is heard from its side. Nothing is drawn from the dice.
    /// </summary>
    private void RustleTheGrottoes(Graphics.FieldLife life)
    {
        if (ScriptRunning || life.Now < nextGrottoRustle) return;
        var grotto = HiddenGrottoes.RustlingNear(flow.Story, flow.Map.Name, flow.Player.GridX, flow.Player.GridY, flow.Pastimes.Steps);
        if (grotto == null) return;
        nextGrottoRustle = life.Now + HiddenGrottoes.RustleEvery;
        life.Rustle(flow.Map, grotto.X, grotto.Y + 1, hard: false, sparkle: false);
        AudioManager.PlaySound("grotto_rustle", PanAt(grotto.X));
    }

    /// <summary>The field's purple flash as poison bites, in two flat steps.</summary>
    private void DrawPoisonFlash()
    {
        if (poisonFlash <= 0f) return;
        // (a quarter at most under the options' REDUCE FLASHES, plan 12 · Q9)
        byte alpha = (byte)MathF.Round(255f * Accessibility.Flash((poisonFlash > PoisonFlashSeconds / 2f ? 110f : 55f) / 255f));
        Graphics.Canvas2D.Draw((w, h) => Raylib_cs.Raylib.DrawRectangle(0, 0, w, h, new Raylib_cs.Color((byte)150, (byte)60, (byte)190, alpha)));
    }

    /// <summary>The roamer of a slot, met in the field: as the save keeps it, in a battle it runs from (plan 06 · R13).</summary>
    private Pokemon RoamerToBattle(int slot)
    {
        EndRadarChain();
        return Roamers.ToBattle(flow.Encounters.Roamers[slot]);
    }

    /// <summary>
    /// What wild Pokémon hold (<c>AddWildMonToParty</c>, <c>Pokemon_GiveHeldItem</c>): every wild Pokémon met in the
    /// field or put in the way by a script, but a roamer, the catching lesson's and Pal Park's.
    /// </summary>
    private void GiveWildItems(IEnumerable<Pokemon> wilds, BattleKind kind)
    {
        if (kind is not (BattleKind.Normal or BattleKind.Safari)) return;
        var lead = WildLead.Of(flow.Party);
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
        bool field = flow.FieldEncounter;
        flow.FieldEncounter = flow.Hooked = false;
        // The catch streak of Pokémon met in sight goes on or breaks (plan 38 · EX3)
        KeepTheStreak(fought);
        // The DexNav's chain goes on or breaks (plan 10 · F12)
        DexNavAfterBattle(fought, defeat);
        // ...and the camp may cure the team once more (plan 10 · F6)
        flow.CampCare.BattleEnded();
        if (!field) return;
        bool won = fought.Result == BattleResult.PlayerVictory, caught = fought.Result == BattleResult.EnemyCaught;
        if (!defeat && fought.EnemyPokemon is { } foe && fought.Kind is BattleKind.Normal or BattleKind.Roamer)
            Roamers.AfterBattle(flow.Encounters, foe, won, caught, PlaceKey(), flow.Story, fieldRandom);
        flow.Radar.AfterBattle(won && !defeat, caught, fieldRandom);
        // A Pokémon of the day's outbreak beaten or caught in its place is counted (plan 38 · EX1); nothing is drawn
        if (!defeat && (won || caught) && fought.EnemyPokemon is { } outbreakFoe && fought.Kind == BattleKind.Normal) CountOutbreak(outbreakFoe);
    }

    /// <summary>
    /// The clock's part (<c>FieldSystem_HandleDailyEvents</c> and the minutes after it): each new day brings the day's
    /// events (<see cref="DailyEvents"/>: the daily flags, the day's number, the hidden items that come back), and the
    /// honey trees count the minutes.
    /// </summary>
    private void KeepTheEncounterClock(int daysPassed)
    {
        // The boxes' moods settle by the day (plan 35 · BN1)
        if (daysPassed > 0) CareRules.DaysPass(flow.Care, daysPassed, flow.Party.Members);
        if (daysPassed > 0) DailyEvents.DaysPass(daysPassed, flow.Story, flow.Encounters, PlaceKey(), fieldRandom, flow.Underground, flow.Frontier, DateOnly.FromDateTime(GameClock.Today));
        flow.Encounters.ClockTo(GameClock.Moment);
        flow.Berries.ClockTo(GameClock.Moment);
    }
}
