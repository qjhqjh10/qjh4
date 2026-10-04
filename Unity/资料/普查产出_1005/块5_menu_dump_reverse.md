# 块5 · `menu_dump.py` 布局求解补 `m_ReverseArrangement`（2026-10-05）

> 结论一句话：**工具以前完全不读 `m_ReverseArrangement`** ⇒ 凡 `reverse=1` 的布局组，打出来的
> 「名字 ↔ 位置」是**镜像**的。现已按 uGUI 源码补上，并加了第 ④ 条 fixture 钉死。
> **影响面：`bundle_menus_assets_all` 17 个布局组 · 14 个宿主窗 · 181 个后代节点坐标变了 · 最大位移 540.00 px。**

---

## 一、改了什么

| 文件:行号 | 改前 → 改后 |
|---|---|
| `工具/menu_dump.py:714`（`apply_layout_to_children` 内、`use_scale` 之后） | **无** → 新增 `rev = bool(mb.get('m_ReverseArrangement'))`（带 17 行出处注释） |
| `工具/menu_dump.py:745`（主轴落位循环） | `for (k, mn, pf, fx, u, sf) in per:` → `seq = list(reversed(per)) if rev else per` + `for … in seq:` |
| `工具/menu_dump.py:431`（`describe()` 的 HV 分支） | 参数列**不带** reverse → 组名那一格**前缀**印 `**reverse=1**（主轴按树序倒排…）`（放最前，因为文本模式那列有 `BODY_MAX=200` 截断，放末尾会被 `Deck Options` 那一行正好砍掉） |
| `工具/menu_dump.py:893`（`walk()`） | `out` 条目 **无** `lgreverse` → 新增 `lgreverse` 字段 |
| `工具/menu_dump.py:1303`（`main()` 结尾） | **无** → 新增「🔴 `m_ReverseArrangement=1` 的布局组 N 个」清单块（表列仍是**树序**，读的人必须被提醒「别按树序第一个 = 最左去读名字」） |
| `工具/menu_dump.py:1046-1114`（`verify_layout()`，函数在 `:903`） | fixture ③ 之后 **无** → 新增 **fixture ④**（倒排回归用例，判据/期望值全靠手算，逐条写在注释里） |

### uGUI 语义（判据，逐行读的本地源码）

`MyGame/Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/Layout/HorizontalOrVerticalLayoutGroup.cs`

```
:153-155  int startIndex = m_ReverseArrangement ? rectChildren.Count - 1 : 0;
          int endIndex   = m_ReverseArrangement ? 0 : rectChildren.Count;
          int increment  = m_ReverseArrangement ? -1 : 1;
:182      float pos = (axis == 0 ? padding.left : padding.top);       // ← 初值不变
:198      for (int i = startIndex; m_ReverseArrangement ? i >= endIndex : i < endIndex; i += increment)
:216          pos += childSize * scaleFactor + spacing;                // ← 推进方向不变
```

⇒ **`pos` 的初值与推进方向都不变**，变的只是**哪一颗子件来消费当前这个 `pos`**：
`reverse=1` 时**树序最后一个**子件落在**起点**（HLG = 最左 / VLG = 最上），树序**第一个**落在终点。
⇒ 一整排的坐标**集合通常不变**，但**名字与位置整体镜像** —— 这正是「读数看着很像对的」那种错。

**只翻主轴这一处，另两处按源码确认与顺序无关，没动**：

* `CalcAlongAxis:100-128` 三个总量是**求和**（`totalMin += …`）⇒ 顺序无关；
* 交叉轴 `alongOtherAxis` 支（`:156-179`）每颗子件**各自算自己的 `GetStartOffset`**（只依赖它自己）。

---

## 二、🔴 Deck Options 验收锚（`bundle_menus_assets_all` / `Deck info Popup`）

组参数（MB `-6741779870866962008`）：`spacing=-50` · `align=5 (MiddleRight)` · `pad=0` ·
`ctrlW=0 ctrlH=0` · `expandW=1 expandH=1` · `scaleW=0 scaleH=0` · **`m_ReverseArrangement=1`** ·
`m_Enabled=1`；组自身矩形 `1263.74,93.00 → 1783.62,243.00`；5 颗子件 `sizeDelta` 全 `74.386×75.605`。

