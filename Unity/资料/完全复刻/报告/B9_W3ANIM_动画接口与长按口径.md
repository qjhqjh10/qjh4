# B9_W3ANIM —— 特效施工单 #2/#6 + 复核 #3/#4 + K-04 五条（长按/轻点口径）

> 执行代理：`B9_W3ANIM` · 2026-10-27 · 未派子代理（本件全部自己做完）
> 白名单：`Battle/AnimFXController.cs` · `Hand/CardInteraction.cs` · `Core/CardHighlight.cs`（**只动了前两处**；`CardInteraction.cs` 未改）
> 未跑 Unity（红线 1）；`typecheck` 见 §四·1。

---

## 一、结论（逐条）

| # | 条目 | 结论 |
|---|---|---|
| 2 | `PlayTriggerAnim` | **施工单前提有两处错**。① 原版是 `CardScript.PlayTriggerAnim(animPrefab, targetCard, traitValue)`，**不是** `AnimFXController.SetData(prefab, card, traitValue)` —— `SetData` 只有 `(actingCard, targetCard)` 两个形参；`traitValue` 的唯一消费者是 **`TraitEffectController.UpdateValue(traitValue)`**。② `AnimFXController.SetData`（`AnimFXController.cs:365`）**已逐句对齐、无缺口**（两句字段写 + 逐模块 `Initialize`/槽 `0x178`）；prefab 空判属于 `PlayTriggerAnim`。⇒ 本波**补了** `AnimFXController.PlayTriggerAnim`（`:433`，四步逐句 + 警告原文照抄）。⚠️ 本 build `battleAnims.sentinelUnitAnim` / `sentinelDeckAnim` **都是空 PPtr** ⇒ 原版那条链**唯一可达分支 = 一条警告 + 返回 null**（铁律 11 例外①）。账行 `特效动画:513` B→**A**（机制级）。 |
| 6 | `StopHandAnim` | **施工单前提整条错**。原版真体（`_d__405 MoveNext` 全文）= `yield WaitForSeconds(delay)` → **`tutorialPointerBattle.OnDisable()`**（与 `TutorialPointer.StopAnim` **同址** RVA `0x854B20` = ICF 折叠）。**与手牌动画、`IsDoingAnim` 完全无关**；原版 `PlayerHand.IsDoingAnim` 全量 **0 调用**。唯一调用点 = `BattleManager.EndTurnClick`（out_il2csharp `:5333`）的 **pending-berzerk 教程支**，`delay = 2.0f`。我仓**没有**这一跳 ⇒ 账行 `特效动画:497` B→**C（缺，P2）**、落点清空；改动请求见 §四·2。 |
| 3 | `ActivateIconAnim`（复核） | **亮图标那半已实现**（`AbilityIconAnim.PlayNow`，`:148`：先 `PulseBadgeByKeyword`〔= `HighlightTraitIcon`〕再 `DisplayTrigger`〔= `DisplayTriggerAnim`〕，顺序与原版一致、且同样**没有** `trait==0` 早退）。🔴 **但整条链在原版就是死代码**：`BattleManager.PlayIconAnim` / `ResolveAbilityIconAnim` 在 `decomp_full/*.c` 与 `out_cpp2il_dev_ilspy/*.cs` **两棵树都只命中定义**（5 个形参也绑不了 UnityEvent），我仓 `AbilityIconAnim.Enqueue/Tick` 同样 0 调用 ⇒ **与原版一致、不构成缺口**。建议调度台裁 **D**（本代理只写建议，不改状态）。 |
| 4 | `ActivateTriggerTraitAnim`（复核） | **高亮那半已接**：`BattleDriver.PlayTraitPulse` 两条都调（`TraitFrames.DisplayTrigger` + `v.PulseBadgeByKeyword` → `CardView.PulseBadge` `:4060`，参数 = 1.6× / 0.5 s / loops2 Yoyo / OutCubic = `BoardTraitIcon.HighlightIcon` 逐句）。早退语义等价（`trait==0` 早退 vs 我们两边各自空转）。**两处小偏差**：① 调用序相反（原版先 Highlight 再 DisplayTriggerAnim）；② `playIfWarlord` 门我们做成「整条 trait 预判」，原版在循环内逐条 effect 判（实测 7 个多行 trait **0 条混合** ⇒ 今天行为等价）。维持 **B**，换序后可升 A。 |
| K-04 | 五条（`HUD:682/683/684/686/687`） | **682 / 683 / 686 已实现并已接线**（`CardHighlight.ActiveWarlordAbilityTouch` 状态机 :946-1120 + `BattleDriver` 四处接线），逐条证据见 §二。**684** 已实现但**接线口径与原版不同**（我们用 `tapHasAbility` 二选一互斥分派，🔶 自设计；原版是两颗组件各自被 touch 事件触发**两条链都跑**）。**687** 状态机 + 0.4 s 时序已在（钩子 `PressedBtnSetActive` 未接时**出声**），**建层判据本地拿不到**（四路搜过，见 §三）。 |

