# J1 · 判据表 —— 战斗时间线 + 战斗 HUD（13 条）

> 只读代理 `J1`，2026-10-25。**未改任何代码 / 数据 / 台账**（唯一可写 = 本文件）。未跑 Unity、未动 git。
> 判据优先级：① `d:/2/tools/decomp_full/` → ② 解包资产字段 → ③ 运行时 dump → ④ 说明书 / 2D 全树 → ⑤ 已有报告（AU1/AU2）。
> 先读了 `普查产出_1025/AU1_…md` 与 `AU2_…md`，不重复它们已闭合的部分；**下面 3 处直接订正了 AU1 的读数**（已在对应条目里写明）。
>
> 本轮新解出的**通用钥匙**（下面多条共用）：
> - `BattleAction` 的字段偏移表（`BattleAction.cs` 声明序 + `BattleManager._ResolvePlayCardFromHand_d__447` 里读 `+0x8 / +0x3c / +0x40` 三处互证）：`+0x08 actingCard` · `+0x3c fromEnemyHand` · `+0x40 actionValueUnits`。
>   ⇒ AU1 记的「`PlayerAction+0x4c`」**根本不是 `PlayerAction` 的字段**，是 `BattleAction.fromEnemyHand`（见 §2）。
> - `VarsGlobal` 字段偏移 = **0x18 + 4×序号**（`cardInCemeteryPos` 是 Vector2，占 8 字节，其后 +4）。三处独立互证：
>   `+0x70 = cardDrawnScale 1.4`（`PlayerHand._AddDrawnCardToHand_d__37:56`）· `+0xb0 = timeToDissolveCard 0.2`（`DissolveWholeCard(card, vars+0xb0)`）· `+0x78 = timeToScaleIntoMoving 0.1`（`MinionPlayedIntoField(delay)`）。
>   数值正本 = `d:/2/tools/assetripper_out/ExportedProject/Assets/MonoBehaviour/GlobalVariables.asset`。
> - `MinionArea`（=`MinionManager` 的 transform，卡在场上的**父节点**）在 **13 个战场**里 `lossyScale` 实测 **全 = 1.0**（脚本逐场走父链，本文件 §8 有复跑命令）。

---

### 1 `play` 桶相对 `Deploy` 的先后 —— 正常单位 = **1.30 s**（不是 0.4 / 0.7 / 1.05 中的任何一个）
- **判据结论**：原版 `play` 桶（`trigger = 0` = `ThisCardPlayed`）的广播点是 `BattleManagerSupport.BroadcastCardPlayed`，**唯一调用点** `BattleManager._ResolvePlayCardFromHand_d__447__MoveNext.c:1426` —— 它在 state 0xb，**前面整条登场链全部 `yield` 完了**。手牌打出一个**普通单位**（无 teleport）的实测链：
  1. `MinionManager.PlayMinionFromHand`（`MinionManager__PlayMinionFromHand.c:11-23`，飞行 tween = `minionToConversionPointTime`，场景实读 **0.3**，`bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4372.json`）
  2. `WaitForSeconds(GetMinionConversionTime(card))` = `GetHandToBoardAnimEventTime()`(**0.55**) + `minionToConversionPointTime`(**0.3**) = **0.85** —— `MinionManager__GetMinionConversionTime.c:5-12` + `CardScript__GetHandToBoardAnimEventTime.c`（返回 clip 第一条 AnimationEvent 的 `time`）
  3. `yield MinionPlayedIntoField(...)` = `delay(=vars.timeToScaleIntoMoving 0.1)` + `card.timeBeforeLand 0.05` + `card.timeToLand 0.2` + `get_AttackStepTime() 0.1` = **0.45**（`CardScript._MinionPlayedIntoField_d__314__MoveNext.c` state 0/1/2/3；两个卡的字段值 = `bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/MonoBehaviour_1744609728290659264.json` 的 `timeToLand 0.2 / timeBeforeLand 0.05 / attackStepTime 0.1`，`+0x150/+0x154` 的 ctor 默认 `0x3e4ccccd/0x3d4ccccd` 同值）
  4. → `ResolveCardPlayed`（自己那一份）+ `BroadcastCardPlayed`（同 state，同步）
  ⇒ **合计 0.85 + 0.45 = 1.30 s**。
  ⚠️ **我们的 `DeployHoldOf(HandPlayed) = 0.4` 是取错了三段**：它 = `MinionPlayedIntoField` 尾那 0.1 + **teleport 支才有的** `WaitForSeconds(0.3)`（`state 4`），并且**整条省掉了 0.85 的 GetMinionConversionTime**。
