# R14 · `战斗界面_HUD` 剩余 B 行的第二轮核验（B87-R14 · 2026-10-27）

> 代理：R14（只读 + 产出补丁）。对象：`账/战斗界面_HUD.tsv` 里**状态 = B 的全部 42 行**
> （⛔ **不是 43**：R6 的补丁已合并，主表现读 **A 509 / B 42 / C 22 / D 233 = 806 行** —— 比 R6 当时的
> 「A 467 / B 99 / C 7」少一条 B、多一条 C，是**合并期间另有一行被判成 C**，与本波无关）。
> 补丁：`账/_parts/B87_R14_HUD剩余.tsv`（11 列表头逐字照主表 · **42 行** · `轮次 = B87-R14` · `域 = 战斗界面/HUD`）。
> 机检：`python -I -X utf8 工具/check_ledger_points.py <补丁>` ⇒ **A 24 · 通过 24 · 漂移 0 · 失败 0 · 告警 3**。
> 三条告警分别是 **MEMBER / GETTER / FORTEST**（都合法，理由写在那两行的「差异」列里，见 §四·1）。
> 复现脚本：`d:/4/_tmp_view/r14_20261027/gen_patch.py`（键 = 原版类 + 方法名，只覆盖已有行，主表只读）。
> ⛔ 本波未跑 Unity、未动 git、未碰主表、未改任何 `.cs`。

---

## ① 统计

| 档 | 条数 | 构成 |
|---|---:|---|
| **A** | **24** | 真对齐 17 · 结构差异 / 离线无可观察量 5（722 · 723 · 777 · 779 · 783）· **原版那一段本身零画面** 2（759 · 769） |
| **B** | **2** | 711（落点机检挡住，见 §四·1）· 801（只差「y 归零」一个量，见 §六） |
| **C** | **8** | 761 / 767 / 776（同一条缺口）· 778 · 724 · 760 · 683 / 686（同一条缺口） |
| **D** | **8** | 720 · 753 · 772 · 793 · 795 · 796 · 798 · 806（每条都有正面证据，见 §三） |
| 合计 | **42** | 覆盖 42/42，无遗漏（`gen_patch.py` 里每个键都断言过、无 MISS / UNUSED） |

**为什么 A 占了一大半**：R6 那 42 条 B 里有相当一部分不是「没做」，而是
①**结构差异**（我们根本没有那个字段 / 那层对象，等价物在别处）②**原版那段是零画面或死代码**
（写进去也不显示 / 压根没有消费者）。这两类按本波口径都能钉成 A 或 D，不需要再挂 B。

---

## ② 8 条 C（按影响面排序）

### C-1 `CardScript.OrientToTargetingDirection` / `SetCardMovement` / `Update`（761 / 767 / 776）—— **同一条缺口：转向与倾斜**

- **缺的行为**：原版瞄准期每帧做两件事，我们只做了第一件。
  1. **位移**（我们做了）：`localPosition = Lerp(当前, originalPosInPlay(+0x2B8) + varsGlobal偏移 × chargeBackModifier(+0x198) × 侧符号, clamp01(dt / timeToChargeAttack(+0x190)))`
  2. **旋转**（我们没有）：`dir = (对方督军世界位（`+0x258` 链、经 `ConvertPositionBetweenCameras` 换到本相机）− 自己)`、x/y 各乘侧符号 ⇒
     `q = Quaternion.LookRotation(dir) × Quaternion.AngleAxis(chargeAttackAngle(+0x194) × 侧符号, transform.right)` ⇒
     `transform.rotation = Slerp(get_rotation(), q, clamp01(dt / timeToChargeAttack))`
  3. 拖卡那一路（`SetCardMovement.c`）还**每帧写旋转**：`rotationTarget = Lerp(minCardRotation(+0x2F4), maxCardRotation(+0x2EC), clamp01(|Δ| / movingRotationReduction(+0x308) + 0.5))`，
     `rotation = FromEulerRad((rotationTarget + forcedExtraRotation) × Deg2Rad)`，另有「距离过小/过大 ⇒ snap」那一支。
