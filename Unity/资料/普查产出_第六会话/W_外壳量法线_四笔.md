# W_外壳量法线_四笔（`A1004` · `A990②` · `A1001` · `A1002` 口径）—— 执行写手

> 白名单内动手（6 个文件）· **⛔ 没跑 Unity 自检**（按派单红线）· **⛔ 没动 git** · ⛔ 没改两张正本（`项目任务.md` / `CLAUDE.md`）。
> 秒级类型检查 3 次（每次改完一波）**运行时 0 / 编辑器 0**（最后一次见 §④）。
> 证据一律**本次现读**；与派单不符的，在 §①/§⑤ 里点明「简报说什么 / 实际是什么」。

---

## ① 结论（四笔各一句）

1. **`A1004`（软边失配的一行）**：**现读 = 那一条【早就改好了】**（`Shell/MenuDraw.cs:656` 的位置项已经走 `PixelOfDesign`，`git log -S` 定位到上一个提交 `1667ff6`）。
   ⇒ 本件在这一笔上做的是**它依赖的那一份换算的收口**：把 `MenuDraw.PixelOfDesign` 的式子搬进 **`LayoutSpace.ToDesignPixel`（全仓唯一一份）**、并把它提 **`public`**（A990② 那三处要用）。
   改前/改后**在非 16:9 下结果不同**（16:9 差 ≤ **2.44e-4 px**；4:3 差 `|设计px − 960| × 0.25`，**在 d=300 处 = 165 设计 px**）。
2. **`A990②`（命中判定那几处）**：**逐处判清了**。· `Shell/PointerLayer`：**按钮命中那一半本来就不偏**（中心与半宽同源，倍率自己约掉）⇒ 我把它**整帧**换成设计帧（指针 + 中心 + **x 半宽**，三行必须同批）；真正**只在 16:9 自洽**的是 `HitScroll` / `AxisOf` 那条**跨帧边界**（4:3 下滚动区多认 **33%**、21:9 少认 **24%**，围着画布中心胀缩）。· `Shell/SettingsWindow.SetFpsFromPointer`：**真点偏（判清）** —— 4:3 下 983 个采样点里 **161 个**取到的档位错、21:9 **114 个**、16:9 **0 个**。· `Shell/ViewportClip.ClipPx`：改的是**只有夹具才走**的那一支（生产 7 处视口节点全记过 `BaseRect`）⇒ 属**潜伏**，判据 = 「两支必须同帧」。
3. **`A1001`（探针前插建窗表）**：**已插入** —— `Editor/ShellScene.cs:5848-5929` 一张 **22 行**建窗表（6 扇走 `WindowsManager` 的 `Open*`、13 扇 `Create(mgr)`、3 扇小夹具）＋ `try/catch` **逐条如实打**（建起来了 **N/22** + 逐类列名 + 建不出来的逐条）＋ 扫完 **`DestroyImmediate`**（`:5981-5994`）；顺带把**「16 + 23 > 38」那个假数**就地订正（`:5942-5948` 的 `ledger` 闸）并补一条**账面自洽断言**（`:6065-6078`）。**未实跑**（红线不许跑 Unity）⇒ 静态可核，真绿红以收口那次为准。
4. **`A1002`（自检宿主份数四种口径）**：**只出口径建议，未动正本**（→ §⑦）。核心 = **两个概念分开**：**自检宿主 = 8 份**（权威 = `CLAUDE.md` 铁律 12 那张**列举**表）· **§15「辅助函数」那一族 = 7 份**（= 8 − `Editor/BattleScene.cs`，实据 `grep -c "MenuCheck\." Editor/*.cs`）。

---

## ② 改动清单（**文件:行号（改后行号）| 改前 | 改后**）

### 2·1 `Core/LayoutSpace.cs`（+63/−1）

| 行 | 改前 | 改后 |
|---|---|---|
| `:167-176`（`ToPixel` 的 doc） | 「世界坐标 → 原版像素点（**`FromPixel` 的逆**）。命中判定要用它。」 | **就地订正**：「那句『FromPixel 的逆』**只对 x=写死 108 这一半、且只在 16:9 成立**；**真正的逆是 `ToDesignPixel`**；凡『读回来的 px 要**去比一个【字面设计 px】**』（命中判定 / 裁剪换算 / 量标尺）一律用 `ToDesignPixel`；`ToPixel` 只留给『世界 px ↔ 世界 px 内部自洽』」 |
| `:189-241`（新增一节） | —— | 新增 **`PxPerWorldX`（`:217`）** = 世界单位→画布 px 的 **x 斜率**（= `DesignPxW ÷ VisibleWidth`；退化档退回 108）+ **`ToDesignPixel(Vector3)`（`:238`）** = `FromPixel` 的**逆**（x 用实测 `VisibleWidth`、y 转调 `PxY`），带「⛔ 别再在调用点手写式子」的说明 |

