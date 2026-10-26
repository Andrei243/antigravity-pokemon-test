using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Models.Frontier;

/// <summary>
/// A round of the Battle Pyramid (plan 14 · B10; <c>docs/mechanics/rulings.md</c>, "The Battle Pyramid"): Emerald's
/// facility as a venue of the Battle Frontier's annex. Three Pokémon go in as copies at level 50 and stay as the floors
/// leave them; seven floors are climbed, each made from a seed drawn as the round begins (<see cref="PyramidFloor"/>), so
/// a round's floors are the same however often they are made. A floor is dark but for a circle round the player, which
/// Flash doesn't lift and every battle won widens (Emerald's <c>UpdatePyramidLightRadius</c>, its light given after a
/// wild battle too); its trainers see the player and battle, its wild Pokémon are of the round's habitat
/// (<see cref="WildTable"/>), its finds are used at once on the three (<see cref="UseFind"/>). The Pyramid King waits at
/// the top of the 21st floor in a row, and of the 70th and every 35th after with his other team. A round is lost when
/// all three have fainted. Every draw at the round's start is the sixteen-bit draw the caller hands over; a floor's own
/// draws are its seed's. No drawing or input.
/// </summary>
public sealed class BattlePyramidChallenge
{
    public const int TeamSize = 3, Floors = 7;

    /// <summary>The light's radius in Emerald's pixels (sixteen to a tile): where each floor begins, what a battle won adds, and the most.</summary>
    public const int StartLight = 32, LightStep = 4, MostLight = 120;

    /// <summary>Our own trainers of the Pyramid King's two teams (<c>Data/our-trainers.json</c>).</summary>
    public const string KingId = "pyramid_king_brandon", KingGoldId = "pyramid_king_brandon_gold";

    /// <summary>The places in the team of the three entered.</summary>
    public IReadOnlyList<int> Slots { get; }

    /// <summary>The three as the round has them, copies at level 50 that every floor leaves as it found them.</summary>
    public Party Team { get; }

    /// <summary>The seed of each of the seven floors, drawn as the round begins.</summary>
    public IReadOnlyList<int> Seeds { get; }

    /// <summary>The floor the player is on, 1 to 7; 8 once the top is reached.</summary>
    public int Floor { get; private set; } = 1;

    /// <summary>The floors climbed in a row before this round.</summary>
    public int StreakBefore { get; }

    /// <summary>The floor laid out, its map and the trainers standing on it, one to each of its trainers' places.</summary>
    public PyramidFloor? Layout { get; private set; }
    public Map? FloorMap { get; private set; }
    public List<FrontierOpponent> FloorFoes { get; } = new();

    /// <summary>How wide the light round the player is, in Emerald's pixels.</summary>
    public int Light { get; private set; } = StartLight;

    /// <summary>A wild Pokémon met on the floor and waiting for its battle (the floor's <c>Wild</c> script).</summary>
    public Pokemon? PendingWild { get; set; }

    private BattlePyramidChallenge(IReadOnlyList<int> slots, Party team, IReadOnlyList<int> seeds, int streakBefore)
    {
        Slots = slots;
        Team = team;
        Seeds = seeds;
        StreakBefore = streakBefore;
    }

    /// <summary>The round of the streak, from 0: what the trainers' rows, the habitat and the finds go by.</summary>
    public int Round => StreakBefore / Floors;

    /// <summary>A round begun with the three entered: one walked out on ends the streak first, then the seven floors' seeds are drawn.</summary>
    public static BattlePyramidChallenge Begin(FrontierRecords records, Party party, IReadOnlyList<int> slots, Func<int> next16)
    {
        var record = records.Pyramid;
        if (record.InProgress) record.Streak = 0;
        record.InProgress = true;
        var seeds = Enumerable.Range(0, Floors).Select(_ => (next16() & 0xFFFF) | (next16() & 0x7FFF) << 16).ToList();
        return new BattlePyramidChallenge(slots.ToList(), FrontierRules.TeamFor(party, slots), seeds, record.Streak);
    }

