# 执行代理 W（B8_W）· 手牌层与卡感 —— 第 8 轮

> 白名单：`CardPresentation/Hand/**`、`CardPresentation/Core/CardFeel.cs`、账补丁 `账/_parts/B8_W.tsv`、本报告。
> 判据源：🥇 `d:/2/tools/decomp_full/*.c` · 🥈 `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/*.cs` · 字段偏移 = `d:/2/tools/il2cpp_out/dump.cs`
> 本轮**未改任何 `.cs`**（结论全部落在账与报告里；要动手的那几条都在白名单外或需要裁定，见 §⑥）。

---

## ① 结论（上一波留下的 7 项）

| # | 项 | 结论 | 状态变化 |
|---|---|---|---|
| 1 | `CardScript.DisplayHandMinusCard` 真体 | **读完**。真身在 `PlayerHand._DisplayHandMinusCard_d__80__MoveNext.c`（123 行）+ ILSpy `PlayerHand.cs:851-947`。**它不是 `CardScript` 的成员，是 `PlayerHand` 的协程**（账里 1307 行挂在 PlayerHand 名下是对的）。唯一调用者 = `PlayerHand.StartMovingHandCard(card)`（ILSpy `PlayerHand.cs:3308-3320`）。 | 保持 **B**：真体读完，但「起拖那一帧不留洞」这一步我们做成了「直接留洞」（差异 1 帧），见 §②-1 |
| 2 | `GetEffectAnchor` 两个锚点 | **全解**：`+0x290` = `CardUI`（`BattleCardUI`）→ **`BasicCardUI +0x30` = `card2DController`**、**`BattleCardUI +0x1A0` = `effectsAnchor`**。判据是 `cardState` 的一个 8 元素集合。 | 升 **A** |
| 3 | `AttackMeleeAnim` 400+ 行 | **逐段读完**（工厂 27 行 + 状态机 313 行 + 三个闭包）。五段全部对上，**四个数值全部从卡预制体实读且逐个相等**；`Sequence.OnComplete = FinishAfterAttack`、`DOMove.OnComplete` = 命中帧那一跳（`WaitUntil(hasReachedTarget)` 让协程**在命中帧就返回**）。 | 升 **A** |
| 4 | `PlayerHand` 其余 14 条 B | 15 行全部逐条读真体并给出真落点（见 §②-4 表）。**升 A 6 条 · 保持 B 7 条（其中 4 条归 `Battle/MulliganPanel`）· 转 C 2 条**。 | +6 A |
| 5 | `MinionManager` 其余 12 条几何 | 12 条全部读到可复算的公式（§②-5 表）。⛔ 按简报**不动代码**，判据交给调度台。**其中 1 条解开了「敌方槽位要不要镜像」** —— 原版不是镜像，是**按 `+0x40` 选另一对数组**。 | 未改账（简报要求） |
| 6 | 13 场参照物取值（帧桥） | **13/13 完全一致**：`HandArea.localScale=(108,108,108)` · `HandAnchor.localPos=(0.125,1.1,0)` · Canvas(SS-Camera) `planeDistance=100` + 相机 `fov=40` ⇒ **14.8364 px/单位** ⇒ 桥 = **7.2794** · 参考分辨率 1920×1080 `match=0`。敌方链 `HandAnchor.y=0.46` 也复核了。 | 已核清（账里没有独立行） |
| 7 | `ShowCardBeingDrawn` 与 `DrawCard` 翻面条件相反 | **两者不矛盾、两个方向都对、必须分别照抄**：同一个 `isPlayer` 在两条路上回答的是两个不同的问题。裁定建议见 §②-7。 | `ShowCardBeingDrawn` **保持 B**，建议调度台改判 **C** |

---

## ② 逐条证据

### ②-1 `PlayerHand.DisplayHandMinusCard`（真体）

**判据（🥇 `decomp_full/PlayerHand._DisplayHandMinusCard_d__80__MoveNext.c:25-123`；ILSpy `PlayerHand.cs:851-947` 逐句互证）**

```
animFlag(+0x61) = true;
idx = 0; pos = 0;
while (idx < cardList.Count) {
    card = cardList[idx];
    if (card != cardToExclude)                       // ★ UnityEngine.Object.op_Inequality
        if (card.cardState(+0x228) != waitingToBePlayed(4))
            PositionCardInHand(card, pos++, cardList.Count - 1,
                               globalVars.timeToPositionCard(+0xA4), selectedCard: -1);
    idx++;
}
yield return new WaitForSeconds(globalVars.timeToPositionCard);
animFlag = false;
```

- **调用者唯一**：`StartMovingHandCard(card)` = `new d__80{ 4__this = this, cardList = currentHand, cardToExclude = card }` + `StartCoroutine`（ILSpy `PlayerHand.cs:3308-3320`；`decomp_full/PlayerHand__StartMovingHandCard.c` 同一件事）。
- 🔴 **上一波把 `UnityEngine_Object__op_Inequality(uVar4)` 读成 `item != null`、于是判「cardToExclude 没用上」—— 那一读是错的**：Ghidra 把双参 `op_Inequality(item, cardToExclude)` 的**第二个实参丢了**。ILSpy 明写 `if (cardScript != cardToExclude)`。⇒ **这个参数是真在用的**。
- **字段名（`dump.cs` 实读）**：`PlayerHand +0x61 = animFlag`（`PlayerHand__IsDoingAnim.c` 的唯一读口）、`+0x50 = currentHand`、`+0x88 = handAnchorLayout`、`+0x28 = isPlayer`、`+0x90 = lastEmptySlot`；`CardScript +0x228 = cardState`；`VarsGlobal +0xA4 = timeToPositionCard`；`CardStateOptions.waitingToBePlayed = 4`。
- **语义**：**「开始拖某张手牌」的那一刻，把其余 N−1 张（排除被拖那张、再排除正在出台的 state 4）按 **`GetPosition(i, N−1)`** 排满 N−1 格 —— 即【不留洞】**；0.2 s 后落 `animFlag=false`。

**我们侧**：`Hand/CardInteraction.cs:871-952`（`PickUp`）→ `:946` `Relayout(false)` → `:1537-1542` → `HandLayout.Refresh`（`HandLayout.cs:708-836`），此处**带洞**：`_insertIndex` 在 `:903` 先被置成这张牌自己的下标，`Refresh` 用 `slots = n+1`。

