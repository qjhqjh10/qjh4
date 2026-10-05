# 批次 1 · 己 —— Collection 六件（A233 · A180 · A248 · A249 · A258 · A198③）

> 写手报告 · 2026-10-11 · 工作目录 `d:/4/Unity`
> 白名单内 6 个文件：`Editor/CollectionScene.cs` · `Shell/{CollectionWindow,DeckInfoPopup,DeckSelectionPopup,PracticeModePopup}.cs` ·
> `Core/FilterPanelModel.cs`（**`Shell/MenuDraw.cs` 一行没动** —— 它不在白名单）。**没碰**别的任何文件。
> ⚠️ 本件**没跑 Unity**（自检归同步点）；跑了秒级类型检查三遍，**运行时 0 / 编辑器 0**。

---

## 一、结论（六件各一句）

| 件 | 结论 | 一句话 |
|---|---|---|
| **A233** | ✅ **做了**（调度台裁「不开口」） | `Shell/PracticeModePopup.cs` 的 `Txt` **删掉** `if (clip.HasValue)` 那道冗余闸（前置条件归被调方 `MenuDraw.ClipText` 的首句）—— **行为零变化**、⛔ 同文件 `ImgTex` 那道闸**没动**（`ApplySoftEdges` 要非空 clip）；⛔ **没给生产类开测试注入口**，牙口 = `Editor/ShellScene.cs` 既有的 ⑤·d-2 / ⑤·d-3（那两条批里在**别人的白名单**，本件没碰）。 |
| **A180** | ✅ **做了**，并且**顺手核掉了判据自标的「未复核」**（结论：邻居那颗**同一套模型**） | 关窗钮命中区按原版**外扩 20/边**（底取**子件**矩形 `1791.573,71.18→1848.43,129.30`，不是按钮矩形）⇒ `1771.57,51.18→1868.43,149.30`（≈原版 `1771.0,51.2→1867.8,149.3`，差 0.573px）；**连带把「原版靠深度定胜负」这件事显式化**（同队列比 z），否则外扩出来的 12×19px 重叠带里谁赢**看运气**。 |
| **A248** | ✅ **做了**（按页分字号） | 异画页那一套 = **输入 35/auto[10~35] · 开关 36/auto[10~36] · 四行 `Title` 36**（原版现读，见 §二）；落点 = `FilterPanelModel` 新增 5 个**专用常量** + `Build`/`BuildTitles` 各加**可选形参**（缺省 = 卡牌页），`CollectionWindow` 侧由 `FilterPanel.Filters` 那个新 `Styles` 标志**一路传下去**；⛔ **共用常量一个字没动**（A247 的先例）。 |
| **A249** | ✅ **做了** | 收藏窗搜索框 `折行` 从 **1 → 3**（`PreserveWhitespaceNoWrap`）—— 为此把 `TextAligned` 的第 10 个形参**从 `bool` 换成原版档位原文 `int`**（布尔表达不了 `3`，`Label.SetWrappingMode` 头明写「别用 `false` 顶替」）。 |
| **A258** | ⚠️ **只做得到 1/3**（**白名单外**，如实报） | `CollectionWindow.TextAligned` 的折行形参**已去掉缺省值**（A258 的裁定）+ 升级成原版档位原文；**另两处（`SocialWindow.Text` / `ProfilePage.Text`）做不了** —— 删缺省值会让 `AlliancesTab.cs`(11 处) / `FriendsTab.cs`(3 处) / `AllianceMemberTab.cs`(9 处) **编不过**，而它们在白名单外（详见 §四·1）。 |
| **A198③** | ✅ **两处调用点都接上了** | `CollectionWindow.BuildDeckCell` 与 `DeckSelectionPopup.RebuildCells` 改走 `DeckCell(GameWindow, …)`；**两处的 `win.Clip` 都 = 原来那个 `clip` 实参**（前者本来就 `Clip = DeckViewport`；后者**此前从不设 `Clip`** ⇒ 补了「临时设 `SvRect` → 循环 → 还原」那两行），`ClipPad` 两处都没人设过（= 0）= 旧写法 `maskPad` 缺省 ⇒ **零行为变化**。 |

