# 远程 archive.org 条目调查 —— Warpforge 1.35.0 APK cache

- 调查日期：2026-10-18
- 调查者：只读调查代理
- 条目页：https://archive.org/details/warhammer-40-000-warpforge-1.35.0apkcache
- 元数据：https://archive.org/metadata/warhammer-40-000-warpforge-1.35.0apkcache
- 本机状态：`d:/4` 源码/正本未改动；未跑 Unity；未动 git

## 结论一句话

🔴 **直连 archive.org 全程失败（DNS 被污染成 Facebook/Twitter/Yahoo 地址 + 真实 IA IP 也不可达），本机没有任何在跑的代理/加速器 ⇒ 元数据一个字节都没取到，无法确认该条目是否含卡牌 SO / `allcards_assets_all_*.bundle` / 能否补 118 张缺卡 ID。需要先启动加速器（或提供可用代理）后重跑。**

## 失败详情（完整错误与状态码）

### 1) 元数据直连（第 1 次，按任务要求）
```
curl -L --max-time 60 -sS https://archive.org/metadata/warhammer-40-000-warpforge-1.35.0apkcache
curl: (28) Failed to connect to archive.org:443 after 21152 ms: Could not connect to server
HTTP_CODE=000  SIZE_DOWNLOAD=0  REMOTE_IP=(空)
```

### 2) DNS 被污染（四个解析器互证）
| 解析器 | 返回的 archive.org 地址 |
|---|---|
| 223.5.5.5（阿里公共DNS） | `2a03:2880:f117:83:face:b00c:0:25de` / `199.59.148.9` |
| 8.8.8.8 | `2a03:2880:f12a:83:face:b00c:0:25de` / `31.13.92.5` |
| 1.1.1.1 | `2a03:2880:f12c:83:face:b00c:0:25de` / `202.160.130.145` |
| AliDNS DoH（dns.alidns.com） | `199.59.148.9` |

- `face:b00c` 是 Facebook 的 IPv6 段特征；`199.59.148.9` 是 Twitter 段、`31.13.92.5` 是 Facebook 段、`202.160.130.145` 是 Yahoo 段 —— **全是假地址，典型 DNS 污染**。
- 注意：连**阿里 DoH 也返回污染结果**，说明污染发生在更上游，换 DoH 绕不过去。

### 3) 绕过 DNS、直连真实 IA IP（一次）
```
curl --resolve archive.org:443:207.241.237.100 --max-time 40 https://archive.org/metadata/...
curl: (28) Failed to connect to archive.org:443 after 21020 ms: Could not connect to server
HTTP=000  size=0
```
⇒ IP 层也不可达（TCP 握手被黑洞）。

### 4) HTTP 80 端口
```
curl --max-time 20 http://archive.org/metadata/...
curl: (28) Connection timed out after 20008 ms
```

### 5) 其它 DoH
- `https://cloudflare-dns.com/dns-query?...` → `curl: (35) Recv failure: Connection was reset`
- `https://dns.google/resolve?...` → `curl: (28) Connection timed out after 15007 ms`

### 6) 对照实验（证明本机外网是通的）
```
curl https://www.baidu.com → HTTP=200  ip=2408:8756:...
```
⇒ 只是 archive.org（及下面这些中转站）被挡，不是断网。

### 7) 第三方中转（让别的服务器替我们拉）
- `https://r.jina.ai/https://archive.org/metadata/...` → `curl: (28) Failed to connect to r.jina.ai:443 after 21461 ms`
- `https://api.allorigins.win/raw?url=...` → `curl: (28) Connection timed out after 45009 ms`

### 8) 内置工具
- **WebFetch** → `Unable to verify if domain archive.org is safe to fetch. This may be due to network restrictions or enterprise security policies blocking claude.ai.`（安全校验要连 claude.ai，同样不可达）
- **WebSearch** → 搜 `"warhammer-40-000-warpforge-1.35.0apkcache"` **零结果**，该条目没有被搜索引擎索引。
  - 附带情报：搜索里出现了 APKPure / APKFab / APKCombo 的 **Warpforge 1.35.0 APK** 页面（是否可达未验证）。⚠️ 那是 APK 本体，**不是**本任务要找的 cache / Addressables 包，不能替代。

