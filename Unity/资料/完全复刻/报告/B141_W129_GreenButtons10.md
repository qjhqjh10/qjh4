# B141 · W129 · 5-14「另 10 颗绿底钮」落点普查 —— `B141-W79`

> 2026-11-04 · 第一百二十八会话 · 批 `B141` · **只读**调查代理 `B141-W79`。
> 输入判据 = `报告/B141_W129_ButtonFx.md` §5-14（W34）· `报告/B141_W129_Button506.md`（W52）·
> `报告/B131_审计_色彩材质与渲染.md:50-51`（A15/A16）· `判据/B141_W129_对账_卡组色彩.md:76,138`。
> 现读源：`d:/2/tools/assetripper_out/ExportedProject/Assets/**.prefab`（1,314 个顶根 + 子目录）·
> `d:/2/新解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour/*.json`（35,014 份）·
> `d:/4/Unity/MyGame/Assets/CardPresentation/**`。
> ⛔ 未改任何源码/资产/账/正本/活状态表/所有权表 · 未跑 Unity · 未动 git · 未改 `d:/2` 一个字节。

---

## 〇、一句话结论

**10 颗已定位，10/10「不需要接绿 Normal」** —— 它们是 **下拉列表的条目模板 `Template ▸ Viewport ▸ Content ▸ Item`**，
组件是 `UnityEngine.UI.Toggle`、**`m_Transition = 2`（SpriteSwap）** ⇒ **`m_Colors` 在原版里根本不参与渲染（死值）**。
屏幕上真正的颜色来自 `Item Background` 的 `Image.m_Color`（设置窗=亮绿、社交=橙），**我们两处都已逐值复刻**。
⇒ `B131色 A16`「⛔缺实现」这一行**判据本身错**（与同一份审计的 `A9`「`trans=2` ⇒ `m_Colors` 死值 ✅一致」自相矛盾）。

**唯一真残差**：两处 `Item Background` 染色用的是**取整字面量**（差 ≤1.4e-3），不是逐位原版值（见 §四 表末）。

---

## 一、10 颗逐颗（原版 prefab 现读）

组件 = `UnityEngine.UI.Toggle`（`m_Script {fileID: 2109663825, guid: d3e719b59ab71ba3f6b398058c866280}`，
`m_IsOn: 1` · `toggleTransition: 1`(Fade)）· `m_Group {fileID: 0}` · `onValueChanged` 空。

**十颗的 `m_Colors` 逐位相同**（`Main Menu Settings Window.prefab:16294/21422/35533/36710` ·
`Social Submenu Variant.prefab:20371/21516/23993/24976/32287/33270`）：

| 格 | 原版值 |
|---|---|
| `m_Transition` | **2 (SpriteSwap)** ← 决定性字段 |
| `m_NormalColor` | (0.36862746, 0.89411765, 0.5882353, 1) |
| `m_HighlightedColor` | (0.9607843, 0.9607843, 0.9607843, 1) |
| `m_PressedColor` | (0.78431374, 0.78431374, 0.78431374, 1) |
| `m_SelectedColor` | (0.9607843, 0.9607843, 0.9607843, 1) |
| `m_DisabledColor` | (0.78431374 ×3, **a 0.5019608**) |
| `m_ColorMultiplier` | **1**（⛔ 不是 A15 那一族的 2） |
| `m_FadeDuration` | 0.1 |
| `m_SpriteState` | HL `40K_dropdown_item_hover` · PR `40K_dropdown_item_press` · SEL `40K_dropdown_item_selected` · DIS null |
| `m_TargetGraphic` | `Item Background` 的 Image |
| `graphic`（勾） | `Item Checkmark` 的 Image（内置 `Checkmark`，`m_Color (1,1,1,1)`） |

