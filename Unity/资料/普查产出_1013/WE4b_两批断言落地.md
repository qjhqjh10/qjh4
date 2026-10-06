# WE4b · 两批断言落地（A640 + A524）

> 执行写手：**2026-10-13（批次 4 · W-E4b）** · 独占文件：`Editor/RewardsScene.cs`（**只加了 3 段，纯增量**）+ 本报告
> ⛔ 没跑 Unity（一次 `-executeMethod` 都没调）· ⛔ 没动 git（只用只读 `git status` / `git diff`）· ⛔ 没改两张正本 · ⛔ 没越白名单
> ⛔ **没碰别人的五段**（W-E3 的 A353/A390 · W-M1 的 A389 · W-E4 的 A503 · W-C1 的 6 条 + W-A537 的 A537/A239/A539）
> ✅ 类型检查**改完立刻跑过 3 次**：`TMPDIR=/tmp/wf_we4b bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（§九）
> 📌 本报告的「实测」只有两种：① **现读源码**（本件亲读，逐条标了 `文件:行号`）② **静态推演**（工具不许跑 ⇒ 见 §四「牙口」那一段的口径）
> 🔴 **起点纪律**：一切以【`WD3` §七 / `WD2` §五 的成品规格 + 现读源码】为准；⛔ 没拿我们自己的常量当过期望值。

---

## 一、结论

| 账 | 状态 | 一句话 |
|---|---|---|
| **A640**（A516–A520 的断言） | ✅ **5 段全落地** | `WD3` §七 的 5 段（A516 · A517 · A518 · A519 · A520）**逐段进了 `RewardsScene.Run()`**，含那个 `TmpEdgePx` 小工具。**两处按本文件实况改了名字**（见 §二·0） |
| **A524**（A493 #1–#8 的断言） | ✅ **8 条全落地** | `WD2` §五 的 8 条 + 它那个 `edgePx` 工具；#7 落在 §四（要用现成的 `e0`）、#8 落在 §四、#1–#6 落在 §五。**一处路径按 A519 之后的父子关系改了**（见 §三·0） |
| **牙口** | ⏸ **全是预判（本件没跑 Unity）** | 另加了一个**只拼日志**的 `teeth(...)`/自报：把「框宽 / 实测量到的渲染宽 / 删掉 `Align*` 后的位移」打进日志 ⇒ **同步点那次 run 的输出直接告诉我们哪几条两态同形**（§四） |
| **改坏法** | ⏸ **静态推演，没实测** | 逐条推演见 §五；每条都标了「红/不红」以及**它凭什么**（几何算式 / 源码行为） |
| 类型检查 | ✅ **0 / 0** | 见 §九（跑 3 次，两次是改完立刻跑的） |
| 行尾 | ✅ **没翻** | 二进制读：**CRLF 0 / LF 9415**（改前 9174 行）；`git diff --numstat` = **957 / 41**，其中**我的三段 = 52 + 46 + 143 = 241 行、纯增量（0 删除）**，另 41 条删除全在**别人**的段里 |

### 我的三段落在哪（`git diff -U0` 的 hunk 头，可复核）

| hunk | 内容 | 段 |
|---|---|---|
| `@@ -5691,0 +6303,52 @@` | 三个量文字的助手（`TmpEdgePx` / `edgePx` / `teeth`） | §四 之前 |
| `@@ -5728,0 +6392,46 @@` | A524 #7 + A640 A516 + A524 #8 | §四 段内 |
| `@@ -5790,0 +6500,143 @@` | A524 #1–#6 + A640 A517/A518/A519/A520 | §五 段内 |

🔴 三段全是 `,0 +`（**零删除**）⇒ **不可能动到别人那五段**（`W-E3` / `W-M1` / `W-E4` / `W-C1` / `W-A537`）。

---

## 二、A640 的 5 段（落点 · 标识符有没有按本文件改 · 每条的期望值来源）

### 0. 落地时**按本文件实况**改掉的四处（逐条给理由）

| # | 规格里写的 | 落地写成 | 为什么必须改 |
|---|---|---|---|
| ① | `TextMeshPro`（裸名） | **`TMPro.TextMeshPro`** | 本文件**没有 `using TMPro;`** —— `:12-17` 只有 `System.Collections.Generic` / `System.IO` / `CardPresentation` / `UnityEditor` / `UnityEditor.SceneManagement` / `UnityEngine`；文件内既有的 3 处（`:335` / `:370` / `:406`）**全是全名**。裸名 ⇒ 编不过 |
| ② | A524 #6 `FindPath(ds.transform, "Header With Back Button/Window Title")` | **`FindChild(ds.transform, "Window Title")`** | 那条路径**已经过期**：A519（同一批）把 `Window Title` 改挂到 `Header Background` **底下**了 ⇒ 原路径返回 null ⇒ `edgePx` 回 NaN ⇒ **假红**。父链本身由 A519 那两条钉着（那才是它该断的地方）；按调度台口径（`WD3` §六）按名字取一律用**递归**的 `FindChild` |
| ③ | 规格里的局部名直接裸露在方法体里（`a1` / `fx1…` / `px1…` / `plate` / `cl` / `cv`） | **每段各自包一层 `{ }`** | C# 禁止嵌套作用域**遮蔽**外层局部（CS0136），而 `Run()` 是个 9000 行的**单个方法体**。核过：`Run()` 体（`:907–9026`）**深度 2 的局部名只有 38 个**（清单 → §九），与本批新增名字**零交集**；包一层是**双保险**（也顺带避开深度 3 兄弟块里的 `lx1` 那一族） |
| ④ | A516 段的改坏法注释编号（规格写「⇒ ①③ 红」「⇒ ② 红」） | 按**落地后的实际断言语序**重写 | 规格那套编号是**它自己块的序**，与落地后不一致 ⇒ 照抄会把下一个会话指错（铁律 5 那一类） |

⚠️ **`TmpEdgePx` 这个名字**：简报正文写作 `TMPEdgePx`，`WD3` §七·1 的成品代码写作 **`TmpEdgePx`** ⇒ 照**代码**那个（本件统一用 `TmpEdgePx`）。

### 1. A516 —— `Daily Reward Popup/Timer` 底下那颗 `EverguildTextMeshPro`

- **落点**：`Section(...)` `:6413`、断言 `:6414-6436`（§四 段内、`Check(dr.MissingArt.Count, 0, …)`（`:6439`）**之前** —— 与规格给的锚点一致），整段包在 `{ }` 里。
- **5 条**：① 那颗在且是 `Timer` 的直接子件 ② `Timer` 的 `childCount == 3` ③ **真渲染右缘 = 935.0** ④ 字号 = 50（`FontPxNow`）⑤ **对照条**：这一颗字距 = 0。
- **期望值来源**（⛔ 没有一条来自我们自己的常量）：
  - ③ `935.0` = 原版 dump 的 `245.5 990.8 → 935.0 1054.1`（`WD3` §二 / `WD2` §七·1 两次亲跑）
  - ④ `50` = 原版 `m_fontSize`（dump 的 `字号=50.0` · 基准 50）
  - ② `3` = 原版兄弟序三件（'Más Recompensas En' + `WF_icon_clock` + '19h 23m'；`资料/说明书/04_界面UI/菜单全树.md:1092` + 正本 `资料/日常_原版规格.md:456`）
  - ⑤ `0` = 原版那颗 dump 的 `字距=` 列**不出现**（= 0）
- **标识符核对**（逐个在本文件里存在）：`FindChild`(`:124`) · `Check`(`:29` 泛型) · `CheckTrue`(`:41`) · `CheckNear`(`:56`) · `TmpEdgePx`(本件新增 `:6315`) · `Label.FontPxNow`(`Battle/Label.cs:550`) · `Label.CharSpacing`(`Battle/Label.cs:536`)。

### 2. A517 —— `字距=5` 三颗

- **落点**：`Section(...)` `:6554`、`csNodes` `:6564`（§五 段内，`var succ = …`（`:6494`）之后 —— 规格给的就是这个锚点）。
- **每条断两条**：① `CharSpacing == 5`（抓「有没有补」）② 真渲染**左缘 == 框左沿**（抓「次序对不对」）。
- **期望值来源**：字距 `5` = 原版组件序列化字面量（`WD3` §三 的全包普查：4894 颗 TMP 里只有 12 颗带 5、`fs 67.55` 那一族 8/8 全带、同字体 540 颗里 537 颗是 0）；左沿 `43.00 / 545.91 / 155.00` = 原版 dump 逐颗实读（与我们的 `S_CurLabel.x1` / `S_CurValue.x1` / `H_Title.x1` 逐值相同 —— **但断言写的是原版字面量，⛔ 不调那三个常量**）。
- **标识符核对**：`Label.CharSpacing` ✓ · `Label.Text`(`Label.cs:79`，牙口自报用) ✓ · 值元组 `(Transform, float, string)[]`（本文件首次用；C# 9 / Unity 6 支持，**类型检查已过**）。
- ⚠️ **如实标（新增）**：那颗 `Current Streak Value` 的文案是 `DailyData.StreakCurrentValue()` = **`StreakCurrent.ToString()`（1–2 位数字）** ⇒ 1 位数时**「字距加宽」恒 0** ⇒ 那条 ② 对「次序」这个改坏法**无牙口**（对「压根没接 `AlignLeft`」仍有牙口，但只有 ~3px 量级，见 §四）。

### 3. A518 —— `Fill Line` 的 `scl=1.2` + 九宫

- **落点**：`bool okF` 那条 = `:6597`（§五 段内）。
- **4 条**：左沿 134.43 · 右沿 1919.99 · 上沿 519.06 · 高 79.64。
- **期望值来源**：**原版 prefab 字段算出来的几何** —— `RectTransform_3187973920738910891.json` 亲读（`m_Pivot=(0,0.5)` · `m_AnchorMin=(0,0.5)` · `m_AnchoredPosition=(134.42572,0)` · `m_SizeDelta=(1487.97852,66.37200)` · `m_LocalScale=1.2000001668930054`）+ dump 行末 `视觉框=×1.2 → 视觉 1785.57×79.65`。
- **本件复核过的两个换算**（静态，可复算）：
  - 左沿 = 134.4257（pivot.x=0 ⇒ **不动**）；右沿 = `134.4257 + 1487.9785 × 1.2` = **1919.994**
  - 上沿 = `558.885 − 66.372×0.6` = **519.063**；高 = `66.372 × 1.2` = **79.644** ⇒ 四条全在 0.5px 容差内 ✓
- **为什么必须走 `RectOfUnion`**（`WD3` §八·2 那条陷阱，本件复读源码确认）：`ImageQuad.CreateNineSlice`（`Battle/ImageQuad.cs:422-495`）建的**根节点上没有 `ImageQuad`**，9 块是**子件**（命名 `Fill Line_00…`）⇒ `Wpx` / `CheckRectPx` / `CheckH` 那族走 `GetComponentInChildren<ImageQuad>()` 的 helper **只量到第一块**。本件另核：`border=(4,4,4,4)` + `borderOutPx=(2,2,2,2)` 在此矩形（1785.56×79.64）下**不挤**（`wl+wr = 4px < 1785`）⇒ **9 块全建** ⇒ 并集 = 整框 ✓

### 4. A519 —— 顶栏底下那颗 `Header Background`

- **落点**：`var plate = FindPath(...)` = `:6616`（§五 段内）。
- **7 条**：① 节点在 ② `Window Title` 挂在**它**底下 ③ 量得到渲染矩形 ④左沿 0 ⑤右沿 595.30 ⑥上沿 21.65 ⑦下沿 137.01。
- **期望值来源**：原版 dump（**跑过布局**那一份）`Header Background 0.0 21.7 → 595.3 137.0`；⛔ **不用 `menu_rect.py --relative` 的 `0.00→0.00`**（那是布局跑之前的模板位 —— 这一颗挂 `CSFMinMax + HLG`，宽 = `155 + 379.30 + 61` = 595.30）。
- **本件复核**（静态）：`CreateNineSlice` 逐行算 —— `border=(335,0,395,0)` ⇒ `wl+wr = 730px > 595.30` ⇒ `scX = 0.8155`、**中段宽 0**；`borderOutPx` 未传 ⇒ 上下边 = 0 ⇒ **实际只建 2 块**（i=0 / i=2，均满高），两块**恰好拼满** [0, 595.30] ⇒ **并集 = 整框** ✓（⚠️ `WD3` §七·5 那句「只建 **6** 块」与源码不符，见 §八·1 —— 结论不受影响）

### 5. A520 —— `Current Streak Value` 的父子关系

- **落点**：`var cl = FindChild(succ, "Current Streak")` = `:6635`（§五 段内）。
- **2 条**：① `cv.parent == cl` ② `cv.IsChildOf(cl)`。
- **期望值来源**：原版树层级（`Current Streak` 深 2 / `Current Streak Value` 深 3）。
- **⛔ 按调度台口径**：**不写** `FindPath(succ, "Current Streak/Current Streak Value")`，用**递归**的 `FindChild` —— 本件另核：`FindChild`（`:124`）走 `GetComponentsInChildren<Transform>(true)`，**穿子树** ✓。

---

## 三、A524 的 8 条（落点 · 同上）

### 0. 落地时的两处适配 + 一条自报

| # | 事 | 说明 |
|---|---|---|
| ① | **#6 的路径改 `FindChild`** | 同 §二·0 的 ②（A519 之后原路径已过期） |
| ② | **`edgePx` 放在 §四 之前**（与 `TmpEdgePx` 并列） | #7 在 §四、#1–#6 在 §五 ⇒ 定义点必须在两段**共同的**外层作用域（`Run()` 体，深度 2）。已核：这两个名字在全文件**原先 0 命中** |
| ③ | **新增 `teeth(...)`**（只拼日志文案） | 见 §四 —— 本件跑不了 Unity，把「牙口」这件事**变成可读的读数** |

### 1. 逐条对照（落点 · 期望值来源 · 标识符）

> ⚠️ 行号 = **收工时刻现读**（`CLAUDE.md` 已记「别按行号定位」，这里只为方便对照）。

| # | 落点 | 断什么 | 期望值（**原版字面量**） | 来源 |
|---|---|---|---|---|
| **#1** | `:6519`（§五） | `Current Streak` 真渲染左缘 | **43.0** | 原版 dump `43.0 212.4 → 533.9 295.1` · `Left/Capline` |
| **#2** | `:6523`（§五） | `Current Streak Value` 真渲染左缘 | **545.91** | 原版 dump `545.9 212.4 → 583.8 295.1`（宽 37.85）· `Left/Capline` |
| **#3** | `:6527`（§五） | `Streak Successful/Info` 左缘 | **47.47** | 原版 dump `47.5 830.0 → 1872.5 880.0` · `Left/Midline`（⚠️ 断签面板那颗是 `Center/Midline`，**别取错树**） |
| **#4** | `:6531`（§五） | `Next Rewards text` **右**缘 | **935.0** | 原版 dump `573.3 997.5 → 935.0 1047.5` · `Right/Midline`（**全窗唯一一颗右**） |
| **#5** | `:6536`（§五） | `Timer Text` 左缘 | **985.0** | 原版 dump `985.0 997.5 → 1346.7 1047.5` · `Left/Midline`（⚠️ 与 #4 **同父反向**） |
| **#6** | `:6547`（§五） | 本窗 `Window Title` 左缘 | **155.0** | 原版 dump（路径 = `Header With Back Button/Header Background/Window Title`）· `Left/Capline` · 与 A475 那颗**同值不同颗** |
| **#7** | `:6400`（**§四**，`if (dr.entries.Count > 0)` 块**末尾**） | `Claimed Tex` 左缘 | **255.1** | 原版 dump，**8 份实例逐值相同**；该格**只有 `Collected` 才亮** ⇒ 取现成的 `e0` |
| **#8** | `:6434`（**§四**） | `Timer/EverguildTextMeshPro (1)` 左缘 | **990.2** | 原版 dump `990.2 990.8 → 1454.0 1054.1` · `Left/Capline` |

- **量法**：8 条全走 `edgePx`（读 TMP 自己的 `textBounds`），⛔ **一条都没用 `Label.WorldW` / `TextLeftPx` / `TextRightPx`**（同 A490 那个病灶）—— 本件另核：`TextLeftPx`(`:427`) / `TextRightPx`(`:433`) 的注释自己就写着判据是 `Label.WorldW`。
- **标识符核对**：`FindChild`(`:124`) · `FindPath`(`:136`) · `edgePx`(本件 `:6326`) · `CheckNear`(`:56`) · `CheckTrue`(`:41`)。#7 的 `e0` / `dr`、#8 的 `dr`、#1–#6 的 `succ` / `ds` **全是现成的**（`:6327` / `:6342` / `:6407`）✓
- **⚠️ 找不到就红、不会静默绿**：`edgePx` 对 `null` / 取不到 TMP 回 **`NaN`** ⇒ `CheckNear` 必红 ✓

### 2. 为什么 #7 必须放在那个 `if` 块里

`e0` 是在 `if (dr.entries.Count > 0) { … }`（`:6323-6389`）**块内**声明的 ⇒ 放外面**编不过**。本件把它放在块内**最末**（`premNode/p1` 那两条之后），语义上也是「这一格的三条检查」的延续。

---

## 四、⚠️ 牙口如实表

> 🔴 **口径**：**本件一次 Unity 都没跑** ⇒ 下面**没有一条是「实测」**，全部是**预判**（判据 = 断言的容差 + 可静态算出来的几何量）。
> ✅ **但本件把「验牙口」这件事做成了可读的读数**：① `teeth(...)`（A524 那 8 条）把「框宽 / 实测量到的渲染宽 / 删掉 `Align*` 后的位移」打进日志；② A517 那三条把「字数 / 字号 / 字距加宽预判」打进日志。
> ⇒ **同步点那次 `RewardsScene.Run` 的输出里直接能读出「哪几条位移 ≤ 1.5px」**，不用再跑第二次。

### 1. A524（8 条）—— 改坏法都是「删掉那一句 `MenuDraw.Align*`」

**判据**：位移 = `(框宽 − 本颗渲染宽) / 2`（删掉对齐后文字块回到**框心**）。**位移 ≤ 容差 1.5px ⇒ 两态同形、无牙口**。
⚠️ **框宽是本件从原版 dump 抄的（可静态复算）；「本颗渲染宽」只能跑起来才知道** ⇒ 下表那一列是**预判**，⛔ 不写成「验过了」。

| # | 框宽（原版 · **确定**） | 本颗渲染宽 | 位移预判 | 牙口预判（**未实测**） |
|---|---|---|---|---|
| #1 `Current Streak` | **490.91** | 未知（`'Current streak:'` @70px + 字距5 ≈ 500~590） | ≈ 0 ~ 50px | ⚠️ **可能无牙口** —— 原版那两颗的框是 `CSF(h:PreferredSize)` **撑出来的** ⇒ 框宽 ≈ 原版文字宽 |
| #2 `Current Streak Value` | **37.85** | 未知（1 位数字 @80px ≈ 35~45） | ≈ −3.6 ~ +1.4px | 🔴 **很可能无牙口**（`WD2` 的预判，本件独立复核 = 同一个方向） |
| #3 `Info` | 1825.06 | 长句 @36px | 数百 px | ✅ 牙口充足（预判） |
| #4 `Next Rewards text` | 361.69 | `'More Rewards In'` @36px | ≈ 40px 量级 | ✅ 牙口充足（预判） |
| #5 `Timer Text` | 361.69 | `'19h 23m'` @36px | ≈ 100px 量级 | ✅ 牙口充足（预判） |
| #6 `Window Title` | **379.36** | 未知（原版那颗也是 CSF 撑的 ⇒ 379.30 **就是**原版文字宽） | ≈ 0px | 🔴 **很可能无牙口**（理由同 #1/#2） |
| #7 `Claimed Tex` | 172.97 | `'Claimed'` @33.56px | ≈ 30px 量级 | ✅ 牙口充足（预判） |
| #8 `EverguildTextMeshPro (1)` | 463.78 | `'19h 23m'` @50px | ≈ 100px 量级 | ✅ 牙口充足（预判） |

🔴 **本件对 #1/#2/#6 的额外如实标注**（比 `WD2` 的预判更强的一条推论）：
那三颗的**原版框宽就是原版的文字宽**（CSF 撑出来的）⇒ 若**我们的渲染宽也等于框宽**，则
「接没接 `AlignLeft`」**连节点位置都一样**（`Label.Create` 的落点 = 框心；`AlignLeftOn` 写的是 `框左 + W/2`，`W == 框宽` 时两者相等）
⇒ **不是「这条断言弱」，而是这两态在渲染与位置上完全同一** ⇒ **任何**渲染/位置类断言都不可能分辨它。
⇒ **出路只有两条（由主对话拍板，本件不裁）**：① 认了 —— 据实记「这一颗改与不改看不出来」，删掉这三条；
② 换判据 —— 断**别的**（例如 `MenuDraw.AlignLeft` 的调用计数 / 一颗专门为它加的探针），但那已经不是「盯原版参数」了。
⛔ 现状（本件落地版）是**留着的**，并且**每次 run 都会把位移打进日志** ⇒ 主对话跑一次就知道到底是不是 0。

### 2. A517（3 颗 × 2 条）

| 条 | ① `CharSpacing == 5` | ② 真渲染左缘（次序） |
|---|---|---|
| `Current Streak`（fs 70 · `'Current streak:'` 15 字） | ✅ 确定性牙口（`Label.CharSpacing` = `_tmp.characterSpacing`，删掉 `SetCharSpacing` ⇒ 读回 0 ⇒ 红） | ✅ **牙口充足**：字距加宽 ≈ `0.05 × 70 × 14` = **49px** ⇒ 次序反了偏 **≈24px** ≫ 1.5 |
| `Current Streak Value`（fs 80 · 1–2 位数字） | ✅ 同上 | 🔴 **1 位数时加宽恒 0 ⇒ 对「次序」无牙口**（`WD3` 自己也标了这一颗，本件复核 = 成立）；对「压根没接 `AlignLeft`」仍有 ~3px 量级的位移 ⇒ **卡在容差边上** |
| `Window Title`（fs 67.55 · `'Daily Streak'` 12 字） | ✅ 同上 | ✅ **牙口充足**：加宽 ≈ `0.05 × 67.55 × 11` = **37px** ⇒ 偏 **≈19px** |

**「每字加 0.05 × fontPx」这条是**怎么来的**（本件为牙口推的，写进代码注释了）：
TMP `Runtime/TMP/TextMeshPro.cs:2235` `currentEmScale = m_fontSize * 0.01f * orthographicMultiplier`（非正交 TMP ⇒ `:2232` 的 `orthographicMultiplier = 0.1f`）
× `:3853` 的 `m_xAdvance += … + characterSpacingAdjustment * currentEmScale`
⇒ 每字加宽 = `5 × 0.001 × fontSize`；本工程 `1 fontSize = 10.238px`（`Label.FontSizeToPx`，`Battle/Label.cs:542-545`）⇒ **`0.05 × fontPx`**。
⚠️ **这是推论不是实测**（TMP 源码 + 本工程换算链，两处都亲读过）；若与同步点那次的自报读数不符，**以读数为准**。

### 3. A516 / A518 / A519 / A520

| 账 | 牙口 |
|---|---|
| **A516** ②`childCount==3` | ✅ **确定性**：`MenuDraw.Text`(`Shell/MenuDraw.cs:1553-1571`) ⇒ `Label.Create` ⇒ **恰好一个 GameObject**（`Battle/Label.cs:60-77`）⇒ 删掉那句 = 2 |
| **A516** ③右缘 935.0 | ✅ 确定性：删掉 ⇒ `NaN` ⇒ 红；写成 `AlignLeft` ⇒ 右缘 = `245.5 + 文字宽`（≥ 300px 差）⇒ 红 |
| **A516** ④字号 · ⑤字距 0 | ✅ 确定性（`FontPxNow` / `CharSpacing` 都是直读 TMP 字段） |
| **A518** 四条 | ✅ **确定性几何**（见 §二·3 的算式）：不接 1.2 ⇒ 右沿差 **297.6px**、高差 **13.3px**、上沿差 **6.6px**；绕中心 ⇒ 左沿差 **148.8px** |
| **A519** 七条 | ✅ **确定性**：节点不在 ⇒ ①②③ 红；改挂回顶栏根 ⇒ ② 红；四条矩形是九宫并集（本件逐行算过 = 整框，§二·4） |
| **A520** 两条 | ✅ 确定性（`parent == cl` / `IsChildOf`） |

---

## 五、改坏法总表（每条断什么 · 改坏它会发生什么）

> ⚠️ **本件没跑 Unity** ⇒ 下表是**静态推演**：每条的「红」都给出**凭什么**（几何算式 / 源码行为），⛔ 没有一条写成「实测过了」。

### A640

| 条 | 改坏法（改哪里） | 会发生什么 | 凭什么 |
|---|---|---|---|
| A516 ① | 删 `Shell/DailyRewardPopup.cs` 的 `BuildTimer` 里 `MenuDraw.Text(t, TimerMore, …)` | `moreN == null` ⇒ **红** | 现读 `DailyRewardPopup.cs:396` |
| A516 ② | 同上 | `childCount` 3 ⇒ **2** ⇒ **红** | `MenuDraw.Text` 只建 1 个节点（源码） |
| A516 ③ | 同上 ⇒ `NaN`；或把那句的 `AlignRight` 改成 `AlignLeft` ⇒ 右缘 = 245.5+文字宽 | 两种都 **红** | `Label.AlignRightOn`(`Label.cs:872-880`) 写的是 `worldRight − W/2` |
| A516 ④ | 把那句的 `50f` 改成 `36f` | `FontPxNow` = 36 ⇒ **红** | `Label.FontPxNow`(`:550`) 直读 `_tmp.fontSize` 换算 |
| A516 ⑤ | 给这颗也加 `SetCharSpacing(5f)`（= 「一刀切」那一版） | **红**（原版这颗是 0） | `Label.CharSpacing`(`:536`) |
| A517 ① | 删任一句 `SetCharSpacing(5f)`（`DailyStreakPopup.cs:329/340/502`） | 读回 0 ⇒ **红** | `Label.CharSpacing` |
| A517 ② | 把那句挪到同行 `MenuDraw.AlignLeft` **之后** | 左缘偏 `Δ宽/2` ⇒ **红**（两颗有牙口；`Current Streak Value` 见 §四·2 的例外） | `AlignLeftOn` 头一句 `RefreshBounds()` 量的是**当时**的宽（`Label.cs:862-870`） |
| A518 | ① 改成画布局框（不接 1.2）② 改成绕**中心**缩 | ① 右沿 1622.40 / 高 66.37 / 上沿 525.70 ⇒ **红**（左沿那条**不红**）；② 左沿 −14.37 ⇒ **红** | `ScaleLeftAbout`（`DailyStreakPopup.cs:81-85`）与 `S_FillLine` 四个数 |
| A519 | ① 删 `MenuDraw.Node(h, "Header Background", H_Plate)` 那句 ② 把 `Window Title` 改回挂 `h` | ① **①②③ 红**（后四条在 `if (okP)` 里、不报）② 只 **② 红** | `FindPath` = `Transform.Find`（路径不存在回 null） |
| A520 | 把 `Current Streak Value` 建回 `Streak Successful` 的**兄弟**（`MenuDraw.Text(p, …)`） | **两条一起红** | `Transform.parent` / `IsChildOf` |

### A524（8 条）

| 条 | 改坏法 | 预期（**预判**） |
|---|---|---|
| #1 #3 #5 #7 #8 | 删掉 `Shell/` 里对应的那一句 `MenuDraw.AlignLeft(...)` | 文字回到框心 ⇒ 左缘位移 `(框宽−文字宽)/2` ⇒ **红**（#3/#5/#7/#8 位移充足；#1 见 §四） |
| #2 | 删 `MenuDraw.AlignLeft(curVal, S_CurValue)` | 🔴 **可能仍绿**（框宽 37.85 ≈ 1 位数字的渲染宽）—— 见 §四·1 |
| #4 | 删 `MenuDraw.AlignRight(nxt, S_TimerNext)` **或**把它错写成 `AlignLeft` | 前者右缘左移、后者左缘对而右缘错 ⇒ **两种都红** |
| #6 | 删 `MenuDraw.AlignLeft(title, H_Title)` | 🔴 **可能仍绿**（原版框宽 = CSF 撑出来的文字宽）—— 见 §四·1 |

---

## 六、必查行三条的 grep 结果（本件亲跑，只读）

### ① 断言自证 / 同义反复

| 查什么 | 结果 |
|---|---|
| 我这 13 段里有没有拿**被测实现的常量**当期望值 | **0 处** —— 期望值全是原版字面量（dump / prefab 字段），只在注释里提到 `S_*` / `H_*` / `TimerText` 是「⛔ 不许拿来当期望值」的反例 |
| 我有没有调 `DailyStreakPopup.*` / `DailyRewardPopup.*` | **只在注释里出现**（说明「期望值不是它」）；代码里取节点一律用**名字**（`FindChild` / `FindPath`），不读那两个类 |
| 有没有「从被测实现里读期望值」的口子 | ⛔ 无 —— 唯一被读的是 **TMP 自己的字段**（`CharSpacing` / `FontPxNow` / `textBounds`）与**节点树结构** |

### ② 弱断言分不出两种状态

→ 全部落在 §四 那张表；三条已知嫌疑（A524 #1/#2/#6）**已如实标**，并**加了自报读数**。

### ③ 「凡 `!RectOfUnion` / 『一个 quad 都没有』式断言」

| 查什么 | 命中 | 会不会被打破 |
|---|---|---|
| `!RectOfUnion` 在 `Editor/*.cs` | **2 处**：`RewardsScene.cs:2815`（`if (!RectOfUnion(…)) continue;` —— 那是**取件条件**，不是「不许有图」）· `ShopScene.cs:961`（`CheckRectPxUnion` helper，**肯定式**） | ✅ **不会** —— 两处都不是本窗的节点 |
| 「一个 quad / 一个激活的 `ImageQuad` 都没有」在 `RewardsScene.cs` | 注释里的 4 处（`:311` / `:556` / `:558` / `:5770`）+ 真断言 `:5504`（`wOut…Length == 0`，**战役奖励窗**）、`:5880`（`pr2…Length == 0`，**A302 的合成探针** x=2000~2200 **整块出框**）、`:7377`（`ephPd`，另一扇窗） | ✅ **不会** —— **一条都不在 `§四`/`§五` 那两段里**（本件按行扫过 `:6341-6650`：`ImageQuad` **零命中**） |
| `ScanSoftCuts(succ, …)`（§五 那三条软边断言） | `:598-636` | ✅ **不会** —— 它**带名字闸**：`if (c == null \|\| c.name.IndexOf("_soft") < 0) continue;`（本件复读 `:616` 确认仍在）；九宫的 9 块叫 `Fill Line_00…` ⇒ 数不进来 |
| 既有的 `Header Background` / `Fill Line` 断言 | `RewardsScene.cs` 里**除本件新增之外 0 处**（grep 全文命中全是本件那几行） | ✅ **不会** —— 这两颗在本文件里**此前零覆盖** |
| `childCount` 断言 | 全文件 **33** 处，**没有一处**覆盖 `dr`/`ds` 的 `Timer` / 顶栏 / `Streak Successful`（唯一落在本窗口的是**本件新增**的那条 `:6419`） | ✅ **不会** |
| `CheckHoverSwap(dr.transform/ds.transform, …)`（`:6440` / `:6707`）· `Check(dr/ds.MissingArt.Count, 0)`（`:6439` / `:6706`）· `CheckAbsorbRule(…ds…)`（`:6712`） | 各自现读 | ✅ **不会** —— 本件**一个 `WindowButton` 都没加**、**没引入任何新贴图**（`ArtHeaderBg` / `ArtFillBar` / `ArtClock` 原来就在用） |
| 渲染队列次序那一段（`Section("渲染队列的次序…")`，`:6123` 一带） | 比的是**我们自己的档常量**（页 vs 弹窗） | ✅ **不会** —— 与绘制层数无关 |

---

## 七、没查清 / 没做的

1. 🔴 **一次 Unity 都没跑** ⇒ ① **13 段断言一次都没执行过**（能不能全绿**未知**）；② §四/§五 的**牙口全是预判**；③ 改坏法**全是静态推演**。**这一步不能算「已验」**。
2. **A524 #1/#2/#6 的「两态同形」问题没结** —— 本件给了自报读数（框宽 / 实测渲染宽 / 位移），但**要跑一次才读得到**；要不要删那三条**由主对话拍板**（§四·1 那两条出路）。
3. **A518/A519 的期望值是几何推算**（prefab 字段 → 画布 px），**没在实况里量过**；容差（0.5 / 1.0px）照规格抄的，**没验过抖动幅度**。
4. **A519 的 `595.30` 依赖「我们不跑 uGUI 布局」这个已知偏离**（`WD3` §五·5：原版是 `clamp(155+标题宽+61, 550, 1250)`）—— 换文案会分家；本断言**只覆盖今天这一档**，⛔ 没建 CSF。
5. **A517 的「每字 0.05×fontPx」是推论**（TMP 源码 `:2232-2235` + `:3853` + 本工程换算链）—— 若与自报读数不符**以读数为准**。
6. **`A516` 那颗的文字内容没断**（文案走 `DailyData.StreakNextRewardsText()`，是**复用连登窗那条口**；`WD3` §十·2 已记「建议另立账统一口名」）—— 本件**只断几何/字号/字距**，⛔ 没断文案。
7. **`Header Background (1)` 仍是 `Simple`**（原版 `Sliced`）、**`Fill Line` 的 tint 差一点点**（`(0.43,0,0.06,0.62)` vs 原版 `(0.434,0,0.0567,0.624)`）—— 这两笔**不在本账**（`WD3` §十·1/§十·3 已记「要做」），本件**一行没动**。

---

## 八、顺手发现（⛔ 本件一个都没改）

1. 🔴 **`WD3` §七·5 那句「原版那一颗…只建 **6** 块」与源码不符 —— 实际是 **2** 块**。
   判据 = `Battle/ImageQuad.cs:481-493` 逐行：`border=(335,0,395,0)` ⇒ `wl+wr = 730px > 595.30` ⇒ `scX = 0.8155`、**中段宽 0**（`i=1` 被 `if (w <= 0f …) continue` 跳过）；`borderOutPx` 未传 ⇒ 上下边宽 0（`j=0`/`j=2` 同样被跳过）⇒ 只剩 `(i,j) = (0,1) / (2,1)`。
   ✅ **结论不受影响**（两块恰好拼满 ⇒ 并集仍是整框）⇒ 只是那个**数目**要订正。⚠️ 本件**没改** `WD3` 那份报告（不在白名单）⇒ 请主对话合并。
2. ℹ️ **两条同义的「量文字的边」助手现在并存**（`TmpEdgePx` 走 mesh 顶点 / `edgePx` 走 `textBounds`）—— **有意的**（两批规格各带一条、互相独立 ⇒ 对不上就是有一边错了），而且本件另核过它们**在本工程的口径下逐值应当相同**（`Label.RefreshBounds`（`Battle/Label.cs:799-809`）把网格的 `b.min.x` 摆到 `−anchor.x·W`）。⚠️ 但那意味着「两条都对」也**证明不了**什么 —— 若将来要收口成一条，别以为留下了两个独立证据。
3. 🔴 **「规格成品块写在别的语境里」是个系统性风险**：`WD2` §五 #6 那条路径（`Header With Back Button/Window Title`）在**它自己写的时候是对的**，被**同一批**的 A519 改挂了之后就**静默过期**（`FindPath` 回 null ⇒ `edgePx` 回 NaN ⇒ 假红）。本件核了**其余 12 条**的路径/名字都对得上（`NormalReward/Gacha Reward Claimed/Claimed Tex` · `Timer/Next Rewards text` · `Timer/Timer Text` · `Timer/EverguildTextMeshPro (1)` 四处逐层现读过 `Shell/` 的建树代码）。⇒ **建议**：跨报告落断言时，**路径一律现读复核**（本件就是这么做的）。
4. ℹ️ **`Run()` 体里 `FindChild` 是「按名字递归」**（`:124`），**碰巧会命中**「后来才改挂到更深处」的节点 —— 这正是 #1/#2/#5 三条用 `FindChild` 而不是 `FindPath` 的原因；反过来也说明**这些断言的父子关系约束是松的**（父子关系另有 A520/A519 那几条专门钉）。
5. ℹ️ **`DailyData.StreakCurrentValue()` 是 `StreakCurrent.ToString()`**（`Shell/DailyData.cs:825`）⇒ 那颗的渲染宽**随连胜天数变**（1 位 vs 2 位数字）⇒ A524 #2 的牙口**还会随天数漂**（本件只覆盖今天这一档，⛔ 没做「状态 → 参数」表 —— 那一层要跑起来才知道）。

---

## 九、类型检查结果

```text
$ TMPDIR=/tmp/wf_we4b bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

✅ **运行时 0 · 编辑器 0** —— 一共跑了 **5 次**（第一次 = 三段落完立刻；第二次 = 加完 `teeth`/牙口自报；第三次 = 收尾改注释；第四次/第五次见下面那条假红）。**没有一条错落在别人的文件上（稳态下）**。

🔴 **如实记一次「假红」**（正是 `CLAUDE.md` 13·3 那条现象，**不是我的文件**）：
收尾那次跑，**运行时报告 6 个错**，全部是 `error CS1061`：
`Assets/CardPresentation/Battle/BattleDriver.cs` 上 `ChatPopupPanel.PointerUpAt` / `AttackSelector.HoverPickReady` / `AttackSelector.MarkHoverPicked` 未定义。

- **判据**：① 逐文件归并 `grep -E "^Assets" | sed 's/(.*//' | sort | uniq -c` ⇒ **6/6 全在那一个文件**；② 那个文件 `git status` = **`M`**、**mtime = 10:41:49**，而我跑检查的那一刻是 **10:41:52**（**3 秒前**）⇒ 那一刻有写手正在写它；③ 我第三次跑（几分钟前）是 **0/0**。
- **处置**：按简报 —— ⛔ **没碰别人的文件**，**等 90 秒重跑** ⇒ 运行时 **0** / 编辑器 **0**。
- ⇒ **我的 `Editor/RewardsScene.cs` 在全部 5 次里从未出现过一条错**。

