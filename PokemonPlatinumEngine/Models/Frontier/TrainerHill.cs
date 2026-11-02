using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Models.Frontier;

/// <summary>Emerald's four challenges of the Trainer Hill (<c>HILL_MODE_*</c>), in its order.</summary>
public enum TrainerHillMode { Normal, Variety, Unique, Expert }

/// <summary>
/// Emerald's Trainer Hill on Route 111 (plan 21, part l; <c>docs/mechanics/rulings.md</c>, "The Trainer Hill"), as the
/// Battle Subway lends the Frontier's rules: a challenge of four floors of two trainers each climbed against the clock to
/// the owner on the roof, Emerald's teams (<see cref="TrainerHillDatabase"/>) brought up to the highest level of the
/// player's team (<c>SetTrainerHillMonLevel</c> with <c>GetHighestLevelInPlayerParty</c>), fought as a battle of the
/// Frontier's on the player's own team (no EXP, no money, nothing seen in the Pokédex, RUN a forfeit), whose damage
/// stays from floor to floor until the counter heals it. A trainer who sees the player while the other of the floor
/// sees them too brings both into a double battle (<c>FillHillTrainersParties</c>). The prize on the roof is
/// Emerald's (<c>GetPrizeItemId</c>) and so is the clock's limit. What a challenge keeps is the story's own variables
/// (<see cref="Vars"/>), so the floor a game was saved on is laid again from them and nothing new is saved. No drawing or input.
/// </summary>
public static class TrainerHill
{
    /// <summary>Floors to a challenge; the roof is the fifth.</summary>
    public const int Floors = 4, Roof = 5;

    /// <summary>Emerald's longest time, in frames of sixty a second (<c>HILL_MAX_TIME</c>: an hour less a frame).</summary>
    public const int MaxFrames = 60 * 60 * 60 - 1;

    /// <summary>The story's variables a challenge keeps its state in.</summary>
    public static class Vars
    {
        /// <summary>0 while no challenge is under way, 1 climbing, 2 lost, 3 at the roof (<see cref="States"/>).</summary>
        public const string State = "VAR_HOENN_TRAINER_HILL_STATE";
        /// <summary>The challenge taken, as <see cref="TrainerHillMode"/>.</summary>
        public const string Mode = "VAR_HOENN_TRAINER_HILL_MODE";
        /// <summary>The floor the player is on, 1 to 4, or 5 on the roof.</summary>
        public const string Floor = "VAR_HOENN_TRAINER_HILL_FLOOR";
        /// <summary>The play time in seconds as the challenge began, which the clock counts from.</summary>
        public const string Start = "VAR_HOENN_TRAINER_HILL_START";
        /// <summary>The time the roof was reached in, in frames.</summary>
        public const string Time = "VAR_HOENN_TRAINER_HILL_TIME";
        /// <summary>The trainers beaten, a bit to each (floor - 1) * 2 + trainer.</summary>
        public const string Beaten = "VAR_HOENN_TRAINER_HILL_BEATEN";
        /// <summary>1 once the owner has given the challenge's prize (<c>receivedPrize</c>).</summary>
        public const string Prize = "VAR_HOENN_TRAINER_HILL_PRIZE";
        /// <summary>1 once the owner has held the time against the best (<c>checkedFinalTime</c>).</summary>
        public const string Checked = "VAR_HOENN_TRAINER_HILL_CHECKED";
        /// <summary>The best time of each challenge in frames; 0 for none yet (<c>trainerHillTimes</c>).</summary>
        public static string Best(TrainerHillMode mode) => $"VAR_HOENN_TRAINER_HILL_BEST_{mode.ToString().ToUpperInvariant()}";
    }

    /// <summary>The values of <see cref="Vars.State"/>.</summary>
    public static class States
    {
        public const int None = 0, Climbing = 1, Lost = 2, Reached = 3;
    }

    public static TrainerHillMode ModeOf(StoryState story) => (TrainerHillMode)Math.Clamp(story.Var(Vars.Mode), 0, 3);

    /// <summary>Whether a challenge is being climbed (the floors and the roof).</summary>
    public static bool Climbing(StoryState story) => story.Var(Vars.State) == States.Climbing;

    /// <summary>A challenge begun: the clock started from the play time, every trainer of it unbeaten, the first floor next (<c>TrainerHillStartChallenge</c>).</summary>
    public static void Begin(StoryState story, TrainerHillMode mode, int playSeconds)
    {
        story.SetVar(Vars.State, States.Climbing);
        story.SetVar(Vars.Mode, (int)mode);
        story.SetVar(Vars.Floor, 1);
        story.SetVar(Vars.Start, playSeconds);
        story.SetVar(Vars.Time, 0);
        story.SetVar(Vars.Beaten, 0);
        story.SetVar(Vars.Prize, 0);
        story.SetVar(Vars.Checked, 0);
    }

