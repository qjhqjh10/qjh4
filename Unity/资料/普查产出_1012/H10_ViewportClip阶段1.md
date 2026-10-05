# H10 —— A198②【B 方案 · 阶段 1】视口状态跟节点：`ViewportClip` + 取状态收口

> 写手：H10（2026-10-12）· 判据来源：`资料/普查产出_1012/V8_判据补查.md` §A198②（第 123–155 行）
> 白名单：**新建** `Shell/ViewportClip.cs` · **改** `Shell/MenuDraw.cs`（只动取状态那条路）· 本报告
> ⛔ 没碰：52 个设站点 · `Shell/WindowsManager.cs` · `Editor/*` · `Battle/*` · 两张正本 · git

---

## 一、结论

1. **阶段 1 做完了**：新建 `Shell/ViewportClip.cs`（挂在视口节点上的组件 + **全壳唯一的取状态解析函数**），
   `MenuDraw` 里**所有真正取裁切状态的入口**（5 个：`Rect` / `Nine` / `Tiled` / `Hit` / `ClipText`，另加
   `DeckCell` 那一处**自成一体的取态**）全部改成转调它。**渲染那一路与命中那一路读的是同一份状态**
   （A188 的硬约束），只是各取一个视图（`RenderClip` vs `裸 Clip + Pad`）。
2. **今天行为逐位不变**（**结构性保证**，不是「应该没事」）：
   · 全仓**没有任何节点**挂 `ViewportClip`（`grep -rn "AddComponent<ViewportClip>"` 只命中本文件的 `Hang`）；
   · 解析函数第 1 支「形参非空 ⇒ 原样返回」**连父链都不走**，第 3 支「都没有 ⇒ 返回形参本身」——
     今天每个调用点必落在 ① 或 ③。
3. **一个判断需要调度台过目**（我按「共存最稳」选的，翻转是一行）：优先级排成
   **显式形参（非空）> 节点 > 无**。理由：本壳的「形参」正是**旧路状态（`RenderClip`/`Clip`/`ClipPad`/`ClipSoftness`）
   的载体**（各窗从 `GameWindow` 那三兄弟转进来），所以「形参非空」= 「旧路还在设」⇒ 迁移期是
   **删掉旧设站点那一刻节点才接管**，每一步可回退；反过来（节点优先）则「挂上节点那一刻行为就变」，
   还会把**故意传 null** 的站点（`MenuWindowBase.cs:437` 左栏那种）被上面某处的节点悄悄裁掉。
   ⚠️ 这条与 V8 §A198② ④ 的措辞（「先沿父链找最近的 `ViewportClip`」）**顺序不同** ——
   那两句能同时成立的唯一读法是「形参非空 = 显式覆盖」，我按这个读法做了，并在 `ViewportClip.cs`
   文件头把**两种排法的后果**都写明（翻转只需把 `Resolve` 第 1 支挪到后面）。
4. **`MenuDraw.Text` / `TextBox` 这 2 个「口」不吃裁切**（现读为准）⇒ 没动签名，理由见 §五·1。
5. ⛔ **本阶段不挂任何节点**；⛔ 没碰 `Editor/*` ⇒ 最小用例只写在 §四（**待接线**，夹具归调度台）。

---

## 二、改动清单（文件:行号 = 改后坐标）

### A. 新建 `Unity/MyGame/Assets/CardPresentation/Shell/ViewportClip.cs`（1 个组件 + 1 个值类型 + 1 个解析函数）

