# A_界面文案一次收口（执行代理 `A-文案` · 2026-10-19）

> 覆盖三笔账：**`A1498`（收藏窗左栏四键）· `A1501`（练习窗大面积英文）· `A1493④`（主菜单模式卡悬停动态）**。
> 同一个 `Loc` 根因**只修一遍**（新增 7 条词条 + 一个共享的取词口），⛔ 没有三处各修一遍。
> 🔴 全程**没跑 Unity**（红线 1）、**没动 git**、**没碰 `Editor/`**、**没越白名单**。

---

## 结论（一句话）

三笔**都改了**：`A1498` 查出真因（**末键被当母版关掉** + 文案硬编码英文 + `Decks` 应为 `Deck`）、`A1501` 六段字全接上 `Loc.T`、`A1493④` 的**颜色渐变那一半本来就已实现**（逐值核过）、**视差那一半本轮把原版编译字节码反汇编出来解出了算式并落进 UV**；剩两处**必须由主对话收口**（`Editor/` 断言 + 白名单外的同一根因文件），逐条列在下面。

---

## 改动清单（文件:行号 —— 改了什么 —— 判据出处）

### ① `Core/Loc.cs`（LF · +46 行 · 无删改）

| 位置 | 改了什么 | 判据出处 |
|---|---|---|
| `:366-381`（新 **F1** 节） | 新增 `MainMenu/MainButtons/ButtonLabel/Deck` = (卡组, Deck) | 键名 = 原版 `Localize.mTerm` 原文（MB `7815000581621342933`，GO `TabButtonLabel`）；EN = 同节点 TMP `m_text='Deck'`（MB `6937732152595899093`，`m_fontStyle=16`）；中文 = **原版中文客户端实拍** |
| 同上 `:381` | 新增 `MenuShop/AlternateArtStyle` = (风格, Styles) | 键名 MB `-681795326754626859`（GO 与 TMP MB `8988648794445374165` 同一个，`m_text='Styles'`）；中文 = 同一张实拍 |
| `:383-397`（新 **F2** 节） | 新增 `Demo/ToBattle`=(开战！, Battle!) · `Demo/DeckSelectionDemo/SelectDeck`=(选择卡组开始游戏, Select deck to play) · `Demo/CardCostBalance`=(卡牌 / 能量费用, Card / Energy cost) · `MainMenu/RankedWindow/ChangeDeck`=(更换卡组, Change Deck) · `Event_Description/Generic/GameMode`=(游戏模式, Game mode) | 键名与 EN = 本轮把原版 `Practice Mode Menu` 整棵 **129 个节点**逐颗打印 `mTerm`/`m_text` 得到（见下「判据」节）；中文 = `数据/本地化/i18n/zh_CN.csv` 按**精确英文键**命中 |
| — | ⛔ **没有重复添加** `MenuShop/ShopItemType/{Cards,Cosmetics}`（中文 张牌 / 美容品）与 `MainMenu/MainButtons/ButtonLabel/Back`（返回）—— 它们在表里**已有**（D3 那一节 / `MainMenu/` 那一族），原版这几处挂的就是同一条词条 | 见下表「复用」两行 |

**表体检**：`Core/Loc.cs` 的 `Table` 现有 **601 条**，用脚本把键名全抽出来数了一遍 —— **0 个重复键**
（若有重复，`Dictionary` 的集合初始化式会在类型初始化时抛 `ArgumentException`；这是本轮唯一可能「一启动就炸」的风险点，已排除）。

### ② `Shell/MenuWindowBase.cs`（LF · 共用件，只动这三处）

| 位置 | 改了什么 | 判据出处 |
|---|---|---|
| `:99` `:120` | `TabBtnSpec` 新增 `public readonly string Term` + 构造末位可选参 `string term = null`（**源码兼容**：四个窗 + `SocialWindow` 位置实参调用照旧编得过，已用编辑器读数验证） | 为什么存**键**不存译文：`static readonly` 的静态初始化早于任何语言设置（同 `Shell/SocialWindow.cs` 的 `LocalizedButtons()` doc） |
| `:390` `:448` | `BuildShell` / `BuildBar` 各加一个 `bool lastIsMaster = true`（**缺省值 = 逐位保留旧行为**）；顶栏 `tb.tabButtonPrefab = lastIsMaster ? res.roots[last] : null` | **原版逐窗实读**（见下「判据」节·四窗表） |
| `:559` | 屏上的文案改走取词口：`string tabLabel = string.IsNullOrEmpty(spec.Term) ? (spec.Label ?? "") : Loc.T(spec.Term);`（`Term` 空 ⇒ **不调 `Loc.T`**，否则会把 `Label` 当键名查、白刷一条缺键警告） | 原版四窗左栏键的 `TabButtonLabel` 上各挂一颗 I2 `Localize` |

