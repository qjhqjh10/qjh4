# T06c · B 组 9 条 + changeTo 3 条的触发点

> 只读普查（2026-10-16）。**12 条全部找到触发点** —— 其中 `T06` §四·3 那句「`backToHand`/`landOnGround`/`createCardInHand` 调用点没查到、原版就没定」
> 🔴 **被推翻**（三条都有调用点，见下）。出处行号 = `d:/2/tools/decomp_full/<文件>.c` 的行号。
> 字段偏移判据 = `d:/2/tools/il2cpp_out/dump.cs:25895-25947`（`BattleCardSounds` 9 字段 + 3 个 getter RVA）。
> ⚠️ **`BattleCardSounds__get_*.c` 这 8 个文件【文件名与内容不符】**（里面装的是 `Room__get_Name` / `AlternateArtCard__get_CardImage` /
> `RoomOptions__get_plugins` / `FriendInfo__get_Room` / `PhotonLagSimulationGui__get_Peer` 等）⇒ 本报告**不引它们**，
> 偏移一律取 `dump.cs` 的字段表 + 调用点现场。

## 一、结论表

| 字段名 | 时机（一句话，能照着接线） | 谁播的（类.方法） | 出处 文件:行号 | 置信度 | 我们这边的等价物 |
|---|---|---|---|---|---|
| `invokeCUE` | 卡从手牌飞到战场后**转成随从**那一刻（紧接 `BattleCardUI.PlayInvokeAnimation`）；**稀有度 ≠ Legendary 且无 ambush** 时用这条 | `CardScript.CardToMinionAnim` → `BattleCardSounds.GetInvokeCue(rarity)` → `SoundManager.Play2D` | `CardScript__CardToMinionAnim.c:13,20-30` · `BattleCardSounds__GetInvokeCue.c:1-8` | 实读 | 无。表里已有（`工具/import_battle_sfx.py:83` 的 `CARD_CUE_FIELDS`，`group=card`），`BattleSfx.cs` 还没常量 |
| `invokeCUELegendary` | 同上那一拍，但 `cardRarity == 4(Legendary)` **或** 该单位带 `DefinedTrait.ambush(1200)` ⇒ 走这条 | 同上（`GetInvokeCue(int)` 里 `param_2==4` 那一支） | `CardScript__CardToMinionAnim.c:20-28` · `CardScript__CardToMinionAnimTeleport.c:27-31` · `dump.cs:45841`(`ambush=1200`) | 实读 | 同上 |
| `minionDeathCUE` | **单位死亡那一拍**（`UnitDeath` 协程里，残骸体/溶解处理之后），在**单位自身位置 3D 播** | `CardScript.<UnitDeath>d__446` → `SoundManager.Play3D` | `CardScript._UnitDeath_d__446__MoveNext.c:359-369` | 实读 | 挂单位死亡事件（我们 `EvtKind.Death`） |
| `meleeAttackHitSmall` | 近战攻击动画的**命中回调**里；`SupportMethods.GetUnitSize(此单位) ∈ {0,1}` 走这条 | `CardScript.ResolveAttackAnimationEffects` ← `<AttackMeleeAnim>b__1` → `SoundManager.Play2D` | `CardScript__ResolveAttackAnimationEffects.c:30-37,64` · `…__DisplayClass357_0___AttackMeleeAnim_b__1.c:16` | 实读 | **攻击命中**那一拍（`EvtKind.Hit` 且紧跟 `EvtKind.Attack`）；档位按单位 size |
| `meleeAttackHitMedium` | 同上，`GetUnitSize == 2` | 同上 | `CardScript__ResolveAttackAnimationEffects.c:38-43,64` | 实读 | 同上 |
| `meleeAttackHitBig` | 同上，`GetUnitSize == 3` | 同上 | `CardScript__ResolveAttackAnimationEffects.c:44-50,64` | 实读 | 同上 |
| `backToHand` | 场上单位**变回手牌卡**那一拍：`BattleCardUI.PlayBackToHandAnimation` 之后、回手位 DOMove 起播时 | `CardScript.<TransformUnitInPlayToCard>d__256` → `SoundManager.Play2D` | `CardScript._TransformUnitInPlayToCard_d__256__MoveNext.c:52,73,76-79` | 实读 | 召回/回手那一拍（原版 `_ResolveRecallToHand` / `_ResolveRecallToDeck` / `_ResolvePlayCardFromHand` 都调它） |
| `landOnGround` | 单位**落到格位**那一拍（进场的落位；紧跟 DOScale 到场上尺寸之后），在单位位置 3D 播，**同一处还播相机震动 `shakeWhenLand`** | `CardScript.LandOnGround()` ← `<MinionPlayedIntoField>` / `<HeroLandIntoField>` → `SoundManager.Play3D` | `CardScript__LandOnGround.c:26-41` · `CardScript._MinionPlayedIntoField_d__314__MoveNext.c:37,193` · `CardScript._HeroLandIntoField_d__318__MoveNext.c:394` | 实读 | 我们挂在 `EvtKind.Deploy`（和 `moveCardIntoBattle`/`playMinionFromHand` 同一拍，但**这一条要 3D 落点**） |
| `createCardInHand` | 卡**出现在手牌**那一拍：`CreateCardInGame` 把卡设成 `inHandJustCreated(12)`、贴上手牌层、跑完 DOMove/DORotate/DOScale 之后播 | `CardScript.<CreateCardInGame>d__250` → `SoundManager.Play2D` | `CardScript._CreateCardInGame_d__250__MoveNext.c:44-50,78-81` | 实读 | 抽牌落手那一拍（原版 `_ResolveDrawCard` 也调 `CreateCardInGame`）——⚠️ 与 A 组 `drawCardFromDeck` **可能是同一拍响两条**，见 §三 |
| `changeToMeleeAttackCUE` | `ChangeAttackType(AttackTypes.Melee=1, playSound=true)`，**且攻击类型真的变了**（`+0x120 != 新值`） | `CardScript.ChangeAttackType` → `SoundManager.Play2D` | `CardScript__ChangeAttackType.c:14-22,53` | 实读 | 玩家点技能条 / AI 出招切换攻击类型那一拍 |
| `changeToRangeAttackCUE` | 同上，`Ranged=2` | 同上 | `CardScript__ChangeAttackType.c:24-27,53` | 实读 | 同上 |
| `changeToActiveAbilityCUE` | 同上，`Active=4`；🔴 **但本 build 该格是空 PPtr ⇒ 原版调到也无声**（铁律 11 例外①） | 同上 | `CardScript__ChangeAttackType.c:48-51,53` · prefab `MonoBehaviour_1744609728290659264.json`（`{"m_FileID":0,"m_PathID":0}`） | 实读 | **不接**（原版自己就是空的） |

