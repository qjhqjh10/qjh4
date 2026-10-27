# B9_W8PREB —— K-07 + K-09 + K-10（卡组/预组/轮抽线的剩余 C）

执行日：2026-10-27 · 代理：`B9_W8PREB` · 判据来源：`判据/B9_P0剩余C_派活包.md` §2 的 K-07 / K-09 / K-10 + `判据/B9_P0剩余C_施工单.tsv`
账补丁：`账/_parts/B9_W8PREB.tsv`（13 行 · 11 列 · 表头在 · 已过 `check_patch_conflicts.py`）

---

## 一、结论

| 包 | 账行号 | 现等级（**现核**，非派活包口径） | 处置 | 结果 |
|---|---|---|---|---|
| K-07 | 卡组:134 | C | 做 | ✅ A —— 督军名那颗命中区补上（`BuildWarlordHit`） |
| K-07 | 卡组:138 | C | 做 | ✅ A —— 卡背 **2 层** + `OpenCardbacks` 0.25 s 入场 |
| K-07 | 卡组:148 | C（**派活包未列**，但是 134 的落点） | 做 | ✅ A —— `OpenWarlordDetail` |
| K-09 | 卡组:284 | **A**（派活包写 P2） | 只核验 | ✅ 核验通过（无改动） |
| K-09 | 卡组:285 | **A**（派活包写 P1） | 只核验 | ✅ 核验通过（无改动） |
| K-09 | 卡组:286 | **A**（派活包写 P1） | 只核验 | ✅ 核验通过（无改动；接线已在 `BattleDriver.cs:34254`） |
| K-09 | 卡组:290 | **A**（派活包写 P1） | 只核验 | ⚠️ 谓词核验通过，但**调用点仍未接** ⇒ 改动请求（见 §四） |
| K-10 | 卡组:305 | C | 做（前置） | ✅ A —— `RegisterPackUi` 缓存 startingPos/startingSize |
| K-10 | 卡组:299 | C | 做 | ✅ A —— 五件补齐（含 **起点 0.6 → 0** 订正） |
| K-10 | 卡组:300 | C | 做 | ✅ A —— 预览音效（cue 名已解出） |
| K-10 | 卡组:302 | C | 做 | ✅ A —— 影子副本 + 隐藏本体 |
| K-10 | 卡组:303 | C | 做 | ✅ A —— 七步里六步（**shiny 等 K-13**） |
| K-10 | 卡组:293 | C | 做 | ✅ A —— 0.4 s 淡出 |

**派活包的「现等级」再次普遍过期**（同批 `B9_W6DECK` 那条先例）：K-09 四条的账里**已经是 A**（施工单写 P1/P2）⇒ 本批只做核验。K-07/K-10 的 8 条确实是 C。

**typecheck：`运行时错误数: 0 / 编辑器错误数: 0`**（改完每一轮都跑，`TMPDIR=/tmp/wf_w8 bash 工具/typecheck.sh`）。
**没跑任何 Unity 宿主**（红线段 1）；**没动 git**；**没越白名单**。

---

## 二、逐条（账行号 | 现等级 | 原版怎么算 | 我们原来怎么算 | 改成什么 | 怎么验）

### K-07（`Shell/PracticeModePopup.cs`）

#### 卡组:134 · `DeckGeneralInfoDemo.Awake()` · C → A

- **原版怎么算**：`Awake` 给 **4 颗**钮挂 `onClick`。🥇 `decomp_full/DeckGeneralInfoDemo__Awake.c`：第 4 个 `AddListener` 指向 `WarlordInfoButtonClick`；🥈 `out_cpp2il_dev_ilspy/DeckGeneralInfoDemo.cs:59-61` 写明 `Button button = warlordTextButton; button.m_OnClick.AddListener(WarlordInfoButtonClick)`。
- **可点区域（本轮现读 prefab，不是照抄施工单）**：
  - `MonoBehaviour_-4747125247262437572` = 那颗 `Button`，`m_TargetGraphic` → **同一个 GameObject 上**的 `warlordName` TMP（`MonoBehaviour_3676532614230396732`）。
  - 该 TMP：`m_RaycastTarget = 1` · **`m_RaycastPadding = (0,0,0,0)`**（⇒ `MenuDraw.PaddedRect` 原样返回，**不外扩**）。
  - 它的 `RectTransform_-2158633528951091396`：`sizeDelta (0, 34)` · `pivot (0, 0.5)` · 挂 `ContentSizeFitter`（横向 PreferredSize）⇒ **宽 = 文字实测宽**（`menu_dump` 现读 `0×34`，那帧还没撑开）。
  - ⇒ 子树里**只有这一颗**吃射线，并集 = 它自己。