**G2 机制差异（可复核）**：原版起拖那一帧是「**无洞、N−1 张间距**」，紧接着的每一帧（`PlayerHand.DisplayHandWithCardSpace`，判据 `decomp_full/PlayerHand__DisplayHandWithCardSpace.c:25-60`，调用点 `BattleManager__Update.c`）才转成「**有洞、N 槽间距**」；我们**把这两步合成了一步**，起拖第一帧就是有洞版。差 1 帧（0.02 s）的间距 —— 数值可算：我方 8 张时无洞间距 = `cap/7`、有洞 = `cap/8`，**差 14.3%**（`HandLayout.IsCompressed` ⇒ `SpacingFor`）。⇒ 稳态一致、**过渡那一帧不同**。
**为什么保持 B 而不是 A**：B2A 口径 G1 要求落点「做的**就是**判据那段」；我们做的其实是**下一段**（`DisplayHandWithCardSpace`）。要 A 就得在 `PickUp` 里先跑一趟无洞重排 —— 那会改真机手感、且与 `UpdateDrag` 的 y 守卫（我们自己加的，见 `CardInteraction.cs:1006` 那段长注）耦合，**本轮不改**，请调度台裁定。
**另外一条同族判据已顺带钉住**：`DisplayHandWithCardSpace` 与我们 `Refresh` 的**洞位**逐句等价 —— 原版 `newIndex = (i<from && slot<=i) ? i+1 : (from<i && i<=slot) ? i-1 : i`，`total = currentHand.Count`（**含被拖那张**）；我们 `GetClosestInHandSlot(world, VisibleCards().Count)` ⇒ `slots = others+1 = N`、`slot_i = (hole && i>=insertIndex) ? i+1 : i`。两者**同一个 N、同一个位移公式** ⇒ 这一支已对齐。

### ②-2 `CardScript.GetEffectAnchor`（两个锚点）

**判据（🥇 `decomp_full/CardScript__GetEffectAnchor.c:8-24`，全文 25 行）**

```
switch 集合 S = { waitingToBePlayed(4), inHandMoving(9), inHandJustCreated(12),
                  inHandPlaying(10), inHandShowing(8), inHand(1), inDeck(0), inDeckDrawing(16) }
if (cardState ∈ S)  return CardUI.card2DController.transform;   // ← 2D 卡体那棵树
else                return CardUI.effectsAnchor;                // ← 特效锚点那棵树
（两处 null 都抛 NRE）
```

**字段偏移 → 名字（`dump.cs` 实读）**：
| 偏移 | 名字 | 出处 |
|---|---|---|
| `CardScript +0x228` | `cardState`（`CardStateOptions`） | `dump.cs:35370+` |
| `CardScript +0x290` | `CardUI`（`BattleCardUI`） | 同上 |
| `BattleCardUI/BasicCardUI +0x30` | **`card2DController`** | `BasicCardUI`（`dump.cs:19811`）字段表 `+0x20 cardImageController / +0x28 cardTextsController / **+0x30 card2DController** / +0x38 flavourTextBGSO` |
| `BattleCardUI +0x1A0` | **`effectsAnchor`**（public Transform） | `BattleCardUI`（`dump.cs:26166`）字段表 |

**⇒ 「两个锚点」的正解**：`effectsAnchor` 是**场上 3D 体的特效锚**；`card2DController.transform` 是**卡还是 2D 平面形态时的锚**。集合 S 恰好就是「这张牌此刻还是 2D 形态」的 8 个状态（**注意 `drawing(7)` 不在 S 里** ⇒ 它在牌库飞出的那一段已经按场上锚算）。

**我们侧**：`Core/CardFeel.cs:968-983`（`EffectsAnchorHeight`）+ `:939-955`（`MeleeEndPos` 两条重载）。唯一消费面 = 近战落点的 `dy`（`EffectsAnchorHeight` 返回「锚点世界 y − 卡**底边**世界 y」，因为我们的卡根是卡心、原版卡根是卡底）。
**G2**：原版返回 **Transform**（两个候选），我们只有**这一个标量**、恒取「特效锚点」那一棵（`CardView.EffectAnchorBoard`）。原版那 8 个 2D 状态在近战落点这条路上**到不了**（近战只发生在 `inPlay`/`inPlayAttacking`，两者都在 S 之外）⇒ **对唯一消费面等价**；若将来有别的消费面要取 2D 锚，得回来补。
**G3 观测量**：受击方 `EffectAnchorHeight` = `1.34 × lossyScale.y`（原版 prefab `Board Elements/EffectAnchor = (0, 1.34, −0.128)`，卡根空间；`dy = 该值`，进 `MeleeEndPos` 时再 `× attackPositionYOffset 0.5`）。视图层读法见 §③。

### ②-3 `CardScript.AttackMeleeAnim`（五段逐句 + 三个闭包）

**判据**：工厂 🥇 `CardScript__AttackMeleeAnim.c:1-27`；真体 🥇 `CardScript._AttackMeleeAnim_d__357__MoveNext.c:1-313`（ILSpy `CardScript.cs:402-780` 互证）；闭包 🥇 `..._b__0.c` / `..._b__1.c` / `..._b__2.c`；`CardScript__get_AttackStepTime.c`。

**逐段（原文 → 我们的落点 `CardFeel.cs:1104-1136`）**

