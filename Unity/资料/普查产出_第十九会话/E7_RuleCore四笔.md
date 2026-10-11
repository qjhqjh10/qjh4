# `E7` —— `RuleCore.cs` 四笔（`A1258` · `A1335` · `A1397` · `A1399`）

> 写手：E7 笔（2026-10-11 第十九会话）· 独占文件 = `Unity/MyGame/Assets/RuleEngine/Core/RuleCore.cs`
> ⛔ 未跑 Unity、未跑 `typecheck.sh`、未动 git、未改任何正本。
> `git diff --numstat RuleCore.cs` = **255 / 36**（只此一个文件）。

---

## 一、改动清单

| 行号 | 改了什么（≤40 字） | 服务哪个 A 号 |
|---|---|---|
| `RuleCore.cs:1126-1144` | 删掉循环外的 stealth 到期段，改成「搬走了 + 判据 + 副作用」的注释 | `A1397②` |
| `RuleCore.cs:1155` | 循环前声明 `int lostStealth = 0;` | `A1397②` |
| `RuleCore.cs:1176-1179` | 旧注释「次序与原版相反」→ 改写成「已修掉」 | `A1397②` |
| `RuleCore.cs:1182-1197` | **stealth 到期搬进逐卡循环、排在 `OpenDropPod` 之后**（真行为改动） | `A1397②` |
| `RuleCore.cs:1220-1222` | 循环后的汇总日志（原来在块内） | `A1397②` |
| `RuleCore.cs:3757-3758` | `DeclareAttack` doc：`forced` 也闸第 ① 条取消通道 | `A1258` |
| `RuleCore.cs:3777-3791` | **`DeclareAttack` 加第 ① 条取消通道的调用点**（`IsValidTarget` 之后 / 消耗攻击之前） | `A1258` |
| `RuleCore.cs:4651-4665` | 新增 `const string AboutToAttackKeyword = "abouttoattack";` + doc | `A1258` |
| `RuleCore.cs:4667-4747` | **新增 `TryCancelAttackByOwnAboutToAttack(...)`**（含判据全文） | `A1258` |
| `RuleCore.cs:5028-5062` | `ApplyDamage` 段：把 `A1335` 三条判据【读全】并落成注释 | `A1335` |
| `RuleCore.cs:5501-5534` | **就地订正**「原版这一族自排除不一致」那句错话（铁律 5） | `A1399` + `A1397①` |

---

## 二、每笔的原版判据（逐句引反编译出处）

### `2.1` `A1397②` —— `OnTurnStart` 里 stealth 到期 vs 开舱的次序 ✅（**已改**）

判据 = `d:/2/tools/decomp_full/CardScript__OnTurnStart.c`（逐行读过），**一张卡自己的序列**：

| 行 | 语句 | 读作 |
|---|---|---|
| `:99` | `CardScript__ActivateMinion(param_1, …,0)` | 解疲劳 |
| `:102` | `bVar3 = false;` | 开舱标记复位 |
| `:103-106` | `HasCurrentTrait(param_1, 0xe6)` ∧ `cardState ∈ {2,0xf,3,0x11}` ∧ `param_2 == card.isPlayer` | 带 `dropPod` ∧ 在场 ∧ 本方回合 |
| `:107` | `CardScript__RemoveDropPod(param_1,0,0)` | **开舱（第 1 步）** |
| `:108` | `bVar3 = true;` | —— |
| `:110` | `cVar4 = HasCurrentTrait(param_1, 0x46);` | **stealth** |
| `:111-113` | `cVar4 != 0` ∧ `cardState ∈ {2,0xf,3,0x11}` ∧ `param_2 == card.isPlayer` | 同一组闸 |
| `:114` | `CardScript__RemoveTraitAndEffects(param_1, 0x46)` | **stealth 到期（第 2 步）** |
| `:116` | `BattleManager__BroadcastUnitLoseStealth(bm, param_1, 0)` | 广播 |
| `:118` | `CardScript__ActivateMinion(param_1, param_2, 0)` | 再跑一次 |
| `:119-126` | `stun(100)` ⇒ `+0x55 = 1` / `blind(0x3cf)` ⇒ `+0x56 = 1` | 两个闸门 |
| `:263-267` | `if (bVar3) OnTrigger(0x1b8 /*Landing=440*/, …)` | `Landing` 排在**很后面** |

