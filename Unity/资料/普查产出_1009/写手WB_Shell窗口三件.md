# 写手 WB —— Shell 窗口三件（A265 · A273 · A229）· 2026-10-09

## 〇、一句话

三件都落了盘、各配了**能红**的断言：**A265**（收藏窗 `Back` 文字用自己的 132.86×48.24，且查实那个数**是序列化的、不是 ARF 算的**）·
**A273**（收件箱条目底图接上 SpriteSwap 悬停）· **A229**（`basis == parent` 守卫 —— ⚠️ **PracticeModePopup 那半的判据指向不存在的东西**，真身是同文件的 `HitOn`，理由见 §3·2）。
类型检查**两遍**：第一遍报 4 条错（**全在我自己新写的 RewardsScene 块里**：`CheckText` 这个 helper 只在 CollectionScene 有），改完 **0/0**。
⛔ 没跑 Unity（无权限）⇒ 断言**一条都没实跑过**，由调度台在同步点跑。

---

## 一、A265 —— 收藏窗 `Back` 的 `Button Text` 用整颗钮的矩形

### 1·1 判据（**现读**，⛔ 不是抄简报）

```
python 工具/menu_dump.py bundle_menus_assets_all "Collection Menu Variant" --depth 12
  …  4    Close Button   192.2  83.4  342.2  143.4  150.00  60.00
  …  5      Button Text  200.5  89.3  333.4  137.5  132.86  48.24  'Back' 字号=40.0 auto[10.0~40.0] 对齐=Center/Capline 折行=0
```
⇒ 与简报一致：**132.86 × 48.24**（那条 dump 行里 `Button Text` 是 `Close Button` 的**子节点**）。

### 1·2 🔴 简报里「未查清的一半」——**已查实 = 序列化的，不是 ARF 算的**

那颗节点上**确实挂着** `AspectRatioFitter`（`m_AspectMode=1` 宽控高 · `m_AspectRatio=3.8386404514312744`）——
**但它是 `m_Enabled = 0`** ⇒ `AspectRatioFitter.UpdateRect` 头一句 `if (!IsActive()) return;` **一个字段都不写**
（这也是 `menu_dump` **没在那一行印 `⚙ARF` 标记**的原因）。

真值完全由 RectTransform 自己的字段解释（父 = 150×60）：
`m_AnchorMin = (0.035511188, 0.099)` · `m_AnchorMax = (0.961261868, 0.903)` ·
`m_SizeDelta = (-6.000002, 0)` · `m_AnchoredPosition = (0, 0)` · `m_Pivot = (0.5,0.5)`
⇒ 宽 `0.92575068 × 150 − 6 = **132.86**`、高 `0.804 × 60 + 0 = **48.24**`（**两个轴逐位对上** dump）。

旁证：ARF 若真在跑，给出的高会是 `132.86 ÷ 3.83864 = 34.61` —— **不是 48.24**。
探针脚本（临时、在系统 temp，不入仓）：用 `工具/menu_rect.py` 的 `Bundle` 直读 `RectTransform/*.json` + `MonoBehaviour/*.json`。

### 1·3 改了什么

| 文件 | 行 | 内容 |
|---|---|---|
| `Shell/CollectionWindow.cs` | `:1814` | 新增常量 `BackTxtL/T/R/B = 200.5 / 89.3 / 333.4 / 137.5`（+ 上面那整段查证写进注释） |
| 同上 | `:2010` | `Text(…)` 的矩形：`cr` → **`bt`**（`CloseBtn*` 那份**不动**，它还是那颗钮自己的 150×60） |
| 同上 | `:2016` | `SetAutoFitBox(Px(bt.W), Px(bt.H), 10, 40)`（原来是 `cr.W/cr.H` = 150/60） |

⛔ **单击命中区照旧用 `cr`**（`AddHit(transform, "CloseHit", cr, …)`）—— 原版那颗按钮**就是** 150×60。

### 1·4 断言

`Editor/CollectionScene.cs:2310`（紧跟在既有「A22② 那颗 Back 钮」那一组之后，**没覆盖**那两条）：

* `CheckNear(tmp.rectTransform.sizeDelta.x * 108f, 132.86f, 0.5f)` —— 读的是**场景里那颗 TMP 的 `rectTransform.sizeDelta`**
  （= `SetWrapWidth` 真写进去的值，形状照抄本仓既有先例 `Editor/ShopScene.cs:1501`、`Editor/MainMenuScene.cs:889`），
  ⛔ **不是**传进 `SetAutoFitBox` 的实参、也不是代码里的常量。
