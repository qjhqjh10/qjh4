// ViewportClip.cs — 🔴 **裁切状态长在【视口节点】上**的那一份（= 原版「每个 `Viewport` 一个 `RectMask2D`」）
//
// ============================ 为什么要有这个文件（A198② 阶段 1）============================
// 原版的裁切**逐节点**：一个视口节点一个 `RectMask2D`、挂在 `ScrollRect` 的 `Viewport` 那一级
// （uGUI 标准位），**一扇窗里几个视口就有几个组件**，**参数只有两个**（`m_Padding` / `m_Softness`）。
// 我们这边原来是「**一扇窗一份**」（`GameWindow.Clip` / `ClipSoftness` / `ClipPad` 三兄弟 +
// `RenderClip` 转发，全族共享，纪律 = 谁设谁还原）⇒ **表达不了「同一扇窗两个视口同时要不同参数」**。
//
// 🔴 **本文件是 A198②【B 方案】的【阶段 1】—— 只做「节点状态能存在、能取、且与旧路共存」**：
//   · 新增本组件（挂在视口节点上）+ 一个**共用解析函数** `Resolve(...)`（`MenuDraw` 的取状态那一路转调它）；
//   · ⛔ **本阶段一个节点都不挂**，⛔ **52 个「设站点」一个都不动**（`grep -rn "AddComponent<ViewportClip>"`
//     在阶段 1 结束时应只命中本文件的 `Hang`/测试夹具）⇒ **今天行为逐位不变**（见下面「共存保证」）。
//   · 阶段 2 才把 52 个设点从「设字段」改成「挂节点」。
//
// ============================ 判据（逐条带出处）============================
// 🔴 **原版那两个字段**（本地 uGUI 源码，第一权威）：
//   `Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/RectMask2D.cs`
//   · `:50-51` `[SerializeField] private Vector4 m_Padding`（字段注释写明 **X = Left · Y = Bottom · Z = Right · W = Top**）
//   · `:70-71` `[SerializeField] private Vector2Int m_Softness`（**setter 里 `Mathf.Max(0, ·)`**）
//   · `:178-185` **射线那一面**：`IsRaycastLocationValid` → `RectangleContainsScreenPoint(rectTransform, sp, eventCamera, m_Padding)`
//     ⇒ **用的是 mask 自己那个 `rectTransform`**（`Graphic.Raycast` 沿父链逐个 `ICanvasRaycastFilter` 都判一遍）
//   · `:205` / `:226` **渲染那一面**：`PerformClipping()` → `Culling/Clipping.FindCullAndClipWorldRect`
//     （`…/UI/Core/Culling/Clipping.cs:17,26-30`：`xMin + offset.x` / `xMax − offset.z` / `yMin + offset.y` / `yMax − offset.w`）
//     ⇒ **渲染也读 `m_Padding`，而且两副面孔缩的是同一个框**（我们这边那两份实现的判据全文 →
//     `MenuDraw.PaddedClip` 上面那一大段；`A188` 那次订正就是这条）。
//
// 🔴 **原版侧的形状**（2026-10-12 · V8 普查 `资料/普查产出_1012/V8_判据补查.md` §A198② ②）：
//   · 计数：`bundle_menus_assets_all` 里 `RectMask2D` **150 个**（判据 = 同时有 `m_Padding` 与 `m_Softness`
//     且无 `m_RaycastTarget`）；全库 **222 个**（菜单 150 / 通用弹窗 5 / 战场 65 / 主菜单 2）。
//   · 挂法**固定**为「mask 挂 `Viewport`，`Scroll View` 是它的父」，**没有一层 mask 罩住另一层 mask 的写法**
//     （例：`Viewport ← Scroll View ← Content ← Reward Window`；`Collection Menu Variant` 一扇窗实读 6 个）。
//   · 参数分布：padding 全 0 的 52 个、`(−8,−5,−8,−5)` 34 个（**全是 `/Text Area`**）…；
//     softness `(0,0)` 最多，非零档有 `(42,0)/(0,174)/(0,50)/(0,25)/(100,0)/(200,0)` …
//   ⚠️ **那张表只覆盖「出厂那一档」**（V8 只读了序列化值，没找赋值点，见铁律 5·c）——
//     本文件沿用同一口径：**本组件的字段就是「这个视口那一份」**，不去猜运行时有没有人改它。
//
// ============================ 🔴 A811 根治（2026-10-16）：框的帧 = 被比矩形的帧 ============================
// **病灶**（全文 → `资料/普查产出_1015/W19_A826A822A811.md` §四·2 · `W20_A811修法与A822断言.md` §四·2）：
//   「看框」这一路原来读的是**节点当下的实时位置**（`ClipPx` 的世界坐标反推 / `MenuScroll.ClipNode` 转调它），
//   而**被比的矩形**是**基准位的绝对设计矩形**（各宿主给的 `Abs(...)` / `MenuScroll.Shift(...)`）。
//   两者在「节点或它的**祖先**被挪过」时**不在同一帧** ⇒ 交集判断恒假 ⇒
//   收起态（整条筛选列被抽屉挪到屏左外 −385px）下发生的**任何一次重建**都会把**所有带闸的件**判成框外。
//
// **为什么「基准位」才是对的那一帧**（不是选出来的口味，是这套模型的必然）：
//   `MenuDraw.Local(parent, r)` 写的是 `RectCenter(r) − PosInDesignSpace(parent)` ⇒ 子件的世界位置
//   `= k × RectCenter(r)` —— **父件挪到哪都抵消掉了**。也就是说：**这一层建出来的每一件，落点只由「传进去
//   那个 `PxRect`」决定**，与任何节点的实时 transform 无关。⇒ 传进 `MenuDraw.*` 的 `PxRect` 与框**必须**同帧，
//   而那一帧就是「宿主写这个矩形时用的那个设计帧」（本文件叫它**基准位**）。
//
// **修法（只有一句话）**：**框的中心 = 宿主写进这个节点的那份设计矩形**（`BaseRect`，由 `Hang` /
//   `MenuDraw.ApplyPxRect` 两条「写矩形」的路自动记下），**不再从实时 transform 反推**。
//   ⇒ 「谁跟着谁」= **看框跟着被比矩形走**；节点（以及它的祖先）被挪动**不再改变框**。
//   ⚠️ 这一条**有意偏离原版**：原版 `RectMask2D` 挂哪就跟哪（祖先挪它也挪）。但原版内容也是
//   **跟着掩码一起挪**的，而我们这一层的内容是**按绝对设计矩形烘焙**的 ⇒ 框跟着挪反而与内容脱钩。
//   ⚠️ **尺寸那一项照旧取节点自己的 `rect`**（尺寸与「祖先挪没挪」无关，取实时值永远不会过期）——
//   **只有中心换了来源**（见 `ClipPx`）。
// ============================ 我们这边的三处口径（⛔ 别在别处再写第二份）============================
// ① **框从哪来**：原版 `RectMask2D` 用的是**它自己那个 `rectTransform`** ⇒ 这里也取**节点自己的 rect**
//    （`ClipPx`）。世界 → 设计 px 走 `MenuDraw.PosInDesignSpace` + `LayoutSpace.ToPixel`，
//    尺寸那一项 `× (DesignPxH ÷ DesignHeight)` —— **与 `MenuDraw.QuadRectPx` 同一份口径**
//    （⚠️ 连它那一档已知边界也一起继承：节点**自带 `localScale`** 时那条恒等式不成立，见 `QuadRectPx` 的注释）。
//    🔴 **2026-10-16（A811 根治）就地订正（铁律 5）**：上面这一句「取节点自己的 rect」**只在下面两档成立** ——
//      · **中心**：记过 `BaseRect` 的节点 **不再读实时 transform**（= 本节第一段那条修法）；
//      · **尺寸**：照旧读节点自己的 `rect`（`sizeDelta`，与祖先位移无关）。
//      ⛔ **别把尺寸也改成读 `BaseRect`**：那样任何一次「只写尺寸」的 `SetPxSize` 都会让框**静默停在旧大小**。
//      ⛔ **也没必要**把 `PosInDesignSpace` 那一趟「世界 → 设计」继续算一遍：记过矩形的节点**绕开了那条
//      恒等式**（它自带「窗根必须在世界原点」那个已知前提，见 `PosInDesignSpace` 的注释）——
//      这是本修法的**顺带收益**，⛔ 别把它改回去。
//    🔴 **2026-10-18（`A840` 收口）就地订正（铁律 5）**：这一栏原来列的是「**没记过 `BaseRect` 的节点**」
//      （`Shell/{ShopWindow,AvatarTab,TitleTab,LiveOpsEventWindow,PracticeModePopup×2}.cs`）—— **那一列现在是空的**：
//      当天按 `grep -rn "AddComponent<ViewportClip>"` **现扫**出的生产站点是 **7 处**，**逐处都补了显式 `CaptureNow()`**
//      （`Shell/ShopWindow.cs` · `AvatarTab.cs` · `TitleTab.cs` · `LiveOpsEventWindow.cs` ·
//        `PracticeModePopup.cs` **×2** · **`TutorialModePopup.cs`**）。⇒ 今天**没有**「逐位回落到旧写法」的视口节点了。
//      ⚠️ **原来那份清单漏了第 7 处 `TutorialModePopup.cs`** —— ⛔ **下一轮别再照这份清单收口，按 `grep` 现扫**
//      （现场与判据 → `资料/普查产出_1018/S1_A840与A867.md` §1）。
//      ⛔ **仍然不许**在 `OnEnable` 里自动抓一份：节点可能是**已经被挪过之后**才挂的组件，抓到的会是一个**错帧**的基准，
//      而且**静默**。要接就显式调 `CaptureNow`（新加视口节点时**必须**照上面这 7 处的形状补一句）。
// ② **符号约定**：`padding` **正值 = 缩小、负值 = 扩大**（同 `MenuDraw.PaddedRect`，判据 = 上面 `Clipping.cs:26-30`）。
// ③ **两条路读同一份状态**（A188 的硬约束）：解析结果 `ClipState` 给两副面孔各一个视图 ——
//    **渲染那一份 = `RenderClip`（= `V − pad`）**、**命中那一份 = 裸 `Clip` + `Pad`**（由 `MenuDraw.Hit`
//    自己把 pad 缩在 `clip` 上）。⛔ **不许只改渲染那一半**。
//
// ============================ 共存保证（阶段 1 的验收）============================
// 解析顺序（**一处**，`Resolve` 里那三段）：
//   ① **显式形参非空 ⇒ 形参赢，而且【连父链都不走】**（= 旧路逐位不变）；
//   ② 否则**沿父链找最近的 `ViewportClip`**（含 `parent` 自己）⇒ 节点赢；
//   ③ 都没有 ⇒ 回落成形参本身（今天没有任何节点 ⇒ **恒走这一支**）。
// ⇒ **今天全仓没有任何节点挂本组件**（阶段 1 不挂）⇒ 每个调用点都落在 ① 或 ③，**行为逐位不变**。
//    可观测点：`NodeResolutions`（走节点态的次数）在既有自检里应恒为 `0`。
// 🆕 **2026-10-12（A435①）：【取状态那一路】已补全**（这是阶段 2 的第一块，⛔ 52 个设点**一个没动**）：
//    · `MenuDraw.ClipText` 收尾把**调用方原样那份形参**交给 `ClippedTextGuard`（不再交「解析后的快照」）
//      ⇒ 守卫**每次重裁都重新解析**（节点后挂/挪动跟得上；旧路那一档逐位不变）；
//    · `MenuDraw` 新增两个**节点态重载** `VisibleAbove` / `ClipRectAbove`（纯矩形函数手上没有 `Transform`，
//      只能由调用方先解析再传 ⇒ 这是给阶段 2 调用点的一句式接口）；
//    · `MenuWindowBase.Text` / `TextBox` 那两处 `if (RenderClip.HasValue)` 守卫改成吃**解析后**的那一份
//      ⇒ 本窗没设 `Clip` 时父链上的节点才接管（今天无节点 ⇒ 逐位不变）。
//    ⛔ 仍然**没有**任何一处挂节点；⛔ 其余 7 处同形守卫（`WindowsManager` / `CollectionWindow` /
//    `ChatPanel` / `PlayerProfileWindow` / `ItemDrawer` / `SettingsWindow` / `AllianceMemberTab` /
//    `PracticeModePopup`）**未动** —— 它们吃的是**自己那一份** clip（不是 `GameWindow` 三兄弟），
//    要一起收编得先定「页自己的 clip 与节点谁优先」，那是调度台的事（详见 H25 报告 §待接线）。
//    ⚠️ 于是本轮的**可观测不变量**变成两条（阶段 1 只有第一条）：
//      · `NodeResolutions == 0`（没有节点 ⇒ 一次都不走节点态）；
//      · 🆕 `NodeShadowedByParam == 0`（没有节点 ⇒ 也不可能被形参盖住）—— 见那个计数的注释。
// ⚠️ **为什么把「显式形参」排在节点前面**（这是本件的一处判断，调度台可一键翻转）：
//    「形参」正是**旧路状态（`RenderClip` / `Clip` / `ClipPad` / `ClipSoftness`）的载体** ——
//    各窗把它从 `GameWindow` 那三兄弟转进来。⇒「形参非空」**就是**「旧路还在设」。
//    排成这一档 ⇒ **迁移期是「旧路优先、把旧的设站点删掉那一刻节点才接管」**，每一步都可回退；
//    反过来（节点优先）则「挂上节点的那一刻行为就变」，而且会让**故意传 null 的那些站点**
//    （例：`Shell/MenuWindowBase.cs` 里「左栏不在任何滚动视口里」那条注释、`Shell/MenuWindowBase.cs` 的
//    `clip:` 默认值那一族）被**上面某处**的节点悄悄裁掉。⛔ 翻转前请先读这一段。

