using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Models;

/// <summary>A kind of thing a room can be decorated with: its name and the tiles it takes, east and south from its corner.</summary>
public sealed record DecorationKind(int Id, string Name, int Width, int Depth);

/// <summary>One thing in the store a room is decorated from: its kind, and whether it stands in the room now.</summary>
public sealed class StoredDecoration
{
    public int Kind { get; init; }
    public bool Out { get; set; }
}

/// <summary>
/// Something standing in a room being decorated: a decoration out of the store (<see cref="Slot"/> is its place
/// there), or a fixture of the room's own (<see cref="Slot"/> -1: the Underground base's PC and boulders), which the
/// room says whether it may be moved or taken away.
/// </summary>
public sealed class RoomPiece
{
    public int Kind { get; init; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; init; } = 1;
    public int Depth { get; init; } = 1;
    public int Slot { get; init; } = -1;
    public string Name { get; init; } = "";

    public bool IsFixture => Slot < 0;
    public bool Covers(int x, int y) => x >= X && x < X + Width && y >= Y && y < Y + Depth;
    public bool Overlaps(int x, int y, int width, int depth) => X < x + width && x < X + Width && Y < y + depth && y < Y + Depth;
}

/// <summary>
/// A room to decorate (the shared seam of plan 06 · R16a's Underground base and plan 21 · H10's Secret Bases): the
/// floor things may stand on, the tiles that must stay clear (before a PC, inside a doorway), what stands there, and
/// how many decorations may be out at once. Its two questions are the room's own rules: whether a fixture may be
/// moved, and whether a piece may be taken away (a reason why not, in words to show; null when it may).
/// </summary>
public sealed class DecorationRoom
{
    public int Left { get; init; }
    public int Top { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public List<(int X, int Y)> KeptClear { get; init; } = new();
    public List<RoomPiece> Pieces { get; init; } = new();
    public int Limit { get; init; } = int.MaxValue;

    /// <summary>Where a piece taken out of the store first hangs, and where the cursor starts.</summary>
    public (int X, int Y) Start { get; init; }

    public Func<RoomPiece, string?> WhyNotMove { get; init; } = _ => null;
    public Func<RoomPiece, string?> WhyNotTakeAway { get; init; } = _ => null;

    /// <summary>What to say once a fixture has been taken away for good (a boulder cleared); null to say nothing more.</summary>
    public Func<RoomPiece, string?> AfterTakingAway { get; init; } = _ => null;

    public bool Inside(int x, int y, int width = 1, int depth = 1) => x >= Left && y >= Top && x + width <= Left + Width && y + depth <= Top + Height;

    public RoomPiece? PieceAt(int x, int y) => Pieces.FirstOrDefault(p => p.Covers(x, y));

    /// <summary>How many decorations out of the store stand in the room.</summary>
    public int Out => Pieces.Count(p => !p.IsFixture);

    /// <summary>Whether a piece of a size may be set down with its corner on a tile: on the floor, on nothing else (but <paramref name="except"/>), off the tiles kept clear.</summary>
    public bool CanStand(int x, int y, int width, int depth, RoomPiece? except = null) =>
        Inside(x, y, width, depth)
        && !Pieces.Any(p => p != except && p.Overlaps(x, y, width, depth))
        && !KeptClear.Any(t => t.X >= x && t.X < x + width && t.Y >= y && t.Y < y + depth);
}

/// <summary>What the decorating is at.</summary>
public enum DecoratingStep
{
    /// <summary>DECORATE, PUT AWAY, MOVE or DONE.</summary>
    Menu,
    /// <summary>The store's list, to take a decoration from.</summary>
    Choosing,
    /// <summary>A decoration in hand over the room, to be set down.</summary>
    Placing,
    /// <summary>The cursor over the room, to take something away.</summary>
    PuttingAway,
    /// <summary>The cursor over the room, to pick something up.</summary>
    Picking,
    /// <summary>Something picked up, to be set down again.</summary>
    Moving,
    Done
}

/// <summary>
/// A room being decorated (plan 06 · R16a; the original's <c>src/underground/base_decoration.c</c>, which Hoenn's
/// Secret Bases share the shape of): a decoration is taken from the store and carried over the floor to be set down;
/// something standing is put away, or picked up and set down elsewhere. The arrows, confirm and cancel are its only
/// buttons, and it has no drawing or input of its own: <c>UI/DecorationScreen</c> is its face. It changes the room's
/// pieces and the store's flags as it goes; whoever owns them reads them back when it is done.
/// </summary>
public sealed class DecoratingSession
{
    public static readonly string[] MenuRows = { "DECORATE", "PUT AWAY", "MOVE", "DONE" };

