# REV_W1 · 独立审查：「卡组线写死英文 ⇒ 词条化」交件（2026-10-18）

> 审查代理 `REV_W1`。**只读**：一个字都没改（唯一写出的文件 = 本文件）。
> 未跑 Unity · 未动 git · 未改正本。
> 审查对象 = `资料/普查产出_1018/W1_词条化交件.md` §1–§8 + 落在 10 个文件上的改动。
> 🔴 本审查**不采信** `Core/Loc.cs` 里已有的键，**每一条都回原版 prefab 亲读**
> （`bundle_menus_assets_all` 的 `MonoBehaviour/*.json` · `mTerm` + 同 GO 上 TMP 的 `m_text`；
> 父链沿 `RectTransform.m_Father` 逐级走）。
> 索引口径：本审查**自己重建了一遍**该包的 `Localize` 全表 ——
> `m_Script == 8610481073976370760` 的实例 **901 个 / 不同 `mTerm` 309 条**
> （`MonoBehaviour/` 文件共 35,014 个）。

---

## 1. 结论一句话

**真问题 1 条（`Energy Cost` 的「原版没词条」是错的，且被本批断言钉死）+ 漏网 6 条 + 计数/名号类小错 4 条 + 存疑 2 条；作者自陈不成立 1 条（就是 `Energy Cost` 那条）。14 处主件里 13 处的键与英文串逐字符正确、父链窗口正确，唯一错的是 `DeckRuntime.cs` 的一处中间节点名。**

---

## 2. 14 处逐处核验表

> 「你实读的 mTerm」列 = 本审查在 `d:/2/新解包资源/assets_full/bundle_menus_assets_all/` 亲自读到的值。
> 父链写法 = 由近到远。

| 处 | 他写的键 | 你实读的原版 mTerm + 原版英文（证据） | 一致? | 父链对不对 |
|---|---|---|---|---|
| 1 | （改值）`Card_Race/Invocation` 中文 计策→祈唤 | `zh_CN.csv:6386` = `Invocation,~,祈唤` ✓；卡图亲读 `Tau/4计策/Warpforge_52_Sense-of-Stone.png` 兵种行 = **`Invocation`** ✓ | ✅ | — |
| 2 | （改值）`Card_Race/Stratagem` 中文 战术→策略 | `zh_CN.csv:6378` = `Stratagem,~,策略` ✓；`:6366` = `Tactic,~,战术`（撞名成立）✓ | ✅ | — |
| 3 | `MenuDeck/Filters/ClearFilters` | `mTerm` ✓ · TMP `Clear filters` ✓ · 5 颗 = `MB_-8938305841530999004 / _4555408510099907285 / _2943367224909750997 / _439117977700524757 / _8308478411536196309`（他列的 5 颗 pid **逐个对上**） | ✅ | ⚠️ **窗口对、中间节点名错**（见 P3） |
| 4 | `MenuDeck/Filters/Army` | ✓ · TMP `Army` ✓ · **6 颗**（他写 6 ✓），本窗那颗 = `MB_5608480798279724836` ✓ | ✅ | ✅ `Deck Editing Menu > Content Area > Cosmetic Display > Cosmetic FIlter > Filters > Army Filter` |
| 5 | （`DeckRuntime` 纵向档）`Midline` | `MB_6959005812579893394.json` 实读 `m_text='Confirm'` · **`m_VerticalAlignment=4096`** · `m_HorizontalAlignment=2` · `m_fontSize=45` ✓ | ✅ | ✅（`Import Deck Popup > Window > Buttons > Generic UI Button > Button Text`） |
| 6 | `MenuDeck/Filters/ClearFilters` | 同 #3 ✓ | ✅ | ✅ 4 颗全在 `Collection Menu Variant > Content Area > Tabs > {CardsTab/Header Filters · Cardback Tab/Header · Alternate Art Tab/Header Filters · Select Deck Tab/Header} > Clear Filter Button`（**`Clear Filter Button` 是这一族才有的父名**） |
| 7 | `MenuDeck/MenuButtons/CreateDeck` | ✓ · TMP **`Create Deck`（大写 D）** ✓ · 1 颗 = `MB_-1342537574798531883` ✓ | ✅ | ✅ `Collection Menu Variant > Content Area > Tabs > Select Deck Tab > Header > Control Buttons > Create/Button Text` |
| 8 | `MenuDeck/MenuButtons/ImportDeck` | ✓ · TMP `Import Deck` ✓ · 1 颗 = `MB_3470913049079964373` ✓ | ✅ | ✅ 同上（兄弟 `Import/Button Text`） |
| 9 | `MenuDeck/Filters/Army` | 同 #4 ✓ · 本窗那颗 = `MB_4174824372200334037` ✓ | ✅ | ✅ `… > Decks Tab > Select Deck Tab > Collection Display > Deck Filters > Army Filter/Title` |
| 10 | `MenuDeck/HUD/SearchFilter` | ✓ · TMP `Search` ✓ · **11 颗**（他写 11 ✓）· 本窗那颗 = `MB_2787539817899942613` ✓ | ✅ | ✅ `… Decks Tab > Deck Filters > Deck Name Filter > Input Field > Text Area > Placeholder` |
| 11 | 同 #10（`FilterPanelModel` 改属性） | 同 #10 ✓ | ✅ | — |
| 12 | `MenuDeck/Button/Random` | ✓ · 全库**只 1 颗** = `MB_-725143033836620417` ✓ · **TMP `m_text = ''` 实读确认空串** ✓ · 所在 GO 文件 = **`Button Text_7450249239293310335.json`** ✓（他写的就是这个 pid） | ✅ | ✅ `Deck Selection Popup with Tabs > Deck Display > Header > Practice buttons > Generic Simplified UI Button > Button Text` |
| 13 | **`MainMenu/Ranked/GoToCreateDeck`** | ✓ · TMP **`Create deck`（小写 d）** ✓ · 3 颗 = `MB_6456082183790828145 / _2897067765952630130 / _5230329239126443952` ✓ | ✅ **本批最容易错的一处，他判对了** | ✅ `{Ranked Deck Selection · RankedEventWindowV2 · SkirmishModeEventWindow} > Ranked Deck Selection > No Deck Text > Generic Simplified UI Button > Button Text` |
| 14 | （`ImportDeckPopup` 纵向档） | 同 #5 ✓ | ✅ | ✅ |

