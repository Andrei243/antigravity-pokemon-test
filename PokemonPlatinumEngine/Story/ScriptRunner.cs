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
public sealed partial class ScriptRunner
{
    /// <summary>The lines a script may carry out in one go before it is taken to be going round in circles.</summary>
    public const int MostLinesAtOnce = 5000;

    /// <summary>How deep scripts may call one another.</summary>
    public const int MostCalls = 32;

    /// <summary>The answers of a plain question, in the order they are shown: the first is yes.</summary>
    public static string[] YesNo => new[] { Text.Of("scriptLines.yes"), Text.Of("scriptLines.no") };

    private readonly ScriptLibrary library;
    private readonly IScriptHost host;
    private readonly Stack<(Script Script, int At)> calls = new();

    private Script? script;
    private string place = "";
    private int at;
    private IReadOnlyList<string> ownLines = Array.Empty<string>();
    private string? speaker;

    // The speaker as the chosen language writes the name; `speaker` stays the identifier a save may keep (an Egg's met-by)
    private string? speakerShown;

    // Where the lines `sayown` says are found in the translation files (plan 17 · N4): a signboard's, else the subject's own
    private (string Place, string Key)? ownSign;
    private bool ownFromSubject;
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

    /// <summary>The Pokémon of the team <c>choosepokemon</c> chose last, which <c>{chosen}</c> names and <c>master</c> sends out (plan 14 · B14).</summary>
    private Pokemon? chosenMember;

    /// <summary>What <c>choosepokemon</c> leaves in RESULT when none was chosen (the party screen's own number).</summary>
    public const int NoneChosen = 255;

    /// <summary>Whether a Pokémon is one <c>choosepokemon "Species"</c> lets be chosen: of that species, and no Egg.</summary>
    public static bool Fits(Pokemon pokemon, string species) =>
        !pokemon.IsEgg && string.Equals(pokemon.Species.Name, species, StringComparison.OrdinalIgnoreCase);

    // The contest being entered and the Pokémon chosen for it, and the accessory last given (plan 06 · R17a)
    private Models.SuperContests.ContestRank contestRank;
    private ContestStat contestCategory;
    private Models.SuperContests.ContestMode contestMode;
    private int contestSlot = -1;
    private string lastAccessory = "";

    // The Pokémon last measured beside the size record, its size and the record's as they are told (plan 01 · M11, part c)
    private Pokemon? measured;
    private string lastSize = "", lastRecord = "";

