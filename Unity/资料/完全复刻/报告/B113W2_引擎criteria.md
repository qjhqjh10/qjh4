## W2（引擎 criteria 组 · 第一百一十三会话 · 第 1 波）

> 判据来源：`判据/B113W_对账_A_引擎规则1.md`（`B-210`/`B-208`/`B-181` 三行）＋
> `判据/B112W_B193_criteria静默丢弃裁定.md` §2·2（可粘贴段）。
> 全部行号 = **2026-11-01 改前现读**；改后行号见本文 §改动清单的 hunk 位置。
> ⛔ 未跑 Unity；`typecheck` 只用了自己的 `TMPDIR=/tmp/wf_w2`。

| ID | 做了什么 | 证据（文件:行号） | 验证 | 剩下 |
|---|---|---|---|---|
| **B-210** | `DoEachUnitDeal` 的**出伤单位池**（扫 `mine.Board`）补上本 op 的 36 格 criteria 闸：`op.TargetCriteriaSet != null` 时逐单位过 `TargetCriteriaEval.Matches(...)`，`false` ⇒ `filtered++; continue;`，`null` ⇒ **保留 + `StructuredConditionNotice` 出声**（与 `ApplyTargetCriteriaFilter` 同一契约，⛔ 不当成 false）。<br>⛔ **没动** `:3597` 那条敌人池 `ResolveTargets`（裁定 §2·2：传 criteria 会把敌人筛空） | 改前 `RuleEngine/Core/EffectResolver.cs:3607-3615`（循环只有 `IsAlive` / `Subject` / `OtherThanSelf`）；改后 = hunk `@@ -3616,0 +3647,25 @@`（+25 行，插在 `if (op.OtherThanSelf …)` 之后、`int n = fixedDmg …` 之前）<br>判据：`TargetCriteriaEval.Matches`（`Core/TargetCriteriaEval.cs:93`）· `op.TargetCriteriaSet`（`EffectResolver.cs:2368` 现成）· 残骸留在棋盘格 = `RuleCore.CleanupDeaths` 翻面支 | `typecheck` **0/0**；`play check` 指纹**与改前逐字节相同**（`cdf608b5da9c079e96d59e70fe549c19`，1129 张 + 事件电池 1073 行的输出 diff = **空**）⇒ 全池零结算变化（探针夹具里没有残骸）<br>断言：`B113W2_Assert.cs` 的 **A1/A2/A3**（见下） | 原版**督军**是否算出伤单位（`UM82` 的 criteria 带 `includeHero=1`）仍未核 —— 沿用 `B193` §2·2 的「未查清」，⛔ 没猜（详见 §尚需确认 ①） |
| **B-208** | `AddSide` 加残骸闸：新形参 `bool includeRemnants`，命中 `if (!includeRemnants && u.IsRemnant) continue;`；判法唯一收在**文件尾新加的** `IncludeRemnantsOf(EffectOp)`（`set != null && set.IncludeRemnants > 0`，与 `TargetCriteriaEval.cs:289` 的 `FlagOn` 同口径）。<br>`ResolveTargets` 的三个 `AddSide` 调用点改传 `IncludeRemnantsOf(op)`（`op == null` ⇒ `false` ⇒ **按原版默认剔残骸** = 修复点） | 改前 `EffectResolver.cs:3030-3042`；hunk：`@@ -3031 +3051 @@`（签名 + 10 行注释闸）· `@@ -3036,0 +3057,10 @@`（闸 + 长注释）· `@@ -1683 +1683,2 @@`（无施放者降级路）· `@@ -1744,2 +1745,5 @@`（己/敌两路）· `@@ -21096,0 +21152,23 @@`（文件尾 `IncludeRemnantsOf`）<br>pick 层（`IsLegalPick:2105/2108`、`PlayTargetPool:2167/2170`）**显式传 `includeRemnants: true`** = 行为逐字不变（理由是那是 `IsValidTarget` 另一条谓词，见 §尚需确认 ③） | `typecheck` 0/0；`play check` 指纹与改前**逐字节相同**（含 `ta=10 allFiltered` 那批卡）<br>断言：**B1/B1'/B2**（`op==null` 剔残骸 · `IncludeRemnants=0` 剔 · `=1` 保留） | 🔴 **形参默认值是 `true` 不是 `false`** —— 见 §尚需确认 ②：`RuleEngine/Core/RuleCore.cs:18002`（`AbilityTargetListFromCriteria`，**不在我的白名单**）自己按 `criteria.IncludeRemnants` 剔，默认改 `false` 会在那条路上把 `includeRemnants=1` 的残骸误剔。若调度台要把默认翻成 `false`，**必须同刻**把那两行改成 `includeRemnants: criteria.IncludeRemnants > 0`（粘贴件已给在 §尚需确认 ②） |
| **B-181** | ① `EffectText.AssignAbilityTargets` 的 G1 拆开：`blocks.Count == 0` 仍直接 `return`；`blocks.Count != abil.Count` 时**先给每条 op 记 `TargetCriteriaGap = "block-mismatch"`** 再 `return`（⛔ 不贴值、不按位置猜）。<br>② `ApplyTargetCriteriaFilter` 的 `set == null` 分支**新增第三个出声口** `else if (op.TargetCriteriaGap == "block-mismatch")` ⇒ `StructuredConditionNotice` | 改前 `RuleEngine/Core/EffectText.cs:2061`（`if (blocks.Count == 0 \|\| blocks.Count != abil.Count) return;`）；改后 = `@@ -2061 +2061,15 @@`（+14 行）<br>出声点：`EffectResolver.cs` 的 `ApplyTargetCriteriaFilter`，hunk `@@ -2392,0 +2403,10 @@`（+10 行） | `typecheck` 0/0；`play check` 指纹不变（该分支只写字段 + 出声，不改池）<br>断言：**C1/C2/C3**（夹具 = `SW49` 形状：`desc` 一句、`cardAbilities` 两条 ⇒ 每条 op 都有 `block-mismatch`、且消费端去重表**真的多一条** `targetcriteria\|block-mismatch\|…`；对照 op（`gap=""`）不产生该键） | 见 §顺手发现 ③：还有**第四条静默路**（`ta.Length != blk.Count`，既无 criteria 也无 gap）本批**没动**（不在本三件范围内，⛔ 没顺手改） |

