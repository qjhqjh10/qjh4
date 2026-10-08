# RECON · 外壳族 A 表现核（只读）

> 只读现核（**未跑 Unity**）。生产根 = `d:/4/Unity/MyGame/Assets/CardPresentation/`。
> 行号会漂 ⇒ 每条都给了**符号名**。⚠️ 本文件是**待办判据的现核**，不是正本；数字/清单请由调度台合并进正本。

---

## A834 —— 状态：**未做**（裁定已下、代码未动）

- 证据：`Shell/CollectionWindow.cs` 的 `RebuildFilterRowsNow` 头（现 ≈`:1772-1790`）**没有任何「摆回基准位」的语句**——
  它只做三件：① `_flt.RowsBuiltOffBase = _flt.HasBasePos && _flt.Slide < 1f;` ② `SetActive(true)` ③ 销毁子件后重建。
  `grep DrawerHome Shell/` = **零命中**；`FilterPanel.BasePos` 只在 `ApplyDrawerSlide`（≈`:536`）**首次捕获**，全文件没有复位点。
- 现在的替代物 = `ApplyDrawerSlide` 的 **④**（≈`:558-599`，`prevSlide < 1f && RowsBuiltOffBase && Scroll != null` 那一支）。
- 判据：`资料/待办判据_1018.md` §A834（裁定 = **甲：建之前把抽屉摆回基准位**）；同类现成写法 = `Deck/DeckRuntime.cs:1602-1607 DrawerHome`（调用点 `:4993` / `:5280`）。
- 要改的落点：`Shell/CollectionWindow.cs` 的 `RebuildFilterRowsNow`（**函数头**，或收尾 ④ 前）——`FilterPanel.HasBasePos/BasePos` 两个字段**已经在了**，等价于把 `DrawerHome` 抄进来。只碰一个宿主 ⇒ 按铁律 12 **只跑 `CollectionScene.Run`**。
- 🔴 **验收口今天不存在**：`Editor/CollectionScene.cs` 里所有量落点的断言（`Cell_owned` 中心 x = `167.905` 那族，≈`:5824`/`:5835`/`:5861`/`:5894`）**都在滑动结束后**量；过渡帧只有「**面板自己**挪了半个行程」（≈`:4099-4106`）。要按 §A834 的现成公式补，判别式 = 同帧再断「`Cell_owned` 与它子件 `Background` 的 x 相等」。

## A866 —— 状态：**已做（2026-10-17）**，只欠核销

- 证据：4 处**全部**转调共件 —— `TutorialModePopup.cs:385` · `LiveOpsEventWindow.cs:789` · `DailyStreakPopup.cs:526` · `EnergySinglePlayerOnlyEventWindow.cs:495`（`grep WindowHeader.WithBackButton` = 4 个生产调用点）。
  共件 = `Shell/MenuWindowBase.cs:585` 的 `public static class WindowHeader`，建法在 `:721 WithBackButton(Transform, Spec)`。
- 被抄两份的常量**已收口**：`WindowHeader.PlateBorder/PlateTexW/PlateTexH`（`:617-618`）；`DailyStreakPopup.cs:110-116` 与 `LiveOpsEventWindow.cs:195-197` 现在**只剩指针注释**（逐值相同那一组已消失）。
- 四份**差异一个都没被抹掉**，全变成 `Spec` 上的显式字段（`TitleFit` 三档 · `BackStyle` 三档 · `TitleVAlign?` · `WingName` · `TitleFitW/H`）—— 逐格对照 → `资料/普查产出_1018/S5_A866窗头收口.md` §2/§4。
- 判据：用户/调度台口径「同一原版 prefab 别抄四份」（CLAUDE.md §三）；白名单指定放 `MenuWindowBase.cs`（`WindowHeader` 的 doc 自陈）。
- 状态：代码完成、`ShellScene` 944/0 已绿；**核销仍被 `A983`（`MainMenuScene` 12 红）卡着**。

