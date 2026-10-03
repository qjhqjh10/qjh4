# `menu_dump.py` 两条缺口修 + 两条影响面报告（2026-10-05）

> 执行代理产出（**一件活一个代理**）。**只碰了 `d:/4/Unity/工具/menu_dump.py`**，其余全是只读。
> 判据正本 = `项目任务.md` §三 第 29 条 **A59 / A60**（我只读，没改）。
> 全量表格在**附录 A/B**（A = flexible 351 行 · B = `m_ChildScale` 274 行）。

---

## 结论（3 行以内）

1. ✅ **两条缺口都补了**：`--md` 表加了「局部缩放→视觉框」列（并与文本模式的 `scl=` 同门槛）＋ 结尾一段「布局框 ≠ 视觉框」清单；`--depth` 截断子树时改为**出声**（「还有 N 个节点没印、最深到第 D 层、用 `--depth D`」），另把「纯 `Transform`（3D）子件没进表」也一并报出来。
2. 📋 **两条只报告**：`_child_sizes` 的 `flexible`（只补那一处 = **32 窗口 / 351 节点**，最大 **347.95px**）；`m_ChildScale` 偏移（**61 窗口 / 274 节点**，最大 **99.07px**）。
3. 🔴 **两条欠账不独立**（本轮查出来的最重要一条）：`flexible` 一落，**`--verify-layout` 的 fixture ③ 会变红**（实测 `icon` 中心 1174.85 → **1137.15**、`text` 左 1213.95 → **1176.25**），也就是 **A59 要落的那两个绝对坐标会再变一次** ⇒ 建议**先定 flexible，再一次性改 `MissionRerollPopup` 的常量**，否则要改两遍。

---

## 改动清单

文件：**`d:/4/Unity/工具/menu_dump.py`**（唯一改过的文件）

| 项 | 读数 |
|---|---|
| `git diff --numstat` | `155 / 10 / Unity/工具/menu_dump.py`（+155 / −10，原文是制表符分隔） |
| `git diff --stat` | `1 file changed, 155 insertions(+), 10 deletions(-)` |
| 行数 | 906 → 1051 |
| 行尾 | **没翻** —— 改后实测 `CRLF 0 / LF 1051`（改前也是纯 LF） |
| 自检 | `python Unity/工具/menu_dump.py --verify-layout` → **三条 fixture 全 ✅、返回码 0** |
| 验收命令 | `python Unity/工具/menu_dump.py bundle_menus_assets_all "Alliance Trophy Info Popup" --depth 12 --md` → 返回码 0、表全 |
| `--md` 列数一致性 | 该窗 30 行**全部 15 列**（原来那 1 行 16 列的坏行已修，见「顺手发现 2」） |
| 数字回归 | 把新增那一列去掉后与**改前逐行比：31 行里只有 1 行不同**（就是那个 `\|` 转义），**所有既有数字一字未动** |

**改了什么**（5 处，全在 `menu_dump.py`）：

1. 新增常量 `SCL_EPS / SCL_WARN_EPS / SCL_WARN_MAX` + 小件 `_scl_of()` / `_scl_is_one()` / `_scl_cell()` / `_md()`。
2. `--md` 表新增一列 **`局部缩放→视觉框`**（插在「宽×高」右边）：缩放 = 1 的行印 `—`；否则印 `**×1.2 → 视觉 67.20×56.00**`（视觉框为 0 时再补一个 ⚠️）。门槛与**文本模式的 `scl=` 完全同一个**（`1e-6`）⇒ 两种模式对「哪几行带缩放」的判断逐行一致。
3. `walk()` 新增 `stats=` 形参（默认 `None` ⇒ `verify_layout` 那三处调用零影响）：① `depth > maxdepth` 时不再静默 return，改记「截掉几个节点 / 最深到第几层 / 第一层被截的是谁」；② `rt` 查不到时记「纯 `Transform` 子件」与「真缺件」两个数。
4. 结尾新增三段**出声**（两种模式都会印，且**跟着 `--active-only` 过滤**，与表里显示的一致）：深度截断 / 纯 `Transform` 子件 / **全查不到的真缺件**（本轮实测真缺件数为 **0**）。
5. 顺手两条小修（都零风险、理由见「顺手发现」）：`--md` 单元格转义 `|`；`scl` 读成 `rt.get('m_LocalScale') or {...}`（原来 `localScale` 是 JSON `null` 时会崩）。

改完的实测输出（`Alliance Trophy Info Popup`，默认深度）：

```text
🔴 **本表不全：`--depth 6` 把子树截断了** —— 还有 **4** 个节点没印（那儿最深到第 **9** 层）⇒ 要看全用 `--depth 9`（**没印 ≠ 不存在**）：
                  Fill Area
                  CheckMark
```

`--depth 9` 实测 ⇒ 印出 4 件、警告消失；`--depth 13` 对 `Missions Tab` 印 417 行 = 1 + 416（与警告里的 416 逐位吻合）。

---

## 报告一：flexible=1 的影响面

### 判据（本地就有权威源，不用推断）

Unity 自带 uGUI 源码：`D:/Unity/Hub/Editor/6000.3.23f1/Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/UGUI/UI/Core/Layout/HorizontalOrVerticalLayoutGroup.cs`

```csharp
private void GetChildSizes(RectTransform child, int axis, bool controlSize, bool childForceExpand,
    out float min, out float preferred, out float flexible)
{
    if (!controlSize) { min = child.sizeDelta[axis]; preferred = min; flexible = 0; }
    else { min = LayoutUtility.GetMinSize(child, axis); preferred = LayoutUtility.GetPreferredSize(child, axis);
           flexible = LayoutUtility.GetFlexibleSize(child, axis); }
    if (childForceExpand) flexible = Mathf.Max(flexible, 1);      // ← 在 if/else 【外面】
}
```

⇒ A60 ② 那条**成立**：`m_ChildControl*=0` 且 `m_ChildForceExpand*=1` 时 `flexible` 是 **1**，本文件给 `0`。

🔴 **但同一文件里还有两处**（`:205-216`，`SetChildrenAlongAxis` 的主轴循环）：**只补 `flexible` 不够** ——

```csharp
float childSize = Mathf.Lerp(min, preferred, minMaxLerp);           // :205
childSize += flexible * itemFlexibleMultiplier;                     // :206  ← 与 controlSize 无关
if (controlSize) SetChildAlongAxisWithScale(child, axis, pos, childSize, scaleFactor);      // :209
else { float offsetInCell = (childSize - child.sizeDelta[axis]) * alignmentOnAxis;          // :213
       SetChildAlongAxisWithScale(child, axis, pos + offsetInCell, scaleFactor); }  // :214 3 参版：sizeDelta 不变
pos += childSize * scaleFactor + spacing;                          // :216  ← 步进用的是【带 flexible 的】childSize
```

> 行号 = **调度台那份**（`d:/4/Unity/MyGame/Library/PackageCache/com.unity.ugui@27635d171b1a/…`，与编辑器
> `BuiltInPackages/…` 那份**逐字节相同**，本轮 `diff -q` 核过 ⇒ 两边行号通用）。
> 上面 `GetChildSizes` 的 `if (childForceExpand) flexible = Mathf.Max(flexible, 1);` 在同一份的 **`:237-238`**。

本文件现在把「**要写回的 sizeDelta**」与「**步进/落位用的 cell**」**混用同一个 `new`**，且**漏了 `offsetInCell`**
⇒ **只补 `flexible`（下面 W2）会得到一个 uGUI 里不存在的中间状态**（对齐偏移没了、格也没撑开）。
⇒ 所以下面给了**两个提议**：**W2 = 只补那一处**（A60 ② 的字面读法）、**W3 = 完整 uGUI**（补 `cell` + `offsetInCell`）。

### 影响面（全 308 个根窗口 · 深度 12 · 三个变体各走一遍）

| 变体 | 含义 | vs 现状 |
|---|---|---|
| **W2** | 只把 `!ctrl && fexp` 的 `flexible` 改成 1.0 | **32 窗口 / 351 节点**，最大 **347.95px**（`Deck info Popup`） |
| **W3** | `flexible=1` **＋** `cell = childSize`（步进）**＋** `offsetInCell`（落位） | **29 窗口 / 376 节点**，最大 **278.36px**（`Deck info Popup`） |
| W2 → W3 | 补了 W2 之后**还要再变**的量 | **34 窗口 / 428 节点**，最大 **347.95px** |

> ⚠️ **W3 才是 uGUI**。上面三行合起来说明：**这条要么按 W3 一次做对，要么别做** ——
> 只落 W2 的话，输出比现状**更不像** uGUI（该撑开的没撑开、该有的对齐没了）。

**机制**（三个变体都实测过、与源码逐条对得上）：`ctrl=0 & fexp=1` 时 `tot_flex` 由 0 变 >0 ⇒
① `startOffset` 那条**对齐偏移整条消失**（`surplus > 0 && totFlex == 0` 才走它）⇒ 子件从「按 `align` 居中/贴底」变成「贴组的左上角」——**这一条就是那些 37.7px 级别的位移**；
② W3 再让每格按 `flexible × fmul` 撑开（`pos += childSize × sf + spacing`），整组正好填满。

### 代表窗口（其余见附录 A 的全量 351 行）

| 窗口 | 变的节点 | 最大位移 | 位移最大那一处 |
|---|---|---|---|
| `Deck info Popup` | 15 | **347.95px** | `Switch Deck Info Button` x1 `1611.69 → 1263.74`（整颗钮左移） |
| `Main Menu Settings Window` | 2 | **322.00px** | `Register Button` x1 `1213.25 → 891.25`（整颗钮左移） |
| `ReRollPopup Variant` | 8 | 37.70px | `ButtonLeft`/`Price Display` 两键各 −37.70（`Buttons` 组 `ctrlW=0 expandW=1 align=4`） |
| `Draft Mode Timed Mode Window` | 53 | 40.36px | `icon` x1 `1168.91 → 1128.54`（轮抽窗 53 处） |
| `Player Profile Window` | 8 | 1.07px | `Rating Text` x1 `790.50 → 789.43`（`align` 为左对齐 ⇒ 只差一点） |

**`Alliance Trophy Info Popup` 不在清单里** —— 它的 `Controls` VLG 虽然也是 `ctrl=0 & expand=1`，但 `align=0 (UpperLeft)` ⇒ 对齐偏移本来就是 0 ⇒ **两式同值**。（这就是「自检绿 ≠ 这条对」的同一个形状。）

### 🔴 落这条会撞 **A59 的坐标**（本轮实测，务必先看这节）

`ReRollPopup Variant` 窗里有两层会被动到的组：外层 `Buttons`（`ctrlW=0 expandW=1 align=4`）和内层 `Price Display`（`ctrlW=0 expandW=0 scaleW=1`）。外层一动，**整块 `Price Display` 跟着挪** ⇒ A59 要落的两个**绝对**坐标会再变一次：

| 状态 | `icon` 中心 x | `text` 左边缘 | 出处 |
|---|---|---|---|
| **W0** = 2026-10-04 之前（连 `m_ChildScale` 也没有） | 1169.25 | 1202.75 | A59 原话 |
| **W1** = **现状**（`m_ChildScale` 已落、`flexible` 未落） | **1174.85** | **1213.95** | A59 要落的值 · 本轮 `--verify-layout` 实测 |
| **W2**（只补 `flexible`） | **1137.15** | **1176.25** | 本轮实测（`--verify-layout` fixture ③ **变红**） |
| **W3**（完整 uGUI） | **1193.70** | **1232.80** | 本轮实测（同上） |

⇒ **两条欠账不独立**：A59 的 `MissionRerollPopup.IconR/PriceTextR`、`Editor/RewardsScene.cs` 的 `CheckAt`/`CheckNear`
**只有在 W1 下才是这两个数**。**建议：先定 flexible（W2/W3/不做），再一次性改那三处** —— 否则要改两遍。
📌 写这份报告时看到调度台已把 **A59 裁定为「落地」**（四处改动与目标值见 `资料/普查产出_1004/调度台_裁定与复核_1005.md` §一）
⇒ **下面这条顺序风险更要紧**：那四处要落的 `1174.85 / 1213.95` 是 **W1 的值**，`flexible` 一落就要再改一次。
⚠️ 三个 fixture 里 **只有 ③ 会红**（①② 的组 `expand=0` 或 `align=0` 不受影响），所以**红的是哪一条也要读清楚**：红 ≠ 算法坏了，而是**期望值本身钉的是旧行为**。

<details><summary>报告一 · 逐窗口汇总（完整 32 行）</summary>

| 窗口 | 变的节点 | 最大位移 px | 位移最大那一处（旧 ⇒ 新） |
|---|---|---|---|
| Alliance Event Score Info | 20 | 10.69 | `Alliance Score Bar Line Level 1` (1364.11,462.88)→(1659.73,523.36) ⇒ (1364.11,452.19)→(1659.73,512.67) |
| Alliance Event Score Panel | 20 | 10.69 | `Alliance Score Bar Line Level 1` (1363.19,449.76)→(1658.81,510.24) ⇒ (1363.19,439.06)→(1658.81,499.55) |
| Base Game Mode Container 1x1 | 5 | 6.81 | `Event Title` (706.08,689.31)→(1219.78,745.01) ⇒ (706.08,682.49)→(1219.78,738.20) |
| Base Game Mode Container 1x1 Claim | 5 | 6.81 | `Event Title` (706.08,689.31)→(1219.78,745.01) ⇒ (706.08,682.49)→(1219.78,738.20) |
| Base Game Mode Container 1x1 Claim AutoCollect | 5 | 6.81 | `Event Title` (706.08,689.31)→(1219.78,745.01) ⇒ (706.08,682.49)→(1219.78,738.20) |
| Base Game Mode Container 1x2 | 5 | 6.81 | `Event Title` (-253.92,1402.82)→(259.78,1458.53) ⇒ (-253.92,1396.00)→(259.78,1451.71) |
| Base Game Mode Container 1x2 No Timer | 1 | 25.08 | `Event Title` (-253.92,1421.08)→(259.78,1476.79) ⇒ (-253.92,1396.00)→(259.78,1451.71) |
| Base Game Mode Container 1x2 Raid or Army Release | 5 | 6.81 | `Event Title` (-253.92,1402.82)→(259.78,1458.53) ⇒ (-253.92,1396.00)→(259.78,1451.71) |
| Collection Menu Variant | 7 | 0.00 | `Create` (1180.00,80.94)→(1425.00,140.94) ⇒ (1179.99,80.94)→(1424.99,140.94) |
| Deck Selection Popup with Tabs | 12 | 36.83 | `Generic Round Button Variant` (479.71,137.45)→(539.71,197.45) ⇒ (442.87,137.45)→(502.87,197.45) |
| Deck info Popup | 15 | 347.95 | `Switch Deck Info Button` (1611.69,130.20)→(1686.08,205.80) ⇒ (1263.74,130.20)→(1338.13,205.80) |
| Draft Game Mode Container 1x2 | 1 | 25.08 | `Event Title` (-253.92,1421.08)→(259.78,1476.79) ⇒ (-253.92,1396.00)→(259.78,1451.71) |
| Draft Mode Deck Info Panel | 20 | 10.69 | `Alliance Score Bar Line Level 1` (1550.54,427.71)→(1846.16,488.19) ⇒ (1550.54,417.02)→(1846.16,477.50) |
| Draft Mode Menu Demo | 33 | 40.36 | `icon` (1178.91,718.00)→(1238.91,758.00) ⇒ (1138.54,718.00)→(1198.54,758.00) |
| Draft Mode Timed Mode Window | 53 | 40.36 | `icon` (1168.91,715.50)→(1228.91,755.50) ⇒ (1128.54,715.50)→(1188.54,755.50) |
| EnergySinglePlayerOnlyEventWindow | 25 | 10.69 | `Score Bar Line Level 1` (1470.09,414.94)→(1765.71,475.42) ⇒ (1470.09,404.25)→(1765.71,464.73) |
| Game mode deck selector | 1 | 25.08 | `Event Title` (6.48,1398.02)→(499.53,1453.73) ⇒ (6.48,1372.94)→(499.53,1428.65) |
| Legendary Display Profile | 4 | 1.07 | `Rating Text` (439.48,337.14)→(770.15,373.44) ⇒ (438.41,337.14)→(769.08,373.44) |
| Main Menu Offer Container 1x1 | 5 | 6.81 | `Event Title` (706.08,689.31)→(1219.78,745.01) ⇒ (706.08,682.49)→(1219.78,738.20) |
| Main Menu Offer Container 1x1 Premium_booster_title_avatarOrResource | 5 | 6.81 | `Event Title` (706.08,689.31)→(1219.78,745.01) ⇒ (706.08,682.49)→(1219.78,738.20) |
| Main Menu Offer Container Carousel 1x1 | 5 | 6.81 | `Event Title` (706.08,689.31)→(1219.78,745.01) ⇒ (706.08,682.49)→(1219.78,738.20) |
| Main Menu Offer Container Static Image 1x1 | 5 | 6.81 | `Event Title` (706.08,689.31)→(1219.78,745.01) ⇒ (706.08,682.49)→(1219.78,738.20) |
| Main Menu Offer Container Static Image 1x2 | 5 | 6.81 | `Event Title` (-253.92,1402.82)→(259.78,1458.53) ⇒ (-253.92,1396.00)→(259.78,1451.71) |
| Main Menu Settings Window | 2 | 322.00 | `Register Button` (1213.25,545.27)→(1513.25,635.27) ⇒ (891.25,545.27)→(1191.25,635.27) |
| MessagePopupWindowDuel | 6 | 37.70 | `Button Skirmish` (610.00,567.00)→(960.00,643.00) ⇒ (572.30,567.00)→(922.30,643.00) |
| Player Profile Window | 8 | 1.07 | `Rating Text` (790.50,601.25)→(1121.17,637.54) ⇒ (789.43,601.25)→(1120.10,637.54) |
| Ranked Game Mode Container 1x2 | 5 | 6.81 | `Event Title` (-253.92,1402.82)→(259.78,1458.53) ⇒ (-253.92,1396.00)→(259.78,1451.71) |
| ReRollPopup Variant | 8 | 37.70 | `ButtonLeft` (610.00,541.00)→(960.00,617.00) ⇒ (572.30,541.00)→(922.30,617.00) |
| Reward Event Container 1x1 | 5 | 6.81 | `Event Title` (706.08,689.31)→(1219.78,745.01) ⇒ (706.08,682.49)→(1219.78,738.20) |
| Skirmish Ranked Game Mode Container 1x2 | 5 | 6.81 | `Event Title` (-253.92,1402.82)→(259.78,1458.53) ⇒ (-253.92,1396.00)→(259.78,1451.71) |
| SkirmishModeEventWindow | 25 | 10.69 | `Score Bar Line Level 1` (151.19,465.72)→(446.81,526.20) ⇒ (151.19,455.03)→(446.81,515.51) |
| Two Sides Event Window | 25 | 10.69 | `Score Bar Line Level 1` (1503.19,452.72)→(1798.81,513.20) ⇒ (1503.19,442.03)→(1798.81,502.51) |

</details>

---

## 报告二：`m_ChildScaleWidth/Height` 偏移的受影响清单

**背景**：2026-10-04 的改动（`_axis_scale`）让 **61 个窗口 / 274 个节点**的输出变了（A59 记的是「62 个窗口」，本轮按「根窗口 × 深度 12 × 逐节点比矩形」实测 **61**；差的 1 个可能是根窗口去重或深度口径，**未逐一对账**）。
**复现方式**：把 `_axis_scale` 退回恒 `1.0` = 2026-10-04 之前的行为，同一个 Bundle 重走一遍，逐节点比 `rect`。

### 代表性「旧值 → 新值」

| 窗口 | 变的节点 | 最大位移 |
|---|---|---|
| `Missions Tab` | **91** | **99.07px** |
| `Rewards Base Submenu Variant` | **91** | **99.07px** |
| `Expansion Pass Reward Container` | 3 | 26.07px |
| `Raid Reward Container` | 6 | 4.80px |
| `General Basic Offer *` 家族（34 窗） | 各 1–2 | 3.62–5.19px |
| `Webshop Offer Container` | 2 | 4.46px |
| `Booster Info Popup` | 2 | 5.19px |

**`Missions Tab` 这一条最值得看**（它就是 A60 ④ 那个 `Normal Missions`）：`Special Missions` / `Daily Missions` 两件自带 `localScale = 1.15`，
旧值宽 **759.50** → 新值宽 **660.43**（= 真 `sizeDelta`），其下 91 个后代整体左移 99.07px。
🔴 **旧值 759.50 是「错得看起来对」的典型**：老算式以为组里有 198px 余量、按 flexible 分给两件各 +99.07，
**碰巧**等于 `660.43 × 1.15`（= 视觉宽）—— 所以 `资料/已知的坑.md`（A23）当年抄下来的「各算 759.5 宽」**看着像真值**。
新值 660.43 才是**布局框**，它的**视觉框**仍然是 759.50（本工具新加的那一列现在会同时印出来：`布局 660.43 ×1.15 ⇒ 视觉 759.50`）。
⇒ A60 ④ 那句「数字要改成『sizeDelta 各 660.43、视觉 759.5』」**本轮实测成立**。

### 我们的 C# 里「疑似抄了旧值」的清单（只列不改）

搜法：把每个变了的**旧值**（±0.02，因为 dump 是 2 位四舍五入、C# 常量来自另一轮 dump）拿去
**「含这个窗口特征节点名的那些 `.cs`」**里搜数字字面量（全仓 `CardPresentation/` 174 个 `.cs` / 10.3 万行）。

