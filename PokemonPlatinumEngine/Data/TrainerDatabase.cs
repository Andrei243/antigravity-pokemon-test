using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json.Serialization;
using PokemonPlatinumEngine.Battle.Sim.Ai;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// One of Platinum's trainers as <c>trainers.json</c> has it (plan 06 · R9, written by <c>tools/DataImporter</c> from
/// the decompilation's <c>res/trainers/data</c>): how the trainer thinks, the items it uses in battle, its team and
/// the prize money the original's formula gives. Never what the trainer says: our overlays write that.
/// </summary>
public sealed class TrainerRecord
{
    /// <summary>The trainer's constant without its prefix, lower case (<c>youngster_tristan</c>).</summary>
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Class { get; set; } = "";

    /// <summary>The routines of the AI script it thinks with (<see cref="AiFlags"/>, by name).</summary>
    public List<string> Ai { get; set; } = new();

    /// <summary>The items it can use in battle, in the order it looks at them.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<string> Items { get; set; } = new();

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool DoubleBattle { get; set; }

    /// <summary>The trainer has a Key Stone and Mega Evolves under rules that have it (plan 06 · R20); none of Platinum's does.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool KeyStone { get; set; }

    /// <summary>The trainer has a Z-Ring and uses Z-Moves under rules that have them (plan 06 · R21); none of Platinum's does.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool ZRing { get; set; }

    /// <summary>The trainer has a Dynamax Band and Dynamaxes under rules that have it (plan 06 · R22); none of Platinum's does.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool DynamaxBand { get; set; }

    /// <summary>The trainer has a Tera Orb and Terastallizes under rules that have it (plan 06 · R23); none of Platinum's does.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool TeraOrb { get; set; }

    public int PrizeMoney { get; set; }
    public List<TrainerPokemonRecord> Party { get; set; } = new();

    /// <summary>
    /// Who the trainer is at each of the Vs. Seeker's five levels (plan 06 · R12; <c>gVsSeekerRematchData</c>): a
    /// trainer's id for each, null for a level with no team of its own, as long as the original's row goes. Absent
    /// for one who can't be battled again.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string?>? Rematches { get; set; }
}

/// <summary>A Pokémon of a trainer's team as the original keeps it, with the personality it works out for it.</summary>
public sealed class TrainerPokemonRecord
{
    public string Species { get; set; } = "";

    /// <summary>The form, by its name (<c>Shellos-East</c>); null for the species' own.</summary>
    public string? Form { get; set; }
    public int Level { get; set; }

    /// <summary>Its IVs, all six alike, as a share of 255 of the most (<c>iv_scale</c>).</summary>
    public int IvScale { get; set; }

    /// <summary>What its nature, gender and ability follow from (<c>TrainerData_BuildParty</c>).</summary>
    public uint Personality { get; set; }

    public string? Item { get; set; }

    /// <summary>The moves the trainer chose for it; null when it knows what its level taught it.</summary>
    public List<string>? Moves { get; set; }

    /// <summary>
    /// The Ball Capsule its ball comes with (plan 06 · R17b; the original's <c>ball_seal</c>): a place of the trainers'
    /// own capsules from 1 (<see cref="SealDatabase.TrainerCapsule"/>), 0 and left out for a plain ball.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int BallSeal { get; set; }
}

/// <summary>
/// Platinum's trainers, read from <c>Data/trainers.json</c> the first time one is asked for, and beside them the
/// trainers of this game's own (plan 14 · B14), read the same way: the Master Trainers of
/// <c>Data/master-trainers.json</c> (written by <c>tools/DataImporter</c>, one for every species) and the people of
/// ours in <c>Data/our-trainers.json</c> (written by hand). <see cref="Get"/> finds any of them; <see cref="All"/> is
/// Platinum's table alone, so whatever goes through the original's trainers goes through those and no others.
/// </summary>
public static class TrainerDatabase
{
    public const string FileName = "trainers.json";