### ③ `Shell/CollectionWindow.cs`（LF · +38/−6）

| 位置 | 改了什么 | 判据出处 |
|---|---|---|
| `:105-116` | `Buttons` 表：**第 1 键 `"Decks"` → `"Deck"`（单数）**；四颗各补 `term:` | TMP `m_text='Deck'`（MB `6937732152595899093`）；四个 `mTerm` 见 F1 节 |
| `:2585-2594` | `BuildShell(..., lastIsMaster: false)`（**本窗四项全是真页签**） | 原版本窗 `TabButtons.tabButtonPrefab` = **null**（见下） |

### ④ `Shell/PracticeModePopup.cs`（LF · +22/−6 · 六处）

`:610` · `:641` · `:804` · `:824` · `:838` · `:848` —— 六处 `Txt(...)` 的第 2 个实参由英文原文改成 `Loc.T(...)`。
**除第 2 个实参外一个字没动**（节点名 / 框 / 字号 / autosize / 折行参数逐位不变）。

### ⑤ `Shell/MainMenuRuntime.cs`（LF · +80/−1 · `A1493④`）

| 位置 | 改了什么 | 判据出处 |
|---|---|---|
| `:1440-1501`（新注释块 + 两个成员） | 新增 `public const float ModeCardParallaxX = 0.2f` 与 `static float ModeCardParallaxDu(float cardCenterPx)`，**判据 / 反证 / 落法 / 验收法全写在上面那段注释里** | 见下「判据」节·视差 |
| `:1525` `:1532` | `BuildModeCard` 第 ① 步：`SetUvRect` 的 `u0` 改成 `u0 + du`（**窗宽一个字没动**） | 同上 |
| `:1655-1662` | 第 ⑥ 步补一段「**色偏那一半不用改**」的查证结论（`m_Colors` 四格 + `m_FadeDuration` 逐值核过） | 见下 |

---

## 判据（每条：资产路径 / 文件:行号 / 实测读数）

### 一、`A1498` 的**真因**：末键被当母版关掉了（**不是清单缺项**）

`Shell/MenuWindowBase.cs` 的 `BuildBar` 原来**无条件**把 `specs` 最后一项当 `TabButtons.tabButtonPrefab` 并
`SetActive(false)`。逐窗实读**原版**那一格（解父链：窗口根 GO → `TabbedWindowComponents.tabButtons` → 那个组件的
`tabButtonPrefab`；路径 = `d:/2/新解包资源/assets_full/bundle_menus_assets_all/`）：

| 窗口 | 根类 / MB | `TabButtons` MB | 原版 `tabButtonPrefab` | 结论 |
|---|---|---|---|---|
| `Rewards Base Submenu Variant` | `MainMenuRewardsWindow` `1349677669050291967` | `638825230631048959` | **非 null**（→ 第 4 个孩子 `Menu Navigation Panel Button`） | 末项**是**母版 ✓ 旧行为对 |
| `Shop Menu Variant` | `ShopWindow` `6184803956894681212` | `764348890710493308` | **非 null**（→ `Shop Icon`） | 末项**是**母版 ✓ 旧行为对 |
| **`Collection Menu Variant`** | `CollectionScreen` `-6401214277658680619` | `1756822263936377557` | **`m_PathID = 0`（null）** | 末项**不是**母版 ⇒ **旧行为错** |
| **`Social Submenu Variant`** | `SocialMenuWindow` `4523486740489232547` | `-5311503351116210013` | **`0`（null）** | 末项**不是**母版 ⇒ **旧行为错** |

`Tab Buttons` 四个孩子（`menu_dump.py … "Collection Menu Variant" --depth 5` 实读）：
`Deck` · `Cards` · `CardBacks` · `Alternate Art` —— 四颗**全是 `EverguildToggle`**，
**没有**奖励窗那种 `Menu Navigation Panel Button` 母版（第 5 个孩子 `Shadow` 不是键，我们走 `BarShadowW` 单独建）。
`CollectionScreen` 的 `tabs[]` 也正好 4 项。

