# B114_W48 · `B-292`（数量格的第二个尺度来源）+ `B-294`（Icon 变体的根比例）

> 第 114 会话 · 写手 `W-48` · 2026-11-01。
> ⛔ 未跑 Unity（`-executeMethod` 全禁）· ⛔ 未动 git · ⛔ 未改账 / 正本 / 场景 / `Editor/ShellScene.cs` / `Editor/BattleScene.cs` / `Shell/ItemDrawer*.cs`（`ItemDrawerLayout.cs` 只改了**文件头注释**）。
> **typecheck（`TMPDIR=/tmp/wf_w48`）= 运行时 0 / 编辑器 0** · **账闸 = A 5841 通过 · 漂移 0 · 失败 0**（与开工前逐字相同）。

---

## 〇、三句结论

| # | 结论 |
|---|---|
| **1（`B-292`）** | **判定「该跟 ARF 对齐」，已实现。** `BuildAmount()` 原来用 `ItemDrawer.FrameOf(ItemBoxRect())` ⇒ 默认 `Contain`；而同一个 `ItemBoxRect()` 画的是 `Cardback Drawer` 那个抽屉（根 `AspectRatioFitter` = **`m_AspectMode 2` 高控宽 · `m_AspectRatio 0.66`**）⇒ 两个尺度来源。**反证「数量格本来就该内接」不成立**：这一格的父格子就是那个抽屉，原版没有「抽屉按 ARF 溢出、而它的数量格按内接缩进框里」这条路（ARF 只作用在**根**上，`Content/Label` 是根的后代 ⇒ 与根同一次缩放）。 |
| **2（`B-292` 实现）** | 新增**唯一入口** `ItemDisplayWindow.AmountCellOf(PxRect box)`（`public`，返回 `AmountCell{ K, Rect }`），`BuildAmount` 只从它取 `K` 与 `Rect`。**只改一行代码 + 一行实现、其余同物理行**；新增块全部追加在**类尾**（`ItemDisplayWindow.cs` 538 → 573 行），账里那 10 个落点行号**一个没动**。 |
| **3（`B-294`）** | `ItemDrawerLayout.cs` 文件头「Icon 变体 = 1080²」**已就地订正**成「按 prefab 分三档」（7 份 ARF 2·1 / 4 份 ARF 2·0.66 / 2 份 ARF 3·FitInParent），并写死「谁要按 1080² 写代码，先按 prefab 名查这张三档表」。**13 份逐份现读复核：`B114_W47` 的 7/4/2 分类是对的。** |

---

## 一、活 1 · `B-292`：`ItemDisplayWindow` 数量格 = 抽屉本体的同一条 ARF 口径

### 1·1 为什么是「对齐 ARF」而不是「数量格本来就该内接」

| 问题 | 判据 |
|---|---|
| 这一格属于谁？ | `Item Holder` 里那个抽屉 —— `ItemDisplayWindow.cs` 文件头已写死：**`Cardback Drawer`**，根 `AspectRatioFitter(m_AspectMode = 2 高控宽, m_AspectRatio = 0.66)`（26 份 prefab 逐份同值）。数量格 = 原版 `ItemDrawer<T>.Draw` 的第 ③ 步（`decomp_generic_trial/out/ItemDrawer_object___Draw.c`），它的 `Content/Label/Quantity` 就在**那个抽屉的根之下**。 |
| 原版那一刻根怎么算？ | `m_AspectMode 2` 只有一句 `SetSizeWithCurrentAnchors(Horizontal, rect.height * m_AspectRatio)`，**完全不看父框宽**（UGUI `AspectRatioFitter.cs:151-156`）⇒ 根高 = 框高、根宽 = 高 × 0.66。 |
| 内接这条路存在吗？ | **不存在。** ARF 只作用在**根**上；`Content/Label` 是根的后代，跟着根的 `UIScaleToFit(fitInParent)` 走（对基础档恒 = `H/1080`）。所以「根按 ARF 溢出、数量格却按内接缩进框里」= 我们自己的两条口径，**原版没有这一支**。 |
| 今天为什么看不出来？ | `ItemBoxRect()` = 429×650，比例 **正好 0.66** ⇒ `Contain` 的 `min(650/1080, 429/712.8)` 与 ARF 的 `650/1080` **逐值相等**。框一变（更窄）就分家。 |

