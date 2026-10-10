# F3 · `RewardsScene.Run` 两条红（`progress` / 登录卡 `count 0` 字号）—— 框量、框修、断言改写

> 执行代理 **F3** · 2026-10-10。白名单内只动了 **两个 `.cs`**：`Shell/MissionsTab.cs`（**+80 / −12**）·
> `Editor/RewardsScene.cs`（**+74 / −11**）。两文件**行尾仍是 LF**（`\r\n` 计数 = 0）。
> ⛔ 没跑 Unity / 自检 · ⛔ 没动 git · ⛔ 没碰两张正本 · ⛔ `d:/2/**` 只读（一个字节没写）· ⛔ 没碰 `工具/*.py`。
> 判据输入 = `D1_九条红诊断.md`（#5 / #6）· `RM_原版TMP框尺寸.md` · `PL_修尺子.md` · `RO_menu_dump尺子裁决.md`。

---

## 1 · 一句话结论

🔴 **两条红里只有 `#6`（登录卡 `count 0`）有真缺陷，而且 `D1` 猜的那两半里【错的是框、不是四格】**
—— 原版那颗 `count` 的 RT 是 **`aMin == aMax == (0,0)`**（它**没有锚点框**），框由父格子上的
`HorizontalLayoutGroup` 给，算出来**高 = 整格高 `89.29` 画布 px**；我们改前给的是 **`30.09`**
（错用了**另一个 prefab** `Reward Display Mission Vertical Variant` 的 `aMax.y = 0.337`）⇒ 差 **+59.20**。
**`#5`（`progress`）的框逐位正确、一个数都不用改**，红的原因纯粹是那条断言钉的是「TMP 的收敛字号」而该颗
`A1205` 起已开自适应（`D1` 判 `(γ)`+`(δ)`）。
⇒ 两笔：**框修 1 处（登录卡奖励格的内层几何，含同笔的图标框）** + **断言改写 2 条（改成窗口 + 关系式，两条都不拿收敛值当期望）**。

---

## 2 · 🔑「原版有效框 vs 我们传的框」对照表

> **怎么算的（三句，够判表里任何一行）**：
> ① 原版值**一律回 prefab 字段亲读**（`d:/2/新解包资源/assets_full/bundle_menus_assets_all/{RectTransform,MonoBehaviour}/*.json`），
> **不抄任何转述**；② 「有效框」= `锚跨 × 父【屏幕】框 + sizeDelta × lossyScale(父)`
> （🔴 **`m_SizeDelta` 只在 `anchorMin == anchorMax` 时才是框** —— `R-M` 那条口径，本表 4 行里有 2 行属于「没有锚点框」那一档）；
> ③ 解算后**用 `工具/menu_dump.py bundle_menus_assets_all "Missions Tab" --depth 12 --no-sprite` 复核**，
> 逐行与手算**逐位相同**（下面每行的「解算框」两列都标了来源）。
> ⚠️ **量纲**：表里「设计 px」= prefab 局部单位（= 我们 `R()` 之前的坐标系，两边同尺度）；
> 「画布 px」= 设计 px × 1.15（`Special Missions` / `Daily Missions` 的 `m_LocalScale`）。

