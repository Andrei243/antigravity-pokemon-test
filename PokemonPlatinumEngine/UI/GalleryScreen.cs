using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;
using Raylib_cs;

namespace PokemonPlatinumEngine.UI;

/// <summary>The gallery's two tabs (plan 12 · Q30): the Pokémon seen and their forms, and the people.</summary>
public enum GalleryTab { Pokemon, People }

/// <summary>
/// A row of the gallery's Pokémon tab: the model it shows (<see cref="Look"/>: a species' name, or a form's as Pokémon
/// Showdown spells it), the species it is of and whether it is one of its forms.
/// </summary>
public sealed record GalleryEntry(string Look, PokemonSpecies Species, bool IsForm);

/// <summary>A row of the gallery's people tab: the character type drawn and the name shown.</summary>
public sealed record GalleryPerson(string Look, string Name);

/// <summary>
/// Where the gallery's models come from (plan 12 · Q30): the game's <see cref="PokemonModels"/>, or a test's own. The gallery
/// asks for a model once (<see cref="Ask"/>), shows it once it is <see cref="Ready"/> and lets go of what it was the first to
/// ask for (<see cref="LetGo"/>), so it keeps no model for good.
/// </summary>
internal interface IGalleryModels
{
    /// <summary>Whether something already holds the model (built, on its way, or asked for by anyone but a baking).</summary>
    bool HeldElsewhere(string look);
    void Ask(string look);
    bool Ready(string look);
    void LetGo(string look);
}

/// <summary>The game's models: <see cref="PokemonModels.Request"/>, <see cref="PokemonModels.TryGet"/>, <see cref="PokemonModels.Release"/>.</summary>
internal sealed class GameGalleryModels : IGalleryModels
{
    public static readonly GameGalleryModels Instance = new();

    // A model on loan to a sprite's baking is nobody's yet: asked for by the gallery, it becomes the gallery's to let go of
    public bool HeldElsewhere(string look) => PokemonModels.Holds(look) && !PokemonModels.IsBorrowed(look);
    public void Ask(string look) => PokemonModels.Request(look);
    public bool Ready(string look) => PokemonModels.TryGet(look, out _);
    public void LetGo(string look) => PokemonModels.Release(look);
}

/// <summary>
/// The gallery (plan 12 · Q30; style guide, "Pokémon in menus" and "Menu screens", "Gallery"): every species the
/// Pokédex has seen, each with every form it has, as its 3D model on a stage of its own, turned, played through its
/// clips and heard; and a second tab of people with their emotes. It opens from the title's GALLERY (the species any slot
/// has seen) and from a Pokédex entry (the game's, on that species), and puts nothing in the save. A model not built yet
/// is asked for once and its still sprite stands in until it is, so no frame waits; the gallery lets go of every model it
/// was the first to ask for as the cursor leaves it and as it closes. Its logic takes no input (<see cref="Move"/>,
/// <see cref="Spin"/>, <see cref="NextClip"/>, <see cref="ToggleBack"/>, <see cref="ChangeZoom"/>, <see cref="ChangeTab"/>,
/// <see cref="Confirm"/>, <see cref="Cancel"/>, <see cref="Advance"/>), so tests and the harness drive it.
/// </summary>
public class GalleryScreen
{
    private const float AppearTime = 0.3f;

    /// <summary>Radians a second the model turns by itself until it is turned by hand: a quarter turn every two seconds.</summary>
    public const float SpinSpeed = MathF.PI / 4f;

    /// <summary>Radians a second a held Left or Right turns it: a half turn a second.</summary>
    public const float TurnSpeed = MathF.PI;

    /// <summary>How long a move's clip plays, and how long it is held before it plays again.</summary>
    public const float ClipSeconds = 1.2f, HoldSeconds = 0.6f;

    /// <summary>How long an emote is given before it plays again.</summary>
    public const float EmoteSeconds = 2.4f;

