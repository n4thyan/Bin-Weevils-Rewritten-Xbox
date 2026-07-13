using System;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace BinWeevilsRewrittenXbox
{
    public sealed partial class MainPage : Page
    {
        private static readonly Uri StartUri = new Uri("https://play.binweevils.app/");

        public MainPage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Required;
            Loaded += MainPage_Loaded;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (GameWebView.Source == null)
            {
                GameWebView.Navigate(StartUri);
            }
        }

        private void GameWebView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            StatusText.Text = "Loading " + (args.Uri?.Host ?? "Bin Weevils Rewritten") + "…";
            StatusOverlay.Visibility = Visibility.Visible;
            LoadingRing.IsActive = true;
        }

        private void GameWebView_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            LoadingRing.IsActive = false;

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

            if (IsBinWeevilsUri(args.Uri))
            {
                sender.Navigate(args.Uri);
                return;
            }

            await Launcher.LaunchUriAsync(args.Uri);
        }

        private static bool IsBinWeevilsUri(Uri uri)
        {
            if (uri == null || uri.Scheme != Uri.UriSchemeHttps)
            {
                return false;
            }

            return uri.Host.Equals("binweevils.app", StringComparison.OrdinalIgnoreCase)
                || uri.Host.EndsWith(".binweevils.app", StringComparison.OrdinalIgnoreCase);
        }
    }
}
