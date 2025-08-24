using UnityEngine;
using TD.Gameplay.Combat;

namespace TD.Gameplay.Combat
{
    /// <summary>
    /// 简易生命组件：管理 HP 与死亡回调。
    /// </summary>
    public class Health : MonoBehaviour, IDamageable
    {
        public float maxHp = 100f;
        public bool destroyOnDeath = true;

        public float Current { get; private set; }
        public bool IsDead => Current <= 0f;

        public System.Action<Health> OnDeath;
        public System.Action<Health, float> OnDamaged;

        private void Awake()
        {
            Current = Mathf.Max(1f, maxHp);
        }

        public void ResetHP(float newMax)
        {
            maxHp = newMax;
            Current = Mathf.Max(1f, newMax);
        }

        public void Damage(float amount)
        {
            if (IsDead) return;
            Current = Mathf.Max(0f, Current - Mathf.Max(0f, amount));
            OnDamaged?.Invoke(this, amount);
            if (Current <= 0f)
            {
                OnDeath?.Invoke(this);
                if (destroyOnDeath)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