- **我们原来怎么算**：3 颗已接（`ShowDeckHit` / `ChangeDeckHit` / `ShowInfoHit`），第 4 颗 **只是 Label、没有命中区**。
- **改成什么**：新增 `BuildWarlordHit(CardDef wl)`（`PracticeModePopup.cs:3623`），在 `BuildDeckInfoHead` 末尾（`:832`）调用；矩形 = `WnL..WnL+实测宽` × `WnT..WnB`，经 `MenuDraw.PaddedRect(..., Vector4.zero)`。实测宽 = `Label.WorldW × LayoutSpace.PxPerWorldX`，夹在 `[8, 352.86]` px；量不到就退回固定框并 **出声一次**（⛔ 不静默变 0 宽 —— 那会「点不着」）。点击 → `OpenWarlordDetail()`（`:3653`）。
- **怎么验**：点督军名 ⇒ 开卡片详情窗（`CardDetailPopup`，日志 `[Practice] 点督军名 ⇒ 开卡片详情窗`）；点其它三颗行为不变；命中区宽度 ≈ 文字宽（不是 352.86）。

#### 卡组:138 · `DeckGeneralInfoDemo.ConfigPanel(...)` · C → A

- **原版怎么算**（🥇 `decomp_full/DeckGeneralInfoDemo__ConfigPanel.c` 整段）：写完四件身份 + 立绘之后，`currentCardback` 为空 ⇒ 整段 return；否则 `sprite = currentCardback.CosmeticSprite`，`foreach (Image img in cardBacks)`：`img.sprite != null && img.sprite == sprite` ⇒ **break**（同图什么都不做）；否则 `img.sprite = sprite; img.enabled = true; cardbacksAnimation.Rewind(); cardbacksAnimation.Play();`。
- 🔴 **「5 连轮播」是错的（就地订正，铁律 5）**：`MonoBehaviour_-3841077181908889796.cardBacks` 数组**实读长度 = 2**，两个 PPtr 分别落到 `Cardback`（`RectTransform_-3917913493052829892`：`localRotation.z = 0.0139622` ⇒ **+1.60°**）与它的子件 `Cardback Front`（`RectTransform_8791577597720547132`：`z = −0.0299279` ⇒ **−3.43°（相对）**）。`menu_dump … "Practice Mode Menu" --depth 8` 那棵树同样只有这两层。
- 🔴 **那条「轮播」= `OpenCardbacks` clip**（`AnimationClip_7721407185759151047`）：`m_SampleRate = 60`、末键 `t = 0.25`、**`m_WrapMode = 1`（= `Once`，不是循环）**。内容：
  - 两条 `m_RotationCurves` 都从**单位四元数**（0°）走到静止角 ⇒ 「扇开」；
  - `Cardback Front` 的 `m_FloatCurves`：`anchoredPosition (0.3, 0.3) → (11.2012, 4.9016)`；
  - 外层 `Cardback` 的两条 `anchoredPosition` 只有 `t = 0.25` 一个键 ⇒ **自己不动**（常量保持）。
- **我们原来怎么算**：只有**一张**卡背 quad，没有第二层、没有入场动画。
- **改成什么**：
  - 新增常量 `CbFrontL/T/R/B = 1553.55 / 324.73 / 1763.11 / 627.06`（外层矩形 + 内层偏移 `+11.2012 / −4.9016` px，与 dump 逐值吻合）、`CbRot = 1.60f`、`CbFrontLocalRot = −3.43f`、`CardbacksAnimDuration = 0.25f`。
  - `BuildCardbacks(DeckInfo)`（`:3685`）建两层（外层 `QPrFront`、内层 `QPrText` —— 两者与任何文字 quad **不重叠**，见 §三①）；
  - `SetCardbackTexture(tex)`（`:3704`）：同图 ⇒ 什么都不做（照抄 `break` 守卫），换图 ⇒ 两层一起换 + 重播；
  - `ApplyCardbacksPose(t)`（`:3716`）：两层 `localRotation` 写 `0→静止角`，内层 `localPosition` 从 `静止位 + (−10.7684, −4.9042) px` 回到静止位（那个向量 = `R(1.6°)·(−10.9012, −4.6016)`，已换算到世界方向）；
  - `PlayCardbacksAnim()`（`:3729`）：`Rewind + Play` 的等价物（先回第 0 帧），**只在真 Play 起补间**（批处理无帧循环 ⇒ 起了会永久停在起始帧）。
  - 三个调用点：建窗 `:884`、玩家卡组 `:2681`、预组 `:2779`。
