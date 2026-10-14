namespace GuGuGaGaTranslator.Core.Text;

public static class DialogueFields
{
    public static string Format(string title, string name, string body, bool structured = false) =>
        structured || title.Length > 0 || name.Length > 0
            ? $"称号：{title}\n姓名：{name}\n正文：{body}" : body;

    public static string Header(string title, string name) =>
        string.Join("｜", new[] { title, name }.Select(value => value.Trim()).Where(value => value.Length > 0));

    public static string Translated(string title, string name, string body) => Join(Header(title, name), body);

    private static string Join(params string[] values) =>
        string.Join("\n", values.Select(s => s.Trim()).Where(s => s.Length > 0));

    public static (string Title, string Name) LegacySpeaker(string speaker)
    {
        // NFKC 会将全角分隔符转为半角，旧选区的两种写法都须读取。
        var fields = speaker.Split(['｜', '|'], 2, StringSplitOptions.TrimEntries);
        return fields.Length == 2 ? (fields[0], fields[1]) : ("", fields[0]);
    }
}
