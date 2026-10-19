# W57 · 工具线 · 收尾注释与 Net 残尾（TOOL-6）

> 2026-10-19 · 收尾执行代理 TOOL-6 · **只做不验**（未跑 Unity / 未起 player / 未跑 `*.Run`；只跑秒级 typecheck）
> 白名单 5 文件：`Shell/ForgeTab.cs` · `Shell/BoosterInfoPopup.cs` · `Core/Loc.cs`（只改注释）· `Net/NetTransport.cs` · `Net/NetSession.cs`。
> ⛔ 未动 git、未改正本、未删文件；⛔ 只改注释/日志文案，未改任何可执行逻辑、数值、断言期望。

## 一、四件逐项结论

### A · `Shell/ForgeTab.cs` 构建日志

- **改前现核**（`:492-494`）：构建期 `Debug.Log` 仍写「**词条表在远端 CCD、本地一个 value 都没有** ⇒ **正文留空、不自己编一个字**（见 `HelpTipBody`）」。
- **现实现**（`:568`）：`HelpTipBody => Loc.T("MainMenu/Forge/Help")`；官方词条在 `Core/Loc.cs:2791`（中英两列都有值）。
- **处置**：日志改为「**现取官方词条的值**（`Loc.T`，见 `HelpTipBody`；中英两列都在 `Core/Loc.cs` 的 `Table` 里）」，并按铁律 5 保留原文 + 更正痕迹（`W57`·TOOL-6，2026-10-19，含「写于官方本地化并入之前」的错因）。现 `:492-496`。

### B · `Shell/BoosterInfoPopup.cs`（文件头 + `HintGaps()` 出声）

- **文件头改前现核**（`:43-44`）：写「**词条表在远端 CCD、本地一个 value 都没有** ⇒ **正文留空、不自己编**」。
- **`HintGaps()` 改前现核**（`:686-691`）：运行期 `Debug.Log` 说「**三处**如实标注」、第 ③ 条 =「`Tooltip` … 正文**是空的** … 词条表在远端 CCD、本地一个 value 都没有 ⇒ 不自己编」。
- **现实现**（`:524`）：`TipBody => Loc.T("MenuShop/BoosterInfo/LegendaryTooltip")`；官方词条在 `Core/Loc.cs:2795`。
- **处置**：
  - 文件头（现 `:43-46`）：改为「直接取官方词条」，保留原文 + 更正痕迹。
  - `HintGaps()`（现 `:685-695`）：由「三处」改为「**两处**」（①服务端 item 文案/计数、②辉光动画），并在同一行日志里保留对旧第 ③ 条的更正痕迹（明写「已过期：自 `N73`（UI-3，2026-10-19）起 = 官方词条」）。

### C · `Core/Loc.cs:2777`（TOOL-5 点名的过期副作用说明）

- **改前现核**（`:2777-2778`）：仍写「⚠️ 副作用：其中 `ForgeTab.HelpTipBody = " "`（**故意留空**，配一条 `ShownBody.Trim() == ""` 的断言）一旦改成读本表，那条断言会红 —— 接线时一起办」。
- **处置**（现 `:2777-2781`）：整段替换为带日期的订正说明 —— 原文已过期；两个属性都已改读本表；`Editor/RewardsScene.cs` 那两条「正文空」断言已于同日由 `W57`·TOOL-5 改成「非空 + == 官方词条值」。
- **同一块内连带订正**（`:2787-2789`）：UI-4 那段里「`RewardsScene` 那两条断言**仍欠**」也已过期（TOOL-5 已办）⇒ 改为「已办」并写明新断言形态（`Loc.HasEntry` 前提 + 非空 + == 官方词条值，测试侧独立取 `Loc.T`）。
- 另把紧随其后的 UI-4 订正句里「上面**这两行**」改成「上面那段**原文**（`HelpTipBody = " "` 那两行）」，避免指向被替换后的文字。
- 词条值本身（`:2791` / `:2795`）**未动**。

### D · Net 残尾（漂移行号 → 符号锚点）

**验收复扫：`Pump:2` / `Pump:1` / `NetSession.cs:<数字>` 在 `NetTransport.cs` 与 `NetSession.cs` 均 0 命中。**

`Net/NetTransport.cs`：

| 改前（现核命中） | 现核真实位置 | 改成 |
|---|---|---|
| `:48` `NetSession.cs:247` | 静默超时判据实际在 `NetSession.cs:293` | `NetSession.Pump()` 里那道静默超时判据 |
| `:103` `Pump:213` | `Pump` 握手边沿在 `NetSession.cs:241`（`_lastAccepted = …` 在 `:250`） | `NetSession.Pump` 的**握手边沿**那一支 |
| `:104` `Pump:232` | 静默超时读连接位在 `NetSession.cs:293` | 同一轮 `Pump` 的**静默超时**判据 |
| `:109` `NetSession.Pump:213-215` | 同上 `:241-250` | `NetSession.Pump` 的**握手边沿**那一支 |
| `:110` `本文件 :300 一带` | `Send()` 实际在 `:507`、判连接在 `:509-510` | `Send()` 开头 `s == null || !IsConnected` 那一判 |
| `:113` `NetSession.cs:247` | `:293` | `NetSession.Pump()` 那道静默超时判据 |
| `:115` `NetSession.cs:213` | `:241` | `Pump` 的握手边沿那一支 |

`Net/NetSession.cs`：

