# T05b · cardAnim 索引映射与位置参数喂给谁

> 只读判据代理产出 · 2026-10-16 · 补 `任务/T05_逐卡演出与特效接线.md` §2·5 / §2·6 缺的那一格。
> 判据权威顺序：原版反编译 `d:/2/tools/decomp_full/` ＞ 字段结构 `d:/2/tools/il2cpp_out/dump.cs`
> ＞ 原版卡 SO 真值快照 `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`
> ＞ 我们自己的表（`数据/游戏数据/*.json` · `数据/索引/anim_address_map.json`）。
> ⛔ 没跑 Unity、没动 git、没改任何正本或 `.cs`。

---

## 一、三问的答案

### 问 1 · `cardAnimations[]` 的 N 条是「同一事件的 N 拍」还是「N 个不同事件」？

**结论：是「同一个 AbilityLogic（= 同一个技能的一次结算）的 N 个【有序演出步骤】」，N 条各有自己的
`CardAnim` SO / `AnimInfo`（各自独立的 start/end/pointTowards/时长），原版**整表按 List 顺序全播**。
⛔ 不是「N 个不同事件」。**

出处（三条互相印证）：

1. **结构**：`AbilityLogic.cardAnimations` 的类型是 `List<AssetReferenceTyped<CardAnim>>`
   （`il2cpp_out/dump.cs:19457`），而 `CardAnim` 只有一个字段 `public AnimInfo animInfo; // 0x18`
   （`dump.cs:20376-20383`）⇒ **列表里每一条 = 一张独立的 CardAnim SO = 一拍**。
   `AbilityLogic` 本身就是「一个技能」（它属于 `CardAbility.cardAbilities[]` 下的一档，
   `dump.cs:20355 public List<AbilityLogic> abilityLogic; // 0x50`）。
2. **播放**：`AbilityLogic.PlayAnim` 对这张表做 **`foreach`**（`AbilityLogic__PlayAnim.c:45-46` 取枚举器、
   `:56` `MoveNext` 循环），每条 `AssetReferenceTyped<CardAnim>.__Load` 出 SO（`:65`），
   取 `so + 0x18`（= `CardAnim.animInfo`，`:70`）——**没有「挑一条」的动作，全部都播**。
3. **数据实证**：原版卡 SO 快照里，卡 `5`（Sororitas，`trigger=605 / chosenAbility=10`）那一条 ability 的
   `cardAnimations` 是**有序的 3 个 GUID**：

   | 序 | GUID（`card_anim_map` 里的 CardAnim 名） | 它指向的 VFX prefab | `sp` / `ep` / `pt` |
   |---|---|---|---|
   | 0 | `Faith_trigger_self` | `Faith_trigger_unit` | 0 / 0 / 0（自身） |
   | 1 | `Atk_Sororitas_MissileVolley_board_UP` | `BulletImpact_Sororitas_MissileVolley_board_up` | 0 / 0 / 2 |
   | 2 | `Atk_Sororitas_MissileVolley_board_DOWN` | `BulletImpact_Sororitas_MissileVolley_board_down` | 4 / 4 / 2（4=enemyHero） |

   —— 一眼就是「一条技能的 3 拍」：自身触发特效 → 上弹着点 → 下弹着点。
   （GUID→名：`数据/索引/anim_address_map.json` 的 `cardanim_guid_to_name`；参数：
   `数据/游戏数据/card_anim_map.json["Sororitas"]`。名→VFX：`cardanim_to_asset.targetName`。）

**顺序 = List 下标顺序**（foreach 从 0 到 N-1）；原版没有「按下标挑一条」的逻辑。

---

### 问 2 · 原版谁来遍历这个数组？按什么挑下标？

**结论：遍历者 = `AbilityLogic.PlayAnim(BattleManager, AbilityData, List<CardScript> abilityTargets)`；
它由 `AbilityLogic.PlayAbility` 调用。它【不挑下标】——整表按序全播。**

- **调用点**：`AbilityLogic__PlayAbility.c:273`（`AbilityLogic.PlayAbility` 结尾；
  前面 `:266-272` 有个「不调」的条件：`chosenAbility == 0x32`(DrawCard) / `0x145` / `0xc` 三档跳过动画）。
  签名：`dump.cs:19501 public void PlayAbility(...)` · `dump.cs:19516 private void PlayAnim(...)`。
