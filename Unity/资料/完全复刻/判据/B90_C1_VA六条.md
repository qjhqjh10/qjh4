# B90_C1 · R9 §三 六条 VA 级悬案（2026-10-28 · 只读代理产出，调度台代落盘）

> 出处：代号 **C1** 的只读代理（`Explore`，无写权限）交回的完整报告，由调度台原样落盘。
> 反编译证据全部为**本轮独立现读**（VA 反汇编 + `.c` + 常量 + `stringliteral.json` + `dump.cs`），未沿抄历史结论；沿用旧结论处已标明。
> 工具：VA = `0x180000000 + RVA`，节表由 `工具/disasm_va.py` 自算；常量由 `工具/read_literal.py` 读，**位宽按 `mulss/comiss` 判定**。

---

## L219 · `BattleManager.BroadcastUnitDamaged`（外层）+ `BattleManagerSupport.BroadcastUnitDamaged`（棋盘跳）

**结论**
1. **外层不是纯转调**。先 `BattleManagerSupport.BroadcastUnitDamaged(this, damagedUnit, damageInfo)`；然后 `if (damageInfo != null)`：取 `source = damageInfo.actingCard(+0x10)`，判 `source != null`（**第二实参就是 null，不是丢参**），再要求 `challengeLog(+0x4D0) != null`，补**两笔战绩统计**：
   - `challengeLog.DamageStat(source.isPlayer(+0x40), damageInfo.damageValue(+0x18))` ⇒ `PlayerBattleStats/EnemyBattleStats.damageDone(+0x18) += value`（`isPlayer` 真 ⇒ Player、假 ⇒ Enemy）；
   - `challengeLog.LogDamage(damageInfo, damagedUnit, dealtByAttacker)` ⇒ 三个前件非空时 `Signal.Raise(new DamageSignal(damageInfo, damagedUnit))`；**`dealtByAttacker` 形参原版从不读**（机器码 `r9b` 未参与任何计算）。
2. **棋盘那一跳的第二实参 = 被打者**：`Object.op_Inequality(boardCard, damagedUnit)` ⇒ **原版确实排除被打者**，我们「预调 + 排除」的写法正确。
3. 棋盘另有一条支：卡**不在场**时，仅当 `damageInfo.damageType(+0x1c) == 4(Attack)` ∧ `damageInfo.actingCard(+0x10) != null` ∧ `op_Equality(actingCard, boardCard)` 三条同时成立才 `ResolveDamageDealt`。手牌两跳**不排除任何卡**（含被打者自己在手牌时会响两次，照抄）。
4. `DamageSignal` 的消费端 = `MissionChallenge` 子类（例 `DamageDeltToEnemyUnit.OnDamageDelt` ⇒ `UpdateProgress(damageInfo.damageValue)`，带 damageType/attackType/trait 过滤）。`DamageStat.damageDone` 进 `SpecificPlayerResult.playerStats`（对局结算/上报）。

