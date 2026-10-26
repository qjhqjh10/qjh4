# B9 · `E10TB` —— `BL47` 加法型 for-each ＋ `UnitState` 三件（P-05）

> 执行代理 **`E10TB`**（有写权限）· 2026-10-26 · 第八十四会话 · 完全复刻批次 9
> 白名单（独占）= `RuleEngine/Core/EffectText.cs` · `RuleEngine/Core/UnitState.cs`
> ⛔ 没跑 Unity · 没动 git · 没改两张正本 · 没改主账表（只写 `账/_parts/B9_E10TB.tsv`）· 没碰白名单外文件。

## ① 结论

| # | 件 | 结论 | 读数（改前 → 改后） |
|---|---|---|---|
| A | **`BL47 Disgustingly Resilient`**：`ReAdditionalForEach` 只认「数字在前」，把**加法型**判成了**重复型** | ✅ **修完**（`EffectText.cs`：新增 `ReAdditionalForEachReversed` ＋ `TryForEach` 两行按 `reversed` 取组） | `ruleprobe card`：`perCount=0` → **`perCount=1`**；目标短语 `[a friendly troop, and an additional armour 1]` → **`[a friendly troop]`**；`play BL47`：**一条变化都没有** → `✎ 改动 P0[3] FIX_A 2/0/0/2 → 2/0/1/2 {armour1}`（护甲 +1）；`pool_effect_check` 该行 **错 → 对**（全表 `错 1 → 0`） |
| B① | **P-05① `1040 HasCurrentTrait`**：缺**残骸闸**、缺「90 档」那一本书 | ✅ **修完**（`Has` 加残骸档；`_remnantTraits` 一本 ＋ `AddKeyword`/`RemoveAll`/`ClearAllKeywords` 三个读写点） | 离线真跑：活单位 `Has(卡面 trait)`=**true** → `IsRemnant=true` 后 = **false**（改前口径 `_keywords.ContainsKey` = **true**）→ 残骸期间 `AddKeyword` 的 = **true** → `IsLeavingRemnant=true` 又 = **true**；`KwValue` 恒不变（原版那一位没闸） |
| B② | **P-05② `938 TransformFromRemnant`**：原账「属性由谁恢复读不到」 | ✅ **判据闭合并落地原语**（`ResetToValuesInHand` 逐句搬 ＋ `TransformBackFromRemnant`）；⚠️ **与派活单验收口径相反**，见 §二·3 | 离线真跑 11 条断言全过（`IsRemnant/IsLeavingRemnant` → false · `AttackBase/MaxHealthBase` 回卡面 · `Health=MaxHealth` · 卡面 trait 又可见 · `statMods=0` · `AttacksThisTurn=0` · `UsedActiveAbilityThisTurn=false` · `OathUsesThisTurn=0`） |
| B③ | **P-05③ `853 RemoveEffect`**：5 / 45 / 40 三档无落点 | 🟡 **按记录摘的原语已备好＋三档落地**（`EffectRecord` + `RemoveEffectRecord`，1/3/90/5/40/45 六档）；**仍缺调用点与两条特判** | 离线真跑 8 条断言全过：case5 攻/血回卡面且夹血 ✓ · case45 drop pod 血量 3→1 ✓ · case1 护甲 2→1 ✓ · case40 主动能力仍是卡面那份 ✓ · `permanentBuff` ⇒ 不摘 ✓ · 空档 buffType 8 ⇒ 不动 ✓ |

**必报三项**

* **typecheck**：`TMPDIR=/tmp/wf_E10TB bash d:/4/Unity/工具/typecheck.sh`
  ⇒ 本代理最后一次 `.cs` 改动之后那一次 **运行时 0 / 编辑器 0**；**收尾复跑**：**运行时 0**，
  **编辑器 1 条**且落在**别人正在写的文件**上 —— `Assets/CardPresentation/Editor/ShellScene.cs(4732,32): CS0117
  「ShellRuntime」未包含「MusicVolumeMultiplier」的定义`（同一轮里 `ShellRuntime.cs` 也曾报过 `QueuedMusic`，随后被那位写手补上）。
  按铁律 13·3 第 2 条：**错误一条都不在 `RuleEngine/Core/{EffectText,UnitState}.cs`**，且
  `工具/ruleprobe`（只编 `Core/*.cs`）**构建 0 错** ⇒ 本批两个文件编得过。

