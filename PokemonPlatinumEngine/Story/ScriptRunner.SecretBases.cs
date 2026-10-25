using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// The command of plan 21 · H10: <c>openbase</c>, a secret base of Hoenn's (<see cref="SecretBases"/>), on the lines of
/// Emerald's scripts it stands for (<c>data/scripts/secret_base.inc</c>, <c>pokemartdecoration</c>).
/// </summary>
public sealed partial class ScriptRunner
{
    /// <summary>
    /// <c>give</c> puts a decoration into the decoration PC without a word (RESULT 1, or 0 with no room for its kind;
    /// <c>{thing}</c> names it); <c>buy</c> does as a decoration shop does (RESULT 1 bought, 0 short of money, 2 no room),
    /// and <c>room</c> is 1 while there is room for one more of its kind. The rest are the field's: <c>open</c> (no word)
    /// makes the spot ahead the player's base, emptying the one before, and goes in to stand before its PC; <c>check</c>
    /// is 0 with no spot ahead, 1 at a spot while the player has no base, 2 at the player's own base, 3 at another spot
    /// while the player has a base elsewhere; <c>kind</c> is the spot's kind (1 to 6, Emerald's order: the red, brown,
    /// blue and yellow caves, a tree, a shrub); <c>enter</c> goes into the player's base at its door; <c>pc</c> opens the
    /// decorating and the script waits; <c>packup</c> takes everything back into the PC, gives the base up and goes out.
    /// </summary>
    private void OpenBase(Instruction i)
    {
        var store = host.Decorations;
        var decoration = i.Other is { Length: > 0 } name ? DecorationDatabase.Get(name) : null;
        switch (i.Name)
        {
            case "give":
                lastThing = decoration!.Name;
                Result = SecretBases.Add(store, decoration.Id) ? 1 : 0;
                break;
            case "room":
                Result = SecretBases.HasRoom(store, decoration!.Id) ? 1 : 0;
                break;
            case "buy":
                lastThing = decoration!.Name;
                if (!SecretBases.HasRoom(store, decoration.Id)) Result = 2;
                else if (host.Money < decoration.Price) Result = 0;
                else
                {
                    host.Money -= decoration.Price;
                    SecretBases.Add(store, decoration.Id);
                    Result = 1;
                }
                break;
            default:
                Result = host.SecretBaseAct(i.Name);
                break;
        }
    }
}