### 2·2 `Shell/MenuDraw.cs`（+7/−20）

| 行 | 改前 | 改后 |
|---|---|---|
| `:587-593` | `static Vector2 PixelOfDesign(Vector3 designPos) { float vw = …; if (vw <= 1e-6f) return LayoutSpace.ToPixel(designPos); return new Vector2((designPos.x / vw + 0.5f) * LayoutSpace.DesignPxW, LayoutSpace.PxY(designPos.y)); }`（**private**，式子写在本文件） | `public static Vector2 PixelOfDesign(Vector3 designPos) => LayoutSpace.ToDesignPixel(designPos);`（**一行转调**；doc 留订正痕：式子逐字搬进 `LayoutSpace`、值不变、**提 public 的理由 = 外壳三处命中判定要用同一条读口**） |

> ⚠️ 这就是 **`A1004` 那一行所依赖的换算**的落点：`QuadRectPx`（`:656`）与 `ClipQuad`（`:1414`）一个字没动，行为差只来自「`x*(W/vw)+960`」与「`(x/vw+0.5)*W`」的 float 舍入（**16:9 实测最大 2.44e-4 px**，见 §④）。

### 2·3 `Shell/PointerLayer.cs`（+52/−10）—— **三行必须同批**

| 行 | 改前 | 改后 |
|---|---|---|
| `:265`（原 `:251`） | `Vector2 px = LayoutSpace.ToPixel(wp);` | `Vector2 px = MenuDraw.PixelOfDesign(wp);`（+13 行注释：这一帧 = **设计 px × 父链缩放**；下游谁受影响、按钮那半为什么不偏、副作用如实记） |
| `:906`（原 `:877`） | `center = new Vector2(LayoutSpace.PxX(p.x), LayoutSpace.PxY(p.y));` | `center = LayoutSpace.ToDesignPixel(p);`（⚠️ 传**裸世界坐标**、不是 `PosInDesignSpace` —— 这一路要的是「指针那一帧」） |
| `:921`（原 `:884`） | `half = new Vector2(q.WorldW * ScaleAbs(ls.x) * k * 0.5f, q.WorldH * ScaleAbs(ls.y) * k * 0.5f);` | `half = new Vector2(q.WorldW * ScaleAbs(ls.x) * LayoutSpace.PxPerWorldX * 0.5f, q.WorldH * ScaleAbs(ls.y) * k * 0.5f);`（**x 换斜率、y 不换**：可见高恒 10 单位 ⇒ y 的斜率恒 = 108） |
| `:339` / `:342` / `:370` / `:373` / `:1230` / `:1232`（doc） | 「`px` 是**世界 px**（`ToPixel(ScreenToWorld(mouse))`）」「→ 指针那一帧（**世界 px**）… `c + (ToPixel(d) − c)·M`」「`px/py` 是**世界 px**」 | 就地订正成「**指针那一帧 = 设计 px × 父链缩放**」，并写明 `DesignToPtrPx` / `AxisOf` / `HitScroll` **一个字都没改**而两层（`M` 那一层 + **宽高比那一层**）现在都闭合 |

### 2·4 `Shell/ViewportClip.cs`（+16/−3）

| 行 | 改前 | 改后 |
|---|---|---|
| `:302`（原 `:289`，`ClipPx` 实时反推那一支） | `Vector2 c = LayoutSpace.ToPixel(MenuDraw.PosInDesignSpace(transform));` | `Vector2 c = MenuDraw.PixelOfDesign(MenuDraw.PosInDesignSpace(transform));`（+9 行注释；**尺寸项 `K` 不动** —— 「与 `MenuDraw.QuadRectPx` 同一份口径」是两支之间唯一的契约） |
| `:59-62` / `:247-249`（文件头 + `ClipPx` doc） | 「世界 → 设计 px 走 `…PosInDesignSpace` + `LayoutSpace.ToPixel`」 | 改成 `+ MenuDraw.PixelOfDesign`，并留「原来写的是 `ToPixel` —— 它 x 写死 108、**只在 16:9 是设计 px**」的订正痕 |

### 2·5 `Shell/SettingsWindow.cs`（**本件只动 1 处**；文件在工作树里本来就有 206/52 的**未提交改动**，不是本件）