| 段 | 原版 | 我们 | 判 |
|---|---|---|---|
| 0 | `rawCardData.GetAttackSound()` → `BattleMgr.PlaySoundClip`；`CustomDebug.Log("AttackMeleeAnim from {self} to {target}, remnant? {isRemnant}")` | 音效与日志在驱动层（不在本代理面） | 形状不同，见差异表 |
| 0′ | `attacksThisTurn(+0x48)++`；`cardState = inPlayAttacking(3)` | `RuleCore.cs:6241-6252`（`AttacksThisTurn++` / `Exhausted`）+ 我们没有 `cardState` | 已记在账 839 |
| 1 | `seq = DOTween.Sequence()`；**`seq.OnComplete(FinishAfterAttack)`** | 收招由 `CardFeel.AttackRecover`（`:2208-2240`）+ `EventTiming` 排 | **差异 A** |
| 2 | **`seq.OnUpdate(⇒ localScale = MinionScaleByPositionSO.GetScale(isPlayer, localPosition, defaultScale))`** ← 闭包 `b__0` | **`HookDepthScale`（`:1080-1097`）逐帧 `ApplyDepthScale`** | ✅ 对齐（含 `GetScale` 的 `x = isPlayer ? localZ : key1+localZ` 与「末端键」两条，见 `:994-1017` 长注） |
| 3 | `start = transform.TransformPoint(originalLocalPosInPlay(+0x2C4))`；`targetPos = target.transform.position`；`margin = isPlayer ? playerAttackMargin(+0x1B0) : enemyAttackMargin(+0x1B4)` | `MeleeEndPos(origin, targetPos, isPlayer, dy)`：`targetPos − dir×margin`；`origin` 由调用点传 | ✅（`+0x2C4 = originalLocalPosInPlay`，见下「字段表」） |
| 4 | `dir = Normalize(targetPos − start)`；`charge = targetPos − dir×margin + Vector3.up × (dy × attackPositionYOffset)`，其中 `dy = target.effectsAnchor.position.y − target.position.y` | `MeleeEndPos`：`targetPos − dir*margin + Vector3.up*(dy*AttackPositionYOffset)` | ✅ **逐项相同**（含「沿 `Vector3.up` 而不是沿 `dir`」这一跳 —— 见下「易错点」） |
| 5 | `DOMove(charge, attackStepTime).SetEase(Ease.Linear).OnComplete(⇒ ResolveAttackAnimationEffects(effectsAnchor) + hasReachedTarget=true + target.ReceiveAttackAnim(transform.position, this))` → `seq.Append` | `seq.Append(tr.DOMove(endPos, AttackStepTime).SetEase(Ease.Linear))` | ✅ 段形与缓动一致；**OnComplete 那三跳**见差异 B |
| 6 | `q = Quaternion.AngleAxis(attackRotationAngle(25), Quaternion.AngleAxis(90, Vector3.up) × dir)` → `DORotateQuaternion(q, attackStepTime)` → `seq.Join` | `AngleAxis(AttackRotDeg 25, AngleAxis(AttackTurnDeg 90, Vector3.up) × toTargetDir.normalized)`，`Join` | ✅（`CardFeel.cs:1109-1124` 的 `AU1/J1` 注释已闭合此处） |
| 7 | `seq.AppendInterval(0.1)` | `:1129` | ✅ |
| 8 | `DOLocalMove(originalLocalPosInPlay, attackStepTime×2).SetEase(Ease.OutQuart)` → `Append` | `:1130` `DOLocalMove(home, AttackReturnTime=2×AttackStepTime).SetEase(OutQuart)` | ✅ |
| 9 | `DORotateQuaternion(Quaternion.identity, attackStepTime×2)` → `Join` | `:1131` `Join(DORotateQuaternion(homeRot, …))` | ✅ 等价（卡在静止位时 `homeRot` 就是它自己那个朝向） |
| 10 | `seq.timeScale = attackSequenceTimeScale(+0x1AC)` | 不设（= DOTween 默认 1） | ✅ **实读 `= 1.0` ⇒ 逐位等价** |
| 11 | `yield return new WaitUntil(() => hasReachedTarget)` ← `b__2` 读 `+0x24`，`b__1` 在 **`DOMove.OnComplete`** 里置 1 | 无协程：`MeleeAttack` 返回 Sequence，命中帧由调用点排 | 见差异 C（**原版协程在命中帧就返回**） |

**闭包三份（Ghidra 名字与实义不一致，以 ILSpy 为准）**
- `b__0` = **`Sequence.OnUpdate`** = `GetScale(isPlayer, localPosition, defaultScale)` → `transform.localScale`（`_b__0.c:17-45`；ILSpy `CardScript.cs:107-127` + `:540-561` 的 `action2`）。
- `b__1` = **`DOMove.OnComplete`**（不是 Sequence 的） = `ResolveAttackAnimationEffects(CardUI.effectsAnchor, out _)` → `hasReachedTarget = true` → `target.ReceiveAttackAnim(transform.position, this)`（`_b__1.c:11-30`；ILSpy `CardScript.cs:128-144` + `:664-680` 的 `action3`）。
- `b__2` = `() => hasReachedTarget`（`_b__2.c:5-7`；ILSpy `:142-145`）。Ghidra 把它的符号错标成 `MinionAnimClipInfo…get_WaitForCompletion`（**同址折叠**）。
- ⇒ **`Sequence.OnComplete` 是 `FinishAfterAttack`**（ILSpy `CardScript.cs:539-541` 明写 `TweenCallback action = _003C_003E4__this.FinishAfterAttack;`）—— 这一条把账 778 与账 839 串起来了。

**四个数值逐值实读**（`d:/2/解包整理/08_预制体特效/战斗预制体/MonoBehaviour/MonoBehaviour_1744609728290659264.json`）：

| 原版字段 | 实读 | 我们的常量 |
|---|---|---|
| `attackStepTime` (+0x1A0) | `0.10000000149011612` | `CardFeel.AttackStepTime = 0.1f` ✅ |
| `attackPositionYOffset` (+0x1A4) | `0.5` | `AttackPositionYOffset = 0.5f` ✅ |
| `attackRotationAngle` (+0x1A8) | `25.0` | `AttackRotDeg = 25f` ✅ |
| `attackSequenceTimeScale` (+0x1AC) | `1.0` | 未设（默认 1） ✅ |
| `playerAttackMargin` (+0x1B0) | `1.7000000476837158` | `PlayerAttackMargin = 1.7f` ✅ |
| `enemyAttackMargin` (+0x1B4) | `1.0` | `EnemyAttackMargin = 1.0f` ✅ |

**易错点（已复核，不是差异）**：`charge` 的那个加量在 Ghidra 里写成 `fVar15 × (Vector3 静态块 +0x18) × attackPositionYOffset`。`+0x18` 是 `UnityEngine.Vector3` 静态场的**第三项**（字段表 `zero 0x0 / one 0xC / **up 0x18** / down 0x24 …`，同文件 `MoveCardToMulliganPos` 用 `+0xC/+0x14` 取 `one` 作基准缩放，可反证）⇒ **就是 `Vector3.up`**，与我们 `Vector3.up × dy × 0.5` 逐位相同。⚠️ 若误当 `Vector3.one`，会得出「x/y/z 三轴都加」的错结论。

**G2 差异（三条，都写进账）**
- **A**：原版 `Sequence.OnComplete = CardScript.FinishAfterAttack`（收招挂在**动画序列末尾**，`0.1+0.1+0.2 = 0.4 s` 后自动触发）；我们由 `CardFeel.AttackRecover` + `EventTiming` 排。⇒ 「谁在 0.4 s 那一刻收招」的实现位置不同，**净时机需调度台在驱动层对一次**（`BattleDriver` 不在本代理白名单）。
- **B**：原版在**命中帧**（`DOMove` 完成那一刻）做 `ResolveAttackAnimationEffects(effectsAnchor)` + `target.ReceiveAttackAnim(attacker.position, attacker)`；这两跳在本仓分在驱动层（受击反应 `CardFeel.HitReact*` 由驱动层在命中帧调）。⇒ 我们的受击反应是**驱动层一次调用**、原版是**目标卡自己每被 `ReceiveAttackAnim` 通知一次**（`DOMove.OnComplete` 只通知一次 ⇒ 次数上等价）。
- **C**：原版 `AttackStepTime` 在**回放跳过动画**时会被 `÷ 常数`（`get_AttackStepTime`：`BattleManager.ReplaySkipAnims() ? attackStepTime / DAT_1834b2e8c : attackStepTime`）；我们是 `.c` 里那个常量还没解（`DAT_1834b2e8c`），且我们**没有**回放快进这一档 ⇒ 回放局里我们的近战 0.4 s 是原版的 N 倍。**如实记：这一条没查清（常量未解）**，已在 §④。

**G3 观测量（视图层，可读）**：见 §③。

### ②-4 `PlayerHand` 15 条（逐条真体）

