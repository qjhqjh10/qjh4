# WL3 · A251-L3（`Referral Popup` · 78 节点）

> 现核时刻：**2026-10-13** 会话 · 写手 **WL3**（批次「清空 A 表」第四轮 · A251 最后一块）
> 代码根 = `d:/4/Unity/MyGame/Assets/CardPresentation/` · 资料根 = `d:/4/Unity/资料/`
> 本件**没跑 Unity**、**没动 git**、**没改 `d:/2/`**、**没碰 `工具/*`**（判据一律现读）
> ⚠️ **判据一律现读**：`menu_dump --relative` + 我自己写的原读副本（`prefab_read.py` → `recon_read.json`）
> + 每个 MB 的原文 + 反编译逐句 + `stringliteral.json` —— **本文里没有一格是推的**
> （「我们算的」那两处逐字标出，见 §五）

---

## 一、结论

- **窗建起来了**：`Shell/ReferralPopupWindow.cs`（🆕 新文件，**817 行**），
  原版 `Referral Popup` 的 **78 个节点**逐个照抄（名字 / 层级 / 兄弟序 / 矩形 / 字号 / `m_fontSizeBase` /
  自适应区间 / 对齐 / 颜色 / 九宫 / `ppuMul` / **出厂显隐**）。
  🔴 **78 个节点里 50 个是 `counter` 下的 `marker`**（= 原版 `referralCounterSteps` 数组长，现读 50）。
- 🔴 **它不在 `menus` 包里** —— prefab 在 **`bundle_generalgamewindows_assets_all`**（A251 那七扇里
  只有它与已裁「不建」的 `Debug Reactivate Event Window` 在这个包）。
- **注册进 `Shell/WindowsManager.cs`**：`PrefabRefReferralPopup`（`:1150`）+ `OpenReferralPopup(...)`（`:1323`）。
  ⛔ **L1 的 39 行 + L2 的 129 行一行没动**（我这一扇净 **+29 行 / 2 个 hunk**）。
- **可复现对账**：`_tmp_view/wl3/check_table.py`（纯 python）把 `Recon` 表与**现读**逐格比
  ⇒ **78 项对账 · 不符 0**（反向也查了：现读里有、表里没有的 = 0）。**脚本做过负例测试**
  （把 `marker#7` 挪 0.02px ⇒ 当场报出来，见 §五·③）。
- **断言**：`Editor/ShopScene.cs` 新加一节（**+225 行**：`ColNear` 帮手 17 行 + 主节 208 行），
  **79 处 `Check*` 调用点**（21 `CheckAt` / 20 `CheckTrue` / 3 `CheckHasKids` / 其余走 `Check`·`CheckNear`·
  `CheckHoverSwap`·`CheckPressedSwap`·`CheckNoMissingSwapArt`·`MenuDraw.CheckShadeRule`）。
  宿主 = **`ShopScene.Run`**（同 A251 另两块的既成宿主，见 §六·0）。
