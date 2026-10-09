# P-J · `A1204`：补上设置窗页签那层遮罩 `Mask Tabs buttons`

> 执行代理 **P-J** · 2026-10-20 · 白名单 = **只有** `Shell/SettingsWindow.cs`（行尾 **LF**，改前 5420 行 / 改后 5545 行）。
> **一条 Unity 自检都没跑**（红线：手头待办没做完不跑；且本件不必由我跑）；只跑了**秒级类型检查**（读数见 §6）。
> **没动 git、没改正本、没碰任何别的 `.cs`。**

---

## 1 一句话结论

**有差，差得实实在在 ⇒ 已实现**（铁律 11）。

- **差在哪**：改前我们**整颗没有** `Mask Tabs buttons` 这一层 —— `Tab Buttons` 直挂 `Menu Area` ⇒ **页签那一列（含 `Separators`）完全没有裁切**。
- **差多少**（设计 px）：`Separators` 上端多画 **29.4385** / 下端多画 **29.8025**（改前 883.08 高、改后 **823.839**）；
  `Support` 那颗键的 `button_bg` 与 `Hit` 下端多画 **6.8063**。
- **怎么落的**：走**本仓已经在用的**视口裁切（`ViewportClip.Hang`，= 原版 `RectMask2D`/`Mask` 的等效物），
  **一个绘制调用点都没改** —— 把 `Tab Buttons` 挂到那颗视口节点底下，`MenuDraw.*` 沿父链自己就解析到了。
- 🔴 **顺手更正了两条文档错记**（铁律 5）：① 那层**不是 `RectMask2D`，是 `UnityEngine.UI.Mask`**；
  ② 它**不是**「回 5 键之后就可以作废的备选方案」，是**原版本来就有的父级节点**。

---

## 2 原版那层的现读（逐字段）

出处 = `d:/2/新解包资源/assets_full/bundle_menus_assets_all`（**只读**）。
量具 = `python -I d:/4/Unity/工具/menu_dump.py` / `menu_rect.py` + 直接读 `RectTransform/*.json`。

### 2·1 树位置（**这是本件最要紧的一条**）

```
Main Menu Settings Window
  Menu Area                                        [328.10,123.11] – [1602.50,966.19]
    Generic Popup Background        (Image 40k_popup)
      Mask                          [338.50,132.55] – [1592.62,956.39]      ← Mask + Image
        Background fill             [510.62,132.55] – [1592.72,956.39]
    Popup BG                        (INACT)
    Generic Close Button
    Mask Tabs buttons               [338.2545,132.5485] – [1592.6241,956.3875]   ← **本件**：Mask + Image
      Tab Buttons                   [328.10,123.10] – [506.52,966.19]        ← TabButtons + VerticalLayoutGroup + ToggleGroup
        Separators                  [505.07,103.11] – [507.97,986.19]        ← LayoutElement(IgnoreLayout=1) + Image
        General / Media / Account / Graphics / Support   ← 五个 EverguildToggle
    Tab Content
```

🔴 **`Mask Tabs buttons` 是 `Tab Buttons` 的父级**（不是兄弟、不是 `Generic Popup Background` 的）。
判据 = `RectTransform_-453959494957105242`（= `Tab Buttons` 的 RT）的 **`m_Father = -423328652650053722`**
（= `Mask Tabs buttons` 的 RT），而它的 `m_Children[0]` **就是** `-453959494957105242`。**双向对上。**

⚠️ **`Menu Area` 下有【两颗】`Mask`**（同名）：
· `Generic Popup Background > Mask` = `[338.50,132.55]`–`[1592.62,956.39]`（`m_SizeDelta.x = −20.268` · `m_AnchoredPosition.x = 0.262024`）
· `Mask Tabs buttons` = `[338.2545,132.5485]`–`[1592.6241,956.3875]`（`m_SizeDelta.x = −20.0238` · `m_AnchoredPosition.x = 0.139893`）
⇒ **左沿差 0.2455**，⛔ **别当同一颗**（我第一版就是拿 `MaskL` 那组常量去摆它，见 §7·3 自查）。

### 2·2 `Mask Tabs buttons` 的字段

