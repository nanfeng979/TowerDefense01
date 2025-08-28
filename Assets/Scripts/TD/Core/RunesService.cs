using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using TD.Common;
using TD.Config;
using TD.Gameplay.Enemy;

namespace TD.Core
{
    /// <summary>
    /// 符文服务：管理符文池、抽取策略与应用。
    /// 此处提供最小骨架，后续接入 UI 与实际效果应用。
    /// </summary>
    public class RunesService : IInitializable, IDisposableEx
    {
        private LevelConfig _level;
        private RuneSelectionConfig _cfg => _level?.runes;
        private readonly List<string> _remainingPool = new List<string>();
        private readonly Dictionary<string, RuneDef> _defs = new Dictionary<string, RuneDef>();
        private System.Random _rng;
        // 敌人移动速度修饰（全局聚合值）与基础速度缓存
        private float _enemySpeedAdd = 0f;
        private float _enemySpeedMult = 1f;
        private readonly Dictionary<EnemyAgent, float> _enemyBaseSpeeds = new Dictionary<EnemyAgent, float>();

        public void Initialize()
        {
            TD.Core.GameEvents.RoundEnded += OnRoundEnded;
            TD.Core.GameEvents.EnemySpawned += OnEnemySpawned;
        }
        public void Dispose()
        {
            TD.Core.GameEvents.RoundEnded -= OnRoundEnded;
            TD.Core.GameEvents.EnemySpawned -= OnEnemySpawned;
            _remainingPool.Clear();
            _defs.Clear();
            _level = null;
            _rng = null;
            _enemySpeedAdd = 0f; _enemySpeedMult = 1f; _enemyBaseSpeeds.Clear();
        }

