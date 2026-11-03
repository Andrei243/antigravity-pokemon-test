using System;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.UI;

/// <summary>What the camp's screen asks of the engine as it closes.</summary>
public enum CampRequest
{
    None,
    /// <summary>The player chose to feed the Pokémon under the cursor: the bag opens on the berries.</summary>
    Feed,
    Close
}

/// <summary>
/// The camp's screen (plan 10 · F6; style guide, "Menu screens (G10)", "The camp"): the team stands in two rows of
/// three (or one row of as many as there are) on a clearing, the cursor moves over them, Confirm pets the one under it
/// and the item button asks for a berry to feed it. Its logic takes no input (<see cref="MoveCursor"/>, <see cref="Pet"/>,
/// <see cref="AskToFeed"/>, <see cref="Feed"/>, <see cref="Cancel"/>), so tests drive it; <see cref="Update"/> reads the
/// keys into those, and <c>ModernUi.DrawCamp</c> draws it. The rules are <see cref="CampVisit"/>'s and <see cref="CampCare"/>'s.
/// </summary>
public sealed class CampScreen
{
    /// <summary>The team in two rows of this many.</summary>
    public const int Columns = 3;

    /// <summary>How long the hearts over a Pokémon just petted, or the berry over one just fed, last.</summary>
    public const float CheerTime = 1.4f;

    /// <summary>How long the screen takes to settle as it opens.</summary>
    public const float AppearTime = 0.35f;

    public bool IsActive { get; private set; }
    public Party? Party { get; private set; }
    public CampVisit Visit { get; private set; } = new();
    public int Cursor { get; private set; }

    /// <summary>How many of the team's conditions the visit cured as it began (none on a second visit with no battle between).</summary>
    public int CuredOnArrival { get; private set; }

    /// <summary>How long the screen has been open.</summary>
    public float Age { get; private set; }

    /// <summary>The line at the screen's foot: what the last thing done came to, or how to begin.</summary>
    public string Note { get; private set; } = "";

    private readonly float[] petAge = new float[Party.MaxSize];
    private readonly float[] fedAge = new float[Party.MaxSize];

    private CampRequest request;

    /// <summary>Opens a visit with the team: a fresh visit (each may be petted and fed again), and the cure if a battle came between.</summary>
    public void Open(Party party, CampCare care)
    {
        Party = party;
        Visit = new CampVisit();
        Cursor = 0;
        request = CampRequest.None;
        CuredOnArrival = care.Cure(party);
        Age = 0f;
        Array.Fill(petAge, -1f);
        Array.Fill(fedAge, -1f);
        Note = CuredOnArrival > 0 ? Text.Of("camp.note.cured") : Text.Of("camp.note.welcome");
        IsActive = true;
    }

    public int Count => Party?.Members.Count ?? 0;

    /// <summary>The row and column of a place: two rows of three for four or more, one row otherwise.</summary>
    public (int Row, int Column) PlaceOf(int index) => Count > Columns ? (index / Columns, index % Columns) : (0, index);

    /// <summary>Moves the cursor a column or a row, staying on the team.</summary>
    public void MoveCursor(int dx, int dy)
    {
        if (Count == 0) return;
        var (row, column) = PlaceOf(Cursor);
        int rows = Count > Columns ? 2 : 1, columns = Count > Columns ? Columns : Count;
        row = Math.Clamp(row + dy, 0, rows - 1);
        column = Math.Clamp(column + dx, 0, columns - 1);
        int index = Count > Columns ? row * Columns + column : column;
        // The second row may be shorter: stay within the team
        int next = Math.Min(index, Count - 1);
        if (next == Cursor) return;
        Cursor = next;
        AudioManager.PlaySound("cursor");
    }

    /// <summary>The Pokémon under the cursor.</summary>
    public Pokemon? Selected => Count == 0 ? null : Party!.Members[Cursor];

    /// <summary>Seconds since the Pokémon at a place was petted this visit, or -1.</summary>
    public float PettedFor(int index) => index >= 0 && index < petAge.Length ? petAge[index] : -1f;