* **`pool_effect_check`**：`--run` 全池 57 族 ⇒ **`对=1145 · 错=0 · 未验到=783`**（723 行并掉 72 行重复）。逐行对比 `HEAD` 那一版主表：**1928 行里只有 `BL47 / 30` 一格变了（错→对）**，其余逐字相同。
  ⚠️ 本批**用私有 cache** 跑（`--cache _tmp_view/p1_pool_E10TB`）—— 共享 cache（`_tmp_view/p1_pool`，19:54 那版）**没有**被我这批覆盖，调度台在同步点重跑一次即可。`--only 30` 也照跑：`对=381 · 错=0`。
* **`play check` 指纹**：**当次现算 = `1b40f42a…`**，钉住的基线文件 = **`af8e8178…`**（= 仓里那份，未动）。
  逐卡差异 **4 行**，逐条解释见 §三·2：**`BL47`（本批 A，预期）· `SW25`（本批 B① 的副作用，已用隔离实验定位）· `BL18` / `UM18`（既有归因 `E7P06` 接 669，非本批）**。

---

## ② 逐条证据

### ②·1 `BL47` —— 解析层把「加法型」判成了「重复型」

* **卡面**：`Give Armour 1 to a friendly troop, and an additional Armour 1 for each Dark Pact on it`。
* **结构化真值**（`cards_truth.json` BL47 两条 logic）：`buff{buffType 1, armourChange 1}` ＋
  `buff{buffType 1, armourChange 1, armourValueOptions 201}` ⇒ 期望 = **1 + n**（n = 该目标身上的 Dark Pact 层数）。
* **改前病根**（`card "Disgustingly Resilient"` 现读）：`ReAdditionalForEach` 的正则要求**数字在前**
  （`and an additional +2 Melee`，`BL43` 那种写法），而 BL47 是**名词在前**（`and an additional Armour 1`）
  ⇒ 那条正则不匹配 ⇒ 落进 `TryForEach` 的「后置 `for each`」支 ⇒ `PerCount=0`、`CountRef="it"`
  ⇒ 结算层按 `TryComputeRepeats` 数出 **0 遍** ⇒ **整条跳过**（连基础那一份也不结算）、附加从句还被目标短语吞掉。
* **改法（两处小改，`BL43` 那条路一字不动）**
  * `EffectText.cs:5046` 新增 `static readonly Regex ReAdditionalForEachReversed`（组序**相反**：组 1 = 属性词、组 2 = 数字、组 3 = 计数短语）；
  * `TryForEach`（`EffectText.cs:4905`）：先试原正则，不中再试反向那条并记 `reversed`；
    `int inc = …（reversed ? 组2 : 组1）`（`:4918`）· `string unitWord = …（reversed ? 组1 : 组2）`。
  * `BaseAmountOf` / `PayloadMatchesUnit` **一个字没改** —— `payload=[armour 1]` 里抠出 `1`、与 `unitWord="armour"` 同属性词，两条本来就通。
* **可达性（全池扫过）**：`cards_engine.json` 1128 张里含 `an additional` 的 **11 张**，其中
  `and an additional <数值> for each` 形状只有 **`BL43`（数字在前）/ `BL47`（名词在前）** 两张 ⇒ 新正则**只可能**命中 BL47。
  ⚠️ **顺手发现**（不在本批白名单）：`8f2da557…` 写的是 `**plus** an additional 1 for each friendly troop`
  —— `and` 换成 `plus` ⇒ 两条正则都不匹配；今天它走的是别的支（`card` 读数 `perCount=0`、目标短语正常），
  **是否另有缺陷记在这里**，请调度台立账。

### ②·2 `1040 HasCurrentTrait` —— 残骸闸 ＋ 「90 档」那一本书

