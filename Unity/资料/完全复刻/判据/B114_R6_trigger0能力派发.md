# `B114_R6` · `B-211` 结构性缺口：`trigger == 0`（`ThisCardPlayed`）的能力由谁派发

> 只读查证代理 `R-6`｜日期：2026-11-01｜会话：第一百一十四会话
> ⛔ 未跑 Unity、⛔ 未动 git、⛔ 未改任何源码/资产。本文件是**唯一**产出。
> 数据面口径：`cards_engine.json` **v47 · 1,129 卡 · `triggerAbilityIndices` 共 1,622 行**（现读）。

---

## 〇、一句话结论

**原版 `trigger == 0` = `AbilityTrigger.ThisCardPlayed`，由「这张牌被打出」那一跳派发**
——`CardScript.NonTargetSpellPlayed` / `TargetedSpellPlayed` / `ResolveCardPlayed`
→ `RawCardScript.OnCardPlayedWithTarget`（`thisCard == cardPlayed` 时）
→ `TriggerAbilities(AbilityTrigger.ThisCardPlayed, …)` → `CardAbility.CanTriggerAbility(…, 0, …)`
→ `RawCardScript.TriggerAbility` → `AbilityLogic.PlayAbility(logic, bm, abilityData, trigger=0)`。

⇒ **`ca ∈ {320, 360, 380}` 落在 `trigger 0` 的那些行是【真可达】的**，随卡牌打出那一刻执行。
**`ca = 480` 全池只有 1 行、落在 `trigger 120 Slay`，不在 trigger 0**（brief 里「全在 trigger 0」这条前提**不成立** —— 43 行里 36 行在 trigger 0，另 7 行在 10/120/145/300/301/455/560/580）。

---

## 一、`AbilityLogic.PlayAbility` 调用方全表

三源合判（`decomp_full/*.c` × `out_cpp2il_dev_ilspy` × `out_il2csharp`），`PlayAbility(` 的调用点**只有 5 处、归 3 个宿主**：

| # | 调用点 | 在哪个宿主里 | 触发时机 | 传进去的 `trigger`（第 4 形参） |
|---|---|---|---|---|
| 1 | `decomp_full/RawCardScript__TriggerAbility.c:89` | `RawCardScript.TriggerAbility`（`RandomAbilityChoice` 为真 ∧ logic 数 > 1 那一支） | 任意 `AbilityTrigger` 触发（含 0） | **`*(undefined4 *)(param_2 + 0x10)`** = 那条 `CardAbility.trigger`（`:74` 取值，`:89` 传入） |
| 2 | `decomp_full/RawCardScript__TriggerAbility.c:103` | 同上（正常循环体，逐条 `abilityLogic` 全跑） | 同上 | 同上（`uVar10 = *(undefined4 *)(param_2 + 0x10)`，`:74`） |
| 3 | `decomp_full/RawCardScript__ResolveActiveAbility.c:65` | `RawCardScript.ResolveActiveAbility`（**主动能力**那一趟，走 `activeAbility` / `alternativeAbility`，不是 `cardAbilities`） | 玩家点主动技能按钮（`CardScript.ResolveActiveAbilityPlayed`，`BattleManager.cs:9428` 唯一外部调用点） | **字面量 `400`**（`ThisUnitAbilityUsed`）——硬编码，不是读字段 |
| 4 | `decomp_full/RawCardScript__ResolveAbilities.c:24` | `RawCardScript.ResolveAbilities`（随机抽一条） | —— | `param_5` 透传 |
| 5 | `decomp_full/RawCardScript__ResolveAbilities.c:40` | 同上（全跑） | —— | `param_5` 透传 |

**#4 / #5 是死壳**：`RawCardScript.ResolveAbilities` 在三源里**都只有定义、0 个外部调用点**
（`decomp_full` / `out_cpp2il_dev_ilspy/RawCardScript.cs:2029` / `out_il2csharp/RawCardScript.cs:1479`）。
它的体与 `TriggerAbility.c:82-106` 逐句同构 ⇒ **被编译器内联进 `TriggerAbility`**（同族先例：本项目「0 调用点可能是内联假象」那条）。
⇒ **真正会跑的生产调用点 = #1 / #2（触发路，trigger 透传）+ #3（主动能力路，trigger 恒 400）。**

