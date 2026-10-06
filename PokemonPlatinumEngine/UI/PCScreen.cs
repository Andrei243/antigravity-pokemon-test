using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>Where the cursor is on the storage screen.</summary>
public enum StorageZone { Party, BoxName, Box }

/// <summary>
/// The Pokémon Storage System: the party beside a box of thirty slots, and the Pokémon under the cursor. The A
/// button puts a party Pokémon into storage or takes a stored one out. Stored Pokémon are one list for now,
/// shown thirty to a box in the order they were put in; boxes with slots of their own, moving Pokémon between
/// them, wallpapers and markings are plan 06 · R12's. Its logic takes no input (<see cref="Move"/>,
/// <see cref="Confirm"/>), so tests and the harness drive it.
/// </summary>
public class PCScreen
{
    private const float AppearTime = 0.3f;

    /// <summary>A box as Platinum has it: six across, five down, eighteen boxes.</summary>
    public const int Columns = 6, Rows = 5, BoxSize = Columns * Rows, BoxCount = 18;

    private float openAge;

    public StorageZone Zone { get; set; }
    public int PartyIndex { get; set; }

    /// <summary>The slot of the box the cursor is on, counted row by row.</summary>
    public int Cell { get; set; }
    public int Box { get; set; }
    public bool IsActive { get; set; }

    public void Open()
    {
        IsActive = true;
        Zone = StorageZone.Party;
        PartyIndex = 0;
        Cell = 0;
        Box = 0;
        openAge = 0f;
        AudioManager.PlaySound("pc_on");
    }

    public void Close() => IsActive = false;

    /// <summary>The place in the list of stored Pokémon that the cursor's slot stands for.</summary>
    public int StoredIndex => Box * BoxSize + Cell;

    /// <summary>The Pokémon under the cursor, if there is one.</summary>
    public Pokemon? Under(Party party, List<Pokemon> stored) => Zone switch
    {
        StorageZone.Party => PartyIndex < party.Count ? party.Members[PartyIndex] : null,
        StorageZone.Box => StoredIndex < stored.Count ? stored[StoredIndex] : null,
        _ => null
    };

    /// <summary>
    /// One step of the cursor. Up and down the party; sideways from the party into the box and from the box's
    /// edge back to it; up from the box's top row (or down from its last) to its name, where left and right
    /// change box.
    /// </summary>
    public void Move(int dx, int dy, int partyCount)
    {
        if (dx == 0 && dy == 0) return;
        int boxBefore = Box;
        int col = Cell % Columns, row = Cell / Columns;
        switch (Zone)
        {
            case StorageZone.Party:
                if (dy != 0 && partyCount > 0) PartyIndex = UiNav.Wrap(Math.Min(PartyIndex, partyCount - 1), dy, partyCount);
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
                        if (partyCount == 0) col = UiNav.Wrap(col, 0, Columns);
                        else
                        {
                            Zone = StorageZone.Party;
                            PartyIndex = Math.Min(row, partyCount - 1);
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

    /// <summary>The A button: a party Pokémon goes into storage (never the last one); a stored one joins the party if there is room.</summary>
    public void Confirm(Party party, List<Pokemon> stored, Action<string> onNotification)
    {
        if (Zone == StorageZone.Party)
        {
            if (PartyIndex >= party.Count) return;
            if (party.Count <= 1)
            {
                onNotification("That's your last Pokémon!");
                return;
            }
            if (stored.Count >= BoxSize * BoxCount)
            {
                onNotification("The boxes are full.");
                return;
            }
            var pokemon = party.Members[PartyIndex];
            party.RemoveAt(PartyIndex);
            stored.Add(pokemon);
            PartyIndex = Math.Min(PartyIndex, party.Count - 1);
            // The box it went into comes up, so it is seen to land
            Box = (stored.Count - 1) / BoxSize;
            AudioManager.PlaySound("select");
            onNotification($"{pokemon.DisplayName} was put in Box {Box + 1}.");
        }
        else if (Zone == StorageZone.Box)
        {
            if (StoredIndex >= stored.Count) return;
            if (party.IsFull)
            {
                onNotification("Your party is full.");
                return;
            }
            var pokemon = stored[StoredIndex];
            stored.RemoveAt(StoredIndex);
            party.Add(pokemon);
            AudioManager.PlaySound("select");
            onNotification($"{pokemon.DisplayName} joined your party.");
        }
    }

    public void Update(Party party, List<Pokemon> boxStorage, Action<string> onNotification, float dt = 1f / 60f)
    {
        if (!IsActive) return;
        openAge += dt;

        int dx = (InputManager.IsActionPressed(GameAction.Right) ? 1 : 0) - (InputManager.IsActionPressed(GameAction.Left) ? 1 : 0);
        int dy = (InputManager.IsActionPressed(GameAction.Down) ? 1 : 0) - (InputManager.IsActionPressed(GameAction.Up) ? 1 : 0);
        if (dx != 0 || dy != 0) Move(dx, dy, party.Count);
        else if (InputManager.IsActionPressed(GameAction.Cancel))
        {
            Close();
            AudioManager.PlaySound("pc_off");
        }
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm(party, boxStorage, onNotification);
    }

    public void Draw(int screenWidth, int screenHeight, Party party, List<Pokemon> boxStorage)
    {
        if (!IsActive) return;
        ModernUi.DrawStorage(screenWidth, screenHeight, this, party, boxStorage, Math.Clamp(openAge / AppearTime, 0f, 1f));
    }
}