**新增/改写的断言：+26 条（旧的 6 条一条没删）**：A180 **7**（四边 + 宽 + 高 + 深度对照，另 1 条前提）· A198③ **4**（宽/高反向对照 2 + 压边格交集 1 + 前提 1）· A248/A249 **15**（异画页 6 + 标题 3 + 计数 1 + 卡牌页对照 5）。

---

## 二、证据（`文件:行号` + 原值）

### 2·1 A233 —— 闸确实是同义反复（判据复读）
- `Shell/MenuDraw.cs` 的 `ClipText` **首句** = `if (lb == null || !clip.HasValue) return false;`（`ClipText` 的签名下面第一行）
  ⇒ `Txt` 里那道 `if (clip.HasValue)` 是**同义反复、行为上不可观测**（调度台裁定的原文见 `资料/普查产出_1010/调度台_口径裁定_1011.md` §A233）。
- 牙口（既有、批里在 `Editor/ShellScene.cs`，**不是我的白名单**）：⑤·d-2 `ApplySoftEdges(…, Vector2.zero)` 硬裁 / ⑤·d-3 `ClipText(…, Vector2.zero)` 硬裁。

### 2·2 A180 —— 原版射线面（**自己重读了一遍，不是转抄**）
读法：`python 工具/_probe_deckinfo.py bundle_menus_assets_all "Switch Deck Info Button" --class Image --no-sprite`
+ `menu_rect.Bundle` 走 prefab 树（临时脚本 `/tmp/wf_sib2.py`，未落库）。

| 节点 | 自己的 `Image` | 子件 `Background` / `Icon` |
|---|---|---|
| `Generic Close Button Orange` | `m_RaycastTarget = 0` · pad 全 0 | **`= 1` · `m_RaycastPadding = (−20,−20,−20,−20)`** |
| **`Switch Deck Info Button`（⚠️ 判据自标「未复核」的那颗）** | **`m_RaycastTarget = 0`** · pad 全 0 | **`= 1` · `(−20,−20,−20,−20)`** |

⇒ **邻居与关窗钮是同一套模型**（⇒ 那条重叠带里**两颗都吃得到**，胜负只能看深度）。

**深度已坐实**（窗根 `Deck info Popup` 的 `m_Children` 顺序，实读）：
`Menu Dark Background(0) → Generic Window Red Background Big(1) → Warlord Image(2) → Deck Details(3)
 → Buttons(4) → **Deck Options(5)** → Info Panel(6) → **Generic Close Button Orange(7)**`
⇒ **关窗钮是最后一个子件 = 最深** ⇒ 那条带里点下去**应当关窗**。✅ 判据自标的那条「未复核」**已复核**。

几何（`menu_rect.walk` 实读，屏心系）：子件 `Background`/`Icon` = `831.0,−468.8 → 887.8,−410.7`
⇒ 屏系 `1791.0,71.2 → 1847.8,129.3`（56.864 × 58.128）⇒ 外扩 20 = **`1771.0,51.2 → 1867.8,149.3`（96.8 × 98.1）**。
我们那两层画在 `CloseL + (CloseW − OptIconW)/2 = 1791.573` ⇒ 复算 `1771.573,51.18 → 1868.433,149.30`，与原版差 **0.573px**。

### 2·3 A248 / A249 —— 异画页那一套（现读，可复跑）
命令：`python 工具/menu_dump.py bundle_menus_assets_all "Collection Menu Variant" --depth 22 --no-sprite`

