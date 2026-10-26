# W1 —— 触发器族 × `ActionPump.cs`（账 B9 的 20 行）· 第八十三会话

> **输入** = `判据/B9_P0_判据表.tsv` 里 `目标文件` 含 `ActionPump.cs` 的 **20 行** ＋ `报告/B9_Q2_P0判据.md`。
> **产出** = `账/_parts/B9_W1.tsv`（20 行，已并入主表）＋ `账/_parts/B9_W1b.tsv`（账 166 改写）＋
> `账/_parts/B9_W1c.tsv`（账 187/603 改判 C→A）＋ `RuleEngine/Core/ActionPump.cs` 的**类尾对照段**（`:1653-1897`）。
> **判据源**（只用这三样，⛔ 没用我们自己的注释当判据）：
> 🥇 `D:/2/tools/decomp_full/**/*.c`（Ghidra） · 🥈 `D:/2/tools/il2cpp_out/dump.cs` 的 RVA 表 ＋
> **`d:/4/Unity/工具/disasm_va.py` 直读 `GameAssembly.dll` 的机器码**（VA 级） · 🥉 卡池数据
> `RuleEngine/Resources/cards_engine.json`（1,128 张）与 `cards_truth.json`（1,351 张）。

---

## 〇、这一轮改了什么口径（相对 `B9_W1` 首次交回）

| 项 | 首交 | 现在 | 触发原因 |
|---|---|---|---|
| 账 166 | `A` → `D`，理由写「我们根本没有等价物」 | **`D`，理由改成「原版有实现、本 build 出货数据 0 入口」+ 时间限定** | 调度台裁定 1（措辞落 `C` 是错的；**结论 D 保留**） |
| 账 187 / 603 | `C`（原判据说「三张卡全不同，两槽表达不了」） | **`A`** | **VA 级定案**：trigger 位那个字面量 `0` = `ThisCardPlayed`（不是哨兵）；且**调用点 `param_3 == param_4`** ⇒ `thisCard == actingCard`，只有两张卡（见 §二） |
| 账 602 | `A`（我已推翻原判据） | 不变 | —— |
| 账 607 | `A`（我已推翻原判据） | 不变 | —— |
| `SpentAllMana` 缺映射行 | 「顺手发现，待裁」 | **已补**（零行号漂移） | 调度台裁定 4；⚠️ 落点其实是 `ActionPump.cs`（不是裁定里说的 `RuleCore.cs`），见 §五 |

**20 条最终：A 15 · B 4 · C 0 · D 1。**
⚠️ **一处与 `B9_L3` 的档位分歧**：账 191 `AddTriggerPlayerCodex` 我判 `A`、`L3` 判 `B`（原版这条入队函数
**零调用点** = 死链，我已独立复核成立，见 §七·②）。若采纳 `L3` 的 `B`，合计变 **A 14 · B 5 · C 0 · D 1**。
🔴 **另：我第一版把 13 条 A 行的「我们的落点」写成了注释行 / `case` 行 ⇒ `check_ledger_points` 判失败；
已修（4 条我改、12 条让给 `B9_L3`），见 §七·①。**

---

## 一、20 行逐条结论

`改了什么` 里「注释」= 只往 `ActionPump.cs` 类尾对照段（`:1653-1897`）写，或就地改写一行注释。