### 1·1 🔴 `PlayAbility` 的第 4 形参 **【不是】全程未使用** —— `B114_W9` 报告 §一·7 这条**要订正**

`AbilityLogic__PlayAbility.c:72` 把 `param_4` 存进 `local_res20` 之后，**有两处真读它**：

| 行 | 代码 | 含义 |
|---|---|---|
| `:285` → `:2271` | `iVar21 = local_res20;` … `AbilityLogic__MatchingTrigger(param_1, *(enchantmentCard + 0x290), iVar21, 0)` | `ca == 340 (removeThisEnchantment)` 那一支（`:2249` `if (iVar8 == 0x154)`）：**只摘「能力匹配当前 trigger」的那件附魔** |
| `:1062` | `if ((cVar4 != '\0') && ((local_res20 == 0x32 \|\| (local_res20 == 0x3c))))` | `0x32 = 50 (UnitAttack)` / `0x3c = 60 (UnitDamaged)` ⇒ 这两族走一条「按攻守方取 uVar10 = 10/15」的分岔 |

⇒ 施工时**不能**假设「trigger 传什么都一样」：**至少 `ca 340` 与「ca 50/60 的攻守方分岔」依赖它**。
（`B114_W9` §五·2 恰好把 `Pray Enchantment` 的 `trigger 60 · ca 340` 归成 `OwnLane`「本趟不管」—— 那一行**正是**这两处都命中的一格。）

### 1·2 `TriggerAbility` 内部另外两处也读 `CardAbility.trigger`

`RawCardScript__TriggerAbility.c` 里 `*(int *)(param_2 + 0x10)` 在调 `PlayAbility` 之前还被读三次：
`:34`（`== 0x14 (20 WhileInPlay)` ⇒ 置 `AbilityData+0x40`）· `:38`（`== 0xf0 (240 WhileInPlayVariable)` ⇒ 置 `+0x41`）·
`:42`（`== 600 (UseSpiritStone)` ⇒ `PlayerManager.UseMana`）——**都与 trigger 0 无关**，但说明「`+0x10` 是 trigger」这条判据在 `TriggerAbility` 里也是同一口径。

---

## 二、`trigger == 0` 的语义与产生者

### 2·1 语义 = `AbilityTrigger.ThisCardPlayed`

`d:/2/Warpforge_code/Scripts/Assembly-CSharp/AbilityTrigger.cs:3` → `ThisCardPlayed = 0`（枚举第一项，紧跟其后是 `OtherCardPlayed = 10`）。
⇒ **`trigger 0` 既不是「哨兵」也不是「空」**，它就是「**这张牌自己被打出**」这条触发。

> 📌 我们的 `ActionPump.cs:1726` 早已 VA 级定案过同一件事（`0 = AbilityTrigger.ThisCardPlayed`，不是「跑整条 ability」的哨兵），
> `CardDef.cs:1997` / `CardDefExtraTraits.cs:70` 两张表也按 0 处理。**本节是独立复核，结论一致。**

### 2·2 谁会构造 `trigger 0` 的请求（`TriggerAbilities(…, 0, …)` 的生产者，全表）

三源合判，`TriggerAbilities` 第 2 形参**字面量 0** 的调用点共 **6 处**：

| # | 调用点（`decomp_full`） | 上游入口 | 时机 |
|---|---|---|---|
| 1 | `RawCardScript__OnCardPlayedWithTarget.c:43` | `CardScript.NonTargetSpellPlayed`（`out_cpp2il_dev_ilspy/CardScript.cs:11915/:11918`）· `CardScript.TargetedSpellPlayed`（`:11993/:11997`）· `CardScript.ResolveCardPlayed`（`:14432/:14436`） | **主力路**：`thisCard == cardPlayed`（即「被打出的就是这张牌自己」）∧ `(!targeted \|\| 有目标)` ⇒ 跑这张卡 `trigger 0` 的全部 ability |
| 2 | `RawCardScript__OnCardPlayedWithTargetJustThisCardPlayed.c:39` | `CardScript.TargetedSpellPlayed`（`:12021`）· `CardScript.ExecuteSynapeEffect`（`:12048`） | **突触（Synapse）重复**：相邻单位身上再跑一遍它们自己的 `ThisCardPlayed` |
| 3 | `RawCardScript__TriggerEnchantment.c:15` | `BattleManager.cs:35620` · `CardScript.cs:14810/:14827` | 触发附魔：**先发 0，紧接着 `:18` 发 500** |
| 4 | `AiScripted__ExecuteAction.c:209` | AI 的脚本化动作 | 脚本动作先发 0，`:212` 再发 500 |
| 5 | `BattleManager__ResolveTriggerCardPlayedByStimulation.c:208` | `CardScript.ResolveCardPlayed` 的 Stimulation 那一支 | 激励（Stimulation）战术被打出时，被激励单位重跑自己那张战术的 `ThisCardPlayed` |
| 6 | `CardScript__TriggerOtherCardAbility.c:20` → `OnTriggerEffectInCard(other, 0, …)` | `AbilityLogic__PlayAbility.c:2869`（**`ca 260 triggerEffectInCard`**，判据 `:2534` `if (iVar8 == 0x104)`）· `BattleManager__ApplyEnvEffect.c:15` · `BattleManager._ResolveSummonUnit_d__510__MoveNext.c:252` · `BattleManager._StartBattlePhase_d__334__MoveNext.c:88/:201` | **让「另一张卡」跑它自己的 `ThisCardPlayed`** |

