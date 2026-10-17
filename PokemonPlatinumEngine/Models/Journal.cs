using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>What a line of the Journal tells of (the original's location events, the ones this game has so far).</summary>
public enum JournalEventKind
{
    RestedAtHome, LeftResearchLab, UsedPcBox, ShoppedAtMart, LotsOfShopping, SoldALittle, SoldALot, BusinessAtMart,
    GymWasTooTough, BeatGymLeader, BeatEliteFourMember, BeatChampion, ArrivedInLocation, LeftCave, LeftBuilding,
    ItemWasObtained, UsedFieldMove,
    /// <summary>Played the slots (the original's <c>LOCATION_EVENT_GAME_CORNER</c>; plan 06 · R16b).</summary>
    GameCorner,
    /// <summary>Took a challenge at the Battle Tower (<c>LOCATION_EVENT_BATTLE_TOWER</c>; plan 06 · R18a).</summary>
    BattleTower,
    /// <summary>Took a challenge at the Battle Factory (<c>LOCATION_EVENT_BATTLE_FACTORY</c>; plan 06 · R18b).</summary>
    BattleFactory,
    /// <summary>Took a challenge at the Battle Hall or the Battle Castle (<c>LOCATION_EVENT_BATTLE_HALL</c>, <c>…_CASTLE</c>; plan 06 · R18c).</summary>
    BattleHall,
    BattleCastle,
    /// <summary>Took a challenge at the Battle Arcade (<c>LOCATION_EVENT_BATTLE_ARCADE</c>; plan 06 · R18d).</summary>
    BattleArcade,
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
        yield return Text.Of("journal.line.tookUp", ("place", PlaceWords.In(page.Place)));
        foreach (var e in page.Events) yield return Line(e);
        if (page.Pokemon is { } p)
        {
            string at = At(p.Place);
            yield return !p.Caught ? Text.Of("journal.line.battledWild", ("at", at), ("species", LocalNames.Species(p.Species)))
                : StartsWithAVowel(p.Species) ? Text.Of("journal.line.caughtAn", ("species", LocalNames.Species(p.Species)), ("at", at))
                : Text.Of("journal.line.caught", ("species", LocalNames.Species(p.Species)), ("at", at));
        }
        if (page.Trainer is { } trainer)
        {
            string at = At(trainer.Place);
            yield return Text.Of("journal.line.battledTrainer", ("trainer", trainer.Name), ("at", at));
        }
    }

    private static string At(string? place) => string.IsNullOrEmpty(place) ? "" : " " + PlaceWords.In(place);

    // Whether English says "an" before the word
    private static bool StartsWithAVowel(string word) => "AEIOU".Contains(char.ToUpperInvariant(word[0]));

    public static string Line(JournalEvent e)
    {
        string subject = e.Subject ?? "";
        return e.Kind switch
        {
            JournalEventKind.RestedAtHome => Text.Of("journal.line.restedAtHome"),
            JournalEventKind.LeftResearchLab => Text.Of("journal.line.researchLab"),
            JournalEventKind.UsedPcBox => Text.Of("journal.line.pcBox"),
            JournalEventKind.ShoppedAtMart => Text.Of("journal.line.shopped"),
            JournalEventKind.LotsOfShopping => Text.Of("journal.line.lotsOfShopping"),
            JournalEventKind.SoldALittle => Text.Of("journal.line.soldALittle"),
            JournalEventKind.SoldALot => Text.Of("journal.line.soldALot"),
            JournalEventKind.BusinessAtMart => Text.Of("journal.line.business"),
            JournalEventKind.GymWasTooTough => Text.Of("journal.line.gymTooTough", ("leader", e.Subject)),
            JournalEventKind.BeatGymLeader => Text.Of("journal.line.beatLeader", ("leader", e.Subject)),
            JournalEventKind.BeatEliteFourMember => Text.Of("journal.line.beatEliteFour", ("name", e.Subject)),
            JournalEventKind.BeatChampion => Text.Of("journal.line.beatChampion", ("name", e.Subject)),
            JournalEventKind.ArrivedInLocation => Text.Of("journal.line.arrived", ("place", PlaceWords.In(subject))),
            JournalEventKind.LeftCave => Text.Of("journal.line.leftCave", ("place", PlaceWords.Named(subject))),
            JournalEventKind.LeftBuilding => Text.Of("journal.line.leftBuilding", ("place", PlaceWords.Named(subject))),
            JournalEventKind.ItemWasObtained => Text.Of("journal.line.found", ("item", LocalNames.Item(e.Subject))),
            JournalEventKind.UsedFieldMove => Text.Of("journal.line.usedFieldMove", ("move", LocalNames.Move(e.Subject))),
            JournalEventKind.GameCorner => Text.Of("journal.line.gameCorner"),
            JournalEventKind.BattleTower => Text.Of("journal.line.tower"),
            JournalEventKind.BattleFactory => Text.Of("journal.line.factory"),
            JournalEventKind.BattleHall => Text.Of("journal.line.hall"),
            JournalEventKind.BattleCastle => Text.Of("journal.line.castle"),
            JournalEventKind.BattleArcade => Text.Of("journal.line.arcade"),
            _ => ""
        };
    }
}
