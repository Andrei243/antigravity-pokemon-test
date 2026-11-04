using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using Xunit;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 26 · V6: the face and the stage. A speaker's mouth moves with the syllables of their take and only while it
/// plays; the introduction's professor speaks his lines; a trainer's lines in battle are the catalogue's, spoken in
/// their voice with their mouth moving. The takes are stand-ins of synthesised vowels made in the test, nobody's voice.
/// </summary>
public sealed class VoiceFaceTests : IDisposable
{
    private const float Frame = 1f / 60f;
    private readonly string folder = Path.Combine(Path.GetTempPath(), "voice-face-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        AudioManager.StopListening();
        if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
    }

    private static VoiceFile Catalogue => VoiceCatalogue.Parse(File.ReadAllText(Path.Combine(Repository.Root, "PokemonPlatinumEngine", "Data", "voice", "lines.json")));

    /// <summary>A pack of stand-in takes of the catalogue's lines whose keys begin with the prefix.</summary>
    private VoiceLibrary PackOf(string prefix)
    {
        VoicePacks.WriteStandIns(folder, Catalogue, "en", (key, _) => key.StartsWith(prefix, StringComparison.Ordinal));
        return VoiceLibrary.Open(folder)!;
    }

    // ------------------------------------------------------------------ the mouth

    [Fact]
    public void AMouthOpensOnALoudLevelShutsOnAQuietOneAndHoldsEachAWhile()
    {
        var mouth = new MouthFlap();
        mouth.Hear(0.005f);
        Assert.False(mouth.Open);
        mouth.Hear(0.1f);
        Assert.True(mouth.Open);
        // Quiet at once: it stays open until it has been open long enough
        mouth.Advance(0.02f);
        mouth.Hear(0f);
        Assert.True(mouth.Open);
        mouth.Advance(MouthFlap.Hold);
        mouth.Hear(0f);
        Assert.False(mouth.Open);
        // Between the two levels it stays as it is
        mouth.Advance(MouthFlap.Hold);
        mouth.Hear((MouthFlap.OpenAt + MouthFlap.ShutAt) / 2);
        Assert.False(mouth.Open);
        Assert.Equal(1, mouth.Opened);
    }

    [Fact]
    public void AMouthMovesWithItsTakesSyllablesAndOnlyWhileItSpeaks()
    {
        const string english = "Hey! Over here! The lake is this way, come and look at it with me!";
        var pack = new VoicePackFile();
        string file = VoicePacks.FileOf("rival", "script:t.A#0.0");
        Directory.CreateDirectory(Path.Combine(folder, "rival"));
        float[] take = VoicePacks.Vowels(english, "rival");
        File.WriteAllBytes(Path.Combine(folder, file), WavDecoder.Encode(take));
        pack.Lines.Add(new VoicePackEntry { Key = "script:t.A#0.0", Voice = "rival", File = file, TextHash = VoiceCatalogue.HashOf(english) });
        File.WriteAllText(Path.Combine(folder, VoiceLibrary.PackFileName), GameDataFiles.Serialize(pack));

        var talk = new VoiceTalk { Mode = VoiceMode.All, Library = VoiceLibrary.Open(folder) };
        Assert.False(talk.MouthOpen);
        talk.Start(new SpokenLine("script:t.A#0.0", english, "rival"));
        int frames = 0, open = 0, changes = 0;
        bool was = false;
        while (talk.IsSpeaking && frames < 2000)
        {
            talk.Advance(Frame);
            frames++;
            if (talk.MouthOpen) open++;
            if (talk.MouthOpen != was) changes++;
            was = talk.MouthOpen;
        }
        int syllables = Math.Clamp(english.Count(char.IsLetter) / 3, 1, 60);
        Assert.InRange(frames, (int)(take.Length / (float)Synthesizer.DsRate / Frame) - 2, (int)(take.Length / (float)Synthesizer.DsRate / Frame) + 2);
        Assert.True(open > frames / 4 && open < frames * 9 / 10, $"open {open} of {frames} frames");
        // It opens and shuts with the syllables, but never faster than its hold lets it
        Assert.InRange(talk.Mouth.Opened, 0, syllables + 1);
        Assert.True(changes >= syllables / 2, $"{changes} changes over {syllables} syllables");
        Assert.True(changes <= frames * Frame / MouthFlap.Hold + 2);
        // Done: shut
        Assert.False(talk.MouthOpen);

        // Cut halfway: shut at once; and with the voices off it never opens
        talk.Start(new SpokenLine("script:t.A#0.0", english, "rival"));
        for (int i = 0; i < 30; i++) talk.Advance(Frame);
        talk.Cut();
        Assert.False(talk.MouthOpen);
        talk.Mode = VoiceMode.Off;
        talk.Start(new SpokenLine("script:t.A#0.0", english, "rival"));
        for (int i = 0; i < 60; i++) { talk.Advance(Frame); Assert.False(talk.MouthOpen); }
    }

