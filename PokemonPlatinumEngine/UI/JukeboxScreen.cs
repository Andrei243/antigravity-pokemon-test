using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>The jukebox's four lists (plan 12 · Q15): the songs, the sound effects, the cries and the ambience beds.</summary>
public enum JukeboxTab { Songs, Sounds, Cries, Beds }

/// <summary>How a song is played: as it is by day, in its night arrangement, or in the one a battle turns to in the red.</summary>
public enum JukeboxArrangement { Day, Night, LowHp }

/// <summary>
/// One line of a jukebox list: what is played (<see cref="Id"/>: a song's id, a sound's name, a cry mode's or a bed's),
/// whether it can be (a song is unlocked once it has been heard; everything else always is) and the arrangements it is
/// offered in (only a song has them: day always, night where the song has one, low HP where it has one).
/// </summary>
public sealed record JukeboxEntry(string Id, bool Unlocked, IReadOnlyList<JukeboxArrangement> Arrangements);

/// <summary>
/// The jukebox (plan 12 · Q15; style guide, "Menu screens", "Jukebox"): every song, sound effect, cry mode and ambience
/// bed in the game on four tabs, to be played from the title, under OPTIONS or from the options in a game. A song is
/// played once it has been heard in a game (<see cref="AudioManager.Heard"/>, kept in the save); until then its row
/// reads "???". Left and right change the tab, the shoulder keys the song's arrangement or the cry's species, the A
/// button plays (and, for a song or a bed, plays again to stop), B goes back. Its logic takes no input
/// (<see cref="Move"/>, <see cref="ChangeTab"/>, <see cref="Confirm"/>…), so tests and the harness drive it; the lists
/// are built with no window (<see cref="Songs"/>, <see cref="Sounds"/>, <see cref="Cries"/>, <see cref="Beds"/>).
/// </summary>
public class JukeboxScreen
{
    private const float AppearTime = 0.3f;

    /// <summary>The rows the list shows: as many as its panel holds (<see cref="ModernUi.RowsIn"/>).</summary>
    public static int VisibleRows => ModernUi.RowsIn(ModernUi.ContentBottom - (ModernUi.ContentTop + 76 + 24));

    private readonly IReadOnlyList<JukeboxEntry>[] lists = new IReadOnlyList<JukeboxEntry>[4];
    private readonly int[] cursors = new int[4];
    private readonly int[] firsts = new int[4];
    private int variant;
    private float openAge;
    private (JukeboxTab Tab, string Id, JukeboxArrangement Arrangement)? playing;

    public bool IsActive { get; private set; }
    public JukeboxTab Tab { get; private set; }

    /// <summary>The species whose cry the Cries tab plays, and the one chosen.</summary>
    public IReadOnlyList<PokemonSpecies> Species { get; private set; } = Array.Empty<PokemonSpecies>();
    public int SpeciesIndex { get; private set; }

    public PokemonSpecies? CryOf => SpeciesIndex >= 0 && SpeciesIndex < Species.Count ? Species[SpeciesIndex] : null;

    /// <summary>How far the screen has slid into place, 0 to 1.</summary>
    public float Appear => Math.Clamp(openAge / AppearTime, 0f, 1f);

    public IReadOnlyList<JukeboxEntry> Entries => lists[(int)Tab];
    public int Cursor => cursors[(int)Tab];
    public int First => firsts[(int)Tab];

    public JukeboxEntry? Selected => Cursor >= 0 && Cursor < Entries.Count ? Entries[Cursor] : null;

    /// <summary>The arrangement the cursor's song would be played in (day for anything else).</summary>
    public JukeboxArrangement Arrangement =>
        Selected is { Arrangements.Count: > 0 } e ? e.Arrangements[Math.Clamp(variant, 0, e.Arrangements.Count - 1)] : JukeboxArrangement.Day;

    /// <summary>The song or bed that is playing, if one is (what the A button would stop).</summary>
    public string? PlayingId => playing?.Id;
    public JukeboxTab? PlayingTab => playing?.Tab;
    public JukeboxArrangement? PlayingArrangement => playing?.Arrangement;

    // ------------------------------------------------------------------ the lists