- **原版证据**：`decomp_full/CardScript__OrientToTargetingDirection.c:1-197` · `CardScript__SetCardMovement.c:1-116` · `CardScript__Update.c:5-8`；
  字段偏移 → 名字 = `dump.cs` 的 `CardScript`（`timeToChargeAttack 0x190` · `chargeAttackAngle 0x194` · `chargeBackModifier 0x198` · `chargeUpModifier 0x19C` ·
  `cardMovementSpeed 0x2E8` · `maxCardRotation 0x2EC` · `minCardRotation 0x2F4` · `rotationTarget 0x2FC` · `movingRotationReduction 0x308`）。
- **我们现状**：`BattleDriver.TickTargetingLean(:15924)` 只写 `localPosition`；`CardInteraction.UpdateDrag(:973)` 只有
  `Vector3.Lerp(position, 指针+_grabOffset, 1−exp(−20·dt))`（🔶 自己设计的平滑，没有速度档、没有倾斜）。
- **建议落点**：`TickTargetingLean` 里写完 `localPosition` 之后追加旋转 slerp（角度用已有常量 `CardFeel.ChargeAngleDeg`）；
  `UpdateDrag` 里位置之后追加倾斜。**762 / 768 那三行（渲染残项）与本条是同一族，⛔ 别各写一份。**
- **验收口**：`BattleScene.Run` 里架一次瞄准态，断言 `Quaternion.Angle(transform.rotation, 期望) < 5°`（⛔ 别精确比浮点）。
- ⚠️ **卡住的半格**：`min/maxCardRotation` 的真值在**卡 prefab** 里，而卡 prefab 未完整导出（见 §四·2）⇒ 拖卡倾斜那一半**判据缺**，要先把资源解决（或先用 `chargeAttackAngle` 这一条已解出来的做瞄准方向）。

### C-2 `CardScript.UpdateHealthText`（778）—— **敌方伏击单位不该显示护甲（池内 12 张可达）**

- **缺的行为**：`HasCurrentTrait(0x4B0 = 1200 = ambush) && !isPlayer` ⇒ `armourCounters(+0xC0).Toggle(false)` + `armourFrame(+0x150).SetActive(false)`。
- **原版证据**：`decomp_full/CardScript__UpdateHealthText.c:118-140`；`0x4B0 = 1200 = DefinedTrait.ambush`（`dump.cs` 的 `DefinedTrait`；⚠️ **不是** `bastion`=850）。
- **可达性**：池内 `cardTraits` 含 `ambush` 的卡 **12 张**（`cards_engine.json` 现读，GSC 族）。
- ⚠️ **同一方法里另两支池内不可达**（判据留档、⛔ 别当成缺）：`dropPod` 血档 = `HasCurrentTrait(230)` ⇒ 池内 `droppod` **0 张**；
  `UpdateBastionr`（`bastionText 0x148`）⇒ 池内 `bastion` **0 张**。
- **建议落点**：`CardView.StatShownFor` 的**护甲**那一格加一条「ambush 且敌方 ⇒ 不显示」。
- **验收口**：摆一张敌方 GSC 伏击卡上场，断言护甲格不显示、我方同卡显示。

### C-3 `BattleManager.SetupDataDependentElements`（724）—— 八跳里缺两跳

- **八跳**（`decomp_full/BattleManager__SetupDataDependentElements.c` 全文链）：
  ① `MatchData.OutputLogContents()` ② `TraitCollection.Init()` ③ 静态字段写 this ④ `VoiceLinesController.SetUpChatOptions(matchData)`
  ⑤ **`battleStartMillis(+0x478) = PlayerDataManager.CurrentMillis()`** ⑥ 双侧 `DeckManager.DisplayDeckSize(false)`
  ⑦ **`isReplayPlayActive(+0x521) = (matchData+0x1C == 0xA0)`** ⑧ `TutorialPointer.SetupCursor(stage+0x2C)`