- **遍历 + 取 ANIMINFO**：`AbilityLogic__PlayAnim.c:45-46`（`GetEnumerator` on `this+0x18`）· `:56`（循环）·
  `:65`（`Load` 出 CardAnim）· `:70`（`+0x18` = `animInfo`）。
- **唯一会「跳过某一条」的条件**：该条的 `startPosOption`/`endPosOption` ∈ {`14`,`15`,`16`,`17`}
  （空位档）时，会先 `MinionManager.IsAvailableSlot(槽号)` 查一下；没空位就 `goto` 跳过这一条
  （`AbilityLogic__PlayAnim.c:104-167`）。**这不是「挑下标」，是「这拍条件不成立就不播」**。
- **一条会因目标数而「复制」**：只要三条选项里**任意一条 == `2`（abilityTarget）**，
  就 `while` 遍历 `abilityTargets` 列表，**每个目标各播一份**该拍（`AbilityLogic__PlayAnim.c:171-249`；
  `bVar13 = (targetIndex == count-1) && animInfo.waitAnimToApplyEffect`（`+0x58`）用来决定最后一拍
  是否 `waitForAnim`）。
- **三选项各自解析成 Transform 后交给 `BattleManager.AddCardAnim` 入队**：
  `AbilityLogic__PlayAnim.c:194/203/215` 调 `AbilityLogic.GetPos(...)`，`:232`（多目标分支）`:273`（单目标分支）
  调 `AddCardAnim(manager, animInfo, startPos, endPos, rotateTowards, waitForAnim, priority, checkIfCasterInPlay, casterCard, targetCard, blockPlayerActions)`。
  `priority = abilityData+0x44`、`blockPlayerActions = abilityData+0x48`、`casterCard = abilityData.thisCard(+0x10)`。
  签名 → `dump.cs:32523`。

---

### 问 3 · 挑出来的那条，它的 `startPosOption` / `endPosOption` 喂给谁？

**分两半答（这一格的答案比 T05 原来写的复杂）：**

**(a) `BattleManager.GetAnimTransform` 的 `param_2` 确实就是 `AnimInfo` 的 `startPosOption`/`endPosOption`。**
- `BattleManager__CreateAnimFromAnimFxController.c:11-17`：
  `uVar1 = GetAnimTransform(mgr, animInfo + 0x18, …)` · `uVar2 = GetAnimTransform(mgr, animInfo + 0x1c, …)`
  → `CreateAnim(mgr, animInfo, uVar1, uVar2)`（即 `startPosOption→startParent`、`endPosOption→endParent`）。
- `BattleManager._DelayedVFXPlay_d__469__MoveNext.c:44-49`：同一对（`animInfo+0x18`、`animInfo+0x1c`）
  → `CreateAnim:59`。
- ⚠️ **`pointTowardsOption`(0x20) 这两处都不用**（只解 0x18/0x1c）；`CreateAnim` 的 `rotateTowards`
  在这两条路上来自别处（`d__469` 的 `rotateTowards` 字段 0x50，由调用者传进来）。

**(b) 🔴 但 `cardAnimations[]`（技能）这条路径，选项→锚点的解析器【不是】`GetAnimTransform`，
而是 `AbilityLogic.GetPos(BattleManager, AbilityData, CardScript abilityTarget, PositionOption)`。**
- 位置：`AbilityLogic__GetPos.c`（**331 行**，`dump.cs:19525` 签名，RVA `0x8174A0`）。
  它与 `BattleManager.GetAnimTransform`（RVA `0x963B00`）是**两个独立函数**——GetPos 体内**不调** GetAnimTransform。
- **链路**：`PlayAnim` → `GetPos`（解出 start/end/rotate 三个 `Transform`）→ `AddCardAnim` →
  **存进 `BattleAction`**（字段 `startParent 0xA8` / `endParent 0xB0` / `rotateTowards 0xB8` / `animInfo 0xA0`，
  `dump.cs:~118982`）→ 入队 → 队列执行 `BattleManager.ResolveAnim(BattleAction)`
  （`BattleManager._ResolveAnim_d__466__MoveNext.c`，读 `+0xA8/0xB0/0xB8`）→
  `DelayedVFXPlay`（`d__469`）→ 因为 **`startParent` 已被 GetPos 填成非空**，直接走 `CreateAnim:59`，
  **跳过** `GetAnimTransform`。
