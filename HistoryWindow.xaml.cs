using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace cztOCR
{
    public partial class HistoryWindow : Window
    {
        private List<OcrRecord> _allRecords = new List<OcrRecord>();
        private int _currentPage = 1;
        private const int ItemsPerPage = 15;
        public OcrRecord SelectedRecord { get; private set; }

        public HistoryWindow()
        {
            InitializeComponent();
            LoadHistoryData();
            UpdateListView();
        }

        private void LoadHistoryData()
        {
            try
            {
                string jsonFilePath = "ocr_results.json";
                if (File.Exists(jsonFilePath))
                {
                    string jsonContent = File.ReadAllText(jsonFilePath);
                    _allRecords = JsonConvert.DeserializeObject<List<OcrRecord>>(jsonContent)?
                        .OrderByDescending(r => r.Time) // 按Unix时间戳降序
                        .ToList() ?? new List<OcrRecord>();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载历史记录失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateListView()
        {
            // 分页数据
            var pagedRecords = _allRecords
                .Skip((_currentPage - 1) * ItemsPerPage)
                .Take(ItemsPerPage)
                .ToList();

            // 绑定到ListView
            HistoryListView.ItemsSource = pagedRecords.Select(r => new
            {
                Label = r.Label,
                Time = DateTimeOffset.FromUnixTimeSeconds(r.Time).LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                OriginalRecord = r // 保留原始记录引用
            }).ToList();

            // 更新分页信息
            PageInfoText.Text = $"第 {_currentPage} 页 / 共 {GetTotalPages()} 页";

            // 按钮状态控制
            PrevPageButton.IsEnabled = _currentPage > 1;
            NextPageButton.IsEnabled = _currentPage < GetTotalPages();
        }

        private int GetTotalPages()
        {
            return (int)Math.Ceiling((double)_allRecords.Count / ItemsPerPage);
        }

        // 事件处理
        private void PrevPage_Click(object sender, RoutedEventArgs e)
        {
            _currentPage--;
            UpdateListView();
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            _currentPage++;
            UpdateListView();
        }

        private void LoadSelected_Click(object sender, RoutedEventArgs e)
        {
            if (HistoryListView.SelectedItem != null)
            {
                // 通过dynamic获取原始记录
                dynamic selectedItem = HistoryListView.SelectedItem;
                SelectedRecord = selectedItem.OriginalRecord as OcrRecord;

                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("请先选择一条记录", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        // 搜索功能
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var keyword = SearchBox.Text.Trim().ToLower();
            if (string.IsNullOrEmpty(keyword))
            {
                UpdateListView();
                return;
            }

            var filtered = _allRecords
                .Where(r => r.Label?.ToLower().Contains(keyword) == true ||
                           r.Text?.ToLower().Contains(keyword) == true)
                .ToList();

            HistoryListView.ItemsSource = filtered
                .Skip((_currentPage - 1) * ItemsPerPage)
                .Take(ItemsPerPage)
                .Select(r => new
                {
                    Label = r.Label,
                    Time = DateTimeOffset.FromUnixTimeSeconds(r.Time).LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    OriginalRecord = r
                }).ToList();
        }

        // 删除选中记录
        private void DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (HistoryListView.SelectedItem == null) return;

            if (MessageBox.Show("确定删除选中记录吗？", "确认",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                dynamic selectedItem = HistoryListView.SelectedItem;
                var recordToDelete = selectedItem.OriginalRecord as OcrRecord;

                _allRecords.Remove(recordToDelete);
                SaveRecordsToFile();
                UpdateListView();
            }
        }

        private void SaveRecordsToFile()
        {
            try
            {
                string json = JsonConvert.SerializeObject(_allRecords, Formatting.Indented);
                File.WriteAllText("ocr_results.json", json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
