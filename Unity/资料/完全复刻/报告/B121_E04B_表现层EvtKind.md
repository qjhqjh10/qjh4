# B121_E04B 执行报告（活状态表 `B120-E04b` · 墓地洗回牌库的表现层）

> 写手 = `B121-E04B`（2026-11-03）· 白名单 = `RuleEngine/Core/BattleEvent.cs` · `RuleEngine/Core/RuleCore.cs` ·
> `CardPresentation/Core/CardFeel.cs` · `CardPresentation/Battle/BattleDriver.cs` · 新建
> `CardPresentation/Editor/B121_E04B_Assert.cs` · 本报告 · `账/_parts/B121_E04B.tsv`。
> **未跑 Unity / 未动 git / 未改账主表 · 活状态表 · 正本**（子代理红线）。

---

## 一、四段时序表（原版逐段 ⇄ 我们的落点）

触发链：`RuleCore.MoveDeckTopToHand` 的空牌库闸（`RuleCore.cs:3178`）→ `ReshuffleCemeteryIntoDeck`
（`RuleCore.cs:14318`）—— 对应原版 `BattleManager._ResolveDrawCard_d__432__MoveNext.c:55-260` 的
`shouldReshuffle == 真` 那一支。

| # | 原版方法 | 谁在什么时机调（参数） | 它做了什么 | 我们的对应（文件:行） |
|---|---|---|---|---|
| ① | `CardScript.UndissolveToDeck()` | 洗回循环里，`ResetToValuesInHand(card,1)` **之后**（`.c:98`） | `SetCardImage(null)` → `FadeCardItems(1, **0**)` 瞬时淡入 → `StopSummonSicknessAnim` → `SetActive(true)` → 虚表 `+0x1f8` 设回牌库态 | 新建视图的初值即这五步的**终态**（α=1 / 真身卡图 / 无召唤病 / 可交互）—— `BattleDriver.cs:41807` |
| ② | （`Transform.set_parent`） | `.c:104`，紧跟 ① | 把那张卡挂到 `BattleManager.GetDeckManager(owner)` | 无等价：**我们没有「墓地里的常驻卡对象」**（阵亡即销毁视图）⇒ 由 ① 处新建一份 |
| ③ | `BattleCardUI.ShowBoardObjects(false)` | `.c:120`（`RemoveFromCemeteryList` 之后） | `2DCard.SetCardVisibility(false)` + `ToggleBody3D(false)` + `Card2DController.Toggle(1)` ⇒ **整张卡藏起来** | **不演出**：外观上没有可观察结果（紧接着 `CreateCardInGame` 就用 state 12 把同一张切回可见做飞行）—— 见 `BattleDriver.cs:41734-41755` 的类尾注释 |
| ④ | `BattleCardUI.SetObjectVisibility(0)` | `.c:123`（state 0 = `inDeck`） | 我方那张：`SetCardVisibility(0)`（隐藏）；敌方那张：`SetCardVisibility(1)`（**换卡背**）+ `ToggleBody3D(false)` | `CardView.SetFace(Hand)`；敌方支 = `SetBackVisible(true, DeckCardBackOf(seat))`（由 `CardFeel.ReturnToDeck` 的 `isPlayer` 选） |
| ⑤ | `CardScript.CreateCardInGame(位姿, **0.1**)` | `.c:150`（`case 0`），随后 `WaitForSeconds(0.1)` | `cardState = 12` + 贴手牌层 + `DOMove/DORotate/DOScale` 到给定位姿；末段有**一条条件 2D 音效**（cue 名未解出） | 并入「起点摆放」（把视图直接放到墓地判据位姿：`CardFeel.CemeteryCardX/Y` + `CemeteryCardScale` 0.65）—— **不做独立的 10 ms 段** |
| ⑥ | `CardScript.MoveToDeck(card, **0**, **0.05**, 0)` | `.c:241`（`case 1`），等它的返回值 | 三条**同长同缓动**（OutQuart）补间（位置/缩放/旋转）飞回 `DeckManager.DeckReferenceObject`；收尾 `SetInDeckPosition()` + `SortingLayer("Background")` + `SetActive(false)`；**返回值 = 0.1 + 0.05 = 0.15 s** | `CardFeel.ReturnToDeck`（`CardFeel.cs:4003`）—— 时长 = `CardFeel.ReshuffleToDeckSeconds`（`CardFeel.cs:4481`，**0.25** = ①⑤ 的 0.1 + ⑥ 的 0.15） |
| ⑦ | `BattleManager.AddCardInPlayToDeck(card, 0)` | `case 2`（`.c:253`） | 数据进牌库（随机落位） | 既有：`RuleCore.cs:14340`（`ps.Deck.Insert`） |

