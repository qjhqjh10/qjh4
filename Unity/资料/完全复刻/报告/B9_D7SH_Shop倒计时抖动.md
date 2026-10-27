# B9_D7SH —— `ShopScene.Run` 两条「倒计时文本」红：时间型抖动的根因与修法

**结论一句话**：不是容差太小，是**容差根本没生效** —— 那条断言的兜底只认 `X小时 Y分钟` 一档，
而失败时剩余 < 1 小时、格式化走的是 `MM分钟 SS秒` 档（没有小时段）⇒ 解析失败 ⇒ 退回**逐字相等** ⇒ 秒数一动就红。
已按「两个读数各自反解成区间 + 用实测漂移窗口解释」重写，判别力未削弱。`typecheck` = 0/0。

- 改动文件：**只有** `CardPresentation/Editor/ShopScene.cs`（+91 / −19）
- ⛔ 未动 `Shell/TimerDisplay.cs`（不需要动 —— 见 §二·3）
- ⛔ 未跑 Unity（由调度台统一复跑）；未动 git / 正本 / 账主表

---

## ① 断言原文 + 抖动机制

### 原文（改前，`Editor/ShopScene.cs` 旧 `:1595-1611`，在「页签切换」那个 `for (p = 0..2)` 循环里 ⇒ 每页跑一次）

```csharp
string tLabel = TextOf(FindChild(tc, "Time"));       // 屏上那颗 `Time`
string tNow   = ShopData.RefreshTime;                // 活值（= TimerDisplay.ShopRefreshText()）
var rx = new Regex(@"(\d+)\s*(?:小时|h|H)\s*(\d+)\s*(?:分钟|m|M)");
bool timerOk = tLabel == tNow;
if (!timerOk && mLabel.Success && mNow.Success) { … timerOk = |a − b2| <= 1; }   // 「差 1 分钟算同一读数」
```

### 抖动机制（三条叠在一起）

1. **标签是快照，不是活值。** 三页的 `Time` 字由 `Shell/ShopTabPage.cs:341-357` 在 `Setup()` 里
   `_win.Text(tc, ShopData.RefreshTime, …)` 写一次，再 `TimerDisplay.Attach(...)` 当场算一次。
   批处理**没有帧循环** ⇒ `TimerDisplay.Update()` **一次都不跑**
   （`Shell/TimerDisplay.cs:116-122`）⇒ 标签永久停在**页面建成那一刻**。
   参考时刻：`ShopWindow.Create` → `EnsureTabsSetup()`（`Shell/ShopWindow.cs:147,250`）—— 三页在**同一次 `Build()`** 里一次建完。
2. **三页的断言不是同一刻跑的。** 日志三条依次是
   `✓ 45分钟 52秒`（第 1 页，差 0s）· `✗ 45分钟 50秒`（第 2 页，差 2s）· `✗ 45分钟 48秒`（第 3 页，差 4s）
   —— 每页自己的那批断言要跑 ~2 秒（日志行号相距 ~3100 行），轮到第 2/3 页时**必然**已经过了几秒。
3. **兜底解析对这一档不成立（真根因）。** `TimerDisplay.FormatTimeString` 一共六档
   （`Shell/TimerDisplay.cs:194-215`）：剩余 > 24h 走 `D天 H小时`、> 1h 走 `H小时 M分钟`、否则走 `M分钟 S秒`。
   旧正则**要求小时段和分钟段都在** ⇒ 剩下不到 1 小时时（`45分钟 52秒`）**匹配不上** ⇒ 退回 `tLabel == tNow`。

### 为什么「上一轮 2,406 / 0，这一轮才红」

`MS` 这一档**只有剩余 < 1 小时**才会走到 ⇒ 商店刷新时刻按 9 点规则算
（`TimerDisplay.ShopRefreshAt`，`CardPresentation/Shell/TimerDisplay.cs:245-257`）
⇒ **只有本机时间落在 08:00–09:00 之间跑，这条才会踩进「秒级」那一档**。
那两条红的标签是 `45分钟 52秒` ⇒ 当时本机时间 ≈ **08:14**。
其余时段剩余 ≥ 1h（`19小时 59分钟` 那种），2026-10-18 那次 ±1 分钟的兜底**恰好**管用 ⇒ 一直绿。
⇒ **这不是「偶发」而是「每天 8 点到 9 点必红」**（只要那一小时内有人跑 `ShopScene.Run`）。
⇒ 与 `animfx_sounds.json` / `ProbeTab` 挪目录**无关**，判 (δ) 时间型抖动成立，**不是回归**。

---

## ② 改法（为什么这样改）

### 1. 让「两侧各带自己那一档的宽度」

新增 `ShopScene.ParseTimerText(text, out lowSecs, out fineSecs)`（追加在**类尾**，见 §四·3）：

