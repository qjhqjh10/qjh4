# `W` · 解析/结算族四笔（`A1407` / `A1388` / `A1414` / `A1415`）—— 执行写手交件

> 第十四会话（2026-10-10）· 执行代理（写手）报告。
> ⛔ 没跑 Unity、没动 git、没改正本；**只改了白名单里的两个文件**：
> `RuleEngine/Core/EffectText.cs`（`A1414`）· `RuleEngine/Core/EffectResolver.cs`（`A1407` / `A1388` / `A1415`）。
> **本文件是我唯一写过的新文件**（⛔ `CardPresentation/**` / `工具/**` / JSON / `RuleEngine/Editor/**` / `RuleCore.cs` /
> `CardDef.cs` 一个字节没碰 —— `git status` 里那些 `M` 是别的写手的）。
>
> **验收**：`TMPDIR=/tmp/wf_esc bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 错 / 编辑器 0 错**（跑了 3 次）。
> **额外验法（本笔独有）**：
> ① **离线驱动真跑**（`D:/tmp/wf_esc/`，只**读**工程源码，两个 `csproj` 各编一份「改前快照 / 工作区」）
> ② **全池解析签名对拍**：2147 条文本（1126 张卡的 `desc` ＋ 所有 `keywords` 原文）逐条比
> ⇒ **`diff` 0 行**（见 §五）。
>
> ⚠️ **本笔所有读数都是「夹具直调」**：走 `RuleCore.NewBattle` / `PlayTactic` / `DeclareAttack` 这些
> **引擎边界**，**没有**经过 `BattleDriver.LocalAct(...)` / `SimpleAI.ExecuteAction(...)` 那两个记账口
> ⇒ 那些局面**不会被本地录像录到**。本笔也不为此负责（它只为了拿读数）。
> ⚠️ **它不替代** Unity 下的 `RuleEngineTest.Run`（那条由调度台在同步点跑）。

---

## 一、`A1407` —— 无主语支在「施放者已死」时静默退回「己方全体」

### 1·1 结论

**做了，判据满足** —— 但**产物与 A 表描述的不完全一样**，如实写清：

- ✅ **修好了 A 表点名的那件事**：那一支**不再静默换成「己方全体」**。
  `source != null`（只是死了）与 `source == null`（战术卡那条路）现在**分开**；
  前者把**施放者自己**选成唯一目标，且**出声**（原来那一支一行日志都没有）。
- 🔴 **但数值上仍然看不见效果**：离线实测改后目标**正是**施放者自己（日志：「给了 1 个目标」），
  可那 10 个动词 handler 的逐目标循环都写着 `if (t == null || !t.IsAlive) continue;`
  ⇒ 已死的目标**被静默丢掉**。⇒ `Sacrifice:` 的 `+1` 从「**错加到督军身上**」变成「**谁都没加上**」。
  **这一条我没顺手改**（理由见 §1·4），已单列成新账线索 → 文末「顺手发现 1」。

### 1·2 回原版查到的判据（**本笔第一件做的事**）

**查到的（有机器码级证据）**：原版 `TargetsAffected` 的**唯一** resolver
`d:/2/tools/decomp_full/AbilityLogic__GetTargets.c` 里，**`self` 与 `actingCard` 这两档【不判生死】**：

| 档 | 地址 | 机器码行为 |
|---|---|---|
| `self = 20` | `AbilityLogic__GetTargets.c:184-191` | `if (iVar6 == 0x14)` ⇒ `FUN_180002430(list, param_3)` = **把 `thisCard` 直接放进结果列表**，无任何存活/在场判断 |
| `actingCard = 80` / `attacker = 40` | 同文件 `:761-769` | `if ((iVar6 == 0x50) \|\| (iVar6 == 0x28))` ⇒ `FUN_180002430(list, param_4)` = **把 `actingCard` 放进去** |

两处都 `goto LAB_180818485`（`:1378` 的**共同返回口**）⇒ **闸门在它们后面**，不会再把这张卡筛掉。
枚举值与偏移：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/TargetsAffected.cs`（`any=0 … self=20 … actingCard=80`）·
`TargetCriteria.cs` 里 `targetsAffected` 是**第一个字段**（IL2CPP 对象头 `0x10`）⇒ 上文那个 `*(int*)(param_2 + 0x10)` 就是它。

