namespace CrcWeb;

/// <summary>
/// 通用 CRC 计算引擎。
/// 参照 ip33.com/crc.html 的“参数模型”定义：WIDTH / POLY / INIT / XOROUT / REFIN / REFOUT。
/// 支持 1..32 任意宽度，输入为字节序列（由调用方按 hex / ascii 解析）。
/// </summary>
public static class CrcAlgorithm
{
    /// <summary>
    /// 计算 CRC。
    /// </summary>
    /// <param name="data">待校验的字节序列。</param>
    /// <param name="width">CRC 宽度（1..32）。</param>
    /// <param name="poly">生成多项式（已忽略最高位的“1”，十六进制给出）。</param>
    /// <param name="init">寄存器初始预置值。</param>
    /// <param name="xorOut">结果异或值。</param>
    /// <param name="refIn">每个输入字节是否按位反转。</param>
    /// <param name="refOut">计算完成后、异或输出前是否整体按位反转。</param>
    /// <returns>计算得到的 CRC 值（已按 width 掩码截断）。</returns>
    public static uint Compute(
        byte[] data,
        int width,
        uint poly,
        uint init,
        uint xorOut,
        bool refIn,
        bool refOut)
    {
        if (width < 1 || width > 32)
            throw new ArgumentOutOfRangeException(nameof(width), "WIDTH 必须在 1..32 之间");

        uint mask = width >= 32 ? 0xFFFF_FFFFu : (1u << width) - 1u;
        uint topBit = width >= 32 ? 0x8000_0000u : (1u << (width - 1));
        poly &= mask;
        uint crc = init & mask;

        for (int i = 0; i < data.Length; i++)
        {
            byte b = data[i];
            if (refIn) b = (byte)ReverseBits(b, 8);

            // 逐位处理（MSB first）：把输入位与寄存器溢出的高位异或后决定是否异或多项式
            for (int j = 7; j >= 0; j--)
            {
                uint bit = (uint)((b >> j) & 1);
                uint top = ((crc & topBit) >> (width - 1)) ^ bit;
                crc = (crc << 1) & mask;
                if (top != 0) crc ^= poly;
            }
        }

        if (refOut) crc = ReverseBits(crc, width);
        crc ^= xorOut;
        crc &= mask;
        return crc;
    }

    /// <summary>
    /// 反转 value 的低 n 位（n &lt;= 32）。
    /// </summary>
    private static uint ReverseBits(uint value, int n)
    {
        uint result = 0;
        for (int i = 0; i < n; i++)
        {
            result = (result << 1) | (value & 1u);
            value >>= 1;
        }
        return result;
    }

    /// <summary>
    /// 把 CRC 结果格式化为十六进制字符串，高位在左，按宽度补零（每 4 bit 一位）。
    /// </summary>
    public static string ToHex(uint value, int width)
    {
        int digits = (width + 3) / 4;
        return value.ToString("X" + digits);
    }

    /// <summary>
    /// 把 CRC 结果格式化为二进制字符串，按宽度补零。
    /// </summary>
    public static string ToBin(uint value, int width)
    {
        return Convert.ToString(value, 2).PadLeft(width, '0');
    }
}
