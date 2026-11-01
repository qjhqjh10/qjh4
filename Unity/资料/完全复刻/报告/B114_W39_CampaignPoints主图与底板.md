# B114_W39 · `B-265`（基础档主图几何）+ `B-266`（基础档底板层）

> 写手 `W-39` · 2026-11-01 · 工作目录 `d:/4`
> 判据来源：`报告/B114_W27_CampaignPoints光圈.md` §九 顺手发现 1 / 2（原始提出）· 本波**回源逐字段复核**两份 prefab。
> 同族实现：`Shell/ItemDrawerPartial_B114W27.cs`（`B-258` 光圈，**未推翻**）· `Shell/ItemDrawerPartial_B114W12.cs`（`B-243`）。

---

## 一、判据（prefab 现读）

**两份 prefab 都亲读**，两个独立来源互证：① AssetRipper 导出的 YAML
（`d:/2/tools/assetripper_out/ExportedProject/Assets/*.prefab` 的 `!u!1 / !u!224 / !u!114`）；
② 真包 `python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Campaign Points Drawer" --depth 2 --relative`。

### 1·1 基础档 `Campaign Points Drawer.prefab`

| 件 | fileID | 现读值 |
|---|---|---|
| `Content/Image` RectTransform | `!u!224 &224017697238300914` | `m_AnchorMin (0, 0.4)` · `m_AnchorMax (1, 0.8)` · `m_SizeDelta (0,0)` · `m_Pivot (0.5,0.5)` · `m_LocalScale (1,1,1)` · `m_Father` = `Content` |
| ↳ 的 `Image` | `!u!114 &114217368724061974` | `m_Sprite {fileID: 0}`（**无出厂图**）· `m_Type 0`(Simple) · **`m_PreserveAspect 1`** · 白 · `m_Enabled 1` |
| `Content/Background` 的 `Image` | `!u!114 &114875609422105747` | `m_Sprite {fileID: 21300000, guid c8f9b1a41360ac845919c316ddae577b}` = `40k_Rewards_icon_base card`（328×498）· `m_Type 0` · **`m_PreserveAspect 1`** · 白 · `m_Enabled 1` |
| `ItemDrawerComponents` | `!u!114 &114302127217294661` | `background = {fileID: 114875609422105747}` ⇒ **真实节点**（不是 null） |
| `Campaign Glow`（`B-258` 已补） | RT `&224371939378208658` | `(0,0.4)-(1,0.8)` + `m_SizeDelta (62.5253,37.8941)` ⇒ 根设计 px `(-30.86,197.05)-(743.66,666.95)` |

`menu_dump` 现读同一份（对照行）：`Image  0.4  216.0  712.4  648.0  712.00  432.00 … preserveAspect`。

### 1·2 Icon 档 `Icon Campaign Points Drawer Variant.prefab`

| 件 | fileID | 现读值 |
|---|---|---|
| `Content/Image` RectTransform | `!u!224 &224313026266067498` | `m_AnchorMin (0, 0)` · `m_AnchorMax (1, 1)` · `m_SizeDelta (0,0)` · `m_LocalScale (1,1,1)` |
| ↳ 的 `Image` | `!u!114 &114632457200066912` | `m_Sprite {fileID: 0}` · `m_Type 0` · **`m_PreserveAspect 1`** |
| `ItemDrawerComponents` | `!u!114 &114985305603660343` | **`background = {fileID: 0}`** ⇒ 那一档**没有底板槽** |

---

## 二、活 1 · `B-265` —— 基础档主图几何

### 2·1 改前 vs 原版

| | 原版（prefab） | 我们（改前） |
|---|---|---|
| 基础档主图框 | `Content/Image` 锚 `(0,0.4)-(1,0.8)` + `sizeDelta 0` ⇒ 根设计 px **`(0.4,216)-(712.4,648)`**（712×432 的**横带**），PA + 256² 方图 ⇒ **432²** 居中在 `(356.4, 432)` | `Square(box, st.ArmyFill)` = **居中方块**（`min(W,H) × ArmyFill`），与锚点无关 |
| Icon 档主图框 | 锚 `(0,0)-(1,1)` + `sizeDelta 0` ⇒ 铺满 1080² `Content`，PA ⇒ **1080²** | `Square(box, ArmyFill)` —— **只有调用点给 `ArmyFill = 1` 时才恰好等价** |