⇒ **错因**：这一句是照奖励窗那一份写的（当时只有奖励窗一扇有左栏），后来商店/收藏/社交各自的键表越长越满，
它**从没回头核过**；症状**静默**（末项照建、只是被关掉，而自检走 `GetComponentsInChildren(true)` 照样数得到它）。

### 二、`A1498` 的四条文案（键名 / 英文 / 中文三列各有出处）

| 位 | 节点 | TMP `m_text` | `Localize.mTerm` | 中文 | 中文出处 |
|---|---|---|---|---|---|
| 1 | `Deck ▸ Label ▸ TabButtonLabel` | **`Deck`**（MB `6937732152595899093`，`m_fontStyle 16` / `m_fontSize 36` / `折行 0`） | `MainMenu/MainButtons/ButtonLabel/Deck` | **卡组** | **用户实拍**（见下）+ `zh_CN.csv` 的 `Deck`→`卡组` |
| 2 | `Cards ▸ …` | `Cards` | `MenuShop/ShopItemType/Cards` | **张牌** | **复用**表里已有那条（它的中文来自用户 2026-10-17 的卡组编辑实拍） |
| 3 | `CardBacks ▸ …` | `Cosmetics` | `MenuShop/ShopItemType/Cosmetics` | **美容品** | 同上（复用） |
| 4 | `Alternate Art ▸ …` | `Styles` | `MenuShop/AlternateArtStyle` | **风格** | **用户实拍** + `zh_CN.csv` 的 `Styles`→`风格` |

**实拍判据**：`C:/Users/qjh36/Desktop/warpforge参考图/收藏—卡组的参考图.png`（原版中文客户端，2026-10-10）
—— 左栏自上而下逐字读到 **卡组 / 张牌 / 美容品 / 风格**，**四颗都在屏上**（本例同时反证「只画出 3 个」是我们侧的缺陷）。
同图左竖导航读到 玩 / 收藏 / 商店 / 奖励 / 社交（与 `Loc` 的 E1 那五条一致，可互证这套读法）。

⚠️ `zh_CN.csv` 对通用词给的是 `Cards`→**卡牌**、`Cosmetics`→**装饰**，与实拍不同 ⇒ **按实拍取**
（实拍是第一档判据；且原版这两颗与卡组编辑窗**共用同一条词条**，两处必须同一个词）。

### 三、`A1501` 的六条键（逐节点实读）

方法：走 `Practice Mode Menu` 根（RT `-3883952598416666820`）的**全部 129 个后代**，逐颗打印它挂的
`Localize.mTerm` 与 TMP 的 `m_text`（脚本在本会话的临时目录，逻辑 = 「按 `m_RectTransform.m_Father` 爬树 +
`GameObject.m_Component` 反查组件」）。

| 我们的节点名 | 深度 | 原版节点 | TMP `m_text` | `Localize.mTerm` | 中文出处 |
|---|---|---|---|---|---|
| `Back Text` | d2 | `Back button ▸ Text` | `Back` | `MainMenu/MainButtons/ButtonLabel/Back`（**表里已有**） | `zh_CN.csv` `Back`→返回 |
| `tooltip` | d3 | `Deck Selector ▸ Deck Buttons ▸ tooltip` | `Select deck to play` | `Demo/DeckSelectionDemo/SelectDeck` | `zh_CN.csv` →**选择卡组开始游戏** |
| `Deck Information Cost/balance text` | d3 | `Deck info ▸ General container ▸ …` | `Card / Energy cost` | `Demo/CardCostBalance` | `zh_CN.csv` →**卡牌 / 能量费用** |
| `Change Deck Text` | d4 | `… ▸ Generic Simplified UI Button ▸ Button Text` | `Change Deck` | `MainMenu/RankedWindow/ChangeDeck` | `zh_CN.csv` →**更换卡组** |
| `Toggle Label` | d3 | `Toggle ▸ Label` | `Game mode` | `Event_Description/Generic/GameMode` | `zh_CN.csv` →**游戏模式** |
| `Battle Text` | d2 | `DeckSelectionContinueButton ▸ Text` | `Battle!` | `Demo/ToBattle` | `zh_CN.csv` →**开战！** |

⚠️ **任务清单上写的中文是「战斗！」，`zh_CN.csv` 精确命中那一行给的是「开战！」** —— 按第一档判据取 **开战！**。
⚠️ 另有一颗 `GameModeText`（`'Game mode: Multiplayer'`）原版**出厂 act=0** ⇒ 我们本来就没建，**不接**。