---

## 二、证据（文件:行号）

### 2.1 施工单 #2 `PlayTriggerAnim`
| 事实 | 出处 |
|---|---|
| 原版签名 `PlayTriggerAnim(GameObject animPrefab, CardScript targetCard, int traitValue = 0)` + 四步真身（🥇 最干净） | `d:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp/CardScript.cs:10420-10460` |
| 同上（Ghidra 互证：`op_Implicit` 判空 → `Instantiate(prefab, +0x1A0)` → `SetData` → 槽 `0x198`=`UpdateValue`） | `d:/2/tools/decomp_full/CardScript__PlayTriggerAnim.c:1-57` |
| 同上（ILSpy 互证） | `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/CardScript.cs:17159-17200` |
| `AnimFXController.SetData` 真身 = 两句字段写 + `foreach modules → Initialize`（**没有** traitValue） | `d:/2/tools/decomp_full/AnimFXController__SetData.c:1-40` |
| 唯一调用点 = `PlaySentinelAnims`，实参 `(sentinelUnitAnim, null, 0)` | `d:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp/PlayerHand.cs:1075-1076`（ILSpy `PlayerHand.cs:3265`） |
| 🔴 `sentinelUnitAnim` / `sentinelDeckAnim` = **空 PPtr**（`m_PathID: 0`） | `d:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/BattleAnims.json`（17 个键，只有 `heal` 非空） |
| 我们的 `SetData` 落点（未改动、逐句已对） | `CardPresentation/Battle/AnimFXController.cs:365` |
| 本波新增 `PlayTriggerAnim` | `CardPresentation/Battle/AnimFXController.cs:399-458`（声明 `:433`） |

### 2.2 施工单 #6 `StopHandAnim`
| 事实 | 出处 |
|---|---|
| 真体：`state0 = new WaitForSeconds(delay)` → `state1 = if (this) tutorialPointerBattle.OnDisable()` | `d:/2/tools/decomp_full/BattleManager._StopHandAnim_d__405__MoveNext.c:1-44`（IL：`out_il2csharp/…/BattleManager.cs:40707-40760`） |
| `StopHandAnim(delay)` 本身只是协程工厂 | `decomp_full/BattleManager__StopHandAnim.c:1-14` · ILSpy `BattleManager.cs:21319-21330` |
| 唯一调用点 + `delay = 2.0f`（紧跟 `DisplayTriggerAnim(berzerk,1)` / `NotifyCantDoAction(PendingBerzerk)` / `StartPointer(clickAndDrag, 单位位, 目标位, 1)`） | `out_il2csharp/Assembly-CSharp/BattleManager.cs:5313-5340`（所在方法 = **`EndTurnClick(bool timeOutFlag)`**，声明 `:5070`） |
| `OnDisable` 体（`+0x70=0` / Kill `+0x78` tween / `pointer(+0x20).gameObject.SetActive(false)`）= `StopAnim` | `decomp_full/TutorialPointer__OnDisable.c:1-26`；`dump.cs:111710-111760` 里 `OnDisable` 与 `StopAnim` **RVA 同为 `0x854B20`** ⇒ ICF 折叠 |
| 原版 `PlayerHand.IsDoingAnim()` **0 调用**（我们拿它当等价物的那条判据不成立） | `decomp_full/PlayerHand__IsDoingAnim.c` 全库 `grep -l` 只命中它自己 |
| 我仓对应位置：`BattleDriver` 的 pending-berzerk 支**只** `NotifyCantDoAction` | `CardPresentation/Battle/BattleDriver.cs:16527-16530` |

