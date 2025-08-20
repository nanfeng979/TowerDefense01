using System.Globalization;
using UnityEngine;

namespace TD.Common
{
    /// <summary>
    /// 颜色工具：支持 #RRGGBBAA / #RRGGBB 解析。
    /// </summary>
    public static class ColorUtil
    {
        /// <summary>
        /// 尝试解析RGBA十六进制字符串为Color对象
        /// </summary>
        /// <param name="hex">十六进制字符串，支持 #RRGGBB 或 #RRGGBBAA 格式</param>
        /// <param name="color">解析成功的颜色值</param>
        /// <returns>解析是否成功</returns>
        public static bool TryParseRgbaHex(string hex, out Color color)
        {
            color = Color.white;
            
            // 检查输入有效性
            if (string.IsNullOrEmpty(hex))
                return false;

            // 移除#前缀
            if (hex[0] == '#')
                hex = hex.Substring(1);

            // 验证长度
            if (hex.Length != 6 && hex.Length != 8)
                return false;

            // 补全Alpha通道
            if (hex.Length == 6)
                hex += "FF";

            // 逐个解析颜色分量
            if (!TryParseHexByte(hex, 0, out byte r) ||
                !TryParseHexByte(hex, 2, out byte g) ||
                !TryParseHexByte(hex, 4, out byte b) ||
                !TryParseHexByte(hex, 6, out byte a))
            {
                return false;
            }

            color = new Color(r / 255f, g / 255f, b / 255f, a / 255f);
            return true;
        }

        /// <summary>
        /// 解析十六进制字符串中的单个字节
        /// </summary>
        private static bool TryParseHexByte(string hex, int startIndex, out byte result)
        {
            return byte.TryParse(hex.Substring(startIndex, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
        }

        /// <summary>
        /// 解析RGBA十六进制字符串为Color对象（便捷方法）
        /// </summary>
        /// <param name="hex">十六进制字符串</param>
        /// <returns>解析成功的颜色值，失败时返回白色</returns>
        public static Color ParseRgbaHex(string hex)
        {
            return TryParseRgbaHex(hex, out Color color) ? color : Color.white;
        }
    }
}