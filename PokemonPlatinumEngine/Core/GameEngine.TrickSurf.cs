using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models.Rides;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// Trick surfing in the game (plan 42 · PS7): a beach hut's <c>ride surf "&lt;course&gt;"</c> opens the course on
/// <see cref="UI.RideScreen"/>; as it closes the run's Battle Points go to the Frontier's records and the script hears the
/// player's place. (The Ride Pager's rides on land are <c>GameEngine.Rides.cs</c>.)
/// </summary>
public partial class GameEngine
{
    /// <summary>The first Pokémon of the team that would carry the player in a riding race (plan 42 · PS8): able and of a riding size.</summary>
    internal Models.Pokemon? RaceMountNow() => flow.Party.Members.FirstOrDefault(Models.Rides.RidingRace.CanCarry);

    /// <summary>Opens a course of trick surfing, or a riding race told as <c>race:&lt;track&gt;</c>: nothing where the table has none.</summary>
    internal void OpenRide(string courseId)
    {
        if (courseId.StartsWith(RaceTracks.Prefix, System.StringComparison.Ordinal))
        {
            var track = RaceTracks.Get(courseId.Substring(RaceTracks.Prefix.Length));
            var mount = RaceMountNow();
            if (track == null || mount == null) return;
            if (!IsHeadless) PokemonSprites.Request(mount.ModelName);
            flow.RideScreen.OpenRace(track, new Models.Athlete(mount.Species.Name, Models.AthleteStats.Of(mount)), flow.Rides, Dice.New(), flow.Frontier.BattlePoints);
            flow.State = GameState.Ride;
            return;
        }
        var course = SurfCourses.Get(courseId);
        if (course == null) return;
        if (!IsHeadless) PokemonSprites.Request(SurfCourses.Mount);
        flow.RideScreen.Open(course, flow.Rides, Settings.TimingAssist, Dice.New(), flow.Frontier.BattlePoints);
        flow.State = GameState.Ride;
    }

    private void UpdateRide(float dt)
    {
        var screen = flow.RideScreen;
        screen.Update(dt);
        if (screen.IsActive) return;
        // The points were worked out as the run was recorded (RideRecords.Record): they are the Frontier's now
        if (screen.Outcome > 0 && screen.Result.Paid > 0) flow.Frontier.GivePoints(screen.Result.Paid);
        flow.ScriptAnswer = screen.Outcome;
        flow.State = GameState.Overworld;
    }
}