* **判据（🥇 `decomp_full/EntityScript__HasCurrentTrait.c:1-75` 全文逐句）** 三支：
  ```
  if (isRemnant(+0x65) == 0 || isLeavingRemnant(+0x66) != 0) {
      CurrentTraits(+0x128) 命中 ⇒ true；否则 activeEffects(+0x108) 里 {buffType==3 addTrait, trait 命中} ⇒ true
  } else {                                  // 残骸 ∧ 不在离场窗口
      activeEffects 里 {buffType==0x5a(90 addTraitRemnant), trait 命中} ⇒ true；否则 false
  }
  ```
  ⇒ **残骸身上只认 90 档授予的**；卡面印的那一份在残骸上**不算**。
* **改前**：`Has` 就是 `_keywords.ContainsKey` —— 一本合并字典，**没有残骸闸**。
  残骸出厂时 `RuleCore.CleanupDeaths` 已经调过 `ClearAllKeywords`（卡面那批确实清掉了），
  但**之后**挂上来的（光环给的 `pack`、残骸期间授予的 trait）落进同一本 ⇒ **残骸看得见非 90 的 trait**。
  实测两例：`SW25` 事件电池里那个残骸带 `{pack1}`（改前 `Has("pack")=true`）。
* **改法**
  * `UnitState.cs:2620` 新增 `readonly HashSet<string> _remnantTraits`（= 原版 `activeEffects` 里 `buffType 90` 那一批）；
  * `UnitState.cs:968` `Has`：`if (IsRemnant && !IsLeavingRemnant) return _remnantTraits.Contains(keyword);`
  * 写点 `UnitState.cs:1157`（`AddKeyword` 里 `if (IsRemnant && !IsLeavingRemnant)` 记账）、
    摘点 `:1286`（`RemoveAll`）/ `:2584`（`ClearAllKeywords`）/ `:2707`（翻回时整本摘）；
  * **为什么「此刻是残骸」就能等价于「90 档」**：闸③（`CardScript__AddEffect.c:159-170`）对残骸**只放行**
    `addStats` 的生命增减与 `buffType 90`，而前者不授 trait ⇒ 残骸期间能被挂上的关键词只有 90 那一批。
  * ⛔ **`KwValue` 不加这个闸** —— 原版 `GetCurrentTraitValueWithModifiers.c:1-80` 里**没有** `isRemnant` 判断（逐句读过）。
* **仍存的差异（今天零可达）**：非残骸档我们的 `_keywords` 里**也含 90 档授予的**，原版非残骸支只扫 `buffType 3`
  ⇒「挂着 90 授予的 trait 又翻回成活单位」时我们看得见、原版看不见。今天不可达：90 只在残骸上授予，
  而翻回原语会把 `_remnantTraits` 那批整本摘光。

### ②·3 `938 TransformFromRemnant` —— **原版属性从哪来：已经读到了**（并附一处与验收口径相反的裁定）

* **原版真实链路**（三份 `.c` 拼起来才闭合）：
  1. `BattleManager__AddTransformFromRemnant.c`：`SetLeavingRemnant()`（`+0x66=1`）+ 入队动作 `0x48`；
  2. `BattleManager__ResolveTransformFromRemnant.c`：`SetAsUnitTransforming` → `CardScript__TransformFromRemnant(onComplete=委托)`；
  3. `CardScript__TransformFromRemnant.c:1-19`（全文三句）：`*(undefined2*)(+0x65)=0`（**一次清两个字节**）·
     `BattleCardUI.ChangeCardToRemnant(ui, 0, …)` · `UpdateHealthText()`。**方法体里不恢复任何属性**；
  4. 委托 `BattleManager.__c__DisplayClass555_0___ResolveTransformFromRemnant_b__0.c:7-31`：
     **第一句 `CardScript__ResetToValuesInHand(card, 1)`** → `BroadcastUnitReanimated` → `SetAsUnitInPlay`
     → `ResetSummonSickness` → `ActivateTraitsOnSummonOrEnchantment` → `UpdateWhileInPlay` → `CheckStun` → `UpdateFigures`。
  ⇒ **属性由 `ResetToValuesInHand` 恢复**（`CardScript__ResetToValuesInHand.c:1-176`），不在 `TransformFromRemnant` 里。