| 账行 | 方法 | 已对齐 / 有缺口 | 改了什么 | 账状态 |
|---|---|---|---|---|
| **166** | `AddRemoveEnchantment` | **本 build 出货数据 0 入口**（三源，见 §三·①） | 就地扩写 `:1477` 一行注释 ＋ 类尾 `:1853-1869` 写三源判据 | **D**（原为 A） |
| **186** | `AddTriggerAmbush` | 已对齐 | 注释（表行 `:1687` + 落点 `:1786`） | A |
| **187** | `AddTriggerCardPlayedByStimulation` | **VA 定案后判已对齐**（§二·③） | 注释（`:1688` + `:1789-1816`） | **A**（原 C） |
| **189** | `AddTriggerCourtesy` | **有缺口**（整条没有；0 卡可达） | 注释（`:1689` + `:1817`） | **B** |
| **190** | `AddTriggerMob` | 已对齐 | 注释（`:1690` + `:1823`） | A |
| **191** | `AddTriggerPlayerCodex` | 已对齐 | 注释（`:1691` + `:1827`） | A |
| **192** | `AddTriggerPray` | 已对齐 | 注释（`:1692` + `:1829`） | A |
| **193** | `AddTriggerRegiment` | 已对齐 | 注释（`:1693` + `:1830`） | A |
| **194** | `AddTriggerRelentless` | **有缺口**（整条没有；0 卡可达） | 注释（`:1694` + `:1831`） | **B** |
| **196** | `AddTriggerSacrifice` | 已对齐（机制已建；0 卡可达只在备注） | 注释（`:1695` + `:1836`） | A |
| **197** | `AddTriggerSlay` | 已对齐（含出队复核） | 注释（`:1696` + `:1837`） | A |
| **198** | `AddTriggerSpiritStone` | 已对齐（**一格未定**，见 §三·④） | 注释（`:1697` + `:1840`） | A |
| **199** | `AddTriggerSynapse` | 已对齐（**但本表不该加 synapse**，见 §三·③） | 注释（`:1698` + `:1842`） | A |
| **200** | `AddTriggerTeleport` | 已对齐 | 注释（`:1699` + `:1850`） | A |
| **201** | `AddTriggerUprising` | 已对齐 | 注释（`:1700` + `:1851`） | A |
| **602** | `ResolveTriggerAmbush` | **已对齐**（原判据说「表达不了」→ 实测观测等价，§二·①） | 注释（`:1713-1721` + `:1786`） | **A** |
| **603** | `ResolveTriggerCardPlayedByStimulation` | **VA 定案后判已对齐**（§二·③） | 注释（`:1722-1737` + `:1789-1816`） | **A**（原 C） |
| **605** | `ResolveTriggerCourtesy` | **有缺口**（同 189） | 注释（`:1738-1739` + `:1817`） | **B** |
| **607** | `ResolveTriggerPlayerCodex` | **已对齐**（群体触发早已实现，§二·②） | 注释（`:1740-1747` + `:1827`） | **A** |
| **610** | `ResolveTriggerRelentless` | **有缺口**（同 194） | 注释（`:1738-1739` + `:1831`） | **B** |

---

## 二、原判据的三条「真缺口」—— **三条全部复核过，三条都不是缺口**

### ① 账 602 `Ambush`：原判据说「两槽表达不了」，实测**观测等价**

我把 `BattleManager__ResolveTriggerAmbush.c` **281 行整份读完**：

* 语义上**只有一个卡字段被读**：`param_2[2]` = `+0x10` = `targetCard`
  （`:31 if (param_2[2] != 0)` → `IsInPlayOrDying(param_2[2])` → `:226 CardScript__TriggerAmbush()`）；
* `+0x08` / `+0xD8` **没有任何语义读点**（全文 `0xd8` 零命中）；那两段 `param_2[1..0x1f]` 的批量赋值
  是 Ghidra 对「整块 0x100 结构体搬运副本」的展开（副本只用到 `local_208[0] = actionType` 传给
  `FinishResolvingAction`）；
* `:127 if (local_f8 != 0) { IsInPlayOrDying(); … }` 的 `local_f8` 是**幻影局部量**（全文件无赋值点）
  ⇒ 应就是 `targetCard`，**不是第三张卡**。

⇒ 「`casterCard` ≠ `actingCard`」在原版这条路径上**不可观测**。我们
`FireTriggerAt(attacker, Ambush, …)`（`RuleCore.cs:6267` / `:8592`）`u` 与 `subject` 都是被翻开那张
= 原版三槽同体；出队闸 `IsInPlayOrDying` 由 `abilityContext = 40`（`ambush` 数据行）承担
（闸在入队那一刻，与 `W63` 收口口径一致）。**判 A。**

### ② 账 607 `PlayerCodex`：原判据说「我们只做了第一段」，**不成立**

`RuleCore.CheckCodex`（`RuleCore.cs:12917` 起）：

