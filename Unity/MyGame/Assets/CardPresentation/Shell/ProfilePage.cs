// ProfilePage.cs — 档案窗每页的公共底座（原版 `WindowTabBase<PlayerProfileMenu>`）。
// 🔴 **2026-10-13 从 `PlayerProfileWindow.cs` 拆出**（A1506：Unity 要求 MonoBehaviour 的**类名与所在文件名相同**——
//    它是六个档案页 + `StrangerProfilePage` 的基类，`AddComponent<派生>()` 时 Unity 要把整条链都解析成脚本资产）。
//    出处与纪律见 `PlayerProfileWindow.cs` 的文件头（本块**逐字搬**，未改一字）。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    // ============================================================ 六个页的公共底座
    //
    // ⚠️ 名字**照原版类名**：`Profile Tab`→`ProfileTab` · `Avatar Tab`→`AvatarTab` · `Title Tab`→`TitleTab` ·
    //    `Battle Log Tab`→`BattleLogTab` · `Trophies Tab`→**`AchievementsMenu`**（原版就叫这个：
    //    键名叫 Trophies、文案是 Achievements）· `Ranking Tab`→`RankedTab`。字段名也照原版
    //    （出处：各页自己的 MonoBehaviour JSON，2026-09-27 用 `menu_dump.py` 读的 `字段: …` 那一列）。

    /// <summary>档案窗一页的公共底座。原版是 `WindowTabBase&lt;PlayerProfileMenu&gt;`；
    /// 我们沿用 `WindowTabBase`（它的 `Type` 是 `WindowTabType` ⇒ 六个档案页的值在**30–35** 段）。</summary>
    public abstract class ProfilePage : WindowTabBase
    {
        /// <summary>宿主窗（`BuildPages` 建完就赋）。</summary>
        public PlayerProfileWindow Win;

        /// <summary>本页的根矩形（原版**逐页不同**：`Avatar`/`Title` 两侧各溢 16.33）。</summary>
        public abstract PxRect PageRect { get; }

        protected Transform Root { get { return transform; } }

        /// <summary>本页的队列起档（六页各占一段 10 个号，互不重叠）。</summary>
        protected int Q { get { return PlayerProfileWindow.QPageBase + PageIndex * 10; } }

        /// <summary>本页在 `Tabs` 表里的下标（0–5）。
        /// 🆕 2026-10-03：改成 **`virtual`**（原来 `abstract`）—— 六个具体页仍各自覆写成常量，
        /// 而 `StrangerProfilePage` **一个类要顶六页** ⇒ 靠建窗那一处统一赋的 `PageIdx`。</summary>
        protected virtual int PageIndex { get { return PageIdx; } }
        /// <summary>建窗那一处赋的页下标（`StrangerProfilePage` 用它算队列档与类型）。</summary>
        public int PageIdx;

        // 🔴 **2026-10-13（A435 阶段 2 · 丙）：`Clip` / `ClipSoftness` 两个字段【已删】。**
        // 它们原来是「滚动区在画内容**之前**设一次、画完清掉」的临时状态（六个页共用），
        // 真值出处（原文保留，值仍有效 ⇒ **节点就是照这些值建的**）：
        // · 本窗 `Avatar Tab` / `Title Tab` 的 `Item Display Panel/Scroll Rect` = **`m_Softness (0,50)`**；
        // · `Trophies Tab/Scroll/Viewport` 与 `Ranking Tab/AllFactions/scroll rect/viewport` = **`(0,0)`**（硬边）。
        // 现在这份状态长在**视口节点**上（各页 `ViewportClip.Hang` / `AddComponent` 处 —— 见
        // `AchievementsMenu` / `AvatarTab` / `TitleTab` / `RankedTab` 四个页文件），
        // 本类下面五个转发口**一律不传 `clip`**（`null` ⇒ `MenuDraw.*` 沿父链解析到那颗节点）。
        // ⚠️ 判据 = 原版 `RectMask2D` 逐视口一份（`Shell/ViewportClip.cs` 文件头）。

        /// <summary>取图（走宿主窗那一个入口，取不到会记进 `MissingArt`）。</summary>
        protected Texture2D Art(string name) { return Win != null ? Win.ArtInternal(name) : null; }

        // ---- 绘图转发（都用本页的队列档；`MenuDraw` 是唯一的画图层，别在这再写一份）----
        protected static Transform Node(Transform parent, string name, PxRect r) { return MenuDraw.Node(parent, name, r); }
        protected Transform Node(string name, PxRect r) { return MenuDraw.Node(Root, name, r); }

        /// <summary>按矩形摆一张图。`art == null` ⇒ 纯色块（原版那种「没 sprite、只有 m_Color」的件）。
        /// 🔴 **2026-10-13（A435 阶段 2 · 丙）**：末两个实参（`clip` / `clipSoftness`）**不传了** ——
        /// 裁切状态在视口节点上，`MenuDraw.Rect` 会沿父链解析（`Resolve` 第 2 支）。</summary>
        protected ImageQuad Rect(Transform parent, string art, PxRect r, string name, int qOff,
                                 Color? tint = null, bool keepAspect = false)
        {
            var tex = art == null ? CardArt.Solid() : Art(art);
            return MenuDraw.Rect(parent, tex, r, name, Q + qOff, tint, keepAspect);
        }
        protected ImageQuad Rect(string art, PxRect r, string name, int qOff, Color? tint = null, bool keepAspect = false)
        { return Rect(Root, art, r, name, qOff, tint, keepAspect); }

        /// <summary>纯色块。</summary>
        protected ImageQuad Solid(Transform parent, PxRect r, string name, int qOff, Color tint)
        { return MenuDraw.Rect(parent, CardArt.Solid(), r, name, Q + qOff, tint); }

        /// <summary>🆕 **装饰品头像**那一批的图（`Resources/Art/avatars/`）。
        /// 🔴 走 `CardArt.Cosmetics` 而**不是** `MenuUi` —— 那一批在**独立目录**，而且
        /// **名字含空格、原样传**（`Avatar_UM_Attack Bike`，别换下划线）。
        /// 取不到时记进 `MissingArt`（**出声**，不静默画个白块）。</summary>
        protected ImageQuad CosmeticRect(Transform parent, string art, PxRect r, string name, int qOff,
                                         bool keepAspect = true)
        {
            Texture2D tex = null;
            if (!string.IsNullOrEmpty(art))
            {
                tex = CardArt.Cosmetics(art);
                if (tex == null && Win != null && !Win.MissingArt.Contains(art)) Win.MissingArt.Add(art);
            }
            return MenuDraw.Rect(parent, tex, r, name, Q + qOff, null, keepAspect);
        }

        /// <summary>九宫格（原版 `Image.Type = Sliced`）。`border` 按**贴图原始像素**给。
        /// 🆕 **2026-10-04：这条路原来【不吃 `Clip`】**（`MenuDraw.Nine` 的 `clip` 参数是显式传的，
        /// 这里漏了那个实参）—— `MenuWindowBase.Nine` / `SocialPage.Nine` 早就传了，**只有这里没传**。
        /// 🔴 **活例（滚动时就有画面后果）**：`AchievementsMenu.RebuildCells` 是
        /// `Clip = _scroll.Viewport; BuildCells(); Clip = null;` —— 每格那个 `ArtPanel` 九宫格底
        /// **是在 `Clip` 生效时建的**（本页视口 `VpT 216.14 → ScB 891.69`、内容高 32 + 18×160 = 2912
        /// ⇒ **必定要滚**）⇒ 补之前，**滚动时压在视口上下边的那几格，九宫格底画到视口外**。
        /// 🔴 **2026-10-13（A435 阶段 2 · 丙）就地订正（铁律 5）**：上面那段「`Clip = …` 区间」的
        /// 说法**已作废**（`Clip` 字段已删、那一对设/清也删了）—— 现在这条路的裁切来自**视口节点**
        /// （`MenuDraw.Nine` 内部 `Resolve`），所以**每次建都吃**，不再依赖「建在 `Clip` 区间里」。
        /// 行为上等价（节点框 = 原来的 `Clip` 值 ⇒ 上面那个活例的后果照旧被修掉）。
        /// ⚠️ 判据 = 原版 `RectMask2D` 对所有子件一视同仁。</summary>
        protected GameObject Nine(Transform parent, string art, PxRect r, Vector4 border, string name, int qOff,
                                  Color? tint = null, bool fillCenter = true)
        {
            var tex = Art(art);
            if (tex == null) return null;
            return MenuDraw.Nine(parent, tex, r, border, tex.width, tex.height, Q + qOff, tint, fillCenter, name);
        }

        /// <summary>TMP 规矩：`m_TextWrappingMode = 1`（限宽换行）/ `0`（不折行）。
        /// 🔴 **别用 `SetFontSize(px/108)`** —— 那会大 2.7 倍；走 `SetGlyphHeight`（`MenuDraw.Text` 已经做了）。
        ///
        /// <para>🔴 **2026-10-08（A212 · 收口）：`autoFit` 不再隐含 `wrap`。**
        /// 判据 = `Label.SetAutoFitBox` 内部第一句就是 `SetWrapWidth(worldW)`，而那个**无条件**把
        /// `m_TextWrappingMode` 设成 `Normal(=1)`（`Core/TmpFont.cs` 的 `SetWrapWidth`）⇒ 「要自适应、但原版**不折行**」的件
        /// 会被**悄悄打开折行**（= A62 那一族的静默偏离）。
        /// 这个口是**档案窗六个页（`ProfileTab`/`AvatarTab`/`RankedTab`/`TitleTab`/`AchievementsMenu`/`BattleLogTab`）
        /// 的公共入口**（子表 A 那 52 处都过它）⇒ 在这里把 `wrap` 变成**唯一那一个开关**：
        /// `<c>autoFit</c>` 跑完若 `wrap` 为假，就把模式**显式设回 `0`**（`SetWrapping(false)`）。
        /// · `wrap: true`（= 原版 `折行=1`）的调用点**行为逐字不变**（它本来就走 `SetWrapWidth`）；
        /// · `wrap` 缺省（= 原版 `折行=0`）的调用点**不再靠副作用**；</para>
        ///
        /// <para>⚠️ **本口只表达 0 与 1 两档**：原版第三档 `3`（`PreserveWhitespaceNoWrap`）走
        /// <see cref="Label.SetWrappingMode"/>，调用点在 `Text(...)` **之后**自己设（先例 = `ProfileTab` 改名窗
        /// 那个 `Text` 节点，`wrap` 不传 ⇒ 本口先设 0、随后它自己设成 3，**终态仍是 3**）。
        /// ⛔ 别用 `wrap: true` 顶替 `3`（= 把 `3` 静默降级）。</para>
        ///
        /// <para>🔴 **2026-10-11（A317 · 已落地）：`autoFit` / `autoMinPx` / `wrap` 的缺省值【删掉、形参必填】**
        /// （`wrap` 是这一条的靶子；另两个是 C# `CS1737`「必填须排在可选之前」的连带 —— 好在那两个
        /// **原本 52 处调用点就全都显式传**，本批只是把它们变必填，**没有一处新读数**）。
        /// 判据 = `资料/普查产出_1010/调度台_口径裁定_1011.md` §A258（「去掉缺省值、形参必填」，
        /// ⛔ **不是**「统一成某一边的缺省值」—— `V4a` 亲跑 `menu_dump` 量过：原版**本来就是混的**，
        /// 缺省值**不是原版概念**）。
        /// 🔴 **`alignLeft` 也【删掉缺省、形参必填】—— 2026-10-12（A323）落地**。
        /// A317 那一批**没能**变必填，唯一的阻塞点就是白名单外那 4 处
        /// （`Shell/AchievementsMenu.cs` 3 处 + `Shell/BattleLogTab.cs` 1 处）；
        /// **本批那两个文件进了白名单** ⇒ 各补一句 `alignLeft: false`。⚠️ **零行为变化**：
        /// 缺省本来就是 `false`，且本批已亲跑复核那 3 处原版 `对齐=` 全是 **`Center`**
        /// （`Tab Toggle Title` = `Center/Middle` · 格内 `counter` = `Center/Middle` ·
        /// `Counter/EverguildTextMeshPro` = **`Center/Capline`**）；
        /// 第 4 处（`BattleLogTab` 那颗**自建钮**）**原版判据本来就空** ⇒ 沿用旧缺省 = 保持现状，不假装复刻。
        /// ⇒ 本口现在 **4 个 bool/float 实参一个不缺**。
        /// 判据 = `python 工具/menu_dump.py bundle_menus_assets_all "Player Profile Window" --depth 25 --md`
        /// （标题格走 `"Title Drawer Horizontal Variant" --depth 8 --md`）—— 本批新补的
        /// **`alignLeft` 25 处 + `wrap` 23 处** 全部取自那两条 dump 的 `对齐=` / `折行=` 两列。
        /// ⚠️ **本批零行为变化**：补出来的值与原来缺省**恰好一致**（缺省 `alignLeft:false` ⇔ 原版 `Center`
        /// 的那 25 处、缺省 `wrap:false` ⇔ 原版 `折行=0` 的那 23 处 —— 逐处核对过，没有一处反向）。
        /// ⚠️ 原版 `对齐=Right` 的件**这个口表达不了**：由调用点在 `Text(...)` 之后自己
        /// `MenuDraw.AlignRight`（先例 = `Shell/ProfileTab.cs` 里 `playerIdText` 那句 `MenuDraw.AlignRight` 的 `playerIdText`，原版 `Right/Middle`）
        /// ⇒ 那种地方 `alignLeft` 传 `false`（别推左边缘），**别传 `true`**。</para></summary>
        protected Label Text(Transform parent, string text, PxRect r, Color color, string name, float fontPx,
                             int qOff, bool autoFit, float autoMinPx, bool wrap, bool alignLeft,
                             float autoMaxPx = 0f, float basePx = 0f,
                             TmpFont.Face face = TmpFont.Face.Cjk)
        {
            // 裁切：① 整块在视口外 ⇒ 不建（收口到 `MenuDraw.Visible` —— 🔴 **2026-10-10 订正（A184）**：
            //   它**现在是全壳唯一一份求交**，不再是「A25④ 那四处内联的唯一实现」）；
            //      ② 🆕 2026-10-04：**压在视口边缘的字切掉**（`MenuDraw.ClipText`）。原来只做 ①。
            //    🔴 **2026-10-13（A435 阶段 2 · 丙）**：两处都改走**节点态**那一版 ——
            //   ① `Visible`（纯矩形函数，手上没有 `Transform`）⇒ **`VisibleAbove(parent, …)`**；
            //   ② `ClipText` 收的必须是**调用方原样那一份**（这里 = `null`，本类已无 `Clip` 字段）——
            //      它内部自己解析，且**每次重排都重新解析**（节点挪了/后挂都跟得上，A484）。
            //   ⛔ 别忘了①：只有②的话「整块在视口外」的件仍会**建出来**（静默，且白建一棵子树）。
            if (!MenuDraw.VisibleAbove(parent, r, null)) return null;
            var lb = MenuDraw.Text(parent, r, text, color, name, fontPx, Q + qOff, face: face);
            if (lb != null)
            {
                if (wrap) lb.SetWrapWidth(LayoutSpace.Px(r.W));
                if (autoFit && fontPx > autoMinPx)
                {
                    // 🔴 **2026-10-12（A333 + A336③）**：上限取**原版 `m_fontSizeMax`**（`autoMaxPx <= 0`
                    //    才退回 `fontPx` = 旧行为 —— 旧写法把 `fontPx` 当上限，**短文案永远画小一档**）；
                    //    `basePx` = 原版 `m_fontSizeBase`（`<= 0` = 旧行为「base = 调用方那一档」）。
                    //    ⚠️ **本口的每个调用点（`ProfileTab`/`AvatarTab`/`RankedTab`/`TitleTab` 四页 ≈50 处）
                    //    的原版逐站值这一轮【没读】** —— 它们**不在本件白名单**（简报黑名单「其余一切」）
                    //    ⇒ 本件只开好形参、**一个站点都没填**，缺的读数见 `资料/普查产出_1012/F1_字号线.md`。
                    lb.SetAutoFitBox(LayoutSpace.Px(r.W), LayoutSpace.Px(r.H), autoMinPx,
                                     autoMaxPx > 0f ? autoMaxPx : fontPx, basePx);
                    // 🔴 **2026-10-08（A212 收口）**：`SetAutoFitBox` 内部无条件把模式开成 `Normal`
                    //    ⇒ 「原版不折行」的那些件必须在这一句之后**显式设回 0**（见方法头那一节）。
                    //    ⚠️ 顺序固定 `SetAutoFitBox → SetWrapping → AlignLeft`：`SetWrapping` 会重排并
                    //       **挪 TMP 子节点**（`ForceRelayout`），对齐必须落在它**之后**（下面的 `alignLeft` 已经满足）。
                    if (!wrap) lb.SetWrapping(false);
                }
                if (alignLeft) MenuDraw.AlignLeft(lb, r);
                // 🔴 **2026-10-13（A435 阶段 2 · 丙）**：原来带 `if (Clip.HasValue)` 守卫 —— 本类已无
                // `Clip` 字段 ⇒ 那个守卫恒假 ⇒ **整页文字一块都不裁**（静默）。改成无条件调：
                // `MenuDraw.ClipText` 内部自己解析，没有节点/没有显式框时**早退返回 false**。
                MenuDraw.ClipText(lb, null, Vector2.zero);            // ⚠️ 必须在定完字号之后
            }
            return lb;
        }

        /// <summary>一个**透明点击区** + `WindowButton`（`PointerLayer` 扫的就是它 —— 照原版，
        /// 这一层就是按钮自己的 `RectTransform`）。
        /// 🆕 **2026-10-03**：把本页的 `Clip` 传下去 —— 视口外的命中区**不建**、压在视口边上的**截到视口内**
        /// （判据 = 原版 `RectMask2D` 的射线那一面，见 `MenuDraw.Hit` / `ClipRect`）。
        /// 本类的 `Rect` 早就传了 `Clip`，**只有这里漏了**；本页确实会真给 `Clip` 赋值
        ///   （⚠️ **2026-10-13**：原写「本页的 `Rect`（:487）」—— 本类搬出 `PlayerProfileWindow.cs` 之后
        ///    「本文件/本页 `:N`」这类行号**必然失效** ⇒ 按口径只写**成员名**，不留行号。）
        /// （`AvatarTab` 那种纵向滚动网格：`AvatarTab.cs:147` / `:239`）。
        /// ⚠️ 现存的症状**不是**「滚出视口的格子还能点」—— 那些格子本来就因 `MenuScroll.Intersects`
        /// （`AvatarTab.cs:205`）没建；这里补的是**压在视口边上那一格**：它的命中区原来按整格算、
        /// **伸出视口外**，点在视口外那条带子上照样会命中（可能还压着别的件）。
        /// 这一处走的就是它（`AvatarTab.cs:224` 那颗格子的命中区）。</summary>
        protected Transform Hit(Transform parent, string name, PxRect r, int qOff, System.Action onClick,
                                ImageQuad target = null, string art = null,
                                string hoverArt = null, string pressedArt = null, string clickCue = null)
        { return MenuDraw.Hit(parent, name, r, Q + qOff, onClick, target, art, hoverArt, pressedArt,
                              clickCue: clickCue); }

        /// <summary>本页的滚动区。**全壳只有 `MenuScroll` 这一份滚动实现**（别在这再写一套偏移+夹取）。
        /// `vertical = true` 用 `TopAligned`（原版 `m_Vertical 1` 那种）。
        /// <para>`notchPx` = 原版那一页 `ScrollRect` 的 `m_ScrollSensitivity`（每格像素；判据 → `D_N29`）。
        /// 每一页**都必须显式传** —— 缺省 `DefaultNotchPx`（48）只是「没现读」的兜底，⛔ 不是原版参数。</para></summary>
        protected MenuScroll NewScroll(PxRect viewport, float contentW, float contentH, bool vertical,
                                       float notchPx = MenuScroll.DefaultNotchPx)
        {
            var s = vertical ? MenuScroll.TopAligned(viewport, contentH, notchPx)
                             : MenuScroll.LeftAligned(viewport, contentW, notchPx);
            s.Owner = Root.gameObject;     // 切走的页签里那些滚动区靠它被跳过
            PointerLayer.RegisterScroll(s);
            return s;
        }

        /// <summary>本页内容。**只在第一次开窗时调一次**（换页只切 `activeSelf`，不重建）。</summary>
        public sealed override void Setup()
        {
            // 🔴 **先把自己名下那些滚动区撤掉**（2026-09-27 补）：下面会「把 Root 的子节点全删了重建」，
            //   而重建出来的是**新 `MenuScroll` 对象** —— 旧的那些 `Owner` 是这个 **Root**（重建时它不会死）
            //   ⇒ 光靠「宿主销毁」判不出它们已经没用 ⇒ 登记表**每开一次窗涨一批**、而且**旧条目还能被滚轮命中**
            //   （`OnChanged` 指向已经销毁的节点）。判据 → `项目任务.md` §〇 A ②。
            PointerLayer.UnregisterOwnedBy(Root.gameObject);
            for (int i = Root.childCount - 1; i >= 0; i--) DestroyNow(Root.GetChild(i).gameObject);
            Build();
            if (Win != null && Win.MissingArt.Count > 0)
                Debug.LogWarning("[Profile] `" + Tag + "` 页有 " + Win.MissingArt.Count
                                 + " 张图取不到（**这些件没画**）：" + string.Join("、", Win.MissingArt.ToArray())
                                 + " —— 导入器：`工具/import_original_art.py` 的 `MENU_IMAGES`");
        }

        /// <summary>本页的代号（日志用；照原版类名）。</summary>
        public string Tag { get { return GetType().Name; } }

        protected abstract void Build();

        /// <summary>立刻销毁（批处理下没有帧循环 ⇒ `Object.Destroy` 不生效）。
        /// 🔴 `protected`：各页的**滚动重建**（`MenuScroll.OnChanged`）也要用它 —— 别各页再抄一份。</summary>
        protected static void DestroyNow(GameObject go)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) { Object.DestroyImmediate(go); return; }
#endif
            Object.Destroy(go);
        }
    }
}