| 组件 | 判据 |
|---|---|
| `RectTransform` | `RectTransform_-423328652650053722.json`：`m_AnchorMin (0,0)` · `m_AnchorMax (1,1)` · `m_Pivot (0.5,0.5)` · `m_AnchoredPosition (**0.139892578125**, **0.178985595703125**)` · `m_SizeDelta (**−20.023799896240234**, **−19.2450008392334**)` · `m_LocalScale (1,1,1)` · `m_LocalRotation` 单位 |
| **`Mask`** | `MonoBehaviour_6350646976119472038.json`：`m_Script.m_PathID = -3041394055549590798` → `bundle_Waprforge_monoscripts/MonoScript/MonoScript_-3041394055549590798.json` 的 **`m_ClassName = "Mask"`**（`m_Namespace = UnityEngine.UI`）· **`m_ShowMaskGraphic = 0`** |
| `Image` | `MonoBehaviour_-4091509089969340506.json`：`m_Sprite` = **`40k_popup`**（`m_FileID 3` / pid `-7511397500040153103`）· `m_Type = 1`(Sliced) · `m_PixelsPerUnitMultiplier = 0.7599999904632568` · `m_PreserveAspect 0` · `m_Color (1,1,1,1)` · **`m_RaycastTarget = 0`** |
| `CanvasRenderer` | `CanvasRenderer_6319842709537390502.json` |

**裁剪矩形（设计 px，未过 `Screen()`）**：

```
Menu Area（RT -8564182181658067034：pivot(0.5,0.5) · m_SizeDelta (1274.3934326171875, 843.083984375)
           · m_AnchoredPosition (5.2993998527526855, −4.646999835968018)，父 = 窗根 pivot(0.5,0.5)@(960,540)）
  实际矩形 = [328.1027, 123.1050] – [1602.4961, 966.1890]
x1 = 328.1027 + 0.139893 + 20.0238/2 = 338.2545
x2 = 1602.4961 + 0.139893 − 20.0238/2 = 1592.6241
y1 = 123.1050  + 0.178986 + 19.2450/2 = 132.5485
y2 = 966.1890  + 0.178986 − 19.2450/2 = 956.3875
⇒  裁剪框 = [338.2545, 132.5485] – [1592.6241, 956.3875]   （1254.3696 × 823.8390）
   屏幕 px = [400.4290, 173.2936] – [1529.3617, 914.7487]  （1128.933 × 741.455）
```

**独立复算**（同源第二个读数）：
· `menu_rect.py bundle_menus_assets_all "Main Menu Settings Window" --depth 3 --no-ancestor-scale` 印
  **`Mask Tabs buttons  338.25  132.55  1592.62  956.39`** ✓
· `menu_dump.py bundle_menus_assets_all --rt -423328652650053722 --depth 5` 印屏幕
  **`400.4  173.3  1529.4  914.7`** ✓（与上面 `Screen()` 算出的 400.429/173.294/1529.362/914.749 逐位吻合）

### 2·3 它**什么时候**生效（`Mask` 的语义）

- `Mask` 是**常开**的：`m_Enabled = 1` · GO `m_IsActive = true` · 没有 `PlatformBasedComponents` 之类的显隐组件
  ⇒ **没有「某档才生效」**（与 `RectMask2D` 那族同：一颗视口一个，出厂就是那一档）。
  ⚠️ 铁律 5·c：我查了**赋值点**（全量反编译 `SettingsMenu*` 一次都没调 `TabButtons.AddTabButton /
  RemoveTabButton`，也没碰过这颗 mask）⇒ **运行时没人改它**，序列化值就是终值。
- **形状**：本地 uGUI 源码 `Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/Mask.cs`
  的 `GetModifiedMaterial` 调的是
  `StencilMaterial.Add(baseMat, 1, StencilOp.Replace, CompareFunction.Always, m_ShowMaskGraphic ? All : 0)`
  —— **没有 alpha-clip 参数** ⇒ 模板写入覆盖**那颗 `Image` 画的整块几何**。
  那张 `40k_popup` 实测**近乎全不透明**（`d:/2/Warpforge_tools/data/ui_extract/menus_assets_all_sprites/Sprite/40k_popup.png`：
  359×336 里 alpha = 0 的只有最外 1–2 px，占 **2346 / 120624 ≈ 1.9%**；中行 / 中列的 alpha 全程 245）
  ⇒ **可见形状 ≈ 它自己的 rect**，与 `RectMask2D` 同一档（差 ≤ ~1 px 的外框 hairline）。