| 节点（`Alternate Art Tab/Collection Display/Card Filters/…`） | 原版 | 我们（改前） |
|---|---|---|
| `Name FIlter/…/Text Area/Placeholder` | `字号 35.0 auto[10.0~35.0] 折行=0` | `InputFontPx` 30 · `auto[18~30]` |
| `Name FIlter/…/Text Area/Text` | `字号 35.0 auto[10.0~35.0] **折行=3**` | 同上（折行落成 **1**） |
| `Owned Toggle/Label`（`'Owned only'`）/ `Upgradable only/Label` | `字号 36.0 auto[10.0~36.0] 折行=0` | `ToggleFontPx` 32 · `auto[18~32]` |
| 四个 `Title`（Army/Rarity/Energy Cost/Type） | `字号 36.0`（**无 `auto[…]`**）· `折行=1` | `TitleFontPx` 32 |
| 对照：**卡牌页**同一棵树（`CardsTab/Card Filters`） | `30 · auto[18~30]` / `32 · auto[18~32]` / `32` | 本来就对 |
| 对照：稀有度 / 费用 / 类型三族（**两页相同**） | `23.2 auto[10~27]` / `45 auto[25~45]` | 对 ⇒ **不按页分** |
| 对照：卡背页 `Owned Toggle (1)/Label` + 它的 `Title Army` | `32 auto[18~32]` / `32` | 对 ⇒ `BuildCosmetics` **不需要**分页（见 §四·3） |

### 2·4 A198③ —— 两处调用点的四个输入
| 调用点 | 旧的 `clip` | 新旧是否同一个值 | `ClipPad` |
|---|---|---|---|
| `Shell/CollectionWindow.cs` `BuildDeckCell` | `DeckViewport`（显式实参） | ✅ `RebuildDeckCells` 已 `Clip = DeckViewport`（`:2530-2531`，循环后还原） | 全仓无 `ClipPad =` 命中 ⇒ 0 |
| `Shell/DeckSelectionPopup.cs` `RebuildCells` | `SvRect`（显式实参） | ✅ **本件补的**：循环前 `Clip = SvRect`、循环后还原（此前**从不设 `Clip`** ⇒ 照抄会变成 `null`） | 同上 ⇒ 0 |

---

## 三、改动清单

