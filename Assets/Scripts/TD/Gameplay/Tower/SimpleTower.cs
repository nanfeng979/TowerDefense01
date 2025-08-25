using UnityEngine;
using TD.Gameplay.Enemy;
using BulletComponent = TD.Gameplay.Bullet.Bullet;
using TD.Core;
using TD.Common.Pooling;

namespace TD.Gameplay.Tower
{
    /// <summary>
    /// 最简塔：在射程内锁定最近敌人并朝其发射子弹预制，使用 PoolService。
    /// </summary>
    public class SimpleTower : MonoBehaviour
    {
        public float range = 8f;
        public float fireRate = 1.5f;
        public GameObject bulletPrefab;
        public string poolKey = "tower_bullet";

        private GameObjectPool _pool;
        private float _timer;

        private void Start()
        {
            if (!ServiceContainer.Instance.TryGet<PoolService>(out var poolSvc))
            {
                enabled = false;
                Debug.LogError("[SimpleTower] PoolService not registered (Bootstrapper missing?)");
                return;
            }
            _pool = poolSvc.GetOrCreate(poolKey, bulletPrefab, transform, prewarm: 8);
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer < 1f / Mathf.Max(0.01f, fireRate)) return;

            var target = EnemyRegistry.GetClosest(transform.position, range);
            if (target == null) return;

            _timer = 0f;
            var dir = (target.transform.position - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            var rot = Quaternion.LookRotation(dir.normalized, Vector3.up);

            var go = _pool.Spawn(transform.position, rot, transform);
            var bullet = go.GetComponent<BulletComponent>();
            if (bullet != null)
            {
                bullet.Setup(OnBulletTimeout);
            }
            // Bullet: 直线飞行，命中或超时通过回调回收
        }

        private void OnBulletTimeout(BulletComponent bullet)
        {
            _pool.Despawn(bullet.gameObject);
        }
    }
}
