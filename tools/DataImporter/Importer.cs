using System.Text.Json;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace DataImporter;

/// <summary>
/// Builds the game's species, moves, abilities and items. Generation 1–4 (national numbers 1–493, moves 1–467,
/// abilities 1–123, Platinum's items) come from Platinum's own data in the decompilation; everything later comes
/// from PokeAPI. Names, categories, egg groups, colours, shapes and the short effect texts come from PokeAPI too.
/// </summary>
public sealed partial class Importer
{
    public const int LastPlatinumSpecies = 493;
    public const int LastPlatinumMove = 467;

    /// <summary>PokeAPI's ids of the forms other than a species' own (10001 on). The evolutions that start from one
    /// (Galarian Meowth into Perrserker) are added with the forms, by <see cref="AddFormEvolutions"/>.</summary>
    private const int FirstAlternateForm = 10000;

    /// <summary>Learnsets for later species come from the newest of these games that has one (PokeAPI version groups).</summary>
    private static readonly int[] LearnsetVersionGroups = { 25, 27, 26, 20, 21, 22, 18, 17, 16, 15, 14, 11, 24, 23, 30, 31, 32, 19 };

    /// <summary>PokeAPI item categories brought in for items after Generation 4: what can be used or held in battle,
    /// healed with, caught with or evolved with. Mega Stones, Z-Crystals, TRs and the like come with plan 06's
    /// sessions on those mechanics.</summary>
    private static readonly HashSet<int> LaterItemCategories = new()
    {
        1, 2, 3, 4, 5, 6, 7, 10, 12, 13, 14, 15, 16, 17, 18, 19, 26, 27, 28, 29, 30, 33, 34, 39, 42, 45, 50,
        MegaStones, ZCrystals
    };

    /// <summary>PokeAPI's categories for the Mega Stones and the Z-Crystals (R1: brought in with whose they are).</summary>
    private const int MegaStones = 44, ZCrystals = 46;

    private readonly Decomp decomp;
    private readonly PokeApi api;
    private readonly int lastSpecies;

    private readonly List<string> speciesConstants, moveConstants, abilityConstants, itemConstants;

    public Importer(Decomp decomp, PokeApi api, int lastSpecies)
    {
        this.decomp = decomp;
        this.api = api;
        this.lastSpecies = lastSpecies;
        speciesConstants = decomp.Constants("species");
        moveConstants = decomp.Constants("moves");
        abilityConstants = decomp.Constants("abilities");
        itemConstants = decomp.Constants("items");
    }

    // ================================================================== moves

    /// <summary>Generation 4's moves and every later one, except the Z-Moves and Max Moves (plan 06 · R20–R21).</summary>
    public List<MoveData> Moves()
    {
        var moves = new List<MoveData>();
        var flags = api.Table("move_flag_map").GroupBy(r => r.Int("move_id")).ToDictionary(g => g.Key, g => g.Select(r => r.Int("move_flag_id")).ToHashSet());
        var statChanges = api.Table("move_meta_stat_changes").GroupBy(r => r.Int("move_id"))
            .ToDictionary(g => g.Key, g => g.Select(r => (r.Int("stat_id"), r.Int("change"))).ToList());

        // Which Generation 4 effect each PokeAPI effect id stands for, so later moves with the same effect share it
        var gen4EffectOf = new Dictionary<int, string>();

        for (int id = 1; id <= LastPlatinumMove; id++)
        {
            var d = decomp.Move(moveConstants[id]);
            var row = api.Moves[id];
            string effect = d.GetProperty("effect").GetProperty("type").GetString()!["BATTLE_EFFECT_".Length..];
            int chance = d.GetProperty("effect").GetProperty("chance").GetInt32();
            if (row["effect_id"] != "") gen4EffectOf.TryAdd(row.Int("effect_id"), effect);

            var m = new MoveData
            {
                Id = id,
                Name = api.MoveName(id),
                Generation = row.Int("generation_id"),
                Type = Gen4Type(d.GetProperty("type").GetString()!),
                Category = Enum.Parse<MoveCategory>(Names.Pascal(d.GetProperty("class").GetString()!, "CLASS_")),
                Power = d.GetProperty("power").GetInt32() is var power && power <= 1 ? 0 : power, // 1 marks variable power
                Accuracy = d.GetProperty("accuracy").GetInt32(),
                MaxPP = d.GetProperty("pp").GetInt32(),
                Priority = d.GetProperty("priority").GetInt32(),
                Target = Gen4Target(d.GetProperty("range").GetString()!),
                Description = MoveText(row, chance)
            };
            var gen4Flags = d.GetProperty("flags").EnumerateArray().Select(f => f.GetString()!).ToHashSet();
            m.Flags = (gen4Flags.Contains("MOVE_FLAG_MAKES_CONTACT") ? MoveFlags.Contact : 0)
                | (gen4Flags.Contains("MOVE_FLAG_CAN_PROTECT") ? MoveFlags.Protect : 0)
                | (gen4Flags.Contains("MOVE_FLAG_CAN_MAGIC_COAT") ? MoveFlags.Reflectable : 0)
                | (gen4Flags.Contains("MOVE_FLAG_CAN_SNATCH") ? MoveFlags.Snatch : 0)
                | (gen4Flags.Contains("MOVE_FLAG_CAN_MIRROR_MOVE") ? MoveFlags.Mirror : 0)
                | (gen4Flags.Contains("MOVE_FLAG_TRIGGERS_KINGS_ROCK") ? MoveFlags.KingsRock : 0)
                | LaterFlags(flags.GetValueOrDefault(id));
            MoveEffects.Gen4(m, effect, chance);
            m.BattleEffect = Names.Pascal(effect, "");
            m.Modern = ModernValues(m, row);
            moves.Add(m);
        }

        foreach (var row in api.Moves.Values.Where(r => r.Int("id") > LastPlatinumMove && r.Int("id") < FirstAlternateForm).OrderBy(r => r.Int("id")))
        {
            string ident = row["identifier"];
            int id = row.Int("id");
            if (ident.Contains("--") || ident.StartsWith("max-") || ident.StartsWith("g-max-") || SignatureZMoves.Contains(id)) continue;
            int? effectId = row.IntOrNull("effect_id");
            int effectChance = row.IntOrNull("effect_chance") ?? 0;
            var m = new MoveData
            {
                Id = id,
                Name = api.MoveName(id),
                Generation = row.Int("generation_id"),
                Type = api.Type(row.Int("type_id")),
                Category = row.Int("damage_class_id") switch { 1 => MoveCategory.Status, 2 => MoveCategory.Physical, _ => MoveCategory.Special },
                Power = row.IntOrNull("power") ?? 0,
                Accuracy = row.IntOrNull("accuracy") ?? 0,
                MaxPP = row.IntOrNull("pp") ?? 5,
                Priority = row.Int("priority"),
                Target = LaterTarget(row.Int("target_id")),
                Flags = (flags.GetValueOrDefault(id)?.Contains(1) == true ? MoveFlags.Contact : 0)
                    | (flags.GetValueOrDefault(id)?.Contains(4) == true ? MoveFlags.Protect : 0)
                    | (flags.GetValueOrDefault(id)?.Contains(5) == true ? MoveFlags.Reflectable : 0)
                    | (flags.GetValueOrDefault(id)?.Contains(6) == true ? MoveFlags.Snatch : 0)
                    | (flags.GetValueOrDefault(id)?.Contains(7) == true ? MoveFlags.Mirror : 0)
                    | LaterFlags(flags.GetValueOrDefault(id)),
                Description = MoveText(row, effectChance)
            };

            bool self = m.Target is MoveTarget.User or MoveTarget.UserSide or MoveTarget.UserAndAllies;
            bool expressible = api.MoveMeta.TryGetValue(id, out var meta) &&
                MoveEffects.FromMeta(m, meta, statChanges.GetValueOrDefault(id) ?? new(), self);

            MoveEffectSupport support;
            string? effect = null;
            if (effectId is { } e && gen4EffectOf.TryGetValue(e, out var gen4))
            {
                // Same effect as a Platinum move: the same verdict (and name) as that move gets
                var probe = new MoveData { Category = m.Category, Power = m.Power };
                MoveEffects.Gen4(probe, gen4, effectChance);
                support = probe.Support;
                effect = probe.Effect;
            }
            else if (effectId is { } known && MoveEffects.FullLaterEffects.Contains(known))
            {
                support = MoveEffectSupport.Full;
            }
            else
            {
                support = MoveEffectSupport.None;
                effect = Names.Pascal(FirstMoveWithEffect(effectId) ?? ident, "");
            }
            if (support == MoveEffectSupport.Full && !expressible)
            {
                support = MoveEffectSupport.None;
                effect ??= Names.Pascal(FirstMoveWithEffect(effectId) ?? ident, "");
            }
            if (support != MoveEffectSupport.Full)
            {
                bool hits = m.Category != MoveCategory.Status && m.Power > 0;
                support = hits ? MoveEffectSupport.Partial : MoveEffectSupport.None;
                m.Effect = effect;
                if (effectChance > 0) m.EffectChance = effectChance;
                if (support == MoveEffectSupport.None || !expressible) MoveEffects.ClearFields(m);
            }
            else if (effect != null)
            {
                // Run by the engine's own code for the effect, which finds it by this name
                m.Effect = effect;
                if (effectChance > 0) m.EffectChance = effectChance;
            }
            m.Support = support;
            moves.Add(m);
        }
        return moves;
    }

    /// <summary>
    /// What the newest games give one of Platinum's moves where that differs from Platinum's own values (PokeAPI's
    /// <c>moves.csv</c> holds the current ones): the values a game played by the modern rules uses. Null when
    /// nothing differs. The target is left as Platinum has it: the two sources describe it in different terms.
    /// </summary>
    private MoveValues? ModernValues(MoveData platinum, CsvRow row)
    {
        int power = row.IntOrNull("power") ?? 0, accuracy = row.IntOrNull("accuracy") ?? 0;
        var category = row.Int("damage_class_id") switch { 1 => MoveCategory.Status, 2 => MoveCategory.Physical, _ => MoveCategory.Special };
        var v = new MoveValues
        {
            Power = power != platinum.Power ? power : null,
            Accuracy = accuracy != platinum.Accuracy ? accuracy : null,
            MaxPP = row.IntOrNull("pp") is { } pp && pp != platinum.MaxPP ? pp : null,
            Priority = row.Int("priority") != platinum.Priority ? row.Int("priority") : null,
            Type = api.Type(row.Int("type_id")) != platinum.Type ? api.Type(row.Int("type_id")) : null,
            Category = category != platinum.Category ? category : null
        };
        bool differs = v.Power != null || v.Accuracy != null || v.MaxPP != null || v.Priority != null || v.Type != null || v.Category != null;
        return differs ? v : null;
    }

    // ================================================================== the later mechanics' moves

