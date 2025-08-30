using System;
using System.Collections.Generic;

namespace TD.Core
{
    /// <summary>
    /// 启动阶段需要预热的资源配置（StreamingAssets/TD/init/must_assets.json）。
    /// - resourcesPrefabs: Resources 路径（例如 "UI/MainMenu"）
    /// - addressableAssets: Addressables 地址（可选）
    /// - uiFonts: 预热字体（保留字段，当前未使用）
    /// </summary>
    [Serializable]
    public class MustAssetsConfig
    {
        public List<string> resourcesPrefabs = new List<string>();
        public List<string> addressableAssets = new List<string>();
        public List<string> uiFonts = new List<string>();
    }
}
