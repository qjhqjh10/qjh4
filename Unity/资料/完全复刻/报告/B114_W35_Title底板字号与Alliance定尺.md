# B114_W35 · `B-275`（Title 底板图）+ `B-276`（Title Army 字号）+ `B-277`（Alliance Badge Icon 固定尺寸根）

> 第一百一十四会话 · 写手 `W-35` · 2026-11-01。
> ⛔ 未跑 Unity（`-executeMethod` 全禁）· ⛔ 未动 git · ⛔ 未改账/正本/`Scenes`/别人的 `.cs`。
> **typecheck（`TMPDIR=/tmp/wf_w35`）= 运行时 0 / 编辑器 0**（每次改完都复跑，最后一版也是 0/0）。

---

## 〇、三句结论

| # | 结论 | 做了什么 |
|---|---|---|
| **1** | `B-275` 成立：`Title Drawer`（竖卡）的 `Content/Background.m_Sprite` = **`40k_Rewards_icon_title card`**（329×499），我们画的是共享的 `40k_Rewards_icon_base card`（328×498）。对照 `Avatar Drawer` **确是 base card** ⇒ 共享常量是**默认值不是通则**。 | `DrawerBackground` 多一个 `string sprite` 形参（默认 = 共享那张，**20 多个既有调用点零改动**），`Title(...)` 显式传 `TitleBgSprite`。 |
| **2** | `B-276` 成立：`Title Drawer` 的 `Content/Army.m_fontSize` = **35.099998**，我们写死 75。对照 `Avatar Drawer` 的 Army **确确实实 75.0** ⇒ 75 是通则、Title 是例外。⚠️ **但这条改的是「标称值（自适应起点）」，今天画面上看不出差别**——见 §二·4。 | `DrawerArmyName` 多一个 `fontPx` 形参（默认 `DrArmyFontPx = 75`），`Title(...)` 传 35.1。 |
| **3** | `B-277` 成立：`Alliance Badge Drawer Icon` 的根**无 ARF**、锚 `(0.5,0.5)`、`m_AnchoredPosition (0, 44.6322)`、`m_SizeDelta **165.9539×155.7357**` ⇒ 原版**永远那么小、只跟着框心平移**。我们原写法是 `frameR = box`（**根当成框**）⇒ 框大画大、框小画小。🔴 **顺带订正 `B114_W32` 报告 §六·3 的一句话**（它说「我们现在走 `FrameOf(box,1080,1080)`」——**不准确**，见 §三·2）。 | 新文件 `Shell/ItemDrawerPartial_B114W35.cs`：`AllianceBadgeIconRootOf(box)` + 正确实现；`Dispatch` **同物理行**前置一句分流（零行数变化）。 |

---

## 一、活 1 · `B-275`：`Title Drawer` 的底板图

### 1. 原版判据（一手 = 现行 build 的真包）

```
python -I -X utf8 工具/menu_dump.py bundle_menus_assets_all "Title Drawer" --depth 4 --relative --no-ancestor-scale
```

| 节点 | 真包现读 | 出处 |
|---|---|---|
| `Content/Background` | 矩形 = 整个 `Content`（设计 712×1080）· sprite = **`40k_Rewards_icon_title card`**（**329×499**）· `Image`（`m_PreserveAspect 1`、`type Simple`） | `menu_dump` 第 2 层那一行 |
| `Content/Background`（对照 `Avatar Drawer`） | sprite = **`40k_Rewards_icon_base card`**（**328×498**） | 同法现读 |
| `Content/Background`（对照横条档） | `40k_Profile_display_title`（322×116） | `B114_W32` 已接，本波不动 |
| 资源落地 | `Resources/Art/ui_menu/40k_Rewards_icon_title_card.png`（**现读存在，329×499**，与真包 sprite 尺寸逐值相同） | `ls` + PNG IHDR 现读 |

⇒ **`40k_Rewards_icon_base card` 是「没写例外的那几档」的默认值，不是「每一档都一样」。**

### 2. 我们改之前是什么

- `Shell/ItemDrawerTitle.cs:57`（改前）：`DrawerBackground(content, fr, st);` —— 转调共享那一份；
- `Shell/ItemDrawerLayout.cs` 的 `DrBackgroundSprite = "40k_Rewards_icon_base_card"`（写死）；
- `ItemDrawerLayout.cs` 文件头 ② / `NodeBackground` 的 doc 都写着「**sprite 恒** `40k_Rewards_icon_base card`」。