## A967 —— 状态：**未做**（删一行即修）

- 证据：`Shell/LiveOpsEventWindow.cs` 的 `BuildHeader` 里 `WingName = "Nine",`（现 ≈`:801`；上一行 `:799-800` 是说明注释）。
  共件缺省 = `WindowHeader.WingName = "Header Background (1)"`（`MenuWindowBase.cs:596`）+ `Spec.WingName = WindowHeader.WingName`（`:686`）。
- ⇒ **删掉那一行**（连 `:799-800` 的注释）即与另三扇对齐；⛔ 不用改共件、不用改别的窗。
- 断言：全仓唯一读 `Header Background (1)` 的是 `Editor/MainMenuScene.cs:3008-3009`，而它读的是 **`tut.transform`（`TutorialModePopup`）** ⇒ **没有断言盯 `LiveOpsEventWindow` 那一颗**（本条**零覆盖**）。
- 判据：原版那颗尖角名 `Header Background (1)`（`menu_dump` 实读，出处同 `WindowHeader.WingName` 的 doc）。

## A968 —— 状态：**未做**，但 🔴 **判据本轮补齐了**（原来缺的那三个字段）

- 现状：`Shell/DailyStreakPopup.cs` 的 `BuildHeader` 传 `TitleMode = WindowHeader.TitleFit.SpacingOnly`（≈`:534`）⇒ 共件那一支**只调 `SetCharSpacing`、不调 `SetAutoFitBox`**（`MenuWindowBase.cs:755-757`）。
- 另三扇实参：`TutorialModePopup` / `LiveOpsEventWindow` = `FitAfterSpacing`；`EnergySinglePlayerOnlyEventWindow` = `FitBeforeSpacing`；三档**共用**同一组自适应常量 `WindowHeader.TitleAutoMinPx 18f · TitleAutoMaxPx 67.55f · TitleAutoBasePx 36f`（`MenuWindowBase.cs:608`），调用点 `:748`/`:753`。
- 🔴 **原版判据（本轮亲读解包资源；这是之前缺的那一格）**：
  - 对象：`d:/2/新解包资源/assets_full/bundle_menus_assets_all/GameObject/Window Title_263730375434445483.json`（GO 名 `Window Title`；父链现读 = `Window Title < Header Background < Header With Back Button < <Daily Streak Popup 根>`）。
  - 它的 TMP 组件 = `MonoBehaviour/MonoBehaviour_3324232435684942507.json`（该 GO 的组件里唯一带 `m_text` 的那颗）：
    `m_text "Daily Streak"` · `m_fontSize 67.55` · `m_fontSizeBase 36.0` · **`m_enableAutoSizing = 1`** · **`m_fontSizeMin = 18.0`** · **`m_fontSizeMax = 67.55`** · `m_characterSpacing 5.0` · `m_HorizontalAlignment 1 (Left)` · `m_VerticalAlignment 8192 (Capline)`。
  - 同包旁证：8 颗 `Window Title` **逐颗现读、逐值相同**（另 7 颗 `m_text` 是 `Game mode`/`Game Mode`）。
  ⇒ **结论**：原版那一颗**是自适应的**，区间与共件常量**逐值相同** ⇒ `DailyStreakPopup` 应当接自适应（`TitleMode` 改成 `FitAfterSpacing` 或 `FitBeforeSpacing`，两档静态收敛 —— 见下「需先裁 ①」）。
- 要改的落点：`Shell/DailyStreakPopup.cs` 的 `BuildHeader` 的 `Spec.TitleMode`（+ 一并删掉 `:534` 那句「只补了这两笔、没补自适应」的注释）。
- ⚠️ **副作用要先核（不是判据，是风险）**：打开自适应后 `"Daily Streak"`（12 字）按标题框会**收字号**（今天恒 67.55）。今天与该颗有关的断言是 `Editor/RewardsScene.cs:7048-7058`（`CharSpacing == 5` + 真渲染**左缘 155.0**）—— **两条都不吃字号** ⇒ 预期不红，但**没有量字号/宽度的断言**，落地后必须跑一次 `RewardsScene.Run`。