- **已落六跳**：① 开局日志 · ② 词条表**按需读**（`TraitFrames` + `trait_effect_bindings`，无「开局建字典」这一步、等价）· ③ 我们不留 manager 静态引用 · ④ `VoiceLines`(5564) · ⑥ 双侧牌库数 · ⑧ `TutorialPointerUi.SetupCursor`
- **缺两跳**：⑤ 对局起始毫秒（全仓 `battleStartMillis` **0 命中**）· ⑦ 回放局自动播放开关（`isReplayPlayActive` **0 命中** —— 我们的 `Space` 是「暂停/继续」，**与 R8 `B19` 的 C-3 是同一格**）。
- **建议落点**：`BattleDriver.Begin` 里两格字段（⑤ `DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()`；⑦ `PlayModes == Replay` 时置 true），回放档共用。⛔ 别与 R8 B19 各写一份。
- **验收口**：回放局开局断言 `IsReplayPlayActiveForTest == true` 且 `BattleStartMillisForTest > 0`。

### C-4 `CardScript.OnTouchUpAsButton`（760）—— 敌方手牌那道 Sabotage 守卫没接线

- **缺的行为**：非我方手牌卡只有 `rawCardData.spellType == SpellType.Sabotage(230)` 才能点开（`CardScript__OnTouchUpAsButton.c:31-36`）。
- **我们现状**：`CardHighlight.EnemyHandCardClickable(:836)` 语义已对（`isPlayer || (hasRawCard && sabotageKind)`），但**全树 0 生产调用点** ⇒ 被亮出的敌方手牌点不点得开，今天没人判。
- **建议落点**：`Hand/CardInteraction` 的轻点支（`ApplyShowInHand` 旁边那条）；判据⛔ 别另造 —— 用 `CreatePool.MatchesKind(card, "sabotage")` 喂现有那个函数。
- **验收口**：亮出敌方手牌后轻点：sabotage 卡开窗、其余不开。

### C-5 `ActiveWarlordAbility.OnTouchUpAsButton` / `…b__12_0`（683 / 686）—— `PressedBtn` 那颗按压件

- **缺的行为**：`TryUsingActiveAbilityOfCard` 返回真（= 技能不需要选目标、当场放掉）⇒ `PressedBtn.SetActive(true)` + `DOVirtual.DelayedCall(0.4f, b__12_0)` 熄灭。
- **我们现状**：②③（填面板 + 那条最容易做反的 return 语义）已接；0.4 s 时序与开关也实现了（`CardHighlight.cs:957` 常量 · `:996` 收进 Tick · `:989` 钩子口）——**只缺那颗按钮本体**。
- **资源现状（本轮复核）**：`assets_full` **全树** `PressedBtn` 只命中 `globalgamemanagers/MonoScript/MonoScript_2686.json`（脚本桩），
  `bundle_battleprefabs_vfxandmisc_assets_all` 的 **3,582 个 GameObject 名里也没有** —— 而同族的 `InBattleId` / `Armour Text` / `Card Info` **在**
  ⇒ 卡 prefab 的子节点是**部分**导出（R6 记的「4 个子 GO 未导出」方向对，但别理解成「整卡都没导」）。
- **建议**：钩子已就位（`CardHighlight.PressedBtnSetActive`），拿到卡 prefab 的 `PressedBtn` 后按 rect + sprite 逐字建；⛔ **别自己挑图**（W3K04 §五·1）。
- **验收口**：`Editor/BattleScene.cs` 的 `K4HoldAssert`（本波禁跑 Unity，一条都没跑过）。

---

## ③ 8 条 D 的正面证据（**这波把「零画面 / 死代码」的判据钉死了**）

