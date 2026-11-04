# B141_W3 · 报告 —— 战役页 `CampaignTab` 的两条 UI 参数（W129 波次 · 账 `4-02` / `4-15`）

> 执行写手 `B141_W3` · 2026-11-04 · 第一百二十八会话 · 批次 `B141`。
> 白名单 = `Shell/CampaignTab.cs` · 新建 `Editor/B141_W3_Assert.cs` · 本文件。
> ⛔ 未跑 Unity、未动 git、未改 `d:/2`、未改账主表 / 其他任何文件。

## 结论（一句话）

`4-02` **已复刻**（spacing −14→**10**、pad 0/0→**L48/R25**，内容宽 1392→**1753**）· `4-15` 的**阵营格**已复刻（命中区 120×130→**142×130**）·
`4-15` 的**另一半（`CampaignTab.cs:1695` info 悬停热区 −15）判据本身错** —— 现读原版 prefab 是 **pad 0**，我们本来就是原版值，**未改**（反证见 §三）。

---

## 一 · `4-02` 战役阵营条 HLG（已复刻）

| 量 | 原版 | 改前（我们） | 改后（我们） | 现读代码 |
|---|---|---|---|---|
| `HorizontalLayoutGroup.m_Spacing` | **10** | **−14** | **10** | `CampaignTab.cs:77` 的 `ArmySpacing` |
| `m_Padding` | **L48 / R25** | 0 / 0 | **48 / 25** | `CampaignTab.cs:77` 新增 `ArmyPadL/ArmyPadR`；`:305` 传给 `UguiLayout.HorizontalContentCentered` |
| 内容宽（13 格） | `48+25+13×120+12×10` = **1753** | 1392 | **1753** | 实测口径 = `Army Content` 的 `sizeDelta.x` |
| 相邻条目中心距 | 120 + 10 = **130** | 106 | **130** | —— |
| 条目组相对内容中心的右偏 | +**11.5**（pad 不对称 `(48−25)/2`） | 0 | **+11.5** | `CampaignTab.cs:973` 的 `HorizontalChild(..., ArmyPadL, ...)` |

**判据（双重现读，两份独立来源逐字同值）**

1. `d:/2/tools/assetripper_out/ExportedProject/Assets/Campaign Tab.prefab` 的 `Army Content`（GO `1099118695303065`）：
   `--- !u!114 &114630643969539439` ⇒ `m_Padding {m_Left 48, m_Right 25, m_Top 0, m_Bottom 0}` · `m_Spacing 10` ·
   `m_ChildAlignment 3` · `m_ChildControlWidth 0` · `m_ChildForceExpandWidth 0`；
   `--- !u!114 &114574304967836189`（ContentSizeFitter）⇒ `m_HorizontalFit 2`（PreferredSize）。
2. `d:/2/新解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-3003792235550560883.json`
   与 `…_4880439462271588095.json`（bundle 里真跑的那两份）：`m_Padding {48, 25, 0, 0}` · `m_Spacing 10.0` —— 逐字同值。

**改了几处（都在同一物理行内改，行号零漂移）**

- `:77` `const float … ArmySpacing = 10f, ArmyPadL = 48f, ArmyPadR = 25f;`（原 `ArmySpacing = -14f`）
- `:305` `HorizontalContentCentered(_selR, n, ArmyItemW, **ArmyPadL, ArmyPadR**, ArmySpacing)`（原 `0f, 0f`）
- `:973` `HorizontalChild(_armyContentR, ArmyItemW, ArmyItemH, i, **ArmyPadL**, ArmySpacing)`（原 `0f`）
- `:1066` `FocusSelectedArmy` 的 `FocusOnAnimated` 目标补上同一个 `ArmyPadL`（否则「对到视口中心」会偏 48 px — 与条目真实位置脱节）

⚠️ `HorizontalChild` 那一处**不是可选项**：原版 `ContentSizeFitter` 撑开的是**内容框**，条目从 `content.x1 + padding.left` 起排；
只改容器 pad 不改 `HorizontalChild` 的 `padLeft`，条目组会贴在内容左沿 ⇒ 右偏 11.5px **不会**出现（正是 `报告/B131` #24 那句「条目组右偏应 +11.5 未加」）。

