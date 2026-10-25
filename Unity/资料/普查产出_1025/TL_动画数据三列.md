# TL —— 三条「代码已就绪、数据缺列」动画数据管线（生成器侧）

> 执行代理 `TL` · 2026-10-25。范围 = **只补数据生成侧**；`Assets/CardPresentation/Resources/*.json` 只经生成器写。
> ⛔ 未跑 Unity · 未动 git · 未改任何 `.cs` / 正本 / 任务总表。

---

## 0. 一句话结论

| 任务 | 结论 | 幂等 |
|---|---|---|
| **TL-1** `minionMovementInfo` 12 列 | ✅ **补齐**（写在 `工具/gen_cardanim_clips.py`，**不是** `gen_card_anim_order.py`）＋顺手补上**从来没写过的 `mva`(animTime)** | 连跑两次 md5 相同 |
| **TL-1 附加**：`_d__358` 曲线位移算式 | ✅ **方法体读全了**（388 行），算式见 §1·4；⛔ **播放器没建**（`BattleDriver.cs` 在黑名单里） | — |
| **TL-2** `ability_anim_durations.json` 的 `alt` | ✅ 运行期件补上（`SW21 = 0.2`） | 连跑两次 md5 相同 |
| **TL-3** `screenShake` 的 preset 名 | ✅ 补 `shk/shkd/shka/shkf/shkoa/shkof` 6 列；PathID→名字**改用 `m_Script`+`m_Name` 判据**（57/57 解出，10 个 preset 全在 `shake_presets.json` 里） | 与 TL-1 同一次生成，连跑两次相同 |
| **追加 ⑥** `otherAbilityAnims` 断源 | ✅ **查清 + 修生成器 + 重出全量件**（§4）：分流是 `gen_card_vfx_by_card.py` 的**有意**改动（T21/R4/C5），错的是本生成器只读 `ability` 一个桶；现改成**跨所有事件桶按 `soField` 前缀收** | `--check` 两份全过 |
| **追加 ⑨** `Shake *.json` 21 vs 17 | ✅ **不是漏收**：多的 4 份是 `Shake Target*` = `UnitTweenSO`（`m_Script …098487`），`gen_shake_presets.py` 早已按设计跳过（§4·4） | — |

**🔴 追加任务的结果一句话**：`数据/游戏数据/ability_anim_durations.json` 已重出（364 → **873** 卡 / `otherAbilityAnims` 453 → **1306** 条），
**93 张卡的 `active` 值与 `activeAnims` 逐条不变**、共用行的 `seconds` diff = 0、**运行期件逐字节未变**（`81bf9df7…`）。
逐类差异见 **§4·3**。

---

## 1. TL-1 `minionMovementInfo` 的 12 列

### 1·1 落到哪个生成器（判据）

- 消费端读的是 **`Resources/cardanim_clips.json`**：`Battle/CardAnimClips.cs:225` `Resources.Load<TextAsset>("cardanim_clips")`
  → `:250-267` 把 `RtRow` 逐列搬进 `CardAnimPlacement`（`MoveBody = r.mvt != 0` … `MoveOffX = r.mox`）。
- `RtRow` 的列名在 `CardAnimClips.cs:175-203`；`CardAnimPlacement` 12 个字段在 `:112-126`。
- ⇒ 数据生成器 = **`工具/gen_cardanim_clips.py`**。
- ⚠️ **`gen_card_anim_order.py` 不是这一件**：它写的是**另一张表** `Resources/card_anim_order.json`（逐拍 `CardAnimBeat`，
  `Battle/CardAnimOrder.cs`），那张表**没有也不能有** minion 列（`PlayMinionMovement` 收的是 `CardAnimPlacement`）。
- 简报里那句「`card_anim_order.json` 只有 2 行、`shouldActivateMinionMovement=1`、`animTime 0.6`」是**源数据口径**：
  `数据/游戏数据/card_anim_order.json` 的 `cardAnims[].animInfo.minionMovementInfo` 共 **536 条**，其中
  **只有 2 条** `shouldActivateMinionMovement=1`（`Scarab_Explosion` / `SporeMine_Explosion`），`animTime` 都 = `0.6`（现核）。

### 1·2 改动（`工具/gen_cardanim_clips.py`）

