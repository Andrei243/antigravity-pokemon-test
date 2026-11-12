using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Story;

/// <summary>Where a request stands in a game: not yet heard of, under way at its next step, or done.</summary>
public enum RequestState { Unknown, Open, Done }

/// <summary>
/// One step of a request (plan 12 · Q21): its name (the key of its line under <c>requests.&lt;id&gt;.step</c>), what says it
/// is done, written as an <c>if</c> condition is (<c>flag FLAG_X</c>, <c>var VAR_X &gt;= 2</c>), and the person of the
/// request's place it is taken to, whom a bubble picks out while the request is pinned (null for the giver).
/// </summary>
public sealed record RequestStep(string Name, string Condition, string? Person = null);

/// <summary>
/// An errand of the game (plan 12 · Q21): an id (what <c>request &lt;id&gt; &lt;step&gt;</c> names), the region and the
/// area whose script file its giver lives in, the giver's key on that map, what makes it known before any step is
/// taken (a condition, or null for "once a step is done"), and its steps in the order the page lists them. Its title,
/// giver, place and reward are words of <c>ui.json</c> under <c>requests.&lt;id&gt;</c>.
/// </summary>
public sealed record Request(string Id, string Region, string Place, string Giver, string? Known, IReadOnlyList<RequestStep> Steps)
{
    /// <summary>Its title in the language chosen.</summary>
    public string Title { get { string id = Id; return Core.Text.Of($"requests.{id}.title"); } }

    /// <summary>Who asked, in words ("The Pokétch Company's president").</summary>
    public string GiverName { get { string id = Id; return Core.Text.Of($"requests.{id}.giver"); } }

    /// <summary>Where they are, in words.</summary>
    public string PlaceName { get { string id = Id; return Core.Text.Of($"requests.{id}.place"); } }

    /// <summary>What it brings, in words.</summary>
    public string Reward { get { string id = Id; return Core.Text.Of($"requests.{id}.reward"); } }

    /// <summary>A step's line, with the player's and the rival's names in.</summary>
    public string StepLine(RequestStep step)
    {
        string id = Id, name = step.Name;
        return Core.PlayerIdentity.Fill(Core.Text.Of($"requests.{id}.step.{name}"));
    }
}

/// <summary>
/// The requests (plan 12 · Q21; GPU-free): every errand the game has, each worked out from the story whenever it is
/// asked, so the save gains no field and an older save needs no migration. A step is done when its condition holds,
/// and every step is done once the last one is (a save that has the reward has done the errand, whatever it kept of the
/// way). Where no flag of the original marks a step, a script marks it with <c>request &lt;id&gt; &lt;step&gt;</c>, which
/// stores the step's number in <see cref="Variable"/>, and the step's condition asks that variable.
/// <para>
/// A session that brings an errand adds its entry here, with its words in <c>ui.json</c> (and Romanian's), and the
/// tests hold every flag and variable it names to something a script sets.
/// </para>
/// </summary>
public static class Requests
{
    /// <summary>The story variable <c>request &lt;id&gt; &lt;step&gt;</c> sets for a request.</summary>
    public static string Variable(string id) => $"VAR_REQUEST_{id.ToUpperInvariant()}";

    /// <summary>Every request, in the order the page lists them.</summary>
    public static IReadOnlyList<Request> All { get; } = new[]
    {
        // Plan 02 · S5: the Pokétch Company's president stops the player in Jubilife City (VAR_POKETCH_CAMPAIGN_STATE 2)
        // and sends them to the three clowns, each with a quiz and a coupon; the three coupons bring the Pokétch
        new Request("poketch", RegionDatabase.Sinnoh, "jubilife_city", "poketch_co_president", "var VAR_POKETCH_CAMPAIGN_STATE >= 2", new[]
        {
            new RequestStep("coupon1", "flag FLAG_RECEIVED_COUPON_1", "clown_1"),
            new RequestStep("coupon2", "flag FLAG_RECEIVED_COUPON_2", "clown_2"),
            new RequestStep("coupon3", "flag FLAG_RECEIVED_COUPON_3", "clown_3"),
            new RequestStep("president", "flag FLAG_RECEIVED_POKETCH"),
        }),
    }.Concat(LockworkCells()).Concat(Cases.All.Select(Cases.RequestOf)).ToArray();

