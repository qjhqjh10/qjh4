# B117-G10-17 · 战斗域第 17 部分「换牌 Mulligan」差异表（2026-11-02）

> 范围：`Battle/MulliganPanel.cs`（1,851 → 1,946 行）· `Battle/CardChoicePanel.cs` / `Battle/ChoosePanel.cs`（**只读**，`G10-14` 刚定稿的三件不碰）· `Battle/BattleDriver.cs:8847-9156`（**只读**，`WBD` 独占）。
> 原版判据口径：① `d:/2/tools/decomp_full/*.c`（主力）② `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/*.cs`（**丢了体的方法只有这里读得到**，本次 `MulliganManager` 整类就是靠它）③ `d:/2/tools/il2cpp_out/dump.cs`（字段偏移）④ 运行期 dump `资料/原版参照图/Unity参照管线_0825/data/runtime_ui_dump_drive_0912.tsv:437-450`。
> ⚠️ 本文件**只写证据与做法**；状态见 `活状态表.md`（对应行由调度台核销）。

---

## 〇、四个重点问题（用户点名的那四件）—— 一句话答案

| # | 问题 | 答案（出处见下文逐条） | 我们 |
|---|---|---|---|
| 1 | **换牌倒计时**长度与超时行为 | **25 s**（`VarsGlobal.mulliganTimeLimit`，`(int)` 截断）· 逐**秒** −1（每次 `yield oneSecDelay`）· **`< 10`** 把剩余秒写进「完成换牌」钮 = `"0:0"+秒` · **`< 1`** 调 `MulliganManager.ProcessMulliganDone()`（= 玩家点完成）· **离线练习（matchType `0x32`）整段跳过** | ✅ 一致（离线那档按用户 2026-09-17 要求做出来，**有据偏离**，见 §三·1） |
| 2 | **点卡 = 看牌 / 标记只走钮** | `CardScript__OnTouchUpAsButton.c:52-58`（换牌期 `cardState ∈ {0xD,0xE}` 只 `DisplayCard(card,1,1)`）· 标记 = `MulliganFrame/Change Card Button` → `ClickMulliganSelected`（`0xD⇄0xE` + `SetAsMulliganSelected` + 钮词条换 `Undo`） | ✅ 一致（两条路在 `MulliganPanel.HandleClick` 分得很清，见 §三·2） |
| 3 | **被换下的牌搬进换牌区还是原地标记** | **原地标记**。整批起手牌在**开面板那一刻**就已经搬进 `MulliganAnchor` 那一行（`AddCardsToMulligan` → `MoveCardToMulliganPos`）；标记只翻 `cardState` + 挂一层红条（`mulliganSelectedPrefab`），**一个字都不搬** | ✅ 一致（本批**新增**断言 `B1–B4` 把它钉住） |
| 4 | **换掉的牌回库后按什么序补** | **补牌从牌库【表头】取（`GetItem(0)` + `RemoveAt(0)`）**；换掉的牌**追加到【表尾】**（`ReplaceMulliganCard` → `Deck.Add`）。**换牌期不洗牌** —— 牌库**只在 `_FinishMulliganFinalPhase` 的 state 3 洗一次（双方各一次）**，在**所有补牌都完成之后** | ⚠️ **不一致**：我们在 `RuleCore.Mulligan` 里**当场洗 + 当场抽**（而且 `Deck.Add` 在我们这套镜像约定下 = 加到**牌堆顶**）⇒ **换掉的牌有概率当次就被抽回来**（原版 0 概率）。落点在引擎（白名单外），见 §二·A |

---

## 一、已修（判据明确 · 当场改）

