# W_CreatedBy断言 — `A985④`「由谁造出来的」的**卡面侧断言**（`CardBaseDemo.AssertCreatedBy`）

**本笔只改了 1 个文件**：`Editor/CardBaseDemo.cs`（净 +137 行）。**没跑 Unity**（铁律 13·3），
只跑了 3 次秒级类型检查（每改完一次 `.cs` 都跑，全 0/0）。实现侧见 `W_CreatedBy.md` / `W_CreatedBy引擎.md`。

---

## 一、断言逐条：名字 · 怎么分两态 · 🧨 改坏法 · 灭自证

**★ 前提三条先断**（挡假绿）：卡池里有 `Howling Banshee Exarch` · 它的中/英卡名**都非空**
（空串会让下面每条 `Contains` **恒真**）· `TmpFont.Available`（取不到字资产 ⇒ 整段退化成 `null == null`）。

| # | 断言 | 怎么分两态 | 🧨 改坏法 | 灭自证 |
|---|---|---|---|---|
| ② | 有来源 ⇒ `CreatedByShown` **非空** | 两态之一 | 注释掉 `FillCreatedBy` 里那句 `Fill(...)` ⇒ 红 | —— |
| ② | 内容 = **创建者的本地化卡名**套在模板里（`Contains(def.NameZh) && != def.NameZh`） | 分开「套了模板」与「裸卡名/玩家名」 | `CreatedByLine` 改回裸卡名 ⇒ 红 | —— |
| ②b | 那行字**真有可见顶点**（`verts > 0`） | 分开「层开着」与「字真画出来」 | 不建层 / 网格不生成 ⇒ 红 | —— |
| ③ | 抽掉来源 ⇒ **层被拆掉**（`SetCreatedBy(null)` 后 `CreatedByShown == null`） | 两态之二（同一张视图，先有后无） | 让 `Fill` 收到空串**不拆层** ⇒ 红（② 仍绿） | ★ 两张卡吃**同一份 `CardData`**，只差「有没有来源」⇒ 必须一个非空、一个 null |
| ④ | `SetFace(Board)` ⇒ **收起来** | 两态之三 | 删掉 `Show(_createdBy, !board)`（或改成恒亮）⇒ 红 | —— |
| ④ | ★ 那一层**建着、只是关着**（`!cb.gameObject.activeSelf`） | 「亮着 / 关着 / 没有」三态分得开 | **只删 `Show` 那一句** ⇒ **只有本条**红 | ★★ **本条就是灭自证**：换成断 `_createdBy != null` ⇒ 两态都绿（同义反复） |
| ⑤ | `SetFace(Hand)` ⇒ 又亮出来、内容**逐字不变** | 回手那一路 | 收起来后不还亮（漏设）⇒ 红 | —— |
| ⑥ | 换语言 ⇒ 换的是**创建者卡名**那一截（`Contains(def.Name)`） | 中/英两档 | 拆 `CardText.Name` 的语言闸 ⇒ 红 | ★ 同一张卡、数据逐字相同、只有语言不同 ⇒ 两串**必须不同** |

**有意没写的一条**：⛔ **不断「渲染宽度 ≤ 框宽」** —— 那要先量出 0.2 描边给网格留了多少白，
**没跑 Unity 量不出来**（铁律 5·c：宁可不写，也不拿猜测当判据）。

## 二、量法用的是哪一份（为什么不用新的）

- **可见性** = `CardView.CreatedByShown` —— 它读的是**真 `activeSelf`**，与 `ArmyShown` / `RaceShown` 同一族，
  **不是我新造的读口**（⛔ 判据不取「层建出来没有」）。
- **真渲出来那一条** = `ShellScene.TmpSpanPx(Transform, …)` —— 全仓**唯一**那一份「TMP 渲出来顶点」量法
  （`A844` 收口；`CollectionScene.TextExtentPx` 也只是转调它）⇒ **没自创扫网格的循环**。
  ⚠️ 它内层的 `LayoutSpace.ToPixel` 只在 **16:9** 自洽：本断言点 `cam.aspect` 正好是 16:9（`Shot()` 设的），
  而且我只取它的 `verts > 0`、**不做坐标换算**，所以不受那条限制影响。
