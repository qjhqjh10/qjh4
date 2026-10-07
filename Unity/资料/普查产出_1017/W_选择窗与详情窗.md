# W · 选择窗与详情窗（D11 / D12 / D16 / D17 / D18）—— 2026-10-17

> 写手代理报告。白名单内改了 3 个文件：`Shell/DeckSelectionPopup.cs` · `Shell/CardDetailPopup.cs` · `Editor/CollectionScene.cs`（只加断言）。
> ⛔ 没跑 Unity；⛔ 没碰白名单外任何文件。

## ① 结论（原版是什么 / 我们原来是什么 / 现在改成什么）

**D11 · 空态**
- **原版**：`…/Collection Display(DeckCollectionDisplay)/Deck Scroll View/` 下**只有一件** `Empty Collection Warning`（出厂 `act=F`），
  rect = **194.5,208.6 → 1759.5,986.7**（整格视口）、**无 Image**；子件一颗 TMP 叫 **`Warning`**，文案
  `'There are no deck in your collection for the selected filters'`，**fs 36.0 · base 36.0 · 无 auto · 折行 1 · 色(1,1,1,1)**。
  判据 = 字段 `DeckCollectionDisplay.emptyWarning` + 解包 prefab 现读（`menu_dump.py … "Deck Selection Popup with Tabs" --depth 6`）。
- **原来**：空态叫 `Empty Note`（`(SvL,SvT+120)-(SvR,SvT+190)` · fs 32 · 灰 0.66），**另有**一行自己加的 `Scope Note`（红底板下沿 · fs 22）—— **原版没有**。
- **现在**：`Scope Note` 节点**删掉**；空态那件**改名 `Empty Collection Warning` + 子节点 `Warning`**，版面照原版（视口整格 · fs 36 · 白 · 居中 · 折行）。
  ⚠️ **文案仍是我们自己的三条**（说明「为什么空」—— 红线「不许静默失败」；原版那句是写死的英文，还带原文语法错误 "no deck"）⇒ **出声**，不是偏离。
- 落点 `DeckSelectionPopup.cs:399-424` / `:628-632`（`RefreshEmptyNote`）· `:661-678`（`RefreshScopeNote`+`ScopeNoteText`）。

**D12 · 页签选中态**
- **原版**（逐 MB 现读：`MonoBehaviour_907016068568626559.json`（`m_IsOn=1`）/ `…_-4477857345849663325.json`（`m_IsOn=0`），**七个同名件字段逐字相同**）：
  `changeSpriteOnValueChange=**1**` · `colorTintOnValueChange=**1**`（**两个开关都开** ⇒ 换图与染色都真生效）· `onSprite=40K_tab_button` ·
  `offSprite=40K_tab_button_overwindow` · `onColor=(1,**0.6308285**,0,1)` · `offColor=(1,**0.5442529**,0,1)`；两颗 `Button Text` 的 TMP 都是 **`(1,1,1,1)`**。
  落地 = `decomp_full/EverguildToggle__RefreshVisuals.c`（按**自己那颗** `m_IsOn` → `ToggleSprite` / `ToggleTint`）。
- **原来**：两张底图**按页签写死**（Pre→`40K_tab_button` / Own→`…_overwindow`，切换**不换图**），选中态**只改文字色**（白 / 灰 0.6）。
- **现在**：底图与染色**都由 `OwnDecks == own` 取**（切页 ⇒ 两张图 / 两个色**互换**）；文字**两态都白**。落点 `:455-497`（新增 `TabOnArt/TabOffArt/TabOnColor/TabOffColor`）+ `BuildTab`。
- ⚠️ 为什么染不到字：`Button Text` 组件表 = `TextMeshProUGUI,EverguildTextController,Localize`，**没有 `EverguildButtonMaterialModifier`**，而 `ToggleTint` 只染带那个组件的 graphic。

**D16 · `Ban Icon` / `Create` 两态 —— 触发条件查到了；两个态在本窗都出不来 ⇒ 不建（照做）**
- 判据 = `decomp_full/CollectionDeck__Config.c`（**唯一一处**动这两个 GameObject 的代码）；字段↔节点：`content`=0x68 · `highlight`=0x70 ·
  **`createObject`=0x78** · **`cardObject`=0x80** · `gameModeIcon`=0x88 · **`bannedImage`=0x90** · `greyscaleController`=0x98 · `easyMark/normalMark/hardMark`=0xa0/0xa8/0xb0
  （旁证：`+0x60` 按 `PrebuiltDeck.difficulty` 换 0xa0/0xa8/0xb0 三图 ⇒ 必是 `deckDifficulty`；`+0x50` 收卡背、`+0x58` 收 `ArmyIconsSO.GetArmyIcon`）。