**主力路的完整链（逐跳）**：

```
BattleManagerSupport.BroadcastCardPlayed(manager, actingCard, targetCard)      out_cpp2il_dev_ilspy/BattleManagerSupport.cs:669
  └─ for each unitsInPlay / 双方手牌:  c.ResolveCardPlayed(actingCard, targetCard)   :679 / :689 / :699
       └─ CardScript.ResolveCardPlayed(cardPlayed, targetCard)                 out_cpp2il_dev_ilspy/CardScript.cs:14432
            └─ rawCardData.OnCardPlayedWithTarget(BattleMgr, this, cardPlayed, null)   :14436
                 └─ RawCardScript.OnCardPlayedWithTarget(...)                  out_cpp2il_dev_ilspy/RawCardScript.cs:1115
                      └─ if (thisCard == cardPlayed) TriggerAbilities(ThisCardPlayed, …)  :1160
                           └─ decomp_full/RawCardScript__TriggerAbilities.c:207  遍历 cardAbilities(+0x290)
                                └─ CardAbility__CanTriggerAbility.c:71   if (ability+0x10 != 0) return 0;   ← 只放 trigger 0 的那几条
                                     └─ :231 / :257  RawCardScript__TriggerAbility(card, ability, …)
                                          └─ :89 / :103  AbilityLogic__PlayAbility(logic, bm, abilityData, 0)
                                               └─ :286  iVar8 = chosenAbility (+0x10) ⇒ 巨型效果 switch
```

> ⚠️ **两处副作用形参**（不是 trigger，但同族）：`TriggerAbilities` 的 `param_8/param_9/param_11` 是「有没有 acting / target / 是否含附魔」。
> `OnCardPlayedWithTarget.c:43` 传的是 `0,0,0` ⇒ 走 `:47-53` 那条路（`param_8 = 1` 被 `:43` 覆盖成 0、`:44` 又把它置回 1）。

### 2·3 `trigger 0` 在池内有多大（`cards_engine.json` v47 现读）

| 量 | 值 |
|---|---|
| `triggerAbilityIndices` 总行数 | **1,622** |
| `trigger == 0` 的行 | **936**（57.7%）· 涉及 **587** 张卡 |
| `trigger == 0` ∧ `ca >= 320` 的行 | **60** |
| `trigger == 0` ∧ `ca ∈ {320, 325, 360, 380, 480}` | **37**（= 320×15 + 325×1 + 360×18 + 380×3） |

---

## 三、可达性判定

### 3·1 ✅ `ca = 380（setReferenceCard）` —— **可达**，3 行，全在 `trigger 0`

| 卡 | 行 | 同组的其它 `ca` | 卡面 |
|---|---|---|---|
| `AM54` At All Costs | `(ab0, lg1)` · trigger 0 | `(ab0,lg0)=0 drawCard` · `(ab0,lg2)=320` · `(ab1,lg0)=30` **在 trigger 410** | `Draw 3 cards. If your Warlord has 10 or less Health, lower their cost by 4` |
| `GOF93` Hunta Rig | `(ab0, lg1)` · trigger 0 | `(ab0,lg0)=250 autoPlayFromHand` · `(ab0,lg2)=320` · `(ab1,lg0)=235` **在 trigger 410** | `Stomp. Rally: Choose a troop in the enemy hand and deploy it. This troop attacks it.` |
| `GOF104` Drag it Down | `(ab0, lg0)` · trigger 0 | `(ab1,lg0)=260 triggerEffectInCard` · `(ab1,lg1)=320` · `(ab2,lg0)=30` **在 trigger 410** | `Deal 1 damage to an enemy for each friendly unit. If target dies, give +1 Health to all friendly troops in hand` |

