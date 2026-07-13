using System;
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

        public MainPage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Required;
            Loaded += MainPage_Loaded;
            Unloaded += MainPage_Unloaded;

            var escape = new KeyboardAccelerator { Key = VirtualKey.Escape };
            escape.Invoked += BackAccelerator_Invoked;
            KeyboardAccelerators.Add(escape);

            var browserBack = new KeyboardAccelerator { Key = VirtualKey.GoBack };
            browserBack.Invoked += BackAccelerator_Invoked;
            KeyboardAccelerators.Add(browserBack);
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            navigationManager.BackRequested += NavigationManager_BackRequested;

            if (GameWebView.Source == null)
            {
                GameWebView.Navigate(StartUri);
            }

            UpdateBackButtonState();
        }

        private void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            navigationManager.BackRequested -= NavigationManager_BackRequested;
        }

        private async void GameWebView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            if (args.Uri != null && !IsInternalUri(args.Uri))
            {
                args.Cancel = true;
                await Launcher.LaunchUriAsync(args.Uri);
                return;
            }

            StatusText.Text = "Loading " + (args.Uri?.Host ?? "Bin Weevils Rewritten") + "…";
            StatusOverlay.Visibility = Visibility.Visible;
            LoadingRing.IsActive = true;
        }

        private void GameWebView_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            LoadingRing.IsActive = false;
            UpdateBackButtonState();

            if (args.IsSuccess)
            {
                StatusOverlay.Visibility = Visibility.Collapsed;
                return;
            }

            StatusText.Text = "The page could not be loaded. Check the Xbox network connection and try again.";
            StatusOverlay.Visibility = Visibility.Visible;
        }

        private async void GameWebView_NewWindowRequested(WebView sender, WebViewNewWindowRequestedEventArgs args)
        {
            args.Handled = true;

            if (IsInternalUri(args.Uri))
            {
                sender.Navigate(args.Uri);
                return;
            }

            if (args.Uri != null)
            {
                await Launcher.LaunchUriAsync(args.Uri);
            }
        }

        private void NavigationManager_BackRequested(object sender, BackRequestedEventArgs e)
        {
            e.Handled = TryGoBack();
        }

        private void BackAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = TryGoBack();
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
            if (uri == null)
            {
                return false;
            }

            if (uri.Scheme == "about")
            {
                return true;
            }

            if (uri.Scheme != Uri.UriSchemeHttps)
            {
                return false;
            }

            return IsHostOrSubdomain(uri.Host, "binweevils.app");
        }

        private static bool IsHostOrSubdomain(string host, string domain)
        {
            return host.Equals(domain, StringComparison.OrdinalIgnoreCase)
                || host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase);
        }
    }
}
