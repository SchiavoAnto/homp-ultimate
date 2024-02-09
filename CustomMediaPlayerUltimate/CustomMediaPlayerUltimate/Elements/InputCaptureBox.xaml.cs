using System.Windows;
using System.Windows.Input;

namespace CustomMediaPlayerUltimate.Elements;

public partial class InputCaptureBox : Window
{
    public Key KeyResult;

    public InputCaptureBox(string title = "Input capture box", string message = "Press a key (escape to cancel)...")
    {
        InitializeComponent();
        Title = title;
        MessageLabel.Content = message;
    }

    private void OnBoxKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
        }
        else if (e.Key != Key.LeftCtrl &&
                 e.Key != Key.RightCtrl &&
                 e.Key != Key.LeftShift &&
                 e.Key != Key.RightShift &&
                 e.Key != Key.LeftAlt &&
                 e.Key != Key.RightAlt &&
                 e.SystemKey != Key.LeftAlt &&
                 e.SystemKey != Key.RightAlt)
        {
            DialogResult = true;
            KeyResult = e.Key;
            Close();
        }
    }
}