    /// <summary>
    /// Lockwork's cells (plan 44 · SD5): one request each, known once the region's Hall of Fame is entered (the agents come
    /// out then), done when the cell's admin is beaten; Unova's has the head and the machine as a second step.
    /// </summary>
    private static IEnumerable<Request> LockworkCells()
    {
        (string Region, string Key, string Place, string Doorman)[] cells =
        {
            (RegionDatabase.Sinnoh, "sinnoh", "fight_area", "lockwork_doorman"),
            (RegionDatabase.Kanto, "kanto", "vermilion_city", "lockwork_doorman"),
            (RegionDatabase.Johto, "johto", "olivine", "lockwork_doorman"),
            (RegionDatabase.Hoenn, "hoenn", "lilycove_city", "lockwork_doorman"),
        };
        foreach (var (region, key, place, doorman) in cells)
            yield return new Request($"lockwork_{key}", region, place, doorman, $"flag {region}HallOfFame", new[]
            {
                new RequestStep("admin", $"flag FLAG_LOCKWORK_{key.ToUpperInvariant()}_ADMIN_BEATEN", doorman)
            });
        yield return new Request("lockwork_unova", RegionDatabase.Unova, "CasteliaCity", "lockwork_agent_1", $"flag {RegionDatabase.Unova}HallOfFame", new[]
        {
            new RequestStep("admin", "flag FLAG_LOCKWORK_UNOVA_ADMIN_BEATEN"),
            new RequestStep("head", "flag FLAG_LOCKWORK_MACHINE_STOPPED"),
        });
    }
    // (and the Open Door Agency's cases, plan 41 · EG7: a request each, under case_<region>)

    private static readonly Dictionary<string, Request> ById = All.ToDictionary(r => r.Id, StringComparer.Ordinal);

    /// <summary>A request by its id; null for one there is none of.</summary>
    public static Request? Find(string id) => ById.GetValueOrDefault(id);

    /// <summary>Whether a step is done: its own condition, or the request's last step (the errand is over).</summary>
    public static bool IsDone(Request request, RequestStep step, StoryState story) =>
        Holds(step.Condition, story) || Holds(request.Steps[^1].Condition, story);

    /// <summary>Where the request stands in this story.</summary>
    public static RequestState State(Request request, StoryState story)
    {
        if (Holds(request.Steps[^1].Condition, story)) return RequestState.Done;
        if (request.Known != null && Holds(request.Known, story)) return RequestState.Open;
        return request.Steps.Any(s => Holds(s.Condition, story)) ? RequestState.Open : RequestState.Unknown;
    }

    /// <summary>The first step not done yet; null once the request is done.</summary>
    public static RequestStep? Next(Request request, StoryState story) =>
        request.Steps.FirstOrDefault(s => !IsDone(request, s, story));

    /// <summary>The requests in a state, in the order of <see cref="All"/>.</summary>
    public static IReadOnlyList<Request> InState(StoryState story, RequestState state) =>
        All.Where(r => State(r, story) == state).ToList();

    /// <summary>Whether the player has heard of any request.</summary>
    public static bool AnyKnown(StoryState story) => All.Any(r => State(r, story) != RequestState.Unknown);

    // ------------------------------------------------------------------ the conditions

    /// <summary>A condition read: whether it is a flag or a variable, its name, and for a variable how it is compared with what.</summary>
    public readonly record struct Parsed(bool Negated, bool IsFlag, string Name, Compare Compare, int Value);

    /// <summary>
    /// Reads a condition as an <c>if</c> writes it: <c>flag NAME</c>, <c>not flag NAME</c> or <c>var NAME op N</c>
    /// (op one of <c>== != &lt; &lt;= &gt; &gt;=</c>). Throws on anything else, so a request misspelt fails its tests.
    /// </summary>
    public static Parsed Parse(string condition)
    {
        var words = condition.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        bool negated = words.Length > 0 && words[0] == "not";
        if (negated) words = words[1..];
        if (words.Length == 2 && words[0] == "flag") return new Parsed(negated, true, words[1], Compare.Equal, 1);
        if (words.Length == 4 && words[0] == "var" && int.TryParse(words[3], out int value))
        {
            var compare = words[2] switch
            {
                "==" => Compare.Equal,
                "!=" => Compare.NotEqual,
                "<" => Compare.Less,
                "<=" => Compare.LessOrEqual,
                ">" => Compare.Greater,
                ">=" => Compare.GreaterOrEqual,
                _ => throw new FormatException($"'{words[2]}' is no comparison in the request's condition '{condition}'")
            };
            return new Parsed(negated, false, words[1], compare, value);
        }
        throw new FormatException($"'{condition}' is no condition a request can ask: flag NAME, or var NAME op N");
    }

    /// <summary>Whether a condition holds in this story.</summary>
    public static bool Holds(string condition, StoryState story)
    {
        var c = Parse(condition);
        bool holds;
        if (c.IsFlag) holds = story.Has(c.Name);
        else
        {
            int v = story.Var(c.Name);
            holds = c.Compare switch
            {
                Compare.Equal => v == c.Value,
                Compare.NotEqual => v != c.Value,
                Compare.Less => v < c.Value,
                Compare.LessOrEqual => v <= c.Value,
                Compare.Greater => v > c.Value,
                _ => v >= c.Value
            };
        }
        return holds != c.Negated;
    }

    /// <summary>Every condition a request asks: what makes it known and each step's.</summary>
    public static IEnumerable<Parsed> ConditionsOf(Request request)
    {
        if (request.Known != null) yield return Parse(request.Known);
        foreach (var step in request.Steps) yield return Parse(step.Condition);
    }
}