**顺带复核了 A 表那条「判据只到一半」的另一半**（自己读的，不是转抄）：
`d:/2/tools/decomp_full/CardScript__CheckIfDead.c`
· `:108-110` 闸门 = `(int)*(param_1 + 0x228) ∈ {2, 0xf, 3, 0x11}`（= **在场上**）且 `CurrentSurvivor >= 1`；
· `:151-157` `HasCurrentTrait(0x1d6 /*sacrifice*/)` ∧ `IsPlayerTurn(bm) == card.isPlayer` ⇒ `CardScript__TriggerSacrifice(param_1)`；
· `:163` 之后才 `CardScript__UseSurvivor(param_1)`。
⇒ **献祭那一刻这张卡仍被原版算作「在场上」（`cardState` 没变）**，`TriggerSacrifice` 确实排在 `UseSurvivor` **之前**。这一半**查到了**。

**没查到的（如实写「判据不足」）**：
> 「**一条无主语正文在原版数据里配的 `targetsAffected`** 到底是 `self`（20）还是别的值」——
> 那是**服务端下发的能力数据**（`CardAbility` 在客户端是**反序列化**出来的），本地
> **既无卡、也无卡面可核**（`Sacrifice:` / `Survivor:` 全池 0 张）。
> ⇒ ⛔ **我没有把结论写成「原版一定打自己」**，只写成
> **「`self` / `actingCard` 这一档本身不按生死换语义」**（上面那张表是硬证据），
> 加上**我们自己的口径**：`EffectTargetSpec.Subjectless` 的 doc 原文就写着
> 「**有施放者就是施放者自己**」—— 那个 `IsAlive` 是**实现时多出来的一道闸**，不是这一档的判据。

### 1·3 改动清单

| `文件:行号` | 一句话 |
|---|---|
| `Core/EffectResolver.cs:835-874` | 无主语支：`source != null` ⇒ **直接收施放者（不看 `IsAlive`）**，并在「它此刻 ≤ 0」时**打一行日志**；`source == null && chosen == null` 那条「己方全体」兜底**补上日志**（并报出**具体收进了谁**）—— 注释里写着「把这件事说清楚」，原来**代码里一行都没有** |
| `Core/EffectResolver.cs:795-834` | 那一支上方的**判据段**：写清原版 `self`/`actingCard` 的实证（地址 + 行为）、为什么去掉 `IsAlive`、**以及判据缺的另一半**（服务端数据）与「改后数值仍看不见」的原因 |

### 1·4 证据（改前 → 改后；离线驱动 `out_before.txt` / `out_after.txt`）

**① 献祭格**（`keywords = {"survivor 3", "Sacrifice: Gain +1 Attack"}`，本方回合、被自己人打 5 伤）：

| 读数 | 改前 | 改后 |
|---|---|---|
| **施放者自己** `Attack` | 2 → **2** ❌ | 2 → **2**（目标已是它，但被 10 道闸丢掉） |
| 旁观单位 `Attack` | 2 → **3** ❌（错加） | 2 → **2** ✅ |
| **督军** `Attack` | 2 → **3** ❌（错加到督军） | 2 → **2** ✅ |
| 本方棋盘总攻 | 6 → 8 | 6 → 6 |
| 日志 | `SACRIFICE：「Gain +1 Attack」给了 2 个目标`（**没有一行说这是兜底**） | 目标 = 「Esc_Sac」**1 个** ＋ 一句 `（…目标是施放者自己「Esc_Sac」——而它此刻生命 -3 ≤ 0 ⇒ …这一条对它不会生效…）` |
| 救回后 `Health` | 3 | 3（两档同） |

**② `Backlash` 格**（`keywords = {"Backlash: Gain +1 Attack"}`，1 血单位被 3 攻打死后触发）：

| 读数 | 改前 | 改后 |
|---|---|---|
| 被打者（已死）`Attack` | 2 → 2 | 2 → 2 |
| 旁观单位 `Attack` | 2 → **3** ❌ | 2 → **2** ✅ |
| 督军 `Attack` | 2 → **3** ❌ | 2 → **2** ✅ |
| 本方棋盘总攻 | 6 | **4**（= 死者离场后的两个） |
| 日志 | `BACKLASH：「Gain +1 Attack」给了 2 个目标` | 目标 = 「Esc_Bl」**1 个** ＋ 不生效那句 |

