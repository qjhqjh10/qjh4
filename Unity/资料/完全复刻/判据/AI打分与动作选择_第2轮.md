# AI 打分与动作选择 · 判据（第 2 轮 · 逐函数）

> 第八十会话 · 代理 **R6**（只查判据、只写判据文件）。
> 触发：`普查产出_第八十会话/G3_IL不可读方法_VA反汇编清单.md` §3·A 的 #1/#2/#3/#5/#6/#7/#22/#23/#30。
>
> **范围（按简报顺序）**：`AI.ScoreFromCriteria` · `AI.GetActionScore` / `AI.ExecuteAction` ·
> `FilterMethods.MeetsConditionCriteria` / `MeetsStatCriteria` · `Evaluation.Check` ·
> `AbilityLogic.GetValueFromCriteria` / `CanPlayAbility` / `GetTargets`。
> 附带把打分族里被这五条直接调用的子打分器一并落成表（§6）。
>
> **每个结论都标了 `实读` / `外推`**：
> · **实读** = 本会话打开了对应的反编译产物 / 指令流，逐句核过，并给出地址或行号；
> · **外推** = 反编译产物在该处被破坏，结论来自上下文或 ILSpy 的重建，**动手前请再核一次**。
> ⛔ **查不到就写查不到**（§9），不拿「大概」填空。

---

## §0 判据源与三条必须先知道的硬事实

### 0·1 判据源（按优先级）

| 顺位 | 源 | 路径 | 本轮用法 |
|---|---|---|---|
| 1 | **VA 指令流** | `D:/2/unity_run_ref/GameAssembly.dll` + `工具/disasm_va.py` / capstone | 浮点算式、跳转表、常量 |
| 2 | **Ghidra `.c`** | `d:/2/tools/decomp_full/*.c` | 分支顺序、字段偏移、调用谁 |
| 3 | **cpp2il（按类）** | `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/*.cs` | **真类型名 / 真方法名 / 字符串**（Ghidra 那侧全被同形合并污染） |
| 4 | **签名桩** | `d:/2/Warpforge_code/Scripts/Assembly-CSharp/*.cs` | 方法签名、枚举值 |
| 5 | **il2cppdumper** | `d:/2/tools/il2cpp_out/dump.cs`（**字段偏移**）· `script.json`（**RVA→方法名**）· `stringliteral.json`（**字面量**） | 偏移↔字段名、地址↔方法名、日志文案 |

> 🔴 **`script.json` 的 `Address` 就是 RVA**；`VA = 0x180000000 + Address`。
> 本轮所有「RVA 0x93xxxx」都是这么来的，可直接喂 `disasm_va.py` 复现。

### 0·2 硬事实①：`.c` 的名字不可信（G4 已知，本轮复核过本批文件）

本批 8 个目标 `.c` **都不在** `G4_文件名与内容不符_全量.tsv` 清单里 ⇒ 文件名可信。
**但调用的 helper 名仍被同形合并污染**：`AI__ScoreFromCriteria.c` 里出现的
`NetworkingPeer__OnMessage` / `System_Collections_Generic_List<AnimatorClipInfo>__get_Item` /
`ItemDrawerOptions___ctor` / `CardScript__CanPlayWithCodexNow` **全是错名**
（真名分别是 `il2cpp_codegen_object_new` / `List<T>.get_Item` / `CardEffect..ctor` / 见 §1）。
⇒ **`.c` 只拿来读「控制流 + 字段偏移」，方法名一律用 cpp2il / `script.json` 交叉。**（实读）

### 0·3 硬事实②（🔴 本轮最重要的发现）：`AI__ScoreFromCriteria.c` **不是「完整 367 行」**

G3 §3·A #3 与 §四·4 写「`.c` 完整 367 行，Ghidra 已建 `LAB_18093b139` 跟到了」——
**结构跟到了，算式没跟到**。读过 367 行就能看到：

- 函数签名被反编译成 **`void`**（`:2`），而真函数**返回 float**（调用点 `AI__GetActionScore.c:85` 拿它当 float 用）；
- **每一处浮点累加都丢了**：
  - `case 1`（`:114-123`）两次 `Where(...).Count(...)` 的结果**被丢弃**，`baseValueFriend/Enemy` 的乘加整段消失；
  - `case 2`（`:126-133`）`CardScript__GetValueInPlay(...)` 的返回值被丢弃；
  - `case 3/4/5/…` 同理，`AI__ScoreFromDamagingUnit` / `ScoreFromHealingUnit` / `ScoreFromBestAttack` 的返回值全部丢弃；
- **函数尾（`:364-365`）只剩一句 `return;`**，真正的 `return <累加器>` 没了。

⇒ **结论**：`.c` 能告诉你「每个 `scoringOption` 走哪一支、调谁、读哪个字段」，
**不能告诉你公式**。公式只能读指令流 —— 本轮已逐 case 读出来了（§1·3），
**这是本会话新补的、原文档没有的东西**。

> 同一族的另一个坑：`AI__ScoreFromAttack.c`（93 行）**也是 `void`**，同样是「结构对、算式丢」；
> 但 `AI__ScoreFromBuffedAttack.c`（134 行）**是正常的 `float`**，可以整份当真。

### 0·4 硬事实③：cpp2il 那份 `ScoreFromCriteria` 缺的比 G3 说的更多

G3 说「`Invalid instruction ×8`，尾部跳到 `0x18093B0B5/0x18093B13F`」——
「×8」是它记的条数，**实际整段 switch 一条都没有**（§1·4 有逐项清单）。

---

## §1 `AI.ScoreFromCriteria`（RVA **0x93A290**，长度 0xEAF）

签名（cpp2il `AI.cs:2774` + `dump.cs` 偏移表核对）：
```csharp
private float ScoreFromCriteria(BattleManager manager, CardScript thisCard,
                                CardScript targetCard, ScoringCriteria criteria)
```
寄存器约定（从序言 `:0x18093A2A7-0x18093A2C6` 读出，**实读**）：

| 寄存器 | 含义 |
|---|---|
| `rsi` | `this`（AI） |
| `rdi` | `manager` |
| `r14` | `thisCard`（**中途会被复用成 `multiplierCriteria` → 再复用成 `targets`**） |
| `rbx` | `targetCard` |
| `r15` | `criteria`（= 第 5 个实参，栈上传入） |
| `r12` | 局部容器地址；**`[r12]` = `thisCard`**（后续所有 case 都从这里取） |
| `r13d` | **multiplierCount** |
| `ebp` | 目标循环下标 |
| **`xmm7`** | **累加器（全程唯一）** |

### 1·1 前置闸门（顺序即原版顺序，⛔ 不许调换）（实读）

| # | 地址 | 判据 | 行为 |
|---|---|---|---|
| 0 | `:0x18093A36A-0x18093A3C6` | — | `dc = new _003C_003Ec__DisplayClass40_0()`；**`dc.thisCard = thisCard`**（`+0x10`） |
| 1 | `:0x18093A3D3` | `criteria == null` | `return 0f`（`:0x18093A42B xorps xmm0,xmm0`） |
| 2 | `:0x18093A3D8` | `criteria.codexEffect(+0x58) == 0` | 跳过（跳 `0x18093A46A`） |
| 3 | `:0x18093A3F2` | `thisCard.CanPlayWithCodexNow() == false` | `Log("Ignoring perfection scoring for " + thisCard.cardName)`；**`return 0f`** |
| 4 | `:0x18093A46A` | `criteria.multiplierCriteria (+0x30) == null` | 抛（`FUN_1803f47a0`，即 NRE） |
| 5 | `:0x18093A4CA` | `criteria.targetCriteria (+0x28)` 见 §1·2 | 用来取 `targets` |

> 字符串真值（`stringliteral.json` @ RVA `0x425EE00`）：**`"Ignoring perfection scoring for "`**。（实读）

### 1·2 累加器初值 `baseValue`（`:0x18093A46A-0x18093A4BD`）（实读）

```
if (UnityEngine.Object.op_Inequality(targetCard, null) == 0)      // targetCard == null
        xmm7 = criteria.baseValueFriend;                          // +0x10
else if (thisCard.side(+0x40) == targetCard.side(+0x40))
        xmm7 = criteria.baseValueFriend;                          // +0x10
else    xmm7 = criteria.baseValueEnemy;                           // +0x14
```
🔴 **`baseValue` 是无条件加的** —— 没有「`targets.Count > 0` 才加」的闸
（`ScoringCriteria.baseValueFriend` 的 `[Tooltip("baseValue is always added if targetCriteria finds at least one target")]`
只是设计者的话，**代码里没有这个判断**）。**实读**。

### 1·3 `multiplierCount` 与 `targets`（`:0x18093A4BD-0x18093A569`）（实读）

```
r13d = 1;
if (criteria.multiplierCriteria.targetsAffected(+0x10) != 0)      // 0 = TargetsAffected.any
    r13d = AbilityLogic.GetTargets(manager, multiplierCriteria, thisCard, thisCard, targetCard, true).Count;

targets = AbilityLogic.GetTargets(manager, criteria.targetCriteria, thisCard, thisCard, targetCard, true);
```
⚠️ `targets` 是 **`AbilityLogic.pooledPossibleTargets`（static 池）那一份**，见 §5·3 ——
**调用方不许持有它**（下一次 `GetTargets` 会把它清空重填）。

### 1·4 switch 分派（`:0x18093A56C-0x18093A58B`）（实读）

```
cmp eax, 0x23 ; ja default
lea r8, [ImageBase]                                   ; r8 = 0x180000000
edx = dword [r8 + rax*4 + 0x93B148]                   ; 4 字节 **RVA** 跳转表
jmp r8 + rdx
```
**跳转表 = RVA `0x93B148`，0x24 个 4 字节项**。解出来的目标（本会话现解，可直接复跑）：

| `scoringOption` | 目标 | `scoringOption` | 目标 |
|---:|---|---:|---|
| 0 | `0x18093ABCF`（= 公共出口） | 18 | `0x18093ABCF` |
| 1 | `0x18093A58D` | 19 | `0x18093ABCF` |
| 2 | `0x18093A656` | **20** | **`0x18093ADAC`** |
| 3 | `0x18093A6ED` | 21–24 | `0x18093ABCF` |
| 4 | `0x18093A81D` | **25** | **`0x18093AE47`** |
| 5 | `0x18093A8F2` | 26–29 | `0x18093ABCF` |
| 6 | `0x18093AA73` | **30** | **`0x18093AED9`** |
| 7 | `0x18093AB10` | 31–34 | `0x18093ABCF` |
| 8 | `0x18093ABA7` | **35** | **`0x18093AF8F`** |
| 9 | `0x18093ABD7` | default(`>0x23`) | `0x18093B0B5` |
| 10 | `0x18093AC09` | 11 | `0x18093AC34` |
| 14 | `0x18093AD1A` | 15–17 | `0x18093ABCF` |
| 12 | `0x18093ACC7` | 13 | `0x18093AD01` |

