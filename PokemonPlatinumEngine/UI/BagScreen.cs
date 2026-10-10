using System;
using System.Collections.Generic;
using System.Linq;
using Raylib_cs;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>What can be done with the item under the cursor.</summary>
public enum BagAction { Use, Give, Toss, Register, Deselect, Cancel }

/// <summary>
/// The bag: Platinum's eight pockets as tabs, the pocket's items as a list and the chosen item beside it. The A
/// button opens what can be done with an item (USE, GIVE, TOSS, CANCEL); using or giving goes on to the party to
/// pick a Pokémon, and an item used on one move (an Ether, a PP Up) to that Pokémon's moves. A TM or an HM is
/// taught to the Pokémon picked, and a Pokémon that knows four moves already is asked which to forget, as it is
/// for a move a Rare Candy's level brings (plan 06 · R11). Its logic takes no input (<see cref="MovePocket"/>,
/// <see cref="MoveCursor"/>, <see cref="Confirm"/>, <see cref="Cancel"/>), so tests and the harness drive it. Up or
/// down kept down runs on through a pocket's list (<see cref="HeldKey"/>).
/// </summary>
public class BagScreen
{
    private const float AppearTime = 0.3f, ChoiceAppearTime = 0.35f;

    /// <summary>How many rows of a pocket show at once.</summary>
    public const int VisibleRows = 8;

    /// <summary>Platinum's pockets, in its order.</summary>
    public static readonly ItemPocket[] Pockets =
    {
        ItemPocket.Items, ItemPocket.Medicine, ItemPocket.PokeBalls, ItemPocket.TMsAndHMs,
        ItemPocket.Berries, ItemPocket.Mail, ItemPocket.BattleItems, ItemPocket.KeyItems
    };

    // Each pocket remembers where its cursor was, as in the games
    private readonly int[] cursors = new int[Pockets.Length], firsts = new int[Pockets.Length];
    private readonly HeldKey upDown = new();
    private readonly CursorTick tick = new();
    private float openAge;

    public ItemPocket CurrentPocket { get; set; } = ItemPocket.Items;
    public bool IsActive { get; set; } = false;

    private int PocketIndex => Math.Max(0, Array.IndexOf(Pockets, CurrentPocket));

    public int SelectedIndex
    {
        get => cursors[PocketIndex];
        set => cursors[PocketIndex] = value;
    }

    /// <summary>The first row of the pocket's list that shows: it follows the cursor (<see cref="UiNav.Window"/>).</summary>
    public int FirstRow => firsts[PocketIndex];

    /// <summary>What can be done with the chosen item, while its menu is up (null otherwise), and the cursor in it.</summary>
    public IReadOnlyList<BagAction>? Actions { get; private set; }
    public int ActionIndex { get; private set; }

    // An item that goes to one Pokémon of the player's choosing waits here while they pick
    private ItemData? choosingFor;
    private bool giving;
    private float choiceAge;
    private EvolutionRequest? request;

    /// <summary>The Pokémon the cursor is on while an item waits for its target.</summary>
    public int TargetIndex { get; private set; }

    /// <summary>The item waiting for the player to pick a Pokémon (null: the bag itself is showing).</summary>
    public ItemData? ChoosingFor => choosingFor;

    /// <summary>Whether the waiting item is to be given to hold rather than used.</summary>
    public bool Giving => giving;

    /// <summary>
    /// The key item kept ready on the item button (the original's registered item, its Y button), by name; the game
    /// sets it as the bag opens and saves it.
    /// </summary>
    public string? Registered { get; set; }

    /// <summary>The Repel's steps and the flute (plan 06 · R11): the game hands its own over as the bag opens.</summary>
    public EncounterAids Aids { get; set; } = new();

    /// <summary>The player's name, for what the bag says when a Repel or a flute is used.</summary>
    public string PlayerName { get; set; } = PlayerIdentity.Name;

    // ---- throwing items away

    /// <summary>True while the bag asks how many of the chosen item to throw away, and the number chosen so far.</summary>
    public bool Tossing { get; private set; }
    public int TossCount { get; private set; } = 1;