### 2.3 复核 #3 / #4
| 事实 | 出处 |
|---|---|
| `ActivateIconAnim` = `GetTraitFromAbilityTrigger` → `HighlightTraitIcon` → `DisplayTriggerAnim(trait, 1)`，**无早退** | `decomp_full/CardScript__ActivateIconAnim.c:1-19` |
| `ActivateTriggerTraitAnim` = 同上，**多一句 `trait==0 ⇒ return`** | `decomp_full/CardScript__ActivateTriggerTraitAnim.c:1-23` |
| `HighlightTraitIcon` = 按 `traits[]` 找同 id，命中才 `BoardTraitIcon.HighlightIcon()` | `decomp_full/BattleCardUI__HighlightTraitIcon.c:1-50` |
| `DisplayTriggerAnim` = `GetTrait` 空则 false；循环 effects，`IsWarlordEquivalent && !playIfWarlord ⇒ return false`；只做 type∈{3,6} | `decomp_full/BattleCardUI__DisplayTriggerAnim.c:1-119` |
| 我们的「亮图标」= `PulseBadge`（1.6 / 0.5 / loops2 / OutCubic，先 Kill 再归位） | `CardPresentation/Core/CardView.cs:4052-4082` + `Core/Badges.cs:215-219` |
| 我们的 #4 调用点（两半都调） | `CardPresentation/Battle/BattleDriver.cs:24602`（`PlayTraitPulse`；两半在 `:24651` / `:24656`，行号现读，W1 正在改本文件） |
| 我们的 #3 调用点（两半都调、顺序对、无早退） | `CardPresentation/Battle/AbilityIconAnim.cs:148-167` |
| `PlayIconAnim` 全量 **0 调用** | `decomp_full/*.c`、`out_cpp2il_dev_ilspy/*.cs` 两棵树 `grep PlayIconAnim` = 只有定义；`ResolveAbilityIconAnim` 同样 0 调用（`ActivateIconAnim` 的唯一调用点 = `BattleManager._ResolveAbilityIconAnim_d__475__MoveNext.c:36`） |
| `playIfWarlord` 数据核对：75 行 / 7 个多行 trait / **0 条混合** | `CardPresentation/Resources/trait_effect_bindings.json`（`rows` 数组，逐行统计） |

### 2.4 K-04 五条
| 账行 | 原版判据 | 我们的实现 | 接线点 |
|---|---|---|---|
| 682 `OnTouchDown` | `ActiveWarlordAbility__OnTouchDown.c:1-14`（两句：`+0x3c=Time.time`、`+0x38=1`） | `CardHighlight.cs:1007` | `BattleDriver.BoardCardTouchDown` `:33972`（先写 `IsPlayer = side==_me`，再 `OnTouchDown()`） |
| 683 `OnTouchExit` | `ActiveWarlordAbility__OnTouchExit.c:1-7`（一句） | `CardHighlight.cs:1017` | 三条路：松手 `:14962`、拖够 `:14978-14980`、**指针出框** `:34811`（`PointerHeldRaw() && !v.HitByCollider(WorldPointer())`，与手牌同一口） |
| 684 `OnTouchUpAsButton` | `ActiveWarlordAbility__OnTouchUpAsButton.c:1-72`（`isPlayer` 闸 → `SetupActiveSkill` → `TryUsingActiveAbilityOfCard(forceSelectCard:1)` → 真且 `PressedBtn!=null` ⇒ `SetActive(true)` + `DelayedCall(0.4f)`） | `CardHighlight.cs:1056` | `BattleDriver.BoardPress` 的 `!held` 支：`WarlordAbilityOf` `:34755` → `WarlordSetupActiveSkill` `:34770` / `WarlordTryUseActiveAbility` `:34783`（返回语义照抄：真 = 当场放掉） |
| 686 `Update` | `ActiveWarlordAbility__Update.c:1-26`（`cardShowTime <= elapsed && elapsed != cardShowTime` = **严格大于**；命中对 `+0x38` 写两字节 `0x100`） | `CardHighlight.cs:1025`（`Tick`，`> CardShowTime`，`CardShowTime = 0.3f`） | `BattleDriver.TickBoardHold` `:34801`，在 `TickBoardHover` / `BoardPress` **之前**调 |
| 687 `b__12_0` | `…b__12_0.c:1-9`（`PressedBtn.SetActive(false)`） | `CardHighlight.cs:1044-1048`（`_pressedBtnHidePending && now >= _pressedBtnHideAt ⇒ PressedBtn(false)`；`Tick` = `:1025`） | `_waPressedBtn` 跨过松手继续 `Tick`（`BattleDriver.TickBoardHold` `:34801` 尾段） |