## 代理 / 加速器现状（只读检查，未改任何配置）

| 检查项 | 结果 |
|---|---|
| 环境变量 `HTTP_PROXY` / `HTTPS_PROXY` / `ALL_PROXY` | 无 |
| Windows Internet Settings | `ProxyEnable=0`；`ProxyServer=127.0.0.1:18661`（**禁用状态**） |
| 127.0.0.1:18661 是否在监听 | ❌ 无监听，`curl -x` 报 `Failed to connect ... after 0 ms` |
| WinHTTP 代理 | 直接访问（无代理） |
| git / npm / pip 代理配置 | 均无 |
| hosts 文件 archive.org 条目 | 无 |
| 常见加速器安装目录（Clash / v2rayN / Shadowsocks / Netch / Steam++ / Watt） | 未发现 |
| 运行中进程（clash/v2ray/verge/netch/xray/sing-box/trojan/uu 等） | 无 |

⇒ **本机现在没有可用的加速器/代理。** 残留的 `127.0.0.1:18661` 很像某个加速器的本地端口（已禁用、无进程），**用户启动加速器后大概率就能跑**。

## 文件清单（name / size / md5）

**无 —— 元数据 JSON 没有取到（0 字节），没有任何文件条目可列。**

⚠️ **必须写清：「这次没列出来」= 没查到，⛔ 不等于「条目里没有这些文件」。**

## 下载

**无。** `d:/4/_tmp_dl/archive135/` 已创建但为空（0 个文件）。未执行任何下载物内容解析（也没有下载物可解析）。

## 归档内容里与卡牌相关的路径

**无 —— 没有任何归档被下载或列出（`7z l` / `tar -tf` 均未有机会执行）。**

## 关于 `allcards_assets_all_*.bundle` 哈希

- 任务要核对的两档：**`ea8bca4f5c6909d3124fd4975dc96173`**（当前 build 期望）与 **`56c290…`**（另一份更早 catalog）。
- 🔴 **两者都无法核对** —— 没抓到文件列表，无法判断该条目里是哪一个（或根本没有）。
- ⛔ **不猜测。**

## 没查到的部分（明确清单）

1. 该条目**是否存在/公开/已被删除**（页面本身没抓到）
2. **全部文件列表**（name / size / md5）与**总大小**
3. 是否含名字带 `cache` / `allcards` / `assets_all` / `catalog` / `bundle` / `card` 的文件，以及任何 `.zip/.7z/.tar/.gz/.apk/.obb` 归档
4. `allcards_assets_all_*.bundle` 的**真实哈希**是 `ea8bca…` / `56c290…` / 其它
5. 卡 SO 里的 `uniqueId` / `cardName` 是否可用于**补 118 张缺卡 ID** —— **完全没判**

## 重跑前置条件与步骤

1. **用户启动加速器**（全局模式或提供本地 HTTP/SOCKS 端口；系统里残留的 `127.0.0.1:18661` 是候选）。
2. 用一条命令验证通了再继续：
   ```bash
   curl -x http://127.0.0.1:18661 --max-time 30 -sS \
     "https://archive.org/metadata/warhammer-40-000-warpforge-1.35.0apkcache" | head -c 300
   ```
   （若加速器是 TUN/全局模式，直接 `curl` 即可，不用 `-x`。）
3. 拿到 JSON 后：列全部 files（name/size/md5）→ 挑最可能含卡牌 SO 的 1–3 个 → 下到 `d:/4/_tmp_dl/archive135/` → 只做 `7z l` / `tar -tf` / `python -I` 字节扫描。
4. 安全口径不变：下载物一律不可信，**不执行**任何脚本/二进制。
