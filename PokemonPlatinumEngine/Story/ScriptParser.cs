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
        "CYCLING",
        "CAUGHT",
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
                return new Instruction { Op = Op.GivePokemon, Line = line, Name = species, Number = level };
            }
            case "givebadge":
                return new Instruction { Op = Op.GiveBadge, Line = line, Name = r.Badge() };
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
                while (r.More)
                {
                    switch (r.OneOf("canlose", "first", "and", "with", "as", "rematch", "inverse"))
                    {
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
                return new Instruction { Op = Op.Battle, Line = line, Name = who, Other = second, Partner = partner, PartnerById = byId, Option = mayLose, FirstBattle = first, AsTrainer = asTrainer, Rematch = rematch, Inverse = inverse };
            }
            // A Master Trainer's battle (plan 14 · B14): the one choosepokemon chose, alone, with the bag closed
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
                if (level is < 1 or > 100) throw r.Error("a Pokémon's level is from 1 to 100");
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
                bool noFleeing = r.More && r.OneOf("nofleeing") == "nofleeing";
                return new Instruction { Op = Op.WildBattle, Line = line, Name = species, Number = level, Option = noFleeing };
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
            case "fade":
            {
                bool toBlack = r.OneOf("out", "in") == "out";
                return new Instruction { Op = Op.Fade, Line = line, Option = toBlack, Seconds = r.More ? r.Seconds() : FadeSeconds };
            }
            case "wait":
                return new Instruction { Op = Op.Wait, Line = line, Seconds = r.Seconds() };
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
                switch (r.OneOf("pan", "release", "shake", "zoom", "follow", "path"))
                {
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
            case "fly":
                return new Instruction { Op = Op.Fly, Line = line };
            case "teleport":
                return new Instruction { Op = Op.Teleport, Line = line };
            case "escape":
                return new Instruction { Op = Op.Escape, Line = line };
            case "sweetscent":
                return new Instruction { Op = Op.SweetScent, Line = line };
            case "poketch":
                r.OneOf("on");
                return new Instruction { Op = Op.Poketch, Line = line };
            case "poketchapp":
                return new Instruction { Op = Op.PoketchApp, Line = line, Name = r.Enum<Models.PoketchApp>("a Pokétch app").ToString() };
            case "safari":
                return new Instruction { Op = Op.Safari, Line = line, Option = r.OneOf("start", "end") == "start" };

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
                return new Instruction { Op = Op.Wardrobe, Line = line, Name = r.More ? r.Text("a boutique's key") : "" };
            case "shop":
                return new Instruction { Op = Op.Shop, Line = line, Name = r.More ? r.Text("a counter's key") : "" };
            case "pc":
                // "pc halloffame" opens the Hall of Fame's records instead of the boxes (plan 06 · R12)
                return new Instruction { Op = Op.Pc, Line = line, Name = r.More ? r.OneOf("halloffame") : "" };
            case "halloffame":
                // "halloffame show" is the scene of the team's entry instead of the record (plan 02 · S14)
                return new Instruction { Op = Op.HallOfFame, Line = line, Name = r.More ? r.OneOf("show") : "" };
            case "travel":
                return new Instruction { Op = Op.Travel, Line = line };
            case "choosepokemon":
                // "choosepokemon "Bidoof"" lets only that species be chosen (plan 14 · B14: a Master Trainer's)
                return new Instruction { Op = Op.ChoosePokemon, Line = line, Name = r.More ? r.Text("a species' name") : "" };
            // Wild Pokémon (plan 06 · R13)
            case "honeytree":
                return new Instruction { Op = Op.HoneyTree, Line = line, Name = r.OneOf("status", "slather", "battle") };
            case "lottery":
                return new Instruction { Op = Op.Lottery, Line = line, Name = r.OneOf("draw", "boxed") };
            case "berry":
                return new Instruction { Op = Op.Berry, Line = line, Name = r.OneOf("status", "mulched", "water", "plant", "mulch", "pick", "berries", "mulches") };
            case "chooseitem":
                return new Instruction { Op = Op.ChooseItem, Line = line, Name = r.OneOf("berries", "mulch") };
            case "swarms":
                r.OneOf("on");
                return new Instruction { Op = Op.Swarms, Line = line };
            case "trophygarden":
                return new Instruction { Op = Op.TrophyGarden, Line = line };
            case "roamer":
            {
                r.OneOf("start");
                string species = r.Text("a roaming Pokémon's species");
                if (Overworld.Roamers.SlotOf(species) == null) throw r.Error($"'{species}' is no Pokémon that roams (Roamers.Slots)");
                return new Instruction { Op = Op.Roamer, Line = line, Name = species };
            }
            case "survivepoison":
                return new Instruction { Op = Op.SurvivePoison, Line = line, Name = r.Variable(writable: false) };

            case "trade":
            {
                string trade = r.Word("a trade");
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
                return new Instruction { Op = Op.GiveEgg, Line = line, Name = r.Text("a species' name") };
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
                int sides = r.Int("how many sides the die has");
                if (sides < 1) throw r.Error("a die has at least one side");
                return new Instruction { Op = Op.Random, Line = line, Number = sides };
            }

            // The Pokédex is told of a Pokémon the story shows (plan 02 · S12; the original's SetSpeciesSeen)
            case "seen":
                return new Instruction { Op = Op.Seen, Line = line, Name = r.Text("a species' name") };

            // Fossils, the Game Corner, the Villa and Pal Park (plan 06 · R16b)
            case "fossil":
                return new Instruction { Op = Op.Fossil, Line = line, Name = r.OneOf("count", "choose", "revive", "ready", "give") };
            case "gamecorner":
            {
                string what = r.OneOf("coins", "room", "add", "take", "slots", "prize", "buy", "hiddenpower", "cointrick");
                int number = what is "room" or "add" or "take" or "slots" ? r.Int(what == "slots" ? "a machine, 0 to 11" : "a number of coins") : 0;
                if (what == "slots" && number is < 0 or > 11) throw r.Error("the Game Corner's machines are 0 to 11");
                if (what != "slots" && number < 0) throw r.Error("coins are counted from nought up");
                return new Instruction { Op = Op.GameCorner, Line = line, Name = what, Number = number };
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
                string what = r.OneOf("points", "give", "prize", "buy", "records", "print", "judge", "judged", "save");
                string which = what switch
                {
                    "prize" => r.OneOf("items", "machines"),
                    "records" or "print" => r.OneOf("tower", "factory", "hall", "castle", "arcade"),
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
                string what = r.OneOf("befriend", "level", "combee", "condition", "massage", "first", "nature", "size", "holds", "deoxys");
                return what switch
                {
                    // Plan 06 · R29: Veilstone City's meteorites (ChangeDeoxysForm): "Deoxys" is its Normal Forme
                    "deoxys" => new Instruction { Op = Op.Team, Line = line, Name = what, Other = DeoxysForm(r) },
                    // Plan 01 · M11, part c: a nature by its name, and a size beside the record ("team size keep" keeps it)
                    "nature" => new Instruction { Op = Op.Team, Line = line, Name = what, Other = NatureWord(r) },
                    "size" when r.PeekWord("keep") => new Instruction { Op = Op.Team, Line = line, Name = what, Option = r.OneOf("keep") == "keep" },
                    "size" => new Instruction { Op = Op.Team, Line = line, Name = what, Other = r.Text("a species' name") },
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
            // The move tutors and the Move Deleter (plan 01 · M11, part d)
            case "tutor":
            {
                string what = r.OneOf("moves", "afford", "pay", "special", "dragons", "learn", "forget", "delete");
                string which = what switch
                {
                    "moves" => r.Text("the place of a shard tutor"),
                    "special" => r.OneOf("dracometeor", "ultimate"),
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
                return new Condition { Query = Query.Badge, Negated = negated, Name = r.Badge() };
            case "badges":
                return Counted(Query.Badges);
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
            case "knows":
                return new Condition { Query = Query.Knows, Negated = negated, Name = r.Text("a move's name") };
            case "has":
                return new Condition { Query = Query.Has, Negated = negated, Name = r.Text("a species' name") };
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
            case "partner":
                return new Condition { Query = Query.Partner, Negated = negated };
            case "time":
                return new Condition { Query = Query.Time, Negated = negated, Name = r.Enum<Core.TimeOfDay>("a time of day").ToString() };
            case "weekday":
                return new Condition { Query = Query.Weekday, Negated = negated, Name = r.Enum<System.DayOfWeek>("a day of the week").ToString() };
            // The National Pokédex is the player's (plan 02 · S14; the original's GetNationalDexEnabled)
            case "nationaldex":
                return new Condition { Query = Query.NationalDex, Negated = negated };
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

    /// <summary>A nature by its name, in any case (<c>team nature serious</c>).</summary>
    /// <summary>A forme of Deoxys's by its name ("Deoxys-Speed"), or "Deoxys" for its Normal Forme.</summary>
    private static string DeoxysForm(Reader r)
    {
        string form = r.Text("a forme of Deoxys's");
        if (form != "Deoxys" && PokemonDatabase.Get("Deoxys")?.Form(form) == null) throw r.Error($"'{form}' is no forme of Deoxys's");
        return form;
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
