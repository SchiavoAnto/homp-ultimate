using System.Windows;

namespace CustomMediaPlayerUltimate
{
    public partial class BigInputBox : Window
    {
        public BigInputBox(string title = "Big Input Box", string message = "Insert here your text...")
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
}
