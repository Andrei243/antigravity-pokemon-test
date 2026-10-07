using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>Where the cursor is on the storage screen.</summary>
public enum StorageZone { Party, BoxName, Box }

/// <summary>What can be done with the Pokémon under the cursor (the box application's menu, plan 06 · R12).</summary>
public enum PcAction { Move, Withdraw, Store, Mark, Release, Cancel }

/// <summary>What can be done with the box whose name is under the cursor.</summary>
public enum PcBoxAction { Name, Wallpaper, Cancel }

/// <summary>
/// The Pokémon Storage System (plan 06 · R12): the party beside one of Platinum's eighteen boxes of thirty places
/// (<see cref="PcBoxes"/>). A on a Pokémon opens what can be done with it: pick it up to put down anywhere else
/// (swapping with whatever is there), take it onto the team or put it in the box, mark it, release it; with a
/// Pokémon picked up, A puts it down. A on the box's name renames the box or changes its wallpaper. The rules are
/// <see cref="PcBoxes"/>'s, and this keeps only the screen's state. Its logic takes no input (<see cref="Move"/>,
/// <see cref="Confirm"/>, <see cref="Cancel"/>, <see cref="Choose"/>), so tests and the harness drive it.
/// </summary>
public class PCScreen
{
    private const float AppearTime = 0.3f;

    /// <summary>A box as Platinum has it: six across, five down, eighteen boxes.</summary>
    public const int Columns = PcBoxes.Columns, Rows = PcBoxes.Rows, BoxSize = PcBoxes.BoxSize, BoxCount = PcBoxes.BoxCount;

    private float openAge;

    public StorageZone Zone { get; set; }
    public int PartyIndex { get; set; }

    /// <summary>The place of the box the cursor is on, counted row by row.</summary>
    public int Cell { get; set; }
    public int Box { get; set; }
    public bool IsActive { get; set; }

    /// <summary>The Pokémon picked up, carried by the cursor until it is put down, and where it came from.</summary>
    public Pokemon? Held { get; private set; }
    public (StorageZone Zone, int Box, int Index) HeldFrom { get; private set; }

    /// <summary>The menu of what to do with the Pokémon under the cursor, while it is open.</summary>
    public IReadOnlyList<PcAction>? Menu { get; private set; }
    public int MenuIndex { get; set; }

    /// <summary>The menu of what to do with the box, while it is open.</summary>
    public IReadOnlyList<PcBoxAction>? BoxMenu { get; private set; }

    /// <summary>The marks being chosen for the Pokémon under the cursor (MARK), or null.</summary>
    public Markings? Marking { get; private set; }
    public int MarkIndex { get; set; }

    /// <summary>A release waiting for its yes (RELEASE asks first).</summary>
    public bool AskingRelease { get; private set; }

    /// <summary>The box's new name being typed (NAME), or null.</summary>
    public NameEntry? Naming { get; private set; }

    public void Open(PcBoxes? pc = null)
    {
        IsActive = true;
        Zone = StorageZone.Party;
        PartyIndex = 0;
        Cell = 0;
        // The PC opens on the box it was last left on
        Box = pc?.CurrentBox ?? 0;
        Held = null;
        CloseMenus();
        openAge = 0f;
        AudioManager.PlaySound("pc_on");
    }

    public void Close() => IsActive = false;

    private void CloseMenus()
    {
        Menu = null;
        BoxMenu = null;
        Marking = null;
        AskingRelease = false;
        Naming = null;
        MenuIndex = 0;
    }

    /// <summary>Whether one of the screen's own menus or questions is open over the boxes.</summary>
    public bool Busy => Menu != null || BoxMenu != null || Marking != null || AskingRelease || Naming != null;

    /// <summary>The place in the boxes that the cursor's place stands for, counted from the first box.</summary>
    public int StoredIndex => Box * BoxSize + Cell;

    /// <summary>The Pokémon under the cursor, if there is one.</summary>
    public Pokemon? Under(Party party, PcBoxes pc) => Zone switch
    {
        StorageZone.Party => PartyIndex < party.Count ? party.Members[PartyIndex] : null,
        StorageZone.Box => pc[Box, Cell],
        _ => null
    };

