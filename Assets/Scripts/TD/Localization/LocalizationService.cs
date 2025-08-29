using System.Collections.Generic;

namespace TD.Core
{
    /// <summary>
    /// 简易本地化服务（内存字典版），可扩展为从 JSON/表格加载。
    /// </summary>
    public class LocalizationService : ILocalizationService
    {
        private string _locale = "zh-CN";
        private readonly Dictionary<string, string> _dict = new Dictionary<string, string>();

        public void SetLocale(string locale)
        {
            _locale = string.IsNullOrEmpty(locale) ? "zh-CN" : locale;
            // 可在此处触发从 IJsonLoader 读取对应 locale 的字典（留空实现）
        }

        public string GetLocale() => _locale;

        public string T(string key, string fallback = null)
        {
            if (string.IsNullOrEmpty(key)) return fallback ?? string.Empty;
            if (_dict.TryGetValue(key, out var val)) return val;
            return fallback ?? key;
        }

        // 可选：对外提供直接注入字典的能力（用于测试/快速填充）
        public void SetDictionary(Dictionary<string, string> map)
        {
            _dict.Clear();
            if (map == null) return;
            foreach (var kv in map) _dict[kv.Key] = kv.Value;
        }
    }
}
