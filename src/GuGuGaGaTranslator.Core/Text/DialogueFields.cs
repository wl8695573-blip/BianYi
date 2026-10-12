namespace GuGuGaGaTranslator.Core.Text;

public static class DialogueFields
{
    public static string Format(string title, string name, string body, bool structured = false) =>
        structured || title.Length > 0 || name.Length > 0
            ? $"称号：{title}\n姓名：{name}\n正文：{body}" : body;

    public static string Header(string title, string name, bool structured = false) =>
        structured || title.Length > 0 || name.Length > 0 ? $"称号：{title}\n姓名：{name}" : "";

    public static (string Title, string Name) LegacySpeaker(string speaker)
    {
        var fields = speaker.Split('｜', 2, StringSplitOptions.TrimEntries);
        return fields.Length == 2 ? (fields[0], fields[1]) : ("", fields[0]);
    }
}