- **`Create`**：`SetActive(0x78, item == null)` ⇒ **「这一格没有卡组」的空位格**（`Config(null)`），文案 `Create\nNew Deck`。
  🔴 **本窗永远不会有**：`DeckSelectionTabController.ShowOwnDecks / ShowPrebuiltDecks` 传的都只是 `GetInventory<…>().Where(筛选).ToArray()` / 预组数组，**没有一处塞 `null`**。
- **`Ban Icon`**：`SetActive(0x90, cVar4)`，`cVar4 = GetLibraryInFull().Any(c => c.CustomGameModeEvent.IsCardBanned(c))`（谓词 = `CollectionDeck.__c__DisplayClass16_0.<Config>b__0` → `PlayEventData.IsCardBanned`）
  ⇒ 触发条件 = **这副牌里有一张被「该卡组的自定义活动」禁掉的卡**；命中时同时 4 张图转灰 + `Deck Name` 色 → `(1,0.3,0.3,1)`。
  🔴 **本 build 判据是空的**（禁卡表在 LiveOps / 远端 CCD；用户边界「过期的活动不做」）⇒ 永不触发。
- **落码 = 0**。长注释落在 `DeckSelectionPopup.cs:573-595`。⚠️ 真要补，落点在 `Shell/MenuDraw.cs` 的 `DeckCell`（与收藏窗 Deck 页共用）—— **白名单外**。

**D17 · 异画面板「没有异画 ⇒ 整块关」**
- **原版**（`AlternateArtPanel__Initialize.c:22-38`，本轮复读）：`HasAlternativeArtStyles(card) == false` ⇒ 填 0 后 **`GameObject.SetActive(this.gameObject, 0)`**（`:33`）；有异画那支才 `SetActive(…,1)`（`:38`）
  ⇒ **「整块关」、节点留在树上**（⛔ 不是「画成 0 of 0」、⛔ 也不是「不建」）。
- **原来**：`CardArt.AltArt(Card.Id) != null` 只决定**文案/计数**，**面板恒开**（画 `Alternate art / 0 of 0 / No alternate art`）。
- **现在**：`BuildAltArt` 末尾 **`p.gameObject.SetActive(has)`**（`CardDetailPopup.cs:733`）；`has` 改成**数据判据** `HasAltArtStyle(id)`（`:695`）—— 查 `CollectionWindow.AltArtCards`（本机 7 张督军异画，出处 `工具/import_original_art.py` 的 `ALT_ART`），等价于原版那句 `HasAlternativeArtStyles`（它查服务端 `CardsSpecialConfig.CanShowAACard`，本地取不到）。
  ⛔ **故意不用 `CardArt.AltArt(id) != null`**：那是「贴图加载成功没有」——`Resources/Art/` 一缺图就会把**所有**卡的这块都关掉，语义错。
- **两扇详情窗都查了**：全工程 grep `异画|AlternateArt|Alternate art` ⇒ **`Battle/` 下零命中** —— 战斗那扇（`Battle/CardDisplayWindow.cs`）**一条 options 面板都没建**。
  🔴 **2026-10-17 订正（铁律 5，主对话落）**：本行原来接着写「原版战斗侧**可能**出这块 ⇒ **是缺口、不是「原版没有」**」—— **记反了**。同日 B4 批逐字段现核：**13 个竞技场**的 `CardDisplayWindow.options` **全是 `{0,0}` = null**（`informationPanel`/`tryShowResources` 同样 13/13 为 0），**只有菜单版有**；`CardDisplayWindow__ShowCard.c:159` 的守卫 `if (options == null || …) goto …` ⇒ **战斗侧永远不出** ⇒ **我们不建 = 与原版一致（不是缺口）**。判据全文 → `W_B4_战斗详情窗.md`。