- **状态 → 参数**（`Deploy` 那一刻 → `play` 桶起播）：

  | 情况 | 偏移 | 依据 |
  |---|---|---|
  | 单位 · 手牌打出（普通） | **1.30 s** | 上面的链 |
  | 单位 · 手牌打出 + `teleport`（`HasDefaultTrait(0x4ce)`） | `ResolveTraitAnim(0x4ce)`（同族 `TriggerHold+TriggerLen` = 0.3+0.2，**待核**）+ 0.45 + 0.3 ≈ **1.25 s** | `_d__447` state 2→3→4→5 |
  | 单位 · 自动从手牌打出（`DeploySource.AutoPlayFromHand`） | **不会发生** —— 原版这条路（`_ResolveCardAutoSummoned`）**不调 BroadcastCardPlayed** | `grep -rl BattleManagerSupport__BroadcastCardPlayed` 全库只有 `_d__447` 一个调用点 |
  | 单位 · 效果召唤 / 复活（`Summon` / `Reanimate`） | **不会发生**（同上，`_ResolveSummonUnit` 不广播） | 同上 |
  | 战术 / 法术（非单位、非有目标） | **0**（`_d__447:641 goto LAB_1809bdcfc` 直落广播块） | `_d__447:470-641` |
  | 战术 · `targeted` 且 `fromEnemyHand` | **0.5 s**（`GetTargettingTime()` = `vars.targettingAnimTime`） | `_d__447:477-491` + `BattleManager__GetTargettingTime.c` |

- **还差什么**：无（`teleport` 那一格的 `ResolveTraitAnim` 时长要靠 `_ResolveTriggerTraitAnim_d__476` 逐 state 核，但它**只影响带 teleport 词条的卡**，池内占比极小）。落点建议：`EventTiming` 给 `Play` 一个「按来源分档」的时长，⛔ 别把 `DeployHoldOf` 直接改成 1.30（那是两件事，见 §5 的模型说明）。

---

### 2 `EvtKind.Play` 的「居中展示」是否进时间线 —— **进，但只对「对手打出的牌」和「回放局」**
- **判据结论**：**AU1 的「关键未知 = `PlayerAction + 0x4c`」是错的对象。** `_ResolvePlayCardFromHand(ref BattleAction battleAction)` 的入参是 **`BattleAction`（256 字节值类型）**，协程里 `*plVar1 + 0x4c` 落到的就是 `BattleAction + 0x3c` = **`fromEnemyHand`**（`BattleAction.cs:13`；互证：`*plVar1 + 0x18` = `BattleAction + 0x08` = `actingCard`、`+0x50` = `+0x40` = `actionValueUnits` 被当落格号传给 `PlayMinionFromHand`）。
  - `BattleAction.fromEnemyHand == true` ⇒ `CardScript.PlayEnemyCard(card, timeToPlayEnemyCard)`（`_d__447:228-247`）
  - 否则**还要** `matchType == 0xA0` ⇒ `CardScript.PlayPlayerCard(...)`（`_d__447:249-269`）；`0xA0 = 160 = MatchType.Replay`（`MatchType.cs`）
  - 两个都不满足 ⇒ `goto LAB_1809bc6fe` ⇒ **我方普通对局里自己打出的牌根本没有居中展示**。
  - `timeToPlayEnemyCard` = `ReplaySkipAnims() ? 0.2 : 0.6`（`BattleManager__get_timeToPlayEnemyCard.c`；两个常量 `0x1834b2bb0=0.2 / 0x1834b3180=0.6` 实读）。
  - `PlayEnemyCard` / `PlayPlayerCard` 是**同一条**协程（两份文件逐字比对，只有方法名与日志串不同）：`ShowCardInCenter(card, t)` → `WaitForSeconds(它的返回值)` → `MoveIntoFieldFromHand()` → `WaitForSeconds(t)`。
  - `ShowCardInCenter`（`CardScript__ShowCardInCenter.c`）= `t`（`card+0x40 != 0`，**我方卡**）或 `t + vars.timeToScaleIntoMoving(0.1)`（`card+0x40 == 0`，**敌方卡**，多一个 `TurnCardAround` + 1.5 倍放大）。`card+0x40` 的语义由 `_ResolveCardAutoSummoned:101-128` 直接钉死：`card+0x40 == 0 ⇒ PlayEnemyCard`（敌方）。
  - ⇒ AU1 的 **0.24 / 0.34 只属于「效果召唤那条链」**（`ResolveCardAutoSummoned` 传的是 `timeToPlayEnemyCard / 5` = 0.12，`_ResolveCardAutoSummoned_d__511:108-128`，除数 `DAT_1834b2df4 = 5`），**不是手牌打出那一档**。手牌打出的同一算式是 0.6。
- **状态 → 参数**：

  | 情况 | 居中展示时长 | 算式 |
  |---|---|---|
  | 对手从手牌打出 · 单位 · 普通局 | **1.30 s** | 0.6 + 0.1（敌卡）+ 0.6 |
  | 回放局（`matchType 0xA0`）· 我方单位 | **1.20 s** | 0.6 + 0.6 |
  | 对手从手牌打出 · 战术/法术 | `DisplayEnemyCardInCenter(card, 0.6×0.6=0.36, 0.6)`（**这一段没展开**，见「还差」） | `_PlayEnemyCard:44-47` |
  | 效果召唤 · 手里那张牌被召唤 · 我方 | **0.24 s** | 0.12 + 0.12 |
  | 效果召唤 · 敌方 | **0.34 s** | 0.12 + 0.1 + 0.12 |
  | 我方普通对局自己打牌 | **0**（没有居中展示） | `_d__447:271 goto` |
  | `ReplaySkipAnims()` 为真 | 上面各值把 0.6 换成 0.2 | `get_timeToPlayEnemyCard` |