| 位置 | 是什么 | 为什么 |
|---|---|---|
| `:74` `ClipState.Clip` / `:76 Softness` / `:78 Pad` / `:81 FromNode` | 一次解析的结果 | **两副面孔各一个视图**：`Clip`+`Pad` 给命中那一路、`RenderClip` 给渲染那一路 ⇒ 「同一个框」这件事只有一处定义 |
| `:91` `ClipState.RenderClip` | = `MenuDraw.PaddedClip(Clip, Pad)` | 复用既有唯一一份 padding 算式（⛔ 不新写第二份） |
| `:124` `padding` | `Vector4`，照原版 `RectMask2D.m_Padding` | 判据 `RectMask2D.cs:50-51` + `Culling/Clipping.cs:26-30`（两副面孔都读它） |
| `:131` `softness` | **`Vector2Int`**（原版就是这个类型） | 判据 `RectMask2D.cs:70-71`；V8 §A198② ② 实读那 150 个实例的 softness **全是整数** |
| `:140` `SoftnessPx` | 取值视图，读时夹 `Mathf.Max(0,·)` | 原版夹在 **setter** 里；我们是公开字段 ⇒ 只能夹在**读**那一处（仍是一份） |
| `:155` `ClipPx` | 节点自己的 rect → `PxRect`（画布 px） | 原版 mask 用的就是**它自己那个 `rectTransform`**（`RectMask2D.cs:178-185/226`）；换算口径与 `MenuDraw.QuadRectPx` 同一份（`PosInDesignSpace` + `×108`）。不是 `RectTransform` 时**出声**（限流 3 条）并返回 `null`（⇒ 回落旧路，⛔ 不裁成 0 面积） |
| `:184` `Hang(parent,name,r,pad,soft)` | 建节点 + 挂组件 | **阶段 2 的迁移入口**（也保证节点是 `RectTransform` 且 `sizeDelta` 已写）；夹具走它才拿得到对的框 |
| `:213` `Resolve(parent, clip, softPx, pad)` | 🔴 **全壳取状态唯一入口** | 三段优先级；`clip` 非空时**不走父链**（逐位不变的保证） |
| `:237` `FindAbove(t)` | 逐级 `GetComponent`（含 `t` 自己） | ⚠️ **不用 `GetComponentInParent<T>()`**：它带一个 `includeInactive` 形参（本地文档 `UnityEngine.CoreModule.xml:5988-5996`），**而那份 XML 没写默认值 = 本机查不到** ⇒ 不拿它当判据。逐级 `GetComponent` **不看 `activeSelf`** ⇒ 批处理里「页签关着也在建几何」那一档照样找得到节点（漏找 = 静默不裁；⚠️ 这是**有意偏离**，见 §六·5） |
| `:251` `NodeResolutions` / `:256` `UnusableNodes` | 计数器 | 「这条路带电」的可断点（见 §四） |

### B. `Unity/MyGame/Assets/CardPresentation/Shell/MenuDraw.cs`（只动取状态那条路）

| 行号 | 改什么 | 为什么 |
|---|---|---|
| `:943-945` `ClipText` | 从**标签自己**的父链解析，`clip = _st.RenderClip; softPx = _st.Softness;` | 文字那一路原来只有「调用方传进来的 clip」一个来源；父链从标签走与从 `parent` 走同一条链（多自己一层，节点同样命中） |
| `:1234-1236` `Rect` | 同上（渲染视图） | 渲染那一路的主入口 |
| `:1297-1299` `Nine` | 同上 | 九宫格子块那条路（`ClipNineChildren` 吃的是解析后的 `clip`） |
| `:1448-1450` `Tiled` | 同上 | 平铺子块那条路 |
| `:1579-1581` `Hit` | `clip = _st.Clip; maskPad = _st.Pad;` | 🔴 **命中那一路要【裸】框 + pad**（pad 由下面那句 `PaddedClip(clip, maskPad)` 自己缩）⇒ ⛔ 喂 `RenderClip` 进去 = pad 缩两次 |
| `:2198-2201` `DeckCell`（裸重载） | 一处解析，**分成两份视图**：`clipR` 给渲染/文字/裁检、`clip`/`maskPad` 给末尾那句命中 | 这一处**两条路在同一函数里**，是 A188 那条硬约束最容易破的地方：喂错一份 = 命中区被内缩两次（或渲染不内缩） |
| `:2207/2218/2224/2227/2236/2244/2252` | 渲染/文字/裁检改用 `clipR` | 同上；`2227` 那处原本是 `clip.HasValue ? ClipText(nl, clip, …)`，**不改成 `clipR` 就会用未内缩的框裁文字** |
| `:2125-2136` · `:2147-2150` · `:2175-2180` | **就地订正三条「`clip: null` = 显式不裁」的说法** | 🔴 铁律 5：`clip: null` 的语义**从本批次起变了**（现在是「不显式覆盖」⇒ 节点说了算）。今天仍等价于「不裁」，但**阶段 2 挂上节点后**这三处（两个 `clip: null` 调用点 + `win==null` 那条警告）都要重看；顺便写明：**「显式不裁」今天没有表达方式**（`PxRect?` 的 `null` 分不出三态），要它得调度台裁定换载体，⛔ 不许顺手发明哨兵矩形 |