* `CheckNear(…sizeDelta.y * 108f, 48.24f, 0.5f)` —— 另一半。
* `CheckAt(bk, 200.5f, 333.4f, 89.3f, 137.5f, …)` —— 中心（那一档**分辨不出**这两态，中心只差 0.25px，**只有框宽那两条有鉴别力**）。

🔴 **改坏法**：把 `:2010` 的 `bt` 换回 `cr`（整颗钮 150×60）⇒ 前两条读出 **150.00 / 60.00** ⇒ 红。

---

## 二、A273 —— 条目底图 `Message Container` 的 SpriteSwap 悬停

### 2·1 判据（**现读**）

`资料/普查产出_1008/波C1_A182_四扇窗裁切.md` **§四·5**（悬停换 `40K_settings_button_hover`）+ **§一·C** 的表
（`Content` = `40K_settings_button`, **trans=2 SpriteSwap**, 色 (1,0.572,0,1)）。**我另 dump 了一次原文**：

```
python 工具/menu_dump.py bundle_menus_assets_all "Message Container" --depth 4
  1    Content  628.71 × 140.70   Image,EverguildButton,…  40K_settings_button 168×156 否 | Simple (1,0.572,0,1)
       | trans=2 target=5058986616577207217 interactable=1 | HL=40K_settings_button_hover P=40K_settings_button_pressed
```
⇒ 常态 **`40K_settings_button`** · 悬停 **`40K_settings_button_hover`** · 按下 `40K_settings_button_pressed` · `m_Transition = 2`。

⚠️ **A273 只点名了 Inbox**：`Daily Streak Popup` 那棵树里**没有** `Message Container`
（`grep -rn "Message Container" --include=*.cs Assets/CardPresentation/` 的**生产代码命中只有 `Shell/InboxWindow.cs`**；另 3 行在 `Editor/RewardsScene.cs`，**都是注释**），
而连登窗的奖格底图 `BG` = `UI_Deck_Information_submenu_Back_opaque`、**节点上没有任何 `Selectable`/`trans`**
（`menu_dump … "Daily Streak Reward Popup Entry" --depth 3` 实读，`colider` 那颗才是 `trans=1`）⇒ **连登窗无此缺口**。

### 2·2 改了什么（`Shell/InboxWindow.cs`）

| 行 | 内容 |
|---|---|
| `:113` / `:119` / `:126` | 新常量 `ArtRowHover` · `ArtRowPressed` · `QRowHit = QOverlay`（各带判据注释） |
| `:383-388` | `BuildRow`：`DrawRect` 的返回值收进 `bg`，再 `AddHit(e, "RowHit", OR(RowBg), QRowHit, <出声的 onClick>, bg, ArtRowBg, ArtRowHover, ArtRowPressed)` |
| `:25-32` / `:344-353` | 文件头 + `BuildRow` 的**就地订正**（原文写「不接点击 ⇒ 于是也不做悬停换图」——**后半句是错的**） |

🔴 **两个坑，都是真的（不照做的话「接上了」是假的）**：

1. **悬停图必须【显式】传 `ArtRowHover`。** ⛔ 不能只写 `Bind(…, ArtRowBg)` 让表推：
   `WindowButton.HoverNames`（`Shell/PromptPopup.cs:501`）有一条 `40K_settings_button → **40K_settings_button_selected**` 的**逐颗覆盖**
   —— 那条来自 `ChatPanel` 的页签 `Toggle`（注释自己写着「⚠️ 不是 `_hover`」）。
   **同一张常态图在不同 prefab 上配的高亮图不同** ⇒ 靠表推会**静默换成 `_selected`**。
2. **命中区档要高过本窗吸收层。** `MenuDraw.Absorb` 给吸收层的档 = `qContentMin − 1` = `QOverlay − 1` = **3013**，
   而条目底图只是 `QPanel`(3006)；`PointerLayer.HitButton` 挑赢家 **队列大的先** ⇒ 挂在 3006 上会被吸收层整个盖掉、
   **悬停换图永远不会发生**（= 假实现）。故 `QRowHit = QOverlay`(3014)（本窗既有的内容命中区 = 关闭钮那颗，也在这一档）。

**点击那半仍未做**：原版点条目 = 右侧 `Message Display` 显示正文，而正文那几件没建 ⇒ 命中区上的 `onClick` **如实出声**
（先例 = 主菜单 `ReplayButton` 那条「点了会出声说回放没做」），⛔ 不挂空 lambda。

### 2·3 断言（`Editor/RewardsScene.cs:4158` 起，插在「灌 8 条假消息」那一节的 `Initialize(null)` 收尾之前）

