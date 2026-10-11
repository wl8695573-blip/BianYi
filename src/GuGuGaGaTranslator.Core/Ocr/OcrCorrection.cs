using System.Text;
using System.Text.RegularExpressions;
using GuGuGaGaTranslator.Core.Translation;

namespace GuGuGaGaTranslator.Core.Ocr;

public sealed class OcrCorrection
{
    public string Language { get; set; } = "en";
    public string Profile { get; set; } = "limbus-company";
    public string Field { get; set; } = "speaker";
    public string Wrong { get; set; } = "";
    public string Correct { get; set; } = "";
}

public static class OcrCorrections
{
    public static List<OcrCorrection> Defaults() =>
    [
        new() { Wrong = "Ähe Index", Correct = "The Index" },
        new() { Wrong = "tiursefather_", Correct = "Nursefather" },
        new() { Wrong = "tiursefather", Correct = "Nursefather" },
        new() { Wrong = "Nursefathel", Correct = "Nursefather" }
    ];

    public static string Apply(string text, string language, string profile, string field, IEnumerable<OcrCorrection> rules)
    {
        text = text.Normalize(NormalizationForm.FormKC);
        foreach (var rule in rules.Where(r => r.Language.Equals(language, StringComparison.OrdinalIgnoreCase)
                     && r.Profile.Equals(profile, StringComparison.OrdinalIgnoreCase) && r.Field == field
                     && !string.IsNullOrWhiteSpace(r.Wrong) && !string.IsNullOrWhiteSpace(r.Correct)))
        {
            var pattern = Regex.Escape(rule.Wrong.Normalize(NormalizationForm.FormKC));
            // 拉丁词必须完整匹配，不能改动普通单词内部。
            if (rule.Wrong.Any(c => c is >= 'A' and <= 'Z' or >= 'a' and <= 'z'))
                pattern = @"(?<![\p{L}\p{N}])" + pattern + @"(?![\p{L}\p{N}])";
            text = Regex.Replace(text, pattern, _ => rule.Correct, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                TimeSpan.FromMilliseconds(100));
        }
        return text;
    }

    public static string Speaker(OcrResult result, string language)
    {
        var lines = result.Lines.Select(l => l.Text.Trim()).Where(s => s.Length > 0).ToArray();
        if (lines.Length <= 1) return string.Join("", lines);
        var separator = language.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? " " : "";
        return string.Join(separator, lines[..^1]) + "｜" + lines[^1];
    }

    public static string? ExactTerm(string text, IReadOnlyList<GlossaryEntry> glossary) =>
        glossary.FirstOrDefault(t => t.Source.Normalize(NormalizationForm.FormKC).Equals(
            text.Trim().Normalize(NormalizationForm.FormKC), StringComparison.OrdinalIgnoreCase))?.Target;
}