**`ChangeAttackType` 的其余两档（同一条判据里）**：`Spell=3` → **`return`，不播**（`:31-33`）；`None=0` → 外层 `iVar5 != 0` 门不过，**不播**（但类型照样写进 `+0x120`）。

## 二、逐条证据

### 1-2 · invokeCUE / invokeCUELegendary
`CardScript__CardToMinionAnim.c:13` `BattleCardUI__PlayInvokeAnimation(*(longlong *)(param_1 + 0x290),0);` —— 先播「召唤」动画。
`:20-28`：
```
cVar3 = EntityScript__HasCurrentTrait(param_1,0x4b0,0);   // 0x4b0 = DefinedTrait.ambush(1200)
if (cVar3 == '\0') uVar4 = EntityScript__get_cardRarity(param_1,0); else uVar4 = 1;  // ← 见 §三 的订正
uVar5 = BattleCardSounds__GetInvokeCue(lVar1, uVar4, 0);   // lVar1 = *(param_1 + 0x1d0) = battleCardSounds
SoundManager__Play2D(local_18, lVar2, uVar5, 0, 0);        // 第 4 实参 0 = MixerType.FX
```
⚠️ **此处的 `uVar4 = 1` 不是我读错** —— 它把「有 ambush」映射成 **选 legendary 那一支**（`GetInvokeCue` 只认 `param_2 == 4`）。
`BattleCardSounds__GetInvokeCue.c:3-7` `if (param_2 == 4) return *(param_1 + 0x18); return *(param_1 + 0x10);` —— 即 `0x18 = invokeCUELegendary`、`0x10 = invokeCUE`。
`CardScript__CardToMinionAnimTeleport.c:27-31` 是**同一条**（这条路径先 `SetActive(false/true)` 藏原卡与「召唤后」那张，再播同一句）。
**触发者**：`MinionManager.MoveMinionToConversionPoint.c:98-99` 非瞬移路把 `DAT_18425e5e0` 当 `OnComplete` 回调挂到 `DOLocalMove` 上（`grep` 全仓只有这一处引用该常量 ⇒ 高度疑似 = `CardToMinionAnim`，**未反汇编证实，标推断**）；
`:106` 瞬移路**直接调** `CardScript__CardToMinionAnimTeleport`。两条路的上一级 = `MinionManager.PlayMinionFromHand` / `MoveMinionIntoSlot`。
⇒ **无论哪条路，invoke cue 都在「卡飞到位、转成随从」那一拍响一次。**

