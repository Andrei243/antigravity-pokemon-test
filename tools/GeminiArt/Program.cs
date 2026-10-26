using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

// Concept art for the sculpting sessions, drawn by Gemini from the proposals' written briefs.
//   dotnet run --project tools/GeminiArt -- list
//   dotnet run --project tools/GeminiArt -- prompt <name>
//   dotnet run --project tools/GeminiArt -- generate [--set kanto|johto|hoenn|sinnoh|megas] [--model <id>] [--size 1K|2K]
//                                                   [--force] [--delay <ms>] [--limit <n>] [name ...]
// The key is read from the GEMINI_API_KEY environment variable and never written anywhere.
// Pictures go to docs/art/reference/gemini/<set>/: references for the sculptors, not game art, not shipped.

return await Cli.Run(args);

static class Cli
{
    const string DefaultModel = "gemini-nano-banana-2.1";
    const string Endpoint = "https://generativelanguage.googleapis.com/v1beta/interactions";

    public static async Task<int> Run(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help")
        {
            Console.WriteLine("usage: GeminiArt list | prompt <name> | generate [--set s] [--model id] [--size 1K] [--force] [--delay ms] [--limit n] [name ...]");
            return args.Length == 0 ? 2 : 0;
        }

        string root = FindRoot();
        var all = Briefs.ReadAll(root);
        string verb = args[0];
        var rest = args.Skip(1).ToList();

        string? set = Take(rest, "--set");
        string model = Take(rest, "--model") ?? Environment.GetEnvironmentVariable("GEMINI_IMAGE_MODEL") ?? DefaultModel;
        string size = Take(rest, "--size") ?? "1K";
        int delay = int.TryParse(Take(rest, "--delay"), out var d) ? d : 1500;
        int limit = int.TryParse(Take(rest, "--limit"), out var l) ? l : int.MaxValue;
        bool force = Flag(rest, "--force");

        var chosen = all.Where(b => set == null || b.Set == set)
                        .Where(b => rest.Count == 0 || rest.Any(n => b.Name.Equals(n, StringComparison.OrdinalIgnoreCase)
                                                                 || b.Slug.Equals(n, StringComparison.OrdinalIgnoreCase)))
                        .ToList();

        switch (verb)
        {
            case "list":
                foreach (var g in all.GroupBy(b => b.Set))
                    Console.WriteLine($"{g.Key,-8} {g.Count()} briefs");
                Console.WriteLine($"{all.Count} in all");
                return 0;

            case "prompt":
                if (chosen.Count == 0) { Console.Error.WriteLine("no brief by that name"); return 2; }
                foreach (var b in chosen.Take(3)) Console.WriteLine(b.Prompt() + "\n");
                return 0;

            case "fetch-official":
                return await FetchOfficial(root, chosen.Where(b => b.IsMegaDynamax).ToList(), delay);

            case "generate":
                return await Generate(root, chosen, model, size, delay, limit, force);

            default:
                Console.Error.WriteLine($"unknown command {verb}");
                return 2;
        }
    }

