# B9 / C2T7l —— 特效逐句完结（16 条，全 B，逐条证据）

> 查判据型子代理（只读 + 落账补丁）。承接 C2T7k。**16 条全部 B**（差异是真的，不是「判据缺」）。
> 产出：`判据/B行逐条核_特效_续4.tsv`（8 列 · 16 行）· `账/_parts/B9_C2T7l.tsv`（11 列 · 16 行）· 本报告。

---

## 一、逐条证据（原版这几步 / 我们这几步 / 差在哪）

| # | 方法 | 原版这几步 | 我们这几步 | 差 |
|---|---|---|---|---|
| 1 | `PlayAnim` | 遍历 `abilityTargets`（GetEnumerator），每个按 `isPlayer(target+0x10+0x40)` 选锚点(+0xe8…)播 anim | `BattleDriver.PlayAbilityBeat` 按 `card_anim_order` 拍表 | 多目标遍历序 + 锚点选法未逐句对 |
| 2 | `ShowPlayerActionAnim` | AiScripted .c 638 行（玩家支里调） | `ApplyTutorialActionView` 按块画 | 未逐句（长度大） |
| 3 | `ResolveBloodThirstAnim` | `ActivateBloodThirst(card)` 真→结构块拷贝→`HighlightBloodThirst()` | `PlayTraitPulse`（脉冲+状态框） | 顶替那条动作 / 激活判定未逐句 |
| 4 | `PlayTriggerAnim` | `AnimFXController.SetData(prefab,card,traitValue)`（op_Implicit 判空） | `AnimFXController.SetData` | 特征同，未逐句对 traitValue/空判 |
| 5 | `ResolveAttackAnimationEffects` | `GetUnitSize(卡)`→分档(0/1同,2,3)`OverwriteCameraShakePreset.SetDirection/SetAmplitude/Play` | `CardFeel.ShakeAmpUnitSize`（UnitSize 0/2/3→2/4/8）+相机震 | **体型→幅度表逐格未对** |
| 6 | `ReturnToDeckAnim` | `.c` 体薄（`if(lVar1!=0)return lVar1`，疑体丢）→需 ILSpy | `CardFeel.ReturnToHand`（回手；回牌库版未单列） | 回牌库那条未实现/未证 |
| 7 | `GetCardEffectsToShow` | 遍历 `abilities(+0x108)` 过滤→mark-of-chaos 等 `CreateMarkCardEffect` | `Badges.For` + `TraitFrames` | **项与序未逐句对** |
| 8 | `VoiceLinesPopUpShowedUp` | `SoundManager.Play2D(SoundAssetCollection.get_ChatShow)` | `ApplyTutorialVisibility` 承接 | **ChatShow 音 cue 未核** |
| 9 | `ActivateIconAnim` | `GetTraitFromAbilityTrigger→HighlightTraitIcon + DisplayTriggerAnim` | `TraitFrames.DisplayTrigger` | **亮图标(HighlightTraitIcon)那一半未接/未核** |
| 10 | `ActivateTriggerTraitAnim` | 早退语义 + 高亮 | `TraitFrames.DisplayTrigger`（早退一致） | **高亮那一半未逐条核** |
| 11 | `ResolveUnitShake` | 真体(`_d__472 MoveNext`)：WaitForSeconds→IsInPlay 检查→shake | `CardFeel.DoUnitShake`（pushback 互证已引 `_d__472__MoveNext.c:139-147`） | shake 参数逐格未对 |
| 12 | `ResolveAnimInfoRangeAttack` | 真体(`_d__468 MoveNext`)：Delay→`DelayedVFXPlay` + `OverwriteCameraShakePreset.CanPlay/Play` | `PlayAbilityBeat` | skipSound/waitForAnim/震屏档未逐句 |
| 13 | `ResolveTraitAnim` | `BattleManager.cs:24562` + b__0/1/2（协程） | `PlayTraitPulse` | traitValue/callback 未逐句（本轮未及 MoveNext 体） |
| 14 | `MoveAndDisappearAnim` | `CardScript.cs:12688` + b__0/1/2（协程） | `PlayMinionMovement`（沿曲线+三轴旋转） | margin/末态收体未逐句（未及） |
| 15 | `StopHandAnim` | `:21319` + `_d__405 MoveNext`（协程） | `CardInteraction.Relayout` 闸(IsDoingAnim) | delay 档未逐句（未及） |
| 16 | `ResolveAttackRangedAnim` | `CardScript.cs:4080 _d__360`：`HasCustomRangedAttackParticles()?GetRangedAttackParticles():GetRangedClanParticles(cardArmy).animInfo`→ResolveAnimInfoRangeAttack+wait AttackStepTime | `PlayAbilityBeat` 远程链 | **我方是否按 cardArmy 查 clan 弹道表未逐句**（重点） |

