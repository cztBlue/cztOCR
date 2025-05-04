using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.IO;

namespace cztOCR
{
    public class ConfigData : INotifyPropertyChanged
    {
        private const string ConfigFile = "config.json";

        // 单例托管,INotify不能做成静态类
        private static readonly Lazy<ConfigData> _lazy =
            new Lazy<ConfigData>(() => LoadFromFile());

        public static ConfigData Instance => _lazy.Value;

        // 私有构造，避免外部 new
        private ConfigData() { }

        // 配置属性
        private string _ocrApiKey = "";
        public string OcrApiKey
        {
            get => _ocrApiKey;
            set { if (_ocrApiKey != value) { _ocrApiKey = value; OnPropertyChanged(nameof(OcrApiKey)); } }
        }

        private string _ocrSecretKey = "";
        public string OcrSecretKey
        {
            get => _ocrSecretKey;
            set { if (_ocrSecretKey != value) { _ocrSecretKey = value; OnPropertyChanged(nameof(OcrSecretKey)); } }
        }

        private string _dsApiKey = "";
        public string DsApiKey
        {
            get => _dsApiKey;
            set { if (_dsApiKey != value) { _dsApiKey = value; OnPropertyChanged(nameof(DsApiKey)); } }
        }

        private string _ocrLanguage = "CHN_ENG";
        public string OcrLanguage
        {
            get => _ocrLanguage;
            set { if (_ocrLanguage != value) { _ocrLanguage = value; OnPropertyChanged(nameof(OcrLanguage)); } }
        }

        private string _prompt = "我会给你一些由OCR识别的文本，请你根据文本的语义上下文来校对这些文本。";
        public string Prompt
        {
            get => _prompt;
            set { if (_prompt != value) { _prompt = value; OnPropertyChanged(nameof(Prompt)); } }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string prop) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        /// <summary>保存当前单例到磁盘</summary>
        public void Save()
        {
            var dto = new ConfigDto
            {
                OcrApiKey = this.OcrApiKey,
                OcrSecretKey = this.OcrSecretKey,
                DsApiKey = this.DsApiKey,
                OcrLanguage = this.OcrLanguage,
                Prompt = this.Prompt
            };
            string json = JsonConvert.SerializeObject(dto, Formatting.Indented);
            File.WriteAllText(ConfigFile, json);
        }

        /// <summary>从磁盘加载一个新的 ConfigData 实例</summary>
        private static ConfigData LoadFromFile()
        {
            if (!File.Exists(ConfigFile))
                return new ConfigData();

            try
            {
                string json = File.ReadAllText(ConfigFile);
                var dto = JsonConvert.DeserializeObject<ConfigDto>(json);
                if (dto != null)
                {
                    return new ConfigData
                    {
                        OcrApiKey = dto.OcrApiKey,
                        OcrSecretKey = dto.OcrSecretKey,
                        DsApiKey = dto.DsApiKey,
                        OcrLanguage = dto.OcrLanguage,
                        Prompt = dto.Prompt
                    };
                }
            }
            catch
            {
                // 忽略错误，返回默认配置
            }
            return new ConfigData();
        }

        /// <summary>内部 DTO，用于序列化/反序列化</summary>
        private class ConfigDto
        {
            public string OcrApiKey { get; set; }
            public string OcrSecretKey { get; set; }
            public string DsApiKey { get; set; }
            public string OcrLanguage { get; set; }
            public string Prompt { get; set; }
        }
    }
}