using UnityEngine;

namespace CardPresentation
{
    /// <summary>一次「取裁切状态」的结果 —— 🔴 **`MenuDraw` 那几路取状态【只此一份】**（A198② 阶段 1）。
    /// <para>它同时装着**两副面孔要的东西**（原版那两个字段各一副，见文件头）：
    /// `Clip` + `Pad` 是**命中那一份**的输入（`V` 与 `V − pad`）、<see cref="RenderClip"/> 是**渲染那一份**。</para>
    /// <para>⚠️ **别自己拿 `Clip` 当渲染边界** —— 渲染边界是 `RenderClip`（= `PaddedClip(Clip, Pad)`）；
    /// 反过来命中区**别拿 `RenderClip`**（那会把 pad 内缩两次，见 `MenuDraw.Hit` 的 `maskPad` 注释）。</para></summary>
    public readonly struct ClipState
    {
        /// <summary>mask 自己那个框（**未内缩**）。`null` = 没有裁切。</summary>
        public readonly PxRect? Clip;
        /// <summary>= 原版 `RectMask2D.m_Softness`（画布像素：x 管左右、y 管上下；`(0,0)` = 硬边）。</summary>
        public readonly Vector2 Softness;
        /// <summary>= 原版 `RectMask2D.m_Padding`（X=Left · Y=Bottom · Z=Right · W=Top；**正值缩小**）。</summary>
        public readonly Vector4 Pad;
        /// <summary>`true` = 这一份来自 `ViewportClip` 节点；`false` = 来自形参（= 旧路）。
        /// 自检可以拿它断「这条路带电」（只断「结果非空」是不够的，那种断言改坏实现不会红）。</summary>
        public readonly bool FromNode;

        /// <summary>🔴 **2026-10-14（A434）**：`true` = 这一份是「**显式不裁**」那个哨兵（见 <see cref="NoClip"/>）。
        /// ⚠️ 它与「普通的 `Clip == null`」**在结果上等价、在意图上不同**：前者是**明确的否定**
        /// （调用点写了 `clip: MenuDraw.NoClip`），后者是「本来就没有裁切」。</summary>
        readonly bool _optOut;
        public bool IsNoClip { get { return _optOut; } }

        /// <summary>🔴 **2026-10-14（A434）「显式不裁」的状态值** —— `ViewportClip.Resolve` 在收到
        /// `MenuDraw.NoClip` 哨兵时**原样返回它**（⛔ 不查父链）。自检可以拿 `IsNoClip` 断「这条路真带电」。</summary>
        public static readonly ClipState NoClip = new ClipState(null, Vector2.zero, Vector4.zero, false, true);

        public ClipState(PxRect? clip, Vector2 softness, Vector4 pad, bool fromNode, bool optOut = false)
        {
            Clip = clip; Softness = softness; Pad = pad; FromNode = fromNode; _optOut = optOut;
        }

        /// <summary>🔴 **渲染那一份** = `Clip` 按 `Pad` 内缩（判据 → `MenuDraw.PaddedClip`）。
        /// 喂给 `MenuDraw.Rect` / `Nine` / `Tiled` / `ClipText` / `Visible` 的就是这一份
        /// （⚠️ 那几路**自己也会解析一次** —— 本属性是给调用方/自检看的同一份算式，不是第二个入口）。</summary>
        public PxRect? RenderClip { get { return MenuDraw.PaddedClip(Clip, Pad); } }

        public override string ToString()
        {
            return $"[ClipState 从节点={FromNode} clip={(Clip.HasValue ? Clip.Value.ToString() : "null")} "
                 + $"render={(RenderClip.HasValue ? RenderClip.Value.ToString() : "null")} "
                 + $"pad=({Pad.x},{Pad.y},{Pad.z},{Pad.w}) soft=({Softness.x},{Softness.y})]";
        }
    }

