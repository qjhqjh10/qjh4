# B117_W1 · `B117-01`：引擎判 trait 只读 `keywords`、不读 `cardTraits`

> 执行写手：`B117_W1`（2026-11-02）· 活状态表 `B117-01`
> 一句话：把「原版 `MeetsTraitCriteria` 读**当前 trait 表**」这条判据，接到两个只读 `keywords` 的消费者上。

---

## 一、改了什么（文件:行号 + 为什么）

### 判据（现读，⛔ 不是我挑的）

| 来源 | 现读内容 |
|---|---|
| `decomp_full/FilterMethods__MeetsTraitCriteria.c:30-47` | `uVar1 = *(undefined8 *)(param_1 + 0x128)` ⇒ 判的是 **`CurrentTraits(+0x128)`**，不是 `keywords` 列 |
| `decomp_full/CardScript__SetBaseCardTraits.c:37-42` | 实例化时 `List += rawCard.CardTraits(+0x230)` **整表灌进** `+0x128` ⇒ 「当前 trait 表」= 卡面 `keywords` ∪ 运行期授予 ∪ **`cardTraits`** |
| `decomp_full/CardScript__TransformIntoRemnant.c:54-75` | 翻残骸那一刻把当前表**逐条摘光**（⇒ 残骸上读不到卡面那批） |

数据现扫（`cards_engine.json`，1129 张）——**全池只有 5 条 trait 是「只在 `cardTraits`、不在 `keywords`」**：

| trait | 卡 | `keywords` | `cardTraits` |
|---|---|---|---|
| `unstoppable` | `SAU7 Canoptek Wraith` | `["Remnant"]` | `["Unstoppable","Remnant"]` |
| `ephemeral` | `Daemonbreaker`（`e01f4d54e8a474d96b5a1167cc28eebf`） | `[]` | `["Ephemeral"]` |
| `flank` | `GOF57 Ork Nob` | `[]` | `["Flank"]` |
| `slay` | `AM72 Vitus Gryf` | `["Flying","Strike"]` | `["Flying","Slay"]` |
| `oathdouble` | `UM100 Chaplain Cassius` | `["Talent: Catechism of Death"]` | `["Talent","oathDouble"]` |

（另 `SOR30 Celestian Sacresant` 的 `keywords` 写的是 `vanguard.`（带句点），`KeywordTable.Normalize` 走前缀匹配仍认得出 ⇒ **不是**缺口。）

### 1. `CardDef.cs` 类尾 —— 新增 `HasTrait`（`:9599-9613`）

`public bool HasTrait(string name)` = **`HasDefaultTrait` 的薄封装**（规则仍只有一处，⛔ 没写第二份归一化）。

- 唯一的增量行为：`cardTraits` **没喂**时 **`LogWarning` 出声一次**（全进程一次；`_warnedNoCardTraits` 在 `:9613`）。
  原版那条 `null` 分支**不存在** ⇒ 我们不许静默把「判不了」当成「没有」。
- ⚠️ **为什么不是「新建一套并集」**：仓里**早就有一份更完整的**
  `UnitState.HasCurrentTrait`（`Core/UnitState.cs:4381`，`B116_W19` 建的）=
  `keywords` ∪ 运行期 ∪ `cardTraits`，且带**残骸档**（`.c:54-69`：只认 90 档）与**已摘除**两道闸。
  ⇒ 场上单位一律用它（见下面第 3 条），`CardDef.HasTrait` 只服务「**手里/牌库里的卡**」这类没有 `UnitState` 的地方。
- 追加在**类尾**（`账/*.tsv` 的 `CardDef.cs` 落点最大 = `:9436`，本段在其后 ⇒ 逐条零漂移，闸已复跑确认）。

### 2. `BattleContext.cs:1546` —— `IsEphemeral` 补上 `cardTraits`

```csharp
return inst.Card.Has("ephemeral") || inst.Card.HasTrait("ephemeral") || inst.EphemeralMarked;
```

为什么：手牌 / 牌库里的卡**没有 `UnitState`**（用不了 `HasCurrentTrait`），只能在 `CardDef` 层补这一位。
受害者 = `Daemonbreaker`：`keywords` 是**空表**、`cardTraits=["Ephemeral"]` ⇒ 改前 `IsEphemeral` 恒假
⇒ 回合末 `RuleCore.SweepEphemeral` **不把它移出**，而原版会。
doc 注释（`:1539-1541`）同步订正成「三个来源」并写明现读张数（`keywords` 103 / `cardTraits` 104）。

### 3. `ConditionEval.cs:242-243` —— 条件 50 `flying` 改走 `HasCurrentTrait`