⇒ 同一份代码在两条调用路径上主图**大小还不一样**：`CampaignTab.CampaignPointStyle()` 给 `1f`、
`ItemDrawerStyle.Default()` 给 `0.34f`，而**两条都不是原版的 432²**（基础档）。

### 2·2 改成什么

新文件 `Shell/ItemDrawerPartial_B114W39.cs`：

```csharp
internal static PxRect CampaignPointsImageRect(PxRect box, ItemDrawerStyle st)
{
    if (st.CampaignPointsGlowIcon)                                   // Icon 档：Content 1080² 铺满
        return FrameOf(box, DrIconRoot, DrIconRoot).Map(0f, 0f, DrIconRoot, DrIconRoot);
    var fr = FrameOf(box);                                           // 基础档：Content 里 216..648 的横带
    return fr.Map(DrCx1, DrImgY1, DrCx2, DrImgY2);
}
```

- 复用 `DrCx1/DrCx2`（`Content` 左右沿 `0.4 / 712.4`）与 `DrImgY1/DrImgY2`（`216 / 648`）——
  **这两组数就是 `(0,0.4)-(1,0.8) × 1080` 算出来的**（`1080 − 0.8×1080 = 216` · `1080 − 0.4×1080 = 648`），
  `Editor/B109_E11_Drawer_Assert.cs:155-156` 也把这组当作「`Content/Image` 的默认矩形」在断 ⇒ 不是巧合、是同一个规则。
- 画仍走调用点那句 `MenuDraw.Rect(..., keepAspect: true, ...)`：`m_PreserveAspect = 1` 那一档由它负责，
  本文件只负责**给对框**（框给错时 PA 救不回来）。
- `ItemDrawer.cs:2197` 的 `Square(box, st.ArmyFill)` → `CampaignPointsImageRect(box, st)`（**同物理行**）。
  ⇒ `st.ArmyFill` 在本抽屉**不再参与几何**（调用点仍传值，行为不再受它影响）。

### 2·3 主图前后值（逐个真实/夹具站点）

| 站点 | box | ArmyFill | 改前 | 改后 |
|---|---|---|---|---|
| `CampaignTab` 高级面板（真实） | 65 × 69.73 | **1f**（`CampaignPointStyle`） | 65² | **65²（不变）** |
| `CampaignRewardWindow.BuildItem`（基础档） | 200 × 300 | 0.34（`Default`） | 68² @ `(66,116)-(134,184)` | **120² @ `(40,60)-(160,180)`** |
| `B97_SHOP_Assert` 夹具 | 60 × 70 | 0.34 | 20.4² | **60²** |
| `B114_W27_Assert` ② 基础档夹具 | 712.8 × 1080 | 0.34 | 242.35² | **432² @ `(140.4,216)-(572.4,648)`** |
| `B114_W27_Assert` ① Icon 夹具 | 1080² | 0.34 | 367.2² | **1080²** |
| `B114_W27_Assert` ③ 端到端夹具 | 65 × 69.73 | 0.34 | 22.1² | **65²** |

（基础档 200×300 的算式：`K = min(300/1080, 200/712.8) = 0.27778` ⇒ 横带 `197.78×120` ⇒ PA 后 `120²`，中心 `(100,120)`。）

---

## 三、活 2 · `B-266` —— 基础档底板 `Content/Background`

### 3·1 🔴 与 `B-256` 的判据区别（必须写清）

- **`B-256`** = 「**槽存在，但目标 `m_IsActive: 0`**」⇒ 原版**不画**（uGUI 判 `activeInHierarchy`）。
- **`B-266`（本条）** = 「**槽指向真实节点，且原版真的画**」：
  `background = {fileID: 114875609422105747}`（真实 `Image`）· 该节点 `m_IsActive 1` · 组件 `m_Enabled 1` · `m_PreserveAspect 1`。
- 反方向：**Icon 档 `background = {fileID: 0}`** ⇒ 那一档**不画**（同族结论先例 = `Editor/B114_C_Assert.cs:121`：
  「`background` 槽是 null ⇒ 不建 `Background`」）。