    /// <summary>🔴 **挂在【视口节点】上的裁切状态**（= 原版 `Viewport` 上那个 `RectMask2D`）。
    /// 一扇窗里**可以挂多个**（原版 `Collection Menu Variant` 一扇窗实读 6 个）—— 这正是它相对
    /// 「一扇窗一份」的 `GameWindow.Clip` 三兄弟的意义。
    ///
    /// <para>**字段只有两个**，逐字照原版：<see cref="padding"/>（`m_Padding`）· <see cref="softness"/>（`m_Softness`）。
    /// 框本身**不长在这里** —— 它就是**这个节点自己的 rect**（原版 `RectMask2D` 用的也是自己的 `rectTransform`），
    /// 由 <see cref="ClipPx"/> 换算。</para>
    ///
    /// <para>🔴 **建节点一律走 <see cref="Hang"/>**（或 `MenuDraw.Node` / `ApplyPxRect`）——
    /// 它保证节点是 `RectTransform` **且 `sizeDelta` 已写**（锚点重合 ⇒ `rect` 只由 `sizeDelta` 决定）。
    /// 手搓的 `RectTransform`（没写过 `sizeDelta`）框会是引擎给的那份默认值 ⇒ **框不对但不报错**。</para>
    ///
    /// <para>⚠️ **本组件不做任何渲染**：它只是**状态**（原版那个组件自己在 shader 里削像素；
    /// 我们是「建的时候把几何截到框内」，两条后端见 `MenuDraw.ClipRect` / `ClipText`）。</para></summary>
    [DisallowMultipleComponent]
    public sealed class ViewportClip : MonoBehaviour
    {
        // ============================================================ 两个字段（照原版类型）
        /// <summary>**原版 `RectMask2D.m_Padding`**（`RectMask2D.cs:50-51`）·
        /// `(x=Left, y=Bottom, z=Right, w=Top)`、**正值缩小、负值扩大**
        /// （判据 = `Culling/Clipping.cs:26-30`，同一份算式在 `MenuDraw.PaddedRect`）。
        /// 🔴 **两副面孔都吃它**（渲染那一份走 `ClipState.RenderClip`、命中那一份由 `MenuDraw.Hit` 现算）——
        /// ⛔ 别只在 `Rect` 那一路上接它。</summary>
        public Vector4 padding = Vector4.zero;