    // ---- an item used on one move

    /// <summary>True while the item waits for one of the chosen Pokémon's moves (an Ether, a PP Up), and the cursor on them.</summary>
    public bool ChoosingMove { get; private set; }
    public int MoveIndex { get; private set; }

    // ---- teaching a move to a Pokémon that knows four

    private readonly Queue<(Pokemon Pokemon, string Move, ItemData? Machine)> toTeach = new();
    private (Pokemon Pokemon, string Move, ItemData? Machine)? teaching;
    private Pokemon? grownByCandy;
    private EvolutionContext? candyContext;

    /// <summary>The Pokémon being taught and the move it wants, while the bag asks which move to forget (null otherwise).</summary>
    public (Pokemon Pokemon, string Move, ItemData? Machine)? Teaching => teaching;

    /// <summary>The cursor of the "forget which move?" panel: 0 to 3 a move, 4 "don't learn it".</summary>
    public int ForgetIndex { get; private set; }

    // An item used in the field itself (the Bicycle, a rod, an Escape Rope), for the game to carry out once the bag closes
    private ItemData? usedInField;

    // ---- the soil ahead, and an item chosen for a script (plan 06 · R14a)

    /// <summary>
    /// Whether the player faces soft soil with nothing growing in it (the original's <c>BERRY_PATCH_FLAG_EMPTY</c>):
    /// then USE plants any berry (<c>UseBerryFromMenu</c>). The game sets it as the bag opens.
    /// </summary>
    public bool SoilAhead { get; set; }

    /// <summary>
    /// True while the bag is open for a script to have an item chosen (<see cref="OpenToChoose"/>; the original's
    /// <c>OpenBerriesBag</c> and <c>OpenItemsBag</c>): one pocket, what the game put in it, A picks and B backs out.
    /// It stays true once the bag has closed, until it is opened again, so the game can read <see cref="Chosen"/>.
    /// </summary>
    public bool Choosing { get; private set; }

    /// <summary>The item picked while <see cref="Choosing"/>; null when the player backed out.</summary>
    public ItemData? Chosen { get; private set; }

    /// <summary>What the screen asks while <see cref="Choosing"/> ("PLANT WHICH BERRY?"); null for the bag's own title.</summary>
    public string? Prompt { get; private set; }

    /// <summary>Opens the bag on one pocket for an item to be chosen and nothing else done with it.</summary>
    public void OpenToChoose(ItemPocket pocket, string prompt)
    {
        Open();
        Choosing = true;
        Chosen = null;
        Prompt = prompt;
        CurrentPocket = pocket;
    }

    /// <summary>Hands over the item chosen to be used in the field, once: the bag has closed for it.</summary>
    public ItemData? TakeFieldUse()
    {
        var item = usedInField;
        usedInField = null;
        return item;
    }

    /// <summary>Hands over the evolution an item has just set off, once.</summary>
    public EvolutionRequest? TakeEvolution()
    {
        var taken = request;
        request = null;
        return taken;
    }

    public void Open()
    {
        IsActive = true;
        CurrentPocket = ItemPocket.Items;
        Array.Clear(cursors);
        Array.Clear(firsts);
        Actions = null;
        choosingFor = null;
        Tossing = false;
        ChoosingMove = false;
        teaching = null;
        toTeach.Clear();
        openAge = 0f;
        upDown.Release();
        Choosing = false;
        Chosen = null;
        Prompt = null;
    }

    public void Close()
    {
        IsActive = false;
        Actions = null;
        choosingFor = null;
        Tossing = false;
        ChoosingMove = false;
    }

    // ---------------------------------------------------------------- moving about

    /// <summary>To the next or the previous pocket, wrapping round; each keeps its own cursor.</summary>
    public void MovePocket(int step)
    {
        if (step == 0 || Actions != null || Tossing || Choosing) return;
        CurrentPocket = Pockets[UiNav.Wrap(PocketIndex, step, Pockets.Length)];
        AudioManager.PlaySound("page");
    }