`default` 支：`if (option == 0x28 /*40*/) → 0x18093B0BE`，否则 → `0x18093ABCF`（只返回累加器）。
**公共出口 `0x18093ABCF` = `xmm0 = xmm7; ret`**（`:0x18093ABCF-0x18093ABD2`）。（实读）

### 1·5 每个 case 的完整公式（**逐指令读出**）

记法：`acc` 初值 = §1·2 的 `baseValue`；`M = r13d`（multiplierCount）；
`mul(t) = t.side == thisCard.side ? criteria.multiplyByFriend(+0x18) : criteria.multiplyByEnemy(+0x1C)`。

| option | 名字（`ScoringCriteria.ScoringOption`） | 公式（实读） | 地址 |
|---:|---|---|---|
| **0** | `FixedValue(0)` | `return baseValue`（**不乘任何系数**） | `0x18093ABCF` |
| **1** | `Count(1)` | `baseValue + (nFriend·multiplyByFriend + nEnemy·multiplyByEnemy) · M`，其中 `nFriend = #targets with t.side == thisCard.side` | `0x18093A58D-0x18093A651` |
| **2** | `SumValueInPlay(2)` | `baseValue + Σ_t GetValueInPlay(t) · M · mul(t)` | `0x18093A656-0x18093A6E8` |
| **3** | `SumValueOfDamage(3)` | `baseValue + Σ_t ScoreFromDamagingUnit(t, DamageInfo(thisCard, dmg, damageCriteria.damageType(+0x1C), -1, 0)) · mul(t)`，`dmg = DamageCriteria.GetAverageDamage(damageCriteria, thisCard, t, M)` —— 🔴 **注意：这一支的 `M` 进了 `GetAverageDamage`，Σ 里【不再】乘 M** | `0x18093A6ED-0x18093A818` |
| **4** | `SumValueOfHealing(4)` | `baseValue + Σ_t ScoreFromHealingUnit(t, heal) · mul(t)`，`heal = HealCriteria.GetAverageValue(healCriteria, thisCard, t, M)`（同上，M 已经进了 Average） | `0x18093A81D-0x18093A8ED` |
| **5** | `ValueOfBuffingAttack(5)` | 遍历 targets，**先过 `t.mightAct` 闸**（假就跳过）；`baseValue + Σ_t [ScoreFromBestAttack(thisCard, criteria.buffEffect(+0x48)) − ScoreFromBestAttack(thisCard, 空 CardEffect)] · M · mul(t)` | `0x18093A8F2-0x18093AA6E` |
| **6** | `CurrentAttack(6)` | `baseValue + Σ_t t.CurrentMeleeAttack · M · mul(t)` | `0x18093AA73-0x18093AB0B` |
| **7** | `SumValueOfJam(7)` | `baseValue + Σ_t ScoreFromJam(t) · M · mul(t)` | `0x18093AB10-0x18093AB41` |
| **8** | `RandomValue(8)` | `acc + (float)manager.GetRandomInt(0, 100)`（**不乘 M、不乘 mul**） | `0x18093ABA7-0x18093ABCC` |
| **9** | `FixedValueIfCountGreaterThanBaseEnemy(9)` | `targets.Count > (int)baseValueEnemy ? baseValueFriend : 0f`（**覆盖 acc，不是加**） | `0x18093ABD7-0x18093AC04` |
| **10** | `FixedValueIfCountLowerThanBaseEnemy(10)` | `(int)baseValueEnemy > targets.Count ? baseValueFriend : 0f`（同上，覆盖） | `0x18093AC09-0x18093AC2F` |
| **11** | `ValueOfBuffing(11)` | `baseValue + Σ_t ScoreFromBuff(t, criteria.buffEffect(+0x48)) · M · mul(t)` | `0x18093AC34-0x18093ACC2` |
| **12** | `ValueOfBuffingCost(12)` | `baseValue + ScoreFromHandBuff(manager, thisCard, targets, criteria) · M`（**整表一次调用，不逐目标；不乘 mul**） | `0x18093ACC7-0x18093ACFC` |
| **13** | `ValueOfBuffingMana(13)` | `baseValue + ScoreFromManaBuff(manager, thisCard, criteria) · M`（同上） | `0x18093AD01-0x18093AD18` |
| **14** | `SumValueOfStun(14)` | `baseValue + Σ_t ScoreFromStun(t) · M · mul(t)` | `0x18093AD1A-0x18093ADA7` |
| **20** | `SumValueOfPoison(20)` | `baseValue + Σ_t ScoreFromPoisoning(t) · M · mul(t)` | `0x18093ADAC-0x18093AE42` |
| **25** | `SumValueOfReturnToHand(25)` | `baseValue + Σ_t ScoreFromReturnToHand(t) · M · mul(t)` | `0x18093AE47-0x18093AED4` |
| **30** | `SumValueOfGivingCharge(30)` | `baseValue + Σ_t ScoreFromGivingCharge(t) · M · mul(t)`（`ScoreFromGivingCharge` 被**内联**：`if (!t.CanActNow() && t.summonSickness(+0x58)) → t.CurrentMeleeAttack; else 0`） | `0x18093AED9-0x18093AF8A` |
| **35** | `ValueOfDrawingCard(35)` | 见 §1·6 | `0x18093AF8F-0x18093B0B0` |
| **40** | `ValueOfChangeWeapon(40)` | `baseValue + Σ_{t : t.rawCard.maxHealth(+0x9C) == 0} mul(t)`（**只加系数本身，不乘任何数**；`maxHealth != 0` 的目标直接跳过） | `0x18093B0BE-0x18093B13D` |
| 其它 | 15–19/21–24/26–29/31–34 及越界 | `return baseValue` | `0x18093ABCF` |

> 🔑 关键模式（除 1/8/9/10/12/13/40 外全部一致）：
> **`acc += f(t) · M · mul(t)`**。`M` 只有在 `multiplierCriteria.targetsAffected != any` 时才 ≠ 1。（实读）

### 1·6 `option 35 ValueOfDrawingCard` 逐句（`0x18093AF8F`）（实读）

```
xmm6 = (thisCard.side(+0x40) == 0) ? criteria.multiplyByFriend(+0x18) : criteria.multiplyByEnemy(+0x1C)
   ↑ ⚠️ 比的是 **thisCard.side == 0**，不是「目标阵营」（这一支没有目标）
deck    = manager.GetDeck(thisCard.side)                          // 0x180968710
delta   = manager.MaxCardsInHand() + (~deck.__size ... 见下)
   MaxCardsInHand()  → 0x18096F630
   hand   = manager.GetCardsInHandRef(thisCard.side)              // 0x180966CC0
   delta  = MaxCardsInHand() + (~hand.Count)  = MaxCardsInHand() - hand.Count - 1
if (deck.Count == 0)
   delta = -2 - (thisCard.side == 0 ? manager.enemyFatigue(+0x38C) : manager.playerFatigue(+0x378))
          // ObscuredInt::op_Implicit(@0x180579E40)
    // ⚠️ 牌库空这一支【不带】上面的 +acc：`xmm1` 被 `-2-fatigue` 覆盖（`:0x18093B092-0x18093B096`）
xmm1 = (float)delta + acc;                 // :0x18093B083  addss xmm1, xmm7
return acc + M * xmm6 * xmm1               // :0x18093B0A1 mulss / :0x18093B0A5 mulss / :0x18093B0A9 addss xmm0, xmm7
```
🔴 **`acc`（= `baseValue`）在这一支里出现了两次** —— 一次进乘法（`delta + acc`）、一次加在外面。
字面式 = **`base + M·mult·(delta + base)`**。
（可能对应原版 C# 的 `return baseValue + ScoreFromDrawingCard(manager, thisCard, baseValue) * M * sideMul;`
—— `ScoreFromDrawingCard(manager, card, score)` 自己就是 `score + delta`。**这一层是外推**，
`addss xmm1, xmm7` 这条指令本身是**实读**。）
⚠️ 我方 `AiScoring.cs:265` 写的是 `base + M·mult·delta`（少了乘法里的那个 `base`）⇒ 见 §8 R6-10。
🔴 **疲劳那两个字段是「按本地人类玩家视角」命名的**：`isPlayer==0`（AI 那一侧）取
`enemyFatigue(+0x38C)`，`isPlayer==1` 取 `playerFatigue(+0x378)` —— 取的是**自己那一方**的疲劳。
（`dump.cs` BattleManager 字段表核对）**实读**。

### 1·7 cpp2il 版 `ScoreFromCriteria` 到底缺了什么（逐项）

cpp2il `AI.cs:2774-2831` 全文只有：`new DisplayClass40_0()` + `dc.thisCard = thisCard` +
`codexEffect` 闸 + `if (targetCard != null) { bool flag = targetCard.isPlayer; if (flag==0) {} }`（**空体**）+
两次 `AbilityLogic.GetTargets` + `float result = default; return result;`。

**缺的段**（对照 §1·5）：

1. **整个 `switch (criteria.scoringOption)`** —— 36 个 case 的**全部 20 个有实体的分支**（option 0–14/20/25/30/35/40）。
2. **`baseValue` 选择**（§1·2）—— cpp2il 里只剩一个空心 `if (targetCard != null) {…}`。
3. **`multiplierCount` 的计算与存寄存器**（§1·3）—— cpp2il 把它塌成了局部 `targetCriteria`，且**没有 `r13d` 这个量**。
4. **`acc += f(t)·M·mul(t)` 这条累加模式** —— 全篇没有。
5. **返回类型**：cpp2il 写成 `float result = default; return result;` ⇒ **恒 0**
   （`.c` 更糟：直接 `void`）。
6. 顺带：`ScoreFromAttack` 里那个 criteria 参数，cpp2il 塌成 `(ScoringCriteria)(object)cardEffect2`（= null），
   真值是 **`actingCard.rawCardData.attackScoringCriteria`（`RawCardScript + 0x300`）**（`AI__ScoreFromAttack.c:37`）。

> ⇒ **G3 §四·4「`.c` 已跟到 ⇒ 可直接用」这句要改成**：
> 「`.c` 只给分支骨架；**算式与返回值必须读 RVA 0x93A290 的指令流**（跳转表 RVA `0x93B148`）。」