        /// <summary>**原版 `RectMask2D.m_Softness`**（`RectMask2D.cs:70-71`）。
        /// 🔴 **原版类型是 `Vector2Int`** ⇒ 这里也用 `Vector2Int`（V8 实读那 150 个实例，值全是整数、
        /// 非零档 `(42,0)/(0,174)/(0,50)/(0,25)/(100,0)/(0,200)` 里没有小数）。
        /// 取值时经 <see cref="SoftnessPx"/> 转 `Vector2` 并**按原版 setter 夹 `Mathf.Max(0, ·)`**
        /// （我们这边是公开字段、没有 setter ⇒ 夹在【读】的那一处，只有一份）。</summary>
        public Vector2Int softness = Vector2Int.zero;

        // ============================================================ 🔴 基准矩形（A811 根治 · 2026-10-16）
        /// <summary>🔴 **宿主写进这个节点的那个【设计矩形】**（画布 px · 左上原点）—— 框的**中心**取它、
        /// **不取实时 `transform`**（病灶 / 为什么 → 文件头那一节 + <see cref="ClipPx"/>）。
        /// <para>**谁写**（只该有这两条路）：① <see cref="Hang"/>（建节点时那一份 `r`）；
        /// ② `MenuDraw.ApplyPxRect`（**任何**把这个节点的矩形重写一遍的地方都自动带上 ——
        /// 调用点一个字都不用改，⚠️ 代价 = 每建一个节点多一次 `GetComponent`，相对建几何是噪声级）。</para>
        /// <para>⚠️ **「写 `localPosition` 直接挪节点」那种写法不算**（`CollectionWindow.ApplyDrawerSlide`
        /// 就是它）—— 那正是本字段要**忽略**的那一类位移：内容矩形不会跟着挪，框也不该跟着挪。</para></summary>
        PxRect _baseRect;
        bool _hasBaseRect;

