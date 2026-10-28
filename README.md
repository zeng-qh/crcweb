# CrcWeb — CRC / 校验和在线计算工具

基于 **.NET 10 Minimal API + 原生 HTML/CSS/JS** 的本地 CRC 与校验和计算工具，分栏布局（左侧输入参数、右侧即时结果），零外部依赖，可离线使用。功能复刻 [ip33.com/crc.html](http://www.ip33.com/crc.html) 并对照 [calctools.online](https://calctools.online/zh/checksum/crc) 补全了算法清单。

## 功能特性

### CRC 计算
- **74 种标准参数模型**（4~32 位）：CRC-4/ITU、CRC-8 系列（SMBUS、MAXIM、ROHC、AUTOSAR、GSM-B、LTE 等 21 种）、CRC-16 系列（MODBUS、IBM、USB、X-25、KERMIT、PROFIBUS、CCITT-FALSE、XMODEM、DNP 等 29 种）、CRC-32 系列（ISO-HDLC、JAMCRC、MPEG-2、BZIP2、CKSUM、ISCSI、MEF 等 12 种）及 4~7 位模型，另支持**自定义**参数（WIDTH 1~32 / POLY / INIT / XOROUT / REFIN / REFOUT）。
- **模糊查询组合框**：模型下拉支持关键字过滤（如 `modbus`、`x25`、`crc16modbus`），三级匹配（连续子串 > 忽略分隔符子串 > 子序列），支持键盘 ↑↓ / Enter / Esc。
- **Hex / Ascii 输入切换**：切换时文本框内容自动互转（如 `1234` ↔ `31 32 33 34`），输入即实时计算。
- **结果展示**：Hex、Bin，以及**大端 / 小端**两种字节序排列（方便直接粘贴进报文）。

### 整帧复制（面向串口调试）
- 所有复制动作输出「**数据 + 校验值**」整帧：
  - Hex 模式自动规范化为两位大写字节对（`xx xx` 格式），**复制后可直接粘贴到串口工具发送**；
  - Ascii 模式输出 `原文本 + 校验hex`。
- 「复制」按钮 = 大端序整帧；点击**大端 / 小端徽章**可复制对应字节序的整帧（小端 = 低字节在前，MODBUS 等协议直接用）；未计算时退化为仅复制规范化后的数据。
- **特殊校验结果格点击即复制整帧**，复制成功有 **Toast 提示**（显示完整复制内容，1.6s 自动淡出）。

### 特殊校验（表格内勾选，默认全启用）
| 校验 | 规则 |
|---|---|
| 累加和 SUM（低 8 位） | 所有字节相加取低 8 位 |
| 累加和 SUM（高 8 位） | `(SUM >> 8) & 0xFF`，双字节累加和先发高字节场景 |
| 异或校验 BCC | 所有字节逐一异或（初始 0） |
| 纵向冗余 LRC | `(0x100 − SUM) & 0xFF`，MODBUS ASCII 协议用法 |

### 其他
- 「常见 CRC 参数模型」参考总表：名称 / 多项式公式 / 宽度 / 参数 / 反转 / 用途，**点击行一键回填**输入框。
- 响应式：窄屏（≤980px）自动收合为单栏；≤520px 隐藏特殊校验表的规则列。
- 无障碍：aria 标签、键盘操作（复制支持 Enter/Space）、读屏 live region 播报。

## 环境要求

- [.NET 10 SDK](https://dotnet.microsoft.com/)（开发时使用 10.0.401）

## 运行

```bash
cd CrcWeb
dotnet run --urls "http://localhost:5173"
```

浏览器打开 <http://localhost:5173> 即可。

## Docker 部署（飞牛 fnOS / 树莓派）

镜像基于官方 `mcr.microsoft.com/dotnet/aspnet:10.0` 多架构基础镜像，同时支持 **linux/amd64 与 linux/arm64**，可直接跑在树莓派 4B（arm64）的飞牛 OS 上。

### 项目部署文件

| 文件 | 用途 |
|---|---|
| `Dockerfile` | 多阶段构建：SDK 编译 → 精简运行时镜像，容器内监听 8080 |
| `docker-compose.yml` | 一键部署编排，端口映射 `5173:8080` |
| `.github/workflows/docker-image.yml` | GitHub Actions 自动构建 amd64+arm64 镜像发布到 GHCR |
| `.dockerignore` | 排除 bin/obj 等无关文件 |

### 方式一：GitHub Actions 自动构建镜像（推荐）

1. 把 `CrcWeb` 目录作为仓库根推到 GitHub（`.github/workflows` 已就位）；
2. 推送到 `main` 分支或打 `v*` 标签即自动构建并发布到 GHCR（`ghcr.io/<用户名>/<仓库>:latest`），QEMU 交叉构建 arm64，**NAS 上无需装 .NET SDK**；
3. 首次构建后在 GitHub 仓库 Packages 设置中把镜像改为 Public，或在 NAS 上 `docker login ghcr.io`。

### 方式二：飞牛 fnOS 界面部署

1. 把整个 `CrcWeb` 目录上传到 NAS（如 `/vol1/1000/docker/crcweb`）；
2. 打开飞牛 **Docker → Compose**，选择「新建项目」，路径指向 `docker-compose.yml`（默认 `build: .` 本地构建）；
3. 启动后访问 `http://<NAS的IP>:5173`。

### 方式三：SSH 命令行

```bash
cd /path/to/CrcWeb
docker compose up -d --build      # 或 docker-compose up -d --build
```

若用 GHCR 镜像，改 compose 中 `image:` 行后 `docker compose pull && docker compose up -d`，或直接：

```bash
docker run -d --name crcweb --restart unless-stopped -p 5173:8080 ghcr.io/<用户名>/crcweb:latest
```

### 说明

- **端口**：容器内固定 8080，改宿主端口只需改 `-p` 左侧或 compose 里 `ports` 左侧；监听地址由环境变量 `ASPNETCORE_URLS` 控制（本地 `dotnet run` 走 launchSettings 的 5173，互不影响）；
- **数据**：应用无状态、不落盘，无需挂载卷；
- **更新**：重新构建镜像后 `docker compose up -d` 会自动重建容器。

### 自动更新（Watchtower，可选）

配合 GitHub Actions 实现全自动发布：`git push` → Actions 构建镜像 → NAS 上 Watchtower 定时检查并自动更新容器。NAS 上的 compose 示例（crcweb 与 watchtower 同栈管理）：

```yaml
services:
  crcweb:
    image: ghcr.io/<用户名>/crcweb:latest
    container_name: crcweb
    ports:
      - "5173:8080"
    restart: unless-stopped
    labels:
      - com.centurylinkfoundation.watchtower.enable=true   # 允许自动更新

  watchtower:
    image: containrrr/watchtower          # 多架构镜像，arm64 可用
    container_name: watchtower
    restart: unless-stopped
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
      # 私有镜像需再挂载 login 后的凭据：- /root/.docker/config.json:/config.json:ro
    command: --interval 300 --cleanup --label-enable
```

- `--interval 300` 每 5 分钟检查一次（可换 `--schedule "0 0 4 * * *"` 凌晨更新）；`--cleanup` 更新后删旧镜像；`--label-enable` 白名单模式，只更新带 label 的容器，不影响 NAS 上其他容器；
- 手动立即更新一次：`docker run --rm -v /var/run/docker.sock:/var/run/docker.sock containrrr/watchtower --run-once crcweb`；
- 注意：`latest` 模式只能向前滚动，需可回滚的项目在 compose 中改用 `v*` 版本 tag（打标签推送时 workflow 会自动产出）。

## Swagger 接口文档

启动后访问 **<http://localhost:5173/swagger>**，可查看并在线调试全部 API：

- 基于 [Swashbuckle.AspNetCore](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)（v10+，内置 Swagger UI）；
- OpenAPI JSON：`/swagger/v1/swagger.json`，可直接导入 Postman / Apifox 等工具；
- 接口按「参数模型 / CRC 计算 / 特殊校验」分组，含中文摘要与请求响应字段说明（来自 XML 注释）。

## 项目结构

```
CrcWeb/
├─ CrcWeb.csproj        # net10.0 Web SDK 项目（引用 Swashbuckle.AspNetCore，生成 XML 注释）
├─ Program.cs           # Minimal API 路由 + 静态文件回退（监听地址交给 launchSettings/环境变量）
├─ Dockerfile           # 多阶段构建（sdk:10.0 → aspnet:10.0，amd64/arm64）
├─ docker-compose.yml   # 一键部署编排（端口 5173:8080）
├─ .dockerignore        # 构建上下文排除项
├─ .github/workflows/docker-image.yml  # CI：构建 amd64+arm64 镜像发布 GHCR
├─ CrcAlgorithm.cs      # 通用位级 CRC 引擎（1~32 位，refin/refout/init/xorout/poly）
├─ CrcModels.cs         # 74 个标准参数模型 + 自定义（含用途说明）
├─ Checksums.cs         # SUM / BCC / LRC 特殊校验引擎
└─ wwwroot/
   ├─ index.html        # 页面（分栏布局）
   ├─ favicon.svg       # 页面图标（玫红显示器 + 浅蓝圆底 + 柱状图融合版）
   ├─ style.css         # 组件样式 + 分栏布局 + 响应式 + Toast
   └─ app.js            # 实时计算、模糊查询、整帧复制、Toast、模型表渲染
```

## API

### `GET /api/models`

返回全部参数模型（含自定义），供下拉与参考表渲染：

```json
[
  {
    "name": "CRC-16/MODBUS",
    "width": 16,
    "polyHex": "8005",
    "initHex": "FFFF",
    "xorOutHex": "0000",
    "refIn": true,
    "refOut": true,
    "usage": "MODBUS RTU 通信"
  }
]
```

### `POST /api/crc`

```json
// 请求
{
  "inputType": "ascii",        // ascii | hex
  "data": "123456789",
  "width": 16,
  "poly": "8005",
  "init": "FFFF",
  "xorOut": "0000",
  "refIn": true,
  "refOut": true
}

// 响应
{ "hex": "4B37", "bin": "0100101100110111", "width": 16 }
```

错误时返回 400 + `{ "error": "中文错误说明" }`（如 Hex 长度为奇数）。

### `POST /api/checksum`

```json
// 请求
{ "inputType": "hex", "data": "01 03 00 00 00 02" }

// 响应
{ "sum": "06", "sumHigh": "00", "bcc": "00", "lrc": "FA" }
```

## 正确性验证

以测试样本 `"123456789"` 对全部 74 个标准模型批量计算，check 值与 [CRC RevEng catalogue](https://reveng.sourceforge.io/crc-catalogue/all.htm) **逐一比对通过（74/74）**；特殊校验用 MODBUS 标准帧 `01 03 00 00 00 02` 交叉核对（SUM=06、BCC=00、LRC=FA）。

## 参考来源

- [ip33.com/crc.html](http://www.ip33.com/crc.html) — 页面功能与参数表原型
- [calctools.online CRC](https://calctools.online/zh/checksum/crc) — 算法清单对照（SUM / BCC / LRC 校验亦参考其规则）
- [CRC RevEng catalogue](https://reveng.sourceforge.io/crc-catalogue/all.htm) — 模型参数与 check 值基准

## 免责声明

1. 本项目仅供学习、研究与内部调试辅助使用，按「现状」提供，**不附带任何明示或默示的担保**，包括但不限于对适销性、特定用途适用性及计算结果准确性的担保。
2. 尽管 CRC / SUM / BCC / LRC 参数已与 CRC RevEng catalogue 等公开资料逐一比对验证，作者仍**不对计算结果的正确性、完整性作任何保证**；将计算结果用于工业控制、串口通信、计费、安全等生产场景前，请自行充分测试并承担相应风险，因使用本项目造成的任何直接或间接损失，作者不承担责任。
3. 本项目参考的网站及所涉及的协议、芯片、品牌名称（如 MODBUS、AUTOSAR、USB 等）仅用于说明参数模型的来源与用途，其权利归各自权利人所有；如有侵权请联系删除。
4. 请勿将本项目用于任何违反法律法规的用途；文中指向的外部链接内容不受本声明约束，访问外部站点请自行评估其条款与风险。
