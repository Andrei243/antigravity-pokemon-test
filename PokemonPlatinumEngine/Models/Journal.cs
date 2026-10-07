using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Models;

/// <summary>What a line of the Journal tells of (the original's location events, the ones this game has so far).</summary>
public enum JournalEventKind
{
    RestedAtHome, LeftResearchLab, UsedPcBox, ShoppedAtMart, LotsOfShopping, SoldALittle, SoldALot, BusinessAtMart,
    GymWasTooTough, BeatGymLeader, BeatEliteFourMember, BeatChampion, ArrivedInLocation, LeftCave, LeftBuilding,
    ItemWasObtained, UsedFieldMove
}

/// <summary>One line of a page: what happened, and the place, item, move or trainer it names.</summary>
public sealed record JournalEvent(JournalEventKind Kind, string? Subject = null);

/// <summary>A Pokémon of the day: one caught, or a place where the player knocked many out.</summary>
public sealed record JournalPokemon(bool Caught, string Species, string? Place);

/// <summary>The last trainer beaten that day, and where.</summary>
public sealed record JournalTrainer(string Name, string Place);

/// <summary>One page of the Journal: a day, where the game was taken up, four lines, a Pokémon and a trainer.</summary>
public sealed class JournalPage
{
    public const int Lines = 4;

    public DateTime Date { get; set; }
    public string Place { get; set; } = "";
    public List<JournalEvent> Events { get; } = new();
    public JournalPokemon? Pokemon { get; set; }

    /// <summary>The last trainer beaten that day, and where.</summary>
    public JournalTrainer? Trainer { get; set; }
}

/// <summary>
/// The Journal (plan 06 · R12; the original's <c>journal.c</c>): a page for each of the last ten days the game was
/// taken up on, newest first. A page begins only when a save is loaded on a new day (a game played past midnight
/// goes on writing on the same page), and the Journal opens by itself as the game is continued when its newest page
/// is two days old or more. A page keeps four lines (the oldest goes for a new one, and the same thing twice in a
/// row is told once), the last Pokémon caught, or knocked out five times in one place, and the last trainer beaten.
/// No drawing or input.
/// </summary>
public sealed class Journal
{
    public const int Pages = 10;

    /// <summary>How many Pokémon must be knocked out in one place before the page tells of it.</summary>
    public const int DefeatsToTell = 5;

    private readonly List<JournalPage> pages = new();

    /// <summary>The pages, newest first.</summary>
    public IReadOnlyList<JournalPage> All => pages;
    public JournalPage? Today => pages.Count > 0 ? pages[0] : null;

    // The wild Pokémon knocked out where the player is now: counted again in every place (the original's map change)
    private int defeatsHere;

    /// <summary>
    /// The game is taken up (<c>Journal_GetSavedPage</c>): a page dated another day makes room for a new one, the
    /// oldest of ten falling away, and today's page notes where the game was taken up.
    /// </summary>
    public void TakenUp(DateTime today, string place)
    {
        if (Today is { } last && last.Date == today.Date) return;
        pages.Insert(0, new JournalPage { Date = today.Date, Place = place });
        if (pages.Count > Pages) pages.RemoveAt(pages.Count - 1);
    }

    /// <summary>
    /// Whether the Journal opens by itself as the game is continued (<c>Journal_CheckOpenOnContinue</c>): its newest
    /// page is two days or more before today. The day before doesn't open it.
    /// </summary>
    public bool OpensOnContinue(DateTime today) => Today is { } last && Math.Abs((today.Date - last.Date).TotalDays) >= 2;

    /// <summary>
    /// A line for today's page, by the original's rules: the same kind of thing as the line before is told once
    /// (the same place, item or move for those that name one); a Gym too tough is forgotten once its Leader is
    /// beaten; only the latest of the Elite Four is kept; the oldest of four goes for a new one.
    /// </summary>
    public void Tell(JournalEvent e)
    {
        if (Today is not { } page) return;
        var events = page.Events;
        if (events.Count > 0 && events[^1].Kind == e.Kind && events[^1].Subject == e.Subject) return;
        if (e.Kind == JournalEventKind.BeatGymLeader) events.RemoveAll(o => o.Kind == JournalEventKind.GymWasTooTough && o.Subject == e.Subject);
        if (e.Kind == JournalEventKind.BeatEliteFourMember) events.RemoveAll(o => o.Kind == JournalEventKind.BeatEliteFourMember);
        events.Add(e);
        if (events.Count > JournalPage.Lines) events.RemoveAt(0);
    }