- ⇒ **在该技能路径上，`GetAnimTransform` 只在 `startParent == null` 时兜底**（`d__469:35-47` 那个
  `Object.op_Implicit(startParent)` 为假的分支）。
- ∴ **T05 §2·6 那张「只有 10 档、其余 LogError + 退回督军锚点」的表，是 `GetAnimTransform` 的行为，
  不是 cardAnim-ability 路径的行为**。`AbilityLogic.GetPos` 把 **36 档全实现了**。

#### `AbilityLogic.GetPos` 逐档（`AbilityLogic__GetPos.c`，逐档亲读）

| 档 | 解析成 | 出处 |
|---|---|---|
| `0 self` | `abilityData.thisCard(+0x10)` → **`CardScript.GetEffectAnchor()`** | `:183-185,187` |
| `1 cardTarget` | `abilityData.targetCard(+0x20)` → `GetEffectAnchor()` | `:186-190` |
| `2 abilityTarget` | 传入的 `param_3` 非空 → 它 `GetEffectAnchor()`；空则退回 `+0x20`/`+0x10` | `:191-219` |
| `3 friendlyHero` / `9 enemyPower` | `BattleManager.GetHero(mgr, isPlayer)` → `GetEffectAnchor()` | `:220-221,321-325` |
| `4 enemyHero` | `BattleManager.GetHero(mgr, !isPlayer)` → `GetEffectAnchor()` | `:223-231` |
| `5 boardCenter` | `mgr + 0x1d0`（裸 `Transform`） | `:232-235` |
| `6/7/8/10/11` | `BattleHud.GetUIAnchor(hud, 档号, isPlayer)` | `:236-241,255-256` |
| `12 friendlyMana` / `13 enemyMana` | `BattleHud.GetUIAnchor(hud, 12/13, isPlayer)` | `:242-254,255-256` |
| `14 nextFriendlyMinionSpot` | **`CreateTempAbilityTransform(isPlayer, getEnemyPos=0, slot=1)`** | `:258-265` |
| `15 nextEnemyMinionSpot` | `CreateTempAbilityTransform(isPlayer, getEnemyPos=1, slot=1)` | `:266-273` |
| `16 secondFriendlyMinionSpot` | `CreateTempAbilityTransform(isPlayer, 0, slot=2)` | `:274-281` |
| `17 thirdFriendlyMinionSpot` | `CreateTempAbilityTransform(isPlayer, 0, slot=3)` | `:282-289` |
| `18 friendlyHand` | `BattleManager.GetPlayerHand(mgr, isPlayer)` 的 `transform` | `:290-296` |
| `19 enemyHand` | `GetPlayerHand(mgr, !isPlayer)` 的 `transform` | `:297-305` |
| `50 friendlyDeck` | `BattleManager.GetDeckManager(mgr, isPlayer)` 的 `transform` | `:306-318` 区段 |
| `55 enemyDeck` | `GetDeckManager(mgr, !isPlayer)` 的 `transform` | `:160-170`（>0x32 区段） |
| `60/61/70/71` | `PlayerManager.Get{SpiritStone,Psychic}ManaTransform`（按 `isPlayer` 取反对侧） | `:110-145` |
| `80/90/120/130` | `PlayerManager.GetNextTo{Psychic,Plasma}ManaTransform` | `:15-70` |
| `100/110` | `PlayerManager.GetPlasmaManaTransform` | `:28-52` |
| `140/145/150/155` | `BattleHud.GetUIAnchor` / `BattleManager.get_{Friendly,Enemy}HandAnchor` | `:75-100` |
| 其余真落不到的 | `switchD_18081797e_caseD_3` → `GetHero(isPlayer).GetEffectAnchor()` | `:321-329` |

`isPlayer` 一律取 `abilityData.thisCard(+0x10) + 0x40`（= `EntityScript.isPlayer`，`dump.cs:21963`）。

#### `GetPos` 与 `GetAnimTransform` 的实质差异（这一条最重要）

