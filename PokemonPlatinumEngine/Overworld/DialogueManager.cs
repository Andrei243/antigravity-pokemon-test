using System;
using System.Collections.Generic;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.Overworld;

public class DialogueManager
{
    private readonly Queue<string> lineQueue = new();
    private RichLine current = Empty;
    private string currentSpeaker = "";
    private int charIndex = 0;
    private float charProgress = 0f;
    private Action? onCompleteCallback;

    private static readonly RichLine Empty = new("", Array.Empty<RunSpan>(), Array.Empty<TextEvent>());

    // Where the typing is in the line's codes (plan 09 · L3): the next event, a pause still to wait out, the pace in force
    private int nextEvent;
    private float pauseLeft;
    private float speed = 1f;

    /// <summary>How fast lines are written out, in characters a second: the options' text speed.</summary>
    public float CharactersPerSecond { get; set; } = GameSettings.CharactersPerSecond(TextSpeed.Normal);

    // A question stays on the box, fully written, until it has its answer
    private bool question;

    /// <summary>The lines written so far, kept to be read again (the backlog).</summary>
    public DialogueLog Log { get; } = new();

    /// <summary>True while the backlog is open over a line that waits.</summary>
    public bool BacklogOpen { get; private set; }

    /// <summary>The backlog's first row on show: it opens on the newest.</summary>
    public int BacklogTop { get; private set; }

    /// <summary>How many rows of the backlog the panel shows.</summary>
    public const int BacklogRows = 5;

    public bool IsActive => current.Plain.Length > 0 || lineQueue.Count > 0;
    public bool IsCurrentLineComplete => charIndex >= current.Plain.Length;

    /// <summary>True once a question is written out and waits for its answer: the box stays as it is.</summary>
    public bool AwaitingAnswer => question && IsActive && IsCurrentLineComplete;

    /// <summary>True while the line shown is a question, written out or not.</summary>
    public bool IsQuestion => question && IsActive;

    /// <summary>The whole line being typed out as it reads (its codes taken out), without the speaker's name.</summary>
    public string CurrentLine => current.Plain;

    /// <summary>The colours of the line's stretches, for whoever draws it.</summary>
    public IReadOnlyList<RunSpan> CurrentSpans => current.Spans;

    public void ShowDialogue(string speaker, IEnumerable<string> lines, Action? onComplete = null)
    {
        // Written lines name the player and the professor's assistant with {player} and {assistant}
        currentSpeaker = PlayerIdentity.Fill(speaker);
        question = false;
        BacklogOpen = false;
        lineQueue.Clear();
        foreach (var l in lines) lineQueue.Enqueue(l);
        onCompleteCallback = onComplete;
        AdvanceLine();
    }

    public void ShowDialogue(string speaker, string singleLine, Action? onComplete = null)
    {
        ShowDialogue(speaker, new[] { singleLine }, onComplete);
    }

    /// <summary>
    /// Shows a line that is a question: it is written out like any other and then stays, with no arrow to go on,
    /// until <see cref="Close"/>. Whoever asked shows the answers beside it and closes the box on the one picked.
    /// </summary>
    public void ShowQuestion(string speaker, string line)
    {
        ShowDialogue(speaker, new[] { line });
        question = true;
    }

    /// <summary>Takes the box away at once, whatever was on it. Nothing is called.</summary>
    public void Close()
    {
        question = false;
        BacklogOpen = false;
        lineQueue.Clear();
        current = Empty;
        currentSpeaker = "";
        onCompleteCallback = null;
    }

    /// <summary>
    /// A line as the box writes it: the player's name in a colour by their look (Platinum's convention, blue for a boy
    /// and red for a girl; <see cref="PlayerIdentity.Fill"/> itself stays plain), the other placeholders filled, then the
    /// markup parsed.
    /// </summary>
    public static RichLine Prepare(string raw, string speaker) => Prepare(raw, speaker, PlayerIdentity.Name, PlayerIdentity.Look);

    /// <summary>As <see cref="Prepare(string, string)"/> for a given player, which a test uses (the identity is the whole process's).</summary>
    public static RichLine Prepare(string raw, string speaker, string name, PlayerLook look)
    {
        string colour = look == PlayerLook.Boy ? "blue" : "red";
        string coloured = name.Length > 0 && raw.Contains("{player}") ? raw.Replace("{player}", "{" + colour + ":" + name + "}") : raw;
        var line = RichText.Parse(PlayerIdentity.Fill(coloured, name, look));

        // The speaker's name is on the tag above the box, so a line written as "Barry: Hey!" drops the prefix
        string prefix = speaker + ": ";
        if (speaker.Length > 0 && line.Plain.StartsWith(prefix, StringComparison.Ordinal)) line = line.Skip(prefix.Length);
        return line;
    }

    private void AdvanceLine()
    {
        if (lineQueue.Count > 0)
        {
            current = Prepare(lineQueue.Dequeue(), currentSpeaker);
            charIndex = 0;
            charProgress = 0f;
            nextEvent = 0;
            pauseLeft = 0f;
            speed = 1f;
            autoAdvanceWaited = 0f;
            Log.Add(currentSpeaker, current.Plain);
            AudioManager.PlaySound("text");
        }
        else
        {
            current = Empty;
            currentSpeaker = "";
            var cb = onCompleteCallback;
            onCompleteCallback = null;
            cb?.Invoke();
        }
    }