| 窗口 | 节点 | 旧值 | 新值 | 我们 C# 里疑似抄了它吗（文件:行号） |
|---|---|---|---|---|
| **`Booster Info Popup`** | `WebShop Button>Button Text` | (1291.23,725.86)→(1423.23,777.74) | **(1296.42,725.86)→(1428.42,777.74)** | ★ **`Shell/BoosterInfoPopup.cs:120 WebTextR = (1291.22, 725.90, 1423.22, 777.75)`** —— 左/右**都是旧值**（差 0.01 是四舍五入）⇒ **要改**。同文件 `:119 WebIconR` 的右边缘 `1291.22` = **旧文字左边缘**；icon 自己的**布局框没动**（它是 `scl=1.2` 那一件，`--md` 现在印 `布局 51.88×51.88 ×1.2 ⇒ 视觉 62.26×62.26`）⇒ icon 的**视觉**右边缘 = 1296.41 ≈ 新文字左边缘 **1296.42** ✅ 自洽 |
| **`Player Profile Window`** | `Price Display>text` | 938.55 → 1030.77 | **943.70 → 1035.92** | ★ **`Shell/ProfileTab.cs:297 PdTxL = 938.55f, PdTxR = 1030.77f`**（`:704-707` 用它建 `text`）⇒ **要改**。同一行的 `PdIcL/PdIcR = 887.08/938.55` **不变**（icon 布局框没动） |
| **`Social Submenu Variant`** | `Create Alliance View>…>Price Display>text` | 542.06 → 609.12 | **546.75 → 613.81** | ★ **`Shell/AlliancesTab.cs:570 new PxRect(542.06f, 730.73f, 609.12f, 777.65f)`**（`"1000"` 那个 `text`）⇒ **要改**。`:569` 的 `icon (495.15..542.06)` **不变** |
| **`ReRollPopup Variant`** | `icon` / `text` | 1141.25 / 1202.75 | 1146.85 / 1213.95 | ★ **已记 A59**：`Shell/MissionRerollPopup.cs:177 IconR`（视觉框，已按 1.2 修）· `:185 PriceTextR = (1202.75,…)`（**还是旧值**）· 注释 `:69/:71/:137/:166/:175/:178`；`Editor/RewardsScene.cs:3107 CheckAt(cellN, 1141.25f, 1141.25f, …)` · `:3131 CheckNear(TextLeftPx(priceTxN), 1202.75f, 2f)` · `:3113/:3115/:3119` 注释。⚠️ **连带报告一**：这几个数在 W2/W3 下会**再变一次** |
| `Missions Tab` / `Rewards Base Submenu Variant` | `Special/Daily Missions` 及 91 个后代 | 宽 759.50 · x2 1891.37 等 | 宽 660.43 · x2 1792.31 等 | ⚠️ **不受这条影响**：`Editor/RewardsScene.cs:550-553` 的 `CheckAt` 用的是**模板值**——本轮实测 `--no-layout` 也印 `372.4 / 1151.6 / 1271.3 / 1810.5 / 1891.4`，**与 C# 逐位吻合** ⇒ 它们是「布局跑之前」的值，`_axis_scale` 动不到。**A60 ④ 的开放问题（运行期该显示哪一套）依然开着** |
| `General Basic Offer *` 家族（34 窗） | `Price Display>text` | 局部坐标，如 −71.31→20.91 | −67.05→25.17 | ⚠️ **判为「没用那些值」**：`Shell/OfferContainer.cs:184-186` 自己写着「**`Price Display` 那一段的排版是我们挑的**……`text` 的量出来的宽是 0（布局没跑）⇒ 我们只建空节点 + 一个 `text`，按**按钮整框居中**画，**不摆 HLG**」⇒ 这 34 个窗口的 dump 值**没有被抄进 C#** |
| 其余 ~56 个窗口 | — | — | — | ⚠️ **本轮没找到命中**（搜法见上）。**这 ≠ 没有**：只搜了「旧值 ±0.02 且同文件含本窗口特征名」的数字字面量，**没搜**：手工换算过的值、`RectTransform` 资产里的值、`.unity` 场景里存的值、以及不是照 dump 抄的推导值 |

<details><summary>报告二 · 逐窗口汇总（完整 61 行）</summary>

| 窗口 | 变的节点 | 最大位移 px | 位移最大那一处（旧 ⇒ 新） |
|---|---|---|---|
| Booster Info Popup | 2 | 5.19 | `Button Text` (1291.23,725.86)→(1423.23,777.74) ⇒ (1296.42,725.86)→(1428.42,777.74) |
| Booster Offer Container | 1 | 4.26 | `text` (-71.31,1223.42)→(20.91,1263.00) ⇒ (-67.05,1223.42)→(25.17,1263.00) |
| Card Shop Tab | 2 | 6.61 | `text` (195.17,1080.62)→(287.39,1080.82) ⇒ (201.78,1080.62)→(294.00,1080.82) |
| Card Shop VIP Tab Variant | 2 | 6.61 | `text` (195.17,1080.62)→(319.67,1080.82) ⇒ (201.78,1080.62)→(326.28,1080.82) |
| Catalog Item Shop Container | 1 | 2.17 | `text` (10.40,1263.55)→(10.40,1282.84) ⇒ (12.57,1263.55)→(12.57,1282.84) |
| Cosmetic Item Shop Container | 1 | 2.17 | `text` (10.40,1263.55)→(10.40,1282.84) ⇒ (12.57,1263.55)→(12.57,1282.84) |
| Daily Shop Tab | 2 | 6.61 | `text` (195.17,1080.62)→(287.39,1080.82) ⇒ (201.78,1080.62)→(294.00,1080.82) |
| Draft Mode Menu Demo | 1 | 5.73 | `text` (1173.15,885.79)→(1263.68,933.02) ⇒ (1178.88,885.79)→(1269.41,933.02) |
| Draft Mode Timed Mode Window | 1 | 5.73 | `text` (1173.15,885.79)→(1263.68,933.02) ⇒ (1178.88,885.79)→(1269.41,933.02) |
| Expansion Pass Progress Bar | 2 | 26.07 | `text` (825.82,822.47)→(934.81,859.71) ⇒ (851.89,822.47)→(960.88,859.71) |
| Expansion Pass Reward Container | 3 | 26.07 | `text` (-150.46,1311.88)→(-41.47,1349.12) ⇒ (-124.40,1311.88)→(-15.41,1349.12) |
| Gacha Tab | 1 | 8.05 | `text` (638.62,807.71)→(638.62,891.92) ⇒ (646.68,807.71)→(646.68,891.92) |
| General Basic Offer Container Booster_CardOrAltArt | 1 | 4.92 | `text` (-29.08,1482.99)→(-29.08,1530.86) ⇒ (-24.16,1482.99)→(-24.16,1530.86) |
| General Basic Offer Container Booster_CardOrAltArt_Cardback_Avatar_Title | 1 | 4.92 | `text` (-91.33,1482.99)→(33.17,1530.86) ⇒ (-86.41,1482.99)→(38.09,1530.86) |
| General Basic Offer Container Booster_CardOrAltArt__AvatarORTitle | 1 | 4.92 | `text` (-91.33,1482.99)→(33.17,1530.86) ⇒ (-86.41,1482.99)→(38.09,1530.86) |
| General Basic Offer Container Variant 2 Currencies | 1 | 4.26 | `text` (-71.31,1417.26)→(20.91,1457.34) ⇒ (-67.05,1417.26)→(25.17,1457.34) |
| General Basic Offer Container Variant Booster + 2 Currencies | 1 | 4.26 | `text` (-60.51,1405.56)→(31.71,1445.64) ⇒ (-56.25,1405.56)→(35.97,1445.64) |
| General Basic Offer Container Variant Booster_avatar_cardback_title | 1 | 3.62 | `text` (-28.39,1417.26)→(-28.39,1457.34) ⇒ (-24.77,1417.26)→(-24.77,1457.34) |
| General Basic Offer Container Variant Booster_avatar_resource | 1 | 4.26 | `text` (-25.20,1417.26)→(-25.20,1457.34) ⇒ (-20.94,1417.26)→(-20.94,1457.34) |
| General Basic Offer Container Variant Booster_cardback_resource | 1 | 4.26 | `text` (-25.20,1417.26)→(-25.20,1457.34) ⇒ (-20.94,1417.26)→(-20.94,1457.34) |
| General Basic Offer Container Variant Booster_title_resource | 1 | 4.26 | `text` (-25.20,1417.26)→(-25.20,1457.34) ⇒ (-20.94,1417.26)→(-20.94,1457.34) |
| General Basic Offer Container Variant Deck_cardback_avatar | 1 | 3.62 | `text` (-28.39,1417.26)→(-28.39,1457.34) ⇒ (-24.77,1417.26)→(-24.77,1457.34) |
| General Basic Offer Container Variant Premium_Booster_avatar_cardback_title | 1 | 3.62 | `text` (-28.39,1417.26)→(-28.39,1457.34) ⇒ (-24.77,1417.26)→(-24.77,1457.34) |
| General Basic Offer Container Variant Premium_Booster_avatar_cardback_title_resource | 1 | 4.26 | `text` (-71.31,1417.26)→(20.91,1457.34) ⇒ (-67.05,1417.26)→(25.17,1457.34) |
| General Basic Offer Container Variant Premium_Premium_cardback_avatar | 1 | 4.26 | `text` (-25.20,1417.26)→(-25.20,1457.34) ⇒ (-20.94,1417.26)→(-20.94,1457.34) |
| General Basic Offer Container Variant Premium_Resource | 1 | 4.26 | `text` (-25.20,1417.26)→(-25.20,1457.34) ⇒ (-20.94,1417.26)→(-20.94,1457.34) |
| General Basic Offer Container Variant Premium_booster_title_avatarOrResource | 1 | 4.26 | `text` (-25.20,1417.26)→(-25.20,1457.34) ⇒ (-20.94,1417.26)→(-20.94,1457.34) |
| General Basic Offer Container Variant Single Item Type | 1 | 4.26 | `text` (-87.45,1417.26)→(37.05,1457.34) ⇒ (-83.19,1417.26)→(41.31,1457.34) |
| General Basic Offer Container Variant avatarOrTitle_resource | 1 | 4.26 | `text` (-25.20,1417.26)→(-25.20,1457.34) ⇒ (-20.94,1417.26)→(-20.94,1457.34) |
| General Basic Offer Container Variant cardback_premiumOrAvatarOrResource_titleOrResource | 1 | 3.62 | `text` (-28.39,1417.26)→(-28.39,1457.34) ⇒ (-24.77,1417.26)→(-24.77,1457.34) |
| General Basic Offer Popup Booster_CardOrAltArt | 2 | 5.19 | `Button Text` (1365.48,689.26)→(1365.48,741.14) ⇒ (1370.67,689.26)→(1370.67,741.14) |
| General Basic Offer Popup Booster_CardOrAltArt_AvatarORTitle | 2 | 5.19 | `Button Text` (1365.48,689.26)→(1365.48,741.14) ⇒ (1370.67,689.26)→(1370.67,741.14) |
| General Basic Offer Popup Booster_CardOrAltArt_Cardback_Avatar_Title | 2 | 5.19 | `Button Text` (1365.48,689.26)→(1365.48,741.14) ⇒ (1370.67,689.26)→(1370.67,741.14) |
| General Basic Offer Popup Just Foreground | 2 | 5.19 | `Button Text` (1365.48,689.26)→(1365.48,741.14) ⇒ (1370.67,689.26)→(1370.67,741.14) |
| General Basic Offer Popup Variant 2 Currencies | 2 | 5.19 | `Button Text` (1443.48,689.26)→(1575.48,741.14) ⇒ (1448.67,689.26)→(1580.67,741.14) |
| General Basic Offer Popup Variant Booster + 2 Currencies | 2 | 5.19 | `Button Text` (1443.48,689.26)→(1575.48,741.14) ⇒ (1448.67,689.26)→(1580.67,741.14) |
| General Basic Offer Popup Variant Booster_avatar_resource | 2 | 5.19 | `Button Text` (1299.48,689.26)→(1431.48,741.14) ⇒ (1304.67,689.26)→(1436.67,741.14) |
| General Basic Offer Popup Variant Booster_cardback_resource | 2 | 5.19 | `Button Text` (1299.48,689.26)→(1431.48,741.14) ⇒ (1304.67,689.26)→(1436.67,741.14) |
| General Basic Offer Popup Variant Booster_title_resource | 2 | 5.19 | `Button Text` (1365.48,689.26)→(1365.48,741.14) ⇒ (1370.67,689.26)→(1370.67,741.14) |
| General Basic Offer Popup Variant Expansion pass | 2 | 5.19 | `Button Text` (1365.48,689.26)→(1365.48,741.14) ⇒ (1370.67,689.26)→(1370.67,741.14) |
| General Basic Offer Popup Variant Premium_Booster_avatar_cardback_title | 2 | 5.19 | `Button Text` (1142.31,689.26)→(1142.31,741.14) ⇒ (1147.50,689.26)→(1147.50,741.14) |
| General Basic Offer Popup Variant Premium_Booster_avatar_cardback_title_resource | 2 | 5.19 | `Button Text` (1444.48,691.43)→(1576.48,743.31) ⇒ (1449.67,691.43)→(1581.67,743.31) |
| General Basic Offer Popup Variant Premium_Premium_cardback_avatar | 2 | 5.19 | `Button Text` (1299.48,689.26)→(1431.48,741.14) ⇒ (1304.67,689.26)→(1436.67,741.14) |
| General Basic Offer Popup Variant Premium_Resource | 2 | 5.19 | `Button Text` (1509.48,689.26)→(1509.48,741.14) ⇒ (1514.67,689.26)→(1514.67,741.14) |
| General Basic Offer Popup Variant Single Item Type | 2 | 5.19 | `Button Text` (1365.48,689.26)→(1365.48,741.14) ⇒ (1370.67,689.26)→(1370.67,741.14) |
| General Basic Offer Popup Variant avatarOrTitle_resource | 2 | 5.19 | `Button Text` (1365.48,689.26)→(1365.48,741.14) ⇒ (1370.67,689.26)→(1370.67,741.14) |
| Item Shop Tab | 2 | 6.61 | `text` (195.17,1080.62)→(287.39,1080.82) ⇒ (201.78,1080.62)→(294.00,1080.82) |
| Item Shop Tab No Automatic Ordering | 2 | 6.61 | `text` (195.17,1080.62)→(287.39,1080.82) ⇒ (201.78,1080.62)→(294.00,1080.82) |
| Missions Tab | 91 | 99.07 | `Special Missions` (205.20,24.91)→(964.70,581.13) ⇒ (205.20,24.91)→(865.63,581.13) |
| Offer Container | 1 | 4.26 | `text` (-71.31,1417.26)→(20.91,1457.34) ⇒ (-67.05,1417.26)→(25.17,1457.34) |
| Player Profile Window | 1 | 5.15 | `text` (938.55,586.77)→(1030.77,638.24) ⇒ (943.70,586.77)→(1035.92,638.24) |
| Price Display Button | 1 | 3.81 | `text` (-15.57,1061.17)→(-15.57,1099.58) ⇒ (-11.76,1061.17)→(-11.76,1099.58) |
| Raid Progress Bar | 2 | 7.45 | `text` (825.82,822.47)→(934.81,859.71) ⇒ (833.27,822.47)→(942.26,859.71) |
| Raid Reward Container | 6 | 7.45 | `text` (-149.04,702.05)→(-40.05,739.29) ⇒ (-141.59,702.05)→(-32.60,739.29) |
| ReRollPopup Variant | 2 | 11.20 | `text` (1202.75,551.00)→(1202.75,607.00) ⇒ (1213.95,551.00)→(1213.95,607.00) |
| Referral Container | 1 | 3.71 | `text` (-27.90,1223.42)→(-27.90,1263.00) ⇒ (-24.19,1223.42)→(-24.19,1263.00) |
| Rewards Base Submenu Variant | 91 | 99.07 | `Special Missions` (372.37,95.85)→(1131.87,652.07) ⇒ (372.37,95.85)→(1032.81,652.07) |
| Shop Item Container | 1 | 4.92 | `text` (-67.99,1223.42)→(24.23,1263.00) ⇒ (-63.07,1223.42)→(29.15,1263.00) |
| Small General Basic Offer Container Variant Single Item Type | 1 | 4.26 | `text` (-25.20,1223.79)→(-25.20,1263.47) ⇒ (-20.94,1223.79)→(-20.94,1263.47) |
| Social Submenu Variant | 1 | 4.69 | `text` (542.06,730.73)→(609.12,777.65) ⇒ (546.75,730.73)→(613.81,777.65) |
| Webshop Offer Container | 2 | 8.51 | `text` (-124.45,1468.76)→(-32.23,1471.24) ⇒ (-115.94,1468.76)→(-23.72,1471.24) |

</details>

---

## 没查清 / 没做的部分

1. **A59 说「62 个窗口」，本轮实测 61** —— 没逐一对账差在哪（可能是根窗口去重口径、或深度 12 之外还有更深的组）。**别把 61 当成权威**，要权威数就按同一脚本重跑（`附录 B` 的 274 行是逐节点的，可自查）。
2. **`flexible` 那条我一行代码都没改**（按简报要求）—— 上面 W2/W3 的数字是用**内存补丁**（`inspect.getsource` 取函数、文本替换后 `exec`）跑出来的，`d:/4/Unity/工具/menu_dump.py` **没有被这两条改动碰到**。复现脚本在 `d:/4/_tmp_view/md1005/`（`diff_variants.py` / `run_w3.py` / `verify_under_w2.py`）。
3. **`LayoutElement.m_FlexibleWidth/Height` 在 `ctrl=1` 分支被忽略**（顺手发现 3）—— 只**读到**了，**没量化**它影响多少窗口。
4. **`rect_of` 的 `scale` 形参是死参**（顺手发现 4）—— 本轮**没动**它（动了会改所有既有输出，属「大面积改输出」那一类，按简报该由调度台定）。
5. **`--md` 的 `|` 转义只做了「输出侧」**：`describe()` 里 `extra` 仍然用 `' | '` 分隔 `m_SpriteState`（文本模式读起来是故意的）。**没统一**。
6. 报告二里「C# 是否抄了旧值」是**数值反查**，不是逐文件读代码 —— 上表每一行都给了 `文件:行号` 供人工复核；**`OfferContainer` 那 34 个窗口只读了它的头注释**，没逐行核。

---

## 顺手发现（不许自己改，按简报只报）

1. 🔴 **`--md` 表里带 `|` 的行会整行多出一列**（**改前就存在**）：`Alliance Trophy Info Popup · Generic Close Button Orange` 那一行原来 **16 列**（表头 14/15），
   原因是 `describe()` 给 `m_SpriteState` 加的 `HL=… P=… | HL=… P=…`（2026-10-03 A17 加的）里的 `|` 没转义。
   **列错了比缺列更坏**（抄的人会从错的列里取值，而且那行看着「有内容、不像坏的」）⇒ **我顺手修了**（`_md()`，转义成 `\|`），改后该窗 30 行**全是 15 列**。
   判据：`CLAUDE.md` 数竖线那条也认 `\|` 这个转义。**这是本轮唯一一处「简报没点名、但我改了」的地方**，觉得越界可以回退这一条（`_md()` + md 行里那 5 处调用）。
2. 🔴 **`walk()` 还有一处静默跳过**：子件是**纯 `Transform`**（3D，例：卡片的 3D 体，`bundle_menus_assets_all` 里 **258 个** `Transform/` 资产）时它没有 `RectTransform` ⇒ 原来一句话不说就从表里消失。
   实测有这种子件的窗口 **~33 个**（`Booster Pack Open Window` 32 个最多）。**已顺手出声**（「N 个纯 Transform 子件不在本表里」），并**顺带把「真缺件」分开报**（真的在 `RectTransform/` 与 `Transform/` 里**都查不到**的那种）—— 本轮实测真缺件 **0**。
3. ⚠️ **`_child_sizes` 的 `ctrl=1` 分支漏了 `LayoutUtility.GetFlexibleSize`**（uGUI 同一处 `:106-112` 的上面几行）：
   本文件在 `ctrl=1` 时返回 `1.0 if fexp else 0.0`，而 uGUI 是 `flexible = GetFlexibleSize(child, axis)`（= `LayoutElement.m_FlexibleWidth/Height`，读不到按 `-1 → 0`）**再** `Max(., 1)`。
   ⇒ `fexp=0` 时 `LayoutElement` 声明的 flexible **被丢掉**。实例：`Price Display>icon` 挂着 `LayoutElement`（字段列里有 `m_FlexibleWidth`），**值没读**。**没量化**（见「没查清 3」）。
4. ⚠️ **`menu_rect.rect_of(rt, parent_rect, scale)` 的 `scale` 形参是死参**（函数体里一次都没用），`walk()` 里一路乘下去的那个 `scale` 因此**从不生效**：
   带 `localScale` 的**容器**（实测有：`Container Drawer>Content scl=(0,0)` 带 7 个子件、`Card Shop VIP Tab Variant>CardUI scl=(0,0)` 带 5 个，全库共 **780 处**「有子件且 scl≠1」）**其后代印出来的绝对矩形都在「未缩放」坐标系里**。
   ⇒ 与本次修的第①条**同一族**（布局框 vs 视觉框），但**更隐蔽**：不只是那一件自己，是**整棵子树**的坐标。
   **本轮没改**（会改大面积输出）。**建议单独排一件**：要么真按 uGUI 把 `scale` 乘进子件矩形，要么在表里显式警告「本行的绝对矩形未含祖先缩放」。
5. ℹ️ **A60 ④ / A23 里的行号已经漂了**：A60 写「`RewardsScene.cs:507-508` 把模板值断言死」，本轮实测在 **`:550-553`**（`CheckAt(nm, 372.37f, 1891.37f, 95.85f, 819.65f, "Normal Missions")` 那一串）。
   我在不同时间两次读到 **530 / 550** 两个行号 ⇒ **该文件此刻可能正被别的写手改动**，引用时**按内容找**、别按行号。
6. ℹ️ **`--verify-layout` 的 fixture ③ 现在同时被两件事钉着**：它既验 `m_ChildScale`（它的本职），又**隐式**钉住了外层 `Buttons` 组的 `flexible` 行为（因为它的期望值是**绝对**坐标）。
   ⇒ 落 `flexible` 时它会红，**红得对**，但要改的是**期望值**不是一个 bug。建议落那条时**把它拆成两条**：一条钉内层相对几何（不受外层影响）、一条钉绝对位置。
7. ℹ️ `--verify-layout` 三条 fixture 全绿**并不覆盖** `flexible`（`ctrl=0&fexp=1`）这条路径：本轮实测 W2/W3 下 ①② 全绿、只有 ③ 红 —— 与文件里已有的三处「自检绿 ≠ 这条对」同族，**第四次**。

---

---

## 附录 A：flexible=1 的**全量**影响清单（W1 现状 → W2 提议）

### A.1 逐节点「旧值 → 新值」

复现：`_child_sizes` 的 `not ctrl` 分支返回 `1.0 if fexp else 0.0`（只改这一处）。