    /// <summary>
    /// One step of the cursor. Up and down the party; sideways from the party into the box and from the box's
    /// edge back to it; up from the box's top row (or down from its last) to its name, where left and right
    /// change box. With a menu open, up and down move through it.
    /// </summary>
    public void Move(int dx, int dy, int partyCount)
    {
        if (dx == 0 && dy == 0) return;
        if (Naming != null) return;
        if (Marking != null)
        {
            MarkIndex = UiNav.Wrap(MarkIndex, dx + dy, 7);
            AudioManager.PlaySound("cursor");
            return;
        }
        if (Menu != null || BoxMenu != null)
        {
            if (dy != 0) MenuIndex = UiNav.Wrap(MenuIndex, dy, (Menu?.Count ?? BoxMenu!.Count));
            AudioManager.PlaySound("cursor");
            return;
        }
        if (AskingRelease) return;

        int boxBefore = Box;
        int col = Cell % Columns, row = Cell / Columns;
        // Someone carried can be put down on the team's empty place after its last member too
        int partyPlaces = Math.Min(Party.MaxSize, partyCount + (Held != null ? 1 : 0));
        switch (Zone)
        {
            case StorageZone.Party:
                if (dy != 0 && partyPlaces > 0) PartyIndex = UiNav.Wrap(Math.Min(PartyIndex, partyPlaces - 1), dy, partyPlaces);
                else if (dx != 0)
                {
                    // Into the box at the row beside this party card, on the near side
                    Zone = StorageZone.Box;
                    Cell = Math.Min(PartyIndex, Rows - 1) * Columns + (dx > 0 ? 0 : Columns - 1);
                }
                break;
            case StorageZone.BoxName:
                if (dx != 0) Box = UiNav.Wrap(Box, dx, BoxCount);
                else
                {
                    Zone = StorageZone.Box;
                    Cell = (dy > 0 ? 0 : Rows - 1) * Columns + col;
                }
                break;
            default:
                if (dx != 0)
                {
                    col += Math.Sign(dx);
                    if (col is < 0 or >= Columns)
                    {
                        if (partyPlaces == 0) col = UiNav.Wrap(col, 0, Columns);
                        else
                        {
                            Zone = StorageZone.Party;
                            PartyIndex = Math.Min(row, partyPlaces - 1);
                            break;
                        }
                    }
                    Cell = row * Columns + col;
                }
                else
                {
                    row += Math.Sign(dy);
                    if (row is < 0 or >= Rows) Zone = StorageZone.BoxName;
                    else Cell = row * Columns + col;
                }
                break;
        }
        AudioManager.PlaySound(Box != boxBefore ? "page" : "cursor");
    }

    /// <summary>What the menu offers for the Pokémon under the cursor.</summary>
    public IReadOnlyList<PcAction> ActionsFor(StorageZone zone) => zone == StorageZone.Party
        ? new[] { PcAction.Move, PcAction.Store, PcAction.Mark, PcAction.Release, PcAction.Cancel }
        : new[] { PcAction.Move, PcAction.Withdraw, PcAction.Mark, PcAction.Release, PcAction.Cancel };

    /// <summary>
    /// The A button: with a Pokémon picked up, puts it down where the cursor is; on a Pokémon, opens what can be
    /// done with it; on the box's name, opens what can be done with the box; in a menu, does what is chosen.
    /// </summary>
    public void Confirm(Party party, PcBoxes pc, Action<string> onNotification)
    {
        if (Naming != null) return;
        if (AskingRelease)
        {
            AnswerRelease(true, party, pc, onNotification);
            return;
        }
        if (Marking is { } marks)
        {
            // The six marks, then OK
            if (MarkIndex < 6) Marking = marks ^ (Markings)(1 << MarkIndex);
            else
            {
                if (Under(party, pc) is { } marked) marked.Marks = (int)marks;
                Marking = null;
            }
            AudioManager.PlaySound("select");
            return;
        }
        if (Menu is { } menu)
        {
            Choose(menu[MenuIndex], party, pc, onNotification);
            return;
        }
        if (BoxMenu is { } boxMenu)
        {
            ChooseForBox(boxMenu[MenuIndex], pc);
            return;
        }
        if (Held != null)
        {
            PutDown(party, pc, onNotification);
            return;
        }
        if (Zone == StorageZone.BoxName)
        {
            BoxMenu = new[] { PcBoxAction.Name, PcBoxAction.Wallpaper, PcBoxAction.Cancel };
            MenuIndex = 0;
            AudioManager.PlaySound("select");
            return;
        }
        if (Under(party, pc) == null) return;
        Menu = ActionsFor(Zone);
        MenuIndex = 0;
        AudioManager.PlaySound("select");
    }

