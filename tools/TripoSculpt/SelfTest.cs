using System.Net;
using System.Text;

/// <summary>
/// The whole pipeline against a fake Tripo on localhost: upload, task, polling, download, the note, going on with a
/// task, install, and a refusal for lack of credits. It spends nothing and needs no key.
/// </summary>
static class SelfTest
{
    static int failures;

    static void Check(bool ok, string what)
    {
        Console.WriteLine($"  {(ok ? "ok  " : "FAIL")} {what}");
        if (!ok) failures++;
    }

    public static async Task<int> Run()
    {
        string root = Path.Combine(Path.GetTempPath(), "triposculpt-selftest-" + Guid.NewGuid().ToString("N")[..8]);
        string art = Path.Combine(root, "docs", "art", "reference", "gemini", "test");
        Directory.CreateDirectory(art);
        File.WriteAllBytes(Path.Combine(art, "001-Fakemon.jpg"), new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 });
        File.WriteAllBytes(Path.Combine(art, "002-Nomoney.jpg"), new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 });

        var fake = new FakeTripo();
        var client = new TripoClient("tsk_test", fake.Base, retryMs: 10);
        var o = Options.Default with { PollMs = 10, DelayMs = 0 };
        var concepts = Concept.ReadAll(root);
        var fakemon = concepts.Where(c => c.Slug == "001-Fakemon").ToList();
        var quiet = new StringWriter();

        try
        {
            Console.WriteLine("self-test of TripoSculpt against a fake server:");
            Check(concepts.Count == 2 && fakemon[0].Species == "Fakemon", "the pictures are found and named for their species");

            var first = await Sculptor.Generate(root, client, fakemon, o, quiet);
            Check(first.Made == 1 && first.Failed == 0, "a picture becomes a sculpt");
            Check(File.Exists(fakemon[0].Glb(root)) && GlbInfo.Read(File.ReadAllBytes(fakemon[0].Glb(root))).Error == null, "the file is a binary glTF that reads");
            Check(Math.Abs(first.Credits - 12.5) < 1e-9, "the credits spent are told");
            Check(fake.Uploads == 1 && fake.Creates == 1 && fake.LastAuth == "Bearer tsk_test", "one upload, one task, with the key");
            Check(fake.Request is { } r && r.Contains("\"model\":\"v3.1-20260211\"") && r.Contains("\"pbr\":false") && r.Contains("\"input\":\"file_fake1\""), "the request names the model, no PBR and the uploaded file");
            Check(!fake.DownloadSawAuth, "the key is not sent to the download link");
            Check(File.ReadAllText(fakemon[0].Meta(root)).Contains("\"taskId\"") && !File.ReadAllText(fakemon[0].Meta(root)).Contains("file_fake1"), "the note keeps the task and not the upload token");

            var again = await Sculptor.Generate(root, client, fakemon, o, quiet);
            Check(again.Kept == 1 && again.Made == 0 && fake.Creates == 1, "a sculpt that is there is kept and costs nothing");

            File.Delete(fakemon[0].Glb(root));
            var resumed = await Sculptor.Generate(root, client, fakemon, o, quiet);
            Check(resumed.Made == 1 && fake.Creates == 1 && fake.Uploads == 1, "a sculpt lost after its task was made goes on with that task");

            Sculptor.Install(root, fakemon, null, false, quiet);
            string installed = Path.Combine(root, "assets", "models", "Fakemon.glb");
            Check(File.Exists(installed), "install puts it in assets/models under the species' name");
            File.WriteAllText(installed, "mine");
            Sculptor.Install(root, fakemon, null, false, quiet);
            Check(File.ReadAllText(installed) == "mine", "install never replaces a file already there");
            Sculptor.Install(root, fakemon, null, false, quiet, untracked: true);
            Check(File.Exists(Path.Combine(root, "overrides", "models", "Fakemon.glb")), "--private puts it in overrides/models instead");

            fake.Broke = true;
            var broke = concepts.Where(c => c.Slug == "002-Nomoney").ToList();
            var refused = await Sculptor.Generate(root, client, broke, o, quiet);
            Check(refused.Aborted && refused.Failed == 1 && !File.Exists(broke[0].Glb(root)), "no credits left stops the run");

            Check(GlbInfo.Read(new byte[] { 1, 2, 3 }).Error != null, "a file that is no GLB is refused");
            var packed = GlbInfo.Read(FakeTripo.GlbWith("\"extensionsRequired\":[\"EXT_meshopt_compression\"]"));
            Check(packed.Error == null && packed.Warnings.Count == 1, "a compressed GLB is read and warned about");
        }
        finally
        {
            fake.Stop();
            try { Directory.Delete(root, true); } catch (IOException) { }
        }

        Console.WriteLine(failures == 0 ? "all passed" : $"{failures} failed");
        return failures == 0 ? 0 : 1;
    }

    sealed class FakeTripo
    {
        readonly HttpListener listener = new();
        public string Base { get; }
        public int Uploads, Creates;
        public string? LastAuth, Request;
        public bool DownloadSawAuth, Broke;
        int polls;

        public FakeTripo()
        {
            int port = new Random(7).Next(20000, 40000);
            while (true)
            {
                try
                {
                    listener.Prefixes.Clear();
                    listener.Prefixes.Add($"http://127.0.0.1:{port}/");
                    listener.Start();
                    break;
                }
                catch (HttpListenerException) { port++; }
            }
            Base = $"http://127.0.0.1:{port}/v3";
            _ = Task.Run(Serve);
        }

        public void Stop() => listener.Close();

        public static byte[] GlbWith(string extra)
        {
            byte[] json = Encoding.UTF8.GetBytes("{\"asset\":{\"version\":\"2.0\"}," + extra + "}");
            int padded = (json.Length + 3) & ~3;
            var file = new byte[20 + padded];
            Array.Copy(GlbInfo.Minimal(), file, 20);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(8), (uint)file.Length);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(12), (uint)padded);
            json.CopyTo(file, 20);
            for (int i = 20 + json.Length; i < file.Length; i++) file[i] = (byte)' ';
            return file;
        }

        async Task Serve()
        {
            while (listener.IsListening)
            {
                HttpListenerContext ctx;
                try { ctx = await listener.GetContextAsync(); }
                catch (Exception) { return; }

                string path = ctx.Request.Url!.AbsolutePath;
                string method = ctx.Request.HttpMethod;
                string? auth = ctx.Request.Headers["Authorization"];
                string body = "";
                if (ctx.Request.HasEntityBody)
                    using (var rd = new StreamReader(ctx.Request.InputStream, Encoding.UTF8)) body = await rd.ReadToEndAsync();

                int status = 200;
                byte[] reply;
                string type = "application/json";

                if (path == "/model.glb")
                {
                    DownloadSawAuth = auth != null;
                    reply = GlbInfo.Minimal();
                    type = "model/gltf-binary";
                }
                else
                {
                    LastAuth = auth;
                    string json;
                    if (method == "POST" && path == "/v3/files") { Uploads++; json = "{\"code\":0,\"data\":{\"file_token\":\"file_fake1\"}}"; }
                    else if (method == "POST" && path == "/v3/generation/image-to-model")
                    {
                        if (Broke) { status = 403; json = "{\"code\":2010,\"status\":\"error\",\"message\":\"You don't have enough credit to create this task\"}"; }
                        else { Creates++; Request = body; json = "{\"code\":0,\"data\":{\"task_id\":\"task_fake0001\"}}"; }
                    }
                    else if (method == "GET" && path == "/v3/tasks/task_fake0001")
                    {
                        json = ++polls % 2 == 1
                            ? "{\"code\":0,\"data\":{\"task_id\":\"task_fake0001\",\"status\":\"running\",\"progress\":40}}"
                            : $"{{\"code\":0,\"data\":{{\"task_id\":\"task_fake0001\",\"status\":\"success\",\"progress\":100,\"output\":{{\"model_url\":\"http://127.0.0.1:{ctx.Request.Url.Port}/model.glb\"}},\"credits_consumed\":12.5}}}}";
                    }
                    else { status = 404; json = "{\"code\":1001,\"status\":\"error\",\"message\":\"no such thing\"}"; }
                    reply = Encoding.UTF8.GetBytes(json);
                }

                ctx.Response.StatusCode = status;
                ctx.Response.ContentType = type;
                ctx.Response.ContentLength64 = reply.Length;
                await ctx.Response.OutputStream.WriteAsync(reply);
                ctx.Response.Close();
            }
        }
    }
}