**没动**：`MenuDraw.Text` / `TextBox`（不吃裁切，§五·1）· `ShadeHit` / `Absorb`（转发给 `Hit`，自动吃到）·
`Visible` / `ClipRect`（纯矩形函数、手上没有 `Transform` ⇒ 由调用方先解析再传，§六·3）·
`RenderClip` / `Clip` / `ClipPad` / `ClipSoftness`（`GameWindow` 那三个字段**一个字节没动**）。

---

## 三、解析函数的行为表（`ViewportClip.Resolve(parent, clip, softPx, pad)`）

| # | 输入状态 | 返回 | `FromNode` | 走父链吗 | 今天会出现吗 |
|---|---|---|---|---|---|
| ① | `clip` **非空**（= 旧路还在设） | 原样返回 `(clip, softPx, pad)` | `false` | **不走**（第 1 句就返回） | ✅ 每次「窗里设了 `Clip`」的绘制 |
| ②a | `clip` 为空 + 父链上有 `ViewportClip`（节点是 `RectTransform`） | `(节点 rect, 节点 softness, 节点 padding)` | `true` | 走，**最近的**那个 | ❌ **今天 0 次**（无节点） |
| ②b | 同上，但节点**不是** `RectTransform` | 回落成 ③ | `false` | 走 | ❌ |
| ③ | `clip` 为空 + 父链上没有节点 | 原样返回 `(null, softPx, pad)` | `false` | 走（找到底为止） | ✅ 每次「不在视口里」的绘制 |

两条**逐位不变**的等价式（都对参数是恒等式）：
- `PaddedClip(clip, Vector4.zero) == clip`（`PaddedClip` 首句 `pad == Vector4.zero ⇒ return clip`）⇒
  **渲染那几路的 `RenderClip` 在 ①/③ 下与原 `clip` 逐位相同**（含 `null` 那支）。
- 命中那两路的 `clip`/`maskPad` 在 ①/③ 下逐位等于原形参 ⇒ 末尾那句 `PaddedClip(clip, maskPad)` 逐字未变。

**反过来说清【有节点】那一态的形状**（阶段 2 的语义）：
```
节点 rect V=(100,100)-(500,500) · padding=(10,0,0,0) · softness=(0,25)
  →  渲染那一路： clip=(110,100)-(500,500) + soft=(0,25)
  →  命中那一路： clip=(100,100)-(500,500) + maskPad=(10,0,0,0) ⇒ 由 Hit 算成 R ∩ (V−pad)
  ⇒ 两条路缩的是**同一个框**（原版 `R ∩ (V − pad)`），只是各取一个视图。
```

---

## 四、验收 / 最小「节点生效」用例（**待接线** —— 按简报 ⛔ 我没碰 `Editor/*`）

### 4.1 等价性（**不加夹具**的既有场景里加，最便宜的那一条）
```csharp
CheckTrue(ViewportClip.NodeResolutions == 0,
    "阶段 1：全仓不挂节点 ⇒ 取状态解析器【恒】不走节点态（走一次就说明有人偷偷挂了节点）");
```
放在任意既有 `*Scene.Run` 末尾即可（每跑一次 `-executeMethod` 是**新进程**，静态计数从 0 起）。
⚠️ **这条与 4.2 必须分在两个场景里**（4.2 自己会挂节点 ⇒ 会把计数顶起来）。

