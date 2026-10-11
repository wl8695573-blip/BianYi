using System.Windows;
using GuGuGaGaTranslator.Core.Capture;

namespace GuGuGaGaTranslator.App;

public partial class MainWindow
{
    private void OnPickSpeaker(object sender, RoutedEventArgs e)
    {
        if (_targetSelections > 0) return;
        if (WindowList.SelectedItem is not WindowInfo target)
        { OnNotice("请先选择目标窗口。"); return; }
        PickRegionFor(target, speaker: true);
    }

    private void OnClearSpeaker(object sender, RoutedEventArgs e)
    {
        _session.Config.Target.SpeakerRegion = null;
        _session.Config.Target.SpeakerReferenceWidth = _session.Config.Target.SpeakerReferenceHeight = 0;
        _session.ForgetContext();
        _session.SaveConfig();
        UpdateSpeakerSummary();
        OnNotice("已清除称号／姓名区域，正文区域保持当前设置。");
    }

    private void UpdateSpeakerSummary()
    {
        SpeakerRegionSummary.Text = _session.Config.Target.SpeakerRegion is { IsUsable: true } region
            ? $"称号／姓名：{region}" : "称号／姓名：未设置（可选）";
    }
}
