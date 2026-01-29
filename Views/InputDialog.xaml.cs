using System.Windows;
using System.Windows.Input;

namespace Sleipnir.App.Views
{
    public partial class InputDialog : Window
    {
        public string ResultText { get; private set; } = string.Empty;

        public InputDialog(string prompt, string defaultText = "")
        {
            InitializeComponent();
            PromptTitle.Text = prompt.ToUpper();
            InputText.Text = defaultText;
            InputText.Focus();
            InputText.SelectAll();
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            ResultText = InputText.Text;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void InputText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) Confirm_Click(sender, e);
            else if (e.Key == Key.Escape) Cancel_Click(sender, e);
        }

        public static string? Show(string prompt, string defaultText = "")
        {
            var dialog = new InputDialog(prompt, defaultText);
            if (dialog.ShowDialog() == true) return dialog.ResultText;
            return null;
        }
    }
}
