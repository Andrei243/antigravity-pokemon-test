using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>What the camp's screen asks of the engine as it closes.</summary>
public enum CampRequest
{
    None,
    /// <summary>The player chose to feed the Pokémon under the cursor: the bag opens on the berries.</summary>
    Feed,
    Close
}

/// <summary>What the camp's screen shows: the team, the pot's pantry (choosing what goes in), the pot on the fire, the dish served.</summary>
public enum CampPhase { Team, Pantry, Stirring, Served }

/// <summary>
/// What the camp cooks with (plan 39 · CR6): the kit's pouch, the bag, the powers a dish starts, the timing assist and the
/// seed the pots of this visit are worked out from. The camp has none without the crafting kit.
/// </summary>
public sealed record CampKitchen(MaterialPouch Pouch, Inventory Bag, PowerState Powers, TimingAssist Assist, uint Seed);

/// <summary>
/// The camp's screen (plan 10 · F6; style guide, "Menu screens (G10)", "The camp"): the team stands in two rows of
/// three (or one row of as many as there are) on a clearing, the cursor moves over them, Confirm pets the one under it
/// and the item button asks for a berry to feed it. Its logic takes no input (<see cref="MoveCursor"/>, <see cref="Pet"/>,
/// <see cref="AskToFeed"/>, <see cref="Feed"/>, <see cref="Cancel"/>), so tests drive it; <see cref="Update"/> reads the
/// keys into those, and <c>ModernUi.DrawCamp</c> draws it. The rules are <see cref="CampVisit"/>'s and <see cref="CampCare"/>'s.
/// With the crafting kit the start button opens the pot (plan 39 · CR6; <see cref="AskToCook"/>): the pantry lists what can go
/// in (<see cref="PantryMove"/>, <see cref="PantryConfirm"/>, <see cref="PantryCancel"/>), the pot is stirred four rounds
/// (<see cref="Stir"/>, <see cref="StepPot"/>, <see cref="TakeOff"/>; the rules are <see cref="Kitchen"/>'s), and the dish is
/// served to the team (<see cref="Meals.Serve"/>) and looked at until Confirm (<see cref="ServedConfirm"/>).
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

    /// <summary>
    /// Opens a visit with the team: a fresh visit (each may be petted and fed again), and the cure if a battle came between;
    /// with the crafting kit's <paramref name="kitchen"/> the pot may be put on (plan 39 · CR6).
    /// </summary>
    public void Open(Party party, CampCare care, CampKitchen? kitchen = null)
    {
        Party = party;
        Visit = care.NewVisit();
        Cursor = 0;
        request = CampRequest.None;
        Supplies = kitchen;
        Phase = CampPhase.Team;
        Pot.Clear();
        OnHand = new();
        Stirring = null;
        Served = null;
        pressPending = false;
        cooked = 0;
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
        if (why is CampRefusal.Egg or CampRefusal.AlreadyThisVisit or CampRefusal.Full)
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
        CampRefusal.Full => Text.Of("camp.note.full", ("name", p.DisplayName)),
        _ => ""
    };

    // ------------------------------------------------------------------ the pot (plan 39 · CR6)

    /// <summary>What the camp cooks with; null without the crafting kit, and the pot isn't offered.</summary>
    public CampKitchen? Supplies { get; private set; }

    public bool CanCook => Supplies != null;

    public CampPhase Phase { get; private set; }

    /// <summary>What may go in the pot, from the pouch's pantry and the bag; the pantry's list has a last row, COOK.</summary>
    public List<KitchenIngredient> OnHand { get; private set; } = new();

    /// <summary>The pantry's cursor (the row past the last ingredient is COOK) and its first row showing.</summary>
    public int PantryCursor { get; private set; }
    public int PantryFirst { get; private set; }

    /// <summary>The rows the pantry's list shows.</summary>
    public const int PantryRows = 5;

    /// <summary>Whether the pantry's cursor is on COOK.</summary>
    public bool OnCook => PantryCursor == OnHand.Count;

    /// <summary>What is in the pot, in the order put in.</summary>
    public List<KitchenIngredient> Pot { get; } = new();

    /// <summary>The dish the pot would cook now.</summary>
    public KitchenDish Expected => Meals.DishFor(Pot);

    /// <summary>The pot on the fire while it is stirred; null otherwise.</summary>
    public Kitchen? Stirring { get; private set; }

    /// <summary>The dish just served and what it came to; null until one is.</summary>
    public MealServed? Served { get; private set; }

    /// <summary>Seconds since the dish was served, for its look.</summary>
    public float ServedFor { get; private set; }

    private bool pressPending;
    private float stirClock;
    private int cooked;

    /// <summary>How many of an ingredient are in the pot.</summary>
    public int InPot(KitchenIngredient ingredient) => Pot.Count(i => i == ingredient);

    /// <summary>The start button: the pot's pantry opens with what can go in, or the note says there is nothing to cook with.</summary>
    public void AskToCook()
    {
        if (Supplies is not { } s || Phase != CampPhase.Team) return;
        OnHand = Meals.OnHand(s.Pouch, s.Bag);
        if (OnHand.Count == 0)
        {
            Note = Text.Of("campPot.note.nothing");
            AudioManager.PlaySound("error");
            return;
        }
        Pot.Clear();
        PantryCursor = 0;
        PantryFirst = 0;
        Phase = CampPhase.Pantry;
        Note = Text.Of("campPot.note.choose", ("n", Meals.Table.Most));
        AudioManager.PlaySound("select");
    }

    /// <summary>The pantry's cursor up (−1) or down (+1), over the ingredients and COOK, wrapping round.</summary>
    public void PantryMove(int delta)
    {
        if (Phase != CampPhase.Pantry || delta == 0) return;
        int rows = OnHand.Count + 1;
        int next = UiNav.Wrap(PantryCursor, Math.Sign(delta), rows);
        if (next == PantryCursor) return;
        PantryCursor = next;
        PantryFirst = UiNav.Window(PantryFirst, PantryCursor, rows, PantryRows);
        AudioManager.PlaySound("cursor");
    }

    /// <summary>A in the pantry: one of the ingredient under the cursor into the pot, or, on COOK, the pot on the fire.</summary>
    public void PantryConfirm()
    {
        if (Phase != CampPhase.Pantry || Supplies is not { } s) return;
        if (OnCook)
        {
            if (Pot.Count == 0) { Refuse(Text.Of("campPot.note.empty")); return; }
            if (!Meals.AllHeld(Pot, s.Pouch, s.Bag)) { Refuse(Text.Of("campPot.note.short")); return; }
            // Each pot of the visit is worked out from the visit's seed, what is in it and how many came before
            uint seed = SplitMix.Mix(s.Seed, (uint)cooked, SplitMix.Fnv(string.Join("|", Pot.Select(i => i.Id))));
            Stirring = new Kitchen(seed, s.Assist);
            stirClock = 0f;
            pressPending = false;
            Phase = CampPhase.Stirring;
            Note = Text.Of("campPot.note.stir");
            AudioManager.PlaySound("select");
            return;
        }
        var ingredient = OnHand[PantryCursor];
        if (Pot.Count >= Meals.Table.Most) { Refuse(Text.Of("campPot.note.full", ("n", Meals.Table.Most))); return; }
        if (InPot(ingredient) >= Meals.Held(ingredient, s.Pouch, s.Bag)) { Refuse(Text.Of("campPot.note.noMore", ("name", ingredient.Name))); return; }
        Pot.Add(ingredient);
        Note = Text.Of("campPot.note.added", ("name", ingredient.Name), ("dish", Meals.NameOf(Expected)));
        AudioManager.PlaySound("select");
    }

    /// <summary>B in the pantry: the last thing put in comes out again, or with the pot empty the team is back.</summary>
    public void PantryCancel()
    {
        if (Phase != CampPhase.Pantry) return;
        if (Pot.Count > 0)
        {
            Pot.RemoveAt(Pot.Count - 1);
            Note = Pot.Count > 0 ? Text.Of("campPot.note.added", ("name", Pot[^1].Name), ("dish", Meals.NameOf(Expected))) : Text.Of("campPot.note.choose", ("n", Meals.Table.Most));
            AudioManager.PlaySound("cancel");
            return;
        }
        Phase = CampPhase.Team;
        Note = Text.Of("camp.note.welcome");
        AudioManager.PlaySound("cancel");
    }

    /// <summary>A on the fire: the stir counts on the pot's next frame.</summary>
    public void Stir()
    {
        if (Phase == CampPhase.Stirring) pressPending = true;
    }

    /// <summary>One frame of the pot, a press or not (what <see cref="Advance"/> runs at thirty a second); the dish is served when the fourth round is scored.</summary>
    public void StepPot(bool press)
    {
        if (Phase != CampPhase.Stirring || Stirring is not { } pot) return;
        int before = pot.Scores.Count;
        pot.Step(press);
        if (pot.Scores.Count > before) AudioManager.PlaySound(pot.Scores[^1] == StirScore.Miss ? "error" : "select");
        if (pot.Done) Serve();
    }

    /// <summary>B on the fire: the pot comes off with nothing cooked and nothing used, and the pantry is back.</summary>
    public void TakeOff()
    {
        if (Phase != CampPhase.Stirring) return;
        Stirring = null;
        pressPending = false;
        Phase = CampPhase.Pantry;
        Note = Text.Of("campPot.note.takenOff");
        AudioManager.PlaySound("cancel");
    }

    // The dish is done: what was in the pot is used, the team eats, and the dish is shown
    private void Serve()
    {
        if (Supplies is not { } s || Stirring is not { } pot || Party == null) return;
        var dish = Expected;
        var type = Meals.TypeFor(Pot);
        Meals.Use(Pot, s.Pouch, s.Bag);
        Served = Meals.Serve(Party, dish, pot.Grade, s.Powers, Visit, type);
        cooked++;
        Stirring = null;
        ServedFor = 0f;
        Phase = CampPhase.Served;
        for (int i = 0; i < Count; i++)
            if (!Party.Members[i].IsEgg) fedAge[i] = 0f;
        Note = Text.Of("campPot.note.served", ("dish", Meals.NameOf(dish)), ("grade", Meals.NameOf(pot.Grade)));
        Pot.Clear();
        AudioManager.PlaySound("heal");
    }

    /// <summary>A on the dish served: the team is back, and the pot may go on again.</summary>
    public void ServedConfirm()
    {
        if (Phase != CampPhase.Served) return;
        Phase = CampPhase.Team;
        Note = Served?.Power is { } power
            ? Text.Of("campPot.note.powerOn", ("power", PowerTable.NameOf(power.Kind)), ("level", power.Level))
            : Text.Of("camp.note.welcome");
        AudioManager.PlaySound("select");
    }

    private void Refuse(string why)
    {
        Note = why;
        AudioManager.PlaySound("error");
    }

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
        if (Phase == CampPhase.Served) ServedFor += dt;
        // The pot runs at thirty frames a second, a stir counting on the frame after it was pressed
        if (Phase == CampPhase.Stirring && Stirring is { } pot)
        {
            stirClock += dt;
            while (Phase == CampPhase.Stirring && pot.Frame < (int)(stirClock * Kitchen.FramesPerSecond))
            {
                bool press = pressPending;
                pressPending = false;
                StepPot(press);
            }
        }
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
        switch (Phase)
        {
            case CampPhase.Pantry:
                if (InputManager.IsActionPressed(GameAction.Up)) PantryMove(-1);
                else if (InputManager.IsActionPressed(GameAction.Down)) PantryMove(1);
                else if (InputManager.IsActionPressed(GameAction.Confirm)) PantryConfirm();
                else if (InputManager.IsActionPressed(GameAction.Cancel)) PantryCancel();
                return;
            case CampPhase.Stirring:
                if (InputManager.IsActionPressed(GameAction.Confirm)) Stir();
                else if (InputManager.IsActionPressed(GameAction.Cancel)) TakeOff();
                return;
            case CampPhase.Served:
                if (InputManager.IsActionPressed(GameAction.Confirm) || InputManager.IsActionPressed(GameAction.Cancel)) ServedConfirm();
                return;
        }
        if (CanCook && InputManager.IsActionPressed(GameAction.Menu))
        {
            AskToCook();
            return;
        }
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
