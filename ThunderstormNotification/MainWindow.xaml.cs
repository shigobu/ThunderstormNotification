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
using OpenCvSharp;
using System.Drawing;
using OpenCvSharp.Extensions;
using System.Windows.Threading;
using Notification.Wpf;
using System.Runtime.InteropServices;

namespace ThunderstormNotification
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private DispatcherTimer timer;

        private float comparisonResult = 0;

        private List<Bitmap> bitmaps = new List<Bitmap>();

        static System.Threading.SemaphoreSlim _semaphore = new System.Threading.SemaphoreSlim(1, 1);

        public MainWindow()
        {
            InitializeComponent();

            // 画面が閉じられるときに、タイマを停止（ラムダ式で記述）
            this.Closing += (s, e) => timer.Stop();

            //画面閉じたときに、通知も閉じるように設定
            Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
        }

        private async void Timer_Tick(object sender, EventArgs e)
        {
            float prev = comparisonResult;

            ImageSource imageSource = await ComparisonImage();

            float aftar = comparisonResult;
            resultTextBox.Text = comparisonResult.ToString("f");

            if (!float.TryParse(thresholdTextBox.Text, out float threshold))
            {
                //失敗したら終了
                return;
            }

            if (imageSource == null)
            {
                return;
            }

            //前がしきい値以上で、あとがしきい値以下の場合
            if (prev > threshold && aftar < threshold)
            {
                StackPanel stackPanel = new StackPanel();
                TextBlock textBlock = new TextBlock();
                textBlock.Text = "雷雨っぽい";
                textBlock.HorizontalAlignment = HorizontalAlignment.Center;
                System.Windows.Controls.Image image = new System.Windows.Controls.Image();
                image.Source = imageSource;
                stackPanel.Children.Add(textBlock);
                stackPanel.Children.Add(image);

                var notificationManager = new NotificationManager();
                //notificationManager.Show(this.Title, "雷雨っぽい", NotificationType.Information);
                notificationManager.Show(stackPanel, null);
                System.Media.SystemSounds.Asterisk.Play();
            }
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
            int fileCount = 1;
            string imagePath = Path.Combine(assemblyDir, $"雷雨{fileCount}.png");
            while (File.Exists(imagePath))
            {
                fileCount++;
                imagePath = Path.Combine(assemblyDir, $"雷雨{fileCount}.png");
            }

            using (FileStream fileStream = new FileStream(imagePath, FileMode.Create))
            {
                await webView.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, fileStream);
                await _semaphore.WaitAsync(); // ロックを取得する
                try
                {
                    bitmaps.Add(new Bitmap(fileStream));
                }
                finally
                {
                    _semaphore.Release(); // 違うスレッドでロックを解放してもOK
                }
            }

            CountImage();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //比較用画像の読み込み
            foreach (var fileName in GetImageFileNames())
            {
                bitmaps.Add(new Bitmap(fileName));
            }
            CountImage();

            //比較領域矩形の初期化
            ComparisonAreaRect.Width = webView.ActualWidth;
            ComparisonAreaRect.Height = webView.ActualHeight;

            //タイマ作成とスタート
            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 2);
            timer.Tick += Timer_Tick;
            timer.Start();
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

        /// <summary>
        /// 比較用の画像の数を数えて表示の更新を行います。
        /// </summary>
        private void CountImage()
        {
            imageCountTextBox.Text = GetImageFileNames().Length.ToString();
        }

        /// <summary>
        /// 比較用ファイル名一覧を取得します。
        /// </summary>
        /// <returns></returns>
        private string[] GetImageFileNames()
        {
            IEnumerable<string> files = Directory.EnumerateFiles(GetAssemblyDirectory(), "雷雨*");
            return files.ToArray();
        }

        /// <summary>
        /// 標準画像と比較し、comparisonResultメンバー変数を更新します。
        /// </summary>
        private async Task<ImageSource> ComparisonImage()
        {
            string[] fileNames = GetImageFileNames();
            if (fileNames.Length == 0)
            {
                return null;
            }

            using (MemoryStream memoryStream = new MemoryStream())
            {
                await webView.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, memoryStream);
                float result = float.MaxValue;
                await _semaphore.WaitAsync(); // ロックを取得する
                try
                {
                    foreach (Bitmap bitmap in bitmaps)
                    {
                        using (Bitmap bitmap1 = new Bitmap(memoryStream))
                        using (Mat mat1 = bitmap1.ToMat())
                        using (Mat mat2 = bitmap.ToMat())
                        {
                            float match = await Task.Run(() => ImageMatch(mat1, mat2, false));
                            if (match < result)
                            {
                                result = match;
                            }
                        }
                    }
                }
                finally
                {
                    _semaphore.Release(); // 違うスレッドでロックを解放してもOK
                }
                comparisonResult = result;

                return BitmapFrame.Create(memoryStream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        private Stream CapturePreview(Stream stream)
        {
            webView.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, stream);
            return stream;
        }

        /// <summary>
        /// 2つの画像の類似度をチェックする
        /// </summary>
        /// <param name="mat1"></param>
        /// <param name="mat2"></param>
        /// <param name="show">画像を表示するかのフラグ</param>
        /// <returns></returns>
        private float ImageMatch(Mat mat1, Mat mat2, bool show)
        {

            using (var descriptors1 = new Mat())
            using (var descriptors2 = new Mat())
            {

                // 特徴点を検出
                var akaze = AKAZE.Create();

                // キーポイントを検出
                akaze.DetectAndCompute(mat1, null, out KeyPoint[] keyPoints1, descriptors1);
                akaze.DetectAndCompute(mat2, null, out KeyPoint[] keyPoints2, descriptors2);

                // それぞれの特徴量をマッチング
                var matcher = new BFMatcher(NormTypes.Hamming, false);
                var matches = matcher.Match(descriptors1, descriptors2);

                // 2つの画像のマッチング結果を表示
                if (show)
                {
                    using (var drowMatch = new Mat())
                    {
                        Cv2.DrawMatches(mat1, keyPoints1, mat2, keyPoints2, matches, drowMatch);
                        Cv2.ImShow("image match", drowMatch);
                    }
                }

                // 平均距離を返却(小さい方が類似度が高い)
                var sum = matches.Sum(x => x.Distance);
                return sum / matches.Length;

            }

        }
    }
}
