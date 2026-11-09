using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// Cooking at the camp and the powers its dishes start (plan 39 · CR6, which took plan 10 · F8 over;
/// <c>docs/mechanics/rulings.md</c>, "Materials, crafting and cooking" and "Powers"). With the crafting kit in the bag the
/// camp is offered whatever the FOLLOWER row says, its pot cooks from the kit's pantry and the bag, and the dish's power
/// is kept in <see cref="GameFlow.Powers"/>, counted down from the play time and told once on the toast as it wears off.
/// The rules are GPU-free in <c>Models/Kitchen.cs</c> and <c>Models/Powers.cs</c>.
/// </summary>
public partial class GameEngine
{
    // The powers that wore off and haven't been told yet
    private List<PowerKind>? wornOffToTell;

    /// <summary>
    /// What the camp cooks with: the kit's pouch, the bag, the powers and the timing assist, with a seed worked out from
    /// the play time (no draw: a game that never cooks rolls as before). None without the kit.
    /// </summary>
    private CampKitchen? CampKitchenNow() => HasCraftingKit
        ? new CampKitchen(flow.Crafting.Pouch, flow.Bag, flow.Powers, Settings.TimingAssist, SplitMix.Hash((int)(flow.PlayTime * 30), 0xC00C1E5u))
        : null;

    /// <summary>The powers count down by a frame of play; those that wear off are told once the field is free.</summary>
    private void KeepThePowers(float dt)
    {
        var worn = flow.Powers.Tick(dt);
        if (worn.Count > 0) (wornOffToTell ??= new()).AddRange(worn);
    }

    /// <summary>Tells the powers that wore off, one line, while the field is shown with nothing else on it.</summary>
    private void TellWornOff()
    {
        if (wornOffToTell == null || !gameStarted) return;
        if (flow.State != GameState.Overworld || ScriptRunning || flow.Toast.Visible) return;
        var told = wornOffToTell;
        wornOffToTell = null;
        ShowNotification(WornOffLine(told));
    }

    /// <summary>The toast's line for powers that wore off: "Your Encounter Power wore off." (one line for all of them).</summary>
    internal static string WornOffLine(IEnumerable<PowerKind> worn) =>
        Text.Of("timedPowers.wornOff", ("list", string.Join(Text.Of("crafting.leftSeparator"), worn.Distinct().Select(PowerTable.NameOf))));

    /// <summary>Forgets what was waiting to be told (a game begun or loaded).</summary>
    private void ForgetWornOff() => wornOffToTell = null;
}
