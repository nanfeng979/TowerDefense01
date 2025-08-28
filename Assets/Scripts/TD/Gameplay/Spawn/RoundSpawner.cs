using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using TD.Config;
using TD.Common.Pooling;

namespace TD.Gameplay.Spawn
{
    /// <summary>
    /// 从 LevelConfig.rounds 读取回合：按数组顺序生成敌人，使用全局/本回合 spawnInterval；回合间等待 roundInterval。
    /// 需在 Inspector 将 enemyId 映射到对应的敌人 Prefab。
    /// </summary>
    public class RoundSpawner : MonoBehaviour
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
        private TD.Core.PoolService _poolService;
        private TD.Core.RunesService _runesService;
        private Dictionary<string, GameObjectPool> _pools;
        private Dictionary<string, GameObject> _prefabMap;
        private LevelConfig _level;
        private bool _running;
        private CancellationTokenSource _cts;

        private async void Start()
        {
            if (!Application.isPlaying) { enabled = false; return; }
            if (!TD.Core.ServiceContainer.Instance.TryGet<IConfigService>(out _config))
            {
                Debug.LogError("[RoundSpawner] IConfigService not found. Ensure Bootstrapper exists.");
                enabled = false; return;
            }
            if (!TD.Core.ServiceContainer.Instance.TryGet<TD.Core.PoolService>(out _poolService))
            {
                Debug.LogError("[RoundSpawner] PoolService not found. Ensure Bootstrapper exists.");
                enabled = false; return;
            }

            _level = await _config.GetLevelAsync(levelId);
            if (_level == null)
            {
                Debug.LogError($"[RoundSpawner] Level not found: {levelId}");
                enabled = false; return;
            }

            // 通知 RunesService 当前关卡（用于读取 runes 配置与种子）
            TD.Core.ServiceContainer.Instance.TryGet<TD.Core.RunesService>(out _runesService);
            if (_runesService != null)
            {
                await _runesService.SetLevelAsync(_level);
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
            var rounds = _level.rounds;
            var list = rounds != null && rounds.list != null ? rounds.list.OrderBy(r => r.round).ToList() : new List<RoundConfig>();
            float defaultSpawnInterval = rounds != null && rounds.global != null ? Mathf.Max(0f, rounds.global.spawnInterval) : 0.8f;
            float roundInterval = rounds != null && rounds.global != null ? Mathf.Max(0f, rounds.global.roundInterval) : 0f;
            foreach (var r in list)
            {
                float spawnInterval = r.spawnInterval > 0f ? r.spawnInterval : defaultSpawnInterval;
                await SpawnRoundAsync(r, spawnInterval, token);
                // 等待清场：所有敌人被消灭
                while (!token.IsCancellationRequested && TD.Gameplay.Enemy.EnemyRegistry.All.Count > 0)
                {
                    try { await Task.Delay(100, token); } catch { return; }
                }
                // 发放奖励并广播 RoundRewardGranted，然后广播 RoundEnded
                Debug.Log($"[RoundSpawner] Round {r.round} completed, broadcasting events");
                TD.Core.GameEvents.RaiseRoundRewardGranted(r.reward);
                TD.Core.GameEvents.RaiseRoundEnded(r.round);
                // 回合间隔（非最后一波）
                if (!ReferenceEquals(r, list.Last()) && roundInterval > 0f)
                {
                    try { await Task.Delay((int)(roundInterval * 1000), token); } catch { return; }
                }
            }
            _running = false;
        }

        private async Task SpawnRoundAsync(RoundConfig r, float spawnInterval, CancellationToken token)
        {
            if (r == null || r.enemies == null || r.enemies.Count == 0) return;

            // 单一路径
            var path = _level.path;
            if (path == null || path.waypoints == null || path.waypoints.Count == 0)
            {
                Debug.LogWarning($"[RoundSpawner] Level.path missing or empty");
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

            foreach (var enemyId in r.enemies)
            {
                string key = poolKeyPrefix + enemyId;
                if (!_pools.TryGetValue(key, out var pool))
                {
                    if (!_prefabMap.TryGetValue(enemyId, out var prefab) || prefab == null)
                    {
                        Debug.LogWarning($"[RoundSpawner] Prefab not mapped for enemyId: {enemyId}");
                        continue;
                    }
                    pool = _poolService.GetOrCreate(key, prefab, enemiesRoot, prewarm: 0);
                    _pools[key] = pool;
                }

                var go = pool.Spawn(spawnPos, spawnRot);
                var mover = go.GetComponent<TD.Gameplay.Enemy.EnemyMover>();
                if (mover != null) { mover.levelId = levelId; }
                // 敌人生成后广播 EnemySpawned(agent)
                var agent = go.GetComponent<TD.Gameplay.Enemy.EnemyAgent>();
                if (agent != null)
                {
                    TD.Core.GameEvents.RaiseEnemySpawned(agent);
                }

                if (!ReferenceEquals(enemyId, r.enemies.Last()) && spawnInterval > 0f)
                {
                    try { await Task.Delay((int)(spawnInterval * 1000), token); } catch { return; }
                }
            }
        }
    }
}
