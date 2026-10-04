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
//    选哪张由 `RankedDivisions.GetDivisionData(divisionNumber)` 决定 —— **那个 SO 本地没有**（§D·1）
//    ⇒ 「段位号 → 图名」的对照表**拿不到**，我们**不画段位图**（画了就是编一个段位）。
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
// 段位图与名次图**不画**。头像/名字/等级那一块**照 Profile 页那套**（我们有真名字与真头像）。
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
        /// ⇒ 立绘必须排在边框**之后**。原来这里是 `L_Art`(2) < `L_Frame`(3) ⇒ 立绘被压成黑块。
        /// 判据 → `ProfileTab.L_ArtOverFrame` 那段注释（含实据路径）。</summary>
        const int L_ArtOverFrame = L_Frame + 1;
        const int L_Text = 4;    // 正文
        const int L_Text2 = 5;   // 次要文字
        const int L_Hit = 6;     // 命中区

        /// <summary>`top4Factions` 的**数组顺序**（§A·6：**不是树序**）—— 只用来给自检断这件事。
        /// 我们摆格子按**树上的名字** `#1..#4`，不按这个数组。</summary>
        public static readonly string[] Top4Order = { "#1", "#3", "#2", "#4" };

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

        MenuScroll _scroll;
        Transform _content;
        ImageQuad _avatarArt;
        Label _playerName;
        ImageQuad[] _rowIcons = new ImageQuad[0];

        /// <summary>这一页建出来的阵营行数（自检用；原版是「前 4 名用树上的格子、其余 Instantiate」）。</summary>
        public int BuiltRows { get; private set; }

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
            Rect(ic, "Player_Avatar_selected", new PxRect(AvHlL, AvHlT, AvHlR, AvHlB), "Highlight", L_Bg2, null, true);
            _avatarArt = CosmeticRect(ic, CurrentAvatarArt(), new PxRect(AvImL, AvImT, AvImR, AvImB), "Image", L_ArtOverFrame);
            Rect(ic, "Player_Profile_Border", new PxRect(AvBdL, AvBdT, AvBdR, AvBdB), "Border", L_Frame, null, true);
            // 🔴 **2026-10-07（A62 子表 A · A39 / 判据 §③「碰巧对」）**：原版 `Avatar Name` 是 **`折行=1`**，
            //   我们原来没显式声明（靠 `SetAutoFitBox` 顺带打开）⇒ 补 `wrap: true` 钉死。
            var an = Text(av, "", new PxRect(AvL, AvB, AvR, AvNmB), Color.white, "Avatar Name", 36f, L_Text2,
                          autoFit: true, autoMinPx: 12f, wrap: true);
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
            var rkNameR = new PxRect(PnL, PnT, PnR, PnB);
            _playerName = Text(nh, ProfileData.PlayerName, rkNameR, NameGold, "Player Name",
                               PnPx, L_Text, autoFit: true, autoMinPx: PnAutoMin, alignLeft: true);
            if (_playerName != null) { _playerName.SetWrapping(false); MenuDraw.AlignLeft(_playerName, rkNameR); }
            var rkTitleR = new PxRect(PtL, PtT, PtR, PtB);
            var rkTitleLb = Text(na, "Player Title", rkTitleR, Color.white, "Player Title",
                                 PtPx, L_Text2, autoFit: true, autoMinPx: PtAutoMin, alignLeft: true);
            if (rkTitleLb != null) { rkTitleLb.SetWrapping(false); MenuDraw.AlignLeft(rkTitleLb, rkTitleR); }

            var lv = Node(info, "Player Level", new PxRect(PlL, PlT, PlR, PlB));
            Rect(lv, "UI_Button_Round_background", new PxRect(PlL, PlT, PlR, PlB), "Image", L_Art);
            Text(lv, "-", new PxRect(PltL, PltT, PltR, PltB), Color.white, "Player Level Text",
                 PltPx, L_Text2, autoFit: true, autoMinPx: PltAutoMin, wrap: true);
        }

        // ============================================================ ② `Top4`

        void BuildTop4()
        {
            var t4 = Node("Top4", new PxRect(T4L, T4T, T4R, T4B));
            Nine(t4, ArtSubmenu, new PxRect(T4L, T4T, T4R, T4B), CardBorder, "bg", L_Bg);
            var ct = Node(t4, "content", new PxRect(CtL, CtT, CtR, CtB));

            // 左列两格（#1 / #2）
            BuildCard(ct, "left-side", "#1", LsL, LsR, Card1T);
            BuildCard(ct, "left-side", "#2", LsL, LsR, Card2T);
            BuildCenter(ct);
            BuildCard(ct, "right-side", "#3", RsL, RsR, Card1T);
            BuildCard(ct, "right-side", "#4", RsL, RsR, Card2T);

            Debug.Log("[Ranked] `Top4`：**阵营分是服务器的**（`LeaderboardManager.GetLeaderboard`，§C·2）⇒ 四格都建、"
                    + "**分数留空**；段位图那一套（7 大段位 ×6 名次）**不画**（「段位号 → 图名」的 SO 本地没有，§D·1）。"
                    + "⚠️ `top4Factions` 的**数组顺序是 `#1,#3,#2,#4`**（不是树序，§A·6）。");
        }

        /// <summary>一列（`left-side` / `right-side`）。列是 VLG（spacing 50 · pad 10 · `ctrlW/ctrlH=1`）。</summary>
        void BuildCard(Transform parent, string colName, string label, float colL, float colR, float top)
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
            Rect(card, null, ir, "icon", L_Bg2);                        // 阵营图：**空态不画**（没有阵营分可排）
            RatingRow(card, "Alliance Rating Display", ArtRankIcon,
                      new PxRect(r.x1 + CardInnerOff, r.y1 + FIconT + FIconH,
                                 r.x1 + CardInnerOff + CardInnerW, r.y1 + FIconT + FIconH + FRtH),
                      SmallIconW, RowValPx, RowValAutoMin);
            RatingRow(card, "MaxRating", ArtGalonIcon,
                      new PxRect(r.x1 + CardInnerOff, r.y1 + FIconT + FIconH + FRtH,
                                 r.x1 + CardInnerOff + CardInnerW,
                                 r.y1 + FIconT + FIconH + FRtH + FMaxH),
                      SmallIconW, RowMxPx, RowValAutoMin);
        }

        /// <summary>一个 `AllianceRatingDisplay` 行（HLG，spacing 0）。原版三件：`Secondary Icon`（**出厂 F** ⇒
        /// 不参与布局）、`Main Icon`、`Individual rating value`。§A·4 的修正：x **不减** `Secondary Icon` 的宽。
        /// ⚠️ 原版是**居中**（align 4）；居中量要用字体度量 ⇒ 我们按**左对齐**摆（分数为空时两者视觉一致）。
        /// ⚠️ `iconW` **逐处不同**（卡里 40 · 排行榜行里 151.82 · 中间列 58.6）—— 别用一个常数套所有。</summary>
        void RatingRow(Transform parent, string name, string iconArt, PxRect r, float iconW, float px, float autoMin)
        {
            var row = Node(parent, name, r);
            var si = Rect(row, ArtRankIcon, new PxRect(r.x1, r.y1, r.x1, r.y1), "Secondary Icon", L_Art, null, true);
            if (si != null) si.gameObject.SetActive(false);             // 出厂 F
            float iw = Mathf.Min(iconW, r.W);
            Rect(row, iconArt, new PxRect(r.x1, r.y1, r.x1 + iw, r.y2), "Main Icon", L_Art, null, true);
            Text(row, ScoreEmpty, new PxRect(r.x1 + iw, r.y1, r.x2, r.y2), Color.white, "Individual rating value",
                 px, L_Text, autoFit: true, autoMinPx: autoMin, alignLeft: true, wrap: true);
        }

        /// <summary>中间那一列：大标题 `Global Rating` → 段位大图 → `footer`（`MainRating` > `Global Rating`）。</summary>
        void BuildCenter(Transform parent)
        {
            var c = Node(parent, "center", new PxRect(CsL, CtT, CsR, CtB));
            // 🔴 **A62 · A44**：原版 `Top4 > center > … > DivisionText`（画 `'Global Rating'`）是 **`折行=0 auto[18~38]`** ⇒ 关掉。
            var grTitle = Text(c, "Global Rating", new PxRect(CsL, DtxT, CsR, DtxB), RankInk, "DivisionText",
                               38f, L_Text, autoFit: true, autoMinPx: 18f);           // ⚠️ 名字叫 DivisionText，**装的是大标题**
            if (grTitle != null) grTitle.SetWrapping(false);
            // `DivisionImage`（段位大图 + `RankImage` 名次数字）—— **不画**：段位图 ↔ 段位号的对照本地没有（判据 ③）
            var di = Node(c, "DivisionImage", new PxRect(CsL, DimgT, CsR, DimgB));
            var ri = Node(di, "RankImage", new PxRect(732.08f, 639.33f, 805.94f, 652.66f));
            ri.gameObject.SetActive(false);

            var ft = Node(c, "footer", new PxRect(CsL, FtT, CsR, FtB));
            // `MainRating`：**`m_Enabled=0`**（原版连它的 HLG 一起关掉 ⇒ 那个布局件不跑）。我们照样建节点、不画底
            var mr = Node(ft, "MainRating", new PxRect(591.29f, MrT, 946.73f, MrB));
            var gr = Node(mr, "Global Rating", new PxRect(591.29f, GrT, 946.73f, GrB));
            var si = Rect(gr, ArtRankIcon, new PxRect(591.29f, GrB, 591.29f, GrB), "Secondary Icon", L_Art, null, true);
            if (si != null) si.gameObject.SetActive(false);
            Rect(gr, ArtRankIcon, new PxRect(591.29f, GrT, 591.29f + SIconW, GrB), "Main Icon", L_Art, null, true);
            Text(gr, ScoreEmpty, new PxRect(591.29f + SIconW, GrT, 946.73f, GrB), Color.white,
                 "Individual rating value", 40f, L_Text, autoFit: true, autoMinPx: 18f, alignLeft: true, wrap: true);
        }

        // ============================================================ ③ `AllFactions`

        void BuildAllFactions()
        {
            var af = Node("AllFactions", new PxRect(AfL, AfT, AfR, AfB));
            // 🔴 **A62 · A46**：原版 `Ranking Tab > AllFactions > Faction Ranking Points`（`'Faction Rating'`）
            //   是 **`折行=0 auto[18~38]`** ⇒ 关掉；关完重排（A205）⇒ 左对齐按新宽度再算一次。
            var afHdR = new PxRect(AfHdL, AfHdT, AfHdR, AfHdB);
            var afHd = Text(af, "Faction Rating", afHdR, RankInk, "Faction Ranking Points",
                            AfHdPx, L_Text, autoFit: true, autoMinPx: AfHdAutoMin, alignLeft: true);
            if (afHd != null) { afHd.SetWrapping(false); MenuDraw.AlignLeft(afHd, afHdR); }
            var info = Node(af, "info", new PxRect(AfInfoL, AfInfoT, AfInfoR, AfInfoB));
            Rect(info, ArtInfo, new PxRect(AfInfoL, AfInfoT, AfInfoR, AfInfoB), "Image", L_Art, null, true);
            Nine(af, ArtSubmenu, new PxRect(AfL, AfT, AfR, AfB), CardBorder, "bg", L_Bg);

            var scR = new PxRect(AfScL, AfScT, AfScR, AfScB);
            var sc = Node(af, "scroll rect", scR);
            var vp = Node(sc, "viewport", scR);         // `RectMask2D`（底图 a=0）
            _scroll = NewScroll(scR, 0f, 0f, true);
            _scroll.Owner = Root.gameObject;
            _scroll.OnChanged = RebuildRows;            // 🔴 滚轮要能重画（同其它页那条教训）
            _content = Node(vp, "content", new PxRect(AfScL, AfScT, AfCoR, AfScT));

            Clip = scR;
            BuildRows();
            Clip = null;
        }

        /// <summary>排行行。原版：**前 4 名复用 `#1..#4 FactionScoreBig`（在 `Top4` 里），第 5 名起
        /// `Instantiate(factionScorePrefab)` 到 `factionListHolder`**（§C·2）。我们**没有阵营分** ⇒
        /// 这一栏只建 4 行空壳（行数照预制体的 4 个烘焙实例），并在日志里说明。</summary>
        public void RebuildRows()
        {
            if (_content == null) return;
            for (int i = _content.childCount - 1; i >= 0; i--) DestroyNow(_content.GetChild(i).gameObject);
            Clip = _scroll.Viewport;
            BuildRows();
            Clip = null;
        }

        void BuildRows()
        {
            const int n = 4;                       // 预制体里烘焙了 4 行（`FactionScoreSmall{,1,2,3}`）
            BuiltRows = 0;
            _rowIcons = new ImageQuad[n];
            _scroll.ContentX1 = AfScT;
            _scroll.ContentX2 = AfScT + n * RowH;
            for (int i = 0; i < n; i++)
            {
                float top = AfScT + i * RowH;
                var rr = _scroll.Shift(new PxRect(AfScL, top, AfScL + RowW, top + RowH));
                if (!_scroll.Intersects(rr)) continue;
                BuildRow(rr, i);
                BuiltRows++;
            }
            Debug.Log("[Ranked] `AllFactions`：**各阵营评分是服务器的**（§C·2）⇒ 4 行都建、**分数留空**；"
                    + "阵营图也不画（没有分就没有名次可排）。第 5 名起原版是 `Instantiate(factionScorePrefab)`，"
                    + "我们没有数据 ⇒ **不建**。");
        }

        /// <summary>一行 `FactionScoreSmall`（`Image + FactionScore`）。</summary>
        void BuildRow(PxRect r, int i)
        {
            var row = Node(_content, i == 0 ? "FactionScoreSmall" : "FactionScoreSmall (" + i + ")", r);
            // 底：UGUI 内置 `Background`（32×32 · 九宫 10,10,10,10 · ppu=200）—— 与我们其它「内置图」一致：**不导不画**
            var ic = new PxRect(r.x1 + RowIcL, r.y1 + RowIcT, r.x1 + RowIcL + RowIcW, r.y1 + RowIcT + RowIcH);
            _rowIcons[i] = Rect(row, null, ic, "icon", L_Bg2);          // 阵营图：空态不画
            RatingRow(row, "Alliance Rating Display", ArtRankIcon,
                      new PxRect(r.x1 + RowRtL, r.y1 + RowRtT, r.x1 + RowRtL + RowRtW, r.y1 + RowRtT + RowRtH),
                      SmallIconWBig, RowValPx, RowValAutoMin);
            RatingRow(row, "Alliance Rating Display (1)", ArtGalonIcon,
                      new PxRect(r.x1 + RowRtL, r.y1 + RowMxT, r.x1 + RowRtL + RowRtW, r.y1 + RowMxT + RowMxH),
                      SmallIconWBig, RowMxPx, RowValAutoMin);
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

        /// <summary>点头像：原版这里是 **`ChangeTab&lt;AvatarTab&gt;()`**（`<Initialize>b__10_0`，§C·1 第 4 条）
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
    }
}
