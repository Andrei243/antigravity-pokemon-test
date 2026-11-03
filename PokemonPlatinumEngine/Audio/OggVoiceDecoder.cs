using System;
using System.Collections.Generic;
using System.IO;
using NVorbis;

namespace PokemonPlatinumEngine.Audio;

/// <summary>
/// Ogg Vorbis for voice packs (plan 26 · V2), through NVorbis (MIT, a pure C# decoder: no audio device, no GPU). Any rate
/// and one or two channels become mono samples at <see cref="Synthesizer.DsRate"/>, as <see cref="WavDecoder"/> does.
/// A file that is not readable throws <see cref="InvalidDataException"/>, which the library counts as a missing take.
/// </summary>
public sealed class OggVoiceDecoder : IVoiceDecoder
{
    public float[] Decode(byte[] file)
    {
        try
        {
            using var reader = new VorbisReader(new MemoryStream(file), true);
            int channels = reader.Channels, rate = reader.SampleRate;
            if (channels is < 1 or > 2 || rate <= 0) throw new InvalidDataException("an Ogg file of unsupported channels or rate");
            var mono = new List<float>();
            var buffer = new float[4096 * channels];
            int read;
            while ((read = reader.ReadSamples(buffer, 0, buffer.Length)) > 0)
                for (int i = 0; i + channels <= read; i += channels)
                    mono.Add(channels == 1 ? buffer[i] : (buffer[i] + buffer[i + 1]) * 0.5f);
            if (mono.Count == 0) throw new InvalidDataException("an Ogg file with no audio");
            var samples = mono.ToArray();
            return rate == Synthesizer.DsRate ? samples : WavDecoder.Resample(samples, rate, Synthesizer.DsRate);
        }
        catch (Exception e) when (e is not InvalidDataException)
        {
            throw new InvalidDataException("not a readable Ogg Vorbis file: " + e.Message, e);
        }
    }
}