### 普查真值（多少卡 / 多少 op 无 criteria）
工具：**新建** `d:/4/Unity/工具/criteria_census.py`（只读，纯 Python；`python -I -X utf8 …`）
产出：`d:/4/Unity/资料/完全复刻/判据/B113W2_criteria普查.tsv`（**1621 行数据 + 1 行表头**，逐卡×逐 `(ab,lg)` 一行）

输入 = `MyGame/Assets/RuleEngine/Resources/cards_engine.json`（**`version=46` / `count=1129`**，两次跑之间 W11 把它从 45 重生成到 46，**结构数字两次完全一致**）。
⛔ 没用 `cards_raw.json`（`full_raw` 会让统计**翻倍**）；`cards_truth.json` **缺 `abilityTriggers` 列**，这一面只能读 `cards_engine.json`。

| 口径 | 真值 | 说明 |
|---|---|---|
| 池 | **1129 张 / 1621 条 logic 行** | logic 行 = 所有 `abilityTargets[].ta` 的长度之和 |
| **缺 criteria 行（`no-row`）** | **0 条** | 🔴 **数据侧从来不缺行** ⇒ 账里那个「99 张上界 / 71 张 `includeRemnants==0`」**不是缺口来源**，请按本表替换 |
| `ta` 长度 == 1（**唯一可挂**的前提） | **982 条** | 挂载要求 `blk.Count == 1` ⇒ 只有 `ta` 长度 1 的 ability 有机会 |
| `ta` 长度 ≥ 2（**结构上挂不上**） | **290 条**（落在 **272 张卡**） | `blk.Count` 必须 == `ta.Length`，而贴值又要求 `blk.Count == 1` ⇒ 这一档**必然 `logic-gate`** |
| `ta` 长度 == 0（没落 `targetsAffected`） | **1 条** | 走 `EffectText.cs` 的 `ta.Length == blk.Count` 闸不成立 ⇒ 静默（见顺手发现 ③） |
| `ta == 0`（`any`：criteria 在，但**等于不筛**） | **287 条** | 这些行**不产生**「残骸进池」的风险（`Matches` 第一句就 `return true`） |
| `includeRemnants == 0`（原版剔残骸） | **1458 条** | |
| `includeRemnants == 1`（原版留残骸） | **163 条** | ⇒ `AddSide` 闸**不能一刀切**（B2 断言钉的就是这个） |
| 整卡 `abilityTargets` 行数 ≥ 2（**可能** `block-mismatch`） | **242 / 1129 张** | ⚠️ 这一档**本脚本判不了**（要 C# 的 `BuildAbilityBlocks`）⇒ 标 `block-risk`，⛔ 别当已确认 |
| 完全 `attach-ok` 的卡 | **548 张** | 单 ability + 单 op 块 |
| 完全**没有** `abilityTargets` 行的卡 | **119 张** | 这类卡的 criteria 面本来就无从挂（`AssignAbilityTargets` 第二句就 return） |