### 3 · minionDeathCUE
`CardScript._UnitDeath_d__446__MoveNext.c:359-369`（在残骸/溶解分支合流之后、`CardScript.StopAnimsInPlay` 之前）：
```
lVar7 = FUN_1805e39d0(0);                       // SoundManager 单例
uVar6 = *(undefined8 *)(*(longlong *)(lVar12 + 0x1d0) + 0x20);   // battleCardSounds.minionDeathCUE
puVar8 = UnityEngine_Transform__get_position(&local_48, lVar9, 0);
SoundManager__Play3D(&local_48, lVar7, uVar6, &local_58, …, 0);   // 在单位自身位置播
```
`_UnitDeath` 入口对状态有门（`:56` `if (… || *(int *)(lVar12 + 0x228) != 5)` 就 `LogWarning` 并返回）⇒ **只在 `inPlay(2)` 那一支的死亡协程里响**。
**触发者**（谁 `StartCoroutine(UnitDeath)`）：`BattleManager._ResolveMinionDeath_d__454__MoveNext` · `_ResolveBacklash_d__451` ·
`_ResolveMultiDamageAndDestroySelf_d__457` · `_DestroyUnitsAfterBattleEnd_d__392` · `CardScript.TriggerUnitDeathActions`。

### 4-6 · meleeAttackHitSmall / Medium / Big
`CardScript__ResolveAttackAnimationEffects.c:30-50` 一张三岔：
```
iVar2 = SupportMethods__GetUnitSize(param_1,0);
if (iVar2 == 0 || iVar2 == 1) → uVar6 = *(battleCardSounds + 0x28);  // small
else if (iVar2 == 2)        → uVar6 = *(battleCardSounds + 0x30);  // medium
else if (iVar2 == 3)        → uVar6 = *(battleCardSounds + 0x38);  // big
else                        → goto small 支                    // :45 兜底回 small
… :63-64  if (**(…(DAT_18429e748 + 0xb8)) != 0) SoundManager__Play2D(&local_38, inst, uVar6, 0, 0);
```
🔴 **它整个裹在相机震动那个块里**（`:51` `if (*(longlong *)(param_1 + 0x1c8) != 0)` = `meleeHitCameraShakePreset` 非空）⇒ 原版这一声是**跟着震动一起**出的。
而且 `small/medium/big` **三条指向同一个 pathID**（`-3640304923993188049`）⇒ 三档本来就同一条音，**档位只影响震动与粒子**（`0x1b8 attackHitSmallParticles` / `0x1c0 attackHitBigParticles`）。
**调用者**：`CardScript.__c__DisplayClass357_0___AttackMeleeAnim_b__1.c:16-17`
`CardScript__ResolveAttackAnimationEffects(lVar1, *(lVar1 + 0x290) + 0x1a0, &dir, 0)` —— 即**近战攻击动画的命中回调**（`<AttackMeleeAnim>` 那支协程在 `BattleManager._ResolveAttack_d__438__MoveNext` 起来）。
⇒ 接法：**只在「近战攻击命中」那一拍**播（我们的判别式 = `EvtKind.Hit` 紧跟 `EvtKind.Attack`，和 `BattleDriver.UnitDamageCueIndex` 跳过的那一条**是同一族**）。

