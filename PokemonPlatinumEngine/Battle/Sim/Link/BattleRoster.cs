using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim.Link;

/// <summary>
/// A Pokémon of a battle as the wire names it (plan 28 · I1): the side it fights for, which of that side's teams
/// (0 the player's own or the first trainer's, 1 a partner's or the second trainer's, −1 the wild Pokémon) and its place
/// in that team. Each machine turns it back into its own copy through the teams it was given at the start
/// (<see cref="BattleRoster"/>): an event never carries a Pokémon over the wire, only this.
/// </summary>
public readonly record struct PokemonRef(BattleSide Side, int Party, int Index);

/// <summary>
/// Which Pokémon each <see cref="PokemonRef"/> of a battle stands for, both ways (plan 28 · I1). One roster is the whole
/// battle's (<see cref="Of"/>, what a server or a replay names the core's own Pokémon by); another is one side's view of it,
/// whose foes are stand-ins numbered as the team preview showed them (<see cref="SideView"/>). Looked up by reference, never
/// by value: two Pokémon alike in every field are still two. GPU-free.
/// </summary>
public sealed class BattleRoster
{
    private readonly Dictionary<Pokemon, PokemonRef> refs = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<PokemonRef, Pokemon> pokemon = new();
    private readonly Func<IEnumerable<(PokemonRef Ref, Pokemon Pokemon)>>? source;
    private readonly Func<PokemonRef, Pokemon?>? unknown;

    /// <param name="entries">Every Pokémon with its name.</param>
    /// <param name="source">Where to look again for a Pokémon that has come since (a wild ally joining); null for a fixed roster.</param>
    /// <param name="unknown">What a name the roster doesn't know stands for, made then and kept (a client's stand-in for a foe it hasn't been told of); null to refuse it.</param>
    public BattleRoster(IEnumerable<(PokemonRef Ref, Pokemon Pokemon)> entries,
        Func<IEnumerable<(PokemonRef Ref, Pokemon Pokemon)>>? source = null, Func<PokemonRef, Pokemon?>? unknown = null)
    {
        this.source = source;
        this.unknown = unknown;
        Add(entries);
    }

    /// <summary>Names one more Pokémon (a foe the preview didn't have, an ally); a name or a Pokémon already named keeps its first.</summary>
    public void Add(PokemonRef name, Pokemon p)
    {
        if (refs.ContainsKey(p) || pokemon.ContainsKey(name)) return;
        refs[p] = name;
        pokemon[name] = p;
    }

    private void Add(IEnumerable<(PokemonRef Ref, Pokemon Pokemon)> entries)
    {
        foreach (var (name, p) in entries) Add(name, p);
    }

    /// <summary>The name of a Pokémon of the battle; one the roster can't name is refused rather than guessed.</summary>
    public PokemonRef RefOf(Pokemon p)
    {
        if (refs.TryGetValue(p, out var name)) return name;
        if (source != null)
        {
            Add(source());
            if (refs.TryGetValue(p, out name)) return name;
        }
        throw new BattleWireException($"{p.Species.Name} is no Pokémon of this battle's roster");
    }

    /// <summary>Whether the roster names this Pokémon.</summary>
    public bool Knows(Pokemon p) => refs.ContainsKey(p);

    /// <summary>The Pokémon a name stands for on this machine.</summary>
    public Pokemon Resolve(PokemonRef name)
    {
        if (pokemon.TryGetValue(name, out var p)) return p;
        if (source != null)
        {
            Add(source());
            if (pokemon.TryGetValue(name, out p)) return p;
        }
        if (unknown?.Invoke(name) is { } made)
        {
            Add(name, made);
            return made;
        }
        throw new BattleWireException($"No Pokémon of this battle's roster is called {name}");
    }

    /// <summary>The whole battle's roster: every team as the core holds it, and the wild Pokémon as they come.</summary>
    public static BattleRoster Of(BattleCore core) => new(Entries(core), () => Entries(core));

    private static IEnumerable<(PokemonRef, Pokemon)> Entries(BattleCore core)
    {
        foreach (var e in Team(BattleSide.Player, 0, core.PlayerParty.Members)) yield return e;
        if (core.Partner != null)
            foreach (var e in Team(BattleSide.Player, 1, core.Partner.Party.Members)) yield return e;
        for (int t = 0; t < core.Trainers.Count; t++)
            foreach (var e in Team(BattleSide.Enemy, t, core.Trainers[t].Party.Members)) yield return e;
        foreach (var e in Team(BattleSide.Enemy, -1, core.WildPokemon)) yield return e;
    }

    /// <summary>A team's Pokémon named in their order.</summary>
    public static IEnumerable<(PokemonRef, Pokemon)> Team(BattleSide side, int party, IEnumerable<Pokemon> members) =>
        members.Select((p, i) => (new PokemonRef(side, party, i), p));
}

/// <summary>Something the wire can't say or read: a Pokémon no roster names, an event with no wire name, another version.</summary>
public sealed class BattleWireException : Exception
{
    public BattleWireException(string message) : base(message) { }
}
