using PokemonPlatinumEngine.Core;
using System;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

// The field of the later games (plan 06 · R19b; docs/mechanics/rulings.md, "The field of the later games"): the
// strong weathers, the four terrains, Aurora Veil and Sticky Web. Nothing in Platinum's rules lays any of them, so
// none of this is reached by a battle of Platinum's: the moves that do (Electric Terrain, Aurora Veil, Sticky Web…)
// are R24 to R26's and the abilities (the Surges, Desolate Land…) R27's, and each calls the entry point here. The
// rules are Showdown's (data/conditions.ts, data/moves.ts, data/abilities.ts at the pinned commit), and every line
// that sets or clears one carries its event, so the screen's look follows (FieldLook.Tell).
public sealed partial class BattleCore
{
    // ---------------------------------------------------------------- the strong weathers

    /// <summary>
    /// Showdown's desolateland, primordialsea and deltastream as their holder comes in: the weather has no end of its
    /// own, no other weather can take its place (<see cref="SetWeather"/> refuses), and it goes once nobody on the
    /// field has the ability that brought it (<see cref="CheckStrongWeather"/>). Another strong weather replaces it.
    /// </summary>
    public bool SetStrongWeather(Battler holder, BattleWeather weather)
    {
        if (weather is not (BattleWeather.HarshSun or BattleWeather.HeavyRain or BattleWeather.StrongWinds))
            throw new ArgumentException($"{weather} is no strong weather", nameof(weather));
        if (Field.Weather == weather)
        {
            Unsaid();
            return false;
        }
        Field.Weather = weather;
        Field.WeatherTurns = 0;
        Field.WeatherLasts = true;
        Field.StrongWeatherBy = holder.Ability?.Name;
        Say(Starts(weather)).With(new WeatherChanged(weather));
        return true;
    }

    /// <summary>
    /// The strong weather goes as the last Pokémon on the field with the ability that brought it leaves (Showdown's
    /// onEnd of the three abilities, which looks for another holder first). <paramref name="leaving"/> is not counted.
    /// </summary>
    private void CheckStrongWeather(Battler? leaving)
    {
        if (!Field.WeatherIsStrong || Field.StrongWeatherBy is not { } by || Result != BattleResult.None) return;
        if (AllBattlers.Any(b => b != leaving && b.IsActive && b.Ability?.Name == by)) return;
        var gone = Field.Weather;
        Field.Weather = BattleWeather.None;
        Field.WeatherLasts = false;
        Field.WeatherTurns = 0;
        Field.StrongWeatherBy = null;
        Say(Stops(gone)).With(new WeatherChanged(BattleWeather.None));
    }

    /// <summary>
    /// The harsh sun burns a damaging Water move away and the heavy rain puts a damaging Fire move out, before it
    /// reaches anyone (Showdown's onTryMove of desolateland and primordialsea): its PP is spent and its line said.
    /// </summary>
    private bool WashedOut(Move move)
    {
        if (move.Category == MoveCategory.Status) return false;
        var sky = Field.SkyInEffect;
        if (sky == BattleWeather.HarshSun && move.Type == PokemonType.Water)
        {
            Say(Text.Of("battle.evaporatedInTheHarshSunlight"));
            return true;
        }
        if (sky == BattleWeather.HeavyRain && move.Type == PokemonType.Fire)
        {
            Say(Text.Of("battle.fizzledOutInTheHeavyRain"));
            return true;
        }
        return false;
    }

    // ---------------------------------------------------------------- the terrains

    /// <summary>The line a terrain comes in with, and the one it goes with.</summary>
    private static string TerrainStarts(FieldCondition terrain) => terrain switch
    {
        FieldCondition.ElectricTerrain => Text.Of("battle.electricCurrentRanAcross"),
        FieldCondition.GrassyTerrain => Text.Of("battle.grassGrewToCover"),
        FieldCondition.MistyTerrain => Text.Of("battle.mistSwirledAround"),
        _ => Text.Of("battle.battlefieldGotWeird")
    };

    private static string TerrainEnds(FieldCondition terrain) => terrain switch
    {
        FieldCondition.ElectricTerrain => Text.Of("battle.theElectricityDisappeared"),
        FieldCondition.GrassyTerrain => Text.Of("battle.theGrassDisappeared"),
        FieldCondition.MistyTerrain => Text.Of("battle.theMistDisappeared"),
        _ => Text.Of("battle.theWeirdnessDisappeared")
    };

    /// <summary>
    /// Lays a terrain for five turns, eight from a Terrain Extender in the setter's hands (Showdown's terrain
    /// conditions' durationCallback). One terrain at a time: a new one takes the old one's place on the same line,
    /// which carries both events. The same terrain already there fails.
    /// </summary>
    public bool SetTerrain(FieldCondition terrain, Battler? setter, string? line = null)
    {
        if (!FieldState.IsTerrain(terrain)) throw new ArgumentException($"{terrain} is no terrain", nameof(terrain));
        if (Field.Has(terrain))
        {
            Unsaid();
            return false;
        }
        var before = Field.Terrain;
        Field.Terrain = terrain;
        // (Found by its name: Platinum's item table gives the later items no hold effect, and an Embargo or Klutz takes it away)
        Field.TerrainTurns = 5 + (setter != null && BattleEffects.ItemInHand(setter)?.Name == "Terrain Extender" ? 3 : 0);
        var said = Say(line ?? TerrainStarts(terrain));
        if (before is { } old && old != terrain) said.With(new FieldConditionChanged(old, Up: false));
        said.With(new FieldConditionChanged(terrain, Up: true));
        TerrainChanged();
        return true;
    }

