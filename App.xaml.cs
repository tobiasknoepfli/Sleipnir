using System.Windows;

namespace Sleipnir.App
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            this.DispatcherUnhandledException += (s, args) =>
            {
                MessageBox.Show($"Unhandled Exception: {args.Exception.Message}\n\n{args.Exception.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };
        }
    }
}