* 扫 `ctx.Players[p].Board` **整排**（不是只触发第一个）；
* `2026-10-14 A1155①` 已经把旧注释里那句「② 一次只触发第一个」的 `break` **删掉了**（注释里留着错因）；
* 每轮**重读** `Board[s]`（不是快照）⇒ 原版 `:392`「不在场就跳过」由它承担；
* 「同阵营」= 只扫 `p` 那一排；「非自己」由循环自然满足；
* `BroadcastUnitCodex` 那一跳 = `FireTriggerDeferred` 尾部的 `BroadcastWhen(Triggers(codex))`
  （`RuleCore.cs:12447-12450`，只在 `only == null` 那一趟发，不会 N+1 倍）。

原版三道跳过（`:388` 同一张 / `:392` 不在场 / `:445` 阵营不同）与我们逐条对得上。
**唯一没对上的是一处次序**：原版把「那一张自己」排**最前**再循环扫全场，我们按**槽号升序**
（池内 Codex 正文不依赖次序 ⇒ 今天无可观测差异，**未验**）。**判 A。**

### ③ 账 187 / 603 `Stimulation`：VA 定案后 **也观测等价** ⇒ C → A

原判据的两句话都不成立：

**(a)「trigger 位那个字面量 `0` 可能是『跑整条 ability』的哨兵」—— 不是。**

判据链三条，从调用点一路读到过滤器：

1. **`RawCardScript.OnTrigger`（7 参重载）是纯转发器**。
   - Ghidra 的 `.c` **整份体只有 19 行**，`RawCardScript__OnTrigger.c` 就这一句实质：
     `RawCardScript__TriggerAbilities(this, trigger, mgr, thisCard, actingCard, targetCard,
      <池里取的 AnonymousType31>, 0, p7, p8, 0, 0);`
   - **VA 级机器码核对**（`工具/disasm_va.py`，VA `0x18094A9F0` / RVA `0x94A9F0`，来自
     `dump.cs:23497` 的 `// RVA: 0x94A9F0`）：
     ```
     18094AA17   mov ebp, edx      ; 第 2 实参（trigger）存进 ebp
     18094AA69   mov edx, ebp      ; 原样放回 => 第 2 实参
     18094AA9F   call 0x18094B420  ; = RawCardScript.TriggerAbilities
     ```
     **对 trigger 没有任何比较/分支**。
2. **`RawCardScript.TriggerAbilities`（VA `0x18094B420`）**把 trigger 原样传给
   `CardAbility__CanTriggerAbility(ability, trigger, …)`（见 `RawCardScript__TriggerAbilities.c:109 / :208`）；
   全文对 `param_2` 只有两处常量比较（`:54 == 0x82 Rage` / `:210 == 0xf0 WhileInPlayVariable`），
   **没有 `== 0` 的特例**。
3. **过滤器在这里**：`CardAbility__CanTriggerAbility.c:71`
   `if (*(int *)(ability + 0x10 /* triggerValue */) != trigger) return 0;`
   ⇒ `trigger == 0` = **只挑 `triggerValue == 0` 的那条能力** = `AbilityTrigger.ThisCardPlayed(0)`。

**结论：`0` 就是 `ThisCardPlayed`，不是哨兵。**（旁证：`CanTriggerAbility.c:75`
`if ((param_2 == 0) || (param_2 == 0x50)) goto LAB_180922e57;` —— trigger 0 与 80 走的是
**skip 表**那一支，跳过 abilityContext 闸；这与我们 `TriggerSkipsAbilityContext` 的 skip 表逐字一致。）

**(b)「三张卡全不同」—— 不成立，只有两张。**

调用点 `RawCardScript__OnCardPlayedWithTarget.c:56`：
`AddTriggerCardPlayedByStimulation(param_2=manager, param_5=chosen, param_4, param_3, 0)`，
而**整个这一段都在 `:19 if (op_Equality(param_3, param_4))` 分支里面** ⇒ `param_3` 与 `param_4`
是**同一张卡**（被打出的那张战术）。代入 `AddTriggerCardPlayedByStimulation`：