### 四、`A1493④`：色偏那一半**已经是对的**（逐值核过，一个字没改）

判据（原版 prefab）：`Base Game Mode Container 1x1` 根挂 `EverguildButton` MB `4126037038486929469`：
`m_Transition = 1`(ColorTint) · `m_TargetGraphic = 7827625613328400445`。

**本轮把那条链路走到底了**（原来是「逐跳解出来」的推断）：`GameObject/Background Image_241192310724640829.json`
的组件表 = `[2868176476803776573, -3236662537065316291, **7827625613328400445**, -8747747232262242243, 7099107274838720573]`，
而 `7827625613328400445` 那颗 MB 的类名 = **`UIParallaxImage`**（`m_Material = 3583370424645672959` · **`useParallax = 1`** · `m_Color` 白）
⇒ `m_TargetGraphic` **确认**就是卡图那颗。

我们把这条色偏接在 `Shell/MainMenuRuntime.cs` 第 ⑥ 步的 `wb.TintOn(cardArtQ)` 上；而
`WindowButton` 的**缺省值**正好就是原版那一组：`HighlightK = SelectedK = 0.9607843` · `PressedK = 0.7843137` ·
Normal 白 · `FadeSeconds = 0.1`（`Shell/WindowButton.cs:54/56/58/101`；本处**没有**覆盖 `HighlightKey`，
只有主菜单那 5 个导航钮覆盖成浅蓝）⇒ **色偏那一半：判据齐、实现齐**。
另：`m_Transition` 就是 `1`，**没有位移 / 缩放 / 换图 / Animator**（本轮复核一遍，与上一会话结论一致）。

### 五、`A1493④`：视差那一半 —— **算式已解出**（本轮新查出来的，落进代码了）

原版 `UIParallaxImage` 的**序列化字段只有一个**（`useParallax`，出厂 1），行为全在方法体里：

- `d:/2/tools/decomp_full/UIParallaxImage__UpdateParallax.c` 逐句：
  `cam = canvas.worldCamera` → `p = transform.position` → `sp = RectTransformUtility.WorldToScreenPoint(cam,p)` →
  `material.SetVector(id, (sp.x/Screen.width, sp.y/Screen.height, 0, 0))`
  ⇒ 写进去的是**这一件在屏幕上的归一化位置**（`id` 来自 `Shader.PropertyToID("<字面量>")`）。
- 材质 = **`Main Menu Container Parallax`**（`bundle_menus_assets_all/Material/Material_3583370424645672959.json`），
  shader = **`Shader Graphs/UI Container Main Menu`**（`Shader_5261144965713594700.json`），
  `m_PropInfo` 只有三个属性：`_MainTex` · **`_Parallax`**（Vector）· **`_ScreenPosition`**（Vector）。
- **算式**：HLSL 源码被剥了，但**编译字节码在**（`Shader.compressedBlob`，LZ4 → DXBC）。本机
  `C:\Windows\System32\D3DCompiler_47.dll` 的 **`D3DDisassemble`**（ctypes 直接调，**没装任何新东西**）反汇编后，
  **四个顶点程序逐字相同**的最后两行是：
  ```
  add r0.xy,  cb2[1].zwzz, l(-0.5, -0.5, 0, 0)     // cb2 = UnityPerMaterial
  mad o5.xy,  cb2[1].xyxx, r0.xyxx, v3.xyxx         // o5 = 传给像素着色器的 _MainTex UV；v3 = 网格 UV
  ```
  而**四个像素程序一个字的视差都没有**（只有 `sample_b t0, v5.xy`）⇒ 整条视差在**顶点着色器**里：
  **`uv' = uv + _Parallax.xy * (_ScreenPosition.xy − 0.5)`**。
- `_Parallax = (0.2, 0, 0, 0)`（那个材质的 `m_SavedProperties.m_Colors`）⇒ **只有水平位移，垂直恒 0**。
  ⚠️ 同文件 `m_Floats` 里还有一条 `_Parallax = 0.005` —— 那是**内置 Standard shader 的残留死值**
  （本 shader 的属性表里 `_Parallax` 是 **Vector**）⇒ **用的是 0.2，不是 0.005**。
  这正是 CLAUDE.md §三「原版材质上带着内置 Standard shader 的残留值」那条，**差点踩**。

