using System;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// Soaring (plan 38 · EX7, part a; the rulings' "Soaring"; style guide, "Soaring"): SOAR on Fly's choice, over the region
/// of the tab shown once its Hall of Fame is set, carried by the flier the choice was opened with (whose Fly it is, or the
/// helper the field lends). The flier's model is asked for before the fade; the flight is a scene of its own; landing
/// goes through Fly's own way (<c>common.UseFly</c>, with Fly's target the place landed at), so a landing in another
/// region crosses as Fly does.
/// </summary>
public partial class GameEngine
{
    private SkyRenderer? skyRenderer;

    private SkyRenderer Sky => skyRenderer ??= new SkyRenderer(renderContext);

    /// <summary>Whether a tab of Fly's choice may be soared over: its region's Hall of Fame is set and someone carries the player.</summary>
    private bool SoarableTab(int tab)
    {
        var tabs = flow.TownMapScreen.Tabs;
        if (tab < 0 || tab >= tabs.Count) return false;
        var region = RegionDatabase.Get(tabs[tab].Region.Region);
        return region != null && Soaring.WhyNot(flow.Story.Has(region.StoryCompleteFlag), tabs[tab].Region, flow.TownMapScreen.Flier != null) == null;
    }

    /// <summary>SOAR asked for on Fly's choice: the flight begins behind a fade, or the team comes back and the toast says why not.</summary>
    private void BeginSoaring()
    {
        var screen = flow.TownMapScreen;
        var state = screen.State;
        var region = state == null ? null : RegionDatabase.Get(state.Region.Region);
        string? why = state == null || region == null ? "soar.refused.noMap"
            : Soaring.WhyNot(flow.Story.Has(region.StoryCompleteFlag), state.Region, screen.Flier != null);
        if (why != null || state == null || screen.Flier is not { } flier)
        {
            ShowNotification(Text.Of(why ?? "soar.refused.noFlier"));
            flow.State = GameState.PartyMenu;
            flow.PartyScreen.IsActive = true;
            return;
        }
        // Over the region the player is in, from the player's own cell; over another, from its map's start
        bool home = screen.Tab == screen.HomeTab;
        // The places of the day stand over the region the player is in alone: landing on one is no crossing (part b)
        var flight = new Soaring(state, home ? state.Here : null) { Places = home ? SkyPlaces.Lit(state.Region, GameClock.Today) : Array.Empty<SkyPlace>() };
        skyFlier = flier;
        // The flier's and the rider's models asked for before the fade, never waited for by a frame
        if (!IsHeadless)
        {
            PokemonModels.Request(flier.LookName);
            CharacterModels.Preload(new[] { InTheKitHere(PlayerIdentity.Character) });
        }
        StartTransition(GameState.Soaring, () => flow.SoarScreen.Open(flight, flier));
    }

    private void UpdateSoaring(float dt)
    {
        var screen = flow.SoarScreen;
        screen.Update(dt);
        if (screen.IsActive) return;
        var landed = screen.TakeLanding();
        var flier = screen.Flier;
        if (screen.TakePlace() is { } place)
        {
            LandOnASkyPlace(place);
            return;
        }
        if (landed == null)
        {
            // Nothing open to land at (never so: the flight began from a place Fly could reach)
            screen.Close();
            flow.State = GameState.Overworld;
            return;
        }
        LandFromTheSky(landed, flier);
    }

    /// <summary>The flight ends at a place: Fly's own way there, its target the place, as Fly's choice would.</summary>
    private void LandFromTheSky(TownMapSpot landed, Pokemon? flier)
    {
        flow.SoarScreen.Close();
        skyRenderer?.Unload();
        flow.FlyTarget = landed.Town;
        flow.State = GameState.Overworld;
        StartScript("common.UseFly", pokemon: flier);
    }

    // The Pokémon that carried the player to a place of the day, which lifts them back into the sky (part b)
    private Pokemon? skyFlier;

