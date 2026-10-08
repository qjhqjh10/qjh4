# W_ClipQuad与量法（`A990` + 量法收口那半）—— 执行写手

白名单 = `Shell/MenuDraw.cs` · `Editor/ShellScene.cs`（**只动了这两个**；⛔ 没跑 Unity · 没动 git · 没改正本）。
改动：`MenuDraw.cs` **+44/−2** · `ShellScene.cs` **+414/−1**（⚠️ 后者含本会话之前就未提交的改动；我这批 = §⑤·d-5 那段 + `TmpSpanPx` doc 那段）。

## ① 两条换算的式子 · 偏量表 · 读点清单 · 可观测性判定

### 式子（`px` = 原版设计 px、左上原点；`w` = 世界 x；`VW = VisibleWidth`）

| 方向 | 函数 | x 的算式 |
|---|---|---|
| **建件（写）** | `LayoutSpace.FromPixel`（`MenuDraw.Local`/`RectCenter` 转调） | `w = (px/1920 − 0.5) × VW` |
| **读回（改前）** | `LayoutSpace.ToPixel` = `PxX` | `px' = 960 + w × 108` |
| **读回（改后）** | `MenuDraw.PixelOfDesign`（新，私有） | `px = (w / VW + 0.5) × 1920`；y 转调 `LayoutSpace.PxY` |

- **y 恒闭合**（两个函数本来就同值：可见高恒 10 单位 = 1080px ⇒ 108 px/单位是**真的**）⇒ A990 只涉及 x。
- 往返倍率 **r = 108 × VW / 1920 = VW / DesignWidth = aspect ÷ (16/9)**（`px' = 960 + r(px − 960)`）。
- ⇒ **「有效裁切框」= 原框绕画布中心 (960,540) 缩放 `1/r` 倍**（x 轴）：`[960+(x1−960)/r, 960+(x2−960)/r]`。

| 宽高比 | `VW` | `r` | 有效框（原框 ×1/r） | 后果 | 我探针框 [600,1000] 的实测（离线模拟） |
|---|---|---|---|---|---|
| **16:9** | 17.7778 | **1.0000** | 原框 | ✅ **闭合** | 旧实现偏差 **0** |
| 16:10 | 16.000 | 0.9000 | ×1.111 | 少夹 11% | 0.3333（探针 560） |
| **4:3** | 13.3333 | **0.7500** | ×1.333 → `[480, 1013.3]` | **少夹 33% ⇒ 文字画到视口外** | **0.2778**（探针 560） |
| 5:4 | 12.500 | 0.7031 | ×1.422 | 少夹 42% | 0.2604 |
| **21:9** | 23.3333 | **1.3125** | ×0.762 → `[685.7, 990.5]` | **多夹 24% ⇒ 框内被提前切掉** | **0.9722**（探针 640/680） |
| 32:9 | 35.5556 | 2.0000 | ×0.500 | 多夹 50% | 1.4815 |

（「实测」= 我按同一套公式离线算的**世界单位**偏差；新读法在**每一档**都是 **0.000000**。）

### 读点清单（谁在用「读回来的 x」· 全文件计数见 ② 表末）

- **`ClipQuad` 内部**：唯一读点（改前 `MenuDraw.cs` 的 `q[i] = LayoutSpace.ToPixel(...)` 那一行）；产物只喂 ① 夹不夹这个**判断** ② 回写几何 ⇒ **没有外部读者** ⇒ 它不是「查询类」缺陷，是**几何类**（夹多了 / 夹少了；被夹的角本身照旧精确落在框沿上）。
- **同一对换算的另一处 `MenuDraw` 读口**：`QuadRectPx`（读）= `PlaceCell`（写，走 `FromPixel`）⇒ 非 16:9 下软边的 `SameRectNear(…, 0.05px)` 判据失配（**没改**，见「待裁」）。
- **命中判定（真·可观测）**：`Shell/PointerLayer.cs:251/877` · `Shell/ViewportClip.cs:289`（`ClipPx`）· `Shell/SettingsWindow.cs:2127` · `Shell/CampaignTab.cs:569`（鼠标/节点世界 → px → 与设计 px 框比）。
- **自检标尺**：`ShellScene.QuadPxRect/PiecesOutsideClip/SpanOfTmp` · `MainMenuScene`(33) · `CollectionScene`(17) · `BattleScene`(10) · `RewardsScene`(6) …