⇒ `Title Drawer` 画的底板**图名差一个词**（`base` → `title`），尺寸只差 1px（328×498 vs 329×499）⇒ **不看图名、只看形状是看不出来的**（这正是它能潜伏 3 波普查的原因）。

### 3. 改成什么

```csharp
// Shell/ItemDrawerLayout.cs
internal static bool DrawerBackground(Transform content, DrawerFrame fr, ItemDrawerStyle st,
                                      string sprite = DrBackgroundSprite)     // ← 新形参，默认 = 旧行为
// Shell/ItemDrawerTitle.cs
const string TitleBgSprite = "40k_Rewards_icon_title_card";
...
DrawerBackground(content, fr, st, TitleBgSprite);
```

- 出声文案也改成带**实际图名**（原来写死共享那个名，换图之后会**报错人**）；
- 文件头 ② / `NodeBackground` / `DrBackgroundSprite` 三处「恒 …」**就地订正**（铁律 5），并写清三档各自的图。

### 4. 断言（`Editor/B114_W35_Assert.cs` §①，10 条）

| # | 断什么 | 怎么才能红 |
|---|---|---|
| `A0` / `A0b` | **硬前提**：两张图都取得到、抽屉路由到 `TitleDrawer` | 删图 / 改路由 ⇒ 红 |
| `A2` | 画出来的底板 `ImageQuad.Texture.name` == `40k_Rewards_icon_title_card` | 改回默认值 ⇒ 读到 `..._base_card` ⇒ 红 |
| `A3` | **灭自证（通则那一侧）**：`Avatar Drawer` 的底板仍是 `40k_Rewards_icon_base_card` | 有人把共享常量直接换成 title card ⇒ 红 |

期望值**写死在断言里**（真包那两个 sprite 名），⛔ 不是读 `DrBackgroundSprite`。

---

## 二、活 2 · `B-276`：`Title Drawer` 的 `Army` 字号

### 1. 原版判据（真包现读）

| prefab | `Content/Army` 的 TMP | 出处 |
|---|---|---|
| **`Title Drawer`** | `'Leviathan'` · **字号 = 35.099998** · 基准 36 · auto **[12, 75]** · 对齐 **Center/Bottom** · 折行 1 · 色 (0.689,0.689,0.689,0.851) · 矩形 (0,702)-(712,810) | `menu_dump … "Title Drawer"` 第 2 层 |
| **`Avatar Drawer`**（对照） | `'Goff'` · **字号 = 75.0** · 基准 36 · auto [12, 75] · 对齐 Center/Middle | `menu_dump … "Avatar Drawer"` 第 2 层 |
| `Title Drawer Horizontal Variant` | `Content/Army` **`m_IsActive = 0`** ⇒ 原版屏幕上没有这一行（我们也不建） | `B114_W32` §二·2 表 |

⇒ **75 = 通则，35.1 = Title 例外**；且 **min/max/base 三个数两档逐值相同**，只有「标称字号」与「垂直档」不同。

### 2. 我们改之前是什么

`Shell/ItemDrawerLayout.cs:204`（改前）：`ClippedText(..., NodeArmy, **75f * fr.K**, ...)` —— 写死 75，所有档共用。

### 3. 改成什么

```csharp
// Shell/ItemDrawerLayout.cs
const float DrArmyFontPx = 75f;                       // 通则（Avatar 实读）
internal static void DrawerArmyName(..., float fontPx = DrArmyFontPx)
{ ... ClippedText(content, r, name, DrArmyInk, NodeArmy, fontPx * fr.K, ...); }   // auto[12,75]/base 36 不动
// Shell/ItemDrawerTitle.cs
const float TitleArmyFontPx = 35.1f;
DrawerArmyName(content, fr, item.CardArmy, st, TitleArmyFontPx);
```

⛔ **没有动通则**：`Avatar`/`Currency`/`Alliance`/`Container`… 那 10 来档一个字节没改（`fontPx` 默认 75）。
⛔ **没动横条档**：`Title Drawer Horizontal Variant` 走 `ItemDrawerPartial_B114W32.cs` 的 `TitleHorizontal(...)`（`B114_W32` 的产物），本波一个字没碰。

### 4. 🔴 如实记：这条改的是**入参保真度**，今天画面上看不出差别

