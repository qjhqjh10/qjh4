# E22 · `MenuDraw.QuadRectPx` 的 x 尺寸项（**先判**）+ `CheckAbsorbRule` 同族双料（**已改**）

> 执行代理 E22 · 2026-10-18 · 白名单 = `Shell/MenuDraw.cs`（第 1 件那一处 + 第 2 件那一处）· `Core/LayoutSpace.cs`（**未动**，见 §④）
> ⛔ 本件**没跑 Unity**（红线）。行号一律**现读**（改完之后数的），若漂了请按**符号**认。

---

## ① 结论

### 第 1 件：`QuadRectPx` 的 x 尺寸项 —— **不是缺陷，`K = 108` 是对的**（只改注释，已落）

**判据 = 派单第 2 步要的那一句：「ImageQuad 摆到画布上时，它的宽度用哪个斜率换算？」——答案是建件那条换算，= `108`。**
建件侧写的是 `SetWorldHeight(LayoutSpace.Px(h))` + 紧跟一句 `SetAspect(w/h)` ⇒ **`WorldW = w ÷ 108`**；
读回侧写的也是 `× K`（`K = DesignPxH / DesignHeight = 108`）⇒ **两侧斜率相等 ⇒ 命中/恢复判据成立 ⇒ 不是缺陷**。

派单里那条「内部矛盾」**成立一半，但矛盾不在这一行**：

- `QuadRectPx` 的**中心项**走 `PixelOfDesign`（= `FromPixel` 的逆，x 用**实测** `VisibleWidth`）、**尺寸项**走 `K` ——
  这**不是自相矛盾**，而是**逐条对着建件那两条**：建件的**位置**用 `FromPixel`（x 用实测宽）、建件的**尺寸**用 `Px`（108）
  ⇒ 读口要当逆，就**必须**一条对一条。**两条路只在 16:9 重合**（见 §①·b 的表）。
- 真正在非 16:9 下不成立的是**建件侧**：位置跟着 `VisibleWidth` 压、尺寸不压 ⇒ 4:3 上元素互相叠
  = 已登记的 **`A990①`**（`LayoutSpace.cs` 那条「全局只有一条换算」还没落）。**`QuadRectPx` 只是忠实照出它。**

🔴 **把 x 换成 `PxPerWorldX` 会坏三处**（派单没预期到这一层，逐条给了实据）：

1. `ApplySoftEdges` 里 `if (!SameRectNear(QuadRectPx(q), vis)) PlaceCell(…)`（`:913`）——
   非 16:9 下**恒不等** ⇒ 每次重切都 `PlaceCell`（那一条的语义本来是「整块在框内 ⇒ 一个字都不动」）。
2. `ClipNineChildren` / `ClipTiledChildren` 是「**读回（`QuadRectPx`）→ 求交 → 再 `SetWorldHeight(LayoutSpace.Px(cr.H))`**」
   的**闭环**（`:1614`/`:1673` 读，`:1627`/`:1688` 写）。**两边同斜率才幂等**；只换读口 ⇒ 同一块每跑一趟再被切一次（**越切越窄**）。
   离线算了 4:3 的一例（框 `[0,100]`、裁 `[50,150]`）：今天是「宽对、中心偏 8.3 设计 px」，
   只换读口之后变成「中心对、宽 = 1.333 倍（两侧各溢出 ≈11 px）」⇒ **两种都错，但没有更好**，而裁切管线会失去幂等性。
3. 🔴 **契约副本**：`Shell/ViewportClip.cs` 的**两支**都是 `K`（`KB` 与 `K` 两个常量，注释写着「**与 `MenuDraw.QuadRectPx` 同一份口径**」）。
   只改 `QuadRectPx` = **两套口径并存**（CLAUDE.md §三），而 `Shell/ViewportClip.cs` **不在本批白名单** ⇒ 按派单红线「需要改 ⇒ 停手写进报告」。

**若换「相机实绘」那一读法（= mesh 的世界宽 × `PxPerWorldX`），则 x 尺寸项**确实是**不成立的**（4:3 下读回比画出来的窄 0.75×）
 —— 但那一读法要**成对修**：**建件那 5 处（`Rect` / `PlaceCell` / `ClipNineChildren` / `ClipTiledChildren` / `SetPxSize`）+ 全部 frame 读口
