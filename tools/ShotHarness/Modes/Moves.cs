using PokemonPlatinumEngine.Audio;

partial class Harness
{
    // ---------------------------------------------------------------- moves of their own (plan 25 · Y6)

    // moves [move ...]: with no move named, boards of Platinum's 467 moves at the moment each lands, sixty to a board in
    // the order of their numbers (mv_00 to mv_07): each move started by the animator itself from the player's Pokémon
    // at the foe in a battle at its menu, the picture as it lands, its name under it with how many quads and ribbons its
    // look draws then (FxList.Count) and whose look it is: a batch's (Y6…), G8's own (G8) or its type's template (T, in
    // red). Given moves (`moves Tackle "Rock Smash"`), four moments of each instead: setting off, landing, after and
    // fading (mv_<key>_1 to _4). Not part of "all".
    public void MovesMode()
    {
        var named = args.Skip(2).ToArray();
        var mine = new Pokemon(PokemonDatabase.Get("Empoleon")!, 40, new Random(81));
        party.HealAll();
        party.Members.Insert(0, mine);
        var foe = new Pokemon(PokemonDatabase.Get("Bidoof")!, 40, new Random(82));
        var (name, x, y) = Place("Route201", -1, -1);
        game.Map = MapDatabase.Get(name);
        if (x >= 0) game.Player.SetPosition(x, y, Direction.Up);
        var b = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = inventory, Pokedex = pokedex, WildPokemon = new List<Pokemon> { foe }, Random = new BattleRandom(80),
            Conditions = new BattleConditions { Terrain = BattleTerrain.Grass }
        });
        game.BattleRenderer.SetArena(game.Map, x, y);
        game.Battle = b;
        game.State = GameState.Battle;
        ToMainMenu(b);

        EffectCue Start(MoveData move)
        {
            // The last cue has run out and the camera has come back by now: one move to a picture
            Skip(BattleAnimator.EffectTime + 0.4);
            bool self = move.Target is MoveTarget.User or MoveTarget.UserSide or MoveTarget.UserAndAllies;
            return b.Anim.Cue(new EffectCue
            {
                Kind = CueKind.Move, Move = move.Name, Type = move.Type, Category = move.Category,
                FromSide = BattleSide.Player, ToSide = self ? BattleSide.Player : BattleSide.Enemy, Terrain = BattleTerrain.Grass
            });
        }

        // How much a look draws at an age, with the battle's own places
        int Drawn(EffectCue cue, float age)
        {
            float was = cue.Age;
            cue.Age = age;
            var fx = new FxList();
            MoveFx.Play(cue, new FxPlaces(), fx);
            cue.Age = was;
            return fx.Count;
        }

        string Whose(string move) => MoveFx.BatchOf(move) ?? (MoveFx.HasOwnEffect(move) ? "G8" : "T");

        if (named.Length > 0)
        {
            // MOVES_BOARD=1: the field of each moment cropped side by side, a row to a move, four moves to a board (mvb_NN)
            bool boardMode = Environment.GetEnvironmentVariable("MOVES_BOARD") is { Length: > 0 };
            var rows = new List<(string Label, List<Image> Tiles)>();
            foreach (string moveName in named)
            {
                // A number names a move by its id (`moves 571 588`), for the names with an apostrophe a shell trips on
                var move = int.TryParse(moveName, out int id) ? MoveDatabase.GetAll().FirstOrDefault(m => m.Id == id) : MoveDatabase.Get(moveName);
                if (move == null)
                {
                    Console.WriteLine($"!! no move called {moveName}");
                    continue;
                }
                string key = SoundBank.MoveKey(move.Name);
                var cue = Start(move);
                Console.WriteLine($"  {move.Name} ({Whose(move.Name)}): sound {SoundBank.MoveSound(move.Name, move.Type)}");
                double at = 0;
                // MOVES_AGES=0.25,0.33 asks for other moments (a look is judged where it is strongest)
                var ages = new[] { (1, 0.15), (2, BattleAnimator.ImpactTime + 0.03), (3, 0.6), (4, 0.95) };
                if (Environment.GetEnvironmentVariable("MOVES_AGES") is { Length: > 0 } custom)
                    ages = custom.Split(',').Select((s, i) => (i + 1, double.Parse(s, System.Globalization.CultureInfo.InvariantCulture))).ToArray();
                foreach (var (moment, age) in ages)
                {
                    Skip(age - at);
                    at = age;
                    if (boardMode)
                    {
                        var snap = Capture();
                        Raylib.ImageResize(ref snap, GameEngine.VirtualWidth, GameEngine.VirtualHeight);
                        var crop = Raylib.ImageFromImage(snap, new Rectangle(230, 40, 1670, 760));
                        Raylib.UnloadImage(snap);
                        Raylib.ImageResize(ref crop, 700, 319);
                        if (!rows.Any(r => r.Label == move.Name)) rows.Add((move.Name, new List<Image>()));
                        rows.First(r => r.Label == move.Name).Tiles.Add(crop);
                    }
                    else Shot($"mv_{key}_{moment}");
                    Console.WriteLine($"    {moment} at {cue.Age:0.00} s: {Drawn(cue, cue.Age)} quads and ribbons");
                }
            }
            for (int first = 0; first < rows.Count; first += 4)
            {
                int n = Math.Min(4, rows.Count - first);
                var sheet = Raylib.GenImageColor(700 * 4, n * 341, new Color(24, 26, 34, 255));
                for (int r = 0; r < n; r++)
                {
                    var (label, tiles) = rows[first + r];
                    for (int t = 0; t < tiles.Count && t < 4; t++)
                        Raylib.ImageDraw(ref sheet, tiles[t], new Rectangle(0, 0, 700, 319), new Rectangle(t * 700, r * 341, 700, 319), Color.White);
                    Raylib.ImageDrawText(ref sheet, label, 6, r * 341 + 321, 18, new Color(150, 240, 150, 255));
                    foreach (var tile in tiles) Raylib.UnloadImage(tile);
                }
                Save(sheet, $"mvb_{first / 4:00}");
            }
        }
        else
        {
            // MOVES_LATER=1: the later games' moves that have a look (plan 25 · Y11 on), on boards of their own (mvl_NN)
            bool later = Environment.GetEnvironmentVariable("MOVES_LATER") is { Length: > 0 };
            string prefix = later ? "mvl" : "mv";
            var moves = MoveDatabase.GetAll().Where(m => later ? m.Id > 467 && MoveFx.BatchOf(m.Name) != null : m.Id <= 467).OrderBy(m => m.Id).ToList();
            const int perBoard = 60, columns = 10, tileW = 288, tileH = 162, label = 22;
            for (int first = 0; first < moves.Count; first += perBoard)
            {
                string board = $"{prefix}_{first / perBoard:00}";
                bool wanted = Wanted(board);
                var sheet = Raylib.GenImageColor(columns * tileW, (perBoard / columns) * (tileH + label), new Color(24, 26, 34, 255));
                for (int i = 0; i < perBoard && first + i < moves.Count; i++)
                {
                    var move = moves[first + i];
                    var cue = Start(move);
                    Skip(BattleAnimator.ImpactTime + 0.03);
                    int count = Drawn(cue, cue.Age);
                    string whose = Whose(move.Name);
                    Console.WriteLine($"  {move.Id,3} {move.Name}: {whose}, {count} at the landing");
                    if (!wanted) continue;
                    var img = Capture();
                    Raylib.ImageResize(ref img, tileW, tileH);
                    int tx = i % columns * tileW, ty = i / columns * (tileH + label);
                    Raylib.ImageDraw(ref sheet, img, new Rectangle(0, 0, tileW, tileH), new Rectangle(tx, ty, tileW, tileH), Color.White);
                    Raylib.UnloadImage(img);
                    var ink = whose == "T" ? new Color(255, 110, 110, 255) : whose == "G8" ? new Color(200, 200, 200, 255) : new Color(150, 240, 150, 255);
                    Raylib.ImageDrawText(ref sheet, $"{move.Id} {move.Name} {count} {whose}", tx + 4, ty + tileH + 3, 16, ink);
                }
                if (wanted) Save(sheet, board);
                else Raylib.UnloadImage(sheet);
            }
        }

        party.Members.Remove(mine);
        game.State = GameState.Overworld;
    }
}