    /// <summary>The Master Trainers (plan 14 · B14): <c>master_&lt;species&gt;</c>, one Pokémon each at <see cref="MasterLevel"/>.</summary>
    public const string MastersFileName = "master-trainers.json";

    /// <summary>The trainers of our own people (plan 14 · B14 first): a team and a name the original never had.</summary>
    public const string OursFileName = "our-trainers.json";

    /// <summary>The level of a master's Pokémon, as Let's Go's masters' are.</summary>
    public const int MasterLevel = 75;

    private static readonly List<TrainerRecord> Platinum = GameDataFiles.Load<List<TrainerRecord>>(FileName);
    private static readonly List<TrainerRecord> MasterList = GameDataFiles.Load<List<TrainerRecord>>(MastersFileName);
    private static readonly List<TrainerRecord> OurList = GameDataFiles.Load<List<TrainerRecord>>(OursFileName);

    /// <summary>
    /// The trainers of each region whose world folder has its own <c>trainers.json</c> (plan 18's decision 8, plan
    /// 19 · K2: Kanto's, from FireRed), by the region's folder name. Their ids carry the region's name before them
    /// (<c>kanto_youngster_ben</c>), so they never meet Platinum's in the list of trainers beaten.
    /// </summary>
    private static readonly Dictionary<string, List<TrainerRecord>> RegionLists = LoadRegions();

    private static readonly Dictionary<string, TrainerRecord> Trainers = Index(
        new[] { (FileName, Platinum), (MastersFileName, MasterList), (OursFileName, OurList) }
            .Concat(RegionLists.Select(r => (Path.Combine(World.Folder, r.Key, FileName), r.Value))));

