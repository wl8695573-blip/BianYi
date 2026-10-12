using System.Windows;
using GuGuGaGaTranslator.Core.Capture;

namespace GuGuGaGaTranslator.App;

public partial class MainWindow
{
    private void OnPickTitle(object sender, RoutedEventArgs e) => PickRole("title");
    private void OnPickName(object sender, RoutedEventArgs e) => PickRole("name");

    private void PickRole(string field)
    {
        if (_targetSelections > 0)
            return;
        if (WindowList.SelectedItem is not WindowInfo target)
        { OnNotice("请先选择目标窗口。"); return; }
        PickRegionFor(target, field);
    }

    private void OnClearTitle(object sender, RoutedEventArgs e) => ClearRole(false);
    private void OnClearName(object sender, RoutedEventArgs e) => ClearRole(true);

    private void ClearRole(bool name)
    {
        if (name)
        {
            _session.Config.Target.NameRegion = null;
            _session.Config.Target.NameReferenceWidth = _session.Config.Target.NameReferenceHeight = 0;
        }
        else
        {
            _session.Config.Target.TitleRegion = null;
            _session.Config.Target.TitleReferenceWidth = _session.Config.Target.TitleReferenceHeight = 0;
        }
        _session.Config.Target.SpeakerRegion = null;
        _session.Config.Target.SpeakerReferenceWidth = _session.Config.Target.SpeakerReferenceHeight = 0;
        _session.ForgetContext();
        _session.SaveConfig();
        UpdateSpeakerSummary();
        OnNotice(name ? "已清除姓名区域。" : "已清除称号区域。");
    }

    private void UpdateSpeakerSummary()
    {
        var target = _session.Config.Target;
        var title = target.TitleRegion is { IsUsable: true } t ? t.ToString() : "未设置（可选）";
        var name = target.NameRegion is { IsUsable: true } n ? n.ToString() : "未设置（可选）";
        SpeakerRegionSummary.Text = $"称号：{title}\n姓名：{name}";
        if (target.SpeakerRegion is { IsUsable: true } && target.TitleRegion is null && target.NameRegion is null)
            SpeakerRegionSummary.Text += "\n旧角色选区仍可用，建议分别重新框选。";
    }
}
