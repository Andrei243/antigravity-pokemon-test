using System;

namespace PokemonPlatinumEngine.Audio;

/// <summary>
/// How loud each bus's content is meant to be (plan 05 · A7's mixing pass), measured the same way for all of it:
/// as the mixer plays it at its own volume, centred, the mean power of 100 ms blocks of the two sides' average,
/// leaving out blocks quieter than −50 dBFS (the rests of a fanfare, the silence after a sound), in dB of full
/// scale. The music sits at about −17.5 dB; the cries a little under it; a sound effect, short and sharp, from
/// about −37 dB (a menu's tick) to −20 dB (a blow in battle); the ambience about ten dB under the music, so a bed
/// can play under a song without either being turned down. <c>MusicTests</c>, <c>SoundTests</c> and
/// <c>tools/MusicRender</c> hold every song, sound, cry and bed to its bus's window.
/// </summary>
public static class Loudness
{
    /// <summary>Blocks quieter than this are left out of the measure.</summary>
    public const double Gate = -50;

    /// <summary>The level each ambience bed is brought to as it is made: what a layer's gain of 1 means.</summary>
    public const double AmbienceBedLevel = -23;

    /// <summary>
    /// The loudest a cry is made, as it is: played centred at the mixer's own volume, about 5 dB lower, it measures
    /// half a decibel under the top of the cry bus's window. Most cries are brought to their peak well under it; a
    /// long, even one would pass it at its peak and is turned down instead.
    /// </summary>
    public const double CryCeiling = -12.5;

    /// <summary>The highest peak a voice file may have (plan 26 · V2's checks): −1 dBFS.</summary>
    public const double VoicePeak = -1;

    /// <summary>The window a bus's content must measure within, in dBFS (at the bus's full volume).</summary>
    public static (double Low, double High) Target(AudioBus bus) => bus switch
    {
        AudioBus.Music => (-20.5, -15),
        AudioBus.Fanfare => (-20.5, -14.5),
        AudioBus.Sound => (-38, -20),
        AudioBus.Cry => (-27, -17),
        // A spoken line (plan 26 · V2): level with the cries, a little over the dipped music, so speech is clear over a song
        AudioBus.Voice => (-24, -18),
        _ => (-30, -26)
    };

    /// <summary>
    /// The window a voice must measure within as the mixer plays it at VOICE VOLUME 100 (stereo, centred, after the
    /// voice bus's make-up gain), in dBFS: over the music's window dipped by <see cref="AudioMixer.VoiceDuckLevel"/>.
    /// </summary>
    public static (double Low, double High) VoicePlayed => (-21.5, -14.5);

    public static float Linear(double db) => (float)Math.Pow(10, db / 20);

    public static double Db(double linear) => 20 * Math.Log10(Math.Max(linear, 1e-9));

    /// <summary>
    /// The gated level of mono samples, or of interleaved stereo ones when <paramref name="channels"/> is 2, at the DS's
    /// rate: a sound effect, a cry or a bed as it is made, or a song from a DS player. What the mixer renders is at its
    /// own rate (<see cref="AudioMixer.Rate"/>) and is measured with the rate given.
    /// </summary>
    public static double Of(ReadOnlySpan<float> samples, int channels = 1) => Of(samples, channels, Synthesizer.DsRate);

    /// <summary>The gated level of samples at another rate (plan 31: the mix's and the modern sound's 48 kHz), in blocks of 100 ms of that rate.</summary>
    public static double Of(ReadOnlySpan<float> samples, int channels, int rate)
    {
        int frames = samples.Length / channels;
        int block = rate / 10;
        double gate = Math.Pow(10, Gate / 10);
        double sum = 0;
        int kept = 0;
        for (int start = 0; start < frames; start += block)
        {
            int n = Math.Min(block, frames - start);
            if (n < block / 2 && kept > 0) break;
            double power = 0;
            for (int i = 0; i < n; i++)
            {
                float x = 0f;
                for (int c = 0; c < channels; c++) x += samples[(start + i) * channels + c];
                x /= channels;
                power += x * x;
            }
            power /= n;
            if (power < gate) continue;
            sum += power;
            kept++;
        }
        return kept == 0 ? -120 : 10 * Math.Log10(sum / kept);
    }
}