| 文件 | 位置（改后） | 改了什么 |
|---|---|---|
| `Shell/PracticeModePopup.cs` | `Txt` 末段 | **删** `if (clip.HasValue)` 一道闸 + 8 行判据注释（含「`ImgTex` 那半不能删」的理由）；顺手订正 4 行**过期注释**（`GameWindow 没有 Clip` 那句 —— A78② 起已上移，铁律 5） |
| `Core/FilterPanelModel.cs` | `TitleFontPx` 之后 | **新增**「异画页那一套字号」5 个常量（`InputFontPxStyles/InputFontAutoMinStyles/ToggleFontPxStyles/ToggleFontAutoMinStyles/TitleFontPxStyles`）+ 现读判据段 |
| 同上 | `Build(...)` | 末尾加**可选**形参 `float togglePx = ToggleFontPx, float toggleAutoMin = ToggleFontAutoMin`；两个开关标签的 `LabelPx/LabelAutoMin` 改读形参 |
| 同上 | `BuildTitles(...)` | 末尾加**可选**形参 `float titlePx = TitleFontPx`；四行 `Px` 改读形参 |
| `Shell/CollectionWindow.cs` | `FilterPanel` 类 | 新增 `public bool Styles;`（与 `Cosmo` 并列；判据与指针写在注释里） |
| 同上 | `BuildFilterPanel(...)` | 末尾加 `bool styles = false`，写进 `FilterPanel.Styles` |
| 同上 | `_fltStyles = BuildFilterPanel(...)` | **传 `styles: true`**（异画页那一份；卡牌/卡背/卡组三份都不传） |
| 同上 | `RebuildFilterRowsNow` | 取 `bool styles = _flt.Styles;`，传给 `BuildFilterRowModel/BuildNameRow/BuildFilterTitles` |
| 同上 | `BuildFilterRowModel(bool)` / `BuildNameRow(Transform,bool)` / `BuildFilterTitles(Transform,bool)` | 按 `styles` 选三对常量（输入 / 开关 / 标题） |
| 同上 | `BuildNameRow` 的 `TextAligned(...)` | 字号按页取 + 第 10 个实参传 **`SearchBoxWrap`**（新常量 = **3**） |
| 同上 | `TextAligned(...)` | 第 10 个形参 `bool wrap = true` → **必填 `int wrapMode`**（原版 `m_TextWrappingMode` 原文）；`if (!wrap) SetWrapping(false)` → `if (wrapMode != 1) SetWrappingMode(wrapMode)`（`!= 1` ⇒ 既有行为逐位不变） |
| 同上 | `RebuildFilterRowsNow` 的格子调用点 | 第 10 个实参 `c.LabelWrap == 1` → **`c.LabelWrap`** |
| 同上 | `TitleRow(..., float fontPx)` | 字号**从 `Title.Px` 传进来**（此前写死 `32f` ⇒ 模型里的 `Px` **一个读者都没有**，异画页那 36 传了也不生效） |
| 同上 | `FltShadowW` 之后 | 新增 `public const int SearchBoxWrap = 3;`（判据 = 原版 `Text` 那一半；含「⛔ 别用 `SetWrapping(false)` 顶替」） |
| 同上 | `BuildDeckCell` | 改走 `MenuDraw.DeckCell(this, …)`（A198③） |
| `Shell/DeckSelectionPopup.cs` | `RebuildCells` | 循环前 `Clip = SvRect` / 循环后还原 + 改走 `MenuDraw.DeckCell(this, …)`（A198③） |
| `Shell/DeckInfoPopup.cs` | 常量段 | 新增 `public static readonly Vector4 ClosePad = new Vector4(−20,−20,−20,−20)`（判据/复算/邻居实测/⛔ 不许写 `zero` 全写在 `<summary>`）+ `const float HitZFront = 0.01f` |
| 同上 | `Build()` 第 8) 节 | `Hit(r)` → `Hit(PaddedHitRect(子件 rect, ClosePad))` + 命中节点 `localPosition.z −= HitZFront`（把「原版最深」显式化） |
| `Editor/CollectionScene.cs` | 关窗钮那一节 | **改写** ① 那组：四边期望换成原版外扩后的 `1771.0/51.2/1867.8/149.3` + **两条相对断言**（宽 96.86 / 高 98.13）+ 新增 ①-b **深度对照**（关窗钮 z < 邻居 z） |
| 同上 | 选卡组窗那一节 | 新增 A198③ 四条（整格宽/高反向对照 + 压边格命中高 = 格 ∩ 窗 `Clip` + 按数据的「前提」） |
| 同上 | 异画页抽屉那一节（抽屉**开着**时） | 新增 A248/A249 十五条：异画页 6（输入 35/10 + 折行 3 · 开关 36/10/折行 0）+ 三个 `Title` 36 + 「≥3 行真建出来」 + **卡牌页对照组 5**（30/18/折行 3 · 32/18 · 32） |

**行尾**：6 个文件改前改后**都是纯 LF**（二进制数过：`CRLF=0`，`loneLF = 总行数`）⇒ 没有翻行尾。
`git diff --numstat`（vs HEAD，含前几批未提交的改动，**不是**只有本件）：
`CollectionScene.cs 537/30` · `CollectionWindow.cs 163/57` · `PracticeModePopup.cs 135/9` · `DeckInfoPopup.cs 71/1` · `FilterPanelModel.cs 59/9` · `DeckSelectionPopup.cs 10/2`。

### 3·1 每条新断言「改坏哪里它会红」

