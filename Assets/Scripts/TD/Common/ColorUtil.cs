using System.Globalization;
using UnityEngine;

namespace TD.Common
{
    /// <summary>
    /// 颜色工具：支持 #RRGGBBAA / #RRGGBB 解析。
    /// </summary>
    public static class ColorUtil
    {
        public static bool TryParseRgbaHex(string hex, out Color color)
        {
            color = Color.white;
            if (string.IsNullOrEmpty(hex)) return false;
            if (hex[0] == '#') hex = hex.Substring(1);
            if (hex.Length == 6) hex += "FF"; // 无透明度则默认不透明
            if (hex.Length != 8) return false;

            bool ok = byte.TryParse(hex.Substring(0, 2), NumberStyles.HexNumber, null, out var r)
                   && byte.TryParse(hex.Substring(2, 2), NumberStyles.HexNumber, null, out var g)
                   && byte.TryParse(hex.Substring(4, 2), NumberStyles.HexNumber, null, out var b)
                   && byte.TryParse(hex.Substring(6, 2), NumberStyles.HexNumber, null, out var a);
            if (!ok) return false;
            color = new Color(r / 255f, g / 255f, b / 255f, a / 255f);
            return true;
        }
    }
}