| 行 | 原版方法 | 判 D 的正面证据（⛔ 都不是「搜不到调用点」这种单薄理由） | 时间限定 |
|---|---|---|---|
| 720 | `BattleManager.ResolveDisappearUnit` | 真体只三句（取视图 → `LogWarning` + `gameObject.SetActive(false)` → `FinishResolvingAction`）；唯一生产者 = `AbilityLogic.PlayAbility` 的 `case 0xF0`（240），而**池内 `chosenAbility == 240` = 0 张**（`cards_truth.json` 1,351 张递归扫 = 0 · `cards_engine.json` 1,129 张的 `chosenAbility` 列 = 0） | 本 build |
| 753 | `CardScript.GetCardImageSprite` | 纯取值器（`cardImage.m_Sprite` 回读），**三份产物全树 0 调用点**（`.c` / dev_ilspy / out_il2csharp 各只有定义） | 本 build |
| 772 | `CardScript.ShowWarlordActiveAbilityForTutorial` | 一行 `card.hasActiveAbility(+0x220) = rawCard.hasActiveAbility(+0x2A9)`；**同一句已在 `SetBaseCardTraits.c:37` 对每张卡跑过**（`CardSetup.c:156` / `ResetToValuesInHand.c:154`），且**全树没有任何把 `+0x220` 置假的写点**（`set_hasActiveAbility` 0 外部调用点，三份产物核过）⇒ 再抄一遍不改值 | 本 build |
| 793 | `RawCardScript.CountShowTraits` | `ShowTraits.Count + CardTraits.Count`，**两份产物全树 0 调用点** | 本 build |
| 795 | `RawCardScript.HasAlternativeArtStyles` | 逐个问 **LiveOps 远端配表** `CardsSpecialConfig.CanShowAACard`；配表取不到（`GetConfig` 返回 null）⇒ `break` ⇒ **恒 false** ⇒ 离线原版**一张异画都不算「有」**（**P3 面**，§四 边界④；我们本地固定 7 条 = 多给的，如实标） | 本 build；LiveOps 可用 ⇒ 转 C |
| 796 | `RawCardScript.HasCustomAbility` | 唯一调用者 = `CardScript.get_showCustomAbility`，而 `showCustomAbility` **三源全树 0 读取点** + `assets_full` 字符串引用 0 命中（prefab `m_MethodName` 也查了） | 本 build |
| 798 | `RawCardScript.ShowDefaultTrait` | 唯一调用点 = `CardSetup.c:198` 问 `0x24E = 590 massive`（命中就 `localScale = (−1,1,1)` 镜像 X）；**`massive` 全池 0 张**（`CardTraits` 0 命中 · `full_raw.ShowTraits` 0 命中） | 本 build |
| 806 | `SupportMethods.IsCustomAbility` | 20 值位掩码；唯一调用者是 `HasCustomAbility`，只在已死的 `showCustomAbility` 链上（见 796） | 本 build |

> **规格**：D 的判据不是「我搜不到调用者」，而是**「生产者 / 消费者两头至少一头被正面证明为空」** —— 上面 8 条都满足。

---

## ④ 关键订正（都已就地写进补丁的「差异 / 备注」列）

1. **779 `UpdateMinionInPlayPosition`**：与 `UpdateAssetInPlayPosition` **逐句同构**（两份 `.c` 归一化后 `diff` 只剩寄存器命名差异）
   ⇒ R6 记的「只差单位卡的偏移」**不在方法体里**，而在**调用方** `MinionManager.ReassembleMinions` 算的 `pos`。我们同一处 = `PoseFor(...)`。**判 A**。
2. **798 `ShowDefaultTrait`**：判据是 **`ShowTraits ∪ CardTraits` 两列并集**（先 `ShowTraits(+0x238)` 再 `CardTraits(+0x230)`），
   **不是**「印不印只看 showTraits」。⇒ R6 §顺手发现 5 的那句「我们只读 CardTraits 可能多印」在**判定结果上不成立**（唯一被问的 `massive` 两列都没有）。
3. **790 `GetInversedPos`**：它的等价物**不是** `ArenaSlots.TryResolveSlot`（那是「屏幕 → 格号」），而是 `CardFeel.DepthScaleFactor` 的敌方分支
   （`x = DepthScaleKey1Time + localZ` = 原版的 `末键 time + z`）。**判 A**。
4. **742 `CardDamageCounterController.Toggle`**：生效对象已查明 = `damageCounterParent(+0x20)`，**唯一调用点 = `BattleCardUI.HideExtraEffects`**
   （`SetObjectVisibility.c:69` / `ChangeCardToRemnant.c:35`）⇒ 它是「卡面杂项全藏 / 变残骸时顺手收掉」的**一次性飘字计数**，
   ⛔ **不是常驻计量表** ⇒ R6 的「若常驻则 742 与 743 一起改」**已结**（不必一起改）。
