using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// Carries a script out, a line at a time, in a host (plan 02 · S1). It runs as far as it can whenever it is
/// updated and stops where the player has to be waited for: text to read, a question, a walk, a battle, a screen,
/// a pause. Where a script goes next is decided here and nowhere else, so a script does the same in the game and
/// in a test's host with no screen.
/// </summary>
public sealed class ScriptRunner
{
    /// <summary>The lines a script may carry out in one go before it is taken to be going round in circles.</summary>
    public const int MostLinesAtOnce = 5000;

    /// <summary>How deep scripts may call one another.</summary>
    public const int MostCalls = 32;

    /// <summary>The answers of a plain question, in the order they are shown: the first is yes.</summary>
    public static readonly string[] YesNo = { "Yes", "No" };

    private readonly ScriptLibrary library;
    private readonly IScriptHost host;
    private readonly Stack<(Script Script, int At)> calls = new();

    private Script? script;
    private string place = "";
    private int at;
    private IReadOnlyList<string> ownLines = Array.Empty<string>();
    private string? speaker;
    private float waiting;
    private bool waitingForWalks;
    private Action? afterBusy;
    private BattleOutcome lastOutcome;
    private string lastItem = "";
    private (string Item, int Count)? ownItem;
    private string? ownFlag;
    private Pokemon? ownPokemon;
    private string lastUser = "";

    // What the last word on a berry patch and the lottery came to (plan 06 · R14a): {berry}, {berries}, {mulch},
    // {lotterymon}
    private string lastBerry = "", lastMulch = "";
    private int lastYield;
    private Lottery.Match lastLottery;

    public ScriptRunner(ScriptLibrary library, IScriptHost host)
    {
        this.library = library;
        this.host = host;
    }

    public bool IsRunning => script != null;

    /// <summary>The script being carried out (the one called, while one calls another); null when none is.</summary>
    public Script? Current => script;

    /// <summary>Whoever the script belongs to: the person spoken to, the trainer who challenged. <c>self</c> in a script.</summary>
    public NPC? Subject { get; private set; }

    /// <summary>
    /// The second of two trainers who saw the player at once and came together (<c>APPROACH_TYPE_VS2</c>, plan 02 ·
    /// S6): <c>pair</c> in the script. Null for any other script.
    /// </summary>
    public NPC? Pair { get; private set; }

    /// <summary>What the last question, battle or handing-over came to. <c>RESULT</c> in a script.</summary>
    public int Result { get; private set; }

    /// <summary>True when the last script ended because the player lost a battle it couldn't go on from.</summary>
    public bool EndedInDefeat { get; private set; }

    /// <summary>
    /// Starts a script. Nothing of it is carried out until <see cref="Update"/>.
    /// </summary>
    /// <param name="subject">Whoever it belongs to, if anyone.</param>
    /// <param name="own">The lines <c>sayown</c> says: a person's, a signboard's.</param>
    /// <param name="item">The item <c>find own</c> gives; left out, what the subject holds (an item ball's).</param>
    /// <param name="flag">The flag <c>setflag own</c> sets; left out, the one that hides the subject.</param>
    /// <param name="pokemon">The Pokémon of the team the script was started for: the one whose field move was chosen in the party menu.</param>
    public void Start(Script start, NPC? subject = null, IReadOnlyList<string>? own = null, (string Item, int Count)? item = null, string? flag = null,
        Pokemon? pokemon = null, NPC? pair = null)
    {
        ownPokemon = pokemon;
        Pair = pair;
        ownItem = item ?? (subject is { Item: { } held } ? ((string, int)?)(held, Math.Max(1, subject.ItemCount)) : null);
        ownFlag = flag ?? subject?.HiddenBy;
        calls.Clear();
        script = start;
        at = 0;
        Subject = subject;
        // Whose people a name means: those of the place the script is written for, or of whoever started a common one
        place = start.File != ScriptLibrary.Common ? start.File : subject?.ScriptFile ?? "";
        ownLines = own ?? subject?.DialogLines ?? (IReadOnlyList<string>)Array.Empty<string>();
        speaker = subject?.Name;
        waiting = 0f;
        waitingForWalks = false;
        afterBusy = null;
        lastOutcome = BattleOutcome.None;
        Result = 0;
        EndedInDefeat = false;
    }

    /// <summary>Stops the script where it is.</summary>
    public void Abort() => Finish();

    /// <summary>Carries the script on as far as it goes without the player.</summary>
    public void Update(float dt)
    {
        if (script == null) return;
        if (waiting > 0f)
        {
            waiting -= dt;
            if (waiting > 0f) return;
            waiting = 0f;
        }

        int budget = MostLinesAtOnce;
        while (script != null)
        {
            if (host.Busy) return;
            if (afterBusy is { } collect)
            {
                afterBusy = null;
                collect();
                continue;
            }
            if (waitingForWalks)
            {
                if (host.Walking) return;
                waitingForWalks = false;
            }
            if (waiting > 0f) return;

            if (at >= script.Code.Count)
            {
                Return();
                continue;
            }
            if (--budget < 0)
                throw new ScriptException($"{script.FullName} carried out {MostLinesAtOnce} lines without a pause: it goes round in circles.");
            Execute(script.Code[at++]);
        }
    }

