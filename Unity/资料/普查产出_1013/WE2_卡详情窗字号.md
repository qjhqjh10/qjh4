# WE2 · 卡片详情窗「计数条」auto-fit（A404）

> 2026-10-13 · 执行写手 WE2 · 账 = **A404**（「清空 A 表」第四轮）。
> 状态：**已落地**（改了 2 个 `.cs`）· 秒级类型检查 **0 错**。
> ⛔ 本件**没跑 Unity**（派活纪律：子代理一律不许 `-executeMethod`）⇒ 那 21 条断言**至今一次没跑过**，
> 本条只能算「已接上」，**要等调度台在同步点跑 `CollectionScene.Run` 才算绿**。

---

## 一、结论

**A404 成立，根因就是 `wrapPx` 传的 `0f`**：三颗计数 TMP（`Counter` / `Slash` / `Duplicates text`）
都只传了 `autoMinPx`（25 / 25 / 18），而 `MenuDraw.Text` 的自适应那一段**整段写在 `if (wrapPx > 0f)` 里面**
⇒ **一次都没执行过**，这三颗**从来没开过自适应**（字段有、画面没有 = 静默失败）。
本机全量扫了一遍（119 个 `MenuDraw.Text(` 调用点，脚本见 §五末）：
**补完这三处之后，「`wrapPx = 0f` 却传了非 0 `autoMinPx`」的调用点 = 0 处**（与 F1 报的「只有这 3 处」吻合）。

**做了两件**（都在白名单内）：

1. **补 `wrapPx`** = **原版那一颗的 `m_SizeDelta.x`**：**`73.283` / `28.02` / `44.09`**（判据 = 正本 §三 的 `sd`，
   `资料/阶段二_卡片详情窗_原版规格.md:90-92`）。
2. 🔴 **补「折行档还原」`SetWrapping(false)`** —— ⛔ 这**不是额外发挥**，是**让第 1 件不引入新偏离的必需品**：
   传 `wrapPx` 会**无条件把折行模式开成 `Normal(1)`**（`Label.SetWrapWidth` → `TmpFont.SetWrapWidthRect`
   `t.textWrappingMode = TextWrappingModes.Normal`，那个函数头上就写着「调用方靠紧跟的 `SetWrapping(false)` 还原」），
   而**原版这三颗逐颗现读都是 `m_TextWrappingMode = 0`（`NoWrap`）**
   （MB pid `1892` / `1848` / `1823`，`bundle_scenes_scenes_mainmenuwarpforge`——本件亲自读的，见 §六·0）。
   ⇒ **只补 `wrapPx` 不加这一句，等于把折行档从「0（本来就对）」改成「1（偏离）」**。
   成对写法在本仓有几十处先例（A205 / A34-F4 那一族），且这一句**只在改模式时才重排**（`SetWrappingMode` 的相等早退）。

**没做**（只报不改，理由见 §六）：`autoBasePx`（原版 **36/36/35**）· 水平对齐档（原版 **4/2/1**）·
TMP 容器高那 0.006px 的原版舍入差。

---

## 二、A 表「文件写错」的订正说明（真身在哪 · 死支路在哪）

A 表原文（`项目任务.md:395`，转引 F1 报告）写的是：
> 「`Shell/MenuDraw.cs` 有 3 个调用点把 `autoMinPx` 传进了死支路（**`:649/:653/:654`**，`wrapPx = 0f` …）」

**照这条去找会走到完全不相干的地方**（本件现读复核）：

| 角色 | A 表指着哪 | **现读实况** |
|---|---|---|
| **调用方（真身）** | `Shell/MenuDraw.cs:649/653/654` | **在 `Shell/CardDetailPopup.cs` 的 `BuildCounter()` 里** —— 本件改动前是 **`:674-675`（`Counter`）/ `:678`（`Slash`）/ `:679-680`（`Duplicates text`）**，本件改动后 **`:689-691` / `:694-696` / `:697-699`** |
| 那三行现在是什么 | —— | `Shell/MenuDraw.cs:649` 起是 **`ApplySoftEdges` 的 XML 文档注释**（讲 uv0 入参），`653/654` 是注释正文 —— **一行都不是调用点** |
| **死支路（被调方）** | （未点名） | **`Shell/MenuDraw.cs:1562-1569`** —— `Text(...)` 里 `if (wrapPx > 0f) { SetWrapWidth(...); if (autoMinPx > 0f && fontPx > autoMinPx) SetAutoFitBox(...); }`。**死的是这一段**（调用方不传 `wrapPx` ⇒ 整段跳过） |

