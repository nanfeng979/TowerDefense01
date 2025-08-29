using UnityEngine;

namespace TD.UI
{
    [CreateAssetMenu(fileName = "RuneSelectionInputConfig", menuName = "TD/UI/Rune Selection Input Config")]
    public class RuneSelectionInputConfig : ScriptableObject
    {
        [Header("选择键（索引 0..2）")]
        public KeyCode[] optionKeys = new KeyCode[3] { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3 };

        [Header("隐藏/恢复切换键")]
        public KeyCode toggleKey = KeyCode.Escape;
    }
}
