# 执行代理 `A3` · `RuleCore.cs` 四件（第八十七会话 · `.cs` 攻坚波）

> **代理**：A3（有写权）· **独占文件** = `Unity/MyGame/Assets/RuleEngine/Core/RuleCore.cs`
> **台账补丁**：`账/_parts/B87_A3_RuleCore.tsv`（6 行 · 11 列表头逐字照主表 · `域`=`战斗逻辑驱动` · `轮次`=`B87-A3`）
> **机检**：`check_ledger_points.py` ⇒ **A=2 通过=2 漂移=0 失败=0**；`merge_ledger_parts.py --dry --only B87_A3_` ⇒ **更新 6 / 新增 0**；`check_patch_conflicts.py` ⇒ **无跨补丁重复键**
> **一句话**：四件里 **3 件做完**（主动能力入口 / 召唤发起义 / 照抄原版白扣），**1 件停手**（oath 付费闸 —— 落点在别人独占的 `EffectResolver.cs`）。

---

## ① 四件各自改了什么

### 1 · `SOR66` 主动能力入口（原版**通用那一格** `activeAbilityLogic`）—— 做完

判据 = `资料/完全复刻/判据/SOR66主动能力_判定.md`（本代理先整份读过）。新增成员**全部追加在 `RuleCore.cs` 类尾**（⛔ 没往中间插，下游账行零漂移）：

| 成员 | 行 | 做什么 |
|---|---|---|
| `CanStartActiveAbility(ctx,p,slot)` | `RuleCore.cs:15297` | 闸（**不判目标**）：`HasActiveAbility==1` · 自己回合 · 在场（`!IsRemnant`）· `!IsStunned` · `!Exhausted` · `duty` 用过就拒 · 有 `oath` 还要 `CanUseOathAbility` · 费用够。**⛔ 没有卡型/关键词要求**（判据 `.c`） |
| `CanUseActiveAbility(ctx,p,slot,targetSlot)` | `:15331` | 上面那条 ＋「要选目标的那一族」的目标闸（候选合法性转发 `CanUseAbilityTarget`，⛔ 不另写一份） |
| `UseActiveAbility` / `UseActiveAbilityCore` | `:15352` / `:15358` | 一次完整动作：**先算「能跑哪几行」→ 费用 → 动作经济 → `EvtKind.Ability` → 逐行执行** |
| `ResolveActiveAbilityRows` | `:15489` | 这一次该用哪一组行（见下面「数据侧优先」那条） |
| `ActiveAbilityNeedsPick` | `:15521` | 要不要先点目标（原版只有 `ta == 30 / 220` 会读 `param_5`） |
| `ActiveAbilityBodyOps` | `:15543` | 取这张卡被 `ApplyActiveAbilityLogic` 标记过的那几条正文 op（`EffectOp.ActiveAbilityRow >= 0`） |
| `ActiveAbilityRowIntOf` | `:15564` | 逐行数组取一格（`CardDef.ActiveRowInt` 是私有的，取不到） |
| `_activeAbilityCostNoted` / `NoteActiveAbilityCostUnknown` | `:15569` / `:15576` | 「主动能力费用未知」的出声口（每张卡一次，走 `Debug.LogWarning`，⛔ 不进 `ctx.Log`——那会动指纹） |

**两个 op**（这是本笔唯一实现的两种 `chosenAbility`）：

* `buffOthers(30/31)` —— **不另写数值**，直接复用这张卡正文层那几条 op（SOR66 = `Rally:` 的 `give +2 Ranged / this turn`），以**玩家点中的那个友方单位**为 `chosen` 调 `ResolveOps`。`ta == 20 (self)` 的行传 `null`（原版 `GetTargets` 对 `self` 直接放 thisCard）。
* `triggerPray(462)` —— 三件事、顺序照 `ResolveTriggerPray.c:217 / :266 / :314`：`FireTriggerAt(Pray)` → `u.Prayed = true` → `BroadcastWhen(Prays)`。
  🔴 **⛔ 不调 `Enchantments.AttachPray`**（那张附魔是卡自己的 `cardAbilities[trigger=655]` 挂的；SOR66 没有 655 行 ⇒ 走了就会**多挂一张原版不会挂的附魔**）。

**三条设计取舍（都写在代码注释里）**：

