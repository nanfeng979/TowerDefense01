using UnityEngine;

namespace TD.Gameplay.Enemy
{
    /// <summary>
    /// 敌人移动速度 Debuff 聚合器：
    /// - 记录基础速度（首次捕获 EnemyMover.speed）
    /// - 应用全局 add/mult 修饰，且不低于基础速度的 20%
    /// - 适应对象池：OnEnable 时重新应用，OnDisable 时复原
    /// </summary>
    [RequireComponent(typeof(EnemyMover))]
    public class EnemySpeedDebuff : MonoBehaviour
    {
        private EnemyMover _mover;
        private bool _captured;
        private float _baseSpeed;
        private float _globalAdd;
        private float _globalMult = 1f;

        private void Awake()
        {
            _mover = GetComponent<EnemyMover>();
        }

        private void OnEnable()
        {
            CaptureBaseIfNeeded();
            Apply();
        }

        private void OnDisable()
        {
            // 复原为基础速度，避免跨回合残留
            if (_captured && _mover != null)
            {
                _mover.speed = _baseSpeed;
            }
        }

        private void CaptureBaseIfNeeded()
        {
            if (_mover == null) return;
            if (!_captured)
            {
                _baseSpeed = Mathf.Max(0.01f, _mover.speed);
                _captured = true;
            }
        }

        public void SetGlobalModifiers(float add, float mult)
        {
            _globalAdd = add;
            _globalMult = mult;
            CaptureBaseIfNeeded();
            Apply();
        }

        private void Apply()
        {
            if (_mover == null || !_captured) return;
            float target = _baseSpeed * _globalMult + _globalAdd;
            float floor = _baseSpeed * 0.2f; // 不低于初始速度 20%
            _mover.speed = Mathf.Max(floor, target);
        }
    }
}