| # | prefab ▸ 节点路径 | Item GO | Toggle MB | `Item Background` Image `m_Color` |
|---|---|---|---|---|
| 1 | `Main Menu Settings Window` ▸ `Menu Area/Tab Content/General Tab/Language Selector/LanguagesDropdown/Template/Viewport/Content/Item` | `1964469411310401` | `114926484194617170` | 亮绿 (0.28627452, 0.9647059, 0.6862745) |
| 2 | 同上 ▸ `…/Media Tab/Visual Settings/WindowMode Selector/Dropdown/Template/Viewport/Content/Item` | `1358105479016104` | `114070843845074976` | 亮绿 同上 |
| 3 | 同上 ▸ `…/Graphics Tab/Content/Quality  Selector/Quality DropDown/Template/Viewport/Content/Item` | `1573076885614623` | `114406034059894822` | 亮绿 同上 |
| 4 | 同上 ▸ `…/Graphics Tab/Content/Text In Hand  Selector/Text in Hand DropDown/Template/Viewport/Content/Item` | `1777414246829702` | `114573708808914349` | 亮绿 同上 |
| 5 | `Social Submenu Variant` ▸ `Content Area/Tabs/Alliances Tab/AllianceNotMemberVariant/Create Alliance View/TopAnchor/Select Language/LanguagesDropdown/Template/Viewport/Content/Item` | `1690175861776792` | `114068642455749504` | 橙 (1, 0.4739555, 0.0990566) |
| 6 | 同上 ▸ `…/Create Alliance View/TopAnchor/Select Privacy/Privacy Dropdown/Template/Viewport/Content/Item` | `1066778049879928` | `114161367720648980` | 橙 同上 |
| 7 | 同上 ▸ `…/AllianceNotMemberVariant/GeneralDetails/Content/Config fields/LanguagesDropdown/Template/Viewport/Content/Item` | `1220697728379336` | `114766875343807881` | 橙 同上 |
| 8 | 同上 ▸ `…/GeneralDetails/Content/Config fields/Privacy Dropdown/Template/Viewport/Content/Item` | `1738892719839630` | `114028443285903574` | 橙 同上 |
| 9 | 同上 ▸ `…/AllianceMemberVariant/GeneralDetails/Content/Config fields/LanguagesDropdown/Template/Viewport/Content/Item` | `1105912343260855` | `114806980798193366` | 橙 同上 |
| 10 | 同上 ▸ `…/GeneralDetails/Content/Config fields/Privacy Dropdown/Template/Viewport/Content/Item` | `1505682267929073` | `114363053058138888` | 橙 同上 |

**同族第 11 颗（B131 那次没数进去）**：`Assets/GameObject/GenericDropdown.prefab ▸ Template/Viewport/Content/Item`
（GO `1099118695303065` · Toggle MB `114694121525821480` · `m_Colors` **逐位同上** · Item Background 亮绿）。
⇒ 这解释了「为什么是 10 而不是 11」：`B131` 只扫 `Assets/*.prefab`（顶根），`GenericDropdown` 在 **`Assets/GameObject/`** 子目录。
**判据佐证**：`bundle_menus_assets_all` 全量 35,014 份 MB 里，`m_NormalColor` 是这一族绿的**恰好 22 颗 = 11 个 `trans=1`(乘数 2) + 11 个 `trans=2`(乘数 1)**
（脚本现扫，见 §六 反证）。

> 📌 `B131色 A16` 引的 prefab 名 `Generic Multi Card Display Combat.prefab` 与它自己写的值**互相矛盾** ——
> 那份 prefab 的 `m_NormalColor` 是 A15 那族的 **b = 0.5874373**（乘数 2），不是 A16 写的 **b = 0.5882353**。
> A16 的**值**和**颗数 10** 都只与本节这族对得上（铁律 5：以现读为准，引名作废）。

---

## 二、为什么这 10 颗的绿是「死值」（机制判据）

1. `Selectable.DoStateTransition` 按 `m_Transition` 分派：**`2 = SpriteSwap` 只走 `DoSpriteSwap()`，不读 `m_Colors`**
   （`transition = 1 (ColorTint)` 才走 `CrossFadeColor`）。