* 🔴 **与派活单验收相矛盾（必须报告）**：派活单写「翻回后 buff/标记/教练数与翻回前**一致**（改前清零）」，
  而 `ResetToValuesInHand.c:45-70` 是**两次 `List.Clear()`**：
  ```
  lVar11 = *(longlong *)(param_1 + 0x108);   // activeEffects
  … *(undefined4 *)(lVar11 + 0x18) = 0;  System_Array__Clear(items, 0, 原 size);   // = List.Clear()
  lVar11 = *(longlong *)(param_1 + 0x110);   // activeEnchantments —— 同样 Clear
  ```
  ⇒ 原版**清掉全部 buff 与附魔**；`SetBaseCardTraits()` 只重挂**卡面那批** trait（标记类 trait 因此也没了）。
  而且**「变成残骸」那一跳也调同一个复位**：`BattleManager._ResolveTransformIntoRemnant_d__554__MoveNext.c:43`。
  **原版唯一保留的是它没碰的字段**：`companionCounter(+0x60)` / `tideCounter(+0x5C)` / `orderInPlay(+0x44)` / `isPraying(+0x67)`
  ⇒ 「**计数器**跨两次翻面保留」这一半验收**成立**（我们 = `CardInstance.CompanionCounter`，实例共享，两条路都保留），
  **「buff / 标记保留」那一半与第一权威相反** —— 我按第一权威实现（清），请调度台核这一条口径。
* **我们的现状（逐条对过）**：`EffectResolver.DoReanimate` 的残骸支是 `new UnitState(rem.Instance, false)`
  ⇒ 卡面数值/卡面 trait/每回合计数全部重来 —— **与 `ResetToValuesInHand` 的净观测量等价**
  （buff 清 ✓ · 附魔在 `CardInstance` 上、原版清、我们没清 ✗ · dropPod 两格未建 ✗ · `questCounter` 复位 ✗ 零可达）。
* **本批交付**（类尾原语，供调用点切换）：
  * `UnitState.cs:2657 ResetToValuesInHand(bool clearBacklash)` —— 逐句搬 `.c` ①～⑨（`activeEffects` 那几本 Clear ·
    `Instance.QuestCounter=0` · 数值回卡面 · `Health=MaxHealth` · `AttacksThisTurn/UsedActiveAbilityThisTurn/OathUsesThisTurn=0` ·
    摘 `jam(130)/stun(100)/blind(975)/stealth(70)` · 残骸档写 `isRemnant=0, isLeavingRemnant=1`（`.c:147-152` 的 `0x100`）·
    `BlindedAtStartOfTurn/RemoveMarkerlightAtEndOfTurn=false` · 卡面 trait 重挂）。**没建的两格如实标在 doc 里**（dropPod / `alreadyTriggeredBacklash`）。
  * `UnitState.cs:2729 TransformBackFromRemnant()` —— `ResetToValuesInHand(true)` ＋ 两个字节一起清。

### ②·4 `853 RemoveEffect` —— 按记录摘的原语（1/3/90/5/40/45 六档）