- **怎么验**：打开卡组信息 ⇒ 两层卡背从「正着」扇到静止倾角（0.25 s），内层顺带滑 ~11 px；关窗再开 ⇒ 从第 1 帧重播；换卡组/换预组 ⇒ 重播；批处理里两层都在**静止态**（不是起始态）。

#### 卡组:148 · `DeckGeneralInfoDemo.WarlordInfoButtonClick()` · C → A（派活包未列，但 134 必须落到它上面）

- **原版怎么算**：🥇 `DeckGeneralInfoDemo__WarlordInfoButtonClick.c`：取 `currentDeck` 的 `classicDeck`/`skirmishDeck`（按 `currentSelectedPlayMode`）→ `PrebuiltDeck.get_DeckHero` → `CardDisplayWindow.Instance.ShowCard(hero, 0,0,0,0,0)`（= `tier: null`、`showOptions: false`）。
- **我们原来怎么算**：`—（无）`；`CardDisplayWindow` 在练习窗里 0 处引用。
- **改成什么**：`OpenWarlordDetail()`（`:3653`）= `CardDetailPopup.Create(Manager)` + `Manager.OpenWindow` + `ShowCard(wl)`，**与 `Shell/DeckInfoPopup.OpenWarlordDetail` 同一个写法**（`CardDetailPopup` = 原版菜单版 `CardDisplayWindow`，见它自己的文件头）。
- **怎么验**：同 134。

### K-09（`Shell/PrebuiltDecks.cs` + 生成器）—— **四条账里都已是 A，本批只核验**

#### 卡组:284 · `PrebuiltDeckCollection.AddDeck` · A（派活包写 P2）

- **原版怎么算**：① 资产名含 `"Tutorial"` ⇒ 不进任何表；② 否则 `decks.Add`；③ `Type == Bot(0)` ⇒ `botDecks.Add`；④ `isPracticeDeck` ⇒ `practiceDecks.Add`。
- **我们怎么算**：生成期按同一组字段分三表（`工具/gen_prebuilt_bot_decks.py` → `Resources/prebuilt_deck_lists.json`），运行期 `MetaOf(:597)` / `Decks(:608)` / `BotDecks(:621)`。
- **核验证据（现读数据文件）**：`md5 fe974249ed217705c551a86b0fbe53df` · `deckCount 224` · `decks 224` / `meta 224` · `botCount 196` · `practiceCount 103` · `type` 直方图 `{0:196, 1:28}` · `isPractice` 直方图 `{0:121, 1:103}` · `complete` 全 true（0 个 false）· `meta.srcName` 含 `Tutorial` 的 = **0**（⇒ 教程 12 副确实已被剔在生成期）。**落点行号与账一致**（597 / 608 / 621）。
- **结论**：✅ 核验通过，**不改**。⚠️ 如实记：`BotDecks` 目前**没有调用点**（等价 API + 条数验收口），不是缺陷。

#### 卡组:285 · `GetDeckByDifficulty` · A（派活包写 P1）

- **原版怎么算**：`Where(d.difficulty==arg && d.gameMode==arg)`（**只有两道**）→ `Random.Range(0, **未过滤** decks.Count)` → `ElementAtOrDefault(过滤后序列, 下标)`（越界 ⇒ **null**）。**原版自带缺陷，照抄不修**。
- **核验证据**：`FilterByDifficulty(:642-648)` 只有那两道（注释写死「不过滤 HasHiddenCards、不过滤 Type」）；`GetDeckByDifficulty(:665-678)` 用 `Decks.Count`（未过滤 224）取 `rng.Next(total)`，再 `index < filtered.Count ? filtered[index] : null`；缺陷在 `:654-657` 写死「照抄、⛔ 不修」。
- **结论**：✅ 核验通过，**不改**。⚠️ **本机无活调用点** —— 原版唯一调用者是排位 bot（难度取自远端 ranked 事件），离线没有那条链。

