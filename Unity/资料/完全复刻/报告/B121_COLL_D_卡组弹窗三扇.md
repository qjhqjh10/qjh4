# B121-COLL-D · 卡组相关三扇弹窗（DeckInfoPopup / DeckSelectionPopup(+TabController+SelectDecksTab) / ImportDeckPopup）对账

- 会话：2026-11-03 · 只读审计（未跑 Unity、未动 git、未改任何源码）
- 判据一手源：`d:/2/tools/decomp_full/*.c`（权威）· `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/*.cs`（主力）· `d:/2/新解包资源/assets_full/bundle_menus_assets_all/`（prefab 字段）· `d:/2/tools/assetripper_out/ExportedProject/Assets/*.prefab`（根字段）
- 我们的实现：`Shell/DeckInfoPopup.cs`(3035) · `Shell/DeckSelectionPopup.cs`(1114) · `Shell/ImportDeckPopup.cs`(582) · `Shell/CollectionWindow.cs` · `Shell/CollectionData.cs` · `RuleEngine/Data/DeckLibrary.cs`

## 一、结论与覆盖数

**结论一句话**：三扇窗的**几何/文案/显隐/交互几乎全部落地**（大量条目连「原版字段值 + 我们行号」都写在注释里，多轮真机修正过）；本轮查出的缺口集中在**四条「链路层」而不是「画得对不对」**——① `Import` 成功后原版先进 `DeckInfoPopup(Import)` 再进编辑器、我们直接落库；② `Confirm` 钮原版出厂不可点、我们恒可点；③ **外壳文本框没有任何「粘贴」通道**（`PointerLayer` 只吃 `Keyboard.onTextInput`），而这两扇窗的整个用途就是「粘卡组串」；④ `Share` 写进剪贴板的串缺原版那段可读头部（只有 base64 尾行）。

| 扇 | 交互件（按钮/可点区） | 对账行数 |
|---|---|---|
| `DeckInfoPopup` | 8 颗（3 大钮 + 5 圆钮）+ 关窗钮 + 点窗外关 + 督军立绘 | 23 行 |
| `DeckSelectionPopup`（含 TabController / SelectDecksTab） | 页签 ×2 + Random + 关窗 + 点窗外关 + 卡格点选 | 18 行 |
| `ImportDeckPopup` | Confirm + 关窗 + 点窗外关 | 10 行 |

**四档计数（51 行）**：`✅符合原版 40` · `⚠️实现了但参数/行为不符 7` · `🟠我们没实现 2` · `❓判据查不到 2`
（§三 P1「粘贴通道缺失」是跨件问题，未单列进主表档位，但实际影响面 ≥ 一个 🟠）

---

## 二、主表

### A. `DeckInfoPopup`（原版 22 个方法，反编译逐方法读过）