    public DecorationRoom Room { get; }
    public IReadOnlyList<StoredDecoration> Store { get; }
    private readonly Func<int, DecorationKind> catalogue;

    public DecoratingStep Step { get; private set; } = DecoratingStep.Menu;
    public int MenuCursor { get; private set; }
    public int StoreCursor { get; private set; }

    /// <summary>The cursor over the room, or the corner of the piece in hand.</summary>
    public int X { get; private set; }
    public int Y { get; private set; }

    /// <summary>The piece in hand, if any: out of the store (<see cref="DecoratingStep.Placing"/>) or picked up off the floor.</summary>
    public RoomPiece? Held { get; private set; }

    /// <summary>The last thing said, until the next button: "It can't be placed there.", "The Yellow Cushion was put away."</summary>
    public string? Message { get; private set; }

    /// <summary>Whether the last press of the button was turned away (what <see cref="Message"/> says is why).</summary>
    public bool Refused { get; private set; }

    /// <summary>Whether anything was set down, put away or moved.</summary>
    public bool Changed { get; private set; }

    private (int X, int Y) heldFrom;

    public DecoratingSession(DecorationRoom room, IReadOnlyList<StoredDecoration> store, Func<int, DecorationKind> catalogue)
    {
        Room = room;
        Store = store;
        this.catalogue = catalogue;
        (X, Y) = room.Start;
    }

    public DecorationKind KindOf(int kind) => catalogue(kind);

    /// <summary>Whether the piece in hand could be set down where it hangs.</summary>
    public bool Fits => Held != null && Room.CanStand(X, Y, Held.Width, Held.Depth, Held);

    /// <summary>What the cursor is on while something is to be put away or picked up.</summary>
    public RoomPiece? Under => Step is DecoratingStep.PuttingAway or DecoratingStep.Picking ? Room.PieceAt(X, Y) : null;

    public void Move(int dx, int dy)
    {
        if (dx == 0 && dy == 0) return;
        Message = null;
        Refused = false;
        switch (Step)
        {
            case DecoratingStep.Menu:
                if (dy != 0) MenuCursor = ((MenuCursor + Math.Sign(dy)) % MenuRows.Length + MenuRows.Length) % MenuRows.Length;
                break;
            case DecoratingStep.Choosing:
                if (dy != 0 && Store.Count > 0) StoreCursor = Math.Clamp(StoreCursor + Math.Sign(dy), 0, Store.Count - 1);
                break;
            case DecoratingStep.Placing or DecoratingStep.Moving when Held != null:
                if (Room.Inside(X + dx, Y + dy, Held.Width, Held.Depth)) (X, Y) = (X + dx, Y + dy);
                break;
            case DecoratingStep.PuttingAway or DecoratingStep.Picking:
                if (Room.Inside(X + dx, Y + dy)) (X, Y) = (X + dx, Y + dy);
                break;
        }
    }

    private void Refuse(string why)
    {
        Message = why;
        Refused = true;
    }

