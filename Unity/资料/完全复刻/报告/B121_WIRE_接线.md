# B121_WIRE —— 收口接线 + 硬编码计数修正（2026-11-03）

执行写手：`E121-WIRE`。范围：把本会话新建的 23 个断言入口接进宿主聚合行 + 修正因「池 1,129 → 1,142 / 版本 47 → 49」而必红的字面量。
全部改动**零行数变化**（字节级就地替换 / 同一物理行追加），行尾未翻（逐文件核对 `\n` 与 `\r\n` 计数前后相同）。

## §1 接线表

| 断言类 | 宿主 · 行 | 挂法 | 累加变量 |
|---|---|---|---|
| `B121_WA_Assert` | `CardPresentation/Editor/BattleScene.cs:47035` | `Run()` | `pass += …Pass; fail += …Fail` |
| `B121_WB_Assert` | 同上 `:47035` | `Run()` | 同上 |
| `B121_WE_Assert` | 同上 `:47035` | `Run()` | 同上 |
| `B121_Q4B_Assert` | 同上 `:47035` | `Run()` | 同上 |
| `B121_E04B_Assert` | 同上 `:47035` | `Run()` | 同上 |
| `B121_E03F_VFX_Assert` | 同上 `:47035` | `Run()` | 同上 |
| `B121D2_Assert`（类名无下划线，以文件为准） | 同上 `:47035` | `Run()` | 同上 |
| `B121AUD_Assert`（同上） | 同上 `:47035` | `Run()` | 同上 |
| `RuleEngineTest_B121WB` | `BattleScene.cs:45405`（**driver 活着**那条聚合行） | `Run(driver)` | 同上 |
| `B121_C1_Assert` | `CollectionScene.cs:13286` | `Run()` | `_sink.Pass / _sink.Fail` |
| `B121_C2_Assert` | 同上 `:13286` | `Run()` | 同上 |
| `B121_C3_Assert` | 同上 `:13286` | `Run()` | 同上 |
| `B121_COLL16_Assert` | 同上 `:13286` | `Run()` | 同上 |
| `B121_COLLFIX_Assert` | 同上 `:13286` | `Run()` | 同上 |
| `B121_WC_Assert` | `DeckScene.cs:1400` | `Run()` | `_sink.Pass / _sink.Fail` |
| `B121_D1_Assert` | `RuleEngineTest.cs:1400` | `Guard("B121D1_Assert", Run)` | `_pass / _fail` |
| `B121_D1B_Assert` | 同上 `:1400` | `Guard("B121D1B_Assert", Run)` | 同上 |
| `B121_D3_Assert` | 同上 `:1400` | `Run()`（文件头形态，无 Guard） | 同上 |
| `B121_D4_Assert` | 同上 `:1400` | **`Step(B121D4Case);`** —— 它不是独立类，是 `partial class RuleEngineTest` 的用例方法（文件头指定） | 走 `Step` 自带 `_pass/_fail` |
| `B121_D5_Assert` | 同上 `:1400` | `Run()` | `_pass / _fail` |
| `B121_D5R_Assert` | 同上 `:1400` | `Run()` | `_pass / _fail` |
| `B121_E03F_DATA_Assert` | 同上 `:1400` | `Run()`（`_pass/_fail`，宿主惯例，非文件头的 `pass/fail`） | `_pass / _fail` |
| `RuleEngineTest_B121WC` | 同上 `:1400` | `Run()` | `_pass / _fail` |

- 每个类**只接一处**（逐名 grep 确认 `*.Run` 命中数 = 1；`B121_D4` 走 `Step(B121D4Case)`）。
- `BattleScene.cs:47035` = **方法尾聚合行**（`driver` 已销毁、全部零前提件都在这里）；插入点在 `Debug.Log(P + $"=== 合计…")` **之前**，所以新增条数**会计入合计**。
- `BattleScene.cs:45405` = 唯一**driver 活着**的聚合行；`RuleEngineTest_B121WB` 按文件头要 `BattleDriver`（`driver == null` 会让 B/C 段报红），故不接在 `RuleEngineTest.cs`（那里没有活 driver），接在 `driver.DetachStaticHooks()` **之前**、所有 B117 G10 断言之后（它自己会 `driver.Begin` 重开一局，后面没有依赖旧棋盘的断言）。
- `CollectionScene.cs:13286` 插入点在 `int total = _sink.Pass + _sink.Fail;` 之前（计入合计）。
- `DeckScene.cs:1400` / `RuleEngineTest.cs:1400` 插入点均在该行**第一个 `//` 之前**（`DeckScene` 在 `// 🆕 2026-10-31（调度台接线 · W7）…` 前，`RuleEngineTest` 在 `// 🆕 2026-11-01（写手 W-19 接线…）` 前），⛔ 没有落进行末注释。
- 「未验」计数（`C1` / `COLL16` / `COLLFIX` 的 `Unverified`）**不并入宿主** —— 与既有惯例一致（`B117_WCOLL_Assert` 也只印不并）。

## §2 计数修正（逐个现读）

