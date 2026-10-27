# B9_W1BDR —— 「特效施工单」落在 `BattleDriver.cs` 的 4 条

> 文件所有权：本代理只改 `CardPresentation/Battle/BattleDriver.cs`（+ 本报告 + 账补丁）。
> 类型检查：`TMPDIR=/tmp/wf_w1 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（改完跑两次；第一次那 17+103 条全在别人的文件 `Core/CardView.cs` / `Editor/ShellScene.cs` 等，属并发写手，第二次全清）。
> ⚠️ 施工单那 4 行的「我们的落点 文件:行」**全部已漂移**（16642 / 22665 / 23982 现在都不是它写的方法）—— 下面一律给**现读行号**。

---

## 一、结论（逐条：原版怎么算 → 我们原来怎么算 → 改成什么 → 怎么验）

### #7 `VoiceLinesPopUpShowedUp` —— **已经是对的，只需次序对齐**（施工单的「未接」是过期结论）

| | 内容 |
|---|---|
| 原版怎么算 | `BattleManager.VoiceLinesPopUpShowedUp()`（`ilspy BattleManager.cs:19522-19528`）：`isAnyPopUpOpen(+0x298) = true` → `SoundManager.Play2D(soundCollection.ChatShow)`。它是 `voiceLinesController.OnPopUpShow` 的订阅者（同文件 `:19507-19511`）。**触发链**：`VoiceLinesController.ClickChat()`（`ilspy VoiceLinesController.cs:788-801`，`isPopUpShown` 为假就只 `Log("Chat unavailable")`）→ `voiceLinesPopupSelector.Clicked()` → `Show()` → **`Show()` 末尾的 `OnShow`**（`VoiceLinesPopupSelector__Show.c:38-43` 那个 `(**(code**)(lVar2+0x18))(...)`）→ `VoiceLinesController.PopUpShowed()`（`ilspy :1157-1170`，顺带 `chatButton.interactable = false`）→ `OnPopUpShow` → 本方法。 |
| 我们原来怎么算 | `ClickChatHud`（`BattleDriver.cs:5891`）里**依次**：`BattleSfx.Play(ChatShow)` → `_chatPopup.Show()` → `UpdateHud()`。音**早就接了**（`T06` 2026-10-15），工序单写的「未接」是过期结论。 |
| 改成什么 | **只调次序**：`_chatPopup.Show()` → `BattleSfx.Play(ChatShow)`（原版那一跳在 `Show()` **之内**的回调里）＋ 就地补判据注释（含「入口唯一性」的核证）。 |
| cue 是否真的响 | ✅ 真：`Resources/battle_sounds.json` 的 `cues[chatShow] = { clips:["Combat_Chats_Show"], srcCue:"Combat Chats Show" }`，音频在 `Resources/Art/audio/sfx/Combat_Chats_Show.wav`（`.meta` 同在），不在 `gaps[]` 那 13 条里。 |
| 怎么验 | 真机点聊天钮 ⇒ `Combat_Chats_Show.wav` 在气泡出现**同帧**响一次；`_chatPopup.Show()` 全仓**只有这一个调用点**（`grep _chatPopup.Show`），且原版 `VoiceLinesPopupSelector__Show` 在 `.c` 全树**没有第二个调用者** ⇒ 不存在「别处开窗不响」的漏口。 |

### #10 `ResolveAnimInfoRangeAttack` —— **补了 `screenShake`；`skipSound` 那条断言本身不成立；`unitsShake` 发现新缺口（未做，见 §三）**

| | 内容 |
|---|---|
| 原版怎么算（`_d__468__MoveNext.c` 全 149 行逐句） | state0：`WaitForSeconds(animInfo.startDelay /*+0x28*/)`（`:29-31`）→ state1：`StartCoroutine(DelayedVFXPlay(...))`，**同帧**接着 `if (animInfo.screenShake /*+0x60*/ != null && screenShake.CanPlay()) screenShake.Play();`（`:46-67`）→ 再 `unitsShake(+0x78)` 非空则逐条 `ResolveUnitShake(new UnitShake(info, acting, target))`、**否则**（if/else）`tweenAnim(+0x80)` 非空则 `BuildSequence` + 可选 `WaitForCompletion` → `ResolveAnimAnimationClip(...)` → state3：`if (waitForAnim) WaitForSeconds(animInfo.GetAnimDuration())`。 |
| `CanPlay` 到底是什么 | `OverwriteCameraShakePreset__CanPlay.c:14-17` = 一句 `UnityEngine.Object.op_Implicit(presetSO)` ⇒ **仅判 `presetSO != null`**（与我们 `CardAnimClips` doc 的写法一致）。`Play()` = `GetModifiedPreset()` → `CameraShakePreset.Play()`；`GetModifiedPreset.c:22-27` 起：`modified.delay(0x10) = this.delay(0x18)` **无条件**，其余 5 项按 `overwrite*` 布尔逐位取 `this.*` 或 base 自身。 |
| `skipSound` | 🔴 **它是死形参**：`dump.cs:28854-28862` 的 `<ResolveAnimInfoRangeAttack>d__468` 字段表只有 `<>1__state / <>2__current / animInfo(+0x20) / <>4__this(+0x28) / actingCard(+0x30) / targetCard(+0x38) / waitForAnim(+0x40)` —— **没有 skipSound**，`MoveNext` 里也**只读 `+0x40` 这一个 byte**（两处：state3 的 `WaitForSeconds(GetAnimDuration)` 与传给 `ResolveAnimAnimationClip` 的第 5 实参）。⇒ 施工单那条验收「skipSound=1 不播音」**在原版里不存在**，⛔ 别照它造一道闸。 |
| 我们原来怎么算 | 远程攻击的「件」走 `ScheduleAttackPresentation`（`:22138`）逐件排期 → `FireAttackVfxWithCtx`；**`screenShake` 一条都没播**（`PlayBeatShake` 只在 `play`/`trigger`/`ability` 拍的 `PlayPlacedOne`（`:23814`）与 `PlayPlacedPieces`（`:24316`）上）。 |
| 改成什么 | 逐件补：`if (e.Ranged)` ⇒ `CardAnimClips.PlacementOf(CardAnim 名)` 的 `ShakePreset` 非空时，`ScheduleAction(vfxAt + startDelay, () => PlayBeatShake(place))` —— 门/覆盖值/延迟全部复用既有唯一实现 `PlayBeatShake`（`:23537`，⛔ 没另写一套震屏）。**时刻**取 `vfxAt + startDelay`（= `ResolveAnimInfoRangeAttack` 进 state1 那一刻），⛔ 不用 `items[i].T`（那是 `vfxDelayTime`，在 `DelayedVFXPlay` **里面**等）。 |
| 为什么只对远程 | 近战的震屏来自 `ResolveAttackAnimationEffects`（已由 `PlayHitFrameShake` 覆盖）；现读 `attack_melee` 桶 12 件**没有一个**带 `shk`（交叉核过），`attack_ranged` 桶 **13 件**带。 |
| `waitForAnim` 那一半 | 原版在整条协程尾部等 `GetAnimDuration()`；我们那条由队尾 `ExtendTimelineTail(AttackVfxTailAt(...))` ← `CardAnimClips.RangedChainDuration`（逐件 `GetAnimDuration`）覆盖，属既有已核实现，本批未动。 |
| 怎么验 | 数据侧可先验：`Resources/cardanim_clips.json` 里 `shk` 非空 57 行 / 10 preset；`attack_ranged` 桶 13 件（`Atk_BulletImpact_9shot_EC|_chaos|Ork Missile Big|Tau_Pulse Bomb|Tyranid_AcidSpray|Psychophage|Pyrovore|Screamer-Killer|Spore Launch|FlamethrowerBurna|TankShot_big|TankShot_small|Psychomancer_Stun`）。真机：这几张卡远程攻击时**镜头会震**（`Shake Light 2s` / `Shake Hit Small` / `Shake Small`），改前完全不震。 |

### #11 `ResolveAttackRangedAnim` —— **口径已逐句核完，本来就对（无需改代码）**

| | 内容 |
|---|---|
| 原版怎么算（`CardScript._ResolveAttackRangedAnim_d__360__MoveNext.c` 全 183 行） | state0：三条 tween（`DOLocalMove(handHome,+0x1a0 时长)` + `DORotateQuaternion(静态 identity 类)` + `DOScale(DefaultScale)` → `WaitForCompletion`）；state1：`WaitForSeconds(AttackStepTime)`；state2：`HasCustomRangedAttackParticles() ? GetRangedAttackParticles() : GetRangedClanParticles(rawCard+0x1c8, rawCard+0x2c /*cardArmy*/)` → `ResolveAnimInfoRangeAttack(bm, animInfo, this, target, waitForAnim:1, skipSound:0)`。`GetRangedClanParticles`（`RangedAttackParticlesByClanSO__GetRangedClanParticles.c:9-38`）= 线性找 `entry.m_Clan == cardArmy`，**查不到回 `entries[0]`**。 |
| 我们原来怎么算 | ①回正：`CardFeel.AttackRecover(...)`（`BattleDriver.cs:24872`，0.1 s 三条同时）；后 0.1 s 是纯等待、已含在 `EventTiming.AttackStepRanged = 0.2`（`EventTiming.cs:781`）。②件：`VfxMap.ResolveRangedAttackPieces(VfxFactionOf(卡自己), cardId, times)`（`VfxMap.cs:1757`）→ `ResolveAllTimed(perCardOnly:false)`，兜底顺序 = **逐卡 → `ByFaction[attackRanged][faction]` → `ByEvent`**（= 原版 SO 第一条 `BulletImpact_2shot_trail`）。③`cardArmy` 来源 = `VfxFactionOf`（`:9642`）：instanceId 反查棋盘 → slot + **名字守卫** → 池里按卡名/阵营查 `Card.Faction` → 都没有才退回座位阵营并**出声**。 |
| 结论 | 三层链、`cardArmy` 口径、`waitForAnim=1`、回正三件**逐条对得上**，本批**不改**。 |
| 怎么验 | 给一张带 `customRangedAttackParticles` 的卡与一张只有 clan 件的卡，各打一次远程 ⇒ 前者播自己的件、后者播本阵营件、跨阵营攻击时不会取成座位阵营的变体。 |
| ⚠️ 一处不可观测项（如实记） | `startDelay`/`vfxDelayTime` 在 `attack_ranged` 件上现读**全 0**（`cardanim_clips.json` 里 `sd != 0` 全池只有 1 行 `Cuts Brutal Warp Spider 0.5`；带 `shk` 的 13 行 `vd` 都是 0）⇒ 「起播偏移」这一档今天没有可观测差，所以我们那条 `vfxAt + T`（不含 `startDelay`）与 `vfxAt + startDelay` 在本卡池上等价。 |

### #13 `ResolveBloodThirstAnim` —— **激活判定第 3 条补齐（本批唯一的逻辑改动）；`顶替动作` 是队列 vs 事件流的架构差**

| | 内容 |
|---|---|
| 原版怎么算 | `ResolveBloodThirstAnim`（`:1-132`）= 结算那一刻**重跑** `CardScript.ActivateBloodThirst(card)`；为真 ⇒ 「结构块拷贝」（就是那三个 32×8 字节的 `do{}while` memcpy，只为把 action 结构传给尾部）→ `HighlightBloodThirst()`；无论真假最后都 `FinishResolvingAction`。`ActivateBloodThirst`（`CardScript__ActivateBloodThirst.c:9-22`）四条合取：`HasCurrentTrait(0xdc /*bloodthirst*/)` && `+0x48 == 1` && **`isPlayer(+0x40) == BattleManager.IsPlayerTurn()`** && `cardState(+0x228) != 5`。`HighlightBloodThirst`（`CardScript__HighlightBloodThirst.c:5-13`）= `BattleCardUI.HighlightBloodThirst(ui)` + `BattleCardUI.DisplayTriggerAnim(ui, 0xdc)`（第 3/4 个实参 `1,0` 是 Ghidra 的残留寄存器，真签名只有 2 参 —— `BattleCardUI__DisplayTriggerAnim.c:2`；traitValue 是它内部现取的 `GetCurrentTraitValueWithModifiers(0xdc)`，`:83-95`）。 |
| 「顶替动作」 | = `CardScript.SendHighlightBloodThirstAction`（`SendHighlightBloodThirstAction.c:20-22`：`action 0x2b`）→ `BattleManager.AddAutoActionToQueue` → `ResolveAction` → 本方法。**我们的事件流没有这条队列**：`RuleCore.EmitBloodThirst` 直接发 `EvtKind.Trigger(keyword=bloodthirst)` 由表现层消费（同一件事的两条架构路，已在本仓多处这样记过）。 |
| 我们原来怎么算 | `PlayTraitPulse`（`:24602`）对**所有**关键词一视同仁：`TraitFrames.DisplayTrigger(kw, isWarlord, KwValue)` + 徽标脉冲。引擎侧 `RuleCore.EmitBloodThirst`（`:12390`）只挡了 ①`u.Has(bloodthirst)` ②`AttacksThisTurn != 1`。 |
| 缺口 | ③「卡属于当前行动侧」**两侧都没挡**（引擎那条的 doc `RuleCore.cs:12373-12374` 明明把「是本方回合」写进了判据，代码里没有）；④`+0x228 != 5` 无等价物（濒死在引擎里已离场，无观测差，早已注明）。另外 `bloodThirstFrame` 那条 `DOFade` 已判**例外①**（原版死字段：`dump.cs:26221` 无 `[SerializeField]`、全库 0 写点、`CardPrefab.prefab` 无该节点 ⇒ 原版不产生画面）。 |
| 改成什么 | 在 `PlayTraitPulse` 开头加**表演闸**：`Ctx != null && Ctx.Active >= 0 && e.Player != Ctx.Active && kw == bloodthirst` ⇒ `Log` + `return`（= 原版「`ActivateBloodThirst` 为假 ⇒ 一行都不演」）。绝对座位换算是 `isPlayer == IsPlayerTurn` 的直接等价（两者都是**相对本机**的说法）。 |
| 为什么闸在表现层而不是引擎 | 本代理白名单只有 `BattleDriver.cs`；`RuleCore.cs` 的同一条缺口已在 §四「改动请求」里交给调度台（两侧都补才彻底：引擎那条还会影响我们自设计的 `BroadcastWhen(Triggers(BloodThirst))`，而原版 bloodthirst 那一跳**不广播**）。 |
| 怎么验 | 构造「A 侧带 bloodthirst、`AttacksThisTurn` 刚变 1 但当前 `Ctx.Active != A`」⇒ 改前会演状态框，改后不演且日志出声；正常（本方回合）那一路**不受影响**（`BattleScene.cs:10896` 那条断言直接调 `DisplayTrigger`，不经过本闸）。 |

---

## 二、证据（文件:行号）

**原版（第一权威）**
- `d:/2/tools/decomp_full/BattleManager__VoiceLinesPopUpShowedUp.c:9-25`（`+0x298=1` + `get_ChatShow` + `Play2D`）
- `d:/2/tools/decomp_full/VoiceLinesPopupSelector__Show.c:14-43`（`SetActive(true)` → `interactable` → `RefreshChatLines` → 建 Sequence → 末尾 `OnShow`）
- `d:/2/tools/decomp_full/BattleManager._ResolveAnimInfoRangeAttack_d__468__MoveNext.c:29-31`（`startDelay`）`:46-67`（`DelayedVFXPlay` + `CanPlay/Play`）`:69-108`（unitsShake / tweenAnim）`:111-124`（`ResolveAnimAnimationClip`）`:126-141`（`waitForAnim`）
- `d:/2/tools/decomp_full/OverwriteCameraShakePreset__CanPlay.c:14-17` · `__Play.c:9-16` · `__GetModifiedPreset.c:22-27,60-150`
- `d:/2/tools/decomp_full/BattleManager._ResolveAnimAnimationClip_d__473__MoveNext.c:46-118`（body clips `+0x68` + `WaitUntil`）
- `d:/2/tools/decomp_full/CardScript._ResolveAttackRangedAnim_d__360__MoveNext.c:46-104`（回正三 tween）`:112-140`（`WaitForSeconds(AttackStepTime)`）`:142-176`（三层链 + `waitForAnim:1`）
- `d:/2/tools/decomp_full/RangedAttackParticlesByClanSO__GetRangedClanParticles.c:9-38`（按 clan 找、**查不到回第一条**）
- `d:/2/tools/decomp_full/BattleManager__ResolveBloodThirstAnim.c:21-29,120-129` · `CardScript__ActivateBloodThirst.c:9-22` · `CardScript__HighlightBloodThirst.c:5-13` · `BattleCardUI__DisplayTriggerAnim.c:2,83-95` · `CardScript__SendHighlightBloodThirstAction.c:20-22`
- `d:/2/tools/decomp_full/UnitShake__.ctor.c:11-21`（`BattleManagerSupport.GetTargetUnit(targetUnit/awayFromUnit, acting, target)`）+ `d:/2/tools/il2cpp_out/dump.cs:45603-45612`（`TargetUnit { undefined 0 · self 10 · cardTarget 20 · abilityTarget 30 · friendlyHero 40 · enemyHero 50 }`）
- `d:/2/tools/il2cpp_out/dump.cs:28854-28862`（`d__468` 字段表 = **无 skipSound**）· `:25205-25243`（`AnimInfo` 字段：`vfxDelayTime 0x24 / startDelay 0x28 / screenShake 0x60 / minionAnimationClipInfo 0x68 / minionMovementInfo 0x70 / unitsShake 0x78 / tweenAnim 0x80`）· `:26221`（`bloodThirstFrame 0x268` 无 `[SerializeField]`）
- `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/BattleManager.cs:19507-19511,19522-19528` · `VoiceLinesController.cs:788-801,1157-1170`

**我们（改后现读）**
- `CardPresentation/Battle/BattleDriver.cs:5914-5919`（#7：`Show()` → `Play(ChatShow)`）
- `CardPresentation/Battle/BattleDriver.cs:22197-22215`（#10：远程逐件 `screenShake`）
- `CardPresentation/Battle/BattleDriver.cs:24617-24634`（#13：bloodthirst 行动侧闸）
- 既有实现（本批未动，仅引用）：`:23537`（`PlayBeatShake`）· `:22138`（`ScheduleAttackPresentation`）· `:23063`（`atkFaction = VfxFactionOf`）· `:24872`（`CardFeel.AttackRecover`）· `:9642`（`VfxFactionOf`）· `Core/VfxMap.cs:1757`（三层链）· `Core/VfxMap.cs:1500`（`ResolveAllTimed`）· `Core/CardAnimClips.cs:159-171`（`ShakePreset/ShakeDelay/...`）· `RuleEngine/Core/RuleCore.cs:12390-12425`（`EmitBloodThirst`）

---

## 三、没查清的部分 / 未做（如实记，⛔ 不许当成已完成）

1. **`AnimInfo.unitsShake`（+0x78）整条链没做**（这是 #10 里我新查出来的**最大一块**）：
   - 原版行为：`_d__468:69-108` 里 `unitsShake` 非空 ⇒ 逐条 `ResolveUnitShake(new UnitShake(info, actingCard, targetCard))`（**抖动单位本体**，不是镜头）；且与 `tweenAnim` 是 **if/else**。
   - 量：全池 **259** 个 CardAnim 有 unitsShake；其中落在 **`attack_ranged` 桶 127/215 件**（`tweenAnim` 另有 **150 件**）。样本：`Atk_Gauss_Tesla` = `{targetUnit: abilityTarget(30), awayFromUnit: self(10), shakeSize: 0, duration 0.4, delay 0.4}`。
   - 为什么没做：要动三处**都不在我白名单**——①数据列（`工具/gen_cardanim_clips.py` 现只写 `mm*`/`shk` 两组，**没有** `unitsShake` 列）②运行期契约（`CardPresentation/Battle/CardAnimClips.cs` 的 `RtRow`）③抖动原语（`Core/CardFeel.cs` 的 `DoUnitShake`/`ShakeUnit`，**W2 正在改这个文件**）。⇒ 交给调度台切块。
2. **`AnimInfo.minionAnimationClipInfo`（+0x68）没有数据列**：`ResolveAnimAnimationClip` 的 body-clip 半（`_d__473:46-118`）我们整条没有。**当前无观测差**（现读 1152 个 CardAnim 里只有 `Atk_Gauss_Tesla` / `Atk_Gauss_Tesla_Annihilator` 两件的 `minionClips` 非空，且两者的 `animationClip.m_PathID = 0`（null）⇒ 原版那两件也什么都不播、`WaitUntil` 立即通过）。**但「没影响」是数据现状，不是实现**。
3. **`tweenAnim`（UnitTweenSO，+0x80）没有消费**：150 个 `attack_ranged` 件有非空 `tweenAnim`；原版在 unitsShake 为空时才走它（`BuildSequence` + 可选 `WaitForCompletion`）。`CardPresentation/Resources/UnitTweens.json` 存在，但 `CardAnimPlacement`/`RtRow` 没有这一列 ⇒ 未接。
4. **`OverwriteCameraShakePreset` 的 3 组覆盖（sustainTime / direction / attackTime / decayTime）我们没传**：`RtRow` 只收了 `amplitude`/`frequency` 两组的 `overwrite*`；`GetModifiedPreset.c:60-150` 还有 `overwriteSustainTime(0x1C)`·`overwriteDirection(0x34)`·`overwriteAttackTime(0x44)`·`overwriteDecayTime(0x4C)`。现读数据里这四组的 `overwrite*` 是否有非 0 —— **没查**（`gen_cardanim_clips.py` 的 `ssOvSustain/ssSustain/ssOvDir/...` 是全量列，但我没统计取值）⇒ 若全 0 则无差；若有非 0 就是真缺口。**这条属于 W2（`CardFeel` 震屏表）口径**，我按简报只报不改。
5. `CardFeel.ShakeAmpUnitSize` **本批没有改动请求**：上面 4 条里没有一条需要改震屏幅度表（`PlayBeatShake` 只是把 `CardAnimPlacement` 已有的 4 个字段原样透传）。
6. `#9 PlayAnim` / `#8 ShowPlayerActionAnim` / `#12 MoveAndDisappearAnim` 按简报**未做**（不在本代理范围）；但顺带确认：施工单给它们的落点（`22665` / `17203` / `22995`）**同样已漂移**，接手的代理要自己按方法名定位。

