using System.Net;
using System.Text;
using System.Text.Json;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Data;

/// <summary>
/// Gemini's speech model (<c>tools/VoiceGen/gemini.json</c>), for the packs in other languages than English, which
/// Kokoro does not speak: <c>generate --engine gemini --language ro</c>. Every voice is designed as the English ones
/// are, a prebuilt voice and a few words of delivery, never a clone. The words go in brackets before the line, which
/// Gemini follows and does not read aloud; a sentence of direction it reads aloud. The key is <c>GEMINI_API_KEY</c>,
/// sent in a header only. A take is asked with the job's seed, though Gemini may still say a line otherwise each time.
/// </summary>
public sealed class GeminiEngine : IVoiceEngine, IDisposable
{
    public sealed class Config
    {
        public string? Note { get; set; }
        public string Model { get; set; } = "gemini-3.8-flash-tts";
        public Dictionary<string, GeminiVoice> Voices { get; set; } = new();
    }

    public sealed class GeminiVoice
    {
        public string Voice { get; set; } = "";
        public string Tags { get; set; } = "";
    }

    private readonly Config config;
    private readonly CastFile cast;
    private readonly string key;
    private readonly HttpClient http = new() { Timeout = TimeSpan.FromMinutes(2) };

    public string Name => "Gemini " + config.Model;

    public GeminiEngine(string configPath, CastFile cast)
    {
        config = GameDataFiles.Deserialize<Config>(File.ReadAllText(configPath));
        this.cast = cast;
        key = Environment.GetEnvironmentVariable("GEMINI_API_KEY") is { Length: > 0 } k ? k
            : throw new InvalidOperationException("GEMINI_API_KEY is not set.");
    }

    /// <summary>The voice and the delivery a cast voice is said in: its own row of the file, or one by rule from its brief.</summary>
    public GeminiVoice VoiceOf(string voice)
    {
        if (config.Voices.TryGetValue(voice, out var own)) return own;
        // An archetype's voices (lass#2) share their archetype's row where it has one
        int hash = voice.IndexOf('#');
        if (hash > 0 && config.Voices.TryGetValue(voice[..hash], out var archetype)) return archetype;
        return ByRule(voice, cast.Voices.TryGetValue(voice, out var v) ? v.Brief : null);
    }

    // Gemini's prebuilt voices by register, low to high (the docs' own words for each beside it)
    private static readonly string[] Men = { "Algenib", "Charon", "Orus", "Alnilam", "Iapetus", "Schedar", "Achird", "Umbriel", "Puck", "Fenrir", "Sadachbia" };
    private static readonly string[] Women = { "Gacrux", "Sulafat", "Kore", "Despina", "Vindemiatrix", "Aoede", "Callirrhoe", "Erinome", "Autonoe", "Zephyr", "Leda", "Laomedeia" };

