using System.Threading.Tasks;

namespace TD.UI
{
    /// <summary>
    /// UI 管理器接口：维护 UI 栈并统一界面显示/隐藏。
    /// </summary>
    public interface IUIManager
    {
        int Count { get; }
        UIPanel Top { get; }

        Task<TPanel> PushAsync<TPanel>(string key, object args = null, bool modal = false) where TPanel : UIPanel;
        Task<bool> PopAsync();
        Task ReplaceAsync<TPanel>(string key, object args = null) where TPanel : UIPanel;

        /// <summary>
        /// 路由返回键：优先交给栈顶面板处理，否则尝试 Pop。
        /// </summary>
        void RouteBack();
    }
}