`ClippedText` → `MenuDraw.TextCore` → `SetAutoFitBox(wrap, H, min, max, **base**)`，而 `base` = **36**（两档相同）。
`Label.SetAutoFitBox` 把 `m_fontSizeBase` 写成 `cur × (basePx / nomPx)`（**折算成 px 恒等 36**），
TMP 的自适应**从 base 起算**并夹在 `[min, max]` 里（两档都是 [12,75]）⇒ **两档最终收敛到同一个字号**。

⇒ **这一条改的是「标称值 = 自适应二分起点」，与原版字段对齐**；`m_fontSize` 本身在原版里也是被同一个自适应盖住的。
（两种自适应模型——「从 base 起只缩」与「从 base 起涨到 max」——**在这两档给出同一结果**，所以哪种都不影响结论。）

**所以断言只能盯标称值**：`Label.FontSize` / `FontSizeMax` 两档**都会读到同一个数**（前者是收敛结果、后者两档都是 75px）⇒ 拿它们断言 = 自证。

### 5. 断言（`Editor/B114_W35_Assert.cs` §②，8 条）

| # | 断什么 | 怎么才能红 |
|---|---|---|
| `B0` | **硬前提**：`Label.NominalPx()` 反射句柄取得到（读「标称字号」的唯一口） | 方法改名/删掉 ⇒ 红（⛔ 不静默跳过） |
| `B0b` | **硬前提**：阵营 10 有徽记图与本地化名（否则 `DrawerArmyName` 早退、不建 `Army`） | 缺图 ⇒ 红 |
| `B3` | `Title Drawer` 的 `Army` 标称字号 = **35.1 px**（方框 `K = 1`） | 改回默认 75 ⇒ 读到 75 ⇒ 红 |
| `B4` | **灭自证（通则那一侧）**：`Avatar Drawer` 的 `Army` 标称 = **75 px** | 把 `DrArmyFontPx` 改成 35.1 ⇒ 红 |
| `B5` | **结构反证**：两档读数**差 > 30px** | 「按档传」被写成一刀切 ⇒ 红 |

---

## 三、活 3 · `B-277`：`Alliance Badge Drawer Icon` 的固定尺寸根

### 1. 原版判据（prefab 逐字段直读）

文件 = `d:/2/tools/assetripper_out/ExportedProject/Assets/Alliance Badge Drawer Icon.prefab`
（**不是 variant**：`m_SourcePrefab` 0 命中；全份 7,572 字节、只有 3 个 GameObject）

| 对象 | 字段（原文） |
|---|---|
| 根 GO `Alliance Badge Drawer Icon` | `m_IsActive: 1` |
| 根 RectTransform `&224618682377417806` | `m_AnchorMin {0.5,0.5}` · `m_AnchorMax {0.5,0.5}` · **`m_AnchoredPosition {0, 44.6322}`** · **`m_SizeDelta {165.9539, 155.7357}`** · `m_Pivot {0.5,0.5}` · `m_LocalScale 1` |
| 根组件 | `ItemDrawerComponents` + `AllianceBadgeDrawer` —— **没有 `AspectRatioFitter`**、也没有 `EverguildButton` |
| `Frame` `&224099118695303065` | 锚 `(0,0)-(1,1)` · `sizeDelta (0,0)` · pivot `(0,0)` ⇒ **满根** |
| `Badge` `&224709867586078768` | 锚 `(0,0)-(1,1)` · `sizeDelta (0,0)` · pivot `(0,0.5)` ⇒ **满根** |
| 四个槽 | `background` / `armyText` / `label` / `quantity` **全 null** ⇒ 这一档只有 `Frame` + `Badge` 两层 |

真包 `menu_dump … "Alliance Badge Drawer Icon" --depth 4 --relative` 复核：根 `165.95 × 155.74`，两颗都满根。

⇒ **原版这一档完全不跟调用框走**：永远是 `165.95×155.74`，**框只决定它的中心在哪**（框心，再上移 44.63）。

### 2. 我们改之前是什么（🔴 顺带订正 `B114_W32` 报告 §六·3 的一句）

`B114_W32` 报告 §六·3 写「我们现在走 **`FrameOf(box, 1080, 1080)`**（跟框缩放）」—— **不准确**。
现读生产：`Shell/ItemDrawerPartial_B111R3W.cs:286`（`B114_WC` 的 `IconAllianceBadge`）

```csharp
var frameR = box;   // 无 ARF ⇒ `Frame`/`Badge` 的锚 (0,0)-(1,1) 就是整个根
```