#### 卡组:286 · `GetRandomDeck` · A（派活包写 P1）

- **原版怎么算**：遍历 `decks`(224)：① `possibleArmies` 非空 ⇒ `Contains(deck.DeckHero.cardArmy)`；② 新匹配开启时命中 `selectableBotDecks`；③ `!HasHiddenCards()`；④ `!DeckHasBannedCards(currentEvent)`；然后 `Random.Range(0, list.Count)`。
- **核验证据**：`FilterDecks(:697-729)` 四道顺序照 `.c`（`:708-721`）+ 两道 🔶 自己设计（`playMode` / `faction`，`:722-725`，账里已登记理由与时间限定）；`GetRandomDeck(:743-761)` 空表返回 null 并出声（原版取 `[0]` 抛）。
- **接线已落地**：`CardPresentation/Battle/BattleDriver.cs:34254` = `PrebuiltDecks.GetRandomDeck(new System.Random(seed), playMode: gameMode, faction: faction)`；本地那份 `PickFrom` 已删（`:34274-34275` 注释）。
- **结论**：✅ 核验通过，**不改**。

#### 卡组:290 · `PrebuiltPack.IsValid` · A（派活包写 P1）

- **原版怎么算**：① `GetCardLibrary().Count == cardIds.Count`（查不到就跳过 ⇒ 少一张即 false）；② 逐张 `IsHidden()` 任一为真 ⇒ false；③ 全过 ⇒ true。活调用点 = `Everguild.LiveOps/DraftEventBase.cs:378`。
- **核验证据**：`PackIsValid(:791-799)` 两条件逐条落地；`hiddenCardIds` 那半是 🔶 自己设计（判据在远端 `FeatureConfig`，本机 0 命中），接口留着造负例。
- **⚠️ 调用点仍未接**：`Shell/DraftPacks.cs:332 RollRound` 的候选池只过 `PacksForArmy(enabled)` + 同 `packId` 去重 + `excludePackIds`，**没有** `PackIsValid` 那一条。现池 882/882 全 valid ⇒ 这条接不接**今天都恒 true**（必须自造负例才验得出）。`DraftPacks.cs` **不在我的白名单** ⇒ 见 §四改动请求。**账仍记 A（谓词本体已复刻）**，但在备注里写明「调用点未接」，别让下一个人以为整条链通了。

### K-10（`Shell/DraftModeWindow.cs`，+ `Shell/BoosterPackAnim.cs` 只改注释）

#### 卡组:305 · `PrebuiltPackUI.Setup()` · C → A（**299/302/303 的前置**）

- **原版怎么算**：① `startingSize = content.sizeDelta`；② `startingPos = content.anchoredPosition`（`cpp2il…/PrebuiltPackUI.cs:127-133`）。
- **我们原来怎么算**：没有缓存 ⇒ `OpenAnim` / `SelectAnimDummy` 的「复位到起始位」没有状态。
- **改成什么**：`PackUi`（`:3668`）新增 `startingPos` / `startingSize` / `restContentPos` 三个字段；`RegisterPackUi(content, selNode, index)`（`:3765`）在建包卡末句缓存它们（`MenuDraw.Node` 建完即落定 ⇒ 这里是安全取值点）。
- **怎么验**：建出来的 `PackUi.startingSize` = 那块 `PxRect` 的尺寸、`startingPos` ≈ `(0,0)`；随后 `PackPlayOpen` 复位读数与之一致。

#### 卡组:299 · `PrebuiltPackUI.OpenAnim(int packIndex)` · C → A

