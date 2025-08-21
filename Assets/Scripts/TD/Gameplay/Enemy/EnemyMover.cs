using System.Collections.Generic;
using UnityEngine;
using TD.Core;
using TD.Config;

namespace TD.Gameplay.Enemy
{
    /// <summary>
    /// 敌人沿 LevelConfig 的 pathId 路径移动的演示。
    /// 仅演示对象池与路径跟随，不含生命/伤害。
    /// </summary>
    public class EnemyMover : MonoBehaviour
    {
        public string pathId = "p_main";
        public float speed = 2f;

        private List<Vector3> _waypoints;
        private int _index;

        private async void Start()
        {
            var config = ServiceContainer.Instance.Get<IConfigService>();
            var level = await config.GetLevelAsync("001");
            var path = level.paths.Find(p => p.id == pathId);
            if (path != null)
            {
                _waypoints = new List<Vector3>(path.waypoints.Count);
                foreach (var v in path.waypoints) _waypoints.Add(v.ToVector3());
                transform.position = _waypoints[0];
                _index = 0;
            }
        }

        private void Update()
        {
            if (_waypoints == null || _waypoints.Count == 0) return;
            if (_index >= _waypoints.Count - 1) return;

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
    }
}