> 把一条 `FormatTimeString` 的输出反解成 **剩余 ∈ [low, low + fine)**，
> `low` = 各项「数字 × 单位秒数」之和，`fine` = 这一串里**最细**那一档单位的秒数。
> **依据**：格式化**只截断不四舍**（`ts.Seconds` / `ts.Minutes` / `ts.Days` 全是截断）
> ⇒ 被丢掉的那一段**正好就是** `fine`。
> 六档全认（官方三档 + 我们拼的三档 `_Only` + en 三档），`最后时刻` / `Last Minutes` 前缀先剥掉。
> **解析不出 ⇒ `false`**（空串 / `--` / 别的形状）。

### 2. 用「实测漂移窗口」代替硬编码容差

在 `RunCore` 建场**之前**记 `tPagesBuilt`（`Build()` 之前取 ⇒ 窗口只会算宽、不会算窄 ⇒ 不会造成新的假红），
断言处算 `driftSec = now − tPagesBuilt`，然后：

```
① lLow + lFine >  nLow                        标签的剩余不低于活值的剩余（剩余只会往下走 ⇒ 倒着来必红）
② lLow        <=  nLow + nFine + driftTol     标签的剩余不老过「活值 + 实测漂移」
③ liveOk: nLow <= cLow + 1 && nLow + nFine > cLow - 1    活值对得上【现在这一刻的时钟】
```

其中 `cLow` = `(ShopRefreshAt(now) − now).TotalSeconds` —— **时钟自己算的**，**不经过 `ShopData.RefreshTime`**
⇒ 两个读数互不印证，换回死串时 ③ 必红（旧版拿 `ShopData.RefreshTime` 同时当被测与判据 = 自证）。
`driftTol = 2 + ceil(driftSec)`；`+2` 是两次读数之间那点秒级偏斜。

**上界为什么必须取 `nLow + nFine`（活值那一侧的上确界）**：这是第一版写错、当场自纠的一处 ——
只拿秒级读数当上界，会误杀「标签落在分钟档上沿、活值已经跨进下一分钟」这种**合法**情形
（`19小时 59分钟` vs `19小时 58分钟` 差 1 分钟、实际只过了 2 秒，2026-10-18 那批就是这么红的）。
两侧都带宽度之后，这两族 flake 由**同一套判据**覆盖，不需要两个特例。

**9 点那一拍**：若窗口内 `ShopRefreshAt(tPagesBuilt) != ShopRefreshAt(now)`（= 正文跨过 9:00 重置），
目标时刻整个滚到次日，标签与活值本来就该差一天 ⇒ 该次只要求标签**解析得出**，
并在消息里**打印**「⚠️ 本窗口内跨了 9 点重置那一拍」。这是**如实放行**，不是放宽
（`rolled` 由两个时刻各自的 9 点规则算出来，不是「像时间就行」）。概率 ~10s/24h。

### 3. 为什么没去「钉死时钟」（调度台给的首选做法）

`TimerDisplay.Refresh()` 里 `var now = System.DateTime.Now;` 是**硬编码**（`Shell/TimerDisplay.cs:128`），
没有注入点；而白名单⛔不许动 `TimerDisplay.cs`。
**也不需要动**：标签天然是「某个时刻的快照」，只要知道那个时刻的**窗口**，就既不用改产品、又能做到无抖动
—— 而且比「钉死时钟」多验了一条（活值是否真的跟着本机时钟走）。

### 4. 判别力（没退化成「非空就算过」）

用最终判据跑了八个用例（真实 flake + 对抗用例，逐条 `PASS/FAIL` 都对）：

| 用例 | 结果 |
|---|---|
| 本轮第 2 页 `45分钟 52秒` vs `45分钟 50秒`（漂移 3s） | **PASS** |
| 本轮第 3 页 `45分钟 52秒` vs `45分钟 48秒`（漂移 5s） | **PASS** |
| 2026-10-18 那族 `19小时 59分钟` vs `19小时 58分钟`（漂移 2s） | **PASS** |
| 正常同秒 | **PASS** |
| 跨小时档 `1小时 0分钟` vs `59分钟 59秒` | **PASS** |
| 跨天档 `2天 0小时` vs `1天 23小时` | **PASS** |
| **死串（3 小时前的值）** vs 活值 | **FAIL**（②） |
| **死串（反向：剩余偏小）** vs 活值 | **FAIL**（①） |
| 空串 / `--` | **FAIL**（解析不出） |

⚠️ 断言**条数不变**（每页仍 1 条 `CheckTrue`）⇒ 复跑合计应回到 **2,406**。

---

## ③ 同类断言清单（「拿活值比文本」的时间型断言 —— 这族迟早还会抖）

扫了 `Editor/*.cs` 里所有 `TimerDisplay.*` / `DateTime.Now` / 定时文本读者，**只有一处**是同族且**真会抖**：