    /// <summary>
    /// Runs the script to its end in a host that never keeps it waiting (see <see cref="HeadlessScriptHost"/>):
    /// pauses pass at once. Throws if it is still going after <paramref name="mostUpdates"/> rounds.
    /// </summary>
    public void RunToEnd(int mostUpdates = 2000)
    {
        string name = script?.FullName ?? "";
        for (int n = 0; script != null; n++)
        {
            if (n >= mostUpdates) throw new ScriptException($"{name} was still running after {mostUpdates} rounds: it never ends, or its host keeps it waiting.");
            Update(3600f);
        }
    }

    private void Finish()
    {
        script = null;
        calls.Clear();
        afterBusy = null;
        waiting = 0f;
        waitingForWalks = false;
    }

    private void Return()
    {
        if (calls.Count == 0)
        {
            Finish();
            return;
        }
        (script, at) = calls.Pop();
    }

    // ------------------------------------------------------------------ a line

    private void Execute(Instruction i)
    {
        var story = host.Story;
        switch (i.Op)
        {
            case Op.If:
                if (Holds(i.Condition!, i)) Execute(i.Then!);
                break;

            case Op.Say:
            case Op.Text:
            {
                // Lines one after another are one talk: the box doesn't close between them
                var lines = new List<string>(i.Lines);
                while (at < script!.Code.Count && script.Code[at].Op == i.Op) lines.AddRange(script.Code[at++].Lines);
                host.Say(i.Op == Op.Say ? speaker : null, lines.Select(Fill).ToList());
                break;
            }
            case Op.SayOwn:
                if (ownLines.Count > 0) host.Say(speaker, ownLines.Select(Fill).ToList());
                break;
            case Op.TrainerLine:
            {
                var whose = i.Name.Length > 0 ? Person(i.Name, i)! : Subject ?? throw Wrong(i, "'trainerline' needs someone the script belongs to");
                var trainer = whose.TrainerData ?? throw Wrong(i, $"{whose.Name} is no trainer and has no trainer's lines");
                string line = i.Option ? trainer.DialogueAfter : trainer.DialogueBefore;
                Result = string.IsNullOrEmpty(line) ? 0 : 1;
                // A line written "Youngster Tristan: ..." is said under that whole title, as the text box shows who speaks
                string? title = line.StartsWith(trainer.FullTitle + ": ", StringComparison.Ordinal) ? trainer.FullTitle : whose == Subject ? speaker : whose.Name;
                if (Result == 1) host.Say(title, new[] { Fill(line) });
                break;
            }
            case Op.Speaker:
                speaker = i.Option ? Subject?.Name : i.Name.Length > 0 ? i.Name : null;
                break;
            case Op.Ask:
                host.Ask(speaker, Fill(i.Lines[0]), YesNo, cancel: 1);
                afterBusy = () => Result = host.Answer == 0 ? 1 : 0;
                break;
            case Op.Choose:
                // Backing out of a menu picks its last answer, which is where a script puts its way out
                host.Ask(speaker, Fill(i.Lines[0]), i.Lines.Skip(1).Select(Fill).ToList(), cancel: i.Lines.Count - 2);
                afterBusy = () => Result = host.Answer;
                break;

            case Op.Goto:
                at = i.Target;
                break;
            case Op.Call:
            {
                var target = library.Find(i.Name, script!.File) ?? throw Wrong(i, $"there is no script '{i.Name}' to call");
                if (calls.Count >= MostCalls) throw Wrong(i, $"scripts call one another more than {MostCalls} deep");
                calls.Push((script, at));
                script = target;
                at = 0;
                break;
            }
            case Op.Return:
                Return();
                break;
            case Op.End:
                Finish();
                break;

            case Op.SetFlag:
                story.Set(i.Own ? OwnFlag(i) : i.Name);
                break;
            case Op.ClearFlag:
                story.Unset(i.Own ? OwnFlag(i) : i.Name);
                break;
            case Op.SetVar:
                story.SetVar(i.Name, i.Number);
                break;
            case Op.AddVar:
                story.AddVar(i.Name, i.Number);
                break;

            case Op.Give:
            case Op.Find:
            {
                // Handed over or picked up: the same fanfare and the same putting away, in other words
                var (item, count) = Given(i);
                // A ball picked up off the ground is a line of the Journal (scripts_visible_items.s); a gift or a hidden find isn't
                Hand(item, count, i.Op == Op.Find ? "found" : "received", journal: i.Op == Op.Find && Subject?.IsItemBall == true);
                break;
            }
            case Op.AddItem:
            {
                var (item, count) = Given(i);
                host.Bag.AddItem(item, count);
                lastItem = item.Name;
                break;
            }
            case Op.Take:
            {
                // All of them or none: a script asks for three coupons and takes three
                var item = ItemOf(i);
                lastItem = item.Name;
                Result = host.Bag.GetQuantity(item) >= i.Number && host.Bag.RemoveItem(item, i.Number) ? 1 : 0;
                break;
            }
            case Op.GivePokemon:
            {
                var species = PokemonDatabase.Get(i.Name) ?? throw Wrong(i, $"there is no species '{i.Name}'");
                Result = host.GivePokemon(new Pokemon(species, i.Number)) ? 1 : 2;
                break;
            }
            case Op.GiveBadge:
                story.GiveBadge(i.Badge);
                host.Fanfare(MusicRole.FanfareBadge);
                break;
            case Op.GiveMoney:
                host.Money += i.Number;
                break;
            case Op.TakeMoney:
                Result = host.Money >= i.Number ? 1 : 0;
                if (Result == 1) host.Money -= i.Number;
                break;
            case Op.Heal:
                host.Party.HealAll();
                break;
            case Op.Turnback:
                host.Turnback();
                break;
            // SetTrainerFlag: a Gym's trainers count as beaten once its Leader is (plan 01 · M9)
            case Op.Defeat:
                story.Defeat(i.Name);
                host.Defeat(i.Name);
                break;
            // AdvanceEternaGymClock: the clock turns on to its next time; RESULT is 1 when it turned, 2 when a
            // fountain drained with it too, 0 once the Leader is beaten and there is no further time
            case Op.FlowerClock:
            {
                int from = story.Var(EternaClock.StateVar);
                Result = EternaClock.Advance(story) ? (EternaClock.HasWater(from, true) != EternaClock.HasWater(from + 1, true)
                    || EternaClock.HasWater(from, false) != EternaClock.HasWater(from + 1, false) ? 2 : 1) : 0;
                if (Result > 0) host.TurnClock(from, from + 1);
                break;
            }
            case Op.Partner:
                if (i.Option)
                {
                    if (TrainerDatabase.Get(i.Other) == null) throw Wrong(i, $"there is no trainer '{i.Other}'");
                    host.TravelWith(Person(i.Name, i) ?? throw Wrong(i, "the player can't travel with themselves"), i.Other);
                }
                else host.TravelWith(null, null);
                break;

            case Op.Battle:
            {
                if (NoPokemonToBattle()) break;
                var foe = Person(i.Name, i) ?? throw Wrong(i, "the player can't be battled");
                if (i.AsTrainer.Length > 0)
                {
                    // Someone of the map who battles with a team of Platinum's: they are that trainer from now on
                    var record = TrainerDatabase.Get(i.AsTrainer) ?? throw Wrong(i, $"there is no trainer '{i.AsTrainer}'");
                    var team = new Trainer { Id = record.Id };
                    TrainerDatabase.Fill(team, record);
                    foe.TrainerData = team;
                }
                if (foe.TrainerData == null) throw Wrong(i, $"{foe.Name} is no trainer and can't be battled");
                NPC? second = null;
                if (i.Other.Length > 0)
                {
                    second = Person(i.Other, i) ?? throw Wrong(i, "the player can't be battled");
                    if (second.TrainerData == null) throw Wrong(i, $"{second.Name} is no trainer and can't be battled");
                }
                Trainer? partner = null;
                // Whoever travels with the player, if anyone does (plan 02 · S6): a fresh team of theirs for each
                // battle, as the original builds the partner's party from the data every time (Trainer_Encounter)
                if (i.Partner == "partner" && !i.PartnerById)
                {
                    if (host.Partner is { } id)
                    {
                        var record = TrainerDatabase.Get(id) ?? throw Wrong(i, $"there is no trainer '{id}'");
                        partner = new Trainer { Id = record.Id };
                        TrainerDatabase.Fill(partner, record);
                    }
                }
                else if (i.PartnerById)
                {
                    var record = TrainerDatabase.Get(i.Partner) ?? throw Wrong(i, $"there is no trainer '{i.Partner}'");
                    partner = new Trainer { Id = record.Id };
                    TrainerDatabase.Fill(partner, record);
                }
                else if (i.Partner.Length > 0)
                {
                    var beside = Person(i.Partner, i) ?? throw Wrong(i, "the player can't be their own partner");
                    partner = beside.TrainerData ?? throw Wrong(i, $"{beside.Name} is no trainer and can't battle beside the player");
                }
                bool mayLose = i.Option;
                // A rematch the Vs. Seeker found: the trainer brings the team of the level the story has reached, for
                // this battle, and stops waiting for one (VsSeeker_GetRematchTrainerID, SetMoveCodeForFacingDirection)
                var own = foe.TrainerData;
                if (i.Rematch && own != null && TrainerDatabase.Get(own.Id) is { } ownRecord && VsSeeker.RematchTeam(ownRecord, story) is { } rematchId
                    && TrainerDatabase.Get(rematchId) is { } rematch)
                {
                    var team = new Trainer { Id = rematch.Id, Name = own.Name, TrainerClass = own.TrainerClass, DialogueBefore = own.DialogueBefore, DialogueAfter = own.DialogueAfter };
                    TrainerDatabase.Fill(team, rematch);
                    foe.TrainerData = team;
                    foe.ReadyForRematch = false;
                    host.Battle(foe, second, partner, mayLose, i.FirstBattle);
                    afterBusy = () =>
                    {
                        foe.TrainerData = own;
                        AfterBattle(mayLose);
                    };
                    break;
                }
                host.Battle(foe, second, partner, mayLose, i.FirstBattle);
                afterBusy = () => AfterBattle(mayLose);
                break;
            }
            case Op.WildBattle:
            case Op.CatchingLesson:
            {
                // The catching lesson is the assistant's battle, fought with the assistant's own Pokémon
                if (i.Op == Op.WildBattle && NoPokemonToBattle()) break;
                var species = PokemonDatabase.Get(i.Name) ?? throw Wrong(i, $"there is no species '{i.Name}'");
                var kind = i.Op == Op.CatchingLesson ? BattleKind.CatchingLesson : BattleKind.Normal;
                host.WildBattle(new Pokemon(species, i.Number), kind, cannotFlee: i.Option);
                afterBusy = () => AfterBattle(mayLose: false);
                break;
            }

            case Op.Face:
            {
                var who = Person(i.Name, i);
                if (i.Direction is { } direction) host.Face(who, direction);
                else if (Toward(host.TileOf(who), host.TileOf(Person(i.Other, i))) is { } toward) host.Face(who, toward);
                break;
            }
            case Op.Walk:
            case Op.Move:
                host.Walk(Person(i.Name, i), i.Path, i.Option);
                if (i.Op == Op.Walk) waitingForWalks = true;
                break;
            case Op.WaitMoves:
                waitingForWalks = true;
                break;
            case Op.Emote:
                host.Emote(Person(i.Name, i), i.Bubble, i.Seconds);
                waiting = i.Seconds;
                break;
            case Op.Show:
            case Op.Hide:
                host.SetVisible(Person(i.Name, i)!, i.Op == Op.Show);
                break;
            case Op.Place:
                host.Place(Person(i.Name, i), i.X, i.Y, i.Direction);
                break;
            case Op.Warp:
                host.Warp(i.Name, i.X, i.Y, i.Direction);
                break;
            case Op.Fade:
                host.Fade(i.Option, i.Seconds);
                break;
            case Op.Wait:
                waiting = i.Seconds;
                break;
            case Op.Camera:
                host.Camera(i.Camera, i.X, i.Y, i.Seconds);
                // A shake goes on under whatever comes next; a pan is waited for
                if (i.Camera != CameraMove.Shake) waiting = i.Seconds;
                break;

            case Op.UseMove:
            {
                // The Pokémon chosen in the party menu, or the first of the team that knows the move (FindPartySlotWithMove)
                var move = FieldMoveRules.Of(i.Name) ?? throw Wrong(i, $"'{i.Name}' is no field move");
                var user = ownPokemon != null && ownPokemon.Moves.Any(m => m.Name == i.Name) ? ownPokemon
                    : FieldMoveRules.Knower(host.Party, move) ?? throw Wrong(i, $"nobody on the team knows {i.Name}");
                lastUser = user.Nickname;
                host.Say(null, new[] { $"{user.Nickname} used {i.Name}!" });
                afterBusy = () => host.UseMove(move, user, Subject);
                break;
            }
            case Op.Surf:
                Result = host.Surf() ? 1 : 0;
                break;
            case Op.Climb:
                Result = host.Climb() ? 1 : 0;
                break;
            case Op.Fly:
                Result = host.Fly() ? 1 : 0;
                break;
            case Op.Teleport:
                Result = host.Teleport() ? 1 : 0;
                break;
            case Op.Escape:
                Result = host.Escape() ? 1 : 0;
                break;
            case Op.Poketch:
                host.Poketch.Enabled = true;
                break;
            case Op.PoketchApp:
                host.Poketch.Register(Enum.Parse<PoketchApp>(i.Name));
                break;
            case Op.Safari:
                if (i.Option) host.Safari.Start();
                else host.Safari.End();
                break;
            case Op.SweetScent:
                // A wild Pokémon comes out where any live, and its battle is waited for
                Result = 0;
                if (host.SweetScent()) afterBusy = () => AfterBattle(mayLose: false);
                break;

            case Op.Music:
                host.Music(i.Option ? null : i.Name);
                break;
            case Op.Fanfare:
                host.Fanfare(i.Role);
                break;
            case Op.Sound:
                host.Sound(i.Name);
                break;
            case Op.Cry:
                host.Cry(i.Own ? Subject?.Species ?? throw Wrong(i, "this script belongs to no Pokémon, so it has no cry of its own") : i.Name);
                break;

            case Op.Starter:
                host.Open(ScriptScreen.Starter, Subject);
                afterBusy = () => Result = host.Answer;
                break;
            case Op.Shop:
                host.Open(ScriptScreen.Shop, Subject, i.Name.Length > 0 ? i.Name : null);
                break;
            case Op.Wardrobe:
                host.Open(ScriptScreen.Wardrobe, Subject, i.Name.Length > 0 ? i.Name : null);
                break;
            case Op.Pc:
                host.Open(i.Name == "halloffame" ? ScriptScreen.HallOfFame : ScriptScreen.Pc, Subject);
                break;
            case Op.HallOfFame:
                host.EnterHallOfFame();
                break;
            case Op.Travel:
                host.Open(ScriptScreen.Travel, Subject);
                afterBusy = () => Result = host.Answer;
                break;
            case Op.ChoosePokemon:
                host.Open(ScriptScreen.ChoosePokemon, Subject);
                afterBusy = () => Result = host.Answer;
                break;
            case Op.Trade:
                // The Pokémon chosen last (choosepokemon's RESULT) for the trade's: 1 if it was the one asked for
                Result = host.Trade(i.Name, Result) ? 1 : 0;
                break;

            // Wild Pokémon (plan 06 · R13)
            case Op.HoneyTree:
            {
                int tree = host.HoneyTreeFaced ?? throw Wrong(i, "the player faces no honey tree");
                var state = host.Encounters;
                switch (i.Name)
                {
                    case "status":
                        // GetHoneyTreeStatus: 1 bare, 2 slathered, 3 ready
                        Result = (int)HoneyTrees.Status(state.Trees[tree]);
                        break;
                    case "slather":
                        HoneyTrees.Slather(state, tree, host.TrainerNumber, host.Chance);
                        break;
                    default:
                    {
                        // StartHoneyTreeBattle: whatever came to the tree, and the honey is gone from it
                        var met = HoneyTrees.Meet(state, tree, SpecialEncounterTables.Sinnoh.HoneyTrees, WildLead.Of(host.Party), host.Chance);
                        var species = PokemonDatabase.Get(met.SpeciesName) ?? throw Wrong(i, $"there is no species '{met.SpeciesName}'");
                        host.WildBattle(new Pokemon(species, met.MinLevel, host.Chance, met.Gender, met.Nature), BattleKind.Normal, cannotFlee: false);
                        afterBusy = () => AfterBattle(mayLose: false);
                        break;
                    }
                }
                break;
            }
            case Op.Swarms:
                host.Encounters.SwarmsOn = true;
                break;
            case Op.TrophyGarden:
                TrophyGardenRules.AddNew(host.Encounters, SpecialEncounterTables.Sinnoh.TrophyGarden, host.Chance);
                break;
            case Op.Roamer:
                Roamers.SetLoose(host.Encounters, Roamers.SlotOf(i.Name) ?? throw Wrong(i, $"'{i.Name}' doesn't roam"), host.Chance);
                break;
            case Op.SurvivePoison:
            {
                // SurvivePoison: the team's Pokémon at the place the variable gives, cured if it came through
                int slot = Value(i.Name);
                Result = slot >= 0 && slot < host.Party.Count && FieldPoison.TrySurvive(host.Party.Members[slot]) ? 1 : 0;
                break;
            }
            // PressPastoriaGymButton: the button of a colour goes down, the others come up, and the water moves to
            // its height while the script waits
            case Op.WaterButton:
                host.PressWaterButton((PastoriaWater.Button)i.Number);
                break;

            // The day's events, berries and the lottery (plan 06 · R14a)
            case Op.Berry:
                Berry(i);
                break;
            case Op.ChooseItem:
                // OpenBerriesBag / OpenItemsBag, then GetSelectedItem: RESULT 1 and {item} the one picked, 0 backed out
                host.Open(ScriptScreen.ChooseItem, Subject, i.Name);
                afterBusy = () =>
                {
                    var picked = ItemDatabase.ById(host.Answer);
                    Result = picked != null ? 1 : 0;
                    if (picked != null) lastItem = picked.Name;
                };
                break;
            case Op.Lottery:
                LotteryCorner(i);
                break;

            default:
                throw Wrong(i, $"the runner doesn't know how to carry out '{i.Op}'");
        }
    }

