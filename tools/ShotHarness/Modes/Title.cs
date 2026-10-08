partial class Harness
{
    // ---------------------------------------------------------------- title screen
    public void TitleMode()
    {
        const float Dt = 1f / 60f;
        TitleScreen NewTitle(SaveData? save)
        {
            var title = new TitleScreen(save);
            game.Title = title;
            game.State = GameState.Title;
            return title;
        }
        void Seconds(float s) => Frames((int)MathF.Round(s / Dt));

        // The opening from the start, with no saved game: notice, three fly-over shots, Giratina, the title, the menu
        var opening = NewTitle(null);
        Seconds(1.6f); Shot("title_1_notice");
        Seconds(TitleScreen.NoticeTime - 1.6f + 1.9f); Shot("title_2_journey_twinleaf");
        Seconds(TitleScreen.SegmentTime); Shot("title_3_journey_route201");
        Seconds(TitleScreen.SegmentTime); Shot("title_4_journey_sandgem");
        Seconds(TitleScreen.SegmentTime - 1.9f + 1.0f); Shot("title_5_reveal_silhouette");
        Seconds(0.9f); Shot("title_6_reveal_lit");
        Seconds(2.2f); Shot("title_7_idle");
        Timing("title");
        opening.PressConfirm();
        Seconds(1f); Shot("title_8_menu_no_save");

        // With a saved game: three badges, twelve and a half hours, a party of five
        var save = new SaveData
        {
            PlayerName = "Lucas",
            CurrentMapName = "SandgemTown",
            PlayTimeSeconds = 12 * 3600 + 34 * 60 + 20,
            Badges = 0b0000_0111,
            Money = 12480,
            CaughtSpecies = Enumerable.Range(387, 14).ToList(),
            Party = new[] { ("Grotle", 24), ("Staravia", 22), ("Luxio", 23), ("Bibarel", 20), ("Riolu", 18) }
                .Select(p => SavedPokemonData.FromPokemon(new Pokemon(PokemonDatabase.Get(p.Item1)!, p.Item2))).ToList()
        };
        var saved = NewTitle(save);
        saved.PressConfirm();
        Seconds(1.2f);
        saved.PressConfirm();
        Seconds(1f); Shot("title_9_menu_continue");
        ShotCrop("title_9b_continue_native", 1016, 250, 824, 452, 2);
        saved.Move(1);
        Seconds(0.2f); Shot("title_10_menu_new_game");
        saved.PressConfirm();
        Seconds(0.2f); Shot("title_11_confirm_new_game");

        // "Yes" leads to the last question: whose rules. Platinum's are offered; the other answer has its own words
        saved.Move(1);
        saved.PressConfirm();
        Seconds(0.2f); Shot("title_12_rules_platinum");
        saved.Move(1);
        Seconds(0.2f); Shot("title_13_rules_modern");

        // A game played by the modern rules says so on its panel
        save.Rules = RulesPreset.Modern;
        var modern = NewTitle(save);
        modern.PressConfirm();
        Seconds(1.2f);
        modern.PressConfirm();
        Seconds(1f); Shot("title_14_continue_modern_rules");

        game.State = GameState.Overworld;
    }
}
