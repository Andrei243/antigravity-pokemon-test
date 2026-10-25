using System.Text.Json;
using System.Text.Json.Nodes;

/// <summary>One concept picture and where its sculpt goes.</summary>
sealed record Concept(string Set, string Slug, string Name, string Image)
{
    public string Dir(string root) => Path.Combine(root, "tools", "TripoSculpt", "out", Set);
    public string Glb(string root) => Path.Combine(Dir(root), Slug + ".glb");
    public string Meta(string root) => Path.Combine(Dir(root), Slug + ".json");

    /// <summary>The species' file name for the game: the concept's slug without the category number GeminiArt puts first.</summary>
    public string Species => System.Text.RegularExpressions.Regex.Replace(Slug, @"^\d+-", "");

    public static List<Concept> ReadAll(string root)
    {
        var list = new List<Concept>();
        string art = Path.Combine(root, "docs", "art", "reference", "gemini");
        if (!Directory.Exists(art)) return list;
        foreach (string dir in Directory.GetDirectories(art).OrderBy(d => d, StringComparer.Ordinal))
            foreach (string file in Directory.GetFiles(dir).Where(f => f.EndsWith(".jpg") || f.EndsWith(".png")).OrderBy(f => f, StringComparer.Ordinal))
            {
                string slug = Path.GetFileNameWithoutExtension(file);
                if (slug.StartsWith('_')) continue;
                list.Add(new Concept(Path.GetFileName(dir), slug, slug.Replace('-', ' '), file));
            }
        return list;
    }

    public bool Matches(string word) =>
        Slug.Equals(word, StringComparison.OrdinalIgnoreCase)
        || Species.Equals(word, StringComparison.OrdinalIgnoreCase)
        || Species.Replace('-', ' ').Equals(word, StringComparison.OrdinalIgnoreCase);
}

sealed record Options(string Model, int FaceLimit, bool Texture, bool Pbr, bool Autofix, int? Seed, bool Force,
                      int Limit, int DelayMs, TimeSpan Timeout, int PollMs)
{
    public const string DefaultModel = "v3.1-20260211";

    // The game reads the base colour only (docs/model-files.md), so PBR maps would be paid for and thrown away;
    // 30,000 triangles is far over what a 128-pixel sprite or a battle platform shows and still loads quickly.
    public static Options Default => new(DefaultModel, 30000, true, false, false, null, false, int.MaxValue, 1000, TimeSpan.FromMinutes(10), 3000);

    public JsonObject Request(string input)
    {
        var body = new JsonObject
        {
            ["input"] = input,
            ["model"] = Model,
            ["face_limit"] = FaceLimit,
            ["texture"] = Texture || Pbr,
            ["pbr"] = Pbr,
            ["enable_image_autofix"] = Autofix,
        };
        if (Seed is int s) body["model_seed"] = s;
        return body;
    }
}

sealed record Outcome(int Made, int Kept, int Failed, double Credits, bool Aborted);