## A932 —— 状态：**未做**（代码零命中）

- 证据：`Shell/SearchingOpponentWindow.cs` 全文只有 `Title`（`MenuDraw.Text(..., "Searching opponent", 36f, ...)`，≈`:200-201`）、两个 `Player Name`、一颗 `Cancel Match` —— **没有任何"状态话/提示行"节点**。
- 判据：**用户已拍板接入**（→ 自建一颗原版没有的节点）。权威记录 = `资料/待办判据_1018.md` §B27 那段末尾（原文「⛔ 硬结论…要接得先裁『给那扇窗新造一个原版没有的节点』」—— 用户已翻案为「**接入**」）。
- 要改的落点：`Shell/SearchingOpponentWindow` 的 `Build(...)`（那一颗 `Title` 附近）；**样式照同族** `Shell/SearchingMatchPopup.cs` 的 `Main Search message`（≈`:239`，原版参数 = autosize 4~50 · base 36 · 折行 700 · Center/Middle）。
- 断言宿主：**`NetSelfTest` 至今一行 UI 都不建**（§B27 已如实记）⇒ 落地要顺带把宿主搭出来。
- 没查清：新节点**摆哪**（那扇窗原版没有一行放得下状态话 ⇒ 位置是我们挑的）。

## A944 —— 状态：**未做**（原版形状本轮亲读坐实）

- 现状：`Deck/DeckRuntime.BuildCosmeticDrag`（≈`:2272-2290`）建**一块 quad**：`ImageQuad.Create(..., U(PrevH * PrevScale) /* 405×0.6 */, ...)` + `SetAspect(PrevW/PrevH)` ⇒ 视觉 **150×243**。
- 🔴 **原版判据（本轮亲读 `bundle_menus_assets_all`，逐字段）**：
  - `Collection Cosmetic`（RT `3595378309407108900` · sizeDelta **250×405** · `m_LocalScale 0.6` · anchor(0,1) · pos(75,−121.5)）
  - ├ `content`（sizeDelta **0×0** · anchorMin(0,0)/anchorMax(1,1) = **撑满父件**）
  - └（`content` 下）`Image`（GO `Image_-6633270251387818204` · RT `-3373774588741030108` · sizeDelta **220×330** · anchor 居中 · anchoredPosition **(0, 0.5)** · `m_Type = 0 (Simple)` · **`m_PreserveAspect = 1`** · `m_RaycastTarget = 1`）
  ⇒ 视觉 = 220×330 × 0.6 = **132×198** ✓（与账上「`150×243 > content > 132×198`」逐值吻合）。
- 要改的落点（2 处）：① `Deck/DeckRuntime.BuildCosmeticDrag` 里那颗 quad 的**高**（`U(PrevH*PrevScale)` → `U(330f*PrevScale)`）与 `SetAspect`（`PrevW/PrevH` → `220f/330f`）；② `Editor/DeckScene.cs:926-927` 那两条断言（`150`/`243` → `132`/`198`）+ `:922` 的注释。
- 没查清：`content` 那一层**要不要建** —— 它 sizeDelta 0×0、纯撑满容器 ⇒ **视觉零影响**，判据不指向「必须建」（见「需先裁 ④」）。

## A985① —— 状态：**未做**（两处都还在）

- 证据：
  - `Deck/DeckEditorState.cs` 的 `TryAdd(CardDef, out string why)`：`why = DeckRules.Describe(e);`（≈`:265`）—— **没有 `Loc.T`**。
  - `Shell/DeckInfoPopup.cs` 那句 `Debug.Log("[DeckInfo] …" + DeckRules.Describe(DeckRules.Validate(...)))`（≈`:575`）—— 同样**没有 `Loc.T`**。