    static async Task<int> Generate(string root, List<Brief> chosen, string model, string size, int delay, int limit, bool force)
    {
        string? key = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (string.IsNullOrWhiteSpace(key))
        {
            Console.Error.WriteLine("set GEMINI_API_KEY first (the key is only read from the environment).");
            return 2;
        }
        if (chosen.Count == 0) { Console.Error.WriteLine("nothing matches."); return 2; }

        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
        http.DefaultRequestHeaders.Add("x-goog-api-key", key);

        int made = 0, skipped = 0, failed = 0;
        foreach (var b in chosen)
        {
            if (made >= limit) break;
            string dir = Path.Combine(root, "docs", "art", "reference", "gemini", b.Set);
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, b.Slug + ".jpg");
            if (File.Exists(path) && !force) { skipped++; continue; }

            Console.Write($"{b.Set}/{b.Slug} ... ");
            // A Mega Dynamax of one of our own Megas is shown that Mega's picture, when it has one: the base design to scale up.
            byte[]? baseImage = null;
            if (b.IsMegaDynamax)
            {
                string basePath = Path.Combine(root, "docs", "art", "reference", "gemini", "megas", Briefs.SlugOf(b.BaseMega) + ".jpg");
                if (File.Exists(basePath)) baseImage = await File.ReadAllBytesAsync(basePath);
                else if (File.Exists(OfficialPath(root, b))) baseImage = await File.ReadAllBytesAsync(OfficialPath(root, b));
            }
            var result = await Ask(http, model, size, b.Prompt(baseImage != null), dir, baseImage);
            if (result.Bytes == null)
            {
                Console.WriteLine("FAILED: " + result.Error);
                failed++;
                if (result.Fatal) return 1;
            }
            else
            {
                await File.WriteAllBytesAsync(path, result.Bytes);
                Console.WriteLine($"{result.Bytes.Length / 1024} KB");
                made++;
            }
            await Task.Delay(delay);
        }
        Console.WriteLine($"made {made}, kept {skipped}, failed {failed}");
        return failed == 0 ? 0 : 1;
    }

    record Reply(byte[]? Bytes, string? Error, bool Fatal);

    static async Task<Reply> Ask(HttpClient http, string model, string size, string prompt, string dumpDir, byte[]? baseImage = null)
    {
        var input = new List<object> { new { type = "text", text = prompt } };
        if (baseImage != null)
            input.Add(new { type = "image", data = Convert.ToBase64String(baseImage), mime_type = "image/jpeg" });
        var body = JsonSerializer.Serialize(new
        {
            model,
            input,
            response_format = new { type = "image", mime_type = "image/jpeg", aspect_ratio = "1:1", image_size = size },
        });

        for (int attempt = 1; attempt <= 4; attempt++)
        {
            HttpResponseMessage resp;
            try
            {
                resp = await http.PostAsync(Endpoint, new StringContent(body, Encoding.UTF8, "application/json"));
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
            {
                if (attempt == 4) return new(null, e.Message, false);
                await Task.Delay(2000 * attempt);
                continue;
            }

            string text = await resp.Content.ReadAsStringAsync();
            if (resp.IsSuccessStatusCode)
            {
                var png = FindImage(text);
                if (png != null) return new(png, null, false);
                // The response shape is not the one this tool expects: keep it (images cut short) to be read.
                string dump = Path.Combine(dumpDir, "_last_response.json");
                File.WriteAllText(dump, Regex.Replace(text, "[A-Za-z0-9+/=]{200,}", "<base64>"));
                return new(null, $"no image in the reply (shape kept in {dump}); the model may have answered in words, or refused the brief", false);
            }

            int code = (int)resp.StatusCode;
            if (code is 400 or 401 or 403 or 404)
                return new(null, $"HTTP {code}: {Trim(text)}", Fatal: true);
            if (attempt == 4) return new(null, $"HTTP {code}: {Trim(text)}", false);
            await Task.Delay(code == 429 ? 15000 * attempt : 3000 * attempt);
        }
        return new(null, "gave up", false);
    }

    // The Interactions API's reply carries the picture as base64 beside an image mime type; whichever field names
    // it uses (data, inlineData.data, inline_data.data), the first long base64 string with an image sibling is the picture.
    static byte[]? FindImage(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return Walk(doc.RootElement);

        static byte[]? Walk(JsonElement e)
        {
            if (e.ValueKind == JsonValueKind.Object)
            {
                foreach (var p in e.EnumerateObject())
                {
                    if (p.Name == "data" && p.Value.ValueKind == JsonValueKind.String && p.Value.GetString()!.Length > 1000)
                    {
                        string mime = "";
                        foreach (var q in e.EnumerateObject())
                            if (q.Name is "mime_type" or "mimeType" && q.Value.ValueKind == JsonValueKind.String) mime = q.Value.GetString()!;
                        if (mime.StartsWith("image/") || mime == "")
                        {
                            try { return Convert.FromBase64String(p.Value.GetString()!); } catch (FormatException) { }
                        }
                    }
                    var inner = Walk(p.Value);
                    if (inner != null) return inner;
                }
            }
            else if (e.ValueKind == JsonValueKind.Array)
            {
                foreach (var x in e.EnumerateArray())
                {
                    var inner = Walk(x);
                    if (inner != null) return inner;
                }
            }
            return null;
        }
    }

    static string Trim(string s) => s.Length > 400 ? s[..400] + "..." : s;

    // The official picture of a Mega Dynamax's base Mega, kept in a git-ignored cache (never committed, never shipped):
    // it is only handed to Gemini as the design to scale up. "Venusaur-Mega-Mmax" -> "venusaur-mega".
    static string OfficialPath(string root, Brief b) =>
        Path.Combine(root, "tools", "GeminiArt", ".cache", "official", b.Form.Replace("-Mmax", "").ToLowerInvariant() + ".jpg");

    static async Task<int> FetchOfficial(string root, List<Brief> chosen, int delay)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(1) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
        int got = 0, had = 0, missing = 0;
        foreach (var b in chosen)
        {
            string path = OfficialPath(root, b);
            string slug = Path.GetFileNameWithoutExtension(path);
            // Our own Megas have a picture in megas/ already; nothing official exists for them.
            if (File.Exists(Path.Combine(root, "docs", "art", "reference", "gemini", "megas", Briefs.SlugOf(b.BaseMega) + ".jpg"))) { had++; continue; }
            if (File.Exists(path)) { had++; continue; }
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            try
            {
                var resp = await http.GetAsync($"https://img.pokemondb.net/artwork/large/{slug}.jpg");
                var bytes = resp.IsSuccessStatusCode ? await resp.Content.ReadAsByteArrayAsync() : null;
                // A JPEG starts FF D8; anything else (an error page) is not a picture.
                if (bytes == null || bytes.Length < 2000 || bytes[0] != 0xFF || bytes[1] != 0xD8)
                {
                    Console.WriteLine($"{slug}: none ({(int)resp.StatusCode})");
                    missing++;
                }
                else
                {
                    await File.WriteAllBytesAsync(path, bytes);
                    Console.WriteLine($"{slug}: {bytes.Length / 1024} KB");
                    got++;
                }
            }
            catch (HttpRequestException e) { Console.WriteLine($"{slug}: {e.Message}"); missing++; }
            await Task.Delay(delay);
        }
        Console.WriteLine($"fetched {got}, had {had}, none {missing}");
        return 0;
    }

    static string? Take(List<string> a, string flag)
    {
        int i = a.IndexOf(flag);
        if (i < 0 || i + 1 >= a.Count) return null;
        string v = a[i + 1];
        a.RemoveRange(i, 2);
        return v;
    }

    static bool Flag(List<string> a, string flag) => a.Remove(flag);

    static string FindRoot()
    {
        for (var d = new DirectoryInfo(Directory.GetCurrentDirectory()); d != null; d = d.Parent)
            if (File.Exists(Path.Combine(d.FullName, "PokemonPlatinum.sln"))) return d.FullName;
        throw new InvalidOperationException("run this from inside the repository");
    }
}