    /// <summary>The terrain has run its turns, or something has cleared it: its line, and the look taken off.</summary>
    internal void EndTerrain(FieldCondition terrain)
    {
        if (Field.Terrain != terrain) return;
        Field.Terrain = null;
        Field.TerrainTurns = 0;
        Say(TerrainEnds(terrain)).With(new FieldConditionChanged(terrain, Up: false));
        TerrainChanged();
    }

    /// <summary>
    /// Whoever answers a change of terrain (Mimicry, R27) hears of it, in the places' order: Showdown's is by Speed,
    /// which R27 sorts once something answers (a sort may roll for a tie, and nothing answers yet).
    /// </summary>
    private void TerrainChanged()
    {
        foreach (var b in AllBattlers.Where(b => b.IsActive).ToList())
            foreach (var e in BattleEffects.Of(b).ToList()) e.OnTerrainChange(this, b);
    }

    /// <summary>
    /// A terrain keeps a condition off whoever stands on it (Showdown's onSetStatus): Misty Terrain every one,
    /// Electric Terrain sleep. Said only when it was a move's whole purpose (<paramref name="announce"/>).
    /// </summary>
    private bool TerrainKeepsOff(Battler target, StatusCondition status, bool announce)
    {
        bool misty = Field.Has(FieldCondition.MistyTerrain), electric = Field.Has(FieldCondition.ElectricTerrain) && status == StatusCondition.Sleep;
        if (!(misty || electric) || !DamageCalculator.OnTheTerrain(target)) return false;
        if (announce) Say(Text.Of(misty ? "battle.surroundsItselfWithMist" : "battle.surroundsItselfWithElectrifiedTerrain", ("target", target.Name)));
        return true;
    }

    /// <summary>Grassy Terrain gives a sixteenth of its HP back to whoever stands on it at a turn's end (Showdown's grassyterrain onResidual).</summary>
    private void GrassyTerrainHeals(Battler b)
    {
        if (!Field.Has(FieldCondition.GrassyTerrain) || !DamageCalculator.OnTheTerrain(b)) return;
        var p = b.Pokemon!;
        if (p.CurrentHP >= p.MaxHP || b.Volatile.HealBlockTurns > 0) return;
        RestoreHp(b, Math.Max(1, p.MaxHP / 16), Text.Of("battle.restoredByTheGrassyTerrain", ("pokemon", b.Name)));
    }

    // ---------------------------------------------------------------- Aurora Veil and Sticky Web

    /// <summary>
    /// Showdown's auroraveil: Reflect and Light Screen at once over the user's side, for five turns (eight with a
    /// Light Clay), only while hail or snow falls; it fails while it is up. What the move of R24 calls.
    /// </summary>
    internal bool RaiseAuroraVeil(Battler user)
    {
        var side = Field.Side(user.Side);
        if (side.AuroraVeil || Field.WeatherInEffect != BattleWeather.Hail)
        {
            Fails();
            return false;
        }
        side.AuroraVeilTurns = 5 + BattleEffects.Of(user).Sum(e => e.ExtraTurns("Screens"));
        Say(Text.Of("battle.auroraVeilMadeStronger", ("team", TeamOf(user.Side)))).With(Now(user.Side, SideCondition.AuroraVeil));
        return true;
    }

    /// <summary>Showdown's stickyweb: one layer over the other side's ground; a second fails. What the move of R26 calls.</summary>
    internal bool LayStickyWeb(BattleSide theirs)
    {
        var side = Field.Side(theirs);
        if (side.StickyWeb)
        {
            Fails();
            return false;
        }
        side.StickyWeb = true;
        Say(Text.Of("battle.aStickyWebHasBeenLaid", ("team", TeamOf(theirs)))).With(Now(theirs, SideCondition.StickyWeb));
        return true;
    }

    /// <summary>
    /// Sticky Web catches whoever comes in on the ground (Showdown's stickyweb onEntryHazard): a stage of Speed, as if
    /// a foe had lowered it, so Clear Body and its kind keep it off. Magic Guard does not walk through it.
    /// </summary>
    private void CaughtInTheWeb(Battler b)
    {
        if (!b.IsActive || !Field.Side(b.Side).StickyWeb || !IsOnTheGround(b)) return;
        Say(Text.Of("battle.wasCaughtInAStickyWeb", ("pokemon", b.Name)));
        var foe = AllBattlers.FirstOrDefault(o => o.IsActive && o.Side != b.Side);
        ChangeStat(b, StatType.Speed, -1, foe, false, By.Other);
    }

    /// <summary>The later things a side can have, taken off it by a line already said (Defog, Rapid Spin, Brick Break): each its own event.</summary>
    private void ClearLaterSide(BattleSide side, Said line, bool veil, bool web)
    {
        var state = Field.Side(side);
        if (veil && state.AuroraVeil)
        {
            state.AuroraVeilTurns = 0;
            line.With(new SideConditionChanged(side, SideCondition.AuroraVeil, 0));
        }
        if (web && state.StickyWeb)
        {
            state.StickyWeb = false;
            line.With(new SideConditionChanged(side, SideCondition.StickyWeb, 0));
        }
        // And G-Max Steelsurge's steel goes where the web does (plan 06 · R22; Showdown's defog and rapidspin)
        if (web && state.GMaxSteelsurge)
        {
            state.GMaxSteelsurge = false;
            line.With(new SideConditionChanged(side, SideCondition.GMaxSteelsurge, 0));
        }
    }
}