---

## §2 `AI.GetActionScore` / `AI.ExecuteAction`

### 2·1 `AI.GetActionScore`（RVA **0x938970**，`AI__GetActionScore.c` 118 行 —— 这份**是完整的**）

签名：`private float GetActionScore(BattleManager manager, List<AvailableAction> availableActions,
                                  int actionIndex, bool calculateUnitedScore = true, AIModes mode = AIModes.normal)`

（`param_1` = AI，`param_2` = manager，`param_3` = list，`param_4` = index，`param_5`/`param_6` = bool/mode）

**分派键 = `AvailableAction.actionType`（`+0x10`）**，`AvailableAction` 偏移（签名桩顺序 + `.c` 交叉）：
`actionType +0x10` · `actingCard +0x18` · `targetCard +0x20` · `actionScore +0x28` · `manaCost +0x2C`。

| `PlayerActions` | 值 | 行为（实读） | 行号 |
|---|---:|---|---|
| `playCard` | 0 | `base = ScoreFromPlayingCard(manager, actingCard, targetCard, mode)` | `:38-44` |
| `attackMelee` | 1 | `base = ScoreFromAttack(manager, actingCard, targetCard, AttackTypes.Melee=1)` | `:45-54` |
| `attackRanged` | 2 | `base = ScoreFromAttack(…, AttackTypes.Ranged=2)` | `:48-54` |
| `useHeroPower` | 3 | **无 case** ⇒ 返回 0（原版 `GetAvailableActions` 也从不产出这一支） | — |
| `endTurn` | 4 | **无 case** ⇒ 返回 0（= 「留牌」的基准线） | — |
| `activeAbility` | 5 | 见下 | `:66-110` |
| `clickWaystone` | 6 | `return 50.0f` —— **常量 `DAT_1834b2ecc` = `0x00004842` = `50.0f`**（本会话读字节复核） | `:111-113` |
| 其它 | — | 0 | — |

**攻击支的后处理（`:55-64`，实读）**：
```
if (targetCard == null) throw;
if (targetCard.cardType(+0x94 经 get_cardType) == 10 /*Hero*/ &&
    CombinedAttackCanKillWarlord(manager, action, availableActions))
        base += 10000.0f;            // DAT_1834b32f4 = 0x00401C46 = 10000.0f（字节复核）
```
⚠️ `cardType == 10` 是 `CardTypeOptions.Hero` —— 即「督军/英雄」，与我们的 `IsWarlord` 同义。

**`activeAbility` 支（`:66-110`，实读）**：
```
ab = actingCard.GetActiveAbility();
if (ab == null) { LogWarning("Null ability for " + actingCard.cardName); }   // RVA 0x425AA58
else {
   if (ab.nonTargetScoringCriteria(+0x58) != null)
        for (i in 0..ab.nonTargetScoringCriteria.Count) base += ScoreFromCriteria(manager, actingCard, targetCard, ab.nonTargetScoringCriteria[i]);
}
// 然后（无论上面走哪一支）：
if (targetCard != null && ab != null && ab.cardTargetScoringCriteria(+0x50) != null)
     for (i in ab.cardTargetScoringCriteria) base += ScoreFromCriteria(manager, actingCard, targetCard, ab.cardTargetScoringCriteria[i]);
```
🔴 **顺序 = 先 nonTarget(+0x58)，再 cardTarget(+0x50)**；`cardTarget` 那一列**要求 `targetCard != null`**。
⚠️ `ab == null` 时 **只打一条 warning，`base` 保持 0**（该支不再累加；`cardTarget` 那段被 `ab != null` 挡住）。

### 2·2 `AI.ExecuteAction`（RVA **0x9382B0**，`AI__ExecuteAction.c` 470 行 —— **完整**）

签名：`private void ExecuteAction(BattleManager manager, AvailableAction action)`（`.c` 的 `param_2`=manager，`param_3`=action）

**分派键 = `action.actionType`（`+0x10`）**，全部动作最终都是「往 `BattleManager` 的动作队列投一条 `BattleAction`」：

| `actionType` | 做的事（实读） | `BattleActionType` |
|---|---|---|
| 0 `playCard` | 构造 `BattleAction`（`FUN_180482360` = `BattleAction` 的 0x100 字节清零 ctor）：`type = 5`、`actingCard`、`targetCard`、一个 bool=1、`transform.position`；**若 `actingCard.cardType == 0 (Minion)` 再取 `manager.minionManager(+0xE8).GetNextSlotCheckingAdjacents()` 作为落点槽号**；然后 `BattleManager.AddAutoActionToQueue(manager, pkt, manager.globalVars(+…0xB4), 0)` | **5 = `playCardFromHand`** |
| 1 `attackMelee` | `actingCard.ChangeAttackType(1)` → `BattleManager.AddAttackAction(manager, actingCard, targetCard, 1, prio, 0, 0)` | （走 attack 队列） |
| 2 `attackRanged` | `actingCard.ChangeAttackType(2)` → `AddAttackAction(…, 2, …)` | 同上 |
| 5 `activeAbility` | `actingCard.SetupActiveAbility()`；`type = 0x1E`；`AddAutoActionToQueue` | **30 = `playActiveAbility`** |
| 6 `clickWaystone` | `type = 0x4C`；`AddAutoActionToQueue` | **76 = `useWaystone`** |
| 默认 | `LogError("Can't ExecuteAction for type " + actionType)`（RVA `0x42AA270`） | — |

收尾：除「中途 `break`」的路径外，一律走 `BattleManager.PlayRandomAIVoiceLine(manager)`
（`LAB_180938911`）。**随机源 = `BattleManager.PlayRandomAIVoiceLine` 内部**（语音，不影响局面）。

> ⚠️ **`.c` 里那一大段 0x100 字节的 `do { puVar13[..] = puVar15[..] } while` 是结构体拷贝**
> （把 0x100 字节的 `BattleAction` 从 `local_208` 拷到 `local_108` / `local_108` 拷到 `local_208`），
> **不是循环逻辑**。⛔ 别照着写循环。

---

## §3 `FilterMethods.MeetsConditionCriteria` / `MeetsStatCriteria`

### 3·1 `MeetsStatCriteria`（`FilterMethods__MeetsStatCriteria.c` 34 行 —— **完整，且已逐句核**）

签名（签名桩）：`MeetsStatCriteria(int cardStat, int thisCardStat, int statFilter, StatFilterOptions statFilterOptions, int altValue = 0)`

```
if (option == 0 NotApplicable) return true;
switch (option) {
  case 1  EqualTo                      : return cardStat == statFilter;
  case 2  EqualOrHigher                : return statFilter <= cardStat;
  case 3  EqualOrLower                 : return cardStat <= statFilter;
  case 4  EqualOrLowerToThisUnitAttack : return cardStat <= thisCardStat;
  case 5  HigherToThisUnitAttack       : return thisCardStat < cardStat;
  case 6  EqualOrLowerIncludingBastion : cardStat += altValue;   // ← 原版**掉进 case 3**
  case 7  LowerToThisUnitAttack        : return cardStat < thisCardStat;
  case 9  EqualToAlt                   : return altValue == statFilter;
  case 10 EqualOrHigherAlt             : return statFilter <= altValue;
  case 11 EqualOrLowerAlt              : return altValue <= statFilter;
  // ⚠️ 没有 case 8（RandomChance）⇒ 落到 `return true`（不掷骰！）
}
return true;
```
**实读**（`.c:8-32`）。🔴 **`RandomChance(8)` 原版就是 `return true`**，⛔ 别按枚举名补 RNG。

### 3·2 `MeetsConditionCriteria`（`FilterMethods__MeetsConditionCriteria.c` 390 行 —— **完整**）

签名：`MeetsConditionCriteria(CardScript card, UnitConditions conditionsFilter, bool excludeTrait, CardScript thisCard = null)`
（`param_1`=card，`param_2`=cond，`param_3`=exclude，`param_4`=thisCard）

**尾判（`:384-389`，实读）**：`if (exclude && !result) return true; return (!exclude) & result;`

| `UnitConditions` | 值 | 判据（实读，行号 = `.c`） | 用到的 trait/keyword |
|---|---:|---|---|
| `none` | 0 | `${card:+0x54} == 0` → true；否则 `${card:+0x228} != 0` → true；否则 `LogWarning("Ignore trap " + cond)` → **false** | — `:362-378` |
| `canAttack` | 10 | `(ObscuredBool @ +0x230)` | — `:102-116` |
| `mightAttack` | 20 | `card.mightAct` | — `:82-85` |
| `damaged` | 40 | `card.GetCurrentDamage() >= 1 && !card.EnoughPendingHealingToFull()` | — `:87-97` |
| `flying` | 50 | `(has(unstoppable 180) ‖ has(flying 390)) && !has(vanguard 980)` | unstoppable/flying/vanguard `:38-45,137-144` |
| `protectedFromDamage` | 100 | `has(dodge 240) ‖ has(invulnerable 310) ‖ has(dropPod 230)` | `:46-54,151-155` |
| `protectedFromAttackDamage` | 110 | `MeetsConditionCriteria(card,100) ‖ card.HasBastionHigherThanAttack(thisCard)` | `:57-66` |
| `protectedFromDamageOrSurvivor` | 120 | `card.IsProtectedFromDamageOrSurvivor()` | `:68-72` |
| `hasAbility` | 190 | `byte @ card+0x220`（= `hasActiveAbility`） | `:73-77` |
| `dealtKillDmg` | 220 | `card.EnoughPendingDamageToDie()` | `:253-257` |
| `diedThisTurn` | 221 | `card.HasDiedThisTurn()` | `:258-262` |
| `diedSinceLastTurn` | 222 | `card.HasDiedSinceLastTurn()` | `:263-267` |
| `stealthAndNotDealtKillDmg` | 230 | `has(stealth 70) && !card.EnoughPendingDamageToDie()` | `:268-277` |
| `hasSecretOrder` | 240 | `card.HasSecretOrders() > 0` | `:281-285` |
| `hasUseSpiritStone` | 241 | `card.rawCard.HasAbilityType(600)` | `:286-289` |
| `hasUseFaith` | 242 | `card.rawCard.HasAbilityType(605)` | `:290-293` |
| `isPraying` | 243 | `byte @ card+0x67` | `:294-297` |
| `hasArmour` | 244 | `(ObscuredInt @ +0xB8) > 0` | `:298-311` |
| `hasAttackValues` | 245 | `card.CurrentMeleeAttack >= 1 ‖ card.CurrentRangeAttack > 0` | `:312-322` |
| `hasQuests` | 250 | `card.HasQuests() > 0` | `:323-327` |
| `costEqualToAvailableEnergy` | 260 | `card.CurrentCost == PlayerManager.GetUnusedMana(manager@+600, card.side)` | `:329-342` |
| `isWarlordEquivalent` | 270 | `card.IsWarlordEquivalent()` | `:343-349` |
| `isWarlordOrVehicle` | 275 | `card.IsWarlordEquivalent() ‖ rawCard.IsKeyword(Vehicle=10)` | `:183-203` |
| `isBattlesuitOrVehicle` | 276 | `rawCard.IsKeyword(Battlesuit=25) ‖ rawCard.IsKeyword(Vehicle=10)` | `:191-203` |
| `isInfrantryOrBeast` | 277 | `rawCard.IsKeyword(Infantry=5) ‖ rawCard.IsKeyword(Beast=35)` | `:204-216` |
| （278 / 279 = `0x116`/`0x117`） | — | **原版没有这两格** ⇒ 落 default（`LogError` + false） | `:217-219` |
| `siege` | 280 | `card.DoesAbilityRequireLoading()` | `:220-225` |
| `hasQuestCompleted` | 320 | 复杂：`!has(jam 130)` 时若 `rawCard+0x240 != 0 && HasQuests() <= 0` → 记 `true`；否则 `false`；**并 `Log("check if quest completed " + bool)`** | `:227-248` |
| `stealthOrWardOrDuplicitous` | 330 | `any(stealth 70, ward 140) ‖ (has(t两家 160) && !has(vanguard 980))` | `:124-145` |
| `stealthOrCamouflage` | 331 | `any(stealth 70, camouflage 985)` | `:146-156` |
| `onlyRemnants` | 340 | `card+0x65 == 0 ‖ card+0x66 != 0` → false；否则 `SupportMethods.IsNecronRemnant(card)` | `:157-169` |
| `canUseDuty` | 341 | `byte @ card+0x4C == 0` | `:170-174` |
| `summonSickness` | 342 | `byte @ card+0x58` | `:175-179` |
| 其它 / 越界 | — | `LogError("Unhandled trait to check if meets criteria " + cond)` → false | `:350-361` |