| 槽 | 值 |
|---|---|
| `casterCard(+0xD8)` = 形参 `param_3` | = 调用点的 `param_4` = **被打出那张战术** |
| `actingCard(+0x08)` = 形参 `param_4` | = 调用点的 `param_3` = **同一张战术** |
| `targetCard(+0x10)` = 形参 `param_2` | = 被刺激的单位 |

⇒ Resolve 侧 `OnTrigger(0, thisCard=+0x08, actingCard=+0xD8, targetCard=+0x10)` 就是
**`thisCard == actingCard`、只有 `targetCard` 不同** —— **两槽模型够用**。

**那这条动作到底做什么？** 它不是「额外再跑一次」：同一函数 `:40` 在**非**激励分支里做的正是
`TriggerAbilities(0)`；激励分支 `goto` 掉了那一句、改成排队一条 `(b)` ⇒ **净效果 = 把「这张战术自己的
效果」从同步改成排队**（并且带上 `actingCard`/`targetCard`）。

**我们的等价物**：`EffectResolver.PlayTactic` 已经就是这个次序（2026-10-22 `W62·R2` 的修正）：

* `:2557` `FireTriggerAt(ctx, st, KeywordTable.Stimulation, …)` —— 对应原版 (a)，进 **Instant** 道；
* `:2588` `EnqueueAbility(Instant, "tactic-body:" + card.Name, …)` —— 对应原版 (b)，
  跑这张战术的卡面正文（= 原版 `TriggerAbilities(0)` 那条 ability 的等价物），`chosen` 传的就是被刺激的单位。

⇒ FIFO「先激励、后战术正文」，与原始同序。**判 A。**

**残留（观测不到，如实标着）**：
1. 原版 `actingCard` 槽填**这张战术**；我们跑自己的正文不需要 actingCard ⇒ 这个区分今天没落地；
   若将来有卡的正文依赖「我是被谁触发的」，这条要重开。
2. 我们跑的是**解析出来的 op**，原版走 `TriggerAbilities(0)`（含 `CanTriggerAbility` 的 ability 级四道闸）
   —— 那属「**卡面正文到达率**」那条更大的账，不在本行。

---

## 三、其余 17 行的关键判据（都写进代码了，这里只列「不写下来就会踩」的）

### ① 账 166 `AddRemoveEnchantment` —— **本 build 出货数据 0 入口**（A → D）

措辞按调度台裁定 1：**不是「我们没有等价物」**（那会落 `C`），而是「**原版有实现、本 build 出货数据
0 入口**」，并带时间限定。

| # | 证据 | 现读结果 |
|---|---|---|
| 1 | **原版有实现、有调用方、有闸** | 唯一调用方 `AbilityLogic__PlayAbility.c:2280`（同段 `:2360`），双闸 = `abilityLogic == 0x154 (340)` ∧ `cardEffect.spellType == 0x0f (15)`。**⛔ 不是死代码。** |
| 2 | **本 build 出货数据 0 入口** | `cards_truth.json` 1,351 张的 `spellType` 分布 = 0/90/150/180/190/200/210/220/230/240/260/265/270，**没有 15**；`cards_engine.json` 1,128 张同 |
| 3 | 我们引擎没有这条动作的发射口 | 全仓 `removeEffect` 只命中 `Core/TutorialScript.cs:114`（枚举名）+ `ActionPump.cs` 的注释；`EffectResolver` 无对应 op 分派档 |

⇒ **D ＋ 时间限定 =「若数据集变化（出现 `spellType == 15` 的卡）需重开」。**

### ② `+0x08`（actingCard）写没写 —— 一条可复用的判法

16 个 `AddTrigger*` 文件里**只有 4 个写**（Mob / Pray / Synapse ＋ 账 187 的 Stimulation，它写的是 `param_4`），
其余 12 个不写。判法 = **Ghidra 局部变量声明宽度**：

* `undefined8 local_208 [2]`（16 字节，盖住 `+0x08`）⇔ **没写**；
* `undefined8 local_208`（8 字节）+ `undefined8 uStack_200` ⇔ **写了**。