    /// <summary>The zoom's three steps.</summary>
    public static readonly float[] Zooms = { 1f, 1.5f, 2f };

    /// <summary>The picture the model is rendered into, in real pixels: drawn 1120 units square on the 4K screen.</summary>
    public const int PictureSize = 1536;

    /// <summary>
    /// The share of the picture the model fills at the first zoom: the picture is drawn larger than the model (1120 units,
    /// clipped to the stage) so that a model zoomed in is cut by the stage's edge and never by the picture's.
    /// </summary>
    public const float FrameShare = 680f / 1120f;

    /// <summary>The rows the list shows: as many as its panel holds.</summary>
    public static int VisibleRows => ModernUi.RowsIn(ModernUi.ContentBottom - (ModernUi.ContentTop + 76 + 24));

    /// <summary>The people's clips: standing, then every emote.</summary>
    internal static IReadOnlyList<Emote> Emotes { get; } = Enum.GetValues<Emote>().ToArray();

    private readonly IGalleryModels models;
    private readonly HashSet<string> asked = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> owned = new(StringComparer.OrdinalIgnoreCase);
    private readonly HeldKey upDown = new();
    private readonly int[] cursors = new int[2];
    private readonly int[] firsts = new int[2];
    private float openAge;
    private RenderTexture2D picture;
    private bool pictureLoaded;

    public GalleryScreen() : this(GameGalleryModels.Instance) { }

    internal GalleryScreen(IGalleryModels models) => this.models = models;

    public bool IsActive { get; private set; }
    public GalleryTab Tab { get; private set; }

    public IReadOnlyList<GalleryEntry> Pokemon { get; private set; } = Array.Empty<GalleryEntry>();
    public IReadOnlyList<GalleryPerson> People { get; private set; } = Array.Empty<GalleryPerson>();

    /// <summary>How many rows the tab has.</summary>
    public int Count => Tab == GalleryTab.Pokemon ? Pokemon.Count : People.Count;
    public int Cursor => cursors[(int)Tab];
    public int First => firsts[(int)Tab];

    public GalleryEntry? Selected => Tab == GalleryTab.Pokemon && Cursor < Pokemon.Count ? Pokemon[Cursor] : null;
    public GalleryPerson? SelectedPerson => Tab == GalleryTab.People && Cursor < People.Count ? People[Cursor] : null;

    /// <summary>The turn over the front's own yaw, 0 to 2π.</summary>
    public float Turn { get; private set; }

    /// <summary>Whether it turns by itself (until Left or Right, or P, takes the turn in hand).</summary>
    public bool Spinning { get; private set; } = true;

    /// <summary>Whether its back is to the viewer: turned more than a quarter turn either way.</summary>
    public bool Back => Turn > MathF.PI / 2f && Turn < MathF.PI * 1.5f;

    /// <summary>The clip (or on the people's tab the emote) played, as an index of <see cref="PokemonAnimation.Clips"/> or <see cref="Emotes"/>.</summary>
    public int ClipIndex { get; private set; }

    public string Clip => PokemonAnimation.Clips[ClipIndex % PokemonAnimation.Clips.Length];
    internal Emote Emote => Emotes[ClipIndex % Emotes.Count];

    /// <summary>Seconds since the clip began (or began again).</summary>
    public float ClipAge { get; private set; }

    public int ZoomStep { get; private set; }
    public float Zoom => Zooms[ZoomStep];

    /// <summary>How far the screen has slid into place, 0 to 1.</summary>
    public float Appear => Math.Clamp(openAge / AppearTime, 0f, 1f);

    /// <summary>How many models the gallery has asked for since it opened (each look once).</summary>
    public int AskedCount => asked.Count;

    /// <summary>The models the gallery holds for itself now: those it was the first to ask for and hasn't let go.</summary>
    public IReadOnlyCollection<string> Owned => owned;

