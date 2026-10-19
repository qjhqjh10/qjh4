# W57 · 界面线执行代理 UI-2 —— N81 断言（TrackBgAbsorbHit）· Cards 页树形两层容器

> 会话：第五十七会话（2026-10-19）。口径 = **只做不验**：⛔ 未跑 Unity / player / `*.Run` / `_run_8_checks.sh`；
> 只跑了秒级 typecheck。⛔ 未动 git、未改正本、未删文件、未碰 `d:/2`。
> 白名单（只改了这三个）：`Editor/RewardsScene.cs` · `Shell/CollectionWindow.cs` · `Editor/CollectionScene.cs`。
> 本轮只读的判据来源：原版 prefab `bundle_menus_assets_all`（`menu_dump.py` 现跑）· UI-1 报告 `W57_界面线_N81与N52.md` ·
> `Shell/DailyRewardPopup.cs`（未改）。

---

## 一、两件逐项结论

### A. N81 —— `TrackBgAbsorbHit` 吸收层纳入断言 ✅

| 项 | 结论 | 证据（文件:行号） | 原版值 vs 我们值 |
|---|---|---|---|
| 节点 | `Tracks ▸ TrackBgAbsorbHit` 是 `Tracks` 的**直接孩子**，`MenuDraw.WasAbsorb` 为真，`WindowButton.absorbOnly` 置位 | 断言 `Editor/RewardsScene.cs:7652-7726`（`Section` 在 `:7672`）；实现 `Shell/DailyRewardPopup.cs:294` | 原版 `Daily Reward Popup/Tracks/bg` 组件 = `RectTransform`+`CanvasRenderer`+`Image`(**`m_RaycastTarget=1`** · sprite=null · Simple · Color 白 · RaycastPadding 0)+`UIGradient`；父链无点击处理器 ⇒ 点它什么都不发生 |
| 矩形 | 四条边逐条 near（±1.5px），量的是吸收层自己的 `ImageQuad` | `Editor/RewardsScene.cs:7689-7700` | 原版 RectTransform 反算 = **242.37, 159.34 → 1918.78, 558.89**；我们同值（`TrackBg`） |
| 档位 | 吸收层 = **3009**；视觉 `Tracks/bg` 仍在且 = **3005**；并断 `bg(3005) < absorb(3009)` | `Editor/RewardsScene.cs:7701-7711` | 原版无显式 renderQueue（UGUI 由 Canvas 排序）；档是我们这套的分层方案（视觉 3005 = `QPanel−1`；吸收 3009 = 公共件算的 `qContentMin−1`，严格夹在压暗 3002 与内容 3010 之间） |
| 档位告警 | `MenuDraw.AbsorbTierWarned` 对**这一颗**为假（按节点记账） | `Editor/RewardsScene.cs:7712-7715` | —— |
| 行为 | 点这颗吸收层（`WindowButton.Click()`）⇒ 窗**仍 Open**；并断它**不是** `ShadeHit` | `Editor/RewardsScene.cs:7716-7736` | 原版：这颗 `Image` 吃掉射线、**不关窗 / 不播音 / 不改状态**（没有 `BackgroundCloseButton`） |

**🔴 与调度台指令的一处偏差（必须让主对话知道）**：任务书写「用现成口 `MenuDraw.CheckAbsorbRule`」，
**本段没有直接调它**，而是用它同一族的原语重建了「适用于本窗」的那几步。原因（实据）：
- `MenuDraw.CheckAbsorbRule` 的第 ⑥ 步**写死**「点面板外 (5,5) ⇒ 窗 `Closed`」且「(5,5) 命中的必须是**本窗的 `ShadeHit`**」（`Shell/MenuDraw.cs:2825-2855`）；
- 本窗原版的压暗层**没有** `BackgroundCloseButton`（判据 → `Shell/DailyRewardPopup.cs:242-257`，同族断言 → `Editor/ShellScene.cs:1633-1677`），
  我们的压暗层是 `MenuDraw.Absorb`（`DailyRewardPopup.cs:256`）⇒ **点它不关窗** ⇒ 直接调 `CheckAbsorbRule` 会**恒红两条**、而且是**口径错**不是缺陷。