| 树序 | 名字 | 改前 menu_dump 给的世界矩形 | 改后 | 排第几（左→右） |
|---|---|---|---|---|
| 0 | `Switch Deck Info Button` | **1333.33**,130.20→1407.72,205.80 | **1709.23**,130.20→1783.62,205.80 | 改前第 1 / **改后第 5（最右）** |
| 1 | `Duplicate Button` | 1427.31,130.20→1501.69,205.80 | 1615.26,130.20→1689.64,205.80 | 改前第 2 / 改后第 4 |
| 2 | `Share Button` | 1521.28,130.20→1595.67,205.80 | 1521.28,130.20→1595.67,205.80 | **不动**（正中那颗） |
| 3 | `Share On Chat` | 1615.26,130.20→1689.65,205.80 | 1427.31,130.20→1501.69,205.80 | 改前第 4 / 改后第 2 |
| 4 | `Delete Button` | **1709.23**,130.20→1783.62,205.80 | **1333.33**,130.20→1407.72,205.80 | 改前第 5（最右）/ **改后第 1（最左）** |

**验收锚成立**：改后 **`Delete Button` = 这一排最左那颗**、**`Switch Deck Info Button` = 最右那颗**。✅

手算核对（照 uGUI 源码，不是照工具）：

```
tot_min = tot_pref = 5×74.38600158691406 + 4×(−50) = 171.9300079345703     (min==pref ⇒ minMaxLerp=0)
surplus = 519.8800048828125 − 171.9300079345703   = 347.9499969482422
tot_flex = 5 × max(0,1) = 5 > 0  ⇒ fmul = 69.58999938964844 ⇒ pos 不走 GetStartOffset、仍 = pad.left = 0
cell = 74.38600158691406 + 69.58999938964844 = 143.9760009765625 ; 步进 = cell − 50 = 93.9760009765625
ctrl=0 ⇒ offsetInCell = (cell − sizeDelta.x) × align(1.0) = 69.58999938964844
⇒ 格 i 的组内左边缘 = 93.9760009765625×i + 69.58999938964844
   = 69.590 / 163.566 / 257.542 / 351.518 / 445.494   (末格右缘 519.880 = 组宽 ✓)
reverse=1 ⇒ 树序 i 落在格 (4−i) ⇒ Delete=69.590(最左) … Switch Deck Info=445.494(最右) ✓ 与工具输出逐位一致
交叉轴：ctrlH=0 · 半分 0.5 · requiredSpace = Clamp(150, 75.605, 150) = 150
⇒ 组内顶 = 37.1974983215332、底 = 112.8025016784668（**与 reverse 无关**）
```

---

## 三、影响面

### 3·1 `reverse=1` 的组：分子/分母怎么数出来的

```bash
cd "d:/2/新解包资源/assets_full"
# 分子（=1 的布局组）
grep -rl '"m_ReverseArrangement": 1' bundle_menus_assets_all/MonoBehaviour | wc -l      # 17
# 分母 A：HVLayoutGroup（m_ReverseArrangement 只存在于这个类，GridLayoutGroup 没有 —— 见源码）
grep -rl '"m_ChildControlWidth"'      bundle_menus_assets_all/MonoBehaviour | wc -l      # 1420
# 分母 B：再加 GridLayoutGroup（m_CellSize）
grep -rl '"m_CellSize"'               bundle_menus_assets_all/MonoBehaviour | wc -l      # 47
```

| 包 | `reverse=1` | HV 布局组（分母 A） | Grid 布局组 | 分母 A+B |
|---|---|---|---|---|
| `bundle_menus_assets_all` | **17** | 1420 | 47 | 1467 |
| `bundle_mainmenualwaysloaded_assets_all` | **0** | 1 | 0 | 1 |
| `bundle_generalgamewindows_assets_all` | **0** | 9 | 0 | 9 |

**全库（`assets_full` 所有包）**：`reverse=1` = **17**（全在上面那 3 个包里、且**全在 `bundle_menus_assets_all`**）；
HV = **1751**、Grid = **73** ⇒ 全部布局组 = **1824**。

### 3·2 🔴 「17 / 1734 / 1751」三个数各自是什么（口径写清）

* **分子 = 17**（=1 的）。
* **1751 = 分母**，不是分子 —— 它既是「**带了 `m_ReverseArrangement` 这个键**的 MB 文件数」
  （= 全部 HVLayoutGroup，因为该字段 **`= false` 也照样序列化**），也是「带 `m_ChildControlWidth` 的文件数」。
  ⇒ **「有人扫到 1751」是分母被当成了分子**（文件里 `: 1` 与 `: 0` 都含 `m_ReverseArrangement` 这个子串）。
