using System.Threading.Tasks;
using TMPro;

namespace TD.UI
{
    /// <summary>
    /// 全局 UI 资源服务：负责按配置加载 TMP 字体并提供并发安全的设置方法。
    /// 仅考虑通过 Assets 绝对路径加载（编辑器内有效）。
    /// </summary>
    public interface IUIResourceService
    {
        /// <summary>初始化服务：读取配置并按需预加载默认字体。</summary>
        Task InitializeAsync();

        /// <summary>获取或加载默认字体。</summary>
        Task<TMP_FontAsset> GetOrLoadDefaultFontAsync();

        /// <summary>为文本设置默认字体（若未加载则等待加载完成后再设置）。</summary>
        Task SetDefaultFontAsync(TMP_Text text);

        /// <summary>为文本设置指定 Assets 路径的字体（若未加载则等待加载完成后再设置）。</summary>
        Task SetFontAsync(TMP_Text text, string assetPath);
    }
}
