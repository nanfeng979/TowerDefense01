using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using TD.Config;
using TD.Core;
using TD.Common.Pooling;

namespace TD.Gameplay.Spawn
{
    /// <summary>
    /// 从 LevelConfig 读取 waves，按时间与组内间隔逐个生成敌人。
    /// 需在 Inspector 将 enemyId 映射到对应的敌人 Prefab。
    /// </summary>
    public class WaveSpawner : MonoBehaviour
    {
        [Header("Config")]
        public string levelId = "001";
        public string poolKeyPrefix = "enemy_"; // 每种敌人一个池：enemy_<id>
        public Transform enemiesRoot;

        [System.Serializable]
        public class EnemyPrefab
        {
            public string enemyId;
            public GameObject prefab;
        }

        [Header("Enemy Prefabs Mapping")]
        public List<EnemyPrefab> enemyPrefabs = new List<EnemyPrefab>();

        private IConfigService _config;
        private PoolService _poolService;
        private Dictionary<string, GameObjectPool> _pools;
        private Dictionary<string, GameObject> _prefabMap;
        private LevelConfig _level;
        private bool _running;
        private CancellationTokenSource _cts;

        private async void Start()
        {
            if (!Application.isPlaying) { enabled = false; return; }
            if (!ServiceContainer.Instance.TryGet<IConfigService>(out _config))
            {
                Debug.LogError("[WaveSpawner] IConfigService not found. Ensure Bootstrapper exists.");
                enabled = false; return;
            }
            if (!ServiceContainer.Instance.TryGet<PoolService>(out _poolService))
            {
                Debug.LogError("[WaveSpawner] PoolService not found. Ensure Bootstrapper exists.");
                enabled = false; return;
            }

            _level = await _config.GetLevelAsync(levelId);
            if (_level == null)
            {
                Debug.LogError($"[WaveSpawner] Level not found: {levelId}");
                enabled = false; return;
            }

            // 构建映射
            _prefabMap = enemyPrefabs.Where(e => e != null && e.prefab != null && !string.IsNullOrEmpty(e.enemyId))
                                     .ToDictionary(e => e.enemyId, e => e.prefab);
            _pools = new Dictionary<string, GameObjectPool>();

            // 预热每种敌人池（按组内最大需求估计，这里简单给个默认预热 8）
            foreach (var kv in _prefabMap)
            {
                var key = poolKeyPrefix + kv.Key;
                _pools[key] = _poolService.GetOrCreate(key, kv.Value, enemiesRoot, prewarm: 8);
            }

            _cts = new CancellationTokenSource();
            _ = RunWavesAsync(_cts.Token);
        }

        private void OnDisable()
        {
            try { _cts?.Cancel(); } catch { }
            _cts?.Dispose();
            _cts = null;
            _running = false;
        }

        private async Task RunWavesAsync(CancellationToken token)
        {
            if (_running) return; _running = true;
            float startTime = Time.time;

            // 按 LevelConfig.waves 的 startTime 排序
            var waves = _level.waves != null ? _level.waves.OrderBy(w => w.startTime).ToList() : new List<WaveConfig>();
            foreach (var w in waves)
            {
                // 等待到该波开始时间
                float wait = Mathf.Max(0f, w.startTime - (Time.time - startTime));
                if (wait > 0f)
                {
                    try { await Task.Delay((int)(wait * 1000), token); } catch { return; }
                }

                // 对每个组并行调度（组内按 spawnInterval 串行）
                var groupTasks = new List<Task>();
                foreach (var g in w.groups)
                {
                    groupTasks.Add(SpawnGroupAsync(g, token));
                }
                try { await Task.WhenAll(groupTasks); } catch { return; }

                // TODO: 可在此处发放奖励 w.reward
            }
            _running = false;
        }

        private async Task SpawnGroupAsync(SpawnGroup g, CancellationToken token)
        {
            if (g == null || g.count <= 0) return;

            // 路径起点坐标
            var path = _level.paths?.FirstOrDefault(p => p.id == g.pathId);
            if (path == null || path.waypoints == null || path.waypoints.Count == 0)
            {
                Debug.LogWarning($"[WaveSpawner] Path not found or empty: {g.pathId}");
                return;
            }

            // 按 grid.cellSize 缩放路径坐标到世界空间
            float cs = Mathf.Max(0.1f, _level.grid != null ? _level.grid.cellSize : 1f);
            List<Vector3> waypoints = new List<Vector3>(path.waypoints.Count);
            for (int i = 0; i < path.waypoints.Count; i++)
            {
                var v = path.waypoints[i].ToVector3();
                waypoints.Add(new Vector3(v.x * cs, v.y, v.z * cs));
            }

            Vector3 spawnPos = waypoints[0];
            Quaternion spawnRot = Quaternion.LookRotation((waypoints.Count > 1 ? (waypoints[1] - spawnPos) : Vector3.forward).normalized, Vector3.up);

            if (g.delay > 0f)
            {
                try { await Task.Delay((int)(g.delay * 1000), token); } catch { return; }
            }

            string key = poolKeyPrefix + g.enemyId;
            if (!_pools.TryGetValue(key, out var pool))
            {
                if (!_prefabMap.TryGetValue(g.enemyId, out var prefab) || prefab == null)
                {
                    Debug.LogWarning($"[WaveSpawner] Prefab not mapped for enemyId: {g.enemyId}");
                    return;
                }
                pool = _poolService.GetOrCreate(key, prefab, enemiesRoot, prewarm: 0);
                _pools[key] = pool;
            }

            for (int i = 0; i < g.count; i++)
            {
                var go = pool.Spawn(spawnPos, spawnRot);
                // 设置 EnemyMover 的路径（由其在 Start 中按 cellSize 加载路径）
                var mover = go.GetComponent<TD.Gameplay.Enemy.EnemyMover>();
                if (mover != null) mover.pathId = g.pathId;

                if (i < g.count - 1 && g.spawnInterval > 0f)
                {
                    try { await Task.Delay((int)(g.spawnInterval * 1000), token); } catch { return; }
                }
            }
        }
    }
}
