using System.Collections.Generic;
using UnityEngine;

namespace TD.Gameplay.Enemy
{
    /// <summary>
    /// 运行时敌人注册表，便于目标选择与查询最近目标。
    /// 采用静态注册，简单可靠。
    /// </summary>
    public static class EnemyRegistry
    {
        private static readonly List<EnemyAgent> _enemies = new List<EnemyAgent>();

        public static IReadOnlyList<EnemyAgent> All => _enemies;

        internal static void Add(EnemyAgent a)
        {
            if (a != null && !_enemies.Contains(a)) _enemies.Add(a);
        }
        internal static void Remove(EnemyAgent a)
        {
            if (a != null) _enemies.Remove(a);
        }

        public static EnemyAgent GetClosest(Vector3 pos, float maxRange)
        {
            EnemyAgent best = null;
            float bestSqr = maxRange * maxRange;
            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                var e = _enemies[i];
                if (e == null) { _enemies.RemoveAt(i); continue; }
                var dSqr = (e.transform.position - pos).sqrMagnitude;
                if (dSqr <= bestSqr)
                {
                    bestSqr = dSqr;
                    best = e;
                }
            }
            return best;
        }
    }
}
