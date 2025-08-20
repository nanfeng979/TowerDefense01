using System.Threading.Tasks;
using UnityEngine;
using TD.Config;

namespace TD.Core
{
    /// <summary>
    /// 仅用于示例：在场景中挂载以便运行时做一次配置加载校验。
    /// 实际项目中将拆分为服务容器与独立系统。
    /// </summary>
    public class Bootstrapper : MonoBehaviour
    {
        private IConfigService _config;

        private void Awake()
        {
            // 最小依赖装配
            IJsonLoader loader = new StreamingAssetsJsonLoader();
            _config = new ConfigService(loader);
        }

        private async void Start()
        {
            await ValidateOnce();
        }

        private async Task ValidateOnce()
        {
            try
            {
                var elements = await _config.GetElementsAsync();
                var towers = await _config.GetTowersAsync();
                var enemies = await _config.GetEnemiesAsync();
                var level = await _config.GetLevelAsync("001");

                Debug.Log($"[Validate] elements={elements?.elements?.Count}, towers={towers?.towers?.Count}, enemies={enemies?.enemies?.Count}");
                Debug.Log($"[Validate] level={level?.levelId}, paths={level?.paths?.Count}, slots={level?.buildSlots?.Count}, waves={level?.waves?.Count}");

                // 简单一致性检查
                foreach (var w in level.waves)
                {
                    foreach (var g in w.groups)
                    {
                        bool enemyExists = enemies.enemies.Exists(e => e.id == g.enemyId);
                        bool pathExists = level.paths.Exists(p => p.id == g.pathId);
                        if (!enemyExists)
                            Debug.LogError($"Enemy not found: {g.enemyId}");
                        if (!pathExists)
                            Debug.LogError($"Path not found: {g.pathId}");
                    }
                }

                Debug.Log("[Validate] JSON load & basic cross-check OK");
            }
            catch (System.SystemException ex)
            {
                Debug.LogError($"[Validate] Failed: {ex.Message}");
            }
        }
    }
}
