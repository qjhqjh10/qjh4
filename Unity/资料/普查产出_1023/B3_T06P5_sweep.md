# 执行代理 B3 —— `T06` P5 sweep（`2026-10-23`）

> 口径（主对话定死）：比的是 **生成物 `ai_scoring.json` ↔ 原版 SO 评分字段**；运行期 `Core/SimpleAI`
> 是另一条独立来源，本批只定位不改。⛔ 未跑 Unity · 未动 git · 未改正本 · 未动基线/指纹。
> ⚠️ 路径订正：产物实际在 **`MyGame/Assets/RuleEngine/Resources/ai_scoring.json`**（不是 `数据/游戏数据/`）。

## ① 结论

| 项 | 数 |
|---|---|
| **真偏差（修前）** | **118 条评分条目 / 117 张卡** —— 全在 `activeAbility` 的两列，**`damage`/`heal`/`buff` 三块被静默丢掉** |
| **真偏差（修后）** | **0** |
| 口径差 | **2 类**：① `filterSpecificCards` PPtr→卡 id（**1 条**，`SAU52`，解析正确）；② DTO 不带 `comboCard`/`comboCardFirst`（**577 条全带**，AI 是否读**未核**） |
| 原版为空 | **0**（池内 1,127 张里「原版无此卡」「原版为空·我们有」都是 0） |
| 覆盖率 | 池内 **1,127/1,127** 全在产物里；产 `count=1351`（含 224 张非池卡，与既有口径一致） |
| 幂等 | 连跑两次 md5 **`1bfe9797f26c2fc9ef63a8a135c94d31`** 相同；`--check` rc=0 |
| 产物变化 | 529,931 → **625,395 字节**；**117 张卡变化，全部只在 `activeAbility`**（结构级 diff 逐卡核过），卡序 / id 集合 / 顶层键**一个没动** |

**`SimpleAI` 读哪一份**（一条来源，不是两条）：`Data/AiScoringData.cs:3`（数据源声明）· `:197`（唯一查询口
`AiScoringData.Get(card.Id)`，懒加载整份）· `:139/:176`（两列 DTO）→
`Data/SimpleAI.cs:610`（`var dto = AiScoringData.Get(u.Card)`）· `:618`（`AiScoring.ScoreActiveAbility(…, dto.activeAbility)`）。
⇒ **`SimpleAI` 只读 `ai_scoring.json` 这一份**，没有第二张表。

## ② 证据

**真偏差本体**（`工具/gen_ai_scoring.py` 修前 `build()` 的 activeAbility 分支）
- 原版 `ActiveAbility` 的评分条目用的是 **SO 原名** `damageCriteria` / `healCriteria` / `buffEffect`
  （`d:/2/Warpforge_code/Scripts/Assembly-CSharp/ScoringCriteria.cs`；现读 `cards_raw.json` 的
  `full_raw.activeAbility.*ScoringCriteria[]` 的键集 = `[baseValueEnemy, baseValueFriend, buffEffect,
  codexEffect, comboCard, comboCardFirst, damageCriteria, handEffect, healCriteria, multiplierCriteria,
  multiplyByEnemy, multiplyByFriend, scoringOption, targetCriteria]`）。
- 而 `norm_entry` 只认 **DTO 名**（`ENTRY_KEYS` = `… 'damage','heal','buff' …`，因为 `cards_truth.json`
  那一跳已改名，`工具/gen_cards_truth.py:349-357`）。
- ⇒ 从 raw 直喂的那一路（**只有 `activeAbility`**）**静默丢三块**。实测（`cards_raw.json` 现读）：
  **AA 评分条目 118 条 / 117 张卡**，其中 `buffEffect` 非默认 **118**、`damageCriteria` **71**、`healCriteria` **5**。
  修前产物里这 118 条**一格 buff 都没有**。
