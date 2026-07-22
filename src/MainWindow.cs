using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Win32;
using CodexUsageViewer.Usage;

namespace CodexUsageViewer
{
    internal sealed class MainWindow : Window
    {
        private const int WsExNoActivate = 0x08000000;
        private readonly UsageService usageService;
        private readonly Border shortPane;
        private readonly Border longPane;
        private readonly TextBlock shortPercent;
        private readonly TextBlock longPercent;
        private readonly TextBlock shortReset;
        private readonly TextBlock longReset;
        private readonly TextBlock statusText;
        private readonly TextBlock refreshText;
        private TextBlock shortLabel;
        private TextBlock longLabel;
        private TextBlock resetTitle;
        private readonly System.Windows.Shapes.Ellipse freshnessDot;
        private readonly Grid expandedDetails;
        private readonly FrameworkElement visualRoot;
        private readonly DispatcherTimer autoRefreshTimer;
        private readonly DispatcherTimer relativeTimeTimer;
        private readonly DispatcherTimer collapseTimer;
        private readonly DispatcherTimer geometryTimer;
        private readonly DispatcherTimer idleTimer;
        private readonly DispatcherTimer fullScreenTimer;
        private readonly DispatcherTimer singleClickTimer;
        private readonly DispatcherTimer hintTimer;
        private CancellationTokenSource refreshCancellation;
        private CachedUsage current;
        private string currentError;
        private UsageError? currentErrorCode;
        private bool exitRequested;
        private bool expanded;
        private bool dragging;
        private bool idle;
        private Point mouseDownScreen;
        private Point windowDown;
        private double animationCenter;
        private double animationStartWidth;
        private double animationTargetWidth;
        private int animationDuration;
        private double animationEdgePosition;
        private readonly Stopwatch geometryClock = new Stopwatch();
        private FrameworkElement normalRoot;
        private Grid collapsedRoot;
        private readonly AppWindowState savedState;
        private bool windowCollapsed;
        private bool hiddenByFullScreen;
        private string lastFullScreenDiagnostic;
        private HwndSource hwndSource;
        private bool nativeLeftDown;
        private Point pendingClickScreen;
        private ToolTip firstUseToolTip;

        public event EventHandler DisplayChanged;
        public MainWindow(UsageService usageService)
        {
            this.usageService = usageService;
            Title = ProgramInfo.Name + " " + ProgramInfo.Version;
            savedState = WindowStateStore.Load();
            savedState.HalfHidden = false;
            Localization.SetPreference(savedState.Language);
            Width = AppSettings.CompactWindowWidth;
            Height = AppSettings.WindowHeight;
            MinWidth = AppSettings.CollapsedHitSize;
            MinHeight = AppSettings.CollapsedHitSize;
            MaxWidth = AppSettings.ExpandedWindowWidth;
            MaxHeight = AppSettings.WindowHeight;
            Topmost = savedState.Topmost;
            ShowInTaskbar = false;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            UseLayoutRounding = true;
            SnapsToDevicePixels = true;
            FontFamily = new FontFamily("Segoe UI");
            WindowStartupLocation = WindowStartupLocation.Manual;

            shortPercent = ValueText("--%");
            longPercent = ValueText("--%");
            shortReset = DetailText("--");
            longReset = DetailText("--");
            statusText = DetailText(Localization.Get("NoData"));
            refreshText = new TextBlock { Text = "", FontSize = 11, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 0, 0) };
            freshnessDot = new System.Windows.Shapes.Ellipse { Width = 7, Height = 7, Fill = new SolidColorBrush(AppSettings.UnknownColor), VerticalAlignment = VerticalAlignment.Center };

            shortPane = CreatePane(Localization.Get("FiveHour"), shortPercent, true);
            longPane = CreatePane(Localization.Get("Weekly"), longPercent, false);
            expandedDetails = CreateExpandedDetails();
            visualRoot = (FrameworkElement)CreateRoot();
            Content = visualRoot;
            if (!double.IsNaN(savedState.Left)) { Left = savedState.Left; Top = savedState.Top; }

            autoRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(AppSettings.RefreshIntervalSeconds) };
            autoRefreshTimer.Tick += async delegate { await RefreshAsync("automatic"); };
            relativeTimeTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
            relativeTimeTimer.Tick += delegate { UpdateFreshness(); };
            collapseTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(AppSettings.CollapseDelayMilliseconds) };
            collapseTimer.Tick += delegate { collapseTimer.Stop(); if (!IsMouseOver) SetExpanded(false); };
            geometryTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };
            geometryTimer.Tick += OnGeometryAnimationTick;
            idleTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(AppSettings.IdleDelayMilliseconds) };
            idleTimer.Tick += delegate { idleTimer.Stop(); if (!IsMouseOver && !dragging) SetIdle(true); };
            fullScreenTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(AppSettings.FullScreenCheckMilliseconds) };
            fullScreenTimer.Tick += OnFullScreenCheck;
            singleClickTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(NativeMethods.GetDoubleClickTime()) };
            singleClickTimer.Tick += async delegate { singleClickTimer.Stop(); await RunOriginalSingleClickAsync(); };
            hintTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            hintTimer.Tick += delegate { hintTimer.Stop(); if (firstUseToolTip != null) firstUseToolTip.IsOpen = false; };

            Loaded += OnLoaded;
            SourceInitialized += OnSourceInitialized;
            Closing += OnClosing;
            MouseEnter += delegate { idleTimer.Stop(); SetIdle(false); if (windowCollapsed) SetCollapsedOpacity(AppSettings.CollapsedHoverOpacity); else { collapseTimer.Stop(); SetExpanded(true); } };
            MouseLeave += delegate { if (windowCollapsed) SetCollapsedOpacity(savedState.CollapsedOpacity); else { collapseTimer.Stop(); collapseTimer.Start(); RestartIdleTimer(); } };
            ContextMenu = CreateContextMenu();
            Localization.Changed += OnLanguageChanged;
            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        }

        public string TrayText
        {
            get
            {
                if (current == null) return currentError ?? Localization.Get("NoData");
                return Localization.Get("FiveHour") + " " + FormatPercent(current.ShortRemaining) + "  " + Localization.Get("Weekly") + " " + FormatPercent(current.LongRemaining) + (string.IsNullOrEmpty(currentError) ? "" : " · " + Localization.Get("DataNotUpdated"));
            }
        }

        private UIElement CreateRoot()
        {
            Grid shell = new Grid { Margin = new Thickness(AppSettings.ShellPadding), UseLayoutRounding = true, SnapsToDevicePixels = true };
            Border shadow = new Border
            {
                CornerRadius = new CornerRadius(AppSettings.PillCornerRadius),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Effect = new DropShadowEffect { BlurRadius = AppSettings.ShadowBlurRadius, ShadowDepth = AppSettings.ShadowDepth, Opacity = AppSettings.ShadowOpacity, Color = Colors.Black }
            };
            Grid pill = new Grid { ClipToBounds = true, Height = AppSettings.ContentHeight, Background = Brushes.Transparent, UseLayoutRounding = true, SnapsToDevicePixels = true };
            pill.Children.Add(expandedDetails);
            shortPane.HorizontalAlignment = HorizontalAlignment.Left;
            longPane.HorizontalAlignment = HorizontalAlignment.Right;
            Panel.SetZIndex(shortPane, 2); Panel.SetZIndex(longPane, 2);
            pill.Children.Add(shortPane); pill.Children.Add(longPane);
            shadow.Child = pill;
            shell.Children.Add(shadow);
            normalRoot = shell;

            Grid root = new Grid { Background = Brushes.Transparent };
            root.Children.Add(new Border { Background = Brushes.Transparent, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch });
            root.Children.Add(shell);
            collapsedRoot = new Grid { Width = AppSettings.CollapsedVisualSize, Height = AppSettings.CollapsedVisualSize, Visibility = Visibility.Collapsed, ClipToBounds = true };
            collapsedRoot.Clip = new EllipseGeometry(new Point(AppSettings.CollapsedVisualSize / 2, AppSettings.CollapsedVisualSize / 2), AppSettings.CollapsedVisualSize / 2, AppSettings.CollapsedVisualSize / 2);
            Grid dotColors = new Grid();
            dotColors.ColumnDefinitions.Add(new ColumnDefinition()); dotColors.ColumnDefinitions.Add(new ColumnDefinition());
            Border left = new Border { Background = shortPane.Background }; Border right = new Border { Background = longPane.Background };
            Grid.SetColumn(right, 1); dotColors.Children.Add(left); dotColors.Children.Add(right);
            collapsedRoot.Children.Add(dotColors); collapsedRoot.Tag = new Border[] { left, right };
            root.Children.Add(collapsedRoot);
            return root;
        }

        private Border CreatePane(string label, TextBlock value, bool left)
        {
            StackPanel compact = new StackPanel { Orientation = Orientation.Vertical, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            TextBlock labelBlock = new TextBlock { Text = label, FontWeight = FontWeights.Normal, FontSize = AppSettings.LabelFontSize, HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, -1) };
            if (left) shortLabel = labelBlock; else longLabel = labelBlock;
            compact.Children.Add(labelBlock);
            compact.Children.Add(value);
            Border pane = new Border
            {
                CornerRadius = left ? new CornerRadius(AppSettings.PillCornerRadius, 0, 0, AppSettings.PillCornerRadius) : new CornerRadius(0, AppSettings.PillCornerRadius, AppSettings.PillCornerRadius, 0),
                Background = new SolidColorBrush(AppSettings.UnknownColor),
                Child = compact,
                Width = AppSettings.ColorPaneWidth,
                Height = AppSettings.ContentHeight,
                BorderThickness = new Thickness(0)
            };
            return pane;
        }

        private Grid CreateExpandedDetails()
        {
            Grid grid = new Grid { Width = AppSettings.DetailWidth, Height = AppSettings.ContentHeight, Opacity = 0, HorizontalAlignment = HorizontalAlignment.Center, Background = new SolidColorBrush(Color.FromRgb(250, 250, 248)) };
            double centerWidth = Math.Ceiling(MeasureStatusWidth()) + 8;
            double sideWidth = Math.Floor((AppSettings.DetailWidth - centerWidth) / 2.0);
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(sideWidth) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(AppSettings.DetailWidth - sideWidth * 2) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(sideWidth) });

            AddResetColumn(grid, shortReset, 0);
            AddResetColumn(grid, longReset, 2);

            resetTitle = new TextBlock { Text = Localization.Get("Resets"), FontSize = AppSettings.ResetFontSize, FontStretch = FontStretches.Condensed, Foreground = new SolidColorBrush(Color.FromRgb(105, 108, 112)), HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center, LineHeight = 12, Margin = new Thickness(0, 0, 0, -1) };
            statusText.FontSize = AppSettings.ResetFontSize; statusText.FontStretch = FontStretches.Condensed; statusText.FontWeight = FontWeights.Light; statusText.Margin = new Thickness(0); statusText.TextAlignment = TextAlignment.Center; statusText.TextWrapping = TextWrapping.NoWrap; statusText.LineHeight = 12;
            StackPanel common = new StackPanel { Orientation = Orientation.Vertical, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            common.Children.Add(resetTitle); common.Children.Add(statusText);
            Grid.SetColumn(common, 1); grid.Children.Add(common);
            return grid;
        }

        private static double MeasureStatusWidth()
        {
            Typeface typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Light, FontStretches.Condensed);
            string[] values = { Localization.Get("UpdatedJustNow"), Localization.Get("Refreshing"), Localization.Format("UpdatedMinutesAgo", 59), Localization.Get("RefreshFailed"), Localization.Get("NetworkFailed"), Localization.Get("NotSignedIn") };
            double maximum = 0;
            foreach (string value in values)
            {
                FormattedText formatted = new FormattedText(value, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, AppSettings.ResetFontSize, Brushes.Black, 1.0);
                maximum = Math.Max(maximum, formatted.WidthIncludingTrailingWhitespace);
            }
            return maximum;
        }

        private static void AddResetColumn(Grid grid, TextBlock resetDate, int column)
        {
            TextBlock resetTime = DetailText("--");
            resetTime.FontWeight = FontWeights.SemiBold;
            resetDate.Tag = resetTime;
            resetDate.Margin = new Thickness(0, 0, 0, -1);
            resetTime.Margin = new Thickness(0);
            StackPanel group = new StackPanel { Orientation = Orientation.Vertical, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            group.Children.Add(resetDate); group.Children.Add(resetTime);
            Grid.SetColumn(group, column); grid.Children.Add(group);
        }

        private static TextBlock ValueText(string text) { return new TextBlock { Text = text, FontSize = AppSettings.PercentFontSize, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Center, LineHeight = 19 }; }
        private static TextBlock DetailText(string text) { return new TextBlock { Text = text, FontSize = AppSettings.DetailFontSize, Foreground = new SolidColorBrush(Color.FromRgb(92, 95, 99)), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(1, 0, 1, 0), LineHeight = 12 }; }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            current = UsageCache.Load();
            if (current != null) ApplyCurrent();
            if (double.IsNaN(Left)) PlaceBottomRight(); else EnsureOnScreen();
            SetWindowCollapsed(savedState.Collapsed, false);
            autoRefreshTimer.Start(); relativeTimeTimer.Start(); fullScreenTimer.Start();
            if (!IsMouseOver) RestartIdleTimer();
            if (!savedState.HintShown) ShowFirstUseHint();
            await RefreshAsync("startup");
        }

        private void OnSourceInitialized(object sender, EventArgs e)
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;
            int style = NativeMethods.GetWindowLong(handle, -20);
            NativeMethods.SetWindowLong(handle, -20, style | WsExNoActivate);
            hwndSource = HwndSource.FromHwnd(handle);
            if (hwndSource != null) hwndSource.AddHook(WindowMessageHook);
        }

        private IntPtr WindowMessageHook(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WmMouseMove = 0x0200, WmLeftDown = 0x0201, WmLeftUp = 0x0202, WmRightUp = 0x0205, WmMouseLeave = 0x02A3;
            if (message != WmMouseMove && message != WmLeftDown && message != WmLeftUp && message != WmRightUp && message != WmMouseLeave) return IntPtr.Zero;
            NativeMethods.NativePoint point = NativeMethods.ClientPointToScreen(hwnd, lParam);
            if (message == WmMouseLeave)
            {
                if (windowCollapsed) SetCollapsedOpacity(savedState.CollapsedOpacity); else { collapseTimer.Stop(); collapseTimer.Start(); RestartIdleTimer(); }
            }
            else if (message == WmMouseMove && !nativeLeftDown)
            {
                idleTimer.Stop(); SetIdle(false); NativeMethods.TrackMouseLeave(hwnd);
                if (windowCollapsed) SetCollapsedOpacity(AppSettings.CollapsedHoverOpacity); else { collapseTimer.Stop(); SetExpanded(true); }
            }
            else if (message == WmRightUp)
            {
                if (ContextMenu != null) { ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint; ContextMenu.IsOpen = true; }
                AppLogger.Info("Context menu opened; collapsed=" + windowCollapsed); handled = true;
            }
            else if (message == WmLeftDown)
            {
                nativeLeftDown = true; dragging = false; idleTimer.Stop(); SetIdle(false);
                mouseDownScreen = new Point(point.X, point.Y); windowDown = new Point(Left, Top); NativeMethods.SetCapture(hwnd);
                AppLogger.Info("Native mouse down; collapsed=" + windowCollapsed + "; screen=" + point.X + "," + point.Y); handled = true;
            }
            else if (message == WmMouseMove && nativeLeftDown)
            {
                Vector physical = new Point(point.X, point.Y) - mouseDownScreen;
                Vector delta = hwndSource == null || hwndSource.CompositionTarget == null ? physical : hwndSource.CompositionTarget.TransformFromDevice.Transform(physical);
                if (!dragging && Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y) > AppSettings.DragThreshold)
                {
                    singleClickTimer.Stop();
                    dragging = true; collapseTimer.Stop(); geometryTimer.Stop(); geometryClock.Stop();
                    if (!windowCollapsed) { expanded = false; expandedDetails.BeginAnimation(UIElement.OpacityProperty, null); Width = AppSettings.CompactWindowWidth; expandedDetails.Opacity = 0; }
                    windowDown = new Point(Left - delta.X, Top - delta.Y);
                }
                if (dragging) { Left = windowDown.X + delta.X; Top = windowDown.Y + delta.Y; }
                handled = true;
            }
            else if (message == WmLeftUp && nativeLeftDown)
            {
                nativeLeftDown = false; NativeMethods.ReleaseCapture();
                AppLogger.Info("Native mouse up; dragging=" + dragging + "; collapsed=" + windowCollapsed);
                if (dragging) { singleClickTimer.Stop(); SnapToNearestEdge(); SaveWindowState(); }
                else if (windowCollapsed) { singleClickTimer.Stop(); SetWindowCollapsed(false, true); }
                else if (singleClickTimer.IsEnabled && Math.Abs(point.X - pendingClickScreen.X) <= NativeMethods.GetSystemMetrics(36) && Math.Abs(point.Y - pendingClickScreen.Y) <= NativeMethods.GetSystemMetrics(37))
                {
                    singleClickTimer.Stop(); SetWindowCollapsed(true, true); AppLogger.Info("Complete widget double-click confirmed; folded to dot");
                }
                else
                {
                    pendingClickScreen = new Point(point.X, point.Y); singleClickTimer.Stop(); singleClickTimer.Start(); AppLogger.Info("Complete widget single-click pending");
                }
                dragging = false; if (!IsMouseOver) RestartIdleTimer(); handled = true;
            }
            return IntPtr.Zero;
        }

        internal async Task RefreshFromTray() { idleTimer.Stop(); SetIdle(false); await RefreshAsync("tray"); if (!IsMouseOver) RestartIdleTimer(); }
        internal void ShowFromTray() { hiddenByFullScreen = false; idleTimer.Stop(); SetIdle(false); EnsureOnScreen(); if (!IsVisible) Show(); AppLogger.Info("Window restored from tray; collapsed=" + windowCollapsed); if (!IsMouseOver) RestartIdleTimer(); }
        internal void HideTemporarily() { hiddenByFullScreen = false; SaveWindowState(); AppLogger.Info("Window hidden to tray"); Hide(); }
        internal bool IsCollapsed { get { return windowCollapsed; } }
        internal bool IsAlwaysOnTop { get { return Topmost; } }
        internal bool IsAutoHideFullScreen { get { return savedState.AutoHideFullScreen; } }
        internal void ToggleCollapsed() { SetWindowCollapsed(!windowCollapsed, true); }
        internal void ToggleTopmost() { Topmost = !Topmost; savedState.Topmost = Topmost; SaveWindowState(); }
        internal void ToggleAutoHideFullScreen() { savedState.AutoHideFullScreen = !savedState.AutoHideFullScreen; SaveWindowState(); }
        internal void RequestExit() { exitRequested = true; autoRefreshTimer.Stop(); relativeTimeTimer.Stop(); collapseTimer.Stop(); geometryTimer.Stop(); idleTimer.Stop(); fullScreenTimer.Stop(); singleClickTimer.Stop(); hintTimer.Stop(); if (refreshCancellation != null) refreshCancellation.Cancel(); SaveWindowState(); Close(); }

        private void ShowFirstUseHint()
        {
            firstUseToolTip = new ToolTip { Content = Localization.Get("DoubleClickHint") + Environment.NewLine + Localization.Get("DotClickHint"), Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom, PlacementTarget = visualRoot };
            firstUseToolTip.IsOpen = true; hintTimer.Start(); savedState.HintShown = true; SaveWindowState();
        }

        private async Task RunOriginalSingleClickAsync()
        {
            if (windowCollapsed || dragging) return;
            AppLogger.Info("Complete widget single-click confirmed; showing update information");
            SetExpanded(true);
            await RefreshAsync("click");
            await Task.Delay(1200);
            if (!windowCollapsed && !IsMouseOver) SetExpanded(false);
        }

        private async Task RefreshAsync(string source)
        {
            if (refreshCancellation != null) { AppLogger.Info("Refresh skipped; request already active (" + source + ")"); return; }
            refreshCancellation = new CancellationTokenSource();
            refreshText.Text = Localization.Get("Refreshing") + " · "; currentError = null; currentErrorCode = null; UpdateFreshness();
            AppLogger.Info("Refresh started (" + source + ")");
            try
            {
                UsageSnapshot snapshot = await usageService.RefreshAsync(refreshCancellation.Token);
                current = ToCached(snapshot, DateTimeOffset.Now);
                UsageCache.Save(current);
                currentError = null;
                currentErrorCode = null;
                ApplyCurrent();
                AppLogger.Info("Refresh succeeded");
            }
            catch (OperationCanceledException) { AppLogger.Info("Refresh cancelled"); }
            catch (UsageException exception) { currentErrorCode = exception.Error; currentError = exception.UserMessage; AppLogger.Error("Refresh failed: " + exception.Error, exception); }
            catch (Exception exception) { currentError = Localization.Get("CannotReadUsage"); AppLogger.Error("Unexpected refresh failure", exception); }
            finally
            {
                refreshText.Text = string.Empty;
                refreshCancellation.Dispose(); refreshCancellation = null;
                UpdateFreshness(); RaiseDisplayChanged();
            }
        }

        internal static CachedUsage ToCached(UsageSnapshot snapshot, DateTimeOffset now)
        {
            int? shortRemaining = snapshot.ShortWindow == null ? (int?)null : Math.Max(0, Math.Min(100, 100 - snapshot.ShortWindow.UsedPercent));
            int? longRemaining = snapshot.LongWindow == null ? (int?)null : Math.Max(0, Math.Min(100, 100 - snapshot.LongWindow.UsedPercent));
            return new CachedUsage(shortRemaining, snapshot.ShortWindow == null ? (long?)null : snapshot.ShortWindow.ResetsAt, longRemaining, snapshot.LongWindow == null ? (long?)null : snapshot.LongWindow.ResetsAt, now);
        }

        private void ApplyCurrent()
        {
            ApplyPane(shortPane, shortPercent, current.ShortRemaining);
            ApplyPane(longPane, longPercent, current.LongRemaining);
            Border[] dots = collapsedRoot.Tag as Border[];
            if (dots != null) { dots[0].Background = shortPane.Background; dots[1].Background = longPane.Background; }
            ApplyReset(shortReset, current.ShortResetUnixSeconds);
            ApplyReset(longReset, current.LongResetUnixSeconds);
            UpdateFreshness(); RaiseDisplayChanged();
        }

        private static void ApplyPane(Border pane, TextBlock text, int? remaining)
        {
            Color color = AppSettings.ColorForRemaining(remaining);
            Brush foreground = AppSettings.TextBrushFor(color);
            pane.Background = new SolidColorBrush(color); text.Foreground = foreground; text.Text = FormatPercent(remaining);
            TextBlock detail = text.Tag as TextBlock; if (detail != null) detail.Text = remaining.HasValue ? remaining.Value + "%" : Localization.Get("NoData");
            StackPanel compact = pane.Child as StackPanel; if (compact != null) foreach (UIElement item in compact.Children) { TextBlock block = item as TextBlock; if (block != null) block.Foreground = foreground; }
        }

        private void UpdateFreshness()
        {
            if (refreshCancellation != null)
            {
                freshnessDot.Fill = new SolidColorBrush(AppSettings.UnknownColor);
                statusText.Text = Localization.Get("Refreshing");
                return;
            }
            if (current == null)
            {
                freshnessDot.Fill = new SolidColorBrush(AppSettings.UnknownColor);
                statusText.Text = string.IsNullOrEmpty(currentError) ? Localization.Get("NoData") : currentError;
                return;
            }
            TimeSpan age = DateTimeOffset.Now - current.SuccessAt;
            if (!string.IsNullOrEmpty(currentError)) { freshnessDot.Fill = new SolidColorBrush(AppSettings.WarningColor); statusText.Text = ShortErrorStatus(currentError); }
            else { freshnessDot.Fill = new SolidColorBrush(age.TotalMinutes >= AppSettings.StaleMinutes ? AppSettings.LowColor : AppSettings.GoodColor); statusText.Text = RelativeAge(age); }
        }

        private static string ShortErrorStatus(string error)
        {
            return Localization.Get("RefreshFailed");
        }

        private static string RelativeAge(TimeSpan age)
        {
            if (age.TotalMinutes < 1) return Localization.Get("UpdatedJustNow");
            if (age.TotalHours < 1) return Localization.Format("UpdatedMinutesAgo", (int)age.TotalMinutes);
            if (age.TotalHours < 24) return Localization.Format("UpdatedHoursAgo", (int)age.TotalHours);
            return Localization.Format("UpdatedDaysAgo", (int)age.TotalDays);
        }

        private static string FormatPercent(int? value) { return value.HasValue ? value.Value + "%" : "--%"; }
        private static void ApplyReset(TextBlock dateBlock, long? unix)
        {
            TextBlock timeBlock = dateBlock.Tag as TextBlock;
            string[] parts = FormatResetParts(unix);
            dateBlock.Text = parts[0];
            if (timeBlock != null) timeBlock.Text = parts[1];
        }

        internal static string[] FormatResetParts(long? unix)
        {
            if (!unix.HasValue || unix.Value <= 0) return new[] { "--", "--" };
            try
            {
                DateTime local = DateTimeOffset.FromUnixTimeSeconds(unix.Value).ToLocalTime().DateTime;
                return new[] { local.ToString("MM.dd", CultureInfo.InvariantCulture), local.ToString("HH:mm", CultureInfo.InvariantCulture) };
            }
            catch (ArgumentOutOfRangeException)
            {
                return new[] { "--", "--" };
            }
        }

        private void SetExpanded(bool value)
        {
            if (expanded == value) return;
            expanded = value;
            double center = Left + Width / 2.0;
            double targetWidth = value ? AppSettings.ExpandedWindowWidth : AppSettings.CompactWindowWidth;
            int duration = value ? AppSettings.ExpandAnimationMilliseconds : AppSettings.CollapseAnimationMilliseconds;
            IEasingFunction easing = value ? (IEasingFunction)new QuadraticEase { EasingMode = EasingMode.EaseOut } : new QuadraticEase { EasingMode = EasingMode.EaseIn };
            Rect area = WindowStateStore.GetBestWorkingArea(new Rect(Left, Top, Width, Height));
            animationEdgePosition = savedState.Edge == DockEdge.Left ? area.Left : area.Right;
            animationCenter = center; animationStartWidth = ActualWidth; animationTargetWidth = targetWidth; animationDuration = duration;
            geometryClock.Restart(); geometryTimer.Start();
            DoubleAnimation detailAnimation = new DoubleAnimation
            {
                To = value ? 1 : 0,
                Duration = TimeSpan.FromMilliseconds(value ? 55 : 45),
                BeginTime = value ? TimeSpan.FromMilliseconds(10) : TimeSpan.Zero,
                EasingFunction = easing,
                FillBehavior = FillBehavior.HoldEnd
            };
            expandedDetails.BeginAnimation(UIElement.OpacityProperty, detailAnimation);
        }

        private void OnGeometryAnimationTick(object sender, EventArgs e)
        {
            double progress = Math.Min(1.0, geometryClock.Elapsed.TotalMilliseconds / animationDuration);
            double eased = expanded ? 1.0 - Math.Pow(1.0 - progress, 2) : progress * progress;
            double width = animationStartWidth + (animationTargetWidth - animationStartWidth) * eased;
            Width = width; Left = savedState.Edge == DockEdge.Left ? animationEdgePosition : animationEdgePosition - width;
            if (progress >= 1.0)
            {
                geometryTimer.Stop(); geometryClock.Stop(); Width = animationTargetWidth; Left = savedState.Edge == DockEdge.Left ? animationEdgePosition : animationEdgePosition - Width;
                expandedDetails.BeginAnimation(UIElement.OpacityProperty, null);
                expandedDetails.Opacity = expanded ? 1 : 0;
            }
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            AppLogger.Info("Mouse down; collapsed=" + windowCollapsed + "; position=" + e.GetPosition(this));
            idleTimer.Stop(); SetIdle(false);
            mouseDownScreen = PointToScreen(e.GetPosition(this)); windowDown = new Point(Left, Top); dragging = false; CaptureMouse(); e.Handled = true;
        }
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || !IsMouseCaptured) { return; }
            Point now = PointToScreen(e.GetPosition(this)); Vector delta = now - mouseDownScreen;
            if (!dragging && Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y) > AppSettings.DragThreshold)
            {
                dragging = true; collapseTimer.Stop(); geometryTimer.Stop(); geometryClock.Stop();
                if (!windowCollapsed) { expanded = false; expandedDetails.BeginAnimation(UIElement.OpacityProperty, null); Width = AppSettings.CompactWindowWidth; expandedDetails.Opacity = 0; }
                windowDown = new Point(Left - delta.X, Top - delta.Y);
            }
            if (dragging) { Left = windowDown.X + delta.X; Top = windowDown.Y + delta.Y; }
        }
        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!IsMouseCaptured) return; ReleaseMouseCapture();
            AppLogger.Info("Mouse up; dragging=" + dragging + "; collapsed=" + windowCollapsed);
            if (dragging) { SnapToNearestEdge(); SaveWindowState(); }
            else
            {
                SetWindowCollapsed(!windowCollapsed, true);
            }
            dragging = false;
            if (!IsMouseOver) RestartIdleTimer();
            e.Handled = true;
        }

        private void RestartIdleTimer()
        {
            idleTimer.Stop();
            if (!IsMouseOver && !dragging) idleTimer.Start();
        }

        private void SetIdle(bool value)
        {
            if (idle == value) return;
            idle = value;
            DoubleAnimation animation = new DoubleAnimation
            {
                To = value ? AppSettings.IdleOpacity : 1.0,
                Duration = TimeSpan.FromMilliseconds(AppSettings.IdleFadeMilliseconds),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
                FillBehavior = FillBehavior.HoldEnd
            };
            visualRoot.BeginAnimation(UIElement.OpacityProperty, animation);
        }

        private ContextMenu CreateContextMenu()
        {
            ContextMenu menu = new ContextMenu();
            MenuItem collapse = new MenuItem { Header = windowCollapsed ? Localization.Get("Expand") : Localization.Get("Collapse") };
            collapse.Click += delegate { ToggleCollapsed(); };
            menu.Items.Add(collapse);
            MenuItem hide = new MenuItem { Header = Localization.Get("HideToTray") }; hide.Click += delegate { HideTemporarily(); }; menu.Items.Add(hide);
            MenuItem top = new MenuItem { Header = Localization.Get("AlwaysOnTop"), IsCheckable = true, IsChecked = Topmost }; top.Click += delegate { Topmost = top.IsChecked; savedState.Topmost = Topmost; SaveWindowState(); }; menu.Items.Add(top);
            MenuItem fullScreen = new MenuItem { Header = Localization.Get("AutoHideFullscreen"), IsCheckable = true, IsChecked = savedState.AutoHideFullScreen }; fullScreen.Click += delegate { savedState.AutoHideFullScreen = fullScreen.IsChecked; SaveWindowState(); }; menu.Items.Add(fullScreen);
            MenuItem refresh = new MenuItem { Header = Localization.Get("RefreshNow") }; refresh.Click += async delegate { await RefreshFromTray(); }; menu.Items.Add(refresh);
            MenuItem language = new MenuItem { Header = Localization.Get("Language") };
            AddLanguageChoice(language, Localization.Get("FollowSystem"), Localization.SystemLanguage);
            AddLanguageChoice(language, Localization.Get("SimplifiedChinese"), Localization.ChineseLanguage);
            AddLanguageChoice(language, Localization.Get("English"), Localization.EnglishLanguage);
            menu.Items.Add(language);
            menu.Items.Add(new Separator());
            MenuItem exit = new MenuItem { Header = Localization.Get("Exit") }; exit.Click += delegate { RequestExit(); Application.Current.Shutdown(); }; menu.Items.Add(exit);
            menu.Opened += delegate { collapse.Header = windowCollapsed ? Localization.Get("Expand") : Localization.Get("Collapse"); top.IsChecked = Topmost; fullScreen.IsChecked = savedState.AutoHideFullScreen; };
            return menu;
        }

        private void AddLanguageChoice(MenuItem parent, string header, string language)
        {
            MenuItem item = new MenuItem { Header = header, IsCheckable = true, IsChecked = Localization.Preference == language };
            item.Click += delegate { SetLanguage(language); };
            parent.Items.Add(item);
        }

        internal void SetLanguage(string language)
        {
            savedState.Language = language; Localization.SetPreference(language); SaveWindowState();
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            if (currentErrorCode.HasValue) currentError = UsageException.MessageFor(currentErrorCode.Value);
            if (shortLabel != null) shortLabel.Text = Localization.Get("FiveHour");
            if (longLabel != null) longLabel.Text = Localization.Get("Weekly");
            if (resetTitle != null) resetTitle.Text = Localization.Get("Resets");
            if (firstUseToolTip != null && firstUseToolTip.IsOpen) firstUseToolTip.Content = Localization.Get("DoubleClickHint") + Environment.NewLine + Localization.Get("DotClickHint");
            ContextMenu = CreateContextMenu(); UpdateFreshness(); RaiseDisplayChanged();
        }

        private void SetWindowCollapsed(bool value, bool save)
        {
            geometryTimer.Stop(); geometryClock.Stop(); collapseTimer.Stop();
            double oldLeft = Left, oldTop = Top, oldWidth = ActualWidth > 0 ? ActualWidth : Width;
            Rect area = WindowStateStore.GetBestWorkingArea(new Rect(oldLeft, oldTop, oldWidth, Height));
            DockEdge edge = Math.Abs((oldLeft + oldWidth / 2) - area.Left) <= Math.Abs(area.Right - (oldLeft + oldWidth / 2)) ? DockEdge.Left : DockEdge.Right;
            if (!value && windowCollapsed) edge = savedState.Edge;
            windowCollapsed = value; savedState.Collapsed = value; savedState.Edge = edge;
            expanded = false; expandedDetails.BeginAnimation(UIElement.OpacityProperty, null); expandedDetails.Opacity = 0;
            normalRoot.Visibility = value ? Visibility.Collapsed : Visibility.Visible;
            collapsedRoot.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            MaxWidth = value ? AppSettings.CollapsedHitSize : AppSettings.ExpandedWindowWidth;
            MaxHeight = value ? AppSettings.CollapsedHitSize : AppSettings.WindowHeight;
            Width = value ? AppSettings.CollapsedHitSize : AppSettings.CompactWindowWidth;
            Height = value ? AppSettings.CollapsedHitSize : AppSettings.WindowHeight;
            Top = Math.Max(area.Top, Math.Min(oldTop, area.Bottom - Height));
            if (edge == DockEdge.Left) Left = area.Left - (value && savedState.HalfHidden ? AppSettings.CollapsedHiddenAmount : 0);
            else Left = area.Right - Width + (value && savedState.HalfHidden ? AppSettings.CollapsedHiddenAmount : 0);
            if (value) SetCollapsedOpacity(IsMouseOver ? AppSettings.CollapsedHoverOpacity : savedState.CollapsedOpacity); else { visualRoot.BeginAnimation(UIElement.OpacityProperty, null); visualRoot.Opacity = 1; }
            if (save) SaveWindowState();
            AppLogger.Info("Window mode changed; collapsed=" + value + "; bounds=" + FormatBounds() + "; edge=" + savedState.Edge);
        }

        private void SetCollapsedOpacity(double value)
        {
            visualRoot.BeginAnimation(UIElement.OpacityProperty, null); visualRoot.Opacity = value; idle = false;
        }

        private void SnapToNearestEdge()
        {
            Rect area = WindowStateStore.GetBestWorkingArea(new Rect(Left, Top, Width, Height));
            savedState.Edge = Left + Width / 2 <= area.Left + area.Width / 2 ? DockEdge.Left : DockEdge.Right;
            Top = Math.Max(area.Top, Math.Min(Top, area.Bottom - Height));
            double hidden = windowCollapsed && savedState.HalfHidden ? AppSettings.CollapsedHiddenAmount : 0;
            Left = savedState.Edge == DockEdge.Left ? area.Left - hidden : area.Right - Width + hidden;
            AppLogger.Info("Window snapped; bounds=" + FormatBounds() + "; edge=" + savedState.Edge);
        }

        private string FormatBounds() { return Math.Round(Left, 1) + "," + Math.Round(Top, 1) + " " + Math.Round(Width, 1) + "x" + Math.Round(Height, 1); }

        private void SaveWindowState()
        {
            savedState.Left = Left; savedState.Top = Top; savedState.Collapsed = windowCollapsed; savedState.Topmost = Topmost;
            WindowStateStore.Save(savedState);
        }

        private void OnFullScreenCheck(object sender, EventArgs e)
        {
            if (!savedState.AutoHideFullScreen) return;
            string diagnostic;
            bool fullScreen = NativeMethods.IsForegroundFullScreen(new WindowInteropHelper(this).Handle, out diagnostic);
            if (diagnostic != lastFullScreenDiagnostic) { lastFullScreenDiagnostic = diagnostic; AppLogger.Info("Fullscreen check: " + diagnostic); }
            if (fullScreen && IsVisible && !hiddenByFullScreen) { hiddenByFullScreen = true; AppLogger.Info("Fullscreen detected; hiding window; collapsed=" + windowCollapsed); Hide(); }
            else if (!fullScreen && hiddenByFullScreen) { hiddenByFullScreen = false; Show(); AppLogger.Info("Fullscreen exited; restored window; collapsed=" + windowCollapsed + "; bounds=" + FormatBounds()); }
        }

        private void PlaceBottomRight()
        {
            Rect area = WindowStateStore.GetBestWorkingArea(new Rect(SystemParameters.WorkArea.Right - Width, SystemParameters.WorkArea.Bottom - Height, Width, Height));
            Left = area.Right - Width - AppSettings.ScreenMargin; Top = area.Bottom - Height - AppSettings.ScreenMargin;
        }
        private void EnsureOnScreen()
        {
            if (windowCollapsed) { SnapToNearestEdge(); return; }
            Rect fixedBounds = WindowStateStore.EnsureVisible(new Rect(Left, Top, Width, Height)); Left = fixedBounds.Left; Top = fixedBounds.Top;
        }
        private void RaiseDisplayChanged() { EventHandler handler = DisplayChanged; if (handler != null) handler(this, EventArgs.Empty); }
        private void OnDisplaySettingsChanged(object sender, EventArgs e) { Dispatcher.BeginInvoke(new Action(EnsureOnScreen)); }
        private void OnClosing(object sender, CancelEventArgs e) { if (!exitRequested) { e.Cancel = true; SaveWindowState(); Hide(); } else { SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged; Localization.Changed -= OnLanguageChanged; if (hwndSource != null) hwndSource.RemoveHook(WindowMessageHook); AppLogger.Info("Application exiting"); } }

        private static class NativeMethods
        {
            [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)] internal struct NativePoint { public int X, Y; }
            [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)] private struct TrackMouseEventData { public int Size; public uint Flags; public IntPtr HwndTrack; public uint HoverTime; }
            [System.Runtime.InteropServices.DllImport("user32.dll")] public static extern int GetWindowLong(IntPtr hWnd, int nIndex);
            [System.Runtime.InteropServices.DllImport("user32.dll")] public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int value);
            [System.Runtime.InteropServices.DllImport("user32.dll")] internal static extern uint GetDoubleClickTime();
            [System.Runtime.InteropServices.DllImport("user32.dll")] internal static extern int GetSystemMetrics(int index);
            [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern bool TrackMouseEvent(ref TrackMouseEventData data);
            [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
            [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern bool ClientToScreen(IntPtr hWnd, ref NativePoint point);
            [System.Runtime.InteropServices.DllImport("user32.dll")] internal static extern IntPtr SetCapture(IntPtr hWnd);
            [System.Runtime.InteropServices.DllImport("user32.dll")] internal static extern bool ReleaseCapture();
            [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out NativeRect rect);
            [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern bool IsIconic(IntPtr hWnd);
            [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr hWnd, uint flags);
            [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
            [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)] private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder text, int count);
            [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)] private struct NativeRect { public int Left, Top, Right, Bottom; }
            [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)] private struct MonitorInfo { public int Size; public NativeRect Monitor; public NativeRect Work; public uint Flags; }
            internal static NativePoint ClientPointToScreen(IntPtr hwnd, IntPtr lParam)
            {
                long packed = lParam.ToInt64(); NativePoint point = new NativePoint { X = unchecked((short)(packed & 0xffff)), Y = unchecked((short)((packed >> 16) & 0xffff)) };
                ClientToScreen(hwnd, ref point); return point;
            }
            internal static void TrackMouseLeave(IntPtr hwnd)
            {
                TrackMouseEventData data = new TrackMouseEventData { Size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(TrackMouseEventData)), Flags = 2, HwndTrack = hwnd };
                TrackMouseEvent(ref data);
            }
            public static bool IsForegroundFullScreen(IntPtr ownWindow, out string diagnostic)
            {
                IntPtr foreground = GetForegroundWindow();
                System.Text.StringBuilder classText = new System.Text.StringBuilder(128); GetClassName(foreground, classText, classText.Capacity);
                string identity = "handle=0x" + foreground.ToInt64().ToString("X") + "; class=" + classText;
                if (foreground == IntPtr.Zero || foreground == ownWindow || IsIconic(foreground) || classText.ToString() == "Progman" || classText.ToString() == "WorkerW" || classText.ToString() == "Shell_TrayWnd") { diagnostic = identity + "; fullscreen=False (excluded)"; return false; }
                NativeRect window; if (!GetWindowRect(foreground, out window)) { diagnostic = identity + "; fullscreen=False (no rectangle)"; return false; }
                MonitorInfo monitor = new MonitorInfo { Size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(MonitorInfo)) };
                if (!GetMonitorInfo(MonitorFromWindow(foreground, 2), ref monitor)) { diagnostic = identity + "; fullscreen=False (no monitor)"; return false; }
                const int tolerance = 2;
                bool result = window.Left <= monitor.Monitor.Left + tolerance && window.Top <= monitor.Monitor.Top + tolerance && window.Right >= monitor.Monitor.Right - tolerance && window.Bottom >= monitor.Monitor.Bottom - tolerance;
                diagnostic = identity + "; window=[" + window.Left + "," + window.Top + "," + window.Right + "," + window.Bottom + "]; monitor=[" + monitor.Monitor.Left + "," + monitor.Monitor.Top + "," + monitor.Monitor.Right + "," + monitor.Monitor.Bottom + "]; fullscreen=" + result;
                return result;
            }
        }
    }
}