1. `_minion_move_block()` 增加 **12 个运行期投影列**（= `RtRow` 的列名）：
   `mvt`(shouldActivateMinionMovement) `mvd`(delay) `mva`(**animTime**) `mvvis`(visibilityStateAfterFinish)
   `mvxz`(useXZCurve) `mvy`(useYCurve) `mvcx/mvcy/mvcz`(三条位移曲线，扁平 `[t,v,…]`，非空才写)
   `mox/moy/moz`(xOffset/yOffset/zOffset)。
2. 🔴 **`mva` 是本次新读的字段** —— 旧版 `_minion_move_block` **根本没读 `animTime`**。没有它，
   消费端那条「时长 = `delay + animTime`」（`BattleDriver.cs:21733-21734` / `:22266-22267`）会算成 `delay + 0`。
3. `animTime` 也进了「非默认」判据（现读对行数**无影响**：带 `animTime` 的 9 行本来就因别的字段非默认而在表里）。
4. **`mm*` 那份保留**（全量记录：`mmRx*/mmRy*/mmRz*` 三轴旋转 `RtRow` 没有字段收）⇒ 两族并存 = 「原样记录」+「运行期契约」，
   文件头与 `what` 字段已写明，⛔ 不是两份真值。

### 1·3 验收（逐值对源）

- 表里 **10 行**带 `mm*`/`mvt`（= 全量记录里非默认的那些 CardAnim）；其中 **`mvt==1` 2 行**：
  `Scarab_Explosion` / `SporeMine_Explosion` —— `mvd=0` `mva=0.6` `mvvis=0` `mvxz=1` `mvy=1`
  `mox=-0.1` `moy=0.2` `moz=-0.1`，三条曲线与源 dump 逐点相同。
- **逐值对账脚本**：对 10 个 minion 行 + 57 个 shake 行，回读
  `d:/2/新解包资源/assets_full/<sourceMono>` 的 `animInfo` 重算每一列 → **mismatch = 0**。
- 抽样源（判据）：`bundle_*cardanims*_assets_all/MonoBehaviour/<CardAnim>.json` 的 `animInfo.minionMovementInfo`；
  字段偏移 = `d:/2/tools/il2cpp_out/dump.cs:25376-25399`（`delay` 0x10 · `shouldActivate` 0x14 · `vis` 0x15 ·
  曲线 X/Y/Z 0x18/0x20/0x28 · `useXZCurve` 0x30 · `useYCurve` 0x31 · `yOffset` 0x34 · `xOffset` 0x38 · `zOffset` 0x3C ·
  `rotate*Axis` 0x40/0x48/0x50 · **`animTime` 0x58**）。`animInfo.minionMovementInfo` 在 `dump.cs:25241`（0x70）。

### 1·4 附加：`CardScript.MoveAndDisappearAnim`（`_d__358`）**算式读全了**

判据（第一权威，388 行全读）：`d:/2/tools/decomp_full/CardScript._MoveAndDisappearAnim_d__358__MoveNext.c`
＋ `CardScript__ApplyMovementDisappearAnimRotations.c`；调用点 `BattleManager._ResolveAnim_d__466__MoveNext.c:214-231`。

**入参**：`MoveAndDisappearAnim(this = 要动的卡, boardPos, targetCard, minionMovementInfo)`
（`boardPos = BattleManager.GetBoardPosOfCard(targetCard)`）。

1. `if (mm.delay > 0) yield return new WaitForSeconds(mm.delay)` —— `:71-79`。
2. `start = this.transform.TransformPoint(this.originalLocalPosInPlay)`
   （`CardScript.originalLocalPosInPlay` = `+0x2C4`，`dump.cs:35458`）—— `:81-87`。
3. `n = normalize(boardPos − start)`（`FUN_180534610` = `Vector3.Normalize`，sret）—— `:101-112`。
4. `margin = this.isPlayer ? this.playerAttackMargin : this.enemyAttackMargin`
   （`isPlayer` = `EntityScript+0x40`；`playerAttackMargin` = `CardScript+0x1B0`、`enemyAttackMargin` = `+0x1B4`）—— `:95-100`。
5. **`end = boardPos − n × margin`** —— `:113-117`。
6. `end += v × (targetCard.CardUI.effectsAnchor.position.y − targetCard.transform.position.y) × this.attackPositionYOffset`
   —— `:126-137`。其中 `CardUI` = `CardScript+0x290`、`effectsAnchor` = `BattleCardUI+0x1A0`、`attackPositionYOffset` = `CardScript+0x1A4`。
   ⚠️ `v` 取自 `Vector3` 静态常量块的 `+0x18` 那一格 —— **具体是哪个基向量没解出**（见 §5「没查清」）。