2. 它们的 `m_SpriteState` 三张图**全是非空真 guid**（hover / press / selected，见 §一），不是「全 null 的空壳」。
3. **同一份审计自己已经立过这条口径**：`B131_审计_色彩材质与渲染.md:47`（A9）「`m_Transition = 2` SpriteSwap ⇒ `m_Colors` 不参与渲染 | 死值 | 不覆盖 ✅一致（原版死值）」。
   ⇒ A16 把它判成「⛔缺实现」，**与 A9 冲突**；同一条机制不能两套判法。
4. 屏幕上的绿/橙来自 `Item Background` 的 **Image `m_Color`**（Image 的颜色**无条件**生效，与 transition 无关），
   这一格我们两处都已复刻（§三）。

---

## 三、我们侧的落点（现读 `文件:行号`）

| 原版族 | 我们建在哪 | 现读 | 与原版比 |
|---|---|---|---|
| 设置窗四颗下拉的条目 | `Shell/SettingsWindow.cs:3953-3972`（`BuildDropdownItem`）· 语言那颗 `:3664`/`:3716` | `Hit3(...)` 传 `ArtLstItem/Hi/Pr/Sel` 四图（`:836-837`）· `Item Background` 染色 `LstItemTint`（`:840`）= (0.2863, 0.9647, 0.6863) | 图四态 ✅ · 染色**取整**（差 ≤1.4e-3） |
| 社交 6 颗下拉的条目 | `Shell/AllianceMemberTab.cs` 的 `AllianceDropdownBox`（类 `:2293` · `Create :2401` · `BindRow :2630` · `ItemArtName :2623` · `ItemSelArt :2320`） | 条目底染色 `new Color(1f, 0.475f, 0.098f, 1f)`（`:2518`）；常态 `40K_dropdown_item` / 选中 `_selected`（`:2623`） | 染色**取整**（真值 0.4739555 / 0.0990566）· HL/PR 两态未显式换图 |
| 建这些下拉的两个宿主 | `Shell/AllianceSearchTab.cs:984-990`（Create Alliance View 的 Select Language / Select Privacy）· `Shell/AllianceSearchTab.cs:1036` · `Shell/AllianceMemberTab.cs:1453-1456`（Member 的 LanguagesDropdown / Privacy Dropdown） | 都走 `AllianceDropdownBox` | 4 个活的 ×2 变体 |
| #7/#8（NonMember GeneralDetails 那两颗） | **不建** —— `AllianceGeneralDetails.Build` 只在 `variant == Member`（`AllianceMemberTab.cs:1451`）建下拉 | 原版那两颗在这一份实例里 **act F**（`AllianceMemberTab.cs:205-208` 记着：act F 里两个下拉全 **F**） | ✅ 等价（原版恒不显示 ⇒ 铁律 11 例外①） |

---

## 四、判定表

| # | 原版 prefab/节点 | 四态/倍率 | 我们落点 | **判定** |
|---|---|---|---|---|
| 1-4 | `Main Menu Settings Window` 四颗 `Item` | trans2 死值 / ×1 | `SettingsWindow.cs:3953` + `:840` | **已对**（绿 Normal 无需接；染色取整待精调） |
| 5-6 | `Social Submenu Variant` Create Alliance View ×2 | 同上 | `AllianceSearchTab.cs:984-990` → `AllianceDropdownBox` | **已对** |
| 9-10 | `Social … AllianceMemberVariant` ×2 | 同上 | `AllianceMemberTab.cs:1453-1456` → `AllianceDropdownBox` | **已对** |
| 7-8 | `Social … AllianceNotMemberVariant` ×2 | 同上 | 不建（原版 act F 恒不显示） | **无落点（原版死支，例外①）** |
| +1 | `GameObject/GenericDropdown.prefab ▸ Item` | 同上 | 我们无共享下拉 prefab（设置窗自建、社交走 `AllianceDropdownBox`） | **无需建**（只是模板；两套实现各自已等价） |

