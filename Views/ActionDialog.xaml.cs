using System;
using System.Windows;

namespace Sleipnir.App.Views
{
    public partial class ActionDialog : Window
    {
        public enum DialogResultAction
        {
            Action1,
            Action2,
            Cancel
        }

        public DialogResultAction Result { get; private set; } = DialogResultAction.Cancel;

        public ActionDialog(string title, string message, string btn1Text = null, string btn2Text = null, string cancelText = "Cancel")
        {
            InitializeComponent();
            DialogTitle.Text = title;
            DialogMessage.Text = message;
            
            if (!string.IsNullOrEmpty(btn1Text))
            {
                Btn1.Content = btn1Text;
                Btn1.Visibility = Visibility.Visible;
            }
            
            if (!string.IsNullOrEmpty(btn2Text))
            {
                Btn2.Content = btn2Text;
                Btn2.Visibility = Visibility.Visible;
            }

            BtnCancel.Content = cancelText;
        }

        public static DialogResultAction Show(string title, string message, string btn1Text = null, string btn2Text = null, string cancelText = "Cancel")
        {
            var dialog = new ActionDialog(title, message, btn1Text, btn2Text, cancelText);
            dialog.ShowDialog();
            return dialog.Result;
        }

        private void Btn1_Click(object sender, RoutedEventArgs e)
        {
            Result = DialogResultAction.Action1;
            DialogResult = true;
            Close();
        }

        private void Btn2_Click(object sender, RoutedEventArgs e)
        {
            Result = DialogResultAction.Action2;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Result = DialogResultAction.Cancel;
            DialogResult = false;
            Close();
        }
    }
}