- **射线那一面**：`Mask.IsRaycastLocationValid` = `RectTransformUtility.RectangleContainsScreenPoint(rectTransform, …)`
  ⇒ **矩形包含**，与 `RectMask2D` 完全同形。

🔴 **更正（铁律 5）**：`资料/普查产出_第十会话/P10_Online改入口.md` §5·3 与 `Shell/SettingsWindow.cs` 的旧注释
都把它记成 **`RectMask2D`** —— **记错了**，实读是 `UnityEngine.UI.Mask`。
**代价 = 0**（两者在本仓这份模型里落点相同，见上两条），但**标签要改对**，否则下一个会话会去找不存在的 `m_Padding`/`m_Softness`。

---

## 3 🔑 「状态 → 参数」表（铁律 5·c）

`TabTop(i, n)`（`SettingsWindow.cs:209`，唯一起排算式）在三种键数下的值：

```
content(n) = n×157.68350219726562 + (n−1)×8.920000076293945
surplus(n) = (966.19 − 123.10) − (content + 13 + 0)          # BarB−BarT = 843.09
TabTop(i,n) = 123.10 + 13 + surplus/2 + i×166.6035
```

| 档 | `content` | `surplus` | 首键顶 | 末键底 | 末键底 vs 遮罩底 **956.3875** | 首键顶 vs 遮罩顶 **132.5485** |
|---|---|---|---|---|---|---|
| **4 键**（旧档，留作反例） | 657.4940 | **+172.5960** | 222.3980 | 879.8920 | 在框内（余 76.50） | 在框内 |
| **5 键**（= **原版真值档**、现状） | 824.0975 | **+5.9925** | **139.09624** | 963.1938 | 🔴 **超出 6.8063** | 在框内（余 6.55） |
| **6 键**（`A1183` 已作废档） | 990.7010 | **−160.6110** | 55.7945 | 1046.4955 | 🔴 **超出 90.1078** | 🔴 **超上界 76.7540** |

**横向**（三档都一样）：键 `x ∈ [341.52, 506.52]`、`Separators` `x ∈ [505.07, 507.97]`
—— 遮罩 `x ∈ [338.2545, 1592.6241]` ⇒ **横向一个都不越界**（键的左沿离遮罩左沿还有 3.27）。

**逐件「有没有超出裁剪矩形、超多少」**（设计 px；`[越界] = 原版遮罩真的会切掉的部分`）：

| 件 | 矩形（设计 px） | 5 键档下 |
|---|---|---|
| `Tab Buttons`（裸节点、不画） | `[328.10,123.10]–[506.52,966.19]` | 越界（左 10.15 / 上 9.45 / 下 9.80）—— **但它不画** ⇒ **无可见差** |
| `Separators`（`40k_Separator Fade Sides Vertical`） | `[505.07,**103.11**]–[507.97,**986.19**]` | 🔴 **上越 29.4385 / 下越 29.8025** ⇒ 883.08 → **823.839** |
| 键 0 `General` | `[341.52,139.09624]–[506.52,296.77975]` | 在框内 |
| 键 1 `Audio` | `…305.69975 – 463.38325` | 在框内 |
| 键 2 `Account` | `…472.30325 – 629.98675` | 在框内 |
| 键 3 `Graphics` | `…638.90675 – 796.59025` | 在框内 |
| 键 4 `Support` | `[341.52,805.51025]–[506.52,**963.19376**]` | 🔴 **下越 6.8063** ⇒ `button_bg` 与 `Hit` 各被切 6.8063 |
| 键上 `Icon`（PA=1） | `[353.52, t+13]–[494.52, t+120]` | 键 4：底 925.510 ⇒ 在框内 |
| 键上 `Tab Toggle Title` | `[346.52, t+106]–[501.52, t+146]` | 键 4：底 951.510 ⇒ 在框内（余 4.88） |

⇒ **可见差只有两处**：`Separators` 两端（各 ~29.4 设计 / ~26.5 屏幕 px）、`Support` 键的底边 6.81 设计 px。
**其余件一个像素不变** —— 这是「回到 5 键之后 surplus = +5.99、首键顶 139.096」的直接后果。