    /// <summary>Whether the model of the row under the cursor is built (otherwise its still sprite stands in).</summary>
    public bool ShowsModel => Selected is { } e && models.Ready(e.Look);

    // ------------------------------------------------------------------ the lists

    /// <summary>
    /// What the Pokémon tab lists: every species of <paramref name="seen"/> (National numbers) in National order, each
    /// followed by every form its data has. A number the data doesn't know is passed over.
    /// </summary>
    public static List<GalleryEntry> EntriesFor(IEnumerable<int> seen)
    {
        var list = new List<GalleryEntry>();
        foreach (int number in seen.Distinct().Order())
        {
            if (PokemonDatabase.GetByDex(number) is not { } species) continue;
            list.Add(new GalleryEntry(species.Name, species, false));
            foreach (var form in species.Forms ?? new List<PokemonForm>())
                list.Add(new GalleryEntry(form.Name, species, true));
        }
        return list;
    }

    /// <summary>
    /// The people the tab lists until the game keeps who has been met (plan 08 · P12's greetings name people, not looks):
    /// the player's look and the assistant's (the other), and the professor of <paramref name="professor"/>'s region.
    /// </summary>
    public static List<GalleryPerson> PeopleFor(string playerLook, string playerName, string otherLook, string otherName, RegionProfessor? professor)
    {
        var list = new List<GalleryPerson> { new(playerLook, playerName), new(otherLook, otherName) };
        if (professor != null) list.Add(new GalleryPerson(professor.Look, Text.OfData(professor.Name)));
        return list;
    }

    /// <summary>What a Pokémon row says: its species' name, or its form's.</summary>
    public static string LabelOf(GalleryEntry entry) =>
        entry.IsForm && entry.Species.Forms?.Find(f => f.Name == entry.Look) is { } form ? form.DisplayName(entry.Species.Name) : entry.Species.DisplayName;

    /// <summary>A clip's name on screen.</summary>
    public static string ClipName(string clip) => Text.Of($"gallery.clip.{clip}");

    /// <summary>An emote's name on screen.</summary>
    internal static string EmoteName(Emote emote) => Text.Of($"gallery.emote.{emote}");

    // ------------------------------------------------------------------ opening and closing

    /// <summary>
    /// Opens on the Pokémon tab with the species seen (<paramref name="seen"/>, National numbers) and the
    /// <paramref name="people"/>; on <paramref name="openOn"/>'s row where it names one (a Pokédex entry's species).
    /// </summary>
    public void Open(IEnumerable<int> seen, IReadOnlyList<GalleryPerson> people, string? openOn = null)
    {
        LetGoOfAll();
        Pokemon = EntriesFor(seen);
        People = people;
        Array.Clear(cursors);
        Array.Clear(firsts);
        Tab = Pokemon.Count == 0 && people.Count > 0 ? GalleryTab.People : GalleryTab.Pokemon;
        if (openOn != null)
        {
            int at = Pokemon.ToList().FindIndex(e => e.Look.Equals(openOn, StringComparison.OrdinalIgnoreCase));
            if (at >= 0)
            {
                cursors[0] = at;
                firsts[0] = UiNav.Window(0, at, Pokemon.Count, VisibleRows);
            }
        }
        Turn = 0f;
        Spinning = true;
        ClipIndex = 0;
        ClipAge = 0f;
        ZoomStep = 0;
        openAge = 0f;
        IsActive = true;
        upDown.Release();
        AudioManager.PlaySound("menu_open");
        KeepTheModelAsked();
    }

    /// <summary>Closes, letting go of every model the gallery was the first to ask for, and of its picture.</summary>
    public void Close()
    {
        LetGoOfAll();
        if (pictureLoaded)
        {
            Gpu.Unload(picture);
            pictureLoaded = false;
        }
        IsActive = false;
    }

    private void LetGoOfAll()
    {
        foreach (var look in owned) models.LetGo(look);
        owned.Clear();
        asked.Clear();
    }

