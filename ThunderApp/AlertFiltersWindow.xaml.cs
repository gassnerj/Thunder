using System.Windows;

namespace ThunderApp;

public partial class AlertFiltersWindow : Window
{
    public AlertFiltersWindow()
    {
        InitializeComponent();

        // Esc closes
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                Close();
                e.Handled = true;
            }
        };
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