        /// <summary>这个节点记过设计矩形没有（= `ClipPx` 现在走哪一支的判据；自检可以拿它断「谁跟着谁」）。
        /// 🔴 **2026-10-18（`A840` 收口）就地订正（铁律 5）**：这一行原来写「**`false` 的那一批是【还没接上】的站点**」——
        /// **那一批现在为空**：当天按 `grep` 现扫出的 **7 处生产站点全部补了显式 `CaptureNow()`**（清单 → 文件头 §① 那条订正）
        /// ⇒ 今天**再读到 `false`，那就是新加的视口节点（或新写法）漏了那一句**，**是缺陷、不是「还没接上」**。</summary>
        public bool HasBaseRect { get { return _hasBaseRect; } }

        /// <summary>记下来的那个设计矩形（**画布 px**，与 `ClipPx` 同一个量纲/原点）。
        /// ⚠️ `HasBaseRect == false` 时它是 `default(PxRect)`（**无意义**）—— 先看那个标志再读它。</summary>
        public PxRect BaseRect { get { return _baseRect; } }

        /// <summary>记下「这个节点被写进去的那个设计矩形」（口径 / 谁该调 → <see cref="BaseRect"/>）。
        /// ⛔ **别在别处随手调**：那等于把框冻在一个**没被写进节点**的矩形上（比现在的不一致更难查）。</summary>
        public void SetBaseRect(PxRect r) { _baseRect = r; _hasBaseRect = true; }

        /// <summary>🆕 **把当前的实时框抓成基准**（= 给那些**没走 `Hang`**、只 `AddComponent&lt;ViewportClip&gt;()`
        /// 的站点用的一次性接口；清单 → 文件头 §①）。实现 = `SetBaseRect(ClipPx_now)`，
        /// 所以**只能在「节点还在它该在的基准位」时调**（建树那一刻）。
        /// <para>🔴 **2026-10-18（`A840` 收口）就地订正（铁律 5）**：这一行原来写「**今天的调用点 = 0**（那几处都在别的文件、
        /// 不在 A811 那一件的白名单里 ⇒ 只报不改）」—— **那已经过期**：当天 **7 处生产站点全部接上了**（`:428`/`:167`/`:140`/`:614`/`:883`/`:1051`/`:510` 那一组）。
        /// **新加视口节点时必须照它们的形状补一句**（⛔ 不是「可选」）。</para>
        /// <para>⛔ 在节点**已经被挪过之后**调它会冻结一个**错帧**的基准，而且**静默**（这正是本类拒绝在 `OnEnable` 里自动抓的理由）。</para></summary>
        public void CaptureNow() { var b = ClipPx; if (b.HasValue) SetBaseRect(b.Value); }

        // ============================================================ 状态 → px 框（换算只此一份）
        /// <summary>本组件的两个字段 + 节点自己的 rect → 一份 `ClipState`（`FromNode = true`）。
        /// ⚠️ 节点给不出框时（不是 `RectTransform`）`Clip` 是 `null` 且**出声**（见 `ClipPx`）——
        /// 那一档解析函数会**回落到旧路**，⛔ 不会静默裁成 0 面积。</summary>
        public ClipState State { get { return new ClipState(ClipPx, SoftnessPx, padding, true); } }

        /// <summary>原版 `m_Softness` 的取值视图（夹非负，见字段注释）。</summary>
        public Vector2 SoftnessPx
        {
            get { return new Vector2(Mathf.Max(0, softness.x), Mathf.Max(0, softness.y)); }
        }

