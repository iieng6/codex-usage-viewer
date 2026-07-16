using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using CodexUsageViewer.Usage;

namespace CodexUsageViewer
{
    internal sealed class MainWindow : Window
    {
        private readonly UsageService usageService;
        private readonly TextBlock shortLabel;
        private readonly StatusValue shortValue;
        private readonly TextBlock longLabel;
        private readonly StatusValue longValue;
        private readonly TextBlock resetValue;
        private readonly Button refreshButton;
        private readonly Button closeButton;
        private readonly RotateTransform refreshRotation;
        private readonly Storyboard refreshStoryboard;
        private CancellationTokenSource refreshCancellation;
        private bool exitRequested;

        public MainWindow(UsageService usageService)
        {
            this.usageService = usageService;
            Title = ProgramInfo.Name;
            Width = 230;
            Height = 175;
            MinWidth = 230;
            MinHeight = 155;
            MaxWidth = 320;
            MaxHeight = 240;
            Topmost = true;
            ShowInTaskbar = true;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.CanResizeWithGrip;
            Background = SystemColors.WindowBrush;
            FontFamily = new FontFamily("Segoe UI");
            FontSize = 13;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            shortLabel = CreateText("5h");
            shortValue = new StatusValue();
            longLabel = CreateText("Week");
            longValue = new StatusValue();
            resetValue = CreateValue("—");
            refreshRotation = new RotateTransform(0);
            refreshButton = CreateRefreshButton(refreshRotation);
            closeButton = CreateCloseButton();
            refreshStoryboard = CreateRefreshStoryboard(refreshRotation);

            Content = CreateLayout();
            WindowStateStore.Restore(this);

            Loaded += OnLoaded;
            Closing += OnClosing;
            MouseLeftButtonDown += OnWindowMouseLeftButtonDown;
            KeyDown += OnKeyDown;
            refreshButton.Click += OnRefreshClick;
            closeButton.Click += OnCloseClick;
        }

        private UIElement CreateLayout()
        {
            Border border = new Border
            {
                BorderBrush = SystemColors.ActiveBorderBrush,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14, 11, 10, 8),
                Background = SystemColors.WindowBrush
            };

            Grid root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Grid header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(27) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Border titleBalance = new Border { Width = 27 };
            Grid.SetColumn(titleBalance, 0);
            header.Children.Add(titleBalance);