1. **「先算清能跑哪几行，再动状态」** —— 一条都跑不了时**不置 `Exhausted`、不发 `EvtKind.Ability`**，直接 `ErrUnimplemented(13)` + 出声。理由 = `UseOathAbility` 2026-10-17 修过的那一类「宣布发动了却没结算」。（实测见 ② 的 `AM12` 读数。）
2. **选支改成「数据侧优先」** —— 原版靠 `hasAlternativeAbility(+0x2AA)` + `normalAbilityBoardCriteria(+0x2B0)`；**这两格我们的卡表都没有**（`hasAlternativeAbility` 现读：1129 张里只有 **1 张**带这个键）⇒ `UsesAlternativeAbility` 恒 `null`。硬按「判不出就拒」会让这条路径**整体做死**（第一版就是这样，三条断言全 ❌）。改成：`ab` 那一列（`-1` active / `-2` alternative）**只有一组有行**时直接用那一组（结构上没有第二种可能）；**两组都有行**时才问运行期闸，仍 `null` ⇒ 不猜、`ErrUnimplemented` + 出声。
3. **费用**：`ActiveAbilityManaCost` 等四列**卡表 v35 还没出**（现读 `cards_engine.json` 无该键）⇒ `ActiveAbilityCostKnown == false` ⇒ **不收费 + 出声一次**，注释写死「⛔ 这不是『原版免费』」。

**连带改了一处断言表**：`Editor/RuleEngineTest.cs` 有两处 `CountOf(… "EvtKind.Ability")` 的结构断言（`.cs` 里发出点计数）。本笔新增第 4 个发出点 ⇒ 就地改 `3→4`（两文件合计）与 `4→5`（`Core/` 全目录合计），并按该处既有写法登记「第 4 个发出点 = `UseActiveAbility`」。**零行数变化**（就地改写那三行与一行注释）。

### 2 · oath 付费闸（`NeedsToChooseFromPool` / 1275）—— **停手**

`ChooseCardCandidates` 的物理位置 = **`RuleEngine/Core/EffectResolver.cs:4553`**，而简报点名「别人在改 `EffectResolver.cs`」⇒ 按文件所有权**不动**。判据已复核、改法已写清 ⇒ 见 ③。

### 3 · 召唤路径发起义广播 —— 做完

`RuleCore.cs:4968`（`DeployFreeCore` 里，`if (fromHand) FireTriggerOnBoard(Rally)` / `if (!reanimated) ResolveFaithAbility(…)` **之后**、同一个 `{ }` 块内）：

```csharp
if (!reanimated)
    FireTriggerOnSide(ctx, owner, KeywordTable.Uprising, unit, requireDeployedThisTurn: true);
```

判据（`CardScript__ResolveUnitSummoned.c:82-105`，本笔亲读全文）：这一跳在 `ResolveUnitSummoned` 里是**无条件**的（在 `cardState != 5` 那三层 `if` **之外**，也**不受** `shouldTriggerSummon` 影响），而 190/195/605 早就在同一条函数上接好了 ⇒ 起义是同一函数更靠后的那一跳。位置与 `.c:44 / :51 / :82` 的先后一致。
⚠️ 与 `Rally`（195）**不同**：Rally 受 `shouldTriggerSummon` 管 ⇒ **别把这一句挪进 `if (fromHand)`**。
⚠️ 再造（`reanimated`）**不发**（那条链根本不调 `ResolveUnitSummoned`，与 195/605 同一条理由）。

### 4 · 改回照抄原版的白扣 —— 做完

`RuleCore.cs:14480`（`DeclareAttackRefundingForced`）：删掉「候选不合法 ⇒ 把预扣原样退回」那一句，改成**只在 `code == OK` 时**才走 `RefundForcedAttackAction`。上方那段长注释（`:14456-14462`）里「**唯一不照抄的一格** ⇒ 我们改为失败即退」**就地订正**（铁律 5），改成「原版白扣 ⇒ **我们照抄**，⛔ 别再修好」。

判据（本笔现读 `decomp_full/BattleManager._ResolveForceAttack_d__504__MoveNext.c`）：`:34` `AI.SelectBestAttackAction` 拿不到 / `IsValidAttackTarget` 判不过 ⇒ **直接 `goto LAB_1809b42e5` 收尾**，`:73` 那条 `if (extraValue /* wasFree */) AddResetActions` **永远排不上** ⇒ 入队时 `RemoveAttackThisTurn` 扣掉的那一点没人还。而 `RemoveAttackThisTurn` 只在 `>0` 时 `--` ⇒ 「攻击前 0 次」那一档白扣不可见、`>0` 那一档真丢 1 次。多候选时连扣多次**同样是照抄**（原版对每个候选各 `AddForceAttack` 一次）。

### 附带（为了让第 1 件**能量出来**，动了两个探针文件）