| # | 是什么 | 实现 | 符合 | 原版参数（出处） | 我们（文件:行） | 差异 | 档 |
|---|---|---|---|---|---|---|---|
| A1 | 窗口参数 | ✅ | ✅ | `type=1 · windowsPlacement=15 · closeOnESC=1 · extraScaleSmallScreen=1`（`Deck info Popup.prefab` 根脚本块） | `DeckInfoPopup.cs:1214-1231` | 无 | ✅ |
| A2 | 压暗层 + 点窗外关 | ✅ | ✅ | `Menu Dark Background` 4574.60×2572.36 · `(0,0,0,.773)` · `BackgroundCloseButton`（dump；`__Open.c:135-146` 挂监听） | `:1295-1306` | 无 | ✅ |
| A3 | 红底 | ✅ | ✅ | `UI_Deck_Information_Back` Sliced，border 42/363/655/81，134.5,82→1839.5,1032 | `:1312-1313` | 无 | ✅ |
| A4 | 督军立绘 + 点击开卡详情 | ✅ | ✅ | `Warlord Image` 1108²，挂 `EverguildButton`；`OnWarlordClick`→`CardDisplayWindow.ShowCard(hero, showOptions:false)`（`DeckInfoPopup__OnWarlordClick.c`） | `:1368`、`:2449-2459` | 无 | ✅ |
| A5 | 卡组名/督军名/阵营图标/模式图标+分割线 | ✅ | ✅ | `Deck Name` fs44.5 base42 `Left/Bottom` · `Warlord Name` fs40 base38 `Left/Middle` · `Army Icon` 100×110 · `Game Mode Separator` ↻−10.60° `(.42,.157,.137,1)` · 图标「无 sprite 即隐藏」（`__Open.c:80-140`） | `:1408-1490` | 无（⚠️ 文件头 `:17` 仍写「居中」，`:286` 已订正为 Left/—见§五） | ✅ |
| A6 | `Info Panel` + 两抽屉（Deck List / Deck Info） | ✅ | ✅ | `UI_Deck_Information_submenu_Back` 659,218→1799,868 · 出厂只有 `Deck List` 开（dump `Deck Info` INACT） | `:1494-1497` | 无 | ✅ |
| A7 | 卡组内容：排序 + 防御卡钉首 + 叠块 + 份数 | ✅ | ✅ | `DeckListDrawer.Initialize/Reorder`：按 `manaCost`→`GetLocalizedCardName`；`spellType==210 && cardType==20` 者 `SetSiblingIndex(0)`；`stackCopies=1`；`SetCount` 里 `1<count` 才印 `xN`（`DeckListDrawer.cs:98-110/376-468`、`UICardInfoItem__SetCount.c:34`） | `:1836-2031` | 无 | ✅ |
| A8a | 行内三张图 + 费用圈 64 + 稀有度 tint | ✅ | ✅ | `40k_deck_cardlist_bg` 九宫150/0/150/0 · `…_rarityColorGradient` x+225.54 · `…_border` 九宫5 · `Card Frame Cost Icon` ARF 1:1→64×64 · `imagesToChangeColorByRarity=[Rarity Gradient, Background Border]`（dump + MB `-2272266055686004801`） | `:1925-1938` | 无 | ✅ |
| A8b | `Deck Info` 抽屉的内容（标题 + 9 行费用曲线 + 卡背） | 部分 | ⚠️ | `DeckInfoDrawer.Initialize`：`cardbackFront` 与 `cardbackBackground` **两个 Image 都喂**；`Cardback`(↻+1.60°, ×1.85) 下**子件 `Cardback Front`**(↻−3.43°, 209.56×302.33, 中心偏 +11.2/−4.9)（`DeckInfoDrawer.cs:14-36` + dump） | `:2109-2156`（只画父层） | **缺 `Cardback Front` 这一层**（我们自己注释承认「老账」，未立账） | 🟠 |
| A9 | `Lore Text` | 不建 | ✅ | 出厂 `act=N`，`DeckInfoDrawer` 也不喂它（dump + 该类 4 个方法全读） | `:2150-2152`（出声不建） | 无（我们无 lore 字段） | ✅ |
| A10 | `DeckInfoDrawer` 上的 deckName/warlordName | 不建 | ✅ | 该实例两字段 **PPtr 全 0**（`MonoBehaviour_2504033521142106536.json`） | — | 无 | ✅ |
| A11 | `Edit Deck` 显隐 | ✅ | ✅ | `SetActive(editButton, state < 2)`（`DeckInfoControls__Initialize.c:77`） | `:700`（`EditDeckShown`）、`:708` | 无 | ✅ |
| A12 | `Edit Deck` 的 `SoftDisable` | ✅ | ✅ | `SoftDisable(editButton, !CanImportDeck(popup, deck))`，无 state 守卫（`:79-83`） | `:982-983` | 无（`CanImportDeck` 改判恒 true 有实拍依据） | ✅ |
| A13 | `Edit Deck` 在 `state==Import` 时**换文案** | 未实现 | ⚠️ | 文案键由 `state==1` 决定：`MenuDeck/MenuButtons/{EditDeck,ImportDeck}`（`DeckInfoControls.cs:84-95`；prefab `Edit Deck/Button Text` 挂 `Localize`） | `:809-819`「两条词条键还没解出来」 | 注释说「未解出」**已过期**：键就是 `MenuDeck/MenuButtons/ImportDeck`，官方 13 语言表本地就有 ⇒ **可补** | ⚠️ |
| A14 | `Select Deck` 显隐 | ✅ | ✅ | `context.SelectButton != null`（`:~100`；第 7 个构造点 `DeckSelectionPopup.UpdateInfo` 传非空） | `:709`、`:1255-1272` | 无 | ✅ |
| A15 | `Share`（写剪贴板） | ✅ | ⚠️ | `MakeDeckString()` = `* 名\n*\n* Card_Race/Warlord: 督军\n*\n` + 每张 `* Nx 卡名\n` + `*\n` + `Serialize(deck)`（base64），再 `GUIUtility.systemCopyBuffer` + `ShowMessage(MenuDeck/Share/ExportSuccesful)`（`__ShareDeck.c`、`__MakeDeckString.c`） | `DeckRuntime.cs:6044-6050`（只写 `ExportString` = **纯 base64 一行**）+ `DeckInfoPopup.cs:2508-2550` | **剪贴板内容缺可读头部**（玩家看到的是乱码一样的一长串；原版是「卡组名 + 督军 + 每张卡」的可读清单，最后一行才是载荷）。载荷格式本身**逐字相同** | ⚠️ |
| A16 | `Share On Chat` | ✅ | ✅ | 开 `GenericOptionsPanel` 两键（`MenuDeck/Share/ShareOn{Alliance,Global}`），三道门：`IsBannedFromChat(Global/Alliance)`、`AlliancesManager.IsInGroup`；末跳 `ChatGlobalManager.ShareDeck(串, 频道)`（`__ShareDeckOnChat.c`） | `:2566-2756` | 末跳无宿主（无聊天服务器）⇒ 如实出声，不写剪贴板 | ✅ |
| A17 | `Delete`（二次确认） | ✅ | ✅ | `ShowPopUp("MenuDeck/HUD/DeleteDeckCheck", localize, closeOnEsc:false, [删除:Demo/FriendsMenu/DeleteFriendButton, 取消:MainMenu/General/Cancel])`；确认后 `Close()`+`Remove`+`DeleteDeck`，失败 `MenuDeck/Error/FailedToDelete` 且道具退回（`__DeleteDeck.c`、`__DeleteDeck_b__27_3.c`） | `:2360-2373`、`:2412-2446` | 无（失败回滚已有 `RestoreDeck`） | ✅ |
| A18 | `Duplicate` | ✅ | ✅ | `new CardDeck(deck)` + 名 `+ "+"` + `syncedToServer=false` + 上传回调里 `InventoryManager.Add` + `Close`；`duplicateButton.interactable = 卡组数 < totalCustomDecks(114)`（`__DuplicateDeck.c`、`DeckInfoControls.cs:239-243`） | `:2376-2393`、`DeckLibrary.cs:244-254`、`:973` | 无 | ✅ |
| A19 | `Practice Deck` | ✅ | ✅ | 显隐 `isPlayerDeck && (state==0 或 2)`；`interactable = DeckUtility.ValidateDeck(deck)`；点击 → 开选卡组窗挑**对手** → `StartPracticeMatch`（`OwnDeckTraining=0xc`，这一副=playerDeck）（`DeckInfoControls.cs:105-112/201-207`、`__SelectPracticeOpponentDeck.c`、`__StartPracticeMatch.c`） | `:714`、`:953-961`、`:2283-2400` | 无（那扇窗用哪个 prefab 变体见 §四 Q1） | ✅ |
| A20 | `Switch Deck Info` | ✅ | ✅ | `toggleDrawerButton` 常显、`SetIsOnWithoutNotify(true)`；`ToggleDrawers(showList)` 两面互斥（`:105/210-211`、`__ToggleDrawers.c`） | `:715`、`:967`、`:2471-2479` | 无 | ✅ |
| A21 | `Select Deck` 动作 | ✅ | ✅ | `popup.Close()` → `context.SelectButton.OnPress(自己GO)`（`DeckInfoControls__OnSelect.c`） | `:2218-2253` | 无 | ✅ |
| A22 | `CanImportDeck` + 失败提示 | ✅ | ✅ | `deck.CustomGameModeEvent==null ⇒ false`；否则按「拥有度」判；失败文案 `MenuDeck/CantImportDeck`，`{0}`=督军阵营名（`__CanImportDeck.c`、`__EditDeck.c:21-45`） | `:859-868`、`:879-883`、`:2198-2209` | 判据改判恒 true（离线无 LiveOps）有实拍依据；`{0}` 语义已查实 | ✅ |

