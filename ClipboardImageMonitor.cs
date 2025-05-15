using System;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace cztOCR
{
    internal class ClipboardImageMonitor
    {
        private DispatcherTimer clipboardTimer;
        private string lastPngHash = null;
        public bool newimagecome = false;

        public ClipboardImageMonitor()
        {
            clipboardTimer = new DispatcherTimer();
            clipboardTimer.Interval = TimeSpan.FromMilliseconds(250);  //100ms执行一次
            clipboardTimer.Tick += TryLoadClip;
        }

        public void StartMonitor() { clipboardTimer.Start(); }

        public void StopMonitor() { clipboardTimer.Stop(); }

        public void TryLoadClip(object sender, EventArgs e)
        {
            try
            {
                if (Clipboard.ContainsImage())
                {
                    BitmapSource clipboardImage = Clipboard.GetImage();
                    using (MemoryStream ms = new MemoryStream())
                    {
                        // 使用 BitmapEncoder 保存图像
                        PngBitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(clipboardImage));
                        encoder.Save(ms);
                        byte[] imageBytes = ms.ToArray();

                        string currentPngHash = GetImageHash(imageBytes);// 出于一致性不用CurrentImageStream里的Hash
                        if (currentPngHash != lastPngHash)
                        {
                            CurrentImageStream.Instance.UpdateImage(imageBytes);
                            newimagecome = true; 
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private string GetImageHash(byte[] imageBytes)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(imageBytes);
                return Convert.ToBase64String(hashBytes);
            }
        }

    }
}