* `工具/ruleprobe/Program.cs`：`LoadPool` 里补 **v21 `hasActiveAbility` ＋ v23 `activeAbilityLogic`** 两列的映射（＋类尾 `A3IntArray` 小工具）。**探针原来一个都没传** ⇒ `CardDef.HasActiveAbility` 在探针里恒 `-1`、`ActiveAbilityLogic` 恒 `null` ⇒ `CanStartActiveAbility` 恒 `ErrNoAbility(14)`，新引擎口**结构上量不到**（实测第一版 `play SOR66 preset aa=1` 读到 `rc=14`）。
  口径与引擎 `CardDatabase.Parse` 一致，**只有一处故意不同**并写在注释里：`hasActiveAbility` **缺键 ⇒ `0`**（引擎侧 DTO 是裸 `public int`，JsonUtility 缺键就是 `0`）——若按「缺列 = 未知」读成 `-1`，两边对同一份表会得出**相反**结论。
* `工具/ruleprobe/PlayProbe.cs`：(a) 电池第 ④ 步后补一格 `aa=<rc>@tgt<slot>/rng±N/pray0|1/exh0|1`（**只对 `hasActiveAbility == 1` 的卡打**；按**实例**找格号，⛔ 不按 `slot`）；(b) 新夹具 `preset aa=1|2` → `RunActiveAbility`（`aa=1` 打自己，`aa=2` 打**己方督军**以验「点谁打谁」）。

---

## ② 验收读数

### 编译

* `TMPDIR=/tmp/wf_a3 bash 工具/typecheck.sh`：**我这一侧 0 错**。运行时会**间歇**报别的写手的半成品（本轮实测出现过 `BattleScene.cs` / `EffectResolver.cs`(DoExchange 一族) / `ScoringEvent.cs` / `ExpansionPassEventWindow.cs` / `ChatTab.cs` / `MainMenuRuntime.cs`）—— 每次都是**别人的文件**、且编辑器程序集恒 `0`；重跑即变。收工前最后一次重试时剩下的是 `CardPresentation/Shell/{ChatTab,MainMenuRuntime}.cs` 的 `SocialData.ChatMessage` 没有 `Channel` 成员（那是别人的一次正在进行的重构）⇒ 与本笔无关。`ruleprobe.sh` 的 `Core/` 编译每次都过（它把 `RuleEngine/Core/**` 整包编了一遍）。
* ⚠️ **没有跑过任何 Unity 批处理**（红线）。

### 第 1 件：`bash d:/4/Unity/工具/ruleprobe.sh play SOR66 preset aa=1`

```
  被测单位    : SOR66 · Armorium Cherub（打出落在第 2 格）
  数据        : `hasActiveAbility`=1 · `activeAbilityLogic` 行数=1 · `ActiveAbilityMap`=attached
  闸          : `CanStartActiveAbility`=0（OK） · `CanUseActiveAbility(tgt=self)`=0（OK）
   ① 目标远程（第 2 格 Armorium Cherub）：`RangedAttack` 2 → 4（净 +2）  **✅ +2**
   ② 发动者祈祷：`Prayed` 假 → 真  **✅**
   ③ 发动者已行动：`Exhausted` 假 → 真 · `AttacksThisTurn` 0 → 1  **✅**
   附：`Attack` 净 +0 · `UseActiveAbility` rc=0（OK）
```

**三条硬断言全过。** 日志侧同时看得见 `（按原版 targetCriteria 36 格筛（(ab=-1, lg=0)）：5 → 1）` ⇒ 36 格 criteria 真的参与了（筛到玩家点的那一个）。

`preset aa=2`（目标改成**己方督军**，= 和施放者不同的那个单位）：

```
   ① 目标远程（第 4 格 FIXWarlord_Sororitas）：`RangedAttack` 0 → 2（净 +2）  **✅ +2**
   ② 发动者祈祷：`Prayed` 假 → 真  **✅**
   ③ 发动者已行动：`Exhausted` 假 → 真 · `AttacksThisTurn` 0 → 1  **✅**
```

⇒ **「点谁打谁」也验了**（附录：这时 SOR66 自己的 `RangedAttack` 不动，只有被点的那一个 +2）。

未实现档（`chosenAbility` 本版没实现）**不静默、也不花行动** —— `play AM12 preset aa=1`（`ca=10 doDamage`）：

```
   ③ 发动者已行动：`Exhausted` 假 → 假 · `AttacksThisTurn` 0 → 0  **❌**
   附：`UseActiveAbility` rc=13（ErrUnimplemented）
   ⚠️ Flamer Trooper 的主动能力**这一版一条都跑不了**（chosenAbility=10（ta=10）） ⇒ **行动没有花掉、也没有宣布发动**
```

### 第 3 / 4 件：**没有探针读数（如实记）**

