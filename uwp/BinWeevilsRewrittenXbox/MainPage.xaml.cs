using System;
using System.Globalization;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Gaming.Input;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace BinWeevilsRewrittenXbox
{
    public sealed partial class MainPage : Page
    {
        private static readonly Uri StartUri = new Uri("https://play.binweevils.app/");
        private const double DefaultZoom = 0.78;
        private const double MinimumZoom = 0.60;
        private const double MaximumZoom = 1.00;
        private const double ZoomStep = 0.05;

        private readonly SystemNavigationManager navigationManager = SystemNavigationManager.GetForCurrentView();
        private readonly DispatcherTimer gamepadTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        private readonly DispatcherTimer zoomHudTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        private Uri lastRequestedUri = StartUri;
        private GamepadButtons previousButtons = GamepadButtons.None;
        private DateTime nextDirectionalInput = DateTime.MinValue;
        private bool gamepadTickRunning;
        private bool controllerHelpShown;
        private double pageZoom = DefaultZoom;

        private const string ControllerBootstrapScript = @"
(function () {
    var zoom = window.__bwrXboxZoom || 0.78;
    var styleId = '__bwrXboxStyle';
    var focusStyle = '__bwrXboxFocusStyle';

    function visible(el) {
        if (!el || el.disabled) return false;
        var style = window.getComputedStyle(el), rect = el.getBoundingClientRect();
        return style.display !== 'none' && style.visibility !== 'hidden' && rect.width > 1 && rect.height > 1;
    }

    function candidates() {
        var modal = document.querySelector('[role=dialog],.modal,.popup,.dialog,.swal2-container,.fancybox-container');
        var root = modal && visible(modal) ? modal : document;
        return Array.prototype.slice.call(root.querySelectorAll('a[href],button,input:not([type=hidden]),select,textarea,[tabindex]:not([tabindex=""-1""])')).filter(visible);
    }

    function centre(el) {
        var r = el.getBoundingClientRect();
        return { x: r.left + r.width / 2, y: r.top + r.height / 2 };
    }

    var nav = window.__bwrXboxNav || {};
    nav.items = [];
    nav.index = -1;
    nav.refresh = function () {
        this.items = candidates();
        var active = document.activeElement;
        this.index = this.items.indexOf(active);
        if (this.index < 0 && this.items.length) this.index = 0;
        return this.items;
    };
    nav.focusCurrent = function () {
        this.refresh();
        if (!this.items.length) return false;
        var el = this.items[Math.max(0, this.index)];
        try { el.focus({ preventScroll: true }); } catch (_) { el.focus(); }
        el.scrollIntoView({ behavior: 'smooth', block: 'center', inline: 'center' });
        return true;
    };
    nav.move = function (direction) {
        this.refresh();
        if (!this.items.length) return false;
        var active = document.activeElement;
        if (!visible(active) || this.items.indexOf(active) < 0) return this.focusCurrent();
        var from = centre(active), best = null, bestScore = Number.MAX_VALUE;
        this.items.forEach(function (el) {
            if (el === active) return;
            var to = centre(el), dx = to.x - from.x, dy = to.y - from.y;
            var valid = direction === 'up' ? dy < -4 : direction === 'down' ? dy > 4 : direction === 'left' ? dx < -4 : dx > 4;
            if (!valid) return;
            var primary = direction === 'up' || direction === 'down' ? Math.abs(dy) : Math.abs(dx);
            var secondary = direction === 'up' || direction === 'down' ? Math.abs(dx) : Math.abs(dy);
            var score = primary + secondary * 2.25;
            if (score < bestScore) { bestScore = score; best = el; }
        });
        if (!best) {
            var step = direction === 'up' || direction === 'left' ? -1 : 1;
            var current = this.items.indexOf(active);
            best = this.items[(current + step + this.items.length) % this.items.length];
        }
        this.index = this.items.indexOf(best);
        try { best.focus({ preventScroll: true }); } catch (_) { best.focus(); }
        best.scrollIntoView({ behavior: 'smooth', block: 'center', inline: 'center' });
        return true;
    };
    nav.activate = function () {
        var el = document.activeElement;
        if (!visible(el) || el === document.body || el === document.documentElement) {
            if (!this.focusCurrent()) return false;
            el = document.activeElement;
        }
        if (el.tagName === 'INPUT' || el.tagName === 'TEXTAREA' || el.tagName === 'SELECT') {
            el.focus();
            if (typeof el.select === 'function' && el.type !== 'checkbox' && el.type !== 'radio') {
                try { el.select(); } catch (_) {}
            }
            return true;
        }
        el.click();
        return true;
    };
    nav.scroll = function (amount) {
        window.scrollBy({ top: amount, behavior: 'smooth' });
        return true;
    };
    nav.setZoom = function (value) {
        zoom = Math.max(0.60, Math.min(1.00, Number(value) || 0.78));
        window.__bwrXboxZoom = zoom;
        document.documentElement.style.zoom = String(zoom);
        document.documentElement.style.width = (100 / zoom) + '%';
        document.documentElement.style.background = '#101010';
        document.body.style.marginLeft = 'auto';
        document.body.style.marginRight = 'auto';
        return zoom;
    };

    if (!document.getElementById(styleId)) {
        var style = document.createElement('style');
        style.id = styleId;
        style.textContent = 'html{background:#101010!important;overflow-x:hidden!important;}body{transform-origin:top center!important;}img,canvas,video,object,embed{max-width:100%;}';
        document.head.appendChild(style);
    }
    if (!document.getElementById(focusStyle)) {
        var focus = document.createElement('style');
        focus.id = focusStyle;
        focus.textContent = '*:focus{outline:4px solid #7ed321!important;outline-offset:3px!important;box-shadow:0 0 0 2px rgba(0,0,0,.65)!important;}';
        document.head.appendChild(focus);
    }

    window.__bwrXboxNav = nav;
    nav.setZoom(zoom);
    nav.refresh();
})();";

        public MainPage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Required;
            Loaded += MainPage_Loaded;
            Unloaded += MainPage_Unloaded;
            AddKeyboardAccelerator(VirtualKey.Escape, BackAccelerator_Invoked);
            AddKeyboardAccelerator(VirtualKey.GoBack, BackAccelerator_Invoked);
            AddKeyboardAccelerator(VirtualKey.F5, RefreshAccelerator_Invoked);
            AddKeyboardAccelerator(VirtualKey.Home, HomeAccelerator_Invoked);
            AddKeyboardAccelerator(VirtualKey.Add, ZoomInAccelerator_Invoked);
            AddKeyboardAccelerator(VirtualKey.Subtract, ZoomOutAccelerator_Invoked);
            gamepadTimer.Tick += GamepadTimer_Tick;
            zoomHudTimer.Tick += ZoomHudTimer_Tick;
            Gamepad.GamepadAdded += Gamepad_GamepadAdded;
            Gamepad.GamepadRemoved += Gamepad_GamepadRemoved;
            LoadZoomSetting();
        }

        private void AddKeyboardAccelerator(VirtualKey key, TypedEventHandler<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs> handler)
        {
            var accelerator = new KeyboardAccelerator { Key = key };
            accelerator.Invoked += handler;
            KeyboardAccelerators.Add(accelerator);
        }

        private void LoadZoomSetting()
        {
            try
            {
                var stored = ApplicationData.Current.LocalSettings.Values["PageZoom"];
                if (stored is double value) pageZoom = ClampZoom(value);
                else if (stored is string text && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)) pageZoom = ClampZoom(parsed);
            }
            catch { pageZoom = DefaultZoom; }
        }

        private void SaveZoomSetting()
        {
            try { ApplicationData.Current.LocalSettings.Values["PageZoom"] = pageZoom; } catch { }
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            navigationManager.BackRequested += NavigationManager_BackRequested;
            gamepadTimer.Start();
            UpdateControllerHelp();
            if (GameWebView.Source == null) NavigateTo(StartUri);
            UpdateBackButtonState();
        }

        private void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            navigationManager.BackRequested -= NavigationManager_BackRequested;
            gamepadTimer.Stop();
            zoomHudTimer.Stop();
            Gamepad.GamepadAdded -= Gamepad_GamepadAdded;
            Gamepad.GamepadRemoved -= Gamepad_GamepadRemoved;
        }

        private async void GameWebView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            if (args.Uri != null && !IsInternalUri(args.Uri)) { args.Cancel = true; await Launcher.LaunchUriAsync(args.Uri); return; }
            if (args.Uri != null) lastRequestedUri = args.Uri;
            ShowLoading("Loading " + (args.Uri?.Host ?? "Bin Weevils Rewritten") + "…");
        }

        private async void GameWebView_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            LoadingRing.IsActive = false;
            UpdateBackButtonState();
            if (args.IsSuccess)
            {
                StatusOverlay.Visibility = Visibility.Collapsed;
                RetryButton.Visibility = Visibility.Collapsed;
                HomeButton.Visibility = Visibility.Collapsed;
                await InstallControllerNavigationAsync();
                await ApplyZoomAsync();
                return;
            }
            StatusText.Text = "The page could not be loaded. Check the Xbox network connection, then try again.";
            RetryButton.Visibility = Visibility.Visible;
            HomeButton.Visibility = Visibility.Visible;
            StatusOverlay.Visibility = Visibility.Visible;
        }

        private async void GameWebView_NewWindowRequested(WebView sender, WebViewNewWindowRequestedEventArgs args)
        {
            args.Handled = true;
            if (IsInternalUri(args.Uri)) { NavigateTo(args.Uri); return; }
            if (args.Uri != null) await Launcher.LaunchUriAsync(args.Uri);
        }

        private void RetryButton_Click(object sender, RoutedEventArgs e) => NavigateTo(lastRequestedUri ?? StartUri);
        private void HomeButton_Click(object sender, RoutedEventArgs e) => NavigateTo(StartUri);
        private void NavigationManager_BackRequested(object sender, BackRequestedEventArgs e) => e.Handled = TryGoBack();
        private void BackAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args) => args.Handled = TryGoBack();
        private void RefreshAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args) { RefreshCurrentPage(); args.Handled = true; }
        private void HomeAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args) { NavigateTo(StartUri); args.Handled = true; }
        private async void ZoomInAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args) { await ChangeZoomAsync(ZoomStep); args.Handled = true; }
        private async void ZoomOutAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args) { await ChangeZoomAsync(-ZoomStep); args.Handled = true; }

        private async void GamepadTimer_Tick(object sender, object e)
        {
            if (gamepadTickRunning || Gamepad.Gamepads.Count == 0) return;
            gamepadTickRunning = true;
            try
            {
                var reading = Gamepad.Gamepads[0].GetCurrentReading();
                var buttons = reading.Buttons;
                if (Pressed(buttons, GamepadButtons.B)) TryGoBack();
                if (Pressed(buttons, GamepadButtons.X)) RefreshCurrentPage();
                if (Pressed(buttons, GamepadButtons.Y)) NavigateTo(StartUri);
                if (Pressed(buttons, GamepadButtons.A)) await InvokeControllerCommandAsync("activate()");
                if (Pressed(buttons, GamepadButtons.Menu)) ToggleControllerHelp();
                if (Pressed(buttons, GamepadButtons.LeftShoulder)) await ChangeZoomAsync(-ZoomStep);
                if (Pressed(buttons, GamepadButtons.RightShoulder)) await ChangeZoomAsync(ZoomStep);
                if (Pressed(buttons, GamepadButtons.View)) await SetZoomAsync(DefaultZoom, true);

                if (DateTime.UtcNow >= nextDirectionalInput)
                {
                    var command = GetDirectionalCommand(reading);
                    if (command != null)
                    {
                        await InvokeControllerCommandAsync(command);
                        nextDirectionalInput = DateTime.UtcNow.AddMilliseconds(180);
                    }
                }
                if (Math.Abs(reading.RightThumbstickY) > 0.25)
                {
                    var amount = (int)(-reading.RightThumbstickY * 90);
                    await InvokeControllerCommandAsync("scroll(" + amount + ")");
                }
                previousButtons = buttons;
            }
            catch { }
            finally { gamepadTickRunning = false; }
        }

        private string GetDirectionalCommand(GamepadReading reading)
        {
            if ((reading.Buttons & GamepadButtons.DPadDown) != 0 || reading.LeftThumbstickY < -0.55) return "move('down')";
            if ((reading.Buttons & GamepadButtons.DPadRight) != 0 || reading.LeftThumbstickX > 0.55) return "move('right')";
            if ((reading.Buttons & GamepadButtons.DPadUp) != 0 || reading.LeftThumbstickY > 0.55) return "move('up')";
            if ((reading.Buttons & GamepadButtons.DPadLeft) != 0 || reading.LeftThumbstickX < -0.55) return "move('left')";
            return null;
        }

        private bool Pressed(GamepadButtons current, GamepadButtons button) => (current & button) != 0 && (previousButtons & button) == 0;

        private async Task InstallControllerNavigationAsync()
        {
            try { await GameWebView.InvokeScriptAsync("eval", new[] { ControllerBootstrapScript }); } catch { }
        }

        private async Task InvokeControllerCommandAsync(string command)
        {
            try { await GameWebView.InvokeScriptAsync("eval", new[] { "window.__bwrXboxNav&&window.__bwrXboxNav." + command + ";" }); }
            catch { await InstallControllerNavigationAsync(); }
        }

        private async Task ChangeZoomAsync(double delta) => await SetZoomAsync(pageZoom + delta, true);

        private async Task SetZoomAsync(double value, bool showHud)
        {
            pageZoom = ClampZoom(value);
            SaveZoomSetting();
            await ApplyZoomAsync();
            if (showHud) ShowZoomHud();
        }

        private async Task ApplyZoomAsync()
        {
            var value = pageZoom.ToString("0.00", CultureInfo.InvariantCulture);
            try
            {
                await GameWebView.InvokeScriptAsync("eval", new[]
                {
                    "window.__bwrXboxZoom=" + value + ";window.__bwrXboxNav&&window.__bwrXboxNav.setZoom(" + value + ");"
                });
            }
            catch { await InstallControllerNavigationAsync(); }
        }

        private static double ClampZoom(double value) => Math.Max(MinimumZoom, Math.Min(MaximumZoom, Math.Round(value, 2)));

        private void ShowZoomHud()
        {
            ZoomText.Text = "Zoom " + Math.Round(pageZoom * 100) + "%";
            ZoomHud.Visibility = Visibility.Visible;
            zoomHudTimer.Stop();
            zoomHudTimer.Start();
        }

        private void ZoomHudTimer_Tick(object sender, object e)
        {
            zoomHudTimer.Stop();
            ZoomHud.Visibility = Visibility.Collapsed;
        }

        private void Gamepad_GamepadAdded(object sender, Gamepad e) => _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, UpdateControllerHelp);
        private void Gamepad_GamepadRemoved(object sender, Gamepad e) => _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, UpdateControllerHelp);
        private void UpdateControllerHelp() => ControllerHelp.Visibility = Gamepad.Gamepads.Count > 0 && controllerHelpShown ? Visibility.Visible : Visibility.Collapsed;
        private void ToggleControllerHelp() { controllerHelpShown = !controllerHelpShown; UpdateControllerHelp(); }
        private void RefreshCurrentPage() => NavigateTo(GameWebView.Source ?? lastRequestedUri ?? StartUri);

        private void NavigateTo(Uri uri)
        {
            if (uri == null || !IsInternalUri(uri)) uri = StartUri;
            lastRequestedUri = uri;
            GameWebView.Navigate(uri);
        }

        private void ShowLoading(string message)
        {
            StatusText.Text = message;
            RetryButton.Visibility = Visibility.Collapsed;
            HomeButton.Visibility = Visibility.Collapsed;
            StatusOverlay.Visibility = Visibility.Visible;
            LoadingRing.IsActive = true;
        }

        private bool TryGoBack()
        {
            if (!GameWebView.CanGoBack) return false;
            GameWebView.GoBack();
            return true;
        }

        private void UpdateBackButtonState()
        {
            navigationManager.AppViewBackButtonVisibility = GameWebView.CanGoBack ? AppViewBackButtonVisibility.Visible : AppViewBackButtonVisibility.Collapsed;
        }

        private static bool IsInternalUri(Uri uri)
        {
            if (uri == null) return false;
            if (uri.Scheme == "about") return true;
            if (uri.Scheme != Uri.UriSchemeHttps) return false;
            return IsHostOrSubdomain(uri.Host, "binweevils.app");
        }

        private static bool IsHostOrSubdomain(string host, string domain)
        {
            return host.Equals(domain, StringComparison.OrdinalIgnoreCase) || host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase);
        }
    }
}