| # | 元素 | 原版值 / 行为 + 出处 | 我们（改前） | 差 | 改法（现读行号） |
|---|---|---|---|---|---|
| 1 | **眼睛钮一次收哪些件**（`HideMulliganButton`） | `cpp2il/out_cpp2il_dev_ilspy/MulliganManager.cs` 的 `ShowMulliganElements(bool showY)` **全文四句**：① `mulliganTextObj.SetActive(showY)`（提示行 `MulliganText`，里面还含一行 `TurnText`）② `mulliganButton.SetActive(showY)`（「完成换牌」那颗钮 = 底条 + 圆钮 + 字）③ `ShowMulliganCards(showY)` = 逐张 `currentMulligan[i].gameObject.SetActive` **+** `mulliganFrameButtons` 循环（卡下那颗钮 / 钮上的字 / **那枚红标记**）④ `manager.shade.SwitchShade(showY, useAnim:false)`。开关判据 = `ToggleMulliganVisibility()`（同文件 `:183`）读 `mulliganTextObj.activeInHierarchy` **取反**。**不动** `buttonsGroup`（`+0x40`）—— 眼睛自己就在它下面（dump：`Mulligan/ButtonsGroup/{MulliganContinueButton, HideMulliganButton}`） | 只收 **3 样**：每张卡的钮 + 钮上的字 + 压暗层（`MulliganPanel.HandleClick` 的 `HitEye` 分支） | **收少了 4 样**：提示行 / 完成钮 / **换牌卡本体** / 红标记。玩家按眼睛本来就是为了看战场 —— 结果三张卡还挡在屏幕中央 | `MulliganPanel.cs:1569`（`HandleClick` 的 `HitEye` 分支改成一句 `SetContentHidden(!_contentHidden)`）＋ **类尾** `:1921` `SetContentHidden`（整组显隐、**不收眼睛自己**）＋ `:1875` `_contentHidden` 字段 ＋ `:967` `Open()` 复位 ＋ `:976` `Close()` **先放回再关**（原版 `ProcessMulliganDone` 第一句就是 `ShowMulliganElements(true)`）＋ `:1536/1538` `ApplyMarks` 的红标记跟着收起态走（放回来时按 `_marked` 复原）。自检口 `:1878-1912`（`ContentHiddenForTest` / `PromptRowActiveForTest` / `DoneActiveForTest` / `CardActiveForTest` / `MarkerActiveForTest` / `EyeActiveForTest`） |
| 2 | 眼睛收起后**那几件还点得中** | 原版收起 = 那几件 **GameObject inactive** ⇒ 收不到射线（uGUI 只对 active 件派发）。⚠️ 挡输入的其实是「它们不存在」，不是某道闸 | **两处照样判中**：`HitDone` 走**常量算出来的框**（`MenuDraw.PaddedHitRect`）、`ButtonIndexAt` 走 `HudPiece.Contains`（**矩形测试、不看 `activeSelf`**）⇒ 收起后点同一枚坐标**会提交 / 会标记** | 隐形却可点 | `MulliganPanel.cs:1575` —— `_contentHidden` 为真时**其余点击一律吃掉不动作**（眼睛自己已在最前判过）。卡那一条本来就靠 `Physics2D`（`inactive` 自动排除），但三条路口径不一致，这里统一挡 |

> 🔴 **复跑 `BattleScene.Run` 前先看这一条**：本批让眼睛**第一次真的收起换牌卡体**，而 `Editor/BattleScene.cs` 的
> **`D6⑧/⑨`** 那两条像素判据取的成对帧里，`pxShadeOff` 正好就是「眼睛收起」那一帧（`:29193-29203`）⇒
> **卡区的内容会从「亮卡」变成「战场」**。按 `ShadePixelStats`（`:48254`）的口径推算 —— 闸是 `off` 帧亮度 ∈ [0.15,0.95]、
> 结论取**中位数**：卡区像素原来落在 lit 簇（透过率≈1.0），现在会变成 `卡/战场 ≈ 2+`（**仍在 ≥0.50 那一簇**，`litRatio > 0.85` 不受影响）；
> `bgFrac` 的分子（0.08–0.30 那档）与分母**同时不变或分母变小** ⇒ **也是安全方向**。
> ⚠️ **这是推算、没跑过**（子代理不许 `-executeMethod`）⇒ 复跑时请先读日志里那行
> `[D6] 像素成对帧 …：背景透过率 … · 没变暗那一簇（卡/HUD）中位 …` 再判。

---

## 二、判据已定 · 未修（有明确阻塞，写清补丁）