### 可观测性判定

- 🔴 **今天在自检里【不可观测】**：**建 UI 的那些宿主一律在「建树之前」把 `cam.aspect` 钉成 `DesignAspect`**（`ShellScene.cs:572` 等）⇒ **没有一条断言能照出它**（A990 是**潜伏缺陷**，「判据成立」这一半靠**判据推理 + 离线算式**，不靠跑）。
- 🔴 **运行时【可达】**：`ShellRuntime.cs:179` 只 `LayoutSpace.Apply(cam)`（正交 / 可见高 10 单位），**不钉 aspect**；钉 aspect 的那几句只在**编辑器建场景**那条路上 ⇒ 真机窗口不是 16:9（4:3 屏 / 21:9 / 窗口化拖动）时 `VW ≠ DesignWidth` ⇒ 按上表：**4:3 上文字画到视口外、21:9 上框内被提前裁**。
- 🔴 **顺带（读代码推得、⛔ 未验、比 A990 大）**：同一对式子还管**尺寸** —— `MenuDraw.SetPxSize` 的宽高走 `LayoutSpace.Px`（常量 108），而**位置**走 `FromPixel`（`VW`）⇒ **非 16:9 下尺寸不跟着位置缩放**（4:3：位置压 0.75、尺寸不动 ⇒ 元素互相叠）。`SmallScreenUI` 那套**不是**aspect 补偿器（`Shell/TransformScalerBySmallScreenUI.cs` 文件头：判据里**没有任何宽度/屏宽判定**、出厂关、每窗 `extraScaleSmallScreen` 是 1.07/1.2/1.35 那种「放大 UI」）。⇒ 与 A990 **同根**（「全局只有一条换算」这条判据还没落），**只报不改**。

## ① 改 / 没改（为什么）· 断言

**改了**（`MenuDraw.cs`）：① 新增 `PixelOfDesign(Vector3)` = `FromPixel` 的逆（x 用**实测** `VW`；y 转调 `PxY`）；② `ClipQuad` 的读回改走它；③ `ClipQuad` 的 doc 补「读/写必须是同一条换算的互逆两条」这条不变量；④ `PixelOfDesign` 的 doc 写明「**逐字对偶**：`FromPixel` 那边一改这一行必须跟着改，否则又变回 A990」。

**为什么只能改读、不能改写**：写回那一半必须是**建件那条换算**的逆（框沿自己就是 `MenuDraw.Node`/`ApplyPxRect` → `Local` → `FromPixel` 摆出来的）—— 改写回 = 「夹到框沿」夹到的不是画出来的框沿。

**为什么只动这一个读口**：`QuadRectPx`（同文件、同一种配对，改一行即可）的判据落在 `MainMenuScene`/`RewardsScene`（**不在白名单**）· `LayoutSpace.ToPixel`/`PxX` 的 ~90 个调用点跨 `Core/` 与十几个宿主 ⇒ 按派单「波及多处 ⇒ 只报告 + 给方案」。

**16:9 零回归**：`PixelOfDesign` 与 `ToPixel` 只差 float 舍入（`1920/17.777779` 实得 **107.99999** ≠ 108 ⇒ 整屏 ≤ **2.5e-4 px**），远小于本壳一律在用的 0.05px 容差。退化档（`VW ≤ 1e-6` = 没相机）**退回旧口**（与 `DivByScale` 同一条处置，不静默）。

**断言**（`Editor/ShellScene.cs` **§⑤·d-5**，插在 A233 块之后、A464·B2 块之前 —— 那两个「节点态计数」不变量因此不受影响）：