- **类型检查**：`TMPDIR=/tmp/wf_wl3 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（跑了 7 次，见 §十）。
- **缺素材：一张都不缺**（9 张图全在 `Resources/Art/` 里 —— 与 L1/L2 那两块不同，本扇**不需要**
  主对话再跑导入腿）。

**两条最有价值的现读结论**（都能反证，不只是照抄）：

1. 🔴 **`counter`（50 颗进度点）在原版成品里永远不画** —— 它 prefab 里 `m_IsActive = 0`，
   而**运行期没有任何代码把它打开**（三条证据见 §三①）。我们**照建**（节点数必须一致）+ **建成关着**
   = 与原版成品同一档。
2. 🔴 **`Refresh()` 的块结构**：`counter number` 的文本与 `error` 的清空**不在 `if (hasReferrer)` 里**、
   而在 `if (referButton != null)` 里 ⇒ **没有推荐人也会写计数串、也会清错误**（§三·③ 那张表）。

---

## 二、逐节点对照表（原版全路径 · 原版值 · 我们值 · 出处命令）

**出处命令**（全表由它出来；本扇根是**拉伸根** ⇒ `--relative` 与绝对框**同一套数**）：

```bash
python d:/4/Unity/工具/menu_dump.py bundle_generalgamewindows_assets_all "Referral Popup" --depth 12 --relative --md
```

**原读副本**（我自己的读法，独立于 `menu_dump`；走 prefab 的 `m_Children` 兄弟序）：

```bash
PYTHONIOENCODING=utf-8 python d:/4/_tmp_view/wl3/prefab_read.py     # → recon_read.json（78 条 → 断言 78）
```

| # | 原版路径（相对根） | 原版矩形（x1,y1→x2,y2） | 出厂 `act` | 我们建的值 | 关键字段（MB 实读） |
|---|---|---|---|---|---|
| 1 | `Referral Popup` | `0,0→1920,1080` | T | 同（拉伸根） | `type=1` · `windowsPlacement=15` · `closeOnESC=1` · **`extraScaleSmallScreen=1.35`** |
| 2 | `Menu Dark Background` | `-1327.30,-746.18→3247.30,1826.18` | T | 同（`CardArt.Solid()` + tint） | `m_Color (0,0,0,0.772549)` · 无 sprite |
| 3 | `window` | `364.03,287.11→1555.97,900.68` | T | 同 | 1191.9448×613.5716 |
| 4 | `window/Generic Window Red Background Big` | `503.93,287.11→1434.17,900.68` | T | 同（`Nine`） | `UI_Deck_Information_Back` 1100×701 · 九宫 42,363,655,81 · Sliced · ppuMul 1 |
| 5 | `window/Generic Close Button Orange` | `1361.78,287.11→1436.17,362.72` | T | 同（`Rect`+PA） | `UI_Button_Round_background` 237² · HL `…bt_yellow_hover` / P `…_pressed` |
| 6 | `…/Generic Close Button Orange/Background` | `1369.93,295.09→1426.79,353.22` | T | 同 | `40k_general_bt_yellow` 71² · PA |
| 7 | `…/Generic Close Button Orange/Icon` | 同上（**同框**） | T | 同 | `40k_general_bt_yellow_close` 71² · PA |
| 8 | `window/content` | `526.04,327.79→1397.11,839.83` | T | 同 | VLG spacing 5 · align 1 · ctrlW 1 / ctrlH 0 |
| 9 | `window/content/Referral Title` | `526.04,327.79→1397.11,380.93` | T | 同（`TextBox`） | fs **40** · base **45.2** · auto[3~40] · Center/Middle · 折行 1 · 白 |
| 10 | `window/content/Input View` | `526.04,385.93→1397.11,560.93` | T | 同 | VLG spacing 0 · align 1 · ctrlW 0 / ctrlH 0 · expandW 1 / expandH 1 |
| 11 | `…/Input View/Label` | `560.37,385.93→1362.79,438.16` | T | 同 | fs 35 · base 39 · auto[3~35] · Center/Middle |
| 12 | `…/Input View/input` | `560.37,438.16→1362.79,483.16` | T | 同（`Nine`） | `40K_dropdown_bg` 119×102 · 九宫 23,20,23,20 · tint **(1,0.3173,0.00392,1)** · **ppuMul 1.64** |
| 13 | `…/input/Text Area` | `570.37,445.16→1352.79,477.16` | T | 同（`Node`） | `RectMask2D` · **`m_Padding = (-8,-5,-8,-5)`**（负=往外扩）· softness 0 |
| 14 | `…/Text Area/Placeholder` | 同上（同框） | T | 同 | fs 40 · base 14 · auto[18~40] · **折行 0** · 色 `(0.972,0.972,0.972,0.5)` |
| 15 | `…/Text Area/Text` | 同上（同框） | T | 同 | fs 45 · base 14 · auto[18~45] · **折行 3** · 白 |
| 16 | `…/Input View/spacing` | `781.94,483.16→1141.21,496.13` | T | 同（空节点） | 无组件 |
| 17 | `…/Input View/Generic Simplified UI Button` | `871.53,496.13→1051.62,544.86` | T | 同 | `UI_Button_Mulligan` 410×124 · **Simple+PA** · `Button` trans=2（HL/P 两张）· 同件还叠一颗 `EverguildButton`（trans=1 ColorTint） |
| 18 | `…/Generic Simplified UI Button/Button Text` | `877.74,498.64→1045.59,542.37` | T | 同 | fs 26.35 · base 12 · auto[10~30] · Center/**Capline** · **折行 0** · **ARF 宽控高 3.83864** |
| 19 | `…/Input View/error` | `757.27,544.86→1165.89,569.07` | T | 同 | fs 16.75 · base 36 · auto[3~35] · 色 `(0.8208,0.04259,0.04259,1)` |
| 20 | `window/content/Referred View` | `526.04,588.43→1397.11,763.43` | **F** | 同（建成关着） | HLG spacing **14.92** · align 4 · ctrlW 1 / ctrlH 0 · expandW 0 / expandH 1 |
| 21 | `…/Referred View/Label` | `770.87,647.08→982.01,719.78` | T | 同（**序列化值**，见 §五·②） | 锚(0,1) piv(0,0.5) sd **211.14 × 72.697** ap(244.829,−95) · fs 35 · base 39 |
| 22 | `…/Referred View/Name` | `996.93,647.08→1152.28,719.78` | T | 同（**序列化值**） | 锚(0,1) piv(0,0.5) sd **155.35 × 72.697** ap(470.889,−95) · fs 35 · base 36 |
| 23 | `window/content/Divisor line members` | `526.04,565.93→1397.11,569.70` | T | 同（`Nine`） | `40k_Separator Fade Sides Horizontal` 128×4 · 九宫 63,0,63,0 · tint `(0.8745,0.5524,0.2863,1)` · **ppuMul 1.64** · PA |
| 24 | `window/content/spacing (1)` | `526.04,574.70→1397.11,587.67` | T | 同（空节点） | 无组件 |
| 25 | `window/content/Title` | `526.04,592.67→1397.11,644.67` | T | 同 | fs **39.95** · base **45.2** · auto[3~40] |
| 26 | `window/content/Descripton` | `526.04,649.67→1397.11,789.67` | T | 同（**纵向对齐差一层**，见 §八⑤） | fs 32.2 · base 39 · auto[3~35] · Center/**Top(256)** |
| 27 | `window/content/counter number` | `526.04,794.67→1397.11,820.17` | T | 同 | fs 26.9 · base 36 · auto[3~32] · 色 `(1,1,1,0.6863)` |
| 28 | `window/content/counter` | `532.08,481.45→1391.08,543.64` | **F** | 同（**建成关着**） | HLG spacing 0 · align 4 · ctrlW 0 / ctrlH 0 · expandW 1 / expandH 1 |
| 29–78 | `…/counter/marker` ×**50** | `535.67+17.18i, 507.54→545.67+17.18i, 517.54` | T | 同（**一条算式**，见 §五·①） | `Image`（**无 sprite** · `m_Color (0,0,0,1)`）+ **`Outline`**（`m_EffectColor` 出厂 = `achievedColor` · `m_EffectDistance (2.5,2.5)` · `m_UseGraphicAlpha 1`） |

**窗口字段 → 节点**（`dump.cs` 的偏移表 + 逐个 pid 反查，**8/8 对得上**）：

| 偏移 | 字段（类型） | 节点 |
|---|---|---|
| `+0x70` | `referralCounter` (EverguildTextMeshPro) | `counter number` |
| `+0x78` | `referralCounterSteps` (**Outline[50]**) | `counter` 下 50 颗 `marker` |
| `+0x80` | `inputView` (GameObject) | `Input View` |
| `+0x88` | `referredView` (GameObject) | `Referred View` |
| `+0x90` | `referredPlayerName` | `Referred View/Name` |
| `+0x98` | `referredPlayerInput` (EverguildInputField) | `Input View/input` |
| `+0xA0` | `errorMessage` | `Input View/error` |
| `+0xA8` | `referButton` (EverguildButton) | `Input View/Generic Simplified UI Button` |
| `+0xB0` / `+0xC0` | `defaultColor` / `achievedColor` | ——（`(0.91764706,0.76862746,0.48235294,1)` / `(0.29293314,0.8396226,0.19406372,1)`） |

⚠️ **`referralCounterSteps` 数组长 50 是我数出来的**：`MonoBehaviour_8365794629712173860.json` 里那个
数组正好 50 个 PPtr（`prefab_read.py` 走树也数出 50 颗 `marker`）—— **两条路同值**。

---

## 三、状态 → 参数 表

### ① `counter`（50 颗 `marker` 的容器）—— **原版成品里永远不画**

| 证据 | 内容 | 出处 |
|---|---|---|
| ① 序列化 | prefab 里 `counter` 的 `m_IsActive = **0**` | `menu_dump` 印 `INACT`/`F`（两处一致）· `prefab_read.py` 印 `act=0` |
| ② 代码 | 窗口的**十个**序列化字段**没有一个是它**；`Refresh()` 只 `SetActive` 了 `Input View`/`Referred View` 两扇 | `dump.cs` 偏移表 · `ReferralPopupWindow__Refresh.c` 逐句 |
| ③ 引用 | 全包按 pid 搜：它的 GO（`-7485178439993369820`）除**它自己的** `HorizontalLayoutGroup` 与 AssetBundle 清单外**零引用**；那棵树上一个 `Animator`/`Animation` 都没有 | `grep -rl` 全包 · 逐 GO 读 `m_Component` |

⇒ **我们的处置 = 照原版**：50 颗**全部建出来**（节点数必须与 prefab 一致）+ `counter` **建成关着**
（同 `debug_buttons` / 那扇已裁「不建」的窗的先例：**不可达 ⇒ 照原版不画**）。
`Refresh()` 里那 50 次 `Outline.effectColor` 赋值**照样做**（原版也照样做 —— 只是画不出来）。

### ② 显隐/颜色：**逐档表**（原版 `Refresh()` 与 `Start()` 的每一跳都有判据）

| 件 | prefab 出厂 | **no-data（没有 `ReferralManager`，= 本地）** | **管理活着 + 无推荐人** | **管理活着 + 有推荐人** | 判据 |
|---|---|---|---|---|---|
| `Input View` | **T** | **T** | **T** | **F** | `SetActive(!has)` |
| `Referred View` | **F** | **F** | **F** | **T** | `SetActive(has)` |
| `Referred View/Name` | `"José Bezerra"` | 原样 | 原样 | `referrer.Name`（`+0x18`） | `SetText` 在 `if (has)` 里 |
| `counter number` | `"You have collected {0} referral rewards!"` | **原样**（一个字都不写） | `Format(词条, count)` | 同左 | 那一跳在 `if (referButton != null)` 里、**不在 `if (has)` 里** |
| `error` | `"AN ERROR HAS OCURRED"` | **原样** | **清成空串**（`SetText("")` —— 字面量从 `stringliteral.json` 读出来 = `""`） | 同左 | 同上 |
| `referButton` | `interactable = 1` | 1 | `!has` = 1 | `!has` = **0** | `set_interactable(!has)` |
| `marker[i]` 描边 | **全 `achievedColor`**（50/50 实读） | 原样 | `i < count` ⇒ `achievedColor`，否则 `defaultColor` | 同左 | `Refresh()` 尾段那个 `for` |
| `counter` | **F** | **F** | **F** | **F** | **没有代码开它**（见上） |

**`error` 的另外两条路**（都不在 no-data 档）：

| 触发 | 效果 | 判据（逐句） |
|---|---|---|
| 输入框的字变了 | **清空** `error` | `Start()`：`referredPlayerInput.m_OnValueChanged`（`TMP_InputField +0x1D0`，`UnityEvent<string>`）→ `<Start>b__10_0(string _)`（参数丢弃） |
| 提交失败 | `error = GetTranslation("CustomErrors/" + err)` **＋** `interactable = true` | `OnSetReferrer()` → 失败回调 `<OnSetReferrer>b__13_0`；前缀 `"CustomErrors/"` 从 `stringliteral.json` 读出（RVA `0x425CCA8`） |
| 提交 | `interactable = false` 先上 | `OnSetReferrer()` 首句 |

⚠️ **两个 `___ctor` 的委托类名有一处与签名对不上**（如实记，见 §八②）：我最终按**事件字段的类型**认
—— `SourceDelegate.Invoke()` **无参** ⇒ 只能挂 `OnSetReferrer()`（无参）；
`m_OnValueChanged` 是 `UnityEvent<string>` ⇒ 只能挂 `b__10_0(string)`。

### ③ 计数串那两条词条

| 分支 | 词条 key | 正文 | 来源 |
|---|---|---|---|
| `count < max` | `MenuShop/referral/counter` | `"You have collected {0} referral rewards!"`（**带 `{0}`**） | key 从 `stringliteral.json` 读出（RVA `0x42D2CE0`）；正文 = prefab 出厂串 |
| `count >= max` | `MenuShop/referral/counterMaximum` | 🔴 **拿不到**（词条表在远端 CCD） | key 读出（`0x42D2DE0`）⇒ 我们那一档**出声 + 退回上面那条**（⛔ 不假装知道） |

### ④ **本扇 6 个 `Localize` 的 term key**（逐颗实读，`Localize.mTerm`）

| 节点 | term |
|---|---|
| `Referral Title` | `MenuShop/referral/mainTitle` |
| `Input View/Label` | `MenuShop/referral/inputLabel` |
| `Input View/Generic Simplified UI Button/Button Text` | `MainMenu/General/Confirm` |
| `Referred View/Label` | `MenuShop/referral/referrerLabel` |
| `Title` | `MenuShop/referral/title` |
| `Descripton` | `MenuShop/referral/description` |

🔴 **prefab 里印着的那一串是【俄文】**（`Реферальная программа` 等）—— 那是**资产里的真字符串**，
原版运行期由 I2 词条覆盖、而**词条表在远端 CCD**（本地一个 value 都没有）⇒ 我们**照抄资产原串**。
✅ **俄文字形画得出来**：字体 `Resources/Fonts/NotoSerifCJK-Regular SDF` 的 `m_AtlasPopulationMode = 1`
（**Dynamic**，字符表为空、按需光栅化），源字体 `Assets/CardPresentation/Fonts/NotoSerifCJK-Regular.ttf`
的 `cmap` 里 **U+041F / U+0435 / U+4E2D 都在**（本件现读）。

---

## 四、注册改动（`WindowsManager.cs:行`）

`git diff --numstat` ⇒ **+158 / −3**（**其中 `:316`/`:318` 那两个 hunk 的 −3/+7 不是我的** ——
那是 L2 报告 §十·① 记过的那一笔，今天仍在）。**我这一扇净 +29 行 / 2 个 hunk**：

| 位置 | 内容 |
|---|---|
| `Shell/WindowsManager.cs:1144-1150`（hunk `@@ -1110,0 +1118,34 @@` 的尾巴） | `public const string PrefabRefReferralPopup = "Referral Popup";` —— 挨着 L2 那四条 `PrefabRef*`，注释写明「**它的 prefab 不在 `menus` 包里**」与「原版入口是 `ReferralContainer.OpenPopup()`」 |
| `Shell/WindowsManager.cs:1310-1329`（新 hunk） | `public static ReferralPopupWindow OpenReferralPopup(ReferralView? view = null)` —— 走 `OpenByRef`，随后 `win.SetReferral(view.Value)`（**复用那一支**也要把新参数喂进去：`TryOpen` 在 `State == Open` 时照原版早退，A217②） |

🔴 **只注册了我这一扇** —— L1 那两条（`PrefabRefBaseOfferPopup` + `OpenBaseOfferPopup`）与 L2 的四条
**一行没动**（`git diff` 里它们的位置与内容原样）。
⚠️ **新建那一支会多建一遍**（`Create` 的 `Open()` 建一次、`SetReferral` 再铺一次）—— 同 L2 那四扇如实记的那一条；
本扇树大（78 节点 / 250 个 quad），但**只在新建那一支**发生，且**没有** `BaseOfferPopup` 那种
「内容没变 ⇒ 不重建」的守卫（参数是调用方每次现给的）。

---

## 五、可复现对账（命令 + 对账项数 / 不符数 + 哪几格是「我们算的」）

```bash
# ① 对账（纯 python、不跑 Unity）：`Recon` 表 ↔ 现读，逐格比
PYTHONIOENCODING=utf-8 python d:/4/_tmp_view/wl3/check_table.py
#   ⇒ === Referral Popup  表里 78 条 · menu_dump 现读 78 条（白名单 2 条走原读）· 对账项 78 · **不符 0**
#      ------------------------------------------------------------------
#      合计：对账项 78 · **不符 0**

