# FIX-1 · 修 `CollectionScene.Run` 的 4 条失败（702/706 → 4 条红）

> 一件活：4 条失败 = **1 个根因 + 3 级联**，根因在 `Editor/CollectionScene.cs` 的「点面板外」那一步。
> 裁决 **不是** 调度台预判的那一支（「我们的实现是对的、问题全在测试」）—— 见下。

---

## 一、第 1 步的裁决（决定性）

### 1-a 原版那颗 `Image` 的 **rect**：**和我们一字不差**

| | 值 |
|---|---|
| 解包路径 | `d:/2/新解包资源/assets_full/bundle_menus_assets_all/` |
| GameObject | `GameObject/Warlord Image_-6735770576364533336.json`（`m_Name = "Warlord Image"`、`m_IsActive = true`、`m_Layer = 5`）—— 父链 = `Deck info Popup`（根 `m_GameObject` pid `-4402140163719919192`）的**第 3 个子件**（兄弟序：`Menu Dark Background` → `Generic Window Red Background Big` → **`Warlord Image`** → `Deck Details` → …） |
| RectTransform | pid `8411164374367242664`：`m_AnchorMin = m_AnchorMax = (0.5, 0.5)` · `m_AnchoredPosition = (-514.9810180664062, -534.0)` · `m_SizeDelta = (1107.9940185546875, 1107.9940185546875)` · `m_Pivot = (0.5, 0.0)` · `m_LocalScale = 1` |
| 复算（UGUI 公式） | `refPx = (960, 540)` → `pivotPx = (445.019, 1074.0)` → **rect `(-108.978, -33.994) → (999.016, 1074.0)`** |
| 交叉验证 | `python 工具/menu_rect.py <bundle> "Deck info Popup" --depth 3` 打同一行：`Warlord Image  -108.98  -33.99  999.02  1074.00  1107.99  1107.99` |
| 我们的常量 | `Shell/DeckInfoPopup.cs` 的 `WarlordL/T/R/B = -108.98 / -33.99 / 999.02 / 1074` ✅ **逐字吻合** |

⇒ **`Warlord Image` 这条 rect 没写错。**

### 1-b 🔴 但「rect 对」≠「命中区对」：那颗 `Image` 带 `m_RaycastPadding`

同一 GameObject 上的 `Image`（`MonoBehaviour/MonoBehaviour_-7131536541767857752.json`）逐字段：

```
"m_RaycastTarget": 1,
"m_RaycastPadding": { "x": 246.8000030517578, "y": 84.44000244140625,
                      "z": 338.6000061035156,  "w": 132.3800048828125 },
```

分量序 = **L, B, R, T**（判据：`Runtime/UGUI/UI/Core/RectMask2D.cs` 里 `padding` 的 doc-comment
「X = Left / Y = Bottom / Z = Right / W = Top」，两个字段喂的是同一个 `offset` 形参）。

⇒ 原版的**命中区**（不是渲染区）：

```
x1 = -108.98 + 246.80 = 137.82      x2 = 999.02 - 338.60 = 660.42
y1 =  -33.99 + 132.38 =  98.39      y2 = 1074.00 -  84.44 = 989.56
            ⇒ (137.82, 98.39) → (660.42, 989.56)  即 522.6 × 891.18
```

**⇒ `(5,5)` 在原版的命中区之外** —— 原版在 (5,5) 命中的就是压暗层（窗正常关）。

缩完那四条边**正好收在窗口内**（这不是巧合，是它的用途）：左 137.82 > 红底左 **134.50** ·
右 660.42 ≈ `Info Panel` 左 **659.0** · 上 98.39 > 红底上 **82** · 下 989.56 < 红底下 **1032**。
不缩的后果也说得通：这颗 `m_RaycastTarget = 1` 的 1108² 件**盖住屏幕左上角与大半块暗底**，
且它在兄弟序里**排在暗底之后**（= 盖在暗底上）⇒ 不缩就**吃掉了「点窗外关窗」的一大片**。

### 1-c 符号判据（**这次坐实了，不是推断**）

