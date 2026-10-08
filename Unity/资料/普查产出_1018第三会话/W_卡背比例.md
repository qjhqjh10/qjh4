# W · `A994③` 后续三组（卡背比例） —— 2026-10-18

口径（上一批已定，未翻）：原版那几颗 `Image` 是 **`m_PreserveAspect = 1`** ⇒ **按【贴图自己】的比例内接**
（uGUI `Image.PreserveSpriteAspectRatio`；⛔ 不是按框的比例）。唯一实现口径 = `Core/DraggableController.cs` 的
`CosmeticPreview.PreserveAspectSize(tex, boxW, boxH, out w, out h)`（**只调用、未改动**）。

实测输入（本轮现量，`Resources/Art/cardbacks/`，233 主图 + 233 SDF）：
主图比例 **0.6188~0.7652**（全部 **> 250/405 = 0.61728** ⇒ 一律**宽定**）；SDF **全部 100×130**（0.76923）。

---

### 三组逐组：改了哪（符号）· 改前/改后（现算式）· 依据

| # | 站点 | 改前 | 改后 | 依据 |
|---|---|---|---|---|
| 1 | `Deck/DeckRuntime.cs` `RefreshCosmetics` 卡背【格】 | `q.SetAspect(CosmoCellW / CosmoCellH)`（= 0.61728 **拉伸**） | `PreserveAspectSize(tex, 250, 405, out cw, out ch)` → `SetWorldHeight(U(ch))` + `SetAspect(cw/ch)` | 原版 `Cardback Container > Cardback` 的 `Image` 带 `m_PreserveAspect = 1`（`A4_装饰页与驱动链.md:91` 那表「Simple preserveAspect」） |
| 2 | 同上 `RefreshCosmeticDrawer` **两处** | `SetAspect(DrawerW / DrawerH)`（= 0.83828 **拉伸**）×2 | 新增私有 `FitDrawerBack(Texture2D tex)`，两处改为调它（建完一次 + **每次换图后再调一次**） | 同上（`Drop`/抽屉那颗同样带 PA=1）。⚠️ 换图那一句 `SetTexture(back)` 是**单参重载**、会先把 `_aspect` 冲成贴图比例 ⇒ 必须再内接一次 |
| 3 | `Shell/CollectionWindow.cs` `RebuildCosmoCells` 卡背本体 | `MenuDraw.Rect(…, "Cardback", QPageRow, null, **false**, null, …)` | 第 7 个实参 `false` → **`true`**（`MenuDraw.Rect` 的 `keepAspect`，doc 就写着「原版 `Image.m_PreserveAspect`」，且在**求交之前**内缩 ⇒ uv 仍跟着裁剩那块走） | `Cardback` 那颗 `Image` 带 PA=1（同表） |
| 4 | 同上 **`Cardback Shadow SDF`** 那一层 | 同上 `false` | 同上 → **`true`** | 同表：`Cardback Shadow SDF` 写着「Simple **preserveAspect**」（rect 337.5×550.8，锚点拉伸 + PA=1 是两回事） |

**偏离量（现算，⛔ 未写死进代码）**：
- 卡背格 `Cardback_AM_Shield of Humanity`（707×981）250×405 → **250×346.89**；偏离最大 `Cardback_All_Premium4`（707×924）⇒ **250×326.73**（差 78.27px）。
- 抽屉同上（335.31×400 → **288.28**×400，原来横向拉宽 16.3%）。
- SDF 337.5×550.8 → **337.5×438.75**，上下各内缩 56.025。
  🔴 **反证**：拉满时 x 缩放 337.5/100 = 3.375、y 缩放 550.8/130 = 4.237（**非等比**，距离场被竖向拉长）；内接后**两轴都是 3.375**。
- 预览（上一批已做，本轮未动）：132×198 → 132×183.16（`Shield of Humanity`）。

---

### 断言（宿主 = `Editor/CollectionScene.cs`；全部加在 Cosmetics 页那一节）

| 断言 | 怎么分两态 | 🧨 改坏法 | 灭自证 |
|---|---|---|---|
| `★ 卡背格逐格比「渲染宽高比 vs 贴图自己的宽高比」`（新，扫**可量的全部格**） | 旧写法恒 0.61728，本仓 233 张**全都 > 0.61728**（最小 0.6188 ≥ 0.61728+0.0015）⇒ 旧写法**每一格都不等** | 把卡背那一跳 `keepAspect` 改回 `false` | 期望值**从贴图资产现算**（`InsetFit`），不是读被测实现 ⇒ 改不回旧写法 |
| `★ 偏离最大的那一格…画出来的高`（新，按**偏离量**挑，⛔ 不写死格号） | 内接高 `250÷比例` vs 拉满 405 | 同上 | 紧跟一条 `★ 灭自证：「内接后的高」严格小于框高 405`（差 >10）—— 拉满时**恒等于**框高，两条不可能同时绿 |
| `★ 卡背宽/高 = 内接宽/高`（改：原来写死 250 / 405） | 同上 | 同上 | 同上（期望 `InsetFit(贴图比例, 250, 405)` 现算） |
| `★ 对照（整块不越界的格）：SDF 层宽/高`（改：原来写死 337.5 / 550.8） | 旧 550.8 vs 新 438.75 | 把 SDF 那一跳 `keepAspect` 改回 `false` | 同上（从 SDF 贴图现算；另附「非等比 3.375/4.237」反证写进文案） |
| `★ 第一排那一格的 SDF 上溢被视口截住`（改：原来 479.925） | 旧 479.925 vs 新 `479.925 − pad` = **423.9** | 同上 | 与上一条同源；`st0`（停在视口上沿 155.94）那条**未动**、仍独立可红 |
| A181 那组「对照：没被切的那一排」 | 原来断 `== 405`，改断 `== 本格自己的内接高` | 同上 | 文案显式写「**不是框高 405**（内接后高只可能 ≤ 框高）」 |