🔴 **`UnitConditions` 35 个成员里有 3 个原版【没有实现】**（不在任何 case 里，落 `default` → LogError + false）：
**`bloodAngelsOrUltramarines(290)` · `mutation(300)` · `specialQuest(310)`**。
（`278`/`279` 在枚举里本来就没有名字，也落同一支。）⇒ **数据侧若出现这 3 个值，原版行为 = 永远 false**，
⛔ 别按名字猜语义去实现。

> 归档口径（`UnitConditions` 枚举，`d:/2/Warpforge_code/Scripts/Assembly-CSharp/UnitConditions.cs`）。

### 3·3 `FilterMethods.CheckIfMeetsCriteria`（`FilterMethods__CheckIfMeetsCriteria.c` 515 行 —— **完整**）

签名：`CheckIfMeetsCriteria(CardScript thisCard, CardScript evaluatedCard, CardScript targetCard, TargetCriteria targetCriteria)`

**这是 AI 选目标的唯一总闸**（`AbilityLogic.GetTargets` 的唯一过滤调用点，§5·3）。
分支顺序（**实读，顺序即语义**）：

| 步 | 判据（`.c` 行号） |
|---|---|
| 0 | `criteria == null` → `LogError("No target criteria defined")`，**false**（`:52-55`） |
| 1 | `targetsAffected == 0 (any)` → **true，立刻返回**（`:57-59`） |
| 2 | `targetsAffected == 20 (self)` → `evaluated == thisCard`（`:60-75`） |
| 3 | `targetsAffected == 30 (target)` → `evaluated == targetCard`（`:76-91`） |
| 4 | `targetsAffected == 200 (referencedCardInThisCard)` → `thisCard.referencedCards(+0x278).Contains(evaluated)`（`:92-107,495-509`） |
| 5 | `targetsAffected == 201 (referencedInPlayCardInThisCard)` → 同上 **∧ `evaluated.IsInPlay()`**（`:109-125`） |
| 6 | `targetsAffected == 235 (warlordsTalent)` → `manager.GetHero(thisCard.side).GetTalentCard().uniqueId == evaluated.rawCard.uniqueId`（`:126-157`） |
| 7 | `evaluated == null` → `LogWarning("Null evaluated card")`，**false**（`:158-166`） |
| 8 | `targetsAffected == 80 (actingCard)` → **true**（`:167-170`） |
| 9 | `targetsAffected == 150 (previousAbilityFiltered)` → 必须在 `AbilityLogic.previousAbilityTargets` 静态表里（`:171-183`） |
| 10 | `targetsAffected ∈ {70 abilityOwner, 40 attacker}` → `evaluated == thisCard`（`:184-192`） |
| 11 | `targetsAffected == 90 (targetIfMeetsCriteria)` → 必须 `evaluated == targetCard`（`:193-202`） |
| 12 | `criteria.excludeSimilarCards(+0x1F)` → `evaluated.rawCard == thisCard.rawCard` ⇒ **false**（`:203-215`） |
| 13 | **阵营闸**：`evaluated.side == thisCard.side` ⇒ 需要 `criteria.friendlyUnits(+0x14)`；否则需要 `enemyUnits(+0x15)`；不满足 ⇒ false（`:216-228`） |
| 14 | `evaluated == thisCard && !includeSelf(+0x1D)` ⇒ false（`:229-235`） |
| 15 | `evaluated != thisCard && excludeNotSelf(+0x1E)` ⇒ false（`:236-243`） |
| 16 | `excludeFriendlyHero(+0x21)` ∧ 目标是督军 ∧ 同阵营 ⇒ false（`:244-256`） |
| 17 | `excludeEnemyHero(+0x22)` ∧ 目标是督军 ∧ 异阵营 ⇒ false（`:257-272`） |
| 18 | `!includeRemnants(+0x1B)` ∧ `evaluated+0x65 != 0`（残骸）⇒ false（`:273-278`） |
| 19 | **卡种闸**（`rawCard.cardType(+0x94)`）：`20 Tactic` 需 `includeSpells(+0x18)`；`10 Hero` 需 `includeHero(+0x16)`；`0 Minion` 需 `includeMinions(+0x17)`；`40 Whispers` 需 `includeWhispers(+0x19)`；否则 false（`:279-293`） |
| 20 | `spellType(+0x68) != 0` ∧ `rawCard.spellType(+0x90) != criteria.spellType` ⇒ false（`:294-297`） |
| 21 | `MeetsConditionCriteria(evaluated, conditionsFilter(+0x38), excludeCondition(+0x3D), thisCard)` 假 ⇒ false（`:298-303`） |
| 22 | 关键词闸 `keywordFilter(+0x2C)` / `excludeRace(+0x30)`（`:304-312`） |
| 23 | `MeetsTraitCriteria(evaluated, traitsFilter(+0x34), excludeTrait(+0x3C), thisCard)` 假 ⇒ false（`:313-319`） |
| 24 | `MeetsStatCriteria(evaluated.CurrentMeleeAttack, thisCard.CurrentMeleeAttack, attackFilter(+0x48), attackFilterOption(+0x4C), alt = evaluated.CurrentRangeAttack)` 假 ⇒ false（`:320-332`） |
| 25 | `MeetsStatCriteria(evaluated.health(ObscuredInt@+0x68), thisCard.CurrentMeleeAttack, currentHealthFilter(+0x50), currentHealthFilterOption(+0x54), alt = evaluated.GetCurrentTraitValueWithModifiers(850 bastion))` 假 ⇒ false（`:333-355`） |
| 26 | `MeetsStatCriteria(evaluated.CurrentCost, thisCard.CurrentMeleeAttack, manaCostFilter(+0x58), manaCostFilterOption(+0x5C), alt = 0)` 假 ⇒ false（`:356-366`） |
| 27 | `MeetsStatCriteria(rawCard.rarity(+0x28), thisCard.CurrentMeleeAttack, rarityFilter(+0x60), rarityFilterOption(+0x64), alt = 0)` 假 ⇒ false（`:367-377`） |
| 28 | `filterSpecificCards(+0x70)` 非空 ⇒ `evaluated.uniqueId` 必须在其中（`:385-484`） |
| 29 | `excludeEnchantments(+0x78)` 非空 ⇒ 该卡的 activeEnchantments 里**没有**同名卡（`:394-463`） |
| 30 | `MeetsArmyCriteria(rawCard.army(+0x2C), chosenArmy(+0x24), armyFilter(+0x28))` 假 ⇒ false（`:397-406`） |
| 31 | `targetsAffected != 100` → **true**（`:407-409`） |
| 32 | `targetsAffected == 100 (adjacentToSelfIfMeetsCriteria)` → `evaluated ∈ manager.GetAdjacentUnits(thisCard)`（`:410-427`） |

🔴 **第 1 步 `any` 直接 `return true`** —— 所有其它过滤都不跑（与 `TargetCriteria` 的
`[Tooltip("'Any' disregards all other filters")]` 一致）。
⚠️ **`includeWeapons(+0x1A)` / `includeHeroParts(+0x1C)` 在代码里【没有任何读取点】**（`.c` 全文 0 处命中）。

---

## §4 `Evaluation.Check`（`Evaluation__Check.c` 123 行 —— **完整**）

签名：`public static bool Check(object x, object y, Operation operation)`

⚠️ 🔴 **它不是「胜负/威胁评估」** —— 本会话查了全量反编译，**唯一调用点**是
`Everguild.LiveOps.AttributeCondition.Validate`（LiveOps 活动条件）。
**与战斗 AI 无关**，`AI` 类里没有任何调用。任务简报里「（胜负/威胁评估）」这个描述要改。（实读，`grep -rl Evaluation__Check` = 2 个文件）

行为（实读，`.c:25-116`）：

| `Operation` | 值 | 行为 |
|---|---:|---|
| `Contains` | 6 | `x.ToString().Contains(y.ToString())` |
| `DontContain` | 7 | `!Contains` |
| `Equals` | 2 | **引用相等** `x == y`（⚠️ 不是 `Equals`，是 `object.ReferenceEquals` 语义） |
| `NotEquals` | 3 | `!(x == y)` |
| `LessThan` | 0 | `TryParseFloat(x) < TryParseFloat(y)` |
| `LessThanEqualTo` | 1 | `<=` |
| `GreaterThan` | 4 | `y < x` |
| `GreaterThanEqualTo` | 5 | `y <= x` |
| 其它 | — | false |