| 行 | 改前 | 改后 |
|---|---|---|
| `:2481`（原 `:2465`） | `public bool SetFpsFromPointer(Vector3 world) => SetFpsFromCanvasX(LayoutSpace.ToPixel(world).x);` | `public bool SetFpsFromPointer(Vector3 world) => SetFpsFromCanvasX(MenuDraw.PixelOfDesign(world).x);`（+18 行 doc：为什么必须是它 = `Screen()` 那一式的形状；**同一帧的另两处不在白名单**，见 §⑤） |

### 2·6 `Editor/ShellScene.cs`（+142/−10）

| 行 | 改前 | 改后 |
|---|---|---|
| `:5709-5716`（A964 段头） | 无 | +8 行：**那一张建窗表是什么、为什么要有它、建完交给同一条现扫路、扫完拆掉**（判据指针 → 查证 §1） |
| `:5848-5866` | 无 | +19 行块头：为什么（覆盖 15/38 是副作用）· 为什么不走反射（方案 B 会**静默跳过**）· 为什么不只标抽样（方案 C 账清不掉）；**并把 `allTypes` 的算法从 `:6047` 上提到这里**（扫描循环要用） |
| `:5876-5905`（新） | 无 | **22 行建窗表**：`("名字", m => 建法)`；6 扇 `WindowsManager.OpenBaseOfferPopup/OpenGenericOptionsPanel/OpenAllianceMemberOptions/OpenRankedRewardEvent/OpenReferralPopup/OpenEnergyEvent` · 11 扇 `Create(m)`（`BoosterInfoPopup`/`BoosterPackOpenWindow`/`CampaignRewardWindow`/`DailyRewardPopup`/`DailyStreakPopup`/`ImportDeckPopup`/`MissionRerollPopup`/`RewardWindow`/`CardDetailPopup`/`ShopWindow`/`CollectionWindow`）· 2 扇可选参（`DeckSelectionPopup.Create(m)` / `TrophyInfoPopup.Open(m)`）· 3 扇小夹具（`DeckInfoPopup.Create(m,0,Edit)` / `SearchingOpponentWindow.Create(m,0)` / `DuelPopupWindow.Create(m,"Goff")`） |
| `:5909-5928` | 无 | 逐条 `try/catch` 建 + **建起来了 N/22** 的日志（逐类列名）+ **建不出来的逐条列**（`Create` 返 null / 抛异常都出声，⛔ 不静默） |
| `:5942-5948` | `string tn = …; if (!hasInstance.Contains(tn)) hasInstance.Add(tn);` | 加 `bool ledger = allTypes.Contains(tn);` ⇒ **裸 `GameWindow` 夹具（不是子类）不入账**；⚠️ **只闸记账、不闸扫描**（那几颗夹具上的钮照旧扫、照旧进 TSV/E1/E3 —— 少扫会丢证据） |
| `:5965` / `:5974-5975` | `failed.Add(...)` / `covered.Add(...)` / `emptyWin.Add(...)` 无条件 | 各加 `ledger` 闸（非子类不入账；`failed` 那行照旧出声） |
| `:5981-5994` | 无 | +15 行：**扫描之后 `DestroyImmediate` 建窗表建出来的那 22 扇** + 「已销毁 N 扇」日志（批处理无帧循环 ⇒ 只能 `DestroyImmediate`） |
| `:6060-6078` | `var allTypes = …`（算第二遍） | 删掉那一份（**上提**了）＋ **新增一条断言**：`★ A1001：覆盖面的账自洽`（`covered({n}) + notHere({m}) ≤ 全库子类 {k}` 且 `hasInstance ∪ covered` 里**一个非子类名字都没有**；🧨 改坏法 = 删掉扫描循环那句闸 ⇒ 账变 39 > 38 ⇒ 红） |
| `:6104-6106` | `notHere` 那行日志 | 补一句：`GameWindowWithTabs` 是**基类**、**永远**留在名单里，那不是缺口 |

---

## ③ 证据

### 3·1 `A1004` 现读 = 早已改好（这是本件最重要的一条「简报 vs 现读」）

- `Shell/MenuDraw.cs:656`：`Vector2 cpx = PixelOfDesign(PosInDesignSpace(q.transform));` —— **已经是**那一行方案。
- `git log -S "PixelInDesignSpace"` 实测命中 **`1667ff6`**（`git log -S "PixelOfDesign(PosInDesignSpace" -- .../MenuDraw.cs` 只回这一个提交）⇒ **上一个会话就落了**；`git status --porcelain …/MenuDraw.cs` **空**（相对 `HEAD` 干净）。
- 判据真身（派单指的） = `资料/普查产出_1018第三会话/W_ClipQuad与量法.md:85`「**方案（一行）**：`:625` 改 `PixelOfDesign(PosInDesignSpace(q.transform))`」 ✓ 与现读逐字一致（行号漂到 `:656`）。
- **`改前/改后在哪一种屏幕比例下不同`**：**只有 16:9 相同**（差 ≤2.44e-4 px）；4:3 差 `|d − 960| × 0.25`、21:9 差 `|d − 960| × 0.3125`（设计 px）—— 我按同一套公式离线算了一遍（脚本见 §⑤ 口径），表：