5. **759 `MoveIntoFieldFromHand`**：那一跳 `StartTargetedSpellAnim` 是 **`Instantiate(heroCard.rawCard.targetedSpellAnim)`**，
   而池内 1,127 张卡的资产引用普查里 `targetedSpellAnim` / `nonTargetedSpellAnim` **非空 0 条**
   （`数据/游戏数据/card_vfx_by_card.json` 的 `_assetRefs` 按 `soField` 逐条统计）+ `.c` 里有 null 闸 ⇒ **原版也不实例化**（零画面）。
   顺带：`ShowReticleNow` **不是**它的等价物，准星是 `MoveCrosshair` 另一路（缺口在 R8 `B11`）。
6. **769 `SetInBattleId`**：写的是 `BattleCardUI.inBattleIdText(+0x230)`，而**那个节点的 GameObject 是关着的** ——
   `bundle_battleprefabs_vfxandmisc_assets_all/GameObject/InBattleId.json` 实读 `m_IsActive: false`，且全库没有第二处读写 `+0x230`。⇒ **零画面**。
7. **801 `DoDestroyByAttack`**：R6 记的「碎石粒子的锚点与 prefab 未核」**其实早已落地** —— `CardFeel.RemnantDestroyByBody` 是
   **逐 prefab 实读的显式表**（`RemnantBody3D Aeldari → Remnant Aeldari Destroy particles` / `Necrons → Remnant Necrons Destroy particles`），
   锚点规则 = 灵族取 `Idle/Spirit Stone Idle`、死灵取残骸体根，尺度 = `lossyScale` **赋值**。**只剩「y 归零」一个量**（见 §六）。
8. **722 / 723 / 777 / 783** 的「结构差异」都写清了等价物是谁（派生状态 / 逐帧绝对位姿 / 绑定表），
   ⛔ 不要因为「找不到同名方法」就当成缺实现（这是 R6 那批 B 的主要成因）。

---

## ⑤ 顺手发现（**只报不改**）

1. 🔴 **`check_ledger_points` 的第二类落点盲点：`隐式 private` + `{` 拆到下一行** ——
   `BattleDriver.cs:17149 bool TutorialAllowsCardDisplay` / `:33952 bool BattleUiIsNormalBattle` 这类声明被判
   「既无 `(` 也无 `{`（不像声明）」；而**一行式 getter**（`get { return …; }`）又被判「`)` 后接的是语句」。
   ⇒ R6 记的「隐式 private」只是两个条件中的一个，**真正卡住的是大括号不在同一行**。
   本波的绕法：693 落公开读口 `AllowsCardDisplayForTest(:17973)`（换来 GETTER + FORTEST 两条告警，都合法）；
   **711 只能留在 B**（它的公开读口一个都没有 —— `BattleUiIsNormalBattle` 自己就是隐式 private）。
   **根治（零语义变化，归下一个 `.cs` 写手）**：给这两个属性补显式 `private` 并把 `{` 挪到同一行；
   或者把闸放宽到「`<类型> <名字>` + 下一行 `{`」。
2. 🟠 **卡 prefab 的子节点是「部分在本地」**：`bundle_battleprefabs_vfxandmisc_assets_all` 里有 `InBattleId` / `Armour Text` /
   `Card Info` / `Card 3D` / `2DCard` 等，但 `PressedBtn` 全库 0 命中（只命中 MonoScript 桩）⇒
   683 / 686 的建层判据仍缺、767 的 `min/maxCardRotation` 也缺（同一份 prefab）。**下一步该去别的 bundle / 远端找那张卡 prefab。**
3. 🟢 **`MinionScaleByPosition` SO 真件在本地**：`bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/MinionScaleByPosition.json`，
   两键 `(0.002811431884765625, 0.36000001430511475) → (7.699999809265137, 0.699999988079071)`，两端 Clamp。
   与 `CardFeel` 那四个 `DepthScale*` 常量**逐位一致** ⇒ 以后别再写「这份 SO 取不到」。
4. 📊 **池内计数（本轮现读，供别处引用）**：`ambush` **12 张** · `droppod` **0** · `bastion` **0** · `massive` **0** ·
   `chosenAbility == 240` **0** · `ShowTraits` 非空 **32 张**（`cards_raw.json` 的 `full_raw`）· `CardTraits` 非空 **750 张**。
   ⚠️ 统计口径：`cards_raw.json` 每张卡有「解析视图 + `full_raw`」两份（CLAUDE.md 那条翻倍坑），本波**只扫 `full_raw` / `cards_truth`**，
   `cards_engine.json` 只用来数**池内 1,129 张**。
