using System.Text.Json;
using System.Text.Json.Nodes;

sealed record RigOptions(string? RigType, string? RigModel, List<string>? Animations, bool Force, int Limit,
                         int DelayMs, TimeSpan Timeout, int PollMs, bool Reanimate = false, bool Refetch = false)
{
    public static RigOptions Default => new(null, null, null, false, int.MaxValue, 1000, TimeSpan.FromMinutes(10), 3000);
}

/// <summary>
/// Rigging and animating a sculpt, in Tripo's three steps: check what kind of skeleton the body takes, rig it, retarget
/// preset animations onto the rig. The clips are named so that the game's own rules (docs/model-files.md, "Animations")
/// find their roles by the words in the names, and the result is one file, <c>&lt;slug&gt;.animated.glb</c>, beside the static one.
/// Each step is noted in the sculpt's .json as soon as its task is made, so a run that stops goes on without paying again.
/// </summary>
static class Rigger
{
    // A preset, and the clip name the game will read the role from. A body with no humanoid presets (Tripo has one walk
    // for each of the others) gets that single clip as its idle: the game plays a lone clip as its idle.
    static readonly Dictionary<string, string> ClipNames = new()
    {
        ["preset:idle"] = "idle",
        ["preset:hurt"] = "hurt",
        ["preset:fall"] = "faint_fall",
        ["preset:slash"] = "strike_slash",
        ["preset:shoot"] = "special_shoot",
        ["preset:jump"] = "entry_jump",
        ["preset:quadruped:walk"] = "idle_walk",
        ["preset:hexapod:walk"] = "idle_walk",
        ["preset:octopod:walk"] = "idle_walk",
        ["preset:serpentine:march"] = "idle_march",
        ["preset:aquatic:march"] = "idle_march",
    };

    // The humanoid presets cover the roles the game plays; the others have only their one walk (the docs of the V3 retarget).
    public static List<string> DefaultPresets(string rigType) => rigType switch
    {
        "quadruped" => new() { "preset:quadruped:walk" },
        "hexapod" => new() { "preset:hexapod:walk" },
        "octopod" => new() { "preset:octopod:walk" },
        "serpentine" => new() { "preset:serpentine:march" },
        "aquatic" => new() { "preset:aquatic:march" },
        "others" => new(),
        _ => new() { "preset:idle", "preset:hurt", "preset:fall", "preset:slash", "preset:shoot" },
    };

    public static string ClipName(string preset) => ClipNames.TryGetValue(preset, out var n) ? n : preset[(preset.LastIndexOf(':') + 1)..];

    // Tripo's advice: v3.0 for humanoids, v2.5 for the other kinds of body.
    public static string ModelFor(string rigType) => rigType == "biped" ? "v3.0-20260909" : "v2.5-20260210";