- 另一个理由（即使忽略上一条）：`TrackBgAbsorbHit` 与整屏 `AbsorbHit`（N117）**同档 3009、同 z** ⇒ `CheckAbsorbRule` 的候选点扫描用的是
  `IsOwnAbsorbHit`（只认「本窗自己的吸收层」、**不认节点名**，`Shell/MenuDraw.cs:2871-2875`）⇒ 它会扫到**整屏那颗**而不是这一颗，对这一颗是**弱断言**。
- 本段因此改为：`WasAbsorb` + `absorbOnly` + 矩形四条 near（原版字面量）+ 档 3009/3005 + 无档位告警 +「点这一颗 ⇒ 窗仍开着」。
  期望值全部来自**原版字段 / 上一代理报告的实读档值**，⛔ 未读 `DailyRewardPopup.TrackBg/QShade/QContent` 实参。
- ⚠️ 若调度台坚持要 `CheckAbsorbRule` 的口径，需要给 `MenuDraw` 那条口加一个「压暗层是吸收层」的档（`MenuDraw.cs` **不在本代理白名单**）

> 行为断言为什么用 `WindowButton.Click()` 而不是 `PointerLayer.ClickAt`：这一颗被整屏 `AbsorbHit` 压住（**同档 3009、同 z** ⇒
> `PointerLayer.HitButton` 胜负退化到枚举顺序；这条实现事实 UI-1 报告 §三·3 已记）。两颗**同族同档、都 absorbOnly** ⇒ 可观测行为无差；
> 拿 `ClickAt` 的「谁赢」去断这一颗反而是弱断言。

### B. Cards 页 `Header Filters ▸ WIldcard Display ▸ Counters` 两层容器 ✅

| 项 | 结论 | 证据（文件:行号） | 原版值 vs 我们值 |
|---|---|---|---|
| 新增容器 | `WIldcard Display`（400×85）+ `Counters`（315×44），均为**纯结构节点**（`Node`，无 quad、无渲染队列、无命中） | `Shell/CollectionWindow.cs:936-953`（`WIldcard Display`）· `:962-963`（`Counters`） | 原版 `CardsTab ▸ Header Filters ▸ WIldcard Display` = **1470.0,70.9 → 1870.0,155.9（400×85）**；`WIldcard Display ▸ Counters` = **1555.0,91.4 → 1870.0,135.4（315×44）**（判据 = 本轮 `menu_dump.py bundle_menus_assets_all "CardsTab" --depth 7` 现读） |
| 重挂 | `Background`（原名 `Wildcard Bg`）→ `WIldcard Display`；四组 `Common/Rare/Epic/Legendary` → `Counters`，每组 `Icon` + `Counter` 装原 4 图标 / 4 数字；`Army Icon` → `WIldcard Display` | `Shell/CollectionWindow.cs:963-1024` | 原版父链 = `Header Filters ▸ WIldcard Display ▸ Background/Counters/Army Icon`；`Counters ▸ {Common,Rare,Epic,Legendary} ▸ Icon/Counter`（**2026-10-19 续已 1:1**） |
| 兄弟序 | `Background`(0) → `Counters`(1) → `Army Icon`(2)（创建序即原版序） | 实现 `:963/:967/:1024`；断言 `Editor/CollectionScene.cs:5065` | 原版 `WildcardDisplay.m_Children` 同序 |
| 视觉 / 命中不变 | 所有孩子仍按**绝对设计 px** 落位（`MenuDraw.Local(parent,…)` 按父链换算）；队列实参一个没动（`QPagePanel/QPageRow/QPageText`） | 同上 | —— |
| 树形断言 | 父链（`Header Filters` → `WIldcard Display` → {`Background`,`Counters`,`Army Icon`}）+ `Counters` → 四组 → `Icon`/`Counter` + 兄弟序 + 各层矩形 | `Editor/CollectionScene.cs:5026-5117`（`Section` 在 `:5038`） | 取法一律 `Transform.Find`（只找直接子件）；🧨 删掉任一层 `Node(…)`、件挂回上一层 ⇒ 对应格红 |

> 断言插在 Cards 页现有断言之间（`Army Icon` 那一段之后、`:5083` 视口命中那段之前），**未触碰** UI-1 在 `:9030-9107` 的 N52 断言块。

