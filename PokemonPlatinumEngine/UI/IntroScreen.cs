using System;
using System.Collections.Generic;
using System.Linq;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>Where the new-game introduction has got to.</summary>
public enum IntroPhase
{
    /// <summary>Black, then the backdrop and the professor fade up.</summary>
    FadeIn,
    Greeting,
    World,
    /// <summary>A ball opens beside the professor and a Pokémon appears.</summary>
    BallOpens,
    Alongside,
    /// <summary>The Pokémon goes back.</summary>
    BallCloses,
    AboutYou,
    ChooseLook,
    ConfirmLook,
    /// <summary>
    /// The clothes to set out in (plan 11 · C10): the look's own first, then the sets of <c>clothes.json</c> for that
    /// look, so a press goes on in the clothes the game always gave.
    /// </summary>
    ChooseOutfit,
    AskName,
    EnterName,
    ConfirmName,
    /// <summary>The professor shows the friend from next door and asks his name (Platinum asks it last).</summary>
    AskRival,
    EnterRival,
    ConfirmRival,
    Farewell,
    /// <summary>The picture closes to black round the player's sprite, which shrinks into the world.</summary>
    SendOff,
    Done
}

/// <summary>What the introduction looks like at this moment (everything a function of its phase and its clocks).</summary>
public readonly record struct IntroLook(float Backdrop, float Professor, float ProfessorShift, float Ball, float BallOpen, float Flash, float Pokemon,
    float Choice, float Dark, float SpriteScale, float Friend = 0f);

/// <summary>
/// The new-game introduction (style guide, "The new-game introduction"): Professor Rowan welcomes the player
/// and shows them a Pokémon, as in Platinum, then asks who they are (a boy or a girl) and their name, and sends
/// them off. The lines are our own. Its logic takes no input (<see cref="Advance"/>, <see cref="PressConfirm"/>,
/// <see cref="PressCancel"/>, <see cref="Move"/>), so tests and the harness play it through. Last, as in Platinum,
/// he shows the friend from next door, the rival, and asks his name (plan 02 · S4).
/// </summary>
public class IntroScreen
{
    public const float FadeInTime = 1.2f, BallOpenTime = 1.5f, BallCloseTime = 0.6f, SendOffTime = 2.6f;

    /// <summary>
    /// The region the new game starts in (plan 18 · W1): its professor, the Pokémon he shows, his lines and the name
    /// offered for the rival all come from its record. Sinnoh's until <see cref="Open"/> is handed another.
    /// </summary>
    public Region Region { get; private set; } = RegionDatabase.Get(RegionDatabase.Sinnoh)!;

    private RegionProfessor Who => Region.Professor ?? RegionDatabase.Get(RegionDatabase.Sinnoh)!.Professor!;

    /// <summary>The Pokémon the professor shows.</summary>
    public string ShownSpecies => Who.Species;

    /// <summary>The name over the professor's lines.</summary>
    public string Professor => Text.OfData(Who.Name);

    /// <summary>The character type the professor is drawn as.</summary>
    public string ProfessorLook => Who.Look;

    /// <summary>Whether the region's rival is a friend to name (false where he is the player's other hero).</summary>
    public bool NamesARival => Region.Rival != null;

    /// <summary>The question that asks the player's look, shown under the choice too.</summary>
    public string BoyOrGirl => Shown(Said.BoyOrGirl);

    /// <summary>The name offered for the rival, and taken when none is entered.</summary>
    public string DefaultRival => Region.RivalFor(Look) ?? PlayerIdentity.DefaultRivalName;

    /// <summary>The character type the rival is drawn as.</summary>
    public const string Rival = "RIVAL";

    // The professor's lines are the region's (plan 18 · W1, `Data/regions/<id>.json`)
    private RegionIntroLines Said => Who.Lines;

    private string[] Greeting => new[] { Shown(Said.Hello), Shown(Said.Welcome), Shown(Said.MyName) };

    private string[] World => new[] { Shown(Said.World) };

    private string[] Alongside => new[] { Shown(Said.OneOfThem), Shown(Said.LiveAlongside), Shown(Said.Study) };

    private string[] AboutYou => new[] { Shown(Said.Enough), Shown(Said.BoyOrGirl) };