- **还差什么**：**非单位卡**那一支的 `CardScript._DisplayEnemyCardInCenter_d__263` 逐 state 没展开（`0.36` 是 `t × DAT_1834b3180(0.6)`，它的返回值/等待链要再读一次）。**要不要真机**：不需要（纯静态可闭合）。

---

### 3 回手 vs 回牌库 —— 回手 **0.75**（我们的值对）；回**牌库 = 0.95**（AU1 记的 0.85 少了 0.1）
- **判据结论**：
  - **回手**：`CardScript.TransformUnitInPlayToCard` 等 `GetHandToBoardAnimEventTime()` = **0.55**（`CardScript._TransformUnitInPlayToCard_d__256__MoveNext.c:80-85`；clip `Card Hand To Board` 唯一事件 `CardHandToBoardAnimationFinished @ 0.55`）＋ `PlayerHand.AddCardNotDrawnToHand` 等 `VarsGlobal.timeToPositionCard` = **0.2**（`GlobalVariables.asset` 实读）⇒ **0.75**。（`teleport` 词条再加 0.55 ⇒ 1.30）
  - **回牌库**：同一段 0.55 ＋ `CardScript.MoveToDeck(card, 1, 0)` **返回 0.4**（不是 0.3）⇒ **0.95**。
    `CardScript__MoveToDeck.c` 的返回值 = `fVar14 + fVar19`：`fVar19 = DAT_1834b2dc8 = **0.3**`（移动/翻转那一段），`fVar14 = vars.timeToScaleIntoMoving = **0.1**`（缩放那一段）—— 两段**是相加的**，尾段整体延迟 0.3 再跑 0.1。
    ⚠️ **这 0.1 就是 Q3 里那句「差的那 0.1 s 是哪一位事件」**：它是 `MoveToDeck` 里 `DOScale / DOLocalRotateQuaternion / DOMove` 的**时长参数**（`vars+0x78`），不是一条独立事件。
    ⚠️ 两个例外会把 0.3 归零（此时返回 **0.1** ⇒ 总 0.65）：`BattleCardUI.ShouldShowCardback(card)` 为真，或 **`card+0x54 != 0`**（这个 bool 没定名，疑 face-down/伏击，**待核**）。
- **状态 → 参数**：

  | 情况 | 时长 |
  |---|---|
  | 回手（`Slot >= 0`，无 teleport） | **0.75** |
  | 回手 + `HasDefaultTrait(0x4ce)` | **1.30** |
  | 回牌库 / 牌库顶（无 teleport，正常正面卡） | **0.95** |
  | 回牌库 + teleport | **1.50** |
  | 回牌库 · 卡背朝上（`ShouldShowCardback` 或 `card+0x54`） | **0.65** |

- **还差什么**：`card+0x54` 的字段名（要 `dump.cs` 里 `CardScript` 的字段序 + 0x54 对齐；本轮没做）。落点：`EventTiming.ReturnHoldOf` 现在对 `Slot>=0` 一律给 0.75 ⇒ **回牌库那一路要 +0.2**，但需要事件带「目的地」（`RuleEngine` 侧，`op.Dest`）。

---

### 4 `Slot < 0` 两档
- **判据结论**：
  - **(a) 墓地 → 手牌 / 牌库顶 / 牌库**（我们的 `EffectResolver.cs:6180`）：原版 = `BattleManager.ResolveCopyUnitInCemetery` → `CardScript.CreateCardInGame(card, …, VarsGlobal.timeToDrawPlayerCard = 0.3)` 并 `yield`（`BattleManager._ResolveCopyUnitInCemetery_d__541__MoveNext.c:125` + `CardScript._CreateCardInGame_d__250__MoveNext.c:78-84`；`vars+0xa8 = timeToDrawPlayerCard 0.3`，按 0x18 基准算并已被 `DissolveWholeCard(vars+0xb0=0.2)` 交叉验证）⇒ **0.3 s**。整条协程**只有这一个等待**，之后再无别的拍。
  - **(b) `ephemeral` 移出**（我们的 `RuleCore.cs:2743`）：**判据为空**。全量反编译 `grep -ril ephemeral` = 20 个文件，战斗侧只有 `BattleCardUI__ShowEphemeral.c`（在卡位置上 `Instantiate` 一个粒子，**不等待、不协程**）与 `CardScript__ActivateCard.c:140`（激活时调它）；**没有**「回合末把临时卡移出」的原版实现可对 ⇒ 这一档**保持 0**，按铁律 11 例外①（原版查不到）。
  - **另**：`EvtKind.Play` + `Slot = -1`（无目标战术卡）不是「两档」之一，它的时间线归属见 §2 表的「战术 / 法术」行 = **0**（除 `targeted + fromEnemyHand` 的 0.5）。
- **状态 → 参数**：只覆盖 (a)(b) 两种（`Return` 且 `Slot < 0`）：(a) 0.3 / (b) 0。
- **还差什么**：(b) 无判据可补（原版没有这条链）；若日后再拿到原版录像，看「临时卡消失到下一拍之间有没有空档」即可定性。

---