| # | 颗（`文件:行号`） | 锚型 | 父（屏幕框 / 设计框） | 锚跨度 `ad` | `sd`（原版字段） | **原版解算框** | **我们传的框** | **差** |
|---|---|---|---|---|---|---|---|---|
| A | 每日行 `progress`<br>`Shell/MissionsTab.cs:606` | **伸锚** `aMin (0,0.33)` · `aMax (1,1)` · `piv (0,0)` | `Mission Milestones Progress Bar`：**59.60**（屏）/ **51.8301**（设计） | `(1, 0.67)` | `pos (0,−3)` · **`sd (0, 6)`** | **高 46.83 画布**（= `0.67 × 59.60 + 6 × 1.15`）<br>宽 = 父宽（伸锚） | 高 **46.835 画布**（= `(0.67 × 51.8301 + 6) × 1.15`） | **0.00 ✅** |
| B | 登录卡 `count 0`**（改前）**<br>`Shell/MissionsTab.cs:1543`（`BuildRewardCell`） | 我们**当成** `aMin (0,0)` · `aMax (1,0.337)` | 奖励格 `Reward Display Mission`：**186.875 × 89.29**（屏）/ **162.5 × 77.643**（设计） | 我们按 `(1, 0.337)` | 我们按 `pos (0, 0.6025)` · `sd (0,0)` | 🔴 **`aMin == aMax == (0,0)` ⇒ 无锚点框；框 = 父格 HLG 给 ⇒ 高 = 整格高 `89.29 画布`**（宽 = TMP `preferredWidth`，内容相关） | 高 **30.09 画布** · 宽 186.875 画布 | **高 −59.20**（少 66.3%） |
| B′ | 登录卡 `count 0`**（改后）** | 同上（我们按「HLG 给的那一格」摆） | 同上 | 同上 | 同上 | 同上 | 高 **89.29 画布** · 宽 **117.875 画布**（= 图标右 → 格右） | **高 0.00 ✅** · 宽 **不可比**（原版是内容宽，见 §6·2） |
| C | 登录卡图标 `drawerHolder`**（顺手查出、同笔修）** | 我们**当成** `(0,0)-(1,1)` + `sd(−35.685,−42.369)` | 同上 | 我们按 `(1,1)` | 原版：`LayoutElement.m_PreferredWidth = **55.0**` · 两轴由 HLG 给 | **55.0 × 77.643 设计** = **63.25 × 89.29 画布** | 改前 **145.84 × 40.57 画布** | 宽 **+82.59** · 高 **−48.72** ⇒ 图标按 `keepAspect` 内接后 **40.57 → 63.25（+55.9%）** |
| D | 每日行奖励格 `count`**（顺手查出、同笔修）** | 与 A 同族（`Vertical Variant`）：`aMin (0,0)` · `aMax (1,0.337)` · `piv (0.5,0)` | 格 `145.28 × 172.50`（屏）/ `126.334 × 150`（设计） | `(1, 0.337)` | 原版：`pos (0,0)` · **`sd (0, 0.602545)`**（我们**把 `0.6025` 写进了 `pos`**） | **58.83 画布** | 改前 **58.13 画布** | **−0.70**（小；改后 0.00 ✅） |

**逐行的「怎么算的」**（每行都能按下面三句原地复算）：

- **A**：原版 RT 实读 `RectTransform_/RectTransform_-4283147013975304449.json`（第 2 格 `-3795425069911971583.json` 同值）
  `aMin (0, 0.33000001311302185)` · `aMax (1,1)` · `m_Pivot (0,0)` · `m_AnchoredPosition (0,−3)` · `m_SizeDelta (0,6)`
  —— **与我们 `:606` 那行写的五元组逐位相同**。父 `Mission Milestones Progress Bar` 的 `m_SizeDelta.y = 51.8301`
  且 `aMin.y == aMax.y == 0.5` ⇒ **它的高与「有几张卡」那几帧无关**（`= 51.8301 × 1.15 = 59.60` 恒定）⇒ 这一格可比。