    private string[] AskName => new[] { Shown(Said.AskName) };

    private string[] AboutRival => new[] { Shown(Said.NextDoor), Shown(Said.NeverStill) };

    private static string Shown(string line) => Text.OfData(line);

    // The line is looked up as the region file writes it, placeholders and all, and filled afterwards (plan 17 · N6)
    private string Fill(string line) => Shown(line).Replace("{name}", Name).Replace("{rival}", RivalName);

    private readonly DialogueManager talk = new();
    private float time, phaseTime;

    public bool IsActive { get; private set; }
    public IntroPhase Phase { get; private set; } = IntroPhase.Done;

    /// <summary>The character the cursor is on, and then the one chosen.</summary>
    public PlayerLook Look { get; private set; }

    /// <summary>
    /// The outfits offered for <see cref="Look"/>: the look's own clothes first (<see cref="Outfit.Own"/>), then each
    /// set of <c>clothes.json</c> that suits it.
    /// </summary>
    public IReadOnlyList<Outfit> Outfits => outfits;
    private List<Outfit> outfits = new() { Outfit.Own };

    /// <summary>The names over <see cref="Outfits"/>, in the same order.</summary>
    public IReadOnlyList<string> OutfitNames => outfitNames;
    private List<string> outfitNames = new() { "" };

    /// <summary>The outfit the cursor is on, and then the one chosen.</summary>
    public int OutfitIndex { get; private set; }

    /// <summary>The outfit chosen (the look's own unless another was picked), which the new game is begun in.</summary>
    public Outfit Outfit => outfits[Math.Clamp(OutfitIndex, 0, outfits.Count - 1)];

    /// <summary>The character the player is drawn as from the outfit's choice on: the look, dressed as chosen.</summary>
    public string Dressed => Outfit.Dress(PlayerIdentity.CharacterOf(Look), Outfit);

    /// <summary>The answer the cursor is on while a question is up ("Is that right?"): yes comes first.</summary>
    public bool AnswerYes { get; private set; } = true;

    /// <summary>The keyboard, while the name is being entered (and after, holding what was entered).</summary>
    public NameEntry? Entry { get; private set; }

    /// <summary>The name chosen: what was entered, or the character's own name if nothing was.</summary>
    public string Name => Entry?.Result(PlayerIdentity.DefaultName(Look)) ?? PlayerIdentity.DefaultName(Look);

    /// <summary>The keyboard while the rival is being named, and after.</summary>
    public NameEntry? RivalEntry { get; private set; }

    /// <summary>The rival's name: what was entered, or his own if nothing was.</summary>
    public string RivalName => RivalEntry?.Result(DefaultRival) ?? DefaultRival;

    /// <summary>Seconds since the introduction began, and since its present phase did.</summary>
    public float Time => time;
    public float PhaseTime => phaseTime;

    /// <summary>The line being spoken, as far as it has been written, and whether it is all there.</summary>
    public string SpokenText => talk.IsActive ? talk.VisibleText : "";
    public string Speaker => talk.IsActive ? talk.Speaker : "";
    public bool LineComplete => talk.IsActive && talk.IsCurrentLineComplete;
    public bool Talking => talk.IsActive;

    public void Open(float charactersPerSecond = 45f, Region? region = null)
    {
        Region = region ?? RegionDatabase.Get(RegionDatabase.Sinnoh)!;
        IsActive = true;
        time = 0f;
        Look = PlayerLook.Boy;
        OfferOutfits();
        AnswerYes = true;
        Entry = null;
        RivalEntry = null;
        talk.CharactersPerSecond = charactersPerSecond;
        Enter(IntroPhase.FadeIn);
    }

    public void Close()
    {
        IsActive = false;
        Phase = IntroPhase.Done;
    }

