namespace CrcWeb;

/// <summary>
/// 一个 CRC 参数模型（NAME）。
/// 参数来源：ip33.com/crc.html 与 calctools.online/zh/checksum/crc，
/// 数值均与 CRC RevEng 标准目录一致；同参数不同名称的别名同时保留。
/// </summary>
public sealed record CrcModel(
    string Name,
    int Width,
    uint Poly,
    uint Init,
    uint XorOut,
    bool RefIn,
    bool RefOut,
    string Usage = "")
{
    /// <summary>多项式（hex 字符串，便于前端显示与回传）。</summary>
    public string PolyHex => Poly.ToString("X");
    /// <summary>初始值（hex 字符串）。</summary>
    public string InitHex => Init.ToString("X");
    /// <summary>结果异或值（hex 字符串）。</summary>
    public string XorOutHex => XorOut.ToString("X");
}

/// <summary>
/// 预置 CRC 参数模型总表：4~7 位（ip33）+ 全部 8/16/32 位标准算法（calctools.online）。
/// </summary>
public static class CrcModels
{
    public static readonly IReadOnlyList<CrcModel> All = new List<CrcModel>
    {
        // ---------- 自定义 ----------
        new("自定义", 0, 0x00, 0x00, 0x00, false, false, "手动输入 WIDTH/POLY/INIT/XOROUT/REFIN/REFOUT"),

        // ---------- 4~7 位（ip33.com 特有） ----------
        new("CRC-4/ITU",   4, 0x03, 0x00, 0x00, true,  true,  "ITU G.704 帧校验"),
        new("CRC-5/EPC",   5, 0x09, 0x09, 0x00, false, false, "EPC Gen2 RFID 标签"),
        new("CRC-5/ITU",   5, 0x15, 0x00, 0x00, true,  true,  "ITU G.704 帧校验"),
        new("CRC-5/USB",   5, 0x05, 0x1F, 0x1F, true,  true,  "USB 令牌包校验"),
        new("CRC-6/ITU",   6, 0x03, 0x00, 0x00, true,  true,  "ITU G.704 帧校验"),
        new("CRC-7/MMC",   7, 0x09, 0x00, 0x00, false, false, "MMC/SD 卡命令校验"),

        // ---------- 8 位 ----------
        new("CRC-8",             8, 0x07, 0x00, 0x00, false, false, "通用 8 位校验（SMBus PEC 同参数）"),
        new("CRC-8/ITU",         8, 0x07, 0x00, 0x55, false, false, "ATM HEC 计算的 ITU 标准"),
        new("CRC-8/ROHC",        8, 0x07, 0xFF, 0x00, true,  true,  "ROHC 鲁棒头部压缩协议"),
        new("CRC-8/MAXIM",       8, 0x31, 0x00, 0x00, true,  true,  "Maxim/Dallas 1-Wire（同 MAXIM-DOW）"),
        new("CRC-8/AUTOSAR",     8, 0x2F, 0xFF, 0xFF, false, false, "AUTOSAR 汽车通信系统"),
        new("CRC-8/BLUETOOTH",   8, 0xA7, 0x00, 0x00, true,  true,  "蓝牙协议"),
        new("CRC-8/CDMA2000",    8, 0x9B, 0xFF, 0x00, false, false, "CDMA2000 移动通信"),
        new("CRC-8/DARC",        8, 0x39, 0x00, 0x00, true,  true,  "DARC 数据广播系统"),
        new("CRC-8/DVB-S2",      8, 0xD5, 0x00, 0x00, false, false, "DVB-S2 数字卫星电视"),
        new("CRC-8/GSM-A",       8, 0x1D, 0x00, 0x00, false, false, "GSM-A 移动通信"),
        new("CRC-8/GSM-B",       8, 0x49, 0x00, 0xFF, false, false, "GSM-B 移动通信"),
        new("CRC-8/HITAG",       8, 0x1D, 0xFF, 0x00, false, false, "HITAG RFID 数据完整性"),
        new("CRC-8/I-432-1",     8, 0x07, 0x00, 0x55, false, false, "ITU-T I.432.1（同 CRC-8/ITU）"),
        new("CRC-8/I-CODE",      8, 0x1D, 0xFD, 0x00, false, false, "Philips I-CODE RFID"),
        new("CRC-8/LTE",         8, 0x9B, 0x00, 0x00, false, false, "4G LTE 移动通信"),
        new("CRC-8/MAXIM-DOW",   8, 0x31, 0x00, 0x00, true,  true,  "Maxim/Dallas 单总线（同 CRC-8/MAXIM）"),
        new("CRC-8/MIFARE-MAD",  8, 0x1D, 0xC7, 0x00, false, false, "MIFARE 应用目录（MAD）"),
        new("CRC-8/NRSC-5",      8, 0x31, 0xFF, 0x00, false, false, "NRSC-5 数字广播"),
        new("CRC-8/OPENSAFETY",  8, 0x2F, 0x00, 0x00, false, false, "OpenSAFETY 工业安全通信"),
        new("CRC-8/SAE-J1850",   8, 0x1D, 0xFF, 0xFF, false, false, "SAE J1850 汽车网络"),
        new("CRC-8/SMBUS",       8, 0x07, 0x00, 0x00, false, false, "SMBus 通信协议（同 CRC-8）"),
        new("CRC-8/TECH-3250",   8, 0x1D, 0xFF, 0x00, true,  true,  "EBU Tech 3250 数字音频接口"),
        new("CRC-8/WCDMA",       8, 0x9B, 0x00, 0x00, true,  true,  "3G WCDMA 移动通信"),

        // ---------- 16 位 ----------
        new("CRC-16/IBM",           16, 0x8005, 0x0000, 0x0000, true,  true,  "同 CRC-16/ARC（ARC/LHA 压缩）"),
        new("CRC-16/MAXIM",         16, 0x8005, 0x0000, 0xFFFF, true,  true,  "Maxim/Dallas 设备（同 MAXIM-DOW）"),
        new("CRC-16/USB",           16, 0x8005, 0xFFFF, 0xFFFF, true,  true,  "USB 数据传输校验"),
        new("CRC-16/MODBUS",        16, 0x8005, 0xFFFF, 0x0000, true,  true,  "Modbus 工业通信协议"),
        new("CRC-16/CCITT",         16, 0x1021, 0x0000, 0x0000, true,  true,  "同 CRC-16/KERMIT"),
        new("CRC-16/CCITT-FALSE",   16, 0x1021, 0xFFFF, 0x0000, false, false, "SDLC/X.25 非反射变体"),
        new("CRC-16/X25",           16, 0x1021, 0xFFFF, 0xFFFF, true,  true,  "X.25 / HDLC FCS"),
        new("CRC-16/XMODEM",        16, 0x1021, 0x0000, 0x0000, false, false, "XMODEM/ZMODEM 文件传输"),
        new("CRC-16/DNP",           16, 0x3D65, 0x0000, 0xFFFF, true,  true,  "DNP3 电力系统自动化"),
        new("CRC-16/ARC",           16, 0x8005, 0x0000, 0x0000, true,  true,  "ARC/LHA 压缩格式"),
        new("CRC-16/CDMA2000",      16, 0xC867, 0xFFFF, 0x0000, false, false, "CDMA2000 移动通信"),
        new("CRC-16/CMS",           16, 0x8005, 0xFFFF, 0x0000, false, false, "CMS 密码消息语法"),
        new("CRC-16/DDS-110",       16, 0x8005, 0x800D, 0x0000, false, false, "DDS-110 数据采集系统"),
        new("CRC-16/DECT-R",        16, 0x0589, 0x0000, 0x0001, false, false, "DECT 无绳电话（带 XOROUT）"),
        new("CRC-16/DECT-X",        16, 0x0589, 0x0000, 0x0000, false, false, "DECT 无绳电话"),
        new("CRC-16/EN-13757",      16, 0x3D65, 0x0000, 0xFFFF, false, false, "EN 13757 智能仪表（M-Bus）"),
        new("CRC-16/GENIBUS",       16, 0x1021, 0xFFFF, 0xFFFF, false, false, "GENIBUS 工业现场总线"),
        new("CRC-16/GSM",           16, 0x1021, 0x0000, 0xFFFF, false, false, "GSM 移动通信"),
        new("CRC-16/IBM-3740",      16, 0x1021, 0xFFFF, 0x0000, false, false, "IBM 3740 / AUTOSAR（同 CCITT-FALSE）"),
        new("CRC-16/IBM-SDLC",      16, 0x1021, 0xFFFF, 0xFFFF, true,  true,  "IBM SDLC 链路控制（同 X.25）"),
        new("CRC-16/KERMIT",        16, 0x1021, 0x0000, 0x0000, true,  true,  "Kermit 文件传输协议"),
        new("CRC-16/LJ1200",        16, 0x6F63, 0x0000, 0x0000, false, false, "LoJack 车辆追踪"),
        new("CRC-16/M17",           16, 0x5935, 0xFFFF, 0x0000, false, false, "M17 数字语音协议"),
        new("CRC-16/MAXIM-DOW",     16, 0x8005, 0x0000, 0xFFFF, true,  true,  "Maxim/Dallas 设备（同 CRC-16/MAXIM）"),
        new("CRC-16/MCRF4XX",       16, 0x1021, 0xFFFF, 0x0000, true,  true,  "Microchip MCRF4XX RFID"),
        new("CRC-16/OPENSAFETY-A",  16, 0x5935, 0x0000, 0x0000, false, false, "OpenSAFETY 工业安全通道 A"),
        new("CRC-16/OPENSAFETY-B",  16, 0x755B, 0x0000, 0x0000, false, false, "OpenSAFETY 工业安全通道 B"),
        new("CRC-16/PROFIBUS",      16, 0x1DCF, 0xFFFF, 0xFFFF, false, false, "PROFIBUS 现场总线"),
        new("CRC-16/SPI-FUJITSU",   16, 0x1021, 0x1D0F, 0x0000, false, false, "富士通 SPI 通信"),
        new("CRC-16/T10-DIF",       16, 0x8BB7, 0x0000, 0x0000, false, false, "SCSI T10 数据完整性字段"),
        new("CRC-16/TELEDISK",      16, 0xA097, 0x0000, 0x0000, false, false, "TeleDisk 磁盘镜像格式"),
        new("CRC-16/UMTS",          16, 0x8005, 0x0000, 0x0000, false, false, "UMTS 移动通信"),

        // ---------- 32 位 ----------
        new("CRC-32",             32, 0x04C11DB7, 0xFFFF_FFFF, 0xFFFF_FFFF, true,  true,  "通用（ZIP/以太网/PNG，同 ISO-HDLC）"),
        new("CRC-32/MPEG-2",      32, 0x04C11DB7, 0xFFFF_FFFF, 0x0000_0000, false, false, "MPEG-2 传输流"),
        new("CRC-32/AIXM",        32, 0x814141AB, 0x0000_0000, 0x0000_0000, false, false, "AIXM 航空信息交换模型"),
        new("CRC-32/AUTOSAR",     32, 0xF4ACFB13, 0xFFFF_FFFF, 0xFFFF_FFFF, true,  true,  "AUTOSAR 汽车开放系统架构"),
        new("CRC-32/BASE91-D",    32, 0xA833982B, 0xFFFF_FFFF, 0xFFFF_FFFF, true,  true,  "BASE91-D 编码校验"),
        new("CRC-32/BZIP2",       32, 0x04C11DB7, 0xFFFF_FFFF, 0xFFFF_FFFF, false, false, "BZIP2 压缩格式"),
        new("CRC-32/CD-ROM-EDC",  32, 0x8001801B, 0x0000_0000, 0x0000_0000, true,  true,  "CD-ROM 错误检测（EDC）"),
        new("CRC-32/CKSUM",       32, 0x04C11DB7, 0x0000_0000, 0xFFFF_FFFF, false, false, "UNIX cksum 命令"),
        new("CRC-32/ISCSI",       32, 0x1EDC6F41, 0xFFFF_FFFF, 0xFFFF_FFFF, true,  true,  "iSCSI 存储（Castagnoli CRC-32C）"),
        new("CRC-32/ISO-HDLC",    32, 0x04C11DB7, 0xFFFF_FFFF, 0xFFFF_FFFF, true,  true,  "ISO-HDLC（同 CRC-32）"),
        new("CRC-32/JAMCRC",      32, 0x04C11DB7, 0xFFFF_FFFF, 0x0000_0000, true,  true,  "JAMCRC（CRC-32 取反）"),
        new("CRC-32/MEF",         32, 0x741B8CD7, 0xFFFF_FFFF, 0x0000_0000, true,  true,  "MEF 城域以太网论坛"),
        new("CRC-32/XFER",        32, 0x000000AF, 0x0000_0000, 0x0000_0000, false, false, "XFER 文件传输"),
    };
}