### 5 `Hit` 的两个例外 —— **Vulnerable(7)/VulnerableCombat(8) 多 0.4 s；`doItFast` 例外本 build 不可达**
- **判据结论**（`CardScript._ReceiveDamage_d__381__MoveNext.c`）：
  - 主体：`if (doItFast == 0) WaitForSeconds(1.0)`（`:340-346`，`0x1834b2bb8` 实读 1.0）⇒ 一拍 1.0 s（我们 `HitWaitSeconds = 1.0` ✅）。
  - **例外①（真例外）= 伤害类型 7 / 8**：`GameStaticData.IsVulnerableDamage(t)` = `(t-7) < 2`（`GameStaticData__IsVulnerableDamage.c`，枚举名 `DamageType.cs: Vulnerable=7 / VulnerableCombat=8`）⇒ 先 `WaitForSeconds(0.4)`（`:291-303`，`0x1834b3158` 实读 0.4）**再**走那 1.0 ⇒ **1.4 s**。
    🔴 **订正 AU1 #16**：AU1 写「那条路**没有这道门**」—— **错**。0.4 之后 `state 2 → LAB_18061493b → LAB_180614944`，尾部同一个 `if (doItFast==0) WaitForSeconds(1.0)` **照样执行** ⇒ 是 **0.4 + 1.0**，不是「绕过」。
  - **例外②（`doItFast`）= 本 build 恒不通**：`CardScript.ReceiveDamage(deathType, damageInfo, bool doItFast=false)`（stub `CardScript.cs:2441`）。全库 6 个调用文件逐个看，**每个调用点第 3 个实参都是 0**（`_ResolveAttack` 5 处、`_ResolveMultiDamage*` 2 处、`_ResolveTraitAdjacentDamage`、`_ResolveTraitStompDamage`）⇒ 那个「AoE 快进」分支**永远不会走**。
- **状态 → 参数**：

  | 情况 | 一拍时长 |
  |---|---|
  | 普通伤害（类型 ≠ 7/8） | **1.0 s** |
  | `DamageType.Vulnerable(7)` 或 `VulnerableCombat(8)` | **1.4 s** |
  | `doItFast = true` | 不可达（本 build 全为 false） |

- **还差什么**：无。**要不要真机**：不要。

---

### 6 `SW21` 的替代能力时间线 —— **0.2 s（普通档 0.4）**，数据已在仓库里，缺的只是「事件带哪一份」
- **判据结论**：`RawCardScript.GetActiveAbility`：`hasAlternativeAbility && BoardAnalysis.MeetsCriteria(normalAbilityBoardCriteria)` **成立** ⇒ 播 `activeAbility`；**不成立** ⇒ 落 `alternativeAbility`。
  - `d:/4/Unity/数据/游戏数据/ability_anim_durations.json` 里 SW21 一条**已经同时记了两支**（本轮现读）：
    `active = 0.4`（`Cut Wulfen` 0.2 + `Cut Wulfen Reverse` 0.2，串行）· **`alternative.seconds = 0.2`**（单条 `Cut Wulfen`，GUID `945014349c7e3cd4b93922bafe782387`）。
  - `normalAbilityBoardCriteria`（同一 json 的 `selectCriteriaDecoded`）：`playerTurn=Self(10)` · `triggerIf=EqualTo(1)` · `referenceValue=0` · 过滤位 `friendlyUnits + includeMinions` · `targetsAffected=allFiltered(10)` ⇒ **「轮到我 + 我不控制其它部队」时走普通档(0.4)，否则替代档(0.2)**。
- **状态 → 参数**：

  | 棋盘状态 | 能力动画时长 |
  |---|---|
  | 轮到这张卡的主人，且场上其它友方部队数 = 0 | **0.4 s** |
  | 其余（含「不是我的回合」） | **0.2 s** |

- **还差什么**：**不缺判据，缺两个落点**：① `RuleEngine`/事件要把「这次走的是哪一支」塞进 `BattleEvent`（现在 `CardDef.Ability` 是卡面文本解析出来的，与 SO 的 `activeAbility` 不是同一样东西）；② `Core/AbilityAnimDurations.cs` 要能按「分支」取值（现在只记 `active`；文件头 `:53-67` 已把 SW21 的条件写清，只是没接 alternative 那一格）。

---

### 7 近战段 2 的旋转基 —— **世界空间：`AngleAxis(25°, AngleAxis(90°, Vector3.up) × normalize(目标位置 − 自己位置))`**
- **判据结论**（`CardScript._AttackMeleeAnim_d__357__MoveNext.c:216-257`，逐行）：
  1. `axis0 = (0,1,0)` —— 来自 `*(longlong *)(DAT_1842da2a8 + 0xb8) + 0x18`，`DAT_1842da2a8` = **`UnityEngine.Vector3` 的 Il2CppClass**、`+0xb8` = `static_fields`、`+0x18` = `upVector`（字段表 `zero 0x0 / one 0xC / up 0x18 / down 0x24 / left 0x30 / right 0x3C`，已在 `资料/普查产出_1007/A111_A112_卡面两件.md:68` 与本仓多处互证）。
  2. `v1 = FUN_180534610(targetPos − selfPos)` = **归一化的世界方向**（`Vector3.normalized`，`:141-164` 同一函数在本段之前也用来算前冲方向）。
  3. `v2 = Quaternion.AngleAxis(90°, up) * v1`（**`Quaternion × Vector3` 重载，返回 Vector3** —— Ghidra 只给了 12 字节，所以 AU1 读成了「未解出的 12 字节」）。
  4. `q = Quaternion.AngleAxis(card.attackRotationAngle, v2)` → `DORotateQuaternion(transform, q, attackStepTime)`。
  - `card.attackRotationAngle` = **25.0**（`bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/MonoBehaviour_1744609728290659264.json`，字段序 `attackStepTime / attackPositionYOffset(0.5) / attackRotationAngle(25) / attackSequenceTimeScale(1.0) / playerAttackMargin(1.7) / enemyAttackMargin(1.0)` 与 `card+0x1a4/0x1a8/0x1ac/0x1b0/0x1b4` 一一对齐）。
  - **旋转基 = 世界 up + 世界空间「自己→目标」方向**；**不乘任何 home rotation、不乘目标旋转**（中性 = 单位四元数）。
