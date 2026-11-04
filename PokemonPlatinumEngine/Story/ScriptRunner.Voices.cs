using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// Who speaks each line (plan 26 · V2): the runner hands every line it shows to its host with the line's key, its English
/// and the cast's voice beside it, by the same rule the catalogue (<see cref="VoiceCatalogue"/>) gave them: the last
/// <c>speaker</c> written before a line is its voice, a script starts as the person spoken to (<c>self</c>), and
/// <c>speaker none</c>, <c>text</c>, signs and Pokémon are nobody's. A voice the catalogue could only call <c>@self</c>
/// is decided here, by who the person spoken to is.
/// </summary>
public sealed partial class ScriptRunner
{
    // The voice of the lines said from here on: the speaker's, or nobody's after 'speaker none'
    private string? voiceNow;
    private bool voiceNone;
    private bool waitingForVoice;

    /// <summary>The cast the runner reads voices from; the game's own unless a test hands it another.</summary>
    public VoiceCast? Cast { get; set; }

    private VoiceCast CastNow => Cast ??= VoiceCast.Default;

    /// <summary>The voice a script starts with: the person spoken to's, or nobody's where there is none.</summary>
    private void StartVoices()
    {
        voiceNow = VoiceOfPerson(Subject);
        voiceNone = false;
        waitingForVoice = false;
    }

    /// <summary>A script's <c>speaker</c>: an id or a display name the cast knows, the person spoken to, or nobody.</summary>
    private void SpeakerVoice(Instruction i)
    {
        voiceNone = false;
        if (i.Option) voiceNow = VoiceOfPerson(Subject);
        else if (i.Name.Length > 0) voiceNow = Settle(CastNow.ForSpeaker(i.VoiceId, i.Name, script!.FullName + "/" + i.Name)) ?? VoiceOfPerson(Subject);
        else { voiceNow = null; voiceNone = true; }
    }

    /// <summary>
    /// A person's voice, as <see cref="VoiceCatalogue"/> casts them: their own entry, a look or name that is a named
    /// character's, the rival's class, their class's or look's archetype by a hash of where they stand. Nobody for a
    /// Pokémon (its cry is its voice) or for no one.
    /// </summary>
    private string? VoiceOfPerson(NPC? who) => VoiceOf(who, place, CastNow);

    /// <summary>The place the script runs in, which a person whose lines name none of their own is cast by.</summary>
    public string Place => place;

    /// <summary>
    /// A person's voice as the runner gives it (plan 26 · V6 asks it of whoever stands on the map, to find whose mouth
    /// moves): <paramref name="place"/> is where they are when their lines name no place of their own.
    /// </summary>
    public static string? VoiceOf(NPC? who, string place, VoiceCast cast)
    {
        if (who == null || who.NpcType == NPC.PokemonType || who.IsThing) return null;
        string? trainerClass = who.TrainerData is { } t && t.TrainerClass is { Length: > 0 } c && c != "Trainer" ? c : null;
        return Settle(cast, cast.ForPerson(who.LinesPlace ?? place, who.LinesKey ?? who.Key ?? "", who.Name, who.TrainerData?.Name, trainerClass,
            who.NpcType, trainerClass != null && TrainerDatabase.IsRival(trainerClass)));
    }

    /// <summary>A voice that chooses at run time (the assistant: the boy or the girl the player isn't) settled now.</summary>
    private string? Settle(string? voice) => Settle(CastNow, voice);

    private static string? Settle(VoiceCast cast, string? voice)
    {
        if (voice == null || !cast.File.Voices.TryGetValue(voice, out var entry) || entry.Chooses is not { Count: >= 2 } two) return voice;
        return PlayerIdentity.Look == PlayerLook.Boy ? two[1] : two[0];
    }

    /// <summary>The kind of place a person's lines are kept under: a hand-made map's name is written in capitals, an area's key never is.</summary>
    private static string PersonKind(string place) => place.Any(char.IsUpper) ? "map" : "world";

    /// <summary>A line of the script spoken by the voice in force, or nobody's after <c>speaker none</c>.</summary>
    private SpokenLine? ScriptVoice(Instruction i, int k) =>
        voiceNone || i.TextIndex < 0 || script == null || k >= i.Lines.Count ? null
            : new SpokenLine(Text.ScriptLineKey(script.FullName, i.TextIndex, k) + Variant(i.Lines[k]), i.Lines[k], voiceNow);

    /// <summary>A line of a person's own (<c>sayown</c>, <c>trainerline</c>), under their key and in their voice.</summary>
    private SpokenLine? PersonVoice(NPC? who, PersonField field, int k, string english)
    {
        if (who?.LinesPlace is not { } at || (who.LinesKey ?? who.Key) is not { } key) return null;
        return VoiceOfPerson(who) is { } voice ? new SpokenLine(Text.PersonLineKey(PersonKind(at), at, key, field, k) + Variant(english), english, voice) : null;
    }

    /// <summary>The voices of what <c>sayown</c> says: a person's lines, never a sign's.</summary>
    private IReadOnlyList<SpokenLine?> OwnVoices()
    {
        var voices = new SpokenLine?[ownLines.Count];
        if (ownSign == null && ownFromSubject && Subject != null)
            for (int k = 0; k < voices.Length; k++) voices[k] = PersonVoice(Subject, PersonField.Dialog, k, ownLines[k]);
        return voices;
    }

    /// <summary>A variant's suffix for the few-valued placeholders a line names, by what they stand for now.</summary>
    private string Variant(string english) => VoiceTalk.VariantOf(english, name => name switch
    {
        "assistant" => PlayerIdentity.DefaultName(PlayerIdentity.Look == PlayerLook.Boy ? PlayerLook.Girl : PlayerLook.Boy),
        "starter" => Shown(host.Story.PlayerStarter),
        "rivalstarter" => Shown(host.Story.RivalStarter),
        _ => null
    });
}