7. `end.x += mm.xOffset; end.z += mm.zOffset`（**yOffset 不在这里加**）—— `:140-142`；随后 `BattleCardUI.StopSummonSicknessAnim` `:153-154`。
8. **补间**（时长全 = `mm.animTime`）—— `:156-225`：
   · `useXZCurve == false` ⇒ 一条 `DOMove(end)`（**不 SetEase** ⇒ DOTween 默认缓动）；
   · `useXZCurve == true` ⇒ `DOMoveX(end.x).SetEase(mm.curveX)` + `DOMoveZ(end.z).SetEase(mm.curveZ)`；
   · `useYCurve == true` ⇒ 另加 `DOMoveY(mm.yOffset + end.y).SetEase(mm.curveY)`。
9. **每帧**：`elapsed += Time.deltaTime`，`WaitUntil(elapsed >= mm.animTime)` 收尾 —— `:226, 269-283, 369-378`。
10. **每帧旋转** —— `:284-378` + `ApplyMovementDisappearAnimRotations`：
    `qAxis = axis.enable ? Quaternion.AngleAxis(axis.maxValue, axisVec) : identity`
    `q = axis.enable ? Quaternion.LerpUnclamped(cur, qAxis, axis.curve.Evaluate(elapsed/animTime)) : cur`
    三轴依次（X 用 `+0x40`、Y 用 `+0x48`、Z 用 `+0x50` 的 `AnimationDefinition{enable@0x10, curve@0x18, maxValue@0x20}`），
    再与起手时记下的 `startRotation`（`:229-236`）**四元数连乘**后 `transform.rotation = 结果`。
    ⚠️ **三轴连乘的顺序没逐句定序**（IL2CPP 把 `Quaternion *` 内联成 4×4 组乘法），见 §5。
11. **收尾**（state 3）—— `:247-267`：`transform.position = end`；
    `visibilityStateAfterFinish != 0` ⇒ **什么都不做**；`== 0`（现读两行都是 0）⇒ `CardUI.ToggleBody3D(false, true)`
    （= 卡体隐藏 + `effectsAnchor` 显示；`BattleCardUI__ToggleBody3D.c` 现读）。
    ⚠️ 我们今天的消费端写的是 `SetBody3DVisible(false)`（`BattleDriver.cs:21689-21694`）—— **卡体那半一致**，
    `effectsAnchor` 那半**没建**（属播放器活，不在本代理白名单内）。

**⇒ 播放器没建**（`BattleDriver.cs` 是黑名单）。今天的行为不变：只按 `delay + animTime` 占时长 + 收尾隐藏 + 出声一次
（`BattleDriver.cs:21672-21696` 的 `LogWarning` 明写，不静默）。

---

## 2. TL-2 `ability_anim_durations.json` 的 `alt`

### 2·1 改动（`工具/gen_ability_anim_durations.py`）

- `rt_items` 生成处（原一行推导式 → 改成显式循环）：对每张有真值的卡，若 `cards[*].alternative.seconds` 非空/非 0，
  写 `"alt": round(seconds, 4)`（**只在非 0 时写** ⇒ 缺键 = 0 = 「没有替代支」）。
- 运行期 `_headline` 补一句「另有 N 张卡的替代支时长在 `alt` 列（SW21）」；`main()` 末尾打印该列。
- ⛔ **没有**把 `s`（普通支）换成替代值 —— 两支按棋盘状态二选一，两列并存。
- 字段名与消费端逐字对上：`Core/AbilityAnimDurations.cs:97` `public float alt;`、`:210-218` `TryGetAlternative`。

### 2·2 验收

- 运行期件现读：`{"alt": 0.2, "id": "SW21", "name": "Wulfen Pack Leader", "s": 0.4, "n": 2}` ⇒ 普通 0.4 / 替代 0.2（= 全量件 `conditional[0].alternative`）。
- 判据链：`RawCardScript__GetActiveAbility.c`（`hasAlternativeAbility && !MeetsCriteria(normalAbilityBoardCriteria)` ⇒ 替代支）；
  替代支 GUID → CardAnim 的解析沿用 W64/B17 那段（`alternativeAbility.activeAbilityLogic[*].cardAnimations[*]`）。
- 只有 **1 张**卡带 `alt`（现读），与全量件 `conditional` 的条数一致。

