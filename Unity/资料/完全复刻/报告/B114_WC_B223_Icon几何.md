# B114_WC · `B-223` Icon 变体几何 7 档（执行报告）

> 第 114 会话第 1 波写手 `W-C` · 2026-11-01。
> ⛔ 未跑 Unity（`-executeMethod` 全禁）· ⛔ 未动 git · ⛔ 未改账 · 只写白名单 4 个文件。
> **typecheck（`TMPDIR=/tmp/wf_wc`）= 运行时 0 / 编辑器 0。**

---

## 〇、结论（一句话）

`Dispatch` 的 `ov` 已经收进来了、但 26 个 `case` 里**只有 `GenericArmy` 往下传** —— 本波加了一句
**同物理行**的 `if (ov == DrawerOverride.Icon && IconVariant(...)) return;`（`ItemDrawer.cs` **零行数变化**），
7 族的 Icon 档实现全部落在 `ItemDrawerPartial_B111R3W.cs` **类尾**。
7 处几何 + 「Icon 档不画名字/数量」的层数差**逐处改完**，并配了 `Editor/B114_C_Assert.cs`（接线进 `ShellScene.Run`）。

**⚠️ 两处与施工单/判据报告不同的地方（都在下面逐条里写清了反证）**：

1. **#2 Avatar 的阵营名我没有画** —— 判据报告写「`armyText` 保留（Icon 档也有）」，但那是**只看 PPtr 非空**的结果：
   `Icon Avatar Drawer Variant.prefab` 的 `Army` GameObject **`m_IsActive = 0`**，而原版 `AvatarDrawer.Draw`
   只写 `armyText.text`、**一次 `SetActive` 都没调** ⇒ 屏幕上不出现。照 PPtr 画 = 凭空多一层可见文字。
   ⭐ **基础档 `Avatar Drawer` 的 `Army` 也是 `m_IsActive = 0`** ⇒ 我们**基础档**那条老分支（`ItemDrawerAvatar.cs:47`）
   也在画一层原版看不见的阵营名 —— **那是本波白名单之外的文件，已记进「顺手发现」，未改**。
2. **#13 Wildcard 用的是 `Scale(FrameOf(...).Map(0,0,1080,1080), 0.94f)`**，不是字面的 `Scale(box, 0.94f)`。
   **方框调用下两者逐值相等**（`K = 1`、`OX = OY = 0`）；非方框时按本族其余 6 档同一套 `FrameOf` 内接。
   ⛔ 没有复用 `IconFill`（那个量服务「居中方块」档，基础档 `Wildcard` 也在用）。
3. 🔴 **就地订正 `B113X10` 报告的 #13 那组数（铁律 5）**：报告写「`m_LocalScale = 0.94` ⇒ 视觉 **(−32.4,−32.4)-(1112.4,1112.4)**（归一化 −0.03..1.03，即「略小于满框」）」——
   **自相矛盾且算反了**：`-0.03..1.03` 是**大于**满框，而它的正文又写着「略小于满框」。
   `m_LocalScale` 是绕 `m_Pivot (0.5,0.5)` **乘**上去的（`DrawerFrame.Scale` 的同一口径），0.94 < 1 只能**缩小**：
   **1080 × 0.94 = 1015.2，居中 ⇒ (32.4,32.4)-(1047.6,1047.6)**。那一组 `-32.4 / 1112.4` 相当于把 0.94 当成了 **÷**。
   ⇒ 本波实现与断言都用 **(32.4,32.4)-(1047.6,1047.6)**；`B113X10` 那张总表的 #13 行**建议就地订正**（本波不改别人的报告文件）。

---

## 一、7 处逐条：原版判据 → 我们（改前）→ 改成什么

> 判据一律 **prefab 现读**（`d:/2/tools/assetripper_out/ExportedProject/Assets/`）：
> `!u!224` 的 `anchorMin/anchorMax/sizeDelta/m_LocalScale` + `!u!114` 且
> `m_Script.guid = 26d6053ad69df806e09ae9cd86daa71f`（`ItemDrawerComponents`）的
> `background / image / label / quantity / premiumHighlight` 五个 PPtr（按 `m_GameObject` 解成节点名）
> + 根上 `AspectRatioFitter` 的 `m_AspectMode/m_AspectRatio` + 每个 GameObject 的 `m_IsActive`。
> 本波 7 份逐份读过（脚本口径写在本报告 §四，可复跑）。

