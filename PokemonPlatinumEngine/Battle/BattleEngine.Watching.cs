using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

/// <summary>
/// A recorded battle watched (plan 08 · P11; style guide, "Menu screens", "Watching"): the Vs. Recorder's file played
/// on screen as a battle with nobody choosing. The rules start from the file (<see cref="BattleRecordFile.ToSetup"/>:
/// its Pokémon, its trainers, its seed) and are handed the record's answers one after another, each once the screen
/// has shown everything the last one worked out; so what is shown is the recorded battle, line for line. Every
/// Pokémon, the bag and the Pokédex are the recording's own and new: nothing of the game's is touched, and a battle
/// watched writes nothing (no money, no EXP kept, no Pokédex, no record). Lines stand <see cref="WatchLineHold"/>
/// seconds and go on by themselves; Run held plays it at <see cref="WatchFastForward"/> times the pace; Menu leaves
/// at any line.
/// </summary>
public partial class BattleEngine
{
    /// <summary>How long a line of a battle watched stands before it goes on by itself.</summary>
    public const float WatchLineHold = 1.4f;

    /// <summary>How much faster a battle watched plays while Run is held.</summary>
    public const float WatchFastForward = 3f;

    /// <summary>The recording watched; null for a battle fought.</summary>
    public BattleRecordFile? Watched { get; }

    /// <summary>A recorded battle is being watched (plan 08 · P11): nobody chooses, and the record answers.</summary>
    public bool Watching => Watched != null;

    /// <summary>Menu was pressed: the viewer left before the end.</summary>
    public bool WatchLeft { get; private set; }

    /// <summary>
    /// The record ran out, or answered what the rules wouldn't take (a recording whose battle a learned move changed
    /// outside the record, plan 08 · P10): the battle can't be played on, and the viewer goes back to the list.
    /// </summary>
    public bool WatchBroke { get; private set; }

    /// <summary>Run is held: the battle plays at <see cref="WatchFastForward"/> times its pace.</summary>
    public bool WatchFast { get; private set; }

    /// <summary>The watching is over: left, broken off, or the battle shown to its end.</summary>
    public bool WatchOver => WatchLeft || WatchBroke || IsBattleOver;

    /// <summary>How many of the record's answers the rules have been handed.</summary>
    public int AnswersFed { get; private set; }

    private float lineHeld;

    /// <summary>
    /// The battle a file recorded, to watch: the rules as the file starts them, the Pokémon and trainers on screen
    /// the file's own, a bag and a Pokédex of nothing but what the recorded battle knew, and no menu ever opened.
    /// </summary>
    /// <param name="scene">The options' Battle Scene, as for a battle fought.</param>
    /// <param name="trainerLines">The options' trainers' lines in battle.</param>
    /// <param name="playerLook">The look the player's trainer stands in on the platform (the viewer's own).</param>
    public static BattleEngine ForReplay(BattleRecordFile file, bool scene = true, bool trainerLines = true, string? playerLook = null)
    {
        var rules = file.ToSetup();
        var pokedex = new Pokedex();
        foreach (int dex in file.Conditions.Caught) pokedex.RegisterCaught(dex);
        var setup = new BattleSetup
        {
            PlayerParty = rules.PlayerParty,
            Inventory = new Inventory(),
            Pokedex = pokedex,
            PcStorage = null,
            Format = rules.Format,
            WildPokemon = rules.WildPokemon,
            Trainers = rules.Trainers,
            FirstTrainerPokemon = rules.FirstTrainerPokemon,
            Random = rules.Random,
            Rules = rules.Rules,
            Conditions = rules.Conditions,
            Kind = rules.Kind,
            Partner = rules.Partner,
            CannotFlee = rules.CannotFlee,
            FirstBattle = rules.FirstBattle,
            SpecialBalls = rules.SpecialBalls,
            PlayerName = rules.PlayerName,
            PlayerLook = playerLook,
            MoveHints = false,
            TrainerLines = trainerLines,
            Frontier = rules.Frontier,
            NoBattleItems = rules.NoBattleItems,
            OffersSwitch = rules.OffersSwitch,
            Difficulty = rules.Difficulty,
            RunLevelCap = rules.RunLevelCap,
            Scene = scene
        };
        return new BattleEngine(setup, file);
    }

    /// <summary>
    /// A frame of a battle watched: <paramref name="fast"/> is Run held, <paramref name="leave"/> Menu pressed. The
    /// animations, what lands after a line and a level's panel run at the pace; a line that has stood its time is
    /// read on; nothing else is asked of the viewer.
    /// </summary>
    public void Watch(float dt, bool fast, bool leave)
    {
        if (WatchOver) return;
        if (leave)
        {
            WatchLeft = true;
            AudioManager.PlaySound("cancel");
            return;
        }
        WatchFast = fast;
        float step = fast ? dt * WatchFastForward : dt;
        Anim.Update(step, (side, slot) => SlotsOf(side).ElementAtOrDefault(slot)?.Pokemon);
        UpdatePendingEffects(step);
        HUD.LevelUp?.Advance(step);

        if (waitingForMessageConfirm)
        {
            lineHeld += step;
            if (lineHeld < WatchLineHold) return;
            lineHeld = 0f;
            ConfirmMessage();
            return;
        }
        lineHeld = 0f;

        // A timed animation (the Poké Ball's throw) holds the queue until it ends
        if (messageWaitTimer > 0f)
        {
            messageWaitTimer -= step;
            if (messageWaitTimer <= 0f) Pump();
            return;
        }
        Pump();
    }

    /// <summary>
    /// The rules wait for an answer and the screen has shown everything before it: the record's next is handed over,
    /// and what it works out is played. A record that has none left, or one the rules refuse, breaks the watching off.
    /// </summary>
    private void FeedTheRecord()
    {
        if (core.Request == null || core.Result != BattleResult.None) return;
        var answers = Watched!.Answers;
        if (AnswersFed >= answers.Count)
        {
            WatchBroke = true;
            return;
        }
        var answer = answers[AnswersFed++].Select(c => c.ToChoice()).ToList();
        try
        {
            SyncToCore();
            core.Submit(answer);
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException)
        {
            WatchBroke = true;
            return;
        }
        // Pump, which found the log played, goes on with what this worked out
        Play(core.TakeLog());
    }
}
