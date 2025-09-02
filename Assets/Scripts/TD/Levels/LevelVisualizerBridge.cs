using UnityEngine;

namespace TD.Levels
{
    /// <summary>
    /// 运行时桥接（可选）：如果场景中挂有 TD.Core.LevelVisualizer 并希望在运行时使用 LevelManager 的配置，
    /// 可将本脚本与 LevelManager 同挂。它会在 LevelManager 初始化后把 levelId 传递给可视化器。
    /// </summary>
    public class LevelVisualizerBridge : MonoBehaviour
    {
        private LevelManager _manager;

        private void Awake()
        {
            _manager = GetComponent<LevelManager>();
        }

        private void Update()
        {
            if (_manager == null || _manager.Config == null) return;
            var vis = GetComponent<TD.Core.LevelVisualizer>();
            if (vis != null)
            {
                vis.levelId = _manager.Context?.LevelId ?? vis.levelId;
                enabled = false; // 一次性设置即可
            }
        }
    }
}