- 名字：`★ A990：ClipQuad 的 x 往返在非 16:9（4:3 · 21:9）下闭合（判据 = FromPixel 的逆）`
- 形状：**直接调公共件** `MenuDraw.ClipQuad`（⛔ 不经 `Label`/TMP —— 字形落在哪由字体度量定、控不住），喂 4 个**由 `FromPixel` 造出来**的角；框 `[600,1000]×[200,600]`（**故意偏左**：偏差正比于离画布中心多远）· 探针 x = 40 / 560 / 640 / 950 / 1400（另 +40 的伙伴角）· y 全在框内（只考 x）。
- **两态**：`4:3` + `21:9`（**旧实现红**）· `16:9`（**旧实现也过** ⇒ 证明改动只动非 16:9）。跑法 = 临时 `cam.aspect = 4/3 → 21/9 → DesignAspect`，`finally` 里**还原 aspect + 销毁探针**。
- 断什么（期望值**全部来自 `FromPixel`**，⛔ 不从被测实现读）：每个角夹完后**世界 x = `FromPixel(Clamp(设计 px))`**（tol **0.002 世界单位** ≈ 0.22px = float 往返余量）；+「整块在框内四角一个都不许动（返回 false）」；+ 两条前提（该档确实非 16:9 · 探针两头都有）。
- **牙口（离线模拟同一算式）**：新读法 4:3/16:10/21:9/32:9 **worstNew = 0.000000**；旧读法 4:3 **0.2778** · 21:9 **0.9722** · 16:9 **0** ⇒ 容差有 **~140×** 余量。
- **改坏法**：① 读回换回 `LayoutSpace.ToPixel` ⇒ 非 16:9 两档红、**16:9 仍绿**（正是 A990 的病灶）；② 写回那半边改成常量 108 ⇒ 也红；③ 探针 aspect 钉回 16:9 ⇒ 前提条红。
- **灭自证**：期望值走 `FromPixel`（建件那条路 = **独立一方**），与被测的读**不是同一个函数**；且**成对** —— 只断「夹到框沿」会放过「一律缩到框沿」那类实现 ⇒ 另加「框内的角不许动」+「探针两头都要有」。⚠️「旧读法偏多少」那个数**只打不判**（全局收口 `ToPixel` 那天它会变 0，那时本节**照旧有效**）。
- 条数：**+13 条**（`资料/交接_1018第三会话.md:16` 记 `ShellScene` **944/0** ⇒ 若全过应变 **957/0**；⛔ 我没跑 Unity，**这个数没验**）。

## ② 三份量法的差异 · 收口结论

**结论：这一半【已经收口了】（2026-10-16 · A844/W17 两批）⇒ 无需改代码，只现核了一遍。**

- 唯一一份实现 = `Editor/ShellScene.cs` 的 **`SpanOfTmp`（`:241`）**；两个入口 `TmpSpanPx(Label,…)`（`:192`）· `TmpSpanPx(Transform,…,out int)`（`:217`）。
- 派单点名的三份**现状**：`MainMenuScene.SpanOf`（`:10540`）= **转调**（丢 y、`includeInactive` 默认）· `MainMenuScene.A822SpanX`（`:7196`）= **转调**（显式 `includeInactive: true`）；同族另两处也已是转调：`DeckScene.TextMeshRectPx`（`:402`，`(true)`）· `CollectionScene.TextExtentPx`（`:301`）。
- **三份的差异只有两点**（收口时逐条保留）：① **出参**（`SpanOf`/`A822SpanX` 只给 `(mnX,mxX)`；`TmpSpanPx` 给四条边 + 顶点数）② **`includeInactive`**。**算法一个字没差** ⇒ 收口没改读数。
- **有意不收**的同族（`SpanOfTmp` 的 doc 已写明，我逐处核过现状一致）：`RewardsScene` 的 `TmpVertPx`/`TmpVertsAndAlpha`/`TmpGlyphUvW`（要**逐点序列**）· `MainMenuScene.CountSoftFadedTextVerts`（按 y 带数 alpha 剖面）· `IconSizeProbe`/`Round1015Probe`/`CardFaceProbe`（**离树合成** TMP、局部单位）· `CollectionScene.cs:6362`（按带比 alpha，不是取包围盒 —— 同上族）⇒ **产物形状不同**（序列 / 计数 / 剖面），并进来要么改契约要么加开关，**不是「互相够不着」那种重复** ⇒ **不是「不能收」，是「不该收」**。
- 🔴 **收了但那把尺子本身带 A990 的毛病**：它内层用 `LayoutSpace.ToPixel`（x 写死 108）⇒ **只在 16:9 自洽**。今天无损（12 宿主全钉 16:9）。已在 `TmpSpanPx` 的 doc **就地写明**（+7 行）：⛔ 别在非 16:9 的断言里拿它当判据 —— 本件新落的 A990 断言因此**不用这把尺子**（直接比世界坐标 / `FromPixel`）。

