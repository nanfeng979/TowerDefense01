using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TD.Core;

namespace TD.UI.Panels
{
    /// <summary>
    /// MainMenuPanel：首页面板（最小占位）。
    /// </summary>
    public class MainMenuPanel : UIPanel
    {
        public override Task OnShowAsync(object args)
        {
            gameObject.name = "[MainMenuPanel]";

            // 绑定按钮（若预制体中存在）
            var startBtn = transform.Find("StartButton")?.GetComponent<Button>();
            if (startBtn != null)
            {
                startBtn.onClick.RemoveListener(OnStartClicked);
                startBtn.onClick.AddListener(OnStartClicked);
            }
            var exitBtn = transform.Find("ExitButton")?.GetComponent<Button>();
            if (exitBtn != null)
            {
                exitBtn.onClick.RemoveAllListeners();
                exitBtn.onClick.AddListener(() =>
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                });
            }

            return base.OnShowAsync(args);
        }

        private async void OnStartClicked()
        {
            if (ServiceContainer.Instance.TryGet<IUIManager>(out var ui))
            {
                await ui.PushAsync<LevelSelectionPanel>("UI/LevelSelection", modal: false);
            }
        }

        public override bool OnBackRequested()
        {
            // 首页按返回键可选择退出游戏或无操作；此处不消费
            return false;
        }
    }
}
