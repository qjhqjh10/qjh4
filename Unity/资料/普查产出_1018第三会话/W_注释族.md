# W_注释族 —— 执行写手（纯注释三件：`A338` 卡组子集 5 处 + `A895` 5 处 · `A942` · ③ 过期注释）

> 2026-10-18 · **一行行为都没改**（只改注释 / 断言文案串）· **没跑 Unity** · **没动 git** · **没碰正本**。
> 类型检查：**运行时 0 错 / 编辑器 0 错**（`TMPDIR=/tmp/wf_cmt bash 工具/typecheck.sh`）。
> ⛔ 全程**没写一个「订正后的行号」** —— 一律按**符号**认。

---

## A338 / A895 —— 逐处：文件 · 原引用（原文） · 真身符号（怎么核的） · 新写法

### A338 · 卡组子集 5 处

| # | 文件（符号定位） | 原引用（原文） | 真身符号（怎么核的） | 新写法 |
|---|---|---|---|---|
| 1 | `Shell/PointerLayer.cs` 文件头「能点的先例」段 | ``（`Deck/DeckRuntime.cs:732 HandlePointer()` / `:1379 Hit()`）`` | 原文里**符号名已经给了**；逐个实读确认：`DeckRuntime` 的 `void HandlePointer()` 头三句就是 `Mouse.current` 轮询、`bool Hit(ImageQuad q, Vector3 wp)` 体就是矩形命中 | ``（`Deck/DeckRuntime.cs` 的 `HandlePointer()` / `Hit()` —— ⛔ 按【符号】认，别抄行号）`` |
| 2 | `Shell/MenuScroll.cs` 文件头「滚轮手感」段 | ``（`Deck/DeckRuntime.cs:1106-1113`）`` | 真身 = `DeckRuntime.HandleScroll`：实读其体第 5 行正是 `float step = dy * 0.4f;`（与原文「用 `dy * 0.4f`」逐字对上） | ``（同上，`Deck/DeckRuntime.cs` 的 `HandleScroll` —— ⛔ 按【符号】认，别抄行号）`` |
| 3 | `Shell/CollectionWindow.cs` 的 `TextAligned`（折行那一档） | ``与 `Deck/DeckRuntime.cs:2982` / `:3110` 那两行`` | 用 `git show 321639d:…DeckRuntime.cs`（该引用的**出生提交** 2026-10-08）读 `:2982` / `:3110`，两处**都是** `lb.SetWrapping(c.LabelWrap == 1);`；再按**内容**回到 HEAD 定位其宿主方法 = `RefreshFilterCells` / `RefreshCosmoFilters`（`DeckRuntime` 里那句自注「本行与上面 `RefreshFilterCells` 那处同一条口径」互相印证） | ``与 `Deck/DeckRuntime.cs` 的 `RefreshFilterCells` / `RefreshCosmoFilters` 里那两行`` |
| 4 | `Editor/DeckScene.cs` 的「藏住的件不许吃点击」段 | ``（`DeckRuntime.cs:1606-1610`，`HandleButtons` 是**最后一站**）`` | 上一行已逐字写出该链的**四个符号**；实读 `DeckRuntime` 分派点：`HandlePoolClick(...) -> HandleDeckRowClick(...) -> HandleCosmeticClick(...) -> HandleButtons(px)`（四者都有定义），确认链序与符号名 | ``（上面那条链就在 `DeckRuntime.cs` 里，`HandleButtons` 是**最后一站** —— ⛔ 按【符号】认，别抄行号）`` |
| 5 | `Editor/BattleScene.cs`（「落点的场景名」那条断言串） | ``本仓同一扇主菜单见 `Deck/DeckRuntime.cs:2081``` | 真身 = `DeckRuntime.BackToMenu()`：其体内 `LoadScene("MainMenu")`（含批处理短路分支）；核过这是全文件**唯一**写 `"MainMenu"` 场景名的地方 | ``本仓同一扇主菜单见 `Deck/DeckRuntime.cs` 的 `BackToMenu()``` |

### A895 · `Editor/BattleScene.cs` 5 处

⚠️ **现状要说清**：账上那 5 处**已经被别人「刷新过行号」了**（`ff6a612` 2026-10-18 那次），所以今天找它们是**按内容**找的，不是按账上那些数字。

