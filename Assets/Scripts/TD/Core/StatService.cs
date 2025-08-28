using TD.Common;

namespace TD.Core
{
    /// <summary>
    /// 全局统计聚合器：提供塔相关的全局加成（加法与乘法）。
    /// 敌人减速采用单体 Debuff，不在此聚合。
    /// </summary>
    public class StatService : IInitializable, IDisposableEx
    {
        // 塔射程
        private float _towerRangeAdd = 0f;
        private float _towerRangeMult = 1f;
        // 塔伤害
        private float _towerDamageAdd = 0f;
        private float _towerDamageMult = 1f;

        public void Initialize() { }
        public void Dispose() { }

        public void Clear()
        {
            _towerRangeAdd = 0f; _towerRangeMult = 1f;
            _towerDamageAdd = 0f; _towerDamageMult = 1f;
        }

        public void AddTowerRangeAdd(float v) => _towerRangeAdd += v;
        public void MulTowerRange(float m) => _towerRangeMult *= m;
        public void AddTowerDamageAdd(float v) => _towerDamageAdd += v;
        public void MulTowerDamage(float m) => _towerDamageMult *= m;

        public float GetTowerRangeAdd() => _towerRangeAdd;
        public float GetTowerRangeMult() => _towerRangeMult;
        public float GetTowerDamageAdd() => _towerDamageAdd;
        public float GetTowerDamageMult() => _towerDamageMult;
    }
}