---

## 二、改动清单

| 文件:行号 | 原值 / 原状态 | 新值 / 新状态 |
|---|---|---|
| `Shell/CollectionWindow.cs:936-953` | `BuildFilterHeader` 之后直接建四件，全部挂在 `page` 上 | 新建 `WIldcard Display`（`page ▸ Header Filters` 之下，1470,70.9→1870,155.9）；`Header Filters` 取不到时**出声告警**并退回 `page` |
| `Shell/CollectionWindow.cs:959-965` | `Nine(page, … "Wildcard Bg")`，rect y 91.5→135.5 | `Nine(wcDisplay, … "Background")`，rect y **91.4→135.4**（队列/九宫参数逐字未动；**续批改名+改 y**） |
| `Shell/CollectionWindow.cs:966-967` | 无 `Counters` | 新建 `Counters`（1555,91.4→1870,135.4，纯容器） |
| `Shell/CollectionWindow.cs:975/982-1008` | 无四组；4 图标 + 4 数字平铺在 `Counters` 下 | 四组 `Common/Rare/Epic/Legendary`（65×44，x=1565/1640/1715/1790）；每组 `Icon`(30×44) + `Counter`(41×44)，原 4 图标/4 数字重挂（**续批**） |
| `Shell/CollectionWindow.cs:991` | `Rect(page, …, "Wildcard Icon i")`，y 91.5→135.5 | `Rect(wcGrp, …, "Icon")`，y **91.4→135.4**（队列/PA 未动） |
| `Shell/CollectionWindow.cs:993-996` | `Text(page, …, "Wildcard Count i")`，y 91.5→135.5 | `Text(wcGrp, …, "Counter")`，y **91.4→135.4**（框/字号/四格未动） |
| `Shell/CollectionWindow.cs:1024` | `_wcArmyQuad = Rect(page, … "Army Icon")` | `_wcArmyQuad = Rect(wcDisplay, …)`（矩形/PA 未动） |
| `Editor/CollectionScene.cs:5026-5117` | 无树形断言 | 新增 `Section("Cards 页：…")`：`WIldcard Display` 直接孩子 / 矩形+400×85 / 三个直接孩子 + 兄弟序 / `Counters` 矩形 / **四组**（父链+兄弟序+rect 65×44+尺寸）/ 每组 `Icon`/`Counter`（直接孩子+兄弟序+rect） |
| `Editor/RewardsScene.cs:7652-7736` | N81 吸收层无断言 | 新增 `Section("★ N81：…")`：节点/公共件/absorbOnly/不是 ShadeHit/矩形四条 near/档 3009 与视觉 3005 及严格大小/无档位告警/点它窗仍开着 |

> 行尾：三个文件改前按二进制数过 —— **全 LF**（`RewardsScene` 11788/11788 · `CollectionWindow` 4595/4595 · `CollectionScene` 10869/10869），
> 改完 `git diff --numstat` 无整篇翻（见 §五）。
> 本轮实际增量（扣掉 UI-1 已落的部分、**两批合计**）：`RewardsScene.cs` **75/0** · `CollectionScene.cs` 总 169/0（UI-1 76 + 本代理 **93**）· `CollectionWindow.cs` 总 62/10（UI-1 19/2 + 本代理 **43/8**）。
> （首批落完时的读数是 133/0 · 75/0 · 44/6；续批追加见 §六。）

---

## 三、没查清的部分（⛔ 不猜）

1. **N81 断言未实跑**：按现行口径（只做不验）没跑 `RewardsScene.Run`。断言本身的取法都用 `Transform.Find` 直接子件 + `RectOf` 的 `ImageQuad` 真测量 ⇒ 实现退回旧形态会红；但「红的实测」未做。
2. **`TrackBgAbsorbHit` 到底吃不吃得到指针**：它与整屏 `AbsorbHit` 同档 3009、同 z，`PointerLayer.HitButton` 同档时胜负退化为枚举顺序。这一颗**是否**在某些枚举序下单独吃到命中，本轮没有实测（批处理外的真机才能量）。**可观测行为无差**（两颗都 `absorbOnly`），如实记。
3. ~~**Y 坐标 0.1px 差**~~ ✅ **2026-10-19（续）已收口**：原「我们 91.5→135.5 / 原版 91.4→135.4」的差已按原版改掉
   （判据 = `menu_dump.py … "CardsTab" --depth 7 --no-layout` 现读，`Background`/`Counters`/四组/`Icon`/`Counter` 全是 **91.4→135.4**；见 §六）。