### 7 · backToHand
`CardScript._TransformUnitInPlayToCard_d__256__MoveNext.c:52,73,76-79`：
```
BattleCardUI__PlayBackToHandAnimation(*(longlong *)(lVar3 + 0x290),0);   // :52 先播动画
uVar5 = DG_Tweening_ShortcutExtensions__DOMove(uVar5,&local_38,DAT_1834b317c,0,0);   // :73 回手位位移起播
if ((*(longlong *)(lVar3 + 0x1d0) != 0) && (lVar6 != 0))
    SoundManager__Play2D(local_28, lVar6, *(undefined8 *)(*(longlong *)(lVar3 + 0x1d0) + 0x40), 0, 0);  // 0x40 = backToHand
… UnityEngine_WaitForSeconds__ctor(uVar5, CardScript__GetHandToBoardAnimEventTime(lVar3,0), 0);
```
⇒ **场上单位变回手牌卡**（`TransformUnitInPlayToCard`）那一拍。**触发者**：`BattleManager._ResolveRecallToHand_d__542` · `_ResolveRecallToDeck_d__543` · `_ResolvePlayCardFromHand_d__447`。

### 8 · landOnGround
`CardScript__LandOnGround.c`（一个直调方法，不是协程）：`:26-27` 先 `OverwriteCameraShakePreset__Play(this.shakeWhenLand /*0x208*/)`，
`:34` `*(battleCardSounds + 0x48)`，`:41` `SoundManager__Play3D(…, &local_48 /*= transform.position*/, …)`；
之后 `:43-49` 按 `HasCurrentTrait(0x4b0 /*ambush*/)` 或 `cardRarity == 4` 挑 legendary 落地位移/粒子。
**调用者**：`CardScript._MinionPlayedIntoField_d__314__MoveNext.c:37`（首发那支）与 `:193`（另一支）、`CardScript._HeroLandIntoField_d__318__MoveNext.c:394`（紧跟 `DOScale` 到 `BattleManager.GetUnitSizeInPlay` 尺寸之后）。
⇒ **单位落到格位那一拍**（随从与督军/英雄两条路都有）。

### 9 · createCardInHand
`CardScript._CreateCardInGame_d__250__MoveNext.c:44-50` 先 `*(lVar2+0x228) = 0xc`（`CardStateOptions.inHandJustCreated`）+ `ShowCardInHandLayer` + `SetObjectVisibility`；
`:55-73` 跑 `DOMove` / `DORotate` / `DOScale` 到「手牌位/手牌角/手牌缩放」（都取自状态机 `param_1 + 0x28..0x4c`）；
`:74-81` 再 `SoundManager__Play2D(…, *(battleCardSounds + 0x50) /*createCardInHand*/, 0, 0)`，**紧接 `WaitForSeconds(param_1 + 0x34)`**。
**触发者**：`BattleManager._ResolveDrawCard_d__432` · `_ResolveCreateHandCard_d__512` · `_ResolveCreateDeckCard_d__513` · `_ResolveCopyUnitInCemetery_d__541` · `_ResolveRecallToHand_d__542` · `_ResolveRecallToDeck_d__543`（`ResolveDrawCard:150` 亲读有 `CardScript__CreateCardInGame`）。

### 10-12 · changeTo*CUE
`CardScript__ChangeAttackType.c`：
```
:14  if (*(int *)(param_1 + 0x120) != iVar5) {          // 只在新旧不同时进
:15      *(int *)(param_1 + 0x120) = iVar5;
:17      BasicCardUI__AttackTypeChanged(*(param_1 + 0x290), param_2, 0);
:18      if ((param_3 != '\0') && (iVar5 != 0)) {       // param_3 = playSound
:20        if (iVar5 == 1) uVar2 = *(param_1 + 0x1e0);  // changeToMeleeAttackCUE
:25        else if (iVar5 == 2) uVar2 = *(param_1 + 0x1e8);  // changeToRangeAttackCUE
:31        else { if (iVar5 == 3) return;  /* Spell 不播 */
:48          if (iVar5 != 4) throw ArgumentOutOfRangeException;
:51          uVar2 = *(param_1 + 0x1f0); }              // changeToActiveAbilityCUE
:53        SoundManager__Play2D(local_18, lVar1, uVar2, 0, 0); } }
```
`AttackTypes` 枚举（`dump.cs:24798-24807`）：`None=0 · Melee=1 · Ranged=2 · Spell=3 · Active=4`。
**全部 5 个调用点都传 `playSound = 1`**：