    private void Enter(IntroPhase phase)
    {
        Phase = phase;
        phaseTime = 0f;
        switch (phase)
        {
            case IntroPhase.Greeting:
                // He hasn't said who he is until the last line of his greeting
                var greeting = Greeting;
                Say("", new[] { greeting[0], greeting[1] }, Voices(nameof(RegionIntroLines.Hello), nameof(RegionIntroLines.Welcome)),
                    () => Say(Professor, new[] { greeting[2] }, Voices(nameof(RegionIntroLines.MyName)), () => Enter(IntroPhase.World)));
                break;
            case IntroPhase.World:
                Say(Professor, World, Voices(nameof(RegionIntroLines.World)), () => Enter(IntroPhase.BallOpens));
                break;
            case IntroPhase.Alongside:
                Say(Professor, Alongside, Voices(nameof(RegionIntroLines.OneOfThem), nameof(RegionIntroLines.LiveAlongside), nameof(RegionIntroLines.Study)), () => Enter(IntroPhase.BallCloses));
                break;
            case IntroPhase.AboutYou:
                Say(Professor, AboutYou, Voices(nameof(RegionIntroLines.Enough), nameof(RegionIntroLines.BoyOrGirl)), () => Enter(IntroPhase.ChooseLook));
                break;
            case IntroPhase.ConfirmLook:
            case IntroPhase.ConfirmName:
            case IntroPhase.ConfirmRival:
                AnswerYes = true;
                break;
            case IntroPhase.AskName:
                Say(Professor, AskName, Voices(nameof(RegionIntroLines.AskName)), () => Enter(IntroPhase.EnterName));
                break;
            case IntroPhase.EnterName:
                if (Entry == null) Entry = new NameEntry();
                else Entry.Reopen();
                break;
            case IntroPhase.AskRival:
                Say(Professor, new[] { Fill(Said.FineName) }.Concat(AboutRival),
                    Voices(nameof(RegionIntroLines.FineName), nameof(RegionIntroLines.NextDoor), nameof(RegionIntroLines.NeverStill)), () => Enter(IntroPhase.EnterRival));
                break;
            case IntroPhase.EnterRival:
                if (RivalEntry == null) RivalEntry = new NameEntry();
                else RivalEntry.Reopen();
                break;
            case IntroPhase.Farewell:
                var parting = new List<string>();
                var partingVoices = new List<string>();
                if (NamesARival) { parting.Add(Fill(Said.OfCourse)); partingVoices.Add(nameof(RegionIntroLines.OfCourse)); }
                parting.Add(Fill(Said.Story));
                parting.Add(Shown(Said.OffYouGo));
                partingVoices.Add(nameof(RegionIntroLines.Story));
                partingVoices.Add(nameof(RegionIntroLines.OffYouGo));
                Say(Professor, parting, Voices(partingVoices.ToArray()), () => Enter(IntroPhase.SendOff));
                break;
            case IntroPhase.Done:
                IsActive = false;
                break;
        }
    }

    private void Say(string speaker, IEnumerable<string> lines, IReadOnlyList<SpokenLine?> voices, Action then) => talk.ShowDialogue(speaker, lines, voices, then);

    /// <summary>The voices of the introduction's box (plan 26 · V6): the game sets their mode and pack as it does the field's.</summary>
    public VoiceTalk Voice => talk.Voice;

    /// <summary>
    /// The professor's lines as the voices know them (plan 26 · V6): <c>intro:&lt;region&gt;.&lt;line&gt;.0</c>, the catalogue's
    /// keys, the English of the region's file and the cast's voice of the professor by name.
    /// </summary>
    public IReadOnlyList<SpokenLine?> Voices(params string[] lines)
    {
        string? voice = VoiceCatalogue.IntroVoice(VoiceCast.Default, Who.Name, Region.Id);
        return lines.Select(name => typeof(RegionIntroLines).GetProperty(name)?.GetValue(Said) is string { Length: > 0 } english
            ? new SpokenLine(VoiceCatalogue.IntroKey(Region.Id, name), english, voice)
            : null).ToList();
    }

    /// <summary>The outfits for the look chosen, the cursor back on its own clothes.</summary>
    private void OfferOutfits()
    {
        var sets = ClothingDatabase.SetsFor(Look);
        outfits = new List<Outfit> { Outfit.Own };
        outfits.AddRange(sets.Select(s => s.Outfit));
        outfitNames = new List<string> { Text.Of("intro.outfit.own") };
        outfitNames.AddRange(sets.Select(s => s.Name));
        OutfitIndex = 0;
    }

