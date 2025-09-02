using System.Threading.Tasks;
using UnityEngine;
using TD.Config;

namespace TD.Levels
{
    /// <summary>
    /// 关卡管理：接收 LevelContext，负责装载配置与驱动轮次切换。
    /// 目前仅加载关卡配置，预留按回合切换子预制体的入口。
    /// </summary>
    public class LevelManager : MonoBehaviour, TD.Core.ILevelManager
    {
    public LevelContext Context { get; private set; }
    public TD.Config.LevelConfig Config { get; private set; }

        public async void Init(LevelContext ctx)
        {
            Context = ctx;
            // 读取配置：StreamingAssets/TD/levels/level_{id}.json
            var id = ctx.LevelId; // e.g., 001
            var path = $"levels/level_{id}.json";
            if (TD.Core.ServiceContainer.Instance.TryGet<TD.Config.IJsonLoader>(out var loader))
            {
                try
                {
                    Config = await loader.LoadAsync<TD.Config.LevelConfig>(path);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[LevelManager] Load config failed: {path}, {ex.Message}");
                }
            }

            // 先渲染地图
            var map = GetComponentInChildren<TD.Gameplay.Map.MapRenderer>() ?? transform.parent.GetComponentInChildren<TD.Gameplay.Map.MapRenderer>();
            if (map != null && Config != null)
            {
                map.Setup(id, Config);
            }

            // 适配现有 RoundSpawner：仅设置 levelId，其他通过其自身流程加载与运行
            var spawner = GetComponentInChildren<TD.Gameplay.Spawn.RoundSpawner>() ?? transform.parent.GetComponentInChildren<TD.Gameplay.Spawn.RoundSpawner>();
            if (spawner != null && Config != null)
            {
                // 先清理可能存在的运行中流程
                spawner.StopAndReset(clearSpawned: true);
                // 由 LevelManager 显式启动回合生成
                spawner.levelId = id;
                spawner.Begin(Config);
            }
        }

        // 接口桥接：允许外部仅以关卡编号进行初始化
        public void Init(int levelNumber)
        {
            Init(new LevelContext { LevelNumber = levelNumber });
        }
    }
}