# ② 原读副本（我自己的读法，独立于 menu_dump；走 `m_Children` 兄弟序 + 断言 78）
PYTHONIOENCODING=utf-8 python d:/4/_tmp_view/wl3/prefab_read.py

# ③ 整棵树
python d:/4/Unity/工具/menu_dump.py bundle_generalgamewindows_assets_all "Referral Popup" --depth 12 --relative --md
```

**脚本的三个必需件**（缺一个就会静默放过）：

1. **同名兄弟要带 `#k`** —— `counter` 下有 **50 个同名的 `marker`**；不编号的话
   `rows[path]` 会被后一个**静默覆盖**，50 格只剩 1 格可比。
2. **反向也查**（现读里有、表里没有的路径）—— 否则「表里少抄一整棵子树」照样绿。
3. **负例测试过**：把 `marker#7` 的 `x1` 挪 **0.02px**（> 容差 0.011）⇒ 脚本当场报出那一条 ✓
   （所以「不符 0」不是「比了个空气」）。

### 「我们算的、不是逐格抄」的两格（铁律 3）

1. 🔴 **`marker` 那 50 格由一条算式生成**：50 个 `m_AnchoredPosition.x` 逐位读出来是
   **`8.59 + 17.18 i`**（float32 序列化噪声最大 **0.00027px**，= 0.003 个像素，看不见）
   ⇒ `ReferralPopupWindow.MarkerRect(i)` 用 `MarkerX0 = 535.67` / `MarkerDx = 17.18` 生成，
   ⛔ **没有抄 50 行**。**对账脚本把那 50 格逐格比过**（0 不符）。
   ⚠️ 这也是全文件**唯一**用算式代替逐格的地方 —— 改版式时**从这一条看起**。
   （`counter` 自己那一格**不是**算的：它是 prefab 序列化值算出来的绝对框，且与 `menu_dump` 印的
   `532.08,481.45` **逐位相同**，可反证。）