**错因**：F1 那次是从**别的文件**（`CardDetailPopup.cs`）数出来的调用点，写进 A 表时把文件栏写成了
`Shell/MenuDraw.cs`（那是**被调方**所在的文件）；行号也对不上（`649/653/654` 是 `ApplySoftEdges` 的注释行）。
F1 报告自己的 §五·1 **写对了文件与行号**（`Shell/CardDetailPopup.cs:649/653/654` 那一版的号）——
是**抄进 A 表那一步错的文件**。
> ⚠️ **A 表那条「要么补 `wrapPx`、要么清掉死实参」的岔路可以删掉** —— 答案明确是**补 `wrapPx`**（见 §一）。
> ⛔ 本件**没改** `项目任务.md`（不在白名单里），这一节交给调度台合并。

---

## 三、逐处改动清单

### 3·1 `d:/4/Unity/MyGame/Assets/CardPresentation/Shell/CardDetailPopup.cs`

| # | 位置（改后行号） | 补的 `wrapPx` | 取自哪个 rect / 哪条判据 |
|---|---|---|---|
| ① | `:689-691`（`"x" + inDeck` · 节点名 `Counter`） | **`CcWrapX1 = 73.283f`** | 正本 §三 第 1 行 `sd(73.283, 70.586)` = `CcX1L..CcX1R`（869.41→942.69，**我们那个 rect 算出来是 73.28** —— 差 0.003px，取**原版 sd**） |
| ② | `:694-696`（`"/ "` · 节点名 `Slash`） | **`CcWrapSl = 28.02f`** | 正本 §三 第 2 行 `sd(28.02, 70.586)` = `CcSlL..CcSlR`（942.69→970.71，两边一致） |
| ③ | `:697-699`（`spares.ToString()` · 节点名 `Duplicates text`） | **`CcWrapX2 = 44.09f`** | 正本 §三 第 3 行 `sd(44.09, 50)` = `CcX2L..CcX2R`（970.71→1014.80，两边一致） |

**另外新增的常量**（`:142-146`，紧挨现有 `Cc*` 那一族）：
```csharp
        /// <summary>🔴 **2026-10-13（A404）**：三颗计数 TMP 的**自适应容器宽** = 原版那一颗的 `m_SizeDelta.x`
        /// （TMP 的折行/自适应容器就是这个 `sizeDelta`；判据 → `资料/阶段二_卡片详情窗_原版规格.md:90-92` 的 `sd`）。
        /// ⚠️ 与上面那三个 rect **不是同一个数**：rect 由 `CcX1L/R` 等给出（原版坐标只抄到 0.01 ⇒ 宽 73.28），
        /// 而原版 `sd.x` 是 **73.283**（差 0.003px）—— 这里取**原版 `sd`**（喂 TMP 容器的是它，不是画 rect 那个）。</summary>
        public const float CcWrapX1 = 73.283f, CcWrapSl = 28.02f, CcWrapX2 = 44.09f;
```
**每处的形状**（三处同形，`⛔` 没有改别的实参）：
```csharp
            var lbC = MenuDraw.Text(box, new PxRect(CcX1L, CcY1, CcX1R, CcY2), "x" + inDeck, Color.white, "Counter",
                          CcX1Px, QCdText, CcWrapX1, 25f);      // ← 第 7 参 wrapPx：0f → CcWrapX1
            if (lbC != null) lbC.SetWrapping(false);            // ← 还原原版 `折行=0`
```
（原代码是 `…, CcX1Px, QCdText, 0f, 25f);` —— **只动了第 7 个实参**，`fontPx`/`autoMinPx`/`autoMaxPx`/`autoBasePx` 一个字没动。）

**改动的形状**：`git diff --numstat` = **24 插入 / 5 删除**（5 = 原来那三行的旧文本）；
行尾**没被翻**（改前改后 `CRLF=0`、`LF` 计数 852 → 871 = +24−5 ✓）。

### 3·2 `d:/4/Unity/MyGame/Assets/CardPresentation/Editor/CollectionScene.cs`