    /// <summary>The time the clock shows: frames since the challenge began, up to Emerald's limit.</summary>
    public static int Frames(StoryState story, int playSeconds) =>
        story.Var(Vars.State) == States.Reached ? story.Var(Vars.Time)
        : (int)Math.Clamp((long)(playSeconds - story.Var(Vars.Start)) * 60, 0, MaxFrames);

    /// <summary>A trainer of the floor beaten, by their place on it (0 or 1).</summary>
    public static bool IsBeaten(StoryState story, int floor, int trainer) => (story.Var(Vars.Beaten) >> ((floor - 1) * 2 + trainer) & 1) != 0;

    public static void Beat(StoryState story, int floor, int trainer) =>
        story.SetVar(Vars.Beaten, story.Var(Vars.Beaten) | 1 << ((floor - 1) * 2 + trainer));

    /// <summary>The stairs climbed: the next floor, or the roof after the fourth, where the clock stops. True at the roof.</summary>
    public static bool Climb(StoryState story, int playSeconds)
    {
        int floor = Math.Min(Roof, story.Var(Vars.Floor) + 1);
        story.SetVar(Vars.Floor, floor);
        if (floor < Roof) return false;
        story.SetVar(Vars.Time, Frames(story, playSeconds));
        story.SetVar(Vars.State, States.Reached);
        return true;
    }

    /// <summary>The level Emerald brings every Hill trainer's Pokémon to: the highest of the player's team, Eggs passed over.</summary>
    public static int LevelFor(Party party) =>
        Math.Clamp(party.Members.Where(p => !p.IsEgg).Select(p => p.Level).DefaultIfEmpty(1).Max(), 1, Ruleset.HighestLevel);

    /// <summary>A trainer of a floor (0 or 1) as a battle takes them: their three at the level, no prize money, the Frontier's three routines.</summary>
    public static Trainer TrainerOf(TrainerHillMode mode, int floor, int trainer, int level)
    {
        var record = TrainerHillDatabase.Floor((int)mode, floor).Trainers[trainer];
        var made = new Trainer
        {
            Id = $"trainer_hill_{record.Number}",
            Name = record.Name,
            TrainerClass = record.Class,
            PrizeMoney = 0,
            // GetTrainerHillAIFlags (unused in Emerald, which leaves the Hill's trainers the Frontier's): bad moves, a faint, viability
            Ai = FrontierTrainers.Mind,
            Look = CharacterStyles.Default.CharacterFor(record.Look),
            Items = new List<string>()
        };
        foreach (var mon in record.Team) made.Party.Add(Build(mon, level));
        return made;
    }

    /// <summary>
    /// One of Emerald's sets as a Pokémon (<c>CreateBattleTowerMon</c>, then <c>SetTrainerHillMonLevel</c>): its own
    /// IVs and effort, its ability by the set's number, its personality's nature and gender, full friendship, never shiny.
    /// </summary>
    public static Pokemon Build(TrainerHillMon set, int level)
    {
        var species = PokemonDatabase.Get(set.Species) ?? throw new InvalidOperationException($"No species called {set.Species}");
        var pokemon = new Pokemon(species, level, set.Personality, new Random(0))
        {
            IsShiny = false,
            IvHP = set.Ivs[0], IvAttack = set.Ivs[1], IvDefense = set.Ivs[2], IvSpeed = set.Ivs[3], IvSpAttack = set.Ivs[4], IvSpDefense = set.Ivs[5],
            EvHP = set.Evs[0], EvAttack = set.Evs[1], EvDefense = set.Evs[2], EvSpeed = set.Evs[3], EvSpAttack = set.Evs[4], EvSpDefense = set.Evs[5]
        };
        var abilities = pokemon.Abilities;
        if (abilities.Count > 0) pokemon.AbilityName = abilities[Math.Min(set.AbilityNum, abilities.Count - 1)];
        pokemon.HeldItem = set.Item != null ? ItemDatabase.Get(set.Item) : null;
        pokemon.Moves.Clear();
        foreach (string move in set.Moves) pokemon.Moves.Add(MoveDatabase.Create(move));
        pokemon.Friendship = 255;
        pokemon.RecalculateStats();
        pokemon.CurrentHP = pokemon.MaxHP;
        return pokemon;
    }