2. 🔴 **`Referred View/Label` 与 `…/Name` 走的是「原读」而不是 `menu_dump`**：
   那颗 HLG **关着** ⇒ uGUI 不跑它，而 `childControlWidth = 1` ⇒ 宽是**布局写的**、
   要 TMP 首选宽度（Unity 的字体度量）⇒ `menu_dump` 在那一格**印的是 0.00 宽**（模拟跑了一遍、量不出字宽）。
   我取 **prefab 序列化值** → `(770.87,647.08,982.01,719.78)` / `(996.93,647.08,1152.28,719.78)`
   （公式 = uGUI 的 `pivot 点 = 锚点矩形左下 + 锚高×pivot + ap`，与 `RankedRewardEventWindow.CardChild`
   同一份推导，**逐位可验**）。对账脚本对这 2 条**换源比**（`raw_rows()` → `uGUI_child()`）。

---

## 六、断言清单（断什么 · 期望值来源 · 改坏法 · 落点）

### 六·0 宿主怎么定的（现读，不是猜）

`Editor/ShopScene.cs` —— 与 **L1/L2 同一个宿主**（A251 既成宿主：`BoosterInfoPopup` 26 处 /
`OfferContainer` 63 处）。**我加在 L2 那一节之后**（`// ---------------- 收尾 ----------------` 之前），
**L1 的 66 处 + L2 的 104 处一行没动**（`git diff` 只有 3 个 hunk：`:480,+21` 与 `:785,+17` 两个帮手、
`:3667,+700` 那一大段 —— 里面同时含 L1/L2 的与我的，git 把相邻新增并成了一个 hunk）。

