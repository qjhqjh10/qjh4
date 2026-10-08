# W · `DeckRuntime` 两处「按贴图比例内接」的**缺口断言**（补在宿主 `Editor/DeckScene.cs`）

- 写手：执行代理（只碰 `d:/4/Unity/MyGame/Assets/CardPresentation/Editor/DeckScene.cs` 一个文件）
- 日期：2026-10-18
- 被测改动（**上一批**，本批只补断言、一个字没改）：
  - `Deck/DeckRuntime.cs` 的 `RefreshCosmetics()`（卡背格，`CosmoCellW/H = 250/405`）
  - `Deck/DeckRuntime.cs` 的 `RefreshCosmeticDrawer()` → 新私有 `FitDrawerBack()`（抽屉，`DrawerW/H = 335.31/400`）
  - 共用口径 = `Core/DraggableController.cs` 的 `CosmeticPreview.PreserveAspectSize(tex, boxW, boxH, out w, out h)`

---

## 两条断言（实为两组）

### 组 1 · 卡背格 `cosm_cell<vi>`

| 项 | 内容 |
|---|---|
| 名字 | 「★ 卡背格**画出来**的宽 = 250（宽定那一支）」·「★ ……高 = **250 ÷ 贴图比例**」·「★ 灭自证：实绘高 **严格小于** 405」 |
| 期望值怎么现算 | **只用两条独立真值**：`tx.width / tx.height`（贴图自己的像素比例，实测 233 张 = 0.6188~0.7652）+ 框常量 `250×405`。算式逐句照 uGUI `Image.PreserveSpriteAspectRatio`：`s > 250/405` ⇒ 宽定（`ew=250, eh=250/s`），否则高定（`ew=405*s, eh=405`）。实测 233 张**全部 > 0.61728** ⇒ 一律落**宽定**支。 |
| 为什么不调 `PreserveAspectSize` | 它**就是被测的那条算式** —— 拿它当期望值，它自己算错时期望值跟着一起错 ⇒ **自证**。（注释里逐句写了这一条。） |
| 怎么分两态 | 拉伸实现画 **250×405**；内接实现画 **250×(250/s)**。两者在**高**上差 78.27px（探针那张）⇒ 远超 0.5px 容差。 |
| 🧨 改坏法 | 把 `RefreshCosmetics` 里那跳 `PreserveAspectSize` + `SetWorldHeight`/`SetAspect` 删掉、退回 `SetAspect(CosmoCellW/CosmoCellH)` ⇒ 「高」那条红。 |
| 灭自证 | `cbMh < 405 - 1`：拉伸时**宽确实是 250**（上一条照样绿），但高**恒等于 405** ⇒ 本条红 ⇒ **两条不可能同时被拉伸满足**。 |
| 量法 | `_rt.UiQuadRect("cosm_cell" + cbProbe, …)`（= `WorldW/WorldH × PxPerUnit`，回落的 `UiNodeRect` 也量世界包围盒）。 |

### 组 2 · 侧栏抽屉 `cosm_drawer`

| 项 | 内容 |
|---|---|
| 名字 | 「★ 抽屉**画出来**的宽 = **400 × 贴图比例**（高定）」·「★ ……高 = 框高 400」·「★ 灭自证：实绘宽 **严格小于** 335.31」 |
| 期望值怎么现算 | 同上口径，框 = `335.31×400`（`DrawerW/DrawerH`）：`s > 335.31/400 = 0.83828` ⇒ 宽定，否则高定。实测 233 张**全部 < 0.83828** ⇒ 一律落**高定**支（`ew = 400*s`）。 |
| 为什么不调 `PreserveAspectSize` | 同上（自证）。 |
| 怎么分两态 | 拉伸画 **335.31×400**；内接画 **(400×s)×400**。探针那张 `Cardback_SAU_Imotekh_AA_HB`（620×1002, s=0.6188）⇒ 内接宽 **247.50** vs 拉伸 **335.31**，**差 87.81px**。 |
| 🧨 改坏法 | 把 `FitDrawerBack` 里那跳 `PreserveAspectSize` 删掉、退回 `SetAspect(DrawerW/DrawerH)` ⇒ 「宽」那条红。 |
| 灭自证 | `drW < 335.31 - 1`：拉伸时宽**恒等于 335.31** ⇒ 与上一条不可能同时绿。 |
| 夹具怎么把抽屉换到探针 | `_rt.EquipCardback(drName)` —— **走生产那条路**（右键装备 / 拖拽投递调的是同一个口，`DeckRuntime.cs:2203`/`:2532`）。插在「右键换 `names[8]`」**之前** ⇒ 本节末尾 `Done` 落盘的仍是 `names[8]`（那条既有断言不受影响）。 |