⇒ **判定：改。** 反证不成立（原版没有「数量格独立内接」的机制），而两边分叉时**只有 ARF 那一支与原版一致**。

### 1·2 改动（`Shell/ItemDisplayWindow.cs`）

| 位置 | 改法 | 净行数 |
|---|---|---|
| `:400`（doc） | `<see cref="AmountCellOf"/>`（= `FrameOf(ItemBoxRect(), fit: RootFit.HeightControlsWidth)`）—— **同物理行改写** | 0 |
| `:408~:412`（`BuildAmount` 体） | `var fr = ItemDrawer.FrameOf(ItemBoxRect());` + `var m = …; var r = fr.Map(…);` + 4 处 `fr.K` ⇒ `var cell = AmountCellOf(ItemBoxRect()); var r = cell.Rect; float k = cell.K;` + 4 处 `k`（**3 行进、3 行进，逐行一一对应**） | 0 |
| 类尾（`:537` 之后） | 新增 `public struct AmountCell { float K; PxRect Rect; }` + `public static AmountCell AmountCellOf(PxRect box)`（**唯一尺度来源**，内含 `fit: RootFit.HeightControlsWidth`） | **+35**（538 → 573） |

- 为什么放**类尾**：本文件被账按行号钉着（`菜单_外壳.tsv` 的 `:130 / :150 / :159 / :191 / :266 / :288 / :296 / :394 / :406 / :532`，另有 `收藏.tsv:271` 的 `ItemDisplayWindow.cs:150/159`）—— 插在字段区或 `BuildAmount` 之前会全顶下去。
- 为什么 `AmountCellOf` 是 `public`：**断言宿主在编辑器程序集**里，本仓**没有** `InternalsVisibleTo`（先例 → `Battle/CardDisplayWindow.cs:1787` 的记录）⇒ 不能返回 `internal` 的 `ItemDrawer.DrawerFrame`。它返回的是我们自己的 `public struct AmountCell`。
- **口径边界**：本支只对「根 `m_AspectMode 2`」的抽屉成立。今天 `Context` 里**没有抽屉名这一格**，唯一生产入口 `ForCardback` 指向 `Cardback Drawer`（ARF 2 · 0.66）。若将来这扇窗要显示 mode 3 / 固定尺寸那几档，**必须按抽屉名分档**（已在方法 doc 里写死）。

### 1·3 断言（新建 `Editor/B114_W48_Assert.cs` + `.meta`）

期望值**手算**（⛔ 不调 `FitK`/`FrameOf`/`Metrics` 反读），夹具是**非 0.66 的框**：