    public void Confirm()
    {
        Message = null;
        Refused = false;
        switch (Step)
        {
            case DecoratingStep.Menu:
                switch (MenuCursor)
                {
                    case 0:
                        if (Store.Count == 0) Refuse("There's nothing in store to put out.");
                        else if (Room.Out + 1 > Room.Limit) Refuse("No more can be put out here.");
                        else
                        {
                            Step = DecoratingStep.Choosing;
                            StoreCursor = Math.Clamp(StoreCursor, 0, Store.Count - 1);
                        }
                        break;
                    case 1:
                        Step = DecoratingStep.PuttingAway;
                        break;
                    case 2:
                        Step = DecoratingStep.Picking;
                        break;
                    default:
                        Step = DecoratingStep.Done;
                        break;
                }
                break;
            case DecoratingStep.Choosing:
            {
                var entry = Store[StoreCursor];
                if (entry.Out)
                {
                    Refuse("That one is out already.");
                    break;
                }
                var kind = catalogue(entry.Kind);
                Held = new RoomPiece { Kind = kind.Id, Name = kind.Name, Width = kind.Width, Depth = kind.Depth, Slot = StoreCursor };
                (X, Y) = Clamped(Room.Start, Held);
                Step = DecoratingStep.Placing;
                break;
            }
            case DecoratingStep.Placing when Held != null:
                if (!Fits)
                {
                    Refuse("It can't be placed there.");
                    break;
                }
                Held.X = X;
                Held.Y = Y;
                Room.Pieces.Add(Held);
                Store[Held.Slot].Out = true;
                Message = $"The {Held.Name} was put out.";
                Held = null;
                Changed = true;
                Step = DecoratingStep.Menu;
                break;
            case DecoratingStep.PuttingAway:
            {
                if (Room.PieceAt(X, Y) is not { } piece) break;
                if (Room.WhyNotTakeAway(piece) is { } why)
                {
                    Refuse(why);
                    break;
                }
                Room.Pieces.Remove(piece);
                Changed = true;
                if (piece.IsFixture)
                {
                    Message = Room.AfterTakingAway(piece) ?? $"The {piece.Name} was taken away.";
                    Step = DecoratingStep.Menu;
                }
                else
                {
                    Store[piece.Slot].Out = false;
                    Message = $"The {piece.Name} was put away.";
                }
                break;
            }
            case DecoratingStep.Picking:
            {
                if (Room.PieceAt(X, Y) is not { } piece) break;
                if (Room.WhyNotMove(piece) is { } why)
                {
                    Refuse(why);
                    break;
                }
                Held = piece;
                heldFrom = (piece.X, piece.Y);
                (X, Y) = (piece.X, piece.Y);
                Step = DecoratingStep.Moving;
                break;
            }
            case DecoratingStep.Moving when Held != null:
                if (!Fits)
                {
                    Refuse("It can't be placed there.");
                    break;
                }
                Changed |= (Held.X, Held.Y) != (X, Y);
                Held.X = X;
                Held.Y = Y;
                Held = null;
                Step = DecoratingStep.Picking;
                break;
        }
    }

    public void Cancel()
    {
        Message = null;
        Refused = false;
        switch (Step)
        {
            case DecoratingStep.Menu:
                Step = DecoratingStep.Done;
                break;
            case DecoratingStep.Placing:
                // Back to the list with nothing set down
                Held = null;
                Step = DecoratingStep.Choosing;
                break;
            case DecoratingStep.Moving when Held != null:
                // Back where it stood
                (Held.X, Held.Y) = heldFrom;
                (X, Y) = heldFrom;
                Held = null;
                Step = DecoratingStep.Picking;
                break;
            default:
                Step = DecoratingStep.Menu;
                break;
        }
    }

    private (int X, int Y) Clamped((int X, int Y) at, RoomPiece piece) =>
        (Math.Clamp(at.X, Room.Left, Room.Left + Room.Width - piece.Width), Math.Clamp(at.Y, Room.Top, Room.Top + Room.Height - piece.Depth));
}
