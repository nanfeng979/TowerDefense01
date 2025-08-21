using UnityEngine;
using TD.Common.Pooling;

namespace TD.Gameplay.Bullet
{
    /// <summary>
    /// 简易子弹：直线飞行，命中或超时回收。
    /// 仅为池演示，不含伤害/命中判定。
    /// </summary>
    public class Bullet : MonoBehaviour, IPoolable
    {
        public float speed = 15f;
        public float lifeTime = 2f;

        private float _life;
        private System.Action<Bullet> _onDespawn;

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
    }
}
