using System;
using System.Collections.Generic;
using Raylib_cs;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// A scene of the story skipped (plan 12 · Q20; style guide, "Story scenes"): the hold that asks for it, the dark
/// that covers it, and the field's host while the runner hurries. A script marks what may be skipped
/// (<c>scene "Name"</c> … <c>endscene</c>), the options' row says which scenes the player may skip
/// (<see cref="SceneSkip.Allowed"/>), and the menu button held for a second while one runs closes the picture,
/// has the runner hurry to the scene's end or to whatever waits for the player (<see cref="ScriptRunner.Hurry"/>),
/// and opens it again on what that left: everyone where the scene puts them, every flag, item and Pokémon as when
/// it is played. With the row off none of this is read, drawn or done.
/// </summary>
public partial class GameEngine
{
    /// <summary>How long the picture takes to go dark once the hold is complete, and to open again on what the skip left.</summary>
    public const float SkipCloseSeconds = 0.2f, SkipOpenSeconds = 0.3f;

    private enum SkipPhase { None, Closing, Opening }

    private readonly SkipHold skipHold = new();
    private readonly ScreenFade skipVeil = new();
    private SkipPhase skipPhase;

    // True only while the runner hurries, in the middle of the dark: the host isn't kept waiting by the dark then
    private bool skipRunning;
    private int scenesSkipped;

    // The people whose bubbles a skip puts away: a list of the engine's own
    private readonly List<NPC> skipNear = new();

    /// <summary>A tool's hand on the menu button in place of the key (the screenshot harness): down while set.</summary>
    internal bool SkipHeld { get; set; }

    /// <summary>
    /// Whether the scene running may be skipped now: a scene is open, the options allow skipping it, and nothing
    /// waits for the player (a question is never skipped, nor the answers beside it).
    /// </summary>
    private bool MaySkipNow =>
        skipPhase == SkipPhase.None && gameStarted && runner.IsRunning && !runner.Hurrying && runner.Scene is { } scene
        && flow.State is GameState.Overworld or GameState.Dialogue
        && !dialogue.IsQuestion && !choice.IsOpen && pendingAnswers == null
        // (nor what a hurry couldn't end at once: a puzzle's pieces on the move, a Pokémon being let out of its ball)
        && !PuzzleMoving && !AwaitingStroll
        && SceneSkip.Allowed(Settings.SkipScenes, scene, story, Settings.ScenesSeen);

    /// <summary>The hold is read while a script has the field or its text is on the screen; complete, the skip begins. True when it did.</summary>
    private bool ReadTheSkip(float dt)
    {
        bool down = SkipHeld || InputManager.IsActionDown(GameAction.Menu);
        if (!skipHold.Update(down, MaySkipNow, dt)) return false;
        BeginSkip();
        return true;
    }

    /// <summary>The text on the screen is put away unread and the picture goes dark; the scene is hurried through once it is.</summary>
    private void BeginSkip()
    {
        if (dialogue.IsActive) dialogue.Close();
        if (flow.State == GameState.Dialogue) flow.State = GameState.Overworld;
        skipVeil.Clear();
        skipVeil.To(true, SkipCloseSeconds);
        skipPhase = SkipPhase.Closing;
    }

    /// <summary>
    /// A frame of a skip's dark: it closes, the scene is hurried through behind it, and it opens on what that left.
    /// True while it has the field, which is the script's and the player's again once it has opened.
    /// </summary>
    private bool UpdateSkip(float dt)
    {
        if (skipPhase == SkipPhase.None) return false;
        skipVeil.Update(dt);
        if (skipVeil.IsMoving) return true;
        if (skipPhase == SkipPhase.Opening)
        {
            skipPhase = SkipPhase.None;
            return false;
        }

        HurryThroughTheScene();
        // What the hurry stopped before is carried out once the picture is back. If something has the screen all the
        // same (a host that had to be seen through after all), the dark is gone at once rather than left over it
        if (flow.State != GameState.Overworld)
        {
            skipVeil.Clear();
            skipPhase = SkipPhase.None;
            return false;
        }
        skipVeil.To(false, SkipOpenSeconds);
        skipPhase = SkipPhase.Opening;
        return true;
    }

    private void HurryThroughTheScene()
    {
        skipRunning = true;
        try
        {
            if (!runner.Hurry()) return;
            scenesSkipped++;
            RunScript(0f);
        }
        finally
        {
            skipRunning = false;
        }
    }

