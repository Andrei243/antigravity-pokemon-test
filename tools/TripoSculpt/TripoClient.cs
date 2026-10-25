using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;

// Tripo's V3 REST API (https://developers.tripo3d.ai): the V2 endpoints are retired on 2026-11-01.
//   POST /files                       multipart "file"  ->  data.file_token
//   POST /generation/image-to-model   { input, model, ... }  ->  data.task_id
//   GET  /tasks/{id}                  ->  data.status, data.progress, data.output.model_url
// Every reply is { code, data } on success and { code, status: "error", message } otherwise.

/// <summary>A call that failed. <see cref="Fatal"/> means no other call of the run can succeed either (key, credits).</summary>
sealed class TripoException(string message, bool fatal) : Exception(message)
{
    public bool Fatal { get; } = fatal;
}

sealed record TripoTask(string Id, string Status, int Progress, string? ModelUrl, double Credits, string? Message,
                        List<string> ModelUrls, JsonElement Output)
{
    public bool Succeeded => Status == "success";
    public bool Failed => Status is "failed" or "cancelled" or "banned" or "expired";
}

sealed class TripoClient
{
    public const string DefaultBase = "https://openapi.tripo3d.ai/v3";
    const int InsufficientCredits = 2010;

    readonly HttpClient http;
    readonly string key;
    readonly string baseUrl;
    readonly int retryMs;

    public TripoClient(string key, string? baseUrl = null, int retryMs = 3000)
    {
        this.key = key;
        this.baseUrl = (baseUrl ?? DefaultBase).TrimEnd('/');
        this.retryMs = retryMs;
        http = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
    }