`Graphic.m_RaycastPadding` 与 `RectMask2D.m_Padding` **喂的是同一个 `offset` 形参**
（`RectTransformUtility.RectangleContainsScreenPoint(rect, sp, cam, offset)`）。
引擎里那个函数是 native（`d:/2/tools/il2cpp_out/dump.cs:1123499-1123502` 里三个重载都是空体，
`[FreeFunction]` ⇒ 上一轮「读不到」的结论**没错**），
**但 UGUI 侧还有一处读得到、而且读的就是同名的 offset**：

`Runtime/UGUI/UI/Core/Culling/Clipping.cs` · `FindCullAndClipWorldRect`（`MyGame/Library/PackageCache/com.unity.ugui@27635d171b1a/`）：

```csharp
Vector4 offset = rectMaskParents[0].padding;
float xMin = current.xMin + offset.x;     // +Left
float xMax = current.xMax - offset.z;     // -Right
float yMin = current.yMin + offset.y;     // +Bottom
float yMax = current.yMax - offset.w;     // -Top
...
validRect = xMax > xMin && yMax > yMin;
```

⇒ **正分量 = 四条边往里推（缩小）**；紧跟着那句 `validRect`（正 padding 能把遮罩算成空）
**只有「缩」才可能触发**。这条把 `MenuDraw.PaddedHitRect` 上挂了很久的 `[TODO-verify]`
（「正 = 缩小 / 负 = 扩大」）**钉死了**。

### 1-d 裁决

| 问 | 答 |
|---|---|
| 原版那颗的 **rect** 是多少 | `-108.98, -33.99 → 999.02, 1074`（1108²）—— **与我们一致** |
| 我们的 **rect** 对不对 | ✅ 对 |
| 原版那颗**盖不盖 (5,5)** | **不盖**（命中区被 `m_RaycastPadding` 缩成 `137.82,98.39 → 660.42,989.56`） |
| **我们的 `WarlordHit` 对不对** | ❌ **不对** —— 裸 1108²，比原版大出一条 585px 宽的竖带 + 上下各一截 |
| ⇒ 走哪一支 | **分叉第 2 支：「我们的矩形错了」⇒ 先修实现**（改 `Shell/DeckInfoPopup.cs`），再改测试 |

⇒ 调度台原来那句「相 1 逐条核过：本批 20 个吸收矩形全都不覆盖 (5,5)」**是对的、但不够** ——
漏掉的是**立绘那颗 Graphic 自己的 `m_RaycastPadding`**（相 1 只看了吸收矩形，没看立绘命中区）。

---

## 二、改动清单（改前 → 改后）

### ① `Unity/MyGame/Assets/CardPresentation/Shell/DeckInfoPopup.cs`（修实现）

**改前**（`Build()` 第 3) 段）：

```csharp
Hit(root, root, "WarlordHit", new PxRect(WarlordL, WarlordT, WarlordR, WarlordB), OpenWarlordDetail);
```

**改后**：

```csharp
Hit(root, root, "WarlordHit",
    MenuDraw.PaddedHitRect(new PxRect(WarlordL, WarlordT, WarlordR, WarlordB), WarlordPad),
    OpenWarlordDetail);
```

并新增常量（挨着 `WarlordL/T/R/B`）：

```csharp
public static readonly Vector4 WarlordPad = new Vector4(246.8f, 84.44f, 338.6f, 132.38f);
```

* **立绘本身（`Img(...)`）一个字没动** —— 仍画满 1108²；原版 `m_PreserveAspect = 0` 也是拉满。
  padding **只改命中区**（原版就是这么分工的：`GraphicRaycaster` 那一句只把它喂给射线）。
* 走公共件 `MenuDraw.PaddedHitRect` 而不是在这里重抄算式 —— 符号约定**只留一处**；
  `pad == Vector4.zero` 时它原样返回 ⇒ 对别的调用点零影响。
* 退化守卫不会响：`522.6 × 891.18` 远大于 0（`PaddedHitDegenerates` 那一格仍是 0，
  `RewardsScene` 那条 `Check(MenuDraw.PaddedHitDegenerates, 0, …)` 不受影响）。
