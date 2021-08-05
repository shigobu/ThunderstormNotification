using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThunderstormNotification
{
    class CompareImage
    {
        /// <summary>
        /// 画像をグレースケール化
        /// </summary>
        /// <param name="img">グレースケール化する画像ファイル</param>
        private static Bitmap ConvertImageGray(Bitmap img)
        {
            using (var dst = new Mat())
            using (var src = BitmapConverter.ToMat(img))
            {
                // グレイスケール
                Cv2.CvtColor(src, dst, ColorConversionCodes.RGB2GRAY);

                return BitmapConverter.ToBitmap(dst);
            }
        }

        /// <summary>
        /// 画像をリサイズ
        /// </summary>
        /// <param name="img">リサイズする画像ファイル</param>
        /// <param name="width">リサイズ後の画像の幅</param>
        /// <param name="height">リサイズ後の画像の高さ</param>
        private static Bitmap ResizeImage(Bitmap img, int width, int height)
        {
            Bitmap resizeBmp = new Bitmap(width, height);
            Graphics g = Graphics.FromImage(resizeBmp);

            // 拡大するときのアルゴリズムの指定
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

            g.DrawImage(img, 0, 0, width, height); // リサイズ
            g.Dispose();
            return resizeBmp;
        }

        /// <summary>
        /// 画像のdHashを計算する
        /// </summary>
        /// <param name="img">PerceptualHashを計算する画像ファイル</param>
        private static byte[] CalcPerceptualDhash(Bitmap img)
        {
            Bitmap grayBmp = ConvertImageGray(img); //画像をグレースケール化
            Bitmap resizeBmp = ResizeImage(grayBmp, 9, 8); //画像をリサイズ
            List<byte> hash = new List<byte>(64);
            for (int y = 0; y < resizeBmp.Height; y++)
            {
                for (int x = 0; x < resizeBmp.Width - 1; x++)
                {
                    if (resizeBmp.GetPixel(x, y).GetBrightness() > resizeBmp.GetPixel(x + 1, y).GetBrightness())
                    {
                        hash.Add(1);
                    }
                    else
                    {
                        hash.Add(0);
                    }
                }
            }
            return hash.ToArray(); //64bitの2進数を返す(ex:"00111101101010111010101....")
        }

        /// <summary>
        /// 2つのハッシュ値のハミング距離を求める
        /// </summary>
        /// <param name="hashA">比較元のハッシュ値(64bit)</param>
        /// <param name="hashB">比較先のハッシュ値(64bit)</param>
        private static int CalcHammingDistance(byte[] hashA, byte[] hashB)
        {
            int hammingCount = 0; //ハミング距離計算用の変数
            for (int i = 0; i < hashA.Length; i++)
            {
                if (hashA[i] != hashB[i])
                {
                    // もしビット差分があればカウントを増やす
                    hammingCount++;
                }
            }
            return hammingCount;
        }

        /// <summary>
        /// 2つの画像の差分(ハミング距離)を求める
        /// </summary>
        /// <param name="imgA">比較する画像A(64bit)</param>
        /// <param name="imgB">比較する画像B(64bit)</param>
        public static int CalcHammingDistance(Bitmap imgA, Bitmap imgB)
        {
            byte[] dHashA = CalcPerceptualDhash(imgA);
            byte[] dHashB = CalcPerceptualDhash(imgB);
            int hamming = CalcHammingDistance(dHashA, dHashB);
            return hamming;
        }
    }
}
