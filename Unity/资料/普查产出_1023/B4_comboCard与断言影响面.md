# 执行代理 B4 —— `comboCard` 定性 + 评分改动的断言影响面（`2026-10-23`，**只读**）

> 本批**没有改任何文件**（除本报告）。未跑 Unity · 未动 git · 未改正本 · 未动数据产物/基线。

## ① 结论

### 一、`comboCard` / `comboCardFirst` → **「AI 不读（本 build）」＝口径差、无需补**

三条证据（逐条可复核）：

1. **字段确实在评分结构体里**：`ScoringCriteria.cs`（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/`）
   `:56 public RawCardScript comboCard;` · `:58 public bool comboCardFirst;` —— 577 条评分条目**全都有**
   （`cards_raw.json` 的 `fullCardTarget/nonTargetScoringCriteria[]` 键集现读）。
2. **唯一的读者是 `AI.ShouldPlayComboCard`**：`d:/2/tools/decomp_full/AI__ShouldPlayComboCard.c:20`
   读 `*(param_3 + 0x60)`（= `comboCard` 的 `RawCardScript`）、`:33` 读 `*(param_3 + 0x68)`（= `comboCardFirst`）。
   它判的是「组合卡**在不在手牌** + 两者费用之和 ≤ 未用能量」⇒ 这是**可打性**判据，不是分值。
3. 🔴 **这个方法在全量反编译里【一个调用点都没有】**：`grep -rl ShouldPlayComboCard decomp_full/`
   **只命中它自己那一个文件** ⇒ 本 build 里它不可达（死代码 / 被裁掉的调用点）。
   ⇒ **评分取值链上没有 `comboCard` 这一跳**：`AI.ScoreFrom*` 那一族没有任何一处读它。

**量化**：如果将来发现它其实可达，影响面 = **577 条评分条目**（每条都带），涉及池内卡按条目所属卡算
（`nonTarget` 387 张 + `cardTarget` 367 张，去重后是池内绝大多数有评分的卡）。
**要补的方案**（⛔ 本批不改，仅方案）：`AiScoringData` 加
`public string comboCardId;`（生成器把 `RawCardScript` PPtr 解析成 `uniqueId`，与 `filterSpecificCards`
同一个 `by_path_id` 口径）+ `public bool comboCardFirst;`，`AiScoringEntryDto` 两列都要加；
生成器侧在 `norm_entry` 前把 `comboCard` 解析成 id。**先别做** —— 本 build 判据是「不读」。

### 二、评分改动的断言影响面（静态）—— **候选 3 个，风险都低**

| 候选用例 | 位置 | 为什么是候选 | 我判的风险 |
|---|---|---|---|
| `TestFullGameWithRealCards` | `Editor/RuleEngineTest.cs:27919` | 用**真卡池**跑完整一局，AI 会出手 ⇒ 若它用到带 `activeAbility` 评分的卡（117 张），分值变 ⇒ 选招可能变 | **低**：断言只有「打完出结果 / 赢家合法 / 督军在场」，不钉具体招 |
| `TestFactionBattle` | `:6873` | 同上；且它断言 `tacticsPlayed > 0` 与 `tacticsPlayed >= Games` | **低**：阈值很松（平均每局 ≥1 张战术），但**理论上**这条最可能因选招变化而飘 |
| `TestAiOriginal` ⑪ | `:27342-27380` | 唯一一条**依赖真卡评分**的 AI 断言（`AM42 Pinning Fire` 的两个目标要分高/分低） | **极低**：它读的是 `cardTargetScoringCriteria`，**这次修复没碰那一列** |

**明确不受影响**（逐条核过）：
- `TestAiUsesAbility`（`:27108`）—— 四个夹具都是**合成卡**（`Reef Guard` / `Brute` / `Big`），
  `AiScoringData.Get(id)` 查不到 ⇒ `0f`（`AiScoring.cs:61-62`），与数据表无关。
- `RunScriptedGame`（`:27554`，被 `TestDeterminism` / `TestSignalDeterminism` 用）—— 起始卡是
  **60 个夹具名**（`Abyss Titan` / `Ballista` / …），只有 1 个名字能对上真池、且**不在** 117 张 AA 集里。
- `TestAiKillLine`（`:27424`）—— 只走攻击打分，不读 `activeAbility`。

**没有任何断言直接读 `ai_scoring.json` 的数值**：grep `AiScoringData|ai_scoring|ScoreFromPlayingNow|ScoreActiveAbility`
在 `Editor/*.cs` 只有 **3 处命中、全在注释里**（`:27342/:27345/:27346`）。

## ② 证据

- `ScoringCriteria.cs:56/58`（字段）· `AI__ShouldPlayComboCard.c:20/33`（读点）· 全库 grep 调用点 = 0。
- `SimpleAI.cs:610`（`AiScoringData.Get(u.Card)`）· `:618`（`ScoreActiveAbility(…, dto.activeAbility)`）·
  `Data/AiScoringData.cs:3/17/139/176/197`。
- `AiScoring.cs:61-62`（查不到 ⇒ `0f`）—— 合成卡不受影响的那一跳。
- 测试侧行号见上表；`Editor/*.cs` 里对评分表的引用**只有注释 3 处**。

## ③ 没查清

1. **`AI.ShouldPlayComboCard` 为什么没有调用点**：可能①本 build 裁掉了调用（AI 简化）、②Ghidra 把调用点
   内联/没解出来、③它是通过委托调用。要坐实得在 `AI__GetActionScore.c` / `AI__ScoreFromPlayingCard.c`
   里逐行找 `+0x60`/`+0x68` 的读点（本次只按「grep 函数名」定位，**没逐行读那两个方法**）。
   ⇒ 结论写的是「**本 build 判据 = 不读**」，不是「原版设计上永远不读」。
2. `TestFactionBattle` 的 `tacticsPlayed` 统计口径（是否只数战术卡、含不含 AI 出的）我没逐行读到 ——
   它只是「候选」，红不红由你在同步点实跑定。

## ④ 改动清单

**无**（本批只读）。报告文件除外。

## ⑤ 顺手发现

1. **`AI.ShouldPlayComboCard` 是「有实现、无调用」的典型**：这类方法在反编译里看着像功能，实际不可达
   ⇒ 以后判「原版有没有某能力」不能只看**方法存在**，要 grep **调用点**（这次的结论就是靠这一条翻的）。
2. `Editor/*.cs` 里**没有任何断言直接钉评分表数值** —— 意味着 `ai_scoring.json` 的回归**测试网为零**，
   上一批那个 118 条静默丢失就是因此活了很久。建议补一条「产物 ↔ 原始 SO」的对拍断言（纯读文件，不跑 AI）。
