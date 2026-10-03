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
        /// <summary>原版像素矩形中心 → **相对 `parent` 的局部坐标**（见文件头坑①）。</summary>
        public static Vector3 Local(Transform parent, float x1, float y1, float x2, float y2)
            => LayoutSpace.RectCenter(x1, y1, x2, y2) - (parent != null ? parent.position : Vector3.zero);

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
        /// 否则自检量不到、将来做点击/滚动也会算错）。</summary>
        public static Transform Node(Transform parent, string name, PxRect r)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = Local(parent, r.x1, r.y1, r.x2, r.y2);
            return t;
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
        /// ⚠️ 退化矩形（宽或高 ≤ 0.01）在**有裁切**时一律判不可见 —— 这是 `Rect` 原来就有的行为，别丢。</summary>
        public static bool ClipRect(PxRect r, PxRect? clip, out PxRect outRect)
        {
            outRect = r;
            if (!clip.HasValue) return true;
            if (r.W <= 0.01f || r.H <= 0.01f) return false;
            var c = clip.Value;
            // 整块在框内 ⇒ **原样返回 `r`**（不新建矩形 ⇒ 调用方能靠逐字段比判出「没截」）
            if (r.x1 >= c.x1 && r.x2 <= c.x2 && r.y1 >= c.y1 && r.y2 <= c.y2) return true;
            float x1 = Mathf.Max(r.x1, c.x1), x2 = Mathf.Min(r.x2, c.x2);
            float y1 = Mathf.Max(r.y1, c.y1), y2 = Mathf.Min(r.y2, c.y2);
            if (x2 <= x1 + 0.01f || y2 <= y1 + 0.01f) return false;
            outRect = new PxRect(x1, y1, x2, y2);
            return true;
        }

        /// <summary>两个 `PxRect` 逐字段相等没有（`PxRect` 是 `struct`、没重写 `Equals`；
        /// 用来判「`ClipRect` 有没有真的截掉一块」—— 自检也可以拿它量「没人动过这个矩形」）。</summary>
        public static bool SameRect(PxRect a, PxRect b)
            => a.x1 == b.x1 && a.x2 == b.x2 && a.y1 == b.y1 && a.y2 == b.y2;

        /// <summary>`SameRect` 的**带容差**版本（`ApplySoftEdges` 的「整块不切」那条路要用）。
        /// 🔴 **为什么不能直接用 `SameRect`**：那里比的两个矩形，一个是调用方给的、一个是
        /// `QuadRectPx(q)` **从世界坐标反推**回来的（隔着一次 `ToPixel`/`FromPixel` 的浮点往返，
        /// 误差在 ~1e-4 px 量级）⇒ 逐字段 `==` 会**假阴**，把「本来一致」判成「不一致」。
        /// 0.05px 的容差对画面无意义，但足以把往返误差挡在外面。</summary>
        static bool SameRectNear(PxRect a, PxRect b, float eps = 0.05f)
            => Mathf.Abs(a.x1 - b.x1) <= eps && Mathf.Abs(a.x2 - b.x2) <= eps
            && Mathf.Abs(a.y1 - b.y1) <= eps && Mathf.Abs(a.y2 - b.y2) <= eps;

        /// <summary>🔴 **「整块在框外 ⇒ 不建」的公共函数**（A25④ 收口 · 2026-10-04）——
        /// `true` = 还有可见部分（**不保证整块在框内**）、`false` = 整块在框外 ⇒ 调用方**一律不建**。
        /// 谁该用它：**建不出几何/uv 的那些件**（文字、纯逻辑节点）—— 图那一路走 `ClipRect`（它还要 `outRect`）。
        ///
        /// 🔴 **和 `ClipRect` 的关系（别把两者合并）**：判据同一条（两轴都判），差别只有一处 ——
        ///   `ClipRect` 多一条「**退化矩形**（宽或高 ≤ 0.01）在有裁切时一律判不可见」的守卫，
        ///   那是 `Rect` / `Nine` / `Hit` 那条路要的（那种尺寸建不出 quad）；
        ///   本函数**不引入**那条 —— 文字/节点建得出来，收口前的四处内联也从来没有这条。
        ///   ⇒ **逐字保留原有语义**（收口不许顺手改行为，铁律 12 的精神）。
        /// <para>⚠️ 这与 `MenuScroll.Intersects`（只判**滚动轴**）**不是同一件事** ——
        /// 那个只适合「按滚动方向整块剔除」的构建循环，别互相替代。</para></summary>
        public static bool Visible(PxRect r, PxRect? clip)
        {
            if (!clip.HasValue) return true;
            var c = clip.Value;
            return !(r.x2 <= c.x1 || r.x1 >= c.x2 || r.y2 <= c.y1 || r.y1 >= c.y2);
        }

        // ============================================================ `RectMask2D.m_Padding`（**只改射线那一面**）
        //
        // 🆕 **2026-10-04（A9/A15 尾巴）**：`Clip` 原来只是**裸矩形**，原版那个 `m_Padding` 我们**没建模**。
        //
        // 🔴 **判据（本地 UGUI 源码，逐行读过）**：`RectMask2D` 上那个 `m_Padding`
        //    （`Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/RectMask2D.cs:51,60-65`）
        //    **全文件只用在一处** —— `IsRaycastLocationValid`（同文件 `:178-185`）：
        //    `RectTransformUtility.RectangleContainsScreenPoint(rectTransform, sp, eventCamera, m_Padding)`。
        //    **渲染那一面（`PerformClipping` / `rootCanvasRect`）压根不读它** ⇒
        //    **padding 只影响「点不点得到」，不影响「画到哪儿」**（这正是它与 `m_Softness` 的分工：
        //    软边只改渲染、padding 只改射线）。
        //    ⇒ 我们这边：`ClipRect`（渲染 + 命中）**照旧不动**，只有**命中区**那一份要先过 padding。
        //
        // 🔴 **符号约定：正值 = 缩小，负值 = 扩大**（`padding` 的 (L,B,R,T) 依次把矩形四边往里推）。
        //    ⚠️ **仍 `[TODO-verify]`（铁律 3：别把推断写成「原版就是这样」）** —— 本地只有两条**旁证**：
        //    ① 惯例（UGUI 里 `m_RaycastPadding` / `RectMask2D.m_Padding` 是同一个 `offset` 语义）；
        //    ② `bundle_menus_assets_all` 里带 `m_RaycastPadding` 的 `Graphic` = **11880** 个，
        //       非零 **208** 个 —— 其中 **207 个是负的（全分量 ≤ 0）**、
        //       **有 1 个是全正的 `(246.8, 84.44, 338.6, 132.38)`**
        //       （`MonoBehaviour_-7131536541767857752.json`；⚠️ 2026-10-04 订正：原来写「208 个**全是**负数」，
        //       差这一个 —— 而**正负相反会让 `PaddedHitRect` 的四个符号全反**，所以这一个反例不能吞掉）。
        //    🔴 **引擎侧判据本地确实拿不到**（独立复核过）：`d:/2/tools/il2cpp_out/dump.cs:1123499-1123502` 里
        //       `RectangleContainsScreenPoint`（两个重载）与 `PointInRectangle` 的**方法体都是空的**；
        //       `工具/disasm_va.py` 反汇编 `PointInRectangle`（VA `0x1833A0F10`）只见 il2cpp 的 icall
        //       解析壳（改名 → `call rax`，那 90 条指令里没有任何浮点比较）⇒ C++ 实现在 `UnityPlayer.dll`、无符号。
        //    ⇒ **接线批里不许把这条符号当已定**；拿到反例（真 Play 点一次）就就地改这一行。
        //
        // ⚠️ **逐处不同、必须逐处实读**（全量表 `d:/4/_tmp_view/q1_rm2d.txt` ——
        //    **150（`bundle_menus_assets_all`）+ 1（`bundle_mainmenualwaysloaded_assets_all`）
        //    + 5（`bundle_generalgamewindows_assets_all`）= 156 个 mask**，就是那张表自己的三个表头；
        //    ⚠️ 2026-10-04 订正：原来写「222 个 mask」，**那个数没有出处** ⇒ 改成数出来的 156）。
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

        /// <summary>按原版 `RectMask2D.m_Padding` 把**命中区**的矩形缩/放一次（**只给命中区用**，见上面那段）。
        /// `pad` = UGUI 的 (x=Left, y=Bottom, z=Right, w=Top)；**正值缩小、负值扩大**。
        /// ⚠️ `PxRect` 是**左上原点、y 向下**，所以 `pad.y`（Bottom）是从 `y2` 往上收、`pad.w`（Top）是从 `y1` 往下收。</summary>
        public static PxRect PaddedHitRect(PxRect r, Vector4 pad)
        {
            if (pad == Vector4.zero) return r;
            var o = new PxRect(r.x1 + pad.x, r.y1 + pad.w, r.x2 - pad.z, r.y2 - pad.y);
            // 🆕 **2026-10-04（F9）：退化守卫** —— `pad` 比矩形还大时会算出「宽或高 ≤ 0」的矩形，
            //    它一路走到 `MakeHitQuad` → `SetAspect(负/0)` 造出**镜像 quad**，
            //    而 `PointerLayer.CollectHits` 的 `Abs(dx) > hw`（`hw < 0` ⇒ **恒真**）判不中
            //    ⇒ **这颗钮静默点不动**（`MakeHitQuad` 只在 `hq == null` 时出声，这条路上它不响）。
            //    ⚠️ `ClipRect` 的退化守卫兜不住这里：它在 `clip == null` 时**第一句就 `return true`**（`:74`），
            //    而 `MenuWindowBase.AddHit` 的 `Clip` 本来就可以是 null。
            //    ⇒ 按「**不扩**」处理（**退回原矩形** + 出声）—— 宁可让这一颗保持原样，也不造一颗点不动的钮。
            // 🔴 **这条守卫与正负号约定无关**（⚠️ **2026-10-04 措辞订正（R5）**：代码是 `||` —— **任一轴**退化就兜，
            //    原文写的「两轴都退化才兜」与实现相反）⇒ 无论 `[TODO-verify]` 那条最后判成
            //    「正 = 缩小」还是反过来，它都成立。⚠️ 但**符号约定本身仍未坐实**（见上面那段判据：
            //    207/208 负 + 那 1 个全正的反例 · 引擎侧读不到）—— 别把这条守卫当成符号已定。
            if (o.W <= 0.01f || o.H <= 0.01f)
            {
                PaddedHitDegenerates++;
                if (PaddedHitDegenerates <= 3)
                    Debug.LogWarning($"[MenuDraw] `m_Padding`({pad.x},{pad.y},{pad.z},{pad.w}) 相对命中区 "
                                   + $"{r.W:F1}×{r.H:F1} **太大了**（算出来 {o.W:F1}×{o.H:F1}）—— 按「**不扩**」处理"
                                   + "（退回原矩形）。不兜的话会建出一颗**镜像 quad** ⇒ 这颗钮**静默点不动**。"
                                   + "⚠️ `m_Padding` 的正负号约定本身仍 `[TODO-verify]`。");
                return r;
            }
            return o;
        }

        /// <summary>`PaddedHitRect` 撞上退化矩形的次数（非 0 = 有处 padding 比命中区还大，
        /// 已按「不扩」兜住 —— 但那个 pad 值多半本身就配错了，见上面那段逐处实读表）。
        /// 🔴 **2026-10-04 R-F 审查订正**：原文写「**自检断它 == 0**」—— **是假的**：全工程**一个读者都没有**
        /// （`Editor/*Scene.cs` 里 0 处），把它整段删掉 11 条自检一条都不会红。
        /// ⇒ **要么在接线批补一条断言，要么别在注释里声称有断言**（现在如实写：**暂无读者**）。</summary>
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

        /// <summary>一个 quad 在画布 px 里的矩形（世界 → px 只走 `LayoutSpace.ToPixel`，见 `ClipNineChildren`）。</summary>
        static PxRect QuadRectPx(ImageQuad q)
        {
            const float K = LayoutSpace.DesignPxH / LayoutSpace.DesignHeight;   // 108 px / 世界单位
            Vector2 cpx = LayoutSpace.ToPixel(q.transform.position);
            float hw = q.WorldW * K * 0.5f, hh = q.WorldH * K * 0.5f;
            return new PxRect(cpx.x - hw, cpx.y - hh, cpx.x + hw, cpx.y + hh);
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

        /// <summary>把一个 quad 按软边剖面处理：**必要时沿带的内沿切开**，每块的四角带上 alpha 斜坡。
        /// 最外层的 quad（`vis` = 它**已经硬裁过**的那块）**留在原节点上**（改成「含矩形中心的那一格」），
        /// 其余格建**子 quad**（同图/同队列/同 tint）。`softPx` 两个分量都 0 ⇒ 立刻返回（硬边 = 现有行为）。
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
            if (softPx.x <= 0f && softPx.y <= 0f) return;
            if (vis.W <= 0.01f || vis.H <= 0.01f) return;
            // 🆕 **2026-10-04（A38③）**：记下**上斜坡之前**的四角色 —— 重切时要先还原，
            //    否则 alpha 会在旧斜坡上再乘一遍（越裁越暗，静默）。见 `ReapplySoftEdges`。
            var baseCorners = CaptureCorners(q);

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
                if (q.SoftEdgeRebuild != null && !SameRectNear(QuadRectPx(q), vis)) PlaceCell(q, vis, vis, uv0);
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
                    var sub = ImageQuad.Create(q.transform, q.Texture,
                                               LayoutSpace.RectCenter(cell.x1, cell.y1, cell.x2, cell.y2) - q.transform.position,
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
        /// 返回 true = 真的动过（自检拿它断「这条路带电」）。</summary>
        public static bool ClipText(Label lb, PxRect? clip, Vector2 softPx)
        {
            if (lb == null || !clip.HasValue) return false;
            var c = clip.Value;
            if (softPx.x < 0f) softPx.x = 0f;
            if (softPx.y < 0f) softPx.y = 0f;
            bool ok = ClipTextNow(lb, c, softPx);
            // 🆕 **2026-10-04（A38②）：挂上「重排之后自动重裁」的守卫** —— 见 `ClippedTextGuard`。
            ArmTextGuard(lb, c, softPx);
            return ok;
        }

        /// <summary>「现在就裁一刀」，**不挂守卫**（守卫用它自动重裁；`ClipText` 是「裁 + 挂」）。
        /// 判据与实现全在上面那一段注释里；两条后端（TMP / 点阵兜底）各走一条。
        /// 拿不到网格时**出声**（`TextClipUnavailable` 计数 + 一条警告），不静默。</summary>
        public static bool ClipTextNow(Label lb, PxRect clip, Vector2 softPx)
        {
            if (lb == null) return false;
            if (softPx.x < 0f) softPx.x = 0f;
            if (softPx.y < 0f) softPx.y = 0f;
            var tmp = lb.GetComponentInChildren<TMPro.TextMeshPro>();
            if (tmp != null) return ClipTmpMesh(tmp, clip, softPx);
            var mf = lb.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null) return ClipQuadMesh(lb.transform, mf.sharedMesh, clip, softPx);
            TextClipUnavailable++;
            if (TextClipUnavailable <= 3)
                Debug.LogWarning($"[MenuDraw] 「{lb.name}」没有可裁的渲染网格（TMP 与点阵兜底都没建起来）"
                               + " —— 这一段文字**不会被裁到框内**（原版 `RectMask2D` 会）。");
            return false;
        }

        /// <summary>`ClipText` 拿不到网格的次数（自检可以断它 == 0）。</summary>
        public static int TextClipUnavailable;

        /// <summary>🆕 **重排之后自动重裁的次数**（A38②；自检可以断这条路**带电** —— 只断「挂上了守卫」
        /// 是不够的，那种断言改坏实现不会红）。</summary>
        public static int TextClipReapplied;

        static void ArmTextGuard(Label lb, PxRect clip, Vector2 softPx)
        {
            if (lb == null) return;
            // ⚠️ 批处理下也能挂（组件本身不依赖帧循环 —— 它靠 TMP「文字已重排」那个事件），
            //    但**没有帧循环就没有重排事件** ⇒ 自检里别指望它替你自证：要验就在 `Play` 里点一次，或者直调 `ClipTextNow`。
            var g = lb.GetComponent<ClippedTextGuard>();
            if (g == null) g = lb.gameObject.AddComponent<ClippedTextGuard>();
            g.Arm(lb, clip, softPx);
        }

        /// <summary>TMP 那条：逐字夹顶点 + 改 uv（+ 软边 alpha）。</summary>
        static bool ClipTmpMesh(TMPro.TextMeshPro tmp, PxRect clip, Vector2 softPx)
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
                int cut = 0;
                for (int k = 0; k < 4; k++) if (al[k] < 0.9999f) cut++;
                if (!moved && cut == 0) continue;                 // 一个字都不用动（含「框内、也不在带里」）
                any = true;
                if (moved)
                    for (int k = 0; k < 4; k++)
                    {
                        mesh.vertices[v + k] = p[k];
                        mesh.uvs0[v + k] = new Vector4(uv[k].x, uv[k].y, mesh.uvs0[v + k].z, mesh.uvs0[v + k].w);
                    }
                // 🔴 **软边对「没被切、但落在渐隐带里」的字**同样要削 alpha（原版掩码是按像素来的，
                //    与「这个字有没有被切」无关）—— 只把 alpha 乘上去，rgb 不动。
                if (cut > 0 && mesh.colors32 != null && v + 3 < mesh.colors32.Length)
                    for (int k = 0; k < 4; k++)
                    {
                        if (al[k] >= 0.9999f) continue;
                        var cc = mesh.colors32[v + k];
                        cc.a = (byte)Mathf.Clamp(Mathf.RoundToInt(cc.a * al[k]), 0, 255);
                        mesh.colors32[v + k] = cc;
                    }
            }
            if (any)
                tmp.UpdateVertexData(TMPro.TMP_VertexDataUpdateFlags.Vertices
                                   | TMPro.TMP_VertexDataUpdateFlags.Uv0
                                   | TMPro.TMP_VertexDataUpdateFlags.Colors32);
            return any;
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

        /// <summary>🔴 **四边形夹进裁切框的唯一一份实现**（TMP 与点阵两条后端共用）。
        /// `p` / `uv` 都是 4 个、顺序 **BL · TL · TR · BR**；`alphaOut` 回传四角的软边 alpha。
        /// 返回 false = 四角一个都不用动（调用方**别回写**）。
        /// 夹完之后 uv 按**同一个仿射关系**跟着走（`u = uL + (x−xL)/(xR−xL)·(uR−uL)`，
        /// `v = vT + (y−yT)/(yB−yT)·(vB−vT)` —— 注意 uv 的 v **自下而上**、而 px 的 y 向下 ⇒ 要翻）。</summary>
        static bool ClipQuad(Transform tr, Vector3[] p, Vector2[] uv, PxRect clip, Vector2 softPx, float[] alphaOut)
        {
            var q = new Vector2[4];
            for (int i = 0; i < 4; i++) q[i] = LayoutSpace.ToPixel(tr.TransformPoint(p[i]));
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
            var rootPos = root.transform.position;
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
                var lp = LayoutSpace.FromPixel(cr.CX, cr.CY) - rootPos;
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
        /// `(0, 0, 本块世界宽/节距, 本块世界高/节距)`（`ImageQuad.cs:293`）
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
            var rootPos = root.transform.position;
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
                var lp = LayoutSpace.FromPixel(cr.CX, cr.CY) - rootPos;
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
        /// 🔴 **别用 `SetFontSize(px/108)`** —— 那会大 2.7 倍；走 `SetGlyphHeight`。</summary>
        public static Label Text(Transform parent, PxRect r, string text, Color color, string name,
                                 float fontPx, int q, float wrapPx = 0f, float autoMinPx = 0f)
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
                    lb.SetAutoFitBox(LayoutSpace.Px(wrapPx), LayoutSpace.Px(r.H), autoMinPx, fontPx);
            }
            return lb;
        }

        /// <summary>限宽换行 + 可选自适应字号的一段文字（原版 `m_TextWrappingMode = 1` + autosize）。
        /// 🔴 **别用 `SetFontSize(px/108)`** —— 那会大 2.7 倍；`Text` 走的是 `SetGlyphHeight`。
        /// 🔴 2026-09-24 从 `MainMenuSubmenuWindow.TextBox` 收口过来（那边**转调**，行为一字未改）。</summary>
        public static Label TextBox(Transform parent, PxRect r, string text, Color color, string name,
                                    float fontPx, float autoMinPx = 0f, int q = QText)
        {
            var lb = Text(parent, r, text, color, name, fontPx, q);
            if (lb == null) return null;
            lb.SetWrapWidth(LayoutSpace.Px(r.W));
            if (autoMinPx > 0f && fontPx > autoMinPx)
                lb.SetAutoFitBox(LayoutSpace.Px(r.W), LayoutSpace.Px(r.H), autoMinPx, fontPx);
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
                                    Vector4 hitPad = default(Vector4))
        {
            PxRect hr;
            // 🔴 **先过 `RectMask2D.m_Padding` 再裁**（`hitPad`，**只改命中区**——原版渲染那一面不吃它，
            //    见 `PaddedHitRect` 上面那一段判据）。`hitPad` 全 0 时这就是原来那一行。
            // 整块在视口外 ⇒ **连节点一起不建**（返回 null；`AddHit` 的调用方都不接返回值）
            if (!ClipRect(PaddedHitRect(r, hitPad), clip, out hr)) return null;
            // ⚠️ 命中区那个**节点自己**摆在父原点（`localPosition = 0`）、quad 摆在矩形中心 ——
            //    照抄 `MainMenuSubmenuWindow.AddHit` 原来的写法**一字不改**
            //    （那边的自检有 1000+ 条断言，换个写法就是改行为）。
            //    部分越界时 quad 摆在**截过那块**的中心 ⇒ `PointerLayer` 用它的中心 + 宽高做命中，自动就跟着截了。
            var hit = new GameObject(name).transform;
            hit.SetParent(parent, false);
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
        /// （`Shell/PointerLayer.cs:492`，`GetComponentInChildren`）⇒ **裸节点进不了命中表**。
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
        //    🔴 **全工程 17 个「压暗层命中区」站点**（⚠️ 这个数**不是裸 grep 能直接数的**：
        //    `CloseHit` 在别的件上是**关窗钮** —— `DeckInfoPopup.cs:625` · `DeckSelectionPopup.cs:379` ·
        //    `ImportDeckPopup.cs:161` · `TrophyInfoPopup.cs:236`（**四个都带一张按钮脸**）——
        //    别把它们算进来；而 `BoosterPackOpenWindow` 那颗又**不叫这个名**）：
        //      · **2 处早就在公共件上**：`ImportDeckPopup.cs:103` · `TrophyInfoPopup.cs:202`；
        //      · **13 处归 A47 接线批的白名单**：`BattleLogPopup` · `BoosterInfoPopup` · `CardDetailPopup` ·
        //        `DeckSelectionPopup` · `DuelPopupWindow` · `LeaderboardWindow` · `MissionRerollPopup` ·
        //        `PlayerProfileWindow` · `BoosterPackOpenWindow` + R-F 审查补出的一族（全叫 `BackdropHit`）
        //        `PracticeModePopup` · `RankedEventWindow` · `SkirmishEventWindow` · `SearchingMatchPopup`。
        //        ⚠️ `BoosterPackOpenWindow.cs` 的 `95/310` 是**同一处**的常量行与建节点行 ⇒ **只算一处**；
        //        把它数成两处，总数就会变成 18（这一段的上一版就是这么错的）；
        //      · **2 处不在那一批的白名单里**：`ChatPanel` · `ProfileTab.cs:675`。
        //      ⇒ **2 + 13 + 2 = 17**。
        //    ✅ **当前状态（2026-10-05 逐条 grep 过）**：`grep -rn "MenuDraw\.ShadeHit("` 命中 **16 条**，
        //       上面那 16 处**逐条对得上**；**只剩 `Shell/ProfileTab.cs:675` 一处仍是旧写法**
        //       （`Hit(_nameWin, "DarkBgHit", …, L_NameBgHit, CancelNameWindow)`）——
        //       🔴 **它是本规矩的第一条【例外】，不是漏掉的欠账**：改名窗是**窗内浮层**，
        //       打开时下层页面内容仍然 active，所以命中档要**夹在下层内容与浮层内容之间**
        //       （`L_Bg=0` / `L_NameBgHit=8` / 浮层里的钮=9）—— 判据与出处 →
        //       `资料/待办判据_阶段二与联机.md` §（一）⑥ ③。⛔ **别去「收口」它**（收口 = 按 `qShade = 压暗档` 走，
        //       那会把改名的按钮点不动）。
        //
        // ⚠️ **落点为什么是 `MenuDraw` 而不是 `MenuWindowBase`**（与 A25⑥ 的措辞有一处出入，理由如下）：
        //    上面那 17 个站点**全都是 `GameWindow` 的子类（弹窗）**，而 `MenuWindowBase.cs` 里那个类
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
        /// <para>· <see cref="ShadeHitTierWarns"/>：**档不合法**的次数（= 上面那条告警响了几次）——
        /// 把 `qShade` 传成 `QContentHit − 1` 这种「看着像派生、其实同档/越档」的写法会被它抓住。
        /// ⚠️ 全工程不变量：**它必须恒为 0**。⚠️ **2026-10-05（A71④）就地订正**：这里原来写「全工程 **15 个
        /// 调用点**逐条核过」—— 那个数与本文件上面那段「压暗层命中区」的清单**又是两套**。
        /// 现在不在这里重复：**总数与清单只留上面那一处**（17 个站点 / 其中 16 个已走公共件）。</para></summary>
        public static int ShadeHitTierWarns;
        /// <summary>🔴 **2026-10-05（A71④）换判法**：原来这里是一张 `static HashSet&lt;Transform&gt;`，
        /// **只 `Add`、从不 `Clear`** ⇒ 窗口反复重建时 ① 无上限增长、② 长期持住**已销毁对象的托管壳**、
        /// ③ `instanceID` 复用时会**假阳性**（新节点被判成「ShadeHit 建的」）。
        /// 现在改成**挂在节点自己身上的一颗空标记**：节点跟着窗口一起销毁 ⇒ 上面三条一次都不成立
        /// （不再需要 `Clear`，也不再有一张全局表）。
        /// ⚠️ `ShadeHit` 的**既有行为一字未改**（那一句 `if (qShade >= qContentMin) …` 仍然那样）。</summary>
        sealed class ShadeHitMark : MonoBehaviour { }
        /// <summary>这个节点**是不是 `ShadeHit` 建的**（自检用 —— 见 `ShadeHit` 的注释）。
        /// 🔴 **签名与语义一字未改**（4 份自检宿主 `Editor/CollectionScene.cs:58` · `Editor/MainMenuScene.cs:58` ·
        /// `Editor/RewardsScene.cs:171` · `Editor/ShopScene.cs:612` 照样调）；
        /// 只有**判法**从「查一张全局表」换成「看节点自己身上有没有那颗标记」。
        /// ⚠️ 已销毁的节点 `node != null` 就是假（Unity 那一套）⇒ 直接返回 false，不会去 `GetComponent`。</summary>
        public static bool WasShadeHit(Transform node)
        {
            return node != null && node.GetComponent<ShadeHitMark>() != null;
        }

        public static Transform ShadeHit(Transform dark, PxRect r, int qShade, int qContentMin,
                                         System.Action onClick, string name = "CloseHit", PxRect? clip = null)
        {
            if (qShade >= qContentMin)
            {
                ShadeHitTierWarns++;
                Debug.LogWarning($"[MenuDraw] 压暗层命中区 `{name}` 的档 {qShade} **不低于**本窗内容命中区档 "
                                 + $"{qContentMin} —— 同档时 `ImageQuad` 的世界 z 恒 0，谁吃到命中退化成"
                                 + "「枚举顺序」，症状是**点不动的钮看着像正常工作**。"
                                 + "判据 → `资料/待办判据_阶段二与联机.md` §A25⑥ · `Shell/BoosterInfoPopup.cs` 的 `QShadeHit`。");
            }
            var hit = Hit(dark, name, r, qShade, onClick, null, null, null, null, clip);
            if (hit != null) hit.gameObject.AddComponent<ShadeHitMark>();   // 见 `WasShadeHit`
            return hit;
        }

        /// <summary>🆕 **自检模板**（A25⑥ ②）：**一扇窗一行**就能核那条不变量 ——
        /// 「压暗命中区档 = 该窗压暗层自己那一档，且**严格低于**本窗内容命中区档」。
        /// <para>用法（各 `Editor/*Scene.cs` 里，`darkHit` = 「点窗外关窗」那个节点，
        /// 例如 `ImportDeckPopup.ShadeHit` / `DeckSelectionPopup.ShadeHit`）：
        /// <c>string why; CheckTrue(MenuDraw.ShadeRuleOk(w.ShadeHit, w.QShade, w.QImpHit, out why),
        ///   "…（" + why + "）");</c> —— 期望值全是**该窗自己的原版档常量**，⛔ 别从被测实现里读。</para>
        /// <para>三样都查：① 节点在（不在 = 点窗外关不了窗）② 它下面真的挂着 `ImageQuad`（裸节点
        /// `PointerLayer` 拿不到 —— A26 那族的同一个坑）③ 档号 = 压暗档 且 压暗档 < 内容档。</para></summary>
        public static bool ShadeRuleOk(Transform darkHit, int qShade, int qContentMin, out string why)
        {
            why = "";
            if (darkHit == null) { why = "压暗层的命中区节点不在"; return false; }
            var q = darkHit.GetComponentInChildren<ImageQuad>();
            if (q == null) { why = "压暗命中区下没有 `ImageQuad`（`PointerLayer` 拿不到 ⇒ 点窗外关不了窗）"; return false; }
            if (q.RenderQueue != qShade)
            { why = $"档是 {q.RenderQueue}，不是压暗层那一档 {qShade}（别拿内容档派生 `±1`）"; return false; }
            if (!(qShade < qContentMin))
            { why = $"压暗档 {qShade} **不低于**内容命中区档 {qContentMin}（同档时谁吃到退化成枚举顺序）"; return false; }
            return true;
        }

        /// <summary>🆕 **2026-10-03（A17）**：`Hit` 的「一步到位」版本 —— **画底 + 建命中区**一次做完。
        /// 给「底就是一张图、没有别的装饰」的按钮用（大部分按钮都是这个形状）；
        /// 底上还要压图标/文字的那些仍走 `Rect` + `Hit` 两步。
        /// 返回**画底那个 `ImageQuad`**（调用方要压东西就用它）。
        /// ⚠️ `clip` 生效且整块在视口外时**底与命中区一起不建 ⇒ 返回 null**（调用方判空）。</summary>
        public static ImageQuad Button(Transform parent, string name, string art, PxRect r, int q,
                                       System.Action onClick, Color? tint = null,
                                       string hoverArt = null, string pressedArt = null,
                                       bool keepAspect = false, PxRect? clip = null,
                                       Vector2 clipSoftness = default(Vector2),
                                       Vector4 hitPad = default(Vector4))
        {
            var tex = CardArt.MenuUi(art);
            var qd = Rect(parent, tex, r, name, q, tint, keepAspect, clip, clipSoftness);
            Hit(parent, name + "Hit", r, q, onClick, qd, art, hoverArt, pressedArt, clip, hitPad);
            return qd;
        }

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

        /// <summary>画**一格卡组**（`r` = 已按缩放算好的显示矩形）。
        /// <paramref name="selected"/> = 画金框（原版 `Highlight Rounded Square`，色 (1,.773,0)）。
        /// <para>🆕 **2026-09-26：补上原版 `<Deck>` 下本来就有、我们此前漏画的两层** ——
        /// **难度角标**（`showDifficulty` 为真才画，对应原版 `DeckCollectionDisplay.displayDifficultyLabel`）
        /// 与**模式图标**（`gameMode` 给了才画；图取不到就整层不建 = 原版那句 `enabled = (icon != null)`）。
        /// 两个参数都给 `null` = 这一格没有这两个概念（收藏窗/我的卡组就是这样）——
        /// 原版对「我的卡组」页也是把难度角标的总开关关掉的。</para>
        /// 🔴 同时**修掉阵营图标的位置**（原来画在**右上**、原版在**左下**，见 `DcFacX` 的注释）。
        /// <para>🆕 **2026-10-03**：`clip` 现在**也管点击区** —— 整格在视口外 ⇒ 连 `Hit` 一起不建，
        /// 压在边上的 ⇒ 命中区截到视口内（判据同 `Hit`：原版 `RectMask2D` 的射线那一面）。</para></summary>
        public static Transform DeckCell(Transform parent, string name, PxRect r, CollectionData.DeckInfo info,
                                         bool selected, int q, int qText, int qOverlay, int qHit,
                                         System.Action onClick, PxRect? clip = null,
                                         int? gameMode = null, int? difficulty = null, bool showDifficulty = false,
                                         Vector4 hitPad = default(Vector4))
        {
            const float K = DeckCellK;
            var cell = Node(parent, name, r);

            Rect(cell, CardArt.MenuUi("40K_bt_deck"),
                 new PxRect(r.x1 + DcFrameX * K, r.y1 + DcFrameY * K,
                            r.x1 + (DcFrameX + DcFrameW) * K, r.y1 + (DcFrameY + DcFrameH) * K),
                 "Frame", q, null, false, clip);

            // ✅ **2026-09-24 起这里画的是「玩家选的卡背」**（`PlayerDeck.CardbackId`，
            //    在卡组编辑的 Cosmetics 页里右键选）；**没选过**的卡组退回该阵营的默认卡背 ——
            //    与原版一致（`CardDeck.GetDeckCardback()`：`cardbackId` 空 ⇒ `GetDefaultCardback(army)`）。
            //    原来那句「我们挑的 / 没有数据源」已随第 46 行做完而作废。
            var back = CardArt.DeckCardback(info.CardbackId, info.Faction);
            if (back != null)
                Rect(cell, back,
                     new PxRect(r.x1 + DcBackX * K, r.y1 + DcBackY * K,
                                r.x1 + (DcBackX + DcBackW) * K, r.y1 + (DcBackY + DcBackH) * K),
                     "CardBack", q, null, false, clip);

            // 卡名：⚠️ 文字没法像图那样截 uv ⇒ **按原版 `RectMask2D` 切成半个字**（`ClipText`）
            //（🆕 2026-10-04：此前只做「**整块**在视口外就不建」、压在视口边上的字照画出去 —— 那条缺口已补）
            var nameR = new PxRect(r.x1 + DcNameX * K, r.y1 + DcNameY * K,
                                   r.x1 + (DcNameX + DcNameW) * K, r.y1 + (DcNameY + DcNameH) * K);
            if (Visible(nameR, clip))
            {
                var nl = Text(cell, nameR, info.Name, Color.white, "Deck Name", DcNamePx * K, qText);
                if (clip.HasValue) ClipText(nl, clip, Vector2.zero);   // 「整块在框外」也由 `ClipText` 兜底（切到 0 宽）
            }

            if (!string.IsNullOrEmpty(info.Faction))
                // ⚠️ 走 `CardArt.MenuUi`（三级兜底 `ui_menu/ → ui_deck/ → ui/`）—— 与收藏窗那边原来那条路一致
                // 🔴 位置 = **左下**（作者系 `-10.5, 273.7`），**别再改回右上**（2026-09-26 更正，见 `DcFacX` 注释）
                Rect(cell, CardArt.MenuUi(DeckRuntime.FactionIcon(info.Faction)),
                     new PxRect(r.x1 + DcFacX * K, r.y1 + DcFacY * K,
                                r.x1 + (DcFacX + DcFacW) * K, r.y1 + (DcFacY + DcFacH) * K),
                     "Faction", q, null, true, clip);

            // 🆕 **模式图标**（作者系 `170.5, 273.68`，右下）—— 原版 `enabled = (icon != null)`：
            //    图取不到就**整层不建**（`Rect` 遇 null 直接 return null，天然满足）
            if (gameMode.HasValue)
                Rect(cell, CardArt.MenuUi(GameModeIconFile(gameMode.Value)),
                     new PxRect(r.x1 + DcModeX * K, r.y1 + DcModeY * K,
                                r.x1 + (DcModeX + DcModeW) * K, r.y1 + (DcModeY + DcModeH) * K),
                     "Game Mode Icon", q, null, true, clip);

            // 🆕 **难度角标**（作者系 `159.19, 15.74`，右上）—— 四档三张图（`0/5 一条杠 · 10 两条 · 15 三条`）。
            //    ⚠️ 原版节点名叫 `DificultyLevel`（**拼错了**，照抄别改，断言要按这个名字找）
            if (showDifficulty && difficulty.HasValue)
                Rect(cell, CardArt.MenuUi(DifficultyMarkFile(difficulty.Value)),
                     new PxRect(r.x1 + DcDiffX * K, r.y1 + DcDiffY * K,
                                r.x1 + (DcDiffX + DcDiffW) * K, r.y1 + (DcDiffY + DcDiffH) * K),
                     "DificultyLevel", qOverlay, null, true, clip);

            if (selected)
                Rect(cell, CardArt.MenuUi("Highlight_Rounded_Square"),
                     new PxRect(r.x1 + DcHiOff, r.y1 + DcHiOff, r.x1 + DcHiW * K + DcHiOff, r.y1 + DcHiH * K + DcHiOff),
                     "Highlight Rounded Square", qOverlay, new Color(1f, 0.773f, 0f, 1f));

            // 点击区：**视口外的不建、压在视口边上的截到视口内** —— 判据与 `Hit` 同一条
            //（原版 `RectMask2D` 的**射线那一面**：滚出视口的格子**点不到**，见 `ClipRect` 的注释）。
            // ✅ **2026-10-04（A26）修掉的那处真缺陷**：这一颗原来只建了个**裸节点**（`Node`、不带 quad），
            //    而 `PointerLayer.CollectHits` 取的是「按钮下第一个 `ImageQuad`」（`Shell/PointerLayer.cs:492`）
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
                // 🔴 先过 `RectMask2D.m_Padding`（`hitPad`，只改命中区）再裁 —— 见 `PaddedHitRect`。
                //    ⚠️ **2026-10-04 就地订正（F2）**：收藏窗 Deck 页那份原版 mask 实测是 **(0,0,0,0)**
                //    （`Collection Menu Variant/…/Select Deck Tab/Decks Tab/…/Deck Scroll View/Viewport`）；
                //    这里原来写的 `(0,9.69,0,9.69)` 是**别人家的值**（`…/Searching Oponent Popup/Window`，
                //    见 `PaddedHitRect` 上面那段逐处实读表）⇒ **这一处不该给 `hitPad`**
                //    （照旧写法接线会在本来不吃 padding 的窗上加 padding）。
                if (ClipRect(PaddedHitRect(r, hitPad), clip, out hr))
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
    ///
    /// ⚠️ **本组件的边界（如实写）**：只覆盖 **TMP 那条后端**（`Label` 正常走的那条）。
    ///    点阵兜底那条**没有事件可订** ⇒ 它仍然只能靠「建完别再改」（那一档只在 TMP 资源缺失时才出现）。
    /// ⚠️ 批处理里没有帧循环 ⇒ **没有重排事件**：自检别拿它自证（要验就在 `Play` 里点一次，或直调 `MenuDraw.ClipTextNow`）。</summary>
    public class ClippedTextGuard : MonoBehaviour
    {
        Label _lb;
        TMPro.TextMeshPro _tmp;
        PxRect _clip;
        Vector2 _soft;
        bool _on;

        /// <summary>记下「裁成什么样」，等下一次重排照着重裁。重复 `Arm` 只更新参数、**不重复订阅**。</summary>
        public void Arm(Label lb, PxRect clip, Vector2 softPx)
        {
            _lb = lb; _clip = clip; _soft = softPx;
            _tmp = lb != null ? lb.GetComponentInChildren<TMPro.TextMeshPro>() : null;
            if (_on) return;
            TMPro.TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
            _on = true;
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
            if (MenuDraw.ClipTextNow(_lb, _clip, _soft)) MenuDraw.TextClipReapplied++;
        }
    }
}