| # | 元素 | 原版值 + 出处 | 我们现状 | 为什么没修 | 补丁（谁接管都能照做） |
|---|---|---|---|---|---|
| **A** | **换牌补抽的次序 / 洗牌时机** | ① `PlayerHand._FinishMulligan_d__52__MoveNext.c`：逐张扫描手牌，`cardState == 0xE` 的 → `ReplaceMulliganCard(card)` → `CardScript.ReturnToDeckAnim(globalVars+0xac)`（**等它演完**）→ `BattleManager.GetMulliganCard(isPlayer)` 拿一张补进来 → `MoveCardToMulliganPos(newCard, 同一个下标, total)`。<br>② `PlayerHand__ReplaceMulliganCard.c`：卡的父件改成牌堆 `DeckManager` 的 transform，然后 **`List.Add`（追加到表尾）**。<br>③ `BattleManager__GetMulliganCard.c`：`GetItem(deck, **0**)` → `RemoveAt(0)` ⇒ **从表头取**（表头 = 牌库顶，判据 `BattleManager__DrawCardFromDeck.c` 也是 `get_Item(0)`）。<br>④ **换牌期一次都不洗** —— `BattleManager._FinishMulliganFinalPhase_d__351__MoveNext.c` 的 **case 3**（此时双方补牌都已完成）才 `ShuffleDeck(player)` + `ShuffleDeck(enemy)`。 | `RuleCore.Mulligan`（`RuleCore.cs:454`）：**一次性**把标记的牌全部 `Deck.Add`（`:491`）→ **立刻 `Shuffle(ps.Deck, ctx)`（`:497`）** → `Draw` N 次（`:500`）。而我们的 `Deck` 约定 **`Count-1` = 牌堆顶**（`RuleCore.cs:3174` `MoveDeckTopToHand` 的注释：原版下标 0 = 顶，我们镜像成末尾 = 顶）⇒ `Deck.Add` = **加到牌堆顶**。 | 落点在**引擎**（`Unity/MyGame/Assets/RuleEngine/Core/RuleCore.cs`），不在本批白名单（本批只写 `MulliganPanel`/`CardChoicePanel`/`ChoosePanel` + 新断言文件）。 | 三处一起改（少一处就是半套）：<br>① 把标记的牌 `Deck.Add` 改成 **`ps.Deck.Insert(0, …)`**（= 原版的「追加到表尾」；我们末尾才是顶）；<br>② **删掉 `:497` 那次 `Shuffle`** —— 换牌期不洗；<br>③ 补抽仍走 `Draw`（它取 `Count-1` = 表头相对的那一端 ✓）；<br>④ 洗牌挪到 `RuleCore.EndMulligan`（= 我们的 `FinishMulliganFinalPhase` 那一下）里做**双方各一次**。<br>⚠️ ②③ 会改**结算指纹**（`play check` 基线）⇒ 必须重钉基线；⚠️ 会改变「换牌后手牌分布」⇒ `RuleEngineTest` 里凡是「先标记再断言抽到哪张」的用例要现读复核（换掉的那张从此**不可能**在同一轮被抽回来 —— 原版语义）。 |
| **B** | **选牌（环境卡）那扇的倒计时由谁喂** | `BattleManager._MulliganCountdown_d__347__MoveNext.c:75`：`if (*(char *)(manager + 0x4f9) != '\0') ChooseCardMenu.CountdownTrigger(count)` —— `+0x4f9` = 「正在选环境卡」标志（`_SetupEnviromentalEffectPhase:60` 置 1、`ClickChosenCardDone` 里清 0）。<br>而 `_SetupEnviromentalEffectPhase`：`:28` `StopCoroutine(DAT_18423bb80)` → `:106` `ChooseCardMenu.Setup(...)` → `:107` `StartCoroutine(DAT_18423bb80)` —— **同一个协程字面量，停完立刻重启** ⇒（推断）计数器**接着剩下的秒数走**，不重置。<br>被喂的那一侧：`ChooseCardMenu__CountdownTrigger.c` —— 没选就先 `Random.Range` 随机选一张 → `String.Format` 写确认钮 → **`param_2 < 3` 直接 `ProcessChooseDone()`**。 | `CardChoicePanel` 里三个方法（`SetTimer` / `CountdownTrigger` / `TickEndSelectionDelay`）**已经照原版实现好了**（`CardChoicePanel.cs:710-737`，带完整判据注释）；但**驱动那一半喂的是【回合时钟】**：`BattleDriver.cs:14777-14793` 只在 `_clockInCountdown`（= 超时后的 15 s）为真时喂。而「选环境卡」发生在**换牌之后、`BeginTurn` 之前** —— 那时候回合时钟**根本没跑** ⇒ **那扇面板的倒计时在实机上永远不出现**（原版是有的：接着换牌剩下的秒数走，到 <3 自动确认）。 | 消费点在 `BattleDriver`（本批**只读**，`WBD` 独占），而且这一段属于「A 域时钟」的接线，改它要动 `TickClock` 的分支结构。 | 在 `BattleDriver` 里给「选环境卡」那一支单开一个喂法：<br>① `OpenMulligan` 时把 `_mulliganLeft`（剩余秒）**带进** `BeginOffensivePhaseIfAny` 那条链；<br>② 面板开着时每帧 `_choosePanelTickSec = Mathf.CeilToInt(remain)`，变化即 `_choosePanel.CountdownTrigger(sec)`（**别复用它现在 `_lastChoiceTick` 那套**，那是回合时钟那一条链的，两套状态别混）；<br>③ `< 3` 由 `CountdownTrigger` 自己 `ProcessChooseDone`（已是现成行为）。<br>⚠️ 原版那一份计数器**不重置**（Stop→Start 同一实例），要不要逐字照抄取决于 §四·1 那条待实况确认的推断。 |
| **C** | **换牌收尾的演出节奏**（补牌逐张飞、面板延后消失） | `PlayerHand._FinishMulligan_d__52__MoveNext.c`：每换一张 → 等 `ReturnToDeckAnim` 演完 → 补一张 → `MoveCardToMulliganPos`；全部换完再等一次 `timeToDraw × 0.5`；之后才 `CompleteMulliganPhase`（= 面板收掉）。单机局这条链是**看得见的一小段动画**。 | 单机局 `_mulligan.Close()` 是**同步立刻**的（`BattleDriver.OnMulliganDone` → `RuleCore.Mulligan` → `Close()`），补牌也在一帧里做完 ⇒ 玩家点完成，面板**瞬间消失**、手牌瞬间补齐。 | 落点在 `BattleDriver.OnMulliganDone` + `RuleCore.Mulligan`（都只读/白名单外）。文件头早就如实记着「**语义一致、节奏不同**」。 | 要照原版就把这条改成协程：逐张 `ReturnToDeckAnim` → 等 → 补一张 → 等 `timeToDraw×0.5`，全部走完再 `Close()` + `BeginBattleAfterSetup()`。⚠️ 期间 `interaction.enabled` 必须保持 false（原版靠压暗层挡）。 |