| 设计 px `d` | 4:3 旧读−真值 | 21:9 旧读−真值 | 新读（任何比例） |
|---|---|---|---|
| 300 | **+165.00** | **−206.25** | 0 |
| 500 | +115.00 | −143.75 | 0 |
| 900 | +15.00 | −18.75 | 0 |
| 960（画布中心） | 0 | 0 | 0 |
| 1284.63 | −81.16 | +101.45 | 0 |

（⇒ 这也是「为什么 `SameRectNear(…, 0.05px)` 会失配」的量化：0.05 的容差 vs 上面这些几十上百 px 的差。）

### 3·2 `A990②` —— 两条式子现读（`Core/LayoutSpace.cs`）

| 方向 | 函数 | x 的算式 | 备注 |
|---|---|---|---|
| **建件（写）** | `LayoutSpace.FromPixel` | `w = (px/1920 − 0.5) × VisibleWidth` | `VisibleWidth = 10 × max(0.1, cam.aspect)` |
| **读回（旧）** | `LayoutSpace.ToPixel` / `PxX` | `px′ = 960 + w × 108` | **只在 `VisibleWidth == DesignWidth`（16:9）是逆** |
| **读回（新）** | `LayoutSpace.ToDesignPixel`（`MenuDraw.PixelOfDesign` 转调它） | `px = 960 + w × (1920 / VisibleWidth)` | y 转调 `PxY`（**y 两个函数本来就同值**：可见高恒 10 单位 = 1080px） |

倍率 **`r = VisibleWidth / DesignWidth`**（16:9 ⇒ 1.0000 · 16:10 ⇒ 0.9 · **4:3 ⇒ 0.75** · **21:9 ⇒ 1.3125**）。⇒ 「旧的读」= 把真值**围着画布中心 960 压/胀 `r` 倍**。

**逐处「文件:行 | 改前 | 改后 | 非 16:9 点偏」：**

| # | 文件:行（改后） | 改前 | 改后 | 「非 16:9 真点偏」判清了吗 |
|---|---|---|---|---|
| 1 | `Shell/PointerLayer.cs:265` | `LayoutSpace.ToPixel(wp)`（108 帧） | `MenuDraw.PixelOfDesign(wp)`（设计帧） | **判清**（见下两行：与命中区的**帧一致性**才是要点） |
| 2 | `Shell/PointerLayer.cs:906` | `PxX/PxY(p)`（108 帧中心） | `ToDesignPixel(p)` | **不偏**（按钮命中那一半）—— 推导：旧 `\|px_p−px_q\|·108 ≤ W·M·108/2` ⇔ 新 `\|px_p−px_q\|·(1920/VW) ≤ W·M·(1920/VW)/2` ⇔ 两边同为 **`\|Δworld\| ≤ W·M/2`** ⇒ **世界空间上逐值等价**（它本来就是「命中区 = 画出来那一块」）。⛔ 但**必须与 #1 同帧**，且 #3 必须同批改 |
| 3 | `Shell/PointerLayer.cs:921` | `… × k(108) × 0.5` | `… × PxPerWorldX(1920/VW) × 0.5`（**x 换、y 不换**） | ↑ 同上。🔴 **只改 #1#2 不改 #3 ⇒ 非 16:9 下命中区被缩小 `r` 倍**（4:3 只剩 **75%**，正是「看着在钮上、点不动」），而 **16:9 照样全绿** ⇒ 这是这一笔最危险的一刀 |
| 4 | `Shell/PointerLayer.cs` 的 `HitScroll`/`AxisOf`（**未改一个字**，靠 #1 换帧救活） | 指针是 108 帧、`MenuScroll.Viewport` 是**字面设计 px** ⇒ 命中判据 `\|d−960\| ≤ \|d_v−960\|/r` | 指针与视口**同一帧** ⇒ 判据 = 视口本身 | **判清（真偏）**：4:3 命中范围围着中心**胀 `1/0.75 = 1.333` 倍**（相邻那一列上滚轮也会滚这一区）· 21:9 **缩到 0.762**（边缘 24% 滚不动）· 16:9 无差 |
| 5 | `Shell/SettingsWindow.cs:2481` | `LayoutSpace.ToPixel(world).x` | `MenuDraw.PixelOfDesign(world).x` | **判清（真偏）**：离线按本窗真实常量（`_fpsL = 1284.63 − 491.18 = 793.45` · `areaW = (491.18−10)×0.9 = 433.06` · `Screen()` 的 0.9 烘进矩形）逐点算：**4:3 下 983 个采样点里 161 个取到的档位错**（误判带 `d ∈ [898.5, 1219.0]`，即**轨道最后 ~65px 报 1 而不是 2**、左端一小段报 1 而不是 0）· **21:9 下 114 个**（带 `[914, 1154]`）· **16:9 下 0 个** |
| 6 | `Shell/ViewportClip.cs:302`（`ClipPx` 实时反推支） | `LayoutSpace.ToPixel(…PosInDesignSpace…)` | `MenuDraw.PixelOfDesign(…)` | **属潜伏、不是「真点偏」**：生产侧 7 处视口节点**全部**记过 `BaseRect`（走上面 ①，**一次换算都不做**）⇒ 这一支今天只在夹具 / 未记矩形的节点上带电。判据 = 两支**必须同帧**（否则同一棵树里两个框的口径会分家）。偏移量同 §3·1 那张表（`\|d−960\|·\|1−r\|`） |