---

## 四、改动清单（全部在 `CardPresentation/Battle/BattleDriver.cs`，行号为改后现读）

| # | 行 | 改法 | 依据 |
|---|---|---|---|
| 1 | `5914-5919` | `BattleSfx.Play(ChatShow)` 与 `_chatPopup.Show()` **对调**（先 Show 再播音），注释改成「回调在 `Show()` 之内 + 入口唯一性已核」 | `BattleManager__VoiceLinesPopUpShowedUp.c:9-25` + `VoiceLinesPopupSelector__Show.c:38-43` + `ilspy VoiceLinesController.cs:1157-1170` |
| 2 | `22197-22215` | 远程攻击**逐件**补 `screenShake`：`e.Ranged` && `PlacementOf(an).ShakePreset` 非空 ⇒ `ScheduleAction(vfxAt + startDelay, () => PlayBeatShake(place))` | `_d__468:46-67` + `OverwriteCameraShakePreset__CanPlay.c:14-17` / `__GetModifiedPreset.c:22-27` |
| 3 | `24617-24634` | `PlayTraitPulse` 加 bloodthirst 的**行动侧表演闸**（`e.Player != Ctx.Active` ⇒ Log + return） | `CardScript__ActivateBloodThirst.c:15-21` + `ResolveBloodThirstAnim.c:21-29` |