static class Sculptor
{
    /// <summary>
    /// Makes a sculpt of each concept. A picture whose sculpt is there is kept. One whose task was submitted but not
    /// fetched (the run was stopped, the download failed) goes on with that task, so no credits are spent twice.
    /// </summary>
    public static async Task<Outcome> Generate(string root, TripoClient client, List<Concept> chosen, Options o, TextWriter log)
    {
        int made = 0, kept = 0, failed = 0;
        double credits = 0;
        foreach (var c in chosen)
        {
            if (made >= o.Limit) break;
            Directory.CreateDirectory(c.Dir(root));
            if (File.Exists(c.Glb(root)) && !o.Force) { kept++; continue; }

            log.Write($"{c.Set}/{c.Slug} ... ");
            try
            {
                var meta = o.Force ? new JsonObject() : ReadMeta(c.Meta(root));
                string? taskId = meta["taskId"]?.GetValue<string>();
                if (taskId == null)
                {
                    string token = await client.Upload(c.Image);
                    var request = o.Request(token);
                    taskId = await client.CreateImageToModel(request);
                    meta = new JsonObject
                    {
                        ["name"] = c.Name, ["set"] = c.Set, ["image"] = Path.GetFileName(c.Image),
                        ["request"] = RequestWithoutToken(request), ["taskId"] = taskId,
                        ["submittedAt"] = DateTime.UtcNow.ToString("o"),
                    };
                    WriteMeta(c.Meta(root), meta);
                    log.Write($"task {taskId[..Math.Min(8, taskId.Length)]} ");
                }
                else log.Write($"going on with task {taskId[..Math.Min(8, taskId.Length)]} ");

                int lastShown = -1;
                var task = await client.Wait(taskId, o.Timeout, o.PollMs, t =>
                {
                    if (t.Progress / 25 != lastShown) { lastShown = t.Progress / 25; log.Write($"{t.Progress}% "); }
                });
                if (task.Failed)
                {
                    meta["status"] = task.Status;
                    meta["message"] = task.Message;
                    meta.Remove("taskId");   // a failed task is not gone back to: the next run submits afresh
                    WriteMeta(c.Meta(root), meta);
                    throw new TripoException($"the task {task.Status}{(task.Message is null ? "" : ": " + task.Message)}", false);
                }
                if (task.ModelUrl == null) throw new TripoException("the task succeeded but names no model", false);

                byte[] bytes = await client.Download(task.ModelUrl);
                var info = GlbInfo.Read(bytes);
                if (info.Error != null) throw new TripoException("the download is not a usable GLB: " + info.Error, false);

                string tmp = c.Glb(root) + ".tmp";
                await File.WriteAllBytesAsync(tmp, bytes);
                File.Move(tmp, c.Glb(root), overwrite: true);
                meta["status"] = "success";
                meta["credits"] = task.Credits;
                meta["bytes"] = bytes.Length;
                WriteMeta(c.Meta(root), meta);

                credits += task.Credits;
                made++;
                log.WriteLine($"{bytes.Length / 1024} KB, {info.Meshes} mesh(es), {info.Images} image(s), {task.Credits:0.##} credits");
                foreach (string w in info.Warnings) log.WriteLine("    ! " + w);
            }
            catch (TripoException e)
            {
                log.WriteLine("FAILED: " + e.Message);
                failed++;
                if (e.Fatal) return new Outcome(made, kept, failed, credits, true);
            }
            await Task.Delay(o.DelayMs);
        }
        log.WriteLine($"made {made}, kept {kept}, failed {failed}, {credits:0.##} credits");
        return new Outcome(made, kept, failed, credits, false);
    }

    /// <summary>
    /// Copies finished sculpts to assets/models (tracked by git) or, untracked, to overrides/models, named for the species.
    /// Never over a file already there.
    /// </summary>
    public static int Install(string root, List<Concept> chosen, string? asName, bool force, TextWriter log, bool untracked = false)
    {
        string dir = Path.Combine(root, untracked ? "overrides" : "assets", "models");
        string shown = untracked ? "overrides/models" : "assets/models";
        int copied = 0, skipped = 0;
        foreach (var c in chosen)
        {
            if (!File.Exists(c.Glb(root))) { log.WriteLine($"{c.Slug}: no sculpt yet"); continue; }
            string target = Path.Combine(dir, (asName ?? c.Species) + ".glb");
            if (File.Exists(target) && !force) { log.WriteLine($"{c.Slug}: {Path.GetFileName(target)} is there already (--force replaces it)"); skipped++; continue; }
            Directory.CreateDirectory(dir);
            File.Copy(c.Glb(root), target, overwrite: true);
            log.WriteLine($"{c.Slug} -> {shown}/{Path.GetFileName(target)}");
            copied++;
        }
        log.WriteLine($"installed {copied}, left {skipped}");
        return copied;
    }

    // The upload token is of no use afterwards and the record is a note of what was asked, not of a secret.
    static JsonObject RequestWithoutToken(JsonObject request)
    {
        var copy = (JsonObject)request.DeepClone();
        copy.Remove("input");
        return copy;
    }

    static JsonObject ReadMeta(string path)
    {
        try { return File.Exists(path) ? (JsonNode.Parse(File.ReadAllText(path)) as JsonObject ?? new JsonObject()) : new JsonObject(); }
        catch (JsonException) { return new JsonObject(); }
    }

    static void WriteMeta(string path, JsonObject meta) =>
        File.WriteAllText(path, meta.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
}