**两处仍在白名单外的同族站点（只报不改）**：`Shell/CampaignTab.cs:569`（`ToPixel(a + ab*0.5f)` 与 `:570` 的 `halfLen = … × 108f` —— **一对**，去比 `_vpR` 那个**设计 px** 视口框，是「一段连线整段在视口外就不建」的裁切换算）· `Shell/SettingsWindow.cs:2401` / `:2405`（`UpdateFpsDrag` 里那两处 `PxX/PxY`，与本件 #5 同一帧的另两个入口）。改法都同 #2#3 那一对（**中心与长度必须同批**）。

---

## ④ 验证

- **秒级类型检查**（`TMPDIR=/tmp/wf_shell bash d:/4/Unity/工具/typecheck.sh`，**本件共 3 次**，最后一次=全部改完之后）：
  `--- 运行时程序集 ---` **运行时错误数: 0** · `--- 编辑器程序集 ---` **编辑器错误数: 0**。
  ⚠️ 中途一次报过 **1 条我自己的**（`ShellScene.cs(5925,62) CS7036` —— `Array.ConvertAll` 对**命名元组**推不出 converter）⇒ 已改成显式 `for` 循环收集名字，**之后两轮全 0**。其余轮次**没有**出现在白名单之外文件的错。
- **行尾**（二进制读，改前 / 改后都是**纯 LF**，`b.count(b'\r\n')` 全 **0**；⛔ 没用 `sed -i`、python 也没用文本模式写）：

| 文件 | 改前 CRLF/LF | 改后 CRLF/LF |
|---|---|---|
| `Core/LayoutSpace.cs` | 0 / 181 | **0 / 243** |
| `Shell/MenuDraw.cs` | 0 / 3057 | **0 / 3044** |
| `Shell/PointerLayer.cs` | 0 / 1209 | **0 / 1251** |
| `Shell/ViewportClip.cs` | 0 / 445 | **0 / 458** |
| `Shell/SettingsWindow.cs` | 0 / 3504 | **0 / 3520** |
| `Editor/ShellScene.cs` | 0 / 6051 | **0 / 6183** |

- **`git diff --numstat`**：

```
63      1       Core/LayoutSpace.cs
7       20      Shell/MenuDraw.cs
52      10      Shell/PointerLayer.cs
16      3       Shell/ViewportClip.cs
206     52      Shell/SettingsWindow.cs     ← ⚠️ 只有 1 处（:2481）是本件，其余是工作树里【本来就有】的未提交改动
142     10      Editor/ShellScene.cs
```
  （⚠️ 派单已提醒：`git diff` 含**上一批未提交的**改动。我逐文件看了 `@@` 头：`SettingsWindow` 的 20+ 个 hunk 里**只有最后那个**（`SetFpsFromPointer` 那一行）是我的；其余六个文件的 hunk **全部**落在本件改动上。）

- **受影响的宿主（逐条）** —— 现读 `grep -c` 各共用件在每份宿主里的引用数（`Editor/*.cs`）：