### 4.2 节点生效（**一个 section，四步**；形状照现有自检的「建件 + 量件」写法）
```csharp
// ① 视口节点：V=(100,100)-(500,500)、pad=(10,0,0,0)、soft=(0,25)
var vp = ViewportClip.Hang(root, "Viewport", new PxRect(100,100,500,500),
                           new Vector4(10,0,0,0), new Vector2Int(0,25));
// ② 在它下面画一个【越界】的件：(−100,−100)-(300,300)
var q = MenuDraw.Rect(vp.transform, CardArt.Solid(), new PxRect(-100,-100,300,300), "Overflow", 3010);
// ③ 渲染框 = 件 ∩ (V−pad) = (110,100)-(300,300) ⇒ 宽 190、高 200
CheckNear(q.WorldW, LayoutSpace.Px(190f), 0.01f, "节点态的渲染框 = 件 ∩ (V − pad)（不是整个件、也不是 V）");
CheckTrue(ViewportClip.NodeResolutions > 0, "这条路**带电**（只断「框非空」那种断言改坏实现不会红）");
// ④ 命中那一路（同一份状态的另一个视图）：命中区 R=(450,100)-(700,200) ⇒ R ∩ (V−pad) = 450..490 ⇒ 宽 40
var h = MenuDraw.Hit(vp.transform, "Btn", new PxRect(450,100,700,200), 3020, () => { });
var hq = h.GetComponentInChildren<ImageQuad>();
CheckNear(hq.WorldW, LayoutSpace.Px(40f), 0.01f, "命中框 = R ∩ (V − pad)（⛔ 不是 R ∩ (V − 2·pad)）");
// ⑤ 反面：形参非空 ⇒ 节点被盖住（= 共存期旧路优先）
var c = ViewportClip.Resolve(vp.transform, new PxRect(0,0,8,8), new Vector2(1,2), new Vector4(3,4,5,6));
CheckTrue(!c.FromNode && c.Clip.Value.x2 == 8f && c.Softness == new Vector2(1,2) && c.Pad == new Vector4(3,4,5,6),
    "显式形参赢：连父链都不走、四个值逐位是形参");
```
（`CheckNear` 若宿主没有，按各 `Editor/*Scene.cs` 现有断言的写法就地算差值；我只给**算式与期望值**，
⛔ 没碰 `Editor/*`。`q.WorldW` 是**设计单位**，`LayoutSpace.Px(px)` 是它那一份换算 —— 两者同量纲。）

### 4.3 逐条**改坏法**（每条都能让上面某一条变红；⛔ 不许只断「建起来了」）

| 改坏哪一处 | 现象 | 哪条红 |
|---|---|---|
| 删 `Rect` 里那 3 行解析 | 节点态的件**整块画出去**（`WorldW` = 400 而不是 190） | 4.2 ③ |
| 删 `Nine` / `Tiled` 里那 3 行（或把 `clip` 换回原形参） | 九宫格/平铺**画到框外**（子块不截） | ⚠️ **今天没有断言** ⇒ 待接线（形状照 4.2 ③，把 `Rect` 换成 `Nine`/`Tiled`） |
| `Hit` 里把 `clip = _st.Clip` 改成 `clip = _st.RenderClip` | **pad 缩两次** ⇒ 命中宽 40 → 30 | 4.2 ④ |
| `Hit` 里删掉解析 | 节点态命中区**不裁**（整块 250 宽，滚出视口还能点） | 4.2 ④ |
| `DeckCell` 里把 `clip = _st.Clip` 改成 `clip = clipR` | 同上（一处改、**两条路同时错**） | 4.2 ④ 的同形 |
| `DeckCell` 里 `ClipText(nl, clipR, …)` 改回 `clip`（裸框） | 文字按**未内缩**的框裁（差 10px，且**只有 pad 非零时**才现形） | 现有 `CollectionScene` 那两条视口 pad 全 0 ⇒ **不会红** = 潜伏缺陷（改夹具 pad 非零才照得出） |
| `Resolve` 把「形参非空」那一支删掉（改成先找节点） | 迁移期「旧路还在设、节点也挂了」时**节点赢** | ⚠️ **今天不会红**（无节点）—— 这正是 §一·3 那个判断的后果，改之前先看 `ViewportClip.cs` 文件头 |
| `ClipPx` 把 `rt.rect.width` 换成 `rt.sizeDelta.x` | **今天等价**（`SetPxSize` 强制锚点重合） | ⚠️ 不会红 = 潜伏；给节点设一个**非重合锚点**才照得出（⛔ 别改成依赖父 rect 的写法） |