（`QuadRectPx` · `ViewportClip.ClipPx` 两支）一起换斜率**，其中 5/7 处不在白名单。**⇒ 本件按派单「停手报告」，一行算式都没动**，
只把裁定与「⛔ 别单改这一行」写进 `QuadRectPx` 的 doc（那是下一个会话会站的那一格）。

### 第 2 件：`MenuDraw.CheckAbsorbRule` 第 ③ 步 —— **真缺陷（同族双料），已改**

- **① 帧错**：中心项 `PxX/PxY`（108 帧）+ 长度项 `× 108f`，比的是**字面设计 px** ⇒ 非 16:9 下四条 `near` 全错（偏 `r = VisibleWidth/DesignWidth`）。
- **② 父链错**：`q.transform.position` **没过 `PosInDesignSpace`** ⇒ 窗根被 `TransformScalerBySmallScreenUI` 乘 M（或窗内自带缩放的节点）时多一层 `M`。
- **改法照上一轮指的方向**：**转调 `QuadRectPx`**（同文件唯一一份正确的口；`A1003` 已把 7 个宿主 / 17 个函数位收到它，**唯独本函数自己这一份没收**），
  ⛔ 没重写算式，⛔ 没改用 `PxPerWorldX`（按 §① 的裁定：尺寸项仍是 `K`）。
- **出厂态（16:9 + M == 1）读数逐条不变**：`PxPerWorldX` 实得 107.99999 ⇒ 与 `PxX` 差 ≤ 3e-4 px，远小于本节 1.5px 容差。

---

## ①·b 第 1 件的代数推导（口径在 16:9 / 4:3 / 21:9 各自的值）

**定义**（全部现读自 `Core/LayoutSpace.cs`）：

| 量 | 式子 | 出处 |
|---|---|---|
| `VisibleWidth` `VW` | `DesignHeight × aspect = 10 × aspect` | `:62-69` |
| `DesignWidth` | `10 × 16/9 = 17.77778` | `:43` |
| `K`（**y** 的斜率） | `DesignPxH ÷ DesignHeight = 1080 ÷ 10 = 108` | `:154` |
| `PxPerWorldX`（**世界 → 画布 px 的 x 斜率**） | `1920 ÷ VW`（= `FromPixel` 的逆式的斜率） | `:221-228` |
| `FromPixel(xPx,yPx)`（**建件**：设计 px → 世界） | `((xPx/1920 − 0.5)·VW, (0.5 − yPx/1080)·10)` | `:157-158` |
| `ToDesignPixel(w)`（**读回**：世界 → 设计 px） | `(w.x·PxPerWorldX + 960, 540 − w.y·108)` | `:242-245` |
| `PixelOfDesign` | 一行转调 `ToDesignPixel` | `MenuDraw.cs:605` |
| `Px(px)`（**建件**：设计长度 → 世界长度） | `px ÷ 108` | `:154` |

| 宽高比 | `VW` | `PxPerWorldX` | `K` | `PxPerWorldX ÷ K` | 两者关系 |
|---|---|---|---|---|---|
| **16:9** | 17.777778 | **107.99999**（float 实得） | 108 | 0.99999992 | **重合**（差 8.2e-6 px/世界单位；整屏 ≤ 2.5e-4 px） |
| **4:3** | 13.333333 | **144** | 108 | **1.3333** | 108 那条**窄 0.75×** |
| **21:9** | 23.333333 | **82.2857** | 108 | **0.7619** | 108 那条**宽 1.3125×** |

**建件侧（`Rect` `:1522-1525` / `PlaceCell` `:1098-1099`，逐字）**：
`WorldH = LayoutSpace.Px(h) = h/108`，紧跟 `SetAspect(w/h)` ⇒ `WorldW = WorldH × (w/h) = w/108`（`ImageQuad.WorldW` 的定义 `WorldW = _worldH × _aspect`，`Battle/ImageQuad.cs:108`）
⇒ **建件侧把「设计宽 `w`」写成 `w/108` 世界单位**（对任何宽高比都这样）。
**读回侧（`QuadRectPx` `:689-695`）**：`hw = WorldW × 108 ÷ 2` ⇒ 反推回 **`w`**。
⇒ **读 = 建件的逐条逆**（x 也一样），任何宽高比下都成立 —— 这就是「相等」那一档的实据。

