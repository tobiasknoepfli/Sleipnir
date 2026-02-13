using System.Windows;
using Sleipnir.App.Views;

namespace Sleipnir.App
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            this.DispatcherUnhandledException += (s, args) =>
            {
                CustomDialogWindow.Show("Error", $"Unhandled Exception: {args.Exception.Message}\n\n{args.Exception.StackTrace}", CustomDialogWindow.DialogType.Error);
                args.Handled = true;
            };

            // Connected to USER's Supabase project (Match MainWindow.xaml.cs)
            var dataService = new Sleipnir.App.Services.SupabaseDataService(
                "https://sagdinxeeztqhxqjvcyo.supabase.co", 
                "sb_publishable_3SmPKI_gR-UWCoxMv3A5Qw_Bqe3du7U");

            await dataService.InitializeAsync();

            var loginVm = new Sleipnir.App.ViewModels.LoginViewModel(dataService);
            var loginWindow = new Sleipnir.App.Views.LoginWindow(loginVm);

            // Check for Auto-login and Ensure Admin
            try
            {
                // Ensure at least one admin exists
                var allUsers = await dataService.GetUsersAsync();
                if (!allUsers.Any())
                {
                    await dataService.CreateUserAsync(new Sleipnir.App.Models.AppUser
                    {
                        Username = "admin",
                        Password = "admin",
                        FirstName = "System",
                        LastName = "Admin",
                        Email = "admin@sleipnir.app",
                        PendingEmail = "admin@sleipnir.app",
                        IsEmailVerified = true,
                        IsRoot = true,
                        IsSuperuser = true,
                        Emoji = "⚡"
                    });
                }

                var autoUser = allUsers.FirstOrDefault(u => u.CanAutoLogin);
                if (autoUser != null)
                {
                    ShowMainWindow(dataService, autoUser);
                    return;
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("public.users") || ex.Message.Contains("'email' column"))
                {
                    CustomDialogWindow.Show("Database Setup Required", "Database Schema Mismatch: The 'users' table or 'email' column is missing.\n\nPlease run the SQL script provided earlier in your Supabase SQL Editor and then click 'Reload PostgREST schema' in Supabase Settings -> API.", CustomDialogWindow.DialogType.Warning);
                }
                else
                {
                     // Log or ignore minor init errors to allow login window to show if possible
                     System.Diagnostics.Debug.WriteLine($"Startup Error: {ex.Message}");
                }
            }

            // Prevent app from shutting down when LoginWindow closes
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            bool? result = loginWindow.ShowDialog();

            if (result == true)
            {
                ShowMainWindow(dataService, loginVm.AuthenticatedUser!);
                // Revert to normal shutdown mode
                this.ShutdownMode = ShutdownMode.OnMainWindowClose;
            }
            else
            {
                Shutdown();
            }
        }

        private void ShowMainWindow(Sleipnir.App.Services.IDataService dataService, Sleipnir.App.Models.AppUser user)
        {
            var mainVm = new Sleipnir.App.ViewModels.MainViewModel(dataService);
            mainVm.CurrentUser = user;
            
            var mainWindow = new Sleipnir.App.Views.MainWindow(mainVm);
            this.MainWindow = mainWindow;
            
            // Ensure app shuts down when main window closes
            this.ShutdownMode = ShutdownMode.OnMainWindowClose;
            
            // Explicitly shutdown when window is closed
            mainWindow.Closed += (s, e) => 
            {
                Application.Current.Shutdown();
            };
            
            mainWindow.Show();
        }
    }
}