| 调用点（`decomp_full/`） | 行 | 场景 |
|---|---|---|
| `UnitOnBoardAttackTypeSelector__AttackButtonClick.c` | 104 | **玩家**点场上单位的攻击类型按钮 |
| `BattleManager__ParseEnemyAction.c` | 115 | 解析敌方动作 |
| `AI__ExecuteAction.c` | 222 / 233 | AI 选近战 / 远程 |
| `AiScripted__ExecuteAction.c` | 1251 | 脚本化 AI |
| `BattleManager._ResolveForceAttack_d__504__MoveNext.c` | 61 | 强制攻击（教程那一路） |

⚠️ `AI__ExecuteAction.c:222` 的第三实参反编译成 `0x180000001`（`:233` 是 `0x180000001, 0`）—— 低字节 = 1，
和另 4 处的 `1` 同义（`playSound = true`）；⛔ 别把它读成一个别的常量。

## 三、没查清的 / 判不准的

1. **`CardToMinionAnim` 是那支 `OnComplete` 回调吗**（§二·1）—— **推断**，不是实读。
   证据只有「`MinionManager__MoveMinionToConversionPoint.c:98` 把 `DAT_18425e5e0` 注册成 `DOLocalMove` 的 `OnComplete`，
   而全仓只有这一处引用该常量」。我按 `资料/战斗规则与数值_出处.md` §三 的办法从 `GameAssembly.dll` 读 `.data:0x18425e5e0`
   得到 `0x600026e5`（既不是 VA 也不是 RVA ⇒ **不是朴素的方法指针**），**没能反查成方法名**。
   ⇒ 若主对话需要确证，得读 `il2cpp.h`/`script.json` 的元数据表或现反汇编 `MinionManager__MoveMinionToConversionPoint`。
   **但接线不受影响**：瞬移路（`:106`）是**直调**，两条路播的是**同一条** invoke cue。
2. **`createCardInHand` 与 A 组 `drawCardFromDeck` 是不是同一拍响两条** —— **没逐条确认**。
   已知：`_ResolveDrawCard:150` 调 `CreateCardInGame`（→`createCardInHand`）；而 `CardScript__DrawCard.c:48-50` 从
   `BattleManager.soundCollection`（`+0x80`）播另一条（T06 §四 记为 `drawCardFromDeck`）。
   **没查清**：`CardScript.DrawCard` 由谁调、是否也落在 `_ResolveDrawCard` 这条链上。⇒ 接线时**别把两条当互斥**。
3. **`0x4b0` 的语义**：`dump.cs:45841` `public const DefinedTrait ambush = 1200;`（1200 = 0x4b0）—— 这一条是**按数值对上**的，
   `HasCurrentTrait(this, 0x4b0, 0)` 的第一个形参到底是 trait id 还是 hash **没查清**（`CardScript._MinionPlayedIntoField…:196` 有 `HasCurrentTrait(lVar4, 5)` 这种裸小整数写法，说明它不止一种重载）。
   ⚠️ **同族的两处 `0x4b0` 用法**：`CardToMinionAnim`（选 legendary invoke 音）与 `LandOnGround`（选 legendary 落地位移）都拿它当「传奇档」判据。
4. **`decomp_full` 不是全量**（25096 个 `.c` vs dump.cs 里 26282 个方法）。我搜过的**词**：
   `get_MinionDeathCue` · `get_MeleeAttackHitSmall/Medium/Big` · `get_BackToHand` · `get_LandOnGround` · `get_CreateCardInHand` ·
   `GetInvokeCue` · `get_invokeCUE` · `get_invokeCUELegendary` · `BattleCardSounds` · 8 条字段名（camelCase 全名）。
   **结果**：8 个 `get_*` 在**全仓 0 命中**（`GetInvokeCue` 2 处）。⇒ **不是因为「不可达」，是因为 getter 被内联成裸字段读**
   （`*(battleCardSounds + 0xNN)`）。我用两步扫描兜底拿全：
   ① `+ 0x1d0)` 后 2000 字符内出现 `+ 0x20/0x28/0x30/0x38/0x40/0x48/0x50)`（66 命中，其中 CardScript 系 6 处，全在本报告里）；
   ② `lVarN = *(… + 0x1d0);` 之后 `lVarN + <音效偏移>`（只 1 命中，非 CardScript）。
   `+ 0x1e0/0x1e8/0x1f0` 用同法（44/55/… 个文件命中，CardScript 系只有 `ChangeAttackType.c`；`ActivateMinion.c` / `SetStateAsMulligan.c` / `OnTouchUpAsButton.c` 那几处经核**是别的类的同偏移字段**）。
   **9 + 3 条全部落位，无缺口。**
