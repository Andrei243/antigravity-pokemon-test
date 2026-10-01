using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace DataImporter;

/// <summary>
/// Turns a move's battle effect into the <see cref="MoveData"/> fields the engine runs. Whatever the fields can't
/// say is kept as <see cref="MoveData.Effect"/> with <see cref="MoveData.Support"/> Partial (the move still hits) or
/// None (the move does nothing until plan 06 writes the effect). When an effect gets its code, move its name to
/// <see cref="Gen4"/>'s fully supported cases (or the later moves' list) and run the importer again.
/// </summary>
public static class MoveEffects
{
    /// <summary>Damaging effects that only make sense with their condition (Dream Eater, Fake Out): without the
    /// condition in the engine the move would hit when it shouldn't, so it does nothing instead.</summary>
    private static readonly HashSet<string> ConditionalHits = new()
    {
        "DAMAGE_WHILE_ASLEEP", "RECOVER_DAMAGE_SLEEP", "ALWAYS_FLINCH_FIRST_TURN_ONLY", "HIT_FIRST_IF_TARGET_ATTACKING",
        "FAIL_IF_NOT_USED_ALL_OTHER_MOVES", "HIT_LAST_WHIFF_IF_HIT", "HIT_IN_3_TURNS", "ONE_HIT_KO", "SPIT_UP",
        "COUNTER", "MIRROR_COAT", "METAL_BURST", "BIDE", "LEVEL_DAMAGE_FLAT", "40_DAMAGE_FLAT", "20_DAMAGE_FLAT",
        "PSYWAVE", "HALVE_HP", "SET_HP_EQUAL_TO_USER", "RANDOM_DAMAGE_1_TO_150_LEVEL"
    };

