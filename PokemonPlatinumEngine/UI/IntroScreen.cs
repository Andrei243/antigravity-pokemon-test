using System;
using System.Collections.Generic;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
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
    AskName,
    EnterName,
    ConfirmName,
    Farewell,
    /// <summary>The picture closes to black round the player's sprite, which shrinks into the world.</summary>
    SendOff,
    Done
}

/// <summary>What the introduction looks like at this moment (everything a function of its phase and its clocks).</summary>
public readonly record struct IntroLook(float Backdrop, float Professor, float ProfessorShift, float Ball, float BallOpen, float Flash, float Pokemon,
    float Choice, float Dark, float SpriteScale);

/// <summary>
/// The new-game introduction (style guide, "The new-game introduction"): Professor Rowan welcomes the player
/// and shows them a Pokémon, as in Platinum, then asks who they are (a boy or a girl) and their name, and sends
/// them off. The lines are our own. Its logic takes no input (<see cref="Advance"/>, <see cref="PressConfirm"/>,
/// <see cref="PressCancel"/>, <see cref="Move"/>), so tests and the harness play it through; naming the rival
/// waits for the story's scripts (plan 02 · S4), which can ask with the same name entry.
/// </summary>
public class IntroScreen
{
    public const float FadeInTime = 1.2f, BallOpenTime = 1.5f, BallCloseTime = 0.6f, SendOffTime = 2.6f;

    /// <summary>The Pokémon the professor shows, as in Platinum.</summary>
    public const string ShownSpecies = "Buneary";

    public const string Professor = "Prof. Rowan";

    private static readonly string[] Greeting =
    {
        "Hello there! I'm glad you could come.",
        "Welcome to the world of Pokémon!",
        "My name is Rowan. People call me the Pokémon Professor."
    };

    private static readonly string[] World =
    {
        "This world is home to the creatures we call Pokémon. They live in the tall grass, in caves, in the sea and in the sky."
    };

    private static readonly string[] Alongside =
    {
        "This is one of them. Friendly, isn't it?",
        "We people live alongside Pokémon. Some of us keep them as friends and some battle together with them.",
        "As for me, I study them. There is still so much about them that nobody knows."
    };

    private static readonly string[] AboutYou =
    {
        "But that's enough about me. Tell me about yourself.",
        "Are you a boy, or are you a girl?"
    };

    private static readonly string[] AskName = { "And what is your name?" };

    private readonly DialogueManager talk = new();
    private float time, phaseTime;

    public bool IsActive { get; private set; }
    public IntroPhase Phase { get; private set; } = IntroPhase.Done;

    /// <summary>The character the cursor is on, and then the one chosen.</summary>
    public PlayerLook Look { get; private set; }

    /// <summary>The answer the cursor is on while a question is up ("Is that right?"): yes comes first.</summary>
    public bool AnswerYes { get; private set; } = true;

    /// <summary>The keyboard, while the name is being entered (and after, holding what was entered).</summary>
    public NameEntry? Entry { get; private set; }

    /// <summary>The name chosen: what was entered, or the character's own name if nothing was.</summary>
    public string Name => Entry?.Result(PlayerIdentity.DefaultName(Look)) ?? PlayerIdentity.DefaultName(Look);

    /// <summary>Seconds since the introduction began, and since its present phase did.</summary>
    public float Time => time;
    public float PhaseTime => phaseTime;

    /// <summary>The line being spoken, as far as it has been written, and whether it is all there.</summary>
    public string SpokenText => talk.IsActive ? talk.VisibleText : "";
    public string Speaker => talk.IsActive ? talk.Speaker : "";
    public bool LineComplete => talk.IsActive && talk.IsCurrentLineComplete;
    public bool Talking => talk.IsActive;