    /// <summary>The question asked while the clothes are chosen, shown under the choice.</summary>
    public string WhichClothes => Text.Of("intro.outfit.question");

    // ------------------------------------------------------------------ what the buttons do

    /// <summary>Runs the clocks: the lines being written and the phases that play out by themselves.</summary>
    public void Advance(float dt)
    {
        if (!IsActive) return;
        time += dt;
        phaseTime += dt;
        if (talk.IsActive) talk.Type(dt);
        talk.Voice.Advance(dt);

        switch (Phase)
        {
            case IntroPhase.FadeIn when phaseTime >= FadeInTime:
                Enter(IntroPhase.Greeting);
                break;
            case IntroPhase.BallOpens when phaseTime >= BallOpenTime:
                Enter(IntroPhase.Alongside);
                break;
            case IntroPhase.BallCloses when phaseTime >= BallCloseTime:
                Enter(IntroPhase.AboutYou);
                break;
            case IntroPhase.SendOff when phaseTime >= SendOffTime:
                Enter(IntroPhase.Done);
                break;
        }
    }

    /// <summary>
    /// The A button: finishes the line being written, or goes on to the next; picks the character, presses the
    /// key under the keyboard's cursor, answers a question.
    /// </summary>
    public void PressConfirm()
    {
        if (!IsActive) return;
        if (talk.IsActive)
        {
            if (!talk.IsCurrentLineComplete) talk.FinishLine();
            else talk.Advance();
            return;
        }

        switch (Phase)
        {
            case IntroPhase.ChooseLook:
                AudioManager.PlaySound("select");
                Enter(IntroPhase.ConfirmLook);
                break;
            case IntroPhase.ConfirmLook:
                AudioManager.PlaySound(AnswerYes ? "select" : "cancel");
                if (AnswerYes) OfferOutfits();
                Enter(AnswerYes ? IntroPhase.ChooseOutfit : IntroPhase.ChooseLook);
                break;
            case IntroPhase.ChooseOutfit:
                AudioManager.PlaySound("select");
                Enter(IntroPhase.AskName);
                break;
            case IntroPhase.EnterName:
                var key = Entry!.Press();
                AudioManager.PlaySound(key == NameKey.Delete ? "cancel" : "select");
                if (Entry.Done) Enter(IntroPhase.ConfirmName);
                break;
            case IntroPhase.ConfirmName:
                AudioManager.PlaySound(AnswerYes ? "select" : "cancel");
                Enter(AnswerYes ? (NamesARival ? IntroPhase.AskRival : IntroPhase.Farewell) : IntroPhase.EnterName);
                break;
            case IntroPhase.EnterRival:
                var letter = RivalEntry!.Press();
                AudioManager.PlaySound(letter == NameKey.Delete ? "cancel" : "select");
                if (RivalEntry.Done) Enter(IntroPhase.ConfirmRival);
                break;
            case IntroPhase.ConfirmRival:
                AudioManager.PlaySound(AnswerYes ? "select" : "cancel");
                Enter(AnswerYes ? IntroPhase.Farewell : IntroPhase.EnterRival);
                break;
        }
    }

    /// <summary>The B button: finishes a line, deletes a letter, or answers no.</summary>
    public void PressCancel()
    {
        if (!IsActive) return;
        if (talk.IsActive)
        {
            if (!talk.IsCurrentLineComplete) talk.FinishLine();
            return;
        }

        switch (Phase)
        {
            case IntroPhase.ConfirmLook:
            case IntroPhase.ChooseOutfit:
                AudioManager.PlaySound("cancel");
                Enter(IntroPhase.ChooseLook);
                break;
            case IntroPhase.EnterName:
                if (Entry!.Backspace()) AudioManager.PlaySound("cancel");
                break;
            case IntroPhase.ConfirmName:
                AudioManager.PlaySound("cancel");
                Enter(IntroPhase.EnterName);
                break;
            case IntroPhase.EnterRival:
                if (RivalEntry!.Backspace()) AudioManager.PlaySound("cancel");
                break;
            case IntroPhase.ConfirmRival:
                AudioManager.PlaySound("cancel");
                Enter(IntroPhase.EnterRival);
                break;
        }
    }