```csharp
return (u.HasCurrentTrait(RuleCore.Unstoppable) || u.HasCurrentTrait(KeywordTable.Flying))
    && !u.HasCurrentTrait(KeywordTable.Vanguard);
```

为什么：原版 `MeetsConditionCriteria.c:38-45` 的三个 `MeetsTraitCriteria` 读的都是 `+0x128`（见上表判据）。
受害者 = `SAU7 Canoptek Wraith`（卡面只印 `Remnant` + 「可无视带 Vanguard 的敌军」这句 `Unstoppable` 的语义，
**不印 `Unstoppable`**）⇒ 改前条件 50 判假。
⚠️ **为什么用 `HasCurrentTrait` 而不是 `Has(x) || Card.HasTrait(x)`**：后者会**绕过残骸档** ——
原版翻残骸那一刻当前表被逐条摘光（`TransformIntoRemnant.c:54-75`），不绕过的写法才能对上；断言 ④ 专门钉这一条。

### 4. 顺手订正（铁律 5 · 就地在 `CardDef.cs:6367-6371` 改，行数不变）

原文写着「**接线不属于本代理白名单** ⇒ `CardDatabase.cs` 的 `new CardDef(...)` 要加 `cardTraits: c.cardTraits`」——
**已过期**：现读 `Data/CardDatabase.cs:1734-1735` 就是 `def.AttachCardTraits(c.cardTraits); def.AttachKeywordId(c.keyword);`
⇒ 生产路径上 `CardTraitsKnown == true`、`KeywordId != -1`；只有**夹具 / 旧表**才可能没喂。

### 影响面（为什么「只改判定、不改解析」）

- 解析层**一行没动**（`EffectText` / `gen_cards_engine.py` / `cards_engine.json` 全未碰）⇒ `ruleprobe check` 指纹不变（见 §二）。
- 结算层：`play check` 也**rc=0**（改前改后结算指纹未变）。
  ⚠️ **归因是推断、未逐条证**：最可能是「两处受害者都没落进探针的夹具路径」
  （`Daemonbreaker` 的手牌态没被那几组电池扫到 · `SAU7` 的条件 50 没被求值）。
  **如实标注**：本条没做隔离实验（只跑了一次改后态），要坐实得用「关掉本笔两行再跑一次 `play all dump` 比逐卡」那套。

---

## 二、typecheck / ruleprobe check 读数

| 闸 | 命令 | 读数 |
|---|---|---|
| 秒级类型检查 | `TMPDIR=/tmp/wf_b117w1 bash d:/4/Unity/工具/typecheck.sh` | 运行时 **0** 错 · 编辑器 **0** 错（**0/0**） |
| 解析对拍 | `bash d:/4/Unity/工具/ruleprobe.sh check` | `=== 全池 1121 张 · 解析差异 0 行 · 改名 0 行 · 新增 0 · 消失 0 ===` · 哨兵合计 **失败 0 条** · **`✓ [输出指纹] … ebdac779…`** · **rc=0** |
| 结算对拍（**额外自查**，简报没要求） | `bash d:/4/Unity/工具/ruleprobe.sh play check` | **rc=0**（基线 `a6ab8bcb…` 未变） |
| 账闸（**额外自查**） | `python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py` | `A 5831 条 · 通过 5831 · 漂移 0 · **失败 0**`（B 档 4 条为既有的散文类，非本批引入） |

⛔ **没有跑 Unity 批处理**（同一工程只能一个实例，按简报交调度台统一跑）。

---

## 三、断言清单（哪一节、几条、灭自证点）

宿主 = **已接线**的 `Editor/RuleEngineTest_B116W19.cs`（`RuleEngineTest.cs:1306` 的 `Guard("CheckB116W19Jam", …)`）
⇒ **零新增接线**：新节 `B117W1CardTraitsOnly()` 在 `:292`，由 `CheckB116W19Jam()` 的 `:41` 调用。
理由：`B117-01` 与本文件是同一条口径（`cardTraits` 列 = 原版 `+0x128` 的一部分），且该文件当前**无写手占用**
（`文件所有权_当前.md`：116 波全部交回）。

共 **10 条**（`Check` / `CheckTrue`）：