⚠️ **「没差」不是「不做」** —— 本件**有差**，所以做了。但即使差为 0 也要落这条判据（上面的算式就是判据本身）。

---

## 4 逐处改动清单

**文件 = `d:/4/Unity/MyGame/Assets/CardPresentation/Shell/SettingsWindow.cs`（唯一白名单）**

| # | 位置 | 改前 → 改后 | 判据 |
|---|---|---|---|
| ① | `Build()` 原 `:1530` | `Rect(area, "Separators", BarSepL, BarSepT, BarSepR, BarSepB, ArtSep, QPanel);` **删除**，改成一句指向 `BuildTabs` 的注释 | 原版 `Separators` 的 `m_Father = -453959494957105242`（= `Tab Buttons`），⛔ 不是 `Menu Area` |
| ② | `BuildTabs()` 头（新 `:1670` 起） | **新增** `var tabsMask = ViewportClip.Hang(area, "Mask Tabs buttons", Screen(TabsMaskL, TabsMaskT, TabsMaskR, TabsMaskB), Vector4.zero, Vector2Int.zero).transform;` | §2·2 的四条算式；`Vector4.zero / Vector2Int.zero` = **该挂 `Mask`（不是 `RectMask2D`）**、没有 padding / softness ⇒ 硬边全 0 |
| ③ | `BuildTabs()` | `var bar = Node(area, "Tab Buttons", …)` → `var bar = Node(**tabsMask**, "Tab Buttons", …)` | `MenuDraw.Local(parent, r) = RectCenter(r) − PosInDesignSpace(parent)` ⇒ **换父级不改矩形**（A811 那条恒等式；`Tab Buttons` 仍在 `[328.10,123.10]–[506.52,966.19]`） |
| ④ | `BuildTabs()`（新 `:1721`） | **新增** `Rect(bar, "Separators", BarSepL, BarSepT, BarSepR, BarSepB, ArtSep, QPanel);`（照 ① 搬进来，父 = `Tab Buttons`） | 原版 `Separators` 在 `Tab Buttons` 底下 ⇒ **它也在遮罩里** |
| ⑤ | 常量区（新 `:155–176`） | **新增** `TabsMaskL/TabsMaskT/TabsMaskR/TabsMaskB = 338.2545 / 132.5485 / 1592.6241 / 956.3875` + 算式 doc | §2·2；并把「它 ≠ `MaskL` 那一组」写进 doc |
| ⑥ | 文件头 `:16–34` | 补记 `Menu Area > Generic Popup Background > Mask > Background fill` 的真实父链 + 新增 `Mask Tabs buttons` 一条 + 更正「两颗同名 Mask 矩形也不同」 | 铁律 5 |
| ⑦ | `TabAlignY` doc（`~:198–208`） | **更正**：原来把「补那层遮罩」列进「随裁定①作废的三条备选」—— **错**，它不是备选、是原版本来就有的节点（铁律 11 必须做）；并删掉「我们**没实现**」那句已不成立的记录 | 铁律 5 + 铁律 11 |

**改动量**：`git diff --numstat` = **131 / 6**（对着 5545 行的文件 ⇒ 不是整篇重写）。全程 **Edit 工具**，
⛔ 无 `sed -i`、⛔ 无 python 文本模式写。**行尾 = 纯 LF（CRLF=0 / LF=5545）**，改前也是纯 LF。

**本件没改**（都不是偷懒，是「原版本身就没有」或「另一笔」）：
- `Image`（`40k_popup` Sliced ppuMul 0.76）**不画** —— `m_ShowMaskGraphic = 0` ⇒ `ColorWriteMask = 0`，原版一个像素都不画。
- 那颗 `Mask` 在树里**没有 `m_Padding` / `m_Softness`**（它是 `Mask` 不是 `RectMask2D`）⇒ 全 0，不是我们挑的。

---

## 5 该断什么（给下一波断言宿主的清单，⛔ 本件没碰 `Editor/`）