- **原版怎么算**（🥇 `decomp_full/PrebuiltPackUI__OpenAnim.c` 整段）：① `SetActive(true)`；② `DOKill(content, complete:true)`；③ `content.anchoredPosition = startingPos`；④ `selectButton.interactable = false`；⑤ `content.localScale = **Vector3.zero**` + `canvasGroup.alpha = 0`；⑥ `Sequence{ AppendInterval(packIndex × 0.175) → Play2DDelayed(appearSound, 同间隔) → Append(DOScale(content, 1, 0.4).SetEase(OutBack)) · Join(DOFade(alpha, 1, 0.4)) }`。调用侧 `OnComplete(EnablePackButton)`（`DraftModeSelectPacksStateDemo.cs:661-662`）⇒ **动画走完才可点**。
- 🔴 **起点订正**：`PackOpenScaleFrom` 原来是 `0.6f`、doc 也写「0.6→1」—— **错的**。`.c` 里 `set_localScale` 喂的是 `Vector3.zero`（三个分量各乘 `DAT_1834b3180`），🥈 `cpp2il…/PrebuiltPackUI.cs:262-266` 的 `num2 = default` 也是 `(0,0,0)`。0.6 是**关包**与**选包第二段**的终值。
- **我们原来怎么算**：只落「0.6→1 缩放 + 0.175 阶梯」；缺起始位复位、缺 alpha、缺**禁用选包钮**、缺 appearSound。
- **改成什么**：`PackPlayOpen(ui, index)`（`:3820`）五件齐；`PackEnableButton(ui)`（`:3804`）在 `OnComplete` 开回来；appear cue = **`Draft Pack Appear`**（`WindowSounds.DraftPackAppearCue`）；`PlayPackOpenAnim(:2555)` 在批处理里直接落终态（scale 1 / alpha 1 / 钮可点）。
  - ⚠️ **音效差异如实记**：原版 `Play2DDelayed(cue, delay)` 是**独立排程**；我们挂 `AppendInterval` 之后的 `AppendCallback` —— 时序相同，但 Sequence 被 Kill 时这一声不会补发（原版会）。
- **怎么验**：开包 ⇒ 从起始位、0 尺寸、0 alpha 长出来；期间选包钮**不可点**、动画结束可点；第 i 张延迟 `i × 0.175 s`；一声 `Draft Pack Appear`。

#### 卡组:300 · `PrebuiltPackUI.PreviewButtonOnClick()` · C → A

- **原版怎么算**：① `SoundManager.Play2D(previewPackSound)`；② `onPackPreview(currentPack)`。
- 🔴 **旧 doc 的说法「cue 名查不到 ⇒ 静音」已作废**（铁律 5 就地订正）：现读 prefab `MonoBehaviour_-2302561337839276558.previewPackSound`，`m_FileID = 6` → `externals[5]` = `soundcollection_assets_all` 的 `CAB-62d1945b…` ⇒ cue 资产名 **`Draft Prebuilt Pack preview`**（本地音效表**有**这条，clip = `UI Click Card 3`）。
- **改成什么**：`ShowPackPreview` 里（`DraftModeWindow.cs:2008`）在两条前置闸**之后**播 `WindowSounds.DraftPrebuiltPackPreviewCue`。
- **怎么验**：点包卡空白处 ⇒ 一声；缺资源时 `WindowSounds` 会出声说明（⛔ 不静默）。

#### 卡组:302 · `PrebuiltPackUI.SelectAnim(Vector3, Transform)` · C → A

- **原版怎么算**：① `DOKill(content)`；② `Instantiate(自己, anchor)` 造**影子副本**、`副本.position = 本体.position`；③ 本体 `SetActive(false)`；④ 副本的 `SelectAnimDummy(targetPosition)`。调用侧 `targetPosition = deckInfoPanel.transform.position`、`anchor = prebuiltPackAnimAnchor`（`DraftModeSelectPacksStateDemo.cs:540-556`）。
- **我们原来怎么算**：只落 `1→1.2→0.6` 缩放（而且还串行，见 303 那条的订正）；缺副本、缺隐藏本体、缺飞行。
- **改成什么**：`PackPlaySelect(pickedIndex)`（`:3874`）= 副本建在 `Select Pack Anim Anchor (It must be out of Select Pack State)` 下、位置对齐本体、隐藏本体、目标 = `_deckPanelRoot.position`。
  - ⚠️ **实现差异如实记**：副本与本体的 quad **共用材质**（`Instantiate` 只复制 renderer、不复制 material 实例），本体随后 `SetActive(false)` ⇒ 补间改的是「看不见的那一份」，画面正确；下一轮 `RefreshPacksPage` 把本体销毁。
- **怎么验**：选包 ⇒ 本体消失、副本飞向右栏；右栏格位与副本落点可读。

#### 卡组:303 · `PrebuiltPackUI.SelectAnimDummy(Vector3)` · C → A（**shiny 除外**）

