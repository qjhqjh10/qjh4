# B9_E2UDW · 溶解 / 撤销溶解族（账 972 · 973 · 730）

> 会话：2026-10-27 · 执行代理 `E2UDW` · 白名单 = `CardPresentation/Core/CardFeel.cs`（独占）· `CardView.cs`（未改）· `UnitTweenTable.cs`（未改）
> 类型检查：`TMPDIR=/tmp/wf_E2UDW bash d:/4/Unity/工具/typecheck.sh` = **运行时 0 / 编辑器 0**（最后一跑）
> ⚠️ 期间跑出过两次别人（`E1RNG`，`RuleEngine/**`）正在写的错：`BattleContext.cs(2206/2209/2218/2234) CS0708`、`EffectResolver.cs(12960) CS0117` —— 都与本批无关，等它写完后再跑即为 0/0。

---

## ① 结论

| 账行 | 方法 | 本轮结论 | 建议状态 |
|---|---|---|---|
| 972 | `CardScript.UnDissolveWholeCard()` | **演出件早已存在且已接线**（现核：`BattleDriver.cs:8176` → `CardFeel.MoveBackToHand` → 本方法）。本轮修掉两处**与原版不符**的地方（淡错了量、缓动错），并补"碰撞体必开"的一条兜底 | C → **B**（不是 A，理由见 ③-①：出对半缺失 ⇒ 观测量今天看不到） |
| 730 | `CardScript.DissolveMinionCard(bool)` | 原账**唯一未查清项**（`BattleCardUI +0x2e0` 是哪颗组件、虚方法是什么）**已结案**，并已落地 | **B 维持**，但「查不到是哪颗」这条**必须从差异列删掉**（已结案） |
| 973 | `CardScript.UndissolveToDeck()` | **维持 C**（无落点）。本轮把判据补硬：两个调用点现读 + 五步的**前提状态** + 我们的模型 | **C**（附"状态等价"论证供调度台裁） |

---

## ② 证据（逐条，全部本轮亲读）

### 972 `UnDissolveWholeCard`
- `decomp_full/CardScript__UnDissolveWholeCard.c:1-42`：async kickoff（`AsyncVoidMethodBuilder.Start<_UnDissolveWholeCard_d__311>`）。
- `decomp_full/CardScript._UnDissolveWholeCard_d__311__MoveNext.c:36-90`（🥇 可见操作三句）：
  `BattleCardUI__FadeCardItems(cardUI, **DAT_1834b2bb8**, *(float*)(globalVars+0xb0), 0)` →
  `Task.Delay(ceil(0.2 × **DAT_1834b3124**))` → `(*(card+0x140)).Toggle(+0x1f8)(1)`。
- 常量实读（`工具/read_literal.py`）：`0x1834b2bb8` **4B float = 1.0**（8B 读成 2.0 是垃圾档，按类型取 4B）· `0x1834b3124` **4B float = 1000**。
- 真实现：`cpp2il/out_cpp2il_dev_ilspy/BasicCardUI.cs:761-770` ——
  `FadeCard(newAlpha, timeToFade, delay=0)` = `DOTween.To(() => fadeGroup.alpha, x => fadeGroup.alpha = x, newAlpha, timeToFade)` + `SetDelay(delay)`。
  ⇒ **① 没有 `SetEase`**（⇒ DOTween 默认缓动）**② 没有 `SetUpdate`**（⇒ `UpdateType.Normal`）**③ 淡的是 `fadeGroup`（`BasicCardUI +0x68`，见 `il2cpp_out/dump.cs:19833`）**。