| # | 断言 | 怎么改坏就红 |
|---|---|---|
| A180-a | 命中区四边 = `1771.0/51.2/1867.8/149.3` | 把 `ClosePad` 写回 `Vector4.zero` / 把 `PaddedHitRect` 那一跳删掉 ⇒ 四边全红（退回 `1782.81/63.20/1857.19/138.80`） |
| A180-b | **宽 96.86 / 高 98.13** | ① 去掉外扩 ⇒ 74.386/75.605 ⇒ 红；② **把 pad 加在按钮矩形上** ⇒ 114.386/115.605 ⇒ 红（每边多 `(74.386−56.86)/2 ≈ 9px`，正是「看着像对了」那一档） |
| A180-c | 关窗钮命中 z **<** 邻居 z | 删掉 `Build()` 第 8) 节那两句 ⇒ 两个 z 相等 ⇒ 红（并退回「看 `FindObjectsByType` 返回序」的运气判） |
| A198③-a | 整格宽/高 = `225 / 364.5` | `Clip` 取错矩形 / `PaddedClip` 把 pad 也吃上 ⇒ 整格被裁小 ⇒ 红 |
| A198③-b | 压边格命中高 = `格 ∩ 窗 Clip` 的高 | 走回裸重载（`clip` 由调用点自己给）而不设 `Clip` ⇒ 那一格**整格高** ⇒ 红；把 `RebuildCells` 里那两行 `Clip = SvRect` 删掉同理（`win.Clip = null` ⇒ 不裁） |
| A248-a | 异画页输入 `FontSizeMax/Min` = `35 / 10` | 删 `BuildFilterPanel(…, styles: true)` 那个实参 / `_flt.Styles` 忘了往下传 ⇒ 退回 30/18 ⇒ 红 |
| A248-b | 异画页开关 `36 / 10` | 同上（退回 32/18） |
| A248-c ×3 | 异画页三个 `Title` `FontPxNow` = 36 | **把 `TitleRow` 那处改回写死 `32f`（本件之前的原状）⇒ 红**；模型层 `BuildTitles` 忘了传 `titlePx` 同理（模型给的 `Px` 是 32） |
| A249-a | 异画页搜索框 `WrappingMode == 3` | 删 `TextAligned` 里那句 `SetWrappingMode` ⇒ 退回 `SetAutoFitBox` 开的 1 ⇒ 红 |
| A248-d ×5 | **卡牌页对照**：30/18/折行 3 · 32/18 · 32 | 把**共用常量**改成 35/36（=「顺手统一」那一档）⇒ 卡牌页被改歪 ⇒ 5 条红 |
| A248-e | 异画页建出 ≥3 行小标题 | 把 `TitleRow` 的视口求交删掉 / 行高算错 ⇒ 计数不足 ⇒ 红（防「只建出 1~2 行 ⇒ 上面那组等于没验」） |

🔴 **A248 那 6 条与 A248-d 那 5 条是【成对】的**：只把异画页改对、顺手把共用常量也改掉 ⇒ **对照那组红**；只保住共用常量、`styles` 没传下去 ⇒ **异画页那组红**。两组一起看才钉得住「按页分参数」这条裁定。

---

## 四、没查清的部分（⛔ 不猜）

1. 🔴 **A258 的第 2、3 处（`SocialWindow.Text` / `ProfilePage.Text`）本件【做不了】，原因是白名单**：
   删掉缺省值 ⇒ **所有**调用点必须显式给值，而它们的调用点在
   `Shell/AlliancesTab.cs`（11 处缺省处，**真值已有**：见 `资料/普查产出_1008/波C3_A212其余_A213_A214.md` §二·表 A）·
   `Shell/FriendsTab.cs`（3 处，真值见同报告 §二·表 B①）· `Shell/AllianceMemberTab.cs`（9 处，真值见 §二·表 B②）
   —— **这三个文件不在本件白名单**。
   ⇒ **要么**把这三个文件加进白名单再派一件，**要么**把这条拆成两件。**真值全都已经读出来了，缺的只是那三次 Edit 的权限**（⛔ 不是「判不了」）。
   ⚠️ 另：`SocialWindow.Text` 的 **`alignLeft` 缺省也是 `true`**，那是**另一族真偏离**（`AlliancesTab` 15 处里 6 处原版是**居中**、我们左对齐；判据见 C3 §八①）—— 与折行同族、但**不是** A258，**本件一行没动**。