    /// <summary>Seconds since the Pokémon at a place was fed this visit, or -1.</summary>
    public float FedFor(int index) => index >= 0 && index < fedAge.Length ? fedAge[index] : -1f;

    /// <summary>Confirm pets the one under the cursor. True if it was petted; the note says what came of it either way.</summary>
    public bool Pet()
    {
        if (Selected is not { } p) return false;
        var why = Visit.WhyNotPet(p);
        if (!Visit.Pet(p))
        {
            Note = Refusal(why, p);
            AudioManager.PlaySound("error");
            return false;
        }
        petAge[Cursor] = 0f;
        Note = Text.Of("camp.note.petted", ("name", p.DisplayName));
        AudioManager.PlaySound("select");
        AudioManager.PlayCry(p, CryMode.FieldEvent);
        return true;
    }

    /// <summary>The player chose to feed the one under the cursor: the engine opens the bag's berries and hands one to <see cref="Feed"/>.</summary>
    public void AskToFeed()
    {
        if (Selected is not { } p) return;
        // What can be said without the bag is said here: an Egg, or a meal had already
        var why = Visit.WhyNotFeed(p, null, new Inventory());
        if (why is CampRefusal.Egg or CampRefusal.AlreadyThisVisit)
        {
            Note = Refusal(why, p);
            AudioManager.PlaySound("error");
            return;
        }
        request = CampRequest.Feed;
    }

    /// <summary>Feeds the one under the cursor the berry picked. True if it ate; the note says what came of it either way.</summary>
    public bool Feed(ItemData berry, Inventory bag)
    {
        if (Selected is not { } p) return false;
        var why = Visit.WhyNotFeed(p, berry, bag);
        if (!Visit.Feed(p, berry, bag))
        {
            Note = Refusal(why, p);
            AudioManager.PlaySound("error");
            return false;
        }
        fedAge[Cursor] = 0f;
        Note = Text.Of("camp.note.fed", ("name", p.DisplayName), ("berry", berry.DisplayName));
        AudioManager.PlayCry(p, CryMode.FieldEvent);
        return true;
    }

    /// <summary>The bag was put away with no berry chosen.</summary>
    public void NoBerryChosen() => Note = Text.Of("camp.note.welcome");

    private static string Refusal(CampRefusal why, Pokemon p) => why switch
    {
        CampRefusal.Egg => Text.Of("camp.note.egg"),
        CampRefusal.AlreadyThisVisit => Text.Of("camp.note.already", ("name", p.DisplayName)),
        CampRefusal.NoBerry => Text.Of("camp.note.noBerry"),
        _ => ""
    };

    /// <summary>Leaves the camp.</summary>
    public void Cancel()
    {
        request = CampRequest.Close;
        IsActive = false;
        AudioManager.PlaySound("cancel");
    }

    /// <summary>What the screen asks of the engine, once.</summary>
    public CampRequest TakeRequest()
    {
        var r = request;
        request = CampRequest.None;
        return r;
    }

    /// <summary>The time on: the screen's own clock and the cheers'.</summary>
    public void Advance(float dt)
    {
        Age += dt;
        for (int i = 0; i < petAge.Length; i++)
        {
            if (petAge[i] >= 0f) petAge[i] += dt;
            if (fedAge[i] >= 0f) fedAge[i] += dt;
        }
    }

    /// <summary>A frame of the keys: the arrows move the cursor, Confirm pets, the item button feeds, Cancel leaves.</summary>
    public void Update(float dt)
    {
        if (!IsActive) return;
        Advance(dt);
        int dx = InputManager.Axis(GameAction.Left, GameAction.Right), dy = InputManager.Axis(GameAction.Up, GameAction.Down);
        if (dx != 0 || dy != 0) MoveCursor(dx, dy);
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Pet();
        else if (InputManager.IsActionPressed(GameAction.Item)) AskToFeed();
        else if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsActive) return;
        ModernUi.DrawCamp(screenWidth, screenHeight, this, Math.Clamp(Age / AppearTime, 0f, 1f));
    }
}