**③ 反向对照：战术卡那条路（`source == null`）行为一字不变，只是现在出声**：
`"Gain +1 Attack"` 当**战术卡**打 ⇒ 我方单位 `1→2`、督军 `1→3`（= 己方全体），**改前改后完全相同**；
差别只在日志多了一行
`（「(未写主语：有施放者就是施放者自己，否则己方全体)」：**没有施放者、也没点目标** ⇒ 按既有近似落到**己方全体**（2 个：Esc_T1、FixtureWarlord）—— 这条近似不是原版语义，如实报出来）`。

**④ 真池受影响面 = 0（两条独立证据）**：
· **静态扫**（Python，`cards_engine.json` 1126 张）：带 `Backlash` 的 10 处正文我逐条读了 ——
  `BL10`/`SAU73` = `create`、`GOF82` = `deploy`、`BL_Ghallaron_s_Disciple`/`GOF6` = 显式目标、`GOF5` = `return`+`costmore`、
  `ASH84`/`BL_Litany_of_Despair`/`DA86`/`UM95` = 显式目标；带 `Sacrifice:`/`Survivor:` 的卡 **0 张**。
· **解析器侧**：2147 条文本的签名里，`Target` 是 `Subjectless` 的 99 条，与 `Backlash` 相交的**只有 `GOF5` 的 `return`**
  —— 而它在 `DoReturn` 里**更早就被接管**（那条 `ctx.ActingUnit != null && !ctx.ActingUnit.IsAlive` 的分支，
  已被 `RuleEngineTest.cs:6324-6378` 的「旁观单位还在场上」那条断言钉着）。
⇒ **本笔对今天 1126 张卡的读数 = 零改动**（是堵形状 + 把静默换成出声）。

### 1·5 我做的判断（判据没定的地方按什么做的、为什么）

1. **为什么是「收施放者、不看生死」，而不是「退成零目标」或「保持退全队」**：
   · 退全队 = 改前的病（静默换语义，`打得比卡面宽`）；· 退成零目标 = 抛弃「有施放者就是它自己」这条
   **我们自己的**口径（`EffectTargetSpec.Subjectless` 的 doc）；· 收施放者 = **口径 + 原版实证**两头都对。
2. **为什么不去放开那 10 道 `!IsAlive` 闸**：`Give +N Health` 会把 `Health = -3` 的尸体推到 `> 0`
   ⇒ **复活一个不该复活的**。放开到哪一档、要不要与 `DoDeal` 保持对称，都**判据未定**
   ⇒ 按简报「⛔ 不许自己发明口径」**停手**，只让它在日志里出声。
3. **为什么两条降级路都要打日志**：A 表点名「那一支一行日志都没有，而注释却写着『并把这件事说清楚』」——
   本工程红线「**不许静默失败**」。日志里带上**具体收了谁**（`Names(list)`），免得又要靠复现去看。

### 1·6 还差什么

1. 🔴 **`Sacrifice:` 的 `+1` 数值上仍不生效**（目标对了、被结算层丢掉）。要真生效得另裁那 10 道闸，
   见「顺手发现 1」（含风险与建议形状）。
2. ⚠️ **判据的另一半仍是空的**：「无主语正文在原版数据里配的 `targetsAffected`」是服务端数据
   ⇒ 本地**取不到**（不是没找）。这一格**只能靠将来真 Play 或远端数据**。

---

## 二、`A1388` —— `SOR6` 的**无条件** `instead` 没抑制第一段

### 2·1 结论

**做了，判据满足。** 根因**不是** `instead` 没贴上（`SOR6` 一直都贴上了），
而是**结算层把它当成了「条件判不了」**：那一圈对**每一条** `instead` 都调 `ConditionHolds`，
而它读 `op.ConditionKind`、`default:` **返 `false` = 判不了**；**没有条件**的 `instead`
⇒ `ConditionKind` 是空串 ⇒ 落 `default` ⇒ `!judgeable` ⇒ 那句 `continue` ⇒ **两条都照常走**。
⇒ 修法：**条件字段为空 = 这条 `instead` 本来就没有条件可判 ⇒ 恒成立**。