**它到底做什么**（`AbilityLogic__PlayAbility.c:2128-2160`，判据链完整）：
```
if (iVar8 != 0x17c) return;                       // 0x17c = 380
if (*puVar1 == 0) goto out;                       // 这条 logic 的目标表为 null ⇒ 跳过
if (0 < count(*puVar1)) {
    card = *(param_3 + 0x10);                     // AbilityData.thisCard = 施放的那张牌
    list = new List<CardScript>();
    *(card + 0x278) = list;                       // ← 直写（覆盖，不追加）
    for each t in *puVar1: list.Add(t);
}
```
`+0x278` 的真身 = **`CardScript.referencedCards`**（`CardScript__get_referencedCards.c:5` / `__set_referencedCards.c:5`）。
**它本身零可见效果**，只给后续行当「目标源」用：
- `AbilityLogic__GetTargets.c:771/:777/:785-799` —— `ta = 200/201` 那一族直接取它当目标表；
- `FilterMethods__CheckIfMeetsCriteria.c:94/:106/:111` —— criteria 求值时读它；
- `AbilityLogic__GetRefCard.c` —— `ca 325` 那一支判「refCard 在不在目标表里」。

⇒ **380 与 320 是同一条链的两段**（`380` 先记牌 → `320` 排后续 → `410` 那条 ability 用 `ta=200/201` 取回那批牌）。
三张卡的数据证据：`abilityTargets` 里 `trigger 410` 那一行的 `ta` 分别是 `[200]`（AM54）、`[201]`（GOF93）、`[50]`（GOF104）
—— **两个 `200/201` 正是 `referencedCards`**（`EffectResolver.cs:1334-1335` 已有同一张映射表的我们侧等价物）。

### 3·2 ✅ `ca = 360（playChosenCardAbility）` —— **可达**，18 行（17 行在 trigger 0）

判据 `AbilityLogic__PlayAbility.c:1994-2070`（`case 0x168`）：读 `manager + 0x2e8`（= `cardChosenInSelection`）
→ 若那张卡带 `oath` trait（`0x4fb`）先 `PayCardPlayedCostOath` 再 `OnTriggerAlt(0, …)`
→ 否则 `OnTriggerAlt(0, …)` —— **`OnTriggerAlt(0)` 就是直接拿这个 0 当 `TriggerAbilities` 的 trigger 再派一次**。
⇒ **360 自身又回到 §2·2 的 #1 那一格**，形成「打出 A（trigger 0）→ 360 → 让 B 也跑 trigger 0」。

卡型分布：17 张 `tactic` / `defence`（`ASH71` `DA46` `SW48` `UM109` `UM110-UM118` …）＋ 1 张 `unit` `TL20 Carnifex`，**全部 trigger 0**。

> ⚠️ 与我们侧对不上的地方：我们给 `ca 360` 建的执行器 `RunPlayChosenCardAbility`（真身 `EffectResolverPartial_B88E2.cs:981`）
> **只接在 `RunActiveRowOpsOrGrantedTrigger`（`EffectResolver.cs:18781` 那一串 `if (ca == …)`）之后** ⇒ 只有「主动能力行」到得了。
> 这 18 行全是 `cardAbilities` 侧的行，今天**走不到那个执行器**。

### 3·3 ✅ `ca = 320（followAfterResolution）` —— **可达**，24 行（15 行在 trigger 0）

判据 `AbilityLogic__PlayAbility.c:2244`（`if (iVar8 != 0x140) return;`）+ 共用尾块 `:2474`
→ `BattleManager__AddFollowAfterResolution(manager, abilityData+0x10, abilityData+0x20, abilityData+0x44, …, 0)`
→ 队列轮到它时 `CardScript__FollowUpResolutionPlayed.c:7` → `OnTrigger(0x19a = 410)`。
（`ca 325 = followResolutionIfPreviousMeetsCriteria` 另有前置检查 `:2373-2470`：`GetRefCard()` 为 null 就 `LogWarning` + **直接 return**，
refCard 不在目标表里就只 `PlayAnim` —— 这一支才是 380 的 refCard 的真消费者。AM54/GOF93/GOF104 用的是 **320**，不是 325。）