`Operation` 枚举值有一个**命名错位**（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/Evaluation.cs`）：
`GreaterThan = 4`、`GreaterThanEqualTo = 5`，而 `EnumMember(Value=">=")` 挂在 **4**、`">"` 挂在 **5**。
⇒ **按数值用，别按 `EnumMember` 字符串用**。（实读）

---

## §5 `AbilityLogic`

### 5·1 `GetValueFromCriteria`（`AbilityLogic__GetValueFromCriteria.c` 366 行 —— **完整**）

签名：`GetValueFromCriteria(BattleManager manager, int minValue, int maxValue, CardScript thisCard,
                         CardScript targetCard, ChooseValueOptions valueOptions, int multiplierFactor = 1, CardScript refCard = null)`
寄存器：`param_2`=manager · `param_3`=min · `param_4`=max · `param_5`=thisCard · `param_6`=targetCard ·
`param_7`=option · `param_8`=multiplier · `param_9`=refCard。（`param_1` = this，**函数体里没用到**）

| `ChooseValueOptions` | 值 | 返回（实读） | 行 |
|---|---:|---|---|
| `Normal` | 0 | `min == max ? min : manager.GetRandomInt(min, max+1)` ← **随机源 = `BattleManager.GetRandomInt`（种子可复现）** | `:38-45` |
| `TargetMeleeAttack` | 10 | `targetCard.CurrentMeleeAttack` | `:46-51` |
| `TargetRangeAttack` | 20 | `targetCard.CurrentRangeAttack` | `:52-55` |
| `TargetHealth` | 30 | `targetCard` 的 `ObscuredInt @ +0x68`（当前血） | `:57-73` |
| `ThisCardAttack` | 40 | `thisCard.CurrentMeleeAttack` | `:74-79` |
| `ThisCardAttackRanged` | 41 | `thisCard.CurrentRangeAttack` | `:81-110` |
| `ThisCardHealth` | 50 | `thisCard` 的 `ObscuredInt @ +0x68` | `:83-99` |
| `TargetDamage` | 60 | `targetCard.GetCurrentDamage()` | `:112-120` |
| `ThisCardDamage` | 70 | `thisCard.GetCurrentDamage()`（`:112` 的 `param_6 = param_5` 那半句） | `:112-120` |
| `MinPlusMaxTimesMultiplier` | 80 | `min + max * multiplier` | `:113-116` |
| `RandomizedOnResolution` | 90 | **`return minValue`**（原版就是原样返回，不做随机） | `:130-132` |
| `RefCardAttack` | 100 | `refCard` 的 `ObscuredInt @ +0x7C` | `:133-155` |
| `BaseRefCardMAttack` | 101 | `thisCard.rawCardData(+0x280) +0xA4` 那个 int | `:157-170` |
| `RefCardHealth` | 110 | `refCard` 的 `ObscuredInt @ +0x68` | `:172-193` |
| `RefCardCost` | 120 | `refCard.CurrentCost` | `:194-209` |
| `TargetCost` | 130 | `targetCard.CurrentCost` | `:211-241` |
| `OwnWarlordAttack` | 140 | `manager.GetHero(thisCard.side).CurrentMeleeAttack * multiplier` | `:212-221` |
| `TargetBastion` | 150 | `targetCard.CurrentBastion`（`min != 0` 时 `min * value`） | `:222-234` |
| `SelfBastion` | 160 | `thisCard.CurrentBastion` | `:243-252` |
| `TargetCardHealthMinusAttack` | 170 | `targetCard.health - targetCard.CurrentMeleeAttack` | `:253-276` |
| `ThisCardMarksOfChaos` | 200 | `min + thisCard.CurrentMarksOfChaos * max` | `:278-282` |
| `OtherCardMarksOfChaos` | 201 | `min + targetCard.CurrentMarksOfChaos * max` | `:283-287` |
| `AllFriendlyMarkOfChaos` | 202 | `min + (Σ 同阵营在场单位 `GetCurrentTraitValueWithModifiers(330 markOfChaos)`) * max` | `:288-319` |
| `ThisCardShuriken` | 203 | `min + thisCard.CurrentShuriken * max` | `:320-324` |
| `FaithValue` | 204 | `manager.GetPlayerManager(thisCard.side).GetCurrentFaith()` | `:325-332` |
| `TargetExcessDamage` | 210 | `health >= 0 ? 0 : -(health * min)` | `:333-359` |
| 其它 | — | `minValue` | `:364` |

**通用收尾规则**（`.c:121-126`，实读）：多数分支在返回前有
`if (param_3 == 0) return value; else return param_3 * value;`（`min == 0` ⇒ 原值，否则 `min × value`）。
⚠️ 上面表里没标 `× min` 的那几支（如 80 / 200–203 / 210）是**各自算好的**，不受这条影响。

### 5·2 `CanPlayAbility`（`AbilityLogic__CanPlayAbility.c` 76 行 —— **完整**）

签名：`CanPlayAbility(BattleManager manager, AbilityData abilityData)`（`param_1` = AbilityLogic `this`）

```c
switch (this.chosenAbility(+0x10)) {
  case doDamage(10) / doDamageMultiRandom(12) / doDamageFast(13) / doDamageAndDestroySelf(15):
      return GetTargets(manager, this.targetCriteria(+0x28), abilityData, checkMaxTargets:true).Count > 0;
  case doHeal(20):
      return GetTargets(manager, this.targetCriteria, abilityData.thisCard(+0x10),
                         abilityData.actingCard(+0x18), abilityData.targetCard(+0x20), true).Count > 0;
  case buffOthers(30) / buffOthersNotResetPool(31):
      return GetTargets(manager, this.targetCriteria, abilityData, true).Count > 0;
  default:
      return true;     // ← 其余【全部】直接 true（含 drawCard(0)、doOnlyVfx(3)、buffSelf(40)…）
}
```
**实读**。🔴 「默认 true」这条很要紧：**只有那 7 档需要目标**，其余技能永远「放得出」。

### 5·3 `GetTargets`（`AbilityLogic__GetTargets.c` 1427 行）

两个重载（签名桩）：
```csharp
static List<CardScript> GetTargets(BattleManager, TargetCriteria, AbilityData, bool checkMaxTargets = true);
static List<CardScript> GetTargets(BattleManager, TargetCriteria, CardScript thisCard,
                                   CardScript actingCard, CardScript targetCard, bool checkMaxTargets = true);
```

**① 返回的是静态池（🔴 最容易踩的一条）**（实读 `:76-86`）
```
pooled = AbilityLogic.pooledPossibleTargets;          // 静态字段（dump.cs：AbilityLogic // 0x0）
if (pooled == null) throw;
n = pooled.Count; pooled._version++; pooled.Count = 0;
if (n > 0) Array.Clear(pooled._items, 0, n);
...                                                      // 往【同一个 pooled】里填
return pooled;
```
⇒ **`GetTargets` 的返回值不是新表**：下一次 `GetTargets` 会把上一次的结果清空。
⛔ 调用方不许缓存 / 不许跨调用持有。**实读**。

**② 分派键 = `criteria.targetsAffected(+0x10)`**，Ghidra 把它摊成了一串 `if`（**不是 switch**，
除 `:1171` 那一处 `switch(criteria.targetSelectMethod(+0x44))`）。全量分支表：

| 值 | `TargetsAffected` | `.c` 行 |
|---:|---|---|
| 180 | `choiceResolution` | `:89-171` |
| 190 | `lastPlayedIfMeetsCriteria` | `:173-183` |
| 20 | `self` | `:184-191` |
| 30 / 220 | `target` / `targetCountTraits` | `:192-207` |
| 160 / 161 / 162 | `currentMana` / `currentSpiritStone` / `currentFaith` | `:220-522` |
| 245 | `secretsPlayed` | `:523-623` |
| 170 | `maxMana` | `:624-720` |
| 120 / 130 | `friendlyHero` / `enemyHero` | `:721-746` |
| 90 | `targetIfMeetsCriteria` | `:747-759` |
| 80 / 40 | `actingCard` / `attacker` | `:760-783` |
| 201 | `referencedInPlayCardInThisCard` | `:784-808` |
| 230 | `highestHealthWarlord` | `:809-834` |
| 240 | `lowestHealth` | `:835-844` |
| 70 | `abilityOwner` | `:845-848` |
| 140 | `previousAbilityTargets` | `:849-866` |
| 150 | `previousAbilityFiltered` | `:867-883` |
| 0 | `any` | `:884-892` |
| 50 / 51 / 52 / 55 / 60 / 65 | `inHandFiltered` / `inHandAndDeckFiltered` / `inHandAndPlayFiltered` / `inPlayOrDyingFiltered` / `inCemeteryFiltered` / `inDeckFiltered` | `:893-971` |
| 10 / 210 | `allFiltered` / `allFilteredCountTraits` | `:972-990` |
| 55 | `inPlayOrDyingFiltered`（第二处） | `:991-1000` |
| 100 | `adjacentToSelfIfMeetsCriteria` | `:1001-1009` |
| 105 / 106 / 110 / 113 / 115 | 五个 `adjacentTo…` | `:1010-1160` |

**③ 通用尾链**（`:1085-1373`，实读，顺序即语义）：
```
1) 倒序过一遍池子：FilterMethods.CheckIfMeetsCriteria(thisCard, cand, targetCard, criteria) 为假 → 从池里删掉
     （:1095；删的时候还会走 code_r0x000180818efb：excludeSimilarCards 那一档会按 rawCard.uniqueId 再删一遍）
2) if (criteria.maxTargets(+0x40) >= 1 && pooled.Count > maxTargets)     // :1164-1170
      switch (criteria.targetSelectMethod(+0x44)) {                       // :1171
        case 1 highestAttack      : OrderBy(...).ThenBy(...)              // :1172-1207
        case 2 highestValueInPlay : OrderByDescending(...)                // :1208-1228
        case 3 lowestValueInPlay  : OrderBy(...)                          // :1229-1249
        case 6 lowestHealth       : OrderBy(...)                          // :1311-1331
        case 5 lastAdded          : 取【末尾】maxTargets 条                // :1274-1310
        case 7 firstAdded         : 取【开头】maxTargets 条                // :1332-1360
        default (含 0 random / 4 cardTarget):
              while (i < maxTargets) { k = manager.GetRandomInt(0, pooled.Count); 取走 pooled[k] 并从池里 RemoveAt(k); }
              // :1250-1273 —— 随机源 = BattleManager.GetRandomInt（种子可复现）
      }
      最后 Enumerable.Take(maxTargets).ToList()                            // :1365-1366
