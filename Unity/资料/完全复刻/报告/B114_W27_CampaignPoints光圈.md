# B114_W27 · `B-258`「`Campaign Points` 两档的 `Content/Campaign Glow`」（执行报告）

> 写手 `W-27` · 2026-11-01 · 工作目录 `d:/4`
> 判据来源：`报告/B114_W20_抽屉0.4与三档光圈.md` §六·1（原始出处）· 本次**回源逐字段复核**了两份 prefab。
> 同族实现：`Shell/ItemDrawerPartial_B114W12.cs`（`B-243`，`Expansion Pass Points` 那一族）。

---

## 一、原版判据（prefab 现读 · `d:/2/tools/assetripper_out/ExportedProject/Assets/`）

两份 prefab 各读 `!u!224`（RectTransform）与 `!u!114`（`ItemDrawerComponents`/Image），**逐字段值如下**（本波亲读，不是转述）：

| 项 | `Campaign Points Drawer.prefab`（基础档） | `Icon Campaign Points Drawer Variant.prefab`（Icon 档） |
|---|---|---|
| `Content` 尺寸 | **712 × 1080**（锚 0.5/0.5，居中在 712.8 宽的根里 ⇒ 左沿 `DrCx1` = 0.4） | **1080 × 1080** |
| `Campaign Glow` 在 `Content.m_Children` | 第 **1** 位（`Background` 之后、`Image` 之前） | 第 **0** 位（`Image` 之前） |
| `anchorMin / anchorMax` | `(0,0.4) / (1,0.8)` | `(0,0) / (1,1)` |
| `m_SizeDelta` | `(62.5253, 37.8941)` | `(0, 0)` |
| `m_AnchoredPosition` / `m_Pivot` | `(0,0)` / `(0.5,0.5)` | `(0,0)` / `(0.5,0.5)` |
| `m_LocalScale` | **1** | **1.4999999** |
| ⇒ 布局框（根设计 px · 左上原点 · y 向下） | **`(-30.86265, 197.05295)-(743.66265, 666.94705)`**（774.5253 × 469.8941） | **`(-270, -270)-(1350, 1350)`**（1620² 过扫） |
| `m_Sprite` | guid `d2b2111cc38ea6542aa8031e1578f84f` = `40K_genearl_icon_Campaign points_big`（256×256） | **同一张图**（同 guid） |
| `m_PreserveAspect` / `m_Type` / `m_Color` / `m_RaycastTarget` | `1` / `0`(Simple) / 白 / `1` | `1` / `0` / 白 / `1` |

算式（基础档）：`Content` 712 宽、锚 `0..1` ⇒ `0..712`，`sizeDelta 62.5253` 两边各撑 `31.26265` ⇒ `-31.26265..743.26265`；
再加 `Content` 在根里的左沿 `0.4` ⇒ **`-30.86265..743.66265`**。
y（uGUI 左下原点）：锚 `0.4..0.8` of 1080 = `432..864`，`sizeDelta 37.8941` ⇒ `413.05295..882.94705`，按 `1080 − y` 翻成左上原点 = **`197.05295..666.94705`**。

算式（Icon 档）：`Content` 1080² 铺满（`sizeDelta 0`）⇒ `0..1080`，`m_LocalScale 1.5` 绕中心 (540,540) ⇒ **`-270..1350`**（两个轴同值）。

⚠️ 两档**都是静态层**：`Campaign Glow` 不在 `ItemDrawerComponents` 的任何槽里（基础档的槽是 `background/image/label/quantity/...`，Icon 档只有 `image` 与转换/暂时那几项），`m_IsActive = 1` 是出厂态 ⇒ **原版不由代码开关**。
⚠️ 与 `B96_W1` 那条「原版这格只画一层」**不冲突**：那说的是 `CampaignPointDrawer.DrawForArmy` 的**方法体**（只写 `image.sprite` + `image.enabled`），prefab 自带的静态层方法体本来就不碰。

---

## 二、我们现状（改之前）

- `Shell/ItemDrawer.cs:2179` 的 `CampaignPoint` **只画主图**（阵营徽记，`Square(box, st.ArmyFill)`），**两档都没有 `Campaign Glow`**。
- 全库 `Campaign Glow` 这一层的已有实现只有两处、且都不是这一族：
  - `B-243`（`ItemDrawerPartial_B114W12.cs`）→ `Expansion Pass Points`；
  - `ItemDrawerPremium.cs` 的 `DrawCampaignGlow` → `Premium Drawer` 那几档（`background` 槽指向 `Campaign Glow` 的那一类）。
