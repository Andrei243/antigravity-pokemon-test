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
    public static readonly string[] BuiltInVariables = { "RESULT", "PLAYER_X", "PLAYER_Y", "MONEY", "PARTY_COUNT", "BADGE_COUNT" };

    private static readonly Regex Identifier = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);
    private static readonly Regex FlagName = new("^(FLAG_[A-Z0-9_]+|[A-Z][A-Za-z]+HallOfFame)$", RegexOptions.Compiled);
    private static readonly Regex VariableName = new("^VAR_[A-Z0-9_]+$", RegexOptions.Compiled);

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
                return new Instruction { Op = Op.TrainerLine, Line = line, Option = r.OneOf("before", "after") == "after" };
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
                return new Instruction { Op = Op.SetVar, Line = line, Name = r.Variable(writable: true), Number = r.Int("a value") };
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
                return new Instruction { Op = Op.GiveBadge, Line = line, Badge = r.Enum<Badge>("a badge") };
            case "givemoney":
            case "takemoney":
            {
                int amount = r.Int("an amount");
                if (amount < 0) throw r.Error("an amount of money is not negative");
                return new Instruction { Op = word == "givemoney" ? Op.GiveMoney : Op.TakeMoney, Line = line, Number = amount };
            }
            case "heal":
                return new Instruction { Op = Op.Heal, Line = line };

            case "battle":
            {
                string who = r.Who();
                if (who == "player") throw r.Error("the player can't be battled");
                // What may follow, in any order: a second trainer, someone at the player's side, a battle that may be lost
                bool mayLose = false, byId = false, first = false;
                string second = "", partner = "";
                while (r.More)
                {
                    switch (r.OneOf("canlose", "first", "and", "with"))
                    {
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
                return new Instruction { Op = Op.Battle, Line = line, Name = who, Other = second, Partner = partner, PartnerById = byId, Option = mayLose, FirstBattle = first };
            }
            case "wildbattle":
            case "catchinglesson":
            {
                string species = r.Text("a species' name");
                int level = r.Int("its level");
                if (level is < 1 or > 100) throw r.Error("a Pokémon's level is from 1 to 100");
                if (word == "catchinglesson") return new Instruction { Op = Op.CatchingLesson, Line = line, Name = species, Number = level };
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
                switch (r.OneOf("pan", "release", "shake"))
                {
                    case "pan":
                    {
                        int x = r.Int("a tile's x"), y = r.Int("a tile's y");
                        return new Instruction { Op = Op.Camera, Line = line, Camera = CameraMove.Pan, X = x, Y = y, Seconds = r.More ? r.Seconds() : PanSeconds };
                    }
                    case "release":
                        return new Instruction { Op = Op.Camera, Line = line, Camera = CameraMove.Release, Seconds = r.More ? r.Seconds() : ReleaseSeconds };
                    default:
                        return new Instruction { Op = Op.Camera, Line = line, Camera = CameraMove.Shake, Seconds = r.More ? r.Seconds() : ShakeSeconds };
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
                return new Instruction { Op = Op.Cry, Line = line, Name = r.Text("a species' name") };

            case "starter":
                return new Instruction { Op = Op.Starter, Line = line };
            case "shop":
                return new Instruction { Op = Op.Shop, Line = line };
            case "pc":
                return new Instruction { Op = Op.Pc, Line = line };
            case "travel":
                return new Instruction { Op = Op.Travel, Line = line };

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
                return new Condition { Query = Query.Badge, Negated = negated, Name = r.Enum<Badge>("a badge").ToString() };
            case "badges":
                return Counted(Query.Badges);
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
            case "pokerus":
                return new Condition { Query = Query.Pokerus, Negated = negated };
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
        public bool PeekDirection => More && !tokens[at].Quoted && DirectionOf(tokens[at].Text) != null;
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

        public T Enum<T>(string expected) where T : struct, System.Enum
        {
            string word = Word(expected);
            if (int.TryParse(word, out _) || !System.Enum.TryParse(word, ignoreCase: true, out T value))
                throw Error($"'{word}' is not {expected}: {string.Join(", ", System.Enum.GetNames<T>().Select(n => n.ToLowerInvariant()))}");
            return value;
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

        /// <summary>The steps of a walk: directions, each with how many tiles when more than one, and <c>fast</c> for a run.</summary>
        public (List<Direction> Steps, bool Fast) Path()
        {
            var steps = new List<Direction>();
            bool fast = false;
            while (More)
            {
                string word = Word("a direction");
                if (word == "fast")
                {
                    fast = true;
                    continue;
                }
                var direction = DirectionOf(word) ?? throw Error($"'{word}' is not a step: up, down, left, right, each with a count, or fast");
                int count = PeekNumber ? Int("how many tiles") : 1;
                if (count is < 1 or > 64) throw Error("a walk goes 1 to 64 tiles one way");
                for (int i = 0; i < count; i++) steps.Add(direction);
            }
            if (steps.Count == 0) throw Error("a walk needs steps: up, down, left, right");
            return (steps, fast);
        }
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