| 项 | `BattleManager.GetAnimTransform`（10 档） | `AbilityLogic.GetPos`（36 档） |
|---|---|---|
| 覆盖档 | `0/1/2/3/4/5/14/18/19/50/55`；**其余 LogError + 本地督军锚点** | **36 档全实现**（6-17 / 60-155 都有真锚点） |
| `0/1/2` | 恒取 `卡 +0x290 + 0x1a0`（= `BattleCardUI.effectsAnchor`，**不看状态**） | `CardScript.GetEffectAnchor()` ⇒ **按卡状态 `+0x228` 二选一**（见下） |
| `14` 空位 | 恒用**本地玩家** `playerMinionManager(+0xe0)` | `CreateTempAbilityTransform(isPlayer, 0, 1)` ⇒ 用**行动方**那一侧 |
| `15/16/17` | 未实现（报错档） | 真实现（`getEnemyPos`/`slot=1/2/3`） |
| `12/13` 能量 | LogError 档 | `BattleHud.GetUIAnchor(12/13, isPlayer)` |

> `CardScript.GetEffectAnchor`（`CardScript__GetEffectAnchor.c`）：读 `card + 0x228`（某状态枚举），
> 若状态 ∈ {`4,9,0xc,10,8,1,0,0x10`} → 返回 `card+0x290 → +0x30` 的 `transform`，
> 否则返回 `card+0x290 → +0x1a0`（= `effectsAnchor`）。
> ⇒ **GetPos 是 state-aware**（在手牌/墓地等状态会锚到卡自己的 transform），GetAnimTransform 不是。

---

### 附 · `AnimInfo` 不是「10 个参数」——实际 ~22 个字段（`dump.cs:25205-25249`，逐行现读）

除 T05 列的那 10 个外，还有：
`animAdressable(0x10)` · `easeCurve(0x40)` · `orientToVelocity(0x48)` · `applyMovementOffsets(0x49)` ·
`offsetMovementAxis(0x50)` · `waitAnimToApplyEffect(0x58)` ·
🔴 **`screenShake(0x60)`（`OverwriteCameraShakePreset`）** · `minionAnimationClipInfo(0x68)` ·
`minionMovementInfo(0x70)` · `unitsShake(0x78, List<UnitShake.UnitShakeInfo>)` · `tweenAnim(0x80, UnitTweenSO)`。

**`ResolveAnim` 真的消费这些**（`BattleManager._ResolveAnim_d__466__MoveNext.c`）：
`screenShake`（`+0x60` → `CanPlay`/`Play`，`:96-105`）· `unitsShake`（`+0x78`，`:120-136`）·
`tweenAnim`（`+0x80` → `UnitTweenSO.BuildSequence`，`:141-156`）·
`minionAnimationClipInfo`（`+0x68` → `ResolveAnimAnimationClip`）· `minionMovementInfo`（`+0x70` → `CardScript.MoveAndDisappearAnim`）。
⇒ **T05 §六第 2 条「原版有没有逐 cardAnim 的震屏字段」的答案：有，`AnimInfo.screenShake`（0x60）。**
⚠️ `shouldMoveVFX` 的旧名是 `shouldMoveAnimation`（`dump.cs:25240` `FormerlySerializedAs`）。

---

## 二、按这个结论，我们这边该怎么接

### 现状（判据：本仓读 + 上面三问）

- 我们引擎侧**一次技能只发一条 `EvtKind.Ability`**（T05 §2·5 自陈）。
- `VfxMap.Resolve(evt, faction, cardId)` **一次只回一个 prefab 名**（`Core/VfxMap.cs:424`），
  按 `confPriority` 把 N 条折叠成 1（`:334-370`）⇒ **下标/顺序在我们这层已经丢了**。
- `数据/游戏数据/card_anim_map.json` 是「阵营 → **CardAnim 资源名** → 参数」，
  **键是 CardAnim 资源名**（`Faith_trigger_self` / `Atk_…_UP` / `AeldariRecall_2_ability`），
  **不是**「<卡名>_<下标>_<事件>」——那个 `<下标>` 是资源名自带的变体号，**不是列表下标**。
- 因此「顺序」这一格的真值**只在原版卡 SO 的 `cardAnimations[]`** 里（见 §一 问 1 的数据实证）。

### 要接，分四步