    /// <summary>
    /// Asks for the model under the cursor once, and lets go of any other the gallery holds for itself: at most one
    /// model is the gallery's at a time.
    /// </summary>
    private void KeepTheModelAsked()
    {
        string? want = Selected?.Look;
        foreach (var look in owned.Where(l => !l.Equals(want, StringComparison.OrdinalIgnoreCase)).ToList())
        {
            models.LetGo(look);
            owned.Remove(look);
            // Let go, it is asked for again if the cursor comes back to it
            asked.Remove(look);
        }
        if (want == null || asked.Contains(want)) return;
        asked.Add(want);
        if (!models.HeldElsewhere(want)) owned.Add(want);
        models.Ask(want);
    }

    // ------------------------------------------------------------------ the buttons

    /// <summary>The cursor up (−1) or down (+1), wrapping round; the clip begins again for what it comes to.</summary>
    public void Move(int delta, bool held = false)
    {
        int t = (int)Tab;
        if (Count == 0 || delta == 0) return;
        int next = held ? Math.Clamp(cursors[t] + delta, 0, Count - 1) : UiNav.Wrap(cursors[t], delta, Count);
        if (next == cursors[t]) return;
        cursors[t] = next;
        firsts[t] = UiNav.Window(firsts[t], next, Count, VisibleRows);
        ClipAge = 0f;
        AudioManager.PlaySound("cursor");
        KeepTheModelAsked();
    }

    /// <summary>The other tab. The clip goes back to the first, since the two tabs' clips are not the same.</summary>
    public void ChangeTab(int delta = 1)
    {
        Tab = (GalleryTab)UiNav.Wrap((int)Tab, delta, 2);
        ClipIndex = 0;
        ClipAge = 0f;
        AudioManager.PlaySound("page");
        KeepTheModelAsked();
    }

    /// <summary>Turns the model by <paramref name="radians"/>, wrapping round, and takes the turn in hand (it stops turning by itself).</summary>
    public void Spin(float radians)
    {
        Spinning = false;
        Turn = Wrap(Turn + radians);
    }

    /// <summary>Front to back or back to front: a half turn, held there.</summary>
    public void ToggleBack()
    {
        Spinning = false;
        Turn = Back ? 0f : MathF.PI;
        AudioManager.PlaySound("cursor");
    }

    /// <summary>The next clip (+1) or the one before (−1), from its start.</summary>
    public void NextClip(int delta = 1)
    {
        int n = Tab == GalleryTab.Pokemon ? PokemonAnimation.Clips.Length : Emotes.Count;
        ClipIndex = UiNav.Wrap(ClipIndex % n, delta, n);
        ClipAge = 0f;
        AudioManager.PlaySound("cursor");
    }

    /// <summary>The zoom a step in (+1) or out (−1), stopping at either end.</summary>
    public void ChangeZoom(int delta)
    {
        int next = Math.Clamp(ZoomStep + delta, 0, Zooms.Length - 1);
        if (next == ZoomStep) return;
        ZoomStep = next;
        AudioManager.PlaySound("cursor");
    }

    /// <summary>The A button: the Pokémon's cry and its clip again from the start (a person's emote again).</summary>
    public void Confirm()
    {
        ClipAge = 0f;
        if (Selected is { } e) AudioManager.PlayCry(e.Species, e.IsForm ? e.Look : null, CryMode.Normal);
        else if (SelectedPerson != null) AudioManager.PlaySound("select");
    }

    /// <summary>The B button: back to where the gallery was opened from.</summary>
    public void Cancel()
    {
        Close();
        AudioManager.PlaySound("cancel");
    }

    /// <summary>The screen's clock: the slide in, the turn by itself and the clip. Also keeps the model under the cursor asked for.</summary>
    public void Advance(float dt)
    {
        if (!IsActive) return;
        openAge += dt;
        ClipAge += dt;
        if (Spinning) Turn = Wrap(Turn + SpinSpeed * dt);
        KeepTheModelAsked();
    }

