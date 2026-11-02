using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The S.S. Tidal's voyage counted in steps (plan 21 · H12, part k; Emerald's <c>CountSSTidalStep</c> under
/// <c>FLAG_SYS_CRUISE_MODE</c>): while the ship is under way (<c>VAR_SS_TIDAL_STATE</c> 1 or 2, or halfway, 5 or 6;
/// <c>ss_tidal_corridor.txt</c> has the values) every step taken aboard counts, and the 205th brings the ship in
/// (<see cref="CruiseArrives"/>), as a rest in the cabin's bed does.
/// </summary>
public partial class GameEngine
{
    /// <summary>Emerald's <c>SS_TIDAL_MAX_STEPS</c>.</summary>
    public const int CruiseMaxSteps = 205;

    public const string CruiseStateVariable = "VAR_SS_TIDAL_STATE";
    public const string CruiseStepVariable = "VAR_CRUISE_STEP_COUNT";

    /// <summary>The script the last step of a voyage starts.</summary>
    public const string CruiseArrives = "ss_tidal_corridor.ReachedStepCount";

    /// <summary>Whether the ship is under way: bound for either harbour, before or past halfway.</summary>
    public static bool UnderWay(int state) => state is 1 or 2 or 5 or 6;

    private bool CruiseStep()
    {
        if (!flow.Map.Name.StartsWith("SSTidal", StringComparison.Ordinal)) return false;
        if (!UnderWay(flow.Story.Var(CruiseStateVariable))) return false;
        int steps = flow.Story.Var(CruiseStepVariable) + 1;
        flow.Story.SetVar(CruiseStepVariable, steps);
        return steps >= CruiseMaxSteps && StartScript(CruiseArrives, file: "ss_tidal_corridor");
    }
}