**我们怎么落**：`ImageQuad` 的 UV 是**烘进网格**的（不是每帧 `SetVector`），而模式卡**不动**、
`_ScreenPosition` 又只跟「这件在屏幕上的位置」有关 ⇒ **建卡时算一次、把位移加进 `SetUvRect` 的 `u0`，等价**。
位移**不会**把 UV 顶出 `[0,1]`（判据见下「状态→参数」表），所以踩不到贴图的 wrap 模式。

---

## 状态 → 参数 表

### 表 1：`A1498` 的「末项是不是母版」（**一个值 ≠ 全部情况**，铁律 5·c）

| 窗 | 原版 `tabButtonPrefab` | 我们传的 `lastIsMaster` | 运行期可见键数 |
|---|---|---|---|
| 奖励窗 | 非 null（第 4 个孩子） | `true`（缺省，**未改**） | 5 项里 4 个可见（含自建的 `DailyStreak`） |
| 商店 | 非 null（`Shop Icon`） | `true`（缺省，**未改**） | 4 项里 3 个可见 |
| **收藏** | **null** | **`false`（本件改）** | **4 项全可见** |
| **社交** | **null** | ⚠️ **仍是 `true`**（`Shell/SocialWindow.cs` 不在白名单）⇒ **今天只显示 1 个** | **2 项里 1 个可见（缺陷仍在）** |

### 表 2：`A1493④` 视差的运行时取值

| 量 | 原版 | 我们 |
|---|---|---|
| `useParallax` | `1`（模式卡那 16 颗）· `0`（另 15 颗）· 另 1 颗 `1` 但 `m_Material` 为 null | 恒生效（我们只建模式卡这一族） |
| `_Parallax` | 材质常数 `(0.2, 0, 0, 0)` | `MainMenuRuntime.ModeCardParallaxX = 0.2f` |
| `_ScreenPosition` | **运行时逐帧写** = `(transformScreenX/Screen.width, transformScreenY/Screen.height, 0, 0)` | 建卡时算一次：`设计x / LayoutSpace.DesignPxW`（≡ 原版那个比值，本工程是 1920×1080 设计空间，与真实分辨率无关） |
| 垂直分量 | 恒 0（`_Parallax.y = 0`）⇒ Unity「屏幕 y 自下而上 / 我们自上而下」**不产生差别** | 不实现 |
| 位移范围 | — | 五张卡 `x∈[CardCol0, CardCol0+3·ColStep]` ⇒ `du ∈ [-0.066, +0.098]`；采样窗宽 **0.715** ⇒ 最坏 `[0.0765, 0.9555]` ⊂ `[0,1]` |

**只覆盖了这一种情况（如实标）**：上表只覆盖**主菜单模式卡**这一族。`bundle_menus_assets_all` 里
共 **32 颗 `UIParallaxImage`**：**16 颗 `useParallax=1`**（其中 11 颗 `m_Color` 白 + 5 颗 `m_Color=0.4245` 灰
—— 另 5 颗应是「未解锁」那一档的同一族 prefab）、15 颗 `useParallax=0`、1 颗 `useParallax=1` 但材质为 null
（那颗取不到视差 shader ⇒ 原版那儿也**没效果**）。**其余用这个 shader 的地方本轮没查**（本包里只有这一个材质用它）。

### 表 3：`A1501` 六段字的语言两列

| 键 | 中文档（出厂） | 英文档 |
|---|---|---|
| `Demo/ToBattle` | 开战！ | Battle! |
| `Demo/DeckSelectionDemo/SelectDeck` | 选择卡组开始游戏 | Select deck to play |
| `Demo/CardCostBalance` | 卡牌 / 能量费用 | Card / Energy cost |
| `MainMenu/RankedWindow/ChangeDeck` | 更换卡组 | Change Deck |
| `Event_Description/Generic/GameMode` | 游戏模式 | Game mode |
| `MainMenu/MainButtons/ButtonLabel/Back` | 返回 | Back |
| `MainMenu/MainButtons/ButtonLabel/Deck`（收藏窗第 1 键） | 卡组 | Deck |
| `MenuShop/ShopItemType/Cards`（收藏窗第 2 键） | 张牌 | Cards |
| `MenuShop/ShopItemType/Cosmetics`（收藏窗第 3 键） | 美容品 | Cosmetics |
| `MenuShop/AlternateArtStyle`（收藏窗第 4 键） | 风格 | Styles |