* 第 3 件：`play` 电池的 `troop2` 走的是 `PlayCard`（`DeployFixture` 里 `RuleCore.PlayCard`），**不是** `DeployFree` ⇒ 结构上抬不到「效果召唤」那条入口。代码判据已逐行核过（见 ①），但**读数 = 无**。
* 第 4 件：电池不构造「强制攻击目标不合法」这一态 ⇒ 同样**没有读数**；改动本身是「删掉一句退回」＋注释订正（`play all` 主档 1129 张逐字节不变，见下）。

### 全池对拍：`play all check`

```
  --- ①-a 逐卡差异（主档 · 打出去那一拍）---
     ✅ 没有一张卡与基线不同（1129 张逐字节一致）
  --- ①-b 逐卡差异（事件电池）---
     🔴 76 张卡变了 · 新增 0 · 消失 0
```

* **主档逐字节不变** ⇒ 第 3 / 4 件与主动能力入口**没有**改坏任何一张卡的「打出去那一拍」。
* 电池那 76 张**全部由新增的 `aa=` 一格引起**：55 张只多了这一格文本（都是 `aa=6` = `ErrExhausted`，因为 `abil=` / `alt(...)` 已经把本回合那次行动花掉了 —— 原版同一个按钮也只给一次）；其余 21 张因为在 `V3` 变体里那条主动能力**真的发动了**（行动还没被花掉）⇒ 后续攻击步骤与净差跟着变（例：`AM23` 的 `netDmg 1 → -2`，是它的主动能力真打了敌方督军）。
* 🔴 **基线现在与实现不一致，需要在「没有 `.cs` 写手 + 补丁已合并完」的重钉窗口里重跑 `play all pf`**。本代理**没有**重钉任何 `Pinned*Md5`。

---

## ③ 没做的 / 做不到的

### 3·1 第 2 件（oath 付费闸）—— 落点不在我的独占文件里

* 位置：`EffectResolver.ChooseCardCandidates`（`RuleEngine/Core/EffectResolver.cs:4553`，`partial class RuleCore` 但**物理文件**是 `EffectResolver.cs`）⇒ 简报点名「别人在改 `EffectResolver.cs`」⇒ **停手**，没有改动它一个字节。
* 判据（本笔读 `decomp_full/CardScript__NeedsToChooseFromPool.c` 全文 153 行，**比 R21 记的更清楚两处**）：
  * ① 开头 `rawCardScript.activeAbility(+0x2A0) == null` 或它的 `count == 0` ⇒ **直接 `return 0`**；
    `cardType == 10 (Hero) && card+0x221 != 0` ⇒ **`return 0`**；然后是「遍历 ability 里 `chosenAbility == 0x5a (90)` 的行 ⇒ `FilterMethods.CheckIfMeetsCriteria`」那一段（R21 记的 ①）。
  * ② 尾部（R21 记的 ②）：**`!HasDefaultTrait(0x4fb = oath)` ⇒ `return 1`**（= **没有 oath 的卡总是需要选池**）；有 oath 时再过分/回合并闸，最后 `GetManaLeft(...) >= GetDefaultTraitValue(0x4fb)` ⇒ **`return 1`**，否则 `0`；中间还有 `HasCurrentTrait(0x82 = 130)` ⇒ `return 0` 的一档。
  * ⇒ **落点应当是「谁决定『打这张卡要不要开选池面板』」，而不是 `ChooseCardCandidates` 本身**（后者是「候选有哪些」，是纯读）。我们这边的等价物在 `EffectResolver.PlayAsks` / `PlayerChooseOps` 那一带（R21 已核过：那两处都没这一档）。
* 可直接落的一处改法（给下一个有 `EffectResolver.cs` 写权的代理）：
  * 新增（可在 `RuleCore.cs` 加，也可直接内联）：`bool NeedsToChooseFromPool(ctx, owner, card)` = `card.HasActiveAbility` 那几条前置 + `card.OathOps.Count == 0 ? true : (ctx.Players[owner].Energy >= card.OathCost && !u.HasFx(KeywordTable.<130 那一位>))`。
  * ⚠️ `0x82 = 130` 那个 trait **我们还没映射成名字**（`DefinedTrait` 里没有它的语义注释）⇒ 落地前必须先查它是什么，⛔ 不许拿「不管它」蒙混（会多判「需要选池」）。

### 3·2 表现层（玩家点不到那颗钮）

`BattleDriver.OpenCommand` 的 `skill` 那一格（`HasAbility || alt || oath`）还没并进 `Card.HasActiveAbility`，按钮目标侧判定同理 ⇒ **引擎通了、UI 还没接**。`BattleDriver.cs` 不在本代理白名单。