### 2·2 改动清单

| `文件:行号` | 一句话 |
|---|---|
| `Core/EffectResolver.cs:180-190` | `instead` 配对循环：`if (string.IsNullOrEmpty(ops[i].Condition)) { judgeable = true; holds = true; }` 否则才走 `ConditionHolds` —— **有条件的 `instead` 一字不变** |
| `Core/EffectResolver.cs:154-179` | 上面那段判据注释（含改前实测读数与「为什么空条件 = 恒成立」） |

### 2·3 证据（`SOR6 Righteous Repugnance` 卡面原文当夹具）

`Give +1 [Attack] and +1 [Ranged] to your units this turn. 4 Energy: Give +2 [Attack] and +2 [Ranged] instead and Heal them 1`
解析出来三条 op（改前改后**完全相同**）：
`[0] give '+1 attack and +1 ranged' cost=0 shared=0 instead=0`
`[1] give '+2 attack and +2 ranged' cost=4 ck=energy shared=0 **instead=1**`
`[2] heal 1 cost=4 shared=1`（尾句）

| 读数 | 改前 | 改后 |
|---|---|---|
| **我方单位近战 `Attack`** | 1 → **4** ❌（第一段 +1 与付费段 +2 都加了） | 1 → **3** ✅（卡面正解 = 1+2） |
| 我方单位 `Health` | 3 → 4 | 3 → 4（治疗那半没变） |
| 能量 | 20 → 14（Δ=−6 = 2 卡费 ＋ 4 激活） | 20 → 14（Δ=−6，**一分没变**） |
| 日志 | 第一段与付费段**都**「给了 2 个目标」 | 只剩付费段那一条（第一段被 `skip[]` 掉） |

**反向对照（**有条件的** `instead` 改前改后一字不变）**：
`2 ☀: Give +1 [Attack] to a friendly troop. If you have less than 6 Energy, give +2 [Attack] to a friendly troop instead`
· 能量 3（条件成立）⇒ 信仰 Δ=**−2**、单位 `1 → 3`（走 `instead` 那条）—— **两档读数相同**
· 能量 20（不成立）⇒ 信仰 Δ=**−2**、单位 `1 → 2`（走前半句）—— **两档读数相同**

### 2·4 我做的判断

- **判据 = `ResolveOneCore` 自己那条闸**：它是 `if (!string.IsNullOrEmpty(op.Condition))` ——
  **空的时候根本不进**。把同一句话补到配对循环，两处口径就一致了（⛔ 不另写一份）。
- **只改「空条件」这一支**：判不了的条件仍照旧「两条都跑 + 交给 `ResolveOneCore` 如实报」——
  那条路是**刻意**的（注释里写着「与其按一个过时的值判错，不如放弃替换」），没动。
- **与 `A1369` 洞 3 的接口自动成立**：无条件 `instead` 落地后，被跳掉的那条**付费** op 由
  现成的 `pays[]` 顺延给 `instead` 那条 ⇒ **一次激活只收一次钱**（这正是 `A1414` 那个「真形状」的可观测读数）。

### 2·5 还差什么

无（本笔自足）。⚠️ 今天**只有 `SOR6` 一张卡**吃到这条修法 —— 但它是**真卡、真偏离**（不是「0 张可达」）。
（`SOR49` 那一句只有 1 条 op ⇒ 构不成一对，改前改后都不动 —— 与 `R7` 的读数一致。）

---

## 三、`A1414` —— `deal` / `heal` / `stun` 没有 `instead` 贴点

### 3·1 结论

**做了三个动词，判据满足。** 新增**唯一**一处剥离口 `EffectText.PeelInstead`（原来是 `TryGive` 内联的 12 行，
**搬移、不是重写**），`TryDeal` / `TryStun` / `TryHeal` 各在自己的匹配**之前**调它；`TryGive` 改成调同一份
（⛔ 不出现第二份 `instead` 语法）。
⚠️ **`destroy` / `blind` 仍然没有**这一支 —— 见「顺手发现 3」（今天 0 张可达，一行一处的同形延伸）。

### 3·2 改动清单

