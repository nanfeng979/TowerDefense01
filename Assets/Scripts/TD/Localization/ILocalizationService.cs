using System.Collections.Generic;

namespace TD.Core
{
    /// <summary>
    /// 统一本地化服务接口，便于跨项目复用与替换。
    /// </summary>
    public interface ILocalizationService
    {
        /// <summary>设置当前语言（如 zh-CN / en-US）。</summary>
        void SetLocale(string locale);
        /// <summary>获取当前语言。</summary>
        string GetLocale();
        /// <summary>翻译 key，若不存在返回 fallback 或 key 本身。</summary>
        string T(string key, string fallback = null);
    }
}