### B. `DeckSelectionPopup` + `DeckSelectionTabController` + `SelectDecksTab`

| # | 是什么 | 实现 | 符合 | 原版参数（出处） | 我们（文件:行） | 差异 | 档 |
|---|---|---|---|---|---|---|---|
| B1 | 窗口参数/压暗/红底/关窗 | ✅ | ✅ | `type=1 · placement=15 · closeOnESC=1 · extraScaleSmallScreen=1`；压暗 `(0,0,0,.773)` 4574.6×2572.36；红底同 A3；关窗钮 1783.11,62.90→1857.49,138.50 | `DeckSelectionPopup.cs:135-216`、`:372-390` | 无 | ✅ |
| B2 | 两页签 + 选中态 | ✅ | ✅ | `Tab buttons` HLG spacing 12.45 · 260×67.6421；两颗共用 `onSprite=40K_tab_button / offSprite=40K_tab_button_overwindow`，`onColor=(1,0.6308,0,1)`、`offColor=(1,0.5443,0,1)`（dump + `EverguildToggle` MB） | `:725-766` | 无 | ✅ |
| B3 | 页签文案 | ✅ | ✅ | 两颗 TMP `m_text` 为空串，只有 `mTerm` = `MenuDeck/Button/{PresetDeck,OwnDeck}` | `:730-766`（走 `Loc.T`） | 无 | ✅ |
| B4 | `Random` 钮 | ✅ | ✅ | `Generic Simplified UI Button` 212.24×63.92 @184.5,135.5；`RandomizeDeck`：从**当前页那一批**随机 → 关窗 + 回调（`DeckSelectionTabController__RandomizeDeck.c`） | `:200-206`、`:858-863` | 无 | ✅ |
| B5 | 标题（`Instructions 2`） | ✅ | ✅ | `Instructions 2` act=Y，`mTerm=MenuDeck/Tip/SelectDeckAgainst`，fs36 base36 Center/Middle | `:263`（`TitleTerm`） | 无 | ✅ |
| B6 | 不建的件（`Instructions` / `Filter Buttons` / 搜索框） | 不建 | ✅ | 三件出厂 `act=N`；`DeckSelectionPopup` 6 个字段里没有它们，`DeckSelectionTabController` 6 个方法零 `SetActive` | `:60-115`（四条证据写在文件头） | 无（搜索/过滤代码路径存在但永远拿不到非空串） | ✅ |
| B7 | 卡格参数 + 选中态 + 列数 | ✅ | ✅ | `RecyclableScrollRect` 1565×778.06 · `_cellWidth/_cellHeight=225/364.5` · `_spacingX=20` ⇒ 列数 `floor(1565/245)=6`；原型格 `Collection Deck With Highlight`（`useSelectedHighlight=1`） | `:219-235`、`:898-980` | 无 | ✅ |
| B8 | 空态 | ✅ | ⚠️ | `Deck Scroll View/Empty Collection Warning`（出厂 INACT），子 TMP `Warning` **无 `Localize` 组件**，`m_text` 硬编码 `'There are no deck in your collection for the selected filters'`；`CollectionDisplay` 在 `filteredCollection.Count==0` 时 `SetActive(true)`（`CollectionDisplay.cs:721`） | `:619-641`、`:982-1006`（`Loc.T("MenuCollection/NoDecksFound")`） | **文案不符**：原版是那句硬编码英文；我们印官方词条「There are no decks … or game mode」（多 `s`、多 `or game mode`，且会随语言变） | ⚠️ |
| B9 | 点一格 = 开信息窗（不是直接选中） | ✅ | ✅ | `UpdateInfo` → `DeckInfoContext{Deck, State=deckInfoMode, SelectButton=非空, CheckOwnership=(state!=3)}` → `OpenWindow(DeckInfoPopup)`；`deckInfoMode` 玩家点得到那一刻 = 3（`__UpdateInfo.c`、TabController 四个入口都传 3） | `:804-847`、`:1255-1279` | 无 | ✅ |
| B10 | 预组页过滤（禁卡 + 事件） | 部分 | ❓ | `ShowPrebuiltDecks`：`d.CustomGameModeEvent == context.Deck.CustomGameModeEvent` 且 `!DeckUtility.HasBannedCards(d)`（`__ShowPrebuiltDecks.c`） | `Shown()` `:444-448`（不过滤） | 我们的等价物没有禁卡表（数据源缺 ⇒ 恒 false）；`CustomGameModeEvent` 离线恒 null ⇒ 本轮**行为等价**，⚠️ 数据补齐后要接 | ❓ |
| B11 | 预组页内容 | 部分 | ⚠️ | 原版列 `practiceDecks` 里 `!HasHiddenCards` + 名非空者，按 `difficulty`→`deckArmy` 排序（`TabController__Awake.c`） | `PrebuiltDecks.Tab`（`:337-355`，同排序）；「只列经典」在 `DeckSelectionPopup.cs:90-92` | **只列经典 30 张那 29 副**，遭遇 12 张那 54 副不列（2026-09-26 用户拍板，因对战不支持 12 张）⇒ 已知偏离 | ⚠️ |
| B12 | 难度角标 / 模式图标 | ✅ | ✅ | `displayDifficultyLabel` 预组页 true、我的卡组页 false（`ShowPrebuiltDecks/ShowOwnDecks`） | `MenuDraw.DeckCell(showDifficulty: !OwnDecks)` | 无 | ✅ |
| B13 | 我的卡组页按模式筛 | ✅ | ✅ | `__TryOpen_b__10_0`：候选 `GameMode == context.Deck.GameMode`，否则 `== context.GameMode` | `:415-450`（`ModePasses`） | 无 | ✅ |
| B14 | `Select`（关窗 + 回调） | ✅ | ✅ | `Select(deck)`：`base.Close()` + `context.Callback(deck)` | `:866-880` | 无 | ✅ |
| B15 | `SelectDecksTab`：模式页 ↔ 列表页 + 返回链 | ✅ | ✅ | `ChooseTabToOpen`：`IOwnDeckPlayEvent` 数 ≤1 ⇒ 直接进列表，≥2 ⇒ 先模式页；`BackButtonClicked`：`currentTab != 模式页` ⇒ 清空 + 重挑；否则落 `CollectionScreen` vtable `+0x1e8`（`__ChooseTabToOpen.c`、`__BackButtonClicked.c`、`CollectionScreen__BackButtonClick.c`） | `CollectionWindow.cs:4600-4672`（`ShouldReturnToDeckModePage`/`OnBackClicked`） | 无（「`currentTab==null` 那一档表达不了」已如实记） | ✅ |
| B16 | `unlockButton` / `createDeck` / `importDeck` 显隐与接线 | ✅ | ✅ | `Setup()` 先 `unlock=off`；`ToggleSelectDeck` 开 unlock+create+import+filterGameObjects；`ToggleSelectGameModeDeck` 关 unlock/create、只留 import（`__Setup.c`、`__ToggleSelect*.c`） | `CollectionWindow.cs:4084-4141`、`:4265-4300` | 无 | ✅ |
| B17 | `importDeck.interactable` | 未实现 | 🟠 | `= InventoryManager.GetOwnedCount<CardDeck>() < GameStaticData.totalCustomDecks(114)`（`__Setup.c:44-53` 亲读） | `CollectionWindow.cs:4084/4139`（无 interactable） | 卡组满 114 时原版导入钮变灰，我们恒可点 | 🟠 |
| B18 | `CreateDeck` / `ImportDeck` 两个入口 | ✅ | ✅ | `CreateDeck`：`CardDeck.GetEmptyDeck()` → 打 `gameMode` → `syncedToServer=false` → `OpenWindow<DeckEditingWindow>`；`ImportDeck`：`OpenWindow<ImportDeckPopup>()`（`__CreateDeck.c`、`__ImportDeck.c`） | `CollectionData.cs:302-330`、`CollectionWindow.cs:5426-5432` | 无 | ✅ |