1. **新建一张带【顺序】的逐卡逐事件表**（离线可做，全部判据本地齐）：
   对每张卡、每条 `cardAbilities[].abilityLogic[]`，按 `cardAnimations[]` 的**数组序**展开成
   `{卡id, trigger, chosenAbility, index, cardAnimName, vfxPrefab, 10(+12)个参数}`。
   join 链（三跳，逐跳本地可解）：
   - `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`
     → `cardAbilities[].abilityLogic[].cardAnimations[].m_AssetGUID`（**有序**）
   - → `数据/索引/anim_address_map.json.cardanim_guid_to_name`（GUID → CardAnim 名；1,099 条）
   - → `数据/游戏数据/card_anim_map.json[<faction>][<名>]`（参数）
   ⇒ 输出一张**有序**的 `<cardId>_<event>` → `[step…]` 表（现有 `card_vfx_by_card.json` 是**折叠后**的，
   ⛔ 别改它去承载顺序，**新开一张**，铁律 6）。
2. **解析器照 `AbilityLogic.GetPos`（36 档）重写/补齐** `Battle/AnimAnchors.cs`：
   - 至少补：`6-13 / 15-17 / 60-155`（GetPos 有真锚点，不再是报错档）；
   - `0/1/2` 改成 **state-aware**（走 `GetEffectAnchor` 语义，按「卡在手牌/场上/墓地」二选一）；
   - `14` 改成**行动方**那一侧（现在写的是本地玩家 `+0xe0`）；
   - `pointTowardsOption(0x20)` 在 GetPos 路径**要解**（喂 `rotateTowards`）。
   ⛔ 别推翻 `AnimAnchors.cs` 文件头里那两条已订正的判据（`+0x40` = `isPlayer`、`18/19` 自洽），
   它们与本节不冲突。
3. **播放**：把「一次技能一条动画」扩成「按表展开的 N 条，按序播」。
   引擎侧 `RuleEngine` 现在只产一条 `EvtKind.Ability` ⇒ 需主对话定「驱动侧按表展开」还是「引擎多发」。
   ⚠️ 只有**最后一拍**才 `waitForAnim`（`bVar13` 逻辑，见 §一 问 2）。
4. **参数**：先接 `startDelay / vfxDelayTime / timeAtStartPos / timeMoving / timeAtEndPos`
   （时长那半已用，见 `Core/AbilityAnimDurations.cs`）+ 三个位置选项；
   后续再按需接 `easeCurve / screenShake / unitsShake / tweenAnim / minionAnimationClipInfo` 等。

⚠️ 全程守 T05 的两条硬规矩：**判据只留一处** · **不许静默失败**。

---

## 三、还差什么 / 判不准的（⛔ 不猜，写清搜过哪些词 / 哪些路径）

1. **`BattleHud.GetUIAnchor(hud, anchorId, isPlayer)` 的内部映射没读** —— 它决定 `6-13/140/145` 档
   （含 `12/13` 能量、`140 createCardPos`、`145 stratagemGeneralShowCase`）落在场上哪个 UI 点。
   没搜到 `BattleHud__GetUIAnchor.c` 的方法体；`dump.cs` 里只有签名。
2. **`CardScript` 的状态枚举 `+0x228` 的取值含义没查** —— 只知道
   `{4,9,0xc,10,8,1,0,0x10}` 走「卡自己的 transform」、其余走 `effectsAnchor`
   （`CardScript__GetEffectAnchor.c:9-25`）。**没定这 8 个值各是什么状态**（手牌/墓地/牌堆/…）⇒
   要精确复刻 state-aware 那一档，还得先把 `+0x228` 的枚举名认出来。
   搜过的词：`GetEffectAnchor` · `CardScript__` 前缀（`decomp_full`）；`+0x228` 在 `dump.cs` 的 `CardScript` 字段表。
3. **`CreateAnimFromAnimFxController.c` 的 `in_stack_00000030/38` 到底是不是 `actingCard`/`targetCard`** ——
   反编译对栈参不可靠（该函数 7 个参数只浮出 2 个寄存器 + 2 个栈槽）。**没定**。
   不影响主结论（option 来自 `animInfo+0x18/0x1c` 是确定的）。