    [Fact]
    public void AnOpenMouthIsPaintedInEveryFaceAndUnderAMoustache()
    {
        var rig = CharacterModels.Build("PLAYER");
        var rowan = CharacterModels.Build("ROWAN");
        foreach (var expression in Enum.GetValues<Expression>())
            foreach (var who in new[] { rig, rowan })
            {
                var shut = CharacterFaces.Paint(who.Style, who.Face, expression, blink: false);
                var open = CharacterFaces.Paint(who.Style, who.Face, expression, blink: false, mouthOpen: true);
                int n = CharacterFaces.TextureSize, differ = 0;
                for (int i = 0; i < n * n; i++)
                    if (!shut.Get(i % n, i / n).Equals(open.Get(i % n, i / n))) differ++;
                Assert.True(differ > 20, $"{(who == rowan ? "Rowan" : "the player")} {expression}: the open mouth changes {differ} texels");
            }
        Assert.True(rowan.Style.Mustache, "the professor was meant to be the face with a moustache");

        // The field's pixel face: the mouth's dark two texels under the eyes (one under a moustache)
        var plain = new PixelCanvas(40, 58);
        var open2 = new PixelCanvas(40, 58);
        CharacterFaces.Stamp(plain, new Vector2(20.4f, 20.6f), 0, rig.Style, Expression.Neutral, blink: false);
        CharacterFaces.Stamp(open2, new Vector2(20.4f, 20.6f), 0, rig.Style, Expression.Neutral, blink: false, mouthOpen: true);
        Assert.NotEqual(Enumerable.Range(0, 40 * 58).Count(i => plain.IsOpaque(i % 40, i / 40)), Enumerable.Range(0, 40 * 58).Count(i => open2.IsOpaque(i % 40, i / 40)));
        var bearded = new PixelCanvas(40, 58);
        CharacterFaces.Stamp(bearded, new Vector2(20.4f, 20.6f), 0, rowan.Style, Expression.Neutral, blink: false, mouthOpen: true);
        Assert.True(bearded.IsOpaque(20, 26));
    }

    [Fact]
    public void ASpritesKeyCarriesTheOpenMouthSoItIsBakedApart()
    {
        var rig = CharacterModels.Build("PLAYER");
        var shut = CharacterSprites.KeyOf(rig, new CharacterPose { Time = 1f }, 0f);
        var open = CharacterSprites.KeyOf(rig, new CharacterPose { Time = 1f, MouthOpen = true }, 0f);
        Assert.NotEqual(shut, open);
        Assert.True(open.Open);
        Assert.True(CharacterSprites.BakePose(open).MouthOpen);
        Assert.EndsWith("_open.png", CharacterSprites.OverrideName(open));
        Assert.DoesNotContain("_open", CharacterSprites.OverrideName(shut));
    }

    // ------------------------------------------------------------------ a language with no voices of its own

    [Fact]
    public void ALanguageWithNoPackSpeaksEnglishsOnlyWhenTheOptionsSaySo()
    {
        var english = new VoiceLibrary(folder, new VoicePackFile { Language = "en" });
        VoiceLibrary? Find(string language) => language == "en" ? english : null;
        Assert.Null(VoiceLibrary.Choose("ro", false, Find));
        Assert.Same(english, VoiceLibrary.Choose("ro", true, Find));
        Assert.Same(english, VoiceLibrary.Choose("en", false, Find));
        var s = new GameSettings();
        Assert.False(s.EnglishVoices);
        OptionsScreen.Change(s, OptionRow.EnglishVoices, 1);
        Assert.True(s.EnglishVoices);
        var (label, _, help) = OptionsScreen.Describe(s, OptionRow.EnglishVoices);
        Assert.Equal("English voices", label);
        Assert.Contains("English under the translated text", help);
        // The row stands with the voices' own, before the help
        var rows = Enum.GetValues<OptionRow>();
        Assert.Equal(Array.IndexOf(rows, OptionRow.LetVoicesFinish) + 1, Array.IndexOf(rows, OptionRow.EnglishVoices));
        Assert.True(Array.IndexOf(rows, OptionRow.EnglishVoices) < Array.IndexOf(rows, OptionRow.Help));
    }