---

## 五、待接线（阶段 2 的活；本阶段**没动**，逐条给出坐标）

1. 🔴 **`MenuDraw.Text` / `TextBox` 不吃裁切（现读）**：`MenuDraw.Text:1497` 与 `TextBox:1523` 是**纯构造器**
   （建 `Label` + 定字号），裁切由**调用方**做（`MenuWindowBase.Text` / `TextBox` 里那两步
   `Visible(...)`→`ClipText(...)`）。⇒ 想让「节点态」对文字生效，必须改**调用点**：
   · `Shell/MenuWindowBase.cs:301`（`Text`）与 `:322`（`TextBox`）—— 两处都写成 `if (RenderClip.HasValue) MenuDraw.ClipText(...)`
     ⇒ **本窗不设 `Clip` 时根本不调**（节点态永远到不了 `ClipText`）；
   · 同形状还有 `CollectionWindow.cs:1846` · `ChatPanel.cs:618` · `PlayerProfileWindow.cs:645` ·
     `ItemDrawer.cs:1007,1243` · `SettingsWindow.cs:1740` · `AllianceMemberTab.cs:790` · `PracticeModePopup.cs:1740` ·
     `DeckRuntime.cs:3400`（`Deck/`，不是 `Shell/`）。
   · ⛔ **不许在 `MenuDraw.Text` 里顺手补一次 `ClipText`** —— `ClipTmpMesh` 的调用契约第 ③ 条写明
     「在已经夹过的网格上再夹一次 = 几何被夹第二次而 uv 只按第一次走（**越裁越错，且静默**）」
     ⇒ 那会和上面那些调用点**叠加成两刀**。
2. **52 个设站点**（V8 §A198② ② 已盘点：34 个文件、52 处，含成对还原）从「设 `GameWindow.Clip/ClipPad/ClipSoftness`」
   改成「挂 `ViewportClip.Hang(...)`」；每迁一处要**同时删掉旧设点**，否则旧路（形参）赢、节点**不生效**
   （这是共存期的**设计行为**，不是缺陷 —— 但迁移时容易漏删，建议迁移期在设点侧加一条自查）。
3. **`Visible` / `ClipRect` 这两条纯矩形函数不解析**（它们手上只有矩形、没有 `Transform`）：
   现在由各入口先解析再传。⇒ 若阶段 2 出现「直接调 `Visible(r, win.RenderClip)`」的新站点，
   **节点态到不了**（例：`MenuScroll.Intersects(onScreen)` 那 19 处构建循环，绑的是 `MenuScroll.Viewport`）。
4. **`ClippedTextGuard` 存的是【解析之后】的那一份**（`MenuDraw.cs:1005 ArmTextGuard` 收 `clip`/`softPx`）——
   节点**移动/改尺寸**之后它重裁的是**旧框**（与旧路同一条边界：`GameWindow.Clip` 也是快照）。要跟就得让守卫
   回调时重新 `Resolve`（⛔ 别在阶段 1 顺手改，那是行为变化）。
5. `ViewportClip.cs.meta` **还没生成**（我没跑 Unity；`gen_csc_rsp.py` 会自动扫到新 `.cs`，类型检查已过）。
   下一次 Unity 批处理导入时会自动建 —— 若调度台要它立刻进仓，跑一次 Unity 即可（⛔ 按简报我没跑）。

---

## 六、没查清 / 明确不知道的

1. **原版那两个字段有没有运行时赋值点**（V8 §A198② ③ 已经如实标注「只覆盖出厂那一档」）。
   本件**沿用同一口径**，没去找赋值点（铁律 5·c 的「状态 → 参数」表因此**只覆盖出厂值这一档**）。
2. **「一扇窗两个视口同时要不同参数」我们仍然没有真机实例**（V8 ③ 已记）⇒ 本件只保证**形状**能表达它，
   **没有**拿它验过任何真实窗口（今天生产非零 pad 只有锻造轨道一处）。