    /// <summary>Every song in <c>Data/music</c>, by id; a song is unlocked when <paramref name="heard"/> holds it.</summary>
    public static List<JukeboxEntry> Songs(IReadOnlySet<string> heard) =>
        MusicLibrary.All.OrderBy(s => s.Id, StringComparer.Ordinal).Select(song =>
        {
            bool unlocked = heard.Contains(song.Id);
            // What a song hasn't shown yet doesn't say whether it has a night arrangement
            var how = new List<JukeboxArrangement> { JukeboxArrangement.Day };
            if (unlocked && song.HasNight) how.Add(JukeboxArrangement.Night);
            if (unlocked && song.HasLowHp) how.Add(JukeboxArrangement.LowHp);
            return new JukeboxEntry(song.Id, unlocked, how);
        }).ToList();

    /// <summary>Every sound effect of the bank, in the order its document lists them.</summary>
    public static List<JukeboxEntry> Sounds() =>
        SoundBank.Names.Select(n => new JukeboxEntry(n, true, Array.Empty<JukeboxArrangement>())).ToList();

    /// <summary>Every mode a cry is played in.</summary>
    public static List<JukeboxEntry> Cries() =>
        Enum.GetValues<CryMode>().Select(m => new JukeboxEntry(m.ToString(), true, Array.Empty<JukeboxArrangement>())).ToList();

    /// <summary>Every ambience bed.</summary>
    public static List<JukeboxEntry> Beds() =>
        Enum.GetValues<AmbienceBed>().Select(b => new JukeboxEntry(b.ToString(), true, Array.Empty<JukeboxArrangement>())).ToList();

    /// <summary>What a row says: a song's title (or "???" while it is locked), a sound's name, a cry mode's or a bed's words.</summary>
    public static string LabelOf(JukeboxTab tab, JukeboxEntry entry)
    {
        string id = entry.Id;
        return tab switch
        {
        JukeboxTab.Songs => !entry.Unlocked ? Text.Of("jukebox.locked")
            : MusicLibrary.Get(entry.Id) is { Title.Length: > 0 } song ? song.Title : entry.Id,
        JukeboxTab.Sounds => entry.Id.Replace('_', ' '),
        JukeboxTab.Cries => Text.Of($"jukebox.cry.{id}"),
            _ => Text.Of($"jukebox.bed.{id}")
        };
    }

    /// <summary>The arrangement's name in the language of the moment.</summary>
    public static string ArrangementName(JukeboxArrangement a) => Text.Of($"jukebox.arrangement.{a}");

    /// <summary>The folder a song is in (its region, or "common"), as a tag.</summary>
    public static string RegionOf(string songId)
    {
        int slash = songId.IndexOf('/');
        return slash < 0 ? songId : songId[..slash];
    }

    // ------------------------------------------------------------------ opening and closing

    /// <summary>
    /// Opens on the songs. <paramref name="heard"/> is what the game has played, <paramref name="species"/> the species the Cries tab can
    /// play (the first of them is chosen).
    /// </summary>
    public void Open(IEnumerable<string> heard, IReadOnlyList<PokemonSpecies> species)
    {
        var set = new HashSet<string>(heard, StringComparer.Ordinal);
        lists[(int)JukeboxTab.Songs] = Songs(set);
        lists[(int)JukeboxTab.Sounds] = Sounds();
        lists[(int)JukeboxTab.Cries] = Cries();
        lists[(int)JukeboxTab.Beds] = Beds();
        Array.Clear(cursors);
        Array.Clear(firsts);
        Species = species;
        SpeciesIndex = 0;
        Tab = JukeboxTab.Songs;
        variant = 0;
        playing = null;
        openAge = 0f;
        IsActive = true;
        AudioManager.PlaySound("menu_open");
    }

    /// <summary>Lets go of whatever is playing; the game puts its own music back.</summary>
    public void Close()
    {
        Stop();
        IsActive = false;
    }

    /// <summary>Stops the song and the bed the jukebox started and leaves the music's arrangement as a game has it.</summary>
    public void Stop()
    {
        if (playing is { Tab: JukeboxTab.Beds }) AudioManager.PreviewBed(null);
        if (playing is { Tab: JukeboxTab.Songs }) AudioManager.StopMusic();
        AudioManager.LowHp = false;
        playing = null;
    }

    // ------------------------------------------------------------------ the buttons