**另一读法（没有采用，但必须记下来，因为它决定要不要动写侧）**：
`FromPixel` 把 **1920 设计 px 铺满 `VW`** ⇒ 反过来「世界宽 `WorldW` 在画布 px 帧里有多宽」= `WorldW × PxPerWorldX`。
⇒ 4:3 下 **相机实绘的那块比 108 读出来的宽 1.333×**（= `A990①` 的症状「元素互相叠」），21:9 下窄 0.762×。
**两者不等的原因在建件侧，不在读侧** —— 修必须成对（见 §①）。

---

## ② 证据（`文件:行号`，全部现读）

| # | 事实 | 出处 |
|---|---|---|
| 1 | 读回：`const K = DesignPxH / DesignHeight`（108）· `hw = q.WorldW * K * 0.5f` · 中心走 `PixelOfDesign(PosInDesignSpace(...))` | `Shell/MenuDraw.cs:689-695`（`K` `:691`） |
| 2 | 建件（`Rect`）：`ImageQuad.Create(parent, tex, Local(...), LayoutSpace.Px(y2-y1), …)` + `SetAspect((x2-x1)/(y2-y1))` | `Shell/MenuDraw.cs:1522-1525` |
| 3 | 建件（九宫格格）：`PlaceCell` = `SetWorldHeight(LayoutSpace.Px(cell.H))` + `SetAspect(cell.W/cell.H)` | `Shell/MenuDraw.cs:1098-1099` |
| 4 | 裁切闭环：`ClipNineChildren` 读 `QuadRectPx(q)`（`:1614`）→ 写 `SetWorldHeight(LayoutSpace.Px(cr.H))`（`:1627`）；`ClipTiledChildren` 同形（`:1673` / `:1688`） | `Shell/MenuDraw.cs` |
| 5 | 同一族已有断言：`if (!SameRectNear(QuadRectPx(q), vis)) PlaceCell(q, vis, vis, uv0);` | `Shell/MenuDraw.cs:913` |
| 6 | 契约副本：`const float KB = DesignPxH / DesignHeight; // 同 QuadRectPx` 与 `const float K = …; // 同 MenuDraw.QuadRectPx`；中心走 `MenuDraw.PixelOfDesign`，**尺寸项不动**（注释原文：「与 `MenuDraw.QuadRectPx` 同一份口径」） | `Shell/ViewportClip.cs:274`、`:292`、`:296-298` |
| 7 | 兄弟件（**指针族**）x 半宽用 `PxPerWorldX`，判据原文「中心项既然活在设计帧，半宽就也必须换成『画出来那块在设计帧里有多宽』…⛔ 两半必须同时换」 | `Shell/PointerLayer.cs:912-921` |
| 8 | 「尺寸项必须是同一条斜率」那条不变量的**原文语境** = **命中区**（`HitBoxPx`），不是 `QuadRectPx` | `Core/LayoutSpace.cs:211-228`（那句在 `:212-217`） |
| 9 | 另一处已收口的「中心 + 半宽成对换」 | `Shell/CampaignTab.cs:582`（`halfLen` 用 `PxPerWorldX`） |
| 10 | A964 的探针**有意把两帧并列记录**（命中区走指针帧、实绘走设计帧），且明写「`M == 1` 且父链无缩放时两式逐位相同」 | `Editor/ShellScene.cs:5855-5870` |
| 11 | 上一轮报的口径（本件复核对象） | `资料/普查产出_第八会话/E18_A1092收尾.md` §5·4（`:245-255`） |
| 12 | 第 2 件的病灶（改前） | `Shell/MenuDraw.cs` 改前 `:2569-2574`（现读对应块 `:2592-2611`） |

---

## ③ 改动清单

### 改动 1 · `Shell/MenuDraw.cs` `QuadRectPx` 的 doc（**纯注释，0 行为**）· 新增段现读 `:651-675`（`+25 / −0`）