**时长分档**：`EventTiming` 对 `EvtKind.Return` + `Slot < 0` 给 `ReturnGraveHold` = **0.3 s**（既有档，
服务「墓地取回」）。本趟 0.25 ≤ 0.3 ⇒ **装得下、不挤压下一条事件**；多的 0.05 是既有余量，
⛔ 没有为它改 `EventTiming`（不在白名单，且那一档还有别的消费者）。

---

## 二、补了什么

### 2.1 事件载体：**复用 `EvtKind.Return`**（不是新开一种）

判据三条：
1. 语义同族且**已有先例**：`EffectResolver.cs:6989` 的「墓地取回」就是 `EvtKind.Return` + `Slot = -1`；
2. 新开 `EvtKind` 必须**同时**登记 `EventTiming`（`ZeroDurationKinds`/`TimedKinds` + `SourceOf`），
   而 `CardPresentation/Core/EventTiming.cs` **不在本批白名单**；漏登记 ⇒ `BattleScene` 的
   `A2·J2③`（每档都要登记）与 `unsourced == 1` 两条断言**直接红**；
3. 消费端本来就按 `Effect` 分档（`disappearFromHand` 那一档先例），分档键够用。

反证：新开一种就得让 `PlayFeel`/`PlayBattleSfx`/`EventTiming`/`VfxMap` 四处各认一遍（`EvtKind` 的
文件尾注释写着「加成员必须同时有发射口与消费口，否则是死代码」）。

### 2.2 落点

| 东西 | 位置 | 说明 |
|---|---|---|
| `BattleEventEffect.ReshuffleToDeck`（`"reshuffleToDeck"`） | `RuleEngine/Core/BattleEvent.cs:917` | **跨层唯一的字符串出处**（引擎写、表现层读）。追加在命名空间末尾 ⇒ 不推走账里最深 `:874` 的落点 |
| 发射点 | `RuleEngine/Core/RuleCore.cs:14340` | 与 `ps.Deck.Insert(...)` **同一物理行** ⇒ 只在**真回填**那一支发；`Slot = -1` · `InstanceId = inst.Id` · 逐张发（原版那个 `while` 就是逐张） |
| 消费端路由 | `BattleDriver.cs` 的 `PlayFeel` → `case EvtKind.Return` | `else if (e.Slot < 0 && e.Effect == BattleEventEffect.ReshuffleToDeck) PlayReshuffleToDeckFeel(e);`（同物理行追加） |
| 音效 | `BattleDriver.cs` 的 `PlayBattleSfx` → `case EvtKind.Return` | 这一档**静音**：原版四段方法体里没有 `BackToHand` 那条 cue（唯一的音是 `CreateCardInGame` 里一条条件 `Play2D`，cue 名本地没解出）⇒ ⛔ 不拿 `BackToHand` 顶上 |
| 判别式（纯函数） | `BattleDriver.cs:41781` `ReshuffleToDeckPlanOf(e, me)` | 三道闸：`Kind == Return` · `Slot < 0` · `Effect == reshuffleToDeck`；产出起点位姿/缩放/时长/是否本机 |
| 演出 | `BattleDriver.cs:41807` `PlayReshuffleToDeckFeel` | 建视图 → 摆到墓地判据位姿 → `CardFeel.ReturnToDeck` 飞回牌堆 → 销毁；两个读口 `ReshuffleToDeckStartedForTest` / `FinishedForTest` |
| 时长常量 | `CardFeel.cs:4481` | `ReshuffleToDeckSeconds = 0.25f`（0.1 + 0.15，逐段出处写在 doc 里） |
| `ReturnToDeck` 新形参 | `CardFeel.cs:4003` | `targetScale`（`null` ⇒ 照旧 `deckRef.localScale`）；本趟传「牌堆实测世界高 ÷ (`CardView.Height × LayoutSpace.Scale`)」——原版收尾照抄 `DeckReferenceObject.localScale`，我们那颗参考件不存在 |

### 2.3 断言（`CardPresentation/Editor/B121_E04B_Assert.cs`，30 条）

- **A 组（真档，10+1 条）**：两张开局牌洗回 ⇒ **恰好 2 条**事件（`Slot` 恒 -1 · `Player = 0` · `CardId` 是那两张 · `InstanceId` 对得上）；**反证**：非「开局那份」的造物**不发**事件；洗回后照旧继续抽。
- **B 组（假档，4 条）**：产品默认值 `shouldReshuffle = false` ⇒ **0 条**洗回事件，且确实走疲劳 +1；
  两个模式默认值仍 false（灭自证：A 证明「闸能开」、B 证明「默认关」）。
