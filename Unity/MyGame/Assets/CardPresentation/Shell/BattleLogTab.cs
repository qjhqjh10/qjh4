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
// ---- 🔴 数据：空态（用户口径「具体的数据和排名这些可以空着」）----
// 数据源 = `Shell/BattleLogData.cs`（**本地自建、默认空**；原版读 `PlayerDataManager.battleLogData`，
// 那是服务器）。⇒ 这一页**现在是一块空面板**（原版没有空态节点，只有被清空的 `Content`）——
// **我们不自造空态文案**，只在日志里说清楚（见 `BuildRows`）。
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
        /// ⚠️ 行高/行距也在那边（`MatchLogRow.RowH` / `RowGap`）—— 本页只留「怎么排」。
        const float RowH = MatchLogRow.RowH, RowGap = MatchLogRow.RowGap;

        MenuScroll _scroll;
        Transform _content;
        BattleLogData.Match[] _rows = new BattleLogData.Match[0];

        /// <summary>这一屏建出来的行数（自检用：条数多时只建看得见的）。</summary>
        public int BuiltRows { get { return _rows.Length; } }

        /// <summary>行 builder 的**画图上下文**（取图 / 队列档 / 裁切）—— `MatchLogRow` 只认它、不认本页，
        /// 这样弹窗那边也能用同一份行。⚠️ `Clip` 是**逐次**设的（滚动区画内容前给、画完清）。</summary>
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
            var vpNode = Node(mt, "Viewport", vp);

            _scroll = NewScroll(vp, vp.W, 0f, true);
            // 🔴 滚轮要能重画（`MenuScroll` 只改 Offset，画是调用方的事）—— 不接 = 滚了什么都不动
            _scroll.OnChanged = RebuildRows;

            _content = Node(vpNode, "Content", new PxRect(vp.x1, vp.y1, vp.x2, vp.y1));

            BuildRows();
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
            var all = BattleLogData.All;
            int n = all.Count;
            float contentH = n == 0 ? 0f : n * RowH + (n - 1) * RowGap;
            // 内容上边 = 视口上边（原版 `Content` 锚在 `a=(0,1)-(1,1)`）；下边 = 行数算出来的高度
            float top = _scroll.Viewport.y1;
            _scroll.ContentX2 = top + contentH;
            _rows = new BattleLogData.Match[0];
            if (n == 0)
            {
                // 原版这一页**没有空态节点**（`OnOpen` 只是把 `Content` 清空）⇒ 我们照原版留空。
                // 🔴 **出声**：不然「一块空面板」看着像坏了／像没做（原版那块数据在服务器，我们本地一条都没有）。
                Debug.Log("[Profile] `Battle Log` 页：本地**没有对局记录**（原版读服务器的 "
                        + "`PlayerDataManager.battleLogData`）⇒ 照原版**留空**（它没有空态节点）。"
                        + "要接的话接在结算那一处：`Shell/BattleLogData.cs` 的 `Add()`。");
                return;
            }
            var built = new System.Collections.Generic.List<BattleLogData.Match>();
            var ctx = RowContext;
            ctx.Clip = _scroll.Viewport;        // 画内容前给一次（等价原版 `Viewport` 的 `RectMask2D`）
            for (int i = 0; i < n; i++)
            {
                float y = top + i * (RowH + RowGap);
                var r = _scroll.Shift(new PxRect(_scroll.Viewport.x1, y, _scroll.Viewport.x2, y + RowH));
                if (!_scroll.Intersects(r)) continue;
                MatchLogRow.Build(ctx, _content, r, all[i]);
                built.Add(all[i]);
            }
            ctx.Clip = null;
            _rows = built.ToArray();
        }
    }
}