> 中文列**不是**原版中文客户端的逐字文案（原版中文在远端 I2 表里），除**收藏窗那四个词**（那是实拍）之外，
> 其余都是 `zh_CN.csv` 那一档。渲染时基类会 `ToUpperInvariant()`：英文档印 `DECK`/`CARDS`/…（照原版），
> 中文档是汉字，**不受影响**。

---

## 没查清的部分

1. 🔴 **视差算式里的 cbuffer 打包是【推断】，不是读出来的字段。** `Shader.m_ParsedForm` 里没有 RDEF 反射块，
   `m_PropInfo` 只给类型（两个都是 `Vector`）不给宽度 ⇒ 「`cb2[1].xy = _Parallax` / `.zw = _ScreenPosition`」
   是从「**只有这一读法两个属性都用得上、量级也合理**」反推的。**反证两条**（若读反了会怎样）：
   · 若 `.xy` 是 `_ScreenPosition` ⇒ 位移变成 `−0.5 × 屏幕位置`（最大 ~0.5 UV ≫ 可见窗 0.715 宽），画面会整块跑掉；
   · 若整个 `cb2[1]` 只是其中**某一个** ⇒ 另一个属性**完全用不上**，那 `UIParallaxImage` 这个类的存在就没意义。
   **还差什么**：一个能给出常量缓冲**变量偏移**的口（Unity 侧的 `ShaderUtil`/inspector 反射，或 RDEF 段）。
   **验收法**：把 `00_主菜单_无弹窗.png` 与 `主界面参考图.png` 的**同一张模式卡并排渲一次**看卡图有没有横向错开
   —— 断言量不出「像不像」（铁律 10 第 6 条）。**判为错的处置**：把 `MainMenuRuntime` 第 ① 步那句 `+ du` 删掉即可回退。
2. **五个模式卡的标题**（`DRAFT MODE` / `RANKED` 那两张）**仍是英文** —— 原版那颗 `Event Title` 由
   `LiveOpsAssetUtility.GetLabel(event)` 从**服务端**灌，本地无据；参考图上只有三个中文（教学/练习/小冲突）。
   ⇒ **⛔ 没自己译**（与本件同一条铁律）。这一条**本来是 `A1493` 里已记的「等拍板」项**，不是本件能收的。
3. **`LiveOpsEventWindow` / `SkirmishEventWindow` / `TutorialModePopup` 里同一族的英文**（`Battle!` / `Game mode`）
   本轮**没查它们的 `mTerm`**（不在白名单）⇒ 见「顺手发现」第 3 条。

---

## 我建议加的断言（逐条 · **主对话统一落盘，我自己没写**）

> 全部在 `Editor/` 下，本件不许碰。⚠️ 期望值**不要读我们的实现常量**（读了 = 自证）。

### A. `A1498`（宿主 `Editor/CollectionScene.cs`）

1. **★ 末键现在真的可见**（这条是**本例的核心**，今天的断言抓不到它）：
   `FindChild(root, "CollectionTabButton_3").gameObject.activeSelf == true`
   —— ⛔ **不要**再用 `GetComponentsInChildren<Transform>(true)` 数个数（那正是「全绿但画面少一个」的来源）。
   **改坏法**：删掉 `BuildShell(..., lastIsMaster: false)` ⇒ 立刻红。
2. **四颗键的文案走词条、逐颗断字面量**（⛔ 别读 `CollectionWindow.Buttons[i]` 的 `Term` —— 那是被测实现里的数）：
   期望值 = `Loc.T("<写死的键名>").ToUpperInvariant()`，键名逐颗写死：
   `MainMenu/MainButtons/ButtonLabel/Deck` / `MenuShop/ShopItemType/Cards` /
   `MenuShop/ShopItemType/Cosmetics` / `MenuShop/AlternateArtStyle`。
   **前提断言**（防「两边一起退化成键名」的假绿）：`Loc.HasEntry(每一个键)` 为真。
3. 🔴 **`Editor/CollectionScene.cs:946` 的 `wantLabels` 必须改**（今天仍写 `{"DECKS","CARDS","COSMETICS","STYLES"}`）
   —— 改成上面第 2 条那种 `Loc.T(键).ToUpperInvariant()` 写法。**不改 ⇒ 本批一跑就是 4 条红**，
   而且红的是**断言过期**、不是实现错。
4. **社交窗同一个缺陷**（宿主 `Editor/MainMenuScene.cs`，与 `SocialTabButton_*` 那一段同址）：
   补一条 `FindChild(bar2, "SocialTabButton_1").gameObject.activeSelf == true`
   —— 它今天**恒假**（原因见「顺手发现」第 1 条）。

