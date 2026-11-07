using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace DataImporter;

// The later games' machines and tutors (plan 36 · TR9; docs/mechanics/rulings.md, "The later TMs and tutors"). Every
// move a machine teaches in a game after Platinum that no machine of Platinum's (nor Hoenn's HM09 or Johto's HM10)
// teaches becomes a machine of ours, ids from MoveTeaching.LaterFrom, named on from Platinum's TM92 by the moves' order
// in the games they come from: Scarlet and Violet's first (TM93 on), then the newer games' before the older. A species
// takes one where it learns its move by a machine or from a tutor in a game after Platinum, and under the modern rules
// alone (PokemonSpecies.LaterMachines, Ruleset.LaterMachines); the same goes for Platinum's own machines whose move a
// species learns so in a later game where its own list lacks them. A training hall's tutor teaches the
// later tutors' moves no machine teaches (MoveTutors.TrainingHall, its list ours), to whoever learns them from a tutor in
// a later game (PokemonSpecies.LaterTutorMoves). Only numbers and names are read: the machines' numbers, the moves they
// teach and who learns what by which way.
public sealed partial class Importer
{
    /// <summary>
    /// The games after Platinum whose machines and tutors a species takes (PokeAPI's version groups): Scarlet and Violet,
    /// Sword and Shield, Brilliant Diamond and Shining Pearl, the four of Alola and Kalos, the four of Unova, Legends:
    /// Arceus and Let's Go. Their downloadable content is folded into the games themselves; Legends: Z-A says nothing
    /// of who learns what.
    /// </summary>
    private static readonly int[] LaterGroups = { 25, 20, 23, 18, 17, 16, 15, 14, 11, 24, 19 };

    /// <summary>The order the later machines are numbered in (decision 7): Scarlet and Violet's, then the newer games' before the older.</summary>
    private static readonly int[] MachineGroups = { 25, 20, 23, 18, 17, 16, 15, 14, 11, 19 };

    private const int LetsGo = 19;

    /// <summary>The first number of the later machines: the one after Platinum's last TM.</summary>
    public const int FirstLaterMachineNumber = 93;

    /// <summary>A later machine of ours: the item, and the version group and number of the machine of the games it was taken from.</summary>
    public sealed record LaterMachine(ItemData Item, int Group, int Number);

    private List<LaterMachine>? laterMachines;

    /// <summary>
    /// The later games' machines of ours, made and added to <paramref name="items"/> (once): every move a later game's
    /// machine teaches that no machine of Platinum's or of the regions' own HMs teaches, in the games' order, each used up
    /// as it teaches as every TM is (plan 39's decision 4: used up in every game, and made again at a PC with the crafting
    /// kit). Its price is ours, by the move's power (<see cref="MachinePrice"/>).
    /// </summary>
    public List<LaterMachine> AddLaterMachineItems(List<ItemData> items, IReadOnlyList<MoveData> moves)
    {
        if (laterMachines != null) return laterMachines;
        if (items.Any(i => i.Id >= MoveTeaching.LaterFrom && i.Id <= MoveTeaching.LaterTo))
            throw new InvalidDataException($"An item already has an id of the later machines' block ({MoveTeaching.LaterFrom} on)");
        var taught = items.Where(i => MoveTeaching.IsMachine(i) && (i.Id < 1000 || i.Id >= 9000 && i.Id < MoveTeaching.LaterFrom))
            .Select(i => i.TeachesMove!).ToHashSet(StringComparer.Ordinal);
        var byName = moves.GroupBy(m => m.Name).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        var made = new List<LaterMachine>();
        foreach (int group in MachineGroups)
            foreach (var r in api.Table("machines").Where(r => r.Int("version_group_id") == group).OrderBy(r => r.Int("machine_number")))
            {
                string move = api.MoveName(r.Int("move_id"));
                if (taught.Contains(move)) continue;
                if (!byName.TryGetValue(move, out var data))
                {
                    Console.Error.WriteLine($"  a later machine teaches {move}, which the data hasn't: left out");
                    continue;
                }
                taught.Add(move);
                int number = FirstLaterMachineNumber + made.Count;
                var item = new ItemData
                {
                    Id = MoveTeaching.LaterFrom + made.Count,
                    Name = $"TM{number:D2}",
                    Pocket = ItemPocket.TMsAndHMs,
                    EffectType = ItemEffectType.None,
                    EffectValue = 0,
                    Price = MachinePrice(data),
                    CanUseInBattle = false,
                    CanUseInOverworld = true,
                    TeachesMove = move,
                    FieldUse = "TmHm",
                    Description = LaterMachineText(move, data.Description)
                };
                made.Add(new LaterMachine(item, group, r.Int("machine_number")));
            }
        if (made.Count > 0 && made[^1].Item.Id > MoveTeaching.LaterTo)
            throw new InvalidDataException($"{made.Count} later machines run past the block's last id ({MoveTeaching.LaterTo})");
        items.AddRange(made.Select(m => m.Item));
        laterMachines = made;
        return made;
    }