    /// <summary>
    /// A place of the day landed on (plan 38 · EX7, part b): laid from the day's seed on its region's own map, the player
    /// put down on its beacon's tile behind a fade.
    /// </summary>
    private void LandOnASkyPlace(SkyPlace place)
    {
        flow.SoarScreen.Close();
        skyRenderer?.Unload();
        var date = GameClock.Today;
        var map = SkyPlaces.ToFile(place, date, SoarScreen.NameOf(place)).ToMap();
        flow.SkyPlaceMap = map;
        var (bx, by) = SkyPlaces.BeaconOf(SkyPlaces.Lay(place, date));
        flow.SkyPlaceShown = place;
        flow.State = GameState.Overworld;
        WarpTo(map.Name, bx, by, Direction.Down);
    }

    /// <summary>
    /// A step that comes onto a place of the day's beacon (part b): the player is lifted back into the sky over the
    /// region, from the place's cell, by the Pokémon that brought them (or whoever knows Fly, or the field's helper).
    /// </summary>
    private bool SkyBeaconStep()
    {
        if (flow.SkyPlaceShown is not { } place || flow.SkyPlaceMap is not { } map || flow.Map != map) return false;
        var floor = SkyPlaces.Lay(place, GameClock.Today);
        if ((flow.Player.GridX, flow.Player.GridY) != SkyPlaces.BeaconOf(floor)) return false;
        var flier = skyFlier is { } f && flow.Party.Members.Contains(f) ? f
            : flow.Party.Members.FirstOrDefault(p => !p.IsEgg && p.Moves.Any(m => m.Data.Name == nameof(FieldMove.Fly)))
            ?? FieldHelp.HelperFor(FieldMove.Fly);
        if (RegionMap.Of(place.Region) is not { } regionMap) return false;
        var state = new TownMapState(regionMap, flow.Story, reachable: Reachable);
        var flight = new Soaring(state, (place.X, place.Y)) { Places = SkyPlaces.Lit(regionMap, GameClock.Today) };
        skyFlier = flier;
        if (!IsHeadless)
        {
            PokemonModels.Request(flier.LookName);
            CharacterModels.Preload(new[] { InTheKitHere(PlayerIdentity.Character) });
        }
        StartTransition(GameState.Soaring, () => flow.SoarScreen.Open(flight, flier));
        return true;
    }

    private void RenderSoaring()
    {
        if (flow.SoarScreen.Flight == null) return;
        var screen = flow.SoarScreen;
        Sky.Render(screen.Flight, screen.Flier, screen.Age, screen.Bank, GameClock.Hour, InTheKitHere(PlayerIdentity.Character));
    }

    private void DrawSoaring()
    {
        if (flow.SoarScreen.Flight == null) return;
        Sky.DrawToScreen();
        flow.SoarScreen.Draw(VirtualWidth, VirtualHeight);
    }

    internal sealed partial class Driver
    {
        /// <summary>The soaring screen (plan 38 · EX7).</summary>
        public SoarScreen SoarScreen => game.flow.SoarScreen;

        /// <summary>Whether a tab of Fly's choice may be soared over, as the choice opened from the party menu says.</summary>
        public bool SoarableTab(int tab) => game.SoarableTab(tab);

        /// <summary>Fly's choice as the party menu opens it for a flier, SOAR told where it may be had.</summary>
        public void OpenFlyChoice(Pokemon flier)
        {
            var (tabs, home) = game.FlyChoiceNow();
            game.flow.TownMapScreen.Open(tabs, home, TownMapMode.Fly, flier);
            game.flow.TownMapScreen.SoarableTab = game.SoarableTab;
            game.flow.State = GameState.FlyMap;
        }

        /// <summary>The place of the day last landed on, and its map (part b).</summary>
        public SkyPlace? SkyPlaceShown => game.flow.SkyPlaceShown;
        public Map? SkyPlaceMap => game.flow.SkyPlaceMap;

        /// <summary>Lands on a place of the day as the soaring screen's A over its beacon would (a tool's hand).</summary>
        public void LandOnASkyPlace(SkyPlace place) => game.LandOnASkyPlace(place);

        /// <summary>Whether the flier was in a cloud at the last frame rendered.</summary>
        public bool SoaringInCloud => game.skyRenderer?.InCloud ?? false;
    }
}