    // ------------------------------------------------------------------ the introduction

    [Fact]
    public void TheProfessorSpeaksHisWelcomeByTheCataloguesKeysAndHisMouthMoves()
    {
        var heard = AudioManager.Listen();
        var intro = new IntroScreen();
        intro.Voice.Mode = VoiceMode.All;
        intro.Voice.Library = PackOf("intro:Sinnoh.");
        intro.Open();
        bool moved = false;
        for (int i = 0; i < 600; i++)
        {
            intro.Advance(Frame);
            moved |= intro.Voice.MouthOpen;
        }
        var mine = heard.Where(h => h.StartsWith("voice intro:", StringComparison.Ordinal)).ToList();
        Assert.Contains("voice intro:Sinnoh.Hello.0 rowan", mine);
        Assert.True(moved, "the professor's mouth never moved as he spoke");
        // Every key he says is the catalogue's
        var keys = Catalogue.Lines.Select(l => l.Key).ToHashSet();
        foreach (string h in mine) Assert.Contains(h.Split(' ')[1], keys);

        // Pressed on: the next line's voice, and Welcome after Hello
        intro.PressConfirm();
        intro.PressConfirm();
        Assert.Contains("voice intro:Sinnoh.Welcome.0 rowan", heard);
    }

    [Fact]
    public void EveryLineOfTheIntroductionIsOneTheCatalogueVoices()
    {
        var intro = new IntroScreen();
        intro.Open();
        var keys = Catalogue.Lines.ToDictionary(l => l.Key);
        var names = typeof(RegionIntroLines).GetProperties().Select(p => p.Name).ToArray();
        foreach (var line in intro.Voices(names).Where(l => l != null))
        {
            Assert.True(keys.TryGetValue(line!.Key, out var entry), $"{line.Key} is not in the catalogue");
            Assert.Equal(entry!.En, line.English);
            Assert.Equal(entry.Voice, line.Voice);
        }
    }

    // ------------------------------------------------------------------ battle

    [Fact]
    public void EveryTrainersBattleLineIsInTheCatalogueAndTheRivalsAreTheRivals()
    {
        var lines = Catalogue.Lines.Where(l => l.Key.StartsWith("battle:", StringComparison.Ordinal)).ToDictionary(l => l.Key);
        foreach (var (key, set) in TrainerLines.All)
            foreach (var (moment, line) in new[] { ("lastPokemon", set.LastPokemon), ("firstDown", set.FirstDown), ("lowHp", set.LowHp), (VoiceCatalogue.GiantMoment, set.Giant) })
            {
                if (line is not { Text.Length: > 0 }) continue;
                Assert.True(lines.TryGetValue(VoiceCatalogue.BattleKey(key, moment), out var entry), $"{key}'s {moment} is not in the catalogue");
                Assert.Equal(line.Text, entry!.En);
                if (key.StartsWith("rival_", StringComparison.Ordinal)) Assert.Equal(VoiceCast.Rival, entry.Voice);
            }
        Assert.Equal("roark", lines["battle:leader_roark.lowHp.0"].Voice);
        // A trainer of a stem speaks the stem's line under the stem's key
        var spoken = VoiceCatalogue.BattleLine(VoiceCast.Default, "rival_route_203_turtwig", "lowHp", "x", "Barry", "Rival");
        Assert.Equal("battle:rival_route_203_*.lowHp.0", spoken!.Key);
        Assert.Equal(VoiceCast.Rival, spoken.Voice);
    }

    /// <summary>
    /// A pack with a take of Cynthia's words as her Garchomp grows (the first line she says against a Blissey that only
    /// Splashes). The catalogue leaves that line shown only, for the {player} in it; a pack is played by its keys and
    /// hashes alone, so a take made by hand is spoken all the same.
    /// </summary>
    private VoiceLibrary CynthiasGrowth()
    {
        string english = TrainerLines.GiantFor("champion_cynthia_megamax")!.Text, key = VoiceCatalogue.BattleKey("champion_cynthia_megamax", VoiceCatalogue.GiantMoment);
        string file = VoicePacks.FileOf("cynthia", key);
        Directory.CreateDirectory(Path.Combine(folder, "cynthia"));
        File.WriteAllBytes(Path.Combine(folder, file), WavDecoder.Encode(VoicePacks.Vowels(english, "cynthia")));
        var pack = new VoicePackFile { Lines = { new VoicePackEntry { Key = key, Voice = "cynthia", File = file, TextHash = VoiceCatalogue.HashOf(english) } } };
        File.WriteAllText(Path.Combine(folder, VoiceLibrary.PackFileName), GameDataFiles.Serialize(pack));
        return VoiceLibrary.Open(folder)!;
    }