    /// <summary>
    /// A battle the player has no Pokémon to fight is never started: the story gives the first Pokémon before any
    /// battle, and a game where that went wrong must not start one it can't play (it ended the game). A line says
    /// why and the script ends there, so whatever the battle would have led to waits for the player to come back
    /// with a Pokémon.
    /// </summary>
    private bool NoPokemonToBattle()
    {
        if (host.CanBattle) return false;
        host.Say(null, new[] { "{player} has no Pokémon that can battle!" });
        Finish();
        return true;
    }

    /// <summary>
    /// A battle is over. Its outcome is what <c>won</c>, <c>lost</c> and <c>RESULT</c> give (1 won, 0 lost, 2 fled,
    /// 3 caught); a battle lost that the script couldn't go on from ends it, as the player wakes up elsewhere.
    /// </summary>
    private void AfterBattle(bool mayLose)
    {
        lastOutcome = host.Outcome;
        Result = lastOutcome switch
        {
            BattleOutcome.Won => 1,
            BattleOutcome.Fled => 2,
            BattleOutcome.Caught => 3,
            _ => 0
        };
        if (lastOutcome == BattleOutcome.Lost && !mayLose)
        {
            Finish();
            EndedInDefeat = true;
        }
    }

    // ------------------------------------------------------------------ what a line names