| 位置 | 旧 → 新 | 依据 |
|---|---|---|
| `RuleEngineTest.cs:22794` | `pool.Count == 1129` → `== 1142` | `RuleEngine/Resources/cards_engine.json` 现读 `version 49 / count 1142 / len 1142` |
| `RuleEngineTest.cs:22793`（注释） | 追加 v48/v49 来历（零行数变化） | `工具/gen_cards_engine.py:7671` 的 `"version": 49` 注释 |
| `RuleEngineTest_EngineD4.cs:102` / `:103` | `Check(dto.version, 47` → `49`；文案 `= **47**` → `**49**` | 同上（v48 = 三处数据列 · v49 = 加卡不加列） |
| `RuleEngineTest_EngineD4.cs:365` | `Check(cards, 1129 …)` → `1142` | 同 §一 |
| `RuleEngineTest_EngineD4.cs:20`（注释） | 文首「现读」清单 `1129` → `1142` | 同上 |
| `RuleEngineTest_G12.cs:57` | `Check(pool.Count, 1129 …)` → `1142` | 同上 |
| `RuleEngineTest_G12.cs:105` / `:106` | `cardsEmpty` `997` → **`1010`**（`1142 − 132`） | 从 `cards_engine.json` 现算：带 active/alt 行的卡 **132** 张（未变）⇒ 空档 = 1142 − 132 = 1010；`:104` 注释同步 |
| `RuleEngineTest_G12.cs:93`（文案） | `1128/1128` → `1142/1142` | 断的是 `notVersioned == 0`（动态），只修文案 |
| `BattleScene.cs:25780` | `Check(n == 1129, $"…卡池张数 = 1129…` → `1142` | 同 §一（文案同步补 v49 来历） |
| `BattleScene.cs:25781` / `:25782` | `0xE59A576E` → **`0x0BFC707A`** | 池 +13 张 ⇒ `id\x01desc\x01descZh\x01` 的 FNV-1a 必变。**算法自证**：拿 `git show HEAD:` 的卡表（v47 / 1129 张）按同一脚本复算 = **`0xE59A576E`**，与旧期望**逐字相同** ⇒ 算法可信；现池复算 = `0x0BFC707A` |

## §3 验证

- `TMPDIR=/tmp/wf_wire bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（改前跑一次、最后一次文案改动后再跑一次，两次都是 0/0）。
- `python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py` ⇒ **A 5899 条 · 通过 5899 · 漂移 0 · 失败 0**（未用 `--fix`）。
- 行数守恒：`BattleScene.cs 51427` · `CollectionScene.cs 13798` · `DeckScene.cs 10486` · `RuleEngineTest.cs 39763` · `RuleEngineTest_EngineD4.cs 558` · `RuleEngineTest_G12.cs 509`（改前改后相同）；`\n` 与 `\r\n` 计数逐文件相同（B/C/EngineD4/G12 原本是纯 LF，另三个是纯 CRLF）。
- 插入点逐条打印回读确认（用 `python -I` 二进制读，按 `\r\n` 切行）。

## §4 没接的 / 偏离任务单的

1. **`RuleEngineTest_B121WB` 从 `RuleEngineTest.cs` 挪到 `BattleScene.cs`** —— 唯一偏离。理由：它的入口是 `Run(BattleDriver driver)`，`driver == null` 时 B/C 两段**报红**（文件头写明「⛔ 静默跳过 = 假绿」），而 `RuleEngineTest.cs` 里没有活 driver。落点见 §1。
2. `B121_D4_Assert` 没有 `Run()`：文件是 `public static partial class RuleEngineTest` + `B121D4Case()`，按文件头接 `Step(B121D4Case);`（`gen_diag_runner` 2026-10-30 起允许一行多条 `Step`；`RunCoverageCompiledCases()` 只认 `Test*` / `Report*` 前缀 ⇒ `B121D4Case` 不进用例表，不会引出覆盖闸红）。
3. 未建账补丁 `账/_parts/B121_WIRE.tsv`：本波只做「接线 + 过期字面量」，没有产生新的「原版 vs 我们」差异行。

## §5 顺手发现

1. **`gen_cards_engine.py` v49 的注释点明**：本版**会改行为**（13 张新卡可被打出、会换督军），同步点要重跑 `ruleprobe play check` 并**重钉 `PinnedPlayMd5`**（+13/+13 行）。⛔ 不在我的白名单，留给调度台在同步点做。
2. **`BattleScene.cs:25782` 那条数据 hash 是「卡表有意更新就红」的哨兵**：本次已按算法重算；日后只要 `desc`/`descZh` 再改（含 `cardface_fixes.json` 的手工列），必须重算，⛔ 别直接删断。
3. **未复核的其他字面量**（因新卡不产生这些行，判断应仍成立，但没有逐格重算）：
   `RuleEngineTest_EngineD4.cs` 的 `1621 / 1010 / 287 / 193 / 132 / 133`；`RuleEngineTest.cs:36649` 的 `withRows = 328` 与 `:36654` 的 `rowTotal = 442`。
   佐证：13 张 `TRANSFORM_*` 的 `abilityTargets` / `activeAbilityTargets` 全为 `[]`、也没有 active/alt 行（现算：带 active 行的卡仍 132 张、合计 logic 仍 133）。
4. `B121_WE_Assert` 是**源级**断言（`ReadSrc("CardPresentation/Battle/BattleDriver.cs")` ⇒ `Application.dataPath` 下现读文本），接在 BattleScene 里跑不依赖帧循环；它读的路径若将来改名会红 —— 那是设计意图。
5. `B121_WA_Assert` 会临时关 `WFSoundPlayer.Enabled`、钉 `ClockOverride`，并自建 `WFSoundPlayer`/`RemnantSfx`（`HideFlags.DontSave`，`finally` 里销毁）；接在**方法尾、合计之前**，其后只剩 `Debug.Log`，不会污染别的断言。
