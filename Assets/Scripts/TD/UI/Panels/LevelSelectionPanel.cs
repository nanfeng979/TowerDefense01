using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TD.Core;

namespace TD.UI.Panels
{
    /// <summary>
    /// LevelSelectionPanel：关卡选择面板（最小占位）。
    /// </summary>
    public class LevelSelectionPanel : UIPanel
    {
        public override Task OnShowAsync(object args)
        {
            gameObject.name = "[LevelSelectionPanel]";

            var backBtn = transform.Find("TopBar/BackButton")?.GetComponent<Button>();
            if (backBtn != null)
            {
                backBtn.onClick.RemoveAllListeners();
                backBtn.onClick.AddListener(async () =>
                {
                    if (ServiceContainer.Instance.TryGet<IUIManager>(out var ui))
                    {
                        await ui.PopAsync();
                    }
                });
            }

            // 临时：为 Content 下的第一个按钮绑定“进入第一关”
            var content = transform.Find("ScrollView/Viewport/Content");
            if (content != null)
            {
                var firstButton = content.GetComponentInChildren<Button>();
                if (firstButton != null)
                {
                    firstButton.onClick.RemoveAllListeners();
                    firstButton.onClick.AddListener(async () =>
                    {
                        if (GameController.Instance != null)
                        {
                            await GameController.Instance.EnterLevel(1);
                        }
                    });
                }
            }

            return base.OnShowAsync(args);
        }
    }
}
