using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// Records mixed between one's own saves (plan 40 · GE10; <see cref="RecordMix"/>): the record desk's command reads the
/// other slots (never writing them), and what was mixed stands in the friends' hall (<see cref="StandTheFriends"/>), in
/// Hoenn as friends' secret bases at their spots, and on the television.
/// </summary>
public partial class GameEngine
{
    /// <summary>
    /// The player's other saves, in slot order: each slot but the game's own that reads whole or from its backup, and is
    /// another game than the one in play. Read with <see cref="SaveSlots.Read(SavePaths)"/>, which writes nothing.
    /// </summary>
    internal IReadOnlyList<SaveData> OtherSaves()
    {
        var own = MixedRecord.KeyOf(flow.TrainerId, flow.AdventureStarted);
        return SaveSlots.Read(SavePaths.Current)
            .Where(s => s.Number != flow.SaveSlot && s.Outcome is SaveLoad.Ok or SaveLoad.Recovered && s.Save != null)
            .Select(s => s.Save!)
            .Where(s => MixedRecord.KeyOf(s.TrainerId, s.Started) != own)
            .ToList();
    }

    /// <summary>The friend's base at the spot ahead, where the player's own base isn't; null with none.</summary>
    private MixedRecord? FriendsBaseAhead() =>
        SpotAhead() is { } spot && !IsOwnBaseAt(flow.Map, spot.X, spot.Y) ? RecordMix.BaseAt(flow.Records, flow.Map.Name, spot.X, spot.Y) : null;

    /// <summary>Into a friend's base at the spot ahead, inside its door, facing into the room.</summary>
    private bool VisitFriendsBase()
    {
        if (FriendsBaseAhead() is not { Base.Layout: { } layout } friend) return false;
        flow.Records.Visiting = friend;
        var map = MapDatabase.Get(layout.Map);
        var door = map.Warps.FirstOrDefault();
        var (x, y) = door == null ? (layout.PcFrontX, layout.PcFrontY) : (door.SourceX, door.SourceY - 1);
        AudioManager.PlaySound("stairs");
        PlayAreaMusic(map, x, y);
        StartTransition(GameState.Overworld, () =>
        {
            flow.Map = map;
            flow.Player.SetPosition(x, y, Direction.Up);
            ArriveOnMap();
            AnnounceLocation();
            world?.Life.Clear();
        });
        return true;
    }

    /// <summary>
    /// A friend's base laid out in its room (Emerald's friends' bases): what stood in it, by kind and tile, and its owner
    /// standing on the open floor nearest the PC, a friend's trainer of the hall's kind (<see cref="FriendsHall"/>). The
    /// door leads back to the friend's spot. Done in place of the player's own room's laying out.
    /// </summary>
    private void LayOutFriendsBase(Map room, MixedRecord friend)
    {
        var b = friend.Base!;
        var layout = b.Layout!;
        foreach (var standing in b.Standing)
        {
            if (DecorationDatabase.Get(standing.Id) is not { } decoration) continue;
            var (w, d) = decoration.Footprint;
            if (!room.InBounds(standing.X, standing.Y) || !room.InBounds(standing.X + w - 1, standing.Y + d - 1)) continue;
            var prop = new Prop { Type = PropType.Decoration, X = standing.X, Y = standing.Y, Width = w, Depth = d, Model = Graphics.DecorationArt.ModelPrefix + decoration.Id };
            room.Props.Add(prop);
            baseProps.Add(prop);
            if (!decoration.Blocks || decoration.Placement == DecorationPlacement.Wall) continue;
            int top = decoration.Placement == DecorationPlacement.Tall && d > 1 ? 1 : 0;
            for (int ty = standing.Y + top; ty < standing.Y + d; ty++)
                for (int tx = standing.X; tx < standing.X + w; tx++)
                    if (room.InBounds(tx, ty) && !room.IsSolid(tx, ty))
                    {
                        room.SetSolid(tx, ty, true);
                        baseSolid.Add((tx, ty));
                    }
        }
        if (room.Warps.FirstOrDefault() is { } door)
        {
            door.TargetMap = b.Map;
            door.TargetX = b.OutX;
            door.TargetY = b.OutY;
            door.TargetFacing = b.OutY > b.Y ? Direction.Down : b.OutY < b.Y ? Direction.Up : b.OutX < b.X ? Direction.Left : Direction.Right;
        }
        if (RecordMix.TrainerOf(friend) is { } file)
        {
            var floor = BaseFloor.Of(room, layout, baseSolid);
            if (RecordMix.OwnerTile(floor, layout, (x, y) => room.IsSolid(x, y) || room.GetNpcAt(x, y) != null) is { } tile)
            {
                var owner = FriendsHall.PersonOf(file, tile);
                room.Add(owner);
                baseOwner = owner;
                flow.FriendsStanding[owner] = file;
            }
        }
    }

    // The friend standing in the base laid out last, taken off with its decorations
    private NPC? baseOwner;

    private void TakeTheOwnerAway()
    {
        if (baseOwner == null) return;
        baseRoom?.NPCs.Remove(baseOwner);
        flow.FriendsStanding.Remove(baseOwner);
        baseOwner = null;
    }

    private sealed partial class FieldHost
    {
        public RecordMixState? MixedRecords => game.flow.Records;
        public IReadOnlyList<SaveData> OtherSaves() => game.OtherSaves();
        public (int TrainerId, System.DateTime? Started) OwnGame => (game.flow.TrainerId, game.flow.AdventureStarted);
    }
}
