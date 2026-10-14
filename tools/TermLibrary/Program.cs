using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GuGuGaGaTranslator.Core.Translation;

var positional = args.Where(value => !value.StartsWith("--", StringComparison.Ordinal)).ToArray();
var directory = Path.GetFullPath(positional.Length > 0 ? positional[0] : "terminology");
Directory.CreateDirectory(directory);
const string name = "limbus-company.ggprofile.json";
var path = Path.Combine(directory, name);
var profile = File.Exists(path) && !args.Contains("--from-code")
    ? GameProfileArchive.Read(path) : GameProfiles.Default().First(profile => profile.Id == "limbus-company");
if (profile.Id != "limbus-company") throw new InvalidDataException("公共词库 ID 须为 limbus-company。");
var previousVersion = profile.ProfileVersion;
var multilingualPath = Path.Combine(directory, "multilingual.json");
var multilingual = File.Exists(multilingualPath) ? MultilingualLibrary.Read(multilingualPath) : null;
if (args.Contains("--from-multilingual"))
{
    if (args.Contains("--from-code")) throw new ArgumentException("不能同时从代码和四语对照导出。");
    if (multilingual is null) throw new FileNotFoundException("缺少四语对照源文件。", multilingualPath);
    profile.Terms = multilingual.CreateTerms();
}
if (args.Contains("--check"))
{
    using var existing = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(directory, "manifest.json")));
    var root = existing.RootElement;
    var checksum = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
    if (root.GetProperty("schemaVersion").GetInt32() != 1 || root.GetProperty("file").GetString() != name
        || root.GetProperty("version").GetString() != profile.ProfileVersion || root.GetProperty("sha256").GetString() != checksum
        || !Version.TryParse(profile.ProfileVersion, out _)) throw new InvalidDataException("公共词库版本或校验清单不匹配。");
    if (multilingual is not null)
    {
        var expected = GameProfileArchive.Clone(profile);
        expected.Terms = multilingual.CreateTerms();
        if (multilingual.Version != profile.ProfileVersion || GameProfileArchive.Serialize(expected) != GameProfileArchive.Serialize(profile)
            || !File.Exists(Path.Combine(directory, "MULTILINGUAL.md"))
            || File.ReadAllText(Path.Combine(directory, "MULTILINGUAL.md")) != multilingual.RenderTable())
            throw new InvalidDataException("四语对照、公共词库或对照表不一致，请使用 --from-multilingual 重新导出。");
    }
    Console.WriteLine($"PASS {profile.Terms.Count} terms, version {profile.ProfileVersion}, matching SHA256 manifest.");
    return;
}
var version = positional.Length > 1 ? positional[1]
    : args.Contains("--from-multilingual") ? multilingual!.Version : profile.ProfileVersion ?? "2026.10.8.1";
if (!Version.TryParse(version, out var requested)) throw new ArgumentException("词库版本须为数字版本号。");
if (Version.TryParse(previousVersion, out var current) && requested < current)
    throw new InvalidDataException("不能降低公共词库版本。");
if (multilingual is not null && version != multilingual.Version)
    throw new InvalidDataException("先更新 multilingual.json 的 version，再导出相同版本。");
profile.ProfileVersion = version;
var bytes = new UTF8Encoding(false).GetBytes(GameProfileArchive.Serialize(profile));
File.WriteAllBytes(path, bytes);
var manifest = new { schemaVersion = 1, version, file = name, sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant() };
File.WriteAllText(Path.Combine(directory, "manifest.json"), JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
if (args.Contains("--from-multilingual"))
    File.WriteAllText(Path.Combine(directory, "MULTILINGUAL.md"), multilingual!.RenderTable(), new UTF8Encoding(false));
Console.WriteLine($"Exported {profile.Terms.Count} terms, version {version}, SHA256 verified manifest generated.");