* 注释里写清了：原版值 / 出处 JSON 的 pid / 复算后的四个边 / **符号判据引的是 `Clipping.cs` 的符号名**。

### ② `Unity/MyGame/Assets/CardPresentation/Editor/CollectionScene.cs`（修测试）

`CheckAbsorbRule` 收尾那 3 行 —— 改前是**弱断言**：

```csharp
// 点面板外：本批 20 个吸收矩形**全都不覆盖 (5,5)**（相 1 逐条核过）
var oHit = pl.ButtonAt(5f, 5f);
CheckTrue(oHit != null && !oHit.absorbOnly && oHit.transform.IsChildOf(winRoot), "…");
CheckTrue(pl.ClickAt(5f, 5f), $"{what}：点面板外 (5,5)（真路径）");
Check(state(), WindowState.Closed, "…");
```

改后：

```csharp
const float OutX = 5f, OutY = 5f;
var oHit = pl.ButtonAt(OutX, OutY);
CheckTrue(oHit != null && oHit.transform.IsChildOf(winRoot) && MenuDraw.WasShadeHit(oHit.transform),
          $"{what}：**({OutX:F0},{OutY:F0}) 命中的就是这扇窗自己的压暗层那一颗**…（实得 `…` = …）");
CheckTrue(pl.ClickAt(OutX, OutY), $"{what}：点面板外 ({OutX:F0},{OutY:F0})（真路径）");
Check(state(), WindowState.Closed, "…");
```

三处判断，逐条说为什么：

1. **点钉死 (5,5)，⛔ 不做「扫一圈找第一个命中压暗层的点」。**
   调度台的第 3 步建议了扫描；**本轮否掉了它**，理由是它会让**刚修掉的那个缺陷从别的候选点上绕过去**
   （`(1915,5)` 落在那条竖带之外，立绘命中区再怎么过大它**照样绿**）——
   正是本工程反复记的那一族「弱断言分不出两种状态」。
   `(5,5)` 现在是**有原版判据的点**（见 1-b），点钉死、期望也钉死。
2. **判据 = 节点**不是「非吸收层 ∧ 属于本窗」那种弱条件**。改成两条合起来：
   * `oHit.transform.IsChildOf(winRoot)` —— **是这一扇自己的**（别家的窗顶掉它就红）；
   * `MenuDraw.WasShadeHit(oHit.transform)` —— **是压暗层那一颗**（**按节点上的标记认、不按名字认**）。
3. 🔴 **为什么不按名字找 `Find("BackgroundHit")`**（我第一版就是这么写的，**已改正**）：
   本文件三处 `CheckAbsorbRule` 里，**聊天窗那颗节点叫 `CloseHit`**
   （`Shell/ChatPanel.cs` 的 `MenuDraw.ShadeHit(cb, CloseBgR, QPanel, QHit, () => Close(), "CloseHit")`；
   日志实证：`_tmp_view/collection.log:20428` →「聊天窗 …（实得 `CloseHit`）」）。
   按名字写会把**本来绿的那一条**变成红。`WasShadeHit` 是工程里专门为这件事做的公共判据
   （`Editor/MainMenuScene.cs` 的 `CheckShadeRule` 已经在用同一个）。
4. 消息里带上**实测命中名 + 失败原因**（`<null>` / **别家的窗** / **本窗的，但不是压暗层那一颗**），
   红的时候一眼看出是被谁吃掉的。

### ③ 同一次改动里顺手对齐的一处**自相矛盾**

`CheckAbsorbRule` 的 doc-comment 原来写着「⛔ 「命中是谁」**不是期望值**，它只是**选点的条件**」——
那是**面板内**那个点的语义。面板外那个点**恰恰相反**（命中是谁**就是**期望值）。
两份说法在同一个文件里打架 ⇒ 已把那段拆成「面板内 / 面板外」两条，说清判据不同。

---

## 三、怎么改坏就会红（自证深度自评）

