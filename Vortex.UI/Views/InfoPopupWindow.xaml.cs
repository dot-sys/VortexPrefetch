using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Vortex.UI.Views
{
    public partial class InfoPopupWindow : Window
    {
        public InfoPopupWindow()
        {
            InitializeComponent();
        }

        public InfoPopupWindow(string title, string content) : this()
        {
            Title = title;

            var textBlock = new TextBlock
            {
                Text = content,
                Foreground = Brushes.White,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(5)
            };

            ContentPresenter.Content = textBlock;
        }

        public InfoPopupWindow(string title, UIElement content) : this()
        {
            Title = title;
            ContentPresenter.Content = content;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                try
                {
                    this.DragMove();
                }
                catch
                {
                }
            }
        }
    }
}