    /// <summary>
    /// Up or down the pocket's list (or the item's actions while they are up), wrapping round. In the list a step
    /// that comes from a key kept down (<paramref name="held"/>) stops at either end instead.
    /// </summary>
    public void MoveCursor(int step, int count, bool held = false)
    {
        if (step == 0 || Tossing) return;
        if (Actions != null)
        {
            ActionIndex = UiNav.Wrap(ActionIndex, step, Actions.Count);
            AudioManager.PlaySound("cursor");
            return;
        }
        if (count <= 0) return;
        int from = Math.Min(SelectedIndex, count - 1);
        int next = held ? Math.Clamp(from + step, 0, count - 1) : UiNav.Wrap(from, step, count);
        if (held && next == from) return;
        SelectedIndex = next;
        Follow(count);
        if (tick.Sounds(held)) AudioManager.PlaySound("cursor");
    }

    /// <summary>Keeps the cursor inside the pocket and the list's window on the cursor.</summary>
    internal void Follow(int count)
    {
        SelectedIndex = Math.Clamp(SelectedIndex, 0, Math.Max(0, count - 1));
        firsts[PocketIndex] = UiNav.Window(firsts[PocketIndex], SelectedIndex, count, VisibleRows);
    }

    // ---------------------------------------------------------------- what an item can do

    /// <summary>
    /// Items used on one Pokémon: medicine and the berries that heal (by their use parameters, <see cref="ItemUse"/>),
    /// Rare Candies, vitamins and EV berries, what evolves a Pokémon, the Gracidea, and TMs and HMs.
    /// </summary>
    public static bool CanUse(ItemData item) =>
        ItemUse.IsUsedOnPokemon(item) || Evolution.IsUsedToEvolve(item) || EffortRules.IsEffortItem(item)
        || item.FieldUse == "Gracidea" || MoveTeaching.IsMachine(item);

    /// <summary>
    /// Items used in the field itself rather than on a Pokémon (plan 02 · S2), by the original's use of each: the
    /// Bicycle, the three rods, the Escape Rope, the Journal and the Vs. Seeker (plan 06 · R12), the Poké Radar and
    /// Honey (plan 06 · R13), the Sprayduck and the mulches on soft soil (plan 06 · R14a). The bag closes and the game
    /// carries them out.
    /// </summary>
    public static bool UsedInField(ItemData item) => item.FieldUse is "Bicycle" or "OldRod" or "GoodRod" or "SuperRod" or "EscapeRope" or "Journal" or "VsSeeker" or "PokeRadar" or "Honey"
        or "Sprayduck" or "Mulch";

    /// <summary>
    /// Whether USE plants the item in the soil the player faces (<c>UseBerryFromMenu</c>, plan 06 · R14a): any of
    /// Platinum's berries, with empty soft soil ahead. Elsewhere a berry is used as it always was, on a Pokémon.
    /// </summary>
    public static bool PlantsHere(ItemData item, bool soilAhead) => soilAhead && BerryPatches.CanPlant(item);

    /// <summary>Items that do their work from the bag itself, on nobody: the Repels and the flutes, and Sacred Ash on the whole team.</summary>
    public static bool UsedInBag(ItemData item) => EncounterAids.IsAid(item) || ItemUse.RevivesAll(item);

    /// <summary>As in Platinum, a Pokémon can hold anything but a Key Item or a TM.</summary>
    public static bool CanGive(ItemData item) => item.Pocket is not (ItemPocket.KeyItems or ItemPocket.TMsAndHMs);

    /// <summary>Whether it can be thrown away: anything but a key item, or an item the table says can't be (an HM).</summary>
    public static bool CanToss(ItemData item) => item.Pocket != ItemPocket.KeyItems && !item.CantBeTossed;

    /// <summary>Items the player aims at one Pokémon, to use or to give.</summary>
    public static bool NeedsTarget(ItemData item) => CanUse(item) || CanGive(item);

