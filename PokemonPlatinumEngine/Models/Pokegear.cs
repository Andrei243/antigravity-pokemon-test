using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Models;

/// <summary>The Pokégear's cards (HeartGold's <c>include/constants/pokegear_card.h</c>, the clock being on every card).</summary>
public enum PokegearCard { Phone, Map, Radio }

/// <summary>
/// What the radio plays over the field (HeartGold's <c>enum RadioMusicPlayingSeq</c>, <c>GetRadioMusicPlayingSeq</c>):
/// the songs whose playing changes the wild Pokémon.
/// </summary>
public enum RadioSong { None, March, Lullaby, HoennSound, SinnohSound, Unown }

/// <summary>The radio's stations, in HeartGold's order (<c>include/constants/radio_station.h</c>).</summary>
public enum RadioStation
{
    PokemonMusic, PokemonTalk, SearchParty, RadioDrama, BuenasPassword, TrainerProfiles, ThatTownThesePeople,
    PokeFlute, Unown, TeamRocket, MahoganySignal, Commercials
}

/// <summary>What the radio picks up where the player is (<c>Radio_GetAvailableChannels</c>, the part Johto has so far).</summary>
public enum RadioSignal { Johto, NoSignal, Alph }

/// <summary>
/// One contact of the phone book (plan 20 · J3; HeartGold's <c>files/tel/pmtel_book.json</c>, which the importer writes
/// into <c>Data/world/johto/phone.json</c> as numbers): who they are, the trainer they are, where they are met, their
/// gift, and the day and hour they ask for a rematch on. No line of a call is in it: those are ours, in
/// <c>Data/scripts/phone.txt</c>.
/// </summary>
public sealed class PhoneContact
{
    /// <summary>The contact's constant without <c>PHONE_CONTACT_</c>, lower case (<c>mother</c>, <c>joey</c>).</summary>
    public string Id { get; set; } = "";

    /// <summary>The original's kind of caller: 0 a trainer, 1 Mom, 2 Elm, 11 the friend, 12 a Gym Leader…</summary>
    public int Type { get; set; }
    public string Class { get; set; } = "";
    public string? Trainer { get; set; }
    public string Area { get; set; } = "";
    public string? Gift { get; set; }
    public string? RematchDay { get; set; }
    public string? RematchTime { get; set; }

    /// <summary>The script of <c>phone.txt</c> a call to or from them plays: their id with its first letter up (<c>Joey</c>).</summary>
    public string Script => Id.Length == 0 ? "" : string.Concat(Id.Split('_').Select(p => p.Length == 0 ? p : char.ToUpperInvariant(p[0]) + p[1..]));

    /// <summary>
    /// The name the phone lists them by: a trainer's class and name (Youngster Joey), everyone else's of the string
    /// table (<c>pokegear.people.mother</c>: Mom).
    /// </summary>
    public string Name
    {
        get
        {
            if (Trainer is { } t && TrainerDatabase.Get(t) is { } record)
                return Text.Of("battle.fullTitle", ("class", Models.Trainer.ClassName(record.Class)), ("name", record.Name));
            // (the table's key is the id in camel case: prof_elm is profElm)
            string person = char.ToLowerInvariant(Script[0]) + Script[1..];
            // (Hoenn's rival is whichever hero the player isn't: the table says {rival})
            return PlayerIdentity.Fill(Text.Of($"pokegear.people.{person}"));
        }
    }

    /// <summary>The region whose book the contact is in (<c>johto</c>, <c>hoenn</c>), set as the book is read.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string Region { get; set; } = "johto";

    /// <summary>The kinds that ring on their own (<c>ov02_02251FDC</c>): trainers, Gym Leaders, the friend, Buena and the juggler.</summary>
    public bool RingsOnItsOwn => Type is 0 or 10 or 11 or 12 or 14;
}

/// <summary>
/// The phone books: Johto's, read once from <c>Data/world/johto/phone.json</c>, and the PokéNav's Match Call of Hoenn
/// (plan 21 · H2, part b), from <c>Data/world/hoenn/phone.json</c>, written by hand. An id is never in both, and
/// <c>addcontact</c> registers a number of either.
/// </summary>
public static class PhoneBook
{
    private static List<PhoneContact> Read(string region)
    {
        string path = Path.Combine(World.Folder, region, "phone.json");
        var book = File.Exists(GameDataFiles.PathOf(path)) ? GameDataFiles.Load<List<PhoneContact>>(path) : new List<PhoneContact>();
        foreach (var contact in book) contact.Region = region;
        return book;
    }