### C. `ImportDeckPopup`

| # | 是什么 | 实现 | 符合 | 原版参数（出处） | 我们（文件:行） | 差异 | 档 |
|---|---|---|---|---|---|---|---|
| C1 | 窗口/压暗/窗框/关窗 | ✅ | ✅ | `type=1 · placement=15 · closeOnESC=1 · extraScaleSmallScreen=1`；压暗 `(0,0,0,.396)` 3161.26×1777.49 · 点它关；窗 560,234.07→1360,685.93；`40k_popup` 九宫169/160；关窗钮 1317.3,202.1→1392.3,277.1 | `ImportDeckPopup.cs:49-87`、`:148-300` | 无 | ✅ |
| C2 | 标题 | ✅ | ✅ | `Main Search message` `m_text='Paste your deck'`、`Localize.mTerm = MenuDeck/Share/PasteDeck`、fs50 base36 Center/Middle | `:109`、`:214`（`Loc.T(TitleTerm)`） | 无 | ✅ |
| C3 | 输入框 | ✅ | ✅ | `TMP_InputField`：`m_LineType=1(MultiLineSubmit)` · `m_ContentType=0` · `m_CharacterLimit=0`（无限）；`Text`/`Placeholder` 均 `Left/Top` fs32；底色 `(.29,.953,.682,1)`；占位 `Enter text...`（`mTerm=MenuDeck/HUD/EnterText`）；`OnEnable` 里 `text=string.Empty`、错误行隐藏 | `:100`、`:148`、`:344-345` | 见 §三 P1（**粘不进来**） | ✅（几何） |
| C4 | `Confirm` 钮 | 部分 | ⚠️ | `OnEnable`：`importButton.interactable = false`；`onValueChanged → ValidateField`：`interactable = !string.IsNullOrEmpty(value)` 并隐藏错误行（`__OnEnable.c`、`__ValidateField.c`） | 建钮在 `:241-261`（`HitOn(... TryImport)`） | **没有「出厂不可点 / 输入非空才亮」这一档** —— 空串点下去才报错 | ⚠️ |
| C5 | 解析规则 | ✅ | ✅ | `text.Split("\n")` → `Last()` → `CardDeck.DeserializeDeckString`（base64 → `名:卡id:…;mode`，`:`→`%3A` 转义、按类型摘防御卡/督军）（`__TryImport.c:14-18`、`CardDeck__DeserializeDeckString.c`） | `:367-397`（跳过尾部空行，更宽容）+ `DeckLibrary.cs:457-510` | 行为一致（我们多一条「空串即报错」，见 C9） | ✅ |
| C6 | 错误行文案 | ✅ | ⚠️ | 解析失败只用 **`badStringError` = `MenuDeck/Share/InvalidDeckString`**（EN `Invalid deck string`）；`failedSaveError` / `invalidDeckError` 在本类里没有消费点（`__TryImport.c` 读 `+0x98`） | `CollectionData.cs:278-296`：三条自定键 `MenuDeck/Error/Import{Empty,BadString,NotPersisted}` | **文案不符**（原版一条、官方键；我们三条自拟键）。三态区分是「更细」，但原版没有 | ⚠️ |
| C7 | 成功后的落点 | 部分 | ⚠️ | 解析成功 ⇒ 构造 `DeckInfoContext{Deck, State=Import, SelectButton=null, CheckOwnership=true}` → `OpenWindow<DeckInfoPopup>` → `Close()` 自己。**不落库**；玩家要在信息窗点「Import Deck（= Edit Deck 那颗）」→ `DeckEditingWindow(context.Deck)` 才保存（`__TryImport.c:20-40`、`__EditDeck.c`） | `:393-396`（`CollectionData.ImportDeck` 直接落库 + 关窗） | **少一站**：原版先给玩家看这副牌（信息窗 Import 态）、再从编辑器保存；我们直接写库关窗（玩家看不到导进来的是什么，也没得改） | ⚠️ |
| C8 | 取消 / 点背景关 | ✅ | ✅ | `closeButton` 与 `backgroundCloseButton` 都挂 `Close`（`__OnEnable.c:8-24`） | `:300-320`、`:460-480` | 无 | ✅ |
| C9 | 空串/空白提交 | ✅ | ❓ | `Convert.FromBase64String("")`/`" "` **合法**（返回空字节）⇒ 原版会造出一个「无名字无卡组无督军」的 `CardDeck` 并开信息窗（`__DeserializeDeckString.c` 无空串守卫） | `CollectionData.cs:283`（空/空白 ⇒ `ImportEmpty`） | 我们更严（不回退成原版那个畸形卡组）；按「原版正确」口径这是**我们有意的偏离**，需要拍板 | ❓ |
| C10 | `OnError(string)` | 不适用 | ✅ | 本类有 `OnError` 但**全类无调用点**（`grep` 只有定义） | — | 无 | ✅ |

