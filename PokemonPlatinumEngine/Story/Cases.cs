using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Story;

/// <summary>A clue of a case (plan 41 · EG7): its key (its words under <c>cases.&lt;id&gt;.clue.&lt;key&gt;</c>) and the flag set where it is found.</summary>
public sealed record CaseClue(string Key, string Flag);

/// <summary>A deduction of a case: the clue that answers its question (<c>cases.&lt;id&gt;.deduce.&lt;n&gt;</c>).</summary>
public sealed record CaseDeduction(string Answer);

/// <summary>
/// A case of the Open Door Agency (plan 41 · EG7, EG8; <c>docs/mechanics/rulings.md</c>, "The agency's cases"): Looker's,
/// set in a finished region's post-game places, its clues found where they lie and its deductions asked at his desk
/// with <c>deduce &lt;case&gt; &lt;n&gt;</c>. It is also a request of plan 12 · Q21 (<see cref="Requests.All"/>, under
/// the id <c>case_&lt;id&gt;</c>), so the requests page lists it and pins its next clue.
/// </summary>
public sealed record Case(string Id, string Region, IReadOnlyList<CaseClue> Clues, IReadOnlyList<CaseDeduction> Deductions)
{
    /// <summary>The flag the case is taken by at Looker's desk.</summary>
    public string TakenFlag => $"FLAG_CASE_{Id.ToUpperInvariant()}_TAKEN";

    /// <summary>The variable of the deductions made: 0 none, 1 the first, 2 both.</summary>
    public string DeducedVariable => $"VAR_CASE_{Id.ToUpperInvariant()}_DEDUCED";

    /// <summary>The flag of the culprit beaten.</summary>
    public string CulpritFlag => $"FLAG_CASE_{Id.ToUpperInvariant()}_CULPRIT";

    /// <summary>The flag of the case closed and paid.</summary>
    public string ClosedFlag => $"FLAG_CASE_{Id.ToUpperInvariant()}_CLOSED";

    /// <summary>The request the case is on the requests page.</summary>
    public string RequestId => $"case_{Id}";

    /// <summary>The clues found in this story, in the case's order.</summary>
    public IReadOnlyList<CaseClue> Found(StoryState story) => Clues.Where(c => story.Has(c.Flag)).ToList();
}

/// <summary>The agency's cases (plan 41 · EG7, EG8), GPU-free: what a clue is and which answers a deduction.</summary>
public static class Cases
{
    private static CaseClue Clue(string id, string key) => new(key, $"FLAG_CASE_{id.ToUpperInvariant()}_CLUE_{key.ToUpperInvariant()}");