—— 它**没调 `FrameOf`**，而是把「整个根」直接当成调用框（`Frame`/`Badge` 的锚确实是 `(0,0)-(1,1)`，但那只是**相对根**）。
**结论（跟框走）没错、量级也一样**（框大画大），但**那句话点错了实现**。本波按实际代码写。
> 错因：`B114_W32` 是从 `B-260` 那套 `FrameOf` 口径推的，没回到这份 `Icon` 档的调用点现读。

### 3. 改成什么

新文件 `Shell/ItemDrawerPartial_B114W35.cs`：

```csharp
public const float AllianceBadgeIconW = 165.9539f, AllianceBadgeIconH = 155.7357f;   // prefab m_SizeDelta
public const float AllianceBadgeIconAnchorY = 44.6322f;                             // prefab m_AnchoredPosition.y
public static PxRect AllianceBadgeIconRootOf(PxRect box)   // 尺寸只由常量决定，框只决定中心
static void IconAllianceBadgeFixedDraw(...)                // = 原版 Draw 等价实现（几何换成上面那个矩形）
```

`Shell/ItemDrawer.cs` 的 `Dispatch` 第一行 `{`，**同一物理行**最前面插一句（**行数不变**，`git diff -U0` 只 `@@ -1918 +1918 @@`）：

```csharp
if (ov == DrawerOverride.Icon && IconAllianceBadgeFixed(drawer, node, box, item, quantity, style, ref res)) return;
```

其余两句（`SetTierFrame(quantity)` + 未达档 `SetGrey`）**逐句照旧**——它们与几何无关。

⚠️ **副作用（如实记）**：接上后 `ItemDrawerPartial_B111R3W.cs:280-298` 的 `IconAllianceBadge` **没有调用点了**
（`IconVariant` 只从 `Dispatch` 那一行被调）。它该由**那份文件的所有者**删掉，⛔ 本波不动别人的文件。

### 4. 断言（`Editor/B114_W35_Assert.cs` §③，9 条）

| # | 断什么 | 怎么才能红 |
|---|---|---|
| `C0` / `C1` / `C1b` | **硬前提**：徽章图取得到；两个框里 `Frame` 都画出来了 | 缺图 / 没画 ⇒ 红 |
| `C2` | 小框（`= 根的尺寸`）下 `Frame` 画成 **155.74**（方图 × `m_PreserveAspect 1` 内接） | PA 关掉 / 尺寸错 ⇒ 红 |
| `C3` | ★★**核心**：框 `165.95×155.74` → `400×400`，画出来**逐值不变** | 用 `frameR = box` ⇒ 大框画成 400 ⇒ 红 |
| `C4`/`C4b` | 根心 = 框心 **减 44.6322** | 漏掉偏移 / 加号用反 ⇒ 差 44.63 ⇒ 红 |
| `C5` | ★★**灭自证（同族对照）**：**基础档** `Alliance Badge Drawer` 在 712.8×1080 与 356.4×540 两个框里画出来**恰好 2:1**（跟着框走） | 有人把固定尺寸套到全族 ⇒ 红 |

---

## 四、改动清单 + 净增净减

