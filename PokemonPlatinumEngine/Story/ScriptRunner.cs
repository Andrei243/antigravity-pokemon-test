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

    // The last lottery drawn, and the day's ticket (plan 06 · R14a)
    private Models.Lottery.Draw lottery;
    private int Ticket => Models.Lottery.TicketOf(host.Encounters.DailyNumber);

    // What the patch of soil the script belongs to holds, as berry status and berry mulched last read it (plan 06 · R14a)
    private string berryName = "", mulchName = "";
    private int berryYield;
    private (string Item, int Count)? ownItem;
    private string? ownFlag;
    private Pokemon? ownPokemon;
    private string lastUser = "";

    // The Pokémon last left at the Day Care and last taken back (plan 06 · R15)
    private string lastLeft = "", lastTaken = "";

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
                host.Bag.AddItem(item, count);
                lastItem = item.Name;
                host.Fanfare(FanfareFor(item));
                // A ball picked up off the ground is a line of the Journal (scripts_visible_items.s); a gift or a hidden find isn't
                if (i.Op == Op.Find && Subject?.IsItemBall == true) host.Note(new JournalEvent(JournalEventKind.ItemWasObtained, item.Name));
                string verb = i.Op == Op.Find ? "found" : "received";
                var lines = new List<string> { count == 1 ? $"{{player}} {verb} the {item.Name}!" : $"{{player}} {verb} {count} × {item.Name}!" };
                // A TM or an HM says what it holds
                if (!string.IsNullOrEmpty(item.TeachesMove)) lines.Add($"{item.Name} holds the move {item.TeachesMove}.");
                lines.Add($"{{player}} put {(count == 1 ? "it" : "them")} away in the {PocketName(item.Pocket)} pocket.");
                host.Say(null, lines);
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
            case Op.ClearGreetings:
                host.Story.ClearGreetings();
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
            // PressPastoriaGymButton: the Pastoria Gym's water sets off for the level of a button's colour
            case Op.PressButton:
                host.PressButton(i.Name switch { "blue" => PastoriaWater.Button.Blue, "orange" => PastoriaWater.Button.Orange, _ => PastoriaWater.Button.Green });
                break;
            // PressSunyshoreGymButton: the Sunyshore Gym's gears turn on a quarter, back a quarter or on a half
            case Op.GearButton:
                host.PressGearButton(i.Name switch { "reverse" => SunyshoreGears.Button.Reverse, "double" => SunyshoreGears.Button.Double, _ => SunyshoreGears.Button.Normal });
                break;
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

            // Berry patches (plan 06 · R14a; scrcmd_berry.c)
            case Op.Berry:
                Berry(i);
                break;
            case Op.ChooseItem:
                host.Open(ScriptScreen.ChooseItem, Subject, i.Name);
                afterBusy = () =>
                {
                    Result = host.Answer;
                    if (host.ChosenItem is { } chosen) lastItem = chosen;
                };
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

            // The Lottery Corner (plan 06 · R14a): RESULT is the digits the day's ticket matched, or whether the
            // Pokémon that matched them is in the boxes
            case Op.Lottery:
                if (i.Name == "draw")
                {
                    lottery = Models.Lottery.Check(Ticket, (int)(host.TrainerNumber & 0xffff), host.Party.Members, host.Boxed);
                    Result = lottery.Digits;
                }
                else Result = lottery.InBox ? 1 : 0;
                break;

            // Poffins (plan 06 · R14c)
            case Op.Poffin:
                Poffin(i);
                break;

            // The Day Care and Eggs (plan 06 · R15)
            case Op.DayCare:
                DayCare(i);
                break;
            case Op.GiveEgg:
            {
                // ScrCmd_GiveEgg: an Egg onto the team, received from whoever gives it; nothing when the team is full
                var species = PokemonDatabase.Get(i.Name) ?? throw Wrong(i, $"there is no species '{i.Name}'");
                if (host.Party.IsFull)
                {
                    Result = 0;
                    break;
                }
                var egg = Breeding.GiftEgg(species, host.TrainerNumber, host.Chance, Ruleset.Current);
                egg.Met(PlayerIdentity.Fill(speaker ?? Subject?.Name ?? "a stranger"), Core.GameClock.Today);
                egg.MetLevel = 0;
                host.Party.Add(egg);
                Result = 1;
                break;
            }
            case Op.Hatch:
                // HatchEgg: the team's first Egg with no cycles left hatches in its scene, which the script waits for
                Result = host.Hatch() ? 1 : 0;
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
    /// <c>berry ...</c>: what the patch of soil the script belongs to holds, and what is done to it. <c>status</c> is
    /// its stage (0 bare to 5 in fruit) and fills <c>{berry}</c> and <c>{yield}</c>; <c>mulched</c> 1 when mulch is laid,
    /// filling <c>{mulch}</c>; <c>water</c>; <c>plant</c> and <c>mulch</c> the item <c>chooseitem</c> chose, taken from
    /// the bag; <c>pick</c> the berries into the bag, their number in RESULT; <c>berries</c> and <c>mulches</c> 1 when
    /// the bag has a berry that grows, or a mulch.
    /// </summary>
    /// <summary>
    /// The Poffin House's commands (plan 06 · R14c): <c>check</c> is the original's <c>CheckCanCookPoffin</c> (RESULT 1
    /// with no berry in the bag, 2 with the case full, else 0); <c>cook</c> opens the cooking, berry by berry, until the
    /// player stops; <c>room</c> is <c>CheckHasEmptyPoffinCaseSlot</c> (RESULT 1 when there is room); <c>give</c> is
    /// <c>GivePoffin</c>: a Poffin of those flavours and that smoothness into the case, RESULT its kind, or 65,535
    /// (<c>POFFIN_NONE</c>) when the case was full.
    /// </summary>
    private void Poffin(Instruction i)
    {
        var poffins = host.Poffins;
        switch (i.Name)
        {
            case "check":
                Result = !host.Bag.AllItems.Any(s => s.Quantity > 0 && s.Data.Pocket == ItemPocket.Berries) ? 1 : poffins.IsFull ? 2 : 0;
                break;
            case "cook":
                host.Open(ScriptScreen.PoffinCooking, Subject);
                break;
            case "room":
                Result = poffins.IsFull ? 0 : 1;
                break;
            case "give":
            {
                var made = Models.Poffins.Make(i.Numbers.Take(5).ToList(), i.Numbers[5], false, host.Chance);
                Result = poffins.Add(made) ? (int)made.Type : 0xFFFF;
                break;
            }
        }
    }

    /// <summary>
    /// The Day Care's commands (plan 06 · R15), as the original's day-care script commands: <c>state</c> is
    /// <c>GetDaycareState</c> (0 empty, 1 an Egg waiting, 2 one Pokémon, 3 two); <c>check</c> whether the player may
    /// leave one at all (1 with only one Pokémon that isn't an Egg, <c>CountPartyNonEggs</c>; 2 when the team's Pokémon
    /// able to fight and the boxes' come to two, <c>CountAliveMonsAndBoxMons</c>; else 0); <c>leave</c> the Pokémon last
    /// chosen with <c>choosepokemon</c> (0 left, crying, and <c>{left}</c> names it; 1 none chosen; 2 an Egg; 3 the last
    /// that can fight; 4 the Day Care is full); <c>take N</c> the Pokémon in place N back for its fee (0 taken, crying,
    /// <c>{taken}</c> names it; 1 the player is short of the fee; 2 the team is full; 3 nobody there); <c>grown N</c> the
    /// levels the Pokémon in place N has grown (<c>BufferDaycareGainedLevelsBySlot</c>); <c>egg</c> the Egg
    /// onto the team (1, or 0 with none or no room); <c>keep</c> the Egg turned down; <c>compatibility</c> how the two get
    /// on, 0 best to 3 not at all. <c>{daycare:N}</c>, <c>{level:N}</c>, <c>{grown:N}</c> and <c>{fee:N}</c> name the
    /// Pokémon in place N, the level its steps have brought it to, the levels it grew and what taking it back costs.
    /// </summary>
    private void DayCare(Instruction i)
    {
        var dayCare = host.DayCare;
        var party = host.Party;
        switch (i.Name)
        {
            case "state":
                Result = (int)dayCare.State;
                break;
            case "check":
            {
                int notEggs = party.Members.Count(p => !p.IsEgg);
                int able = party.Members.Count(p => !p.IsFainted) + host.Boxed.Count(p => !p.IsEgg);
                Result = notEggs == 1 ? 1 : able == 2 ? 2 : 0;
                break;
            }
            case "leave":
            {
                int slot = Result;
                if (slot < 0 || slot >= party.Count) Result = 1;
                else if (party.Members[slot].IsEgg) Result = 2;
                else if (Models.DayCare.WhyNot(party, slot) == "last") Result = 3;
                else if (dayCare.Count >= Models.DayCare.Places) Result = 4;
                else
                {
                    var left = party.Members[slot];
                    lastLeft = left.DisplayName;
                    dayCare.Leave(party, slot);
                    host.Cry(left.ModelName);
                    Result = 0;
                }
                break;
            }
            case "take":
            {
                if (dayCare[i.Number] == null) Result = 3;
                else if (party.IsFull) Result = 2;
                else if (host.Money < dayCare.Fee(i.Number)) Result = 1;
                else
                {
                    host.Money -= dayCare.Fee(i.Number);
                    var taken = dayCare.TakeBack(party, i.Number)!;
                    lastTaken = taken.DisplayName;
                    host.Cry(taken.ModelName);
                    Result = 0;
                }
                break;
            }
            case "egg":
            {
                var egg = dayCare.GiveEgg(party, host.TrainerNumber, host.Chance, Ruleset.Current);
                if (egg != null)
                {
                    egg.Met(Models.Breeding.DayCareCouple, Core.GameClock.Today);
                    egg.MetLevel = 0;
                }
                Result = egg != null ? 1 : 0;
                break;
            }
            case "grown":
                Result = dayCare.LevelsGained(i.Number);
                break;
            case "keep":
                dayCare.KeepEgg();
                break;
            case "compatibility":
                Result = Breeding.Level(dayCare.Compatibility);
                break;
        }
    }

    private void Berry(Instruction i)
    {
        var patches = host.Berries;
        if (i.Name == "berries" || i.Name == "mulches")
        {
            string what = i.Name == "berries" ? "berries" : "mulch";
            Result = host.Bag.AllItems.Any(s => s.Quantity > 0 && UI.BagScreen.Fits(what, s.Data)) ? 1 : 0;
            return;
        }
        int id = Subject?.BerryPatch ?? throw Wrong(i, "this script belongs to no patch of soil");
        var patch = patches[id];
        switch (i.Name)
        {
            case "status":
                Result = (int)patch.Stage;
                berryName = patch.Berry ?? "";
                berryYield = patch.Yield;
                break;
            case "mulched":
                Result = patch.Mulch != Mulch.None ? 1 : 0;
                mulchName = BerryPatches.NameOf(patch.Mulch) ?? "";
                break;
            case "water":
                patches.Water(id);
                break;
            case "plant":
            {
                var berry = ItemDatabase.Get(lastItem);
                if (berry?.Berry == null) throw Wrong(i, $"'{lastItem}' is no berry that grows");
                if (patch.Stage != BerryStage.None) throw Wrong(i, "something grows here already");
                host.Bag.RemoveItem(berry, 1);
                patches.Plant(id, berry.Name);
                berryName = berry.Name;
                break;
            }
            case "mulch":
            {
                var mulch = BerryPatches.MulchOf(lastItem);
                if (mulch == Mulch.None) throw Wrong(i, $"'{lastItem}' is no mulch");
                host.Bag.RemoveItem(ItemDatabase.Get(lastItem)!, 1);
                patches.LayMulch(id, mulch);
                mulchName = lastItem;
                break;
            }
            case "pick":
            {
                var (berry, count) = patches.Pick(id);
                Result = 0;
                if (berry == null || count <= 0 || ItemDatabase.Get(berry) is not { } item) break;
                host.Bag.AddItem(item, count);
                lastItem = item.Name;
                berryName = item.Name;
                berryYield = count;
                Result = count;
                break;
            }
        }
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
        "GREETINGS" => host.Story.Greetings,
        "SEEN" => host.SeenInSinnoh,
        // The friendship of the first of the team (the original's GetFirstNonEggInParty and GetPartyMonFriendship)
        "LEAD_FRIENDSHIP" => host.Party.Count > 0 ? host.Party.Members[0].Friendship : 0,
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
            // ScrCmd_CheckPoketchAppRegistered
            Query.PoketchApp => host.Poketch.Has(Enum.Parse<PoketchApp>(c.Name)),
            Query.Safari => host.Safari.Active,
            Query.Partner => host.Partner != null,
            // ScrCmd_CheckPartyPokerus: one of the team carries it or has had it
            Query.Pokerus => host.Party.Members.Any(p => p.Pokerus != 0),
            // GetTimeOfDay and GetDayOfWeek (plan 06 · R14a)
            Query.Time => host.TimeOfDay.ToString() == c.Name,
            Query.Weekday => host.Weekday.ToString() == c.Name,
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
            "berry" => berryName,
            "yield" => berryYield.ToString(),
            "mulch" => mulchName,
            "user" => lastUser,
            "money" => host.Money.ToString(),
            "result" => Result.ToString(),
            // The team's Pokémon at the place a variable gives (poison's survivors)
            "member" when argument.Length > 0 => Value(argument) is var slot && slot >= 0 && slot < host.Party.Count ? host.Party.Members[slot].Nickname : "",
            // The day's swarm and where it is, and the Trophy Garden's newest (plan 06 · R13)
            "swarm" => Swarms.Species(Swarms.AreaOf(host.Encounters.SwarmDaily)),
            "swarmplace" => Swarms.PlaceName(Swarms.AreaOf(host.Encounters.SwarmDaily)),
            "trophygarden" => TrophyGardenRules.SpeciesIn(host.Encounters.TrophyFirst, SpecialEncounterTables.Sinnoh.TrophyGarden) ?? "",
            // The day's lottery ticket, and whose Pokémon matched it (plan 06 · R14a)
            "ticket" => Ticket.ToString("D5"),
            "winner" => lottery.Winner?.DisplayName ?? "",
            // The Day Care's two by their places, and who was last left or taken back (plan 06 · R15)
            "daycare" when DayCarePlace(argument) is { } at => host.DayCare[at]?.DisplayName ?? "",
            "level" when DayCarePlace(argument) is { } at => host.DayCare.LevelNow(at).ToString(),
            "grown" when DayCarePlace(argument) is { } at => host.DayCare.LevelsGained(at).ToString(),
            "fee" when DayCarePlace(argument) is { } at => host.DayCare.Fee(at).ToString(),
            "left" => lastLeft,
            "taken" => lastTaken,
            _ => match.Value
        };
    }) : text;

    private static int? DayCarePlace(string argument) => argument is "0" or "1" ? argument[0] - '0' : null;

    /// <summary>The fanfare an item is received to, as the original's: a TM or HM its own, a key item its own, anything else the item's.</summary>
    public static MusicRole FanfareFor(ItemData item) => item.Pocket switch
    {
        ItemPocket.TMsAndHMs => MusicRole.FanfareTM,
        ItemPocket.KeyItems => MusicRole.FanfareKeyItem,
        _ => MusicRole.FanfareItem
    };
}
