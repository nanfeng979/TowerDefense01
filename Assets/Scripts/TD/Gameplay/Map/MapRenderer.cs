using System.Collections.Generic;
using UnityEngine;
using TD.Config;
using TD.Core;
using TD.Gameplay.Tower;

namespace TD.Gameplay.Map
{
    /// <summary>
    /// 地图渲染器：根据 LevelConfig 渲染地面网格、路径与建造点。
    /// 将本组件挂到关卡原点物体上，并指定 levelId 与预制体/材质。
    /// </summary>
    public class MapRenderer : MonoBehaviour
    {
        [Header("Config")]
        public string levelId = "001";
        public bool autoGenerateOnStart = true;
        public bool clearBeforeGenerate = true;
        public float yOffset = 0f;

        [Header("Prefabs / Materials")]
        [Tooltip("地面格子预制体（建议 1x1 XZ 单位大小）")] public GameObject groundTilePrefab; // 旧方案：整格地面
        [Tooltip("路径瓦片预制体（建议 1x1 XZ 单位大小）。留空则不单独渲染路径")] public GameObject pathTilePrefab; // 旧方案：路径瓦片
        [Tooltip("建造点占位预制体")] public GameObject buildSlotPrefab;
        public Material pathLineMaterial; // 已废弃（不再使用 LineRenderer）

        [Header("Terrain Prefabs (New)")]
        [Tooltip("草地 Quad 预制体（1x1 单位，已朝上铺在 XZ 平面）")] public GameObject grassQuadPrefab;
        [Tooltip("土地（道路）Quad 预制体（1x1 单位，已朝上铺在 XZ 平面）")] public GameObject soilQuadPrefab;
        [Tooltip("岩石预制体（可选），props 中 type==rock 时使用")] public GameObject rockPrefab;

        [Header("Default Tower On Slots (Optional)")]
        [Tooltip("在每个建造点上自动挂载 SimpleTower 组件")] public bool addSimpleTowerOnSlots = true;
        [Tooltip("默认塔使用的子弹预制体（可为空，为空则组件先禁用）")] public GameObject simpleTowerBulletPrefab;
        [Tooltip("默认塔射程")] public float simpleTowerRange = 8f;
        [Tooltip("默认塔射速（每秒发射次数）")] public float simpleTowerFireRate = 1.5f;
        [Tooltip("对象池键名")] public string simpleTowerPoolKey = "tower_bullet";

        [Header("Rendering Options")]
        [Tooltip("地面瓦片缩放到 cellSize")] public bool scaleGroundToCell = true;
        [Tooltip("路径步进相对 cellSize 的倍数（越小越密）")][Range(0.1f, 2f)] public float pathStepMultiplier = 0.5f;
        [Tooltip("LineRenderer 路径宽度 = cellSize * 此系数")][Range(0.05f, 2f)] public float lineWidthScale = 0.6f;

        [Header("Roots (Auto) ")]
        public Transform groundRoot;
        public Transform pathRoot;
        public Transform slotRoot;
        public Transform terrainRoot;
        public Transform propsRoot;

        private LevelConfig _level;

        private async void Start()
        {
            if (!autoGenerateOnStart) return;
            await GenerateAsync();
        }

        [ContextMenu("Generate Map Now")]
        public async void GenerateNow()
        {
            await GenerateAsync();
        }

        public async System.Threading.Tasks.Task GenerateAsync()
        {
            if (clearBeforeGenerate)
            {
                ClearAll();
            }

            IConfigService config;
            if (!ServiceContainer.Instance.TryGet<IConfigService>(out config))
            {
                // 编辑器/无 Bootstrapper 场景的兜底
                config = new ConfigService(new StreamingAssetsJsonLoader());
            }

            _level = await config.GetLevelAsync(levelId);
            if (_level == null)
            {
                Debug.LogError($"[MapRenderer] Level not found: {levelId}");
                return;
            }

            EnsureRoots();
            // 优先使用新地形系统（若提供了 grass/soil 预制体或 JSON 提供了 terrain 字段）
            bool useNewTerrain = (grassQuadPrefab != null || soilQuadPrefab != null || _level.terrain != null);
            if (useNewTerrain)
            {
                RenderTerrain();
            }
            else
            {
                RenderGround();
                RenderPaths();
            }
            RenderSlots();
            RenderProps();
        }