    /// <summary>
    /// What the A button offers for an item, CANCEL last: a key item that can be kept on the item button offers that
    /// too, and a berry offers USE wherever it can be planted (<paramref name="soilAhead"/>).
    /// </summary>
    public static List<BagAction> ActionsFor(ItemData item, string? registered = null, bool soilAhead = false)
    {
        var actions = new List<BagAction>();
        if (CanUse(item) || UsedInField(item) || UsedInBag(item) || PlantsHere(item, soilAhead)) actions.Add(BagAction.Use);
        if (CanGive(item)) actions.Add(BagAction.Give);
        if (CanToss(item)) actions.Add(BagAction.Toss);
        if (item.CanBeRegistered && UsedInField(item)) actions.Add(registered == item.Name ? BagAction.Deselect : BagAction.Register);
        actions.Add(BagAction.Cancel);
        return actions;
    }

    /// <summary>
    /// The A button: on an item, opens what can be done with it; on one of those, does it. Using and giving
    /// both go on to the party to pick a Pokémon; throwing away asks how many.
    /// </summary>
    public void Confirm(Inventory inventory, Party party, Action<string> onNotification)
    {
        var items = inventory.GetPocketItems(CurrentPocket);
        if (items.Count == 0) return;
        Follow(items.Count);
        var item = items[SelectedIndex].Data;

        // An item chosen for a script is only chosen: the script does what comes of it
        if (Choosing)
        {
            Chosen = item;
            AudioManager.PlaySound("select");
            Close();
            return;
        }

        if (Tossing)
        {
            int count = Math.Clamp(TossCount, 1, inventory.GetQuantity(item));
            inventory.RemoveItem(item, count);
            Tossing = false;
            AudioManager.PlaySound("select");
            onNotification(count == 1 ? $"Threw away the {item.Name}." : $"Threw away {count} × {item.Name}.");
            Follow(inventory.GetPocketItems(CurrentPocket).Count);
            return;
        }

        if (Actions == null)
        {
            var actions = ActionsFor(item, Registered, SoilAhead);
            if (actions.Count == 1)
            {
                onNotification("It can't be used here.");
                AudioManager.PlaySound("error");
                return;
            }
            Actions = actions;
            ActionIndex = 0;
            AudioManager.PlaySound("select");
            return;
        }

        var action = Actions[Math.Clamp(ActionIndex, 0, Actions.Count - 1)];
        Actions = null;
        switch (action)
        {
            case BagAction.Cancel:
                AudioManager.PlaySound("cancel");
                return;
            case BagAction.Register or BagAction.Deselect:
                Registered = action == BagAction.Register ? item.Name : null;
                AudioManager.PlaySound("select");
                onNotification(action == BagAction.Register ? $"The {item.Name} is ready on the item button." : $"The {item.Name} is off the item button.");
                return;
            case BagAction.Toss:
                Tossing = true;
                TossCount = 1;
                AudioManager.PlaySound("select");
                return;
            // A berry with soft soil ahead is planted there (UseBerryFromMenu), and anything used in the field
            case BagAction.Use when PlantsHere(item, SoilAhead) || UsedInField(item):
                usedInField = item;
                AudioManager.PlaySound("select");
                Close();
                return;
            case BagAction.Use when EncounterAids.IsAid(item):
                var (said, usedUp) = Aids.Use(item, PlayerName);
                if (usedUp) inventory.RemoveItem(item, 1);
                AudioManager.PlaySound(usedUp || item.Name.EndsWith("Flute") ? "select" : "error");
                onNotification(said);
                Follow(inventory.GetPocketItems(CurrentPocket).Count);
                return;
            case BagAction.Use when ItemUse.RevivesAll(item):
                // Sacred Ash brings round the whole team at once (Party_ApplyItemEffects to each)
                var revived = ItemUse.ReviveAll(item, party);
                if (revived.Count == 0)
                {
                    onNotification("It won't have any effect.");
                    AudioManager.PlaySound("error");
                    return;
                }
                inventory.RemoveItem(item, 1);
                AudioManager.PlaySound("heal");
                onNotification(revived.Count == 1 ? $"{revived[0]} came round, fully healed." : $"{string.Join(", ", revived)} came round, fully healed.");
                Follow(inventory.GetPocketItems(CurrentPocket).Count);
                return;
        }
        if (party.Count == 0)
        {
            onNotification("There is no Pokémon to give it to.");
            return;
        }
        BeginTargetChoice(item, give: action == BagAction.Give);
    }