---

## 三、问题清单（用户可见优先）

**P1 · `Import Deck Popup` 实际上「粘贴不进来」（🟠 · 用户可见 · 与外壳文本框共用件重叠）**
- 原版：`TMP_InputField m_LineType=1 (MultiLineSubmit)`，靠平台粘贴（Ctrl+V）喂一长串 base64。
- 我们：`Shell/PointerLayer.cs:196-232` 的文本焦点只吃新输入系统的 `Keyboard.current.onTextInput`（`OnText(char)` `:231` 只收非控制字符）+ `TypeChar` `:169-174` 同样 `char.IsControl` 直接丢。
- 结论：**全仓 `Assets/CardPresentation` 里没有任何 `systemCopyBuffer` 读点**（`grep systemCopyBuffer` 只有 `DeckRuntime.cs:6048` 的写、以及 `Editor/` 的自检），也**没有 `KeyCode.V`/Ctrl 组合** ⇒ 玩家只能逐字敲 1–2 KB 的 base64，且**回车/换行永远输不进去**（换行是控制字符）。
- 连带：`Share` 复制的串**无法回到本窗**（原版是「复制→发给朋友→朋友粘贴」的闭环）。
- 判据：`ImportDeckPopup.cs:344-357`（`BeginText(_text, 4096, …)`）+ `PointerLayer.cs:169-174/196-232`；原版 `Import Deck Popup.prefab` 的 `m_LineType: 1`。

