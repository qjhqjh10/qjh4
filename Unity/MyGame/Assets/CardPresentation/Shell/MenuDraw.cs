// MenuDraw.cs — 菜单界面**共用的绘图助手**（原版像素矩形 → 世界坐标 → 摆一张图 / 一段字）
//
// ============================ 为什么要单独一份 ============================
// 阶段二这几层（`RewardsWindow` · `PromptPopup` · `DailyRewardPopup` · 后面还有锻造厂/商店/卡组线）
// 都要做同一件事：拿**原版 prefab 的像素矩形**（左上原点、y 向下，见 `资料/*_原版规格.md` 的表）
// 在场景里摆一个 `ImageQuad` / `Label`。这套换算**只能有一份**（CLAUDE.md §三：
// 「两处写同一条规则 = 迟早不一致」）。
//
// 🔴 **三条已经踩过的坑，都固化在这里**：
//   ① `ImageQuad.Create` / `Label.Create` 的 `pos` 是 **`localPosition`**（相对父节点），
//      而 `LayoutSpace.RectCenter` 给的是**世界坐标** ⇒ 必须减掉父节点的世界位置
//      （不减 = 父节点一有偏移就**双倍错位**，而断言量矩形中心、量不到）。
//   ② 分层用**渲染队列**、不能用 z（全是透明队列，按到相机的 3D 距离排序，屏幕中间的反而更近）
//      ⇒ 每个件都显式 `SetRenderQueue`。
//   ③ 原版 `Image.m_PreserveAspect = 1` 的那些件要**等比放进框、居中**（UGUI 用 `pivot` 定位，
//      这几处的 pivot 实测都是 (.5,.5)）；`Image` **没有 sprite 也会渲染**（回落 `Graphic.OnPopulateMesh`
//      画一块纯色矩形）—— 所以「没 sprite + 只有 `m_Color`」的件要照画。
using UnityEngine;

namespace CardPresentation
{
    public static class MenuDraw
    {
        /// <summary>原版像素矩形中心 → **相对 `parent` 的局部坐标**（见文件头坑①）。
        /// 🔴 **2026-10-11（A294）**：父的世界位置要先换算进**设计空间**（`PosInDesignSpace`）——
        /// 原来写的是 `RectCenter − parent.position`，**少除了一次父链缩放**：小屏缩放开关一开
        /// （`TransformScalerBySmallScreenUI` 把窗根乘 M），整扇窗的**文字与图**都会按 `(1−M)·nl` 偏
        /// （`nl` = 父件到窗根的距离）。判据 / 算式 / 除的是哪一级 → <see cref="PosInDesignSpace"/>。
        /// 📌 开关出厂是**关**的 ⇒ 今天 `k == 1` ⇒ 本行与旧写法**逐位相同**（自检两态在 `Editor/ShopScene.cs`）。</summary>
        public static Vector3 Local(Transform parent, float x1, float y1, float x2, float y2)
            => LayoutSpace.RectCenter(x1, y1, x2, y2) - PosInDesignSpace(parent);

        /// <summary>🔴 **一个节点的世界位置 → 设计空间**（2026-10-11 · A294）—— 全壳**唯一一份**
        /// （`Local` 与两条 `Clip*Children` 都走它；口径照 A228 那条裁定 `资料/调度台_口径裁定_1011.md` §A228）。
        ///
        /// <para>**为什么必须有它**：本工程换算出来的一律是**设计空间**的量（`LayoutSpace.FromPixel` /
        /// `RectCenter` / `Px`），而 `Transform.position` 是**已缩放的视觉世界坐标** ——
        /// 两者只在「父链 `lossyScale == 1`」时才同量纲。窗根被 `TransformScalerBySmallScreenUI`
        /// 乘了 M（开关开 **且** `extraScaleSmallScreen ≠ 1`）之后，子件的世界位置 = `M × 设计位置`
        /// ⇒ 不除这一次，把 `RectCenter − parent.position` 写进 `localPosition` 会多出 `(1−M)·nl`
        /// （`nl` = 父件到窗根的距离；父件越靠窗根这一项越小 ⇒ 窗根附近看不出来，离得越远偏得越多）。</para>
        ///
        /// <para>🔴 **除的是哪一级的 `lossyScale`：节点的【父节点】那一级**（= 缩放它的那一级），
        /// ⛔ **不是节点自己的** —— 自己的 `localScale` 只影响**它的孩子**、不影响它自己的位置。
        /// 这一条在本工程里**带电**：`Shell/CampaignTab.cs` 里 `Premium Mark` 那颗 `localScale` 那一句 把 `localScale = (2,2,1)` 打在一个节点上
        /// （⚠️ **2026-10-11 就地订正（铁律 5，A298 顺手核出来的）**：这里原来写 `CampaignTab.cs:342`
        /// —— **行号是旧的**（`342` 今天是 `BuildNode` 的声明行附近，`localScale` 那一句在 `:358`）；
        /// 同一句旧行号还抄在 `资料/普查产出_1010/A297_MenuWindowBase副本.md:74` 与
        /// `共用件_A294_A292.md:107` 两处（**资料不在本批白名单 ⇒ 没动，见报告 §六**）。）
        /// （`Premium Mark`，原版 `Image` 的 `localScale=2`）、紧接着拿它当 `parent` 画子件
        /// （`_win.Rect(pm, …, r, …)`）—— 除自己那一份会把那颗标记整个搬到 `2 × 设计点` 上去。
        /// ⚠️ **与 A228 那份的关系（有意分家，别「统一」）**：`Battle/Label.cs` 的
        /// `ParentXInDesignSpace()` 除的是**父自己的** `lossyScale` —— 两者在「父件自己不带 `localScale`」
        /// （= 本壳的常态、也是 A228 那两处夹具）时**同值**；只有上面那种「父自己带缩放」的档才分家，
        /// 那一档按本函数这一份（`Align*On` 只挪标签自己的 x，不会有这个交叉）。</para>
        ///
        /// <para>⚠️ **只在「窗根在世界原点」时精确**（同 A228）：根一旦有偏移 `Rx`，正确的设计位置是
        /// `(position − Rx)/k`，而这里拿不到 `Rx`。生产侧满足：`WindowsManager.EnsureHost` 那三个 Holder
        /// 与 `AttachToAnchor`（把窗根归到 `localPosition = 0`）全在 `ShellRuntime` 的根下，
        /// 而那个根是**无父**的 `Shell` ⇒ 原点。**这一条要是哪天红了，就该改成
        /// `parent.InverseTransformPoint` 那条路（A228 的选项 (c)）**。</para>
        ///
        /// <para>⚠️ **退化档**（缩放非有限 / ≈0）：那一档下这个节点本来也画不出来 ⇒ **照旧不除**
        /// （与 `Label.ParentXInDesignSpace` 同一条处置），不加别的兜底、也不出声。</para>
        ///
        /// <para>🔴 **2026-10-11（A297）：本函数已【公开】**，因为 `Shell/MenuWindowBase.cs` 里那**第二份同形副本**
        /// （`MainMenuSubmenuWindow.Local` 的两个重载）已经**转调**它 —— 那一份服务的正是那**四个子菜单窗**
        /// （`RewardsWindow` / `ShopWindow` / `SocialWindow` / `CollectionWindow`）的 `Node` / `Text` / `TextBox`。
        /// 不转调就是**两套口径并存**（一边设计空间、一边世界空间），比两边都错更难查。
        /// ⛔ **别在任何地方再写一份 `X − parent.position`** —— 那正是 A294 / A297 两轮修掉的东西。</para></summary>
        public static Vector3 PosInDesignSpace(Transform t)
        {
            if (t == null) return Vector3.zero;          // 无父 ⇒ 设计坐标就是世界坐标（= 旧行为）
            var above = t.parent;
            if (above == null) return t.position;        // 它就是根：链上没有任何缩放可除
            var k = above.lossyScale;
            var p = t.position;
            return new Vector3(DivByScale(p.x, k.x), DivByScale(p.y, k.y), DivByScale(p.z, k.z));
        }

        /// <summary>`PosInDesignSpace` 的逐分量除法 + **退化守卫**（非有限 / ≈0 ⇒ 原样返回，逐字照
        /// `Label.ParentXInDesignSpace` 那一条）。</summary>
        static float DivByScale(float v, float k)
            => (float.IsNaN(k) || float.IsInfinity(k) || Mathf.Abs(k) < 1e-6f) ? v : v / k;

        /// <summary>清空一个节点的全部子件。🔴 **批处理下必须 `DestroyImmediate`** —— 没有帧循环，
        /// `Destroy` 不会立刻消失，会和新净的叠在一起。
        /// ⚠️ 2026-09-27：这段原来只在 `MainMenuSubmenuWindow.DestroyChildren` 一处，
        /// 聊天窗（不是 `MainMenuSubmenuWindow` 的子类）也要用 ⇒ **收口到这里**，那边**转调**。</summary>
        public static void ClearChildren(Transform root)
        {
            if (root == null) return;
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var go = root.GetChild(i).gameObject;
#if UNITY_EDITOR
                if (!Application.isPlaying) { Object.DestroyImmediate(go); continue; }
#endif
                Object.Destroy(go);
            }
        }

        /// <summary>空节点（**有矩形语义** —— 原版每个节点都有自己的 rect，位置要摆对，
        /// 否则自检量不到、将来做点击/滚动也会算错）。
        /// <para>🔴 **2026-10-07（A92）：这个节点是 `RectTransform`，不是裸 `Transform`。**
        /// 原来建的是 `new GameObject(name)` —— 那样连 `rect` 都没有，原版那些容器节点带矩形语义的地方
        /// 我们这一层**表达不了**（参与不了布局、`rect` 量不出宽高）。
        /// **判据 = 原版节点类型**：`bundle_menus_assets_all` 16768 个 `GameObject` 里
        /// **16510 是 `RectTransform`、只有 258 个是裸 `Transform`**，而那 258 个**清一色是卡框的 3D 子锚
        /// 与粒子件**（`Card Info` 41 · `Tactic Container` 41 · `EffectAnchor` 41 · `Textbackgrounds` 41 ·
        /// `MinionOrWarlord Container` 40 · `Glow*` / `Trails` / `Sparks` / `Wave*` / `Particle System nebula` …），
        /// **没有一个菜单容器**。⇒ 建容器一律用本方法（= `RectTransform`）。
        /// ⛔ **别写成 `AddComponent&lt;RectTransform&gt;()`** —— 那是「先建裸 `Transform` 再加一个」，
        /// 多一步且语义不同（本仓统一用 `new GameObject(name, typeof(RectTransform))`）。</para>
        /// <para>⚠️ **原版是裸 `Transform` 的那种件别用本方法** —— 见 `MenuWindowBase.NewPlainTransform`
        /// （唯一实例：`Particle System nebula`；配套断言 `Editor/RewardsScene.cs` 那条「**没有** `RectTransform`」）。</para>
        /// <para>🔴 **2026-10-11（A218）**：位置与尺寸**一起**由 `ApplyPxRect` 写（此前只写了位置 ⇒
        /// `rect` 的宽高还是默认值，「空节点 + `PxRect`」的宽高验收不了）。</para></summary>
        public static Transform Node(Transform parent, string name, PxRect r)
        {
            var t = new GameObject(name, typeof(RectTransform)).transform;
            t.SetParent(parent, false);
            ApplyPxRect(t, parent, r);          // 位置（矩形中心）+ 尺寸（`sizeDelta`）—— 同一份换算
            return t;
        }

        /// <summary>🔴 **`PxRect` 的尺寸那一半 → 节点 `RectTransform.sizeDelta`**（2026-10-11 · A218）。
        /// 位置那一半见 <see cref="ApplyPxRect"/> / <see cref="Local"/>。**换算只有 `LayoutSpace.Px` 这一份**
        /// （⛔ 别在别处再乘/除一次 108 —— 同 `LayoutSpace.Px` 的注释）。
        ///
        /// <para>**为什么尺寸要写进 `sizeDelta`**：原版每个容器节点都有自己的 `rect`，而我们这套**没有 uGUI
        /// 运行时**（没有 `Canvas` / `EventSystem` / `LayoutGroup`，见 A92 的普查）⇒ 矩形只能由节点自己带着。
        /// A92 只补了类型（`RectTransform`），`sizeDelta` 仍是默认值 ⇒「空节点 + `PxRect`」的**宽高验收不了**
        /// （判据原文 = `资料/待办判据_1007.md` §A218 的 ①）。</para>
        ///
        /// <para>🔴 **锚点与 pivot 一律写死成 `(0.5,0.5)` 重合 + 居中，理由是「让 `rect` 只由 `sizeDelta` 决定」**：
        /// uGUI 的算式 `rect.width = |anchorMax.x − anchorMin.x| × 父宽 + sizeDelta.x`
        /// （本仓那份推导在 `Core/UguiRect.Child`）⇒ 锚点一重合，**第二个乘积项就没了**、`rect` 与父节点的矩形无关；
        /// pivot 居中则让 `localPosition` 正好落在**矩形中心**（与 `Local()` 给的是同一个量纲）。
        /// ⚠️ **这是一处【有意偏离】、不是复刻**：原版这些件的锚点**每个都不一样**（窗口根多半 `(0,0)-(1,1)`
        /// stretch、HUD 件多半是重合点、`WaitText` 是 `(0.5,1)`…），而本工程**没有 uGUI 父矩形**可依赖
        /// ⇒ 我们只复刻**矩形这四个数**（位置走 `Local`、尺寸走这里），**锚点不复刻**（原版值逐条记在
        /// 各调用点的注释里，将来真上 uGUI 才用得上）。判据出处：`bundle_menus_assets_all` /
        /// `bundle_scenes_scenes_battlearena1` 的 `RectTransform` 字段实读（2026-10-11 逐个核过，见报告）。</para>
        ///
        /// <para>⚠️ **本函数保证「不动位置」**：先存一份 `localPosition`、写完锚点/尺寸再放回去 ——
        /// uGUI 里 `localPosition` ↔ `anchoredPosition` 是**互相推导**的，改锚点/pivot 有可能让引擎按
        /// `anchoredPosition` 反算出另一个 `localPosition`（父矩形非 0 时会差 `(refNorm − pivot) × 父宽`）
        /// ⇒ 不留这一手，「只写尺寸」这句就**只是碰巧成立**。
        /// 改坏法：删掉存/放那两行 ⇒ `Editor/RewardsScene.cs` §A218 的「写 `sizeDelta` 不挪位置」那条红。</para>
        ///
        /// <para>⚠️ `t` 是裸 `Transform` 时（= `MenuWindowBase.NewPlainTransform` 那一档，原版就没有
        /// `RectTransform`）**静默返回、不建组件** —— ⛔ 别顺手 `AddComponent&lt;RectTransform&gt;()`，
        /// 那会毁掉那处的保真（判据见 `MenuWindowBase.NewPlainTransform` 的注释）。</para></summary>
        public static void SetPxSize(Transform t, float wPx, float hPx)
        {
            if (t == null) return;
            var rt = t as RectTransform;
            if (rt == null) return;                  // 裸 `Transform`（原版如此的那一件）⇒ 没有 rect 可写
            var lp = t.localPosition;                // ⚠️ 见上面那条：写完锚点要把位置放回去
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(LayoutSpace.Px(wPx), LayoutSpace.Px(hPx));
            t.localPosition = lp;
        }

        /// <summary>把一个 `PxRect` 写进节点：**位置（矩形中心）+ 尺寸（`sizeDelta`）**。
        /// 🔴 **两个量共用同一份换算**（`Local` + `LayoutSpace.Px`）—— ⛔ 别在别处再抄一套
        /// （CLAUDE.md §三：两处写同一条规则 = 迟早不一致）。
        /// ⚠️ 顺序有意如此：**先尺寸、后位置** —— 理由见 `SetPxSize` 那条「写完锚点要把位置放回去」。
        ///
        /// <para>🔴 **2026-10-16（A811 根治）：本方法是【唯一一处】「把矩形写进节点」的口** ——
        /// 尾句顺手把这份矩形**记到节点身上的 <see cref="ViewportClip"/>**（有的话）。
        /// 为什么必须在这里、而不是各建站点各写一次：**框的中心必须与「传进 `MenuDraw.*` 的那个 `PxRect`」
        /// 同帧**，而这一句正是「这个节点的矩形」被确定下来的那一刻
        /// （口径 / 病灶 → `Shell/ViewportClip.cs` 文件头那一节）。⛔ 少了这一句 ⇒
        /// 任何「把视口节点换个矩形」的地方都只能改到**几何**、框会**静默**停在旧矩形上。
        /// ⚠️ 代价 = 每建一个节点多一次 `GetComponent`（相对建几何是噪声级）；
        /// ⚠️ **只写尺寸**的那种调用（`SetPxSize` 直调）**不**更新它 —— 那是**有意**的：框的**尺寸**本来就取
        /// 节点自己的 `rect`（见 `ViewportClip.ClipPx`），只有**中心**需要这份记录。</para></summary>
        public static void ApplyPxRect(Transform t, Transform parent, PxRect r)
        {
            SetPxSize(t, r.W, r.H);
            t.localPosition = Local(parent, r.x1, r.y1, r.x2, r.y2);
            // 🆕 A811 根治：把「写进这个节点的矩形」记给视口节点（没有 `ViewportClip` 的节点 = 一次查表，无事发生）
            if (t != null)
            {
                var vc = t.GetComponent<ViewportClip>();
                if (vc != null) vc.SetBaseRect(r);
            }
        }

        // ============================================================ 裁切（等效 `RectMask2D`）

        /// <summary>🔴 **“越界怎么算”只此一份** —— 把 `r` 与裁切边界 `clip` 求交。
        /// 返回 **false = 整块在框外 ⇒ 调用方一律「不建」**（画不出、也点不到，与原版被裁掉的部分一致）。
        /// `outRect` = 实际要画 / 要吃点击的那块；**无裁切、或整块在框内 ⇒ 原样返回 `r`**
        /// （`SameRect(outRect, r)` 就能判出「没被截」）。滚动区把 `Viewport` 给进来，等价原版
        /// `Viewport` 上那个 `RectMask2D`。
        ///
        /// 🔴 **判据 = 原版 `RectMask2D`**（它有**两副面孔**，两副都要等效）：
        ///   · **渲染**：走 `IClipper` 管线，越界的像素画不出来 ⇒ 我们截矩形 + 截 uv（只截矩形不截 uv 会把图压扁）；
        ///   · **射线**：它还实现 `ICanvasRaycastFilter`，`IsRaycastLocationValid`
        ///     （UGUI 源码 `Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/RectMask2D.cs:178-185`）
        ///     就是一句 `RectTransformUtility.RectangleContainsScreenPoint(...)` ⇒ **框外的点判不中任何东西**
        ///     ⇒ 我们这边的**命中区也要截到框内**（`Hit` / `DeckCell` 用它）。
        /// ⇒ `Rect` / `Nine` / `Hit` / `DeckCell` **全部转调这一份**（CLAUDE.md §三：两处写同一条规则 = 迟早不一致）。
        /// ⚠️ 退化矩形（宽或高 ≤ 0.01）在**有裁切**时一律判不可见 —— 这是 `Rect` 原来就有的行为，别丢。
        /// 🔴 **2026-10-14（A434）**：`clip == <see cref="NoClip"/>`（**哨兵**）= 「**显式不裁**」⇒ 直接返回
        /// `true`（原样吐出 `r`），⛔ **别**让无限大的矩形走进求交（那会得到 NaN）。</summary>
        public static readonly PxRect NoClip = new PxRect(float.NegativeInfinity, float.NegativeInfinity,
                                                          float.PositiveInfinity, float.PositiveInfinity);

        /// <summary>🔴 **2026-10-14（A434）**：`clip` 实参是不是「**显式不裁**」那个哨兵。
        /// 判据 = 左上角是负无穷（`NoClip` 的定义；⛔ 别用 `==` 比整个矩形 —— 里面有 `Infinity`，
        /// `PxRect` 是 struct，逐字段比会遇上 `−∞ != −∞` 那类坑）。</summary>
        public static bool IsNoClip(PxRect? clip)
        {
            return clip.HasValue && float.IsNegativeInfinity(clip.Value.x1);
        }

        public static bool ClipRect(PxRect r, PxRect? clip, out PxRect outRect)
        {
            outRect = r;
            if (IsNoClip(clip)) return true;
            if (!clip.HasValue) return true;
            if (r.W <= 0.01f || r.H <= 0.01f) return false;
            var c = clip.Value;
            // 整块在框内 ⇒ **原样返回 `r`**（不新建矩形 ⇒ 调用方能靠逐字段比判出「没截」）
            if (r.x1 >= c.x1 && r.x2 <= c.x2 && r.y1 >= c.y1 && r.y2 <= c.y2) return true;
            // 🔴 **2026-10-07（A12①）：「有没有交集」那一问转调 `Visible`**（全壳唯一一份求交）。
            //    这一句是**纯短路、逐字不改行为** —— `!Visible` 的四种情形代进下面那对 `Max/Min` 必然
            //    也得到 `x2 <= x1 + 0.01f || y2 <= y1 + 0.01f`（例：`r.x2 <= c.x1` ⇒ `x2 = r.x2`、
            //    `x1 = c.x1 ≥ r.x2`）⇒ 原来也会跟着那一行 `return false`。
            //    ⚠️ **下面那一行不许删**：它还管**薄交集**（重叠 < 0.01px）与退化 `clip`，那不是 `Visible` 的职责。
            if (!Visible(r, clip)) return false;
            float x1 = Mathf.Max(r.x1, c.x1), x2 = Mathf.Min(r.x2, c.x2);
            float y1 = Mathf.Max(r.y1, c.y1), y2 = Mathf.Min(r.y2, c.y2);
            if (x2 <= x1 + 0.01f || y2 <= y1 + 0.01f) return false;
            outRect = new PxRect(x1, y1, x2, y2);
            return true;
        }

        /// <summary>🔴 **`ClipRect` 的【节点态】版本**（A198② 阶段 2 的接口；实现 = 同一份 `ClipRect`，
        /// 本重载只多做一件事：**先把裁切状态解析出来**）。
        ///
        /// <para>**为什么需要它**：`ClipRect` / `Visible` 是**纯矩形函数**、手上没有 `Transform`
        /// ⇒ 它们**解析不了节点**（判据 → `资料/普查产出_1012/H10_ViewportClip阶段1.md` §五·3）。
        /// 于是「裁切状态长在视口节点上」这件事对它们**天然到不了** —— 调用方若直接
        /// `ClipRect(r, (PxRect?)null, out v)` 或 `Visible(r, (PxRect?)null)`，那一处就**绕过了父链上
        /// 任何一个 `ViewportClip`**（静默不裁；而且它长得跟「这一处不裁」一模一样）。
        /// 本重载把「先解析、再求交」压成一句，`parent` = **要被裁的那个件挂在哪**（解析沿它往上走）。</para>
        ///
        /// <para>⚠️ **`clip` 形参照旧要传**（哪怕调这个重载）：它**非空 = 显式覆盖**（旧路那一份状态赢、
        /// 连父链都不走，判据 → `ViewportClip.Resolve` 三段优先级）—— ⛔ **别把本重载当成
        /// 「空剪切版」**（喂进来的东西一样会被尊重）。⇒ 本重载与旧路的差别**只在 `clip == null` 那一格**：
        /// 旧路 = 不裁，本重载 = 父链上最近的 `ViewportClip` 说了算（没有节点时两者**逐位相同**）。</para>
        ///
        /// <para>⚠️ **今天全仓没有任何节点**（阶段 1 不挂）⇒ 本重载与 `ClipRect(r, clip, out outRect)`
        /// 的返回值**逐位相同**（`Resolve` 第 1/3 支对参数是恒等式，`PaddedClip(·, zero)` 首句早退）。
        /// ⛔ 反过来：**新建一处「手上只有矩形」的调用点时，别再用裸 `ClipRect`** —— 用本重载，
        /// 否则那一处永远吃不到节点态（这正是 `MenuScroll.Intersects(onScreen)` 那 19 处构建循环的形状，
        /// 待接线 → 上面的出处报告 §五·3）。</para></summary>
        public static bool ClipRectAbove(Transform parent, PxRect r, PxRect? clip, out PxRect outRect)
        {
            return ClipRect(r, ViewportClip.Resolve(parent, clip, default(Vector2), default(Vector4)).RenderClip,
                            out outRect);
        }

        /// <summary>两个 `PxRect` 逐字段相等没有（`PxRect` 是 `struct`、没重写 `Equals`；
        /// 用来判「`ClipRect` 有没有真的截掉一块」—— 自检也可以拿它量「没人动过这个矩形」）。</summary>
        public static bool SameRect(PxRect a, PxRect b)
            => a.x1 == b.x1 && a.x2 == b.x2 && a.y1 == b.y1 && a.y2 == b.y2;

        /// <summary>`SameRect` 的**带容差**版本（`ApplySoftEdges` 的「整块不切」那条路要用）。
        /// 🔴 **为什么不能直接用 `SameRect`**：那里比的两个矩形，一个是调用方给的、一个是
        /// `QuadRectPx(q)` **从世界坐标反推**回来的（隔着一次「读口 / 写口 `FromPixel`」的浮点往返，
        /// 误差在 ~1e-4 px 量级 —— 🆕 **2026-10-18（A1004）读口换成 `PixelOfDesign` 之后它更小了**，
        /// 但容差**照旧留着**：它同时挡着「世界↔设计」那一路的浮点残差，⛔ 别因为变小了就删）
        /// ⇒ 逐字段 `==` 会**假阴**，把「本来一致」判成「不一致」。
        /// 0.05px 的容差对画面无意义，但足以把往返误差挡在外面。</summary>
        static bool SameRectNear(PxRect a, PxRect b, float eps = 0.05f)
            => Mathf.Abs(a.x1 - b.x1) <= eps && Mathf.Abs(a.x2 - b.x2) <= eps
            && Mathf.Abs(a.y1 - b.y1) <= eps && Mathf.Abs(a.y2 - b.y2) <= eps;

        /// <summary>🔴 **「整块在框外 ⇒ 不建」的公共函数**（A25④ 收口 · 2026-10-04）——
        /// `true` = 还有可见部分（**不保证整块在框内**）、`false` = 整块在框外 ⇒ 调用方**一律不建**。
        /// 谁该用它：**建不出几何/uv 的那些件**（文字、纯逻辑节点）—— 图那一路走 `ClipRect`（它还要 `outRect`）。
        ///
        /// 🔴 **和 `ClipRect` 的关系**：判据同一条（两轴都判），差别只有一处 ——
        ///   `ClipRect` 多一条「**退化矩形**（宽或高 ≤ 0.01）在有裁切时一律判不可见」的守卫，
        ///   那是 `Rect` / `Nine` / `Hit` 那条路要的（那种尺寸建不出 quad）；
        ///   本函数**不引入**那条 —— 文字/节点建得出来，收口前的四处内联也从来没有这条。
        ///   ⇒ **逐字保留原有语义**（收口不许顺手改行为）。
        ///
        /// <para>🔴 **2026-10-07（A12①）：全壳「求交」只有这一份实现。**
        ///   · `MenuScroll.Intersects(onScreen)`（滚动区那 **19 处**构建循环）= **本函数把 `Viewport` 绑进去**，
        ///     它自己**一句比较都没有**。收口前它判的是「**只判滚动轴**」= 第二套语义：整块落在
        ///     **横轴**框外的件照样建 —— 那些件本来就被 `clip` 整块丢掉（`ClipRect` / `Nine` / `Hit`
        ///     全转调同一份）⇒ **画不出也点不到 ⇒ 收成两轴是可见行为不变**；
        ///     判据 = 原版 `RectMask2D`（**四边都裁**：渲染走 `IClipper`、射线走 `IsRaycastLocationValid`）。
        ///   · `ClipRect` 里「有没有交集」那一问**转调本函数**（见那儿的注释）。
        ///   ⛔ **别再写第三份**：收口前 `MenuWindowBase.Text` / `TextBox` · `PlayerProfileWindow.Text` ·
        ///      `SocialWindow.Text` · `MenuDraw.DeckCell` 五处各内联过一遍「两轴都在框外吗」，
        ///      2026-10-04（A25④）已收进这里；2026-10-07 又收了两处（`LiveOpsEventWindow.RebuildArmyCells`
        ///      的内联纵轴判断 · `PracticeModePopup.Inside` 那个「**完整**落在视口里才建」）。
        ///   ✅ 两处**故意**不算「第二份」：`Editor/MainMenuScene.cs` 的 `RowsInViewport` / `CellsInViewport`
        ///      是**自检的独立算一遍**（拿它跟实现对齐就等于自证 —— 见 A131 的通则），别去「收」它。</para></summary>
        public static bool Visible(PxRect r, PxRect? clip)
        {
            if (!clip.HasValue) return true;
            var c = clip.Value;
            return !(r.x2 <= c.x1 || r.x1 >= c.x2 || r.y2 <= c.y1 || r.y1 >= c.y2);
        }

        /// <summary>🔴 **`Visible` 的【节点态】版本**（A198② 阶段 2 的接口）—— 与
        /// <see cref="ClipRectAbove"/> **逐条同形**：先在 `parent` 的父链上解析出裁切状态，再把
        /// **解析后的** `RenderClip` 交给上面那个 `Visible`（**「有没有交集」仍然只有一份实现**，
        /// 本重载一句比较都没写）。
        ///
        /// <para>**它解决的那个缺口**：`Visible` 原来只有「矩形 + 框」两个形参 ⇒ 调用方手上没有
        /// `Transform` 时**只能就地内联一遍求交**（H10 §五·3 记的那一类，例：`MenuScroll.Intersects(onScreen)`
        /// 那 19 处构建循环）。有了本重载，那一类调用点**既能表达「父链上的节点说了算」、
        /// 又不必再抄一份求交**。</para>
        ///
        /// <para>⚠️ `clip` 非空 = **显式覆盖**（旧路赢，不走父链）—— 同 `ClipRectAbove`；
        /// ⚠️ 今天无节点 ⇒ 与 `Visible(r, clip)` **逐位相同**（`Resolve` 第 1/3 支对参数是恒等式）。</para></summary>
        public static bool VisibleAbove(Transform parent, PxRect r, PxRect? clip)
        {
            return Visible(r, ViewportClip.Resolve(parent, clip, default(Vector2), default(Vector4)).RenderClip);
        }

        // ============================================================ `RectMask2D.m_Padding`（**两副面孔都吃它**）
        //
        // 🆕 **2026-10-04（A9/A15 尾巴）**：`Clip` 原来只是**裸矩形**，原版那个 `m_Padding` 我们**没建模**。
        //
        // 🔴 **判据（本地 UGUI 源码，逐行读过）**：`RectMask2D.m_Padding`
        //    （`Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/RectMask2D.cs:51,60-65`
        //    —— 字段注释写明 `X = Left · Y = Bottom · Z = Right · W = Top`）**两副面孔都读它**：
        //    · **射线**：`IsRaycastLocationValid`（`RectMask2D.cs:178-185`）
        //      → `RectTransformUtility.RectangleContainsScreenPoint(rectTransform, sp, eventCamera, m_Padding)`
        //      —— `Graphic.Raycast`（`UI/Core/Graphic.cs:868-930`）**沿父链**逐个调 `ICanvasRaycastFilter`
        //      ⇒ 挂在 `Viewport` 上的这个 mask 会把**子件的**射线按它自己的矩形 + padding 过一遍。
        //    · **渲染**：`PerformClipping()`（`RectMask2D.cs:205`）→ `:226`
        //      `Culling/Clipping.FindCullAndClipWorldRect`（`UI/Core/Culling/Clipping.cs:17`）——
        //      该函数 `:25-30` 就是 `xMin = current.xMin + offset.x` / `xMax = current.xMax − offset.z` /
        //      `yMin = current.yMin + offset.y` / `yMax = current.yMax − offset.w`，
        //      紧随的 `:47` `validRect = xMax > xMin && yMax > yMin` **只有「缩」才可能被触发**
        //      ⇒ 算出来的 `clipRect`（**已经内缩过**）再经 `SetClipRect(clipRect, validRect)`（`:248`）
        //      交给每个 `MaskableGraphic` 当 `_ClipRect` ⇒ **渲染裁的是内缩后的框**。
        //    🔴 **2026-10-07 就地订正（铁律 5，A140）**：本节原文写「`m_Padding` **全文件只用在一处**
        //      （`IsRaycastLocationValid`）、**渲染那一面（`PerformClipping` / `rootCanvasRect`）压根不读它**
        //      ⇒ padding 只影响「点不点得到」，不影响「画到哪儿」」—— **两句都错**。
        //      错因 = **只 grep 了 `RectMask2D.cs`**，而真正的裁剪算式在 `Culling/Clipping.cs`（另一个文件）。
        //      ⇒ **padding 既改「点不点得到」、也改「画到哪儿」**（两处用同一个 `offset` 语义，方向一致）。
        //    ⇒ 我们这边：**两份都要过 padding，而且两份缩的都是「mask 自己那个框」** ——
        //      渲染那一份走 `PaddedClip`（`MenuWindowBase.RenderClip` 用它算，见下面），
        //      命中那一份**也**走 `PaddedClip`（`Hit` / `DeckCell` 的 `maskPad` **作用在 `clip` 上**，见那里）。
        //
        //    🔴 **2026-10-08 就地订正（铁律 5，A188）：命中那一份原来缩错了对象。**
        //      本节上一版写「命中区走 `PaddedHitRect`（缩**命中区自己的矩形**）」—— **原版不是这样**。
        //      射线那一关其实有**两道**（都要过，`Graphic.Raycast` 沿父链逐个 `ICanvasRaycastFilter` 判）：
        //        · `GraphicRaycaster.Raycast`（`…/UI/Core/GraphicRaycaster.cs:327`）=
        //          `RectangleContainsScreenPoint(graphic.rectTransform, pointerPosition, eventCamera, graphic.raycastPadding)`
        //          ⇒ **图形自己的 rect + 图形自己的 `m_RaycastPadding`**（两种 pad 不是同一个字段！）；
        //        · `RectMask2D.IsRaycastLocationValid`（`…/UI/Core/RectMask2D.cs:178-184`）=
        //          `RectangleContainsScreenPoint(rectTransform, sp, eventCamera, m_Padding)` ⇒ **mask 自己的 `rectTransform`**。
        //      ⇒ 合成判据 = `p ∈ R_图形 ∧ p ∈ (V_mask − pad)`，**不是** `p ∈ (R − pad)`。
        //      代价（实测那一幕：锻造轨道、按钮整块落在 `V − pad` 里）⇒ **原版命中宽 200.762 / 旧写法 190.762**。
        //      ⇒ **`PaddedHitRect` 现在只服务另一种 pad**（`Graphic.m_RaycastPadding`；全工程唯一使用者 =
        //        `Shell/DeckInfoPopup.cs` 的 `WarlordPad`）—— 那一处**本来就是对的**，⛔ 别把它也改成缩 `clip`。
        //      端到端断言（含改坏法）→ `Editor/RewardsScene.cs` 锻造轨道那一段 + 跨边那一条。
        //
        // 🔴 **符号约定：正值 = 缩小，负值 = 扩大**（`padding` 的 (L,B,R,T) 依次把矩形四边往里推）。
        //    ✅ **2026-10-07：已坐实（`[TODO-verify]` 摘掉）** —— 判据就是上面 `Clipping.cs:26-30` 那四行
        //       逐字：`xMin + `、`xMax − `、`yMin + `、`yMax − `。**不必再等真 Play**。
        //    📌 **更正痕迹**：这一条原来标 `[TODO-verify]`，当时的旁证是「本机 208 个非零 `m_RaycastPadding`
        //       有 207 个是负的」+ 一台读不到方法体的 il2cpp dump —— 那两条**都不必再用了**
        //       （引擎侧读不到方法体是真的，但**裁剪算式在托管侧 UGUI 里**，本地逐行可读）。
        //
        // ⚠️ **逐处不同、必须逐处实读**（全量表 `d:/4/_tmp_view/q1_rm2d.txt` ——
        //    它**只扫了 3 个菜单族包**：150（`bundle_menus_assets_all`）+ 1（`bundle_mainmenualwaysloaded_assets_all`）
        //    + 5（`bundle_generalgamewindows_assets_all`）= **156**，**那不是全库数**）。
        //    🔴 **全库 = 222 个**（2026-10-05 逐包复算：上面那 156 + `bundle_scenes_scenes_mainmenuwarpforge` 1
        //    + 13 个 `bundle_scenes_scenes_battlearena*` 各 5 = **65**；其余包 0）
        //    —— 逐包数字 / 两条复现命令 / `m_Script` 的 PathID 判据 → `MenuWindowBase.ClipSoftness` 的注释（**数字只留那一处**）。
        //    🔴 **更正痕迹（铁律 5）**：2026-10-04 那次订正写「原来那个 222 没有出处 ⇒ 改成数出来的 156」，
        //    **订过头了** —— 错因 = **把菜单族那三包当成了全库**（`bundle_scenes_scenes_battlearena*` 从未被 grep）；
        //    **222 一直是对的**（`W6审查_共用件.md` §F3 那条结论同样订过头，别再照它改回去）。
        //    🔴 **2026-10-04 就地订正（F2）**：下面这几行**原来 7 行里 5 行的值配到了错的路径上**
        //    （错因：照「哪几处看着像」填，没回全量表逐条核）。这一版是**按值分组把 61 条非零逐条过完**重写的：
        //    · `(−8,−5,−8,−5)` × **38** —— 路径末尾**全部**是 `/Text Area`（**输入框/文本框那一族**：
        //      设置窗账号、找玩家、建联盟、牌名筛选、调试输入框…）；**没有一处是 `Viewport` / `Scroll Rect`**。
        //    · `(0,0,−500,0)` × **6** —— 进度条那一族（`…/Progress Bar/Bar` · `Forge Menu Reward Button/Progress Bars`）。
        //    · `(0,9.69,0,9.69)` × **5** —— `…/Searching Oponent Popup/Window`（`RankedEventWindow` ·
        //      `RankedEventWindowV2` · `Practice Mode Menu` · `SkirmishModeEventWindow` · 顶层那份，各一）。
        //      ⚠️ **不是**收藏窗 Deck 页、也**不是** `Generic Shop Tab`（那两处是 (0,0,0,0)）。
        //    · `(0,0,0,−10)` × **3** —— `Draft Mode {Timed Mode Window, Menu Demo}/…/Cards in deck panel/`
        //      `Scroll View/Viewport`（2）+ `Player Profile Window/…/Ranking Tab/AllFactions/scroll rect/viewport`（1）。
        //    · `(10,0,0,0)` × **3** —— `…/Forge Tab/Rewards Scroll View/Viewport`（2）+ `Raid Progress Tab/…`（1）。
        //    · 其余各 1~2 条：`(0,−15,0,0)`×2（`…/Dynamic Content`，offer container 两处）·
        //      `(2.49,2.49,2.49,2.49)`×1（`Main Menu Offer Container Static Image 1x2`）·
        //      `(−26,0,−26,0)`×1（`Inbox Menu/…/Scroll View/Viewport`）·
        //      `(−25,0,0,0)`×1（`Debug Reward Window/…/Scroll View/Viewport`）· `(83,0,0,0)`×1（`Gacha Tab/Chest panel/Mask`）。
        //    🔴 **本壳真正非零的只有锻造轨道那一族** `…/Forge Tab/Rewards Scroll View/Viewport` = **(10,0,0,0)**
        //      （`Rewards Base Submenu Variant/Content Area/Tabs/Forge Tab/…` 与裸 `Forge Tab/…` 各一份）；
        //      **我们那几扇窗的滚动区 Viewport 全是 `(0,0,0,0)`** —— 商店各页（`…/Packs Scroll View/Viewport`）·
        //      收藏 Deck 页（`…/Select Deck Tab/…/Deck Scroll View/Viewport`）· 聊天（`Chat Tab/Viewport`）·
        //      档案成就（`…/Trophies Tab/Scroll/Viewport`）· 联盟成员杯（`…/AllianceMemberVariant/TrophiesWindow/Scroll Rect`）·
        //      奖励窗（`Reward Window/…/Scroll View/Viewport`）· 排行榜（`…/Scroll View/Viewport`）·
        //      卡组编辑（`Deck Editing Menu/…/Scroll View/Viewport`）
        //      ⇒ **给它们加 padding 全是多余的**（原来那句「锻造/战役两条轨道 + 收藏 Deck 页…三处」**只有锻造一处成立**）。
        //      ⚠️ 唯一一处例外是 `Player Profile Window/…/Ranking Tab/AllFactions/scroll rect/viewport` = (0,0,0,−10)。