    /// <summary>The B button: out of a step of the item's use, out of its actions, then out of the bag.</summary>
    public void Cancel()
    {
        if (Tossing) Tossing = false;
        else if (Actions != null) Actions = null;
        else Close();
        AudioManager.PlaySound("cancel");
    }

    /// <summary>Left and right change the number to throw away by one, up and down by ten, between one and all of them.</summary>
    public void MoveToss(int dx, int dy, int have)
    {
        if (!Tossing || have <= 0) return;
        int next = Math.Clamp(TossCount + dx - dy * 10, 1, have);
        if (next == TossCount) return;
        TossCount = next;
        AudioManager.PlaySound("cursor");
    }

    /// <param name="context">What evolutions need to know (the hour, the map); just the party and the bag when left out.</param>
    public void Update(Inventory inventory, Party party, Action<string> onNotification, EvolutionContext? context = null, float dt = 1f / 60f)
    {
        if (!IsActive) return;
        openAge += dt;
        tick.Update(dt);

        // Up or down kept down runs on through a pocket's list; everything else takes presses only
        bool list = choosingFor == null && Actions == null && !Tossing;
        int dx = InputManager.Axis(GameAction.Left, GameAction.Right);
        int dy = upDown.Advance(dt, InputManager.Axis(GameAction.Up, GameAction.Down),
            list ? InputManager.Axis(GameAction.Up, GameAction.Down, held: true) : 0);
        bool confirm = InputManager.IsActionPressed(GameAction.Confirm), cancel = InputManager.IsActionPressed(GameAction.Cancel);

        if (choosingFor != null)
        {
            choiceAge += dt;
            if (teaching != null)
            {
                if (dy != 0) MoveForget(dy);
                else if (cancel) { ForgetIndex = 4; ConfirmForget(inventory, onNotification); }
                else if (confirm) ConfirmForget(inventory, onNotification);
            }
            else if (ChoosingMove)
            {
                if (dy != 0) MoveMove(dy, party);
                else if (cancel) CancelMove();
                else if (confirm) UseOnMove(inventory, party, onNotification);
            }
            else if (dx != 0 || dy != 0) MoveTarget(dx, dy, party.Count);
            else if (cancel) CancelTarget();
            else if (confirm) UseOnTarget(inventory, party, onNotification, context ?? new EvolutionContext { Party = party, Bag = inventory });
            return;
        }

        if (Tossing)
        {
            var items = inventory.GetPocketItems(CurrentPocket);
            if (dx != 0 || dy != 0) MoveToss(dx, dy, items.Count > 0 ? items[Math.Min(SelectedIndex, items.Count - 1)].Quantity : 0);
            else if (cancel) Cancel();
            else if (confirm) Confirm(inventory, party, onNotification);
            return;
        }

        if (dx != 0) MovePocket(dx);
        else if (dy != 0)
        {
            int count = inventory.GetPocketItems(CurrentPocket).Count;
            for (int i = 0; i < Math.Abs(dy); i++) MoveCursor(Math.Sign(dy), count, upDown.Repeating);
        }

        // A button pressed while the list runs on still counts: it acts on the item the cursor has come to
        if (cancel) Cancel();
        else if (confirm) Confirm(inventory, party, onNotification);
    }

    // ---------------------------------------------------------------- items used on a Pokémon of the player's choosing

    /// <summary>Goes on to the party with an item: to use it if it can be used, to give it otherwise.</summary>
    public void BeginTargetChoice(ItemData item) => BeginTargetChoice(item, give: !CanUse(item));

    public void BeginTargetChoice(ItemData item, bool give)
    {
        choosingFor = item;
        giving = give;
        TargetIndex = 0;
        ChoosingMove = false;
        choiceAge = 0f;
        AudioManager.PlaySound("select");
    }

    public void MoveTarget(int dx, int dy, int count)
    {
        int next = UiNav.Grid(TargetIndex, count, 2, dx, dy);
        if (next == TargetIndex) return;
        TargetIndex = next;
        AudioManager.PlaySound("cursor");
    }

