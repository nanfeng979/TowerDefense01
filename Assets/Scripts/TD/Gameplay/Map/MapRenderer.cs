using System.Collections.Generic;
using UnityEngine;
using TD.Config;
using TD.Core;

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
        [Tooltip("地面格子预制体（建议 1x1 XZ 单位大小）")] public GameObject groundTilePrefab;
        [Tooltip("路径瓦片预制体（建议 1x1 XZ 单位大小）。留空则使用 LineRenderer 渲染路径")] public GameObject pathTilePrefab;
        [Tooltip("建造点占位预制体")] public GameObject buildSlotPrefab;
        public Material pathLineMaterial;

        [Header("Rendering Options")]
        [Tooltip("地面瓦片缩放到 cellSize")] public bool scaleGroundToCell = true;
        [Tooltip("路径步进相对 cellSize 的倍数（越小越密）")] [Range(0.1f, 2f)] public float pathStepMultiplier = 0.5f;
        [Tooltip("LineRenderer 路径宽度 = cellSize * 此系数")] [Range(0.05f, 2f)] public float lineWidthScale = 0.6f;

        [Header("Roots (Auto) ")]
        public Transform groundRoot;
    public Transform pathRoot;
        public Transform slotRoot;

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
            RenderGround();
            RenderPaths();
            RenderSlots();
        }

        private void EnsureRoots()
        {
            if (groundRoot == null) groundRoot = CreateChild("[Ground]");
            if (pathRoot == null) pathRoot = CreateChild("[Path]");
            if (slotRoot == null) slotRoot = CreateChild("[BuildSlots]");
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
                // 使用 LineRenderer 渲染路径（局部坐标，受父物体变换影响）
                var go = new GameObject($"Path_Line");
                go.transform.SetParent(pathRoot, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = false; // 关键：使用局部坐标
                lr.positionCount = single.waypoints.Count;

                var pts = new Vector3[single.waypoints.Count];
                for (int i = 0; i < single.waypoints.Count; i++)
                {
                    var v = single.waypoints[i].ToVector3();
                    // 按 cellSize 缩放并叠加 yOffset
                    pts[i] = new Vector3(v.x * cs, yOffset + v.y + 0.02f, v.z * cs);
                }
                lr.SetPositions(pts);

                lr.startWidth = lr.endWidth = cs * lineWidthScale;
                lr.material = pathLineMaterial != null ? pathLineMaterial : new Material(Shader.Find("Sprites/Default"));
                lr.textureMode = LineTextureMode.Stretch;
                lr.alignment = LineAlignment.TransformZ;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;
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
                }
                else
                {
                    var go = Instantiate(buildSlotPrefab, slotRoot);
                    go.transform.localPosition = pos;
                }
            }
        }
    }
}