        /// <summary>🔴 **这个视口那个框**（画布像素 · 左上原点）。
        ///
        /// <para>**中心**走两支（判据 / 为什么 → 文件头那一节）：
        /// ① 记过 <see cref="BaseRect"/>（`Hang` / `MenuDraw.ApplyPxRect` 写进去的那份）⇒ **就用它** ——
        ///    `PxRect` 本来就是画布 px / 左上原点，**直接取 `CX`/`CY`，连一次换算都不做**；
        /// ② 没记过 ⇒ 照旧从**实时 transform** 反推：`MenuDraw.PosInDesignSpace` +
        ///    `LayoutSpace.ToPixel`（那一档连 `PosInDesignSpace` 「窗根必须在世界原点」的已知前提一起继承）。
        /// **尺寸**（两支共用）= 节点自己的 `rect ÷ 2 × (DesignPxH ÷ DesignHeight)`，**与 `MenuDraw.QuadRectPx`
        /// 同一份口径** —— 尺寸与「祖先挪没挪」无关，取实时值反而永远不过期（⛔ 别改成读 `BaseRect`）。</para>
        ///
        /// <para>⚠️ **它并不是一个「纯几何量」**（原版 `RectMask2D` 是）—— 这一点是**有意**的，理由见文件头。</para>
        ///
        /// <para>返回 `null` = **这个节点给不出框**（没记过矩形**且**不是 `RectTransform`）—— 出声一次（限流）并
        /// 由 `Resolve` 回落到旧路。⛔ 别改成「返回一个 0 面积矩形」：那会把整棵子树裁没、且**静默**。</para></summary>
        public PxRect? ClipPx
        {
            get
            {
                // 🔴 ① 记过设计矩形 ⇒ 中心用它（**这是 A811 根治那一行**：框不再读实时 transform 的中心）
                if (_hasBaseRect)
                {
                    RecordedRects++;
                    var brt = transform as RectTransform;
                    if (brt == null)
                    {
                        // 记过矩形就说明建节点那一趟是走 `MenuDraw.Node`（恒 `RectTransform`）⇒ 走到这里
                        // 只可能是**节点被换过组件**。⛔ 不许静默给一个 0 面积框（那会把整棵子树裁没）
                        // ⇒ 照旧出声 + 回落（与下面那一支同一句话，只是这一支**有矩形却没得量尺寸**）。
                        WarnNotRect("有设计矩形、但拿不到 `rect`（组件被换过？）");
                        return null;
                    }
                    const float KB = LayoutSpace.DesignPxH / LayoutSpace.DesignHeight;   // 108 px / 单位（同 `QuadRectPx`）
                    float hwB = brt.rect.width * KB * 0.5f;
                    float hhB = brt.rect.height * KB * 0.5f;
                    return new PxRect(_baseRect.CX - hwB, _baseRect.CY - hhB,
                                      _baseRect.CX + hwB, _baseRect.CY + hhB);
                }

                // ② 没记过 ⇒ 旧路：中心从实时 transform 反推（⚠️ 这一档与「祖先被挪过」不同帧 —— 病灶那一档）
                var rt = transform as RectTransform;
                if (rt == null)
                {
                    WarnNotRect("拿不到 `rect`");
                    return null;
                }
                LiveDerivations++;
                // ⚠️ `rect` 是**本地**尺寸（设计世界单位），与 `sizeDelta` 同量纲（锚点被 `MenuDraw.SetPxSize`
                //    强制成重合 ⇒ `rect` 只由 `sizeDelta` 决定）；乘 K 才是画布 px。**父链缩放不该在这里除**
                //    （父链缩放只影响「画出来多大」，坐标那半边已经由 `PosInDesignSpace` 除过了）。
                const float K = LayoutSpace.DesignPxH / LayoutSpace.DesignHeight;   // 108 px / 单位（同 `MenuDraw.QuadRectPx`）
                Vector2 c = LayoutSpace.ToPixel(MenuDraw.PosInDesignSpace(transform));
                float hw = rt.rect.width * K * 0.5f;
                float hh = rt.rect.height * K * 0.5f;
                return new PxRect(c.x - hw, c.y - hh, c.x + hw, c.y + hh);
            }
        }

        /// <summary>两支共用的告警（限流 3 条）—— 文案里那一句「按「没有节点」处理」对两支都成立。</summary>
        void WarnNotRect(string why)
        {
            if (UnusableNodes++ < 3)
                Debug.LogWarning($"[ViewportClip] 「{name}」身上挂了 `ViewportClip`，但它不是 `RectTransform` "
                               + $"（{why}）⇒ **这一处视口不生效**、按「没有节点」处理。"
                               + "建视口节点请走 `ViewportClip.Hang` / `MenuDraw.Node`。");
        }

        // ============================================================ 建节点（阶段 2 的入口；夹具也走它）
        /// <summary>按原版矩形建一个**视口节点**并在它身上挂 `ViewportClip`，返回那个组件。
        /// 建节点走 `MenuDraw.Node`（⇒ `RectTransform` + 锚点重合 + `sizeDelta` 已写，见 `ClipPx` 的告警）。
        /// <para>🔴 **阶段 2 的迁移就是把「设 `win.Clip`/`ClipPad`/`ClipSoftness`」换成这一句**。
        /// ⚠️ **2026-10-16 就地订正（铁律 5，A811 顺手核出来的）**：原文写「阶段 1 只有自检夹具会调它」——
        /// **早就不成立了**：阶段 2（A435）起生产侧有 **54 处 / 21 个文件**
        /// （`CampaignTab` 8 · `CollectionWindow` 6 · `ForgeTab` 5 · `AllianceMemberTab` 5 · `LeaderboardWindow` 3 ·
        /// `InboxWindow` 3 · `DailyStreakPopup` 3 · `CampaignRewardWindow` 3 · …；
        /// 现读命令 = `grep -rn "ViewportClip.Hang(" Shell/*.cs`）。⛔ 别照旧话去「改造夹具」——
        /// 该改的是**生产调用点**。</para></summary>
        public static ViewportClip Hang(Transform parent, string name, PxRect r, Vector4 pad, Vector2Int soft)
        {
            var node = MenuDraw.Node(parent, name, r);
            var vc = node.gameObject.AddComponent<ViewportClip>();
            vc.padding = pad;
            vc.softness = soft;
            // 🔴 2026-10-16（A811 根治）：把「写进这个节点的那个矩形」记下来（= 框的中心那一帧）。
            //    ⚠️ **必须在 `AddComponent` 之后** —— `MenuDraw.Node` 里那次 `ApplyPxRect` 写矩形时
            //    组件还不存在（那一句的穿透写在 `MenuDraw.ApplyPxRect` 尾），只靠它这里会空。
            vc.SetBaseRect(r);
            return vc;
        }