**⇒ 一句话真值**：**「没有 criteria」不是数据缺行（0 条），而是挂载闸拦掉的** —— 可确认的 = **290 条 logic 行 / 272 张卡**（`ta` 多值，结构上必然 `logic-gate`）＋ **1 条**（`ta` 空）；另有 **242 张卡**处在 `block-risk`（要解析器才能定），**548 张**完全挂得上。

### 改动清单（文件 + 净行数）
| 文件 | 净变化 | hunk（改前行号） | 内容 |
|---|---|---|---|
| `Assets/RuleEngine/Core/EffectResolver.cs` | **+86 / −8（净 +78）** | `1683` · `1744`（ResolveTargets 的 AddSide 传值）· `2105` `2108` `2167` `2170`（pick 层显式 `true`）· `2392`（出声支 +10）· `3031` `3036`（AddSide 签名 + 残骸闸 +10）· `3616`（B-210 出伤池 criteria 闸 +25）· `21096`（文件尾 `IncludeRemnantsOf` +23） | 三件账的主改动（全部按「能原地改就原地改、新成员追加类尾」） |
| `Assets/RuleEngine/Core/EffectText.cs` | **+15 / −1（净 +14）** | `2061` | G1 分支拆开 + `block-mismatch` 原因码 |
| `Assets/RuleEngine/Editor/RuleEngineTest.cs` | **+1 / −1（净 0）** | `1372` | **同一行末尾追加** `Step(TestB113W2);`（⛔ 该行无行尾 `//`，已先确认；账行号**零漂移**） |
| `Assets/CardPresentation/Editor/B113W2_Assert.cs` | **新建 367 行** | — | `public static partial class RuleEngineTest`（复用既有夹具 `DeckOf`/`ProbeBattle`/`Place`/`HandIdx`/`Check`，⛔ 没抄第二份夹具）。⚠️ **`.meta` 还没生成** —— 本波不跑 Unity，等首次导入时由 Unity 生成（同目录既有 assert 都有 `.cs.meta`，这一件没被任何场景/prefab 引用 ⇒ 不影响别处 GUID） |
| `工具/criteria_census.py` | **新建**（只读工具） | — | 普查；`--out` 可改输出路径；无参数 = 用默认路径 |
| `资料/完全复刻/判据/B113W2_criteria普查.tsv` | **新建 1622 行** | — | 普查产物 |

行尾：`EffectResolver.cs` / `RuleEngineTest.cs` 保持 **CRLF**（现读 `crlf == lf`，`bare_cr = 0`）；`EffectText.cs` 保持 **LF**（`crlf = 0`）；新文件按各自惯例（`.cs` LF/CRLF 混用不产生裸 CR）。