    private ScriptException Wrong(Instruction at, string what) =>
        new($"{script?.File ?? "?"}.txt({at.Line}): {what}.");

    private ItemData ItemOf(Instruction i) => ItemDatabase.Get(i.Name) ?? throw Wrong(i, $"there is no item '{i.Name}'");

    /// <summary>
    /// Puts an item in the bag with its fanfare and the lines that say so (<c>give</c>, <c>find</c>, a lottery's
    /// prize): "received" or "found", what a TM or an HM holds, and the pocket it went into.
    /// </summary>
    private void Hand(ItemData item, int count, string verb, bool journal = false)
    {
        host.Bag.AddItem(item, count);
        lastItem = item.Name;
        host.Fanfare(FanfareFor(item));
        if (journal) host.Note(new JournalEvent(JournalEventKind.ItemWasObtained, item.Name));
        var lines = new List<string> { count == 1 ? $"{{player}} {verb} the {item.Name}!" : $"{{player}} {verb} {count} × {item.Name}!" };
        // A TM or an HM says what it holds
        if (!string.IsNullOrEmpty(item.TeachesMove)) lines.Add($"{item.Name} holds the move {item.TeachesMove}.");
        lines.Add($"{{player}} put {(count == 1 ? "it" : "them")} away in the {PocketName(item.Pocket)} pocket.");
        host.Say(null, lines);
    }

