using System.Collections.Generic;
using UnityEngine;
using TD.Core;
using TD.Config;

namespace TD.Gameplay.Enemy
{
    /// <summary>
    /// 敌人沿 LevelConfig 的单一路径（level.path）移动的演示。
    /// 仅演示对象池与路径跟随，不含生命/伤害。
    /// </summary>
    public class EnemyMover : MonoBehaviour
    {
    public string levelId = "001";
    public float speed = 2f;

    private List<Vector3> _waypoints;
    private int _index;
    private bool _manualWaypoints;

        private async void Start()
        {
            if (_manualWaypoints) return; // 已手动设置
            var config = ServiceContainer.Instance.Get<IConfigService>();
            var level = await config.GetLevelAsync(levelId);
            var path = level.path;
            if (path != null)
            {
                float cs = Mathf.Max(0.1f, level.grid != null ? level.grid.cellSize : 1f);
                _waypoints = new List<Vector3>(path.waypoints.Count);
                foreach (var v in path.waypoints)
                {
                    var w = v.ToVector3();
                    _waypoints.Add(new Vector3(w.x * cs, w.y, w.z * cs));
                }
                transform.position = _waypoints[0];
                _index = 0;
            }
        }

        private void Update()
        {
            if (_waypoints == null || _waypoints.Count == 0) return;
            if (_index >= _waypoints.Count - 1) 
            {
                // 敌人到达终点，扣除生命值（简化实现）
                Debug.Log("敌人到达终点！");
                
                // 移除自己
                var pool = TD.Core.ServiceContainer.Instance.Get<TD.Core.PoolService>();
                if (pool != null)
                {
                    // 尝试回到池中
                    gameObject.SetActive(false);
                }
                else
                {
                    Destroy(gameObject);
                }
                return;
            }

            var target = _waypoints[_index + 1];
            var pos = transform.position;
            var dir = (target - pos);
            var dist = dir.magnitude;
            if (dist < 0.001f)
            {
                _index++;
                return;
            }
            dir.Normalize();
            transform.position = pos + dir * speed * Time.deltaTime;
            transform.forward = dir;
        }

        /// <summary>
        /// 直接设置路径点（世界坐标）。调用后将忽略 Start 中的自动加载。
        /// </summary>
        public void SetWaypoints(List<Vector3> worldWaypoints)
        {
            if (worldWaypoints == null || worldWaypoints.Count == 0) return;
            _waypoints = worldWaypoints;
            _manualWaypoints = true;
            _index = 0;
            transform.position = _waypoints[0];
        }
    }
}