        /// <summary>按原版 `RectMask2D.m_Padding` 把矩形缩/放一次 —— 🔴 **「符号怎么算」的全工程唯一一份**。
        /// `pad` = UGUI 的 (x=Left, y=Bottom, z=Right, w=Top)；**正值缩小、负值扩大**（判据见上面那段）。
        /// ⚠️ `PxRect` 是**左上原点、y 向下**，所以 `pad.y`（Bottom）是从 `y2` 往上收、`pad.w`（Top）是从 `y1` 往下收
        /// —— 与 UGUI 的 `xMin + offset.x / xMax − offset.z / yMin + offset.y / yMax − offset.w`
        /// （`Culling/Clipping.cs:26-30`）逐项对应（y 轴翻转后 `yMin ↔ y2`、`yMax ↔ y1`）。
        /// 🔴 **本函数不带退化守卫**：越界的 pad 会算出「宽/高 ≤ 0」的矩形 —— 那是调用方的事
        /// （命中区那一面兜「不扩」，渲染那一面照原版「整块不裁」，见各自的注释）。</summary>
        public static PxRect PaddedRect(PxRect r, Vector4 pad)
        {
            if (pad == Vector4.zero) return r;
            return new PxRect(r.x1 + pad.x, r.y1 + pad.w, r.x2 - pad.z, r.y2 - pad.y);
        }

        /// <summary>🔴 **渲染那一份裁切**：`clip` 按原版 `RectMask2D.m_Padding` 内缩（**正 = 缩小**）。
        /// 判据 = UGUI `RectMask2D.PerformClipping()`（`RectMask2D.cs:205`）→ `:226`
        /// `Clipping.FindCullAndClipWorldRect`（`Culling/Clipping.cs:17,26-30`）—— **渲染也读 padding**
        /// （本行原来写「渲染那一面压根不读它」，**2026-10-07 已就地订正**，见上面那段判据）。
        /// 谁该用它：**凡是要喂给 `Rect` / `Nine` / `Tiled` / `ClipText` / `Visible` 的那一份 `Clip`**
        /// （本壳唯一入口 = `MenuWindowBase.RenderClip`）**，以及命中区那一份** ——
        /// 🔴 **2026-10-08（A188）**：`Hit` / `DeckCell` 的 **`maskPad`（= 原版 `RectMask2D.m_Padding`）
        /// 缩的正是 `clip`**（`ClipRect(r, PaddedClip(clip, maskPad), out hr)` = 原版的 `R ∩ (V−pad)`）。
        /// 本节上一版写「⛔ **命中区不要用它** —— 命中区缩的是自己的矩形（`PaddedHitRect`）」= **旧模型**，
        /// 已按 uGUI 判据订正（两道射线关 + 两模型之差 → 上面那一整段，以及 `Hit` 的 `maskPad` 注释）。
        /// ⚠️ 退化那一支（pad 比框还大）对本函数**两条调用路都对**：**整块不裁** = 原版的
        /// `validRect = false` ⇒ `DisableRectClipping()`（见下面）。
        /// ⚠️ 剩下那个**只服务 `Graphic.m_RaycastPadding`** 的入口是 `PaddedHitRect`（见它的注释）。
        ///
        /// 退化（`pad` 比框还大）⇒ **返回 `null` = 不裁**：这就是原版那一支 —— `Culling.cs:47`
        /// 的 `validRect = xMax > xMin &amp;&amp; yMax > yMin` 不成立时 `clipRect = Rect.zero`（`RectMask2D.cs:240-241`），
        /// 而 `SetClipRect(rect, false)` 落到 `CanvasRenderer.DisableRectClipping()`
        /// ⇒ **原版此时整块不裁**（不是「裁到没有」）。本壳照做，但**出声**（不许静默：这一支几乎只可能是
        /// pad 配错了，画面表现是「视口外的内容全露出来」）。</summary>
        public static PxRect? PaddedClip(PxRect? clip, Vector4 pad)
        {
            // 🔴 **2026-10-14（A434）**：「显式不裁」哨兵 ⇒ **没有裁切**（无限大的框再内缩也是无限大，
            //   留着走进下面的 NaN/Inf 运算只会得到脏值）⇒ 这里直接把它折成 `null`（= 不裁）。
            if (IsNoClip(clip)) return null;
            if (!clip.HasValue || pad == Vector4.zero) return clip;
            var o = PaddedRect(clip.Value, pad);
            if (o.W <= 0.01f || o.H <= 0.01f)
            {
                if (_paddedClipDegenerates++ < 3)
                    Debug.LogWarning($"[MenuDraw] `m_Padding`({pad.x},{pad.y},{pad.z},{pad.w}) 相对裁切框 "
                                   + $"{clip.Value.W:F1}×{clip.Value.H:F1} **太大了**（算出来 {o.W:F1}×{o.H:F1}）—— "
                                   + "按**原版那一支**处理（`validRect = false` ⇒ `DisableRectClipping()` ⇒ **整块不裁**）。"
                                   + "⚠️ 这一支几乎只可能是那个 pad 值配错了（原版 prefab 里没有这种组合）。");
                return null;
            }
            return o;
        }

        /// <summary>`PaddedClip` 落进「退化 ⇒ 整块不裁」那一支的次数（只用来**限流那条警告**；
        /// ⚠️ 刻意**不做成公开计数** —— 本壳没有一处 pad 够得着这一支（唯一非零 = 锻造轨道 `(10,0,0,0)`，
        /// 视口宽 1588.7），做了也没处断。真出现了，那三条 `Debug.LogWarning` 就是证据）。</summary>
        static int _paddedClipDegenerates;

        /// <summary>按**图形自己的** `Graphic.m_RaycastPadding` 把一个矩形缩/放一次
        /// （🔴 **只服务这一个字段** —— 全工程唯一使用者 = `Shell/DeckInfoPopup.cs` 的 `WarlordPad`，
        /// 判据 = `GraphicRaycaster.cs:327` 那句「图形自己的 rect + 图形自己的 `raycastPadding`」）。
        /// 🔴 **2026-10-08（A188）措辞订正**：本节原文写「按原版 `RectMask2D.m_Padding` 把**命中区**的矩形缩/放一次
        /// （**只给命中区用**）」—— **两个说法都错**：`RectMask2D.m_Padding` 那一支**不缩这里**，
        /// 它缩的是 `clip`（走 `PaddedClip`，见 `Hit` 的 `maskPad` 注释）。
        /// `pad` = UGUI 的 (x=Left, y=Bottom, z=Right, w=Top)；**正值缩小、负值扩大** ——
        /// 算式与渲染那一份**共用 `PaddedRect`**（两处写同一条规则 = 迟早不一致），本函数只多一条退化守卫。</summary>
        public static PxRect PaddedHitRect(PxRect r, Vector4 pad)
        {
            if (pad == Vector4.zero) return r;
            var o = PaddedRect(r, pad);
            // 🆕 **2026-10-04（F9）：退化守卫** —— `pad` 比矩形还大时会算出「宽或高 ≤ 0」的矩形，
            //    它一路走到 `MakeHitQuad` → `SetAspect(负/0)` 造出**镜像 quad**，
            //    而 `PointerLayer.CollectHits` 的 `Abs(dx) > hw`（`hw < 0` ⇒ **恒真**）判不中
            //    ⇒ **这颗钮静默点不动**（`MakeHitQuad` 只在 `hq == null` 时出声，这条路上它不响）。
            //    ⚠️ `ClipRect` 的退化守卫兜不住这里：它在 `clip == null` 时**第一句就 `return true`**，
            //    而本函数的调用点（`DeckInfoPopup` 那颗督军立绘）本来就没有 `clip`
            //    （`MenuDraw.Hit` 那条路已改走 `PaddedClip`，见 `maskPad` 的注释）。
            //    ⇒ 按「**不扩**」处理（**退回原矩形** + 出声）—— 宁可让这一颗保持原样，也不造一颗点不动的钮。
            //    ⚠️ 渲染那一面（`PaddedClip`）的退化处置**与这里不同**（那边照原版「整块不裁」）——
            //    两边的「正常算」共用 `PaddedRect`，**只有越界时怎么兜不一样**，各自的注释都写了理由。
            // 🔴 **这条守卫与正负号约定无关**（⚠️ **2026-10-04 措辞订正（R5）**：代码是 `||` —— **任一轴**退化就兜，
            //    原文写的「两轴都退化才兜」与实现相反）。
            if (o.W <= 0.01f || o.H <= 0.01f)
            {
                PaddedHitDegenerates++;
                if (PaddedHitDegenerates <= 3)
                    Debug.LogWarning($"[MenuDraw] 图形自己的 `m_RaycastPadding`({pad.x},{pad.y},{pad.z},{pad.w}) 相对那个矩形 "
                                   + $"{r.W:F1}×{r.H:F1} **太大了**（算出来 {o.W:F1}×{o.H:F1}）—— 按「**不扩**」处理"
                                   + "（退回原矩形）。不兜的话会建出一颗**镜像 quad** ⇒ 这颗钮**静默点不动**。"
                                   + "⚠️ 走这条路的只有 `Graphic.m_RaycastPadding`（`Shell/DeckInfoPopup.cs` 的 `WarlordPad`）——"
                                   + "`RectMask2D.m_Padding` 那一支由 `PaddedClip` 兜（原版「整块不裁」）。");
                return r;
            }
            return o;
        }

        /// <summary>`PaddedHitRect` 撞上退化矩形的次数（非 0 = 有处 pad 比它要缩的那个矩形还大，
        /// 已按「不扩」兜住 —— 但那个 pad 值多半本身就配错了，见上面那段逐处实读表）。
        /// 🔴 **2026-10-08（A188）本计数器的作用域变了**：它现在**只由 `Graphic.m_RaycastPadding` 那一条路计数**
        /// （唯一站点 = `Shell/DeckInfoPopup.cs` 的 `WarlordPad`）；**`RectMask2D.m_Padding` 那一支不再经过这里**
        /// —— 它改走 `PaddedClip`（退化 ⇒ **整块不裁** + 那条限流 warning，原版那一支）。
        /// ⇒ 下面那条断言仍然恒真、仍然值得留着当**将来的看门狗**：文字与判据都不必改。
        /// 🔴 **2026-10-04 R-F 审查订正**：原文写「**自检断它 == 0**」—— **当时是假的**：全工程**一个读者都没有**
        /// （`Editor/*Scene.cs` 里 0 处），把它整段删掉 11 条自检一条都不会红。
        /// ✅ **2026-10-05（A48 接线批）照「补一条断言」那一支做了**：本字段现在的**唯一读者** =
        /// `Editor/RewardsScene.cs` 的 `Run` 里那条 `Check(MenuDraw.PaddedHitDegenerates, 0, …)`。
        /// ⚠️ 计数是**全过程全局累计**的，而那条 `Check` 只在剧本的一个时间点上读它（锻造那一段）⇒
        /// **将来新接 pad 的站点若排在那之后才建，就要把它挪到剧本末尾**（或另加一条），别让它漏检。
        /// 🔴 **2026-10-07（A77⑬⑨）如实记一笔：那一条断言【今天红不了】= 它是「恒真」的。**
        /// 判据：全工程**只有一处**给非零 `ClipPad`（`Shell/ForgeTab.cs` 的锻造轨道 `(10,0,0,0)`），
        /// 而那块命中区是 **191×47**（0 高/0 宽都够不着）⇒ 这一支**走不到**。
        /// ⇒ 它的价值**不是**「当场能红」，而是「**将来谁接上一个比命中区还大的 pad 时，那一条会红**」；
        /// ⛔ **别把它读成「已验证过兜底逻辑」**（A48 那份独立审查的原话：`PaddedHitDegenerates == 0`
        /// 「目前恒真、红不了」；② 那条真能红的是它旁边同一节里量宽度的两条 `CheckNear`）。</summary>
        public static int PaddedHitDegenerates;

        // ============================================================ 软边遮罩（原版 `RectMask2D.m_Softness`）
        //
        // 🔴 **原版是什么**：`RectMask2D` 除了硬裁，还有 `m_Softness`（`Vector2Int`，**逐处不同**：
        //    商店 `Packs Scroll View/Viewport` = **(0,25)** · `Forge Army Selector/Viewport` = **(42,0)** ·
        //    练习窗 `Army Selector` = (0,50) · `Reward Window`/`Campaign Reward Window` = (200,0) ·
        //    `Chat Tab/Viewport` = (0,22) · 其余多数是 (0,0)）⇒ 掩码边上有 `softness` 像素宽的**渐隐带**
        //    （`x` 管左右、`y` 管上下；0 = 硬边）。
        //    判据 = uGUI `RectMask2D.m_Softness`（`Runtime/UGUI/UI/Core/RectMask2D.cs:71,284-301`：
        //    `UpdateClipSoftness()` 把 `m_Softness` 交给每个 clippable）→ 由 `UI/Default` shader 的
        //    `_ClipRect` + `_UIMaskSoftnessX/Y` 落成 `saturate(该像素到最近边的距离 ÷ softness)`。
        //    ⚠️ 我们这套**没有 canvas / 没有那个 shader**（全是世界空间 `ImageQuad` + `Sprites/Default`）——
        //    下面是**几何等效**：把 quad 沿渐隐带的**内沿**切开，带宽那一块用**逐顶点 alpha**（`SetCornerColors`，
        //    `Sprites/Default` 会把 `顶点色 × 材质色 × 贴图` 乘起来）做**线性**斜坡。
        //    ⚠️ 与原版的差别只有一处，且**如实记**：原版是逐像素的（含透视插值修正），我们是**线性斜坡 +
        //    分段线性顶点色** —— 对**轴对齐的矩形内容**两者**逐像素等价**（带内 alpha 都是线性的），
        //    只对斜放/带旋转的内容有细微差别（本壳全线轴对齐）。
        //
        // 🔴 **为什么必须切几何，不能只给整块调顶点色**：content 常常**远大于**带宽（商店一格 475px、带 25px）
        //    —— 只给 4 个角做斜坡，整格都会渐变（原版只有靠边的 25px 渐变）。所以按「带的内沿」切开，
        //    带宽那一块单独做斜坡，其余保持 alpha=1。
        //
        // ✅ **代价（2026-10-04 · A38③ 已修，⛔ 别照旧说法写「这是真缺口」）**：切出来的**额外块挂成原
        //    quad 的子物体** —— 原 quad 之后若被 `SetTint` / 改几何 / `SetActive` / `Destroy`：
        //      · `SetActive` ✅ 子物体跟着（正合语义）；`Destroy` ✅ 跟着；
        //      · `SetTint` ✅ **现在跟随** —— `ImageQuad.SetTint` 会照着 `SoftEdgeRegister` 登记过的
        //        子块刷一遍（`Battle/ImageQuad.cs:163-171` + `MenuDraw.cs:268` 的登记；
        //        不刷的话症状是「边带那一条颜色不对」，**静默**）；
        //      · 改几何（`SetAspect` / `SetWorldHeight`）✅ **现在会重切** —— 宿主被挂上 `SoftEdgeRebuild`
        //        回调（`ArmSoftRebuild` → `ReapplySoftEdges`），切出来的块按新框重摆一遍。
        //        ⚠️ **只有 `Rect` 那条路上带电**：`Nine` / `Tiled` 也给每个子块挂了回调，但全工程没有
        //        「建完之后再改这些子块几何」的调用点（改它们的是 `ClipNineChildren` / `ClipTiledChildren`，
        //        都跑在 `ApplySoftEdges` **之前**）⇒ 那两条路上挂的回调**不会触发**。
        //    ⚠️ 仍然成立的一条：**图（`Texture`）是建的时候就定死的**（换图请整段重建）。
        //    ⇒ 画软边区里的东西时，tint/图 **仍建议走 `Rect`/`Nine` 的入参**（少一次重切、也少一层子块）。

        /// <summary>软边的 alpha 剖面（单轴）：**离最近的那条框边有多少距离** ÷ 带宽，夹到 [0,1]。
        /// 0 = 正好压在框边上（原版这里 alpha 也是 0）、1 = 已经进到带宽以内。
        /// 🔴 判据 = uGUI `UI/Default` 的 `half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(...)) * IN.mask.zw)`
        /// —— `_ClipRect.zw - _ClipRect.xy` 是框的尺寸、`abs(...)` 是「到框中心的 2 倍距离」
        /// ⇒ 化简后就是「到**最近那条边**的距离」，乘的 `mask.zw` ≈ `1 / softness`。**线性**。</summary>
        static float SoftAlpha(float v, float c1, float c2, float soft)
        {
            if (soft <= 0f) return 1f;
            return Mathf.Min(Mathf.Clamp01((v - c1) / soft), Mathf.Clamp01((c2 - v) / soft));
        }

        /// <summary>🔴 **A990（2026-10-18）：世界（设计空间）→ 画布 px —— 必须是 `LayoutSpace.FromPixel` 的【逆】。**
        ///
        /// <para>**为什么必须取「那一份」的逆**：本壳**建件时的 x** 走 `LayoutSpace.FromPixel`
        /// （`MenuDraw.Local` / `RectCenter` ⇒ `x = (px / 1920 − 0.5) × VisibleWidth`），而「画布 px 那一侧」
        /// 的矩形（原版 prefab 的 `m_SizeDelta` · `RectMask2D` 那个框 · 各调用点传进来的 `PxRect`）
        /// **全是同一套设计 px** ⇒ 「这个角对应哪个设计 px」只能由「**建它时用的那条换算**」反过来回答；
        /// 否则「夹到框沿」夹到的**不是画面里那条框沿**。</para>
        /// <para>⛔ **`LayoutSpace.ToPixel` 的 x 不是它的逆** —— 那一份写死 **108px/世界单位**
        /// （= `LayoutSpace.PxX`），与 `FromPixel` **只在 `VisibleWidth == DesignWidth`（16:9）时重合**；
        /// 别的宽高比下两者差 **`VisibleWidth / DesignWidth` 倍**（4:3 ⇒ **0.75** · 21:9 ⇒ **1.3125**）
        /// ⇒ x 的往返不闭合（y 那一半两个函数本来就同值：可见高恒 10 世界单位 = 1080px ⇒ 108 是**真的**）。
        /// 🔴 判据 = 原版 uGUI 只有**一条** `RectTransform` 的世界↔屏幕换算（`LayoutSpace.cs:164` 也把
        /// `ToPixel` 自己声明成「`FromPixel` 的逆」）—— 「写用实测、读用常量」两套并存就是 A990。</para>
        /// <para>⚠️ **16:9 下与 `LayoutSpace.ToPixel` 只差 float 舍入**（`DesignPxW / VisibleWidth` 实得
        /// **107.99999** 而不是 108 ⇒ 整屏范围内偏差 **≤ 2.5e-4 px**）—— 远小于本壳各处断言用的 0.05px 容差，
        /// 也远小于画面上看得见的量 ⇒ **12 条自检里那些把 `cam.aspect` 钉成 `DesignAspect` 的宿主零回归**。
        /// ⚠️ y 那一半**直接转调 `LayoutSpace.PxY`**（⛔ 别在这里再写一遍 y 的式子 —— CLAUDE.md §三）。
        /// 🔴 **本式 = `LayoutSpace.FromPixel` 的逆、逐字对偶**：那边 x 的算式一改（例如全局裁定那天把
        /// `FromPixel` 改成与 `Px`/`ToPixel` 同一条常量换算），**这一行必须跟着改** —— 两处不同步就又变回 A990。</para>
        /// <para>🔴 **同一族【还没收口】的读口（A990 只动了 `ClipQuad` 那一个，其余留给调度台裁）**：
        /// `LayoutSpace.ToPixel` / `PxX` 的 **~90 个调用点**（命中判定 · `ViewportClip.ClipPx` ·
        /// `PointerLayer` · 各 `*Scene.cs` 的标尺 …）—— 它们**全是「只在 16:9 自洽」的那一批**，
        /// 改动面跨 `Core/` 与十几个宿主，不在本件白名单里。
        /// ⚠️ **2026-10-18 就地订正（铁律 5 · A1004）**：这一段原来还把「下一段 `QuadRectPx`」列在**没收口**里
        /// —— `QuadRectPx` 的位置项已改走本函数（`A1004`，16:9 下与旧读差 ≤2.5e-4px）⇒ **它已经不在这一族了**。</para></summary>
        static Vector2 PixelOfDesign(Vector3 designPos)
        {
            float vw = LayoutSpace.VisibleWidth;
            // 退化档（没有相机 / 宽度不可用）：退回旧口 —— 与 `DivByScale` 同一条处置，**不静默改行为**
            if (vw <= 1e-6f) return LayoutSpace.ToPixel(designPos);
            return new Vector2((designPos.x / vw + 0.5f) * LayoutSpace.DesignPxW,
                               LayoutSpace.PxY(designPos.y));
        }

        /// <summary>一个 quad 在**画布 px（设计空间）**里的矩形（位置项走 <see cref="PixelOfDesign"/> ——
        /// 🆕 **2026-10-18（A1004）**：改前写的是 `LayoutSpace.ToPixel`，见下面那条订正）。
        ///
        /// <para>🆕 **2026-10-18（A1004）：位置项从 `LayoutSpace.ToPixel` 换成 <see cref="PixelOfDesign"/>。**
        /// 这一段的**写**那一侧是 `PlaceCell` → `LayoutSpace.FromPixel`（用**实测** `VisibleWidth`），
        /// 而 `ToPixel` 的 x 写死 108 ⇒ **非 16:9 下读/写不互逆**（4:3 偏 0.2778 · 21:9 偏 0.9722 世界单位；
        /// 见 `资料/普查产出_1018第三会话/W_ClipQuad与量法.md:85` 那份「一行方案」）。
        /// **16:9 下与旧读差 ≤ 2.5e-4 px**（`DesignPxW / VisibleWidth` 实得 107.99999）⇒ 12 条自检里把
        /// `cam.aspect` 钉成 `DesignAspect` 的宿主**零回归**；y 那一半 `PixelOfDesign` 直接转调 `LayoutSpace.PxY`，**逐位不变**。
        /// ⚠️ **`A990①` 那一档（尺寸项 `WorldW × K` 在非 16:9 下不跟位置缩放）【不在本件】** —— 见 `:42`。</para>
        ///
        /// <para>🔴 **2026-10-11（A298）**：**位置项先换算进【设计空间】，再喂 `ToPixel`** ——
        /// 即 `PosInDesignSpace(q.transform)`（除的是**该 quad 的父级那一级** `lossyScale`，
        /// 与 `Local` / `ClipNineChildren` 的 `rootDesign` **同一份口径**）。
        /// **改前**写的是裸 `ToPixel(q.transform.position)`：`Transform.position` 是**已缩放**的视觉世界坐标，
        /// 而 `clip`（原版 prefab 上那个 `RectMask2D` 框）与各处的 `vis` 一律是**设计 px**
        /// ⇒ 小屏缩放开关一开（窗根 ×M、`TransformScalerBySmallScreenUI`），这里拿到的是**放大过的那份 px**
        /// ⇒ `ClipRect` / `ClipNineChildren` / `ClipTiledChildren` / `ApplySoftEdges`
        /// （`SameRectNear(QuadRectPx(q), vis)`）/ `ReapplySoftEdges` 的**求交与切点全在错的量纲上**。
        /// **判据** = 原版 `RectMask2D` 是在 **canvas 空间**裁的（uGUI
        /// `Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/Culling/Clipping.cs:17,26-30`
        /// 读的是 `rectTransform` 的 canvas 坐标），最后才由窗根那一级缩放把整棵树放大
        /// ⇒ 我们这一套要等价，就得把「世界 → px」**先除回设计缩放**。</para>
        ///
        /// <para>✅ **为什么这样改就是对的（与 A294 同一份口径，可自证）**：建一个 quad 时写进去的是
        /// `localPosition = RectCenter(r) − PosInDesignSpace(parent)`（`Local`），而
        /// `position = parent.position + parent.lossyScale ⊙ localPosition`
        /// ⇒ `PosInDesignSpace(q) = position / parent.lossyScale
        /// = PosInDesignSpace(parent) + localPosition = RectCenter(r)`
        /// ⇒ **反推回来的中心 = 建它时给的那个矩形中心**（这一条正是 `SameRectNear(QuadRectPx(q), vis)`
        /// 敢用 0.05px 容差的根据）。
        /// ⚠️ **上面那个恒等式成立的条件 = `q.parent` 自己不带 `localScale`**
        /// （即 `q.parent.lossyScale == q.parent.parent.lossyScale`）—— 因为 `PosInDesignSpace(q.parent)`
        /// 除的是**它上面那一级**。若宿主父件自带缩放 `s`，两条路会分家：`PosInDesignSpace(q) = RectCenter(r)
        /// − parent.position/(s·k) + …`（见报告 `资料/普查产出_1011/W4_子3.md` §五 的逐处可达性判定：
        /// 现读全壳「自带 `localScale` 且**有子件**」的节点只有三处 —— `Shell/CampaignTab.cs` 里 `Premium Mark` 那颗 `localScale` 那一句
        /// 的 `Premium Mark`(=2) · `Shell/RewardWindow.cs` 里 punch 抽屉节点那两处 `localScale` 的 punch 抽屉节点(动画值) ·
        /// `Battle/WfSlider.cs` 里那句 `_fillRoot.transform.localScale = …` 的九宫格根(=值)；三处**今天都到不了本函数的「按几何用」那几条路**，
        /// 但**这一档的量纲语义是「留待调度台裁」的**，别当成已收口）。</para>
        ///
        /// <para>⚠️ **尺寸项（`q.WorldW/WorldH × K`）本来就在设计量纲上、一个字不动**：
        /// `ImageQuad.Create` / `SetWorldHeight` 收的是 `LayoutSpace.Px(设计高)` = **设计长度**
        /// （渲染时由父链那同一份缩放放大）⇒ 除以 `K` 就是设计 px。⛔ 别顺手给它也除一次缩放。</para>
        ///
        /// <para>⚠️ **软边子块的父级 = 宿主 quad 自己**（`ApplySoftEdges` 里 `ImageQuad.Create(q.transform, …)`）
        /// ⇒ `PosInDesignSpace(sub)` 除的是**宿主那一级** `lossyScale`。宿主自己不带 `localScale` 时
        /// （今天的全部调用点）与 `PosInDesignSpace(q)` 同值 ⇒ 子块的「设计点」= 宿主设计点 + 它的局部偏移，
        /// 与 `PlaceCell(sub, …)` 写进去的那一份**逐位一致**。宿主哪天自带 `localScale` 时这一条要重新想
        /// （现读没有这种站点）。</para>
        ///
        /// <para>📌 **`k == 1`（小屏缩放开关出厂关着）时与改前【逐位相同】**
        /// （`DivByScale(v, 1f)` 就是 `v`）⇒ 今天零可观测差异 —— ⚠️ **唯一例外**就是上面那句说的
        /// 「`q.parent` 自带 `localScale`」那三处（那时 `k==1` 但 `q.parent.lossyScale ≠ 1`）。
        /// 🔴 **改坏法：把 `PosInDesignSpace(q.transform)` 换回裸 `q.transform.position` ——
        /// ⚠️ 今天【一条现有断言都不会红】**（`k == 1` 两式逐位相同）⇒ 它是**潜伏缺陷**、不是
        /// 「有断言挡着」，只有拿「态二」（开关开 + 窗根 ×1.2 + `clip` 非空 + **九宫格/平铺的部分越界**）
        /// 才照得出来 —— 要配的两态断言（夹具 / 断言什么 / 为什么这个形状能照出它）
        /// 写在报告 `资料/普查产出_1011/W4_子3.md` §四（本批白名单里没有 `Editor/*Scene.cs` ⇒ 没动手）。</para></summary>
        public static PxRect QuadRectPx(ImageQuad q)
        {
            const float K = LayoutSpace.DesignPxH / LayoutSpace.DesignHeight;   // 108 px / 世界单位
            Vector2 cpx = PixelOfDesign(PosInDesignSpace(q.transform));         // 🆕 A298 除回设计缩放 · A1004 走设计帧读口
            float hw = q.WorldW * K * 0.5f, hh = q.WorldH * K * 0.5f;
            return new PxRect(cpx.x - hw, cpx.y - hh, cpx.x + hw, cpx.y + hh);
        }

        /// <summary>🆕 **2026-10-18（A1003，A298 那条之后提的 `public`）**：`out` 形（给各宿主的量法助手转调用，
        /// 它们**保留自己的函数名与形参**、只把函数体换成一行转调 ⇒ 调用点一个都不用改）。
        /// 返回 `false` = `q == null`（四个 out 归零 —— 与各宿主原来的契约逐字一致）。</summary>
        public static bool QuadRectPx(ImageQuad q, out float x1, out float y1, out float x2, out float y2)
        {
            x1 = y1 = x2 = y2 = 0f;
            if (q == null) return false;
            var r = QuadRectPx(q);
            x1 = r.x1; y1 = r.y1; x2 = r.x2; y2 = r.y2;
            return true;
        }

        /// <summary>🆕 **2026-10-18（A1003）**：并集时对每一块 `ImageQuad` 的**激活闸**。
        /// 🔴 **三种口径各自都有生产用例 ⇒ 收口时逐条保留、⛔ 不许统一**
        /// （原判据 → `资料/普查产出_第四会话/查证_并集判据与疑似可销四条.md` §一·4）。</summary>
        public enum QuadGate
        {
            /// <summary>**完全不过滤** —— `Editor/MainMenuScene.cs` 的 `UnionQuadRect` 传 `true` 后**没有闸**，
            /// 那在那边是**有意**的。</summary>
            None = 0,
            /// <summary>只看块自己 `activeSelf`（**父链关着也算进来**）—— `Editor/RewardsScene.cs` 的 `RectOfUnion`。</summary>
            Self = 1,
            /// <summary>块自己 + 父链全活（= `GetComponentsInChildren` 缺省那一档）——
            /// `Editor/DeckScene.cs` · `Editor/SettingsScene.cs` · `Editor/ShopScene.cs` · `Editor/ShellScene.cs`。</summary>
            InHierarchy = 2,
        }