    /// <summary>
    /// Every file's trainers in one table by id. An id is one trainer's across every file (a region's prefix is what
    /// keeps them apart, <c>kanto_</c>, <c>johto_</c>), and two files that give the same one are named, both of them.
    /// </summary>
    public static Dictionary<string, TrainerRecord> Index(IEnumerable<(string File, List<TrainerRecord> Records)> files)
    {
        var table = new Dictionary<string, TrainerRecord>(StringComparer.OrdinalIgnoreCase);
        var from = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (file, records) in files)
            foreach (var t in records)
            {
                if (from.TryGetValue(t.Id, out string? first))
                    throw new InvalidOperationException(
                        $"The trainer id {t.Id} is given by both {first} and {file}: ids must be unique across every file of trainers (a region's carry its prefix, kanto_ or johto_)");
                table[t.Id] = t;
                from[t.Id] = file;
            }
        return table;
    }

    private static Dictionary<string, List<TrainerRecord>> LoadRegions()
    {
        var lists = new Dictionary<string, List<TrainerRecord>>(StringComparer.OrdinalIgnoreCase);
        string root = GameDataFiles.PathOf(World.Folder);
        if (!Directory.Exists(root)) return lists;
        foreach (string dir in Directory.GetDirectories(root).OrderBy(d => d, StringComparer.Ordinal))
        {
            string relative = Path.Combine(World.Folder, Path.GetFileName(dir), FileName);
            if (File.Exists(GameDataFiles.PathOf(relative)))
                lists[Path.GetFileName(dir)] = GameDataFiles.Load<List<TrainerRecord>>(relative);
        }
        return lists;
    }

    /// <summary>The regions with trainers of their own, by their world folder's name (<c>kanto</c>, <c>johto</c>).</summary>
    public static IEnumerable<string> Regions => RegionLists.Keys;

    /// <summary>A region's own trainers (<c>Data/world/&lt;region&gt;/trainers.json</c>), or none.</summary>
    public static IReadOnlyList<TrainerRecord> OfRegion(string region) =>
        RegionLists.TryGetValue(region, out var list) ? list : Array.Empty<TrainerRecord>();

    /// <summary>
    /// A trainer placed in a region's world, by its id or its constant as the region's own data name it
    /// (<c>TRAINER_YOUNGSTER_BEN</c> in Kanto is <c>kanto_youngster_ben</c>): the region's table first, then the
    /// others (<see cref="Get(string?)"/>). Sinnoh's trainers are Platinum's table, which has no prefix.
    /// </summary>
    public static TrainerRecord? Get(string? idOrConstant, string? region)
    {
        if (string.IsNullOrEmpty(idOrConstant)) return null;
        if (!string.IsNullOrEmpty(region) && RegionLists.ContainsKey(region))
        {
            string key = idOrConstant.StartsWith("TRAINER_", StringComparison.Ordinal) ? idOrConstant["TRAINER_".Length..] : idOrConstant;
            if (Trainers.TryGetValue(region.ToLowerInvariant() + "_" + key, out var own)) return own;
        }
        return Get(idOrConstant);
    }

    /// <summary>Platinum's trainers, in the original's order.</summary>
    public static IEnumerable<TrainerRecord> All => Platinum;

    /// <summary>The Master Trainers, in National Pokédex order.</summary>
    public static IReadOnlyList<TrainerRecord> Masters => MasterList;

    /// <summary>The trainers of our own people (the Fight Area's scientist).</summary>
    public static IReadOnlyList<TrainerRecord> Ours => OurList;

    /// <summary>
    /// The id of a species' master: <c>master_</c> and the species' name in the table's own style, lower case with
    /// words joined by underscores (<c>master_bidoof</c>, <c>master_mr_mime</c>, <c>master_nidoran_f</c>,
    /// <c>master_farfetchd</c>, <c>master_flabebe</c>).
    /// </summary>
    public static string MasterId(string species)
    {
        var sb = new System.Text.StringBuilder("master_");
        foreach (char c in species.Normalize(System.Text.NormalizationForm.FormD))
        {
            if (c == '\u2640') sb.Append("_f");
            else if (c == '\u2642') sb.Append("_m");
            else if (char.IsAsciiLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
            else if (c is ' ' or '-' or '_' or ':' && sb[^1] != '_') sb.Append('_');
        }
        while (sb[^1] == '_') sb.Length--;
        return sb.ToString();
    }

    /// <summary>The rival's trainer class: his data's name stands for the one the player gives him.</summary>
    public const string RivalClass = "Rival";

    /// <summary>The rival's class, or one of FireRed's two of him ("Rival Early", "Rival Late"; plan 19 · K3).</summary>
    public static bool IsRival(string trainerClass) =>
        trainerClass == RivalClass || trainerClass.StartsWith(RivalClass + " ", StringComparison.Ordinal);

    /// <summary>
    /// Kanto's Champion, who is the rival (plan 19 · K7): FireRed's TRAINER_CHAMPION_FIRST_* and _REMATCH_* are of the
    /// Champion's class, which picks the League's band and song, under the rival's own placeholder name.
    /// </summary>
    public static bool IsRivalAsChampion(TrainerRecord record) =>
        record.Class == "Champion" && record.Id.StartsWith("kanto_champion_", StringComparison.Ordinal);

    /// <summary>A trainer by its id (<c>youngster_tristan</c>) or its constant (<c>TRAINER_YOUNGSTER_TRISTAN</c>); null for none.</summary>
    public static TrainerRecord? Get(string? idOrConstant)
    {
        if (string.IsNullOrEmpty(idOrConstant)) return null;
        string key = idOrConstant.StartsWith("TRAINER_", StringComparison.Ordinal) ? idOrConstant["TRAINER_".Length..] : idOrConstant;
        return Trainers.TryGetValue(key, out var t) ? t : null;
    }

    /// <summary>
    /// Gives a trainer of the map what Platinum's data says of it: how it thinks and the items it uses, and, where the
    /// map's record names the same team (or none), the team itself as the original builds it, with its prize money.
    /// A team the map writes differently is left as it is.
    /// </summary>
    public static void Fill(Trainer trainer, TrainerRecord record)
    {
        trainer.Ai = AiFlagNames.Parse(record.Ai);
        trainer.Items = record.Items.ToList();
        trainer.KeyStone = record.KeyStone;
        trainer.ZRing = record.ZRing;
        trainer.DynamaxBand = record.DynamaxBand;
        trainer.TeraOrb = record.TeraOrb;
        var mine = trainer.Party.Members;
        bool same = mine.Count == 0 || (mine.Count == record.Party.Count &&
            mine.Zip(record.Party).All(pair => pair.First.Species.Name == pair.Second.Species && pair.First.Level == pair.Second.Level));
        if (!same) return;
        if (mine.Count == 0)
        {
            if (string.IsNullOrEmpty(trainer.Name)) trainer.Name = record.Name;
            // The rival's name is the one the player gives him, not the data's
            if ((IsRival(record.Class) || IsRivalAsChampion(record)) && trainer.Name == record.Name) trainer.Name = "{rival}";
            // FireRed splits its rival's class in two (RIVAL_EARLY, RIVAL_LATE, plan 19 · K3): both are the rival's class here
            if (string.IsNullOrEmpty(trainer.TrainerClass) || trainer.TrainerClass == "Trainer") trainer.TrainerClass = IsRival(record.Class) ? RivalClass : record.Class;
            trainer.PrizeMoney = record.PrizeMoney;
            trainer.DoubleBattle |= record.DoubleBattle;
        }
        trainer.Party.Clear();
        foreach (var m in record.Party) trainer.Party.Add(Build(m));
    }

    /// <summary>A fresh team of the trainer's, built as the original builds it.</summary>
    public static Party PartyOf(TrainerRecord record)
    {
        var party = new Party();
        foreach (var m in record.Party) party.Add(Build(m));
        return party;
    }

    /// <summary>
    /// A trainer's Pokémon as <c>TrainerData_BuildParty</c> makes it: every IV the IV scale's share of 31, the nature,
    /// gender and ability from its personality (the nature is the personality's remainder by 25, the gender its last
    /// byte against the species' ratio, the second ability its lowest bit), never shiny, holding its item and knowing
    /// the moves chosen for it or else what its level taught it.
    /// </summary>
    public static Pokemon Build(TrainerPokemonRecord m)
    {
        var species = PokemonDatabase.Get(m.Species) ?? throw new InvalidOperationException($"No species called {m.Species}");
        int iv = m.IvScale * 31 / 255;
        // OTID_NOT_SHINY: its first trainer's number is drawn until it isn't
        var pokemon = new Pokemon(species, m.Level, m.Personality, new Random((int)(m.Personality & 0x7FFFFFFF)))
        {
            IsShiny = false,
            IvHP = iv, IvAttack = iv, IvDefense = iv, IvSpAttack = iv, IvSpDefense = iv, IvSpeed = iv
        };
        if (m.Form != null) pokemon.ChangeForm(m.Form);
        var abilities = pokemon.Abilities;
        pokemon.AbilityName = abilities.Count == 0 ? null : abilities.Count > 1 && (m.Personality & 1) == 1 ? abilities[1] : abilities[0];
        pokemon.HeldItem = m.Item == null ? null : ItemDatabase.Get(m.Item);
        if (m.Moves is { Count: > 0 } chosen)
        {
            pokemon.Moves.Clear();
            foreach (string move in chosen) pokemon.Moves.Add(MoveDatabase.Create(move));
        }
        // Pokemon_SetBallSeal: the trainers' capsule goes on its ball as capsule number 1
        if (SealDatabase.TrainerCapsule(m.BallSeal) is { } capsule)
        {
            pokemon.CapsuleId = 1;
            pokemon.Capsule = capsule;
        }
        pokemon.RecalculateStats();
        pokemon.CurrentHP = pokemon.MaxHP;
        return pokemon;
    }

    /// <summary>The original's gender from a personality: its last byte under the species' share of females (in 256ths) makes it female.</summary>
    public static Gender GenderOf(PokemonSpecies species, uint personality) => Personality.GenderOf(species, personality);
}
