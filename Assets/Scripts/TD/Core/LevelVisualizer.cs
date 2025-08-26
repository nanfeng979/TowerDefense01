using System.Linq;
using UnityEngine;
using TD.Config;
using TD.Common;

namespace TD.Core
{
    /// <summary>
    /// 读取 LevelConfig 并在场景中用 Gizmos 可视化网格、路径与建造点。
    /// 将本脚本挂到场景中任意物体；在 Inspector 指定 levelId（短格式，如 "001"）。
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

        [System.Serializable]
        public class ValidationSummary
        {
            public string levelId;
            public int elementsCount;
            public int towersCount;
            public int enemiesCount;
            public int pathsCount;
            public int buildSlotsCount;
            public int roundsCount;
            public int issuesCount;
            public string lastCheckedAt; // ISO8601
        }

        [SerializeField] private ValidationSummary _summary;
        [SerializeField] private string[] _issues;
        private bool _validating;

        public ValidationSummary Summary => _summary;
        public string[] Issues => _issues;

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
#if UNITY_EDITOR
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
#endif
        }

        [ContextMenu("Validate Now")]
        public async void RefreshValidation()
        {
            if (_validating) return;
            _validating = true;
            try
            {
                if (_config == null)
                    _config = new ConfigService(new StreamingAssetsJsonLoader());

                var elements = await _config.GetElementsAsync();
                var towers = await _config.GetTowersAsync();
                var enemies = await _config.GetEnemiesAsync();
                var level = _level ?? await _config.GetLevelAsync(levelId);

                var issuesList = new System.Collections.Generic.List<string>();
                if (level?.rounds?.list != null)
                {
                    foreach (var r in level.rounds.list)
                    {
                        if (r.enemies == null) continue;
                        foreach (var id in r.enemies)
                        {
                            bool enemyExists = enemies.enemies.Exists(e => e.id == id);
                            if (!enemyExists)
                                issuesList.Add($"Enemy not found: {id} (round {r.round})");
                        }
                    }
                }

                _summary = new ValidationSummary
                {
                    levelId = level?.levelId,
                    elementsCount = elements?.elements?.Count ?? 0,
                    towersCount = towers?.towers?.Count ?? 0,
                    enemiesCount = enemies?.enemies?.Count ?? 0,
                    pathsCount = level?.path != null ? 1 : 0,
                    buildSlotsCount = level?.buildSlots?.Count ?? 0,
                    roundsCount = level?.rounds?.list?.Count ?? 0,
                    issuesCount = issuesList.Count,
                    lastCheckedAt = System.DateTime.Now.ToString("s")
                };
                _issues = issuesList.ToArray();

#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.SceneView.RepaintAll();
#endif
            }
            finally
            {
                _validating = false;
            }
        }

        private void OnDrawGizmos()
        {
            if (_level == null) return;

            float cs = 1f;
            if (_level.grid != null)
                cs = Mathf.Max(0.1f, _level.grid.cellSize);

            // 网格
            if (_level.grid != null && _level.grid.showGizmos)
            {
                if (ColorUtil.TryParseRgbaHex(_level.grid.gizmoColor, out var gridCol))
                    Gizmos.color = gridCol;
                else
                    Gizmos.color = Color.green;

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
            if (_level.path != null)
            {
                Gizmos.color = pathColor;
                var p = _level.path;
                for (int i = 0; i < p.waypoints.Count - 1; i++)
                {
                    var av = p.waypoints[i].ToVector3();
                    var bv = p.waypoints[i + 1].ToVector3();
                    var a = transform.position + new Vector3(av.x * cs, av.y, av.z * cs);
                    var b = transform.position + new Vector3(bv.x * cs, bv.y, bv.z * cs);
                    Gizmos.DrawLine(a, b);
                    Gizmos.DrawSphere(a, 0.1f);
                }
                if (p.waypoints.Count > 0)
                {
                    var lv = p.waypoints.Last().ToVector3();
                    Gizmos.DrawSphere(transform.position + new Vector3(lv.x * cs, lv.y, lv.z * cs), 0.1f);
                }
            }

            // 建造点
            if (_level.buildSlots != null)
            {
                Gizmos.color = slotColor;
                foreach (var s in _level.buildSlots)
                {
                    if (s.type != "ground") continue; // 当前仅支持 ground
                    var pos = transform.position + new Vector3(s.x * cs, s.y, s.z * cs);
                    Gizmos.DrawCube(pos + Vector3.up * 0.25f, new Vector3(0.8f, 0.5f, 0.8f) * 0.9f);
                }
            }
        }
    }
}