判据源：🥇 `decomp_full/PlayerHand__<方法>.c` 与 `PlayerHand._<方法>_d__NN__MoveNext.c`；ILSpy `PlayerHand.cs` 互证。字段名全部 `dump.cs` 实读。

| 账行 | 方法 | 真体（判据出处） | 我们的落点（行号范围） | 判定 |
|---|---|---|---|---|
| 1307 | `DisplayHandMinusCard(cardList, cardToExclude)` | `PlayerHand._DisplayHandMinusCard_d__80__MoveNext.c:25-123` | `Hand/CardInteraction.cs:871-952` → `:1537-1542` → `HandLayout.cs:708-836` | **B**（真体已读；差「起拖那一帧的无洞版」，见 §②-1） |
| 1309 | `FinishMulligan(bool)` | 工厂 `PlayerHand__FinishMulligan.c:5-20` + 真体 `PlayerHand._FinishMulligan_d__52__MoveNext.c:28-171` | `Battle/MulliganPanel.cs`（**白名单外**，未逐行核） | **B**（转派） |
| 1310 | `FinishMulliganFinalPhase()` | `PlayerHand__FinishMulliganFinalPhase.c:17-35`：`if (!isPlayer) return;` 否则对 `currentMulligan` 每张 `BattleCardUI.FadeCardItems(CardUI, DAT_1834b2bb8, 0)` | 同上 | **B**（转派） |
| 1320 | `HandCardMovedToNewIndex(card, newIndex)` | `PlayerHand__HandCardMovedToNewIndex.c:17-46`：`i=IndexOf`；`newIndex==i` **或** `Count<=newIndex` ⇒ 只重排；否则 `Remove`+`Insert(newIndex)`+重排+**`manager.CheckForTutorialPointer()`**；返回 bool | `Hand/CardInteraction.cs:1438-1452`（`Release` 的 else 支） | **B** —— 列表语义逐句对上；**只差最后那一句 `CheckForTutorialPointer`（教程线）**。调度台若认该句归教程线，可直接判 A |
| 1321 | `HandCardReleased()` | `PlayerHand__HandCardReleased.c:5-11`（全文 7 行）= `StartCoroutine(MoveCardsInHandToPosition(-1, null))` | `Hand/CardInteraction.cs:1501`（`Relayout(true)`） | **A** |
| 1322 | `HideHand()` | `PlayerHand__HideHand.c:17-52`：`currentHand` 每张 `gameObject.SetActive(false)`，再 `currentMulligan` 每张同 | **全仓 0 命中**（`grep HideHand` = 0） | **C**（`P2`） |
| 1327 | `LightPlayableHand()` | `PlayerHand__LightPlayableHand.c:19-47`：逐张，跳过 `cardState==4`、跳过 `==10`；`ActivateCard(card, BattleManager.CanPlayCard(card))` | `Battle/BattleDriver.cs:26817-26846`（`RefreshHandPlayable`） | **A**（落点在白名单外，只引用） |
| 1328 | `MoveCardToMulliganPos(card, cardPos, totalCards)` | `PlayerHand__MoveCardToMulliganPos.c:9-101`：`pos = GetPosition(mulliganLayout(+0x1e8→+0x58), cardPos, totalCards, -1)`；敌方支另改 `SetSortingOrder(currentHand.Count−cardPos−1)`、把 `card+0x148` 指向手牌布局、再 `GetPosition(handAnchorLayout, total−cardPos−1, total, −1)`；缩放 = 静态 `Vector3.one` × 布局 `+0x78(m_scale)`（我方）/ `GetCardInHandScale()`（敌方）；时长 = `matchData(+0xa8).+0x49 ? TimeToDrawCard(isPlayer) : 0`；最后 `DrawCard(pos, scale, dur)` + `SetStateAsMulligan(false)` | `Battle/MulliganPanel.cs:673-700`（`RowWorldPosAt` / `RowLocalAt`）、`:628/640`（`CardWidthPx 292` / `RowSpacingPx 414`） | **B** —— **判据已钉死且发现数值差**，见下「换牌行几何」 |
| 1331 | `PlaySentinelAnims(battleAnims)` | `PlayerHand__PlaySentinelAnims.c:32-152`：① `GetUnitsInPlayPooledList` 里带 sentinel 特性的单位 → `LogWarning` + `PlayTriggerAnim()`；② `!GameStaticData._disableParticles && battleAnims.sentinelDeckAnim(+0x30) != null` ⇒ 在 `DeckManager.transform.position` 处 `Instantiate` 并挂成牌库子件；③ `soundCollection.activateSentinel.Load()` → `Play2D` | **只在 `Battle/BattleSfx.cs:36` 出现过 `activateSentinel` 这个 cue 名；动画/特效那两半全仓 0 命中** | **C**（`P2`） |
| 1333 | `RemoveCard(card)` | `PlayerHand__RemoveCard.c:6-16`（全文 11 行）= `currentHand.Remove(card)`（裸摘、不重排） | `Hand/CardInteraction.cs:1386`（`_cards.Remove(card)`） | **A** |
| 1341 | `SetParentOfCard(card)` | `PlayerHand__SetParentOfCard.c:5-22` = `card.transform.SetParent(handAnchorLayout.transform, worldPositionStays: true)` | 结构上无此动作：卡视图**创建时**就挂在手牌父节点下、出/入手牌都不搬父（`Hand/CardInteraction.cs:1519-1524` 的 `VisibleCards` 只决定「谁参与布局」） | **B** —— 见 §⑥ 请求①（要调度台判「结构等价能否记 A」） |
| 1343 | `SetupCardInMulligan(card, option)` | `PlayerHand__SetupCardInMulligan.c:9-27`：`card.SetMulliganSortingLayer()`；`SetSortingOrder( MaxCardsInHand() − currentHand.Count + 1 )`；`card +0x260(mulliganOption) = option`（`+0x260` 名字 `dump.cs` 实读） | `Battle/MulliganPanel.cs`（层/序在 `QMulliganCard = 3007` 那一段） | **B**（转派） |
| 1344 | `ShowHandSize(bool showY)` | `PlayerHand__ShowHandSize.c:20-72`：`cardsInHandText(+0x30)==null ⇒ return`；`!CanDisplayDeckSize() ⇒ return`；`showY==0 ⇒ SetActive(false)`；`activeSelf ⇒ return`；否则 `SetActive(true)` + `text = Loc("Battle/HUD/CardsInHand") + ": " + currentHand.Count` | `Battle/BattleDriver.cs:30753`（`_handLabel.SetText(CountText(HandCountTerm, me.Hand.Count))`）+ `:16475`（可见性）+ `:27731-27752`（建件）+ `:17265`（`HandSizeVisibleForTest`）+ `Core/Loc.cs:1465-1466`（两条 `_DAT_` 常量已在 `stringliteral.json` 逐条读出） | **A** |
| 1346 | `StartMovingHandCard(card)` | `PlayerHand__StartMovingHandCard.c:5-25`（全文 25 行）= **纯工厂**：`new d__80{4__this, cardList=currentHand, cardToExclude=card}` + `StartCoroutine` | `Hand/CardInteraction.cs:871-952`（`PickUp`，含 `TweenPose` 放大 1.1 / `timeToScaleIntoMoving` 0.1 / `Relayout(false)`） | **B** —— 工厂本身等价；它起的协程内容 = 1307 行（B）。两条绑定 |
| 1347 | `StopLightPlayableHand()` | `PlayerHand__StopLightPlayableHand.c:15-29`：`currentHand` 每张 `ActivateCard(false)` | `Battle/BattleDriver.cs:26817-26846` 同一函数：`myTurn==false ⇒ playable=false`（全部 `Unplayable`） | **A** |