> 行数影响：文件 `35091 → 35130` 行（+39）。**下游行号整体漂移** ⇒ 同步点 `check_ledger_points --all-A --fix` 需要重跑一次（`BattleDriver.cs` 里 `:24635` 之后的所有 A 行都会移到 +19/+39 区间）。⚠️ 已在 `_parts/B9_W1BDR.tsv` 里把这 4 条账的落点改成**现读行号**。
> 行尾：改后仍是纯 CRLF（`CRLF 35129 / LF 35129 / bareCR 0`），**没有翻行尾**。
> 类型检查：`typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。

### 改动请求（不在本代理白名单，请调度台转派）
- 🔴 **`RuleEngine/Core/RuleCore.cs:12390 EmitBloodThirst`** 缺第 3 条判据「是本方回合」——它自己的 doc（`RuleCore.cs:12373-12374`）已经把 `&& 是本方回合` 写进原版判据，代码只判了 `Has` 与 `AttacksThisTurn == 1`。建议加 `if (ctx.Active != p) return;`（**注意**：加它会同时挡住我们自设计的 `BroadcastWhen(Triggers(BloodThirst))`；而原版 bloodthirst 那一跳本来就不广播 ⇒ 加了才与原版一致）。本代理已在**表现层**补了等价闸（改动 3），但引擎侧那条仍建议收口（否则将来若有人按事件做别的消费端，又会漏）。
- ⚠️ **`CardPresentation/Battle/CardAnimClips.cs`（`RtRow` + `CardAnimPlacement`）与 `工具/gen_cardanim_clips.py`**：`unitsShake`（5 列：targetUnit/awayFromUnit/shakeSize/shakeDuration/shakeDelay）与 `tweenAnim`（UnitTweenSO 引用）需要加列，才能接 §三·1/§三·3。**同批不要与 W2 的 `CardFeel.cs` 撞车**（抖动原语在那边）。

---

## 五、顺手发现（不是本批 4 条里的，但下个会话需要知道）

1. **施工单的行号全体漂移**（#7→16642 现是 `ScriptedChangeAttackMatches`、#10/#11→22665 现是 `VfxMap` doc 段、#13→23982 现是 `PlayTraitPulse` 之前的注释块）。这不是文档写错，是「账按行号引用 + 文件一直在长」的必然结果 ⇒ 派活时**给方法名，别给行号**。
2. **`BattleManager._ResolveAttack_d__438:700/803` 还有两个 `ResolveAnimInfoRangeAttack` 调用点**（走的是 `CardTrait(0x500).CustomObject` 取到的 CardAnim，`acting/target` 与主链**相反**，`waitForAnim/skipSound` 来自栈上布尔）—— 这是「哨戒 / 反制」那一族，**我们没复刻**（`grep` 全仓无对应）。它同时也是「`skipSound` 唯一可能为真」的入口。⇒ 建议单开一条账，别混进本批。
3. `EvtKind` 的 20 个成员里没有 bloodthirst 专用档（走的是 `EvtKind.Trigger` + `Keyword`），与 CLAUDE.md 里那条「⛔ 别为了让数字对上新增 `EvtKind` 成员」一致 —— 本批没有新增枚举。
4. `CardAnimPlacement.StartDelay` 全池只有 1 行非 0（`Cuts Brutal Warp Spider 0.5`），且它不在 `attack_ranged` 桶 ⇒ 我那条新震屏用的 `vfxAt + StartDelay` 在今天**恒等于 `vfxAt`**，不会改变现有时序；将来若有件带 `startDelay`，它会自动跟上（这也正是原版 state0 的位置）。
