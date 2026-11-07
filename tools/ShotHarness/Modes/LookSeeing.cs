partial class Harness
{
    /// <summary>
    /// Plan 12 · Q8 (`look_10_*`), the end of `look` so that no shot before it moves: the three rows of seeing in the
    /// options, then under each colour remap a battle's move cards (Grass, Fire, Water and Electric, the lead in the red)
    /// and the team (one in the yellow and one in the red), then large text in the field's box and the battle's, and high
    /// contrast in both. Prints what each remap makes of Grass and Fire and of the HP colours.
    /// </summary>
    private void LookSeeing()
    {
        var options = game.Options;
        var settings = engine.Settings;
        void GoToRow(OptionRow row) => options.Select(row);
        void Apply() => game.ApplySettings();

        // The rows
        game.State = GameState.Options;
        options.Open();
        GoToRow(OptionRow.TextSize);
        Frames(1); Shot("look_10_options_text_size");
        GoToRow(OptionRow.ColourVision);
        OptionsScreen.Change(settings, OptionRow.ColourVision, 1);
        Apply();
        Frames(1); Shot("look_10b_options_colour_vision_protan");
        OptionsScreen.Change(settings, OptionRow.ColourVision, -1);
        GoToRow(OptionRow.HighContrast);
        Apply();
        Frames(1); Shot("look_10c_options_high_contrast");
        game.State = GameState.Overworld;

        // The lead knows a move of each of four types, and stands in the red
        var lead = party.Members[0];
        var movesBefore = lead.Moves.ToList();
        int hpBefore = lead.CurrentHP;
        lead.Moves.Clear();
        foreach (var name in new[] { "Razor Leaf", "Ember", "Water Gun", "Thunder Shock" }) lead.Moves.Add(new Move(MoveDatabase.Get(name)));
        lead.CurrentHP = Math.Max(1, lead.MaxHP / 6);

        foreach (var vision in new[] { ColourVision.Normal, ColourVision.Protan, ColourVision.Deutan, ColourVision.Tritan })
        {
            settings.ColourVision = vision;
            Apply();
            string v = vision.ToString().ToLowerInvariant();
            foreach (var type in new[] { PokemonType.Grass, PokemonType.Fire })
            {
                var c = Seeing.Seen(Palette.GetTypeColor(type.ToString()));
                var seen = Seeing.Simulate(c, vision);
                Console.WriteLine($"{vision}: {type} drawn {c.R},{c.G},{c.B}, seen {seen.R},{seen.G},{seen.B}");
            }
            GoTo("TwinleafTown", 11, 8, Direction.Down);
            var battle = StartBattle("Shinx", 5);
            ToMainMenu(battle);
            battle.HUD.MenuState = BattleMenuState.Moves;
            Frames(1); Shot($"look_10d_{v}_moves");
            ShotCrop($"look_10e_{v}_moves_close", 40, 840, 1120, 220, 2);
            battle.HUD.MenuState = BattleMenuState.Main;
            game.State = GameState.PartyMenu;
            game.PartyScreen.Open();
            Frames(40); Shot($"look_10f_{v}_party");
            game.PartyScreen.Close();
            game.State = GameState.Overworld;
        }
        settings.ColourVision = ColourVision.Normal;

        // Large text and high contrast, in the field's box and the battle's
        const string fieldLine = "Barry: Hey, Lucas! You're finally ready! Professor Rowan is waiting at Lake Verity, and he said not to keep him waiting, so hurry up! If you dawdle, I'm going to fine you a million! Come on, come on, let's go!";
        // The battle's line as long as Normal's two rows hold, so that Large takes a third
        string battleLine = "";
        foreach (var word in "The opposing Shinx's Intimidate cuts the attack of every Pokémon on your side, and Turtwig's Shell Armor keeps it safe from every single critical hit for the rest of this long, long battle in the tall grass of Twinleaf Town!".Split(' '))
        {
            string longer = battleLine.Length == 0 ? word : battleLine + " " + word;
            if (PokemonPlatinumEngine.UI.Kit.TextMetrics.Wrap(longer, ModernUi.MessageBoxWidth - ModernUi.MessageMargin, ModernUi.MessageSize, PokemonPlatinumEngine.UI.Kit.UiWeight.ExtraBold).Count > 2) break;
            battleLine = longer;
        }
        foreach (var (name, large, contrast) in new[] { ("large", true, false), ("contrast", false, true), ("large_contrast", true, true) })
        {
            settings.TextSize = large ? TextSize.Large : TextSize.Normal;
            settings.HighContrast = contrast;
            Apply();
            GoTo("TwinleafTown", 12, 7, Direction.Up);
            game.Dialogue.ShowDialogue("Barry", new List<string> { fieldLine });
            game.State = GameState.Dialogue;
            Frames(480); Shot($"look_10g_{name}_dialogue");
            game.Dialogue = new DialogueManager();
            game.State = GameState.Overworld;

            var battle = StartBattle("Shinx", 5);
            ToMainMenu(battle);
            battle.CurrentMessage = battleLine;
            battle.HUD.MenuState = BattleMenuState.Message;
            Frames(1); Shot($"look_10h_{name}_battle_line");
            Console.WriteLine($"{name}: the battle line in {ModernUi.MessageRows(battleLine, ModernUi.MessageBoxWidth).Count} rows at {ModernUi.MessageSizeNow}, the field's in {PokemonPlatinumEngine.UI.Kit.DialogueBox.RowsOf(fieldLine, 1920, large).Count} of {PokemonPlatinumEngine.UI.Kit.DialogueBox.RowsNow}");
            battle.HUD.MenuState = BattleMenuState.Main;
            Frames(1); Shot($"look_10i_{name}_battle_menu");
            game.State = GameState.Overworld;
        }

        // Everything put back
        settings.TextSize = TextSize.Normal;
        settings.HighContrast = false;
        Apply();
        lead.Moves.Clear();
        lead.Moves.AddRange(movesBefore);
        lead.CurrentHP = hpBefore;
        game.State = GameState.Overworld;
    }
}