常量亲读（`工具/read_literal.py`，本轮实测输出）：
- `0x1834b3158` = **4B float 0.4**（`DOVirtual.DelayedCall` 的时长；第三实参 `1` = `ignoreTimeScale`）
- `0x1834b2bb0` = **4B float 0.2**（手牌那条 `THRESHOLD_FOR_OPENING_CARD_DISPLAY`，⛔ 与棋盘 0.3 不是同一条）
- `ActiveWarlordAbility.__ctor` 写 `+0x40 = 0x3e99999a` = **0.3f** ⇒ `cardShowTime` 不是「字段默认 0」

---

## 三、没查清的部分（如实记）

1. **`PressedBtn` 的节点名 / rect / sprite —— 四路都没命中**（判据仍缺）：
   - `d:/2/tools/assetripper_out/ExportedProject/Assets` 全量 `grep -rl "e7c2f59b1afee1b54de6696e2a5c4a07"`（该脚本的 AR guid）`--include=*.prefab --include=*.unity` ⇒ **0 命中**；`grep -rl "PressedBtn" --include=*.prefab` ⇒ 0。
   - `d:/2/解包整理`（整棵树，含 `08_预制体特效/战斗预制体/`）`grep -rl "PressedBtn" --include=*.json` ⇒ **0 命中**；按字段签名（`parentCard`+`activeSkillDisplay`+`PressedBtn` 三键同在）扫该目录 **5,694 个 MonoBehaviour** ⇒ 0 命中。
   - `assets_full` 整树 `grep -rl "ActiveWarlordAbility"` ⇒ **只有** `globalgamemanagers/MonoScript/MonoScript_2686.json`（= 类元数据，**不是实例**）。
   - `CardPrefab`（`08_预制体特效/战斗预制体/GameObject/CardPrefab_-7895724578163876928.json`）根上 **10 个组件逐个解过**（Transform / Animation / 8 个 MonoBehaviour：CardImage 控制器、CardTexts×5、CardHighlight 那台…），**没有** `ActiveWarlordAbility`；它的 4 个子节点 Transform 在，但**没有**一个组件挂在 `ActiveWarlordAbility` 上。
   ⇒ 结论仍为「判据本地拿不到」，但**至少要建层时得知道那颗钮的 rect**，所以这条**不能**按「自己挑一个图」结案（红线 6）。
2. `DOVirtual.DelayedCall` 的 `ignoreTimeScale`：从 Ghidra 调用实参 `(0x1834b3158, cb, 1, 0)` 里的 `1` 推断为 `true`（⇒ **unscaled**），**没做 VA 级反汇编复核**。我们 `Tick` 用的是同一台 `Clock`（`Time.time`，scaled）⇒ 已在 `CardHighlight.cs:996` 打 `🔶 自己设计` 标记；本仓无全局 `Time.timeScale`，今天看不出差别。
3. 「原版 `PlayIconAnim` 零调用」有 **1% 可能是被内联**（CLAUDE.md「编译器内联会让调用点消失」）：两树 grep 都没有、5 参方法绑不了 UnityEvent、`ResolveAbilityIconAnim` 同样 0 调用 —— 三条旁证，**未做 VA 反汇编**。所以我在账里只写「**建议 D**」，没替调度台裁。
4. **K-04 五条一条断言都没有**：`Assets/CardPresentation/Editor/BattleScene.cs` 里 `grep -n "WarlordAbility|PressedBtn|HoldDisplayed|InPlayClickFlag"` = **0 命中**。本波禁跑 Unity ⇒ 没有跑过任何一条，真机也没验（按住 0.3 s / 0.4 s 两档手感**未实机验证**）。
5. `StopHandAnim` 的 `delay` 是否还有别的调用点（比如按难度/脚本变）：全量两树只命中 `EndTurnClick` 一处、值 `2.0f` 字面量 ⇒ **只有一档**，但这只是「本 build 实读」。

