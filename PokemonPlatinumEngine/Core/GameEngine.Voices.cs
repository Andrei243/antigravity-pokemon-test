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

    private void UseVoices()
    {
        var talk = flow.Dialogue.Voice;
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