### 落地纪律自检

| 项 | 值 |
|---|---|
| 行尾 | ✅ 纯 LF（二进制读：**CRLF 0 / LF 9415**；改前 9174 行）—— **没用 `sed -i`**，全部走 **Edit** |
| `git diff --numstat` | `957 / 41` —— 我的三段 = **52 + 46 + 143 = 241 行、纯增量（0 删除）**；其余 716 增 / 41 删是**别人**在飞的改动 |
| 我发过的 `Edit` 调用 | **14 个**，全部落在 `Editor/RewardsScene.cs`（3 个大段 + 11 处注释/牙口自报的字面订正）⇒ ⛔ 没碰任何别的 `.cs` |
| ⛔ 没跑 Unity | 一次 `-executeMethod` 都没调（Unity 全局串行、只有主对话能跑） |
| ⛔ 没动 git | 只用只读 `git diff` / `git diff --numstat` |
| ⛔ 没改两张正本 | `项目任务.md` / `CLAUDE.md` **一个字没动** |

### 附：`Run()` 体里【深度 2】的既有局部名（判「有没有遮蔽」用，本件亲跑脚本扫的）

`area · b3 · badge0 · badge1 · bar · bar0 · before · bg · camView · camp · collect · cpan2 · dr · ds · dupOf · fail · fgo · forge · inbox · inboxContent · loginCard · mdNode · mhName · msgList · msgListContent · msgListVp · nm · refill · rows · rw0 · skullsCard · states · succ · tab · warnNode · weekly · win · wm2`（**38 个**；➕ 本批新增 3 个 —— `TmpEdgePx :6315` / `edgePx :6326` / `teeth :6347` = **41 个**）
⇒ 与上表**零交集**；本批其余新增的局部名（`timerN / moreN / moreLb / csNodes / a1 / plate / cl / cv / fx1… / px1… / nch / dW / okF / okP`）全在各自 `{ }` 里、也不与上表相交。方法体范围：`Run()` 的 `{` 在 `:907`，闭合在 **`:9267`**（本件落地**之后**的现读；改前是 `:9026`）。