---

## 四、改动清单

### 4.1 代码（3 个文件里动了 2 个）
| 文件 | 改动 | 行数 |
|---|---|---|
| `CardPresentation/Battle/AnimFXController.cs` | **新增** `public static GameObject PlayTriggerAnim(GameObject animPrefab, Transform effectsAnchor, Transform actingCard, Transform targetCard, int traitValue = 0)`，**追加在类尾**（`:433` 声明，`:399-458` 含判据注释）。四步/警告原文/返回实例逐句照原版；`🔶 自己设计` = 锚点与 actingCard 形参化 | **+62 行，0 删**（`git diff --numstat` = `62 0`） |
| `CardPresentation/Core/CardHighlight.cs` | 两处 `🔶 自己设计（非原版）` 标记，**就地改写已有注释行、零行数变化**（`_holdDisplayedThisPress` `:994` · `_pressedBtnHidePending` `:996`）—— 该文件被 `账/战斗界面_HUD.tsv:687` 按行号引用（`:957` / `:1056-1070` / `:989`），**行数必须不变** | `2 2` |
| `CardPresentation/Hand/CardInteraction.cs` | **未改**（#6 的真落点不在本类，见 §一） | `0 0` |

**typecheck**（`TMPDIR=/tmp/wf_w3 bash 工具/typecheck.sh`）：
- ✅ **收工最后一次 = `运行时错误数: 0 / 编辑器错误数: 0`**（两处 `.cs` 改完各跑一次，最终 0/0 达到）。
- ⚠️ 如实记中间态：改完第一处时是 **1 / 62**，但**逐条核过、没有一条在我这三个文件里** ——
  运行时那 1 条 = `Assets/CardPresentation/Effects/UIEffectTogglerCoffeeEffects.cs(59,21) CS0246 Coffee`（= `Assets/ThirdParty/**`，W5 正在写）；
  编辑器 61 条 = `Editor/BattleVfxAssertE3.cs` / `Editor/TraitEffectAssert.cs` / `RuleEngine/Editor/RuleEngineTest_*.cs`
  （别人的活：`CountSetSpec` / `ConditionSpec` / `ResolvedVfx` / `HuntMarkController` 那些类型此刻正被改名）。
  按 CLAUDE.md 13·3「错误全部集中在不是你负责的文件上 ⇒ 不是你的问题」处理，没有去动别人的文件；等他们收工后复跑即为 0/0。

### 4.2 账补丁
- `资料/完全复刻/账/_parts/B9_W3ANIM.tsv`（11 列 × 4 行 + 表头，LF，无裸 CR）· 生成器 `_parts/_gen_B9_W3ANIM.py`
- 4 行的 `(原版类, 方法)` 在全 21 份主表里**各命中 1 处**（逐份核过，不会被 merge 的「>1 处报错跳过」拦下）：
  - `特效动画:497` `BattleManager.StopHandAnim(float delay)` —— 判据出处/要点重写、**B→C**、落点 `—`、等级 P2、备注写明「2 s 后收教程指针」
  - `特效动画:501` `CardScript.ActivateIconAnim(AbilityTrigger trigger)` —— 落点改到真正两半都调的 `AbilityIconAnim.cs:148（PlayNow）`、备注写 0 调用证据 + **建议 D**
  - `特效动画:504` `CardScript.ActivateTriggerTraitAnim(AbilityTrigger trigger)` —— 落点改到 `BattleDriver.cs:24602（PlayTraitPulse）`、备注写两处偏差
  - `特效动画:513` `CardScript.PlayTriggerAnim(...)` —— **B→A**、落点 `AnimFXController.cs:433（PlayTriggerAnim）`、备注订正施工单原句
- ⚠️ 只写补丁，**没动主表**（合并是调度台同步点的动作）。`merge_ledger_parts.py --dry` 现读会一并合并全仓 30 条未合并补丁（含别人的），调度台按 `--only B9_` 分批即可。