        /// <summary>🆕 **2026-10-18（A1003）：全壳唯一一份「九宫格 active 子块并集」量法。**
        /// 一棵子树里**全部 `ImageQuad` 的渲染矩形并集**（画布 px · 左上原点 · y 向下）——
        /// 九宫格 / 平铺 / 软边切块**必须**用并集：⛔ 只取第一块会量成某个角块
        /// （实据 = `Editor/SettingsScene.cs` 那条「弹窗底量成 182×173」）。
        ///
        /// <para>`gate` = 激活闸（见 <see cref="QuadGate"/> —— ⚠️ **三种口径都是生产在用的，⛔ 别统一**）；
        /// `searchInactive` = `GetComponentsInChildren&lt;ImageQuad&gt;(searchInactive)` 那个形参
        /// （`false` = 只找激活链上的 —— `Editor/ShopScene.cs` 那两处原样就是 `false`）；
        /// `quads` = **真被算进去的块数**（软边切几块都行；`Editor/ShopScene.cs` 的「九宫格应该 9 块」用它）。
        /// 返回 `false` = 一块都没量到（四个 out 归零）。</para>
        ///
        /// <para>🔴 **两项【有意保留】的宿主差异**（本函数**不**接管，⛔ 别顺手统一）：
        /// ① `Editor/RewardsScene.cs` 的 `RectOfUnion` 里有「**一个 quad 都没有**时退回 `RectOf`（`Label` 那条路）」
        /// —— 只有它有，其余宿主回 false；
        /// ② `Editor/MainMenuScene.cs` 的 `UnionQuadRect` 那一路**完全不过滤**激活态（`QuadGate.None`）。</para></summary>
        public static bool UnionQuadRectPx(Transform t, QuadGate gate, bool searchInactive,
                                           out float x1, out float y1, out float x2, out float y2)
            => UnionQuadRectPx(t, gate, searchInactive, out x1, out y1, out x2, out y2, out _);

        /// <summary>同上，多给一个「真被算进去的块数」。见 <see cref="UnionQuadRectPx(Transform, QuadGate, bool, out float, out float, out float, out float)"/>。</summary>
        public static bool UnionQuadRectPx(Transform t, QuadGate gate, bool searchInactive,
                                           out float x1, out float y1, out float x2, out float y2, out int quads)
        {
            x1 = y1 = float.MaxValue; x2 = y2 = float.MinValue; quads = 0;
            if (t == null) { x1 = y1 = x2 = y2 = 0f; return false; }
            foreach (var q in t.GetComponentsInChildren<ImageQuad>(searchInactive))
            {
                if (q == null) continue;
                if (gate == QuadGate.Self && !q.gameObject.activeSelf) continue;
                if (gate == QuadGate.InHierarchy && !q.gameObject.activeInHierarchy) continue;
                float a1, b1, a2, b2;
                if (!QuadRectPx(q, out a1, out b1, out a2, out b2)) continue;
                if (a1 < x1) x1 = a1;
                if (b1 < y1) y1 = b1;
                if (a2 > x2) x2 = a2;
                if (b2 > y2) y2 = b2;
                quads++;
            }
            if (quads == 0) { x1 = y1 = x2 = y2 = 0f; return false; }
            return true;
        }

        /// <summary>软边带宽的**内沿**落在 `[a1,a2]` 内部的那两个断点（最多两个，已排序）。
        /// 只切「真正落在本块内部」的：整块都在带里、或整块都在带外 ⇒ **不切**（剖面在块内是线性的）。</summary>
        static int SoftCuts(float[] dst, float a1, float a2, float p, float q)
        {
            int n = 0;
            if (p > a1 + 0.01f && p < a2 - 0.01f) dst[n++] = p;
            if (q > a1 + 0.01f && q < a2 - 0.01f && (n == 0 || Mathf.Abs(q - p) > 0.01f)) dst[n++] = q;
            if (n == 2 && dst[0] > dst[1]) { float t = dst[0]; dst[0] = dst[1]; dst[1] = t; }
            return n;
        }

        /// <summary>🆕 **2026-10-08（A225-①）**：把 `vis` **硬裁**到 `clip` 内（`uv0` 按同一段相对几何缩过去）。
        /// 返回 `false` = **整块在框外** ⇒ 调用方一个像素都别画。
        ///
        /// <para>🔴 **为什么要它**：`ApplySoftEdges` 只管「沿带宽切开 + 上 alpha 斜坡」，**不负责裁** ——
        /// 它假定调用方给的 `vis` 已经在框内（`Rect` / `Nine` / `Tiled` 三条路都先过 `ClipRect`）。
        /// 但 **`Shell/PracticeModePopup.ImgTex` 这第 4 条路没裁**：它把**原始矩形**直接交进来
        /// ⇒ 越出视口的那一条**既不消失、也不削 alpha**（那一侧的 softness 常是 0 ⇒ `SoftAlpha` 第一句就
        /// `return 1f`）⇒ **整条画到视口外**，而原版 `RectMask2D` 是裁掉的。
        /// 实测（2026-10-07 · A225-①）：练习窗卡组列表**第 2 列**那格的 `IsPlayerDeck`（`Purity Seal_02`）
        /// 右沿 **640.27** 比视口右沿 **634.88** 多出 **5.39px** ⇒ 那 5.39px 一直画在视口外。</para>
        ///
        /// <para>🔑 **它也是自检那条红的原因**：`SoftCuts` 的切刀位置 = `clip.x1 + softPx.x` /
        /// `clip.x2 − softPx.x` —— `softPx.x == 0` 时那**就是框自己的两条边**，而它们在越界的 `vis` 内部
        /// ⇒ `ApplySoftEdges` 会沿框边把 quad 切成左右两块、子块**贴着宿主的边**
        /// ⇒ `Editor/MainMenuScene.cs` 的 `ScanSoftCuts(vertical: true)` 把它数成「两条竖切线」
        /// （期望 0 —— 语义是「`(0,23)` 只渐变上下」）。硬裁之后框边 == `vis` 的边，
        /// 不满足 `SoftCuts` 的「严格落在内部」⇒ 不再切，那两条也就不存在了。</para>
        ///
        /// <para>⚠️ **对本壳既有调用点零行为变化**：`Rect` / `Nine` / `Tiled` 传进来的 `vis` 已经在框内
        /// （`ClipRect` 的结果 / `ClipXxxChildren` 裁过的子块）⇒ 这里第一句就走「框内 ⇒ 一个字都不动」。</para>
        /// <para>⚠️ 世界↔像素一律走 `LayoutSpace`；uv 的 y **自下而上**而 `PxRect` 自上而下 ⇒ 纵向翻一次
        /// （与 `MenuDraw.Rect` 建 uv 那两行、`PlaceCell` 同一套规矩）。</para></summary>
        static bool ClipVisToClip(ref PxRect vis, PxRect clip, ref Rect uv0)
        {
            PxRect v;
            if (!ClipRect(vis, clip, out v)) return false;      // 整块在框外（含退化矩形）⇒ 不画
            if (SameRect(v, vis)) return true;                 // 整块在框内 ⇒ 一个字都不动
            float w = Mathf.Max(1e-6f, vis.W), h = Mathf.Max(1e-6f, vis.H);
            uv0 = new Rect(uv0.x + uv0.width * (v.x1 - vis.x1) / w,
                           uv0.y + uv0.height * (vis.y2 - v.y2) / h,
                           uv0.width * v.W / w,
                           uv0.height * v.H / h);
            vis = v;
            return true;
        }

        /// <summary>把一个 quad 按软边剖面处理：**必要时沿带的内沿切开**，每块的四角带上 alpha 斜坡。
        /// 最外层的 quad（`vis` = 它**已经硬裁过**的那块）**留在原节点上**（改成「含矩形中心的那一格」），
        /// 其余格建**子 quad**（同图/同队列/同 tint）。`softPx` 两个分量都 0 = **没有渐隐带**（不切、不上斜坡）
        /// —— 🔴 **但仍然按 `clip` 硬裁**（A277）。⛔ 原来这里写的是「⇒ 立刻返回（硬边 = 现有行为）」，
        /// 那句只对**调用方自己先裁过**的那三条路（`Rect`/`Nine`/`Tiled`）成立，见下面那条分支的注释。
        /// ⚠️ **返回后原 quad 的矩形可能不再等于 `vis`**（它是其中一格）—— 断言要按「所有块的并集」量。
        ///
        /// <param name="uv0">🔴 **建这一份时的「整张图的 uv」**（每一格都从它里面取一小块），
        /// **不是** `q.UvRect`。⚠️ **2026-10-04（F1）改成显式入参** —— 以前这里写的是
        /// `var uv0 = q.UvRect;`，而 `PlaceCell` 会把「主格那一小块」写回 `q.UvRect`
        /// ⇒ **重切时读到的已经是缩过的那一份**，于是每重切一次采样区再乘一次（实测框 200/带 25：
        /// `(0.125,0.125,0.75,0.75)` → 重切 `0.5625` → `0.4219`，每次 ×0.75）。
        /// 宿主与子块**彼此自洽**，画面只是被放大/裁掉 ⇒ **既看不出缝、断言也不响**；
        /// 一直没被抓到是因为探针用的是 `CardArt.Solid()`（1×1 白图，uv 不可观测）。
        /// ⛔ **别改回读 `q.UvRect`** —— 触发点真的带电（`Nine` 那条路上每个子块都挂着重切回调）。</param></summary>
        public static void ApplySoftEdges(ImageQuad q, PxRect vis, PxRect clip, Vector2 softPx, Rect uv0)
        {
            if (q == null) return;
            // 🔴 **2026-10-09（A277）就地订正（铁律 5）**：这里原来是
            //    `if (softPx.x <= 0f && softPx.y <= 0f) return;`（紧跟在 `q == null` 后面、第 3 行就早退）
            //    —— 于是「`clip` 有值 + `soft = (0,0)`」时**连硬裁都不做**（调用方传了 `clip` 也不裁）。
            //    那条早退**已删除**：硬裁挪到下面 `ClipVisToClip` 之后**无条件**做，两分量都 0 只表示
            //    「没有渐隐带」（不切、不上斜坡）⇒ 见下面那条零 softness 分支。
            if (vis.W <= 0.01f || vis.H <= 0.01f) return;
            // 🆕 **2026-10-04（A38③）**：记下**上斜坡之前**的四角色 —— 重切时要先还原，
            //    否则 alpha 会在旧斜坡上再乘一遍（越裁越暗，静默）。见 `ReapplySoftEdges`。
            var baseCorners = CaptureCorners(q);

            // 🆕 **2026-10-08（A225-①）**：**先按 `clip` 硬裁一刀**（`uv0` 同步缩）。
            //    判据 / 为什么必须有它 / 对本壳既有调用点为什么零变化 → `ClipVisToClip` 的注释。
            //    ⚠️ 顺序：**在切刀之前**（`SoftCuts` 的判据是「切刀严格落在 `vis` 内部」——
            //      裁完框边就等于 `vis` 的边，不再满足 ⇒ 不会在框边冒一条假切口）。
            PxRect visRaw = vis;      // 🆕 A277：裁之前那一份 —— 下面「零 softness」那条分支靠它判「有没有真裁掉一块」
            if (!ClipVisToClip(ref vis, clip, ref uv0))
            {
                // 整块在裁切框外 ⇒ 原版 `RectMask2D` 是**一个像素都不画**。
                // ⚠️ **不关节点、不销毁**（保持结构；也让 `ReapplySoftEdges` 之后还能救回来）：
                //    四角 alpha 清 0 就够（`baseCorners` 已经在上面记好了 ⇒ 重切时能还原）。
                q.SetCornerColors(new Color(1f, 1f, 1f, 0f), new Color(1f, 1f, 1f, 0f),
                                  new Color(1f, 1f, 1f, 0f), new Color(1f, 1f, 1f, 0f));
                ArmSoftRebuild(q, vis, QuadRectPx(q), baseCorners, clip, softPx, uv0);
                return;
            }

            // 🆕 **2026-10-09（A277）**：`softPx` 两分量都 0 = **没有渐隐带** ⇒ 不切、不上 alpha 斜坡，
            //    但**硬裁照做** —— 上面那一句 `ClipVisToClip` 已经算完了，这里只要把 quad 摆到裁剩的那块。
            // 🔴 **判据** = 原版 `RectMask2D`：uGUI `Culling/Clipping.cs:17` 的 `FindCullAndClipWorldRect`
            //    **四边都求交**（`xMin/xMax/yMin/yMax` 各一条），而 `m_Softness` 只经
            //    `UpdateClipSoftness()` 交给 shader 当**渐隐带宽** ⇒
            //    **「某个分量 = 0」推不出「那个方向不被裁」** ⇒ 「有 `clip` 就必须裁」，与 softness 无关。
            //    （同一条判据的另一种说法 → `资料/已知的坑.md` 2026-10-08「「软边」≠「裁切」」那一节。）
            // 🔴 **为什么原来那两路没事**：`Rect` / `Nine` / `Tiled` 三条路**自己**先过了一遍裁切
            //    （`ClipRect` / `ClipNineChildren` / `ClipTiledChildren`）⇒ 它们早退的只是「软边加工」；
            //    而 `PracticeModePopup.ImgTex` 那条**没自裁**（它把原始矩形直接交进来，判据见 `ClipVisToClip`）
            //    ⇒ 早退在它身上就是「传了 `clip` 也画到框外」。
            // ⚠️ **今天仍然带着那道闸的只剩 `Rect`/`Nine`/`Tiled` 三条路**（它们**自己**先裁过一遍 ⇒ 那道闸
            //    只省掉「软边加工」，不会漏裁）；`PracticeModePopup.ImgTex` 那道闸**本批已去掉**（A233）
            //    ⇒ 本条对它**带电**（但今天没有调用点给它传 `(0,0)` ⇒ 实际行为仍是**零变化**）；
            //    牙口 → `Editor/ShellScene.cs` 的「`clipSoftness = 0` **也照样硬裁**」那一节。
            if (softPx.x <= 0f && softPx.y <= 0f)
            {
                // 真裁掉一块才动几何/uv —— 整块本来就在框内 ⇒ **一个字节都不写**（与旧行为逐字相同）
                if (!SameRect(vis, visRaw)) PlaceCell(q, vis, vis, uv0);
                // 与相邻两支（切开 / 整块不切）一致：进了本函数的 quad 都挂上重切回调 —— 之后再改几何时
                // 裁切跟着重算。⛔ 不挂的话，改完几何就会把越界那一条**静默**又画出来（正是 A225-① 那个病）。
                ArmSoftRebuild(q, vis, vis, baseCorners, clip, softPx, uv0);
                return;
            }

            var cutX = new float[2]; int nx = SoftCuts(cutX, vis.x1, vis.x2, clip.x1 + softPx.x, clip.x2 - softPx.x);
            var cutY = new float[2]; int ny = SoftCuts(cutY, vis.y1, vis.y2, clip.y1 + softPx.y, clip.y2 - softPx.y);
            if (nx == 0 && ny == 0)
            {
                // 整块都在同一个「线性段」里 ⇒ **不用切**，只上四角 alpha（含「整块都在带里」那种）
                // 🆕 **2026-10-04（F1 同族）**：这条路上**重切进来**时宿主身上还留着上一刀那块主格 ——
                //    子块已经在 `ReapplySoftEdges` 的 `SoftEdgeClear` 里销毁了、这里又不再切
                //    ⇒ 不把它摆回「整个 `vis`」的话，**画面只剩主格那一块**（缺掉的部分**静默不画**），
                //    uv 也停在上一次那份（= 同一个「越缩越小的子矩形」病，只是不再逐次相乘）。
                //    判据：宿主当前矩形与 `vis` 不一致（**0.05px 容差** —— `QuadRectPx` 是「世界→px」
                //    反推，与建的时候那组数隔着一次浮点往返，逐字段 `==` 会假阴）⇒ 摆回 `vis` + `uv0`。
                //    ⚠️ **首次切进来时 `vis` 就是宿主自己的矩形**（`Rect` 建它的那个 / `Nine`·`Tiled` 的
                //    `QuadRectPx`），而且那时 `SoftEdgeRebuild` **还没挂** ⇒ 两个条件都不成立，
                //    这一句**一个字都不动**（零行为变化，且与浮点往返无关）。
                //    ✅ **2026-10-05（A58-R7）：这一支现在有探针了** —— `Editor/ShellScene.cs` 的 ⑤·c-2
                //    （先按常规切一刀 3×3，再把宿主缩到 15px ⇒ 映射后 `vis=(90,90,110,110)`、
                //     两个带内沿 25/175 都在它外面 ⇒ 逼它落进这一支）。
                //    **怎么改坏就会红**：拿掉下面这句 `PlaceCell` ⇒ ① 宿主高停在 15（而不是 20）
                //    ② uv 停在上一刀的主格那份（0.5625 vs 1.0）③ `SoftEdgeUvDrifts` 涨 1 —— **三条同时红**。
                // 🔴 **2026-10-10（A282）去掉了那个 `q.SoftEdgeRebuild != null` 闸** —— 它的**前提已被推翻**：
                //    上面那条注释假设「首次切进来时 `vis` 就是宿主自己的矩形」，而 **A225-① 已在它前面插了
                //    `ClipVisToClip`** ⇒ 首次调用时 `vis` 就可能是**裁小过的**那份 ⇒ 两个条件里后一个**成立**、
                //    前一个（`SoftEdgeRebuild` 还没挂）**不成立** ⇒ **这一句一个字都不动** ⇒
                //    「**裁过了、但没落在任何切刀之间**」的件，宿主几何不动 ⇒ **越界像素照旧画**。
                //    实算（写手 W2 纯算术复算，`资料/普查产出_1009/写手W2_A277_A233.md` §三·1）：
                //    练习窗 `DeckRow_0` 第 2 列 `IsPlayerDeck`（`Purity Seal_02`）**右侧 5.39px 仍在视口外**。
                //    判据 = 未裁时 `QuadRectPx(q)` 与 `vis` 在 0.05px 内（`SameRectNear` 就是为浮点往返准备的）
                //    ⇒ **去掉闸之后，「本来就没裁」的那些件仍然一个字不动**（零行为变化），
                //      只有「真裁过」的才 `PlaceCell`。
                //    ⚠️ 验收 = 练习窗那几颗的并集右沿 ≤ 634.88 + `MainMenuScene.Run` 切线条数 0/0
                //      + `ShellScene.Run` 的 `SoftEdgeUvDrifts` 仍 0。
                if (!SameRectNear(QuadRectPx(q), vis)) PlaceCell(q, vis, vis, uv0);
                SetRamp(q, vis, clip, softPx);
                ArmSoftRebuild(q, vis, vis, baseCorners, clip, softPx, uv0);
                CheckSoftEdgeUv(q, uv0, "整块不切");
                return;
            }
            var xs = new float[4]; xs[0] = vis.x1; xs[1 + nx] = vis.x2;
            for (int i = 0; i < nx; i++) xs[1 + i] = cutX[i];
            var ys = new float[4]; ys[0] = vis.y1; ys[1 + ny] = vis.y2;
            for (int i = 0; i < ny; i++) ys[1 + i] = cutY[i];
            int sx = nx + 1, sy = ny + 1;                     // 段数

            // 原节点留给**含矩形中心**的那一格（确定性）；其余格建子 quad
            int mi = 0, mj = 0;
            for (int i = 0; i < sx; i++)
                if (vis.CX >= xs[i] && (vis.CX < xs[i + 1] || i == sx - 1)) { mi = i; break; }
            for (int j = 0; j < sy; j++)
                if (vis.CY >= ys[j] && (vis.CY < ys[j + 1] || j == sy - 1)) { mj = j; break; }

            var oldTint = q.Tint;
            int oldQ = q.RenderQueue;
            string baseName = q.gameObject.name;

            // ⚠️ **主格先摆**：子块是挂在**本节点**下的，它的世界位置要当参考原点 —— 主格没摆好之前
            //    算出来的偏移会差「原中心 → 新中心」那一段（会静默地整体错位）。
            var mainCell = new PxRect(xs[mi], ys[mj], xs[mi + 1], ys[mj + 1]);
            PlaceCell(q, mainCell, vis, uv0);
            SetRamp(q, mainCell, clip, softPx);

            for (int i = 0; i < sx; i++)
                for (int j = 0; j < sy; j++)
                {
                    if (i == mi && j == mj) continue;
                    var cell = new PxRect(xs[i], ys[j], xs[i + 1], ys[j + 1]);
                    if (cell.W <= 0.01f || cell.H <= 0.01f) continue;
                    // 🔴 **2026-10-11（A294）**：这一句的 `RectCenter − 宿主世界位置` 与 `Local` 是**同一个形状**
                    //    （写进的是**新子件**的 `localPosition`），同样少除一次父链缩放 ⇒ 收口到同一份
                    //    `PosInDesignSpace`（`k == 1` 时逐位相同）。⚠️ 紧接着的 `PlaceCell(sub, …)` 会把几何
                    //    再摆一遍（那一步本来就走 `Local`）—— 这一句只影响**建的那一刻**的落点，
                    //    也就是 `PlaceCell` 里那个「相对本节点」参考原点用的世界位置。
                    var sub = ImageQuad.Create(q.transform, q.Texture,
                                               LayoutSpace.RectCenter(cell.x1, cell.y1, cell.x2, cell.y2) - PosInDesignSpace(q.transform),
                                               LayoutSpace.Px(cell.H), new Vector2(0.5f, 0.5f),
                                               baseName + "_soft" + i + j);
                    if (sub == null) continue;
                    PlaceCell(sub, cell, vis, uv0);
                    sub.SetTint(oldTint);
                    sub.SetRenderQueue(oldQ);
                    SetRamp(sub, cell, clip, softPx);
                    q.SoftEdgeRegister(sub);          // 🆕 A38③：登记 ⇒ 之后父件 `SetTint` 时它跟着刷
                }

            // 🆕 **A38③**：挂上「几何一变就重切」的回调（在此之前**不能挂** —— 重切要走一遍
            //    `PlaceCell`（内含 `SetAspect`/`SetWorldHeight`），挂了会在半路被自己叫回来）。
            ArmSoftRebuild(q, vis, mainCell, baseCorners, clip, softPx, uv0);
            CheckSoftEdgeUv(q, uv0, "切开");
        }

        /// <summary>🆕 **2026-10-04（A38③）：软边的重切。** 触发点 = 宿主 quad 之后被
        /// `SetAspect` / `SetWorldHeight` 改了几何（`ImageQuad.NotifySoftEdgeChanged`）。
        ///
        /// 🔴 **为什么必须重切**：子块是按**切那一刻**的框摆的（位置/尺寸/uv 全是那次切的产物）
        /// ⇒ 父件一改几何，边带那几块就停在旧框上（**静默**：只有挨着视口边那一条不对）。
        /// <para>**新框怎么来**（⚠️ **这条映射是【我们挑的】，不是原版值** —— 原版每次重排都由 UGUI
        /// 整块重算，压根没有「切完之后再改尺寸」这件事，所以拿不到判据）：
        /// 取「**旧主格 → 新主格**」那个线性映射（平移 + 逐轴缩放），把**旧并集**整体搬过去
        /// —— 语义是「你改的那个格子带着其余部分按同比例走」。主格 = 含矩形中心的那一格
        /// （`ApplySoftEdges` 就是这么选的，所以映射锚点稳定）。</para>
        /// <para>顺序要紧：① 先销毁旧子块 ② 把宿主四角色**还原成上斜坡之前的**（不然 alpha 会再乘一遍）
        /// ③ 再照新框切一刀。</para>
        /// <para>🔴 **2026-10-04（F1）**：`uv0`（**第一次切时那一份「整张图的 uv」**）一路带下来，
        /// 重切时**从它重新推导**，⛔ **不能**读 `q.UvRect`（那已经是上一刀缩过的主格了 —— 那样每重切
        /// 一次采样区再缩一次，画面被放大/裁掉且**宿主与子块彼此自洽、断言不响**）。</para></summary>
        static void ReapplySoftEdges(ImageQuad q, PxRect visOld, PxRect cellOld, Color[] baseCorners,
                                     PxRect clip, Vector2 softPx, Rect uv0)
        {
            if (q == null) return;
            q.SoftEdgeClear();
            RestoreCorners(q, baseCorners);
            var cellNew = QuadRectPx(q);
            float sx = cellOld.W > 0.01f ? cellNew.W / cellOld.W : 1f;
            float sy = cellOld.H > 0.01f ? cellNew.H / cellOld.H : 1f;
            var visNew = new PxRect(cellNew.CX + (visOld.x1 - cellOld.CX) * sx,
                                    cellNew.CY + (visOld.y1 - cellOld.CY) * sy,
                                    cellNew.CX + (visOld.x2 - cellOld.CX) * sx,
                                    cellNew.CY + (visOld.y2 - cellOld.CY) * sy);
            SoftEdgeRebuilds++;
            // 🔴 **新框仍要过一遍裁切**：重切不重新裁的话，变大之后那些块会**画到视口外**
            //    （原版 `RectMask2D` 一视同仁地裁）。整块落到框外 ⇒ 一块都不留（= 原版全被裁掉）。
            PxRect visClip;
            if (!ClipRect(visNew, clip, out visClip))
            {
                // 🔴 **只有软边宿主会自关**（2026-10-05 注明口径，行为不变）：走到这一支的是**软边那棵树**
                //    （`ApplySoftEdges` 登记过、`SoftEdgeRebuild` 挂着回调的宿主）—— 它整块重切后被裁光，
                //    于是连宿主一起 `SetActive(false)`（子块已经清空，留着只会是空节点）。
                //    **非软边件出框不关**：`MenuDraw.Rect/Nine/Tiled` 走的是「整块在视口外 ⇒ 连节点一起不建」
                //    或「建完逐子块截」（`ClipXxxChildren`），**关不关、什么时候重建由调用方管**。
                q.gameObject.SetActive(false);
                ArmSoftRebuild(q, visNew, cellNew, baseCorners, clip, softPx, uv0);   // 以后再改还可能回来
                return;
            }
            if (!q.gameObject.activeSelf) q.gameObject.SetActive(true);
            ApplySoftEdges(q, visClip, clip, softPx, uv0);      // 递归由 `ImageQuad._softBusy` 挡住
        }

        /// <summary>重切的次数（自检可以断这条路带电）。</summary>
        public static int SoftEdgeRebuilds;

        /// <summary>软边树 **uv 不变量**的违规次数（非 0 = **宿主 uv 不是本趟写进去的那一份**）。
        /// ⚠️ 它**只在真的切过软边时**才可能动（没进软边那条路的一次都不加）。
        /// 🔴 **2026-10-04 R-F 审查订正：它的牙口比原来写的窄得多** —— 期望值是从**本趟传进来的 `uv0`** 算的，
        /// 而每块的 uv 也是 `PlaceCell` 从同一个 `uv0` 切出来的 ⇒ **面积望远镜求和恒等**：
        /// 把 `uv0` 换回 `q.UvRect`（F1 那个真缺陷）时 `want` 跟着缩，**四种情形计数都不动**。
        /// ⇒ 它能抓的**只有**「宿主 uv 不是本趟那份」（= `ReapplySoftEdges` 里不切分支的洞②）。
        /// ✅ **能真红的那条断言在宿主侧**（`Editor/ShellScene.cs` ⑤·c）：**出生时抓一份 uv、重切后比面积和**
        /// —— 那一份期望值**独立于实现**，换回旧写法得 0.5625 vs 1.0 ⇒ 立刻红。</summary>
        public static int SoftEdgeUvDrifts;

        /// <summary>🔴 **软边树的 uv 不变量**：宿主 + 全部子块的 uv 加起来必须**正好铺满整张图**（`uv0`），
        /// 且**没有一块采到整张图之外**。
        /// <para>**为什么它不是自证**（铁律 12 那条）：期望值有两重**独立**来源 ——
        /// ① `uv0` 是**调用方给的「整张图的 uv」**（`Rect` 是「整张图 ∩ 裁切框」那个算式的结果 ·
        /// `Nine`/`Tiled` 是子块建好那一刻自己的 `UvRect` · **重切那条路是第一次切时存下来的那一份**）
        /// —— 不是「切完再回读宿主」；② 面积和是**从几何独立算出来的**（每块越界多少就取多少 uv）。</para>
        /// <para>⚠️ **它抓不到什么（R-F 2026-10-04 复核）**：「把 `uv0` 换回 `q.UvRect`」那一档**抓不到** ——
        /// 见上面 `SoftEdgeUvDrifts` 的订正说明；那一档由**宿主侧**那条（出生时抓 uv、比重切后的面积和）负责。</para>
        /// <para>自检怎么用（一行，宿主 = 已经切过软边的那颗 quad）：
        /// <c>Check(MenuDraw.SoftEdgeUvDrifts, 0, "宿主 uv 是本趟写进去的那一份");</c>
        /// —— 期望值是**字面量 0**，⛔ 别从被测实现里读；⛔ 也别把这条当成「uv0 传错」的判据。</para></summary>
        static void CheckSoftEdgeUv(ImageQuad host, Rect uv0, string tag)
        {
            if (host == null) return;
            double want = (double)uv0.width * uv0.height;
            if (want <= 1e-9) return;                       // 退化（整块被裁没）：没有可断的不变量
            double got = 0; int n = 0; bool outside = false;
            var all = host.GetComponentsInChildren<ImageQuad>(true);
            for (int i = 0; i < all.Length; i++)
            {
                var k = all[i];
                if (k == null) continue;
                var u = k.UvRect;
                got += (double)u.width * u.height; n++;
                if (u.xMin < uv0.xMin - 1e-4f || u.yMin < uv0.yMin - 1e-4f
                    || u.xMax > uv0.xMax + 1e-4f || u.yMax > uv0.yMax + 1e-4f) outside = true;
            }
            float tol = Mathf.Max(1e-3f, (float)want * 0.01f);   // 1%：真漂移是 25% 起，浮点只在 1e-6 量级
            if (!outside && Mathf.Abs((float)(got - want)) <= tol) return;
            SoftEdgeUvDrifts++;
            if (SoftEdgeUvDrifts <= 3)
                Debug.LogWarning($"[MenuDraw] 软边的 uv **没有铺满整张图**（{tag} · 「{host.name}」）："
                               + $"{n} 块加起来 {got:F6}，整张图 {want:F6}"
                               + (outside ? "；且有块采到了整张图之外" : "")
                               + " —— 症状是「**每重切一次采样区再缩一次**」（画面被放大/裁掉，"
                               + "宿主与子块彼此自洽 ⇒ 看不出缝）。判据：`uv0` 必须是**建这一份时的原始 uv**，"
                               + "⛔ 不能读回被切过的 `q.UvRect`。");
        }

        static void ArmSoftRebuild(ImageQuad q, PxRect vis, PxRect cell, Color[] baseCorners,
                                   PxRect clip, Vector2 softPx, Rect uv0)
        {
            if (q == null) return;
            q.SoftEdgeRebuild = () => ReapplySoftEdges(q, vis, cell, baseCorners, clip, softPx, uv0);
        }

        static Color[] CaptureCorners(ImageQuad q)
        {
            var c = q != null ? q.CornerColors : null;
            if (c == null || c.Length != 4) return null;
            return new[] { c[0], c[1], c[2], c[3] };
        }

        /// <summary>把四角色还原成基线（`null` = 从来没设过 ⇒ 四角全白，与 `RebuildMesh` 的默认一致）。</summary>
        static void RestoreCorners(ImageQuad q, Color[] b)
        {
            if (q == null) return;
            if (b == null) { q.SetCornerColors(Color.white, Color.white, Color.white, Color.white); return; }
            q.SetCornerColors(b[0], b[1], b[2], b[3]);
        }

        /// <summary>把一块（`cell`，是 `vis` 的一格）摆到它该在的位置：几何 = 格子、uv = 从 `uv0` 里对应的一小块。</summary>
        static void PlaceCell(ImageQuad q, PxRect cell, PxRect vis, Rect uv0)
        {
            var lp = Local(q.transform.parent, cell.x1, cell.y1, cell.x2, cell.y2);
            lp.z = q.transform.localPosition.z;               // z 不动（同队列里还靠它排序）
            q.transform.localPosition = lp;
            q.SetWorldHeight(LayoutSpace.Px(cell.H));
            q.SetAspect(cell.W / Mathf.Max(1e-6f, cell.H));
            // 本格在 `vis` 里的相对位置 → 映射进 `uv0`（uv 的 y **自下而上**、`PxRect` 自上而下 ⇒ 翻一次，
            // 与 `ClipNineChildren` / `MenuDraw.Rect` 的 uv 那几行同一条规矩）
            float w = Mathf.Max(1e-6f, vis.W), h = Mathf.Max(1e-6f, vis.H);
            q.SetUvRect(new Rect(uv0.x + uv0.width * (cell.x1 - vis.x1) / w,
                                 uv0.y + uv0.height * (vis.y2 - cell.y2) / h,
                                 uv0.width * cell.W / w,
                                 uv0.height * cell.H / h));
        }

        /// <summary>一块的四角 alpha 斜坡：**只乘 alpha**（rgb 不动 —— 乘了 rgb 会在 `Sprites/Default`
        /// 的 `c.rgb *= c.a` 之外再暗一次）。四角都 ≥ 1 且本来就没有顶点色 ⇒ **一个字节都不改**。</summary>
        static void SetRamp(ImageQuad q, PxRect cell, PxRect clip, Vector2 softPx)
        {
            float bl = SoftAlpha(cell.x1, clip.x1, clip.x2, softPx.x) * SoftAlpha(cell.y2, clip.y1, clip.y2, softPx.y);
            float br = SoftAlpha(cell.x2, clip.x1, clip.x2, softPx.x) * SoftAlpha(cell.y2, clip.y1, clip.y2, softPx.y);
            float tr = SoftAlpha(cell.x2, clip.x1, clip.x2, softPx.x) * SoftAlpha(cell.y1, clip.y1, clip.y2, softPx.y);
            float tl = SoftAlpha(cell.x1, clip.x1, clip.x2, softPx.x) * SoftAlpha(cell.y1, clip.y1, clip.y2, softPx.y);
            if (bl >= 0.9999f && br >= 0.9999f && tr >= 0.9999f && tl >= 0.9999f) return;
            var c = q.CornerColors;
            Color b0 = c != null && c.Length == 4 ? c[0] : Color.white;
            Color b1 = c != null && c.Length == 4 ? c[1] : Color.white;
            Color b2 = c != null && c.Length == 4 ? c[2] : Color.white;
            Color b3 = c != null && c.Length == 4 ? c[3] : Color.white;
            q.SetCornerColors(new Color(b0.r, b0.g, b0.b, b0.a * bl),
                              new Color(b1.r, b1.g, b1.b, b1.a * br),
                              new Color(b2.r, b2.g, b2.b, b2.a * tr),
                              new Color(b3.r, b3.g, b3.b, b3.a * tl));
        }

        // ============================================================ 文字的部分越界（原版 `RectMask2D` 对 TMP）
        //
        // 🔴 **原版**：`RectMask2D` 对文字和图片**一视同仁**（掩码在 shader 里按像素裁）。
        //   我们这套是「文字没法像图那样截 uv」——TMP 的 mesh 每个字就是一个四边形，
        //   **可以按同样办法切**：把落在框外的那些顶点**夹到框边上**、并**按比例改它的 uv**
        //   （判据 = 同一个四边形：`uv` 是顶点位置的仿射函数 ⇒ 位置夹多少、uv 就跟多少）。
        //   ⇒ 压在视口边上的那几个字**会被切半个**，与原版同形（此前是整块照画出去）。
        // ⚠️ **必须在最后调**：`SetText` / `SetGlyphHeight` / `SetAutoFitBox` 任何一次重排都会把 mesh 重算回去
        //   （`Label` 内部走 `ForceMeshUpdate`）。各调用点都在「建完 + 定完字号」之后才调它。