**§8 A 组**

| 处 | 他写的键 | 实读 | 一致? |
|---|---|---|---|
| `Army → MenuDeck/Filters/Army` | 6 颗 · TMP `Army` | ✅ |
| `Rarity → MenuDeck/HUD/Rarity` | 3 颗 · TMP `Rarity` | ✅ |
| `Type → MenuDeck/Filters/Type` | 3 颗 · TMP `Type` | ✅ |
| `Energy Cost → null（留英文）` | 🔴 **错** —— 全库 17 颗 TMP 上**每颗都挂着 `Localize.mTerm = "Battle/Tips/EnergyCost"`** | ❌ **见 P1** |

**§8 B 组**

| 处 | 他写的键 | 实读 | 一致? | 父链 |
|---|---|---|---|---|
| `CollectionWindow.cs` 页头 `"Filters"` → `MenuDeck/Filters/Filters` | ✓ · 5 颗 · TMP `Filters` ✓ | ✅ | ✅ 四颗在 `Collection Menu Variant > … > {CardsTab/Header Filters · Cardback Tab/Header · Alternate Art Tab/Header Filters · Select Deck Tab/Header} > Filter Toggle > label`（另 1 颗在 `Deck Editing Menu > … > Header > Filters > Label`） |
| `DeckRuntime.cs` `imp_ok_t` → `MainMenu/General/Confirm` | ✓ · 3 颗（本窗那 1 颗 TMP `Confirm`；同键另两颗 TMP 是 `CONFIRM` / `Confirm `） | ✅ | ✅ `Import Deck Popup > Window > Buttons > Generic UI Button > Button Text` |

---

## 3. 必查 2 / 3 / 4 / 5 / 6 / 7 逐条结论

