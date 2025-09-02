namespace TD.Core
{
    /// <summary>
    /// 关卡管理接口（最小化）：当前仅支持以关卡编号进行初始化。
    /// </summary>
    public interface ILevelManager
    {
        void Init(int levelNumber);
    }
}