**P2 · 导入成功后少了「先看牌再保存」那一站（⚠️）**
- 原版：解析成功 → `DeckInfoPopup(State=Import)`（±「Import Deck」直加钮）→ 编辑器保存；我们：`CollectionData.ImportDeck` 直接落库 + 关窗（`ImportDeckPopup.cs:393-396`）。
- 另：聊天 `DeckShare` 那条路（它的等价物 = `ImportDeckPopup.TryImportDeck`）原版在卡组合法时会给一颗**直加钮**（`MenuDeck/MenuButtons/ImportDeck`，动作 = `InventoryManager.Add` + 回调）；我们 `ChatPanel.cs:955-958` 传的是 `selectButton=null` ⇒ 这颗钮没有。

**P3 · `Confirm` 钮没有禁用态（⚠️）** — 原版 `interactable=false` 出厂、输入非空才亮（`__OnEnable.c`/`__ValidateField.c`）；我们这个位上恒可点，空串时才报「先粘贴卡组串」。`ImportDeckPopup.cs:241-261`。

**P4 · 分享串缺可读头部（⚠️）** — 原版 `MakeDeckString` 四段（名 / 督军 / 每张卡 Nx）＋末行 base64；我们只写 base64（`DeckRuntime.cs:6044-6050`）。载荷格式一致，导入互通不受影响。