- **状态 → 参数**：只有一种（不分敌方/我方；`card+0x40` 在这条式子里**不参与**，它参与的是同一段里的 `playerAttackMargin 1.7 / enemyAttackMargin 1.0`）。
- **还差什么**：无判据缺口。**我们现在的式子**是 `homeRot × Euler(0,90,0) × Euler(0,0,25)`（`CardFeel.cs:996-998`）—— 两者**不等价**：原版是「先转轴再转 25°」，我们是「先转 25° 再绕 Y 转 90°」。**若真机看觉得「卡面朝向偏 90°」，就是这一处**。

---

### 8 后坐的 `ToOurs` 语义 —— **`ToOurs` 是对的，不是缺陷**（AU1 的怀疑不成立）
- **判据结论**：
  - 原版：`CardScript__ReceiveAttackAnim.c:27-89` —— `punch = normalize(受害者世界位置 − 攻击者世界位置) × GetPushBackFactor(GetUnitSize(攻击者近战))`，然后 `transform.DOPunchPosition(punch, 0.4, vibrato 8, elasticity 0.3)`。`DOPunchPosition` 打的是 **`localPosition`**，所以幅度实际乘的是**父节点 lossyScale**。
  - 父链实测量（本轮现读，脚本见文末）：卡的父 = `MinionManager` 的 transform（`MinionManager__PlayMinionFromHand.c:16` `Transform.set_parent(card, minionManager.transform)`），其 GameObject 名 = **`MinionArea`**；**13 个战场各两份 MinionArea，父链 lossyScale 全 = (1,1,1)**。
  - ⇒ 原版 punch 的**世界幅度 = 0.5 / 1.0 / 1.5 原版世界单位，1:1**。
  - 我们的 `ToOurs = 182.14 / 108 = 1.6865` 是**像素密度换算**（原版棋盘层 182.14 px/世界单位 vs 我们 `LayoutSpace` 108 px/世界单位），**不是**父链 scale —— 两者是不同的问题。同一段屏幕位移在原版是 `d` 个原版单位、在我们这就是 `d × 1.6865` 个我们的单位。⇒ **×1.686 是正确的**，删掉它反而会让后坐只有原版 59% 的屏幕位移。
- **状态 → 参数**：`PushBackMagnitude` 三档 0.5 / 1.0 / 1.5（判据 = **攻方**近战攻击 `<=3 / <=7 / >7`）· `ToOurs` 恒 1.6865 · 时长 0.4 / vibrato 8 / elasticity 0.3。
- **还差什么**：无。**⛔ 别按 AU1 的说法把这 1.686 去掉。**

---

### 9 `minionMovementInfo`（N3）零消费 —— **原版在 `ResolveAnim` 里消费，而且它【改变这一拍时长】**
- **判据结论**：
  - 消费点 = `BattleManager._ResolveAnim_d__466__MoveNext.c:165-233`（state 1 之后走 `case 3 → LAB_1809accae`）：
    `if (animInfo + 0x70 != null)`：
    - `movementInfo.shouldActivateMinionMovement(+0x14) == 0` ⇒ 退回 `WaitForSeconds(GetAnimDuration())`（state 5），或直接收尾；
    - **`!= 0` ⇒ `CardScript.MoveAndDisappearAnim(card, boardPos, target, movementInfo)` 并 `yield`（state 4）**。
  - `MoveAndDisappearAnim` 的内部时长 = `WaitForSeconds(movementInfo.delay)`（`_d__358:71-77`，`movementInfo + 0x10` = `delay`）+ 一段 `WaitUntil(elapsed >= animTime)`（`:269-283`，`movementInfo + 0x58` = `animTime`；字段偏移与声明序 `delay / shouldActivateMinionMovement / visibilityStateAfterFinish / 三条 curve / useXZ / useY / yOffset / xOffset / zOffset / rotateXYZ / animTime` 逐项吻合）。
  🔴 **订正 AU1 #23**：「⚠ 不改变这一拍时长（时长仍是 `GetAnimDuration`）」—— **错**。两条分支是 `if/else`：走了 `MoveAndDisappearAnim` 就**不再**等 `GetAnimDuration`，这一拍的长度变成它的长度。