### 顺手发现
1. **`cards_engine.json` 在我两次普查之间被重生成**（`version 45 → 46`，W11 的活；`count` 仍 1129）。两次结构数字**逐项相同** ⇒ 本报告的数字不受影响，但**引用 version 时请现读**。
2. **`no-row` 在数据侧恒不发生（0/1621）** ⇒ 账 `B-208` 里那句「上界 99 张 / 71 张 `includeRemnants==0`」应就地换成上面的真值表（`B113W_对账_A` 的「尚需确认」第一条可以关掉）。
3. 🔴 **还有第四条「静默不挂」的路（本批没动）**：`EffectText.cs:2090` 的 `if (ta != null && ta.Length == blk.Count)` —— 整卡块数对得上、但**这一块里的 op 数 ≠ 这条 ability 的 `ta` 长度**时，**既不贴值也不记 gap** ⇒ 与 `B-181` 修掉的那条同形（`ApplyTargetCriteriaFilter` 的两个出声口都不喊）。普查里 `ta` 长度 0 的那 1 条走的就是这一族。建议另立账（⛔ 我没有顺手改：不在本三件范围，且要定「块内 op 数 ≠ logic 数」该记什么原因码）。
4. **`RuleCore.AbilityTargetListFromCriteria`（`RuleEngine/Core/RuleCore.cs:17992-17993`）也是 `AddSide` 的调用点**，它在池建完后（`:18002`）**自己按 `criteria.IncludeRemnants == 0` 剔** ⇒ 那一档的语义本来就对。这也是我把新形参默认值定成 `true` 的原因（见下条）。**`RuleCore.cs` 不在我的白名单，一字未动。**
5. `ApplyTargetCriteriaFilter` 的 `set == null` 分支在 `TargetCriteriaGap == ""` 时**依然完全静默** —— 那是「这张卡确实没有 criteria 数据」的合法形状（本池 0 张），也是上面第 3 条那条路的形状（静默、但**应该是 bug**）。两者的区别只能靠原因码，⛔ 别把 `set == null` 一律出声（会把 1129 张卡刷屏）。

### 尚需确认
1. **原版督军是否算出伤单位**（`B-210` 的另一半）：`UM82`/`ASH43` 两份 criteria 都是 `includeHero=1`，`B193` §2·2 明确写「未查清」⇒ 我**没有改**这条语义，只在断言夹具里把 `includeHero` 设为 **0** 让「出伤单位个数」可数（并注明理由）。要钉死得跑 `play check` 对拍或读 `CardScript__TriggerOtherCardAbility` 下游的 `param_4` 用法。
2. **`AddSide` 的默认值**：现在是 `true`（= 不剔，与改前逐字一致），残骸闸**由调用点按 criteria 决定**。若要把默认翻成 `false`（更接近「原版默认剔残骸」的字面），**必须同刻**改外部调用点：
   ```csharp
   // RuleEngine/Core/RuleCore.cs:17992-17993（不在本波白名单）
   if (criteria.FriendlyUnits > 0) AddSide(pool, ctx.Players[owner], false, false, -1,
                                          includeRemnants: criteria.IncludeRemnants > 0);
   if (criteria.EnemyUnits > 0) AddSide(pool, ctx.Players[1 - owner], true, false, owner,
                                       includeRemnants: criteria.IncludeRemnants > 0);
   ```
   不改而翻默认 ⇒ 那条路上 `includeRemnants=1` 的残骸会被**误剔**（静默）。
3. **pick 层（`IsLegalPick` / `PlayTargetPool`）要不要也剔残骸**：我**故意保持原样**（显式 `includeRemnants: true`）。理由是这一层镜像的是原版 `IsValidTarget`（另一条谓词），它对残骸的态度**本波没查**；而结算层单目标那一支会 `list.Add(chosen)`（`ResolveTargets` 的 `spec.Count == 1` 支）⇒ 今天不会出现「高亮能点、打出去空过」。若要统一，得先核 `RawCardScript__IsValidTarget` 对残骸的判定。
4. **`block-risk` 的 242 张卡**到底有多少真的 `block-mismatch`：只有跑 C# 解析器（`RuleEngineTest` 里的 `EffectText.Parse`）才知道。建议在同步点后补一条只读普查断言（遍历 1129 张卡、统计 `TargetCriteriaGap` 分布）——本波不跑 Unity，做不了。