### 2·3 `--check`（追加任务后已全绿）

```
✅ 一致: d:/4/Unity/数据/游戏数据/ability_anim_durations.json
✅ 一致: d:/4/Unity/MyGame/Assets/CardPresentation/Resources/ability_anim_durations.json
=== --check 全过 ✅
```
- 运行期件：**与改动前逐字节相同**（`md5 81bf9df7…`，8,076 B）—— `alt` 那一列是本轮唯一的内容变化，
  且它不改变任何现有消费路径（`TryGetAlternative` 之前恒 false）。
- 全量件：**已按追加任务重出**（见 §4·1）；两份都**连跑两次 md5 相同**。

---

## 3. TL-3 `screenShake` 的 preset 名（PathID → 名字）

### 3·1 改动（`工具/gen_cardanim_clips.py`）

1. `_preset_id_index()` **换判据**（⛔ 不再只信文件名）：
   - 读每份 `*_<pathID>.json` 的 **`m_Script.m_PathID`**，必须 == `SHAKE_SCRIPT_PID = 3633289390197731284`；
     **这个数怎么来的**：`assets_full/bundle_tweenandshakes_assets_all/MonoBehaviour/Shake *.json` **21 份全是它**
     （同一类的脚本引用）⇒ 用它把「屏震 preset」从那 148 份混杂 SO 里挑出来（目录里还有 `AeldariRecallTween` 之类）。
   - 再拿内容里的 **`m_Name`** 当名字；与文件名前缀不一致时**以 `m_Name` 为准并打印**（现读 0 条不一致）。
   - 结果是 **17 份 / 17 个 pathID**，与 `Resources/WarpforgeVFX/shake_presets.json` 的 **17 个 preset 逐名对上**。
2. `_shake_block()` 改名参（加 `ca`）并补 **6 个运行期投影列**（= `RtRow` 的列名）：
   `shk`(preset 名) `shkd`(delay) `shka`(amplitude) `shkf`(frequency)
   `shkoa`(overwriteAmplitude) `shkof`(overwriteFrequency)。
   `ss*` 那份**保留**（`sustain` / `direction` / `attack` / `decay` 四组运行期今天没有字段）。
3. 🔴 **对不上的那条：出声 + 留空**（`shk` 不写 ⇒ 消费端 `ShakePreset` 空 ⇒ 这一拍不震），
   `PROBLEMS` 里带 CardAnim 名 + pathID + 索引目录 + 脚本 pid。⛔ **没给任何默认 preset**。
   （另有第二条出声：名字解出来了但**不在 `shake_presets.json`** 的 17 个里 ⇒ 运行期没有它的数值。）

### 3·2 验收

- 表里 **57 行**带 `shk`，**57 行全部解出名字**；distinct = **10** 个：
  `Shake Earthquake / Shake Hit Medium / Shake Hit Medium-Big / Shake Hit Small / Shake Land on ground /
   Shake Light 2s / Shake Medium 2s / Shake Resurrection Orb / Shake Small / Shake Very Light 4s`
  —— 与简报「36 条 / 10 个 preset」的个数口径一致（条数差 = 运行期表覆盖面比全量件大，见 §1·1）。
- **逐名 ∈ `shake_presets.json` 的 presets**（脚本复核：`not in presets: []`）。
- 覆盖位：`shkoa=1` 只有 `RitesOfPossession_self`（`shka=6.0`）；`shkof=1` 有 `BlackCrusade_battleground` / `BlackCrusade_self`（`shkf=5.0`）——
  与 `CardAnimPlacement.ShakeAmplitude/ShakeFrequency` 的注释口径（34 条 0 / 1 条 4.0 / 1 条 6.0）自洽。
- 消费链：`BattleDriver.PlayBeatShake`（`BattleDriver.cs:21651-21665`）→ `WFModuleScreenShake.Resolve/OnShake`；
  `RtRow.shk → CardAnimPlacement.ShakePreset`（`CardAnimClips.cs:265`）⇒ 今天起**不再是 `string.IsNullOrEmpty` 早退**。

---

## 4. 追加任务：`otherAbilityAnims` 断源 → **已查清、已修、已重出**

### 4·1 分流是谁做的、分到哪几张表（现核）

