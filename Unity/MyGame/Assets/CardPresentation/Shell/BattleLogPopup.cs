// BattleLogPopup.cs — 多人界面那一批 第 4 件之四：**对局历史（独立弹窗）**
//   （原版节点名 `Battle Log Popup`，类名 `BattleLogPopup`；与档案窗那一页是**两扇不同的窗**）
//
// ============================ 出处（唯一正本） ============================
// `资料/普查产出_0927/对局历史_行模板与弹窗.md` —— §B 是它的**层 × 参数**表（4 个 RT + 两层子件）·
// §B·补列 ①②③④ 是工具印不出的那些值（窗口字段 / 压暗层 / 关闭钮两层 / `ScrollRect` 的真值）·
// §E 是入口追查 · §F 是查不到的。
//
// ---- 🔴 五条判据（读原始 JSON 定的）----
// ① **窗口属性**（MB `-845058224705724360`）：`type = 1`(**Popup**) · **`windowsPlacement = 15`(Popup)** ·
//    `closeOnESC = 1` · `updateNavPanel = 0` · `extraScaleSmallScreen = 1.0` ·
//    `holder = 4975276729081765944`（= 表里那个 `Content`）· `closeButton = 8586477743801671736`。
// ② 🔴 **行就是这个 `logPrefab`**：与 `BattleLogTab` 是**同一个 prefab**（两处 MB 的 `logPrefab` 都指
//    `m_PathID 8607776031950241599`）⇒ 行几何走 `Shell/MatchLogRow.cs`，**别另写一份**。
// ③ **`Close Button` 自己的底图 `m_Enabled = 0`**（`UI_Button_Round_background` 237×237）⇒ **原版不画它**；
//    看得见的圆是子节点 `Background`（`40k_general_bt_yellow`）+ `Icon`（`40k_general_bt_yellow_close`），
//    而且**略偏左上**（左缝 14.49 / 右缝 16.41）—— 原版就这么摆，**别「居中」掉**。
// ④ 🔴 **`Matches` 的 `ScrollRect` 与 Tab 那份【不同】**：这里 `m_MovementType = 2 (Clamped)` ·
//    `m_ScrollSensitivity = 1.0`；Tab 那份是 `1 (Elastic)` · `50`。**别套 Tab 的滚动手感**。
// ⑤ **`Menu Dark Background` 上没有接好的关闭**：`BackgroundCloseButton` 的 `window` / `onClick`
//    序列化值**都是空的**（运行期才接）；我们**照「点窗外关」做**（同一扇族里别的窗都是这个语义，
//    而且这是唯一能关的路径之一 —— 否则只剩 ESC）。
//
// 🆕 **2026-10-03（A25②）：这一格补上了滚动区** —— 原来 `Matches` 里**连 `MenuScroll` 都没有**
//    （`grep MenuScroll Shell/BattleLogPopup.cs` 零命中）⇒ 内容高过视口时，**第 5 行起永远看不到也点不到**
//    （那些行早就被 `MenuDraw.ClipRect(rr, ViewportR)` 整行跳过、连节点都不建）。
//    判据就是上面 ④ 那一条（`ScrollRect` `h=0 v=1` · **mode=2 Clamped** · 灵敏度 1.0）+ 普查 §B 表里
//    `Content` 的 `ContentSizeFitter m_VerticalFit=1` ⇒ **可滚范围 = 内容高 − 视口高**（同 `BattleLogTab`
//    那一页，两处的行高/行距/内容高算式逐值相同）。⚠️ 全壳一个滚轮手感（`MenuScroll.NotchK`），
//    灵敏度 1.0 那一档**不逐处复刻** —— 已知自选，记在普查 §D6。

