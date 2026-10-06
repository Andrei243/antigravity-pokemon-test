using System;
using System.Collections.Generic;
using System.Linq;
using Raylib_cs;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The story in the field (plan 02 · S1): what starts a script, and the game as a script's host. A script runs
/// over the field, in <see cref="GameState.Overworld"/>, and the player's keys do nothing meanwhile; it goes on
/// only while the game is in that state, so whatever it starts that changes the state (text, a battle, a screen, a
/// fade into another map) is waited for by itself.
/// </summary>
public partial class GameEngine
{
    private ScriptLibrary scripts = null!;
    private ScriptRunner runner = null!;

    // A question's answers, shown beside the text box once the question is written out
    private readonly ChoiceBox choice = new();
    private IReadOnlyList<string>? pendingAnswers;
    private int pendingCancel;
    private int scriptAnswer;

    // What a script set going: people walking, the player walking, the screen gone black, the camera sent away
    private readonly List<NpcWalk> npcWalks = new();
    private PlayerWalk? playerWalk;
    private readonly ScreenFade scriptFade = new();
    private bool cameraSent;

    // The battle a script started: how it came out, and whether losing it is part of the story
    private BattleOutcome scriptOutcome;
    private bool battleMayBeLost;

    // The story as the maps last saw it, and whether the place the player came to has yet to run its own script
    private int presenceRevision = -1;
    private bool arrived;

    /// <summary>True while a script has the field: the player's keys do nothing.</summary>
    public bool ScriptRunning => runner != null && runner.IsRunning;

    /// <summary>The answers on screen for a question a script asked: tests and the harness pick one with its own buttons.</summary>
    public ChoiceBox Choice => choice;

    private void LoadScripts()
    {
        scripts = ScriptLibrary.Default;
        runner = new ScriptRunner(scripts, new FieldHost(this));
    }

    // ------------------------------------------------------------------ what starts a script

    /// <summary>
    /// Starts a script over the field. Its name is looked for in the file of the place first (the subject's own,
    /// or where the player stands) and among the common ones second. False, with nothing started, when there is
    /// no such script.
    /// </summary>
    /// <param name="item">The item the script's <c>find own</c> gives, when it isn't the subject's (a hidden item).</param>
    /// <param name="flag">The flag its <c>setflag own</c> sets, when it isn't the one that hides the subject.</param>
    public bool StartScript(string name, NPC? subject = null, IReadOnlyList<string>? own = null, string? file = null,
        (string Item, int Count)? item = null, string? flag = null)
    {
        file ??= subject?.ScriptFile ?? currentMap.ScriptFileAt(subject?.GridX ?? player.GridX, subject?.GridY ?? player.GridY);
        if (scripts.Find(name, file) is not { } script)
        {
            Console.Error.WriteLine($"No script '{name}' for {file}.");
            return false;
        }
        runner.Start(script, subject, own, item, flag);
        return true;
    }

    /// <summary>Starts a script that is in no file: one a tool wrote for the occasion (the screenshot harness's scene).</summary>
    public void StartScript(Script script, NPC? subject = null) => runner.Start(script, subject);

    /// <summary>The script of the place the player has come to (<c>OnEnter</c> in its file), if it has one.</summary>
    private bool StartEnterScript()
    {
        string file = currentMap.ScriptFileAt(player.GridX, player.GridY);
        if (scripts.In(file, ScriptLibrary.OnEnter) is not { } script) return false;
        runner.Start(script);
        return true;
    }

    /// <summary>A step ended on tiles that start a script while the story is where they wait for it.</summary>
    private bool TryStepTrigger()
    {
        if (FieldScripts.TriggerAt(currentMap, player.GridX, player.GridY, story) is not { } trigger) return false;
        return StartScript(trigger.Script, file: trigger.ScriptFile ?? currentMap.Name);
    }

    /// <summary>Puts everyone of every map where the story has them, when the story has changed since they were last put.</summary>
    private void RefreshPresence(bool startOver = false)
    {
        if (!startOver && presenceRevision == story.Revision) return;
        presenceRevision = story.Revision;
        MapDatabase.ApplyPresence(story.Has, forget: startOver);
    }

