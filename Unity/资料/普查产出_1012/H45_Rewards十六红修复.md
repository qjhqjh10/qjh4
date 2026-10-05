# H45 · `RewardsScene.Run` 十六条红 —— **照 D10 落地修复**

> 写手代理 · 2026-10-13 · 落地文件 = `Unity/MyGame/Assets/CardPresentation/Editor/RewardsScene.cs`（**只改夹具/断言**）。
> 根因与最小改法**全部照** `资料/普查产出_1012/D10_Rewards十六红诊断.md`（⛔ 未重查）。
> ⛔ 没跑 Unity（`RewardsScene.Run` 是**主对话在同步点的验收点**）· ⛔ 未动 git · ⛔ 未动 `Shell/*` 与任何正本 · 未越白名单。
> ⚠️ **行号以本文件写作时刻的现读为准**（D10 与本次的编号一致；但同文件**有别的写手在并发改**，见 §顺手发现 ②）。

**结论**：D10 的两个根因、两个改法**均已落地**，共 **5 处编辑 / 3 条新增语句**（`rw.Build()` × 3 + 一处**顺序调换**）。
类型检查 **0/0**（运行时 0 · 编辑器 0）。十六红**未实跑验证**（按调度台口径，本轮不跑自检）。
⚠️ **本文件的行号是「落笔时刻」的读数**，而**同文件有别的写手在并发增删** ⇒ 过一会儿可能整体漂几行 —— 认位置请按**锚点句**（那三句 `rw.Build();` / `ForceStreakClaimedForTest(dClaim, false);` 与它下面那句 `bool claimed0 = …`），⛔ 别按裸行号去 `sed`。

---

## 一、改动清单

| # | 位置（现读） | 改动 | 为什么 |
|---|---|---|---|
| ① | `RewardsScene.cs:5311` | 新增一句 `rw.Build();`（+ 9 行注释） | 红 1–6：ctx#2（12 条）再开时 `OpenWindow` 落在 `Open` 支、**不重建内容** ⇒ 量到的还是首开那 2 条 |
| ② | `RewardsScene.cs:5416` | 新增一句 `rw.Build();`（+ 3 行注释） | 红 7–9：ctx#3（判别档）同理 —— `_premium` / `_reveal` 都只在 `Build()` 里置 |
| ③ | `RewardsScene.cs:5448` | 新增一句 `rw.Build();`（+ 3 行注释） | 红 10–13：ctx#4（预览档）同理 —— 四件可见性全读 `Build()` 里的 `_ctx` |
| ④ | `RewardsScene.cs:5939-5955` | **把 `ForceStreakClaimedForTest(dClaim, false)` 挪到 `claimed0` 读取之前**；前提断言文案改成「**已被本节显式置成「没领过」**」；附 9 行订正注释 | 红 14–16：原来读到的是**上一段残留**（§五 `CheckAbsorbRule` 收尾真点了压暗层 ⇒ `DailyStreakPopup.cs:222` → `StreakAutoCollect()` → `DailyData.cs:852` 置位） |
| ⑤ | `RewardsScene.cs:6031-6035` | 订正一段**已过期**的注释（「`claimed0` 就在手上」那半句） | 铁律 5：`claimed0` 的取法变了，注释留着会把下一个会话引向旧口径 |

🔴 **`：6004` / `：6085` 两处还原一个字没改** —— 它们仍取 `claimed0`（现在恒 `false`）⇒ 红 14/15/16 一起转绿；**新增语句共 3 条**，其余全是注释。

**⛔ 刻意没做的两件事**（都是 D10 点名过的坑）：
- **没**把 ①②③ 写成「先 `rw.Close()` 再开」：`RewardWindow.Close()` 先发 `_ctx.OnClose`（`Shell/RewardWindow.cs:845-850`）
  ⇒ ctx#1 那颗 `closeCalls++` 会先 +1，**把 §⑩ 的 `Check(closeCalls, 1)`（`:5495`）打红**。