⇒ **开舱（`:107`）在 stealth 到期（`:114`）之前**。我们改前把 stealth 那一整段单独提到
循环之外、排在开舱之前 ⇒ 相对次序与原版相反。**已改**：现在逐卡 `RefreshForNewTurn()`
→ `OpenDropPod(...)` → stealth 到期 → stun/blind 闸门，逐格对上。

### `2.2` `A1399` + `A1397①` —— `TriggerOtherCard*` 一族**统一自排除** ✅（**判据已核清，改法不在本文件**）

🔴 **账上那句「原版自身不一致」是【错的】**（铁律 5 已在 `RuleCore.cs:5501` 就地订正）。

1. **调用点**（`d:/2/tools/decomp_full/BattleManagerSupport__BroadcastUnitUsedSacrifice.c`）：
   三跳形状**完全相同**——
   · 棋盘 `:29-34`：`op_Inequality(卡, param_2)` → `CardScript__ReactToUnitUsedSacrifice(卡, param_2)`；
   · 当前回合方手牌 `:45-50`：同形；
   · 另一方手牌 `:64-70`：同形。
2. 🔴 **`.c` 里前两跳看着只传了 1 个实参 —— 那是 Ghidra 掉了 `rdx`，机器码里没掉。**
   本会话按 VA 反汇编核对（`D:/2/unity_run_ref/GameAssembly.dll`，base `0x180000000`）：

   ```
   BroadcastUnitUsedSacrifice  VA 0x1809BA580（RVA 0x9BA580，入口 mov rsi, rdx = param_2）
   0x1809BA625  mov rdx, rsi      ┐ 棋盘跳
   0x1809BA62B  call 0x182FFB920  ┘ = UnityEngine.Object.op_Inequality(卡, param_2, 0)
   0x1809BA651  mov rdx, rsi      ┐ 同一跳的第二半
   0x1809BA657  call 0x1805F59C0  ┘ = CardScript.ReactToUnitUsedSacrifice(卡, param_2)
   0x1809BA6C0 / 0x1809BA6EC      ← 手牌第 1 跳，同形
   0x1809BA756 / 0x1809BA77E      ← 手牌第 2 跳，同形
   ```
   `ReactToUnitUsedSacrifice` 自身（VA `0x1805F59C0`，13 条指令）里**确实没有**这道守卫
   —— 守卫**写在调用方**（见上）。
3. **同族函数体内的同一条判据**：`CardScript__TriggerOtherCardLanding.c:14` · `Mob.c:14` ·
   `Codex.c:14` · `SpiritStone.c:14` · `Requiem.c:14` · `Regiment.c:14` 全是
   `cVar1 = UnityEngine_Object__op_Inequality(param_2, param_1, 0); if (cVar1 != '\0') { …OnTrigger… }`
   （`param_1` = 接收者自己、`param_2` = 触发源）⇒ **别的卡才算**。
   `BroadcastUnitLanded`（VA `0x1809B8230`，入口 `mov rbp, rdx`）棋盘跳 `0x1809B82E1 mov rdx, rbp`
   ⇒ 调用侧也把触发源传进去了，同一条语义。
