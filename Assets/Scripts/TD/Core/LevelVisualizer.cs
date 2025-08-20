using System.Linq;
using UnityEngine;
using TD.Config;
using TD.Common;

namespace TD.Core
{
    /// <summary>
    /// 读取 LevelConfig 并在场景中用 Gizmos 可视化网格、路径与建造点。
    /// 将本脚本挂到场景中任意物体；在 Inspector 指定 levelId。
    /// </summary>
    [ExecuteAlways]
    public class LevelVisualizer : MonoBehaviour
    {
        [Header("Config")]
        public string levelId = "001";

        [Header("Colors (fallback)")]
        public Color pathColor = new Color(1f, 0.92f, 0.016f, 1f); // 黄色
        public Color slotColor = new Color(0.2f, 0.8f, 1f, 0.8f);    // 青色

        private IConfigService _config;
        private LevelConfig _level;

        private void OnEnable()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update += EditorTick;
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= EditorTick;
#endif
        }

        private async void EditorTick()
        {
            // 在编辑器下懒加载配置，避免频繁 IO
            if (_level == null && !Application.isPlaying)
            {
                if (_config == null)
                {
                    _config = new ConfigService(new StreamingAssetsJsonLoader());
                }
                _level = await _config.GetLevelAsync(levelId);
                UnityEditor.SceneView.RepaintAll();
            }
        }

        private void OnDrawGizmos()
        {
            if (_level == null) return;

            // 网格
            if (_level.grid != null && _level.grid.showGizmos)
            {
                if (ColorUtil.TryParseRgbaHex(_level.grid.gizmoColor, out var gridCol))
                    Gizmos.color = gridCol;
                else
                    Gizmos.color = Color.green;

                float cs = Mathf.Max(0.1f, _level.grid.cellSize);
                int w = Mathf.Max(1, _level.grid.width);
                int h = Mathf.Max(1, _level.grid.height);

                Vector3 origin = transform.position; // 以挂载物体为关卡原点
                for (int x = 0; x <= w; x++)
                {
                    Vector3 a = origin + new Vector3(x * cs, 0, 0);
                    Vector3 b = origin + new Vector3(x * cs, 0, h * cs);
                    Gizmos.DrawLine(a, b);
                }
                for (int z = 0; z <= h; z++)
                {
                    Vector3 a = origin + new Vector3(0, 0, z * cs);
                    Vector3 b = origin + new Vector3(w * cs, 0, z * cs);
                    Gizmos.DrawLine(a, b);
                }
            }

            // 路径
            if (_level.paths != null)
            {
                Gizmos.color = pathColor;
                foreach (var p in _level.paths)
                {
                    for (int i = 0; i < p.waypoints.Count - 1; i++)
                    {
                        var a = transform.position + p.waypoints[i].ToVector3();
                        var b = transform.position + p.waypoints[i + 1].ToVector3();
                        Gizmos.DrawLine(a, b);
                        Gizmos.DrawSphere(a, 0.1f);
                    }
                    if (p.waypoints.Count > 0)
                        Gizmos.DrawSphere(transform.position + p.waypoints.Last().ToVector3(), 0.1f);
                }
            }

            // 建造点
            if (_level.buildSlots != null)
            {
                Gizmos.color = slotColor;
                foreach (var s in _level.buildSlots)
                {
                    if (s.type != "ground") continue; // 当前仅支持 ground
                    var pos = transform.position + new Vector3(s.x, s.y, s.z);
                    Gizmos.DrawCube(pos + Vector3.up * 0.25f, new Vector3(0.8f, 0.5f, 0.8f));
                }
            }
        }
    }
}
