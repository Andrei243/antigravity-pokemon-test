using PokemonPlatinumEngine.Audio;

partial class Harness
{
    // ---------------------------------------------------------------- the face and the stage (plan 26 · V6)

    // A speaker's mouth moving with their voice, with a pack of stand-in takes (synthesised vowels, nobody's voice)
    // written into the out dir for the lines it shows: the professor's welcome with his mouth open on a syllable and shut
    // between two (vo01, vo02), and the nurse of a Pokémon Center the same (vo10, vo11). Not part of `all`. It prints how
    // often each mouth opened and whose it was.
    public void VoicesMode()
    {
        string folder = Path.Combine(outDir, "voicepack");
        var catalogue = VoiceCatalogue.Parse(File.ReadAllText(GameDataFiles.PathOf(Path.Combine("voice", "lines.json"))));
        int takes = VoicePacks.WriteStandIns(folder, catalogue, "en",
            (key, _) => key.StartsWith("intro:Sinnoh.", StringComparison.Ordinal) || key.StartsWith("script:common.Nurse", StringComparison.Ordinal));
        engine.VoicePackOverride = VoiceLibrary.Open(folder);
        engine.Settings.Voices = VoiceMode.All;
        engine.ApplySettings(window: false);
        Console.WriteLine($"  voices: {takes} stand-in takes in {folder}");

        // Frames until something holds, or says it never did
        bool Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
            return holds();
        }

        // ---- the introduction: the professor's welcome
        var intro = game.Intro;
        game.State = GameState.Intro;
        intro.Open();
        Until(() => intro.Phase == IntroPhase.Greeting, "the greeting");
        Frames(40);
        Until(() => intro.Voice.MouthOpen, "the professor's mouth open");
        Shot("vo01_rowan_mouth_open");
        Until(() => intro.Voice.IsSpeaking && !intro.Voice.MouthOpen, "the professor's mouth shut between syllables");
        Shot("vo02_rowan_mouth_shut");
        Until(() => !intro.Voice.IsSpeaking, "the line's end", 1800);
        Console.WriteLine($"  rowan: the mouth opened {intro.Voice.Mouth.Opened} times on {intro.Voice.Speaking?.Key ?? "the first line"}; speaking now {intro.Voice.IsSpeaking}, open {intro.Voice.MouthOpen}");
        intro.Close();

        // ---- the field: the nurse's welcome
        engine.StartNewGame();
        PastTheOpening();
        engine.Settings.NextObjective = RulesDefault.Off;
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        At("PokemonCenter", 8, 6, Direction.Up);
        game.Interact();
        Frames(3);
        var box = game.Dialogue;
        Until(() => box.Voice.MouthOpen && game.World.Talking != null, "the nurse's mouth open");
        Shot("vo10_nurse_mouth_open");
        Console.WriteLine($"  nurse: '{box.Voice.Speaking?.Key}' in {box.Voice.Speaking?.Voice}; the mouth is {game.World.Talking?.Name ?? "nobody"}'s");
        Until(() => box.Voice.IsSpeaking && !box.Voice.MouthOpen, "the nurse's mouth shut between syllables");
        Shot("vo11_nurse_mouth_shut");
        Console.WriteLine($"  nurse: the mouth is {(game.World.Talking == null ? "nobody's (shut)" : game.World.Talking.Name + "'s")}");

        engine.VoicePackOverride = null;
        engine.Settings.Voices = VoiceMode.Off;
        engine.ApplySettings(window: false);
    }
}