**改前**（原文只有一段，全文照抄）：

```
        /// <para>⚠️ **尺寸项（`q.WorldW/WorldH × K`）本来就在设计量纲上、一个字不动**：
        /// `ImageQuad.Create` / `SetWorldHeight` 收的是 `LayoutSpace.Px(设计高)` = **设计长度**
        /// （渲染时由父链那同一份缩放放大）⇒ 除以 `K` 就是设计 px。⛔ 别顺手给它也除一次缩放。</para>
```

**改后**（保留原段，其后新增两段；全文照抄）：

```
        /// <para>⚠️ **尺寸项（`q.WorldW/WorldH × K`）本来就在设计量纲上、一个字不动**：
        /// `ImageQuad.Create` / `SetWorldHeight` 收的是 `LayoutSpace.Px(设计高)` = **设计长度**
        /// （渲染时由父链那同一份缩放放大）⇒ 除以 `K` 就是设计 px。⛔ 别顺手给它也除一次缩放。</para>
        ///
        /// <para>🔴 **2026-10-18（E22 现核裁定）：x 的长度项【也不许】换成 `LayoutSpace.PxPerWorldX`** ——
        /// 这一行的 `K` **是对的**。判据 = **读口必须是【建件那条换算】的逆**，而建件那两条本来就不同斜率：
        /// · **中心项** ↔ 建件的**位置**（`Local` → `LayoutSpace.RectCenter` → `FromPixel`，x 用**实测**
        ///   `VisibleWidth`）⇒ 它的逆 = `PixelOfDesign`（A1004 已换，⛔ 别换回 `ToPixel`）；
        /// · **尺寸项** ↔ 建件的**尺寸**（`Rect` / `PlaceCell` = `SetWorldHeight(LayoutSpace.Px(h))`
        ///   + 紧跟一句 `SetAspect(w/h)` ⇒ `WorldW = w ÷ K`）⇒ 它的逆 = **`K`**。
        /// ⇒ 两条路只在 **16:9** 重合（`PxPerWorldX` 实得 107.99999，见 `LayoutSpace` 那条）；非 16:9
        /// （4:3 ⇒ `PxPerWorldX = 144`、21:9 ⇒ 82.29）下**逐条对着它要逆的那条**才是自洽，
        /// 把 x 换成 `PxPerWorldX` 只会让**读口 ≠ 建件口**。
        /// ⚠️ **`Shell/PointerLayer.HitBoxPx` 的 `half`（x 用 `PxPerWorldX`）不是反例** —— 那一对量的是**相对指针**
        /// （「命中区 = 画出来那一块」，判据见 `LayoutSpace.PxPerWorldX` 的 doc），**不是**「建件时给的那个设计矩形」。
        /// 两族各自的 `中心/半宽` 必须**同族内**同斜率 —— 本函数的中心走设计帧、尺寸也走设计帧（= 建件帧）✓。</para>
        ///
        /// <para>🔴 **换掉会坏的三处**（⛔ 别再试探）：① `ApplySoftEdges` 里那条
        /// `if (!SameRectNear(QuadRectPx(q), vis)) PlaceCell(…)` —— 非 16:9 下**恒不等** ⇒ 每次重切都 `PlaceCell`；
        /// ② `ClipNineChildren` / `ClipTiledChildren` 是「**读回 → 求交 → 再 `PlaceCell`**」的闭环
        /// （两条都是先 `var qr = QuadRectPx(q)`、后 `SetWorldHeight(LayoutSpace.Px(cr.H))`），
        /// **两边同斜率才幂等** ⇒ 只换读口，同一块每跑一趟再被切一次（**越切越窄**）；
        /// ③ `Shell/ViewportClip.cs` 的**两支**都是 `K`（`_hasBaseRect` 那支的 `KB` · 实时反推那支的 `K`，
        /// 两处都写着注释「**与 `MenuDraw.QuadRectPx` 同一份口径**」）= 本函数的**契约副本**
        /// （`ViewportClip.ClipPx` 的框要与各宿主给的设计矩形求交）⇒ 只改这里就是两套口径。
        /// 🔴 **非 16:9 下真正错的是【建件侧】**：位置跟着 `VisibleWidth` 压、尺寸不压
        /// ⇒ 4:3 上元素互相叠 = 已登记的 **`A990①`**（`LayoutSpace` 那条「全局只有一条换算」还没落）。
        /// 要修就得**成对**修（建件那五处 + 全部读口一起换斜率），**⛔ 不是在这里单改一行**。</para>
```