- **状态 → 参数**（数据侧本轮现读 `d:/4/Unity/数据/游戏数据/card_anim_order.json`，536 条 animinfo）：

  | `shouldActivateMinionMovement` | 这一拍时长 |
  |---|---|
  | 0（534 行 / 全池绝大多数） | `GetAnimDuration()`（现状不变） |
  | 1（**2 行**） | `delay(0) + animTime(0.6)` = **0.6 s**；两条偏移都是 `(-0.1, 0.2, -0.1)`、`useXZCurve=1 / useYCurve=1`、`visibilityStateAfterFinish=0`（= 结束后卡本体隐藏） |

- **还差什么 / 要不要接**：**要接，而且数据侧不用重新查**（`card_anim_order.json` 已经把 `minionMovementInfo` 整棵（含三条曲线键、偏移、`animTime`）导出来了，只是 `工具/gen_cardanim_clips.py` 没把它写进运行期的 `Resources/cardanim_clips.json`）。缺的是三件：① 生成器加列；② `CardAnimClips` 装载；③ `BattleDriver` 里新增「卡本体沿曲线位移 + 结束时隐藏」的播法（**这一件是新的表现件，不是改现有件**）。

---

### 10 `screenShake`（N2）零消费 —— **原版在拍子起播那一帧并行播，门 = `preset != null`；数据与 presets 都已就位**
- **判据结论**：
  - 消费点 = `BattleManager._ResolveAnim_d__466__MoveNext.c:79-105`：`DelayedVFXPlay` 起播的**同一帧**，`if (OverwriteCameraShakePreset.CanPlay()) OverwriteCameraShakePreset.Play()`。`CanPlay()` = `preset(+0x10) != null`（`OverwriteCameraShakePreset__CanPlay.c`）；`Play()` = `GetModifiedPreset()`（把 `overwrite*` 六项覆盖到 preset 上）→ `CameraShakePreset.Play()`（`OverwriteCameraShakePreset__Play.c`）。
  - 数据：`card_anim_order.json` 里 **536 条 animinfo 中有 36 条 `screenShake.presetSO.m_PathID != 0`**（不是 AU1 记的 92 行 / 71 卡；那个数应该是「带这个结构的行」）。其中 `amplitude` 覆盖值：34 条 = 0（用 preset 自身的）、1 条 = 4.0、1 条 = 6.0。
  - **preset 数值已经在仓库**：`MyGame/Assets/Resources/WarpforgeVFX/shake_presets.json`（`工具/gen_shake_presets.py` 产出，含 27 个 preset + `rawSignal` 的三带 sin/Perlin 算式）。
  - **PathID → preset 名的索引也在本地**：`d:/2/解包整理/09_游戏数据/动画曲线/MonoBehaviour/<名字>_<pathID>.json`。本轮抽查 4 条全部命中：
    `6782848008566651810 = Shake Light 2s`（11 行）· `-4556152188679786237 = Shake Hit Medium`（7 行）· `8064075334217351003 = Shake Very Light 4s`（1 行）· `4555857231027011253 = Shake Hit Small`（3 行）。
  - 10 个不同的 preset 被卡动画引用（行数：11 / 7 / 4 / 3 / 3 / 3 / 2 / 1 / 1 / 1）。
- **状态 → 参数**：`preset == null`（500 行）⇒ 不震；`preset != null`（36 行）⇒ 用 preset 数值 + 该行 `overwrite*` 覆盖；`delay` 字段（本轮见到的取值 0 / 1.0）决定震屏延后。
- **还差什么 / 要不要接**：**要接**。数据只差一步：把 `screenShake` 那六件（preset 名 / delay / 4 个 overwrite 开关与值）写进 `Resources/cardanim_clips.json`（`gen_cardanim_clips.py` 改），消费点直接复用现成的 `WFModuleScreenShake`（它已经在吃 animfx 层的同一套 preset json）。

---

