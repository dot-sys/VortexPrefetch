using System;
using System.Diagnostics;
using System.Windows;

// Main application UI layer namespace
namespace Vortex.UI
{
    // Application entry point and initialization handler
    public partial class App : Application
    {
        // Initializes application and disables binding trace output
        public App()
        {
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Off;
            InitializeComponent();
        }

        // Suppresses cryptographic exceptions during startup
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.FirstChanceException += (sender, args) =>
            {
                if (args.Exception is System.Security.Cryptography.CryptographicException)
                {
                }
            };

            base.OnStartup(e);
        }
    }
}