        /// <summary>
        /// 设置关卡并预加载符文定义与池。
        /// </summary>
        public async Task SetLevelAsync(LevelConfig level)
        {
            _level = level;
            _remainingPool.Clear();
            _defs.Clear();
            _enemySpeedAdd = 0f; _enemySpeedMult = 1f; _enemyBaseSpeeds.Clear();
            _rng = null;

            if (level != null && level.runes != null && level.runes.poolIds != null)
            {
                _remainingPool.AddRange(level.runes.poolIds);
                Debug.Log($"[RunesService] Initial pool size: {_remainingPool.Count}");
                // RNG
                if (level.runes.useRandomSeed)
                    _rng = new System.Random(level.runes.randomSeed);
                else
                    _rng = new System.Random(Environment.TickCount);

                // 预加载符文定义
                var loader = ServiceContainer.Instance.Get<IJsonLoader>();
                foreach (var id in level.runes.poolIds.Distinct())
                {
                    try
                    {
                        var def = await loader.LoadAsync<RuneDef>($"runes/{id}.json");
                        if (def != null) 
                        {
                            _defs[id] = def;
                            Debug.Log($"[RunesService] Loaded rune: {def.name} ({def.rarity})");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[RunesService] Load rune failed for id={id}: {ex.Message}");
                    }
                }
                Debug.Log($"[RunesService] Loaded {_defs.Count} rune definitions");
            }
        }

        private void OnEnemySpawned(EnemyAgent agent)
        {
            if (agent == null) return;
            ApplyEnemySpeed(agent);
        }

        private void OnRoundEnded(int round)
        {
            Debug.Log($"[RunesService] Round {round} ended");
            if (_level == null || _cfg == null) 
            {
                Debug.Log($"[RunesService] Level or config is null - level: {_level != null}, cfg: {_cfg != null}");
                return;
            }
            var rc = _level.rounds?.list?.FirstOrDefault(x => x.round == round);
            if (rc == null) 
            {
                Debug.Log($"[RunesService] Round config not found for round {round}");
                return;
            }
            if (!rc.offerRunes) 
            {
                Debug.Log($"[RunesService] Round {round} does not offer runes");
                return;
            }
            Debug.Log($"[RunesService] Round {round} should offer runes with rarity: {rc.rarity}");
            // 交给 UI 处理，UI 将调用 GetOffersForRound 和 ChooseRune
            // 这里只是一个钩子，实际 UI 在 RuneSelectionUI 中监听 RoundEnded。
        }

        /// <summary>
        /// 获取某回合可供选择的 3 个（或 0 个）符文。遵循稀有度、重置池与跳过策略。
        /// </summary>
        public List<RuneDef> GetOffersForRound(int round)
        {
            Debug.Log($"[RunesService] GetOffersForRound called for round {round}");
            if (_level == null || _cfg == null) 
            {
                Debug.Log($"[RunesService] Level or config is null in GetOffersForRound");
                return new List<RuneDef>();
            }
            var rc = _level.rounds?.list?.FirstOrDefault(x => x.round == round);
            if (rc == null || !rc.offerRunes) 
            {
                Debug.Log($"[RunesService] No valid round config or offerRunes is false for round {round}");
                return new List<RuneDef>();
            }

            var rarity = string.IsNullOrEmpty(rc.rarity) ? (_cfg.defaultRarity ?? "Common") : rc.rarity;
            Debug.Log($"[RunesService] Target rarity: {rarity}, remaining pool count: {_remainingPool.Count}");
            
            var tiers = new[] { "Epic", "Rare", "Common" }; // 从高到低
            int startIdx = Array.IndexOf(tiers, rarity);
            if (startIdx < 0) startIdx = Array.IndexOf(tiers, "Common");

            // 从剩余池中过滤当前稀有度
            List<string> pickPool = null;
            string selectedTier = null;
            
            for (int i = startIdx; i < tiers.Length; i++)
            {
                var tier = tiers[i];
                pickPool = _remainingPool.Where(id => _defs.TryGetValue(id, out var d) && d.rarity == tier).Distinct().ToList();
                Debug.Log($"[RunesService] Tier {tier} has {pickPool.Count} available runes in remaining pool");
                
                if (pickPool.Count >= 3) 
                {
                    selectedTier = tier;
                    break;
                }
                
                // 当前稀有度不足3个，尝试重置该稀有度的符文池
                var allOfThisTier = _level.runes.poolIds.Where(id => _defs.TryGetValue(id, out var d) && d.rarity == tier).ToList();
                Debug.Log($"[RunesService] Total {tier} runes in config: {allOfThisTier.Count}");
                
                if (allOfThisTier.Count >= 3)
                {
                    Debug.Log($"[RunesService] Resetting {tier} rune pool from {pickPool.Count} to {allOfThisTier.Count} runes");
                    // 重置该稀有度的符文池
                    ResetRarityPool(tier);
                    pickPool = _remainingPool.Where(id => _defs.TryGetValue(id, out var d) && d.rarity == tier).Distinct().ToList();
                    selectedTier = tier;
                    break;
                }
                else if (allOfThisTier.Count < 2)
                {
                    Debug.LogWarning($"[RunesService] Warning: {tier} rarity has only {allOfThisTier.Count} runes configured. Recommend having at least 2 runes per rarity.");
                }
                
                if (!_cfg.autoDowngradeRarity) break;
                // 若允许降级则继续下一 tier
            }

            if (pickPool == null || pickPool.Count < 3)
            {
                Debug.Log($"[RunesService] Not enough runes after trying all tiers, pickPool count: {pickPool?.Count ?? 0}");
                if (_cfg.skipIfInsufficient) 
                {
                    Debug.Log($"[RunesService] Skipping due to insufficient runes");
                    return new List<RuneDef>();
                }
                // 如果不跳过，则尝试混合稀有度从所有剩余中随机挑选
                pickPool = _remainingPool.Distinct().ToList();
                Debug.Log($"[RunesService] Fallback to all remaining: {pickPool.Count} runes");
                
                // 如果仍然不足，尝试重复使用已选过的符文
                if (pickPool.Count < 3)
                {
                    var allIds = _level.runes.poolIds.Where(id => _defs.ContainsKey(id)).ToList();
                    Debug.Log($"[RunesService] Still not enough, using all available runes: {allIds.Count}");
                    pickPool = allIds;
                }
            }

            if (pickPool.Count < 3) 
            {
                Debug.Log($"[RunesService] Still not enough runes after fallback: {pickPool.Count}");
                return new List<RuneDef>();
            }

            var chosenIds = RandomDistinct(pickPool, 3);
            var result = chosenIds.Select(id => _defs[id]).ToList();
            Debug.Log($"[RunesService] Offering {result.Count} runes from {selectedTier ?? "mixed"}: {string.Join(", ", chosenIds)}");
            return result;
        }

        /// <summary>
        /// 选择一个符文并应用其效果，移出池，其它候选保持在池中。
        /// </summary>
        public void ChooseRune(string runeId)
        {
            Debug.Log($"[RunesService] ChooseRune called with id: {runeId}");
            if (string.IsNullOrEmpty(runeId)) return;
            if (!_defs.TryGetValue(runeId, out var def)) 
            {
                Debug.Log($"[RunesService] Rune definition not found for id: {runeId}");
                return;
            }
            // 从池中移除一次（若存在多次，可移除首个）
            bool removed = _remainingPool.Remove(runeId);
            Debug.Log($"[RunesService] Removed rune {runeId} from pool: {removed}, remaining pool size: {_remainingPool.Count}");
            ApplyRune(def);
        }

        private void ApplyRune(RuneDef def)
        {
            if (def?.effects == null || def.effects.Count == 0) return;

            var stats = ServiceContainer.Instance.Get<StatService>();
            foreach (var e in def.effects)
            {
                if (e.target == "Tower")
                {
                    if (e.attribute == "range")
                    {
                        if (e.operation == "add") stats.AddTowerRangeAdd(e.value);
                        else if (e.operation == "mult") stats.MulTowerRange(e.value);
                    }
                    else if (e.attribute == "damage")
                    {
                        if (e.operation == "add") stats.AddTowerDamageAdd(e.value);
                        else if (e.operation == "mult") stats.MulTowerDamage(e.value);
                    }
                }
                else if (e.target == "Enemy" && e.attribute == "moveSpeed")
                {
                    if (e.operation == "add") _enemySpeedAdd += e.value;
                    else if (e.operation == "mult") _enemySpeedMult *= e.value;
                }
            }

            // 将当前聚合的敌人移动速度修饰应用到所有在场敌人
            foreach (var agent in EnemyRegistry.All.ToArray())
            {
                if (agent == null) continue;
                ApplyEnemySpeed(agent);
            }
        }

        private List<string> RandomDistinct(List<string> pool, int count)
        {
            var list = new List<string>(pool);
            var result = new List<string>();
            for (int i = 0; i < count && list.Count > 0; i++)
            {
                int idx = _rng != null ? _rng.Next(list.Count) : UnityEngine.Random.Range(0, list.Count);
                result.Add(list[idx]);
                list.RemoveAt(idx);
            }
            return result;
        }

        /// <summary>
        /// 重置指定稀有度的符文池，将该稀有度的所有符文重新加入剩余池中。
        /// </summary>
        private void ResetRarityPool(string rarity)
        {
            if (_level?.runes?.poolIds == null) return;
            
            // 找到该稀有度的所有符文
            var rarityRunes = _level.runes.poolIds.Where(id => _defs.TryGetValue(id, out var d) && d.rarity == rarity).ToList();
            
            // 重新添加到剩余池中（避免重复）
            foreach (var runeId in rarityRunes)
            {
                if (!_remainingPool.Contains(runeId))
                {
                    _remainingPool.Add(runeId);
                }
            }
            
            Debug.Log($"[RunesService] Reset {rarity} pool: added {rarityRunes.Count} runes back to pool");
        }

        private void ApplyEnemySpeed(EnemyAgent agent)
        {
            if (agent == null) return;
            var mover = agent.GetComponent<EnemyMover>();
            if (mover == null) return;
            if (!_enemyBaseSpeeds.ContainsKey(agent))
            {
                _enemyBaseSpeeds[agent] = Mathf.Max(0.01f, mover.speed);
            }
            var baseSpeed = _enemyBaseSpeeds[agent];
            float target = baseSpeed * _enemySpeedMult + _enemySpeedAdd;
            float floor = baseSpeed * 0.2f; // 不低于初始速度 20%
            mover.speed = Mathf.Max(floor, target);
        }

        public bool PauseOnSelection => _cfg?.pauseOnSelection ?? true;
    }
}