### B. `A1501`（宿主 `Editor/MainMenuScene.cs`）

5. 🔴 **`:1721` / `:1722` / `:1723` / `:1686` 四条字面量断言必须改**成
   `Loc.T("<键>")`（键见「判据」节·三那张表；`:1686` 是 `Demo/CardCostBalance`）。**不改 ⇒ 一跑就红。**
6. 建议顺手补一条**「六处都接了词条」的结构断言**：`PracticeModePopup` 建完之后，
   六颗（`Back Text` / `tooltip` / `Deck Information Cost/balance text` / `Change Deck Text` / `Toggle Label` / `Battle Text`）
   各自 `TextOf(...) == Loc.T(键)` —— 断言 5 是逐条比字面量，这条是**逐条比「键对得上」**，
   一起放能挡「把某处接错键」（六处的键**互不相同**，接错会当场露）。

### C. `A1493④`（宿主 `Editor/MainMenuScene.cs`，主菜单那一段）

7. **视差那一半**：读口现成 —— `Battle/ImageQuad.cs:400` 的 `public Rect UvRect { get { return _uv; } }`
   ⇒ 断**五张卡的 `uv.x` 互不相同、且随卡的中心 x 单调**。
   **形状判别式**（防「两边一起改回去」）：`uv(最左那张).x < uv(最右那张).x`（`_Parallax.x>0` ⇒ 单调递增）。
   ⛔ **别断具体数值** —— 那是从我们自己的常量推的；要断就断「`uv.x − u0` 与 `(卡中心/1920 − 0.5)` 成比例」
   （比例常数仍是我们挑的，所以这条只算**形状**断言，**真值靠并排渲**）。
8. **色偏那一半**：加一条**「本处不许覆盖 `HighlightKey`」**的断言
   （`wb.HighlightKey == WindowButton 的缺省值`）—— 原版这三张卡吃的是全库默认那组（`0.9608`），
   而主菜单另有 5 颗导航钮**故意覆盖成浅蓝**，两条很容易被一起改错。
   ⚠️ 若 `Shell/WindowButton.cs` 的属性可读，**优先读 `TintKeyForTest`**（`:563` 已有这个口）。

---

## 顺手发现（看见了但**没改**）

1. 🔴 **社交窗（`Social Submenu Variant`）今天只画出 1 个键** —— **和 `A1498` 是同一个根因**
   （原版本窗 `tabButtonPrefab = null`，两个孩子 `Alliances` / `Friends` 都是真页签，我们却把第 2 个
   （`Friends`）当母版关掉了）。判据 / 实测见「判据」节·一那张表。
   **改法 = 一行**：`Shell/SocialWindow.cs` 的 `Build()` 里 `BuildShell(..., "Tabs")` → 加 `lastIsMaster: false`。
   ⛔ **不在本件白名单**（红线 5）⇒ **停手报上来**。
   ⚠️ 今天的断言抓不到它：`Editor/MainMenuScene.cs:7221-7225` 用 `FindChild`（含 inactive）找
   `SocialTabButton_0/1`，**两件都找得到** ⇒ 全绿。建议断言见上面第 4 条。
2. **`CollectionWindow` 的 `TabButtons.options[i].type` 是**错的值**（今天无害，将来会咬人）**：
   `BuildBar` 建 `options` 时读的是 `visualTypes[i]`，而**收藏窗/社交窗都在 `BuildShell` **之后**才填
   `visualTypes`** ⇒ 那一刻读到的还是基类里那份**奖励窗的默认表**
   （`Missions/Campaign/Forge/DailyStreak/None`）⇒ 收藏窗第 4 格的 `options[3].type` 会是 `DailyStreak`。
   今天**全工程没有任何代码读 `options[i].type`**（`grep` 过；只在注释里被引用）⇒ 不影响画面。
   **没改**：改它要动 `BuildShell` 的调用顺序或 `TabButtons` 的填充时机，属结构改动，**请调度台裁定**。
3. **同一根因、白名单外的其他英文站点**（都在我看到的范围内，**都没动**）：
   · `Shell/LiveOpsEventWindow.cs:912` —— 开战钮写死 `"Battle!"`（原版应是 `Demo/ToBattle`，**未核**）；
     同文件 `:855` `TitleText = "Game mode"`。
   · `Shell/TutorialModePopup.cs:148` `TitleText = "Game mode"`（`Editor/MainMenuScene.cs:3423` 现按**字面量**断它）。
   · `Editor/MainMenuScene.cs:3423/5277`（`Window Title` = `Game mode`）· `:5372`（`To Battle Button ▸ Button Text` = `Battle!`）
     —— 这两处是**别的窗**，我**没查它们的 `mTerm`**，所以**不能**说它们该用我新加的那两条键。
