using System.Collections.Generic;
using UnityEngine;
using TD.Config;
using TD.Core;
using TD.Gameplay.Tower;
using TD.Gameplay.Map.Rendering;

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
        public bool autoGenerateOnStart = false;
        public bool clearBeforeGenerate = true;
        public float yOffset = 0f;

        public enum StrategySelection
        {
            Auto,
            NewTerrain,
            LegacyTiles
        }
        [Header("Strategy")]
        [Tooltip("选择渲染策略：Auto 根据资源/配置自动判断；NewTerrain 使用新地形掩码/terrainMap；LegacyTiles 使用旧瓦片渲染")]
        public StrategySelection strategy = StrategySelection.Auto;

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
        [HideInInspector] public Transform groundRoot;
        [HideInInspector] public Transform pathRoot;
        [HideInInspector] public Transform slotRoot;
        [HideInInspector] public Transform terrainRoot;
        [HideInInspector] public Transform propsRoot;

        private LevelConfig _level;

        /// <summary>
        /// 由 LevelManager 调用：传入关卡配置与关卡ID，执行渲染。
        /// </summary>
        public void Setup(string inLevelId, LevelConfig cfg)
        {
            if (clearBeforeGenerate)
            {
                ClearAll();
            }

            levelId = inLevelId;
            _level = cfg;
            if (_level == null)
            {
                Debug.LogError("[MapRenderer] Setup failed: LevelConfig is null");
                return;
            }

            EnsureRoots();
            IMapTerrainRenderer renderer;
            switch (strategy)
            {
                case StrategySelection.NewTerrain:
                    renderer = new TerrainMaskRenderer();
                    break;
                case StrategySelection.LegacyTiles:
                    renderer = new LegacyTilesRenderer();
                    break;
                default:
                    bool useNewTerrain = (grassQuadPrefab != null || soilQuadPrefab != null || _level.terrain != null || _level.terrainMap != null);
                    renderer = useNewTerrain ? (IMapTerrainRenderer)new TerrainMaskRenderer() : new LegacyTilesRenderer();
                    break;
            }
            renderer.Render(this, _level);
            RenderSlots();
            RenderProps();

            Debug.Log($"[MapRenderer] Setup completed: {levelId}");
        }

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
            // 选择并调用渲染策略（直接类型绑定 + 可强制）
            IMapTerrainRenderer renderer;
            switch (strategy)
            {
                case StrategySelection.NewTerrain:
                    renderer = new TerrainMaskRenderer();
                    break;
                case StrategySelection.LegacyTiles:
                    renderer = new LegacyTilesRenderer();
                    break;
                default:
                    bool useNewTerrain = (grassQuadPrefab != null || soilQuadPrefab != null || _level.terrain != null || _level.terrainMap != null);
                    renderer = useNewTerrain ? (IMapTerrainRenderer)new TerrainMaskRenderer() : new LegacyTilesRenderer();
                    break;
            }
            renderer.Render(this, _level);
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

        // 旧的 Ground/Path 渲染已被提取到 LegacyTilesRenderer
        // 新的地形掩码渲染已被提取到 TerrainMaskRenderer

        private void RenderSlots()
        {
            if (_level.buildSlots == null || _level.buildSlots.Count == 0) return;
            float cs = Mathf.Max(0.1f, _level.grid != null ? _level.grid.cellSize : 1f);

            foreach (var s in _level.buildSlots)
            {
                var pos = new Vector3(s.x * cs, s.y + yOffset, s.z * cs);

                if (buildSlotPrefab == null)
                {
                    var towerPrefab = Resources.Load<GameObject>("Game/Tower");
                    if (towerPrefab == null)
                    {
                        Debug.LogError("[MapRenderer] Resources/Game/Tower.prefab 未找到，请将预制体放到 Resources/Game/ 下并命名为 Tower");
                        continue;
                    }
                    var go = Instantiate(towerPrefab, slotRoot);
                    go.transform.localPosition = pos;
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

        // 供策略渲染器调用，确保额外根存在
        public void EnsureExtraRoots()
        {
            if (terrainRoot == null) terrainRoot = CreateChild("[Terrain]");
            if (propsRoot == null) propsRoot = CreateChild("[Props]");
        }

        // 不再使用反射装配渲染策略，类型在编译期绑定
    }
}