- `BattleCardUI.FadeCardItems(float,float)` = `base.FadeCard(newAlpha, timeToDissolve)`（`cpp2il/.../BattleCardUI.cs:1491-1494`，纯转发）。
- 碰撞体那颗件：`CardScript +0x140` = `CardCollider2D cardCollider2D`（`dump.cs:35376`）；`CardCollider.Toggle(bool)` = **Slot 12**（`dump.cs:33974`）。
- 缓动默认值：本工程 `Assets/Resources/DOTweenSettings.asset:31` `defaultEaseType: 6`（= `Ease.OutQuad`）；反编译里 `DOTween.Init(null, null, null)`（`out_cpp2il_dev_ilspy/BattleManager.cs:15158`）**没有改** `defaultEaseType` ⇒ 原版单条 `DOTween.To` = **OutQuad**。
- 量的选择：`_alpha2D` 的 doc 明写「原版 `2DCard` 那个 CanvasGroup」「**不能拿整卡 `SetAlpha` 顶替 —— 那个会连 3D 体和数值层一起淡（原版只淡 2D 那一层）**」（`CardView.cs:6532-6544`；同族注释 `CardView.cs:6516`）；`FadeCard` 淡的 `fadeGroup` 也是 uGUI `CanvasGroup`（不影响 `MeshRenderer`）⇒ 两者同量。
- 调用点（现核）：`CardScript__MoveBackToHand.c:23-27`、`CardScript__OutFromPlayingAreaAnim.c:22-27`；我们这侧只剩 `BattleDriver.cs:8176` 一处（原版三个离场入口折成一处，见账 819 的备注）。

### 730 `DissolveMinionCard`
- 🥇 `cpp2il/out_cpp2il_dev_ilspy/CardScript.cs:11625-11649`（**ILSpy 全方法体**，逐句）：
  `FadeCard(!dissolveY ? 1 : 0, globalVars.timeToDissolveCard)` → `ActivateCard(!dissolveY)` → `if (dissolveY) return;` →
  `if (body3D != null) { body3D.SetActive(false); if (RemnantBody3D) RemnantBody3D.Toggle(option: false); }`。
- `+0x2e0` 的结案证据：`il2cpp_out/dump.cs:26237` = `private RemnantBody <RemnantBody3D>k__BackingField; // 0x2E0`；
  `RemnantBody.Toggle` = `public abstract void Toggle(bool option, bool withAnimation = True, Action onComplete)`（`dump.cs:41885`，Slot 8）。
  ⇒ 原账「`dump.cs` 的字段表在 `0x298` 就断了 ⇒ 查不到是哪颗」**是错的**（表一直排到 `0x2E8`）⇒ 已就地改正。
- `.c` 侧的 `(0, 1, 0)` 三个实参 = `option:false` + `withAnimation:true`（默认）+ `onComplete:null`（默认）—— 与该签名逐一吻合。
- 我们这侧：`CardView.SetRemnantBody(false, …)` 已实现撤场两档（死灵播 `From Remnant` 由动画事件收尾 / 灵族立即撤），判据在它自己的 doc（`CardView.cs:2541-2575`）。

### 973 `UndissolveToDeck`
- `cpp2il/out_cpp2il_dev_ilspy/CardScript.cs:11683-11692`（ILSpy 逐句，五步）：
  `SetCardImage()` → `FadeCardItems(1f, **0f**)` → `StopSummonSicknessAnim()` → `gameObject.SetActive(true)` → `cardCollider2D.Toggle(true)`。
  （`.c` 侧 `CardScript__UndissolveToDeck.c:8-21` 同形，`FadeCardItems` 的时长实参是 `0` ⇒ **瞬时**，不是 0.2。）
- 两个调用点（本轮现读、原账没写）：
  · `BattleManager._ResolveDrawCard_d__432__MoveNext.c:96-103`：牌库空 ⇒ 从**墓地**（`+0x110` = CemeteryManager）逐个往回捞时，先 `ResetToValuesInHand(1)` → `UndissolveToDeck()` → 重挂到 `DeckManager.transform` → `RemoveFromCemeteryList` → `ShowBoardObjects(false)` / `SetObjectVisibility(false)` …
  · `BattleManager._ResolveRecallToDeck_d__543__MoveNext.c:182` 与 `:319`（同一族的召回）。