---

## 选靶（⛔ 都没写死格号 / 没写死哪一张）

- **卡背格**：遍历 `CardArt.CosmeticNames()`，按 `UiCosmeticCellCenter(idx)` 只取**这一屏内**的格，
  再**跳过被视口切过的格**（判据 = **内接之后**的矩形与 `CosmoView`(155.97 / 924.06) 的四边关系，
  不是外接框 —— 第 0 行外接框上沿恰好压着视口上沿 155.97，用外接框判会把它误杀）。
  在**留下的候选**里取「内接高离 405 最远」的那张 = **比例最大**那张。
  本节起手 scroll=0（显式 `UiScrollCosmetics(-1e6f)` 回顶并断言 `CosmoScrollPx == 0`）⇒
  第 0/1 行（idx 0..11）留下，第 2/3 行被视口判出局。**实测留下 12 格，探针 = 第 8 格
  `Cardback_AM_Shock and Awe`（706×947, s=0.7455），两种算法差 69.66px**。
  （窄带那 11 张比例最接近 0.61728 的 ⇒ 差值只有 ~1px，正好会被 `cbDev > 3f` 这条前提挡下。）
- **抽屉**：遍历全 233 张，取「内接宽离 335.31 最远」的那张 = **比例最小**那张 ⇒
  `Cardback_SAU_Imotekh_AA_HB`（620×1002, s=0.6188），差 **87.81px**。
- 两张探针的「两种算法差」都过了 `> 3f` 的前提断言（差值 ≤ 3px 时前提条先红，不让下面两条空转）。

**现算的支撑数据**（本机现读 `Resources/Art/cardbacks/*.png` 的 PNG IHDR，排除 `_sdf`）：
233 张 · 比例 min `0.6188`（`Cardback_SAU_Imotekh_AA_HB` 620×1002）·
max `0.7652`（`Cardback_All_Premium4` 707×924）；`全部 > 250/405=0.61728` · `全部 < 335.31/400=0.83828`。

---

## 类型检查 · 行尾 · 没查清 / 停手

- **类型检查**：`TMPDIR=/tmp/wf_deck2 bash d:/4/Unity/工具/typecheck.sh`
  ⇒ **运行时错误数 0 · 编辑器错误数 0**（改了两次、跑了两次，两次都是 0）。全程**没跑 Unity**。
- **行尾**：改前 `b'\r\n'` = `b'\n'` = **5854/5854**（纯 CRLF）；改后 **5963/5963，lone LF = 0** ⇒ 保持纯 CRLF。
  未用 `sed -i`。
- **`git diff --numstat`（本文件）**：`158 / 12`。
  ⚠️ 说明：本会话**开工时**这个文件已经是 `49 / 12`（上一批的改动）⇒ 本批净增 **+109 行、0 删**，
  那 12 个删行**不是我加的**。
- **只碰了一个文件**：`Deck/DeckRuntime.cs` · `Shell/CollectionWindow.cs` · `Editor/CollectionScene.cs` ·
  `Core/DraggableController.cs` **全部只读**（它们在工作区里显示 M 是本会话之前就有的）。
- 🔴 **本批断言没跑过 Unity** —— 断言**是否全绿未验**，只验了「编得过 + 期望值算式与素材真值自洽」。
  需要跑的时候：`DeckScene.Run`（只动这一个宿主 ⇒ 只跑这一条，铁律 12）。
- **没查清 / 风险**（留给跑自检那一轮看）：
  1. 卡背网格**没有**任何裁切代码（`ClipCellToBand` 只服务筛选抽屉的格子，`DeckRuntime.cs:5206`）——
     第 2/3 行「越界不裁」目前只是**没裁**。我按「跳过被视口切过的格」办了，但**这不是**在断言「有裁切」。
  2. `UiQuadRect` 对 `cosm_cell*` / `cosm_drawer` 走的是 `_named` 落空后的 `UiNodeRect` 兜底
     （走 `Root.Find` + 子树包围盒）⇒ 这两个 key **首次**被用；若两处的量法契约跟 `_named` 那条不同，
     读数可能不是 `WorldW/WorldH × PxPerUnit`（代码逐句看是**同一条**，但没实跑过）。
  3. 组 2 遍历 233 张各调一次 `UiCosmeticTex`（会出声的取值路）⇒ `UiCosmeticTexCalls` 计数器大涨。
     已核：唯一读它的断言是**增量**口径（`calls0` / `warn0` 差值）⇒ 不受影响。