    private static readonly Lazy<List<PhoneContact>> all = new(() => Read("johto"));
    private static readonly Lazy<List<PhoneContact>> hoenn = new(() => Read("hoenn"));

    /// <summary>Every contact of the Pokégear, in the book's order (the order the Pokégear lists them in).</summary>
    public static IReadOnlyList<PhoneContact> All => all.Value;

    /// <summary>Every contact of the PokéNav's Match Call, in the book's order.</summary>
    public static IReadOnlyList<PhoneContact> Hoenn => hoenn.Value;

    public static PhoneContact? Get(string id) =>
        All.FirstOrDefault(c => string.Equals(c.Id, id, StringComparison.OrdinalIgnoreCase))
        ?? Hoenn.FirstOrDefault(c => string.Equals(c.Id, id, StringComparison.OrdinalIgnoreCase));

    /// <summary>The contact who is a trainer, by the trainer's id; null for none.</summary>
    public static PhoneContact? ByTrainer(string trainer) => All.FirstOrDefault(c => string.Equals(c.Trainer, trainer, StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// The Pokégear (plan 20 · J3): the cards it holds, the numbers registered in its phone, the call a contact has
/// waiting and the radio's songs. What it remembers is the story's own flags and variables (the cards by HeartGold's
/// names, <c>FLAG_GOT_POKEGEAR</c>, <c>FLAG_GOT_MAP_CARD</c>, <c>FLAG_GOT_RADIO_CARD</c>; a number by a flag of ours,
/// <c>FLAG_PHONE_CONTACT_JOEY</c>), so a save keeps it with no field of its own, as Amity Square's walk is kept. It is
/// Johto's alone: in any other region it is nothing. No drawing or input.
/// </summary>
public static class Pokegear
{
    public const string Flag = "FLAG_GOT_POKEGEAR", MapCardFlag = "FLAG_GOT_MAP_CARD", RadioCardFlag = "FLAG_GOT_RADIO_CARD";

    /// <summary>
    /// What a call is, which the engine sets before a contact's script plays (a variable of ours): 0 the player rang
    /// them, 1 they rang to talk, 2 they rang to ask for a rematch, 3 a rematch they asked for is still waiting.
    /// </summary>
    public const string CallVar = "VAR_PHONE_CALL";
    public const int CallMade = 0, CallTalk = 1, CallRematch = 2, CallWaiting = 3;

    /// <summary>The script of <c>phone.txt</c> a contact with none of their own plays.</summary>
    public const string NobodysCall = "Nobody";

    /// <summary>The variables of the Pokégear that the game itself keeps: a script may ask them and sets none.</summary>
    public static readonly IReadOnlyList<string> KeptByTheGame = new[] { CallVar };

    /// <summary>Whether the Pokégear does anything here: it is Johto's, and the player has been given it.</summary>
    public static bool Has(StoryState story, string region) =>
        string.Equals(region, RegionDatabase.Johto, StringComparison.OrdinalIgnoreCase) && story.Has(Flag);

    /// <summary>The cards it holds, in its own order: the phone always, the map and the radio once given.</summary>
    public static IReadOnlyList<PokegearCard> Cards(StoryState story)
    {
        var cards = new List<PokegearCard> { PokegearCard.Phone };
        if (story.Has(MapCardFlag)) cards.Add(PokegearCard.Map);
        if (story.Has(RadioCardFlag)) cards.Add(PokegearCard.Radio);
        return cards;
    }

    /// <summary>The flag a contact's number is kept by.</summary>
    public static string ContactFlag(string id) => "FLAG_PHONE_CONTACT_" + id.ToUpperInvariant();

    /// <summary>The numbers registered, in the book's order.</summary>
    public static IReadOnlyList<PhoneContact> Contacts(StoryState story) => PhoneBook.All.Where(c => story.Has(ContactFlag(c.Id))).ToList();

    /// <summary>Registers a number (<c>addcontact</c>). False for one the book doesn't have or already registered.</summary>
    public static bool Register(StoryState story, string id)
    {
        if (PhoneBook.Get(id) is not { } contact || story.Has(ContactFlag(contact.Id))) return false;
        story.Set(ContactFlag(contact.Id));
        return true;
    }

    /// <summary>
    /// What the radio adds to a step's share of attempts that get through (HeartGold's <c>FieldSystem_EncounterRateRoll</c>:
    /// the March adds 25, the Lullaby takes 25 away).
    /// </summary>
    public static int RadioRateBonus(RadioSong song) => song switch
    {
        RadioSong.March => 25,
        RadioSong.Lullaby => -25,
        _ => 0
    };
}

/// <summary>
/// A trainer's rematch over the phone (plan 20 · J3; plan 06 · R12's rule of which team comes next,
/// <see cref="VsSeeker.RematchTeam"/>, which this is Johto's door to as the Vs. Seeker is Sinnoh's). A trainer whose
/// number is registered and who has been beaten asks for a rematch when they ring, or are rung, on their day of the
/// week at their time of day; they then wait on their map, spinning as the Vs. Seeker's do, until fought. Which team
/// they bring is the first of their row not yet beaten, held back to the levels the Johto Badges have unlocked
/// (<see cref="Unlocked"/>). No drawing or input.
/// </summary>
public static class PhoneRematch
{
    /// <summary>The flag a rematch asked for is kept waiting by (ours).</summary>
    public static string WaitingFlag(string contactId) => "FLAG_PHONE_REMATCH_" + contactId.ToUpperInvariant();

    /// <summary>
    /// The Badges of Johto each level of a row asks for: HeartGold moves each caller's team on by flags of its own
    /// story in their own scripts; ours by the Badges, about where those flags fall (a ruling).
    /// </summary>
    public static readonly int[] BadgesForLevel = { 2, 4, 6, 8 };

    /// <summary>Whether a level of a Johto trainer's row (1 to 5) is open: the first four by the Badges, the fifth after Johto's Hall of Fame.</summary>
    public static bool Unlocked(int level, StoryState story) => level switch
    {
        >= 1 and <= 4 => story.BadgeCount(RegionDatabase.Johto) >= BadgesForLevel[level - 1],
        5 => story.Has(RegionDatabase.Get(RegionDatabase.Johto)!.StoryCompleteFlag),
        _ => false
    };

    /// <summary>The wild encounters' time of day (HeartGold's morning, day and night) a moment falls in.</summary>
    public static string TimeOf(TimeOfDay time) => time switch
    {
        TimeOfDay.Morning => "Morning",
        TimeOfDay.Day or TimeOfDay.Twilight => "Day",
        _ => "Night"
    };

    /// <summary>Whether it is the contact's day of the week and time of day.</summary>
    public static bool OnItsHour(PhoneContact contact, DateTime day, TimeOfDay time) =>
        contact.RematchDay is { } d && contact.RematchTime is { } t
        && string.Equals(day.DayOfWeek.ToString(), d, StringComparison.OrdinalIgnoreCase) && string.Equals(TimeOf(time), t, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Whether a call now with the contact asks for a rematch: a trainer with a row of teams, registered, beaten, and
    /// on their hour, not waiting for one already.
    /// </summary>
    public static bool Wants(PhoneContact contact, StoryState story, DateTime day, TimeOfDay time) =>
        contact.Trainer is { } trainer && TrainerDatabase.Get(trainer) is { Rematches.Count: > 0 }
        && story.Has(Pokegear.ContactFlag(contact.Id)) && story.HasDefeated(trainer)
        && !story.Has(WaitingFlag(contact.Id)) && OnItsHour(contact, day, time);

    /// <summary>The trainers of the book waiting for a rematch now.</summary>
    public static IEnumerable<string> Waiting(StoryState story) =>
        PhoneBook.All.Where(c => c.Trainer != null && story.Has(WaitingFlag(c.Id))).Select(c => c.Trainer!);

    /// <summary>
    /// What a call is (<see cref="Pokegear.CallVar"/>), decided as it begins: a rematch already waiting, one asked for
    /// now (which from then on waits), or a talk, made by the player or by the contact.
    /// </summary>
    public static int Call(PhoneContact contact, StoryState story, DateTime day, TimeOfDay time, bool incoming)
    {
        if (story.Has(WaitingFlag(contact.Id))) return Pokegear.CallWaiting;
        if (Wants(contact, story, day, time))
        {
            story.Set(WaitingFlag(contact.Id));
            return Pokegear.CallRematch;
        }
        return incoming ? Pokegear.CallTalk : Pokegear.CallMade;
    }

    /// <summary>Whoever of these people a call asked to wait for a rematch spins where they stand until fought, as the Vs. Seeker's do.</summary>
    public static void KeepWaiting(IEnumerable<NPC> people, StoryState story)
    {
        var waiting = Waiting(story).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (waiting.Count == 0) return;
        foreach (var npc in people)
            if (npc.IsTrainer && npc.TrainerData != null && waiting.Contains(npc.TrainerData.Id)) npc.ReadyForRematch = true;
    }

    /// <summary>A rematch has been fought: the trainer waits no longer.</summary>
    public static void Fought(StoryState story, string trainer)
    {
        if (PhoneBook.ByTrainer(trainer) is { } contact) story.Unset(WaitingFlag(contact.Id));
    }
}

/// <summary>
/// When a contact rings on their own (HeartGold's <c>GearPhoneRingManager</c>, <c>ov02_02251F20</c>): once ten minutes
/// have passed since the last ring, the next step asks once, and one time in two (51 in a hundred) a contact rings: one
/// of the registered numbers that ring on their own, not met where the player is, drawn evenly. A miss, or nobody to
/// ring, waits another ten minutes. The minutes are the field's own; no drawing or input.
/// </summary>
public sealed class PhoneRinger
{
    public const float WaitSeconds = 600f;
    public const int Chance = 50;

    /// <summary>The field's seconds since the last ring or the last try.</summary>
    public float Waited { get; private set; }

    public void Advance(float seconds) => Waited += Math.Max(0f, seconds);

    public void Reset() => Waited = 0f;

    /// <summary>On a step: the contact who rings now, or null.</summary>
    public PhoneContact? TryRing(StoryState story, string? areaHere, Random rng)
    {
        if (Waited < WaitSeconds) return null;
        Waited = 0f;
        // (MTRandom() % 100 > unk_var12: a miss)
        if (rng.Next(100) > Chance) return null;
        var callers = Pokegear.Contacts(story).Where(c => c.RingsOnItsOwn && !string.Equals(c.Area, areaHere, StringComparison.OrdinalIgnoreCase)).ToList();
        return callers.Count == 0 ? null : callers[rng.Next(callers.Count)];
    }
}

/// <summary>
/// What the radio's stations say (plan 20 · J3): lines of our own in <c>Data/world/johto/radio.json</c>, by station,
/// drawn with the dice the game hands over. No drawing or input.
/// </summary>
public static class RadioShows
{
    private static readonly Lazy<Dictionary<string, List<string>>> lines = new(() =>
    {
        string path = Path.Combine(World.Folder, "johto", "radio.json");
        return File.Exists(GameDataFiles.PathOf(path))
            ? GameDataFiles.Load<Dictionary<string, List<string>>>(path)
            : new Dictionary<string, List<string>>();
    });

    /// <summary>A station's lines; none for one that says nothing.</summary>
    public static IReadOnlyList<string> Of(RadioStation station) =>
        lines.Value.TryGetValue(station.ToString(), out var list) ? list : (IReadOnlyList<string>)Array.Empty<string>();

    /// <summary>
    /// A line of a station, drawn: <paramref name="species"/> and <paramref name="place"/> fill Professor Oak's talk (a
    /// Pokémon the player has seen and a place it lives in, or none, when his talk has nothing to say yet).
    /// </summary>
    public static string Line(RadioStation station, Random rng, string? species, string? place)
    {
        var all = Of(station);
        if (all.Count == 0) return "";
        var usable = all.Where(l => !l.Contains("{species}", StringComparison.Ordinal) || species != null).ToList();
        if (usable.Count == 0) return "";
        string line = usable[rng.Next(usable.Count)];
        return line.Replace("{species}", species ?? "").Replace("{place}", place ?? "");
    }
}

/// <summary>
/// The radio's dial (HeartGold's <c>overlay_101_021F4F34.c</c>): a cursor moved over a round dial 52 units across its
/// middle, in the touch screen's own units, and the stations as spots on it. Within a spot's inner ring a station comes
/// in clearly; within its outer ring, through static. Which station a spot is depends on the hour
/// (<see cref="StationOf"/>) and on what is received where the player is. No drawing or input.
/// </summary>
public sealed class RadioDial
{
    /// <summary>The dial's middle and reach.</summary>
    public const int CenterX = 128, CenterY = 92, Reach = 52;

    /// <summary>How far one press of an arrow moves the cursor.</summary>
    public const int Step = 4;

    /// <summary>A station's spot on the dial: its middle, the ring it is clear within and the one it is heard within, and the slot it is.</summary>
    public readonly record struct Spot(int X, int Y, int Clear, int Heard, int Slot);

    /// <summary>The spots of each signal (<c>sTuningHitboxes_Johto</c>, <c>_Alph</c>).</summary>
    public static IReadOnlyList<Spot> SpotsOf(RadioSignal signal) => signal switch
    {
        RadioSignal.Johto => new[] { new Spot(112, 76, 4, 16, 0), new Spot(152, 76, 4, 20, 1), new Spot(96, 108, 4, 16, 2), new Spot(136, 116, 4, 20, 3) },
        RadioSignal.Alph => new[] { new Spot(128, 92, 4, 16, 5) },
        _ => Array.Empty<Spot>()
    };

    public int X { get; private set; } = CenterX;
    public int Y { get; private set; } = CenterY;
    public RadioSignal Signal { get; set; }

    /// <summary>Moves the cursor, kept on the dial.</summary>
    public void Move(int dx, int dy)
    {
        int x = X + dx * Step, y = Y + dy * Step;
        if ((x - CenterX) * (x - CenterX) + (y - CenterY) * (y - CenterY) > Reach * Reach) return;
        X = x;
        Y = y;
    }

    public void Place(int x, int y)
    {
        X = x;
        Y = y;
    }

    /// <summary>The spot under the cursor, and whether it is clear; null where nothing is received.</summary>
    public (Spot Spot, bool Clear)? Tuned()
    {
        foreach (var spot in SpotsOf(Signal))
        {
            int d2 = (X - spot.X) * (X - spot.X) + (Y - spot.Y) * (Y - spot.Y);
            if (d2 <= spot.Heard * spot.Heard) return (spot, d2 <= spot.Clear * spot.Clear);
        }
        return null;
    }

    /// <summary>
    /// The station a slot is at an hour (<c>RadioShow_TranslateStationID</c>): the music, Professor Oak's talk, the
    /// trainers' profiles at even hours and the towns' people at odd, the search party, the serial drama and Buena's
    /// password by the hour's remainder by three, the Unown in the Ruins of Alph.
    /// </summary>
    public static RadioStation StationOf(int slot, int hour) => slot switch
    {
        0 => RadioStation.PokemonMusic,
        1 => RadioStation.PokemonTalk,
        2 => RadioStation.TrainerProfiles + hour % 2,
        3 => RadioStation.SearchParty + hour % 3,
        4 => RadioStation.PokeFlute,
        5 => RadioStation.Unown,
        6 => RadioStation.TeamRocket,
        7 => RadioStation.MahoganySignal,
        _ => RadioStation.PokemonMusic
    };

    /// <summary>
    /// The song of the Pokémon Music channel on a day (<c>RadioShow_PokemonMusic_Setup</c>): the March on Sundays,
    /// Mondays and Fridays, the Lullaby on Tuesdays and Saturdays, and with the National Pokédex the Hoenn Sound on
    /// Wednesdays and the Sinnoh Sound on Thursdays (the March and the Lullaby without it). GB Sounds is decision 8's.
    /// </summary>
    public static RadioSong MusicOn(DayOfWeek day, bool nationalDex) => day switch
    {
        DayOfWeek.Tuesday or DayOfWeek.Saturday => RadioSong.Lullaby,
        DayOfWeek.Wednesday => nationalDex ? RadioSong.HoennSound : RadioSong.March,
        DayOfWeek.Thursday => nationalDex ? RadioSong.SinnohSound : RadioSong.Lullaby,
        _ => RadioSong.March
    };

    /// <summary>The song a station plays over the field, if any.</summary>
    public static RadioSong SongOf(RadioStation station, DayOfWeek day, bool nationalDex) => station switch
    {
        RadioStation.PokemonMusic => MusicOn(day, nationalDex),
        RadioStation.Unown => RadioSong.Unown,
        _ => RadioSong.None
    };

    /// <summary>What is received in an area (<c>MapHeader_CanReceiveRadioSignal</c>, <c>sAlphMaps</c>): nothing in a cave, the Unown in the Ruins of Alph's halls.</summary>
    public static RadioSignal SignalAt(string? areaKey, bool cave) =>
        areaKey is { } key && key.StartsWith("ruins_of_alph_", StringComparison.OrdinalIgnoreCase) && key.Contains("hall", StringComparison.OrdinalIgnoreCase) ? RadioSignal.Alph
        : cave ? RadioSignal.NoSignal : RadioSignal.Johto;

    /// <summary>The song's file under <c>Data/music/johto</c>.</summary>
    public static string? SongFile(RadioSong song) => song switch
    {
        RadioSong.March => "johto/radio_march",
        RadioSong.Lullaby => "johto/radio_lullaby",
        RadioSong.HoennSound => "johto/radio_hoenn_sound",
        // (HeartGold's Sinnoh Sound is Route 201's theme: ours is Route 201's own)
        RadioSong.SinnohSound => "sinnoh/route201",
        RadioSong.Unown => "johto/radio_unown",
        _ => null
    };
}
