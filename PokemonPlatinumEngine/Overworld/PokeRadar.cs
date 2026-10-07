using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>How a patch of grass shakes (<c>PATCH_SHAKE_*</c>): a hard shake brings the place's radar species into the table.</summary>
public enum PatchShake { Soft, Hard }

/// <summary>One of the four patches of grass the Poké Radar sets shaking, one on each ring round the player.</summary>
public sealed class GrassPatch
{
    public int X { get; set; }
    public int Y { get; set; }
    public PatchShake Shake { get; set; }
    public bool Active { get; set; }

    /// <summary>Whether walking into it goes on with the chain (its species, at its level), or draws a Pokémon afresh.</summary>
    public bool ContinuesChain { get; set; }

    /// <summary>The patch that sparkles: what waits in it is shiny.</summary>
    public bool Shiny { get; set; }
}

/// <summary>What using the Poké Radar came to.</summary>
public enum RadarUse
{
    /// <summary>Patches of grass are shaking.</summary>
    Shaking,
    /// <summary>The battery isn't charged: <see cref="RadarChain.BatterySteps"/> steps are needed since it was last used.</summary>
    NotCharged,
    /// <summary>No grass round the player could shake.</summary>
    Quiet,
    /// <summary>Not usable here: not standing in tall grass, on a Bicycle, or travelling with someone.</summary>
    CantUse
}

/// <summary>What a step into a shaking patch brings: its shake, whether it goes on with the chain, and whether the Pokémon is shiny.</summary>
public readonly record struct RadarStep(PatchShake Shake, bool KeepsChain, bool Shiny);

/// <summary>
/// The Poké Radar and its chain (plan 06 · R13; <c>src/pokeradar.c</c> and the radar's parts of
/// <c>wild_encounters.c</c>), with no drawing or input. Used in tall grass with its battery charged (50 steps), it
/// sets one patch shaking on each of four rings round the player (9, 7, 5 and 3 tiles across; a patch only on tall
/// grass at the player's height in the same place). Walking into one always meets a Pokémon: the first starts a chain
/// of its species; after that a patch that goes on with the chain brings that species at the chain's level and adds
/// one to the chain, and any other draws afresh, adding one if it happens to draw the same species and ending the
/// chain if not. After a battle won or a Pokémon caught the four patches are set again, each going on with the chain
/// 88, 68, 48 and 28 times in a hundred from the outer ring in (98, 78, 58 and 38 after a catch), the others shaking
/// softly or hard as a coin falls; a patch that goes on with a chain of n is shiny one time in 8,200 − 200n (never
/// better than one in 200). Running, losing, any other Pokémon met, a warp, the Bicycle or every patch out of sight
/// end the chain.
/// </summary>
public sealed class RadarChain
{
    public const int BatterySteps = 50;
    public const int MaxCount = 999;

    // Tiles in each ring, from the outer one in, and the odds that a patch of each goes on with the chain
    private static readonly int[] RingTiles = { 32, 24, 16, 8 };
    private static readonly int[] KeepOdds = { 88, 68, 48, 28 };
    private static readonly int[] KeepOddsAfterACatch = { 98, 78, 58, 38 };

    public int Count { get; private set; }
    public PatchShake Shake { get; private set; }

    /// <summary>The chain's species and level; null before its first Pokémon.</summary>
    public string? Species { get; private set; }
    public int Level { get; private set; }

    /// <summary>Whether patches are shaking and a chain may go on.</summary>
    public bool Active { get; private set; }

    /// <summary>Whether the player has walked into one of the chain's patches (<c>unk_18</c>).</summary>
    public bool SteppedIn { get; private set; }

    // Whether the next patch walked into is the chain's first (unk_14)
    private bool fresh = true;

    public GrassPatch[] Patches { get; } = Enumerable.Range(0, 4).Select(_ => new GrassPatch()).ToArray();