    /// <summary>The Start button: on the keyboard, the cursor jumps to OK.</summary>
    public void PressStart()
    {
        if (!IsActive || talk.IsActive) return;
        if (Phase == IntroPhase.EnterName) Entry!.ToDone();
        else if (Phase == IntroPhase.EnterRival) RivalEntry!.ToDone();
    }

    // ---------------------------------------------------------------- the pointer (plan 12 · Q6)

    /// <summary>
    /// What lies under a point: anywhere while the professor speaks (a click reads on); the boy (0) or the girl (1)
    /// while the player chooses; YES (0) or the other answer (1) of a question; a key of the keyboard (its row times
    /// 16 plus its column) while a name is typed.
    /// </summary>
    public int? HitTest(System.Numerics.Vector2 at)
    {
        if (!IsActive) return null;
        if (talk.IsActive) return UiPointer.Anywhere;
        int sw = GameEngine.VirtualWidth, sh = GameEngine.VirtualHeight;
        switch (Phase)
        {
            case IntroPhase.ChooseLook:
                for (int i = 0; i < 2; i++)
                    if (UiPointer.Inside(new Rectangle(sw / 2f + (i == 0 ? -500 - 30 : 30), 96, 500, 640), at)) return i;
                return null;
            case IntroPhase.ConfirmLook or IntroPhase.ConfirmName or IntroPhase.ConfirmRival:
                return UiPointer.Hit(ModernUi.PromptButtons(sw, sh, 2), at);
            case IntroPhase.ChooseOutfit:
                // The card (a click takes the outfit on it; the arrows change it)
                return UiPointer.Inside(ModernUi.IntroOutfitCard(sw), at) ? 0 : null;
            case IntroPhase.EnterName or IntroPhase.EnterRival when (Phase == IntroPhase.EnterName ? Entry : RivalEntry) is { } entry:
                return ModernUi.HitRow(ModernUi.KeyboardKeys(ModernUi.NameBoardRect(sw), entry), at);
            default:
                return null;
        }
    }

    /// <summary>A hover: the character, the answer or the key under the pointer.</summary>
    public void PointAt(int index)
    {
        if (!IsActive || talk.IsActive || index == UiPointer.Anywhere) return;
        switch (Phase)
        {
            case IntroPhase.ChooseLook when (index == 1 ? PlayerLook.Girl : PlayerLook.Boy) != Look:
                Move(index == 1 ? 1 : -1, 0);
                break;
            case IntroPhase.ConfirmLook or IntroPhase.ConfirmName or IntroPhase.ConfirmRival when (index == 0) != AnswerYes:
                Move(index == 0 ? -1 : 1, 0);
                break;
            case IntroPhase.EnterName or IntroPhase.EnterRival:
                if ((Phase == IntroPhase.EnterName ? Entry : RivalEntry)?.PointAt(index / 16, index % 16) == true) AudioManager.PlaySound("cursor");
                break;
        }
    }

    /// <summary>The arrows: between the two characters, over the keyboard, between the answers.</summary>
    public void Move(int dx, int dy)
    {
        if (!IsActive || talk.IsActive || (dx == 0 && dy == 0)) return;
        switch (Phase)
        {
            case IntroPhase.ChooseLook when dx != 0:
                Look = dx > 0 ? PlayerLook.Girl : PlayerLook.Boy;
                AudioManager.PlaySound("cursor");
                break;
            case IntroPhase.ChooseOutfit when dx != 0 && outfits.Count > 1:
                // Round and round: past the last set is the look's own clothes again
                OutfitIndex = ((OutfitIndex + Math.Sign(dx)) % outfits.Count + outfits.Count) % outfits.Count;
                AudioManager.PlaySound("cursor");
                break;
            case IntroPhase.ConfirmLook or IntroPhase.ConfirmName or IntroPhase.ConfirmRival when dx != 0:
                AnswerYes = dx < 0;
                AudioManager.PlaySound("cursor");
                break;
            case IntroPhase.EnterName:
                Entry!.Move(dx, dy);
                AudioManager.PlaySound("cursor");
                break;
            case IntroPhase.EnterRival:
                RivalEntry!.Move(dx, dy);
                AudioManager.PlaySound("cursor");
                break;
        }
    }