### 3·4 ⛔ brief 前提订正：`ca = 480` **不在 trigger 0**

`ca 480（highlightBloodThirst）` 全池 **仅 1 行**：`SW34 Murderfang` · `(ab0, lg2)` · **`trigger 120 (Slay)`**。
判据 `PlayAbility.c:1961-1972`（`if (iVar8 != 0x1e0) return;` → `CardScript__SendHighlightBloodThirstAction(目标[0])`），纯表现。
⇒ 它是 **Slay 路可达**，与 `trigger 0` 无关。**brief 里「`ca ∈ {320,360,380,480}` 全在 trigger 0」这条要改成「43 行里 36 行在 trigger 0，另 7 行在 10/120/145/300/301/455/560/580」**（铁律 5）。

### 3·5 ⇒ 判定

**✅ 可达。** 原版这三条链都是活的，不存在「唯一生产者恒空 / 0 调用点」那种例外。
**因此 `B-211` 不能按铁律 11 例外① 判「不做」。**

---

## 四、我们的缺口具体在哪一行 + 施工建议

### 4·1 缺口位置（三处，逐层）

| 层 | 位置 | 现状 |
|---|---|---|
| ① **数据面已就绪** | `cards_engine.json` v47 `triggerAbilityIndices[].ca` | ✅ 有值（1622 行逐 `(ab,lg)`），`CardDef.TriggerAbilityIndexSpec.Ca` 已挂载 |
| ② **派发口够不到 trigger 0** | `EffectResolver.cs:21067` `FireTriggerByNumberWithSink` | ⛔ 生产上只被 **`60 / 185 / 187 / 210 / 410`** 调用（`EffectResolver.cs:20605-20607`、`:20679`、`EffectResolverPartial_B97W2.cs:451`、`RuleCore.cs:13036`）⇒ `B114_W9` 建的 `FireTriggerRowsByChosenAbility` **永远看不到 `trigger 0`** |
| ③ **战术/防御的 `ThisCardPlayed` 本体走的是另一条路** | `EffectResolver.cs:2815` `PlayTacticBodyDeferred` → `:2832` `ResolveOps(..., abilityFamilyTrigger: "ThisCardPlayed")` | 正文 = **卡面 `desc` 文本解析出来的 op**（`:2622` / `:2046` 的 `EffectText.Parse(card.Desc, …)`），**不是** `TriggerOpsByNumber(0)` |

### 4·2 🔴 不能「在卡打出时补一次 `FireTriggerByNumber(0)`」—— 那是**打两次**

`PlayTacticBodyDeferred` 已经把整张卡的 `desc` 当正文跑过（`:2622` `EffectText.Parse(card.Desc, …)`），
而 `CardDatabasePartial_B98H7.MapTriggerOpsByNumber` 造 `{trigger → ops}` 表时**喂进去的正是同一批文本 op**
（它先 `TriggerOps(kw)` 全收成 `body`，再按 `op.AbilityIndex == row.Ab` 分桶挂到各 trigger）。
⇒ **`TriggerOpsByNumber(0)` 与 `card.Desc` 解析结果是同一批对象** ⇒ 再派一次 = 每张战术卡效果翻倍。
（这正是 `B114_W9` §〇 与 `B113X7` §四 已经写死的那条结论，本次复核**确认成立**。）

### 4·3 施工建议（按「先安全、后完整」两档）

**第 1 档（低风险 · 只补「没有任何文本对应物」的那一格）**：`ca 380 setReferenceCard`

- **为什么这一格安全**：380 是**纯记账**（`card+0x278 = 目标表`），**卡面文字里没有任何一句对应它**
  —— 它的消费者在 `ta = 200/201`，而 `ta 200/201` 的那几行（410 能力）才是「their / it」的落点。
  文本解析器永远解不出「把这次的目标存进引用表」这件事。