### 必查 2 —— `A879` 那两行的判据（三条独立复核）

| 他/主对话的判据 | 独立核验 | 结论 |
|---|---|---|
| ① 卡图 `…/Warpforge_52_Sense-of-Stone.png` 兵种行印 `Invocation` | **我自己 `Read` 了那张 png**：卡名 `Sense of Stone` · 阵营行 `T'au Empire` · 效果行 `Give ⟨盾⟩ Armour 1 to your troops` · **底部橙字（兵种行）= `Invocation`** | ✅ 成立 |
| ② 「计策」是**卡类别**（`type='tactic'`、PnP 目录 `4计策/`） | 卡表实读 `Unity/数据/游戏数据/card_stats.json` → `Sense of Stone` 的 `type == "tactic"` ✓；全表 `type` 分布 = `{unit:637, tactic:478, defence:40, hero:58}` ✓；PnP 目录实读 = `d:/2/Warpforge部队卡片/Tau/4计策/` ✓（⚠️ 目录名是 **`Tau`**，不是 `T'au`） | ✅ 成立（原值与卡类别撞名） |
| ③ `Stratagem` 与 `Tactic` 同译「战术」撞名 | `Core/Loc.cs:423` `Card_Race/Tactic = 战术` ✓；改动前 `:419` `Card_Race/Stratagem = 战术` ✓ | ✅ 成立 |

**改动后**还有没有别的撞名 —— 我把 `Loc.cs` 里**全部 79 条** `{key, new Entry(zh, en)}`（`grep -c "new Entry("` 也 = 79 ⇒ 穷举）做了双向比对：

| 撞名 | 键 | 判断 |
|---|---|---|
| `创建卡组` | `MenuDeck/MenuButtons/CreateDeck` + `MainMenu/Ranked/GoToCreateDeck` | 🟢 **不是新缺陷** —— 两条的英文源串 `Create Deck`(`csv:7`) 与 `Create deck`(`csv:230`) 在我们那份表里**本来就是同一个中文**，照抄没错；且两条指向的是同一件事（建卡组） |
| `任务` | `Card_Race/Mission` + `Card_Race/mission` | 🟡 **本批之前就有**（大小写两条键同名，是既存状态，不是本批引入） |
| `咒语` | `Card_Race/Spell` + `Card_Race/spell` | 🟡 同上，本批之前就有 |

英文列撞名 = **0 组**。⇒ **本批没有引入新的语义撞名。**

### 必查 3 —— 断言鉴别力

1. ✅ **「键写死在测试里、不从 `FilterPanelModel` 读回来」属实。**
   `Editor/DeckScene.cs:214 ExpectedTitleText` 里三条 `case` 的字面量就是键，**不调** `FilterPanelModel.TitleTerm/TitleText`。
   ⇒ 若把 `TitleTerm` 的 `"Army"` 映射改错，测试的期望值（`Loc.T("MenuDeck/Filters/Army")`）与渲染值会分叉 ⇒ **会红**。不是自证。
2. ✅ **「删实现 / 改回写死英文会不会红」—— 抽查 8 条，7 条会红**：
   `DeckScene.cs:2033`（⚠️ 这条**不经过** helper，直接 `Loc.T("MenuDeck/Filters/Army")` ⇒ 即使把 `ExpectedTitleText` 也一起改回 `return title`，它照旧红 —— 这是本批**唯一一条真正「灭自证」**的断言，形状对）·
   `CollectionScene.cs:1094/1096`（Import/Create 钮）·
   `CollectionScene.cs:3946` 区（占位符）·
   `CollectionScene.cs:4348` 区（页头 Filters/Clear filters）·
   `DeckScene.cs:2407` 区（`imp_ok_t`）·
   `DeckScene.cs:4965` 区（`cosmoflt_title`）。
3. ✅ **「`CollectionScene.TitleLeftPx` 只量左沿、不比字」属实**（`:390-398`：`FindChild` 按名找 → `LabelRenderedPx` 量宽 → **全程不与 `title` 比字符串**）。
   ⇒ 那 4 条左沿断言（`:3858 / :3861 / :3875 / :3877` 与 `:5804`）在本批**确实不会红**，作者的理由成立。
   同理 `sTitleNames`（`:5501`）与 `:5708` 只读字号，不红 ✓。