- 判据：`RuleEngine/Core/DeckRules.cs:340-343` 的 `Describe` 现在 `return e == DeckError.None ? "" : TermPrefix + e;`（**只出词条键**）；已改对的出口 = `Deck/DeckRuntime.cs:3470` `DeckErrorText(e) => Loc.T(DeckRules.Describe(e))`。
- 要改的落点：上面两个调用点（包 `Loc.T(...)`）。⚠️ `DeckEditorState.why` 被当**人话**用（会进 `DeckScene` 十几条断言消息）⇒ 见「需先裁 ⑤」。

## A985② —— 状态：**已查清**（答案与假设不同：**不指向任何方法**）

- 🔴 **原版判据（本轮亲读，13 个战场场景全查）**：
  `d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena*/GameObject/SkipTutorial Button.json` → 组件 `EverguildButton`（脚本 pid `1015376240363272691`）的 **`m_OnClick.m_PersistentCalls.m_Calls` = 空表**（**13/13 全空**，逐场景核过）⇒ **它不指向任何方法**，更不是「指向一个不存在的方法」。
- 接线在**代码**里（判据链完整）：
  - 场景里那颗钮的 GO 父链（现走 RectTransform/GameObject 图）= **`SkipTutorial Button < Bottom buttons < BattleSettingsPanel < Safe area FrontCanvas < FrontCanvas < Canvas`**；`BattleSettingsPanel` 的 `m_IsActive = false`（窗，按需开）。
  - `BattleSettingsPanel` 上挂的 `BattleSettingsWindow` 实例 = `MonoBehaviour/MonoBehaviour_4034.json`，它的 **`skipTutorialButton` 字段 → 组件 pid 4945**（= 那颗钮）。
  - `BattleSettingsWindow.Awake()` 在实例偏移 **`0xC8`**（= `d:/2/tools/il2cpp_out/dump.cs:76533` 的 `private EverguildButton skipTutorialButton; // 0xC8`）上做 `onClick(+0x100).AddListener(<MethodInfo DAT_18423f3f8>)`。
  - 地址反查（`d:/2/tools/il2cpp_out/script.json` → `ScriptMetadataMethod`）：**`0x423F3F8 = Method$BattleSettingsWindow.SkipTutorialButtonOnClick()`**；方法体在 `d:/2/tools/decomp_full/BattleSettingsWindow__SkipTutorialButtonOnClick.c`（`WindowsManager.CloseWindow` + `BattleManager.ClickSkip`）。
- ⇒ **「跳过教程」在发行版里不是死的**（方法存在 + 接线在 `Awake`）。配套同名账 `BattleSettingsWindow.ClickSkipTutorial`（`decomp_full/BattleSettingsWindow__ClickSkipTutorial.c`）也在。
- 🔴 **顺带抓到我们一条错注释（铁律 5 该订正，不属本条编号）**：`Battle/BattleDriver.cs` 的 `ApplyTutorialSkip`/`HandleTutorialSkip` 那段 doc 写着「HUD 底部那颗（`Bottom buttons/SkipTutorial Button`）的处理器活在**场景的序列化 `onClick`** 里…**是两颗不同的钮**」—— **两条都不成立**：① 场景 `onClick` 是**空的**；② 全库只有**一颗**（父链证明它在 `BattleSettingsPanel` 下）。
  同错还有 `Battle/TutorialOverlay.cs` 的类 doc（「原版那六层**本来就挂在同一个 `Tutorial` 根下**」—— 含 `SkipTutorial Button`）；⚠️ 同文件 `:31` 的树图**是对的**（把它画成 `Bottom buttons` 的子件、`Tutorial` 的兄弟）。

## A833（尾巴）—— 状态：**部分做**（生产侧有 clip 节点；那页断言仍**零**）