| # | 断的是什么 | 灭自证（🧨 改哪儿会红） |
|---|---|---|
| ①-a | 前提：`SAU7` 的 `keywords` 里**没有** unstoppable（缺口本体） | 若哪天数据被改 ⇒ 本条红（提醒「病已经不在原处」） |
| ①-b | `SAU7` 的 `cardTraits` 里**有** Unstoppable ⇒ `CardDef.HasTrait` 为真 | 删掉 / 改成只读 `keywords` ⇒ 红 |
| ①-c | 场上单位 `HasCurrentTrait(unstoppable)` 为真 | `HasCurrentTrait` 退回 `Has` ⇒ 红（那条在 `UnitState.cs`，本批没动） |
| ①-d | **消费端**：`RuleCore.MeetsUnitCondition(ctx, u, 50, false) == true`（`SAU7`） | **把 `ConditionEval` 那三处 `HasCurrentTrait` 改回 `Has` ⇒ 红**（本件主诉） |
| ④-a | 前提：活着的时候条件 50 为真（与 ④-b 成对） | —— |
| ④-b | **残骸档**：`u.IsRemnant = true` 之后条件 50 **必须为假** | **把并集写成 `u.Has(x) \|\| u.Card.HasTrait(x)`（绕过残骸档）⇒ 红** |
| ②-a | 前提：`Daemonbreaker` 的 `keywords` 是空表 ⇒ `Has` 判不到 | —— |
| ②-b | `cardTraits` 里有 `Ephemeral` ⇒ `CardDef.HasTrait` 为真 | 删掉 `HasTrait` ⇒ 红 |
| ②-c | **消费端**：`ctx.IsEphemeral(新实例)` 为真（且前提 `!inst.EphemeralMarked`，排除「标记那一位」在充数） | **把 `BattleContext.IsEphemeral` 里的 `HasTrait("ephemeral")` 去掉 ⇒ 红** |
| ③ | **反证**：`ASH83 Autarch`（两张表都没有 ephemeral）⇒ `IsEphemeral` **为假** | 恒 true / 把 `HasTrait` 写成恒真 ⇒ 红 |

⚠️ 全部按「先 `Check(前提)` 再 `Check(结论)`」写（没有 `if (前提) Check(结论)` 那种静默跳过）。
⚠️ 断言**我只写不跑**（简报要求），真跑由调度台用 `RuleEngineTest.Run` 统一做。

---

## 四、其他 `MeetsTraitCriteria` / `HasDefaultTrait` 等价物清单（**只报告、未改**）

扫描口径：`grep -n "MeetsTraitCriteria" Core/ConditionEval.cs` 的注释映射 ＋ `grep "u.Has(\|Card.Has(" Core/*.cs`（105 处）逐处判
＋ 全池数据对账（§一那张 5 行的表就是「今天真受影响」的全集）。

### A. 今日**有真实受害者**的（同源缺陷，建议排期）

| 落点 | 原版判据 | 现状 | 池内受害者 |
|---|---|---|---|
| `Core/ConditionEval.cs:220` 条件 20 `mightAttack` | `CardScript__get_mightAct.c` 的 ③ `displaySummonSickness` = `+0x58 && !fast && !flank`，读**当前 trait** | `u.Has("flank")` | **`GOF57 Ork Nob`**（`Flank` 只在 `cardTraits`）⇒ 部署当回合会被判成召唤病、**不能攻击**，原版能。⚠️ 同一行 `fast` 那一格今天无受害者 |
| `Core/RuleCore.cs:3986`（成就 `Ach_12 Play Card With Ephemeral`） | 原版 `withTrait=[5]` = `DefinedTrait.ephemeral`（**trait**） | `card.Has("ephemeral")`（注释还写着「104 张现读」—— 而 `Has` 只数得到 **103**） | **`Daemonbreaker`** 打出去**不计**这条成就 |
| `EffectResolver.OathExtraReplays` → `u.Card.OathDouble` | `BattleManager__IsThereDoubleOathEffect.c:33` 扫**当前 trait** | `OathDouble` 这个 bool 由**正文文本**落（`CardDef.cs:4401` `case "誓约加倍"`），不走 `cardTraits` | **`UM100 Chaplain Cassius`**（`cardTraits` 里有 `oathDouble`）—— ⚠️ 待核：它的正文是否另有一句落到那个 bool（本批没查到明确的否证，**写「待核」不写结论**） |

### B. 今日**零影响**、但形状同源（同类闸，一旦数据出现就静默失效）

