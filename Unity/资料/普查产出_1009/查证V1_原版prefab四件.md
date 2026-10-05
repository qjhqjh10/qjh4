# 查证 V1 —— A224② · A178 · A183 · A264

> 一句话：**4 件全查清** —— A224② 原版**不裁建**（全量 instantiate）· A178 item prefab = `Practice Army Select Button` · A183 **同一颗** `UI Collider`（x 比应为 0.90443）· A264 **只有收藏窗**有 `Shared`。
> 只读查证：⛔ 未改任何 `.cs` / 正本 / 工具 / `d:/2/`。尺子 = `工具/menu_dump.py` + `d:/2/tools/decomp_full/`（第一权威）+ 原始 MB 字段现读。

---

## 一、A224② —— 筛选抽屉有 `ScrollRect`+`Viewport`，但子件**全量建**；原版【不裁建】

- **结论（一句话）**：🔴 **原版不裁建** —— 每个 option 都 `Instantiate` 一个 toggle，代码里**没有任何可见性判据**；裁切只发生在**渲染层**。
  ⇒ 我们 `Deck/DeckRuntime.cs:2917` 那句「滚出面板的不建」**与原版不符**（铁律 11 ⇒ **要做**）。

- **证据（资产 · 第一层）**：`menu_dump.py bundle_menus_assets_all "Deck Editing Menu" --depth 18 --md`
  · 抽屉 = `Card Display > ···3 Card Filters`（`CardCollectionFilterController`，字段 `animationTime,clearFiltersButton,filters,hiddenPosition`）
  · `····4 Scroll View` = `Image,ScrollRect` —— **`h=0 v=1`** · `mode=1` · `inertia=1` ⇒ **原版这里确实有 ScrollRect**
  · `·····5 Viewport` = `Image,Mask`（**不是 `RectMask2D`**）· **`showGraphic=0`**
  · `·····6 Filters` = `VerticalLayoutGroup + ContentSizeFitter + LayoutGroupContentFixer`，模板高 **839.02**（视口 **924.06**）
  · `······7` 七行 = `Name FIlter` / `Owned Toggle` / `Upgradable Toggle` / `Army Filter`(`CardArmyFilter`) / `Rarity FIlter`(`CardRarityFilter`) / `Cost Filter`(`CardCostFilter`) / `Type Filter`(`CardTypeFilter`)

- **证据（反编译 · 第一权威）** `d:/2/tools/decomp_full/`：
  · `CardRarityFilter__FillToggleList.c` —— 枚举 `get_options()` 全表 → 每个 `UnityEngine_Object__Instantiate<object>` → `CollectionFilterToggle__Initialize` → 加进列表，**循环体里没有任何视口 / 滚动位置 / 可见性判据**（唯一那个 bool 是 option 自己的标志位 `+0x120`）。
  · `CardTypeFilter__FillToggleList.c` —— **与上面逐句同形**（同一个泛型的两次实例化）。
  · `CardArmyFilter__FillToggleList.c` —— 是**基类泛型 thunk**（`FUN_1813642a0`），本地只有桩；但上面两份就是它那份实现的实例化 ⇒ Army 同形。
  · `CollectionFilterToggle__Initialize.c` —— 只 `set_sprite` + 赋值，无显隐判据。
  · ⇒ 判据 = **全套 option 一次建完**；**不是** `RecyclableScrollRect` 那种「只建可见的」。
    （同包的 `Cosmetic Display > Scroll View` 才是 `RecyclableScrollRect` —— 那是**卡背列表**，⛔ **不是筛选格**，别混。）

- **⇒ 该怎么做**：
  1. 删掉 `Deck/DeckRuntime.cs:2917` 的 `if (b.y2 < FltY || b.y1 > FltY + FltH) continue; // 滚出面板的不建` ⇒ 七行**恒定全建**、数量不随滚动变。
  2. **同时补裁切**：我们的格子是 `ImageQuad.Create(FltParent, tex, …)`，**一个 clip 都没传**（`DeckRuntime.cs` 全文件 0 处 `RectMask2D` / `clip` / `MenuDraw.Visible`）⇒ 压在带口（`FltY=156` / `FltY+FltH=1080.1`）的格子会**越界画出去**（会压进 `Header`），原版 `Mask` 把它裁掉。
     ⚠️ 这一条是**代码判据**，我**没有实跑截图**量溢出（见 §五·2）。
  3. **滚动不用新做**：已有 `HandleScroll`(`:2720-2724`) → `_fltScroll` → `RefreshFilters`；上界 `FilterPanelModel.ContentHFor(state) - FltH` 也在。⇒ 本条只改「建多少」+「裁不裁」。

---

