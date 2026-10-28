namespace CrcWeb;

/// <summary>
/// 特殊校验：累加和 SUM、异或校验 BCC、纵向冗余校验 LRC。
/// 规则参照 calctools.online /checksum/sum、/checksum/bcc、/checksum/lrc。
/// </summary>
public static class Checksums
{
    /// <summary>
    /// 累加和 SUM：将数据流中所有字节相加，取低 8 位作为校验值。
    /// </summary>
    public static byte Sum8(byte[] data) => (byte)(SumTotal(data) & 0xFF);

    /// <summary>
    /// 累加和 SUM 高 8 位：取字节总和低 16 位中的高字节，即 (SUM >> 8) &amp; 0xFF。
    /// 用于双字节累加和传输场景（先发高字节、后发低字节）。
    /// </summary>
    public static byte Sum8High(byte[] data) => (byte)((SumTotal(data) >> 8) & 0xFF);

    /// <summary>数据流所有字节之和（完整值）。</summary>
    private static uint SumTotal(byte[] data)
    {
        uint sum = 0;
        foreach (byte b in data) sum += b;
        return sum;
    }

    /// <summary>
    /// BCC（块校验字符 / 异或校验）：对数据流中所有字节逐一异或，初始值 0。
    /// </summary>
    public static byte Bcc(byte[] data)
    {
        byte x = 0;
        foreach (byte b in data) x ^= b;
        return x;
    }

    /// <summary>
    /// LRC（纵向冗余校验）：所有字节之和的二补码，即 (0x100 - SUM) 的低 8 位。
    /// MODBUS ASCII 协议使用的正是这种 LRC。
    /// </summary>
    public static byte Lrc(byte[] data)
    {
        return (byte)((0x100 - Sum8(data)) & 0xFF);
    }

    /// <summary>字节转两位大写十六进制字符串。</summary>
    public static string ToHex2(byte b) => b.ToString("X2");
}
