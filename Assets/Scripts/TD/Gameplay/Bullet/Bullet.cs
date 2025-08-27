using UnityEngine;
using TD.Common.Pooling;
using TD.Gameplay.Combat;

namespace TD.Gameplay.Bullet
{
    /// <summary>
    /// 简易子弹：直线飞行，命中或超时回收。
    /// 支持触发命中：需要本体 Collider.IsTrigger=true，目标带有 Health 组件。
    /// </summary>
    public class Bullet : MonoBehaviour, IPoolable
    {
        public float speed = 15f;
        public float lifeTime = 2f;

        private float _life;
        private System.Action<Bullet> _onDespawn;
        public float damage = 10f;

        public void Setup(System.Action<Bullet> onDespawn)
        {
            _onDespawn = onDespawn;
        }

        private void Update()
        {
            transform.position += transform.forward * speed * Time.deltaTime;
            _life += Time.deltaTime;
            if (_life >= lifeTime)
            {
                _onDespawn?.Invoke(this);
            }
        }

        public void OnSpawned()
        {
            _life = 0f;
        }

        public void OnDespawned()
        {
            _life = 0f;
        }

        private void OnTriggerEnter(Collider other)
        {
            // 只对敌人造成伤害：需要 EnemyAgent + IDamageable
            var enemyAgent = other.GetComponentInParent<TD.Gameplay.Enemy.EnemyAgent>();
            if (enemyAgent == null) return;

            var dmg = other.GetComponent<IDamageable>() ?? enemyAgent.GetComponent<IDamageable>();
            if (dmg == null) return;

            dmg.Damage(damage);
            _onDespawn?.Invoke(this);
        }
    }
}