### #2 `Icon Avatar Drawer Variant.prefab` — Avatar

| 项 | 原版（现读） | 我们（改前） | 改成 |
|---|---|---|---|
| 根 | ARF `m_AspectMode 2`(HeightControlsWidth) · `m_AspectRatio 1` ⇒ **1080×1080** | `FrameOf(box)` = **712.8×1080** | `FrameOf(box, DrIconRoot, DrIconRoot)` |
| `Content` | 锚 `(0,0)-(1,1)` ⇒ 1080² | 712×1080 | `DrawerIconContent` |
| `image` 槽 | 节点 **`avatar`**，锚 `-0.285..1.285 × -0.195..1.38`、`sizeDelta 0`、scale 1 ⇒ **(-307.8,-210.6)-(1387.8,1490.4)**（1.57× 根宽过扫，整棵 prefab 无 `Mask`） | `AvatarImageFrame` = (-65.0,65.6)-(795.2,796.1)（基础档 `Avatar Drawer` 那一格） | `fr.Map(-307.8,-210.6, 1387.8,1490.4)` |
| `background` / `label` / `quantity` | **全 null**（prefab 里连 `Background` 节点都没有） | `DrawerBackground` 画底板 + `DrawerNameQuantity(hasName:true, hasQuantity:false)` | **都不建** |
| `armyText` | 非 null，但 **`Army` GameObject `m_IsActive = 0`** | `DrawerArmyName(...)` 画一行可见文字 | **不画**（见 §〇①） |

灭自证：删掉这一支 ⇒ 头像回到 860×730 那一格、且多出 `Background`/`Label`/`Army` 三层 ⇒ 断言 A3/A4/A5/A6 全红。

### #4 `Icon Container Drawer Variant.prefab` — Container

| 项 | 原版 | 我们（改前） | 改成 |
|---|---|---|---|
| 根 | ARF `2` / `1` ⇒ **1080²** | 712.8×1080 | `FrameOf(box, DrIconRoot, DrIconRoot)` |
| `image` 槽 | 节点 **`Image`**，锚 `(0,0)-(1,1)`、`m_LocalScale = 1.1` ⇒ 满帧 ×1.1 = **(-54,-54)-(1134,1134)** | `ContainerImgScale = 1.2`、帧 712.8×1080 ⇒ (112.8,-108)-(967.2,1188) | 满帧 × **1.1** |
| `armyIcon` | **null**（Icon 变体 `Content` 下没有 `Army Icon` 节点） | `item.CardArmy != 0` 就画徽记 | **不建** |
| `background`/`label`/`quantity` | **全 null** | 画底板 + 名字 + 数量 | **都不建** |

### #5 `Icon Currency Drawer Variant.prefab` — Currency

| 项 | 原版 | 我们（改前） | 改成 |
|---|---|---|---|
| 根 | ARF `2` / `1` ⇒ **1080²** | `DrawCurrency` 的 712.8×1080（`CurRootX1 = 603.6`） | `FrameOf(box, DrIconRoot, DrIconRoot)` |
| `image` 槽 | 节点 `Image`，锚 `(0,0)-(1,1)`、scale **1.0** ⇒ 满帧 | (604,0)-(1316,864) ×0.9 | 满帧 (0,0)-(1080,1080) |
| `background` / `icon`（小图标）/ `label` / `quantity` | **四个槽全 null** | 底板 + 名条行 + 108² 小图标 + 数量 | **四段全跳过**（整格只有一张主图） |
| 自检读口 `LastCurrency` | — | 基础档五格 | Icon 档 = `MainImage=true`，其余 false |

### #6 `Icon Expansion Pass Points Drawer Variant.prefab` — Expansion Pass Points

