namespace PokemonPlatinumEngine.Story;

/// <summary>What an act of the Stroll Counter answered (plan 35 · BN7): the script's RESULT, and the words and number it names.</summary>
public readonly record struct StrollAnswer(int Result, string Thing = "", int Number = 0);

/// <summary>
/// The command of plan 35 · BN7: <c>stroll</c>, the Stroll Counter's at a PC (docs/scripts.md, "The Stroll Counter"). Every
/// act is the host's (<see cref="IScriptHost.Stroll"/>), which answers RESULT and may name a thing (<c>{thing}</c>: the
/// Pokémon's name) and a number (<c>{number}</c>). A host with no counter answers 0 to everything.
/// </summary>
public sealed partial class ScriptRunner
{
    private void StrollCommand(Instruction i)
    {
        var answer = host.Stroll(i.Name);
        Result = answer.Result;
        if (answer.Thing.Length > 0) lastThing = answer.Thing;
        if (i.Name is "back" or "caught" or "found") lastNumber = answer.Number;
    }
}
