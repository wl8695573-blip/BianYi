using System.Collections.Concurrent;
using System.IO;
using System.Windows;
using GuGuGaGaTranslator.App;
using GuGuGaGaTranslator.Core.Capture;
using GuGuGaGaTranslator.Core.Config;
using GuGuGaGaTranslator.Core.Ocr;
using GuGuGaGaTranslator.Core.Pipeline;
using GuGuGaGaTranslator.Core.Text;
using GuGuGaGaTranslator.Core.Translation;

internal static class DialogueFieldChecks
{
    public static async Task RunAsync(string scratch)
    {
        var rules = OcrCorrections.Defaults();
        Program.Check(OcrCorrections.Apply("グンテ", "ja", "limbus-company", "name", rules) == "ダンテ", "fields: Dante correction in Japanese name only");
        foreach (var field in new[] { "body", "title" })
            Program.Check(OcrCorrections.Apply("グンテ", "ja", "limbus-company", field, rules) == "グンテ", "fields: no Dante replacement in " + field);
        Program.Check(OcrCorrections.Apply("グンテ", "ja", "other", "name", rules) == "グンテ", "fields: Dante rule scoped to game");
        Program.Check(OcrCorrections.Apply("グンテル", "ja", "limbus-company", "name", rules) == "グンテル", "fields: Dante correction requires complete name");
        Program.Check(OcrCorrections.Apply("tiursefather", "en", "limbus-company", "title", rules) == "Nursefather", "fields: legacy speaker rule still covers title");
        var config = new AppConfig();
        config.Ocr.Corrections = rules.Where(r => r.Field != "name").ToList();
        config.NormalizeLanguages();
        config.NormalizeLanguages();
        Program.Check(config.Ocr.Corrections.Count(r => r.Wrong == "グンテ") == 1, "fields: correction migration adds rule once");
        config.Ocr.Corrections.RemoveAll(r => r.Wrong == "グンテ");
        config.NormalizeLanguages();
        Program.Check(!config.Ocr.Corrections.Any(r => r.Wrong == "グンテ"), "fields: deleted correction stays deleted");
        Program.Check(DialogueFields.Format("管理人", "但丁", "测试台词") == "称号：管理人\n姓名：但丁\n正文：测试台词", "fields: explicit three-line output");
        Program.Check(DialogueFields.Format("", "", "网页正文") == "网页正文", "fields: plain translation stays plain");
        Program.Check(DialogueFields.Format("管理人", "", "测试") == "称号：管理人\n姓名：\n正文：测试", "fields: missing name is not invented");

        var root = Path.Combine(scratch, "three-regions");
        await using (var session = new AppSession(root))
        {
            session.LoadConfig();
            var window = Window("LimbusCompany");
            session.SetRegion(window, new(200, 450, 500, 100));
            session.SetSpeakerRegion(window, new(0, 380, 200, 100));
            session.SetRoleRegion(window, new(0, 380, 160, 35), false);
            session.SetRoleRegion(window, new(10, 420, 180, 60), true);
            Program.Check(session.Config.Target.SpeakerRegion is null, "fields: separate selection supersedes legacy region");
            session.SaveConfig();
            session.SetTarget(Window("browser"));
            Program.Check(session.Config.Target.TitleRegion is null && session.Config.Target.NameRegion is null, "fields: role regions independent across windows");
            session.SetTarget(window);
            Program.Check(session.Config.Target.TitleRegion == new RegionRect(0, 380, 160, 35)
                && session.Config.Target.NameRegion == new RegionRect(10, 420, 180, 60), "fields: restores title and name independently");
            session.SaveConfig();
        }
        var loaded = new ConfigStore(root).Load().Target;
        Program.Check(loaded.Region == new RegionRect(200, 450, 500, 100) && loaded.TitleRegion == new RegionRect(0, 380, 160, 35)
            && loaded.NameRegion == new RegionRect(10, 420, 180, 60), "fields: all three regions persist across restart");
        Program.Check(loaded.TitleReferenceWidth == 800 && loaded.NameReferenceHeight == 600, "fields: each role saves reference size");

        var recognizer = new FieldRecognizer();
        var translator = new FieldTranslator();
        var results = new ConcurrentQueue<PipelineUpdate>();
        await using var pipeline = new TranslationPipeline(new()
        {
            RegionProvider = () => new(0, 0, 100, 80),
            TitleRegionProvider = () => new(0, 0, 20, 20),
            NameRegionProvider = () => new(0, 20, 20, 20),
            SpeakerRegionProvider = () => new(0, 0, 10, 10),
            Capture = r => new()
            {
                Width = r.Width,
                Height = r.Height,
                SourceRegion = r,
                CapturedAt = DateTimeOffset.Now,
                Bgra = Enumerable.Repeat((byte)80, r.Width * r.Height * 4).ToArray()
            },
            Recognizer = recognizer,
            Translator = translator,
            Languages = () => new("ja", "zh-Hans"),
            CorrectOcr = (text, field) => OcrCorrections.Apply(text, "ja", "limbus-company", field, rules),
            Profile = () => new([new("管理人", "管理人"), new("ダンテ", "但丁"), new("リョウシュウ", "良秀")]),
            Options = new() { OcrScale = 1, PollIntervalMs = 15, ForceRefreshMs = 0, TextSettleMs = 0, MaskOwnWindows = false, ScriptGuard = false }
        });
        pipeline.Updated += u => { if (u.Status == PipelineStatus.Translated) results.Enqueue(u); };
        pipeline.Start();
        async Task<PipelineUpdate> Next()
        {
            var end = Environment.TickCount64 + 4000;
            while (Environment.TickCount64 < end)
            { if (results.TryDequeue(out var result)) return result; await Task.Delay(15); }
            throw new TimeoutException("three-field pipeline");
        }
        var first = await Next();
        Program.Check(first.TitleSource == "管理人" && first.NameSource == "ダンテ" && first.SourceText == "Hello there.", "fields: OCR fields remain isolated");
        Program.Check(first.TitleTranslation == "管理人" && first.NameTranslation == "但丁" && first.Translation == "测试台词", "fields: exact name term after scoped correction");
        Program.Check(first.StructuredDialogue && translator.Calls == 1, "fields: role glossary requires no extra API calls");
        Program.Check(recognizer.BodyMasked && recognizer.RolePreserved, "fields: overlapping roles excluded only from body copy");
        pipeline.InvalidateTranslation();
        var cached = await Next();
        Program.Check(cached.FromCache && cached.NameTranslation == "但丁" && translator.Calls == 1, "fields: cached output retains all fields");
        recognizer.Name = "リョウシュウ";
        var changed = await Next();
        Program.Check(changed.NameTranslation == "良秀" && translator.Calls == 2, "fields: changed name with same body invalidates previous identity");
        pipeline.RetranslateCurrent();
        var fresh = await Next();
        Program.Check(!fresh.FromCache && fresh.NameTranslation == "良秀" && translator.Calls == 3, "fields: retranslate bypasses cache for three-field result");
        await pipeline.StopAsync();
    }