> 🔴 **先说一条会红的既有断言**（**必须先改，否则下一趟 `SettingsScene.Run` 必红**）：
> `Editor/SettingsScene.cs:702`
> ```csharp
> CheckRectS(FindChild(Area(root), "Separators"), SettingsWindow.BarSepL, SettingsWindow.BarSepT,
>            SettingsWindow.BarSepR, SettingsWindow.BarSepB, "`Separators`");
> ```
> 改后 `Separators` 那颗 quad 被裁到 **2.610 × 741.455**（屏幕），而这里期望的是未裁的 **2.610 × 794.772**
> ⇒ **红**。应改为（把上下沿换成遮罩那条边，**并写明这两个数来自原版 prefab、不是从我们的实现反推的**）：
> ```csharp
> CheckRectS(FindChild(Area(root), "Separators"), SettingsWindow.BarSepL, 132.5485f,
>            SettingsWindow.BarSepR, 956.3875f, "`Separators`（被 `Mask Tabs buttons` 裁过的那一段）");
> ```
> ⚠️ **这两个字面量直接取原版 prefab 的遮罩上下沿**（`TabsMaskT/TabsMaskB` 的同值手写）
> —— ⛔ 别写成 `SettingsWindow.TabsMaskT`，那会变成「拿被测实现的常量证明被测实现」（本仓那条老坑）。

**建议新增的断言**（每条 = 节点名 + 期望的裁剪矩形 + 改坏法）：

| # | 断什么 | 期望值 | 🧨 改坏法（必须能红） |
|---|---|---|---|
| ① | **前提·不静默**：`FindChild(Area(root), "Mask Tabs buttons")` 取得到 | 非 null，且 `GetComponent<ViewportClip>() != null` | 把 `ViewportClip.Hang(...)` 换回 `Node(area, "Mask Tabs buttons", …)` ⇒ 没有组件 ⇒ 红 |
| ② | **它记过基准矩形**（框的中心那一帧） | `vc.HasBaseRect == true` 且 `vc.BaseRect` 逐字段 = **`[400.4290,173.2936]–[1529.3617,914.7487]`**（**字面量**，⛔ 不过 `Screen()`） | ① 传 `Screen(MaskL,…)`（= 那颗**兄弟**）⇒ 左沿 400.65 ⇒ 红；② 忘了 `SetBaseRect`（改走裸 `AddComponent`）⇒ `HasBaseRect == false` ⇒ 红 |
| ③ | **层级归真**：`Tab Buttons` 的父级**就是**它 | `FindChild(Area(root),"Tab Buttons").parent == FindChild(Area(root),"Mask Tabs buttons")` | 把 `bar` 改回 `Node(area, …)` ⇒ 红（本条是钉 `A1204` 的那一颗钉子） |
| ④ | **`Separators` 在遮罩里** | `FindChild(Area(root),"Separators").parent.name == "Tab Buttons"` | 搬回 `area` ⇒ 红（**而且 ④ 红的时候 ⑥ 也该红** —— 两条一起动才算真的钉住） |
| ⑤ | **换父级不改矩形**（防「顺手把子件也挪了」） | `Tab Buttons` 仍在 `Screen(BarL,BarT,BarR,BarB)` 的中心、宽高 178.42×843.08 **过 0.9 之后那一档** | 在 `Node(tabsMask, …)` 之前先 `MenuDraw.ApplyPxRect(bar, area, …)` 之类 ⇒ 红 |
| ⑥ | 🔑 **裁切真的生效**（这一条才是「这条路带电」） | `MenuDraw.ClipRectAbove(FindChild(Area(root),"Separators").parent, new PxRect(设计矩形…), null, out cr)` ⇒ `cr` 的 **y1 == 173.294 / y2 == 914.749**（屏幕），且 `MenuDraw.SameRect(cr, 原矩形) == **false**` | 把 `Vector4.zero` 换成内缩的 pad、或把遮罩矩形放大到整屏 ⇒ `SameRect == true` ⇒ 红（**只断「节点建出来了」不够** —— 那是那个窗参六条全绿、树却是空的老坑） |
| ⑦ | 🔑 **「该藏的时候藏住了吗」**（本仓那条硬规矩的等效物） | `Support` 键的 `Hit` 那颗 quad：底沿 == **914.749**（屏幕），**不是**未裁的 **920.874** | 把 `TabsMaskB` 加大到 966.19 ⇒ 底沿回到 920.874 ⇒ 红 |
| ⑧ | **遮罩不越界**（别把它挂到 `Menu Area` 上，那会把五页内容也裁了） | `ViewportClip.FindAbove(FindChild(Area(root),"General Tab")) == null` | 把 `Hang` 的父从 `area` 改成…（即误挂到 `Area(root)`）⇒ 非 null ⇒ 红 |
| ⑨ | **`Mask` 的 Image 不画**（防顺手补一张 `40k_popup`） | `Mask Tabs buttons` 节点上 `GetComponent<ImageQuad>() == null`、且子树里没有 `40k_popup` 的 quad | 加一句 `Nine(tabsMask, "bg", …)` ⇒ 红 |