record Brief(string Set, string Name, string Slug, string Typing, string Category, string Look, string Size, string Form = "")
{
    // One prompt for every creature, so the sheet reads as one set of references.
    public bool IsMegaDynamax => Name.StartsWith("Mega Dynamax ", StringComparison.OrdinalIgnoreCase);

    // "Mega Dynamax Charizard X" grows from "Mega Charizard X".
    public string BaseMega => "Mega " + Name["Mega Dynamax ".Length..];

    public string Prompt(bool baseAttached = false)
    {
        if (IsMegaDynamax) return MegaDynamaxPrompt(baseAttached);
        var sb = new StringBuilder();
        sb.AppendLine("Concept art for a 3D sculptor: one original fictional creature for a monster-collecting video game, drawn as a character design reference.");
        sb.AppendLine("Show the whole body in a clean three-quarter view, standing on nothing, centred on a plain white background, with enough room round it. Clean dark outlines, flat colours with one soft shade and one highlight, readable silhouette. No text, no labels, no logo, no border, no second creature, no scenery.");
        sb.AppendLine("This is a new, original design. It must not copy or imitate any existing Pokémon or other franchise character.");
        sb.AppendLine();
        sb.AppendLine($"Name (do not write it in the picture): {Name}");
        if (Typing.Length > 0) sb.AppendLine($"Elemental typing, for mood and palette: {Typing}");
        if (Category.Length > 0) sb.AppendLine($"Category: {Category}");
        if (Size.Length > 0) sb.AppendLine($"Real-world size: {Size}");
        sb.AppendLine("Design brief:");
        sb.AppendLine(Look);
        return sb.ToString();
    }

    // A Mega Dynamax is the giant (Dynamax) state of a Mega-Evolved creature, so the base Mega is named and kept true to.
    string MegaDynamaxPrompt(bool baseAttached)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Concept art for a 3D sculptor: the giant Dynamax form of a Mega-Evolved creature from a monster-collecting video game, drawn as a character design reference.");
        sb.AppendLine($"The base creature is {BaseMega}. It has Mega Evolved and then Dynamaxed, so this picture is {Name}: the same Mega-Evolved creature made colossal, as in the games' Dynamax battles, with the same design, colours and markings as {BaseMega}, only grown to titanic size and given the changes in the brief below.");
        if (baseAttached)
            sb.AppendLine($"The attached picture is {BaseMega} at its normal size: keep its design, proportions and colours, and apply the brief to it.");
        sb.AppendLine("Show the whole body in a three-quarter view seen from slightly below, so it reads as a towering giant, centred on a plain white background with enough room round it. A swirl of red and violet Dynamax energy and cloud rises round its feet and the edges of its body, and nothing else; the creature itself is the subject. Clean dark outlines, flat colours with one soft shade and one highlight, readable silhouette. No text, no labels, no logo, no border, no second creature, no scenery.");
        sb.AppendLine();
        sb.AppendLine($"Name (do not write it in the picture): {Name}");
        if (Typing.Length > 0) sb.AppendLine($"Elemental typing, for mood and palette: {Typing}");
        if (Size.Length > 0) sb.AppendLine($"Height at this size: {Size}");
        sb.AppendLine("Design brief:");
        sb.AppendLine(Look);
        return sb.ToString();
    }
}