### 4.3 改动请求（`Battle/BattleDriver.cs` = W1 独占，⛔ 我没动）
1. **K-04/684 的接线口径**（现读 `:14960-14976`）：原版是**两颗组件各接一次** touch 事件 ⇒ 松手那一下**两条链都跑**：`CardScript.OnTouchUpAsButton`（开卡窗 / 残骸收集）**和** `ActiveWarlordAbility.OnTouchUpAsButton`（填技能面板 + 试放）。
   我们现在是 `bool tapHasAbility = tapU.HasAbility;` → `tapHasAbility ? wa链 : TapBoardCard` 的**互斥分派**（`BattleDriver.cs:14968/:14970`，注释自己写着 🔶）。
   ⇒ 请求：去掉互斥，两条都调 —— `CardBoardHighlight.OnTouchUpAsButton(BoardTouchInput(v, side))`（忠实实现已在 `CardHighlight.cs:796`）+ `wa.OnTouchUpAsButton()`。
   ⚠️ 落地前要处理**重复**：`TapBoardCard` 现在同时承担「残骸/灵魂石收集」和「`ToggleUnitCard` 开关窗」，与 `CardBoardHighlight.OnTouchUpAsButton` 的 inPlay 支重叠 ⇒ **二选一留原版那条**，`ToggleUnitCard` 的「关窗」差量单独保留（原版关窗走别的路）。**⛔ 别顺手删 `ToggleUnitCard`（既有断言指着它）。**
   ⚠️ 顺带（本轮亲读，纠正一条容易踩的推断）：**棋盘 inPlay 档原版没有 0.2 s 门** ——
   `CardScript__OnTouchUpAsButton.c` 里那道 `DAT_1834b2bb0(0.2) <= timeSinceLevelLoad − +0x320` 只挂在
   **手牌两档**（`cardState 1 = inHand` / `8 = inHandShowing`，`.c:31-44`），`inPlay(2) / inPlayAttacking(3) /
   inPlayAimingAttack(17)` 走的是 `.c:63-83` 那段（残骸收集 / `DisplayCard(1, !mulliganVisible)`），**不量时长**；
   `mulligan(13/14)` 也不量 ⇒ ⛔ 别给棋盘加 0.2 s（我们 `CardBoardHighlight.OnTouchUpAsButton`
   就这么实现的，是对的）。
2. **#6 `StopHandAnim`**（现读 `BattleDriver.cs:16527-16530` 的 pending-berzerk 支）：原版那一段是**四件**，我们只有第 2 件：
   ① `battleCardUi.DisplayTriggerAnim(berzerk, 1)`（= `TraitFrames.DisplayTrigger("berzerk", …)`）
   ② `NotifyCantDoAction(Loc.T("Battle/Tips/PendingBerzerk"))` ✅ 已有
   ③ `tutorialPointerBattle.StartPointer(clickAndDrag, Get2DPosOfCard(狂暴单位), Get2DPosOfCard(目标), 1)`（我们的战斗侧指针 = `Battle/TutorialOverlay.SetPointer(bool on, Vector3 world)`，`Shell/TutorialPointerUi` 只有 Menu/Wildcards 两档）
   ④ `<StopHandAnim>d__405(2.0f)` = **2.0 s 后 `TutorialPointer.StopAnim()`**（= `Pointer.activeAnim=false` + Kill tween + `pointer.gameObject.SetActive(false)`）
   ⇒ 请求：补 ①③④；计时可挂在现有的每帧泵上（`OnEndTurnClick` 里发一个 2 s 的待办，`Tick` 到点 `SetPointer(false, …)`）。
3. **#4 换序**（现读 `PlayTraitPulse` `:24651` / `:24656`）：原版先 `HighlightTraitIcon` 再 `DisplayTriggerAnim`；我们把 `v.PulseBadgeByKeyword(kw)` 放在 `DisplayTrigger` 之后 ⇒ 请求互换两行（`if (v.PulseBadgeByKeyword(kw)) return;` 现在会**提前 return**，换序时要顺手把 return 语义拆掉，别吞掉状态框那一支的日志）。
4. **`TraitFrames.BlocksForWarlord`（`Core/TraitFrames.cs:518`）**：原版 `playIfWarlord` 门在**循环内逐条 effect** 判（前面播过的保留），我们是「整条 trait 预判」⇒ 只有将来出现**混合 trait** 才分叉（今天数据 0 条）。**建议**改成逐条判并配一条断言；不急。

