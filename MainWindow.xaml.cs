using Baidu.Aip.Ocr;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;



namespace cztOCR
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private string currentImagePath = null; // 存储当前加载的图片路径
        private ClipboardImageMonitor clip_monitor;
        MemoryStream ms_current_image;
        private readonly Ocr client;
        private bool _isExiting;
        private bool _isPressX;


        public MainWindow()
        {
            InitializeComponent();
            LoadLatestOcrResult();
            clip_monitor = new ClipboardImageMonitor();
            AutoLoadClipboard_CheckedChanged(null, null);
            ms_current_image = new MemoryStream();

            client = new Ocr(ConfigData.Instance.OcrApiKey, ConfigData.Instance.OcrSecretKey)
            {
                Timeout = 60000 // 设置超时时间
            };
            var hash = CurrentImageStream.Instance.GetImageHash(); //懒加载，触发一下刷出图片

            this.StateChanged += MainWindow_StateChanged;
            this.Closed += (sender, e) => ConfigData.Instance.Save();
            this.DataContext = ConfigData.Instance;
        }

        // 打开图片文件
        private void OpenImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "图片文件 (*.jpg;*.jpeg;*.png;*.bmp;*.gif)|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
                Title = "选择图片"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                currentImagePath = filePath;
                try
                {
                    byte[] imageData = File.ReadAllBytes(filePath);
                    CurrentImageStream.Instance.UpdateImage(imageData);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("加载图片时发生错误: " + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        // 开始 OCR 识别
        private void StartOcr_Click(object sender, RoutedEventArgs e)
        {
            clip_monitor.newimagecome = false;

            if (CurrentImageStream.Instance.IsImageEmpty())
            {
                OcrTextBox.Text = "提示: 请先选择一张图片";
                return;
            }

            try
            {
                var image = CurrentImageStream.Instance.GetImageBytes();

                var options = new Dictionary<string, object>
                {
                    { "detect_direction", "true" },
                    { "probability", "false" },
                    {"language_type", ConfigData.Instance.OcrLanguage},
                    {"detect_language", "false"},
                };

                // 调用高精度识别接口
                var result = client.AccurateBasic(image, options);

                if (result["words_result"] is Newtonsoft.Json.Linq.JArray wordsArray)
                {
                    string recognizedText = string.Join("\n", wordsArray.Select(w => w["words"].ToString()));
                    OcrTextBox.Text = recognizedText;
                }
                else
                {
                    OcrTextBox.Text = "识别失败：未获取到结果";
                }
                SaveResult();
            }
            catch (Exception ex)
            {
                OcrTextBox.Text = "识别失败：" + ex.Message;
            }
        }

        // 保存识别结果
        private void SaveResult()
        {
            if (CurrentImageStream.Instance.IsImageEmpty())
                return;

            string saveFilePath = "ocr_results.json";
            List<OcrRecord> records = new List<OcrRecord>();

            // 计算图片哈希
            string imageHash = CurrentImageStream.Instance.GetImageHash();

            // 读取现有记录
            if (File.Exists(saveFilePath))
            {
                try
                {
                    string json = File.ReadAllText(saveFilePath);
                    records = JsonConvert.DeserializeObject<List<OcrRecord>>(json) ?? new List<OcrRecord>();
                }
                catch
                {
                    MessageBox.Show("读取原记录失败，将重新创建文件。", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            // 删除旧记录（同哈希）
            records.RemoveAll(r => r.Hash == imageHash);

            // 构造新记录
            string text = OcrTextBox.Text.Trim();
            string label = text.Length > 10 ? text.Substring(0, 10) : text;
            long unixTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds();

            records.Add(new OcrRecord
            {
                Hash = imageHash,
                Label = label,
                Text = text,
                Time = unixTimestamp
            });

            // 保存
            string output = JsonConvert.SerializeObject(records, Formatting.Indented);
            File.WriteAllText(saveFilePath, output);
        }

        public void LoadLatestOcrResult()
        {
            try
            {
                string jsonFilePath = "ocr_results.json";

                // 检查文件是否存在
                if (!File.Exists(jsonFilePath))
                {
                    Console.WriteLine("OCR结果文件不存在");
                    return;
                }

                // 读取并解析JSON文件
                string jsonContent = File.ReadAllText(jsonFilePath);
                var ocrResults = JsonConvert.DeserializeObject<List<OcrRecord>>(jsonContent);

                if (ocrResults == null || !ocrResults.Any())
                {
                    Console.WriteLine("没有可用的OCR结果");
                    return;
                }

                // 按时间戳降序排序并获取最新记录
                var latestResult = ocrResults
                    .OrderByDescending(r => r.Time)
                    .FirstOrDefault();

                if (latestResult != null)
                {
                    // 将文本显示到OcrTextBox
                    OcrTextBox.Text = latestResult.Text;

                    // 可选：显示时间（转换为本地时间）
                    DateTime localTime = DateTimeOffset.FromUnixTimeSeconds(latestResult.Time).LocalDateTime;
                    Console.WriteLine($"已加载最新OCR结果（时间：{localTime:yyyy-MM-dd HH:mm:ss}）");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载OCR结果失败：{ex.Message}");
                // 可以根据需要在这里设置默认文本
                OcrTextBox.Text = "加载OCR结果失败";
            }
        }

        // AI校对
        private async void CheckApi_Click(object sender, RoutedEventArgs e)
        {
            Button checkButton = sender as Button;
            checkButton.IsEnabled = false;
            string originalContent = checkButton.Content.ToString();
            checkButton.Content = "正在校对...";
            //Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                string apiKey = ConfigData.Instance.DsApiKey;
                string apiUrl = "https://api.deepseek.com/chat/completions";
                string userInput = OcrTextBox.Text;

                if (string.IsNullOrWhiteSpace(userInput))
                {
                    MessageBox.Show("识别结果为空，无法进行 AI 校对。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var requestBody = new
                {
                    model = "deepseek-chat",
                    messages = new[]
                    {
                //new { role = "system", content = PromptTextBox.Text},
                new { role = "system", content = PromptTextBox.Text + " 只要给我校对好的内容，其他多余的不要给我。" },
                new { role = "user", content = userInput }
            },
                    stream = false
                };

                var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                string json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(apiUrl, content);
                response.EnsureSuccessStatusCode();

                string responseString = await response.Content.ReadAsStringAsync();

                dynamic result = JsonConvert.DeserializeObject(responseString);
                string corrected = result.choices[0].message.content;

                OcrTextBox.Text = corrected.Trim();
                SaveResult();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"AI 校对失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // 恢复界面状态
                checkButton.IsEnabled = true;
                checkButton.Content = originalContent;
                Mouse.OverrideCursor = null;
            }
        }


        // 退出应用程序
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();  // 退出应用
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {

            // 创建并初始化设置窗口
            var settingsWindow = new SettingsWindow
            {
                Owner = this,  // 设置为模态对话框
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            bool? dialogResult = settingsWindow.ShowDialog();
            if (dialogResult == true)
            {
                MessageBox.Show("设置已更新并保存！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
            }
        }

        //prompt失焦保存
        private void PromptTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ConfigData.Instance.Save();
        }

        // 打开历史页面
        private void OpenHistory_Click(object sender, RoutedEventArgs e)
        {
            var historyWindow = new HistoryWindow();
            if (historyWindow.ShowDialog() == true && historyWindow.SelectedRecord != null)
            {
                OcrTextBox.Text = historyWindow.SelectedRecord.Text;
            }
        }

        private void AutoLoadClipboard_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (ConfigData.Instance.IsDetected)
            {
                clip_monitor.StartMonitor();
            }
            else
            {
                clip_monitor.StopMonitor();
            }
        }

        private void LoadClipboard_Click(object sender, RoutedEventArgs e)
        {
            clip_monitor.TryLoadClip(null, null);
        }

        //隐藏界面到托盘
        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized && _isPressX)
            {
                Hide();
                MyTrayIcon.Visibility = Visibility.Visible;
                _isPressX = false;
            }
        }

        private async void RestoreWindow_Click(object sender, RoutedEventArgs e)
        {
            Show();
            WindowState = WindowState.Normal;
            MyTrayIcon.Visibility = Visibility.Collapsed;
            Activate();

            // 异步执行OCR
            if (clip_monitor.newimagecome && ConfigData.Instance.IsAutoOCR)
            {
                await Task.Run(() =>
                {
                    Dispatcher.Invoke(() => StartOcr_Click(null, null));
                });
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_isExiting == false)
            {
                e.Cancel = true;
                _isPressX = true;
                this.WindowState = WindowState.Minimized;
            }
            else
            {
                base.OnClosing(e);
            }
        }

        private void ExitApplication_Click(object sender, RoutedEventArgs e)
        {
            _isExiting = true;
            MyTrayIcon.Dispose();
            Close();
        }

        public static void SetAutoStart(bool enable)
        {
            // 获取应用程序的可执行文件路径
            string appPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

            // 打开注册表项
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey(
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            // 应用名称（确保唯一）
            string appName = "MyFloatingBallApp";

            // 添加开机启动项
            if (enable)
            {

                regKey.SetValue(appName, appPath);
            }
            else
            {
                // 移除开机启动项
                if (regKey.GetValue(appName) != null)
                {
                    regKey.DeleteValue(appName);
                }
            }

            regKey.Close();
        }

    }
}