    /// <summary>Cynthia's Mega Max team with her Garchomp first, against a Blissey that only Splashes (as GiantChampionTests has her).</summary>
    private static BattleEngine AgainstCynthia(VoiceLibrary pack)
    {
        var record = TrainerDatabase.Get("champion_cynthia_megamax")!;
        var cynthia = new Trainer { Id = record.Id };
        TrainerDatabase.Fill(cynthia, record);
        int ace = cynthia.Party.Members.FindIndex(p => p.Species.Name == "Garchomp");
        var g = cynthia.Party.Members[ace];
        cynthia.Party.Members.RemoveAt(ace);
        cynthia.Party.Members.Insert(0, g);
        var party = new Party();
        party.Add(Mon("Blissey", 100, "Splash"));
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(), Trainers = new List<Trainer> { cynthia },
            Random = Calm(), Rules = Ruleset.Modern, TrainerLines = true, Scene = true
        });
        battle.Voice.Mode = VoiceMode.All;
        battle.Voice.Library = pack;
        Settle(battle);
        return battle;
    }

    [Fact]
    public void ATrainersLineInBattleIsSpokenInTheirVoiceWithTheirMouthMovingAndAPressCutsIt()
    {
        var heard = AudioManager.Listen();
        var battle = AgainstCynthia(CynthiasGrowth());
        bool spoke = false, moved = false, cut = false;
        int said = 0;
        for (int i = 0; i < 4000 && !battle.IsBattleOver && !cut; i++)
        {
            if (battle.CurrentSpeaker == "Cynthia") said++;
            if (battle.Voice.IsSpeaking)
            {
                spoke = true;
                Assert.Equal("cynthia", battle.Voice.Speaking!.Voice);
                moved |= battle.Anim.Speaking?.MouthOpen == true;
                if (moved && battle.IsWaitingForConfirm)
                {
                    battle.ConfirmMessage();
                    Assert.False(battle.Voice.IsSpeaking);
                    cut = true;
                    break;
                }
            }
            else if (battle.HUD.MenuState == BattleMenuState.Main)
            {
                foreach (var p in battle.PlayerSlots.Where(s => s.Pokemon != null)) p.Pokemon!.CurrentHP = p.Pokemon.MaxHP;
                battle.SelectMainMenuOption(0);
                battle.SelectMove(0);
            }
            else if (battle.IsWaitingForConfirm && !battle.Voice.IsSpeaking) battle.ConfirmMessage();
            battle.Update(Frame);
        }
        Assert.True(spoke, $"Cynthia never spoke: {said}; heard {string.Join(", ", heard.Where(h => h.StartsWith("voice")))}");
        Assert.True(moved, "her mouth never moved as she spoke");
        Assert.True(cut, "a press never cut her line");
        Assert.Contains(heard, h => h.StartsWith("voice battle:champion_cynthia_megamax.", StringComparison.Ordinal) && h.EndsWith(" cynthia", StringComparison.Ordinal));
    }

    [Fact]
    public void WithTheVoicesOffABattleHearsNothingAndNoMouthMoves()
    {
        var heard = AudioManager.Listen();
        var battle = AgainstCynthia(CynthiasGrowth());
        battle.Voice.Mode = VoiceMode.Off;
        for (int i = 0; i < 2000 && !battle.IsBattleOver; i++)
        {
            Assert.False(battle.Voice.IsSpeaking);
            Assert.NotEqual(true, battle.Anim.Speaking?.MouthOpen);
            if (battle.HUD.MenuState == BattleMenuState.Main)
            {
                foreach (var p in battle.PlayerSlots.Where(s => s.Pokemon != null)) p.Pokemon!.CurrentHP = p.Pokemon.MaxHP;
                battle.SelectMainMenuOption(0);
                battle.SelectMove(0);
            }
            else if (battle.IsWaitingForConfirm) battle.ConfirmMessage();
            battle.Update(Frame);
        }
        Assert.DoesNotContain(heard, h => h.StartsWith("voice battle:", StringComparison.Ordinal));
    }
}