    /// <summary>The berry patch the script belongs to: the soft soil spoken to, or faced as the bag was used.</summary>
    private int PatchOf(Instruction i) =>
        Subject?.Patch ?? throw Wrong(i, "this script belongs to no patch of soft soil");

    /// <summary>The item a line plants or lays: the script's own (one used from the bag), or the one chosen last (<c>chooseitem</c>).</summary>
    private ItemData? Sown(Instruction i) => i.Own ? Given(i).Item : ItemDatabase.Get(lastItem);

    /// <summary>
    /// A word on the berry patch the script belongs to (plan 06 · R14a; <c>scrcmd_berry.c</c>): what grows there,
    /// how wet the soil is, planting, mulch, watering and picking. What is planted or laid comes out of the bag here.
    /// </summary>
    private void Berry(Instruction i)
    {
        int patch = PatchOf(i);
        var patches = host.Berries;
        var p = patches[patch] ?? throw Wrong(i, $"there is no berry patch {patch}");
        switch (i.Name)
        {
            case "status":
                // GetBerryGrowthStage, GetBerryItemID, GetBerryYield, GetBerryMulchType
                Result = (int)p.Stage;
                lastBerry = p.Berry ?? "";
                lastYield = p.Yield;
                lastMulch = BerryPatches.NameOf(p.Mulch) ?? "";
                break;
            case "moisture":
                Result = (int)patches.MoistureOf(patch);
                break;
            case "mulched":
                // GetBerryMulchType, asked as the soil's own question: 1 when mulch is down
                Result = p.Mulch != Mulch.None ? 1 : 0;
                lastMulch = BerryPatches.NameOf(p.Mulch) ?? "";
                break;
            case "plant":
            {
                // PlantBerry, with the RemoveItem before it: nothing happens where nothing can be planted
                Result = 0;
                if (Sown(i) is not { } berry || !BerryPatches.CanPlant(berry) || !patches.IsEmpty(patch) || !host.Bag.RemoveItem(berry, 1)) break;
                patches.Plant(patch, berry.Name);
                lastBerry = lastItem = berry.Name;
                Result = 1;
                break;
            }
            case "mulch":
            {
                // SetBerryMulch, with the RemoveItem before it
                Result = 0;
                if (Sown(i) is not { } mulch || BerryPatches.MulchOf(mulch) is var kind && kind == Mulch.None
                    || !patches.CanMulch(patch) || !host.Bag.RemoveItem(mulch, 1)) break;
                patches.LayMulch(patch, kind);
                lastMulch = lastItem = mulch.Name;
                Result = 1;
                break;
            }
            case "water":
                // The Sprayduck's watering (BerryPatches_ResetMoisture)
                Result = patches.HasBerry(patch) ? 1 : 0;
                patches.Water(patch);
                break;
            default:
            {
                // HarvestBerry: the berries into the bag, and a point on the Trainer Card
                var (berry, count) = patches.Pick(patch);
                Result = count;
                if (berry == null || count == 0) break;
                host.Bag.AddItem(ItemDatabase.Get(berry) ?? throw Wrong(i, $"there is no item '{berry}'"), count);
                host.AddScore(TrainerScore.BerryHarvested);
                lastBerry = lastItem = berry;
                lastYield = count;
                break;
            }
        }
    }