* **判据（🥇 `decomp_full/CardScript__RemoveEffect.c:1-279`，全文逐 case 对过）**：
  ① `permanentBuff(+0x74) != 0 ⇒ return`；② `activeEffects.Remove(effect)`；③ `switch(buffType)`：
  | buffType | 原版（`.c` 行） | 我们（`RemoveEffectRecord`） |
  |---|---|---|
  | 1 addStats（`:155-192`） | `currentHealth = min(currentHealth, currentMaxHealthProcessed)`；`armourChange != 0 ⇒ AddArmour(-x)` | `ClampHealthToMax()` ＋ `RemoveKeyword(armour, x)`（护甲只有关键词一本账，等价） |
  | 2 / 10 | 只 `UpdateFigures` | 无动作（无表现层） |
  | 3 addTrait / 90 addTraitRemnant（`:60-96`） | 卡面印着就留着；否则 `RemoveTraitSilently(trait)`（＋`trait==230 ⇒ RemoveDropPod`、`trait==610 ⇒ +0x358=0`） | 卡面印着就留着；否则 `RemoveAll(trait, silent:true)`。⚠️ **两条特判无落点**（前者 = `RuleCore.OpenDropPod` 那条独立机制，后者缓存在 `Aura.cs`） |
  | 4/6/7/8/9/0xB/0xD/0xE | 空档（`:196-201` 只有 `break`） | 无动作 |
  | 5 swapStats（`:202-238`） | `ChangeBaseAttack(卡面近战)` + `ChangeMaxHealth(卡面血)` + 夹血 | `AttackBase=卡面` / `MaxHealthBase=卡面` / `ClampHealthToMax()` |
  | 0xC/0xF/0x10（附魔） | 摘 `+0x110` 那条；`cardType==0x28 && spellType==0x78 && !fromJam` ⇒ `BroadcastQuestResolved` + `questCounter=0` | **不在本方法**（`Enchantment.Remove` 那一套）；`fromJam` 形参留着给它 |
  | 0x2D(45) addStatsDropPod（`:98-152`） | `currentDropPodHealth(+0xcc) -= healthChange(+0x60)`；`currentDropPodAttack(+0xe0) -= meleeAttackChange(+0x48)` | 血量那半落成 `KwValue(droppod)` 的增减；**攻击那半未建**（我们没有 drop pod 的两个字段） |
  | default 40 replaceAbility（`:262-272`） | `hasActiveAbility(+0x220) = 卡面(+0x2A9)`；`activeAbilityRef(+0x118) = null` | **净等价、无动作**：我们的 `Ability` 恒读 `Card.Ability`（没有运行时覆盖位）⇒ 已经是「重置态」 |
* **仍然缺的三处**：① **调用点**（今天没有任何一处按「一条记录」摘 —— `RevertBuffs` / `RevertGrantsFrom` /
  `Enchantment.Remove` 都是「按来源/关键词整批摘」）；② case 3 的两条特判；③ case 12 的 QuestResolved 那一支。
  池内 `buffType 5/40/45` **各 0 条** ⇒ 今天零可观察。

---

## ③ 读数 · 回归 · 指纹逐条解释

### ③·1 `play all dump` 前后比（**主档 1128 张，逐字节相同**）

```
bash 工具/ruleprobe.sh play all dump <A>     # 2026-10-26 22:40，活 A 改完、B 未开始
bash 工具/ruleprobe.sh play all dump <B>     # 22:50，B 三件全部改完
diff A B  ⇒ 0 行
```
⇒ **`UnitState` 这三件在主档夹具上零可观察**（预期：主档不造残骸、也没有 `buffType 5/40/45` 的记录）。

### ③·2 `play check` 的 4 行差异 —— 逐条

| 行 | 段 | 结论 | 归因证据 |
|---|---|---|---|
| **`BL47`** | ①-a 主档 | **本批 A 的预期结果**：`other 0→1`、`changes 4→5`，`changesFull` 多一条 `✎ 改动 P0[3] FIX_A 2/0/0/2 → 2/0/1/2 {armour1}` | — |
| **`SW25`** | ①-b 事件电池 | 🔴 **本批 B①（残骸闸）的副作用**，方向**与原版一致** | **隔离实验**：把 `UnitState.cs` 换成 `HEAD` 那一版（只这一份，其余源码保持当前）、用同一份探针源码另编一份跑 `play all pf` ⇒ **整表 1714 行里只有 `E\|SW25` 一行不同**（其余逐字节相同）⇒ 这一行**百分之百是本批 `UnitState` 改的**。机制：该夹具里那个带 `pack` 的**残骸**改前 `Has("pack")=true`（→ `FieldAttack` 的 pack 加成 / `HasWhileInPlayAbility` ⑤ 为真），改后残骸档只读 90 档那一本 ⇒ `false`。**原版同口径**：`HasCurrentTrait` 残骸支只扫 `buffType 90`，而 `pack` 池内是 `buffType 3` ⇒ 原版也看不见。 |
| **`BL18`** / **`UM18`** | ①-b 事件电池 | **既有归因 `E7P06` 接 669**（调度台已确认、本批未追） | 改前 22:39 那次 `play check` 里这两行**就已经在差异表**上 |

### ③·3 隔离实验的副产品（也是"顺手发现"）