    /// <summary>The B button: closes what is open, or puts a Pokémon carried back where it came from; false when there was nothing to back out of.</summary>
    public bool Cancel(Party party, PcBoxes pc)
    {
        if (Naming != null) Naming = null;
        else if (AskingRelease) AskingRelease = false;
        else if (Marking != null) Marking = null;
        else if (Menu != null || BoxMenu != null) { Menu = null; BoxMenu = null; }
        else if (Held != null) PutBack(party, pc);
        else return false;
        AudioManager.PlaySound("cancel");
        return true;
    }

    /// <summary>Does what was chosen in the Pokémon's menu.</summary>
    public void Choose(PcAction action, Party party, PcBoxes pc, Action<string> onNotification)
    {
        Menu = null;
        var pokemon = Under(party, pc);
        if (pokemon == null) return;
        switch (action)
        {
            case PcAction.Move:
                // Taken off the team, it must be one the team can spare
                if (Zone == StorageZone.Party && pc.WhyNotDeposit(party, PartyIndex) is { } stays && !stays.StartsWith("The boxes"))
                {
                    onNotification(stays);
                    return;
                }
                PickUp(party, pc);
                break;
            case PcAction.Withdraw:
                if (party.IsFull)
                {
                    onNotification("Your party is full.");
                    return;
                }
                pc.Withdraw(party, Box, Cell);
                AudioManager.PlaySound("select");
                onNotification($"{pokemon.DisplayName} joined your party.");
                break;
            case PcAction.Store:
            {
                if (pc.WhyNotDeposit(party, PartyIndex) is { } why)
                {
                    onNotification(why);
                    return;
                }
                // Into the box on the screen, or the next with room
                int box = Box;
                for (int i = 0; i < BoxCount && pc.Boxes[box].Count >= BoxSize; i++) box = (box + 1) % BoxCount;
                int slot = Array.IndexOf(pc.Boxes[box].Slots, null);
                pc.Deposit(party, PartyIndex, box, slot);
                Box = box;
                PartyIndex = Math.Max(0, Math.Min(PartyIndex, party.Count - 1));
                AudioManager.PlaySound("select");
                onNotification($"{pokemon.DisplayName} was put in {pc.Boxes[box].Name}.");
                break;
            }
            case PcAction.Mark:
                Marking = (Markings)pokemon.Marks;
                MarkIndex = 0;
                break;
            case PcAction.Release:
                AskingRelease = true;
                break;
        }
    }

    /// <summary>The answer to "Release it?": yes lets it go if <see cref="PcBoxes.WhetherToRelease"/> allows.</summary>
    public void AnswerRelease(bool yes, Party party, PcBoxes pc, Action<string> onNotification)
    {
        AskingRelease = false;
        if (!yes || Under(party, pc) is not { } pokemon) return;
        switch (pc.Release(pokemon, party))
        {
            case ReleaseOutcome.Released:
                PartyIndex = Math.Max(0, Math.Min(PartyIndex, party.Count - 1));
                onNotification($"{pokemon.DisplayName} was released outside. Bye-bye, {pokemon.DisplayName}!");
                break;
            case ReleaseOutcome.CameBack:
                onNotification($"{pokemon.DisplayName} came back! It must not want to leave you.");
                break;
            case ReleaseOutcome.HoldsMail:
                onNotification("Please remove the Mail first.");
                break;
            default:
                onNotification("That's your last Pokémon able to battle!");
                break;
        }
    }

    /// <summary>Does what was chosen in the box's menu: NAME starts typing its name, WALLPAPER puts up the next one it may have.</summary>
    public void ChooseForBox(PcBoxAction action, PcBoxes pc)
    {
        BoxMenu = null;
        if (action == PcBoxAction.Name) Naming = new NameEntry(pc.Boxes[Box].Name, PcBoxes.NameLength);
        else if (action == PcBoxAction.Wallpaper) NextWallpaper(pc);
    }

    /// <summary>Puts up the next wallpaper the PC has (the sixteen, and the eight once unlocked).</summary>
    public void NextWallpaper(PcBoxes pc)
    {
        var box = pc.Boxes[Box];
        int next = box.Wallpaper;
        do next = (next + 1) % PcBoxes.WallpaperNames.Count;
        while (!pc.HasWallpaper(next));
        box.Wallpaper = next;
        AudioManager.PlaySound("select");
    }

    /// <summary>Ends typing the box's name: given, it is the box's name from now on.</summary>
    public void FinishNaming(PcBoxes pc, string? name)
    {
        if (name != null) pc.Rename(Box, name);
        Naming = null;
    }

