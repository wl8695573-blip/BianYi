using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using GuGuGaGaTranslator.App;
using GuGuGaGaTranslator.Core.Config;
using GuGuGaGaTranslator.Core.Translation;
using GuGuGaGaTranslator.Core.Updates;
using GuGuGaGaTranslator.Installation;

internal static class BianYiChecks
{
    public static async Task RunAsync(string scratch)
    {
        var incoming = GameProfiles.Default().Single(profile => profile.Id == "limbus-company");
        Program.Check(incoming.Terms.Count > 0 && incoming.Terms.All(term => !LegacyTermRetirement.IsUnchangedLegacy(term)
            && (!string.IsNullOrWhiteSpace(term.Note) || !string.IsNullOrWhiteSpace(term.SourceUrl))), "BianYi: baseline documents provenance and excludes retired collection");
        Program.Check(GameProfiles.ForDirection(incoming, "ja", "zh-Hans").Any(term => term.Source == "ダンテ" && term.Target == "但丁"),
            "BianYi: Japanese Dante remains available");
        var legacy = new GameTerm { Language = "en", Source = "Dante", Target = "但丁", Forbidden = ["丹特", "丹堤"], ReviewStatus = "历史收录" };
        Program.Check(LegacyTermRetirement.IsUnchangedLegacy(legacy), "BianYi: recognizes legacy fingerprint without old collection");
        var local = GameProfileArchive.Clone(incoming);
        local.Terms = [legacy, new() { Language = "en", Source = "Custom", Target = "自定义" }];
        var migrated = LegacyTermRetirement.PrepareLocal(local, incoming);
        Program.Check(migrated.Terms.Single(term => term.Source == "Dante").Forbidden.Count == 0
            && migrated.Terms.Single(term => term.Source == "Dante").ReviewStatus == "独立整理", "BianYi: replaces unchanged old term");
        Program.Check(migrated.Terms.Any(term => term.Source == "Custom"), "BianYi: migration preserves user addition");
        Program.Check(legacy.Forbidden.Count == 2 && local.Terms.Count == 2, "BianYi: migration leaves original objects intact");
        var withdrawn = GameProfileArchive.Clone(incoming);
        withdrawn.Terms.Clear();
        var retired = LegacyTermRetirement.PrepareLocal(local, withdrawn);
        Program.Check(retired.Terms.All(term => term.Source != "Dante") && retired.Terms.Any(term => term.Source == "Custom"),
            "BianYi: removed public terms retire while private additions survive");
        legacy.Target = "个人译名";
        Program.Check(!LegacyTermRetirement.IsUnchangedLegacy(legacy), "BianYi: preserves changed translation");
        legacy.Target = "但丁"; legacy.Enabled = false;
        Program.Check(!LegacyTermRetirement.IsUnchangedLegacy(legacy), "BianYi: preserves disabled term");
        legacy.Enabled = true; legacy.Note = "个人说明";
        Program.Check(!LegacyTermRetirement.IsUnchangedLegacy(legacy), "BianYi: preserves user note");
        legacy.Note = null; legacy.ReviewStatus = "本人核对";
        Program.Check(!LegacyTermRetirement.IsUnchangedLegacy(legacy), "BianYi: preserves user review");
        legacy.ReviewStatus = "历史收录";
        foreach (var baseline in new[] { false, true })
        {
            var directory = Path.Combine(scratch, "bianyi-legacy-" + baseline);
            var config = new AppConfig();
            local.ProfileVersion = "2026.10.8.1";
            local.SourceUrl = "https://www.zeroasso.top/archive/main/";
            local.Author = "LCTA 社区维护";
            config.Translation.GameProfiles = [GameProfileArchive.Clone(local)];
            config.Updates.TermBaseline = baseline ? GameProfileArchive.Clone(local) : null;
            if (config.Updates.TermBaseline is { } publicBaseline)
                publicBaseline.Terms.RemoveAll(term => term.Source == "Custom");
            config.Translation.PersonalTerms = [new() { From = "ja", To = "zh-Hans", Source = "個人", Target = "个人" }];
            new ConfigStore(directory).Save(config);
            await using var session = new AppSession(directory);
            session.LoadConfig();
            var result = session.FindProfile("limbus-company")!;
            Program.Check(result.Terms.Any(term => term.Source == "Dante" && term.ReviewStatus == "独立整理")
                && result.SourceUrl == incoming.SourceUrl && result.Author == incoming.Author,
                "BianYi: legacy upgrade updates terms and provenance, baseline=" + baseline);
            Program.Check(result.Terms.Any(term => term.Source == "Custom") && session.Config.Translation.PersonalTerms.Count == 1,
                "BianYi: legacy upgrade preserves personal data, baseline=" + baseline);
        }
        Program.Check(InstallationManifest.DisplayName == "边译" && InstallationManifest.ExecutableName == "BianYi.exe",
            "BianYi: product display and executable names");
        foreach (var from in new[] { "zh-Hans", "ja", "en", "ko" })
        foreach (var to in new[] { "zh-Hans", "ja", "en", "ko" }.Where(language => language != from))
        {
            var prompt = OpenAiCompatibleTranslator.PreviewPrompts(new() { Text = "sample", From = from, To = to }, PromptStyle.Sakura);
            Program.Check(from == "ja" && to == "zh-Hans"
                ? prompt.User.Contains("从日文翻译成简体中文")
                : !prompt.User.Contains("从日文翻译成简体中文") && prompt.System.Contains("target language"),
                "BianYi: Sakura prompt respects direction " + from + "->" + to);
        }
        foreach (var executable in new[] { "GuGuGaGaTranslator.exe", "LCTA.exe", "BianYi.exe" })
        {
            var directory = Path.Combine(scratch, "identity-" + executable);
            Directory.CreateDirectory(directory);
            new InstallationManifest(InstallationManifest.ProductName, Guid.NewGuid().ToString(), [executable]).Save(directory);
            Program.Check(InstallationManifest.Read(directory).Files.Contains(executable), "BianYi: recognizes install manifest " + executable);
        }
        foreach (var reversed in new[] { false, true })
        {
            var names = new[] { "BianYi-Setup-1.7.0.exe", "LCTA-Setup-1.7.0.exe", "SHA256SUMS-1.7.0.txt" };
            if (reversed) Array.Reverse(names);
            using var http = new HttpClient(new ReleaseHandler(names));
            var offer = await new UpdateClient(http).CheckAppAsync("1.6.5", CancellationToken.None);
            Program.Check(offer?.InstallerName == "BianYi-Setup-1.7.0.exe", "BianYi: canonical installer preferred regardless of asset order");
        }
    }

    private sealed class ReleaseHandler(string[] names) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(new
            {
                tag_name = "v1.7.0", prerelease = false,
                assets = names.Select(name => new { name, browser_download_url = "https://github.com/" + UpdateClient.Repository + "/releases/download/v1.7.0/" + name })
            }), Encoding.UTF8, "application/json") });
    }
}