- **B / B′**：`RectTransform_/RectTransform_-1209690867104868609.json`（另一格 `-2268973933791275265.json` 同值）
  `m_AnchorMin = m_AnchorMax = (0,0)` · `m_AnchoredPosition = (0,0)` · `m_SizeDelta = (0,0)` · `m_LocalScale = (1,1)`。
  父格 `Reward Display Mission` 的 HLG 实读（`MonoBehaviour/MonoBehaviour_7266792112342177535.json` 第 0 格 ·
  `MonoBehaviour_-3940592343783237889.json` 第 1 格）：`m_ChildControlHeight = 1` **∧** `m_ChildForceExpandHeight = 1`
  ⇒ uGUI `HorizontalOrVerticalLayoutGroup.SetChildrenAlongAxis` 的**交叉轴**支 `requiredSpace = Clamp(innerSize, min, size)`
  = 整格高（推导见 §4）。格高本身：`Rewards` 的 `m_SizeDelta = (325, 77.64299774169922)`（`RectTransform_-7239511795786443009.json`）
  被 `m_ChildControlWidth/ForceExpandWidth = 1/1` 等分成两格 ⇒ `162.5 × 77.643` 设计。
- **C**：`MonoBehaviour_6650146143716251391.json`（`drawerHolder` 的 `LayoutElement`）
  `m_MinWidth = 40.0` · **`m_PreferredWidth = 55.0`** · `m_PreferredHeight = -1`；`RectTransform_7200957088318297855.json`
  `aMin = aMax = (0,0)`（位置由 HLG 给）。`55 × 1.15 = 63.25 画布` —— 与 `menu_dump` 打出来的 **63.25** 逐位同。
- **D**：`RectTransform_-633122907750812999.json`（每日行第 1 格）/ `RectTransform_-7110481322823208263.json`（第 3 行）
  两处都是 `m_AnchoredPosition = (0,0)` · `m_SizeDelta = (0, 0.6025450825691223)`。
  解算 `0.337 × 172.50 + 0.602545 × 1.15 = 58.825` → **58.83** = `menu_dump` 打出来的值（逐位同）。

**两处「不可比」的说明**：A 的**宽**与 B′ 的**宽** —— 原版的宽分别是「父宽（伸锚）」与「TMP 自己的
`preferredWidth`（内容相关）」，**都不是固定值** ⇒ 表里只比高（**紧的那一边也确实是高**，见 §4）。

---

## 3 · 逐处改动

### 3·1 【框那一笔】`Shell/MissionsTab.cs`

| 处 | 改前 | 改后 | 判据 |
|---|---|---|---|
| `BuildRewardCell` 形参（`:1543`） | `(… autoMinPx, autoMaxPx, autoBasePx)` | 追加 **`float sideIconW = 0f, float sideGap = 0f`**（两个都缺省 ⇒ 既有两个调用点**逐位不变**） | 铁律 5·c「一个值 ≠ 全部情况」：**这一格里有【两个不同的 prefab】的几何**，只能由调用点选 |
| `BuildRewardCell` 函数体 | 单一几何：`dh` = `(0,0)-(1,1)+sd(−35.685,−42.369)`；`count` = `(1,0.337)` 锚区 + `pos.y 0.6025` | **分两档**：`sideIconW > 0` ⇒ **横向档**（图标 `[r.x1, r.x1+55] × 整格高`；数字 `[图标右 + sideGap, r.x2] × 整格高`）；否则**竖向档**（原样）。竖向档里 `0.6025` **从 `pos` 挪进 `sizeDelta`** | 见 §2 的 B / C / D 三行；prefab 字段路径全写在代码注释里 |
| 登录卡调用点（`:799`） | `… 10f, 40f, 36f)` | `… 10f, 40f, 36f, sideIconW: 55f, sideGap: i == 0 ? 5f : 4f)` | `sideIconW` = `LayoutElement.m_PreferredWidth 55.0`；`sideGap` = 两格**各自**的 `m_Spacing`（第 0 格 **5.0** / 第 1 格 **4.0** —— 不是同一个值） |

- **⛔ 四格一个数没动**（`10 / 40 / 36` 照旧；折行仍 `wrapOff: true`）—— `D1` 明令、且 §4 证明它们是对的。
- **⛔ 每日行的 `dh` 一个数没动**（那一档的 `(−35.685, −42.369)` 实读正确，见 `Vertical Variant` 的 RT）。
- **⛔ 骷髅卡那一路仍走缺省（竖向档）** —— 它**本来也需要改**，但那是另一笔量（见 §6·1）。