**换牌行几何（`MoveCardToMulliganPos` 的判据数值，交给调度台）**
原版换牌行用的是 `CardsHorizontalLayout` 实例 MB **4881**（`bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4881.json`，与选牌面板 4052 逐字段相同）：

| 字段 | 原版值 | 含义 |
|---|---|---|
| `m_scale` | **183.41** | 卡缩放；父链 scale ≈ 1（`Transform_1292` scale 1，唯一一个 0 缩放节点 = 面板开场动画的起始态）⇒ **卡宽 = 2.0927 × 183.41 = 383.8 px** |
| `m_betweenElementsSpacing` | **502.2** | 相邻中心距（同一单位 ⇒ px） |
| `m_maxLayoutSize` | **0.98** | 压缩上限（× 可见宽） |
| `m_numberOfCardsForMaxHeight` | 7 | 满弧张数 |
| `m_maxHeight` | 0.44 | 弧高上限 |
| `m_invertRotation` / `m_useRotation` | 0 / **0** | **不转**（这就是「换牌行是平的」的判据） |
| `m_verticalOffsetLookTo` | 0 | — |
| `useExtraSpaceOnSelectedCard` | 0 | 不让位 |

我们：`MulliganPanel.CardWidthPx = 292` · `RowSpacingPx = 414` ⇒ 卡宽 −24%、间距 −18%、**间距/卡宽比 1.418 vs 原版 1.308**。且原版**按 `m_maxLayoutSize 0.98 × 可见宽` 压缩**（4 张以上就开始压），我们是恒 414 不压。⇒ 这是一条**真实数值偏离**，落点在 `Battle/MulliganPanel.cs`（白名单外）。

### ②-5 `MinionManager` 12 条几何（**只交判据，⛔ 不动代码**）

字段名未逐条锚定（`dump.cs` 的 `MinionManager` 字段表没展开核），**下表用偏移表示，等接手时再钉名字**。

| 账行 | 方法 | 原版算什么（可复算） | 判据出处 |
|---|---|---|---|
| 1237 | `AdjustMinionBoardPos(Vector3)` | `ret = (p.x×+0x84, p.y×+0x88, p.z)` —— **只缩 x/y，z 原样** | `MinionManager__AdjustMinionBoardPos.c:11-18` |
| 1239 | `AreMinionsOutOfPos()` | 左/右两组各遍历：`pos = GetLeft/RightSlotPos(i) + Vector3.up×(+0x94)`，与单位 `localPosition` 比 **曼哈顿距离**，超阈值 ⇒ true。**阈值 = `DAT_1834b2bb0 = 0.2f`**（与本工程「长按 0.2 s」是同一个字面量，⛔ 别混用语义） | `MinionManager__AreMinionsOutOfPos.c:32-115` |
| 1251 | `GetDummyDistance(p1,p2)` | `|Δy| + |Δx|`（掩码 `DAT_1834b2e60 = 0x7FFFFFFF` = abs） | `MinionManager__GetDummyDistance.c:13-16` |
| 1256 | `GetLeftSlotPos(i)` | 从**左侧** `List<Vector2>` 取第 `i` 项：`x = *(base + 0x20 + i*8)`、`y = *(base + 0x24 + i*8)`；`i >= Count` ⇒ `LogError("<左越界>{i}")` 后**夹到 `Count−1`**（`Count−1 < 0` ⇒ 0） | `MinionManager__GetLeftSlotPos.c:21-80` |
| 1265 | `GetNextSlotPos(pos, nextSlotRequired)` | 在左/右两组里**按「空位」跳着数**：`+0x78` / `+0x70` 两张表非空 ⇒ 该槽被占、跳过；数到第 `nextSlotRequired` 个空位就返回 `GetRightSlotPos(n−1)` 或 `GetLeftSlotPos(~n)`；数不满 ⇒ `LogWarning/LogError`（带 `x,y / 需要数 / 右表数 / +0x68 数 / +0x60 数` 五段）**并回退 `GetRightSlotPos(−1)`（= 越界夹取）** | `MinionManager__GetNextSlotPos.c:40-183` |
| 1269 | `GetRightMostPos()` | **右侧**表最后一项 ⇒ `(x, y, z=0)`；**表为空 ⇒ `Vector3.zero`**（静态块 `+0x0`，不是 `(0,0,0)` 字面量） | `MinionManager__GetRightMostPos.c:14-56` |
| 1270 | `GetRightSlotPos(i)` | 同 1256，取 `+0x28` / `+0x38` 那一对表；越界 `LogError("<右越界>{i}")` + 夹取 | `MinionManager__GetRightSlotPos.c:21-80` |
| 1271 | `GetSlotPos(slotNum)` | **有符号槽号**：`slot >= 1 ⇒ GetRightSlotPos(slot−1)`；`slot <= −1 ⇒ GetLeftSlotPos(~slot)`（= `−slot−1`）；`slot == 0` ⇒ `~0 = −1` ⇒ 走越界夹取分支 | `MinionManager__GetSlotPos.c:7-16` |
| 1272 | `GetSlots(direction)` | 读到循环终止条件（未逐句）：按 `direction` 返回其中一组表 | `MinionManager__GetSlots.c:1-60` |
| 1274 | `HideCardShadow()` | = `SetCardShadow(Vector2.zero, false, false)` | `MinionManager__HideCardShadow.c:8-12` |
| 1285 | `ReassembleMinions(minionToSkipAnim)` | 见账（上一轮 B，本轮未重读） | `MinionManager__ReassembleMinions.c` |
| 1291 | `SetCardShadow(pos, showY, boardPosition)` | `shadow(+0x48).transform`：**`boardPosition==0` ⇒ `localPosition = (pos.x, pos.y, 0)`**；`==1` ⇒ 同样置位 **且** `rotation = Quaternion.Euler(pos.x, pos.y, 0)`（`Internal_FromEulerRad`，两角取自 pos） | `MinionManager__SetCardShadow.c:20-56` |
| 1292 | `SlotPosToBoardPos(targetPos)` | **`= targetPos + Vector3.up × (+0x94)`**（静态块 `+0x18` = up；x/z 加 `up.x/up.z = 0`） | `MinionManager__SlotPosToBoardPos.c:13-23` |

