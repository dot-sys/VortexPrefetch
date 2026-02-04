using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using Vortex.UI.ViewModels;

// Main application UI layer namespace
namespace Vortex.UI
{
    // Primary application window with navigation frame
    public partial class MainWindow : Window
    {
        // Tracks if first navigation occurred
        private bool _isFirstNavigation = true;

        // Initializes window and language menu handlers
        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += MainWindow_Loaded;

            foreach (MenuItem item in LanguageButton.ContextMenu.Items)
            {
                item.Click += LanguageMenuItem_Click;
            }
        }

        // Sets frame reference in viewmodel after load
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.SetFrame(MainFrame);
            }

            CloseButton.ApplyTemplate();
        }

        // Adjusts corner radius based on window state
        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            var cornerRadius = WindowState == WindowState.Maximized
            ? new CornerRadius(0)
            : new CornerRadius(8);

            if (this.Content is Border mainBorder)
            {
                mainBorder.CornerRadius = cornerRadius;
            }

            var closeButtonRadius = WindowState == WindowState.Maximized
                ? new CornerRadius(0)
                : new CornerRadius(0, 8, 0, 0);

            UpdateCloseButtonCornerRadius(closeButtonRadius);
        }

        // Updates close button border corner radius
        private void UpdateCloseButtonCornerRadius(CornerRadius radius)
        {
            if (CloseButton != null)
            {
                CloseButton.ApplyTemplate();
                if (CloseButton.Template?.FindName("CloseBorder", CloseButton) is Border border)
                {
                    border.CornerRadius = radius;
                }
            }
        }

        // Enables window dragging from titlebar
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                this.DragMove();
            }
            catch
            {
            }
        }

        // Minimizes window to taskbar
        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        // Refreshes current view through viewmodel
        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.RefreshCurrentView();
            }
        }

        // Opens language selection context menu
        private void Language_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.ContextMenu != null)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                button.ContextMenu.IsOpen = true;
            }
        }

        // Toggles maximized and normal window state
        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            ToggleMaximizeRestore();
        }

        // Switches between maximized and normal states
        private void ToggleMaximizeRestore()
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        // Closes application window
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Animates page transitions with fade effect
        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if (!(MainFrame.Content is FrameworkElement content))
                return;

            if (_isFirstNavigation)
            {
                _isFirstNavigation = false;
                content.Opacity = 1;
                return;
            }

            content.Opacity = 0;

            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.8),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            content.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }

        // Handles language menu item selection
        private void LanguageMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Header is string languageCode)
            {
                ChangeLanguage(languageCode);
            }
        }

        // Switches application language resource dictionary
        private void ChangeLanguage(string languageCode)
        {
            string resourcePath = GetResourcePath(languageCode);

            var existingLanguageDict = Application.Current.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source != null &&
                (d.Source.OriginalString.Contains("Resources/Strings.") ||
                 d.Source.OriginalString.Contains("/Resources/Strings.")));

            if (existingLanguageDict != null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(existingLanguageDict);
            }

            var newDict = new ResourceDictionary
            {
                Source = new Uri(resourcePath, UriKind.Relative)
            };

            Application.Current.Resources.MergedDictionaries.Insert(0, newDict);

            LanguageButton.Content = languageCode;

            if (DataContext is MainWindowViewModel vm)
            {
                vm.ReloadCurrentView();
            }
        }

        // Maps language code to resource file path
        private static string GetResourcePath(string languageCode)
        {
            switch (languageCode)
            {
                case "DE":
                    return "Resources/Strings.de-DE.xaml";
                case "UA":
                    return "Resources/Strings.uk-UA.xaml";
                case "RU":
                    return "Resources/Strings.ru-RU.xaml";
                case "ES":
                    return "Resources/Strings.es-ES.xaml";
                case "PT":
                    return "Resources/Strings.pt-PT.xaml";
                case "EN":
                default:
                    return "Resources/Strings.en-US.xaml";
            }
        }
    }
}