- **落点**：**`:4031-4090`**（`if (cnt != null) { … }` 那一块**之后**、`// 三块面板的动作` **之前**）。
- **避让**：W-E4（A503）那 19 条在 **`:5091+`**（本件改完后的行号，调度台给的原始区间是 `:5075-5151`）——
  **本件一个字都没碰它**（本件是**纯插入**：`git diff --numstat` 本件那一段 = 60 插入 / 0 删除；
  整个文件 135/0，差额 75 是 W-E4 的）。
- 位置选在这里的理由：**在 `Craft`/`Upgrade` 两次点击之前**（那两次会改 `Owned`/`Level` 并重建视图），
  量的是「刚开窗那一版」的数字与版面。

---

## 四、断言清单（21 条 = 3 颗 × 7 条）

**判据来源**（全部**原版值**，⛔ 没有一条写我们那一侧传进去的实参）：
`资料/阶段二_卡片详情窗_原版规格.md:90-92`（§三 表）+ **本件逐颗现读原版 MB**
（`bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/MonoBehaviour_{1892,1848,1823}.json`）
+ **一份运行时实况 dump 佐证**（`资料/原版实拍/menu_topbar_0927/runtime_ui_dump_MainMenu_Warpforge.tsv:186-189`，
实况那三格的 `sizeDelta` = **73.3 / 28.0 / 44.1**、`50.0`，与序列化 `sd` **逐位吻合** ⇒ 「框」这个量有**静态 + 实况两条独立来源**）：

| 节点 | 原版 `m_fontSize`(auto) | 原版 `m_SizeDelta` | 原版 `m_TextWrappingMode` | MB pid |
|---|---|---|---|---|
| `…/Counters/Counter` | 52.6 (25–52.6) | (73.283, 70.586) | **0** | 1892 |
| `…/Counters/Slash` | 57 (25–57) | (28.02, 70.586) | **0** | 1848 |
| `…/Slash/Duplicates text` | 52.6 (**18**–52.6) | (44.09, 50) | **0** | 1823 |

| # | 断什么 | 期望值来源 | **改坏法**（怎么改会红） |
|---|---|---|---|
| 1 | `Label` 节点在 | —— | 节点改名 |
| 2 | **`flb.AutoSizing == true`** | 原版 `m_enableAutoSizing = 1` | **三处 `wrapPx` 改回 `0f`** ⇒ 那一段永不执行 ⇒ 红（**A404 的判别式**） |
| 3 | `FontSizeToPx(lb.FontSizeMin) ≈ 25 / 25 / 18` | 原版 `m_fontSizeMin`（px 口径） | ①同上（改前这俩是 TMP 出厂 **0/0**）②`autoMinPx` 传错（25→10） |
| 4 | `FontSizeToPx(lb.FontSizeMax) ≈ 52.6 / 57 / 52.6` | 原版 `m_fontSizeMax`（= 原版 `m_fontSize`） | ①同上 ②`autoMaxPx` 传错 |
| 5 | **`lb.WrappingMode == 0`** | 原版 `m_TextWrappingMode = 0`（逐颗现读） | **删掉紧跟的 `SetWrapping(false)`** ⇒ 回 1 ⇒ 红（**第 2 件的判别式**） |
| 6 | **TMP 容器宽 = `LayoutSpace.Px(73.283/28.02/44.09)`**（读 `TMPro.TextMeshPro.rectTransform.sizeDelta.x`） | 原版 `m_SizeDelta.x` | **三处 `wrapPx` 改回 `0f`** ⇒ 容器停在**出厂 100** ⇒ 红（**A404 的另一条判别式**；⛔ 不写「我们传了多少」= 同义反复） |
| 7 | **渲出来 `WorldW×WorldH ≤ 原版框 73.283×70.586 / 28.02×70.586 / 44.09×50`（容差 1.5px）** | 原版 `m_SizeDelta` | 传一个**偏大**的字号 / 把框改小 ⇒ 红 |

### 4·1 🔴「**渲染宽度 ≤ 框宽**」那条**写了**（第 7 条，宽 + 高一起断）

- 量的东西 = **`Label.WorldW/WorldH` = TMP `textBounds` 的真测量**（`Label.RefreshBounds`）——
  ⛔ 不是节点位置、⛔ 不是「对齐枚举 == Left」那种同义反复（本仓 `TitleLeftPx` 头上记过这一条）。