- ~~**落点**：`EffectResolver.cs` 的 `PlayTacticBodyDeferred`（`:2815`），**在 `:2832` 的 `ResolveOps` 之前**~~
  > 🔴 **2026-11-01 就地订正（铁律 5 · `B114_W23` 反证）：这条施工建议与本文 §三 自相矛盾，照它做会接成已知缺陷的形状。**
  > `AM54` 的 `triggerAbilityIndices` 现读 `ab0/lg0 ca=0`（drawCard）→ `ab0/lg1 ca=380` → `ab0/lg2 ca=320`，且 380 行 `ta=150` ⇒ **它的目标集是上一跳（lg0 抽 3 张）的产出**。
  > 放在 `:2832 ResolveOps` **之前**时，抽牌还没发生 ⇒ 只会读到 `ctx.PreviousAbilityTargets`（`BattleContext.cs:1134`）里**上一张卡的残值** —— 那正是 `E8` 当年修过的缺陷形状。
  > ⇒ **正确次序 = 在 `lg0` 结算之后**；且 `ResolveTargets` 当前**没有 `ta=150` 这一支**（只特判 200/201 与 60），`SetReferenceCards` 算不出目标集。
  > 完整判定见 `报告/B114_W23_ca380引用表.md`。
- **加什么**：对 `card.TriggerAbilityRowsOf(0)` 里 `Ca == 380` 的每一行，调**已有**的
  `SetReferenceCards`（`EffectResolver.cs:1366`，执行体早已落齐、`B102_EXEC` 已核过 `:2131-2132` 的 `count>0` 闸）。
- **⛔ 拦路石（必须先解决，否则别动）**：`SetReferenceCards` 的签名是
  `(BattleContext ctx, int owner, UnitState acting, …)` —— 要一个 **`UnitState`**；
  而战术/防御卡在我们模型里**没有 `UnitState`**（`EffectResolver.cs:1496-1508` 已如实标注这条结构差：
  `UnitState.ReferencedCards` 只装 `UnitState`，手牌里的卡装不进去）。
  ⇒ **要么**给战术路开一个 `CardDef` 级引用表（`ctx.` 上挂一格），**要么**先只覆盖「目标是在场单位」的那半
  （GOF104 的 `ta` 是 `50/150/20`、GOF93 是 `50/150/20` —— 不全是单位）。**这一格是本条活的真正成本。**
- **验收怎么断**：夹具 `AM54 打出 → 抽 3 张`，断「引用表 ⊇ 那 3 张」+「**灭自证**：同夹具把 `Ca` 改成 `-1` ⇒ 引用表为空」。
  断言宿主 `RuleEngineTest`，别拿「事件发了」当通过（`EmitFollowUpResolution` 现在就在发，但它不发执行）。

**第 2 档（完整 · 但必须先解决「打两次」）**：`ca 320 / 360` 在 `trigger 0` 上也接执行器

- 现状：17 张「`320` 挂在 `ThisCardPlayed`」的战术/防御（`FollowUpSchedulerOf` 硬编码表，`EffectResolver.cs:7950-7974`）
  在 `:2917-2928` **只调 `EmitFollowUpResolution`（纯表现事件）**；
  而关键词那一路（`RuleCore.cs:13036`）在同一个位置**多调了一次 `FireTriggerByNumber(ctx, u, 410, …)`**。
  ⇒ **两条路不对称**：战术卡的 `410` ability **只播了动画，正文没跑**。
- **为什么当初没接（真实拦路石，本次查实）**：`FireTriggerByNumber` 的签名要 `UnitState u`（`RuleCore.cs:17666`），
  战术卡没有 `UnitState` ⇒ 那一跳**结构上接不上**，不是漏写。
- **建议**：先做 §4·3 第 1 档，**再单独立一条账**处理「战术卡 410 正文」这一格
  （要么给 `FireTriggerByNumber` 开 `CardDef` 重载，要么把 410 正文并进 `desc` 解析 —— 但后者要先证明
  `op.AbilityIndex == 410 那一行的 Ab` 的 op **不在** `desc` 里，否则双发）。
  ⛔ **在没证明之前，别在 `:2917` 补 `FireTriggerByNumber`。**

### 4·4 一句话给调度台