- **谁做的**：`工具/gen_card_vfx_by_card.py` —— 两轮有意改动，都在文件头留了判据：
  · **2026-10-18 `T21`**：「`cardAbilities[].cardAnimations[]` 一律归 `ability`」是**错的**（那会让
    `ThisCardPlayed` 那批在「发动技能」那一拍播）；改成**按原版 `CardAbility.trigger` 归口**（`TRIGGER_EVENT`）。
  · **2026-10-24 `W2` · `R4`**：再加两档 —— `ThisUnitAbilityUsed(400)` → `ability`（`ABILITY_BEAT_TRIGGERS`）；
    `VfxMap.IsRoutedTriggerName` 放行的名字 → `trigger`；其余 → `trigger_unmapped`（留档不播）。
  · 同轮 `J1`·`C5`·`D14` 又把关键词天赋 `EffectTriggerType.OnPlay(2)` 单列成 `trait_onplay`。
- **分到哪几张表**（都在同一个 `byCard[<卡>]` 下，按事件名分列表）：
  `play` · `trigger` · `death` · `hit` · `attack_melee` · `attack_ranged` · `deploy` ·
  `trait_onplay` · `trigger_unmapped` · 以及 `ability`（**现在只剩 `activeAbility` + `ThisUnitAbilityUsed`**）。
  另有 `_assetRefs`（立绘/语音，不是 anim 行）。
- **原版语义判据（全量反编译，逐句读）**：
  · `cardAbilities[i]` 是**一个个独立的 `CardAbility`**，各有自己的 `trigger`：
    `RawCardScript__TriggerAbility.c:24` 读 `ability+0x10`（`AbilityTrigger` 枚举）；
    `:62-110` 拿**这个 ability 自己的** `abilityLogic[*]` 逐组 `AbilityLogic.PlayAbility`。
  · `AbilityLogic__PlayAnim.c:32-60` 遍历 `abilityLogic+0x18`（= `cardAnimations`）逐条播。
  · `activeAbility` 是**另一个 SO 字段**（`+0x260`；替代支 `+0x2b8`，`RawCardScript__GetActiveAbility.c:9-18`
    按 `hasAlternativeAbility && MeetsCriteria(normalAbilityBoardCriteria)` 二选一）—— 只有它对应
    「发动技能」那一拍（`EvtKind.Ability`）。
  ⇒ **`cardAbilities[]` 的 anim 行归「它自己那个 trigger 的时机」** ⇒ `card_vfx_by_card.json` 现在的分桶**是对的**。
  ⇒ 真正错的是**本生成器只读 `ability` 一个桶**（2026-10-25 `TL` 就地修）。

### 4·2 修法（`工具/gen_ability_anim_durations.py`）

1. 新常量 `ABILITY_ROW_BUCKETS`（固定顺序：`ability` 打头，其余按 `play/trigger/trait_onplay/trigger_unmapped/
   attack_melee/attack_ranged/hit/death/deploy`）—— 顺序固定 ⇒ 产物确定；`ability` 打头只为让
   `activeAnims` 顺序与旧产物逐条一致（少出无意义 diff）。
2. 取源改成**跨这些桶收**，再按 **`soField` 前缀**分类（⛔ 不按桶名判，桶名是播放路由、会随引擎发射口变）：
   · `cardSO.activeAbility…` → `act`（求和，= 运行时那份 `s`）
   · `cardSO.cardAbilities[N].abilityLogic[M].cardAnimations[K]` → `other`（备查，**不计入时长**）
   · `src == "cardtrait-so"` → `notAbilityRows`（关键词演出行，⛔ 不算时长、也不算缺口）
   · 其它（`customDeathParticles` / `remnantBodyRepair` / `customRangedAttackParticles` / `_assetRefs`）→ **跳过**
     （它们不是 anim 行；旧版因为只看 `ability` 桶从没碰到过，扩桶后必须显式排除，否则会被误记成缺口）。
3. `_headline` ③④ 与 `_conventions.scope` 同步改成「跨所有事件桶按 `soField` 前缀收」。
4. **覆盖审计（不静默丢行）**：`card_vfx_by_card.json` 里 `conf == authoritative` 且带 `cardanim` 的行共 **2,617** 条，
   本表吃掉 **2,099**（`act` 160 + `other` 1306 + `notAbilityRows` 633）+ `unresolved` 0，**剩下 518 条全部是
   `soField = customRangedAttackParticles` 那一类卡 SO VFX 字段**（`Atk_Artillery_manticore` / `Psychomancer_Stun` …，
   走 `attack_ranged` 那条播放路，**不是 ability 链**）⇒ 按设计跳过，⛔ 不塞进 `notAbilityRows`（那会把「关键词演出」
   和「远程攻击粒子」混成一锅）。**518 条不是丢失 = 逐条有归处（`byCard[*].attack_ranged`）**。