⚠️ 现状核对：`const float K` / `cpx` / `hw` / `hh` 四行**一字未动**（`git diff -U0` 该 hunk = `+25 / −0`，全部落在 `///` 注释行上）。

### 改动 2 · `Shell/MenuDraw.cs` `CheckAbsorbRule` 第 ③ 步（**行为改动**）· 现读 `:2592-2611`（`+18 / −6`）

**改前**（全文照抄）：

```csharp
            else
            {
                float w = q.WorldW * 108f, h = q.WorldH * 108f;
                float cx = LayoutSpace.PxX(q.transform.position.x), cy = LayoutSpace.PxY(q.transform.position.y);
                near(cx - w * 0.5f, x1, 1.5f, $"{what}：吸收层渲染矩形**左沿** = 原版面板底图");
                near(cy - h * 0.5f, y1, 1.5f, $"{what}：…**上沿**");
                near(cx + w * 0.5f, x2, 1.5f, $"{what}：…**右沿**");
                near(cy + h * 0.5f, y2, 1.5f, $"{what}：…**下沿**");
```

**改后**（全文照抄）：

```csharp
            else
            {
                // 🔴 **2026-10-18（E22 就地收口）：这四条 near 的矩形【转调】本文件唯一一份正确的口。**
                //    改前这里自己写了一遍「中心 + 半宽」，**同族双料**：
                //      · **帧错**：中心走 `PxX/PxY`（**108 帧**）、半宽也 `× 108f`，而比的是**字面设计 px**
                //        ⇒ 非 16:9 下四条 `near` 全错，偏 `r = VisibleWidth / DesignWidth` 倍
                //        （4:3 ⇒ 0.75 · 21:9 ⇒ 1.3125）；
                //      · **父链错**：`q.transform.position` 是**已缩放**的视觉世界坐标、**没过 `PosInDesignSpace`**
                //        ⇒ 窗根被 `TransformScalerBySmallScreenUI` 乘 M（或窗内自带缩放的节点）时**多一层 M**。
                //    ⇒ 转调 `QuadRectPx`（A298 除回设计缩放 · A1004 走设计帧读口；`A1003` 那一轮已把
                //    **7 个宿主 / 17 个函数位**收到它，**唯独本函数自己这一份没收**）。
                //    ⚠️ **尺寸项仍是 `K = 108`，⛔ 别换成 `PxPerWorldX`** —— 那是**有意**的，
                //    判据（为什么它是建件那条换算的逆）写在 `QuadRectPx` 的 doc 里。
                //    📌 **出厂态（16:9 + M == 1）读数一字不变**：与改前只差 float 舍入
                //    （`PxPerWorldX` 实得 107.99999 ⇒ 全屏 ≤ 3e-4 px，远小于这里的 1.5px 容差）。
                var qr = QuadRectPx(q);
                near(qr.x1, x1, 1.5f, $"{what}：吸收层渲染矩形**左沿** = 原版面板底图");
                near(qr.y1, y1, 1.5f, $"{what}：…**上沿**");
                near(qr.x2, x2, 1.5f, $"{what}：…**右沿**");
                near(qr.y2, y2, 1.5f, $"{what}：…**下沿**");
```

- **`near` 条数不变**（每个调用点仍是 4 条）⇒ 各宿主**断言合计条数不变**；`q == null` 那条 `chk(false, …)` 与 ④⑤⑥ 三步**一字未动**。
- 语义映射核对：`qr.x1` = 左沿（对应改前 `cx − w/2`）· `qr.y1` = 上沿（`cy − h/2`，`PxRect` 是**左上原点 y 向下**）· `qr.x2`/`qr.y2` 同。
- ⛔ `Editor/*.cs`（6 个宿主的薄壳）**一个字没改** —— 它们只是转调，改里面它们不用动。
- 同族余党：`CheckAbsorbRule` 体内**已无** `108f` / `PxX(` / `PxY(`（grep 复核：整个 `MenuDraw.cs` 里只剩注释里的字面引用）。