## 二、A178 —— 练习窗那颗 82×82 Army 格的 item prefab = **`Practice Army Select Button`**

- **结论（一句话）**：**是另一颗 prefab**（类 `PracticeArmySelectionButton` + `EverguildButton`），层清单与 A118 那颗**完全不同**；我们现在只建了 `Icon` 一层。

- **判据链（可复查）**：
  · `MonoBehaviour_-5721221448436461764.json`（`PracticeModePopup`）字段 `armySelectionButton` → `{m_FileID:0, m_PathID:-6931238765327743977}`
  · `MonoBehaviour_-6931238765327743977.json` = 类 **`PracticeArmySelectionButton`**（`m_Script` → `mono_index()` 解名），字段 `armyIcon` / `highlight` / `highlightPlayerDeckObject`；其 GO = **`Practice Army Select Button`**（pid `-16359150157888489`，RT `8940217906067473431`）
  · 父链：`Practice Mode Menu > Deck Selector > Army Selector > Viewport > Filters`，而 `Filters` 在原版 prefab 里 **children = 0**（格全是运行期 instantiate）。

- **子件清单（`menu_dump … --rt 8940217906067473431 --root-size 82x82`，格已是 82²，坐标相对格左上角）**：

| 子件 | rect（82² 格内） | 尺寸 | 图（名 · 原尺寸） |
|---|---|---|---|
| `Highlight` | −6.83,−6.83 → 88.83,88.83（**四周各出 6.83**） | 95.65×95.65 | `UI_Deck_button_click` 169×169 |
| `Background` | 0,0 → 82,82（拉伸锚 + `sd(0,0)`） | 82×82 | `UI_Button_Round_background` 237×237 |
| `Icon` | 0,0 → 82,82（拉伸锚 + `sd(0,0)`） | 82×82 | **无图**（运行期由 `armyIcon` 喂） |
| `Has Player Deck` | 54.00,45.00 → 81.00,102.25 | 27.00×57.24 | `Purity Seal_02` 128×256 |

  根件组件：`EverguildButton`（`trans=1` · `interactable=1`）+ `PracticeArmySelectionButton`。

- **与 A118 那颗的对照（证明「不是同一颗」）**：A118 查过的是 **`Ranked Army Selector Container V2`**（168²，层 = `Background > On` / `ProgressBar > Fill Area > Fill + Separator` / `Army Icon` / `Featured Icon`，类 `RankedArmySelectorContainer`）——**两族层清单零重叠**。⇒ A178 当初的怀疑成立，⛔ **别照 A118 的口径改练习窗**。

- **⇒ 该怎么做**：`Shell/PracticeModePopup.cs:765-797` `RebuildArmyCells` 现在每格只建 `Icon`（+ `Hit`）
  ⇒ 按上表补 **`Highlight` / `Background` / `Has Player Deck`** 三层，节点名与层级照原版
  （⚠️ `Highlight` 比格大 6.83/边，**会伸出格框** —— 原版如此，别内缩；`Has Player Deck` 是右下角那颗火漆印，显示条件未查，见 §五·4）。

---

## 三、A183 —— 原版那扇窗用的是**同一颗** `UI Collider` ⇒ **x 比应为 0.90443**

- **结论（一句话）**：**同一颗**（与 A156 核过的战斗侧那颗**逐字段、逐位相同**）⇒ 我们现在 x/y 双轴同用 `0.8679` **x 那一路是错的**。

- **证据（原始序列化字段 · 现读）**：`bundle_menus_assets_all` 的 `Booster Pack Open Window` 名下共 **5 颗** `UI Collider`（五张卡各一颗），**5 颗逐字段相同**：
  · `m_AnchorMin (0,0)` · `m_AnchorMax (1,1)`（**拉伸锚**）· `m_Pivot (0.5,0.5)`
  · `m_AnchoredPosition **(0, −0.02)**` · `m_SizeDelta **(−0.2, −0.44)**`
  · 父件 = **`2DCard`**，`m_SizeDelta = (2.0927, 3.3313)`
  （RT pid 例：`-8719435385506612189` / `6429976486133054499` / `876504314555284515` / `-2166550367152489437` / `1600754383137618979`）
- **复算**（与 `Core/CardFan.cs:107-108` 同一算式）：
  · x = (2.0927 − 0.2) / 2.0927 = **0.90443** · y = (3.3313 − 0.44) / 3.3313 = **0.86792**
  ⇒ **x 比也是 0.90443**，不是 `0.8679`。