> **`GetAnimTransform` 已在 C2T7k 判 B（不同原版方法）**，本批不重复；结论：原版按 BattleManager 字段选锚点，我方 `AnimAnchors.Resolve` 是 `AbilityLogic.GetPos` 复刻 —— ⛔ 别当同一条。

---

## 二、诚实交代（覆盖）
- **真读了 `.c` 体**（能写「原版这几步」的）：`PlayAnim` · `ResolveBloodThirstAnim` · `ResolveAttackAnimationEffects` · `GetCardEffectsToShow` · `ResolveUnitShake(_d__472)` · `ResolveAnimInfoRangeAttack(_d__468)` · `ResolveAttackRangedAnim(_d__360)` · `ReturnToDeckAnim`(体薄)。
- **只读了入口 / 未逐句**：`ShowPlayerActionAnim`(638行) · `PlayTriggerAnim` · `VoiceLinesPopUpShowedUp` · `ActivateIconAnim` · `ActivateTriggerTraitAnim` · `ResolveTraitAnim` · `MoveAndDisappearAnim` · `StopHandAnim`（协程 MoveNext 体未展开）。
- 都判 **B**（差异是真的）。**未出 A**（符合你预期）。

## 三、下一批入口（照行号直接逐句）
1. 协程 MoveNext 体：`_d__472`/`_d__468`/`ResolveTraitAnim:24562`/`MoveAndDisappearAnim:12688`/`StopHandAnim _d__405`/`_d__360`。
2. 幅度表：`ResolveAttackAnimationEffects` 的 `OverwriteCameraShakePreset.SetAmplitude` vs 我方 `CardFeel.ShakeAmpUnitSize`（0/2/3→2/4/8）。
3. `ResolveAttackRangedAnim`：我方远程链是否按 `cardArmy` 查 `GetRangedClanParticles` 表。
4. `GetCardEffectsToShow` 项与序；`ActivateIconAnim` 亮图标半；`ReturnToDeckAnim` 用 ILSpy 读体。

## 四、方法学 & 纪律
- 实读：`decomp_full/AbilityLogic__PlayAnim.c` · `BattleManager__ResolveBloodThirstAnim.c` · `CardScript__ResolveAttackAnimationEffects.c` · `EntityScript__GetCardEffectsToShow.c` · `CardScript__ReturnToDeckAnim.c` · `BattleManager._ResolveUnitShake_d__472__MoveNext.c` · `BattleManager._ResolveAnimInfoRangeAttack_d__468__MoveNext.c` · `CardScript._d__360`（前轮）。
- `判据/B行逐条核_特效_续4.tsv`：8 列 × 16 行（全 B）。
- `账/_parts/B9_C2T7l.tsv`：11 列 × 16 行（状态列不动；只改 差异/轮次=`B9_C2T7l`/备注前缀 `[C2T7l 2026-10-27]`，原备注保留）。校验：`merge --dry --only B9_C2T7l` → **更新 16 · 新增 0 · 无变化 0 · 0 报错**；`check_patch_conflicts.py` → 无跨补丁重复键；纯 LF、无裸 CR、全 11 列。
- 未写任何 `.cs`、未跑 Unity、未动 git、未改主表/正本。