| 项 | 原版 | 我们（改前） | 改成 |
|---|---|---|---|
| 根 | ARF `m_AspectMode 3`(FitInParent) / `1` ⇒ **1080²**（`Content` = 1080²） | 712.8×1080 | `FrameOf(box, DrIconRoot, DrIconRoot)` |
| `image` 槽 | 节点 `Image`，锚 `(0,0)-(1,1)` ⇒ 满帧 | `(0.4,216)-(712.4,648)` 中间横带 | 满帧 |
| `armyText`/`label`/`quantity`/`background` | **全 null**（`Content` 下只有 `Image` 一个活节点） | 画阵营名 + 名字 + 数量 + 底板 | **都不建** |

⚠️ 这一族**没有独立的 Icon 类**（`ItemTypeSets` 的 Icon(10) 列 = `ExpansionPassPointDrawer`）⇒ 分流只能挂在本类上。

### #10 `Icon Random Card Drawer Variant.prefab` — Random Card

> 这一份的根是 **712.8×1080**（ARF `2` / `0.66`）——**与我们的 `FrameOf(box)` 同形，不换帧**。

| 项 | 原版 | 我们（改前） | 改成 |
|---|---|---|---|
| `Base Background` / `Background` | (0,0)-(712.8,1080) / `-0.05..1` 高（= 到 1134） | 同（已一致） | **不动** |
| `image` 槽 | 节点 `Image`，锚 `(0,0)-(1,1)` ⇒ **满帧 (0,0)-(712.8,1080)** | 与 `Random Image` 同一格 `slotR` = (0.4,108)-(712.4,648) | `fr.Map(0.4,0, 712.4,1080)` |
| `Random Image` | 锚 `(0.1,0.1)-(0.9,0.9)` ⇒ **(71.3,108)-(641.5,972)** = 570.2×864 | 同 `slotR` = 712×540 | `fr.Map(71.3,108, 641.5,972)` |
| `Random Image/Icon`（问号） | 锚 **(0.2,0.3)-(0.8,0.7)**（基础档是 `(0.2,0.2)-(0.8,0.8)` —— **两份不一样**） | 复用基础档的四个常量 `RandomIconX1..Y2` | 按新 `Random Image` 框 + `(0.2,0.3)-(0.8,0.7)` 内接 |
| `armyText`/`label`/`quantity` | 全 null（**prefab 里连 `Army` 节点都没有**） | 画阵营名 + 名字 + 数量 | **都不建** |

### #13 `Icon Wildcard Drawer Variant.prefab` — Wildcard

| 项 | 原版 | 我们（改前） | 改成 |
|---|---|---|---|
| 根 / `Content` | 1080² / `Content` 1080² | 无帧概念（直接画在 `box` 上） | `FrameOf(box, DrIconRoot, DrIconRoot).Map(0,0,1080,1080)` |
| `image` 槽 | 节点 `Image`，锚 `(0,0)-(1,1)`、`m_LocalScale = 0.94`、`m_Pivot (0.5,0.5)` ⇒ 视觉 **1015.2² 居中 = (32.4,32.4)-(1047.6,1047.6)**（⚠️ **`B113X10` 报告那一组 -32.4..1112.4 是【除反了】**，见 §〇③） | `IconRect` → `Square(box, IconFill)`，默认 **0.7**（调用点另有 0.95 / 1） | 满框 × **0.94** |
| `background`/`label`/`quantity` | 全 null（`Content` 下只有 `Image`） | 只画一张图（本来就无文字层） | 不变 |

### #14 `Alliance Badge Drawer Icon.prefab` — Alliance Badge

| 项 | 原版 | 我们（改前） | 改成 |
|---|---|---|---|
| 根 | **根上没有 `AspectRatioFitter`**（`m_SizeDelta = 165.9539 × 155.7357`，随调用框走） | `FrameOf(box)`（712.8×1080 竖版帧） | **不换算，`frameR = box`** |
| `Content` | **没有 `Content` 层**（`Frame` / `Badge` 是**根的直接子件**） | `DrawerContent` + `Background` | 不建 `Content`、不建 `Background` |
| `Frame` / `Badge` | 两者锚 `(0,0)-(1,1)`、`sizeDelta 0` ⇒ **都满根** | 都在 `(0.4,216)-(712.4,648)` 那一横带里 | 都 = `box` |
| `armyText` / `label` / `quantity` | **全 null** | 画阵营名 + 名字 + 数量 | **都不建** |