    /// <summary>
    /// A word on Jubilife TV's lottery (plan 06 · R14a; <c>scrcmd_jubilife_lottery.c</c>): the day's number against the
    /// trainer IDs of the team and the PC, whether the best match is in the PC, and its prize.
    /// </summary>
    private void LotteryCorner(Instruction i)
    {
        switch (i.Name)
        {
            case "check":
                lastLottery = Models.Lottery.Check(host.Story.Var(Models.Lottery.NumberVar), host.Party.Members, host.Stored, (int)(host.TrainerNumber & 0xffff));
                Result = lastLottery.Digits;
                break;
            case "boxed":
                Result = lastLottery.InBox ? 1 : 0;
                break;
            default:
            {
                // The prize of the last check, handed over with its fanfare as the corner's Common_GiveItemQuantity does
                Result = 0;
                if (Models.Lottery.PrizeFor(lastLottery.Digits) is not { } prize) break;
                Hand(ItemDatabase.Get(prize) ?? throw Wrong(i, $"there is no item '{prize}'"), 1, "received");
                Result = 1;
                break;
            }
        }
    }

    /// <summary>"an Oran Berry" or "3 Oran Berries": a count of berries in words.</summary>
    public static string Berries(string berry, int count)
    {
        if (berry.Length == 0) return "";
        if (count != 1) return $"{count} {(berry.EndsWith('y') ? berry[..^1] + "ies" : berry + "s")}";
        return ("AEIOU".Contains(char.ToUpperInvariant(berry[0])) ? "an " : "a ") + berry;
    }