| 宿主 | `PointerLayer.` | `MenuDraw.` | `ViewportClip`/`ClipPx` | `LayoutSpace.ToPixel/PxX/PxY` | 本件影响 |
|---|---|---|---|---|---|
| `ShellScene` | 55 | 206 | 126 | 22 | 🔴 **最重** + **本件改了它本身**（建窗表） |
| `MainMenuScene` | 36 | 197 | 26 | 37 | 经 `PointerLayer` 指针帧 + `MenuDraw` |
| `RewardsScene` | 47 | 147 | 18 | 8 | 同上 |
| `ShopScene` | 9 | 70 | 13 | 4 | 同上 |
| `CollectionScene` | 22 | 96 | 35 | 19 | 同上 |
| `SettingsScene` | 8 | 48 | 0 | 1 | 🔴 **本件改了它本身**（1 行）+ A167 那两条两态断言 |
| `DeckScene` | 0 | 19 | 0 | 1 | 只间接（`MenuDraw` 共用件） |
| `BattleScene` | 0 | 8 | 0 | 17 | 只间接（`MenuDraw` 共用件） |

  **为什么 16:9 零回归（可离线核）**：所有建 UI 的宿主都在**建树之前**把 `cam.aspect` 钉成 `DesignAspect` ⇒ `r = 1.00000`。此时
  ① `ToDesignPixel` 与 `ToPixel` 的差 = **float 舍入**，我按 float32 逐点算过：**16:9 下最大 2.44e-4 px**（`vw = 17.777779` ⇒ `1920/vw = 107.99999`）⇒ 本仓最紧的容差是 `0.01f px`（`DeckScene` A328①）与 `0.05px`（`SameRectNear`）⇒ **余量 ≥ 40 倍**；
  ② `PxPerWorldX` 与 `k` 在 16:9 同值（同上舍入）⇒ `HitBoxPx` 的返回值只动第 4 位小数（A167 那两条判别带是 **40px / 25px** 量级）；
  ③ 各宿主**驱动** `PointerLayer` 时喂的 px 仍由 `LayoutSpace.ToPixel/PxX` 算出（它们只跑在 16:9）⇒ 与新帧在 16:9 重合。
  ⚠️ **反向风险如实记**：**若哪一天某个宿主改成非 16:9 跑**，那些「用 `ToPixel/PxX` 算 px 再喂 `ClickAt/WheelAt/ScrollUnder/MoveTo`」的驱动点要**跟着换** `PixelOfDesign`（现读它们全在 16:9，所以今天不受影响）。
- **`A990②` 的数值判据**：三条结论都是**离线按被测那两条公式**算的（脚本不在仓库里、只跑在临时目录，⛔ 没拿被测实现当期望值：模型里 `FromPixel` / `PxY` 是自己重写的一遍）。**⛔ 没跑 Unity** ⇒ 「点偏」是**算式级结论**，不是实机读数。

---

## ⑤ 没查清 / 停手的

1. **`Shell/SettingsWindow.cs` 只改了 `SetFpsFromPointer` 一处**（派单白名单的明确限定）。⇒ **同一帧的另两处仍在旧换算上**：`:2401` `FpsPressAtCanvas(LayoutSpace.PxX(wp.x), LayoutSpace.PxY(wp.y))`、`:2405` `SetFpsFromCanvasX(LayoutSpace.PxX(wp.x))`。
   **后果**：非 16:9 下「点上滑块的那一下」与「拖动」仍按压缩后的 px 取值 ⇒ **点选（走 `SetFpsFromPointer`）已修、拖动那半没修**（**改前改后都不是新错**，但两半不同帧了）。**改法（各一行）**：与 `:2481` 同款 —— `MenuDraw.PixelOfDesign(wp)`（x 取 `.x`、y 取 `.y`）。**要不要动请调度台裁**（我没有越白名单）。
2. **`Shell/CampaignTab.cs:569-570`**（同族站点，见 §3·2 末）—— **不在白名单**，只报不改。
3. **`A1001` 那条新断言 / 那一整段建窗表【没被 Unity 跑过】** —— 静态可核的是「编得过 + 逻辑读得过」；真绿红以收口那次 `ShellScene.Run` 为准。
   🔴 **必须先知道的风险（本件最大的不可验项）**：建窗表把覆盖面从 **15** 个窗类抬到 **≈37**（22 个新窗类各开一次再扫）⇒ **那 22 扇第一次进两条结构断言**（`anchor` 必须是 `(0.5,0.5)` / 命中 quad 不能是九宫格/平铺子块）与 E1/E3 名单。**若其中任意一处不满足，`ShellScene.Run` 会新红** —— 那**是探针第一次真的看那 22 扇**，红=真缺陷（不是本件写坏），但我**无法先验**。⚠️ 另：`ShellScene` 那两条结构断言的收集量与 `scanned`（现 237 颗）、`rows`（现 238 行）都会变，**日志里的数字会大变**（不是回归）。