**D18 · `Select Deck` 入口 —— 记一行，⛔ 不建**
- 判据**已存在**：`Shell/DeckInfoPopup.cs:66-70`（6 个 `DeckInfoContext` 调用点全传 `null` + `set_SelectButton` 全库无调用者）+ `:109`（偏移表第 2 行 `selectButton` = 节点 `Select Deck`）。本轮在白名单文件补上那一行：`DeckSelectionPopup.cs:48-58`。
- 🔴 **如实标注未验的部分**：全量反编译里**只有 `DeckInfoPopup.SelectPracticeOpponentDeck` 一处**用 `deckSelectionPopup`（`param_1[0x1a]`，`grep -l "param_1\[0x1a\]"` 全 `decomp_full` 只此一命中），**`Select Deck` 那颗钮的 onClick 落点没有反编译产物** ⇒「它是不是本窗的第 5 条入口」**连线未验**，⛔ 没写成本窗的入口。⇒ 账上 D18「只差文档一行」**本轮已销**。

## ② 证据（路径 + 字段 / 行号）

| 事 | 判据 |
|---|---|
| 空态只有一件 + 字号/矩形 | `menu_dump.py bundle_menus_assets_all "Deck Selection Popup with Tabs" --depth 6` ⇒ `Empty Collection Warning`（`act=F`，194.5,208.6→1759.5,986.7）+ 子 `Warning`（`fs 36 · base 36 · Center/Middle · 折行 1`）；字段 `DeckCollectionDisplay.emptyWarning` |
| 页签 on/off 图与色 + 两个开关 | `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_907016068568626559.json`（`m_IsOn=1`）· `…_-4477857345849663325.json`（`m_IsOn=0`）；落地 `EverguildToggle__RefreshVisuals.c` / `__ToggleSprite.c` / `__ToggleTint.c` |
| `Ban Icon` / `Create` 触发 | `CollectionDeck__Config.c` · `CollectionDeck.__c__DisplayClass16_0___Config_b__0.c` · `DeckSelectionTabController__ShowOwnDecks.c` / `__ShowPrebuiltDecks.c` · `d:/2/Warpforge_code/Scripts/Assembly-CSharp/CollectionDeck.cs`；全表旁证 `资料/普查产出_0923/A2_Deck页.md` §2·2 / §2·3 |
| 异画面板整块关 | `AlternateArtPanel__Initialize.c:24` / `:33` / `:38` · `RawCardScript__HasAlternativeArtStyles.c`（→ `CardsSpecialConfig.CanShowAACard`） · `工具/import_original_art.py` 的 `ALT_ART` |
| `Select Deck` 永不出现 | `Shell/DeckInfoPopup.cs:66-70` / `:109` · 本轮补 `decomp_full/DeckInfoPopup__SelectPracticeOpponentDeck.c` |

## ③ 改动清单（文件 / 改了什么 / 断言盯什么）

**`Shell/DeckSelectionPopup.cs`**（+144/−27，705 行）
- D11：删 `Scope Note`；空态改 `Empty Collection Warning` + 子 `Warning`（视口整格 / fs 36 / 白 / 折行）；`_emptyNode`/`_emptyLabel` 字段化。
  🔴 `Build` 建完**立刻**补一次 `RefreshEmptyNote()` —— **必须有**：`RebuildCells` 那次跑在它**之前**（当时 `_emptyNode` 还是 null），
  否则「列表非空」时这一件会**留在开着**（只是文案空串、看不见 —— 静默错），自检那条 `activeSelf == false` 会红。
- D12：`BuildTab` 去掉 `art` 形参，底图/染色按 `on` 取；颜色**逐字抄 prefab 浮点**（⛔ 没四舍五入成 0.631/0.544）。
- D16 / D18：只加判据注释（偏移表、触发条件、出处、未验部分）。`ScopeText` 保留 + 注明「已不上屏」，`RefreshScopeNote` 改 `Debug.Log` 出声。

**`Shell/CardDetailPopup.cs`**（+40/−1）：新增 `public static bool HasAltArtStyle(string)`；`BuildAltArt` 末尾 `p.gameObject.SetActive(has)`；文件头补 D17 与战斗侧缺口记录。