5. **⚠️ 本报告推翻的两句**（铁律 5，请主对话就地改 `T06`）：
   - `T06_战斗音响接线.md` §四·3 原话「`backToHand` / `landOnGround` / `createCardInHand` **全仓只找到 getter、调用点没查到** ⇒ 那三条的时机原版就没定」
     ⇒ **错**。三条的调用点分别是 `_TransformUnitInPlayToCard_d__256`（`:76-79`）· `CardScript.LandOnGround`（`:34,41`）· `_CreateCardInGame_d__250`（`:78-81`）。
     **错因**：只按 `get_<Prop>` 名字 grep —— **getter 被内联**（见上条 4）。
   - `T06` §二 那句「**`BattleCardSounds` 逐卡序列化在卡 prefab 上**」也**不成立**：全 `assets_full` 只有 **1 份**（`bundle_battleprefabs_vfxandmisc_assets_all/…/MonoBehaviour_1744609728290659264.json`），
     且 `param_1 + 0x1d0 == 0` 在每个调用点都是**显式守卫**（提前 bail）⇒ **没挂这个组件的卡就是静音**。
     ⇒ 我们照 **「一张全局默认表」**接线是对的，不需要逐卡。

## 四、顺手发现（与 T06 无关，交主对话分流）

1. 🔴 **`changeToActiveAbilityCUE` = 空 PPtr**（`m_FileID:0, m_PathID:0`，亲读那份 MonoBehaviour）—— **原版调用点存在**（`ChangeAttackType.c:51`），
   但资产是空的 ⇒ 归**铁律 11 例外①**（原版本来就没声音），**不是我们没做**。
2. 🔴 同族的 `0x48 landOnGround` / `0x50 createCardInHand` / `0x20 minionDeathCUE` 等**都有具体 pathID**（都在 `m_FileID:9` 那个 CAB）⇒ 与 A 组的 12 条不同，**B 组这一族是能解出 cue 的**。
3. **`BattleCardSounds` 的 8 个 getter 在 `decomp_full` 里天然是死代码**（全仓 0 名字命中，只有 `GetInvokeCue` 被调）——
   凡以后遇到「原版这个属性有没有人用」，**别 grep getter 名**，要按**字段偏移扫内联读**。
   这是 `资料/已知的坑.md` 里「0 调用点 = 被内联」那一条的**第二个独立实例**（第一个是 `BattleManager.Resolve*` 那 63 个）。
4. ⚠️ **`decomp_full` 的文件名/内容不符又多三例**（简报里只给了 `VoiceLinesController__ToggleMute.c` 一例）：
   `BattleCardSounds__get_{BackToHand,CreateCardInHand,LandOnGround,MeleeAttackHitBig/Medium/Small,MinionDeathCue}.c`（8 个）
   ＋ `CardScript.__c__DisplayClass357_0___AttackMeleeAnim_b__2.c`（装的是 `MinionAnimClipInfo…__get_WaitForCompletion`）。
   ⇒ **引方法体前先核首行函数名**这条纪律是必须的，命中率不低。
5. **`RawCardScript.GetAvailableSounds`**（`RawCardScript__GetAvailableSounds.c`）是**另一族**声音：把 `RawCardScript` 上
   `0x188/0x190/0x198/0x1a0/0x1a8/0x1b0/0x1b8/0x1c0/0x1c8/0x1d0/0x1d8/0x1e0/0xb8/0xc0/0xc8` 共 15 个音收成
   `List<AudioCue>` 再 `Where`+`ToList`（疑似卡详情/收藏试听）。**不在 T06 范围**，但它是 `CardScript` 之外的**第三个战斗音效族**。
6. `BattleCardSounds` 9 条字段全在一份 **`m_Name` 为空** 的 MonoBehaviour 上，位于 `battleprefabs_vfxandmisc` 束 ⇒
   我们若想「逐卡不同音」，原版**没有**这个能力（除非改装备份 prefab）。