🔑 **「敌方槽位要不要镜像」的正解（顺带解决另一路正在复核的那条）**：`GetLeftSlotPos` / `GetRightSlotPos` / `GetRightMostPos` / `GetNextSlotPos` 里**每一处**都是
`lVar = (*(char*)(this + 0x40) == 0) ? *(this + 0x20/0x28) : *(this + 0x30/0x38);`
⇒ **`+0x40` 选的是「另一对数组」，不是「把坐标取反」**。也就是：`MinionManager` 里**并存四张槽位表**（左×两组、右×两组），
`+0x40` 二选一。⇒ **判据层面不存在「镜像」这一步**；「敌方那半」的值来自**另一份序列化表**（每个战场场景各一份）。
⚠️ 这只是 `MinionManager` 侧；`Board/**` 侧我们怎么摆是另一件事，本代理未碰。

### ②-6 13 场参照物取值（帧桥）

脚本（只读）：`/tmp/b8w/frame_bridge.py`、`frame_bridge2.py`、`frame_bridge3.py`（本轮临时件，未入仓）。
扫描对象：`d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena*/`（**13 个包全在，逐个扫**）。

| 量 | 13 场结果 | 出处 |
|---|---|---|
| `HandArea.localScale` | **13/13 = (108, 108, 108)** | 各场 `Transform/Transform_<n>.json`（arena1 = `Transform_1401.json`，父 `1296`） |
| `HandArea.localPosition` | 13/13 = (0,0,0) | 同上 |
| `HandAnchor.localPosition` | **13/13 = (0.125, 1.1, 0)** ⇒ **13.5 px / 118.8 px** | arena1 = `Transform_1455.json`（父 = `1401` = HandArea） |
| Canvas（`m_RenderMode = 1` ScreenSpaceCamera） | **13/13 `m_PlaneDistance = 100.0`**，`m_Camera` → 那颗 `fov = 40.0` 的相机 | arena1 = `Canvas/Canvas_2569.json` / `Canvas_2572.json` → `Camera_1462` fov 40 |
| CanvasScaler | 13/13 `m_UiScaleMode = 1`（ScaleWithScreenSize）· `m_ReferenceResolution = (1920,1080)` · `m_MatchWidthOrHeight = 0` | `MonoBehaviour_<n>.json`（arena1 = `MonoBehaviour_4731.json`） |
| **px / 世界单位** | `1080 / (2×100×tan(20°)) = 1080/72.794047 = `**`14.8364`** | 由上面两行**算**出来的，不是读出来的 |
| **帧桥 = HandArea × 1/(px/单位)** | `108 / 14.8364 = `**`7.2794`** | — |
| 敌方链 | `HandAnchor`(敌) `y = 0.46` ⇒ `0.46 × 108 = 49.68 px`；父 `1295`(HandArea, scale 108) → `2798`(EnemyArea, sizeDelta 100×100, ap 0,0) → `2762`(UpperAnchor, 100×100) ⇒ 距顶 `50 − 49.68 = 0.32 px` | arena1 `Transform_1257` / `_1295` / `RectTransform_2798` / `_2762` |

**⇒ 结论：帧桥是「场景无关」的常量，13 场逐值相同；`HandLayout.cs:443` 的 `OriginalPxPerUnit = 14.835f` 与实算 `14.8364` 差 `0.0094%`（165 px 的卡差 0.016 px）—— 可忽略，但**如实记**（`HandLayout.cs:453-457` 的 7.28 与实算 7.2794 一致到 4 位）。**

### ②-7 `ShowCardBeingDrawn` 与 `DrawCard` 的翻面条件相反 —— 裁定建议

**两条真体（🥈 ILSpy 是最清楚的一份；🥇 Ghidra 各自互证）**

```
// 🥇 CardScript__ShowCardBeingDrawn.c:10-17  ≡ 🥈 CardScript.cs:9727-9740
cardState(+0x228) = inHandMoving(9);
CardUI.SetSortingLayer("HandMoving");
if (!isPlayer) TurnCardAround(stepTime, delay: stepTime);      // ← 敌方翻

// 🥇 CardScript__DrawCard.c:25-130  ≡ 🥈 CardScript.cs:8918-8966
if ((cardState & 0xFFFFFFEF) != 0) { LogError("Trying to draw card {名} but it is already in {state}"); return; }
SoundManager.Play2D(soundCollection.DrawCardFromDeck);
SetStateInHand();                                              // cardState = inHand(1)
if (!isPlayer) CardUI.SetObjectVisibility(cardState);           // ← 敌方不翻
else           TurnCardAround(timeToDraw, delay: 0f);           // ← 我方翻
transform.DOScale(newScale, timeToDraw).SetEase(OutQuart);
if (!isPlayer) transform.DORotate(Vector3.zero, timeToDraw);    // ★ 无 SetEase（吃默认）
transform.DOMove(pos, timeToDraw).SetEase(OutQuart);

// 🥇 CardScript__TurnCardAround.c:29-37 ≡ 🥈 CardScript.cs:8972-8997
DORotate((0, 90, 0), timeToDraw × 0.25).SetEase(Linear).SetDelay(delay)
    .OnComplete(⇒ FinishCardDrawRotation(timeToDraw));
// 🥇 CardScript__FinishCardDrawRotation.c:13-24
FinishCardDrawRotation(t): CardUI.SetObjectVisibility(cardState); DORotate(Vector3.zero, t × 0.25).SetEase(Linear);
```

**为什么方向相反而不是 bug**
`+0x40` = `EntityScript.isPlayer`（`dump.cs:21973` 实读 `private bool <isPlayer>k__BackingField; // 0x40`）。同一个字段在两条路上回答的是**两个不同的问题**：

| 入口 | 之后这张牌要朝哪 | 「要不要翻」的真判据 | 我方 | 敌方 |
|---|---|---|---|---|
| `DrawCard` | 落在**持有者的手牌区**（我方手牌对玩家是**正面**；敌方手牌对玩家是**背面**） | 它现在对玩家正面吗？ | 牌库出来是背面 ⇒ **翻 180°** | 对玩家仍是背面、且玩家不该看到正面 ⇒ **只 `SetObjectVisibility`** |
| `ShowCardBeingDrawn` | **亮出来给玩家看**（公共展示位） | 它现在对玩家正面吗？ | 本来正面 ⇒ **不翻** | 本来背面 ⇒ **翻 180°** |

**调用者坐实（两条链互不相干、谁也不调谁）**
- `DrawCard` ← `PlayerHand.AddDrawnCardToHand` / `AddActiveTrapToHand` / `MoveCardToMulliganPos`（抽牌 / 换牌入位）。
- `ShowCardBeingDrawn` ← **`BattleManager._ResolveDiscardFromHand`（弃牌）** 与 **`_ResolveStealFromDeck`（从牌库偷牌）** 两个状态机的 `MoveNext`（判据：`decomp_full/` 里只有这两个文件引用它）。
- **没有先后关系**（不是同一个动作的两步）。