* `rowWb != null && rowWb.target != null` + `onClick != null`
* `Check(rowWb.NormalTexForTest.name, "40K_settings_button")` · `Check(rowWb.HoverTexForTest.name, "40K_settings_button_hover")`
  —— 读**它自己那两个字段**（`NormalTexForTest`/`HoverTexForTest`，`Shell/PromptPopup.cs:798-799`），期望值是**原版字面量**。
* 命中区渲染矩形 4 条 `CheckNear`：`99.57 / 359.68 / 728.28 / 500.38`（视觉第 1 行 = `Rows[6]`，**不在软边带内**）。
* **真鼠标路径**：`PointerLayer.HoverAt(行中心)` ⇒ 命中的**就是**这一颗 ⇒ 底图换成 `…_hover` ⇒ 移开到 (1200,500) ⇒ 还原。

🔴 **改坏法（两条，机理都写进注释了）**：
① 去掉 `ArtRowHover` 那个实参 ⇒ `HoverTexForTest` 变 `…_selected` ⇒ 两条红；
② `QRowHit` 退回 `QPanel` ⇒ 吸收层盖住它 ⇒ `HoverAt` 命中的不是这一颗 ⇒ 红。

---

## 三、A229 —— `basis == parent` 守卫

### 3·1 `DeckInfoPopup`（简报那一半，成立）

`:1222` 的 `Txt`（签名 `:1213`）补上与同文件 `Nine`（`:1207`）**同一条**守卫（`ReferenceEquals` + `Debug.LogWarning`，⛔ 只加「不同就出声」那一句）。
另加自检口 `public Label TxtBasisProbeForTest(parent, basis)`（`:1241`，`Align` 是私有 enum、自检够不到 ⇒ 由它代填）。

### 3·2 🔴🔴 `PracticeModePopup` —— **简报/账上那句判据指向【不存在】的东西**

A229 原文（`资料/待办判据_1008.md:33` 与波 B4 报告 §1·4 🅁2 / §三·3）说
「`Shell/PracticeModePopup.cs:1526-1540` 的 **`Txt`** helper 没有守卫」。**现读**：

* 该文件 `Label Txt(...)` 的签名是 **`(Transform parent, string text, float x1, …)`** —— **根本没有 `basis` 参数**，
  落位用 `Local3(parent, …)` ⇒ **`basis == parent` 恒成立、结构上不可能不等**（HEAD 版也在同一行 `:1529`，`git log -S` 查无带 basis 的版本）。
* 该文件里**带 `basis` 的** helper 一共三个：`Nine`（`:1510`，**早就有守卫 `:1528`**）· `Hit`（`:1562`，只转调）· **`HitOn`（`:1581`，没有守卫）**。

⇒ **判据「缺守卫」是真的，只是认错了函数**：真身 = **`HitOn`**（位置 `Local3(basis, …)`、树父 `parent`，与 `Txt`/`Nine` 同一形状）。
**我照同一口径补在 `HitOn` 一处（守卫在 `:1593`）**（`Hit` 转调它，故不写第二份）+ 自检口 `HitBasisProbeForTest`（`:1569`）。
判据引的是**该文件自己的约定**（`:1453-1454`：「坐标一律页面绝对 px；**`parent` 与 `basis` 给同一个节点**（第三种错法见 `已知的坑.md`）」）。
9 个调用点**全部**传同一个对象（`HitOn(root, root, …)` / `(cell.transform, cell.transform, …)` / `(_general, _general, …)` / `(_cardHolder, _cardHolder, …)`）⇒ **零行为变化**。
⚠️ **这一步是我做的裁断，请调度台过一眼**：如果认为 A229 只该动 `DeckInfoPopup`，把 `PracticeModePopup.cs:1584-1600` 那一段守卫删掉即可（`HitBasisProbeForTest` 与对照断言一并撤）。

### 3·3 断言（`Editor/CollectionScene.cs`，两处）

范式 = 本仓既有的「出声」断言：`Application.logMessageReceived` 计数（先例 `Editor/RewardsScene.cs:3067` 的 `hWarn` 那一段），
**正例 + 反例成对** ⇒ 断的是**守卫本身**，不是「有没有警告」：

* `:1596-1625`（DeckInfoPopup，`v2` 那扇 `state=2` 的窗里，探针挂 `A229_TxtProbe`）
  · `Check(wSame, 0, …)` 正例 `basis == parent` **不许出声** · `Check(wDiff, 1, …)` ★ 反例 **必须出声**（读 0 = 守卫被删了）。
