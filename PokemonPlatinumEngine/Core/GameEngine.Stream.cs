using System;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The streamer kit (plan 13 · V24, <see cref="LiveFeed"/>): with the options' STREAM FEED row on, the run is written to
/// <c>stream/state.json</c> as it changes, the overlay is copied beside it once, and with the third value a LiveSplit
/// splits file is kept in step with the run timer's splits. With the row off nothing is read, built or written.
/// </summary>
public partial class GameEngine
{
    private readonly LiveFeed liveFeed = new();

    /// <summary>The game's own seconds the feed is timed by: the sum of the frames the game was played in.</summary>
    private double streamClock;

    private StreamFeedMode streamWas;
    private int splitsFileHas = -1;

    /// <summary>A frame of the kit: the feed offered, the splits file kept up. Nothing at all with the row off.</summary>
    private void KeepTheStream(float dt)
    {
        var mode = Settings.StreamFeed;
        if (mode != streamWas)
        {
            // Switched on (or off) while playing: the next offer writes, and the splits file is made afresh
            streamWas = mode;
            liveFeed.Reset();
            splitsFileHas = -1;
        }
        if (mode == StreamFeedMode.Off || !gameStarted) return;
        streamClock += dt;

        string folder = SavePaths.Current.Stream;
        InstallTheOverlay(folder);
        bool timerShown = Settings.RunTimer != TimerCategory.Off && flow.RunState.Timer != null && !TimerStandsStill(flow.State);
        liveFeed.Offer(StreamState(), streamClock, Path.Combine(folder, SavePaths.FeedFileName), timerShown);

        if (mode == StreamFeedMode.FileAndSplits && flow.RunState.Timer is { } timer && timer.Splits.Count != splitsFileHas)
        {
            splitsFileHas = timer.Splits.Count;
            LiveFeed.WriteWhole(Path.Combine(folder, SavePaths.LiveSplitFileName), LiveSplitFile.Of(timer, Settings.RunTimer, LiveFeed.GameName));
        }
    }

    private bool overlayTried;

    // The overlay of ours is copied beside the feed, once per run of the game and only where it differs: a browser source then
    // reads state.json by a path relative to itself
    private void InstallTheOverlay(string folder)
    {
        if (overlayTried) return;
        overlayTried = true;
        string source = Path.Combine(AppContext.BaseDirectory, "Assets", SavePaths.StreamName, SavePaths.OverlayFileName);
        string target = Path.Combine(folder, SavePaths.OverlayFileName);
        try
        {
            if (!File.Exists(source)) return;
            Directory.CreateDirectory(folder);
            if (!File.Exists(target) || File.ReadAllText(target) != File.ReadAllText(source)) File.Copy(source, target, overwrite: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Log.Warn($"The stream overlay couldn't be copied to {target}: {e.Message}");
        }
    }

    /// <summary>What the feed says now (see <see cref="FeedState"/>): the party, the place, the badges, the rules, the graveyard, the timer and a battle's two sides.</summary>
    internal FeedState StreamState()
    {
        var region = Here;
        var story = flow.Story;
        var timer = flow.RunState.Timer;
        return new FeedState
        {
            Updated = WallClock.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", System.Globalization.CultureInfo.InvariantCulture),
            Place = flow.State == GameState.Title ? "" : PlaceName(),
            Region = region.Id,
            Badges = region.BadgeNames.Where(b => story.HasBadge(region.Id, b)).ToList(),
            Rules = LiveFeed.RulesOf(flow.Run),
            Party = flow.Party.Members.Select(LiveFeed.Mon).ToList(),
            Graveyard = LiveFeed.GraveyardOf(flow.RunState),
            AreasMet = flow.RunState.AreasMet.Count,
            Timer = LiveFeed.TimerOf(timer, Settings.RunTimer),
            Battle = flow.State == GameState.Battle && flow.Battle is { } battle ? BattleFeed(battle) : null
        };
    }

    // Both sides as the screen shows them (the animator's views: what the log has shown so far, not the rules a turn ahead)
    private static FeedBattle BattleFeed(BattleEngine battle)
    {
        System.Collections.Generic.List<FeedMon> Side(BattleSide side)
        {
            var list = new System.Collections.Generic.List<FeedMon>();
            for (int slot = 0; slot < battle.Anim.SlotsOf(side); slot++)
                if (battle.Anim[side, slot] is { Present: true, Shown: { } shown }) list.Add(LiveFeed.Mon(shown));
            return list;
        }
        return new FeedBattle(battle.IsTrainerBattle ? "trainer" : "wild", battle.OpponentTrainer?.FullTitle, Side(BattleSide.Player), Side(BattleSide.Enemy));
    }
}