- 变体判据缺口（**这一族「同一个类、两套 prefab」**）：`CampaignPoint` 收不到「这一格走哪一档」的任何信号 ——
  `CampaignTab` 的点数格是 **Icon 档**（`Campaign Tab.prefab` 内嵌 `Icon Campaign Points Drawer Variant`，现读 grep 命中 1 处、无同名基础档），
  而 `DrawCampaignPointsReward` 原来传 `DrawerOverride.Default`、视图也不存 override。

---

## 三、改成什么

### 3·1 变体判据：挂 `ItemDrawerStyle`，⛔ 不是挂 `ov`

新增 `ItemDrawerStyle.CampaignPointsGlowIcon`（`bool`，默认 `false`）：

- `true` ⇒ 按 **Icon 档**几何（根 1080²）；
- `false` ⇒ 按 **基础档**几何（根 712.8×1080）。

**为什么不用 `DrawerOverride.Icon`**（`B-223` 那 7 族的方式）：`ItemDrawerView.Refresh()` 重画时**只带 `_style`**、
**不存 override**（`ItemDrawerPartial_B109E11.cs:33` 与 `ItemDrawerPremium.cs:282` 都写着这条已知限制）⇒
挂 `ov` 的话，这颗格子一刷新就退回基础档几何（光圈宽度会从 97.5 掉到 ≈30.3，**而且不报错**）。
挂 `style` 则 `Bind` 会把 `_style` 一起存进视图 ⇒ 刷新后仍然对。

置位点 = `ItemDrawer.DrawCampaignPointsReward`（`ItemDrawer.cs:2237`）—— 本口专供 `Campaign Tab` 高级面板那颗点数格（= Icon 档）。
战役奖励窗奖励格那条（`CampaignRewardWindow.BuildItem` → `ItemDrawer.Draw(..., Default, st)`）**不置位** ⇒ 走基础档。

### 3·2 画这一层

`Shell/ItemDrawerPartial_B114W27.cs`（新文件）：

| 方法 | 干什么 |
|---|---|
| `DrawCampaignPointsGlowBase(node, box, st)` | `FrameOf(box)`（根 712.8×1080）⇒ `Map(CpGlowBaseX1..Y2)` |
| `DrawCampaignPointsGlowIcon(node, box, st)` | `FrameOf(box, DrIconRoot, DrIconRoot)`（根 1080²）⇒ `Map(CpGlowIconX1..Y2)` |
| `DrawCampaignPointsGlow(...)` | 两档共用那一句：`Tex(PremiumGlowSprite)` → 取不到就 `Note` 出声**不画**（⛔ 不拿别的图顶替）→ `MenuDraw.Rect(..., NodeCampaignGlow, CampaignPointsGlowQueue(st), keepAspect: true, st.Clip, st.ClipSoftness)` |

- 图**复用** `ItemDrawer.PremiumGlowSprite`（`ItemDrawerPremium.cs:233`，同一串 `40K_genearl_icon_Campaign_points_big`）——
  ⛔ 没再声明第二个同串常量（`Premium Drawer` 那一族的出厂横带**就是这张**，guid 也相同）。
- 节点名**复用** `NodeCampaignGlow`（`ItemDrawerPremium.cs:43`）—— 再声明会 `CS0102`。
- 画在抽屉根 `node` 下（这一族我们没有 `Content` 包装层，`FrameOf` 已经把 `Content` 的偏移与缩放算进 `K`/`OX`/`OY`；
  与 `CampaignPoint` 现有的 `Square(box, ...)` 同一套坐标口径）。

### 3·3 调用点

`CampaignPoint` 的方法体**第一句之前**（`{` 那一行、同物理行）加：

```csharp
st.QArt = CampaignPointsArtQueue(st);
if (st.CampaignPointsGlowIcon) DrawCampaignPointsGlowIcon(node, box, st);
else                          DrawCampaignPointsGlowBase(node, box, st);
```

⚠️ 这一段在 `if (fac == null) return;` **之前** —— 故意的：原版那颗 `Campaign Glow` 是独立节点，
`image.enabled = false`（阵营认不出/徽记取不到）**不影响它**，所以它照常画。

---

## 四、队列号怎么定的

```
CampaignPointsGlowQueue(st) = st.QBoard + 1
CampaignPointsArtQueue(st)  = Mathf.Max(st.QArt, st.QBoard + 2)
```

理由（与本项目「分层用渲染队列、不用 z」那条 `CLAUDE.md` §三 对齐）：