    /// <summary><c>RadarChain_Clear</c>: the chain is over and no patch shakes.</summary>
    public void Clear()
    {
        Count = 0;
        Shake = PatchShake.Soft;
        Species = null;
        Level = 0;
        Active = false;
        fresh = true;
        SteppedIn = false;
        foreach (var p in Patches)
        {
            p.Active = p.ContinuesChain = p.Shiny = false;
            p.Shake = PatchShake.Soft;
            p.X = p.Y = 0;
        }
    }

    /// <summary>
    /// <c>RefreshRadarChain</c>: the radar used where the player stands. With its battery charged it is spent, and
    /// the patches are set round the player as after a battle won; with none to set, the chain ends.
    /// </summary>
    public RadarUse Use(SpecialEncounters state, Map map, int x, int y, float height, TravelMode travel, bool partner, Random rng)
    {
        // CanUsePokeRadar
        if (partner || travel == TravelMode.Cycling || !map.InBounds(x, y) || map.BehaviourAt(x, y) != TileBehavior.TallGrass) return RadarUse.CantUse;
        if (state.RadarCharge < BatterySteps) return RadarUse.NotCharged;
        state.RadarCharge = 0;
        if (!Spawn(map, x, y, height, rng)) return RadarUse.Quiet;
        SetUp(caught: false, rng);
        return RadarUse.Shaking;
    }

    /// <summary><c>RadarChargeStep</c>: a step with the radar in the bag charges its battery, up to <see cref="BatterySteps"/>.</summary>
    public static void Charge(SpecialEncounters state)
    {
        if (state.RadarCharge < BatterySteps) state.RadarCharge++;
    }

    /// <summary>
    /// <c>RadarSpawnPatches</c>: one patch on each ring round the player, a tile of the ring drawn at random; it shakes
    /// only on tall grass at the player's height in the player's own place. With none, the chain ends.
    /// </summary>
    public bool Spawn(Map map, int px, int py, float height, Random rng)
    {
        int found = 0;
        var here = map.AreaAt(px, py);
        for (int ring = 0; ring < Patches.Length; ring++)
        {
            int roll = rng.Next(RingTiles[ring]);
            int side = 9 - ring * 2;
            int column, row;
            switch (roll / side)
            {
                case 0:
                    column = ring + roll % side;
                    row = ring;
                    break;
                case 1:
                    column = ring + roll % side;
                    row = ring + side - 1;
                    break;
                default:
                    int rest = roll - side * 2;
                    row = ring + rest / 2 + 1;
                    column = rest % 2 == 0 ? ring : ring + side - 1;
                    break;
            }
            var patch = Patches[ring];
            patch.X = px - 4 + column;
            patch.Y = py - 4 + row;
            patch.Active = map.InBounds(patch.X, patch.Y) && map.BehaviourAt(patch.X, patch.Y) == TileBehavior.TallGrass
                && MathF.Abs(map.HeightAt(patch.X, patch.Y) - height) < 0.01f && map.AreaAt(patch.X, patch.Y) == here;
            if (patch.Active) found++;
        }
        if (found == 0)
        {
            Clear();
            return false;
        }
        Active = true;
        return true;
    }

    /// <summary>
    /// <c>SetupGrassPatches</c>: whether each patch goes on with the chain (by its ring, better after a catch); one
    /// that doesn't shakes softly or hard on a coin and isn't shiny, one that does shakes as the chain does and may be.
    /// </summary>
    public void SetUp(bool caught, Random rng)
    {
        var odds = caught ? KeepOddsAfterACatch : KeepOdds;
        for (int ring = 0; ring < Patches.Length; ring++)
        {
            var patch = Patches[ring];
            if (!patch.Active) continue;
            patch.ContinuesChain = rng.Next(100) < odds[ring];
            if (!patch.ContinuesChain)
            {
                patch.Shake = rng.Next(100) < 50 ? PatchShake.Soft : PatchShake.Hard;
                patch.Shiny = false;
            }
            else
            {
                patch.Shake = Shake;
                patch.Shiny = ShinyPatch(Count, rng);
            }
        }
    }

    /// <summary><c>CheckPatchShiny</c>: one time in 8,200 − 200 × the chain (never better than one in 200), none before the chain has begun.</summary>
    public static bool ShinyPatch(int count, Random rng)
    {
        if (count == 0) return false;
        int rate = Math.Max(200, 8200 - count * 200);
        return rng.Next(rate) == 0;
    }