---

## 二 · `4-15` 阵营格命中区（已复刻 · **2026-11-04 W16 续做后 = 142×132**）

| 量 | 原版 | 改前 | 第一版（W3） | **续做后（W16，现行）** |
|---|---|---|---|---|
| 命中区宽 | **142** | 120 | 142 | **142** |
| 命中区高（未裁） | **132** | 130 | 130 | **132**（上沿 −6 / 下沿 +126） |
| 命中区高（与视口交集） | 129.5（视口只给 3.5px 余量） | 130 | 130 | **129.5** |

**判据**（`Campaign Army Item Button.prefab` 逐字段现读）：子树 5 颗 `m_RaycastTarget = 1` 的图 —
`HighlightBG`(120×110 @顶边) · `Icon`(120×100 @顶边下 5) · `Badge Highlight`(35×35) · `Slider/Background` 与 `Slider/Outline`（滑条带 110×20 @110..130 里锚 `(0,0.2)-(1,0.8)` 的 **12 高芯** @114..126）—
**只有 `Icon` 带 `m_RaycastPadding = (−11,−11,−11,−11)`**（`--- !u!114 &114002669891404536`），其余四颗全 0。
原版可点区 = 各 Graphic **各自** `rect + raycastPadding` 后取并集：
横向 `−11 … 131` = **142** · 竖向 `−6 … 126` = **132**（`Badge` 那块含在 `HighlightBG` 里；`Slider/Fill Area/Fill` 是 `sizeDelta 0` 的退化矩形 ⇒ 不贡献）。

**落点**：`CampaignTab.cs:1001` `AddHit(item, "Hit", ArmyHitRect(r), …)`（原传整格 `r`）。
`ArmyHitRect(r)` + `ArmyHitPad` 都追加在**类尾**（`CampaignTab.cs:1963-1999`，见 §五 行数），
算式 = `MenuDraw.Union(PaddedRect(iconR, ArmyHitPad), hlR, sliderBarR)`，⛔ 外扩只走 `MenuDraw.PaddedRect`（唯一那份「正缩负扩」）。

⚠️ **如实记一笔（与审计的差）**：`报告/B131` 七·一 #10 写「并集 142×130」—— 那是**圆整值**；逐颗算出来是 **142×132**。
第一版（W3）按审计的 130 落地，**W16 已按真值改成 132**（见 §八）。
⚠️ **视口会再裁一刀**：本页 `Campaign Army Selector` 高 **137**、条目高 130 ⇒ 顶边只剩 3.5px 余量，
`RectMask2D` 把上沿那 6px 里的 2.5px 裁掉 ⇒ **量到的命中区高约 129.5**（= 132 与视口的交集）。
**原版也这样**（同一个 137 高视口 + 同一个 130 高条目），⛔ 不是我们的偏差，别据此判「132 没落地」。

---

## 三 · `4-15` 的另一半 —— ⛔ 判据本身错（未改，反证如下）

审计与 W129 都对 `Shell/CampaignTab.cs:1695`（战役页 **info 悬停热区**）写了「原版 **−15 全边**」。**现读不成立**：

| 证据 | 内容 |
|---|---|
| `Campaign Tab.prefab` ▸ `Info Button`（GO `1002669891404536`）的 RectTransform（`224275549183895104`） | `sz = 41.1569 × 41.1573` · 锚 (0.5,0.5) · pos **(179.57, 28.829)** —— 与生产代码 `CampaignTab.cs` 里 `infoR` 的三个数**逐字对上** |
| 同一 GO 的 Image（`--- !u!114 &114187454977326620`） | `m_RaycastTarget 1` · **`m_RaycastPadding (0,0,0,0)`** · sprite guid `aae24d8fa1365cb48833f815b8fcb5e5` |
| 「−15 全边」那 20 颗 `info` 在哪 | 全库扫 `m_RaycastPadding: {x: -15, y: -15, z: -15, w: -15}` ⇒ 27 行，其中 20 行属于名为 `info` 的节点 —— **全部落在任务族 prefab**：`Missions Tab` / `Mission Header` / `Mission Container Vertical` / `Daily Skulls Mission Container(×3)` / `Weekly Mission Container` / `Expansion Period…` / `Daily Login Bonus Container` / `Rewards Base Submenu Variant`（其中 4 颗是**任务页**的 `info`，ancestor 链逐条核过，无一含 `Campaign`）/ `Player Profile Window` / `RankedEventWindow(V2)` |
| 两页是**同一个图标、不同 prefab** | `Missions Tab.prefab` 的 `info` 也是 guid `aae24d8f…`、也是 41×41 框，但 pad = −15 ⇒ 71×71。**这是任务页的值**，⛔ 不能搬到战役这颗 |