1. **不能复用 `QBoard` / `QArt`**：本族的调用点传的是 `ItemDrawerStyle.Default(QPanelPts, QPanelPts, QPanelPts)`
   （`CampaignTab.cs:1897` 的 `CampaignPointStyle()`）⇒ **`QBoard == QArt == QText` 是本档常态**。
   光圈与主图同号 ⇒ 透明件按「到相机的距离」排序，那块近不透明的金属盘会随机压住主图。
2. **原版靠 sibling 序** `Background < Campaign Glow < Image`（基础档）/ `Campaign Glow < Image`（Icon 档）⇒
   必须翻成两个互不相同的号。
3. 三档调用点给的号常常是**连号**（`Default(q,q,q)` / `Default(3000,3001,3002)`）⇒「在两个已有号之间插一层」需要空档，
   所以把主图往上让：`max(QArt, QBoard+2)`。**代价（如实记）**：`QArt` 距 `QBoard` 不足 2 时主图上移 1~2 档；
   已分开 ≥2 的调用点逐值不变（`Mathf.Max` 取原值）。这是 `B-243` 已经接受过的同一笔代价，本波**逐条同构**。
4. 语义次序在真实站点上是对的：`Campaign Tab` 里 `Quantity` 文本先建（`QPanelPts`）、抽屉后建 ⇒ 现在
   `文本(QPanelPts) < 光圈(QPanelPts+1) < 主图(QPanelPts+2)`，与「抽屉节点排在 `Quantity` 之后」一致。

🔶 这一段是**自己设计（非原版）**，按铁律 18 标在代码注释里（`ItemDrawerPartial_B114W27.cs` 的「渲染队列」一节）。

---

## 五、改动清单 + 净增净减

| 文件 | 改动 | 净行数 |
|---|---|---|
| `Shell/ItemDrawerPartial_B114W27.cs`（**新建**） | `ItemDrawerStyle.CampaignPointsGlowIcon` + 两档常量/队列/画法 | **+134**（新文件） |
| `…B114W27.cs.meta`（**新建**） | guid `0b8e938f4a544b45b27a3cae375aa837`（全仓 grep 0 命中） | 新文件 |
| `Editor/B114_W27_Assert.cs`（**新建**） | 4 段共 **36 条**断言（① Icon · ② 基础 · ③ 端到端 · ④ 灭自证；含硬前提） | **+316**（新文件） |
| `…B114_W27_Assert.cs.meta`（**新建**） | guid `b6a12a62c50740a7b135113671a59b89` | 新文件 |
| `Shell/ItemDrawer.cs` | 4 行**同物理行改写**：`2178`（doc 补变体判据）· `2179`（签名不变）· `2180`（`{` 后追加三句）· `2237`（`style.CampaignPointsGlowIcon = true;` + 恢复 `Default`） | **0**（2497 → 2497；`git diff --numstat` = `4 / 4`） |

**行号纪律自检**（`python -I` 二进制读）：
- `ItemDrawer.cs`：`LF 2497 / CRLF 0 / 裸 CR 0`，行数与开工时**逐值相同**（2497）。
- 两个新 `.cs`：纯 LF、裸 CR 0、无 BOM 追加。
- 两个 `.meta`：与仓内既有 `.cs.meta` 同格式（LF、无尾换行）。
- `check_ledger_points.py`（只读跑，未加 `--fix`）：**A 5841 条 · 通过 5841 · 漂移 0 · 失败 0**。

---

## 六、接线片段（可粘贴 · ⛔ 本波没动宿主）

`Editor/ShellScene.cs` 的聚合行（`W-26` 正在改那条行，⛔ 我没碰）—— **同一物理行、第一个 `//` 之前**追加：

```
B114_W27_Assert.Run(); _sink.Pass += B114_W27_Assert.Pass; _sink.Fail += B114_W27_Assert.Fail;
```

---

## 七、断言四段（怎么才能红）

