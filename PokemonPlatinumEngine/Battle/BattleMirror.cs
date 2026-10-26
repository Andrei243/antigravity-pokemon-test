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
        var copy = new Party { Limit = party.Limit };
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
        Format = trainer.Format,
        Ai = trainer.Ai,
        Items = new List<string>(trainer.Items),
        KeyStone = trainer.KeyStone,
        ZRing = trainer.ZRing,
        DynamaxBand = trainer.DynamaxBand,
        MegaMaxBand = trainer.MegaMaxBand,
        TeraOrb = trainer.TeraOrb
    };

    /// <summary>The game's own Pokémon that one of the rules' copies stands for.</summary>
    public Pokemon Shown(Pokemon copy)
    {
        if (shownOf.TryGetValue(copy, out var shown)) return shown;
        // One the rules made in the middle of the battle and the screen hasn't been told of yet (plan 14 · B5): taken in as it is
        Adopt(copy, copy.Clone());
        return shownOf[copy];
    }

    /// <summary>
    /// Takes in a Pokémon the rules made themselves in the middle of the battle (plan 14 · B5: an ally that answered a
    /// call), with <paramref name="shown"/> the copy of it as it came, which the screen shows from now on.
    /// </summary>
    public void Adopt(Pokemon copy, Pokemon shown)
    {
        if (shownOf.ContainsKey(copy)) return;
        shownOf[copy] = shown;
        copyOf[shown] = copy;
    }

    /// <summary>
    /// Brings the rules' copies up to the game's own, before the rules are asked anything. Between turns the two
    /// are the same unless something outside the battle changed a Pokémon (a test setting a burn, the harness
    /// staging a picture), and the rules have to know of it.
    /// </summary>
    /// <returns>
    /// Whether any of them had changed in what the rules fight with (HP, status, level, held item, moves and PP): a
    /// change no answer of the record holds, so a record of the battle no longer replays (plan 16 · T19's follow-up).
    /// </returns>
    public bool Adopt()
    {
        bool changed = false;
        foreach (var (shown, copy) in copyOf)
        {
            if (!changed && !Alike(shown, copy)) changed = true;
            copy.CopyStateFrom(shown);
        }
        return changed;
    }

    private static bool Alike(Pokemon a, Pokemon b)
    {
        if (a.CurrentHP != b.CurrentHP || a.Status != b.Status || a.Level != b.Level || a.HeldItem?.Name != b.HeldItem?.Name || a.Moves.Count != b.Moves.Count) return false;
        for (int i = 0; i < a.Moves.Count; i++)
            if (a.Moves[i].Data.Name != b.Moves[i].Data.Name || a.Moves[i].CurrentPP != b.Moves[i].CurrentPP) return false;
        return true;
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