| # | 文件（符号定位） | 原引用（原文） | 真身符号（怎么核的） | 新写法 |
|---|---|---|---|---|
| 1 | `BattleScene.cs` 的 A849 结案块（`SyncBoard` / `SetLayer` 那一节） | `` `BattleScene.cs:15458`（= `BuildScene`，…）`` | 原文自己写了「= `BuildScene`」；实读 `driver.use3DBoard = boardCam != null;` 那一行确实落在 `BuildScene(...)` 体内（同段注释另称「`BuildScene` 末尾那两行」，互相印证） | `` `BuildScene` 里那句（本文件，**在建任何卡视图之前**按 `boardCam != null` 定死，一次成型）`` |
| 2 | 同上（同一行的后半） | ``+ 本节的 `:13068` / `:13109`（自检翻转…）`` | 「自检翻转」→ 按内容找本节的 `driver.use3DBoard = false` / `= true` 两处赋值（全文件只有三处写点，第三处即 #1） | ``+ 本节的 `driver.use3DBoard = false` / `= true` 两处（自检翻转，且**翻回来了**）`` |
| 3 | `BattleScene.cs`（A304/A337 诊断块的「两处都错①」） | ``（`Hand/CardInteraction.cs:465`）`` | `:465` **已漂**（现读 = `ExplainDrop` 的 doc）。按内容找真身 = `CardInteraction.Release(...)` 里那句 `DeploySequence.Play(card, which.DropTargetWorld(land), …)`（现读该句宿主方法 = `Release`，实读确认） | ``（`Hand/CardInteraction.cs` 的 `Release` 里那个补间终点）`` |
| 4 | 同上（A337 更正句） | ``（`CardInteraction.cs:465` = `which.DropTargetWorld(land)`，…）`` | 同 #3 | ``（`Hand/CardInteraction.cs` 的 `Release` 里那句 = `which.DropTargetWorld(land)`，…）`` |
| 5 | 同上（改坏法那句） | ``（= 与 `CardInteraction.cs:465` 的终点**不一致**）`` | 同 #3 | ``（= 与 `Hand/CardInteraction.cs` 的 `Release` 的终点**不一致**）`` |

> ⚠️ #3 那句的**语义**（「落位补间飞向的是 `SlotPosition(land)`」）是**记 A304 当时那一态**，
> ⛔ 我**没动它的语义**（A337 之后真身已是 `DropTargetWorld`）—— 只把坐标换成稳定符号。
> 现状全部仍在 `Editor/BattleScene.cs` 一个文件内，**5 处** ✓（与账上「全在 `BattleScene.cs`」一致）。

---

## A942 —— 现读原文 + 我做了什么

**落点**：`RuleEngine/Editor/RuleEngineTest.cs` 的 `TestDeathAccountsAfterBacklash`（账上写 `Editor/BattleScene.cs` 是**错的**，与巡检同判）。

**现读**：那一段**已经有**一条 `🚧 护栏` 注释（讲 2026-10-17 收口时改正文、两套文法串一句、`EffectText` vs `EffectSpec`、判据 `D_Backlash条件句_诊断.md`），
但**「另开一件」这条裁定本身没落进去**。

**我做的**（**只加注释，零行为改动**）：在那条护栏块**之后、`CheckTrue(probe.TriggerOps(...))` 之前**补一段裁定注释，写明三样：
- **裁定**：另开一件 = 账 `A942`，⛔ 不在 F1 那件里顺手改；
- **为什么**：① `ConditionHolds("deaths")` 的 `MatchesKind(卡, "troop")` 与夹具 `type == "unit"` **能不能过谁都没验** ② 正文一改，①/② 两条断言的**期望值本身**要重定（现在钉「3 点**没打出来**」）⇒ 那是**改用例语义**；
- **出处**：`资料/待办判据_1018.md`「2026-10-17 · 收口自检第一次大修（F1–F4）」那一行末段（原文「🔴 裁定（它请我裁的 `:16288` 那条护栏）：**另开一件**」）+ 本轮只读现核 `资料/普查产出_1018第三会话/RECON_剩余账册.md` 的 `A942` 一节（含「落点写错」那条订正）。
- 并写明**现状**：①② **一条都没判** ⇒ ⛔ 别把「正文已经改通」当 `A942` 已收口。

⚠️ 我**没有**去改正文、也没有动任何期望值 —— 那正是裁定说「另开一件」的部分。

---

## ③ DraggableController —— 改前 / 改后

**位置**：`Core/DraggableController.cs` 的 `CosmeticDraggingController.Initialize`（`SetTexture(t, keepAspect: true)` 上面那段）。

**改前**
```
//   —— 本处**正是漏的那一处**：`DeckRuntime` 建这一格时给的是 `SetAspect(250 / 405)`
//   （= 布局框 250×405，×`m_LocalScale 0.6` 后**画出来 150×243**，原版 `Collection Cosmetic`），
//   被换成贴图比例之后画出来就成了 `243 × (707/981) = **175.1284**`（实测逐位吻合：
//   拖的是 `Cardback_AM_Shield of Humanity` = 707×981）⇒ `DeckScene` 那条「预览画出来的宽 = 150」红。
```