### 4·3 重出前后差异（逐类说明：**变的是断源修好，不是新噪声**）

| 量 | 改前（HEAD） | 改后 | 变化说明 |
|---|---|---|---|
| `cards[]` | 364 | **873** | **+509 张**：以前只有「`ability` 桶里有行」的卡才进表；现在「有 `cardAbilities` 行」的卡也进（这本来就是旧文件的形态 —— 它 364 里 271 张 `active=None`）。**旧卡 0 张丢失**（`old-only = 0`） |
| `activeAnims` | 160 | **160** | **逐卡逐条不变**（值 + 列表顺序全同，diff = 0） |
| `otherAbilityAnims` | 453 | **1306** | 旧 452 条唯一行**全部仍在**（`old-only = 0`）；**共用行的 `seconds` 逐条相同**（diff = 0）。+853 条来自两类：① 509 张新进表的卡的 `cardAbilities` 行；② 75 张老卡多出 `cardAnimations[1..3]` 与 `cardAbilities[1..]` 组（旧表只收了 `cardAnimations[0]`） |
| `notAbilityRows` | 142 | **633** | +491 = `trigger` 桶里的关键词演出行（`src == cardtrait-so`，`EffectTriggerType.Start/Trigger/TriggerOnPlay`）；旧表只有 `trait_onplay` 那 142 条在 `ability` 桶里。**它们本来就不算时长**，只是以前没被看见 |
| `tweenRows` / `tweenWaitedRows` | 67 / 4 | **133 / 21** | 跟着 `other` 行数涨；`cardsWhoseTweenIsWaited` 仍 **0** ⇒ **`activeWithTween == active` 逐卡相等**，不影响时长 |
| `unresolved` / `conditional` / `uncovered` / 两链交叉校验 | 0 / 1 / 3 / 93 全一致 | **同** | 全部不变 |
| **运行期件** | 8,076 B · `81bf9df7…` | **8,076 B · `81bf9df7…`** | **逐字节相同** —— 重出只修全量件的备查段，运行期一个字节没动 |

- 一处**看着像重复、其实不是**：`SOR21 / Death Cult Strike / cardAbilities[0].abilityLogic[0]` 出现 **2 行** ——
  SO 里同一 GUID 确实挂在 `cardAnimations[1]` 与 `[2]` 两个槽（原版会播两次），如实保留。
- 口径未变的证据：共用 `other` 行的 `seconds` **diff = 0**；`active` 的 93 张卡**逐卡值 + 逐条 anim 全同**。

### 4·4 ⑨ `Shake *.json` 21 份 vs 运行期表 17 个 —— 查清了，**不是漏收**

- 多出的 **4 份**是 `Shake Target 1.5s` · `Shake Target 1.5s subtle` · `Shake Target 2s` · `Shake Target Deathspinner`。
- 判据：这 4 份的 `m_Script.m_PathID = 8352632435378098487`（**不是** 屏震 preset 的 `…284`），
  结构键是 `tweens` / `mutedTweens` / `waitAnimation` / `callBackTime`（= **`UnitTweenSO` 的补间资产**），
  且它们都在 `Resources/UnitTweens.json` 的 `items` 里、在 `数据/索引/anim_address_map.json` 的
  `guid_to_asset` 里各有一条 `MonoBehaviour` 记录 —— 名字只是碰巧都以 `Shake Target` 开头。
- `工具/gen_shake_presets.py` 早已按 `m_Name`/`preset` 为空**按设计跳过**并在输出里单独计数（`skipped`），
  **不是缺陷、也不是我们漏收** ⇒ **不改数据**。

---

## 5. 没查清的部分（⛔ 都不许猜）

1. `_d__358` 里 `end += v × Δy × attackPositionYOffset` 的 **`v` 具体是哪个基向量** —— 它来自 `Vector3` 类的静态块
   （`DAT_1842da2a8 + 0xb8` 的 `+0x18` 格），静态块是**运行时初始化**的，静态读 DLL 拿不到值。
   三轴旋转的 `axisVec` 同理（`+0x3c` / `+0x18` / `+0x48` 三格）。
   ⇒ 要做播放器时，**先跑原版用探针打这三个常量**（`scenejumpshot` 可加探针），或从 `UnityEngine.CoreModule` 的
   `Vector3` 静态字段初始化顺序反推；⛔ 不要按「x=right / y=up / z=forward」直接拍。
