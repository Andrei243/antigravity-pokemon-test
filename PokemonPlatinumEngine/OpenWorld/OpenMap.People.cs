using System;
using System.Collections.Generic;

namespace PokemonPlatinumEngine.OpenWorld;

/// <summary>
/// An open map's people (plan 32 · OW6): everyone laid on it, in the order laid, and kept by the square of
/// <see cref="PeopleSquare"/> they stand in as the grid's <c>PeopleList</c> keeps its people by squares of eight
/// tiles, so who stands near a place is asked of a few squares and never of everyone in the region.
/// </summary>
public sealed partial class OpenMap
{
    /// <summary>The side of a square of the people's index, in millimetres (the grid's eight tiles, as metres).</summary>
    public const int PeopleSquare = 8000;

    private readonly List<OpenPerson> people = new();
    private readonly Dictionary<string, OpenPerson> personByKey = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<(int, int), List<OpenPerson>> peopleSquares = new();

    /// <summary>Everyone of the map, present or not, in the order laid.</summary>
    public IReadOnlyList<OpenPerson> People => people;

    /// <summary>Adds someone; two of one name are refused, since a script names them.</summary>
    public void Add(OpenPerson person)
    {
        if (person.Index >= 0) throw new ArgumentException($"{person.Key} is someone of a map already");
        if (string.Equals(person.Key, "player", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("'player' is the player's name in a script; nobody else may have it");
        if (Marker(person.Key) != null)
            throw new ArgumentException($"open map {Name} has a marker and someone both named {person.Key}: a script's faceto couldn't tell them apart");
        if (!personByKey.TryAdd(person.Key, person)) throw new ArgumentException($"open map {Name} has two people named {person.Key}");
        person.Index = people.Count;
        people.Add(person);
        person.Square = SquareOf(person.Pos);
        SquareList(person.Square).Add(person);
    }

    /// <summary>Someone of the map by name (case aside), or null.</summary>
    public OpenPerson? Person(string key) => personByKey.GetValueOrDefault(key);

    /// <summary>Tells the index someone has moved (after their tick, a placing): kept under the square they stand in now.</summary>
    public void Moved(OpenPerson person)
    {
        var square = SquareOf(person.Pos);
        if (square == person.Square) return;
        peopleSquares[person.Square].Remove(person);
        person.Square = square;
        SquareList(square).Add(person);
    }

    /// <summary>
    /// Everyone standing within <paramref name="reach"/> millimetres of a place on the ground, present or not, in the
    /// order laid: into the caller's list, which is cleared first.
    /// </summary>
    public List<OpenPerson> PeopleNear(OpenPos at, int reach, List<OpenPerson>? into = null)
    {
        into ??= new List<OpenPerson>();
        into.Clear();
        var (x0, z0) = SquareOf(at.Plus(-reach, 0, -reach));
        var (x1, z1) = SquareOf(at.Plus(reach, 0, reach));
        long reach2 = (long)reach * reach;
        for (int z = z0; z <= z1; z++)
            for (int x = x0; x <= x1; x++)
                if (peopleSquares.TryGetValue((x, z), out var list))
                    foreach (var p in list)
                        if (p.Pos.GroundDistanceSquared(at) <= reach2) into.Add(p);
        into.Sort((a, b) => a.Index.CompareTo(b.Index));
        return into;
    }

    private List<OpenPerson> SquareList((int, int) square)
    {
        if (!peopleSquares.TryGetValue(square, out var list)) peopleSquares[square] = list = new List<OpenPerson>();
        return list;
    }

    private static (int X, int Z) SquareOf(OpenPos p) => (FloorDiv(p.X, PeopleSquare), FloorDiv(p.Z, PeopleSquare));
}