    public void Update(float dt)
    {
        if (!IsActive) return;
        int dx = (InputManager.IsActionPressed(GameAction.Right) ? 1 : 0) - (InputManager.IsActionPressed(GameAction.Left) ? 1 : 0);
        int dy = (InputManager.IsActionPressed(GameAction.Down) ? 1 : 0) - (InputManager.IsActionPressed(GameAction.Up) ? 1 : 0);
        if (dx != 0 || dy != 0) Move(dx, dy);
        else if (InputManager.IsActionPressed(GameAction.Confirm)) PressConfirm();
        else if (InputManager.IsActionPressed(GameAction.Cancel)) PressCancel();
        else if (InputManager.IsActionPressed(GameAction.Menu)) PressStart();
        Advance(dt);
    }

    // ------------------------------------------------------------------ what it looks like

    private static float Ramp(float t, float from, float to) => Math.Clamp((t - from) / (to - from), 0f, 1f);

    /// <summary>The picture at this moment: how far everything has faded, moved, opened and shrunk.</summary>
    public IntroLook Appearance()
    {
        float t = phaseTime;
        bool withPokemon = Phase is IntroPhase.BallOpens or IntroPhase.Alongside or IntroPhase.BallCloses;
        bool choosing = Phase is IntroPhase.ChooseLook or IntroPhase.ConfirmLook or IntroPhase.ChooseOutfit;
        bool naming = Phase is IntroPhase.EnterName or IntroPhase.ConfirmName;

        float backdrop = Phase == IntroPhase.FadeIn ? UiMotion.EaseInOut(Ramp(t, 0.2f, 0.9f)) : 1f;
        float professor = Phase switch
        {
            IntroPhase.FadeIn => UiMotion.EaseInOut(Ramp(t, 0.5f, FadeInTime)),
            IntroPhase.ChooseLook => 1f - Ramp(t, 0f, 0.25f),
            IntroPhase.ConfirmLook or IntroPhase.ChooseOutfit or IntroPhase.EnterName or IntroPhase.ConfirmName or IntroPhase.EnterRival or IntroPhase.ConfirmRival => 0f,
            IntroPhase.AskName or IntroPhase.AskRival => Ramp(t, 0f, 0.3f),
            IntroPhase.SendOff => 1f - Ramp(t, 0f, 0.5f),
            IntroPhase.Done => 0f,
            _ => 1f
        };
        // He steps aside to make room for the Pokémon, and back once it has gone
        float shift = Phase switch
        {
            IntroPhase.BallOpens => UiMotion.EaseInOut(Ramp(t, 0f, 0.4f)),
            IntroPhase.Alongside => 1f,
            IntroPhase.BallCloses => 1f - UiMotion.EaseInOut(Ramp(t, 0.2f, BallCloseTime)),
            // and for his young friend, who comes up beside him
            IntroPhase.AskRival => UiMotion.EaseInOut(Ramp(t, 0f, 0.4f)),
            _ => 0f
        };
        float friend = Phase == IntroPhase.AskRival ? UiMotion.EaseOut(Ramp(t, 0.2f, 0.6f)) : 0f;
        // The ball comes up, opens in a flash of white light, and the Pokémon grows out of the light as it goes
        float ball = Phase == IntroPhase.BallOpens ? UiMotion.EaseOut(Ramp(t, 0.25f, 0.5f)) * (1f - Ramp(t, 0.95f, 1.1f)) : 0f;
        float ballOpen = Phase == IntroPhase.BallOpens ? UiMotion.EaseOut(Ramp(t, 0.65f, 0.85f)) : 0f;
        float flash = Phase switch
        {
            IntroPhase.BallOpens => Ramp(t, 0.65f, 0.8f) * (1f - Ramp(t, 0.9f, 1.35f)),
            IntroPhase.BallCloses => Ramp(t, 0f, 0.15f) * (1f - Ramp(t, 0.2f, 0.5f)),
            _ => 0f
        };
        float pokemon = !withPokemon ? 0f : Phase switch
        {
            IntroPhase.BallOpens => UiMotion.EaseOut(Ramp(t, 0.85f, 1.35f)),
            IntroPhase.BallCloses => 1f - UiMotion.EaseIn(Ramp(t, 0.05f, 0.3f)),
            _ => 1f
        };
        // The outfit's card rises in as the two cards did; the two give way to it at once
        float choice = choosing ? UiMotion.EaseOut(Phase is IntroPhase.ChooseLook or IntroPhase.ChooseOutfit ? Ramp(t, 0.1f, 0.45f) : 1f) : 0f;
        float dark = Phase switch
        {
            IntroPhase.SendOff => Ramp(t, 0.3f, 0.9f),
            IntroPhase.Done => 1f,
            _ => 0f
        };
        // The player's sprite stands in the dark, then shrinks away into the world
        float sprite = Phase == IntroPhase.SendOff ? Ramp(t, 0.5f, 0.8f) * (1f - UiMotion.EaseIn(Ramp(t, 1.3f, 2.3f))) : 0f;
        _ = naming;
        return new IntroLook(backdrop, professor, shift, ball, ballOpen, flash, pokemon, choice, dark, sprite, friend);
    }