- 生产侧：`Shell/BattleLogTab.cs:89` `var vpVc = ViewportClip.Hang(mt, "Viewport", vp, Vector4.zero, Vector2Int.zero);` ⇒ **档案窗第 4 页那颗 `Viewport` 上「有」`ViewportClip` 节点**（现核确认，不再是未知）。
- 断言侧：`A833` 那两族全落 `Editor/MainMenuScene.cs`（`LeaderboardRow` ≈`:5681-5851` · `MatchLogRow` ≈`:9134-9280`），**宿主 = 排行榜窗 + `BattleLogPopup`**；`:9132-9137` 自己写着「两扇宿主（本弹窗与档案窗那一页）共用同一份 builder ⇒ 缺口是**两份一起**的」⇒ **`BattleLogTab` 那一族断言仍是零**。
- 要改的落点：`Editor/MainMenuScene.cs` **已有**的那个 `BattleLogTab` 段（≈`:3851` 起，`pp.Page(WindowTabType.ProfileBattleLog) as BattleLogTab`）里补一族；量法与形状**照 `:9134-9280` 抄**，⛔ 不另写一份。

## A971 —— 状态：**未做**（9 处措辞；按内容找到，行号会漂）

**全部在 `Battle/BattleDriver.cs`**，"三档" → "五档"：
1. 段标题横幅：`// 🆕 2026-10-18（A940 尾账）：**动作音效 + 聊天三档 + 「等提示」**`
2. `/// ⚠️ **聊天那三档不走这里** —— 它们的 sound 就是那句台词的 VO…`
3. `/// 聊天那三档要用它的立绘/卡名。认不出 ⇒ null…`（`ActingCardOf` 那段的 doc）
4. `/// 🆕 **聊天三档**（PlayerChat 50 / PlayerChatBig 55 / AiChat 60 / AiChatBig 65 / RadioMessage 90）× 三颗气泡…` ← **这一处自己就把 5 档列全了**（最明显的自相矛盾）
5. **出声文案**：`Debug.LogWarning("[Tutorial] 聊天三档要 UnitChatPanel，可它是 null ⇒ 这一句**没显示**…`
6. `…⇒ **引擎侧一个字节都不动**，要的只是**按节拍把它们演出来**（聊天三档 + 音效）。`
7. `// ⚠️ 走的是**表现那条链**（ApplyTutorialActionView）—— 它自己会放音效、演聊天三档；`
8. `// ⚠️ 聊天那三档在 PlayTutorialSound 里被跳过（它们的 sound 就是那句话的 VO…`
9. `// ---- ②′ 🆕 2026-10-18（A940 尾账 · 聊天三档）----`

- 五档实据：`0x32`=50 `PlayerChat` · `0x37`=55 `PlayerChatBig` · `0x3C`=60 `AiChat` · `0x41`=65 `AiChatBig` · `0x5A`=90 `RadioMessage`（与 `IsTutorialChatKind` 五个枚举值逐值对上；出处 = A 表 `A971` 行）。
- 🆕 **第 10 处（账上没列，本轮新发现）**：`Battle/UnitChatPanel.cs:90` 的 doc `/// <summary>🆕 2026-10-18（A940 尾账 · 聊天三档）：**第三颗气泡 RadioChat**…` —— 同一族措辞，建议一并订正。
- ⛔ **别一起改的「零接线」6 处**（历史/断言文案，**正确**）：
  ① `Battle/BattleDriver.cs` 的 `LeaveNetRoom` doc：`…而 **OnClosed 全仓零接线**（只有定义与三处 Invoke，生产侧没人订阅）⇒ 对面那台收得到、玩家看不到。同一族的还有 OnPeerLost（掉线那条）也零接线`
  ② `Editor/NetBattleTest.cs:570`「原来 `NetSession.OnClosed` / `OnPeerLost` 在**生产侧零接线**」
  ③ `Editor/NetBattleTest.cs:628` 断言文案「（原来零接线 ⇒ 一条都没有；实得 …）」
  ④ `Editor/NetBattleTest.cs:650` 同上（对面离开那条）
  ⑤ `Editor/NetSelfTest.cs:536`「M⑥ ★ 大厅阶段对面掉线要弹一条（原来零接线 ⇒ 一条都没有）」
  ⑥ `Net/NetBattle.cs:244`「账 `A880`：…原来**全仓零接线**（只有定义 + 三处 `Invoke`）」
  🔴 **但 ① 是例外**：它是**现在时**（"**`OnClosed` 全仓零接线**…生产侧没人订阅"），而 `Net/NetBattle.cs:279-280`（`_s.OnPeerLost += HandlePeerLost` / `_s.OnClosed += HandlePeerClosed`）与 `Net/NetMatchmaking.cs:134` **都已经接了** ⇒ 这半句**今天不成立**（属另一笔 · 铁律 5，⛔ 不属 A971 的 9 处）。