// ---- 🔴 入口：**原版的打开点查不到** ----
// 普查 §E 追过：它只出现在 `WindowsManager.temporaryWindowDictionary` 那张**预载表**里
// （键是 Addressables 容器 GUID `c4402264327016e479ace86fff266d7d` → GO `7408828764512624696`），
// 打开方式是 `WindowsManager.OpenWindow<BattleLogPopup>(…)` —— **而那个泛型调用的产物缺失**，
// 全 91 包按字节搜 GUID/pid、反编译 74 处 `OpenWindow(` 逐个看过，都没有具体调用点
// （`CemeteryManager.ShowCemeteryLogBtn` 名字像，读过全文**不是**）。
// ⇒ **这一扇窗建出来了，但界面里【没有入口】**（我们**不编**一个入口出来）。自检直接 `Create()` 验它。
//    要接的话接在哪 —— **等用户拍板**（记在 `项目任务.md` §三 第 18 条）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `BattleLogPopup`。</summary>
    public class BattleLogPopup : GameWindow
    {
        // 队列档：**弹窗 > 页 > 窗**（比社交页 3200+、聊天窗 3300+、挑战弹窗 3400+ 都高一段）
        public const int QBase = 3450;
        // ⚠️ `public`（2026-10-04 A47 接线批）：自检宿主要拿这两个档核「压暗命中区档 = 压暗层那一档
        //    且严格 < 本窗内容命中区最低档」这条不变量（`MenuDraw.ShadeRuleOk`）。
        public const int QPanel = QBase, QBg = QBase + 1, QContent = QBase + 2, QRow = QBase + 4, QHit = QBase + 8;

        public static BattleLogPopup LastOpened { get; private set; }

        // ---- 真值（§B 那张表，绝对画布像素）----
        static readonly PxRect DarkBgR = new PxRect(-1327.30f, -746.18f, 3247.30f, 1826.18f);
        static readonly Color DarkBgTint = new Color(0f, 0f, 0f, 0.773f);
        static readonly PxRect ContentR = new PxRect(135f, 55f, 1785f, 1055f);
        static readonly Vector4 ContentBorder = new Vector4(42f, 363f, 655f, 81f);
        static readonly PxRect CloseR = new PxRect(1685f, 25f, 1815f, 155f);
        static readonly PxRect CloseCircleR = new PxRect(1699.49f, 39.06f, 1798.59f, 138.74f);
        static readonly PxRect MatchesR = new PxRect(235f, 130f, 1710f, 980f);
        static readonly PxRect ViewportR = new PxRect(235f, 130f, 1710f, 963f);

        public readonly System.Collections.Generic.List<string> MissingArt =
            new System.Collections.Generic.List<string>();

        public int BuiltRows { get; private set; }
        public float ContentBottom { get; private set; }

        Transform _content;
        readonly RowCtx _rowCtx = new RowCtx();

        /// <summary>🆕 **2026-10-03（A25②）**：`Matches` 那一格的**纵向滚动区**（全壳唯一一份滚动实现
        /// = `MenuScroll`）。原版 = `ScrollRect h=0 v=1` · **`m_MovementType=2`(Clamped)** · `m_Inertia=1`
        /// · `m_Elasticity=0.1` · `m_DecelerationRate=0.135`（普查 §B 表 / §D6）—— `MenuScroll` 的默认
        /// 就是 Clamped + 有惯性，逐值对得上。</summary>
        MenuScroll _scroll;

        /// <summary>自检用：批处理里没有滚轮事件 ⇒ 直调 `MenuScroll.Wheel/SetOffset`（**和真滚同一条路**）。</summary>
        public MenuScroll RowsScroll { get { return _scroll; } }

        Texture2D Art(string n)
        {
            if (string.IsNullOrEmpty(n)) return null;
            var t = CardArt.MenuUi(n);
            if (t == null && !MissingArt.Contains(n)) MissingArt.Add(n);
            return t;
        }
        static Transform Node(Transform p, string n, PxRect r) { return MenuDraw.Node(p, n, r); }
        ImageQuad Rect(Transform p, string art, PxRect r, string n, int q, Color? tint = null, bool keepAspect = false)
        { return MenuDraw.Rect(p, art == null ? CardArt.Solid() : Art(art), r, n, q, tint, keepAspect); }
        GameObject Nine(Transform p, string art, PxRect r, Vector4 b, string n, int q)
        { var t = Art(art); return t == null ? null : MenuDraw.Nine(p, t, r, b, t.width, t.height, q, null, true, n); }

        // ---------------------------------------------------------- 建

        public static BattleLogPopup Create(WindowsManager mgr)
        {
            var go = new GameObject("Battle Log Popup");
            var win = go.AddComponent<BattleLogPopup>();
            win.type = WindowType.Popup;                       // 实证 type = 1
            win.placement = WindowsPlacement.Popup;            // 实证 windowsPlacement = 15
            win.closeOnEsc = true;                             // 实证 closeOnESC = 1
            win.extraScaleSmallScreen = 1f;                    // 实证 1.0
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            return win;
        }

        public override void Open()
        {
            LastOpened = this;
            Build();
        }

        public void Build()
        {
            MenuDraw.ClearChildren(transform);
            MissingArt.Clear();

            var dark = Node(transform, "Menu Dark Background", DarkBgR);
            Rect(dark, null, DarkBgR, "Image", QPanel, DarkBgTint);
            // 🔴 **2026-10-04（A47 接线批）：压暗层的命中区改走公共件 `MenuDraw.ShadeHit`。**
            //   档 = **压暗层自己那一档**（`QPanel` = `QBase` = 3450），且严格低于本窗内容命中区档
            //   （`QHit` = 3458）。原编码本来就是 `QPanel` **合规矩**，这一批只是收口到一份实现
            //   （规矩与出处 → `MenuDraw.ShadeHit` 的注释 · `资料/待办判据_阶段二与联机.md` §A25⑥）。
            MenuDraw.ShadeHit(dark, DarkBgR, QPanel, QHit, () => Close(), "CloseHit");

            var content = Node(transform, "Content", ContentR);
            Nine(content, "UI_Deck_Information_Back", ContentR, ContentBorder, "Background", QBg);

            // `Close Button`：**它自己的底图 `m_Enabled=0` ⇒ 不画**（判据 ③）；画的是两个子节点
            var close = Node(content, "Close Button", CloseR);
            var closeQ = Rect(close, "40k_general_bt_yellow", CloseCircleR, "Background", QBg, null, true);
            Rect(close, "40k_general_bt_yellow_close", CloseCircleR, "Icon", QContent, null, true);
            // 🆕 A17：原版 `Battle Log Popup>Content>Close Button` 是 SpriteSwap（普查 §块 5 第 20 行）
            MenuDraw.Hit(close, "Hit", CloseR, QHit, () => Close(), closeQ, "40k_general_bt_yellow");

            // `Matches`（`ScrollRect` **Clamped** · 灵敏度 1.0 —— 判据 ④）
            // ⚠️ 它自己的底是 UGUI 内置 `Background`、`m_Color=(1,1,1,0)` ⇒ **看不见 ⇒ 不画**
            var matches = Node(content, "Matches", MatchesR);
            var vp = Node(matches, "Viewport", ViewportR);      // 原版是 `UIMask` + `showGraphic=0` ⇒ 只建节点
            // 🆕 2026-10-03（A25②）：**照原版把滚动区补上**（此前这一格一处滚动都没有 —— 见文件头那条）。
            //   ⚠️ `Viewport` 在**原版里就是遮罩节点**（`Mask.m_ShowMaskGraphic=0`）⇒ 我们拿它的矩形当
            //     `MenuScroll.Viewport`（= `RectMask2D` 的等效物），**没有另挑一个矩形**。
            PointerLayer.UnregisterOwnedBy(gameObject);   // 重建 ⇒ 旧的那一份是死条目（同 `PlayerProfileWindow.Setup`）
            _scroll = MenuScroll.TopAligned(ViewportR, 0f);   // 内容高在 `BuildRows` 里按条数写（原版 `ContentSizeFitter`）
            _scroll.Owner = gameObject;                       // 关着的宿主 ⇒ 指针层跳过（页签/窗口切走那套）
            _scroll.OnChanged = BuildRows;   // 🔴 滚轮只改 `Offset`、**画是调用方的事**：不接 = 滚了什么都不动
            // 🔴 **2026-10-04（A35⑦）：走 `SocialPage.RegisterScroll` 那一份，别直调 `PointerLayer.RegisterScroll`**
            //   —— 直调那份在 `PointerLayer.Instance == null` 时是 **`return`（静默空转）**（`PointerLayer.cs:183-187`），
            //   登记表都没建 ⇒ 滚轮永远落不到这一格上，而**画面看着完全正常**（正是本批要治的那类缺陷）；
            //   `SocialPage.RegisterScroll`（`SocialWindow.cs:319-329`）先判这一条、**会 `Debug.LogWarning` 出声**。
            //   ⚠️ 同批那三处社交的滚动区走的都是会出声的那条 ⇒ 这里原来是**全批唯一一处可诊断性不一致**。
            SocialPage.RegisterScroll(_scroll);
            _content = Node(vp, "Content", new PxRect(ViewportR.x1, ViewportR.y1, ViewportR.x2, ViewportR.y1));
            BuildRows();

            if (MissingArt.Count > 0)
                Debug.LogWarning("[BattleLogPopup] ⚠️ 有 " + MissingArt.Count + " 张图取不到（**这些件没画**）："
                                 + string.Join("、", MissingArt.ToArray()));
        }

        /// <summary>逐行建（行几何**与档案窗那一页共用** `MatchLogRow`；行高 203.20 / 间距 25 —— 两扇窗逐值相同）。
        /// 数据源同样是 `Shell/BattleLogData.cs`（本地自建、默认空）。
        /// 🆕 2026-10-03（A25②）：行按**滚动偏移之后**的位置摆（`MenuScroll.Shift`），整行在视口外的**不建**。</summary>
        void BuildRows()
        {
            if (_content == null) return;
            MenuDraw.ClearChildren(_content);
            BuiltRows = 0;

            var all = BattleLogData.All;
            int n = all.Count;
            float h = n == 0 ? 0f : n * MatchLogRow.RowH + (n - 1) * MatchLogRow.RowGap;
            ContentBottom = ViewportR.y1 + h;
            // 🔴 **内容高要写进滚动区**（= 原版 `Content` 上 `ContentSizeFitter m_VerticalFit=1` 跑出来的高度；
            //   推算见普查 §B·补列：`Content` 高 = N × 203.2 + (N−1) × 25）。
            //   不写 ⇒ `ContentX1 == ContentX2 == Viewport.y1` ⇒ `ClampLo == ClampHi == 0` ⇒ **这一格滚不动**，
            //   而「整行滚出视口 ⇒ 不建」那道守卫会把第 5 行起**彻底藏掉**（不是「画到框外至少看得见」了）。
            //   ⚠️ 空表那一支也要写（写成 0）—— 否则上一次的内容高留在区里，是个静默的脏值（同 `LeaderboardWindow` 那条）。
            if (_scroll != null) _scroll.ContentX2 = ContentBottom;
            if (n == 0)
            {
                Debug.Log("[BattleLogPopup] 本地**没有对局记录**（原版读服务器）⇒ 照原版**留空**"
                        + "（这一扇窗原版也没有空态节点）。");
                return;
            }

            var vpR = _scroll != null ? _scroll.Viewport : ViewportR;   // 滚动区就是唯一那份；没有才退回常量
            _rowCtx.Art = Art; _rowCtx.Q = QRow; _rowCtx.Clip = vpR;
            for (int i = 0; i < n; i++)
            {
                float y = ViewportR.y1 + i * (MatchLogRow.RowH + MatchLogRow.RowGap);   // 内容坐标（原版 `Content` 空间）
                var rr = new PxRect(ViewportR.x1, y, ViewportR.x2, y + MatchLogRow.RowH);
                if (_scroll != null) rr = _scroll.Shift(rr);   // 内容坐标 → 屏幕坐标（**只做偏移、不裁**）
                // 🆕 2026-10-03：**整行滚出视口 ⇒ 连节点一起不建**（与档案窗那一页 `BattleLogTab.cs:182` 同形；
                //   那一页原来就有这道守卫，这一棵树上**一直缺**）。
                // 🔴 求交那一份 = `MenuDraw.ClipRect`（**全工程唯一一份**，别在这儿再写一遍 `Max/Min`）。
                // ⚠️ 它比 `MenuScroll.Intersects` 多判横轴 —— 这里安全：行的左右边**就是**视口的左右边
                //   （`ViewportR.x1/x2`），横轴恒相交。
                if (!MenuDraw.ClipRect(rr, vpR, out _)) continue;
                MatchLogRow.Build(_rowCtx, _content, rr, all[i]);
                BuiltRows++;                     // 现在 = **真建出来几行**（滚出视口的不算；断言用）
            }
            _rowCtx.Clip = null;
        }

        /// <summary>自检用：喂了数据之后重画。</summary>
        public void RebuildForTest() { BuildRows(); }
    }
}