        // ============================================================ 共用解析（🔴 全壳取状态只此一份）
        /// <summary>🔴 **`MenuDraw` 那几路「取裁切状态」的【唯一入口】**（A198② 阶段 1）——
        /// 调用点的形参原样传进来，返回**这一处该用的那一份状态**。三段优先级（判据/理由 → 文件头）：
        /// <list type="number">
        /// <item>**形参 `clip` 非空 ⇒ 形参赢**（= 旧路，**连父链都不走**）—— 这是逐位不变的保证；</item>
        /// <item>否则**沿父链找最近的 `ViewportClip`**（含 `parent` 自己）⇒ 节点赢；</item>
        /// <item>都没有 ⇒ **回落到形参本身**（今天恒走这一支 ⇒ 与旧实现逐位相同）。</item>
        /// </list>
        /// <para>⚠️ **第 2 步用的是 `GetComponent`【逐级】而不是 `GetComponentInParent&lt;T&gt;()`**：
        /// 后者带一个 `includeInactive` 形参（本地 Unity 文档 `UnityEngine.CoreModule.xml:5988-5996` 逐字：
        /// *Whether to include inactive parent GameObjects in the search.*），
        /// 🔴 **而那份 XML 没写它的默认值**（= 本机查不到，**不拿它当判据**）⇒ 干脆不用它：
        /// 逐级 `GetComponent` **不看 `activeSelf`**、取舍由下面这一句定死。
        /// **为什么「照样找得到未激活的节点」才对**：这个壳是**批处理里一次性建几何**的
        /// （页签关着时也在建：`Forge Tab` 出厂 `activeSelf=false` 那种），漏找 = **建的时候不裁、
        /// 后来显示了也不裁**（静默，且几何不再重算）。⚠️ **这是有意偏离**（原版 inactive 的
        /// `RectMask2D` 不裁）—— 记在报告 §六·5 里。</para>
        /// <para>⚠️ **代价**：只有 `clip == null` 时才走父链（形参非空第一条就返回）⇒ 本阶段（无节点）
        /// 每次至多几次 `GetComponent`，相对 `ImageQuad.Create` 那种建网格是噪声级。**没做缓存** ——
        /// 缓存与「阶段 2 随时新挂节点」冲突（缓存会是**静默失效**，正是本工程的红线）。</para></summary>
        /// <param name="parent">从哪个节点往上找（= 要被裁的那个件挂在哪；它是 `parent` 本身也算）。</param>
        /// <param name="clip">调用点原来那个 `clip` 形参（**非空 = 显式覆盖**，见上面第 1 条）。</param>
        /// <param name="softPx">调用点原来那个软边形参（回落时原样带出）。</param>
        /// <param name="pad">调用点原来那个 pad 形参（渲染那几路传 `Vector4.zero`、命中那两路传 maskPad）。</param>
        public static ClipState Resolve(Transform parent, PxRect? clip, Vector2 softPx, Vector4 pad)
        {
            // ①′ 🔴 **2026-10-14（A434）「显式不裁」哨兵**：传 `MenuDraw.NoClip` = **明确的否定** ——
            //     连父链都不看，直接给一个「没有裁切」的状态（⛔ 与「传 `null`」是两件事：
            //     `null` = 按父链解析）。记个数，让自检能钉住「这条路真被走过」。
            if (MenuDraw.IsNoClip(clip))
            {
                OptOuts++;
                NodeShadowedByParam++;      // 同 `①` 的理由：父链上还挂着节点这件事仍然要看得见
                return ClipState.NoClip;
            }

            // ① 显式形参 = 旧路还在设 ⇒ 原样返回（**不走父链**）
            if (clip.HasValue)
            {
                // 🆕 **2026-10-12（A435①）：显式形参赢 ⇒ 但「父链上还挂着节点」那种「双状态」必须看得见。**
                //    按设计这时的行为是「旧路赢」（阶段 2 的迁移纪律：**删掉旧设站点那一刻节点才接管**，
                //    见文件头那段先后顺序）—— 所以**不能出声报警**（那是设计行为、不是缺陷），
                //    但它是**迁移漏删旧设站点**时唯一会留下的痕迹 ⇒ 记个数，让自检能把它钉成 0。
                if (FindAbove(parent) != null) NodeShadowedByParam++;
                return new ClipState(clip, softPx, pad, false);
            }

            // ② 沿父链找最近的节点
            var node = FindAbove(parent);
            if (node != null)
            {
                var st = node.State;
                if (st.Clip.HasValue)
                {
                    NodeResolutions++;
                    return st;
                }
                // 节点在、但给不出框（不是 `RectTransform`）：`ClipPx` 已经出声过一次 ⇒ 回落旧路
            }

            // ③ 旧路（= 形参本身；今天恒走这一支）
            return new ClipState(null, softPx, pad, false);
        }