    // ------------------------------------------------------------------ drawing

    private const int FigureWidth = 900, FigureHeight = 1500, PokemonSize = 900, BallSize = 640;

    private RenderTexture2D professorPicture, boyPicture, girlPicture, pokemonPicture, ballPicture, friendPicture;
    private bool picturesLoaded;
    private Texture2D? sprite;

    /// <summary>Renders the 3D figures of this moment into their own pictures. Call outside any texture mode.</summary>
    internal void Render(RenderContext context)
    {
        if (!IsActive) return;
        context.EnsureLoaded();
        if (!picturesLoaded)
        {
            professorPicture = Gpu.Target(FigureWidth, FigureHeight, "ui");
            boyPicture = Gpu.Target(FigureWidth, FigureHeight, "ui");
            girlPicture = Gpu.Target(FigureWidth, FigureHeight, "ui");
            pokemonPicture = Gpu.Target(PokemonSize, PokemonSize, "ui");
            ballPicture = Gpu.Target(BallSize, BallSize, "ui");
            friendPicture = Gpu.Target(FigureWidth, FigureHeight, "ui");
            foreach (var picture in new[] { professorPicture, boyPicture, girlPicture, pokemonPicture, ballPicture, friendPicture })
                Raylib.SetTextureFilter(picture.Texture, TextureFilter.Bilinear);
            picturesLoaded = true;
        }

        var look = Appearance();
        bool blink = (time + 1.3f) % 3.7f < 0.12f;
        if (look.Professor > 0.01f)
        {
            // He waves as he says hello, and nods at the player's name
            // His mouth moves with his voice (plan 26 · V6)
            var pose = new CharacterPose { Time = time, Blink = blink, MouthOpen = talk.Voice.MouthOpen };
            if (Phase == IntroPhase.Greeting && phaseTime < CharacterAnimation.Duration(Emote.Wave)) (pose.Emote, pose.EmoteTime) = (Emote.Wave, phaseTime);
            else if (Phase == IntroPhase.Farewell && phaseTime < CharacterAnimation.Duration(Emote.Nod)) (pose.Emote, pose.EmoteTime) = (Emote.Nod, phaseTime);
            CharacterStudio.Render(context, ProfessorLook, professorPicture, pose, yaw: look.ProfessorShift * -0.35f);
        }
        if (Phase == IntroPhase.ChooseOutfit)
        {
            // The look chosen in the outfit under the cursor: one not built yet is started in the background and the
            // last one shown stays meanwhile (the look's own clothes, built for the choice before), as at the wardrobe
            string wanted = Dressed;
            tried.Add(wanted);
            string plain = PlayerIdentity.CharacterOf(Look);
            if (CharacterModels.TryGet(wanted, context.Shaders) != null) shownOutfit = wanted;
            else if (shownOutfit == null || !shownOutfit.StartsWith(plain, StringComparison.Ordinal)
                     || CharacterModels.TryGet(shownOutfit, context.Shaders) == null)
                shownOutfit = CharacterModels.TryGet(plain, context.Shaders) != null ? plain : null;
            if (shownOutfit != null)
            {
                float wave = CharacterAnimation.Duration(Emote.Wave);
                var pose = new CharacterPose { Time = time, Blink = (time + 0.5f) % 3.3f < 0.12f };
                (pose.Emote, pose.EmoteTime) = (Emote.Wave, phaseTime % (wave + 1.6f));
                if (pose.EmoteTime >= wave) pose.Emote = Emote.None;
                CharacterStudio.Render(context, shownOutfit, Look == PlayerLook.Girl ? girlPicture : boyPicture, pose);
            }
        }
        else if (look.Choice > 0.01f)
        {
            foreach (var (picture, which) in new[] { (boyPicture, PlayerLook.Boy), (girlPicture, PlayerLook.Girl) })
            {
                var pose = new CharacterPose { Time = time + (which == PlayerLook.Girl ? 0.8f : 0f), Blink = (time + (int)which * 1.1f) % 3.3f < 0.12f };
                // The one the cursor is on waves, over and over
                if (which == Look)
                {
                    float wave = CharacterAnimation.Duration(Emote.Wave);
                    (pose.Emote, pose.EmoteTime) = (Emote.Wave, phaseTime % (wave + 0.6f));
                    if (pose.EmoteTime >= wave) pose.Emote = Emote.None;
                }
                CharacterStudio.Render(context, PlayerIdentity.CharacterOf(which), picture, pose);
            }
        }
        if (look.Friend > 0.01f)
        {
            // He can't keep still: a wave as soon as he's shown
            var pose = new CharacterPose { Time = time + 0.4f, Blink = (time + 0.7f) % 3.1f < 0.12f };
            if (phaseTime < CharacterAnimation.Duration(Emote.Wave)) (pose.Emote, pose.EmoteTime) = (Emote.Wave, phaseTime);
            CharacterStudio.Render(context, Rival, friendPicture, pose, yaw: 0.3f);
        }
        if (look.Pokemon > 0.01f)
            PokemonSprites.Render(context, PokemonModels.Get(ShownSpecies), SpriteView.Front, new PokePose { Time = time }, pokemonPicture);
        // The ball turns a little toward the eye as it comes up
        if (look.Ball > 0.01f) CharacterStudio.RenderBall(context, ballPicture, "Poké Ball", look.BallOpen, 0.5f - 0.5f * look.Ball);

        // The field sprite of whoever is being named, for the name entry, and the player's for the send-off
        if (Phase is IntroPhase.EnterRival or IntroPhase.ConfirmRival)
            sprite = CharacterSprites.Portrait(context, Rival);
        else if (Phase is IntroPhase.EnterName or IntroPhase.ConfirmName or IntroPhase.Farewell or IntroPhase.SendOff)
            sprite = CharacterSprites.Portrait(context, Dressed);
    }

