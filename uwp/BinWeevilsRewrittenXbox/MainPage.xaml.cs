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
        private Uri lastRequestedUri = StartUri;

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

            var refresh = new KeyboardAccelerator { Key = VirtualKey.F5 };
            refresh.Invoked += RefreshAccelerator_Invoked;
            KeyboardAccelerators.Add(refresh);

            var home = new KeyboardAccelerator { Key = VirtualKey.Home };
            home.Invoked += HomeAccelerator_Invoked;
            KeyboardAccelerators.Add(home);
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            navigationManager.BackRequested += NavigationManager_BackRequested;

            if (GameWebView.Source == null)
            {
                NavigateTo(StartUri);
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

            if (args.Uri != null)
            {
                lastRequestedUri = args.Uri;
            }

            ShowLoading("Loading " + (args.Uri?.Host ?? "Bin Weevils Rewritten") + "…");
        }

        private void GameWebView_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            LoadingRing.IsActive = false;
            UpdateBackButtonState();

            if (args.IsSuccess)
            {
                StatusOverlay.Visibility = Visibility.Collapsed;
                RetryButton.Visibility = Visibility.Collapsed;
                HomeButton.Visibility = Visibility.Collapsed;
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

        private void RetryButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(lastRequestedUri ?? StartUri);
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(StartUri);
        }

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
            NavigateTo(GameWebView.Source ?? lastRequestedUri ?? StartUri);
            args.Handled = true;
        }

        private void HomeAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            NavigateTo(StartUri);
            args.Handled = true;
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