---

## ③·b 哪些断言的行为会变（两条各一份）

### 第 1 件（纯注释）⇒ **一条都不变**。本件**不需要任何自检条目**（铁律 12 判据：纯注释 = 0 条）。

### 第 2 件（`CheckAbsorbRule` 第 ③ 步转调）⇒ **5 个宿主 / 23 个调用点 / 92 条 `near` 的读数语义变了**

**受影响的面**（宿主 → 调用点 → 窗名，逐个现数；`grep -c "CheckAbsorbRule"` 的数是**含包装/注释**的，下面是**真调用点**）：

| 宿主 | 调用点数 | 窗名 |
|---|---|---|
| `Editor/MainMenuScene.cs` | **12** | 选卡组窗 · 练习窗（选卡组那一列）· 练习窗（右半那块红底）· 战斗日志弹窗 · 玩家档案窗 · 匹配弹窗 · 遭遇战窗 · 排位窗 · 排行榜弹窗 · 奖杯详情弹窗 · 好友挑战弹窗 · 能源活动窗 |
| `Editor/RewardsScene.cs` | **5** | 战役奖励窗 · 原版奖励窗 · 每日连登窗 · 收件箱窗 · 重摇任务窗 |
| `Editor/CollectionScene.cs` | **4** | 卡组信息窗 · 练习窗（阵营纵列底图）· 导入卡组窗 · 聊天窗 |
| `Editor/SettingsScene.cs` | **1** | 设置窗 |
| `Editor/ShopScene.cs` | **1** | 卡包详情窗 |
| **合计** | **23** | 每个调用点 **4 条** `near` ⇒ **92 条** |

**行为变化（逐档说清）**：

| 状态 | 改前读数 | 改后读数 | 结论 |
|---|---|---|---|
| **16:9 + M == 1**（自检全线钉的出厂态） | `PxX/PxY`（108 帧）+ `×108` | `PixelOfDesign`（107.99999）+ `×108`，且除一次父级 `lossyScale`（= 1 ⇒ 逐位不动） | **差 ≤ 3e-4 px ＜ 1.5px 容差 ⇒ 92 条一条都不翻**；断言**条数不变** |
| **非 16:9**（4:3 / 21:9） | 四条全偏 `r` 倍（4:3 偏 0.75、21:9 偏 1.3125） | 四条**正确** | **改前应红而没红**（宿主钉了 aspect）⇒ 本件把它**修对** |
| **窗根 M ≠ 1**（小屏缩放开关开 / 窗内自带缩放） | 中心多一层 `M` | 除回设计空间 | 同上：**改前潜伏**，改后正确 |

**⛔ 不受影响**（逐条点名，免得误报）：
- `CheckAbsorbRule` 的 ④（档 = `qContentMin − 1`）· ⑤（档位告警按窗记账）· ⑥（两条真点击 `pl.ClickAt` + 窗状态）**一字未动**；
- `CheckShadeRule`（同文件另一条）、`MenuDraw.Absorb` / `Hit` 的建件路径、`PointerLayer.HitBoxPx`（**早就**是 `PxPerWorldX`）；
- 各宿主**自己**的几何断言（`CheckRectPx` 一族、`RectOf`/`QuadPxRect` 等，A1003 已收口到 `QuadRectPx`）—— 本件没碰它们。

**该跑哪几条**（本件碰了共用件 `Shell/MenuDraw.cs` 且改的是**断言辅助件**）：`MainMenuScene.Run` · `RewardsScene.Run` · `CollectionScene.Run` · `SettingsScene.Run` · `ShopScene.Run`（5 条）。
⚠️ **预期 = 与改前逐条相同**；若其中出现**新的红**，先怀疑**并行写手的半成品**（本波在改 `Editor/{DeckScene,ShellScene,ShopScene,BattleScene,SettingsScene}.cs`、`Shell/SettingsWindow.cs`、`Battle/ErrorMessageBanner.cs`），⛔ 别当成本件。按铁律 12 由**调度台**在同步点跑（本件没跑 Unity）。