    public void Open(float charactersPerSecond = 45f)
    {
        IsActive = true;
        time = 0f;
        Look = PlayerLook.Boy;
        AnswerYes = true;
        Entry = null;
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
                Say("", new[] { Greeting[0], Greeting[1] }, () => Say(Professor, new[] { Greeting[2] }, () => Enter(IntroPhase.World)));
                break;
            case IntroPhase.World:
                Say(Professor, World, () => Enter(IntroPhase.BallOpens));
                break;
            case IntroPhase.Alongside:
                Say(Professor, Alongside, () => Enter(IntroPhase.BallCloses));
                break;
            case IntroPhase.AboutYou:
                Say(Professor, AboutYou, () => Enter(IntroPhase.ChooseLook));
                break;
            case IntroPhase.ConfirmLook:
            case IntroPhase.ConfirmName:
                AnswerYes = true;
                break;
            case IntroPhase.AskName:
                Say(Professor, AskName, () => Enter(IntroPhase.EnterName));
                break;
            case IntroPhase.EnterName:
                if (Entry == null) Entry = new NameEntry();
                else Entry.Reopen();
                break;
            case IntroPhase.Farewell:
                Say(Professor, new[]
                {
                    $"{Name}! That's a fine name.",
                    "Your very own story is about to begin. You will meet many Pokémon and many people on your way.",
                    "Now, off you go. The world of Pokémon is waiting!"
                }, () => Enter(IntroPhase.SendOff));
                break;
            case IntroPhase.Done:
                IsActive = false;
                break;
        }
    }

    private void Say(string speaker, IEnumerable<string> lines, Action then) => talk.ShowDialogue(speaker, lines, then);

    // ------------------------------------------------------------------ what the buttons do

    /// <summary>Runs the clocks: the lines being written and the phases that play out by themselves.</summary>
    public void Advance(float dt)
    {
        if (!IsActive) return;
        time += dt;
        phaseTime += dt;
        if (talk.IsActive) talk.Type(dt);

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
                Enter(AnswerYes ? IntroPhase.AskName : IntroPhase.ChooseLook);
                break;
            case IntroPhase.EnterName:
                var key = Entry!.Press();
                AudioManager.PlaySound(key == NameKey.Delete ? "cancel" : "select");
                if (Entry.Done) Enter(IntroPhase.ConfirmName);
                break;
            case IntroPhase.ConfirmName:
                AudioManager.PlaySound(AnswerYes ? "select" : "cancel");
                Enter(AnswerYes ? IntroPhase.Farewell : IntroPhase.EnterName);
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
        }
    }

    /// <summary>The Start button: on the keyboard, the cursor jumps to OK.</summary>
    public void PressStart()
    {
        if (IsActive && !talk.IsActive && Phase == IntroPhase.EnterName) Entry!.ToDone();
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
            case IntroPhase.ConfirmLook or IntroPhase.ConfirmName when dx != 0:
                AnswerYes = dx < 0;
                AudioManager.PlaySound("cursor");
                break;
            case IntroPhase.EnterName:
                Entry!.Move(dx, dy);
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
        bool choosing = Phase is IntroPhase.ChooseLook or IntroPhase.ConfirmLook;
        bool naming = Phase is IntroPhase.EnterName or IntroPhase.ConfirmName;

        float backdrop = Phase == IntroPhase.FadeIn ? UiMotion.EaseInOut(Ramp(t, 0.2f, 0.9f)) : 1f;
        float professor = Phase switch
        {
            IntroPhase.FadeIn => UiMotion.EaseInOut(Ramp(t, 0.5f, FadeInTime)),
            IntroPhase.ChooseLook => 1f - Ramp(t, 0f, 0.25f),
            IntroPhase.ConfirmLook or IntroPhase.EnterName or IntroPhase.ConfirmName => 0f,
            IntroPhase.AskName => Ramp(t, 0f, 0.3f),
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
            _ => 0f
        };
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
        float choice = choosing ? UiMotion.EaseOut(Phase == IntroPhase.ChooseLook ? Ramp(t, 0.1f, 0.45f) : 1f) : 0f;
        float dark = Phase switch
        {
            IntroPhase.SendOff => Ramp(t, 0.3f, 0.9f),
            IntroPhase.Done => 1f,
            _ => 0f
        };
        // The player's sprite stands in the dark, then shrinks away into the world
        float sprite = Phase == IntroPhase.SendOff ? Ramp(t, 0.5f, 0.8f) * (1f - UiMotion.EaseIn(Ramp(t, 1.3f, 2.3f))) : 0f;
        _ = naming;
        return new IntroLook(backdrop, professor, shift, ball, ballOpen, flash, pokemon, choice, dark, sprite);
    }

    // ------------------------------------------------------------------ drawing

    private const int FigureWidth = 900, FigureHeight = 1500, PokemonSize = 900, BallSize = 640;

    private RenderTexture2D professorPicture, boyPicture, girlPicture, pokemonPicture, ballPicture;
    private bool picturesLoaded;
    private Texture2D? sprite;

    /// <summary>Renders the 3D figures of this moment into their own pictures. Call outside any texture mode.</summary>
    internal void Render(RenderContext context)
    {
        if (!IsActive) return;
        context.EnsureLoaded();
        if (!picturesLoaded)
        {
            professorPicture = Raylib.LoadRenderTexture(FigureWidth, FigureHeight);
            boyPicture = Raylib.LoadRenderTexture(FigureWidth, FigureHeight);
            girlPicture = Raylib.LoadRenderTexture(FigureWidth, FigureHeight);
            pokemonPicture = Raylib.LoadRenderTexture(PokemonSize, PokemonSize);
            ballPicture = Raylib.LoadRenderTexture(BallSize, BallSize);
            foreach (var picture in new[] { professorPicture, boyPicture, girlPicture, pokemonPicture, ballPicture })
                Raylib.SetTextureFilter(picture.Texture, TextureFilter.Bilinear);
            picturesLoaded = true;
        }

        var look = Appearance();
        bool blink = (time + 1.3f) % 3.7f < 0.12f;
        if (look.Professor > 0.01f)
        {
            // He waves as he says hello, and nods at the player's name
            var pose = new CharacterPose { Time = time, Blink = blink };
            if (Phase == IntroPhase.Greeting && phaseTime < CharacterAnimation.Duration(Emote.Wave)) (pose.Emote, pose.EmoteTime) = (Emote.Wave, phaseTime);
            else if (Phase == IntroPhase.Farewell && phaseTime < CharacterAnimation.Duration(Emote.Nod)) (pose.Emote, pose.EmoteTime) = (Emote.Nod, phaseTime);
            CharacterStudio.Render(context, "ROWAN", professorPicture, pose, yaw: look.ProfessorShift * -0.35f);
        }
        if (look.Choice > 0.01f)
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
        if (look.Pokemon > 0.01f)
            PokemonSprites.Render(context, PokemonModels.Get(ShownSpecies), SpriteView.Front, new PokePose { Time = time }, pokemonPicture);
        // The ball turns a little toward the eye as it comes up
        if (look.Ball > 0.01f) CharacterStudio.RenderBall(context, ballPicture, "Poké Ball", look.BallOpen, 0.5f - 0.5f * look.Ball);

        // The field sprite of whoever was chosen, for the name entry and the send-off
        if (Phase is IntroPhase.EnterName or IntroPhase.ConfirmName or IntroPhase.Farewell or IntroPhase.SendOff)
            sprite = CharacterSprites.Portrait(context, PlayerIdentity.CharacterOf(Look));
    }

    public void Draw(int sw, int sh)
    {
        if (!IsActive && Phase != IntroPhase.Done) return;
        var look = Appearance();
        ModernUi.DrawIntro(sw, sh, this, look,
            picturesLoaded ? professorPicture.Texture : null, picturesLoaded ? boyPicture.Texture : null,
            picturesLoaded ? girlPicture.Texture : null, picturesLoaded ? pokemonPicture.Texture : null,
            picturesLoaded ? ballPicture.Texture : null, sprite);
    }
}