| 条 | 断什么 | 值 |
|---|---|---|
| A0 / A0b | **前提**：窄框夹具有效、算出的 `K` 是正常正数 | 400×650 · K>0 有限 |
| **A1 ★★★** | 窄框下 `K` = **框高/根高**（原版 ARF，不看框宽） | **0.6018519**（650/1080） |
| **A2 / A3** | 那一格宽 / 高 | **174.7778**（290.4×K）· **65**（108×K） |
| **A4 / A5** | 左沿 / 上沿（根左上 = 85.5 / 50） | **122.213** · **537.5** |
| **A6 ★★★** | **灭自证**：实测 `K` 必须**远离**等比内接的 `400/712.8` | 差 **0.0407**（> 0.03）｜🧨 删掉 `fit:` ⇒ A1/A6 同时红 |
| **A7 ★★★** | **灭自证·结构**：两口径格宽差 | **11.81 px**（> 10，不是浮点噪声） |
| **A8 / A9** | 对照：宽框（1200×650）下两支**相同**（分叉只在「框比根更窄」一侧） | K 仍 0.6018519 · 格宽同 174.7778 |
| **A10 ★★** | **灭自证**：框换 400×700 ⇒ `K` 必须跟着变（挡「恒返回固定值」） | **0.6481481**（700/1080；内接会给 0.5611672） |
| B0~B2b | **前提**：窗建得出、数量格建得出、**它挂在 `Item Holder` 下** | —— |
| **B3 ★★** | **端到端**：生产窗里那一格的字号 = `75 × K` | **45.1389 px**（`GlyphHeightWorld × 108`） |
| B4 | 渲出来的字号粗检（自适应上限 = 75×K） | (30, 60) |
| B5 | 那一格写的是 `Amount` | `"3"` |

**🧨 灭自证怎么才能红**（写进文件头）：把 `AmountCellOf` 里的 `fit: ItemDrawer.RootFit.HeightControlsWidth` 删掉（回默认 `Contain`）⇒ **A1 / A2 / A4 / A6 / A7 / A10 同时红**；把「跟着框高」改成「按框宽」⇒ **A8 / A9 红**；把 `K` 写死 ⇒ **A10 红**。

**⚠️ 这一节能证到哪 / 证不到哪（如实记）**：生产窗的框是**常量** `ItemBoxRect()` = 429×650（比例正好 0.66），两支在这里**逐值相等** ⇒ 「`BuildAmount` 是否还调 `AmountCellOf`」**端到端抓不到**（有人把那一行改回内联 `Contain`，端到端仍绿）。能证的是：① `AmountCellOf` 在**任意框**上都是 ARF；② 生产窗真的从它取 `K`（B3 的字号）。真要端到端可判别，得给窗加「框可注入」的测试口 —— 本批**没加**（会让生产 API 为测试变形，且 `Item Holder` 是 prefab 常量）。

**怎么接（只由调度台改宿主）**：`Editor/ShellScene.cs` 那条聚合行 **同一物理行、第一个 `//` 之前**追加：

```csharp
B114_W48_Assert.Run(); _sink.Pass += B114_W48_Assert.Pass; _sink.Fail += B114_W48_Assert.Fail;
```

---

## 二、活 2 · `B-294`：`ItemDrawerLayout.cs` 文件头的「Icon 变体 = 1080²」

### 2·1 原文错在哪

原文（改前 `:14-15`）：

```
//    · Icon 变体（`Icon Title Drawer Variant` / `Icon VIP Premium Campaign Drawer Variant` /
//      `Icon Forge Points Drawer Variant`）：根 **1080×1080**、ARF **1**，`Content` 1080×1080 ⇒ 用 `FrameOf(box, 1080, 1080)`
```

三处不成立：① 只点了 **13 份里的 3 份**；② **根比例不止一种**；③ 那 7 份「比例 1」的 `Content` **序列化并不是 1080×1080**（是锚 `0..1` **拉伸**到正方形根上；`1080` 只是我们取的设计边长）。

### 2·2 逐份现读（13 份 → 三档）

> **判据口径（关键，见 §五·1）**：读 `d:/2/tools/assetripper_out/ExportedProject/Assets/<prefab>.prefab` 里**根**那个 GameObject（`m_Father: {fileID: 0}` 的 Transform 所指）上的 `!u!114`。