- **原版怎么算** 七步：① 藏 `selectButton`；② `anchoredPosition = startingPos` · `scale = 1` · `alpha = 1`；③ `Join(DOScale(content, 1.2, 0.3, **OutBack**))`；④ `uiEffectToggler.Toggle(true)`；⑤ `AppendInterval(0.2)` → `Append(DOMove(target, 0.4))`，`OnStart` 播 `movementSound` 并把 shiny 关掉（0.4 s）；⑥ `Join(DOFade(alpha, 0, 0.4))` + `Join(DOScale(content, 0.6, 0.4))`，`OnComplete` 播 `arrivalSound`；⑦ `AppendCallback(Destroy)`。
- 🔴 **另一处就地订正**：`BuildPackSelectTween` 的旧 doc 写「1→1.2（0.3 s）**→** 0.6（0.4 s）**两段串行**」—— 原版第二段是跟 `DOMove` / `DOFade` **同时 Join** 的，不是串行。已改注释并只保留第一段。
- 🔴 **shiny（第 ④ 步）跳过 = 等 K-13**：依赖 Coffee `UIEffect Shiny` 的 shader，与收藏窗扫光（`收藏:348`）是同一条资源缺口（另一路代理在做）⇒ 本版只**出声一次**说明，⛔ 不自己编扫光。
- **改成什么**：`PackSelectFly(dup, targetWorld)`（`:3916`）落地六步；movement cue = **`Draft Pack Selected Moving`** · arrival cue = **`Draft Pack Finish Animation`**（两条现读 prefab PPtr 解出，本地音效表都有）。`PickPackThenAdvance` 的等待值随之订正为 **0.3 + 0.2 + 0.4 = 0.9 s**（原来写 0.7）。
- **怎么验**：飞向右栏 + 淡出 + 销毁；movement / arrival 各一声；shiny 那条日志出现一次。

#### 卡组:293 · `PrebuiltPackUI.CloseAnim()` · C → A

- **原版怎么算**：① `DOKill(content, complete:true)`；② `scale = 1`；③ `alpha = 1`；④ `DOFade(alpha, 0, 0.4)` ＋ ⑤ `DOScale(content, 0.6, 0.4)`（**同时**起）。
- **我们原来怎么算**：只做缩放 1→0.6；doc 还写着「alpha 常量没读到 ⇒ 只做缩放」。
- **改成什么**：`PackPlayClose(ui)`（`:3849`）= `Sequence{Append(fade 1→0 / 0.4) · Join(scale 1→0.6 / 0.4)}`。alpha 走 `PackFade`（世界空间 MeshRenderer 逐后端写：`ImageQuad.SetTint` / `Label.SetColor` / `CardView.SetAlpha`），不是 `CanvasGroup` —— 这只 `CanvasGroup` 乘不到自建网格，判据与写法同 `Shell/CollectionWindow.CellFade`。
- **怎么验**：关包（其余那些包）⇒ 缩放与淡出在 0.4 s 内同时完成（读 `PackFade.Alpha` 与 `localScale` 两个读数）。

---

## 三、没查清的部分（如实记，⛔ 不拿猜测填）

1. **卡背内层 `QPrText`(=3109) 与外层 `QPrFront`(=3108)**：逐矩形比过 `Deck info` 下所有同队列文字（`Lore Text` y 647–791 · `Deck Information Cost/balance text` x 1276–1540 · `Change Deck` y 680–764），**都与卡背矩形 (1542–1763, 325–627) 不重叠** ⇒ 撞队列不会串色。但**没有跑真机**确认；若将来某颗文字移到卡背上，这里要重新排队列。
2. **`OpenCardbacks` 的 Hermite 切线**：clip 里两端 `inSlope/outSlope = 0`、中间是 Unity 自动切线；我用 `Ease.InOutQuad` 近似。**帧间曲线形状没有逐帧对拍**（关键帧本身是原版值）。要更准得把 16 个旋转键 + 2 个位置键逐帧采出来 —— 判据在 `AnimationClip_7721407185759151047`，随时可补。
3. **选包影子副本的材质共用**：静态分析判「本体隐藏 ⇒ 画面正确」，但 `Instantiate` 是否复制 material 实例取决于 `ImageQuad` 用的是 `sharedMaterial` 还是 `material`；**没跑真 Play 验过**（本批禁跑 Unity）。若真机上出现「副本飞走后原来的位置闪一下」，改法 = 给副本逐 quad `new Material(...)`。
4. **`Label.WorldW` 在建窗那一刻的可用性**：有 `[8, 352.86]` px 的夹取 + 出声兜底，但**没在真机环境量过**督军名的实测宽（批处理里字体是 Dynamic，测量值可能与运行时差几 px）。
5. **K-09 的「236 SO / 剔教程 12」**这个上游口径来自生成器自己的断言，本批**只核了产物**（224/196/103 + `srcName` 无 `Tutorial`），没重跑 `gen_prebuilt_bot_decks.py`（跑了会覆盖 `Resources/prebuilt_deck_lists.json`，属批次外动作）。