static class Briefs
{
    static readonly (string Set, string File)[] Files =
    {
        ("kanto", "D14-proposals-kanto.md"), ("johto", "D14-proposals-johto.md"),
        ("hoenn", "D14-proposals-hoenn.md"), ("sinnoh", "D14-proposals-sinnoh.md"),
        ("unova", "D14-proposals-unova.md"), ("kalos", "D14-proposals-kalos.md"),
        ("alola", "D14-proposals-alola.md"), ("galar", "D14-proposals-galar.md"),
        ("paldea", "D14-proposals-paldea.md"), ("megas", "R20b-mega-proposals.md"), ("megadynamax", "R20c-mega-dynamax-proposals.md"),
        ("alola", "D14-proposals-alola-supplement.md"), ("galar", "D14-proposals-galar-supplement.md"),
        ("kalos", "D14-proposals-kalos-supplement.md"), ("paldea", "D14-proposals-paldea-supplement.md"),
    };

    public static List<Brief> ReadAll(string root)
    {
        var list = new List<Brief>();
        foreach (var (set, file) in Files)
        {
            string path = Path.Combine(root, "docs", "plans", file);
            if (!File.Exists(path)) continue;
            list.AddRange(Read(set, File.ReadAllLines(path)));
        }
        return list;
    }

    static readonly Regex Heading = new(@"^### \d+\. (?<head>.+)$");

    static List<Brief> Read(string set, string[] lines)
    {
        var found = new List<Brief>();
        for (int i = 0; i < lines.Length; i++)
        {
            var m = Heading.Match(lines[i]);
            if (!m.Success) continue;

            // "Name, from Forebear (20): say"  or  "Mega Name: `Form`, Stone"
            string head = m.Groups["head"].Value;
            string name = Regex.Match(head, @"^[^,:(]+").Value.Trim();
            string typing = "", look = "", category = "", size = "", number = "";
            string form = Regex.Match(head, "`(?<f>[^`]+)`").Groups["f"].Value; // "Venusaur-Mega-Mmax"
            for (int j = i + 1; j < lines.Length && !lines[j].StartsWith("### ") && !lines[j].StartsWith("## "); j++)
            {
                string t = lines[j];
                if (t.StartsWith("- **Typing**:")) typing = Clean(After(t));
                else if (t.StartsWith("- **Look**:")) look = Clean(After(t));
                else
                {
                    var cat = Regex.Match(t, @"^- \*\*(?<c>The [^*]+)\*\*, No\. (?<n>\d+)\.? ?(?<s>.*)$");
                    if (cat.Success) { category = cat.Groups["c"].Value; number = cat.Groups["n"].Value; size = Clean(cat.Groups["s"].Value); }
                }
            }
            if (look.Length == 0) continue;
            // A Mega Dynamax's height is in its heading: "Mega Dynamax Venusaur: `Venusaur-Mega-Mmax`, about 29 m (yes)".
            if (size.Length == 0)
            {
                var about = Regex.Match(head, @"about (?<h>[0-9.]+ m)");
                if (about.Success) size = about.Groups["h"].Value;
            }
            // Typing lines argue the case for the typing; the part after the arrow (or the first sentence) is the result.
            typing = typing.Contains('→') ? typing[(typing.IndexOf('→') + 1)..] : typing;
            typing = Regex.Match(typing.Trim(), @"^[A-Za-z /]+").Value.Trim();

            string slug = Slug(number.Length > 0 ? $"{number}-{name}" : name);
            found.Add(new Brief(set, name, slug, typing, category, look, size, form));
        }
        return found;
    }

    static string After(string line) => line[(line.IndexOf("**:", StringComparison.Ordinal) + 3)..].Trim();

    // The notes' own markup (bold, code, our engine's words) means nothing to a picture-maker.
    static string Clean(string s) => Regex.Replace(s.Replace("**", "").Replace("`", ""), @"\s+", " ").Trim();

    public static string SlugOf(string name) => Slug(name);

    static string Slug(string s) => Regex.Replace(s, @"[^A-Za-z0-9\-]+", "-").Trim('-');
}