### 11 技能面板底板（`B-23`）—— **AU2 的三条硬事实都成立，但都解释不了「白底白字」；本轮把「谁改 cardFrame」封死到只剩 α**
- **判据结论**（全部本轮现读）：
  1. **`ActiveSkillDisplay` 里对 `cardFrame`（`+0x28`）的全部写入 = 只有 α**：`FadeIn`（`SetSpriteAlpha(cardFrame,0)`）· `FadePower`（`DOFade(cardFrame, 目标 α, fadeTime)`）· `SetAlpha`。全库 `grep "+ 0x28)" ActiveSkillDisplay__*.c` 只有这三处。
  2. **本 build 从不给 `cardFrame` 赋 sprite**：`ActiveSkillDisplay` 里唯一的 `Image.set_sprite` 在 `SetupActiveSkill.c:88`，赋的是 **`manaIcon(+0x60) ← costSprite(+0x68)`**，而 `costSprite` 在 13 个战场里**都是 null**（`ActiveSkillDisplay` MB 现读）。
  3. `cardFrame` 的 Image：`m_Sprite = null` · `m_Color = (1,1,1,1)` · `m_Material = null` · `m_Type = 0`（arena1 = `MonoBehaviour_4864.json`，arena2 = `5081.json`，两场同值）。
  4. 三颗字 `nameText/costText/descText` 的 TMP：`m_fontColor = (1,1,1,1)`、`m_fontColor32 = 0xFFFFFFFF`、`m_sharedMaterial = Asar-Regular Atlas Material`（`Material_1185407168034066855.json`，`_FaceColor = (1,1,1,1)`）。
  5. `cardFrame` 的**矩形不是 0**：`AbilityContainer` 的 RectTransform（arena1 = 3128）是 **stretch 锚点**（`anchorMin (0.0687,0.0831) / anchorMax (0.9126,0.7644)`、`sizeDelta 0`），父 `ActiveSkillDesc`（2911）也是 stretch（`0.6656→0.9658 × 0→0.304`）⇒ 实尺寸 ≈ **486×223 px**。（2D 全树里那个 `-0x-2` 是 `m_SizeDelta`，不是实际尺寸。）
  6. **面板在场景里恒关，但 13 场关的节点不一样**：`ActiveSkillDesc.m_IsActive` —— arena1 = **false**、其余 12 场 = **true**；`AbilityContainer.m_IsActive` —— arena1 = **true**、其余 12 场 = **false** ⇒ **`AbilityContainer` 那一块（白板 + 三颗字）在 13 场里没有一场可见**。（⚠️ 其余 12 场里 `Lights` / `TargetsAvailableText` 这两个**兄弟**节点本身是 active 的 —— 它们各自的 Image α 本轮没查，另记。）
  7. **`FadeIn` 打开的不是面板**：`SetActive(param_1+0x20)` 中 `+0x20 = hideSkillBtn` = GameObject **PathID 1048**，其 Image（`MonoBehaviour_4207.json`）`m_Color = (1,1,1,**0**)` + `m_Sprite = 内置 Background`、3708×1985 ⇒ 那只是那块**全透明点击遮罩**（= 我们 `SkillPanel` 的 `HideBtnW/H … col (1,1,1,0)`，AU2 已核 ✅）。**`FadeIn` 既不激活 `ActiveSkillDesc` 也不激活 `AbilityContainer`。**
- **状态 → 参数**：只有一种静态状态（白板 α1 + 白字），**没有第二档**。
- **还差什么**：**判据仍不闭合**，但方向收窄成一句可查的话：**「谁把 `AbilityContainer`（或其祖先）打开」在 Assembly-CSharp 的 `ActiveSkillDisplay` 一家与 arena 场景的序列化引用里都找不到**（本轮把 13 场的 `GameObject/AbilityContainer.json`、`ActiveSkillDesc.json` 的 `m_IsActive` 全读了，也把 arena1 全库 MonoBehaviour 里指向这两个 GO 的 `m_PathID` 引用搜了一遍 —— 只有它们自己的 `m_GameObject`）。要么在 `BattleManager`/`BattleHud` 的一个我们没扫到的字段上，要么这屏在原版**就是不展示**（那就等于「原版没有」，按铁律 11 例外① 可不做）。
  **要不要真机**：**要** —— 唯一能结案的是进原版跑到 `ActiveSkillDesc` 打开状态拍一张（或读 `Image.color` / `activeInHierarchy`）。在那之前，**继续用深色板（现值 `rgba(.05,.06,.09,.86)`）是唯一可读的选择**；⛔ 不要照抄「白底白字」。

---

### 12 `Avatar Name` / `RadioChat` 两件 —— **`RadioChat` 有驱动（判据齐）；`Avatar Name` 根本不是战斗 HUD 件**
- **判据结论**：
  - **`RadioChat`**（GO 1189，组件 = `VoiceLineBox` + `TransformScalerBySmallScreenUI`）：
    - 持有者 = **`UnitsVoiceLinesPanel.radioMessage`**（arena1 `MonoBehaviour_5205.json`；同件还有 `playerVoiceLineBox 4272 / opponentVoiceLineBox 5215`，`GetChatBox` 里 `+0x30` = radio 那一个）。
    - 显隐**在 `VoiceLineBox` 自己身上**：`VoiceLineBox.ShowChat()` 协程**第一句** `gameObject.SetActive(true)` 再 `WaitForSeconds(0.1)`（`VoiceLineBox._ShowChat_d__19__MoveNext.c:59-63`）；`Disable()` / `FinishChat()` 收尾 `SetActive(false)`（`VoiceLineBox__Disable.c:8-9`）。
    - 驱动链：`VoiceLinesController.ShowRadioMessage(...)` → `if (CanUseVOs(参数))` → `UnitsVoiceLinesPanel.ShowChatBox(panel, **2**, data)`（`VoiceLinesController__ShowRadioMessage.c:8-22`；`2` = radio 那一格）。
    - ⇒ **`T13` 记的「反编译零赋值点」是假阴性**：`VoiceLineBox` 改的是 `this.gameObject`，按 GameObject PathID 去搜必然 0 命中。**`B2:101` 那句（`VoiceLinesController.ShowRadioMessage / CloseRadio` 有驱动）是对的**。
  - **`Avatar Name`**（GO 288）：挂的是 `TextMeshProUGUI` + `EverguildButtonMaterialModifier`，**它是一对组件的子件**：父 GO 923 上同时挂着 **`AvatarDisplay`**（`MonoBehaviour_4376.json`，字段 `avatarName → 3986` = 这个 TMP）和 **`ItemDrawerComponents`**（`5045.json`，字段 `label → 3986`）。
    - 2D 全树的路径：`ChatPopup(inactive) → Background → EnemyInfo → Avatar Item Small → Avatar Name(inactive)` ⇒ 它是**「打开对方聊天弹窗时那张头像卡的名字标签」**，属于 ChatPopup 那一族，**不是战斗 HUD 的常驻件**。
    - arena1 全库搜「谁引用 GO 288 / TMP 3986」：只有它自己两个组件 + 上面那两个持有者，**没有任何战斗侧驱动**。