**`Editor/CollectionScene.cs`**（+142/−0，**纯追加**，两节）
- `:2261-2358`（`Practice Deck` 那一节，`sel` 刚开出来）—— **14 条 + 3 条前提**：
  D12 起手 4 条（`CheckArt`×2 = 原版 `onSprite`/`offSprite` 图名；`CheckTint`×2 = 原版 `onColor`/`offColor` 字面量）·
  D12 文字 2 条（**两颗颜色必须相同** + 共同值 = 白 ⇒ 改前那套「一白一灰」必红）·
  D11 5 条（`Empty Collection Warning` 在 · 非空时**关着** · `CheckAt` **原版 rect** · 子 `Warning` 在 · **fs = 36**）·
  `Scope Note` **不存在** 1 条（改坏法：把 `Build()` 第 7b 步加回来 ⇒ 红）·
  D12 判别式 4 条（`SwitchTab(true)` 后**两图互换** + 两色互换）· 前提 3 条（起手在预组页 / 预组页非空 / 两颗文字取得到）。
  ⚠️ **节点每次都现取**（`SwitchTab` → `RebuildAll` 会销毁重建，缓存引用会**假红**）。
- `:4921-4970`（卡片详情窗那一节**末尾**，`cd` 那一支）—— **6 条**（D17）：前提 2 条（异画表里有卡在本地卡池 / 当前这张**不在**异画表里）+
  ①有异画 ⇒ 面板 `activeSelf == true` + ②没异画 ⇒ 节点**仍在** 且 `activeSelf == false` ⇒ 两向合起来才同时关住「恒开」与「恒关」。末尾 `cd.Close()` 交还一个**关着**的窗。

⚠️ **断言期望值一律写原版字面量**（`40K_tab_button` / `(1,0.6308285,0,1)` / `194.5,208.6,1759.5,986.7` / `36f`），⛔ **没有**引用 `DeckSelectionPopup.TabOnArt` / `TabOnColor` 这类被测实现侧常量（避免自证）。
⚠️ **本批没跑 Unity 自检**（按简报由主对话收口时统一跑）。覆盖到的宿主 = `CollectionScene.Run` 这一条。

## ④ 没查清 / 欠账

1. 🔴 **`Editor/MainMenuScene.cs:2651` 与 `:2670` 现在断的是「一个不上屏的字符串」** —— 那两条读 `ds2.ScopeText`（`Contains("本页列 "+tab.Count)` / `Length == 0`）。
   D11 把那一行从屏上删了，而**该文件不在白名单** ⇒ 属性没删（删了整工程编不过）⇒ **拿到 `MainMenuScene.cs` 时必须把那两条改成「`Scope Note` 节点不存在」或直接删**。
   ⚠️ **它们今天仍绿**（字符串照算）—— 别被绿骗了。已写进 `ScopeText` 的注释（`DeckSelectionPopup.cs:219-226`）。
2. **D16 两态没有落码**（判据已查到、结论是「本窗出不来」）；**没为它写断言**（没有改动可配，也不写负向断言去钉死一个决定）。真要补落点在 `Shell/MenuDraw.cs` 的 `DeckCell`（白名单外）。
3. **D18「`Select Deck` 是否连本窗」连线未验**（那颗钮的 onClick 落点无反编译产物）—— 只验到「它在本 build 永不出现」。
4. **异画面板的其余偏离不在本批范围**（左右钮 / 风格名 / `Buy original card` 那几件仍是我们的近似版）—— D17 只关「有没有 ⇒ 开关」。
5. ~~**战斗侧详情窗完全没有 options 三块面板**~~ ⇒ 🔴 **2026-10-17 订正（铁律 5）：原版战斗版【也没有】**（13 个竞技场 `options` 全 null；只有菜单版有）⇒ **不建 = 与原版一致**，**不是缺口**（本批只如实记的那句已作废；判据 → `W_B4_战斗详情窗.md`）。

## ⑤ 类型检查（`TMPDIR=/tmp/wf_b2 bash d:/4/Unity/工具/typecheck.sh`）· 行尾

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```
⚠️ 中途三次重跑各出现 **1 条**错，**全部落在别人的在飞文件上**（按简报口径：不是我负责的文件 ⇒ 不是我的问题）：
`Deck/DeckRuntime.cs(938,13) CS0103 _verdict` · `Editor/DeckScene.cs(1984,54) CS1061 UiInfoActionsVisible` · `Shell/SettingsWindow.cs(648,24) CS0103 BuildGeneralPage` —— 隔几分钟重跑即消失。**没有去改别人的文件。**

行尾：三个文件改前都是**纯 LF**（`b.count(b'\r\n') == 0`）；`git diff --numstat` = `144/27` · `40/1` · `142/0`，与文件行数（705 / 1006 / 6759）相比是**小数字** ⇒ **没有翻行尾**。改文件走 Edit 工具与 python `wb`（⛔ 没用 `sed -i`）。
