using UnityEngine;
using TD.Core;
using TD.Common.Pooling;

namespace TD.Gameplay.Bullet
{
    /// <summary>
    /// 子弹发射演示：按固定频率朝前发射子弹，使用 GameObjectPool。
    /// </summary>
    public class BulletShooter : MonoBehaviour
    {
        public GameObject bulletPrefab;
        public float fireRate = 2f;
        public int prewarm = 16;

        private GameObjectPool _pool;
        private float _timer;

        private void Start()
        {
            if (!ServiceContainer.Instance.TryGet<PoolService>(out var poolSvc))
            {
                Debug.LogError("[BulletShooter] PoolService not registered. Ensure a Bootstrapper exists in the scene and runs in Awake.");
                enabled = false;
                return;
            }
            _pool = poolSvc.GetOrCreate("bullet", bulletPrefab, null, prewarm);
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= 1f / Mathf.Max(0.01f, fireRate))
            {
                _timer = 0f;
                var go = _pool.Spawn(transform.position, transform.rotation);
                var b = go.GetComponent<Bullet>();
                if (b != null)
                {
                    b.Setup(OnBulletTimeout);
                }
            }
        }

        private void OnBulletTimeout(Bullet bullet)
        {
            _pool.Despawn(bullet.gameObject);
        }
    }
}