**证据**
- VA `0x180958700`（RVA `0x958700`，外层）：`0x180958764 mov rbp,[rbx+0x10]`；`0x180958776~77E xor r8d,r8d / xor edx,edx / mov rcx,rbp / call 0x182FFB920`（**第二实参 edx=0 ⇒ 比 null**）；`0x1809587A1 mov r8d,[rbx+0x18]` · `0x1809587A8 movzx edx,byte[rax+0x40]` · `0x1809587AC call 0x18061D230`（DamageStat）；`0x1809587BD movzx r9d,r14b` · `0x1809587D0 call 0x18061D3D0`（LogDamage）。
- VA `0x1809B77C0`（Support）：`0x1809B78BC mov rdx,r14`（r14 = 2nd 形参 = damagedUnit）+ `0x1809B78C2 call 0x182FFB920` ⇒ `boardCard != damagedUnit`；`0x1809B78D4 cmp dword[rdi+0x1c],4` · `0x1809B78FE call 0x182FFB920`（source≠null）· `0x1809B793C call 0x182FFB790`（source==boardCard）。
- `.c`：`decomp_full/BattleManager__BroadcastUnitDamaged.c:13`（Support 预调）、`:16-18`（source≠null）、`:21-34`（两笔统计）；`BattleManagerSupport__BroadcastUnitDamaged.c:20-61`（棋盘）。`CardScript__ResolveDamageDealt.c:31`（absorbed 早退）。
- `dump.cs`：`DamageInfo` 字段 `actingCard +0x10 / damageValue +0x18 / damageType +0x1c / secondaryValue +0x20 / damageAbsorbed +0x24`（119230–119237）；`EntityScript.isPlayer +0x40`（21973）；`ChallengeLogMgr.DamageStat` RVA `0x61D230`（37225）、`LogDamage` RVA `0x61D3D0`（37228）；`PlayerBattleStats.damageDone +0x18`（37249）。`cpp2il/out_cpp2il_dev_ilspy/ChallengeLogMgr.cs:1202-1221`。`DamageSignal.cs`（载荷 2 字段）。`DamageDeltToEnemyUnit.cs`（消费者）。

**与现有实现的差**
- **排除被打者：已一致**（`RuleEngine/Core/RuleCore.cs:9350` 的 `if (absorbed || amount == 0) return;` + `:9372 PreCallBoardListener(...)`；注释已把 `0x1809B78BC` 写对）。
- **两笔战绩统计：没有对应物**。`DamageStat → damageDone` 全仓无此字段/无此累计；`LogDamage → DamageSignal` 我们无信号总线。最接近的替代是 `RuleEngine/Core/AchievementTally` 的 `DamageOf(seat, DamageKind)` + `CardPresentation/Shell/ProfileData.cs:662 case "DamageDeltToEnemyUnit"` —— 但它按 `DamageKind` 粗分，**不带**原版的 `dealtByShuriken/Sentry/Markerlight` 三个过滤位与精确 `attackType` 判据，属**近似**。这两笔按账上口径属 P2/P3（挑战榜/结算上报），不影响对局引擎行为。

**还差什么**：**判据已闭合**。仅剩「把两笔统计落地」属实现/账务问题（需调度台定归属）。

---

## L354 · `BattleManager.GetGameState`（状态串）

**结论**：真格式串**唯一**，是

```
WLP: {0}; WLE: {1}; CPP: {2}; CPE: {3}; CHP: {4}; CHE: {5}; CDP: {6}; CDE: {7}
```

（分隔符 = `"; "`，**不是** `": "`、**不是**换行）。8 个实参按 **(player, enemy)** 成对：
`WarlordDataString(player), WarlordDataString(enemy), CardsPlayPlayer(player), CardsPlayPlayer(enemy), CardsHandPlayer(player), CardsHandPlayer(enemy), CardsDeckPlayer(player), CardsDeckPlayer(enemy)`。
空表/空值的子串返回 **`""`（空串）**，不是 `"-"`。

**证据**
- `DAT_184281140` = VA `0x184281140`，RVA `0x4281140`；`il2cpp_out/stringliteral.json` 精确命中上面那一串（本轮现读）。
- `decomp_full/BattleManager__GetGameState.c:116` `System_String__Format(DAT_184281140, plVar1, 0)`（plVar1 是 8 元素数组，长度在 `plVar1+0x18`，元素是 `plVar1[4..0xb]`）。
- 8 个写的次序 = `.c:16`（Warlord 1）/`:32`（Warlord 0）/`:44`（Play 1）/`:56`（Play 0）/`:68`（Hand 1）/`:80`（Hand 0）/`:92`（Deck 1）/`:104`（Deck 0）。`1` = player（`GetWarlordDataString` 的 `param_2==1 ⇒ +0xC0 playerManager`、`0 ⇒ +0xC8 enemyManager`，dump.cs 30738-30739；`GetCardsDeckPlayer` 的 `GetCardsInDeck(...,isPlayer=param_2, !param_2)`，`.c:23`）。
- 子串字面量（本轮现读 `stringliteral.json`）：`0x18423c7d8="idg:"` · `0x184281ff8=",id:"` · `0x184281ef8=",h:"` · `0x184281c08=",am:"` · `0x184281d00=",ar:"` · `0x184281e00=",ca:"`；空表 fallback `0x1842b80e0=""`。
- 账上原先「二选一」的两个候选 `0x18426eaf8` / `0x18426ebf0` 分别是 `"{0}: {1}\n {2}: {3}\n {4}: {5}\n {6}: {7}"` 与其带尾随 `\n ` 版 —— **都不是 GetGameState 用的那条**。

