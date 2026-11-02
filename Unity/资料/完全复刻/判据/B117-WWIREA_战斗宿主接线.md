# B117-WWIREA —— 战斗宿主断言接线报告（2026-11-02）

> 执行代理 `B117-WWIREA`。独占宿主：`Assets/CardPresentation/Editor/BattleScene.cs`（51,301 行，CRLF）· `Assets/RuleEngine/Editor/RuleEngineTest.cs`（39,763 行）。
> 只做**接线**，⛔ 未改任何断言类本身、未改 `BattleDriver.cs` / 其它 `Editor/*Scene.cs`、未跑 Unity 批处理、未动 git。

---

## 一、接了哪几个、接在哪一行

### 1. `Editor/BattleScene.cs`

| 位置（`文件:行号`） | 接了什么 | 为什么接在这儿 |
|---|---|---|
| `BattleScene.cs:651` | `B117_G10_21_Assert.Run()`（计数在 `:655` 补） | 🔴 它要求「场景里此刻**没有别的 `ErrorMessageBanner`**」（单例）。`driver.Begin()`（`:652`）→ `BuildHud` → `BuildErrorBanner` 会建出战斗横幅 ⇒ **必须排在 `Begin` 之前**。挂在 `if (driver.skillPanel != null) driver.skillPanel.Build();` 同一物理行；`pass/fail` 那时还没声明 ⇒ 计数放到 `int pass = 0, fail = 0;` 那一行，两行都是**行内追加**。 |
| `BattleScene.cs:22146` | `B117G1022_Assert.Run(chat)` + `B117G1022_Assert.RunBigChatStub()` | 它要一台 `UnitChatPanel`。挂在语音条那一节 `chat.HideAll(); B117F2_Assert.Run(chat);` **之后**（文件头指定），`Run(chat)` 会在第 2 颗 `RadioChat` 上真说两句 ⇒ 排在本节其它聊天断言之后。 |
| `BattleScene.cs:45280` | `B117_G10_16_Assert.Run(driver)` · `B117_G10_09_Assert.Run()` · `B117_G10_15_Assert.Run()` · `B117_WBD_Assert.Run()` · `B117_G10_04_Assert.RunWithDriver(driver)` | 五件**都需要活着的 driver 现场**，接在 `driver.DetachStaticHooks();` **之前**（本行随后就 `DestroyImmediate(driver)`）：G10-16 录像栏四颗钮的 cue 要活 HUD；G10-09 要 `FindObjectOfType<SkillPanel>()`；G10-15 要 driver 建出的 `BattleLogPanel`；WBD 要 `LayoutSpace.Cam`/`HudCanvas` 才能 `BattleLogPanel.Create(null)`；G10-04 按它文件头要求排在**所有依赖棋盘/HUD 现状的断言之后**、卸 driver 之前（它自己 `finally` 原样还原 + 重刷 HUD）。 |
| `BattleScene.cs:46910` | **24 件零前提**（方法尾聚合行、合计之前） | 见下节。 |

`BattleScene.cs:46910` 的 24 件（全部**零前提**：自带临时根/临时相机，收尾自己 `DestroyImmediate`；不读存档、不写 PlayerPrefs）：
`B117G1002a_Assert` · `B117_G10_03_Assert` · `B117_G10_05_Assert` · `B117_G10_06_Assert` · `B117_G10_07_Assert` · `B117_G10_08_Assert` ·
`B117_G10_10a_Assert` · `B117G1010bAssert` · `B117G1011Assert` · `B117_G10_12_Assert` · `B117_G10_13_Assert` · `B117_G10_14_Assert` ·
`B117_G10_17_Assert` · `B117_G10_18_Assert` · `B117G1019Assert` · `B117_G10_20_Assert` · `B117_G10_23_Assert` · `B117G1024Assert` ·
`B117G1025Assert` · `B117G1026_Assert` · `B117WCurveAssert` · `B117_WGB_Assert` · `B117_WREPLAY2_Assert` · `B117_WSOUNDCOOL_Assert`

