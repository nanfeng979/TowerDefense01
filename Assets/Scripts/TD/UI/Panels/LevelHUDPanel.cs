using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TD.Core;

namespace TD.UI.Panels
{
    /// <summary>
    /// 关卡内 HUD 覆盖层。提供关闭按钮，ESC 返回时弹出确认框。
    /// 预制体资源键：UI/LevelHUD
    /// 必备子节点：CloseButton (Button)
    /// </summary>
    public class LevelHUDPanel : UIPanel
    {
        public override Task OnShowAsync(object args)
        {
            gameObject.name = "[LevelHUDPanel]";
            var closeBtn = transform.Find("CloseButton")?.GetComponent<Button>();
            if (closeBtn != null)
            {
                closeBtn.onClick.RemoveAllListeners();
                closeBtn.onClick.AddListener(async () =>
                {
                    if (ServiceContainer.Instance.TryGet<IUIManager>(out var ui))
                    {
                        await ui.PushAsync<ConfirmDialogPanel>("UI/ConfirmDialog", modal: true);
                    }
                });
            }
            return base.OnShowAsync(args);
        }

        public override bool OnBackRequested()
        {
            // 关卡内按返回：打开确认对话框
            _ = ShowConfirmAsync();
            return true; // 已消费
        }

        private async Task ShowConfirmAsync()
        {
            if (ServiceContainer.Instance.TryGet<IUIManager>(out var ui))
            {
                await ui.PushAsync<ConfirmDialogPanel>("UI/ConfirmDialog", modal: true);
            }
        }
    }
}
