using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Media.Imaging;

namespace cztOCR
{
    public class CurrentImageStream : INotifyPropertyChanged
    {
        private static readonly Lazy<CurrentImageStream> _instance =
            new Lazy<CurrentImageStream>(() => new CurrentImageStream());

        public static CurrentImageStream Instance => _instance.Value;

        private MemoryStream _stream;
        private BitmapImage _bitmapImage;
        private readonly object _lock = new object();
        public event PropertyChangedEventHandler PropertyChanged;
        private const string SavePath = "last_image.dat";


        private CurrentImageStream()
        {
            _stream = new MemoryStream();
            _bitmapImage = new BitmapImage();
        }

        public BitmapImage CurrentImage
        {
            get => _bitmapImage;
            private set
            {
                _bitmapImage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentImage)));
            }
        }

        /// <summary>
        /// 更新当前图片（自动清除旧数据）
        /// </summary>
        public void UpdateImage(byte[] imageData)
        {
            lock (_stream)
            {
                _stream.SetLength(0);
                _stream.Write(imageData, 0, imageData.Length);
                _stream.Position = 0;

                // 创建新的BitmapImage
                var newImage = new BitmapImage();
                newImage.BeginInit();
                newImage.CacheOption = BitmapCacheOption.OnLoad;
                newImage.StreamSource = new MemoryStream(imageData); // 使用新流避免锁定
                newImage.EndInit();
                newImage.Freeze();

                CurrentImage = newImage;
            }
        }

        public byte[] GetImageBytes()
        {
            lock (_stream) 
            {
                if (_stream.Length == 0)
                    return Array.Empty<byte>();

                _stream.Position = 0; // 重置流位置
                return _stream.ToArray(); // 返回完整字节数组
            }
        }

        public bool IsImageEmpty()
        {
            lock (_lock) 
            {
                return _stream.Length == 0; // 直接检查流长度
            }
        }

        public string GetImageHash()
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(GetImageBytes());
                return Convert.ToBase64String(hashBytes);
            }
        }

        private void SaveToDisk()
        {
            lock (_lock)
            {
                try
                {
                    byte[] data = GetImageBytes();
                    if (data.Length > 0)
                    {
                        File.WriteAllBytes(SavePath, data);
                        Console.WriteLine("图片数据已自动保存");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"保存失败: {ex.Message}");
                }
            }
        }

        //加载不出来不知道为什么，以后再调
        private void LoadFromDisk()
        {
            
            lock (_lock)
            {
                try
                {
                    if (File.Exists(SavePath))
                    {
                        byte[] data = File.ReadAllBytes(SavePath);
                        _stream = new MemoryStream(data);
                        Console.WriteLine("历史图片数据已加载");
                    }
                    else
                    {
                        _stream = new MemoryStream();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"加载失败: {ex.Message}");
                    _stream = new MemoryStream();
                }
            }
        }

        /// <summary>
        /// 获取当前图片流的只读副本
        /// </summary>
        public MemoryStream GetImageStreamCopy()
        {
            lock (_lock)
            {
                var copy = new MemoryStream();
                _stream.Position = 0;
                _stream.CopyTo(copy);
                copy.Position = 0;
                return copy;
            }
        }

        public void Dispose()
        {
            //SaveToDisk();
            _stream?.Dispose();
        }
    }
}