    // The Seal and the ribbon last given or asked about, how many Seals, and whom a ribbon was for (plan 06 · R17b)
    private string lastSeal = "", lastRibbon = "", lastWearer = "";
    private int lastSeals;

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
    /// <param name="sign">Where a signboard's lines are found in the translation files: the place and the sign's id there.</param>
    public void Start(Script start, NPC? subject = null, IReadOnlyList<string>? own = null, (string Item, int Count)? item = null, string? flag = null,
        Pokemon? pokemon = null, NPC? pair = null, (string Place, string Key)? sign = null)
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
        ownSign = own != null ? sign : null;
        ownFromSubject = own == null && subject != null;
        speaker = subject?.Name;
        speakerShown = ShownNameOf(subject);
        waiting = 0f;
        waitingForWalks = false;
        afterBusy = null;
        lastOutcome = BattleOutcome.None;
        Result = 0;
        chosenMember = null;
        EndedInDefeat = false;
        turns = 0;
        // (A script started over another's leaves no scene of that one open)
        EndTheScene(seen: false);
        stoppedAt = null;
    }

    /// <summary>The turns of every battle this script has fought, added up (<c>TURNS</c>; plan 01 · M11, part b).</summary>
    private int turns;

    /// <summary>
    /// Whoever is battled again once beaten (plan 01 · M11, part b: the Seven Stars Restaurant's couples on another
    /// day) brings their team afresh from Platinum's data, as the original builds a trainer's team for every battle.
    /// </summary>
    private static void Refresh(NPC? who)
    {
        if (who?.TrainerData is { Party.Count: > 0 } trainer && !trainer.Party.HasUsablePokemon && TrainerDatabase.Get(trainer.Id) is { } record)
            TrainerDatabase.Fill(trainer, record);
    }

    /// <summary>Stops the script where it is.</summary>
    public void Abort() => Finish();

    /// <summary>Carries the script on as far as it goes without the player.</summary>
    public void Update(float dt)
    {
        if (script == null) return;
        if (waiting > 0f && !Hurrying)
        {
            waiting -= dt;
            if (waiting > 0f) return;
            waiting = 0f;
        }

        int budget = MostLinesAtOnce;
        while (script != null)
        {
            if (host.Busy)
            {
                // Something has to be seen through after all: the scene goes on at its own pace from here
                if (Hurrying) StopHurrying();
                return;
            }
            if (afterBusy is { } collect)
            {
                afterBusy = null;
                collect();
                continue;
            }
            // Whoever holds the button the whole time (a test, a tool) hurries on again after each thing that waited
            // for them, but never into the line the hurry has just stopped at
            if (Impatient && scene != null && !Hurrying && stoppedAt == null) Hurry();
            if (Hurrying)
            {
                // In a hurry nothing is waited for: a pause, a camera's move, an effect, a set piece. Their time
                // passes all the same for whoever is walking, and where the scene waits for its walks they arrive
                if (waiting > 0f) host.Elapse(waiting);
                waiting = 0f;
                if (waitingForWalks) host.Arrive();
                waitingForWalks = false;
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
            var next = script.Code[at];
            if (Hurrying && StopsNow(next))
            {
                // What waits for the player is never hurried into: the hurry ends before it, and the line is carried
                // out at the scene's own pace (once the host is ready for it: a skip's dark has opened)
                StopHurrying();
                stoppedAt = next;
                continue;
            }
            if (ReferenceEquals(next, stoppedAt)) stoppedAt = null;
            at++;
            Execute(next);
        }
    }

    // ------------------------------------------------------------------ scenes and the hurry (plan 12 · Q20)

    // The scene the script is in: its key, and how deep the calls were when it began
    private string? scene;
    private int sceneDepth;

    // The line a hurry stopped before, until it has been carried out
    private Instruction? stoppedAt;

    /// <summary>
    /// The key of the scene the script is in, between its <c>scene "Name"</c> and its <c>endscene</c>
    /// (<see cref="SceneSkip.Key"/>); null outside one. Only a scene can be hurried through.
    /// </summary>
    public string? Scene => scene;

    /// <summary>
    /// True while the runner hurries through a scene the player is skipping: it waits for nothing (a pause, a walk, a
    /// camera's move), the host ends at once or drops whatever would be shown (<see cref="IScriptHost.Skipping"/>),
    /// and every line still does to the story, the bag, the team and the map what it does when played.
    /// </summary>
    public bool Hurrying { get; private set; }

    /// <summary>
    /// A player who keeps the button down the whole time: every scene is hurried through from its first line, and
    /// again after each thing that stopped the hurry. How a test plays a script both ways, and a tool that has no time.
    /// </summary>
    public bool Impatient { get; set; }

    /// <summary>
    /// Puts the runner in a hurry until the scene's <c>endscene</c>, or until something that waits for the player (a
    /// question, a battle, a screen, a save: <see cref="StopsAHurry"/>), which is then carried out at the scene's own
    /// pace; hurrying again afterwards skips on. False, with nothing changed, outside a scene.
    /// </summary>
    public bool Hurry()
    {
        if (script == null || scene == null) return false;
        if (Hurrying) return true;
        Hurrying = true;
        host.BeginSkipping();
        return true;
    }

    private void StopHurrying()
    {
        if (!Hurrying) return;
        Hurrying = false;
        host.EndSkipping();
    }

    /// <summary>
    /// The scene ends: at its <c>endscene</c>, which is what makes it seen (by this game and by the host's machine),
    /// or without one, when the script that began it ends or returns (an <c>end</c> on a way out, a battle lost, a
    /// script gone wrong), which leaves it unseen. Either way a hurry ends with it.
    /// </summary>
    private void EndTheScene(bool seen)
    {
        StopHurrying();
        if (scene is not { } key) return;
        scene = null;
        if (seen) host.Story.SeeScene(key);
        host.SceneEnds(key, seen);
    }

    private bool StopsNow(Instruction next) =>
        next.Op == Op.If ? Holds(next.Condition!, next) && StopsAHurry(next.Then!) : StopsAHurry(next);

    /// <summary>
    /// Whether a line stops a hurry: true for everything that waits for the player or that the host can't end at
    /// once (a question, a battle, a screen, a save, a field move, a puzzle's pieces on the move), false for what is
    /// only said, shown or heard (the host drops it or ends it at once) and for what changes the story, the bag, the
    /// team or the map (which happens as ever). Every command says which it is: one added to the language is told of
    /// here, or this throws for it.
    /// </summary>
    public static bool StopsAHurry(Instruction i) => i.Op switch
    {
        // What is said: dropped unread
        Op.Say or Op.Text or Op.SayOwn or Op.TrainerLine or Op.Speaker => false,
        // A question waits for its answer
        Op.Ask or Op.Choose => true,
        // Where the script goes, and what the story remembers
        Op.If or Op.Goto or Op.Call or Op.Return or Op.End => false,
        Op.SetFlag or Op.ClearFlag or Op.SetVar or Op.AddVar or Op.ClearGreetings => false,
        // What is given and taken: the fanfare and the lines are dropped, the bag and the team are as ever
        Op.Give or Op.Find or Op.AddItem or Op.Take or Op.GivePokemon or Op.GiveBadge or Op.GiveMoney or Op.TakeMoney or Op.Heal => false,
        Op.GiveAccessory or Op.GiveEgg or Op.Seen or Op.NationalDex or Op.Trade => false,
        // A battle is fought
        Op.Battle or Op.WildBattle or Op.CatchingLesson or Op.SweetScent or Op.SmashEncounter or Op.Master => true,
        // People and the field: a walk ends where it would have, a warp arrives, a fade and a camera's move are over at once
        Op.Face or Op.Walk or Op.Move or Op.WaitMoves or Op.Emote or Op.Act or Op.Mood or Op.Show or Op.Hide or Op.Place or Op.Warp or Op.Fade or Op.Wait => false,
        Op.Camera or Op.Letterbox or Op.SlowMotion or Op.Effect or Op.Anim or Op.Shake or Op.Flicker => false,
        // A field move's cut-in and what it does, a step out onto the water or up a face, a flight: the player's own to see
        Op.UseMove or Op.Surf or Op.Climb or Op.Fly or Op.Teleport or Op.Escape or Op.Dive => true,
        Op.Poketch or Op.PoketchApp or Op.Safari or Op.Turnback or Op.Partner or Op.Defeat or Op.Bicycle => false,
        // A puzzle's pieces move while the field waits
        Op.FlowerClock or Op.PressButton or Op.GearButton => true,
        Op.PlatformLift => !i.Option,
        // Sound: a change of music is kept, a fanfare, a sound and a cry are dropped
        Op.Music or Op.Fanfare or Op.Sound or Op.Cry => false,
        // The screens
        Op.Starter or Op.Shop or Op.Pc or Op.Travel or Op.Wardrobe or Op.ChoosePokemon or Op.ChooseItem or Op.Credits or Op.Hatch => true,
        Op.HallOfFame => i.Name == "show",
        // The game written to its save is the player's to see
        Op.Save => true,
        // Wild Pokémon, the day's events, the berries, the team: asked and changed without a screen, but a tree's battle
        Op.HoneyTree => i.Name == "battle",
        Op.Swarms or Op.TrophyGarden or Op.Roamer or Op.SurvivePoison or Op.Lottery or Op.Berry or Op.DayCare or Op.Ribbon or Op.Team or Op.Random => false,
        Op.Poffin => i.Name == "cook",
        Op.SuperContest => i.Name is "enter" or "photo",
        Op.Seal => i.Name is "capsules" or "shop",
        // Below ground only what is handed over or counted needs no screen and no tunnel
        Op.Underground => i.Name is not ("give" or "room" or "flags"),
        // The pastimes, the Battle Frontier's facilities, the tutors and Amity Square's walk open screens, ask and
        // battle on most of their words: none of them is hurried through
        Op.Fossil or Op.GameCorner or Op.Villa or Op.PalPark => true,
        Op.Frontier or Op.Tower or Op.Factory or Op.Hall or Op.Castle or Op.Arcade => true,
        Op.Tutor or Op.Amity => true,
        // A scene's own two lines
        Op.Scene or Op.EndScene => false,
        // A number registered: the flag is set and the notice dropped
        Op.AddContact => false,
        // A panel of the Ruins of Alph is the player's own to put together
        Op.Puzzle => true,
        // A television's programme is only said (its die is drawn all the same); a map on a wall is the player's to look at
        Op.Broadcast => false,
        Op.TownMap => true,
        _ => throw new ArgumentOutOfRangeException(nameof(i), i.Op, "This command doesn't say what it does in a hurry: name it in ScriptRunner.StopsAHurry.")
    };

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
        // A scene still open ends with its script, unseen (plan 12 · Q20)
        EndTheScene(seen: false);
        stoppedAt = null;
    }

    private void Return()
    {
        if (calls.Count == 0)
        {
            Finish();
            return;
        }
        (script, at) = calls.Pop();
        // A scene begun by a script that was called ends as that script returns, unseen
        if (scene != null && calls.Count < sceneDepth) EndTheScene(seen: false);
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
                var lines = new List<string>(Spoken(i));
                while (at < script!.Code.Count && script.Code[at].Op == i.Op) lines.AddRange(Spoken(script.Code[at++]));
                host.Say(i.Op == Op.Say ? speakerShown : null, lines.Select(Fill).ToList());
                break;
            }
            case Op.SayOwn:
                if (ownLines.Count > 0) host.Say(speakerShown, OwnSpoken().Select(Fill).ToList());
                break;
            case Op.TrainerLine:
            {
                var whose = i.Name.Length > 0 ? Person(i.Name, i)! : Subject ?? throw Wrong(i, "'trainerline' needs someone the script belongs to");
                var trainer = whose.TrainerData ?? throw Wrong(i, $"{whose.Name} is no trainer and has no trainer's lines");
                string line = i.Option ? trainer.DialogueAfter : trainer.DialogueBefore;
                Result = string.IsNullOrEmpty(line) ? 0 : 1;
                // A line written "Youngster Tristan: ..." is said under that whole title, as the text box shows who speaks;
                // a translated line begins with the title as its language says it ("Puști Tristan: ...", plan 17 · N6)
                string said = Text.PersonLine(whose.LinesPlace, whose.LinesKey, i.Option ? PersonField.After : PersonField.Before, 0, new[] { line });
                string? title = said.StartsWith(trainer.FullTitle + ": ", StringComparison.Ordinal) || line.StartsWith(trainer.FullTitle + ": ", StringComparison.Ordinal)
                    ? trainer.FullTitle : whose == Subject ? speakerShown : ShownNameOf(whose);
                if (Result == 1)
                    host.Say(title, new[] { Fill(said) });
                break;
            }
            case Op.Speaker:
                speaker = i.Option ? Subject?.Name : i.Name.Length > 0 ? i.Name : null;
                speakerShown = i.Option ? ShownNameOf(Subject) : i.Name.Length > 0 ? Text.Line(script!.FullName, i.TextIndex, 0, i.Words) : null;
                break;
            case Op.Ask:
                host.Ask(speakerShown, Fill(Spoken(i)[0]), YesNo, cancel: 1);
                afterBusy = () => Result = host.Answer == 0 ? 1 : 0;
                break;
            case Op.Choose:
                // Backing out of a menu picks its last answer, which is where a script puts its way out
                host.Ask(speakerShown, Fill(Spoken(i)[0]), Spoken(i).Skip(1).Select(Fill).ToList(), cancel: i.Lines.Count - 2);
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
                story.SetVar(i.Name, i.Other.Length > 0 ? Value(i.Other) : i.Number);
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
                string itemName = item.DisplayName;
                var lines = new List<string>
                {
                    i.Op == Op.Find ? Text.Of("found.found", ("n", count), ("item", itemName)) : Text.Of("found.received", ("n", count), ("item", itemName))
                };
                // A TM or an HM says what it holds
                if (!string.IsNullOrEmpty(item.TeachesMove)) lines.Add(Text.Of("found.holdsMove", ("item", itemName), ("move", LocalNames.Move(item.TeachesMove))));
                lines.Add(Text.Of("found.putAway", ("n", count), ("pocket", PocketName(item.Pocket))));
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
                var gift = new Pokemon(species, i.Number);
                if (i.Other.Length > 0) gift.HeldItem = ItemDatabase.Get(i.Other) ?? throw Wrong(i, $"there is no item '{i.Other}'");
                Result = host.GivePokemon(gift) ? 1 : 2;
                break;
            }
            case Op.GiveBadge:
                if (StoryState.FindBadge(host.Region, i.Name) < 0) throw Wrong(i, $"{host.Region} has no badge '{i.Name}'");
                story.GiveBadge(host.Region, i.Name);
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
            // TriggerPlatformLift: the League's platform sets off up its shaft with the player on it; with "check",
            // CheckPlatformLiftNotUsedWhenEnteredMap: whether it stood at the foot as the player came in
            case Op.PlatformLift:
                Result = (i.Option ? host.LiftNotUsed() : host.TriggerLift()) ? 1 : 0;
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
                // What the line asks of the battle itself (plan 14 · B1): an inverse battle
                var terms = i.Inverse ? new BattleTerms(Inverse: true) : null;
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
                    // (and one asked for over the Pokégear's phone waits no longer, plan 20 · J3)
                    Models.PhoneRematch.Fought(story, own.Id);
                    host.Battle(foe, second, partner, mayLose, i.FirstBattle, terms);
                    afterBusy = () =>
                    {
                        foe.TrainerData = own;
                        AfterBattle(mayLose);
                    };
                    break;
                }
                Refresh(foe);
                Refresh(second);
                host.Battle(foe, second, partner, mayLose, i.FirstBattle, terms);
                afterBusy = () => AfterBattle(mayLose);
                break;
            }
            // A Master Trainer's battle (plan 14 · B14; docs/mechanics/rulings.md, "Master Trainers"): the person
            // fights as the master of the species of the Pokémon choosepokemon chose, and the player's side is that
            // Pokémon alone, the team's own, so what the battle does to it stays; nothing may be used from the bag by
            // either side. With nobody chosen who can fight, no battle is fought and RESULT is 255.
            case Op.Master:
            {
                if (NoPokemonToBattle()) break;
                var master = Person(i.Name, i) ?? throw Wrong(i, "the player can't be battled");
                var member = chosenMember;
                if (member == null || !host.Party.Members.Contains(member) || member.IsEgg || member.IsFainted)
                {
                    Result = NoneChosen;
                    break;
                }
                var record = TrainerDatabase.Get(TrainerDatabase.MasterId(member.Species.Name))
                    ?? throw Wrong(i, $"there is no master of {member.Species.Name}");
                var team = new Trainer { Id = record.Id };
                TrainerDatabase.Fill(team, record);
                master.TrainerData = team;
                bool mayLoseThis = i.Option;
                host.Battle(master, null, null, mayLoseThis, false, new BattleTerms(NoBattleItems: true, Alone: member));
                afterBusy = () => AfterBattle(mayLoseThis);
                break;
            }
            case Op.WildBattle:
            case Op.CatchingLesson:
            {
                // The catching lesson is the assistant's battle, fought with the assistant's own Pokémon
                if (i.Op == Op.WildBattle && NoPokemonToBattle()) break;
                // A wild Pokémon may be met in a form of its species, named as the form is (Giratina-Origin, plan 02 · S12)
                var species = PokemonDatabase.Get(i.Name) ?? PokemonDatabase.SpeciesOfForm(i.Name) ?? throw Wrong(i, $"there is no species or form '{i.Name}'");
                var kind = i.Op == Op.CatchingLesson ? BattleKind.CatchingLesson : BattleKind.Normal;
                var wild = new Pokemon(species, i.Number);
                if (!string.Equals(species.Name, i.Name, StringComparison.OrdinalIgnoreCase)) wild.ChangeForm(i.Name);
                // A lesson someone else shows (plan 21 · H2): their name and their Pokémon
                var catcher = i.Op == Op.CatchingLesson && i.Other.Length > 0
                    ? new LessonCatcher(i.Other, new Pokemon(PokemonDatabase.Get(i.Partner) ?? throw Wrong(i, $"there is no species '{i.Partner}'"), i.X))
                    : null;
                host.WildBattle(wild, kind, cannotFlee: i.Option, catcher);
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
            case Op.Act:
                // Waited for to its end (the host's Busy), as a walk is
                host.Act(Person(i.Name, i), i.Gesture, i.Number);
                break;
            case Op.Mood:
                host.Mood(Person(i.Name, i), i.Mood);
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
            {
                // (The host says how long the move takes: a path's time depends on where the camera is when it begins)
                float takes = host.Camera(i.Camera, i.X, i.Y, i.Seconds, i.Factor, i.Camera == CameraMove.Follow ? Person(i.Name, i) : null, i.Tiles);
                // A shake goes on under whatever comes next; any other move is waited for, unless its line says nowait
                if (i.Camera != CameraMove.Shake && !i.Option) waiting = takes;
                break;
            }
            case Op.Effect:
                if (i.Name == "stop")
                {
                    host.StopEffect(i.X, i.Y);
                    break;
                }
                host.Effect(i.Effect, i.X, i.Y, i.Seconds, i.Direction, i.Name == "hold");
                // An effect plays on under whatever comes next, unless the script waits for it to end
                if (i.Name == "wait") waiting = i.Seconds;
                break;

            case Op.UseMove:
            {
                // The Pokémon chosen in the party menu, or the first of the team that knows the move (FindPartySlotWithMove)
                var move = FieldMoveRules.Of(i.Name) ?? throw Wrong(i, $"'{i.Name}' is no field move");
                var user = ownPokemon != null && ownPokemon.Moves.Any(m => m.Name == i.Name) ? ownPokemon
                    : FieldMoveRules.Knower(host.Party, move) ?? throw Wrong(i, $"nobody on the team knows {i.Name}");
                lastUser = user.DisplayName;
                host.Say(null, new[] { Text.Of("fieldMoves.used", ("name", user.DisplayName), ("move", LocalNames.Move(i.Name))) });
                afterBusy = () => host.UseMove(move, user, Subject);
                break;
            }
            case Op.Surf:
                Result = host.Surf() ? 1 : 0;
                break;
            case Op.Climb:
                Result = host.Climb() ? 1 : 0;
                break;
            case Op.Dive:
                Result = host.Dive() ? 1 : 0;
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
                if (i.Option) host.Safari.Start(i.Number > 0 ? i.Number : i.Number < 0 ? SafariGame.NoStepLimit : SafariGame.StartSteps);
                else host.Safari.End();
                break;
            case Op.Bicycle:
                host.Ride(i.Option);
                break;
            case Op.SweetScent:
                // A wild Pokémon comes out where any live, and its battle is waited for
                Result = 0;
                if (host.SweetScent()) afterBusy = () => AfterBattle(mayLose: false);
                break;
            case Op.SmashEncounter:
                // A Pokémon may come out from under the rock just smashed (plan 21 · H4), and its battle is waited for
                Result = 0;
                if (host.SmashEncounter())
                {
                    Result = 1;
                    afterBusy = () => AfterBattle(mayLose: false);
                }
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
                // "halloffame show": the scene of the team's entry, which the script waits for (plan 02 · S14; FieldTask_StartHallOfFame)
                if (i.Name == "show")
                {
                    host.Open(ScriptScreen.Ceremony, Subject);
                    break;
                }
                // Party_GiveChampionRibbons: everyone who enters the Hall of Fame wears Sinnoh's Champion Ribbon (plan 06 · R17b)
                RibbonRules.GiveChampion(host.Party);
                host.EnterHallOfFame();
                break;
            case Op.Travel:
                host.Open(ScriptScreen.Travel, Subject);
                afterBusy = () => Result = host.Answer;
                break;
            case Op.ChoosePokemon:
            {
                // With a species named (plan 14 · B14) only a Pokémon of it may be chosen, never an Egg: the screen
                // greys the rest, and whatever a host hands back that the filter refuses is no choice
                string only = i.Name;
                host.Open(ScriptScreen.ChoosePokemon, Subject, only.Length > 0 ? only : null);
                afterBusy = () =>
                {
                    Result = host.Answer;
                    chosenMember = Result >= 0 && Result < host.Party.Count ? host.Party.Members[Result] : null;
                    // (Without a species, RESULT is whatever the screen said, as it always was)
                    if (only.Length > 0 && (chosenMember == null || !Fits(chosenMember, only)))
                    {
                        chosenMember = null;
                        Result = NoneChosen;
                    }
                };
                break;
            }
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
                // The game's record of Eggs hatched, which the Villa's catalogue asks (plan 06 · R16b)
                if (Result == 1) host.Pastimes.EggsHatched++;
                break;

            // The Super Contests and the Fashion Case (plan 06 · R17a)
            case Op.SuperContest:
                SuperContest(i);
                break;
            case Op.GiveAccessory:
            {
                // Common_GiveAccessory: into the Fashion Case with the item fanfare and a line, when it fits
                int accessory = SuperContestDatabase.Accessory(i.Name);
                if (accessory < 0) throw Wrong(i, $"there is no accessory '{i.Name}'");
                lastAccessory = SuperContestDatabase.AccessoryName(accessory);
                if (!host.FashionCase.Add(accessory, i.Number))
                {
                    Result = 0;
                    break;
                }
                Result = 1;
                host.Fanfare(MusicRole.FanfareItem);
                host.Say(null, new List<string>
                {
                    Text.Of("found.received", ("n", i.Number), ("item", lastAccessory)),
                    Text.Of("found.putInCase", ("n", i.Number))
                });
                break;
            }

            // Ball Capsules, Seals and ribbons (plan 06 · R17b)
            case Op.Seal:
                Seal(i);
                break;
            case Op.Ribbon:
                Ribbon(i);
                break;

            // The Underground (plan 06 · R16a)
            case Op.Underground:
                Underground(i);
                break;

            // The Pokédex is told of a Pokémon the story shows (plan 02 · S12)
            case Op.Seen:
                host.Seen(PokemonDatabase.Get(i.Name) ?? throw Wrong(i, $"there is no species '{i.Name}'"));
                break;

            // Fossils, the Game Corner, the Villa and Pal Park (plan 06 · R16b)
            case Op.Fossil:
                Fossil(i);
                break;
            case Op.GameCorner:
                GameCorner(i);
                break;
            case Op.Villa:
                Villa(i);
                break;
            case Op.PalPark:
                PalPark(i);
                break;

            // A die of the script's own (plan 01 · M11), and a species the Pokédex has seen (plan 19 · K9, part c)
            case Op.Random:
                Result = i.Name == "seen" ? host.SpeciesSeenAtRandom() : host.Random(i.Number);
                break;

            // The Battle Frontier's frame and the Battle Tower (plan 06 · R18a)
            case Op.Frontier:
                Frontier(i);
                break;
            case Op.Tower:
                Tower(i);
                break;

            // What a room's people ask of the team and do for it (plan 01 · M11, part b)
            case Op.Team:
                Team(i);
                break;

            // The Battle Factory (plan 06 · R18b)
            case Op.Factory:
                Factory(i);
                break;

            // The Hall of Fame's end (plan 02 · S14): the game is written to its save where the player stands, without
            // being asked (ClearGame's SaveData_Save), and the credits play while the script waits
            case Op.Save:
                Result = host.Save() ? 1 : 0;
                break;
            case Op.Credits:
                host.Open(ScriptScreen.Credits, Subject);
                break;

            // Furniture and the programmes (plan 08 · P8). A television's programme: one of those on, drawn on the
            // script's die, its lines made of what is true of the game (Broadcasts) and said as one talk
            case Op.Broadcast:
                host.Say(speakerShown, Broadcasts.Compose(host.Broadcast(), host.Random).ToList());
                break;
            // A map on a wall: the Town Map's screen in its wall mode while the script waits. RESULT is 1 when it
            // opened, 0 where the region has no map
            case Op.TownMap:
                host.Open(ScriptScreen.TownMap, Subject);
                afterBusy = () => Result = host.Answer;
                break;

            // After the Hall of Fame (plan 02 · S15): the Pokédex is upgraded to the National one, without a word
            // (SetNationalDexEnabled). The words and the fanfare are the script's
            case Op.NationalDex:
                host.GiveNationalDex();
                break;

            // A number registered in the Pokégear (plan 20 · J3; the original's RegisterPhoneNumber): kept by a flag of
            // the story, and the host says so. RESULT is 1 for a number new to the phone, 0 for one it had
            case Op.AddContact:
                Result = Models.Pokegear.Register(story, i.Name) ? 1 : 0;
                if (Result == 1) host.ContactAdded(Models.PhoneBook.Get(i.Name)!);
                break;

            // A stone panel of the Ruins of Alph (plan 20 · J5): RESULT is 1 when the picture was put back together, 0
            // when the panel was left as it was
            case Op.Puzzle:
                host.Open(ScriptScreen.AlphPuzzle, Subject, i.Name);
                afterBusy = () => Result = host.Answer;
                break;

            // The Battle Hall and the Battle Castle (plan 06 · R18c)
            case Op.Hall:
                Hall(i);
                break;
            case Op.Castle:
                Castle(i);
                break;

            // The move tutors and the Move Deleter (plan 01 · M11, part d)
            case Op.Tutor:
                Tutor(i);
                break;

            // The Battle Arcade (plan 06 · R18d)
            case Op.Arcade:
                Arcade(i);
                break;

            // Amity Square's walk (plan 06 · R16c)
            case Op.Amity:
                Amity(i);
                break;

            // The camera kit (plan 09 · L4): the bars slide and the field slows under whatever comes next
            case Op.Letterbox:
                host.Letterbox(i.Option, i.Seconds);
                break;
            case Op.SlowMotion:
                host.SlowMotion(i.Factor, i.Seconds);
                break;

            // The original's set pieces (plan 09 · L12): each is waited for by the runner itself, for as long as the
            // host says it takes, which is nothing where it can't be played (a room with no machine, someone not on
            // the map), so a script is never left waiting on one
            case Op.Anim:
                waiting = host.Anim(i.Piece, i.Piece switch
                {
                    // CountPartyNonEggs: a ball on the nurse's tray for every Pokémon that is no Egg
                    SetPiece.Heal => host.Party.Members.Count(p => !p.IsEgg),
                    // GetPartyCount: the Hall of Fame's machine takes the whole team's
                    SetPiece.HallOfFame => host.Party.Count,
                    SetPiece.ElevatorUp or SetPiece.ElevatorDown => i.Number,
                    _ => 0
                });
                break;
            case Op.Shake:
                waiting = host.Shake(Person(i.Name, i), i.Number);
                break;
            case Op.Flicker:
                waiting = host.Flicker(Person(i.Name, i), i.Number);
                break;

            // A scene that may be skipped (plan 12 · Q20): from its first line to its endscene, which is what makes
            // it seen. One begun again while it is open (a jump back over its first line) goes on as it was
            case Op.Scene:
            {
                string key = SceneSkip.Key(script!.File, i.Name);
                if (scene == key) break;
                EndTheScene(seen: false);
                scene = key;
                sceneDepth = calls.Count;
                host.SceneBegins(key);
                break;
            }
            case Op.EndScene:
                EndTheScene(seen: true);
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
        host.Say(null, new[] { Text.Of("scriptLines.noPokemonToBattle") });
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
        turns += host.BattleTurns;
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
    /// The Contest Hall's commands (plan 06 · R17a), on the original's contest script commands: <c>rank</c>,
    /// <c>category</c> and <c>mode</c> set what is to be held (the original's <c>VAR_0x8004</c>, <c>VAR_0x8005</c> and
    /// <c>VAR_0x8007</c>); <c>check</c> takes the place of the team <c>choosepokemon</c> left in RESULT and says
    /// whether that Pokémon may enter (<c>CheckContestEligibility</c>: 0 it may, 1 none chosen, 2 an Egg or fainted, 3
    /// it lacks the ribbon of the rank before, 4 it knows fewer than two moves); <c>enter</c> holds the contest for the
    /// Pokémon checked (<c>NewContest</c>, <c>RunContestApplication</c>, <c>EndContest</c>), the script waiting for it,
    /// and RESULT is the player's place, 0 for the winner; <c>ribbon</c> and <c>prize</c> ask what that win gave: RESULT
    /// 1 with a ribbon the Pokémon hadn't (<c>{ribbon}</c>) or an accessory put in the Fashion Case
    /// (<c>{accessory}</c>; <c>GetFirstTimeVictoryAccessory</c>). <c>{contestant}</c> names the Pokémon entered.
    /// </summary>
    private void SuperContest(Instruction i)
    {
        switch (i.Name)
        {
            case "rank":
                contestRank = Enum.Parse<Models.SuperContests.ContestRank>(i.Other, ignoreCase: true);
                break;
            case "category":
                contestCategory = Enum.Parse<ContestStat>(i.Other, ignoreCase: true);
                break;
            case "mode":
                contestMode = i.Other switch
                {
                    "visual" => Models.SuperContests.ContestMode.PracticeVisual,
                    "dance" => Models.SuperContests.ContestMode.PracticeDance,
                    "acting" => Models.SuperContests.ContestMode.PracticeActing,
                    _ => Models.SuperContests.ContestMode.Official
                };
                break;
            case "check":
            {
                int slot = Result;
                contestSlot = -1;
                if (slot < 0 || slot >= host.Party.Count)
                {
                    Result = 1;
                    break;
                }
                // A practice round asks no ribbon (the original holds it at the Normal Rank)
                var rank = contestMode == Models.SuperContests.ContestMode.Official ? contestRank : Models.SuperContests.ContestRank.Normal;
                Result = Models.SuperContests.ContestRules.WhyNot(host.Party.Members[slot], contestCategory, rank) switch
                {
                    null => 0,
                    "egg" or "fainted" => 2,
                    "rank" => 3,
                    _ => 4
                };
                if (Result == 0) contestSlot = slot;
                break;
            }
            case "enter":
            {
                if (contestSlot < 0 || contestSlot >= host.Party.Count) throw Wrong(i, "no Pokémon has been checked for the contest (supercontest check)");
                var rank = contestMode == Models.SuperContests.ContestMode.Official ? contestRank : Models.SuperContests.ContestRank.Normal;
                host.HoldContest(contestSlot, contestCategory, rank, contestMode);
                afterBusy = () => Result = host.ContestOutcome?.Placement ?? 0;
                break;
            }
            case "ribbon":
                Result = host.ContestOutcome?.NewRibbon != null ? 1 : 0;
                if (host.ContestOutcome?.NewRibbon is { } newRibbon) lastRibbon = newRibbon;
                break;
            case "prize":
                Result = host.ContestOutcome?.Prize != null ? 1 : 0;
                if (host.ContestOutcome?.Prize is { } won) lastAccessory = won;
                break;
            case "photo":
                // ContestPhotoHasData: the frame of the contest last named; with a photograph in it, it is shown
                if (host.ContestRecords.PhotoOf(contestCategory) == null)
                {
                    Result = 0;
                    break;
                }
                Result = 1;
                host.Open(ScriptScreen.ContestPhoto, Subject, contestCategory.ToString());
                break;
            case "fashioncase":
                // JubilifeCity_GiveFashionCaseAndAccessories: what comes with the Fashion Case, ten accessories and three backdrops
                host.FashionCase.FillAsGiven(host.Chance);
                break;
        }
    }

    /// <summary>
    /// The Underground's commands (plan 06 · R16a), on the original's (<c>GiveTrap</c>, <c>GiveSphere</c>,
    /// <c>SendGoodToPC</c>, <c>CheckHasRoomForGoodsInPC</c>, <c>GetCapturedFlagCount</c>): <c>give trap</c>, <c>give
    /// sphere</c> and <c>give good</c> put one into the Underground's bag, or a good into the base's PC, without a word
    /// (RESULT 1, or 0 with no room; <c>{thing}</c> names it); <c>room</c> is 1 while the PC has room for a good;
    /// <c>flags</c> the flags brought home. The rest happen in the tunnels and are the host's: <c>down</c> and
    /// <c>up</c>, <c>mine</c> (the wall ahead dug at, the script waiting), <c>hiker</c>, <c>pc</c> and <c>bag</c> (their
    /// screens), <c>cantake</c> and <c>takeflag</c> (another digger's flag), <c>home</c> (the flag carried put on the
    /// player's own PC: 0 none, 1 brought home, 2 and the base's flag is a better one).
    /// </summary>
    private void Underground(Instruction i)
    {
        var state = host.Underground;
        switch (i.Name)
        {
            case "give" when i.Other == "sphere":
            {
                var kind = Enum.Parse<Data.SphereKind>(i.Partner, ignoreCase: true);
                lastThing = Data.UndergroundDatabase.SphereName(kind);
                Result = state.TryAddSphere(kind, i.Number) ? 1 : 0;
                break;
            }
            case "give" when i.Other == "trap":
                lastThing = Data.UndergroundDatabase.TrapName(Data.UndergroundDatabase.TrapId(i.Partner));
                Result = state.TryAddTrap(Data.UndergroundDatabase.TrapId(i.Partner)) ? 1 : 0;
                break;
            case "give":
                lastThing = Data.UndergroundDatabase.GoodName(Data.UndergroundDatabase.GoodId(i.Partner));
                Result = state.TryAddPcGood(Data.UndergroundDatabase.GoodId(i.Partner)) ? 1 : 0;
                break;
            case "room":
                Result = state.PcGoods.Count < Models.Underground.UndergroundState.MaxPcGoods ? 1 : 0;
                break;
            case "flags":
                Result = state.Records.FlagsCaptured;
                break;
            default:
                Result = host.UndergroundAct(i.Name, Subject);
                break;
        }
    }

    private string lastThing = "";

    /// <summary>
    /// The Seals' commands (plan 06 · R17b), on the original's (<c>GiveOrTakeSeal</c>, <c>CountUniqueSealsInSealCase</c>,
    /// <c>OpenSealCapsuleEditor</c>, <c>PokeMartSeal</c>): <c>give</c> puts so many of a kind into the Seal Case without a
    /// word (RESULT 1, or 0 when ninety-nine would be passed; <c>{seal}</c> and <c>{seals}</c> say which and how many);
    /// <c>first</c> the five that come with the case (RESULT how many went in); <c>unown</c> the Seals the boy of
    /// Solaceon Town gives for the team's first Unown (RESULT 0 with no Unown, 1 given, with <c>{wearer}</c> the Unown,
    /// 2 when it is the shape shown last this visit or the kind is full); <c>count</c> how many kinds the player has;
    /// <c>capsules</c> opens the Ball Capsules and <c>shop</c> the market's stall of the day, the script waiting.
    /// </summary>
    private void Seal(Instruction i)
    {
        var sealCase = host.SealCase;
        switch (i.Name)
        {
            case "give":
            {
                int seal = SealDatabase.Seal(i.Other);
                lastSeal = SealDatabase.Name(seal);
                lastSeals = i.Number;
                Result = sealCase.Give(seal, i.Number) ? 1 : 0;
                break;
            }
            case "first":
                Result = CapsuleRules.FirstSeals(host.Chance).Count(seal => sealCase.Give(seal));
                break;
            case "unown":
            {
                // FindPartySlotWithSpecies, then the shape against the one shown last (VAR_MAP_LOCAL_0x00, forgotten on leaving)
                const string shown = StoryState.LocalVariablePrefix + "UNOWN_SEAL";
                var unown = host.Party.Members.FirstOrDefault(p => CapsuleRules.UnownSeal(p) != 0);
                if (unown == null)
                {
                    Result = 0;
                    break;
                }
                int seal = CapsuleRules.UnownSeal(unown);
                int count = CapsuleRules.UnownSeals(sealCase, seal);
                lastWearer = unown.DisplayName;
                lastSeal = SealDatabase.Name(seal);
                if (host.Story.Var(shown) == seal || count <= 0 || !sealCase.Give(seal, count))
                {
                    Result = 2;
                    break;
                }
                host.Story.SetVar(shown, seal);
                lastSeals = count;
                Result = 1;
                break;
            }
            case "count":
                Result = sealCase.Unique;
                break;
            case "capsules":
                host.Open(ScriptScreen.BallCapsules, Subject);
                break;
            case "shop":
                host.Open(ScriptScreen.SealShop, Subject);
                break;
        }
    }

    /// <summary>
    /// The ribbons' commands (plan 06 · R17b), on the original's (<c>GetPartyMonRibbon</c>, <c>SetPartyMonRibbon</c>,
    /// <c>CountPartyRibbons</c>, <c>GetPartyMonEVTotal</c>), all about the first of the team that is no Egg
    /// (<c>GetFirstNonEggInParty</c>; <c>{wearer}</c> names it): <c>has</c> asks whether it has a ribbon and <c>give</c>
    /// puts one on (RESULT 1 when it hadn't it); the ribbon is its constant, <c>today</c> for the one Julia gives on
    /// this day of the week, or <c>offer</c> for the one the Syndicate offers it next. <c>{ribbon}</c> names the ribbon
    /// either spoke of. <c>count</c> is how many different ribbons the team shows; <c>offer</c> the price of the
    /// Syndicate's next ribbon for it (0 once it has all three); <c>effort</c> 1 when its effort values are full;
    /// <c>feeling</c> how Dr. Footstep reads its friendship, 1 to 5.
    /// </summary>
    private void Ribbon(Instruction i)
    {
        int slot = RibbonRules.FirstNonEgg(host.Party);
        var pokemon = slot >= 0 ? host.Party.Members[slot] : null;
        lastWearer = pokemon?.DisplayName ?? "";
        switch (i.Name)
        {
            case "has":
            case "give":
            {
                string? ribbon = i.Other switch
                {
                    "today" => RibbonRules.Daily(host.Weekday),
                    "offer" => pokemon != null ? RibbonRules.SyndicateOffer(pokemon)?.Ribbon : null,
                    _ => i.Other
                };
                if (pokemon == null || ribbon == null)
                {
                    Result = 0;
                    break;
                }
                lastRibbon = RibbonDatabase.Name(ribbon);
                Result = i.Name == "has" ? (RibbonRules.Has(pokemon, ribbon) ? 1 : 0) : RibbonRules.Give(pokemon, ribbon) ? 1 : 0;
                break;
            }
            case "count":
                Result = RibbonRules.CountPartyRibbons(host.Party);
                break;
            case "offer":
            {
                var offer = pokemon != null ? RibbonRules.SyndicateOffer(pokemon) : null;
                if (offer is var (ribbon, price)) lastRibbon = RibbonDatabase.Name(ribbon);
                Result = offer?.Price ?? 0;
                break;
            }
            case "effort":
                Result = pokemon != null && RibbonRules.EarnsEffort(pokemon) ? 1 : 0;
                break;
            case "feeling":
                Result = pokemon != null ? RibbonRules.FeelingLevel(pokemon) : 0;
                break;
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
                // The game's record of berries planted, which the Villa's catalogue asks (plan 06 · R16b)
                host.Pastimes.BerriesPlanted++;
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

    /// <summary>The Massage Girl's sixteen accessories, in the original's order (ACCESSORY_PRETTY_DEWDROP to ACCESSORY_GLITTER_POWDER).</summary>
    public static readonly IReadOnlyList<string> MassageAccessories = new[]
    {
        "Pretty Dewdrop", "Snow Crystal", "Sparks", "Shimmering Fire", "Mystic Fire", "Determination", "Peculiar Spoon", "Puffy Smoke",
        "Poison Extract", "Wealthy Coin", "Eerie Thing", "Spring", "Seashell", "Humming Note", "Shiny Powder", "Glitter Powder",
    };

    /// <summary>
    /// <c>team ...</c> (plan 01 · M11, part b): what the people of the houses ask of the team and do for it, each after
    /// one command of the original's.
    /// </summary>
    private void Team(Instruction i)
    {
        var team = host.Party.Members;
        switch (i.Name)
        {
            // ScrCmd_IncreasePartyMonFriendship, for the Pokémon RESULT names (after choosepokemon): 255 with none
            // chosen, 254 for an Egg
            case "befriend":
            {
                int slot = Result;
                if (slot < 0 || slot >= team.Count) { Result = 255; break; }
                if (team[slot].IsEgg) { Result = 254; break; }
                // (RESULT stays the Pokémon's place, so a line can go on naming it with {member:RESULT})
                FriendshipRules.ChangeByScript(team[slot], i.Number);
                break;
            }
            // The loop of GetPartyMonLevel in the original's Route 221 house: the first of the team at the level a
            // variable holds, or 255
            case "level":
            {
                int level = Value(i.Other);
                int slot = team.FindIndex(p => !p.IsEgg && p.Level == level);
                Result = slot < 0 ? 255 : slot;
                break;
            }
            // ScrCmd_CheckPartyCombeeGenderCount: 0 with no Combee, 1 with one sex, 2 with both
            case "combee":
            {
                bool male = team.Any(p => !p.IsEgg && p.Species.Name == "Combee" && p.Gender == Gender.Male);
                bool female = team.Any(p => !p.IsEgg && p.Species.Name == "Combee" && p.Gender == Gender.Female);
                Result = male && female ? 2 : male || female ? 1 : 0;
                break;
            }
            // GetFirstNonEggInParty and GetPartyMonContestStat
            case "condition":
            {
                var lead = team.FirstOrDefault(p => !p.IsEgg);
                Result = lead == null ? 0 : i.Other switch { "cool" => lead.Cool, "beauty" => lead.Beauty, "cute" => lead.Cute, "smart" => lead.Smart, _ => lead.Tough };
                break;
            }
            // GetFirstNonEggInParty (plan 01 · M11, part c: the Ribbon Syndicate's spa): the first of the team that is no
            // Egg, 255 with none (the original says 0 there, which a team of Eggs alone never asks)
            case "first":
            {
                int slot = team.FindIndex(p => !p.IsEgg);
                Result = slot < 0 ? 255 : slot;
                break;
            }
            // ScrCmd_FindPartySlotWithNature: the first of the team, no Egg, of a nature, or 255
            case "nature":
            {
                var nature = Enum.Parse<Nature>(i.Other);
                int slot = team.FindIndex(p => !p.IsEgg && p.Nature == nature);
                Result = slot < 0 ? 255 : slot;
                break;
            }
            // ScrCmd_CalcSizeContestResult, for the Pokémon RESULT names (after choosepokemon), with the script's own
            // check of its species before it: 0 smaller than the record, 1 the same, 2 larger, 3 for another species
            // (an Egg is none, as the original's GetPartyMonSpecies has it). "team size keep" is
            // ScrCmd_UpdateSizeContestRecord for the one last measured
            case "size" when i.Option:
                if (measured != null) host.Story.SetVar(SizeContest.VariableFor(i.Record), SizeContest.Factor(measured));
                break;
            case "size":
            {
                int slot = Result;
                measured = null;
                Result = 3;
                if (slot < 0 || slot >= team.Count || team[slot].IsEgg || team[slot].Species.Name != i.Other) break;
                measured = team[slot];
                int record = SizeContest.Record(host.Story, i.Record);
                lastSize = SizeContest.Told(SizeContest.Millimetres(measured));
                lastRecord = SizeContest.Told(SizeContest.Millimetres(measured.Height, record));
                Result = (int)SizeContest.Compare(measured, record);
                break;
            }
            // The record alone, as a size, for the chart on a measurer's wall (plan 19 · K9, part c: FireRed's
            // GetHeracrossSizeRecordInfo and GetMagikarpSizeRecordInfo, which fill their buffer with no Pokémon shown).
            // RESULT is 1 where a record is kept and 0 where none is yet
            case "record":
            {
                var species = PokemonDatabase.Get(i.Other);
                int kept = host.Story.Var(SizeContest.VariableFor(i.Record));
                lastRecord = species == null ? "" : SizeContest.Told(SizeContest.Millimetres(species.Height, SizeContest.Record(host.Story, i.Record)));
                Result = kept == 0 ? 0 : 1;
                break;
            }
            // ScrCmd_CheckPartyHasHeldItem (plan 02 · S15, part c): 1 when anyone of the team holds the item, as Mt.
            // Coronet's summit asks of the Adamant and the Lustrous Orb beside the bag. An Egg holds nothing
            case "holds":
                Result = team.Any(p => !p.IsEgg && p.HeldItem?.Name == i.Other) ? 1 : 0;
                break;
            // ScrCmd_ChangeDeoxysForm (plan 06 · R29): every Deoxys of the team takes the meteorite's forme. RESULT is
            // how many changed
            case "deoxys":
            {
                string? form = i.Other == "Deoxys" ? null : i.Other;
                int changed = 0;
                foreach (var p in team.Where(p => !p.IsEgg && p.Species.Name == "Deoxys" && p.Form != form))
                {
                    p.ChangeForm(form);
                    changed++;
                }
                Result = changed;
                break;
            }
            // ScrCmd_TryGetRandomMassageGirlAccessory and Common_GiveAccessory: one of her sixteen, drawn among those the
            // Fashion Case still has room for
            case "massage":
            {
                var fits = MassageAccessories.Select(SuperContestDatabase.Accessory).Where(a => a >= 0 && host.FashionCase.CanFit(a)).ToList();
                if (i.Option) { Result = fits.Count > 0 ? 1 : 0; break; }
                if (fits.Count == 0) { Result = 0; break; }
                int accessory = fits[host.Random(fits.Count)];
                host.FashionCase.Add(accessory);
                lastAccessory = SuperContestDatabase.AccessoryName(accessory);
                Result = 1;
                host.Fanfare(MusicRole.FanfareItem);
                host.Say(null, new List<string> { Text.Of("found.received", ("n", 1), ("item", lastAccessory)), Text.Of("found.putInCase", ("n", 1)) });
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
        ItemPocket.Medicine => Text.Of("found.pocket.Medicine"),
        ItemPocket.PokeBalls => Text.Of("found.pocket.PokeBalls"),
        ItemPocket.TMsAndHMs => Text.Of("found.pocket.TMsAndHMs"),
        ItemPocket.KeyItems => Text.Of("found.pocket.KeyItems"),
        ItemPocket.Berries => Text.Of("found.pocket.Berries"),
        ItemPocket.Mail => Text.Of("found.pocket.Mail"),
        ItemPocket.BattleItems => Text.Of("found.pocket.BattleItems"),
        _ => Text.Of("found.pocket.Items")
    };

    // ------------------------------------------------------------------ conditions

    /// <summary>
    /// The species a <c>has</c> asks after: the name it was written with, or, for <c>has var VAR_X</c> (plan 19 · K9,
    /// part c), the species whose National Pokédex number the variable holds; null where the variable holds no species.
    /// </summary>
    private string? SpeciesAskedAbout(Condition c)
    {
        if (c.Other == null) return c.Name;
        return PokemonDatabase.GetByDex(Value(c.Other))?.Name;
    }

    private int Value(string variable) => variable switch
    {
        "RESULT" => Result,
        "PLAYER_X" => host.TileOf(null).X,
        "PLAYER_Y" => host.TileOf(null).Y,
        "MONEY" => host.Money,
        "PARTY_COUNT" => host.Party.Count,
        "BADGE_COUNT" => host.Story.BadgeCount(host.Region),
        "GREETINGS" => host.Story.Greetings,
        "SEEN" => host.SeenInSinnoh,
        // The friendship of the first of the team (the original's GetFirstNonEggInParty and GetPartyMonFriendship)
        "LEAD_FRIENDSHIP" => host.Party.Count > 0 ? host.Party.Members[0].Friendship : 0,
        // The turns of the battles this script has fought, as the original counts them (a battle's last turn is not
        // one), and the Pokémon caught in the Safari Game under way or just over (plan 01 · M11, part b)
        "TURNS" => turns,
        "SAFARI_CAUGHT" => host.Safari.Caught,
        // How many times the team has entered the Hall of Fame (the original's GetLeagueVictories; plan 02 · S15: the
        // rival's team in the Survival Area)
        "LEAGUE_VICTORIES" => host.LeagueVictories,

        // Whether the player rides the Bicycle (plan 01 · M11, part d: the Cycling Road's gate keepers)
        "CYCLING" => host.Cycling ? 1 : 0,
        // How many species are caught (plan 19 · K3: Oak's aides, who hand out a reward for so many)
        "CAUGHT" => host.Caught,
        _ => host.Story.Var(variable)
    };

    private bool Holds(Condition c, Instruction at)
    {
        var story = host.Story;
        bool yes = c.Query switch
        {
            Query.Flag => story.Has(c.Name),
            Query.Var => Condition.Holds(Value(c.Name), c.Compare, c.Other != null ? Value(c.Other) : c.Number),
            Query.Badge => StoryState.FindBadge(host.Region, c.Name) is var badge and >= 0 ? story.HasBadge(host.Region, badge) : throw Wrong(at, $"{host.Region} has no badge '{c.Name}'"),
            Query.Badges => Condition.Holds(story.BadgeCount(host.Region), c.Compare, c.Number),
            Query.MoveBadge => FieldMoveRules.BadgeFor(FieldMoveRules.Of(c.Name)!.Value, host.Region) is not { } needed
                || StoryState.FindBadge(host.Region, needed) is var index and >= 0 && story.HasBadge(host.Region, index),
            Query.Item => Condition.Holds(ItemDatabase.Get(c.Name) is { } item ? host.Bag.GetQuantity(item) : 0, c.Compare, c.Number),
            Query.Party => Condition.Holds(host.Party.Count, c.Compare, c.Number),
            Query.Knows => host.Party.Members.Any(p => p.Moves.Any(m => m.Name == c.Name)),
            // "has var VAR_X" asks after the species whose National Pokédex number the variable holds (plan 19 · K9, part c)
            Query.Has => SpeciesAskedAbout(c) is { } asked && host.Party.Members.Any(p => p.Species.Name == asked && (c.Place == null || p.MetLocation == c.Place)),
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
            Query.Region => string.Equals(host.Region, c.Name, StringComparison.OrdinalIgnoreCase),
            Query.Subject => Subject?.Key == c.Name,
            Query.Partner => host.Partner != null,
            // ScrCmd_CheckPartyPokerus: one of the team carries it or has had it
            Query.Pokerus => host.Party.Members.Any(p => p.Pokerus != 0),
            // GetTimeOfDay and GetDayOfWeek (plan 06 · R14a)
            Query.Time => host.TimeOfDay.ToString() == c.Name,
            Query.Weekday => host.Weekday.ToString() == c.Name,
            // GetNationalDexEnabled (plan 02 · S14)
            Query.NationalDex => host.NationalDex,
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
    /// <summary>A species named by the data, as the chosen language shows it (plan 17 · N3); nothing for none.</summary>
    private static string NameOfSpecies(string? species) => string.IsNullOrEmpty(species) ? "" : LocalNames.Species(species);

    /// <summary>An item named by the data, as the chosen language shows it (plan 17 · N3); nothing for none.</summary>
    private static string NameOfItem(string? item) => string.IsNullOrEmpty(item) ? "" : LocalNames.Item(item);

    // ------------------------------------------------------------------ the words of a script in the chosen language (plan 17 · N4)

    /// <summary>The lines of a text-bearing instruction as the chosen language says them: the script's own English where its translation has none.</summary>
    private IReadOnlyList<string> Spoken(Instruction i)
    {
        if (i.TextIndex < 0 || script == null) return i.Lines;
        var words = i.Lines;
        var said = new string[words.Count];
        for (int k = 0; k < said.Length; k++) said[k] = Text.Line(script.FullName, i.TextIndex, k, words);
        return said;
    }

    /// <summary>What <c>sayown</c> says: a signboard's or the subject's own lines, translated by where they are written.</summary>
    private IReadOnlyList<string> OwnSpoken()
    {
        var said = new string[ownLines.Count];
        for (int k = 0; k < said.Length; k++)
            said[k] = ownSign is { } sign ? Text.SignLine(sign.Place, sign.Key, k, ownLines)
                : ownFromSubject && Subject != null ? Text.PersonLine(Subject.LinesPlace, Subject.LinesKey, PersonField.Dialog, k, ownLines)
                : Text.SignLine(null, null, k, ownLines);
        return said;
    }

    /// <summary>One row of a prize list: the item (a TM with the move it holds) and its price, written by the key given.</summary>
    private static string PrizeRow(string itemName, string priceKey, string priceArgument, string price)
    {
        var item = ItemDatabase.Get(itemName);
        string shown = item?.DisplayName ?? itemName;
        if (item?.TeachesMove is { Length: > 0 } move) shown = Text.Of("scriptLines.tmRow", ("item", shown), ("move", LocalNames.Move(move)));
        return Text.Of(priceKey, ("name", shown), (priceArgument, price));
    }

    /// <summary>A person's name as the chosen language writes it (their English name where it has no other).</summary>
    private static string? ShownNameOf(NPC? who) => who == null ? null : Text.PersonName(who.LinesPlace, who.LinesKey, who.Name);

    public string Fill(string text) => text.Contains('{') ? Placeholder.Replace(text, match =>
    {
        string argument = match.Groups[2].Value;
        return match.Groups[1].Value switch
        {
            "var" when argument.Length > 0 => Value(argument).ToString(),
            "lead" => host.Party.Members.FirstOrDefault()?.DisplayName ?? "your Pokémon",
            "starter" => NameOfSpecies(host.Story.PlayerStarter),
            "rivalstarter" => NameOfSpecies(host.Story.RivalStarter),
            "assistantstarter" => NameOfSpecies(host.Story.AssistantStarter),
            "self" => ShownNameOf(Subject) ?? "",
            "item" => NameOfItem(lastItem),
            "berry" => NameOfItem(berryName),
            "yield" => berryYield.ToString(),
            "mulch" => NameOfItem(mulchName),
            "user" => lastUser,
            "money" => host.Money.ToString(),
            "result" => Result.ToString(),
            // The Pokémon choosepokemon chose last (plan 14 · B14)
            "chosen" => chosenMember?.DisplayName ?? "",
            // The team's Pokémon at the place a variable gives (poison's survivors)
            "member" when argument.Length > 0 => Value(argument) is var slot && slot >= 0 && slot < host.Party.Count ? host.Party.Members[slot].DisplayName : "",
            // The species whose National Pokédex number a variable holds (plan 19 · K9, part c: the one Selphy wishes for)
            "species" when argument.Length > 0 => NameOfSpecies(PokemonDatabase.GetByDex(Value(argument))?.Name ?? ""),
            // The day's swarm and where it is, and the Trophy Garden's newest (plan 06 · R13)
            "swarm" => NameOfSpecies(Swarms.Species(Swarms.AreaOf(host.Encounters.SwarmDaily))),
            "swarmplace" => Swarms.PlaceName(Swarms.AreaOf(host.Encounters.SwarmDaily)),
            "trophygarden" => NameOfSpecies(TrophyGardenRules.SpeciesIn(host.Encounters.TrophyFirst, SpecialEncounterTables.Sinnoh.TrophyGarden)),
            // The Pokémon the day has put in one of the Great Marsh's six areas, {marsh:1} to {marsh:6} (plan 01 · M11,
            // part b: the observatory's binoculars); the Sinnoh list's, as nothing a script can ask says the National one
            "marsh" when int.TryParse(argument, out int marshArea) && marshArea >= 1 && marshArea <= GreatMarsh.Areas.Length =>
                NameOfSpecies(GreatMarsh.DailySpecies(SpecialEncounterTables.Sinnoh.GreatMarsh, host.Encounters.MarshDaily, GreatMarsh.Areas[marshArea - 1], nationalDex: false)),
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
            // The size last measured and the record it was held against (plan 01 · M11, part c)
            "size" => lastSize,
            "record" => lastRecord,
            // The Super Contests (plan 06 · R17a): the accessory last given, the ribbon last won, and who was entered
            "accessory" => lastAccessory,
            "ribbon" => lastRibbon.Length > 0 ? lastRibbon : host.ContestOutcome?.NewRibbon ?? "",
            // Ball Capsules, Seals and ribbons (plan 06 · R17b): the Seal last given and how many, the Unown or the ribbon's wearer
            "seal" => lastSeal,
            "seals" => lastSeals.ToString(),
            "wearer" => lastWearer,
            // The Underground (plan 06 · R16a): what was last given, and whose base or flag it is
            "thing" => lastThing,
            // The move a Pokémon gave up for the one a tutor taught it (plan 01 · M11, part d)
            "forgotten" => lastForgotten,
            // Fossils, the Game Corner, the Villa and Pal Park (plan 06 · R16b): the coins in the case, a price, a number
            "coins" => host.Pastimes.Coins.Coins.ToString("N0"),
            "price" => lastPrice.ToString("N0"),
            "number" => lastNumber.ToString("N0"),
            "digger" => host.DiggerName,
            // The Battle Frontier and the Battle Tower (plan 06 · R18a): the Battle Points held, who is about to be met, the
            // banned species seen, what a partner of the Battle Salon tells of, and who battles beside the player
            "points" => host.Frontier.BattlePoints.ToString("N0"),
            "foe" => lastFoe,
            "banned" => lastBanned.Length > 0 ? lastBanned : "certain rare Pokémon",
            "ally" when argument is "0" or "1" => allies[argument[0] - '0'],
            "allymove" when argument is "0" or "1" => allyMoves[argument[0] - '0'],
            "partner" => host.Tower?.Partner is { } beside ? Models.Frontier.BattleTowerChallenge.PartnerNames[beside.Id] : "",
            // The Battle Factory (plan 06 · R18b): what the staff's hint names, and the three rentals in hand
            "hint" when argument is "0" or "1" or "2" => hints[argument[0] - '0'],
            "rental" when argument is "0" or "1" or "2" => host.Factory?.Team is { } rented && argument[0] - '0' < rented.Count ? rented[argument[0] - '0'].Species : "",
            // The Battle Hall and the Battle Castle (plan 06 · R18c): who was entered, the type chosen and its rank, the Castle Points held
            "entrant" => lastEntrant,
            "type" => lastType,
            "rank" => lastRank.ToString(),
            "cp" => host.Castle is { } castle ? castle.Points(host.Frontier).ToString("N0") : "0",
            // The Battle Arcade (plan 06 · R18d): the event the game board stopped on
            "event" => lastEvent,
            "struck" => lastStruck,
            // Amity Square's walk (plan 06 · R16c): the Pokémon the receptionist asks about, or the one walking along
            "walker" => lastWalker,
            "contestant" => contestSlot >= 0 && contestSlot < host.Party.Count ? host.Party.Members[contestSlot].DisplayName : host.ContestOutcome?.Pokemon ?? "",
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
