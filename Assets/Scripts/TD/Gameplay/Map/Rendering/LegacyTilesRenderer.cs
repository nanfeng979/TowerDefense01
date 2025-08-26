using UnityEngine;
using TD.Config;

namespace TD.Gameplay.Map.Rendering
{
    /// <summary>
    /// 旧渲染：groundTilePrefab 铺地，pathTilePrefab 沿路径打点。
    /// </summary>
    public class LegacyTilesRenderer : IMapTerrainRenderer
    {
        public void Render(MapRenderer owner, LevelConfig _level)
        {
            // Ground
            int w = Mathf.Max(1, _level.grid != null ? _level.grid.width : 1);
            int h = Mathf.Max(1, _level.grid != null ? _level.grid.height : 1);
            float cs = Mathf.Max(0.1f, _level.grid != null ? _level.grid.cellSize : 1f);

            if (owner.groundTilePrefab == null)
            {
                var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                plane.name = "GroundPlane";
                plane.transform.SetParent(owner.groundRoot, false);
                plane.transform.localScale = new Vector3(w * cs / 10f, 1f, h * cs / 10f);
                plane.transform.localPosition = new Vector3(w * cs * 0.5f, owner.yOffset, h * cs * 0.5f);
            }
            else
            {
                for (int x = 0; x < w; x++)
                {
                    for (int z = 0; z < h; z++)
                    {
                        var pos = new Vector3(x * cs, owner.yOffset, z * cs);
                        var tile = GameObject.Instantiate(owner.groundTilePrefab, owner.groundRoot);
                        tile.transform.localPosition = pos;
                        if (owner.scaleGroundToCell)
                        {
                            tile.transform.localScale = new Vector3(cs, tile.transform.localScale.y, cs);
                        }
                    }
                }
            }

            // Path
            if (_level.path == null || _level.path.waypoints == null || _level.path.waypoints.Count == 0) return;
            if (owner.pathTilePrefab == null) return; // 不再回退到 LineRenderer

            float stepLen = Mathf.Max(0.05f, cs * owner.pathStepMultiplier);
            var single = _level.path;
            for (int i = 0; i < single.waypoints.Count - 1; i++)
            {
                var a = single.waypoints[i].ToVector3();
                var b = single.waypoints[i + 1].ToVector3();
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
                    var go = GameObject.Instantiate(owner.pathTilePrefab, owner.pathRoot);
                    go.transform.localPosition = new Vector3(pos.x, owner.yOffset, pos.z);
                    go.transform.localRotation = rot;
                    go.transform.localScale = new Vector3(cs, go.transform.localScale.y, cs);
                }
            }
        }
    }
}