**P5 · 卡组 `Deck Info` 抽屉缺 `Cardback Front` 一层（🟠）** — 原版两层（父 ↻+1.60°、子 ↻−3.43°，子 209.56×302.33、中心偏 +11.2/−4.9，两个 Image 同贴图）；我们只画父层（`DeckInfoPopup.cs:2130-2148`）。参数已在注释里，可直接补。

**P6 · `Select Decks Tab` 的 `Import` 钮没有 `interactable`（🟠 · 可能与其他代理的「卡组列表页」重叠）** — 原版 `卡组数 < totalCustomDecks(114)` 才可点（`SelectDecksTab__Setup.c:44-53`）；`CollectionWindow.cs:4084/4139` 无此逻辑。

**P7 · 三处文案与原版不一致（⚠️ · 均低风险但用户看得见）**
- `Edit Deck` 在 `state==Import` 时原版换成 `MenuDeck/MenuButtons/ImportDeck`（键**已可读**，见 `ImportDeckPopup.prefab` 的 `importString.mTerm`）——我们仍印英文 `Edit Deck`（`DeckInfoPopup.cs:809-819` 的「键未解出」已过期）。
- 导入错误行：原版 `MenuDeck/Share/InvalidDeckString`（`Invalid deck string`）；我们三条自拟键（`CollectionData.cs:278-296`）。
- 选卡组空态：原版硬编码 `There are no deck in your collection for the selected filters`（无 `Localize`）；我们用官方 `MenuCollection/NoDecksFound`（`DeckSelectionPopup.cs:1006`）。

**P8 · 预组页只列经典 29 副（⚠️ · 已知、用户拍板过）** — 原版列 `practiceDecks` 里全部合法项（含遭遇 12 张）；我们排除（`DeckSelectionPopup.cs:60-75`）。

---

## 四、查不到清单

- **Q1 · `DeckInfoPopup.deckSelectionPopup` 到底指哪一扇选卡组窗 —— 判不出。**
  本地有两个变体：`Deck Selection Popup.prefab`（只有 `DeckSelectionPopup`，`autoInitialize=1`）与 `Deck Selection Popup with Tabs.prefab`（+`DeckSelectionTabController`，`autoInitialize=0`）。`Deck info Popup.prefab` 的 `deckSelectionPopup.m_AssetGUID = e724bbe6c48fc43469cfe5c52b1932d8`，**该 guid 在 ExportedProject 的 `.meta` 里无对应**（只在 `catalog.bin`/`menus_assets_all.bundle` 里出现，addressable 侧 guid 与导出工程的 regenerated guid 不同）。唯一确定的是：`ComponentReference<GameWindow>` 只可能指这两扇之一。
  影响：若原版走**无页签**那扇（`autoInitialize=1`），则「练习对手」这条路原版列的是**玩家自己的卡组里同模式那几副**、且 `deckInfoMode` 默认 `Edit`；我们 `DeckInfoPopup.cs:2289` 开的是**带页签**那扇并落在预组页。搜过：`ExportedProject/Assets/*.prefab`（两个变体都读了根字段）· `StreamingAssets/aa/catalog.bin`（guid 存在但名字未解）· `bundle_Waprforge_monoscripts`（两个变体同名类，无法区分）。