        /// <summary>把一段文字的渲染网格**裁到框内**（同时按需叠软边 alpha 斜坡）。
        /// 两条后端都管：TMP（正常那条）与点阵兜底（那是一个贴图四边形 ⇒ 同一套「夹顶点 + 改 uv」）。
        /// 两条都拿不到网格时**出声**（`TextClipUnavailable` 计数 + 一条警告），不静默。
        /// 返回 true = 真的动过（自检拿它断「这条路带电」）。
        /// ⚠️ **2026-10-08 起 `false` 有两种含义**：① 这段文字压根没有可裁的网格（`TextClipUnavailable`）；
        /// ② 有网格、但**这一刻没有在渲染的那一份**（TMP 没 `Awake` 过 ⇒ 上传会 NRE，见 `ClipTmpMesh`）
        /// —— 第二种会数进 `TextClipUploadSkipped`。两者都**出声**，区别只在计数器。</summary>
        public static bool ClipText(Label lb, PxRect? clip, Vector2 softPx)
        {
            // 🔴 **2026-10-12（A198② 阶段 1）**：取裁切状态走同一份解析（`ViewportClip.Resolve`）。
            //    ⚠️ 父链从**这个标签自己**往上走 —— `Label` 是 `Text` 建在调用方那个 `parent` 之下的
            //    ⇒ 与「从 `parent` 走」同一条链（多一层自己，而节点上挂 `ViewportClip` 同样命中）。
            //    🔴 **2026-10-12（A435①）**：交给 `ArmTextGuard` 的必须是 **`clip` 形参【调用方原样那一份】**
            //    （未经解析）—— 守卫现在**自己会在每次重裁时重新解析**，理由写在它那儿。
            //    🔴🔴 **2026-10-13（A484）就地订正（铁律 5）**：上面这句自陈原来**与代码相反** ——
            //    下面那句 `clip = _st.RenderClip;` 把形参冲掉了 ⇒ `ArmTextGuard(lb, clip, …)` 交出去的
            //    其实是**解析后的快照**（`Resolve` 第 1 支「形参非空 ⇒ 连父链都不走」），两个后果都静默：
            //      ① 节点挪了 / 后挂 ⇒ 守卫**永远按 `Arm` 那一刻的旧框重裁**（字被切在错的位置上）；
            //      ② 每重裁一次就给漏删探测器 `ViewportClip.NodeShadowedByParam` **+1** —— 它本意是抓
            //         「旧设站点没删干净」，却把守卫自己存的快照**误报**成那种痕迹。
            //    ⇒ 先把形参留一份（`clipArg` 才是「调用方原样那一份」）；`clip` 本身照旧被覆盖成解析后的框
            //      （下面 `ClipTextNow` 那一刀用的就是它）。牙口 = `Editor/ShellScene.cs` 的 A464·B3。
            //    ⚠️ `softPx` 那一份**不必**另留（逐位等价，⛔ 别顺手「修」它）：形参非空时 `Resolve` 第 1 支
            //      把 `softPx` 原样带出（`_st.Softness == softPx`）；形参为 `null` 时软边由节点状态给
            //      （第 2 支），守卫重裁那一支**不读**存下来的这一份 ⇒ 两种取法结果相同。
            var clipArg = clip;   // ⛔ 别删、别把下面那句改成传 `clip` —— 理由见上（A484）
            var _st = ViewportClip.Resolve(lb != null ? lb.transform : null, clip, softPx, default(Vector4));
            clip = _st.RenderClip;
            softPx = _st.Softness;
            if (lb == null || !clip.HasValue) return false;
            var c = clip.Value;
            if (softPx.x < 0f) softPx.x = 0f;
            if (softPx.y < 0f) softPx.y = 0f;
            bool ok = ClipTextNow(lb, c, softPx);
            // 🆕 **2026-10-04（A38②）：挂上「重排之后自动重裁」的守卫** —— 见 `ClippedTextGuard`。
            // 🔴 交的是 `clipArg`（= 形参原样）—— ⛔ 别改回 `clip`（那是被上面 `clip = _st.RenderClip;` 覆盖成的解析后快照，A484）。
            ArmTextGuard(lb, clipArg, softPx);
            return ok;
        }