### 六·1 分组（79 处调用点）

| 组 | 断什么 | 期望值来源（**都不是自证**） | 改坏法 |
|---|---|---|---|
| **窗参 + 前提**（8） | `type=1` / `placement=15` / `closeOnEsc=1` / **`extra=1.35`** / 根名 / 注册键 / 根上没烤 scaler | MB 原文实读 | `extra` 写成 1.0 ⇒ 红（**同批另四扇是 1.0 —— 这是本扇判别力最强的一条**） |
| **层级**（10） | 根 / `window` / `content` / `Input View` 的**直接子件名 + 兄弟序**（`KidNames`）· `Text Area`·关窗钮·确认钮的直属子件（`CheckHasKids`） | prefab `m_Children` 现读 | `counter` 挂回 `content` 别处 / `Background` 挂成关窗钮的兄弟 ⇒ 红 |
| **50 颗 marker**（4） | 子件数 **50** · 全叫 `marker` · 每颗**恰好 4 份 `Outline`** · 副本合计 **200** | `referralCounterSteps` 数组长 + `Outline` 组件实读 | 少建 1 颗 / 只画 1 份 Outline ⇒ 红 |
| **出厂显隐**（7 + 2 反例） | `Input View` 开 · `Referred View` 关 · **`counter` 关** · 确认钮命中区**开着** · `error`/`counter number` = prefab 原文 · 文本 = U+200B | prefab `act` + 反编译明文 | 把「关着」写成「不建」⇒ 红；`Title`/`Input View` 那两条反例 ⇒「整棵树没建」蒙不过去 |
| **几何**（21 `CheckAt`，冻结字面量） | 18 个绝对框 + `marker` **#0 / #49** + `Referred View` 两个孩子 | `menu_dump` 现读的 px（后两条走原读） | 改任一常量 ⇒ 红（**断言不读** `Recon` / 那几个 `…R` 字段）+ 步骤改了 ⇒ #49 红 |
| **压暗层不变量**（1） | 命中 quad 的档 == 视觉压暗层那颗的档 且 `< QHit` | `MenuDraw.CheckShadeRule`（唯一定义处） | 把 `ShadeHit` 的 `qShade` 换成 `QHit−1` 这类派生值 ⇒ 红 |
| **两态（有推荐人）**（11） | `Referred View` 开 / `Input View` 关（**互斥**）· `Name` = `referrer.Name` · `interactable=false` + **命中区也关** · 计数串 `Format(词条,3)` · `error` 清空 · **`counter` 仍关** | 反编译 `Refresh()` 逐句 | 只断一个方向 / 让 `counter` 跟着开 ⇒ 红 |
| **第三档（无推荐人但管理活着）**（6） | `Input View` 开 / `Referred View` 关 · 可填 · 计数串写 0 · `error` 清空 · 连 `#0` 都是 `defaultColor` · 输入框显示 = `InputText` | 反编译那一支的**块结构** | 把计数/清错那两跳挪进 `if (has)` ⇒ 红（这一档专抓它） |
| **marker 颜色边界**（4） | `#0`/`#2` = `achievedColor` · **`#3` = `defaultColor`** · `#49` = `defaultColor` | MB 两个 `Color` 字段实读 | 改成「全同色」/ 差一位（`<=` vs `<`）⇒ 红 |
| **交互**（3） | 前提 `interactable=false` → `SubmitReferrer()` → **true**（**真的状态转移**，不是同义反复） | 反编译 `OnSetReferrer()` 首句 + 失败回调 | 不恢复 / 一按就死 ⇒ 红 |
| **缺图 / 换图**（4） | `MissingArt == 0` · `MissingSwapArt == 0` · `MissingPressedArt == 0` · 悬停/按下**来回换得动** | 现查 `Resources/Art/` 九张全在 | 缺图不出声 / 换图不还原 ⇒ 红 |
| **复用 / 关过再开**（4） | 同键 ⇒ **同一实例** · 关过再开 ⇒ **新建** · 新建那一扇**回到出厂态** | 原版 `automaticallyLoadedWindows` + `CloseWindowCO` | 删复用缓存 / 让数据带过去 ⇒ 红 |