2. **A258 的形参口径被本件改了一档**（请调度台确认）：`CollectionWindow.TextAligned` 的第 10 个形参**从 `bool wrap` 换成了 `int wrapMode`**（原版 `m_TextWrappingMode` 原文）。
   理由：A249 要的 `3` **布尔表达不了**，而 `Battle/Label.cs` 的 `SetWrappingMode` 头明写「⛔ 别用 `false` 顶替」。
   ⚠️ 副作用：**同族另外两处（`SocialWindow.Text` / `ProfilePage.Text`）仍是 `bool wrap`** ⇒ 三处**形参名/类型仍不统一**（A258 的原文只要求「去掉缺省值」，没说改类型）。要统一的话请一并裁。
3. **A248 的 `BuildCosmetics` 那一格【不需要】按页分**（与简报的落点清单不同，如实报）：
   简报把 `BuildCosmetics:518` 列进落点，但**卡背页**（`Cosmetic FIlter`）那颗 `'Owned only'` 原版实测 = **`32 · auto[18~32]`**
   （`menu_dump … "Collection Menu Variant" --depth 22` 的 `Owned Toggle (1)/Label` 那一行），**与我们共用常量本就一致**；
   它的 `Title Army` 也实测 **32**。⇒ 只加了「`Build`/`BuildTitles` 那一族」的可选形参，`BuildCosmetics` **没加**任何形参（A247 那次加的是 `labelAutoMin`，已经够用）。
   ⚠️ 若调度台的手册里那条落点是**照抄旧清单**，请就地订正（铁律 5）。
4. **`NameRowRects` 也没有按页分**：它只吐**三个矩形**，两页的矩形**逐条相同**（`0.3,155.9→335.6,235.0` / `37.3,182.4→268.5,209.4`）⇒ 字号不在它那里，改的是**调用点**（`BuildNameRow`）。
5. ⚠️ **A180 那 5 颗 `Deck Options` 圆钮的同类外扩【本件没做】** —— 见 §五·1（本件的裁定只点关窗钮一颗，我没顺手改别人的账）。
6. ⚠️ **`HitZFront` 是本件新引入的一把尺子**（用 `z` 表达原版的**子件深度**）—— 判据是 `PointerLayer.HitButton` 自己写着的「同队列再比 z、越小越靠前」。
   它**不是**渲染分层（那张 quad 全透明、本来就在本窗最上面）。若调度台认为「本窗该改成逐颗显式队列」或「z 不许这么用」，请裁 —— 本件的写法是可换的，**判据（那条带里该关窗）不变**。

---

## 五、顺手发现（⛔ 只报不改）

1. 🔴 **同一个窗里，5 颗 `Deck Options` 圆钮的命中区也【比原版小 ≈20px/边】**（与 A180 完全同源、同一份实读证据）：
   `Switch Deck Info Button` / `Duplicate Button` / `Share Button` / `Share On Chat` / `Delete Button` **五颗**，
   **自己那颗 `Image` 全是 `m_RaycastTarget = 0`**，吃射线的是**两个子件** `Background`/`Icon`（`= 1`，pad `(−20,−20,−20,−20)`）
   ⇒ 原版每颗的可点区 = **子件 rect 外扩 20 = 96.864 × 98.128**，而我们 `Shell/DeckInfoPopup.cs` 那 5 处 `Hit(…, r, …)`
   用的是**按钮矩形 74.386 × 75.605**。
   ⇒ **真偏离、要做**（铁律 11）。⚠️ 与关窗钮那条的**差别**：5 颗之间步进只有 **93.976** ⇒ 外扩后**彼此重叠 2.888px**
   （原版就这样），谁赢同样靠**兄弟序**（`m_ReverseArrangement=1` ⇒ 树序第 0 的 `Switch Deck Info` 在最右、**最浅**）。
   本件**没做**（不在 A180 的裁定范围内），**证据齐**、改法与 A180 同款（`PaddedHitRect`），**建议单开一件**。