**与现有实现的差**：`RuleEngine/Core/RuleCore.cs:14863` 现在写的正是**错的那个候选** `"{0}: {1}\n {2}: {3}\n {4}: {5}\n {6}: {7}"` ⇒ 必须改成上面那条。另外四个子串构造器（`WarlordDataString/CardsPlayedString/CardsInHandString/CardsInDeckString`，同文件 14872-14930）目前是自定格式（`名字 血/上限 …`、`卡id#实例号`、空表 `"-"`），与原版 `idg:/id:/h:/am:/ar:/ca:`、`index:`、末尾 `/`、空表 `""` 全部不同 —— 要重写（判据见账 L354 的 B89-R1b 段，本轮复核了父格式串与参数次序）。

**还差什么**：**父格式串 + 8 参次序已闭合**。四个子串的字段级格式本轮**未逐字复核**（沿用 B89-R1b 的结论）；要钉死下一步 = 逐条反汇编 `BattleManager__GetWarlordDataString/GetCardsPlayPlayer/GetCardsHandPlayer/GetCardsDeckPlayer` 的 `String.Format` 字面量（子字面量地址见上，可直接续查）。

---

## L381 · `BattleManager.GetTargetInPos`

**结论**：**形状不是「相机穿鼠标点打线 × 棋盘平面求交」**，而是**两次物理射线拾取**（触控松手时「指针下是哪张卡」）：
1. `origin = hudCamera(+0xA0).transform.position`；`dir = normalize(worldSpaceMousePosition)`，**当 `|v| ≤ 1e-5f`（`Vector3.kEpsilon`）时 `dir = Vector3.zero`**。
2. `Physics2D.GetRayIntersectionNonAlloc(new Ray(origin, dir), targetOnTouchUp(+0x340), -1f, 0)` —— **距离是 `-1f`**（原版怪值）。按 Unity `distance` 语义，负距离不产生命中 ⇒ **这支视为死支**。若命中，取 `hit[0]`（不排序），按三档：`ActiveSkillDisplay`（写 `activeAbilityDisplay(+0x590)`；`UIstate(+0x290) ∈ {0xA(10), 0xD(13)}` ⇒ 返回 null，否则返回 `cardSelected(+0x410)`）/ `ActiveWarlordAbility`（返回其 `parentCard`，+0x20）/ 裸 `CardScript`。
3. **实际生效的回退支**：`boardCamera(+0xA8).ScreenPointToRay(new Vector3(TouchInputManager.TouchPosition.x, .y, 0))` → 方向 **× `200f`** → `Physics.RaycastNonAlloc(ray.origin, ray.direction, boardTargetOnTouchUp(+0x348), **200f**, 1 << LayerMask.NameToLayer("BoardElements"))` → `hit.transform.GetComponentInParent<CardScript>()`。无命中 ⇒ 返回 0。
4. 两支各有一次 `Debug.DrawLine`（颜色 `(0,0,0,1)`）/ `Debug.DrawRay`（颜色 `(1,0.9216,0.0157,1)`），**第 4 实参 `1f` 是「绘制时长」，不是距离**（易与 200f 混淆）。