- **找节点**：新加的小工具 `LayerTmp(Transform, name)`（扫法与 `AssertTitleMidline` 那一段逐句相同）。

## 三、并排比：拿哪张卡 · 看什么 · 提醒

- 🔴 **本笔改了卡面输出 ⇒ 按铁律 10⑥ 要配一次并排比**：**拿 `Howling Banshee Exarch`（`ASH79`，
  工程里当尺子的那张、`CardFaceProbe` 里就有）**，**让它带一个创建者**再渲。
- ⚠️ **本宿主看不到这行字**：`CardBaseDemo` 的演示卡是 `CardData.Placeholder`、没有创建者；
  我的探针在 `SaveScene` **之后**建、`finally` 里拆 ⇒ **没进任何截图**。
  要真看到就走 `BattleScene`（效果造出来的那张牌），或给 `CardFaceProbe` 加一次 `SetCreatedBy`。
- **看两件**：① 这行字**落在卡的哪个位置**（原版 y = 1.65 是**从卡心量起**、父件 `CardUI` 高 3.3686
  ⇒ 它在**卡体上沿**附近）；② **有没有压住卡名 / 立绘**（这工程踩过「文字底板把立绘盖死」）。
- ⚠️ **PnP 是印刷品、不含这一行** ⇒ 并排比只能验「像不像 / 压没压住」，⛔ **不能当参数源**。
- ⚠️ 提醒：**这个宿主出截图**（`d:/4/_tmp_view/cardbase/`）—— 跑完**断言绿了也要看一眼图**（我跑不了 Unity）。

## 四、类型检查 · 行尾 · 没查清

- **类型检查**：`TMPDIR=/tmp/wf_cbd bash d:/4/Unity/工具/typecheck.sh`，跑 3 次 ——
  **运行时 0 / 编辑器 0**。⛔ 没跑任何 Unity 批处理（铁律 13·3）。
- **行尾**：改前 **CRLF 全量**（1015/1015）；改后 **1152 CRLF / 1152 LF**（仍全量 CRLF，原样）。
  没用 `sed -i`（用 `wb` + 显式 `\r\n`）。
- **顺手核到的一条（不是缺陷，是防将来改错）**：`CreatedByAt01` 的 y = 1.65 是**从卡心**量起 ——
  父件（卡根 `CardUI`）rect = **2.5437×3.3686**（`RectTransform_-4981929636261473480.json`），
  `2DCard` = 2.1×3.3，两者**同心**（锚点/轴心都 0.5）⇒ ⛔ **别照 3.3686 再折一遍**，那会把这一行
  整体压低 ~0.019 卡单位（≈2 px）。**实况旁证**：13 个战场 dump × 每场 8 个实例 = **104 行**，
  `CreatedByText` 全是 `pos 0.0,1.6` / `size 2.5,0.4` / `activeSelf=false`（出厂关着）。已写进方法头注释。
- **没查清**：
  1. 🔴 **断言一条都没实跑过**（我不许跑 Unity）。尤其 **②b `verts > 0`**：静态推过 `Fill` → `FitToWidth` /
     `FitToBox` 都会 `ForceMeshUpdate()`，网格应当有顶点，**但没实跑**。若它红，先看「TMP 在这个宿主里
     有没有生成网格」，别急着改实现。
  2. `git diff --numstat` = **196 / 7**：那 **7 行删减不是我加的** —— 它们落在 `AssertTitleMidline` 那一段
     （`title_probe_card` / `armySeen` / `Check(armySeen && raceSeen …)` 等），是**本会话早前某位写手留在
     工作区的未提交改动**。我只插了两处（调用一行 + 方法一段）。
  3. **⑤ 走的 `SetFace(Board → Hand)` 本工程今天没有调用点**（`SetFace` 只用于出牌那一步）
     ⇒ 这条是「防将来加回手入口时漏设」，**红了不代表现网有缺陷**。
  4. **子件次序**：原版 `CreatedByText` 是卡根**第一个子件**（排在 `2DCard` 之前）；我们这层是
     `FillCreatedBy` 现场建的，位置由建卡顺序决定。本项目分层走**渲染队列**（不是兄弟序）⇒ 判「不影响画面」，
     **但我没实跑验过**。