16 个文件 **100% 一致**，且与各自 Resolve 侧的读法自洽 ⇒ 可直接复用到别的 `Add*` 族。

### ③ 两条「同一方法两个 actionType」

* **190 `AddTriggerMob`**：`0x50 (80)`，`isAdditionalMobTrigger` 真时**在同一方法里**重建成
  `0x51 (81)`（`:29-31`）。两条 type 在 `ResolveTriggerMob.c:218` 落到**同一个** `OnTrigger(300)`
  （差别只在 `param_3` 的一个分支）⇒ 我们**不需要**建模第二条 type。
* **199 `AddTriggerSynapse`** 的 `78` 与 **191 `PlayerCodex`** 的 `70` 是 ActionType；
  AbilityTrigger 分别是 **630 `TriggeredSynapse`** 与 **145 `Codex`** —— 两个轴，别混。
  ⚠️ `AddTriggerSynapse` 的 AbilityTrigger 是 **630**，不是 78；`199` 这一行在
  `TriggerNameOfKeyword` 里**不该加**（突触不走 `FireTriggerAt`，见调度台裁定 2 —— 我同意，理由：
  走 `BroadcastKeywordEvent(Triggers(synapse))`，630 的名字映射在
  `EffectResolver.TriggerNameOfWhenKind` 的 `TN_TriggeredSynapse`；加进来 = 0 调用点死代码 + 插行漂移）。

### ④ `SpiritStone(600)` / `TriggeredSynapse(630)` 的 `actingCard` 槽具体是谁 —— **读不到**

`CardScript__TriggerSpiritStone.c:16-18` 传的 `param_2`，在 `ResolveTriggerSpiritStone.c:266` 由
`local_30` 提供；`local_30` 在 Ghidra 体里**无赋值点**（幻影局部量）。`TriggeredSynapse` 的
`param_2` 同理。⇒ **「那一槽是哪张卡」读不到**，⛔ 不猜。今天无观测差异（这两路我们都不走
`FireTriggerAt`），将来要接三槽语义时先补判据。

---

## 四、`SpentAllMana` 缺 `TriggerNameOfKeyword` 行 —— **已补**（零行号漂移）

> 调度台裁定 4 说「落点在 `RuleCore.cs`（`A3` 正在改）⇒ 只写进报告」。
> ⚠️ **就地订正（铁律 5）**：落点其实是 **`ActionPump.cs`** —— 缺的那一行在
> `TriggerNameOfKeyword`（`ActionPump.cs:1081`），`RuleCore.cs` 那边只有**消费端**
> （`FireSpentAllMana`，`RuleCore.cs:12802` 起，`FireTriggerAt(..., SpentAllMana, ...)` 在 `:12812`），
> 消费端**一个字都不用改**。⇒ 既然是我的文件、且能零漂移，我这轮**直接补了**。

**判据**
* 原版对应物存在：`CardScript__TriggerSpentAllMana` → `OnTrigger(0x281 = 641 = AbilityTrigger.SpentAllMana)`；
  名字在 `CardDef.AbilityTriggerNumberOfName` 里本来就有（`CardDef.cs:1940`）。
* 缺口后果：`TriggerNameOfKeyword("spentallmana")` 返回 `null` ⇒ `AbilityContextOfAbilityV14(card, null, …)`
  拿不到 trigger 名 ⇒ `abilityContext` **恒走 `-1` 回退口径** ⇒ ability 级四道闸**静默不判**。
* **今天零可观测**（现读 `cards_engine.json` 1,128 张）：`abilityTriggers` 含 641 的 **0 张**、
  `desc`/`descZh` 出现 `SpentAllMana` 的也是 **0 张**。

**怎么补的（零行号漂移）**
* ⛔ 不能在 `:1084-1120` 那张 switch 里插 `case KeywordTable.SpentAllMana` —— 会让本文件下游
  **所有账落点 + 类尾整段（`:1653` 起）整体 +1**。
* ✅ 改的是**已有的一行**：`default: return null;` → `default: return ExtraTriggerName(keyword);`
  （`:1121`，行数不变）；