    public void CancelTarget()
    {
        choosingFor = null;
        ChoosingMove = false;
        AudioManager.PlaySound("cancel");
    }

    /// <summary>
    /// Uses the waiting item on the Pokémon under the cursor, or gives it to hold. An item used on one move goes on
    /// to the Pokémon's moves; a TM or an HM teaches its move, asking which to forget when there are four. An
    /// evolution it sets off is left for <see cref="TakeEvolution"/>: one from a Rare Candy's level can be stopped,
    /// one from a stone can't. Something that would have no effect stays in the bag and the choice stays open.
    /// </summary>
    public void UseOnTarget(Inventory inventory, Party party, Action<string> onNotification, EvolutionContext context)
    {
        if (choosingFor == null || TargetIndex >= party.Count) return;
        var item = choosingFor;
        var target = party.Members[TargetIndex];

        if (giving)
        {
            GiveToHold(item, inventory, target, onNotification);
        }
        else if (MoveTeaching.IsMachine(item))
        {
            // A TM or an HM: ABLE, NOT ABLE or LEARNED already (UseTMHMFromMenu)
            string move = item.TeachesMove!;
            switch (MoveTeaching.Answer(target, item))
            {
                case TeachAnswer.Learned:
                    onNotification($"{target.DisplayName} knows {move} already.");
                    AudioManager.PlaySound("error");
                    return;
                case TeachAnswer.NotAble:
                    onNotification($"{target.DisplayName} can't learn {move}.");
                    AudioManager.PlaySound("error");
                    return;
            }
            if (target.Moves.Count < 4)
            {
                onNotification(MoveTeaching.Learn(target, move));
                if (!MoveTeaching.IsHm(item)) inventory.RemoveItem(item, 1);
                AudioManager.PlayFanfare(MusicRole.FanfareLevelUp);
            }
            else
            {
                BeginTeaching(target, move, item);
                return;
            }
        }
        else if (ItemUse.NeedsMove(item))
        {
            // An Ether, a PP Up, a Leppa Berry: on to the Pokémon's moves
            if (!ItemUse.WouldHelpAnyMove(item, target))
            {
                onNotification("It won't have any effect.");
                AudioManager.PlaySound("error");
                return;
            }
            ChoosingMove = true;
            MoveIndex = 0;
            AudioManager.PlaySound("select");
            return;
        }
        else if (Evolution.IsUsedToEvolve(item) && !ItemUse.IsUsedOnPokemon(item))
        {
            context.Item = item;
            var evolution = Evolution.Find(target, EvolutionTrigger.UseItem, context);
            if (evolution == null)
            {
                onNotification("It won't have any effect.");
                return;
            }
            inventory.RemoveItem(item, 1);
            request = new EvolutionRequest(target, evolution, Cancellable: false);
        }
        else if (item.FieldUse == "Gracidea")
        {
            // Shaymin takes to the sky by day (Pokemon_CanShayminSkyForm); the flower stays in the bag
            if (!FormRules.CanTakeToTheSky(target, (int)GameClock.Hour))
            {
                onNotification("It won't have any effect.");
                return;
            }
            target.ChangeForm("Shaymin-Sky");
            AudioManager.PlayCry(target);
            onNotification($"{target.DisplayName} changed into its Sky Forme!");
        }
        else if (ItemUse.IsUsedOnPokemon(item) || EffortRules.IsEffortItem(item))
        {
            // Medicine, berries, vitamins and the Rare Candy, by the item table (Pokemon_ApplyItemEffects)
            var result = ItemUse.Apply(item, target);
            if (!result.Applied)
            {
                onNotification(result.Message);
                AudioManager.PlaySound("error");
                return;
            }
            inventory.RemoveItem(item, 1);
            onNotification(result.Message);
            if (result.NewLevel != null)
            {
                AudioManager.PlayFanfare(MusicRole.FanfareLevelUp);
                // The moves the level brings that don't fit are asked about one by one; the evolution waits for them
                grownByCandy = target;
                candyContext = context;
                context.Item = null;
                foreach (string wanted in result.Wanted) toTeach.Enqueue((target, wanted, null));
                if (NextTeaching()) return;
                CheckCandyEvolution();
            }
            else
            {
                AudioManager.PlaySound("heal");
                // While there is more of it, the party stays up for the next Pokémon, as in the games
                if (inventory.GetQuantity(item) > 0) return;
            }
        }
        else
        {
            GiveToHold(item, inventory, target, onNotification);
        }
        choosingFor = null;
    }