---

## 二、15/15 的另一个共性（§4·1）落地情况

原版 **15/15 的 `label` / `quantity` 槽都是 `{fileID:0}`** ⇒ 基类 `ItemDrawer<T>.Draw` 的 `if (label)` 那一跳直接跳过。
本波在 **6 个** Icon 分支里显式关掉了这两层（`Container` / `Currency` / `ExpansionPassPoint` / `RandomCard` / `AllianceBadge` / `Avatar`；`Wildcard` 本来就没有文字层）。
断言用 `ItemDrawerStyle.Default(...)`（`NamePx = 26` / `QuantityPx = 34` **都非 0**）跑 —— **不是把宿主设成 0 换来的「绿」**。

---

## 三、改动清单

| 文件 | 改动 | 净行数 |
|---|---|---|
| `Assets/CardPresentation/Shell/ItemDrawer.cs` | **仅 1 行**：`Dispatch` 的 `{` 行（`:1918`）**同物理行**追加 `if (ov == DrawerOverride.Icon && IconVariant(...)) return;` + 说明注释 | **0**（2497 → 2497，`git diff --numstat` = `1 / 1`） |
| `Assets/CardPresentation/Shell/ItemDrawerPartial_B111R3W.cs` | 类尾新增：`IconVariant` 分流 + 7 个 `IconXxx` 实现 + `DrawRandomIconVariant` + 一段判据头注 | **+238**（61 → 299） |
| `Assets/CardPresentation/Editor/B114_C_Assert.cs`（**新建**） | 8 段断言（7 档 + 1 段灭自证），527 行 | 新文件 |
| `Assets/CardPresentation/Editor/B114_C_Assert.cs.meta`（**新建**） | guid `3c71ae5b90d24f148a0c6d2b7e41f5a8`（全仓查过，无重复） | 新文件 |
| `Assets/CardPresentation/Editor/ShellScene.cs` | **仅 1 行**（`:11093` 那条聚合行）**第一个 `//` 之前**追加 `B114_C_Assert.Run(); _sink.Pass += ...; _sink.Fail += ...; ` | **0**（11179 → 11179，`git diff --numstat` = `1 / 1`） |

**⚠️ 行号纪律自检**：改完 `python` 二进制数过 —— `ItemDrawer.cs` 2497 行 / 纯 LF / 裸 CR 0；
`ShellScene.cs` 11179 行 / 纯 LF / 裸 CR 0。两个文件都**只换内容不换行数** ⇒ 账的 A 落点零漂移
（顺手核过 `账/收藏.tsv` 里指向 `Shell/ItemDrawer.cs:1931/1938/1966` 的那几条，位置未变）。

---

## 四、断言（`Editor/B114_C_Assert.cs`）

| 段 | 断什么 | 期望值（**独立常量**，来自 prefab，⛔ 不读我们代码的常量） |
|---|---|---|
| A #2 Avatar | 主图矩形 / 无 `Background`·`Label`·`Army` / 路由 `AvatarDrawer` | 主图框 (-307.8,-210.6)-(1387.8,1490.4) |
| B #4 Container | 主图矩形 / 无 `Army Icon`·`Label`·`Background` | 满帧 ×1.1 = (-54,-54)-(1134,1134) |
| C #5 Currency | 主图矩形 / 无 `Background`·`Label`·小图标·`Quantity` / `LastCurrency` 五格 | 满帧 (0,0)-(1080,1080) |
| D #6 ExpPassPt | 主图矩形 / 无 `Army`·`Label`·`Background` | 满帧 (0,0)-(1080,1080) |
| E #10 RandomCard | `Random Image` 矩形 · 问号按 `(0.2,0.3)-(0.8,0.7)` 内接 · 非中立档 `Image` 满帧 · 无 `Label`/`Army` | RI = (71.3,108)-(641.5,972)；`Image` = (0.4,0)-(712.4,1080) |
| F #13 Wildcard | 主图矩形 / 无 `Label`·`Background` | 满框 ×0.94 = (32.4,32.4)-(1047.6,1047.6) |
| G #14 Alliance | `Frame` 与 `Trophy Icon` 都 = 调用框 / 无 `Content`·`Background`·`Label` | 夹具故意用**非方框** (100,50)-(266,205.74) |
| H 灭自证 | `DrawerOverride.Default` 下**基础档仍是基础档**（Container 帧 712.8 / 比例 1.2 / 仍建 `Label`） | 主图 = (112.8,-108)-(967.2,1188) |

