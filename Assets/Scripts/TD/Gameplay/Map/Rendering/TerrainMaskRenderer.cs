using System.Collections.Generic;
using UnityEngine;
using TD.Config;

namespace TD.Gameplay.Map.Rendering
{
    /// <summary>
    /// 使用 JSON 的 terrain 字段与路径派生掩码渲染 grass/soil，并实例化 props。
    /// </summary>
    public class TerrainMaskRenderer : IMapTerrainRenderer
    {
        public void Render(MapRenderer owner, LevelConfig _level)
        {
            int w = Mathf.Max(1, _level.grid != null ? _level.grid.width : 1);
            int h = Mathf.Max(1, _level.grid != null ? _level.grid.height : 1);
            float cs = Mathf.Max(0.1f, _level.grid != null ? _level.grid.cellSize : 1f);

            owner.EnsureExtraRoots();

            var cellType = new string[w, h];
            // 1) 若存在字符网格 terrainMap，则优先使用它
            if (_level.terrainMap != null && _level.terrainMap.rows != null && _level.terrainMap.rows.Count > 0)
            {
                var legend = new System.Collections.Generic.Dictionary<char, string>();
                if (_level.terrainMap.legend != null)
                {
                    foreach (var entry in _level.terrainMap.legend)
                    {
                        if (entry == null || string.IsNullOrEmpty(entry.key) || string.IsNullOrEmpty(entry.type)) continue;
                        char k = entry.key[0];
                        legend[k] = entry.type.ToLowerInvariant();
                    }
                }
                for (int z = 0; z < h; z++)
                {
                    string row = z < _level.terrainMap.rows.Count ? _level.terrainMap.rows[z] : null;
                    for (int x = 0; x < w; x++)
                    {
                        char key = (row != null && x < (row?.Length ?? 0)) ? row[x] : ' ';
                        if (!legend.TryGetValue(key, out var t)) t = "grass";
                        cellType[x, z] = t;
                    }
                }
            }
            else
            {
                // 2) 否则回退到：default + 路径派生 + overrides
                string defaultType = _level.terrain != null && !string.IsNullOrEmpty(_level.terrain.@default)
                    ? _level.terrain.@default.ToLowerInvariant()
                    : "grass";
                for (int x = 0; x < w; x++)
                    for (int z = 0; z < h; z++)
                        cellType[x, z] = defaultType;

                // 路径派生
                if (_level.path != null && _level.path.waypoints != null && _level.path.waypoints.Count > 1)
                {
                    bool fromPathEnabled = _level.terrain == null || _level.terrain.fromPath == null ? true : _level.terrain.fromPath.enabled;
                    float pathWidthCells = _level.terrain != null && _level.terrain.fromPath != null ? _level.terrain.fromPath.widthInCells : 1.5f;
                    float halfWidth = Mathf.Max(0.05f, pathWidthCells * cs * 0.5f);
                    if (fromPathEnabled)
                    {
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

                // overrides
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
            }

            // 实例化地块
            for (int x = 0; x < w; x++)
            {
                for (int z = 0; z < h; z++)
                {
                    var t = cellType[x, z];
                    GameObject prefab = null;
                    if (t == "soil") prefab = owner.soilQuadPrefab != null ? owner.soilQuadPrefab : owner.grassQuadPrefab;
                    else prefab = owner.grassQuadPrefab != null ? owner.grassQuadPrefab : owner.soilQuadPrefab;
                    if (prefab == null) continue;

                    var go = UnityEngine.Object.Instantiate(prefab, owner.terrainRoot);
                    go.SetActive(true);
                    go.transform.localPosition = new Vector3(x * cs, owner.yOffset, z * cs);
                    var s = go.transform.localScale;
                    go.transform.localScale = new Vector3(cs, s.y, cs);
                }
            }

            // props
            if (_level.props != null)
            {
                foreach (var p in _level.props)
                {
                    if (p == null || string.IsNullOrEmpty(p.type)) continue;
                    var t = p.type.ToLowerInvariant();
                    GameObject prefab = null;
                    switch (t)
                    {
                        case "rock": prefab = owner.rockPrefab; break;
                    }
                    if (prefab == null) continue;
                    var go = UnityEngine.Object.Instantiate(prefab, owner.propsRoot);
                    go.SetActive(true);
                    go.transform.localPosition = new Vector3(p.x * cs, p.y + owner.yOffset, p.z * cs);
                    go.transform.localRotation = Quaternion.Euler(0f, p.rotY, 0f);
                    go.transform.localScale = Vector3.one * (p.scale <= 0f ? 1f : p.scale);
                }
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
