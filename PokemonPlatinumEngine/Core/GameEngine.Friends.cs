using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;
using Raylib_cs;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The friends' page of the battle room (plan 13 · V22; rulings, "Teams and trainers as text"): trainer files and codes
/// read, teams pasted and sent, and a friend's trainer fought on copies at level 50. The clipboard is raylib's
/// (<c>GetClipboardText</c>, <c>SetClipboardText</c>), reached through <see cref="IFriendsHost"/> so a test has its own.
/// </summary>
public partial class GameEngine
{
    /// <summary>The trainer chosen on the friends' page, waiting for the team that takes them on.</summary>
    private TrainerFile? friendToFight;

    /// <summary>The clipboard the page uses; the game's own unless a tool gave it another.</summary>
    internal IFriendsHost FriendsClipboard { get; set; } = new RaylibClipboard();

    private sealed class RaylibClipboard : IFriendsHost
    {
        public string? ReadClipboard() => Raylib.GetClipboardText_();
        public void WriteClipboard(string text) => Raylib.SetClipboardText(text);
    }

    private void OpenTheFriendsPage()
    {
        flow.FriendsScreen.Open(BuiltTeams.Read(TeamsFile), SavePaths.Current.Trainers, FriendsClipboard);
        flow.State = GameState.Friends;
    }

    private void UpdateFriends(float dt)
    {
        var page = flow.FriendsScreen;
        page.Update(dt);
        if (page.IsActive) return;
        if (page.Changed) BuiltTeams.Write(TeamsFile, page.Teams);
        var teams = page.Teams.Teams;
        flow.State = GameState.BattleRoom;
        if (page.TakeChosen() is { } chosen)
        {
            friendToFight = chosen;
            flow.BattleRoomScreen.PickForFriend(teams);
        }
        else flow.BattleRoomScreen.Resume(teams);
    }

    internal sealed partial class Driver
    {
        public FriendsScreen Friends => game.flow.FriendsScreen;

        /// <summary>The friends' page opened as the room's FRIENDS opens it, on a clipboard of the tool's own.</summary>
        public void OpenFriends(IFriendsHost clipboard)
        {
            game.FriendsClipboard = clipboard;
            game.OpenTheFriendsPage();
        }
    }
}