**证据**（VA，`0x18096ADD0` / RVA `0x96ADD0`）
- `0x18096AEF8~AF35` 长度平方 + `sqrt`；`0x18096AF3C comiss xmm3,[0x1834B2F44]` → **`0x1834B2F44` 4B float = `1e-05`**（8B 读法 `5.33e-35` 是垃圾）。
- `0x18096AFF8 call 0x1830CED20`，`xmm2 = [0x1834B2BC8]` → **4B float = `-1`**（8B 垃圾）。
- `0x18096B0E8~B119` 方向分量 `mulss` 由 `0x1834B3258` 提供 → **4B float = `200`**；同一常量在 `0x18096B39A` 作为 RaycastNonAlloc 的 maxDistance。
- `0x18096B3C9 call 0x182FF3D70`（LayerMask.NameToLayer，rcx=`DAT_184294cb0` = 字符串 **"BoardElements"**，`stringliteral.json` 现读）；`0x18096B414 shl eax,cl`；`0x18096B432 call 0x1830DA5E0`（Physics.RaycastNonAlloc）；`0x18096B47A call 0x180C64830`（GetComponentInParent）。
- `.c`：`BattleManager__GetTargetInPos.c:78`、`:104`、`:145`、`:214-224`、`:238`、`:240`、`:248`。字段名：`dump.cs` 30732/30733/30873/30936 = `hudCamera +0xA0` / `boardCamera +0xA8` / `cardSelected +0x410` / `activeAbilityDisplay +0x590`，30720 段内 `targetOnTouchUp +0x340` / `boardTargetOnTouchUp +0x348`。

**与现有实现的差**：账上「候选等价物 = `CardPresentation/Battle/TargetReticle.cs:511 ResolveAim`」是**错配** —— `ResolveAim` 是 `TargetReticleController.UpdateTrail`（准星两平面求交）的等价物，**不是** `GetTargetInPos`。真正同职能（指针下是哪张卡）的是 `CardPresentation/Hand/CardInteraction.cs:1638 Frontmost`（`MulliganPanel.cs:1361 CardAt` 转发它）：它用 `Physics2D.OverlapPoint`/真 collider + **自己按 z 排序取最小**，而不是原版 `GetRayIntersectionNonAlloc` 取 `[0]`；而且原版那一支因 `-1f` **恒 0 命中** ⇒ 我们的「对应物」其实对应的是原版**走不到的**分支。原版真正生效的回退支（boardCamera + ScreenPointToRay ×200f + BoardElements 层）我们**完全没有对应该几何**。

**还差什么**：**系数与结构已闭合**（1e-5f / -1f / 200f / 层名全部取到）。唯一未实测 =「`-1f` 在目标 Unity 版本下确实恒 0 命中」—— 从 Unity API `distance` 语义推的，**没有跑 Unity 实测**；要写成铁判据，下一步 = 在 Unity 里跑一次 `Physics2D.GetRayIntersectionNonAlloc(ray, buf, -1f, 0)` 看返回值。

---

## L748 · `CardScript.GetAdjustedDamage`

**结论**：**不是死存**。被 Ghidra 反成「算了没用」的 `uVar3`（机器码 `ebp/bpl`）就是 `DamageInfo` 构造的**第 6 个实参 `absorbed`**（落到 `+0x24 damageAbsorbed`），真被消费：
- `absorbed = HasCurrentTrait(230 dropPod /*0xe6*/) ∨ Has(240 dodge /*0xf0*/) ∨ Has(310 invulnerable /*0x136*/) ∨ Has(80 shield /*0x50*/)`；
- 若 `shouldCountBastion ∧ CurrentBastion>0 ∧ 伤害 ≤ CurrentBastion` ⇒ `absorbed = 1`；
- 随机档只在 `damageType ∈ {5(Random),10(RandomPoison),12(RandomAbility)}`（位掩码 **`0x1420`**）时走 `GetRandomInt(damageValue, secondaryValue+1)`；类型映射 `5→0(Normal)`、`12→3(Ability)`；
- 末：`new DamageInfo(actingCard, n, type', secondaryValue: 0, absorbed)`。
- 消费点 = `CardScript__ResolveDamageDealt.c:31`：`if (*(char*)(damageInfo+0x24) != 0) return;`（**被整份挡下 ⇒ 接收端根本不跑**）。

