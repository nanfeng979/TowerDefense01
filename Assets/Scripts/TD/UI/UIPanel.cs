using System.Threading.Tasks;
using UnityEngine;

namespace TD.UI
{
    /// <summary>
    /// UIPanel：所有 UI 面板的抽象基类。
    /// - 提供异步生命周期，便于后续接入过渡动画。
    /// - 提供返回键处理扩展点。
    /// </summary>
    public abstract class UIPanel : MonoBehaviour
    {
        public bool IsModal { get; set; }

        /// <summary>
        /// 面板显示时的回调；返回 Task 以便等待动画。
        /// </summary>
        public virtual Task OnShowAsync(object args)
        {
            gameObject.SetActive(true);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 面板隐藏时的回调；返回 Task 以便等待动画。
        /// </summary>
        public virtual Task OnHideAsync()
        {
            gameObject.SetActive(false);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 返回键请求；返回 true 表示已消费，false 则由 UIManager 处理（默认返回 false）。
        /// </summary>
        public virtual bool OnBackRequested() => false;
    }
}