    /// <summary>
    /// The prize the owner gives (<c>GetPrizeItemId</c>): a set by the sum of the challenge's trainer numbers, a list
    /// by their low five bits XORed (Expert one list on), and a place in it by the minutes the climb took. A Champion
    /// alone climbs, so the lists with a machine are always the ones drawn from.
    /// </summary>
    public static string PrizeFor(TrainerHillMode mode, int frames)
    {
        var numbers = TrainerHillDatabase.TrainersOf((int)mode).Select(t => t.Number).ToList();
        int set = numbers.Sum() / 256 % TrainerHillDatabase.PrizeSets.Count;
        int list = numbers.Aggregate(0, (x, n) => x ^ (n & 0x1F)) % 10;
        if (mode == TrainerHillMode.Expert) list = (list + 1) % 10;
        int minutes = frames / 3600;
        int place = minutes < 12 ? 0 : minutes < 13 ? 1 : minutes < 14 ? 2 : minutes < 16 ? 3 : minutes < 18 ? 4 : 5;
        return TrainerHillDatabase.PrizeSets[set][list][place];
    }

    /// <summary>
    /// The time held against the challenge's best (<c>CheckFinalTime</c>): 0 a new record (kept), 1 not, 2 already
    /// held once this climb.
    /// </summary>
    public static int CheckTime(StoryState story)
    {
        if (story.Var(Vars.Checked) == 1) return 2;
        story.SetVar(Vars.Checked, 1);
        var mode = ModeOf(story);
        int time = story.Var(Vars.Time), best = story.Var(Vars.Best(mode));
        if (best != 0 && best <= time) return 1;
        story.SetVar(Vars.Best(mode), Math.Max(1, time));
        return 0;
    }

    /// <summary>A time in frames as the owner and the board say it.</summary>
    public static string TimeOf(int frames)
    {
        frames = Math.Clamp(frames, 0, MaxFrames);
        return Core.Text.Of("trainerHill.time", ("minutes", frames / 3600), ("seconds", $"{frames % 3600 / 60:00}"), ("hundredths", $"{frames % 60 * 100 / 60:00}"));
    }
}

/// <summary>
/// A floor of the Trainer Hill as a map of ours (plan 21, part l): Emerald's sixteen by sixteen of collision under a
/// strip of five rows whose middle is walled off, the way up from below on its west side and the stairs up on its
/// east, so the climb goes through the floor (Emerald's floor is sixteen by twenty-one, the first five rows always the
/// same, <c>GenerateTrainerHillFloorLayout</c>). The floor's two trainers stand on their places, facing their ways and
/// seeing as far as Emerald says, those beaten already standing aside. Laid from the story's variables alone, so the
/// same climb lays the same floor every time. No drawing or input.
/// </summary>
public static class TrainerHillFloor
{
    /// <summary>The map every floor is laid on (<c>Data/maps/TrainerHillFloor.json</c> is the floor before any challenge).</summary>
    public const string MapName = "TrainerHillFloor";

    /// <summary>The floor's first tile on the map, the strip's rows and the floor's side; the map's size round them.</summary>
    public const int Left = 1, Top = 3, Strip = 5, Side = 16;
    public const int Width = Side + 2, Height = Top + Strip + Side + 1;

    /// <summary>Where the way down lies, where one comes up onto the floor, the stairs up and where one comes down beside them.</summary>
    public static (int X, int Y) WayDown => (Left + 2, Top);
    public static (int X, int Y) Arrival => (Left + 2, Top + 1);
    public static (int X, int Y) Stairs => (Left + 12, Top);
    public static (int X, int Y) BesideTheStairs => (Left + 12, Top + 1);

    /// <summary>The strip's wall between its two sides (Emerald's floors all begin with it in their first row).</summary>
    public const int WallFrom = 6, WallTo = 8;

    /// <summary>A floor person's key: <c>hill_trainer_1</c> or <c>hill_trainer_2</c>.</summary>
    public static string TrainerKey(int trainer) => $"hill_trainer_{trainer + 1}";

    /// <summary>The trainer someone of the floor is (0 or 1); null for anyone else.</summary>
    public static int? TrainerOf(NPC? person) =>
        person?.Key is { } key && key.StartsWith("hill_trainer_") && int.TryParse(key["hill_trainer_".Length..], out int n) && n is 1 or 2 ? n - 1 : null;

    /// <summary>Whether a tile of the map is open floor, by the challenge's floor (a ledge is not: it is hopped over).</summary>
    public static bool IsOpen(TrainerHillFloorRecord floor, int x, int y) => CodeAt(floor, x, y) == '.';

    /// <summary>Whether a tile is a ledge hopped down to the south (Emerald's <c>MB_JUMP_SOUTH</c>, the Hill's only kind).</summary>
    public static bool IsLedge(TrainerHillFloorRecord floor, int x, int y) => CodeAt(floor, x, y) == 'v';

    // '.' open, '#' blocked, 'v' a ledge, by the strip's wall above the floor's sixteen rows
    private static char CodeAt(TrainerHillFloorRecord floor, int x, int y)
    {
        if (x < Left || x >= Left + Side || y < Top || y >= Top + Strip + Side) return '#';
        int fx = x - Left;
        if (y < Top + Strip) return fx < WallFrom || fx > WallTo ? '.' : '#';
        return floor.Rows[y - Top - Strip][fx];
    }