**证据**
- VA `0x1805ED4B0`（RVA `0x5ED4B0`）：`0x1805ED534~54A` 类型 5→0、12→3；`0x1805ED555/569/57D/590` 依次 `HasCurrentTrait(0xE6/0xF0/0x136/0x50)`（`0x1805ED595 movzx ebp,al`、`0x1805ED59A mov bpl,1`）；`0x1805ED5A7/5B5 call 0x180931710`（CurrentBastion）；`0x1805ED5C6 cmovle ebp,ecx(=1)`；`0x1805ED5E5 mov byte[rsp+0x28], bpl`；`0x1805ED5FE call 0x180878270`（`DamageInfo..ctor`，dump.cs 119241）。
- `.c`：`CardScript__GetAdjustedDamage.c:23`（`0x1420` 位掩码）、`:25`（GetRandomInt）、`:39/41/43/45`（四 trait）、`:53-55`（bastion）、`:63`（DamageInfo 构造）。
- `dump.cs:119227-119249`（DamageInfo 字段与 ctor 形参次序）；`dump.cs:45731/45744/45745/45750` = `shield=80 / dropPod=230 / dodge=240 / invulnerable=310`。
- `CardScript__ResolveDamageDealt.c:31`（`+0x24` 早退）。

**与现有实现的差**：**等价实现已落**：`RuleEngine/Core/RuleCore.cs:8443 ApplyDamageCore`（`:8469 hadShield/hadDodge`、`:8506 invulnerable`、空投舱/堡垒在 `DamageAfterReduction` 段 ~7370-7400），挡下时 `EmitHit(ctx,u,0)+return 0`。表示法不同（原版是 DamageInfo 上的一个合并 bool，我们是「直接返 0 + 发 0 伤事件」），但原版该 flag **唯一消费者**就是 `ResolveDamageDealt` 首行早退 ⇒ **可观测等价**（本轮确认）。

**还差什么**：**已闭合**。

---

## L1241 · `DAT_1834b3400`（`BattleManager.<ChooseUnitTarget>d__780.MoveNext` 里 `GetLocalizedTargetted` 调用点的 tooltip 偏移）

**结论**：`DAT_1834b3400` = **4 字节 float `-3.0f`**（**不是** 8 字节 `57.6`）。用法 = **`Vector3.up × (-3f)`**（即 `(0,-3,0)`）加到 `BattleManager.Get2DPosOfCard(card, useCenter:true)` 的结果上 ⇒ **tooltip 位置 = `Get2DPosOfCard(card) + (0,-3,0)`**，再传给 `EverguildTooltipManager.Show(..., position, ...)`。`GetLocalizedTargetted` 本体**不含偏移**，偏移在调用方 `_ChooseUnitTarget…MoveNext`。

**证据**
- VA `0x1809A3F45`：`movss xmm1, dword ptr [rip+0x2b0f4b3]` → 目标 `0x1834B3400`；随后 `0x1809A3F77/7B/85` 三条 **`mulss`（单精度）** 把 `Vector3.up` 三分量乘 `xmm1`，`0x1809A3F95/99/FA9` `addss` 加到 `Get2DPosOfCard` 的三分量上。
- `read_literal.py`：`0x1834B3400` → 4B float **`-3`**（✅）/ 8B double **`57.6`**（✅，**同址双档都像字面量 ⇒ 陷阱**；按 `mulss` 必须取 4B）。
- `Vector3` 静态块 `dump.cs:537034-537036`：`zeroVector 0x0 / oneVector 0xC / upVector 0x18` ⇒ 乘的是 `Vector3.upVector`。
- `BattleManager._ChooseUnitTarget_d__780__MoveNext.c:52-55`（`*(...+0xb8)+0x18` = upVector，`DAT_1834b3400` 乘上去）；`get2DPosOfCard` 真身 `BattleManager__Get2DPosOfCard.c:20-60`（InPlayOrDying ⇒ effectAnchor → boardCamera.WorldToScreenPoint → hudCamera.ScreenToWorldPoint(z=planeDistance)；否则裸 transform.position）。