⇒ 结论：**战役 info 的命中区吃整颗 41.16² 矩形、pad 0 = 原版**。我们改前就是 0 ⇒ **本就一致**，本轮**未改**，
并在 `CampaignTab.AddInfoTooltip` 的 doc 注释里就地写死这条反证（防将来有人照审计那行改成 −15）。

📌 **请调度台据此就地订正**：`判据/B141_W129_对账_UI动画.md` 的 `4-15` 行、`报告/B138_审计汇总_问题清单.md` 的 `4-15` 行
（本代理⛔ 无权改账/判据正文）。`4-15` 剩下的 6 处（`MissionsTab:1757` / `CollectionWindow:3961` / `PracticeModePopup:1440` /
`LiveOpsEventWindow:1488` / `ExpansionPassEventWindow:991` / `TopBar:320`）不在本包，未核、未动。

---

## 四 · 断言

**文件**：`MyGame/Assets/CardPresentation/Editor/B141_W3_Assert.cs`（全局命名空间、类名 == 文件名；`.meta` 由 Unity 下次导入生成）

**入口**：`Run()`（自建一扇 `RewardsWindow`，跑完拆）· `RunCamp(RewardsWindow win = null)`（宿主传窗，W132 接线用）。
**共 26 条**（14 条前提 + 12 条被测；`win.tabButtons == null` 时少 1 条前提）：

| 组 | 条 | 断什么 | 🧨 怎么才能红 |
|---|---|---|---|
| A | A1–A6 | 前提（窗/页/三件/条目/相邻两条/滚动区） | 夹具坏 ⇒ 明确红（⛔ 不写 `if (x != null) Check`） |
| A | ★A7 | 内容宽 = `48+25+n×120+(n−1)×10`（13 格 = 1753） | pad 改回 `0f,0f` ⇒ 1670；spacing 再回 −14 ⇒ **1392** |
| A | ★A8 | 灭自证（结构性）：内容宽比改前的 1392 宽出 > 300 | 「只改 spacing」被 A7 抓、「只改 pad」被 A9 抓 ⇒ 本条要求**两者都改** |
| A | ★A9 | 相邻条目中心距 = 130 | spacing 回 −14 ⇒ **106** |
| A | ★A10 | 条目左沿 − 内容左沿 − 序号步进 = **padLeft 48**（⛔ 不读实现常量） | `HorizontalChild(..., 0f, ...)` ⇒ 0 |
| B | B1–B3 | 前提（命中区量得到 / 参考格与它的命中区都拿得到） | —— |
| B | ★B4 | 命中区宽 = **142** | 去掉 `MenuDraw.PaddedRect` ⇒ 120 |
| B | ★B5 | 灭自证（结构性）：`hitW − itemW ≈ 22`（>20 且 <24） | 传整格 ⇒ 差 0 |
| B | ★B6 | 原版并集（**未裁**）高 = **132** = 下沿 +126 − 上沿 −6（用 prefab 的 5 / 11 / 110 / 20×0.8 独立算） | —— |
| B | ★B7 | 命中区**下沿** = 条目顶边 + **126** | 沿用整格 ⇒ +130 |
| B | ★B8 | 命中区**上沿** = `max(条目顶边 − 6, 视口上沿)` | 不扩 ⇒ 上沿 = 条目顶边 |
| B | ★B9 | 命中区高 = 132 与视口的交集（本夹具 ≈ **129.5**，视口吃掉 2.5px） | 上一版（130 且下沿 +130）⇒ B7/B8/B9 全红 |
| C | C1–C2 | 前提（`Info Button TipHit` 在、是 RT） | —— |
| C | ★C3/C4 | info 热区 = **41.1569 × 41.1573**（pad 0） | 按审计「−15 全边」去扩 ⇒ 71.16 ⇒ 红 |

