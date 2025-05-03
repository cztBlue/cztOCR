using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace cztOCR
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            // 将 ConfigData 的值设置到控件
            OcrApiKeyBox.Text = ConfigData.Instance.OcrApiKey;
            OcrSecretKeyBox.Text = ConfigData.Instance.OcrSecretKey;
            DsApiKeyBox.Text = ConfigData.Instance.DsApiKey;
            // 选中 ComboBox
            LanguageCombo.SelectedItem = FindComboItemByContent(ConfigData.Instance.OcrLanguage);
        }

        private ComboBoxItem FindComboItemByContent(string content)
        {
            foreach (ComboBoxItem item in LanguageCombo.Items)
            {
                if ((string)item.Content == content)
                    return item;
            }
            // 默认返回第一项
            return (ComboBoxItem)LanguageCombo.Items[0];
        }

        // 保存按钮
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // 把控件值写入 ConfigData 静态类
            ConfigData.Instance.OcrApiKey = OcrApiKeyBox.Text.Trim();
            ConfigData.Instance.OcrSecretKey = OcrSecretKeyBox.Text.Trim();
            ConfigData.Instance.DsApiKey = DsApiKeyBox.Text.Trim();
            ConfigData.Instance.OcrLanguage = ((ComboBoxItem)LanguageCombo.SelectedItem).Content.ToString();

            // 序列化到 JSON
            try
            {
                ConfigData.Instance.Save();
                DialogResult = true;
            }
            catch (IOException ex)
            {
                MessageBox.Show("保存配置失败: " + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            Close();
        }

        // 取消按钮
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