**量法**：主图那类件是 `ImageQuad` ⇒ `MenuDraw.QuadRectPx(q)`（设计 px · 左上原点 · y 向下）。
`MenuDraw.Rect(..., keepAspect: true)` 会按贴图比例内接，所以期望值 = `CardbackFace.Fit(实际贴图, 【独立的 prefab 框常量】)`
—— 即**复用生产代码那条 fit**、但喂进去的是**判据里的框**。框一改就红。
⚠️ `Content` 这类**纯容器**（`MenuDraw.Node`，没有 `ImageQuad`）**不量** —— 少一个与生产代码分家的量具；
帧形一律由主图矩形反推（各档主图矩形对帧/比例都是一一对应的）。

---

## 五、没查清的部分（如实记）

1. **非方框调用下的 ARF 行为仍未读**：7 档里 5 档是 ARF「高控宽」、1 档 FitInParent、1 档无 ARF。
   我们的 `FrameOf` 一律按 `min(box.H/rootH, box.W/rootW)` 内接；原版 ARF 在**容器比根更窄**时是溢出还是按宽缩
   **没有实测**（= `Shell/ItemDrawerLayout.cs` 文件头 ① 那条 TODO，与 `ItemDrawerCurrency.cs` 文件头 ② 是同一条）。
   本波断言全部用**方框**夹具（`K = 1`），所以这一条不影响断言结论，但**非方框实景仍可能差一层**。
2. **`Icon Avatar Drawer Variant` 的 `Content.m_LocalScale` 序列化值是 `(0,0)`**（由 `UIScaleToFit` 运行时改写）。
   本波按「根 1080²、Content 满拉伸 ⇒ 缩放 = 1」处理（`B113X10` §六·4 的判断），**没有跑 Unity 验**。
3. **`showItemInfoOnClick` 的取值**（Icon 档里 3 个是 1）不在几何范围，本波没碰 —— 留给点击链那条账。
4. **只接了 `Icon`(10) 一档**。`Horizontal`(15)/`Shop`(20)/`OfferPopups`(30) 的变体几何**一份都没查**
   （`ItemTypeSets` 里 `PlayerAvatar` 的 Shop 列也指向 `AvatarDrawer`、`CosmeticItemAvatarBorder` 的 Shop/Popup 列指向 `AvatarBorderDrawer`）
   ⇒ 那几档今天**仍按基础档几何画**，⛔ 别以为是本波漏了、也⛔ 别顺手接上（判据没查）。
5. **断言未在 Unity 里跑过**（本波不跑 Unity）。B114_C_Assert 里每档都先断「图取得到」这个**硬前提**
   （⛔ 不是 `if (图) Check(...)` 静默跳过）—— 万一某个图名在 `Resources/` 里解析不到，会在宿主里**红**而不是静默绿。

---

## 六、顺手发现（⛔ 都没改，交调度台分流）

1. ⭐🔴 **基础档 `Avatar Drawer` 的阵营名也是一层原版看不见的文字**（本波受白名单限制没改）。
   `Avatar Drawer.prefab` 的 `Army` GameObject **`m_IsActive = 0`**（与 Icon 变体同值），
   而 `AvatarDrawer.Draw` 只写 `armyText.text`、不 `SetActive` ⇒ 原版**两个档都不显示阵营名**。
   我们的 `Shell/ItemDrawerAvatar.cs:47` `DrawerArmyName(content, fr, item.CardArmy, st);` **会画出来**。
   ⇒ **待办**：删掉那一句（或与 Icon 档一起收口成「不画」）。判据 = 本报告 §〇① 的 prefab 现读。
   ⚠️ 删之前先 grep 有没有断言按 `Army` 节点存在判过 —— 本波没查（不在白名单）。