* ✅ 新方法 `ExtraTriggerName` **追加在类尾**（`:1894`，在**代码** §八 里），只认 `spentallmana → "SpentAllMana"`。
* 📌 **同形状的既有先例**：`WhenEvents.ExtraTriggerNameForKind`（`Core/WhenEvent.cs:2087`）——
  `TriggerNameOfWhenKind` 的 switch 之后也是这么兜底的（`EffectResolver.cs:7421`）。
* ⚠️ `backlash` **不用补**：`TriggerSkipsAbilityContext` 的**关键词**那一半已经兜住了它。

**验**：`ruleprobe check` / `play check` **逐字节不变**（见 §五）—— 与「0 张卡可达」的预测一致。

---

## 五、验

| 项 | 命令 | 结果 |
|---|---|---|
| 秒级类型检查 | `TMPDIR=/tmp/wf_w1b bash d:/4/Unity/工具/typecheck.sh` | **运行时 0 / 编辑器 0** |
| `ruleprobe check` | `bash 工具/ruleprobe.sh check` | **rc=0**（哨兵失败 0 · 全池 1121 张 · 解析差异 0 行） |
| `ruleprobe play check` | `bash 工具/ruleprobe.sh play check` | **rc=0** |
| 账落点漂移 | `python -I -X utf8 工具/check_ledger_points.py` | **`ActionPump.cs` 零漂移 / 零失败**（其余 52 条漂移是改动前就有的，全在 `RuleCore.cs` 等别的地盘） |
| 补丁冲突 | `python -I -X utf8 工具/check_patch_conflicts.py` | ✅ 无跨补丁重复键 |

**指纹变化：无。** 改动是「纯注释 + 一条 0 卡可达的映射」，`check` / `play check` 都逐字节不变
⇒ **调度台不需要重钉基线**。`git diff --numstat` = **250 新增 / 2 删除**
（2 处删除 = `:1477` 与 `:1121` 的各一行**就地改写**，行数不变）。

### 零行号漂移是怎么保证的

本文件被 `账/*.tsv` 按 `文件:行号` 引用（现读 12 处，最大 `:1606`）。所以：

* **两处就地改写**：`:1477`（`removeEffect` 零可达那行）、`:1121`（`default:` 转调）—— 行数都不变；
* **其余全部追加在类尾**（`:1653-1897`，在最后一个成员 `BoardLevelCriteriaAllows` 之后）。

⚠️ 代价如实说：那 14 条 trigger 的对照表**不在** `TriggerNameOfKeyword`（`:1081`）旁边，而在类尾
（代码 §一～§八）。代码里写清了「为什么放类尾、⛔ 别再往 `:1084-1120` 中间插行」。

---

## 六、文件

| 文件 | 改动 |
|---|---|
| `Unity/MyGame/Assets/RuleEngine/Core/ActionPump.cs` | 类尾追加 `:1653-1897`（代码 §一～§八）；`:1477` 与 `:1121` 两行就地改写。`git diff --numstat` = **250 新增 / 2 删除** |
| `Unity/资料/完全复刻/账/_parts/B9_W1.tsv` | 20 行（**已并入主表并归档**） |
| `Unity/资料/完全复刻/账/_parts/B9_W1b.tsv` | **1 行** = 账 166 改写（照裁定 1 的措辞） |
| `Unity/资料/完全复刻/账/_parts/B9_W1c.tsv` | **1 行** = 账 603 改判 `C → A`（账 187 **让给 `B9_L3`**，见 §七） |
| `Unity/资料/完全复刻/账/_parts/B9_W1d.tsv` | **2 行** = 账 602 / 607 的「我们的落点」改成**声明行**（见 §七） |
| `Unity/资料/完全复刻/报告/W1_触发器族与ActionPump.md` | 本文件 |

📌 **合并顺序**：`B9_W1`（已归档）→ `B9_L3` → `--only B9_W1b_` → `--only B9_W1c_` → `--only B9_W1d_`。
`check_patch_conflicts` 现读 **6 份补丁 · 0 条跨补丁重复键**。

---

## 七、第二轮（`W1b/W1c/W1d`）多出来的两件事

