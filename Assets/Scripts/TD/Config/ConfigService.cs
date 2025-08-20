using System.Threading.Tasks;

namespace TD.Config
{
    /// <summary>
    /// 配置服务的最小实现：使用 IJsonLoader 从 StreamingAssets 加载并缓存。
    /// </summary>
    public class ConfigService : IConfigService
    {
        private readonly IJsonLoader _loader;
        private ElementsConfig _elements;
        private TowersConfig _towers;
        private EnemiesConfig _enemies;

        public ConfigService(IJsonLoader loader)
        {
            _loader = loader;
        }

        public async Task<ElementsConfig> GetElementsAsync()
        {
            if (_elements == null)
                _elements = await _loader.LoadAsync<ElementsConfig>("elements.json");
            return _elements;
        }

        public async Task<TowersConfig> GetTowersAsync()
        {
            if (_towers == null)
                _towers = await _loader.LoadAsync<TowersConfig>("towers.json");
            return _towers;
        }

        public async Task<EnemiesConfig> GetEnemiesAsync()
        {
            if (_enemies == null)
                _enemies = await _loader.LoadAsync<EnemiesConfig>("enemies.json");
            return _enemies;
        }

        public Task<LevelConfig> GetLevelAsync(string levelId)
        {
            return _loader.LoadAsync<LevelConfig>($"levels/level_{levelId}.json");
        }
    }
}