| `文件:行号` | 一句话 |
|---|---|
| `Core/EffectText.cs:4837-4886` | 新增私有 `static bool PeelInstead(ref string low)` + 判据 doc（只认句中 ` instead and ` 与句尾 ` instead`；条件句那条路为何不受影响；与 `A1388` 的配对关系） |
| `Core/EffectText.cs:4900-4913` | `TryDeal`：`SplitAndTail` 之后摘 ` instead`，`Instead = instead` 进 `EffectOp` |
| `Core/EffectText.cs:5140-5151` | `TryStun`：同上（不摘的话那个词会并进 `^stuns?\s+(.+)$` 的**目标短语**） |
| `Core/EffectText.cs:5240-5310` | `TryHeal`：同上；**四条返回支**（`ReHeal` / `ReHealVerbThem` / `ReHealPronounSubj` / `ReHealReverse`）**全都**带上 `Instead` —— 漏一条就是静默少贴 |
| `Core/EffectText.cs:6816-6825` | `TryGive`：内联的 12 行 → `PeelInstead`（**行为逐字不变**） |

### 3·3 证据（改前 → 改后）

**① 解析层**（探针 `seg` 与离线驱动同读数）：

| 文本 | 改前 | 改后 |
|---|---|---|
| `2 ☀: Deal 1 damage. Deal 2 damage instead` | `ops=1` ＋ `unparsed = [Deal 2 damage instead]` | `ops=2`；`[1] deal amt=2 cost=2 ck=faith shared=1 **instead=1**` |
| `2 ☀: Heal 2 to a friendly troop. Heal 4 to a friendly troop instead` | `[1].instead=**0**`（词留在句尾，靠放宽的 `ReHeal` 侥幸吃下） | `[1].instead=**1**` |
| `2 ☀: Stun an enemy troop. Stun 2 enemy troops instead` | `[1].instead=**0**` | `[1].instead=**1**` |
| `Deal 2 damage instead`（**没有可替换的那条**） | `ops=0` ＋ `unparsed` | `ops=1`（`instead=1`，但**配不上对** ⇒ 按普通 `deal` 结算）—— 如实记：这是「**整句不认 → 按正文结算**」的行为变化，⛔ 不是「静默换成别的目标」 |

**② 结算层**（离线驱动真打一发）：

| 夹具 | 读数 | 改前 | 改后 |
|---|---|---|---|
| `2 ☀: Deal 1 damage. Deal 2 damage instead`（敌人 9 血） | **rc** | **13**（解析不全 ⇒ 打不出去） | **0** |
| | 敌人生命 | 9 → 9（挨 0） | 9 → **7**（挨 2 ✅ 卡面正解） |
| | 信仰 | 10 → 10（Δ=0） | 10 → **8**（Δ=**−2**，一次激活只付一次 ✅） |
| `2 ☀: Heal 2 to a friendly troop. Heal 4 to a friendly troop instead`（我方 3/9 血） | 我方生命 | 3 → **9**（两段叠加、顶格） | 3 → **7** ✅（只结算 `instead` 那条 4 点） |
| | 信仰 | Δ=−2 | Δ=−2 |

改后日志（`deal` 那格）：
`「deal 1 damage」那条**付款的**被 instead 跳过了 ⇒ 付款顺延到同组的「Deal 2 damage instead」（一次激活只付一次）`
→ `付了 2 点信仰激活「Deal 2 damage instead」` → `对 1 个目标造成 2 点伤害（共 2）`。

**③ 全池解析签名对拍**：2147 条文本 **`diff` 0 行**；探针 `check` = **全池 1120 张 · 解析差异 0 行 · 哨兵 0 失败**；
`b19test` = **PASS 45 · FAIL 0**。⇒ 对**今天的卡池是零改动**（真池里 12 处 `instead`：10 处带条件走 `TryIf`、
2 处是 `give`）。

### 3·4 我做的判断

- **剥离口只留一处**：`PeelInstead` 是 `TryGive` 原逻辑的**逐字搬移**（先把 ` instead and ` 抠掉、
  再看句尾），四个动词共用 —— 本工程明令「两处写同一条规则 = 迟早不一致」。
- **剥离的判据收得很紧**（只认那两种写法）：松一点就会吃到
  `Any attack against your Warlord targets this troop instead.`（`SAU42`）这类**整句句尾、但主语在前**的句子。