* **1734 复现不出**：1824（HV+Grid）、1751（HV）、1829（带 `m_ChildAlignment` 的）、2051（带 `m_Padding` 的）
  **没有一个是 1734**。⇒ 那条待办里的 1734 **判为陈旧/口径丢失**，**以本节为准**。
* ⚠️ 数的时候**不能按 `m_Name` 过滤**（布局组的 `m_Name` **常常是空的**，名字在 GameObject 上）—— 本清单
  **从不看 `m_Name`**，只按字段指纹数。

### 3·3 逐处：宿主父链 → 我们工程的哪个文件

17 处**互不嵌套**（逐个爬 `m_Father` 验过）。

| # | 宿主节点 | 类型 | 宿主父链（宿主 → 窗根） | 我们工程 | 结论 |
|---|---|---|---|---|---|
| 1 | `Checkbox` | HLG (2 子) | selectButton ← Controls ← RightSide ← window ← **Alliance Trophy Info Popup** | `Shell/TrophyInfoPopup.cs` | ⚠️ **判不了** —— 工具自打 `unk`（`ctrl=1`+TMP 首选尺寸要字体度量），两个孩子都退化到 0 宽；我们的实现也已在注释里写明「是**我们挑的**」 |
| 2 | `Score Levels` | VLG (5) | Scoring Bar Event Score Info ← Reward Display ← Event On Going Tab ← **Two Sides Event Window** | **未建** | — |
| 3 | `Score Levels` | VLG (5) | Alliance Event Score Info ← In Alliance ← Alliance Event Score Panel | **未建** | — |
| 4 | `Alliance Skull Count` | HLG (3) | Alliance Content ← Content ← **Draft Mode Expiring Popup** | **未建** | — |
| 5 | `Deck Options` | HLG (5) | **Deck info Popup** | `Shell/DeckInfoPopup.cs` | 🔴 **建反了**（见下） |
| 6 | `Score Levels` | VLG (5) | Scoring Bar Event Score Info ← Reward Progress Panel ← **EnergySinglePlayerOnlyEventWindow** | `Shell/LiveOpsEventWindow.cs`（共用基类；**这一扇窗本身没建**） | 不适用 |
| 7 | `Score Levels` | VLG (5) | … ← Draft Mode Deck Info Panel ← **Draft Mode Timed Mode Window** | **未建** | — |
| 8 | `Score Levels` | VLG (5) | … ← **Draft Mode Deck Info Panel** | **未建** | — |
| 9 | `Tab Buttons` | VLG (**1 子**) | Chat ← Holder ← **ChatPanel** | `Shell/ChatPanel.cs` | ✅ 倒排是**空操作**（只有 1 颗子件） |
| 10 | `Score Levels` | VLG (5) | Scoring Bar Event Score Info ← Reward Display ← **SkirmishModeEventWindow** | `Shell/SkirmishEventWindow.cs`(+`LiveOpsEventWindow.cs`) | 🔴 **建反了**（见下） |
| 11 | `Control Buttons` | HLG (3) | Header ← Select Deck Tab ← Tabs ← Content Area ← **Collection Menu Variant** | `Shell/CollectionWindow.cs` | 🔴 **建反了**（见下） |
| 12 | `ButtonIcons` | HLG (2) | To Battle Button ← **RankedEventWindowV2** | `Shell/RankedEventWindow.cs`(+`LiveOpsEventWindow.cs`) | 🔴 **建反了**（见下） |
| 13 | `Score Levels` | VLG (5) | **Alliance Event Score Info** | **未建** | — |
| 14 | `Content` | VLG (**0 子**) | Viewport ← Message List ← Content ← **Inbox Menu** | `Shell/InboxWindow.cs` | ✅ 导出里 `m_Children` **是空的** ⇒ 倒排是空操作（**也说明这处 `Content` 的卡片是运行期填的**） |
| 15 | `ButtonIcons` | HLG (2) | To Battle Button ← **SkirmishModeEventWindow** | `Shell/SkirmishEventWindow.cs`(+`LiveOpsEventWindow.cs`) | 🔴 **建反了**（见下） |
| 16 | `Score Levels` | VLG (5) | … ← Draft Mode Pay State ← **Draft Mode Timed Mode Window** | **未建** | — |
| 17 | `Score Levels` | VLG (5) | … ← Draft Mode Deck Info Panel ← **Draft Mode Menu Demo** | **未建** | — |

#### 🔴 逐处「建反了」的实据（**只报不改** —— 那些文件正被别的写手占着）

