using System;
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
/// The camp's screen (plan 10 · F6), its logic alone: the team stands in two rows of three (or one row of as many as
/// there are), the cursor moves over them, Confirm pets the one under it and Feed asks for a berry. Its methods take
/// no input, so tests drive it. Its drawing (the stage, the six models, their faces and the hearts) and its place in
/// the START menu wait for a session that can look at it: until then nothing opens it.
/// </summary>
public sealed class CampScreen
{
    /// <summary>The team in two rows of this many.</summary>
    public const int Columns = 3;

    public bool IsActive { get; private set; }
    public Party? Party { get; private set; }
    public CampVisit Visit { get; private set; } = new();
    public int Cursor { get; private set; }

    /// <summary>The care the camp keeps between visits, which a visit's cure goes by.</summary>
    public int CuredOnArrival { get; private set; }

    private CampRequest request;

    /// <summary>Opens a visit with the team: a fresh visit (each may be petted and fed again), and the cure if a battle came between.</summary>
    public void Open(Party party, CampCare care)
    {
        Party = party;
        Visit = new CampVisit();
        Cursor = 0;
        request = CampRequest.None;
        CuredOnArrival = care.Cure(party);
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
        Cursor = Math.Min(index, Count - 1);
    }

    /// <summary>The Pokémon under the cursor.</summary>
    public Pokemon? Selected => Count == 0 ? null : Party!.Members[Cursor];

    /// <summary>Confirm pets the one under the cursor. True if it was petted.</summary>
    public bool Pet() => Selected is { } p && Visit.Pet(p);

    /// <summary>The player chose to feed the one under the cursor: the engine opens the bag's berries and hands one to <see cref="Feed"/>.</summary>
    public void AskToFeed()
    {
        if (Selected != null) request = CampRequest.Feed;
    }

    /// <summary>Feeds the one under the cursor the berry picked. True if it ate.</summary>
    public bool Feed(ItemData berry, Inventory bag) => Selected is { } p && Visit.Feed(p, berry, bag);

    /// <summary>Leaves the camp.</summary>
    public void Cancel()
    {
        request = CampRequest.Close;
        IsActive = false;
    }

    /// <summary>What the screen asks of the engine, once.</summary>
    public CampRequest TakeRequest()
    {
        var r = request;
        request = CampRequest.None;
        return r;
    }
}