            TextBlock title = new TextBlock
            {
                Text = ProgramInfo.Name,
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = SystemColors.WindowTextBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                ToolTip = "双击查看 About"
            };
            title.MouseLeftButtonDown += OnTitleMouseLeftButtonDown;
            Grid.SetColumn(title, 1);
            header.Children.Add(title);
            Grid.SetColumn(closeButton, 2);
            header.Children.Add(closeButton);
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            Grid rows = new Grid();
            rows.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(62) });
            rows.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            rows.RowDefinitions.Add(new RowDefinition { Height = new GridLength(25) });
            rows.RowDefinitions.Add(new RowDefinition { Height = new GridLength(25) });
            rows.RowDefinitions.Add(new RowDefinition { Height = new GridLength(25) });
            AddRow(rows, 0, shortLabel, shortValue);
            AddRow(rows, 1, longLabel, longValue);
            AddRow(rows, 2, CreateText("Reset"), resetValue);
            Grid.SetRow(rows, 2);
            root.Children.Add(rows);

            Grid.SetRow(refreshButton, 4);
            root.Children.Add(refreshButton);
            border.Child = root;
            return border;
        }

        private static void AddRow(Grid grid, int row, TextBlock label, FrameworkElement value)
        {
            Grid.SetRow(label, row);
            Grid.SetColumn(label, 0);
            Grid.SetRow(value, row);
            Grid.SetColumn(value, 1);
            grid.Children.Add(label);
            grid.Children.Add(value);
        }

        private static TextBlock CreateText(string text)
        {
            return new TextBlock
            {
                Text = text,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = SystemColors.WindowTextBrush
            };
        }

        private static TextBlock CreateValue(string text)
        {
            TextBlock block = CreateText(text);
            block.HorizontalAlignment = HorizontalAlignment.Right;
            return block;
        }

        private static Button CreateRefreshButton(RotateTransform rotation)
        {
            TextBlock icon = new TextBlock
            {
                Text = "↻",
                FontSize = 20,
                Width = 22,
                Height = 22,
                TextAlignment = TextAlignment.Center,
                RenderTransform = rotation,
                RenderTransformOrigin = new Point(0.5, 0.5)
            };

            return new Button
            {
                Content = icon,
                Width = 34,
                Height = 29,
                Padding = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Right,
                ToolTip = "重新读取 Codex Usage"
            };
        }

        private static Button CreateCloseButton()
        {
            return new Button
            {
                Content = "✕",
                Width = 24,
                Height = 22,
                Padding = new Thickness(0),
                Margin = new Thickness(6, -3, -3, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = SystemColors.WindowTextBrush,
                ToolTip = "Hide to system tray"
            };
        }

        private static Storyboard CreateRefreshStoryboard(RotateTransform rotation)
        {
            DoubleAnimation animation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(0.8),
                RepeatBehavior = RepeatBehavior.Forever
            };
            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(animation);
            Storyboard.SetTarget(animation, rotation);
            Storyboard.SetTargetProperty(animation, new PropertyPath(RotateTransform.AngleProperty));
            return storyboard;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await RefreshAsync();
        }

        private async void OnRefreshClick(object sender, RoutedEventArgs e)
        {
            await RefreshAsync();
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            HideToTray();
        }

        internal async void RefreshFromTray()
        {
            await RefreshAsync();
        }

        internal void ShowFromTray()
        {
            if (!IsVisible)
            {
                Show();
            }
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }
            Activate();
            Topmost = true;
        }

        internal void RequestExit()
        {
            exitRequested = true;
            if (refreshCancellation != null)
            {
                refreshCancellation.Cancel();
            }
            WindowStateStore.Save(this);
            Close();
        }

        private async Task RefreshAsync()
        {
            if (refreshCancellation != null)
            {
                return;
            }

            refreshCancellation = new CancellationTokenSource();
            refreshButton.IsEnabled = false;
            refreshStoryboard.Begin();

            try
            {
                UsageSnapshot snapshot = await usageService.RefreshAsync(refreshCancellation.Token);
                ApplySnapshot(snapshot);
            }
            catch (OperationCanceledException)
            {
            }
            catch (UsageException exception)
            {
                MessageBox.Show(this, exception.Message, ProgramInfo.Name, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch
            {
                MessageBox.Show(this, "数据解析失败。", ProgramInfo.Name, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                refreshStoryboard.Stop();
                refreshRotation.Angle = 0;
                refreshButton.IsEnabled = true;
                refreshCancellation.Dispose();
                refreshCancellation = null;
            }
        }

        private void ApplySnapshot(UsageSnapshot snapshot)
        {
            ApplyWindow(snapshot.ShortWindow, shortLabel, shortValue, "5h");
            ApplyWindow(snapshot.LongWindow, longLabel, longValue, "Week");
            resetValue.Text = snapshot.LongWindow == null ? "—" : FormatRemaining(snapshot.LongWindow.ResetsAt);
        }

        private static void ApplyWindow(UsageWindow window, TextBlock label, StatusValue value, string emptyLabel)
        {
            if (window == null)
            {
                label.Text = emptyLabel;
                value.SetUnavailable();
                return;
            }

            label.Text = FormatDurationLabel(window.DurationMinutes);
            int remainingPercent = 100 - window.UsedPercent;
            value.SetRemaining(remainingPercent);
        }

        private sealed class StatusValue : StackPanel
        {
            private static readonly Brush GreenBrush = new SolidColorBrush(Color.FromRgb(22, 163, 74));
            private static readonly Brush YellowBrush = new SolidColorBrush(Color.FromRgb(217, 154, 0));
            private static readonly Brush RedBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38));
            private readonly Ellipse indicator;
            private readonly TextBlock percentage;

            public StatusValue()
            {
                Orientation = Orientation.Horizontal;
                HorizontalAlignment = HorizontalAlignment.Right;
                VerticalAlignment = VerticalAlignment.Center;

                indicator = new Ellipse
                {
                    Width = 9,
                    Height = 9,
                    Margin = new Thickness(0, 0, 6, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Visibility = Visibility.Collapsed
                };
                percentage = new TextBlock
                {
                    Text = "—",
                    Foreground = SystemColors.WindowTextBrush,
                    VerticalAlignment = VerticalAlignment.Center
                };

                Children.Add(indicator);
                Children.Add(percentage);
            }

            public void SetUnavailable()
            {
                indicator.Visibility = Visibility.Collapsed;
                percentage.Text = "—";
            }

            public void SetRemaining(int remainingPercent)
            {
                indicator.Fill = remainingPercent >= 50
                    ? GreenBrush
                    : remainingPercent >= 20 ? YellowBrush : RedBrush;
                indicator.Visibility = Visibility.Visible;
                percentage.Text = remainingPercent + "%";
            }
        }

        private static string FormatDurationLabel(long minutes)
        {
            if (minutes < 1440 && minutes % 60 == 0)
            {
                return (minutes / 60) + "h";
            }
            if (minutes == 10080)
            {
                return "Week";
            }
            if (minutes >= 1440 && minutes % 1440 == 0)
            {
                return (minutes / 1440) + "d";
            }
            return "—";
        }

        private static string FormatRemaining(long unixSeconds)
        {
            DateTimeOffset reset = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
            TimeSpan remaining = reset - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                return "0m";
            }

            long totalMinutes = (long)Math.Ceiling(remaining.TotalMinutes);
            long days = totalMinutes / 1440;
            long hours = (totalMinutes % 1440) / 60;
            long minutes = totalMinutes % 60;

            if (days > 0)
            {
                return days + "d " + hours + "h";
            }
            if (hours > 0)
            {
                return hours + "h " + minutes + "m";
            }
            return minutes + "m";
        }

        private void OnWindowMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed && !IsWindowControlSource(e.OriginalSource as DependencyObject))
            {
                DragMove();
            }
        }

        private void OnTitleMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                e.Handled = true;
                ShowAbout();
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F1)
            {
                e.Handled = true;
                ShowAbout();
            }
        }

        internal void ShowAbout()
        {
            string message =
                "Program Name: " + ProgramInfo.Name + "\n" +
                "Version: v" + ProgramInfo.Version + "\n" +
                "License: MIT\n" +
                "Data Source: Official codex app-server\n" +
                "Program Requests: account/rateLimits/read\n" +
                "No Telemetry: Yes\n" +
                "No Analytics: Yes\n" +
                "No Cookie Access: Yes\n" +
                "No Chat Access: Yes\n" +
                "Program Description: Local, read-only Codex Usage viewer.";

            if (IsVisible)
            {
                MessageBox.Show(this, message, "About", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(message, "About", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private bool IsWindowControlSource(DependencyObject source)
        {
            while (source != null)
            {
                if (ReferenceEquals(source, refreshButton) || ReferenceEquals(source, closeButton))
                {
                    return true;
                }
                source = VisualTreeHelper.GetParent(source);
            }
            return false;
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!exitRequested)
            {
                e.Cancel = true;
                HideToTray();
                return;
            }
        }

        private void HideToTray()
        {
            WindowStateStore.Save(this);
            Hide();
        }
    }
}