**选靶方式**：① 全量扫（可量的格逐格比比例）② 按 `|H − 405|` 偏离量挑**差得最远**的那一张做精确断言
（视口内可量的 12 格里是 `Cardback_AM_Shock and Awe` 706×947 ⇒ 335.34；⛔ 不写死格号）。
被视口切过的格**跳过**（渲染矩形本来就是「裁剩那块」的比 ⇒ 归 A181 那组管）。
新加的自检辅助 `InsetFit(spriteAspect, boxW, boxH)` = uGUI 规则的**独立复述**（doc 里写明为什么不调 `PreserveAspectSize`）。

---

### 两处过期注释：改前 / 改后

1. `DeckRuntime.BuildCosmeticDrag`：「预览本体：**画出来 = 220×330 × 0.6 = 132×198**」→
   改成「220×330×0.6 是**外接框**；真画出来是**按贴图比例内接进它**（例 132×183.16），实现在 `CosmeticPreview.Initialize`」。
2. `DeckRuntime.UiCosmeticPreviewSize` 的 doc：「= `Image_…` 的 `sizeDelta` 220×330 × 0.6 = **132×198**」→
   改成「**不是 132×198**（那是外接框）+ `A994③` 再更正那一段 + 例 132×183.16」。

---

### 需要复跑哪些宿主

| 宿主 | 为什么 |
|---|---|
| **`CollectionScene.Run`** | 我改的断言都在它这儿；卡背页两层几何都变了（必须跑） |
| **`DeckScene.Run`** | `Deck/DeckRuntime.cs` 是本仓**最热共用件之一**：卡背格 + 抽屉两处几何变了；`DeckScene` 有 4 条拖拽/预览断言与 2 条卡背格常量断言（2907/2908 只断**布局常量** 250/405，**不受影响**） |
| `BattleScene.Run` / `RuleEngineTest.Run` 等 | **不涉及**（没碰引擎、没碰战斗、没碰共用件 `MenuDraw`/`Label`/`ImageQuad`） |

---

### 类型检查 · 行尾 · 没查清 / 停手的地方

- **类型检查**：每改完一个文件各跑一次 `TMPDIR=/tmp/wf_deck bash d:/4/Unity/工具/typecheck.sh`，
  三次全 **运行时 0 / 编辑器 0**（没出现「错在别人文件上」那种情况）。
- **行尾**（改完立刻 `git diff --numstat` + 二进制数 `\r\n`）：
  · `DeckRuntime.cs` **CRLF 6340/6340** ✓（保持 CRLF）
  · `CollectionWindow.cs` **纯 LF 3191** ✓
  · `CollectionScene.cs` **纯 LF 7489** ✓ —— ⚠️ **简报里写「CollectionScene.cs 是 CRLF」不成立**（`HEAD` 那份也是纯 LF 7224）。用 Edit 工具改的，未用 `sed -i`。
  · numstat：`DeckRuntime 91/13` · `CollectionWindow 97/14` · `CollectionScene 300/35`（相对 `HEAD`；其中含**上一批**已改的内容）。
- **没查清 / 未做（⛔ 不在本批白名单内，只报告）**：
  1. **`DeckRuntime` 这两处（卡背格 / 抽屉）现在一条断言都盖不到** —— 它的断言宿主是 `Editor/DeckScene.cs`
     （`2905-2915` · `2955-2970` 那一族），**在我白名单之外**。本轮实读：那两条只断**布局常量**
     （`CosmoCellWpx==250` / `CosmoCellHpx==405`，不受影响）与**抽屉贴图名**（`CosmeticDrawerTex`），
     **没有任何一条量这两处四边形的实绘尺寸** ⇒ 需要另派一个写手在 `DeckScene.cs` 补
     「卡背格实绘高 = `250÷比例`」「抽屉实绘宽 = `400×比例`」两条（判据同本报告）。
  2. **卡背格的命中区仍是整格 250×405**（`CollectionWindow.AddHit(cell, "Hit", r, …)`）—— **本轮未动**：
     原版那颗 `CollectionCosmetic` 根节点自己**不挂 `Image`**，真正的 raycast 目标是哪一颗（`Cardback`？`SDF`？根？）
     **判据不足**（A4 只给了组件表，没给 `raycastTarget`）。⇒ 记一条账：**「命中区要不要跟着内接缩」没查清**。
  3. `MenuScroll.Intersects` 用的仍是**未内接**的格子框 ⇒ 极贴边的那几格里，卡背可能整块落在内接后的空白带里
     （`MenuDraw.Rect` 的 `ClipRect` 返回 false ⇒ 该层不建，**画面与「裁剩一条空带」等价**，但**节点数**会与「先内接再判交集」不同）。
     **未改**（改它要动 `Intersects` 契约，跨件）⇒ 记一条账。