- 我们这侧：墓地回牌库是**数据级**（`RuleEngine/Core/RuleCore.cs:3073` `ReshuffleCemeteryIntoDeck`，只搬卡 id）⇒ **没有可复用的"停成半溶"视图**；
  新抽到是 `CardView.Create` 新建 ⇒ 其初值就是 α=1 / `_hitEnabled=true` / 无召唤病动画 ⇒ 五步的**终态**天然成立。
  ⇒ 维持 C（⛔ 不新建"把视图停半溶再复原"的路径，账里已写死）。

---

## ③ 没查清 / 未覆盖

1. 🔴 **972 的观测量今天看不到 —— 缺的是「出对半」**：原版在**把 targeted 战术/附魔卡打到场上**那一刻会 `DissolveWholeCard(timeToDissolveCard 0.2)`（`CardScript__MoveIntoFieldFromHand.c:60-75`；ILSpy `CardScript.cs:11651+`：`cardCollider2D.Toggle(cardType==10)` + `DOScale(×globalVars+0x40)` + `StopShowingCodex` + `StopReflected` + `ToggleHighlight(false)` + `FadeCardItems(0, t)`），
   取消时才 `UnDissolveWholeCard` 淡回来。
   **我们没有这一跳**：战术卡打出去是「视图直接销毁」（`BattleDriver.cs:13708-13710` 原话），而我们这条链上 `_alpha2D`/`_alpha` 起点本来就是 1 ⇒ 972 的「α 0→1」在**当前链路上不可观察**。
   ⇒ **建议另立一条账**（`CardScript.MoveIntoFieldFromHand` + `DissolveWholeCard` 的出对半）。落点在 `Battle/**`（`E3CMP` 独占），**不在本批白名单** ⇒ 本轮只报不做。
2. `BasicCardUI.fadeGroup`（`+0x68`）与 `2DCard` 那个 CanvasGroup **是否同一个节点**没核到 prefab 实例。两者都只作用于 2D UI（CanvasGroup 不影响 `MeshRenderer`）⇒ 取 `_alpha2D` 这个**量**是对的，但"同一个 CanvasGroup"这句不当判据。
3. 原版 `FadeCard` 没写 `SetUpdate` ⇒ 用 Unity 默认（scaled）；我们一律走 `CardTween.Mode`（批处理 = `Manual`，否则自检验不了动画）—— 这是**自检需要**，不算偏离，但记在这里。
4. `RemnantBody.Toggle` 的 vtable 槽号与偏移换算**对不上**（`CreateRemnantBody.c:79` 的 `Initialize` 是 `+0x1a8`、本处 `Toggle` 是 `+0x1b8`，按 `dump.cs` 的 `Slot` 号算是 Slot 9 = `DoDestroy`，与实参形状不符）。**不影响结论**（ILSpy 直接给出方法名），但"槽号 ↔ 偏移"这条换算**别当判据用**。
5. 部队支那一跳我们是**等价替身**（不是逐句）：原版 `FadeCardItems(1,0.2)` 淡的是 `fadeGroup`（2D 整卡）；我们淡的是 `ArtAlpha`（2D 立绘层），理由是「整卡那一档在场上本来就是 1，看得见的那一半是卡面」。这条是 `E3P13` 的既有裁定，本轮只改了它的缓动，**没有推翻**。

---

## ④ 改动清单

文件：`Unity/MyGame/Assets/CardPresentation/Core/CardFeel.cs`（`git diff --numstat` = **24 / 15**，净 +9 行；行尾仍是 **LF**，HEAD 也是 LF，没翻）

