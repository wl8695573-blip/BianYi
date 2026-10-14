using System.IO;

namespace GuGuGaGaTranslator.Core.Translation;

/// <summary>内置基础词库来自仓库中的公共 JSON，避免代码与随包数据分叉。</summary>
internal static class LimbusProfile
{
    public static GameProfile CreateProfile() => GameProfileArchive.Deserialize(BundledProfile());
    private static string BundledProfile()
    {
        using var stream = typeof(LimbusProfile).Assembly.GetManifestResourceStream("terminology.limbus-company.json")
            ?? throw new InvalidOperationException("内置术语资源缺失。");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
    public const string Worldview = "《边狱巴士》（Limbus Company，Project Moon）的剧情文字。专有名词以本次匹配到的术语为准；没有匹配到的内容忠实翻译，不补充原文未提及的世界设定、人物身份或后续剧情。";
    public const string StyleHint = "保留原文的语气、停顿和黑色幽默。不添加原文没有的感叹号、语气词、人物身份或台词。说话者不明确时采用自然、中性的表达。";
}