- **没**把 ①②③ 写成 `wm2.OpenWindow(rw); rw.Build();` 的无参两行写法：无参那条重载**不调 `SetupData`**
  （`Shell/WindowsManager.cs:446-449`，判据 = A447 亲读指令流）⇒ `_ctx` **留在上一档**，`Build()` 照旧画旧内容。
  必须是「**带参 `OpenWindow`**（换 `_ctx`）+ **显式 `Build()`**」这一组合（已在注释里写死）。

---

## 二、逐条：修的是什么 · 改坏法

| 红 | 修的是什么 | **改坏法**（删掉/改回哪一句 ⇒ 哪些红回来） |
|---|---|---|
| **1–6** | `Open` 支不重建 ⇒ 补显式 `Build()`（①） | 删 `:5311` 的 `rw.Build();` ⇒ 红 1（`ClampHi = 520`）· 2（抽屉 12 vs 实得 2）· 3（数量文字 8）· 4（`bandMax > 1725`）一起回来；**5、6 是红 4 的算术后果**（那条 label 不在渐隐带 ⇒ 剖面恒 1）⇒ 随红 4 一起回来 |
| **7–9** | 同上（②） | 删 `:5416` 的 `rw.Build();` ⇒ 红 7（`Premium Disclaimer` 关着）· 8（`RevealProgress` 停在 1.00）· 9（`MaskPad.x` = 0）回来。⚠️ **只补 ① 不补 ②③ ⇒ 红 7–13 照红** |
| **10–13** | 同上（③） | 删 `:5448` 的 `rw.Build();` ⇒ 红 10（底图没换 Preview）· 11（`Collect Button` 还开着）· 12（`Tap To Continue` 没关）· 13（`Premium Disclaimer`）回来 |
| **14** | 起点快照改取「**显式置位之后**」的读数（④） | 把 `:5950`/`:5951` 两句**换回原顺序**（先读 `claimed0` 再置位）⇒ 读到残留 `true` ⇒ 这条红 |
| **15/16** | 同一个 `claimed0` 的**下游**（`:6004` / `:6089` 的还原） | 同「改坏法（14）」⇒ 那两处把格子还原成 `true` ⇒ 「Collect **又画出来了**」两条一起红。⛔ **另一条（更弱）的改坏法**：只把 `:6004`/`:6089` 删掉（不还原）⇒ **不会红**（`claimed0` 已恒 `false`，格子的态本来就对）—— 记在这里免得被当成「这两条咬得住随意改动」 |

🔴 **⛔ 没有为了让它们变绿而放宽任何断言**：十六条的期望值、容差、文案一个字没动；只**补语句**与**调顺序**。

---

## 三、⛔ 没查清（按 D10 §没查清 的编号）

1. **D10 §没查清 ①（`rw.Build()` 的副作用）仍未实跑**。**粗核的结论**：看着干净 —— `Build()` 首句清空全部 root 子件
   （`:1009`）+ `_scroll`/`_listHolder`/`_itemNodes`/`_punches` 全清（`:1012-1020`），`_ctx` 由带参 `OpenWindow` 的 `SetupData` 先换好，
   `Build()` 再读它。**但**「`PointerLayer.RegisterScroll` 那份登记表」有一处**下面那条**独立问题，且
   **只有真跑 `RewardsScene.Run` 才算数** —— 本件的验收点就是它。
2. **D10 §没查清 ③（A302 段会不会转成另一批红）未验** —— 那四条（红 4/5/6 加 `qLabels == 8`）本件只按 D10 的算式改动，
   **实跑前不写「一定转绿」**。
3. **D10 §没查清 ⑤（`DailyRewardPopup` 的 `dr` 有没有同型「同窗再开」）没做** —— 不在本件范围（只修那 16 条）。
4. **`closeCalls` 在方案 (a) 下的终值（D10 §没查清 ②）未核完** —— 只确认了 ①②③ **都不触碰** `OnClose`：
   ctx#2 不带 `OnClose`，ctx#3 不带，ctx#4 带的是它自己那颗；§⑩ 的 `CheckAbsorbRule` 仍是**唯一**一次关窗。**没跑**。

