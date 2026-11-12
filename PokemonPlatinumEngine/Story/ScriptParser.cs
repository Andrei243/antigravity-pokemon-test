using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// Reads a script file (<c>docs/scripts.md</c>): one command to a line, <c>script Name</c> opening each script,
/// <c>label Name</c> marking a place to go to, texts in double quotes and <c>#</c> before a remark. Everything that
/// can be told wrong from the line alone is told here, with the file and the line: a command that doesn't exist, a
/// word where a number belongs, a jump to a label the script doesn't have. Names of things outside the file (items,
/// species, other scripts) are checked by <see cref="ScriptLibrary.Problems"/>.
/// </summary>
public static class ScriptParser
{
    /// <summary>Variables a script may read but not write: the game keeps them.</summary>
    public static readonly string[] BuiltInVariables =
    {
        "RESULT", "PLAYER_X", "PLAYER_Y", "MONEY", "PARTY_COUNT", "BADGE_COUNT", "GREETINGS", "SEEN", "LEAD_FRIENDSHIP",
        "TURNS", "SAFARI_CAUGHT",
        "LEAGUE_VICTORIES",
        "CYCLING", "BIKE", "RIDING", "RAMMING",
        "CAUGHT",
        // How many Pokédexes' diplomas the player holds (plan 40 · GE4)
        "DIPLOMAS",
        "FAINTED", "LEAD_SPECIES", "SECOND_SPECIES",
        "CROSSED_IN",
        "PASSWORD",
    };