**注意（写 ⑦ 时踩的坑，写在这里免得下一波再算错）**：
`Hit` 节点的**节点自己**摆在父原点、**quad** 摆在矩形中心 ⇒ 量它得用 `MenuDraw.UnionQuadRectPx`
（宿主的 `RectOf`）或那个 `Hit` 节点的 quad（`GetComponentInChildren<ImageQuad>()` 的 `WorldW/H`）。
`Support` 键的 `Hit` 逻辑矩形 = `[341.52,805.51025]–[506.52,963.19376]`（设计）⇒ 屏幕
`[403.368,779.6597]–[551.868,920.8744]`，被裁到 `y2 = 914.7487`。

---

## 6 验证

```
TMPDIR=/tmp/wf_pj bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---   运行时错误数: 0
--- 编辑器程序集 ---   编辑器错误数: 0        ← 收尾读数
```

跑了 **3 次**：
1. 第 1 次（改完第 ①②③④ 处，还没补 `TabsMask*` 常量）：**两栏 0 错**。
2. 第 2 次：编辑器栏 **1 错** —— `Assets\CardPresentation\Editor\ShellScene.cs(6652,75): error CS0103: 当前上下文中不存在名称"FontPxOf"`。
   🔴 **那不是本件的错** —— 全在**别人正在写的文件**上（`Editor/ShellScene.cs` 不在本件白名单）。
   按简报口径：**没去改别人的文件**，隔 90 秒重跑 ⇒ 第 3 次干净。
3. 第 3 次：**两栏 0 错**。

```
git diff --numstat -- Unity/MyGame/Assets/CardPresentation/Shell/SettingsWindow.cs
131     6       Unity/MyGame/Assets/CardPresentation/Shell/SettingsWindow.cs

行尾（python -I 二进制数）：CRLF = 0 · LF = 5545     ← 改前也是纯 LF（0 / 5420）
```

**没跑任何 Unity 自检**（红线）。本件覆盖到哪几条（**给调度台按覆盖面挑**）：
- **必跑**：`SettingsScene.Run`（本件主宿主 —— 页签那一列的层级 + 那颗 `Separators` 的矩形 + ⑨ 那片几何）。
- **建议**：`ShellScene.Run`（它那一节会 `SettingsWindow.Create` ⇒ 走 `Build()` / `BuildTabs()` 新路径）。
- **不必跑**：其余 10 条（没碰它们的宿主；⛔ 也**没碰任何共用件** —— `MenuDraw` / `ViewportClip` 一个字节没动，只是**调用**它们）。

---

## 7 没查清 / 停手的部分

1. **全部是静态推出来的**（本件不跑 Unity ⇒ 树 / 矩形 / 裁切 / 命中都只证明了「编得过 + 算式对」）。
   至少要真跑一次才算数的两件：
   ① `Separators` 直观看**变短 26.5×2 = 53 屏幕 px** 之后像不像（它是一条 2.6 px 宽的竖分隔线）；
   ② `Support` 键被切掉底边 6.13 屏幕 px 之后，`40K_settings_button` 那张图的**底缘**会不会看起来「断了」
      （原版也是这样断的，但**眼见为实**）。
2. **`Mask` 的 alpha 语义我是按「本地 uGUI 源码 + 贴图 alpha 实测」判的**（§2·3），**没有跑到实况**。
   原版客户端用的 uGUI 版本可能与本机 `com.unity.ugui@27635d171b1a` 不同 ——
   若那一版 `Mask` **带** alpha-clip，可见形状仍相同（`40k_popup` 99.8% 不透明）⇒ **结论不受影响**。