    private static readonly Dictionary<string, StatType> ShowdownStats = new()
    {
        ["atk"] = StatType.Attack, ["def"] = StatType.Defense, ["spa"] = StatType.SpAttack, ["spd"] = StatType.SpDefense,
        ["spe"] = StatType.Speed, ["accuracy"] = StatType.Accuracy, ["evasion"] = StatType.Evasion
    };

    /// <summary>The first id given to G-Max Moves, which have no number of their own in the games' move list.</summary>
    public const int FirstGMaxMoveId = 2001;

    /// <summary>
    /// Adds what Z-Moves and Dynamax need (plan 06 · R21–R22) from Pokémon Showdown's move table: each damaging
    /// move's power as a Z-Move and as a Max Move, what Z-Power adds to each status move, and the Z-Moves, Max
    /// Moves and G-Max Moves themselves, which do nothing until those sessions write them.
    /// </summary>
    public void AddLaterMechanics(List<MoveData> moves, Showdown showdown)
    {
        var standard = showdown.Moves.Where(e => !e.Has("isZ") && !e.Has("isMax") && e.Number("num") is > 0)
            .GroupBy(e => e.Number("num")!.Value).ToDictionary(g => g.Key, g => g.First());

        foreach (var m in moves)
        {
            if (!standard.TryGetValue(m.Id, out var e)) continue;
            string? z = e.Fields.GetValueOrDefault("zMove"), max = e.Fields.GetValueOrDefault("maxMove");
            if (e.Text("category") == "Status")
            {
                m.ZBonus = ZBonusOf(z, m.Name);
                continue;
            }
            if (m.Name == "Struggle") continue;
            int power = e.Number("basePower") ?? 0;
            m.ZPower = Showdown.Inner(z, "basePower") ?? ZPowerOf(power, e.Fields.GetValueOrDefault("multihit")?.StartsWith('[') == true);
            m.MaxPower = Showdown.Inner(max, "basePower") ?? MaxPowerOf(power, e.Text("type")!);
        }

        // A type's Z-Crystal is the one that names no move of its own
        var typeCrystals = showdown.Items.Where(i => i.Is("zMove", "true")).Select(i => i.Id).ToHashSet();
        int gmax = FirstGMaxMoveId;
        foreach (var e in showdown.Moves.Where(e => e.Has("isZ") || e.Has("isMax")).OrderBy(e => e.Name, StringComparer.Ordinal))
        {
            bool isZ = e.Has("isZ"), isGMax = e.Text("isMax") != null;
            bool ofOneSpecies = isZ && !typeCrystals.Contains(e.Text("isZ")!);
            var type = Enum.Parse<PokemonType>(e.Text("type")!);
            int power = e.Number("basePower") ?? 0;
            var m = new MoveData
            {
                Id = isGMax ? gmax++ : e.Number("num")!.Value,
                Name = e.Name,
                Generation = isZ ? 7 : 8,
                Type = type,
                Category = Enum.Parse<MoveCategory>(e.Text("category")!),
                // 1 and 10 stand for "takes its power from the move it is made from"
                Power = power is 1 or 10 ? 0 : power,
                Accuracy = 0,
                MaxPP = e.Number("pp") ?? 1,
                Priority = e.Number("priority") ?? 0,
                Target = isZ || e.Text("target") is "normal" or "adjacentFoe" ? MoveTarget.Selected : MoveTarget.User,
                Kind = isZ ? MoveKind.ZMove : isGMax ? MoveKind.GMaxMove : MoveKind.MaxMove,
                GigantamaxOf = e.Text("isMax"),
                Effect = isZ ? "ZMove" : isGMax ? "GMaxMove" : "MaxMove",
                Support = MoveEffectSupport.None,
                Description = ofOneSpecies ? "A Z-Move one species makes out of its own move with its Z-Crystal."
                    : isZ ? $"The {type}-type Z-Move. Its power comes from the move it is made out of."
                    : isGMax ? $"The {type}-type Max Move of Gigantamax {e.Text("isMax")}."
                    : e.Text("category") == "Status" ? "What every status move becomes under Dynamax: it protects the user."
                    : $"The {type}-type Max Move. Its power comes from the move it is made out of."
            };
            moves.Add(m);
        }
    }

    /// <summary>Showdown's rule for a move without a Z power of its own (<c>sim/dex-moves.ts</c>).</summary>
    public static int ZPowerOf(int basePower, bool hitsSeveralTimes)
    {
        if (hitsSeveralTimes) basePower *= 3;
        return basePower switch
        {
            0 => 100, >= 140 => 200, >= 130 => 195, >= 120 => 190, >= 110 => 185, >= 100 => 180,
            >= 90 => 175, >= 80 => 160, >= 70 => 140, >= 60 => 120, _ => 100
        };
    }

    /// <summary>Showdown's rule for a move without a Max power of its own: Fighting and Poison moves get less.</summary>
    public static int MaxPowerOf(int basePower, string type)
    {
        if (basePower == 0) return 100;
        bool low = type is "Fighting" or "Poison";
        return basePower switch
        {
            >= 150 => low ? 100 : 150, >= 110 => low ? 95 : 140, >= 75 => low ? 90 : 130, >= 65 => low ? 85 : 120,
            >= 55 => low ? 80 : 110, >= 45 => low ? 75 : 100, _ => low ? 70 : 90
        };
    }

    private static ZBonus? ZBonusOf(string? zMove, string move)
    {
        if (zMove == null) return null;
        if (Showdown.InnerText(zMove, "effect") is { } effect)
        {
            return new ZBonus
            {
                Effect = effect switch
                {
                    "clearnegativeboost" => "ClearNegativeBoost", "heal" => "Heal", "healreplacement" => "HealReplacement",
                    "crit2" => "Crit2", "redirect" => "Redirect", "curse" => "Curse",
                    _ => throw new InvalidDataException($"Unknown Z-Power effect {effect} on {move}")
                }
            };
        }
        var boosts = Showdown.InnerTable(zMove, "boost");
        if (boosts.Count == 0) return null;
        if (boosts.Any(b => b.Value != boosts[0].Value)) throw new InvalidDataException($"{move}'s Z-Power raises its stats by different amounts");
        return new ZBonus { Stats = boosts.Select(b => ShowdownStats[b.Key]).ToArray(), Stages = boosts[0].Value };
    }

    /// <summary>Z-Moves named after one species' Z-Crystal (the generic ones have "--physical" ids).</summary>
    private static readonly HashSet<int> SignatureZMoves = new() { 658, 695, 696, 697, 698, 699, 700, 701, 702, 703, 719, 723, 724, 725, 726, 727, 728 };

    private string? FirstMoveWithEffect(int? effectId) => effectId == null
        ? null
        : api.Moves.Values.Where(r => r.IntOrNull("effect_id") == effectId).OrderBy(r => r.Int("id")).Select(r => r["identifier"]).FirstOrDefault();

    private string MoveText(CsvRow move, int chance) =>
        move["effect_id"] == "" ? "" : api.Prose("move_effect_prose", "move_effect_id", move.Int("effect_id"), chance);

    private static MoveFlags LaterFlags(HashSet<int>? ids)
    {
        if (ids == null) return MoveFlags.None;
        var f = MoveFlags.None;
        if (ids.Contains(8)) f |= MoveFlags.Punch;
        if (ids.Contains(9)) f |= MoveFlags.Sound;
        if (ids.Contains(16)) f |= MoveFlags.Bite;
        if (ids.Contains(17)) f |= MoveFlags.Pulse;
        if (ids.Contains(18)) f |= MoveFlags.Ballistic;
        if (ids.Contains(15)) f |= MoveFlags.Powder;
        if (ids.Contains(21)) f |= MoveFlags.Dance;
        return f;
    }

    /// <summary>Platinum's types; Curse's "???" type became Ghost in Generation 5, which is what this game uses.</summary>
    private static PokemonType Gen4Type(string constant) =>
        constant == "TYPE_MYSTERY" ? PokemonType.Ghost : Enum.Parse<PokemonType>(constant["TYPE_".Length..], ignoreCase: true);

    private static MoveTarget Gen4Target(string range) => range switch
    {
        "RANGE_SINGLE_TARGET" or "RANGE_SINGLE_TARGET_SPECIAL" or "RANGE_SINGLE_TARGET_ME_FIRST" => MoveTarget.Selected,
        "RANGE_RANDOM_OPPONENT" => MoveTarget.RandomFoe,
        "RANGE_ADJACENT_OPPONENTS" => MoveTarget.AllFoes,
        "RANGE_ALL_ADJACENT" => MoveTarget.AllOthers,
        "RANGE_USER" => MoveTarget.User,
        "RANGE_USER_SIDE" => MoveTarget.UserSide,
        "RANGE_FIELD" => MoveTarget.Field,
        "RANGE_OPPONENT_SIDE" => MoveTarget.FoeSide,
        "RANGE_ALLY" => MoveTarget.Ally,
        "RANGE_USER_OR_ALLY" => MoveTarget.UserOrAlly,
        _ => throw new InvalidDataException($"Unknown move range {range}")
    };

    private static MoveTarget LaterTarget(int id) => id switch
    {
        1 or 2 or 10 => MoveTarget.Selected,
        3 => MoveTarget.Ally,
        4 => MoveTarget.UserSide,
        5 => MoveTarget.UserOrAlly,
        6 => MoveTarget.FoeSide,
        7 or 16 => MoveTarget.User,
        8 => MoveTarget.RandomFoe,
        9 => MoveTarget.AllOthers,
        11 => MoveTarget.AllFoes,
        12 => MoveTarget.Field,
        13 => MoveTarget.UserAndAllies,
        14 => MoveTarget.AllPokemon,
        15 => MoveTarget.Allies,
        _ => throw new InvalidDataException($"Unknown PokeAPI move target {id}")
    };

    // ================================================================== abilities

    public List<AbilityDatabase.AbilityRecord> Abilities() =>
        api.Abilities.Values.Where(r => r.Bool("is_main_series") && r.Int("id") < FirstAlternateForm).OrderBy(r => r.Int("id"))
            .Select(r => new AbilityDatabase.AbilityRecord
            {
                Id = r.Int("id"),
                Name = api.AbilityName(r.Int("id")),
                Generation = r.Int("generation_id"),
                Description = api.Prose("ability_prose", "ability_id", r.Int("id"))
            }).DistinctBy(a => a.Name).ToList(); // As One has an id for each of Calyrex's forms

    // ================================================================== species