| 改动 | 怎么改坏它就红 | 自证深度 |
|---|---|---|
| `WarlordPad` 漏掉 / 写成 `Vector4.zero` | ① `(5,5)` 又被 `WarlordHit` 吃掉 ⇒ **`WasShadeHit` 那条红**；② 后面 3 条级联又回来 | **② 期望值来自原版 prefab 的字段**，不是我们自己的常量 —— 不假绿 |
| `WarlordPad` 四个数少了 / 反了（把 padding 当「放大」） | 同上那条红（`(5,5)` 会落进放大后的区域）；**再多缩**时 `(137.82,98.39)` 那一带会点不到立绘 —— 但目前**没有**一条断言钉立绘命中区的四条边（见 §四「还欠的」） | ⚠️ 只有「(5,5) 这一侧」被钉住 |
| `PaddedHitRect` 的符号约定被翻过来 | `ShellScene.Run` 的 `(10,0,0,0)` / `(−8,−5,−8,−5)` 两条 padding 断言 + 本轮这条一起红 | ② |
| 断言退回「非吸收层 ∧ 属于本窗」 | **不会红** —— 这正是它原来的病（`WarlordHit` 三条全满足）。改动本身**没有**任何断言能挡住它退化回去 ⇒ 只能靠注释 | ⛔ 这一层是**注释在防**，不是断言 |
| 「点面板外」的点改成扫描 | 不会红（而且会**掩盖**立绘命中区过大）⇒ 注释里写死了理由 | ⛔ 同上 |
| `CloseHit` ↔ `BackgroundHit` 换成按名字找 | `CheckAbsorbRule` 的**聊天窗**那一条红 | ② |

⚠️ **三条级联**（`★ A11 前置` / `★ 位移没停稳 ⇒ 真命中路点不到它` / `★ 同一格现在又点得到了`）
的机理**已在日志里实证**：`(5,5)` 那一下点了 `WarlordHit` → `OpenWarlordDetail()` → 开出一扇
**没有任何人关的 `CardDetailPopup`**（它的 `BackgroundHit` 是 1920×1080 全屏、档比筛选栏高）
⇒ 后面两条「真命中路」量到的都是它。所以根因一修，三条**应当**一起消失；
**但我不能跑 Unity**（调度台统一跑）⇒ 这三条是否转绿，以同步点那次 `CollectionScene.Run` 为准。

---

## 四、没查清 + 顺手发现

### 4·1 顺手发现（**都不在本轮白名单内，只报不改**）

**(a) 🔴 `MenuDraw` 上那条符号判据里有一句是错的 —— `RectMask2D.m_Padding` **渲染那一面也读它**。**
`Runtime/UGUI/UI/Core/RectMask2D.cs` 的 `PerformClipping` 里那行
`Rect clipRect = Clipping.FindCullAndClipWorldRect(m_Clippers, out validRect);` —
`Clipping`（`Runtime/UGUI/UI/Core/Culling/Clipping.cs`）**读的正是 `padding`**，并用它把裁剪矩形内缩；
而 `MaskUtilities.GetRectMasksForClip` 走的是 `GetComponentsInParent`（**含自身**）⇒ 自己那份 padding 算在内。
⚠️ 现在留在注释里的那句「**渲染那一面（`PerformClipping` / `rootCanvasRect`）压根不读它**」是
**只 grep 了 `RectMask2D.cs` 一个文件**得出的（`rootCanvasRect` 那个属性确实没读，但真正的裁剪在 `Clipping.cs`）。
两条后果：
① `MenuDraw.PaddedHitRect` 头上那句「**只给命中区用**」与 `MenuWindowBase` 的对应注释**要订正**，
   `[TODO-verify]` 那条**可以摘掉**（符号判据本机就有）；
② **一处真偏离**：带**非零** `m_Padding` 的 `RectMask2D`，我们的**渲染裁剪**比原版宽了一个 padding
   （本壳真正非零的只有锻造轨道那一族 `(10,0,0,0)` 等 —— 逐处表在 `MenuDraw` 那段注释里）。
   ⇒ 建议单开一件（本轮白名单不含 `MenuDraw.cs` / `MenuWindowBase.cs`）。
   ⚠️ 与本轮改动**无关**：`Warlord Image` 是 `Graphic`（只有射线那一面吃 padding），不是 `RectMask2D`。

