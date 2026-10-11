using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using GuGuGaGaTranslator.Core.Config;
using GuGuGaGaTranslator.Core.Ocr;

namespace GuGuGaGaTranslator.App;

public partial class MainWindow
{
    private void OnEditOcrCorrections(object sender, RoutedEventArgs e)
    {
        var original = _session.Config.Ocr.Corrections;
        var rows = new ObservableCollection<OcrCorrection>(original.Select(r => new OcrCorrection
        { Language = r.Language, Profile = r.Profile, Field = r.Field, Wrong = r.Wrong, Correct = r.Correct }));
        var window = new Window { Owner = this, Title = "OCR 纠错规则", Width = 850, Height = 480,
            MinWidth = 680, MinHeight = 350, WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = Background, Foreground = Foreground };
        var panel = new DockPanel { Margin = new Thickness(14) };
        var hint = new TextBlock { Text = "每条规则仅作用于指定游戏和语言。speaker = 称号／姓名，body = 正文。游戏标识可在游戏档案中查看。",
            TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,0,0,12) };
        DockPanel.SetDock(hint, Dock.Top); panel.Children.Add(hint);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        DockPanel.SetDock(buttons, Dock.Bottom); panel.Children.Add(buttons);
        var grid = new DataGrid { ItemsSource = rows, AutoGenerateColumns = false, CanUserAddRows = true,
            SelectionMode = DataGridSelectionMode.Extended, Background = Theme.Brush("FieldBrush", System.Windows.Media.Colors.Black),
            Foreground = Foreground, RowBackground = Theme.Brush("SurfaceBrush", System.Windows.Media.Colors.Black),
            HeadersVisibility = DataGridHeadersVisibility.Column };
        var headers = new Style(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader));
        headers.Setters.Add(new Setter(Control.BackgroundProperty, Theme.Brush("SurfaceBrush", System.Windows.Media.Colors.Black)));
        headers.Setters.Add(new Setter(Control.ForegroundProperty, Foreground));
        headers.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(8)));
        grid.ColumnHeaderStyle = headers;
        grid.GridLinesVisibility = DataGridGridLinesVisibility.None;
        foreach (var (label, path) in new[] { ("语言", "Language"), ("游戏标识", "Profile"), ("字段", "Field"), ("识别错字", "Wrong"), ("正确文字", "Correct") })
            grid.Columns.Add(new DataGridTextColumn { Header = label, Binding = new Binding(path),
                MinWidth = 95, Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
        panel.Children.Add(grid);
        var remove = new Button { Content = "删除选中" };
        remove.Click += (_, _) => { foreach (var row in grid.SelectedItems.OfType<OcrCorrection>().ToArray()) rows.Remove(row); };
        var save = new Button { Content = "保存并应用" };
        save.Click += (_, _) =>
        {
            if (!grid.CommitEdit(DataGridEditingUnit.Cell, true) || !grid.CommitEdit(DataGridEditingUnit.Row, true)) return;
            if (rows.Count > 500 || rows.Any(r => !DefaultLanguagePresets.IsSupported(r.Language, false)
                || r.Field is not ("speaker" or "body") || r.Wrong.Length is 0 or > 200 || r.Correct.Length is 0 or > 200
                || string.IsNullOrWhiteSpace(r.Wrong) || string.IsNullOrWhiteSpace(r.Correct)
                || (r.Profile.Length > 0 && _session.FindProfile(r.Profile) is null)))
            { MessageBox.Show(window, "语言填写 en、ja、zh-Hans 或 ko；字段填写 speaker 或 body。游戏标识须对应现有档案（通用翻译留空）。错字和正确文字须为 1–200 字，最多 500 条。"); return; }
            try
            {
                _session.Config.Ocr.Corrections = rows.ToList();
                _session.SaveConfig(); _session.ForgetContext(); window.DialogResult = true;
            }
            catch (Exception) { _session.Config.Ocr.Corrections = original; MessageBox.Show(window, "无法保存规则，请检查配置目录权限。"); }
        };
        buttons.Children.Add(remove); buttons.Children.Add(save); buttons.Children.Add(new Button { Content = "取消", IsCancel = true });
        window.Content = panel;
        window.ShowDialog();
    }
}