跑法 / 判绿口径写在文件头（按**行首** `[B141W3] ✗` 数，⛔ 别 `grep -c ✗`）。

---

## 五 · 行数变化（供账闸重钉）

| 文件 | 改前 | 改后 | 差 | 说明 |
|---|---|---|---|---|
| `Shell/CampaignTab.cs` | 1962 | **1999** | **+37** | 全部**追加在类尾**（`ArmyHitPad` + `ArmyHitRect` + doc）。⛔ 文件里**任何既有行号都没有漂移**（`:77` / `:305` / `:973` / `:1001` / `:1030` / `:1056` / `:1066` / `:1693` / `:1695` / `:1780` / `:1825` 等账落点逐个复读确认仍在原位） |
| `Editor/B141_W3_Assert.cs` | — | 新建 **307** 行 | 新文件 | 不在任何账落点里 |
| 行尾 | LF | LF | 0 | `b.count(b'\r\n') = 0`、裸 CR = 0（`io.open('rb')` 现读） |

> 分两段：W3 那一版 +12（1962→1974）；W16 续做再 +25（1974→**1999**，把 `ArmyHitPad` 的 doc 重写 + 新增 `ArmyHitRect`）。**两段都只在类尾动，既有行号零漂移。**

**账落点变化：无**（没有一处既有行被移动/新增在它们之前）。⇒ 本包**不需要** `check_ledger_points --fix`。

---

## 六 · typecheck 读数

- **W3（`TMPDIR=/tmp/wf_b141_w3`）最终**：运行时 **0** / 编辑器 **0**（下面三次留痕）。
- **W16 续做后（`TMPDIR=/tmp/wf_b141_w3b`）**：运行时 **0** / 编辑器 **0**。

**最终读数（复跑第 3 次）**：

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

⇒ **本包两个文件（`Shell/CampaignTab.cs` + 新建的 `Editor/B141_W3_Assert.cs`）都编译通过**。

**前两次读到的都不是本包的错（留痕）**：

| 次序 | 运行时 | 编辑器 | 归属 |
|---|---|---|---|
| 第 1 次（新 `TMPDIR`，冷启） | **2**：`Battle/BattleDriver.cs(42212,65/97)` CS1061 — `CardInteraction` 没有 `HasLastDropWorld` / `LastDropWorld` | 1（`CS0006` 找不到 `WFCheck.dll` = 运行时没编过的级联） | **别人的在飞文件**：`Battle/CardInteraction.cs` 当时 `grep` 这两个成员 **0 命中**；两个文件都不在本包白名单 |
| 第 2 次（默认 `TMPDIR`，里面有 11-03 23:59 的旧 `WFCheck.dll`） | 同上 2 条 | **184**，全部是「旧 DLL 缺新成员」的级联（`B139_E_Assert` 的 `PopNumber.ResetLive` / `CardFeel.UnitShakeEase` / `B139_V4` 的 `DeckStore.BatchModeProbe` …），**`B141_W3_Assert.cs` 一条错都没有** | 级联（不可信读数，只用来确认本包文件没报错） |
| 第 3 次 | **0** | 第 1 遍只剩 1 条（`RuleEngine/Editor/B141_W9_Assert.cs(73,9)` CS7036，**别的代理的文件**）；再复跑一遍 ⇒ **0 / 0** | —— |

⚠️ 提醒（CLAUDE.md 那条）：**运行时程序集编不过时脚本不会清旧 `WFCheck.dll`** ⇒ 编辑器那半会拿旧 DLL 编、报一串假错。
本包按那条判据处理：**错误全部集中在不是我负责的文件上 ⇒ 不是本包的问题**，隔一会儿复跑即归零。

---

## 七 · 顺手发现（⛔ 未改，只报）

