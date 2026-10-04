// LeaderboardRow.cs — 排行榜的**行**（两款行族的**唯一一份** builder）
//
// ============================ 出处（唯一正本） ============================
// `资料/普查产出_0927/排行榜_嵌入版与行族.md` —— §B（`PlayerRankingRow` 全表）· §C（`For Army` 的差别）·
// §D/§E（两款联盟行）· §F 重名 · §G 哪些值算不准 · §H 查不到的。
//
// ---- 🔴 五条判据（读原始 JSON 定的）----
// ① **行的两支**：**玩家行** `PlayerRankingRow`（有 `border`+`Icon` 头像格、`Guild Name` 出厂 **active**）
//    与**联盟行** `AllianceRankingRow Variant` / `Skulls Variant`（`border` 那格换成 `BadgeDrawer`→
//    `Frame`+`Badge`、**没有头像**、`Guild Name` 出厂 **inactive**）。两款联盟行**逐字段 diff 只差一件事**：
//    `RankingIcon` 的图（`40k_battle_Win Skull` 60.016×91.807 vs `40k_UI_icon_ranked_Skirmish` 108.3×165.667）。
// ② 🔴 **只有玩家行有 Button** —— 挂在 **`border`** 那一层，`m_TargetGraphic` = **`Icon` 的 Image**
//    （不是 `border` 自己的图）。联盟行**全族一个 Button 都没有**（点击靠 `AllianceBadgeDrawer.showItemInfoOnClick`，
//    本处 = 0）。⇒ 我们**照它建**：玩家行整格可点，联盟行不可点。
// ③ 🔴 **高亮不是「出厂关着、用时打开」** —— 出厂 `Background` 与 `BackgroundHighlight` **两个都 active**，
//    运行期靠 `Image.enabled = OwnPlayer / !OwnPlayer` 二选一（`UIRankingRow.Config`）。⇒ 我们按 `IsSelf` 画其中一张。
// ④ 🔴 **`Guild Name` 空串 ⇒ 原版把整个 GameObject 关掉**（`AllianceUIRankingRow.Config`）⇒ 我们照做（不画）。
// ⑤ ⚠️ **`Name Holder` 的 `Name` / `Guild Name` 高 0.00 是近似值**（VLG `ctrlH=1` + TMP 首选高度要字体度量，
//    工具自报 `unk`）。宽 `710.788`、起点 `290`、`Name Holder` 的 `733.95×100` 是**可信**的
//    ⇒ 那两行的**上下切分是我们按两段文字的分辨率推的**（不是原版值，下面 `NameR`/`GuildR` 标了）。
//    🔴 **上面「可信」的独立出处 = 原版 prefab 自己的序列化字段**（2026-10-06 重出：此前**只有** `menu_dump`
//      的读数背书，而它正是当天查出「TMP 首选尺寸被当 0、还标成确定值」那处旧口径的**当事工具**）：
//      `bundle_menus_assets_all` 里**全部 8 个** `Name Holder`（两款行族 × 独立 prefab / 内嵌副本各若干：
//      `PlayerRankingRow`(`RectTransform_2593917104962726882.json`) · `… For Army`(`_888457738768082983.json`) ·
//      `AllianceRankingRow Variant`(`_3609084321486446954.json`) · `… Skulls Variant`(`_5184539827280079228.json`) …）
//      字段**逐一相同**：`m_SizeDelta = (733.9500122070312, 100)` · `m_AnchorMin = m_AnchorMax = (0, 0.5)` ·
//      `m_AnchoredPosition = (290, 0)` · `m_Pivot = (0, 0.5)`。
//      🔑 **点锚点（`m_AnchorMin == m_AnchorMax`）⇒ uGUI 里尺寸恒等于 `m_SizeDelta`** —— 不经父框、
//      不经布局组、**不经字体度量** ⇒ 这个数**结构上就与工具那套布局模拟无关**（`Name` / `Guild Name` 的
//      宽 `710.78800` 同样是序列化值；**只有它们的高 0** 才是 `ctrlH=1` 运行时按 TMP 首选高写进去的
//      ⇒ 高度那一格仍属「查不到」）。
//      ⚠️ 没有第二处会改它：`Name Holder` 上**只有一个非 RT 组件**（`VerticalLayoutGroup`，`ctrlH=1`，
//      **没有 CSF**）；父节点（行根）也只有 `CanvasRenderer` + `PlayerUIRankingRow`，**不是布局组**。
//
// ⚠️ **坐标一律「绝对画布像素」** —— `MenuDraw.Node/Text/Rect/Hit` 收的都是**绝对矩形**（它们自己减父节点位置），
//    ⇒ 本文件里的行内常量是**行内坐标**，画之前一律过 `Abs(rowRect, …)`（别把行内坐标直接喂进去）。
// ============================ 与 `MatchLogRow` 的分工 ============================
// 两者共用同一个 `RowCtx`（取图 / 队列档 / 裁切边界），**不各写一份上下文**。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>一行要显示的东西（原版 `PlayerRankingRowData` / `AllianceRankingRowData` 的本地最小集）。
    /// 🔴 本地**一条都没有**（原版读服务器）—— 这张结构只为「自检能证明行画得对」而存在，见 `LeaderboardData`。</summary>
    public struct LeaderboardRowData
    {
        /// <summary>`Ranking`（名次）。</summary>
        public int Rank;
        /// <summary>`Name`（玩家名）。</summary>
        public string Name;
        /// <summary>`Guild Name`（联盟名 / 玩家所属公会）。**空串 ⇒ 原版整格关掉**（判据 ④）。</summary>
        public string Guild;
        /// <summary>`Points`。</summary>
        public string Points;
        /// <summary>`Icon` 的立绘名（`Avatar_<阵营>_<单位>`，走 `CardArt.Cosmetics`）。</summary>
        public string Avatar;
        /// <summary>是不是自己 —— 原版等于 `OwnPlayer`，决定画 `Background` 还是 `BackgroundHighlight`（判据 ③）。</summary>
        public bool IsSelf;
    }

    /// <summary>行族。**四款 = 两个家族 × 各自的两个变体**（判据 ①）。</summary>
    public enum LeaderboardRowFamily
    {
        /// <summary>`PlayerRankingRow`（`useZeroBasedRank = 1`）。</summary>
        Player,
        /// <summary>`PlayerRankingRow For Army` —— 与 `Player` **逐值相同**，只差 `useZeroBasedRank` 与字体特性。</summary>
        PlayerForArmy,
        /// <summary>`AllianceRankingRow Variant`（`RankingIcon` = `40k_UI_icon_ranked_Skirmish`）。</summary>
        Alliance,
        /// <summary>`AllianceRankingRow Skulls Variant`（`RankingIcon` = `40k_battle_Win Skull`）。</summary>
        AllianceSkulls,
    }

    /// <summary>排行榜行的**唯一一份**几何（两款行族共用；逐值来自正本 §B/§D）。</summary>
    public static class LeaderboardRow
    {
        /// <summary>行高 **100**、行距 **15**（`Content` 的 `VerticalLayoutGroup`：spacing 15 / align 1 UpperCenter；
        /// 三扇全屏榜与嵌入版**逐值相同**）。</summary>
        public const float RowH = 100f, RowGap = 15f;
        /// <summary>行宽 **1200**（内层 `Content` 实测 `360→1560`，左右各留 111）。</summary>
        public const float RowW = 1200f;

        // ---- 行内各件（**行内坐标**：x 从行左边起、y 从行顶边起；值 = 正本 §B 的绝对 rect − 行左上角）----
        //  `Background`/`BackgroundHighlight`（两族同）：1200×106.44，上下各溢出 3.22
        static readonly PxRect BgR = new PxRect(0f, -3.22f, 1200f, 103.22f);
        /// <summary>`Ranking`（名次）：`apos=(12,0)` 宽 100 高 71.915 · **hAlign = Center**。</summary>
        static readonly PxRect RankR = new PxRect(12f, 14.04f, 112f, 85.96f);
        /// <summary>玩家行的头像格（**这一层才是 Button**，判据 ②）。</summary>
        static readonly PxRect BorderR = new PxRect(135f, 0.98f, 255f, 107.42f);
        /// <summary>头像立绘（挂在 `border` 下，比 `border` 大一圈；`apos=(-11.5,11)` 204.635×167.11）。</summary>
        static readonly PxRect IconR = new PxRect(81.18f, -40.36f, 285.82f, 126.75f);
        /// <summary>联盟行的徽章格（`BadgeDrawer`；`Frame`/`Badge` **两个子件都没有 sprite** ⇒ 只建结构不画）。</summary>
        static readonly PxRect BadgeDrawerR = new PxRect(125.91f, 0f, 236.69f, 103.96f);
        static readonly PxRect NameHolderR = new PxRect(290f, 0f, 1023.95f, 100f);
        /// <summary>⚠️ `Name` 的**上下切分是我们推的**（判据 ⑤）：原版两行都印 0 高，真高要字体度量。
        /// 可信的是 `x 290..1000.79`、以及 `Name Holder` 是 100 高的 `VerticalLayoutGroup`（spacing 0 · Middle）。
        /// 我们按「40px 与 30px 两行文字各占一半」切 ⇒ 6..52 / 50..96。</summary>
        static readonly PxRect NameR = new PxRect(290f, 6f, 1000.79f, 52f);
        /// <summary>同上（推的）。</summary>
        static readonly PxRect GuildR = new PxRect(290f, 50f, 1000.79f, 96f);
        /// <summary>`RankingIcon`：**右对齐族**（`a=(1,.5) p=(1,.5) apos=(-150,-1.7)`）⇒ 右缘 = 行宽 − 150。</summary>
        static readonly PxRect IconRankR = new PxRect(941.70f, -31.14f, 1050f, 134.53f);
        /// <summary>同上，`Skulls Variant` 那份（宽 60.016 高 91.807）。</summary>
        static readonly PxRect SkullRankR = new PxRect(989.98f, 3.10f, 1050f, 94.91f);
        /// <summary>`Points`：`a=(1,.5) p=(1,.5)` 140×77.974 ⇒ 右缘贴行右缘。</summary>
        static readonly PxRect PointsR = new PxRect(1060f, 11.27f, 1200f, 89.24f);

        // ---- 图 / 颜色 / 字号 ----
        /// <summary>行底 = Unity **内置** `Background`（32×32 · 九宫 10,10,10,10 · ppu 200）。
        /// 🔴 两行用的是**同一张图**，只靠 `m_Color` 区分（判据 ③）。取自
        /// `bundle_Warpforge_unitybuiltinassets/Texture2D/Background.png`（`工具/import_original_art.py` 的 `BUILTIN_IMAGES`）。</summary>
        const string ArtRowBg = "Background";
        static readonly Vector4 BgBorder = new Vector4(10f, 10f, 10f, 10f);
        /// <summary>`Background` 色（正本 §B：`(0.83,0.192,0.428,0.165)`）。</summary>
        public static readonly Color BgNormalTint = new Color(0.83f, 0.192f, 0.428f, 0.165f);
        /// <summary>`BackgroundHighlight` 色（正本 §B：`(0.978,1,0,0.165)`）。</summary>
        public static readonly Color BgHighlightTint = new Color(0.978f, 1f, 0f, 0.165f);
        static readonly Color GuildColor = new Color(1f, 0.643f, 0.373f, 1f);

        const string ArtAvatarBorder = "Player_Profile_Border";
        const string ArtRankIcon = "40k_UI_icon_ranked_Skirmish";     // `RankingIcon`（玩家行 / 联盟 Variant）
        const string ArtSkullIcon = "40k_battle_Win_Skull";           // `RankingIcon`（联盟 Skulls Variant）

        const float RankPx = 50.3f, NamePx = 40f, GuildPx = 30f, PointsPx = 43.2f;
        const float RankMin = 18f, NameMin = 18f, GuildMin = 18f, PointsMin = 18f;

        // ---- 行内队列档（相对 `RowCtx.Q`）----
        const int L_Bg = 0, L_Art = 2, L_Text = 4, L_Text2 = 5, L_Hit = 8;

        /// <summary>行内坐标 → **绝对画布坐标**（`MenuDraw.*` 收的是绝对矩形）。</summary>
        static PxRect Abs(PxRect row, PxRect local)
        {
            return new PxRect(row.x1 + local.x1, row.y1 + local.y1, row.x1 + local.x2, row.y1 + local.y2);
        }

        /// <summary>画一行。`r` = 行在**画布绝对坐标**里的矩形（左上原点，宽 `RowW`、高 `RowH`）。</summary>
        public static void Build(RowCtx c, Transform parent, PxRect r, LeaderboardRowData d,
                                 LeaderboardRowFamily fam)
        {
            if (c == null || parent == null) return;
            bool alliance = fam == LeaderboardRowFamily.Alliance || fam == LeaderboardRowFamily.AllianceSkulls;

            var row = MenuDraw.Node(parent, alliance ? "AllianceRankingRow" : "PlayerRankingRow", r);

            // ---- 底 + 高亮（判据 ③：**二选一**，不是「加一层」）----
            // 🆕 2026-10-03：**行底九宫格也吃 `c.Clip` 了**（此前这一处漏了 —— 滚动区里行底一直画到视口外，
            //    因为 `Nine` 那时根本没有 `clip` 参数）。判据 / 求交那一份 = `MenuDraw.ClipRect`（唯一一份）。
            //    ⚠️ `BgR` **上下各溢出 3.22**（1200×106.44），压在视口边上的那一行正是靠这条截住的
            //    —— 整块在框外时 `Nine` 返回 null ⇒ **连节点一起不建**。
            MenuDraw.Nine(row, c.Art(ArtRowBg), Abs(r, BgR), BgBorder, 32f, 32f, c.Q + L_Bg,
                          d.IsSelf ? BgHighlightTint : BgNormalTint, true,
                          d.IsSelf ? "BackgroundHighlight" : "Background", clip: c.Clip);

            // ---- 名次（hAlign = **Center** ⇒ 不调 `AlignLeft`，`Label` 默认就是居中）----
            MenuDraw.Text(row, Abs(r, RankR), d.Rank > 0 ? d.Rank.ToString() : "", Color.white,
                          "Ranking", RankPx, c.Q + L_Text, RankR.W, RankMin);

            // ---- 头像格（玩家族）/ 徽章格（联盟族）----
            if (!alliance)
            {
                var borderAbs = Abs(r, BorderR);
                var border = MenuDraw.Node(row, "border", borderAbs);
                MenuDraw.Rect(border, c.Art(ArtAvatarBorder), borderAbs, "Image", c.Q + L_Art, null, true, c.Clip);
                // `Icon` 挂在 `border` 下（**原版层级如此**，不是挂在行上）
                var iconAbs = Abs(r, IconR);
                Texture2D iconTex = null;
                if (!string.IsNullOrEmpty(d.Avatar))
                {
                    iconTex = CardArt.Cosmetics(d.Avatar);
                    if (iconTex == null) c.Art(d.Avatar);      // 记进宿主的 `MissingArt`
                }
                if (iconTex != null)
                    // 🔴 队列**必须比 `border` 高一档** —— `Player_Profile_Border` 那张图的**中心是不透明黑**
                    //    （实测 RGBA=(0,0,0,255)），两张同队列时谁盖谁由「到相机的距离」定 ⇒ 立绘会被压成黑块。
                    //    原版层级是 `border` → `Icon`（子件后画）⇒ 我们照它：边框在后、立绘在前。
                    MenuDraw.Rect(border, iconTex, iconAbs, "Icon", c.Q + L_Art + 1, null, true, c.Clip);
                // 判据 ②：**整格可点**（原版 Button 在 `border` 上、`target` = `Icon` 的 Image）
                // 🆕 2026-10-03：命中区也吃 `c.Clip` —— 判据 = 原版 `RectMask2D` 的**射线那一面**
                //    （`IsRaycastLocationValid` = `RectTransformUtility.RectangleContainsScreenPoint`）
                //    ⇒ 滚出视口的行**点不到**、压在视口边上的那行命中区**截到视口内**（`MenuDraw.Hit` 转调 `ClipRect`）。
                MenuDraw.Hit(border, "Hit", borderAbs, c.Q + L_Hit, () => OnRowClicked(d), clip: c.Clip);
            }
            else
            {
                // `BadgeDrawer` → `Frame` + `Badge`：**两个子件都没有 sprite**（运行时由
                // `AllianceBadgeDrawer.Draw(Badge)` 画）⇒ 只建结构、不画（判据 ②/§D）。
                var drawerAbs = Abs(r, BadgeDrawerR);
                var drawer = MenuDraw.Node(row, "BadgeDrawer", drawerAbs);
                MenuDraw.Node(drawer, "Frame", drawerAbs);
                MenuDraw.Node(drawer, "Badge", drawerAbs);
            }

            // ---- 名字 + 公会名（判据 ④：`Guild` 空串 ⇒ 原版整格关掉 ⇒ 我们连节点都不建）----
            var holder = MenuDraw.Node(row, "Name Holder", Abs(r, NameHolderR));
            if (!string.IsNullOrEmpty(d.Name))
            {
                var nameAbs = Abs(r, NameR);
                var nm = MenuDraw.Text(holder, nameAbs, d.Name, Color.white, "Name", NamePx, c.Q + L_Text,
                                       NameR.W, NameMin);
                if (nm != null) MenuDraw.AlignLeft(nm, nameAbs);
            }
            if (!string.IsNullOrEmpty(d.Guild))
            {
                var guildAbs = Abs(r, GuildR);
                var gn = MenuDraw.Text(holder, guildAbs, d.Guild, GuildColor, "Guild Name", GuildPx, c.Q + L_Text2,
                                       GuildR.W, GuildMin);
                if (gn != null) MenuDraw.AlignLeft(gn, guildAbs);
            }

            // ---- 右端两件：`RankingIcon`（族徽 / 骷髅）+ `Points` ----
            bool skulls = fam == LeaderboardRowFamily.AllianceSkulls;
            MenuDraw.Rect(row, c.Art(skulls ? ArtSkullIcon : ArtRankIcon), Abs(r, skulls ? SkullRankR : IconRankR),
                          "RankingIcon", c.Q + L_Art, null, true, c.Clip);
            var pointsAbs = Abs(r, PointsR);
            var pts = MenuDraw.Text(row, pointsAbs, d.Points ?? "", Color.white, "Points", PointsPx,
                                    c.Q + L_Text, PointsR.W, PointsMin);
            if (pts != null) MenuDraw.AlignLeft(pts, pointsAbs);
        }

        /// <summary>行被点（玩家族）。**原版是开那个玩家的档案窗**（`profileButton`）——
        /// 🆕 2026-10-03（§三第 29 条 A3②）：我们**照做**（开**同一扇** `PlayerProfileWindow`），
        /// 但走 `CreateFor(mgr, 名字)` 那一支 ⇒ 六页画的是「服务器数据、本地没有」的**如实说明**，
        /// 🔴 **不拿本地自己那一份冒充他**（那会是假信息）。</summary>
        static void OnRowClicked(LeaderboardRowData d)
        {
            var mgr = WindowsManager.Instance;
            if (mgr == null)
            {
                Debug.LogWarning("[Leaderboard] 点了第 " + d.Rank + " 名「" + (d.Name ?? "")
                                 + "」—— 没有 `WindowsManager` ⇒ 开不了档案窗（原版 `profileButton` 开的那扇）");
                return;
            }
            var w = PlayerProfileWindow.CreateFor(mgr, d.Name);
            mgr.OpenWindow(w);
            Debug.Log("[Leaderboard] 点了第 " + d.Rank + " 名「" + (d.Name ?? "") + "」⇒ **开他的档案窗**"
                      + "（原版 `profileButton` 的语义）。⚠️ 他的资料在服务器 ⇒ 六页是如实说明那一支");
        }
    }
}
