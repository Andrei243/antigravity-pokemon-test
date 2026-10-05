using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Audio;
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

    /// <summary>What the last question, battle or handing-over came to. <c>RESULT</c> in a script.</summary>
    public int Result { get; private set; }

    /// <summary>True when the last script ended because the player lost a battle it couldn't go on from.</summary>
    public bool EndedInDefeat { get; private set; }

    /// <summary>
    /// Starts a script. Nothing of it is carried out until <see cref="Update"/>.
    /// </summary>
    /// <param name="subject">Whoever it belongs to, if anyone.</param>
    /// <param name="own">The lines <c>sayown</c> says: a person's, a signboard's.</param>
    public void Start(Script start, NPC? subject = null, IReadOnlyList<string>? own = null)
    {
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
                var trainer = (Subject ?? throw Wrong(i, "'trainerline' needs someone the script belongs to")).TrainerData
                    ?? throw Wrong(i, $"{Subject.Name} is no trainer and has no trainer's lines");
                string line = i.Option ? trainer.DialogueAfter : trainer.DialogueBefore;
                Result = string.IsNullOrEmpty(line) ? 0 : 1;
                // A line written "Youngster Tristan: ..." is said under that whole title, as the text box shows who speaks
                string? title = line.StartsWith(trainer.FullTitle + ": ", StringComparison.Ordinal) ? trainer.FullTitle : speaker;
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
                story.Set(i.Name);
                break;
            case Op.ClearFlag:
                story.Unset(i.Name);
                break;
            case Op.SetVar:
                story.SetVar(i.Name, i.Number);
                break;
            case Op.AddVar:
                story.AddVar(i.Name, i.Number);
                break;

            case Op.Give:
            {
                var item = ItemOf(i);
                host.Bag.AddItem(item, i.Number);
                lastItem = item.Name;
                host.Fanfare(MusicRole.FanfareItem);
                host.Say(null, new[]
                {
                    i.Number == 1 ? $"{{player}} received the {item.Name}!" : $"{{player}} received {i.Number} × {item.Name}!",
                    $"{{player}} put {(i.Number == 1 ? "it" : "them")} away in the {PocketName(item.Pocket)} pocket."
                });
                break;
            }
            case Op.AddItem:
            {
                var item = ItemOf(i);
                host.Bag.AddItem(item, i.Number);
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

            case Op.Battle:
            {
                var foe = Person(i.Name, i) ?? throw Wrong(i, "the player can't be battled");
                if (foe.TrainerData == null) throw Wrong(i, $"{foe.Name} is no trainer and can't be battled");
                bool mayLose = i.Option;
                host.Battle(foe, mayLose);
                afterBusy = () => AfterBattle(mayLose);
                break;
            }
            case Op.WildBattle:
            {
                var species = PokemonDatabase.Get(i.Name) ?? throw Wrong(i, $"there is no species '{i.Name}'");
                host.WildBattle(new Pokemon(species, i.Number));
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

            case Op.Music:
                host.Music(i.Option ? null : i.Name);
                break;
            case Op.Fanfare:
                host.Fanfare(i.Role);
                break;
            case Op.Sound:
                host.Sound(i.Name);
                break;

            case Op.Starter:
                host.Open(ScriptScreen.Starter, Subject);
                afterBusy = () => Result = host.Answer;
                break;
            case Op.Shop:
                host.Open(ScriptScreen.Shop, Subject);
                break;
            case Op.Pc:
                host.Open(ScriptScreen.Pc, Subject);
                break;
            case Op.Travel:
                host.Open(ScriptScreen.Travel, Subject);
                afterBusy = () => Result = host.Answer;
                break;

            default:
                throw Wrong(i, $"the runner doesn't know how to carry out '{i.Op}'");
        }
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

    /// <summary>Who a line means: null for the player.</summary>
    private NPC? Person(string who, Instruction at) => who switch
    {
        "player" => null,
        "self" => Subject ?? throw Wrong(at, "'self' is nobody here: no person started this script"),
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
            Query.Taken => story.HasTaken(c.Name),
            Query.Starter => story.PlayerStarter == c.Name,
            Query.Money => Condition.Holds(host.Money, c.Compare, c.Number),
            Query.Facing => host.FacingOf(null).ToString() == c.Name,
            Query.Boy => host.PlayerLook == PlayerLook.Boy,
            Query.Girl => host.PlayerLook == PlayerLook.Girl,
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
    /// <c>{starter}</c> and <c>{rivalstarter}</c>, <c>{self}</c>, <c>{item}</c> (the last one given or taken),
    /// <c>{money}</c> and <c>{result}</c>. <c>{player}</c> and <c>{assistant}</c> are left for the host, which
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
            "self" => Subject?.Name ?? "",
            "item" => lastItem,
            "money" => host.Money.ToString(),
            "result" => Result.ToString(),
            _ => match.Value
        };
    }) : text;
}