### 3·2 【断言那一笔】`Editor/RewardsScene.cs`

| 处 | 改前 | 改后 | 判据 |
|---|---|---|---|
| `:1297 → :1308`（每日行 `progress`） | `CheckNear(FontPxOf(…), **40.25f**, 1.5f, "… = 35 × 1.15 …")` —— 钉**收敛值** | `CheckFontWindow(rows[0], "progress", **11.5f, 46f**, …, **41.4f, true**)` | 期望值 = **原版 `m_fontSizeMin/Max/Base` 原值 × 1.15**（`10/40/36 → 11.5/46/41.4`），⛔ 不是收敛值 |
| `:1537 → :1563`（登录卡 `count 0`） | `CheckFontPx(smLogin, "count 0", **46f**, …)` | `CheckFontWindow(smLogin, "count 0", **11.5f, 46f**, …, **41.4f, true**)` | 同上（两颗原版字段**亲读**：`m_fontSize = 40` · `m_fontSizeBase = 36` · `m_enableAutoSizing = 1` · `m_fontSizeMin/Max = 10/40`） |
| `CheckFontWindow`（`:10926`） | `(card, nodeName, wantMinPx, wantMaxPx, what)`；只断 min / max | 追加 `float wantBasePx = 0f`（缺省 0 ⇒ 不填）与 **`bool guards = false`**（缺省 ⇒ 后两条不跑）+ 补：**base**（填了才断）· **`lb.AutoSizing`** · **`FontPxNow ∈ [min, max]`（关系式）** | ⛔ 三条都不拿收敛值当期望；**`AutoSizing` 那一条专门挡「为了让收敛值对上字面量而把自适应关掉」**（= `D1` §共同纪律 明令禁止的那一手 —— 缺了它，「两边一起改回旧写法」照样绿） |

🔴 **`guards` 为什么缺省 `false`（这一条要交接清楚）**：`CheckFontWindow` 全文件共 **15 处调用**，
本件**只对「亲手核过判据」的两站**（`progress` · `count 0`）把 `guards` 打开。
其余 **13 处**各自吃一个前提 ——「标签建在**激活**父件下、`SetAutoFitBox` 真跑过」——
**逻辑上应当都成立，但本件跑不了 Unity、没实跑验证** ⇒ ⛔ **没有一口气铺开**（铺开 = 26 条我无法验证的
新断言，很可能给调度台送去一批假红）。要铺开只是**改那一个缺省值**，建议放在**收口那次自检**里一次做掉。

**为什么这几条是「不自证」的**：`min/max/base` 是**原版 prefab 的字段值 × 1.15**（`m_fontSizeMax` 40 ·
`m_fontSizeBase` 36 · `m_fontSizeMin` 10，MB 亲读），**与我们的框、与字体无关**；
`FontPxNow ∈ [min, max]` 是**性质**不是值；`AutoSizing` 是**开关**不是值。
⇒ 改 `SM_Scale`、改框、换字体都不会让它们一起变绿。

**断言条数会变**（⚠️ 调度台复跑时按新数认）：`−2`（两条 `CheckFontPx`）+ 新增 **`5 × 2 = 10`**
（两条 `CheckFontWindow` 各 5 条：min / max / base / AutoSizing / range）⇒ **净 +8**
（`RewardsScene.Run` 的合计 2034 → 约 **2042**，**要实跑确认**；其余 13 处**一条都没增减**）。

---

## 4 · `count 0` 那个「互斥」是怎么解的 —— **错的是框，四格是对的**

`D1` 的原话：「**原版收敛在最大字号 `40`」与「框高只有 30 px」算术上互斥** ⇒ 这一站的框或四格有一个是错的」。
**实据裁决：框错了。** 三段：

