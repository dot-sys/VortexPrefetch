using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Vortex.UI.ViewModels;
using Prefetch;
using Prefetch.Models;
using System.Collections.ObjectModel;

namespace Vortex.UI.Views
{
    public partial class WelcomeView : Page
    {
        private MainWindowViewModel _viewModel;
        private bool _isTraceStarted = false;
        private readonly DispatcherTimer _dotsTimer;
        private int _dotsCount = 0;
        private Storyboard _logoSpinStoryboard;

        public WelcomeView()
        {
            InitializeComponent();

            _dotsTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _dotsTimer.Tick += DotsTimer_Tick;
        }

        private string GetResourceString(string key)
        {
            try
            {
                var resource = Application.Current.TryFindResource(key);
                return resource as string ?? key;
            }
            catch
            {
                return key;
            }
        }

        private void DotsTimer_Tick(object sender, EventArgs e)
        {
            _dotsCount = (_dotsCount + 1) % 4;
            DotsText.Text = new string('.', _dotsCount);
        }

        private string FormatDate(DateTime? value)
        {
            if (!value.HasValue || value.Value == DateTime.MinValue)
            {
                return "N/A";
            }

            return value.Value.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private async void StartTrace_Click(object sender, RoutedEventArgs e)
        {
            if (_isTraceStarted) return;

            _isTraceStarted = true;
            _viewModel = DataContext as MainWindowViewModel;

            if (_viewModel == null)
            {
                MessageBox.Show(GetResourceString("UnableToAccessViewModel"), GetResourceString("Error"));
                return;
            }

            StartTraceButton.IsEnabled = false;
            StatusPanel.Visibility = Visibility.Visible;
            StatusText.Text = GetResourceString("StartingAnalysis");

            StartLogoSpin();

            await Task.Delay(500);

            await FadeOutStatusText();

            _dotsCount = 0;
            DotsText.Text = "";
            _dotsTimer.Start();

            await AnalyzePrefetchAsync();
        }

        private async Task AnalyzePrefetchAsync()
        {
            try
            {
                StatusText.Text = GetResourceString("AnalyzingPrefetchFiles");
                await FadeInStatusText();

                var prefetchDir = @"C:\Windows\Prefetch";
                if (!Directory.Exists(prefetchDir))
                {
                    throw new DirectoryNotFoundException("Prefetch directory not found: " + prefetchDir);
                }

                string[] prefetchFiles;
                try
                {
                    prefetchFiles = Directory.GetFiles(prefetchDir, "*.pf");
                }
                catch (Exception ex)
                {
                    throw new IOException("Failed to enumerate prefetch files in " + prefetchDir, ex);
                }
                var prefetchData = new System.Collections.Generic.List<PrefetchData>();

                await Task.Run(() =>
                {
                    foreach (var file in prefetchFiles)
                    {
                        try
                        {
                            var data = PrefetchAnalyzer.AnalyzePrefetchFile(file);
                            if (data != null)
                            {
                                prefetchData.Add(data);
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                });

                _dotsTimer.Stop();
                StatusText.Text = GetResourceString("AnalysisComplete");
                DotsText.Text = "";

                StopLogoSpin();

                await Task.Delay(500);
                await FadeOutAndNavigateToResults(prefetchData);
            }
            catch (Exception ex)
            {
                _dotsTimer.Stop();
                DotsText.Text = "";
                StatusText.Text = string.Format(GetResourceString("ErrorAnalyzingPrefetch"), ex.Message);
                StartTraceButton.IsEnabled = true;
                _isTraceStarted = false;

                MessageBox.Show(string.Format(GetResourceString("ErrorAnalyzingPrefetch"), ex.Message),
                                GetResourceString("AnalysisError"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task FadeOutStatusText()
        {
            await AnimateOpacity(StatusPanel, 1.0, 0.0, 0.5);
        }

        private async Task FadeInStatusText()
        {
            await AnimateOpacity(StatusPanel, 0.0, 1.0, 0.5);
        }

        private async Task FadeOutAndNavigateToResults(System.Collections.Generic.List<PrefetchData> prefetchData)
        {
            await AnimateOpacity(MainGrid, 1.0, 0.0, 0.5);
            _viewModel.PrefetchViewModel.SetFilteredFiles(prefetchData);
            _viewModel?.NavigateToPrefetchAnalyzer();
        }

        private async Task AnimateOpacity(UIElement element, double from, double to, double durationSeconds)
        {
            var animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromSeconds(durationSeconds),
                EasingFunction = new QuadraticEase { EasingMode = from > to ? EasingMode.EaseOut : EasingMode.EaseIn }
            };

            var tcs = new TaskCompletionSource<bool>();
            animation.Completed += (s, e) => tcs.SetResult(true);
            element.BeginAnimation(UIElement.OpacityProperty, animation);

            await tcs.Task;
        }

        private void ShowStatus(string message)
        {
            StatusPanel.Visibility = Visibility.Visible;
            StatusText.Text = message;
            DotsText.Text = "";
        }

        private void StartLogoSpin()
        {
            if (_logoSpinStoryboard != null)
                return;

            var logoImage = (Image)this.FindName("WelcomeLogoImage");
            if (logoImage == null)
                return;

            var storyboard = new Storyboard
            {
                RepeatBehavior = RepeatBehavior.Forever
            };

            var rotationAnimation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(1.2),
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new PowerEase
                {
                    EasingMode = EasingMode.EaseInOut,
                    Power = 2
                }
            };

            Storyboard.SetTarget(rotationAnimation, logoImage);
            Storyboard.SetTargetProperty(rotationAnimation, new PropertyPath("(UIElement.RenderTransform).(RotateTransform.Angle)"));
            storyboard.Children.Add(rotationAnimation);

            _logoSpinStoryboard = storyboard;
            _logoSpinStoryboard.Begin();
        }

        private void StopLogoSpin()
        {
            if (_logoSpinStoryboard == null)
                return;

            var logoImage = (Image)this.FindName("WelcomeLogoImage");
            if (logoImage == null)
                return;

            _logoSpinStoryboard.Stop();

            var resetAnimation = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            if (logoImage.RenderTransform is RotateTransform rotateTransform)
            {
                rotateTransform.BeginAnimation(RotateTransform.AngleProperty, resetAnimation);
            }
            _logoSpinStoryboard = null;
        }
    }
}