1. `报告/B131` 七·一 #12 与 W129 的 `4-15` 把**任务页**的 `info` pad（−15）记到了战役页头上 ⇒ 见 §三，**建议就地订正**。
2. `CampaignTab.FocusSelectedArmy` 里那个「对到视口中心」的目标表达式**必须与 `BuildArmyItems` 的落位同源**
   —— 它原来按 `_armyContentR.x1 + i×(120−14)` 算，既没有 pad、也带着错的 spacing。本轮已同时补 `ArmyPadL`；
   ⚠️ 将来若再动 `ArmySpacing/ArmyPad*`，**这两处必须一起改**（否则只是「焦点偏一点」，不会报错 = 静默）。
3. `ArmySpacing = −14` 与 `ForgeTab.SelSpacing = −14` 同名同值 —— `报告/B131` §272 怀疑是当年抄错了页。
   本轮战役侧已改回 10；**锻造页的 −14 是原版值**（`Forge Tab.prefab` 的 HLG `m_Spacing = −14`，见 `B141_W7`），⛔ 别顺手也改。

---

## 八 · 续做 `B141-W16`（2026-11-04）：阵营格命中区竖轴 → **132**（按真值，不再沿用审计的 130）

**改了什么**

| 项 | W3 那一版 | **W16 现行** |
|---|---|---|
| 落点 | `AddHit(item, "Hit", MenuDraw.PaddedRect(r, ArmyHitPad), …)`（`ArmyHitPad = (−11,0,−11,0)`，**只横向**） | `AddHit(item, "Hit", ArmyHitRect(r), …)` |
| 算式 | 整格 `r` 横向各 −11 | `MenuDraw.Union(PaddedRect(iconR, (−11,−11,−11,−11)), hlR, sliderBarR)` |
| 并集 | 142 × 130（y `0 … 130`） | **142 × 132**（y `−6 … 126`） |

`ArmyHitRect(r)`（类尾，`CampaignTab.cs`）逐项：
`iconR = (r.x1, r.y1+5, r.x1+120, r.y1+105)` 扩 11 → `(−11,−6)…(131,116)`；
`hlR = (r.x1, r.y1, r.x1+120, r.y1+110)`；
`sliderBarR` = 滑条带中心 `r.y1+110+10` ± `ArmyBarH/2 = 6`，x `r.x1+5 … r.x1+115` ⇒ `(5,114)…(115,126)`（= 原版 `Slider/Background` 的锚 `(0,0.2)-(1,0.8)`）。
并集 = `x −11…131`（**142**）· `y −6…126`（**132**）。
⛔ `Badge Highlight`（35×35，`(85,0)…(120,35)`）含在 `hlR` 里；`Slider/Fill Area/Fill` 是 `sizeDelta 0` 的退化矩形 ⇒ 都不进并集。

**⚠️ 量出来的高不是 132，是约 129.5 —— 这是对的**

`Campaign Army Selector` 高 **137**、条目高 **130** ⇒ 条目顶边距视口上沿只有 **3.5px**；
命中区上沿比条目顶边高出的那 6px 里，有 2.5px 落在 `RectMask2D` 外，被 `MenuDraw.Hit` 的 `ClipRect` 裁掉
⇒ **命中区高 = 132 与视口的交集 = 129.5**。**原版同一个 137 高视口 + 同一个 130 高条目也会裁**，不是偏差。
断言因此写成三条边/高的判据（`B6` 未裁真值 132 · `B7` 下沿 +126 · `B8` 上沿 `max(顶边−6, 视口上沿)` · `B9` 高 = 交集），
而不是钉一个在夹具里永远读不到的 132。

**行数 / 验证**

- `Shell/CampaignTab.cs` 1974 → **1999**（**+25**，全在类尾：重写 `ArmyHitPad` 的 doc + 新增 `ArmyHitRect`；`:1001` 在同一物理行内改调用）⇒ **既有行号零漂移**。
- `Editor/B141_W3_Assert.cs`：B 组从 3 条扩到 6 条（新增「未裁 132 / 下沿 126 / 上沿 / 高 = 交集」+ 两条前提），总数 **21 → 26**。
- `TMPDIR=/tmp/wf_b141_w3b bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
- ⛔ info 那半（`:1695`，pad 0）**一个字没动**。