**A. `UnDissolveWholeCard`（账 972，行号仍是 3653 —— 账落点零漂移）**
1. 淡的量：`card.Alpha`（整卡，连 3D 卡体一起淡）→ **`card.Alpha2D`**（= 原版 `fadeGroup`/`2DCard` CanvasGroup 那个量）。判据见 ②-972 与本文件 `CardView.cs:6540-6546` 那条既有的「⛔ 不能拿整卡 `SetAlpha` 顶替」。
2. 缓动：`Ease.Linear` → **`Ease.OutQuad`**（原版 `FadeCard` 无 `SetEase`；`DOTweenSettings.asset:31 defaultEaseType = 6`；`CardTween.cs:78-82` 规定「单条 `DOTween.To` 直接传 OutQuad」）。同时把套一层 `Sequence` 的写法收成单条 tween（形状才与 `FadeCard` 一致）。
3. 新增 `OnKill`：补 `SetAlpha2D(1f)` + `SetHitEnabled(true)`。原版那 200 ms 是 `Task.Delay`（**不受补间打断影响**），且 `if (plVar2 != null)` 才动 ⇒ 被 kill 也要把碰撞开回来；α 复位成 1 是安全侧（同 `HandToBoard` 的 `OnKill` 纪律）。doc 段落同步改成 `SetAlpha2D`。

**B. `MoveBackToHand` 部队支（账 730 的最后一块拼图）**
1. **补齐原版第 4 步**：`body3D.SetActive(false)` 之后 `if (RemnantBody3D) Toggle(false, withAnimation: true, null)` —— 我们的等价物
   `if (card.RemnantBodyRoot != null) card.SetRemnantBody(false, card.RemnantBodyPrefabName);`
2. 那一跳的 2D 淡入缓动 `Linear` → `OutQuad`（同一条理由：原版 `FadeCard` 无 `SetEase`）。
3. `MoveBackToHand` 的 doc 里那句「`+0x2e0` **查不到是哪颗**：`dump.cs` 字段表在 `0x298` 就断了」**就地改掉**（铁律 5）—— 真相 = `RemnantBody3D`，并写上 ILSpy 行号。

**C. 顺手就地改正（铁律 5）**：`CardFeel.UnDissolve` 的 doc 原来写「用户侧入口 = `HideHeroAtStart` / `UndissolveToDeck`」——
现核这两个**都不是活路**（`HideHeroAtStart` 全库 **0 调用点**，账 401 判 D；`UndissolveToDeck` 账 973 判 C）⇒ 已改成这句，避免下个会话照着它去找"入口"。

### 顺手发现（⛔ 未改，交调度台分流）
1. 🔴 **缺「出对半」**：见 ③-① —— 原版 `MoveIntoFieldFromHand` 对 targeted 战术/附魔卡会 `DissolveWholeCard(0.2)`（2D α→0 + `DOScale` + 关碰撞）。我们**没有这一跳**，所以 972 的淡入今天看不到；也意味着「打出战术卡那一刻 2D 卡面应有的淡出/缩小」整段缺失。建议单独立账。
2. 🔴 **账 730 的差异列有一句已被推翻**（「`+0x2e0` 查不到是哪颗」）—— 建议调度台把那一句换成「= `RemnantBody3D`（`dump.cs:26237`），原版是 `Toggle(false, withAnimation: true, null)`；我们已接」。
3. 账 973 可以补一句「`FadeCardItems` 的时长实参是 **0**（瞬时），不是 0.2」——原账没写，容易让人误以为也走 0.2。
4. `RemnantBody` 的 vtable 槽号 ↔ 对象偏移换算在三份产物之间对不上（③-④）。以后别再拿「`dump.cs` 的 `Slot:` 号 × 8 + 基址」当判据，优先用 ILSpy 的类型化调用。

### 该跑哪些宿主自检（⛔ 我没跑，Unity 批处理归调度台）
- **必跑**：`BattleScene.Run` —— 972 / 730 两条都在这条链上（`MoveBackToHand` 唯一调用点 `BattleDriver.cs:8176`）。
- **建议一起跑（引用 `CardFeel` 的宿主）**：`RewardsScene.Run`（11s）、`CardBaseDemo.Run`（卡面四档分辨率；本轮没动版面，但它引 `CardFeel`）。
- **不必**：`RuleEngineTest.Run`（未碰引擎）· `ShellScene` / `DeckScene` / `CollectionScene` / `ShopScene` / `MainMenuScene` / `SettingsScene`（都不引用 `CardFeel`）· 联机三条（未碰 `Net/`、`BattleDriver`）。
- ⛔ 不需要 `_run_8_checks.sh` 全套。


---