---

## ④ 没查清的部分

1. 🔴 **`Core/LayoutSpace.cs` 本件【一个字没动】**，理由：它的**工作区里已经有别人的未提交改动**（`git diff --numstat` = `5 / 1`，是 E18 那轮补登
   `CampaignTab.BuildLine` / `SettingsWindow.UpdateFpsDrag` 的注释），本批它**不在我的白名单**（白名单只许我"需要时补落地范围清单"）。
   ⇒ 我原打算在 `PxPerWorldX` 的 doc 里加 3 行「⛔ 别拿本条去改 `QuadRectPx`」的**防回归**注，**没加**（避免与在写的写手撞车）；
   **改法见下**（给调度台，两处要一起改，`LayoutSpace` 那份自陈「兄弟副本在 `MenuDraw.PixelOfDesign` doc 里」）：
   - `Core/LayoutSpace.cs` 的 `PxPerWorldX` doc（现读 `:211-228`，末段那句「尺寸项必须是同一条斜率」）：补一句「**这句话的适用面 = 命中区那一对（中心与半宽都取世界坐标的同一帧）**；
     ⛔ 不要拿它去改 `MenuDraw.QuadRectPx` 的 x 尺寸项 —— 那一对是**建件那条换算**（`FromPixel` 位置 / `Px` 尺寸）的逆，判据见该函数 doc」。
   - `Shell/MenuDraw.cs` 的 `PixelOfDesign` doc（现读 `:605` 上方那一段「📌 落地范围」）：同批补同一句（两份是**契约副本**，⛔ 别只改一处）。
2. ⚠️ **「相机实绘」那一读法下的数字（4:3 裁切那例）是【离线手算】的、⛔ 没跑**：`R=[0,100]` / `C=[50,150]` / 4:3 ⇒ 今天的世界区间 `[-6.3773, -5.9143]`（宽 0.46296 对、中心偏 0.0579 世界单位 ≈ 8.3 设计 px）；只换读口后 `[-6.3966, -5.7794]`（中心对、宽 0.61728 = 1.333× ⇒ 两侧各溢出 ≈ 11 px）。**结论方向可信（差的就是那个 1.333 因子），具体 px 数没验。**
3. **`A990①`（建件侧 x 尺寸斜率）本件没修、也没验**：它要动的 5 处（`Rect` · `PlaceCell` · `ClipNineChildren` · `ClipTiledChildren` · `SetPxSize`（后者是 `sizeDelta` 元数据、只影响验收不影响渲染））**全不在白名单**，而且它有**全局渲染后果**（4:3 下每一项 UI 的横向宽度都会变）⇒ 必须**成对**做、且要有非 16:9 的两态夹具（今天 12 条宿主**全钉 16:9**，没有任何一条能照出它）。
   📌 建议的登记文字：`A990①` **补充**「读侧（`QuadRectPx` + `ViewportClip.ClipPx` 两支）与写侧必须**同批**换斜率；任何一侧单独换 = 读口 ≠ 建件口（见 `资料/普查产出_第八会话/E22_QuadRectPx与CheckAbsorbRule.md` §①）」。
4. `Shell/PointerLayer.cs` / `Shell/ViewportClip.cs` / `Editor/*` 的**非 16:9 实况**（真窗口拖到 4:3）**没跑**，全部结论都是**静态推导 + 现读算式**。
5. 上一轮记的「`Editor/RewardsScene.cs` 只数到 5 个调用点、旧文写 7」那条**没查**（不是本件范围）；本件现数的 23 与 `MenuDraw.cs` 里 2026-10-16 那次订正后的数**一致**。

---

## ⑤ 顺手发现的东西