    public List<PokemonSpecies> Species()
    {
        var list = new List<PokemonSpecies>();
        for (int dex = 1; dex <= lastSpecies; dex++)
            list.Add(dex <= LastPlatinumSpecies ? PlatinumSpecies(dex) : LaterSpecies(dex));

        // Platinum species that gained evolutions in later games (Eevee → Sylveon, Scyther → Kleavor)
        foreach (var s in list.Where(s => s.DexNumber <= LastPlatinumSpecies))
        {
            var later = LaterEvolutionsOf(s.DexNumber).Where(e => PokemonDexOf(e.TargetSpecies) > LastPlatinumSpecies).ToList();
            if (later.Count == 0) continue;
            var evolutions = s.Evolutions ?? new();

            // The first evolution whose condition holds is the one that happens, so one that asks for friendship
            // and something more (Sylveon) goes before the ones that ask for friendship alone (Espeon, Umbreon)
            int friendship = evolutions.FindIndex(e => e.Method is EvolutionMethod.Friendship or EvolutionMethod.FriendshipDay or EvolutionMethod.FriendshipNight);
            if (friendship >= 0) evolutions.InsertRange(friendship, later.Where(e => e.NeedsFriendship));
            else evolutions.AddRange(later.Where(e => e.NeedsFriendship));
            evolutions.AddRange(later.Where(e => !e.NeedsFriendship));
            s.Evolutions = evolutions;
        }
        foreach (var s in list)
        {
            if (s.HiddenAbility != null && s.Abilities.Contains(s.HiddenAbility)) s.HiddenAbility = null;
            s.DexEntry = DexEntry(s, list);
        }

        // Plan 03 · D11: the newest games' values beside Platinum's, every form, and the evolutions that start from
        // a form or lead to one (after the Pokédex text, which tells of the species' own evolutions)
        AddModernValues(list);
        AddForms(list);
        AddFormEvolutions(list);

        // Platinum's regional numbers (its table's first entry is a placeholder, not number 0)
        var sinnoh = decomp.SinnohPokedex();
        for (int number = 1; number < sinnoh.Count; number++)
        {
            int dex = speciesConstants.IndexOf(sinnoh[number]);
            if (dex < 1 || dex > list.Count) throw new InvalidDataException($"Sinnoh Pokédex entry {number} is {sinnoh[number]}, which isn't a species");
            list[dex - 1].SinnohNumber = number;
        }
        return list;
    }

    private int PokemonDexOf(string name)
    {
        dexByName ??= Enumerable.Range(1, lastSpecies).ToDictionary(api.SpeciesName, i => i);
        return dexByName.GetValueOrDefault(name);
    }

    private Dictionary<string, int>? dexByName;

    private PokemonSpecies PlatinumSpecies(int dex)
    {
        var d = decomp.Species(speciesConstants[dex]);
        var s = Common(dex);

        var types = d.GetProperty("types").EnumerateArray().Select(t => Gen4Type(t.GetString()!)).ToList();
        s.PrimaryType = types[0];
        s.SecondaryType = types[1] == types[0] ? null : types[1];

        var stats = d.GetProperty("base_stats");
        s.BaseHP = stats.GetProperty("hp").GetInt32();
        s.BaseAttack = stats.GetProperty("attack").GetInt32();
        s.BaseDefense = stats.GetProperty("defense").GetInt32();
        s.BaseSpAttack = stats.GetProperty("special_attack").GetInt32();
        s.BaseSpDefense = stats.GetProperty("special_defense").GetInt32();
        s.BaseSpeed = stats.GetProperty("speed").GetInt32();

        var ev = d.GetProperty("ev_yields");
        s.EvYield = Spread(ev.GetProperty("hp").GetInt32(), ev.GetProperty("attack").GetInt32(), ev.GetProperty("defense").GetInt32(),
            ev.GetProperty("special_attack").GetInt32(), ev.GetProperty("special_defense").GetInt32(), ev.GetProperty("speed").GetInt32());

        s.CatchRate = d.GetProperty("catch_rate").GetInt32();
        s.BaseExpYield = d.GetProperty("base_exp_reward").GetInt32();
        s.GrowthRate = Enum.Parse<GrowthRate>(Names.Pascal(d.GetProperty("exp_rate").GetString()!, "EXP_RATE_"));
        s.GenderRatio = d.GetProperty("gender_ratio").GetString() switch
        {
            "GENDER_RATIO_MALE_ONLY" => 0,
            "GENDER_RATIO_FEMALE_12_5" => 1,
            "GENDER_RATIO_FEMALE_25" => 2,
            "GENDER_RATIO_FEMALE_50" => 4,
            "GENDER_RATIO_FEMALE_75" => 6,
            "GENDER_RATIO_FEMALE_ONLY" => 8,
            "GENDER_RATIO_NO_GENDER" => -1,
            var g => throw new InvalidDataException($"Unknown gender ratio {g}")
        };
        s.HatchCycles = d.GetProperty("hatch_cycles").GetInt32();
        s.BaseFriendship = d.GetProperty("base_friendship").GetInt32();
        s.SafariFleeRate = d.GetProperty("safari_flee_rate").GetInt32();
        // What it may hold in the wild (plan 06 · R13; Pokemon_GiveHeldItem reads the two)
        var held = d.GetProperty("held_items");
        string? Held(string which) => held.GetProperty(which).GetString() is { } item && item != "ITEM_NONE" ? ItemName(item) : null;
        string? commonItem = Held("common"), rareItem = Held("rare");
        if (commonItem != null || rareItem != null) s.WildItems = new WildItems { Common = commonItem, Rare = rareItem };
        if (d.TryGetProperty("catching_show", out var show))
        {
            int Area(string key, string prefix) =>
                show.GetProperty(key).GetString() switch { var a when a!.EndsWith("NORTH_WEST") => 1, var a when a.EndsWith("NORTH_EAST") => 2, var a when a.EndsWith("SOUTH_WEST") => 3, var a when a.EndsWith("SOUTH_EAST") => 4, _ => 0 };
            s.PalPark = new PalParkData
            {
                LandArea = Area("pal_park_land_area", "PAL_PARK_AREA_LAND_"),
                WaterArea = Area("pal_park_water_area", "PAL_PARK_AREA_WATER_"),
                Rarity = show.GetProperty("rarity").GetInt32(),
                CatchingPoints = show.GetProperty("catching_points").GetInt32()
            };
        }

        s.Abilities = d.GetProperty("abilities").EnumerateArray().Select(a => a.GetString()!)
            .Where(a => a != "ABILITY_NONE").Select(a => api.AbilityName(abilityConstants.IndexOf(a))).Distinct().ToList();

        s.Learnset = d.GetProperty("learnset").GetProperty("by_level").EnumerateArray()
            .Select(e => new LearnableMove { Level = e[0].GetInt32(), MoveName = api.MoveName(moveConstants.IndexOf(e[1].GetString()!)) }).ToList();
        // The machines and tutors it learns from (plan 06 · R11): the TMs and HMs by name, the tutors' moves
        var machines = d.GetProperty("learnset").GetProperty("by_tm").EnumerateArray().Select(t => t.GetString()!).ToList();
        s.TmMoves = machines.Count > 0 ? machines : null;
        var tutored = d.GetProperty("learnset").GetProperty("by_tutor").EnumerateArray()
            .Select(m => api.MoveName(moveConstants.IndexOf(m.GetString()!))).ToList();
        s.TutorMoves = tutored.Count > 0 ? tutored : null;
        // What an Egg is born knowing from its father, and what species an Egg of it is (plan 06 · R15)
        var eggMoves = d.GetProperty("learnset").TryGetProperty("egg_moves", out var egg)
            ? egg.EnumerateArray().Select(m => api.MoveName(moveConstants.IndexOf(m.GetString()!))).ToList() : new();
        s.EggMoves = eggMoves.Count > 0 ? eggMoves : null;
        if (d.TryGetProperty("offspring", out var offspring) && speciesConstants.IndexOf(offspring.GetString()!) is var child && child > 0 && child != dex)
            s.Offspring = api.SpeciesName(child);

        var evolutions = d.GetProperty("evolutions").EnumerateArray().Select(PlatinumEvolution).ToList();
        s.Evolutions = evolutions.Count > 0 ? evolutions : null;
        return s;
    }

    private EvolutionData PlatinumEvolution(JsonElement e)
    {
        var parts = e.EnumerateArray().ToList();
        string method = parts[0].GetString()!["EVO_".Length..];
        var param = parts.Count == 3 ? parts[1] : (JsonElement?)null;
        var evo = new EvolutionData { TargetSpecies = api.SpeciesName(speciesConstants.IndexOf(parts[^1].GetString()!)) };
        int level = param is { ValueKind: JsonValueKind.Number } n ? n.GetInt32() : 0;
        string? text = param is { ValueKind: JsonValueKind.String } t ? t.GetString() : null;

        switch (method)
        {
            case "LEVEL": evo.Level = level; break;
            case "LEVEL_HAPPINESS": evo.Method = EvolutionMethod.Friendship; break;
            case "LEVEL_HAPPINESS_DAY": evo.Method = EvolutionMethod.FriendshipDay; break;
            case "LEVEL_HAPPINESS_NIGHT": evo.Method = EvolutionMethod.FriendshipNight; break;
            case "TRADE": evo.Method = EvolutionMethod.Trade; break;
            case "TRADE_WITH_HELD_ITEM": evo.Method = EvolutionMethod.TradeHoldingItem; evo.Item = ItemName(text!); break;
            case "USE_ITEM": evo.Method = EvolutionMethod.UseItem; evo.Item = ItemName(text!); break;
            case "USE_ITEM_MALE": evo.Method = EvolutionMethod.UseItemMale; evo.Item = ItemName(text!); break;
            case "USE_ITEM_FEMALE": evo.Method = EvolutionMethod.UseItemFemale; evo.Item = ItemName(text!); break;
            case "LEVEL_ATK_GT_DEF": evo.Method = EvolutionMethod.LevelAttackHigher; evo.Level = level; break;
            case "LEVEL_ATK_EQ_DEF": evo.Method = EvolutionMethod.LevelAttackEqual; evo.Level = level; break;
            case "LEVEL_ATK_LT_DEF": evo.Method = EvolutionMethod.LevelDefenseHigher; evo.Level = level; break;
            case "LEVEL_PID_LOW": evo.Method = EvolutionMethod.LevelPersonalityLow; evo.Level = level; break;
            case "LEVEL_PID_HIGH": evo.Method = EvolutionMethod.LevelPersonalityHigh; evo.Level = level; break;
            case "LEVEL_NINJASK": evo.Method = EvolutionMethod.LevelNinjask; evo.Level = level; break;
            case "LEVEL_SHEDINJA": evo.Method = EvolutionMethod.LevelShedinja; evo.Level = level; break;
            case "LEVEL_BEAUTY": evo.Method = EvolutionMethod.Beauty; evo.Value = level; break;
            case "LEVEL_WITH_HELD_ITEM_DAY": evo.Method = EvolutionMethod.LevelHoldingItemDay; evo.Item = ItemName(text!); break;
            case "LEVEL_WITH_HELD_ITEM_NIGHT": evo.Method = EvolutionMethod.LevelHoldingItemNight; evo.Item = ItemName(text!); break;
            case "LEVEL_KNOW_MOVE": evo.Method = EvolutionMethod.LevelKnowsMove; evo.Move = api.MoveName(moveConstants.IndexOf(text!)); break;
            case "LEVEL_SPECIES_IN_PARTY":
                evo.Method = EvolutionMethod.LevelWithSpeciesInParty;
                evo.Species = api.SpeciesName(speciesConstants.IndexOf(text!));
                break;
            case "LEVEL_MALE": evo.Method = EvolutionMethod.LevelMale; evo.Level = level; break;
            case "LEVEL_FEMALE": evo.Method = EvolutionMethod.LevelFemale; evo.Level = level; break;
            case "LEVEL_MAGNETIC_FIELD": evo.Method = EvolutionMethod.LevelAtLocation; evo.Location = "Magnetic Field"; break;
            case "LEVEL_MOSS_ROCK": evo.Method = EvolutionMethod.LevelAtLocation; evo.Location = "Moss Rock"; break;
            case "LEVEL_ICE_ROCK": evo.Method = EvolutionMethod.LevelAtLocation; evo.Location = "Ice Rock"; break;
            default: throw new InvalidDataException($"Unknown evolution method {method}");
        }
        return evo;
    }