* 同一分钟里 `RuleEngine/Core/RuleCore.cs` 被**别的写手**改过（mtime 22:52）。为了让归因不被它污染，
  隔离实验用的是「**当前** `RuleCore` ＋ `HEAD` 的 `UnitState`」对「**当前** `RuleCore` ＋ 本批 `UnitState`」
  ⇒ 两边只差 `UnitState` 一个文件，结论不受别人改动影响。
* 顺带核到：本批 `play check` 指纹从 22:39 的 `bb33e5ad…` 走到 `1b40f42a…`，**差的正好是 `SW25` 那一行**
  （22:39 的差异表是 3 行、现在是 4 行）。⚠️ `RuleCore` 那次改动的**副产物是否会在下一轮显现**，本批无法证明，如实标着。

### ③·4 复验 `E9ER` 那两条**没漂**

* `bash 工具/ruleprobe.sh play ASH73 preset tpick=1` ⇒ `P1[3] FIX_E1 2/0/0/6 → 2/0/0/1`、**对敌伤害 5** ✓（与 E9 读数一致）。
* `pool_effect_check --only 30` ⇒ `对=381 · 错=0`，`BL47` 判 **对** ✓。

### ③·5 离线读数口（新开，不在仓库里）

`UnitState` 的三件在主档/电池夹具上大多不可达（要造残骸、要 `buffType 5/40/45` 的记录），
所以另起了一个**临时探针**（`/tmp/e10tb_probe`，编法照 `工具/ruleprobe`：`Core/*.cs` ＋ 四个 `Data` 文件 ＋ `Stub.cs`），
直接摆 `UnitState` 调那几个原语，**26 条断言 0 失败**（①7 条 + ②11 条 + ③8 条）。
⚠️ 它只在 `/tmp`，**没有进仓库**（不占账、不占断言总量）；要长期保留请调度台定。

---

## ④ 改动清单 ＋ 改动请求

**改了什么（两个文件，共 +291/−1 与 +17/−3）**

| 文件 | 落点 | 内容 |
|---|---|---|
| `RuleEngine/Core/EffectText.cs` | `:5046` | 新增 `ReAdditionalForEachReversed`（名词在前的追加型 for-each） |
| 同上 | `:4905` / `:4918` | `TryForEach` 先试原正则、不中再试反向那条；组 1/2 按 `reversed` 取 |
| `RuleEngine/Core/UnitState.cs` | `:968` | `Has` 的**残骸闸**（残骸档只读 `_remnantTraits`） |
| 同上 | `:1157` / `:1286` / `:2584` / `:2707` | `_remnantTraits` 的写/摘/清（`AddKeyword` / `RemoveAll` / `ClearAllKeywords` / 翻回） |
| 同上 | `:2620` | `readonly HashSet<string> _remnantTraits`（类尾；`Has` 早于它声明，C# 合法） |
| 同上 | `:2657` | `ResetToValuesInHand(bool)` —— 原版 `CardScript.ResetToValuesInHand` 等价位 |
| 同上 | `:2729` | `TransformBackFromRemnant()` —— 原版 `TransformFromRemnant` ＋ 上面那条复位 |
| 同上 | `:2744` / `:2792` | `EffectRecord` 类 ＋ `RemoveEffectRecord(...)` —— 原版 `RemoveEffect` 等价位 |

🔶 **本批新增的唯一一处「自己设计」标记**：`RemoveEffectRecord` 的 case 45 那行注释就地改写成了
`🔶 自己设计（非原版）：drop pod 血量用 KwValue(droppod) 表达 …`（**零行数变化**，铁律 18·b ——
该映射是 `CardDef.cs` 既有的模型口径，本批只是延伸引用，未新增口径）。

**改动请求（白名单外，⛔ 我没碰）**

1. **`RuleEngine/Core/EffectResolver.cs` 的 `DoReanimate`（现读 `:15315` 一带）**：
   两处 `new UnitState(rem.Instance, false)`（场上残骸支；另一处是墓地退路）应改为
   `rem.TransformBackFromRemnant()`（**原地翻回**）。收益 = ① 把「翻回 = 复位成手牌数值」这条规则收进
   `UnitState` 一处（现在 EffectResolver 必须自己记得 `DeployedTurn`/`PlayOrder`/`ApplyDeployTurnState` 三件）；
   ② 顺手补上今天缺的那一格：`Instance.QuestCounter` 与附魔（原版 `ResetToValuesInHand` 会清 `activeEnchantments`）。
   ⚠️ 切换时**保留**现有那三句（`DeployedTurn = ctx.Turn` · `PlayOrder = ctx.TakePlayOrder()` · `RuleCore.ApplyDeployTurnState(back)`），
   因为它们对应的是原版回调里 `SetAsUnitInPlay`/`ResetSummonSickness`/`ActivateTraitsOnSummonOrEnchantment` 那三跳。