---

## 三、核过一致 / 有据偏离（**不需要改**）

| # | 元素 | 判据 + 出处 | 我们的状态 |
|---|---|---|---|
| 1 | 换牌倒计时的**总秒数 / 显示 / 超时** | `VarsGlobal.mulliganTimeLimit = **25.0**`（`资料/VarsGlobal_原版数值.md:27`；`_MulliganCountdown_d__347__MoveNext.c:44` 是 `(int)*(float*)(globalVars+0x28)` ⇒ 截断成 25）；`:52-57` 递减闸 = `UIstate == 0xb && !waitingWhileDisconnected`；`:60-63` `< 1` → `MulliganManager.ProcessMulliganDone()`；`:66-67` `< 10` → `SetMulliganTimer`；`:76-78` 每轮 `yield oneSecDelay`（`BattleManager+0x480` = `WaitForSeconds oneSecDelay`，`dump.cs:30886`）。`SetMulliganTimer` 全文 = `mulliganButtonText.text = "0:0" + n.ToString()`（ILSpy） | ✅ `BattleDriver.TickMulligan`（`:8901`）：`Mathf.CeilToInt` 逐帧折成秒、`<10` 写 `"0:0"+s`、`<1` 走「= 玩家点完成」同一条路。**新增断言 C1–C3** 钉住面板侧的格式。<br>⚠️ **有据偏离**：原版 `:24-26` 对 **matchType `0x32`（离线练习）整段 `return`**（不播 `Mulligan` cue、不计时）；用户 2026-09-17 明确要求「一切按原版、把倒计时做出来」⇒ 我们**这一档也计时**。该偏离已写在 `BattleDriver.mulliganSeconds` 的 doc 里，**不是漏做**。 |
| 2 | 点卡 = 看牌 / 标记只走钮 | `CardScript__OnTouchUpAsButton.c:52-58`（换牌期只 `displayingCardFlag = 1` + `BattleManager.DisplayCard(card,1,1)`，指针移开由 `OnTouchExit.c:15-16` 收）；标记 = `MulliganFrame__ChangeCardButtonOnClick.c:5` → `CardScript__ClickMulliganSelected.c`（闸 = `UIstate==0xb` ∧ `isPlayer(+0x40)` ∧ `mulliganOption(+0x260) != 0x1e`；`0xE→0xD` 取消 / `0xD→0xE` 标记 + `SetAsMulliganSelected`） | ✅ `HandleClick` 里两条路分开（`ButtonIndexAt` → `ToggleMark`；`CardAt` → `OnCardSeen`）；`A1663` 那次删掉的「点卡也能标记」**没有回来**。**新增断言 B2–B4** 把「标记不动牌」钉住。 |
| 3 | 换牌区的**排布**（整批搬 + 原地标记） | `PlayerHand._AddCardsToMulligan_d__41__MoveNext.c:119` 逐张 `MoveCardToMulliganPos`（行心 = `MulliganAnchor` 的 `m_LocalPosition (0,29,0)`）；标记**不搬**（见上） | ✅ 卡宽 292 / 行心 511 / 间距 `RowSpacingPx`（`MulliganPanel.cs:617-661`，间距那条 2026-10-29 已按算法改成 502.2，**与实拍 ~400 的冲突仍挂着**、账 1178/A1749 未结）。**新增断言 B1** 断「三张都在 `RowWorldPos` 上」。 |
| 4 | 「完成换牌」的**键盘**路径 | `MulliganManager.Update()`（ILSpy）：`Input.GetKeyDown(Space) \|\| GetKeyDown(Return)` ∧ `buttonsGroup.activeSelf` → `ProcessMulliganDone()`（掉线时只警告） | ✅ `BattleDriver.HandleMulligan` 同形（门控从「按钮组在不在」换成「面板可不可见」—— 如实记，非逐字）。 |
| 5 | 钮上词条 `Replace` ⇄ `Undo` 的**切换点** | `MulliganFrame__UpdateButtonText.c` / `__SetupMulligan` / `__ChangeCardButtonOnClick` **三处都按 `targetCard.cardState == 0xE` 二选一**（`Battle/Mulligan/Replace` ↔ `Battle/Mulligan/Undo`） | ✅ `ApplyMarks` 里 `CardBtnWord(m)` 一处。 |
| 6 | 每张卡的钮**挂在哪** | `MulliganManager__SetupMulliganButton.c`：从 `+0x50`（`cardMulliganButton` prefab）实例化 → **位置 = 卡的 `transform.position`**、旋转 = 单位四元数、**localScale = 卡的 localScale**、父件 = `MulliganManager` 自己（不是卡）；随后 `Localize.Term` 二选一 + `UnityEvent.AddListener(ChangeCardButtonOnClick)` + `Card2DController.ToggleTooltips(enableToolTipsOnCard)` | ✅ 我们按「卡宽 55.7% / 钮心在卡下沿下 39 px」世界位贴（几何判据见 `BtnWFactor/BtnHFactor/BtnBelowCardPx` 三条 doc）。⚠️ **`ToggleTooltips(enableToolTipsOnCard)` 这一跳我们没做**（原版序列化默认 `true` ⇒ 换牌期每张卡挂 tooltip）—— 归 **`G10-13 Tooltip`** 那一片，见 §四·3。 |