    /// <summary>The item a line gives and how many: the one it names, or the script's own.</summary>
    private (ItemData Item, int Count) Given(Instruction i)
    {
        if (!i.Own) return (ItemOf(i), i.Number);
        var (name, count) = ownItem ?? throw Wrong(i, "this script has no item of its own: no item ball and no hidden item started it");
        return (ItemDatabase.Get(name) ?? throw Wrong(i, $"there is no item '{name}'"), count);
    }

    /// <summary>The flag that is the script's own: the one that hides whoever it belongs to, or marks a hidden item found.</summary>
    private string OwnFlag(Instruction i) =>
        ownFlag ?? throw Wrong(i, "this script has no flag of its own: nothing hides whoever it belongs to");

    /// <summary>Who a line means: null for the player.</summary>
    private NPC? Person(string who, Instruction at) => who switch
    {
        "player" => null,
        "self" => Subject ?? throw Wrong(at, "'self' is nobody here: no person started this script"),
        "pair" => Pair ?? throw Wrong(at, "'pair' is nobody here: no second trainer came with this one"),
        _ => host.FindNpc(who, place.Length > 0 ? place : null) ?? throw Wrong(at, $"nobody on this map is called '{who}'")
    };

    /// <summary>The way to turn to look from one tile at another; null when they are the same tile.</summary>
    public static Direction? Toward((int X, int Y) from, (int X, int Y) to)
    {
        int dx = to.X - from.X, dy = to.Y - from.Y;
        if (dx == 0 && dy == 0) return null;
        if (Math.Abs(dx) > Math.Abs(dy)) return dx > 0 ? Direction.Right : Direction.Left;
        return dy > 0 ? Direction.Down : Direction.Up;
    }

    /// <summary>How the bag's pockets are called in a line.</summary>
    public static string PocketName(ItemPocket pocket) => pocket switch
    {
        ItemPocket.Medicine => "Medicine",
        ItemPocket.PokeBalls => "Poké Balls",
        ItemPocket.TMsAndHMs => "TMs & HMs",
        ItemPocket.KeyItems => "Key Items",
        ItemPocket.Berries => "Berries",
        ItemPocket.Mail => "Mail",
        ItemPocket.BattleItems => "Battle Items",
        _ => "Items"
    };

    // ------------------------------------------------------------------ conditions

    private int Value(string variable) => variable switch
    {
        "RESULT" => Result,
        "PLAYER_X" => host.TileOf(null).X,
        "PLAYER_Y" => host.TileOf(null).Y,
        "MONEY" => host.Money,
        "PARTY_COUNT" => host.Party.Count,
        "BADGE_COUNT" => host.Story.BadgeCount,
        _ => host.Story.Var(variable)
    };