- **条件句那条路一个字没动**：`TryIf` / `TryIfNoComma` 在送进 `Dispatch` **之前**就把句尾 ` instead` 剥掉并顺手
  `op.Instead = instead` ⇒ 那些 op 到这里时句子里已经没有 `instead`，`PeelInstead` 返回 `false`（= 零影响）。
- **`instead=1` 但配不上对时按普通正文结算**（不丢整句）：比「整句落 `unparsed`」好（卡能打、卡面不打 `*`），
  且**不比改前更错**（改前那种写法是「认不出来」）。今天 0 张可达。

### 3·5 还差什么

- `destroy` / `blind` 的贴点（见「顺手发现 3」）—— ⛔ 本笔按台账只做点名的三个，**没有自作主张扩大**。

---

## 四、`A1415` —— `Repeat this effect` 之前的付费 op 被**再收一次**

### 4·1 结论

**做了，判据满足。** `DoRepeat` 里那条 `ResolveOne(ctx, owner, null, by, o, …)` 改成
`ResolveOne(…, RoutePay(o, false), …)` —— 走 `A1369` 刚落地的那个**现成**工具（一致时递原件、零拷贝），
**不新写第二份付费逻辑**，也不写回 `op.CostShared`。

### 4·2 改动清单

| `文件:行号` | 一句话 |
|---|---|
| `Core/EffectResolver.cs:8000-8031` | `DoRepeat` 的重复循环：每条 `o` 先过 `RoutePay(o, false)`（= 「这一趟不付钱」）；上方写清判据、为什么不再收、以及「递副本、⛔ 不写回」 |

### 4·3 证据（改前 → 改后）

夹具 `2 Energy: Deal 2 damage to an enemy. Repeat this effect`
（解析：`[0] deal amt=2 cost=2 ck=energy shared=0` · `[1] repeat cost=2 shared=1`；`RepeatOps = [deal]`）：

| 读数 | 改前 | 改后 |
|---|---|---|
| **能量** | 20 → **16（Δ=−4）** ❌ 收了两遍 | 20 → **18（Δ=**−2**)）** ✅ |
| 敌人挨的伤害 | 4（2 + 2） | **4**（重复本身照旧跑 ✅） |
| 日志 | `付了 2 点能量激活「deal 2 damage to an enemy」` 打**两遍** | 第二遍明说 `「deal 2 damage to an enemy」的 2 点能量**不再收**（与上一条同属一份付费前缀 —— 一次激活只付一次）` |

**反向对照：`repeat` 那批【本来就没价】时行为一字不变** ——
`Deal 2 damage to an enemy. Repeat this effect` ⇒ 改前改后都是 **能量 Δ=0 · 敌人挨 4 点**。

**真池可达性 = 0（用活的工作区解析器逐条跑，不是眼看正则）**：
2147 条文本里 `repeat` op 共 **16** 个；其 `RepeatOps` 里带 `Cost > 0 && !CostShared` 的 **0 个**。
带价的 `repeat` 共 **7 处**：`ASH52`（2 灵魂石）· `ASH53`（1）· `ASH59`（2）· `ASH24`（1）· `ASH43`（2）·
`SOR50`（4 信仰）· `UM98`（`Oath 3:`）—— **价都贴在 `repeat` op 自己身上**，它重复的那条 `cost = 0`。
⇒ 本行**今天不改任何真卡读数**，是**堵形状**（`A1369` 报告 §五·2 的同一条）。
⚠️ **它属于铁律 11 明令「不许拿『0 张可达』当不做理由」的那一类** —— 本笔按台账做了。

### 4·4 我做的判断

- **用 `RoutePay(o, false)` 而不是「另写一个不收钱的分支」**：判据仍然是**唯一那一份**
  `EffectOp.CostShared`（`EffectText.StampPaidCost` 贴、这里只**读/转写**）；且
  `o.Cost <= 0` 或 `o.CostShared` 已为真时**递原件** ⇒ 今天那 7 张真卡**一个字节都不经过新代码**。
