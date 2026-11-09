using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// Who speaks each line of a script by the way the runner carries it out (plan 26 · Kanto's session): the runner starts a
/// script as the person spoken to, a <c>speaker</c> holds until another comes (across a <c>goto</c>, a <c>call</c> and its
/// return), so the speaker of a line is a question about every way to it, which a linear read of the file (the file's
/// order, a label forgetting nothing) answers wrongly after a label that is jumped to. This walks the ways: each
/// instruction gets the set of voices that can be in force when it runs. GPU-free.
/// </summary>
public sealed class VoiceFlow
{
    /// <summary>The voice of "the person spoken to" before it is decided, and of nobody (<c>speaker none</c>).</summary>
    public const string Self = VoiceCast.Self, None = "\u0001none";

    private readonly Dictionary<Script, HashSet<string>> entry = new();
    private readonly Dictionary<Script, HashSet<string>> exit = new();
    private readonly Dictionary<Instruction, HashSet<string>> at = new(ReferenceEqualityComparer.Instance);
    private readonly Func<Script, Instruction, string> speakerOf;
    private readonly Func<string, string, Script?> find;

    /// <summary>
    /// <paramref name="speakerOf"/> gives the token a <c>speaker</c> line sets (a cast voice, <see cref="Self"/> where the cast has none);
    /// <paramref name="find"/> finds the script a <c>call</c> names, from the calling script's file.
    /// </summary>
    public VoiceFlow(IEnumerable<Script> scripts, Func<Script, Instruction, string> speakerOf, Func<string, string, Script?> find)
    {
        this.speakerOf = speakerOf;
        this.find = find;
        var all = scripts.ToList();
        foreach (var s in all) { entry[s] = new HashSet<string> { Self }; exit[s] = new HashSet<string>(); }
        bool changed;
        int rounds = 0;
        do
        {
            changed = false;
            foreach (var s in all) changed |= Walk(s);
        } while (changed && ++rounds < 64);
    }

    /// <summary>The voices that can be in force as the instruction runs (<see cref="Self"/>, <see cref="None"/> and cast voices); empty where no way reaches it.</summary>
    public IReadOnlyCollection<string> Before(Instruction i) => at.TryGetValue(i, out var set) ? set : Array.Empty<string>();

    private bool Walk(Script s)
    {
        bool changed = false;
        var code = s.Code;
        var states = new HashSet<string>?[code.Count + 1];
        states[0] = new HashSet<string>(entry[s]);
        var work = new Queue<int>();
        work.Enqueue(0);
        var queued = new HashSet<int> { 0 };

        void Flow(int to, HashSet<string> tokens)
        {
            if (to < 0 || to >= code.Count) return;
            var have = states[to] ??= new HashSet<string>();
            int before = have.Count;
            have.UnionWith(tokens);
            if (have.Count != before && queued.Add(to)) work.Enqueue(to);
        }

        while (work.Count > 0)
        {
            int n = work.Dequeue();
            queued.Remove(n);
            var state = states[n]!;
            var i = code[n];
            if (Record(i, state)) changed = true;
            var next = Apply(s, i, state, out bool falls, ref changed, Flow);
            if (i.Then != null)
            {
                if (Record(i.Then, state)) changed = true;
                var after = Apply(s, i.Then, state, out bool thenFalls, ref changed, Flow);
                // The line an if guards is carried out, and the script goes on (or not) from there; if the question is no, it goes on as it was
                var both = new HashSet<string>(state);
                if (thenFalls) both.UnionWith(after);
                if (falls || thenFalls) Flow(n + 1, both);
                continue;
            }
            if (falls) Flow(n + 1, next);
        }
        return changed;
    }

    private bool Record(Instruction i, HashSet<string> state)
    {
        if (!at.TryGetValue(i, out var set)) at[i] = set = new HashSet<string>();
        int before = set.Count;
        set.UnionWith(state);
        return set.Count != before;
    }

    /// <summary>What an instruction does to the voices in force; whether the script goes on to the next one.</summary>
    private HashSet<string> Apply(Script s, Instruction i, HashSet<string> state, out bool falls, ref bool changed, Action<int, HashSet<string>> flow)
    {
        falls = true;
        switch (i.Op)
        {
            case Op.Speaker:
                if (i.Name.Length > 0) return new HashSet<string> { speakerOf(s, i) };
                return new HashSet<string> { i.Option ? Self : None };
            case Op.Goto:
                flow(i.Target, state);
                falls = false;
                return state;
            case Op.Call:
            {
                var target = find(i.Name, s.File);
                if (target == null) return state;
                int before = entry[target].Count;
                entry[target].UnionWith(state);
                if (entry[target].Count != before) changed = true;
                // What is in force once it returns: what it left, or nothing known yet (the way after is then not reached until it has been walked)
                return new HashSet<string>(exit[target]);
            }
            case Op.Return:
            {
                int before = exit[s].Count;
                exit[s].UnionWith(state);
                if (exit[s].Count != before) changed = true;
                falls = false;
                return state;
            }
            case Op.End:
                falls = false;
                return state;
            default:
                return state;
        }
    }
}
