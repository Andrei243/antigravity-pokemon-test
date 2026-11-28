using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Data;

/// <summary>
/// Takes written as Ogg Vorbis (OggVorbisEncoder, MIT), about a tenth of a WAV's size, which the game plays as it
/// plays a WAV (<see cref="OggVoiceDecoder"/>). <c>generate</c> writes every new take this way, and <c>ogg</c> turns
/// a pack's WAV takes into Ogg ones, so the packs are small enough to keep in Git LFS.
/// </summary>
static class OggEncode
{
    /// <summary>Speech in mono: 0.5 is about 60 kbit/s (0.4 writes books NVorbis cannot read), clear for a voice and far under the WAV's 524.</summary>
    const float Quality = 0.5f;

    public static (byte[] Bytes, string Extension) Take(float[] samples, int rate) => (Encode(samples, rate), ".ogg");

    public static byte[] Encode(float[] samples, int rate)
    {
        // The codec rings past a take's edges and over its loudest peak: 25 ms of silence each side (5 left some ends ringing) keep
        // its ends at nothing (no click), and a ceiling of -2 dB keeps the decoded peak under the check's -1 dB
        const float ceiling = 0.79f;
        int pad = rate / 40;
        float peak = samples.Length == 0 ? 0 : samples.Max(MathF.Abs);
        float gain = peak > ceiling ? ceiling / peak : 1f;
        var padded = new float[samples.Length + 2 * pad];
        for (int i = 0; i < samples.Length; i++) padded[pad + i] = samples[i] * gain;
        samples = padded;
        var info = OggVorbisEncoder.VorbisInfo.InitVariableBitRate(1, rate, Quality);
        // A fixed serial, so the same take encodes to the same bytes and its hash holds from run to run
        var stream = new OggVorbisEncoder.OggStream(1);
        stream.PacketIn(OggVorbisEncoder.HeaderPacketBuilder.BuildInfoPacket(info));
        stream.PacketIn(OggVorbisEncoder.HeaderPacketBuilder.BuildCommentsPacket(new OggVorbisEncoder.Comments()));
        stream.PacketIn(OggVorbisEncoder.HeaderPacketBuilder.BuildBooksPacket(info));
        var output = new MemoryStream();
        void Pages(bool flush)
        {
            while (stream.PageOut(out var page, flush))
            {
                output.Write(page.Header, 0, page.Header.Length);
                output.Write(page.Body, 0, page.Body.Length);
            }
        }
        Pages(true);
        var processing = OggVorbisEncoder.ProcessingState.Create(info);
        // In blocks, as the encoder is meant to be fed
        const int block = 4096;
        for (int at = 0; at < samples.Length; at += block)
        {
            int n = Math.Min(block, samples.Length - at);
            var chunk = new float[n];
            Array.Copy(samples, at, chunk, 0, n);
            processing.WriteData(new[] { chunk }, n);
            while (!stream.Finished && processing.PacketOut(out var packet))
            {
                stream.PacketIn(packet);
                Pages(false);
            }
        }
        processing.WriteEndOfStream();
        while (!stream.Finished && processing.PacketOut(out var packet))
        {
            stream.PacketIn(packet);
            Pages(false);
        }
        Pages(true);
        return output.ToArray();
    }

    /// <summary>
    /// The takes <see cref="VoicePacks.Check"/> finds wrong encoded again from their WAVs kept elsewhere (a backup of the
    /// pack as it was before <see cref="Convert"/>), so none is encoded twice from an Ogg.
    /// </summary>
    public static int Redo(string folder, string wavFolder, VoiceFile catalogue)
    {
        string packPath = Path.Combine(folder, VoiceLibrary.PackFileName);
        var library = VoiceLibrary.Open(folder) ?? throw new InvalidOperationException($"no pack at {folder}");
        var wrong = VoicePacks.Check(library, catalogue).Wrong.Select(f => f.Key).ToHashSet(StringComparer.Ordinal);
        var pack = GameDataFiles.Deserialize<VoicePackFile>(File.ReadAllText(packPath));
        var wav = new WavDecoder();
        int done = 0, missing = 0;
        foreach (var entry in pack.Lines.Where(e => wrong.Contains(e.Key)))
        {
            string from = Path.Combine(wavFolder, Path.ChangeExtension(entry.File, ".wav"));
            if (!File.Exists(from)) { missing++; continue; }
            byte[] bytes = Encode(wav.Decode(File.ReadAllBytes(from)), Synthesizer.DsRate);
            string relative = Path.ChangeExtension(entry.File, ".ogg").Replace('\\', '/');
            File.WriteAllBytes(Path.Combine(folder, relative), bytes);
            entry.File = relative;
            entry.FileHash = VoiceLibrary.FileHashOf(bytes);
            done++;
        }
        File.WriteAllText(packPath, GameDataFiles.Serialize(pack));
        Console.WriteLine($"{folder}: {done} of {wrong.Count} takes found wrong encoded again from {wavFolder}" + (missing > 0 ? $"; {missing} had no WAV there" : "") + ".");
        return missing > 0 ? 1 : 0;
    }

    /// <summary>
    /// Every WAV take of a pack written again as Ogg: the file beside it, the entry's file and hash, the WAV deleted
    /// once the Ogg decodes, and the pack's manifest saved every few hundred so a stop loses nothing.
    /// </summary>
    public static int Convert(string folder)
    {
        string packPath = Path.Combine(folder, VoiceLibrary.PackFileName);
        var pack = GameDataFiles.Deserialize<VoicePackFile>(File.ReadAllText(packPath));
        var wav = new WavDecoder();
        var ogg = new OggVoiceDecoder();
        var todo = pack.Lines.Where(e => e.File.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)).ToList();
        long before = 0, after = 0;
        int done = 0, missing = 0;
        var deleted = new List<string>();
        void Save()
        {
            File.WriteAllText(packPath, GameDataFiles.Serialize(pack));
            foreach (var path in deleted) File.Delete(path);
            deleted.Clear();
        }
        foreach (var entry in todo)
        {
            string from = Path.Combine(folder, entry.File);
            if (!File.Exists(from)) { missing++; continue; }
            byte[] wavBytes = File.ReadAllBytes(from);
            // The game decodes to the DS rate, which is the rate every take is baked at
            float[] samples = wav.Decode(wavBytes);
            byte[] bytes = Encode(samples, Synthesizer.DsRate);
            if (ogg.Decode(bytes).Length == 0) throw new InvalidOperationException($"{entry.Key}: its Ogg came back empty");
            string relative = Path.ChangeExtension(entry.File, ".ogg").Replace('\\', '/');
            File.WriteAllBytes(Path.Combine(folder, relative), bytes);
            entry.File = relative;
            entry.FileHash = VoiceLibrary.FileHashOf(bytes);
            deleted.Add(from);
            before += wavBytes.Length;
            after += bytes.Length;
            if (++done % 250 == 0)
            {
                Save();
                Console.WriteLine($"{done} of {todo.Count} ({before / 1048576} MB of WAV to {after / 1048576} MB of Ogg)");
            }
        }
        Save();
        Console.WriteLine($"{folder}: {done} takes now Ogg, {before / 1048576} MB of WAV to {after / 1048576} MB" + (missing > 0 ? $"; {missing} entries had no file" : "") + ".");
        return missing > 0 ? 1 : 0;
    }
}
