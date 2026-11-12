namespace PokemonPlatinumEngine.Story;

/// <summary>
/// The command of plan 35 · BN5: <c>pasture</c>, the pasture's (docs/scripts.md, "The pasture"). Every act is the host's
/// (<see cref="IScriptHost.Pasture"/>), which answers RESULT and may name a thing (<c>{thing}</c>: a box's name, a mood's
/// word) and an item (<c>{item}</c>). A host with no pasture answers 0 to everything.
/// </summary>
public sealed partial class ScriptRunner
{
    private void PastureCommand(Instruction i)
    {
        var answer = host.Pasture(i.Name, i.Number, Subject, lastItem);
        Result = answer.Result;
        if (answer.Thing.Length > 0) lastThing = answer.Thing;
        if (answer.Item != null) lastItem = answer.Item;
        if (i.Name is "box" or "count" or "job") lastNumber = answer.Result;
    }
}