| 档 | 份数 | prefab | 根 ARF | 我们的根/口径 |
|---|---|---|---|---|
| **(a)** | **7** | `Icon Avatar` · `Icon Container` · `Icon Currency` · `Icon Forge Points` · `Icon Title` · `Icon VIP Premium Campaign` · `Icon Wildcard` | **enabled 1 · mode 2 · ratio 1** | `FrameOf(box, DrIconRoot, DrIconRoot, RootFit.HeightControlsWidth)` |
| **(b)** | **4** | `Icon Avatar Border` · `Icon Expansion Pass Premium` · `Icon Premium Campaign` · `Icon Random Card` | **enabled 1 · mode 2 · ratio 0.66** | **默认根** `FrameOf(box, fit: RootFit.HeightControlsWidth)`（⛔ 别传 1080） |
| **(c)** | **2** | `Icon Campaign Points` · `Icon Expansion Pass Points` | **enabled 1 · mode 3（FitInParent）· ratio 1** | `FrameOf(box, DrIconRoot, DrIconRoot)`（= `Contain`，⛔ 别迁） |

补充（同一份 prefab 现读）：
- (a) 7 份的 `Content` 全是锚 `(0,0)-(1,1)` + `sizeDelta 0` ⇒ **拉伸铺满**正方形根；
- (c) 2 份的 `Content` 是 `sizeDelta 1080×1080` + 锚 `(0.5,0.5)`，根锚 `(0,0)` + `sizeDelta 0×0`；
- (b) 里 `Icon Avatar Border` / `Icon Expansion Pass Premium` 的 `Content` 是 `712×1080`，`Icon Random Card` / `Icon Premium Campaign` 是拉伸 —— 都是基础档那套 712.8×1080 设计空间。

### 2·3 改法

只改 `Shell/ItemDrawerLayout.cs` **文件头**（该文件在账 `*.tsv` **0 命中**，加行不触漂移）。写成三档表 + 判据口径 + 「谁按 1080² 写代码先查表」，并保留铁律 5 的更正痕迹（「2026-11-01 `B114_W48` 就地订正 … 原来写 X，实际是 Y，错因是 Z」）。逐处落点指回 `报告/B114_W47_ARF迁移.md` §一 / §二（那 26 处已经按 prefab 迁完，本批**不改任何调用点**）。

> **净行数**：`ItemDrawerLayout.cs` 357 → **377（+20）**，全在文件头注释里。

---

## 三、验收读数

```bash
TMPDIR=/tmp/wf_w48 bash d:/4/Unity/工具/typecheck.sh
#   --- 运行时程序集 ---  运行时错误数: 0
#   --- 编辑器程序集 ---  编辑器错误数: 0

python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py
#   合计：A 5841 条 · 通过 5841（其中告警 859 条）· 漂移 0 · 失败 0
#   B 档：B 4 条 · 落点问题 2 条（[PROSE] 2 条，与开工前逐字相同）
```

**行号纪律自检**（python 二进制读）：

| 文件 | 行数（前 → 后） | 行尾 |
|---|---|---|
| `Shell/ItemDisplayWindow.cs` | 538 → **573**（+35，全部在类尾 `:537` 之后） | 纯 LF · 裸 CR 0 |
| `Shell/ItemDrawerLayout.cs` | 357 → **377**（+20，全在文件头） | 纯 LF · 裸 CR 0 |
| `Editor/B114_W48_Assert.cs` | 新建 **209** | 纯 LF · 裸 CR 0 |

账里那 10 个 `ItemDisplayWindow.cs` 落点（`:130 :150 :159 :191 :266 :288 :296 :394 :406 :532`）**逐行现读仍是原来那一句**（`Context` / `Amount` / `ShowAmount` / `ForCardback` / `Open` / `Build` / 空上下文闸 / `}` / `BuildAmount` / `IsEmptyContext`）—— 这也是 `:408` 那三行必须「逐行一一对应地替换」的原因。

---

## 四、没查清的部分（如实记）