- **⇒ 该怎么做**：`Shell/BoosterPackOpenWindow.cs:463-466` —— 把 `const float HitRatio = 0.8679f` 拆成
  `HitRatioX = 0.90443f` / `HitRatioY = 0.86792f`（照 `Core/CardFan.cs:107-108` 的写法），
  **并删掉那句「取**较小的那个**做统一比例，宁可小一圈也别伸出卡外」** —— 那是**我们自己挑的**，不是原版。
  · 影响量级：卡体宽 523.175 时，x 两沿各**多内缩 ~9.6px**（0.90443 → 0.8679）。
- ⚠️ **同源偏置**：`m_AnchoredPosition(0,−0.02)` 与 `hitCy` 那 5px 同源（A156 换算：0.02 × 250 = **5px** 在卡体 523.175px 那一档）——
  **本窗卡体比战斗侧小得多，这 5px 不能照抄**（见 §五·1）。
- ⚠️ 别把 depth-2 那颗 `Collider`（`NonDrawingGraphic`，3853×2232，全窗吸收层）当成卡体命中区 —— **是两件东西**。

---

## 四、A264 —— `Shared` 下**只有 `Close Button`**；**只有收藏窗有这一层**

- **结论（一句话）**：① `Shared` **只挂着 `Close Button` 一个子件**（`Close Button` 下才是 `Button Text`）
  ② **不是「四扇窗都该有」** —— 全库**只有收藏窗**有；其余三窗 + 卡组编辑窗**都没有**。

- **证据 ①（子树，逐行现读）**：`menu_dump --rt 5226810256338313941`（该 RT 的 `m_Children` 只有 1 个 pid）
  · `Shared` 167.17,70.94 → 1920.01,1080.00（1752.83×1009.06，**与 `Content Area` 同矩形**）
  · `Close Button` 192.17,83.44 → 342.17,143.44（**150×60**，`UI_Button_Mulligan`，`EverguildButton`）
  · `Button Text` 200.50,89.26 → 333.36,137.50（**132.86×48.24**）· `'Back'` 字号 40 · `auto[10~40]` · Center/Capline · `AspectRatioFitter`

- **证据 ②（全库普查）**：
  · `ls -d */GameObject/Shared*.json`（`d:/2/新解包资源/assets_full`，28 个**有 GameObject 导出**的包）⇒ **命中 1 个**：`bundle_menus_assets_all/GameObject/Shared.json`
  · `bundle_menus_assets_all` 的 **616 个 prefab 根**（`m_Father=0` 的 RT）逐棵遍历「有没有叫 `Shared` 的节点」⇒ **只有 `Collection Menu Variant`**
  · 点名复查（同包，逐根走树）：`Collection Menu Variant` **✅ 有** ｜ `Rewards Base Submenu Variant` **❌** ｜ `Shop Menu Variant` **❌** ｜ `Social Submenu Variant` **❌** ｜ `Deck Editing Menu` **❌**

- **⚠️ 口径更正（供调度台落盘，我未改正本）**：Y 报告 §七·① 写「原版是 `Content Area` 的**兄弟** `Shared`」——
  **实测不是兄弟，是 `Tabs` 的子件**。逐级读 `m_Father`：
  `Collection Menu Variant` → `Content Area` → **`Tabs`** → **`Shared`** → `Close Button` → `Button Text`（`Shared` 是 `Tabs` 的直接子件）。

- **⇒ 该怎么做**：**只在收藏窗补 `Shared` 这一层壳**（把 `Close Button` 连它下面的 `Button Text` 一起挪进去）；
  **其余窗不动**（原版本来就没有）。补一条断言：按原版路径 `…/Tabs/Shared/Close Button/Button Text` 取得到。

---

## 五、没查清的（差什么）