    // ---------------------------------------------------------------- the move an item is used on

    public void MoveMove(int dy, Party party)
    {
        if (!ChoosingMove || TargetIndex >= party.Count) return;
        int count = party.Members[TargetIndex].Moves.Count;
        if (count == 0) return;
        MoveIndex = UiNav.Wrap(MoveIndex, Math.Sign(dy), count);
        AudioManager.PlaySound("cursor");
    }

    public void CancelMove()
    {
        ChoosingMove = false;
        AudioManager.PlaySound("cancel");
    }

    /// <summary>Uses the waiting item on the move under the cursor; one it would do nothing for stays in the bag.</summary>
    public void UseOnMove(Inventory inventory, Party party, Action<string> onNotification)
    {
        if (!ChoosingMove || choosingFor is not { } item || TargetIndex >= party.Count) return;
        var target = party.Members[TargetIndex];
        var result = ItemUse.Apply(item, target, MoveIndex);
        if (!result.Applied)
        {
            onNotification(result.Message);
            AudioManager.PlaySound("error");
            return;
        }
        inventory.RemoveItem(item, 1);
        AudioManager.PlaySound("heal");
        onNotification(result.Message);
        ChoosingMove = false;
        choosingFor = null;
    }

    // ---------------------------------------------------------------- a move to learn when four are known

    private void BeginTeaching(Pokemon p, string move, ItemData? machine)
    {
        teaching = (p, move, machine);
        ForgetIndex = 0;
        AudioManager.PlaySound("select");
    }

    private bool NextTeaching()
    {
        while (toTeach.Count > 0)
        {
            var next = toTeach.Dequeue();
            if (next.Pokemon.Knows(next.Move)) continue;
            BeginTeaching(next.Pokemon, next.Move, next.Machine);
            return true;
        }
        teaching = null;
        return false;
    }

    public void MoveForget(int dy)
    {
        if (teaching == null) return;
        ForgetIndex = UiNav.Wrap(ForgetIndex, Math.Sign(dy), 5);
        AudioManager.PlaySound("cursor");
    }

    /// <summary>
    /// Forgets the move under the cursor for the new one (a TM is used up, an HM isn't), or, on the last row, gives
    /// the new move up. A move an HM taught can't be forgotten here.
    /// </summary>
    public void ConfirmForget(Inventory inventory, Action<string> onNotification)
    {
        if (teaching is not var (p, move, machine)) return;
        if (ForgetIndex >= 4 || ForgetIndex >= p.Moves.Count)
        {
            onNotification($"{p.DisplayName} did not learn {move}.");
            AudioManager.PlaySound("cancel");
        }
        else if (MoveTeaching.WhyNotForget(p, ForgetIndex) is { } why)
        {
            onNotification(why);
            AudioManager.PlaySound("error");
            return;
        }
        else
        {
            onNotification(MoveTeaching.Learn(p, move, ForgetIndex));
            if (machine != null && !MoveTeaching.IsHm(machine)) inventory.RemoveItem(machine, 1);
            AudioManager.PlayFanfare(MusicRole.FanfareLevelUp);
        }
        teaching = null;
        if (NextTeaching()) return;
        CheckCandyEvolution();
        choosingFor = null;
    }

    private void CheckCandyEvolution()
    {
        if (grownByCandy is { } grown && candyContext is { } context && Evolution.Find(grown, EvolutionTrigger.LevelUp, context) is { } evolution)
            request = new EvolutionRequest(grown, evolution, Cancellable: true);
        grownByCandy = null;
        candyContext = null;
    }