    /// <summary>
    /// The floor the player is on laid out and made a map (<see cref="PyramidFloor.ToFile"/>): its trainers drawn from the
    /// Tower's row of the round with three each, stood on their places with their looks, and the light set back to where
    /// a floor begins. Made twice from the same seed it is the same floor with the same trainers.
    /// </summary>
    public Map MakeFloor()
    {
        int seed = Seeds[Math.Clamp(Floor - 1, 0, Floors - 1)];
        var layout = PyramidFloor.Generate(seed, Floor - 1);
        var rng = new Random(seed ^ 0x5A5A5A);
        int Next16() => rng.Next(0x10000);
        var map = layout.ToFile(FindsFor(Round, layout.Finds.Count, Next16), WildTable(Round)).ToMap();
        map.IsDark = true;
        map.FlashLifts = false;
        map.DarkReach = ReachOf(StartLight);
        FloorFoes.Clear();
        int[] range = FrontierDatabase.TowerRooms[Math.Min(7, Round)];
        var drawn = new HashSet<int>();
        for (int i = 0; i < layout.Trainers.Count; i++)
        {
            int trainer;
            int guard = 0;
            do trainer = range[0] + Next16() % (range[1] - range[0] + 1); while (!drawn.Add(trainer) && ++guard < 100);
            var foe = FrontierTrainers.ForTrainers(TeamSize, trainer, trainer, false, Next16)[0];
            FloorFoes.Add(foe);
            var (x, y, facing) = layout.Trainers[i];
            map.Add(new NPC
            {
                Name = $"{foe.Record.Class} {foe.Record.Name}",
                NpcType = FrontierTrainers.LookOf(foe.Record),
                GridX = x, GridY = y, Facing = facing,
                Key = TrainerKey(i),
                LinesPlace = PyramidFloor.MapName,
                LinesKey = TrainerKey(i),
                Script = "Trainer",
                IsTrainer = true,
                TrainerData = new Trainer
                {
                    Id = $"pyramid_{TrainerKey(i)}", Name = foe.Record.Name, TrainerClass = foe.Record.Class, PrizeMoney = 0,
                    SightRange = PyramidFloor.Sight, Ai = FrontierTrainers.Mind, Look = FrontierTrainers.LookOf(foe.Record)
                }
            });
        }
        Light = StartLight;
        PendingWild = null;
        Layout = layout;
        FloorMap = map;
        return map;
    }

    /// <summary>A floor trainer's key on the map: <c>trainer_1</c> to <c>trainer_8</c>.</summary>
    public static string TrainerKey(int i) => $"trainer_{i + 1}";

    /// <summary>The Frontier trainer someone of the floor is, by their key; null for anyone else.</summary>
    public FrontierOpponent? FoeOf(NPC? person) =>
        person?.Key is { } key && key.StartsWith("trainer_") && int.TryParse(key["trainer_".Length..], out int n) && n >= 1 && n <= FloorFoes.Count
            ? FloorFoes[n - 1] : null;

    /// <summary>How far round the player a floor is seen for a light of so many pixels: the circle in tiles and the dark's soft edge (<see cref="DewfordLights.ReachFor"/>'s way).</summary>
    public static float ReachOf(int light) => light / 16f + Darkness.Soft;

    /// <summary>A battle won on the floor (Emerald's <c>PYRAMID_LIGHT_INCR_RADIUS</c>, after any battle won): the light widens, up to its most.</summary>
    public void Brighten() => Light = Math.Min(MostLight, Light + LightStep);

    /// <summary>
    /// The stairs climbed: the floor counts toward the streak, and the next floor is the one to come. True once the
    /// seventh is climbed and the top is reached.
    /// </summary>
    public bool Climb(FrontierRecords records)
    {
        var record = records.Pyramid;
        record.Streak = Math.Min(FrontierRecords.Most, record.Streak + 1);
        record.Best = Math.Max(record.Best, record.Streak);
        Floor++;
        Layout = null;
        FloorMap = null;
        FloorFoes.Clear();
        return Floor > Floors;
    }