- **状态 → 参数**：
  - `RadioChat`：显示条件 = **有 radio 语音要播 且 `CanUseVOs` 通过**；时长 = `VoiceLineBox` 自己的打字机 + `FinishChat`（另有独立账）。
  - `Avatar Name`：随 **ChatPopup** 打开（我们**没有做** ChatPopup）⇒ 战斗 HUD 侧**不需要建**。
- **还差什么**：`CanUseVOs(param_5)` 的入参语义（是哪一类 VO 开关）没读 —— 只影响「什么时候不播」，不影响「该不该建」。**要不要真机**：不要。

---

### 13 回合高亮 —— **原版是「每副牌堆一对、各自互斥」；实况里两枚同 True 完全正确，不是矛盾**
- **判据结论**：
  - `DeckManager.SetYourTurn(bool)`：`yourTurnIndicator(+0x28).SetActive(b)` + `enemyTurnIndicator(+0x30).SetActive(!b)`（`DeckManager__SetYourTurn.c`）—— **同一个 DeckManager 内确实互斥**。
  - **但战场里有【两个】 DeckManager**（arena1：`MonoBehaviour_4452` 与 `MonoBehaviour_4681`），各自持一对指示器：
    | DeckManager | `yourTurnIndicator` | `enemyTurnIndicator` |
    |---|---|---|
    | MB 4452 | **`YourTurnImage`**（GO 515） | `NotYourTurnImage_1089`（GO 1089） |
    | MB 4681 | `YourTurnImage_903`（GO 903） | **`NotYourTurnImage`**（GO 267） |
  - 实况 dump 里同时为 True 的两枚 = **`YourTurnImage`(515) 与 `NotYourTurnImage`(267)** —— 它们分别属于**两个不同的 DeckManager**。在「我方回合」这一状态下：4452 的 your=true / enemy=false，4681 的 your=false / enemy=true ⇒ **本来就该同时亮**。
  - 四个节点在场景里 `m_IsActive` **全是 True**（`GameObject/YourTurnImage.json`、`YourTurnImage_903.json`、`NotYourTurnImage.json`、`NotYourTurnImage_1089.json`），=`SetYourTurn` 跑之前的状态。
- **状态 → 参数**：**每个 DeckManager 一对、互斥**；两副牌堆的 `SetYourTurn` 取值互为相反数（一个代表「我的回合」、一个代表「对手的回合」）。
- **还差什么**：无判据缺口。**我们要做的**：断言按「每副牌堆一对、各自互斥」写（现在是按单对写的）；⛔ **别因为 dump 里两枚同 True 就把互斥实现改成两枚一起开**。

---

## 附：本轮复跑命令（都是纯读，可原样再跑）

```bash
# ① 常量（read_literal，必须 -X utf8；-I 会忽略 PYTHONIOENCODING）
D:/2/Warpforge_tools/py312/python.exe -X utf8 d:/4/Unity/工具/read_literal.py \
  "D:/2/unity_run_ref/GameAssembly.dll" 0x1834b3180 0x1834b2bb0 0x1834b2bb8 0x1834b2dc8 0x1834b2dc4 0x1834b2df4 0x1834b3158 0x1834b3090
# ② 13 个战场的 MinionArea lossyScale（本轮实跑：全部 (1,1,1)）
#    走 GameObject/MinionArea*.json → m_Component[0] → Transform/RectTransform 的 m_Father 链，逐级 m_LocalScale 相乘
# ③ cardanim 的 minionMovementInfo / screenShake 统计（本轮实跑：2 行 / 36 行）
#    d:/4/Unity/数据/游戏数据/card_anim_order.json 里递归找同时含 'screenShake'+'minionMovementInfo' 的字典
# ④ shake preset PathID → 名字
ls "d:/2/解包整理/09_游戏数据/动画曲线/MonoBehaviour/" | grep <PathID>
```

## 附：本轮对 AU1 报告的三处订正（供主对话合并）

1. **§1·3 #23** `minionMovementInfo`「不改变这一拍时长」⇒ **错**：`MoveAndDisappearAnim` 与 `WaitForSeconds(GetAnimDuration)` 是 `if/else`，时长变成 `delay + animTime`。
2. **§1·3 #16** Hit 的例外①「DamageType 7/8 那条路没有这道门」⇒ **错**：0.4 之后**照样**执行那个 1.0，是 0.4+1.0=1.4。
3. **回牌库 0.85** ⇒ **0.95**：`MoveToDeck` 的返回值是 `vars.timeToScaleIntoMoving(0.1) + 0.3`，两段相加。
4. （不顺带改结论，只补出处）`§3·1` 里「0.4 / 0.7 / 1.05 三个候选」的答案是 **1.30**；`§3·2` 的 `PlayerAction+0x4c` = **`BattleAction.fromEnemyHand`**。