4. 🔴 **有一条断言在「锁死缺陷」**：`Editor/CollectionScene.cs:3871` 把 `Energy Cost` **钉成恒英文**（连同 `Editor/DeckScene.cs:209-221` 的 default 分支）⇒ 下一个人要按原版接上 `Battle/Tips/EnergyCost` 时，**会先撞红这三处**。这是比「漏做」更坏的一档：它把错口径写成了检验标准。
5. ⚪ **仍有两条口子，本批未堵**（见 §5 存疑 S1/S2）。

### 必查 4 —— 结构约束（节点名一字未动）

✅ **成立。** 逐字读改动确认：
- `DeckRuntime.cs:1675`：节点名那半句仍是 `"flt_title_" + tl.Text.Replace(" ", "_")`，**只有显示文案那个实参**换成了 `FilterPanelModel.TitleText(tl.Text)`。
- `CollectionWindow.cs:2074`：节点名仍是 `"Title " + text`（英文原名），只换了显示文案实参。
- 其余 12 处的**节点名实参**（`hdr_clear_t` / `cosmoflt_title` / `imp_ok_t` / `"Clear filters Text"` / `"Create Text"` / `"Import Text"` / `"Random Text"` / `"Filters Label"` / `"Button Text"`(LiveOps) / `"Title"`）也逐个核过 —— **全部未动** ✓。

### 必查 5 —— `InputPlaceholder` 由 `const` 改属性

✅ **「全仓只 2 个只读消费点」是穷举。**
`grep -rn "InputPlaceholder" --include=*.cs Unity/`（排除 `obj/`）的全部命中：
- 定义处 `Core/FilterPanelModel.cs:121/124/127`（+ 2 处 doc）
- **代码读点 2 个**：`Deck/DeckRuntime.cs:4933` · `Shell/CollectionWindow.cs:1965` —— 都是 `string.IsNullOrEmpty(cur) ? FilterPanelModel.InputPlaceholder : cur` 的**三目只读**（含 `Editor/` 与 `RuleEngine/`，两处都没有）。
- 其余全是 `Editor/{DeckScene,CollectionScene}.cs` 与 `Shell/CollectionWindow.cs:2735` 的**注释**。
✅ **无 `const` 语境**：全仓 `InputPlaceholder` 没有出现在 `case` 标签 / 特性 / `switch` 里（专项 grep 零命中）；改属性后语义不变，类型检查由作者跑过（0 错）。
（`InputPlaceholderTerm` / `InputPlaceholderEn` 两个新常量把「键」与「英文原文」留了出口 ✓。）

### 必查 6 —— 「我们自拟」有没有被写成「原版」

| 处 | 代码/注释里怎么写的 | 结论 |
|---|---|---|
| `MenuDeck/Button/Random`（`Loc.cs` + `DeckSelectionPopup.cs:407-410`） | 明写「🔴 **英文原文取不到** …… 原版那颗 TMP `m_text = ''`（**本批实读**）⇒ **中英两列都是我们拟的**」「⛔ 别写成『照抄原版』」 | ✅ **如实**（且我独立读到了空串） |
| `MenuDeck/MenuButtons/ImportDeck` 的中文 | 明写「**中文列是我们自拟的**：`zh_CN.csv` 里没有 `Import Deck` 这个键」 | ✅ 如实（我精确查 `zh_CN.csv` 第一列 == `Import Deck`，**0 命中**） |
| `MainMenu/Ranked/GoToCreateDeck` | 引 `zh_CN.csv:230` | ✅ 属实（`:230` = `Create deck,~,创建卡组`） |
| 🔴 **A 组 `Energy Cost`** | `Core/FilterPanelModel.cs:649-659` · `Editor/DeckScene.cs:209-221` · `Editor/CollectionScene.cs:3871` 三处都写「**原版本身没有**（铁律 11 例外①）」 | ❌ **把「没查到」写成了「原版就是这样」** —— 见 P1 |