### 3·2 改成什么

```csharp
internal static bool DrawCampaignPointsBackground(Transform node, PxRect box, ItemDrawerStyle st)
{
    if (node == null) return false;
    if (st.CampaignPointsGlowIcon) return false;      // Icon 档 background: {fileID: 0} ⇒ 原版不画
    return DrawerBackground(node, FrameOf(box), st);  // 复用共用件：取不到图 ⇒ Note 出声一次、不画
}
```

- **复用 `ItemDrawer.DrawerBackground`**（`ItemDrawerLayout.cs:200`）—— 画的就是 `Content` 整块
  （`fr.Map(DrCx1, 0, DrCx2, DrRootH)`），而 `MenuDraw.Rect` 吃的是**绝对 px**（`Local(parent, …)` 自己换算成局部）
  ⇒ 直接画在抽屉根 `node` 上与原版挂在 `Content` 下**等价**（这一族本来就没有 `Content` 包装层，`B-258` 的光圈也是这么画的）。
- 调用点 = `ItemDrawer.cs:2180`（`{` 那一行，**同物理行**、第一个 `//` 之前），
  **建层次序照 prefab sibling 序** `Background → Campaign Glow → Image`。
- 队列：`background = st.QBoard` < `Campaign Glow = QBoard+1` < `Image = max(QArt, QBoard+2)`
  ⇒ 与 `B-258` 已定的那两个号**自动串成 0/1/2 的次序**，没有新增常量。

### 3·3 底板前后值

| | 改前 | 改后 |
|---|---|---|
| 基础档 `Background` 节点 | **不存在** | 存在，框 = `Content` = 根设计 px `(0.4,0)-(712.4,1080)`（712×1080） |
| 基础档队列次序 | （无这一层） | `QBoard < QBoard+1 < max(QArt, QBoard+2)` |
| Icon 档 `Background` 节点 | 不存在 | **仍然不存在**（`{fileID: 0}`） |

⚠️ **已知亚像素差（如实记）**：原版这颗 `Image` 是 `m_PreserveAspect = 1`，而共用件 `DrawerBackground` 走
`keepAspect: false`（**全库每一份抽屉都是这一条**）。这张图 328×498 与 `Content` 712×1080 的宽高比只差 **0.1%**
（`0.65863` vs `0.65926`）⇒ 实画宽度差 **≤ 0.7px**。要改就得动共用件、影响全部抽屉，⛔ 不在本账范围内。

---

## 四、`B114_W27_Assert` 的连带改动（含前后值）

> 验收第 2 条要求：活 1 会牵动 W-27 那条端到端断言 ⇒ 必须一起核。

### 4·1 查证结论：`C3`（光圈宽 = 97.5）**不受影响**，值不变

`C3` 量的是 `Campaign Glow`，它的框来自 `FrameOf(box, DrIconRoot, DrIconRoot).Map(CpGlowIconX1..Y2)`
⇒ `1620 × (65/1080) = 97.5`，**与主图框无关**（主图只影响 `ImageQuad` 的矩形，不参与光圈的 `Map`）。
本波**没有改** `ItemDrawerPartial_B114W27.cs` 一行。

### 4·2 但端到端那一格的**主图**确实变了（22.1 → 65），已补断言并写清前后值

`SectionRewardHelperIsIcon`（③ 段）新增：

| 断言 | 内容 |
|---|---|
| `C5` | 硬前提：主图节点建出来了 |
| `C5b` ★★ | 主图宽 = `min(65,69.73) = 65`。**前后值**：本夹具（`Default` ⇒ `ArmyFill 0.34`）**改前 22.1** ⇒ **改后 65**；真实站点 `CampaignPointStyle()` 给 `ArmyFill = 1f` ⇒ 那边**改前改后都是 65**（不变） |
| `C5c` ★ | 方图 PA ⇒ 宽 == 高 |

### 4·3 新增两段（`Run()` 里排在 ④ 之后）