---

## 四、改动请求（不在我的白名单，交给对应写手）

### R1 → `Shell/DraftPacks.cs` 的写手（账 `卡组:290` 的调用点）

- **要改的一处**：`DraftPacks.cs:332 RollRound(...)` 的候选循环（`:338-346`），在 `cand.Add(p)` 之前加一条：
  ```
  if (!PrebuiltDecks.PackIsValid(p.cardIds, ResolvedCardCount(p.cardIds), null)) continue;
  ```
  其中 `ResolvedCardCount` = 「把 `p.cardIds` 逐张在工程卡池里 `CardDatabase.FindById(CardPool(), id)` 查，数出查到的张数」——**判据只写这一处**（`PrebuiltDecks.PackIsValid` 里已经有「条数不等 / 命中 hidden」两条），⛔ 别在 `DraftModeWindow` 里再判第二次。
- **为什么不是我做**：`Shell/DraftPacks.cs` 不在 `B9_W8PREB` 的文件白名单里（红线段 4）。
- **怎么验**：现池 882/882 全 valid ⇒ 必须**造负例**：① 正常包 ⇒ true；② `cardIds` 少一条（或塞一个查不到的 id）⇒ false；③ `hiddenCardIds` 含包内某张 ⇒ false。

### R2 → `Battle/BattleDriver.cs`（W1）—— **无需改动，仅备案**

账 `卡组:285/286` 的接线请求**已经落地**（`:34254`），本批只核；若 W1 那边正重写 `PickBotDeck`，请保留这条调用与「一律走 `PrebuiltDecks.GetRandomDeck` 唯一过滤链」的口径。

---

## 五、改动清单

| 文件 | 改动 | 行数变化 |
|---|---|---|
| `CardPresentation/Shell/PracticeModePopup.cs` | `:832` 挂命中区调用（**行内追加**）· `:883-884` 换成 `BuildCardbacks(d)`（**2 行→2 行**）· `:2681` / `:2779` 换成 `SetCardbackTexture(...)`（**各 1 行→1 行**）· 类尾追加 `BuildWarlordHit` / `OpenWarlordDetail` / `BuildCardbacks` / `SetCardbackTexture` / `ApplyCardbacksPose` / `PlayCardbacksAnim` + 6 个常量/字段（`:3620-3743`） | +167 行，**账里 8 个落点（:772/:816/:835/:844/:859/:895/:921/:2445）逐一行号不变** |
| `CardPresentation/Shell/DraftModeWindow.cs` | `PackOpenScaleFrom` 0.6→0 + doc 订正（:272-278）· `PackSelectFlyInterval` 常量（:291）· `ShowPackPreview` 音效 + doc 订正（:1989-2008）· `BuildPackCard` 末句改 `RegisterPackUi` · `BuildPackOpenTween` / `BuildPackCloseTween` / `BuildPackSelectTween` / `PlayPackOpenAnim` / `PlayPickPackAnims` / `PickPackThenAdvance` 重写 · 类尾追加 `PackUi` / `PackFade` / `MakePackFade` / `RegisterPackUi` / `PackUiOf` / `KillPackTween` / `PackEnableButton` / `PackPlayOpen` / `PackPlayClose` / `PackPlaySelect` / `PackSelectFly` | 3635 → 3964 行（`git diff --numstat`：**+368 / −40**） |
| `资料/完全复刻/账/_parts/B9_W8PREB.tsv` | 新建（13 行账补丁） | 新文件 |
| `资料/完全复刻/报告/B9_W8PREB_预组与轮抽C.md` | 新建（本文件） | 新文件 |
| `Shell/BoosterPackAnim.cs` | **未改** —— 它文件头把 `OpenCardbacks.anim` 记成「用途未查清，不接」；本批已查清（= `DeckGeneralInfoDemo` 的卡背两层入场）。为避免和别人的写手撞车，只在本报告里记，**建议由 `BoosterPackAnim.cs` 的下一个写手把那一行改成指针**：`OpenCardbacks.anim`（0.25 s · Once）= `DeckGeneralInfoDemo.cardbacksAnimation`（卡背两层扇开 + 内层滑 11 px），消费者 = `Shell/PracticeModePopup.BuildCardbacks`。 | 0 |