    public async Task<string> Upload(string path)
    {
        byte[] bytes = await File.ReadAllBytesAsync(path);
        string type = Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg",
        };
        var data = await Send("upload", () =>
        {
            var form = new MultipartFormDataContent();
            var file = new ByteArrayContent(bytes);
            file.Headers.ContentType = new MediaTypeHeaderValue(type);
            form.Add(file, "file", Path.GetFileName(path));
            return new HttpRequestMessage(HttpMethod.Post, baseUrl + "/files") { Content = form };
        });
        return Text(data, "file_token") ?? throw new TripoException("upload: the reply holds no file_token", false);
    }

    public Task<string> CreateImageToModel(JsonObject body) => CreateTask("/generation/image-to-model", body);

    /// <summary>The first-step rig check, rigging and retargeting are tasks like the generation: a path, a body, a task id.</summary>
    public async Task<string> CreateTask(string path, JsonObject body)
    {
        var data = await PostJson("create task " + path, path, body);
        return Text(data, "task_id") ?? throw new TripoException($"create task {path}: the reply holds no task_id", false);
    }

    /// <summary>Whatever the reply to a POST holds, for a call that may answer at once (rig-check) or with a task.</summary>
    public Task<JsonElement> PostJson(string what, string path, JsonObject body)
    {
        string json = body.ToJsonString();
        return Send(what, () => new HttpRequestMessage(HttpMethod.Post, baseUrl + path)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
        });
    }

    public async Task<TripoTask> GetTask(string id)
    {
        var d = await Send("task " + id, () => new HttpRequestMessage(HttpMethod.Get, baseUrl + "/tasks/" + id));
        string? model = null;
        var urls = new List<string>();
        JsonElement output = default;
        if (d.TryGetProperty("output", out output) && output.ValueKind == JsonValueKind.Object)
        {
            model = Text(output, "model_url") ?? Text(output, "model");
            // A retarget of several animations answers with model_urls: a list, or an object of name -> address.
            if (output.TryGetProperty("model_urls", out var many))
            {
                if (many.ValueKind == JsonValueKind.Array)
                    urls.AddRange(many.EnumerateArray().Where(u => u.ValueKind == JsonValueKind.String).Select(u => u.GetString()!));
                else if (many.ValueKind == JsonValueKind.Object)
                    urls.AddRange(many.EnumerateObject().Where(u => u.Value.ValueKind == JsonValueKind.String).Select(u => u.Value.GetString()!));
            }
            if (urls.Count == 0 && model != null) urls.Add(model);
        }
        double credits = 0;
        foreach (string name in new[] { "credits_consumed", "consumed_credit" })
            if (d.TryGetProperty(name, out var c) && c.ValueKind == JsonValueKind.Number) { credits = c.GetDouble(); break; }
        int progress = d.TryGetProperty("progress", out var p) && p.ValueKind == JsonValueKind.Number ? p.GetInt32() : 0;
        return new TripoTask(id, Text(d, "status") ?? "unknown", progress, model, credits, Reason(d), urls, output.ValueKind == JsonValueKind.Object ? output.Clone() : default);
    }

    /// <summary>Polls until the task has ended one way or the other, or the time is up.</summary>
    public async Task<TripoTask> Wait(string id, TimeSpan limit, int pollMs, Action<TripoTask>? onProgress = null)
    {
        var until = DateTime.UtcNow + limit;
        while (true)
        {
            var task = await GetTask(id);
            onProgress?.Invoke(task);
            if (task.Succeeded || task.Failed) return task;
            if (DateTime.UtcNow >= until) throw new TripoException($"task {id} was still {task.Status} after {limit.TotalMinutes:0} minutes (it is kept: run the same command again to go on waiting)", false);
            await Task.Delay(pollMs);
        }
    }

    /// <summary>The model's address is a signed link on Tripo's CDN: the key is not sent there.</summary>
    public async Task<byte[]> Download(string url)
    {
        for (int attempt = 1; attempt <= 4; attempt++)
        {
            try
            {
                using var resp = await http.GetAsync(url);
                if (resp.IsSuccessStatusCode) return await resp.Content.ReadAsByteArrayAsync();
                if ((int)resp.StatusCode is not (429 or >= 500) || attempt == 4)
                    throw new TripoException($"download: HTTP {(int)resp.StatusCode}", false);
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
            {
                if (attempt == 4) throw new TripoException("download: " + e.Message, false);
            }
            await Task.Delay(retryMs * attempt);
        }
        throw new TripoException("download: gave up", false);
    }

    async Task<JsonElement> Send(string what, Func<HttpRequestMessage> make)
    {
        for (int attempt = 1; attempt <= 4; attempt++)
        {
            string text;
            int status;
            try
            {
                using var req = make();
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
                using var resp = await http.SendAsync(req);
                status = (int)resp.StatusCode;
                text = await resp.Content.ReadAsStringAsync();
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
            {
                if (attempt == 4) throw new TripoException($"{what}: {e.Message}", false);
                await Task.Delay(retryMs * attempt);
                continue;
            }

            JsonElement root = default;
            bool parsed = false;
            try { root = JsonDocument.Parse(text).RootElement; parsed = root.ValueKind == JsonValueKind.Object; }
            catch (JsonException) { }

            int code = parsed && root.TryGetProperty("code", out var c) && c.ValueKind == JsonValueKind.Number ? c.GetInt32() : -1;
            if (status is >= 200 and < 300 && code == 0 && root.TryGetProperty("data", out var data))
                return data.Clone();

            string message = parsed ? Text(root, "message") ?? "" : "";
            if (message.Length == 0) message = text.Length > 300 ? text[..300] + "..." : text;
            bool transient = status == 429 || status >= 500;
            if (!transient || attempt == 4)
            {
                bool fatal = status is 401 or 402 or 403 || code == InsufficientCredits;
                throw new TripoException($"{what}: HTTP {status}, code {code}: {message}", fatal);
            }
            await Task.Delay(status == 429 ? retryMs * 5 * attempt : retryMs * attempt);
        }
        throw new TripoException($"{what}: gave up", false);
    }

    // Tripo's own reason for a failed task: its error_message and error_code, or the older names
    static string? Reason(JsonElement d)
    {
        string? text = Text(d, "error_message") ?? Text(d, "error_msg") ?? Text(d, "message");
        return d.ValueKind == JsonValueKind.Object && d.TryGetProperty("error_code", out var code) && code.ValueKind == JsonValueKind.Number && code.GetInt32() != 0
            ? (text == null ? "" : text + " ") + "(code " + code.GetInt32() + ")" : text;
    }

    static string? Text(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}