    /// <summary>Gives an item to a Pokémon to hold; whatever it held goes back in the bag.</summary>
    public static void GiveToHold(ItemData item, Inventory inventory, Pokemon holder, Action<string> onNotification)
    {
        inventory.RemoveItem(item, 1);
        var previous = holder.HeldItem;
        holder.HeldItem = item;
        AudioManager.PlaySound("select");
        // Giratina with the Griseous Orb and Arceus with a plate change form as they take it (plan 06 · R10)
        string changed = FormRules.ByHeldItem(holder) ? $" {holder.DisplayName} changed its form!" : "";
        if (previous != null)
        {
            inventory.AddItem(previous, 1);
            onNotification($"{holder.DisplayName} swapped its {previous.Name} for the {item.Name}.{changed}");
        }
        else onNotification($"{holder.DisplayName} was given the {item.Name} to hold.{changed}");
    }

    /// <summary>Whether the waiting item would do anything for a Pokémon: ABLE or NOT ABLE on its card (null: nothing to say).</summary>
    public bool? WouldWorkOn(Pokemon p, EvolutionContext probe)
    {
        if (choosingFor is not { } item || giving) return null;
        if (MoveTeaching.IsMachine(item)) return MoveTeaching.Answer(p, item) == TeachAnswer.Able;
        if (Evolution.IsUsedToEvolve(item) && !ItemUse.IsUsedOnPokemon(item))
        {
            probe.Item = item;
            return Evolution.Find(p, EvolutionTrigger.UseItem, probe) != null;
        }
        if (item.FieldUse == "Gracidea") return FormRules.CanTakeToTheSky(p, (int)GameClock.Hour);
        if (ItemUse.IsUsedOnPokemon(item) || EffortRules.IsEffortItem(item)) return ItemUse.WouldHelpAnyMove(item, p);
        return null;
    }

    /// <summary>What a Pokémon's card says while an item waits for it: ABLE or NOT ABLE, and for a TM or an HM LEARNED too.</summary>
    public string? TagFor(Pokemon p, EvolutionContext probe)
    {
        if (choosingFor is { } item && !giving && MoveTeaching.IsMachine(item))
            return MoveTeaching.Answer(p, item) switch { TeachAnswer.Able => "ABLE", TeachAnswer.Learned => "LEARNED", _ => "NOT ABLE" };
        return WouldWorkOn(p, probe) is { } can ? (can ? "ABLE" : "NOT ABLE") : null;
    }

    public void Draw(int screenWidth, int screenHeight, Inventory inventory, Party party, EvolutionContext? context = null)
    {
        if (!IsActive) return;

        if (choosingFor != null)
        {
            // An item used on a Pokémon says who it would work on, as the games do
            var item = choosingFor;
            var probe = context ?? new EvolutionContext { Party = party, Bag = inventory };
            string prompt = giving ? $"Give the {item.Name} to which Pokémon?"
                : MoveTeaching.IsMachine(item) ? $"Teach {item.TeachesMove} to which Pokémon?" : $"Use the {item.Name} on which Pokémon?";
            ModernUi.DrawPartyChoice(screenWidth, screenHeight, party, TargetIndex, prompt, p => TagFor(p, probe),
                Math.Clamp(choiceAge / ChoiceAppearTime, 0f, 1f), giving ? "Give" : "Use");
            var panel = new Rectangle(screenWidth - 780, 120, 716, 760);
            if (teaching is var (p, move, _)) ModernUi.MoveChoice(panel, p, move, ForgetIndex, 1f);
            else if (ChoosingMove && TargetIndex < party.Count)
                ModernUi.MovePick(panel, party.Members[TargetIndex], MoveIndex, $"Use the {item.Name} on which move?");
            return;
        }

        var shown = inventory.GetPocketItems(CurrentPocket);
        Follow(shown.Count);
        ModernUi.DrawBag(screenWidth, screenHeight, this, inventory, Math.Clamp(openAge / AppearTime, 0f, 1f));
        if (Tossing && shown.Count > 0)
            ModernUi.TossBox(screenWidth, screenHeight, shown[Math.Min(SelectedIndex, shown.Count - 1)].Data, TossCount);
    }
}
