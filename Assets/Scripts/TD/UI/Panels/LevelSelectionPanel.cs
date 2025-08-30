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

            var backBtn = transform.Find("BackButton")?.GetComponent<Button>();
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

            return base.OnShowAsync(args);
        }
    }
}