⇒ 除 `Energy Cost` 一处外，「自拟」都如实标注了；没有别的把我们的选择冒称原版的地方。

### 必查 7 —— 行尾（二进制读，自己数）

| 文件 | `\r\n` | `\n` | 判定 |
|---|---|---|---|
| `Deck/DeckRuntime.cs` | **6169** | **6169** | CRLF ✓ 未翻 |
| `Editor/DeckScene.cs` | **5442** | **5442** | CRLF ✓ 未翻 |
| `Core/Loc.cs` | 0 | 561 | LF ✓ |
| `Core/FilterPanelModel.cs` | 0 | 838 | LF ✓ |
| `Shell/CollectionWindow.cs` | 0 | 3076 | LF ✓ |
| `Shell/DeckSelectionPopup.cs` | 0 | 742 | LF ✓ |
| `Shell/ImportDeckPopup.cs` | 0 | 462 | LF ✓ |
| `Shell/LiveOpsEventWindow.cs` | 0 | 1181 | LF ✓ |
| `Editor/CollectionScene.cs` | 0 | 7107 | LF ✓ |
| `Editor/MainMenuScene.cs` | 0 | 10695 | LF ✓ |

**10 件全部无翻行尾**；两件 CRLF 的计数与作者报的（6169/6169 · 5442/5442）**逐数相同** ✓。

---

## 4. 新发现的撞名 / 漏网

### P1 🔴 `Energy Cost` 的判据是错的（**真问题**，且被断言钉死）

**他的结论**：「全库 `m_text == "Energy Cost"` 的 TMP 共 **3 颗**、**一颗 `Localize` 都没挂** ⇒ 原版在任何语言下都印英文 ⇒ 铁律 11 例外① ⇒ 不接词条。」

**实读（我自己跑的，可复跑）**：
- 全库（`d:/2/新解包资源/assets_full` 下**所有** bundle）`m_text == "Energy Cost"` 的 TMP = **17 颗**，分布在 **15 个 bundle**：
  `bundle_menus_assets_all` **3** 颗 · 13 个 `bundle_scenes_scenes_battlearena*` 各 1 颗 · `bundle_scenes_scenes_mainmenuwarpforge` 1 颗。
- **这 17 颗，每一颗所在的那个 GameObject 上，都挂着 `Localize`，`mTerm` 一律 = `"Battle/Tips/EnergyCost"`。**
- 其中 **3 颗就是本批那四行筛选小标题里的 `Energy Cost` 行**，父链 = `Title < Cost Filter < Filters < Viewport < Scroll View < Card Filters < …`：
  | TMP pid | 所在 GO 文件 | `Localize` 文件（`mTerm`） | 父链窗口 |
  |---|---|---|---|
  | `-7201797398173261099` | `Title_5522003964329779925.json` | `MonoBehaviour_-2584076255982591275.json` = `Battle/Tips/EnergyCost` | `Collection Display > CardsTab > Tabs > Content Area > Collection Menu Variant` |
  | `3514715868034035492` | `Title_-4212939967390290140.json` | `MonoBehaviour_4049109910718639908.json` = `Battle/Tips/EnergyCost` | `Card Display > Content Area > Deck Editing Menu` |
  | `677985213984531157` | `Title_2409293289170264789.json` | `MonoBehaviour_2278421928472928981.json` = `Battle/Tips/EnergyCost` | `Collection Display > Alternate Art Tab > …` |
- **交叉判据**：`数据/本地化/i18n/zh_CN.csv:98` = `Energy Cost,~,能量费用` —— 我们那份译表**本来就有这一条**（他只查了 C# 侧，没查 CSV 第一列）。