1. 🔴 **`Shell/MenuDraw.cs` `PixelOfDesign` 的 doc 里有一个多余的 `</para>`**（现读 `:605` 上方那段末尾 = `…旧名。</para></summary>`，
   `</para>` 与 `</summary>` 之间没有对应的 `<para>` 开标签）。**`git show HEAD` 那一版也有**（同一处文本，只是行号早 12 行 ⇒ **不是本件引入的**）。
   编译器只当 XML doc 警告、typecheck 的「错误数」照旧 0 ⇒ **不会红、也不会被发现**。同一族的连字符级问题（`<para>` 99 / `</para>` 100）**只在 `MenuDraw.cs` 里出现一次**，就是这一处。
   📌 值不值得修：它会让**任何 XML doc 校验 / IDE 提示**报一条噪声；**没改**（不在白名单的两处之内）。
2. **`CheckAbsorbRule` 的宿主数量账现在是干净的**：本件现数 **5 个宿主 / 23 个调用点**，与 `MenuDraw.cs` 里 2026-10-16 那条订正（「原文写 26 / 实测 23」）**一致** ⇒ 那条「差的那 2 处没查清」可以销账（差的那 2 处就是原文把 `RewardsScene` 数成 7 的那两个）。
3. **A964 探针的两帧并列是【有意】的**（`Editor/ShellScene.cs:5855-5870` 注释写着「窗根被乘 M 时两帧分家 —— 那正是本探针要盯的一档」）⇒ 它**不是**「拿两个不同帧的量硬比」那种缺陷。但**非 16:9** 下 `HitBoxForTest`（指针帧，`PxPerWorldX`）与 `QuadPxRect`（设计帧，`K`）会**并列差 1.333 倍**（4:3），
   而那**不是**窗根缩放造成的 —— 看 TSV 的人很容易把它读成「命中区过大/过小」。📌 建议在那段注释里补一句：「⚠️ 此外**非 16:9**（`VisibleWidth ≠ DesignWidth`）也会让两栏差 `r` 倍，那与 `M` 无关」。（`Editor/*.cs` 是黑名单 ⇒ **没改**。）
4. **`git status` 提示**：本波 `Shell/MenuDraw.cs` 的工作区 diff = **60 / 11**，其中**我的只有两处**（`+25/−0` 的 doc、`+18/−6` 的 `CheckAbsorbRule`），
   另 **`+17/−5`** 是 E18 那一轮**未提交**的注释改动（在 `:585` / `:596` / `:600` 三个 hunk 上）⇒ 提交/收口时别把它当成我的。

---

## 附：机械读数

- **秒级类型检查**（`TMPDIR=/tmp/wf_e22 bash d:/4/Unity/工具/typecheck.sh`）—— 本件每改一版都跑：
  - 改完两处：**运行时错误数: 0 / 编辑器错误数: 0**
  - 修掉自己那个多余 `</summary>` 之后再跑：**运行时 0 / 编辑器 0**
  - 最终（把 doc 里的旧行号改成符号引用之后）：**运行时 0 / 编辑器 0**
  - ⚠️ 四次**一次都没出现**「错误集中在别人文件上」那种情况 ⇒ 本波并行写手当时没在写坏别的文件（本件也没去动任何别人的文件）。
- **行尾**：`Shell/MenuDraw.cs` 改前改后都是**纯 LF**（`b.count(b'\r\n') == 0`；总行数 3056 → 3093）。⛔ 没用过 `sed -i`、没用过文本模式 `open(...,'w')`。
- **`git diff --numstat`**（本件两个白名单文件）：
  ```
  60	11	Unity/MyGame/Assets/CardPresentation/Shell/MenuDraw.cs      ← 我的 25/0 + 18/6，另 17/5 是 E18 的
  5	1	Unity/MyGame/Assets/CardPresentation/Core/LayoutSpace.cs     ← **不是本件**（E18 的未提交改动，本件没动这个文件）
  ```
- **没跑 Unity**（红线）⇒ 第 2 件的新读数**没实跑过**；「16:9 零回归」是**算式推的**（`PxPerWorldX` 实得 107.99999 ⇒ ≤3e-4 px），不是跑出来的。
- **本件覆盖哪几条自检**：第 1 件 = 0 条；第 2 件 = `MainMenuScene.Run` / `RewardsScene.Run` / `CollectionScene.Run` / `SettingsScene.Run` / `ShopScene.Run`（5 条，由调度台在同步点跑）。