**`B-211` 的真实缺口不是 `ca=380 没有派发口」，而是「`trigger 0` 这条路我们走的是『解析卡面文本』，原版走的是『按 ability 逐条 PlayAbility』」—— `380` 是这条岔路上唯一一条【文本里根本不存在】的行。**
⇒ 只补 380 是安全的、可验证的；补 320/360 要先解决「文本 op vs 数据行」的去重。

---

## 五、没查清的部分

1. 🔴 **`SetReferenceCards` 对「手牌里的卡」到底能不能表达 —— 没查清**。
   `UnitState.cs:642` 与 `EffectResolver.cs:1496-1508` 都标着「只装 `UnitState`」；
   但 `AM54` 的引用表**必须**是刚抽进手牌的 3 张卡（`ta = [200]` 的 410 行要拿它们降费）。
   ⇒ 原版写的是 `CardScript.referencedCards`（**卡**，不限于在场单位）；我们这一格**结构上装不下**。
   这条要单独立账，不是本报告能定的。
2. **`ta = 150 / 20`（AM54 的 trigger 0 目标表 `[0, 150, 20]`）在 `AbilityLogic__GetTargets` 里具体是哪一族 —— 没查**。
   只查实了 `200/201` 走 `referencedCards`（`EffectResolver.cs:1334-1335` 已有我们侧映射）。
3. **`ca 360` 的执行器 `RunPlayChosenCardAbility` 的完整判据**（尤其「按 `oathDouble` 补放几次」那一半）没逐句复核 ——
   本次只确认了 `PlayAbility.c:1994-2070` 的调用形状（`AddTriggerCourtesy` 那段是 **400** 的，别混）。
4. **`BattleManager__ResolveAction.c:9249` 那条 `OnTrigger(…, 0, …)` 属于哪个 actionType 没定位到**
   （它在巨型 switch 内、前后被数组拷贝淹没）。不过 ILSpy 的同一行（`BattleManager.cs:30775`）
   落在 `ResolveTriggerCardPlayedByStimulation` 里 ⇒ 已由 §2·2 #5 覆盖，不影响结论。

---

## 六、顺手发现（⛔ 我没动手改，请调度台分流）

1. 🔴 **`报告/B114_W9_B211派发口.md` §一·7 那条判据是错的**：
   它写「`PlayAbility` 的第 4 个形参 `trigger` 只被存进 `local_res20`（`:72`），**全程未使用**」。
   **实际有两处消费**：`:285 → :2271`（`AbilityLogic__MatchingTrigger(logic, 附魔卡+0x290, local_res20, 0)`，
   `ca 340 removeThisEnchantment` 那一支）与 `:1062`（`local_res20 == 0x32 || 0x3c` = 50/60 的攻守方分岔）。
   ⇒ 该报告 §五·2 把 `Pray Enchantment`（`trigger 60 · ca 340`）判成 `OwnLane`「本趟不管」，
   正好压在**这两处都会命中**的那一格上。建议该报告就地订正（铁律 5）。
2. 🔴 **brief 里「`ca ∈ {320,360,380,480}` 全在 trigger 0」不成立**：43 行里 **36 行**在 trigger 0，
   另 7 行在 `10 / 120 / 145 / 300 / 301 / 455 / 560 / 580`；**`ca 480` 只有 1 行且在 `trigger 120 (Slay)`**。
3. 🟡 **`RawCardScript.ResolveAbilities` 是死壳**（三源 0 外部调用点），体被内联进 `TriggerAbility.c:82-106`。
   本仓若有人按名字去搜「第二个派发口」，会误判成「还有一条路没接」。
4. 🟡 **`FollowUpSchedulerOf` 那张硬编码表（`EffectResolver.cs:7950`）与 v47 的 `ca` 数据面可以互相对账了** ——
   表里「17 张战术/防御 = `ThisCardPlayed`」这条，本次用 `triggerAbilityIndices` 现扫**完全对上**
   （`ca` 320 且 `trigger 0` 的战术/防御正是那 17 张 + `GOF93` 这张 unit）。建议在该方法 doc 里补一句
   「v47 起可直接用 `TriggerAbilityRowsOf(0)` 现算，硬编码表可退役」——**但退役是行为改动，要另批做**。
5. 🟡 **`EffectResolver.cs:2917` 与 `RuleCore.cs:13036` 两处「320 触发」不对称**（一处只发事件、一处发事件 + `FireTriggerByNumber(410)`）。
   这是 §4·3 第 2 档的根，也是「战术卡的 410 ability 只有动画没有正文」这条独立缺口的证据。
