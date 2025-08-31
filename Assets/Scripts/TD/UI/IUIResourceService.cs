using System.Threading.Tasks;
using TMPro;

namespace TD.UI
{
    /// <summary>
    /// 全局 UI 资源服务。
    /// </summary>
    public interface IUIResourceService
    {
        /// <summary>初始化服务。</summary>
        Task InitializeAsync();

        /// <summary>获取当前默认字体（可能为 null）。</summary>
        TMP_FontAsset GetDefaultFont();

        /// <summary>设置默认字体（同步）。</summary>
        void SetDefaultFont(TMP_FontAsset font);
    }
}