    private bool Holds(Condition c, Instruction at)
    {
        var story = host.Story;
        bool yes = c.Query switch
        {
            Query.Flag => story.Has(c.Name),
            Query.Var => Condition.Holds(Value(c.Name), c.Compare, c.Other != null ? Value(c.Other) : c.Number),
            Query.Badge => story.HasBadge(Enum.Parse<Badge>(c.Name)),
            Query.Badges => Condition.Holds(story.BadgeCount, c.Compare, c.Number),
            Query.Item => Condition.Holds(ItemDatabase.Get(c.Name) is { } item ? host.Bag.GetQuantity(item) : 0, c.Compare, c.Number),
            Query.Party => Condition.Holds(host.Party.Count, c.Compare, c.Number),
            Query.Knows => host.Party.Members.Any(p => p.Moves.Any(m => m.Name == c.Name)),
            Query.Has => host.Party.Members.Any(p => p.Species.Name == c.Name),
            Query.Yes => Result == 1,
            Query.No => Result == 0,
            Query.Won => lastOutcome == BattleOutcome.Won,
            Query.Lost => lastOutcome == BattleOutcome.Lost,
            Query.Result => Condition.Holds(Result, c.Compare, c.Number),
            Query.Defeated => c.Name == "self" ? SubjectDefeated(at) : story.HasDefeated(c.Name),
            Query.Rematch => (Subject ?? throw Wrong(at, "'rematch self' needs someone the script belongs to")).ReadyForRematch,
            Query.Taken => story.HasTaken(c.Name),
            Query.Starter => story.PlayerStarter == c.Name,
            Query.Money => Condition.Holds(host.Money, c.Compare, c.Number),
            Query.Facing => host.FacingOf(null).ToString() == c.Name,
            Query.Boy => host.PlayerLook == PlayerLook.Boy,
            Query.Girl => host.PlayerLook == PlayerLook.Girl,
            Query.Poketch => host.Poketch.Enabled,
            Query.Safari => host.Safari.Active,
            Query.Partner => host.Partner != null,
            // ScrCmd_CheckPartyPokerus: one of the team carries it or has had it
            Query.Pokerus => host.Party.Members.Any(p => p.Pokerus != 0),
            // GetDayOfWeek (plan 06 · R14a): the Valley Windworks' Drifloon comes on Fridays
            Query.Weekday => host.Today.DayOfWeek.ToString() == c.Name,
            // CheckPocketHasItems
            Query.Pocket => host.Bag.GetPocketItems(Enum.Parse<ItemPocket>(c.Name)).Count > 0,
            _ => throw Wrong(at, $"the runner can't answer '{c.Query}'")
        };
        return yes != c.Negated;
    }

    /// <summary>Whether whoever the script belongs to has been beaten: here and now, or in a game saved since.</summary>
    private bool SubjectDefeated(Instruction at)
    {
        var self = Subject ?? throw Wrong(at, "'defeated self' needs someone the script belongs to");
        return self.HasBattled || (self.TrainerData is { Id.Length: > 0 } trainer && host.Story.HasDefeated(trainer.Id));
    }

    // ------------------------------------------------------------------ text

    private static readonly Regex Placeholder = new(@"\{([a-z]+)(?::([A-Z0-9_]+))?\}", RegexOptions.Compiled);

    /// <summary>
    /// Fills in what a line leaves to the moment: <c>{var:NAME}</c>, <c>{lead}</c> (the first Pokémon of the team),
    /// <c>{starter}</c>, <c>{rivalstarter}</c> and <c>{assistantstarter}</c> (the one neither took), <c>{self}</c>, <c>{item}</c> (the last one given or taken),
    /// <c>{user}</c> (the Pokémon the last <c>usemove</c> named), <c>{money}</c> and <c>{result}</c>. <c>{player}</c>, <c>{assistant}</c> and <c>{rival}</c> are left for the host, which
    /// knows who the player is.
    /// </summary>
    public string Fill(string text) => text.Contains('{') ? Placeholder.Replace(text, match =>
    {
        string argument = match.Groups[2].Value;
        return match.Groups[1].Value switch
        {
            "var" when argument.Length > 0 => Value(argument).ToString(),
            "lead" => host.Party.Members.FirstOrDefault()?.Nickname ?? "your Pokémon",
            "starter" => host.Story.PlayerStarter ?? "",
            "rivalstarter" => host.Story.RivalStarter ?? "",
            "assistantstarter" => host.Story.AssistantStarter ?? "",
            "self" => Subject?.Name ?? "",
            "item" => lastItem,
            "user" => lastUser,
            "money" => host.Money.ToString(),
            "result" => Result.ToString(),
            // The team's Pokémon at the place a variable gives (poison's survivors)
            "member" when argument.Length > 0 => Value(argument) is var slot && slot >= 0 && slot < host.Party.Count ? host.Party.Members[slot].Nickname : "",
            // The day's swarm and where it is, and the Trophy Garden's newest (plan 06 · R13)
            "swarm" => Swarms.Species(Swarms.AreaOf(host.Encounters.SwarmDaily)),
            "swarmplace" => Swarms.PlaceName(Swarms.AreaOf(host.Encounters.SwarmDaily)),
            "trophygarden" => TrophyGardenRules.SpeciesIn(host.Encounters.TrophyFirst, SpecialEncounterTables.Sinnoh.TrophyGarden) ?? "",
            // A berry patch's last word (plan 06 · R14a): its berry, its berries counted ("an Oran Berry", "3 Oran
            // Berries"), its mulch; the lottery's number of the day in five digits, and the Pokémon its last check matched
            "berry" => lastBerry,
            "berries" => Berries(lastBerry, lastYield),
            "mulch" => lastMulch,
            "lottery" => host.Story.Var(Models.Lottery.NumberVar).ToString("D5"),
            "lotterymon" => lastLottery.Pokemon?.Nickname ?? "",
            _ => match.Value
        };
    }) : text;

    /// <summary>The fanfare an item is received to, as the original's: a TM or HM its own, a key item its own, anything else the item's.</summary>
    public static MusicRole FanfareFor(ItemData item) => item.Pocket switch
    {
        ItemPocket.TMsAndHMs => MusicRole.FanfareTM,
        ItemPocket.KeyItems => MusicRole.FanfareKeyItem,
        _ => MusicRole.FanfareItem
    };
}