    /// <summary>Which of his two teams the Pyramid King brings to the top: none, silver (21 floors in a row) or gold (70, and every 35 after).</summary>
    public static int KingFor(int floors) => floors == 21 ? 1 : floors == 70 || (floors > 70 && (floors - 70) % 35 == 0) ? 2 : 0;

    /// <summary>The Pyramid King with the team the top calls for, at level 50.</summary>
    public static Trainer King(int team)
    {
        var record = TrainerDatabase.Get(team == 2 ? KingGoldId : KingId)
            ?? throw new InvalidOperationException("The Pyramid King has no team in our-trainers.json.");
        return BattlePikeChallenge.OurBrain(record, "brandon");
    }

    /// <summary>The floor's trainer as a battle takes them, their Pokémon as copies.</summary>
    public static Trainer AsTrainer(FrontierOpponent foe) => BattlePikeChallenge.Copied(foe);

    /// <summary>All three of the round have fainted: the round is lost.</summary>
    public bool AllDown => Team.Members.All(p => p.IsFainted);

    /// <summary>The round is over, won or lost: the record is written, a loss ending the streak.</summary>
    public void Finish(FrontierRecords records, bool won)
    {
        var record = records.Pyramid;
        record.InProgress = false;
        if (!won)
        {
            record.Streak = 0;
            return;
        }
        record.Rounds = Math.Min(FrontierRecords.Most, record.Rounds + 1);
    }

    /// <summary>The Battle Points a round won earns: the Dome's rule of ours, by the rounds won in a row before it.</summary>
    public int PointsEarned => Floor > Floors ? BattleDomeChallenge.PointsFor(Round) : 0;

    // ------------------------------------------------------------------ the habitats and the finds

    /// <summary>
    /// The habitat of each round's wild Pokémon (ours: Emerald's rounds each have a theme of their own, which this game
    /// takes from the habitats its Pokédex gives every species), one round after another and round again after eight.
    /// </summary>
    public static IReadOnlyList<DexHabitat> Habitats => new[]
    {
        DexHabitat.Cave, DexHabitat.RoughTerrain, DexHabitat.Mountain, DexHabitat.Forest,
        DexHabitat.Grassland, DexHabitat.Urban, DexHabitat.WatersEdge, DexHabitat.Sea
    };

    /// <summary>The habitat of a round.</summary>
    public static DexHabitat HabitatOf(int round) => Habitats[Math.Max(0, round) % Habitats.Count];

    /// <summary>
    /// A round's wild Pokémon (ours): eight species of the Sinnoh Pokédex whose habitat is the round's, no legendary,
    /// drawn by the round alone so every floor of it has the same, at levels from 38 rising a level a round to 50, the
    /// later slots a little higher, as Emerald's tables rise (<c>sLevel50WildMons_Round*</c>).
    /// </summary>
    public static List<WildEncounterEntry> WildTable(int round)
    {
        var habitat = HabitatOf(round);
        var pool = PokemonDatabase.GetAll()
            .Where(s => s.SinnohNumber is > 0 && !s.Legendary && !s.Mythical && DexEntries.HabitatOf(s.Name) == habitat)
            .OrderBy(s => s.SinnohNumber).ToList();
        var rng = new Random(0x50594D + round);
        var chosen = new List<PokemonSpecies>();
        while (chosen.Count < 8 && pool.Count > 0)
        {
            var s = pool[rng.Next(pool.Count)];
            pool.Remove(s);
            chosen.Add(s);
        }
        int level = Math.Min(48, 38 + Math.Max(0, round));
        return chosen.Select((s, i) => new WildEncounterEntry
        {
            SpeciesName = s.Name, MinLevel = Math.Min(50, level + i / 2), MaxLevel = Math.Min(50, level + i / 2 + 2), Weight = 10
        }).ToList();
    }

