using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>What the Stroll Counter's screen shows.</summary>
public enum StrollPage { Home, Seek, Meet, Courses }

/// <summary>A row of the counter's own menu.</summary>
public enum StrollRow { Seek, Meet, Course, Close }

/// <summary>
/// The Stroll Counter's screen (plan 35 · BN7; style guide, "The Stroll Counter"): the device in two tones, its Pokémon,
/// strides and sparks, the course and what it holds; a seek (three bushes, two tries) and a meet (three rounds of STRIKE,
/// DODGE and CATCH). The rules are <see cref="StrollCounter"/>'s; the steps take no input of their own, so tests and the
/// harness drive it. Every draw is on a seek's or a meet's own generator, never the dice.
/// </summary>
public sealed class StrollCounterScreen
{
    private const float AppearTime = 0.35f;

    public bool IsActive { get; private set; }
    public float Age { get; private set; }
    public StrollPage Page { get; private set; }
    public StrollSave Save { get; private set; } = new();

    /// <summary>The Pokémon riding in it as the screen opened, made once to be drawn (the save holds it whole).</summary>
    public Pokemon? Rider { get; private set; }

    private RunRules? run;
    private uint trainer;
    private Func<string, bool> entered = _ => false;
    private Func<StrollMeet, Pokemon?> keep = _ => null;

    /// <summary>The row of the menu, the bush or the meet's choice, and the course under the cursor.</summary>
    public int Cursor { get; private set; }

    public StrollSeek? Seek { get; private set; }
    public StrollMeet? Meet { get; private set; }

    /// <summary>The Pokémon a meet caught, while its line is up.</summary>
    public Pokemon? Caught { get; private set; }

    /// <summary>The line under the device: what the last press did, or why it did nothing; null for none.</summary>
    public string? Message { get; private set; }

    /// <summary>Whether the last line is a refusal (drawn muted).</summary>
    public bool Refused { get; private set; }

    /// <summary>The menu's rows: with nothing in it only CLOSE.</summary>
    public IReadOnlyList<StrollRow> Rows => Save.Pokemon == null
        ? new[] { StrollRow.Close }
        : new[] { StrollRow.Seek, StrollRow.Meet, StrollRow.Course, StrollRow.Close };

    /// <summary>The courses it may walk now.</summary>
    public IReadOnlyList<StrollCourse> Courses => StrollCounter.Open(Save.StridesEver, entered);

    /// <summary>The three choices of a meet, in order.</summary>
    public static readonly IReadOnlyList<StrollMove> Moves = new[] { StrollMove.Strike, StrollMove.Dodge, StrollMove.Catch };

    /// <summary>
    /// Opens the counter: <paramref name="keep"/> is the game's hand on a meet won (the Pokémon made and held, at the
    /// course's levels as the region's map them), <paramref name="entered"/> says which regions' courses are open.
    /// </summary>
    public void Open(StrollSave save, RunRules? run, uint trainer, Func<string, bool> entered, Func<StrollMeet, Pokemon?> keep)
    {
        Save = save;
        Rider = save.Pokemon?.ToPokemon();
        this.run = run;
        this.trainer = trainer;
        this.entered = entered;
        this.keep = keep;
        // A course of a region not entered, or none chosen, is the first open one's
        if (StrollCounter.Course(save.Course) is not { } course || !Courses.Contains(course))
            save.Course = Courses.FirstOrDefault()?.Id ?? "";
        Page = StrollPage.Home;
        Cursor = 0;
        Seek = null;
        Meet = null;
        Caught = null;
        Message = save.Pokemon == null ? Text.Of("stroll.empty") : null;
        Refused = save.Pokemon == null;
        Age = 0f;
        IsActive = true;
        AudioManager.PlaySound("menu_open");
    }

    public void Close()
    {
        IsActive = false;
        AudioManager.PlaySound("menu_close");
    }

    private void Say(string line, bool refused)
    {
        Message = line;
        Refused = refused;
    }

    private int CountOn(StrollPage page) => page switch
    {
        StrollPage.Home => Rows.Count,
        StrollPage.Seek => StrollCounter.Bushes,
        StrollPage.Meet => Moves.Count,
        _ => Math.Max(1, Courses.Count)
    };

    /// <summary>The arrows: up and down the menu and the courses, left and right over the bushes and the meet's choices.</summary>
    public void Move(int dx, int dy)
    {
        int step = Page is StrollPage.Seek or StrollPage.Meet ? dx : dy;
        if (step == 0) return;
        Cursor = UiNav.Wrap(Math.Min(Cursor, CountOn(Page) - 1), Math.Sign(step), CountOn(Page));
        AudioManager.PlaySound("cursor");
    }