**裁定建议（给调度台）**
1. **两处都必须照抄各自的方向**，⛔ **不许「统一」**（统一到任一方都会让另一半的翻面错 180°）。
2. 我们目前只实现了 `DrawCard` 那一路，而且**方向是对的**：`CardFeel.DealInPose`（我方 → 含 `TurnCardAround` 两段 90°）· `CardFeel.DealInQuad`（敌方 → 只位移/缩放/淡入）—— 判据 `CardFeel.cs:3197-3230` 那一整段 `A1213/W64/B13` 长注已经记过。
3. `ShowCardBeingDrawn`（弃牌 / 偷牌揭示）在我们这边**整条路径都没有**（不只是翻面：`cardState=inHandMoving` 的档、`SetSortingLayer("HandMoving")`、`delay = stepTime` 的延迟翻，三样都没有）。⇒ **建议把账 1047 从 B 改判 C**（缺口），而不是 A。
4. 附一条**顺带发现**：`DrawCard` 敌方那一支的 `DORotate(Vector3.zero, timeToDraw)` **没写 `SetEase`**（吃 `DOTween.defaultEaseType = OutQuad`），而 `DOScale`/`DOMove` 是 `OutQuart`。我们 `DealInQuad` 若把三条统一成同一个 ease，就错了 —— 见 §⑤-2。

---

## ③ 视图层观测量（本轮的「G3」口径 + 怎么读）

简报允许表现层用「视图层可观测状态」当可观察量。本轮用到的三个量、读法与两侧期望值：

| # | 观测量 | 怎么读（可复核） | 原版侧期望值（推导） | 我们侧期望值（代码） |
|---|---|---|---|---|
| 1 | **近战收招后的静止位姿** | `CardView.transform.localPosition` / `localEulerAngles.z` / `localScale.x`（出招前后各读一次） | `localPosition == originalLocalPosInPlay`（+0x2C4，段 8 的目标）；角度回 `identity`；`localScale` = 段 2 那个 `GetScale`（= 静止缩放，因卡回自家那一行） | `MeleeAttack` 段 4 `DOLocalMove(home, …)` / 段 5 `DORotateQuaternion(homeRot, …)`；缩放由 `HookDepthScale.OnKill` 复位 `baseScale` ⇒ **同值** |
| 2 | **近战整条序列时长** | 序列的 `Duration()`（`CardFeel.MeleeAttack` 返回值）或出招→收招的时间戳差 | `attackStepTime(0.1) + AppendInterval(0.1) + 2×attackStepTime(0.2) = `**`0.4 s`** | `AttackStepTime + AttackHoldTime + AttackReturnTime = 0.1+0.1+0.2 = `**`0.4 s`** |
| 3 | **手牌数文本与可见性** | `BattleDriver.MyHandText`（`_handLabel.Text`）与 `HandSizeVisibleForTest` | `Loc("Battle/HUD/CardsInHand") + ": " + currentHand.Count`；`CanDisplayDeckSize()==false` ⇒ 整组 `SetActive(false)` | `BattleDriver.cs:30753` 与 `:16475`（同构；已有 `BattleScene.Run` 断言 `:35415-35492`） |
| 4 | **手牌可打描边** | 每张手牌 `CardHighlight` 状态（`Normal` = 有描边 / `Unplayable` = 无描边）+ `CardView.Grey`（恒 0） | 逐卡 `CanPlayCard` 为真者亮 | `RefreshHandPlayable`（`BattleDriver.cs:26817-26846`） |
| 5 | **起拖那一帧的邻牌中心距** | 起拖按下后**同一帧**读 `HandLayout._lastPose` 里相邻两张的 `pos.x` 差 | `SpacingFor(N−1)`（无洞） | `SpacingFor(N)`（有洞）⇒ 8 张时差 **14.3%** |

⚠️ 上面 1/2/5 需要**跑一次宿主**才读得到（`BattleScene.Run`）。本轮**未跑**（子代理不跑 Unity / 不自证）⇒ 见 §⑥ 请求②。

---

## ④ 没查清的部分（如实列）

1. **`AttackStepTime` 的回放压缩常数 `DAT_1834b2e8c` 未解**（`CardScript__get_AttackStepTime.c:12`）。⇒ 回放快进那一档的时长比例算不出来；我们不实现回放快进，所以现在无影响，但**判据缺一格**。
2. **`PlayerHand +0x40`/`+0x68`/`+0x70`/`+0x78` 四个字段名没锚定**（`MinionManager` 那 12 条几何里用到 `+0x70/+0x78` 判「槽被占」、`+0x60/+0x68` 只出现在日志里）。判据方向清楚（两张表 = 占用表），**名字没钉**。
3. **`MinionManager` 的 `+0x84/+0x88/+0x94/+0x64` 等字段名未钉**（同上）。数值用法已给出，名字待接手时补。
4. **`GetSlots(direction)` / `ReassembleMinions` 未逐句重读**（上一轮已记 B；本轮时间给了前面 7 项）。
5. **换牌行（`MoveCardToMulliganPos`）的父链里那个 `localScale=(0,0,0)` 节点**：我按「换牌面板开场动画的起始态、运行时会被动画拉到 1」处理（否则整块不可见）。⚠️ **这是推断，没读动画曲线验证**；若它真不是被动画拉起来的，那 183.41/502.2 的 px 换算要重算。
6. **`Battle/MulliganPanel.cs` 整份未逐行核**（白名单外）。上面关于换牌行的结论只到「判据数值 vs 我们的常量」这一层。
7. **`b__1` 与 `DOMove.OnComplete` 里那个 inline lambda 是两份**：Ghidra 把两者折叠成一个符号；ILSpy 显示 `Sequence.OnUpdate` 用 `b__0`、`DOMove.OnComplete` 用**一个同体的 inline lambda**（`CardScript.cs:664-680`）。⇒ 「原版只通知一次 `ReceiveAttackAnim`」这个结论我按 inline lambda 只有一份来判；**若 Ghidra 的折叠暗示还有第三处调用，我没有证据**。

---

## ⑤ 顺手发现（不属于本代理的活，按铁律 13·4⑧ 报）