**为什么这是缺陷而不是「没做」**：另外三行（`Army`/`Rarity`/`Type`）接了词条、中文档印中文，**同一面板里的 `Energy Cost` 印英文** ⇒ 界面自相矛盾。
**要做**（判据 → 上面那 3 条 `Localize`，铁律 11 没有例外）。**先做这个**，且要先拆掉下面这两处「钉死缺陷」的断言：
- `Editor/CollectionScene.cs:3871`（`CheckText(TextOf(FindChild(fltPanel,"Title Energy Cost")), "Energy Cost", …)`，消息里写着「⛔ 别给它编一条」）
- `Editor/DeckScene.cs:209-221` `ExpectedTitleText` 的 `default` 分支（连带 `TitleLeftPx` / `FilterTitleLabel` 两条 helper）
- 以及 `Core/FilterPanelModel.cs:649-659` `TitleTerm` 的 doc 与 `default: return null`

### P2 🟡 同一处的计数错 + 「全库」的范围没打出

「3 颗」实为 **17 颗**；「全库」实际只扫了 `bundle_menus_assets_all` 一个包。
（这正是「报『没有』之前必须先打出**搜过哪几个包**」那条 —— 本次审查搜的包与词见 §6。）

### P3 🟡 `DeckRuntime.cs:968` 的父链节点名写错

代码注释写「本窗这颗的父链 = `Deck Editing Menu > Content Area > Header/Filters/Clear Filter Button/Button Text`」。
**实读**：那颗 `Button Text` 的直接父是 **`Generic Simplified UI Button_updated`**，不是 `Clear Filter Button`
——`Clear Filter Button` 是**收藏窗那 4 颗**（`… > Clear Filter Button < Filters < Header Filters …`）的父名，作者把两棵树的中间节点混了。
**键与英文串本身都对**，只错这一处节点名。

### P4 🟡 漏网（同族仍是写死英文，且**原版确有词条**）—— 全部在 10 个文件里

| # | 我们写死的地方 | 文案 | 原版 `Localize.mTerm`（本审查亲读） | 我们表里有没有 |
|---|---|---|---|---|
| 1 | `Shell/CollectionWindow.cs:841` · `:1407` · `Deck/DeckRuntime.cs:1970` | `There are no cards in your collection for the selected filters` | **`MenuCollection/NoCardsFound`** | ❌ 没有 |
| 2 | `Shell/CollectionWindow.cs:1017` | `There are no cardbacks in your collection for the selected filters` | **`MenuCollection/NoCardbackFound`** | ❌ 没有 |
| 3 | `Shell/CollectionWindow.cs:2598` | `There are no decks in your collection` | **`MenuCollection/NoDecksFound`** —— ⚠️ **原版串与我们的不一样**：原版 TMP 是 `There are no deck in your collection for the selected filters`（单数 `deck`，原版自己的语病） | ❌ 没有 |
| 4 | `Shell/CollectionWindow.cs:993` | `Your cosmetics collection` | **`MenuCollection/Label/Cosmetics`** | ❌ 没有 |
| 5 | `Shell/CollectionWindow.cs:2401` | `Back` | **`MainMenu/MainButtons/ButtonLabel/Back`**（该键 8 颗之一 = `Button Text < Close Button < Shared < Tabs < Content Area < Collection Menu Variant` ⇒ **就是收藏窗那颗**） | ❌ 没有 |
| 6 | `Deck/DeckRuntime.cs:1176` | `Tap to edit deck name` | **`MenuDeck/HUD/EditDeckName`**（父链 `Placeholder < Text Area < Deck Name < Window Options < Sidebar < Content Area < Deck Editing Menu`） | ❌ 没有 |

⚠️ P4-#5 我**没有**逐个核对「我们那颗节点 == 原版那颗节点」的坐标：本审查按**用例**核（节点名 + 归属窗口），坐标没核 —— 接之前照铁律 3 补一次。

### P5 🟡 §7.2 的计数错

`MenuDeck/HUD/SearchFilter` 的 11 颗里，`SocialMenu/` / `Draft Mode` 族共 **6 颗**：
1 颗 `Alliances Tab > … > Search Field > Text Area > Placeholder`（`MB_-3110518334810562397`）
+ **5 颗** `Join Alliances Button/Button Text`（`MB_-1855266789001519607` · `MB_-4650365615920678203` · `MB_15448124028997042` · `MB_4073553327063984874` · `MB_465837748097280746`，分属 `Alliance Event Score Panel` / `Draft Mode Deck Info Panel` / `Draft Mode Timed Mode Window` / `Alliance Scoring Panel`）。
作者写「其中 3 颗」并列了 4 个。**不影响结论**（我们侧确实只有 2 颗：`Shell/AlliancesTab.cs:430` · `Shell/AllianceEventScorePanel.cs:144`，本审查已核），只影响「剩余面」的规模描述。