| 落点 | 原版 | 现状 |
|---|---|---|
| `ConditionEval.cs:250` 条件 100 `protectedFromDamage` | `MeetsTraitCriteriaFromList({dodge(240), invulnerable(310), dropPod(230)})` | `u.Has(...)` ×3 |
| `ConditionEval.cs:289-290` 条件 120 | `MeetsTraitCriteriaFromList`（＋ shield / survivor） | `u.Has(...)` ×4 |
| `ConditionEval.cs:340` 条件 70 `stealth` | `MeetsTraitCriteria(70 stealth)` | `u.Has(Stealth)` |
| `ConditionEval.cs:501` 条件 130 `jam` | `!HasCurrentTrait(130 jam)`（**注释自己写的就是 `HasCurrentTrait`**，代码却是 `Has`） | `u.Has(KeywordTable.Jam)` |
| `ConditionEval.cs:525-526` 条件 150 `tainted` | `MeetsTraitCriteriaFromList({stealth,ward})` ∨ (`MeetsTraitCriteria(160 tainted)` ∧ `!MeetsTraitCriteria(980 vanguard)`) | `u.Has(...)` ×4 |
| `ConditionEval.cs:533` 条件 170 `camouflage` | `MeetsTraitCriteriaFromList({stealth(70), camouflage(985)})` | `u.Has(...)` ×2 |
| `ConditionEval.cs:215/222/223` 条件 20 的另外三道 | `HasCurrentTrait(bloodthirst/cantAttack/nonCombatant)` | `u.Has(...)` |
| `RuleCore.HasDeployExemption`（`fast`/`flank`/`ferocity`）、`EffectResolver.cs:9680` 注释提到的同一族 | 读**当前 trait** | `u.Has(...)` |

> 数据现扫：这些 trait 在 `cardTraits`-only 的卡 **= 0**（`flank` 除外，已在 A 段）⇒ 今天**零行为差异**。
> 建议做法统一为一句话：**凡是原版写 `MeetsTraitCriteria` / `HasCurrentTrait` / `HasDefaultTrait` 的读点，
> 一律走 `UnitState.HasCurrentTrait` / `CardDef.HasTrait`**；`Has` 只留「卡面关键词行」这一层语义。

### C. 顺带说明：`slay` 那张为什么不算缺口

`AM72 Vitus Gryf` 的 `Slay` 只在 `cardTraits`，但**全仓 `Has("slay")` / `Has(KeywordTable.Slay)` = 0 命中** ——
Slay 的触发走 `abilityTriggers` 列（`ActionPump.cs:1100` 的 `TriggerLabel`），不走关键词表 ⇒ **今天不可达、零影响**。

---

## 五、顺手发现（**未改**，交调度台分流）

1. **三处过期注释仍在说「`cardTraits` 不喂引擎」**（与本批在 `CardDef.cs:6367` 就地订正的是同一句错）：
   - `Core/EffectResolver.cs:19117`：「⚠️ `CardDef` 的 `cardTraits` 列**没喂引擎** ⇒ 这里走不了 `HasDefaultTrait`」
     —— 现读 `HasDefaultTrait` 在 `CardDatabase.cs:1734` 之后是**可用**的，这句话会让下一个人以为必须走文本兜底。
   - `Core/RuleCore.cs:13754`：「数据是纯落值、不喂引擎」。
   - `Data/CardDatabase.cs:170`（`CardDto.cardTraits` 的 doc）：「**本列不喂引擎**（纯落值）」。
   ⛔ 三个文件都不在本批白名单 ⇒ 只报告。
2. **`UnitState.cs:4378` 的交接注也半过期了**：它写「`EffectResolver.cs` / `ConditionEval.cs` /
   `EffectResolverPartial_B88E2.cs` 共 4 处仍走 `Has`」—— `ConditionEval` 的 **jam(:501)** 那处仍在（见 §四·B），
   但**条件 50 已经换过来了**；建议那条注改成逐处点名，别写文件级。
3. **`ConditionEval.cs:501` 的自相矛盾**：上一行注释写「原版 `:227` 那条 `!HasCurrentTrait(130)`」，
   下一行代码是 `u.Has(KeywordTable.Jam)`。今天池内 `jam` 在 `cardTraits`-only 的卡 = 0 ⇒ 零影响，
   但这是「注释与代码各说一套」的典型，值得和 §四·B 一起批量收口。
4. **`RuleCore.cs:3985` 那个「104 张现读」与本行代码对不上**：`card.Has("ephemeral")` 现数得到 **103**，
   104 是 `cardTraits` 那一列的数 ⇒ 数字本身是「两列混着写」的产物（见 §四·A 第 2 行）。
5. **`CardDef.Has` 与 `HasCurrentTrait` 的分工建议写进 `CardDef.Has` 的 doc**（它现在只说「卡面事实 vs 场上状态」，
   没提「原版 trait 家族读的是后者」）。本批没动 `Has` 的 doc（`CardDef.cs:6121-6143` 那一段被账按行号引用，
   插行会整片漂）⇒ 只报告。
