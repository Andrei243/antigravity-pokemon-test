using System.Collections.Generic;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

/// <summary>
/// The two sets of Pokémon a battle on screen has. The game's own are what the screen shows and what the party
/// keeps afterwards; the battle's rules (<see cref="Sim.BattleCore"/>) work on copies, because they work a whole
/// turn out at once and the screen must not show a hit before its line has been read. This keeps the two paired.
/// </summary>
internal sealed class BattleMirror
{
    private readonly Dictionary<Pokemon, Pokemon> shownOf = new(), copyOf = new();

    /// <summary>The copy the rules work on (made the first time it is asked for).</summary>
    public Pokemon Copy(Pokemon shown)
    {
        if (copyOf.TryGetValue(shown, out var copy)) return copy;
        copy = shown.Clone();
        copyOf[shown] = copy;
        shownOf[copy] = shown;
        return copy;
    }

    public Party Copy(Party party)
    {
        var copy = new Party();
        foreach (var p in party.Members) copy.Add(Copy(p));
        return copy;
    }

    public Trainer Copy(Trainer trainer) => new()
    {
        Id = trainer.Id,
        // The rival's data names him "{rival}": the battle calls him what the player did
        Name = PlayerIdentity.Fill(trainer.Name),
        TrainerClass = trainer.TrainerClass,
        Look = trainer.Look,
        Party = Copy(trainer.Party),
        PrizeMoney = trainer.PrizeMoney,
        DoubleBattle = trainer.DoubleBattle,
        Ai = trainer.Ai,
        Items = new List<string>(trainer.Items)
    };

    /// <summary>The game's own Pokémon that one of the rules' copies stands for.</summary>
    public Pokemon Shown(Pokemon copy) => shownOf[copy];

    /// <summary>
    /// Brings the rules' copies up to the game's own, before the rules are asked anything. Between turns the two
    /// are the same unless something outside the battle changed a Pokémon (a test setting a burn, the harness
    /// staging a picture), and the rules have to know of it.
    /// </summary>
    public void Adopt()
    {
        foreach (var (shown, copy) in copyOf) copy.CopyStateFrom(shown);
    }

    /// <summary>
    /// Brings the game's own up to the rules' copies, once everything the rules worked out has been shown. The
    /// log already changed what the screen shows along the way (HP, conditions, levels); this is what makes sure
    /// nothing else is left behind.
    /// </summary>
    public void Publish()
    {
        foreach (var (copy, shown) in shownOf) shown.CopyStateFrom(copy);
    }
}