### 六·2 **派活必查行三条**（逐条自查）

| 检查 | 结果 |
|---|---|
| ① **断言自证 / 同义反复** | ✅ 期望值全是**冻结字面量**（`menu_dump` 现读的 px / MB 原文 / 反编译分支 / `stringliteral.json`），⛔ **一处都不读** `Recon` / `ShadeR` / `MarkerRect` |
| ② **弱断言分不出两种状态** | ✅ 凡有另一态的地方**两档都断**：`Input View` ↔ `Referred View`（互斥）· `interactable` true/false（**连命中区的开/关一起断**）· marker `#2`/`#3` 那一对 · `error` 原文 ↔ 空串 · 计数串原文 ↔ 格式化 · **三档**（无管理 / 无推荐人 / 有推荐人）· 复用 ↔ 新建 |
| ③ **`!RectOfUnion` / 「一个 quad 都没有」式断言** | ✅ **一条都没有** —— 「关着」一律走 `activeSelf`，且**逐条配了反例**（`Title` 开着 / 确认钮命中区开着） |
| **灭自证**（防两边一起改回去） | ✅ ①**`counter` 有数据也不开**（改实现去开它 ⇒ 红，这条断言结构上不可能与实现一起变绿）；②`interactable=false` 那一档**同时断命中区关掉**；③`#2`/`#3` 的**边界对**（只改阈值的 `<=`→`<` 也会红） |
| 分层用**渲染队列**不是 z | ✅ 本窗自成一档 **3348–3361**；压暗层命中区与视觉**同档**（`ShadeRuleOk` 要求）、吸收层 = `QHit−1 = 3360`、内容命中区 = `3361` —— **三段互不同档**（同档时谁吃到命中退化成枚举顺序） |
| 「新加一层就配一条该藏的时候藏住了吗」 | ✅ `counter`（新加的那一层）/ `Referred View` 的**关**都逐条断；反过来「建出来了」也逐条断（`CounterNode != null` / 50 颗） |
| 断言**落在正确的 `*Scene.Run`** | ✅ `ShopScene.Run`（§六·0） |

⚠️ **本节本次【没有跑】**（铁律 12：A 表清零前中途不跑 Unity 自检）—— **如实记**，⛔ 不当「已验过」。
**建议怎么跑**（同步点）：`ShopScene.Run` 一条即可（**只动了一个宿主**）：耗时 ≈ **45 秒**。
⚠️ 判绿红看「`=== 合计：N 通过 / M 失败 ===`」那一行（本宿主**不用** `✗` 标记失败）。

---

## 七、缺的素材（逐张：原版 sprite 名 · 源在哪 · 我们缺不缺）

| # | 原版 sprite 名 | 用在哪 | 我们 | 处置 |
|---|---|---|---|---|
| 1 | `UI_Deck_Information_Back`（1100×701 · 九宫 42,363,655,81） | 窗体底 | ✅ `Resources/Art/ui_deck/` | 正常画 |
| 2 | `UI_Button_Round_background`（237²） | 关窗钮底 | ✅ `Resources/Art/ui/` | 正常画 |
| 3 | `40k_general_bt_yellow`（71²） | 关窗钮 `Background` | ✅ `Resources/Art/ui/` | 正常画 |
| 4 | `40k_general_bt_yellow_close`（71²） | 关窗钮 `Icon` | ✅ `Resources/Art/ui_deck/` | 正常画 |
| 5 | `40k_general_bt_yellow_hover` / `_pressed` | 关窗钮悬停/按下 | ✅ `Resources/Art/ui_menu/` | 正常画 |
| 6 | `40K_dropdown_bg`（119×102 · 九宫 23,20,23,20 · ppuMul 1.64） | `input` 底 | ✅ `Resources/Art/ui_deck/` | 正常画 |
| 7 | `40k_Separator Fade Sides Horizontal`（128×4 · 九宫 63,0,63,0 · ppuMul 1.64） | `Divisor line members` | ✅ `Resources/Art/ui_menu/`（切片名空格换下划线） | 正常画 |
| 8 | `UI_Button_Mulligan`（410×124） | 确认钮底 | ✅ `Resources/Art/ui/` | 正常画 |
| 9 | `UI_Button_Mulligan_hover` / `_Pressed` | 确认钮悬停/按下 | ✅ `Resources/Art/ui/` | 正常画 |