    /// <summary>What a later machine says of itself: the move, that it is used up (as a TR says), then the move's own text.</summary>
    private static string LaterMachineText(string move, string? moveText) =>
        $"Teaches {move} to a compatible Pokémon, and is used up. {moveText}".TrimEnd();

    /// <summary>
    /// A later machine's price, ours, set against Platinum's own machines' (a status move 1,500 to 2,000, most hits 3,000,
    /// a move of 120 power 5,500 and one of 150 7,500): a status move 2,000, a hit under 60 power 2,000, one under 120 (and
    /// one whose power the battle works out) 3,000, one under 150 5,500, the strongest 7,500, by the newest games' numbers.
    /// </summary>
    public static int MachinePrice(MoveData move)
    {
        var category = move.Modern?.Category ?? move.Category;
        int power = move.Modern?.Power ?? move.Power;
        if (category == MoveCategory.Status) return 2000;
        if (power == 0) return 3000;
        return power < 60 ? 2000 : power < 120 ? 3000 : power < 150 ? 5500 : 7500;
    }

    /// <summary>
    /// Each species' moves by the ways of the later games (a machine, a tutor), by PokeAPI's id of its own default form,
    /// across <see cref="LaterGroups"/>.
    /// </summary>
    private Dictionary<int, (HashSet<string> Machine, HashSet<string> Tutor)> LaterWays()
    {
        if (laterWays != null) return laterWays;
        var groups = LaterGroups.ToHashSet();
        laterWays = new();
        foreach (var r in api.Table("pokemon_moves"))
        {
            int method = r.Int("pokemon_move_method_id");
            if (method is not (3 or 4) || !groups.Contains(r.Int("version_group_id"))) continue;
            int pokemon = r.Int("pokemon_id");
            if (!laterWays.TryGetValue(pokemon, out var ways)) laterWays[pokemon] = ways = (new(StringComparer.Ordinal), new(StringComparer.Ordinal));
            (method == 4 ? ways.Machine : ways.Tutor).Add(api.MoveName(r.Int("move_id")));
        }
        return laterWays;
    }

    private Dictionary<int, (HashSet<string> Machine, HashSet<string> Tutor)>? laterWays;

    /// <summary>
    /// Every species' <see cref="PokemonSpecies.LaterMachines"/> and <see cref="PokemonSpecies.LaterTutorMoves"/>: the
    /// later machines of ours and Platinum's own machines (where its own list lacks them) whose move it learns by a
    /// machine or from a tutor in a later game, by the machines' order, and the training hall's
    /// tutor's moves it learns from a tutor in one, by the tutor's order. A species of ours takes its forebear's
    /// afterwards (<see cref="SpeciesPastHundred.CopyMachines"/>).
    /// </summary>
    public void AddLaterCompatibility(List<PokemonSpecies> species, List<ItemData> items)
    {
        var ways = LaterWays();
        var machines = items.Where(i => MoveTeaching.IsMachine(i) && (i.Id < 1000 || MoveTeaching.IsLater(i))).OrderBy(i => i.Id).ToList();
        var hall = MoveTutors.At(MoveTutors.TrainingHall).Select(m => m.Move).ToList();
        foreach (var s in species.Where(s => !s.IsOurs))
        {
            if (!ways.TryGetValue(s.DexNumber, out var learned))
            {
                s.LaterMachines = null;
                s.LaterTutorMoves = null;
                continue;
            }
            var own = (s.TmMoves ?? new()).ToHashSet(StringComparer.Ordinal);
            var later = machines.Where(m => !own.Contains(m.Name) && (learned.Machine.Contains(m.TeachesMove!) || learned.Tutor.Contains(m.TeachesMove!)))
                .Select(m => m.Name).ToList();
            s.LaterMachines = later.Count > 0 ? later : null;
            var tutored = hall.Where(learned.Tutor.Contains).ToList();
            s.LaterTutorMoves = tutored.Count > 0 ? tutored : null;
        }
    }

