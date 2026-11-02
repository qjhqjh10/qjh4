# `B117-FIXC` · 选择器 / 点行 / ESC 那一簇失败

执行代理 `B117-FIXC` · 2026-11-03 · 写者白名单 = `Editor/BattleScene.cs`（本报告除外）
基线 = `_tmp_view/battle.log`（`BattleScene.Run` 那一跑）

> **一句话**：这一簇 **9 条红里 8 条是「断言过期」**（生产判据按原版改了，断言还钉着旧行为），
> **1 条（音高/音量）是断言取值时机错**。**没有一条是复刻回归**。
> 全部改动 **零行数变化**（就地改写已有物理行），`log.Hide()` 仍在 `:7254`（与调度台快照一致）。

---

## 一 · 逐条根因与改法

| # | 断言（`battle.log` 行） | 根因 | 分类 | 改法（`BattleScene.cs`） |
|---|---|---|---|---|
| 1 | `711134` ★ 选完打法，选择器收起 | `WSEL` 补丁① 删了 `CommitCommand` 里的 `selector.Hide()`（原版 `UnitOnBoardAttackTypeSelector__AttackButtonClick.c` **全篇无 `Toggle(false)`**，活到 `StopTracking`） | **断言过期** | `:9438` `!driver.SelectorOpen` → `driver.SelectorOpen`，文案改成「**仍开着**」并写判据 |
| 2 | `714337` ★ 定下打法 → 选择器收起 | 同上 | 断言过期 | `:10179` 同上 |
| 3 | `968305` ★ A462「悬停即选中」 | 同上（这一条断的是 `!SelectorOpen && Command == Ranged`，**只有前半过期**；`Command == Ranged` 仍成立，探针 `Ballista` 的 `LastAttackType = 0`） | 断言过期 | `:34541` 只翻前半，文案同步改「选择器仍开着」 |
| 4 | （顺手）`:31450` 的「收起，别留给下一节」 | 该行靠「commit 会关窗」收尾 —— 补丁① 之后 commit **不再关窗**，补丁③ 还会让**同档 commit 直接 return** ⇒ 选择器会留给下一节 | 断言/夹具 | `driver.SimulateCommand(Melee)` → `driver.SimulateDeselect()`（走 `ClearSelection` 那一口） |
| 5 | `707402` ★ 点行 ⇒ 大卡窗显示的**就是那一行那张卡**（实测 `猎鹰` / 期望 `Falcon`） | **显示名 vs 英文键名**（不是取错卡）。`ShownTitle` = 卡面显示名；`wantKey` = `RowLinkKey` = **英文键**。中文档下两者结构上不可能相等 | 断言过期 | `:7224` 改断 **身份**：`CardDisplay.FrontDef.Name == wantKey`（与语档无关）+ `ShownTitle` 非空（保留「窗真的开着 / 小预览卡收掉」那两半） |
| 6 | `707454` ★ 换一行再点 ⇒ 选中行跟着换 | `B117-49` 把命中判据从「整行底板矩形」收窄成**行内 TMP 链接**（原版 `CemeteryManager__ClickCardLink.c:56-70`）⇒ 原来点的第 1 行**没有链接** ⇒ `LinkRowAt = -1` ⇒ 这一下没被吃掉 | 断言过期（判据收窄后夹具形状不成立） | `:7245-7249` 先找**第二行带链接的**；本局只有一行链接时退回**文档化降级口** `ForceLinkProbeUnavailableForTest`（那一档验的是「选行记账跟着换」，链接命中另有 `02b-2` / `B117_WBD` 专门断言），并在文案里**打出走了哪一档** |
| 7 | `987354` ★ 点第 1 行 ⇒ 被吃掉 + 记下选中行 | 同上：本段面板刚 `Show()` 就点，批处理无帧循环 ⇒ `FindLinkProbe` 扫不到压在字形上的点 ⇒ 生产判据（只认链接）答 -1 ⇒ 没被吃掉 | 夹具环境 | `:37200` 命中口走 `ForceLinkProbeUnavailableForTest`（**只包住这一下点击**，用完立刻复位）。⚠️ 本段验的是 **`hideCemetery` 闸门**，不是链接命中 |
| 8 | `987677` ★ 闸门关着时仍被吃掉、但不记选中行 | 同上 | 夹具环境 | `:37248` 同上 |
| 9 | `708073` ★ ESC ⇒ 打开设置窗 | `WSEL` 补丁② 之后 `_command` **起手就 = 卡的 `attackType``（`OpenCommand`）⇒ 只要上一段留着 `_selectedSlot`，`IsTargeting` 就为真 ⇒ `HandleSettingsEsc` 第二闸（原版 `UIstate == normalBattle`）**正确地**吞掉 ESC | 夹具前提不成立 | `:7767-7768` 点之前 `drv3c.SimulateDeselect(); drv3c.RefreshAll();`，并把前提断言扩成「设置窗关着 **且 `!IsTargeting`**」（⛔ 没放宽生产闸） |

### 顺带（调度台转派，均已在同一文件落地）

| 来源 | 位置 | 改法 |
|---|---|---|
| `FIXB`（大卡展示窗 22 条红） | `:7254` | `log.Hide(); if (drv.CardDisplay != null) drv.CardDisplay.Hide();` —— `B117-48` 之后「点日志行」的终点是大卡窗**且留着**（原版同），本段收尾必须关回去，否则下游「棋盘轻点」整节每条断言错一位 |
| `FIXA`（音高/音量 2 条红） | `:17002`（`var cueAe = …` 那一行） | 追加 `RemnantSfx.ResetThrottle(); RemnantSfx.Play(RemnantSfx.Moment.Death, true);` —— 上一格 `ResetCounters()` 按契约把 `LastPitch/LastVolume` 清零、`collectDeath: true` 那次**故意不播** ⇒ 断言读到的是清零值（**取值时机**，不是时钟）。⛔ 没碰 `ResetCounters` 的清零契约（那会打红 `B117_G10_12` 的 B1） |

---

## 二 · 为什么 `707402` 是「显示名 vs 键名」而不是取错卡（证据链）

1. 日志行可见文字 = `你出牌 猎鹰战车`（`battle.log:707136`），行内链接键 = `Falcon`（`battle.log:707175` 那条断言通过）。
2. 大卡窗标题 = `猎鹰`。
3. 两个名字**各自都来自生产代码**，只是**分别来自两张同名的卡**：
   - 日志行的 `LinkText` 走 `BattleDriver.Zh(e.CardId)` → `CardDatabase.Find(_pool,"Falcon")` → **卡池**那张 `ASH27`，`CardDef.NameZh = 猎鹰战车`；
   - 场上的那一张是 **`StarterCards.Unit("Falcon", …)`**（`RuleEngine/Data/StarterCards.cs:56`，**自造起始卡、没有 `NameZh`**）⇒ `CardText.Name(c)` 落到 `Card_Name/Falcon` 那条手写键 = **`猎鹰`**（`CardPresentation/Core/Loc.cs:2712`）。
   - `ShowLogCardLarge` 优先取**场上那一份**（`u.Card.Name == key`）⇒ 窗里画的是玩家手里那张 ⇒ `猎鹰` **是对的**。
4. ⇒ 断言该比**身份**（`FrontDef.Name`）而不是显示串。改后既保住「窗显示的就是那一行那张卡」，也不再绑死语档。

> 📌 **顺手发现（未改，超出白名单）**：`BattleDriver.Zh(string)` 把 `e.CardId` 送给**卡池**查名 ⇒ 起始卡 `Falcon` 在日志里被印成**卡池那张同名卡**的名字（`猎鹰战车`），与玩家在手里看到的 `猎鹰` 不一致。根因 = **两个不同的卡共用英文名 `Falcon`**，而 `Zh` 只看名字、不看来源。要么 `Zh` 改收 `CardDef`，要么起始卡改名。属 `BattleDriver.cs`，交调度台分流。

---

## 三 · 应该转绿的清单（下一次 `BattleScene.Run`）

| `battle.log` 行 | 断言 |
|---|---|
| `707402` | ★ 点行 ⇒ 大卡信息窗显示的就是那一行那张卡 |
| `707454` | ★ 换一行再点 ⇒ 选中的行跟着换 |
| `708073` | ★ ESC ⇒ 打开设置窗 |
| `711134` | 选完打法，选择器**仍开着** |
| `714337` | 定下打法 → 选择器**仍开着** |
| `968305` | ★ A462 悬停即选中 |
| `987354` | ★ 点第 1 行 ⇒ 被吃掉 + 记下选中行 |
| `987677` | ★ 闸门关着时仍被吃掉、但不记选中行 |
| `737408` / `737421` | ★ 音高在区间里 / 音量从区间取（`FIXA`） |
| `712052`…`713533`（22 条） | 大卡展示窗 / 效果清单整节（`FIXB`） |

⚠️ 新增的**前提**断言（如果环境不成立会红，如实报、不静默）：
- `7246` 面板重开；
- `7768` 设置窗关着**且不在选目标态**。

---

## 四 · 三道闸

| 闸 | 命令 | 结果 |
|---|---|---|
| 秒级类型检查 | `TMPDIR=/tmp/wf_fixc bash Unity/工具/typecheck.sh` | **运行时 0 / 编辑器 0** |
| 聚合行闸 | `python -I -X utf8 Unity/工具/check_aggregate_lines.py Editor/BattleScene.cs` | **PASS**（聚合行 1 · 调用 74 · 违规 0） |
| 账落点闸 | `python -I -X utf8 Unity/工具/check_ledger_points.py` | 全仓 A 5831 · 通过 5539 · 漂移 263 · 失败 29 —— **输出里 `Editor/BattleScene.cs` 命中 0 条**（都不在我这个文件上） |

**行数 / 行尾**：`wc -l` = **51322**（与改前一致；`log.Hide()` 仍在 `:7254`，与调度台快照逐行一致）·
`CRLF 51322 · LF 51322 · 裸 CR 0`（`io.open(p,'rb')` 读）。

---

## 五 · 仍红 / 不在本支

- `712052`…`713533` 那 22 条由 `FIXB` 的那一行收尾负责，本代理只落了那一行，**没跑宿主验证**（调度台统一跑）。
- `722650`「技能结算时面板铺白那层」· `704562`「换环境时旧实例先淡出」· `728067` / `728800`（E8 · 字号）·
  `737434`（节流）· `800493`（D6⑦ 材质队列）· `1002903` / `1004016` / `1005063` / `1043*` —— **不属本簇**，未动。
- ⚠️ **`987354/987677` 用的是文档化降级口**：如果将来 `BattleLogPanel` 能在「刚 `Show()` 的同一拍」里
  量出字形，那两条会走正常链接命中（同一断言照样成立）；反之它们**只证明闸门与选行记账**，
  **不证明链接命中** —— 链接命中由 `02b-2` + `B117_WBD` 的 `SecLogClickHitTest` 单独覆盖。