**① 四格是对的（亲读 MB `MonoBehaviour_1211048966688544511.json`）**
`m_fontSize = 40.0` · `m_fontSizeBase = 36.0` · `m_enableAutoSizing = 1` · `m_fontSizeMin = 10.0` · `m_fontSizeMax = 40.0`
—— 与我们 `A1205` 传的 `10 / 40 / 36` **逐位相同** ⇒ 这一格不用动（且 `D1` 也明令别动）。

**② 框根本不是「格高的 0.337 倍」—— 那颗 `count` 没有锚点框**
`RectTransform_-1209690867104868609.json`：`m_AnchorMin = m_AnchorMax = (0,0)` · `m_SizeDelta = (0,0)`。
⇒ 它的框由**父格的 `HorizontalLayoutGroup`** 决定。推导（uGUI 源码，逐句可查
`Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/Layout/HorizontalOrVerticalLayoutGroup.cs`）：

```
SetChildrenAlongAxis(axis = 1 /* 交叉轴 */, isVertical = false)
  innerSize = size − padding.vertical                       // = 格高 77.643 − 0
  GetChildSizes(child, 1, controlSize = ctrlH = 1, childForceExpand = expandH = 1, …)
      → flexible = Mathf.Max(flexible, 1)                   // ⇒ flexible = 1 > 0（forceExpand 那一句）
  requiredSpace = Mathf.Clamp(innerSize, min, flexible > 0 ? size : preferred)
                = Clamp(77.643, minHeight≈47, 77.643) = 77.643   // ⇒ **整格高**
  SetChildAlongAxisWithScale(child, 1, startOffset, requiredSpace, 1)
```
⇒ 原版框高 = **`77.643 设计 = 89.29 画布 px`**，不是 `0.337 × 77.643 = 26.17`。

**③ 于是「不互斥」了**：`89.29` 的框里放 `40`（局部单位 ⇒ 屏上 46）绰绰有余
（即使用最保守的 1.0 em 也只要 40；我们那份 CJK 字体的行盒 ≈1.52 em ⇒ 也只要 60.8 < 89.29）
⇒ **「收敛到上限」与「框够大」自洽**。改前那个 30.09 的框是**两个 prefab 张冠李戴**的产物：
`aMax.y = 0.337` 属于 `Reward Display Mission Vertical Variant`（图标在上、数字在下的那一份，**每日行**用的），
而登录卡这两格是 `Reward Display Mission`（**图标在左、数字在右**，`count` 无锚点框）。

**④ 顺带钉死的一条**：`#5 progress` 的框**不用改** —— 它的五元组与原版 RT **逐位相同**、解算高 `46.83` 与我们的
`46.835` 逐位同（§2 表 A）。它红的原因**只有**断言形态（收敛值 ≠ 标称 × 1.15），属于 `(γ)`+`(δ)`。

---

## 5 · 验证

- ✅ **秒级类型检查**：`TMPDIR=/tmp/wf_f3 bash d:/4/Unity/工具/typecheck.sh` ⇒
  **运行时错误数: 0 / 编辑器错误数: 0**（跑在改完两处之后）。
- ✅ **`git diff --numstat`**：`Shell/MissionsTab.cs` **+80 / −12** · `Editor/RewardsScene.cs` **+85 / −11**
  （不是整篇翻写 ⇒ 行尾没翻）。
- ✅ **行尾复核**（二进制读）：`MissionsTab.cs` `crlf = 0 / lf = 1704` · `RewardsScene.cs` `crlf = 0 / lf = 11016`
  ⇒ 两份**都是 LF 且仍是 LF**。
- ⛔ **没跑任何 Unity 自检**（口径见 `CLAUDE.md` 铁律 12 第六会话那一档；本件是写手，只跑类型检查）。
- ⛔ 没动 git 状态（没用 `commit/checkout/stash/reset`；`git diff` 只读）。

---

## 6 · 没查清 / 停手