2. 🔴 **`Shell/PracticeModePopup.cs` 一处过期注释已就地订正（铁律 5）**：`HitOn` 的 `<summary>` 里写着「本窗基类 `GameWindow` **没有** `Clip`（那是 `MenuWindowBase` 的）」
   —— `Clip`/`ClipSoftness`/`ClipPad` **2026-10-07（A78②）起已上移到 `GameWindow`**（`Shell/WindowsManager.cs` 的「裁切状态」那节：
   「只声明一次，全 `GameWindow` 族都继承得到」）⇒ 本窗**现在也有**。上面那两句历史叙述照旧成立，只订正了断言那一句。
   （同句在别的文件还有没有第二份：**没查**。）
3. ⚠️ **`MenuDraw.DeckCell` 两个重载的文档注释里还留着「今天两处调用点还没改用它」**（`Shell/MenuDraw.cs` 里那句
   「**上面那两处调用点还没改用它**」）—— 本件落地之后**已经改了** ⇒ 那句过期。
   ⛔ `Shell/MenuDraw.cs` **不在本件白名单**（别人在做）⇒ **没动**，请调度台指给有权限的那一件。
4. ⚠️ **`PointerLayer` 的同队列同 z 打平**是个**全局性**的口子（本件只在`DeckInfoPopup` 一处把它显式化）：
   `AllButtons()` = `FindObjectsByType<WindowButton>(FindObjectsSortMode.None)`（**返回序不保证**），
   而 `HitButton` 只在 `q.RenderQueue > bestQ || (== bestQ && z < bestZ)` 时换人 ⇒ **同队列同 z 时「先到者赢」**，
   而「先到」= 那个不保证的顺序。本件**没查**别的窗有多少处这样打平（**只知道本窗是 9 颗全打平**）。

---

## 六、跑过的检查

- ✅ **秒级类型检查**（三次，`TMPDIR=/tmp/wf_j bash d:/4/Unity/工具/typecheck.sh`）：
  **改完 A233/A249/A248/A198③ 那批 ⇒ 运行时 0 / 编辑器 0** · **加完断言 ⇒ 运行时 0 / 编辑器 0** · **收尾再跑 ⇒ 运行时 0 / 编辑器 0**。
  ⚠️ 三次都**没有**「错在别人文件上」的那种读数 ⇒ 没遇到「编到别人半成品」那一档。
- ⛔ **没跑 Unity**（红线：代理不跑）。⇒ 本件 26 条新断言**一条都没实跑过**，红/绿以同步点那次 `CollectionScene.Run` 为准。
- ⛔ **没跑 `menu_dump` 之外的自检**。跑过的只读工具：`menu_dump.py`（A248/A249 判据复读）·
  `_probe_deckinfo.py`（A180 的 `m_RaycastTarget` 实读）· 临时脚本 `/tmp/wf_sib2.py` + `/tmp/wf_rects2.py`（prefab 树 / 兄弟序，**未落库**）。
- ✅ **行尾**：6 个文件改前改后**纯 LF**（`b.count(b'\r\n') == 0`），全程只用 Edit 工具。

### 首跑重点观察（我无法静态证明的两条）
① **`sel.Cells` 里有没有「整格在视口里」的那一格**（A198③ 反向对照那两条靠它）—— 视口 778.06 高 > 格高 364.5
   ⇒ 起手**必然**有；代码里是**现找**的（⛔ 不拿 `Cells[0]` 顶替 —— 滚动位置一变它就可能压边 ⇒ 假红），
   找不到则当场红并说清「一格都没有 = 前面那套建格就错了」。
② **异画页 `Title Type` 在视口外、本该没有节点**（`1155.9 > 1079.99`）—— 那一条我写成了「找不到就 `Debug.Log` + 跳过」，
   并用「**≥3 行真建出来**」兜住鉴别力（**不是** `CheckTrue(false,…)`：那会把「按视口建」这件**对**的事判成红）。
