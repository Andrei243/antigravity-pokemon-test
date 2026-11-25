using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

// Carpathia's signature moves and abilities (plan 46 · CP10, ours; docs/mechanics/rulings.md, "Carpathia's signature moves
// and abilities"): what the proposals' sentences ask where the rules already bite. Three Heads, Anvil and Echo Pouch are
// abilities the core reads by name, as Parental Bond is (their marks: Turn.HeadStrike in the damage formula's power chain,
// the miss in AfterStrike, the echoes at the turn's end beside Future Sight); Eclipse tells the Vârcolac's tale from the
// ability; Sun Seizure, Moon Howl, Hailstorm Rider and Banner Howl are entries of the move table below. Codru's Ambush is
// First Impression's own rule and Smith's Hammer and Delta Horn their data's.
public sealed partial class BattleCore
{
    // ---------------------------------------------------------------- Three Heads

    /// <summary>
    /// Three Heads: its first damaging move of a turn, of one hit at one target, strikes three times, each at 40% of its
    /// power (<see cref="TurnFlags.HeadStrike"/> in the formula; a spread move, a charge, a fixed amount and a move that
    /// already strikes several times are left as they are, as Parental Bond leaves them).
    /// </summary>
    private void ThreeHeads(MoveUse use)
    {
        var user = use.User;
        if (!Has(user, "Three Heads") || user.Turn.HeadsUsed || use.StrikeCount != 1 || use.Targets.Count != 1 || use.Effect.Deals != null || use.Effect.SelfKo) return;
        if (use.Move.Category == MoveCategory.Status || use.Move.Data.Kind != MoveKind.Standard) return;
        use.StrikeCount = 3;
        user.Turn.HeadStrike = user.Turn.HeadsUsed = true;
    }

    // ---------------------------------------------------------------- Anvil

    /// <summary>
    /// Anvil: a contact move that missed (not one the target's guard stopped or that had nothing to reach) still deals a
    /// quarter of what it would have, with a line, unless a Substitute or a type that is immune is in the way.
    /// </summary>
    private void AnvilGlances(MoveUse use, MoveHit hit)
    {
        var user = use.User;
        var t = hit.Target;
        if (!Has(user, "Anvil") || !use.Data.MakesContact || use.Move.Category == MoveCategory.Status) return;
        if (t == user || !t.IsActive || user.Pokemon is not { IsFainted: false } || t.HasSubstitute) return;
        if (DamageCalculator.Effectiveness(user, t, use.Move, Rules) == 0f) return;
        var result = DamageCalculator.Calculate(user, t, use.Move, rng, spread: false, rules: Rules, noCrit: true);
        LoseHp(t, Math.Max(1, result.Damage / 4), Text.Of("battle.anvilGlances", ("pokemon", user.Name), ("ability", AbilityOf(user)), ("target", t.Name)), direct: true);
    }

    // ---------------------------------------------------------------- Echo Pouch

    /// <summary>A sound move struck again: who used it (and which Pokémon it was), where it landed and on what turn.</summary>
    private readonly List<(Place User, Pokemon By, Place Target, Move Move, int Turn)> echoes = new();

    /// <summary>Echo Pouch: each target a damaging sound move landed on is struck again at the end of the turn after, at half power.</summary>
    private void EchoPouch(MoveUse use)
    {
        var user = use.User;
        if (!Has(user, "Echo Pouch") || (use.Data.Flags & MoveFlags.Sound) == 0 || use.Move.Category == MoveCategory.Status || use.Data.Kind != MoveKind.Standard) return;
        foreach (var hit in use.Hits.Where(h => h.Landed && h.Target != user))
            echoes.Add((user.Place, user.Pokemon!, hit.Target.Place, use.Move, Turn));
    }

    /// <summary>
    /// The echoes of the turn before land, in the order they were made: half the move's damage on whoever stands there now,
    /// as long as the Pokémon that made it is still out (and a Substitute takes it). False when the battle ended.
    /// </summary>
    private bool EchoesLand()
    {
        if (echoes.Count == 0) return true;
        foreach (var echo in echoes.Where(e => Turn > e.Turn).ToList())
        {
            echoes.Remove(echo);
            var user = At(echo.User);
            var t = At(echo.Target);
            if (!user.IsActive || user.Pokemon != echo.By || !t.IsActive || t == user) continue;
            if (DamageCalculator.Effectiveness(user, t, echo.Move, Rules) == 0f) continue;
            var line = Text.Of("battle.echoLands", ("move", echo.Move.DisplayName), ("target", t.Name));
            var result = DamageCalculator.Calculate(user, t, echo.Move, rng, spread: false, rules: Rules, powerTenths: 5, noCrit: true);
            int damage = Math.Max(1, result.Damage);
            if (t.HasSubstitute)
            {
                int taken = Math.Min(t.Volatile.SubstituteHp, damage);
                t.Volatile.SubstituteHp -= taken;
                Say(line);
                if (t.Volatile.SubstituteHp == 0) Say(Text.Of("battle.substituteFaded", ("target", t.Name)));
            }
            else LoseHp(t, damage, line, direct: true);
            if (!Settle()) return false;
        }
        return true;
    }

