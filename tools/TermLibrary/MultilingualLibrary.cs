using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GuGuGaGaTranslator.Core.Translation;

internal sealed class MultilingualLibrary
{
    public int SchemaVersion { get; set; }
    public string Version { get; set; } = "";
    public List<MultilingualEntry> Entries { get; set; } = [];
    private static readonly string[] Languages = ["ja", "ko", "en"];

    public static MultilingualLibrary Read(string path)
    {
        var library = JsonSerializer.Deserialize<MultilingualLibrary>(File.ReadAllText(path), new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        }) ?? throw new InvalidDataException("四语对照为空。");
        if (library.SchemaVersion != 1 || !System.Version.TryParse(library.Version, out _) || library.Entries.Count == 0)
            throw new InvalidDataException("四语对照的格式或版本无效。");
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var chinese = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in library.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Id) || !ids.Add(entry.Id) || string.IsNullOrWhiteSpace(entry.Category)
                || string.IsNullOrWhiteSpace(entry.Zh) || !chinese.Add(entry.Zh)
                || Languages.Any(language => string.IsNullOrWhiteSpace(entry.Text(language)))
                || string.IsNullOrWhiteSpace(entry.Note) || entry.Sources.Count == 0
                || entry.Sources.Any(source => !Uri.TryCreate(source, UriKind.Absolute, out var uri) || uri.Scheme != "https")
                || entry.Aliases.Any(pair => !Languages.Contains(pair.Key) || pair.Value.Any(string.IsNullOrWhiteSpace)))
                throw new InvalidDataException($"四语条目不完整、重复或含无效来源：{entry.Id}");
        }
        library.CreateTerms();
        return library;
    }

    public List<GameTerm> CreateTerms()
    {
        var result = new List<GameTerm>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in Entries)
        foreach (var language in Languages)
        {
            // 跨语种转换以中文为桥梁，最后一条决定反向译名，因此别名在前、规范写法在后。
            var aliases = entry.Aliases.GetValueOrDefault(language) ?? [];
            foreach (var text in aliases.Append(entry.Text(language)))
            {
                if (!seen.Add(language + ":" + text)) throw new InvalidDataException($"原文重复：{language}:{text}");
                result.Add(new GameTerm
                {
                    Language = language, Source = text, Target = entry.Zh,
                    Note = $"{entry.Category}；{entry.Note} 对照编号：{entry.Id}。参考：{string.Join("；", entry.Sources)}",
                    SourceUrl = entry.Sources[0], ReviewStatus = "独立整理", Enabled = true,
                });
            }
        }
        return result;
    }

    public string RenderTable()
    {
        static string Cell(string value) => value.Replace("|", "\\|").Replace("\r", "").Replace("\n", "<br>");
        var text = new StringBuilder();
        // 固定使用 LF，避免不同系统生成不同的表格。
        void Line(string value = "") => text.Append(value).Append('\n');
        Line("# 中日韩英术语对照");
        Line();
        Line($"版本：`{Version}`。共 **{Entries.Count} 个概念、{CreateTerms().Count} 条语言映射（含别名）**。");
        Line();
        Line("中文为本项目选用的译名；日、韩、英写法按下列公开参考逐条整理。称号与姓名分别建条目，不表示人物与称号的一对一绑定。本文由 [四语源数据](multilingual.json) 生成，修改方法见 [维护说明](README.md#维护者如何发布词库)。");
        foreach (var group in Entries.GroupBy(entry => entry.Category))
        {
            Line();
            Line($"## {group.Key}");
            Line();
            Line("| 中文 | 日文 | 韩文 | 英文 |");
            Line("|---|---|---|---|");
            foreach (var entry in group)
                Line($"| [{Cell(entry.Zh)}](#{entry.Id}) | {Cell(entry.Ja)} | {Cell(entry.Ko)} | {Cell(entry.En)} |");
        }
        Line();
        Line("## 别名、选词说明与参考");
        foreach (var entry in Entries)
        {
            Line();
            Line($"<a id=\"{entry.Id}\"></a>");
            Line();
            Line($"### {entry.Zh}");
            Line();
            Line(entry.Note);
            if (entry.Aliases.Count > 0)
            {
                Line();
                Line("别名：" + string.Join("；", entry.Aliases.Select(pair => $"{pair.Key}：{string.Join("、", pair.Value)}")) + "。别名用于识别，反向翻译优先采用表中的规范写法。");
            }
            Line();
            Line("参考：" + string.Join("、", entry.Sources.Select((source, index) => $"[来源 {index + 1}]({source})")) + "。");
        }
        return text.ToString();
    }
}

internal sealed class MultilingualEntry
{
    public string Id { get; set; } = "";
    public string Category { get; set; } = "";
    public string Zh { get; set; } = "";
    public string Ja { get; set; } = "";
    public string Ko { get; set; } = "";
    public string En { get; set; } = "";
    public Dictionary<string, List<string>> Aliases { get; set; } = [];
    public string Note { get; set; } = "";
    public List<string> Sources { get; set; } = [];
    public string Text(string language) => language switch { "ja" => Ja, "ko" => Ko, "en" => En, _ => throw new ArgumentException("不支持的语言。") };
}