3. ⚠️ **`Resolve` 的父链查找是「最近的」**：原版是「mask 管自己子树」，我们等价成「从件往上找第一个节点」。
   V8 记「**没有一层 mask 罩住另一层 mask 的写法**」⇒ 今天没有歧义；**嵌套视口**那一档没查（也没有实例）。
4. **`FindAbove` 逐级 `GetComponent` 的代价没有实测**（没跑 Unity）。估算是噪声级（只有 `clip == null` 才走、
   每级一次 `GetComponent`），⚠️ 若哪个场景真的量出可感开销，改法**不是**加缓存（缓存会与「阶段 2 随时挂节点」冲突 ⇒ 静默失效），
   而是考虑在 `GameWindow` 侧解析一次往下传。
5. **`ViewportClip` 挂在未激活节点上的取舍**（我选「照样找得到」，理由=批处理一次性建几何，见 §二 `FindAbove`）——
   原版那一档的语义（inactive 的 `RectMask2D` 不裁）我们**没有**真机验证过，这属于**有意偏离**。

---

## 七、顺手发现（⚠️ **只报不改**）

1. **同一份「视口状态」在本仓已经有好几份平行载体**（都是阶段 2 该收编的对象）：
   `Shell/ItemDrawer.cs:173` 的 `ItemDrawerOptions.Clip`（+ `:187 ClipSoftness`）·
   `Shell/MatchLogRow.cs:26` · `Shell/PlayerProfileWindow.cs:508/516`（`ProfilePage` 里那一对）·
   `Shell/SocialWindow.cs:226`（`SocialPage`，另有写入口 `SetClip`/`ClipNow`）。
   ⚠️ **它们不是重影**：两处 `Clip` 都住在**页/视图类**里（不在 `GameWindow` 子类里）⇒ **今天没有字段遮蔽**
   （我按「有没有 `CS0108`」核过：`csc` 编译运行时程序集**零条** `CS0108/CS0109/CS0114`）。
   但迁移到节点时这四份都要一并看（否则会出现「节点一份 + 页里一份」的双状态）。
2. **`DeckCell` 的渲染那几层原来吃的是【未内缩】的窗 `Clip`**（`DeckCell(win,…)` 转发 `win.Clip` 而**不是** `win.RenderClip`），
   只有 `maskPad` 非零时才会与「渲染也读 padding」那条判据分家。今天两处调用点的 pad 全是 0
   ⇒ **无可观测差异**（`MenuDraw.cs:2275` 那一段自己写着这一点）。本件**没改这个语义**（改了就是改行为），
   但阶段 2 挂节点之后 `clipR` 会带上节点 pad ⇒ 那两处**行为会变**，届时要一起核。
3. `MenuDraw.cs` 里 `DeckCell(win,…)` 那条 `win == null` 警告的措辞我改了（`clip: null` 不再等于「显式不裁」），
   ⚠️ 若有自检按**文案**断言那条警告（`grep` 没找到，但只搜了 `.cs`），得跟着改。
4. `资料/普查产出_1012/` 目录下本报告是新文件；`ViewportClip.cs.meta` 见 §五·5。

---

## 八、自检状态（按简报：本轮**不跑** 11 条）
- ✅ **跑过**：`TMPDIR=/tmp/wf_h10 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（全仓无编译错误）。
- ⚠️ 同一次 `csc` 顺手核了**遮蔽类警告**（`CS0108/0109/0114`）⇒ **零条**（这是 §七·1 那条结论的判据）。
- ⛔ 未跑 Unity 批处理；⛔ 未跑 `_run_8_checks.sh`。
- 📌 影响面提示（调度台决定何时复跑）：本件动的是 **`Shell/MenuDraw.cs`（共用件）** + 新文件 ⇒
  按铁律 12 第 2 条，**碰共用件要先 `grep` 谁在用** —— `MenuDraw.{Rect,Nine,Tiled,Hit,ClipText,DeckCell}`
  的使用者覆盖**整个 `Shell/` + `Editor/*`** ⇒ 真要复跑就按覆盖面走**全套**（不是「只跑一条宿主」）。