3. **0.2455 那个左沿差**：我照原版取了 338.2545，但**它目前裁不到任何东西**（遮罩下最靠左的**绘制件**是键 341.52）
   ⇒ 视觉上 338.25 与 338.50 等价。**若将来往那一列左边加件**，差的正是这条线。
4. **登录弹窗那颗 `Mask`（`Shell/SettingsWindow.cs` 的 `LwMaskL…LwMaskB`）本件没动** ——
   它是**另一扇窗**的一层，形状相同（也把我们压平了），另立账。

---

## 8 🔑 顺手发现（⛔ 只报不改）

1. 🔴 **`Editor/SettingsScene.cs:702` 这条既有断言会因本件变红**（见 §5 开头，改写方案已给）。**必须先改它**。
2. 🔴 **`Support` 键的 `Badge Highlight` 我们【没建】**（原版有，判据 = `menu_dump --rt -423328652650053722`
   印的 `Support > Badge Highlight`：`Image(UiBadgeNotification)` · 贴图 `40K_notification_number` 65×65 ·
   屏幕 `519.7 787.6 → 551.2 819.1` ⇒ 设计 `[470.77,815.11]–[505.90,850.00]` · `31.5×31.5` ·
   子树里还有一颗 `OneText`（TMP，fs 34 / 基准 36 / 自动 [18,34] / 折行 1）。
   `Shell/SettingsWindow.cs` 里 **`grep "Badge"` 零命中** ⇒ **整块缺**。
   ⚠️ 它落在遮罩框**里面**（底 850.00 < 956.39）⇒ **与本件无关、是独立的一笔**。
3. 🔴 **`Separators` 的画法与原版不同**（独立缺陷）：
   原版那颗 `Image`（`MonoBehaviour_159295325986979750.json`）是 **`m_Type = 1`（Sliced）** ·
   `m_PixelsPerUnitMultiplier = **1.0**` · `m_RaycastTarget = 0` · 色 `(0.0157, 0.3216, 0.2078, 1)`，
   九宫格 `(0,63,0,63)`（= 上下各 63 贴图 px 的**淡出端帽**，`40k_Separator Fade Sides Vertical` 4×128）；
   而我们 `Build()` 用的是 **`Rect(...)`（Simple、整张贴图拉伸）** ⇒ 端帽被拉成整条高（淡出长度差一个量级）。
   ⚠️ **本条与 `A1204` 无关**（本件只负责「裁到哪」、不负责「怎么画」）⇒ 另立账。
   ⚠️ 也**不影响本件的结论**：无论哪种画法，遮罩都把那 29.4/29.8 px 切掉。
4. ⚠️ **`Generic Popup Background > Mask` 我们压平了**（那颗 `Mask` 建在 `Menu Area` 下、`Background fill` 抬成兄弟）：
   可见结果**等价**（它的 `Image` 本意是裁 `Background fill`，而 `Background fill` 右沿 1592.72 只比遮罩右沿 1592.62
   出 **0.10 px**、上下齐平）⇒ **不值得单独开一笔**，已写进文件头。⛔ 但**别把它与本件那颗混成一颗**（矩形差 0.2455）。
5. ⚠️ **`P10` 那份报告 §5·3 的两处错**（已被本件就地改掉）：
   ① 把它记成 `RectMask2D`（实为 `Mask`）；② 把它写成「回到 5 键后**只影响很小的出入眉**」——
   实读 `Separators` 两端各被切 **~29.4 设计 px**（不是「很小」），`Support` 键底 6.81。
   代价 = `A1204` 这一笔的优先级被低估了。
6. 🔴 **一处会误导下一个会话的措辞**：`Shell/SettingsWindow.cs` 的 `TabAlignY` doc 原来把
   「补那层遮罩」与「改上对齐 / 缩键高」并列为**三条备选** —— 前两条确实是「为了塞 6 键」的补救，
   **第三条不是**。本件已就地更正（铁律 5），记在这里是因为**同类措辞可能被复制到别处**：
   `grep -rn "Mask Tabs buttons" d:/4/Unity/资料/*.md` 里若有同样口径，**也要一起改**（本件没动 `资料/` 的别的文件）。