    // ---------------------------------------------------------------- Eclipse

    /// <summary>
    /// A tale told from an ability (Eclipse): no trainer tells it, so no trainer's one-tale-a-battle count and no Book
    /// are touched, and a tale of the trainers' own may be told after it ends. Nothing under rules without the Tales, under
    /// a run with no later mechanics, or while a tale holds the field already. The tale's teller is the holder's side, so
    /// whoever of that side is bound to the tale wakes into its form.
    /// </summary>
    public bool TellTaleFromAbility(Battler holder, string tale, string line)
    {
        if (!Rules.Tales || LaterMechanicsBarred || Field.Tale != null || TaleTable.Get(tale) is not { } rule)
        {
            Unsaid();
            return false;
        }
        int turns = TaleTable.Turns;
        Say(line).With(new TaleTold(holder.Side, tale, turns));
        Say(Text.Of($"basme.{tale}.told"));
        bool brought = rule.Weather is { } weather && SetWeather(weather, turns, Starts(weather));
        Field.Tale = new TaleState(tale, holder.Side, turns, rule, brought);
        foreach (var mine in AllBattlers.Where(x => x.Side == holder.Side && x.IsActive)) WakeToTheTale(mine);
        return true;
    }

    // ---------------------------------------------------------------- the moves

    private static readonly Dictionary<string, MoveEffect> CarpathiaEffects = new()
    {
        // Sun Seizure: harsh sunlight for five turns that no weather move or ability can replace (Showdown's strong weather rule
        // without a holder), once it has hit
        ["SunSeizure"] = new() { Then = (b, use) => b.SeizeTheSun() },
        // Moon Howl: the weather is blown away once it has hit every foe (a strong weather a holder keeps stays)
        ["MoonHowl"] = new() { Then = (b, use) => b.BlowTheWeatherAway() },
        // Hailstorm Rider: the weather of the tale told over the field, or rain where none brings one, for five turns
        ["HailstormRider"] = new() { Then = (b, use) => b.RideTheStorm() },
        // Banner Howl: a stage of Attack and Sp. Atk to its user and its allies once it has hit
        ["BannerHowl"] = new() { Then = (b, use) => b.RaiseTheBanner(use.User) }
    };

    /// <summary>Sun Seizure: the harsh sun for five turns (not over a strong weather an ability holds, nor again over its own while one lasts).</summary>
    private void SeizeTheSun()
    {
        if (Field.StrongWeatherBy != null || Field.WeatherLasts && Field.Weather == BattleWeather.HarshSun) return;
        if (Field.Weather == BattleWeather.HarshSun)
        {
            Field.WeatherTurns = 5;
            return;
        }
        Field.Weather = BattleWeather.HarshSun;
        Field.WeatherTurns = 5;
        Field.WeatherLasts = false;
        Say(Starts(BattleWeather.HarshSun)).With(new WeatherChanged(BattleWeather.HarshSun));
    }

    /// <summary>Moon Howl: the weather goes, with its line, unless a Pokémon's ability keeps it (Desolate Land, Noonday and the rest).</summary>
    private void BlowTheWeatherAway()
    {
        if (Field.Weather == BattleWeather.None || Field.StrongWeatherBy != null) return;
        var gone = Field.Weather;
        Field.Weather = BattleWeather.None;
        Field.WeatherTurns = 0;
        Field.WeatherLasts = false;
        Say(Stops(gone)).With(new WeatherChanged(BattleWeather.None));
    }

    /// <summary>Hailstorm Rider: the weather the tale over the field brings (rain's or hail's), or rain, for five turns.</summary>
    private void RideTheStorm()
    {
        var weather = TaleInForce?.Weather ?? BattleWeather.Rain;
        SetWeather(weather, 5, Starts(weather));
    }

    /// <summary>Banner Howl: a stage of Attack and Sp. Atk to everyone of the user's side on the field (the user included).</summary>
    private void RaiseTheBanner(Battler user)
    {
        foreach (var ally in AllBattlers.Where(x => x.IsActive && x.Side == user.Side && !AreFoes(user, x)).ToList())
        {
            ChangeStat(ally, StatType.Attack, 1, user, false, By.Move);
            ChangeStat(ally, StatType.SpAttack, 1, user, false, By.Move);
        }
    }
}