    /// <summary>The player has come to a map by a door, a warp or a save: whoever a script moved off it or onto it is back as the flags say.</summary>
    private void ArriveOnMap()
    {
        currentMap.ForgetForced();
        currentMap.ApplyPresence(story.Has);
        // Whoever was still walking on the map left behind stands where they were going, not between two tiles
        foreach (var walk in npcWalks) walk.Finish();
        npcWalks.Clear();
        playerWalk = null;
        arrived = true;
    }

    // ------------------------------------------------------------------ a frame

    /// <summary>One frame of the field while a script has it.</summary>
    private void UpdateScript(float dt)
    {
        AdvanceScriptedWalks(dt);
        RunScript(dt);
    }

    private void RunScript(float dt)
    {
        try
        {
            runner.Update(dt);
        }
        catch (ScriptException e)
        {
            // A script that goes wrong ends there; the game goes on
            Console.Error.WriteLine(e.Message);
            ShowNotification("A script went wrong: " + e.Message);
            runner.Abort();
        }
        RefreshPresence();
        if (!runner.IsRunning) EndScript();
    }

    /// <summary>People a script set walking take their steps, and the player theirs or stands still. Also under text.</summary>
    private void AdvanceScriptedWalks(float dt)
    {
        for (int i = npcWalks.Count - 1; i >= 0; i--)
        {
            npcWalks[i].Update(dt);
            if (npcWalks[i].IsDone) npcWalks.RemoveAt(i);
        }

        if (playerWalk == null)
        {
            player.StandStill(dt);
            return;
        }
        playerWalk.Update(dt, player, currentMap, StepLeavesItsMark);
        if (playerWalk.IsDone) playerWalk = null;
    }

    /// <summary>Whatever a script left going is brought to its end: nobody is left mid-step, the screen comes back, the camera returns.</summary>
    private void EndScript()
    {
        foreach (var walk in npcWalks) walk.Finish();
        npcWalks.Clear();
        playerWalk = null;
        if (scriptFade.Level > 0f) scriptFade.To(false, ScriptParser.FadeSeconds);
        if (cameraSent)
        {
            world.ReleaseCamera(ScriptParser.ReleaseSeconds);
            cameraSent = false;
        }
        // (Answers already given slide away by themselves; only a question left open is taken off the screen)
        if (choice.IsOpen) choice.Dismiss();
        pendingAnswers = null;
        // A trainer's eye theme that no battle cut in on (their script had no battle) gives way to the place's own
        if (eyeThemePlaying && currentState is GameState.Overworld or GameState.Dialogue) PlayFieldMusic();
    }

    /// <summary>
    /// One frame of text on the screen: the line is written and waits, a question waits for its answer, and the
    /// script that showed it goes on in the same frame the box closes, so the box doesn't blink between two talks.
    /// </summary>
    private void UpdateDialogue(float dt)
    {
        if (ScriptRunning) AdvanceScriptedWalks(dt);

        if (choice.IsOpen) choice.ReadKeys();
        else dialogue.Update(dt);
        AnswerQuestion();

        // Back to the field, unless the last line led somewhere else (a ship leaving for another region)
        if (!dialogue.IsActive && currentState == GameState.Dialogue)
        {
            currentState = GameState.Overworld;
            if (ScriptRunning) RunScript(0f);
        }
    }

    /// <summary>A question written out shows its answers; the answer given closes the box.</summary>
    private void AnswerQuestion()
    {
        if (!dialogue.AwaitingAnswer || choice.IsOpen) return;
        if (choice.Take() is { } picked)
        {
            scriptAnswer = picked;
            dialogue.Close();
        }
        else if (pendingAnswers != null)
        {
            choice.Open(pendingAnswers, pendingCancel);
            pendingAnswers = null;
        }
    }

    /// <summary>The script's own fade and its question's answers, drawn over the field: the text box is between them.</summary>
    private void DrawScriptFade()
    {
        if (scriptFade.Level <= 0f) return;
        Raylib.DrawRectangle(0, 0, VirtualWidth, VirtualHeight, new Color(0, 0, 0, (int)MathF.Round(scriptFade.Level * 255f)));
    }

    private void DrawChoice()
    {
        if (choice.Visible) ModernUi.DrawChoices(VirtualWidth, VirtualHeight, choice.Options, choice.Cursor, choice.Shown);
    }