    /// <summary>
    /// A training hall's machine shelf (plan 36 · TR9): the later machines of ours in their order, the specialty
    /// <c>training_hall_machines</c> its clerk sells once a Hall of Fame is entered (<c>common.TrainingClerk</c>).
    /// </summary>
    public const string MachineShelf = "training_hall_machines";

    public static void AddMachineShelf(MartData marts, IEnumerable<ItemData> items) =>
        marts.Specialties[MachineShelf] = items.Where(MoveTeaching.IsLater).OrderBy(i => i.Id).Select(i => i.Name).ToList();

    /// <summary>
    /// What the later tutors teach that no machine of this game teaches (every later game's tutor moves but Let's Go's,
    /// whose are its partners' alone), to write a training hall's tutor's list from, newest game first, in each game's
    /// order of the moves' names. Only a species' own form is asked, since a form takes its species' lists (Legends:
    /// Arceus's Power Shift, two of Hisui's forms' alone, is no species').
    /// </summary>
    public List<string> LaterTutorCandidates(IEnumerable<ItemData> items)
    {
        var machineMoves = items.Where(MoveTeaching.IsMachine).Where(i => i.Id < 1000 || i.Id >= 9000).Select(i => i.TeachesMove!).ToHashSet(StringComparer.Ordinal);
        var found = new List<string>();
        foreach (int group in LaterGroups.Where(g => g != LetsGo))
            foreach (var move in api.Table("pokemon_moves")
                         .Where(r => r.Int("version_group_id") == group && r.Int("pokemon_move_method_id") == 3 && r.Int("pokemon_id") < FirstAlternateForm)
                         .Select(r => api.MoveName(r.Int("move_id"))).Distinct().OrderBy(m => m, StringComparer.Ordinal))
                if (!machineMoves.Contains(move) && !found.Contains(move)) found.Add(move);
        return found;
    }

    /// <summary>
    /// The check that every move a species of the games can learn by the modern rules has a way to be taught it here
    /// (plan 36 · TR9): its newest game's (<see cref="LearnsetVersionGroups"/>, as its learnset) moves by a machine or from
    /// a tutor, each held to a machine it takes (its own or a later one), a tutor's list it is on (Platinum's, a training
    /// hall's, Grandma Wilma's Draco Meteor for a Dragon type, the old man's strongest moves for a starter fully grown),
    /// its learnset or its Egg moves. Returns what nothing teaches, a line to a species; the importer prints it.
    /// </summary>
    public List<string> LaterGaps(IReadOnlyList<PokemonSpecies> species, IReadOnlyList<ItemData> items)
    {
        var teaches = items.Where(MoveTeaching.IsMachine).GroupBy(i => i.Name).ToDictionary(g => g.Key, g => g.First().TeachesMove!);
        var gaps = new List<string>();
        var rows = api.Table("pokemon_moves").Where(r => r.Int("pokemon_move_method_id") is 1 or 3 or 4)
            .GroupBy(r => r.Int("pokemon_id")).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var s in species.Where(s => !s.IsOurs))
        {
            if (!rows.TryGetValue(s.DexNumber, out var own)) continue;
            int? newest = LearnsetVersionGroups.Cast<int?>().FirstOrDefault(vg => own.Any(r => r.Int("version_group_id") == vg && r.Int("pokemon_move_method_id") == 1));
            if (newest == null) continue;
            var asked = own.Where(r => r.Int("version_group_id") == newest && r.Int("pokemon_move_method_id") is 3 or 4)
                .Select(r => api.MoveName(r.Int("move_id"))).Distinct().ToList();
            var ways = new HashSet<string>(StringComparer.Ordinal);
            foreach (var m in (s.TmMoves ?? new()).Concat(s.LaterMachines ?? new())) if (teaches.TryGetValue(m, out var move)) ways.Add(move);
            foreach (var m in (s.TutorMoves ?? new()).Concat(s.LaterTutorMoves ?? new())) ways.Add(m);
            foreach (var m in s.Learnset.Concat(s.Modern?.Learnset ?? new())) ways.Add(m.MoveName);
            foreach (var m in s.EggMoves ?? new()) ways.Add(m);
            if (s.PrimaryType == PokemonType.Dragon || s.SecondaryType == PokemonType.Dragon) ways.Add(MoveTutors.DracoMeteor);
            if (MoveTutors.UltimateMoveOf(s.Name) is { } ultimate) ways.Add(ultimate);
            var missing = asked.Where(m => !ways.Contains(m)).ToList();
            if (missing.Count > 0) gaps.Add($"{s.Name} (game {newest}): {string.Join(", ", missing)}");
        }
        return gaps;
    }
}