**汇总：10 颗 = 8「已对」+ 2「无落点（原版死支）」+ 0「可接线」。**
**唯一可派的小活**：把两处取整字面量换成逐位值 ——
`SettingsWindow.cs:840` `LstItemTint` → `(0.28627452, 0.9647059, 0.6862745)`；
`AllianceMemberTab.cs:2518` → `(1, 0.4739555, 0.0990566)`（`:2412`/`:2416` 同一族一并看）。
⚠️ 这两处属**参数取整**（铁律 21「色彩」横切），与 5-14 的「绿底钮」不是一回事，⛔ 别混派。

---

## 五、另一候选解读（若调度台的「10 颗」指 A15 那一族）

`B131` 引的 prefab（`Generic Multi Card Display Combat`）实际属于**绿底 + `m_ColorMultiplier 2` + `trans=1`** 那 11 颗，
`m_NormalColor` 是 **b = 0.5874373** 的**另一个值**。若「另 10 颗」其实指这一族，则现状是：

- **已做 2 颗**：`Practice Mode Menu ▸ Continue Button` · `Generic Multi Card Display Combat ▸ Button`（W52）；战斗侧 `CardChoicePanel` Continue（5-04 / W35）。
- **剩 8 个节点 / 5 个 prefab 行**：`RankedEventWindow ▸ Continue Button ×2` · `RankedEventWindowV2 ▸ Continue Button` ·
  `SkirmishModeEventWindow ▸ Continue Button` · `DeckSelectionContinueButton ▸ Continue Button` ·
  `Draft Mode Ongoing State ▸ Battle Button Button/Button`（+ 3 颗 Debug，原版 `OnEnable` 强制 SetActive(false)）。
- 这条**已被 W52 §六.2/§六.3 挂号**：「原版本来就是 `To Battle Button` 还是 `Continue Button`、该显示哪颗，要读
  `LiveOpsEventWindow` 的 `Awake/OnEnable` 逐颗 `SetActive`」⇒ **判据未定，⛔ 不属于本次 10 颗**，也⛔ 别在本报告口径下开工。
- 我方现读：`Shell/DraftModeWindow.cs:1711`（`To Battle Button`，`UI_Button_Mulligan`、**白 tint**）、
  `Shell/RankedEventWindow.cs`/`Shell/SkirmishEventWindow.cs` **无** Continue 钮（全文件 0 命中）· `DeckSelectionContinueButton` 只在
  `Shell/PracticeModePopup.cs` 出现。

---

## 六、建议派工

| 活 | 文件 | 颗数 | 说明 |
|---|---|---|---|
| **改判据、核销 A16/5-14 那半条**（调度台做，⛔ 非写手） | `活状态表.md` 的 `B141-BUTTON-514` · `B131_审计_色彩材质与渲染.md:51` · `判据/B141_W129_对账_卡组色彩.md:76,138` | 10 颗 | 判据正文写清「`trans=2` ⇒ `m_Colors` 死值，可见色 = Item Background 的 `Image.m_Color`，两处已复刻」 |
| （可选 · 低优先）染色精调 | `Shell/SettingsWindow.cs:840` · `Shell/AllianceMemberTab.cs:2518`（+`:2412`,`:2416`） | 2 处常量 | 取整 → 逐位；差 ≤1.4e-3，纯参数精度 |
| ⛔ 不要派 | —— | —— | 「给这 10 颗接绿 Normal」= 给原版死值造行为，会**多出一层染色**（绿 × 绿） |

---

## 七、❓/反证（搜过什么，⛔ 没猜）

1. **搜索口径**（全部现读、可复跑）：
   - `ExportedProject/Assets/**/*.prefab` 1,314 个顶根 + 子目录 → 正则 `m_NormalColor: {r: 0.36862…}` ⇒ **22 处 = 11 T1 + 11 T2**（脚本 `/tmp/w79/scan.py`、`/tmp/w79/paths.py`、`/tmp/w79/itemdetail.py`）。
   - `bundle_menus_assets_all/MonoBehaviour/*.json` 35,014 份 → 同值 **22 处**（`/tmp/w79/bundle.py`）；`trans=2` 那 11 处里 1 处属 `GenericDropdown`。
   - `ExportedProject/Assets/**/*.unity` 13 份战场场景 → 每份 **3 颗 T1（Continue Button + Button ×2）+ 1 颗 T2 Item**，全部是战斗侧（`A15` 族），**不含本报告这 10 颗**。
