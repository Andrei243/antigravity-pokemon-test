using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The Week's Work in the game (plan 41 · EG6; <see cref="MissionBoard"/>): the engine tells the board what it counts
/// already (a Pokémon caught, a trainer beaten, an Egg hatched, a place come to, a facility's battle won, a contest won),
/// each where the player stands; a timed mission's clock runs in the field and its battles while the player is in its
/// region, its time left at the field's top right; the board's screen is read from the sign by the avenue or Pennant
/// Hall's back wall. Nothing is told before the board has been read once.
/// </summary>
public partial class GameEngine
{
    /// <summary>Tells the board of something that happened where the player stands; a mission it finishes is told on the field.</summary>
    private void TellTheBoard(MissionEventKind kind, string area = "", Pokemon? caught = null)
    {
        if (!flow.Missions.Opened) return;
        var e = new MissionEvent(kind, Here.Id, area, caught?.PrimaryType, caught?.SecondaryType);
        foreach (var done in flow.Missions.Tell(e))
            ShowNotification(Text.Of("hubMissions.notice.doneInField", ("mission", MissionWeek.Describe(done))));
    }

    /// <summary>The area the player stands in, by key, or the map's name where no area is (a room).</summary>
    private string AreaKeyHere() => flow.Map.AreaAt(flow.Player.GridX, flow.Player.GridY)?.Key is { Length: > 0 } key ? key : flow.Map.Name;

    /// <summary>What a battle of the field that has ended counts for the board: a catch, and a trainer beaten (a rematch's team besides).</summary>
    private void TellTheBoardOfABattle(BattleEngine fought)
    {
        if (fought.Kind == BattleKind.CatchingLesson || fought.IsFrontier) return;
        if (fought.Caught is { } caught) TellTheBoard(MissionEventKind.Caught, AreaKeyHere(), caught);
        if (fought.Result == BattleResult.PlayerVictory && fought.IsTrainerBattle)
            TellTheBoard(fought.Trainers.Any(t => TrainerDatabase.IsRematchTeam(t.Id)) ? MissionEventKind.BeatRematch : MissionEventKind.BeatTrainer);
    }

    /// <summary>A timed mission's clock: in the field and in its battles, in its region; the moment it runs out is told.</summary>
    private void TickMissions(float dt)
    {
        if (flow.Missions.Running == null || flow.State is not (GameState.Overworld or GameState.Dialogue or GameState.Battle)) return;
        if (flow.Missions.Tick(dt, Here.Id)) ShowNotification(Text.Of("hubMissions.notice.timeUp"));
    }

    /// <summary>A timed mission's corner over the field while the player is in its region.</summary>
    private void DrawMissionCorner()
    {
        if (flow.Missions.Running is not { } m || !string.Equals(m.Region, Here.Id, System.StringComparison.OrdinalIgnoreCase)) return;
        ModernUi.MissionCorner(VirtualWidth, m, flow.Missions.SecondsLeft);
    }

    /// <summary>The board's screen, opened by a script's <c>missions open</c>; the script hears the pennants it paid out.</summary>
    private void OpenMissionBoard()
    {
        flow.MissionBoardScreen.Open(flow.Missions, flow.Plaza, flow.Bag);
        flow.State = GameState.MissionBoard;
    }

    private void UpdateMissionBoard(float dt)
    {
        flow.MissionBoardScreen.Update(dt);
        if (flow.MissionBoardScreen.IsActive) return;
        flow.ScriptAnswer = flow.MissionBoardScreen.Paid;
        flow.State = GameState.Overworld;
    }
}