---

## 五、顺手发现

1. 🔴 **账 `特效动画:497` 的旧落点（`CardInteraction.Relayout` / `IsDoingAnim`）是错的** —— 原版 `PlayerHand.IsDoingAnim()` 全量 **0 调用**，`animFlag` 在反编译里也**查不到写点**（ILSpy 把私有 setter 反编译成 `obj = value`、字段名丢了 ⇒ 这条只能算「查不到」，⛔ 不能说「没有来源」）。我们 `Relayout` 的 `_animGate` 是**手牌重排动画期**那件事（账 `战斗逻辑驱动:1174` 已 A），**别**再拿它当 `StopHandAnim` 的替身。
2. 🔴 **施工单把两个方法揉成一个**：`#2` 写的「`AnimFXController.SetData(prefab, card, traitValue)`（`op_Implicit` 判空）」= 把 `CardScript.PlayTriggerAnim` 的四步压成了 `SetData` 一行的形参表。同类坑已在别处出现过（「照 ILSpy 写把一整排实参当 `default`」），建议施工单里凡引用跨方法调用，**把被调方法的签名单独写一行**。
3. **账 501/504 的落点原来都指向 `TraitFrames.DisplayTrigger`** —— 但真正**两条都调**的位置分别是 `AbilityIconAnim.PlayNow` 与 `BattleDriver.PlayTraitPulse`；只指 `DisplayTrigger` 会让下一个代理以为**亮图标那半没人调**（这正是施工单 #3 起疑的原因）。已在本补丁里改正。
4. **`CardBoardHighlight.OnTouchUpAsButton` 忠实实现存在但未接线**（账 `战斗界面_HUD:761` 已记 B）—— 它和 K-04 的 **684 是同一个根因**：棋盘松手那支只走了一条链。这两条账应一起收（改动请求 §4.3-1）。
5. **`AbilityIconAnim` 整条线（queue + Tick + PlayNow + 3 处判据注释）是完好但零调用的**，而且**原版也零调用** ⇒ 它不是「欠接线」，是「原版没有」。⛔ 别为了让它跑起来去 BattleDriver 里凭空 enqueue（那会**多出**原版没有的行为）。
6. **原版 `BattleCardUI.DisplayCard` 对同卡重复开窗幂等**（`card == +0x588` 只清指针就 return；`card != +0x588` 且 `activeSelf == show` 直接 return）⇒ 我们 `CardDisplayWindow.Toggle` 是**真 toggle**，所以 `_holdDisplayedThisPress` 那道显式挡是**必要**的（已打 `🔶 自己设计`）。**要真复刻**就得把 `DisplayCard` 的三支早退补进 `CardDisplayWindow` —— 那是界面线的活，记账待办（本波白名单外）。
7. `merge_ledger_parts.py --dry` 现读显示全仓还有 **30 条**未合并补丁（其他代理的）⇒ 调度台同步点一次合；**合并前必跑 `check_patch_conflicts.py`**（本补丁 4 个 key 全唯一，但我没跑跨补丁冲突检查，留给调度台）。
8. **`CardInteraction.cs` 我一行没改**：本波它的两个候选落点（#6 的 `Relayout`、K-04 手牌侧 0.2 s 门）经核查**都不需要动** —— 手牌 0.2 s 门（`LongPressNoDisplaySeconds`，`:140`）与原版 `CardScript__OnTouchUpAsButton.c:37-44` 一致（长按**不**开窗），⛔ 别为了「统一阈值」去改它。

---

## 六、交接给下一个代理（若轮换）

- 本件已收口：**3 个代码文件里改了 2 个**（+62/0、+2/−2）、账补丁 4 行、本报告。
- 未做（不在白名单 / 需 Unity）：§4.3 四条改动请求（W1 / 界面线）· K-04 五条的断言与真机两档（`BattleScene.cs` 现在 **0 条**覆盖）· `PressedBtn` 建层（判据缺）。
- 复跑入口：`TMPDIR=/tmp/wf_w3 bash 工具/typecheck.sh`（要等别的代理收工才可能 0/0）· `python -I -X utf8 工具/merge_ledger_parts.py --dry`。