---

## 四、判据未定 / 查不到（⛔ 别当「没有」）

1. **`_SetupEnviromentalEffectPhase` 里那次 `StopCoroutine → StartCoroutine` 到底是不是「接着剩下的秒数走」** —— 两处引用**同一个字面量** `DAT_18423bb80` ⇒ 极可能是同一个 `IEnumerator` 实例被停掉再启动（那样计数器 `+0x28` 保留），但**没有实况可以确认**（原版已关服、这一相位又只在「有 ≥2 张环境卡」时才进）。⇒ §二·B 的补丁只钉「**由换牌剩余秒数喂**」，不钉「是否重置」。
2. **`MulliganManager.Setup()` / `ActivateMulligan()` 里那些「没有参数」的 `WaitForSeconds`**（`_SetupMulliganPhase_d__341__MoveNext.c` 里 `WaitForSeconds(uVar6)` 实参被反编译器吞了）—— 那是 `globalVars` 上某两个时长字段，**值没读出来**。属表现层节奏，已记在 `BattleDriver.cs:8847-9155` 那段头注释的「我们省掉的」里。
3. **`Card2DController.ToggleTooltips(enableToolTipsOnCard)`**（换牌期每张卡挂 tooltip）—— 判据明确（见 §三·6），但消费点是 tooltip 泵（`BattleDriver.TickTooltipAt`）与 `Card2DController`，**都不在本批白名单**，且属 `G10-13`。⇒ **记账，不改**。
4. **敌方换牌行**：我们**不画**（`_cards` 只装本方起手牌）。原版 `MoveCardToMulliganPos` 的 `!isPlayer` 支会走 `(total − cardPos) − 1` 的翻转序 + `SetSortingOrder`（`MulliganPanel.RowSortRankFor` 已经把这个口径写死、**目前零调用**）。是否要画敌方的换牌行 —— 原版确实有（`PlayerHand.currentMulligan` 双方各一份），但**实拍里看不到**（用户给的两张参考图只有本方）。⇒ **判据未定**，留给调度台裁定要不要补。