2. **❓1 根组件读取口径**：本节所有 `m_Colors` / `m_SpriteState` 都取自 **`.prefab` 里 `m_Father: {fileID: 0}` 的根子树**（脚本按 GO→Transform→`m_Father` 反解完整路径，见 §一 路径列），⛔ 未用 `assets_full` 的同名副本。
3. **❓2 「10 vs 11」的口径差**：本报告与 `B131` 都落在 **10**（顶根扫描），第 11 颗在 `Assets/GameObject/GenericDropdown.prefab`。若调度台口径是「全工程 11 颗」，第 11 颗判定同 §四（无需建）。
4. **❓3 `m_Colors` 是否真的完全不生效**：本机 `UnityEngine.UI.dll` 无源码（`TMP_Dropdown.cs` 那份是 TMP 的、不是 `Selectable`）⇒ 结论依据是 UGUI 公开语义（`DoStateTransition` 按 `Transition` 分派）+ **同仓 A9 已按此口径判过并通过复核**。⚠️ 未跑 Unity 实测像素（本代理不跑 Unity）。
5. **未查/未做**：`AllianceDropdownBox` 的条目 **HL/PR 两态是否换图**（`:2639` 两个 `null` 实参）—— 属 5-17（SpriteSwap 逐颗映射）与 5-07 的账，**不在本次 10 颗范围**，只记在这里供后续写手回源。

---

## 八、【续】`B141-W85` 施工记录 —— 两处下拉底色「取整 → 逐位」（2026-11-04）

> 执行写手 `B141-W85`（第一百二十八会话 · 批 `B141`）· 输入判据 = 本报告 §四 表末那两条 · 铁律 21「色彩参数」。
> 写入 = `Shell/SettingsWindow.cs`（仅 `:840` 那三行）· `Shell/AllianceMemberTab.cs`（仅列表底板那一行）·
> `Editor/B141_W70_Assert.cs`（并入 C 组）。⛔ 未跑 Unity · 未动 git · 未改 `d:/2` · 未改账/活状态表/所有权表。

### 八·〇 一句话结果

**设置窗那颗按原版逐位改完；社交那颗的「原版值」按调用方给的那一组落不了 —— 因为 `:2518` 不是「条目底」而是「列表底板」，它自己的真值是另一组。已按 prefab 现读取真值，并把两者搞混的证据与另一处真缺陷（条目底 tint = `null`）写在本节。**

### 八·一 两处改前 / 改后

| 落点（改后行号） | 对象（原版） | 改前 | 改后（现读） | 判据出处 |
|---|---|---|---|---|
| `Shell/SettingsWindow.cs:840` | `Main Menu Settings Window.prefab` 四颗 `Item Background ▸ Image.m_Color` | `new Color(0.2863f, 0.9647f, 0.6863f, 1f)` | `new Color(0.28627452f, 0.9647059f, 0.6862745f, 1f)` | prefab `:16349` · `:21477` · `:35588` · `:36765`（`m_Color: {r: 0.28627452, g: 0.9647059, b: 0.6862745, a: 1}`） |
| `Shell/AllianceMemberTab.cs:2526`（改前 `:2518`） | `Social Submenu Variant.prefab` 六颗 `Template ▸ Image.m_Color` | `new Color(1f, 0.475f, 0.098f, 1f)` | `new Color(1f, 0.4745098f, 0.09803922f, 1f)` | prefab `:20174` · `:21319` · `:23796` · `:24779` · `:32090` · `:33073`（`r: 1, g: 0.4745098, b: 0.09803922` = **121/255 · 25/255**） |

- **行数变化 = 0**（两处都是就地改写；`SettingsWindow.cs` 的 doc 注释同物理行改写，⛔ 没新增注释行 —— 账按行号引用，插行会让 `check_ledger_points` 报漂移）。
- 行尾：两文件全篇 **LF**（`crlf=0` · `bare_cr=0`，二进制现数）。
- 两处都打了 `✅ 已复刻（2026-11-04 · B141 · <判据出处>；断言 = B141_W70_Assert C*）`。