**改后**
```
//   —— 本处**正是漏的那一处**：`DeckRuntime` 建这一格时给的是 `SetAspect(CosmImgW / CosmImgH)`
//   （= 图框 `CosmImgW`×`CosmImgH` = **220×330**，×`m_LocalScale 0.6` 后**画出来 132×198**，
//   原版 `Collection Cosmetic` 的 `content > Image_…` 那颗 `Image`；⚠️ 那两个量在
//   `DeckRuntime` 里是**常量** `CosmImgW` / `CosmImgH` —— ⛔ 按【符号】认，别抄行号），
//   被换成贴图比例之后画出来就成了 `198 × (707/981) ≈ **142.7**`（= 上面那两个数算出来的，
//   ⛔ 不是实跑读数；同一张图：拖的是 `Cardback_AM_Shield of Humanity` = 707×981）⇒
//   `DeckScene` 那条「预览**画出来**的宽 = **132**」红。
```

**核过**：`DeckRuntime` 里 `const float CosmImgW = 220f, CosmImgH = 330f;` · `quad.SetAspect(CosmImgW / CosmImgH);` · 同文件自注「画出来 = 220×330 × 0.6 = 132×198」· `Editor/DeckScene.cs` 那条断言文案现读 = 「预览**画出来**的宽 = **132**（= `Image` 的 `sizeDelta` 220 × `m_LocalScale` 0.6）」。

⚠️ **`142.7` 是算出来的，不是实跑读数** —— 我**没跑 Unity**；已在注释里如实标明。
⚠️ **`SetAspect(250 / 405)` 那处（卡背【格】的矩形比例）我【没动】** —— 按简报，那是另一个口径问题。

---

## 类型检查：运行时 0 错 / 编辑器 0 错

`TMPDIR=/tmp/wf_cmt bash d:/4/Unity/工具/typecheck.sh`（改完 7 个文件后跑）⇒ `运行时错误数: 0` · `编辑器错误数: 0`。
**行尾**：全程用 `Edit` 工具、⛔ 没用 `sed -i`；每个文件改完 `git diff --numstat` 都核过（`1/1` 级，**无整篇翻**）。

---

## 没查清 / 停手的地方

1. 🔴 **`DeckScene.cs` 同段里还有两处行号引用我【没动】**：`（`:1634` 要求 `_tab==0`）` 与 `（`:1159` 要求 `_tab==2`）` —— 属 A338 那一族，但**不在「卡组子集 5 处」这份清单里** ⇒ 按简报「⛔ 别去扩面」**只报告**。（`Editor/BattleScene.cs` 里同类「指向本文件自身」的行号引用还有约 20 处，同理没动。）
2. ⚠️ **`A942` 的两条待判（①②）我一条都没判** —— 简报明确「若看不出裁定是什么就只报告」，我看得出**裁定**（另开一件），但 ①② 属**要判的东西**，不是注释能定的 ⇒ **如实挂着**。
3. ⚠️ **`A895` 账上记的「真身」数字今天同样过期**（`14593` / `12541` / `12582` 都找不到）⇒ 印证了「连订正后的行号都不能信」。我全程**按内容/符号**定位，**没抄任何一个数字**。
4. ⚠️ **没跑 Unity 自检**（按简报禁止）。本件跨 `BattleScene` / `DeckScene` / `CollectionScene` 三个宿主，收口时若要覆盖面，应跑 `BattleScene.Run` + `DeckScene.Run` + `CollectionScene.Run`。
5. ℹ️ **`DeckScene.cs` / `CollectionWindow.cs` 在会话开始时就是 M（有别人的在途改动）** —— 我只动了自己那一行，`numstat` 里多出来的差额不是我的。
6. 🆕 **顺手发现：`RuleEngineTest.cs` 里【还有第二个写手在飞】（⚠️ 请调度台核「一个文件一个写手」）** —— 工作区里另有一段**不是我写的**未提交改动：`RuleEngineTest` 的汇总前新增了 `🆕 2026-10-18（第三会话 · 引擎族收口 5 件）` 一节，
   `Section(...)` + 五个 `Step(TestA947NoncombatantAttackBan / TestDiedThisTurnDuringBacklash / TestA974NotePlayedAfterPlayBroadcast / TestA976LastCreatedNotCarriedAcrossCards / TestA963HandListenerSelfDoesNotSpread)`。
   我**一个字节都没碰它**；类型检查当时是 **0 错** ⇒ 那五个方法已存在。（⛔ 我**没去核**它们的实现，那不是本件的活。）