    /// <summary>Where a trainer of the floor stands on the map.</summary>
    public static (int X, int Y) SpotOf(TrainerHillSpot spot) => (Left + spot.X, Top + Strip + spot.Y);

    /// <summary>Whether a trainer standing on their spot sees a tile: along the way they face, within their range, over open floor.</summary>
    public static bool Sees(TrainerHillFloorRecord floor, TrainerHillSpot spot, int x, int y)
    {
        var (sx, sy) = SpotOf(spot);
        var (dx, dy) = spot.Facing switch { Direction.Up => (0, -1), Direction.Down => (0, 1), Direction.Left => (-1, 0), _ => (1, 0) };
        for (int step = 1; step <= spot.Range; step++)
        {
            int tx = sx + dx * step, ty = sy + dy * step;
            if (!IsOpen(floor, tx, ty)) return false;
            if (tx == x && ty == y) return true;
        }
        return false;
    }

    /// <summary>Every open tile walked to from a tile by the four ways, or hopped to south over a ledge, past none of the tiles named.</summary>
    public static HashSet<(int X, int Y)> Reachable(TrainerHillFloorRecord floor, (int X, int Y) from, IReadOnlySet<(int, int)> blocked)
    {
        var seen = new HashSet<(int X, int Y)> { from };
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue(from);
        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            foreach (var (dx, dy) in new[] { (0, -1), (0, 1), (-1, 0), (1, 0) })
            {
                var next = (x + dx, y + dy);
                // A ledge south of the tile is hopped over onto the tile beyond it
                if (dy == 1 && IsLedge(floor, next.Item1, next.Item2)) next = (x, y + 2);
                if (!IsOpen(floor, next.Item1, next.Item2) || blocked.Contains(next) || !seen.Add(next)) continue;
                queue.Enqueue(next);
            }
        }
        return seen;
    }

    /// <summary>The floor as a map's file: a hall's floor and its partitions, the way down and the stairs up the floor's script's triggers.</summary>
    public static MapFile ToFile(TrainerHillFloorRecord floor)
    {
        var ground = new List<string>();
        var solid = new List<string>();
        for (int y = 0; y < Height; y++)
        {
            var g = new char[Width];
            var s = new char[Width];
            for (int x = 0; x < Width; x++)
            {
                bool open = IsOpen(floor, x, y), ledge = IsLedge(floor, x, y);
                g[x] = (x, y) == Stairs || (x, y) == WayDown ? 's' : ledge ? 'v' : open ? '_' : '#';
                s[x] = open || ledge ? '.' : '#';
            }
            ground.Add(new string(g));
            solid.Add(new string(s));
        }
        return new MapFile
        {
            Name = MapName,
            DisplayName = "Trainer Hill",
            BgmTrack = "sinnoh/veilstone",
            Interior = InteriorStyle.Gym,
            BattleArena = BattleArena.Gym,
            Width = Width,
            Height = Height,
            Ground = ground,
            Solid = solid,
            Triggers = new List<MapFile.TriggerRecord>
            {
                new() { X = Stairs.X, Y = Stairs.Y, Script = "Stairs" },
                new() { X = WayDown.X, Y = WayDown.Y, Script = "Down" }
            }
        };
    }

    /// <summary>
    /// The floor of the climb under way made a map: its two trainers stood on their places with their looks, sight and
    /// script, those beaten already marked so (they speak and battle no more). Null while nothing is being climbed.
    /// </summary>
    public static Map? Make(StoryState story)
    {
        if (!TrainerHill.Climbing(story)) return null;
        var mode = TrainerHill.ModeOf(story);
        int number = Math.Clamp(story.Var(TrainerHill.Vars.Floor), 1, TrainerHill.Floors);
        var floor = TrainerHillDatabase.Floor((int)mode, number);
        var map = ToFile(floor).ToMap();
        for (int t = 0; t < floor.Trainers.Count; t++)
        {
            var record = floor.Trainers[t];
            var (x, y) = SpotOf(floor.Spots[t]);
            string look = CharacterStyles.Default.CharacterFor(record.Look);
            map.Add(new NPC
            {
                Name = $"{record.Class} {record.Name}",
                NpcType = look,
                GridX = x, GridY = y, Facing = floor.Spots[t].Facing,
                Key = TrainerKey(t),
                LinesPlace = MapName,
                LinesKey = TrainerKey(t),
                Script = "Trainer",
                IsTrainer = true,
                HasBattled = TrainerHill.IsBeaten(story, number, t),
                TrainerData = new Trainer
                {
                    Id = $"trainer_hill_{record.Number}", Name = record.Name, TrainerClass = record.Class, PrizeMoney = 0,
                    SightRange = floor.Spots[t].Range, Ai = FrontierTrainers.Mind, Look = look
                }
            });
        }
        return map;
    }
}