4. **`BattleAction` 队列的执行器没定位**（谁 `StartCoroutine(ResolveAnim(battleAction))`）——
   `BattleManager__ResolveAnim.c`（创建方法）在 `decomp_full` 里**找不到调用者**
   （该目录只有 25,096/26,282 个方法；已按简报提示，未做 DLL 字节扫描兜底）。
   但 `ResolveAnim` 的状态机字段（`BattleAction battleAction // 0x28`，`dump.cs:28777`）与其
   `MoveNext` 读的 `BattleAction` 字段（0xA8/0xB0/0xB8/0xA0）**已能完全对上**，
   ⇒ 链路结论不受影响。
5. **`card_anim_map.json` 的 `_guid_summary`（402 条「只有 GUID、没名字」）** 是什么
   （变体？未被任何卡引用的 CardAnim？）—— **没与卡片列表 join 核**；本轮只当它不参与主问题。
6. **`AbilityTrigger` 枚举值 → 名的映射**（数据里的 `605`/`40`/`600`）—— 本轮没查（`dump.cs` 有 `AbilityTrigger`，
   未逐值展开）；它只影响「事件归类」，不影响本三问。

---

## 四、顺手发现的新问题（铁律 5·b → 请主对话立账）

1. 🔴 **`Battle/AnimAnchors.cs` 建在 `BattleManager.GetAnimTransform` 上，而 `cardAnimations[]`（技能）路径
   实际走的是 `AbilityLogic.GetPos`** —— 两者是同 36 档枚举、**不同覆盖 + 部分不同语义**（见 §一 问 3 的差异表）。
   ⇒ 至少这三处与原版不一致：① `6-17/60-155` 档在原版**有真锚点**（不是报错兜底）；
   ② `0/1/2` 档原版是 **state-aware**；③ `14` 档原版用 **行动方**那一侧（现在写的是本地玩家）。
   出处：`AbilityLogic__GetPos.c`（331 行，RVA 0x8174A0）vs `BattleManager__GetAnimTransform.c`（144 行，RVA 0x963B00）；
   GetAnimTransform 的调用者只有 `CreateAnimFromAnimFxController.c:11-17` 与 `d__469:44-49`。
2. 🔴 **`AnimInfo` 不是 10 个字段，是 ~22 个**；其中 **`screenShake`（0x60）真实存在** ⇒
   **答 T05 §六第 2 条**：「原版有没有逐 cardAnim 的震屏字段」→ **有**（`dump.cs:25246`）。
   另有 `easeCurve/orientToVelocity/applyMovementOffsets/offsetMovementAxis/waitAnimToApplyEffect/
   minionAnimationClipInfo/minionMovementInfo/unitsShake/tweenAnim`，且 `ResolveAnim` 真的消费其中 5 个。
3. 🔴 **T05 §2·6 那两句结论要就地订正**（铁律 5）：
   · 「原版自己就只做这 10 档」—— **只对 `GetAnimTransform` 成立**；
   · 「`12`/`13`（能量）/`15`（敌方空位）落进『其余』= 报错 + 退回督军锚点」—— **对 cardAnim 路径不成立**
     （cardAnim 走 GetPos，`12/13`→`BattleHud.GetUIAnchor`、`15/16/17`→`CreateTempAbilityTransform`）。
   实据：卡 `5` 的实测 sp/ep 里就有值 `4`（enemyHero）等落在 GetPos 真实现档上。
4. **`card_anim_map.json` 的键是 CardAnim 资源名（形态 `<base>_<variant>_<event>`），
   ⛔ 不是「`<卡名>_<下标>_<事件>`」** —— 那个数字是资源名自带的变体号（`AeldariRecall_self` 与
   `AeldariRecall_2_self` 是两张不同资源），**不是列表下标**。列表**顺序**信息我们这层没有
   （只在原版 `cardAnimations[]`，`cards_1195_2022时代.json` 里）。出处：`AeldariRecall_*` 键组 +
   `Faith_trigger_self` 等实测。
5. **`pointTowardsOption`（0x20）在 `GetAnimTransform` 的两个调用点都不用**（只解 0x18/0x1c），
   只在 `AbilityLogic.GetPos` 路径被解出并喂给 `rotateTowards`（`PlayAnim.c:215` → `AddCardAnim` 的 `rotateTowards`）。
