// RankedTab.cs — 玩家档案窗第 6 页：`Ranking Tab`（键名 `Ranked`、文案 `Ranking`、原版类名 `RankedTab`）
//
// ============================ 出处（唯一正本） ============================
// `资料/普查产出_0927/档案窗_Ranking页与图名表.md` —— §A 层×参数（工具逐行）· §A·1 activeSelf ·
// §A·2 图参数 · §A·3 段位图那一套（**13 张，不是金/银/铜**）· **§A·4 工具的两处偏差（表值要改口）** ·
// §A·5 重名 · **§A·6 序列化引用（`top4Factions` 乱序！）** · §C 入口调用监听 · §D 查不到的。
// 表 = `python 工具/menu_dump.py bundle_menus_assets_all --rt -1610774836786529742 --depth 8 --md`
//
// ---- 🔴 这一页的四条判据（读反编译/原始 JSON 定的）----
// ① **`top4Factions` 的顺序不是树序**：数组是 `#1, #3, #2, #4`（= 左列上、右列上、左列下、右列下）。
//    照树序抄会张冠李戴（§A·6）。⇒ 我们**按格子的名字摆**（`#1..#4` 就是它们在树上的位置），
//    自检另外断「数组顺序 ≠ 树序」这件事（见 `Top4Order`）。
// ② **前 4 行复用树上的 `#1..#4 FactionScoreBig`，第 5 名以后才 `Instantiate(factionScorePrefab)`**（§C·2）。
//    我们的**阵营分是服务器的**（`LeaderboardManager.GetLeaderboard`）⇒ 空态：4 格都建、**不填分数**。
// ③ **`DivisionImage` 那一套是 7 大段位 + 6 个名次数字**（`01-Rook`…`07-Legend` / `Roman I–VI`），
//    **不是** `WF_UI_Ranked_Background_{Gold,Silver,Bronze}`（那是 **Profile 页**传奇奖杯的底，§A·3 ④）。
//    选哪张由 `RankedDivisions.GetDivisionData(divisionNumber)` 决定 —— **那个 SO 本地没有**（§D·1）。
//    🔴 **2026-10-24（A1）就地订正（铁律 5）**：本行原文写「⇒『段位号 → 图名』的对照表**拿不到**，
//      我们**不画段位图**（画了就是编一个段位）」—— **已过期**：`RankedDivisionInfo.DivisionArt` 那张表
//      现在有实测证据（4=`04-Admiral` / 7=`07-Legend` 两档逐块像素钉死，其余按图名序号 + 图案单调升级），
//      ⇒ 本页**照事件窗同一份判据**画（`RankedDivisionInfo.DivisionArtName(DemoDivisionNumber)`）——
//      **两个窗口不可能画出不同的段位**。段位号仍是**演示值**（`DemoDivisionNumber`，我们挑的，真值在服务器）。
//      ⚠️ 本页的 `DivisionText` 装的是大标题 `'Global Rating'`（**不是段位名**，§A·3）⇒ **只画图、不画段位名**。
//      `RankImage`（罗马数字）**仍关着** —— 名次号也在服务器（判据与表的归属见 `RankedDivisionInfo.RankArt`）。
// ④ **两处 `Main Icon` 用的图不一样**（§A·2 末）：值那一行 = `40k_UI_icon_ranked_Skirmish`、
//    最高分那一行（`MaxRating` 与 `Alliance Rating Display (1)`）= **`Menu_Icon_Galon`**。**别全填一张**。
//
// ---- 🔴 §A·4 那两处「表值要改口」（照抄会错位）----
//   · **布局组里若有 `activeSelf=false` 的子节点，工具表把它们也算进了主轴** ⇒ 那几个 `Main Icon` /
//     `Individual rating value` 的 x **要减掉 `Secondary Icon` 的宽**（44.4 / 60）。**本文件用的已是修正值**。
//   · `⚠️unk` 的五个布局组（`left-side` / `center` / `right-side` / `MainRating` / `Name and Title Holder`）
//     —— 工具算不出（链上撞到 TMP 首选尺寸）⇒ 表里 `#1..#4 FactionScoreBig` 的**宽 0.00 不是真值**。
//     🔴 **2026-10-06 重出独立出处**（此前**只有** `menu_dump` 的读数背书 —— 而它正是当天查出
//     「TMP 首选尺寸被当 0、还标成确定值」那处旧口径的**当事工具**）。两条：
//     ① **那个 `0.00` 不是原版字段**：`bundle_menus_assets_all` 的 `Ranking Tab` 树
//        `Top4/content/left-side` 下四格（`RectTransform/RectTransform_795116902452329010.json` = `#1`；
//        `#2/#3/#4` 逐值相同）**序列化**是 `m_SizeDelta = (190, 230.44754)` · `m_AnchoredPosition = (105, -125.22377)`；
//        而表里那两列写 `(0,230.448)` / `(10,…)` ⇒ **那两列是工具算完布局之后的值**。
//     ② **真值 = 190（= 列内宽），不是 0、也不是 164** —— 拿 prefab 字段 + uGUI 算法**自己算**：
//        `left-side` 的 VLG 序列化字段 `m_ChildControlWidth = 1 · m_ChildForceExpandWidth = 0`（**组**管子件宽）
//        ⇒ `SetChildrenAlongAxis` 的 `controlSize=true` 支 ⇒ 交叉轴那一行
//        `requiredSpace = Mathf.Clamp(innerSize, min, flexible > 0 ? size : preferred)`
//        = `Clamp(210 − 2×10, 164, 210) = 190`（`HorizontalOrVerticalLayoutGroup.cs:167`；`:146` 的 `size` = 组的整宽）。
//        其中 `min = preferred = 164` —— 卡自己的 VLG `ctrlW = 0` ⇒ 交叉轴取**子件 `sizeDelta.x` 的最大值**
//        （`:114-118` 的 `Mathf.Max`）= `icon` 的 164（`Image` 的 sliced 首选宽够不到它）；`flexible = 1`
//        —— 卡自己的 VLG `expandW = 1` + 子件 `ctrlW = 0` ⇒ 每个子件 flexible=1、取 max（`:124-127`）。
//        🔑 **独立佐证**：卡内三件的**序列化** `m_AnchoredPosition.x = 95` —— 只有卡宽 = **190** 时，
//        「164 宽的子件在卡内居中」（卡内 VLG `align = 4` MiddleCenter ⇒ `:167-176` 的 `offsetInCell`）
//        才 = `(190−164)/2 + 164×0.5 = 13 + 82 = 95`。⇒ **卡宽 190 · 子件宽 164 · 子件左缘 = 卡左 + 13**。
//        ⚠️ 行号取自本机 uGUI 包 `MyGame/Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/Layout/`
//        ——我们的版本，原版是另一版 Unity ⇒ **认方法名、别认行号**。
//     ✅ **2026-10-06（A146）已照原版改回**：`CardW = 190`（**卡矩形宽**）+ 新增 `CardInnerW = 164` /
//        `CardInnerOff = 13`（**卡内三件**）—— `BuildCard` 里吃 `CardW` 的那 4 处一起换过。
//        改前是 `CardW = 164` 把**卡**也画成了 164 宽
//        ⇒ 卡底比原版**窄 26**、卡内三件整体**左了 13**（`BuildCard` 里 `r` 与三个子件 x 都吃 `CardW`）。
//        正确 = **卡矩形宽 190（= `left-side` / `right-side` 的 `210 − 2×pad 10`）· 卡内三件仍 164、居中共 +13**。
//        断言同步：`Editor/MainMenuScene.cs` 那条 `left + 190f` + **新增的四张卡子件断言**（此前一个都没有）。
//
// ---- 🔴 数据：**全是服务器的**（用户口径「具体的数据和排名这些可以空着」）----
// 段位 / 名次 / 全局评分 / 各阵营评分 —— 本地一条都没有（§C·2 说明它们来自 `LeaderboardManager`）⇒
// 逐格空态：分数用原版预制体自己的空态串（`'------'` / `'3000'` 那种占位**不抄**，见下），
// **段位图按演示段位号画**（见 ③，2026-10-24 A1）、名次图**不画**。头像/名字/等级那一块**照 Profile 页那套**（我们有真名字与真头像）。
// ⚠️ 头像那一格是**另一个实例**（`EverguildButton.target` 不同，§A·5 跨页重名）⇒ 我们仍从 `AvatarTab` 取，
//    但**不共用 Profile 页的实例**（两个页根各建一份，与正本一致）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `RankedTab`（`WindowTabBase&lt;PlayerProfileMenu&gt;`）。六个字段：
    /// `divisionImage, factionListHolder, factionScorePrefab, globalRating, infoSection, top4Factions`。</summary>
    public class RankedTab : ProfilePage
    {
        public override WindowTabType Type { get { return WindowTabType.ProfileRanking; } }
        protected override int PageIndex { get { return 5; } }
        public override PxRect PageRect
        {
            get { return new PxRect(PlayerProfileWindow.ContentL, PlayerProfileWindow.RedT,
                                    PlayerProfileWindow.ContentR, PlayerProfileWindow.AreaB); }
        }

        // ============================================================ 队列档（页内分层）
        const int L_Bg = 0;      // 面板 / 卡片底
        const int L_Bg2 = 1;     // 第二层底
        const int L_Art = 2;     // 图标 / 立绘
        const int L_Frame = 3;   // 边框 / 钮底 / 描边
        /// <summary>**压在头像框之上**那一档（2026-09-27 修，与 `ProfileTab` 同一处病）：
        /// 原版 `Player_Profile_Border` 的中心是**不透明黑**，兄弟序是 `Highlight → Border → Image`
        /// ⇒ 立绘必须排在边框**之后**。原来这里是 `L_Art`(2) &lt; `L_Frame`(3) ⇒ 立绘被压成黑块。
        /// 判据 → `ProfileTab.L_ArtOverFrame` 那段注释（含实据路径）。</summary>
        const int L_ArtOverFrame = L_Frame + 1;
        const int L_Text = 4;    // 正文
        const int L_Text2 = 5;   // 次要文字
        const int L_Hit = 6;     // 命中区

        /// <summary>`top4Factions` 的**数组顺序**（§A·6：**不是树序**）—— 只用来给自检断这件事。
        /// 我们摆格子按**树上的名字** `#1..#4`，不按这个数组。</summary>
        public static readonly string[] Top4Order = { "#1", "#3", "#2", "#4" };

        // ============================================================ 🆕 2026-10-24（`U2` V2）：`Top4` 两支
        //
        // 🔴 **问题**：原版 `Top4` = **四名玩家的阵营大分**（`#1..#4 FactionScoreBig`：阵营徽记 +
        //    大分 + 最高分）；我们的分数**在服务器**（`LeaderboardManager`，本地一条都没有）⇒ 画出来
        //    就是「空框 + 双箭头 + 破折号」四块占位 —— 用户报的正是这个（V2）。
        // ⇒ **两支**（`资料/普查产出_1024c/U2_账户窗多余按钮复查.md` §5·2 V2）：
        //    · **有数据**：四格照原版画（图 + 值 + 最高分）；
        //    · **无数据**：**整格不画**（`SetActive(false)`）—— ⛔ 不留空框、不印破折号。
        //    ⚠️ 节点**照建**（结构 = 原版 prefab 的 `#1..#4`，宿主断言按原版矩形量它们；
        //       同 `AvatarTab` 的 `Avatar Name`、`ProfileTab` 的 `Avatar Name` 那条处置）。
        //    ⚠️ 数据入口 = `Top4Data`（**现在恒空** = 服务器没有本地副本）。哪天接上真数据，
        //       只往这里填，绘制那一半不用改；⛔ 别在绘制里编数字。

        /// <summary>`Top4` 一格的数据（= 原版 `top4Factions[i]` 里运行期填进去的那三样）。</summary>
        public struct Top4Entry
        {
            /// <summary>阵营徽记的图名（🆕 2026-10-29 `B97_W13`：必须是原版 `IconsByArmySO` 那 13 条之一 —— `FactionOfSprite` 按它反查 `CardArmy`；空 / 认不出 ⇒ 不画图、只出声）。</summary>
            public string FactionArt;
            /// <summary>大分（`Alliance Rating Display` 那一格）。空 ⇒ 该格退回原版空态串 `------`。</summary>
            public string Rating;
            /// <summary>最高分（`MaxRating` 那一格）。空 ⇒ 同上。</summary>
            public string MaxRating;
        }

        /// <summary>四名玩家的阵营大分（**下标 = 名次−1**，与树上 `#1..#4` 同序）。
        /// 🔴 **默认空**：原版这块读 `LeaderboardManager.GetLeaderboard`（服务器）⇒ 本地没有来源
        /// （§C·2 / 文件头「数据」那一节）。⛔ 别为了「看着有东西」塞假数据。</summary>
        public static Top4Entry[] Top4Data = new Top4Entry[0];

        /// <summary>有没有 `Top4` 数据（= 四格画不画的总闸）。</summary>
        public static bool HasTop4Data { get { return Top4Data != null && Top4Data.Length > 0; } }

        /// <summary>`#N` 那一格的数据；越界 / 没有 ⇒ `null`（= 无数据支）。</summary>
        public static Top4Entry? EntryOf(int rank)
        {
            if (!HasTop4Data || rank < 1 || rank > 4) return null;
            int i = rank - 1;
            if (i >= Top4Data.Length) return null;
            return Top4Data[i];
        }

        // ============================================================ ① `Profile Player Info`（与 Profile 页同构）
        const float PiL = 351.03f, PiT = 168.16f, PiR = 1186.98f, PiB = 320.54f;
        const float AvL = 351.03f, AvT = 168.16f, AvR = 510.19f, AvB = 320.54f;
        const float AvIcL = 351.03f, AvIcT = 168.16f, AvIcR = 510.19f, AvIcB = 283.18f;
        const float AvHlL = 351.03f, AvHlT = 164.63f, AvHlR = 513.52f, AvHlB = 281.50f;
        const float AvBdL = 351.03f, AvBdT = 179.66f, AvBdR = 510.19f, AvBdB = 294.68f;
        const float AvImL = 351.03f, AvImT = 165.46f, AvImR = 510.19f, AvImB = 280.48f;
        const float AvNmB = 361.85f;
        const float InsL = 510.18f, InsT = 168.16f, InsR = 1405.87f, InsB = 320.54f;
        const float IhL = 510.19f, IhT = 168.16f, IhR = 1127.11f, IhB = 217.99f;
        const float EnbL = 510.19f, EnbT = 168.16f, EnbR = 563.29f, EnbB = 217.99f;
        const float PnL = 510.19f, PnT = 169.45f, PnR = 752.61f, PnB = 216.69f;      // 这一份的 Player Name 是**紧跟钮**的（修正值）
        const float PnPx = 38.6f, PnAutoMin = 23f;
        const float PtL = 510.19f, PtT = 217.99f, PtR = 1127.10f, PtB = 267.81f;
        const float PtPx = 35f, PtAutoMin = 20f;
        const float PlL = 453.15f, PlT = 259.19f, PlR = 506.27f, PlB = 312.31f;
        const float PltL = 460.01f, PltT = 266.05f, PltR = 499.41f, PltB = 305.45f;
        const float PltPx = 37.2f, PltAutoMin = 18f;
        static readonly Color NameGold = new Color(0.98f, 0.686f, 0.169f, 1f);
        static readonly Color RankInk = new Color(0.961f, 0.914f, 0.737f, 1f);
        /// <summary>🆕 **2026-10-24（E12 · P1-10）**：`icon` 那两格（`Top4` 的卡里 · `FactionScoreSmall` 的行里）
        /// 的**空态**用色 = 全透明 —— 原版那儿是阵营图，我们没有阵营分 ⇒ 不画；⛔ 别再用 `null` 的缺省 tint
        /// （那会落到 `CardArt.Solid()` 的白图 = 用户实拍里那四处白块）。</summary>
        static readonly Color Transparent = new Color(1f, 1f, 1f, 0f);

        // ============================================================ ② `Top4`（四格阵营分 + 中间那一列）
        const float T4L = 351.03f, T4T = 333.95f, T4R = 1186.99f, T4B = 886.97f;
        const float CtL = 367.75f, CtT = 345.01f, CtR = 1170.27f, CtB = 875.91f;      // content（HLG）
        const float LsL = 374.36f, LsR = 584.36f;                                     // left-side（VLG spacing 50）
        const float CsL = 584.36f, CsR = 953.66f;                                     // center
        const float RsL = 953.66f, RsR = 1163.66f;                                    // right-side
        /// <summary>**卡矩形宽** —— 🔴 **判据 = 原版 prefab 字段 + uGUI 算法**（推导全文见文件头 §A·4 那一段的 ②）：
        /// `left-side` / `right-side` 的序列化 `m_SizeDelta.x = 210`（点锚点 ⇒ 宽恒 210）、减去两侧 pad 10
        /// ⇒ 组内宽 190；卡自己的序列化 `m_SizeDelta.x` 也是 190，而卡内三件（`icon` / 两行 rating）
        /// 序列化 `sizeDelta.x = 164`、`m_AnchoredPosition.x = 95` —— **只有卡宽 190 时那个 95 才是
        /// 「164 居中」（13 + 82）**。
        /// ✅ **2026-10-06（A146）已照原版改成 190**：改前是 **164**（原来按「= `icon` 的 `sizeDelta.x`、
        /// 表里 `0.00` 不是真值」推的 —— 那个 164 是**卡内子件的宽**，不是卡的宽）⇒ 卡底比原版窄 26、
        /// 卡内三件整体左了 13（`BuildCard` 里 `r` 与三个子件 x 都吃同一个 `CardW`）。
        /// 现在四处各归各位：卡用 `CardW`，三件用 `CardInnerW` 且左缘 = 卡左 + `CardInnerOff`。</summary>
        const float CardW = 190f, CardH = 230.448f, CardGap = 50f;
        /// <summary>卡**内**三件（`icon` / `Alliance Rating Display` / `MaxRating`）的宽与左缘偏移 ——
        /// 🆕 **2026-10-06（A146）**。判据 = 三件的序列化 `m_SizeDelta.x = 164` · `m_AnchoredPosition.x = 95`
        /// （= 卡内 VLG `align 4`(MiddleCenter) 的 `offsetInCell = (190 − 164)/2 = 13`，加半个子件宽 82）。
        /// ⚠️ 这两个数**不是 `CardW` 的别名**：卡宽变了它们也不该跟着动（原版就是 190 的卡里放 164 的子件）。</summary>
        const float CardInnerW = 164f, CardInnerOff = 13f;
        const float Card1T = 355.01f, Card2T = 635.46f;
        const float CardInL = 374.36f, CardInR = 963.66f;      // 两列的卡 x1（= 列左 + pad 10）
        static readonly Vector4 CardBorder = new Vector4(18f, 18f, 18f, 18f);
        const string ArtSubmenu = "UI_Deck_Information_submenu_Back";
        // 卡内部（VLG align 4 MiddleCenter · spacing 0 · 三件高 145.802 / 41 / 26.7615 ⇒ 上下各留 8.44）
        const float FIconT = 8.44f, FIconH = 145.802f, FRtH = 41f, FMaxH = 26.7615f;
        const string ArtRankIcon = "40k_UI_icon_ranked_Skirmish";
        /// <summary>最高分那一行用的图 —— **与值那一行不同**（§A·2 末）。⚠️ 工程里文件名是 `Menu_Icon_Galon.png`。</summary>
        const string ArtGalonIcon = "Menu_Icon_Galon";
        const float SmallIconW = 40f;                                  // 卡里 `Main Icon` 40×41
        const float SmallIconWBig = 151.82f, SmallTextW = 297.07f;     // `AllFactions` 行里那两件（修正后）
        const float SIconW = 58.6f, STextW = 296.839f;                 // 中间列 `Global Rating` 那两件（修正后）
        /// <summary>分数文字的空态串：**照原版预制体自己在值那一格写的那两个**（`'------'` / `'3000'` 里的前者）。
        /// 原版 `'3000'` / `'4879'` / `'5000'` 都是示例数字 ⇒ **不抄**。</summary>
        public const string ScoreEmpty = "------";

        /// <summary>中间列：`DivisionText`（⚠️ 名字叫段位字，装的是**大标题 `Global Rating`**）、
        /// `DivisionImage`（段位大图 + 名次数字）、`footer`（`MainRating` > `Global Rating`）。</summary>
        const float DtxT = 355.01f, DtxB = 599.34f;                    // `DivisionText`（244.33 高）
        const float DimgT = 599.34f, DimgB = 732.62f;                  // `DivisionImage`（133.28 高）
        const float FtT = 732.62f, FtB = 865.91f;                      // `footer`
        const float MrT = 769.10f, MrB = 829.43f;                      // `MainRating`（**m_Enabled=0**，原版不跑它的布局件）
        const float GrT = 770.59f, GrB = 827.94f;                      // `Global Rating`（HLG）

        // ============================================================ ③ `AllFactions`（右边的阵营排行榜）
        const float AfL = 1302.77f, AfT = 264.81f, AfR = 1746.97f, AfB = 886.97f;
        const float AfHdL = 1302.77f, AfHdT = 200.13f, AfHdR = 1672.07f, AfHdB = 260.13f;   // 'Faction Rating'
        const float AfInfoL = 1686.86f, AfInfoT = 205.89f, AfInfoR = 1737.87f, AfInfoB = 254.37f;
        const string ArtInfo = "40K_generic_bt_info";
        const float AfScL = 1305.29f, AfScT = 288.62f, AfScR = 1746.97f, AfScB = 864.38f;   // scroll rect / viewport
        const float AfCoR = 1735.56f;                                                       // content 右缘
        const float RowH = 117.711f, RowW = 438.74f;
        const float RowIcL = 0f, RowIcT = -10.79f, RowIcW = 143.098f, RowIcH = 128.5f;      // icon（**比行还高、还往上冒**）
        const float RowRtL = -10.15f, RowRtT = -15.30f, RowRtW = 448.89f, RowRtH = 110.91f; // Alliance Rating Display（值）
        const float RowMxT = 50.49f, RowMxH = 72.3365f;                                     // Alliance Rating Display (1)（最高）
        const float AfHdPx = 38f, AfHdAutoMin = 18f;
        const float RowValPx = 90f, RowValAutoMin = 18f, RowMxPx = 76.35f;
        // 🔴 **A406（2026-10-12）：`Individual rating value` 的【上限】与【base】—— 两族不同值，逐颗实读**
        //   （判据命令与逐节点清单 → `RatingRow` 的文档头）：
        //   · **卡那一族**（`#1..#4 FactionScoreBig`）= `m_fontSizeMax **40.0**` · `m_fontSizeBase **31.3799991607666**`；
        //   · **榜单行那一族**（`FactionScoreSmall*`）= `m_fontSizeMax **90.0**` · `m_fontSizeBase` **同一个 31.3799991607666**。
        //   ⛔ **别用 `RowValPx`/`RowMxPx` 顶替上限** —— 那两个是 `m_fontSize`（收敛值），不是上限。
        const float ScoreBigAutoMax = 40f, ScoreSmallAutoMax = 90f, ScoreAutoBase = 31.3799991607666f;
        // 🔴 **A413（2026-10-12）：`m_fontSize`（= 我们那个 `px` 标称）也【两族不同值】，和上面 max/base 同源** ——
        //   逐颗实读（`python 工具/menu_dump.py bundle_menus_assets_all "Player Profile Window" --depth 25 --md`，
        //   本件亲跑；每个节点都用祖先链核过，别只看「字号=」那一列）：
        //   · **卡那一族** `Ranking Tab/Top4/content/{left,right}-side/#1..#4 FactionScoreBig/`
        //     `{Alliance Rating Display, MaxRating}/Individual rating value`
        //     ⇒ **`字号=40.0` / `字号=28.200000762939453`**（8 颗逐颗读过，`#1..#4` 同值）；
        //   · **榜单行那一族** `Ranking Tab/AllFactions/scroll rect/viewport/content/FactionScoreSmall*/`
        //     `{Alliance Rating Display, Alliance Rating Display (1)}/Individual rating value`
        //     ⇒ **`字号=90.0` / `字号=76.3499984741211`**（8 颗逐颗读过）。
        //   ⚠️ **此前 `px` 是两族【共用】的**（`RowValPx`/`RowMxPx`）⇒ **卡那一族的标称一直是错的**（90/76.35）。
        //   ⚠️ 标称只管「自适应**不跑**」那一档的字号：这四站的 `autoFit` 都跑（`px > autoMin 18`，
        //      改后 `40/28.2` 仍 > 18）⇒ **这一改不动今天的画面**，只是把声明改对（判据同 `RatingRow` 的文档头）。
        const float CardValPx = 40f, CardMxPx = 28.2f;

        MenuScroll _scroll;
        Transform _content;
        ImageQuad _avatarArt;
        Label _playerName;
        ImageQuad[] _rowIcons = new ImageQuad[0];

        /// <summary>这一页建出来的阵营行数（自检用；原版是「前 4 名用树上的格子、其余 Instantiate」）。</summary>
        public int BuiltRows { get; private set; }

        /// <summary>🆕 **2026-10-24（A1）**：`center/DivisionImage` 那颗实际挂上的**段位图资源名**
        /// （= `RankedDivisionInfo.DivisionArtName(DemoDivisionNumber)`；建页之前为 `null`）。
        /// 🔴 **自检专用**（`Editor/RankedArtAssert.cs` 拿它和事件窗那颗比，证明**两个窗口同一份判据**）——
        /// ⛔ 生产代码别读它，要图名请走 `RankedDivisionInfo.DivisionArtName`。</summary>
        public string DivisionArtUsed { get; private set; }

        /// <summary>🆕 **2026-10-13（A465 · W-A435己）**：这一页那个**纵向**滚动区（**自检用** ——
        /// 同 `LeaderboardWindow.RowsScroll` / `BattleLogTab.RowsScroll` 那条理由：断言要能读到
        /// `ClipNode` 与 `Viewport` 两态）。⛔ 生产代码不用它。</summary>
        public MenuScroll RowsScroll { get { return _scroll; } }

        protected override void Build()
        {
            BuildTop4();
            BuildAllFactions();
            BuildPlayerInfo();
            RefreshIdentity();
        }

        /// <summary>切到本页时重读「我是谁」（与 Profile 页同一套理由）。</summary>
        public override void OnOpen() { RefreshIdentity(); }

        // ============================================================ ① 玩家信息那一块（照 Profile 页那套）

        void BuildPlayerInfo()
        {
            var info = Node("Profile Player Info", new PxRect(PiL, PiT, PiR, PiB));
            var av = Node(info, "Avatar Item Small", new PxRect(AvL, AvT, AvR, AvB));
            var ic = Node(av, "Image Container", new PxRect(AvIcL, AvIcT, AvIcR, AvIcB));
            // 🔴 **2026-10-24（E18）**：三层各自按原版 `m_LocalScale` **绕布局框中心放大**
            //   （头像 ×2.0 · 框 ×1.25 · 高亮 ×1.9 —— 判据/理由见 `Shell/AvatarTab.cs` 的 `ScaleImage`
            //    常量注释；`Image Container` 本身仍按布局框摆、不放大）。⚠️ 放大后溢出容器（原版也溢出）。
            Rect(ic, "Player_Avatar_selected",
                 AvatarTab.ScaleAboutCenter(new PxRect(AvHlL, AvHlT, AvHlR, AvHlB), AvatarTab.ScaleHighlight),
                 "Highlight", L_Bg2, null, true);
            _avatarArt = CosmeticRect(ic, CurrentAvatarArt(),
                                      AvatarTab.ScaleAboutCenter(new PxRect(AvImL, AvImT, AvImR, AvImB),
                                                                 AvatarTab.ScaleImage),
                                      "Image", L_ArtOverFrame);
            Rect(ic, "Player_Profile_Border",
                 AvatarTab.ScaleAboutCenter(new PxRect(AvBdL, AvBdT, AvBdR, AvBdB), AvatarTab.ScaleBorder),
                 "Border", L_Frame, null, true);
            // 🔴 **2026-10-07（A62 子表 A · A39 / 判据 §③「碰巧对」）**：原版 `Avatar Name` 是 **`折行=1`**，
            //   我们原来没显式声明（靠 `SetAutoFitBox` 顺带打开）⇒ 补 `wrap: true` 钉死。
            // 🔴 **A406**：`Ranking Tab/Profile Player Info/Avatar Item Small/Avatar Name`
            //   原版 `auto[12.0~36.0] 基准=36.0` ⇒ 上限 36（= 标称）· base 36.0。判据 =
            //   `python 工具/menu_dump.py bundle_menus_assets_all "Player Profile Window" --depth 25 --md`。
            // 🔴 **A1024③（2026-10-17）换面**：本页 `Player Name` / `Player Title` / `DivisionText`（含
            //    `'Global Rating'` 那颗大标题）/ `Faction Ranking Points` 的 `m_fontAsset` = **`Asar-Regular SDF`**；
            //    `Avatar Name`（`Profile Player Info ▸ Avatar Item Small`）/ `Player Level Text` /
            //    `Individual rating value` 全是 **Pragati**。判据 = `menu_dump … "Player Profile Window" --depth 30`。
            var an = Text(av, "", new PxRect(AvL, AvB, AvR, AvNmB), Color.white, "Avatar Name", 36f, L_Text2,
                          autoFit: true, autoMinPx: 12f, alignLeft: false, wrap: true,
                          autoMaxPx: 36f, basePx: 36f, face: TmpFont.Face.LatinCardBody);
            if (an != null) an.gameObject.SetActive(false);          // 出厂 F（原版只 set_text、从不 SetActive）
            Hit(info, "AvatarHit", new PxRect(341.16f, 128.01f, 520.06f, 342.45f), L_Hit, OnAvatarClick);

            // 两份 `Info Section` 互斥（出厂 = with-OFF / without-ON）；`Edit Name Button` 两份**都 F** ——
            // 原版由「是不是本人」在运行期点亮（§A·1）。**我们看的永远是自己的档案 ⇒ 按运行期那份：显示**。
            var wa = Node(info, "Info Section with Alliance", new PxRect(InsL, InsT, InsR, InsB));
            wa.gameObject.SetActive(false);
            var na = Node(info, "Info Section without Alliance", new PxRect(InsL, InsT, InsR, InsB));
            var nh = Node(Node(na, "Name and Title Holder", new PxRect(IhL, IhT, IhR, IhB)),
                          "NameHolder", new PxRect(IhL, IhT, IhR, IhB));
            var enb = Node(nh, "Edit Name Button", new PxRect(EnbL, EnbT, EnbR, EnbB));
            Nine(enb, "40k_menu_bt_general_bg", new PxRect(EnbL, EnbT, EnbR, EnbB),
                 new Vector4(15f, 15f, 15f, 15f), "Image", L_Frame, new Color(0.212f, 0.0941f, 0.098f, 1f));
            Nine(enb, "40k_menu_bt__general_outline", new PxRect(510.69f, EnbT, 562.79f, EnbB),
                 new Vector4(15f, 15f, 15f, 15f), "Button Outline", L_Frame + 1,
                 new Color(0.945f, 0.842f, 0.0314f, 1f), fillCenter: false);
            Rect(enb, "40k_general_bt_yellow_edit", new PxRect(EnbL, EnbT, EnbR, EnbB), "Icon", L_Text, null, true);
            Hit(nh, "EditNameHit", new PxRect(EnbL, EnbT, EnbR, EnbB), L_Hit, OnEditName);
            // 🔴 **2026-10-07（A62 子表 A · A40/A41）**：原版 `Ranking Tab > Profile Player Info > …`
            //   的 `Player Name` / `Player Title` 都是 **`折行=0`**（判据 = `md "Player Profile Window" --depth 25 --md`）
            //   ⇒ `SetAutoFitBox` 之后显式关掉；关完重排（A205）⇒ 左对齐按新宽度再算一次。
            // 🔴 **A406**：`Ranking Tab/Profile Player Info/Info Section without Alliance/…/NameHolder/Player Name`
            //   原版 `auto[23.0~**45.0**] 基准=35.0` ⇒ 上限 **45**（我们原来拿 38.6 当上限）· base 35.0。
            var rkNameR = new PxRect(PnL, PnT, PnR, PnB);
            _playerName = Text(nh, ProfileData.PlayerName, rkNameR, NameGold, "Player Name",
                               PnPx, L_Text, autoFit: true, autoMinPx: PnAutoMin, alignLeft: true, wrap: false,
                               autoMaxPx: 45f, basePx: 35f, face: TmpFont.Face.LatinUi);
            if (_playerName != null) { _playerName.SetWrapping(false); MenuDraw.AlignLeft(_playerName, rkNameR); }
            // 🔴 **A406**：同树 `Player Title` 原版 `auto[20.0~35.0] 基准=35.0` ⇒ 上限 35 · base 35.0。
            var rkTitleR = new PxRect(PtL, PtT, PtR, PtB);
            var rkTitleLb = Text(na, "Player Title", rkTitleR, Color.white, "Player Title",
                                 PtPx, L_Text2, autoFit: true, autoMinPx: PtAutoMin, alignLeft: true, wrap: false,
                                 autoMaxPx: 35f, basePx: 35f, face: TmpFont.Face.LatinUi);
            if (rkTitleLb != null) { rkTitleLb.SetWrapping(false); MenuDraw.AlignLeft(rkTitleLb, rkTitleR); }

            var lv = Node(info, "Player Level", new PxRect(PlL, PlT, PlR, PlB));
            Rect(lv, "UI_Button_Round_background", new PxRect(PlL, PlT, PlR, PlB), "Image", L_Art);
            // 🔴 **A406**：`Ranking Tab/Profile Player Info/Player Level/Player Level Text`
            //   原版 `auto[18.0~37.2] 基准=36.88999938964844` ⇒ 上限 37.2（= 标称）· base 36.89。
            // 🆕 **2026-10-26（批次 4 · F7 · 任务 ③）**：值从 `"-"` 改成 **`CardTierConfig.PlayerLevelText`**
            //   —— 与顶栏 / 档案窗**同一个口**（⛔ 别在这里各算一份）。原版这一格也是
            //   `PlayerProfileUIController.SetPlayerLevel` 写的（判据 → `Shell/TopBar.cs` 那一大段）。
            _levelLabel = Text(lv, CardTierConfig.PlayerLevelText, new PxRect(PltL, PltT, PltR, PltB), Color.white, "Player Level Text",
                 PltPx, L_Text2, autoFit: true, autoMinPx: PltAutoMin, alignLeft: false, wrap: true,
                 autoMaxPx: 37.2f, basePx: 36.88999938964844f, face: TmpFont.Face.LatinCardBody);
        }

        // ============================================================ ② `Top4`

        void BuildTop4()
        {
            var t4 = Node("Top4", new PxRect(T4L, T4T, T4R, T4B));
            BuildTop4Content(t4);
        }

        /// <summary>`Top4` 那一棵的**内容**（`bg` → `content` → 四格 + 中间列）。
        /// 🆕 **2026-10-24（`U2` V2）**：从 `BuildTop4` 拆出来 —— 数据变了要能**整棵重建**
        /// （见 <see cref="RebuildTop4"/>），而重建时 `Top4` 这个根节点本身要留着（宿主断言量它）。</summary>
        void BuildTop4Content(Transform t4)
        {
            Nine(t4, ArtSubmenu, new PxRect(T4L, T4T, T4R, T4B), CardBorder, "bg", L_Bg);
            var ct = Node(t4, "content", new PxRect(CtL, CtT, CtR, CtB));

            // 左列两格（#1 / #2）· 中间列 · 右列两格（#3 / #4）
            // 🔴 **V2**：每格带自己的数据（`EntryOf(名次)`）；`null` ⇒ **整格不画**（`BuildCard` 里关掉）
            BuildCard(ct, "left-side", "#1", LsL, LsR, Card1T, EntryOf(1));
            BuildCard(ct, "left-side", "#2", LsL, LsR, Card2T, EntryOf(2));
            BuildCenter(ct);
            BuildCard(ct, "right-side", "#3", RsL, RsR, Card1T, EntryOf(3));
            BuildCard(ct, "right-side", "#4", RsL, RsR, Card2T, EntryOf(4));

            if (!HasTop4Data)
                Debug.Log("[Ranked] `Top4`：**阵营分是服务器的**（`LeaderboardManager.GetLeaderboard`，§C·2）"
                        + "⇒ 四格**整格不画**（`SetActive(false)`；⛔ 不留空框、不印破折号）—— 数据一到就往 "
                        + "`RankedTab.Top4Data` 里填，绘制那一半不用改。"
                        + "⚠️ `top4Factions` 的**数组顺序是 `#1,#3,#2,#4`**（不是树序，§A·6）。");
            else
                Debug.Log("[Ranked] `Top4`：喂了 " + Top4Data.Length + " 条数据 ⇒ 四格照原版画（图 + 大分 + 最高分）"
                        + "；⚠️ 数据是我们灌进 `RankedTab.Top4Data` 的（原版读服务器）。");
        }

        /// <summary>🆕 **2026-10-24（`U2` V2）**：清掉 `Top4` 的子件再重建（数据变了调它；自检也用它
        /// 把「有数据 / 无数据」两支都跑一遍）。⚠️ `Top4` 这个**根节点**不动 —— 它的矩形是原版真值。
        /// ⚠️ 与 `RebuildRows` 同一条纪律：**先清再建**（不清会一层层叠上去）。</summary>
        public void RebuildTop4()
        {
            Transform t4 = null;
            for (int i = 0; i < transform.childCount; i++)
                if (transform.GetChild(i).name == "Top4") { t4 = transform.GetChild(i); break; }
            if (t4 == null)
            {
                Debug.LogWarning("[Ranked] `RebuildTop4`：找不到 `Top4` 节点（页面还没建？）⇒ 什么都没做");
                return;
            }
            for (int i = t4.childCount - 1; i >= 0; i--) DestroyNow(t4.GetChild(i).gameObject);
            BuildTop4Content(t4);
        }

        /// <summary>一列（`left-side` / `right-side`）。列是 VLG（spacing 50 · pad 10 · `ctrlW/ctrlH=1`）。
        /// <para>🔴 **2026-10-24（`U2` V2）**：`e == null`（服务器没这条数据）⇒ 结构照建、**整格关掉**
        /// （`SetActive(false)`）—— 原版那四格画的是**四名玩家的大分**，我们没数据时**不该留空框**。
        /// ⚠️ 关的是**卡**（`#N FactionScoreBig`）不是列（`left-side` / `right-side`）——
        /// 列里还有别的格（`#2` / `#4`），关列会把有数据的那格也关掉。</para></summary>
        void BuildCard(Transform parent, string colName, string label, float colL, float colR, float top,
                       Top4Entry? e)
        {
            var col = parent.Find(colName);
            if (col == null) col = Node(parent, colName, new PxRect(colL, CtT, colR, CtB));
            var r = new PxRect(colL + 10f, top, colL + 10f + CardW, top + CardH);
            var card = Node(col, label + " FactionScoreBig", r);
            Nine(card, ArtSubmenu, r, CardBorder, "Image", L_Bg);      // 卡底（出厂 m_Enabled=0 只在 `Edit Name Button` 上，这里正常）
            // 三件：icon（`CardInnerW`×145.802）· Alliance Rating Display（164×41）· MaxRating（164×26.7615）
            // 横向：三件**都不是卡宽**（164 ≠ 190）—— 卡内 VLG `align 4`(MiddleCenter) ⇒ 左右各留 `(190−164)/2 = 13`
            //   （判据 = 三件的序列化 `m_SizeDelta.x = 164` / `m_AnchoredPosition.x = 95 = 13 + 82`）。
            // 纵向：VLG align 4（MiddleCenter）⇒ 上下各留 8.44（已核：icon 顶 = 卡顶 + 8.44 ✓）
            var ir = new PxRect(r.x1 + CardInnerOff, r.y1 + FIconT,
                                r.x1 + CardInnerOff + CardInnerW, r.y1 + FIconT + FIconH);
            // 🔴 **2026-10-24（E12 · P1-10）就地订正（铁律 5）**：这里原来只传 `art = null` ⇒
            //   `ProfilePage.Rect(art: null)` 会落到 `CardArt.Solid()`（**纯白**）+ 默认白 ⇒ 四张卡上各挂一块
            //   **白矩形**（用户实拍 `_tmp_view/menu/14_档案窗_排名页.png` 那四处白块就是它；
            //   `ProfileTab` 自己那份 `CurrentAvatarArt` 注释里点过同一个坑）。
            //   ⇒ 现在**tint 全透明**（`a = 0`）：图上什么都不画（空态没有阵营分 = 没有阵营图可画），
            //   但**节点保留 `ImageQuad`** —— ⚠️ 这条是有意的，不是偷懒：
            //   `MenuCheck.NodeRectPx` 对「既无 `Label` 也无 `ImageQuad`」的节点**返回 false**
            //   （`Editor/MenuCheck.cs` 那句 `var q = t.GetComponentInChildren<ImageQuad>(); if (q == null) return false;`）
            //   而 `Editor/MainMenuScene.cs:7687` / `:7751` 那两条宿主断言正是拿 `icon` 节点量矩形
            //   ⇒ 删成裸节点会让那两条**与画面无关地变红**。⛔ 别改成 `Node(...)`。
            // 🆕 **2026-10-29（`B97_W13`）订正**：徽记图**不再在这里画** —— 原版那颗 `icon` 是 `FactionScore.factionImage`，
            //   由 `FactionScore.Initialize` 按 `CardArmy` 取图写（判据 `FactionScore__Initialize.c:17-19`）；本方法只建**空态占位**。
            //   ⛔ 只建一个 `icon` 节点（同名两颗会让宿主断言量错一颗；E12 那一段的理由仍然成立）。
            //   ⚠️ 所以下面这格建的是**空态占位**：`Rect(card, null, …)` = `CardArt.Solid()` + **全透明 tint**（同 `BuildRow.FactionIconQuad`）。
            //   有数据时 `FactionScore.Initialize` 会把 `factionImage` 换成真徽记并**转回不透明**（`Shell/FactionScore.cs` 的 `SetTint(Color.white)`）。
            var icon = Rect(card, null, ir, "icon", L_Bg2, Transparent);   // 空态：节点在、画面无（贴图由 `fs.InitializeOrEmpty` 写）
            // 原版这四格**本身就是一颗 `FactionScore`**（`RankedTab.top4Factions` 数组 ⇒ `FactionScore.Initialize(army, cur, high)`）；`Big` 的第二颗评级叫 `MaxRating`（⛔ 不是 `… (1)`）。
            var fs = card.gameObject.AddComponent<CardPresentation.ScoreBars.FactionScore>();
            // ⚠️ `Bind` 必须排在这两颗 `RatingRow` **之后**（`ArdOf` 按节点名找 `Alliance Rating Display` / `MaxRating`）；无数据（`e` 为 null）时不写 —— 原版那时整卡 `SetActive(false)`，见末尾。
            RatingRow(card, "Alliance Rating Display", ArtRankIcon,
                      new PxRect(r.x1 + CardInnerOff, r.y1 + FIconT + FIconH,
                                 r.x1 + CardInnerOff + CardInnerW, r.y1 + FIconT + FIconH + FRtH),
                      SmallIconW, CardValPx, RowValAutoMin, ScoreBigAutoMax, ScoreAutoBase, center: true,
                      value: e.HasValue ? e.Value.Rating : null);   // A413/A1537：标称 40（原版 `字号=40.0`；align 4 居中）
            RatingRow(card, "MaxRating", ArtGalonIcon,
                      new PxRect(r.x1 + CardInnerOff, r.y1 + FIconT + FIconH + FRtH,
                                 r.x1 + CardInnerOff + CardInnerW,
                                 r.y1 + FIconT + FIconH + FRtH + FMaxH),
                      SmallIconW, CardMxPx, RowValAutoMin, ScoreBigAutoMax, ScoreAutoBase, center: true,
                      value: e.HasValue ? e.Value.MaxRating : null);    // A413/A1537：标称 28.2（原版 `字号=28.200000762939453`；align 4 居中）
            fs.Bind(icon, ArdOf(card, "Alliance Rating Display"), ArdOf(card, "MaxRating"));
            if (e.HasValue)
                fs.InitializeOrEmpty(FactionOfSprite(e.Value.FactionArt), ScoreOf(e.Value.Rating),
                                     ScoreOf(e.Value.MaxRating), ScoreEmpty);   // 三句 = 原版 `FactionScore.Initialize` 的唯一实现
            // 🔴 **2026-10-24（`U2` V2）**：**无数据 ⇒ 整格不画**（`SetActive(false)`；⛔ 不留空框）；关在**最后**（上面结构照建）。
            if (!e.HasValue) card.gameObject.SetActive(false);
        }

        /// <summary>一个 `AllianceRatingDisplay` 行（HLG，spacing 0）。原版三件：`Secondary Icon`（**出厂 F** ⇒
        /// 不参与布局）、`Main Icon`、`Individual rating value`。§A·4 的修正：x **不减** `Secondary Icon` 的宽。
        /// 🔴 **2026-10-17（`A1537`）**：原版对齐是**实例级 prefab 字段**，两族不同 ——
        /// `#N FactionScoreBig` / `Global Rating` = **align 4 (MiddleCenter)**；`FactionScoreSmall` = **align 3 (MiddleLeft)**
        /// ⇒ 本方法加 `center` 形参，**逐个调用点表态**（⛔ 别做「统一」）。分数恒空 ⇒ 可见差别在图标位置。
        /// ⚠️ `iconW` **逐处不同**（卡里 40 · 排行榜行里 151.82 · 中间列 58.6）—— 别用一个常数套所有。
        /// <para>🔴 **A406（2026-10-12）：`autoMax` / `basePx` 逐处实读，而且【两族不同值】** ——
        /// 判据 = `python 工具/menu_dump.py bundle_menus_assets_all "Player Profile Window" --depth 25 --md`
        /// 的 `auto[…]` / `基准=` 两列：
        /// · `Top4/content/{left,right}-side/#N FactionScoreBig/{Alliance Rating Display,MaxRating}/Individual rating value`
        ///   ⇒ `auto[18.0~**40.0**] 基准=31.3799991607666`（4 格 ×2 行 = 8 颗，逐颗读过）；
        /// · `AllFactions/scroll rect/viewport/content/FactionScoreSmall*/{Alliance Rating Display,Alliance Rating Display (1)}/Individual rating value`
        ///   ⇒ `auto[18.0~**90.0**] 基准=31.3799991607666`（4 行 ×2 行 = 8 颗，逐颗读过）。
        /// ⚠️ 两族的 `m_fontSize` 也不同（卡 40 / 28.2 · 行 90 / 76.35）—— 本函数原来的 `px` 参数**两族共用**
        /// （`RowValPx`/`RowMxPx`），所以**卡那一族的 `px` 本身就不对**；A406 只补了 `max`/`base`
        /// （`px` 只在自适应不跑时才决定字号）—— 🔴 **`px` 那一笔已由 A413（2026-10-12）收掉**：
        /// 卡那一族改用新常量 `CardValPx = 40f` / `CardMxPx = 28.2f`，`RowValPx`/`RowMxPx` 只留给榜单行那一族。</para></summary>
        void RatingRow(Transform parent, string name, string iconArt, PxRect r, float iconW, float px, float autoMin,
                       float autoMax, float basePx, bool center = false, string value = null)
        {
            var row = Node(parent, name, r);
            var si = Rect(row, ArtRankIcon, new PxRect(r.x1, r.y1, r.x1, r.y1), "Secondary Icon", L_Art, null, true);
            if (si != null) si.gameObject.SetActive(false);             // 出厂 F
            float iw = Mathf.Min(iconW, r.W);
            // 🔴 **2026-10-17（`A1537`）**：原版两族的 HLG 对齐**不同**（这是**实例级 prefab 字段**，
            //   不是「同一个组件同一条判据」——⛔ 别做「统一」）：
            //     · `#N FactionScoreBig` / `Global Rating` = **align 4 (MiddleCenter)**
            //     · `FactionScoreSmall`                     = **align 3 (MiddleLeft)**
            //   （判据 = `menu_dump.py bundle_menus_assets_all "#1 FactionScoreBig"` 与 `"FactionScoreSmall"` 现读）。
            //   ⚠️ 我们的分数**恒为空**（服务器数据）⇒ 可见差别只在**图标位置**：居中档把图标摆到行心。
            float ix = center ? r.x1 + (r.W - iw) * 0.5f : r.x1;
            Rect(row, iconArt, new PxRect(ix, r.y1, ix + iw, r.y2), "Main Icon", L_Art, null, true);
            // 🔴 **2026-10-24（`U2` V2）**：值那一格改为**可喂**（`value`）—— 有数据时印真分数、
            //   没有才退回原版自己的空态串 `------`（⛔ 别把 `------` 当成「有数据」画上去）。
            Text(row, string.IsNullOrEmpty(value) ? ScoreEmpty : value, new PxRect(ix + iw, r.y1, r.x2, r.y2),
                 Color.white, "Individual rating value",
                 px, L_Text, autoFit: true, autoMinPx: autoMin, alignLeft: !center, wrap: true,
                 autoMaxPx: autoMax, basePx: basePx, face: TmpFont.Face.LatinCardBody);
        }

        /// <summary>中间那一列：大标题 `Global Rating` → 段位大图 → `footer`（`MainRating` > `Global Rating`）。</summary>
        void BuildCenter(Transform parent)
        {
            var c = Node(parent, "center", new PxRect(CsL, CtT, CsR, CtB));
            // 🔴 **A62 · A44**：原版 `Top4 > center > … > DivisionText`（画 `'Global Rating'`）是 **`折行=0 auto[18~38]`** ⇒ 关掉。
            // 🔴 **A406**：`Ranking Tab/Top4/content/center/DivisionText`（画 `'Global Rating'`）
            //   原版 `auto[18.0~38.0] 基准=23.0` ⇒ 上限 38（= 标称）· base **23.0**。
            var grTitle = Text(c, "Global Rating", new PxRect(CsL, DtxT, CsR, DtxB), RankInk, "DivisionText",
                               38f, L_Text, autoFit: true, autoMinPx: 18f, alignLeft: false, wrap: false,
                               autoMaxPx: 38f, basePx: 23f, face: TmpFont.Face.LatinUi);           // ⚠️ 名字叫 DivisionText，**装的是大标题**
            if (grTitle != null) grTitle.SetWrapping(false);
            // `DivisionImage`（段位大图）+ `RankImage`（名次数字）
            // 🔴 **2026-10-24（A1）**：改前**不画**（理由写在文件头 ③ 的旧文里，已订正）。
            //   现在**照事件窗（`RankedDivisionInfo`）同一份判据**画 —— 号 = `RankedDivisionInfo.DemoDivisionNumber`、
            //   号→图 = `RankedDivisionInfo.DivisionArtName` ⇒ 两个窗口不可能画出不同的段位。
            //   判据/逐行证据表 → `RankedDivisionInfo.DivisionArt` 的 doc ＋ `资料/普查产出_1024c/A1_段位图案映射.md`。
            //
            //   几何**照原版 prefab 字段**（`menu_dump.py bundle_menus_assets_all "Player Profile Window" --depth 25 --md`
            //   的 `Ranking Tab/Top4/content/center/DivisionImage` 那一行）：
            //   `m_SizeDelta = 369.298 × 133.282` · **`m_PreserveAspect = 1`** · **`m_Type = 0`(Simple)** ·
            //   占位图 = `04-Admiral`（512²）⇒ 512² 的图**按比例内接成 133.28²、水平居中**
            //   （⛔ 别拉满 369.30 宽：那是一颗被 PA 内接过的方图，不是横图）。
            //   队列 = 本页 `L_Art`(2)：`center` 这一列里 `DivisionText`(599.34 之上) 与 `footer`(732.62 之下)
            //   都**不与它重叠**（原版兄弟序 `DivisionText → DivisionImage → footer`，画序在这里无关）。
            var divImgR = new PxRect(CsL, DimgT, CsR, DimgB);
            var di = Node(c, "DivisionImage", divImgR);
            DivisionArtUsed = RankedDivisionInfo.DivisionArtName(RankedData.DisplayDivisionNumber);
            if (string.IsNullOrEmpty(DivisionArtUsed))
            {
                Debug.LogWarning("[Ranked] 排名页的段位大图**没画** —— `RankedDivisionInfo.DivisionArt` 里没有段位 "
                                 + RankedData.DisplayDivisionNumber + " 对应的图；⛔ 不拿别的段位顶替");
            }
            else if (Rect(di, DivisionArtUsed, divImgR, "Image", L_Art, null, true) == null)
            {
                Debug.LogWarning("[Ranked] 排名页的段位大图**没画** —— 图 `" + DivisionArtUsed
                                 + "` 取不到（`Resources/Art/ui_menu/` 里没有，或 `ArtInternal` 记进了 `MissingArt`）");
            }
            var ri = Node(di, "RankImage", new PxRect(732.08f, 639.33f, 805.94f, 652.66f));
            ri.gameObject.SetActive(false);

            var ft = Node(c, "footer", new PxRect(CsL, FtT, CsR, FtB));
            // `MainRating`：**`m_Enabled=0`**（原版连它的 HLG 一起关掉 ⇒ 那个布局件不跑）。我们照样建节点、不画底
            var mr = Node(ft, "MainRating", new PxRect(591.29f, MrT, 946.73f, MrB));
            var gr = Node(mr, "Global Rating", new PxRect(591.29f, GrT, 946.73f, GrB));
            var si = Rect(gr, ArtRankIcon, new PxRect(591.29f, GrB, 591.29f, GrB), "Secondary Icon", L_Art, null, true);
            if (si != null) si.gameObject.SetActive(false);
            // 🔴 **2026-10-17（`A1537`）**：原版这颗 `Global Rating` 的 HLG 是 **align 4 (MiddleCenter)**
            //   （判据 = `menu_dump … "#1 FactionScoreBig"` 现读；与 `FactionScoreSmall` 的 align 3 是两回事）。
            //   分数为空 ⇒ 可见差别 = 图标位置：摆到行心（591.29 + (355.44 − SIconW)/2）。
            float gx = 591.29f + (946.73f - 591.29f - SIconW) * 0.5f;
            Rect(gr, ArtRankIcon, new PxRect(gx, GrT, gx + SIconW, GrB), "Main Icon", L_Art, null, true);
            // 🔴 **A406**：`Ranking Tab/Top4/content/center/footer/MainRating/Global Rating/Individual rating value`
            //   原版 `auto[18.0~40.0] 基准=31.3799991607666` ⇒ 上限 40（= 标称）· base 31.38。
            Text(gr, ScoreEmpty, new PxRect(gx + SIconW, GrT, 946.73f, GrB), Color.white,
                 "Individual rating value", 40f, L_Text, autoFit: true, autoMinPx: 18f, alignLeft: false, wrap: true,
                 autoMaxPx: 40f, basePx: ScoreAutoBase, face: TmpFont.Face.LatinCardBody);
        }

        // ============================================================ ③ `AllFactions`

        void BuildAllFactions()
        {
            var af = Node("AllFactions", new PxRect(AfL, AfT, AfR, AfB));
            // 🔴 **A62 · A46**：原版 `Ranking Tab > AllFactions > Faction Ranking Points`（`'Faction Rating'`）
            //   是 **`折行=0 auto[18~38]`** ⇒ 关掉；关完重排（A205）⇒ 左对齐按新宽度再算一次。
            // 🔴 **A406**：`Ranking Tab/AllFactions/Faction Ranking Points`（`'Faction Rating'`）
            //   原版 `auto[18.0~38.0] 基准=23.0` ⇒ 上限 38（= 标称）· base **23.0**（与 `center/DivisionText` 同值，
            //   但**两处各读一次**）。
            var afHdR = new PxRect(AfHdL, AfHdT, AfHdR, AfHdB);
            var afHd = Text(af, "Faction Rating", afHdR, RankInk, "Faction Ranking Points",
                            AfHdPx, L_Text, autoFit: true, autoMinPx: AfHdAutoMin, alignLeft: true, wrap: false,
                            autoMaxPx: 38f, basePx: 23f, face: TmpFont.Face.LatinUi);
            if (afHd != null) { afHd.SetWrapping(false); MenuDraw.AlignLeft(afHd, afHdR); }
            var info = Node(af, "info", new PxRect(AfInfoL, AfInfoT, AfInfoR, AfInfoB));
            Rect(info, ArtInfo, new PxRect(AfInfoL, AfInfoT, AfInfoR, AfInfoB), "Image", L_Art, null, true);
            Nine(af, ArtSubmenu, new PxRect(AfL, AfT, AfR, AfB), CardBorder, "bg", L_Bg);

            var scR = new PxRect(AfScL, AfScT, AfScR, AfScB);
            var sc = Node(af, "scroll rect", scR);
            // 🔴 **2026-10-13（A435 阶段 2 · 丙）**：裁切状态**长在这个视口节点上**（= 原版 `Viewport` 那个 `RectMask2D`）。
            //    参数 = 原版 `Ranking Tab/AllFactions/scroll rect/viewport` 实读的
            //    `m_Padding = (0,0,0,0)` · `m_Softness = (0,0)`（硬边）—— 与迁移前的 `Clip = scR`（软边恒 0）逐位同值。
            var vpVc = ViewportClip.Hang(sc, "viewport", scR, Vector4.zero, Vector2Int.zero);
            var vp = vpVc.transform;
            _scroll = NewScroll(scR, 0f, 0f, true, 10f);   // 原版 `Ranking Tab/AllFactions/scroll rect` = 10（D_N29）
            // 🔴 **2026-10-13（A465 · W-A435己）**：构建循环那一行（`BuildRows` 里
            //   `if (!_scroll.Intersects(rr)) continue;`）从今天起读**同一颗节点**的状态
            //   （`MenuScroll.Intersects` 走 `ClipNode.State.RenderClip`）—— 就是上面那颗 `viewport`。
            //   ⚠️ 今天两值同（节点框 = `scR`、`padding` 全 0）⇒ **逐个位不变**。
            _scroll.ClipNode = vpVc;
            _scroll.Owner = Root.gameObject;
            _scroll.OnChanged = RebuildRows;            // 🔴 滚轮要能重画（同其它页那条教训）
            _content = Node(vp, "content", new PxRect(AfScL, AfScT, AfCoR, AfScT));

            BuildRows();
        }

        /// <summary>排行行。原版：**前 4 名复用 `#1..#4 FactionScoreBig`（在 `Top4` 里），第 5 名起
        /// `Instantiate(factionScorePrefab)` 到 `factionListHolder`**（§C·2）。我们**没有阵营分** ⇒
        /// 这一栏按 `CampaignData.Armies` 建 **13 行**、**分数留空**（B92-N4 2026-10-28 改为 13 行，见类尾）。
        /// 🔴 **2026-10-13（A435 阶段 2 · 丙）**：原来这里重设了一对 `Clip`/`Clip = null` —— **删掉了**
        /// （裁切状态已迁到 `viewport` 节点上，重画时自动继续生效）。</summary>
        public void RebuildRows()
        {
            if (_content == null) return;
            for (int i = _content.childCount - 1; i >= 0; i--) DestroyNow(_content.GetChild(i).gameObject);
            BuildRows();
        }

        void BuildRows()
        {
            string[] armies = RankedFactionArmies();   // 13 个阵营（原版 = 服务器 per-army 排行，判据见类尾）
            int n = armies != null ? armies.Length : 0;   // 原版按服务器返回的阵营条数逐行 Instantiate
            BuiltRows = 0;
            _rowIcons = new ImageQuad[n];
            _scroll.ContentX1 = AfScT;
            _scroll.ContentX2 = AfScT + n * RowH;
            for (int i = 0; i < n; i++)
            {
                float top = AfScT + i * RowH;
                var rr = _scroll.Shift(new PxRect(AfScL, top, AfScL + RowW, top + RowH));
                if (!_scroll.Intersects(rr)) continue;
                BuildRow(rr, i, armies[i]);
                BuiltRows++;
            }
            Debug.Log("[Ranked] `AllFactions`：**各阵营分数是服务器的**（§C·2）⇒ " + n + " 行都建、**分数留空**；"
                    + "阵营徽记按 `CampaignData.Armies[i]` 画（`ArmyIcons.IconName` ⇒ 原版 `ArmyUtilities.GetArmyIcon`）。");
        }

        /// <summary>一行 `FactionScoreSmall`（`Image + FactionScore`）。🆕 2026-10-29（`B97_W2`）：三件（徽记 + 两颗评级）的**写入**收口到 `FactionScore.Initialize`（原版这一行就是一颗挂着该组件的 `FactionScoreSmall` prefab 实例）。</summary>
        void BuildRow(PxRect r, int i, string army)
        {
            var row = Node(_content, i == 0 ? "FactionScoreSmall" : "FactionScoreSmall (" + i + ")", r);
            var ic = new PxRect(r.x1 + RowIcL, r.y1 + RowIcT, r.x1 + RowIcL + RowIcW, r.y1 + RowIcT + RowIcH);
            // 🔴 **2026-10-24（E12 · P1-10）**：`icon` 的 tint 必须**全透明**（原来 `art = null` ⇒ `CardArt.Solid()` **纯白**
            //   ⇒ 这一栏每行各挂一块白矩形）。画面无、节点与 `ImageQuad` 都留着（宿主 `Editor/MainMenuScene.cs:7751` 要拿它量矩形）。
            _rowIcons[i] = FactionIconQuad(row, ic);   // 空态节点（贴图由 `FactionScore.Initialize` 第 1 句写，⛔ 不在这里画第二份）
            RatingRow(row, "Alliance Rating Display", ArtRankIcon,
                      new PxRect(r.x1 + RowRtL, r.y1 + RowRtT, r.x1 + RowRtL + RowRtW, r.y1 + RowRtT + RowRtH),
                      SmallIconWBig, RowValPx, RowValAutoMin, ScoreSmallAutoMax, ScoreAutoBase, false, null);
            RatingRow(row, "Alliance Rating Display (1)", ArtGalonIcon,
                      new PxRect(r.x1 + RowRtL, r.y1 + RowMxT, r.x1 + RowRtL + RowRtW, r.y1 + RowMxT + RowMxH),
                      SmallIconWBig, RowMxPx, RowValAutoMin, ScoreSmallAutoMax, ScoreAutoBase, false, null);
            var fs = row.gameObject.AddComponent<CardPresentation.ScoreBars.FactionScore>();   // 原版这一行 = 一颗挂着 `FactionScore` 的 `FactionScoreSmall`
            fs.Bind(_rowIcons[i], ArdOf(row, "Alliance Rating Display"), ArdOf(row, "Alliance Rating Display (1)"));
            fs.InitializeOrEmpty(FactionOf(army), FactionScoreValue(i, false), FactionScoreValue(i, true), ScoreEmpty);   // 三句 = 原版 `FactionScore.Initialize` 的唯一实现
        }

        // ============================================================ 交互 / 数据

        /// <summary>当前该显示哪张头像 —— 🔴 **读的是全工程唯一那一份**（`ProfileData.AvatarArt`）。
        /// 原来要绕到 `Avatar` 页那个实例上取（那扇页没建出来就取不到）。判据 → `ProfileTab.CurrentAvatarArt`。</summary>
        string CurrentAvatarArt()
        {
            return ProfileData.AvatarArt;
        }

        /// <summary>刷「我是谁」（名字 + 头像）。</summary>
        public void RefreshIdentity()
        {
            if (_playerName != null) _playerName.SetText(ProfileData.PlayerName);
            // 🆕 2026-10-26（F7 · 任务 ④）：等级那一格也刷 —— 原版是 `PlayerProfileUIController.OnCollectorLevelUpdate`
            //   那条升级信号驱动的（`PlayerProfileUIController__OnCollectorLevelUpdate.c`）；我们没信号层 ⇒
            //   **每次开页（`OnOpen`）重读同一个口**（`CardTierConfig.PlayerLevelText`），与顶栏/档案窗一致。
            if (_levelLabel != null) _levelLabel.SetText(CardTierConfig.PlayerLevelText);
            string art = CurrentAvatarArt();
            if (_avatarArt != null)
            {
                var tex = string.IsNullOrEmpty(art) ? null : CardArt.Cosmetics(art);
                if (tex != null) { _avatarArt.SetTexture(tex); _avatarArt.gameObject.SetActive(true); }
                else
                {
                    _avatarArt.gameObject.SetActive(false);
                    Debug.LogWarning("[Profile] `Ranking` 页的头像立绘取不到（" + (art ?? "清单为空") + "）—— 那一层不画");
                }
            }
        }

        /// <summary>点头像：原版这里是 **`ChangeTab&lt;AvatarTab&gt;()`**（`&lt;Initialize>b__10_0`，§C·1 第 4 条）
        /// —— **点本页头像会跳到 Avatar 页**。我们照做（走宿主窗的 `ChangeTab`）。</summary>
        void OnAvatarClick()
        {
            if (Win == null || Win.tabButtons == null) return;
            for (int i = 0; i < PlayerProfileWindow.Tabs.Length; i++)
                if (PlayerProfileWindow.Tabs[i].Node == "Avatar Button") { Win.tabButtons.Click(i); break; }
            Debug.Log("[Profile] 点 `Ranking` 页的头像 ⇒ 切到 `Avatar` 页（原版 `RankedTab.<Initialize>b__10_0` 就是这条）");
        }

        /// <summary>改名牌：⚠️ **原版这一页点了什么都不发生** —— `ProfileNameTitleSection.OnChangePlayerNameButtonClick`
        /// 的多播链上**只有 `ProfileTab` 订阅了**，本页的 `Profile Player Info` 是**另一个实例**（§C·2 的表里没有它）
        /// ⇒ 我们**如实出声**，不自己发明一条链（红线的另一面：不许静默失败）。</summary>
        void OnEditName()
        {
            Debug.Log("[Profile] `Ranking` 页的改名键：**原版这一页没有订阅者**（改名链只挂在 `ProfileTab` 那个实例上，§C·2）"
                    + "⇒ 点了什么都不发生。要改名请到 `Profile` 页点那个键。");
        }

        /// <summary>🆕 **2026-10-26（批次 4 · F7 · 任务 ④）**：本页 `Player Level Text` 那颗字
        /// （`RefreshIdentity` 里按 `CardTierConfig.PlayerLevelText` 重写）。
        /// ⚠️ 声明在**类尾**（`CLAUDE.md` §三「新成员追加到类尾」）—— 本页既有成员的行号一个不动。</summary>
        Label _levelLabel;

        // ============================================================ 🆕 2026-10-28（B92-N4 批）
        // 🔴 纪律：追加在类尾（本文件被账按行号引用 —— 既有落点最高到 `:659`，本段全在其后 ⇒ 零漂）。

        /// <summary>`AllFactions` 那 13 行**按哪个次序、建哪几个阵营**。
        /// <para>原版：这栏的每一条是**服务器 `LeaderboardConfig` 的 per-army 排行项**（`RankedTab.InitializeFactionScores`，
        /// 判据 → 账 `个人资料/成就/排行 · RankedTab.InitializeFactionScores`：`LeaderboardDefinition[]` 由
        /// `LeaderboardConfigProvider` 取，**本地没有那份配置**）⇒ 原版的**行数与次序都来自服务端**。</para>
        /// <para>🔶 自己设计（非原版）：**行的集合与次序**取 `CampaignData.Armies`（本机 13 个阵营的表序）。
        /// 为什么必须自己定 —— 服务器已关服，per-army 排行目录（`LeaderboardConfig`）在远端、`assets_full`
        /// 与远端 722 个 MonoScript 双侧 0 命中 ⇒ 本机**永远拿不到**那份目录（判据 → 账里那一段）；
        /// 而「一栏一行都建不出来」是可观测的功能缺失。**分数仍留空**（⛔ 不编数值）。
        /// 次序在原版是「按分数降序」，我们没有分数 ⇒ 退到本机阵营表序（⛔ 别当成原版次序）。
        /// · B92-N4 2026-10-28，调度台裁定（`B92_波4_派活与文件所有权` 行 38 的返工清单 ①）。</para></summary>
        static string[] RankedFactionArmies()
        {
            var a = CampaignData.Armies;
            if (a == null || a.Length == 0)
            {
                Debug.LogWarning("[Ranked] `CampaignData.Armies` 为空 ⇒ `AllFactions` 一行都不建（出声，⛔ 不编阵营表）");
                return null;
            }
            return a;
        }

        /// <summary>建一行里那颗 `icon` 节点（**空态**：节点与 `ImageQuad` 在、**画面无**）。
        /// <para>🔴 **2026-10-29（`B97_W2`）就地订正（铁律 5）：本方法不再解析、也不再画图。** 徽记那**一句**
        /// （原版 `FactionScore__Initialize.c:17-19` 的 `factionImage.sprite = ArmyUtilities.GetArmyIcon(faction)`）
        /// 从此**只有一份实现** = `FactionScore.Initialize`（`BuildRow` 末尾的 `fs.InitializeOrEmpty(...)` 调它）；
        /// 本方法只剩「结构」那一半，与 `FactionScore.Build` 的 `IconNodeAt(..., empty)` 同一口径。
        /// ⇒ 旧的 `ArmyIcons.IconName` + `Art()` 取图与那条缺图警告**已移除**（⛔ 别加回来 = 两处各画一次）。</para>
        /// <para>⚠️ **节点与 `ImageQuad` 必须留着**（⛔ 别删成裸节点）：宿主断言 `Editor/MainMenuScene.cs` 拿这颗
        /// `icon` 量矩形；`Editor/ProfileWindowAssertE12.cs` 还要求「`icon` 节点数 == 带 `ImageQuad` 的节点数」。</para>
        /// <para>⚠️ **不传 `keepAspect`**（= 默认拉伸）：原版这一格是固定框内的 `Image`，而宿主断言量的是
        /// **整框宽 1305.29..1448.39** ⇒ 一旦改成等比内接，渲出来的宽会小于框宽、那条断言会红。</para></summary>
        ImageQuad FactionIconQuad(Transform row, PxRect ic)
        {
            return Rect(row, null, ic, "icon", L_Bg2, Transparent);   // 空态：节点在、画面无（贴图由 `FactionScore.Initialize` 写）
        }

        // ============================================================ 原版 `FactionScore.Initialize`（B94_S3 · 2026-10-29）
        // 🔴 追加在**类尾**（本文件被账按行号引用 263/272/355/603/610/631/659 ⇒ 插中间会整体漂移）。
        //
        // 判据 = `d:/2/tools/decomp_full/FactionScore__Initialize.c` **全文**（逐句）：
        //   ```
        //   factionImage(+0x20).sprite = ArmyUtilities.GetArmyIcon(Instance, faction);   // 阵营徽记
        //   allianceRatingDisplay(+0x28).Initialize(currentScore, null, …, disableIfZero: 1, …);
        //   allianceRatingDisplay2(+0x30).Initialize(highestScore, null, …, disableIfZero: 1, …);
        //   ```
        //   ⇒ 三件：**一张徽记图 + 两颗 AllianceRatingDisplay 的分数**（`disableIfZero` 那一格 `.c` 实参是 `1`）。
        // ⚠️ 我们这一侧的对应物 = `BuildRow`（`FactionScoreSmall` 那一行）：
        //   🆕 2026-10-29（`B97_W2`）：**三句**都过 `FactionScore` —— 唯一实现 = `FactionScore.Initialize`（`BuildRow` 末尾 `fs.InitializeOrEmpty(...)`；`FactionIconQuad` 只建**空态节点**、不画图），结构走 `RatingRow`（`Alliance Rating Display` / `… (1)`）；**分数本身是服务器的**
        //   （`RankedTab.InitializeFactionScores` 那一族）⇒ 没有数据时原样保留原版的空态 `------`。
        //   🔶 自己设计（非原版）：`FactionScoreProvider` / `FactionHighestProvider` 这两个注入点 ——
        //   原版分数由服务器下发（关服后判据在远端）⇒ 本地留一个「谁来喂」的口，默认 null = 没有数据 ·
        //   调度台 2026-10-29 裁定（B94_S3）.

        /// <summary>= 原版 `FactionScore.Initialize(faction, currentScore, …)` 的第 2 实参来源。
        /// 形参 = 行下标（= `CampaignData.Armies` 的下标）。**返回 null = 没有数据**（画空态 `------`）。</summary>
        public static System.Func<int, int?> FactionScoreProvider;
        /// <summary>= 第 3 实参（`highestScore`）来源。语义同上。</summary>
        public static System.Func<int, int?> FactionHighestProvider;

        /// <summary>某一行的分数文本（`null` ⇒ <see cref="ScoreEmpty"/>，即原版那颗 display 的空态串）。
        /// <paramref name="highest"/> = 取第 2 颗（`Alliance Rating Display (1)`）还是第 1 颗。</summary>
        public static string FactionScoreText(int row, bool highest)
        {
            int? v = FactionScoreValue(row, highest);         // 🔴 单一来源（同两个注入点，见类尾）—— ⛔ 别在这里再读一遍 provider
            return v.HasValue ? v.Value.ToString() : null;    // 没有数据源 ⇒ null（交给 RatingRow 落空态）
        }

        /// <summary>自检口（两个静态注入点 ⇒ 每条断言收尾必须还原）。</summary>
        public static void ResetFactionScoreForTest()
        {
            FactionScoreProvider = null; FactionHighestProvider = null;
        }

        // ============================================================ 🆕 2026-10-29（B97_W2）：`FactionScore` 接线（三个小口）
        // 🔴 纪律：追加在**类尾**（本文件被账按行号引用 263/272/355/603/610/659 ⇒ 插中间会整体漂移）。

        /// <summary>把 `RatingRow` 建好的那颗节点包成 <see cref="CardPresentation.ScoreBars.AllianceRatingDisplay"/>
        /// （原版那是 prefab 上的序列化组件）。
        /// <para>为什么不让 `RatingRow` 直接返回它：`RatingRow` 被 `BuildCard`（`Top4` 的 `Big` 那四张卡）共用，
        /// 改它的签名会让那一族的行号与行为一起动 —— 本批只接线 `AllFactions` 那一族（`Small`）。</para>
        /// <para>子件名 = `Main Icon` / `Individual rating value`（`RatingRow` 建的就是这两个；取不到 ⇒ 传 null，组件自己出声）。</para></summary>
        static CardPresentation.ScoreBars.AllianceRatingDisplay ArdOf(Transform row, string nodeName)
        {
            if (row == null) return null;
            var t = row.Find(nodeName);
            if (t == null) return null;
            var ard = t.gameObject.AddComponent<CardPresentation.ScoreBars.AllianceRatingDisplay>();
            var lbl = t.Find("Individual rating value");
            var icn = t.Find("Main Icon");
            ard.Bind(lbl != null ? lbl.GetComponent<Label>() : null,
                     icn != null ? icn.GetComponent<ImageQuad>() : null);
            return ard;
        }

        /// <summary>阵营名（`CampaignData.Armies[i]`）→ <see cref="CardArmy"/>（`FactionScore.Initialize` 的实参类型）。
        /// 认不出 ⇒ `Neutral`(0)（= 原版 `ArmyIconsSO` 里没有的那一格 ⇒ 取图返回 null、⛔ 不顶替别的阵营）。</summary>
        static CardArmy FactionOf(string army)
        {
            int id = ArmyIcons.Id(army);                      // 认不出 ⇒ -1（⚠️ 不是 0 —— 0 是 `Neutral`，合法值）
            return (CardArmy)(id >= 0 ? id : ArmyIcons.Neutral);
        }

        /// <summary>某一行的分数值（`null` = 没有数据源）。与 <see cref="FactionScoreText"/> **共用同两个注入点**，
        /// 只是要 `int?` 形态（`FactionScore.Initialize` 吃 `int`，见 <see cref="CardPresentation.ScoreBars.FactionScore.InitializeOrEmpty"/>）。</summary>
        static int? FactionScoreValue(int row, bool highest)
        {
            var f = highest ? FactionHighestProvider : FactionScoreProvider;
            if (f == null) return null;
            return f(row);
        }

        // ============================================================ 🆕 2026-10-29（B97_W13）：`Top4` 那四颗 `FactionScoreBig` 的接线口
        // 🔴 纪律：追加在**类尾**（本文件被账按行号引用 263/272/355/603/610/631/659 ⇒ 插中间会整体漂移）。

        /// <summary>`Top4Entry.FactionArt`（那一格喂进来的**阵营徽记 sprite 名**）→ <see cref="CardArmy"/>。
        /// <para>原版这一族拿到的是 **`CardArmy` 值本身** —— `RankedTab__InitializeFactionScores.c` 里
        /// `FactionScore__Initialize(top4Factions[i], army, value, highestValue)` 的第 2 实参来自
        /// `FeatureConfig.GetValidArmies()` 的元组；我们的 `Top4Data` 带的是**图名**（`Top4Entry.FactionArt`）
        /// ⇒ 在这里按 `ArmyIcons.Table` 的 `Sprite` 字段反查回去。</para>
        /// <para>⛔ **别按前缀拼名字**：`Genestealers` / `SpaceWolves` 不带 `Faction`、`Goff → FactionOrks`
        /// （判据 → `Core/ArmyIcons.cs` 文件头 ② 与它那张 13 行表）。</para>
        /// <para>认不出 / 空 ⇒ `Neutral`(0) 并**出声**（= 原版 `ArmyIconsSO` 没有这一条 ⇒ `GetArmyIcon` 返回 null、
        /// ⛔ 不顶替别的阵营的徽记）。</para></summary>
        public static CardArmy FactionOfSprite(string sprite)
        {
            if (!string.IsNullOrEmpty(sprite))
            {
                var t = ArmyIcons.All;                       // 13 条，顺序 = `IconsByArmySO` 的序列化顺序
                for (int i = 0; i < t.Length; i++)
                    if (t[i].Sprite == sprite) return (CardArmy)t[i].Army;
            }
            Debug.LogWarning("[Ranked] `Top4` 的阵营徽记图名「" + (sprite ?? "<空>") + "」不在 `ArmyIcons` 的 13 条里 "
                           + "⇒ 按 `Neutral`(0) 处理（原版 `ArmyIconsSO` 没有这一条 ⇒ 取图返回 null、⛔ 不顶替别的阵营）");
            return (CardArmy)ArmyIcons.Neutral;
        }

        /// <summary>`Top4Entry.Rating` / `MaxRating`（我们数据模型里是**字符串**）→ 原版 `FactionScore.Initialize`
        /// 第 2 / 3 实参要的 `int`。空串 / 解析不出 ⇒ `null`（= 没有数据 ⇒ `InitializeOrEmpty` 落原版空态串 `------`）。
        /// <para>为什么要这一步：原版那两格收到的就是 `LeaderboardResult` 里的真 `int`
        /// （`RankedTab__InitializeFactionScores.c` 那句 `FactionScore__Initialize(top4Factions[i], army, value, highestValue)`），
        /// 而我们的 `Top4Data` 是字符串形态 ⇒ 在这里归一。⛔ **别改成写死 `null`** —— 那会把已经喂进来的分数抹成 `------`。</para></summary>
        static int? ScoreOf(string s)
        {
            int v;
            return !string.IsNullOrEmpty(s) && int.TryParse(s, out v) ? (int?)v : null;
        }
    }
}