⇒ 🔴 **本扇一张都不缺**（断言里钉了 `MissingArt == 0` / `MissingSwapArt == 0` / `MissingPressedArt == 0`）。
**不需要主对话再跑 `工具/import_original_art.py` 的腿**（与 L1/L2 那两件不同）。
> ⛔ 我**没有**动 `工具/*`、也**没有**往 `Resources/` 里塞文件（都在白名单外）。

---

## 八、没查清 / 没做的（⛔ 不许猜）

1. **`counter >= max` 那条词条（`MenuShop/referral/counterMaximum`）的正文** —— key 读出来了
   （`stringliteral.json` RVA `0x42D2DE0`），但**正文在远端 CCD** ⇒ 那一档**照抄不到**：
   我们**出声 + 退回带 `{0}` 那条**（`Apply()` 里那条 `LogWarning`，只响一次）。
   ⚠️ 顺带：**`MaxRewards` 的原值也读不到**（远端 LiveOps 配置的 `+0x18`）⇒ 缺省取 **50**（= 进度点颗数）
   —— **这是我们挑的、不是原版的做法**，如实标在 `ReferralView.MaxRewards` 的注释里。
2. **`Start()` 那两条监听的「委托类名 vs 签名」对不上**：反编译把第二个 `___ctor` 印成
   `EverguildDropfield.DropDelegate`（它的 `Invoke` 收 `PointerEventData`），而它注册的事件是
   `UnityEvent<bool>`、挂的方法体是 `b__10_0(string)` —— **三处签名互不相同**。
   我按**事件字段的类型**认（`TMP_InputField.m_OnValueChanged` 是 `UnityEvent<string>`，
   与 `b__10_0(string)` 严丝合缝；`SourceDelegate.Invoke()` 无参 ⇒ 只能挂 `OnSetReferrer()`），
   **实测没跑**（原版跑不起来那扇窗）⇒ **如实标「认法有依据、但没跑到实况**」。
3. **`ReferralManager` 的取值语义**：`GetReferrer()` / `GetReferrals().Count(!IsRedeemed)` /
   `referrer.Name`（`ReferralEntry +0x18`）三处都是**服务端**，本地一条都没有 ⇒ 我们的
   `ReferralView` 是**等价物、不是原件**。⚠️ `ReferralEntry.IsRedeemed` 那个 predicate 逐字读出来了
   （`ReferralPopupWindow.<>c.<Refresh>b__12_0` = `!x.IsRedeemed`）✓。
4. **`referButton` 上叠了两颗 `Selectable`**：`Button`（`trans = 2` SpriteSwap，**HL/P 两张实读**）＋
   `EverguildButton`（`trans = 1` **ColorTint**）挂在**同一件**上。原版哪一颗生效**我没查到判据**
   ⇒ 照**同族既成做法**（`GenericOptionsPanel` 的 `Template` 是**同一颗资产、同一套组件**，
   那边接的是换图）只接**换图**，如实记。⚠️ 这一条**将来该补**（判据 = 两颗 `Selectable` 在
   uGUI 里的 `DoStateTransition` 派发序）。
5. **`Descripton` 的 `m_VerticalAlignment = 256 (Top)` 我们复刻不了** —— 本仓 `Label` **只有**
   `SetAlignLeft`/`AlignLeftOn`/`AlignRightOn`（**没有纵向对齐那一层**）⇒ 建成**居中**。
   140px 高的框里那三段字，Top 与 Middle 看得出差别 ⇒ **这是一处可见偏离**（⛔ 没假装做到了）。
   要做的话是一处公共件扩参（`Label` / `MenuDraw.Text`），**不在本件白名单内**。
6. **输入框的「不限长」我们给成了 64** —— 原版 `EverguildInputField.m_CharacterLimit = **0**`（不限长），
   而本壳的 `PointerLayer.BeginText` **要一个上限** ⇒ 取 **64** 并**出声**（`LogWarning` 里明说）。
   **我们挑的**，如实标。
7. **`ReferralContainer.OpenPopup()` 那条入口链我们没建** —— 反编译读出来的：
   `LiveOpsAssetUtility.GetComponentReference(…, 0xc)` → `WindowsManager.OpenWindow(manager, 窗口引用, payload)`
   ⇒ **原版确实有一个本地可达的入口**（一个 LiveOps 容器上的按钮）。⛔ 我们**只提供「怎么开」**，
   **没编入口**（`ReferralContainer` 那棵树是另一件活）。
8. **`CanvasGroup`（挂在 `window` 上）我们没建**：实读 `m_Alpha = 1` · `interactable/blocksRaycasts = true` ·
   `ignoreParentGroups = false` ⇒ **出厂是「全通」那一档**，本仓也没有窗口淡入淡出那一层 ⇒ 不建、不影响可观测结果（如实记）。
9. **`BackgroundOverDrawController`（挂在 `Menu Dark Background` 上）**：那是**全局栈**
   （同一时刻只画最上面那一个整屏底），**它不写 `m_Color`** ⇒ 我们没实现那条栈（同 L2 §十·④ 记过的那一条）。