| 改前（现核命中） | 现核真实位置 | 改成 |
|---|---|---|
| `:280` 上面那一支（`:213`）读计数 | `:241` | `Pump` 的**握手边沿**（`Role == NetRole.Host && _t.AcceptedCount != _lastAccepted`） |
| `:290` 下一帧 `:213` 那支 | `:241` | `Pump` 的握手边沿那一支 |
| `:304` `PeerLost` 那一支（`:176`）（同族额外） | 分支实际在 `:214` | `Pump` 里 `_t.PeerLost` 那一支 |
| `:406` 本文件 `:348 · :352 · :411`（同族额外） | `StatusText` 写点在 `:102` / `:330` / `:750`（`SetState`） | 本文件 `SetState` 那几处 |
| `:677` `:413 SessionToken = m.sessionToken`（同族额外） | 赋值实际在 `:457`（`Handle` 的 `NetKind.Ack` 那一支） | `Handle` 的 `NetKind.Ack` 那一支 |
| `:735-736` `NetBattle.cs:887` / `:890`（同族额外） | `_d.NetSelfResign(...)` 在 `NetBattle.cs:892`；`_s.StopReconnecting()` 在 `:895` | 符号锚点（`_d.NetSelfResign(...)` 那一句，紧接着本方法 `_s.StopReconnecting()`） |

> 额外 4 条都是「**同一类漂移**」且都在白名单内、只动注释；`NetSession.cs:366` 的 `:181` 现核是**准的**（同一条键的 `SetState`），未动。

## 二、改动清单（5 文件；行尾均 LF，Python 二进制复核 CRLF 0）

| 文件 | 位置（改后现行号） | 改了什么 |
|---|---|---|
| `Unity/MyGame/Assets/CardPresentation/Shell/ForgeTab.cs` | `:492-496` | 构建日志：正文「留空/不自己编」→「现取官方词条值」+ 更正痕迹 |
| `Unity/MyGame/Assets/CardPresentation/Shell/BoosterInfoPopup.cs` | `:43-46` | 文件头：同句过期 → 直接取官方词条 + 更正痕迹 |
| 同上 | `:685-695` | `HintGaps()`：「三处」→「两处」，去掉「正文是空的」，保留更正痕迹 |
| `Unity/MyGame/Assets/CardPresentation/Core/Loc.cs` | `:2777-2781` | `HelpTipBody = " "` 的过期副作用说明 → 现状态订正 |
| 同上 | `:2782` | 「上面这两行」→「上面那段**原文**」（指向修正） |
| 同上 | `:2787-2789` | RewardsScene 两条断言「仍欠」→「TOOL-5 已办」+ 新断言形态 |
| `Unity/MyGame/Assets/CardPresentation/Net/NetTransport.cs` | `:48` · `:103-105` · `:109-111` · `:112-116` | 7 处漂移行号 → 符号锚点（`Pump` 两支 / `Send()` 那一判 / `Setup`+`AcceptedCount`） |
| `Unity/MyGame/Assets/CardPresentation/Net/NetSession.cs` | `:280-281` · `:291` · `:304` · `:407` · `:677` · `:735-736` | 6 处漂移行号 → 符号锚点 |

`git diff --numstat`（**含 TOOL-3/4/5 及 UI 线在本分支这些文件上的既有改动**，非本轮单独增量）：
`Loc.cs 15/2` · `NetSession.cs 68/27` · `NetTransport.cs 42/7` · `BoosterInfoPopup.cs 21/19` · `ForgeTab.cs 13/10`。

行尾复核（二进制）：ForgeTab CRLF 0 / LF 1,035 · BoosterInfoPopup 0 / 707 · Loc 0 / 3,625 · NetTransport 0 / 655 · NetSession 0 / 796 —— **一个都没被翻**。

## 三、没查清的部分（未改，只报）

1. **`Net/NetBattle.cs:1111` 的 `NetSession.cs:334` 已漂**：现核 `NetSession.cs:334` 是分隔注释；「本局**没有设密码**」那条 `Debug.LogWarning` 实际在 `NetSession.cs:400`。⚠️ **`NetBattle.cs` 不在本代理白名单** ⇒ 未改。
2. **`Core/Loc.cs` 内还有大量跨文件行号锚点**（例：`:2228-2230` 的 `NetSession.cs :102/:573…`、`NetTransport.cs :420…`；`:2314-2315` 的 `NetMatchmaking.cs :197…`）。本件点名范围是 Forge/Booster 词条与 Net 残尾，**这批未逐条现核**，引用条数/行号一律以现读为准。
3. **`Shell/BoosterInfoPopup.cs:106`** 引用 `Shell/MenuDraw.cs` 的 `:1068` —— 未核（不在本件范围）。
4. **`NetSession.cs:721` 的 `:26-27`** 指的是反编译文件 `BattleManager__FailedToReconnectAfterDisconnect.c`，不是本仓源码 ⇒ 未动。
5. 本波**未跑任何 Unity 自检 / 未起 player**（红线）。

## 四、typecheck 结果

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

- **我的 5 个文件**：全部属于运行时程序集（`Shell/`、`Core/`、`Net/`）⇒ **0 错**。
- **关于 `RuleCore.cs` 在飞**：TOOL-5 报告里那 3 条 `CS0103: EffectResolver`（`Assets/RuleEngine/Core/RuleCore.cs:8981-8983`）**本轮已不再出现** —— 第一次跑就是 0/0，之后又复跑两次仍 0/0。判断：那批在飞代码已落定或被他人改走，**与本代理无关**（本代理一个字没碰 `RuleEngine/`）。
- 秒级 typecheck 共跑 3 次（改完 A/B/C/D 主体后、加完同族额外订正后、最终确认），结果均为 **0/0**。