# 第二批（2026-10-27 · 施工单 930 / 856 / 908 / 806 + 账补丁）

> 白名单同前：`CardPresentation/Core/CardFeel.cs` 与 `CardView.cs` 本波仍归本代理独占。
> 类型检查：`TMPDIR=/tmp/wf_E2UDW bash d:/4/Unity/工具/typecheck.sh` = **运行时 0 / 编辑器 0**。
> 账补丁：`账/_parts/B9_E2UDW.tsv`（生成器 `账/_parts/_gen_B9_E2UDW.py`，**不带 `--write` 只 dry-run**）。

## ① 结论

| 施工单行 | 方法 | 本轮做法 | 等级建议 |
|---|---|---|---|
| 90 | `CardScript.StopAnimsInPlay()`（930） | **补上原版第 ① 步** `CleanStatusAnims(clearAll: true)`；②③④ **无落点**（证据见下） | P2（不动；缺口已收） |
| 83 | `CardScript.RemoveInHandEffects()`（856） | **只做判据，不动代码** —— 原版那条链**恒 no-op** ⇒ 建议 **改判 D**（铁律 11 例外①） | P2 → **D** |
| 87 | `CardScript.SetStateInHand()`（908） | **视图侧零缺口**（六步逐一映射，④⑤ 都已有）；仍判 **A** | A（维持） |
| 73 | `CardScript.MoveBackToHand()`（806） | 视图侧四步全在（上批收口），本轮经 ② 自动获得 930 的 ①；`OutFromPlayingAreaAnim` 与 `SetStateInHand` 的挂父半归驱动 | P1（视图侧完成） |

## ② 证据

### 930 `StopAnimsInPlay`（`decomp_full/BattleCardUI__StopAnimsInPlay.c:20-71` 全文亲读）
原版四段：① `CleanStatusAnims(**1**)`；② 倒序遍历 `activeAnimsInPlay`（`BattleCardUI +0x258`）逐件 `DestroyImmediate`；
③ `if (+0x260 summonSicknessFrame != null) Destroy(它)`；④ 清表（`version++` / `size = 0` / `Array.Clear`）。
前置 `CardScript__StopAnimsInPlay.c:5-6` 只是转发 `cardUI(+0x290).StopAnimsInPlay()`。

我们对齐情况：
- **① 本轮补上**：新增 `CardView.CleanStatusAnimsClearAll()`（类尾，`CardView.cs:14773`）→ `TraitFrames.Clean(null, clearAll: true)`
  （两本字典 `PlayFrames`/`TriggerFrames` 全清，收尾 = `AnimFXController.Exit()` 的等价 `WarpforgeEffectPlayer.Exit()/Kill()`，
  判据在 `TraitFrames.Clean` 的 doc）。调用点由 `StopInPlayAnims` 第一句给出（`CardView.cs:14480-14482`）。
  ⚠️ 用 `GetComponent` **不用** `TraitFrames.Attach` —— 后者会**顺手建组件 + `effectsAnchor` 子节点**。
- **② 无落点**：原版那批「过渡对象」在我们这边**就是 DOTween 补间本身**（没有可 Destroy 的临时件），
  `StopInPlayAnims` 的 `trs[i].DOKill()` 已覆盖；它那条「没有 target 的 `To` 杀不掉」的边界也写在方法上。
  ⚠️ 另：`activeAnimsInPlay` 在反编译全树**只有读/清、没有写入点**（写入方的方法体没产出）⇒ 那批对象是什么**查不到**（不影响结论）。
- **③ 无落点（铁律 11 例外①）**：`BattleAnims.summonSickness` 是**空 AssetReference**
  （实读 `d:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/BattleAnims.json`：
  `summonSickness = {"m_AssetGUID":"","m_SubObjectName":"","m_SubObjectType":""}`；全库**只有这一份** BattleAnims 资产）
  ⇒ `StartSummonSicknessAnim` 的 `RuntimeKeyIsValid()` 恒假（`out_cpp2il_dev_ilspy/BattleCardUI.cs:2448`）
  ⇒ 那个 frame **永远不会被创建** ⇒ 销毁它是恒 no-op。