    /// <summary>
    /// What a later species may hold in the wild (plan 06 · R13), from the newest game PokeAPI gives items for: an
    /// item held half the time or more is its common one, one held less often its rare one, and one always held both.
    /// </summary>
    private WildItems? LaterWildItems(int pokemonId)
    {
        var rows = api.Table("pokemon_items").Where(r => r.Int("pokemon_id") == pokemonId).ToList();
        if (rows.Count == 0) return null;
        int newest = rows.Max(r => r.Int("version_id"));
        string? common = null, rare = null;
        foreach (var r in rows.Where(r => r.Int("version_id") == newest))
        {
            string item = api.ItemName(r.Int("item_id"));
            int rarity = r.Int("rarity");
            if (rarity >= 100) common = rare = item;
            else if (rarity >= 50) common ??= item;
            else rare ??= item;
        }
        return common == null && rare == null ? null : new WildItems { Common = common, Rare = rare };
    }

    private PokemonSpecies LaterSpecies(int dex)
    {
        var s = Common(dex);
        var p = api.DefaultPokemon[dex];
        int pid = p.Int("id");
        var row = api.Species[dex];

        var types = api.Table("pokemon_types").Where(r => r.Int("pokemon_id") == pid).OrderBy(r => r.Int("slot")).Select(r => api.Type(r.Int("type_id"))).ToList();
        s.PrimaryType = types[0];
        s.SecondaryType = types.Count > 1 ? types[1] : null;

        var stats = Stats(pid);
        s.BaseHP = stats[1].Base;
        s.BaseAttack = stats[2].Base;
        s.BaseDefense = stats[3].Base;
        s.BaseSpAttack = stats[4].Base;
        s.BaseSpDefense = stats[5].Base;
        s.BaseSpeed = stats[6].Base;
        s.EvYield = Spread(stats[1].Effort, stats[2].Effort, stats[3].Effort, stats[4].Effort, stats[5].Effort, stats[6].Effort);

        s.CatchRate = row.Int("capture_rate");
        s.BaseExpYield = p.IntOrNull("base_experience") ?? 0;
        s.GrowthRate = api.Growth(row.Int("growth_rate_id"));
        s.GenderRatio = row.Int("gender_rate");
        s.HatchCycles = row.IntOrNull("hatch_counter") ?? 0;
        s.BaseFriendship = row.IntOrNull("base_happiness") ?? 0;

        s.Abilities = AbilitiesOf(pid).Where(a => !a.Hidden).Select(a => a.Name).Distinct().ToList();
        s.WildItems = LaterWildItems(pid);
        s.Learnset = LaterLearnset(pid);
        s.EggMoves = LaterEggMoves(pid);
        // An Egg of it is the first of its line (plan 06 · R15)
        int first = dex;
        while (api.Species[first].IntOrNull("evolves_from_species_id") is { } from && from >= 1) first = from;
        if (first != dex) s.Offspring = api.SpeciesName(first);
        var evolutions = LaterEvolutionsOf(dex);
        s.Evolutions = evolutions.Count > 0 ? evolutions : null;
        return s;
    }

    /// <summary>What PokeAPI says about any species: name, category, generation, size, egg groups, looks, hidden ability.</summary>
    private PokemonSpecies Common(int dex)
    {
        var row = api.Species[dex];
        var p = api.DefaultPokemon[dex];
        string genus = api.Genus(dex);
        var s = new PokemonSpecies
        {
            DexNumber = dex,
            Name = api.SpeciesName(dex),
            Category = genus.EndsWith(" Pokémon") ? genus[..^" Pokémon".Length] : genus,
            Generation = row.Int("generation_id"),
            Legendary = row.Bool("is_legendary"),
            Mythical = row.Bool("is_mythical"),
            Height = p.Int("height") / 10f,
            Weight = p.Int("weight") / 10f,
            Color = api.Color(row.Int("color_id")),
            Shape = row["shape_id"] == "" ? null : api.Shape(row.Int("shape_id")),
            EggGroups = api.Table("pokemon_egg_groups").Where(r => r.Int("species_id") == dex).Select(r => api.EggGroup(r.Int("egg_group_id"))).ToList(),
        };
        s.HiddenAbility = AbilitiesOf(p.Int("id")).Where(a => a.Hidden).Select(a => a.Name).FirstOrDefault();
        return s;
    }

    private IEnumerable<(string Name, bool Hidden)> AbilitiesOf(int pokemonId)
    {
        abilitiesByPokemon ??= api.Table("pokemon_abilities").GroupBy(r => r.Int("pokemon_id"))
            .ToDictionary(g => g.Key, g => g.OrderBy(r => r.Int("slot")).Select(r => (api.AbilityName(r.Int("ability_id")), r.Bool("is_hidden"))).ToList());
        return abilitiesByPokemon.GetValueOrDefault(pokemonId) ?? new();
    }

    private Dictionary<int, List<(string, bool)>>? abilitiesByPokemon;

    private Dictionary<int, (int Base, int Effort)> Stats(int pokemonId)
    {
        statsByPokemon ??= api.Table("pokemon_stats").GroupBy(r => r.Int("pokemon_id"))
            .ToDictionary(g => g.Key, g => g.ToDictionary(r => r.Int("stat_id"), r => (r.Int("base_stat"), r.Int("effort"))));
        return statsByPokemon[pokemonId];
    }

    private Dictionary<int, Dictionary<int, (int, int)>>? statsByPokemon;

    private static StatSpread? Spread(int hp, int atk, int def, int spa, int spd, int spe) =>
        hp + atk + def + spa + spd + spe == 0 ? null
            : new StatSpread { HP = hp, Attack = atk, Defense = def, SpAttack = spa, SpDefense = spd, Speed = spe };

    private List<LearnableMove> LaterLearnset(int pokemonId)
    {
        levelUp ??= api.Table("pokemon_moves").Where(r => r.Int("pokemon_move_method_id") == 1)
            .GroupBy(r => r.Int("pokemon_id")).ToDictionary(g => g.Key, g => g.ToList());
        var rows = levelUp.GetValueOrDefault(pokemonId) ?? new();
        foreach (int vg in LearnsetVersionGroups)
        {
            var learned = rows.Where(r => r.Int("version_group_id") == vg).ToList();
            if (learned.Count == 0) continue;
            return learned.OrderBy(r => r.Int("level")).ThenBy(r => r.IntOrNull("order") ?? 0)
                .Select(r => new LearnableMove { Level = r.Int("level"), MoveName = api.MoveName(r.Int("move_id")) })
                .DistinctBy(l => (l.Level, l.MoveName)).ToList();
        }
        return new();
    }

    private Dictionary<int, List<CsvRow>>? levelUp;

    /// <summary>A later species' egg moves (plan 06 · R15), from the newest game PokeAPI gives them for, in its order.</summary>
    private List<string>? LaterEggMoves(int pokemonId)
    {
        eggMoveRows ??= api.Table("pokemon_moves").Where(r => r.Int("pokemon_move_method_id") == 2)
            .GroupBy(r => r.Int("pokemon_id")).ToDictionary(g => g.Key, g => g.ToList());
        var rows = eggMoveRows.GetValueOrDefault(pokemonId) ?? new();
        foreach (int vg in LearnsetVersionGroups)
        {
            var learned = rows.Where(r => r.Int("version_group_id") == vg).ToList();
            if (learned.Count == 0) continue;
            return learned.OrderBy(r => r.IntOrNull("order") ?? 0).ThenBy(r => r.Int("move_id"))
                .Select(r => api.MoveName(r.Int("move_id"))).Distinct().ToList();
        }
        return null;
    }

    private Dictionary<int, List<CsvRow>>? eggMoveRows;