2. 三轴旋转的**四元数连乘顺序**（`:323-365` 那 4 组乘法）没逐句定序 —— 位置公式不受影响，只有姿态会在多轴同时开时差一个顺序。
3. `visibilityStateAfterFinish != 0` 时原版**什么都不做**（不是「显示」）—— 现读 2 行都是 0，其它值的行为**没有样本可验**。
（原「§4·1 断源是有意还是回归」这一条**已查清**：有意（`gen_card_vfx_by_card.py` 的 T21/R4/C5 三轮注明判据），
且**错的不是分桶、是本生成器只读一个桶** —— 已修，见 §4。）

---

## 6. 改动清单

| 文件 | 性质 | 数字 |
|---|---|---|
| `d:/4/Unity/工具/gen_cardanim_clips.py` | 生成器 | `+150 / -38` |
| `d:/4/Unity/工具/gen_ability_anim_durations.py` | 生成器 | `+120 / -46`（含 §4·2 的取源改造） |
| `d:/4/Unity/MyGame/Assets/CardPresentation/Resources/cardanim_clips.json` | 生成产物（运行期） | `+594 / -58`（250,297 B → 258,932 B） |
| `d:/4/Unity/MyGame/Assets/CardPresentation/Resources/ability_anim_durations.json` | 生成产物（运行期） | `+2 / -1`（7,902 B → 8,076 B） |
| `d:/4/Unity/数据/游戏数据/cardanim_clips.json` | 生成产物（**全量副本**，⚠️ 见下） | `+594 / -58`（与运行期那份逐字节相同） |
| `d:/4/Unity/数据/游戏数据/ability_anim_durations.json` | 生成产物（**全量件，已按追加任务重出**） | 291,946 B → **774,404 B**（见 §4·3 的逐类差异） |

⚠️ **不变式：`数据/游戏数据/cardanim_clips.json` 与 `Assets/CardPresentation/Resources/cardanim_clips.json` 必须逐字节相同**
（`gen_cardanim_clips.py` 文件头 `:24-27` 明写；两份由同一次 `dump_bytes()` 写出）。所以即使「生成产物」那一格只列了
`Resources/*.json`，这一件也必须一起写 —— 只写一份 = 这条不变式当场破掉 + 下一次 `--check` 必红。
要退回请**两个文件一起** revert。（`ability_anim_durations` 那两份**没有**这条不变式：全量件是给工具/人看的备查件。）

## 7. 幂等证据（md5）

| 文件 | 改前 md5 | 改后 md5（连跑两次相同） |
|---|---|---|
| `Resources/cardanim_clips.json` | `eabf542d57ec48c7f2ec4845fc8aa3fb` | `ff76c6581c027f643142f02772883975` |
| `数据/游戏数据/cardanim_clips.json` | `eabf542d57ec48c7f2ec4845fc8aa3fb` | `ff76c6581c027f643142f02772883975` |
| `Resources/ability_anim_durations.json` | `07b56e0e1858b4c4736e8c0132a5267b` | `81bf9df7aa9272e1c6988a7f31590fb5`（追加任务前后**同一个**） |
| `数据/游戏数据/ability_anim_durations.json` | `070b046764cfc2a74d2b1d20f7e12737` | `3f2d47e29ba98bdfa74eda55e1a3a4ae`（重出，774,404 B） |

行尾：全部 `CRLF=0 / LF=N`（与改前一致，`.json` 由生成器 `newline="\n"` 写；`.py` 只走 Edit 工具）。
改前那份全量件已备份到 `d:/4/_tmp_view/_tl_backup_ability_anim_durations_full.json`（复核差异用，可随时删）。

## 8. 自检怎么跑（给调度台）

- `python -I -X utf8 d:/4/Unity/工具/gen_cardanim_clips.py --check` → **`check: OK`**（两份）。
- `python -I -X utf8 d:/4/Unity/工具/gen_ability_anim_durations.py --check` → **`--check 全过 ✅`**（两份）。
- 未跑 Unity（红线①）；受影响宿主若要走批处理：`BattleScene.Run`（`CardAnimClips` 的消费点在 BattleDriver）。