---

## 四、顺手发现（**只报不改**，⛔ 越界）

1. 🔴 **【与 QA 相关 · 强烈建议调度台分流】一个连登轨的口径歧义 —— 它决定红 14–16 的「改坏法」哪种才对**。
   `DailyData.cs:828` 的读口是 **`_streakClaimed[SI(i)] || SI(i) < StreakCollected`**；今天是 `StreakCollected = 5` 且
   `SI(i) >= 5` ⇒ **只有前半截在决定**。若**将来把 `StreakCollected` 调小**（或夹具跑在更小的 `StreakCollected` 上）：
   `ForceStreakClaimedForTest(i, false)` 之后 `StreakRewardClaimed(i)` 会因为 **`SI(i) < StreakCollected`** 仍读 `true`
   ⇒ 本次这条前提断言会**立刻变红**，而且「删掉 `:6004`/`:6089` 的还原也不红」那条**弱化的改坏法**会反过来咬人。
   **建议**（不在本件白名单内）：改成取**那个守卫真正问的那位**（`StreakRewardUnlocked` 或裸 `_streakClaimed`），
   或把前提断成 `CheckTrue(DailyData.StreakRewardUnlocked(dClaim), …)`。**本件按 D10 原样落地，⛔ 未擅自改动**。
2. 🔴 **`RewardWindow.Build()` 每次重建都 `RegisterScroll` 一个新的 `MenuScroll`，却从没撤过旧的** ——
   `Shell/RewardWindow.cs` 全文件 **零命中** `PointerLayer.UnregisterOwnedBy`。而 `MenuScroll` **不是 Unity Object**
   （普通 `class` ⇒ `== null` 永不成立），`RegisterScroll` 自带的 `PruneScrolls()`（`PointerLayer.cs:191`）只清
   「`s == null` 或 `Owner == null`」⇒ **`Owner` = 窗根（still alive）的那一堆旧条目永远留表**，
   而它们**仍会被滚轮命中**（`PointerLayer.cs:195-200` 白纸黑字记着这颗雷：表现 = 「登记表每次开窗涨一批，
   而且旧条目还能被滚轮命中」）。同族 6 处（`PlayerProfileWindow.cs:683` · `InboxWindow.cs:383` · `FriendsTab.cs:195` ·
   `ChatPanel.cs:507` · `BattleLogPopup.cs:162` · `SettingsWindow.cs:663`）都补了那一句，**本窗没有**。
   **本件① 每档会让这扇窗多留一条**（约 +3），**但这是既有缺陷**（生产路「关窗→重开」同样每次 `Build()` 涨一条）。
   ⛔ **`Shell/*` 不在白名单 ⇒ 本件一个字没动**；建议进 A 表（判据齐、改法一行）。
3. ⚠️ **本文件在并发被改**：`git diff` 里除了本件 5 处，还有**别人**的活（`A327` 两态夹具 / `CheckScaleTwo` /
   `WindowsManager.EnsureHost` 那段注释）。本件所有改动**是纯增**（新增 3 句 + 换序 + 注释），
   **改完已回读 5 处站点逐条核对**；但**同一文件两写手**这件事本身请调度台留意（撞车会静默覆盖）。

---

## 五、验收状态

- ✅ **类型检查 0/0**（`TMPDIR=/tmp/wf_h45 bash d:/4/Unity/工具/typecheck.sh` ⇒ 运行时 `0` · 编辑器 `0`，
  在**最后一次编辑之后**跑的；无一条错落在别人文件上）。
- ✅ 行尾：本文件全 LF（`CRLF 0 / LF 8499`）—— 与改动前一致，**没翻行尾**。
- ⏳ **`RewardsScene.Run` = 验收点，本件未跑**（按调度台口径：收尾时跑）。十六红是否真转绿**以那一次为准**。