1. 🔴 **骷髅卡的奖励格我们也错了，但本件【没动】**（`Shell/MissionsTab.cs:894` 那一处）。
   实据：我们给它传的 `rw` = **`109.25 × 47.433` 设计**（`footer` = `325 × 75.84`），
   而**页内实例**（`…/Special Missions/Daily Skulls Mission Container/background/footer/Rewards`）与
   **独立 prefab** 那一份（路径以 `Daily Skulls Mission Container / background / footer / Rewards` 起）
   **两处的 `Rewards` 都是 `m_SizeDelta = (325, 77.643)`**、两格各 `162.5 × 77.643`（`look.py` 逐条实读，
   `RectTransform_7177619992586360575.json` / `8292020558862412.json` 一族）⇒ **我们那一格的框整条都不对**
   （且走的还是「竖向」那一档）。**为什么停手**：它牵到骷髅卡 `footer`（我们 `325 × 75.84`，原版 `325 × 181.86`）
   与 `counter` / `TimerHolder` **整条链**，要单独一轮量（**不是**「值不值得做」，是本件白名单外的另一站；
   按铁律 11 已如实登记，**要做**，只是不在这一笔）。本件改的 `BuildRewardCell` 形参**两个都缺省** ⇒
   它走的那一路**除 §3·1 表 D 那一项外逐位不变**。
   ⚠️ **顺带一条静默缺口**：骷髅卡的 `count 1` **没有任何断言覆盖它**（`rewards.log` 里绿，因为它没被断）。
2. ⚠️ **`count` 的【宽】复刻不了（残差，已写进代码注释）**：原版那颗的宽 = TMP 自己的 `preferredWidth`
   （随文案与字号变，uGUI 里是布局反馈环路）⇒ **静态算不出来**。我们取「图标右 → 格右」那一整条，
   两格因此在同一条口径下同解。**真正未复刻的** = 两格 `m_ChildAlignment` 不同
   （第 0 格 `5 (MiddleRight)` + `m_Padding.m_Left = −20`、第 1 格 `3 (MiddleLeft)` + `10`）
   所产生的那一段**「按 `preferredWidth` 推到对齐边」的位移** —— 我们把块摆在**格左**（= 第 1 格那一档）。
   **本件没给结论、也没假装对上。**
3. ⚠️ **两帧不同、宽不可比**：原版 `menu_dump` 这一帧的 `Daily Missions` 是 `965.19` 局部 / `1109.96` 屏上，
   我们那一帧是 `609.50` 设计 / `700.93` 屏上（`Editor/RewardsScene.cs:1273` 那条断言钉的就是 700.93）。
   这是**「Daily Missions 那一列有多宽」这一站**的账（属于 `Normal Missions` 的 HLG 分配），
   **不是本条（字号/框）的账**；本件只把两边都量出来、**没裁**。⇒ 建议另立一条。
4. **没查**：`m_enableAutoSizing` 之外，`count` 那颗还挂了 `EverguildTextController`（`maxFontSize` / `minFontSize`
   两个字段，`menu_dump` 的 `字段:` 列印出来了）—— **本件没读它的方法体**，不知道运行期是否还会改字号窗口。
   如实登记（若它会改，窗口类断言要跟着改口径）。

---

## 7 · 顺手发现（⛔ 一处都没自己改）

1. 🔴 **一个「同形同错」的第三站**：骷髅卡奖励格（见 §6·1）—— 它的 `rw` / `footer` 与两处 prefab 都对不上。
   数字已经量好，下一笔直接可用。
2. 🔴 **`count` 在【每日行】那一档也少了一项**（`sizeDelta.y 0.602545` 被写成了 `pos.y`）—— 已同笔修（§3·1 表 D）。
   ⚠️ 这一条**本来是绿的**（没有任何断言盯每日行 `count` 的字号）⇒ **它是「静默站」**：
   改前它收敛在 `58.13 ÷ 1.547 = 37.6`、原版是 `40`（屏上 46）—— 差 18%，**屏上看不出来、断言也抓不到**。