### P6 ⚪ 文件名笔误

§2 表格下方写 `Shell/AllianceEventsScorePanel.cs:144`，实际文件是 **`Shell/AllianceEventScorePanel.cs`**（§8·6 那个写对了）。全仓无 `AllianceEventsScorePanel.cs`。

### P7 ⚪ 行号笔误

§6·E 说 `RuleEngineTest.cs:2093` 的断言**消息**里写着「计策」。实读：那句在 **`:2104`**（`CheckTrue(realTac != null, "卡池里挑得到一张真的「计策」（subtype=Stratagem）")`）；`:2098` 是**注释**（「必须用卡池里真的计策卡」）、`:2127` 是另一条消息「**对手打计策 → 触发**」。⇒ 现象对、行号错，且**是 3 处不是 1 处**。

### P8 ⚪ 文件指错

§8·6.2 说 `Editor/CollectionScene.cs:1960` 有那句过期注释。实读：那句在 **`Shell/CollectionWindow.cs:1960`**（`// 字：… 空时是占位符 "Search"`）；`Editor/CollectionScene.cs:1960` 是 A329 的容差注释，与占位符无关。（§7.3 写对了。）

### 🟢 顺手发现里**核过、成立**的（免得下一轮重查）

- §7.1 ✅ `MenuDeck/MenuButtons/CreateDeck` ≠ 活动窗那颗 —— 两条键、两棵树、两串英文（`Create Deck` 大写 D / `Create deck` 小写 d），**实读确认**。
- §7.4 ✅ `MenuDeck/Filters/Army` 的实例**跨 5 棵子树**（`Alternate Art Tab` / `Decks Tab` / `Cardback Tab > Cardback Display` / `Cosmetic Display/Deck Editing Menu` / `CardsTab` / `Card Display`）—— 6 颗同一条 `mTerm`，一处改六处齐 ✓。
- §6·C ✅ **完全成立**：全库 `mTerm == "MenuDeck/MenuButtons/Select"` **只 1 颗**（`MB_1813973329225681320`），父链 `Button Text < Select Deck < Buttons < Deck info Popup`，TMP `'Seleccionar'`（**西语，两个 c**）；三颗 `Select Avatar Button`（`Select Avatar Button.json` / `…_-52393199903933902.json` / `…_8225512474556734329.json`）的子树里**一个 `Localize` 都没有**、TMP 全是 `'Selecionar'`（**葡语，一个 c**）。⇒ 「两颗被混成一颗」这条现核**属实**，`MainMenuScene.cs:3355` 保持 `"Select"` 是**对的**（判据为空）。
- §6·D ✅ `DeckRuntime.cs:1876` 区那条注释「Deck info 的动作钮（🔴 已删，见下）」仍在，Import 钮未加回来 ✓。
- §2 未动项 ✅ `LiveOpsEventWindow.cs:409` 的 `"Warlord"` 是**节点名**（`MenuDraw.Rect(...)` 的第 4 实参），不是渲染文案 ⇒ 不接词条是对的。

---

## 5. 没查清 / 存疑

### S1 ⚪ 断言对 `Loc` 表的**值**是同义反复（本批没堵）

本批所有新断言都是 `Check(渲染出来的字, Loc.T(同一把键))` —— 两边都过 `Loc.T`。
⇒ 改坏 `Loc.cs` 里那条**中文值**（例如把 `MenuDeck/Filters/Army` 的「军队」改成「兵力」）**全部断言照绿**。
本批**没有任何一条**把中文值钉到来源（`zh_CN.csv:11`）。
这是词条化断言的固有形状（不是回归），但既然本批专门写了「防两边一起退化」的 `HasEntry`，**值得再补一条抽查「Loc 值 == CSV 列」**，否则「军队」是否忠于原版判据**没有任何自动守卫**。
（`RuleEngine` 侧没有等价物可借，本审查也未找到既有先例。）