| 段 | 盯什么 | 🧨 怎么才能红 |
|---|---|---|
| **⑤ `B-265`**（`SectionMainImage`） | `E0` 硬前提图 · `E1/E1b/E2` 前提 · `E3×4` 基础档框 `(140.4,216)-(572.4,648)` · `E3b` 宽 432 · `E3c` 宽==高 · **`E4` 灭自证**（432 ≠ 旧写法两种 `ArmyFill` 的 712.8 / 242.35）· `E5/E5b/E6` 前提 · `E7×4` Icon 档框 `(0,0)-(1080,1080)` · `E7b` 宽 1080 | 回退成 `Square(box, st.ArmyFill)` ⇒ E3/E4/E7b 红；把 Icon 支也写成分帧根 ⇒ E7 红 |
| **⑥ `B-266`**（`SectionBaseBackground`） | `F0` 硬前提图 · `F1` 前提 · `F1b` 基础档**建出了** `Background` · `F2×4 + F2b×2` 框 = `Content`（`(0.4,0)-(712.4,1080)`）· `F3` 用的是 `background` 槽那张图 · `F4a` 前提 · `F4` 次序 `Background < Glow < Image` · `F5` 前提 · **`F5b` 两档判别**（Icon 档**不建** `Background`） | 删掉 `CampaignPoint` 里那句调用 ⇒ F1b 红；把 `CampaignPointsGlowIcon` 那道闸删掉（两档都画）⇒ **F5b 红而 F1b/F2 仍绿** —— 两组**结构上**不可能被一个错的实现同时满足 |

🆕 共 **+36 条**断言（③ 段 **3** 条 + ⑤ 段 **19** 条 + ⑥ 段 **14** 条；含硬前提，⛔ 没有 `if (前提) Check(...)` 这种静默跳过）。
文件头另加一行，注明本波追加了哪几段（审计用）。

---

## 五、改动清单 + 净增净减

| 文件 | 改动 | 净行数 |
|---|---|---|
| `Shell/ItemDrawerPartial_B114W39.cs`（**新建**） | `CampaignPointsImageRect` + `DrawCampaignPointsBackground` + 判据/口径/已知差全文 | **+86**（新文件） |
| `Shell/ItemDrawerPartial_B114W39.cs.meta`（**新建**） | guid `3f9c1d47b2a84e6d9c05f1a7b6d24e83`（全仓 grep 0 命中） | 新文件 |
| `Shell/ItemDrawer.cs` | **7 行同物理行改写**：`2178`（doc 去掉「自己设计」）· `2180`（`{` 后追加 `DrawCampaignPointsBackground`）· `2187`（🔴 就地订正「出厂 sprite」那句，见 §八-1）· `2188`（几何注释改成两档）· `2197`（`Square(...)` → `CampaignPointsImageRect(...)`）· `2198/2199`（把 🔶 自己设计那段换成「照 prefab」+ 判据指针） | **0**（2497 → 2497；`git diff --numstat` = `8 / 8`，其中 `1918` 是别的写手的、`2237` 是 `W-27` 的，**都不是本波**） |
| `Editor/B114_W27_Assert.cs` | ③ 段补 `C5/C5b/C5c` · `Run()` 加两行调用 · 新增 ⑤⑥ 两段 · 文件头加一行 | **+220**（316 → 536） |

**行号纪律自检**（`python -I` 二进制读）：
- `ItemDrawer.cs`：`LF 2497 / CRLF 0 / 裸 CR 0`，**回到开工时的 2497**。
- 新 `.cs`：纯 LF、无 BOM、裸 CR 0。新 `.meta`：与仓内既有 `.cs.meta` 同格式。
- 每一处 `.cs` 的追加都在**该行第一个 `//` 之前**（逐行现读确认）。

---

## 六、验收（本波实跑）