```
⚠️ `case 4 cardTarget` **没有单独分支**，落到 `default` = **随机挑**（实读；Ghidra 的 switch 分支表里确实没有 4）。

**④ 本会话【没读】的部分（如实标注）**：上面 ①②③ 之外，**每一个分支内部怎么填池子**
（例如 `allFiltered` 怎么枚举全场、`inHandFiltered` 怎么取手牌 + 附魔判据）**只到「调了哪个管理器方法」这一层**，
没有逐句核到「候选集怎么拼」。判据若要到那一层，得逐支读 —— 见 §9。

---

## §6 打分族子函数（`ScoreFromCriteria` 与被它调用的那些）

> 全部 **实读**（cpp2il 逐句 + `.c` / 指令流交叉），除非另有标注。
> 这些是我们 `Data/AiScoring.cs` / `Data/SimpleAI.cs` 直接对应的函数。

### 6·1 真值常量表（本会话读 `GameAssembly.dll` 字节复核）

| 常量 | RVA | 字节 | 值 | 用处 |
|---|---|---:|---:|---|
| `DAT_1834b32f4` | `0x34B32F4` | `00 40 1C 46` | **10000.0f** | `GetActionScore` 合击可斩督军加分 |
| `DAT_1834b2ecc` | `0x34B2ECC` | `00 00 48 42` | **50.0f** | `GetActionScore` `clickWaystone` 固定分 |
| `DAT_1834b2dc4` | `0x34B2DC4` | — | **0.1f** | `ScoreFromBuffedAttack`：挨完反击还活着 |
| `DAT_1834b3188` | `0x34B3188` | — | **0.7f** | `ScoreFromBuffedAttack`：毒（poisonous）追加分的系数 |
| （立即数） | — | — | **1e6** | `ScoreFromDamagingBuffedUnit` 打死督军 |

### 6·2 `ScoreFromPlayingCard` / `ScoreFromPlayingNow`

- `ScoreFromPlayingCard = (SupportMethods.IsSpellType(cardType) ? 0 : actingCard.GetValueInPlay()) + ScoreFromPlayingNow(...)`
  - `IsSpellType(t) = (t == 20 Tactic) || (t == 40 Whispers)` —— `SupportMethods__IsSpellType.c:5`（**完整**）
- `ScoreFromPlayingNow`（cpp2il 可读）：
  1. `if (thisCard.targeted && targetCard != null)` → 遍历 **`rawCard.cardTargetScoringCriteria`**，
     每条先过 `FilterMethods.CheckIfMeetsCriteria(thisCard, targetCard, targetCard, e.targetCriteria)`，过了才 `+= ScoreFromCriteria(...)`；
  2. 再遍历 **`rawCard.nonTargetScoringCriteria`**（所有卡都跑，无闸）。

### 6·3 `ScoreFromAttack`（RVA **0x939710**）

```
s = ScoreFromBuffedAttack(thisCard, targetCard, 空CardEffect, 空CardEffect, attackType)
if (thisCard.HasSpecialAttackScoring() && !thisCard.has(sacrifice 470))
        s += ScoreFromCriteria(manager, thisCard, targetCard, thisCard.rawCard.attackScoringCriteria(+0x300))
if (thisCard.has(sacrifice 470)) {
        dmg = attackType==1 ? targetCard.CurrentMeleeAttack
            : attackType==2 ? targetCard.CurrentRangeAttack
            :                 targetCard.currentCombatAttack
        if (thisCard.health <= dmg && !thisCard.has(dodge 240) && !thisCard.has(shield 80)) {
                if (!thisCard.HasSpecialAttackScoring()) s += 2.0f;
                else s += ScoreFromCriteria(manager, thisCard, targetCard, rawCard.attackScoringCriteria(+0x300));
        }
}
if (thisCard.CurrentBlast > 0) {
        adj = manager.GetAdjacentUnits(targetCard);
        if (adj != null && adj.Count > 0) s += thisCard.CurrentBlast * adj.Count;
}
if (targetCard != null && targetCard.CurrentMarkerlight > 0 && attackType == 2)
        s += targetCard.CurrentMarkerlight;
return s;
```
（`AI__ScoreFromAttack.c:25-93` + cpp2il `AI.cs:1254-1360` 交叉；`.c` 的返回值被丢，但**结构与调用的判据齐全**）
⚠️ `blast` 与 `markerlight` 两段在 `.c` 里只剩「读了那个 getter」，增量被丢了 ——
数值来自 cpp2il（`num2 += (float)currentBlast2 * (float)adjacentUnits2._size` / `num2 += currentMarkerlight2`），
两条**外推**（算式本身很直白，但没有指令流逐条验）。

### 6·4 `ScoreFromBuffedAttack`（RVA **0x939E30**）—— `AI__ScoreFromBuffedAttack.c` **完整（float）**

```
atkDmg  = thisCard.CurrentMeleeAttack + actingCardBuff.meleeAttackChange(+0x48)
if (attackType == 2) atkDmg = thisCard.CurrentRangeAttack + actingCardBuff.rangeAttackChange(+0x50)
tgtDmg  = targetCard.CurrentMeleeAttack + targetCardBuff.meleeAttackChange(+0x48)
if (attackType == 2) tgtDmg = targetCard.CurrentRangeAttack + targetCardBuff.rangeAttackChange(+0x50)
if (targetCard.has(ambush 1200)) tgtDmg = 4                       // ← 固定 4
s = ScoreFromDamagingBuffedUnit(targetCard, DamageInfo(thisCard, atkDmg, thisCard.attackDamageType, -1, 0),
                                targetCardBuff, ignoreDroppod=false, shouldCountDamagedBastion=true)
if (thisCard.EnemyReturnsAttack(targetCard, isAttacker:true, attackType)) {
      s -= ScoreFromDamagingBuffedUnit(thisCard, DamageInfo(targetCard, tgtDmg, ...),
                                       actingCardBuff, ignoreDroppod=true, shouldCountDamagedBastion=false)
      if (thisCard.health > tgtDmg) s += 0.1f                      // DAT_1834b2dc4
}
s += thisCard.rawCard.extraScoreFromAttacking(+0x2D4) + targetCard.rawCard.extraScoreFromTargetingOnAttack(+0x2D8)
if (thisCard.has(poisonous 380) && !targetCard.has(dodge 240) && !targetCard.has(shield 80)
    && !targetCard.has(dropPod 230) && atkDmg < targetCard.health + targetCard.CurrentBastion)
      s += ScoreFromPoisoning(targetCard) * 0.7f                   // DAT_1834b3188