    private static WindowInfo Window(string process) => new()
    {
        Handle = 0,
        Title = process,
        ProcessName = process,
        ClassName = "test",
        ProcessId = 0,
        ClientRect = new(0, 0, 800, 600),
        FrameRect = new(0, 0, 800, 600),
        IsMinimized = false,
        IsOwned = false
    };

    private sealed class FieldRecognizer : ITextRecognizer
    {
        public string Id => "field-test";
        public string LanguageTag => "ja";
        public volatile string Name = "グンテ";
        public volatile bool BodyMasked, RolePreserved;
        public Task<OcrResult> RecognizeAsync(Frame frame, CancellationToken cancellationToken = default)
        {
            var body = frame.Width == 100;
            if (body)
                BodyMasked = frame.Bgra[0] == 0 && frame.Bgra[20 * frame.Stride] == 0 && frame.Bgra[40 * 4] == 80;
            else
                RolePreserved = frame.Bgra[0] == 80;
            var text = body ? "Hello there." : frame.SourceRegion.Y == 0 ? "管理人" : Name;
            return Task.FromResult(new OcrResult
            {
                Lines = [new(text, new(0, 0, 10, 8), [])],
                RecognizerId = Id,
                LanguageTag = LanguageTag,
                SourceWidth = frame.Width,
                SourceHeight = frame.Height,
                Duration = TimeSpan.Zero
            });
        }
    }
    private sealed class FieldTranslator : ITranslator
    {
        public string Id => "field-translator";
        public bool RequiresNetwork => false;
        public int Calls;
        public Task<string> TranslateAsync(TranslationRequest request, CancellationToken cancellationToken = default)
        { Interlocked.Increment(ref Calls); return Task.FromResult("测试台词"); }
    }
}