    /// <summary>
    /// <c>PokeRadar_ShouldDoRadarEncounter</c>: a step onto a tile. Null unless it is a shaking patch; then a Pokémon is
    /// met there whatever the odds. After the chain's first, a patch that goes on with it adds one to it now.
    /// </summary>
    public RadarStep? StepOnto(int x, int y)
    {
        if (!Active) return null;
        var patch = Patches.FirstOrDefault(p => p.Active && p.X == x && p.Y == y);
        if (patch == null) return null;
        SteppedIn = true;
        if (!fresh && patch.ContinuesChain)
        {
            Count = Math.Min(MaxCount, Count + 1);
            return new RadarStep(patch.Shake, true, patch.Shiny);
        }
        fresh = false;
        Shake = patch.Shake;
        return new RadarStep(patch.Shake, false, false);
    }

    /// <summary>
    /// The Pokémon a patch brings (<c>TryGenerateGrassEncounter_WithRadar</c>): a hard shake puts the place's radar
    /// species in the table first. One that goes on with the chain is its species at its level; otherwise a slot is
    /// drawn (with Magnet Pull's and Static's say) and its species starts the chain, adds to it or ends it. Neither
    /// Keen Eye nor a Repel keeps a patch's Pokémon away. Then the patches are set round the player again.
    /// </summary>
    public WildEncounterEntry Meet(IReadOnlyList<WildEncounterEntry> table, MapArea? area, RadarStep step, WildLead? lead, Map map, int x, int y, float height, Random rng)
    {
        var grass = table.Select(e => new WildEncounterEntry { SpeciesName = e.SpeciesName, MinLevel = e.MinLevel, MaxLevel = e.MaxLevel, Weight = e.Weight }).ToList();
        if (step.Shake == PatchShake.Hard && area != null) EncounterSlots.PutRadarSpecies(grass, area);
        string species;
        int level;
        if (step.KeepsChain && Species != null)
        {
            species = Species;
            level = Level;
        }
        else
        {
            var slot = WildEncounterRules.Slot(grass, water: false, lead, rng);
            species = slot.SpeciesName;
            level = Math.Max(slot.MinLevel, slot.MaxLevel);
            if (Species == null)
            {
                Species = species;
                Level = level;
                Count = Math.Min(MaxCount, Count + 1);
            }
            else if (species == Species)
            {
                level = Level;
                Count = Math.Min(MaxCount, Count + 1);
            }
            else Clear();
        }
        var met = new WildEncounterEntry
        {
            SpeciesName = species,
            MinLevel = level,
            MaxLevel = level,
            Shiny = step.KeepsChain && step.Shiny,
            Gender = WildEncounterRules.GenderFor(PokemonDatabase.Get(species), lead, rng),
            Nature = WildEncounterRules.NatureFor(lead, rng)
        };
        Spawn(map, x, y, height, rng);
        return met;
    }

    /// <summary>
    /// What the end of a battle does to the chain (<c>FieldTask_WildEncounter</c>): it goes on only after a Pokémon of a
    /// patch was knocked out or caught, and then its patches are set again.
    /// </summary>
    public void AfterBattle(bool won, bool caught, Random rng)
    {
        if (!Active) return;
        if (!SteppedIn || !(won || caught))
        {
            Clear();
            return;
        }
        SetUp(caught, rng);
    }

    /// <summary>
    /// <c>PokeRadar_ClearIfAllOutOfView</c>: a patch out of sight stops shaking, and with none left in sight the chain
    /// ends. In sight is the original's screen round the player: eight tiles to either side, six up and down.
    /// </summary>
    public bool KeepInView(int px, int py)
    {
        if (!Active) return false;
        foreach (var p in Patches)
            if (p.Active && (Math.Abs(p.X - px) > 8 || Math.Abs(p.Y - py) > 6)) p.Active = false;
        if (Patches.Any(p => p.Active)) return false;
        Clear();
        return true;
    }
}
