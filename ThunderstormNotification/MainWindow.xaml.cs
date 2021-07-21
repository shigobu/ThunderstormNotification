using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.Web.WebView2.Core;
using System.IO;

namespace ThunderstormNotification
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Goボタン押下時イベントハンドラ
        /// </summary>
        private void ButtonGo_Click(object sender, RoutedEventArgs e)
        {
            NavigationToAddressBar();
        }

        /// <summary>
        /// アドレスバーのキーアップイベントハンドラ
        /// </summary>
        private void AddressBar_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                NavigationToAddressBar();
            }
        }

        /// <summary>
        /// ナビゲーション開始時イベントハンドラ
        /// </summary>
        private void WebView_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            progressBar.IsIndeterminate = true;

            addressBar.Text = e.Uri;
        }

        /// <summary>
        /// ナビゲーション成功時イベントハンドラ
        /// </summary>
        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            progressBar.IsIndeterminate = false;
        }

        private async void SetButton_Click(object sender, RoutedEventArgs e)
        {
            string assemblyDir = GetAssemblyDirectory();
            string imagePath = Path.Combine(assemblyDir, "雷雨.png");

            using (FileStream fileStream = new FileStream(imagePath, FileMode.Create))
            {
                await webView.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, fileStream);
            }
        }

        /// <summary>
        /// アドレスバーに入力されているアドレスへ移動します。
        /// </summary>
        private void NavigationToAddressBar()
        {
            if (webView != null && webView.CoreWebView2 != null)
            {
                webView.CoreWebView2.Navigate(addressBar.Text);
            }
        }

        /// <summary>
        /// exeのあるディレクトリを返します。
        /// </summary>
        private string GetAssemblyDirectory()
        {
            Assembly myAssembly = Assembly.GetEntryAssembly();
            string assemblyPath = myAssembly.Location;
            return Path.GetDirectoryName(assemblyPath);
        }
    }
}