---

## ⛔ 判据不足 / 需先裁

1. **A968 的次序**：原版序列化字段**不表达**「先字距还是先自适应」（`MenuWindowBase.cs` 的 `TitleFit` doc 自陈「无牙口，两档静态收敛」）⇒ 必须自选一档。建议 `FitAfterSpacing`（另三扇里 2/3 是它）。
2. **A968 顺带发现（未成账）**：原版 8 颗 `Window Title` 的 `m_VerticalAlignment` **全是 `8192 = Capline`**，而我们只给 `DailyStreakPopup` 传了 `TitleVAlign = Capline`（`:536`），`TutorialModePopup` / `LiveOpsEventWindow` / `EnergySinglePlayerOnlyEventWindow` 的 `Spec.TitleVAlign` **空** ⇒ 走 `Label` 出厂档 `Middle`（`MenuWindowBase.cs:679-681`）。A712 阶段 2 的白名单（`资料/普查产出_1016/W16_A712阶段2.md:40` 第 28 行）**只收了 DailyStreak 那一颗** ⇒ 另 3 颗（同族旁证 7 颗）**疑似漏网**，与 `A848` 同族。**要不要成账 / 并 A848，请调度台裁。**
3. **A932 新节点的位置与尺寸**：原版那扇窗没有这一颗 ⇒ **无判据**。可参考同族那颗 `Main Search message`（700×148 · autosize 4~50 · Center/Middle），但那是**另一扇窗**的值 ⇒ 要拍板「照抄它 / 还是另定」。
4. **A944 的 `content` 层要不要建**：原版 sizeDelta `0×0`、纯撑满 ⇒ 视觉零影响；判据只钉住 `Image` 的 `220×330 + preserveAspect`。⛔ 别把「补 `content`」当成判据。
5. **A985① 的连带**：`DeckEditorState.why` 包上 `Loc.T` 之后，`Editor/DeckScene.cs` 那十几条断言消息会跟着变（今天是键名）⇒ 要不要连断言文案一起改，请裁。

## 🔴 顺手发现（超出本批账，未成账）

- **A985② 带出的我们自己的错注释**（见该节末）：`Battle/BattleDriver.cs`（`HandleTutorialSkip`/`ApplyTutorialSkip` 的 doc）与 `Battle/TutorialOverlay.cs:92-93` 的类 doc —— 两处都把 HUD `SkipTutorial Button` 说成「另一颗钮 / 挂在 `Tutorial` 根下」。
- **A971 的第 10 处**：`Battle/UnitChatPanel.cs:90`。
- **A971 「零接线」第 ① 处已过期**：`Battle/BattleDriver.cs` 的 `LeaveNetRoom` doc（现在时）。

---
（只读现核，**未跑 Unity**；所有"原版值"均来自 `d:/2/新解包资源/assets_full/` 的字段实读或 `d:/2/tools/decomp_full/` 的方法体，未用截图、未用猜测。）