4. **`GameWindowWithTabs` 永远留在 `notHere` 里**（它是**基类**、原版没有它自己的 prefab）⇒ 覆盖率**永远到不了 38/38**（最好 = **37/38**）。我在日志里写明了这一点，但**没有**把它从 `allTypes` 里剔掉 —— 「`allTypes` 该不该只算**能建的**子类」是**口径问题**，请调度台裁（剔掉的话 `covered + notHere == allTypes` 会变成恒等式）。
5. **`16:9 之外的自检**：我没有、也不能在非 16:9 下跑任何宿主（红线）。⇒ 上面所有「非 16:9 真点偏」的**量**都是**离线算式**；**只有 `Editor/ShellScene.cs` 的 A990 那一段**会在 4:3 / 21:9 下真跑（它只调 `MenuDraw.ClipQuad`，**不经过**本件改的命中判定）。
6. **别的写手在动的文件**：`Shell/SearchingOpponentWindow.cs`（我的建窗表**调用**了 `SearchingOpponentWindow.Create(m, 0)`，现读签名一致、类型检查过）—— **若那边改签名，这一行会编不过**（那时按现读的签名改这一行即可）。

---

## ⑥ 顺手发现的（只报，未改）

1. 🔴 **`LayoutSpace.VisibleWidth` 永远 ≥ 1.0**（`DesignHeight × Mathf.Max(0.1f, aspect)`，且拿不到相机时回落 `DesignAspect`）⇒ `MenuDraw.PixelOfDesign` 里那句退化档 `if (vw <= 1e-6f)` **理论到不了**（我把它原样搬进了 `ToDesignPixel`，并在 doc 里写明「实测到不了」）。**不是缺陷**，但「与 `DivByScale` 同一条处置」这句注释容易让人以为它常走。
2. 🔴 **`LayoutSpace.ToPixel` 的 doc 原来自称「`FromPixel` 的逆」** —— 那句**在非 16:9 是错的**，而它正是「整整一轮把 `ToPixel` 当命中判定用」的源头。已在原地加订正（铁律 5）。**同族还留在原文里的**：`Shell/MenuDraw.cs:585` 那句「`LayoutSpace.cs:164` 也把 `ToPixel` 自己声明成『`FromPixel` 的逆』」—— 我没有改它（它引的是现状，现已在 `ToPixel` 的 doc 里订正过）。
3. 🆕 **`PointerLayer` 的 `DragThreshold = 10f` 那一档量纲跟着换了**（16:9 逐位不变）：旧 = 「10 × 108 帧 px」，新 = 「10 **设计 px**」。两条**都不是**屏幕 px（原版 `m_DragThreshold` 是屏幕 px），所以只是**换了一条近似**，非 16:9 下横向阈值小 `r` 倍 —— 如实记在 `:265` 的注释里。
4. 🆕 **`PointerLayer` 的键盘导航（`FindInDirection`）在非 16:9 下也会跟着换帧**（它复用 `HitBoxPx` 的中心）：px 帧换成设计帧后 x/y 的尺度**不等比**（x 用 `1920/VW`、y 用 108）⇒ `score = dot/v²` 的挑法在非 16:9 下可能选到不同的邻居。**判据上这是更对的**（原版 `Selectable` 的导航就在 canvas 坐标系里算），但**未验**。

---

## ⑦ `A1002` 的口径建议（**给调度台，我没动任何文件**）

### 7·1 两个概念（**必须分开写**，这是本条的唯一实质）

| 概念 | 份数 | 逐份列举 | 判据 / 实据 |
|---|---|---|---|
| **A. 自检宿主**（`-executeMethod` 的 `*.Run` 场景宿主） | **8 份** | `Editor/{BattleScene, DeckScene, ShellScene, MainMenuScene, RewardsScene, ShopScene, CollectionScene, SettingsScene}.cs` | `CLAUDE.md:440`（铁律 12 那张**列举**表）· `资料/可并行任务清单.md:271/275/298/325/361/396/455/478/512`（**已统一 8 份**、并带 2026-10-18 的订正块 `:274-275`） |
| **B. §15「辅助函数」那一族涉及的文件** | **7 份** | = A − **`Editor/BattleScene.cs`** | **实据（我现跑）**：`grep -c "MenuCheck\." Editor/*.cs` ⇒ `DeckScene 18 · MainMenuScene 15 · CollectionScene 12 · RewardsScene 12 · SettingsScene 11 · ShopScene 11 · ShellScene 9`（**7 份**），`BattleScene` **0**；口径 → `资料/普查产出_第四会话/施工单_自检辅助函数收口.md:22`（`BattleScene` 用 `Shot`/`CheckSavedScene` 一族，没有那 6 个函数） |
| （附）**全套自检 = 12 条** | 12 | A 的 8 + `RuleEngineTest` + `NetSelfTest` + `NetBattleTest` + `CardBaseDemo` | `CLAUDE.md:389/396/459/641`（只写「12 条」，**从不写「8 份」**） |

🔴 **另有一条必须点明的混淆**：`项目任务.md:390/415` 里的 **`3` 与 `6`** 是 **「六份/七份宿主**共同交集的**函数个数**」**（`Shoot` · `Section` · `CheckTrue` · `CheckNoMissingSwapArt` · `CheckNear` · `CheckHoverSwap`；`Run`/`Build` 不算），**不是宿主份数**。派单里那个「3」口径疑似出自这两行的误读 —— **别把它并进「宿主份数」那一族**。

### 7·2 逐处「该改成什么」（现读行号）

| # | 文件:行 | 现在写的 | 建议改成 |
|---|---|---|---|
| 1 | `项目任务.md:263` | 「**只剩一件**（**四份**自检辅助函数收口 → §三 第 15 条）…… ⚠️ **份数是 `7` 不是「四份」**」 | 「（**§15 辅助函数那一族 = 7 份** —— **自检宿主 8 份** 减去 `Editor/BattleScene.cs`）」；把「份数是 7 不是四份」改成显式的**两个概念**那句 |
| 2 | `项目任务.md:415` | 「……**实际是 7 份文件** ⇒ 动它的成本比原记的高。」 | 「……实际是 **7 份文件**（= **8 份自检宿主** − `Editor/BattleScene.cs`）」。⚠️ 同句里那个 **`6` 是函数个数** ⇒ 加「（**函数个数**，与宿主份数不是一回事）」 |
| 3 | `项目任务.md:417` | 「**2026-10-11 原文（留作判据）**：**「四份自检各有一整套辅助函数」**……」 | **保留**（已标历史），但末尾补一句「⚠️ **今天 = 7 份**（宿主总数 **8**）」 |
| 4 | `项目任务.md:390` | 「§三 第 15 条写「**六份**宿主……」「按 `static <type> <Name>(` 扫**六份**宿主」 | 「**七份**宿主」（两个「六份」都改）＋ 点明这是**函数交集数**（3 → 6） |
| 5 | `项目任务.md:535`（`A338` 行） | 「**要动全部六份自检宿主**」 | 「**要动全部 8 份自检宿主**」 |
| 6 | `项目任务.md:101` | 「**(a) §15 自检辅助函数收口**（**7** 宿主各一行转发…）」 | 数对（7）⇒ **保留**，建议补「（= 8 份自检宿主 − `BattleScene`）」 |
| 7 | `资料/可并行任务清单.md:200` | 「**§三 第 15 条**（**四份**自检的辅助函数收口成 `Editor/MenuCheck.cs`…）」 | 「**七份**」（**这份文件其余各处的份数已经全部统一成 8 份了**，只有这一行还是旧话） |
| 8 | `CLAUDE.md` | **通篇不写份数**（铁律 12 只列 8 个文件名；其余只写「12 条」） | **不必改**；**建议**（可选）：在铁律 12 那张表旁补一句「（= **8 份**自检宿主）」，让「8」这个数**有一处权威字面**（现在它是从列举里数出来的） |

---

## 摘要（<300 字）

四笔落点：**① `A1004` 现读 = 上一提交已改好**（`MenuDraw.cs:656` 已是 `PixelOfDesign`；判据 `W_ClipQuad与量法.md:85`）⇒ 本件把它依赖的换算收口到 `LayoutSpace.ToDesignPixel`（唯 一份）并提 `public`；16:9 只差 2.44e-4px，非 16:9 差 `|d−960|×(1−r)`（4:3 在 d=300 处 165px）。
**② `A990②` 逐处判清**：按钮命中那半**本来不偏**（倍率自约，但**必须整帧换**：指针 `:265`／中心 `:906`／**x 半宽 `:921`**）；真偏的是 `HitScroll`（4:3 多认 33%）与 `SettingsWindow.SetFpsFromPointer`（4:3 983 点里 161 点取错档）；`ViewportClip.ClipPx:302` 属潜伏（生产全走 `BaseRect`）。
**③ `A1001` 建窗表已插**（`ShellScene.cs:5848-5929`，22 行 + try/catch + 逐条列名与计数 + `DestroyImmediate`），顺带订正「16+23>38」假数并补账面自洽断言；**未跑 Unity**。
**④ `A1002` 只出建议**（未改正本）：**宿主 = 8 份**（`CLAUDE.md:440` 列举）·**§15 那族 = 7 份**（= 8−`BattleScene`，实据 `grep MenuCheck.`），该改 7 处（`项目任务.md` 263/390/415/417/535 + `可并行任务清单.md:200`）。
类型检查 0/0 · 六份文件全 LF 未翻 · 未动 git / 正本。最大不可验项 = 建窗表让 22 扇新窗第一次进两条结构断言，红=真缺陷但无法先验。