### 2. `RuleEngine/Editor/RuleEngineTest.cs`

| 位置 | 接了什么 |
|---|---|
| `RuleEngineTest.cs:1294` | `Guard("RuleEngineTest_B117WGB", RuleEngineTest_B117WGB.Run); _pass += …Pass; _fail += …Fail;` → `Guard("RuleEngineTest_B117WMULL", …);` → `Guard("B117DROPPOD_Assert", B117DROPPOD_Assert.Run); _pass += …Pass; _fail += …Fail;` |

追加点 = 该行**第一个 `//`**（`// [B98_Q2 2026-10-29 代接线]`）之前，紧跟 `_fail += B104_D_Assert.Fail;`。
`RuleEngineTest_B117DROPPOD.cs` 的类名是 `B117DROPPOD_Assert`（文件名 ≠ 类名），已按调度台 2026-11-02 追加要求接上。

**合计接线 34 件**（BattleScene 31 + RuleEngineTest 3）。

---

## 二、未接线清单

**无。** 34 / 34 全部接上，没有「找不到合适宿主」的条目。

> 补充说明：`B117_G10_04_Assert` **不在**调度台原简报「二、要接的断言类」列表里（列表从 `02a / 03 / 05` 跳过了 `04`），但简报 §三-2、§三-4 都点名了它的 `RunWithDriver(BattleDriver)` 与排序要求，且它只能挂在 `BattleScene.cs` ⇒ 本次一并接上。若调度台另有安排，删掉 `:45280` 那一小段即可（`B117_G10_04_Assert.RunWithDriver(driver); pass += …; fail += …;`）。

---

## 三、三项读数

| 检查 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_wwirea bash 工具/typecheck.sh` | **运行时错误数 0 · 编辑器错误数 0**（全部改动完成后跑的；收尾前又复跑一次，仍 0/0） |
| `python -I -X utf8 工具/check_aggregate_lines.py <两个宿主>` | **违规 0**（`BattleScene.cs:46910`：**73 个调用全在 `//` 之前**，首个 `//` 在字符 8115；`RuleEngineTest.cs` 未达聚合行阈值，不判） |
| 「类名出现在**别的文件**的**非注释片段**」自检 | **目标 34 · 已接线 34 · 未接线 0**（脚本 `d:/4/_tmp_view/wirecheck_b117_wwirea.py`，逐字符跳过 `//` / `/* */` / 字符串字面量，并**排除定义文件本身**） |

**行数 / 行尾守恒**：
- `BattleScene.cs`：51,301 → **51,301** 行；`CRLF 51,301 / LF 51,301`（保持 CRLF）。
- `RuleEngineTest.cs`：39,763 → **39,763** 行；`CRLF 39,763 / LF 39,763`。
- ⚠️ **就地订正简报**：简报写「39,763 行的那个是 LF」——**实测两份都是 CRLF**（`b.count(b'\r\n') == b.count(b'\n')`），已按 CRLF 原样保留。
- `git diff -U0` 里带 `B117-WWIREA` 标记的行 = **5 行**（我的 5 处改动）；`git diff --numstat` 报 `17/17` / `9/9`，其余是本会话**别人未提交**的改动，**不是本次引入**。

---

## 四、我判断可能红的那几条（附原因）

