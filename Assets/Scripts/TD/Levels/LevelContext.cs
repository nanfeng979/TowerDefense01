using System;

namespace TD.Levels
{
    /// <summary>
    /// 关卡上下文：当前仅包含关卡编号，用于拼接关卡配置/回合资源路径。
    /// </summary>
    [Serializable]
    public class LevelContext
    {
        public int LevelNumber;

        /// <summary>格式化为三位 ID（001/002...）。</summary>
        public string LevelId => LevelNumber.ToString("D3");
    }
}