    /// <summary>The cursor up (−1) or down (+1), wrapping round.</summary>
    public void Move(int delta)
    {
        int t = (int)Tab;
        if (Entries.Count == 0) return;
        int next = UiNav.Wrap(cursors[t], delta, Entries.Count);
        if (next == cursors[t]) return;
        cursors[t] = next;
        firsts[t] = UiNav.Window(firsts[t], next, Entries.Count, VisibleRows);
        variant = 0;
        AudioManager.PlaySound("cursor");
    }

    /// <summary>The next tab (+1) or the one before (−1), wrapping round.</summary>
    public void ChangeTab(int delta)
    {
        Tab = (JukeboxTab)UiNav.Wrap((int)Tab, delta, 4);
        variant = 0;
        AudioManager.PlaySound("cursor");
    }

    /// <summary>
    /// The shoulder keys: on the Songs tab the next or previous arrangement of the song under the cursor (only those it
    /// has), on the Cries tab the next or previous species.
    /// </summary>
    public void ChangeVariant(int delta)
    {
        if (Tab == JukeboxTab.Songs && Selected is { Arrangements.Count: > 1 } entry)
        {
            variant = UiNav.Wrap(variant, delta, entry.Arrangements.Count);
            AudioManager.PlaySound("cursor");
        }
        else if (Tab == JukeboxTab.Cries && Species.Count > 0)
        {
            SpeciesIndex = UiNav.Wrap(SpeciesIndex, delta, Species.Count);
            AudioManager.PlaySound("cursor");
        }
    }

    /// <summary>
    /// The A button: plays what the cursor is on. A locked song says no; a song or a bed already playing (in that
    /// arrangement) is stopped instead.
    /// </summary>
    public void Confirm()
    {
        if (Selected is not { } entry) return;
        switch (Tab)
        {
            case JukeboxTab.Songs:
                if (!entry.Unlocked)
                {
                    AudioManager.PlaySound("error");
                    return;
                }
                if (playing is { Tab: JukeboxTab.Songs } now && now.Id == entry.Id && now.Arrangement == Arrangement)
                {
                    Stop();
                    return;
                }
                if (playing is { Tab: JukeboxTab.Beds }) AudioManager.PreviewBed(null);
                AudioManager.PreviewMusic(entry.Id, Arrangement == JukeboxArrangement.Night, Arrangement == JukeboxArrangement.LowHp);
                playing = (JukeboxTab.Songs, entry.Id, Arrangement);
                break;
            case JukeboxTab.Sounds:
                AudioManager.PlaySound(entry.Id);
                break;
            case JukeboxTab.Cries:
                if (CryOf is { } species) AudioManager.PlayCry(species, null, Enum.Parse<CryMode>(entry.Id));
                break;
            default:
                if (playing is { Tab: JukeboxTab.Beds } bed && bed.Id == entry.Id)
                {
                    AudioManager.PreviewBed(null);
                    playing = null;
                }
                else
                {
                    AudioManager.PreviewBed(Enum.Parse<AmbienceBed>(entry.Id));
                    playing = (JukeboxTab.Beds, entry.Id, JukeboxArrangement.Day);
                }
                break;
        }
    }

    /// <summary>The B button: back to where the jukebox was opened from.</summary>
    public void Cancel()
    {
        Close();
        AudioManager.PlaySound("cancel");
    }

    public void Update(float dt = 1f / 60f)
    {
        if (!IsActive) return;
        openAge += dt;
        if (InputManager.IsActionPressed(GameAction.Up)) Move(-1);
        else if (InputManager.IsActionPressed(GameAction.Down)) Move(1);
        else if (InputManager.IsActionPressed(GameAction.Left)) ChangeTab(-1);
        else if (InputManager.IsActionPressed(GameAction.Right)) ChangeTab(1);
        else if (InputManager.IsActionPressed(GameAction.PageLeft)) ChangeVariant(-1);
        else if (InputManager.IsActionPressed(GameAction.PageRight)) ChangeVariant(1);
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
        else if (InputManager.IsActionPressed(GameAction.Cancel) || InputManager.IsActionPressed(GameAction.Menu)) Cancel();
    }

    /// <summary>Draws the jukebox.</summary>
    public void Draw(int screenWidth, int screenHeight) => ModernUi.DrawJukebox(screenWidth, screenHeight, this, Appear);
}
