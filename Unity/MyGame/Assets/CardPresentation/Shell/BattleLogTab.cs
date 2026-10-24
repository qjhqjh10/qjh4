// BattleLogTab.cs — 玩家档案窗第 4 页：`Battle Log Tab`（原版类名 `BattleLogTab`）
//
// ============================ 出处（唯一正本） ============================
// `资料/普查产出_0927/档案窗_BattleLog与页签按钮.md` —— §A·1 是**层 × 参数**表（工具逐行）·
// §A·4 补列（activeSelf / ppu / 九宫格）· **§B 是行模板 `logPrefab` 的追查** ·
// §C 入口/调用/监听 · §D 查不到的（**不猜**）。表 = `python 工具/menu_dump.py bundle_menus_assets_all --rt -574318498314159566 --depth 8 --md`
//
// ---- 🔴 这一页的三条判据（读反编译/原始 JSON 定的）----
// ① **行是「运行期 `Instantiate(logPrefab, holder)`」出来的，不是画在场景里的**：
//    `BattleLogTab.OnOpen` 先 `DestroyAllChildren(holder)` 再逐条 instantiate（§B·3 有伪码）
//    ⇒ 场景里 `Content` 底下那个 `Match Log` 实例**运行期必被销毁**，它只是美术的原位参照。
//    我们的做法同形：**`Content` 底下按条数逐行建**，条数 = 0 就什么都不建（原版也没有空态节点）。
// ② **`logPrefab` 全游戏只有一份**（`Tab` 与 `BattleLogPopup` 共用，§B·5）⇒ 这个行模板
//    就是后面「对局历史」那件要用的同一个模板 —— **别另写一份**。
// ③ **`Matches` 比页根宽 25px（两侧各溢）**：`sizeDelta=(50,−101.441)` ⇒ x 326.03..1771.97，
//    而页根是 351.03..1746.97。**真值**（原版就这么摆），别「对齐」掉。
//
// ---- 🔴 数据：本地自建（🆕 2026-10-24 E13 起**落盘**，并带录像兜底）----
// 数据源 = `Shell/BattleLogData.cs`（**本地自建**；原版读 `PlayerDataManager.battleLogData`，那是服务器）。
// · 表默认是空的（不编假数据）；打过局之后结算处写一条（`BattleDriver.RecordBattleLog`），并**落盘**。
// · 🆕 页面读的是 **`BattleLogData.All`（实时）+ `BattleLogData.FallbackRows()`（盘上录像的兜底行，按页缓存）**
//   （历史项丢了但录像还在时，玩家仍列得出、点得动那些录像）。缓存理由与门 → `_extra` 的注释 / `FallbackRows`。
// ⇒ 仍然**不自造空态文案**（原版没有空态节点），只在日志里说清楚（见 `BuildRows`）。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `Battle Log Tab`（`BattleLogTab`）。两个字段：`holder`（= `Content`）、`logPrefab`（行模板）。</summary>
    public class BattleLogTab : ProfilePage
    {
        public override WindowTabType Type { get { return WindowTabType.ProfileBattleLog; } }
        protected override int PageIndex { get { return 3; } }
        public override PxRect PageRect
        {
            get { return new PxRect(PlayerProfileWindow.ContentL, PlayerProfileWindow.RedT,
                                    PlayerProfileWindow.ContentR, PlayerProfileWindow.AreaB); }
        }

        // ============================================================ 真值（绝对画布像素）
        // `Matches`（`ScrollRect`）：a=(0,0)-(1,1) p=(.5,.5) pos=(0,6.80032) sz=(50,−101.441)
        // ⇒ 326.03,162.84 → 1771.97,904.48（**比页根宽 25、两侧各溢**，判据 ③）。
        // ⚠️ 它的 `m_Sprite` 是 **UGUI 内置 `Background`**、**`m_Color=(1,1,1,0)`（a=0 ⇒ 看不见）** ⇒ **不画**。
        static readonly Vector2 MtA = UguiRect.A00, MtB2 = UguiRect.A11;
        static readonly Vector2 MtPos = new Vector2(0f, 6.80032f), MtSz = new Vector2(50f, -101.441f);
        /// <summary>`Viewport`：`sz=(0,−17)` ⇒ 底边 **887.48**（比 `Matches` 少 17）。`Mask` + `showGraphic=0` ⇒ 只建节点。</summary>
        const float VpSzY = -17f;
        /// <summary>`Content` 的 `ContentSizeFitter`：🔴 **2026-09-27 订正** —— 原来写「两个 Fit 字段**查不到**，
        /// 我们按 `VerticalFit=PreferredSize` 算」。真值是 **`m_HorizontalFit=0` / `m_VerticalFit=1`（= `MinSize`）**
        /// （普查 §D3 直读 MB `-321169639700006350`）。⚠️ **行为一字不用改**：这一处的 `ChildControlHeight=0`
        /// ⇒ `min == preferred == sizeDelta.y`，两种 Fit 在这个配置下等价；**要改的只是这条注释**。
        /// 📌 同族教训：`menu_dump` 的「字段: …」印的是**字段名**不是值，别拿它当「查不到」（`资料/已知的坑.md`）。
        ///
        /// 🔴 **行本身（原版 `logPrefab`）整棵树抽到了 `Shell/MatchLogRow.cs`** —— 原版
        /// `BattleLogTab.logPrefab` 与 `BattleLogPopup.logPrefab` 是**同一个 prefab**
        /// ⇒ 两扇窗必须共用一份行几何（普查 `对局历史_行模板与弹窗.md` §B·补列 ①）。
        /// ⚠️ 行高/行距也在那边（`MatchLogRow.RowH` / `RowGap`）—— 本页只留「怎么排」。</summary>
        const float RowH = MatchLogRow.RowH, RowGap = MatchLogRow.RowGap;

        MenuScroll _scroll;
        Transform _content;
        BattleLogData.Match[] _rows = new BattleLogData.Match[0];
        /// <summary>🆕 **2026-10-24（E13）**：**读盘那一半**的快照（= `BattleLogData.FallbackRows()`：
        /// 盘上有录像、本表没有对应记录的那几条）。
        /// 🔴 为什么只缓存这一半：读盘贵（最多 50 个 JSON），而 `RebuildRows` 会被滚轮反复调用；
        /// 而**本表**是**每次现读**（`BattleLogData.All`）—— 自检喂一条再 `RebuildRows` 的老口径
        /// 因此**一字不变**（`Editor/MainMenuScene.cs` 那几条 `BuiltRows` 断言照旧成立）。
        /// 数据变了要重来：调 <see cref="RefreshRows"/>（或重开这一页）。</summary>
        List<BattleLogData.Match> _extra = new List<BattleLogData.Match>();

        /// <summary>这一屏建出来的行数（自检用：条数多时只建看得见的）。</summary>
        public int BuiltRows { get { return _rows.Length; } }

        /// <summary>这一页那个**纵向**滚动区（自检用 —— 同 `LeaderboardWindow.RowsScroll` 那条理由：
        /// 断言要能读到**档位**）。🔴 原版档位 = `Matches` 的 `m_MovementType = 1`（**Elastic**），
        /// 判据写在 `Build()` 里 `_scroll.Elastic = true;` 那一段。</summary>
        public MenuScroll RowsScroll { get { return _scroll; } }

        /// <summary>行 builder 的**画图上下文**（取图 / 队列档 / 裁切）—— `MatchLogRow` 只认它、不认本页，
        /// 这样弹窗那边也能用同一份行。
        /// 🔴 **2026-10-13（A435 阶段 2 · 丙）**：`RowCtx.Clip` **本页不再写**（原来这里写
        /// `ctx.Clip = _scroll.Viewport` / 末尾清 `null`）—— 裁切状态已经长在 `Build()` 建的
        /// `Viewport` 节点上（`ViewportClip`），行 builder 传的 `null` 会沿父链解析到它。
        /// ⚠️ 那个字段**留着**（= 显式覆盖那一档，同 `GameWindow.Clip` 的处置），只是生产路径上恒为 `null`。</summary>
        readonly RowCtx _rowCtx = new RowCtx();
        RowCtx RowContext { get { _rowCtx.Art = Art; _rowCtx.Q = Q; return _rowCtx; } }

        // ---------------------------------------------------------- 建

        protected override void Build()
        {
            var page = PageRect;
            var mtR = UguiRect.Child(page, MtA, MtB2, MtPos, MtSz);            // `Matches`
            var mt = Node("Matches", mtR);
            // ⚠️ `Matches` 自己的底（UGUI 内置 `Background`）**a=0 ⇒ 原版看不见** ⇒ 我们不画（判据在常量注释）

            var vp = UguiRect.Child(mtR, UguiRect.A00, UguiRect.A11, UguiRect.P01, Vector2.zero,
                                    new Vector2(0f, VpSzY));                    // `Viewport`（Mask · showGraphic=0）
            var vpVc = ViewportClip.Hang(mt, "Viewport", vp, Vector4.zero, Vector2Int.zero);
            var vpNode = vpVc.transform;
            // 🔴 **2026-10-13（A435 阶段 2 · 丙）**：上面这一句把「视口节点 = 裁切状态的载体」定下来
            //    （= 原版 `Viewport` 那个 `RectMask2D`；参数取原版实读的全 0 ⇒ 与迁移前的 `ctx.Clip = vp` 逐位同值）。
            //    行 builder（`MatchLogRow`）**本页不再喂 `RowCtx.Clip`** —— 那个字段在生产路径上恒 `null`，
            //    于是 `MenuDraw.*` 沿父链解析到**这颗节点**（见 `Shell/MatchLogRow.cs` 的四处读点）。

            _scroll = NewScroll(vp, vp.W, 0f, true, 50f);   // 原版 `Battle Log Tab/Matches` = 50（D_N29）
            // 🔴 **2026-10-13（A465 · W-A435己）**：构建循环那一行（`BuildRows` 里
            //   `if (!_scroll.Intersects(r)) continue;`）从今天起读**同一颗节点**的状态
            //   （`MenuScroll.Intersects` 走 `ClipNode.State.RenderClip`）—— 就是上面那颗 `Viewport`。
            //   ⚠️ 今天两值同（节点框 = `vp`、`padding` 全 0）⇒ **逐个位不变**。
            _scroll.ClipNode = vpVc;
            // 🔴 档位 = 原版 `Matches` 的 `m_MovementType = 1` ⇒ UGUI **Elastic**
            // （真值 `0 Unrestricted / 1 Elastic / 2 Clamped`，本地 UGUI 源码亲读）。
            // 判据 = 原始 JSON 实读：`python 工具/menu_dump.py bundle_menus_assets_all "Battle Log Tab"`
            // ⇒ `Matches` = `h=0 v=1 mode=1`。⛔ **别套那一族的另一档**：`Battle Log Popup` 的 `Matches` 是 `mode=2`(Clamped)
            // （`资料/普查产出_0927/对局历史_行模板与弹窗.md:149-150` 两行对照表）。
            _scroll.Elastic = true;
            // 🔴 滚轮要能重画（`MenuScroll` 只改 Offset，画是调用方的事）—— 不接 = 滚了什么都不动
            _scroll.OnChanged = RebuildRows;

            _content = Node(vpNode, "Content", new PxRect(vp.x1, vp.y1, vp.x2, vp.y1));

            BuildOpenPopupButton(page, mtR);
            // 🆕 2026-10-24（E13）：**读盘那一半**拍一次快照（本表那一半每次现读，见 `_extra` 的注释）。
            _extra = BattleLogData.FallbackRows();
            BuildRows();
        }

        // ============================================================ 🆕 2026-09-27：开「对局历史弹窗」的入口
        //
        // 🔴 **这个入口是【我们定的】，不是复刻** —— 用户 2026-09-27 拍板「接在档案窗 `Battle Log` 页」。
        //    原版那扇窗（`Battle Log Popup`）的**打开点本地查不到**：它只出现在 `WindowsManager` 的
        //    预载表里，`OpenWindow<BattleLogPopup>()` 那个泛型调用的**产物缺失**
        //    （普查 `对局历史_行模板与弹窗.md` §E：74 处 `OpenWindow(` 逐个看过 + 全 91 包按字节搜 GUID/pid 零命中）
        //    ⇒ 照红线**不编入口**了整整一轮；**这一颗是用户拍板补的**，所以：
        //    ① 它**不在原版那一页的节点表里**（那一页只有 `Matches`→`Viewport`→`Content`）；
        //    ② 摆在**列表上方那条空带**里（页顶 118.917 → 列表顶 162.84，**其余页也没占**），
        //       **不去动任何一个原版节点的 rect**；
        //    ③ 文案用原版自己的词（`Battle Log`），但我们不假装它是原版的东西。
        /// <summary>🆕 **2026-10-24（调度台 · 真机发现）**：这颗钮上屏改走**官方词条**
        /// `PlayerProfile/BattleLog`（中文「战斗记录」），取不到才回英文原文。
        /// ⚠️ 钮本身仍是**我们补的**（原版打开点本地查不到，用户 2026-09-27 拍板），
        /// 这里只把「用原版自己的词」落到**当前语言** —— 中文档下原来印英文 `Battle Log`。</summary>
        public const string PopupButtonFallback = "Battle Log";
        public static string PopupButtonLabel
        {
            get
            {
                string v = Loc.T("PlayerProfile/BattleLog");
                return string.IsNullOrEmpty(v) || v == "PlayerProfile/BattleLog" ? PopupButtonFallback : v;
            }
        }
        /// <summary>按钮矩形（绝对画布像素）—— 右上对齐到内容区右缘、落在列表上方那条空带里。</summary>
        public static PxRect PopupButtonRect(PxRect page)
        {
            float h = 36f;
            float y1 = page.y1 + 4f;                 // 118.917 + 4 = 122.92（离页顶 4、离列表顶还有 ~40）
            return new PxRect(page.x2 - 240f, y1, page.x2, y1 + h);
        }

        void BuildOpenPopupButton(PxRect page, PxRect listRect)
        {
            if (_popupBtn != null) return;           // 建过一次就复用（`Build()` 可能被重跑）
            var r = PopupButtonRect(page);
            // ⚠️ 别压到列表：这条只是防呆（真值上差着 40px）
            if (r.y2 > listRect.y1 - 2f) { Debug.LogWarning("[Profile] 对局历史入口与列表重叠了 —— 没建"); return; }

            _popupBtn = Node("Open Log Popup Button", r);
            // 底图用壳里那颗通用的 `UI_Button_Mulligan`（**没有九宫 border** ⇒ 走拉伸，同 `DeckSelectionPopup` 那条注释）
            Rect(_popupBtn, ArtButton, r, "Image", 2);
            // 🔴 **2026-10-08（A212）这一处必须显式声明 `wrap`**：`ProfilePage.Text` 从本批起
            //    **`autoFit` 不再隐含折行**（见那个方法的头）⇒ 不写这一句，这颗钮的字会**静默**从
            //    `Normal` 变成 `NoWrap`。而**它的折行判据是空的** —— 见上面那一段：这颗钮**不在原版那一页的
            //    节点表里**（原版打开点本地查不到，用户 2026-09-27 拍板补的），我们**没有**原版
            //    `m_TextWrappingMode` 可对 ⇒ 按纪律「判据空 ⇒ 保持现状并标明是我们挑的」，**保持**折行。
            //    ⛔ 别把这里改成 `wrap: false` 又当成「复刻」—— 那只是换一种我们挑的写法。
            Text(_popupBtn, PopupButtonLabel,
                 new PxRect(r.x1 + 12f, r.y1 + 4f, r.x2 - 12f, r.y2 - 4f),
                 Color.white, "Button Text", 26f, 4, autoFit: true, autoMinPx: 12f,
                 alignLeft: false, wrap: true);
            // 🔴 **2026-10-12（A323）**：`alignLeft: false` 显式补上 —— **零行为变化**
            //   （`ProfilePage.Text` 的 `alignLeft` 缺省本来就是 `false`）。
            //   ⚠️ 这一颗的**对齐判据与折行判据一样是空的**：它**不在原版那一页的节点表里**
            //   （用户 2026-09-27 拍板补的自建钮，见上面那一段）⇒ 「`Center`」**不是原版读数，
            //   是我们沿用旧缺省值**（= 保持现状、不假装复刻）。要改这一档得先拿到原版节点。
            Hit(_popupBtn, "Hit", r, 9, OpenPopup);
        }

        Transform _popupBtn;
        const string ArtButton = "UI_Button_Mulligan";

        /// <summary>开那扇窗。**开不出来要出声**（红线：不许静默失败 —— `OpenByRef` → `EnsureHost`
        /// 在真的没有 `WindowsManager` 时会现建一台并出声，所以这里不再自己判一次）。
        /// 🆕 **2026-10-11（A177）**：改走 `WindowsManager.OpenByRef`（= 原版 `automaticallyLoadedWindows`
        /// 命中就复用）—— 原来直调 `BattleLogPopup.Create` ⇒ **连点两次叠出两扇**。</summary>
        void OpenPopup()
        {
            WindowsManager.OpenByRef(WindowsManager.PrefabRefBattleLog, m => BattleLogPopup.Create(m));
            Debug.Log("[Profile] 「对局历史弹窗」入口（**这一颗是我们加的** —— 原版的打开点查不到，"
                      + "用户 2026-09-27 拍板接在 `Battle Log` 页）⇒ 开了 `Battle Log Popup`");
        }

        /// <summary>滚轮改了偏移 ⇒ 重画（**先清再建**，回调会重入 —— 同 `ForgeTab.BuildRewardCells` 那条）。
        /// 数据变了也调它（原版 `OnOpen` 就是「清空 + 逐条 instantiate」那两步）。</summary>
        public void RebuildRows()
        {
            if (_content == null) return;
            for (int i = _content.childCount - 1; i >= 0; i--) DestroyNow(_content.GetChild(i).gameObject);
            BuildRows();
        }

        /// <summary>逐行建。`Content` 的 `VerticalLayoutGroup`：**spacing 25 / UpperLeft**，行高由行自己定（203.20）。
        /// ⚠️ 视口外的整行**不建**（省 quad，顺带它的点击区也不存在 —— 等价 `RectMask2D` 裁掉）。</summary>
        void BuildRows()
        {
            // 🆕 2026-10-24（E13）：**本表实时**（`RebuildRows` 的老口径不变）+ **盘上录像兜底行走快照**。
            //   兜底行接在本表**后面**（本表是「真打过的记录」，新的在前）。
            var own = BattleLogData.All;
            var ex = _extra != null ? _extra : new List<BattleLogData.Match>();
            int n = own.Count + ex.Count;
            float contentH = n == 0 ? 0f : n * RowH + (n - 1) * RowGap;
            // 内容上边 = 视口上边（原版 `Content` 锚在 `a=(0,1)-(1,1)`）；下边 = 行数算出来的高度
            float top = _scroll.Viewport.y1;
            _scroll.ContentX2 = top + contentH;
            _rows = new BattleLogData.Match[0];
            if (n == 0)
            {
                // 原版这一页**没有空态节点**（`OnOpen` 只是把 `Content` 清空）⇒ 我们照原版留空。
                // 🔴 **出声**：不然「一块空面板」看着像坏了／像没做（原版那块数据在服务器，我们本地一条都没有）。
                Debug.Log("[Profile] `Battle Log` 页：本地**没有对局记录**、盘上也没有可读的录像文件 ⇒ 照原版**留空**"
                        + "（它没有空态节点）。");
                return;
            }
            var built = new List<BattleLogData.Match>();
            var ctx = RowContext;
            for (int i = 0; i < n; i++)
            {
                var m = i < own.Count ? own[i] : ex[i - own.Count];
                float y = top + i * (RowH + RowGap);
                var r = _scroll.Shift(new PxRect(_scroll.Viewport.x1, y, _scroll.Viewport.x2, y + RowH));
                if (!_scroll.Intersects(r)) continue;
                MatchLogRow.Build(ctx, _content, r, m);
                built.Add(m);
            }
            _rows = built.ToArray();
        }

        /// <summary>🆕 **2026-10-24（E13）**：重拍「读盘那一半」再重画（数据变了调它）。
        /// ⚠️ **滚轮不走这里**（见 `_extra` 的注释：读盘贵）—— 滚轮走 <see cref="RebuildRows"/>。</summary>
        public void RefreshRows()
        {
            _extra = BattleLogData.FallbackRows();
            RebuildRows();
        }
    }
}