**与现有实现的差**：`CardPresentation/Battle/BattleDriver.cs:35964` `Tooltip.Show(body, card.transform.position)` —— **既没走 `Get2DPosOfCard` 的相机回环，也没加 `(0,-3,0)`**。要改就在 35964 这一行前把 `card.transform.position` 换成「原版两步」。⚠️ 我们的 `Canvas2DElements.planeDistance = 20.3f`（账 L1241 已记），回环那一步有本地值。

**还差什么**：**已闭合**（常量位宽、乘的向量、基座链、调用点全部取到）。

---

## L1142 · `MinionManager.RemoveSwarm` / `CardScript.RemoveSwarm`（+ `MoveBackToHand` / `AddExecuteSwarm` 的撤销支）

**结论**：**原版没有「回滚点」**，也不需要回滚。
1. 属性只在 `CardScript.<>c__DisplayClass401_0.<ExecuteSwarm>b__0`（协程完成回调）里**一次性挂上**：构造 `CardEffect{ buffType=1(addStats), melee=本卡 CurrentMeleeAttack(+0x48), range=本卡 CurrentRangeAttack(+0x50), health=本卡 **currentHealth(+0x68)** }` → `host.AddEffect(effect, **enchantmentCardId = -1**)`（**无源、永久**）→ `DestroyUnitSilent(just)` → `BroadcastUnitSwarm(just, host)`。**中间没有任何「先挂上 / 失败再撤」的两段式**。
2. 出队时（`BattleManager.ResolveExecuteSwarm`）只做闸：`targetCard(+0x28) 不在场 ⇒ FinishResolvingAction`；`actingCard(+0x20) 不在场 ⇒ FinishResolvingAction`；两者都在场才 `CardScript.ExecuteSwarm(actingCard, targetCard, callback)`。⇒ **被跳过时 buff 从没挂上**，无需撤销。
3. `AddExecuteSwarm` 只是把 `BattleAction(actionType = 0x4F = 79 executeSwarm, actingCard, targetCard)` 入 **第 3 道（DelayedInstant，`+0x360`）**（调用点 `CardScript__ResolveCardPlayed.c:79` 传 `priority=3, blockPlayerActions=1`）。
4. `RemoveSwarm` 两个方法**只关图标**：`CardScript.RemoveSwarm` = `SwarmIconController.Toggle(false)`；`MinionManager.RemoveSwarm` = 遍历**双方**在场表、跳过 `exceptionCard` 与非 `IsInPlay`，逐张调 `CardScript.RemoveSwarm`。**不摘单位、不动属性**。
5. `CardScript.MoveBackToHand`：`SetSortingLayer(Hand)` → `StopAnimsInPlay` → 若 `cardState(+0x228) ∈ {10,4}`：`cardType==0(Minion)` ⇒ `DissolveMinionCard(false)+SetStateInHand`；`cardType ∈ {0x14,0x28} ∧ targeted` ⇒ `UnDissolveWholeCard` → `SetStateInHand`。**同样不含任何属性回滚**。

**证据**
- `decomp_full/CardScript.__c__DisplayClass401_0___ExecuteSwarm_b__0.c:24/27`（CurrentMelee/RangeAttack）、`:31`（`+0x68` currentHealth）、`:41`（effect +0x60）、`:45`（`AddEffect(host, effect, -1)`）、`:51`（DestroyUnitSilent）、`:54`（BroadcastUnitSwarm）。
- `decomp_full/BattleManager__ResolveExecuteSwarm.c:81/84/88/95`；`BattleManager__AddExecuteSwarm.c:28`（`BattleAction(0x4f,…)`）、`:80`；调用点 `CardScript__ResolveCardPlayed.c:79`（`…,3,1,0`）。
- `CardScript__RemoveSwarm.c:6-21`；`MinionManager__RemoveSwarm.c:15-83`；`CardScript__MoveBackToHand.c:13/15/16/17/19/24/26/30`。
- 运行时地址：`MinionManager$$RemoveSwarm` VA `0x18062D370`（RVA `0x62D370`）· `CardScript$$ExecuteSwarm` VA `0x1805EC880`（RVA `0x5EC880`）。

