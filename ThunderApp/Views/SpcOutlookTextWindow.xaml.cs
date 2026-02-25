using System.Windows;
using System.Windows.Input;

namespace ThunderApp.Views;

public partial class SpcOutlookTextWindow : Window
{
    public SpcOutlookTextWindow()
    {
        InitializeComponent();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (IsClickOnCaptionButton(e.OriginalSource as DependencyObject)) return;

        if (e.ClickCount == 2)
        {
            ToggleMaximize();
            return;
        }

        try { DragMove(); } catch { }
    }

    private static bool IsClickOnCaptionButton(DependencyObject? original)
    {
        if (original is null) return false;
        var cur = original;
        while (cur is not null)
        {
            if (cur is System.Windows.Controls.Primitives.ButtonBase) return true;
            cur = System.Windows.Media.VisualTreeHelper.GetParent(cur);
        }
        return false;
    }

    private void Minimize_OnClick(object sender, RoutedEventArgs e) => SystemCommands.MinimizeWindow(this);
    private void Maximize_OnClick(object sender, RoutedEventArgs e) => ToggleMaximize();
    private void Close_OnClick(object sender, RoutedEventArgs e) => Close();

    private void ToggleMaximize()
    {
        if (WindowState == WindowState.Maximized) SystemCommands.RestoreWindow(this);
        else SystemCommands.MaximizeWindow(this);
    }

}