| 项 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_w39 bash d:/4/Unity/工具/typecheck.sh` | **运行时 0 / 编辑器 0** |
| `python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py`（只读，未加 `--fix`） | **A 5841 · 通过 5841 · 漂移 0 · 失败 0**（与开工基线逐值相同） |
| 行尾 / 行数 | 见 §五 |

---

## 七、没查清的部分（如实记）

1. **基础档那条路今天有没有消费者，仍未证死**：战役点要作为 item 流过 `ItemDrawer.Draw(..., Default, st)`
   才走得到（`CampaignRewardWindow.BuildItem`）。本波只证「代码路径存在 + 几何照 prefab」，
   **没**去证「某条战役节点奖励真的发战役点 item」（沿用 `W-27` §八-1）。
2. **非方框 / 窄容器下 `FrameOf` 的行为**仍是 `ItemDrawerLayout.cs` 文件头 ① 那条 TODO
   （原版 `AspectRatioFitter` 在宽不够时是溢出还是按宽缩**没读过**）—— 两段新断言都只跑**方框 / 整帧**夹具。
3. **`Campaign Tab` 那颗格子今天真的跑不到**（`LocalPremiumState()` 恒 2 ⇒ `ChangeState(2)` 关掉整层）
   ⇒ 本波的可视验收仍只能靠断言，**没有实拍**。
4. **底板那 0.7px 的 PA 差**（§3·3）没有照原版做，理由写在代码注释里；要不要改是**共用件 `DrawerBackground` 全库**的事。
5. **本波没跑 Unity**（红线）⇒ `A*/E*/F*` 全是静态推导 + prefab 现读，没有实跑读数；红了先查 `Resources/Art/` 与 `FrameOf` 的根假设。

---

## 八、顺手发现（⛔ 都没改别人的文件，交调度台分流）

1. 🔴 **`ItemDrawer.cs:2187` 旧注释是错的 —— 已就地订正（铁律 5，同物理行）**：
   原句「那张 `Image` 的出厂 sprite 恰好是 `40K_genearl_icon_Campaign points_big`」**不成立**。
   现读：`ItemDrawerComponents.image` 指的 `Image`（基础档 `&114217368724061974` / Icon 档 `&114632457200066912`）
   **`m_Sprite = {fileID: 0}`（两档都无出厂图）**，`menu_dump` 现读也印 `<无图>`。
   那张 `40K_genearl_icon_Campaign points_big`（guid `d2b2111c…`）是**同级 `Campaign Glow`** 的图，被记串了。
   ⇒ `DrawForArmy` 是从零填这颗图，不是「换掉出厂图」；旧结论的后半句（「原来按 `GetIcon(true)` 画 = 画错」）**仍然成立**。
2. **`CampaignTab.cs` 的 `CampaignPointStyle()` 里 `st.ArmyFill = 1f` 已成死值**（注释还写着「徽记铺满 min(W,H)」）——
   `CampaignPoints` 现在两档都从 `Content/Image` 锚点算框，`ArmyFill` 不再参与几何。**不是错误**，是那处注释过期了。
   `Shell/CampaignTab.cs` **不在我的白名单** ⇒ 交调度台派给该文件的所有者。
3. **`账/战役.tsv:53` 的残差 ①** 写着「三层矩形（`IconFill`/`ArmyFill`/`NameStrip`）照同族 `Wildcard`、**不是从 prefab 量的**」
   —— 其中**主图那一层（`ArmyFill`）本波已改成照 prefab 量**；`IconFill` 这条抽屉不用；`NameStrip`（阵营名条）仍是我们的
   （但本族 `st.NamePx = 0` ⇒ 不画）。**账不由我改**，请调度台按此更新残差表述。
4. **`B-266` 与 `B-256` 不是同一条**（已在 §3·1 写清）：`B-256` = 「有槽但 `m_IsActive: 0`」原版不画；
   本条 = 「有槽且原版真的画」。⚠️ 同一份 `ItemDrawerComponents` 上还有 `label = {fileID: 114291844617409189}` /
   `quantity = {fileID: 114518489005915545}`（基础档非 null，但 `Army` / `Label` 那两层今天由别的账管）——
   我没动它们，只是提示：**基础档这几个槽都不是 null，Icon 档全是 `{fileID: 0}`**，是这一族「两档」的通用判别面。
5. **`ItemDrawer.cs:1918` 在本工作树里已被别的写手（`W-32`/`W-35`/`W-38` 那一族）改过**（Icon 变体分流那句）。
   本波**没碰**那一行；`ItemDrawer.cs` 行数仍是 2497，我的改动全在 `2178/2180/2187/2188/2197/2198/2199`。
   ⚠️ 并发提示：`ItemDrawer.cs` 今天不止一个写手，合并时请以「同物理行替换、行数守恒」为准（本波已满足）。