4. **`Counters` 原版的布局组**：原版 `Counters` = `HorizontalLayoutGroup(spacing=5, align=0, pad=10,10,0,0)`，四组在布局后落位（1565/1640/1715/1790 等）。我们仍是**绝对 px 直摆**（与原版布局跑完的值一致）；未复刻布局组件本身。

---

## 四、顺手发现（只报，本批**未改**）

1. ✅ ~~**`Counters` 底下还差一层（4 组）**~~ —— **2026-10-19（续）已按原版建出**（见 §六·1）。
2. ✅ ~~**节点名与原版不同**~~ —— **2026-10-19（续）已改名**：`Wildcard Bg`→`Background` · `Wildcard Icon i`→`Icon` · `Wildcard Count i`→`Counter`
   （全仓无其它消费者；`Shell/CardDetailPopup.cs` 的同名族是**另一扇窗自己的节点**，未动）。见 §六·2。
3. **`MenuDraw.CheckAbsorbRule` 缺「压暗层是吸收层」这一档**：本窗（`DailyRewardPopup`）与同族（`GenericPromptWindow`，见 `Editor/ShellScene.cs:1587-1625`）都只能手写断言。公共口的第 ⑥ 步把「点面板外 ⇒ 关窗」写死 ⇒ 这一族**永远用不上**那条口。建议给 `MenuDraw` 补一个模式（不在本代理白名单）。
4. **`Tracks` 孩子数**：原版 4（`bg` → Sep Top → Sep Bottom → Scroll View），我们 5（多 `TrackBgAbsorbHit`）。这是「命中层必须是独立 quad」的实现代价，UI-1 报告 §四·4 已记；本批断言不按 childCount 数 `Tracks` ⇒ 无影响。
5. **N117 整屏 `AbsorbHit` 与 N81 这颗同档 3009**：见 §三·2。如果将来想要「节点级 1:1」真正可观测，可以考虑给 `TrackBgAbsorbHit` 一个**更低的档**（例如 3006～3008 之间），但那会改动分层方案，本批**未做**。

---

## 五、typecheck 结果（原文摘要）