| 窗口 | # | 缩进 | 节点 | 局部缩放 | 旧 x1 | 旧 y1 | 旧 x2 | 旧 y2 | 新 x1 | 新 y1 | 新 x2 | 新 y2 | 最大位移 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Alliance Event Score Info | 6 | 2 | Alliance Score Bar Line Level 1 | 1 | 1364.11 | 462.88 | 1659.73 | 523.36 | 1364.11 | 452.19 | 1659.73 | 512.67 | 10.69 |
| Alliance Event Score Info | 7 | 3 | Chest | 0.8696 | 1536.60 | 452.52 | 1627.64 | 533.72 | 1536.60 | 441.83 | 1627.64 | 523.03 | 10.69 |
| Alliance Event Score Info | 8 | 3 | Skull | 0.8696 | 1436.23 | 467.00 | 1494.81 | 519.25 | 1436.23 | 456.31 | 1494.81 | 508.55 | 10.69 |
| Alliance Event Score Info | 9 | 4 | Score | 1 | 1313.77 | 468.12 | 1429.92 | 518.12 | 1313.77 | 457.43 | 1429.92 | 507.43 | 10.69 |
| Alliance Event Score Info | 10 | 2 | Alliance Score Bar Line Level 2 | 1 | 1364.11 | 523.36 | 1659.73 | 583.85 | 1364.11 | 512.67 | 1659.73 | 573.15 | 10.69 |
| Alliance Event Score Info | 11 | 3 | Chest | 0.8696 | 1536.60 | 513.01 | 1627.64 | 594.20 | 1536.60 | 502.31 | 1627.64 | 583.51 | 10.69 |
| Alliance Event Score Info | 12 | 3 | Skull | 0.8696 | 1436.23 | 527.48 | 1494.81 | 579.73 | 1436.23 | 516.79 | 1494.81 | 569.04 | 10.69 |
| Alliance Event Score Info | 13 | 4 | Score | 1 | 1313.77 | 528.60 | 1429.92 | 578.60 | 1313.77 | 517.91 | 1429.92 | 567.91 | 10.69 |
| Alliance Event Score Info | 14 | 2 | Alliance Score Bar Line Level 3 | 1 | 1364.11 | 583.85 | 1659.73 | 644.33 | 1364.11 | 573.15 | 1659.73 | 633.64 | 10.69 |
| Alliance Event Score Info | 15 | 3 | Chest | 0.8696 | 1536.60 | 573.49 | 1627.64 | 654.68 | 1536.60 | 562.80 | 1627.64 | 643.99 | 10.69 |
| Alliance Event Score Info | 16 | 3 | Skull | 0.8696 | 1436.23 | 587.96 | 1494.81 | 640.21 | 1436.23 | 577.27 | 1494.81 | 629.52 | 10.69 |
| Alliance Event Score Info | 17 | 4 | Score | 1 | 1313.77 | 589.09 | 1429.92 | 639.09 | 1313.77 | 578.39 | 1429.92 | 628.39 | 10.69 |
| Alliance Event Score Info | 18 | 2 | Alliance Score Bar Line Level 4 | 1 | 1364.11 | 644.33 | 1659.73 | 704.81 | 1364.11 | 633.64 | 1659.73 | 694.12 | 10.69 |
| Alliance Event Score Info | 19 | 3 | Chest | 0.8696 | 1536.60 | 633.97 | 1627.64 | 715.17 | 1536.60 | 623.28 | 1627.64 | 704.47 | 10.69 |
| Alliance Event Score Info | 20 | 3 | Skull | 0.8696 | 1436.23 | 648.44 | 1494.81 | 700.69 | 1436.23 | 637.75 | 1494.81 | 690.00 | 10.69 |
| Alliance Event Score Info | 21 | 4 | Score | 1 | 1313.77 | 649.57 | 1429.92 | 699.57 | 1313.77 | 638.88 | 1429.92 | 688.88 | 10.69 |
| Alliance Event Score Info | 22 | 2 | Alliance Score Bar Line Level 5 | 1 | 1364.11 | 704.81 | 1659.73 | 765.29 | 1364.11 | 694.12 | 1659.73 | 754.60 | 10.69 |
| Alliance Event Score Info | 23 | 3 | Chest | 0.8696 | 1536.60 | 694.45 | 1627.64 | 775.65 | 1536.60 | 683.76 | 1627.64 | 764.96 | 10.69 |
| Alliance Event Score Info | 24 | 3 | Skull | 0.8696 | 1436.23 | 708.93 | 1494.81 | 761.17 | 1436.23 | 698.23 | 1494.81 | 750.48 | 10.69 |
| Alliance Event Score Info | 25 | 4 | Score | 1 | 1313.77 | 710.05 | 1429.92 | 760.05 | 1313.77 | 699.36 | 1429.92 | 749.36 | 10.69 |
| Alliance Event Score Panel | 11 | 4 | Alliance Score Bar Line Level 1 | 1 | 1363.19 | 449.76 | 1658.81 | 510.24 | 1363.19 | 439.06 | 1658.81 | 499.55 | 10.69 |
| Alliance Event Score Panel | 12 | 5 | Chest | 0.8696 | 1535.68 | 439.40 | 1626.72 | 520.59 | 1535.68 | 428.71 | 1626.72 | 509.90 | 10.69 |
| Alliance Event Score Panel | 13 | 5 | Skull | 0.8696 | 1435.31 | 453.87 | 1493.89 | 506.12 | 1435.31 | 443.18 | 1493.89 | 495.43 | 10.69 |
| Alliance Event Score Panel | 14 | 6 | Score | 1 | 1312.85 | 455.00 | 1429.00 | 505.00 | 1312.85 | 444.31 | 1429.00 | 494.31 | 10.69 |
| Alliance Event Score Panel | 15 | 4 | Alliance Score Bar Line Level 2 | 1 | 1363.19 | 510.24 | 1658.81 | 570.72 | 1363.19 | 499.55 | 1658.81 | 560.03 | 10.69 |
| Alliance Event Score Panel | 16 | 5 | Chest | 0.8696 | 1535.68 | 499.88 | 1626.72 | 581.08 | 1535.68 | 489.19 | 1626.72 | 570.39 | 10.69 |
| Alliance Event Score Panel | 17 | 5 | Skull | 0.8696 | 1435.31 | 514.35 | 1493.89 | 566.60 | 1435.31 | 503.66 | 1493.89 | 555.91 | 10.69 |
| Alliance Event Score Panel | 18 | 6 | Score | 1 | 1312.85 | 515.48 | 1429.00 | 565.48 | 1312.85 | 504.79 | 1429.00 | 554.79 | 10.69 |
| Alliance Event Score Panel | 19 | 4 | Alliance Score Bar Line Level 3 | 1 | 1363.19 | 570.72 | 1658.81 | 631.20 | 1363.19 | 560.03 | 1658.81 | 620.51 | 10.69 |
| Alliance Event Score Panel | 20 | 5 | Chest | 0.8696 | 1535.68 | 560.36 | 1626.72 | 641.56 | 1535.68 | 549.67 | 1626.72 | 630.87 | 10.69 |
| Alliance Event Score Panel | 21 | 5 | Skull | 0.8696 | 1435.31 | 574.84 | 1493.89 | 627.08 | 1435.31 | 564.15 | 1493.89 | 616.39 | 10.69 |
| Alliance Event Score Panel | 22 | 6 | Score | 1 | 1312.85 | 575.96 | 1429.00 | 625.96 | 1312.85 | 565.27 | 1429.00 | 615.27 | 10.69 |
| Alliance Event Score Panel | 23 | 4 | Alliance Score Bar Line Level 4 | 1 | 1363.19 | 631.20 | 1658.81 | 691.68 | 1363.19 | 620.51 | 1658.81 | 680.99 | 10.69 |
| Alliance Event Score Panel | 24 | 5 | Chest | 0.8696 | 1535.68 | 620.84 | 1626.72 | 702.04 | 1535.68 | 610.15 | 1626.72 | 691.35 | 10.69 |
| Alliance Event Score Panel | 25 | 5 | Skull | 0.8696 | 1435.31 | 635.32 | 1493.89 | 687.57 | 1435.31 | 624.63 | 1493.89 | 676.88 | 10.69 |
| Alliance Event Score Panel | 26 | 6 | Score | 1 | 1312.85 | 636.44 | 1429.00 | 686.44 | 1312.85 | 625.75 | 1429.00 | 675.75 | 10.69 |
| Alliance Event Score Panel | 27 | 4 | Alliance Score Bar Line Level 5 | 1 | 1363.19 | 691.68 | 1658.81 | 752.17 | 1363.19 | 680.99 | 1658.81 | 741.47 | 10.69 |
| Alliance Event Score Panel | 28 | 5 | Chest | 0.8696 | 1535.68 | 681.33 | 1626.72 | 762.52 | 1535.68 | 670.64 | 1626.72 | 751.83 | 10.69 |
| Alliance Event Score Panel | 29 | 5 | Skull | 0.8696 | 1435.31 | 695.80 | 1493.89 | 748.05 | 1435.31 | 685.11 | 1493.89 | 737.36 | 10.69 |
| Alliance Event Score Panel | 30 | 6 | Score | 1 | 1312.85 | 696.92 | 1429.00 | 746.92 | 1312.85 | 686.23 | 1429.00 | 736.23 | 10.69 |
| Base Game Mode Container 1x1 | 4 | 2 | Event Title | 1 | 706.08 | 689.31 | 1219.78 | 745.01 | 706.08 | 682.49 | 1219.78 | 738.20 | 6.81 |
| Base Game Mode Container 1x1 | 5 | 2 | Timer With Time Description | 1 | 706.08 | 740.81 | 1130.79 | 781.54 | 706.08 | 734.00 | 1130.79 | 774.73 | 6.81 |
| Base Game Mode Container 1x1 | 6 | 3 | Timer Description | 1 | 706.08 | 740.81 | 706.08 | 781.54 | 706.08 | 734.00 | 706.08 | 774.73 | 6.81 |
| Base Game Mode Container 1x1 | 7 | 4 | Clock Icon | 1 | 706.08 | 740.80 | 746.84 | 781.56 | 706.08 | 733.98 | 746.84 | 774.74 | 6.81 |
| Base Game Mode Container 1x1 | 8 | 5 | Timer Text | 1 | 746.84 | 738.42 | 945.83 | 783.94 | 746.84 | 731.60 | 945.83 | 777.12 | 6.81 |
| Base Game Mode Container 1x1 Claim | 4 | 2 | Event Title | 1 | 706.08 | 689.31 | 1219.78 | 745.01 | 706.08 | 682.49 | 1219.78 | 738.20 | 6.81 |
| Base Game Mode Container 1x1 Claim | 5 | 2 | Timer With Time Description | 1 | 706.08 | 740.81 | 1130.79 | 781.54 | 706.08 | 734.00 | 1130.79 | 774.73 | 6.81 |
| Base Game Mode Container 1x1 Claim | 6 | 3 | Timer Description | 1 | 706.08 | 740.81 | 706.08 | 781.54 | 706.08 | 734.00 | 706.08 | 774.73 | 6.81 |
| Base Game Mode Container 1x1 Claim | 7 | 4 | Clock Icon | 1 | 706.08 | 740.80 | 746.84 | 781.56 | 706.08 | 733.98 | 746.84 | 774.74 | 6.81 |
| Base Game Mode Container 1x1 Claim | 8 | 5 | Timer Text | 1 | 746.84 | 738.42 | 945.83 | 783.94 | 746.84 | 731.60 | 945.83 | 777.12 | 6.81 |
| Base Game Mode Container 1x1 Claim AutoCollect | 4 | 2 | Event Title | 1 | 706.08 | 689.31 | 1219.78 | 745.01 | 706.08 | 682.49 | 1219.78 | 738.20 | 6.81 |
| Base Game Mode Container 1x1 Claim AutoCollect | 5 | 2 | Timer With Time Description | 1 | 706.08 | 740.81 | 1130.79 | 781.54 | 706.08 | 734.00 | 1130.79 | 774.73 | 6.81 |
| Base Game Mode Container 1x1 Claim AutoCollect | 6 | 3 | Timer Description | 1 | 706.08 | 740.81 | 706.08 | 781.54 | 706.08 | 734.00 | 706.08 | 774.73 | 6.81 |
| Base Game Mode Container 1x1 Claim AutoCollect | 7 | 4 | Clock Icon | 1 | 706.08 | 740.80 | 746.84 | 781.56 | 706.08 | 733.98 | 746.84 | 774.74 | 6.81 |
| Base Game Mode Container 1x1 Claim AutoCollect | 8 | 5 | Timer Text | 1 | 746.84 | 738.42 | 945.83 | 783.94 | 746.84 | 731.60 | 945.83 | 777.12 | 6.81 |
| Base Game Mode Container 1x2 | 4 | 2 | Event Title | 1 | -253.92 | 1402.82 | 259.78 | 1458.53 | -253.92 | 1396.00 | 259.78 | 1451.71 | 6.81 |
| Base Game Mode Container 1x2 | 5 | 2 | Timer With Time Description | 1 | -253.92 | 1454.33 | 170.79 | 1495.06 | -253.92 | 1447.51 | 170.79 | 1488.24 | 6.81 |
| Base Game Mode Container 1x2 | 6 | 3 | Timer Description | 1 | -253.92 | 1454.33 | -253.92 | 1495.06 | -253.92 | 1447.51 | -253.92 | 1488.24 | 6.81 |
| Base Game Mode Container 1x2 | 7 | 4 | Clock Icon | 1 | -253.92 | 1454.31 | -213.16 | 1495.07 | -253.92 | 1447.50 | -213.16 | 1488.26 | 6.81 |
| Base Game Mode Container 1x2 | 8 | 5 | Timer Text | 1 | -213.16 | 1451.93 | -14.17 | 1497.45 | -213.16 | 1445.12 | -14.17 | 1490.64 | 6.81 |
| Base Game Mode Container 1x2 No Timer | 4 | 2 | Event Title | 1 | -253.92 | 1421.08 | 259.78 | 1476.79 | -253.92 | 1396.00 | 259.78 | 1451.71 | 25.08 |
| Base Game Mode Container 1x2 Raid or Army Release | 16 | 2 | Event Title | 1 | -253.92 | 1402.82 | 259.78 | 1458.53 | -253.92 | 1396.00 | 259.78 | 1451.71 | 6.81 |
| Base Game Mode Container 1x2 Raid or Army Release | 17 | 2 | Timer With Time Description | 1 | -253.92 | 1454.33 | 170.79 | 1495.06 | -253.92 | 1447.51 | 170.79 | 1488.24 | 6.81 |
| Base Game Mode Container 1x2 Raid or Army Release | 18 | 3 | Timer Description | 1 | -253.92 | 1454.33 | -253.92 | 1495.06 | -253.92 | 1447.51 | -253.92 | 1488.24 | 6.81 |
| Base Game Mode Container 1x2 Raid or Army Release | 19 | 4 | Clock Icon | 1 | -253.92 | 1454.31 | -213.16 | 1495.07 | -253.92 | 1447.50 | -213.16 | 1488.26 | 6.81 |
| Base Game Mode Container 1x2 Raid or Army Release | 20 | 5 | Timer Text | 1 | -213.16 | 1451.93 | -14.17 | 1497.45 | -213.16 | 1445.12 | -14.17 | 1490.64 | 6.81 |
| Collection Menu Variant | 67 | 6 | Create | 1 | 1180.00 | 80.94 | 1425.00 | 140.94 | 1179.99 | 80.94 | 1424.99 | 140.94 | 0.00 |
| Collection Menu Variant | 68 | 7 | Button Text | 1 | 1191.70 | 82.18 | 1412.50 | 139.70 | 1191.70 | 82.18 | 1412.50 | 139.70 | 0.00 |
| Collection Menu Variant | 69 | 6 | Import | 1 | 1450.00 | 80.94 | 1695.00 | 140.94 | 1449.99 | 80.94 | 1694.99 | 140.94 | 0.00 |
| Collection Menu Variant | 70 | 7 | Button Text | 1 | 1461.70 | 82.18 | 1682.50 | 139.70 | 1461.70 | 82.18 | 1682.50 | 139.70 | 0.00 |
| Collection Menu Variant | 71 | 6 | Unlock | 1 | 1720.00 | 80.94 | 1906.01 | 140.94 | 1719.99 | 80.94 | 1906.00 | 140.94 | 0.00 |
| Collection Menu Variant | 72 | 7 | Button Outline | 1 | 1720.50 | 80.94 | 1905.51 | 140.94 | 1720.49 | 80.94 | 1905.50 | 140.94 | 0.00 |
| Collection Menu Variant | 73 | 7 | Text | 1 | 1728.36 | 89.54 | 1897.65 | 132.35 | 1728.35 | 89.54 | 1897.64 | 132.35 | 0.00 |
| Deck Selection Popup with Tabs | 24 | 4 | Generic Round Button Variant | 1 | 479.71 | 137.45 | 539.71 | 197.45 | 442.87 | 137.45 | 502.87 | 197.45 | 36.83 |
| Deck Selection Popup with Tabs | 25 | 5 | Button Text | 1 | 487.71 | 137.45 | 531.71 | 197.45 | 450.87 | 137.45 | 494.87 | 197.45 | 36.83 |
| Deck Selection Popup with Tabs | 26 | 5 | Image | 1 | 489.59 | 147.33 | 529.83 | 187.56 | 452.76 | 147.33 | 492.99 | 187.56 | 36.83 |
| Deck Selection Popup with Tabs | 27 | 4 | Generic Round Button Variant (1) | 1 | 539.71 | 137.45 | 599.71 | 197.45 | 502.87 | 137.45 | 562.87 | 197.45 | 36.83 |
| Deck Selection Popup with Tabs | 28 | 5 | Button Text | 1 | 547.71 | 137.45 | 591.71 | 197.45 | 510.87 | 137.45 | 554.87 | 197.45 | 36.83 |
| Deck Selection Popup with Tabs | 29 | 5 | Image | 1 | 549.59 | 151.23 | 589.83 | 191.46 | 512.76 | 151.23 | 552.99 | 191.46 | 36.83 |
| Deck Selection Popup with Tabs | 30 | 5 | Image (1) | 1 | 549.59 | 138.13 | 589.83 | 178.36 | 512.76 | 138.13 | 552.99 | 178.36 | 36.83 |
| Deck Selection Popup with Tabs | 31 | 4 | Generic Round Button Variant (2) | 1 | 599.71 | 137.45 | 659.71 | 197.45 | 562.87 | 137.45 | 622.87 | 197.45 | 36.83 |
| Deck Selection Popup with Tabs | 32 | 5 | Button Text | 1 | 607.71 | 137.45 | 651.71 | 197.45 | 570.87 | 137.45 | 614.87 | 197.45 | 36.83 |
| Deck Selection Popup with Tabs | 33 | 5 | Image | 0.8,0.9 | 609.59 | 155.13 | 649.83 | 195.36 | 572.76 | 155.13 | 612.99 | 195.36 | 36.83 |
| Deck Selection Popup with Tabs | 34 | 5 | Image (1) | 0.8,0.9 | 609.59 | 144.33 | 649.83 | 184.56 | 572.76 | 144.33 | 612.99 | 184.56 | 36.83 |
| Deck Selection Popup with Tabs | 35 | 5 | Image (2) | 0.8,0.9 | 609.59 | 134.33 | 649.83 | 174.56 | 572.76 | 134.33 | 612.99 | 174.56 | 36.83 |
| Deck info Popup | 19 | 2 | Switch Deck Info Button | 1 | 1611.69 | 130.20 | 1686.08 | 205.80 | 1263.74 | 130.20 | 1338.13 | 205.80 | 347.95 |
| Deck info Popup | 20 | 3 | Background | 1 | 1619.84 | 138.18 | 1676.70 | 196.30 | 1271.89 | 138.18 | 1328.75 | 196.30 | 347.95 |
| Deck info Popup | 21 | 3 | Icon | 1 | 1619.84 | 138.18 | 1676.70 | 196.30 | 1271.89 | 138.18 | 1328.75 | 196.30 | 347.95 |
| Deck info Popup | 22 | 2 | Duplicate Button | 1 | 1636.08 | 130.20 | 1710.46 | 205.80 | 1288.13 | 130.20 | 1362.51 | 205.80 | 347.95 |
| Deck info Popup | 23 | 3 | Background | 1 | 1644.23 | 138.18 | 1701.09 | 196.30 | 1296.28 | 138.18 | 1353.14 | 196.30 | 347.95 |
| Deck info Popup | 24 | 3 | Icon | 1 | 1644.23 | 138.18 | 1701.09 | 196.30 | 1296.28 | 138.18 | 1353.14 | 196.30 | 347.95 |
| Deck info Popup | 25 | 2 | Share Button | 1 | 1660.46 | 130.20 | 1734.85 | 205.80 | 1312.51 | 130.20 | 1386.90 | 205.80 | 347.95 |
| Deck info Popup | 26 | 3 | Background | 1 | 1668.61 | 138.18 | 1725.48 | 196.30 | 1320.66 | 138.18 | 1377.53 | 196.30 | 347.95 |
| Deck info Popup | 27 | 3 | Icon | 1 | 1668.61 | 138.18 | 1725.48 | 196.30 | 1320.66 | 138.18 | 1377.53 | 196.30 | 347.95 |
| Deck info Popup | 28 | 2 | Share On Chat | 1 | 1684.85 | 130.20 | 1759.23 | 205.80 | 1336.90 | 130.20 | 1411.28 | 205.80 | 347.95 |
| Deck info Popup | 29 | 3 | Background | 1 | 1693.00 | 138.18 | 1749.86 | 196.30 | 1345.05 | 138.18 | 1401.91 | 196.30 | 347.95 |
| Deck info Popup | 30 | 3 | Icon | 1 | 1693.00 | 138.18 | 1749.86 | 196.30 | 1345.05 | 138.18 | 1401.91 | 196.30 | 347.95 |
| Deck info Popup | 31 | 2 | Delete Button | 1 | 1709.23 | 130.20 | 1783.62 | 205.80 | 1361.28 | 130.20 | 1435.67 | 205.80 | 347.95 |
| Deck info Popup | 32 | 3 | Background | 1 | 1717.39 | 138.18 | 1774.25 | 196.30 | 1369.44 | 138.18 | 1426.30 | 196.30 | 347.95 |
| Deck info Popup | 33 | 3 | Icon | 1 | 1717.39 | 138.18 | 1774.25 | 196.30 | 1369.44 | 138.18 | 1426.30 | 196.30 | 347.95 |
| Draft Game Mode Container 1x2 | 7 | 2 | Event Title | 1 | -253.92 | 1421.08 | 259.78 | 1476.79 | -253.92 | 1396.00 | 259.78 | 1451.71 | 25.08 |
| Draft Mode Deck Info Panel | 132 | 7 | Alliance Score Bar Line Level 1 | 1 | 1550.54 | 427.71 | 1846.16 | 488.19 | 1550.54 | 417.02 | 1846.16 | 477.50 | 10.69 |
| Draft Mode Deck Info Panel | 133 | 8 | Chest | 0.8696 | 1723.03 | 417.36 | 1814.07 | 498.55 | 1723.03 | 406.66 | 1814.07 | 487.86 | 10.69 |
| Draft Mode Deck Info Panel | 134 | 8 | Skull | 0.8696 | 1622.66 | 431.83 | 1681.24 | 484.08 | 1622.66 | 421.14 | 1681.24 | 473.39 | 10.69 |
| Draft Mode Deck Info Panel | 135 | 9 | Score | 1 | 1500.20 | 432.95 | 1616.35 | 482.95 | 1500.20 | 422.26 | 1616.35 | 472.26 | 10.69 |
| Draft Mode Deck Info Panel | 136 | 7 | Alliance Score Bar Line Level 2 | 1 | 1550.54 | 488.19 | 1846.16 | 548.68 | 1550.54 | 477.50 | 1846.16 | 537.98 | 10.69 |
| Draft Mode Deck Info Panel | 137 | 8 | Chest | 0.8696 | 1723.03 | 477.84 | 1814.07 | 559.03 | 1723.03 | 467.15 | 1814.07 | 548.34 | 10.69 |
| Draft Mode Deck Info Panel | 138 | 8 | Skull | 0.8696 | 1622.66 | 492.31 | 1681.24 | 544.56 | 1622.66 | 481.62 | 1681.24 | 533.87 | 10.69 |
| Draft Mode Deck Info Panel | 139 | 9 | Score | 1 | 1500.20 | 493.44 | 1616.35 | 543.44 | 1500.20 | 482.74 | 1616.35 | 532.74 | 10.69 |
| Draft Mode Deck Info Panel | 140 | 7 | Alliance Score Bar Line Level 3 | 1 | 1550.54 | 548.68 | 1846.16 | 609.16 | 1550.54 | 537.98 | 1846.16 | 598.47 | 10.69 |
| Draft Mode Deck Info Panel | 141 | 8 | Chest | 0.8696 | 1723.03 | 538.32 | 1814.07 | 619.52 | 1723.03 | 527.63 | 1814.07 | 608.82 | 10.69 |
| Draft Mode Deck Info Panel | 142 | 8 | Skull | 0.8696 | 1622.66 | 552.79 | 1681.24 | 605.04 | 1622.66 | 542.10 | 1681.24 | 594.35 | 10.69 |
| Draft Mode Deck Info Panel | 143 | 9 | Score | 1 | 1500.20 | 553.92 | 1616.35 | 603.92 | 1500.20 | 543.23 | 1616.35 | 593.23 | 10.69 |
| Draft Mode Deck Info Panel | 144 | 7 | Alliance Score Bar Line Level 4 | 1 | 1550.54 | 609.16 | 1846.16 | 669.64 | 1550.54 | 598.47 | 1846.16 | 658.95 | 10.69 |
| Draft Mode Deck Info Panel | 145 | 8 | Chest | 0.8696 | 1723.03 | 598.80 | 1814.07 | 680.00 | 1723.03 | 588.11 | 1814.07 | 669.31 | 10.69 |
| Draft Mode Deck Info Panel | 146 | 8 | Skull | 0.8696 | 1622.66 | 613.28 | 1681.24 | 665.52 | 1622.66 | 602.58 | 1681.24 | 654.83 | 10.69 |
| Draft Mode Deck Info Panel | 147 | 9 | Score | 1 | 1500.20 | 614.40 | 1616.35 | 664.40 | 1500.20 | 603.71 | 1616.35 | 653.71 | 10.69 |
| Draft Mode Deck Info Panel | 148 | 7 | Alliance Score Bar Line Level 5 | 1 | 1550.54 | 669.64 | 1846.16 | 730.12 | 1550.54 | 658.95 | 1846.16 | 719.43 | 10.69 |
| Draft Mode Deck Info Panel | 149 | 8 | Chest | 0.8696 | 1723.03 | 659.28 | 1814.07 | 740.48 | 1723.03 | 648.59 | 1814.07 | 729.79 | 10.69 |
| Draft Mode Deck Info Panel | 150 | 8 | Skull | 0.8696 | 1622.66 | 673.76 | 1681.24 | 726.01 | 1622.66 | 663.07 | 1681.24 | 715.31 | 10.69 |
| Draft Mode Deck Info Panel | 151 | 9 | Score | 1 | 1500.20 | 674.88 | 1616.35 | 724.88 | 1500.20 | 664.19 | 1616.35 | 714.19 | 10.69 |
| Draft Mode Menu Demo | 20 | 4 | Free Button | 1 | 987.00 | 707.24 | 1157.00 | 768.76 | 958.36 | 707.24 | 1128.36 | 768.76 | 28.64 |
| Draft Mode Menu Demo | 21 | 5 | Button Text | 1 | 996.04 | 718.28 | 1147.41 | 757.72 | 967.40 | 718.28 | 1118.77 | 757.72 | 28.64 |
| Draft Mode Menu Demo | 22 | 5 | FreeTimeText | 1 | 987.00 | 778.00 | 1157.00 | 848.00 | 958.36 | 778.00 | 1128.36 | 848.00 | 28.64 |
| Draft Mode Menu Demo | 23 | 4 | Premium Button | 1 | 1157.00 | 707.24 | 1327.00 | 768.76 | 1128.36 | 707.24 | 1298.36 | 768.76 | 28.64 |
| Draft Mode Menu Demo | 24 | 5 | layout | 1 | 1167.18 | 707.24 | 1316.20 | 768.76 | 1138.54 | 707.24 | 1287.56 | 768.76 | 28.64 |
| Draft Mode Menu Demo | 25 | 6 | icon | 1 | 1178.91 | 718.00 | 1238.91 | 758.00 | 1138.54 | 718.00 | 1198.54 | 758.00 | 40.36 |
| Draft Mode Menu Demo | 26 | 6 | text | 1 | 1238.91 | 718.00 | 1304.48 | 758.00 | 1198.54 | 718.00 | 1264.12 | 758.00 | 40.36 |
| Draft Mode Menu Demo | 27 | 5 | PremiumText | 1 | 1157.00 | 778.00 | 1327.00 | 848.00 | 1128.36 | 778.00 | 1298.36 | 848.00 | 28.64 |
| Draft Mode Menu Demo | 28 | 4 | Premium Ten Button | 1 | 1327.00 | 707.24 | 1497.00 | 768.76 | 1298.36 | 707.24 | 1468.36 | 768.76 | 28.64 |
| Draft Mode Menu Demo | 29 | 5 | layout | 1 | 1337.18 | 707.24 | 1486.20 | 768.76 | 1308.54 | 707.24 | 1457.56 | 768.76 | 28.64 |
| Draft Mode Menu Demo | 30 | 6 | icon | 1 | 1348.91 | 718.00 | 1408.91 | 758.00 | 1308.54 | 718.00 | 1368.54 | 758.00 | 40.36 |
| Draft Mode Menu Demo | 31 | 6 | text | 1 | 1408.91 | 718.00 | 1474.48 | 758.00 | 1368.54 | 718.00 | 1434.12 | 758.00 | 40.36 |
| Draft Mode Menu Demo | 32 | 5 | Premium10xText | 1 | 1327.00 | 778.00 | 1497.00 | 848.00 | 1298.36 | 778.00 | 1468.36 | 848.00 | 28.64 |
| Draft Mode Menu Demo | 261 | 8 | Alliance Score Bar Line Level 1 | 1 | 1549.54 | 463.77 | 1845.16 | 524.25 | 1549.54 | 453.08 | 1845.16 | 513.56 | 10.69 |
| Draft Mode Menu Demo | 262 | 9 | Chest | 0.8696 | 1722.03 | 453.41 | 1813.07 | 534.61 | 1722.03 | 442.72 | 1813.07 | 523.92 | 10.69 |
| Draft Mode Menu Demo | 263 | 9 | Skull | 0.8696 | 1621.66 | 467.89 | 1680.24 | 520.14 | 1621.66 | 457.20 | 1680.24 | 509.44 | 10.69 |
| Draft Mode Menu Demo | 264 | 10 | Score | 1 | 1499.20 | 469.01 | 1615.35 | 519.01 | 1499.20 | 458.32 | 1615.35 | 508.32 | 10.69 |
| Draft Mode Menu Demo | 265 | 8 | Alliance Score Bar Line Level 2 | 1 | 1549.54 | 524.25 | 1845.16 | 584.73 | 1549.54 | 513.56 | 1845.16 | 574.04 | 10.69 |
| Draft Mode Menu Demo | 266 | 9 | Chest | 0.8696 | 1722.03 | 513.90 | 1813.07 | 595.09 | 1722.03 | 503.20 | 1813.07 | 584.40 | 10.69 |
| Draft Mode Menu Demo | 267 | 9 | Skull | 0.8696 | 1621.66 | 528.37 | 1680.24 | 580.62 | 1621.66 | 517.68 | 1680.24 | 569.93 | 10.69 |
| Draft Mode Menu Demo | 268 | 10 | Score | 1 | 1499.20 | 529.49 | 1615.35 | 579.49 | 1499.20 | 518.80 | 1615.35 | 568.80 | 10.69 |
| Draft Mode Menu Demo | 269 | 8 | Alliance Score Bar Line Level 3 | 1 | 1549.54 | 584.73 | 1845.16 | 645.22 | 1549.54 | 574.04 | 1845.16 | 634.52 | 10.69 |
| Draft Mode Menu Demo | 270 | 9 | Chest | 0.8696 | 1722.03 | 574.38 | 1813.07 | 655.57 | 1722.03 | 563.69 | 1813.07 | 644.88 | 10.69 |
| Draft Mode Menu Demo | 271 | 9 | Skull | 0.8696 | 1621.66 | 588.85 | 1680.24 | 641.10 | 1621.66 | 578.16 | 1680.24 | 630.41 | 10.69 |
| Draft Mode Menu Demo | 272 | 10 | Score | 1 | 1499.20 | 589.98 | 1615.35 | 639.98 | 1499.20 | 579.28 | 1615.35 | 629.28 | 10.69 |
| Draft Mode Menu Demo | 273 | 8 | Alliance Score Bar Line Level 4 | 1 | 1549.54 | 645.22 | 1845.16 | 705.70 | 1549.54 | 634.52 | 1845.16 | 695.01 | 10.69 |
| Draft Mode Menu Demo | 274 | 9 | Chest | 0.8696 | 1722.03 | 634.86 | 1813.07 | 716.06 | 1722.03 | 624.17 | 1813.07 | 705.36 | 10.69 |
| Draft Mode Menu Demo | 275 | 9 | Skull | 0.8696 | 1621.66 | 649.33 | 1680.24 | 701.58 | 1621.66 | 638.64 | 1680.24 | 690.89 | 10.69 |
| Draft Mode Menu Demo | 276 | 10 | Score | 1 | 1499.20 | 650.46 | 1615.35 | 700.46 | 1499.20 | 639.77 | 1615.35 | 689.77 | 10.69 |
| Draft Mode Menu Demo | 277 | 8 | Alliance Score Bar Line Level 5 | 1 | 1549.54 | 705.70 | 1845.16 | 766.18 | 1549.54 | 695.01 | 1845.16 | 755.49 | 10.69 |
| Draft Mode Menu Demo | 278 | 9 | Chest | 0.8696 | 1722.03 | 695.34 | 1813.07 | 776.54 | 1722.03 | 684.65 | 1813.07 | 765.85 | 10.69 |
| Draft Mode Menu Demo | 279 | 9 | Skull | 0.8696 | 1621.66 | 709.82 | 1680.24 | 762.06 | 1621.66 | 699.12 | 1680.24 | 751.37 | 10.69 |
| Draft Mode Menu Demo | 280 | 10 | Score | 1 | 1499.20 | 710.94 | 1615.35 | 760.94 | 1499.20 | 700.25 | 1615.35 | 750.25 | 10.69 |
| Draft Mode Timed Mode Window | 20 | 4 | Free Button | 1 | 977.00 | 704.74 | 1147.00 | 766.26 | 948.36 | 704.74 | 1118.36 | 766.26 | 28.64 |
| Draft Mode Timed Mode Window | 21 | 5 | Button Text | 1 | 986.04 | 710.77 | 1137.41 | 760.23 | 957.40 | 710.77 | 1108.77 | 760.23 | 28.64 |
| Draft Mode Timed Mode Window | 22 | 5 | FreeTimeText | 1 | 977.00 | 775.50 | 1147.00 | 845.50 | 948.36 | 775.50 | 1118.36 | 845.50 | 28.64 |
| Draft Mode Timed Mode Window | 23 | 4 | Premium Button | 1 | 1147.00 | 704.74 | 1317.00 | 766.26 | 1118.36 | 704.74 | 1288.36 | 766.26 | 28.64 |
| Draft Mode Timed Mode Window | 24 | 5 | layout | 1 | 1157.18 | 704.74 | 1306.20 | 766.26 | 1128.54 | 704.74 | 1277.56 | 766.26 | 28.64 |
| Draft Mode Timed Mode Window | 25 | 6 | icon | 1 | 1168.91 | 715.50 | 1228.91 | 755.50 | 1128.54 | 715.50 | 1188.54 | 755.50 | 40.36 |
| Draft Mode Timed Mode Window | 26 | 6 | text | 1 | 1228.91 | 715.50 | 1294.48 | 755.50 | 1188.54 | 715.50 | 1254.12 | 755.50 | 40.36 |
| Draft Mode Timed Mode Window | 27 | 5 | PremiumText | 1 | 1147.00 | 775.50 | 1317.00 | 845.50 | 1118.36 | 775.50 | 1288.36 | 845.50 | 28.64 |
| Draft Mode Timed Mode Window | 28 | 4 | Premium Ten Button | 1 | 1317.00 | 704.74 | 1487.00 | 766.26 | 1288.36 | 704.74 | 1458.36 | 766.26 | 28.64 |
| Draft Mode Timed Mode Window | 29 | 5 | layout | 1 | 1327.18 | 704.74 | 1476.20 | 766.26 | 1298.54 | 704.74 | 1447.56 | 766.26 | 28.64 |
| Draft Mode Timed Mode Window | 30 | 6 | icon | 1 | 1338.91 | 715.50 | 1398.91 | 755.50 | 1298.54 | 715.50 | 1358.54 | 755.50 | 40.36 |
| Draft Mode Timed Mode Window | 31 | 6 | text | 1 | 1398.91 | 715.50 | 1464.48 | 755.50 | 1358.54 | 715.50 | 1424.12 | 755.50 | 40.36 |
| Draft Mode Timed Mode Window | 32 | 5 | Premium10xText | 1 | 1311.67 | 775.50 | 1497.13 | 845.50 | 1283.03 | 775.50 | 1468.49 | 845.50 | 28.64 |
| Draft Mode Timed Mode Window | 65 | 6 | Alliance Score Bar Line Level 1 | 1 | 1363.19 | 482.99 | 1658.81 | 543.47 | 1363.19 | 472.29 | 1658.81 | 532.78 | 10.69 |
| Draft Mode Timed Mode Window | 66 | 7 | Chest | 0.8696 | 1535.68 | 472.63 | 1626.72 | 553.82 | 1535.68 | 461.94 | 1626.72 | 543.13 | 10.69 |
| Draft Mode Timed Mode Window | 67 | 7 | Skull | 0.8696 | 1435.31 | 487.10 | 1493.89 | 539.35 | 1435.31 | 476.41 | 1493.89 | 528.66 | 10.69 |
| Draft Mode Timed Mode Window | 68 | 8 | Score | 1 | 1312.85 | 488.23 | 1429.00 | 538.23 | 1312.85 | 477.54 | 1429.00 | 527.54 | 10.69 |
| Draft Mode Timed Mode Window | 69 | 6 | Alliance Score Bar Line Level 2 | 1 | 1363.19 | 543.47 | 1658.81 | 603.95 | 1363.19 | 532.78 | 1658.81 | 593.26 | 10.69 |
| Draft Mode Timed Mode Window | 70 | 7 | Chest | 0.8696 | 1535.68 | 533.11 | 1626.72 | 614.31 | 1535.68 | 522.42 | 1626.72 | 603.62 | 10.69 |
| Draft Mode Timed Mode Window | 71 | 7 | Skull | 0.8696 | 1435.31 | 547.58 | 1493.89 | 599.83 | 1435.31 | 536.89 | 1493.89 | 589.14 | 10.69 |
| Draft Mode Timed Mode Window | 72 | 8 | Score | 1 | 1312.85 | 548.71 | 1429.00 | 598.71 | 1312.85 | 538.02 | 1429.00 | 588.02 | 10.69 |
| Draft Mode Timed Mode Window | 73 | 6 | Alliance Score Bar Line Level 3 | 1 | 1363.19 | 603.95 | 1658.81 | 664.43 | 1363.19 | 593.26 | 1658.81 | 653.74 | 10.69 |
| Draft Mode Timed Mode Window | 74 | 7 | Chest | 0.8696 | 1535.68 | 593.59 | 1626.72 | 674.79 | 1535.68 | 582.90 | 1626.72 | 664.10 | 10.69 |
| Draft Mode Timed Mode Window | 75 | 7 | Skull | 0.8696 | 1435.31 | 608.07 | 1493.89 | 660.31 | 1435.31 | 597.38 | 1493.89 | 649.62 | 10.69 |
| Draft Mode Timed Mode Window | 76 | 8 | Score | 1 | 1312.85 | 609.19 | 1429.00 | 659.19 | 1312.85 | 598.50 | 1429.00 | 648.50 | 10.69 |
| Draft Mode Timed Mode Window | 77 | 6 | Alliance Score Bar Line Level 4 | 1 | 1363.19 | 664.43 | 1658.81 | 724.91 | 1363.19 | 653.74 | 1658.81 | 714.22 | 10.69 |
| Draft Mode Timed Mode Window | 78 | 7 | Chest | 0.8696 | 1535.68 | 654.07 | 1626.72 | 735.27 | 1535.68 | 643.38 | 1626.72 | 724.58 | 10.69 |
| Draft Mode Timed Mode Window | 79 | 7 | Skull | 0.8696 | 1435.31 | 668.55 | 1493.89 | 720.80 | 1435.31 | 657.86 | 1493.89 | 710.11 | 10.69 |
| Draft Mode Timed Mode Window | 80 | 8 | Score | 1 | 1312.85 | 669.67 | 1429.00 | 719.67 | 1312.85 | 658.98 | 1429.00 | 708.98 | 10.69 |
| Draft Mode Timed Mode Window | 81 | 6 | Alliance Score Bar Line Level 5 | 1 | 1363.19 | 724.91 | 1658.81 | 785.40 | 1363.19 | 714.22 | 1658.81 | 774.70 | 10.69 |
| Draft Mode Timed Mode Window | 82 | 7 | Chest | 0.8696 | 1535.68 | 714.56 | 1626.72 | 795.75 | 1535.68 | 703.87 | 1626.72 | 785.06 | 10.69 |
| Draft Mode Timed Mode Window | 83 | 7 | Skull | 0.8696 | 1435.31 | 729.03 | 1493.89 | 781.28 | 1435.31 | 718.34 | 1493.89 | 770.59 | 10.69 |
| Draft Mode Timed Mode Window | 84 | 8 | Score | 1 | 1312.85 | 730.15 | 1429.00 | 780.15 | 1312.85 | 719.46 | 1429.00 | 769.46 | 10.69 |
| Draft Mode Timed Mode Window | 317 | 8 | Alliance Score Bar Line Level 1 | 1 | 1549.54 | 463.77 | 1845.16 | 524.25 | 1549.54 | 453.08 | 1845.16 | 513.56 | 10.69 |
| Draft Mode Timed Mode Window | 318 | 9 | Chest | 0.8696 | 1722.03 | 453.41 | 1813.07 | 534.61 | 1722.03 | 442.72 | 1813.07 | 523.92 | 10.69 |
| Draft Mode Timed Mode Window | 319 | 9 | Skull | 0.8696 | 1621.66 | 467.89 | 1680.24 | 520.14 | 1621.66 | 457.20 | 1680.24 | 509.44 | 10.69 |
| Draft Mode Timed Mode Window | 320 | 10 | Score | 1 | 1499.20 | 469.01 | 1615.35 | 519.01 | 1499.20 | 458.32 | 1615.35 | 508.32 | 10.69 |
| Draft Mode Timed Mode Window | 321 | 8 | Alliance Score Bar Line Level 2 | 1 | 1549.54 | 524.25 | 1845.16 | 584.73 | 1549.54 | 513.56 | 1845.16 | 574.04 | 10.69 |
| Draft Mode Timed Mode Window | 322 | 9 | Chest | 0.8696 | 1722.03 | 513.90 | 1813.07 | 595.09 | 1722.03 | 503.20 | 1813.07 | 584.40 | 10.69 |
| Draft Mode Timed Mode Window | 323 | 9 | Skull | 0.8696 | 1621.66 | 528.37 | 1680.24 | 580.62 | 1621.66 | 517.68 | 1680.24 | 569.93 | 10.69 |
| Draft Mode Timed Mode Window | 324 | 10 | Score | 1 | 1499.20 | 529.49 | 1615.35 | 579.49 | 1499.20 | 518.80 | 1615.35 | 568.80 | 10.69 |
| Draft Mode Timed Mode Window | 325 | 8 | Alliance Score Bar Line Level 3 | 1 | 1549.54 | 584.73 | 1845.16 | 645.22 | 1549.54 | 574.04 | 1845.16 | 634.52 | 10.69 |
| Draft Mode Timed Mode Window | 326 | 9 | Chest | 0.8696 | 1722.03 | 574.38 | 1813.07 | 655.57 | 1722.03 | 563.69 | 1813.07 | 644.88 | 10.69 |
| Draft Mode Timed Mode Window | 327 | 9 | Skull | 0.8696 | 1621.66 | 588.85 | 1680.24 | 641.10 | 1621.66 | 578.16 | 1680.24 | 630.41 | 10.69 |
| Draft Mode Timed Mode Window | 328 | 10 | Score | 1 | 1499.20 | 589.98 | 1615.35 | 639.98 | 1499.20 | 579.28 | 1615.35 | 629.28 | 10.69 |
| Draft Mode Timed Mode Window | 329 | 8 | Alliance Score Bar Line Level 4 | 1 | 1549.54 | 645.22 | 1845.16 | 705.70 | 1549.54 | 634.52 | 1845.16 | 695.01 | 10.69 |
| Draft Mode Timed Mode Window | 330 | 9 | Chest | 0.8696 | 1722.03 | 634.86 | 1813.07 | 716.06 | 1722.03 | 624.17 | 1813.07 | 705.36 | 10.69 |
| Draft Mode Timed Mode Window | 331 | 9 | Skull | 0.8696 | 1621.66 | 649.33 | 1680.24 | 701.58 | 1621.66 | 638.64 | 1680.24 | 690.89 | 10.69 |
| Draft Mode Timed Mode Window | 332 | 10 | Score | 1 | 1499.20 | 650.46 | 1615.35 | 700.46 | 1499.20 | 639.77 | 1615.35 | 689.77 | 10.69 |
| Draft Mode Timed Mode Window | 333 | 8 | Alliance Score Bar Line Level 5 | 1 | 1549.54 | 705.70 | 1845.16 | 766.18 | 1549.54 | 695.01 | 1845.16 | 755.49 | 10.69 |
| Draft Mode Timed Mode Window | 334 | 9 | Chest | 0.8696 | 1722.03 | 695.34 | 1813.07 | 776.54 | 1722.03 | 684.65 | 1813.07 | 765.85 | 10.69 |
| Draft Mode Timed Mode Window | 335 | 9 | Skull | 0.8696 | 1621.66 | 709.82 | 1680.24 | 762.06 | 1621.66 | 699.12 | 1680.24 | 751.37 | 10.69 |
| Draft Mode Timed Mode Window | 336 | 10 | Score | 1 | 1499.20 | 710.94 | 1615.35 | 760.94 | 1499.20 | 700.25 | 1615.35 | 750.25 | 10.69 |
| EnergySinglePlayerOnlyEventWindow | 15 | 4 | Score Bar Line Level 1 | 1 | 1470.09 | 414.94 | 1765.71 | 475.42 | 1470.09 | 404.25 | 1765.71 | 464.73 | 10.69 |
| EnergySinglePlayerOnlyEventWindow | 16 | 5 | Highlight Crate | 0.796 | 1610.96 | 376.67 | 1761.83 | 511.10 | 1610.96 | 365.98 | 1761.83 | 500.41 | 10.69 |
| EnergySinglePlayerOnlyEventWindow | 17 | 5 | Chest | 0.8696 | 1642.58 | 404.58 | 1733.61 | 485.78 | 1642.58 | 393.89 | 1733.61 | 475.09 | 10.69 |
| EnergySinglePlayerOnlyEventWindow | 18 | 5 | Skull | 0.8696 | 1541.97 | 415.66 | 1601.02 | 474.71 | 1541.97 | 404.96 | 1601.02 | 464.02 | 10.69 |
| EnergySinglePlayerOnlyEventWindow | 19 | 6 | Score | 1 | 1419.75 | 420.18 | 1535.90 | 470.18 | 1419.75 | 409.49 | 1535.90 | 459.49 | 10.69 |
| EnergySinglePlayerOnlyEventWindow | 20 | 4 | Score Bar Line Level 2 | 1 | 1470.09 | 475.42 | 1765.71 | 535.91 | 1470.09 | 464.73 | 1765.71 | 525.21 | 10.69 |
| EnergySinglePlayerOnlyEventWindow | 21 | 5 | Highlight Crate | 0.796 | 1610.96 | 437.15 | 1761.83 | 571.58 | 1610.96 | 426.46 | 1761.83 | 560.89 | 10.69 |
| EnergySinglePlayerOnlyEventWindow | 22 | 5 | Chest | 0.8696 | 1642.58 | 465.07 | 1733.61 | 546.26 | 1642.58 | 454.38 | 1733.61 | 535.57 | 10.69 |
| EnergySinglePlayerOnlyEventWindow | 23 | 5 | Skull | 0.8696 | 1541.97 | 476.14 | 1601.02 | 535.19 | 1541.97 | 465.45 | 1601.02 | 524.50 | 10.69 |
| EnergySinglePlayerOnlyEventWindow | 24 | 6 | Score | 1 | 1419.75 | 480.66 | 1535.90 | 530.66 | 1419.75 | 469.97 | 1535.90 | 519.97 | 10.69 |
| EnergySinglePlayerOnlyEventWindow | 25 | 4 | Score Bar Line Level 3 | 1 | 1470.09 | 535.91 | 1765.71 | 596.39 | 1470.09 | 525.21 | 1765.71 | 585.70 | 10.69 |
| EnergySinglePlayerOnlyEventWindow | 26 | 5 | Highlight Crate | 0.796 | 1610.96 | 497.63 | 1761.83 | 632.06 | 1610.96 | 486.94 | 1761.83 | 621.37 | 10.69 |
| EnergySinglePlayerOnlyEventWindow | 27 | 5 | Chest | 0.8696 | 1642.58 | 525.55 | 1733.61 | 606.74 | 1642.58 | 514.86 | 1733.61 | 596.05 | 10.69 |
| EnergySinglePlayerOnlyEventWindow | 28 | 5 | Skull | 0.8696 | 1541.97 | 536.62 | 1601.02 | 595.67 | 1541.97 | 525.93 | 1601.02 | 584.98 | 10.69 |
| EnergySinglePlayerOnlyEventWindow | 29 | 6 | Score | 1 | 1419.75 | 541.15 | 1535.90 | 591.15 | 1419.75 | 530.46 | 1535.90 | 580.46 | 10.69 |
| EnergySinglePlayerOnlyEventWindow | 30 | 4 | Score Bar Line Level 4 | 1 | 1470.09 | 596.39 | 1765.71 | 656.87 | 1470.09 | 585.70 | 1765.71 | 646.18 | 10.69 |
| EnergySinglePlayerOnlyEventWindow | 31 | 5 | Highlight Crate | 0.796 | 1610.96 | 558.11 | 1761.83 | 692.54 | 1610.96 | 547.42 | 1761.83 | 681.85 | 10.69 |
| EnergySinglePlayerOnlyEventWindow | 32 | 5 | Chest | 0.8696 | 1642.58 | 586.03 | 1733.61 | 667.23 | 1642.58 | 575.34 | 1733.61 | 656.54 | 10.69 |
| EnergySinglePlayerOnlyEventWindow | 33 | 5 | Skull | 0.8696 | 1541.97 | 597.10 | 1601.02 | 656.16 | 1541.97 | 586.41 | 1601.02 | 645.46 | 10.69 |
| EnergySinglePlayerOnlyEventWindow | 34 | 6 | Score | 1 | 1419.75 | 601.63 | 1535.90 | 651.63 | 1419.75 | 590.94 | 1535.90 | 640.94 | 10.69 |
| EnergySinglePlayerOnlyEventWindow | 35 | 4 | Score Bar Line Level 5 | 1 | 1470.09 | 656.87 | 1765.71 | 717.35 | 1470.09 | 646.18 | 1765.71 | 706.66 | 10.69 |
| EnergySinglePlayerOnlyEventWindow | 36 | 5 | Highlight Crate | 0.796 | 1610.96 | 618.60 | 1761.83 | 753.02 | 1610.96 | 607.91 | 1761.83 | 742.33 | 10.69 |
| EnergySinglePlayerOnlyEventWindow | 37 | 5 | Chest | 0.8696 | 1642.58 | 646.51 | 1733.61 | 727.71 | 1642.58 | 635.82 | 1733.61 | 717.02 | 10.69 |
| EnergySinglePlayerOnlyEventWindow | 38 | 5 | Skull | 0.8696 | 1541.97 | 657.58 | 1601.02 | 716.64 | 1541.97 | 646.89 | 1601.02 | 705.95 | 10.69 |
| EnergySinglePlayerOnlyEventWindow | 39 | 6 | Score | 1 | 1419.75 | 662.11 | 1535.90 | 712.11 | 1419.75 | 651.42 | 1535.90 | 701.42 | 10.69 |
| Game mode deck selector | 4 | 2 | Event Title | 1 | 6.48 | 1398.02 | 499.53 | 1453.73 | 6.48 | 1372.94 | 499.53 | 1428.65 | 25.08 |
| Legendary Display Profile | 7 | 3 | Rating Text | 1 | 439.48 | 337.14 | 770.15 | 373.44 | 438.41 | 337.14 | 769.08 | 373.44 | 1.07 |
| Legendary Display Profile | 8 | 4 | Secondary Icon | 1 | 439.48 | 373.44 | 439.48 | 373.44 | 438.41 | 373.44 | 438.41 | 373.44 | 1.07 |
| Legendary Display Profile | 9 | 4 | Main Icon | 1 | 439.48 | 332.79 | 499.48 | 377.79 | 438.41 | 332.79 | 498.41 | 377.79 | 1.07 |
| Legendary Display Profile | 10 | 4 | Individual rating value | 1 | 499.48 | 355.29 | 770.15 | 355.29 | 498.41 | 355.29 | 769.08 | 355.29 | 1.07 |
| Main Menu Offer Container 1x1 | 4 | 2 | Event Title | 1 | 706.08 | 689.31 | 1219.78 | 745.01 | 706.08 | 682.49 | 1219.78 | 738.20 | 6.81 |
| Main Menu Offer Container 1x1 | 5 | 2 | Timer With Time Description | 1 | 706.08 | 740.81 | 1130.79 | 781.54 | 706.08 | 734.00 | 1130.79 | 774.73 | 6.81 |
| Main Menu Offer Container 1x1 | 6 | 3 | Timer Description | 1 | 706.08 | 740.81 | 706.08 | 781.54 | 706.08 | 734.00 | 706.08 | 774.73 | 6.81 |
| Main Menu Offer Container 1x1 | 7 | 4 | Clock Icon | 1 | 706.08 | 740.80 | 746.84 | 781.56 | 706.08 | 733.98 | 746.84 | 774.74 | 6.81 |
| Main Menu Offer Container 1x1 | 8 | 5 | Timer Text | 1 | 746.84 | 738.42 | 945.83 | 783.94 | 746.84 | 731.60 | 945.83 | 777.12 | 6.81 |
| Main Menu Offer Container 1x1 Premium_booster_title_avatarOrResource | 4 | 2 | Event Title | 1 | 706.08 | 689.31 | 1219.78 | 745.01 | 706.08 | 682.49 | 1219.78 | 738.20 | 6.81 |
| Main Menu Offer Container 1x1 Premium_booster_title_avatarOrResource | 5 | 2 | Timer With Time Description | 1 | 706.08 | 740.81 | 1130.79 | 781.54 | 706.08 | 734.00 | 1130.79 | 774.73 | 6.81 |
| Main Menu Offer Container 1x1 Premium_booster_title_avatarOrResource | 6 | 3 | Timer Description | 1 | 706.08 | 740.81 | 706.08 | 781.54 | 706.08 | 734.00 | 706.08 | 774.73 | 6.81 |
| Main Menu Offer Container 1x1 Premium_booster_title_avatarOrResource | 7 | 4 | Clock Icon | 1 | 706.08 | 740.80 | 746.84 | 781.56 | 706.08 | 733.98 | 746.84 | 774.74 | 6.81 |
| Main Menu Offer Container 1x1 Premium_booster_title_avatarOrResource | 8 | 5 | Timer Text | 1 | 746.84 | 738.42 | 945.83 | 783.94 | 746.84 | 731.60 | 945.83 | 777.12 | 6.81 |
| Main Menu Offer Container Carousel 1x1 | 4 | 2 | Event Title | 1 | 706.08 | 689.31 | 1219.78 | 745.01 | 706.08 | 682.49 | 1219.78 | 738.20 | 6.81 |
| Main Menu Offer Container Carousel 1x1 | 5 | 2 | Timer With Time Description | 1 | 706.08 | 740.81 | 1130.79 | 781.54 | 706.08 | 734.00 | 1130.79 | 774.73 | 6.81 |
| Main Menu Offer Container Carousel 1x1 | 6 | 3 | Timer Description | 1 | 706.08 | 740.81 | 706.08 | 781.54 | 706.08 | 734.00 | 706.08 | 774.73 | 6.81 |
| Main Menu Offer Container Carousel 1x1 | 7 | 4 | Clock Icon | 1 | 706.08 | 740.80 | 746.84 | 781.56 | 706.08 | 733.98 | 746.84 | 774.74 | 6.81 |
| Main Menu Offer Container Carousel 1x1 | 8 | 5 | Timer Text | 1 | 746.84 | 738.42 | 945.83 | 783.94 | 746.84 | 731.60 | 945.83 | 777.12 | 6.81 |
| Main Menu Offer Container Static Image 1x1 | 4 | 2 | Event Title | 1 | 706.08 | 689.31 | 1219.78 | 745.01 | 706.08 | 682.49 | 1219.78 | 738.20 | 6.81 |
| Main Menu Offer Container Static Image 1x1 | 5 | 2 | Timer With Time Description | 1 | 706.08 | 740.81 | 1130.79 | 781.54 | 706.08 | 734.00 | 1130.79 | 774.73 | 6.81 |
| Main Menu Offer Container Static Image 1x1 | 6 | 3 | Timer Description | 1 | 706.08 | 740.81 | 706.08 | 781.54 | 706.08 | 734.00 | 706.08 | 774.73 | 6.81 |
| Main Menu Offer Container Static Image 1x1 | 7 | 4 | Clock Icon | 1 | 706.08 | 740.80 | 746.84 | 781.56 | 706.08 | 733.98 | 746.84 | 774.74 | 6.81 |
| Main Menu Offer Container Static Image 1x1 | 8 | 5 | Timer Text | 1 | 746.84 | 738.42 | 945.83 | 783.94 | 746.84 | 731.60 | 945.83 | 777.12 | 6.81 |
| Main Menu Offer Container Static Image 1x2 | 4 | 2 | Event Title | 1 | -253.92 | 1402.82 | 259.78 | 1458.53 | -253.92 | 1396.00 | 259.78 | 1451.71 | 6.81 |
| Main Menu Offer Container Static Image 1x2 | 5 | 2 | Timer With Time Description | 1 | -253.92 | 1454.33 | 170.79 | 1495.06 | -253.92 | 1447.51 | 170.79 | 1488.24 | 6.81 |
| Main Menu Offer Container Static Image 1x2 | 6 | 3 | Timer Description | 1 | -253.92 | 1454.33 | -253.92 | 1495.06 | -253.92 | 1447.51 | -253.92 | 1488.24 | 6.81 |
| Main Menu Offer Container Static Image 1x2 | 7 | 4 | Clock Icon | 1 | -253.92 | 1454.31 | -213.16 | 1495.07 | -253.92 | 1447.50 | -213.16 | 1488.26 | 6.81 |
| Main Menu Offer Container Static Image 1x2 | 8 | 5 | Timer Text | 1 | -213.16 | 1451.93 | -14.17 | 1497.45 | -213.16 | 1445.12 | -14.17 | 1490.64 | 6.81 |
| Main Menu Settings Window | 193 | 6 | Register Button | 1 | 1213.25 | 545.27 | 1513.25 | 635.27 | 891.25 | 545.27 | 1191.25 | 635.27 | 322.00 |
| Main Menu Settings Window | 194 | 7 | Button Text | 1 | 1224.25 | 562.66 | 1501.85 | 616.66 | 902.25 | 562.66 | 1179.85 | 616.66 | 322.00 |
| MessagePopupWindowDuel | 8 | 3 | Button Skirmish | 1 | 610.00 | 567.00 | 960.00 | 643.00 | 572.30 | 567.00 | 922.30 | 643.00 | 37.70 |
| MessagePopupWindowDuel | 9 | 4 | Button Text | 1 | 712.39 | 573.49 | 947.00 | 636.51 | 674.69 | 573.49 | 909.30 | 636.51 | 37.70 |
| MessagePopupWindowDuel | 10 | 4 | Skirmish image | 1 | 618.65 | 553.20 | 718.65 | 653.20 | 580.95 | 553.20 | 680.95 | 653.20 | 37.70 |
| MessagePopupWindowDuel | 11 | 3 | Button Classic | 1 | 960.00 | 567.00 | 1310.00 | 643.00 | 922.30 | 567.00 | 1272.30 | 643.00 | 37.70 |
| MessagePopupWindowDuel | 12 | 4 | Button Text | 1 | 1068.10 | 573.49 | 1297.00 | 636.51 | 1030.40 | 573.49 | 1259.30 | 636.51 | 37.70 |
| MessagePopupWindowDuel | 13 | 4 | Classic Image | 1 | 975.65 | 554.10 | 1075.65 | 654.10 | 937.95 | 554.10 | 1037.95 | 654.10 | 37.70 |
| Player Profile Window | 143 | 9 | Rating Text | 1 | 790.50 | 601.25 | 1121.17 | 637.54 | 789.43 | 601.25 | 1120.10 | 637.54 | 1.07 |
| Player Profile Window | 144 | 10 | Secondary Icon | 1 | 790.50 | 637.54 | 790.50 | 637.54 | 789.43 | 637.54 | 789.43 | 637.54 | 1.07 |
| Player Profile Window | 145 | 10 | Main Icon | 1 | 790.50 | 596.89 | 850.50 | 641.89 | 789.43 | 596.89 | 849.43 | 641.89 | 1.07 |
| Player Profile Window | 146 | 10 | Individual rating value | 1 | 850.50 | 619.39 | 1121.17 | 619.39 | 849.43 | 619.39 | 1120.10 | 619.39 | 1.07 |
| Player Profile Window | 155 | 9 | Rating Text | 1 | 790.50 | 389.60 | 1121.17 | 425.89 | 789.43 | 389.60 | 1120.10 | 425.89 | 1.07 |
| Player Profile Window | 156 | 10 | Secondary Icon | 1 | 790.50 | 425.89 | 790.50 | 425.89 | 789.43 | 425.89 | 789.43 | 425.89 | 1.07 |
| Player Profile Window | 157 | 10 | Main Icon | 1 | 790.50 | 385.25 | 850.50 | 430.25 | 789.43 | 385.25 | 849.43 | 430.25 | 1.07 |
| Player Profile Window | 158 | 10 | Individual rating value | 1 | 850.50 | 407.75 | 1121.17 | 407.75 | 849.43 | 407.75 | 1120.10 | 407.75 | 1.07 |
| Ranked Game Mode Container 1x2 | 16 | 2 | Event Title | 1 | -253.92 | 1402.82 | 259.78 | 1458.53 | -253.92 | 1396.00 | 259.78 | 1451.71 | 6.81 |
| Ranked Game Mode Container 1x2 | 17 | 2 | Timer With Time Description | 1 | -253.92 | 1454.33 | 170.79 | 1495.06 | -253.92 | 1447.51 | 170.79 | 1488.24 | 6.81 |
| Ranked Game Mode Container 1x2 | 18 | 3 | Timer Description | 1 | -253.92 | 1454.33 | -253.92 | 1495.06 | -253.92 | 1447.51 | -253.92 | 1488.24 | 6.81 |
| Ranked Game Mode Container 1x2 | 19 | 4 | Clock Icon | 1 | -253.92 | 1454.31 | -213.16 | 1495.07 | -253.92 | 1447.50 | -213.16 | 1488.26 | 6.81 |
| Ranked Game Mode Container 1x2 | 20 | 5 | Timer Text | 1 | -213.16 | 1451.93 | -14.17 | 1497.45 | -213.16 | 1445.12 | -14.17 | 1490.64 | 6.81 |
| ReRollPopup Variant | 8 | 3 | ButtonLeft | 1 | 610.00 | 541.00 | 960.00 | 617.00 | 572.30 | 541.00 | 922.30 | 617.00 | 37.70 |
| ReRollPopup Variant | 9 | 4 | Button Text | 1 | 623.00 | 541.00 | 947.00 | 617.00 | 585.30 | 541.00 | 909.30 | 617.00 | 37.70 |
| ReRollPopup Variant | 10 | 3 | Price Display | 1 | 960.00 | 541.00 | 1310.00 | 617.00 | 922.30 | 541.00 | 1272.30 | 617.00 | 37.70 |
| ReRollPopup Variant | 11 | 4 | Generic UI Button | 1 | 960.00 | 541.00 | 1310.00 | 617.00 | 922.30 | 541.00 | 1272.30 | 617.00 | 37.70 |
| ReRollPopup Variant | 12 | 5 | Button Text | 1 | 1128.75 | 541.00 | 1128.75 | 617.00 | 1091.05 | 541.00 | 1091.05 | 617.00 | 37.70 |
| ReRollPopup Variant | 13 | 5 | Price Display | 1 | 1141.25 | 541.00 | 1141.25 | 617.00 | 1103.55 | 541.00 | 1103.55 | 617.00 | 37.70 |
| ReRollPopup Variant | 14 | 6 | icon | 1.2 | 1146.85 | 551.00 | 1202.85 | 607.00 | 1109.15 | 551.00 | 1165.15 | 607.00 | 37.70 |
| ReRollPopup Variant | 15 | 6 | text | 1 | 1213.95 | 551.00 | 1213.95 | 607.00 | 1176.25 | 551.00 | 1176.25 | 607.00 | 37.70 |
| Reward Event Container 1x1 | 4 | 2 | Event Title | 1 | 706.08 | 689.31 | 1219.78 | 745.01 | 706.08 | 682.49 | 1219.78 | 738.20 | 6.81 |
| Reward Event Container 1x1 | 5 | 2 | Timer With Time Description | 1 | 706.08 | 740.81 | 1130.79 | 781.54 | 706.08 | 734.00 | 1130.79 | 774.73 | 6.81 |
| Reward Event Container 1x1 | 6 | 3 | Timer Description | 1 | 706.08 | 740.81 | 706.08 | 781.54 | 706.08 | 734.00 | 706.08 | 774.73 | 6.81 |
| Reward Event Container 1x1 | 7 | 4 | Clock Icon | 1 | 706.08 | 740.80 | 746.84 | 781.56 | 706.08 | 733.98 | 746.84 | 774.74 | 6.81 |
| Reward Event Container 1x1 | 8 | 5 | Timer Text | 1 | 746.84 | 738.42 | 945.83 | 783.94 | 746.84 | 731.60 | 945.83 | 777.12 | 6.81 |
| Skirmish Ranked Game Mode Container 1x2 | 16 | 2 | Event Title | 1 | -253.92 | 1402.82 | 259.78 | 1458.53 | -253.92 | 1396.00 | 259.78 | 1451.71 | 6.81 |
| Skirmish Ranked Game Mode Container 1x2 | 17 | 2 | Timer With Time Description | 1 | -253.92 | 1454.33 | 170.79 | 1495.06 | -253.92 | 1447.51 | 170.79 | 1488.24 | 6.81 |
| Skirmish Ranked Game Mode Container 1x2 | 18 | 3 | Timer Description | 1 | -253.92 | 1454.33 | -253.92 | 1495.06 | -253.92 | 1447.51 | -253.92 | 1488.24 | 6.81 |
| Skirmish Ranked Game Mode Container 1x2 | 19 | 4 | Clock Icon | 1 | -253.92 | 1454.31 | -213.16 | 1495.07 | -253.92 | 1447.50 | -213.16 | 1488.26 | 6.81 |
| Skirmish Ranked Game Mode Container 1x2 | 20 | 5 | Timer Text | 1 | -213.16 | 1451.93 | -14.17 | 1497.45 | -213.16 | 1445.12 | -14.17 | 1490.64 | 6.81 |
| SkirmishModeEventWindow | 36 | 4 | Score Bar Line Level 1 | 1 | 151.19 | 465.72 | 446.81 | 526.20 | 151.19 | 455.03 | 446.81 | 515.51 | 10.69 |
| SkirmishModeEventWindow | 37 | 5 | Highlight Crate | 0.796 | 292.07 | 427.45 | 442.93 | 561.88 | 292.07 | 416.76 | 442.93 | 551.19 | 10.69 |
| SkirmishModeEventWindow | 38 | 5 | Chest | 0.8696 | 323.68 | 455.37 | 414.72 | 536.56 | 323.68 | 444.67 | 414.72 | 525.87 | 10.69 |
| SkirmishModeEventWindow | 39 | 5 | Skull | 0.8696 | 223.07 | 466.44 | 282.13 | 525.49 | 223.07 | 455.74 | 282.13 | 514.80 | 10.69 |
| SkirmishModeEventWindow | 40 | 6 | Score | 1 | 100.85 | 470.96 | 217.00 | 520.96 | 100.85 | 460.27 | 217.00 | 510.27 | 10.69 |
| SkirmishModeEventWindow | 41 | 4 | Score Bar Line Level 2 | 1 | 151.19 | 526.20 | 446.81 | 586.69 | 151.19 | 515.51 | 446.81 | 576.00 | 10.69 |
| SkirmishModeEventWindow | 42 | 5 | Highlight Crate | 0.796 | 292.07 | 487.93 | 442.93 | 622.36 | 292.07 | 477.24 | 442.93 | 611.67 | 10.69 |
| SkirmishModeEventWindow | 43 | 5 | Chest | 0.8696 | 323.68 | 515.85 | 414.72 | 597.04 | 323.68 | 505.16 | 414.72 | 586.35 | 10.69 |
| SkirmishModeEventWindow | 44 | 5 | Skull | 0.8696 | 223.07 | 526.92 | 282.13 | 585.97 | 223.07 | 516.23 | 282.13 | 575.28 | 10.69 |
| SkirmishModeEventWindow | 45 | 6 | Score | 1 | 100.85 | 531.45 | 217.00 | 581.45 | 100.85 | 520.75 | 217.00 | 570.75 | 10.69 |
| SkirmishModeEventWindow | 46 | 4 | Score Bar Line Level 3 | 1 | 151.19 | 586.69 | 446.81 | 647.17 | 151.19 | 576.00 | 446.81 | 636.48 | 10.69 |
| SkirmishModeEventWindow | 47 | 5 | Highlight Crate | 0.796 | 292.07 | 548.41 | 442.93 | 682.84 | 292.07 | 537.72 | 442.93 | 672.15 | 10.69 |
| SkirmishModeEventWindow | 48 | 5 | Chest | 0.8696 | 323.68 | 576.33 | 414.72 | 657.53 | 323.68 | 565.64 | 414.72 | 646.83 | 10.69 |
| SkirmishModeEventWindow | 49 | 5 | Skull | 0.8696 | 223.07 | 587.40 | 282.13 | 646.46 | 223.07 | 576.71 | 282.13 | 635.76 | 10.69 |
| SkirmishModeEventWindow | 50 | 6 | Score | 1 | 100.85 | 591.93 | 217.00 | 641.93 | 100.85 | 581.24 | 217.00 | 631.24 | 10.69 |
| SkirmishModeEventWindow | 51 | 4 | Score Bar Line Level 4 | 1 | 151.19 | 647.17 | 446.81 | 707.65 | 151.19 | 636.48 | 446.81 | 696.96 | 10.69 |
| SkirmishModeEventWindow | 52 | 5 | Highlight Crate | 0.796 | 292.07 | 608.90 | 442.93 | 743.32 | 292.07 | 598.20 | 442.93 | 732.63 | 10.69 |
| SkirmishModeEventWindow | 53 | 5 | Chest | 0.8696 | 323.68 | 636.81 | 414.72 | 718.01 | 323.68 | 626.12 | 414.72 | 707.32 | 10.69 |
| SkirmishModeEventWindow | 54 | 5 | Skull | 0.8696 | 223.07 | 647.88 | 282.13 | 706.94 | 223.07 | 637.19 | 282.13 | 696.25 | 10.69 |
| SkirmishModeEventWindow | 55 | 6 | Score | 1 | 100.85 | 652.41 | 217.00 | 702.41 | 100.85 | 641.72 | 217.00 | 691.72 | 10.69 |
| SkirmishModeEventWindow | 56 | 4 | Score Bar Line Level 5 | 1 | 151.19 | 707.65 | 446.81 | 768.13 | 151.19 | 696.96 | 446.81 | 757.44 | 10.69 |
| SkirmishModeEventWindow | 57 | 5 | Highlight Crate | 0.796 | 292.07 | 669.38 | 442.93 | 803.81 | 292.07 | 658.69 | 442.93 | 793.11 | 10.69 |
| SkirmishModeEventWindow | 58 | 5 | Chest | 0.8696 | 323.68 | 697.29 | 414.72 | 778.49 | 323.68 | 686.60 | 414.72 | 767.80 | 10.69 |
| SkirmishModeEventWindow | 59 | 5 | Skull | 0.8696 | 223.07 | 708.36 | 282.13 | 767.42 | 223.07 | 697.67 | 282.13 | 756.73 | 10.69 |
| SkirmishModeEventWindow | 60 | 6 | Score | 1 | 100.85 | 712.89 | 217.00 | 762.89 | 100.85 | 702.20 | 217.00 | 752.20 | 10.69 |
| Two Sides Event Window | 67 | 5 | Score Bar Line Level 1 | 1 | 1503.19 | 452.72 | 1798.81 | 513.20 | 1503.19 | 442.03 | 1798.81 | 502.51 | 10.69 |
| Two Sides Event Window | 68 | 6 | Highlight Crate | 0.796 | 1644.07 | 414.45 | 1794.93 | 548.88 | 1644.07 | 403.76 | 1794.93 | 538.19 | 10.69 |
| Two Sides Event Window | 69 | 6 | Chest | 0.8696 | 1675.68 | 442.37 | 1766.72 | 523.56 | 1675.68 | 431.67 | 1766.72 | 512.87 | 10.69 |
| Two Sides Event Window | 70 | 6 | Skull | 0.8696 | 1575.07 | 453.44 | 1634.13 | 512.49 | 1575.07 | 442.74 | 1634.13 | 501.80 | 10.69 |
| Two Sides Event Window | 71 | 7 | Score | 1 | 1452.85 | 457.96 | 1569.00 | 507.96 | 1452.85 | 447.27 | 1569.00 | 497.27 | 10.69 |
| Two Sides Event Window | 72 | 5 | Score Bar Line Level 2 | 1 | 1503.19 | 513.20 | 1798.81 | 573.69 | 1503.19 | 502.51 | 1798.81 | 563.00 | 10.69 |
| Two Sides Event Window | 73 | 6 | Highlight Crate | 0.796 | 1644.07 | 474.93 | 1794.93 | 609.36 | 1644.07 | 464.24 | 1794.93 | 598.67 | 10.69 |
| Two Sides Event Window | 74 | 6 | Chest | 0.8696 | 1675.68 | 502.85 | 1766.72 | 584.04 | 1675.68 | 492.16 | 1766.72 | 573.35 | 10.69 |
| Two Sides Event Window | 75 | 6 | Skull | 0.8696 | 1575.07 | 513.92 | 1634.13 | 572.97 | 1575.07 | 503.23 | 1634.13 | 562.28 | 10.69 |
| Two Sides Event Window | 76 | 7 | Score | 1 | 1452.85 | 518.45 | 1569.00 | 568.45 | 1452.85 | 507.75 | 1569.00 | 557.75 | 10.69 |
| Two Sides Event Window | 77 | 5 | Score Bar Line Level 3 | 1 | 1503.19 | 573.69 | 1798.81 | 634.17 | 1503.19 | 563.00 | 1798.81 | 623.48 | 10.69 |
| Two Sides Event Window | 78 | 6 | Highlight Crate | 0.796 | 1644.07 | 535.41 | 1794.93 | 669.84 | 1644.07 | 524.72 | 1794.93 | 659.15 | 10.69 |
| Two Sides Event Window | 79 | 6 | Chest | 0.8696 | 1675.68 | 563.33 | 1766.72 | 644.53 | 1675.68 | 552.64 | 1766.72 | 633.83 | 10.69 |
| Two Sides Event Window | 80 | 6 | Skull | 0.8696 | 1575.07 | 574.40 | 1634.13 | 633.46 | 1575.07 | 563.71 | 1634.13 | 622.76 | 10.69 |
| Two Sides Event Window | 81 | 7 | Score | 1 | 1452.85 | 578.93 | 1569.00 | 628.93 | 1452.85 | 568.24 | 1569.00 | 618.24 | 10.69 |
| Two Sides Event Window | 82 | 5 | Score Bar Line Level 4 | 1 | 1503.19 | 634.17 | 1798.81 | 694.65 | 1503.19 | 623.48 | 1798.81 | 683.96 | 10.69 |
| Two Sides Event Window | 83 | 6 | Highlight Crate | 0.796 | 1644.07 | 595.90 | 1794.93 | 730.32 | 1644.07 | 585.20 | 1794.93 | 719.63 | 10.69 |
| Two Sides Event Window | 84 | 6 | Chest | 0.8696 | 1675.68 | 623.81 | 1766.72 | 705.01 | 1675.68 | 613.12 | 1766.72 | 694.32 | 10.69 |
| Two Sides Event Window | 85 | 6 | Skull | 0.8696 | 1575.07 | 634.88 | 1634.13 | 693.94 | 1575.07 | 624.19 | 1634.13 | 683.25 | 10.69 |
| Two Sides Event Window | 86 | 7 | Score | 1 | 1452.85 | 639.41 | 1569.00 | 689.41 | 1452.85 | 628.72 | 1569.00 | 678.72 | 10.69 |
| Two Sides Event Window | 87 | 5 | Score Bar Line Level 5 | 1 | 1503.19 | 694.65 | 1798.81 | 755.13 | 1503.19 | 683.96 | 1798.81 | 744.44 | 10.69 |
| Two Sides Event Window | 88 | 6 | Highlight Crate | 0.796 | 1644.07 | 656.38 | 1794.93 | 790.81 | 1644.07 | 645.69 | 1794.93 | 780.11 | 10.69 |
| Two Sides Event Window | 89 | 6 | Chest | 0.8696 | 1675.68 | 684.29 | 1766.72 | 765.49 | 1675.68 | 673.60 | 1766.72 | 754.80 | 10.69 |
| Two Sides Event Window | 90 | 6 | Skull | 0.8696 | 1575.07 | 695.36 | 1634.13 | 754.42 | 1575.07 | 684.67 | 1634.13 | 743.73 | 10.69 |
| Two Sides Event Window | 91 | 7 | Score | 1 | 1452.85 | 699.89 | 1569.00 | 749.89 | 1452.85 | 689.20 | 1569.00 | 739.20 | 10.69 |