- 容差 **1.5px**：TMP 自适应是**二分**，收敛粒度 = `fontSizeDelta > 0.051` 才继续
  （`Runtime/TMP/TextMeshPro.cs:4139`），折成 px ≈ 0.5px；留 1.5px 是给「advance 宽 vs 墨宽」那点差。
- ⚠️ **如实说清这一条的判别力**（本仓惯例：断言要写清「对哪件事没有判别力」）：
  **它对 A404（wrapPx 缺失）没有判别力** —— 本夹具三颗的文案是 `x2`（`cap = 2`）/ `/ `/ **一位数**
  （`spares = 9`：`CardDetailPopup.NeedCopies` = `{0,0,2,5,9}` ⇒ `Owned − min(Owned,cap) = 9`），
  按 52.6 / 57px **不缩**大概也塞得进那三个框（⚠️ 三个字的墨宽**没在实机上量过**，这句是估的）。
  它是**另一族**（「字号字段对、字却溢出」）的守卫 —— 同 `Editor/RewardsScene.cs` 里记过的同一件事
  （「折行一开、宽也 ≤ 框宽，两种状态都绿」）。
  ⇒ **咬住 A404 的是第 2、3、4、6 条（共 12 条）**，这是本件**唯一的判别式**。

### 4·2 落点与形态

- 全在一个 `{ }` 块里（不污染外围作用域），`var fitCases = new[] { … }` 驱动一个 `foreach`。
- 7 条里 6 条走本文件既有的 `CheckTrue` / `CheckNear` / `Check`（`:32-44` / `:219`），**没新加辅助函数**（⛔ 不改公共件）。

---

## 五、没查清 / 没做的

1. ⛔ **断言一次没跑过**（子代理不许跑 Unity）—— 第 7 条的 1.5px 容差、以及
   「这三颗在实机上到底缩没缩」都是**按 TMP 源码推的**，不是实测。请调度台在同步点跑
   `CollectionScene.Run`（**只碰了 `CardDetailPopup.cs` + `CollectionScene.cs` ⇒ 按覆盖面只跑这一条**）。
2. **A404 只让「容器/字段」与原版一致** —— 容器宽在**折行=0**下的作用路径是
   **`TextMeshPro.cs:3542-3588`「Text Exceeds Horizontal Bounds - Reducing Point Size」**
   （`isBaseGlyph && textWidth > widthOfTextArea` 那一支的 `else` 分支：**NoWrap 时按字宽缩字号**；
   ⚠️ 我一开始读错了，以为 NoWrap 只走「纵向超界」（`:3044`）那一支 ⇒ **已就地订正**），
   所以「框宽」确实会驱动收缩 —— 但**具体缩到几号、字落在格里哪个位置**都要实机才知道。
3. **没碰共用件 `Shell/MenuDraw.cs`**（`Text(...)` 的 `wrapPx` 语义 = 顺带开折行，是 A484 刚收口的件）——
   本件只在**调用方**补参 + 还原档位。
4. **没断 TMP 容器高**：我们传进去的是 `r.H`（= `CcY1..CcY2` = **70.58**），原版 `sd.y` = **70.586**
   （同一个「坐标只抄到 0.01」的舍入）⇒ 差 0.006px，写成断言只会给未来留一条噪声线。第 7 条已从这个方向覆盖。
5. **没改 `项目任务.md` / 没改 `资料/阶段二_卡片详情窗_原版规格.md`**（都不在白名单）——§二 与 §六 的订正请调度台合并。
6. 本件 §一 那条「全量扫过 119 个 `MenuDraw.Text(` 调用点」用的是**python 括号配对**扫的
   （脚本当场写、当场跑，按顶层逗号切参）：`wrapPx == "0f"` 且 `autoMinPx != "0f"` 的**剩余命中 = 0**。
   ⚠️ 它只认**字面量写法**（`CcWrapX1` 这种符号不会命中）⇒ 能证「没有第 4 处字面量 `0f`」，**不是全称证明**。

---

## 六、顺手发现（⛔ 本件一个都没改）

0. **[本件改动自身要用到的判据]** 原版这三颗的 `m_TextWrappingMode` **都是 0**、`m_enableAutoSizing` **都是 1**、
   `m_fontSizeBase` = **36 / 36 / 35** —— 逐颗现读（MB pid `1892` / `1848` / `1823`，
   `d:/2/新解包资源/assets_full/bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/`）。
   ⚠️ 正本 §三 那张表**没有「折行」这一列**，F1 也没记 ⇒ **不要**拿「正本没写」当「原版是 1」。