    private void PickUp(Party party, PcBoxes pc)
    {
        if (Zone == StorageZone.Party)
        {
            Held = party.Members[PartyIndex];
            HeldFrom = (StorageZone.Party, 0, PartyIndex);
            party.RemoveAt(PartyIndex);
        }
        else
        {
            Held = pc.TakeOut(Box, Cell);
            HeldFrom = (StorageZone.Box, Box, Cell);
        }
        AudioManager.PlaySound("select");
    }

    /// <summary>Puts the Pokémon carried down where the cursor is: into an empty place, or swapped with whoever is there.</summary>
    private void PutDown(Party party, PcBoxes pc, Action<string> onNotification)
    {
        var held = Held!;
        if (Zone == StorageZone.BoxName) return;
        if (Zone == StorageZone.Party)
        {
            if (PartyIndex < party.Count)
            {
                // Swapped with the member there, who is carried now
                var there = party.Members[PartyIndex];
                party.RemoveAt(PartyIndex);
                party.Insert(PartyIndex, FromTheBox(held));
                Held = there;
                HeldFrom = (StorageZone.Party, 0, PartyIndex);
            }
            else
            {
                party.Add(FromTheBox(held));
                Held = null;
            }
        }
        else
        {
            if (pc[Box, Cell] is { } there)
            {
                pc.TakeOut(Box, Cell);
                pc.PutIn(Box, Cell, held);
                Held = there;
                HeldFrom = (StorageZone.Box, Box, Cell);
            }
            else
            {
                pc.PutIn(Box, Cell, held);
                Held = null;
            }
        }
        AudioManager.PlaySound("select");
    }

    /// <summary>A Pokémon put on the team from the boxes comes out of them as withdrawing does.</summary>
    private Pokemon FromTheBox(Pokemon pokemon)
    {
        if (HeldFrom.Zone == StorageZone.Box) PcBoxes.AsWithdrawn(pokemon);
        return pokemon;
    }

    /// <summary>Puts a Pokémon carried back where it was picked up (or, if that place has been filled, anywhere with room).</summary>
    private void PutBack(Party party, PcBoxes pc)
    {
        var held = Held!;
        if (HeldFrom.Zone == StorageZone.Party && !party.IsFull) party.Insert(Math.Min(HeldFrom.Index, party.Count), held);
        else if (HeldFrom.Zone == StorageZone.Box && pc[HeldFrom.Box, HeldFrom.Index] == null) pc.PutIn(HeldFrom.Box, HeldFrom.Index, held);
        else if (!party.IsFull) party.Add(held);
        else pc.Store(held);
        Held = null;
    }

    public void Update(Party party, PcBoxes pc, Action<string> onNotification, float dt = 1f / 60f)
    {
        if (!IsActive) return;
        openAge += dt;

        if (Naming is { } naming)
        {
            // The keyboard's own keys while a box is being named
            int nx = (InputManager.IsActionPressed(GameAction.Right) ? 1 : 0) - (InputManager.IsActionPressed(GameAction.Left) ? 1 : 0);
            int ny = (InputManager.IsActionPressed(GameAction.Down) ? 1 : 0) - (InputManager.IsActionPressed(GameAction.Up) ? 1 : 0);
            if (nx != 0 || ny != 0) naming.Move(nx, ny);
            else if (InputManager.IsActionPressed(GameAction.Cancel)) { if (!naming.Backspace()) FinishNaming(pc, null); }
            else if (InputManager.IsActionPressed(GameAction.Menu)) naming.ToDone();
            else if (InputManager.IsActionPressed(GameAction.Confirm))
            {
                naming.Press();
                if (naming.Done) FinishNaming(pc, naming.Result(pc.Boxes[Box].Name));
            }
            return;
        }

        int dx = (InputManager.IsActionPressed(GameAction.Right) ? 1 : 0) - (InputManager.IsActionPressed(GameAction.Left) ? 1 : 0);
        int dy = (InputManager.IsActionPressed(GameAction.Down) ? 1 : 0) - (InputManager.IsActionPressed(GameAction.Up) ? 1 : 0);
        if (dx != 0 || dy != 0) Move(dx, dy, party.Count);
        else if (InputManager.IsActionPressed(GameAction.Cancel))
        {
            if (!Cancel(party, pc))
            {
                // The PC remembers the box it was left on
                pc.CurrentBox = Box;
                Close();
                AudioManager.PlaySound("pc_off");
            }
        }
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm(party, pc, onNotification);
    }

    public void Draw(int screenWidth, int screenHeight, Party party, PcBoxes pc)
    {
        if (!IsActive) return;
        ModernUi.DrawStorage(screenWidth, screenHeight, this, party, pc, Math.Clamp(openAge / AppearTime, 0f, 1f));
    }
}