    public static async Task<Outcome> Run(string root, TripoClient client, List<Concept> chosen, RigOptions o, TextWriter log)
    {
        int made = 0, kept = 0, failed = 0;
        double credits = 0;
        foreach (var c in chosen)
        {
            if (made >= o.Limit) break;
            if (File.Exists(c.Animated(root)) && !o.Force && !o.Reanimate && !o.Refetch) { kept++; continue; }
            if (!File.Exists(c.Glb(root))) { log.WriteLine($"{c.Set}/{c.Slug}: no sculpt to rig yet (generate first)"); continue; }

            log.Write($"{c.Set}/{c.Slug} ... ");
            double spent = 0;
            try
            {
                var meta = Sculptor.ReadMeta(c.Meta(root));
                string? sculptTask = meta["taskId"]?.GetValue<string>() ?? meta["sculptTaskId"]?.GetValue<string>();
                if (sculptTask == null) throw new TripoException("the note holds no task of the sculpt (it was not made by this tool)", false);
                var rig = o.Force || meta["rig"] is not JsonObject ? new JsonObject() : (JsonObject)meta["rig"]!;
                meta["rig"] = rig;
                // --reanimate keeps the rig that was paid for and retargets other animations onto it
                if (o.Reanimate && !o.Force) { rig.Remove("retargetTaskId"); rig.Remove("animations"); }

                // 1. what kind of body
                string? type = o.RigType ?? rig["type"]?.GetValue<string>();
                if (type == null)
                {
                    var check = await client.PostJson("rig check", "/animations/rig-check", new JsonObject { ["input"] = sculptTask });
                    JsonElement answer = check;
                    if (!HasProperty(check, "rig_type") && Str(check, "task_id") is { } checkId)
                    {
                        var t = await client.Wait(checkId, o.Timeout, o.PollMs);
                        spent += t.Credits;
                        if (t.Failed) throw new TripoException($"the rig check {t.Status}", false);
                        answer = t.Output;
                    }
                    if (answer.ValueKind == JsonValueKind.Object && answer.TryGetProperty("riggable", out var r) && r.ValueKind == JsonValueKind.False)
                        throw new TripoException("Tripo says this body cannot be rigged", false);
                    type = Str(answer, "rig_type") ?? throw new TripoException("the rig check names no rig type (pass --rig-type)", false);
                }
                rig["type"] = type;
                log.Write($"{type} ");

                // 2. the rig
                string? rigTask = rig["taskId"]?.GetValue<string>();
                bool rigIsNew = rigTask == null;
                if (rigTask == null)
                {
                    rigTask = await client.CreateTask("/animations/rig", new JsonObject
                    {
                        ["input"] = sculptTask,
                        ["model"] = o.RigModel ?? ModelFor(type),
                        ["rig_type"] = type,
                        ["out_format"] = "glb",
                    });
                    rig["taskId"] = rigTask;
                    Sculptor.WriteMeta(c.Meta(root), meta);
                }
                TripoTask rigged;
                try { rigged = await Wait(client, rigTask, o, log, "rig"); }
                catch (TripoException)
                {
                    // A failed rig is not gone back to: the next run makes a new one
                    rig.Remove("taskId");
                    Sculptor.WriteMeta(c.Meta(root), meta);
                    throw;
                }
                if (rigIsNew) spent += rigged.Credits;   // a rig that was paid for in an earlier run is not charged again

                // 3. the animations
                var presets = o.Animations ?? DefaultPresets(type);
                if (presets.Count == 0) throw new TripoException($"Tripo has no preset animations for a {type} body: name some with --animations", false);
                if (presets.Count > 5) throw new TripoException("Tripo takes at most five animations in one task", false);
                string? animTask = rig["retargetTaskId"]?.GetValue<string>();
                bool animIsNew = animTask == null;
                if (animTask == null)
                {
                    var body = new JsonObject { ["input"] = rigTask, ["out_format"] = "glb", ["animate_in_place"] = true };
                    if (presets.Count == 1) body["animation"] = presets[0];
                    else body["animations"] = new JsonArray(presets.Select(p => (JsonNode)p).ToArray());
                    animTask = await client.CreateTask("/animations/retarget", body);
                    rig["retargetTaskId"] = animTask;
                    rig["animations"] = new JsonArray(presets.Select(p => (JsonNode)p).ToArray());
                    Sculptor.WriteMeta(c.Meta(root), meta);
                }
                TripoTask animated;
                try { animated = await Wait(client, animTask, o, log, "animate"); }
                catch (TripoException)
                {
                    // A failed retarget is not gone back to: the next run asks for its animations afresh on the same rig
                    rig.Remove("retargetTaskId");
                    rig.Remove("animations");
                    Sculptor.WriteMeta(c.Meta(root), meta);
                    throw;
                }
                if (animIsNew) spent += animated.Credits;
                if (animated.ModelUrls.Count == 0) throw new TripoException("the animation task names no model", false);

                // 4. one file
                var files = new List<byte[]>();
                foreach (string url in animated.ModelUrls)
                {
                    var bytes = await client.Download(url);
                    if (GlbInfo.Read(bytes).Error is { } bad) throw new TripoException("a download is not a usable GLB: " + bad, false);
                    files.Add(bytes);
                }
                var names = (rig["animations"] as JsonArray)?.Select(n => n!.GetValue<string>()).ToList() ?? presets;
                var clipNames = names.Select(ClipName).ToList();
                while (clipNames.Count < files.Count) clipNames.Add("clip" + clipNames.Count);
                var notes = new List<string>();
                // Tripo may answer with one file holding every clip, each named for its preset (alphabetical): name them by that
                byte[] merged = files.Count == 1
                    ? GlbMerge.RenameClips(files[0], (own, i) => names.Count == 1 ? clipNames[0] : names.FirstOrDefault(p => p[(p.LastIndexOf(':') + 1)..] == own) is { } preset ? ClipName(preset) : own.Length > 0 ? own : clipNames[Math.Min(i, clipNames.Count - 1)])
                    : GlbMerge.Merge(files, clipNames, notes);
                var info = GlbInfo.Read(merged);
                if (info.Error != null) throw new TripoException("the merged file is not a usable GLB: " + info.Error, false);

                string tmp = c.Animated(root) + ".tmp";
                await File.WriteAllBytesAsync(tmp, merged);
                File.Move(tmp, c.Animated(root), overwrite: true);
                rig["status"] = "success";
                rig["credits"] = spent;
                rig["clips"] = new JsonArray(GlbMerge.AnimationNames(merged).Select(n => (JsonNode)n).ToArray());
                Sculptor.WriteMeta(c.Meta(root), meta);

                credits += spent;
                made++;
                log.WriteLine($"{merged.Length / 1024} KB, {info.Skins} skin(s), clips {string.Join(", ", GlbMerge.AnimationNames(merged))}, {spent:0.##} credits");
                foreach (string n in notes.Concat(info.Warnings)) log.WriteLine("    ! " + n);
            }
            catch (TripoException e)
            {
                log.WriteLine("FAILED: " + e.Message);
                failed++;
                credits += spent;
                if (e.Fatal) return new Outcome(made, kept, failed, credits, true);
            }
            await Task.Delay(o.DelayMs);
        }
        log.WriteLine($"rigged {made}, kept {kept}, failed {failed}, {credits:0.##} credits");
        return new Outcome(made, kept, failed, credits, false);
    }

    static async Task<TripoTask> Wait(TripoClient client, string id, RigOptions o, TextWriter log, string what)
    {
        log.Write($"{what} {id[..Math.Min(8, id.Length)]} ");
        int lastShown = -1;
        var task = await client.Wait(id, o.Timeout, o.PollMs, t =>
        {
            if (t.Progress / 25 != lastShown) { lastShown = t.Progress / 25; log.Write($"{t.Progress}% "); }
        });
        if (task.Failed) throw new TripoException($"the {what} task {task.Status}{(task.Message is null ? "" : ": " + task.Message)}", false);
        return task;
    }

    static bool HasProperty(JsonElement e, string name) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out _);

    static string? Str(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}