1. 🔴 **`autoBasePx` 缺省值 ≠ 原版**（**要做**，属 A305① 那一族，不是 A404）：
   原版三颗的 `m_fontSizeBase` = **36 / 36 / 35**，而我们三处都没传第 10 参 ⇒ base 落到「调用方那一档」
   （= 52.6 / 57 / 52.6px）。按 `Label.SetAutoFitBox` 自己的注释，base **只影响二分起点**、渲染差
   ≤ 0.05 fontSize 单位（≈0.5px）—— **小，但不是 0**。判据做起来是现成的（上面那三个数），
   一行一个（`…, 25f, 0f, 36f` / `…, 25f, 0f, 36f` / `…, 18f, 0f, 35f`）⇒ 建议单开一条（⛔ 本件没改）。

2. 🔴 **水平对齐档没建模**（**要做**，另一笔）：原版三颗 `m_HorizontalAlignment` = **4 / 2 / 1**。
   TMP 的枚举是 `Left = 0x1, Center = 0x2, Right = 0x4, Justified = 0x8, Flush = 0x10, Geometry = 0x20`
   （`Runtime/TMP/TMP_Text.cs:74-77`，**这是本件亲眼核的**）⇒ **4 = `Right`**（**不是** `Flush`）。
   我们这三颗一律是 `Center`（`TmpFont.NewText` 的出厂档，`MenuDraw.Text` 不设对齐）⇒
   **第 1 颗（原版右对齐）与第 3 颗（原版左对齐）在各自格里的落位与原版不同**
   （两处都是「半格左右的量级」，**⚠️ 没实机量过**）。
   做法照本仓惯例（⛔ **不是**去改 TMP 的 `alignment`）：`Shell/MenuDraw.AlignLeft/AlignRight(Label, PxRect)` ——
   `Shell/AllianceMemberTab.cs` 那一族就是照原版档位逐站接的（那边把「没传 ⇒ 被推到左边缘」明确记成**真偏离**）。
   > ⚠️ 顺带一条**文档错**：`资料/阶段二_卡片详情窗_原版规格.md:90` 把 `4` 注成 **`4(Flush)`** ——
   > `Flush = 0x10`，**`4` 是 `Right`**（同表 `:91` 的 `2(Center)`、`:92` 的 `1(Left)` 都对，只有这一格错）。
   > 建议就地订正（铁律 5），⛔ 本件没改那个文件（不在白名单）。

