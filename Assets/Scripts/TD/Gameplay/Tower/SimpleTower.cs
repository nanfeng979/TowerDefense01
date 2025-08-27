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

        [Header("Obstruction Check")]
        [Tooltip("发射前检测射线是否被其他防御塔阻挡（避免子弹打到己方塔）")] public bool checkLineOfFire = true;
        [Tooltip("射线起点抬高（避免贴地碰撞）")] public float rayStartHeight = 0.5f;
        [Tooltip("距离比较的冗余")] public float pathEpsilon = 0.05f;
        [Tooltip("射线检测的层遮罩（用于限制只检测塔所在层）。默认检测所有层")] public LayerMask lineOfFireMask = ~0;
        public enum ObstructionCastType { Ray, Sphere }
        [Tooltip("阻挡检测的投射类型：Ray（射线）或 Sphere（球体射线，更稳健）")] public ObstructionCastType obstructionCast = ObstructionCastType.Sphere;
        [Tooltip("当使用 SphereCast 时的半径")] public float obstructionRadius = 0.25f;
        [Tooltip("是否包含 Trigger 碰撞体（有些占位或塔可能使用 Trigger）")] public bool includeTriggers = false;
        [Tooltip("阻挡检测的最大命中缓存尺寸（使用 NonAlloc，减少GC）")][Range(4, 64)] public int obstructionMaxHits = 12;

        [Header("Visualization")]
        [Tooltip("是否在运行时显示攻击范围圆")]
        public bool showRange = true;
        [Tooltip("范围圆段数（越高越圆滑）")][Range(8, 128)] public int rangeSegments = 48;
        [Tooltip("范围圆宽度")] public float rangeLineWidth = 0.04f;
        [Tooltip("范围圆颜色")] public Color rangeColor = new Color(0f, 1f, 0f, 0.35f);
        [Tooltip("范围圆 Y 偏移（避免与地面 Z-fight）")] public float rangeYOffset = 0.02f;

        [Tooltip("是否在开火时渲染射线")]
        public bool showShotRay = true;
        [Tooltip("射线宽度")] public float shotRayWidth = 0.05f;
        [Tooltip("射线显示时长（秒）")] public float shotRayDuration = 0.12f;
        [Tooltip("射线颜色")] public Color shotRayColor = new Color(1f, 0.92f, 0.16f, 0.9f);

        private GameObjectPool _pool;
        private float _timer;
        private LineRenderer _rangeLR;
        private LineRenderer _shotLR;
        private float _lastRange = -1f;
        private bool _pendingShot = false; // 冷却完成后置为 true，直到真正发射才复位
        private RaycastHit[] _hitsBuf;

        private void Start()
        {
            if (!ServiceContainer.Instance.TryGet<PoolService>(out var poolSvc))
            {
                enabled = false;
                Debug.LogError("[SimpleTower] PoolService not registered (Bootstrapper missing?)");
                return;
            }
            _pool = poolSvc.GetOrCreate(poolKey, bulletPrefab, transform, prewarm: 8);
            _hitsBuf = new RaycastHit[Mathf.Max(4, obstructionMaxHits)];

            EnsureRangeRenderer();
            UpdateRangeRenderer();
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            // 动态更新范围可视化
            if (showRange)
            {
                if (_rangeLR != null && (Mathf.Abs(_lastRange - range) > 0.001f || !_rangeLR.enabled))
                {
                    UpdateRangeRenderer();
                }
            }
            else if (_rangeLR != null && _rangeLR.enabled)
            {
                _rangeLR.enabled = false;
            }

            // 冷却：当达到阈值后进入待发射状态，但不立即消耗冷却，直到真正发射
            if (!_pendingShot)
            {
                if (_timer < 1f / Mathf.Max(0.01f, fireRate)) return;
                _pendingShot = true;
            }

            // 持续尝试：找到可攻击目标，且线路无阻挡时才发射
            var target = EnemyRegistry.GetClosest(transform.position, range);
            if (target == null) return; // 下一帧继续尝试

            var origin = transform.position + Vector3.up * rayStartHeight;
            var targetPos = target.transform.position + Vector3.up * rayStartHeight;
            var dir = (targetPos - origin);
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            var rot = Quaternion.LookRotation(dir.normalized, Vector3.up);

            if (checkLineOfFire)
            {
                if (!HasClearLineOfFire(origin, targetPos, out var blockedPoint))
                {
                    if (showShotRay) DrawShotRay(origin, blockedPoint);
                    return; // 保持 _pendingShot 为 true，下一帧继续检测
                }
            }

            if (showShotRay) DrawShotRay(origin, targetPos);

            var go = _pool.Spawn(transform.position, rot, transform);
            var bullet = go.GetComponent<BulletComponent>();
            if (bullet != null)
            {
                bullet.Setup(OnBulletTimeout);
            }
            // 真正发射后，重置冷却与待发射状态
            _timer = 0f;
            _pendingShot = false;
        }

        private bool HasClearLineOfFire(Vector3 origin, Vector3 targetPos, out Vector3 blockedPoint)
        {
            blockedPoint = targetPos;
            float dist = Vector3.Distance(origin, targetPos);
            if (dist <= 0.001f) return true;
            var dirN = (targetPos - origin).normalized;
            var qti = includeTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;

            if (_hitsBuf == null || _hitsBuf.Length != Mathf.Max(4, obstructionMaxHits))
            {
                _hitsBuf = new RaycastHit[Mathf.Max(4, obstructionMaxHits)];
            }

            int hitCount;
            if (obstructionCast == ObstructionCastType.Sphere)
            {
                hitCount = Physics.SphereCastNonAlloc(origin, Mathf.Max(0.01f, obstructionRadius), dirN, _hitsBuf, dist, lineOfFireMask, qti);
            }
            else
            {
                hitCount = Physics.RaycastNonAlloc(origin, dirN, _hitsBuf, dist, lineOfFireMask, qti);
            }

            if (hitCount <= 0) return true;

            // 选择最近的有效阻挡（忽略自身塔与敌人）
            float closest = float.MaxValue;
            bool blocked = false;
            for (int i = 0; i < hitCount; i++)
            {
                var h = _hitsBuf[i];
                // 忽略敌人
                var enemy = h.collider.GetComponentInParent<EnemyAgent>();
                if (enemy != null) continue;
                // 忽略自身
                var self = h.collider.GetComponentInParent<SimpleTower>();
                if (self != null && self == this) continue;
                if (self != null)
                {
                    var d = h.distance;
                    if (d < closest)
                    {
                        closest = d;
                        blockedPoint = h.point;
                        blocked = true;
                    }
                }
            }

            return !blocked;
        }

        private void EnsureRangeRenderer()
        {
            if (_rangeLR == null)
            {
                var go = new GameObject("[RangeCircle]");
                go.transform.SetParent(transform, false);
                _rangeLR = go.AddComponent<LineRenderer>();
                _rangeLR.useWorldSpace = false;
                _rangeLR.loop = true;
                _rangeLR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _rangeLR.receiveShadows = false;
                _rangeLR.material = new Material(Shader.Find("Sprites/Default"));
                _rangeLR.startColor = _rangeLR.endColor = rangeColor;
                _rangeLR.startWidth = _rangeLR.endWidth = rangeLineWidth;
            }
        }

        private void UpdateRangeRenderer()
        {
            if (_rangeLR == null) return;
            _rangeLR.enabled = showRange;
            _rangeLR.positionCount = Mathf.Max(8, rangeSegments);
            _rangeLR.startWidth = _rangeLR.endWidth = rangeLineWidth;
            _rangeLR.startColor = _rangeLR.endColor = rangeColor;
            var cnt = _rangeLR.positionCount;
            float r = Mathf.Max(0.01f, range);
            for (int i = 0; i < cnt; i++)
            {
                float t = i / (float)cnt * Mathf.PI * 2f;
                float x = Mathf.Cos(t) * r;
                float z = Mathf.Sin(t) * r;
                _rangeLR.SetPosition(i, new Vector3(x, rangeYOffset, z));
            }
            _lastRange = range;
        }

        private void EnsureShotRenderer()
        {
            if (_shotLR == null)
            {
                var go = new GameObject("[ShotRay]");
                go.transform.SetParent(transform, false);
                _shotLR = go.AddComponent<LineRenderer>();
                _shotLR.useWorldSpace = true;
                _shotLR.loop = false;
                _shotLR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _shotLR.receiveShadows = false;
                _shotLR.material = new Material(Shader.Find("Sprites/Default"));
                _shotLR.startColor = _shotLR.endColor = shotRayColor;
                _shotLR.startWidth = _shotLR.endWidth = shotRayWidth;
                _shotLR.positionCount = 2;
                _shotLR.enabled = false;
            }
        }

        private void DrawShotRay(Vector3 a, Vector3 b)
        {
            EnsureShotRenderer();
            StopCoroutineSafe(_shotHideCR);
            _shotLR.startColor = _shotLR.endColor = shotRayColor;
            _shotLR.startWidth = _shotLR.endWidth = shotRayWidth;
            _shotLR.SetPosition(0, a);
            _shotLR.SetPosition(1, b);
            _shotLR.enabled = true;
            _shotHideCR = StartCoroutine(HideShotAfter(shotRayDuration));
        }

        private System.Collections.IEnumerator HideShotAfter(float t)
        {
            yield return new WaitForSeconds(t);
            if (_shotLR != null) _shotLR.enabled = false;
            _shotHideCR = null;
        }

        private void StopCoroutineSafe(Coroutine cr)
        {
            if (cr != null) StopCoroutine(cr);
        }

        private Coroutine _shotHideCR;

        private void OnBulletTimeout(BulletComponent bullet)
        {
            _pool.Despawn(bullet.gameObject);
        }
    }
}