    /// <summary>A Pokémon caught: today's Pokémon from now on.</summary>
    public void Caught(string species, string? place)
    {
        if (Today is { } page) page.Pokemon = new JournalPokemon(true, species, place);
    }

    /// <summary>A wild Pokémon knocked out: the fifth in one place, and every one after, is told of.</summary>
    public void Defeated(string species, string? place)
    {
        if (++defeatsHere >= DefeatsToTell && Today is { } page) page.Pokemon = new JournalPokemon(false, species, place);
    }

    /// <summary>The player has come to another place: the count of Pokémon knocked out begins again.</summary>
    public void ChangedPlace() => defeatsHere = 0;

    /// <summary>A trainer beaten (not a Gym Leader, the Elite Four or the Champion, who have lines of their own).</summary>
    public void BeatTrainer(string name, string place)
    {
        if (Today is { } page) page.Trainer = new JournalTrainer(name, place);
    }

    /// <summary>Brings back what a save kept, newest first.</summary>
    public void Restore(IEnumerable<JournalPage> kept)
    {
        pages.Clear();
        pages.AddRange(kept.Take(Pages));
    }

    /// <summary>A page told in words of our own: one line for each thing it keeps.</summary>
    public static IEnumerable<string> Lines(JournalPage page)
    {
        yield return $"Took up the adventure in {page.Place}.";
        foreach (var e in page.Events) yield return Line(e);
        if (page.Pokemon is { } p)
            yield return p.Caught ? $"Caught {Article(p.Species)} {p.Species}{At(p.Place)}." : $"Battled lots of wild Pokémon{At(p.Place)}, {p.Species} among them.";
        if (page.Trainer is { } trainer) yield return $"Battled {trainer.Name}{At(trainer.Place)}.";
    }

    private static string At(string? place) => string.IsNullOrEmpty(place) ? "" : $" in {place}";

    private static string Article(string word) => "AEIOU".Contains(char.ToUpperInvariant(word[0])) ? "an" : "a";

    public static string Line(JournalEvent e) => e.Kind switch
    {
        JournalEventKind.RestedAtHome => "Had a rest at home.",
        JournalEventKind.LeftResearchLab => "Called in at the Pokémon Research Lab.",
        JournalEventKind.UsedPcBox => "Sorted the Pokémon in the PC's boxes.",
        JournalEventKind.ShoppedAtMart => "Did some shopping at the Poké Mart.",
        JournalEventKind.LotsOfShopping => "Bought plenty at the Poké Mart.",
        JournalEventKind.SoldALittle => "Sold a few things at the Poké Mart.",
        JournalEventKind.SoldALot => "Sold a lot at the Poké Mart.",
        JournalEventKind.BusinessAtMart => "Bought and sold at the Poké Mart.",
        JournalEventKind.GymWasTooTough => $"Tried the {e.Subject} Gym, and it was too tough.",
        JournalEventKind.BeatGymLeader => $"Beat {e.Subject} and won a Gym Badge!",
        JournalEventKind.BeatEliteFourMember => $"Beat {e.Subject} of the Elite Four.",
        JournalEventKind.BeatChampion => $"Beat the Champion, {e.Subject}!",
        JournalEventKind.ArrivedInLocation => $"Arrived in {e.Subject}.",
        JournalEventKind.LeftCave => $"Made it out of {e.Subject}.",
        JournalEventKind.LeftBuilding => $"Left {e.Subject}.",
        JournalEventKind.ItemWasObtained => $"Found {e.Subject}.",
        JournalEventKind.UsedFieldMove => $"Used {e.Subject} in the field.",
        _ => ""
    };
}