| 文件 | 改动 | 净行数 |
|---|---|---|
| `Assets/CardPresentation/Shell/ItemDrawerLayout.cs` | ① 文件头 ②③ 两处「逐份相同 / sprite 恒」**就地订正**成「默认值 + 例外清单」；② 新增 `DrArmyFontPx = 75f` 常量；③ `DrawerBackground` 加 `string sprite = DrBackgroundSprite` 形参（出声文案改用实际图名）；④ `DrawerArmyName` 加 `float fontPx = DrArmyFontPx` 形参；⑤ 文件头 ① 那句「`Alliance Badge Drawer Icon` 见报告 §六·3」改成「已落地 + 落点」 | **299 → 322（+23）**（该文件在 `账/*.tsv` 里 **0 命中** ⇒ 可自由加行） |
| `Assets/CardPresentation/Shell/ItemDrawerTitle.cs` | ① 文件头版面段：`Army` 的 `fs75` → **`fs 35.099998`** + `Background` 图名 + 订正痕迹；② 新增 `TitleBgSprite` / `TitleArmyFontPx` 两个常量；③ `Title(...)` 两句改成显式传（`DrawerBackground(..., TitleBgSprite)` / `DrawerArmyName(..., TitleArmyFontPx)`）；④ `Title` 的 doc 订正「`TitleDrawerHorizontal` 共用这一份」（横条档已另走） | **107 → 128（+21）**（`账/*.tsv` **0 命中**） |
| `Assets/CardPresentation/Shell/ItemDrawerPartial_B114W35.cs`（**新建**） | `AllianceBadgeIconRootOf` + `IconAllianceBadgeFixed` + `IconAllianceBadgeFixedDraw` + 3 个 prefab 常量 | **112 行**（新文件） |
| `…ItemDrawerPartial_B114W35.cs.meta`（**新建**） | guid `300387b4e012408196b06ce0275fc7f3`（全仓 `.meta` 扫过，无重复） | 新文件 |
| `Assets/CardPresentation/Editor/B114_W35_Assert.cs`（**新建**） | 三段共 **27 条**（① 底板图 **10** · ② Army 字号 **8** · ③ 固定根 **9**） | **391 行**（新文件） |
| `…B114_W35_Assert.cs.meta`（**新建**） | guid `e8e25b9999394f1a821cc98cea2435d8` | 新文件 |
| `Assets/CardPresentation/Shell/ItemDrawer.cs` | `Dispatch` 第一行**同物理行**最前面插一句分流 + 行尾注释补一段说明 | **0**（2497 → 2497；`git diff -U0` 只有 `@@ -1918 +1918 @@` 这一条）（`账/*.tsv` 里该文件有 37+ 处 A 落点 ⇒ **必须零行数变化**） |

**行号纪律自检**（python 二进制数）：七个文件全 **纯 LF / 裸 CR 0**；
`ItemDrawer.cs` 追加的那一句落在**第一个 `//`（第 672 字节）之前**（行内第 1 段代码区），行尾追加的说明在注释里 ⇒ **没有代码掉进 `//` 后面**。

---

## 五、接线片段（可粘贴 · ⛔ 宿主是别人的，我没改 `Editor/ShellScene.cs`）

`Editor/ShellScene.cs` 里 `ShellScene.Run` 那条聚合行，**同一物理行、第一个 `//` 之前**追加：

```
B114_W35_Assert.Run(); _sink.Pass += B114_W35_Assert.Pass; _sink.Fail += B114_W35_Assert.Fail;
```

（与 `B114_W32_Assert` / `B114_W20_Assert` 同形；`B114_W35_Assert.Run()` 内部只建临时 GameObject、
`DestroyImmediate` 收尾，**不写盘、不建场景**。）

---

## 六、没查清的部分（如实记）

1. **本波三条都没有跑过断言、也没有实机看过**（⛔ 不跑 Unity 是红线）⇒ 「`C3` 真的会红/绿」「`B3` 读到的确实是 35.1」
   都是**按代码推的**，不是观测值。接上宿主后请把 `[B114W35]` 那一段的逐条结果回读一次。
2. **`B-276` 的画面影响没有观测到**（§二·4）：两个自适应模型下都不改画面，但**我没有在真机/编辑器里量过 `FontPxNow`**。
   这条的判据（标称值）是硬的，**画面差值是「预期 0」而不是「实测 0」**。
3. **`Alliance Badge Drawer Icon` 的真实宿主父框没查**：我按「`box` 的中心 = 原版父框的中心」建模，
   于是那个 `+44.6322` 落成「框心**上**移 44.63」。若某个调用点给它的 `box` 与原版那个父容器的矩形**不是同一个**，
   这 44.63 会整体偏。今天 `DrawerOverride.Icon` 的三个调用点（`CampaignTab` · `EventBadge` · `MissionInfoDisplays`）
   **没有一处是专门给它算的框**，而且**生产里有没有真实的 `AllianceTrophyData` + Icon 档组合也没核** ⇒ 这一档今天可能根本不可观测。
4. **`Title Drawer` 的 `Army` 垂直档没接**（原版 **Center/Bottom**，我们出厂 `Middle`）—— 见 §七·1。
5. `Title Drawer` 的 `Content/Image` 出厂**无图**、`Image Top` 是 **alpha 0.729 的静态件**（不是 `Draw` 产物）—— 与既有实现一致，本波只复核未改。

---

## 七、顺手发现（⛔ 都没改，交调度台分流）