```
TMPDIR=/tmp/wf_ui2 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

（最后一次在**全部改动落完之后**跑的；0/0。独立 `TMPDIR`，没有和别人互相覆盖。）

`git diff --numstat`（只列白名单三个文件，含 UI-1 已落的部分；**续批落完之后现读**）：

```
169  0  Unity/MyGame/Assets/CardPresentation/Editor/CollectionScene.cs
75   0  Unity/MyGame/Assets/CardPresentation/Editor/RewardsScene.cs
62  10  Unity/MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs
```

（`CollectionScene` 那 169 行里包含 UI-1 的 76 行 N52 断言（本代理 93 行 = 首批 57 + 续批 36）；
`CollectionWindow` 的 10 行删除 = UI-1 那 2 行 + 本代理两批合计 8 行（改名/改 y/重挂）。
行尾全部保持 LF，无整篇重写迹象。）

---

## 六、§ 续：三条 Cards 页树形尾巴收口（2026-10-19 同会话追加）

> 口径不变：**只做不验**（未跑 Unity / `*.Run`），只跑 `typecheck`（`TMPDIR=/tmp/wf_ui2b`）。
> 白名单不变（`Shell/CollectionWindow.cs` · `Editor/CollectionScene.cs` · `Editor/RewardsScene.cs`）；`RewardsScene` 本轮无改动。

### 1. `Counters ▸ {Common,Rare,Epic,Legendary} ▸ {Icon, Counter}` ✅

**原版值**（判据 = 本轮 `python -I -X utf8 工具/menu_dump.py bundle_menus_assets_all "CardsTab" --depth 7 --no-layout` 现读，2026-10-19）：

| 组 | rect（x1,y1 → x2,y2） | 尺寸 | 孩子 |
|---|---|---|---|
| `Common` | 1565.0, 91.4 → 1630.0, 135.4 | 65×44 | `Icon` 1565→1595（30×44）· `Counter` 1595→1636（41×44） |
| `Rare` | 1640.0, 91.4 → 1705.0, 135.4 | 65×44 | `Icon` 1640→1670 · `Counter` 1670→1711 |
| `Epic` | 1715.0, 91.4 → 1780.0, 135.4 | 65×44 | `Icon` 1715→1745 · `Counter` 1745→1786 |
| `Legendary` | 1790.0, 91.4 → 1855.0, 135.4 | 65×44 | `Icon` 1790→1820 · `Counter` 1820→1861 |

**做了什么**：`Shell/CollectionWindow.cs:975/982-1008` 在 `Counters` 下按上表建四组（`Node`，纯结构节点），
每组里 `Rect(…, "Icon")` + `Text(…, "Counter")` 装原来的 4 个稀有度图标/数字；父链、兄弟序（Common→Rare→Epic→Legendary；
组内 Icon→Counter）与原版逐字一致。**改名**：`Wildcard Icon i`→`Icon`、`Wildcard Count i`→`Counter`（旧名全仓无消费者）。
**视觉**：图标/数字的矩形、队列（`QPageRow`/`QPageText`）、PA 全部未动 ⇒ 只有 0.1px 的 y 修正（见 3）。
⚠️ 原版组宽 **65** 而 `Counter` 右沿到 `x+71` —— 孩子**溢出组框 6px**，照抄（dump 实读，不是笔误）。

**断言**：`Editor/CollectionScene.cs:5067-5116`（新增）——
每组的直接父 = `Counters`、`CheckAt` 组矩形 + `sizeDelta` 65×44、`Icon`/`Counter` 是组直接孩子、
组内兄弟序 Icon→Counter、`Icon`/`Counter` 各自 `CheckAt` 到原版矩形；另断四组兄弟序。
**验收读数**：`CheckAt` 容差 1.08px（`0.01` 世界单位）、`CheckNear` 容差 0.5px（尺寸 65/44），全部按上表字面量。

### 2. `Wildcard Bg` → 原版 `Background` ✅

- 实现：`Shell/CollectionWindow.cs:963-965`（`Nine(…, "Background")`）；**全仓只有本行建它**（grep 现核）。
- 断言：`Editor/CollectionScene.cs:5055`（`wd.Find("Background")`）+ 文案里写明「⛔ 不是以前的 `Wildcard Bg`」。
- ⚠️ 现存 `FindChild(root, "Background")`（`Editor/CollectionScene.cs:6113` 那条窗口底图队列断言）**不受影响**：
  它找的是窗根（`Content Area/Background`，`MenuShell` 先建）——DFS 序在 `Tabs` 之前命中，本轮未改。
- `Shell/CardDetailPopup.cs` 的同名族是**另一扇窗自己的节点**（`Wildcard Bg`/`Wildcard Icon`/`Wildcard Count`），未动（不在白名单、也不该动）。

### 3. y `91.5/135.5` → 原版 `91.4/135.4` ✅

- 判据：**同一份 `--no-layout` 现读**（原始 RectTransform 值，不经布局求解）—— `Background`、`Counters`、四组、`Icon`、`Counter`
  **全部是 91.4 → 135.4**（高 44）⇒ 不是旧表的圆整，按原版改。
- 实现：`Shell/CollectionWindow.cs:959-965`（`Background`）· `:986`（四组）· `:991`（`Icon`）· `:993-994`（`Counter` 的 y1 = 91.4、
  y2 = `91.4 + DeckRuntime.WcCountBoxH(44)` = 135.4）。
- 断言：`Editor/CollectionScene.cs` 段内所有 `CheckAt` 的 y 用 **91.4/135.4**。
- ⚠️ `Army Icon` 仍是 **70.94→155.94**（原版 dump 是 70.9→155.9，差 0.04px）——不在本轮三条范围内，**未动**，如实记。

### typecheck（续批）

```
TMPDIR=/tmp/wf_ui2b bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

（在**续批全部改动落完之后**跑的；0/0。独立 `TMPDIR`。）

（无整篇重写迹象；行尾全部保持 LF。）