        /// <summary>「现在就裁一刀」，**不挂守卫**（守卫用它自动重裁；`ClipText` 是「裁 + 挂」）。
        /// 判据与实现全在上面那一段注释里；两条后端（TMP / 点阵兜底）各走一条。
        /// 拿不到网格时**出声**（`TextClipUnavailable` 计数 + 一条警告），不静默。
        /// 🔴 **2026-10-08 补第二种「拿不到网格」**：TMP 的渲染网格（`MeshFilter.sharedMesh`）不在
        /// —— 算出来的那一刀**不上传**（上传会 NRE，见 `ClipTmpMesh` 里那段根因），并
        /// **出声**（`TextClipUploadSkipped` 计数 + 一条警告）。两种情况分开计数，别合成一个。</summary>
        public static bool ClipTextNow(Label lb, PxRect clip, Vector2 softPx)
        {
            if (lb == null) return false;
            if (softPx.x < 0f) softPx.x = 0f;
            if (softPx.y < 0f) softPx.y = 0f;
            var tmp = lb.GetComponentInChildren<TMPro.TextMeshPro>();
            if (tmp != null) return ClipTmpMesh(tmp, clip, softPx);
            var mf = lb.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null) return ClipQuadMesh(lb.transform, mf.sharedMesh, clip, softPx);
            // 🔴 **2026-10-15（A821）：计数改走 `Label.TakeClipFailMark`（一颗标签只记一次）** ——
            //    见那一口的注释：`MenuDraw.Text` 末尾那一刀 + 7 个包装器各自的第二刀会把同一颗数两遍。
            if (lb.TakeClipFailMark(false))
            {
                TextClipUnavailable++;
                if (TextClipUnavailable <= 3)
                    Debug.LogWarning($"[MenuDraw] 「{lb.name}」没有可裁的渲染网格（TMP 与点阵兜底都没建起来）"
                                   + " —— 这一段文字**不会被裁到框内**（原版 `RectMask2D` 会）。");
            }
            return false;
        }

        /// <summary>`ClipText` 拿不到网格的**唯一标签**数（自检断它 == 0：**`MainMenuScene.Run`** 与 `RewardsScene.Run` 各一条 ——
        /// A195② 之前主菜单那一边**没有读者**，落空只在控制台出声、不进断言账）。
        /// <para>🔴 **2026-10-15（A821）：口径从「次数」改成「**唯一标签**数」。** 上面 `TextCore` 的注释早就点出
        /// 「同一颗 `Label` 裁两刀会让两个诊断计数虚高」，而那条纪律**在包装器那一层没有对应物**：
        /// `ItemDrawer.TextCentered` / `ClippedText` · `ChatPanel.Text` · `AllianceMemberTab.TextSoft` ·
        /// `SettingsWindow.Text` · `PlayerProfileWindow.Text` · `WindowsManager.Text` · `MenuWindowBase.Text` /
        /// `TextBox` 这 **7 个口**都是「先 `MenuDraw.Text`（末尾自己裁一刀）、**再自己 `ClipText` 一刀**」
        /// ⇒ 同一颗标签被数两遍。**计数与那三条 `Debug.LogWarning` 都改走 `Label.TakeClipFailMark`**（一颗一次）。
        /// ⚠️ **画面一个字都没变**（那两刀本来就落在同一代 mesh 上、同框幂等 —— 调度台 2026-10-15 裁：
        /// 不删那 7 句、不改行为）；⚠️ 断 `== 0` 的那两条**判别力一字不减**（它们问的是「有没有标签落空」，
        /// 而「同一个标签落空两次」从来不是它们要抓的东西）。判据全文 → `资料/已知的坑.md` §「2026-10-15 新增」1。</para></summary>
        public static int TextClipUnavailable;

        /// <summary>🆕 **重排之后自动重裁的次数**（A38②；自检可以断这条路**带电** —— 只断「挂上了守卫」
        /// 是不够的，那种断言改坏实现不会红）。
        /// ⚠️ 它只数**事件那条路**（`OnTextChanged`）。定完版面之后那一刀走 `TextReclipAfterPlace`。</summary>
        public static int TextClipReapplied;

        /// <summary>🆕 **2026-10-08（A225-②）**：`ClippedTextGuard.Reclip()` 真的重裁了一刀的次数
        /// （= 由 `Label.RefreshBounds()` 在**把 TMP 子节点挪到位之后**触发的那些）。
        /// 自检可以像 `TextClipReapplied` 那样断它**带电**（只断「挂上了守卫」那种断言改坏实现不会红）。</summary>
        public static int TextReclipAfterPlace;

        /// <summary>🆕 **2026-10-08（`RewardsScene.Run` 抛 NRE 那一件）**：算出来的那一刀**没能上传**的次数
        /// —— 即 TMP 身上**没有 `MeshFilter.sharedMesh`**（`Awake` 没跑过 / 那份网格已销毁）⇒
        /// `UpdateVertexData()` 会读空的 `m_mesh` 当场抛 `NullReferenceException`
        /// （根因与判据全文 → `ClipTmpMesh` 里那一大段注释）。
        /// <para>🔴 **2026-10-15（A821）：口径从「次数」改成「**唯一标签**数」** —— 理由与做法同
        /// `TextClipUnavailable` 那一条（相邻那一段），即**计数改走 `Label.TakeClipFailMark`**、
        /// 挂点 = 沿父链取回来的那颗 `Label`。「`MenuDraw.Text` 一刀 + 包装器一刀」那种同一颗数两遍
        /// 从此不再发生。</para>
        /// <para>🔴 **它 != 0 是【正常】的**（未激活的页签里建的标签都算 —— 比如 `Forge Tab` 出厂
        /// `activeSelf=false` 时那些重建），**所以别把它断成 0**；它 != 0 的意思是
        /// 「这些标签此刻没在渲染，那一刀等它们真显示出来时由 `ClippedTextGuard` 补」
        /// —— **不是**「裁切坏了」。要断就断「这条路带电」（== 由 `RewardsScene` 的跨边那一幕产生）。</para>
        /// <para>⚠️ 与 `TextClipUnavailable` **分开计数**：那个的语义是「`Label` 连渲染网格都没建起来」
        /// （`RewardsScene.cs:2891` 断它 == 0）；本条是「网格建起来了、但这一刻没有在渲染的那一份」。
        /// 合成一个会让那条既有断言在本场景里假红。</para></summary>
        public static int TextClipUploadSkipped;

        /// <param name="clip">**`ClipText` 收到的形参原样**（可空 —— `null` = 「不显式覆盖」，
        /// 重裁时由 `ClippedTextGuard.CurClip` 沿父链解析）。🔴 A435①：⛔ 别改成「解析后的框」。</param>
        static void ArmTextGuard(Label lb, PxRect? clip, Vector2 softPx)
        {
            if (lb == null) return;
            // ⚠️ 批处理下也能挂（组件本身不依赖帧循环 —— 它靠 TMP「文字已重排」那个事件），
            //    但**没有帧循环就没有重排事件** ⇒ 自检里别指望它替你自证：要验就在 `Play` 里点一次，或者直调 `ClipTextNow`。
            var g = lb.GetComponent<ClippedTextGuard>();
            if (g == null) g = lb.gameObject.AddComponent<ClippedTextGuard>();
            g.Arm(lb, clip, softPx);
        }

        /// <summary>TMP 那条：逐字夹顶点 + 改 uv（+ 软边 alpha）。
        /// <para>返回 `any` = 真改过至少一个字的数组。🔴 **但「改过数组」不等于「画面上生效」**：
        /// 把数组推给渲染网格那一步（`UpdateVertexData`）**要求 TMP 有一份在渲染的 `Mesh`**，
        /// 那一份不在时本函数**跳过上传、数进 `TextClipUploadSkipped`、返回 `false`** ——
        /// 根因（`RewardsScene.Run` 的 NRE）与判据全文 → 下面 `if (any)` 里那一大段。</para>
        /// <para>🆕 **2026-10-11（A250）：本方法是「把一段 TMP 的渲染网格裁进一个框」的【唯一实现】，已公开成公共件。**
        /// 调用契约（照抄，⛔ 别自己再写一份）：
        /// ① `clip` = **画布像素**矩形（左上原点）；② `softPx` = 原版 `RectMask2D.m_Softness`
        /// （**硬边就给 `Vector2.zero`** —— `SoftAlpha` 在 `soft ≤ 0` 时恒 1 ⇒ 不削 alpha、也不切几何）；
        /// ③ **调用它之前先保证 TMP 那份网格是新鲜的**（`tmp.ForceMeshUpdate()`）——
        /// 在**已经夹过的**网格上再夹一次 = 几何被夹第二次而 uv 只按第一次的比例走（**越裁越错，且静默**）；
        /// ④ 本方法**不判 `activeInHierarchy`**、也**不动** `textInfo` 之外的状态 ⇒ 那几条守卫归调用方
        /// （先例：`Core/CardView.ClipTextMesh` —— 卡不能裁未激活的 TMP，见那边）。</para></summary>
        public static bool ClipTmpMesh(TMPro.TextMeshPro tmp, PxRect clip, Vector2 softPx)
        {
            var ti = tmp.textInfo;
            if (ti == null || ti.characterInfo == null || ti.meshInfo == null) return false;
            int n = Mathf.Min(ti.characterCount, ti.characterInfo.Length);
            bool any = false;
            var p = new Vector3[4];
            var uv = new Vector2[4];
            var al = new float[4];
            for (int ci = 0; ci < n; ci++)
            {
                var ch = ti.characterInfo[ci];
                if (!ch.isVisible) continue;
                int mi = ch.materialReferenceIndex;
                if (mi < 0 || mi >= ti.meshInfo.Length) continue;
                var mesh = ti.meshInfo[mi];
                if (mesh.vertices == null || mesh.uvs0 == null) continue;
                int v = ch.vertexIndex;
                if (v < 0 || v + 3 >= mesh.vertices.Length || v + 3 >= mesh.uvs0.Length) continue;
                p[0] = mesh.vertices[v]; p[1] = mesh.vertices[v + 1];
                p[2] = mesh.vertices[v + 2]; p[3] = mesh.vertices[v + 3];
                for (int k = 0; k < 4; k++) { uv[k] = new Vector2(mesh.uvs0[v + k].x, mesh.uvs0[v + k].y); }
                bool moved = ClipQuad(tmp.transform, p, uv, clip, softPx, al);
                bool touched = false;
                if (moved)
                {
                    for (int k = 0; k < 4; k++)
                    {
                        mesh.vertices[v + k] = p[k];
                        mesh.uvs0[v + k] = new Vector4(uv[k].x, uv[k].y, mesh.uvs0[v + k].z, mesh.uvs0[v + k].w);
                    }
                    touched = true;
                }
                // 🔴 **软边对「没被切、但落在渐隐带里」的字**同样要削 alpha（原版掩码是按像素来的，
                //    与「这个字有没有被切」无关）—— 只动 alpha，rgb 一个字节都不碰。
                // 🔴🔴 **2026-10-08（A225-②）：这里写的是【绝对值】，基准 = 这个字自己的顶点色**
                //    （`TMP_CharacterInfo.vertex_{BL,TL,TR,BR}.color` —— TMP 的 `SaveGlyphVertexInfo`
                //     写好、`MeshInfo` 抄进 `colors32` 的那一份，见 `TMP_Text.cs:5317/5477/5566-5569`）。
                //    ⛔ **别改回 `cc.a * al[k]`**：那是「在现值上再乘」——同一代 mesh 里裁两刀就会
                //    **越裁越暗**（静默，只在软边带里现形）。而 `ClippedTextGuard.Reclip()` 现在会
                //    在**每次把版面定下来之后**都重裁一刀（见 `Label.RefreshBounds` 的尾巴）⇒ 必须幂等。
                //    ⚠️ 顺带：这样写**还能把上一代留下的斜坡冲掉**（标签滚出带外时不会「粘住」暗）。
                //    ⚠️ `al[k]` 里已经包含「硬裁」那一半（框外的角被夹到框边上 ⇒ `SoftAlpha` = 0）——
                //    ⇒ 写在框外的字仍然是**全透明**，与原版被 `RectMask2D` 裁掉等价。
                if (mesh.colors32 != null && v + 3 < mesh.colors32.Length)
                    for (int k = 0; k < 4; k++)
                    {
                        int want = Mathf.Clamp(Mathf.RoundToInt(BaseCornerAlpha(ch, k) * al[k]), 0, 255);
                        if (mesh.colors32[v + k].a == want) continue;      // 没变 ⇒ 一个字节都不写
                        var cc = mesh.colors32[v + k];
                        cc.a = (byte)want;
                        mesh.colors32[v + k] = cc;
                        touched = true;
                    }
                any |= touched;                                   // 「一个字都不用动」= touched 仍为 false
            }
            if (any)
            {
                // 🔴🔴 **2026-10-08（`RewardsScene.Run` 抛 NRE 那一件的根因 · 不许删这一道）**
                //    `UpdateVertexData` **不是**「往 `MeshInfo` 数组写」的别名 —— 它是
                //    `TextMeshPro.cs:408-446` 那段：**直接读私有字段 `m_mesh`（`:417`）再往里赋值（`:430`）**，
                //    而那句 `mesh.vertices = …` **一个 null 检查都没有**（同族的 Phase III 反而有：
                //    `GenerateTextMesh` 里 `if (m_subTextObjects[i] == null) continue;`）。
                //    `m_mesh` 只在一处被创建 —— `TextMeshPro.Awake()`（`:581-593`，判据 = 反编译里
                //    `Stfld TMP_Text.m_mesh` 只有 `Awake` 与 `get_mesh` 两个写点）——
                //    ⇒ **只要 `Awake()` 没跑过（对象在未激活的父链下 `AddComponent`）它就是 null。**
                //
                //    ⚠️ **而这种状态在本工程里是【会发生的】**：`RewardsScene` 的 `Forge Tab` 出厂
                //    `activeSelf=false`（原版 §二·4，自检 `Editor/RewardsScene.cs` 的 `Run` 里那条「`Forge Tab` 出厂关着」断言 断着），
                //    而滚动回调 `MenuScroll.OnChanged → ForgeTab.BuildRewardCells` 照样在**它没激活时**重建格
                //    ⇒ 那些标签的 TMP `Awake` 一次都没跑（`m_mesh == null`、连 `MeshFilter` 都还没挂）。
                //    可 **`textInfo` 照样是满的**：`TmpFont.SetWrapWidth`（每个 `TextBox` 都走）调
                //    `TextMeshPro.GetTextInfo()`，而那个方法**没有 `m_isAwake` 这道闸**，且它把
                //    `m_renderMode` 设成 `DontRender` **跳过了 Phase III** ⇒ 字形模型全生成好、
                //    `m_mesh` 一个字节都没碰（`TextMeshPro.cs:360-374`）。
                //    ⇒ 于是「`characterCount > 0` + `meshInfo` 有顶点 + `m_mesh == null`」这个状态是**真的**。
                //
                //    🔴 **为什么以前没炸**：这一句只在 `any`（真有一个字的角被夹出框）时才走到，而
                //    本文那一格里**唯一贴着视口边的是 `LevelLabel`**（`Shell/ForgeTab.cs` 里 `LevelLabel` 那一处，格心 −0.6 处、宽 85）——
                //    格心离开视口边 10px 以内才会被夹。A188 新加的「跨边那一幕」
                //    （`Editor/RewardsScene.cs:1934` 把该格中心滚到 `330.968`）**正是第一次**造成这种夹切。
                //    ⇒ 这一句从写下来那天起就带着这颗雷，只是**没有用例踩到过**（A225 那轮改的是 `any` 的口径，
                //      本案里 `moved == true` ⇒ 新旧口径都会走到这里 ⇒ **A225 不是诱因**，见本件报告）。
                //
                //    判据用 **TMP 自己的 `MeshFilter.sharedMesh`**（= `Awake`/`OnEnable` 里被赋成 `m_mesh`
                //    的那一份，`:589` / `:659`），**不是** `tmp.mesh` 那个属性 —— 后者在 `m_mesh == null` 时
                //    **会自己 new 一个**（`:143-155`）⇒ 拿它当判据等于把雷捂住，而且 new 出来的那份
                //    **没挂到 MeshFilter 上**、根本不会被画出来（静默）。
                //    ⚠️ 它同时兜住「`m_mesh` 已被 `OnDestroy`/`Reset` 销毁」那一档 —— Unity 的 `==` 重载
                //    对已销毁对象返回 null。
                var tmf = tmp.GetComponent<MeshFilter>();
                if (tmf == null || tmf.sharedMesh == null)
                {
                    // 🔴 **2026-10-15（A821）：计数改走 `Label.TakeClipFailMark`（一颗标签只记一次）** ——
                    //    见那一口的注释。⚠️ 本函数手上只有 TMP 那个**子节点**（`Label` 是它的父件）⇒
                    //    沿父链把 `Label` 取回来（带 `true`：未激活的页里那层父链**是关着的**，不带就取不到）。
                    //    ⛔ 别把标记改挂到 TMP 自己的 `GameObject` 上 —— 那样同一颗标签的两个档会各记一份、
                    //    而且 `Label` 换过 TMP 子件时旧标记会留在孤儿节点上。
                    //    ⚠️ 取不到 `Label`（例：`Core/CardView.ClipTextMesh` 那些卡面 TMP 不是 `Label` 建的）
                    //    ⇒ **退回旧行为「照数」**（那一族没有「同一颗裁两刀」的调用方，数不出虚高）。
                    var owner = tmp.GetComponentInParent<Label>(true);
                    if (owner == null || owner.TakeClipFailMark(true))
                    {
                        TextClipUploadSkipped++;
                        if (TextClipUploadSkipped <= 3)
                            Debug.LogWarning($"[MenuDraw] 「{tmp.name}」这一刀**没落到会被画出来的网格上**"
                                           + "（它身上没有 `MeshFilter.sharedMesh` —— TMP 还没 `Awake` 过，"
                                           + "或那份网格已销毁）⇒ 只改了 `textInfo` 里的数组、**没上传**。"
                                           + "⚠️ 这不是「裁不裁得动」，是「这段文字此刻根本没在渲染」；"
                                           + "等它真被显示出来（父链激活 ⇒ `Awake`/`OnEnable` ⇒ TMP 重排发"
                                           + "`ON_TEXT_CHANGED`）时，`ClippedTextGuard` 会照常补这一刀。");
                    }
                    return false;      // 「这一刀没落到会被画出来的东西上」—— 别谎报成功
                }
                tmp.UpdateVertexData(TMPro.TMP_VertexDataUpdateFlags.Vertices
                                   | TMPro.TMP_VertexDataUpdateFlags.Uv0
                                   | TMPro.TMP_VertexDataUpdateFlags.Colors32);
            }
            return any;
        }

        /// <summary>一个字里**第 `k` 个角**的基础 alpha（`k` = **0·1·2·3 = BL·TL·TR·BR**）。
        /// 🔴 这个序 = `ClipQuad` 的 `p`/`uv`/`al` 的序，也 = TMP 把颜色写进 `MeshInfo.colors32` 的序
        /// （`TMP_Text.cs:5566-5569`：`colors32[0]=vertex_BL` … `colors32[3]=vertex_BR`）——**三处同序**。
        /// <para>出处 = `TMP_Text.SaveGlyphVertexInfo`（`:5317/5477`）与 `SaveSpriteVertexInfo`（`:5477`）
        /// 都往 `characterInfo[i].vertex_{BL,TL,TR,BR}.color` 里写过 ⇒ 它就是「**没被我们动过的那一份**」。
        /// ⚠️ 用它是为了让写入**幂等**（见 `ClipTmpMesh` 里边那段）：乘法版本每裁一次都会再乘一遍。
        /// ⚠️ `m_ConvertToLinearSpace` 那条只动 rgb（Unity 的 `Color32.GammaToLinear` 不改 alpha）
        /// ⇒ 从 `characterInfo` 取 **alpha** 与 `colors32` 里的基准 alpha 是同一个数。</para></summary>
        static byte BaseCornerAlpha(TMPro.TMP_CharacterInfo ch, int k)
        {
            switch (k)
            {
                case 0: return ch.vertex_BL.color.a;
                case 1: return ch.vertex_TL.color.a;
                case 2: return ch.vertex_TR.color.a;
                default: return ch.vertex_BR.color.a;
            }
        }

        /// <summary>点阵兜底那条：`Label` 自己那个四边形（贴图 = 整段字的点阵图）⇒ 同一套夹法。</summary>
        static bool ClipQuadMesh(Transform tr, Mesh mesh, PxRect clip, Vector2 softPx)
        {
            var vs = mesh.vertices;
            var uvs = mesh.uv;
            if (vs == null || uvs == null || vs.Length < 4 || uvs.Length < 4) return false;
            // `Label.RebuildMesh` 的顶点序 = **BL · BR · TR · TL**（0=左下 … 3=左上）、uv = (0,0)(1,0)(1,1)(0,1)
            // ⇒ 重排成 `ClipQuad` 的口径 **BL · TL · TR · BR**（**别照抄顺序**：搞错了「左右」就成了对角平均）
            var p = new[] { vs[0], vs[3], vs[2], vs[1] };
            var uv = new[] { uvs[0], uvs[3], uvs[2], uvs[1] };
            var al = new float[4];
            if (!ClipQuad(tr, p, uv, clip, softPx, al)) return false;
            vs[0] = p[0]; vs[3] = p[1]; vs[2] = p[2]; vs[1] = p[3];
            uvs[0] = uv[0]; uvs[3] = uv[1]; uvs[2] = uv[2]; uvs[1] = uv[3];
            mesh.vertices = vs; mesh.uv = uvs; mesh.RecalculateBounds();
            return true;
        }

        /// <summary>🔴 **四边形夹进裁切框的唯一一份实现**（TMP 与点阵两条后端共用；🆕 **2026-10-11（A250）已公开**）。
        /// `p` / `uv` 都是 4 个、顺序 **BL · TL · TR · BR**；`alphaOut` 回传四角的软边 alpha
        /// （**传 `null` = 不要软边、也不回传**，硬边调用方这么用）。
        /// 返回 false = 四角一个都不用动（调用方**别回写**）。
        /// 夹完之后 uv 按**同一个仿射关系**跟着走（`u = uL + (x−xL)/(xR−xL)·(uR−uL)`，
        /// `v = vT + (y−yT)/(yB−yT)·(vB−vT)` —— 注意 uv 的 v **自下而上**、而 px 的 y 向下 ⇒ 要翻）
        /// —— ⛔ 只夹顶点不改 uv 会把纹理拉花。
        /// ⚠️ **顶点序必须先转成 BL · TL · TR · BR**（点阵那条后端就是别的序，见 `ClipQuadMesh` 里那次重排；
        /// 搞错了「左右」就成了对角平均）。
        /// <para>🔴 **A990（2026-10-18）：本函数的「读」（世界 → px）与「写」（px → 世界）必须是
        /// 【同一条换算】的**互逆**两条** —— 写的那一半只能是 `LayoutSpace.FromPixel`（= `MenuDraw.Local`
        /// 建件时用的那条，**框沿画在哪由它说了算**），所以读的那一半必须取它的逆（`PixelOfDesign`）。
        /// 原来读的是 `LayoutSpace.ToPixel`（x 写死 108px/单位）⇒ **只在 16:9 成立**；
        /// 偏量表与「为什么不能改写成那一半」→ `PixelOfDesign` 的 doc（⛔ 别把这两半改回两套）。</para></summary>
        public static bool ClipQuad(Transform tr, Vector3[] p, Vector2[] uv, PxRect clip, Vector2 softPx, float[] alphaOut)
        {
            var q = new Vector2[4];
            // 🔴 **A990（2026-10-18）**：读回走 `PixelOfDesign`（= `FromPixel` 的**逆**），⛔ 不是
            //    `LayoutSpace.ToPixel` —— 那一份的 x 写死 108px/世界单位，**只在 16:9 与「写回」那条路重合**
            //    ⇒ 非 16:9 下 x 的往返不闭合（4:3 差 0.75 倍）：框**内**的角会被误夹、框**外**的角会被放过去。
            //    判据 / 偏量表 / 「同一族还有哪些读口没收口」→ `PixelOfDesign` 的 doc。
            for (int i = 0; i < 4; i++) q[i] = PixelOfDesign(tr.TransformPoint(p[i]));
            float xL = (q[0].x + q[1].x) * 0.5f, xR = (q[2].x + q[3].x) * 0.5f;
            float yT = (q[1].y + q[2].y) * 0.5f, yB = (q[0].y + q[3].y) * 0.5f;
            float uL = (uv[0].x + uv[1].x) * 0.5f, uR = (uv[2].x + uv[3].x) * 0.5f;
            float vT = (uv[1].y + uv[2].y) * 0.5f, vB = (uv[0].y + uv[3].y) * 0.5f;
            float dx = xR - xL, dy = yB - yT;
            bool moved = false;
            for (int i = 0; i < 4; i++)
            {
                float x = Mathf.Clamp(q[i].x, clip.x1, clip.x2);
                float y = Mathf.Clamp(q[i].y, clip.y1, clip.y2);
                if (alphaOut != null)
                    alphaOut[i] = SoftAlpha(x, clip.x1, clip.x2, softPx.x) * SoftAlpha(y, clip.y1, clip.y2, softPx.y);
                if (x == q[i].x && y == q[i].y) continue;
                moved = true;
                if (Mathf.Abs(dx) > 1e-6f) uv[i].x = uL + (x - xL) / dx * (uR - uL);
                if (Mathf.Abs(dy) > 1e-6f) uv[i].y = vT + (y - yT) / dy * (vB - vT);
                float zOld = p[i].z;                    // ⚠️ `FromPixel` 给的是 **z=0 的世界平面** ⇒ 回写前把原 z 留回去
                p[i] = tr.InverseTransformPoint(LayoutSpace.FromPixel(x, y));
                p[i].z = zOld;
            }
            return moved;
        }

        /// <summary>按**原版像素矩形**摆一张图。`tex == null` = 纯色块（原版那种「没 sprite、只有 `m_Color`」的件）。</summary>
        /// <param name="keepAspect">原版 `Image.m_PreserveAspect`：按图自身宽高比放进框、**居中**（不拉伸）。</param>
        /// <param name="clip">🔴 **裁切边界**（画布像素 · 左上原点）。非空时越界部分**不画**、且 **uv 跟着截**
        /// —— 这是原版 `RectMask2D` 的等效物（滚动区画内容前给一次）。
        /// ⚠️ **不截 uv 只截矩形的话，那一格图会被压扁**（同 `ImageQuad.SetUvRect` 的注释：
        /// `SetTexture` 会把 `_aspect` 改成贴图自己的）。
        /// 🔴 2026-09-24：这段逻辑原来只在 `MenuWindowBase.Rect` 里，**这里又写一份就是两处同一条规则**
        /// ⇒ 现在**只有这一份**，`MenuWindowBase.Rect` 转调它。</param>
        /// <param name="clipSoftness">🆕 2026-10-04：**原版 `RectMask2D.m_Softness`**（画布像素：x 管左右、y 管上下；
        /// `(0,0)` = 硬边 = 本函数原来那套行为）。非 0 时按剖面**把 quad 沿渐隐带的内沿切开**并给四角上 alpha 斜坡
        /// —— 见 `ApplySoftEdges` 那一大段（判据/代价都在那儿）。⚠️ 只在 `clip` 有值时有意义。</param>
        public static ImageQuad Rect(Transform parent, Texture2D tex, PxRect r, string name, int q,
                                     Color? tint = null, bool keepAspect = false, PxRect? clip = null,
                                     Vector2 clipSoftness = default(Vector2))
        {
            if (tex == null) return null;
            // 🔴 **2026-10-12（A198② 阶段 1）：裁切状态走【一处】共用解析**（`ViewportClip.Resolve`）——
            //    优先级 = **显式形参（非空）= 旧路** > 父链上最近的 `ViewportClip` 节点 > 没有。
            //    ⚠️ 形参非空时 `RenderClip` **逐位等于**原 `clip`（`PaddedClip(·, Vector4.zero)` 首句早退）
            //    ⇒ 本壳全部设站点（今天 52 处）行为一字不变；今天也**没有任何节点**
            //    （阶段 1 不挂）⇒ 每个调用点都落在「形参」或「无」两支上（判据/共存保证 → `ViewportClip` 文件头）。
            //    🔴 渲染这一路吃的是 **`RenderClip`（= `V − pad`）**；命中那一路（`Hit` / `DeckCell`）吃的是
            //    **裸 `Clip` + `Pad`** —— 两路读的是**同一份状态**（A188 的硬约束），⛔ 别只改这一半。
            var _st = ViewportClip.Resolve(parent, clip, clipSoftness, default(Vector4));
            clip = _st.RenderClip;
            clipSoftness = _st.Softness;
            float x1 = r.x1, x2 = r.x2, y1 = r.y1, y2 = r.y2;
            if (keepAspect && tex.height > 0)
            {
                float sprAspect = (float)tex.width / tex.height, rectAspect = (x2 - x1) / Mathf.Max(1e-6f, y2 - y1);
                if (sprAspect > rectAspect) { float nh = (x2 - x1) / sprAspect, d = ((y2 - y1) - nh) * 0.5f; y1 += d; y2 -= d; }
                else { float nw = (y2 - y1) * sprAspect, d = ((x2 - x1) - nw) * 0.5f; x1 += d; x2 -= d; }
            }
            Rect uv = new Rect(0f, 0f, 1f, 1f);
            if (clip.HasValue)
            {
                // 🔴 2026-10-03：**求交那一段收口到 `ClipRect`**（`Hit` / `Nine` / `DeckCell` 共用同一份判据）——
                //    旧写法在这里内联了一遍 `Max/Min`，正是「两处写同一条规则」那一类。行为一字未改。
                float w0 = x2 - x1, h0 = y2 - y1;
                PxRect cr;
                // 整块在视口外（任一轴）⇒ 不建（也就不吃点击）—— 照原版 `RectMask2D` 的语义
                if (!ClipRect(new PxRect(x1, y1, x2, y2), clip, out cr)) return null;
                // uv 的 y 轴是**自下而上**，而 `PxRect` 是自上而下 ⇒ 上下要翻过来
                //（顶点序：0=左下 1=右下 2=右上 3=左上，`ImageQuad.RebuildMesh`；
                //  `MenuScroll.Place` 那一份只做横向，纵向这一份是新的）
                uv = new Rect((cr.x1 - x1) / w0, (y2 - cr.y2) / h0, cr.W / w0, cr.H / h0);
                x1 = cr.x1; x2 = cr.x2; y1 = cr.y1; y2 = cr.y2;
            }
            var quad = ImageQuad.Create(parent, tex, Local(parent, x1, y1, x2, y2), LayoutSpace.Px(y2 - y1),
                                        new Vector2(0.5f, 0.5f), name);
            if (quad == null) return null;
            quad.SetAspect((x2 - x1) / Mathf.Max(1e-6f, y2 - y1));
            quad.SetRenderQueue(q);
            if (uv.x > 0.0005f || uv.width < 0.9995f
                || uv.y > 0.0005f || uv.height < 0.9995f) quad.SetUvRect(uv);
            if (tint.HasValue) quad.SetTint(tint.Value);
            // 🆕 2026-10-04：软边（在 tint 之后 —— 切出来的子块要抄这份 tint；顶点色与材质色互不干扰）
            // 🔴 传进去的 `uv` 就是**这张图这一份的整张 uv**（= 「整张图 ∩ 裁切框」那个算式的结果，
            //    见上面 `uv = new Rect(...)` 那两行）—— **别改成 `quad.UvRect`**（切开后它会被写小，见 `ApplySoftEdges`）
            if (clip.HasValue && (clipSoftness.x > 0f || clipSoftness.y > 0f))
                ApplySoftEdges(quad, new PxRect(x1, y1, x2, y2), clip.Value, clipSoftness, uv);
            return quad;
        }

        /// <summary>原版 `Image.Type = Sliced`：九宫格。`border` 是**贴图像素**的四边（L,B,R,T）。
        /// 🆕 2026-10-03 加 `borderOutPx` —— **画出来的角块长**（不传 = 与 `border` 相同）。
        /// 为什么要有它：原版 `Image` 的 **`m_PixelsPerUnitMultiplier`** 会**缩放画出来的角块**
        /// （例：左栏 `Highlight` = `40k_main_bt_selected BW` 71² · `m_Border 30` · **ppuMul 0.92**
        ///  ⇒ 画出来是 **30 ÷ 0.92 = 32.61px**）。只给一个量的话，UV 切分或角块大小必有一个是错的。
        /// 判据 → `资料/每日…`（B2）与 `ImageQuad.CreateNineSlice` 的同名参数注释。
        /// <para>🆕 **2026-10-03：这条九宫格路也吃 `Clip` 了**（此前**完全没接** —— 滚动区里画九宫格
        /// 会一直画到视口外）。做法与 `Rect` **同一条判据**（`ClipRect`），但**不能只缩目标矩形**：
        /// 九宫格的**角块位置是按整块算的**，把目标矩形先缩小 ⇒ 角块会跑到视口边上（画错）。
        /// ⇒ 照原版 `RectMask2D` 的语义：**整棵树仍按原矩形建**，建完再**逐子块**截（见 `ClipNineChildren`）。
        /// ⚠️ **根节点的位置/矩形不动**（原版 `RectMask2D` 也只裁渲染、不挪 `RectTransform`）——
        ///    自检量「这一层在哪」时读到的是原矩形，不是被截过的那块。</para></summary>
        /// <param name="clip">裁切边界（画布像素 · 左上原点）。整块在框外 ⇒ **不建**（返回 null）。</param>
        /// <param name="clipSoftness">🆕 2026-10-04：软边（原版 `m_Softness`，画布像素）。非 0 时**逐子块**
        /// 再按剖面处理（`ApplySoftEdges`）—— 九块各自都可能落在渐隐带里/跨过带的内沿。同 `Rect`。</param>
        public static GameObject Nine(Transform parent, Texture2D tex, PxRect r, Vector4 border,
                                      float texW, float texH, int q, Color? tint = null, bool fillCenter = true,
                                      string name = "Nine", Vector4? borderOutPx = null, PxRect? clip = null,
                                      Vector2 clipSoftness = default(Vector2))
        {
            if (tex == null) return null;
            // 🔴 **2026-10-12（A198② 阶段 1）**：取裁切状态走同一份解析（同 `Rect` 的那一段，判据在 `ViewportClip`）。
            var _st = ViewportClip.Resolve(parent, clip, clipSoftness, default(Vector4));
            clip = _st.RenderClip;
            clipSoftness = _st.Softness;
            PxRect vis;
            if (!ClipRect(r, clip, out vis)) return null;          // 整块在视口外 ⇒ 连节点一起不建
            bool partial = clip.HasValue && !SameRect(vis, r);     // 部分越界 ⇒ 建完要逐子块截
            var go = ImageQuad.CreateNineSlice(parent, tex, border, texW, texH,
                                               Local(parent, r.x1, r.y1, r.x2, r.y2),   // ⚠️ 用**整块**矩形建
                                               LayoutSpace.Px(r.W), LayoutSpace.Px(r.H), name,
                                               borderOutPx ?? border, fillCenter);
            if (go == null) return null;
            foreach (var q2 in go.GetComponentsInChildren<ImageQuad>())
            {
                if (tint.HasValue) q2.SetTint(tint.Value);
                q2.SetRenderQueue(q);
            }
            if (partial) ClipNineChildren(go, clip.Value);
            if (clip.HasValue && (clipSoftness.x > 0f || clipSoftness.y > 0f))
            {
                // 🔴 **逐子块**上软边（顺序在 `ClipNineChildren` **之后** —— 那一趟会挪/缩子块，
                //    先上软边的话切开的位置就作废了）。
                foreach (var q2 in go.GetComponentsInChildren<ImageQuad>(true))
                {
                    if (q2 == null || !q2.gameObject.activeSelf) continue;   // 整块在框外的那几块已被关掉
                    // ⚠️ `uv0` = **这个子块自己的整张 uv**（建好那一刻读出来、之后不再读）——
                    //    切完它会变成「主格那一小块」，见 `ApplySoftEdges` 的 `uv0` 参数注释
                    ApplySoftEdges(q2, QuadRectPx(q2), clip.Value, clipSoftness, q2.UvRect);
                }
            }
            return go;
        }

        /// <summary>**九宫格的部分越界**处理：把每一块子 quad 截到框内（`Nine` 里 `partial` 时才走）。
        /// 整块在框外的子件直接 `SetActive(false)` —— **不删节点**（树形/子块个数与未裁切时一致，
        /// 自检按名字找得到；`PointerLayer` 本来就会跳过未激活的）。
        /// <para>每一块自己的 uv 里对应「裁剩的那段」：子块的 uv 底边 ↔ 它的**下边**（`ImageQuad.RebuildMesh`
        /// 顶点序 0=左下 ⇒ `vMin` 在下），而 `PxRect` 自上而下 ⇒ 纵向要翻一次。</para>
        /// ⚠️ 平铺（`Tiled`）那条同族见 **`ClipTiledChildren`** —— 形状与这里一样，**只有 uv 的换算基准不同**
        /// （平铺按**节距**、不按本块宽高）。改这一份时**顺手看一眼那一份**。
        /// ⚠️ 世界↔像素只走 `LayoutSpace.ToPixel/FromPixel`（别在别处再乘 108）。</summary>
        static void ClipNineChildren(GameObject root, PxRect clip)
        {
            // 🔴 **2026-10-11（A294）**：根的**世界**位置先换算进设计空间（`PosInDesignSpace`）——
            //    下面那句写进 `q.localPosition` 的 `FromPixel(...) − 根位置` 与 `Local` 是**同一个形状**，
            //    原来同样少除一次父链缩放。`k == 1`（开关出厂关）时**逐位相同**。
            //    ✅ **2026-10-11（A298）：另一半也修了** —— 本函数的 `QuadRectPx(q)`（「世界 → px」的反向映射）
            //    原来在 `k ≠ 1` 时给的是**放大过的那份 px**、与设计 px 的 `clip` 求交 ⇒ 见 `QuadRectPx` 的注释
            //    （判据 / 为什么与 `Local` 恰为逆运算 / 尺寸项为什么不动）。`k == 1` 时两半都**逐位相同**。
            var rootDesign = PosInDesignSpace(root.transform);
            foreach (var q in root.GetComponentsInChildren<ImageQuad>(true))
            {
                if (q == null) continue;
                // 子块中心 = 它自己的世界位置（网格以 transform 为中心、锚 (.5,.5)，见 `RebuildMesh`）
                // 🆕 2026-10-04：这一段（世界 → px 反推矩形）收口到 `QuadRectPx`（软边那条路也用它）
                var qr = QuadRectPx(q);
                PxRect cr;
                if (!ClipRect(qr, clip, out cr)) { q.gameObject.SetActive(false); continue; }   // 这一块整块在框外
                if (SameRect(cr, qr)) continue;                                                 // 整块在框内 ⇒ 不动
                var uv = q.UvRect;
                float w = Mathf.Max(1e-6f, qr.W), h = Mathf.Max(1e-6f, qr.H);
                float u1 = uv.x + uv.width * (cr.x1 - qr.x1) / w;
                float u2 = uv.x + uv.width * (cr.x2 - qr.x1) / w;
                float v1 = uv.y + uv.height * (qr.y2 - cr.y2) / h;      // uv 的 y 自下而上 ⇒ 翻
                float v2 = uv.y + uv.height * (qr.y2 - cr.y1) / h;
                var lp = LayoutSpace.FromPixel(cr.CX, cr.CY) - rootDesign;   // 🆕 A294：根位置走设计空间
                lp.z = q.transform.localPosition.z;                     // z 不动（同队列里还靠它排序）
                q.transform.localPosition = lp;
                q.SetWorldHeight(LayoutSpace.Px(cr.H));
                q.SetAspect(cr.W / Mathf.Max(1e-6f, cr.H));
                q.SetUvRect(new Rect(u1, v1, u2 - u1, v2 - v1));
            }
        }

        /// <summary>**平铺的部分越界**处理（`Tiled` 里 `partial` 时才走）—— `ClipNineChildren` 的同族：
        /// 形状一模一样（整块在框外的子件 `SetActive(false)`、**不删节点**；部分越界的那块**挪 + 缩 + 截 uv**，
        /// 挪缩那四行也逐字相同）。**要动脑的只有下面那两行 uv**。
        /// <para>🔴 **规则**：平铺的每一块 u 方向是**重复采样**，几何 1:1 对应的**不是一个「块」而是一个「节距」**
        /// ⇒ uv 的增量按**节距**换算（`Δuv = Δpx ÷ 节距`）。
        /// 判据 = 原版 UGUI `Image.GenerateTiledSprite`
        /// （`Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/Image.cs:1352-1361`）：
        /// 末列那块写的是 `clipped.x = uvMin.x + (uvMax.x − uvMin.x) · (xMax − x1) / (x2 − x1)` ——
        /// **分母 `x2 − x1` 就是整格宽 `tileWidth`**（:1354-1355 的 `x1/x2`）、分子是「离本格左边的距离」
        /// ⇒ 就是「按节距截 uv」。
        /// ⚠️ 更关键的一条：原版 **`RectMask2D` 根本不改这份 mesh**（掩码在 shader 里裁像素）
        /// ⇒ **平铺的相位不因裁切而变**：格子零点仍钉在矩形的左下角，被裁掉的只是像素。
        /// 我们「截几何 + 截 uv」是它的等效物 ⇒ **相位必须照原样留** ——
        /// ⛔ 别把平铺重新对齐到视口边（那会让花纹在视口边上错位），
        /// ⛔ 也别把 uv 拉成整张 `(0,0,1,1)`（那会把被裁剩的一小块花纹**压扁**）。</para>
        /// <para>节距从**子块自己**反推，不去碰调用方那个 `tilePx`：`CreateTiled` 给每一块设的 uv 就是
        /// `(0, 0, 本块世界宽/节距, 本块世界高/节距)`（`Battle/ImageQuad.cs` 的 `CreateTiled`）
        /// ⇒ **`节距 = 本块几何宽 ÷ 本块 uv 宽`**，对**末格被截短**的那一块同样成立（它的宽与 uv 宽同比例缩过）。
        /// ⚠️ **节距走「本块几何宽 ÷ 本块 uv 宽」反推，不直接拿调用方那个 `tilePx`**。
        /// 🔴 **2026-10-05 就地订正**：上面这条做法的**理由过期了** —— 旧注释写的是
        /// 「`CreateTiled` 内部按 `ImageQuad.PixelsPerUnit`（**100**）折世界尺寸，而画布是 **108** ⇒ 差 8%，
        /// 花纹在裁切处被缩放 1.08」。**那个 100 是不对的**：该常量在 **2026-10-03** 已经改成
        /// `LayoutSpace.DesignPxH / DesignHeight` = **108**（见它自己的注释：写成推导式就是为了「换算只有一处」）
        /// ⇒ 今天 `tilePx` 与画布 px 是**同一个口径**、比值恒 1，**那 8% 不存在**。
        /// 仍然保留「反推」这个写法，是因为它**一个常量都不依赖**（末格被裁短的那块照样成立）。
        /// 📌 **亲算过（2026-10-03）**：按上面这条定义，本式的 `Δuv` 与 `ClipNineChildren` 那条
        /// `uv.width · Δpx / qr.W` **数值上恒等**（因为 `uv.width ≡ qr.W ÷ 节距`）。
        /// 所以这里**不是**「另一套公式」，而是同一个斜率的另一种写法 —— 写成「÷节距」是为了**直接对着判据读**，
        /// 且对**末格那块**（`qr.W ≠ 节距`）一眼能看明白为什么不能拿本块宽当分母。</para>
        /// ⚠️ 纵轴同 `ClipNineChildren`：uv 的 y **自下而上**、而 `PxRect` 自上而下 ⇒ 翻一次。</summary>
        static void ClipTiledChildren(GameObject root, PxRect clip)
        {
            // 🔴 **2026-10-11（A294）**：同 `ClipNineChildren` 的同一条改动（同一个形状、同一个病根）——
            //    根的**世界**位置先换算进设计空间；`k == 1` 时逐位相同。
            //    ✅ **2026-10-11（A298）**：那里那句「`QuadRectPx` 的反向映射仍没修」**已作废** ——
            //    反向那一半本批一起修了（同一份 `PosInDesignSpace` 口径），见 `QuadRectPx` 的注释。
            var rootDesign = PosInDesignSpace(root.transform);
            foreach (var q in root.GetComponentsInChildren<ImageQuad>(true))
            {
                if (q == null) continue;
                var qr = QuadRectPx(q);          // 同上：矩形反推只此一份
                PxRect cr;
                if (!ClipRect(qr, clip, out cr)) { q.gameObject.SetActive(false); continue; }   // 这一块整块在框外
                if (SameRect(cr, qr)) continue;                                                 // 整块在框内 ⇒ 不动
                var uv = q.UvRect;
                // 🔴 节距（画布 px）= 本块几何宽 ÷ 本块 uv 宽 —— 见上面第 2 段（末格截短的那块也是这个数）
                float pitchX = qr.W / Mathf.Max(1e-6f, uv.width);
                float pitchY = qr.H / Mathf.Max(1e-6f, uv.height);
                float u1 = uv.x + (cr.x1 - qr.x1) / pitchX;
                float u2 = uv.x + (cr.x2 - qr.x1) / pitchX;
                float v1 = uv.y + (qr.y2 - cr.y2) / pitchY;      // uv 的 y 自下而上 ⇒ 翻
                float v2 = uv.y + (qr.y2 - cr.y1) / pitchY;
                var lp = LayoutSpace.FromPixel(cr.CX, cr.CY) - rootDesign;   // 🆕 A294：根位置走设计空间
                lp.z = q.transform.localPosition.z;                     // z 不动（同队列里还靠它排序）
                q.transform.localPosition = lp;
                q.SetWorldHeight(LayoutSpace.Px(cr.H));
                q.SetAspect(cr.W / Mathf.Max(1e-6f, cr.H));
                q.SetUvRect(new Rect(u1, v1, u2 - u1, v2 - v1));
            }
        }

        /// <summary>原版 `Image.Type = Tiled`：按贴图原始尺寸重复铺。
        /// 🆕 **2026-10-03：这条路现在也吃 `Clip` 了** —— ⚠️ **就地订正**：同一天的旧注释写着
        /// 「这条路**仍不吃** `Clip`（本轮只把九宫格那条接上）」，**那是上一批的状态、已过期**（当时只改了注释没接线）。
        /// 接法与 `Nine` **完全同一套**：`ClipRect` 判整块 ⇒ 不建（返回 null）；部分越界 ⇒
        /// **照原矩形建完再逐子块截**（见 `ClipTiledChildren`，那里写着「平铺为什么不能只缩矩形」）。
        /// ⚠️ **5 处调用（`DuelPopupWindow:129` · `ProfileTab:683` · `RankedEventWindow:64` ·
        /// `SearchingMatchPopup:175` · `SettingsWindow:770`）都没传 `clip`** —— 它们全在弹窗 / 页头、
        /// 没有一个在滚动视口内 ⇒ 加了这道能力之后**它们的画面与点击一字未变**（要接就在调用处补一个 `Clip` 实参）。</summary>
        /// <param name="clipSoftness">🆕 2026-10-04：软边（原版 `m_Softness`）。非 0 时**逐格**按剖面处理
        /// （`ApplySoftEdges`）。⚠️ 平铺的**相位不因裁切/软边而变**（见 `ClipTiledChildren` 第 2 段）——
        /// 子块被切开只影响几何与顶点色，格子零点仍钉在矩形左下角。</param>
        public static GameObject Tiled(Transform parent, Texture tex, PxRect r, float tilePx, int q, string name,
                                       Color? tint = null, PxRect? clip = null,
                                       Vector2 clipSoftness = default(Vector2))
        {
            if (tex == null) return null;
            // 🔴 **2026-10-12（A198② 阶段 1）**：取裁切状态走同一份解析（同 `Rect` 的那一段，判据在 `ViewportClip`）。
            var _st = ViewportClip.Resolve(parent, clip, clipSoftness, default(Vector4));
            clip = _st.RenderClip;
            clipSoftness = _st.Softness;
            PxRect vis;
            if (!ClipRect(r, clip, out vis)) return null;          // 整块在视口外 ⇒ 连节点一起不建
            bool partial = clip.HasValue && !SameRect(vis, r);     // 部分越界 ⇒ 建完要逐子块截
            var go = ImageQuad.CreateTiled(parent, tex, tilePx, tilePx,
                                           Local(parent, r.x1, r.y1, r.x2, r.y2),   // ⚠️ 用**整块**矩形建
                                           LayoutSpace.Px(r.W), LayoutSpace.Px(r.H), name);
            if (go != null)
            {
                foreach (var q2 in go.GetComponentsInChildren<ImageQuad>())
                {
                    // 🔴 **tint 不能丢**：活动窗的 `Noise` 原版是 col(0.311,0.127,0,0.718)，
                    //    不染就是**全白全不透明**一条 —— 2026-09-24 找茬子代理抓到的。
                    if (tint.HasValue) q2.SetTint(tint.Value);
                    q2.SetRenderQueue(q);
                }
                if (partial) ClipTiledChildren(go, clip.Value);
                if (clip.HasValue && (clipSoftness.x > 0f || clipSoftness.y > 0f))
                    foreach (var q2 in go.GetComponentsInChildren<ImageQuad>(true))
                    {
                        if (q2 == null || !q2.gameObject.activeSelf) continue;
                        // ⚠️ 同 `Nine`：`uv0` = 这个子块自己的整张 uv（切之前读一次）
                        ApplySoftEdges(q2, QuadRectPx(q2), clip.Value, clipSoftness, q2.UvRect);
                    }
            }
            return go;
        }

        /// <summary>按原版 TMP 的 `m_fontSize`（画布像素）摆一段字。
        /// `wrapPx > 0` ⇒ **限宽换行**（原版 `m_TextWrappingMode = 1`）；`autoMinPx > 0` ⇒ 开自适应。
        /// 🔴 **别用 `SetFontSize(px/108)`** —— 那会大 2.7 倍；走 `SetGlyphHeight`。
        ///
        /// <para>🔴 **2026-10-12（A333）：`autoMaxPx` = 原版那一颗的 `m_fontSizeMax`**（画布 px，与
        /// `autoMinPx` 同量纲）。**`&lt;= 0` ⇒ 旧行为**（上限 = `fontPx`）。
        /// 为什么必须单列一个形参：原版的 `m_fontSizeMax` **不一定等于** `m_fontSize` ——
        /// 旧写法把 `fontPx` 当上限 ⇒ **短文案永远画小一档**。
        /// 判据（逐站实读，不是通则）：`CacheTab/Title` `m_fontSize 31.75` / `max 35` ·
        /// `Points` `34.8` / `max 40` · `Match Log/Result` `65.35` / `max 75` · `Score` `46.25` / `max 50` ·
        /// `skullCounter` `30.2` / `max 45`（`资料/普查产出_1011/V7_A305_A304_普查.md` §一 / §三·1）。
        /// ⚠️ 交叉反证：拿我们传的 `(min,max)` 去全库查原版 TMP 的 `(m_fontSizeMin, m_fontSizeMax)`，
        /// 有 3 个档**原版全库一个对象都没有**（`(25,31.75)` · `(18,34.8)` · `(7,31.9)`）</para>
        ///
        /// <para>🔴 **2026-10-12（A336②）：`autoBasePx` = 原版那一颗的 `m_fontSizeBase`**（同量纲）。
        /// **`&lt;= 0` ⇒ 旧行为**（base = 调用方那一档）。它只影响自适应的**二分起点**
        /// （`TextMeshPro.cs:2148-2149` `Clamp(m_fontSizeBase, min, max)`），终点两侧都收敛到
        /// 「装得下的最大号」⇒ 渲染差 ≤ 0.05 fontSize 单位。判据 = `Battle/Label.cs:549-570` 那段
        /// （`m_fontSizeBase` 全库 200+ 种取值、`36.0` 是 TMP 出厂默认 ⇒ **只能逐站现读**）。</para>
        ///
        /// <para>🔴 **2026-10-13（A781）：补上【裁切】那半边。** 本方法原来**没有 `clip` 形参、
        /// 也不走 `ViewportClip.Resolve`** ⇒ 视口里那一行字**一个顶点都不裁**（容器滚到一半时字会溢出
        /// 视口画出来），而**同一格的图**（`MenuDraw.Rect` / `Nine`）与命中区（`Hit`）**已经被裁** —— 半拉子。
        /// 判据 = **原版 `RectMask2D` 对文字与图片一视同仁**（见 `ClipText` 上面那一大段）；
        /// 形状逐字照同一族那几个静态件（`Rect` / `Nine` / `Tiled` / `Hit` 都是「签名末尾两个可选形参
        /// + 一句 `ViewportClip.Resolve`」）：**新形参加在末尾（可选 ⇒ 旧调用点一字不改）**、
        /// **取状态走【一处】共用解析**（三段优先级 → `ViewportClip.Resolve`）。
        /// ⇒ `clip == null` 且父链上没有节点时落 `Resolve` 第 3 支 + `ClipText` 首句早退 ⇒ **逐位不变**。
        /// ⚠️ 裁切必须是**最后一步**（`SetGlyphHeight` / `SetAutoFitBox` 任何一次重排都会把 mesh 重算回去）。
        /// ✅ **2026-10-14（A798）就地订正（铁律 5）：那道闸【加上了】。** 本段原来写的是
        /// 「本方法**不做**「整块在视口外 ⇒ 不建」那道闸（`MenuWindowBase.Text` 在它自己那一层做）……
        /// 那一条差异**仍然开着**（另立账），⛔ 别在这儿顺手加」—— **已过期**：A798 那笔账落地，
        /// 闸就加在**本方法第一句**（`if (!Visible(r, _st.RenderClip)) return null;`），
        /// 与同族四个静态件（`Rect` / `Nine` / `Tiled` / `Hit`）同一条判据。当时列的两条理由逐条核过：
        /// ① 「加闸会把返回契约从『总有标签』改成『可能是 `null`』（全仓 200 个调用点）」——
        ///    契约**本来就是「可能 `null`」**（上一句 `if (lb == null) return null` 早在），
        ///    且 A798 把调用点**逐个数过**：**199 个**（`Text` 146 / `TextBox` 53）、其中 **141 个赋值点里
        ///    `0` 处**在解引用前无守卫（返回值被丢弃 / 交给自带 null 挡的 `MenuDraw.AlignLeft` /
        ///    先 `if (… != null)` 再解引用）⇒ **改 0 个调用点**；
        /// ② 「只裁不建在画面上等价」—— **这一条是对的** ⇒ 本闸的**语义代价 = 0 画面差**
        ///    （整块在框外 ⇒ 今天也被 `ClipText` 夹成零面积、画不出像素，同原版被掩码裁掉）；
        ///    收益是**与同族那四个一致**（框外 ⇒ 连节点都不建，省一次 `Label.Create` 的网格）。
        /// ⚠️ **残留风险（不是 NRE，A798 已如实记）**：返回 `null` 会让「拿 label 的 `transform`
        /// 当父件」的调用点落到**兜底分支**（例 `Shell/DailyStreakPopup.cs` 那处）⇒ **层级 / 节点名会变**
        /// （那些点都有守卫，不炸）。判据全文 → `资料/普查产出_1014/RO_文字半边与压暗层.md` §一。</para></summary>
        /// <param name="clip">裁切边界（画布像素 · 左上原点）。**非空 = 显式覆盖**（旧路赢、连父链都不走）；
        /// `null` = 由父链上最近的 `ViewportClip` 节点说了算（没有节点 ⇒ 不裁，= 旧实现逐位相同）。</param>
        /// <param name="clipSoftness">= 原版 `RectMask2D.m_Softness`（画布像素：x 管左右 / y 管上下）；同 `Rect`。</param>
        public static Label Text(Transform parent, PxRect r, string text, Color color, string name,
                                 float fontPx, int q, float wrapPx = 0f, float autoMinPx = 0f,
                                 float autoMaxPx = 0f, float autoBasePx = 0f,
                                 PxRect? clip = null, Vector2 clipSoftness = default(Vector2))
        {
            var _st = ViewportClip.Resolve(parent, clip, clipSoftness, default(Vector4));
            // 🔴 **2026-10-14（A798）：整块在框外 ⇒ 连节点一起不建**（同 `Rect` / `Nine` / `Tiled` / `Hit`
            //    那四条 —— 它们都在各自的头几行写着「整块在视口外 ⇒ 连节点一起不建」；
            //    本方法与 `TextBox` 是**唯一漏掉的一族**）。
            //    ⛔ **判「整块在框外」用 `Visible`、不是 `ClipRect`**：`ClipRect` 多一条「退化矩形（宽或高 ≤ 0.01）
            //    在有裁切时一律判不可见」的守卫，那是给**图**那一路的（那种尺寸建不出 quad）；
            //    文字建得出来，且 `Visible` 的注释写着它就是给「建不出几何/uv 的那些件」用的。
            //    ⚠️ 无裁切时 `_st.RenderClip` 是 `null` ⇒ `Visible` 首句返回 `true` ⇒ **行为逐位不变**
            //    （`PaddedClip(null, ·)` 首句早退，见 `ViewportClip.ClipState.RenderClip`）。
            //    ⚠️ 位置必须在 `TextCore` **之前**：建完再返回 `null` = 那个节点留在树里没人管（漏节点）。
            if (!Visible(r, _st.RenderClip)) return null;
            var lb = TextCore(parent, r, text, color, name, fontPx, q, wrapPx, autoMinPx, autoMaxPx, autoBasePx);
            if (lb == null) return null;
            // 🔴 A781：最后一步才裁。交给 `ClipText` 的是**调用方原样那一份 `clip`**
            //    （⛔ **不是** `_st.RenderClip` 那份解析后的快照）—— 同 `MenuWindowBase.Text` / A484：
            //    `ClippedTextGuard` 要能在**节点挪动之后重新解析**，存快照会拿旧框重裁。
            if (_st.RenderClip.HasValue) ClipText(lb, clip, clipSoftness);
            return lb;
        }

        /// <summary>`Text` / `TextBox` 的**内层**：只建 + 定字号/换行，**不裁**。
        /// 🔴 **为什么要有它**：两个公开入口都必须在**各自把版面定完之后**才落最后一刀，而 `TextBox`
        /// 比 `Text` 多两步（`SetWrapWidth` + `SetAutoFitBox`）⇒ 内层若自己裁就成了「先裁一刀、再重排、
        /// 再裁第二刀」：既白做一次，又会让 `TextClipUnavailable` / `TextClipUploadSkipped` 这两个
        /// 诊断计数**虚高**（它们只在 `ClipText` 真跑起来之后才数）。
        /// ⚠️ **2026-10-15（A821）**：包装器那一层同样的「第二刀」**不再是**计数虚高的来源了
        /// （计数改成一颗标签只记一次 —— 见 `Label.TakeClipFailMark`），但**本内层的拆法照旧**：
        /// 它免掉的是那一刀**本身**（白做一次 + 被重排冲掉），不只是计数。
        /// ⚠️ 形参表 = `Text` 原来那十个（含 `wrapPx`），**行为逐字等于拆出来之前那一版** ——⛔ 别在这里加裁切。</summary>
        static Label TextCore(Transform parent, PxRect r, string text, Color color, string name,
                              float fontPx, int q, float wrapPx, float autoMinPx, float autoMaxPx, float autoBasePx)
        {
            var lb = Label.Create(parent, text, Local(parent, r.x1, r.y1, r.x2, r.y2), 5, color,
                                  new Vector2(0.5f, 0.5f), name);
            if (lb == null) return null;
            lb.SetRenderQueue(q);
            if (fontPx > 0f) lb.SetGlyphHeight(LayoutSpace.Px(fontPx));
            if (wrapPx > 0f)
            {
                lb.SetWrapWidth(LayoutSpace.Px(wrapPx));
                if (autoMinPx > 0f && fontPx > autoMinPx)
                    // 🔴 A333：上限取**原版 `m_fontSizeMax`**（`autoMaxPx <= 0` 才退回 `fontPx` = 旧行为）
                    lb.SetAutoFitBox(LayoutSpace.Px(wrapPx), LayoutSpace.Px(r.H), autoMinPx,
                                     autoMaxPx > 0f ? autoMaxPx : fontPx, autoBasePx);
            }
            return lb;
        }

        /// <summary>限宽换行 + 可选自适应字号的一段文字（原版 `m_TextWrappingMode = 1` + autosize）。
        /// 🔴 **别用 `SetFontSize(px/108)`** —— 那会大 2.7 倍；`Text` 走的是 `SetGlyphHeight`。
        /// 🔴 2026-09-24 从 `MainMenuSubmenuWindow.TextBox` 收口过来（那边**转调**，行为一字未改）。
        /// <para>🔴 **2026-10-12（A333 + A336②）：`autoMaxPx` / `autoBasePx` 与 `Text` 同义** ——
        /// 原版那一颗的 `m_fontSizeMax` / `m_fontSizeBase`（画布 px）；**都 `&lt;= 0` ⇒ 旧行为**
        /// （上限 = `fontPx`、base = 调用方那一档）。判据全文 → `Text` 的注释。</para>
        /// <para>🔴 **2026-10-13（A781）：与 `Text` 同批补上 `clip` / `clipSoftness` 两个可选形参**
        /// （同族的 `Rect` / `Nine` / `Tiled` / `Hit` 早就有了 —— 这两个是漏的）。
        /// 判据、形参语义、以及「整块在外 ⇒ 连节点一起不建」那道闸（2026-10-14 · A798 起**两个入口都有**）
        /// → `Text` 的注释最后两段（⛔ 别抄第二份）。
        /// ⚠️ 与 `Text` 的唯一区别：本方法多了 `SetWrapWidth` + `SetAutoFitBox` 两步**重排** ⇒
        /// 裁切落在**它们之后**（内层走 `TextCore`，⛔ 不是 `Text`）。</para></summary>
        /// <param name="clip">同 `Text`：**非空 = 显式覆盖**（旧路赢）；`null` = 父链上最近的 `ViewportClip` 说了算。</param>
        /// <param name="clipSoftness">= 原版 `RectMask2D.m_Softness`（画布像素）；同 `Rect`。</param>
        public static Label TextBox(Transform parent, PxRect r, string text, Color color, string name,
                                    float fontPx, float autoMinPx = 0f, int q = QText,
                                    float autoMaxPx = 0f, float autoBasePx = 0f,
                                    PxRect? clip = null, Vector2 clipSoftness = default(Vector2))
        {
            var _st = ViewportClip.Resolve(parent, clip, clipSoftness, default(Vector4));
            // 🔴 **2026-10-14（A798）：与 `Text` 逐字同一条闸**（判据 / 为什么用 `Visible` 而不是 `ClipRect` /
            //    为什么必须在建节点之前 → `Text` 里那一段，⛔ 别抄第二份）。
            //    ⚠️ 与 `Text` 的唯一区别：本方法的**重排**（`SetWrapWidth` / `SetAutoFitBox`）在闸**之后** ——
            //    那道闸判的是**调用方给的 `r`**（重排不改 `r`）⇒ 判一次就够，不必等重排完再判一次。
            if (!Visible(r, _st.RenderClip)) return null;
            // ⚠️ A781：内层走 `TextCore`（**不裁**）—— 裁切必须落在下面 `SetWrapWidth` / `SetAutoFitBox`
            //    **之后**（那两步会重排 mesh、把上一刀抹掉）。⛔ 别改回 `Text(...)`：那会先裁一刀、
            //    被重排冲掉、再裁第二刀（白做 + 两个诊断计数虚高）。
            var lb = TextCore(parent, r, text, color, name, fontPx, q, 0f, 0f, 0f, 0f);
            if (lb == null) return null;
            lb.SetWrapWidth(LayoutSpace.Px(r.W));
            if (autoMinPx > 0f && fontPx > autoMinPx)
                // 🔴 A333：上限取**原版 `m_fontSizeMax`**（`autoMaxPx <= 0` 才退回 `fontPx` = 旧行为）
                lb.SetAutoFitBox(LayoutSpace.Px(r.W), LayoutSpace.Px(r.H), autoMinPx,
                                 autoMaxPx > 0f ? autoMaxPx : fontPx, autoBasePx);
            // 🔴 A781：最后一步才裁（形参交**调用方原样那一份**，理由 → `Text` 里那三行）
            if (_st.RenderClip.HasValue) ClipText(lb, clip, clipSoftness);
            return lb;
        }

        /// <summary>文字队列的默认档（原版那批件的文字在同一档里；`MainMenuSubmenuWindow` 用的是 3011）。</summary>
        public const int QText = 3011;

        /// <summary>**左对齐**到 `r` 的左边缘。原版这批 TMP 实测多为 `m_HorizontalAlignment = 1 (Left)`
        /// （`Label` 默认把文字块**居中**放在锚点上，不对齐就会与右对齐的件叠字）。</summary>
        public static void AlignLeft(Label lb, PxRect r)
        {
            if (lb != null) lb.AlignLeftOn(LayoutSpace.FromPixel(r.x1, 0f).x);
        }

        /// <summary>**右对齐**到 `r` 的右边缘。</summary>
        public static void AlignRight(Label lb, PxRect r)
        {
            if (lb != null) lb.AlignRightOn(LayoutSpace.FromPixel(r.x2, 0f).x);
        }

        /// <summary>🆕 **2026-10-15（A712）**：把一段文字设成原版那一颗的**垂直档**
        /// （= 原版 TMP 的 `m_VerticalAlignment`：`Middle` / `Capline` / `Midline` / `Bottom` / `Top`）。
        /// 🔴 **本层只转发** —— 档位 → 位移的那条换算**只有一份**，在 `Battle/Label.cs` 的 `SetVAlign`
        /// 里（推导 + 逐档算式 + 为什么不能用 TMP 自带的 `alignment` → 那里的注释，⛔ 别在这儿再写一份）。
        ///
        /// <para>**为什么要有这个口**（而不是让调用方自己写 `lb.SetVAlign(...)`）：`Label` 收的框高是
        /// **世界单位**，而 Shell 这一层的矩形一律是**画布 px**（`PxRect`）—— px↔世界单位那条换算与
        /// 上面 `AlignLeft`/`AlignRight` 走**同一份**（`LayoutSpace`），别在每个调用点各写一次。</para>
        /// <para>⚠️ **不调本函数 = `Middle`**（`Label` 的出厂档）⇒ 本阶段全工程一个调用点都不用改，
        /// `Middle` 的字墨校正照常生效；本函数是给**第二阶段**那 1/3 非 `Middle` 的件用的。
        /// 判据（哪一颗原版是哪个档、用的哪份字体）→ `资料/普查产出_1013/WA712_垂直对齐普查.md` §二/§三
        /// （生产代码里已有的 80 行注释 / 24 个文件就是现成清单）；⛔ 别按「看着像居中」猜。</para>
        /// <para>⚠️ 排在**重排之后**才稳（`SetWrapWidth` / `SetAutoFitBox` / `SetText` 任何一次重排都会
        /// 重跑 `RefreshBounds`，而它每次都按当前档重算 —— 幂等，所以先设档也不会被冲掉；
        /// 但**框高**若走 `SetAutoFitBox` 那条兜底（不传本函数的 `r` 也一样），就要等它先被调过）。</para></summary>
        public static void SetVAlign(Label lb, Label.VAlign tier, PxRect r,
                                     Label.OrigFace face = Label.OrigFace.Pragati)
        {
            if (lb != null) lb.SetVAlign(tier, LayoutSpace.Px(r.H), face);
        }

        /// <summary>一个**透明点击区**（整块矩形）+ `WindowButton`，返回那个节点。
        /// 原版这一层就是按钮自己的 `RectTransform`；我们这套没有 uGUI 事件 ⇒ 单独一个透明 quad 当命中区
        /// —— **`PointerLayer` 扫的就是它**（`GetComponentInChildren&lt;ImageQuad&gt;()` 拿矩形）。
        /// 🔴 2026-09-24：这段原来只有 `MainMenuSubmenuWindow.AddHit` 一份，新的活动窗/搜索弹窗也要
        /// ⇒ 收口到这里，那边**转调**（CLAUDE.md §三：两处写同一条规则 = 迟早不一致）。
        /// 🆕 **2026-10-03（A17）**：加 `target` / `art` / `hoverArt` / `pressedArt` —— 悬停**换图**
        /// （原版 `SpriteSwap` 那 630 颗）。⚠️ **`Hit` 节点上没有常态图**（我们是「画底 + 建透明命中区」
        /// 两步走）⇒ **高亮图必须由调用方把「画底那个 quad」和「常态图名」传进来**；
        /// 不传 = 这一颗维持原样（色偏兜底），**原版是 ColorTint / `trans=0` 的那些就该不传**。
        /// <para>🆕 **2026-10-03：`clip` —— 视口外的命中区不建、压在视口边上的截到视口内**。
        /// 判据 = 原版 `RectMask2D` 的**射线那一面**（`IsRaycastLocationValid`：框外的点判不中任何东西，
        /// 见 `ClipRect` 的注释）。⚠️ 本工程**从前没有这道守卫**（老注释里那句「Clip 生效时视口外的
        /// 点击区不会被建」是**写错了**）⇒ 那时滚出视口的条目**照样能点**（点上还是看不见的那一件）。
        /// 🆕 **2026-10-04：本函数【故意】不收 `clipSoftness`** —— 原版的软边只改**渲染**
        /// （掩码在 shader 里削 alpha），**射线那一面只看矩形**（`IsRaycastLocationValid` 用的是
        /// 未削软的 `rectTransform` + `m_Padding`）⇒ 命中区**不许**跟着缩。别顺手给它加上。</para></summary>
        public static Transform Hit(Transform parent, string name, PxRect r, int q, System.Action onClick,
                                    ImageQuad target = null, string art = null,
                                    string hoverArt = null, string pressedArt = null, PxRect? clip = null,
                                    Vector4 maskPad = default(Vector4))
        {
            // 🔴 **2026-10-12（A198② 阶段 1）**：取裁切状态走同一份解析（`ViewportClip.Resolve`）。
            //    ⚠️ 命中这一路要的是【**裸**框 + `pad`】—— pad 由**下面那一句** `PaddedClip(clip, maskPad)` 缩
            //    （= 原版 `R ∩ (V − pad)`）⇒ ⛔ **别把 `RenderClip` 那一份喂进来**，那会把 pad 缩两次。
            //    ⚠️ 软边**故意**不进这一路（原版射线只看 `rectTransform` + `m_Padding`，见上面那段判据）
            //    ⇒ 解析时给 `Vector2.zero`，也别把它接到 `maskPad` 上。
            var _st = ViewportClip.Resolve(parent, clip, default(Vector2), maskPad);
            clip = _st.Clip;
            maskPad = _st.Pad;
            PxRect hr;
            // ============================================================ 命中区 = `R ∩ (V − pad)`
            //
            // 🔴 **本参数缩的是 `clip`（= 原版 mask 自己那个框），⛔ 不是命中区自己的矩形** ——
            //    名字也一样分家：**`maskPad` = `RectMask2D.m_Padding`**，`PaddedHitRect` 那份 pad
            //    是**另一个字段**（`Graphic.m_RaycastPadding`，唯一使用者 = `Shell/DeckInfoPopup.cs` 的 `WarlordPad`）。
            //
            // 🔴 **判据（本地 uGUI 源码，两关都要过）—— 2026-10-08（A188）就地订正（铁律 5）**：
            //    · `GraphicRaycaster.Raycast`（`…/UI/Core/GraphicRaycaster.cs:327`）=
            //      `RectangleContainsScreenPoint(graphic.rectTransform, pointerPosition, eventCamera, graphic.raycastPadding)`
            //      ⇒ **图形自己的 rect**（本颗按钮的 `m_RaycastPadding = (0,0,0,0)`）；
            //    · `RectMask2D.IsRaycastLocationValid`（`…/UI/Core/RectMask2D.cs:178-184`）=
            //      `RectangleContainsScreenPoint(rectTransform, sp, eventCamera, m_Padding)` ⇒ **mask 自己那个 `rectTransform`**
            //      （`Graphic.Raycast` 沿父链逐个 `ICanvasRaycastFilter` 都判一遍 ⇒ 挂在 `Viewport` 上的 mask 管的是**它自己**的框）。
            //    ⇒ 合成 = `p ∈ R_图形 ∧ p ∈ (V_mask − pad)`；**不是** `p ∈ (R − pad)`。
            //    本节这一行原来写的是 `ClipRect(PaddedHitRect(r, hitPad), clip, …)` = **`(R − pad) ∩ V`**（错的对象），
            //    代价实测（锻造轨道那一幕，按钮整块落在 `V − pad` 里）：**原版命中宽 200.762 / 旧写法 190.762**。
            //    ⚠️ 改回旧写法 ⇒ `Editor/RewardsScene.cs` 那三条端到端断言立刻红（含改坏法）。
            //
            // ⚠️ **`maskPad` 全 0 / 无 `clip` 时逐字等价于原来那一行**（`PaddedClip` 两个早退 ⇒ 原样返回 `clip`）。
            // 🔴 **`PaddedClip` 的退化支（pad 比裁切框还大）在这里 = 「整块不裁」（`clip` 变 null ⇒ `ClipRect` 第一句
            //    `return true` ⇒ 命中区 = 原矩形）** —— 那正是原版那一支（`validRect` 不成立 ⇒
            //    `CanvasRenderer.DisableRectClipping()` ⇒ 这个 mask **不再过滤任何点**，见 `PaddedClip` 的注释）。
            //    ⛔ 别把它改成「造一个退化矩形」—— 那会建出**镜像 quad ⇒ 这颗钮静默点不动**（`PaddedHitRect` 的注释里有全过程）。
            // 整块在视口外 ⇒ **连节点一起不建**（返回 null；`AddHit` 的调用方都不接返回值）
            if (!ClipRect(r, PaddedClip(clip, maskPad), out hr)) return null;
            // ⚠️ 命中区那个**节点自己**摆在父原点（`localPosition = 0`）、quad 摆在矩形中心 ——
            //    照抄 `MainMenuSubmenuWindow.AddHit` 原来的写法**一字不改**
            //    （那边的自检有 1000+ 条断言，换个写法就是改行为）。
            //    部分越界时 quad 摆在**截过那块**的中心 ⇒ `PointerLayer` 用它的中心 + 宽高做命中，自动就跟着截了。
            // 🔴 **2026-10-07（A92）**：这个节点也从**裸 `Transform`** 改成 **`RectTransform`** ——
            //    它就是上面那句「**原版这一层就是按钮自己的 `RectTransform`**」的那一层；
            //    而同一族还有两条路也在建同一种 `Hit` 节点（`MenuWindowBase` 的 `New(b, "Hit")` 与
            //    它 `AddHit` 的转调）—— 那两条走的是本次一起改过的工厂 ⇒ **同一件东西不能一半一种类型**
            //    （分裂的类型比统一错更难查）。判据同上：原版 16768 个节点里 16510 是 `RectTransform`。
            //    ⚠️ **位置与命中都不受影响**：本节点 `localPosition` 恒 0、命中走 `PointerLayer` 的
            //    「quad 中心 + `WorldW/H`」（`HitBoxPx`），全程只读世界坐标 —— 与节点类型无关。
            var hit = new GameObject(name, typeof(RectTransform)).transform;
            hit.SetParent(parent, false);
            // 🔴 **2026-10-11（A218）**：命中区节点也写 `sizeDelta`（= 命中区矩形那块的大小）——
            //    与 `Node` / `MenuWindowBase.Node` **同一份换算**（`SetPxSize`）。
            //    ⚠️ 本节点 `localPosition` **恒 0**（见上面那句「摆在父原点」）⇒ **只写尺寸、不挪位**
            //      （`PointerLayer` 命中的是那颗 quad 的「中心 + `WorldW/H`」，与节点自己的位置无关）。
            //    改坏法：删掉这一句 ⇒ `Editor/RewardsScene.cs` §A218 的「`MenuDraw.Hit` 的命中区矩形 = 传进去那块」红。
            SetPxSize(hit, hr.W, hr.H);
            MakeHitQuad(hit, hr, q, Local(hit, hr.x1, hr.y1, hr.x2, hr.y2));
            var wb = hit.gameObject.AddComponent<WindowButton>();
            wb.onClick = onClick;
            if (target != null) wb.Bind(target, art, hoverArt, pressedArt);
            return hit;
        }

        /// <summary>🔴 **命中区里那颗透明 quad** —— 全工程只此一份（2026-10-04 A26 收口）。
        /// 已有三处在各写一遍：`Hit` · `DeckCell`（本文件）· `MainMenuSubmenuWindow.BuildTabButton`（左栏键）。
        ///
        /// <para>🔴 **它必须存在**：`PointerLayer.CollectHits` 取的是「按钮下**第一个 `ImageQuad`**」
        /// （`Shell/PointerLayer.cs` 的 `CollectHits`，`GetComponentInChildren`）⇒ **裸节点进不了命中表**。
        /// **A26 那个真缺陷就是这个**：`MenuDraw.DeckCell` 的 `Hit` 当年是 `Node()` 建的裸节点
        /// ⇒ 收藏窗卡组页 / 选卡组窗里的卡组格**真鼠标点不动**，而 `WindowButton.onClick` 在着、
        /// 自检直调 `wb.Click()` 也过 ⇒ **自检永远看不出来**（只有真鼠标能发现）。</para>
        ///
        /// <para>⚠️ **纯色件必须传 `CardArt.Solid()`** —— 平色块那一路遇 `null` 会**静默不建**
        /// （`Rect` 的守则）。所以这里建不出来时**出声**（红线：不许静默失败）——
        /// 否则就又回到「节点在、quad 没了、真鼠标点不动」那个原始症状。</para>
        ///
        /// <para>`pos` = quad 相对 `node` 的位置。**两种历史摆法都要能表达**（⛔ 别统一）：
        /// `Hit` 把**节点**摆在父原点、quad 摆在矩形中心；`DeckCell` 把**节点**摆在**矩形中心**、quad 摆 0
        /// —— 后者是既有断言量过的位置（`Editor/CollectionScene.cs:505-513`）。</para></summary>
        static void MakeHitQuad(Transform node, PxRect hr, int q, Vector3 pos)
        {
            var hq = ImageQuad.Create(node, CardArt.Solid(), pos, LayoutSpace.Px(hr.H),
                                      new Vector2(0.5f, 0.5f), "Hit");
            if (hq == null)
            {
                Debug.LogWarning($"[MenuDraw] `{node.name}` 的命中 quad 没建起来（`CardArt.Solid()` 取不到？）"
                                 + " —— `PointerLayer` 只认 `ImageQuad` ⇒ 这一颗**真鼠标点不动**（A26 那个症状）。");
                return;
            }
            hq.SetAspect(hr.W / Mathf.Max(1e-6f, hr.H));
            hq.SetTint(new Color(0f, 0f, 0f, 0f));
            hq.SetRenderQueue(q);
        }

        // ============================================================ 压暗层（「点窗外关窗」）的命中区
        //
        // 🔴 **规矩（A27 那一批用真缺陷买来的，A25⑥ 收口）**：压暗层的命中区必须落在
        //    **压暗层自己那一档**，且**严格低于本窗任何内容命中区档**。
        //    为什么：`ImageQuad` 的世界 z 恒 0，同档命中区谁吃到由**枚举顺序**决定
        //    ⇒ 症状是「点不动的钮**看着像正常工作**」（`BoosterInfoPopup` 2026-10-03 实测：
        //    压暗层把 `Tooltip` 图标、价签、`WebShop` 三颗钮的命中**全抢走了** —— 见那个文件
        //    `QShadeHit` 的长注释与 `_tmp_view/shop.log:11896`）。
        //
        // ✅ **2026-10-05 就地订正（A71④）**：这一段被一轮轮「追加订正」写成了**五套数并存**
        //    （7 / 10 / 12 / 16 / 17，其中 **16 与 17 同时留在段里**，还带着一句「以本条 16 为准」）
        //    —— 按铁律 6「数字与清单只留一处」，现在**只留下面这一个数**，旧的那几套连同
        //    「以本条 16 为准」那句一并删掉。判据文件那一处（`资料/待办判据_阶段二与联机.md` §（一）⑥）
        //    已经写清了口径，本段**照它抄、不另编**。
        //    🔴🔴 **2026-10-06 就地订正（铁律 5）**：本段原来写「全工程 **17 个**站点 / `grep` 命中 **16 条**，
        //       **以本条 16 为准**」—— **那个数已经过期了**（当时裸 `grep` 实测 = **21 条**）。
        //       **错因**：**A81 那批又加了 5 扇窗**（`DeckInfoPopup:578` · `CampaignRewardWindow:251` ·
        //       `DailyStreakPopup:150` · `InboxWindow:104` · `SettingsWindow:242`）—— A81 那一行自己写着
        //       「全工程站点 **16 → 21**」，**但本段的注释与判据文件都没跟着改**，于是两处都说成 16/17。
        //    ✅ **正确口径（2026-10-12 A409 逐行现读，铁律 5 就地订正：本条取代上面那个 21/22）**：
        //       🔴 **真调用 = 23 处**。**口径 = 只数【真调用】，不数【注释散文】**。
        //       · **怎么数的**：`grep -rn "MenuDraw\.ShadeHit(" --include=*.cs`
        //         （在 `Assets/CardPresentation/` 下）**裸命中 = 27 条**，其中 **4 条是注释散文**
        //         （`Editor/CollectionScene.cs:190` · `Editor/MainMenuScene.cs:170` ·
        //         `Editor/RewardsScene.cs:293` · `Editor/ShopScene.cs:874` —— 讲的都是
        //         「名字是各调用点自己传的形参」那一句）⇒ **27 − 4 = 23**。
        //       · **数了哪些**：**`Shell/` 22 处**（逐窗一处，全是 `GameWindow` 子类弹窗 ——
        //         就是下面那串清单 **2 + 13 + 1 + 5 = 21 处** **＋ `Shell/RewardWindow.cs:726`**，
        //         那个文件 **2026-10-11 批次 2** 才进本仓，是 21 之后新加的那一处）
        //         **＋ `Editor/ShellScene.cs:2314` 1 处**（自检探针 `BgProbeB` 那扇，**不是生产站点**）。
        //       · ➕ 另有 **1 处裁定过的例外**（`Shell/ProfileTab.cs` 里那句 `Hit(_nameWin, "DarkBgHit", …, L_NameBgHit, CancelNameWindow)`，走旧写法、⛔ 不许收口，见下）
        //         ⇒ **生产站点 = 23 个**（= 上面那 22 扇窗 ＋ 这 1 处例外）。
        //       ⚠️ 「真调用 23」与「生产站点 23」**数值相同纯属巧合**，是两件事，别混为一谈。
        //       ⚠️ **2026-10-07 就地订正（A77⑬①现读）**：这处例外原来写的行号是 `675` —— 那颗节点
        //       （`Hit(_nameWin, "DarkBgHit", …, L_NameBgHit, CancelNameWindow)`）已被后续波次推到 **`:739`**；
        //       判据文件（`资料/待办判据_审查发现_1005.md` ⑬①）里那个数**也是 675**，同属过期行号。
        //       那 21 处的来历（⚠️ **它是 A81 当时那份 `Shell/` 清单** —— 现读 = 这 21 处
        //       **＋ `Shell/RewardWindow.cs`** 1 处 = **22 扇窗**；⚠️ 这个数**不是裸 grep 能直接数的**：
        //       `CloseHit` 在别的件上是**关窗钮** —— `DeckInfoPopup.cs:625` · `Shell/DeckSelectionPopup.cs` 里那句 `Hit(root, "CloseHit", …)` ·
        //       `Shell/ImportDeckPopup.cs` 里那句 `Hit(root, "CloseHit", …)` · `TrophyInfoPopup.cs:236`（**四个都带一张按钮脸**）——
        //       别把它们算进来；而 `BoosterPackOpenWindow` 那颗又**不叫这个名**）：
        //      · **2 处早就在公共件上**：`ImportDeckPopup.cs:103` · `TrophyInfoPopup.cs:202`；
        //      · **13 处归 A47 接线批的白名单**：`BattleLogPopup` · `BoosterInfoPopup` · `CardDetailPopup` ·
        //        `DeckSelectionPopup` · `DuelPopupWindow` · `LeaderboardWindow` · `MissionRerollPopup` ·
        //        `PlayerProfileWindow` · `BoosterPackOpenWindow` + R-F 审查补出的一族（全叫 `BackdropHit`）
        //        `PracticeModePopup` · `RankedEventWindow` · `SkirmishEventWindow` · `SearchingMatchPopup`。
        //        ⚠️ `BoosterPackOpenWindow.cs` 的 `95/310` 是**同一处**的常量行与建节点行 ⇒ **只算一处**；
        //        把它数成两处，总数就会变成 18（这一段的上一版就是这么错的）；
        //      · **1 处不在那一批的白名单里**：`ChatPanel` ⇒ **2 + 13 + 1 = 16**（= A81 之前的数）；
        //      · **A81 又加 5 处**（上面那五扇）⇒ **16 + 5 = 21**（⚠️ **那是 A81 当时的数**；
        //        现读真值 → 上面那条 2026-10-12 的「真调用 = 23 处」，多出来的 2 处是那之后新加的）✅。
        //    🔴 **2026-10-07 就地订正（A77⑬①现读）**：上面那一串 `文件:行号` 是**当时的坐标，多数已漂**
        //       —— 现读的实位：`DeckInfoPopup:633`（原记 578）· `CampaignRewardWindow:260`（251）·
        //       `SettingsWindow:438`（242）· `TrophyInfoPopup:250`（202）；`DailyStreakPopup:150` /
        //       `InboxWindow:104` / `ImportDeckPopup:103` 三处**仍对**。⛔ **要行号就现 `grep -n`**
        //       （老坑：引用的行号会被后续波次推走）。
        //    ✅ **当前状态（2026-10-12 A409 逐条 grep 过；那之前的 2026-10-07 是 21 处）**：真调用 **23 处**
        //       （口径与清单见上面那条）**全部**走公共件；
        //       **只剩 `Shell/ProfileTab.cs` 里那句 `Hit(_nameWin, "DarkBgHit", …, L_NameBgHit, CancelNameWindow)` 一处仍是旧写法**（⇒ 生产站点 = 上面那 22 扇窗 ＋ 这 1 处 = **23 个**）
        //       （`Hit(_nameWin, "DarkBgHit", …, L_NameBgHit, CancelNameWindow)`）——
        //       🔴 **它是本规矩的第一条【例外】，不是漏掉的欠账**：改名窗是**窗内浮层**，
        //       打开时下层页面内容仍然 active，所以命中档要**夹在下层内容与浮层内容之间**
        //       （`L_Bg=0` / `L_NameBgHit=8` / 浮层里的钮=9）—— 判据与出处 →
        //       `资料/待办判据_阶段二与联机.md` §（一）⑥ ③。⛔ **别去「收口」它**（收口 = 按 `qShade = 压暗档` 走，
        //       那会把改名的按钮点不动）。
        //
        // ⚠️ **落点为什么是 `MenuDraw` 而不是 `MenuWindowBase`**（与 A25⑥ 的措辞有一处出入，理由如下）：
        //    上面那 22 处 `Shell/` 站点**全都是 `GameWindow` 的子类（弹窗）**，而 `MenuWindowBase.cs` 里那个类
        //    （`MainMenuSubmenuWindow`）只服务**子菜单窗**（奖励/商店/社交/收藏）——
        //    放那儿这些站点**一处也够不着**，等于再多一层皮。`MenuDraw.Hit` 才是它们**本来就在用**的公共件。
        //    ⇒ 这是把「一份」放在**能覆盖全工程**的那一层，不是另起一套。

        /// <summary>**压暗层（「点窗外关窗」）的命中区** —— 全工程唯一一份（2026-10-04 A25⑥ 收口）。
        /// <paramref name="qShade"/> = **该窗压暗层自己那一档**（例如 `BoosterInfoPopup.QShade`），
        /// <paramref name="qContentMin"/> = **本窗内容命中区里最低的那一档**（用来现场核那条不变量）。
        /// <para>🔴 **`qShade >= qContentMin` 会当场告警**（把静默失败变响）—— 那正是 A27 查出来的
        /// 「钮点不动、看着却像正常工作」的成因；告警文案里带上两个档号与出处，便于定位。</para>
        /// <para>⚠️ 本函数**不收 `clipSoftness`**（原版软边只改渲染、不改射线那一面 —— 同 `Hit`）；
        /// `clip` 可传（压暗层通常整屏，用得上时再说）。</para>
        /// <para>🆕 **2026-10-04（A47 接线批）自检用的一小份记录** —— 让自检能真的分辨「这一扇窗走的是公共件」
        /// 与「它偷偷留了一份自己的 `MenuDraw.Hit` 调用」（后者**没有任何行为差异可测**：档号一样时
        /// 两条路的四元组完全一致）：</para>
        /// <para>· <see cref="WasShadeHit"/>：这个命中区节点**是不是本函数建的**（逐节点，不是全局计数）；</para>
        /// <para>· <see cref="ShadeHitTierWarned"/>：**这一颗**命中区**当时**档不合法吗
        /// （= 上面那条告警对**它**响过）—— 把 `qShade` 传成 `QContentHit − 1` 这种
        /// 「看着像派生、其实同档/越档」的写法会被它抓住。</para>
        /// <para>🔴 **2026-10-07（A77⑬⑦）换成【按窗记账】**：原来这里是一个全局计数器
        /// `ShadeHitTierWarns`（**全程累积、从不复位**），而 21 条断言各自都读它 ⇒
        /// **任一窗报警会让后面每一条都红、文案却指着别的窗**（审查原话：「文案却指错窗」）。
        /// 现在记在**那颗命中区节点自己身上**（`ShadeHitTierWarn` 标记组件）⇒ 查询天然按窗、
        /// 也没有任何跨窗/跨次的状态。⚠️ 全工程不变量不变：**每扇窗各自都必须是「没报过」**。</para></summary>
        sealed class ShadeHitTierWarn : MonoBehaviour { public string Why; }

        /// <summary>这一颗命中区**当时**档不合法吗（`qShade >= qContentMin`）。
        /// `why` = 当时那条告警的正文（原样带出来，省得断言只报一个布尔）。
        /// ⚠️ 已销毁的节点 `node != null` 就是假 ⇒ 直接返回 false，不会去 `GetComponent`。</summary>
        public static bool ShadeHitTierWarned(Transform node, out string why)
        {
            why = "";
            if (node == null) return false;
            var w = node.GetComponent<ShadeHitTierWarn>();
            if (w == null) return false;
            why = w.Why;
            return true;
        }
        /// <summary>🔴 **2026-10-05（A71④）换判法**：原来这里是一张 `static HashSet&lt;Transform&gt;`，
        /// **只 `Add`、从不 `Clear`** ⇒ 窗口反复重建时 ① 无上限增长、② 长期持住**已销毁对象的托管壳**、
        /// ③ `instanceID` 复用时会**假阳性**（新节点被判成「ShadeHit 建的」）。
        /// 现在改成**挂在节点自己身上的一颗空标记**：节点跟着窗口一起销毁 ⇒ 上面三条一次都不成立
        /// （不再需要 `Clear`，也不再有一张全局表）。
        /// ⚠️ `ShadeHit` 的**既有行为一字未改**（那一句 `if (qShade >= qContentMin) …` 仍然那样告警；
        /// 🔴 只有计数那一半换了载体：`ShadeHitTierWarns++` → 往同一颗节点上挂 `ShadeHitTierWarn`）。</summary>
        sealed class ShadeHitMark : MonoBehaviour { }
        /// <summary>这个节点**是不是 `ShadeHit` 建的**（自检用 —— 见 `ShadeHit` 的注释）。
        /// 🔴 **签名与语义一字未改**（**5 份**自检宿主 `Editor/CollectionScene.cs:52` · `Editor/MainMenuScene.cs:52` ·
        /// `Editor/RewardsScene.cs:165` · `Editor/SettingsScene.cs:71` · `Editor/ShopScene.cs:679` 各自那个
        /// `CheckShadeRule` 包装照样调；⚠️ **2026-10-07（A77⑬⑥）起那 5 份包装已收口到
        /// 本文件的 `MenuDraw.CheckShadeRule`** ⇒ 这 5 个行号是**收口前**的坐标，收口后本函数只剩
        /// `MenuDraw.CheckShadeRule` 一个调用点）；
        /// 只有**判法**从「查一张全局表」换成「看节点自己身上有没有那颗标记」。
        /// ⚠️ 已销毁的节点 `node != null` 就是假（Unity 那一套）⇒ 直接返回 false，不会去 `GetComponent`。</summary>
        public static bool WasShadeHit(Transform node)
        {
            return node != null && node.GetComponent<ShadeHitMark>() != null;
        }

        public static Transform ShadeHit(Transform dark, PxRect r, int qShade, int qContentMin,
                                         System.Action onClick, string name = "CloseHit", PxRect? clip = null)
        {
            // 🔴 **告警与「记账」都要落在【这一颗节点】上**（`ShadeHitTierWarned`）——
            //    不能再拿一个全局计数器去回答「哪扇窗的档不合法」（A77⑬⑦）。
            string why = null;
            if (qShade >= qContentMin)
            {
                why = $"压暗层命中区 `{name}` 的档 {qShade} **不低于**本窗内容命中区档 "
                      + $"{qContentMin} —— 同档时 `ImageQuad` 的世界 z 恒 0，谁吃到命中退化成"
                      + "「枚举顺序」，症状是**点不动的钮看着像正常工作**。"
                      + "判据 → `资料/待办判据_阶段二与联机.md` §A25⑥ · `Shell/BoosterInfoPopup.cs` 的 `QShadeHit`。";
                Debug.LogWarning("[MenuDraw] " + why);
            }
            var hit = Hit(dark, name, r, qShade, onClick, null, null, null, null, clip);
            if (hit == null) return null;                                  // ⚠️ 节点没建出来 ⇒ 两颗标记都没处挂
            hit.gameObject.AddComponent<ShadeHitMark>();                   // 见 `WasShadeHit`
            if (why != null) hit.gameObject.AddComponent<ShadeHitTierWarn>().Why = why;   // 见 `ShadeHitTierWarned`
            return hit;
        }

        // ============================================================ 窗内面板的「吸收层」（点窗内空白处 ⇒ 原版什么都不发生）

        // 🔴 **原版语义（2026-10-06 逐窗实读 prefab 的 `Image.m_RaycastTarget`）**：
        //    窗内面板本体那颗 `Image` 的 `m_RaycastTarget = 1` ⇒ 射线**打到面板自己**；
        //    而「点它关窗」那个 `BackgroundCloseButton` **全库 88/88 都挂在压暗层
        //    `Menu Dark Background` 上**（一颗也不在面板上），而原版派发 =
        //    `ExecuteEvents.GetEventHandler<IPointerClickHandler>(命中的 Graphic)` **沿父链向上找**
        //    ⇒ 从面板出发的父链上没有处理器 ⇒ **无事发生**。
        //
        // 🔴 **我们这边原来会关窗（A94 的根因）**：命中候选**只收 `WindowButton`**
        //    （`PointerLayer` 的 `CollectHits` → `AllButtons` → `HitBoxPx`）⇒ **一颗没有 `WindowButton`
        //    的 quad 对命中完全透明**，射线**穿过面板**落到压暗层那颗「点窗外关窗」上。
        //    本函数就是补上「面板吃一下、什么都不做」的那颗命中区。
        //
        // 🔴 **三条实现红线（每条都有实证代价，⛔ 别绕过）**：
        //   ① **档不能沿用面板自己那一档** —— 同档时 `ImageQuad` 的世界 z 恒 0，
        //      谁吃到命中**退化成枚举顺序**（老坑，见上面「压暗层」那一段）。
        //      ⇒ 档**由本函数算**（`qContentMin - 1`），调用方不许自己挑。
        //   ② **不能只把 `onClick` 置 null** —— `PointerLayer.LogHit` 会把 `onClick == null`
        //      报成「🔴 这个命中区没有绑动作」，那是给「**忘了绑**」用的**真告警** ⇒ 会把吸收层误报成缺陷。
        //   ③ **不能裸挂 `WindowButton`** —— 它自带两处副作用：`tintOnHover` 的悬停色偏
        //      （指针划过整块面板会让面板**变暗**，而原版面板没有 `Selectable`、没有任何悬停变化）、
        //      以及 `Press()` 那条「连高亮图也没有 ⇒ 按下画面什么都不变」的告警。
        //      ⇒ 走 `WindowButton.absorbOnly`（那四个入口 + `Click` 全直接返回：零视觉、零告警）。

        /// <summary>`q &lt;= qShade` 时的告警正文（**按窗记账**：挂在**那颗吸收层节点**上，照 `ShadeHitTierWarned` 的形状）。
        /// 🔴 **2026-10-09（A221④）**：原来是一个全局累积计数器 `AbsorbTierWarns`（**从不复位**）——
        /// 任一窗告警会让**后面每一条** `CheckAbsorbRule` 都红、文案却指着别的窗（与 A77⑬⑦ 修掉的
        /// `ShadeHitTierWarns` 同族；那一轮只点了 `ShadeHit` 那一头，本字段当时没被点）。
        /// 现在记在节点自己身上 ⇒ 查询天然按窗、也没有跨窗/跨次状态。
        /// ⚠️ 全工程不变量不变：**每扇窗各自都必须是「没报过」**。
        /// 什么算不合法：`qContentMin - 1 &lt;= qShade`（该窗**没有空档** ⇒ 吸收层会与压暗层同档/越档，
        /// 赢家退化成枚举顺序 ⇒ 症状是「点窗内空白处**有时**会关窗」）。</summary>
        sealed class AbsorbTierWarn : MonoBehaviour { public string Why; }

        /// <summary>这一颗吸收层**当时**档不合法吗（`qContentMin - 1 &lt;= qShade`）。
        /// `why` = 当时那条告警的正文（原样带出来，省得断言只报一个布尔）。
        /// ⚠️ 已销毁的节点 `node != null` 就是假 ⇒ 直接返回 false，不会去 `GetComponent`。</summary>
        public static bool AbsorbTierWarned(Transform node, out string why)
        {
            why = "";
            if (node == null) return false;
            var w = node.GetComponent<AbsorbTierWarn>();
            if (w == null) return false;
            why = w.Why;
            return true;
        }

        /// <summary>挂在吸收层节点上的**空标记**（照 <see cref="ShadeHitMark"/> 的形状：
        /// 节点跟着窗口一起销毁 ⇒ 不需要 `Clear`、也不会有全局表的假阳性）。</summary>
        sealed class AbsorbMark : MonoBehaviour { }

        /// <summary>这个节点**是不是 `Absorb` 建的**（自检「这扇窗的面板吸收了没有」用）。
        /// ⚠️ 已销毁的节点 `node != null` 就是假（Unity 那一套）⇒ 直接返回 false。</summary>
        public static bool WasAbsorb(Transform node)
        {
            return node != null && node.GetComponent<AbsorbMark>() != null;
        }

        /// <summary>**窗内面板的「吸收层」** —— 全工程唯一一份（2026-10-06 A94）。
        /// 语义 = **原版面板那颗 `Image`（`m_RaycastTarget = 1`、父链上没有点击处理器）**：
        /// 这一下**被吃掉、什么都不做**（不关窗、不派发）。
        ///
        /// <para>参数：<paramref name="r"/> = **该窗面板底图的原版矩形**（逐窗读 prefab 里那块 `Image` 的 rect，
        /// ⛔ 别拿我们自己的常量反推）；<paramref name="qShade"/> / <paramref name="qContentMin"/> =
        /// 与同一扇窗那次 <see cref="ShadeHit"/> **同两个档**（压暗层自己那一档 / 本窗内容命中区最低的那一档）。</para>
        ///
        /// <para>🔴 **档由本函数算**：`q = qContentMin - 1`（严格夹在压暗档与内容命中区之间）——
        /// 「面板矩形 / 档位」是逐窗的，让调用方各挑一档**迟早挑错**（`ImportDeckPopup` 的旧注释里
        /// 就记着一次「内容档 − 1 正好撞上文字档」的教训）。</para>
        ///
        /// <para>⚠️ **`q` 与某个【文字档】同号是无害的**（本工程好几扇窗如此，例如设置窗的 `QText`）：
        /// ① 文字层**不带命中区**，同档不会抢命中；② 本标准 quad 的 tint 是**全透明**
        /// （`MakeHitQuad` 给的 `(0,0,0,0)`）⇒ 队列只当**命中优先级**用，不参与画面排序。
        /// ⇒ 看到「档号撞上文字档」**不要去改它**（改了就不是 `qContentMin - 1` 了）。</para>
        ///
        /// <para>⛔ **没有面板底图的窗不接**（例：`CardDetailPopup` —— 原版那里射线直接落到压暗层，
        /// 点了**确实会关**，我们的行为本来就对）；`BoosterPackOpenWindow` 同理
        /// （原版整屏那层 `Collider` 是 `UIGenericEventCatcher` ⇒ **点哪儿都关**）。
        /// 判断办法：去 prefab 里读那颗面板 `Image` 在不在、`m_RaycastTarget` 是不是 1。</para></summary>
        public static Transform Absorb(Transform parent, string name, PxRect r, int qShade, int qContentMin,
                                       PxRect? clip = null)
        {
            int q = qContentMin - 1;
            // 🔴 2026-10-09（A221④）：告警**先算成字符串**、等节点建出来再挂上去（按窗记账）。
            //    ⛔ **既有行为一字未改**：那条 `Debug.LogWarning` 照旧响，只是计数那一半换了载体。
            string why = null;
            if (q <= qShade)
            {
                why = $"吸收层 `{name}` 算出来的档 {q}（= 内容命中区档 {qContentMin} − 1）"
                      + $" **不高于**本窗压暗层档 {qShade} —— 该窗**没有空档**，同档时谁吃到命中退化成"
                      + "「枚举顺序」（症状：点窗内空白处**有时**会关窗）。"
                      + "判据 → `Shell/MenuDraw.cs` 的 `Absorb` 与 `ShadeHit` 两段注释。";
                Debug.LogWarning("[MenuDraw] " + why);
            }
            var hit = Hit(parent, name, r, q, null, null, null, null, null, clip);
            if (hit == null) return null;
            // 🔴 **`absorbOnly` 必须在挂上之后的同一帧置位**（`Click` 靠它 early-return）——
            //    本函数建完就返回，没有中间窗口可插。
            var wb = hit.GetComponent<WindowButton>();
            if (wb != null) wb.absorbOnly = true;      // 见 `WindowButton.absorbOnly`
            hit.gameObject.AddComponent<AbsorbMark>();  // 见 `WasAbsorb`
            if (why != null) hit.gameObject.AddComponent<AbsorbTierWarn>().Why = why;   // 见 `AbsorbTierWarned`
            return hit;
        }

        /// <summary>🆕 **自检模板**（A25⑥ ②）：**一扇窗一行**就能核那条不变量 ——
        /// 「压暗命中区的档 = **同一扇窗里视觉压暗层那颗 quad 的档**，且**严格低于**本窗内容命中区档」。
        /// <para>用法（各 `Editor/*Scene.cs` 里，`darkHit` = 「点窗外关窗」那个节点，
        /// `darkVisual` = 同一扇窗那块**画出来的**压暗层节点，例如 `Menu Dark Background`；
        /// 逐窗的取法 → 各调用点的注释）：<c>MenuDraw.ShadeRuleOk(w.ShadeHit, w.ShadeVisual, w.QImpHit, out why)</c>。</para>
        ///
        /// <para>🔴🔴 **2026-10-07（A77⑬③）去自证 —— 这一版的期望值【不再来自调用方传的常量】。**
        /// 上一版签名是 `ShadeRuleOk(darkHit, qShade, qContentMin, …)`，而那**两个实参正是被测实现
        /// 传给 `ShadeHit` 的同一对常量** ⇒ 「档 == `qShade`」与「`qShade` &lt; 内容档」**两条子判据全是
        /// 同义反复**（A47/A48 的独立审查：13/13 全中）—— 它证明不了「这个档号就是压暗层那一档」，
        /// 而那恰恰是这一批唯一的实质目标。
        /// 现在改成**量同一扇窗里【视觉压暗层】那颗 quad 的 `RenderQueue`**（见
        /// <see cref="ShadeVisualQuad"/>）：那颗 quad 是**另一处代码**（窗口自己那句
        /// `Rect(…, "Menu Dark Background", QShade, …)`）建的**另一个对象** ⇒ 它与命中区那颗
        /// 档号不一致时必红。⛔ **别再把它改回「比传进来的常量」**。</para>
        ///
        /// <para>四条子判据：① 命中区节点在（不在 = 点窗外关不了窗）② 它下面真的挂着 `ImageQuad`
        /// （裸节点 `PointerLayer` 拿不到 —— A26 那族的同一个坑）③ **档号 == 视觉压暗层那颗的档号**
        /// ④ 档号**严格低于**内容命中区档。<br/>
        /// ⚠️ **④ 仍是一个「常量互锁」**（左端现在是场景真值、右端 `qContentMin` 仍是各窗声明的常量）：
        /// 它能抓住「有人把两个常量改成同档/越档」，但**证不了 `qContentMin` 就是内容命中区的档**
        /// —— 如实记在这里，别当成它已经证过了（审查原话：**唯一能真红的是 ③**）。</para></summary>
        public static bool ShadeRuleOk(Transform darkHit, Transform darkVisual, int qContentMin, out string why)
        {
            why = "";
            if (darkHit == null) { why = "压暗层的命中区节点不在"; return false; }
            var q = darkHit.GetComponentInChildren<ImageQuad>();
            if (q == null) { why = "压暗命中区下没有 `ImageQuad`（`PointerLayer` 拿不到 ⇒ 点窗外关不了窗）"; return false; }
            // 🔴 ③ 独立判据：期望值 = **视觉压暗层那颗 quad 的档**（场景真值），不是调用方传进来的常量。
            if (darkVisual == null)
            {
                why = "**视觉压暗层**那个节点没找到（`Menu Dark Background` / 各窗的等价物）"
                      + " ⇒ 期望值就只剩「被测实现自己传的那个常量」= 同义反复 ⇒ 这条判据**不成立**"
                      + "（把该窗那块压暗层节点传进来，别传 null）";
                return false;
            }
            var v = ShadeVisualQuad(darkVisual, darkHit);
            if (v == null)
            { why = $"视觉压暗层 `{darkVisual.name}` 下量不到 `ImageQuad`（它得是那块**画出来的**压暗层）"; return false; }
            if (q.RenderQueue != v.RenderQueue)
            {
                why = $"命中 quad 的档是 {q.RenderQueue}，而**同一扇窗里视觉压暗层** `{darkVisual.name}` 那颗是 "
                      + $"{v.RenderQueue} ⇒ 这一颗命中区**不在压暗层那一档**上"
                      + "（改坏法：把 `ShadeHit` 的 `qShade` 换成 `QShade+1` / `QContentMin−1` 这类派生值）";
                return false;
            }
            if (!(q.RenderQueue < qContentMin))
            { why = $"压暗档 {q.RenderQueue} **不低于**内容命中区档 {qContentMin}（同档时谁吃到退化成枚举顺序）"; return false; }
            return true;
        }

        /// <summary>**视觉压暗层那颗 quad** —— 从 `visual` 子树里取**第一颗不属于 `exclude`** 的 `ImageQuad`
        /// （`includeInactive` = true：那一层的开关由窗口自己管，量的是它建出来时那个档）。
        /// 两种历史摆法都要吃：① `visual` **自己**就是那颗 quad（`MenuDraw.Rect(…, "Menu Dark Background", …)` 建的）；
        /// ② 它是那颗 quad 的**父节点**（`MenuDraw.Node(…)` 建节点 + 子件名叫 `Image` 的 quad）。
        /// ⚠️ `exclude` = 命中区那个节点 —— 它的 quad 也是 `visual` 的后代（②那种摆法下），必须跳过去。
        /// <para>🆕 **2026-10-07（A77⑬③）加了第三条路**：`visual` 自己量不到 quad 时，再看**同名的兄弟**
        /// —— 同一处历史上真出现过「**同名的两个兄弟**」：一个只当节点（`Node(root, "Menu Dark Background", …)`）、
        /// 另一个才是带 quad 的（紧接着 `Solid(root, "Menu Dark Background", …)`），
        /// 见 `Shell/SettingsWindow.cs` 里那对同名兄弟（`Node(…)` 与 `Solid(root, "Menu Dark Background", …)`） ⇒ 只按名字 `Find` 会拿到**没 quad 的那个**。
        /// ⛔ 这不是「兜底猜」：同名 + 同一个父，判据是那两行代码本身。</para></summary>
        public static ImageQuad ShadeVisualQuad(Transform visual, Transform exclude)
        {
            if (visual == null) return null;
            var q = FirstQuadIn(visual, exclude);
            if (q != null) return q;
            var p = visual.parent;
            if (p == null) return null;
            for (int i = 0; i < p.childCount; i++)
            {
                var c = p.GetChild(i);
                if (c == visual || c.name != visual.name) continue;
                q = FirstQuadIn(c, exclude);
                if (q != null) return q;
            }
            return null;
        }

        /// <summary>`t` 子树里第一颗不属于 `exclude` 的 `ImageQuad`（`ShadeVisualQuad` 的一步）。</summary>
        static ImageQuad FirstQuadIn(Transform t, Transform exclude)
        {
            if (t == null) return null;
            foreach (var q in t.GetComponentsInChildren<ImageQuad>(true))
            {
                if (q == null) continue;
                if (exclude != null && (q.transform == exclude || q.transform.IsChildOf(exclude))) continue;
                return q;
            }
            return null;
        }

        /// <summary>自检宿主的断言回调 —— 形状与 `Editor/*Scene.cs` 各自的 `CheckTrue(bool, string)` **逐字相同**
        /// （所以各宿主直接传方法组：`MenuDraw.CheckShadeRule(CheckTrue, …)`）。</summary>
        public delegate void MenuCheck(bool ok, string msg);

        /// <summary>🆕 **2026-10-07（A77⑬⑥）：压暗层命中区那条不变量的断言 —— 全工程唯一一份。**
        /// 上一版这一段在 **5 个** `Editor/*Scene.cs` 里各抄一份（`CheckShadeRule`，逐字相同）——
        /// 与「两处写同一条规则 = 迟早不一致」同族（`CLAUDE.md` §三），审查点名要收口。
        /// <para>三条子判据：① `ShadeRuleOk`（**档 == 视觉压暗层那颗 quad 的档** + 严格 &lt; 内容档）
        /// ② 这一颗是公共件 `MenuDraw.ShadeHit` 建的 ③ **这一颗**没被 `ShadeHit` 报过档位告警
        /// （按窗记账，见 `ShadeHitTierWarned`）。</para>
        /// <para>🔴 **为什么必须问 `WasShadeHit`**：档本来就对的那几扇窗，走不走公共件**没有任何可见行为差异**
        /// ⇒ 只有这一句能分出两种状态（改回自己那份 `MenuDraw.Hit(...)` 就红）。</para></summary>
        public static void CheckShadeRule(MenuCheck chk, string what, Transform darkHit, Transform darkVisual,
                                          int qContentMin)
        {
            if (chk == null) return;
            string why;
            chk(ShadeRuleOk(darkHit, darkVisual, qContentMin, out why),
                $"{what}：压暗层的命中区「档 == **视觉压暗层**那颗 quad 的档({VisualQueueOf(darkVisual, darkHit)})"
                + $" 且 < 内容命中区档({qContentMin})」"
                + "（" + (why.Length > 0 ? why : "四条都过：节点在 + 带 `ImageQuad` + 档与视觉压暗层一致 + 低于内容档") + "）");
            chk(WasShadeHit(darkHit),
                $"{what}：这条命中区**是公共件 `MenuDraw.ShadeHit` 建的**"
                + "（改回本窗自己那份 `MenuDraw.Hit(...)` 这条就红）");
            string tw;
            bool warned = ShadeHitTierWarned(darkHit, out tw);
            chk(!warned,
                $"{what}：`MenuDraw.ShadeHit` 对**这一颗**命中区**没报过档位告警**"
                + "（按窗记账 —— 只认这颗节点上的标记，不受别的窗影响）"
                + (warned ? "；⚠️ 实得告警：" + tw : ""));
        }

        /// <summary>只给上面那条断言的**文案**用：视觉压暗层那颗 quad 的档号（量不到就 `−1`）。</summary>
        static int VisualQueueOf(Transform visual, Transform darkHit)
        {
            var v = ShadeVisualQuad(visual, darkHit);
            return v != null ? v.RenderQueue : -1;
        }

        /// <summary>🆕 **2026-10-14（A796）：压暗层「**点了会不会关**」的动作侧断言 —— 全工程唯一一份。**
        ///
        /// <para>🔴 **为什么要有它**（缺口原话 → `资料/普查产出_1014/RO_文字半边与压暗层.md` §三）：
        /// 上面 `CheckShadeRule` 那三条（合计 6 条子判据）**一条都不问点击** —— 它只答「这颗命中区在不在、
        /// 带的 `ImageQuad` 对不对、档对不对、是不是公共件建的」。于是**只配了几何断言的那几扇窗**
        /// （卡包详情 / 开包 / 卡片详情那一族）上，「点窗外 ⇒ 关窗」这件事**没有任何站立点**：
        /// 把这颗命中区的 `onClick` 换成空动作、或整颗换成 `MenuDraw.Absorb` 那条吸收层，
        /// **既有断言一条都不会红**（`CheckAbsorbRule` 只覆盖配了它的那些窗）。
        /// ⚠️ 另一个方向（「点了**不会**关」）今天**全仓没有一扇窗需要** ⇒ 本口只管「**该关的能关**」。</para>
        ///
        /// <para>两条**互为对照**（缺一条就分不出「压根没接通」与「本来就关不掉」）：
        /// ① 点**之前** `state() == Open`（**负向态** —— 一扇开不起来的窗不能靠「反正关着」把 ② 蒙过去）；
        /// ② 对 `darkHit` 上那颗 `WindowButton` 调 `Click()`（= `PointerLayer` **唯一的派发口**，
        ///    `Shell/PromptPopup.cs`；`ClickForTest` 也是同一条）⇒ `state() == Closed`。
        /// 🔴 **改坏法**（判别力就靠它）：把 `MenuDraw.ShadeHit(…, () => Close())` 换成 `MenuDraw.Absorb(…)`
        /// （或给那颗 `WindowButton` 置 `absorbOnly`）⇒ `Click()` 第一句就早退 ⇒ **②必红**。</para>
        ///
        /// <para>🔴 **③ 那条结构断言是给「两边一起改」准备的**（灭自证，`CLAUDE.md` §三）：
        /// 「这颗不是吸收层」（`!absorbOnly`）与 ② **结构上不可能同时满足** ——
        /// 若有人把实现换成吸收层、再顺手把 `WindowButton.Click()` 里那句 `absorbOnly` 早退删掉，
        /// 两条**行为**断言会一起变绿，而这一条**照样红**。</para>
        ///
        /// <para>⚠️ **它【不】答什么**（⛔ 别把这两件事并进来，也别以为本口盖住了它们）：
        /// 「几何 / 档对不对」= `CheckShadeRule` 的职责；
        /// 「**屏幕坐标**点得到吗（真路径 `PointerLayer.ClickAt`）」= `CheckAbsorbRule` 那一条的职责
        /// （它用**钉死**的点，⛔ 不是「扫一圈找第一个命中压暗层的点」—— 那种弱条件分不出两种状态）。
        /// 本口走**派发口直调** ⇒ **不看坐标、不看遮挡**，那是有意的（否则每扇窗都得先裁一个钉死的点；
        /// 要更强的覆盖就在宿主侧补一条 `PointerLayer.ClickAt`，⛔ 别在本口里扫点）。</para>
        ///
        /// <para>🔴 **时机是一条硬约束：本口会把窗【真的关掉】—— 请在【本窗其它断言都跑完之后】再调它。**
        /// `wb.Click()` → 窗自己的 `Close()` **是同步的**（同一个调用里就写 `CurrentState = Closed`、
        /// 末句 `SetActive(false)`；`Shell/WindowsManager.cs` 的 `GameWindow.Close`），而且它打在**顶窗**上时
        /// `NotifyClosed` 还会 `ShowPreviousWindow()` 把底窗带回前台（`Shell/WindowsManager.cs` 的 `NotifyClosed`）。
        /// ⇒ 调完这一口，**这扇窗的树已经不是原来那一份了**（`activeSelf` 变了，`PointerLayer` 的命中
        /// 也不再是它）。⛔ **别试「就地重开」** —— `TryOpen(null)` 会**重建内容**、把刚灌进去的假数据
        /// 清掉（同族实测留档 → `Editor/RewardsScene.cs` 的 `Run` 里「探针跑在关闭的窗上」那一段 收件箱那一块；那里的原话是
        /// 「⛔ 不许再把本块挪回 `CheckAbsorbRule` 之后」）。
        /// ⚠️ 这**不是本口的新坑**：`CheckAbsorbRule` 的第 ⑥ 步（「点面板外 ⇒ 窗 `Closed`」）早就在真点击，
        /// 它的宿主已经踩过同一次红 —— **同一族、同一个成因**。</para>
        ///
        /// <para>调用点怎么接（**本批只做这个口，宿主那一侧的调用是下一批**）：
        /// <c>MenuDraw.CheckShadeClickRule(CheckTrue, "卡包详情窗", t, darkHitN, () =&gt; win.CurrentState);</c>
        /// —— `winRoot` = **那一扇窗的根**（用来核「这颗命中区属于这一扇」，防传错节点 / 被别家的窗顶掉）。</para></summary>
        /// <param name="chk">宿主自己的 `CheckTrue(bool, string)`（形状同 `MenuCheck`）。</param>
        /// <param name="what">宿主在报告里用的窗名（例 `"卡包详情窗"`）。</param>
        /// <param name="winRoot">这一扇窗的根节点（= 刚 `Open()` 出来那棵树）。</param>
        /// <param name="darkHit">压暗层那颗命中区（`MenuDraw.ShadeHit` 的返回值，或 `FindChild(win, "CloseHit")`）。</param>
        /// <param name="state">**这一刻**的窗状态（喂 `() =&gt; win.CurrentState`）。</param>
        public static void CheckShadeClickRule(MenuCheck chk, string what, Transform winRoot, Transform darkHit,
                                               System.Func<WindowState> state)
        {
            if (chk == null) return;
            // ⓪ 前置：三样都得在，否则下面两条等于没查 ⇒ **如实报红**（⛔ 不许静默早退 —— 项目红线）
            if (winRoot == null || darkHit == null || state == null)
            {
                chk(false, $"{what}：压暗层点击这条**查不了** —— "
                          + (winRoot == null ? "窗根 `winRoot` 是 `null`;" : "")
                          + (darkHit == null ? "压暗层命中区 `darkHit` 是 `null`（`CheckShadeRule` 那条几何断言会先报它）;" : "")
                          + (state == null ? "`state` 是 `null`;" : ""));
                return;
            }
            // ① 这颗命中区**属于这一扇窗**（传错节点 / 被别家的窗顶掉 ⇒ 红）。
            //    ⚠️ 报红**不早退**：下面那条点击行为照样查得下去（两条互为对照，少一条就瘦一圈）。
            chk(darkHit.IsChildOf(winRoot),
                $"{what}：压暗层那颗命中区**挂在这一扇窗的树里**（`darkHit.IsChildOf(winRoot)`）");
            var wb = darkHit.GetComponent<WindowButton>();
            if (wb == null)
            {
                chk(false, $"{what}：那颗命中区上挂着 `WindowButton`（`PointerLayer` 只派发它 ⇒ 没有它**点了什么都不会发生**）");
                return;
            }
            // ② 结构（灭自证那一条）：它必须是**真动作**，不是吸收层（`absorbOnly` 在 `Click()` 第一句就早退）
            chk(!wb.absorbOnly,
                $"{what}：那颗是**真动作**（⛔ 不是 `MenuDraw.Absorb` 那条吸收层 —— 吸收层点了什么都不做）");
            // ③④ 两条互为对照的行为断言（顺序不能换：③ 是 ④ 的前提）
            var before = state();
            chk(before == WindowState.Open,
                $"{what}：（前提）点**之前**窗是**开**着的（实得 `{before}`）—— "
                + "一扇开不起来的窗不能靠「反正关着」把下面那条蒙过去");
            if (before != WindowState.Open) return;      // 前提不成立 ⇒ 下面那条无意义（别硬点）
            wb.Click();                                  // = `PointerLayer` 的派发口（与真点同一条路）
            var after = state();
            chk(after == WindowState.Closed,
                $"{what}：**点压暗层 ⇒ 窗关掉**（走 `WindowButton.Click()`；实得 `{after}`）。"
                + "改坏法：把这颗的 `onClick` 换成空动作、或整颗换成 `MenuDraw.Absorb` ⇒ **本行必红**。");
        }

        /// <summary>自检宿主的**浮点近等**断言回调 —— 形状与 `Editor/*Scene.cs` 各自的
        /// <c>static void CheckNear(float got, float want, float tol, string msg)</c> **逐字相同**
        /// （所以各宿主直接传方法组：<c>MenuDraw.CheckAbsorbRule(CheckTrue, CheckNear, …)</c>）。
        /// <para>🔴 **为什么另开一个口、而不是把四条近等折进 `MenuCheck`**：宿主那一版会在文案尾巴上打
        /// 「（{got:F2} ≈ {want:F2}±{tol:F2}）」这个**实得值**；折进去就得在这里**再手写一份格式** ——
        /// 而那正是「两处写同一条规则 = 迟早不一致」（`CLAUDE.md` §三）。换口 = 那份格式化实现照旧只有一处。</para></summary>
        public delegate void MenuNear(float got, float want, float tol, string msg);

        /// <summary>🆕 **2026-10-15（A825）：吸收层那组不变量的断言 —— 全工程唯一一份。**
        /// 上一版这一段在 **5 个** `Editor/*Scene.cs` 里各抄一份（`CheckAbsorbRule`，签名**逐字相同**、
        /// 26 个调用点）—— 与 `CheckShadeRule` 同族（那条 2026-10-07 · A77⑬⑥ 已收口）。
        ///
        /// <para>🔴 **它为什么比当初记账时更值钱**：上面 `CheckShadeRule` 那 6 条子判据**一条都不问点击**，
        /// 而本函数的第 ⑤⑥ 步**早在真点**（`PointerLayer.ClickAt` ⇒ 断 `state()`）——
        /// A796 那 24 处压暗层调用点里，**13 处的「点了会关」站立点就在本函数末两步**。</para>
        ///
        /// <para>**六步**：① 吸收层节点在；② 它是**公共件** `MenuDraw.Absorb` 建的（`WasAbsorb`）；
        /// ③ 渲染矩形 = 原版面板底图（量那颗 `ImageQuad` 自己的真值，四沿各 ±1.5px）；
        /// ④ 档 = **内容命中区档 − 1**、且**严格夹在**压暗层档与内容档之间；
        /// ⑤ `Absorb` 对**这一颗**没报过档位告警（按节点记账）；⑥ 两条**互为对照**的行为断言
        /// （点面板 ⇒ **不关** · 点面板外 ⇒ **关**）。</para>
        ///
        /// <para>⚠️ **点哪儿（两个点，判据不同）**：
        /// · **面板内**：先试**原版矩形中心**，被窗内真件（按钮）盖住时沿一圈**固定的**候选点找一个
        ///   「命中是吸收层」的点。那一处「命中是谁」**不是期望值**，它只是**选点的条件** ——
        ///   断的是**窗的状态**（`state()`）。
        /// · **面板外**：⛔ **不扫、钉死屏幕左上角 (5,5)**，而且「命中是谁」**就是期望值**
        ///   （必须是**本窗压暗层那一颗**：`IsChildOf(winRoot)` ∧ `MenuDraw.WasShadeHit`）。
        ///   扫一圈会让「某颗命中区过大、把压暗层吃掉一半」这类缺陷从别的候选点上绕过去。
        ///   为什么钉 (5,5) 是**原版判据**算出来的 → 下面那一段行内注释。</para>
        ///
        /// <para>🔴 **为什么两条行为必须一起断**：只断「点面板 ⇒ 不关」时，一个**根本关不掉的窗**也能绿；
        /// 只断「点面板外 ⇒ 关」时，把窗建小到「点哪儿都关」也绿。两条互为对照才分得出这两条路。</para>
        ///
        /// <para>🔴 **调用点计数（2026-10-16 现核订正 · 铁律 5）**：本行原文写「**26 个**调用点 · `MainMenuScene 13 · RewardsScene 7`」——
        /// **实测是 23**（`Editor/{MainMenuScene **12** · RewardsScene **5** · CollectionScene 4 · SettingsScene 1 · ShopScene 1}`，
        /// 逐文件现数）。⚠️ 前一位代理在 `RewardsScene` 只数到 **5**、本文件原文写 **7** ⇒ **差的那 2 处没查清**，
        /// 谁要引用这个数**请自己现数一次**（`grep -c` 按宿主）。
        /// ✅ **2026-10-16 全部收口**：`Editor/MainMenuScene.cs` 那一份**也已换成本函数的包装**（W3 · A825 第 5 份）——
        /// 下面那段「只收到 4 份 / MainMenuScene 仍是副本」**已过期**，留作历史痕迹。
        /// ⛔ 以下为原文（留痕）：**26 个调用点一个字都没动**（`Editor/{CollectionScene 4 · MainMenuScene 13 · RewardsScene 7 ·
        /// SettingsScene 1 · ShopScene 1}` —— 2026-10-15 实读）：宿主各自留一个**同签名**的包装**转调**本函数。
        /// 🔴 **2026-10-15 只收到 4 份**（`CollectionScene` / `RewardsScene` / `SettingsScene` / `ShopScene`）——
        /// ⛔ **`Editor/MainMenuScene.cs` 那份仍是它自己的一份副本**（那一刻该文件被另一个写手占着，
        /// 本件不越界）⇒ **它 13 个调用点走的是老路**（行为与本节逐位相同，只是没接上公共件）。
        /// 下次动 `Editor/MainMenuScene.cs` 时把那一份删掉、改成本函数的包装。</para></summary>
        /// <param name="chk">宿主自己的 `CheckTrue(bool, string)`（形状同 `MenuCheck`）。</param>
        /// <param name="near">宿主自己的 `CheckNear(float,float,float,string)`（形状同 `MenuNear`）。</param>
        /// <param name="what">宿主在报告里用的窗名（例 `"卡包详情窗"`）。</param>
        /// <param name="winRoot">这一扇窗的根节点。</param>
        /// <param name="nodeName">吸收层那个节点的名字（各调用点自己传给 `MenuDraw.Absorb` 的那个）。</param>
        /// <param name="x1"/><param name="y1"/><param name="x2"/><param name="y2">原版面板底图的矩形（画布 px）。
        /// ⚠️ 本函数**不过 `Screen()`** —— 各调用点传进来的**已经是画布 px**（`SettingsScene` 那几处的 0.9 烘在里面）。</param>
        /// <param name="qShade">本窗压暗层档（只用于第 ④ 步的夹取与文案）。</param>
        /// <param name="qContentMin">本窗内容命中区档。</param>
        /// <param name="state">**这一刻**的窗状态（喂 `() =&gt; win.CurrentState`）。</param>
        public static void CheckAbsorbRule(MenuCheck chk, MenuNear near, string what, Transform winRoot, string nodeName,
                                           float x1, float y1, float x2, float y2,
                                           int qShade, int qContentMin, System.Func<WindowState> state)
        {
            if (chk == null) return;
            if (near == null)
            {
                // ⛔ 别静默早退（项目红线）：缺 `near` 这一口 = 第 ③ 步那四条近等**等于没查**。
                chk(false, $"{what}：吸收层那条**查不了** —— 宿主没把 `MenuNear`（近等断言回调）传进来");
                return;
            }
            // ① 节点在 ② 是公共件建的
            // ⚠️ **先按窗根的直接子件取**（相 1：20 个吸收层都是窗根的直接子件；只有 `RankedEventWindow`
            //   那个嵌在 `General Red Background` 底下）—— 直接子件取不到再退到递归查找。
            //   🔴 为什么不能一上来就递归找：`SkirmishEventWindow` 里**嵌着** `Searching Oponent Popup`，
            //   那扇自己也有一个 `AbsorbHit` ⇒ 递归找会按层级序先撞上谁不好说（本窗自己的那个排在前面，
            //   但那是**层级序的巧合**，不是判据）。
            var node = winRoot != null ? winRoot.Find(nodeName) : null;
            if (node == null) node = FindChildIn(winRoot, nodeName);
            chk(node != null,
                $"{what}：吸收层节点 `{nodeName}` 在（`MenuDraw.Absorb` 建的 —— 原版面板那颗 `Image` 的等价物）");
            chk(WasAbsorb(node),
                $"{what}：它是**公共件 `MenuDraw.Absorb` 建的**（`MenuDraw.WasAbsorb`；哪扇窗自己再写一份就红）");
            // ③ 矩形 = 原版那块面板底图的 rect（量 `ImageQuad` 自己的渲染真值）
            // 🔴 **取法只有这一份**：`node` 子树里**第一颗 `ImageQuad`**。
            //   · `Absorb` → `Hit` 只建「一个裸 `RectTransform` 命中节点 + 它下面那颗命中 quad」，
            //     **整条路上没有任何 `Label`**（`Shell/MenuDraw.cs` 的 `Hit`）⇒「先找 `Label` 再退 `ImageQuad`」
            //     那种通用取法（例 `Editor/ShopScene.cs` 的 `RectOf`）落在吸收层上**结果相同**；
            //   · `node == null` ⇒ 矩形量不到、下面四条 near 一条都不该过（如实按「没 `ImageQuad`」报红）。
            var q = node != null ? node.GetComponentInChildren<ImageQuad>() : null;
            if (q == null)
            {
                chk(false, $"{what}：吸收层下面**没有 `ImageQuad`**（`PointerLayer` 的命中候选靠它 ⇒ 这一层等于没建）");
            }
            else
            {
                float w = q.WorldW * 108f, h = q.WorldH * 108f;
                float cx = LayoutSpace.PxX(q.transform.position.x), cy = LayoutSpace.PxY(q.transform.position.y);
                near(cx - w * 0.5f, x1, 1.5f, $"{what}：吸收层渲染矩形**左沿** = 原版面板底图");
                near(cy - h * 0.5f, y1, 1.5f, $"{what}：…**上沿**");
                near(cx + w * 0.5f, x2, 1.5f, $"{what}：…**右沿**");
                near(cy + h * 0.5f, y2, 1.5f, $"{what}：…**下沿**");
                // ④ 档 = 内容命中区档 − 1，且**严格夹在**压暗层与内容命中区之间
                int wantQ = qContentMin - 1;
                // ⚠️ 比 `RenderQueue` 时**把实得值写进文案**（而不是另开一个泛型断言口）：
                //    上一版 5 份里有一份走的是 `Check(got, want, msg)`，它出红时打「期望 [N]，实得 [M]」——
                //    这里用同一个 `chk` 口 + 自带实得值，**断言条数不变、实得值照样看得到**。
                chk(q.RenderQueue == wantQ,
                    $"{what}：吸收层的档 = **内容命中区档 − 1**（{qContentMin} − 1 = {wantQ}；实得 {q.RenderQueue}）");
                chk(qShade < q.RenderQueue && q.RenderQueue < qContentMin,
                    $"{what}：**{qShade} < {q.RenderQueue} < {qContentMin}** —— 严格夹在压暗层与内容命中区之间"
                    + "（同档时 `ImageQuad` 的世界 z 恒 0，谁吃到命中退化成枚举顺序）");
            }
            // 🔴 2026-10-09（A221④）：改成**按窗记账** —— 只认**这一颗**吸收层节点上的标记，
            //    不再读那个全局累积计数器（`MenuDraw.AbsorbTierWarns` 已删）。
            //    改坏法：把这一扇窗的档传错 ⇒ **只有本窗**红，且文案带**这一颗节点当时**的告警正文。
            bool aWarned = AbsorbTierWarned(node, out string aw);
            chk(!aWarned,
                $"{what}：`MenuDraw.Absorb` 对**这一颗**吸收层**没报过档位告警**（按窗记账 —— 只认这颗节点上的标记，"
                + "不受别的窗影响）" + (aWarned ? "；⚠️ 实得告警：" + aw : ""));

            // ⑤⑥ 两条行为（互为对照）
            var pl = PointerLayer.Instance;
            chk(pl != null, $"{what}：场景里有指针层（没有的话下面两条等于没查）");
            if (pl == null) return;
            float ccx = (x1 + x2) * 0.5f, ccy = (y1 + y2) * 0.5f;
            // 候选点：**原版矩形中心**优先 → 中心外一圈(±80) → 最后**贴着四条边内缩的那一圈**
            // （面板的边框那一圈通常没有内容件；例：练习窗选卡组那一列中间**全被卡组格盖住**，
            //  只有左边距那 25px 是空的）。⛔ 候选是**固定**的（不扫描全图）⇒ 点了哪儿可复现。
            // ⚠️ 用定长数组而不是 `List<Vector2>`：本文件只 `using UnityEngine`（⛔ 别为这一行去动 using 块），
            //    而**候选的序与取值与 5 份副本逐位相同**（中心 1 + 8 向 + 每档 8 条）。
            var cand = new Vector2[9 + AbsorbEdgeInset.Length * 8];
            int cn = 0;
            cand[cn++] = new Vector2(0f, 0f);
            cand[cn++] = new Vector2(0f, -80f); cand[cn++] = new Vector2(0f, 80f);
            cand[cn++] = new Vector2(-80f, 0f); cand[cn++] = new Vector2(80f, 0f);
            cand[cn++] = new Vector2(-80f, -80f); cand[cn++] = new Vector2(80f, -80f);
            cand[cn++] = new Vector2(-80f, 80f); cand[cn++] = new Vector2(80f, 80f);
            for (int k = 0; k < AbsorbEdgeInset.Length; k++)
            {
                float e = AbsorbEdgeInset[k];
                cand[cn++] = new Vector2(x1 + e - ccx, y1 + e - ccy); cand[cn++] = new Vector2(x2 - e - ccx, y1 + e - ccy);
                cand[cn++] = new Vector2(x1 + e - ccx, y2 - e - ccy); cand[cn++] = new Vector2(x2 - e - ccx, y2 - e - ccy);
                cand[cn++] = new Vector2(x1 + e - ccx, 0f);           cand[cn++] = new Vector2(x2 - e - ccx, 0f);
                cand[cn++] = new Vector2(0f, y1 + e - ccy);           cand[cn++] = new Vector2(0f, y2 - e - ccy);
            }
            float px = 0f, py = 0f; bool found = false;
            for (int i = 0; i < cn && !found; i++)
            {
                float tx = ccx + cand[i].x, ty = ccy + cand[i].y;
                if (tx <= x1 + 3f || tx >= x2 - 3f || ty <= y1 + 3f || ty >= y2 - 3f) continue;   // 必须落在**原版**矩形里
                if (tx < 2f || tx > 1918f || ty < 2f || ty > 1078f) continue;                    // 而且**在屏幕里**（玩家点不到屏外的点）
                var hb = pl.ButtonAt(tx, ty);
                if (hb != null && hb.absorbOnly) { px = tx; py = ty; found = true; }
            }
            // 兜底：上面那圈**全都撞上内容件**时，按 **40px 固定步长**在矩形里走一遍（确定性 —— 不是随机），
            // 取第一个「命中是吸收层」的点。⚠️ 它只决定**点哪儿**，不参与任何期望值。
            for (float gy = y1 + 4f; gy <= y2 - 4f && !found; gy += 40f)
                for (float gx = x1 + 4f; gx <= x2 - 4f && !found; gx += 40f)
                {
                    if (gx < 2f || gx > 1918f || gy < 2f || gy > 1078f) continue;
                    var hbg = pl.ButtonAt(gx, gy);
                    if (hbg != null && hbg.absorbOnly) { px = gx; py = gy; found = true; }
                }
            chk(found, $"{what}：**原版面板矩形以内找得到一个点、它的命中是吸收层**"
                       + "（找不到 ⇒ 窗内空白处没吃下这一下，射线会穿到压暗层上 ⇒ A94 那个缺陷还在）");
            if (!found) return;
            chk(state != null && state() == WindowState.Open, $"{what}：（前提）这一刻窗是开着的");
            if (state == null) return;
            chk(pl.ClickAt(px, py), $"{what}：点面板（真路径 `PointerLayer.ClickAt`，实点 ({px:F1},{py:F1})）");
            chk(state() == WindowState.Open,
                $"{what}：**点面板 ⇒ 窗不关**（原版面板那颗 `m_RaycastTarget = 1` 的 `Image` 吃掉了这一下）");
            // 点面板外：**钉死屏幕左上角 (5,5)**。
            // 🔴 **为什么偏偏是 (5,5)，而且不许改成「扫一圈找第一个命中压暗层的点」**（2026-10-06 FIX-1）：
            //    · 原版 `Deck info Popup > Warlord Image` 那颗 `Image` 的 rect 是
            //      `−108.98,−33.99 → 999.02,1074`（1108²）—— **它确实盖着 (5,5)**；但它带
            //      `m_RaycastPadding = (246.8, 84.44, 338.6, 132.38)`，**正 = 往里缩**（见实现侧注释），
            //      ⇒ 原版的**命中区**只剩 `137.82,98.39 → 660.42,989.56` ⇒ 原版在 (5,5) 命中的就是压暗层。
            //    · ⛔ 若改成「四角/四边按固定顺序扫，取第一个命中本窗压暗层的点」：立绘命中区一旦
            //      **又变回过大**（= 我们刚修掉的那个缺陷），搜索会从 `(1915,5)` 之类**绕过去**、
            //      照样绿 ⇒ 这一条就再也查不出那个缺陷了（本工程那一族「弱断言分不出两种状态」）。
            //      ⇒ **选点的判据是原版 prefab，不是「扫到一个能用的」** —— 点钉死、期望钉死。
            //    · 打印实测点与实测命中名（下面那条），出红时能直接看出「是被谁吃掉的」。
            const float OutX = 5f, OutY = 5f;
            var oHit = pl.ButtonAt(OutX, OutY);
            // 🔴 **判据 = 两条合起来**，⛔ 不许再写成「非吸收层 ∧ 属于本窗」那种**分不出两种状态**的弱条件
            //    —— 旧写法下 `WarlordHit`（立绘命中区）三条全满足、**照样绿**，正是它把这个缺陷放过去了：
            //      · `oHit.transform.IsChildOf(winRoot)` = **是这一扇自己的**命中区（别家的窗顶掉它就红）；
            //      · `MenuDraw.WasShadeHit(oHit.transform)` = **是压暗层那一颗**（`ShadeHit` 建的，
            //        按节点上的标记认、**不按名字认** —— 本工程三扇窗里这颗节点**两个名字**：
            //        卡组信息窗/导入卡组窗叫 `BackgroundHit`、**聊天窗叫 `CloseHit`**（名字是各调用点自己传的
            //        `MenuDraw.ShadeHit(..., name)` 形参）⇒ 按名字写 `Find("BackgroundHit")` 会把聊天窗那条**误判成红**）。
            //      ⛔ **别只写 `WasShadeHit`**：它认的是「是不是压暗层那颗」、**不认「是哪一扇的」**。
            chk(oHit != null && oHit.transform.IsChildOf(winRoot) && WasShadeHit(oHit.transform),
                $"{what}：**({OutX:F0},{OutY:F0}) 命中的就是这扇窗自己的压暗层那一颗**"
                + "（立绘命中区 / 吸收层 / 别家的窗把它顶掉时**这条红** —— 旧写法分辨不出，就是它放过了 A94）"
                + "（实得 `" + (oHit != null ? oHit.name : "<null>") + "`"
                + (oHit == null ? " = **什么都没命中**"
                   : !oHit.transform.IsChildOf(winRoot) ? " = **别家的窗**"
                   : !WasShadeHit(oHit.transform) ? " = **本窗的，但不是压暗层那一颗**" : "")
                + "）");
            chk(pl.ClickAt(OutX, OutY), $"{what}：点面板外 ({OutX:F0},{OutY:F0})（真路径）");
            chk(state() == WindowState.Closed, $"{what}：**点面板外 ⇒ 关窗**（两条互为对照才分得出）");
        }

        /// <summary>`CheckAbsorbRule` 贴边候选的**内缩**距离（px，固定三档；见那段注释）。
        /// 🔴 **2026-10-15（A825）起只有这一份**：上一版 5 个宿主各有一份同值的 `EdgeInset`，
        /// 而它们**只被各自那份 `CheckAbsorbRule` 读**。
        /// ⚠️ **`Editor/{CollectionScene,RewardsScene,SettingsScene,ShopScene}.cs` 那 4 份已随副本一起删**；
        /// ⛔ **`Editor/MainMenuScene.cs` 那一份【还在】**（那一轮该文件被另一个写手占着 ⇒ 它的 `CheckAbsorbRule`
        /// 也**还没收口**，仍读自己那份 `EdgeInset`）—— 下次收那一份时把它一并删掉。</summary>
        static readonly float[] AbsorbEdgeInset = { 6f, 20f, 40f };

        /// <summary>按名字在 `parent` 子树里找第一个节点（含未激活）—— `CheckAbsorbRule` 第 ① 步
        /// 「窗根的直接子件取不到 ⇒ 再递归找」那一条退路用的。
        /// ⚠️ **这是 5 个宿主各自那个私有 `FindChild` 的一份副本**（`Editor/*Scene.cs` 里各一份，
        /// 逐字相同；`Shell/MenuWindowBase.cs` 与 `Core/` 下**都没有**公共的同名件可借 —— 2026-10-15 实读）。
        /// ⛔ 别在这上面加「只找直接子件」之类的变体：直接子件那一步是上面 `winRoot.Find(nodeName)`。</summary>
        static Transform FindChildIn(Transform parent, string name)
        {
            if (parent == null) return null;
            foreach (var t in parent.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        // 🔴 **2026-10-10（A254②）删掉了一个死件**：原来这里有个
        //    `public static ImageQuad Button(parent, name, art, r, q, onClick, …)`（2026-10-03 A17 加的「一步到位」版本）。
        //    删它的两条理由：
        //      ① **全工程零调用点**（`grep "\\bButton("` 实测只剩定义那一行）；
        //      ② 它内部**两半 pad 路径不一致** —— `Rect(...)` 不收 `maskPad`、`Hit(...)` 收
        //         ⇒ 谁用了就会拿到「视觉掩码没缩、命中掩码缩了」的**半套**（正是 A188 那条口径的尸体）。
        //    ⛔ **别照老写法再长回来** —— 要「画底 + 命中区一次做完」就现写 `Rect` + `Hit` 两步（本仓现在一律这么写）。
        //    判据 → `项目任务.md` §三 第 29 条 **A254**；原件在 `git log` 里（`git show HEAD:…`）。

        // ============================================================ 卡组格（两页共用）
        //
        // 🔴 **2026-09-24 收口**：原版 **`Collection Deck`**（收藏窗 Deck 页）与
        //    **`Collection Deck With Highlight`**（`Deck Selection Popup`）是**同一份 prefab 几何的两个变体**
        //    —— 逐个字段 diff 过，**唯一差别是根组件的 `useSelectedHighlight`（1 / 0）**，其余差异全是子引用 pid。
        //    ⇒ 两处画法**只能有一份**（CLAUDE.md §三）。出处：`资料/普查产出_0923/A2_Deck页.md` §三
        //    与 `A1_外壳与弹窗.md` §3；2026-09-24 亲核：逐项**吻合到 &lt;0.5px**。
        //
        // 作者尺寸 **250×405**，RSR 把它们缩到 **225×364.5**（`_cellWidth/_cellHeight`）⇒ 内部每一件都 **×0.9**。

        /// <summary>格内各件相对格左上的比例（作者 250×405 下的值 ×0.9 已在调用处用 `K`）。</summary>
        public const float DeckCellK = 0.9f;
        /// <summary>原版 `Frame 40K_bt_deck` 在作者系里的矩形（246×368 @ (2,17)）。</summary>
        public const float DcFrameX = 2f, DcFrameY = 17f, DcFrameW = 246f, DcFrameH = 368f;
        /// <summary>卡背图（`Deck Image`，228×306 @ (11,26.1)）。</summary>
        public const float DcBackX = 11f, DcBackY = 26.1f, DcBackW = 228f, DcBackH = 306f;
        /// <summary>`Deck Name`（fs32，@ (20,344.2) 宽 210）。</summary>
        public const float DcNameX = 20f, DcNameY = 344.2f, DcNameW = 210f, DcNameH = 35.8f, DcNamePx = 32f;
        /// <summary>阵营图标 **84.5×85.7** @ **(-10.5, 273.7)**（作者系 250×405 下的值）。
        /// 🔴 **2026-09-26 就地更正**：原来写「贴右上内缩 8」、并拿 `r.x2` 当基准 —— **那是错的**，原版在**左下**：
        /// `资料/说明书/04_界面UI/卡组界面说明书.md:56` 原文「`Faction Icon [-10,274 85x86]` ← **左下阵营图标**」；
        /// `资料/普查产出_0923/A2_Deck页.md:123` 给的是 `-10.5,273.7,84.5,85.7`（`工具/menu_rect.py` 实读同值）。
        /// 这一处**从来没有任何断言盯过** ⇒ 错到现在才被发现。</summary>
        public const float DcFacX = -10.5f, DcFacY = 273.7f, DcFacW = 84.5f, DcFacH = 85.7f;
        /// <summary>难度角标 `DificultyLevel`（**原版拼错了**）**84.93×84.01** @ **(159.19, 15.74)**（作者系，**右上**）。
        /// 出厂 `INACT` —— 由 `DeckCollectionDisplay.displayDifficultyLabel` 运行时打开（**预组页 = 1 · 我的卡组页 = 0**）。</summary>
        public const float DcDiffX = 159.19f, DcDiffY = 15.74f, DcDiffW = 84.93f, DcDiffH = 84.01f;
        /// <summary>模式图标 `Game Mode Icon` **84.5×85.75** @ **(170.5, 273.68)**（作者系，**右下**）。</summary>
        public const float DcModeX = 170.5f, DcModeY = 273.68f, DcModeW = 84.5f, DcModeH = 85.75f;

        /// <summary>难度四档 → 三张图（`CollectionDeck__Config.c:117-136`）：`0/5 → Gallons_1` · `10 → _2` · `15 → _3`，其余给 null（原版也是 null）。</summary>
        public static string DifficultyMarkFile(int difficulty)
        {
            switch (difficulty)
            {
                case 0: case 5: return "Menu_Icon_Gallons_1";
                case 10: return "Menu_Icon_Gallons_2";
                case 15: return "Menu_Icon_Gallons_3";
                default: return null;
            }
        }

        /// <summary>模式 → 图标（原版 `ScriptableObjectsCollectionsUtilities.GetGameModeIcon(playMode)`，
        /// `DeckDrawer__Draw.c` 用 `PrebuiltDeck.gameMode`）。只认我们有的两张；其余给 null ⇒ **整层不建**
        /// （对应原版那句 `enabled = (icon != null)`，`资料/普查产出_0923/A2_Deck页.md:139`）。</summary>
        public static string GameModeIconFile(int gameMode)
        {
            switch (gameMode)
            {
                case 0: return "40k_gamemode_icon_classic";    // `PlayModes.Classic`
                case 13: return "40k_gamemode_icon_skirmish";  // `PlayModes.Skirmish`
                default: return null;
            }
        }
        /// <summary>选中高亮 `Highlight Rounded Square` **289.8×427.3**，往格左上偏 (−1.4, −1.4)。</summary>
        public const float DcHiW = 289.8f, DcHiH = 427.3f, DcHiOff = -1.4f;

        /// <summary>🆕 **2026-10-11（A198③）：`DeckCell` 的 `GameWindow` 包装** —— 裁切与内缩**都取本窗**
        /// （`win.Clip` → `clip` · `win.ClipPad` → `maskPad`），调用点**不必**再自己写
        /// `clip:` / `maskPad: ClipPad`（那两样本来就是全族的状态，见 `GameWindow.ClipPad` 的注释）。
        ///
        /// <para>🔴 **为什么要有它（判据 = 原版模型）**：原版这类裁切/内缩**不长在窗口上，而是每个视口节点
        /// 自己挂的 `RectMask2D`**（`m_Padding` / `m_Softness` 都在组件上）⇒ **无 mask 即无 padding**。
        /// 这条包装把 pad 与 `Clip` **绑成一对往下传**：`Hit`/`DeckCell` 里那句
        /// `ClipRect(r, PaddedClip(clip, maskPad), …)` 的 `PaddedClip` 首句就是
        /// `!clip.HasValue ⇒ return clip` ⇒ **本窗没设 `Clip` 时 pad 一个字节都不生效**
        /// （= 原版「没有 mask 就没有 padding」那一支；见 `PaddedClip` 与 `GameWindow.ClipPad` 的注释）。
        ///
        /// <para>🔴 **2026-10-12 就地订正（A198② 阶段 1 · 铁律 5）**：本节原来写「⛔『我就是要不裁』的显式写法 =
        /// 走【裸的那个重载】并显式写 `clip: null`」—— **`clip: null` 的语义从本批次起不再是「不裁」**：
        /// 它现在是「**没有显式覆盖**」⇒ 交给**父链上最近的 `ViewportClip` 节点**（`ViewportClip.Resolve` 第 2 支）。
        /// ⚠️ **今天仍然等价于「不裁」**（阶段 1 全仓一个节点都不挂 ⇒ 恒落到第 3 支），
        /// 但**阶段 2 挂上节点之后**，下面这两个 `clip: null` 的调用点、以及 `win == null` 那条警告的措辞，
        /// 都要**重新看一遍**。两种意图（用本窗 / 用调用点自己那一份）**照旧靠重载区分**，⛔ 别再加第三种写法。
        /// 📌 **「我就是显式不裁」今天【没有】表达方式**（`PxRect?` 的 `null` 分不出「没给」与「给的就是 null」）——
        /// 真要它得给 `clip` 换一个能分三态的载体，那是**调度台裁定的事**，
        /// ⛔ 别在这里顺手发明一个「哨兵矩形」（那种写法会静默地把真矩形当成不裁）。</para>
        /// ⚠️ **`clip` 与 `maskPad` 在裸重载里是「成对」语义**：只传 `clip`、不传 `maskPad` ⇒ pad 恒 0
        /// （今天两处调用点就是这一档：`CollectionWindow.BuildDeckCell` · `DeckSelectionPopup`，
        /// 两处原版视口的 `m_Padding` 实测都是 (0,0,0,0) ⇒ 本来就该是 0）。</para>
        /// ⚠️ **`win == null` 是调用方写错了**（不是「不裁」的合法写法 —— 那个用上面的裸重载写）：
        /// 出声一次并按「不裁」走，⛔ 不静默。</summary>
        public static Transform DeckCell(GameWindow win, Transform parent, string name, PxRect r,
                                         CollectionData.DeckInfo info, bool selected,
                                         int q, int qText, int qOverlay, int qHit, System.Action onClick,
                                         int? gameMode = null, int? difficulty = null, bool showDifficulty = false)
        {
            if (win == null)
            {
                if (DeckCellNullWindow++ < 3)
                    Debug.LogWarning("[MenuDraw] `DeckCell(win, …)` 收到了 **null 窗口** —— 拿不到本窗的 "
                                   + "`Clip` / `ClipPad` ⇒ 这一格**不按本窗裁、不内缩**。要「按本窗裁」就传窗口；"
                                   + "⚠️ 要走「不显式覆盖、由父链上的 `ViewportClip` 节点说了算」那一支，"
                                   + "**只有裸重载的 `clip: null` 这一个写法**（⚠️ 它**不等于**「显式不裁」，"
                                   + "见 `DeckCell(win, …)` 的注释里那条订正）。");
                return DeckCell(parent, name, r, info, selected, q, qText, qOverlay, qHit, onClick,
                                null, gameMode, difficulty, showDifficulty, Vector4.zero);
            }
            return DeckCell(parent, name, r, info, selected, q, qText, qOverlay, qHit, onClick,
                            win.Clip, gameMode, difficulty, showDifficulty, win.ClipPad);
        }

        /// <summary>`DeckCell(win, …)` 收到 null 窗口的次数（只用来**限流那条警告**，不是缺陷计数）。</summary>
        static int DeckCellNullWindow;

        /// <summary>画**一格卡组**（`r` = 已按缩放算好的显示矩形）。
        /// <paramref name="selected"/> = 画金框（原版 `Highlight Rounded Square`，色 (1,.773,0)）。
        /// <para>🆕 **2026-09-26：补上原版 `&lt;Deck>` 下本来就有、我们此前漏画的两层** ——
        /// **难度角标**（`showDifficulty` 为真才画，对应原版 `DeckCollectionDisplay.displayDifficultyLabel`）
        /// 与**模式图标**（`gameMode` 给了才画；图取不到就整层不建 = 原版那句 `enabled = (icon != null)`）。
        /// 两个参数都给 `null` = 这一格没有这两个概念（收藏窗/我的卡组就是这样）——
        /// 原版对「我的卡组」页也是把难度角标的总开关关掉的。</para>
        /// 🔴 同时**修掉阵营图标的位置**（原来画在**右上**、原版在**左下**，见 `DcFacX` 的注释）。
        /// <para>🆕 **2026-10-03**：`clip` 现在**也管点击区** —— 整格在视口外 ⇒ 连 `Hit` 一起不建，
        /// 压在边上的 ⇒ 命中区截到视口内（判据同 `Hit`：原版 `RectMask2D` 的射线那一面）。</para>
        /// 🔴 **2026-10-11（A198③）**：`clip` 与 `maskPad` 在这一支里**全由调用点自己给**
        /// （本重载**不看**任何窗口）—— 「用本窗的 `Clip` + `ClipPad`」走
        /// <see cref="DeckCell(GameWindow,Transform,string,PxRect,CollectionData.DeckInfo,bool,int,int,int,int,System.Action,int?,int?,bool)"/>
        /// 那个重载；**`clip: null` = 「不显式覆盖」**（⚠️ **2026-10-12 就地订正**：原来这一行写的是
        /// 「『我就是要不裁』的显式写法」—— `null` 分不出「没给」与「给的就是 null」，见上面那个重载的注释）
        /// ⇒ 从此起**父链上最近的 `ViewportClip` 节点说了算**（`ViewportClip.Resolve` 第 2 支；
        /// 阶段 1 无节点 ⇒ 与旧行为逐位相同）。
        /// （`PaddedClip(clip, maskPad)` 首句 `!clip.HasValue ⇒ return clip` ⇒ **连带 pad 一起不生效**，
        /// 与原版「无 mask 即无 padding」一致 —— ⚠️ 那一句判的是**解析之后的** `clip`：节点给了框就有效）。</summary>
        public static Transform DeckCell(Transform parent, string name, PxRect r, CollectionData.DeckInfo info,
                                         bool selected, int q, int qText, int qOverlay, int qHit,
                                         System.Action onClick, PxRect? clip = null,
                                         int? gameMode = null, int? difficulty = null, bool showDifficulty = false,
                                         Vector4 maskPad = default(Vector4))
        {
            const float K = DeckCellK;
            // 🔴 **2026-10-12（A198② 阶段 1）：取裁切状态走【一处】共用解析**（`ViewportClip.Resolve`）——
            //    ⚠️ **本函数里两条路要的是两份不同的视图，⛔ 别合成一份**（A188 的硬约束就在这里落地）：
            //      · 渲染 / 裁检 / 文字那几层吃 **`clipR` = `RenderClip`（= `V − pad`）**；
            //      · 命中那一层（末尾那句 `ClipRect(r, PaddedClip(clip, maskPad), …)`）吃 **裸框 + pad**、
            //        由它自己缩 ⇒ **⛔ 别把 `clipR` 喂进去**（pad 会缩两次）。
            //    ⚠️ **旧路逐位不变**：形参非空时 `clipR` 逐位等于形参 `clip`，
            //      而 `clip`/`maskPad` 逐位等于原来的形参（`PaddedClip(·, Vector4.zero)` 首句早退）。
            //    ⚠️ 本函数**不吃软边**（卡名那一路原来就传 `Vector2.zero`、图那几层压根没传）——
            //      原版这两处视口的 `m_Softness` 实测都是 `(0,0)`（判据 → V8 §A198② ②）
            //      ⇒ 阶段 2 挂节点时**别顺手给这几层接软边**（那是改行为，不是补缺口）。
            var _st = ViewportClip.Resolve(parent, clip, Vector2.zero, maskPad);
            var clipR = _st.RenderClip;      // 渲染/文字/裁检那一份
            clip = _st.Clip;                 // 命中那一份（裸框 —— 见上面第二条；本地名沿用，末尾那句不动）
            maskPad = _st.Pad;
            var cell = Node(parent, name, r);

            Rect(cell, CardArt.MenuUi("40K_bt_deck"),
                 new PxRect(r.x1 + DcFrameX * K, r.y1 + DcFrameY * K,
                            r.x1 + (DcFrameX + DcFrameW) * K, r.y1 + (DcFrameY + DcFrameH) * K),
                 "Frame", q, null, false, clipR);

            // ✅ **2026-09-24 起这里画的是「玩家选的卡背」**（`PlayerDeck.CardbackId`，
            //    在卡组编辑的 Cosmetics 页里右键选）；**没选过**的卡组退回该阵营的默认卡背 ——
            //    与原版一致（`CardDeck.GetDeckCardback()`：`cardbackId` 空 ⇒ `GetDefaultCardback(army)`）。
            //    原来那句「我们挑的 / 没有数据源」已随第 46 行做完而作废。
            var back = CardArt.DeckCardback(info.CardbackId, info.Faction);
            if (back != null)
                Rect(cell, back,
                     new PxRect(r.x1 + DcBackX * K, r.y1 + DcBackY * K,
                                r.x1 + (DcBackX + DcBackW) * K, r.y1 + (DcBackY + DcBackH) * K),
                     "CardBack", q, null, false, clipR);

            // 卡名：⚠️ 文字没法像图那样截 uv ⇒ **按原版 `RectMask2D` 切成半个字**（`ClipText`）
            //（🆕 2026-10-04：此前只做「**整块**在视口外就不建」、压在视口边上的字照画出去 —— 那条缺口已补）
            var nameR = new PxRect(r.x1 + DcNameX * K, r.y1 + DcNameY * K,
                                   r.x1 + (DcNameX + DcNameW) * K, r.y1 + (DcNameY + DcNameH) * K);
            if (Visible(nameR, clipR))
            {
                var nl = Text(cell, nameR, info.Name, Color.white, "Deck Name", DcNamePx * K, qText);
                if (clipR.HasValue) ClipText(nl, clipR, Vector2.zero);   // 「整块在框外」也由 `ClipText` 兜底（切到 0 宽）
            }

            if (!string.IsNullOrEmpty(info.Faction))
                // ⚠️ 走 `CardArt.MenuUi`（三级兜底 `ui_menu/ → ui_deck/ → ui/`）—— 与收藏窗那边原来那条路一致
                // 🔴 位置 = **左下**（作者系 `-10.5, 273.7`），**别再改回右上**（2026-09-26 更正，见 `DcFacX` 注释）
                Rect(cell, CardArt.MenuUi(DeckRuntime.FactionIcon(info.Faction)),
                     new PxRect(r.x1 + DcFacX * K, r.y1 + DcFacY * K,
                                r.x1 + (DcFacX + DcFacW) * K, r.y1 + (DcFacY + DcFacH) * K),
                     "Faction", q, null, true, clipR);

            // 🆕 **模式图标**（作者系 `170.5, 273.68`，右下）—— 原版 `enabled = (icon != null)`：
            //    图取不到就**整层不建**（`Rect` 遇 null 直接 return null，天然满足）
            if (gameMode.HasValue)
                Rect(cell, CardArt.MenuUi(GameModeIconFile(gameMode.Value)),
                     new PxRect(r.x1 + DcModeX * K, r.y1 + DcModeY * K,
                                r.x1 + (DcModeX + DcModeW) * K, r.y1 + (DcModeY + DcModeH) * K),
                     "Game Mode Icon", q, null, true, clipR);

            // 🆕 **难度角标**（作者系 `159.19, 15.74`，右上）—— 四档三张图（`0/5 一条杠 · 10 两条 · 15 三条`）。
            //    ⚠️ 原版节点名叫 `DificultyLevel`（**拼错了**，照抄别改，断言要按这个名字找）
            if (showDifficulty && difficulty.HasValue)
                Rect(cell, CardArt.MenuUi(DifficultyMarkFile(difficulty.Value)),
                     new PxRect(r.x1 + DcDiffX * K, r.y1 + DcDiffY * K,
                                r.x1 + (DcDiffX + DcDiffW) * K, r.y1 + (DcDiffY + DcDiffH) * K),
                     "DificultyLevel", qOverlay, null, true, clipR);

            if (selected)
                Rect(cell, CardArt.MenuUi("Highlight_Rounded_Square"),
                     new PxRect(r.x1 + DcHiOff, r.y1 + DcHiOff, r.x1 + DcHiW * K + DcHiOff, r.y1 + DcHiH * K + DcHiOff),
                     "Highlight Rounded Square", qOverlay, new Color(1f, 0.773f, 0f, 1f));

            // 点击区：**视口外的不建、压在视口边上的截到视口内** —— 判据与 `Hit` 同一条
            //（原版 `RectMask2D` 的**射线那一面**：滚出视口的格子**点不到**，见 `ClipRect` 的注释）。
            // ✅ **2026-10-04（A26）修掉的那处真缺陷**：这一颗原来只建了个**裸节点**（`Node`、不带 quad），
            //    而 `PointerLayer.CollectHits` 取的是「按钮下第一个 `ImageQuad`」（`Shell/PointerLayer.cs` 的 `CollectHits`）
            //    ⇒ **裸节点进不了命中表** ⇒ 收藏窗卡组页、选卡组窗里的卡组格**真鼠标点不动**
            //    （`WindowButton.onClick` 在着、自检直调 `wb.Click()` 也过 —— 所以自检当年全绿）。
            //    ⇒ 照 `Hit` 的写法补一颗**透明 quad**（`MakeHitQuad`，纯色件必须传 `CardArt.Solid()`）。
            //    ⚠️ **节点仍摆在交集块的中心**（不是父原点）—— 既有断言量的是**这个节点的位置**
            //    （`Editor/CollectionScene.cs:505-513`「命中区中心 = 露出来那块的中心」）⇒ 换摆法 = 改行为。
            //    ⚠️ **这会改掉那两扇窗的点击面**（原来整格都点不动）⇒ 自检里单开一段按**真命中路**
            //    （`PointerLayer.ButtonAt`）验它，别拿 `wb.Click()` 自证。
            if (onClick != null)
            {
                PxRect hr;
                // 🔴 **`maskPad`（= 原版 `RectMask2D.m_Padding`）缩的是 `clip`、⛔ 不是命中区自己的矩形**
                //    —— 判据（两关都要过）与两个模型之差 → `Hit` 的 `maskPad` 注释（2026-10-08 · A188）。
                // ⚠️ **今天本形参在【所有】调用点上都是全 0**（`CollectionWindow.BuildDeckCell` /
                //    `DeckSelectionPopup` 两处都吃默认值）⇒ 这一行与 `Hit` 那条路**行为逐字一致**；
                //    🆕 **2026-10-11（A198③）**：现在多了一个**喂得到它**的入口 = 本文件上面那个
                //    `DeckCell(GameWindow, …)` 包装（`win.ClipPad` → 本形参），但**上面那两处调用点还没改用它**
                //    ⇒ 今天生产上仍然全 0（那两处原版视口 pad 实测本来就都是 (0,0,0,0)）。
                //    改它只为「**同一个形参不许两套语义**」（CLAUDE.md §三：两处写同一条规则 = 迟早不一致）。
                //    ⚠️ **2026-10-04 就地订正（F2）**：收藏窗 Deck 页那份原版 mask 实测是 **(0,0,0,0)**
                //    （`Collection Menu Variant/…/Select Deck Tab/Decks Tab/…/Deck Scroll View/Viewport`）；
                //    这里原来写的 `(0,9.69,0,9.69)` 是**别人家的值**（`…/Searching Oponent Popup/Window`，
                //    见 `PaddedHitRect` 上面那段逐处实读表）⇒ **这一处不该给 `maskPad`**
                //    （照旧写法接线会在本来不吃 padding 的窗上加 padding）。
                if (ClipRect(r, PaddedClip(clip, maskPad), out hr))
                {
                    var hit = Node(cell, "Hit", hr);
                    MakeHitQuad(hit, hr, qHit, Vector3.zero);      // 节点已在矩形中心 ⇒ quad 摆 0
                    var wb = hit.gameObject.AddComponent<WindowButton>();
                    wb.onClick = onClick;
                }
            }
            return cell;
        }
    }

    /// <summary>🆕 **2026-10-04（A38②）：让「文字裁切」跟着之后的每一次重排走。**
    ///
    /// 🔴 **缺口是什么**：`MenuDraw.ClipText` 是**建的时候**裁一刀（TMP 逐字夹顶点 + 按同一仿射改 uv）。
    ///    之后任何一次 `Label.SetText` / 改字号 / 自动适配带来的重排，TMP 都会**重算 mesh**
    ///    ⇒ 我们那一刀被抹掉、压在视口边上的字**又画出去了**（**静默**：画面不对、断言也不会响）。
    ///
    /// 🔴 **判据（订的是 TMP 自己的事件，不是猜的）**：TMP 在**每次重排完、并且已经把新 mesh 写进 `Mesh` 之后**
    ///    发一条 `TMPro_EventManager.ON_TEXT_CHANGED(this)`（`Library/PackageCache/com.unity.ugui@…/
    ///    Runtime/TMP/TextMeshPro.cs:5047-5063`，源码原注释就是 *“Event indicating the text has been regenerated.”*）
    ///    ⇒ 我们订阅它、只认自己那一个 TMP，收到就**再裁一刀**。
    ///   ⚠️ **正因为事件是在重排【之后】发的**，重裁拿到的永远是**原始 mesh** ⇒ **不会把软边 alpha 一遍遍乘下去**。
    ///   ⛔ 别改成「`LateUpdate` 里无脑重裁」—— 那正是会越裁越暗的写法（静默、且只在软边区现形）。
    ///   🔴 **2026-10-08（A225-②）就地订正（铁律 5）：上面那两句「不会越裁越暗」的理由已经不必再靠它撑了** ——
    ///     `ClipTmpMesh` 现在写的是**绝对值**（基准 = `TMP_CharacterInfo.vertex_*.color`，
    ///     见 `BaseCornerAlpha`），**同一代 mesh 里裁几刀结果都一样**（幂等）。
    ///     ⇒ 「无脑重裁会变暗」这条隐患**从机制上没了**；下面 `Reclip()` 正是靠它才敢随手调。
    ///     ⛔ 但**别把幂等当许可证**去每帧重裁：那还是白跑一遍逐字循环（没有帧循环的批处理里更没意义）。
    ///
    /// 🔴 **2026-10-12（A435①）：本组件不再存「解析后的快照」，存的是【取状态的两个实参】。**
    ///    裁切状态现在**可以长在视口节点上**（`ViewportClip`）⇒ 光存 `Arm` 那一刻的 `PxRect` 是**取早了**：
    ///      · 那一格里 `MenuDraw.ClipText` 拿到的形参本来就是 `null`（= 「不显式覆盖」）⇒ 框是**当场解析**出来的，
    ///        把它冻成快照 = 从此**只认那一刻的父链**；
    ///      · 于是「保住快照」这条路有两个错法，**都不出声**：节点后挂/后迁 ⇒ **永远不裁**；
    ///        节点挪了/改尺寸 ⇒ **拿旧框裁**（字被切在错误的位置上）。
    ///     ⇒ 现在存 `_clipArg` / `_softArg`（= `ClipText` 的**调用方原样传进来**的那两份）+
    ///       重裁时**从 `_lb` 自己重新 `ViewportClip.Resolve`**（`Resolve` 是纯函数、不返 `null`）。
    ///     ⚠️ **不是为了「跟新框」**：框的来源**照旧只在两处**（显式形参 / 解析结果），本组件不引入第三份；
    ///        `Resolve` 的优先级（显式形参非空 ⇒ 赢、连父链都不走）原样保留 ⇒ 旧路径重裁出来的**逐位还是旧框**。
    ///     ⚠️ 节点态下重裁的结果**只有在节点真的动过**时才与 `Arm` 那一刻不同 —— 那正是要补的那一格。
    ///
    /// ⚠️ **本组件的边界（如实写）**：只覆盖 **TMP 那条后端**（`Label` 正常走的那条）。
    ///    点阵兜底那条**没有事件可订** ⇒ 它仍然只能靠「建完别再改」（那一档只在 TMP 资源缺失时才出现）。
    /// ⚠️ 批处理里没有帧循环 ⇒ **没有重排事件**：自检别拿它自证（要验就在 `Play` 里点一次，或直调 `MenuDraw.ClipTextNow`）。</summary>
    public class ClippedTextGuard : MonoBehaviour
    {
        Label _lb;
        TMPro.TextMeshPro _tmp;
        /// <summary>`ClipText` 的**调用方原样那一份** `clip`（`null` = **不显式覆盖** ⇒ 重裁时解析父链）。
        /// ⛔ 别改回「`Arm` 那一刻解析出来的 `PxRect`」—— 理由 → 类注释（A435①）。</summary>
        PxRect? _clipArg;
        /// <summary>同上，那份软边（解析时由 `Resolve` 决定最终值）。</summary>
        Vector2 _softArg;
        bool _on;

        /// <summary>记下「**按什么取裁切状态**」，等下一次重排重新解析 + 重裁。重复 `Arm` 只更新参数、**不重复订阅**。
        /// 🔴 **参数是 `ClipText` 收到的那两份原样**（`clip` 可空 = 不显式覆盖）；⛔ 别在这里先解析 —— 解析归 `CurClip`。
        /// ⚠️ 本方法**不碰**网格：重裁与建时那一刀走的是同一个 `ClipTextNow`（幂等，见类注释）。</summary>
        public void Arm(Label lb, PxRect? clip, Vector2 softPx)
        {
            _lb = lb; _clipArg = clip; _softArg = softPx;
            _tmp = lb != null ? lb.GetComponentInChildren<TMPro.TextMeshPro>() : null;
            if (_on) return;
            TMPro.TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
            _on = true;
        }

        /// <summary>🔴 **按「现在」的裁切状态**算这一刀该落在哪个框里（= 重新走一遍 `ViewportClip.Resolve`）。
        /// 实参 = `Arm` 记下的那两份（**不是** `Arm` 那一刻解析出来的框）⇒ 显式覆盖仍然覆盖、
        /// 「不显式覆盖」那一档跟**当下的**父链（节点后挂/挪动都跟得上，见类注释 A435①）。
        /// 返回 `false` = 这一刻**没有裁切**（没有显式形参、父链上也没有节点）⇒ 调用方**跳过**，
        /// ⛔ 别拿一个「空矩形」顶上（`ClipTextNow` 的 `PxRect` 是非空的 ⇒ 顶上就等于**把字全裁没**）。</summary>
        bool CurClip(out PxRect clip, out Vector2 soft)
        {
            clip = default(PxRect); soft = default(Vector2);
            if (_lb == null) return false;
            var st = ViewportClip.Resolve(_lb.transform, _clipArg, _softArg, default(Vector4));
            if (!st.RenderClip.HasValue) return false;
            clip = st.RenderClip.Value; soft = st.Softness;
            return true;
        }

        /// <summary>🆕 **2026-10-08（A225-②）**：**按「现在」的位置重裁一刀**（幂等，见类注释的订正）。
        ///
        /// <para>🔴 **为什么光有事件不够**：那一刀要落在**文字真正被画的位置**上，而 TMP 的重排与
        /// 「把文字摆到该在的地方」是**两件事** —— `Label.RefreshBounds()` 会在重排**之后**再挪一次
        /// TMP 子节点。事件驱动的重裁只能发生在 `ForceMeshUpdate()` 里（= **挪之前**）
        /// ⇒ 裁出来的渐隐带整体偏「新旧位置之差」（`资料/待办判据_1007.md` 的 A206 记的就是它），
        /// 实测症状：练习窗卡组格的名字条**一个顶点都没被削**（`MainMenuScene.Run` 那条「卡组名也吃软边」红）。
        /// ⇒ `Label.RefreshBounds()` 的**末句**调本函数（每一条定版面的路都从那儿收口）。</para>
        /// <para>⚠️ 它同时是「重排之后**没收到事件**」那种情况的兜底（快照/订阅出任何岔子都不会静默）。</para></summary>
        public void Reclip()
        {
            if (_lb == null) { OnDisable(); return; }      // 标签先没了 ⇒ 自己下岗（同 `OnTextChanged`）
            // 🔴 A435①：框**当场重新解析**（`CurClip`），⛔ 不是 `Arm` 那一刻冻下来的快照。
            PxRect c; Vector2 s;
            if (!CurClip(out c, out s)) return;            // 这一刻没有裁切 ⇒ 没什么可重裁的
            if (MenuDraw.ClipTextNow(_lb, c, s)) MenuDraw.TextReclipAfterPlace++;
        }

        /// <summary>摘订阅。⚠️ `DestroyImmediate` 也会走到这里（`MenuDraw.ClearChildren` 批处理下就是它）
        /// —— 忘了摘的话，静态事件表会一直攥着这块内存（而且它已经不在场景里了）。</summary>
        void OnDisable()
        {
            if (!_on) return;
            TMPro.TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
            _on = false;
        }

        void OnTextChanged(Object obj)
        {
            if (_lb == null) { OnDisable(); return; }     // 标签先没了 ⇒ 自己下岗
            if (obj != _tmp) return;                      // 全局事件：只认自己那一个 TMP
            PxRect c; Vector2 s;
            if (!CurClip(out c, out s)) return;           // 🔴 A435①：同 `Reclip()` —— 框当场重解析
            if (MenuDraw.ClipTextNow(_lb, c, s)) MenuDraw.TextClipReapplied++;
        }
    }
}