1. **`B117_G10_24_Assert` 的 E 组**（`TapHitMaterialDefinition` 打不到）—— 它文件头自陈：`ArenaBuilder` 的整树刷层把宿主刷回 layer 8 ⇒ **E1 今天就会红**，这是**已知真缺陷**，不是接线错。
2. **`B117_WSOUNDCOOL_Assert` 的 A0**（`Time.time > 0`）—— 批处理里 `Time.time` 恰为 0 时会红；调度台已确认**这是有意的**（原版 `&& (last != 0)` 边界照抄），**别改断言**。
3. **`B117_G10_04_Assert` 的 B / C 组** —— 它文件头写明：**`BattleDriver.cs` 的补丁 A/B 没落地时 B 组与 C 组会红，那正是它们的判别性**。本次只接线、未碰 `BattleDriver.cs`。
4. **`B117_G10_09_Assert` 的 A 组前提**（`FindObjectOfType<SkillPanel>()`）—— `SkillPanel` 收起时**只压 alpha、不 `SetActive(false)`**（`SkillPanel.cs:1055-1058` 实读），所以应当找得到；若某个宿主把它整件关掉就会红在「前提」那一条（⛔ 不是静默跳过）。
5. **`B117_G10_15_Assert` 的 B0 前提**（活着的 `BattleLogPanel`）—— 依赖 `driver` 已经 `Begin` 过（`BattleDriver.cs:29536` `_logPanel = BattleLogPanel.Create(root)`）。本次排在 `CheckManaBuffHud`（会 `Begin` 重开一局）**之后**，面板仍在；若 driver 的 HUD 那条路今天没建日志面板，B0 会红。
6. **`B117_G10_21_Assert`** —— 挂在 `driver.Begin()` **之前**，前提是「`BuildScene` 不建 `ErrorMessageBanner`」（已 grep 确认 `Editor/BattleScene.cs` 全文 **0 处** `ErrorMessageBanner`）。若哪天有人在 `BuildScene` 里加横幅，它会红在单例前提。
7. **`B117_G10_16_Assert.Run(driver)` 的 F 组** —— 要 HUD 里那四颗录像钮带 cue；排在 `CheckManaBuffHud` 之后（那一局是新 `Begin` 的），若新局没重建 `ReplayBar`，F 组会红。

> 其余 20+ 件都是「自带临时根 + 收尾销毁」的零前提断言，理论上不会因宿主状态红。

---

## 五、本波次顺手发现（供调度台分流）

- `B117_G10_21_Assert` 是**唯一**一个「必须先于 `driver.Begin()`」的断言类，因此无法挂在两条标准聚合行上，只能挂在 `:651` 并跨行累加计数 —— **这是本批唯一一处「同一件事分两行」的接线**，下一个接线的代理别把它当成重复接线删掉。
- `BattleScene.cs` 里 `pass` / `fail` 是 `RunCore()` 的**局部变量**（`:655` 声明，`RunCore` 范围 `:535`–`:46920`），不是字段；`:45280` 与 `:46910` 两处都能直接 `+=`（`:22146` 亦然）。
- 🔴 **`check_aggregate_lines.py` 只认 `\w+_Assert\.Run()`** —— 本批有 **6 个类名不以 `_Assert` 结尾**：`B117G1010bAssert` · `B117G1011Assert` · `B117G1019Assert` · `B117G1024Assert` · `B117G1025Assert` · `B117WCurveAssert`。它们**不计入那道闸的「73 个调用」**（55 原有 + 18 命中 = 73；本批实接 24 + 5 + 1 = 30 处），所以闸「PASS」**证明不了它们没被注释吞掉**。已用该工具自己的 `_scan_first_line_comment()` 单独逐位核过：6 个的字符位 6084 / 6170 / 6713 / 6980 / 7063 / 7232，全部 < 该行第一个真 `//` 的 8115 ⇒ 未被吞。同理 `:651 / :655 / :22146 / :45280` 四处（都不在聚合行阈值内）也已逐位核过，全部在真 `//` 之前。
- `RuleEngineTest.cs` 的聚合行**不满足** `check_aggregate_lines.py` 的阈值（`.Run()` 形态 < 20 个，它大量用 `Guard("x", X.Run)` 方法组），⇒ **这道机器闸对 `RuleEngineTest.cs` 是盲的**。往 `:1294` 追加时只能靠人读「第一个 `//` 在哪」，建议以后给它单独写一条检查。
