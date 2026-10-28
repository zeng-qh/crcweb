using CrcWeb;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);
// 监听地址不在此硬编码：开发环境由 launchSettings.json 指定（http://localhost:5173），
// Docker/生产部署通过环境变量 ASPNETCORE_URLS 控制（见 Dockerfile / docker-compose.yml）。

// 注册 Swagger / OpenAPI 文档生成服务
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CRC 校验计算服务",
        Version = "v1",
        Description = "提供 75 个 CRC 参数模型（74 标准 + 自定义）的 CRC 计算，" +
                      "以及累加和 SUM（低/高 8 位）、异或 BCC、纵向冗余 LRC 等特殊校验。" +
                      "数据输入支持 Hex（自动清洗非十六进制字符）与 Ascii 两种格式。"
    });

    // 载入 XML 注释（csproj 已开启 GenerateDocumentationFile）
    string xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
});

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

// Swagger JSON（/swagger/v1/swagger.json）与交互式 UI（/swagger）
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "CRC 校验计算服务 v1");
    options.DocumentTitle = "CRC 校验 API 文档";
});

// 返回所有预置的 CRC 参数模型（供前端下拉填充）
app.MapGet("/api/models", () => Results.Ok(CrcModels.All))
   .WithTags("参数模型")
   .WithSummary("获取全部 CRC 参数模型")
   .WithDescription("返回 75 个条目：1 个自定义模型 + 74 个标准模型（对齐 CRC RevEng catalogue）。")
   .Produces<CrcModel[]>(StatusCodes.Status200OK);

// 计算 CRC
app.MapPost("/api/crc", (CrcRequest req) =>
{
    try
    {
        byte[] data = req.InputType switch
        {
            "ascii" => System.Text.Encoding.UTF8.GetBytes(req.Data ?? string.Empty),
            _ => HexHelper.ParseHex(req.Data ?? string.Empty)
        };

        int width = req.Width;
        if (width < 1 || width > 32)
            return Results.BadRequest(new { error = "WIDTH 必须在 1..32 之间" });

        uint poly = HexHelper.ParseHexToUint(req.Poly);
        uint init = HexHelper.ParseHexToUint(req.Init);
        uint xorOut = HexHelper.ParseHexToUint(req.XorOut);

        uint crc = CrcAlgorithm.Compute(data, width, poly, init, xorOut, req.RefIn, req.RefOut);
        var resp = new CrcResponse(CrcAlgorithm.ToHex(crc, width), CrcAlgorithm.ToBin(crc, width), width);
        return Results.Ok(resp);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithTags("CRC 计算")
.WithSummary("按指定参数模型计算 CRC")
.WithDescription("width 支持 1..32 位；poly/init/xorOut 为十六进制字符串（可带 0x 前缀）；" +
                 "refin/refout 对应输入/输出反射。校验失败或数据非法时返回 400 与 error 说明。")
.Produces<CrcResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest);

// 计算特殊校验（累加和 SUM / 异或 BCC / 纵向冗余 LRC），与 CRC 使用同一套输入解析
app.MapPost("/api/checksum", (ChecksumRequest req) =>
{
    try
    {
        byte[] data = req.InputType switch
        {
            "ascii" => System.Text.Encoding.UTF8.GetBytes(req.Data ?? string.Empty),
            _ => HexHelper.ParseHex(req.Data ?? string.Empty)
        };

        var resp = new ChecksumResponse(
            Checksums.ToHex2(Checksums.Sum8(data)),
            Checksums.ToHex2(Checksums.Sum8High(data)),
            Checksums.ToHex2(Checksums.Bcc(data)),
            Checksums.ToHex2(Checksums.Lrc(data)));
        return Results.Ok(resp);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithTags("特殊校验")
.WithSummary("计算累加和 SUM、异或 BCC、纵向冗余 LRC")
.WithDescription("一次返回四项特殊校验结果（两位大写十六进制）：sum=累加和低 8 位、" +
                 "sumHigh=累加和高 8 位、bcc=逐字节异或、lrc=MODBUS ASCII 二补码。数据非法时返回 400。")
.Produces<ChecksumResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest);

app.MapFallbackToFile("index.html");

app.Run();

// ---------- 请求 / 响应模型 ----------
/// <summary>CRC 计算请求</summary>
/// <param name="InputType">输入类型："hex"（自动清洗非十六进制字符）或 "ascii"（UTF-8 编码）</param>
/// <param name="Data">待计算的数据字符串</param>
/// <param name="Width">CRC 宽度（1..32 位）</param>
/// <param name="Poly">生成多项式，十六进制（如 "8005"，可带 0x 前缀）</param>
/// <param name="Init">初始值，十六进制（如 "FFFF"）</param>
/// <param name="XorOut">结果异或值，十六进制（如 "0000"）</param>
/// <param name="RefIn">输入字节是否反转（RefIn）</param>
/// <param name="RefOut">输出 CRC 是否反转（RefOut）</param>
public record CrcRequest(
    string InputType,
    string Data,
    int Width,
    string Poly,
    string Init,
    string XorOut,
    bool RefIn,
    bool RefOut);

/// <summary>CRC 计算结果</summary>
/// <param name="Hex">十六进制结果（大写，按宽度补零）</param>
/// <param name="Bin">二进制结果（按宽度补零）</param>
/// <param name="Width">CRC 宽度</param>
public record CrcResponse(string Hex, string Bin, int Width);

/// <summary>特殊校验请求</summary>
/// <param name="InputType">输入类型："hex" 或 "ascii"</param>
/// <param name="Data">待计算的数据字符串</param>
public record ChecksumRequest(string InputType, string Data);

/// <summary>特殊校验结果（均为两位大写十六进制）</summary>
/// <param name="Sum">累加和 SUM（低 8 位）</param>
/// <param name="SumHigh">累加和 SUM（高 8 位）</param>
/// <param name="Bcc">逐字节异或校验 BCC</param>
/// <param name="Lrc">纵向冗余校验 LRC（二补码，MODBUS ASCII 兼容）</param>
public record ChecksumResponse(string Sum, string SumHigh, string Bcc, string Lrc);

// ---------- 解析辅助 ----------
static class HexHelper
{
    public static byte[] ParseHex(string s)
    {
        var sb = new System.Text.StringBuilder();
        foreach (char c in s)
            if (Uri.IsHexDigit(c)) sb.Append(char.ToUpperInvariant(c));

        string hex = sb.ToString();
        if (hex.Length == 0) return Array.Empty<byte>();
        if (hex.Length % 2 != 0)
            throw new FormatException("十六进制数据长度必须为偶数（每两个字符表示一个字节）");

        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        return bytes;
    }

    public static uint ParseHexToUint(string s)
    {
        s = (s ?? string.Empty).Trim().Replace("0x", "", StringComparison.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(s)) return 0;
        if (!Uri.IsHexDigit(s[0]))
            throw new FormatException("不是合法的十六进制数值：" + s);
        return Convert.ToUInt32(s, 16);
    }
}
