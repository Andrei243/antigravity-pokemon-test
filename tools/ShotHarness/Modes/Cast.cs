partial class Harness
{
    // ---------------------------------------------------------------- the character table's looks (plan 11 · C2 to C6)

    /// <summary>The named cast who battle, each stood on the platform as a Leader or a Commander is: as themselves.</summary>
    static readonly string[] CastInBattle = { "roark", "gardenia", "fantina", "maylene", "crasher_wake", "mars", "jupiter" };

    // cast [look ...]: every look of the character table as a turntable (front, three-quarter, side and back), twenty
    // to a board (56_cast_NN), or only the looks named; then a trainer battle against each of the named cast who
    // battle, or each look named, stood as that look (57_cast_battle_<look>). Not part of "all"
    public void CastMode()
    {
        var named = args.Skip(2).Select(n => n.ToLowerInvariant()).ToArray();
        var looks = named.Length > 0 ? named : CharacterStyles.Default.Names.OrderBy(n => n, StringComparer.Ordinal).ToArray();
        var context = game.RenderContext;
        var pose = new CharacterPose { Time = 0.4f, Expression = Expression.Neutral };
        const int ViewW = 170, ViewH = 250, Label = 34, Columns = 4, Rows = 5;
        float[] yaws = { 0f, 0.65f, MathF.PI / 2f, MathF.PI };
        int cellW = ViewW * yaws.Length, cellH = ViewH + Label;
        for (int page = 0; page * Columns * Rows < looks.Length; page++)
        {
            string name = $"56_cast_{page + 1:D2}";
            if (!Wanted(name)) continue;
            var board = Raylib.GenImageColor(cellW * Columns, cellH * Rows, new Color(206, 218, 232, 255));
            for (int i = 0; i < Columns * Rows && page * Columns * Rows + i < looks.Length; i++)
            {
                string look = looks[page * Columns * Rows + i];
                int x = i % Columns * cellW, y = i / Columns * cellH;
                var views = CharacterStudio.Turntable(context, look, yaws, ViewW, ViewH, pose, 1.7f, 0.7f);
                Raylib.ImageDraw(ref board, views, new Rectangle(0, 0, views.Width, views.Height), new Rectangle(x, y + Label, cellW, ViewH), Color.White);
                Raylib.UnloadImage(views);
                Raylib.ImageDrawRectangle(ref board, x, y, cellW, Label, new Color(52, 64, 96, 255));
                Raylib.ImageDrawText(ref board, look, x + 12, y + 7, 20, Color.White);
            }
            Save(board, name);
        }

        foreach (string look in named.Length > 0 ? named : CastInBattle)
        {
            string name = $"57_cast_battle_{look}";
            if (!Wanted(name)) continue;
            var trainer = new Trainer { Name = look, TrainerClass = "Leader", Look = look };
            trainer.Party.Add(new Pokemon(PokemonDatabase.Get("Geodude")!, 12));
            var b = StartBattle("", 0, trainer);
            Skip(130 / 60.0);
            Shot(name);
            game.State = GameState.Overworld;
        }
    }
}