    public void Update(float dt)
    {
        if (!IsActive) return;

        if (BacklogOpen)
        {
            UpdateBacklog();
            return;
        }

        if (!IsCurrentLineComplete)
        {
            Type(dt);
            if (InputManager.IsActionPressed(GameAction.Confirm) || InputManager.IsActionPressed(GameAction.Cancel))
            {
                FinishLine();
            }
        }
        else if (!question)
        {
            if (InputManager.IsActionPressed(GameAction.Up) && Log.Count > 1)
            {
                OpenBacklog();
            }
            else if (InputManager.IsActionPressed(GameAction.Confirm))
            {
                AdvanceLine();
            }
            else if (AutoAdvanceAfter is { } pause)
            {
                // A whole line goes on by itself once it has been read (plan 08 · P2); a question never does
                autoAdvanceWaited += dt;
                if (autoAdvanceWaited >= pause) AdvanceLine();
            }
        }
    }

    // ------------------------------------------------------------------ the backlog

    /// <summary>Opens the backlog on the newest lines (only over a line that is whole and plain: a question's Up is its cursor's).</summary>
    public void OpenBacklog()
    {
        if (!IsActive || !IsCurrentLineComplete || question || Log.Count == 0) return;
        BacklogOpen = true;
        BacklogTop = Math.Max(0, Log.Count - BacklogRows);
    }

    public void CloseBacklog() => BacklogOpen = false;

    /// <summary>Moves the backlog's window by rows, up to older lines (negative) or down to newer ones.</summary>
    public void ScrollBacklog(int rows) => BacklogTop = Math.Clamp(BacklogTop + rows, 0, Math.Max(0, Log.Count - BacklogRows));

    private void UpdateBacklog()
    {
        if (InputManager.IsActionPressed(GameAction.Up)) ScrollBacklog(-1);
        else if (InputManager.IsActionPressed(GameAction.Down)) ScrollBacklog(1);
        else if (InputManager.IsActionPressed(GameAction.Cancel) || InputManager.IsActionPressed(GameAction.Confirm)) CloseBacklog();
    }

    /// <summary>How long a whole line waits before it goes on by itself (the options' auto-advance, plan 08 · P2); null when it waits for a press.</summary>
    public float? AutoAdvanceAfter { get; set; }
    private float autoAdvanceWaited;

    /// <summary>
    /// Writes out as much of the line as the time allows, at the same pace whatever the frame rate. The line's codes act
    /// as the typing reaches them: a pause holds it, a speed changes its pace, a sound plays.
    /// </summary>
    public void Type(float dt)
    {
        if (IsCurrentLineComplete) return;
        var events = current.Events;
        int length = current.Plain.Length;
        float time = dt;
        while (time > 0f && charIndex < length)
        {
            // What lies at or before the place the typing has reached acts first
            while (nextEvent < events.Count && events[nextEvent].At <= charIndex)
            {
                var e = events[nextEvent++];
                if (e.Kind == TextEventKind.Pause) pauseLeft += e.Amount;
                else if (e.Kind == TextEventKind.Speed) speed = e.Amount;
                else if (e.Sound.Length > 0) AudioManager.PlaySound(e.Sound);
            }
            if (pauseLeft > 0f)
            {
                float held = Math.Min(time, pauseLeft);
                pauseLeft -= held;
                time -= held;
                continue;
            }
            float rate = CharactersPerSecond * speed;
            if (rate <= 0f) return;
            int stop = nextEvent < events.Count ? Math.Min(events[nextEvent].At, length) : length;
            float need = stop - charIndex - charProgress;
            float can = time * rate;
            if (can >= need)
            {
                charIndex = stop;
                charProgress = 0f;
                time -= need / rate;
            }
            else
            {
                charProgress += can;
                int whole = (int)charProgress;
                charProgress -= whole;
                charIndex = Math.Min(charIndex + whole, length);
                time = 0f;
            }
        }
    }

    /// <summary>How much of the line shows so far, and who is speaking.</summary>
    public string VisibleText => current.Plain[..charIndex];
    public string Speaker => currentSpeaker;

    /// <summary>Shows the whole line at once (the A button while it is being written); sounds written into it are not played.</summary>
    public void FinishLine()
    {
        charIndex = current.Plain.Length;
        nextEvent = current.Events.Count;
        pauseLeft = 0f;
    }

    /// <summary>Goes on to the next line, or ends the talk after the last (the A button once a line is complete).</summary>
    public void Advance()
    {
        if (IsActive && IsCurrentLineComplete && !question) AdvanceLine();
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsActive) return;
        UI.ModernUi.DrawDialogue(screenWidth, screenHeight, currentSpeaker, current.Plain[..charIndex], IsCurrentLineComplete && !question, current.Spans);
        if (BacklogOpen) UI.ModernUi.DrawBacklog(screenWidth, screenHeight, Log.Entries, BacklogTop, BacklogRows);
    }
}