- **修法**：新增 `SO_TO_DTO` + `rename_so_entry()`（只补缺、不就地改源），activeAbility 分支改成
  `norm_entry(rename_so_entry(e))`。**为什么不是自证**：期望值取自**原版 SO 的键名/取值**（`cards_raw.json`），
  不是我们自己的常量；修完由「独立写的一份 sweep 脚本」按同一套归一重算，差异从 118 → 0。

**口径差 ①**：`SAU52` 的 `nonTargetScoringCriteria[0].targetCriteria.filterSpecificCards` ——
raw 是 PPtr `{"m_FileID":0,"m_PathID":3514668718221226008}`，我们是 `["SAU1"]`；
`SAU1 = Canoptek Scarab`，而 `SAU52` 卡面 = `Destroy all friendly Canoptek Scarabs…` ⇒ **解析正确**，
差在表示（PPtr vs 卡 id），不是缺陷。

**口径差 ②**：`comboCard` / `comboCardFirst`（`ScoringCriteria.cs:56/58`）在 **577 条**评分条目里全都有
（`cards_raw.json` 现读），我们的 DTO 不带。反编译侧有 `AI__ShouldPlayComboCard.c` / `CardScript__CheckForCombo.c`
⇒ **AI 确实有组合卡逻辑**；但「评分条目里的 `comboCard` 有没有被 `AI.Score*` 读到」**本批没核**（见 §三）。

## ③ 没查清

1. **`comboCard` / `comboCardFirst` 是否被 AI 评分读**：要读 `AI__ShouldPlayComboCard.c` 与 `AI.Score*` 的取值链。
   若读 ⇒ 又是一条真偏差（且要改 `RuleEngine/Data/AiScoringData.cs`，本批**不许动运行时**）。
2. **这次修复会改变 AI 的主动能力打分**（`SimpleAI.cs:618` 读 `dto.activeAbility`）⇒ `RuleEngineTest` 的
   AI 相关断言 / `ruleprobe play` 的结算指纹**可能会动**。本批**没有**重钉任何基线（照红线），
   主对话跑自检时若见 AI 行为变化，**先看这里**。
3. 只比了「评分条目」这一层；`cardType/spellType/keyword/targeted/hasActiveAbility/manaCost` 这六列
   没逐张核（它们在 `cards_truth.json` 里另有一跳）。

## ④ 改动清单

| 文件 | 改动 |
|---|---|
| `d:/4/Unity/工具/gen_ai_scoring.py` | 新增 `SO_TO_DTO` + `rename_so_entry()`；`activeAbility` 分支改走 `norm_entry(rename_so_entry(e))`（+ 25 行注释/2 行代码）。行尾仍是 LF。 |
| `d:/4/Unity/MyGame/Assets/RuleEngine/Resources/ai_scoring.json` | **重新生成**：117 张卡的 `activeAbility` 两列补回 `damage`/`heal`/`buff`；其余**逐字节不变**（结构级 diff 已核）。`git diff --numstat` = `1/1`（单行 JSON 的正常形态）。 |

⛔ 没碰：`RuleEngine/**`（运行时）· `CardPresentation/**` · `WarpforgeVFX/**` · 正本 · 基线/指纹 · git · Unity。

## ⑤ 顺手发现（只报不改）

1. **同一个「SO 名 vs DTO 名」的坑还有两处入口**：`gen_cards_truth.py` 走的是 `compact_scoring`（自带改名），
   本生成器的 truth 那一路没问题；**只有 raw 那一路**漏了。以后任何「从 `cards_raw.json` 直取评分条目」的新代码
   都必须先 `rename_so_entry`（建议照抄这个函数，别另写一份）。
2. **`--check` 抓不到这一类缺陷**：它比的是「重算 vs 盘上」，两边都错就恒绿。⇒ 需要一条**独立**的
   「产物 ↔ 原始 SO」对拍（本批的 `/tmp` 脚本就是雏形，建议收进 `工具/`）。
3. `ai_scoring.json` 的 `count=1351`（全真值），而池只有 1127 —— AI 侧按 `card.Id` 查，不会误用，但那 224 张
   是纯体积（~18%）。要瘦身可以只落池内，属主对话取舍。
