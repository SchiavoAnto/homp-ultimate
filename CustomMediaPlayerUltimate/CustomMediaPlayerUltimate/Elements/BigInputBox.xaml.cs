using System.Windows;
using System.Windows.Input;

namespace CustomMediaPlayerUltimate.Elements;

public partial class BigInputBox : Window
{
    public BigInputBox(string title = "Big Input Box", string message = "Insert here your text...")
    {
        InitializeComponent();
        Title = title;
        MessageLabel.Content = message;

        InputTextBox.Focus();
    }

    private void OkButtonClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CancelButtonClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void WindowKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            CancelButtonClick(null!, null!);
        }
        else if (e.Key == Key.Enter)
        {
            OkButtonClick(null!, null!);
        }
    }
}