    /// <summary>What a step leaves behind: a print, dust, leaves, a ring on the water, a splash where they rode out onto it.</summary>
    private void StepLeavesItsMark()
    {
        if (player.JustRodeOut) world.Life.Splash(currentMap, player.GridX, player.GridY);
        else world.Life.Footstep(currentMap, player.GridX, player.GridY, player.Facing, player.IsRunning, player.Mode);
        if (player.JustLanded && player.Mode != TravelMode.Surfing) world.Life.Landing(currentMap, player.GridX, player.GridY);
    }

    /// <summary>Goes to a tile of a map through a fade, as a door does: a script's warp.</summary>
    private void WarpTo(string mapName, int x, int y, Direction? facing)
    {
        var target = MapDatabase.Get(mapName);
        PlayAreaMusic(target, x, y);
        StartTransition(GameState.Overworld, () =>
        {
            currentMap = target;
            player.SetPosition(x, y, facing ?? player.Facing);
            world.Life.Clear();
            ArriveOnMap();
            AnnounceLocation();
        });
    }

    // ------------------------------------------------------------------ the host

    /// <summary>The game as a script sees it.</summary>
    private sealed class FieldHost : IScriptHost
    {
        private readonly GameEngine game;

        public FieldHost(GameEngine game) => this.game = game;

        public StoryState Story => game.story;
        public Party Party => game.playerParty;
        public Inventory Bag => game.playerInventory;

        public int Money
        {
            get => game.playerMoney;
            set => game.playerMoney = Math.Max(0, value);
        }

        public string PlayerName => PlayerIdentity.Name;
        public PlayerLook PlayerLook => PlayerIdentity.Look;

        // ---- the field

        public NPC? FindNpc(string name, string? place) => game.currentMap.FindPerson(name, place);

        public (int X, int Y) TileOf(NPC? who) => who == null ? (game.player.GridX, game.player.GridY) : (who.GridX, who.GridY);

        public Direction FacingOf(NPC? who) => who?.Facing ?? game.player.Facing;

        public void Face(NPC? who, Direction direction)
        {
            if (who == null) game.player.Facing = direction;
            else who.Facing = direction;
        }

        public void Place(NPC? who, int x, int y, Direction? facing)
        {
            if (who == null)
            {
                game.player.SetPosition(x, y, facing ?? game.player.Facing);
                return;
            }
            StopWalk(who);
            (who.GridX, who.GridY) = (x, y);
            if (facing is { } direction) who.Facing = direction;
        }

        public void SetVisible(NPC who, bool visible)
        {
            who.Forced = visible;
            game.currentMap.ApplyPresence(game.story.Has);
        }

        public void Walk(NPC? who, IReadOnlyList<Direction> steps, bool fast)
        {
            if (who == null)
            {
                game.playerWalk = new PlayerWalk(steps, fast);
                return;
            }
            StopWalk(who);
            game.npcWalks.Add(new NpcWalk(who, steps, fast));
        }

        /// <summary>Whoever is told to go somewhere else first arrives where they were going.</summary>
        private void StopWalk(NPC who)
        {
            foreach (var walk in game.npcWalks.Where(w => w.Who == who)) walk.Finish();
            game.npcWalks.RemoveAll(w => w.Who == who);
        }

        public bool Walking => game.playerWalk != null || game.npcWalks.Count > 0;

        public void Emote(NPC? who, EmoteBubble bubble, float seconds)
        {
            if (who == null) game.player.ShowBubble(bubble, seconds);
            else who.ShowBubble(bubble, seconds);
            if (bubble == EmoteBubble.Exclaim) AudioManager.PlaySound("exclaim", who == null ? 0f : game.PanAt(who.GridX));
        }

        public void Camera(CameraMove move, int x, int y, float seconds)
        {
            switch (move)
            {
                case CameraMove.Pan:
                    game.world.PanCamera(x + 0.5f, y + 0.5f, seconds);
                    game.cameraSent = true;
                    break;
                case CameraMove.Release:
                    game.world.ReleaseCamera(seconds);
                    game.cameraSent = false;
                    break;
                default:
                    game.world.ShakeCamera(seconds);
                    break;
            }
        }

