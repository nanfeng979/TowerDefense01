using UnityEngine;
using TD.Gameplay.Enemy;
using TD.Gameplay.Combat;

namespace TD.Gameplay.Enemy
{
    /// <summary>
    /// 敌人占位脚本：注册到 EnemyRegistry，并在死亡时自动移除。
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class EnemyAgent : MonoBehaviour
    {
        private Health _hp;

        private void Awake()
        {
            _hp = GetComponent<Health>();
            _hp.OnDeath += _ => EnemyRegistry.Remove(this);
        }

        private void OnEnable()
        {
            EnemyRegistry.Add(this);
        }

        private void OnDisable()
        {
            EnemyRegistry.Remove(this);
        }
    }
}