**(b) 同一条弱断言还复制在另外 4 个宿主里**（各 1 条）：
`Editor/MainMenuScene.cs` · `Editor/RewardsScene.cs` · `Editor/SettingsScene.cs` · `Editor/ShopScene.cs`
——（`grep -rn "命中的是这扇窗自己的压暗层" Editor/`）。它们现在没红（那些窗没有超大命中区），
但判据一模一样地**分不出两种状态**。建议照本轮的两条判据统一收紧（`IsChildOf(winRoot)` ∧ `MenuDraw.WasShadeHit`）。
`Editor/CollectionScene.cs` 自己那 **3 处**（卡组信息窗 / 导入卡组窗 / 聊天窗）本轮已随公共函数一起收紧。
⚠️ **四角/四边那一层没有回归网**（那些宿主仍按 (5,5) 与「非吸收层」判）。

**(c) `Warlord Image` 的 `m_PreserveAspect`：原版是 `0`（拉伸画满 1108²），我们传的是 `keepAspect: true`。**
同一 JSON：`"m_PreserveAspect": 0` · `"m_Type": 0`。我们的 `Img(...)` 那一步会**内接留边**。
若督军立绘不是正方形，这是一处**可见的观感差异**（原版拉满 / 我们留边）。
⚠️ **没查清**：`CardArt.PortraitByName` 喂进去的那张图**是不是方的**（是方的则 PA 无差别）⇒ 这条**只记不判**。

### 4·2 还欠的（本轮白名单外 / 判不了）

* **立绘命中区的四条边没有断言**：本轮只钉住了「(5,5) 不在命中区里」这一侧；
  `137.82 / 98.39 / 660.42 / 989.56` 这四个值**没有任何自动化的尺子**
  （⛔ 别拿 `DeckInfoPopup` 里的常量当期望值 —— 那就是最浅一档的同式自证；
  要用**解包 prefab 的字段字面量**写，形状照 `CheckRectPx` / 原版矩形那几条）。
  想补就得动 `Editor/CollectionScene.cs`（白名单内，但本轮没做：会牵出「命中区节点怎么量」的一串，见下）。
* **命中区节点的 rect 怎么量**：`DeckInfoPopup.Hit(...)` 把节点摆在 `Local3(...)` 上、
  透明 quad 是 `ImageQuad`，`CheckRectPx` / `CheckAt` 两条现成的尺子都能量 —— 但**没跑过实况**，
  没法确认这次改完 `Hit` 节点的实际位置就是 `(137.82, 98.39) → (660.42, 989.56)`（**只有静态推理**）。
* **真 Play**：这一条改动直接改了鼠标手感（窗口左上角那一片现在会关窗了），
  归 `资料/真Play待验清单.md`（用户 2026-09-26 定的口径：真 Play 攒着一次性过）。
* **`bundle_menus_assets_all` 里 non-zero `m_RaycastPadding` 的逐条口径**：本轮只用了这一条
  （全正的唯一一条）；其余 207 条负值的**逐处真值**仍在 `MenuDraw` 那段表里，**没复核**。

---

## 五、跑了什么

* `TMPDIR=/tmp/wf_fix1 bash d:/4/Unity/工具/typecheck.sh`（改完两轮 .cs 后各跑一次）—— **两次都 0/0**。
* `git diff --numstat` + 二进制数行尾：两个文件**都是纯 `LF`（CRLF 计数 0）**，**行尾没翻**；
  diff 增量 = 只增我写的那几十行（`CollectionScene.cs` 677→706 / `DeckInfoPopup.cs` 238→273，含同批别人未提交的改动）。
* **没跑 Unity**（调度台统一跑）· **没动 git** · **没碰白名单外任何文件** · **没改两张正本**。

类型检查：运行时 0 / 编辑器 0