---

## 五、断言与验证

- **新文件**：`Unity/MyGame/Assets/CardPresentation/Editor/B117_G10_17_Assert.cs`（静态类 + `Run()`，**不改任何既有宿主**）。
- 三组 / **共 41 条 `Check`**（A 组 27 · B 组 6 · C 组 6 · `Run()` 里 2 条；其中 **17 条是硬前提**、1 条是异常兜底 —— ⛔ 前提那 17 条不是「有 ✓ 就算过」，它们是**防假绿**的）：
  - **A（16 条）** 眼睛收整组：A1 收 · A2 提示行 · A3 完成钮 · A4 卡钮+压暗 · A5 **卡体** · A6 红标记 · A7 **眼睛自己仍 active**（灭自证）· A8 收的是内容不是窗 · A9 **收起后点完成钮坐标不提交**（灭自证）· A10 **收起后点卡钮坐标不标记**（灭自证）· A11 放 · A12 五组复活 · A13 红标记按 `_marked` 复原 · A14 **没标记的不许被画上红条**（反向灭自证）· A15 放回后提交仍通 · A16 **`Close()` 不许把收起态带回手牌**。
  - **B（7 条）** 标记 = 原地：B1.0–B1.2 三张都在换牌区那一行 · B2 标记前后位移 < 1e-3 · B3 只标点中的那张 · B4 取消标记同样不动牌。
  - **C（4 条）** 倒计时文案：C1 `"0:09"` · C2 `"0:01"` · C3 能写回默认文案（+ 2 条前提）。
- **灭自证的三条**都是**结构上不可能同时满足**的：A7（连眼睛一起收 ⇒ 点不回来）、A9/A10（只切 `activeSelf` 不闸命中 ⇒ 隐形可点）、A14（`SetActive(true)` 一把梭 ⇒ 没标记的也画红条）。
- 跑法（⚠️ 只有调度台能跑 Unity）：
  ```
  unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
    -projectPath "D:\4\Unity\MyGame" -executeMethod B117_G10_17_Assert.Run -logFile -
  ```
  判绿红看 `B117G1017 ==== 合计：✓ N · ✗ M ====` 那一行（⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。
- **typecheck**：`TMPDIR=/tmp/wf_g1017 bash d:/4/Unity/工具/typecheck.sh` —— 本批两个文件（`MulliganPanel.cs` · `B117_G10_17_Assert.cs`）**改完后那一次 = 0 / 0（运行时 0 · 编辑器 0）**；之后又跑了几轮，工作树被**别的写手**的在飞改动打断（`Core/EventTiming.cs` / `Battle/EventTiming.cs` 与 `WarpforgeArena1/Editor/ArenaBuilder.cs:4634`，mtime 都在本批之后），**每一轮里属于本批两个文件的 error 恒为 0 条**（`grep MulliganPanel\|B117_G10_17` = 0 命中）。
- **行号守恒**：`MulliganPanel.cs` 在账里的 **13 个 A 行落点**（`:131 :251 :458 :965 :973 :979 :1011 :1171 :1287 :1439 :1479 :1516 :1550`）**逐条现读仍在原符号上**（只有 >1550 的区域被追加）；新成员全部追加在**类尾**。
- ⚠️ **没有跑 Unity**（红线：子代理不许 `-executeMethod`）⇒ 本批的断言**一条都还没执行过**，绿红以调度台复跑为准。