    /// <summary>
    /// Applies a Generation 4 battle effect (<c>BATTLE_EFFECT_*</c> without the prefix) with its chance to the move.
    /// Returns true when the fields describe the effect completely.
    /// </summary>
    public static void Gen4(MoveData m, string effect, int chance)
    {
        bool full = true;
        string? partial = null; // the effect is named, and the fields still run

        void Status(StatusCondition s, int pct) { m.InflictStatus = s; m.StatusChancePercent = pct; }
        void Stat(StatType stat, int amount, bool self, int pct, params StatType[] also)
        {
            m.TargetStatChange = stat;
            m.StatStageAmount = amount;
            m.StatChangeTargetSelf = self;
            m.StatChangeChancePercent = pct;
            if (also.Length > 0) m.AlsoChangesStats = also;
        }

        switch (effect)
        {
            case "HIT": case "PRIORITY_1": break;
            case "BYPASS_ACCURACY": case "PRIORITY_NEG_1_BYPASS_ACCURACY": m.Accuracy = 0; break;
            case "HIGH_CRITICAL": m.CritStage = 1; break;

            // Status as a side effect
            case "POISON_HIT": Status(StatusCondition.Poison, chance); break;
            case "BADLY_POISON_HIT": Status(StatusCondition.Toxic, chance); break;
            case "BURN_HIT": Status(StatusCondition.Burn, chance); break;
            case "FREEZE_HIT": Status(StatusCondition.Freeze, chance); break;
            case "PARALYZE_HIT": Status(StatusCondition.Paralyze, chance); break;
            case "THAW_AND_BURN_HIT": Status(StatusCondition.Burn, chance); m.ThawsUser = true; break;
            case "FLINCH_HIT": m.FlinchChancePercent = chance; break;
            case "CONFUSE_HIT": m.ConfuseChancePercent = chance; break;
            case "FLINCH_BURN_HIT": Status(StatusCondition.Burn, chance); m.FlinchChancePercent = chance; break;
            case "FLINCH_FREEZE_HIT": Status(StatusCondition.Freeze, chance); m.FlinchChancePercent = chance; break;
            case "FLINCH_PARALYZE_HIT": Status(StatusCondition.Paralyze, chance); m.FlinchChancePercent = chance; break;
            case "HIGH_CRITICAL_POISON_HIT": m.CritStage = 1; Status(StatusCondition.Poison, chance); break;
            case "HIGH_CRITICAL_BURN_HIT": m.CritStage = 1; Status(StatusCondition.Burn, chance); break;

            // Stat changes as a side effect
            case "LOWER_ATTACK_HIT": Stat(StatType.Attack, -1, false, chance); break;
            case "LOWER_DEFENSE_HIT": Stat(StatType.Defense, -1, false, chance); break;
            case "LOWER_SPEED_HIT": Stat(StatType.Speed, -1, false, chance); break;
            case "LOWER_SP_ATK_HIT": Stat(StatType.SpAttack, -1, false, chance); break;
            case "LOWER_SP_DEF_HIT": Stat(StatType.SpDefense, -1, false, chance); break;
            case "LOWER_ACCURACY_HIT": Stat(StatType.Accuracy, -1, false, chance); break;
            case "LOWER_SP_DEF_2_HIT": Stat(StatType.SpDefense, -2, false, chance); break;
            case "RAISE_ATTACK_HIT": Stat(StatType.Attack, 1, true, chance); break;
            case "RAISE_DEF_HIT": Stat(StatType.Defense, 1, true, chance); break;
            case "RAISE_SP_ATK_HIT": Stat(StatType.SpAttack, 1, true, chance); break;
            case "RAISE_ALL_STATS_HIT":
                Stat(StatType.Attack, 1, true, chance, StatType.Defense, StatType.SpAttack, StatType.SpDefense, StatType.Speed); break;
            case "SPEED_DOWN_HIT": Stat(StatType.Speed, -1, true, 100); break;
            case "DEF_SPD_DOWN_HIT": Stat(StatType.Defense, -1, true, 100, StatType.SpDefense); break;
            case "LOWER_OWN_ATK_AND_DEF": Stat(StatType.Attack, -1, true, 100, StatType.Defense); break;
            case "USER_SP_ATK_DOWN_2": Stat(StatType.SpAttack, -2, true, 100); break;

            // Recoil and drain
            case "RECOIL_QUARTER": m.RecoilPercent = 25; break;
            case "RECOIL_THIRD": m.RecoilPercent = 33; break;
            case "RECOIL_HALF": m.RecoilPercent = 50; break;
            case "RECOIL_BURN_HIT": m.RecoilPercent = 33; Status(StatusCondition.Burn, chance); m.ThawsUser = true; break;
            case "RECOIL_PARALYZE_HIT": m.RecoilPercent = 33; Status(StatusCondition.Paralyze, chance); break;
            case "RECOVER_HALF_DAMAGE_DEALT": m.DrainPercent = 50; break;

            // Status moves
            case "STATUS_SLEEP": Status(StatusCondition.Sleep, 100); break;
            case "STATUS_PARALYZE": Status(StatusCondition.Paralyze, 100); break;
            case "STATUS_BURN": Status(StatusCondition.Burn, 100); break;
            case "STATUS_POISON": Status(StatusCondition.Poison, 100); break;
            case "STATUS_BADLY_POISON": Status(StatusCondition.Toxic, 100); break;
            case "STATUS_CONFUSE": case "CONFUSE_ALL": m.ConfuseChancePercent = 100; break;
            case "ATK_UP": Stat(StatType.Attack, 1, true, 100); break;
            case "ATK_UP_2": Stat(StatType.Attack, 2, true, 100); break;
            case "DEF_UP": Stat(StatType.Defense, 1, true, 100); break;
            case "DEF_UP_2": Stat(StatType.Defense, 2, true, 100); break;
            case "SP_ATK_UP": Stat(StatType.SpAttack, 1, true, 100); break;
            case "SP_ATK_UP_2": Stat(StatType.SpAttack, 2, true, 100); break;
            case "SP_DEF_UP_2": Stat(StatType.SpDefense, 2, true, 100); break;
            case "SPEED_UP_2": Stat(StatType.Speed, 2, true, 100); break;
            case "EVA_UP": Stat(StatType.Evasion, 1, true, 100); break;
            case "ATK_DEF_UP": Stat(StatType.Attack, 1, true, 100, StatType.Defense); break;
            case "DEF_SPD_UP": Stat(StatType.Defense, 1, true, 100, StatType.SpDefense); break;
            case "ATK_SPD_UP": Stat(StatType.Attack, 1, true, 100, StatType.Speed); break;
            case "SP_ATK_SP_DEF_UP": Stat(StatType.SpAttack, 1, true, 100, StatType.SpDefense); break;
            case "ATK_DOWN": Stat(StatType.Attack, -1, false, 100); break;
            case "ATK_DOWN_2": Stat(StatType.Attack, -2, false, 100); break;
            case "DEF_DOWN": Stat(StatType.Defense, -1, false, 100); break;
            case "DEF_DOWN_2": Stat(StatType.Defense, -2, false, 100); break;
            case "SPEED_DOWN": Stat(StatType.Speed, -1, false, 100); break;
            case "SPEED_DOWN_2": Stat(StatType.Speed, -2, false, 100); break;
            case "SP_DEF_DOWN_2": Stat(StatType.SpDefense, -2, false, 100); break;
            case "ACC_DOWN": Stat(StatType.Accuracy, -1, false, 100); break;
            case "EVA_DOWN": Stat(StatType.Evasion, -1, false, 100); break;
            case "ATK_DEF_DOWN": Stat(StatType.Attack, -1, false, 100, StatType.Defense); break;
            case "ATK_UP_2_STATUS_CONFUSION": Stat(StatType.Attack, 2, false, 100); m.ConfuseChancePercent = 100; break;
            case "SP_ATK_UP_CAUSE_CONFUSION": Stat(StatType.SpAttack, 1, false, 100); m.ConfuseChancePercent = 100; break;
            case "RESTORE_HALF_HP": m.HealPercent = 50; break;

            // The fields run and the rest waits for its code (weather, two-turn moves, semi-invulnerable targets)
            case "HEAL_HALF_MORE_IN_SUN": m.HealPercent = 50; partial = effect; break;
            case "HEAL_HALF_REMOVE_FLYING_TYPE": m.HealPercent = 50; partial = effect; break;
            case "DEF_UP_DOUBLE_ROLLOUT_POWER": Stat(StatType.Defense, 1, true, 100); partial = effect; break;
            case "EVA_UP_2_MINIMIZE": Stat(StatType.Evasion, 2, true, 100); partial = effect; break;
            case "SP_DEF_UP_DOUBLE_ELECTRIC_POWER": Stat(StatType.SpDefense, 1, true, 100); partial = effect; break;
            case "THUNDER": Status(StatusCondition.Paralyze, chance); partial = effect; break;
            case "BLIZZARD": Status(StatusCondition.Freeze, chance); partial = effect; break;
            case "POISON_MULTI_HIT": Status(StatusCondition.Poison, chance); partial = effect; break;
            case "FLINCH_MINIMIZE_DOUBLE_HIT": m.FlinchChancePercent = chance; partial = effect; break;
            case "FLINCH_DOUBLE_DAMAGE_FLY_OR_BOUNCE": m.FlinchChancePercent = chance; partial = effect; break;
            case "CHARGE_TURN_HIGH_CRIT": m.CritStage = 1; partial = effect; break;
            case "CHARGE_TURN_HIGH_CRIT_FLINCH": m.CritStage = 1; m.FlinchChancePercent = chance; partial = effect; break;
            case "BOUNCE": Status(StatusCondition.Paralyze, chance); partial = effect; break;

            default:
                full = false;
                break;
        }

        if (partial != null)
        {
            m.Effect = Names.Pascal(partial, "");
            m.Support = MoveEffectSupport.Partial;
        }
        else if (!full)
        {
            m.Effect = Names.Pascal(effect, "");
            if (chance > 0) m.EffectChance = chance;
            bool hits = m.Category != MoveCategory.Status && m.Power > 0 && !ConditionalHits.Contains(effect);
            m.Support = hits ? MoveEffectSupport.Partial : MoveEffectSupport.None;
        }
    }