**与现有实现的差**：**结构已一致**。`RuleCore.TrySwarmMerge:4568-4578` 入 `DelayedInstant` 队列（`:4576`），出队 `SwarmMergeNow:4582` 按身份现查、不在场就不合并；数值也已是原版口径 —— `:4605 host.Health += just.Health`、`:4606 host.AddStatMod("health", just.Health, ...)`（2026-10-28 B89-E14 已把 `MaxHealth` 改成 `Health`）。**不需要加回滚**。唯一「不建模」= `blockPlayerActions` 那个 bool 位（原版照抄值 = 1，我们队列模型无对应位；语义未解，**不建议凭猜补**）。

**还差什么**：**已闭合**（由「属性只在回调里挂 + 出队两条 IsInPlay 闸 + RemoveSwarm 只关图标 + MoveBackToHand 无属性操作」四条共同证明）。若要更严可补：`CardScript.ExecuteSwarm` 内部只播动画、回调由 `UIGenericEventCatcher.SourceDelegate` 触发（`.c:70-89` 已读），**动画被中断时回调走不走**属 Unity 表现层，未实测。

---

## 六条汇总表

| 账行 | 闭合/未闭合 | 一句话结论 |
|---|---|---|
| L219 | ✅ 闭合 | 棋盘跳 `op_Inequality(boardCard, **damagedUnit**)` ⇒ 原版**排除被打者**；外层第二实参 `= null`；两笔统计 = `DamageStat(actingCard.isPlayer, damageValue)→damageDone` + `LogDamage(damageInfo, damagedUnit, dealtByAttacker)`，后者 `dealtByAttacker` **未被使用**；我们两项统计均无对应物（近似项 = AchievementTally/ProfileData） |
| L354 | ✅ 闭合 | 真格式串 = `WLP: {0}; WLE: {1}; CPP: {2}; CPE: {3}; CHP: {4}; CHE: {5}; CDP: {6}; CDE: {7}`，8 参按 (player, enemy) 成对；空表 = `""`；我们 `RuleCore.cs:14863` 用的是**错候选** |
| L381 | ✅ 闭合（含 1 点未实测） | **不是棋盘平面求交**，是 hudCamera 射线 `Physics2D.GetRayIntersectionNonAlloc(..., -1f)`（死支）+ boardCamera `ScreenPointToRay(TouchPosition)` `×200f` → `Physics.RaycastNonAlloc(..., 200f, 1<<NameToLayer("BoardElements"))`；常量 `1e-5f / -1f / 200f`；「-1f 恒 0 命中」未在 Unity 实测 |
| L748 | ✅ 闭合 | `absorbed`（DamageInfo `+0x24`）**不是死存**：四 trait(230/240/310/80) 或 bastion≥伤害 ⇒ 1，被 `ResolveDamageDealt.c:31` 消费；我们 ApplyDamageCore 已等价 |
| L1241 | ✅ 闭合 | `DAT_1834b3400` = **4B float `-3.0f`**（8B 的 57.6 是误读）；`tooltip 位置 = Get2DPosOfCard(card) + Vector3.up×(-3)`；我们 `BattleDriver.cs:35964` 没加偏移也没走相机回环 |
| L1142 | ✅ 闭合 | 原版**没有回滚点**：属性只在 ExecuteSwarm 完成回调里 `AddEffect(..., -1)` 一次性挂上，出队两条 IsInPlay 闸挡掉即「从未挂上」；`RemoveSwarm` 只 `SwarmIconController.Toggle(false)`、`MoveBackToHand` 只改状态/图层；我们已入 DelayedInstant + 用 currentHealth |