    /// <summary>A voice by rule: the register and pitch choose among Gemini's voices (the id's hash among those near), the brief's words make the tags.</summary>
    public static GeminiVoice ByRule(string voice, CastBrief? brief)
    {
        bool woman = brief?.Register is "woman" or "girl";
        var pool = woman ? Women : Men;
        int third = pool.Length / 3;
        int from = brief?.Pitch switch { "low" => 0, "high" => pool.Length - third, _ => third };
        if (brief?.Register is "boy" or "girl") from = pool.Length - third;
        string name = pool[from + (int)(VoiceCast.Hash32(voice) % (uint)third)];
        var tags = new List<string>();
        if (brief != null)
        {
            string who = (brief.Register, brief.Age) switch
            {
                ("boy", _) => "young boy",
                ("girl", _) => "young girl",
                (_, "teen") => woman ? "teenage girl" : "teenage boy",
                (_, "elder") => woman ? "elderly woman" : "elderly man",
                (_, "middle-aged") => woman ? "middle-aged woman" : "middle-aged man",
                _ => woman ? "woman" : "man"
            };
            tags.Add(who);
            if (brief.Pace is "slow" or "quick") tags.Add(brief.Pace == "quick" ? "fast" : "slow");
            if (brief.Manner is { Length: > 0 } manner)
            {
                // The manner's first words, without a "Name:" or "of the …" lead, as a few tags
                string m = manner.Contains(':') ? manner[(manner.IndexOf(':') + 1)..] : manner;
                tags.AddRange(m.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).Where(t => t.Length > 0 && !t.StartsWith("of ", StringComparison.Ordinal)).Take(3));
            }
        }
        return new GeminiVoice { Voice = name, Tags = string.Join(", ", tags) };
    }

    /// <summary>The text Gemini is handed: the delivery in brackets, then the line.</summary>
    public string PromptOf(VoiceJob job)
    {
        var v = VoiceOf(job.Voice);
        string direction = job.Direction switch
        {
            "calm" or "" => "",
            "frustrated" => "frustrated",
            "excited" => "excited",
            "shouting" => "shouting",
            "laughing" => "laughing",
            "quiet" => "quietly",
            "whispering" => "whispering",
            "sad" => "sad",
            "smug" => "smug",
            var d => d
        };
        string tags = string.Join(", ", new[] { v.Tags, direction }.Where(t => t.Length > 0));
        return tags.Length > 0 ? $"[{tags}] {job.Text}" : job.Text;
    }

    /// <summary>Whether Gemini has said the day's requests are used up: every take asked after that is given up unasked.</summary>
    public bool OutForTheDay { get; private set; }

    public (float[] Samples, int Rate)? Speak(VoiceJob job, int seed)
    {
        if (OutForTheDay) return null;
        var v = VoiceOf(job.Voice);
        var body = new
        {
            contents = new[] { new { parts = new[] { new { text = PromptOf(job) } } } },
            generationConfig = new
            {
                responseModalities = new[] { "AUDIO" },
                seed,
                speechConfig = new { voiceConfig = new { prebuiltVoiceConfig = new { voiceName = v.Voice } } }
            }
        };
        string json = JsonSerializer.Serialize(body);
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{config.Model}:generateContent";
        for (int attempt = 0; attempt < 6; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
            request.Headers.Add("x-goog-api-key", key);
            using var response = http.Send(request);
            string answer = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            // The day's requests used up (the free tier gives 100 a day): no wait helps, so every line after this one is
            // given up at once, and the voices already made are written into the pack; the next run picks up from there
            if (response.StatusCode == HttpStatusCode.TooManyRequests && answer.Contains("PerDay", StringComparison.Ordinal))
            {
                if (!OutForTheDay) Console.Error.WriteLine($"{job.Key}: Gemini's requests for the day are used up; the rest of this run is left for another day.");
                OutForTheDay = true;
                return null;
            }
            if (response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable or HttpStatusCode.InternalServerError)
            {
                int wait = 10 * (1 << attempt);
                Console.Error.WriteLine($"{job.Key}: Gemini said {(int)response.StatusCode}; waiting {wait} s.");
                Thread.Sleep(wait * 1000);
                continue;
            }
            if (!response.IsSuccessStatusCode)
            {
                Console.Error.WriteLine($"{job.Key}: Gemini said {(int)response.StatusCode}: {answer[..Math.Min(300, answer.Length)]}");
                return null;
            }
            return AudioOf(answer, job.Key);
        }
        return null;
    }

    private static (float[] Samples, int Rate)? AudioOf(string answer, string key)
    {
        using var doc = JsonDocument.Parse(answer);
        if (!doc.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0
            || !candidates[0].TryGetProperty("content", out var content) || !content.TryGetProperty("parts", out var parts))
        {
            Console.Error.WriteLine($"{key}: Gemini gave no audio: {answer[..Math.Min(300, answer.Length)]}");
            return null;
        }
        foreach (var part in parts.EnumerateArray())
        {
            if (!part.TryGetProperty("inlineData", out var data)) continue;
            string mime = data.GetProperty("mimeType").GetString() ?? "";
            byte[] bytes = Convert.FromBase64String(data.GetProperty("data").GetString() ?? "");
            if (mime.Contains("wav", StringComparison.OrdinalIgnoreCase))
            {
                var wave = WavDecoder.Read(bytes);
                return (wave.Mono, wave.Rate);
            }
            // audio/L16;codec=pcm;rate=24000: bare 16-bit little-endian mono
            int rate = 24000;
            if (mime.IndexOf("rate=", StringComparison.Ordinal) is int r and >= 0 && int.TryParse(new string(mime[(r + 5)..].TakeWhile(char.IsDigit).ToArray()), out int given)) rate = given;
            var samples = new float[bytes.Length / 2];
            for (int i = 0; i < samples.Length; i++) samples[i] = BitConverter.ToInt16(bytes, i * 2) / 32768f;
            return (samples, rate);
        }
        Console.Error.WriteLine($"{key}: Gemini's answer had no audio part.");
        return null;
    }

    public void Dispose() => http.Dispose();
}