1. 🔴 **新断言没被执行过** —— 本波红线禁止跑 Unity。请调度台在同步点把 `B114_W48_Assert.Run()` 接进 `ShellScene.cs` 的聚合行后跑一次 `ShellScene.Run`，看有没有 `[B114W48]` 那一段的 A0~B5。
2. 🔴 **「生产框恒 0.66」这条限制让端到端抓不到「`BuildAmount` 是否还调 `AmountCellOf`」**（见 §一·3 末尾）。已写进断言文件头；要不要为它加「框可注入」的测试口，**请调度台定夺**（我认为不值得：`Item Holder` 是 prefab 常量，加口只会让生产 API 为测试变形）。
3. **本窗的 `Item Holder` 卡背图本身**（`MenuDraw.Rect(_holder, _ctx.Art, box, …, keepAspect: true)`）走的是 `CardbackFace` 两段式，**没有复刻 `Cardback Drawer` 内部的 `Content`/`Image` 二级几何**（文件头早就标了「近似」）—— 本批只收口了**数量格的尺度来源**，没有动那块近似。
4. `Context` 里**没有「抽屉名」这一格** ⇒ `AmountCellOf` 无法像 `ItemDrawer.Draw` 那样按 prefab 分档。今天唯一生产入口是 `ForCardback`（`Cardback Drawer`）所以安全；**若将来接别的抽屉族，这是必须先补的一格**（已写进方法 doc 的「口径边界」）。

---

## 五、顺手发现（⛔ 都没改，交调度台分流）

1. 🔴🔴 **`assets_full` 里的同名 GameObject **不是** prefab 根 —— 有 30+ 个嵌套副本，按它读 ARF 会读错档。**
   我先按「`GameObject/Icon Campaign Points Drawer Variant.json`」读，得到 **mode 2**（与 `B114_W47` 的「mode 3」冲突）；再按 `assetripper_out/ExportedProject/Assets/<prefab>.prefab` 里 **`m_Father: {fileID: 0}` 的那个根** 读，才是 **mode 3**。
   实测规模：`Icon Campaign Points Drawer Variant` 在 `assets_full/bundle_menus_assets_all/GameObject/` 下有 **36 份**同名文件（34 份 mode 3 + 2 份 mode 2，真根不在这 36 份里），`Icon Premium Campaign Drawer Variant` 有 **19 份**（含 3 份 ARF `m_Enabled = 0`）。
   ⇒ **判 prefab 根上的组件，一律走 `assetripper` 的 `.prefab` + `m_Father: 0`**；`menu_dump` 也可能抽到副本（它对 `Icon Campaign Points Drawer Variant` 打的是「高控宽(1)」，与真正的根 mode 3 不符 —— 建议顺手核一下 `menu_dump` 的 prefab 解析口径）。
   **结论：`B114_W47` 那份 7/4/2 是对的**，我一开始的「好像错了」是被副本骗的。
2. **`Icon ... Drawer Variant` 的 `Content` 几何有三种写法**（同一批 prefab 里混着）：锚 `0..1` 拉伸（7 份 (a) 全这样 + (b) 的 `Random Card` / `Premium Campaign`）· `sizeDelta 712×1080` 锚 `(0.5,0.5)`（(b) 的 `Avatar Border` / `Expansion Pass Premium`）· `sizeDelta 1080×1080`（(c) 两份）。我们现在的 `FrameOf` 把它们都折算成「设计空间 + K」，**结论没变**，但「`Content` 都是 1080×1080」这类说法以后别再写（原文件头就是这么错的）。
3. **`ItemDisplayWindow.cs` 文件头那行「26 份 prefab 逐份同值」**（讲 `Cardback Drawer` 的 ARF）**成立**，可直接当 `B-292` 的判据锚：`m_AspectMode = 2`、`m_AspectRatio = 0.66`。
4. **`B114_W47` §七·1 那条建议的改法与本次实测一致**（`fit: …HeightControlsWidth` + 「若显示 mode 3 / 固定尺寸要按 drawer 名分档」）；本批把那条建议落成了**唯一入口 + 断言**，并把「没有 drawer 名这一格」写成显式的口径边界。