    // The looks the outfit's choice asked to be built, and the one on show
    private readonly HashSet<string> tried = new(StringComparer.OrdinalIgnoreCase);
    private string? shownOutfit;

    /// <summary>True while the figure on the outfit's card isn't yet the outfit under the cursor (it is being made).</summary>
    public bool Dressing => !string.Equals(shownOutfit, Dressed, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The dressed looks the choice built and the game didn't keep, for the engine to let go of once the introduction
    /// is over (as the wardrobe's, <see cref="WardrobeScreen.TakeTried"/>); asking empties the list.
    /// </summary>
    public IEnumerable<string> TakeTried()
    {
        string kept = Dressed;
        var left = tried.Where(t => !t.Equals(kept, StringComparison.OrdinalIgnoreCase) && t.Contains('@')).ToList();
        tried.Clear();
        shownOutfit = null;
        return left;
    }

    public void Draw(int sw, int sh)
    {
        if (!IsActive && Phase != IntroPhase.Done) return;
        var look = Appearance();
        ModernUi.DrawIntro(sw, sh, this, look,
            picturesLoaded ? professorPicture.Texture : null, picturesLoaded ? boyPicture.Texture : null,
            picturesLoaded ? girlPicture.Texture : null, picturesLoaded ? pokemonPicture.Texture : null,
            picturesLoaded ? ballPicture.Texture : null, sprite, picturesLoaded ? friendPicture.Texture : null);
    }
}