return s
```
🔴 `ambush` 那一支把**守方伤害**改成常数 **4**（不是 +4，是覆盖）。
✅ 毒（poisonous）的条件是「**这一下打不死**（`atkDmg < health + bastion`）」—— 不是「打得死」。

### 6·5 `ScoreFromDamagingBuffedUnit`（RVA **0x93B1E0**）

```
if (target.has(ambush 1200)) return 10.0f;
bastion = shouldCountDamagedBastion ? target.CurrentBastion : 0;
if (!target.has(invulnerable 310)) {
    totalMax = target.currentMaxHealthProcessed + buff.healthChange + bastion;
    totalCur = target.currentHealth(ObscuredInt@+0x68) + buff.healthChange + bastion;
    if (totalMax == 0) { LogError("Trying to divide by 0! in ScoreFromDamagingUnit: currentMaxHealth …, buffedMaxHealth …"); return 0f; }
    if (target.has(dodge 240) || target.has(shield 80)) return target.CurrentMeleeAttack / 3.0f + 1.0f;
    if (target.has(longrange 1160)) return 3.0f;                 // ⚠️ 位置在 dodge/shield 之后
    if (target.has(dropPod 230) && !ignoreDroppod) return 0.5f;
    dmg = damageInfo.damageValue;
    if (target.CurrentVulnerable > 0 && !GameStaticData.IsVulnerableDamage(damageInfo.damageType))
            dmg += target.CurrentVulnerable;
    if (target.cardType == Minion) {
        if (dmg < totalCur)  v = target.GetValueInPlay() * 0.6f * dmg / totalMax;
        else                 v = target.GetValueInPlay() * (target.CurrentSurvivor > 0 ? 0.8f : 1.0f);
    } else if (target.cardType == Hero) {
        if (dmg < totalCur)  v = dmg * ((totalCur - dmg) <= 15 ? 1.8f : 1.2f);
        else                 v = 1000000.0f;
    } else v = 0f;
    v += rawCard.extraScoreFromDamaging(+0x2D0);
    if (dmg >= totalCur) v += rawCard.extraScoreFromKilling(+0x2CC);
    return v;
}
return -10.0f;                                                   // invulnerable
```
（cpp2il `AI.cs:1516-1665`，其中 `if (num3 == 0)` 是 Ghidra/ILSpy 把 `!=` 反成 `==`；
「除零日志 + return 0」在**落空**那一侧，故真条件是 `totalMax != 0`。）
⚠️ **顺序**：`ambush → invulnerable → dodge/shield → longrange → dropPod → 计算`。
⛔ `longrange` 在 `dodge/shield` **之后**（cpp2il 里 `if (!HasCurrentTrait(longrange)) {…} return 3f;` 的结构可证）。

### 6·6 其余打分器（cpp2il 逐句，全部 **实读**）

| 函数 | 公式 |
|---|---|
| `ScoreFromHealingUnit(t, heal)` | `h = min(heal, t.maxHealth - t.health)`；`h <= 0` ⇒ 0；督军：`t.health > 10 ? h : 2h`；小兵：`t.GetValueInPlay() * h / t.maxHealth + rawCard.extraScoreFromHealing` |
| `ScoreFromBestAttack(m, card, actBuff, tgtBuff)` | `max over GetPotentialBuffedAttackActions(m, card, actBuff)` of `ScoreFromBuffedAttack(...)`，初值 0 |
| `ScoreFromJam(t)` | `ValueOfEnchantments(t) + ValueOfTraits(t) + (t.has(jam 130) ? 0 : rawCard.extraScoreFromJamming)` |
| `ScoreFromStun(t)` | 前置：`!t.stunnedAtStartOfTurn && (!t.has(cantAttack 150) \|\| t.hasActiveAbility) && !t.has(unstunnable 250)`；然后 `!MeetsConditionCriteria(t, mightAttack, exclude=true)` 时：督军 → `CurrentMeleeAttack`，否则 `×0.5`；否则（会攻击）：`hasActiveAbility ? t.GetValueInPlay()*0.2f : 0`；前置不满足 ⇒ 0 |
| `ScoreFromPoisoning(t)` | 前置 `!t.has(poisoned 200) && !t.has(resistant 600) && !t.IsWarlordEquivalent()`；系数：`stunnedAtStartOfTurn` ⇒ 0.8；否则 `!has(cantAttack 150)` 时 `t.health <= 2 ? 0.2 : 0.5`（`<=2` 那步是 **外推**，cpp2il 的 `ObscuredInt` 被塌成 `default`）；`return t.GetValueInPlay() * 系数` |
| `ScoreFromReturnToHand(t)` | `v = 2*manaCost + 1`；`x = (v - t.GetValueInPlay()) * 1.5`；`- (rawCard.HasDefaultTrait(rally 10) ? 2 : 0) - (rawCard.HasDefaultTrait(codex 1000) ? 2 : 0)`；`return t.CurrentCost - x - 上面两项` |
| `ScoreFromGivingCharge(t)` | `(!t.CanActNow() && t.summonSickness(+0x58)) ? t.CurrentMeleeAttack : 0` |
| `ScoreFromDrawingCard(m, card, score)` | `deck = m.GetDeck(card.side)`；`v = m.MaxCardsInHand() - hand.Count - 1`；`deck.Count == 0 ⇒ v = -2 - 该方疲劳`；`return score + v`（cpp2il 在牌库空那一支把 `-2 - fatigue` 反成了 `4.29e9 - x`，**以 §1·6 的指令流为准**） |
| `GetCardValueInPlay(m, card)` | `base = rawCard.overrideDefaultValueInPlay ? (int)rawCard.manualValueInPlay : rawCard.manaCost`；`v = ValueOfEnchantments(card) + (2*base + 1)`；`if (health < maxHealth) v *= 0.5*health/maxHealth + 0.5`；`if (has(stun 100) \|\| has(pindown 970)) v *= 0.8` |
| `GetCardReferenceValueInPlay(card)` | `2*(override ? manualValueInPlay : manaCost) + 1` |
| `ValueOfEnchantments(t)` | 遍历 `t.activeEffects`，跳过 `extrinsic`；`buffType==addStats(1)` ⇒ `+(melee+range+health)*0.5`；`buffType ∈ {12 enchantment, 15 singleUse, 16 choice}` ⇒ `+1`，且若 `enchantment.spellType == 60 SecretOrder` ⇒ `-2`；其余 0 |
| `ValueOfTraits(t)` | `Σ CardTraitCollection.GetTrait(trait)._aIScore`（cpp2il 此处被破坏，**外推**：求和 + 取 `_aIScore` 两件事是实读，遍历源不确定） |
| `ScoreFromBuff(t, e)` | `addStats(1)` ⇒ `e.healthChange + e.rangeAttackChange + e.meleeAttackChange`；`enchantment(12)/singleUse(15)` ⇒ 1；**其它 buffType > addTrait(3) 且 ≠ addTraitRemnant(90)** ⇒ 0；`addTrait(3)/addTraitRemnant(90)` ⇒ 按 `e.trait` 查表（见下） |

**`ScoreFromBuff` 的 trait→分表**（`AI.cs:1807-1965`，实读；`traitValue` = `e.traitValue`）：

| trait | 分 | trait | 分 |
|---|---:|---|---:|
| `vanguard` `dodge` `shield` `survivor` `firstStrike` `precognition` | **2.0** | `stealth` `sneakAttack` `sacrifice` `pack` | **1.5** |
| `ward` `camouflage` `longrange` `unstoppable` `flying` `syphon` `tainted` `coward` `backlash` `relentless` `transient` `resolution` `terror` `psyker` `reactor` `requiem` `landing` | **1.0** | `dropPod` `berzerk` | **0.5** |
| `stun` `pindown` `blind` | **−1.5** | `vulnerable` `markerlight` | **−1.0** |
| `cantAttack` | **−2.0** | `poisoned` | **−4.0** |
| `poisonous` | **3.0** | `bloodThirst` | `t.CurrentMeleeAttack` |
| `maintenance` | `t.CurrentMaintenance` | `bastion` | `t.CurrentBastion` |
| `mutation` | `t.GetCurrentTraitValue(mutation)` | `blast` `shuriken` | `e.traitValue` |
| 其它 | 0 | | |

### 6·7 外围（`PlayTurn` / `TweakAvailableActions` / `SelectBestAction` / `IsBetterAction` / `GetAiMulliganCards`）

- **`PlayTurn`**（cpp2il `AI.cs:110-159`，实读）：`playedFirstComboCard=false` →
  `list = manager.GetAvailableActions(isPlayer:false)` → 逐条 `a.actionScore = GetActionScore(manager, list, i)`
  → `list = TweakAvailableActions(manager, list)` → `best = SelectBestAction(list)`
  → **`best.actionType != endTurn` 才 `ExecuteAction`** → `return best.actionType != endTurn`。
- **`TweakAvailableActions`**（cpp2il `AI.cs:216-348`，**前半可读、循环体半坏**）：
  - 闸：`FeatureConfig.ShouldDisableNewMatchMaking()` ⇒ 不动；
    `matchType ∈ {RankedBot, ClosedDeckBot, FastModeBot, PracticeOffline(非 FTUE)}` ⇒ **照做**；
    其它（含 FTUE）⇒ 不动。
  - `kMax = matchData.GetBotConfigMaxSkips()`、`kMin = GetBotConfigMinSkips()`、`chance = GetBotConfigSkipChance()`；
  - `list = currentActions.OrderByDescending(a => a.actionScore).ToList()`
  - 外层跑 `kMax` 轮；每轮**从表尾（最低分）往前扫**，碰到 `endTurn` 跳过（继续往前）；
    `轮次 >= kMin` ⇒ `r = manager.GetRandomInt(0,100)`，`chance > r` 才删（删完 `break` 出内层）；
    否则**无条件删**（删完 `break`）。日志 `"Skip AI action by maxsteps: … random: …"` / `"… by minsteps: …"`。
  - ⚠️ 内层那段的控制流在 cpp2il 里被塌过（`flag6/flag8` 的语义），**以上是外推 + `.c` 交叉**。
- **`SelectBestAction` / `IsBetterAction`**（实读）：基准 = 第一个 `endTurn`（没有就 `[0]`）；
  遍历：`a.score > best.score` ⇒ 换；否则**仅在 `best` 还是 `endTurn` 且 `a` 是近战/远程**时，
  按「例外」判：`actingCard.has(berzerk 360)` ⇒ **换**；`has(stealth 70)` / `has(vanguard 980)` /
  `HasSpecialAttackScoring()` / `cardType == Hero(10)` ⇒ **不换**；其余 ⇒ 换。
- **`GetAiMulliganCards`**（`AI__GetAiMulliganCards.c` + 指令流 **实读**）：
  `mulliganOption(+0x260) == 30` ⇒ **保留**；否则 `rawCard.manaCost(+0x78) > 4`（`cmp … ,4` + **`setg`**）⇒ **换掉**；
  其它保留。日志 `"%%%% Analyzing card {name} for mulligan, discard: {bool}"`。

---

## §7 我方实现对照总表（`Unity/MyGame/Assets/RuleEngine/**`）

| 原版 | 我方 | 结论 |
|---|---|---|
| `AI.GetActionScore` | `Data/SimpleAI.cs:485 ScoreAction` | ✅ 分派一致（含 `+10000`、`clickWaystone=50`） |
| `AI.ScoreFromCriteria` | `Data/AiScoring.cs:109`（**已按 RVA 0x93A290 逐支读过**，文件头 `:6-19` 明写） | ✅ 0/1/2/3/4/5/6/9/10/12/13/14/25/35 与本节逐字一致；❌ 7/8/11/20/30/40 未实现（见 §8） |
| `AI.ScoreFromPlayingNow` / `PlayingCard` | `Data/AiScoring.cs:57` / `SimpleAI.cs:533` | ✅ 顺序、闸门一致 |
| `AI.ScoreFromAttack` | `Data/SimpleAI.cs:566/584` | ⚠️ **缺 4 项**（见 §8） |
| `AI.ScoreFromBuffedAttack` | `Data/SimpleAI.cs:584` 内联 | ⚠️ 见 §8 R6-06/07 |
| `AI.ScoreFromDamagingBuffedUnit` | `Data/SimpleAI.cs:724 ScoreDamaging` | ⚠️ 分支顺序 ✅；**部分伤害比值口径不同**（§8 R6-05） |
| `AI.ScoreFromHealingUnit` | `SimpleAI.cs:768` | ✅ |
| `FilterMethods.MeetsStatCriteria` | `Core/TargetCriteriaEval.cs:458` | ✅ **逐值一致**（含 `case 8 ⇒ true`） |
| `FilterMethods.MeetsConditionCriteria` | `Core/ConditionEval.cs` | ✅（`MeetsConditionCriteria.c` 行号逐条标了） |
| `FilterMethods.CheckIfMeetsCriteria` | `Core/TargetCriteriaEval.cs`（⑪ 四组 stat 在 `:335/:401`） | ✅ 结构一致（本轮未逐行核完 32 步，见 §9） |
| `AbilityLogic.CanPlayAbility` | — | ✅ 语义并入 `RuleCore.CanUseAbility`（未逐条核 7 档） |
| `AbilityLogic.GetValueFromCriteria` | — | 见 §9（本轮未核我方对应） |
| `AbilityLogic.GetTargets` | `Data/AiTargets.cs`（1061 行） | 见 §9（只核了原版侧） |
| `Evaluation.Check` | **无**（`Core/` `Data/` 0 命中） | ✅ 不需要：唯一调用点是 LiveOps，与战斗无关 |
| `AI.GetAiMulliganCards` | `SimpleAI.cs:1708 AiMulliganIndices` | ✅ `>4` + `==30` 都对着（本轮指令流复核） |
| `AI.TweakAvailableActions` | `SimpleAI.cs:1294` | ⚠️ RNG 消耗个数与原版不同（**我方注释已承认**）；旋钮值我方按难度硬编码，原版来自 `MatchData.GetBotConfig*` |

---

## §8 与原版不符 / 缺漏（可直接派给执行代理）

> 🔴 **编号以【本判据 §8】为准** —— `报告/R6_AI打分判据_第2轮.md` §四 的摘要表用的是**旧编号**，
> 两边 **`R6-06 / 07 / 08 / 09` 指的不是同一条**（例：R6 报告把「挨反击存活那 0.1 分」记成 `R6-06`，
> 本表记成 `R6-07`）。执行时**一律照本表**；引用旧编号的地方要写清「R6 报告版」。
>
> ⚠️ **`play all dump` 结构上看不见 AI**（2026-10-25 `R11` 实测）：它是**结算层**全池 sweep，
> 列里没有 AI 决策/分数；`工具/ruleprobe/` grep `SimpleAI|AiScoring|AiRng|"ai"` = **0 命中**。
> ⇒ 本表任何一条改完，`play all dump` **逐字节不变是正常的** —— ⛔ 既不能拿它当「改动没生效」的证据，
> ⛔ 也不能拿它当「改动正确」的证据。AI 改动的读数口 = **`ruleprobe ai <卡id>`**（同一批新增，
> 见 `报告/R11_AI打分落地_第2轮.md` §二段）；没有读数口时只能给**解析式读数**（按池内数据手算）。

| # | 文件:行 | 现在怎样 | 应改成 | 判据 | 风险 |
|---|---|---|---|---|---|
| **R6-01** | `Data/AiScoring.cs:268`（`default` 支） | option **7 SumValueOfJam / 8 RandomValue / 11 ValueOfBuffing / 20 SumValueOfPoison / 30 SumValueOfGivingCharge / 40 ValueOfChangeWeapon** 一律 `return baseValue` | 按 §1·5 逐支实现：<br>7 = `base + Σ ScoreFromJam(t)·M·mul(t)`<br>8 = `acc + (float)AiRngOrManager.GetRandomInt(0,100)`<br>11 = `base + Σ ScoreFromBuff(t, e.buff)·M·mul(t)`<br>20 = `base + Σ ScorePoisoning(t)·M·mul(t)`<br>30 = `base + Σ ScoreGivingCharge(t)·M·mul(t)`<br>40 = `base + Σ_{t: rawCard.maxHealth==0} mul(t)` | RVA `0x18093AB10/ABA7/AC34/ADAC/AED9/B0BE`（§1·5） | **低**（`AUD_T06` 说池内 0 条用这些 option，行为零变化；但铁律 11「全部都要做」） |
| **R6-02** | `Data/AiScoring.cs:195-205`（option 5） | 没判 `t.mightAct` | 循环体内先 `if (!t.MightAct) continue;` | RVA `0x18093A934-0x18093A936`（`CardScript.get_mightAct` @`0x180603CA0`，假则 `je 0x18093AA63` 跳过） | **低**（池内数据） |
| **R6-03** | `Data/SimpleAI.cs:584 ScoreFromAttack` | **没有**「攻击方带 `sacrifice(470)`」那一支 | `if (u.Has("sacrifice"))`：`dmg = 按档取守方攻击力`；`if (u.Health <= dmg && !u.Has(Dodge) && !u.Has(Shield))`：非 special ⇒ `s += 2f`；special ⇒ `s += ScoreFromCriteria(rawCard.attackScoringCriteria)` | `AI__ScoreFromAttack.c:39-70` | **中**（`sacrifice` 是池内真实 trait；会改 AI 出招偏好） |
| **R6-04** | 同上 | **没有** `blast` / `markerlight` / `poisonous` 三条加分 | `blast`：`if (u.CurrentBlast > 0) s += u.CurrentBlast * adjCount`；`markerlight`：`if (ranged && target.CurrentMarkerlight > 0) s += target.CurrentMarkerlight`；`poisonous`：`if (u.Has(poisonous) && !target 有 dodge/shield/droppod && atk < target.Health+armour) s += ScorePoisoning(target) * 0.7f` | `AI__ScoreFromAttack.c:71-86` · `AI__ScoreFromBuffedAttack.c:96-123` · 常量 `0.7f @ RVA 0x34B3188` | **中**（三条都是池内真实 trait） |
| **R6-05** | `Data/SimpleAI.cs:741` | 小兵「没打死」的比值 = `ValueInPlay · 0.6 · dealt / MaxHealth`，其中 `dealt` 是**扣甲后**伤害 | 原版 = `ValueInPlay · 0.6 · damageValue / (maxHealth + buffHealthChange + bastion)`，`damageValue` 是**扣甲前**（+ vulnerable） | cpp2il `AI.cs:1588-1596`（`num2 < obj5` / `num6/num3`） | **中**（有护甲/脆弱时数值不同；不影响「打死与否」的判定 —— 那个等价） |
| **R6-06** | 同上 `:728-733` | `ambush → 10`、`invulnerable → -10`、`dodge/shield → atk/3+1`、`longrange → 3`、`dropPod → 0.5` | **顺序与值都对** ✅（仅记录：`longrange` 在 `dodge/shield` **之后**，别调换） | `AI__ScoreFromBuffedAttack.c` / cpp2il `AI.cs:1530-1575` | — |
| **R6-07** | `Data/SimpleAI.cs:607` | 挨反击后还活着 `+0.1f` 的判据 = `DamageAfterReduction(u, counter) < u.Health` | 原版 = `thisCard.health > tgtDmg`（**扣甲前**的伤害） | `AI__ScoreFromBuffedAttack.c:87-91` + `DAT_1834b2dc4 = 0.1f` | **低** |
| **R6-08** | `Data/SimpleAI.cs:698 ValueInPlay` | `2*Cost + 1 + …` | 漏了 `rawCard.overrideDefaultValueInPlay ? manualValueInPlay : manaCost` | cpp2il `AI.cs:551` | **低**（真值里有没有非 0 的 `overrideDefaultValueInPlay` 未查，见 §9） |
| **R6-09** | `Data/AiScoring.cs:35`（注释） | 「option 12 的 `ScoreFromPlayingCard` 递推不再带目标」 | 原版 `ScoreFromHandBuff` 内部确实把 `targetCard = null` 传下去（**我这轮只到这一步**）；⚠️ `ScoreFromHandBuff` 的完整算式**没读出来**（§9） | `AI__ScoreFromHandBuff.c` cpp2il 版几乎全坏 | **低** |
| **R6-10** | `Data/AiScoring.cs:265`（option 35） | `base + M·mult·delta` | 指令流是 `base + M·mult·(delta + base)`（`base` 进乘法一次、再在外面加一次：`:0x18093B083` + `:0x18093B0A9`） | §1·6 | **低**（两者恒差 `M·mult·base`；`base == 0` 或 `M·mult == 1` 时零差） |

> ⚠️ **R6-01 / R6-02 今天零行为变化**（池内数据 0 条），但仍要按铁律 11 补齐 —— **只有先后、没有不做**。

---

## §9 查不到 / 没查清（⛔ 别拿猜测填空）

| # | 缺什么 | 还差什么 |
|---|---|---|
| 1 | **`AI.ScoreFromHandBuff` 的完整算式** | `AbilityLogic__GetTargets` 那侧的调用点只到「整表一次、结果 × M」；函数体在 cpp2il 里被 ILSpy 打成 30+ 条 `Unmanaged memory load` 占位，`.c` 里是一长串结构体拷贝。**要读 RVA `0x93B7A0` 的指令流**（约 0x7E0 字节）。本轮没读。 |
| 2 | **`AI.ValueOfTraits` 的遍历源** | cpp2il 只剩 `trait._aIScore` 的求和与 `CardTraitCollection.GetTrait`，**遍历的是哪个集合**（`card.currentTraits`？`rawCard.defTraits`？）读不出。 |
| 3 | **`AbilityLogic.GetTargets` 每个分支内部怎么填候选集** | 本轮只到「分派键 → 分支 → 调了哪个 manager 方法」。逐支的候选拼装（尤其 `allFiltered` / `inHand*`）没逐句核。 |
| 4 | **`Data/AiTargets.cs` 与原版 `GetTargets` 的逐支对账** | 本轮**只核原版侧**，没逐支核我方（1061 行）。下一步：照本文件 §5·3 的分支表逐行比。 |
| 5 | **`Data/AiTargets.MeetsStat` / `Core/TargetCriteriaEval` 的 32 步顺序** | 本轮核了 `MeetsStatCriteria`（逐值一致）与 `MeetsConditionCriteria`，**`CheckIfMeetsCriteria` 的 32 步顺序没与我方逐行比**。 |
| 6 | **`rawCard.overrideDefaultValueInPlay` / `manualValueInPlay` 在全卡池里的分布** | 没查（决定 R6-08 是「真缺口」还是「零影响」）。 |
| 7 | **`AbilityLogic.GetValueFromCriteria` 我方有没有对应** | 没查（本轮只读原版）。 |
| 8 | **`BattleManager.globalVars + 0xB4` 是哪个字段** | `ExecuteAction` 把它当 `AddAutoActionToQueue` / `AddAttackAction` 的 `priority` 传；`dump.cs` 里 `GameplayVariables` 的 0xB4 没查。**不影响复刻**（我们引擎的优先级另有 入口）。 |
| 9 | **`ScoreFromPoisoning` 的 `<= 2` 阈值** | cpp2il 把那处的 `ObscuredInt` 塌成 `default`；`t.health <= 2 ? 0.2 : 0.5` 这个阈值是**外推**（`0x18093C4E0` 的指令流没读）。 |

---

## §10 复跑办法（全部只读 `D:/2`）

```bash
PY=D:/2/Warpforge_tools/py312/python.exe

# 1) 跳转表（ScoreFromCriteria）：RVA 0x93B148，36 项
$PY - <<'EOF'
import struct;d=open(r'D:/2/unity_run_ref/GameAssembly.dll','rb').read()
print([hex(struct.unpack_from('<I',d,0x938c90+0x93B148-0x93A290+i*4)[0]+0x180000000) for i in range(4)])
EOF

# 2) 任意方法名 → VA
$PY d:/4/Unity/工具/rva_of_method.py "AI\$\$ScoreFromCriteria"

# 3) 按 VA 反汇编（线性；以 ret 为界）
$PY d:/4/Unity/工具/disasm_va.py D:/2/unity_run_ref/GameAssembly.dll 0x18093A290 120

# 4) 方法名反查（本轮用的那个脚本的内联版）
$PY - <<'EOF'
import json;j=json.load(open(r'D:/2/tools/il2cpp_out/script.json',encoding='utf-8'))
print([m['Name'] for m in j['ScriptMethod'] if m['Address']==0x93b580])
EOF

# 5) 字符串字面量（按 RVA）
$PY -c "import json;m={int(e['address'],16):e['value'] for e in json.load(open(r'D:/2/tools/il2cpp_out/stringliteral.json',encoding='utf-8'))};print(repr(m.get(0x425ee00)))"
```