| 段 | 盯什么 | 🧨 怎么才能红 |
|---|---|---|
| ① Icon 档 | `A0` 硬前提图取得到 · `A1/A1b` 根/路由 · `A2` 光圈建出来了 · `A3/A3b` 框 `(-270,-270)-(1350,1350)`、宽 1620 · `A4` 图名 | 删掉 `CampaignPoint` 里 Icon 分支 ⇒ 节点找不到；把 ×1.5 漏掉（`0..1080`）⇒ A3 红 |
| ② 基础档 | `B0` 图 · `B1` 根/路由 · `B2` 光圈 · `B3/B3b/B3c` 框（PA 内接 469.8941²、居中） · `B4` 图名 · `B5` 层次序 | 套用 Icon 档那组数 ⇒ B3 红；漏掉 `sizeDelta` 那两半（只用锚 0.4..0.8 = 432 高）⇒ B3b 红 |
| ③ 端到端 | `DrawCampaignPointsReward`（真实站点框 65×69.73）· `C3` 光圈宽 = **97.5**（= 65×1.5，Icon 档几何） | 删掉 `style.CampaignPointsGlowIcon = true` ⇒ 回落基础档 ≈30.3 ⇒ C3 红 |
| ④ 灭自证 | `D1` 两档判据框结构上就不同（1620² vs 469.8941²）· `D2` 两档**同一张图** | 把两档合并成一套常量 ⇒ `D1` 与 `A3`/`B3` 里必有一边红 |

⚠️ 所有「前提」都是**硬断言**（`Tr(tex != null, …)`，⛔ 不是 `if (图) Check(...)` 静默跳过）。
⚠️ 本文件**没在 Unity 里跑过**（本波不跑 Unity）；`A0`/`B0` 是判据外的世界（图在不在），红了先查 `Resources/Art/ui_menu/`。

---

## 八、没查清的部分（如实记）

1. **基础档那条路今天有没有消费者，没证死**：`CampaignPoints` 奖励要作为 item 流过 `ItemDrawer.Draw(..., Default, st)`
   才走得到（`CampaignRewardWindow.BuildItem`）。本波只证了「代码路径存在 + 几何照 prefab」，
   **没**去证「某条战役节点奖励真的发战役点 item」。
2. **非方框 / 窄容器下 `FrameOf` 的行为**仍是 `ItemDrawerLayout.cs` 文件头 ① 那条 TODO
   （原版 `AspectRatioFitter` 在宽不够时是溢出还是按宽缩**没读过**）—— 两档断言都只跑方框/整帧夹具。
3. **`Campaign Tab` 那颗格子今天真的跑不到**（`LocalPremiumState()` 恒 2 ⇒ `BuildPremiumPanel` 仍建节点但 `ChangeState(2)` 会关掉整层）
   ⇒ 本波的可视验收只能靠断言，**没有实拍**。
4. **`ItemDrawerStyle` 三个号同值时，`QText` 那一层现在落在主图下面**（`QBoard == QText < QBoard+2`）。
   本族今天不画名字/数量（`CampaignPointStyle` 给 0），奖励格那条也不重叠；但如果将来有调用方让名字/数量和徽记重叠，
   需要再给 `QText` 让号 —— 这是 `B-243` 同一笔已知代价的延续，**不是新引入的**。

---

## 九、顺手发现（⛔ 都没改，交调度台分流）

1. 🔴 **`CampaignPoint` 的 `Square(box, st.ArmyFill)` 对「基础档」也是"自己设计"**：
   原版基础档 `Content/Image` 的锚是 `(0,0.4)-(1,0.8)`（与光圈同一个横带）、`m_PreserveAspect = 1`，
   不是「居中方块」。我们两条路共用 `Square`（只有 Icon 档恰好等价：`(0,0)-(1,1)` + PA ⇒ min 边正方）。
   ⇒ **基础档那格的主图几何与 prefab 不符**（`st.ArmyFill` 是 `Default` 的 0.34 时差得更远）。本波只补 `Campaign Glow`，**没改主图**。
2. 🔴 **`DrawCampaignPointsReward` 之前传 `DrawerOverride.Default`，但它的唯一真实消费者是 Icon 档 prefab** ——
   本波用 `style.CampaignPointsGlowIcon` 兜住了「光圈用哪档」，**但 override 仍是 `Default`**：
   若将来 `B-223` 那类「按 `ov` 分流整格几何」扩到 `CampaignPoints`，这一处要一起改（`PickDrawer` 的 Icon 列本来就是 `CampaignPointDrawer`，改 `ov` 不会换类）。
3. **`ItemDrawerView.Refresh()` 拿不到 override** 这条已知限制仍开着（`B109E11`/`ItemDrawerPremium` 都记过）——
   本波用 `style` 绕开了自己这一格，但别的族没绕开。
4. `Campaign Points Drawer.prefab` 的 `ItemDrawerComponents.background` 槽**不是 `{fileID:0}`**（指向 `Background` 节点），
   而 `CampaignPoint` 从不画底板 ⇒ 基础档那格还缺一层底板（与 1 同族、同一条缺口）。