## 类型检查 · 行尾 · 没查清

- **类型检查**：`TMPDIR=/tmp/wf_md bash d:/4/Unity/工具/typecheck.sh` —— 我每改完一版都跑：**前 3 次运行时 0 / 编辑器 0**；**最后一次（收尾前）变成了 2 个错**，但**两个都不在我这两个文件里**：
  · `Assets/CardPresentation/Battle/BattleDriver.cs(3327,60)` CS1061 —— `AlliancePanelWindow` 没有 `HitBody`（**别人正在写的文件**，本轮有 `W_AlliancePanel.md`）
  · `Assets/RuleEngine/Editor/RuleEngineTest.cs(195,14)` CS0103 —— 找不到 `TestCemeteryEvents`（**引擎族那条线的文件**）
  ⇒ 按铁律 13·3 的判据（错**全部**集中在不是我负责的文件上 ⇒ 不是我引入的），我**没去动别人的文件**；本件两个文件在四次里**一次都没有报错**。
  🔴 **给调度台**：**整个运行时/编辑器程序集现在编不过** ⇒ 同步点想跑 `ShellScene.Run` 之前，得先等那两个文件收口（否则一行自检都跑不起来）。
- **行尾**：两个文件都是**纯 LF**（`b.count(b'\r\n')` = 0，改前改后都是 0；`MenuDraw` 2935→2977 行 · `ShellScene` 5950→6070 行）。`git diff --numstat`：`MenuDraw.cs 44/2` · `ShellScene.cs 414/1`。⛔ 没用 `sed -i`。
- **没跑 Unity** ⇒ 新断言 **没实跑过**；算式是离线 python 按同一套公式模拟的（表在 ①）。「编译过 + 逻辑核过」≠「跑绿」。
- **待裁 / 没查清**：
  1. `MenuDraw.QuadRectPx`（`:625`）与 `PlaceCell`（`:1479`/`:1540`）是**同一对**（写 `FromPixel` / 读 `ToPixel`）⇒ 非 16:9 下 `SameRectNear` 失配。**方案（一行）**：`:625` 改 `PixelOfDesign(PosInDesignSpace(q.transform))`（16:9 与今天差 ≤2.5e-4px）。⛔ 没动：判据在 `MainMenuScene`/`RewardsScene`（不在白名单），`ReapplySoftEdges` 那一路我没验过。
  2. `LayoutSpace.ToPixel`/`PxX` 的 **~90 个调用点**（MainMenuScene 33 · CollectionScene 17 · ShellScene 15 · BattleScene 10 · RewardsScene 6 · CardDisplayWindow 5 · MenuDraw 4 · ShopScene 4 · BattleLogPanel 4 · SettingsWindow 3 · PointerLayer 2 · `ViewportClip`/`TrophyInfoPopup`/`CampaignTab`/`Round1015Probe`/`DeckScene`/`BattleDriver` 各 1）——**命中判定那几处**在非 16:9 下真可观测（点偏）。要动先裁「哪一条是全局口径」（①末条），跨 `Core/` 与十几个宿主。
  3. ①末那条「尺寸不跟位置缩放」是**读代码推出来的**，**没跑过 4:3 的壳** ⇒ 记为**未验**。