### 八·二 ⚠️ 判据订正（铁律 5）—— `:2518` 不是「条目底」

本报告 §三 / §四 把 `AllianceMemberTab.cs:2518` 写成「**条目底**染色」，并把「原版 `(1, 0.4739555, 0.0990566)`」挂在它上面。**现读 prefab 后不成立**：

| 原版节点 | sprite（guid 反查到 `.asset` 名） | `Image.m_Color` | 我们的落点 |
|---|---|---|---|
| `…/LanguagesDropdown/Template`（**列表底板**） | `40K_dropdown_bg`（`56f27927…`） | **(1, 0.4745098, 0.09803922)** | `AllianceMemberTab.cs:2526`（`_list` 那个 `MenuDraw.Nine`） |
| `…/Template/Viewport/Content/Item/Item Background`（**条目底**） | `40K_dropdown_item`（`16252dde…`） | **(1, 0.4739555, 0.0990566)** | `AllianceMemberTab.cs:2546` —— **tint 传的是 `null`**（= 白） |
| `…/LanguagesDropdown`（**合上面的框**） | `40K_dropdown_field_closed`（`7b4f088e…`） | (1, 0.4745098, 0.09803922) | `AllianceMemberTab.cs:2413` / `:2417`（取整 `(1, 0.475, 0.098)`） |

⇒ **两组橙是真·不同的两个值**（`0.4745098 vs 0.4739555` · `0.09803922 vs 0.0990566`），不是同一值的两种写法。
若按调用方给的「`:2518 → (1, 0.4739555, 0.0990566)`」照做，底板 b 通道的误差会从现在的 **3.9e-5** 变成 **1.0e-3**（≈26 倍），**是退步**。
⇒ 因此**按 prefab 现读取了 `Template` 的真值**（同一次「取整 → 逐位」的活，只是值取对了）。判据现读口径：
`sprite guid → .meta → Assets/Sprite/40K_dropdown_bg_0.asset.meta`（`grep -rl 56f27927…`），再按 `m_GameObject` 反解整条父链（脚本 `/tmp/b141w85/scan.py` / `lines.py`）。

### 八·三 顺手发现（**未改** —— 都在白名单外，交调度台派活）

1. 🔴 **条目底的 tint 根本没染**：`AllianceMemberTab.cs:2546` `MenuDraw.Rect(..., "Item Background", QItemBg, `**`null`**`, false)`
   —— 原版 `Item Background` 的 `Image.m_Color = (1, 0.4739555, 0.0990566, 1)`。`MenuDraw.Rect` 的 `tint` 为 `null` 时**不调 `SetTint`**（`Shell/MenuDraw.cs:1682` `if (tint.HasValue) quad.SetTint(...)`）⇒ 我们画的是**白 tint × 中性灰底图**（`40K_dropdown_item.png` 平均 opaque = (18,18,18)）⇒ 条目行是**灰**的，原版是**橙**的。
   一行补丁（建议原样派）：`2546` 行把 `null` 换成 `new Color(1f, 0.4739555f, 0.0990566f, 1f)`。
   ⚠️ 这条与调用方给的那组值**完全对得上** —— 值没错，**只是挂错了行**。
2. 🟠 **同族取整**（同一个值，四处）：`AllianceMemberTab.cs:2413` · `:2417`（合上面的框 `40K_dropdown_field_closed/_opened`）· `AllianceSearchTab.cs:1014`（`bgTint:`）· 以及三处**注释**里的 `(1,0.475,0.098,1)`（`AllianceMemberTab.cs:1623` · `:2406` · `TextInputField.cs:130` / `:395`）。原版真值 = `(1, 0.4745098, 0.09803922)`。
3. 🟠 **设置窗的另一个绿色常量也是取整**：`SettingsWindow.cs:683` `GenFieldTint = new Color(0.286f, 0.965f, 0.686f, 1f)` —— 原版真值 `(0.28627452, 0.9647059, 0.6862745)`（与本批改的 `LstItemTint` 逐位相同）。它被用在 **4 处**（`:2293` 字段底 · `:3681` / `:3933` 列表底板 · `:4477` / `:5594` 两格底）—— 而原版这几处（`Template` 3 颗 + `Item Background` 4 颗 + 那几颗字段）**全是同一个绿**（`Main Menu Settings Window.prefab:15754/16060/16349/20919/21225/21477/…`）。
   ⚠️ 这也说明 `:840` 那条 doc 注释里「两处各抄各的」**只在对象层面成立、值层面其实同值**（别据此把两个常量合并 —— 仍是两颗不同对象）。