4. ⇒ **结论：原版这一族统一「被触发那张卡自己不被叫醒」。** 原版把「自己」与「别人」分成
   **两个 trigger id**（`AbilityTrigger.Mob = 300` 给自己 / `OtherCardMob = 645` 给别人；
   `ABILITY` 表见 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/AbilityTrigger.cs`）。
5. **我们这边**：`FireTriggerAlways` → `BroadcastWhen`（`EffectResolver.cs:5445`）→
   `WhenEvents.Matches`（`WhenEvent.cs:1185`）—— **没有任何**「监听者 ≠ 事件主体」这一关，
   只有 opt-in 的**反向**开关 `WhenEvent.SelfOnly`（`:1197-1201`，要求监听者**就是**主体）。
   ⇒ 「`When another unit uses Sacrifice, …` 的牌自己献祭时叫醒自己」= **真差异**。

🔴 **改法的唯一落点不在本文件** ⇒ 见 §四。

### `2.3` `A1258` —— 取消攻击第 ① 条通道 ✅（**判据已核清，已落成形；并订正账上一处实据**）

🔴 **订正（铁律 5）**：账上写 `HasCancelAttackEffect` 扫 `card + 0x110`（`List<CardAbility>`）
—— **`+0x110` 不是能力表**：
· `EntityScript + 0x110` = `List<ActiveEnchantment> activeEnchantments`（`il2cpp_out/dump.cs:22027`）；
· `ActiveEnchantment.enchantmentCard` 是 `RawCardScript`（`+0x10`，`dump.cs:118929`）；
· 真正的能力表是 `RawCardScript.cardAbilities`（`+0x290`）。

逐句读 `CardScript__HasCancelAttackEffect.c`：
`:18 lVar2 = *(longlong *)(param_1 + 0x110)` → `:31 RawCardScript__HasAbilityType(enchantmentCard, 0x118, 0)`
→ `:36-50` 在 `enchantmentCard + 0x290`（`cardAbilities`）里逐条
`CardAbility__CanTriggerAbility(ability, 0x118, bm, 本卡, param_2, param_3, 0, 0)`。
⇒ **① 扫的是「攻方身上的【附魔卡】」**。
而 `CardScript__HasCancelAttackAbility.c:16-49` = **① 加上** 攻方原生
`rawCardData.cardAbilities(+0x290)` 里 `*(int*)(ability + 0x10) == 0x118` 的那条
—— **②（扫全场）用的正是这个更宽的口子**（`BattleManager__CheckUnitsCancellingAttack.c:40`）。

**调用链（两条通道）**：
· ① `BattleManager._ResolveAttack_d__438__MoveNext.c:1310` → `BattleManager__CheckIfAbilityCancelsAttack.c:7`
  → `HasCancelAttackEffect(攻方, 攻方, 原目标)`；命中后 `:1314 CardScript__CancelAttack(攻方)`
  → `:1316/:1318 ClearPendingDamage(攻方/原目标)` → `:1365 RegisterPlayerActionCancelled`
  → `:1368 CardScript__ResolveCancelAttack(攻方, 攻方, 原目标)`，随后 `return`（**不出伤害**）。
· `CardScript__ResolveCancelAttack.c:12-14` → `RawCardScript__OnTrigger(0x118, bm, 攻方, 攻方, 原目标, …)`
  ⇒ **把那条能力放出来**（`0x118 = 280 = AbilityTrigger.AboutToAttack`，`AbilityTrigger.cs:44`）。
· `RegisterPlayerActionCancelled.c:28-34` 只做一件：把 `bm + 0x3bc` 那个 `ObscuredInt`
  **`op_Decrement` 后写回**。⚠️ 它管的是哪一格、配额要不要回退 —— **没查清**（见 §四）。

**我们落成的形状（如实标着 ≠ 原版形状）**：
我们引擎**没有「附魔」这一层**（`Core/BattleEvent.cs:180-183` 自己写着「`RuleEngine` 里根本没有
『战将身上的附魔』这一层」）⇒ ① 的原版载体不存在。改成「**攻方自己身上有一条 `AboutToAttack(280)`
的能力 ⇒ 这一记不打**」，判据转调 `u.FxOps(kw)` / `u.Effect(kw)`（全仓唯一一份「卡面原生正文
**或** 运行时挂上去的那份」）——与前例 `LandingKeyword`（`RuleCore.cs:4620`）同一条路数。
**今天 0 张卡命中 ⇒ 本支恒假、逐字节不动**（不可达性实据：`cards_engine.json` 1126 张整条记录
文本扫 `about to attack` / `cancel` / `before` + `attack` ⇒ **0 命中**）。

### `2.4` `A1335` —— 三条判据（① 没做，② / ③ 已清）

**① 易伤那一份**（`CardScript._ReceiveDamage_d__381__MoveNext.c:188-257`）—— 判据**读全了**：

| 行 | 语句 |
|---|---|
| `:188` | `iVar8 = EntityScript__get_CurrentVulnerable(lVar2)` |
| `:189-191` | `易伤 > 0` ∧ `!HasCurrentTrait(0xf0)` ∧ `!HasCurrentTrait(0x50)` ∧ `!HasCurrentTrait(0x136)` ∧ **`!bVar17`** |
| `:197` | `if (GameStaticData__IsVulnerableDamage(*(int*)(damageInfo + 0x1c)) == 0)` |
| `:200` | `if (0 < *(int *)(*(longlong *)(param_1 + 0x28) + 0x18))`（这一击的量 > 0） |
| `:211` | `if (0 < iVar8 /*当前血*/ && *(char *)(lVar2 + 0x65) == 0 /*不是残骸*/)` |
| `:226-227` | `DamageInfo___ctor(…, uVar11 /*= CurrentVulnerable*/, (iVar8 == 4) + 7 /*类型 7 或 8*/, …)` |
| `:252-254` | `BattleManager__DealMultiDamage(bm, damageInfo, list, 2, …, thisCard, …)` |

`bVar17` 的来源：`:130 bVar17 = -1 < iVar8;`（`iVar8 = CardScript__RemoveBastionDamage(...)`
的返回值 = 扣完的堡垒值，**可为负**）⇒ `bVar17` 真 = 「堡垒把这一击**整份**吃下」。
⇒ 「没打穿」那一档原版**抑制**；「打穿（`remaining < 0`）」与「有 `dropPod`」两档原版**会补**。
`IsVulnerableDamage`（`GameStaticData__IsVulnerableDamage.c:5`）= `(uint)(type - 7) < 2`
⇒ **只挡类型 7/8**（= 那一次自己用的类型）⇒ 只防递归。
**预览侧同式**：`CardScript__EnoughPendingDamageToDieWithDamageValues.c:183-205`
= `iVar7 = Math.Max(1, CurrentVulnerable − armour); iVar5 += iVar7`（**每个伤害条目各加一次**，
与「每段伤害各自一条 `ReceiveDamage`」自洽）。

🔴 **为什么没做**（⛔ 不是「影响小 / 成本高」）：
· ⓐ 我们的模型把易伤**折进** `DamageAfterReduction` **一个数**
（`RuleCore.cs:4351`/`:4430`：`max(1, 伤害 + 易伤 − 护甲)`），与原版
`max(1, 伤害 − 护甲) + max(1, 易伤 − 护甲)` **在普通档就不等价**；只补堡垒/空投舱那两档
⇒ 同一份规则出现**第三个算式**，比现在更难核（账上那句「半抄会做出更没法核的东西」）。
· ⓑ 要拉开模型必须动 `DamageAfterReduction` —— 而它**被现有断言钉着**：
`RuleEngine/Editor/RuleEngineTest_S14.cs:633`
`Check(RuleCore.DamageAfterReduction(t, 3), 5, "（前提）易伤 2 确实折进了伤害公式（3 + 2 = 5）")`
⇒ 一动就红，而**那个文件不在本笔白名单**（铁律 13·3）。
· ⓒ 还会牵动预览侧（`DamageAfterReductionOne` / `WouldKillByEntries`）⇒ 预览与结算必须同式。
⇒ **本笔不落半抄**，判据交出（要拉开 = 单开一笔、把 `RuleEngineTest_S14.cs` 一并列入）。

**② `deathType`** —— ✅ **已做**：`ignoreBastion`（`RuleCore.cs:4952` / `:5172`），
全仓只有反击那一处传 `true`，判据 = `_ReceiveDamage…:63` 的 `deathType == combatAttacker(10)`。
「将来**新增伤害点**要照 `TryCreditKill` 的口径再判」是一条**常设规矩**（已写在 doc 里），今天没有缺口。

**③ 「被堡垒整份吃下」算不算「受到伤害」** —— 🆕 **判据读清了：算。**
· 广播点 = `BattleManager__ResolveAttackDamage.c:30`
`BattleManager.BroadcastUnitDamaged(bm, 被打者, adjustedDamageInfo, isCombat)` ——
`:19-31` 一路下来**没有任何 `if` 挡它**（`:19 param_2 != 0` → `:21 param_3 != 0` 只是空值检查）。
· `CardScript__GetAdjustedDamage.c:52-60`：**只在 `param_3 != 0`（isCombat）时**
`if (CurrentBastion != 0 && iVar4 <= CurrentBastion) uVar3 = 1;` ——
`uVar3` 就是 `DamageInfo` 里那个「挡下」位，与 `0xe6`/`0xf0`/`0x136`/`0x50`（`:39-50`）
**同一个位** ⇒ 它**不影响广播发不发**，只影响听众读到的标记。
⇒ 原版照样广播；我们这一支（堡垒整份吃下 ⇒ `EmitHit(ctx, u, 0)` + `return 0`）与
盾 / 无敌 / 空投舱**同一个口径** ⇒ **两边一致，不改**。

---

## 三、每笔的判别式（新加的守卫能被什么改动弄红）

> ⚠️ 本笔**没有**新增/修改任何断言 —— `RuleEngine/Editor/RuleEngineTest*.cs` **不在白名单**。
> 下面是**交给断言写手**的判别式（含 🧨 改坏法）。

**`A1397②`（stealth 到期次序）**
· 夹具（手工 `CardDef`，不是真卡 —— 全池 0 张同时带这两词）：给一个单位同时
`AddKeyword(KeywordTable.Stealth, 0)` + `AddKeyword(KeywordTable.DropPod, N)`，
而**那张卡的 `WhenTriggers` 里登记一条 `When … triggers Landing` 的监听器**（用于观测内序）。
· 断言：跑一次 `BeginTurn` ⇒ **`EvtKind.Trigger{keyword="landing"}` 的下标 < `LosesStealth` 广播那条日志的下标**。
· 🧨 **改坏法**：把 stealth 那一段搬回 `Auras.Recompose` 之前（= 退回旧写法）⇒ 本行红；
  而「stealth 确实到期了」（`!u.Has(Stealth)`）那一格**仍然绿** ⇒ 两格结构上不可能被同一实现同时满足
  （这就是灭自证的那一条）。
· ⚠️ 不要只断「stealth 被摘掉了」：那只覆盖「有没有做」，覆盖不了「次序」。

**`A1399` / `A1397①`（自排除）** —— 交给 `WhenEvent.cs` 的写手（见 §四）。
· 夹具：手工卡 A 同时带 `sacrifice`（或 `swarm`/`mob` 任一有触发时机的词）+ 一条
`triggers:<同一个词>` 监听器（`SelfOnly == false`）；卡 B 只带监听器。
· 断言：B 的正文结算（或它那条 `EvtKind.Trigger`）出现 ⇒ 正例；
  **A 自己那条监听器一次都不响** ⇒ 反例（这一条就是新守卫）。
· 🧨 **改坏法**：把 `Matches` 里那句删掉 ⇒ 正例仍绿、反例红。
· 🔴 **`SelfOnly` 那一支必须同时钉住**（`this unit triggers X` 仍然只在「自己触发」时才响）
  —— 只加「不许自己」会把它一起杀掉（`RuleEngineTest.cs:17261-17265` 盯着它）。

**`A1258`（第 ① 条取消通道）**
· 夹具：手工卡给攻方加一条 `abouttoattack` 的**运行时授予正文**
（今天 `CardDef.RoutableTriggers` 没登记这个词 ⇒ `FxOps` 恒 null ⇒ 直接 `AddKeyword` 也拿不到 ops）。
⚠️ **本笔实测：今天这门夹具立不起来** —— 要立得先让 `CardDef.cs` 登记这个词（不在本笔白名单）。
· 断言（登记之后）：攻方带 `abouttoattack` 正文 ⇒ `DeclareAttack` 返回 `RuleCodes.OK`、
  **`target.Health` 一点不动**、**`attacker.AttacksThisTurn` 不变**、`EvtKind.Attack` **一条都没有**。
· 🧨 **改坏法**：删掉 `DeclareAttack` 里那段 `if (!forced && TryCancel…)` ⇒ 上面三条同时红
  （伤害会出现、配额会被吃、`EvtKind.Attack` 会发）。
· 🔴 **反向沙包（灭自证）**：**不带** `abouttoattack` 的攻击**必须照常打**（伤害/配额/事件三样都在）
  —— 只断「带了就取消」的话，一个「永远取消」的错实现也会全绿。
· 🔴 **`forced` 那一格也要钉**：`forced: true` 时**不取消**（判据 = `CheckIfAbilityCancelsAttack`
  全库只有 `_ResolveAttack` 一个调用者）。🧨 把 `!forced &&` 删掉 ⇒ 本行红、上面全绿。

**`A1335③`（堡垒吃下也广播）**
· 夹具：手工单位 `AddKeyword(Bastion, 5)`，打 3。
· 断言：`EvtKind.Hit` **有一条**且 `Amount == 0`（与 Shield / Invulnerable 同一个口径）。
  ⚠️ 现成断言 `RuleEngineTest_S14.cs:477` 已经在钉「挡下也发 `Amount = 0`」那一家族，
  加固堡那一格即可。🧨 把 `EmitHit(ctx, u, 0)` 从那一段删掉 ⇒ 本行红。

---

## 四、没查清 / 没做的 + 顺手发现的

1. 🔴 **`A1399` + `A1397①` 的**改法**落点 = `RuleEngine/Core/WhenEvent.cs`（⛔ 不在本笔白名单，停手）。**
   要加的是 `WhenEvents.Matches` 里一句：**`triggers:<kw>` 这一族中，`SelfOnly == false`
   的监听器不许 `ReferenceEquals(listenerUnit, subject)`**（原版正是用它把「自己」与「别人」
   分成 `Mob = 300` / `OtherCardMob = 645` 两个 id）。
   ⛔ **本笔没有**在 `RuleCore` 里另写一份广播来绕过它（那就是「两处写同一条规则」）。
   ⇒ **请调度台派一个拿到 `WhenEvent.cs` 的写手一次收口。**
2. 🔴 **`A1258` 的 ① 在**原版**里是「攻方身上的【附魔卡】」那一族**（见 §2.3），
   而我们引擎**没有附魔层** ⇒ 那半边**做不了**。本笔落的是**语义等价的那一半**
   （攻方自己的 `AboutToAttack` 能力 ⇒ 取消）。⇒ **要真照原版做 ①，得先建「附魔」这一层**
   （那是另一个工程；`Core/BattleEvent.cs:180-183` 已记着这一层不存在）。
3. ⚠️ **`A1258` 没查清的那一条（原账就标着，本笔仍未查清）**：`RegisterPlayerActionCancelled`
   （`:1365`）自减的那个 `bm + 0x3bc` 计数器**管哪一格**、以及取消之后
   **「攻击配额 / 已行动标记要不要回退」**。本笔**故意不碰** `AttacksThisTurn` / `Exhausted`
   （调用点排在消耗之前 ⇒ 天然「没消耗」）。**不要**拿猜测去填这一格。
4. ⚠️ **`CardDef.cs` 要收的两件**（本笔白名单外，已写在代码 doc 里）：
   · 把 `RuleCore.LandingKeyword`（`RuleCore.cs:4620`）挪成 `KeywordTable.Landing`；
   · 把 `RuleCore.AboutToAttackKeyword`（`RuleCore.cs:4665`）挪成 `KeywordTable.AboutToAttack`，
     并按需要登记 `Prefixes` / `RoutableTriggers`（不登记 ⇒ 卡面写 `About To Attack: …` 会被
     `Normalize` **静默丢掉**）。
5. ⚠️ **顺手发现①**：`A1399` 账上那句「`ReactToUnitUsedSacrifice` 的棋盘那一跳传的是**空**」
   **是 Ghidra 掉实参的假象** —— 这一条**同一个坑**在别的账上也出现过风险：
   `BattleManagerSupport__BroadcastUnitLanded.c` / `BroadcastUnitMob.c` 的棋盘跳在 `.c` 里
   同样「看着只传 1 个实参」，机器码里各有一条 `mov rdx, rbp`（VA `0x1809B82E1`）。
   ⇒ **建议**：凡「靠 `.c` 里实参个数」下的结论，一律回 VA 核一遍
   （工具现成：`工具/rva_of_method.py` + `工具/disasm_va.py`）。
6. ⚠️ **顺手发现②**：本笔搬迁 stealth 到期那一段**改变了它与 `Auras.Recompose` 的相对次序**
   （原来在重算之前、现在在之后）。全池 **0 张**卡用光环授予 `Stealth`
   （`cards_engine.json` 1126 张扫 `have Stealth` / `gain Stealth`：只有一次性 `Give Stealth`）
   ⇒ **今天不可观测**；万一将来加了这种光环，这一格要重新判（已就地写进 `RuleCore.cs:1136-1141`）。
7. ⚠️ **顺手发现③（新账，归 `A1335`）**：本笔把「易伤折进一个数」这条口径的**代价**量清了：
   我们的 `max(1, 伤害 + 易伤 − 护甲)` 与原版 `max(1, 伤害 − 护甲) + max(1, 易伤 − 护甲)`
   **在普通档就不等价**（例：伤害 5 / 易伤 2 / 护甲 1 ⇒ 我们 6、原版 5）。
   ⇒ 这不是「堡垒那两档」的问题，是**整条易伤口径**的问题 ⇒ 要修就得**拉开模型**
   （`DamageAfterReduction` + `DamageAfterReductionOne` + 预览侧 + `RuleEngineTest_S14.cs:633`）
   ⇒ **建议单开一笔**，⛔ 别再当「子档位」处理。

---

## 五、验证（本笔做了哪些、没做哪些）

· ✅ **做了**：`git diff --numstat`（`RuleCore.cs` = **255 / 36**，**只此一个文件**）；
  行尾核对 = 该文件**纯 LF**（`b.count(b'\r\n')` = 0 / `b.count(b'\n')` = 7504+），
  改完仍 LF、**没有整篇重写**；逐段回读 diff 两遍。
· ⛔ **没做**（按简报）：**没跑 Unity**、**没跑 `typecheck.sh`**（编译由主对话统一跑）、
  **没动 git 的写操作**、**没改任何正本**、**没碰白名单外的任何文件**。
· ⚠️ **一次只改一个变量**：本笔四处改动彼此不相干（`BeginTurn` 次序 / `DeclareAttack` 新分支 /
  两个注释块），任一处单独回滚都不影响其余三处。
