# W58-HEALCOMMENT —— 「负数 Hit = 治疗」旧注释订正

> 日期：2026-10-20 · 执行代理：W58-HEALCOMMENT
> 任务：只订正过期文案（注释 / 断言说明文字），⛔ 不改行为 / 常量 / 断言期望值。
> 判据来源：`资料/普查产出_1020/W58_战斗线_治疗表现落地.md` §7「没查清」第 2 条
> ＋ 现行实现 `RuleCore.HealUnit`（唯一治疗口，发 `EvtKind.Heal`、`Amount` 为正数）
> ＋ `RuleCore.CleanupDeaths`（负数 `Hit` 的唯一发出点，仅作幸存者广播标记，表现层静默）。

## 结论

两处指定旧文案已订正，并顺带订正了 `RuleEngineTest_S14.cs` 里另外两处同族旧文案（仍是同一文件、只改说明文字）。
**代码逻辑、常量、断言期望值（`-6` / `1` / `2` / `0` 这些参数）一字未动**；
`git diff --numstat` = `UnitState.cs 8/2` · `RuleEngineTest_S14.cs 6/3`；行尾仍为 LF（与改前一致）。
`RuleEngineTest.cs` 里还有 3 处同族旧文案（不在本笔白名单、另一代理正在改），**未动**，列在下面交主对话。

## 逐处（旧 → 新）

### 1. `Unity/MyGame/Assets/RuleEngine/Core/UnitState.cs:360`（`UseSurvivor` 的 doc）

- **旧**：`:87 BroadcastUnitHealed` → 我们发一条**负数 `Hit`**（= 治疗，表现层据此走绿字，与 `EndTurn` 的再生段**同一个约定**）。
- **新**：保留「我们发一条负数 `Hit`」这一事实，后接订正块：
  「🔴 2026-10-20（`W58`）就地订正（铁律 5）：本行原来写「负数 `Hit` = 治疗，表现层据此走绿字，与 `EndTurn` 的再生段同一个约定」—— **已过期**。
  现行口径：**治疗 = `EvtKind.Heal`**（唯一口 `RuleCore.HealUnit`，`Amount` = 实际治疗量、正数；再生 / 猎杀标记 / `heal` op / `heal` 能力都走它）；
  **负数 `Hit` 仅作幸存者广播的标记**（发出点 = `RuleCore.CleanupDeaths`，表现层对它全静默 —— 原版 Survivor 不进 `CardScript.Heal`，不飘治疗字、没有治疗圈）。
  详见 `BattleEvent.Amount` 的 doc；⛔ 别把它改成 `EvtKind.Heal`。」
- 只改注释行，`UseSurvivor()` 方法体一字未动。

### 2. `Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest_S14.cs:138`（`Amount = -6` 那条断言的说明文字）

- **旧**：`（负数 = 治疗，表现层据此走绿字；回血量 = 3 − (2 − 5) = 6）`
- **新**：`（🔴 2026-10-20（`W58`）就地订正：原写「负数 = 治疗，表现层据此走绿字」—— 已过期。现行口径：治疗 = `EvtKind.Heal`（唯一口 `RuleCore.HealUnit`，`Amount` 为正数）；负数 `Hit` 仅作幸存者广播的**标记**，表现层对它**全静默**。回血量 = 3 − (2 − 5) = 6）`
- 断言本体 `Check(S14Sig(ctx, EvtKind.Hit, null, -6), 1, …)` **未动**。

### 3. `RuleEngineTest_S14.cs:144`（同族，一并订正）

- **旧**：`……这一格总共只有两条 Hit（一下 5 伤 + 一条治疗），没有多发的`
- **新**：`……这一格总共只有两条 Hit（一下 5 伤 + 一条幸存者广播的负数 Hit，没有多发的）`
- 断言本体（两条 `Hit` 相加 = 2）**未动**。

### 4. `RuleEngineTest_S14.cs:168`（同族，一并订正）

- **旧**：`……也没有那条治疗 Hit`
- **新**：`……也没有那条幸存者广播的负数 Hit（它只是标记，表现层对它静默）`
- 断言本体 `Check(S14Sig(ctx, EvtKind.Hit, null, -6), 0, …)` **未动**。

## `RuleEngineTest.cs` 残留（给主对话，本笔未改）

> ⛔ 按任务红线，`RuleEngine/Editor/RuleEngineTest.cs` 一个字都没动（另一代理正在改它）。
> 行号按**本笔落盘时**的当前文件内容（该文件正被并行编辑，行号会漂；按内容搜更稳）。

1. `RuleEngineTest.cs:30925` —— doc 注释：
   `（Hit 且 Amount < 0 —— 负数才是治疗，见 EvtKind.Hit 的口径）`
   ⇒ 建议改为「`Hit` 且 `Amount < 0` 仅幸存者广播标记（表现静默）；治疗看 `EvtKind.Heal`」。
2. `RuleEngineTest.cs:31255` —— `A1576` 头注的事件次序：
   `Trigger(sacrifice) → Trigger(survivor) → Hit(负数=治疗)`
   ⇒ 建议把末项改为 `Hit(负数，幸存者广播标记)`。
3. `RuleEngineTest.cs:31289` —— 断言说明文字：
   `（前提）发了一条治疗事件（Hit 且 Amount < 0）`
   ⇒ 建议改为「发了一条幸存者广播的负数 `Hit`（仅标记）」；⚠️ 该断言的**期望值**若要改成 `EvtKind.Heal`，属于行为口径变更，**不属本笔范围**，需主对话裁定。

## typecheck

```
TMPDIR=/tmp/wf_healc bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

- 未跑任何 `*.Run` · 未跑 Unity 批处理 · 未动 git · 未改正本。
- `git diff --numstat`：
  - `Unity/MyGame/Assets/RuleEngine/Core/UnitState.cs` → `8 2`
  - `Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest_S14.cs` → `6 3`
- 行尾：两文件均 **LF**（改前改后 `CRLF 0`；`UnitState.cs` LF 1302→1308、`RuleEngineTest_S14.cs` LF 2404→2407，增量即新增行数）。