### S2 ⚪ 「两边一起改回去」的口子仍在 `Loc.T` 自身

所有断言都经 `Loc.T`，而 `Loc.HasEntry` 只挡「键不在表里」；`Loc.T` 若退化成**恒返回键名**，则「渲染值 == 期望值 == 键名」两边同时退化 ⇒ **全绿**、`HasEntry` 也照绿。
本批未堵这条（`DeckScene.cs:2033` 那种「不经过 helper 的直比」只挡 `TitleTerm` 映射错，挡不了 `Loc.T` 本身坏）。

### 我没能核到的（如实记，别当「没有」）

- **`Battle/Tips/EnergyCost` 的中文列该取什么**：`zh_CN.csv:98` 给的是「能量费用」，但**那条 CSV 是按英文源串索引的**，它到底对应 `Battle/Tips/EnergyCost` 还是别的 `Energy Cost`，我**没查清**（要拿到远端 I2 表才能钉）。⇒ 接这条键时**先确认中文取哪一列**，别直接照 `:98` 抄。
- **P4-#5（`Back`）我们那颗节点与原版那颗是不是同一颗**：只按用例核了（节点名 `Button Text` + 归属 `Collection Menu Variant`），**坐标没核**。
- **`AlliancesTab.cs:430` / `AllianceEventScorePanel.cs:144` 那两颗**：只核了「原版同键、我们仍印英文」，**没核**「我们这两颗分别对应原版哪一颗」（5 颗 `Join Alliances Button` 与 2 颗我们侧节点的配对没做）。

### 本次审查搜过的包 / 词（供复现）

- **包**：`d:/2/新解包资源/assets_full/` 下**全部 80 个 bundle 目录**（`Localize`/TMP 判定全覆盖）；
  `Localize` 全表只在 `bundle_menus_assets_all`（901 实例 / 309 条 term）建，另在
  `bundle_battlesharedresources_assets_all` · `bundle_generalgamewindows_assets_all` ·
  `bundle_battleprefabs_vfxandmisc_assets_all` · 13 个 `bundle_scenes_scenes_battlearena*` ·
  `bundle_scenes_scenes_mainmenuwarpforge` 做过 **`grep -rl "8610481073976370760"`**（`Localize` 脚本 pid）确认落点。
- **词/串**：`m_Script == 8610481073976370760`（`Localize`）· `7477354737935883349`（TMP）·
  `mTerm` 逐条：`MenuDeck/Filters/{ClearFilters,Army,Type,Filters}` · `MenuDeck/HUD/{SearchFilter,Rarity,EditDeckName}` ·
  `MenuDeck/MenuButtons/{CreateDeck,ImportDeck,Select}` · `MenuDeck/Button/Random` · `MenuDeck/Tip/SelectDeckAgainst` ·
  `MainMenu/Ranked/GoToCreateDeck` · `MainMenu/General/{Confirm,Select}` · `MainMenu/MainButtons/ButtonLabel/Back` ·
  `MenuCollection/{NoCardsFound,NoCardbackFound,NoDecksFound,Label/Cosmetics}` · `Battle/Tips/EnergyCost`；
  `m_text` 逐串：`Energy Cost` · `Clear filters` · `Army` · `Search` · `Create Deck` · `Import Deck` · `Random` ·
  `Rarity` · `Type` · `Filters` · `Confirm` · `Select` · `Back` · `Your cosmetics collection` ·
  `Tap to edit deck name` · `There are no {cards,cardbacks,decks} in your collection…`。
- **文件**：`数据/本地化/i18n/zh_CN.csv`（6,425 行，`\r\n`=6370 / `\n`=6424 ⇒ **混行尾**，改它要当心）·
  `数据/游戏数据/card_stats.json`（1,213 条）· `d:/2/Warpforge部队卡片/Tau/4计策/`（含逐张开图）。
