using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Vortex.UI.Controls
{
    public static class TimestampMask
    {
        private const string MASK = "YYYY-MM-DD HH:MM:SS";

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(TimestampMask),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }

        public static string GetFilterValue(TextBox textBox)
        {
            if (textBox == null || string.IsNullOrEmpty(textBox.Text) || textBox.Text == MASK)
                return null;

            return textBox.Text;
        }

        public static bool MatchesWildcardPattern(string timestamp, string pattern)
        {
            if (string.IsNullOrEmpty(pattern) || pattern == MASK)
                return true;

            if (string.IsNullOrEmpty(timestamp))
                return false;

            if (timestamp.Length < pattern.Length)
                return false;

            for (int i = 0; i < pattern.Length; i++)
            {
                char patternChar = pattern[i];
                if (char.IsDigit(patternChar) && patternChar != timestamp[i])
                    return false;
            }

            return true;
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                if ((bool)e.NewValue)
                {
                    textBox.PreviewTextInput += OnPreviewTextInput;
                    textBox.PreviewKeyDown += OnPreviewKeyDown;
                    textBox.PreviewKeyUp += OnPreviewKeyUp;
                    textBox.PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;
                    textBox.GotFocus += OnGotFocus;
                    textBox.LostFocus += OnLostFocus;
                }
                else
                {
                    textBox.PreviewTextInput -= OnPreviewTextInput;
                    textBox.PreviewKeyDown -= OnPreviewKeyDown;
                    textBox.PreviewKeyUp -= OnPreviewKeyUp;
                    textBox.PreviewMouseLeftButtonUp -= OnPreviewMouseLeftButtonUp;
                    textBox.GotFocus -= OnGotFocus;
                    textBox.LostFocus -= OnLostFocus;
                }
            }
        }

        private static void OnGotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox)sender;
            if (string.IsNullOrEmpty(textBox.Text))
            {
                textBox.Text = MASK;
                textBox.CaretIndex = 0;
            }
        }

        private static void OnLostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox)sender;
            if (textBox.Text == MASK)
                textBox.Text = "";
        }

        private static void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var textBox = (TextBox)sender;

            textBox.Dispatcher.BeginInvoke(new Action(() =>
            {
                int clickPosition = textBox.CaretIndex;
                int bracketStart = GetBracketStartPosition(clickPosition);

                if (bracketStart != clickPosition)
                {
                    textBox.CaretIndex = bracketStart;
                }
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = (TextBox)sender;
            if (!char.IsDigit(e.Text[0]))
            {
                e.Handled = true;
                return;
            }

            var pos = textBox.CaretIndex;
            while (pos < textBox.Text.Length && !IsValidInputPosition(pos))
                pos++;

            if (pos < textBox.Text.Length)
            {
                var chars = textBox.Text.ToCharArray();
                chars[pos] = e.Text[0];
                textBox.Text = new string(chars);
                textBox.CaretIndex = pos + 1;
            }
            e.Handled = true;
        }

        private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            var textBox = (TextBox)sender;

            if (e.Key == Key.Back)
            {
                if (e.IsRepeat)
                {
                    DeleteAllBackwards(textBox);
                }
                else if (textBox.CaretIndex > 0)
                {
                    var pos = textBox.CaretIndex - 1;

                    if (pos >= 0 && pos < MASK.Length && textBox.Text[pos] == MASK[pos] && !char.IsDigit(MASK[pos]))
                    {
                        textBox.CaretIndex = pos;
                        e.Handled = true;
                        return;
                    }

                    while (pos >= 0 && !IsValidInputPosition(pos) && !char.IsDigit(textBox.Text[pos]))
                        pos--;

                    if (pos >= 0 && char.IsDigit(textBox.Text[pos]))
                    {
                        var chars = textBox.Text.ToCharArray();
                        chars[pos] = MASK[pos];
                        textBox.Text = new string(chars);
                        textBox.CaretIndex = pos;
                    }
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Delete)
            {
                if (e.IsRepeat)
                {
                    DeleteAllForwards(textBox);
                }
                else if (textBox.CaretIndex < textBox.Text.Length)
                {
                    var pos = textBox.CaretIndex;

                    if (pos >= 0 && pos < MASK.Length && textBox.Text[pos] == MASK[pos] && !char.IsDigit(MASK[pos]))
                    {
                        e.Handled = true;
                        return;
                    }

                    if (pos >= 0 && pos < textBox.Text.Length && char.IsDigit(textBox.Text[pos]))
                    {
                        var chars = textBox.Text.ToCharArray();
                        chars[pos] = MASK[pos];
                        textBox.Text = new string(chars);
                        textBox.CaretIndex = pos;
                    }
                }
                e.Handled = true;
            }
        }

        private static void OnPreviewKeyUp(object sender, KeyEventArgs e)
        {
        }

        private static void DeleteAllBackwards(TextBox textBox)
        {
            var chars = textBox.Text.ToCharArray();
            bool anyChanged = false;

            for (int i = textBox.CaretIndex - 1; i >= 0; i--)
            {
                if (char.IsDigit(chars[i]) && IsValidInputPosition(i))
                {
                    chars[i] = MASK[i];
                    anyChanged = true;
                }
            }

            if (anyChanged)
            {
                textBox.Text = new string(chars);
                textBox.CaretIndex = 0;
            }
        }

        private static void DeleteAllForwards(TextBox textBox)
        {
            var chars = textBox.Text.ToCharArray();
            bool anyChanged = false;

            for (int i = textBox.CaretIndex; i < chars.Length; i++)
            {
                if (char.IsDigit(chars[i]) && IsValidInputPosition(i))
                {
                    chars[i] = MASK[i];
                    anyChanged = true;
                }
            }

            if (anyChanged)
            {
                textBox.Text = new string(chars);
            }
        }

        private static bool IsValidInputPosition(int position)
        {
            if (position < 0 || position >= MASK.Length)
                return false;

            char maskChar = MASK[position];
            return maskChar != '-' && maskChar != ' ' && maskChar != ':';
        }

        private static int GetBracketStartPosition(int position)
        {
            if (position < 0 || position >= MASK.Length)
                return position;

            if (position >= 0 && position <= 3) return 0;
            if (position >= 5 && position <= 6) return 5;
            if (position >= 8 && position <= 9) return 8;
            if (position >= 11 && position <= 12) return 11;
            if (position >= 14 && position <= 15) return 14;
            if (position >= 17 && position <= 18) return 17;

            return position;
        }
    }
}