using System.Text.Json;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace DataImporter;

/// <summary>
/// Builds the game's species, moves, abilities and items. Generation 1–4 (national numbers 1–493, moves 1–467,
/// abilities 1–123, Platinum's items) come from Platinum's own data in the decompilation; everything later comes
/// from PokeAPI. Names, categories, egg groups, colours, shapes and the short effect texts come from PokeAPI too.
/// </summary>
public sealed class Importer
{
    public const int LastPlatinumSpecies = 493;
    public const int LastPlatinumMove = 467;

    /// <summary>Later evolutions of Platinum species that start from a regional form (Galarian Meowth and the like),
    /// which this game doesn't have yet.</summary>
    private const int FirstAlternateForm = 10000;

    /// <summary>Learnsets for later species come from the newest of these games that has one (PokeAPI version groups).</summary>
    private static readonly int[] LearnsetVersionGroups = { 25, 27, 26, 20, 21, 22, 18, 17, 16, 15, 14, 11, 24, 23, 30, 31, 32, 19 };

    /// <summary>PokeAPI item categories brought in for items after Generation 4: what can be used or held in battle,
    /// healed with, caught with or evolved with. Mega Stones, Z-Crystals, TRs and the like come with plan 06's
    /// sessions on those mechanics.</summary>
    private static readonly HashSet<int> LaterItemCategories = new()
    {
        1, 2, 3, 4, 5, 6, 7, 10, 12, 13, 14, 15, 16, 17, 18, 19, 26, 27, 28, 29, 30, 33, 34, 39, 42, 45, 50
    };

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
            m.Support = support;
            moves.Add(m);
        }
        return moves;
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

        s.Abilities = d.GetProperty("abilities").EnumerateArray().Select(a => a.GetString()!)
            .Where(a => a != "ABILITY_NONE").Select(a => api.AbilityName(abilityConstants.IndexOf(a))).Distinct().ToList();

        s.Learnset = d.GetProperty("learnset").GetProperty("by_level").EnumerateArray()
            .Select(e => new LearnableMove { Level = e[0].GetInt32(), MoveName = api.MoveName(moveConstants.IndexOf(e[1].GetString()!)) }).ToList();

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
        s.Learnset = LaterLearnset(pid);
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
    /// the head of the party, and Legends: Arceus's agile and strong style moves are plain uses of the move.
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
                else if (time != "") { evo.Method = EvolutionMethod.Other; notes.Add($"level up at {time}"); }

                if (r.IntOrNull("minimum_steps") is { } steps) { evo.Method = EvolutionMethod.LevelAfterSteps; evo.Value = steps; }
                if (r.IntOrNull("used_move_id") != null) AfterMoveUses();
                if (r.IntOrNull("minimum_damage_taken") is { } damage) { evo.Method = EvolutionMethod.Other; notes.Add($"after losing {damage} HP from recoil without fainting"); }
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
        int hp = Num("hpRestored");
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
        if (hold != "HOLD_EFFECT_NONE") item.HoldEffect = Names.Pascal(hold, "HOLD_EFFECT_");
        if (d.TryGetProperty("teachesMove", out var teaches) && teaches.GetString() is { } move)
            item.TeachesMove = api.MoveName(moveConstants.IndexOf(move));
        return item;
    }

    private string ItemName(string constant)
    {
        int index = itemConstants.IndexOf(constant);
        int? id = api.ItemByGen4Index(index);
        return id != null ? api.ItemName(id.Value) : throw new InvalidDataException($"No PokeAPI item for {constant}");
    }
}
