using System;

namespace TD.Config
{
    /// <summary>
    /// 符文选择 UI 的外部配置（从 StreamingAssets 读取）。
    /// </summary>
    [Serializable]
    public class RuneSelectionUiSettings
    {
        /// <summary>语言（如 zh-CN / en-US）。</summary>
        public string locale = "zh-CN";

        /// <summary>选择键（最多 3 个）。</summary>
        public string[] optionKeys = new[] { "Alpha1", "Alpha2", "Alpha3" };

        /// <summary>隐藏/恢复切换键。</summary>
        public string toggleKey = "Escape";
    }
}