- **④ 无落点**：表里没有对象（同 ②）。

### 856 `RemoveInHandEffects`（建议 D）
- 原版两句：`CardScript__RemoveInHandEffects.c:5-6` = `cardUI.StopReflected()`；`BattleCardUI__StopReflected.c:5-20` =
  `if (reflectedFrame(BattleCardUI +0x2b8) != null) Destroy(it)`（没有别的逻辑）。
- `reflectedFrame` 的**唯一创建点** = `BattleCardUI.ShowEphemeral()`（`out_cpp2il_dev_ilspy/BattleCardUI.cs:2578-2599`）：
  `if (battleAnims.reflectedPrefab != null) { Instantiate(...); reflectedFrame = go; SetupStatusFrame(go); }`。
- 🔴 **`BattleAnims.reflectedPrefab` 是空引用**：`{"m_FileID": 0, "m_PathID": 0}`
  （同一份 JSON 里 `reflectedCreationAnim` / `reflectedDestroyedAnim` / `codexActivePrefab` / `stealthPrefab` **也都是 0**）。
  ⇒ `reflectedFrame` **恒 null** ⇒ `StopReflected` **恒 no-op** ⇒ `RemoveInHandEffects` **没有任何可见副作用**。
  ⇒ 判 **D**（铁律 11 例外①：原版本身没有；⛔ 别为一颗永远不存在的 frame 造一层）。
  （旁证：`TraitFrames.cs:61/532-537` 的 `MissingFrames` 早把 `reflectedFrame` 归进「原版空引用 ⇒ 按例外①结案」。）

### 908 `SetStateInHand`（视图侧零缺口，维持 A）
六步逐一（`decomp_full/CardScript__SetStateInHand.c:14-57`）：

| 原版步 | 我们 | 归属 |
|---|---|---|
| ① `transform.SetParent(BattleManager+0xd0)` | `BattleDriver.cs:8178`（取消支：`card.transform.SetParent(transform, true)`） | 驱动 |
| ② `IsWarlordEquivalent(rawCard)` ⇒ LogError + return | **没有**（`GameStaticData.IsWarlordEquivalent` 在 `dump.cs:119718`，**方法体没产出**） | 引擎（E1RNG） |
| ③ `cardState = 1` | 视图侧**不存**这一列（`ShowCardBeingDrawn` 的 doc 已写明） | 引擎 |
| ④ `DisplayCreatedByText(1)` | `CardView.SetCreatedBy(...)`（`CardView.cs:3524`），18 档表**逐值照抄**（`:3482-3516`），`inHand=1` ⇒ `Hidden`（静置手牌不印） | 视图 ✅ |
| ⑤ `SetCardTextInHand(isPlayer)` = `ToggleSimplifiedView(1, GameStaticData.cardsTextInHandFormat)`（`BasicCardUI.cs:529-532`） | `CardView.HandTextFormat` / `SetHandTextFormat` / `ComputeTextState`（`CardView.cs:5105-5190`），且 `SetFace(Hand)` 在 `CardView.cs:2380` 重建文字层 ⇒ 换形态即生效 | 视图 ✅ |
| ⑥ `PlayerHand.SetParentOfCard(this)` | `SyncHand`（驱动） | 驱动 |

两侧的档位默认都是 **0（Default = 不改）** ⇒ 默认无行为差异。施工单「我们缺什么」列是 10-18 之前的快照，已被判词栏（E3P13）推翻。

### 806 `MoveBackToHand`（视图侧完成）
`CardFeel.MoveBackToHand`（`CardFeel.cs:3701-3731`）：① `SetQueue(handQueue)`（= `SetSortingLayer(Hand)`）·
② `StopInPlayAnims()`（**本轮起含 `CleanStatusAnims(true)`**）· ③ 部队 ⇒ `DissolveMinionCard(false)` 的等价
（α→1 + `body3D` 关 + **残骸体撤场**〔上批补〕）／战术·附魔且 `targeted` ⇒ `UnDissolveWholeCard` · ④ `SetStateInHand` 归驱动。
仍欠两条（都不在视图层）：`OutFromPlayingAreaAnim` 入口（账 819 已 A，我们无「出牌区」概念）、`SetStateInHand` 的挂父 + `SyncHand`。