1. **A183 的 `−0.02` 卡单位中心偏置在本窗折算成几 px** —— 要先定本窗卡的渲染尺度，而它是 `Card2DController.cardScales` / `bigSizeMultiplier` 在**运行期**喂的（`CardInBoosterPack UI n > Content > CardUI > 2DCard` 那段 prefab 里只有 2.09×3.33 的设计值）。**静态读不出，没查清。**
2. **A224② 的「压在带口的格子会越界」** —— 判据是「`DeckRuntime.cs` 里没有任何裁切路径」（代码级），**没有实跑 / 没截图**量溢出量。要坐实得加一次 Play 或临时探针。
3. **`CardArmyFilter__FillToggleList` 的方法体** —— 本地 `decomp_full` 里只有基类泛型 thunk 桩（`FUN_1813642a0`）。「Army 行也全量建」是**靠 Rarity/Type 那两份同形实例化 + 组件字段（`options,togglePrefab`）自洽**推出来的，**不是直接读到 Army 那一段**。
4. ~~**A178 的 `Has Player Deck` 什么时候显示** —— `highlightPlayerDeckObject` 的赋值点在反编译里没找（`PracticeArmySelectionButton` 只有 `.ctor` 那一支有文件）。**没查。**~~
   🔴 **2026-10-11 订正（铁律 5）：这一条「没查」是错的 —— 查得到，而且当天就落地了**（W6 写手在 A178 现场读出来的）。
   · **错因**：只去 `PracticeArmySelectionButton` **自己身上**找赋值点；**真身在 `PracticeModePopup` 上**。
   · **实际判据（`d:/2/tools/decomp_full/`）**：
     · `PracticeModePopup__ConfigureArmyFilterButtons.c` 尾段 ⇒ **出厂 `highlight` / `highlightPlayerDeckObject` 两颗都是 `SetActive(0)`**；
     · `PracticeModePopup__ChangeSelectedArmy.c` 循环 ⇒ **`Highlight` 亮 ⟺ 这一格就是当前选中的阵营**；**`Has Player Deck` 亮 ⟺ 有选中卡组、且它的阵营 == 这一格的阵营**（**与「哪一格被选中」无关**）；
     · `PracticeModePopup__Open.c` 尾 ⇒ 开窗必调 `ChangeSelectedArmy(有卡组 ? 卡组阵营 : 10)` ⇒ **恒有一格亮**。
   · **落地**：`Shell/PracticeModePopup.cs`（四层 + 两处 `SetActive`）· 断言在 `Editor/MainMenuScene.cs:746-935`。
   · 📌 **教训（同族第二条）**：**「在 A 类里没找到」≠「没查」—— 赋值点常常在调用它的那个类里**；报「没查到」之前先 `grep` **谁在写这个字段**。
   · 出处 = `资料/普查产出_1010/W6_A178_A284_A222.md` §②/§⑤·1。

---

## 六、顺手发现的（⛔ 一个都没改）

1. **同一个包里两种裁切机制并存**：卡组编辑筛选抽屉 `Card Filters > Scroll View > Viewport` 用 **`Image,Mask`（模板/stencil）**；而练习窗 `Army Selector > Viewport` 与收藏窗 `Cardback Display/…/Viewport` 用 **`RectMask2D`**。二者都裁，但机制不同 —— 抄判据时**先看是哪一种**。
2. 卡组编辑 `Card Filters > Scroll View > Viewport` 的 `Image` **`showGraphic=0`** ⇒ 那层 `UIMask` 图**不画**。我们建的那层若带可见底板，会与原版差一档。
3. `Practice Mode Menu > Army Selector > Viewport > Filters` 在原版 prefab 里 **children = 0**（格全靠运行期 instantiate）—— 我们照原版建的「GridView + 运行期格」**一致**，不是缺口。
4. 卡组编辑 `Army Filter > Content`（`GridLayoutGroup`）在 prefab 里只存 **1 颗 0×0 的模板 `Toggle`**，而 `CardArmyFilter` 的字段是 `options,togglePrefab` ⇒ 与「运行期按 option 全量 instantiate」自洽（旁证，非主证）。
5. `bundle_menus_assets_all` 共 **616 个 prefab 根**（`m_Father=0` 的 RT）—— 这个数是「全包逐根普查」那条路的规模参考。
6. 顺带给 A265② 补一枚判据：收藏窗 `Shared > Close Button > Button Text` = `200.50,89.26 → 333.36,137.50`（**132.86×48.24**）· `m_SizeDelta (−6,0)` · 带 `AspectRatioFitter`。
   🔴 **2026-10-11 订正（铁律 5）**：本行**后半句原写「即原版那个内缩是 `ARF` 算出来的」—— 不成立**，别照它用。
   · **实据**（`资料/普查产出_1009/写手WB_Shell窗口三件.md` §1·2，现读）：那颗 `AspectRatioFitter`（`m_AspectMode=1` 宽控高 · `m_AspectRatio=3.8386404514312744`）**`m_Enabled = 0`** ⇒ `AspectRatioFitter.UpdateRect` 头一句 `if (!IsActive()) return;` **一个字段都不写**（这也是 `menu_dump` 没在那一行印 `⚙ARF` 标记的原因）。
   · **旁证（算术反证）**：ARF 若真在跑，高会是 `132.86 ÷ 3.83864 = 34.61` —— **不是 48.24**。
   ⇒ 真值 = **锚点 + `m_SizeDelta(-6,0)` 在 150×60 上逐位算出**的 132.86 / 48.24（序列化的，不是算出来的）。
   📌 **措辞教训**：`带 AspectRatioFitter` ≠ `ARF 在生效` —— **要读 `m_Enabled`**（本项目同一形状的坑另见 A265 行）。