1. 🔴 **`Title Drawer` 的 `Army` 垂直档是 `Center/Bottom`，我们画 `Middle`**（真包现读；对照 `Avatar Drawer` 是 `Center/Middle`）。
   同一份 prefab 里 `Content/Label/Name` 是 **`Center/Midline`**（我们也画 Middle）。
   **做法已经现成**：`MenuDraw.SetVAlign(Label lb, Label.VAlign.Bottom, PxRect r, Label.OrigFace face)`
   —— 但它要求拿到 `Label` 实例，而 `DrawerArmyName` 现在返回 `void` ⇒ 需要①把它改成返回 `Label`（对 20 多个既有调用点**源码兼容**），
   ②在 `Title(...)` 里拿到返回值后调一次。
   **为什么本波没做**：这会**第一次**把垂直档引入抽屉层，影响 108px 高框里那行字的落墨（≈36 设计 px 的位置差），
   而本波**不能跑 Unity 验证**（红线），写进去就是「照着推理改一个没人看过的像素位」。
   ⇒ 建议单开一条（判据已经有了，落点和做法就是上面这两步）。
2. **`Title Drawer` 的 `Content/Background` 那张图的 `m_PreserveAspect` 是 1，我们是拉伸（`keepAspect: false`）**。
   本次没动：sprite 329×499（0.65932）与框 712×1080（0.65926）比例差 0.009% ⇒ 拉伸与 PA 视觉等价（< 0.1 px）。
   ⚠️ 但这是「凑巧对」，换图就会露 —— 记在这里。
3. **`Icon` 档徽章的节点名**：原版根下那颗叫 **`Badge`**，我们建的是 `Trophy Icon`（`NodeTrophyIcon`，沿基础档 `Content/Frame/Trophy Icon` 的名字）。
   **不影响画面**，但会影响「按节点名查树」的自检与将来对 prefab 的逐节点核对。⛔ 本波没改（改名会打翻既有断言）。
4. **`ItemDrawerPartial_B111R3W.cs` 的注释 `无 ARF ⇒ Frame/Badge 的锚 (0,0)-(1,1) 就是整个根`** 只说对了一半
   （那两句只说明「相对根」，根自己既不是框也不是 1080²）—— `B-277` 就是被这半句误导的。建议那份文件的所有者顺手补一句。
5. `B114_W32` 报告 §六·3 的「我们现在走 `FrameOf(box, 1080, 1080)`」**不准确**（实际是 `frameR = box`，见 §三·2）。
   该报告是别人的产物，⛔ 我没改它，只在本报告里订正并写清错因。

---

## 八、给复核者的两分钟核对口

```bash
# 1) 三条的判据（真包现读）
python -I -X utf8 Unity/工具/menu_dump.py bundle_menus_assets_all "Title Drawer" --depth 4 --relative --no-ancestor-scale | grep -E "Background|Army |Title "
python -I -X utf8 Unity/工具/menu_dump.py bundle_menus_assets_all "Avatar Drawer" --depth 4 --relative --no-ancestor-scale | grep -E "Background|Army "
python -I -X utf8 Unity/工具/menu_dump.py bundle_menus_assets_all "Alliance Badge Drawer Icon" --depth 4 --relative
# 判据：Title 底板 = 40k_Rewards_icon_title card(329×499) · Title Army 字号 35.099998 · Avatar Army 字号 75.0
#       Alliance Icon 根 = 165.95×155.74（无 ARF）

# 2) prefab 根字段（固定尺寸那一档）
python -I -X utf8 - <<'PY'
import io,re
b=io.open(r'd:/2/tools/assetripper_out/ExportedProject/Assets/Alliance Badge Drawer Icon.prefab','rb').read().decode()
i=b.find('!u!224'); print(b[i:i+700])
PY

# 3) `ItemDrawer.cs` 零行数变化
wc -l Unity/MyGame/Assets/CardPresentation/Shell/ItemDrawer.cs          # 2497
git diff -U0 -- Unity/MyGame/Assets/CardPresentation/Shell/ItemDrawer.cs | grep '@@'   # 应见 @@ -1918 +1918 @@（另有 3 条是别人的）

# 4) 两个新文件 + 双 meta
ls -l Unity/MyGame/Assets/CardPresentation/Shell/ItemDrawerPartial_B114W35.cs* \
      Unity/MyGame/Assets/CardPresentation/Editor/B114_W35_Assert.cs*

# 5) 类型检查
TMPDIR=/tmp/wf_w35 bash Unity/工具/typecheck.sh     # 期望：运行时 0 / 编辑器 0
```