    /// <summary>What a floor's finds may be (ours, a round's row rising as Emerald's pickup tables do): the first rounds' plain medicine, then stronger.</summary>
    public static IReadOnlyList<string> FindsOfTheRound(int round) => round switch
    {
        0 => new[] { "Potion", "Super Potion", "Full Heal", "Ether", "Oran Berry", "Revive" },
        1 or 2 => new[] { "Super Potion", "Hyper Potion", "Full Heal", "Ether", "Sitrus Berry", "Revive" },
        _ => new[] { "Hyper Potion", "Max Potion", "Full Restore", "Elixir", "Lum Berry", "Revive", "Max Revive" }
    };

    /// <summary>The finds of a floor, one drawn from the round's row for each place.</summary>
    public static List<string> FindsFor(int round, int count, Func<int> next16)
    {
        var row = FindsOfTheRound(round);
        return Enumerable.Range(0, count).Select(_ => row[next16() % row.Count]).ToList();
    }

    /// <summary>
    /// A find used at once on the three (ours, where Emerald puts it in the Pyramid's own bag): a medicine heals whoever
    /// has lost the most HP, a cure cures the first with a condition, a revive raises the first fainted, an Ether or an
    /// Elixir fills the move with the fewest PP left. The one it was used on, or null when none of the three needs it.
    /// </summary>
    public Pokemon? UseFind(string item)
    {
        var standing = Team.Members.Where(p => !p.IsFainted).ToList();
        Pokemon? MostHurt() => standing.Where(p => p.CurrentHP < p.MaxHP).OrderBy(p => p.CurrentHP - p.MaxHP).FirstOrDefault();
        Pokemon? Ailing() => standing.FirstOrDefault(p => p.Status != StatusCondition.None);
        Pokemon? Fallen() => Team.Members.FirstOrDefault(p => p.IsFainted);
        Pokemon? Spent() => standing.Where(p => p.Moves.Any(m => m.CurrentPP < m.MaxPP)).OrderBy(p => p.Moves.Min(m => m.CurrentPP)).FirstOrDefault();
        void Cure(Pokemon p) { p.Status = StatusCondition.None; p.SleepTurns = 0; p.ToxicCounter = 0; }
        switch (item)
        {
            case "Potion" or "Super Potion" or "Hyper Potion" or "Oran Berry" or "Sitrus Berry" or "Max Potion":
            {
                if (MostHurt() is not { } p) return null;
                int heal = item switch
                {
                    "Potion" => 20, "Super Potion" => 50, "Hyper Potion" => 200, "Oran Berry" => 10,
                    "Sitrus Berry" => Math.Max(1, p.MaxHP / 4), _ => p.MaxHP
                };
                p.CurrentHP = Math.Min(p.MaxHP, p.CurrentHP + heal);
                return p;
            }
            case "Full Restore":
            {
                if ((MostHurt() ?? Ailing()) is not { } p) return null;
                p.CurrentHP = p.MaxHP;
                Cure(p);
                return p;
            }
            case "Full Heal" or "Lum Berry":
            {
                if (Ailing() is not { } p) return null;
                Cure(p);
                return p;
            }
            case "Revive" or "Max Revive":
            {
                if (Fallen() is not { } p) return null;
                p.Revive(item == "Revive" ? p.MaxHP / 2 : p.MaxHP);
                return p;
            }
            case "Ether" or "Elixir":
            {
                if (Spent() is not { } p) return null;
                if (item == "Elixir") foreach (var m in p.Moves) m.CurrentPP = m.MaxPP;
                else
                {
                    var m = p.Moves.OrderBy(m => m.CurrentPP).First();
                    m.CurrentPP = Math.Min(m.MaxPP, m.CurrentPP + 10);
                }
                return p;
            }
            default:
                return null;
        }
    }

    /// <summary>A wild Pokémon of the floor as met: of the table's species and level drawn by the field.</summary>
    public static Pokemon WildOf(WildEncounterEntry entry, Random rng) =>
        new(PokemonDatabase.Get(entry.SpeciesName)!, rng.Next(entry.MinLevel, entry.MaxLevel + 1), rng);
}
