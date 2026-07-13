using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Gaming.Input;
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
        private readonly SystemNavigationManager navigationManager = SystemNavigationManager.GetForCurrentView();
        private readonly DispatcherTimer gamepadTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };

        private Uri lastRequestedUri = StartUri;
        private GamepadButtons previousButtons = GamepadButtons.None;
        private DateTime nextDirectionalInput = DateTime.MinValue;
        private bool gamepadTickRunning;
        private bool controllerHelpShown;

        private const string ControllerBootstrapScript = @"
(function () {
    if (window.__bwrXboxNav) { window.__bwrXboxNav.refresh(); return; }

    var nav = {
        items: [],
        index: -1,
        styleId: '__bwrXboxFocusStyle',
        refresh: function () {
            this.items = Array.prototype.slice.call(document.querySelectorAll(
                'a[href],button,input:not([type=hidden]),select,textarea,[tabindex]:not([tabindex=""-1""])'
            )).filter(function (el) {
                var style = window.getComputedStyle(el);
                var rect = el.getBoundingClientRect();
                return !el.disabled && style.display !== 'none' && style.visibility !== 'hidden' && rect.width > 0 && rect.height > 0;
            });
            if (this.index >= this.items.length) this.index = this.items.length - 1;
        },
        focus: function (delta) {
            this.refresh();
            if (!this.items.length) return false;
            this.index = (this.index + delta + this.items.length) % this.items.length;
            var el = this.items[this.index];
            el.focus({ preventScroll: true });
            el.scrollIntoView({ behavior: 'smooth', block: 'center', inline: 'center' });
            return true;
        },
        activate: function () {
            var el = document.activeElement;
            if (!el || el === document.body || el === document.documentElement) {
                this.refresh();
                if (!this.items.length) return false;
                this.index = Math.max(0, this.index);
                el = this.items[this.index];
                el.focus();
            }
            if (el.tagName === 'INPUT' || el.tagName === 'TEXTAREA' || el.tagName === 'SELECT') {
                el.focus();
                el.click();
                return true;
            }
            el.click();
            return true;
        },
        scroll: function (amount) {
            window.scrollBy({ top: amount, behavior: 'smooth' });
            return true;
        }
    };

    if (!document.getElementById(nav.styleId)) {
        var style = document.createElement('style');
        style.id = nav.styleId;
        style.textContent = '*:focus{outline:4px solid #7ed321 !important;outline-offset:3px !important;}';
        document.head.appendChild(style);
    }

    window.__bwrXboxNav = nav;
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

            gamepadTimer.Tick += GamepadTimer_Tick;
            Gamepad.GamepadAdded += Gamepad_GamepadAdded;
            Gamepad.GamepadRemoved += Gamepad_GamepadRemoved;
        }

        private void AddKeyboardAccelerator(VirtualKey key, TypedEventHandler<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs> handler)
        {
            var accelerator = new KeyboardAccelerator { Key = key };
            accelerator.Invoked += handler;
            KeyboardAccelerators.Add(accelerator);
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            navigationManager.BackRequested += NavigationManager_BackRequested;
            gamepadTimer.Start();
            UpdateControllerHelp();

            if (GameWebView.Source == null)
            {
                NavigateTo(StartUri);
            }

            UpdateBackButtonState();
        }

        private void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            navigationManager.BackRequested -= NavigationManager_BackRequested;
            gamepadTimer.Stop();
            Gamepad.GamepadAdded -= Gamepad_GamepadAdded;
            Gamepad.GamepadRemoved -= Gamepad_GamepadRemoved;
        }

        private async void GameWebView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            if (args.Uri != null && !IsInternalUri(args.Uri))
            {
                args.Cancel = true;
                await Launcher.LaunchUriAsync(args.Uri);
                return;
            }

            if (args.Uri != null)
            {
                lastRequestedUri = args.Uri;
            }

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

            if (IsInternalUri(args.Uri))
            {
                NavigateTo(args.Uri);
                return;
            }

            if (args.Uri != null)
            {
                await Launcher.LaunchUriAsync(args.Uri);
            }
        }

        private void RetryButton_Click(object sender, RoutedEventArgs e) => NavigateTo(lastRequestedUri ?? StartUri);
        private void HomeButton_Click(object sender, RoutedEventArgs e) => NavigateTo(StartUri);

        private void NavigationManager_BackRequested(object sender, BackRequestedEventArgs e)
        {
            e.Handled = TryGoBack();
        }

        private void BackAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = TryGoBack();
        }

        private void RefreshAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            RefreshCurrentPage();
            args.Handled = true;
        }

        private void HomeAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            NavigateTo(StartUri);
            args.Handled = true;
        }

        private async void GamepadTimer_Tick(object sender, object e)
        {
            if (gamepadTickRunning || Gamepad.Gamepads.Count == 0)
            {
                return;
            }

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
            catch
            {
                // Controller input must never be able to crash the browser shell.
            }
            finally
            {
                gamepadTickRunning = false;
            }
        }

        private string GetDirectionalCommand(GamepadReading reading)
        {
            if ((reading.Buttons & GamepadButtons.DPadDown) != 0 || reading.LeftThumbstickY < -0.55) return "focus(1)";
            if ((reading.Buttons & GamepadButtons.DPadRight) != 0 || reading.LeftThumbstickX > 0.55) return "focus(1)";
            if ((reading.Buttons & GamepadButtons.DPadUp) != 0 || reading.LeftThumbstickY > 0.55) return "focus(-1)";
            if ((reading.Buttons & GamepadButtons.DPadLeft) != 0 || reading.LeftThumbstickX < -0.55) return "focus(-1)";
            return null;
        }

        private bool Pressed(GamepadButtons current, GamepadButtons button)
        {
            return (current & button) != 0 && (previousButtons & button) == 0;
        }

        private async Task InstallControllerNavigationAsync()
        {
            try
            {
                await GameWebView.InvokeScriptAsync("eval", new[] { ControllerBootstrapScript });
            }
            catch
            {
                // Some pages may temporarily block script injection while redirecting.
            }
        }

        private async Task InvokeControllerCommandAsync(string command)
        {
            try
            {
                var script = "window.__bwrXboxNav&&window.__bwrXboxNav." + command + ";";
                await GameWebView.InvokeScriptAsync("eval", new[] { script });
            }
            catch
            {
                await InstallControllerNavigationAsync();
            }
        }

        private void Gamepad_GamepadAdded(object sender, Gamepad e)
        {
            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, UpdateControllerHelp);
        }

        private void Gamepad_GamepadRemoved(object sender, Gamepad e)
        {
            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, UpdateControllerHelp);
        }

        private void UpdateControllerHelp()
        {
            ControllerHelp.Visibility = Gamepad.Gamepads.Count > 0 && controllerHelpShown
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void ToggleControllerHelp()
        {
            controllerHelpShown = !controllerHelpShown;
            UpdateControllerHelp();
        }

        private void RefreshCurrentPage()
        {
            NavigateTo(GameWebView.Source ?? lastRequestedUri ?? StartUri);
        }

        private void NavigateTo(Uri uri)
        {
            if (uri == null || !IsInternalUri(uri))
            {
                uri = StartUri;
            }

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
            if (!GameWebView.CanGoBack)
            {
                return false;
            }

            GameWebView.GoBack();
            return true;
        }

        private void UpdateBackButtonState()
        {
            navigationManager.AppViewBackButtonVisibility = GameWebView.CanGoBack
                ? AppViewBackButtonVisibility.Visible
                : AppViewBackButtonVisibility.Collapsed;
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
            return host.Equals(domain, StringComparison.OrdinalIgnoreCase)
                || host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase);
        }
    }
}