## 顺手发现

1. 🔴 **`RuleCore.GetGameState` 落的是错候选格式串**（`RuleCore.cs:14863`）—— 本轮唯一「代码与真判据直接冲突」的一条；连同四个子串构造器一起要重写。
2. 🔴 **账 L381 的「候选等价物 = `TargetReticle.ResolveAim`」是错配**：`ResolveAim`（`TargetReticle.cs:511`）与其 doc 都指向 `UpdateTrail`（准星两平面求交）；`GetTargetInPos` 是「松手时指针下是哪张卡」的拾取器，职能对应物应是 `CardInteraction.Frontmost:1638` / `MulliganPanel.CardAt:1361`。而且它们对应的是原版 `-1f` **死支**的分支语义（取数组 `[0]`），原版真正生效的回退几何我们完全没做。账上这条「差异」应按此重写。
3. **`ChallengeLogMgr.LogDamage` 的 `dealtByAttacker` 是死形参**（VA `0x18061D3D0` 全程不读 `r9b`；ILSpy `ChallengeLogMgr.cs:1210-1221` 同样不用）。抄接口时别以为它参与过滤。
4. **`DamageInfo.damageAbsorbed(+0x24)` 是「四 trait + bastion」合并成的一个位**；我们引擎没有这个字段，靠「返 0 + EmitHit(0)」表达。唯一消费者是 `ResolveDamageDealt` 首行早退 ⇒ **可观测等价**；将来若再加一个「读 absorbed 但不等价于早退」的消费者，就会暴露差异。
5. **`read_literal.py` 的双档陷阱在 L1241 实锤**：`0x1834B3400` 的 4B/8B **都 ✅「像字面量」**（-3 / 57.6）。判宽度只能看机器码操作码（`mulss`=4B / `mulsd`=8B）。
6. **`B88_R9` §三 这张表整体已过期**：本轮独立复核后，L219/L381/L748/L1241/L1142 的判据与 B89-R1b 一致（本轮给的是**独立 VA/常量证据**，不是沿抄）；只有 **L354 的代码仍用错串**、**L1241 的偏移在代码里仍未应用** 这两条是真·未落地。
7. **`BattleManager__GetTargetInPos` 的 `Debug.DrawRay` 颜色不是白色**：`0x1834B3040` 四字节 = `(1, 0.921569, 0.0156863, ?)`（黄系准星色）；`DrawLine` 那条是 `(0,0,0,1)`（黑）。两条的 `1f` 都是**时长**。抄这套几何时别把颜色/时长当距离。
8. **`GetCardsDeckPlayer` 空表返回 `""` 而非 `"-"`**（`DAT_1842b80e0` = 空串），且牌库子串**无 `index:` 前缀**（账上 B89-R1b 已记，本轮常量侧复核一致）。

## 交接给下一个代理

- 所有 VA 均为「VA = `0x180000000` + RVA」，节表映射由 `工具/disasm_va.py` 自算；常量由 `工具/read_literal.py` 读，**位宽按 `mulss/comiss` 判定**。
- L354 的子串格式要钉死：`BattleManager__GetWarlordDataString/GetCardsPlayPlayer/GetCardsHandPlayer/GetCardsDeckPlayer` 四个 `.c` 都在 `d:/2/tools/decomp_full/`，子字面量地址已在 L354 证据里，可直接 `stringliteral.json` 展开，**不必再跑 Ghidra**。
- L381 的 `-1f` 要钉成铁判据：需在 Unity 里实测一次 `Physics2D.GetRayIntersectionNonAlloc(ray, buf, -1f, 0)`（只读代理不能跑 Unity）。
- **未做**：任何 `.cs`/`.tsv`/账主表/`CLAUDE.md` 均未改；`d:/2` 只读、未写。