    private static float Wrap(float a)
    {
        float t = a % MathF.Tau;
        return t < 0f ? t + MathF.Tau : t;
    }

    /// <summary>The Pokémon's pose at this moment of its clip: a move's clip plays over <see cref="ClipSeconds"/> and is held for <see cref="HoldSeconds"/>.</summary>
    internal PokePose Pose()
    {
        string clip = Clip;
        if (PokemonAnimation.ClipLoops(clip)) return PokemonAnimation.ClipPose(clip, ClipAge);
        float cycle = ClipAge % (ClipSeconds + HoldSeconds);
        var pose = PokemonAnimation.ClipPose(clip, Math.Min(1f, cycle / ClipSeconds));
        pose.Time = ClipAge;
        return pose;
    }

    /// <summary>A person's pose: standing and breathing, with the emote begun again every <see cref="EmoteSeconds"/>.</summary>
    internal CharacterPose PersonPose() => new()
    {
        Time = ClipAge,
        Emote = Emote,
        EmoteTime = Emote == Emote.None ? 0f : ClipAge % EmoteSeconds
    };

    public void Update(float dt = 1f / 60f)
    {
        if (!IsActive) return;
        int dy = upDown.Advance(dt, InputManager.Axis(GameAction.Up, GameAction.Down), InputManager.Axis(GameAction.Up, GameAction.Down, held: true));
        for (int i = 0; i < Math.Abs(dy); i++) Move(Math.Sign(dy), upDown.Repeating);
        int turn = InputManager.Axis(GameAction.Left, GameAction.Right, held: true);
        if (turn != 0) Spin(turn * TurnSpeed * dt);
        if (InputManager.IsActionPressed(GameAction.Item)) NextClip(1);
        else if (InputManager.IsActionPressed(GameAction.Poketch)) ToggleBack();
        else if (InputManager.IsActionPressed(GameAction.PageLeft)) ChangeZoom(-1);
        else if (InputManager.IsActionPressed(GameAction.PageRight)) ChangeZoom(1);
        else if (InputManager.IsActionPressed(GameAction.Menu)) ChangeTab(1);
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
        else if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
        if (IsActive) Advance(dt);
    }

    // ------------------------------------------------------------------ drawing

    /// <summary>
    /// Renders the model under the cursor offscreen, if it is built (a person's rig if it is ready): never waits for
    /// either. Call outside any other texture mode, before the virtual screen's.
    /// </summary>
    internal void Render(RenderContext context)
    {
        if (!IsActive) return;
        context.EnsureLoaded();
        if (!pictureLoaded)
        {
            picture = Gpu.Target(PictureSize, PictureSize, "ui");
            Raylib.SetTextureFilter(picture.Texture, TextureFilter.Bilinear);
            pictureLoaded = true;
        }
        PictureShows = null;
        if (Selected is { } e && PokemonModels.TryGet(e.Look, out var model))
        {
            PokemonSprites.Render(context, model, SpriteView.Turntable, Pose(), picture, turn: Turn, zoom: Zoom * FrameShare);
            PictureShows = e.Look;
        }
        else if (SelectedPerson is { } person && CharacterModels.TryGet(person.Look, context.Shaders) != null)
        {
            CharacterStudio.Render(context, person.Look, picture, PersonPose(), Turn, 1.9f / (Zoom * FrameShare), 0.8f);
            PictureShows = person.Look;
        }
    }

    /// <summary>What the picture holds this frame: the look rendered, or null while the stand-in shows.</summary>
    public string? PictureShows { get; private set; }

    internal Texture2D? Picture => pictureLoaded ? picture.Texture : null;

    public void Draw(int screenWidth, int screenHeight) => ModernUi.DrawGallery(screenWidth, screenHeight, this, Appear);
}