3. 🟡 **`Duplicates text` 身上挂着一个 `LayoutElement`，`Counters` 上挂着一个 `HorizontalLayoutGroup`** ——
   两条都**查过、都不影响本件**，但第 2 条留了一个**位置**上的未决问题：

   **(a) LayoutElement（MB pid 2444，挂在 `Duplicates text` 上）—— 死值，不影响本件。**
   实读：`m_Enabled=1` · `m_HorizontalFit=2`(`PreferredSize`) · `m_VerticalFit=0` ·
   `clampWidth=1` · **`widthMin=17.7215`** · **`widthMax=44.099998`** · `clampHeight=0`。
   ⇒ 它要生效得**父节点上有布局组**，而 `Duplicates text` 的父是 `Slash`
   （组件只有 `RectTransform` + `CanvasRenderer` + `TextMeshPro`，**没有布局组**）；
   即使有，`Counters` 那个组的 **`m_ChildControlWidth = 0`**（见下）也不会理它。
   ⇒ **44.09 这个宽不是它算出来的**（但 `widthMax` 恰好 = 44.09，多半是同一次编辑留下的）。
   ✅ **结论：本件的 `wrapPx = 44.09` 不受它影响，不用动。**

   **(b) 🔴 `Counters` 上真有一个 `HorizontalLayoutGroup`（MB pid 2176）—— 它可能**重排三个格子的 x 位置**。**
   实读：`m_Enabled=1` · `m_Padding = (10,10,0,0)` · **`m_Spacing = 5`** · `m_ChildAlignment = 0`(UpperLeft) ·
   `m_ChildForceExpandWidth/Height = 1/1` · **`m_ChildControlWidth = 0`** · `m_ChildControlHeight = 1` ·
   `m_ChildScaleWidth/Height = 0/0` · `m_ReverseArrangement = 0`。
   - **宽度那一半已经定案**：`ChildControlWidth = 0` ⇒ **不吃布局、用各自的 `sizeDelta`**
     ⇒ 三个格子的宽就是 `73.283 / 28.02 / 44.09`（**本件的 `wrapPx` 与断言 #6 站在实地上**）。
     `ChildControlHeight = 1` + `ForceExpandHeight = 1` ⇒ 两个直接子件的高 = 容器高 = **70.586**
     —— **与它们序列化的 `sd.y` 一致**（第三个 `Duplicates text` 是 `Slash` 的子件、不归这个组管，保持 50）✓。
   - **位置那一半没查清**：`ChildControlWidth = 0` 时 Unity 的 `HorizontalOrVerticalLayoutGroup`
     **仍然会沿轴重排**（`SetChildAlongAxisWithScale`：`pos = padding.left + Σ(子件宽 + spacing)`）。
     ⚠️ **两说并存**：
     · **序列化那一说**：三格**首尾相接、零间距**（869.41→942.69→970.71→1014.80），
       与 `spacing = 5` / `padding = 10` **对不上**；
     · **布局组那一说**：真跑起来会按 5px 间距 + 左内边距 10 重新排 ⇒ 三格的 x **整体左移约 23px**。
     · **实况 dump 说**（`资料/原版实拍/menu_topbar_0927/runtime_ui_dump_MainMenu_Warpforge.tsv:186-189`）：
       `Counter` pos **(−50.7, −5.5)** · `Slash` pos **(0, −5.5)** · `Duplicates text` pos (0,0)
       —— **与序列化值逐位吻合**（`−50.652` = `Counter` 中心 906.05 − `Counters` 容器中心 956.70 ✓）。
       ⚠️ **但那一份 dump 里整棵树 `activeSelf=True / activeInHierarchy=False`**（窗没开）
       ⇒ **布局组可能压根没跑过** ⇒ 它**证不了**「开窗之后也不重排」。
     ⇒ **定案只能靠真 Play**：开一次卡片详情窗、dump 那三个节点的 `m_AnchoredPosition`。
     **建议挂进 `资料/真Play待验清单.md`**（⛔ 本件不推断、不猜）。⚠️ **它不影响 A404**（本件动的是「容器宽」）。
     📌 反过来说：**CLAUDE.md §三 那条「0×0 / 落在父容器外的 rect 是 VLG 布局跑之前的模板位」在这里有嫌疑** ——
     这两处都是**布局组 + 手工摆位**并存，正是那条坑的形状。

4. 🟡 **`Counter` 那一格的「对齐 4」顺带解释了一个疑点**：三个格子是**首尾相接**的
   （869.41→942.69→970.71→1014.80），而 `Right`/`Center`/`Left` 正好让 `x2` 贴着 `/`、`9` 也从 `/` 后面起 ——
   ⇒ 原版那串读起来是**连续的 `x2/ 9`**，不是三块各自居中。这条是旁证，不是判据（**没有实拍**）。

---

## 七、类型检查结果

```
TMPDIR=/tmp/wf_we2 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---   运行时错误数: 0
--- 编辑器程序集 ---   编辑器错误数: 0
```
跑了两遍（改完 `CardDetailPopup.cs` 一遍；改完 `CollectionScene.cs` 的注释后又一遍），**两遍都是 0/0**，
**没有出现「错误集中在别人的文件上」那种并发症状**（调度台提醒的 W-E4 并发写入**没发生** ——
本件对 `CollectionScene.cs` 的两次 Edit 都一次成功，且 `git diff --numstat` 显示本件那一段是**纯插入**）。

**本件改了哪些行**（给调度台对账）：
- `Shell/CardDetailPopup.cs` —— `:142-146`（新常量 5 行）· `:679-699`（注释 + 三处调用点，净 +19/−5）
- `Editor/CollectionScene.cs` —— `:4031-4090`（**纯插入 60 行**，A404 那 21 条断言）
- 新建 `资料/普查产出_1013/WE2_卡详情窗字号.md`（本文件）
- ⛔ 没碰 `项目任务.md` / `CLAUDE.md` / `Shell/MenuDraw.cs` / `工具/*` / git