### ⚠️ 账漂移（`DraftModeWindow.cs`）—— 请调度台在同步点跑一次 `--fix`

`卡组编辑` / `轮抽` / `日常_奖励` / `未分类_其他` / `战斗逻辑驱动` 五个域都按 `文件:行号` 引用 `DraftModeWindow.cs`。本次改动让**行号整体下移**（漂移，**不是失败**）：

| 原行号（账里写的） | 符号 | 现在实际行号 | 位移 |
|---|---|---|---|
| 1981 | `ShowPackPreview` | 1991 | +10 |
| 2411 | `BuildPackCard` | 2423 | +12 |
| 2554 | `PlayPickPackAnims` | 2572 | +18 |
| 2662 | `PackTitle` | 2681 | +19 |
| 2708 | `RefreshDeckPanel` | 2727 | +19 |
| 3084 | `OpenWarlordCard` | 3103 | +19 |
| 3101 | `OpenCardRowDetail` | 3120 | +19 |
| 3139 | `ChooseWarlord` | 3158 | +19 |
| 3208 | `PickPack` | 3227 | +19 |
| 3254 | `PickPackThenAdvance` | 3275 | +21 |
| 3595 | `AnimateWarlordCardsIn` | 3616 | +21 |
| 3618 | `PlayWarlordCardsOut` | 3639 | +21 |

- 建议：`python -I -X utf8 工具/check_ledger_points.py --all-A --fix`（**只在没有 `.cs` 写手 + 补丁已合并完时**跑）。
- ⚠️ `轮抽.tsv` 里另有一大批引用（`GetBaseSave()` / `Validate(TS save)` / `LoadEventData(...)` …）**在改动前就已经指错**（那些是 LiveOps 类的方法，不是本文件的）—— 那是**存量**漂移，不是本批造成的；`--fix` 会给它们打 `NEED-HUMAN`。请调度台按符号 + 语义人工确认，⛔ 别机械往上找最近的声明。

---

## 六、顺手发现

1. **施工单/派活包说「卡背 5 连轮播」→ prefab 实读是 2 层**（已就地订正，见 §二 卡组:138）。建议顺手 grep 一遍还有没有别处抄了「5 连」这个说法。
2. **`卡组编辑.tsv` 的 `卡组:134` 备注把 `ShowInfoHit` 记在 `:2445`** —— 实际在同一份账写作时它就在 `:2478`（本批前后都**没有**改动过那片行号，是存量偏差）。本批已把 134 的落点改成 `BuildWarlordHit`（`:3623`），并在备注里写对了 `ShowInfoHit` 的当前行号。
3. **`BuildPackSelectTween` 的 doc 原来把 `SelectAnim` 写成「两段缩放串行」** —— 原版是「放大 → 停 0.2 s → 飞（同时缩到 0.6 + 淡出）」。已订正（见 §二 卡组:303）。
4. **`OpenCardbacks.anim` 的用途查清了**：它就是 `DeckGeneralInfoDemo.cardbacksAnimation` 那条 clip（`Shell/BoosterPackAnim.cs` 文件头记的是「用途未查清，不接」）。建议该文件的下一个写手把那一行改成指针（我没动那个文件，避免撞车）。
5. **`WindowSounds` 里 K-10 要用的四条 cue 早就在表里**（`DraftPackAppearCue` / `DraftPackFinishAnimCue` / `DraftPackSelectedMovingCue` / `DraftPrebuiltPackPreviewCue`，`Shell/WindowSounds.cs:245-248`）—— 之前几轮把「cue 名查不到」记成了缺口，实际只差**接**。
6. **`PrebuiltDecks.BotDecks` 目前零调用点**：它是等价 API + 条数验收口（196），不是缺陷；但若将来有人「清理死代码」时看到它，别删 —— 账 `卡组:284` 的验收口径就是它。