    private static readonly Regex Identifier = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);
    private static readonly Regex FlagName = new("^(FLAG_[A-Z0-9_]+|[A-Z][A-Za-z]+HallOfFame)$", RegexOptions.Compiled);
    // The original's own local variables keep its spelling (VAR_MAP_LOCAL_0x01), which its coordinate events name too
    private static readonly Regex VariableName = new("^(VAR_[A-Z0-9_]+|VAR_MAP_LOCAL_0x[0-9A-F]{2})$", RegexOptions.Compiled);

    /// <summary>The longest a scene's name may be, and what it may be made of (plan 12 · Q20): it is part of a key a save keeps.</summary>
    public const int MostSceneName = 40;
    private static readonly Regex SceneName = new("^[A-Za-z0-9][A-Za-z0-9 _'-]{0,39}$", RegexOptions.Compiled);

    public const float EmoteSeconds = 0.9f, FadeSeconds = 0.4f, PanSeconds = 0.8f, ReleaseSeconds = 0.6f, ShakeSeconds = 0.5f;

    /// <summary>The most answers a <c>choose</c> may offer: what its box holds.</summary>
    public const int MostChoices = 6;

    private readonly record struct Token(string Text, bool Quoted);

    public static List<Script> Parse(string file, string text)
    {
        var scripts = new List<Script>();
        string? name = null;
        int startLine = 0;
        var code = new List<Instruction>();
        var labels = new Dictionary<string, int>(StringComparer.Ordinal);

        void Close()
        {
            if (name == null) return;
            foreach (var i in code.SelectMany(c => c.Then != null ? new[] { c, c.Then } : new[] { c }).Where(i => i.Op == Op.Goto))
            {
                if (!labels.TryGetValue(i.Name, out int target))
                    throw new ScriptException($"{file}.txt({i.Line}): script {name} has no label '{i.Name}'.");
                i.Target = target;
            }
            // The lines that speak are numbered in the order they are written, for the translation files (plan 17 · N4)
            int textIndex = 0;
            foreach (var spoken in code.SelectMany(c => c.Then != null ? new[] { c, c.Then } : new[] { c }).Where(c => c.SpeaksText)) spoken.TextIndex = textIndex++;
            scripts.Add(new Script(file, name, startLine, code.ToList(), new Dictionary<string, int>(labels, StringComparer.Ordinal)));
            code.Clear();
            labels.Clear();
        }

        string[] lines = text.Replace("\r\n", "\n").Split('\n');
        for (int n = 0; n < lines.Length; n++)
        {
            int lineNumber = n + 1;
            var reader = new Reader(file, lineNumber, Tokens(lines[n], file, lineNumber));
            if (!reader.More) continue;

            if (reader.PeekIs("script"))
            {
                reader.Next();
                Close();
                name = reader.Identifier("the script's name");
                startLine = lineNumber;
                if (scripts.Any(s => s.Name == name)) throw reader.Error($"there are two scripts called '{name}'");
                reader.End();
                continue;
            }

            if (name == null) throw reader.Error("a line outside any script: begin with 'script <Name>'");

            if (reader.PeekIs("label"))
            {
                reader.Next();
                string label = reader.Identifier("the label's name");
                if (!labels.TryAdd(label, code.Count)) throw reader.Error($"script {name} has two labels called '{label}'");
                reader.End();
                continue;
            }

            code.Add(Line(reader));
            reader.End();
        }
        Close();
        return scripts;
    }

    // ------------------------------------------------------------------ a line

    private static Instruction Line(Reader r)
    {
        if (!r.PeekIs("if")) return Command(r);

        r.Next();
        var condition = ConditionOf(r);
        if (!r.More) throw r.Error("an 'if' with nothing to do after its question");
        if (r.PeekIs("if")) throw r.Error("an 'if' can't guard another 'if': jump to a label and ask there");
        // (A scene begins and ends where its script says, whatever the story holds: what may be skipped is told from the file)
        if (r.PeekIs("scene") || r.PeekIs("endscene")) throw r.Error("an 'if' can't guard a scene's beginning or its end: jump to a label and begin it there");
        return new Instruction { Op = Op.If, Line = r.Line, Condition = condition, Then = Command(r) };
    }

    private static Instruction Command(Reader r)
    {
        string word = r.Word("a command");
        int line = r.Line;
        switch (word)
        {
            case "say":
            case "text":
                return new Instruction { Op = word == "say" ? Op.Say : Op.Text, Line = line, Lines = r.Texts(1, int.MaxValue, "what is said") };
            case "sayown":
                return new Instruction { Op = Op.SayOwn, Line = line };
            case "trainerline":
            {
                bool after = r.OneOf("before", "after") == "after";
                // The second of two trainers who saw the player at once says their line too
                string whose = r.More ? r.OneOf("pair") : "";
                return new Instruction { Op = Op.TrainerLine, Line = line, Option = after, Name = whose };
            }
            case "speaker":
                // `speaker rowan "Prof. Rowan"`: a cast id before the name (plan 26 · V1), so the shown name stays free to change
                if (!r.PeekQuoted && !r.PeekWord("none") && !r.PeekWord("self"))
                {
                    string voice = r.Identifier("a voice id");
                    return new Instruction { Op = Op.Speaker, Line = line, VoiceId = voice, Name = r.Text("the speaker's name") };
                }
                if (r.PeekQuoted) return new Instruction { Op = Op.Speaker, Line = line, Name = r.Text("the speaker's name") };
                return r.OneOf("none", "self") == "self"
                    ? new Instruction { Op = Op.Speaker, Line = line, Option = true }
                    : new Instruction { Op = Op.Speaker, Line = line };
            case "ask":
                return new Instruction { Op = Op.Ask, Line = line, Lines = r.Texts(1, 1, "the question") };
            case "choose":
                return new Instruction { Op = Op.Choose, Line = line, Lines = r.Texts(3, MostChoices + 1, "a question and at least two answers") };

            case "goto":
                return new Instruction { Op = Op.Goto, Line = line, Name = r.Identifier("a label") };
            case "call":
                return new Instruction { Op = Op.Call, Line = line, Name = r.ScriptName() };
            case "return":
                return new Instruction { Op = Op.Return, Line = line };
            case "end":
                return new Instruction { Op = Op.End, Line = line };

            case "setflag":
            case "clearflag":
            {
                var op = word == "setflag" ? Op.SetFlag : Op.ClearFlag;
                if (!r.PeekIs("own")) return new Instruction { Op = op, Line = line, Name = r.Flag() };
                r.Next();
                return new Instruction { Op = op, Line = line, Own = true };
            }
            case "setvar":
            {
                // A number, or another variable's value as it is now (the original's SetVar takes either): "setvar VAR_X RESULT"
                string variable = r.Variable(writable: true);
                if (r.PeekVariable) return new Instruction { Op = Op.SetVar, Line = line, Name = variable, Other = r.Variable(writable: false) };
                return new Instruction { Op = Op.SetVar, Line = line, Name = variable, Number = r.Int("a value") };
            }
            case "addvar":
                return new Instruction { Op = Op.AddVar, Line = line, Name = r.Variable(writable: true), Number = r.Int("an amount") };

            case "give":
            case "find":
            case "additem":
            case "take":
            {
                var op = word == "give" ? Op.Give : word == "find" ? Op.Find : word == "additem" ? Op.AddItem : Op.Take;
                // The script's own item: what lies in the ball, or in the ground, that started it
                if (op != Op.Take && r.PeekIs("own"))
                {
                    r.Next();
                    return new Instruction { Op = op, Line = line, Own = true };
                }
                string item = r.Text("an item's name");
                int count = r.More ? r.Int("how many") : 1;
                if (count < 1) throw r.Error("an item is given or taken one at a time at least");
                return new Instruction { Op = op, Line = line, Name = item, Number = count };
            }
            case "givepokemon":
            {
                string species = r.Text("a species' name");
                int level = r.Int("its level");
                if (level is < 1 or > 100) throw r.Error("a Pokémon's level is from 1 to 100");
                // (plan 21 · H6: `holding "Mystic Water"`, the item the Pokémon comes with, as Emerald's Castform does)
                // (plan 20 · J11, part c: `knowing "Extreme Speed"`, a move in its first place, as the Dragon Shrine's elder gives)
                // (plan 40 · GE7: an event Pokémon's `in "Cherish Ball"`, `ribbon RIBBON_GIFT_DAY` and `from "the Registry"`)
                string held = "", knows = "", ball = "", ribbon = "", sender = "";
                while (r.More)
                {
                    switch (r.OneOf("holding", "knowing", "in", "ribbon", "from"))
                    {
                        case "holding": held = r.Text("the item it holds"); break;
                        case "knowing": knows = r.Text("the move it knows"); break;
                        case "in": ball = EventBall(r); break;
                        case "ribbon": ribbon = EventRibbon(r); break;
                        default: sender = r.Text("who sends it"); break;
                    }
                }
                return new Instruction { Op = Op.GivePokemon, Line = line, Name = species, Number = level, Other = held, Move = knows, Ball = ball, Ribbon = ribbon, Sender = sender };
            }
            case "givebadge":
            {
                string badge = r.Badge();
                return new Instruction { Op = Op.GiveBadge, Line = line, Name = badge, Other = r.BadgeRegion(badge) };
            }
            case "givemoney":
            case "takemoney":
            {
                int amount = r.Int("an amount");
                if (amount < 0) throw r.Error("an amount of money is not negative");
                return new Instruction { Op = word == "givemoney" ? Op.GiveMoney : Op.TakeMoney, Line = line, Number = amount };
            }
            case "heal":
                return new Instruction { Op = Op.Heal, Line = line };
            case "greetings":
                r.OneOf("clear");
                return new Instruction { Op = Op.ClearGreetings, Line = line };
            case "turnback":
                return new Instruction { Op = Op.Turnback, Line = line };
            case "defeat":
                return new Instruction { Op = Op.Defeat, Line = line, Name = r.Text("a trainer's id") };
            case "flowerclock":
                return new Instruction { Op = Op.FlowerClock, Line = line };
            case "pressbutton":
                return new Instruction { Op = Op.PressButton, Line = line, Name = r.OneOf("blue", "green", "orange") };
            case "gearbutton":
                return new Instruction { Op = Op.GearButton, Line = line, Name = r.OneOf("normal", "reverse", "double") };
            case "platformlift":
                // "platformlift check" asks; a bare "platformlift" sets the platform off
                return new Instruction { Op = Op.PlatformLift, Line = line, Option = r.PeekWord("check") && r.OneOf("check") == "check" };
            case "partner":
            {
                if (r.PeekWord("off"))
                {
                    r.OneOf("off");
                    return new Instruction { Op = Op.Partner, Line = line };
                }
                string who = r.Who();
                if (who == "player") throw r.Error("the player can't travel with themselves");
                string id = r.Text("the id of the trainer who battles beside the player");
                return new Instruction { Op = Op.Partner, Line = line, Name = who, Other = id, Option = true };
            }

            case "battle":
            {
                string who = r.Who();
                if (who == "player") throw r.Error("the player can't be battled");
                // What may follow, in any order: a second trainer, someone at the player's side, a battle that may be lost
                bool mayLose = false, byId = false, first = false, rematch = false, inverse = false;
                string second = "", partner = "", asTrainer = "";
                var staged = new Battle.BattleTerms();
                while (r.More)
                {
                    string option = r.OneOf("canlose", "first", "and", "with", "as", "rematch", "inverse", "hidelevel", "untouchable", "passive", "endafter");
                    switch (option)
                    {
                        // A staged battle's terms (docs/mechanics/rulings.md, "Staged battles")
                        case "hidelevel":
                        case "untouchable":
                        case "passive":
                        case "endafter":
                            staged = StagedTerm(r, option, staged);
                            break;
                        case "inverse":
                            inverse = true;
                            break;
                        case "rematch":
                            rematch = true;
                            break;
                        case "as":
                            asTrainer = r.Text("a trainer's id");
                            break;
                        case "canlose":
                            mayLose = true;
                            break;
                        case "first":
                            first = true;
                            break;
                        case "and":
                            second = r.Who();
                            if (second == "player" || second == who) throw r.Error("the second trainer is someone else of the map");
                            break;
                        default:
                            byId = r.PeekQuoted;
                            partner = byId ? r.Text("a trainer's id") : r.Who();
                            if (partner == "player" || partner == who || partner == second) throw r.Error("the player's partner is someone else of the map");
                            break;
                    }
                }
                return new Instruction { Op = Op.Battle, Line = line, Name = who, Other = second, Partner = partner, PartnerById = byId, Option = mayLose, FirstBattle = first, AsTrainer = asTrainer, Rematch = rematch, Inverse = inverse,
                    Staged = staged.Staged ? staged : null };
            }
            // A Master Trainer's battle (plan 14 · B14): the one choosepokemon chose, alone, with the bag closed
            // A Battle Royal (plan 14 · B13): three people of the map, each named with the trainer of the data they battle
            // as (`royal a "royal_a" b "royal_b" c "royal_c" canlose`), every one for themselves against the player
            case "royal":
            {
                var people = new List<string>();
                var ids = new List<string>();
                for (int k = 0; k < 3; k++)
                {
                    string who = r.Who();
                    if (who == "player") throw r.Error("the player can't be battled");
                    if (people.Contains(who)) throw r.Error("a Battle Royal is three different people of the map");
                    people.Add(who);
                    ids.Add(r.Text("the id of the trainer they battle as"));
                }
                bool mayLose = r.More && r.OneOf("canlose") == "canlose";
                return new Instruction { Op = Op.Royal, Line = line, Name = people[0], Other = people[1], Partner = people[2], Trainers = ids, Option = mayLose };
            }
            case "master":
            {
                string who = r.Who();
                if (who == "player") throw r.Error("the player can't be battled");
                bool mayLose = r.More && r.OneOf("canlose") == "canlose";
                return new Instruction { Op = Op.Master, Line = line, Name = who, Option = mayLose };
            }
            case "wildbattle":
            case "catchinglesson":
            {
                string species = r.Text("a species' name");
                int level = r.Int("its level");
                // A staged foe may stand above any level a game reaches, up to the highest a Pokémon has (checked once its terms are read)
                if (level is < 1 or > Data.Ruleset.HighestLevel || (word == "catchinglesson" && level > 100)) throw r.Error("a Pokémon's level is from 1 to 100");
                if (word == "catchinglesson")
                {
                    // Who shows the lesson, when it isn't the assistant (plan 21 · H2: Wally with his father's
                    // Zigzagoon): `by "Wally" "Zigzagoon" 7`, the catcher's name, their Pokémon and its level
                    if (!r.More) return new Instruction { Op = Op.CatchingLesson, Line = line, Name = species, Number = level };
                    r.OneOf("by");
                    string catcher = r.Text("the catcher's name");
                    string theirs = r.Text("the catcher's Pokémon");
                    int theirLevel = r.More ? r.Int("its level") : 5;
                    if (theirLevel is < 1 or > 100) throw r.Error("a Pokémon's level is from 1 to 100");
                    return new Instruction { Op = Op.CatchingLesson, Line = line, Name = species, Number = level, Other = catcher, Partner = theirs, X = theirLevel };
                }
                // `nofleeing` (a battle that can't be run from) and `shiny` (plan 20 · J11: the Lake of Rage's red
                // Gyarados), in either order
                // and `holding "Soul Dew"` (plan 21 · H12, part a: Emerald's seteventmon of the eon Pokémon on Southern Island)
                // and `totem defense [2] [ally "Alomomola"]` (plan 23 · Z6: a trial's totem, with its aura's stats, their
                // stages and the species its first call brings; a totem can't be run from)
                bool noFleeing = false, shiny = false, mayLose = false, mayRun = false;
                string held = "";
                Battle.Sim.TotemAura? totem = null;
                var staged = new Battle.BattleTerms();
                while (r.More)
                {
                    string option = r.OneOf("nofleeing", "shiny", "holding", "totem", "ally", "canlose", "hidelevel", "untouchable", "passive", "endafter", "mayrun");
                    if (option == "shiny") shiny = true;
                    // A staged battle's terms, and a loss the story goes on from (docs/mechanics/rulings.md, "Staged battles")
                    else if (option is "hidelevel" or "untouchable" or "passive" or "endafter") staged = StagedTerm(r, option, staged);
                    else if (option == "canlose") mayLose = true;
                    else if (option == "mayrun") mayRun = true;
                    else if (option == "holding") held = r.Text("an item's name");
                    else if (option == "totem")
                    {
                        var stats = new List<StatType>();
                        while (Battle.Sim.TotemAura.StatWords.FirstOrDefault(r.PeekWord) is { } statWord)
                        {
                            r.OneOf(statWord);
                            stats.AddRange(Battle.Sim.TotemAura.StatsOf(statWord));
                        }
                        if (stats.Count == 0) throw r.Error("a totem's aura names the stats it raises (attack, defense, spattack, spdefense, speed, accuracy, evasion or all)");
                        int stages = r.PeekNumber ? r.Int("its stages") : 1;
                        if (stages is < 1 or > 6) throw r.Error("an aura raises a stat by one to six stages");
                        totem = new Battle.Sim.TotemAura(stats, stages);
                        noFleeing = true;
                    }
                    else if (option == "ally")
                    {
                        if (totem == null) throw r.Error("only a totem names its ally: `totem <stats>` comes first");
                        totem = totem with { Ally = r.Text("a species' name") };
                    }
                    else noFleeing = true;
                }
                if (level > 100 && !staged.HideLevel) throw r.Error("a Pokémon's level is from 1 to 100 (above it only with hidelevel, a staged foe's)");
                if (totem != null && (staged.Staged || mayLose)) throw r.Error("a totem's battle is no staged battle and is never lost to go on");
                // A staged foe can't be run from unless the line lets it be (`mayrun`)
                if (mayRun && (!staged.Staged || noFleeing)) throw r.Error("only a staged battle says mayrun, and never with nofleeing");
                if (staged.Staged && !mayRun) noFleeing = true;
                return new Instruction { Op = Op.WildBattle, Line = line, Name = species, Number = level, Option = noFleeing, Shiny = shiny, Other = held, Totem = totem,
                    Staged = staged.Staged ? staged : null, CanLose = mayLose };
            }

            case "face":
            {
                string who = r.Who();
                if (r.PeekDirection) return new Instruction { Op = Op.Face, Line = line, Name = who, Direction = r.Direction() };
                return new Instruction { Op = Op.Face, Line = line, Name = who, Other = r.Who() };
            }
            case "walk":
            case "move":
            {
                string who = r.Who();
                var (path, fast) = r.Path();
                return new Instruction { Op = word == "walk" ? Op.Walk : Op.Move, Line = line, Name = who, Path = path, Option = fast };
            }
            case "waitmoves":
                return new Instruction { Op = Op.WaitMoves, Line = line };
            case "act":
            {
                // act <who> <gesture> [times] (plan 11 · C11): played, and waited for as a walk is
                string who = r.Who();
                var gesture = r.Enum<Gesture>("a gesture");
                int times = r.More ? r.Int("how many times") : 1;
                if (times < 1 || times > Acting.MostTimes) throw r.Error($"a gesture is played 1 to {Acting.MostTimes} times in a row");
                return new Instruction { Op = Op.Act, Line = line, Name = who, Gesture = gesture, Number = times };
            }
            case "pose":
            {
                // pose <who> <stance> (plan 11 · C7): sit, sitground, lie or stand, until the place is come to again
                string who = r.Who();
                return new Instruction { Op = Op.Pose, Line = line, Name = who, Stance = r.Enum<Stance>("a stance") };
            }
            case "mood":
            {
                // mood <who> <mood> (plan 11 · C11): the face held until it is changed or the place is come to again
                string who = r.Who();
                return new Instruction { Op = Op.Mood, Line = line, Name = who, Mood = r.Enum<Mood>("a mood") };
            }
            case "emote":
            {
                string who = r.Who();
                var bubble = r.Enum<EmoteBubble>("a bubble");
                if (bubble == EmoteBubble.None) throw r.Error("'none' is no bubble to show");
                return new Instruction { Op = Op.Emote, Line = line, Name = who, Bubble = bubble, Seconds = r.More ? r.Seconds() : EmoteSeconds };
            }
            case "show":
            case "hide":
            {
                string who = r.Who();
                if (who == "player") throw r.Error("the player is never hidden");
                return new Instruction { Op = word == "show" ? Op.Show : Op.Hide, Line = line, Name = who };
            }
            case "place":
            {
                string who = r.Who();
                int x = r.Int("a tile's x"), y = r.Int("a tile's y");
                return new Instruction { Op = Op.Place, Line = line, Name = who, X = x, Y = y, Direction = r.More ? r.Direction() : null };
            }
            case "warp":
            {
                string map = r.Text("a map's name");
                int x = r.Int("a tile's x"), y = r.Int("a tile's y");
                return new Instruction { Op = Op.Warp, Line = line, Name = map, X = x, Y = y, Direction = r.More ? r.Direction() : null };
            }
            // The open world's forms of the place commands (plan 32 · OW5): a marker's name where the grid's take a tile
            case "walkto":
            {
                // walkto <who> <marker> [fast]: waited for as a walk is
                string who = r.Who();
                string marker = r.Identifier("a marker's name");
                bool fast = false;
                if (r.PeekIs("fast"))
                {
                    r.Next();
                    fast = true;
                }
                return new Instruction { Op = Op.WalkTo, Line = line, Name = who, Other = marker, Option = fast };
            }
            case "placeat":
                return new Instruction { Op = Op.PlaceAt, Line = line, Name = r.Who(), Other = r.Identifier("a marker's name") };
            case "faceto":
                // faceto <who> <marker|who>: turned toward a marker, or toward someone (the player)
                return new Instruction { Op = Op.FaceTo, Line = line, Name = r.Who(), Other = r.Who() };
            case "warpto":
                return new Instruction { Op = Op.WarpTo, Line = line, Name = r.Text("an open map's name"), Other = r.Identifier("a marker's name") };
            case "fade":
            {
                bool toBlack = r.OneOf("out", "in") == "out";
                return new Instruction { Op = Op.Fade, Line = line, Option = toBlack, Seconds = r.More ? r.Seconds() : FadeSeconds };
            }
            case "wait":
                return new Instruction { Op = Op.Wait, Line = line, Seconds = r.Seconds() };
            case "waitvoice":
                return new Instruction { Op = Op.WaitVoice, Line = line };
            case "camera":
            {
                // A move the script waits for goes on under whatever comes next when its line ends in 'nowait' (plan
                // 09 · L4): a path and a zoom at once are a path written so and the zoom after it
                bool NoWait()
                {
                    if (!r.PeekIs("nowait")) return false;
                    r.Next();
                    return true;
                }
                switch (r.OneOf("pan", "release", "shake", "zoom", "follow", "path", "look"))
                {
                    case "look":
                    {
                        // camera look <marker> [seconds] [nowait] (plan 32 · OW5): an open map's camera sent to a marker
                        string marker = r.Identifier("a marker's name");
                        float seconds = r.PeekSeconds ? r.Seconds() : PanSeconds;
                        return new Instruction { Op = Op.LookAt, Line = line, Name = marker, Seconds = seconds, Option = NoWait() };
                    }
                    case "pan":
                    {
                        int x = r.Int("a tile's x"), y = r.Int("a tile's y");
                        float seconds = r.PeekSeconds ? r.Seconds() : PanSeconds;
                        return new Instruction { Op = Op.Camera, Line = line, Camera = CameraMove.Pan, X = x, Y = y, Seconds = seconds, Option = NoWait() };
                    }
                    case "release":
                    {
                        float seconds = r.PeekSeconds ? r.Seconds() : ReleaseSeconds;
                        return new Instruction { Op = Op.Camera, Line = line, Camera = CameraMove.Release, Seconds = seconds, Option = NoWait() };
                    }
                    case "zoom":
                    {
                        // camera zoom <factor> [seconds] [nowait]: the lens alone; 1 is the place's own
                        float factor = r.Number("a zoom's factor", CameraRig.WidestZoom, CameraRig.TightestZoom);
                        float seconds = r.PeekSeconds ? r.Seconds() : CameraRig.ZoomSeconds;
                        return new Instruction { Op = Op.Camera, Line = line, Camera = CameraMove.Zoom, Factor = factor, Seconds = seconds, Option = NoWait() };
                    }
                    case "follow":
                    {
                        // camera follow <who> [seconds] [nowait]: on someone's feet until told otherwise; the player's are home
                        string who = r.Who();
                        float seconds = r.PeekSeconds ? r.Seconds() : CameraRig.FollowSeconds;
                        return new Instruction { Op = Op.Camera, Line = line, Camera = CameraMove.Follow, Name = who, Seconds = seconds, Option = NoWait() };
                    }
                    case "path":
                    {
                        // camera path X Y [X Y …] [tiles a second] [nowait]: a number left over after the pairs is the pace
                        var numbers = new List<string>();
                        while (r.PeekSeconds) numbers.Add(r.Word("a tile's x"));
                        if (numbers.Count < 2) throw r.Error("a path goes through at least one tile: its x and its y");
                        float pace = CameraRig.PathPace;
                        if (numbers.Count % 2 == 1)
                        {
                            string last = numbers[^1];
                            numbers.RemoveAt(numbers.Count - 1);
                            if (!float.TryParse(last, NumberStyles.Float, CultureInfo.InvariantCulture, out pace) || pace < CameraRig.SlowestPath || pace > CameraRig.FastestPath)
                                throw r.Error($"'{last}' is no pace for a path ({CameraRig.SlowestPath.ToString(CultureInfo.InvariantCulture)} to {CameraRig.FastestPath.ToString(CultureInfo.InvariantCulture)} tiles a second): the number left over after the tiles' pairs is the pace");
                        }
                        if (numbers.Count / 2 > CameraRig.MostPathTiles) throw r.Error($"a path goes through {CameraRig.MostPathTiles} tiles at the most");
                        var tiles = new List<(int X, int Y)>();
                        for (int n = 0; n < numbers.Count; n += 2)
                        {
                            if (!int.TryParse(numbers[n], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int x)
                                || !int.TryParse(numbers[n + 1], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int y))
                                throw r.Error($"'{numbers[n]} {numbers[n + 1]}' is not a tile: a path goes through whole tiles");
                            tiles.Add((x, y));
                        }
                        return new Instruction { Op = Op.Camera, Line = line, Camera = CameraMove.Path, Tiles = tiles, Factor = pace, Option = NoWait() };
                    }
                    default:
                        return new Instruction { Op = Op.Camera, Line = line, Camera = CameraMove.Shake, Seconds = r.More ? r.Seconds() : ShakeSeconds };
                }
            }

            case "effect":
            {
                // effect <kind> X Y [seconds] [way] [wait|hold], or effect stop X Y (plan 09 · L11)
                if (r.PeekIs("stop"))
                {
                    r.Next();
                    int sx = r.Int("a tile's x"), sy = r.Int("a tile's y");
                    return new Instruction { Op = Op.Effect, Line = line, Name = "stop", X = sx, Y = sy };
                }
                var kind = r.Enum<SceneEffect>("a scene effect");
                int x = r.Int("a tile's x"), y = r.Int("a tile's y");
                float seconds = SceneEffects.Length(kind);
                bool timed = r.PeekSeconds;
                if (timed)
                {
                    seconds = r.Seconds();
                    if (seconds <= 0f) throw r.Error("an effect lasts longer than nothing");
                }
                Direction? way = null;
                if (r.PeekDirection)
                {
                    if (!SceneEffects.Moves(kind)) throw r.Error($"only a shadow is sent a way: a {kind.ToString().ToLowerInvariant()} stays where it is lit");
                    way = r.Direction();
                }
                string mode = r.More ? r.OneOf("wait", "hold") : "";
                if (mode == "hold" && timed) throw r.Error("a held effect lasts until 'effect stop', and is given no time");
                return new Instruction { Op = Op.Effect, Line = line, Effect = kind, X = x, Y = y, Seconds = seconds, Direction = way, Name = mode };
            }

            case "usemove":
            {
                string move = r.Text("a field move's name");
                if (FieldMoveRules.Of(move) == null) throw r.Error($"'{move}' is no move a Pokémon uses in the field");
                return new Instruction { Op = Op.UseMove, Line = line, Name = move };
            }
            case "surf":
                return new Instruction { Op = Op.Surf, Line = line };
            case "climb":
                return new Instruction { Op = Op.Climb, Line = line };
            case "dive":
                return new Instruction { Op = Op.Dive, Line = line };
            case "fly":
                return new Instruction { Op = Op.Fly, Line = line };
            case "teleport":
                return new Instruction { Op = Op.Teleport, Line = line };
            case "escape":
                return new Instruction { Op = Op.Escape, Line = line };
            case "sweetscent":
                return new Instruction { Op = Op.SweetScent, Line = line };
            case "rocksmash":
            case "smashencounter":
                return new Instruction { Op = Op.SmashEncounter, Line = line };
            case "headbutt":
                // The tree in front shaken (plan 38 · EX4), and what falls out of it battled
                return new Instruction { Op = Op.Headbutt, Line = line };
            case "poketch":
                r.OneOf("on");
                return new Instruction { Op = Op.Poketch, Line = line };
            case "poketchapp":
                return new Instruction { Op = Op.PoketchApp, Line = line, Name = r.Enum<Models.PoketchApp>("a Pokétch app").ToString() };
            case "safari":
            {
                // A Safari Game of another game's own number of steps (plan 19 · K6: FireRed's Safari Zone is 600)
                bool start = r.OneOf("start", "end") == "start";
                // ...or of none: HeartGold's Safari Zone counts no steps (plan 20 · J8, part b: `safari start endless`)
                bool endless = start && r.PeekWord("endless");
                if (endless) r.Next();
                int steps = start && !endless && r.PeekNumber ? r.Int("how many steps") : 0;
                if (steps < 0) throw r.Error("a Safari Game has steps");
                if (endless) steps = -1;
                return new Instruction { Op = Op.Safari, Line = line, Option = start, Number = steps };
            }
            case "bicycle":
                // On or off the Bicycle by the story's say (plan 19 · K6: FireRed's ForcePlayerOntoBike on the Cycling Road)
                return new Instruction { Op = Op.Bicycle, Line = line, Option = r.OneOf("on", "off") == "on" };
            case "ride":
                // On a Ride Pager's Pokémon or off it (plan 23, wave 91): `ride "Tauros"`, `ride off`
                if (r.PeekWord("off"))
                {
                    r.OneOf("off");
                    return new Instruction { Op = Op.RidePokemon, Line = line };
                }
                return new Instruction { Op = Op.RidePokemon, Line = line, Name = r.Text("a species' name"), Option = true };

            case "music":
                if (r.PeekQuoted) return new Instruction { Op = Op.Music, Line = line, Name = r.Text("a song") };
                return new Instruction { Op = Op.Music, Line = line, Option = r.OneOf("area", "stop") == "area" };
            case "fanfare":
                return new Instruction
                {
                    Op = Op.Fanfare,
                    Line = line,
                    Role = r.OneOf("heal", "item", "pokemon", "levelup", "keyitem", "tm", "badge", "evolution") switch
                    {
                        "heal" => MusicRole.FanfareHeal,
                        "item" => MusicRole.FanfareItem,
                        "pokemon" => MusicRole.FanfarePokemon,
                        "keyitem" => MusicRole.FanfareKeyItem,
                        "tm" => MusicRole.FanfareTM,
                        "badge" => MusicRole.FanfareBadge,
                        "evolution" => MusicRole.FanfareEvolution,
                        _ => MusicRole.FanfareLevelUp
                    }
                };
            case "sound":
                return new Instruction { Op = Op.Sound, Line = line, Name = r.Text("a sound's name") };
            case "cry":
                // The Pokémon the script belongs to cries (plan 10 · F1)
                if (r.PeekIs("own"))
                {
                    r.Next();
                    return new Instruction { Op = Op.Cry, Line = line, Own = true };
                }
                return new Instruction { Op = Op.Cry, Line = line, Name = r.Text("a species' name") };

            case "starter":
                return new Instruction { Op = Op.Starter, Line = line };
            case "wardrobe":
                // The salon's chair and the dye house's counter (plan 43 · LF3): bare words, the dye house with its boutique's key
                if (r.PeekIs("salon"))
                {
                    r.Next();
                    return new Instruction { Op = Op.Wardrobe, Line = line, Name = Instruction.SalonCounter };
                }
                if (r.PeekIs("dye"))
                {
                    r.Next();
                    return new Instruction { Op = Op.Wardrobe, Line = line, Name = Instruction.DyeCounter + r.Text("a boutique's key") };
                }
                return new Instruction { Op = Op.Wardrobe, Line = line, Name = r.More ? r.Text("a boutique's key") : "" };
            case "shop":
                return new Instruction { Op = Op.Shop, Line = line, Name = r.More ? r.Text("a counter's key") : "" };
            case "pc":
                // "pc halloffame" opens the Hall of Fame's records instead of the boxes (plan 06 · R12)
                return new Instruction { Op = Op.Pc, Line = line, Name = r.More ? r.OneOf("halloffame") : "" };
            case "jobs":
                // The Poké Jobs board (plan 15 · E3)
                return new Instruction { Op = Op.Jobs, Line = line };
            case "halloffame":
                // "halloffame show" is the scene of the team's entry instead of the record (plan 02 · S14)
            {
                // "halloffame look <region>" opens the record on one region's teams (plan 41 · EG2: the Laurel Gallery's
                // plinths), "earlier" the teams from before a team's region was kept, "Circuit" the circuit's finals
                string how = r.More ? r.OneOf("show", "look") : "";
                if (how != "look") return new Instruction { Op = Op.HallOfFame, Line = line, Name = how };
                string tab = r.Word("a region's id, earlier or Circuit");
                if (tab != "earlier" && tab != Models.ChampionsCircuit.CircuitRegion && RegionDatabase.Get(tab) == null)
                    throw r.Error($"there is no region '{tab}'");
                return new Instruction { Op = Op.HallOfFame, Line = line, Name = how, Other = tab };
            }
            case "travel":
            {
                // "travel go" is the crossing itself, over a script's dark (plan 18 · W5: common.Crossing)
                // "travel where" and "travel others" are a harbour's question of where to sail (plan 18 · W8)
                string how = r.More ? r.OneOf("go", "to", "where", "others") : "";
                if (how != "to") return new Instruction { Op = Op.Travel, Line = line, Name = how };
                // "travel to "BattleFrontier" 47 51 up": a crossing onto a tile of another region's map, by no vessel
                // (plan 20 · J13, part d: Route 40's way to the Battle Frontier and back)
                string map = r.Text("a map's name");
                int x = r.Int("a tile's x"), y = r.Int("a tile's y");
                return new Instruction { Op = Op.Travel, Line = line, Name = how, Other = map, X = x, Y = y, Direction = r.More ? r.Direction() : null };
            }
            case "choosepokemon":
                // "choosepokemon "Bidoof"" lets only that species be chosen (plan 14 · B14: a Master Trainer's)
                return new Instruction { Op = Op.ChoosePokemon, Line = line, Name = r.More ? r.Text("a species' name") : "" };
            // Wild Pokémon (plan 06 · R13)
            case "honeytree":
                return new Instruction { Op = Op.HoneyTree, Line = line, Name = r.OneOf("status", "slather", "battle") };
            case "lottery":
                return new Instruction { Op = Op.Lottery, Line = line, Name = r.OneOf("draw", "boxed", "weekly") };
            case "berry":
                return new Instruction { Op = Op.Berry, Line = line, Name = r.OneOf("status", "mulched", "water", "plant", "mulch", "pick", "berries", "mulches", "pot", "crossed") };
            // Apricorn trees, the Apricorn Box and Kurt's balls (plan 20 · J6)
            case "apricorn":
            {
                string what = r.OneOf("tree", "pick", "box", "held", "count", "order", "status", "collect");
                string colour = what is "count" or "order" ? r.OneOf("red", "blue", "yellow", "green", "pink", "white", "black") : "";
                return new Instruction { Op = Op.Apricorn, Line = line, Name = what, Other = colour };
            }
            // Cameron's photographs (plan 20, part q)
            case "photo":
            {
                // The photo book and its spots (plan 43 · LF4): `photo book`, `photo spot "<id>"`, `photo booked`, how many it holds
                string photoWord = r.OneOf("take", "album", "count", "book", "spot", "booked", "studio");
                return new Instruction { Op = Op.Photo, Line = line, Name = photoWord, Other = photoWord == "spot" ? r.Text("a photo spot's id") : null };
            }
            case "chooseitem":
                return new Instruction { Op = Op.ChooseItem, Line = line, Name = r.OneOf("berries", "mulch", "gift", "meal") };
            case "swarms":
                r.OneOf("on");
                return new Instruction { Op = Op.Swarms, Line = line };
            case "trophygarden":
                return new Instruction { Op = Op.TrophyGarden, Line = line };
            case "roamer":
            {
                r.OneOf("start");
                string species = r.Text("a roaming Pokémon's species");
                // (a region named after it: FireRed's beast roams Kanto, plan 29 · LS9, where a Raikou or an Entei is otherwise
                // Johto's; X and Y's bird roams Kalos, plan 23 · Z4b, where an Articuno is otherwise one of Sinnoh's three)
                string? region = null;
                if (r.PeekWord("kanto")) { r.Next(); region = "kanto"; }
                else if (r.PeekWord("kalos")) { r.Next(); region = "kalos"; }
                if (Overworld.Roamers.SlotOf(species, region) == null) throw r.Error($"'{species}' is no Pokémon that roams{(region == null ? "" : " in " + region)} (Roamers.Slots)");
                return new Instruction { Op = Op.Roamer, Line = line, Name = species, Other = region };
            }
            case "survivepoison":
                return new Instruction { Op = Op.SurvivePoison, Line = line, Name = r.Variable(writable: false) };

            case "trade":
            {
                string trade = r.Word("a trade");
                // The trade board and blind trades (plan 40 · GE8): `trade board open`, `trade board 1` to `3` (a request of
                // the day met with the Pokémon chosen last), `trade blind`
                if (trade == "board")
                {
                    if (r.PeekWord("open")) return new Instruction { Op = Op.Trade, Line = line, Name = "board", Other = r.OneOf("open") };
                    int n = r.Int("a request of the day, 1 to 3");
                    if (n < 1 || n > Models.TradeBoard.RequestsADay) throw r.Error($"the board has requests 1 to {Models.TradeBoard.RequestsADay}");
                    return new Instruction { Op = Op.Trade, Line = line, Name = "board", Number = n };
                }
                if (trade == "blind") return new Instruction { Op = Op.Trade, Line = line, Name = "blind" };
                if (Models.NpcTrades.Get(trade) == null) throw r.Error($"'{trade}' is no trade (NpcTrades)");
                return new Instruction { Op = Op.Trade, Line = line, Name = trade };
            }

            // Poffins (plan 06 · R14c): can the player cook, cook, is there room, and a Poffin given
            case "poffin":
            {
                string what = r.OneOf("check", "cook", "room", "give");
                if (what != "give") return new Instruction { Op = Op.Poffin, Line = line, Name = what };
                var numbers = new List<int>();
                for (int k = 0; k < 6; k++)
                {
                    int n = r.Int(k < 5 ? "a flavour, 0 to 255" : "a smoothness, 0 to 255");
                    if (n is < 0 or > 255) throw r.Error($"{n} is no byte: a Poffin's flavours and smoothness are 0 to 255");
                    numbers.Add(n);
                }
                return new Instruction { Op = Op.Poffin, Line = line, Name = what, Numbers = numbers };
            }

            // The Day Care and Eggs (plan 06 · R15): its state, whether a Pokémon may be left, leaving, the fee and taking
            // back, the Egg handed over or kept, how the two get on; an Egg a person gives; an Egg hatching
            case "daycare":
            {
                string what = r.OneOf("state", "check", "leave", "take", "grown", "egg", "keep", "compatibility");
                if (what is not ("take" or "grown")) return new Instruction { Op = Op.DayCare, Line = line, Name = what };
                int place = r.Int("a place of the Day Care, 0 or 1");
                if (place is < 0 or >= Models.DayCare.Places) throw r.Error("the Day Care's places are 0 and 1");
                return new Instruction { Op = Op.DayCare, Line = line, Name = what, Number = place };
            }
            case "giveegg":
            {
                string species = r.Text("a species' name");
                // (plan 40 · GE7: a gift's Egg in its ball, with its ribbon and the name of who sent it)
                string ball = "", ribbon = "", sender = "";
                while (r.More)
                {
                    switch (r.OneOf("in", "ribbon", "from"))
                    {
                        case "in": ball = EventBall(r); break;
                        case "ribbon": ribbon = EventRibbon(r); break;
                        default: sender = r.Text("who sends it"); break;
                    }
                }
                return new Instruction { Op = Op.GiveEgg, Line = line, Name = species, Ball = ball, Ribbon = ribbon, Sender = sender };
            }
            case "hatch":
                return new Instruction { Op = Op.Hatch, Line = line };

            // The Super Contests (plan 06 · R17a): the rank, the quality and what is held are set, the Pokémon chosen is
            // checked, the contest is held, and what a win gave is asked; an accessory for the Fashion Case
            case "supercontest":
            {
                string what = r.OneOf("rank", "category", "mode", "check", "enter", "ribbon", "prize", "photo", "fashioncase");
                string value = what switch
                {
                    "rank" => r.OneOf("normal", "great", "ultra", "master"),
                    "category" => r.OneOf("cool", "beauty", "cute", "smart", "tough"),
                    "mode" => r.OneOf("official", "visual", "dance", "acting"),
                    _ => ""
                };
                return new Instruction { Op = Op.SuperContest, Line = line, Name = what, Other = value };
            }
            case "giveaccessory":
            {
                string accessory = r.Text("an accessory's name");
                int count = r.More ? r.Int("how many") : 1;
                if (count < 1) throw r.Error("an accessory is given one at a time at least");
                return new Instruction { Op = Op.GiveAccessory, Line = line, Name = accessory, Number = count };
            }

            // Ball Capsules, Seals and ribbons (plan 06 · R17b): Seals into the Seal Case (one kind, the five that come
            // with the case, an Unown's own), the count of kinds, the capsules' screen and the market's stall; a ribbon
            // asked of or put on the first of the team that is no Egg, the team's count, and what earns one
            case "seal":
            {
                string what = r.OneOf("give", "first", "unown", "count", "capsules", "shop");
                if (what != "give") return new Instruction { Op = Op.Seal, Line = line, Name = what };
                string seal = r.Text("a Seal's name");
                if (Data.SealDatabase.Seal(seal) == 0) throw r.Error($"there is no Seal '{seal}'");
                int count = r.More ? r.Int("how many") : 1;
                if (count < 1) throw r.Error("a Seal is given one at a time at least");
                return new Instruction { Op = Op.Seal, Line = line, Name = what, Other = seal, Number = count };
            }
            case "ribbon":
            {
                string what = r.OneOf("give", "has", "count", "offer", "effort", "feeling");
                if (what is not ("give" or "has")) return new Instruction { Op = Op.Ribbon, Line = line, Name = what };
                string ribbon = r.Word("a ribbon (RIBBON_ALERT), 'today' or 'offer'");
                if (ribbon is not ("today" or "offer") && Data.RibbonDatabase.Get(ribbon) == null) throw r.Error($"there is no ribbon '{ribbon}'");
                return new Instruction { Op = Op.Ribbon, Line = line, Name = what, Other = ribbon };
            }
            case "underground":
            {
                string what = r.OneOf("give", "room", "flags", "down", "up", "mine", "hiker", "pc", "bag", "cantake", "takeflag", "home");
                if (what != "give") return new Instruction { Op = Op.Underground, Line = line, Name = what };
                string kind = r.OneOf("trap", "sphere", "good");
                if (kind == "sphere")
                {
                    string colour = r.OneOf("prism", "pale", "red", "blue", "green");
                    int size = r.More ? r.Int("the Sphere's size") : 1;
                    if (size is < 1 or > 99) throw r.Error("a Sphere's size is 1 to 99");
                    return new Instruction { Op = Op.Underground, Line = line, Name = what, Other = kind, Partner = colour, Number = size };
                }
                string thing = r.Text(kind == "trap" ? "a trap's name" : "a good's name");
                if (kind == "trap" ? Data.UndergroundDatabase.TrapId(thing) == 0 : Data.UndergroundDatabase.GoodId(thing) == 0) throw r.Error($"there is no {kind} '{thing}'");
                return new Instruction { Op = Op.Underground, Line = line, Name = what, Other = kind, Partner = thing };
            }
            // A die of the script's own (plan 01 · M11; the original's GetRandom): RESULT is 0 to one less than its sides
            case "random":
            {
                // "random seen" draws a species the Pokédex has seen (plan 19 · K9, part c: FireRed's
                // SampleResortGorgeousMon, which Selphy's wish is drawn with): RESULT is its National Pokédex number
                if (r.PeekWord("seen")) { r.Next(); return new Instruction { Op = Op.Random, Line = line, Name = "seen" }; }
                int sides = r.Int("how many sides the die has");
                if (sides < 1) throw r.Error("a die has at least one side");
                return new Instruction { Op = Op.Random, Line = line, Number = sides };
            }

            // The Pokédex is told of a Pokémon the story shows (plan 02 · S12; the original's SetSpeciesSeen)
            case "seen":
                return new Instruction { Op = Op.Seen, Line = line, Name = r.Text("a species' name") };

            // Fossils, the Game Corner, the Villa and Pal Park (plan 06 · R16b)
            case "fossil":
            {
                string what = r.OneOf("count", "choose", "revive", "ready", "give");
                // A Pokémon handed over at another game's level (plan 19 · K6, part b: FireRed's lab on Cinnabar Island gives level 5)
                int level = what == "give" && r.PeekNumber ? r.Int("a level") : 0;
                if (level is < 0 or > 100) throw r.Error("a level is 1 to 100");
                return new Instruction { Op = Op.Fossil, Line = line, Name = what, Number = level };
            }
            case "gamecorner":
            {
                string what = r.OneOf("coins", "room", "add", "take", "slots", "reels", "prize", "buy", "hiddenpower", "cointrick", "wheel", "cards", "pokemon", "pay");
                bool machine = what is "slots" or "reels";
                int number = what is "room" or "add" or "take" or "slots" or "reels" or "wheel" ? r.Int(machine ? "a machine, 0 to 11" : "a number of coins") : 0;
                if (machine && number is < 0 or > 11) throw r.Error("the Game Corner's machines are 0 to 11");
                if (what == "wheel" && number is not (1 or 3)) throw r.Error("Mauville's roulette tables take a wager of 1 or 3 Coins");
                if (!machine && number < 0) throw r.Error("coins are counted from nought up");
                // The exchange's list: Veilstone's, or another Game Corner's by its name (plan 21 · H3, part c: mauville)
                string list = what == "prize" && r.PeekWord("mauville") ? r.OneOf("mauville")
                    : what == "prize" && r.PeekWord("goldenrod") ? r.OneOf("goldenrod") : "";
                return new Instruction { Op = Op.GameCorner, Line = line, Name = what, Number = number, Other = list };
            }
            case "villa":
            {
                string what = r.OneOf("catalogue", "choose", "is", "buy", "furnish", "owned");
                if (what is not ("choose" or "is")) return new Instruction { Op = Op.Villa, Line = line, Name = what };
                string piece = r.Text("a piece of furniture");
                if (Models.VillaRules.Find(piece) == null) throw r.Error($"the Villa's catalogue has no '{piece}'");
                return new Instruction { Op = Op.Villa, Line = line, Name = what, Other = piece };
            }
            case "palpark":
            {
                string what = r.OneOf("check", "start", "go", "active", "caught", "points", "record", "prize", "store", "end");
                string which = what == "points" ? r.OneOf("catching", "time", "type", "total") : "";
                return new Instruction { Op = Op.PalPark, Line = line, Name = what, Other = which };
            }

            // The Battle Frontier's frame and the Battle Tower (plan 06 · R18a)
            case "frontier":
            {
                string what = r.OneOf("points", "give", "prize", "buy", "records", "print", "judge", "judged", "save", "prints", "printed");
                string which = what switch
                {
                    // (and the later places' counters, plan 37's decision 6: Kiloude City's Battle Maison)
                    "prize" => r.OneOf("items", "machines", "training", "tera", "maison", "tree"),
                    "records" or "print" or "printed" => r.OneOf("tower", "factory", "hall", "castle", "arcade"),
                    "judged" => r.OneOf("total", "best"),
                    _ => ""
                };
                int points = what == "give" ? r.Int("a number of Battle Points") : 0;
                if (points < 0) throw r.Error("Battle Points are counted from nought up");
                return new Instruction { Op = Op.Frontier, Line = line, Name = what, Other = which, Number = points };
            }
            case "tower":
            {
                string what = r.OneOf("enough", "begin", "choose", "check", "enter", "pause", "resume", "forfeit", "end", "active", "paused", "mode",
                    "next", "opponent", "battle", "won", "sevens", "streak", "palmer", "beat", "brain", "finish", "points", "beaten", "ribbon", "trophy",
                    "waiting", "partners", "tells", "partner", "partnered", "door", "salon");
                string which = what switch
                {
                    "begin" => r.OneOf("single", "double", "multi"),
                    "beat" => r.OneOf("silver", "gold"),
                    "finish" => r.OneOf("won", "lost"),
                    "ribbon" => r.OneOf("palmer", "streak"),
                    _ => ""
                };
                int number = what is "tells" or "partner" ? r.Int("a partner of the Battle Salon, 0 to 4") : what == "enough" && r.More ? r.Int("how many Pokémon") : 0;
                if (what is "tells" or "partner" && number is < 0 or > 4) throw r.Error("the Battle Salon's partners are 0 to 4");
                if (what == "enough" && number is < 0 or > 4) throw r.Error("a challenge takes two, three or four Pokémon");
                return new Instruction { Op = Op.Tower, Line = line, Name = what, Other = which, Number = number };
            }

            // What a room's people ask of the team (plan 01 · M11, part b)
            case "team":
            {
                string what = r.OneOf("befriend", "level", "combee", "condition", "massage", "first", "nature", "size", "record", "holds", "deoxys", "ends", "rotom", "fliers");
                return what switch
                {
                    // Plan 20 · J15, part n: Silph Co.'s Rotom room, an appliance by its word (`team rotom heat`)
                    "rotom" => new Instruction { Op = Op.Team, Line = line, Name = what, Other = r.OneOf(PokemonPlatinumEngine.Models.FormChanges.ApplianceWords.ToArray()) },
                    // Plan 21 · H12, part a: the Sealed Chamber's riddle (CheckRelicanthWailord): the first of the team of one
                    // species and the last of another, `team ends "Wailord" "Relicanth"`
                    "ends" => new Instruction { Op = Op.Team, Line = line, Name = what, Other = r.Text("the first Pokémon's species"), Record = r.Text("the last Pokémon's species") },
                    // Plan 06 · R29: Veilstone City's meteorites (ChangeDeoxysForm): "Deoxys" is its Normal Forme
                    "deoxys" => new Instruction { Op = Op.Team, Line = line, Name = what, Other = DeoxysForm(r) },
                    // Plan 01 · M11, part c: a nature by its name, and a size beside the record ("team size keep" keeps it)
                    "nature" => new Instruction { Op = Op.Team, Line = line, Name = what, Other = NatureWord(r) },
                    "size" => SizeLine(r, what, line),
                    // Plan 19 · K9, part c: the size a measurer's record comes to, with no Pokémon measured
                    // (FireRed's GetHeracrossSizeRecordInfo), for the chart on their wall
                    "record" => new Instruction { Op = Op.Team, Line = line, Name = what, Other = r.Text("a species' name"), Record = r.PeekVariable ? r.Variable(writable: false) : "" },
                    // Plan 02 · S15, part c: whether anyone of the team holds an item (the Adamant and Lustrous Orbs)
                    "holds" => new Instruction { Op = Op.Team, Line = line, Name = what, Other = r.Text("an item's name") },
                    "befriend" => new Instruction { Op = Op.Team, Line = line, Name = what, Number = r.Int("how much friendlier") },
                    "level" => new Instruction { Op = Op.Team, Line = line, Name = what, Other = r.Variable(writable: false) },
                    "condition" => new Instruction { Op = Op.Team, Line = line, Name = what, Other = r.OneOf("cool", "beauty", "cute", "smart", "tough") },
                    // "team massage room" asks; a bare "team massage" gives
                    "massage" => new Instruction { Op = Op.Team, Line = line, Name = what, Option = r.PeekWord("room") && r.OneOf("room") == "room" },
                    _ => new Instruction { Op = Op.Team, Line = line, Name = what }
                };
            }

            // The Battle Factory (plan 06 · R18b)
            case "factory":
            {
                string what = r.OneOf("begin", "enter", "pause", "resume", "forfeit", "end", "active", "paused", "way", "level", "battles", "next", "hint",
                    "rent", "draw", "trade", "stand", "head", "announce", "odds", "opponent", "battle", "won", "brain", "beat", "streak", "finish", "points", "beaten");
                string which = what switch
                {
                    "begin" => r.OneOf("single", "double") + " " + r.OneOf("50", "open"),
                    "beat" => r.OneOf("silver", "gold"),
                    "finish" => r.OneOf("won", "lost"),
                    _ => ""
                };
                return new Instruction { Op = Op.Factory, Line = line, Name = what, Other = which };
            }

            // The Hall of Fame's end (plan 02 · S14): the game saved unasked, as the original's ClearGame saves it, and the credits
            case "save":
                return new Instruction { Op = Op.Save, Line = line };
            case "credits":
                return new Instruction { Op = Op.Credits, Line = line };
            // Furniture and the programmes (plan 08 · P8): a television's programme, and a map on a wall
            case "broadcast":
            {
                // One programme by its name (the interview, plan 08 · P9), or whichever the die picks of those on
                string programme = r.More ? r.OneOf(Broadcasts.Names) : "";
                return new Instruction { Op = Op.Broadcast, Line = line, Name = programme };
            }
            // A phrase of two words (plan 08 · P9): asked for on the picker and kept by its name, in capitals as the
            // placeholder {phrase:NAME} that says it
            case "phrase":
            {
                string name = r.Identifier("the phrase's name");
                if (!System.Text.RegularExpressions.Regex.IsMatch(name, "^[A-Z][A-Z0-9_]*$")) throw r.Error($"'{name}' is not a phrase's name: capitals, digits and _ only");
                return new Instruction { Op = Op.Phrase, Line = line, Name = name };
            }
            case "townmap":
                return new Instruction { Op = Op.TownMap, Line = line };
            // The Seaside Cycling Road's timed run (plan 21 · H12, part k): "cyclingroad finish" judges the run just ended,
            // "cyclingroad record" reads the best (RESULT nought where there is none)
            case "cyclingroad":
                return new Instruction { Op = Op.CyclingRoad, Line = line, Name = r.OneOf(new[] { "finish", "record" }) };
            // What to do next (plan 12 · Q14): "objective roark", a name of Story/Objectives.cs
            case "objective":
            {
                string name = r.Identifier("the objective's name");
                if (Objectives.Find(name) == null) throw r.Error($"'{name}' is not an objective: Story/Objectives.cs lists them");
                return new Instruction { Op = Op.Objective, Line = line, Name = name };
            }
            // A request's step taken where no flag of the original marks it (plan 12 · Q21): "request poketch 2", an id of
            // Story/Requests.cs and the step's number, stored in VAR_REQUEST_<ID>
            case "request":
            {
                string name = r.Identifier("the request's id");
                if (Requests.Find(name) == null) throw r.Error($"'{name}' is not a request: Story/Requests.cs lists them");
                return new Instruction { Op = Op.Request, Line = line, Name = name, Number = r.Int("the step's number") };
            }
            // A trainer tip raised (plan 12 · Q23): "tip cutTree", an id of Models/Tips.cs
            case "tip":
            {
                string name = r.Identifier("the tip's id");
                if (Models.Tips.Find(name) == null) throw r.Error($"'{name}' is not a tip: Models/Tips.cs lists them");
                return new Instruction { Op = Op.Tip, Line = line, Name = name };
            }
            // A region's rival named on the introduction's keyboard (plan 18 · W4, part b)
            case "namerival":
                return new Instruction { Op = Op.NameRival, Line = line };
            // A floor switch of the Mossdeep Gym (plan 21 · H8): "rotatetiles yellow" moves whoever stands on the
            // colour's arrows a step round their loop and turns them a quarter
            case "rotatetiles":
                return new Instruction { Op = Op.RotateTiles, Line = line, Name = r.OneOf("yellow", "blue", "green", "purple", "red") };
            // The National Park's Bug-Catching Contest (plan 15 · E5)
            case "bugcontest":
                return new Instruction
                {
                    Op = Op.BugContest, Line = line,
                    Name = r.OneOf("open", "on", "start", "balls", "caught", "keep", "release", "judge", "results", "prize", "end")
                };
            // The Pokéathlon (plan 15 · E6)
            case "athlon":
            {
                string what = r.OneOf("able", "open", "points", "records", "shop", "earned");
                // `athlon earned <honour>` (plan 15, the bonuses): RESULT 1 where the halls' honour is earned
                string honour = what == "earned" ? r.OneOf("shoes", "clothes", "flag", "pokegear", "ball", "statue") : "";
                return new Instruction { Op = Op.Athlon, Line = line, Name = what, Other = honour };
            }
            // Shoal Cave's tide (plan 21 · H8, part d): FLAG_SYS_SHOAL_TIDE by the hour, when come in from outdoors
            case "tide":
                return new Instruction { Op = Op.Tide, Line = line };
            // A secret base (plan 21 · H10): made at the spot ahead and gone into, asked about, decorated, packed up, and
            // the decoration PC filled
            case "openbase":
            {
                string what = r.More ? r.OneOf("check", "kind", "enter", "pc", "packup", "visit", "give", "buy", "room") : "open";
                if (what is not ("give" or "buy" or "room")) return new Instruction { Op = Op.OpenBase, Line = line, Name = what };
                string decoration = r.Text("a decoration's name");
                if (Data.DecorationDatabase.Get(decoration) == null) throw r.Error($"there is no decoration '{decoration}'");
                return new Instruction { Op = Op.OpenBase, Line = line, Name = what, Other = decoration };
            }
            // The move tutors and the Move Deleter (plan 01 · M11, part d)
            case "tutor":
            {
                string what = r.OneOf("moves", "afford", "pay", "special", "dragons", "learn", "forget", "delete", "once", "remember");
                string which = what switch
                {
                    "moves" => r.Text("the place of a shard tutor"),
                    "special" => r.OneOf("dracometeor", "ultimate"),
                    // Plan 19 · K5: a tutor of Kanto's who teaches one move, once (MoveTutors.Kanto)
                    "once" => r.Text("the move a tutor of Kanto teaches"),
                    _ => ""
                };
                return new Instruction { Op = Op.Tutor, Line = line, Name = what, Other = which };
            }

            // After the Hall of Fame (plan 02 · S15): the Pokédex becomes the National one (SetNationalDexEnabled)
            case "nationaldex":
                return new Instruction { Op = Op.NationalDex, Line = line };
            // A number registered in the Pokégear's phone (plan 20 · J3), by the phone book's id
            case "addcontact":
                return new Instruction { Op = Op.AddContact, Line = line, Name = r.Identifier("a contact of the phone book") };
            // A stone panel of the Ruins of Alph put back together (plan 20 · J5), by its name
            case "puzzle":
                return new Instruction { Op = Op.Puzzle, Line = line, Name = r.Identifier("a panel of the Ruins of Alph") };

            // The Battle Hall and the Battle Castle (plan 06 · R18c)
            case "hall":
            {
                string what = r.OneOf("enough", "choose", "streak", "drop", "begin", "enter", "pause", "resume", "forfeit", "end", "active", "paused", "way",
                    "battles", "next", "nth", "round", "board", "argenta", "opponent", "battle", "won", "rank", "brain", "beat", "finish", "points", "beaten",
                    "keeper", "told", "fan", "fanstage", "visitors", "species");
                string which = what switch
                {
                    "enough" or "choose" or "streak" or "drop" or "begin" => r.OneOf("single", "double"),
                    "beat" => r.OneOf("silver", "gold"),
                    "finish" => r.OneOf("won", "lost"),
                    "told" => r.OneOf("total", "passed", "coming", "points"),
                    _ => ""
                };
                return new Instruction { Op = Op.Hall, Line = line, Name = what, Other = which };
            }
            case "castle":
            {
                string what = r.OneOf("enough", "choose", "check", "begin", "enter", "pause", "resume", "forfeit", "end", "active", "paused", "way",
                    "battles", "next", "nth", "round", "heard", "cp", "self", "foe", "pass", "passable", "draw", "darach", "announce", "opponent", "battle",
                    "won", "done", "fainted", "brain", "beat", "finish", "points", "beaten");
                string which = what switch
                {
                    "begin" => r.OneOf("single", "double"),
                    "beat" => r.OneOf("silver", "gold"),
                    "finish" => r.OneOf("won", "lost"),
                    _ => ""
                };
                return new Instruction { Op = Op.Castle, Line = line, Name = what, Other = which };
            }

            // The Battle Arcade (plan 06 · R18d)
            case "arcade":
            {
                string what = r.OneOf("enough", "choose", "check", "begin", "enter", "pause", "resume", "forfeit", "end", "active", "paused", "way",
                    "battles", "next", "nth", "round", "draw", "opponent", "board", "event", "side", "immune", "free", "skip", "battle", "won", "done",
                    "dahlia", "announce", "brain", "beat", "finish", "points", "beaten");
                string which = what switch
                {
                    "begin" => r.OneOf("single", "double"),
                    "beat" => r.OneOf("silver", "gold"),
                    "finish" => r.OneOf("won", "lost"),
                    _ => ""
                };
                return new Instruction { Op = Op.Arcade, Line = line, Name = what, Other = which };
            }

            // A friend's trainer of the friends' hall (plan 13 · V23)
            case "friend":
                return new Instruction { Op = Op.Friend, Line = line, Name = r.OneOf("beaten", "before", "after", "battle") };

            // The Global Link counter (plan 07 · O5, O7): "globallink check" sets RESULT 1 with a server set, "globallink" opens it
            case "globallink":
                return new Instruction { Op = Op.GlobalLink, Line = line, Name = r.More ? r.OneOf("check") : "" };

            // The Battle Subway (plan 22 · U6, part d)
            case "subway":
            {
                string what = r.OneOf("enough", "begin", "choose", "check", "active", "end", "next", "boss", "opponent", "battle", "won", "streak",
                    "best", "finish", "points", "mode", "foe", "super", "opened", "pause", "paused", "resume", "records", "friends", "partner",
                    "beaten", "print", "prize", "buy");
                string which = what switch
                {
                    "begin" => r.OneOf("single", "double", "multi", "supersingle", "superdouble", "supermulti"),
                    "partner" => r.OneOf("staff", "cheren", "bianca", "again"),
                    "opened" => r.OneOf("single", "double", "multi"),
                    "finish" => r.OneOf("won", "lost"),
                    "foe" => r.Text("a trainer's id"),
                    _ => ""
                };
                return new Instruction { Op = Op.Subway, Line = line, Name = what, Other = which };
            }

            // Treats (plan 35 · BN3): `treat bake` (three berries of the flavour the bag has most of), `treat bake spicy`
            // and the other four, `treat bake mixed` (one berry of each flavour), and `treat can` (RESULT 1 when a bake could be made)
            // Locked hearts (plan 44 · SD2): `heart has`, `heart give`, `heart spring`, `heart open`, `heart locked`,
            // `heart count`, `heart missed`, `heart opened`
            case "heart":
                return new Instruction { Op = Op.Heart, Line = line, Name = r.OneOf("has", "give", "spring", "open", "locked", "count", "missed", "opened", "courier") };

            // The photo tour (plan 44 · SD5, SD6): `tour ride "<course>"`, `tour open "<course>"`, `tour points`, `tour report`, `tour reward`
            case "tour":
            {
                string tourWord = r.OneOf("ride", "open", "points", "report", "reward");
                return new Instruction { Op = Op.Tour, Line = line, Name = tourWord, Other = tourWord is "ride" or "open" ? r.Text("a course's id") : null };
            }

            // The pasture (plan 35 · BN5): `pasture enter`, `pasture leave`, `pasture box` (with -1 or 1 to turn it), `pasture count`,
            // and of the Pokémon the script belongs to `pasture mood`, `find`, `pet`, `feed` (the item last chosen), `care`, `take`
            case "pasture":
            {
                string act = r.OneOf("enter", "leave", "box", "count", "mood", "find", "pet", "feed", "care", "take", "job");
                int turn = act == "box" && r.More ? r.Int("-1 or 1") : 0;
                return new Instruction { Op = Op.Pasture, Line = line, Name = act, Number = turn };
            }

            case "treat":
            {
                string what = r.OneOf("bake", "can");
                string flavour = r.More ? r.OneOf("spicy", "dry", "sweet", "bitter", "sour", "mixed", "most") : "most";
                return new Instruction { Op = Op.Treat, Line = line, Name = what, Other = flavour };
            }

            // The crafting kit and its pouch (plan 39 · CR1): `craft has`, `craft give kit`, `craft give "Soft Fluff" 3`,
            // `craft take "Soft Fluff" 3`, `craft count "Soft Fluff"`, `craft open`; a material by its id or its English name;
            // CR4's `craft tm`, CR3's apprentice's `craft tradable` and `craft trade`, CR6's `craft pantry` and `craft meal`
            case "raid":
            {
                // Galar's Max Raid dens (plan 33 · GL3): `raid den "rolling_1"`, `raid watts`, `raid fight`, `raid catch "Poké Ball"`, and `raid wish`, a Wishing Piece thrown into the den looked into
                string what = r.OneOf("den", "watts", "fight", "catch", "wish");
                if (what == "den") return new Instruction { Op = Op.Raid, Line = line, Name = what, Other = r.Text("a den's id") };
                if (what == "catch")
                {
                    string ball = r.Text("a Poké Ball");
                    if (ItemDatabase.Get(ball) is not { Pocket: ItemPocket.PokeBalls }) throw r.Error($"'{ball}' is no Poké Ball");
                    return new Instruction { Op = Op.Raid, Line = line, Name = what, Other = ball };
                }
                return new Instruction { Op = Op.Raid, Line = line, Name = what };
            }

            case "craft":
            {
                string what = r.OneOf("has", "give", "take", "count", "open", "tm", "tradable", "trade", "pantry", "meal", "gather", "cleared", "crosses");
                // A dish eaten there and then (plan 39 · CR6): `craft meal "beanHotpot"`, or with its grade, `craft meal "beanHotpot" superb`
                if (what == "meal")
                {
                    string dish = r.Text("a dish's id");
                    if (Models.Meals.Dish(dish) == null) throw r.Error($"'{dish}' is no dish (Data/recipes.json's kitchen)");
                    string grade = r.More ? r.OneOf("plain", "good", "great", "superb") : "great";
                    return new Instruction { Op = Op.Craft, Line = line, Name = what, Other = dish, Number = (int)System.Enum.Parse<Models.DishGrade>(grade, ignoreCase: true) };
                }
                // What a tree cut or a rock smashed leaves (plan 39 · CR2): `craft cleared tree`, `craft cleared rock`
                if (what == "cleared")
                    return new Instruction { Op = Op.Craft, Line = line, Name = what, Other = r.OneOf("tree", "rock") };
                if (what == "give" && r.PeekWord("kit"))
                {
                    r.Next();
                    return new Instruction { Op = Op.Craft, Line = line, Name = what, Other = "kit" };
                }
                if (what is "give" or "take" or "count")
                {
                    string material = r.Text("a material's name");
                    if (Models.MaterialTable.ByName(material) == null) throw r.Error($"'{material}' is no material (Data/materials.json)");
                    int many = what != "count" && r.More ? r.Int("how many") : 1;
                    if (many < 1) throw r.Error("a material is given or taken one at a time at least");
                    return new Instruction { Op = Op.Craft, Line = line, Name = what, Other = Models.MaterialTable.ByName(material)!.Id, Number = many };
                }
                return new Instruction { Op = Op.Craft, Line = line, Name = what };
            }

            // The training hall (plan 36 · TR2): `train open`, `train drill`, `train reset all`, `train reset attack`,
            // `train prize`; drill and reset act on the Pokémon the last `choosepokemon` chose
            case "train":
            {
                string what = r.OneOf("open", "drill", "reset", "prize", "peak", "tera");
                if (what is "peak" or "tera") return new Instruction { Op = Op.Train, Line = line, Name = what, Other = r.More ? r.OneOf("check") : "" };
                if (what != "reset") return new Instruction { Op = Op.Train, Line = line, Name = what };
                string stat = r.OneOf("all", "hp", "attack", "defense", "spatk", "spdef", "speed");
                return new Instruction { Op = Op.Train, Line = line, Name = what, Other = stat };
            }

            // The powers (plan 36 · TR8): `power level`, `power learn`, `power grant exp 2 10` (a kind, a level, minutes of play)
            case "power":
            {
                string what = r.OneOf("level", "learn", "grant");
                if (what != "grant") return new Instruction { Op = Op.Power, Line = line, Name = what };
                string kind = r.Text("a power's kind");
                if (!System.Enum.TryParse<Models.PowerKind>(kind, true, out _)) throw r.Error($"'{kind}' is no power (Models/Powers.cs's PowerKind)");
                int level = r.Int("a level");
                if (level < 1 || level > Models.PowerTable.TopLevel) throw r.Error($"a power's level is 1 to {Models.PowerTable.TopLevel}");
                int minutes = r.Int("minutes of play");
                if (minutes < 1) throw r.Error("a power lasts a minute at least");
                return new Instruction { Op = Op.Power, Line = line, Name = what, Other = kind, Number = level, Partner = minutes.ToString() };
            }

            // Black City's Black Tower (plan 22 · U10, the last part)
            case "blacktower":
                return new Instruction { Op = Op.BlackTower, Line = line, Name = r.OneOf("enough", "begin", "choose", "check", "active", "next", "area",
                    "top", "opponent", "battle", "won", "finish", "best", "cleared") };

            // The Trainer Hill's climb (plan 21, part l)
            case "trainerhill":
            {
                string what = r.OneOf("begin", "active", "mode", "floor", "laid", "opponent", "fight", "lost", "climb", "down", "prize", "record", "end");
                string which = what == "begin" ? r.OneOf("normal", "variety", "unique", "expert") : "";
                return new Instruction { Op = Op.TrainerHill, Line = line, Name = what, Other = which };
            }

            // Join Avenue's growth (plan 22 · U11): the day's visitors, the rank, a shop's stock by the rank
            case "joinavenue":
            {
                string what = r.OneOf("visit", "rank", "visitors", "next", "shop", "open", "dojo", "salon", "nursery", "raffle", "visitor");
                string which = what switch
                {
                    "shop" => r.OneOf("market", "flowers", "cafe", "stones", "vitamins", "antiques"),
                    "open" => r.OneOf("raffle", "dojo", "salon", "nursery", "antiques", "stones", "vitamins"),
                    "dojo" => r.OneOf("hp", "attack", "defense", "speed", "spatk", "spdef"),
                    "visitor" => r.OneOf("1", "2", "3"),
                    _ => ""
                };
                return new Instruction { Op = Op.JoinAvenue, Line = line, Name = what, Other = which };
            }

            // A Hidden Grotto (plan 22 · U11): the mouth stood before, what it holds, its Pokémon battled, its item taken
            case "grotto":
            {
                string what = r.OneOf("mouth", "which", "visit", "battle", "take");
                return new Instruction { Op = Op.Grotto, Line = line, Name = what };
            }

            // Hoenn's contests (plan 21 · H11): the rank and quality set, the Pokémon checked, the appeals held, what a win gave
            case "appeals":
            {
                string what = r.OneOf("rank", "category", "check", "enter", "ribbon", "prize", "painter", "hang", "records", "winner", "painting", "paintings", "view");
                string value = what switch
                {
                    "rank" => r.OneOf("normal", "super", "hyper", "master"),
                    "category" => r.OneOf("cool", "beauty", "cute", "smart", "tough"),
                    "records" => r.OneOf("entered", "won"),
                    "winner" => r.OneOf("1", "2", "3", "4", "5", "6"),
                    "painting" or "view" => r.OneOf("cool", "beauty", "cute", "smart", "tough"),
                    _ => ""
                };
                return new Instruction { Op = Op.Appeals, Line = line, Name = what, Other = value };
            }
            // The Battle Dome's tournament and its bracket (plan 14 · B8)
            case "bracket":
            {
                string what = r.OneOf("enough", "choose", "check", "begin", "active", "round", "show", "opponent", "pick", "battle", "won", "lost",
                    "finish", "points", "streak", "best", "end");
                string which = what == "finish" ? r.OneOf("won", "lost") : "";
                return new Instruction { Op = Op.Bracket, Line = line, Name = what, Other = which };
            }
            // The Lilycove Lady's favour (plan 21 · H11, part c)
            case "favorlady":
                return new Instruction { Op = Op.FavorLady, Line = line, Name = r.OneOf("which", "give", "new") };
            // The later games' battle places (plan 37 · FC2): the place, then the frame's word, then what the word takes
            case "facility":
            {
                string place = r.OneOf(Models.Facilities.FacilityPlaces.Names);
                string what = r.OneOf("enough", "size", "choose", "check", "way", "hard", "hardopen", "begin", "active", "paused", "resume",
                    "next", "host", "opponent", "partner", "battle", "won", "opened", "trophy", "points", "finish", "pause", "streak", "best", "latest",
                    "board", "end", "scouted", "scout", "pick", "picked", "cast", "partnercast",
                    "member", "visit", "visitor", "dress", "level",
                    "challenge", "climbing", "floor", "kind", "fight", "cleared", "lost", "climb", "prize", "record",
                    "cup", "cupcheck", "rent", "cupbegin", "cupnext", "cupbattle", "cupwon", "cupprize", "cupend",
                    "region", "roundbegin", "roundnext", "roundbattle", "roundwon", "roundend",
                    "test", "testbegin", "testnext", "testbattle", "testscore", "testend", "season", "ladderformat", "laddernext", "ladderbattle", "band", "testleft");
                string which = what switch
                {
                    "way" => r.OneOf(System.Linq.Enumerable.ToArray(Models.Facilities.FacilityPlaces.WaysOf(place))),
                    "hard" => r.OneOf("normal", "hard"),
                    "finish" => r.OneOf("won", "lost"),
                    // The Trainer Tower's four challenges (plan 37 · FC6)
                    "challenge" => r.OneOf("single", "double", "knockout", "mixed"),
                    // The stadium's cups and the regions with a Leaders' round (plan 37 · FC7)
                    "cup" => r.OneOf(System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(Models.Facilities.Cups.All, c => c.Id))),
                    "region" => r.OneOf(System.Linq.Enumerable.ToArray(Models.Facilities.LeadersRound.Regions)),
                    // The Battle Institute's test's format, and its ladder's choosing (plan 37 · FC8)
                    "test" => r.OneOf("single", "double"),
                    "choose" => r.More ? r.OneOf("ladder") : "",
                    _ => ""
                };
                // The Battle Tree's scouted partners (plan 23, Alola finished): pick's number, 0 the tree's own and a scouted
                // one's from 1; left out, the one 'scout' last named
                int number = what == "pick" ? (r.More ? r.Int("a scouted partner's number") : -1) : 0;
                return new Instruction { Op = Op.Facility, Line = line, Name = place, Other = what, Move = which, Number = number };
            }
            // The Battle Palace's and the Battle Arena's rounds (plan 14 · B9)
            case "venue":
            {
                string what = r.OneOf("enough", "choose", "check", "begin", "active", "next", "opponent", "fight", "won", "finish", "points", "streak", "best", "end",
                    "tent", "rent", "trade", "length");
                // Hoenn's Battle Tents (plan 21 · H3's last part): a round of three begun in a tent, Slateport's rentals and trades
                string which = what is "begin" or "streak" or "best" ? r.OneOf("palace", "arena") : what == "finish" ? r.OneOf("won", "lost")
                    : what == "tent" ? r.OneOf("verdanturf", "fallarbor", "slateport") : "";
                return new Instruction { Op = Op.Venue, Line = line, Name = what, Other = which };
            }
            // The Battle Pike's rooms and the Battle Pyramid's floors (plan 14 · B10)
            case "pike":
            {
                string what = r.OneOf("enough", "choose", "check", "begin", "active", "hint", "hintdoor", "door", "room", "opponents", "fight",
                    "queen", "status", "giver", "healed", "lost", "passed", "finish", "points", "streak", "best", "end");
                string which = what == "finish" ? r.OneOf("won", "lost") : what == "door" ? r.OneOf("0", "1", "2") : "";
                return new Instruction { Op = Op.Pike, Line = line, Name = what, Other = which };
            }
            case "pyramid":
            {
                string what = r.OneOf("enough", "choose", "check", "begin", "active", "enter", "arrive", "floor", "opponent", "fight", "wild", "light",
                    "find", "climb", "king", "lost", "finish", "points", "streak", "best", "end");
                string which = what == "finish" ? r.OneOf("won", "lost") : "";
                return new Instruction { Op = Op.Pyramid, Line = line, Name = what, Other = which };
            }
            // The World Tournament (plan 14 · B11)
            case "tournament":
            {
                string what = r.OneOf("events", "enough", "choose", "check", "begin", "titles", "active", "round", "show", "opponent", "battle", "won", "lost",
                    "finish", "points", "end", "rental");
                string which = what == "finish" ? r.OneOf("won", "lost") : "";
                return new Instruction { Op = Op.Tournament, Line = line, Name = what, Other = which };
            }
            // The Berry Blender and the Pokéblock Case (plan 21 · H11)
            case "pokeblock":
            {
                string what = r.OneOf("blend", "count", "full", "taste", "lady", "feeder", "place");
                int others = 0;
                if (what == "blend")
                {
                    others = r.Int("how many others sit at the blender, 1 to 3");
                    if (others is < 1 or > 3) throw r.Error("the blender seats one to three others");
                }
                return new Instruction { Op = Op.Pokeblock, Line = line, Name = what, Number = others };
            }

            // Amity Square's walk (plan 06 · R16c)
            case "amity":
                return new Instruction
                {
                    Op = Op.Amity, Line = line,
                    Name = r.OneOf("arrive", "may", "next", "walk", "leave", "walker", "grown", "find", "take", "gift", "give")
                };

            // The camera kit (plan 09 · L4): the cinema bars slid in or away, and the field slowed for a time
            case "letterbox":
            {
                bool on = r.OneOf("on", "off") == "on";
                return new Instruction { Op = Op.Letterbox, Line = line, Option = on, Seconds = r.More ? r.Seconds() : CameraRig.BarSeconds };
            }
            case "slowmo":
            {
                // slowmo <share> [seconds]: the field at a share of its pace, back by itself; a share of 1 ends it now
                float share = r.Number("a share of the field's pace", CameraRig.SlowestPace, 1f);
                float seconds = r.More ? r.Seconds() : CameraRig.SlowSeconds;
                if (seconds <= 0f) throw r.Error("slow motion lasts longer than nothing");
                return new Instruction { Op = Op.SlowMotion, Line = line, Factor = share, Seconds = seconds };
            }

            // The original's set pieces (plan 09 · L12): each is played where the player stands and waited for
            case "anim":
            {
                // anim heal | anim pc on|off | anim elevator up|down [hums] | anim halloffame
                switch (r.OneOf("heal", "pc", "elevator", "halloffame"))
                {
                    case "heal":
                        return new Instruction { Op = Op.Anim, Line = line, Piece = SetPiece.Heal };
                    case "pc":
                        return new Instruction { Op = Op.Anim, Line = line, Piece = r.OneOf("on", "off") == "on" ? SetPiece.PcOn : SetPiece.PcOff };
                    case "elevator":
                    {
                        var way = r.OneOf("up", "down") == "up" ? SetPiece.ElevatorUp : SetPiece.ElevatorDown;
                        int hums = r.More ? r.Int("how many times the lift hums") : SetPieces.Hums;
                        if (hums < 1 || hums > SetPieces.MostHums) throw r.Error($"a lift hums 1 to {SetPieces.MostHums} times on a ride");
                        return new Instruction { Op = Op.Anim, Line = line, Piece = way, Number = hums };
                    }
                    default:
                        return new Instruction { Op = Op.Anim, Line = line, Piece = SetPiece.HallOfFame };
                }
            }
            // photograph: the screen as it stands, kept beside the player's screenshots (plan 20 · J13)
            case "photograph":
                return new Instruction { Op = Op.Photograph, Line = line };
            case "shake":
            case "flicker":
            {
                // shake <who> [times], flicker <who> [times]: the original's ShakeObject and FlickerObject
                string who = r.Who();
                bool shake = word == "shake";
                int times = r.More ? r.Int("how many times") : shake ? SetPieces.Shakes : SetPieces.Flickers;
                if (times < 1 || times > SetPieces.MostTimes)
                    throw r.Error(shake ? $"someone is shaken 1 to {SetPieces.MostTimes} times" : $"someone flickers 1 to {SetPieces.MostTimes} times");
                return new Instruction { Op = shake ? Op.Shake : Op.Flicker, Line = line, Name = who, Number = times };
            }

            // A scene that may be skipped (plan 12 · Q20): scene "Name" where it begins, endscene where it has been
            // played to its end. The name is the scene's own in its file, which with the file is its key in a save
            case "scene":
            {
                string scene = r.Text("the scene's name");
                if (!SceneName.IsMatch(scene))
                    throw r.Error($"'{scene}' is no name for a scene: one to {MostSceneName} letters, digits, spaces, hyphens, apostrophes or underscores, beginning with a letter or a digit");
                return new Instruction { Op = Op.Scene, Line = line, Name = scene };
            }
            case "endscene":
                return new Instruction { Op = Op.EndScene, Line = line };

            // The charms of a Pokédex filled (plan 40 · GE5): "charm due" how many are earned and not in the bag, "charm give" the first
            case "records":
                return new Instruction { Op = Op.Records, Line = line, Name = r.OneOf("mix", "bases", "count") };
            case "charm":
                return new Instruction { Op = Op.Charm, Line = line, Name = r.OneOf("due", "give") };

            // The gift box (plan 40 · GE7): "gift due" how many wait, "gift open" the box, "gift give" the gift chosen, "gift words
            // NAME" the phrase NAME told, "gift backdrop "Lantern"" a backdrop into the Fashion Case, "gift waiting" what waits for room
            case "gift":
            {
                string what = r.OneOf("due", "open", "give", "words", "backdrop", "waiting");
                if (what == "words") return new Instruction { Op = Op.Gift, Line = line, Name = what, Other = r.Identifier("the phrase's name") };
                if (what != "backdrop") return new Instruction { Op = Op.Gift, Line = line, Name = what };
                string backdrop = r.Text("a backdrop's name");
                if (Data.SuperContestDatabase.Backdrop(backdrop) < 0) throw r.Error($"there is no backdrop '{backdrop}'");
                return new Instruction { Op = Op.Gift, Line = line, Name = what, Other = backdrop };
            }
            // The stage show (plan 42 · PS1, PS2): "stageshow trunk" the owner's gift, "stageshow has" whether it was given,
            // "stageshow dress" the dressing room, "stageshow perform" a show on the stage, "stageshow fan fan_1" a fan's gift
            case "studio":
            {
                // The film studio (plan 42 · PS4): "studio shoot "emptySeat"", "studio open|stars "id"", "studio rank"
                string what = r.OneOf("shoot", "open", "stars", "rank", "premiere");
                if (what == "rank") return new Instruction { Op = Op.Studio, Line = line, Name = what };
                string film = r.Text("a film's id");
                if (Models.Studio.StudioFilms.Get(film) == null) throw r.Error($"'{film}' is no film of the studio's");
                return new Instruction { Op = Op.Studio, Line = line, Name = what, Other = film };
            }
            case "stageshow":
            {
                string what = r.OneOf("trunk", "has", "dress", "perform", "fan");
                if (what != "fan") return new Instruction { Op = Op.StageShow, Line = line, Name = what };
                string fan = r.Word("a fan's key");
                if (!Models.StageShow.ShowFans.Keys.Contains(fan)) throw r.Error($"'{fan}' is no fan of the theatre's");
                return new Instruction { Op = Op.StageShow, Line = line, Name = what, Other = fan };
            }
            case "circuit":
            {
                // The Laurel Circuit (plan 41 · EG4): `circuit begin|running|ready|level|stamps|won|regions`, and
                // `circuit final <Region>` / `circuit stamped <Region>` about one region
                string what = r.OneOf("begin", "running", "ready", "level", "stamps", "won", "regions", "final", "stamped", "count");
                string region = "";
                if (what is "final" or "stamped")
                {
                    region = r.Word("a region's id");
                    if (RegionDatabase.Get(region) == null) throw r.Error($"there is no region '{region}'");
                }
                return new Instruction { Op = Op.Circuit, Line = line, Name = what, Other = region };
            }
            case "deduce":
            {
                // A case's deduction (plan 41 · EG7): `deduce sinnoh 1`
                string id = r.Word("a case's id");
                if (Cases.Find(id) is not { } found) throw r.Error($"there is no case '{id}'");
                int n = r.Int("the deduction's number");
                if (n < 1 || n > found.Deductions.Count) throw r.Error($"the case '{id}' has deductions 1 to {found.Deductions.Count}");
                return new Instruction { Op = Op.Deduce, Line = line, Name = id, Number = n };
            }

            // Pennant Hall (plan 41 · EG3): `plaza open`, `rank`, `next`, `coins`, `pitches`, `pitch 2`, `give 10`,
            // `spend 10`, `guests`, `stall`, `shop`, `greet`, `battle`
            case "plaza":
            {
                string what = r.OneOf("open", "rank", "next", "coins", "pitches", "pitch", "give", "spend", "guests", "stall", "shop", "greet", "battle");
                if (what is "pitch" or "give" or "spend")
                {
                    int n = r.Int(what == "pitch" ? "a pitch's number" : "how many pennants");
                    if (n < 1) throw r.Error(what == "pitch" ? "the pitches are counted from 1" : "pennants are given or spent one at least");
                    return new Instruction { Op = Op.Plaza, Line = line, Name = what, Number = n };
                }
                return new Instruction { Op = Op.Plaza, Line = line, Name = what };
            }

            // The Week's Work (plan 41 · EG6): `missions read`, `open`, `begin 4`, `waiting`, `claim`
            case "missions":
            {
                string what = r.OneOf("read", "open", "begin", "waiting", "claim");
                int n = what == "begin" ? r.Int("a mission's place on the board") : 0;
                if (what == "begin" && n < 1) throw r.Error("the board's missions are counted from 1");
                return new Instruction { Op = Op.Missions, Line = line, Name = what, Number = n };
            }

            case "script":
            case "label":
                throw r.Error($"'{word}' begins a line of its own");
            default:
                throw r.Error($"there is no command '{word}'");
        }
    }

    // ------------------------------------------------------------------ a condition

    private static Condition ConditionOf(Reader r)
    {
        bool negated = false;
        while (r.PeekIs("not"))
        {
            r.Next();
            negated = !negated;
        }

        string word = r.Word("what to ask");
        switch (word)
        {
            case "flag":
                return new Condition { Query = Query.Flag, Negated = negated, Name = r.Flag() };
            case "var":
            {
                string name = r.Variable(writable: false);
                var compare = r.Compare();
                if (r.PeekNumber) return new Condition { Query = Query.Var, Negated = negated, Name = name, Compare = compare, Number = r.Int("a value") };
                return new Condition { Query = Query.Var, Negated = negated, Name = name, Compare = compare, Other = r.Variable(writable: false) };
            }
            case "badge":
            {
                string badge = r.Badge();
                return new Condition { Query = Query.Badge, Negated = negated, Name = badge, Other = r.BadgeRegion(badge) };
            }
            case "badges":
            {
                // ...of another region by its name (plan 20 · J15: Mt. Silver's guard counts Kanto's eight beside Johto's)
                if (!r.PeekWord("of")) return Counted(Query.Badges);
                r.Next();
                string region = r.Word("a region");
                if (RegionDatabase.Get(region) is not { } named) throw r.Error($"'{region}' is no region");
                var compare = r.Compare();
                return new Condition { Query = Query.Badges, Negated = negated, Name = named.Id, Compare = compare, Number = r.Int("a number") };
            }
            case "movebadge":
            {
                string move = r.Text("a field move's name");
                if (FieldMoveRules.Of(move) == null) throw r.Error($"'{move}' is no field move");
                return new Condition { Query = Query.MoveBadge, Negated = negated, Name = move };
            }
            case "item":
            {
                string item = r.Text("an item's name");
                if (!r.PeekCompare) return new Condition { Query = Query.Item, Negated = negated, Name = item, Compare = Compare.GreaterOrEqual, Number = 1 };
                var compare = r.Compare();
                return new Condition { Query = Query.Item, Negated = negated, Name = item, Compare = compare, Number = r.Int("how many") };
            }
            case "party":
                return Counted(Query.Party);
            case "friendship":
                return Counted(Query.Friendship);
            case "granted":
                return new Condition { Query = Query.Granted, Negated = negated, Name = r.Text("a field move's name") };
            case "knows":
                return new Condition { Query = Query.Knows, Negated = negated, Name = r.Text("a move's name") };
            case "has":
            {
                // ...the species a variable holds, by its National Pokédex number (plan 19 · K9, part c: Selphy asks
                // for one she has heard of and then whether the player has brought it)
                if (r.PeekWord("var")) { r.Next(); return new Condition { Query = Query.Has, Negated = negated, Other = r.Variable(writable: false) }; }
                string species = r.Text("a species' name");
                // ...met at a place (plan 20 · J8, part b: Baoba's tests ask for a Pokémon caught in the Safari Zone)
                if (!r.PeekWord("met")) return new Condition { Query = Query.Has, Negated = negated, Name = species };
                r.Next();
                return new Condition { Query = Query.Has, Negated = negated, Name = species, Place = r.Text("a place's name") };
            }
            case "yes":
                return new Condition { Query = Query.Yes, Negated = negated };
            case "no":
                return new Condition { Query = Query.No, Negated = negated };
            case "won":
                return new Condition { Query = Query.Won, Negated = negated };
            case "lost":
                return new Condition { Query = Query.Lost, Negated = negated };
            case "result":
                return Counted(Query.Result);
            case "defeated":
                return new Condition { Query = Query.Defeated, Negated = negated, Name = r.Text("a trainer's id, or self") };
            case "rematch":
                return new Condition { Query = Query.Rematch, Negated = negated, Name = r.Text("self") };
            case "taken":
                return new Condition { Query = Query.Taken, Negated = negated, Name = r.Text("an item's id") };
            case "starter":
                return new Condition { Query = Query.Starter, Negated = negated, Name = r.Text("a species' name") };
            case "money":
                return Counted(Query.Money);
            case "facing":
                return new Condition { Query = Query.Facing, Negated = negated, Name = r.Direction().ToString() };
            case "boy":
                return new Condition { Query = Query.Boy, Negated = negated };
            case "girl":
                return new Condition { Query = Query.Girl, Negated = negated };
            case "poketch":
                return new Condition { Query = Query.Poketch, Negated = negated };
            case "poketchapp":
                return new Condition { Query = Query.PoketchApp, Negated = negated, Name = r.Enum<Models.PoketchApp>("a Pokétch app").ToString() };
            case "pokerus":
                return new Condition { Query = Query.Pokerus, Negated = negated };
            case "safari":
                return new Condition { Query = Query.Safari, Negated = negated };
            case "region":
                return new Condition { Query = Query.Region, Negated = negated, Name = r.Text("a region") };
            case "subject":
                return new Condition { Query = Query.Subject, Negated = negated, Name = r.Text("someone's id") };
            case "partner":
                return new Condition { Query = Query.Partner, Negated = negated };
            case "time":
                return new Condition { Query = Query.Time, Negated = negated, Name = r.Enum<Core.TimeOfDay>("a time of day").ToString() };
            case "weekday":
                return new Condition { Query = Query.Weekday, Negated = negated, Name = r.Enum<System.DayOfWeek>("a day of the week").ToString() };
            // The National Pokédex is the player's (plan 02 · S14; the original's GetNationalDexEnabled)
            case "nationaldex":
                return new Condition { Query = Query.NationalDex, Negated = negated };
            case "objective":
                return new Condition { Query = Query.Objective, Negated = negated };
            // The chosen Pokémon's master is beaten already (plan 14 · B15, the Masters' Hall)
            case "titled":
                return new Condition { Query = Query.Titled, Negated = negated };
            // Mirage Island stands today for this team (plan 21 · H12, part h; Emerald's IsMirageIslandPresent)
            case "mirage":
                return new Condition { Query = Query.Mirage, Negated = negated };
            case "modern":
                return new Condition { Query = Query.Modern, Negated = negated };
            // The PC offers the pasture (plan 35 · BN5)
            case "pasture":
                return new Condition { Query = Query.Pasture, Negated = negated };
            // The diploma of a Pokédex is held (plan 40 · GE4)
            case "diploma":
            {
                string named = r.Word("a region, National or Ours");
                if (Models.Pokedex.DiplomaDexes(named).Count == 0) throw r.Error($"'{named}' is no region, Pokédex, National or Ours");
                return new Condition { Query = Query.Diploma, Negated = negated, Name = named };
            }
            // The Pokédex holds this species caught (plan 40 · GE6: an older save's legendaries marked caught)
            case "caught":
                return new Condition { Query = Query.Caught, Negated = negated, Name = r.Text("a species' name") };
            // The phrase kept under a name is these two words (plan 21 · H12, part k: the Berry Master's wife)
            case "phrase":
            {
                string name = r.Identifier("the phrase's name");
                string first = r.Text("a word's key (feelings:great)"), second = r.Text("a word's key (people:rival)");
                foreach (string key in new[] { first, second })
                    if (!Data.PhraseGroups.IsWord(key)) throw r.Error($"'{key}' is no word of the phrase picker: a group's tag, ':' and the word's key");
                return new Condition { Query = Query.Phrase, Negated = negated, Name = name, Other = first + " " + second };
            }
            // A rule of the run chosen at NEW GAME (plan 13 · V5)
            case "rule":
            {
                string rule = r.Text("a rule of the run").ToLowerInvariant();
                if (!Models.RunRules.RuleNames.Contains(rule)) throw r.Error($"'{rule}' is no rule of a run ({string.Join(", ", Models.RunRules.RuleNames)})");
                return new Condition { Query = Query.Rule, Negated = negated, Name = rule };
            }
            default:
                throw r.Error($"'{word}' is nothing an 'if' can ask");
        }

        Condition Counted(Query query)
        {
            var compare = r.Compare();
            return new Condition { Query = query, Negated = negated, Compare = compare, Number = r.Int("a number") };
        }
    }

    // ------------------------------------------------------------------ words

    private static List<Token> Tokens(string line, string file, int lineNumber)
    {
        var tokens = new List<Token>();
        int i = 0;
        while (i < line.Length)
        {
            char c = line[i];
            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }
            if (c == '#') break;

            if (c == '"')
            {
                var text = new StringBuilder();
                bool closed = false;
                i++;
                while (i < line.Length)
                {
                    c = line[i++];
                    if (c == '\\' && i < line.Length)
                    {
                        char escaped = line[i++];
                        text.Append(escaped == 'n' ? '\n' : escaped);
                    }
                    else if (c == '"')
                    {
                        closed = true;
                        break;
                    }
                    else text.Append(c);
                }
                if (!closed) throw new ScriptException($"{file}.txt({lineNumber}): a text is missing its closing quote.");
                tokens.Add(new Token(text.ToString(), true));
                continue;
            }

            int start = i;
            while (i < line.Length && !char.IsWhiteSpace(line[i]) && line[i] != '"' && line[i] != '#') i++;
            tokens.Add(new Token(line[start..i], false));
        }
        return tokens;
    }

    private sealed class Reader
    {
        private readonly string file;
        private readonly List<Token> tokens;
        private int at;

        public int Line { get; }

        public Reader(string file, int line, List<Token> tokens)
        {
            this.file = file;
            this.tokens = tokens;
            Line = line;
        }

        public bool More => at < tokens.Count;
        public bool PeekQuoted => More && tokens[at].Quoted;
        public bool PeekIs(string word) => More && !tokens[at].Quoted && tokens[at].Text == word;
        public bool PeekNumber => More && !tokens[at].Quoted && int.TryParse(tokens[at].Text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _);
        public bool PeekVariable => More && !tokens[at].Quoted && (VariableName.IsMatch(tokens[at].Text) || BuiltInVariables.Contains(tokens[at].Text));
        public bool PeekDirection => More && !tokens[at].Quoted && DirectionOf(tokens[at].Text) != null;
        public bool PeekSeconds => More && !tokens[at].Quoted && float.TryParse(tokens[at].Text, NumberStyles.Float, CultureInfo.InvariantCulture, out _);

        /// <summary>Whether the next word is this one, unquoted.</summary>
        public bool PeekWord(string word) => More && !tokens[at].Quoted && tokens[at].Text == word;
        public bool PeekCompare => More && !tokens[at].Quoted && CompareOf(tokens[at].Text) != null;

        public ScriptException Error(string message) => new($"{file}.txt({Line}): {message}.");

        public void Next() => at++;

        private Token Take(string expected)
        {
            if (!More) throw Error($"the line ends where {expected} belongs");
            return tokens[at++];
        }

        public void End()
        {
            if (More) throw Error($"'{tokens[at].Text}' is more than the line needs");
        }

        /// <summary>A bare word: a command, a keyword.</summary>
        public string Word(string expected)
        {
            var token = Take(expected);
            if (token.Quoted) throw Error($"\"{token.Text}\" is a text where {expected} belongs");
            return token.Text;
        }

        public string Identifier(string expected)
        {
            string word = Word(expected);
            if (!ScriptParser.Identifier.IsMatch(word)) throw Error($"'{word}' is not a name: letters, digits and _ only");
            return word;
        }

        /// <summary>A script to call: a name, or <c>file.Name</c>.</summary>
        public string ScriptName()
        {
            string word = Word("a script's name");
            string[] parts = word.Split('.');
            if (parts.Length > 2 || parts.Any(p => !ScriptParser.Identifier.IsMatch(p))) throw Error($"'{word}' is not a script's name");
            return word;
        }

        /// <summary>A name that may have spaces: quoted, or one bare word.</summary>
        public string Text(string expected) => Take(expected).Text;

        public List<string> Texts(int least, int most, string expected)
        {
            var texts = new List<string>();
            while (More)
            {
                var token = tokens[at++];
                if (!token.Quoted) throw Error($"'{token.Text}' is not in quotes; {expected} is written in quotes");
                texts.Add(token.Text);
            }
            if (texts.Count < least) throw Error($"the line needs {expected}");
            if (texts.Count > most) throw Error($"the line has {texts.Count} texts and may have {most}");
            return texts;
        }

        public string OneOf(params string[] words)
        {
            string word = Word(string.Join(" or ", words.Select(w => $"'{w}'")));
            if (!words.Contains(word)) throw Error($"'{word}' is not {string.Join(" or ", words.Select(w => $"'{w}'"))}");
            return word;
        }

        public int Int(string expected)
        {
            string word = Word(expected);
            if (!int.TryParse(word, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int value)) throw Error($"'{word}' is not a number, and {expected} is one");
            return value;
        }

        public float Seconds()
        {
            string word = Word("a time in seconds");
            if (!float.TryParse(word, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) || value < 0f || value > 60f)
                throw Error($"'{word}' is not a time in seconds (0 to 60)");
            return value;
        }

        /// <summary>A number that may have a fraction, within its bounds: a zoom's factor, slow motion's share of the pace.</summary>
        public float Number(string expected, float least, float most)
        {
            string word = Word(expected);
            if (!float.TryParse(word, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) || value < least || value > most)
                throw Error($"'{word}' is not {expected} ({least.ToString(CultureInfo.InvariantCulture)} to {most.ToString(CultureInfo.InvariantCulture)})");
            return value;
        }

        public T Enum<T>(string expected) where T : struct, System.Enum
        {
            string word = Word(expected);
            if (int.TryParse(word, out _) || !System.Enum.TryParse(word, ignoreCase: true, out T value))
                throw Error($"'{word}' is not {expected}: {string.Join(", ", System.Enum.GetNames<T>().Select(n => n.ToLowerInvariant()))}");
            return value;
        }

        /// <summary>
        /// A badge's name, as some region's file lists it (plan 18 · W2): which region's it must be is the place's, told
        /// by <see cref="ScriptLibrary.Problems"/> and at run time by the host's region.
        /// </summary>
        public string Badge()
        {
            string word = Word("a badge");
            if (!RegionDatabase.All.Any(r => StoryState.FindBadge(r.Id, word) >= 0)) throw Error($"'{word}' is not a badge of any region");
            return word;
        }

        /// <summary>
        /// After a badge's name, <c>of &lt;region&gt;</c> where the badge is another region's than the place's (plan 20 ·
        /// J15, part b: HeartGold's Kanto, Johto's own maps, gives Kanto's Badges): the region's id, or null for none.
        /// </summary>
        public string? BadgeRegion(string badge)
        {
            if (!PeekWord("of")) return null;
            Next();
            string region = Word("a region");
            if (RegionDatabase.Get(region) is not { } named) throw Error($"'{region}' is no region");
            if (StoryState.FindBadge(named.Id, badge) < 0) throw Error($"{named.Name} has no badge '{badge}'");
            return named.Id;
        }

        public Direction Direction()
        {
            string word = Word("a direction");
            return DirectionOf(word) ?? throw Error($"'{word}' is not a direction: up, down, left, right");
        }

        public Compare Compare()
        {
            string word = Word("a comparison");
            return CompareOf(word) ?? throw Error($"'{word}' is not a comparison: ==, !=, <, <=, >, >=");
        }

        public string Flag()
        {
            string word = Word("a flag");
            if (!FlagName.IsMatch(word)) throw Error($"'{word}' is not a flag's name: FLAG_ and capitals");
            return word;
        }

        public string Variable(bool writable)
        {
            string word = Word("a variable");
            if (VariableName.IsMatch(word)) return word;
            if (BuiltInVariables.Contains(word))
            {
                if (writable) throw Error($"{word} is kept by the game: a script reads it and can't change it");
                return word;
            }
            throw Error($"'{word}' is not a variable's name: VAR_ and capitals, or one of {string.Join(", ", BuiltInVariables)}");
        }

        /// <summary>Someone in the field: <c>player</c>, <c>self</c> (whoever the script belongs to), or a person's id or name.</summary>
        public string Who()
        {
            var token = Take("who (player, self, or a person's name)");
            if (!token.Quoted && DirectionOf(token.Text) != null) throw Error($"'{token.Text}' is a direction where a person belongs: player, self, or a name");
            if (token.Text.Length == 0) throw Error("a person's name is empty");
            return token.Text;
        }

        /// <summary>
        /// The steps of a walk: directions, each with how many tiles when more than one, and <c>fast</c> for a run.
        /// <c>onspot</c> makes the next direction's steps on the spot, and <c>back</c> takes them keeping the facing
        /// (plan 11 · C11; the original's <c>WalkOnSpot</c> and <c>LockDir</c>).
        /// </summary>
        public (List<ScriptStep> Steps, bool Fast) Path()
        {
            var steps = new List<ScriptStep>();
            bool fast = false;
            while (More)
            {
                string word = Word("a direction");
                if (word == "fast")
                {
                    fast = true;
                    continue;
                }
                var kind = ScriptStepKind.Walk;
                if (word is "onspot" or "back")
                {
                    kind = word == "onspot" ? ScriptStepKind.OnSpot : ScriptStepKind.Back;
                    if (!More) throw Error($"'{word}' needs a direction after it: up, down, left, right");
                    string next = Word("a direction");
                    word = DirectionOf(next) != null ? next : throw Error($"'{word}' needs a direction after it, not '{next}': up, down, left, right");
                }
                var direction = DirectionOf(word) ?? throw Error($"'{word}' is not a step: up, down, left, right, each with a count, onspot or back before one, or fast");
                int count = PeekNumber ? Int("how many tiles") : 1;
                if (count is < 1 or > 64) throw Error("a walk goes 1 to 64 tiles one way");
                for (int i = 0; i < count; i++) steps.Add(new ScriptStep(direction, kind));
            }
            if (steps.Count == 0) throw Error("a walk needs steps: up, down, left, right");
            return (steps, fast);
        }
    }

    /// <summary>
    /// A staged battle's term on a <c>battle</c> or <c>wildbattle</c> line (docs/mechanics/rulings.md, "Staged battles"):
    /// <c>hidelevel</c>, <c>untouchable</c>, <c>passive</c>, or <c>endafter N</c> with its turns, each once.
    /// </summary>
    private static Battle.BattleTerms StagedTerm(Reader r, string term, Battle.BattleTerms so)
    {
        bool twice = term switch
        {
            "hidelevel" => so.HideLevel,
            "untouchable" => so.Untouchable,
            "passive" => so.Passive,
            _ => so.EndAfterTurns > 0
        };
        if (twice) throw r.Error($"'{term}' is said once");
        switch (term)
        {
            case "hidelevel": return so with { HideLevel = true };
            case "untouchable": return so with { Untouchable = true };
            case "passive": return so with { Passive = true };
        }
        int turns = r.Int("the turns it lasts");
        if (turns is < 1 or > 99) throw r.Error("a staged battle ends after 1 to 99 turns");
        return so with { EndAfterTurns = turns };
    }

    /// <summary>A nature by its name, in any case (<c>team nature serious</c>).</summary>
    /// <summary>A forme of Deoxys's by its name ("Deoxys-Speed"), or "Deoxys" for its Normal Forme.</summary>
    /// <summary>An event Pokémon's ball (plan 40 · GE7): an item of the Poké Balls' pocket.</summary>
    private static string EventBall(Reader r)
    {
        string ball = r.Text("a ball's name");
        if (Data.ItemDatabase.Get(ball) is not { } item || !item.Name.EndsWith("Ball", StringComparison.Ordinal)) throw r.Error($"'{ball}' is no ball");
        return item.Name;
    }

    /// <summary>An event Pokémon's ribbon (plan 40 · GE7): one of <c>ribbons.json</c>.</summary>
    private static string EventRibbon(Reader r)
    {
        string ribbon = r.Word("a ribbon (RIBBON_GIFT_DAY)");
        if (Data.RibbonDatabase.Get(ribbon) == null) throw r.Error($"there is no ribbon '{ribbon}'");
        return ribbon;
    }

    private static string DeoxysForm(Reader r)
    {
        string form = r.Text("a forme of Deoxys's");
        if (form != "Deoxys" && PokemonDatabase.Get("Deoxys")?.Form(form) == null) throw r.Error($"'{form}' is no forme of Deoxys's");
        return form;
    }

    /// <summary>
    /// <c>team size "Species" [VAR_RECORD]</c> and <c>team size keep [VAR_RECORD]</c> (plan 01 · M11, part c; plan 19 ·
    /// K9, part c): a measurer with a record of its own names the variable it keeps it in, as FireRed's three do; with
    /// no variable the record is the story's own (<see cref="Models.SizeContest.RecordVariable"/>).
    /// </summary>
    private static Instruction SizeLine(Reader r, string what, int line)
    {
        bool keep = r.PeekWord("keep");
        if (keep) r.Next();
        string species = keep ? "" : r.Text("a species' name");
        string record = r.PeekVariable ? r.Variable(writable: true) : "";
        return new Instruction { Op = Op.Team, Line = line, Name = what, Other = species, Option = keep, Record = record };
    }

    private static string NatureWord(Reader r)
    {
        string word = r.Word("a nature");
        if (!Enum.TryParse<Nature>(word, ignoreCase: true, out var nature) || !Enum.IsDefined(nature)) throw r.Error($"'{word}' is no nature");
        return nature.ToString();
    }

    private static Direction? DirectionOf(string word) => word switch
    {
        "up" => Direction.Up,
        "down" => Direction.Down,
        "left" => Direction.Left,
        "right" => Direction.Right,
        _ => null
    };

    private static Compare? CompareOf(string word) => word switch
    {
        "==" or "=" => Compare.Equal,
        "!=" => Compare.NotEqual,
        "<" => Compare.Less,
        "<=" => Compare.LessOrEqual,
        ">" => Compare.Greater,
        ">=" => Compare.GreaterOrEqual,
        _ => null
    };
}