    /// <summary>
    /// The TMs, HMs and tutor moves of the species after Platinum (plan 06 · R11): each Platinum machine whose move
    /// the species learns by a machine in any of its own games, and each of Platinum's tutor moves it learns from a
    /// tutor in them. Platinum's own species keep the decompilation's lists.
    /// </summary>
    public void AddLaterMachines(List<PokemonSpecies> species, List<ItemData> items)
    {
        var machineOf = items.Where(i => i.TeachesMove != null && i.Id < 1000 && i.Pocket == ItemPocket.TMsAndHMs)
            .OrderBy(i => i.Id).GroupBy(i => i.TeachesMove!).ToDictionary(g => g.Key, g => g.First());
        var tutorMoves = species.Where(s => s.TutorMoves != null).SelectMany(s => s.TutorMoves!).ToHashSet();
        var learned = api.Table("pokemon_moves").Where(r => r.Int("pokemon_move_method_id") is 3 or 4)
            .GroupBy(r => r.Int("pokemon_id")).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var s in species.Where(s => s.Generation > 4))
        {
            var rows = learned.GetValueOrDefault(s.DexNumber) ?? new();
            var byMachine = rows.Where(r => r.Int("pokemon_move_method_id") == 4).Select(r => api.MoveName(r.Int("move_id"))).ToHashSet();
            var byTutor = rows.Where(r => r.Int("pokemon_move_method_id") == 3).Select(r => api.MoveName(r.Int("move_id"))).ToHashSet();
            var machines = machineOf.Where(m => byMachine.Contains(m.Key)).Select(m => m.Value).OrderBy(i => i.Id).Select(i => i.Name).ToList();
            s.TmMoves = machines.Count > 0 ? machines : null;
            var tutored = byTutor.Where(tutorMoves.Contains).OrderBy(m => m, StringComparer.Ordinal).ToList();
            s.TutorMoves = tutored.Count > 0 ? tutored : null;
        }
    }

    /// <summary>
    /// The Poké Marts' stock (plan 06 · R11), from the decompilation's lists (<c>include/data/mart_items.h</c>): the
    /// common counter's items with the badges each needs, and every specialty counter's, by the original's id
    /// without its prefix (<c>jubilife</c>, <c>eterna_house</c>, <c>veilstone_1f_right</c>). Item names only.
    /// </summary>
    public MartData Marts()
    {
        string header = decomp.Include("data", "mart_items.h");
        var marts = new MartData();
        string common = System.Text.RegularExpressions.Regex.Match(header, @"PokeMartCommonItems\[\] = \{(.*?)\};", System.Text.RegularExpressions.RegexOptions.Singleline).Groups[1].Value;
        foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(common, @"\{ (ITEM_\w+), 0x([0-9A-Fa-f]+) \}"))
            marts.Common.Add(new MartItem { Item = ItemName(m.Groups[1].Value), Badges = Convert.ToInt32(m.Groups[2].Value, 16) });
        var arrays = System.Text.RegularExpressions.Regex.Matches(header, @"const u16 (\w+)\[\] = \{(.*?)\};", System.Text.RegularExpressions.RegexOptions.Singleline)
            .ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);
        foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(header, @"\[MART_SPECIALTIES_ID_(\w+)\] = (\w+)"))
        {
            string key = m.Groups[1].Value.ToLowerInvariant();
            var stock = System.Text.RegularExpressions.Regex.Matches(arrays[m.Groups[2].Value], @"\bITEM_\w+").Select(i => ItemName(i.Value)).ToList();
            marts.Specialties[key] = stock;
        }
        return marts;
    }

    /// <summary>
    /// A species' evolutions from PokeAPI (the default entry of each), leaving out regional-form ones. Rows that
    /// differ only in the form they lead to (Dudunsparce's two or three segments, Hisuian Decidueye's later level)
    /// are one evolution here.
    /// </summary>
    private List<EvolutionData> LaterEvolutionsOf(int dex)
    {
        var targets = api.Species.Values.Where(r => r.IntOrNull("evolves_from_species_id") == dex && r.Int("id") <= lastSpecies)
            .Select(r => r.Int("id")).ToHashSet();
        return api.Table("pokemon_evolution")
            .Where(r => targets.Contains(r.Int("evolved_species_id")) && r.Bool("is_default"))
            .Where(r => (r.IntOrNull("required_pokemon_form_id") ?? 0) < FirstAlternateForm)
            .OrderBy(r => r.Int("evolved_species_id")).ThenBy(r => r.Int("id"))
            .Select(r => LaterEvolution(r, dex))
            .DistinctBy(e => (e.Method, e.TargetSpecies, e.Item, e.Move, e.Type, e.Species, e.Location))
            .ToList();
    }

    private string ItemNamed(string identifier) => api.ItemName(api.Items.Values.First(i => i["identifier"] == identifier).Int("id"));

    /// <summary>
    /// One PokeAPI row as an evolution of this game. Where the original leans on something this game lacks, the
    /// method is the stand-in docs/mechanics/rulings.md settles on: playing next to another player (Finizen) and
    /// levelling up inside a battle (Tandemaus) are plain levels, steps taken in the Let's Go mode are steps at
    /// the head of the party, Legends: Arceus's agile and strong style moves are plain uses of the move, and
    /// walking under the Dusty Bowl's stone arch (Galarian Yamask) is levelling up where a map has a Stone Arch.
    /// </summary>
    private EvolutionData LaterEvolution(CsvRow r, int fromDex)
    {
        var evo = new EvolutionData
        {
            TargetSpecies = api.SpeciesName(r.Int("evolved_species_id")),
            Level = r.IntOrNull("minimum_level") ?? 0
        };
        string? item = r["trigger_item_id"] == "" ? null : api.ItemName(r.Int("trigger_item_id"));
        string? held = r["held_item_id"] == "" ? null : api.ItemName(r.Int("held_item_id"));
        string time = r["time_of_day"];
        var notes = new List<string>();

        void AfterMoveUses()
        {
            evo.Method = EvolutionMethod.LevelAfterMoveUses;
            evo.Move = api.MoveName(r.Int("used_move_id"));
            evo.Value = r.IntOrNull("minimum_move_count") ?? 1;
        }

        switch (r.Int("evolution_trigger_id"))
        {
            case 1: // level up
                if (r.IntOrNull("known_move_type_id") is { } moveType)
                {
                    evo.Method = EvolutionMethod.LevelKnowsMoveType;
                    evo.Type = api.Type(moveType);
                    evo.NeedsFriendship = r.IntOrNull("minimum_happiness") != null || r.IntOrNull("minimum_affection") != null;
                }
                else if (r.IntOrNull("minimum_happiness") != null)
                    evo.Method = time == "day" ? EvolutionMethod.FriendshipDay : time == "night" ? EvolutionMethod.FriendshipNight : EvolutionMethod.Friendship;
                else if (r.IntOrNull("minimum_affection") is { } affection) { evo.Method = EvolutionMethod.Affection; evo.Value = affection; }
                else if (r.IntOrNull("minimum_beauty") is { } beauty) { evo.Method = EvolutionMethod.Beauty; evo.Value = beauty; }
                else if (held != null)
                {
                    evo.Method = time == "day" ? EvolutionMethod.LevelHoldingItemDay : time == "night" ? EvolutionMethod.LevelHoldingItemNight : EvolutionMethod.LevelHoldingItem;
                    evo.Item = held;
                }
                else if (r.IntOrNull("known_move_id") is { } move) { evo.Method = EvolutionMethod.LevelKnowsMove; evo.Move = api.MoveName(move); }
                else if (r.IntOrNull("party_species_id") is { } species) { evo.Method = EvolutionMethod.LevelWithSpeciesInParty; evo.Species = api.SpeciesName(species); }
                else if (r.IntOrNull("party_type_id") is { } type) { evo.Method = EvolutionMethod.LevelWithTypeInParty; evo.Type = api.Type(type); }
                else if (r.IntOrNull("location_id") is { } location) { evo.Method = EvolutionMethod.LevelAtLocation; evo.Location = api.LocationName(location); }
                else if (r.Bool("needs_overworld_rain")) evo.Method = EvolutionMethod.LevelInRain;
                else if (r.Bool("turn_upside_down")) evo.Method = EvolutionMethod.LevelUpsideDown;
                else if (r.IntOrNull("relative_physical_stats") is { } rel)
                    evo.Method = rel > 0 ? EvolutionMethod.LevelAttackHigher : rel < 0 ? EvolutionMethod.LevelDefenseHigher : EvolutionMethod.LevelAttackEqual;
                else if (r.IntOrNull("gender_id") is { } gender) evo.Method = gender == 1 ? EvolutionMethod.LevelFemale : EvolutionMethod.LevelMale;
                else if (time is "day" or "night") evo.Method = time == "day" ? EvolutionMethod.LevelDay : EvolutionMethod.LevelNight;
                else if (time == "dusk") evo.Method = EvolutionMethod.LevelDusk;
                else if (time != "") { evo.Method = EvolutionMethod.Other; notes.Add($"level up at {time}"); }

                if (r.IntOrNull("minimum_steps") is { } steps) { evo.Method = EvolutionMethod.LevelAfterSteps; evo.Value = steps; }
                if (r.IntOrNull("used_move_id") != null) AfterMoveUses();
                if (r.IntOrNull("minimum_damage_taken") is { } damage) { evo.Method = EvolutionMethod.LevelAfterRecoil; evo.Value = damage; }
                break;
            case 2:
                if (held != null) { evo.Method = EvolutionMethod.TradeHoldingItem; evo.Item = held; }
                else if (r.IntOrNull("trade_species_id") is { } other) { evo.Method = EvolutionMethod.TradeWithSpecies; evo.Species = api.SpeciesName(other); }
                else evo.Method = EvolutionMethod.Trade;
                break;
            case 3:
                evo.Method = r.IntOrNull("gender_id") switch { 1 => EvolutionMethod.UseItemFemale, 2 => EvolutionMethod.UseItemMale, _ => EvolutionMethod.UseItem };
                // Ursaring's Peat Block works under a full moon: at night here
                if (time is "night" or "full-moon") evo.Method = EvolutionMethod.UseItemNight;
                evo.Item = item;
                break;
            case 4:
                evo.Method = EvolutionMethod.LevelShedinja;
                break;
            case 8: // three critical hits in one battle
                evo.Method = EvolutionMethod.CriticalHits;
                evo.Value = 3;
                break;
            case 9: // HP lost to moves without fainting, then the stone arch
                evo.Method = EvolutionMethod.LevelAfterDamage;
                evo.Value = r.IntOrNull("minimum_damage_taken") ?? 49;
                evo.Location = "Stone Arch";
                break;
            case 13: // HP lost to its own recoil without fainting; the gender only picks Basculegion's form
                evo.Method = EvolutionMethod.LevelAfterRecoil;
                evo.Value = r.IntOrNull("minimum_damage_taken") ?? 294;
                break;
            case 5: // spinning round while it holds a sweet; which way and for how long only picks Alcremie's form
                evo.Method = EvolutionMethod.SpinHoldingItem;
                evo.Item = held;
                break;
            case 10: // levelling up in a battle: a plain level
                break;
            case 11 or 12 or 14 when r.IntOrNull("used_move_id") != null: // a move used some number of times
                AfterMoveUses();
                break;
            case 15: // three of its own kind knocked out
                evo.Method = EvolutionMethod.LevelAfterDefeating;
                evo.Species = api.SpeciesName(fromDex);
                evo.Value = 3;
                break;
            case 16:
                evo.Method = EvolutionMethod.LevelWithItemsInBag;
                evo.Item = ItemNamed("gimmighoul-coin");
                evo.Value = 999;
                break;
            case 17: // Pokémon GO's 400 candies, the only way there has ever been
                evo.Method = EvolutionMethod.LevelWithItemsInBag;
                evo.Item = ItemNamed("meltan-candy");
                evo.Value = 400;
                break;
            default:
                evo.Method = EvolutionMethod.Other;
                string trigger = api.Table("evolution_triggers").First(t => t.Int("id") == r.Int("evolution_trigger_id"))["identifier"];
                notes.Insert(0, Names.Title(trigger).ToLowerInvariant());
                if (item != null) notes.Add($"with {item}");
                break;
        }
        if (notes.Count > 0) evo.Note = string.Join(", ", notes);
        return evo;
    }

    // ================================================================== forms (plan 03 · D11)

    /// <summary>The prefixes of PokeAPI's identifiers for the forms of other regions (a Pikachu's cap aside).</summary>
    private static readonly string[] RegionalForms = { "alola", "galar", "hisui", "paldea" };

    /// <summary>Platinum's forms with data of their own in the decompilation: PokeAPI's identifier, and the folders it is in.</summary>
    private static readonly Dictionary<string, (string Species, string Form)> PlatinumForms = new()
    {
        ["deoxys-attack"] = ("deoxys", "attack"), ["deoxys-defense"] = ("deoxys", "defense"), ["deoxys-speed"] = ("deoxys", "speed"),
        ["wormadam-sandy"] = ("wormadam", "sandy"), ["wormadam-trash"] = ("wormadam", "trash"),
        ["giratina-origin"] = ("giratina", "origin"), ["shaymin-sky"] = ("shaymin", "sky"),
        ["rotom-heat"] = ("rotom", "heat"), ["rotom-wash"] = ("rotom", "wash"), ["rotom-frost"] = ("rotom", "frost"),
        ["rotom-fan"] = ("rotom", "fan"), ["rotom-mow"] = ("rotom", "mow")
    };

    /// <summary>What a species or a form is, in the fields a form may differ in.</summary>
    private sealed record Look(List<PokemonType> Types, StatSpread BaseStats, List<string> Abilities, string? HiddenAbility, float Height, float Weight,
        int BaseExp, StatSpread? EvYield, int? CatchRate, List<LearnableMove> Learnset);

    /// <summary>A PokeAPI entry (<c>pokemon.csv</c>) as a form sees it, with the newest games' values.</summary>
    private Look PokeApiLook(CsvRow p)
    {
        int pid = p.Int("id");
        var stats = Stats(pid);
        var abilities = AbilitiesOf(pid).ToList();
        return new Look(TypesOf(pid), Spread6(stats, s => s.Base), abilities.Where(a => !a.Hidden).Select(a => a.Name).Distinct().ToList(),
            abilities.Where(a => a.Hidden).Select(a => a.Name).FirstOrDefault(), p.Int("height") / 10f, p.Int("weight") / 10f,
            p.IntOrNull("base_experience") ?? 0, Spread(stats[1].Effort, stats[2].Effort, stats[3].Effort, stats[4].Effort, stats[5].Effort, stats[6].Effort),
            null, LaterLearnset(pid));
    }

    /// <summary>Platinum's own data for a species or a form (the decompilation's <c>data.json</c>), size aside.</summary>
    private Look PlatinumLook(JsonElement d, float height, float weight)
    {
        var types = d.GetProperty("types").EnumerateArray().Select(t => Gen4Type(t.GetString()!)).Distinct().ToList();
        var b = d.GetProperty("base_stats");
        var ev = d.GetProperty("ev_yields");
        int Stat(JsonElement e, string name) => e.GetProperty(name).GetInt32();
        return new Look(types,
            new StatSpread { HP = Stat(b, "hp"), Attack = Stat(b, "attack"), Defense = Stat(b, "defense"), SpAttack = Stat(b, "special_attack"), SpDefense = Stat(b, "special_defense"), Speed = Stat(b, "speed") },
            d.GetProperty("abilities").EnumerateArray().Select(a => a.GetString()!).Where(a => a != "ABILITY_NONE")
                .Select(a => api.AbilityName(abilityConstants.IndexOf(a))).Distinct().ToList(),
            null, height, weight, d.GetProperty("base_exp_reward").GetInt32(),
            Spread(Stat(ev, "hp"), Stat(ev, "attack"), Stat(ev, "defense"), Stat(ev, "special_attack"), Stat(ev, "special_defense"), Stat(ev, "speed")),
            d.GetProperty("catch_rate").GetInt32(),
            d.GetProperty("learnset").GetProperty("by_level").EnumerateArray()
                .Select(e => new LearnableMove { Level = e[0].GetInt32(), MoveName = api.MoveName(moveConstants.IndexOf(e[1].GetString()!)) }).ToList());
    }

    private List<PokemonType> TypesOf(int pokemonId) =>
        api.Table("pokemon_types").Where(r => r.Int("pokemon_id") == pokemonId).OrderBy(r => r.Int("slot")).Select(r => api.Type(r.Int("type_id"))).ToList();

    private static StatSpread Spread6(Dictionary<int, (int Base, int Effort)> stats, Func<(int Base, int Effort), int> pick) => new()
    {
        HP = pick(stats[1]), Attack = pick(stats[2]), Defense = pick(stats[3]), SpAttack = pick(stats[4]), SpDefense = pick(stats[5]), Speed = pick(stats[6])
    };

    private static bool Same(StatSpread? a, StatSpread? b) =>
        a?.HP == b?.HP && a?.Attack == b?.Attack && a?.Defense == b?.Defense && a?.SpAttack == b?.SpAttack && a?.SpDefense == b?.SpDefense && a?.Speed == b?.Speed;

    private static bool Same(List<LearnableMove> a, List<LearnableMove> b) =>
        a.Select(m => (m.Level, m.MoveName)).SequenceEqual(b.Select(m => (m.Level, m.MoveName)));

    /// <summary>What the newest games changed of a set of values: null when nothing.</summary>
    private static SpeciesValues? Changes(Look platinum, Look modern)
    {
        var values = new SpeciesValues
        {
            Types = modern.Types.SequenceEqual(platinum.Types) ? null : modern.Types,
            BaseStats = Same(modern.BaseStats, platinum.BaseStats) ? null : modern.BaseStats
        };
        return values.Types == null && values.BaseStats == null ? null : values;
    }

    /// <summary>The newest games' types and base stats of Platinum's species, where they differ from Platinum's own.</summary>
    private void AddModernValues(List<PokemonSpecies> list)
    {
        foreach (var s in list.Where(s => s.DexNumber <= LastPlatinumSpecies))
        {
            var platinum = new Look(s.SecondaryType is { } t ? new() { s.PrimaryType, t } : new() { s.PrimaryType },
                new StatSpread { HP = s.BaseHP, Attack = s.BaseAttack, Defense = s.BaseDefense, SpAttack = s.BaseSpAttack, SpDefense = s.BaseSpDefense, Speed = s.BaseSpeed },
                s.Abilities, s.HiddenAbility, s.Height, s.Weight, s.BaseExpYield, s.EvYield, s.CatchRate, s.Learnset);
            s.Modern = Changes(platinum, PokeApiLook(api.DefaultPokemon[s.DexNumber]));
        }
    }

    /// <summary>
    /// Each species' forms: every other entry PokeAPI has for it (its <c>pokemon_forms.csv</c>), totems aside, with
    /// only what differs from the species written down. A form's name is the species' and PokeAPI's form identifier
    /// in capitals, as Pokémon Showdown spells them and the Mega Stones already name them (<c>Charizard-Mega-X</c>,
    /// <c>Rotom-Heat</c>, <c>Meowth-Galar</c>). A form is compared with its species in the same source: Platinum's own
    /// forms (Deoxys, Wormadam, Giratina, Shaymin and Rotom) with the decompilation's species, keeping the newest
    /// games' changes beside them, and every other form with PokeAPI's, so that a Mega or a regional form of a
    /// Platinum species follows the rules the game is played by wherever it is the species' own.
    /// </summary>
    private void AddForms(List<PokemonSpecies> list)
    {
        var pokemon = api.Table("pokemon").ToDictionary(r => r.Int("id"));
        var formTypes = api.Table("pokemon_form_types").GroupBy(r => r.Int("pokemon_form_id"))
            .ToDictionary(g => g.Key, g => g.OrderBy(r => r.Int("slot")).Select(r => r.Int("type_id")).ToList());
        var formsOf = api.Table("pokemon_forms").GroupBy(f => pokemon[f.Int("pokemon_id")].Int("species_id")).ToDictionary(g => g.Key, g => g.OrderBy(f => f.Int("order")).ToList());

        foreach (var s in list)
        {
            var main = api.DefaultPokemon[s.DexNumber];
            var mainLook = PokeApiLook(main);
            var platinumData = s.DexNumber <= LastPlatinumSpecies ? decomp.Species(speciesConstants[s.DexNumber]) : (JsonElement?)null;
            var forms = new List<PokemonForm>();
            foreach (var f in formsOf.GetValueOrDefault(s.DexNumber) ?? new())
            {
                var p = pokemon[f.Int("pokemon_id")];
                bool ownEntry = !p.Bool("is_default");
                if (!ownEntry && f.Bool("is_default")) continue; // the species itself
                if (p["identifier"].Contains("-totem")) continue; // Alola's trial bosses, not a form anyone can have
                string suffix = FormSuffix(f, api.Species[s.DexNumber]["identifier"]);

                var form = new PokemonForm
                {
                    Name = s.Name + "-" + string.Join("-", suffix.Split('-').Select(w => char.ToUpperInvariant(w[0]) + w[1..])),
                    Kind = f.Bool("is_mega") || suffix.StartsWith("mega") ? FormKind.Mega
                        : suffix == "primal" ? FormKind.Primal
                        : suffix == "gmax" || suffix.EndsWith("-gmax") ? FormKind.Gigantamax
                        : f.Bool("is_battle_only") ? FormKind.Battle
                        : RegionalForms.Any(r => suffix == r || suffix.StartsWith(r + "-")) && !suffix.EndsWith("-cap") ? FormKind.Regional
                        : ownEntry ? FormKind.Alternate : FormKind.Look
                };

                // The types a form has of its own (Arceus's plates) are listed for the form rather than its entry
                List<PokemonType>? ownTypes = null;
                if (formTypes.TryGetValue(f.Int("id"), out var typeIds))
                {
                    try { ownTypes = typeIds.Select(api.Type).ToList(); }
                    catch (ArgumentException) { continue; } // the ??? type, which this game hasn't got
                }

                if (PlatinumForms.TryGetValue(p["identifier"], out var folders) && platinumData is { } data)
                {
                    var platinum = PlatinumLook(decomp.SpeciesForm(folders.Species, folders.Form), p.Int("height") / 10f, p.Int("weight") / 10f);
                    Differences(form, platinum, PlatinumLook(data, s.Height, s.Weight));
                    form.Modern = Changes(platinum, PokeApiLook(p));
                }
                else if (ownEntry) Differences(form, PokeApiLook(p), mainLook);
                if (ownTypes != null && !ownTypes.SequenceEqual(mainLook.Types)) form.Types = ownTypes;
                // A form held only in battle keeps the moves it had before
                if (form.Kind is FormKind.Mega or FormKind.Primal or FormKind.Gigantamax or FormKind.Battle) form.Learnset = null;
                // An entry of its own with nothing in it that the species hasn't (Pikachu's caps) is only a look
                if (form.Kind == FormKind.Alternate && form is { Types: null, BaseStats: null, Abilities: null, HiddenAbility: null, Height: null,
                        Weight: null, BaseExpYield: null, EvYield: null, CatchRate: null, Learnset: null, Modern: null })
                    form.Kind = FormKind.Look;
                forms.Add(form);
            }
            s.Forms = forms.Count > 0 ? forms.DistinctBy(f => f.Name).ToList() : null;
        }
    }

    /// <summary>
    /// What a form's name adds to its species' name: PokeAPI's identifier for it without the species' (Mega-X of
    /// <c>charizard-mega-x</c>), which tells apart forms that PokeAPI gives the same form identifier
    /// (Tatsugiri's three Megas are all <c>mega</c>).
    /// </summary>
    private static string FormSuffix(CsvRow form, string speciesIdentifier) =>
        form["identifier"].StartsWith(speciesIdentifier + "-") ? form["identifier"][(speciesIdentifier.Length + 1)..] : form["form_identifier"];

    /// <summary>Writes down what a form has that its species (in the same source) hasn't.</summary>
    private static void Differences(PokemonForm form, Look look, Look species)
    {
        if (!look.Types.SequenceEqual(species.Types)) form.Types = look.Types;
        if (!Same(look.BaseStats, species.BaseStats)) form.BaseStats = look.BaseStats;
        // None listed is PokeAPI not knowing them yet (the Megas of Legends: Z-A): the species' stand in
        if (look.Abilities.Count > 0 && !look.Abilities.SequenceEqual(species.Abilities)) form.Abilities = look.Abilities;
        if (look.HiddenAbility != null && look.HiddenAbility != species.HiddenAbility && !look.Abilities.Contains(look.HiddenAbility))
            form.HiddenAbility = look.HiddenAbility;
        if (look.Height != species.Height) form.Height = look.Height;
        if (look.Weight != species.Weight) form.Weight = look.Weight;
        if (look.BaseExp != species.BaseExp && look.BaseExp > 0) form.BaseExpYield = look.BaseExp;
        if (!Same(look.EvYield, species.EvYield)) form.EvYield = look.EvYield;
        if (look.CatchRate != null && look.CatchRate != species.CatchRate) form.CatchRate = look.CatchRate;
        if (look.Learnset.Count > 0 && !Same(look.Learnset, species.Learnset)) form.Learnset = look.Learnset;
    }

    /// <summary>
    /// The evolutions that start from a form or lead to one: from a regional form (Galarian Meowth into Perrserker,
    /// Alolan Vulpix into Alolan Ninetales), from one look to the same look (Burmy's sandy cloak makes a sandy
    /// Wormadam), into a form by the hour (Lycanroc's Midnight Form), and into a regional form only in its region
    /// (Pikachu into Alolan Raichu in Alola), which goes before the species' other evolutions. An evolution into
    /// another form by the same method as one into the species' own (Dudunsparce's three segments, Toxtricity's Low
    /// Key, which the games pick by chance or nature) stays the one into the species' own form.
    /// </summary>
    private void AddFormEvolutions(List<PokemonSpecies> list)
    {
        var bySpecies = list.ToDictionary(s => s.DexNumber);
        var forms = api.Table("pokemon_forms").ToDictionary(f => f.Int("id"));
        var pokemon = api.Table("pokemon").ToDictionary(r => r.Int("id"));
        var regions = api.Table("regions").ToDictionary(r => r.Int("id"), r => Names.Title(r["identifier"]));

        // A form row's name in the species' forms, or null for the species' own
        string? FormName(int? id)
        {
            if (id is not { } formId || !forms.TryGetValue(formId, out var f)) return null;
            var p = pokemon[f.Int("pokemon_id")];
            if (p.Bool("is_default") && f.Bool("is_default")) return null;
            var species = bySpecies.GetValueOrDefault(p.Int("species_id"));
            string suffix = FormSuffix(f, api.Species[p.Int("species_id")]["identifier"]);
            return species?.Forms?.FirstOrDefault(x => x.Name.Equals(species.Name + "-" + suffix, StringComparison.OrdinalIgnoreCase))?.Name;
        }

        foreach (var r in api.Table("pokemon_evolution").Where(r => r.Bool("is_default")).OrderBy(r => r.Int("evolved_species_id")).ThenBy(r => r.Int("id")))
        {
            int target = r.Int("evolved_species_id");
            if (target > lastSpecies || api.Species[target].IntOrNull("evolves_from_species_id") is not { } from || !bySpecies.TryGetValue(from, out var s)) continue;
            string? fromForm = FormName(r.IntOrNull("required_pokemon_form_id"));
            string? toForm = FormName(r.IntOrNull("evolved_pokemon_form_id"));
            string? region = r.IntOrNull("region_id") is { } regionId ? regions[regionId] : null;
            if (fromForm == null && toForm == null && region == null) continue;

            var evo = LaterEvolution(r, from);
            evo.FromForm = fromForm;
            evo.TargetForm = toForm;
            evo.Region = region;
            var evolutions = s.Evolutions ??= new();

            // The same evolution into the species' own form: the form is its by the same condition, or by chance
            var same = evolutions.FirstOrDefault(e => e.TargetSpecies == evo.TargetSpecies && e.Method == evo.Method && e.Item == evo.Item && e.Move == evo.Move
                && e.Type == evo.Type && e.Species == evo.Species && e.Location == evo.Location && e.Level == evo.Level && e.FromForm == evo.FromForm && e.Region == evo.Region);
            if (same != null)
            {
                // A form of its own condition (Lycanroc's by the hour, Urshifu's by the scroll) was there already,
                // into the species' own form; one only by chance or nature was merged with the species' own
                if (same.TargetForm == null && toForm != null && LaterEvolutionsOf(from).Count(e => e.TargetSpecies == evo.TargetSpecies) > 1)
                    same.TargetForm = toForm;
                continue;
            }
            if (region != null) evolutions.Insert(0, evo);
            else evolutions.Add(evo);
        }
    }

    /// <summary>
    /// Gives each form the Pokédex colour Pokémon Showdown has for it where that isn't its species' (Mega Charizard X
    /// is black, Alolan Vulpix white): PokeAPI has one colour to a species, and generated models are painted by it.
    /// </summary>
    public void AddFormColors(List<PokemonSpecies> list, Showdown showdown)
    {
        var colors = showdown.Species.Where(e => e.Text("color") != null).DistinctBy(e => e.Name.ToLowerInvariant())
            .ToDictionary(e => e.Name, e => e.Text("color")!, StringComparer.OrdinalIgnoreCase);
        foreach (var species in list)
            foreach (var form in species.Forms ?? new())
                if (colors.TryGetValue(form.Name, out var color) && color != species.Color) form.Color = color;
    }

    /// <summary>
    /// The Pokédex text, generated from the data in our own words (the games' entries are not used): what it is,
    /// its size and how it evolves.
    /// </summary>
    private static string DexEntry(PokemonSpecies s, List<PokemonSpecies> all)
    {
        string region = s.Generation switch
        {
            1 => "Kanto", 2 => "Johto", 3 => "Hoenn", 4 => "Sinnoh", 5 => "Unova", 6 => "Kalos", 7 => "Alola", 8 => "Galar", _ => "Paldea"
        };
        string types = s.SecondaryType is { } second ? $"{s.PrimaryType}- and {second}-type" : $"{s.PrimaryType}-type";
        string article = "AEIOU".Contains(s.Category.FirstOrDefault()) ? "an" : "a";
        var text = $"{s.Name} is {article} {s.Category} Pokémon, {types}, first found in {region}. " +
                   $"It stands {s.Height:0.0#} m tall and weighs {s.Weight:0.0#} kg.";

        var from = all.FirstOrDefault(o => o.Evolutions?.Any(e => e.TargetSpecies == s.Name) == true);
        if (from != null) text += $" It evolves from {from.Name}.";
        if (s.Evolutions is { Count: > 0 } evos)
        {
            var into = evos.Select(e => e.TargetSpecies).Distinct().ToList();
            text += into.Count == 1 ? $" It evolves into {into[0]}." : $" It can evolve into {string.Join(", ", into.SkipLast(1))} or {into[^1]}.";
        }
        return text;
    }

    // ================================================================== items

    /// <param name="evolutionItems">Items species evolve with, brought in whatever their category.</param>
    /// <param name="moves">The game's moves, for the text of the TMs and HMs that teach them.</param>
    public List<ItemData> Items(IReadOnlySet<string> evolutionItems, IReadOnlyList<MoveData> moves)
    {
        var items = new List<ItemData>();
        var names = new HashSet<string>();

        for (int index = 1; index < itemConstants.Count; index++)
        {
            var d = decomp.Item(itemConstants[index]);
            if (d == null) continue; // unused ids
            var data = d.Value;
            int? apiId = api.ItemByGen4Index(index);
            string name = apiId != null && api.HasItemName(apiId.Value) ? api.ItemName(apiId.Value) : Names.Clean(data.GetProperty("name").GetString()!);
            var item = PlatinumItem(index, name, data);
            item.Description = item.TeachesMove is { } move ? MachineText(move, moves.FirstOrDefault(m => m.Name == move)?.Description)
                : apiId != null ? api.Prose("item_prose", "item_id", apiId.Value) : "";
            if (names.Add(item.Name)) items.Add(item);
            else Console.Error.WriteLine($"  Platinum has two items called {item.Name}: {itemConstants[index]} is left out");
        }

        // Later generations' items that matter in battle, healing, catching or evolving
        foreach (var row in api.Items.Values.OrderBy(r => r.Int("id")))
        {
            int id = row.Int("id");
            if (id >= FirstAlternateForm || !api.HasItemName(id)) continue;
            if (!LaterItemCategories.Contains(row.Int("category_id")) && !evolutionItems.Contains(api.ItemName(id))) continue;
            if (api.ItemGeneration(id) is var gen && (gen <= 4 && gen != 0)) continue;
            string name = api.ItemName(id);
            if (!names.Add(name)) continue;
            int pocketId = api.Table("item_categories").First(c => c.Int("id") == row.Int("category_id")).Int("pocket_id");
            var pocket = pocketId switch
            {
                2 => ItemPocket.Medicine, 3 => ItemPocket.PokeBalls, 4 => ItemPocket.TMsAndHMs, 5 => ItemPocket.Berries,
                6 => ItemPocket.Mail, 7 => ItemPocket.BattleItems, 8 => ItemPocket.KeyItems, _ => ItemPocket.Items
            };
            // A Z-Crystal is held, so it is an ordinary item here (the games keep the ones in the bag apart)
            if (row.Int("category_id") == ZCrystals) pocket = ItemPocket.Items;
            items.Add(new ItemData
            {
                Id = 1000 + id,
                Name = name,
                Pocket = pocket,
                EffectType = pocket == ItemPocket.PokeBalls ? ItemEffectType.CatchPokemon : ItemEffectType.None,
                EffectValue = pocket == ItemPocket.PokeBalls ? 10 : 0,
                Price = row.IntOrNull("cost") ?? 0,
                CanUseInBattle = pocket is ItemPocket.PokeBalls or ItemPocket.BattleItems or ItemPocket.Medicine,
                CanUseInOverworld = pocket is ItemPocket.Medicine || row.Int("category_id") is 10 or 26 or 50,
                Description = api.Prose("item_prose", "item_id", id)
            });
        }
        return items;
    }

    /// <summary>
    /// What a TM or HM says of itself: the move Platinum's machine teaches, then that move's own text. PokeAPI's
    /// text for a machine names the move a later generation gave it, with the earlier ones in brackets (TM01 is
    /// Hone Claws there, HM05 Waterfall).
    /// </summary>
    private static string MachineText(string move, string? moveText) =>
        $"Teaches {move} to a compatible Pokémon. {moveText}".TrimEnd();

    /// <summary>A Platinum item: pocket, price, what using it does (the parts the engine knows) and its hold effect.</summary>
    private ItemData PlatinumItem(int index, string name, JsonElement d)
    {
        string pocket = d.GetProperty("fieldPocket").GetString()!;
        string fieldUse = d.GetProperty("fieldUseFunc").GetString()!;
        string battleUse = d.GetProperty("battleUseCategory").GetString()!;
        var p = d.GetProperty("itemUseParams");
        bool Has(string key) => p.ValueKind == JsonValueKind.Object && p.TryGetProperty(key, out var v) && v.ValueKind != JsonValueKind.False;
        int Num(string key) => p.ValueKind == JsonValueKind.Object && p.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;

        var item = new ItemData
        {
            Id = index,
            Name = name,
            Pocket = pocket switch
            {
                "POCKET_ITEMS" => ItemPocket.Items,
                "POCKET_MEDICINE" => ItemPocket.Medicine,
                "POCKET_BALLS" => ItemPocket.PokeBalls,
                "POCKET_TMHMS" => ItemPocket.TMsAndHMs,
                "POCKET_BERRIES" => ItemPocket.Berries,
                "POCKET_MAIL" => ItemPocket.Mail,
                "POCKET_BATTLE_ITEMS" => ItemPocket.BattleItems,
                "POCKET_KEY_ITEMS" => ItemPocket.KeyItems,
                _ => throw new InvalidDataException($"Unknown pocket {pocket}")
            },
            Price = d.GetProperty("price").GetInt32(),
            CanUseInBattle = battleUse != "BATTLE_USE_CATEGORY_NONE" || pocket == "POCKET_BATTLE_ITEMS",
            CanUseInOverworld = fieldUse != "ITEM_USE_FUNC_NONE" && pocket != "POCKET_BALLS",
        };

        string[] statusKeys = { "healSleep", "healPoison", "healBurn", "healFreeze", "healParalysis", "healConfusion" };
        int statusCount = statusKeys.Count(Has);
        // The table keeps the HP as a signed byte: all, half and a quarter of it are -1, -2 and -3, and anything
        // else below zero is an amount past 127 (a Hyper Potion's and an Energy Root's 200)
        int hp = Num("hpRestored");
        if (hp < -3) hp += 256;
        if (item.Pocket == ItemPocket.PokeBalls)
        {
            item.EffectType = ItemEffectType.CatchPokemon;
            item.EffectValue = name switch { "Master Ball" => 9999, "Ultra Ball" => 20, "Great Ball" or "Safari Ball" => 15, _ => 10 };
        }
        else if (item.Pocket == ItemPocket.KeyItems) item.EffectType = ItemEffectType.KeyItem;
        else if (Has("levelUp")) { item.EffectType = ItemEffectType.LevelUp; item.EffectValue = 1; }
        else if (Has("revive")) { item.EffectType = ItemEffectType.Revive; item.EffectValue = hp == -1 ? 100 : 50; }
        else if (hp == -1 && statusCount >= 5) { item.EffectType = ItemEffectType.FullRestore; item.EffectValue = 9999; }
        else if (hp != 0 && item.Pocket != ItemPocket.Berries) { item.EffectType = ItemEffectType.HealHP; item.EffectValue = hp == -1 ? 9999 : hp; }
        else if (statusCount > 0 && item.Pocket != ItemPocket.Berries)
        {
            item.EffectType = ItemEffectType.HealStatus;
            // One status named; several (Full Heal) leave it None, which cures any
            if (statusCount == 1)
                item.HealsStatus = statusKeys.First(Has) switch
                {
                    "healSleep" => StatusCondition.Sleep,
                    "healPoison" => StatusCondition.Poison,
                    "healBurn" => StatusCondition.Burn,
                    "healFreeze" => StatusCondition.Freeze,
                    "healParalysis" => StatusCondition.Paralyze,
                    _ => StatusCondition.None
                };
        }

        string hold = d.GetProperty("holdEffect").GetString()!;
        if (hold != "HOLD_EFFECT_NONE")
        {
            item.HoldEffect = Names.Pascal(hold, "HOLD_EFFECT_");
            item.HoldParam = d.GetProperty("effectParam").GetInt32();
        }
        if (d.TryGetProperty("teachesMove", out var teaches) && teaches.GetString() is { } move)
            item.TeachesMove = api.MoveName(moveConstants.IndexOf(move));

        // The rest of the table, for plan 06 · R8 and R11: numbers and the decompilation's own names, never its text
        string? Named(string key, string prefix) =>
            d.GetProperty(key).GetString() is { } value && !value.EndsWith("_NONE") ? Names.Pascal(value, prefix) : null;
        item.FlingPower = d.GetProperty("flingPower").GetInt32();
        item.FlingEffect = Named("flingEffect", "FLING_EFFECT_");
        item.PluckEffect = Named("pluckEffect", "PLUCK_EFFECT_");
        item.NaturalGiftPower = d.GetProperty("naturalGiftPower").GetInt32();
        if (d.GetProperty("naturalGiftType").GetString() is { } giftType) item.NaturalGiftType = Gen4Type(giftType);
        item.CantBeTossed = d.GetProperty("preventToss").GetBoolean();
        item.CanBeRegistered = d.GetProperty("canRegister").GetBoolean();
        item.FieldUse = Named("fieldUseFunc", "ITEM_USE_FUNC_");
        item.BattleUse = Named("battleUseCategory", "BATTLE_USE_CATEGORY_");
        item.BattlePocket = Named("battlePocket", "BATTLE_POCKET_MASK_");
        if (hold == "HOLD_EFFECT_NONE") item.EffectParam = d.GetProperty("effectParam").GetInt32();
        if (p.ValueKind == JsonValueKind.Object)
        {
            var use = new Dictionary<string, int>();
            foreach (var param in p.EnumerateObject())
            {
                if (param.Value.ValueKind == JsonValueKind.True) use[param.Name] = 1;
                else if (param.Value.ValueKind == JsonValueKind.Number && param.Value.GetInt32() != 0)
                    use[param.Name] = param.Name == "hpRestored" ? hp : param.Value.GetInt32();
            }
            if (use.Count > 0) item.Use = use;
        }
        // A berry's growing and its taste (plan 06 · R14a): numbers only
        if (d.TryGetProperty("berryData", out var berry) && berry.ValueKind == JsonValueKind.Object)
            item.Berry = new BerryData
            {
                Size = berry.GetProperty("size").GetInt32(),
                Firmness = Names.Pascal(berry.GetProperty("firmness").GetString()!, "FIRMNESS_"),
                BaseYield = berry.GetProperty("baseYield").GetInt32(),
                StageDuration = berry.GetProperty("stageDuration").GetInt32(),
                MoistureDrainRate = berry.GetProperty("moistureDrainRate").GetInt32(),
                Spiciness = berry.GetProperty("spiciness").GetInt32(),
                Dryness = berry.GetProperty("dryness").GetInt32(),
                Sweetness = berry.GetProperty("sweetness").GetInt32(),
                Bitterness = berry.GetProperty("bitterness").GetInt32(),
                Sourness = berry.GetProperty("sourness").GetInt32(),
                Smoothness = berry.GetProperty("smoothness").GetInt32()
            };
        return item;
    }

    /// <summary>
    /// Gives the Mega Stones and Z-Crystals their owners from Pokémon Showdown's item table (plan 06 · R20–R21):
    /// the species and form a stone brings out, the type a crystal is for, or the one species' move it turns
    /// into a Z-Move of its own. A stone or crystal PokeAPI doesn't have yet is added from Showdown alone.
    /// </summary>
    public void AddLaterMechanics(List<ItemData> items, Showdown showdown)
    {
        var byName = items.ToDictionary(i => i.Name, StringComparer.OrdinalIgnoreCase);
        foreach (var e in showdown.Items.Where(e => e.Has("megaStone") || e.Has("zMove")).OrderBy(e => e.Number("num") ?? 0))
        {
            // Past games' fakes and the Create-A-Pokémon project's items are not the main games'
            if (e.Text("isNonstandard") == "CAP") continue;
            if (!byName.TryGetValue(e.Name, out var item))
            {
                item = new ItemData
                {
                    Id = ShowdownOnlyItemBase + (e.Number("num") ?? throw new InvalidDataException($"{e.Name} has no number")),
                    Name = e.Name,
                    Pocket = ItemPocket.Items,
                    EffectType = ItemEffectType.None,
                    Price = 0,
                    CanUseInBattle = false,
                    CanUseInOverworld = false,
                    Description = ""
                };
                items.Add(item);
                byName[item.Name] = item;
            }

            if (Showdown.Pairs(e.Fields.GetValueOrDefault("megaStone")) is { Count: > 0 } stone)
            {
                // A few stones are for one form of a species (Floette's Eternal Flower): the species is the
                // longest start of the name that is one
                string holder = stone[0].Key, owner = holder;
                while (PokemonDexOf(owner) == 0 && owner.Contains('-')) owner = owner[..owner.LastIndexOf('-')];
                if (PokemonDexOf(owner) == 0) throw new InvalidDataException($"{e.Name} is for {holder}, which is no species");
                item.MegaStone = new MegaStone { Species = owner, Form = stone[0].Value, HeldByForm = owner == holder ? null : holder };
                if (item.Description == "") item.Description = $"Held by {holder}, it lets it Mega Evolve in battle.";
            }
            else if (e.Is("zMove", "true"))
            {
                var type = Enum.Parse<PokemonType>(e.Text("zMoveType")!);
                item.ZCrystal = new ZCrystal { Type = type };
                if (item.Description == "") item.Description = $"Held, it turns a {type}-type move into a Z-Move.";
            }
            else if (e.Text("zMove") is { } zMove)
            {
                var users = Showdown.List(e.Fields.GetValueOrDefault("itemUser"));
                item.ZCrystal = new ZCrystal { Move = zMove, From = e.Text("zMoveFrom"), Users = users };
                if (item.Description == "") item.Description = $"Held by {string.Join(" or ", users)}, it turns {e.Text("zMoveFrom")} into a Z-Move.";
            }
        }
    }

    /// <summary>Ids of items only Pokémon Showdown has start here (plus its own number for the item).</summary>
    public const int ShowdownOnlyItemBase = 5000;

    private string ItemName(string constant)
    {
        int index = itemConstants.IndexOf(constant);
        int? id = api.ItemByGen4Index(index);
        return id != null ? api.ItemName(id.Value) : throw new InvalidDataException($"No PokeAPI item for {constant}");
    }
}