- **C 组（消费端判别式，9 条）**：真实事件 ⇒ `Valid` + 起点 (−8.14, −0.2)/0.65 + 时长 0.25；**反证 4 条**：棋盘上的回手（`Slot = 4`）· `disappearFromHand` · 「墓地取回」（无 `Effect`）· 别的种类/null **都不归本路**；+ 时长 ≤ `ReturnGraveHold`、载体是已登记且有出处的档。
- **D 组（源码闸，6 条）**：`RuleCore` 的发出点在 `ps.Deck.Insert` 那一行 · `PlayFeel` 真路由 · 消费端真调 `CardFeel.ReturnToDeck` · `PlayBattleSfx` 真静音 · 引擎/表现层**都没有裸字面量**。

---

## 三、验证

| 项 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_e04b bash 工具/typecheck.sh` | **0 / 0**（第一次跑出现过 3 条 `BattleDoors.cs` 红 —— 那是 `E121-AUD` 在飞的文件，隔一轮复跑 0/0） |
| `bash 工具/ruleprobe.sh play check` | **rc = 0**；结算指纹当次 = 基线 = `a8b7eac3884b94b0df3e8e6ba6d53577`（本趟产品默认 `shouldReshuffle=false` ⇒ 行为零变化） |
| `python 工具/check_ledger_points.py` | **漂移 0 · 失败 0**（全仓 A 5876 条全通过；中途两次跑到的是别的写手半成品——`CollectionWindow.cs` / `CardDatabase.cs`，他们落盘后回到全绿） |
| `python 工具/merge_ledger_parts.py --dry --only B121_E04B` | **更新 4 · 新增 0**（四个键都命中既有行：`UndissolveToDeck()` / `ShowBoardObjects(bool)` / `SetObjectVisibility(CardStateOptions)` / `MoveToDeck(bool, float)`） |
| `python 工具/check_patch_conflicts.py` | 本批补丁无冲突行 |

---

## 四、还差什么（如实）

1. **断言未接线**：宿主聚合行在 `CardPresentation/Editor/BattleScene.cs`（⛔ 不在白名单）⇒ 由调度台代接线：
   `B121_E04B_Assert.Run(); pass += B121_E04B_Assert.Pass; fail += B121_E04B_Assert.Fail;`
   （本件**零前提**，可接在方法尾「合计」那一行之前）。
2. **消费端的运行期验证**（真建卡 + 真播补间）需要活 HUD ⇒ 本件只覆盖到「判别式 + 源码闸」；
   首次 `BattleScene.Run` 后看 `B121E04B` 前缀。
3. **新视图没有 3D 卡体**：原版那张是带 3D 体的常驻对象，我们用 `CardFace.Hand` 的 2D 卡面替代
   （`CardView.Create` 的 `CardFace` 三档里没有「带 3D 体的手牌态」）。缩到牌堆尺寸时肉眼差小，但**确实缺一层**。
4. **`CreateCardInGame` 里那条条件 2D 音效**（`_d__250:78-81`，`card+0x1d0` 里的 cue）**cue 名本地没解出** ⇒ 静音，
   ⛔ 没拿 `BackToHand` 顶（那是回手倒放那条链的音）。
5. **`RecallToDeck`（棋盘上的单位回牌库）那一支仍是非原版形态**：我们走 `CardFeel.ReturnToHand`（倒放
   `Card Hand To Board`），而原版 `_ResolveRecallToDeck…:319/352` 走的是 `UndissolveToDeck` + `MoveToDeck`。
   属**另一条链**，本件没动；且 `:126` 附近还有一条 `TransformUnitInPlayToCard` 支（哪些档用哪一支未查清）⇒ 不擅自改。
6. **时间线余 0.05 s**（分档 0.3 vs 本趟 0.25，见 §一 末）；**「新建一份」不是「原对象复活」**——我们阵亡即
   `DestroyImmediate` 视图，原版是同一个 `CardScript` 在 deck/hand/board/cemetery 之间换父（结构差，本层消不掉）。

---

## 五、顺手发现（未改，交调度台）

1. 🔴 **账 `战斗逻辑驱动.tsv:807` 的一句旧结论不成立**：「原版两个调用者 RecallToHand / RecallToDeck 走同一条倒放」——
   `_ResolveRecallToHand…:126` 才是 `TransformUnitInPlayToCard → PlayBackToHandAnimation`；
   `_ResolveRecallToDeck…:319/352` 是 `UndissolveToDeck + MoveToDeck`。已在**本批账补丁的那一行**里就地订正
   （主表由调度台合并时才写）⇒ **RecallToDeck 的表现仍是非原版形态**，请立新账。
2. **`EffectResolver.cs:6989` 的「墓地取回」也没有任何表现消费者**：`PlayReturnFeel` 对 `Slot < 0` 直接
   `return` ⇒ 那条链今天同样**只有事件没有画面**；本件只接了「洗回」这一档（用 `Effect` 分流），
   墓地取回要不要同一套（起点也是墓地位姿）**待判**。
3. `HudPiece.WorldH`（`Battle/HudPiece.cs:107`）此前**全仓 0 个消费者**（`DeckCardWorldH` 也只是定义没被用）；本件把它用起来。