| # | 位置 | 形状 | 会不会抖 | 处置 |
|---|---|---|---|---|
| 1 | `Editor/ShopScene.cs:1611-1642`（改后的「倒计时文本」） | 拿**冻结标签**比**活值** | **会（已修）** | 本轮修完 |
| 2 | `Editor/RewardsScene.cs:2546-2549` | `timerText.StartsWith(前缀) && Length > 前缀.Length` | 不抖（不比数值），但**没有鉴别力**：前缀对、后面是死串 ⇒ 照样绿 | ⛔ 不在本件白名单，**没动**；建议后续用 `ParseTimerText` 那一族补一条「值也得在窗口内」 |
| 3 | `Editor/MainMenuScene.cs:11824` · `9437-9446` · `Editor/SkirmishWindowAssertE15.cs:209` | 期望 = `TimerDisplay.Compose(词条, "--")` | 不抖 —— 本地无截止时刻 ⇒ **值恒 `--`**，与时钟无关 | 已经是「正确形状」的样板，不必改 |
| 4 | `Editor/RewardsScene.cs:9520-9531` | 期望 `2天 0小时` / `2d` | 不抖 —— 那条读的是 `EphemeralEndTime − EphemeralStartTime`（`Shell/Inventory.cs:806` 走 **start** 那一支，是**时长**不是剩余）⇒ 与时钟无关 | 不必改 |
| 5 | `Editor/BattleScene.cs:30236-30247` | `TimerTextForTest == "0:07"` | 不抖 —— `SetTimer(7)` / `CountdownTrigger(5)` 是**喂进去的**计数，不走 `DateTime` | 不必改 |
| 6 | `Editor/SettingsScene.cs` | 无定时文本断言（唯一 `Stopwatch` 是联机超时轮询，6 秒上限） | 不抖 | — |

**判据（给下一次查这族的简式）**：一条断言只要**左边是屏上的字、右边带 `DateTime.Now` / `ShopData.RefreshTime` /
`DailyData.*TimerText()` / `TextFor(NextResetAt, …)`**，就必须问两句：① 这串字**是什么时刻**写下的（快照还是活值）？
② 那两者之间最长能隔多久（= 容差该怎么算）？—— **快照 + 活值**这一对**不能**用「同一秒相等」判。

---

## ④ 顺手发现（都没改，只报）

1. **「`TimerDisplay` 挂上去的字在批处理里永远是快照」是一条跨宿主的坑，不只商店。**
   `Shell/` 里至少有 8 个消费点走 `TimerDisplay.Attach`（`ShopTabPage` · `DailyRewardPopup` · `DailyStreakPopup` ·
   `MissionsTab` ×3 · `MainMenuRuntime`），谁先写一条「拿活值比这颗字」的断言，就会重现本轮这个形状。
   ⇒ 建议把 `ShopScene.ParseTimerText` 这一族**收口到 `Editor/MenuCheck.cs`**（本轮没收：那是共用件、
   且同一时刻可能有别的写手在动，留给出同步点的调度台裁）。
2. **文档里的 `Editor/ShopScene.cs:<行号>` 全部后移了**（本轮 +72 行）。
   偏移：原 `1592 < N ≤ 5820` ⇒ **+32**；原 `N > 5820` ⇒ **+74**；`N ≤ 1274` 不动。
   真·活文档（该改）：
   - `资料/完全复刻/判据/存档体系_原版语义与施工方案.md:201` → `Editor/ShopScene.cs:1879`（`InventoryStore` 落盘那句注释）⇒ 应是 **1911**
   - `资料/完全复刻/报告/B9_B1UI_开场链与offer底图.md:439` → `ShopScene.cs:1614`（`var offers = ShopData.Offers(p);`）⇒ 应是 **1646**
   - `资料/完全复刻/判据/自设计清单.md` 是 `工具/selfdesign_census.py` 的产物（重跑即重排），**别手改**。
   其余 `资料/普查产出_1xxx/…` 里的行号是**当时快照**，⛔ 不要回填（那是历史记录）。
3. **账（`账/*.tsv`）零影响**：`check_ledger_points.py`（只读跑了一遍）报 A 3731 · 通过 3388 · 漂移 275 · 失败 68，
   **其中没有任何一条落在 `Editor/ShopScene.cs`**（全表 grep `ShopScene` 零命中）⇒ 本轮不产生账漂移，
   不需要 `--fix`。另外按纪律把新增成员 `ParseTimerText` **追加在类尾**（⛔ 没插在既有成员之间）。
4. **行尾**：改前改后都是纯 **LF**（`CRLF=0` / 裸 CR=0，二进制数出来的）⇒ 没有翻行尾。
5. **一个观察（不是缺陷）**：这条断言的三次运行**共用同一个 `tPagesBuilt`**，第 3 页的漂移窗口把第 1/2 页
   跑检查的时间也算进去了（本轮实测 ~5s）。这只会让窗口偏宽几秒，**不会误红**；
   真要更紧，得让 `ShopTabPage.Setup()` 自己记时间戳（那要动 `Shell/`，不在白名单）。

---

## 验收

```
TMPDIR=/tmp/wf_D7SH bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---  运行时错误数: 0
--- 编辑器程序集 ---  编辑器错误数: 0
```

⛔ 未跑 Unity（`ShopScene.Run` 请调度台统一复跑；期望 **2,406 / 0**，断言条数不变）。
**没有**动 `Shell/TimerDisplay.cs`（§二·3：不需要）。
