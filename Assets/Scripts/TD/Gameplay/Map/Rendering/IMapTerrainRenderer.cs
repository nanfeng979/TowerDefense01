using TD.Config;

namespace TD.Gameplay.Map.Rendering
{
    /// <summary>
    /// 地形/路径可视化渲染策略接口。
    /// 由 MapRenderer 根据关卡与配置选择具体实现调用。
    /// </summary>
    public interface IMapTerrainRenderer
    {
        void Render(MapRenderer owner, LevelConfig level);
    }
}