2. **同上：残骸**创建支**（`RuleCore.CleanupDeaths` 的 `u.IsRemnant` 那一支）** —— 原版「翻成残骸」也会先
   `ResetToValuesInHand(card, 0)`（`BattleManager._ResolveTransformIntoRemnant_d__554__MoveNext.c:43`），
   我们还额外多做一步（`ClearAllKeywords`）。若要让两侧形状一致，可在那支里改调 `ResetToValuesInHand(false)`；
   ⚠️ **这不影响观测量**（我们新造对象本来就全清），属形状收敛，优先级低。
3. **`RemoveEffectRecord` 的调用点**：谁先落地都行，建议顺序 = ① `EffectResolver` 里「摘一条直到回合末的修饰」
   那条路（`RevertBuffs` 现在是整批撤）→ ② `Enchantment.Remove` 的附魔支（case 12 那一档 + `fromJam`）。
   池内 `buffType 5/40/45` 各 0 条 ⇒ 这两步**不改任何今天可观察的行为**。

**账**：`账/_parts/B9_E10TB.tsv` —— 首行 11 列表头，共 **7 行**（3 行本批三件 ＋ **4 行落点重指**）。
⚠️ 那 4 行（`ClearEndOfTurnEffects` / `HasWhileInPlayAbility` / `HasWhileInPlayAdjacentEffect` / `GetCurrentTraitValueWithModifiers`）
是**本批插行顶掉的**，`check_ledger_points` 把它们判成 **「失败」而不是「漂移」**（落点落到注释行/语句中间），
`--fix` 修不了 ⇒ 已由补丁重指。**其余纯行号漂移**（`Has` 942→968 · `KwValue` 1074→1104 · `AddKeyword` 1090→1120 ·
`SyncKeywordState` 1202→1237 · `RemoveAll` 1247→1282 等）交给调度台的 `check_ledger_points --all-A --fix`。

---

## ⑤ 没查清 / 留给下一轮

1. **`BL47` 修好后 `armourValueOptions 201` 那一半没有独立验证**：`play BL47` 的夹具里 Dark Pact 层数 = 0
   ⇒ 只验到「基础那一份」（+1）。「1 + n」的 n 那一半要等**带 Dark Pact 的夹具**（`BL43` 同样问题）。
2. **`8f2da557…`（`plus an additional 1 for each friendly troop`）**：`and`→`plus` 两条正则都不认；
   今天走的是别的支（目标短语正常、`perCount=0`）—— 是否另有缺陷**没查**（不在本批白名单，先记在 §二·1 末）。
3. **`RemoveEffectRecord` 的 case 3 两条特判**（`trait 230 ⇒ RemoveDropPod` · `trait 610 ⇒ +0x358=0`）：
   落点分别在 `RuleCore.OpenDropPod` 与 `Aura.cs`，不在本批白名单 ⇒ **未做**。
4. **drop pod 的两个字段**（`currentDropPodHealth/Attack`）：我们只有「血量 = `KwValue(droppod)`」这半个模型，
   攻击那一格**没有落点** ⇒ case 45 的攻击那半**未建**（如实标）。
5. **翻回路径的附魔清除**：`CardInstance.ActiveEnchantments` 在翻回时没清（原版清 `+0x110`）。
   归调用方（`Enchantment` 侧），本批只在 doc 与账里标着。
6. **`SW25` 那一行变化后的最终态是否符合原版逐拍**：本批只证到「改前/改后差在哪、为什么原版的 `pack` 在残骸上不可见」；
   `net 26→27` 那一步的**逐动作次序**要拿原版实况（本机可跑原版）才能坐实 —— 引擎侧判据已经够，实录未做。