4. **`Shell/PracticeModePopup.cs` 的 `Txt(...)` 现在会吃 `null`**（`Loc.T(null)` 返回空串）——本件六处**都传的是真键**，
   没有实际影响；记一笔是因为 `Shell/DeckInfoPopup.cs`（**不在白名单**）里那份同族的 `Card / Energy cost` /
   `Change Deck` **也还是英文**（我从 `Editor/MainMenuScene.cs` 的注释里看到那扇窗有同名节点，**没去核**）。
5. **本会话期间有别的写手在同一棵树上动文件**：`Shell/{WindowButton.cs(新), PromptPopup.cs, ShellParts.cs, WindowsManager.cs}`、
   `Core/{DraggableController.cs, CardDraggingController.cs(新), …}`、`Battle/BattleDriver.cs`、`Editor/*` 各有改动
   —— `Shell/PromptPopup.cs` 从 1109 行缩到 325 行（`WindowButton` 被搬到新的 `Shell/WindowButton.cs`）。
   ⚠️ 与本件**零交集**（本件五个文件都在我的白名单里），但**类型检查的读数是在这种状态下取的**（见下）。

---

## 类型检查读数（运行时 / 编辑器）

```
$ TMPDIR=/tmp/wf_a bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

**两读数都是 0**（取读数的时刻：本件五个文件都已改完）。
⚠️ **当时有别的写手在改白名单外的文件**（见「顺手发现」第 5 条），而那两读数是 **0** ⇒ 说明那一刻树上是可编译的、
**没有撞上别人的半成品**。若调度台在别的时候重跑看到非 0：先看错误**集中在哪个文件**，再判是不是别人的活（铁律 13·3）。

**行尾**（改完立刻量的，`git diff --numstat` 也逐条看过，数字接近文件行数 = 翻了行尾的那种事故**没发生**）：

```
Core/Loc.cs               CRLF 0 / LF 2495 / 2496 行   diff 46/0
Shell/MenuWindowBase.cs   CRLF 0 / LF  910 /  911 行   diff 56/9
Shell/CollectionWindow.cs CRLF 0 / LF 3441 / 3442 行   diff 38/6
Shell/PracticeModePopup.cs CRLF 0 / LF 2085 / 2086 行  diff 22/6
Shell/MainMenuRuntime.cs  CRLF 0 / LF 1686 / 1687 行   diff 80/1
```

**没跑自检**（红线 1：同一工程同时只能一个 Unity 实例）。**该跑哪几条**（按铁律 12 的覆盖面判据）：
本件动了**共用件**（`MenuWindowBase` 的 `TabBtnSpec` / `BuildShell` / `BuildBar`）⇒ **先 `grep` 谁在用**：
`TabBtnSpec` 的**代码**引用只在四扇窗 + `MenuWindowBase`（`RewardsWindow` / `ShopWindow` / `SocialWindow` / `CollectionWindow`；
`Battle/Label.cs` 与两处 `Editor/*` 里**都只是注释**，已逐处看过）
⇒ 那四扇窗**都要跑到**：`CollectionScene.Run` + `MainMenuScene.Run`（社交窗的宿主在这里）+ `RewardsScene.Run` + `ShopScene.Run`；
`A1501` 的宿主也是 `MainMenuScene`（练习窗在那里建），已含在内。
⚠️ 三扇窗**没传 `Term`、也没传 `lastIsMaster`** ⇒ 走的是两个缺省分支（`Term` 空 = 逐位等于旧行为；
`lastIsMaster` 缺省 `true` = 逐位等于旧行为）⇒ 预期零变化，但**共用件改了就要按这条跑**。

---

## 收口前必须先做的两件事（**都在 `Editor/`，本件不许碰**）

1. `Editor/CollectionScene.cs:946` 的 `wantLabels` 改成按 `Loc.T(键)` 取（**现在一跑必红 4 条**）。
2. `Editor/MainMenuScene.cs:1686 / 1721 / 1722 / 1723` 四条字面量断言改成 `Loc.T(键)`（**现在一跑必红 4 条**）。
