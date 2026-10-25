using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// Hoenn's secret bases in the game (plan 21 · H10; Emerald's <c>secret_base.c</c>): a spot faced runs
/// <c>common.SecretBaseSpot</c>, Secret Power makes it the player's base and goes in, the base's room is laid out with
/// what stands in it and its door aimed back at its spot, and its PC opens the shared decorating
/// (<see cref="DecorationScreen"/>, the seam of plan 06 · R16a's Underground). The rules are <see cref="SecretBases"/>'.
/// </summary>
public partial class GameEngine
{
    /// <summary>The flag Emerald sets once the Secret Power man of Route 111 has given his TM.</summary>
    public const string ReceivedSecretPowerFlag = "FLAG_RECEIVED_SECRET_POWER";

    // What the base laid out last put in its room: the decorations' props and the tiles they made solid, which the next
    // laying out takes back first
    private readonly List<Prop> baseProps = new();
    private readonly HashSet<(int X, int Y)> baseSolid = new();
    private Map? baseRoom;

    /// <summary>Whether the decorating screen shows a secret base (and not the Underground's base).</summary>
    private bool decoratingBase;

    private void LoadSecretBase(SecretBaseSave? save, List<StoredDecoration>? store)
    {
        flow.SecretBase = save;
        flow.Decorations = store ?? new List<StoredDecoration>();
        // What stands in the base is what the PC calls out, whatever an older file said
        for (int i = 0; i < flow.Decorations.Count; i++) flow.Decorations[i].Out = save?.Placed.Any(p => p.Slot == i) == true;
        decoratingBase = false;
        // The maps outlast a game: the room the last game laid out is bare again
        if (baseRoom != null) LayOutSecretBase(baseRoom);
    }

    /// <summary>The spot right in front of the player, with its base's id; null where there is none.</summary>
    private (int X, int Y, int Id)? SpotAhead()
    {
        var (dx, dy) = FieldMovement.Delta(flow.Player.Facing);
        int x = flow.Player.GridX + dx, y = flow.Player.GridY + dy;
        return flow.Map.SecretBaseSpots.TryGetValue((x, y), out int id) ? (x, y, id) : null;
    }

    private bool IsOwnBaseAt(Map map, int x, int y) => flow.SecretBase is { } b && b.Map == map.Name && b.X == x && b.Y == y;

    /// <summary>A spot faced in the field, or a base's PC faced inside it, runs its script.</summary>
    private bool TryInteractSecretBase(int x, int y)
    {
        if (flow.Map.SecretBaseSpots.ContainsKey((x, y)))
            return StartScript(FieldScripts.SecretBaseSpot);
        if (SecretBaseLayouts.IsBaseMap(flow.Map.Name) && flow.Map.InBounds(x, y) && flow.Map.BehaviourAt(x, y) == TileBehavior.Computer)
            return StartScript(FieldScripts.SecretBasePc);
        return false;
    }

    /// <summary>What <c>openbase</c> does where the player stands (<see cref="ScriptRunner"/>'s words); the number is RESULT.</summary>
    internal int SecretBaseAct(string what)
    {
        switch (what)
        {
            case "check":
                if (SpotAhead() is not { } spot) return 0;
                if (flow.SecretBase == null) return 1;
                return IsOwnBaseAt(flow.Map, spot.X, spot.Y) ? 2 : 3;
            case "kind":
                return SpotAhead() is { } faced ? (int)(SecretBaseLayouts.Of(faced.Id)?.Kind ?? SecretBaseKind.None) : 0;
            case "open":
            {
                if (SpotAhead() is not { } at || SecretBaseLayouts.Of(at.Id) == null) return 0;
                flow.SecretBase = SecretBases.Open(flow.SecretBase, flow.Decorations, at.Id, flow.Map.Name, at.X, at.Y, flow.Player.GridX, flow.Player.GridY);
                GoIntoSecretBase(first: true);
                return 1;
            }
            case "enter":
                if (flow.SecretBase == null) return 0;
                GoIntoSecretBase(first: false);
                return 1;
            case "pc":
                if (flow.SecretBase == null || SecretBaseLayouts.OfMap(flow.Map.Name) != flow.SecretBase.Layout) return 0;
                OpenSecretBaseDecoration();
                return 1;
            case "packup":
            {
                if (flow.SecretBase is not { } room) return 0;
                SecretBases.PackUp(room, flow.Decorations);
                flow.SecretBase = null;
                if (SecretBaseLayouts.IsBaseMap(flow.Map.Name))
                {
                    LayOutSecretBase(flow.Map);
                    GoOut(room.Map, room.OutX, room.OutY, AwayFrom(room));
                }
                return 1;
            }
            default:
                return 0;
        }
    }