        // ---- what the script waits for

        public bool Busy => game.currentState != GameState.Overworld || game.dialogue.IsActive || game.scriptFade.IsMoving;

        public void Say(string? speaker, IReadOnlyList<string> lines)
        {
            if (lines.Count == 0) return;
            game.dialogue.ShowDialogue(speaker ?? "", lines);
            game.currentState = GameState.Dialogue;
        }

        public void Ask(string? speaker, string question, IReadOnlyList<string> answers, int cancel)
        {
            game.choice.Dismiss();
            game.pendingAnswers = answers;
            game.pendingCancel = cancel;
            game.scriptAnswer = Math.Max(0, cancel);
            game.dialogue.ShowQuestion(speaker ?? "", question);
            game.currentState = GameState.Dialogue;
        }

        public int Answer => game.scriptAnswer;

        public void Battle(NPC trainer, NPC? second, Trainer? partner, bool mayLose, bool first)
        {
            game.scriptOutcome = BattleOutcome.None;
            game.battleMayBeLost = mayLose;
            game.StartTrainerBattle(trainer, second, partner, first);
        }

        public void WildBattle(Pokemon wild, BattleKind kind, bool cannotFlee)
        {
            game.scriptOutcome = BattleOutcome.None;
            game.battleMayBeLost = false;
            game.MeetWildPokemon(wild, kind, cannotFlee);
        }

        public BattleOutcome Outcome => game.scriptOutcome;

        public void Open(ScriptScreen screen, NPC? subject)
        {
            game.scriptAnswer = 0;
            switch (screen)
            {
                case ScriptScreen.Starter:
                    game.currentState = GameState.StarterSelect;
                    game.starterSelectScreen.Open();
                    break;
                case ScriptScreen.Shop:
                    game.currentState = GameState.Shop;
                    game.shopScreen.Open(game.currentMap.DisplayNameAt(game.player.GridX, game.player.GridY));
                    break;
                case ScriptScreen.Pc:
                    game.currentState = GameState.PCStorage;
                    game.pcScreen.Open();
                    break;
                case ScriptScreen.Travel:
                    // The way to the next region, where this map has one: the attendant says how things stand,
                    // and the ship leaves when the story of this region is done
                    if (RegionDatabase.RegionOfMap(game.currentMap.Name) is not { } here || RegionDatabase.LinkFrom(here.Id) is not { } link) break;
                    var check = RegionDatabase.CheckTravel(link, game.story);
                    game.scriptAnswer = 1;
                    game.dialogue.ShowDialogue(subject?.Name ?? "", RegionDatabase.AttendantLines(link, check),
                        check == TravelCheck.Ready ? () => game.TravelTo(RegionDatabase.Get(link.To)!) : null);
                    game.currentState = GameState.Dialogue;
                    break;
            }
        }

        public bool GivePokemon(Pokemon pokemon)
        {
            game.playerPokedex.RegisterSeen(pokemon.Species.DexNumber);
            game.playerPokedex.RegisterCaught(pokemon.Species.DexNumber);
            if (game.playerParty.Add(pokemon)) return true;
            game.pcBoxStorage.Add(pokemon);
            return false;
        }

        public void Warp(string map, int x, int y, Direction? facing) => game.WarpTo(map, x, y, facing);

        public void Fade(bool toBlack, float seconds) => game.scriptFade.To(toBlack, seconds);

        // ---- sound

        public void Music(string? song)
        {
            if (song == null) game.PlayFieldMusic();
            else if (song.Length == 0) AudioManager.StopMusic();
            else AudioManager.PlayMusic(song);
        }

        public void Fanfare(MusicRole role) => AudioManager.PlayFanfare(role);

        public void Sound(string name) => AudioManager.PlaySound(name);

        // The original's field cries (a legendary in its lair, a Pokémon a script brings out) have an echo beside them
        public void Cry(string species)
        {
            if (PokemonDatabase.Get(species) is { } own) AudioManager.PlayCry(own, null, CryMode.FieldEvent);
            else if (PokemonDatabase.SpeciesOfForm(species) is { } of) AudioManager.PlayCry(of, species, CryMode.FieldEvent);
        }
    }
}