    /// <summary>Later moves' PokeAPI effect ids whose behaviour the meta fields describe completely (checked by hand).</summary>
    public static readonly HashSet<int> FullLaterEffects = new()
    {
        21, 62, 278, 291, 296, 297, 323, 328, 329, 330, 331, 335, 344, 346, 349, 357, 358, 359, 365, 372, 375, 379, 381,
        390, 396, 397, 405, 406, 412, 467, 469
    };

    /// <summary>Fills the side-effect fields of a later move from PokeAPI's move_meta. False when they can't hold it.</summary>
    public static bool FromMeta(MoveData m, CsvRow meta, IEnumerable<(int Stat, int Change)> statChanges, bool selfTargeted)
    {
        bool expressible = true;
        int chance(int raw) => raw == 0 ? 100 : raw;
        int category = meta.Int("meta_category_id");

        switch (meta.Int("meta_ailment_id"))
        {
            case 0: break;
            case 1: m.InflictStatus = StatusCondition.Paralyze; m.StatusChancePercent = chance(meta.Int("ailment_chance")); break;
            case 2: m.InflictStatus = StatusCondition.Sleep; m.StatusChancePercent = chance(meta.Int("ailment_chance")); break;
            case 3: m.InflictStatus = StatusCondition.Freeze; m.StatusChancePercent = chance(meta.Int("ailment_chance")); break;
            case 4: m.InflictStatus = StatusCondition.Burn; m.StatusChancePercent = chance(meta.Int("ailment_chance")); break;
            case 5: m.InflictStatus = StatusCondition.Poison; m.StatusChancePercent = chance(meta.Int("ailment_chance")); break;
            case 6: m.ConfuseChancePercent = chance(meta.Int("ailment_chance")); break;
            default: expressible = false; break;
        }

        m.FlinchChancePercent = meta.Int("flinch_chance");
        int crit = meta.Int("crit_rate");
        if (crit is 1 or 2) m.CritStage = crit;
        else if (crit != 0) expressible = false;

        int drain = meta.Int("drain");
        if (drain > 0) m.DrainPercent = drain;
        else if (drain < 0) m.RecoilPercent = -drain;
        if (meta.Int("healing") > 0) m.HealPercent = meta.Int("healing");
        if (meta.IntOrNull("min_hits") != null) expressible = false; // multi-hit (min_turns is only how long a status lasts)

        var changes = statChanges.ToList();
        if (changes.Count > 0)
        {
            if (changes.Any(c => c.Change != changes[0].Change) || changes.Any(c => c.Stat is < 2 or > 8)) return false;
            bool self = selfTargeted || category == 7; // 7: damage that raises the user's stats
            var stats = changes.Select(c => StatOf(c.Stat)).ToArray();
            m.TargetStatChange = stats[0];
            m.StatStageAmount = changes[0].Change;
            m.StatChangeTargetSelf = self;
            m.StatChangeChancePercent = chance(meta.Int("stat_chance"));
            if (stats.Length > 1) m.AlsoChangesStats = stats[1..];
        }
        return expressible;
    }

    private static StatType StatOf(int pokeApiStat) => pokeApiStat switch
    {
        2 => StatType.Attack,
        3 => StatType.Defense,
        4 => StatType.SpAttack,
        5 => StatType.SpDefense,
        6 => StatType.Speed,
        7 => StatType.Accuracy,
        8 => StatType.Evasion,
        _ => StatType.HP
    };

    /// <summary>Clears every side-effect field, for a move that does nothing until its effect is written.</summary>
    public static void ClearFields(MoveData m)
    {
        m.InflictStatus = StatusCondition.None;
        m.StatusChancePercent = 0;
        m.TargetStatChange = null;
        m.StatStageAmount = 0;
        m.StatChangeTargetSelf = false;
        m.StatChangeChancePercent = 0;
        m.AlsoChangesStats = null;
        m.RecoilPercent = 0;
        m.DrainPercent = 0;
        m.CritStage = 0;
        m.FlinchChancePercent = 0;
        m.ConfuseChancePercent = 0;
        m.HealPercent = 0;
        m.ThawsUser = false;
    }
}