    /// <summary>Every case, in the order the regions are finished along the chain.</summary>
    public static IReadOnlyList<Case> All { get; } = new[]
    {
        // Sinnoh's (EG7, the sheet's 7a): the Villa Larder. Food vanishes from the Resort Area's villa larder before each
        // party; the young Gible blamed for it never bit the crates, the dates on them are the ferry's, and the ferry's log
        // has a gap where its captain sailed off his course
        new Case("sinnoh", RegionDatabase.Sinnoh, new[]
        {
            Clue("sinnoh", "label"), Clue("sinnoh", "bite"), Clue("sinnoh", "log"), Clue("sinnoh", "receipt"),
        }, new[] { new CaseDeduction("bite"), new CaseDeduction("log") }),
        // Kanto's (EG8): the Ferry's Extra Passenger. A seat on the Sevii ferry is paid for on every sailing and always
        // empty; the stub under it was torn by no ticket office's hand, a duffel nobody owned came off at Two Island, and
        // the old fisherman on Vermilion's quay sees the dockhand wave one last traveller up the cargo ramp after the count
        new Case("kanto", RegionDatabase.Kanto, new[]
        {
            Clue("kanto", "stub"), Clue("kanto", "duffel"), Clue("kanto", "remark"),
        }, new[] { new CaseDeduction("stub"), new CaseDeduction("remark") }),
        // Johto's (EG8): the Alph Etchings. Chalk copies of the wall appear at the Ruins of Alph overnight; they are labelled
        // letter by letter like a blackboard, lanterns walk the ruins in a line after dark, and a pupil's teacher comes to
        // class with chalk on her sleeves and a quiz on the Unown
        new Case("johto", RegionDatabase.Johto, new[]
        {
            Clue("johto", "chalk"), Clue("johto", "lanterns"), Clue("johto", "dust"),
        }, new[] { new CaseDeduction("chalk"), new CaseDeduction("dust") }),
        // Hoenn's (EG8): the Salt Run. Crates marked "salt" come in to Dewford light as air; their customs seal carries a
        // second stamp, the crew's song is about lamps, and the skipper's boat comes in when the tide table says the customs
        // hut is shut
        new Case("hoenn", RegionDatabase.Hoenn, new[]
        {
            Clue("hoenn", "seal"), Clue("hoenn", "song"), Clue("hoenn", "tide"),
        }, new[] { new CaseDeduction("seal"), new CaseDeduction("song") }),
        // Unova's (EG8): the Double-Booked Hall. Two tournaments on one afternoon at the World Tournament; both schedules
        // are stamped alike, one booking has a name rubbed out under it, and the janitor's key log has the clerk in the
        // office after hours
        new Case("unova", RegionDatabase.Unova, new[]
        {
            Clue("unova", "schedule"), Clue("unova", "smudge"), Clue("unova", "keys"),
        }, new[] { new CaseDeduction("smudge"), new CaseDeduction("keys") }),
        // Kalos's (EG8, an outline of ours not yet approved: plan 41's end): the Borrowed Masterpiece. A painting of
        // Lumiose's museum leaves its frame every night and is back by morning; the frame's screws are worn bright, a lady
        // with a long flat parcel takes the last train to Kiloude every night, and a painter there smells fresh varnish
        new Case("kalos", RegionDatabase.Kalos, new[]
        {
            Clue("kalos", "screws"), Clue("kalos", "ticket"), Clue("kalos", "varnish"),
        }, new[] { new CaseDeduction("screws"), new CaseDeduction("ticket") }),
    };

    private static readonly Dictionary<string, Case> ById = All.ToDictionary(c => c.Id, StringComparer.Ordinal);

    /// <summary>A case by its id; null for none.</summary>
    public static Case? Find(string id) => ById.GetValueOrDefault(id);

    /// <summary>
    /// The answers <c>deduce</c> offers for a case's deduction: the clues found, in the case's order. Its RESULT is 1
    /// where the clue chosen is the one that answers, 0 for any other (and for none chosen).
    /// </summary>
    public static IReadOnlyList<CaseClue> Answers(Case @case, StoryState story) => @case.Found(story);

    /// <summary>Whether the clue at <paramref name="answer"/> of the answers offered answers the deduction <paramref name="n"/> (from 1).</summary>
    public static bool Answers(Case @case, int n, StoryState story, int answer)
    {
        var offered = Answers(@case, story);
        return n >= 1 && n <= @case.Deductions.Count && answer >= 0 && answer < offered.Count && offered[answer].Key == @case.Deductions[n - 1].Answer;
    }

    /// <summary>The case's request (plan 12 · Q21): taken at Looker's desk, its clues in order, its deductions, the culprit and its close.</summary>
    public static Request RequestOf(Case @case)
    {
        var steps = @case.Clues.Select(c => new RequestStep($"clue_{c.Key}", $"flag {c.Flag}")).ToList();
        steps.Add(new RequestStep("deduce", $"var {@case.DeducedVariable} >= {@case.Deductions.Count}"));
        steps.Add(new RequestStep("culprit", $"flag {@case.CulpritFlag}"));
        steps.Add(new RequestStep("closed", $"flag {@case.ClosedFlag}"));
        return new Request(@case.RequestId, RegionDatabase.Hub, "Hub_Office", "looker", $"flag {@case.TakenFlag}", steps);
    }
}