4. ⚠️ **并发写手动过本文件**：本次开工时 `AllianceMemberTab.cs` = **2,872** 行，收工 **2,885** 行（`BuildList` 从 `:2506` 挪到 `:2513`，我的目标行 `2518 → 2526`）⇒ **账里凡是引用该文件 ≥ `:2293` 的 A 行都可能已漂**，调度台合并时请跑一次 `check_ledger_points`。我的两处编辑本身零行数变化、且已现读确认落点正确。
5. ❓**未核**（只记录、不置评）：原版每个下拉还有一颗 `Viewport` Image（内置 sprite `0000…f000` · `m_Type 1`），它的 `m_Color` 也是 `(1, 0.4739555, 0.0990566)`；我们列表里**没有建 `Viewport` 节点**（`BuildList` 直接 `Template` → `Item n`）。它是否承载 `RectMask2D` 裁切、不建会不会漏裁，**没查**。

### 八·四 断言（`Editor/B141_W70_Assert.cs` · C 组，逐通道 1e-6）

| 编号 | 断的是什么 | 灭自证（什么坏法会让它红） |
|---|---|---|
| **C1** | `SettingsWindow.LstItemTint` 三通道 = (0.28627452, 0.9647059, 0.6862745)，容差 1e-6 | 改回 `0.2863/0.9647/0.6863` ⇒ C1 红（差分 2.5e-5 / 5.9e-6 / 2.6e-5，都 > 1e-6） |
| **C2** | **逐通道都不等于**旧取整值（`Far`，> 1e-6） | 只要有一个通道回到取整值 ⇒ C2 红 |
| **C2b** | `LstItemTint.a == 1` | 原版 `a: 1`；改成 0.9 之类 ⇒ 红 |
| **C3** | 从 `AllianceMemberTab.cs` 源码里正则取 `QListBg, new Color(r,g,b,a)` 四个数，逐通道 = (1, 0.4745098, 0.09803922, 1) | 改回 `(1, 0.475, 0.098)` ⇒ C3 红 |
| **C4** | **g/b 都不等于**旧取整 `0.475` / `0.098` | 只把 g 改回、b 留着 ⇒ 仍红（两个通道都要 Far） |

- `Run()` 里已加 `GroupC_DropdownTints();`（在 `GroupB_AntiSelfProof();` 之后）。
- 🔴 **C 组与 A/B 组一样，目前仍未被任何宿主调用** —— 活状态表 `B141-W70-ASSERT-WIRE`（`:272`）那条还没关；接线由调度台做。
  挂法不变：`B141_W70_Assert.Run(); pass += B141_W70_Assert.Pass; fail += B141_W70_Assert.Fail;`。
- 判绿红：按行首 `B141W70 ✓` / `B141W70 ✗` 数（⛔ 别 `grep -c ✗`）。

### 八·五 验证

- `TMPDIR=/tmp/wf_b141_w85 bash 工具/typecheck.sh` ⇒ **运行时错误数 0 · 编辑器错误数 0**。
- 现读复核：`grep -n "0.4745098f"` = 1 处（`:2526`）· `QListBg, new Color(...)` 正则命中且四通道逐位正确 · `LstItemTint` 现读 = `(0.28627452, 0.9647059, 0.6862745)`。
- 旧取整值仍在 `:2413` / `:2417`（**故意留着** —— 那两行是「合上面的框」，属八·三·2 的待派活，不在本写手白名单）。