- **Q2 · 原版 `DeckInfoPopup.Close` 里 `deckHero.ReleaseFullHeroSprite()` + `gameModeIcons.Release(gameMode)` 的等价物 —— 判不了该不该有。**
  这两句是原版**资源引用计数**（`fullHeroSprite` 是按需加载的整幅立绘、`GameModesIconCollection` 是 Addressables 图标缓存）。我们的美术是常驻缓存（`CardArt`），没有「租/还」这套；`CardArt.Get` 会把 null 也缓存（见 CLAUDE.md）。⇒ 不是「漏了释放」，而是**机制不同**；但「原版这两句在释放什么」在我们的资源体系里没有对应，**未验**。

---

## 五、顺手发现（不在本三扇窗范围，但当场看见）

1. 📄 **文档内部打架**：`DeckInfoPopup.cs:17`（文件头「结构」摘要）仍写 `Deck Name（fs44.5 居中）+ Warlord Name（fs40 居中）`，而 `:286` 已就地订正为 `Left/Bottom` / `Left/Middle`，代码（`:1471-1486`）也是 `Align.Left`。⇒ 摘要那行是**过期描述**（铁律 5：两份说法打架比没有更糟），建议改掉摘要。
2. 🔁 **`Share` 的闭环断在「读剪贴板」**：`DeckRuntime.CopyDeckToClipboard` 是全仓**唯一**写剪贴板处（`:6044`）；读点只有 `Editor/CollectionScene.cs:3681`（自检）。见 P1。
3. 🧩 **`ViewDeckInfoPopup`（只读版卡组详情窗）在本 build 里【不存在可用实例】**，所以不复刻是**正确的**，不是缺口：
   - 全量反编译里**只有它自己的 5 个方法**，`grep -rl ViewDeckInfoPopup` 扫 `decomp_full` / `out_cpp2il_dev_ilspy` ⇒ **0 个外部调用点**；
   - `bundle_Waprforge_monoscripts/MonoScript/*.json` 里**没有它的 MonoScript**（同批 `DeckInfoPopup` / `DeckSelectionPopup` / `DeckInfoControls` / `ImportDeckPopup` **4 个全在**）⇒ 任何 prefab 都引用不到它；
   - ExportedProject 里只有 `Scripts/Assembly-CSharp/ViewDeckInfoPopup.cs(.meta)`，**没有同名 prefab**。
   ⇒ 结论：原版这份代码是**遗留/被裁**的，判据为空（铁律 11 例外①）。
4. 🧾 **`DeckSelectionTabController` 的 `searchBar` 是「做了但从未启用」的件**：`Awake` 里挂了 `m_OnValueChanged` 监听、`Filter()` 也真在用 `m_Text`，但 `Collection Menu Input Field` 出厂 `act=N` 且 `DeckSelectionTabController` 6 个方法里**零 `SetActive`**（我们文件头那四条证据复核一致）。⇒ 我们的「不建搜索框」成立；⚠️ 但这也意味着 **B10 的预组过滤里那条 `Filter`（按 army/督军名/卡组名）在原版永远收不到非空串**，所以没接不算缺陷。
5. 🔢 **费用曲线两个常量复核无误**（顺手核）：`DeckEnergyCostDrawer` 归一化分母 = **常数 10**、`DeckCostQuantityRowDrawer.maxCostToDraw = 8`（末行印 `8+`），与我们 `Core/CostCurveDrawer.cs:34-50` 的注释逐字一致；抽屉 9 行、`localScale 1.8`、`VerticalLayoutGroup spacing 3.43` 与 dump 一致。
6. 🖼️ **`Deck Info` 抽屉的 `Cardback` 视觉尺寸**（供 P5 施工）：父层布局 210×305 × `localScale 1.85` ⇒ 视觉 **388.50×564.25**（中心 1466,552.6）；子件 `Cardback Front` 布局 387.68×559.32（父链已带 1.85）。