（共 **351** 行）

## 附录 B：`m_ChildScaleWidth/Height` 的**全量**影响清单（W0 旧 → W1 现状）

### B.1 逐节点「旧值 → 新值」

复现：`_axis_scale` 恒返回 `1.0` = 2026-10-04 之前的行为。

| 窗口 | # | 缩进 | 节点 | 局部缩放 | 旧 x1 | 旧 y1 | 旧 x2 | 旧 y2 | 新 x1 | 新 y1 | 新 x2 | 新 y2 | 最大位移 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Booster Info Popup | 29 | 7 | text | 1 | 1054.34 | 725.06 | 1146.56 | 779.60 | 1058.70 | 725.06 | 1150.92 | 779.60 | 4.36 |
| Booster Info Popup | 34 | 5 | Button Text | 1 | 1291.23 | 725.86 | 1423.23 | 777.74 | 1296.42 | 725.86 | 1428.42 | 777.74 | 5.19 |
| Booster Offer Container | 12 | 6 | text | 1 | -71.31 | 1223.42 | 20.91 | 1263.00 | -67.05 | 1223.42 | 25.17 | 1263.00 | 4.26 |
| Card Shop Tab | 18 | 10 | icon | 1.2 | 162.10 | 1080.62 | 195.17 | 1080.82 | 165.41 | 1080.62 | 198.47 | 1080.82 | 3.31 |
| Card Shop Tab | 19 | 10 | text | 1 | 195.17 | 1080.62 | 287.39 | 1080.82 | 201.78 | 1080.62 | 294.00 | 1080.82 | 6.61 |
| Card Shop VIP Tab Variant | 28 | 10 | icon | 1.2 | 162.10 | 1080.62 | 195.17 | 1080.82 | 165.41 | 1080.62 | 198.47 | 1080.82 | 3.31 |
| Card Shop VIP Tab Variant | 29 | 10 | text | 1 | 195.17 | 1080.62 | 319.67 | 1080.82 | 201.78 | 1080.62 | 326.28 | 1080.82 | 6.61 |
| Catalog Item Shop Container | 10 | 6 | text | 1 | 10.40 | 1263.55 | 10.40 | 1282.84 | 12.57 | 1263.55 | 12.57 | 1282.84 | 2.17 |
| Cosmetic Item Shop Container | 10 | 6 | text | 1 | 10.40 | 1263.55 | 10.40 | 1282.84 | 12.57 | 1263.55 | 12.57 | 1282.84 | 2.17 |
| Daily Shop Tab | 18 | 10 | icon | 1.2 | 162.10 | 1080.62 | 195.17 | 1080.82 | 165.41 | 1080.62 | 198.47 | 1080.82 | 3.31 |
| Daily Shop Tab | 19 | 10 | text | 1 | 195.17 | 1080.62 | 287.39 | 1080.82 | 201.78 | 1080.62 | 294.00 | 1080.82 | 6.61 |
| Draft Mode Menu Demo | 85 | 6 | text | 1 | 1173.15 | 885.79 | 1263.68 | 933.02 | 1178.88 | 885.79 | 1269.41 | 933.02 | 5.73 |
| Draft Mode Timed Mode Window | 141 | 6 | text | 1 | 1173.15 | 885.79 | 1263.68 | 933.02 | 1178.88 | 885.79 | 1269.41 | 933.02 | 5.73 |
| Expansion Pass Progress Bar | 6 | 2 | icon | 1.7 | 788.58 | 822.47 | 825.82 | 859.71 | 801.61 | 822.47 | 838.85 | 859.71 | 13.03 |
| Expansion Pass Progress Bar | 7 | 2 | text | 1 | 825.82 | 822.47 | 934.81 | 859.71 | 851.89 | 822.47 | 960.88 | 859.71 | 26.07 |
| Expansion Pass Reward Container | 9 | 5 | text | 1 | -124.17 | 1384.30 | -124.17 | 1423.32 | -119.90 | 1384.30 | -119.90 | 1423.32 | 4.26 |
| Expansion Pass Reward Container | 22 | 4 | icon | 1.7 | -187.70 | 1311.88 | -150.46 | 1349.12 | -174.67 | 1311.88 | -137.43 | 1349.12 | 13.03 |
| Expansion Pass Reward Container | 23 | 4 | text | 1 | -150.46 | 1311.88 | -41.47 | 1349.12 | -124.40 | 1311.88 | -15.41 | 1349.12 | 26.07 |
| Gacha Tab | 14 | 5 | text | 1 | 638.62 | 807.71 | 638.62 | 891.92 | 646.68 | 807.71 | 646.68 | 891.92 | 8.05 |
| General Basic Offer Container Booster_CardOrAltArt | 96 | 6 | text | 1 | -29.08 | 1482.99 | -29.08 | 1530.86 | -24.16 | 1482.99 | -24.16 | 1530.86 | 4.92 |
| General Basic Offer Container Booster_CardOrAltArt_Cardback_Avatar_Title | 160 | 6 | text | 1 | -91.33 | 1482.99 | 33.17 | 1530.86 | -86.41 | 1482.99 | 38.09 | 1530.86 | 4.92 |
| General Basic Offer Container Booster_CardOrAltArt__AvatarORTitle | 160 | 6 | text | 1 | -91.33 | 1482.99 | 33.17 | 1530.86 | -86.41 | 1482.99 | 38.09 | 1530.86 | 4.92 |
| General Basic Offer Container Variant 2 Currencies | 37 | 6 | text | 1 | -71.31 | 1417.26 | 20.91 | 1457.34 | -67.05 | 1417.26 | 25.17 | 1457.34 | 4.26 |
| General Basic Offer Container Variant Booster + 2 Currencies | 49 | 6 | text | 1 | -60.51 | 1405.56 | 31.71 | 1445.64 | -56.25 | 1405.56 | 35.97 | 1445.64 | 4.26 |
| General Basic Offer Container Variant Booster_avatar_cardback_title | 89 | 6 | text | 1 | -28.39 | 1417.26 | -28.39 | 1457.34 | -24.77 | 1417.26 | -24.77 | 1457.34 | 3.62 |
| General Basic Offer Container Variant Booster_avatar_resource | 51 | 6 | text | 1 | -25.20 | 1417.26 | -25.20 | 1457.34 | -20.94 | 1417.26 | -20.94 | 1457.34 | 4.26 |
| General Basic Offer Container Variant Booster_cardback_resource | 59 | 6 | text | 1 | -25.20 | 1417.26 | -25.20 | 1457.34 | -20.94 | 1417.26 | -20.94 | 1457.34 | 4.26 |
| General Basic Offer Container Variant Booster_title_resource | 37 | 6 | text | 1 | -25.20 | 1417.26 | -25.20 | 1457.34 | -20.94 | 1417.26 | -20.94 | 1457.34 | 4.26 |
| General Basic Offer Container Variant Deck_cardback_avatar | 80 | 6 | text | 1 | -28.39 | 1417.26 | -28.39 | 1457.34 | -24.77 | 1417.26 | -24.77 | 1457.34 | 3.62 |
| General Basic Offer Container Variant Premium_Booster_avatar_cardback_title | 102 | 6 | text | 1 | -28.39 | 1417.26 | -28.39 | 1457.34 | -24.77 | 1417.26 | -24.77 | 1457.34 | 3.62 |
| General Basic Offer Container Variant Premium_Booster_avatar_cardback_title_resource | 127 | 6 | text | 1 | -71.31 | 1417.26 | 20.91 | 1457.34 | -67.05 | 1417.26 | 25.17 | 1457.34 | 4.26 |
| General Basic Offer Container Variant Premium_Premium_cardback_avatar | 75 | 6 | text | 1 | -25.20 | 1417.26 | -25.20 | 1457.34 | -20.94 | 1417.26 | -20.94 | 1457.34 | 4.26 |
| General Basic Offer Container Variant Premium_Resource | 51 | 6 | text | 1 | -25.20 | 1417.26 | -25.20 | 1457.34 | -20.94 | 1417.26 | -20.94 | 1457.34 | 4.26 |
| General Basic Offer Container Variant Premium_booster_title_avatarOrResource | 92 | 6 | text | 1 | -25.20 | 1417.26 | -25.20 | 1457.34 | -20.94 | 1417.26 | -20.94 | 1457.34 | 4.26 |
| General Basic Offer Container Variant Single Item Type | 214 | 6 | text | 1 | -87.45 | 1417.26 | 37.05 | 1457.34 | -83.19 | 1417.26 | 41.31 | 1457.34 | 4.26 |
| General Basic Offer Container Variant avatarOrTitle_resource | 67 | 6 | text | 1 | -25.20 | 1417.26 | -25.20 | 1457.34 | -20.94 | 1417.26 | -20.94 | 1457.34 | 4.26 |
| General Basic Offer Container Variant cardback_premiumOrAvatarOrResource_titleOrResource | 114 | 6 | text | 1 | -28.39 | 1417.26 | -28.39 | 1457.34 | -24.77 | 1417.26 | -24.77 | 1457.34 | 3.62 |
| General Basic Offer Popup Booster_CardOrAltArt | 26 | 7 | text | 1 | 1043.39 | 687.52 | 1167.89 | 743.98 | 1047.89 | 687.52 | 1172.39 | 743.98 | 4.50 |
| General Basic Offer Popup Booster_CardOrAltArt | 34 | 5 | Button Text | 1 | 1365.48 | 689.26 | 1365.48 | 741.14 | 1370.67 | 689.26 | 1370.67 | 741.14 | 5.19 |
| General Basic Offer Popup Booster_CardOrAltArt_AvatarORTitle | 26 | 7 | text | 1 | 1043.39 | 687.52 | 1167.89 | 743.98 | 1047.89 | 687.52 | 1172.39 | 743.98 | 4.50 |
| General Basic Offer Popup Booster_CardOrAltArt_AvatarORTitle | 34 | 5 | Button Text | 1 | 1365.48 | 689.26 | 1365.48 | 741.14 | 1370.67 | 689.26 | 1370.67 | 741.14 | 5.19 |
| General Basic Offer Popup Booster_CardOrAltArt_Cardback_Avatar_Title | 26 | 7 | text | 1 | 1043.39 | 687.52 | 1167.89 | 743.98 | 1047.89 | 687.52 | 1172.39 | 743.98 | 4.50 |
| General Basic Offer Popup Booster_CardOrAltArt_Cardback_Avatar_Title | 34 | 5 | Button Text | 1 | 1365.48 | 689.26 | 1365.48 | 741.14 | 1370.67 | 689.26 | 1370.67 | 741.14 | 5.19 |
| General Basic Offer Popup Just Foreground | 26 | 7 | text | 1 | 1043.39 | 687.52 | 1167.89 | 743.98 | 1047.89 | 687.52 | 1172.39 | 743.98 | 4.50 |
| General Basic Offer Popup Just Foreground | 34 | 5 | Button Text | 1 | 1365.48 | 689.26 | 1365.48 | 741.14 | 1370.67 | 689.26 | 1370.67 | 741.14 | 5.19 |
| General Basic Offer Popup Variant 2 Currencies | 50 | 7 | text | 1 | 1203.53 | 687.52 | 1295.75 | 743.98 | 1208.03 | 687.52 | 1300.25 | 743.98 | 4.50 |
| General Basic Offer Popup Variant 2 Currencies | 58 | 5 | Button Text | 1 | 1443.48 | 689.26 | 1575.48 | 741.14 | 1448.67 | 689.26 | 1580.67 | 741.14 | 5.19 |
| General Basic Offer Popup Variant Booster + 2 Currencies | 62 | 7 | text | 1 | 1203.53 | 687.52 | 1295.75 | 743.98 | 1208.03 | 687.52 | 1300.25 | 743.98 | 4.50 |
| General Basic Offer Popup Variant Booster + 2 Currencies | 70 | 5 | Button Text | 1 | 1443.48 | 689.26 | 1575.48 | 741.14 | 1448.67 | 689.26 | 1580.67 | 741.14 | 5.19 |
| General Basic Offer Popup Variant Booster_avatar_resource | 38 | 7 | text | 1 | 1059.53 | 687.52 | 1151.75 | 743.98 | 1064.03 | 687.52 | 1156.25 | 743.98 | 4.50 |
| General Basic Offer Popup Variant Booster_avatar_resource | 46 | 5 | Button Text | 1 | 1299.48 | 689.26 | 1431.48 | 741.14 | 1304.67 | 689.26 | 1436.67 | 741.14 | 5.19 |
| General Basic Offer Popup Variant Booster_cardback_resource | 38 | 7 | text | 1 | 1059.53 | 687.52 | 1151.75 | 743.98 | 1064.03 | 687.52 | 1156.25 | 743.98 | 4.50 |
| General Basic Offer Popup Variant Booster_cardback_resource | 46 | 5 | Button Text | 1 | 1299.48 | 689.26 | 1431.48 | 741.14 | 1304.67 | 689.26 | 1436.67 | 741.14 | 5.19 |
| General Basic Offer Popup Variant Booster_title_resource | 38 | 7 | text | 1 | 1059.53 | 687.52 | 1151.75 | 743.98 | 1064.03 | 687.52 | 1156.25 | 743.98 | 4.50 |
| General Basic Offer Popup Variant Booster_title_resource | 46 | 5 | Button Text | 1 | 1365.48 | 689.26 | 1365.48 | 741.14 | 1370.67 | 689.26 | 1370.67 | 741.14 | 5.19 |
| General Basic Offer Popup Variant Expansion pass | 183 | 7 | text | 1 | 1043.39 | 687.52 | 1167.89 | 743.98 | 1047.89 | 687.52 | 1172.39 | 743.98 | 4.50 |
| General Basic Offer Popup Variant Expansion pass | 191 | 5 | Button Text | 1 | 1365.48 | 689.26 | 1365.48 | 741.14 | 1370.67 | 689.26 | 1370.67 | 741.14 | 5.19 |
| General Basic Offer Popup Variant Premium_Booster_avatar_cardback_title | 115 | 7 | text | 1 | 836.36 | 687.52 | 928.58 | 743.98 | 840.86 | 687.52 | 933.08 | 743.98 | 4.50 |
| General Basic Offer Popup Variant Premium_Booster_avatar_cardback_title | 123 | 5 | Button Text | 1 | 1142.31 | 689.26 | 1142.31 | 741.14 | 1147.50 | 689.26 | 1147.50 | 741.14 | 5.19 |
| General Basic Offer Popup Variant Premium_Booster_avatar_cardback_title_resource | 115 | 7 | text | 1 | 1188.39 | 689.69 | 1312.89 | 746.15 | 1192.89 | 689.69 | 1317.39 | 746.15 | 4.50 |
| General Basic Offer Popup Variant Premium_Booster_avatar_cardback_title_resource | 123 | 5 | Button Text | 1 | 1444.48 | 691.43 | 1576.48 | 743.31 | 1449.67 | 691.43 | 1581.67 | 743.31 | 5.19 |
| General Basic Offer Popup Variant Premium_Premium_cardback_avatar | 88 | 7 | text | 1 | 1059.53 | 687.52 | 1151.75 | 743.98 | 1064.03 | 687.52 | 1156.25 | 743.98 | 4.50 |
| General Basic Offer Popup Variant Premium_Premium_cardback_avatar | 96 | 5 | Button Text | 1 | 1299.48 | 689.26 | 1431.48 | 741.14 | 1304.67 | 689.26 | 1436.67 | 741.14 | 5.19 |
| General Basic Offer Popup Variant Premium_Resource | 64 | 7 | text | 1 | 1249.64 | 687.52 | 1249.64 | 743.98 | 1254.14 | 687.52 | 1254.14 | 743.98 | 4.50 |
| General Basic Offer Popup Variant Premium_Resource | 72 | 5 | Button Text | 1 | 1509.48 | 689.26 | 1509.48 | 741.14 | 1514.67 | 689.26 | 1514.67 | 741.14 | 5.19 |
| General Basic Offer Popup Variant Single Item Type | 26 | 7 | text | 1 | 1105.64 | 687.52 | 1105.64 | 743.98 | 1110.14 | 687.52 | 1110.14 | 743.98 | 4.50 |
| General Basic Offer Popup Variant Single Item Type | 34 | 5 | Button Text | 1 | 1365.48 | 689.26 | 1365.48 | 741.14 | 1370.67 | 689.26 | 1370.67 | 741.14 | 5.19 |
| General Basic Offer Popup Variant avatarOrTitle_resource | 80 | 7 | text | 1 | 1059.53 | 687.52 | 1151.75 | 743.98 | 1064.03 | 687.52 | 1156.25 | 743.98 | 4.50 |
| General Basic Offer Popup Variant avatarOrTitle_resource | 88 | 5 | Button Text | 1 | 1365.48 | 689.26 | 1365.48 | 741.14 | 1370.67 | 689.26 | 1370.67 | 741.14 | 5.19 |
| Item Shop Tab | 18 | 10 | icon | 1.2 | 162.10 | 1080.62 | 195.17 | 1080.82 | 165.41 | 1080.62 | 198.47 | 1080.82 | 3.31 |
| Item Shop Tab | 19 | 10 | text | 1 | 195.17 | 1080.62 | 287.39 | 1080.82 | 201.78 | 1080.62 | 294.00 | 1080.82 | 6.61 |
| Item Shop Tab No Automatic Ordering | 18 | 10 | icon | 1.2 | 162.10 | 1080.62 | 195.17 | 1080.82 | 165.41 | 1080.62 | 198.47 | 1080.82 | 3.31 |
| Item Shop Tab No Automatic Ordering | 19 | 10 | text | 1 | 195.17 | 1080.62 | 287.39 | 1080.82 | 201.78 | 1080.62 | 294.00 | 1080.82 | 6.61 |
| Missions Tab | 2 | 2 | Special Missions | 1.15 | 205.20 | 24.91 | 964.70 | 581.13 | 205.20 | 24.91 | 865.63 | 581.13 | 99.07 |
| Missions Tab | 136 | 2 | Daily Missions | 1.15 | 964.70 | 24.91 | 1724.20 | 580.78 | 964.70 | 24.91 | 1625.13 | 580.78 | 99.07 |
| Missions Tab | 137 | 3 | Daily Missions Holder | 1 | 964.70 | 79.34 | 1724.20 | 580.78 | 964.70 | 79.34 | 1625.13 | 580.78 | 99.07 |
| Missions Tab | 138 | 4 | Daily Mission Container | 1 | 964.70 | 93.68 | 1724.20 | 243.68 | 964.70 | 93.68 | 1625.13 | 243.68 | 99.07 |
| Missions Tab | 139 | 5 | title | 1 | 1101.38 | 106.43 | 1714.42 | 168.68 | 1101.38 | 106.43 | 1616.67 | 168.68 | 97.75 |
| Missions Tab | 140 | 5 | description | 1 | 1101.37 | 106.43 | 1714.39 | 168.68 | 1101.37 | 106.43 | 1616.64 | 168.68 | 97.75 |
| Missions Tab | 141 | 5 | timer | 1 | 1101.38 | 106.43 | 1593.78 | 168.68 | 1101.38 | 106.43 | 1494.72 | 168.68 | 99.07 |
| Missions Tab | 160 | 5 | Mission Milestones Progress Bar | 1 | 1101.37 | 171.79 | 1264.06 | 223.62 | 1101.37 | 171.79 | 1232.76 | 223.62 | 31.30 |
| Missions Tab | 161 | 6 | Progress Bar | 1 | 1106.87 | 212.02 | 1264.06 | 223.62 | 1106.87 | 212.02 | 1232.76 | 223.62 | 31.30 |
| Missions Tab | 162 | 7 | Background | 1 | 1106.87 | 212.02 | 1264.06 | 223.62 | 1106.87 | 212.02 | 1232.76 | 223.62 | 31.30 |
| Missions Tab | 164 | 7 | Handle Slide Area | 1 | 1106.87 | 212.02 | 1254.06 | 223.62 | 1106.87 | 212.02 | 1222.76 | 223.62 | 31.30 |
| Missions Tab | 166 | 6 | progress | 1 | 1101.37 | 168.79 | 1264.06 | 209.52 | 1101.37 | 168.79 | 1232.76 | 209.52 | 31.30 |
| Missions Tab | 167 | 5 | Generic UI Button | 1 | 1451.59 | 174.73 | 1706.20 | 231.21 | 1352.53 | 174.73 | 1607.14 | 231.21 | 99.07 |
| Missions Tab | 168 | 6 | Button Text | 1 | 1458.59 | 174.73 | 1699.20 | 231.21 | 1359.53 | 174.73 | 1600.14 | 231.21 | 99.07 |
| Missions Tab | 169 | 5 | Trash mission | 1 | 1392.21 | 178.28 | 1441.31 | 227.65 | 1293.14 | 178.28 | 1342.24 | 227.65 | 99.07 |
| Missions Tab | 170 | 6 | Button Text | 1 | 1400.21 | 178.28 | 1433.31 | 227.65 | 1301.14 | 178.28 | 1334.24 | 227.65 | 99.07 |
| Missions Tab | 171 | 6 | Image | 1 | 1392.21 | 179.28 | 1439.31 | 226.65 | 1293.14 | 179.28 | 1340.24 | 226.65 | 99.07 |
| Missions Tab | 172 | 5 | Mission Debug Buttons | 1 | 1093.97 | 79.05 | 1881.95 | 112.71 | 1044.44 | 79.05 | 1832.42 | 112.71 | 49.53 |
| Missions Tab | 173 | 6 | Reset | 1 | 1093.97 | 79.05 | 1197.24 | 112.71 | 1044.44 | 79.05 | 1147.71 | 112.71 | 49.53 |
| Missions Tab | 174 | 7 | Button Text | 1 | 1106.97 | 79.05 | 1184.24 | 112.71 | 1057.44 | 79.05 | 1134.71 | 112.71 | 49.53 |
| Missions Tab | 175 | 6 | Re-Roll | 1 | 1197.24 | 79.05 | 1300.51 | 112.71 | 1147.71 | 79.05 | 1250.98 | 112.71 | 49.53 |
| Missions Tab | 176 | 7 | Button Text | 1 | 1210.24 | 79.05 | 1287.51 | 112.71 | 1160.71 | 79.05 | 1237.98 | 112.71 | 49.53 |
| Missions Tab | 177 | 6 | GameObject | 1 | 1300.51 | 79.05 | 1403.78 | 112.71 | 1250.98 | 79.05 | 1354.25 | 112.71 | 49.53 |
| Missions Tab | 178 | 7 | Increase one | 1 | 1300.51 | 79.05 | 1352.15 | 112.71 | 1250.98 | 79.05 | 1302.62 | 112.71 | 49.53 |
| Missions Tab | 179 | 8 | Button Text | 1 | 1313.51 | 79.05 | 1339.15 | 112.71 | 1263.98 | 79.05 | 1289.62 | 112.71 | 49.53 |
| Missions Tab | 180 | 7 | Increase half | 1 | 1352.15 | 79.05 | 1403.78 | 112.71 | 1302.62 | 79.05 | 1354.25 | 112.71 | 49.53 |
| Missions Tab | 181 | 8 | Button Text | 1 | 1365.15 | 79.05 | 1390.78 | 112.71 | 1315.62 | 79.05 | 1341.25 | 112.71 | 49.53 |
| Missions Tab | 182 | 6 | Complete | 1 | 1403.78 | 79.05 | 1552.73 | 112.71 | 1354.25 | 79.05 | 1503.20 | 112.71 | 49.53 |
| Missions Tab | 183 | 7 | Button Text | 1 | 1416.78 | 79.05 | 1539.73 | 112.71 | 1367.25 | 79.05 | 1490.20 | 112.71 | 49.53 |
| Missions Tab | 184 | 6 | GameObject (1) | 1 | 1552.73 | 79.05 | 1673.79 | 112.71 | 1503.20 | 79.05 | 1624.26 | 112.71 | 49.53 |
| Missions Tab | 185 | 7 | Text (TMP) | 1 | 1552.73 | 79.05 | 1673.79 | 112.71 | 1503.20 | 79.05 | 1624.26 | 112.71 | 49.53 |
| Missions Tab | 186 | 4 | Daily Mission Container (1) | 1 | 964.70 | 262.23 | 1724.20 | 412.23 | 964.70 | 262.23 | 1625.13 | 412.23 | 99.07 |
| Missions Tab | 187 | 5 | title | 1 | 1101.38 | 274.98 | 1714.42 | 337.23 | 1101.38 | 274.98 | 1616.67 | 337.23 | 97.75 |
| Missions Tab | 188 | 5 | description | 1 | 1101.37 | 274.98 | 1714.39 | 337.23 | 1101.37 | 274.98 | 1616.64 | 337.23 | 97.75 |
| Missions Tab | 189 | 5 | timer | 1 | 1101.38 | 274.98 | 1593.78 | 337.23 | 1101.38 | 274.98 | 1494.72 | 337.23 | 99.07 |
| Missions Tab | 208 | 5 | Mission Milestones Progress Bar | 1 | 1101.37 | 340.34 | 1264.06 | 392.17 | 1101.37 | 340.34 | 1232.76 | 392.17 | 31.30 |
| Missions Tab | 209 | 6 | Progress Bar | 1 | 1106.87 | 380.57 | 1264.06 | 392.17 | 1106.87 | 380.57 | 1232.76 | 392.17 | 31.30 |
| Missions Tab | 210 | 7 | Background | 1 | 1106.87 | 380.57 | 1264.06 | 392.17 | 1106.87 | 380.57 | 1232.76 | 392.17 | 31.30 |
| Missions Tab | 212 | 7 | Handle Slide Area | 1 | 1106.87 | 380.57 | 1254.06 | 392.17 | 1106.87 | 380.57 | 1222.76 | 392.17 | 31.30 |
| Missions Tab | 214 | 6 | progress | 1 | 1101.37 | 337.34 | 1264.06 | 378.07 | 1101.37 | 337.34 | 1232.76 | 378.07 | 31.30 |
| Missions Tab | 215 | 5 | Generic UI Button | 1 | 1451.59 | 343.28 | 1706.20 | 399.76 | 1352.53 | 343.28 | 1607.14 | 399.76 | 99.07 |
| Missions Tab | 216 | 6 | Button Text | 1 | 1458.59 | 343.28 | 1699.20 | 399.76 | 1359.53 | 343.28 | 1600.14 | 399.76 | 99.07 |
| Missions Tab | 217 | 5 | Trash mission | 1 | 1392.21 | 346.83 | 1441.31 | 396.20 | 1293.14 | 346.83 | 1342.24 | 396.20 | 99.07 |
| Missions Tab | 218 | 6 | Button Text | 1 | 1400.21 | 346.83 | 1433.31 | 396.20 | 1301.14 | 346.83 | 1334.24 | 396.20 | 99.07 |
| Missions Tab | 219 | 6 | Image | 1 | 1392.21 | 347.83 | 1439.31 | 395.20 | 1293.14 | 347.83 | 1340.24 | 395.20 | 99.07 |
| Missions Tab | 220 | 5 | Mission Debug Buttons | 1 | 1093.97 | 247.60 | 1881.95 | 281.26 | 1044.44 | 247.60 | 1832.42 | 281.26 | 49.53 |
| Missions Tab | 221 | 6 | Reset | 1 | 1093.97 | 247.60 | 1197.24 | 281.26 | 1044.44 | 247.60 | 1147.71 | 281.26 | 49.53 |
| Missions Tab | 222 | 7 | Button Text | 1 | 1106.97 | 247.60 | 1184.24 | 281.26 | 1057.44 | 247.60 | 1134.71 | 281.26 | 49.53 |
| Missions Tab | 223 | 6 | Re-Roll | 1 | 1197.24 | 247.60 | 1300.51 | 281.26 | 1147.71 | 247.60 | 1250.98 | 281.26 | 49.53 |
| Missions Tab | 224 | 7 | Button Text | 1 | 1210.24 | 247.60 | 1287.51 | 281.26 | 1160.71 | 247.60 | 1237.98 | 281.26 | 49.53 |
| Missions Tab | 225 | 6 | GameObject | 1 | 1300.51 | 247.60 | 1403.78 | 281.26 | 1250.98 | 247.60 | 1354.25 | 281.26 | 49.53 |
| Missions Tab | 226 | 7 | Increase one | 1 | 1300.51 | 247.60 | 1352.15 | 281.26 | 1250.98 | 247.60 | 1302.62 | 281.26 | 49.53 |
| Missions Tab | 227 | 8 | Button Text | 1 | 1313.51 | 247.60 | 1339.15 | 281.26 | 1263.98 | 247.60 | 1289.62 | 281.26 | 49.53 |
| Missions Tab | 228 | 7 | Increase half | 1 | 1352.15 | 247.60 | 1403.78 | 281.26 | 1302.62 | 247.60 | 1354.25 | 281.26 | 49.53 |
| Missions Tab | 229 | 8 | Button Text | 1 | 1365.15 | 247.60 | 1390.78 | 281.26 | 1315.62 | 247.60 | 1341.25 | 281.26 | 49.53 |
| Missions Tab | 230 | 6 | Complete | 1 | 1403.78 | 247.60 | 1552.73 | 281.26 | 1354.25 | 247.60 | 1503.20 | 281.26 | 49.53 |
| Missions Tab | 231 | 7 | Button Text | 1 | 1416.78 | 247.60 | 1539.73 | 281.26 | 1367.25 | 247.60 | 1490.20 | 281.26 | 49.53 |
| Missions Tab | 232 | 6 | GameObject (1) | 1 | 1552.73 | 247.60 | 1673.79 | 281.26 | 1503.20 | 247.60 | 1624.26 | 281.26 | 49.53 |
| Missions Tab | 233 | 7 | Text (TMP) | 1 | 1552.73 | 247.60 | 1673.79 | 281.26 | 1503.20 | 247.60 | 1624.26 | 281.26 | 49.53 |
| Missions Tab | 234 | 4 | Daily Mission Container (2) | 1 | 964.70 | 430.78 | 1724.20 | 580.78 | 964.70 | 430.78 | 1625.13 | 580.78 | 99.07 |
| Missions Tab | 235 | 5 | title | 1 | 1101.38 | 443.53 | 1714.42 | 505.78 | 1101.38 | 443.53 | 1616.67 | 505.78 | 97.75 |
| Missions Tab | 236 | 5 | description | 1 | 1101.37 | 443.53 | 1714.39 | 505.78 | 1101.37 | 443.53 | 1616.64 | 505.78 | 97.75 |
| Missions Tab | 237 | 5 | timer | 1 | 1101.38 | 443.53 | 1593.78 | 505.78 | 1101.38 | 443.53 | 1494.72 | 505.78 | 99.07 |
| Missions Tab | 256 | 5 | Mission Milestones Progress Bar | 1 | 1101.37 | 508.89 | 1264.06 | 560.72 | 1101.37 | 508.89 | 1232.76 | 560.72 | 31.30 |
| Missions Tab | 257 | 6 | Progress Bar | 1 | 1106.87 | 549.12 | 1264.06 | 560.72 | 1106.87 | 549.12 | 1232.76 | 560.72 | 31.30 |
| Missions Tab | 258 | 7 | Background | 1 | 1106.87 | 549.12 | 1264.06 | 560.72 | 1106.87 | 549.12 | 1232.76 | 560.72 | 31.30 |
| Missions Tab | 260 | 7 | Handle Slide Area | 1 | 1106.87 | 549.12 | 1254.06 | 560.72 | 1106.87 | 549.12 | 1222.76 | 560.72 | 31.30 |
| Missions Tab | 262 | 6 | progress | 1 | 1101.37 | 505.89 | 1264.06 | 546.62 | 1101.37 | 505.89 | 1232.76 | 546.62 | 31.30 |
| Missions Tab | 263 | 5 | Generic UI Button | 1 | 1451.59 | 511.83 | 1706.20 | 568.31 | 1352.53 | 511.83 | 1607.14 | 568.31 | 99.07 |
| Missions Tab | 264 | 6 | Button Text | 1 | 1458.59 | 511.83 | 1699.20 | 568.31 | 1359.53 | 511.83 | 1600.14 | 568.31 | 99.07 |
| Missions Tab | 265 | 5 | Trash mission | 1 | 1392.21 | 515.38 | 1441.31 | 564.75 | 1293.14 | 515.38 | 1342.24 | 564.75 | 99.07 |
| Missions Tab | 266 | 6 | Button Text | 1 | 1400.21 | 515.38 | 1433.31 | 564.75 | 1301.14 | 515.38 | 1334.24 | 564.75 | 99.07 |
| Missions Tab | 267 | 6 | Image | 1 | 1392.21 | 516.38 | 1439.31 | 563.75 | 1293.14 | 516.38 | 1340.24 | 563.75 | 99.07 |
| Missions Tab | 268 | 5 | Mission Debug Buttons | 1 | 1093.97 | 416.15 | 1881.95 | 449.81 | 1044.44 | 416.15 | 1832.42 | 449.81 | 49.53 |
| Missions Tab | 269 | 6 | Reset | 1 | 1093.97 | 416.15 | 1197.24 | 449.81 | 1044.44 | 416.15 | 1147.71 | 449.81 | 49.53 |
| Missions Tab | 270 | 7 | Button Text | 1 | 1106.97 | 416.15 | 1184.24 | 449.81 | 1057.44 | 416.15 | 1134.71 | 449.81 | 49.53 |
| Missions Tab | 271 | 6 | Re-Roll | 1 | 1197.24 | 416.15 | 1300.51 | 449.81 | 1147.71 | 416.15 | 1250.98 | 449.81 | 49.53 |
| Missions Tab | 272 | 7 | Button Text | 1 | 1210.24 | 416.15 | 1287.51 | 449.81 | 1160.71 | 416.15 | 1237.98 | 449.81 | 49.53 |
| Missions Tab | 273 | 6 | GameObject | 1 | 1300.51 | 416.15 | 1403.78 | 449.81 | 1250.98 | 416.15 | 1354.25 | 449.81 | 49.53 |
| Missions Tab | 274 | 7 | Increase one | 1 | 1300.51 | 416.15 | 1352.15 | 449.81 | 1250.98 | 416.15 | 1302.62 | 449.81 | 49.53 |
| Missions Tab | 275 | 8 | Button Text | 1 | 1313.51 | 416.15 | 1339.15 | 449.81 | 1263.98 | 416.15 | 1289.62 | 449.81 | 49.53 |
| Missions Tab | 276 | 7 | Increase half | 1 | 1352.15 | 416.15 | 1403.78 | 449.81 | 1302.62 | 416.15 | 1354.25 | 449.81 | 49.53 |
| Missions Tab | 277 | 8 | Button Text | 1 | 1365.15 | 416.15 | 1390.78 | 449.81 | 1315.62 | 416.15 | 1341.25 | 449.81 | 49.53 |
| Missions Tab | 278 | 6 | Complete | 1 | 1403.78 | 416.15 | 1552.73 | 449.81 | 1354.25 | 416.15 | 1503.20 | 449.81 | 49.53 |
| Missions Tab | 279 | 7 | Button Text | 1 | 1416.78 | 416.15 | 1539.73 | 449.81 | 1367.25 | 416.15 | 1490.20 | 449.81 | 49.53 |
| Missions Tab | 280 | 6 | GameObject (1) | 1 | 1552.73 | 416.15 | 1673.79 | 449.81 | 1503.20 | 416.15 | 1624.26 | 449.81 | 49.53 |
| Missions Tab | 281 | 7 | Text (TMP) | 1 | 1552.73 | 416.15 | 1673.79 | 449.81 | 1503.20 | 416.15 | 1624.26 | 449.81 | 49.53 |
| Missions Tab | 282 | 3 | Mission Header | 1 | 967.38 | 27.14 | 1724.20 | 79.91 | 967.38 | 27.14 | 1625.13 | 79.91 | 99.07 |
| Missions Tab | 283 | 4 | info | 1 | 1683.20 | 33.02 | 1724.20 | 74.02 | 1584.13 | 33.02 | 1625.13 | 74.02 | 99.07 |
| Missions Tab | 284 | 4 | name | 1 | 990.08 | 28.52 | 1603.11 | 78.52 | 987.11 | 28.52 | 1519.89 | 78.52 | 83.21 |
| Missions Tab | 285 | 4 | Refill Counter | 1 | 967.38 | 28.52 | 1670.34 | 78.52 | 967.38 | 28.52 | 1571.28 | 78.52 | 99.07 |
| Offer Container | 12 | 6 | text | 1 | -71.31 | 1417.26 | 20.91 | 1457.34 | -67.05 | 1417.26 | 25.17 | 1457.34 | 4.26 |
| Player Profile Window | 208 | 9 | text | 1 | 938.55 | 586.77 | 1030.77 | 638.24 | 943.70 | 586.77 | 1035.92 | 638.24 | 5.15 |
| Price Display Button | 5 | 3 | text | 1 | -15.57 | 1061.17 | -15.57 | 1099.58 | -11.76 | 1061.17 | -11.76 | 1099.58 | 3.81 |
| Raid Progress Bar | 6 | 2 | icon | 1.2 | 788.58 | 822.47 | 825.82 | 859.71 | 792.30 | 822.47 | 829.54 | 859.71 | 3.72 |
| Raid Progress Bar | 7 | 2 | text | 1 | 825.82 | 822.47 | 934.81 | 859.71 | 833.27 | 822.47 | 942.26 | 859.71 | 7.45 |
| Raid Reward Container | 10 | 4 | icon | 1.2 | -186.28 | 702.05 | -149.04 | 739.29 | -182.56 | 702.05 | -145.32 | 739.29 | 3.72 |
| Raid Reward Container | 11 | 4 | text | 1 | -149.04 | 702.05 | -40.05 | 739.29 | -141.59 | 702.05 | -32.60 | 739.29 | 7.45 |
| Raid Reward Container | 21 | 4 | icon | 1.2 | -187.70 | 1301.27 | -150.46 | 1338.51 | -183.98 | 1301.27 | -146.74 | 1338.51 | 3.72 |
| Raid Reward Container | 22 | 4 | text | 1 | -150.46 | 1301.27 | -41.47 | 1338.51 | -143.02 | 1301.27 | -34.03 | 1338.51 | 7.45 |
| Raid Reward Container | 26 | 4 | text | 1 | -43.27 | 1215.40 | 81.23 | 1215.40 | -39.26 | 1215.40 | 85.24 | 1215.40 | 4.01 |
| Raid Reward Container | 32 | 6 | text | 1 | -43.37 | 1254.16 | 81.13 | 1317.07 | -39.20 | 1254.16 | 85.30 | 1317.07 | 4.16 |
| ReRollPopup Variant | 14 | 6 | icon | 1.2 | 1141.25 | 551.00 | 1197.25 | 607.00 | 1146.85 | 551.00 | 1202.85 | 607.00 | 5.60 |
| ReRollPopup Variant | 15 | 6 | text | 1 | 1202.75 | 551.00 | 1202.75 | 607.00 | 1213.95 | 551.00 | 1213.95 | 607.00 | 11.20 |
| Referral Container | 12 | 6 | text | 1 | -27.90 | 1223.42 | -27.90 | 1263.00 | -24.19 | 1223.42 | -24.19 | 1263.00 | 3.71 |
| Rewards Base Submenu Variant | 36 | 5 | Special Missions | 1.15 | 372.37 | 95.85 | 1131.87 | 652.07 | 372.37 | 95.85 | 1032.81 | 652.07 | 99.07 |
| Rewards Base Submenu Variant | 134 | 5 | Daily Missions | 1.15 | 1131.87 | 95.85 | 1891.37 | 651.72 | 1131.87 | 95.85 | 1792.31 | 651.72 | 99.07 |
| Rewards Base Submenu Variant | 135 | 6 | Daily Missions Holder | 1 | 1131.87 | 150.28 | 1891.37 | 651.72 | 1131.87 | 150.28 | 1792.31 | 651.72 | 99.07 |
| Rewards Base Submenu Variant | 136 | 7 | Daily Mission Container | 1 | 1131.87 | 164.62 | 1891.37 | 314.62 | 1131.87 | 164.62 | 1792.31 | 314.62 | 99.07 |
| Rewards Base Submenu Variant | 137 | 8 | title | 1 | 1268.55 | 177.37 | 1881.59 | 239.62 | 1268.55 | 177.37 | 1783.85 | 239.62 | 97.75 |
| Rewards Base Submenu Variant | 138 | 8 | description | 1 | 1268.55 | 177.37 | 1881.56 | 239.62 | 1268.55 | 177.37 | 1783.82 | 239.62 | 97.75 |
| Rewards Base Submenu Variant | 139 | 8 | timer | 1 | 1268.55 | 177.37 | 1760.96 | 239.62 | 1268.55 | 177.37 | 1661.89 | 239.62 | 99.07 |
| Rewards Base Submenu Variant | 147 | 8 | Mission Milestones Progress Bar | 1 | 1268.55 | 242.73 | 1431.24 | 294.56 | 1268.55 | 242.73 | 1399.93 | 294.56 | 31.30 |
| Rewards Base Submenu Variant | 148 | 9 | Progress Bar | 1 | 1274.05 | 282.96 | 1431.24 | 294.56 | 1274.05 | 282.96 | 1399.93 | 294.56 | 31.30 |
| Rewards Base Submenu Variant | 149 | 10 | Background | 1 | 1274.05 | 282.96 | 1431.24 | 294.56 | 1274.05 | 282.96 | 1399.93 | 294.56 | 31.30 |
| Rewards Base Submenu Variant | 151 | 10 | Handle Slide Area | 1 | 1274.05 | 282.96 | 1421.24 | 294.56 | 1274.05 | 282.96 | 1389.93 | 294.56 | 31.30 |
| Rewards Base Submenu Variant | 153 | 9 | progress | 1 | 1268.55 | 239.73 | 1431.24 | 280.46 | 1268.55 | 239.73 | 1399.93 | 280.46 | 31.30 |
| Rewards Base Submenu Variant | 154 | 8 | Generic UI Button | 1 | 1618.77 | 245.67 | 1873.38 | 302.15 | 1519.70 | 245.67 | 1774.31 | 302.15 | 99.07 |
| Rewards Base Submenu Variant | 155 | 9 | Button Text | 1 | 1625.77 | 245.67 | 1866.38 | 302.15 | 1526.70 | 245.67 | 1767.31 | 302.15 | 99.07 |
| Rewards Base Submenu Variant | 156 | 8 | Trash mission | 1 | 1559.38 | 249.22 | 1608.48 | 298.59 | 1460.31 | 249.22 | 1509.42 | 298.59 | 99.07 |
| Rewards Base Submenu Variant | 157 | 9 | Button Text | 1 | 1567.38 | 249.22 | 1600.48 | 298.59 | 1468.31 | 249.22 | 1501.42 | 298.59 | 99.07 |
| Rewards Base Submenu Variant | 158 | 9 | Image | 1 | 1559.38 | 250.22 | 1606.48 | 297.59 | 1460.31 | 250.22 | 1507.42 | 297.59 | 99.07 |
| Rewards Base Submenu Variant | 159 | 8 | Mission Debug Buttons | 1 | 1261.15 | 149.99 | 2049.13 | 183.65 | 1211.62 | 149.99 | 1999.60 | 183.65 | 49.53 |
| Rewards Base Submenu Variant | 160 | 9 | Reset | 1 | 1261.15 | 149.99 | 1364.42 | 183.65 | 1211.62 | 149.99 | 1314.89 | 183.65 | 49.53 |
| Rewards Base Submenu Variant | 161 | 10 | Button Text | 1 | 1274.15 | 149.99 | 1351.42 | 183.65 | 1224.62 | 149.99 | 1301.89 | 183.65 | 49.53 |
| Rewards Base Submenu Variant | 162 | 9 | Re-Roll | 1 | 1364.42 | 149.99 | 1467.69 | 183.65 | 1314.89 | 149.99 | 1418.16 | 183.65 | 49.53 |
| Rewards Base Submenu Variant | 163 | 10 | Button Text | 1 | 1377.42 | 149.99 | 1454.69 | 183.65 | 1327.89 | 149.99 | 1405.16 | 183.65 | 49.53 |
| Rewards Base Submenu Variant | 164 | 9 | GameObject | 1 | 1467.69 | 149.99 | 1570.96 | 183.65 | 1418.16 | 149.99 | 1521.43 | 183.65 | 49.53 |
| Rewards Base Submenu Variant | 165 | 10 | Increase one | 1 | 1467.69 | 149.99 | 1519.32 | 183.65 | 1418.16 | 149.99 | 1469.79 | 183.65 | 49.53 |
| Rewards Base Submenu Variant | 166 | 11 | Button Text | 1 | 1480.69 | 149.99 | 1506.32 | 183.65 | 1431.16 | 149.99 | 1456.79 | 183.65 | 49.53 |
| Rewards Base Submenu Variant | 167 | 10 | Increase half | 1 | 1519.32 | 149.99 | 1570.96 | 183.65 | 1469.79 | 149.99 | 1521.43 | 183.65 | 49.53 |
| Rewards Base Submenu Variant | 168 | 11 | Button Text | 1 | 1532.32 | 149.99 | 1557.96 | 183.65 | 1482.79 | 149.99 | 1508.43 | 183.65 | 49.53 |
| Rewards Base Submenu Variant | 169 | 9 | Complete | 1 | 1570.96 | 149.99 | 1719.91 | 183.65 | 1521.43 | 149.99 | 1670.38 | 183.65 | 49.53 |
| Rewards Base Submenu Variant | 170 | 10 | Button Text | 1 | 1583.96 | 149.99 | 1706.91 | 183.65 | 1534.43 | 149.99 | 1657.38 | 183.65 | 49.53 |
| Rewards Base Submenu Variant | 171 | 9 | GameObject (1) | 1 | 1719.91 | 149.99 | 1840.97 | 183.65 | 1670.38 | 149.99 | 1791.44 | 183.65 | 49.53 |
| Rewards Base Submenu Variant | 172 | 10 | Text (TMP) | 1 | 1719.91 | 149.99 | 1840.97 | 183.65 | 1670.38 | 149.99 | 1791.44 | 183.65 | 49.53 |
| Rewards Base Submenu Variant | 173 | 7 | Daily Mission Container (1) | 1 | 1131.87 | 333.17 | 1891.37 | 483.17 | 1131.87 | 333.17 | 1792.31 | 483.17 | 99.07 |
| Rewards Base Submenu Variant | 174 | 8 | title | 1 | 1268.55 | 345.92 | 1881.59 | 408.17 | 1268.55 | 345.92 | 1783.85 | 408.17 | 97.75 |
| Rewards Base Submenu Variant | 175 | 8 | description | 1 | 1268.55 | 345.92 | 1881.56 | 408.17 | 1268.55 | 345.92 | 1783.82 | 408.17 | 97.75 |
| Rewards Base Submenu Variant | 176 | 8 | timer | 1 | 1268.55 | 345.92 | 1760.96 | 408.17 | 1268.55 | 345.92 | 1661.89 | 408.17 | 99.07 |
| Rewards Base Submenu Variant | 184 | 8 | Mission Milestones Progress Bar | 1 | 1268.55 | 411.28 | 1431.24 | 463.11 | 1268.55 | 411.28 | 1399.93 | 463.11 | 31.30 |
| Rewards Base Submenu Variant | 185 | 9 | Progress Bar | 1 | 1274.05 | 451.51 | 1431.24 | 463.11 | 1274.05 | 451.51 | 1399.93 | 463.11 | 31.30 |
| Rewards Base Submenu Variant | 186 | 10 | Background | 1 | 1274.05 | 451.51 | 1431.24 | 463.11 | 1274.05 | 451.51 | 1399.93 | 463.11 | 31.30 |
| Rewards Base Submenu Variant | 188 | 10 | Handle Slide Area | 1 | 1274.05 | 451.51 | 1421.24 | 463.11 | 1274.05 | 451.51 | 1389.93 | 463.11 | 31.30 |
| Rewards Base Submenu Variant | 190 | 9 | progress | 1 | 1268.55 | 408.28 | 1431.24 | 449.01 | 1268.55 | 408.28 | 1399.93 | 449.01 | 31.30 |
| Rewards Base Submenu Variant | 191 | 8 | Generic UI Button | 1 | 1618.77 | 414.22 | 1873.38 | 470.70 | 1519.70 | 414.22 | 1774.31 | 470.70 | 99.07 |
| Rewards Base Submenu Variant | 192 | 9 | Button Text | 1 | 1625.77 | 414.22 | 1866.38 | 470.70 | 1526.70 | 414.22 | 1767.31 | 470.70 | 99.07 |
| Rewards Base Submenu Variant | 193 | 8 | Trash mission | 1 | 1559.38 | 417.77 | 1608.48 | 467.14 | 1460.31 | 417.77 | 1509.42 | 467.14 | 99.07 |
| Rewards Base Submenu Variant | 194 | 9 | Button Text | 1 | 1567.38 | 417.77 | 1600.48 | 467.14 | 1468.31 | 417.77 | 1501.42 | 467.14 | 99.07 |
| Rewards Base Submenu Variant | 195 | 9 | Image | 1 | 1559.38 | 418.77 | 1606.48 | 466.14 | 1460.31 | 418.77 | 1507.42 | 466.14 | 99.07 |
| Rewards Base Submenu Variant | 196 | 8 | Mission Debug Buttons | 1 | 1261.15 | 318.54 | 2049.13 | 352.20 | 1211.62 | 318.54 | 1999.60 | 352.20 | 49.53 |
| Rewards Base Submenu Variant | 197 | 9 | Reset | 1 | 1261.15 | 318.54 | 1364.42 | 352.20 | 1211.62 | 318.54 | 1314.89 | 352.20 | 49.53 |
| Rewards Base Submenu Variant | 198 | 10 | Button Text | 1 | 1274.15 | 318.54 | 1351.42 | 352.20 | 1224.62 | 318.54 | 1301.89 | 352.20 | 49.53 |
| Rewards Base Submenu Variant | 199 | 9 | Re-Roll | 1 | 1364.42 | 318.54 | 1467.69 | 352.20 | 1314.89 | 318.54 | 1418.16 | 352.20 | 49.53 |
| Rewards Base Submenu Variant | 200 | 10 | Button Text | 1 | 1377.42 | 318.54 | 1454.69 | 352.20 | 1327.89 | 318.54 | 1405.16 | 352.20 | 49.53 |
| Rewards Base Submenu Variant | 201 | 9 | GameObject | 1 | 1467.69 | 318.54 | 1570.96 | 352.20 | 1418.16 | 318.54 | 1521.43 | 352.20 | 49.53 |
| Rewards Base Submenu Variant | 202 | 10 | Increase one | 1 | 1467.69 | 318.54 | 1519.32 | 352.20 | 1418.16 | 318.54 | 1469.79 | 352.20 | 49.53 |
| Rewards Base Submenu Variant | 203 | 11 | Button Text | 1 | 1480.69 | 318.54 | 1506.32 | 352.20 | 1431.16 | 318.54 | 1456.79 | 352.20 | 49.53 |
| Rewards Base Submenu Variant | 204 | 10 | Increase half | 1 | 1519.32 | 318.54 | 1570.96 | 352.20 | 1469.79 | 318.54 | 1521.43 | 352.20 | 49.53 |
| Rewards Base Submenu Variant | 205 | 11 | Button Text | 1 | 1532.32 | 318.54 | 1557.96 | 352.20 | 1482.79 | 318.54 | 1508.43 | 352.20 | 49.53 |
| Rewards Base Submenu Variant | 206 | 9 | Complete | 1 | 1570.96 | 318.54 | 1719.91 | 352.20 | 1521.43 | 318.54 | 1670.38 | 352.20 | 49.53 |
| Rewards Base Submenu Variant | 207 | 10 | Button Text | 1 | 1583.96 | 318.54 | 1706.91 | 352.20 | 1534.43 | 318.54 | 1657.38 | 352.20 | 49.53 |
| Rewards Base Submenu Variant | 208 | 9 | GameObject (1) | 1 | 1719.91 | 318.54 | 1840.97 | 352.20 | 1670.38 | 318.54 | 1791.44 | 352.20 | 49.53 |
| Rewards Base Submenu Variant | 209 | 10 | Text (TMP) | 1 | 1719.91 | 318.54 | 1840.97 | 352.20 | 1670.38 | 318.54 | 1791.44 | 352.20 | 49.53 |
| Rewards Base Submenu Variant | 210 | 7 | Daily Mission Container (2) | 1 | 1131.87 | 501.72 | 1891.37 | 651.72 | 1131.87 | 501.72 | 1792.31 | 651.72 | 99.07 |
| Rewards Base Submenu Variant | 211 | 8 | title | 1 | 1268.55 | 514.47 | 1881.59 | 576.72 | 1268.55 | 514.47 | 1783.85 | 576.72 | 97.75 |
| Rewards Base Submenu Variant | 212 | 8 | description | 1 | 1268.55 | 514.47 | 1881.56 | 576.72 | 1268.55 | 514.47 | 1783.82 | 576.72 | 97.75 |
| Rewards Base Submenu Variant | 213 | 8 | timer | 1 | 1268.55 | 514.47 | 1760.96 | 576.72 | 1268.55 | 514.47 | 1661.89 | 576.72 | 99.07 |
| Rewards Base Submenu Variant | 221 | 8 | Mission Milestones Progress Bar | 1 | 1268.55 | 579.83 | 1431.24 | 631.66 | 1268.55 | 579.83 | 1399.93 | 631.66 | 31.30 |
| Rewards Base Submenu Variant | 222 | 9 | Progress Bar | 1 | 1274.05 | 620.06 | 1431.24 | 631.66 | 1274.05 | 620.06 | 1399.93 | 631.66 | 31.30 |
| Rewards Base Submenu Variant | 223 | 10 | Background | 1 | 1274.05 | 620.06 | 1431.24 | 631.66 | 1274.05 | 620.06 | 1399.93 | 631.66 | 31.30 |
| Rewards Base Submenu Variant | 225 | 10 | Handle Slide Area | 1 | 1274.05 | 620.06 | 1421.24 | 631.66 | 1274.05 | 620.06 | 1389.93 | 631.66 | 31.30 |
| Rewards Base Submenu Variant | 227 | 9 | progress | 1 | 1268.55 | 576.83 | 1431.24 | 617.56 | 1268.55 | 576.83 | 1399.93 | 617.56 | 31.30 |
| Rewards Base Submenu Variant | 228 | 8 | Generic UI Button | 1 | 1618.77 | 582.77 | 1873.38 | 639.25 | 1519.70 | 582.77 | 1774.31 | 639.25 | 99.07 |
| Rewards Base Submenu Variant | 229 | 9 | Button Text | 1 | 1625.77 | 582.77 | 1866.38 | 639.25 | 1526.70 | 582.77 | 1767.31 | 639.25 | 99.07 |
| Rewards Base Submenu Variant | 230 | 8 | Trash mission | 1 | 1559.38 | 586.32 | 1608.48 | 635.69 | 1460.31 | 586.32 | 1509.42 | 635.69 | 99.07 |
| Rewards Base Submenu Variant | 231 | 9 | Button Text | 1 | 1567.38 | 586.32 | 1600.48 | 635.69 | 1468.31 | 586.32 | 1501.42 | 635.69 | 99.07 |
| Rewards Base Submenu Variant | 232 | 9 | Image | 1 | 1559.38 | 587.32 | 1606.48 | 634.69 | 1460.31 | 587.32 | 1507.42 | 634.69 | 99.07 |
| Rewards Base Submenu Variant | 233 | 8 | Mission Debug Buttons | 1 | 1261.15 | 487.09 | 2049.13 | 520.75 | 1211.62 | 487.09 | 1999.60 | 520.75 | 49.53 |
| Rewards Base Submenu Variant | 234 | 9 | Reset | 1 | 1261.15 | 487.09 | 1364.42 | 520.75 | 1211.62 | 487.09 | 1314.89 | 520.75 | 49.53 |
| Rewards Base Submenu Variant | 235 | 10 | Button Text | 1 | 1274.15 | 487.09 | 1351.42 | 520.75 | 1224.62 | 487.09 | 1301.89 | 520.75 | 49.53 |
| Rewards Base Submenu Variant | 236 | 9 | Re-Roll | 1 | 1364.42 | 487.09 | 1467.69 | 520.75 | 1314.89 | 487.09 | 1418.16 | 520.75 | 49.53 |
| Rewards Base Submenu Variant | 237 | 10 | Button Text | 1 | 1377.42 | 487.09 | 1454.69 | 520.75 | 1327.89 | 487.09 | 1405.16 | 520.75 | 49.53 |
| Rewards Base Submenu Variant | 238 | 9 | GameObject | 1 | 1467.69 | 487.09 | 1570.96 | 520.75 | 1418.16 | 487.09 | 1521.43 | 520.75 | 49.53 |
| Rewards Base Submenu Variant | 239 | 10 | Increase one | 1 | 1467.69 | 487.09 | 1519.32 | 520.75 | 1418.16 | 487.09 | 1469.79 | 520.75 | 49.53 |
| Rewards Base Submenu Variant | 240 | 11 | Button Text | 1 | 1480.69 | 487.09 | 1506.32 | 520.75 | 1431.16 | 487.09 | 1456.79 | 520.75 | 49.53 |
| Rewards Base Submenu Variant | 241 | 10 | Increase half | 1 | 1519.32 | 487.09 | 1570.96 | 520.75 | 1469.79 | 487.09 | 1521.43 | 520.75 | 49.53 |
| Rewards Base Submenu Variant | 242 | 11 | Button Text | 1 | 1532.32 | 487.09 | 1557.96 | 520.75 | 1482.79 | 487.09 | 1508.43 | 520.75 | 49.53 |
| Rewards Base Submenu Variant | 243 | 9 | Complete | 1 | 1570.96 | 487.09 | 1719.91 | 520.75 | 1521.43 | 487.09 | 1670.38 | 520.75 | 49.53 |
| Rewards Base Submenu Variant | 244 | 10 | Button Text | 1 | 1583.96 | 487.09 | 1706.91 | 520.75 | 1534.43 | 487.09 | 1657.38 | 520.75 | 49.53 |
| Rewards Base Submenu Variant | 245 | 9 | GameObject (1) | 1 | 1719.91 | 487.09 | 1840.97 | 520.75 | 1670.38 | 487.09 | 1791.44 | 520.75 | 49.53 |
| Rewards Base Submenu Variant | 246 | 10 | Text (TMP) | 1 | 1719.91 | 487.09 | 1840.97 | 520.75 | 1670.38 | 487.09 | 1791.44 | 520.75 | 49.53 |
| Rewards Base Submenu Variant | 247 | 6 | Mission Header | 1 | 1134.55 | 98.08 | 1891.37 | 150.85 | 1134.55 | 98.08 | 1792.31 | 150.85 | 99.07 |
| Rewards Base Submenu Variant | 248 | 7 | info | 1 | 1850.37 | 103.96 | 1891.37 | 144.96 | 1751.31 | 103.96 | 1792.31 | 144.96 | 99.07 |
| Rewards Base Submenu Variant | 249 | 7 | name | 1 | 1157.26 | 99.46 | 1770.28 | 149.46 | 1154.28 | 99.46 | 1687.07 | 149.46 | 83.21 |
| Rewards Base Submenu Variant | 250 | 7 | Refill Counter | 1 | 1134.55 | 99.46 | 1837.52 | 149.46 | 1134.55 | 99.46 | 1738.45 | 149.46 | 99.07 |
| Shop Item Container | 12 | 6 | text | 1 | -67.99 | 1223.42 | 24.23 | 1263.00 | -63.07 | 1223.42 | 29.15 | 1263.00 | 4.92 |
| Small General Basic Offer Container Variant Single Item Type | 127 | 6 | text | 1 | -25.20 | 1223.79 | -25.20 | 1263.47 | -20.94 | 1223.79 | -20.94 | 1263.47 | 4.26 |
| Social Submenu Variant | 115 | 11 | text | 1 | 542.06 | 730.73 | 609.12 | 777.65 | 546.75 | 730.73 | 613.81 | 777.65 | 4.69 |
| Webshop Offer Container | 11 | 6 | icon | 1.2 | -167.00 | 1468.76 | -124.45 | 1471.24 | -162.74 | 1468.76 | -120.19 | 1471.24 | 4.26 |
| Webshop Offer Container | 12 | 6 | text | 1 | -124.45 | 1468.76 | -32.23 | 1471.24 | -115.94 | 1468.76 | -23.72 | 1471.24 | 8.51 |

（共 **274** 行）
