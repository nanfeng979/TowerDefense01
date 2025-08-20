using System.Threading.Tasks;

namespace TD.Config
{
    /// <summary>
    /// 配置聚合读取服务（仅接口，便于后续替换为本地/远端等实现）。
    /// </summary>
    public interface IConfigService
    {
        Task<ElementsConfig> GetElementsAsync();
        Task<TowersConfig> GetTowersAsync();
        Task<EnemiesConfig> GetEnemiesAsync();
        Task<LevelConfig> GetLevelAsync(string levelId);
    }
}
