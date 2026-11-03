using System.Linq;

using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The friends' hall (plan 13 · V23; <see cref="FriendsHall"/>): as Jubilife City's Pokémon Center upstairs is come to,
/// the trainers of the files in <c>trainers/</c> beside the save are stood on its spots, those of the last visit taken off
/// first, so the room always holds what the folder holds now. Each is a person of the room with the hall's script; who
/// they are is kept beside them (<c>GameFlow.FriendsStanding</c>), never on the map's files, so nothing of a friend is
/// written into a save but the checksum of a trainer beaten today.
/// </summary>
public partial class GameEngine
{
    /// <summary>Where the friends' files are read from: the folder beside the saves unless a tool or a test names another.</summary>
    internal string FriendsFolder
    {
        get => friendsFolder ?? SavePaths.Current.Trainers;
        set => friendsFolder = value;
    }

    private string? friendsFolder;

    /// <summary>Lays the hall out afresh on the map come to, when it is the hall; leaves every other map alone.</summary>
    private void StandTheFriends()
    {
        if (flow.Map.Name != FriendsHall.Room) return;
        foreach (var gone in flow.Map.NPCs.Where(FriendsHall.IsFriend).ToList()) flow.Map.NPCs.Remove(gone);
        flow.FriendsStanding.Clear();
        foreach (var (file, spot) in FriendsHall.Stand(TrainerFiles.In(FriendsFolder)))
        {
            // A spot someone of the room's own stands on is left empty rather than shared
            if (flow.Map.GetNpcAt(spot.X, spot.Y) != null) continue;
            var person = FriendsHall.PersonOf(file, spot);
            flow.Map.Add(person);
            flow.FriendsStanding[person] = file;
        }
        // (Their rigs are made ready behind the fade with everyone else's of the room: KeepCharactersNear)
    }

    private sealed partial class FieldHost
    {
        public TrainerFile? FriendOf(NPC? person) => person != null && game.flow.FriendsStanding.TryGetValue(person, out var file) ? file : null;
        public FriendsHallState? FriendsHall => game.flow.FriendsHall;
        public void FriendWon() => game.flow.Statistics.Count(Statistics.TrainerWon);
    }
}