* **#5 `Shell/DeckInfoPopup.cs`**：`opts[]` 数组（`:748-755`）是**树序** `[Switch Deck Info, Duplicate, Share,
  Share On Chat, Delete]`，而 `:733` `x1 = optX0 + i * OptStep` 让它**从左往右**摆 ⇒ **现在建的是
  `Switch Deck Info` 最左**。修正后应当是 **`Delete` 最左**。
  🔴 **并且：该文件 `:698-700` 那条「2026-10-03 就地订正（A10）」本身就是这次 bug 的产物** ——
  它写「我们原来是『`Delete` 最左』…… 那条注释猜的是『`reverse=1` ⇒ 与树序相反』，**实测不成立**」，
  而那次「实测」拿的正是**镜像读数**。⇒ **A10 把一处本来正确的实现改成错的，A10 要回滚。**
* **#10/#12/#15 `Shell/LiveOpsEventWindow.cs:606-612`**（`SkirmishEventWindow` 与 `RankedEventWindowV2` 共用）：
  代码把 `TrophyIcon` 放在 `x2 = BIconsR`（**最右**）、`ShieldIcon` 摆在它左边。
  修正后应当是 **`TrophyIcon` 最左（1285.97）、`ShieldIcon` 最右（1357.66）**。
* **#11 `Shell/CollectionWindow.cs:1941-1956`**：注释与代码都是 `Create`(1180) → `Import`(1450) → `Unlock`(1720)。
  修正后应当是 **`Unlock` 1180 / `Import` 1391.00 / `Create` 1661.01**（`Unlock` 原版 `m_Enabled=0` 不可见，
  我们照纪律不建，但那两条**要跟着挪**）。
* **#1 `Shell/TrophyInfoPopup.cs`**：**本工具判不了**（`unk`）。我们的实现显式选了「方框在左、文字在右」
  并注明了「是我们挑的」；按 uid 语义倒排会让 `Label` 在左。**⇒ 需要另用别的手段核，别拿工具这句话当证据。**

### 3·4 坐标变了的窗口 / 节点 + 最大位移

`bundle_menus_assets_all` 全包 **181 个节点**坐标变了，**全局最大位移 540.00 px**。
（比对法：把改前的 `menu_dump` 源码在内存里 `exec` 成第二个模块，两边各喂一份**全新** `Bundle`，
逐节点比 `rect` 四元组；`walk` 会**就地改** RT dict，所以两边必须各用一份，否则比的是同一份。）

| # | 组 | 窗（链顶） | 子件数 | 挪位节点数 | 最大 Δ | 逐颗（**改前 → 改后**，组内主轴坐标） |
|---|---|---|---|---|---|---|
| 11 | Control Buttons | `Collection Menu Variant` | 3 | 7 | **540.00** | `Unlock` 1720→1180 · `Import` 1450→1391.00 · `Create` 1180→1661.01 |
| 5 | Deck Options | `Deck info Popup` | 5 | 12 | **375.90** | 见 §二 |
| 2/3/6/7/8/10/13/16/17 | Score Levels（9 处，全部 `align=4`·`ctrl=0`·`spacing=0`） | Two Sides / EnergySinglePlayerOnly / SkirmishMode / Draft Mode Timed / Draft Mode Deck Info / Draft Mode Menu Demo / `Alliance Event Score Info` | 5 | 各 16–20 | **259.03** | `Level 1` ⇄ `Level 5`（`Level 3` 不动）：`Level 1` 顶 +259.03 · `Level 2` +129.52 · `Level 4` −129.52 · `Level 5` −259.03 |
| 4 | Alliance Skull Count | `Draft Mode Expiring Popup` | 3 | 2 | 83.96 | `Main Icon` 945.02→1028.97 · `Individual rating value` 1005.02→945.02 |
| 12/15 | ButtonIcons | `RankedEventWindowV2` / `SkirmishModeEventWindow` | 2 | 各 2 | 71.69 | `ShieldIcon` ⇄ `TrophyIcon` |
| 9 | Tab Buttons | `ChatPanel` | 1 | **0** | 0 | 只有 1 颗 ⇒ 空操作 |
| 1 | Checkbox | `Alliance Trophy Info Popup` | 2 | **0** | 0 | 两颗都退化到 0 宽、贴右端 ⇒ 倒排对读数**无影响**（且该组是 `unk`） |
| 14 | Content | `Inbox Menu` | **0** | **0** | 0 | `m_Children` 是空的 ⇒ 空操作 |

**顶窗汇总（按变动节点数）**：`Draft Mode Timed Mode Window` 32 · `SkirmishModeEventWindow` 22 ·
`Two Sides Event Window` 20 · `EnergySinglePlayerOnlyEventWindow` 20 · `Alliance Event Score Panel` 16 ·
`Draft Mode Deck Info Panel` 16 · `Alliance Event Score Info` 16 · `Draft Mode Menu Demo` 16 ·
`Deck info Popup` 12 · `Collection Menu Variant` 7 · `Draft Mode Expiring Popup` 2 · `RankedEventWindowV2` 2 ·
`Alliance Trophy Info Popup` 0 · `ChatPanel` 0。

---

## 四、uGUI 有、`menu_dump` 没建模的字段（**只报不改**）

以本地 uGUI 源码为准（`PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/Layout/`）：

| # | 字段 / 组件 | 出处 | 现状 | 量级（`bundle_menus_assets_all`） |
|---|---|---|---|---|
| 1 | **`m_ReverseArrangement`** | `HorizontalOrVerticalLayoutGroup.cs:80,153-155,198` | ✅ **本次补上** | 17 处 |
| 2 | **`LayoutUtility.layoutPriority`** | `LayoutUtility.cs:135-175`：「**优先级最高**的那个 ILayoutElement 说话；**同优先级取 max**」 | ❌ **未建模** —— 现在是按 `m_Component` 序「**后写覆盖**」 | 挂 **≥2 颗** `LayoutElement` 的 GO **全包只有 1 个**（1333 个 GO 挂了 ≥1 颗）⇒ **量小但是真缺口** |
| 3 | **`ContentSizeFitter`**（`ILayoutSelfController`） | `ContentSizeFitter.cs` —— 按子件首选尺寸**回写自己的 `sizeDelta`** | ❌ 未建模（表里给的是序列化 `sizeDelta`） | **882 个** |
| 4 | **`AspectRatioFitter`**（`ILayoutSelfController`） | `AspectRatioFitter.cs` —— 按 `m_AspectMode` 回写自己的尺寸/锚点 | ❌ 未建模 | **2136 个** |
| 5 | **`RectTransform.m_LocalRotation`（z 非零）** | —— 旋转会让绝对矩形与外接框不等 | ❌ 未建模（`rect_of` 拿到 `scale` 参数却**根本不用**） | **203 / 16510** 个 RT 的 z 分量非零（**是否落在布局组内没查**） |
| 6 | **`GridLayoutGroup`** | `GridLayoutGroup.cs`（`m_CellSize`/`m_StartCorner`/`m_StartAxis`/`m_Constraint`） | ❌ 只返回 `'grid?'` **出声不出数**（照纪律是对的） | 47 个 |
| 7 | **嵌套布局组作为子件的首选尺寸** | `LayoutGroup.minWidth/preferredWidth/flexibleWidth` | ❌ `unknown=True` **出声**（要递归算，含字体） | —— |
| 8 | **TMP / `Text` 的首选尺寸** | `Text.cs` / TMP 的 `ILayoutElement` 实现 | ❌ `unknown=True` **出声**（要 Unity 字体度量） | —— |
| 9 | **`ScrollRect`（`m_Content` 驱动 Content 尺寸）** | `ScrollRect.cs` | ❌ `unknown=True` **出声** | —— |
| 10 | `LayoutElement.m_IgnoreLayout` | `LayoutGroup.cs:60-79` | ✅ 已建（且**正确地不看 `m_Enabled`**，与 `LayoutUtility` 那条**故意不一样**） | 1334 |
| 11 | `m_Enabled=0` 的 `LayoutElement` | `LayoutUtility.cs:152-155` | ✅ 已建（2026-10-05 补的） | 18 个带有效值 |

---

## 五、没查清的

1. **#1 `Checkbox`（`Alliance Trophy Info Popup`）到底建没建反 —— 判不了。**
   工具自己打 `unk`（`ctrl=1` + TMP 首选尺寸要字体度量），两个孩子宽度算出来都是 0、
   都贴在组右端 ⇒ **倒排前后读数一模一样**。要判它得另找手段（真 Play 截图，或读原版 prefab 里
   `Toggle`/`Label` 的 `m_SizeDelta` 再按字体估首选宽）。
2. **`1734` 这个数从哪来 —— 复现不出**（见 §3·2）。只能在下一轮把那条待办的数**就地改成 17/1751**。
3. **203 个带 z 旋转的 RT 里，有几个落在布局组内** ⇒ 会不会影响这批 `reverse=1` 的读数 —— **没查**。
4. **`ContentSizeFitter` / `AspectRatioFitter`（#3/#4，共 3018 个组件）到底改掉了几张表里的 w/h** ——
   **没查**（要做的话是另一个块：逐节点比「序列化 `sizeDelta`」vs「自控后的真值」）。
5. **#6 `EnergySinglePlayerOnlyEventWindow` 这一扇窗我们只有共用基类、没建窗** ——
   它算「未建」还是「建了一部分」，**由调度台定**。

---

## 六、顺手发现（不属本件，**我没动**）

1. 🔴 **`Shell/DeckInfoPopup.cs:698-700` 的 A10「就地订正」是这次 bug 的直接产物，应当回滚。**
   A10 的原文是「原来『`Delete` 最左』……『`reverse=1` ⇒ 与树序相反』**实测不成立**」——
   那次「实测」用的就是镜像读数。**A10 把一处本来正确的实现改成了错的。**
   （同文件 `:711-717` 还挂着一条已自认「走的是旧模型」的 `OptStep = OptW − 50f` 待改项，
   `Editor/CollectionScene.cs:915-923` 也有配套的期望值待改 —— 与本条**同一批**。）
2. **#14 `Inbox Menu>Content` 的 `m_Children` 在导出里是空数组**（0 子件）。同一个包里
   `Message List` 之下的 `Content` 本该装信件的行 —— ⇒ **那一层是运行期填的**，
   照 prefab 搭树会**少一层且看不出来**（同族：本文件头 `new_stats()` 那条「深度截断不许静默」）。
3. `Shell/TrophyInfoPopup.cs:341-344` **已经写明了**「dump 里那两个子件是退化值（都 0 宽、贴在右端）」
   —— 本次修复**没有改变**这个结论（倒排前后都是 0 宽），但**那条注释是对的、别当它过期**。
4. `menu_rect.py` 的 `rect_of(rt, parent_rect, scale)` **收了 `scale` 参数但函数体里一次都没用**
   （`menu_rect.py:162-185`）。`menu_dump.walk` 一路把它传下去，等于**放大/缩小子树的绝对矩形也没乘缩放**。
   ⛔ 我**没动**它（简报划在红线外），**先报**。
5. `bundle_mainmenualwaysloaded_assets_all` 里**只有 1 个 HV 布局组**、
   `bundle_generalgamewindows_assets_all` 里 **9 个** —— **两包 `reverse=1` 各 0 个** ⇒
   这两条线**以后也不必担心这个坑**。

---

## 七、自查

* **改了哪些文件**：只有 `工具/menu_dump.py`（+ 本报告）。⛔ 没碰两张正本 / 任何 `.cs` / `menu_rect.py` / `d:/2/`。
* **跑的命令与结果**：
  * `python d:/4/Unity/工具/menu_dump.py --verify-layout` → **EXIT=0**，**四条 fixture 全绿**
    （① Tab Buttons 矩形 + 6 页签 ② VLG `align=0` 回归 ③ 缩放子件 a/b ④ **新增：倒排 5 颗 + 「最左 = `Delete Button`」**）。
    前三条的期望值**一个字没动** ⇒ 本次改动对 `reverse=0` 的组**零影响**（同一套断言原先怎么过、现在还怎么过）。
  * `python 工具/menu_dump.py bundle_menus_assets_all "Deck info Popup" --depth 12 --no-sprite`（文本模式）→
    `Delete Button` 1333.33…、`Switch Deck Info Button` 1709.23…；结尾多出「🔴 `m_ReverseArrangement=1` … 1 个」块。
  * 同命令 `--md` → `Deck Options` 那一行的竖线数 = **16**（与表头一致，**没多列**；结尾那两个 0 竖线的匹配行是页脚文字，不是表行）。
  * `python 工具/menu_dump.py --verify-layout >/dev/null` → `EXIT=0`。
* **行尾 / `git diff --numstat`**：
  * `io.open('工具/menu_dump.py','rb')` → `CRLF 0 / LF 1315`（**纯 LF，没翻**）。
  * `git diff --numstat -- 工具/menu_dump.py` → `316 52`（改动前该文件在未提交工作区里已经是 `188 43`
    ⇒ **本次净增 +128 / −9**，远小于文件 1315 行 ⇒ **没有整篇重写**）。
* **改 `.py` 的方式**：全程 `Edit` 工具；⛔ 没用 `sed -i`、没用 `io.open(p,'wb').write(<表达式>)`。
* ⛔ 没跑 Unity、没动 git、没改正本。