3. ⚠️ **`Editor/RewardsScene.cs:10857` 的 `FontPxOf` 现在只剩 `CheckFontPx` 那一处调用**
   （② `Collect` 文案那一颗，它确实无自适应）—— **不是死代码，没动**。
   ⚠️ 同时记一条**规模**：`CheckFontWindow` 全文件 **15 处** `调用`（不是 3 处）—— 这是本件把新守卫做成
   `guards` 缺省关的**直接原因**（见 §3·2）。
4. ✅ **一条「口径已过期」的注释就地订正**：`BuildRewardCell` 里那句
   `` `count`  N(7, 0,0, 1,0.337, 0.5,0, 0,0.6025, 0,0) `` 把 `0.6025` 记在 **`pos`** 那一格
   —— 实读 prefab 是 **`sizeDelta`**（两处 RT 都是 `m_AnchoredPosition = (0,0)`）。已按铁律 5 就地改掉并留下更正痕迹。
5. 📌 **给下一个会话的方法学**：`menu_dump.py <bundle> "<根名>" --depth 12 --no-sprite` 一次就能拿到
   **整棵树**（596 行），而且它现在（`P-L` 修完尺子之后）打出来的宽高**能被 prefab 字段手算复现**
   （本件逐行核过 A/B/C/D 四行）—— ⚠️ 但**只用它做「屏幕框」**，**字段原值一律回 `RectTransform/*.json` 亲读**
   （`m_SizeDelta` 是不是框，判据只有 `anchorMin == anchorMax`）。

---

## 附：本件搜过 / 读过的范围（供「报没有之前先打出来」那条规矩用）

- 原版字段：`d:/2/新解包资源/assets_full/bundle_menus_assets_all/` 的
  `RectTransform/*.json`（按 `m_Father` 爬链、逐颗看 `m_AnchorMin/Max` · `m_Pivot` · `m_AnchoredPosition` · `m_SizeDelta` · `m_LocalScale`）·
  `MonoBehaviour/*.json`（HLG 的 `m_Padding/m_ChildAlignment/m_Spacing/m_ChildControl*/m_ChildForceExpand*` ·
  `LayoutElement.m_PreferredWidth` · TMP 的 `m_fontSize/m_fontSizeBase/m_enableAutoSizing/m_fontSizeMin/m_fontSizeMax`）·
  `GameObject/*.json`（`m_Component` 与 `m_Children` 树序）。
- 实跑工具（**只读**）：`python -I d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Missions Tab" --depth 12 --no-sprite`
  （stderr 有一行「命中 2 个，取第一个：`-6116860271499889991`」——**取到的是页内那一份**，与我们要的宿主一致）；
  自写只读索引脚本在 `%TEMP%/f3/`（**没进工程**）。
- 第三方判据：`Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/Layout/HorizontalOrVerticalLayoutGroup.cs`
  （`SetChildrenAlongAxis` / `GetChildSizes` / `GetStartOffset`）· 同仓 `Core/UguiRect.cs`（我们的锚点公式）·
  `Battle/Label.cs`（`SetAutoFitBox` / `FontSizeMin/Max/Base` / `AutoSizing` / `FontPxNow` / `FontSizeToPx`）。
- 我们的代码：`Shell/MissionsTab.cs`（`R/FS/Txt/Txt1/FitWindow/BuildRewardCell/BuildLoginCard/BuildSkullsCard`）·
  `Editor/RewardsScene.cs`（`progress` 那一节 · A143 那一节 · `CheckFontPx/CheckFontWindow/FontPxOf`）·
  `Shell/MenuWindowBase.cs`（`Text` / `TextBox` 那条漏斗）· `Shell/DailyData.cs`（`LoginRewardCells` = 2）。
- 没动：`d:/2/**`（只读）· `工具/*.py` · `项目任务.md` · `CLAUDE.md` · git。