### ① 我第一版的「我们的落点」全部指错形态 —— `check_ledger_points` 判失败

`B9_W1` 并入主表后，那个闸立刻红出 **13 条属于我的行**，原因一模一样：
**落点指向的是注释行 / `case` 行，不是声明行**。闸的判据（`check_ledger_points.py` 文件头第 1 条）是
「那一行**既不像声明也不是非空数据行**（注释行 / 空行 / 语句行）」⇒ 失败。

* 例：`ActionPump.cs:1113（KeywordTable.Ambush）` —— `1113` 是 `case KeywordTable.Ambush: return "Ambush";`
  （语句行）⇒ 失败；`ActionPump.cs:1712-1736（类尾，代码 §三）` —— `1712` 是注释行 ⇒ 失败。
* ⚠️ **闸只检 `状态 = A`** 的行 —— 所以这 13 条里 166(D)/189·194·605·610(B)/187·603(C) 当时**没被红**，
  一改成 A 就会红。

**修法（两个方向）**
1. **我这 4 条**（166 / 602 / 603 / 607）的落点改成**真实声明行**：
   `Enchantment.cs:269（Remove）` · `ActionPump.cs:707（AbilityContextOfAbilityV14）` ·
   `ActionPump.cs:1136（TriggerSkipsAbilityContext）`；
2. **同族其余 12 条**（`AddTrigger*`）**让给 `B9_L3.tsv`** —— 它把落点修到了**方法级**
   （`RuleCore.cs:12232（FireTriggerAt）` / `EffectResolver.cs:2408（PlayTactic）` /
   `EffectResolver.cs:14687（DoTriggerAbility）` / `EffectResolver.cs:2720（RepeatTacticOnAdjacent）` /
   `RuleCore.cs:13042（FireTriggerOnSide）`），比我的 `TriggerNameOfKeyword` 指针**更准**
   （我那个只是「关键词→trigger 名」的映射口，不是这些 `Add*` 的等价实现）。
   ⇒ **`W1d` 故意不碰这 11 行**，⛔ 别用 W1d 覆盖 L3。

**已用「模拟合并」验过**（把补丁按序套到主表的**临时副本**上跑闸，`_tmp_view` 用完即删）：

| 套了谁 | 我那 4 行的结果 | 全表失败数 |
|---|---|---|
| 只套 `W1b/W1c/W1d` | **全过** | 21（其中 11 条是我的 `AddTrigger*` 旧落点，等 L3） |
| 再套 `B9_L3` | **全过** | **10**（剩下的 10 条**没有一条是我的**；`ActionPump.cs` 零命中） |

### ② 🔴 顺手核出：`AddTriggerPlayerCodex` 原版**零调用点**（我把这一行让给了 L3）

`B9_L3` 把账 191 `AddTriggerPlayerCodex` 判成 `B`，理由「原版这一条入队函数零调用点」。
我独立复核 —— **成立**：

* `decomp_full/` 全树按名检索，只有它自己的定义 `BattleManager__AddTriggerPlayerCodex.c`，**0 个调用点**；
* `cpp2il/out_cpp2il_dev_ilspy/BattleManager.cs:38747` 也只有定义；
* 而 `ResolveTriggerPlayerCodex` **有**调用点（`BattleManager__ResolveAction.c`），
  也就是说**活的那条是 `AddTriggerCodex`（type 0x45）→ `ResolveTriggerCodex`**，
  `AddTriggerPlayerCodex`（type 0x46=70）这条入队口是**死链**。

⇒ 我原来给账 191 判 `A` 的依据（「两槽够用 + 群体触发已实现」）**本身没错**，但**漏了这个更硬的事实**。
按铁律 11 的既有先例（`996 ResolveWeaponKilled` 零调用点 ⇒ `D` + 时间限定），这一条的档位
**A（不差）· B（死链没建）· D（死代码）三种写法都说得通** —— 我**不抢这一行**，交给调度台按 `L3` 的
`B` 收，并在账里保留「零调用点」这个硬事实。
⚠️ 真正**活**的那条（账 607 `ResolveTriggerPlayerCodex`）我仍判 **A**，判据见 §二·②。
