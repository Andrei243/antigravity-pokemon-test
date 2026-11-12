using System;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// Rifts in the field as the game plays them (plan 38 · EX5; rules in <see cref="Rifts"/>): the hour's rift of the region the
/// player is in, its table in the moment's grass and rods within its reach, its three finds laid in its ground as hidden items
/// under daily flags, the tear stood on its centre, and a toast as it opens and closes. Nothing is drawn from the dice: the
/// hour's rift is worked out from the day's number and the hour, so a save made in one loads into it within its hour.
/// </summary>
public partial class GameEngine
{
    // The hour's rift as last worked out, and the key it was worked out for (region, day, hour)
    private (string Region, uint Daily, int Hour)? riftKey;
    private Rift? rift;

    // The rift whose finds are laid in its map's ground, and the rift told of by a toast
    private Rift? riftLaid;
    private Map? riftLaidOn;
    private (string Region, uint Daily, int Hour)? riftTold;

    /// <summary>The hour's rift in the region the player is in; null where the hour has none, or before the Hall of Fame.</summary>
    internal Rift? RiftNow()
    {
        if (!Rifts.Open(Here, flow.Story) || Here.OverworldMap is not { } overworld || MapDatabase.TryGet(overworld) is not { } map) return null;
        var key = (Here.Id, flow.Encounters.DailyNumber, Math.Clamp((int)GameClock.Hour, 0, 23));
        if (riftKey != key)
        {
            riftKey = key;
            rift = Rifts.Of(Here.Id, key.DailyNumber, key.Item3, map, OutbreaksNow());
        }
        return rift;
    }

    /// <summary>The rift's table where the player stands within its reach on its map, for the moment's grass and rods; null elsewhere.</summary>
    private RiftTable? RiftHere() =>
        RiftNow() is { } r && flow.Map.Name == Here.OverworldMap && r.Holds(flow.Player.GridX, flow.Player.GridY) ? Rifts.TableOf(r.Region) : null;

    /// <summary>
    /// A frame of the field: the hour's rift laid in its map's ground (its finds, the tear on its centre, lit again where a change
    /// of place cleared it) and taken away when its hour is over, with a toast as one opens and as it closes, outdoors.
    /// </summary>
    private void KeepTheRift()
    {
        var now = RiftNow();
        if (!Equals(riftLaid, now) || (now != null && riftLaidOn != flow.Map && flow.Map.Name == Here.OverworldMap))
        {
            // The finds of the rift before are taken out of its ground
            if (riftLaid != null && riftLaidOn != null)
            {
                foreach (var (x, y, _) in riftLaid.Finds)
                    if (riftLaidOn.HiddenItems.TryGetValue((x, y), out var hidden) && hidden.Flag.StartsWith("FLAG_DAILY_RIFT_FIND_", StringComparison.Ordinal))
                        riftLaidOn.HiddenItems.Remove((x, y));
                world.Life.StopEffect(riftLaid.X, riftLaid.Y);
                if (now == null && !flow.Map.IsIndoors) ShowNotification(Text.Of("riftToast.closed", ("place", FieldReport.PlaceName(riftLaid.Region, riftLaid.Area))));
            }
            riftLaid = null;
            riftLaidOn = null;
            if (now != null && flow.Map.Name == Here.OverworldMap)
            {
                for (int n = 0; n < now.Finds.Count; n++)
                {
                    var (x, y, item) = now.Finds[n];
                    if (!flow.Map.HiddenItems.ContainsKey((x, y))) flow.Map.HiddenItems[(x, y)] = new HiddenItem(item, 1, Rifts.FindFlag(n));
                }
                riftLaid = now;
                riftLaidOn = flow.Map;
            }
        }
        if (now == null) return;
        // The toast, once a rift, and only outdoors in its region
        if (riftTold != riftKey && !flow.Map.IsIndoors && flow.State == GameState.Overworld)
        {
            riftTold = riftKey;
            ShowNotification(Text.Of("riftToast.opened", ("place", FieldReport.PlaceName(now.Region, now.Area))));
        }
        // The tear on its centre, lit again where a change of place has cleared it
        if (riftLaidOn == flow.Map && !world.Life.HasEffect(SceneEffect.Rift, now.X, now.Y))
            world.Life.Effect(flow.Map, SceneEffect.Rift, now.X, now.Y, SceneEffects.Length(SceneEffect.Rift), Direction.Down, hold: true);
    }
}