    /// <summary>
    /// Whatever a script left going is at its end, at once: a gesture is over and a bubble gone, a field move's band
    /// is closed, the script's fade and the camera's moves are where they were going, an effect that passes and a
    /// set piece are over. Whoever is walking is left walking: their steps are taken in the time the scene gives them
    /// (<see cref="WalkOnFor"/>, <see cref="FinishTheWalks"/>).
    /// </summary>
    private void SettleTheScene()
    {
        if (actor.Waiting)
        {
            // (The gesture alone: the face they hold is the scene's to keep)
            if (actor.Who is { } who)
            {
                var held = who.Mood;
                who.StopActing();
                who.Mood = held;
            }
            else
            {
                var held = player.Mood;
                player.StopActing();
                player.Mood = held;
            }
            actor = default;
        }
        cutIn = null;
        cutInSubject = null;
        scriptFade.Finish();
        world.Camera.Settle();
        player.ShowBubble(EmoteBubble.None, 0f);
        foreach (var npc in currentMap.NPCs.Near(player.GridX, player.GridY, Wandering.Reach, skipNear)) npc.ShowBubble(EmoteBubble.None, 0f);
        world.Life.EndPassingEffects();
        world.Pieces.Settle(world.Life.Now);
    }

    // A frame of the walks a hurry takes unseen, and the longest wait it lets pass for them
    private const float SkipFrame = 1f / 60f, LongestElapsed = 30f;

    private bool SomeoneWalks => playerWalk != null || npcWalks.Count > 0 || player.IsMoving;

    /// <summary>
    /// Everyone a script set walking takes their steps to the end, a frame at a time and all together, as they would
    /// have with the picture on: the player's are the player's own (a ledge is hopped, a wall or someone in the way
    /// given up at), so a skipped walk ends where a played one does. Where a played scene waits for its walks.
    /// </summary>
    private void FinishTheWalks()
    {
        // (A walk gives up at a wall after three quarters of a second, so this ends by itself; the count is a guard)
        for (int i = 0; i < 60 * 120 && SomeoneWalks; i++) WalkAFrame();
        foreach (var walk in npcWalks) walk.Finish();
        npcWalks.Clear();
        playerWalk = null;
        fieldMoveStep = false;
    }

    /// <summary>
    /// The time a hurry doesn't wait passes for whoever is walking: they take the steps they would have taken in it,
    /// so someone a scene sends ahead before a pause is as far ahead when the next walk begins as when it is played.
    /// </summary>
    private void WalkOnFor(float seconds)
    {
        int frames = (int)MathF.Round(Math.Clamp(seconds, 0f, LongestElapsed) / SkipFrame);
        for (int i = 0; i < frames && SomeoneWalks; i++) WalkAFrame();
    }

    private void WalkAFrame()
    {
        AdvanceScriptedWalks(SkipFrame);
        along?.Update(SkipFrame, player.Stride);
    }

    /// <summary>Goes to a tile of a map at once, behind a skip's dark: what a script's warp does at the middle of its own fade.</summary>
    private void ArriveAtOnce(string mapName, int x, int y, Direction? facing)
    {
        var target = MapDatabase.Get(mapName);
        PlayAreaMusic(target, x, y);
        currentMap = target;
        player.SetPosition(x, y, facing ?? player.Facing);
        world.Life.Clear();
        ArriveOnMap();
        AnnounceLocation();
    }

    /// <summary>The skip's dark over the field and everything on it, and the hold's ring over that, while the button is held.</summary>
    private void DrawSkip()
    {
        if (skipVeil.Level > 0f)
            Raylib.DrawRectangle(0, 0, VirtualWidth, VirtualHeight, new Color(0, 0, 0, (int)MathF.Round(skipVeil.Level * 255f)));
        // Beside a Safari Game's panel, whose corner the ring's is
        if (skipHold.Showing && MaySkipNow) ModernUi.SkipRing(VirtualWidth, skipHold.Fill, safari.Active ? 32f + 300f + 24f : ModernUi.SkipRingMargin);
    }

    /// <summary>A new game, a loaded one: no skip is under way and no hold is counted.</summary>
    private void ResetTheSkip()
    {
        skipHold.Reset();
        skipVeil.Clear();
        skipPhase = SkipPhase.None;
        skipRunning = false;
        AudioManager.Hushed = false;
    }

    private sealed partial class FieldHost
    {
        public bool Skipping { get; private set; }

        public void BeginSkipping()
        {
            Skipping = true;
            // Nothing a hurry passes is heard, whoever would have asked for it; a change of music is kept
            AudioManager.Hushed = true;
            game.SettleTheScene();
        }

        public void Elapse(float seconds) => game.WalkOnFor(seconds);

        public void Arrive() => game.FinishTheWalks();

        public void EndSkipping()
        {
            // What the hurry let pass is gone; whoever is still walking walks on as the picture opens, as they
            // would be walking here with the scene played
            game.world.Life.EndPassingEffects();
            Skipping = false;
            AudioManager.Hushed = false;
            // From here the dark keeps the script waiting again, until it has opened
            game.skipRunning = false;
        }

        public void SceneBegins(string key)
        {
        }

        public void SceneEnds(string key, bool seen)
        {
            // This machine has seen it too, whatever the options say of skipping: written once for each scene
            if (seen && game.Settings.SeeScene(key)) game.Settings.Save();
        }
    }
}
