using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.Audio;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The voices (plan 26 · V2): the options' three rows given to the text box's <see cref="VoiceTalk"/>, and the pack of the
/// language chosen found once. With VOICES off nothing is looked for, read or played; with no pack the text shows as it
/// always did.
/// </summary>
public partial class GameEngine
{
    private string? voicePackLanguage;
    private VoiceLibrary? voicePack;

    /// <summary>
    /// A pack a tool hands the game in place of the one beside it (a test's pack of synthesised vowels, the harness's):
    /// used whatever the language while set.
    /// </summary>
    public VoiceLibrary? VoicePackOverride { get; set; }

    /// <summary>The pack the game's voices come from now, or null.</summary>
    public VoiceLibrary? VoicePack => flow.Dialogue.Voice.Library;

    /// <summary>How far from the player a speaker whose mouth moves may stand, in tiles.</summary>
    public const int SpeakerReach = 12;

    private readonly List<NPC> nearSpeaker = new();

    /// <summary>
    /// Whose mouth moves with the text box's voice now (plan 26 · V6): nobody while no voice speaks or between syllables;
    /// otherwise the person spoken to, where the line is in their voice, else the nearest person on the map within
    /// <see cref="SpeakerReach"/> tiles whose voice the line is (the rival walking up in a scene). A line in the voice of
    /// nobody on the map (a call over the Pokégear) moves no mouth.
    /// </summary>
    public NPC? SpeakerNow()
    {
        var talk = flow.Dialogue.Voice;
        if (!talk.MouthOpen || talk.Speaking?.Voice is not { Length: > 0 } voice) return null;
        var cast = VoiceCast.Default;
        string place = flow.Map.Name;
        if (flow.Runner is { Subject: { } subject } runner && ScriptRunner.VoiceOf(subject, runner.Place, cast) == voice) return subject;
        NPC? best = null;
        int bestDistance = int.MaxValue;
        foreach (var npc in flow.Map.NPCs.Near(flow.Player.GridX, flow.Player.GridY, SpeakerReach, nearSpeaker))
        {
            int d = Math.Abs(npc.GridX - flow.Player.GridX) + Math.Abs(npc.GridY - flow.Player.GridY);
            if (d < bestDistance && ScriptRunner.VoiceOf(npc, place, cast) == voice)
            {
                best = npc;
                bestDistance = d;
            }
        }
        nearSpeaker.Clear();
        return best;
    }

    private void UseVoices()
    {
        UseVoices(flow.Dialogue.Voice);
        // The introduction's box and a battle's speak by the same rows (plan 26 · V6)
        UseVoices(flow.IntroScreen.Voice);
        if (flow.Battle != null) UseVoices(flow.Battle.Voice);
    }

    /// <summary>A box's voices set from the options: their mode, whether they are let finish, and the pack.</summary>
    internal void UseVoices(VoiceTalk talk)
    {
        talk.Mode = Settings.Voices;
        talk.LetFinish = Settings.LetVoicesFinish;
        if (Settings.Voices == VoiceMode.Off)
        {
            talk.Cut();
            talk.Library = null;
            return;
        }
        if (VoicePackOverride != null)
        {
            talk.Library = VoicePackOverride;
            return;
        }
        // The pack is looked for once a language (headless and tool runs look in the same places and find none)
        string language = Text.Language.Tag;
        if (voicePackLanguage != language)
        {
            voicePackLanguage = language;
            voicePack = VoiceLibrary.Find(language);
        }
        talk.Library = voicePack;
    }
}