    /// <summary>Confirm: the row chosen, a bush looked in, a round played, a course taken; and on from a seek or a meet over.</summary>
    public void Confirm()
    {
        switch (Page)
        {
            case StrollPage.Home:
                ChooseRow(Rows[Math.Min(Cursor, Rows.Count - 1)]);
                break;
            case StrollPage.Seek when Seek is { } seek:
                if (seek.Over) { Home(); break; }
                if (!seek.Look(Cursor)) { AudioManager.PlaySound("cancel"); break; }
                if (seek.Found)
                {
                    StrollCounter.EndSeek(Save, seek);
                    Say(Text.Of("stroll.seek.found", ("item", Data.LocalNames.Item(seek.Find))), refused: false);
                    AudioManager.PlaySound("select");
                }
                else if (seek.Over) Say(Text.Of("stroll.seek.nothing"), refused: true);
                else Say(Text.Of("stroll.seek.tryAgain"), refused: true);
                break;
            case StrollPage.Meet when Meet is { } meet:
                if (meet.End != StrollMeetEnd.Going) { Home(); break; }
                meet.Play(Moves[Cursor]);
                AudioManager.PlaySound(meet.LastLanded ? "select" : "cursor");
                Say(RoundLine(meet), refused: meet.End is StrollMeetEnd.Fled or StrollMeetEnd.Lost);
                if (meet.End == StrollMeetEnd.Caught)
                {
                    Caught = keep(meet);
                    Say(Text.Of("stroll.meet.caught", ("name", Data.LocalNames.Species(meet.Species))), refused: false);
                }
                break;
            case StrollPage.Courses:
                if (Courses.Count > 0) Save.Course = Courses[Math.Min(Cursor, Courses.Count - 1)].Id;
                AudioManager.PlaySound("select");
                Home();
                break;
        }
    }

    private void ChooseRow(StrollRow row)
    {
        switch (row)
        {
            case StrollRow.Seek:
            {
                var why = StrollCounter.WhyNotSeek(Save);
                if (why != StrollRefusal.None) { Refuse(why); return; }
                Seek = StrollCounter.BeginSeek(Save, trainer);
                Page = StrollPage.Seek;
                Cursor = 1;
                Say(Text.Of("stroll.seek.begin"), refused: false);
                AudioManager.PlaySound("select");
                return;
            }
            case StrollRow.Meet:
            {
                var why = StrollCounter.WhyNotMeet(Save, run);
                if (why != StrollRefusal.None) { Refuse(why); return; }
                Meet = StrollCounter.BeginMeet(Save, run, trainer);
                Caught = null;
                Page = StrollPage.Meet;
                Cursor = 0;
                Say(Text.Of("stroll.meet.begin", ("name", Data.LocalNames.Species(Meet!.Species)), ("level", Meet.Level)), refused: false);
                AudioManager.PlaySound("select");
                return;
            }
            case StrollRow.Course:
                Page = StrollPage.Courses;
                Cursor = Math.Max(0, Courses.ToList().FindIndex(c => c.Id == Save.Course));
                Message = null;
                AudioManager.PlaySound("select");
                return;
            default:
                Close();
                return;
        }
    }

    private void Refuse(StrollRefusal why)
    {
        string word = Word(why);
        Say(Text.Of($"stroll.why.{word}"), refused: true);
        AudioManager.PlaySound("cancel");
    }

    /// <summary>A refusal's key word in <c>stroll.why.*</c>.</summary>
    internal static string Word(StrollRefusal why) => why switch
    {
        StrollRefusal.Empty => "empty",
        StrollRefusal.NoSparks => "sparks",
        StrollRefusal.HandsFull => "full",
        StrollRefusal.NoCatching => "run",
        _ => "course"
    };

    /// <summary>What a round of a meet came to, in a line.</summary>
    internal static string RoundLine(StrollMeet meet)
    {
        string name = Data.LocalNames.Species(meet.Species);
        string first = meet.LastMove switch
        {
            StrollMove.Strike => Text.Of(meet.LastLanded ? "stroll.meet.struck" : "stroll.meet.missed", ("name", name)),
            StrollMove.Dodge => Text.Of("stroll.meet.dodged", ("name", name)),
            _ => Text.Of("stroll.meet.slipped", ("name", name))
        };
        if (meet.LastHit) first += " " + Text.Of("stroll.meet.hit", ("name", name));
        return meet.End switch
        {
            StrollMeetEnd.Fled => first + " " + Text.Of("stroll.meet.fled", ("name", name)),
            StrollMeetEnd.Lost => first + " " + Text.Of("stroll.meet.lost"),
            _ => first
        };
    }

    private void Home()
    {
        Page = StrollPage.Home;
        Cursor = 0;
        Seek = null;
        Meet = null;
    }

    /// <summary>Back: out of the courses, a seek or a meet over (a meet under way is run from), or the counter closed.</summary>
    public void Cancel()
    {
        if (Page == StrollPage.Home) { Close(); return; }
        if (Page == StrollPage.Seek && Seek is { Over: false }) { AudioManager.PlaySound("cancel"); return; }
        AudioManager.PlaySound("cancel");
        Home();
    }

    public void Update(float dt)
    {
        if (!IsActive) return;
        Age += dt;
        int dy = InputManager.Axis(GameAction.Up, GameAction.Down);
        int dx = InputManager.Axis(GameAction.Left, GameAction.Right);
        if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
        else if (dx != 0 || dy != 0) Move(dx, dy);
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsActive) return;
        ModernUi.DrawStroll(screenWidth, screenHeight, this, Math.Clamp(Age / AppearTime, 0f, 1f));
    }
}
