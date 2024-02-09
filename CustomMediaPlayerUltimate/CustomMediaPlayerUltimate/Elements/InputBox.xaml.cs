using System.Windows;

namespace CustomMediaPlayerUltimate.Elements;

public partial class InputBox : Window
{
    public InputBox(string title = "Input Box", string message = "Insert here your text...")
    {
        InitializeComponent();
        Title = title;
        MessageLabel.Content = message;
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
}