5. 🟠 **`RawCardScript.GetOriginalCard` 的 10 处调用点全在收藏 / 锻造 / 卡面显示域**（`CardServices.*` / `CardDisplayOptions.*` /
   `CraftingPanel` / `AlternateArtPanel`），**战场域 0 处** ⇒ 本域判 A 无风险，收藏异画页签归 W16。
6. 🟠 **`CardScript.showCustomAbility` 是死属性**（三源 0 读取点 + 资源字符串 0 命中）⇒ 796 / 806 一起判 D，
   ⛔ 别再为「自定义能力」造位掩码表（没有消费端 = 死代码）。

---

## ⑥ 还欠什么（都不是「没查清」，而是**要么要资源、要么要 Unity 一个量**）

| 行 | 欠什么 | 具体怎么收 |
|---|---|---|
| **801** | 一个量：原版 `GetDeathParticlePosition` **只填 x/z（y 恒 0）**，我们取整只 `root.position` | 断言读「残骸 Destroy 件生成后 `transform.position.y == 0f`（容差 1e-3）」；`BattleScene.Run` 里 `animateFeel` 关着也能读（`SpawnRemnantDestroy` 不看它）。若 ≠ 0 ⇒ 改 `new Vector3(p.x, 0f, p.z)` |
| **711** | 落点机检挡住（隐式 private + 拆行大括号）+ 缺 5 档判定 | 补显式 `private` 且 `{` 同行（零语义）；要真做成单口还得补 playingMinion / minionAttacking / choosingUnitTarget / targetedActiveAbility / choosingAbilityTarget 这 5 档 |
| **683 / 686** | 卡 prefab 的 `PressedBtn`（rect / sprite） | 先找到那张卡 prefab；钩子与 0.4 s 时序已就位 |
| **767** | `min/maxCardRotation` 真值（同一张卡 prefab） | 同上；⛔ 别自己编角度 |

**没有「待查」项了**：42 行每一条的判据都读到了具体出处（`.c` / 字段偏移 / 资源文件），
剩下未定的都是**资源不在本地**或**要在 Unity 里读一个量**，不是判据没查到。

---

## ⑦ 逐行索引（42 行 → 档 + 一句话）

| 状态 | 行（原版类.方法） |
|---|---|
| **A（24）** | 693 `CanDisplayLargeCardInTutorial` · 706 `FinishedLeavingBattle` · 717 `OnDestroy` · 722 `SetAllCardsInHandsStart` · 723 `SetCardTracked` · 735 `StopDisplayCardDescription` · 736 `StopDisplayingActiveAbility` · 742 `CardDamageCounterController.Toggle` · 759 `MoveIntoFieldFromHand` · 764 `ResolveRangedAttack` · 766 `SetAsDeck` · 769 `SetInBattleId` · 773 `StopDisplayingCardDescription` · 777 `UpdateAssetInPlayPosition` · 779 `UpdateMinionInPlayPosition` · 783 `GetTraitsFromAbilitiesRawCard` · 786 `HideCardShadow` · 789 `SetCardShadow` · 790 `GetInversedPos` · 791 `GetScale` · 794 `GetOriginalCard` · 799 `BodyVisibilityToggle` · 800 `DoDestroy` · 802 `Initialize` |
| **B（2）** | 711 `GetUIState`（§六）· 801 `DoDestroyByAttack`（§六） |
| **C（8）** | 683 / 686（`PressedBtn`）· 724（起始毫秒 + 回放自动播放）· 760（敌方手牌守卫未接线）· 761 / 767 / 776（转向 & 倾斜）· 778（敌方伏击不显护甲） |
| **D（8）** | 720 `ResolveDisappearUnit` · 753 `GetCardImageSprite` · 772 `ShowWarlordActiveAbilityForTutorial` · 793 `CountShowTraits` · 795 `HasAlternativeArtStyles` · 796 `HasCustomAbility` · 798 `ShowDefaultTrait` · 806 `IsCustomAbility` |