2. 🔴 **`Icon Expansion Pass Points Drawer Variant` 还有一层我们在两个档都没画的静态件 `Campaign Glow`**：
   - 它是 `Content` 的**第一个子件**（在 `Image` 之前 ⇒ 在下面）、`m_IsActive = 1`、`m_PreserveAspect = 1`、
     `m_Color` 白、`m_RaycastTarget = 1`；
   - 锚 `(0,0)-(1,1)`、`sizeDelta (-360.3243,-360.3243)` ⇒ 框 **(180.16,180.16)-(899.84,899.84)**，
     `m_LocalScale = 1.5` ⇒ 视觉 **(0.24,0.24)-(1079.76,1079.76)**（≈ 满帧）；
   - `m_Sprite` guid `6c77cbd6b01d7c549952437eb07de765` ⇒ **`40K_general_icon_Expansion Pass XP`**
     （我们 `Resources/Art/ui_menu/40K_general_icon_Expansion_Pass_XP.png` **已经在**）；
   - 它**不在** `ItemDrawerComponents` 的任何槽里（`background` 是 `{fileID:0}`）⇒ 原版也不由代码开关，**恒显**。
   ⇒ **待办**：本支应补一层 `MenuDraw.Rect(content, glow, Scale(fr.Map(180.16,180.16,899.84,899.84),1.5f), "Campaign Glow", st.QBoard, null, true, …)`。
   ⚠️ 本波**没有顺手做**的理由（如实说）：它改的是**画面上多出一大块发光**，而本波**不跑 Unity 验不了**；
   且在「QBoard 与 QArt 同号」的宿主（`CampaignTab` 的 `Default(q, q, q)`）里同队列的前后关系不可靠。
   ⇒ 建议下一波连「给这一层单独的队列号（`QBoard`）」+ 一条「glow 矩形 = (0.24,0.24)-(1079.76,1079.76)」的断言一起做。

3. **`Icon Random Card Drawer Variant` 与基础档 `Random Card Drawer` 的 `Base Background` 出厂激活态不同**
   （Icon 变体 `1`、基础档 `0`），但 `RandomCardDrawer.Draw` 第 ①/② 段按 `cardRarity` 一开一关 ⇒ **无可观测差异**。
   记在这里只为「别人照 prefab 静态读数时别再吓一跳」。

4. **`Icon Expansion Pass Points Drawer Variant` 根上的 ARF 是 `m_AspectMode 3`(FitInParent)**，
   而本族其余 5 档是 `2`(HeightControlsWidth)。两者在方框下等价；非方框下的差别**没读**（并进 §五·1）。

5. **`Icon Random Card Drawer Variant` 的 `Random Image/Icon` 锚点与基础档不同**（`(0.2,0.3)-(0.8,0.7)` vs `(0.2,0.2)-(0.8,0.8)`）
   —— 本波已按变体实现（`DrawRandomIconVariant`），但 **`B109_E18` 那条断言只覆盖基础档**，
   若以后有人把 `DrawRandomIcon` 的四个常量「统一」成一套，这条新断言（E4）会替我们挡住。

6. **`ItemDrawerStyle.Default` 的 `IconFill 0.7 / ArmyFill 0.34` 仍在被各调用点各挑一个数**
   （`B113X10` §4·3）。本波**没有**动它（那是「居中方块」档的量），但 Icon 档现在有了自己的固定几何
   ⇒ 以后 `IconFill` 在 Icon 档上**只剩 `Placeholder` 兜底那一条路**用到（图缺失时才轮到它）。

---

## 七、给复核者的两分钟核对口

```bash
# 1) 只看本波动的三处（应当只有 1/1、1/1、238/0）
git diff --numstat -- Unity/MyGame/Assets/CardPresentation/Shell/ItemDrawer.cs \
                      Unity/MyGame/Assets/CardPresentation/Shell/ItemDrawerPartial_B111R3W.cs \
                      Unity/MyGame/Assets/CardPresentation/Editor/ShellScene.cs

# 2) 分流那一句（只应命中 1 处）
grep -n "IconVariant(drawer" Unity/MyGame/Assets/CardPresentation/Shell/ItemDrawer.cs

# 3) 类型检查
TMPDIR=/tmp/wf_wc bash Unity/工具/typecheck.sh      # 期望：运行时 0 / 编辑器 0
```