- **与 `for each` / `N times` 同一条口径**：`A1369` 洞 1 修的是「同一段跑 N 遍只收一次」，
  这里是「同一段被 `repeat` 再跑一遍只收一次」—— 同一件事（原版收费在 **ability 层**，
  `BattleManager__PayActiveAbilityCostOath.c:22` 一次激活只调一次 `UseMana`）。
- ⛔ **没有写回 `op.CostShared`**（`EffectOp` 列表是解析产物、会被复用）。

### 4·5 还差什么

无（本笔自足）。⚠️ 同族还剩 `A1369` §四·2 那两格（**(i) `repeats == 0` 时收 0 次** ·
**(ii) `CostShared` 那一支不查余额**）—— **那两格不在本笔的清单里**，如实指出、没动。

---

## 五、本笔怎么验的（**下批可复用**）

- **离线驱动**：`D:/tmp/wf_esc/`（工程外、临时件）。两个项目：
  `after/after.csproj`（`<Compile Include>` 工作区 `Core/*.cs`）·
  `before/before.csproj`（`Exclude` 掉那两个文件、改吃**动手前拍的快照** `../before/{EffectText,EffectResolver}.cs`）。
  跑：`cd /d/tmp/wf_esc/{after,before} && dotnet build -v q && dotnet run --no-build -- <tag>`；
  产物 `out_before.txt` / `out_after.txt`。另有三种模式：`siga|sigb <texts.tsv>`（解析签名）·
  `reach <texts.tsv>`（全池可达性）。
  ⚠️ **夹具直调**（见文首）。
- **全池解析签名对拍**：`D:/tmp/wf_esc/texts.tsv` = 1126 张卡的 `desc` ＋ 全部 `keywords` 原文 = **2147 条**；
  签名含每条 op 的 `verb/amount/payload/cost/CostKind/CostShared/**Instead**/condition/Target.Raw` ＋
  `unparsed` / `partial` 清单。**改前 vs 改后 `diff` = 0 行**（`A1414` / `A1388` / `A1415` 对真池零改动；
  `A1407` 的改动是**结算层**、`Parse` 看不见 —— 它的证据是上面那三格真打读数）。
  ⚠️ **本探针式对拍不替代** `RuleEngineTest.Run`。
- **探针（只跑没改）**：`bash d:/4/Unity/工具/ruleprobe.sh check` ⇒ 全池 **1120** 张 · 解析差异 **0 行** · 哨兵 **0 失败**；
  `… b19test` ⇒ **PASS 45 · FAIL 0**；`… seg "2 ☀: Deal 1 damage. Deal 2 damage instead"` ⇒ 两条 op、
  第二条 `cost=2 shared=1`（⚠️ 探针的 dump **没有 `Instead` 列**，与 `A1368` 记的一致）。