---

## 九、顺手发现（⛔ 别自己顺手改）

1. 🔴 **`Shell/ProfileTab.cs` 与 `Shell/PlayerProfileWindow.cs` 今天有别的写手在动**（见 §十）——
   我这一轮**没碰**它们。⚠️ 两次类型检查拿到的是**他们的半成品**（`error CS0103: 当前上下文中不存在名称"Clip"`），
   按纪律**不碰、继续**。**请调度台核一下那个文件的最终归属**（形状像是「三兄弟」`Clip`/`ClipSoftness`
   从 `MenuWindowBase` 上移到 `GameWindow` 那一件 —— 我的新文件里**没有**引用那两个字段）。
2. ⚠️ **原版 `Referral Popup` 的 50 颗 `marker` 是「死资产」**（§三①）——
   同族还有多少扇窗是这种形状（**prefab 里有节点、运行期没有任何代码打开它**）值得单独盘一次：
   判据法 = ① `menu_dump` 看 `m_IsActive` ② 窗口类字段表里有没有它 ③ 全包按 pid 搜引用。
   **本件只报，没有去盘全库。**
3. ⚠️ **`MenuDraw.Absorb` 的档恒 = `QHit − 1`** —— 本窗的 `QHit = 3361` ⇒ 吸收层落在 **3360**。
   我把 marker 那一档排在 **3359** 就是为了**避开它**（撞上就是「点窗内空白处有时会关窗」那一族）。
   将来谁把内容层的档排到 `QHit − 1`，症状是**静默的**（同 L1 报告 §十·④ 那条）。
4. ⚠️ **本扇的 `Menu Dark Background` 是「以画布中心为中心」**（`-1327.30,-746.18→3247.30,1826.18`，
   与 `BaseOfferPopup`/`RankedBoost` 那两扇**同值**），而同族的 `GenericOptionsPanel` /
   `Member Options Panel` 那两扇是**以面板中心为中心**、`m_Color` 还是 `a = 0` ——
   **三档逐扇不同**（铁律 5·c）。⛔ 别按名字去推。
5. ⚠️ **`CloseR` 比 `window` 还高出去 0** —— 逐位核过：`1361.78,287.11→1436.17,362.72`，
   `window` 的右上角是 `(1555.97, 287.11)` ⇒ 关窗钮**完全落在 `window` 内**（与 `BaseOfferPopup` 那扇
   跑到窗外的 `1487.08,159.85→1561.47,235.45` **不是同一个位置**）。⛔ 别把两扇的坐标互推。
6. ⚠️ **`40k_Separator Fade Sides Horizontal` 的切片名是「下划线版」**（原 sprite 名里有空格）——
   `CardArt.MenuUi` 那条路要求「切片名里的空格换成下划线」，与 `MenuUi` 的注释一致；
   我在代码里存的是**下划线版**、注释里记的是**原 sprite 名**。

---

## 十、类型检查结果

```text
$ TMPDIR=/tmp/wf_wl3 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

**跑了 7 次**（独立 `TMPDIR`，按铁律 13·3），其中 **2 次拿到的是别人的半成品**（**不碰、如实记**）：

| 第几次 | 结果 |
|---|---|
| 1 | **0 / 0**（只加了 `Shell/ReferralPopupWindow.cs`） |
| 2 | **0 / 0**（加完注册） |
| 3 | **0 / 0**（加完断言） |
| 4 | 运行时 **6** 个，**全部在 `Shell/PlayerProfileWindow.cs`**（`error CS0103: Clip / ClipSoftness`）—— 不是我的文件 ⇒ 按纪律**不碰** |
| 5 | 运行时 **1** 个，在 `Shell/ProfileTab.cs`（同一个 `Clip`）—— 同上 |
| 6 | **0 / 0**（对方改完了） |
| 7 | **0 / 0** ✅（收尾，`marker` 变量改名之后） |

⚠️ **本节的断言本次【一条都没跑】**（铁律 12）—— 见 §六·1 末段的「建议怎么跑」。

---

## 附：本件的产物（下个会话可直接跑）

```bash
python d:/4/_tmp_view/wl3/prefab_read.py        # 原读副本 → recon_read.json（78 条）
PYTHONIOENCODING=utf-8 python d:/4/_tmp_view/wl3/check_table.py   # 对账：78 项 · 不符 0
python d:/4/Unity/工具/menu_dump.py bundle_generalgamewindows_assets_all "Referral Popup" --depth 12 --relative --md
```

代码改动（`git diff --numstat`）：

| 文件 | 动作 | 行数 |
|---|---|---|
| `Shell/ReferralPopupWindow.cs` | 🆕 新建 | **817 行**（`??` 未跟踪） |
| `Shell/WindowsManager.cs` | ✏️ `:1144-1150` + `:1310-1329` | **+29 / −0**（本扇；该文件累计 +158/−3，含 L1/L2 与那笔非我的 −3/+7） |
| `Editor/ShopScene.cs` | ✏️ `:785` 帮手（17 行）+ `:4159-4364` 主节（208 行） | **+225 / −0**（该文件累计 +738/−0） |

**行尾**：三个文件**改动后仍是纯 LF**（`CRLF 0`；`ReferralPopupWindow.cs` 817 行 LF）——
`git diff --numstat` 的删除数 **0** = **没有整篇重写**（⛔ 全程用 Edit / python `wb`，没用过 `sed -i`）。
