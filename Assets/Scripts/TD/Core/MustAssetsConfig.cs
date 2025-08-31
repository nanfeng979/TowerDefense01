using System;
using System.Collections.Generic;

namespace TD.Core
{
    /// <summary>
    /// 启动阶段需要预热的资源配置（StreamingAssets/TD/init/must_assets.json）。
    /// - resourcesPrefabs: Resources 路径（例如 "UI/MainMenu"）
    /// - uiInject: UI 相关的注入设置（例如默认字体）
    /// </summary>
    [Serializable]
    public class MustAssetsConfig
    {
        public List<string> resourcesPrefabs = new List<string>();
        public UIInject uiInject = new UIInject();
    }

    [Serializable]
    public class UIInject
    {
        /// <summary>
        /// 默认字体路径（Addressables 或 Resources 可识别的路径，均不带扩展名）
        /// </summary>
        public string defaultFont;
    }
}