- **类型检查**：`TMPDIR=/tmp/wf_esc bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（跑了 3 次；
  **没出现**简报里说的那类假错）。
- **行尾**：`EffectResolver.cs` 全程 **CRLF**（`8119 / 8119`，`lone_lf = 0`）· `EffectText.cs` 全程 **LF**（`8039 / 8039`）；
  `git diff --numstat` = **208/7** 与 **90/20**（**没有整篇翻**）。新增成员名 `PeelInstead` 全仓**唯一**（`grep` 10 命中，全在本文件）。
- ⚠️ **本笔没有跑任何 Unity 自检**（简报禁止；同一工程同一时刻只能一个实例）——
  `RuleEngineTest.Run` 由调度台在同步点跑。

---

## 六、顺手发现（**一条一句话 + 出处**；⛔ 一条都没顺手改）

1. 🔴 **结算层有一条「一律不给非活体加效果」的【一致约定】，共 10 处** —— 它把 `A1407` 修好的目标**又丢掉了**：
   `DoGive` / `DoDeal` / `DoHeal` / `DoStun` / … 的逐目标循环里都写着
   `if (t == null || !t.IsAlive) continue;`（`grep -c` 实测 **10 处**，`Core/EffectResolver.cs`）。
   后果（离线实测）：`Sacrifice: Gain +1 Attack` 改后**目标对了，但数值上谁都没加上**。
   ⛔ **我没放开它**：`Give +N Health` 能把 `Health = -3` 的尸体推成 `> 0` ⇒ **复活一个不该复活的**。
   要放开得先裁「放开到哪一档」（建议形状：只对 `spec.Subjectless && ReferenceEquals(t, ctx.ActingUnit)` 开一格，
   并配一条「**别的动词仍不许碰尸体**」的反向沙包）—— 请调度台裁。出处 = `Core/EffectResolver.cs:6284-6286`（`DoGive` 那一处）。
2. ⚠️ **`DoRepeat` 传的是 `source = null`**（`ResolveOne(ctx, owner, **null**, by, …)`，`Core/EffectResolver.cs:8030`）
   ⇒ `ResolveOne` 会把 `ctx.ActingUnit` 也设成 `null` ⇒ 重复的那批 op 里若有**无主语正文**，
   会落到「`source == null` ⇒ 己方全体」那一档（**与 `A1407` 同族的第一半**，只是另一条入口）。
   **今天 0 张可达**：12 张 `repeat` 卡逐张看过，`RepeatOps` 里 `Subjectless` 的 **0 个**（读数同 §4·3）。
   ⛔ 不在本笔清单里，未动。
3. ⚠️ **`destroy` / `blind` 仍然没有 `instead` 贴点**（`A1414` 的同形延伸）：
   `EffectOp.Instead` 现在的贴点是 `TryIf` / `TryIfNoComma` / `TryGive` / `TryDeal` / `TryStun` / `TryHeal`。
   真池里 `Destroy … instead`（`SW55` / `TAU53`）**走的是条件句那条路**（`TryIf` 已经剥）⇒ **今天不需要**；
   `Blind … instead` **0 处**。要按铁律 11 补齐 = 在 `TryDestroy` / `TryBlind` 各加一行 `PeelInstead`（本笔没做：台账只点名三个动词）。
4. ⚠️ **`SOR6` 的付费段自身的 `Target` 是 `Subjectless`**（卡面靠上一句的 `to your units` 承接，文本层解不出来）
   ⇒ 它是**靠「己方全体」那条兜底**才打对的。今天行为正确、只是来源含糊；
   本笔新加的日志把这件事实**说了出来**（`（…没有施放者、也没点目标 ⇒ 按既有近似落到己方全体…）`）。
   出处 = `Core/EffectResolver.cs:869-871` + 离线驱动 `out_after.txt` 的 SOR6 那格。
5. ⚠️ **`A1369` 报告 §五·2 有一处数字口径不一致**（**只报不改**，那文件不在本笔白名单）：
   它写「全池 **6 张** `repeat` 带价的卡」却列了 **7 个 id**（`ASH52`/`ASH53`/`ASH59`/`ASH24`/`ASH43`/`SOR50`/`UM98`）。
   我独立扫的结果 = **7 处**（6 张 `N [货币]:` ＋ `UM98` 的 `Oath 3:`），**与它列的 id 一致、与它写的数字不一致**。
   出处 = `资料/普查产出_第十四会话/W_A1369付费三洞.md:114-116`。
6. ✅ **`A1388` 那个「`default: false` 一个返回值干两件事」的形状，我把另外两个调用点也核了 —— 都安全**：
   `ConditionHolds` 全仓**三个**调用点 —— `:189`（`instead` 配对循环，**本笔修的**）·
   `:504`（`ResolveOneCore` 的条件闸，**上一句就是 `if (!string.IsNullOrEmpty(op.Condition))`**）·
   `:6839`（`AltHolds`，**上一句是 `if (string.IsNullOrEmpty(op.AltCondition)) return false;`**）。
   后两处**只在真有条件时**才进 ⇒ `default: false` 在那儿正好只表达「**这个条件本版不认识**」。
   ⇒ 同一形状**只此一处**，没有第二笔同族账。出处 = `Core/EffectResolver.cs:189 / 504 / 6839`。
7. ⚠️ **`W_SurvivorSacrifice族.md` §4·1 的离线探针目录是 `D:/tmp/wf_sursac/probe/`、§4·2 是 `cmp/`** ——
   两份 `csproj` 都在、不算错；写一笔是为了下批复现时知道**得两处都找**。
   本笔的做法与它同形（`after/` + `before/`），只换到 `D:/tmp/wf_esc/`。
   出处 = `资料/普查产出_第十四会话/W_SurvivorSacrifice族.md:208 / 217`。
