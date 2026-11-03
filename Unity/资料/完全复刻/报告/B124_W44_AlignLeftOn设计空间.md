# B124_W44 · `B123-ARMYTITLE` —— `AlignLeftOn` 的「设计帧 → 父局部」换算（2026-11-03）

> 文件所有权：只动 `CardPresentation/Battle/Label.cs`（+ 新建 `Editor/B124_W44_Assert.cs`）。`MenuDraw.cs` 只读、
> 未改任何 `Editor/*Scene.cs`（⛔ 没动 git / Unity）。typecheck（`TMPDIR=/tmp/wf_b124w44`）= **运行时 0 / 编辑器 0**。

## 一 改法（= `Label.cs:1937-1940` 那段 doc 早就写死的选项 (c) / A228）

`worldLeftX` 现读确认是**设计帧的 x**（`MenuDraw.AlignLeft` 喂 `LayoutSpace.FromPixel(r.x1,0f).x`），
而写进去的是 `localPosition` ——差**一整条父链**。

| | 式子（`P` = 父世界 x，`k` = 父 `lossyScale.x`，`W` = `WorldW`） | 渲染出来的左缘 |
|---|---|---|
| **改前** | `localX = worldX − P/k ± W/2` | **恒 = `k·worldX`**（`P` 在式子里**整体约掉**） |
| **改后** | `localX = parent.InverseTransformPoint(root.TransformPoint(worldX)) ± sign(k)·W/2`（`root` = 顶层祖先 = 设计帧） | **恒 = `root.TransformPoint(worldX).x`** |

`Label.cs:1951` 的 `ParentXInDesignSpace(float worldX)` 现在返回**父局部 x**（不再是「父的世界 x 除 k」）；
两个写入点 `:1773` / `:1792` 只改调用形状。**退化档原样保留**（`k` 非有限 / `≈0` ⇒ 不除 `k`，与旧写法逐位一致；
无父 ⇒ 直接返回 `worldX`）。类尾新增 `ParentScaleSign()`（`:2799`）：`k < 0`（镜像父链）时半个字宽要反号，
否则整块再偏一个文字宽。

**为什么批处理一直是绿的**：`k == 1` 且设计帧（顶层 `Shell` 根）在世界原点 ⇒ 两个式子**逐位恒等**
⇒ `DeckScene.Run` 读到 `worldPx = 35.96`、父 `lossyScale = 1,1` 全对；只有真机上父链带缩放/镜像/根偏移时才分叉。
`Editor/SettingsScene.cs` 的 A228 两态断言仍逐值成立（开态那一份夹具的**顶层**就是被乘 1.2 的窗根 ⇒ 渲染 = `1.2·x`）。

## 二 断言 → **新建** `Editor/B124_W44_Assert.cs`（类在全局命名空间，可 `-executeMethod B124_W44_Assert.Run`）

期望值**全是字面量**，⛔ 不用被测函数算期望；量的是**渲出来的左/右缘**（`transform.position ± WorldW×lossyScale`，量法同 A228）。
| 组 | 夹具 | 期望 | 改回 `P/k` 时 |
|---|---|---|---|
| A | `k=1` + 根在原点 | `localPosition` 与旧式**逐值相同**（生产 20 个调用点的回归闸）+ 左右缘 = `wantX` | 仍绿（有意） |
| B | 根挪到 `x = 3.5` | 左缘 = `3.5 + wantX` | **红**（旧式给 `wantX`，差 3.5 世界单位） |
| C | 根与父之间夹 `k = 1.2` | 左缘 = `wantX` | **红**（旧式给 `1.2·wantX`，偏 0.2·wantX ≈ 88px） |
| D | 同上但是 `k = −1`（镜像） | 左缘 = `wantX` | **红**（旧式给 `−wantX − W`，整块翻到屏幕另一头） |

B/C/D 覆盖三种不同父链 ⇒ 任何「单一回退常量」都同时满足不了（灭自证）。⚠️ **未接线**：宿主里加一行
`B124_W44_Assert.Run(); _sink.Pass += B124_W44_Assert.Pass; _sink.Fail += B124_W44_Assert.Fail;`
（建议插在 `Editor/SettingsScene.cs` A228 那一节之后 —— 该文件不在本代理白名单，请调度台派人接）。

## 三 核过的东西

- **行数**：`Label.cs` 2795 → **2805**，`+10` 全在**类尾**（最末账落点 `:2696`，⛔ 零漂移）。
  `python 工具/check_ledger_points.py` 全表 **漂移 0 / 失败 0**。行尾全 CRLF（2805/2805，裸 CR 0）。
- `ParentXInDesignSpace` 仅 `:1773` / `:1792` 两处调用（无外部引用）；新文件 LF，与同目录 `B124_W10_Assert.cs` 一致。

## 四 如实登记：这条修法**还不能核销**，两件事要真机/宿主确认

1. **本代理没跑 Unity**（红线）⇒ 断言一条都没实跑过，只有 `typecheck 0/0` + 逐条手推算式。
2. 🔴 **我量了真机实拍，发现「纯缩放」解释不了那个读数** —— `08_filter.png`（2560×1440）里「军队」的
   墨迹 x = **1830..1915 实际 px** ⇒ 1920 设计帧 **1372..1436px** ⇒ 世界 **3.8..4.4**（字**没有镜像**）。
   而旧式渲染 = `k·worldX`（`worldX = FromPixel(2.2) = −8.87`）：要落到 +3.8 得 `k ≈ −0.43`，
   但负 `k` 会把字镜像 —— 实拍是正字。⇒ **真机那个值不是「单纯父链缩放」能给的**，另有机制
   （最可能：`AlignLeftOn` 当时走了 `!HasMeasuredWidth` 的早退、字停在 `Txt()` 给的**绝对** `localPosition`，
   之后父链才被摆到别处）。新式把「渲染结果 = 设计帧映射」**钉死**，对上述两类都成立；
   但**若真因是「`AlignLeftOn` 根本没执行」**，这条修不到。
   ⇒ **建议核销前先跑一次 `DeckScene.Run` 看 D43 那三条的读数**，或按 `判据/B124_对账_B_界面收藏.md` §一·6 的探针
   在真机读 `_cosmoArmyTitle.localPosition` / 父 `position` / `lossyScale` / `BasePos`。