        /// <summary>沿父链找最近的 `ViewportClip`（**含 `t` 自己**）。逐级 `GetComponent`（不看 `activeSelf`，
        /// 理由 → `Resolve`）。找不到返回 `null`。</summary>
        public static ViewportClip FindAbove(Transform t)
        {
            for (; t != null; t = t.parent)
            {
                var v = t.GetComponent<ViewportClip>();
                if (v != null) return v;
            }
            return null;
        }

        // ============================================================ 可观测点（自检断「这条路带电」用）
        /// <summary>走「**节点态**」那一支的次数（= 解析函数真的从某个 `ViewportClip` 取到了框）。
        /// 🔴 **阶段 1 的共存保证就是断它 == 0**（今天没有任何节点 ⇒ 每个调用点都落在第 1/3 支）；
        /// 阶段 2 与夹具则断它 **&gt; 0**（只断「结果非空」不行 —— 那种断言改坏实现不会红）。
        ///
        /// <para>⚠️ **2026-10-12（A435①）：「谁调它」多了一处** —— `ClippedTextGuard` 现在**每次重裁都重新解析**
        /// （`CurClip`，见 `MenuDraw.ClippedTextGuard` 的类注释）⇒ 一个被裁过的标签每重排一次就 +1
        /// （节点态下）或 +0（旧路那几支，形参非空 ⇒ 第 1 支就返回、根本不到这里）。
        /// ✅ **今天仍然是 0**：全仓没有节点 ⇒ 每一条都落在第 1/3 支（不变量不变，`== 0` 那条断言照旧成立）。</para></summary>
        public static int NodeResolutions;
        /// <summary>🔴 **2026-10-14（A434）**：「**显式不裁**」那条路走了几次（= 收到 `MenuDraw.NoClip` 哨兵）。
        /// 自检用它断「这条路真带电」（⛔ 只断「结果没裁」是不够的 —— 今天大多数站点本来就不裁）。</summary>
        public static int OptOuts;
        /// <summary>节点挂了、但**给不出框**的次数（`ClipPx` 那条 `RectTransform` 告警的计数，
        /// 只用来限流那 3 条 warning；⚠️ **不是**「节点被形参盖住」的计数 ——
        /// 形参赢是**设计如此**（见文件头那段先后顺序），不当缺陷、也不出声）。</summary>
        public static int UnusableNodes;

        /// <summary>🆕 **2026-10-12（A435①）：解析时「父链上明明有节点，却被非空的显式形参盖住」的次数。**
        /// <para>🔴 **它是阶段 2 迁移期的【漏删探测器】**：按设计「形参非空 ⇒ 形参赢、且**连父链都不走**」
        /// （= 删掉旧设站点那一刻节点才接管，见文件头）—— 于是**旧设站点没删干净**时，
        /// 表现恰好是「节点挂在着、却一个像素都没生效」，而**行为一切正常、不出声、断言全绿**（静默）。
        /// 这个计数就是那种状态的唯一痕迹。</para>
        /// <para>**怎么断**：迁移完成（52 个设点全部改成挂节点）之后，把它断成 **0**；
        /// ⚠️ **迁移进行中它会非 0，那是【正常】的**（一个视口两处真值只可能出现在过渡期）
        /// —— 所以它是**进度指标**、不是缺陷计数，⛔ 别拿它当断言去卡迁移中的每一批。</para>
        /// <para>⚠️ 代价：形参非空那条**现在也会走一次父链**（原来第一句就返回）—— 每级一次
        /// `GetComponent`，相对建几何是噪声级（同 `FindAbove` 那条注释）。</para></summary>
        public static int NodeShadowedByParam;

        /// <summary>🆕 **2026-10-16（A811 根治）**：`ClipPx` 走「**记下来的设计矩形**」那一支的次数
        /// （= 框与被比矩形**同一帧**的那一档 · 每个被裁过的件每次取状态各 +1）。</summary>
        public static int RecordedRects;

        /// <summary>🆕 **2026-10-16（A811 根治）**：`ClipPx` 走「**实时 transform 反推**」那一支的次数
        /// （= 还没记过矩形的节点 · 文件头 §① 那条订正里列的那一批）。
        /// <para>🔴 **它是「根治还没盖到哪些节点」的进度指标**，⛔ **不是缺陷计数**（那一档今天逐位 = 改前的行为）。
        /// 判据用法：某个宿主把它的视口改成 `Hang` / 补一句 `CaptureNow()` 之后，这个数的**增量**应当变小
        /// （⚠️ 它是**全局**静态、跨窗累计 ⇒ 只能在「同一段场景里、同一个动作前后」做差分，别横向比绝对值）。</para></summary>
        public static int LiveDerivations;
    }
}