### 3·3 主动能力的**费用四列**

卡表 v35 现读**没有** `activeAbilityManaCost / PsychicCost / PlasmaCost / Targeted`（生成器那一半没做）⇒ `AttachActiveAbilityCost` 全 `-1` ⇒ 不收费 + 出声一次。判据文件 §4·1 的「数据段」仍然待做。

---

## ④ 顺手发现（只报不改）

1. 🔴 **`play` 电池第 ④ 步按【原始格号】回读被测单位** ⇒ 被测卡换格 / 回手之后量到的是**别人**。实证：`SOR66` 卡面自带「回合开始回到手牌」，电池过完来回之后 `Board[slot]` 已经是 `FIX_WAY`，于是 `abil=14`（`ErrNoAbility`）与 `atkM=21`（`ErrNoMeleeAttack`）其实是**夹具部队**的读数。本笔只给自己新加的那一格做了「按实例找格号」，`abil=` / `atkM` **没动**（动了会改掉全池读数）。建议另开一笔按实例找。
2. 🔴 **卡表缺 `hasAlternativeAbility` 列**：现读 1129 张里只有 **1 张**带这个键 ⇒ `CardDef.UsesAlternativeAbility` 全池几乎恒 `null`，而它是「主动能力走普通支还是替代支」的**唯一运行期闸**。今天不致命（绝大多数卡只有一组 `activeAbilityLogic` 行），但这是**数据侧真缺口**：一旦有卡两组行都在，我们就只能「不猜」（`ResolveActiveAbilityRows` 会 `ErrUnimplemented`）。
3. ⚠️ **`hasActiveAbility` 只在非 0 时写键**（1129 张里 87 张带、其余缺键），引擎 DTO 是裸 `public int` ⇒ 缺键 = `0` = 「没有」。**口径自洽但脆**：任何人把 DTO 改成 `MissingIndex` 默认，全池就会从「没有」翻成「未知」（`ConditionEval` 190/280 跟着从「确定假」变成「判不了」）。建议生成器逐卡写全，或在 DTO 那行钉一条注释。
4. ⚠️ **`Editor/RuleEngineTest.cs` 同时被另一个代理改**（`status` 里那段：`pool.Count 1128→1129`（`A1a` 收进 `DA42` 附魔模板）、`abilityTargetCriteria 1616→1621`）。我的两处在 `:33126 / :33148` 附近，与它不相邻、已按零行数变化就地改；**合并/提交前请确认两边都在**（若它整篇覆盖回去，我这两处会丢，`RuleEngineTest` 会红）。
5. ⚠️ **`Uprising` 池内现读 9 张**（旧账多处写 10）—— 按 `keywords` 含 `uprising` 现扫（`GSC23/GSC34/GSC30/GSC6/GSC18/GSC8/GSC38/GSC70/GSC73`）。
6. ⚠️ `play` 电池只在 `V3` 变体里才让主动能力真跑得起来（主档 / `V1` 里 `alt(...)` 已经把行动花掉）⇒ 想量「这条主动能力到底做了什么」，**用 `preset aa=1|2`（单卡）或看 `V3:` 那一段**，⛔ 别看主档那格 `aa=6`。

---

## 附 · 改动清单（文件:行）

| 文件 | 改了什么 |
|---|---|
| `RuleEngine/Core/RuleCore.cs` | 类尾新增 8 个成员（`:15297`–`:15586`）· `DeployFreeCore` 补起义广播（注释 `:4951` 起 / 那一句在 `:4968`）· `DeclareAttackRefundingForced` 改照抄白扣（`:14480` 起）＋ 上方注释块就地订正（`:14456`） |
| `RuleEngine/Editor/RuleEngineTest.cs` | 两条 `EvtKind.Ability` 计数断言 `3→4` / `4→5`（零行数变化） |
| `工具/ruleprobe/Program.cs` | `LoadPool` 补 v21 `hasActiveAbility` ＋ v23 `activeAbilityLogic` 两列映射；类尾新增 `A3IntArray` |
| `工具/ruleprobe/PlayProbe.cs` | 电池 ④b `aa=` 一格 · `PresetSpec.ActiveAbility` / `aa=` 解析 · `RunActiveAbility`（`preset aa=1` / `aa=2`）· `RunBatterySteps` 加可选 `tested` 参数 |
| `账/_parts/B87_A3_RuleCore.tsv` | 6 行补丁（`ResolveActiveAbilityPlayed` · `ResolveActiveAbility` · `ResolveUnitSummoned` · `RemoveAttackThisTurn` · `AddForceAttack` · `ResetActions`） |