* `:1824-1855`（PracticeModePopup，`prac` 那扇窗里，探针挂 `A229_HitProbe`）同形，匹配串是 `` `HitOn` 的 `basis` ``。

🔴 **改坏法**：删掉 `Txt`（或 `HitOn`）开头那句守卫 ⇒ 对应反例读 **0** ⇒ 红。
探针节点两次都 `Object.DestroyImmediate` 收掉（批处理没有帧循环）。

---

## 四、没做成的 / 判据对不上的

1. **A229 的 `PracticeModePopup.Txt` 不存在**（§3·2）—— 见那里的处置与「请调度台过一眼」。
2. **按下图 `40K_settings_button_pressed` 本地没有**：`Resources/Art/ui_menu/` 只有 `40K_settings_button` / `_hover` / `_selected` 三张
   ⇒ `WindowButton.Bind` 会把它记进 `MissingPressedArt`。按该表既有口径**只出声、不当缺点断**（我们的 `Press()` 取不到按下图时退回高亮图）。
   ⚠️ 但**铁律 11**：原版那颗 `m_SpriteState.m_PressedSprite` 是 `40K_settings_button_pressed` ⇒ 这张图**该补进来**，建议立账。
3. **没有实跑**：本批不跑 Unity（调度台统一跑）⇒ 上面每条断言的**红/绿都还是推理**。

---

## 五、顺手发现的（⛔ 都没动手改）

1. **`WindowButton.HoverNames` 那条 `40K_settings_button → 40K_settings_button_selected` 不是全库判据**
   （第二次实例）：`Message Container/Content` 的 `m_SpriteState.m_HighlightedSprite` 是 **`_hover`**，不是 `_selected`。
   建议在那张表旁注明「只对 `ChatPanel` 页签那几颗成立」——否则下一个人照样会「按表推名字」而静默换错图。
2. **软边切块会让悬停换图只换半条**（跨窗的既有形状，不是本件引入）：`MenuDraw.ApplySoftEdges` 会把跨渐隐带的 quad
   切成「主格 + `_softij` 子块」（`Shell/MenuDraw.cs:578`），而 `WindowButton.SwapTo` 只换 `target`（+`_nine`）
   ⇒ **压在软边带里的件悬停只换主格那部分**（本例：条目视觉第 0 行顶上那 25px、第 6 行底下那 25px）。
   ⚠️ **⛔ 别顺手改成 `BindNine`**：它对每一块都 `SetAspect(主格的比例)`，而 `ImageQuad.WorldW = _worldH × _aspect`
   （`Battle/ImageQuad.cs:108`）⇒ 会把细条**拉成整条宽**。要真做，得让 `SwapTo` 认识 `SoftEdgeRegister` 登记过的子块。
3. **`QRowHit = QOverlay` 的副作用**：条目的命中区与关闭钮的图标**同档**（几何上不重叠，今天无影响）；
   同档时 `HitButton` 退化成「枚举顺序」（`Shell/PointerLayer.cs:975-987`）⇒ 将来若有条目挪到右上角要重新对档。
4. **A265 的「未查清的一半」可以销账**：见 §1·2（序列化、ARF 关着）—— 这条**已经写进 `CollectionWindow.BackTxtL` 的注释**，正本那边不必再开一条。

---

## 六、跑过什么 / 证据坐标

* **类型检查**（`TMPDIR=/tmp/wf_wb bash d:/4/Unity/工具/typecheck.sh`）：第 1 遍 **编辑 4 错**（全在我自己新写的 `Editor/RewardsScene.cs` 块：
  `CheckText` 只存在于 CollectionScene ⇒ 已改成本地 `Check<T>`）；第 2 遍 **运行时 0 / 编辑器 0**。
* **行尾**：6 个文件改前改后**全 LF**（`crlf=0`），`git diff --numstat` 读数 = 纯增量（无整篇翻行）。
  ⚠️ `Shell/PracticeModePopup.cs` / `Editor/RewardsScene.cs` / `Editor/CollectionScene.cs` 的 `git diff` 里**还混着别人的在飞改动**（A233/A269/A270/A271/X 那几笔），
  我的增量只有带 **A265 / A273 / A229** 标记的那几段。
* **没碰**：`d:/2/**` · `工具/**` · 两张正本 · `Shell/{SettingsWindow,MenuDraw}.cs` · `Battle/{WaitBanner,SettingsPanel,MulliganPanel}.cs` · `Editor/MainMenuScene.cs` · git。
* 探针脚本（只读、临时）：`%TEMP%/wf_wb_probe_close.py`（读 `Collection Menu Variant` 的原始序列化字段）。