1. **上一波把 `DisplayHandMinusCard` 的 `cardToExclude` 判成「没用上」**（因为 Ghidra 把 `op_Inequality` 的第二实参丢了）。**这是「Ghidra 丢实参」的第 N 个实例**（本报告里另有 `TurnCardAround` 被传 4 个实参给 3 参函数那一次）。⇒ 建议在 `资料/已知的坑.md` 里补一条：**「Ghidra 的 `op_Inequality/op_Equality` 单参调用 = 参数被吞，必须回 ILSpy 核」**。
2. **`CardScript.DrawCard` 敌方那一支的 `DORotate(Vector3.zero, timeToDraw)` 没有 `SetEase`**（吃 `defaultEaseType = 6 OutQuad`），而同方法的 `DOScale` / `DOMove` 都是 `OutQuart`。⇒ 若 `CardFeel.DealInQuad` 把三条统一成同一个 ease，就错了（本代理未核 `DealInQuad` 的 ease —— 它在 `CardFeel.cs` 白名单内但属另一条账行）。
3. **`ShowCardBeingDrawn` 里 `TurnCardAround(stepTime, stepTime)` 的第二个实参是 `delay`，不是「第二段时长」**。因为原版 `TurnCardAround(timeToDraw, delay)` 每段固定 `timeToDraw × 0.25`。⇒ 弃牌/偷牌揭示那一路是「**先等一个 stepTime 再翻**」（不是「翻得更慢」）。这一条与上一波账里「两参都是 stepTime」的措辞不一致，容易被后人读反。
4. **换牌面板与选牌面板共用同一份 `CardsHorizontalLayout` 配置**（MB 4052 与 4881 逐字段相同：`m_scale 183.41` / `m_betweenElementsSpacing 502.2` / `m_useRotation 0`）。⇒ 「换牌行是平的」这件事**有序列化判据**（`m_useRotation=0`），不必靠截图。
5. **`CardsHorizontalLayout.GetCardsInHandNumberWhereDistanceBetweenThemMustBeReduced` 在本 build 是【零调用者】**（`decomp_full` + ILSpy 全树只有定义、没有任何调用点）。它的返回值语义已读出（= 第一个让 `n×spacing > m_maxLayoutSize` 的 `n`，跑满回 0），但**没有观察面** ⇒ 账里那一行（`菜单_外壳` 63 行）保持 B，理由换成「死码、无观察面」，不再是「阈值未逐字段比」。
6. **`PlayerHand.HideHand` 与 `PlaySentinelAnims` 全仓 0 命中** ⇒ 两条真缺口（已转 C）。`PlaySentinelAnims` 只残留一个 cue 名 `activateSentinel` 在 `BattleSfx.cs:36` 的注释里 —— **别拿它当「做了」的证据**。

---

## ⑥ 给调度台的请求

1. **裁定 `PlayerHand.SetParentOfCard` 能否记 A**：原版是「搬父节点」，我们的结构是「卡视图恒挂在手牌层、永不搬父」⇒ **行为等价但没有一处『执行搬父』的落点**。B2A 口径 G1 要求落点真的执行判据那段 ⇒ 我按规则保持 B。若调度台认「结构等价」可记 A，请直接改。
2. **要跑一次 `BattleScene.Run`（`-executeMethod BattleScene.Run`）** 来闭合三处视图层观测量（§③ 的 1/2/5），并顺手看 §③-5 那个「起拖第一帧间距」的 14.3% 差是不是肉眼可辨。⚠️ 该宿主会重写 `Scenes/Battle.unity`，跑前请留副本（`CLAUDE.md` §二 铁律）。
3. **转派：`Battle/MulliganPanel.cs`**（不在本代理白名单）——
   a) 换牌行几何按 §②-4 的「换牌行几何」表逐值核：`卡宽 383.8 / 间距 502.2 / maxLayoutSize 0.98 压缩 / maxHeight 0.44 / 无旋转`；现状 `292 / 414 / 恒不压 / 平`；
   b) `SetupCardInMulligan` 的 `SetSortingOrder(MaxCardsInHand − handCount + 1)` 与 `mulliganOption`；
   c) `FinishMulligan` / `FinishMulliganFinalPhase` 两条淡出链。
4. **转派：教程线** —— `HandCardMovedToNewIndex` 末尾的 `manager.CheckForTutorialPointer()`（我们 0 处）。这一句落在就绪后，账 1320 可以升 A。
5. **转派：`Battle/BattleDriver.cs`（弃牌 / 偷牌揭示）** —— `ShowCardBeingDrawn` 那一整条（`inHandMoving` 档 + `SetSortingLayer("HandMoving")` + `delay=stepTime` 的翻面）在弃牌/偷牌演出里**没有对应件**。建议账 1047 判 **C**。
6. **账漂移预期**：本轮**没改任何 `.cs`** ⇒ `check_ledger_points` 的 A 行落点**不应漂移**（我引用的行号全部是**现读**的、且没动那三个文件）。若合并后报漂移，说明是别的批次在写这三个文件。
7. **`PaintPoint` 提醒**：`卡片面_渲染.tsv` 与 `战斗界面_HUD.tsv` 里有若干行也引 `CardFeel.cs` / `HandLayout.cs` 的行号 —— 我**没改这两个文件**，所以它们不受影响。

---

## ⑦ 改了哪些文件 / 新增行数

| 文件 | 改动 |
|---|---|
| `账/_parts/B8_W.tsv` | **新增**（19 行 + 表头，11 列，LF） |
| 其余 | **零** —— 本轮**没有修改任何 `.cs`**，因此**不需要跑 typecheck**（无 `.cs` 变更）；为免责仍跑了一次，见 §⑦-1 |

### ⑦-1 自查结果（实跑）

```
$ TMPDIR=/tmp/wf_b8w bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```
（本轮未改 `.cs`，这是「没把别人正在写的东西带脏」的旁证。）

```
$ python -I -X utf8 工具/check_a_vs_diff.py d:/4/Unity/资料/完全复刻/账/_parts/B8_W.tsv
B8_W | （A 共 7 条，无命中）
合计 0 条（强 0 · 弱 0）
$ python -I -X utf8 工具/verify_annot.py
同名 721 · 异名已注解 2332 · 缺注解 0
```
补丁本体：20 行（表头 + 19）· **纯 LF（CRLF 0 / 裸 CR 0）** · 逐行 11 字段（0 行例外）。

---

## ⑧ 本轮账目数字

- 触碰行数：**19**（`战斗逻辑驱动` 18 + `菜单_外壳` 1）
- **B → A：7 条**（`AttackMeleeAnim` · `GetEffectAnchor` · `HandCardReleased` · `RemoveCard` · `LightPlayableHand` · `StopLightPlayableHand` · `ShowHandSize`）
- **B → C：2 条**（`HideHand` · `PlaySentinelAnims`）
- **保持 B 但补全判据/真落点：10 条**（`DisplayHandMinusCard` · `FinishMulligan` · `FinishMulliganFinalPhase` · `HandCardMovedToNewIndex` · `MoveCardToMulliganPos` · `SetupCardInMulligan` · `SetParentOfCard` · `StartMovingHandCard` · `ShowCardBeingDrawn` · `GetCardsInHandNumberWhereDistanceBetweenThemMustBeReduced`）
- 未触碰：`MinionManager` 12 条（简报要求只交判据）
