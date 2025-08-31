using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TD.Core;
using TD.UI.Panels;

namespace TD.UI.Panels
{
    /// <summary>
    /// ConfirmDialogPanel：通用确认对话框面板（最小占位）。
    /// </summary>
    public class ConfirmDialogPanel : UIPanel
    {
        public override Task OnShowAsync(object args)
        {
            gameObject.name = "[ConfirmDialogPanel]";
            IsModal = true;

            // 暂停游戏
            if (GameController.Instance != null)
                GameController.Instance.Pause();

            var confirmBtn = transform.Find("ConfirmButton")?.GetComponent<Button>();
            var cancelBtn = transform.Find("CancelButton")?.GetComponent<Button>();

            if (confirmBtn != null)
            {
                confirmBtn.onClick.RemoveAllListeners();
                confirmBtn.onClick.AddListener(async () =>
                {
                    // 确认退出关卡
                    if (GameController.Instance != null)
                        GameController.Instance.Resume();

                    if (ServiceContainer.Instance.TryGet<IUIManager>(out var ui))
                    {
                        // 关闭对话框
                        await ui.PopAsync();
                        // 关闭 HUD（如果在栈顶且为非模态）
                        if (ui.Top != null && ui.Top != this && !ui.Top.IsModal)
                            await ui.PopAsync();
                        // 销毁关卡并回到 LevelSelection
                        await GameController.Instance.ExitLevel();
                        await ui.ReplaceAsync<LevelSelectionPanel>("UI/LevelSelection");
                    }
                });
            }

            if (cancelBtn != null)
            {
                cancelBtn.onClick.RemoveAllListeners();
                cancelBtn.onClick.AddListener(async () =>
                {
                    if (GameController.Instance != null)
                        GameController.Instance.Resume();
                    if (ServiceContainer.Instance.TryGet<IUIManager>(out var ui))
                        await ui.PopAsync();
                });
            }

            return base.OnShowAsync(args);
        }

        public override Task OnHideAsync()
        {
            // 关闭对话框时，如果仍然是暂停状态，则恢复时间（容错）
            if (GameController.Instance != null && GameController.Instance.State == GameController.GameState.Paused)
                GameController.Instance.Resume();
            return base.OnHideAsync();
        }
    }
}