## ③ 没查清 / 无落点
1. `activeAnimsInPlay`（`BattleCardUI +0x258`）**没有任何写入点**在反编译产物里 ⇒ 原版往那批塞什么，查不到（不影响我们的等价结论）。
2. `GameStaticData.IsWarlordEquivalent`（`dump.cs:119718`）**无方法体**（724 条无体之一）⇒ 908 的 ② 那道闸**判据空**，⛔ 别猜它的语义。
3. 908 ⑤ 的门在原版还有 `isPlayer && (IsDuringBattle() 或 matchType == 0xc)`（`.c:41-50`）；我们的 `ComputeTextState` 只要 `_faceMode == Hand` 就套档位
   ⇒ **对手手牌**理论上也会套（我们视图里没有 `isPlayer` 列）⇒ 记成已知边界，⛔ 不因此新加一个标志位。

## ④ 改动清单（第二批）
`CardPresentation/Core/CardView.cs`（+24 行，净 +24；**行尾仍纯 LF**，HEAD 也是 LF）：
1. `StopInPlayAnims` 第一句加 `CleanStatusAnimsClearAll();`（**与 `var trs = ...` 同行**，保持行数中性 —— 账在 `CardView.cs` 里引用到 **14706** 行，插行会让 14571 / 14607 / 14706 三条漂移）。
2. 类尾（`14773`，在最后一条账引用之后）新增 `void CleanStatusAnimsClearAll()` —— 判据、`Attach` 陷阱、③ 的空引用证据全写在它的 doc 上。

**账补丁**：`账/_parts/B9_E2UDW.tsv` = 表头（11 列，逐字抄主表）+ 三行（`DissolveMinionCard(bool dissolveY)` / `UnDissolveWholeCard()` / `UndissolveToDeck()`）。
`状态` / `等级` **原值未动**（C·C·B / P1·P1·P2），只改 `差异` + `备注` + `轮次 = B9_E2UDW`；幂等（生成器检测到 `B9_E2UDW` 已在该行差异列就不再追加）。

### 顺手发现（⛔ 未改，交调度台分流）
1. 🔴 **`cardsTextInHandFormat` 有玩家设置入口，我们没有**：原版设置页 `GraphicsTab.cs:236` 写 `GameStaticData.cardsTextInHandFormat`，
   并落盘到 `LocalPlayerData.cardsTextInHandFormat`（`PlayerDataManager.cs:10243`）。我们 `CardView.SetHandTextFormat` 目前**只有编辑器探针**在调
   （`Editor/CardFaceProbe.cs:1607+`）⇒ 建议在 `Shell/` 设置页补一颗（⛔ 不在本批白名单）。默认值两侧都是 0 ⇒ 今天无可见差。
2. 账 856 的「等级」建议由 P2 改 **D**（证据 = `BattleAnims.reflectedPrefab` 空引用 + 唯一创建点 `ShowEphemeral`）。
3. 账 930 的「我们缺什么」列可改成「②③④ 无落点，① 已由 `CleanStatusAnimsClearAll` 补上（`CardView.cs:14482/14773`）」。

### 该跑哪些宿主自检（⛔ 我没跑，Unity 批处理归调度台）
- **必跑**：`BattleScene.Run` —— `StopInPlayAnims` 的五个生产调用点全在战斗链（`BattleDriver.cs:22480 / 26031 / 26703` + `CardFeel.cs:1874 / 3705`）。
- **建议一起跑**：`AnimFXCheck.Run`（状态框那一族 —— 本轮新增了 `TraitFrames.Clean(clearAll: true)` 的调用）。
- **不必**：`RuleEngineTest.Run`（未碰引擎）· 其余壳宿主（未引用 `CardFeel`/`CardView.StopInPlayAnims`）。