        private void EnsureRoots()
        {
            if (groundRoot == null) groundRoot = CreateChild("[Ground]");
            if (pathRoot == null) pathRoot = CreateChild("[Path]");
            if (slotRoot == null) slotRoot = CreateChild("[BuildSlots]");
            if (terrainRoot == null) terrainRoot = CreateChild("[Terrain]");
            if (propsRoot == null) propsRoot = CreateChild("[Props]");
        }

        private Transform CreateChild(string name)
        {
            var go = new GameObject(name);
            var t = go.transform;
            t.SetParent(transform, false);
            return t;
        }

        public void ClearAll()
        {
            if (groundRoot != null) DestroyAllChildren(groundRoot);
            if (pathRoot != null) DestroyAllChildren(pathRoot);
            if (slotRoot != null) DestroyAllChildren(slotRoot);
            if (terrainRoot != null) DestroyAllChildren(terrainRoot);
            if (propsRoot != null) DestroyAllChildren(propsRoot);
        }

        private void DestroyAllChildren(Transform root)
        {
            if (root == null) return;
            var toDestroy = new List<GameObject>();
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                toDestroy.Add(root.GetChild(i).gameObject);
            }
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                foreach (var go in toDestroy) DestroyImmediate(go);
                return;
            }
#endif
            foreach (var go in toDestroy) Destroy(go);
        }

        private void RenderGround()
        {
            int w = Mathf.Max(1, _level.grid != null ? _level.grid.width : 1);
            int h = Mathf.Max(1, _level.grid != null ? _level.grid.height : 1);
            float cs = Mathf.Max(0.1f, _level.grid != null ? _level.grid.cellSize : 1f);

            if (groundTilePrefab == null)
            {
                // 无预制体时，生成一个整体地面平面
                var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                plane.name = "GroundPlane";
                plane.transform.SetParent(groundRoot, false);
                // Unity Plane 是 10x10 单位，缩放到格子尺寸
                plane.transform.localScale = new Vector3(w * cs / 10f, 1f, h * cs / 10f);
                plane.transform.localPosition = new Vector3(w * cs * 0.5f, yOffset, h * cs * 0.5f);
                return;
            }

            for (int x = 0; x < w; x++)
            {
                for (int z = 0; z < h; z++)
                {
                    var pos = new Vector3(x * cs, yOffset, z * cs);
                    var tile = Instantiate(groundTilePrefab, groundRoot);
                    tile.transform.localPosition = pos;
                    if (scaleGroundToCell)
                    {
                        tile.transform.localScale = new Vector3(cs, tile.transform.localScale.y, cs);
                    }
                }
            }
        }

        private void RenderPaths()
        {
            if (_level.path == null || _level.path.waypoints == null || _level.path.waypoints.Count == 0) return;

            float cs = Mathf.Max(0.1f, _level.grid != null ? _level.grid.cellSize : 1f);

            var single = _level.path;
            if (pathTilePrefab != null)
            {
                float stepLen = Mathf.Max(0.05f, cs * pathStepMultiplier);
                for (int i = 0; i < single.waypoints.Count - 1; i++)
                {
                    var a = single.waypoints[i].ToVector3();
                    var b = single.waypoints[i + 1].ToVector3();
                    // 按 cellSize 缩放到真实坐标系
                    a = new Vector3(a.x * cs, a.y, a.z * cs);
                    b = new Vector3(b.x * cs, b.y, b.z * cs);

                    var dir = (b - a);
                    float len = new Vector2(dir.x, dir.z).magnitude;
                    if (len < 0.0001f) continue;
                    dir.y = 0f;
                    int steps = Mathf.Max(1, Mathf.CeilToInt(len / stepLen));
                    var fwd = dir.normalized;
                    var rot = Quaternion.LookRotation(new Vector3(fwd.x, 0f, fwd.z), Vector3.up);
                    for (int s = 0; s <= steps; s++)
                    {
                        float t = s / (float)steps;
                        var pos = Vector3.Lerp(a, b, t);
                        var go = Instantiate(pathTilePrefab, pathRoot);
                        go.transform.localPosition = new Vector3(pos.x, yOffset, pos.z);
                        go.transform.localRotation = rot;
                        go.transform.localScale = new Vector3(cs, go.transform.localScale.y, cs);
                    }
                }
            }
            else
            {
                // 不再渲染 LineRenderer 作为回退；未提供 pathTilePrefab 时跳过路径可视化
                return;
            }
        }

        private void RenderSlots()
        {
            if (_level.buildSlots == null || _level.buildSlots.Count == 0) return;
            float cs = Mathf.Max(0.1f, _level.grid != null ? _level.grid.cellSize : 1f);

            foreach (var s in _level.buildSlots)
            {
                var pos = new Vector3(s.x * cs, s.y + yOffset, s.z * cs);

                if (buildSlotPrefab == null)
                {
                    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.name = "BuildSlot";
                    cube.transform.SetParent(slotRoot, false);
                    cube.transform.localScale = new Vector3(0.8f * cs, 0.5f, 0.8f * cs);
                    cube.transform.localPosition = new Vector3(pos.x, pos.y + 0.25f, pos.z);
                    TryAttachSimpleTower(cube);
                }
                else
                {
                    var go = Instantiate(buildSlotPrefab, slotRoot);
                    go.transform.localPosition = pos;
                    TryAttachSimpleTower(go);
                }
            }
        }

        private void TryAttachSimpleTower(GameObject host)
        {
            if (!addSimpleTowerOnSlots || host == null) return;
            // 避免重复添加
            var existing = host.GetComponent<SimpleTower>();
            if (existing != null) return;

            var tower = host.AddComponent<SimpleTower>();
            tower.range = simpleTowerRange;
            tower.fireRate = simpleTowerFireRate;
            tower.poolKey = simpleTowerPoolKey;
            tower.bulletPrefab = simpleTowerBulletPrefab;

            // 若未指定子弹预制体，则先禁用，避免运行时报错；稍后可在实例上手动指定再启用
            if (tower.bulletPrefab == null)
            {
                tower.enabled = false;
            }
        }

        // === New Terrain System ===
        private void RenderTerrain()
        {
            int w = Mathf.Max(1, _level.grid != null ? _level.grid.width : 1);
            int h = Mathf.Max(1, _level.grid != null ? _level.grid.height : 1);
            float cs = Mathf.Max(0.1f, _level.grid != null ? _level.grid.cellSize : 1f);

            // 初始类型：默认 grass
            string defaultType = _level.terrain != null && !string.IsNullOrEmpty(_level.terrain.@default)
                ? _level.terrain.@default.ToLowerInvariant()
                : "grass";

            var cellType = new string[w, h];
            for (int x = 0; x < w; x++)
                for (int z = 0; z < h; z++)
                    cellType[x, z] = defaultType;

            // 从路径派生的土路
            if (_level.path != null && _level.path.waypoints != null && _level.path.waypoints.Count > 1)
            {
                bool fromPathEnabled = _level.terrain == null || _level.terrain.fromPath == null ? true : _level.terrain.fromPath.enabled;
                float pathWidthCells = _level.terrain != null && _level.terrain.fromPath != null ? _level.terrain.fromPath.widthInCells : 1.5f;
                float halfWidth = Mathf.Max(0.05f, pathWidthCells * cs * 0.5f);

                if (fromPathEnabled)
                {
                    // 预计算世界坐标的路径点
                    var wp = _level.path.waypoints;
                    var worldPts = new List<Vector3>(wp.Count);
                    for (int i = 0; i < wp.Count; i++)
                    {
                        var v = wp[i].ToVector3();
                        worldPts.Add(new Vector3(v.x * cs, v.y, v.z * cs));
                    }

                    for (int x = 0; x < w; x++)
                    {
                        for (int z = 0; z < h; z++)
                        {
                            var p = new Vector3((x + 0.5f) * cs, 0f, (z + 0.5f) * cs);
                            // 判断与任一线段的水平距离
                            bool nearPath = false;
                            for (int i = 0; i < worldPts.Count - 1 && !nearPath; i++)
                            {
                                float d = DistPointToSegmentXZ(p, worldPts[i], worldPts[i + 1]);
                                if (d <= halfWidth) nearPath = true;
                            }
                            if (nearPath) cellType[x, z] = "soil";
                        }
                    }
                }
            }

            // overrides 应用（矩形强制类型）
            if (_level.terrain != null && _level.terrain.overrides != null)
            {
                foreach (var ov in _level.terrain.overrides)
                {
                    if (ov == null || ov.rect == null || string.IsNullOrEmpty(ov.type)) continue;
                    string t = ov.type.ToLowerInvariant();
                    int x0 = Mathf.Max(0, ov.rect.x);
                    int z0 = Mathf.Max(0, ov.rect.z);
                    int x1 = Mathf.Min(w, ov.rect.x + ov.rect.w);
                    int z1 = Mathf.Min(h, ov.rect.z + ov.rect.h);
                    for (int x = x0; x < x1; x++)
                        for (int z = z0; z < z1; z++)
                            cellType[x, z] = t;
                }
            }

            // 实例化 Quad
            for (int x = 0; x < w; x++)
            {
                for (int z = 0; z < h; z++)
                {
                    var t = cellType[x, z];
                    GameObject prefab = null;
                    if (t == "soil") prefab = soilQuadPrefab != null ? soilQuadPrefab : grassQuadPrefab;
                    else prefab = grassQuadPrefab != null ? grassQuadPrefab : soilQuadPrefab;
                    if (prefab == null) continue; // 两者都没配则跳过

                    var pos = new Vector3(x * cs, yOffset, z * cs);
                    var go = Instantiate(prefab, terrainRoot);
                    go.transform.localPosition = pos;
                    // 尺寸缩放到 cellSize（仅缩放 X/Z，保留当前 Y 尺寸）
                    var s = go.transform.localScale;
                    go.transform.localScale = new Vector3(cs, s.y, cs);
                }
            }
        }

        private void RenderProps()
        {
            if (_level.props == null || _level.props.Count == 0) return;
            float cs = Mathf.Max(0.1f, _level.grid != null ? _level.grid.cellSize : 1f);
            foreach (var p in _level.props)
            {
                if (p == null || string.IsNullOrEmpty(p.type)) continue;
                string t = p.type.ToLowerInvariant();
                GameObject prefab = null;
                switch (t)
                {
                    case "rock": prefab = rockPrefab; break;
                    default: prefab = null; break;
                }
                if (prefab == null) continue;
                var go = Instantiate(prefab, propsRoot);
                go.transform.localPosition = new Vector3(p.x * cs, p.y + yOffset, p.z * cs);
                go.transform.localRotation = Quaternion.Euler(0f, p.rotY, 0f);
                go.transform.localScale = Vector3.one * (p.scale <= 0f ? 1f : p.scale);
            }
        }

        private float DistPointToSegmentXZ(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector2 p2 = new Vector2(p.x, p.z);
            Vector2 a2 = new Vector2(a.x, a.z);
            Vector2 b2 = new Vector2(b.x, b.z);
            Vector2 ab = b2 - a2;
            float len2 = Vector2.Dot(ab, ab);
            if (len2 < 1e-6f) return Vector2.Distance(p2, a2);
            float t = Vector2.Dot(p2 - a2, ab) / len2;
            t = Mathf.Clamp01(t);
            Vector2 c = a2 + ab * t;
            return Vector2.Distance(p2, c);
        }
    }
}
