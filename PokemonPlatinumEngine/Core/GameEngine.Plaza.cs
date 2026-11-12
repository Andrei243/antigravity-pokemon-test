using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// Pennant Hall in the game (plan 41 · EG3; <see cref="Plaza"/>): as the hall is come to, and whenever a stall is set out
/// or the day's guests are drawn, it is furnished afresh as the plaza stands (the Villa's way): a booth under its stall's
/// awning at each pitch with a stall, its keeper dressed as the stall's, the counter before them spoken across, and the
/// day's guests at their spots in their classes' looks. The pennants held stand over the field while a script speaks of
/// them, as the Battle Points do. A stall's goods counter is the shop's screen paid in pennants.
/// </summary>
public partial class GameEngine
{
    // The booths laid in the hall this time, and the counters' tiles as the room had them before
    private readonly List<Prop> plazaProps = new();
    private readonly Dictionary<(int X, int Y), TileBehavior> plazaCounters = new();
    private bool showPennants;

    /// <summary>Lays the hall out afresh on the map come to, when it is Pennant Hall; leaves every other map alone.</summary>
    private void FurnishPlaza()
    {
        var room = flow.Map;
        if (room.Name != Plaza.Map) return;
        // The day's guests, drawn once a day on the field's own chance
        flow.Plaza.GuestsFor(GameClock.Today, flow.Story, fieldRandom);
        Plaza.KeepFlags(flow.Story, flow.Plaza);
        RefreshPresence();
        foreach (var prop in plazaProps) room.Props.Remove(prop);
        plazaProps.Clear();
        var places = Plaza.File.Places;
        for (int p = 1; p <= places.Count; p++)
        {
            var place = places[p - 1];
            var stall = Plaza.Stall(flow.Plaza.At(p));
            var counter = (place.X + 1, place.Y + 2);
            if (!plazaCounters.ContainsKey(counter)) plazaCounters[counter] = room.BehaviourAt(counter.Item1, counter.Item2);
            for (int y = place.Y; y < place.Y + 3; y++)
                for (int x = place.X; x < place.X + 3; x++)
                    if (x != place.X + 1 || y != place.Y + 1) room.SetSolid(x, y, stall != null);
            room.SetBehaviour(counter.Item1, counter.Item2, stall != null ? TileBehavior.Counter : plazaCounters[counter]);
            if (room.Everyone.FirstOrDefault(n => n.Id == Plaza.KeeperPrefix + p) is { } keeper && stall != null)
            {
                keeper.NpcType = stall.Keeper;
                keeper.Name = Text.Of($"hubPlaza.stall.{stall.Id}.keeper");
            }
            if (stall == null) continue;
            var booth = new Prop { Type = PropType.Booth, X = place.X, Y = place.Y, Width = 3, Depth = 3, Tint = stall.Awning };
            room.Props.Add(booth);
            plazaProps.Add(booth);
        }
        for (int g = 1; g <= Plaza.File.Spots.Count; g++)
        {
            if (room.Everyone.FirstOrDefault(n => n.Id == Plaza.GuestPrefix + g) is not { } guest || flow.Plaza.GuestAt(g) is not { } record) continue;
            guest.NpcType = Plaza.LookOf(record, g);
            guest.Name = Text.Of("battle.fullTitle", ("class", Trainer.ClassName(record.Class)), ("name", record.Name));
        }
        world?.Forget(room, 0, 0, room.Width, room.Height);
        KeepCharactersNear(trim: false);
    }

    /// <summary>The pennants stand over the field while a script speaks of them, and go as it ends.</summary>
    private void DrawPennants()
    {
        if (!ScriptRunning) showPennants = false;
        if (showPennants) ModernUi.PennantsCount(flow.Plaza.Coins);
    }

    /// <summary>A stall's goods counter: the shop's screen with the stall's goods at the hall's rank, paid in pennants.</summary>
    private void OpenPlazaShop(string stallId)
    {
        if (Plaza.Stall(stallId) is not { } stall) return;
        var goods = Plaza.StockAt(stall, flow.Plaza.Rank)
            .Select(g => (Item: ItemDatabase.Get(g.Item), g.Price))
            .Where(g => g.Item != null)
            .Select(g => (g.Item!, g.Price));
        flow.ShopScreen.OpenForPoints(Text.Of($"hubPlaza.stall.{stall.Id}.name"), goods, ShopScreen.PennantsUnit);
        flow.State = GameState.Shop;
    }

    private sealed partial class FieldHost
    {
        public PlazaState? Plaza => game.flow.Plaza;
        public MissionBoard? Missions => game.flow.Missions;
        public void PlazaChanged() => game.FurnishPlaza();
        public void ShowPennants(bool shown) => game.showPennants = shown;
    }
}