    /// <summary>The way out of a base faces: away from its spot.</summary>
    private static Direction AwayFrom(SecretBaseSave room) =>
        room.OutY > room.Y ? Direction.Down : room.OutY < room.Y ? Direction.Up : room.OutX < room.X ? Direction.Left : Direction.Right;

    /// <summary>
    /// Into the player's base (Emerald's <c>EnterSecretBase</c>, <c>EnterNewlyCreatedSecretBase</c>): the first time
    /// before its PC, afterwards inside its door, both facing into the room.
    /// </summary>
    private void GoIntoSecretBase(bool first)
    {
        var room = flow.SecretBase!;
        var layout = room.Layout!;
        var map = MapDatabase.Get(layout.Map);
        var door = map.Warps.FirstOrDefault();
        var (x, y) = first || door == null ? (layout.PcFrontX, layout.PcFrontY) : (door.SourceX, door.SourceY - 1);
        if (room.TimesEntered < 255) room.TimesEntered++;
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
    }

    /// <summary>Out of a base to the tile before its spot.</summary>
    private void GoOut(string mapName, int x, int y, Direction facing)
    {
        var map = MapDatabase.Get(mapName);
        AudioManager.PlaySound("stairs");
        PlayAreaMusic(map, x, y);
        StartTransition(GameState.Overworld, () =>
        {
            flow.Map = map;
            flow.Player.SetPosition(x, y, facing);
            ArriveOnMap();
            AnnounceLocation();
            world?.Life.Clear();
        });
    }

    /// <summary>
    /// A base's room as the player comes in (Emerald's <c>InitSecretBaseAppearance</c>): what stands in it laid as props,
    /// in the way where it blocks, and its door aimed at the tile before its spot. A room that is no base of the player's
    /// is bare.
    /// </summary>
    internal void LayOutSecretBase(Map room)
    {
        if (baseRoom != null)
        {
            foreach (var prop in baseProps) baseRoom.Props.Remove(prop);
            foreach (var (x, y) in baseSolid) baseRoom.SetSolid(x, y, false);
            if (baseRoom != room) world?.Forget(baseRoom, 0, 0, baseRoom.Width, baseRoom.Height);
        }
        baseProps.Clear();
        baseSolid.Clear();
        baseRoom = room;
        var layout = SecretBaseLayouts.OfMap(room.Name);
        if (flow.SecretBase is { } own && layout != null && own.Layout == layout)
        {
            foreach (var (decoration, x, y) in SecretBases.Standing(own, flow.Decorations))
            {
                var (w, d) = decoration.Footprint;
                var prop = new Prop { Type = PropType.Decoration, X = x, Y = y, Width = w, Depth = d, Model = DecorationArt.ModelPrefix + decoration.Id };
                room.Props.Add(prop);
                baseProps.Add(prop);
                if (!decoration.Blocks || decoration.Placement == DecorationPlacement.Wall) continue;
                // A tall one's top row stands before the wall, and is walked behind as Emerald's are
                int top = decoration.Placement == DecorationPlacement.Tall && d > 1 ? 1 : 0;
                for (int ty = y + top; ty < y + d; ty++)
                    for (int tx = x; tx < x + w; tx++)
                        if (room.InBounds(tx, ty) && !room.IsSolid(tx, ty))
                        {
                            room.SetSolid(tx, ty, true);
                            baseSolid.Add((tx, ty));
                        }
            }
            if (room.Warps.FirstOrDefault() is { } door)
            {
                door.TargetMap = own.Map;
                door.TargetX = own.OutX;
                door.TargetY = own.OutY;
                door.TargetFacing = AwayFrom(own);
            }
        }
        world?.Forget(room, 0, 0, room.Width, room.Height);
    }

    /// <summary>The base's PC (Emerald's <c>ShowSecretBaseDecorationMenu</c>): the shared decorating over its floor.</summary>
    private void OpenSecretBaseDecoration()
    {
        var own = flow.SecretBase!;
        var floor = BaseFloor.Of(flow.Map, own.Layout!, baseSolid);
        decoratingBase = true;
        flow.State = GameState.Decoration;
        var session = new DecoratingSession(SecretBases.Room(own, flow.Decorations, floor), flow.Decorations, SecretBases.KindOf);
        flow.DecorationScreen.Open(session, Text.Of("secretBase.decorating"), piece => DecorationArt.PlanLook(piece.Kind));
    }

    /// <summary>The decorating done: what stands in the room is the base's, laid out at once, and the field is the player's again.</summary>
    private void FinishSecretBaseDecoration()
    {
        decoratingBase = false;
        if (flow.SecretBase is { } own)
        {
            SecretBases.Keep(own, flow.DecorationScreen.Session.Room, flow.Decorations);
            LayOutSecretBase(flow.Map);
        }
        flow.State = GameState.Overworld;
    }
}
