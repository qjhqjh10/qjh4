// ProfileTab.cs — 玩家档案窗第 1 页：`Profile Tab`（原版类名 `ProfileTab`）
//
// ============================ 出处（唯一正本） ============================
// `资料/普查产出_0927/档案窗_Profile页.md` —— §A 是**层 × 参数**表（工具逐行）·
// §A.1 `activeSelf` 的出现条件（**字段↔节点绑定，权威**）· §A.2 图参数 · §A.3 `ChooseNameWindow`
// · §B 入口/调用/监听 · §C 查不到的（**不猜**）。
// 表 = `python 工具/menu_dump.py bundle_menus_assets_all --rt -8094654694055052750 --depth 8 --md`
//
// ---- 🔴 这一页的五条判据（读绑定表/反编译定的，不是照字段猜的）----
// ① **出厂 ON 的是 `Info Section without Alliance`**（with-Alliance 那份 F）：`PlayerInfoDisplay.Initialize`
//    按「被看的人有没有联盟名」二选一（§A.1）。我们没有联盟（无服务器）⇒ 照**运行期那条主导路径**：
//    without-ON / with-OFF / `Invite to alliance` 也**藏**（它只在一条很窄的路上才保持 T，§A.1）。
// ② **`Legendary Display Profile` ↔ `Highest Rank` + `Legendarey Counter` 是互斥两支**
//    （`ProfileRankingSection.Initialize:103-111` 非传奇支 / `:146-154` 传奇支）——
//    **出厂两份都是 T ⇒ 出厂态本身不是任何一个运行态**。我们没有段位数据 ⇒ 走**非传奇支**。
// ③ **改名是一条四跳链**（`Edit Name Button.OnClick → ProfileNameTitleSection → PlayerInfoDisplay →
//    ProfileTab.OnChangePlayerNameButtonClick → WindowsManager.OpenWindow(ChooseNameWindow)`，§B.3）
//    ⇒ 我们直接把 `Edit Name Button` 的命中接到「开这扇内嵌窗」上（中间那三跳是纯转发，我们没有那三层）。
// ④ **`ChooseNameWindow` 是这一页的直接子节点，却铺满整屏**（1920.25×1080，§A.3）—— 它不是子面板，是一扇窗。
// ⑤ **`ProfileTab.changeNameButton`(0x60) 是「只序列化不读」的遗留字段**（§A.1 末：全类没有任何方法读它）——
//    我们照原版把它接在 **with-Alliance 那一份** `Edit Name Button` 上（那是字段真正指的节点），
//    命中则接在**出厂 ON 的 without-Alliance 那一份**上（否则玩家点不到）。
//
// ---- 🔴 数据：一律空态（用户 2026-09-26 口径「有什么复刻什么，具体的数据和排名这些可以空着」）----
// 逐类规则（**别混起来**，每一类都能复查）：
//   ⓐ 原版预制体自己就写成**空态串**的（`'-'` = `Player Level Text`；`'------'` = Current Rank 的
//      `Individual rating value`）⇒ **照抄那个串**。
//   ⓑ 原版预制体写的是**占位文字**的（`'Player Name'` / `'Player Title'` / `'Avatar name'` / `'Alliance Name'`）
//      ⇒ 有本地数据的用数据，没有的**照抄占位串**。
//   ⓒ 原版预制体写的是**示例数据**的（`'Ultramarines'` / `'312 days'` / `'4879'` / `'Division V'` /
//      `'16'` / `'Level: 0'`）⇒ **留空串**（不编数字，铁律 3）。
//   ⓓ **玩家名是唯一的例外**：我们**有**一个名字源（`ProfileData.PlayerName`，与联机层**同一个**，
//      默认机器名）⇒ 显示它；`ChooseNameWindow` 改的也是它。**原版这个名字来自服务器**（如实标着）。
//   ⓔ 原版预制体里的**静态标题**照抄（含原版自己的拼写 `'Highest Warlod Mastery'`、
//      以及 Forge 那一格**疑似复制粘贴**的 `'Current campaign'`）—— 照抄是唯一可复查的做法；
//      运行期这两格会被服务端事件文案覆盖，**本地拿不到**（§C）。
//
// ---- 🔴 三处「我们挑的 / 没做」（逐条出声，别当原版行为）----
//   · **头像取自同一扇窗的 `Avatar` 页**（`AvatarTab.Selected`）—— 原版是 `PlayerAvatarDataManager`
//     的存档；我们**没有存档** ⇒ 跨页共用一份内存状态（每次 `OnOpen` 重读）。
//   · **`m_PixelsPerUnitMultiplier` 没实现**（输入框 1.2 / `Generic UI Button` 0.01 /
//     六个页签键 0.92）—— 我们这套 `ImageQuad` 没有这个属性（同 `PlayerProfileWindow` 文件头那条）。
//   · **`Mask` / `RectMask2D` 不做真裁切** —— 我们**没有掩码体系**（工程先例：`ImportDeckPopup`
//     / `PromptPopup` / `SettingsWindow` 都只建节点不裁）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `Profile Tab`（`ProfileTab`）。八个原版字段：`changeNameButton, changeNameWindow,
    /// consecutiveLoginCount, eventSection, infoSection, inviteToAllianceButton, playerIdDisplay, rankingSection`。</summary>
    public class ProfileTab : ProfilePage
    {
        public override WindowTabType Type { get { return WindowTabType.ProfileInfo; } }
        protected override int PageIndex { get { return 0; } }
        public override PxRect PageRect
        {
            get { return new PxRect(PlayerProfileWindow.ContentL, PlayerProfileWindow.RedT,
                                    PlayerProfileWindow.ContentR, PlayerProfileWindow.AreaB); }
        }

        // ============================================================ 队列档（页内分层）
        //
        // 🔴 分层用**渲染队列**、不用 z（CLAUDE.md §三）。三条硬规矩：
        //   · **重叠的两层不许同号**（同号时谁盖谁只看「到相机的距离」，不可控 —— 本工程踩过三次）；
        //   · **命中区要三档**：`PointerLayer` 挑赢家是「队列大的先吃」⇒
        //     本页件 < 改名窗压暗层 < 改名窗里的钮。**这条不写清楚，症状就是「点了没反应」**
        //     （命中区是透明的，肉眼与截图都看不出来）。
        //   · 画图层 0–6、命中区 7–9，正好占满本页那 10 个号（`QPageBase + PageIndex*10`）。
        //
        // 重叠对的分配（逐对核过，别随手改）：
        //   头像 `Highlight`(1) < 立绘(2) < 头像框(3)｜`RankTitleBG`(1) < 段位名(4)｜
        //   钮底(3) < 钮上的图标(4) / 钮上的字(6)｜容器底(0) < 标题(4) < 队伍名(5) < 等级(6)｜
        //   输入框底(1) < 占位(4) / 正文(5)｜压暗(0) < 面板(1,2) < 面板上的字(4,5,6)
        const int L_Bg = 0;        // 卡片 / 容器底板 / 压暗
        const int L_Bg2 = 1;       // 第二层底（`RankTitleBG` / `Highlight` / 输入框底）
        const int L_Art = 2;       // 立绘 / 图标 / 平铺底
        const int L_Frame = 3;     // 边框 / 钮底
        /// <summary>**压在头像框之上**那一档（2026-09-27 修）。
        /// 🔴 原版 `Player_Profile_Border` 那张图的**中心是不透明黑**，而原版的兄弟序是
        /// `Highlight → Border → Image`（`Image` **最后 = 画在最上面**，实据 → `菜单全树.md:4888`）
        /// ⇒ **立绘必须排在边框之后**。原来这里给的是 `L_Art`(2) < `L_Frame`(3) ⇒ **立绘被压成黑块**。
        /// ⚠️ 与 `Button Outline`（也是 `L_Frame + 1`）同值，但两者**不同区域、不重叠**；
        /// 自检直接比两张 quad 的 `RenderQueue`（**不比 z** —— 透明物体按到相机的距离排，见 `CLAUDE.md` §三）。</summary>
        const int L_ArtOverFrame = L_Frame + 1;
        const int L_Title = 4;     // 标题字 / 钮上的图标 / 占位字
        const int L_Text = 5;      // 正文（名字 / 数值 / 输入的字）
        const int L_Text2 = 6;     // 次要文字（称号 / 等级 / 钮上的字）
        const int L_Hit = 7;         // 本页的命中区
        const int L_NameBgHit = 8;   // 改名窗的压暗层（点它 = 关）
        const int L_NameHit = 9;     // 改名窗里的钮（最上）

        // ============================================================ 1) 顶部三件（§A 三行）
        /// <summary>`PlayerId`：`a=(.5,.5)-(.5,.5) p=(.5,.5) pos=(-416.777,-346.916) sz=(562.382,40)`。</summary>
        const float PidL = 351.03f, PidT = 867.38f, PidR = 913.41f, PidB = 907.38f;
        /// <summary>`PlayerId/Image`（复制图标 `40k_profile_icon_copy` 27×34，**Simple + preserveAspect**）。</summary>
        const float PidIcL = 350.67f, PidIcT = 867.88f, PidIcR = 377.85f, PidIcB = 906.87f;
        const float PidTxL = 387.03f, PidTxR = 906.14f;
        /// <summary>`playerIdText`：fs 32 · auto[10~32] · **Left/Middle** · 折行 0 · 色 (1,1,1,**0.631**)。</summary>
        const float PidPx = 32f, PidAutoMin = 10f;
        /// <summary>两处 `playerIdText` 共用的色（`Consecutive login days` 那份逐位相同）。</summary>
        static readonly Color Faded = new Color(1f, 1f, 1f, 0.631f);

        /// <summary>`Consecutive login days`（**出厂 F** · 全包查不到激活点，§A.1 第 10 条）——
        /// 我们照预制体**保持不显示**（`Initialize` 也只改文本、不改 active）。</summary>
        const float ClL = 1258.04f, ClT = 867.37f, ClR = 1747.96f, ClB = 902.35f;
        const float ClPx = 30f, ClAutoMin = 23f;

        /// <summary>`Invite to alliance`：`a=(1,0)-(1,0) p=(1,0) pos=(0,51) sz=(214.543,45.135)`。
        /// 出厂 T，但 `Initialize:126-131` 末尾那条唯一的显式 `SetActive(false)` 走的就是我们这种情况
        /// （不在联盟 ⇒ 藏）⇒ **建成后立刻关**（§A.1）。</summary>
        const float IvL = 1532.42f, IvT = 865.87f, IvR = 1746.97f, IvB = 911.00f;
        const float IvOutL = 1532.92f, IvOutT = 865.87f, IvOutR = 1746.47f, IvOutB = 911.00f;
        const float IvTxL = 1544.52f, IvTxT = 874.46f, IvTxR = 1734.87f, IvTxB = 902.41f;
        const float IvPx = 29.45f, IvAutoMin = 18f;
        const string ArtBtnBg = "40k_menu_bt_general_bg", ArtBtnOutline = "40k_menu_bt__general_outline";
        const string ArtCopyIcon = "40k_profile_icon_copy";
        /// <summary>通用钮的九宫格（两张图都是 51×52、border 15,15,15,15）。</summary>
        static readonly Vector4 BtnBorder = new Vector4(15f, 15f, 15f, 15f);
        static readonly Color BtnBgTint = new Color(0.212f, 0.0941f, 0.098f, 1f);
        static readonly Color BtnOutlineTint = new Color(0.945f, 0.842f, 0.0314f, 1f);

        // ============================================================ 2) `Player Info`（头像 + 名号 + 等级）
        const float PiL = 351.03f, PiT = 168.16f, PiR = 1186.98f, PiB = 320.54f;
        /// <summary>`Avatar Item Small`（`EverguildButton + AvatarDisplay + ItemDrawerComponents`）。</summary>
        const float AvL = 351.03f, AvT = 168.16f, AvR = 510.19f, AvB = 320.54f;
        /// <summary>`Raycast Target`（**透明命中区** 178.90×214.44，色 (1,1,1,**0**)）—— 我们用一个 `Hit` 表达。</summary>
        const float AvHitL = 341.16f, AvHitT = 128.01f, AvHitR = 520.06f, AvHitB = 342.45f;
        const float AvIcL = 351.03f, AvIcT = 168.16f, AvIcR = 510.19f, AvIcB = 283.18f;
        /// <summary>`Image Container/Highlight`（`Player_Avatar_selected`，出厂 T）。</summary>
        const float AvHlL = 351.03f, AvHlT = 164.63f, AvHlR = 513.52f, AvHlB = 281.50f;
        /// <summary>`Image Container/Border`（`Player Profile Border`）。⚠️ 传**下划线版**文件名。</summary>
        const float AvBdL = 351.03f, AvBdT = 179.66f, AvBdR = 510.19f, AvBdB = 294.68f;
        /// <summary>`Image Container/Image`（**立绘**那一层；预制体里 `m_Sprite = 0`，运行期由 `AvatarDisplay` 喂）。</summary>
        const float AvImL = 351.03f, AvImT = 165.46f, AvImR = 510.19f, AvImB = 280.48f;
        /// <summary>`Avatar Item Small/Avatar Name`（**出厂 F** · 比父件低 41.31 —— 真值）。</summary>
        const float AvNmB = 361.85f;

        /// <summary>`Info Section without Alliance`（**出厂 ON 的那一份**）：`510.18,168.16 → 1405.87,320.54`。</summary>
        const float InsL = 510.18f, InsT = 168.16f, InsR = 1405.87f, InsB = 320.54f;
        /// <summary>`Name and Title Holder`（这一份的 VLG 是 `[ok]`，值可信）+ 它的 `NameHolder`。</summary>
        const float NthT = 168.16f, NthB = 217.99f;
        const float NhL = 510.19f, NhR = 1127.11f;
        const float EnbL = 510.19f, EnbT = 168.16f, EnbR = 563.29f, EnbB = 217.99f;
        const float EnoOutL = 510.69f, EnoOutR = 562.79f;
        const string ArtEditIcon = "40k_general_bt_yellow_edit";
        /// <summary>`Player Name`：fs 38.6 · auto[23~45] · **Left/Midline** · 折行 0 · 色 (0.98,0.686,0.169)。</summary>
        const float PnL = 563.29f, PnT = 170.74f, PnR = 805.71f, PnB = 217.99f;
        const float PnPx = 38.6f, PnAutoMin = 23f;
        static readonly Color NameGold = new Color(0.98f, 0.686f, 0.169f, 1f);
        /// <summary>`Player Title`：fs 35 · auto[20~35] · **Left/Middle** · 折行 0 · 白。</summary>
        const float PtL = 510.19f, PtT = 217.99f, PtR = 1127.10f, PtB = 267.81f;
        const float PtPx = 35f, PtAutoMin = 20f;

        /// <summary>`Player Level`（圆底 `UI_Button_Round_background` 237×237）+ 里面的数字。</summary>
        const float PlL = 453.15f, PlT = 259.19f, PlR = 506.27f, PlB = 312.31f;
        const float PltL = 460.01f, PltT = 266.05f, PltR = 499.41f, PltB = 305.45f;
        const float PltPx = 37.2f, PltAutoMin = 18f;

        /// <summary>`Info Section with Alliance`（**出厂 F**，与上面那份互斥）—— 只建结构 + 关掉。</summary>
        const float IwaL = 510.18f, IwaR = 1405.87f;
        /// <summary>`Alliance Info`（`ProfileAllianceDisplay`）：`510.19,220.54 → 1388.84,286.53`。</summary>
        const float AlL = 510.19f, AlT = 220.54f, AlR = 1388.84f, AlB = 286.53f;
        const float AlNmT = 212.13f, AlNmB = 251.06f;      // Alliance Name（比父还高 —— 真值）
        const float AlRtL = 510.19f, AlRtT = 251.06f, AlRtR = 774.12f, AlRtB = 286.53f;
        const string ArtRankIcon = "40k_UI_icon_ranked_Skirmish";
        const float AlSicT = 251.06f, AlSicB = 310.23f;     // Secondary Icon（44.40×59.17，出厂 F）
        const float AlMicT = 246.94f, AlMicB = 290.64f;     // Main Icon（49.31×43.70）
        const float AlRtPx = 37.4f, AlRtAutoMin = 18f;

        // ============================================================ 3) `Ranking`（Current / Highest / 传奇）
        const float RkL = 351.02f, RkT = 327.70f, RkR = 1137.12f, RkB = 857.21f;
        /// <summary>`Current Rank`（`RankingDisplay`）：425.26×529.51 —— `Highest Rank` 与传奇那份同 y。</summary>
        const float CrL = 351.03f, CrT = 327.71f, CrR = 776.28f, CrB = 857.22f;
        const float CrCtL = 375.74f, CrCtT = 339.65f, CrCtR = 748.34f, CrCtB = 829.13f;   // Content
        const float CrTtL = 381.55f, CrTtT = 339.65f, CrTtR = 742.53f, CrTtB = 387.65f;   // Title
        const float CrBgL = 374.42f, CrBgT = 387.65f, CrBgR = 749.67f, CrBgB = 447.65f;   // RankTitleBG
        const float CrDtL = 384.34f, CrDtR = 739.74f;                                     // DivisionText
        /// <summary>`Current Rank/Timer`（**出厂 F**）的原始四值 —— ⚠️ 建的时候**不用它们**：
        /// 那棵子树两份卡逐位相同、只差 `Content` 在哪 ⇒ 按 `Content` 推（见 `BuildRankCard` 里那条注释）。
        /// 这四个数留在这儿只为「表里写的就是这个」可复查。</summary>
        const float CrTmT = 815.13f, CrTmB = 848.13f, CrTmTL = 803.68f, CrTmTB = 859.58f;
        const float CrFtL = 357.04f, CrFtT = 457.65f, CrFtR = 767.04f, CrFtB = 637.65f;   // footer
        const float CrMrL = 368.85f, CrMrT = 457.65f, CrMrR = 755.23f, CrMrB = 517.98f;   // MainRating
        const float CrMsL = 368.85f, CrMsT = 444.56f, CrMsR = 562.04f, CrMsB = 531.07f;   // Mission Milestones
        const float CrGlL = 562.04f, CrGlT = 459.14f, CrGlR = 755.23f, CrGlB = 516.49f;   // Global Rating
        const string ArtRankBg = "40K_main_rank_display";                  // 315×64 · Simple
        const string ArtCardRedSimple = "UI_Deck_Selection_Back_simple";   // 440×656 · border 197,0,199,0
        const string ArtCardRed = "UI_Deck_Selection_Back";                // 439×664 · border 0,325,0,35
        static readonly Vector4 RedSimpleBorder = new Vector4(197f, 0f, 199f, 0f);
        static readonly Vector4 RedBorder = new Vector4(0f, 325f, 0f, 35f);
        /// <summary>卡片标题那行：fs 40 · auto[18~40] · Center/Middle · 色 (0.961,0.914,0.737) · 字距 −2.6。</summary>
        const float RkTitlePx = 40f, RkTitleAutoMin = 18f;
        static readonly Color RankInk = new Color(0.961f, 0.914f, 0.737f, 1f);
        /// <summary>`DivisionText`：fs 36 · auto[18~36]（`Highest Rank` 那份的 fontSize 是 31.35）。</summary>
        const float DvPx = 36f, DvAutoMin = 18f, DvPxHighest = 31.35f;
        /// <summary>`RankTitleBG` 的 `m_Color.a`（两份都是 0.918）。</summary>
        const float RankBgA = 0.918f;
        /// <summary>四个 `RankedSealStep`：48.30×60.33，从 368.85 起、步进 48.2967。</summary>
        const float SealW = 48.2967f, SealH = 60.325f, SealStep = 48.2967f;
        /// <summary>`Individual rating value`：fs 40 · auto[18~40] · Left/Middle。</summary>
        const float RvPx = 40f, RvAutoMin = 18f;
        /// <summary>`LeaderboardButton`（出厂 F · 全包无字段引用，§A.1）—— 我们按「键里 40K_button 系列」摆位。</summary>
        const string ArtMulligan = "UI_Button_Mulligan";
        static readonly Vector4 MulliganBorder = new Vector4(333f, 96f, 333f, 96f);

        /// <summary>`Highest Rank`（另一份 `RankingDisplay`）：359.40×529.51。</summary>
        const float HrL = 777.72f, HrR = 1137.12f;
        const float HrCtL = 798.26f, HrCtT = 340.49f, HrCtR = 1113.41f, HrCtB = 806.70f;
        const float HrTtL = 805.84f, HrTtT = 340.49f, HrTtR = 1105.84f, HrTtB = 388.49f;
        /// <summary>⚠️ `Highest Rank/RankTitleBG` **比卡片还宽**（727.24..1184.44 溢出右边界 47.32）—— **真值**。</summary>
        const float HrBgL = 727.24f, HrBgT = 388.49f, HrBgR = 1184.44f, HrBgB = 448.49f;
        const float HrDtL = 778.14f, HrDtR = 1133.53f;
        /// <summary>同上（`Highest Rank/Timer` 的原始四值，建的时候按 `Content` 推）。</summary>
        const float HrTmT = 792.70f, HrTmB = 825.70f, HrTmTL = 781.25f, HrTmTB = 837.16f;
        const float HrFtL = 750.84f, HrFtT = 448.49f, HrFtR = 1160.84f, HrFtB = 589.39f;
        const float HrMrL = 762.65f, HrMrT = 497.39f, HrMrR = 1149.02f, HrMrB = 540.49f;
        const float HrGlL = 762.65f, HrGlT = 501.74f, HrGlR = 1149.02f, HrGlB = 536.14f;
        const float HrLgL = 818.74f, HrLgT = 589.39f, HrLgR = 1092.94f, HrLgB = 619.39f;   // legendary title
        /// <summary>`Legendarey Counter`（原版拼写就是 `Legendarey`）：`789.43,619.39 → 1122.24,619.39`（**高 0**）。</summary>
        const float HrLcL = 789.43f, HrLcR = 1122.24f;
        const float HrLcTL = 790.50f, HrLcTT = 601.25f, HrLcTR = 1121.17f, HrLcTB = 637.54f;

        /// <summary>`Legendary Display Profile`（`LegendaryRankAllTrophiesDisplay`）—— **出厂 T，但与 `Highest Rank` 互斥**（判据 ②）⇒ 走非传奇支时**关掉**。</summary>
        const float LgCtL = 798.26f, LgCtT = 340.49f, LgCtR = 1113.41f, LgCtB = 806.70f;
        const float LgDiT = 340.49f, LgDiB = 671.45f;
        const string ArtLegend = "07-Legend";       // 512×512 · **ppu=50（不是 100）** ⇒ 显示尺寸 ×2
        /// <summary>传奇卡里 `DivisionImage` 的色 (1,1,1,**0.306**)，`preserveAspect`。</summary>
        const float LegendArtA = 0.306f;
        const float LgTtT = 683.00f, LgTtB = 717.26f;     // legendary title
        const float LgCnT = 728.81f, LgCnB = 771.71f;     // Legendary Counter
        const float LgRtL = 790.50f, LgRtT = 732.12f, LgRtR = 1121.17f, LgRtB = 768.41f;   // 它里面的 Rating Text
        /// <summary>三档奖杯行（Gold / Silver / Bronze）：313.02×106.88，从 783.26 起步进 118.43。</summary>
        const float TpHT = 106.878f, TpStep = 118.43f, TpL = 799.33f, TpR = 1112.35f;
        static readonly string[] TrophyBgs = { "WF_UI_Ranked_Background_Gold", "WF_UI_Ranked_Background_Silver", "WF_UI_Ranked_Background_Bronze" };
        static readonly string[] TrophyNames = { "Gold", "Silver", "Bronze" };
        const string ArtTrophyIcon = "WF_UI_Trophy_Gold";   // ⚠️ 三档**都用 Gold 那张**（预制体真值）
        static readonly Vector4 TrophyBorder = new Vector4(20f, 20f, 20f, 20f);

        // ============================================================ 4) `Events`（三个事件容器）
        const float EvL = 1047.88f, EvT = 327.71f, EvR = 1730.12f, EvB = 857.22f;
        const float EvX1 = 1175.12f, EvX2 = 1730.12f, EvH = 175f, EvStep = 180f;
        const string ArtSubmenuBack = "UI_Deck_Information_submenu_Back";
        const string ArtForgeBg = "40K_profile_ForgeLevel_bg";      // 631×194 · Simple
        static readonly Vector4 SubmenuBorder = new Vector4(18f, 18f, 18f, 18f);
        /// <summary>三格的静态标题 —— 🔴 **照抄预制体字面值**（含 `'Highest Warlod Mastery'` 的原版拼写、
        /// 以及 Forge 那一格疑似复制粘贴来的 `'Current campaign'`）。运行期这三行会被服务端事件文案覆盖。</summary>
        const string EvTitleWarlord = "Highest Warlod Mastery";
        const string EvTitleCampaign = "Current campaign";
        const float EvTitlePx = 25f, EvTitleAutoMin = 12f;
        /// <summary>`ArmyName`：fs 35 · auto[14~35] · **Left/Bottom** · 色 (0.992,0.647,0.188)。</summary>
        const float EvArmyPx = 35f, EvArmyAutoMin = 14f;
        static readonly Color ArmyOrange = new Color(0.992f, 0.647f, 0.188f, 1f);
        /// <summary>`Level`：fs 27 · auto[23~27] · Left/Middle。</summary>
        const float EvLvlPx = 27f, EvLvlAutoMin = 23f;

        // ============================================================ 5) `ChooseNameWindow`（内嵌全屏改名窗）
        const float CnwL = -0.13f, CnwT = 0f, CnwR = 1920.13f, CnwB = 1080f;
        /// <summary>`Dark Background`（`BackgroundCloseButton`）：3161.26×1777.49，色 (0,0,0,**0.694**)。</summary>
        const float DbL = -620.63f, DbT = -213.74f, DbR = 2540.63f, DbB = 1563.74f;
        static readonly Color DarkBg = new Color(0f, 0f, 0f, 0.694f);
        /// <summary>`Generic Popup Background`（`40k_popup` 九宫格 169,160,169,160）。</summary>
        const float PpL = 519.49f, PpT = 395.00f, PpR = 1400.51f, PpB = 696.03f;
        static readonly Vector4 PopupBorder = new Vector4(169f, 160f, 169f, 160f);
        /// <summary>`Mask`/`Background fill`（`40k_popup_texture` 128×128 · **Tiled · ppuMul=2.0** ⇒ 平铺格 64，
        /// 同 `PromptPopup.FillTilePx` 那几处先例）。</summary>
        const float MkL = 529.89f, MkT = 404.44f, MkR = 1390.64f, MkB = 686.22f;
        const string ArtPopupFill = "40k_popup_texture";
        const float FillTilePx = 64f;
        /// <summary>`Choose Name Input Field`（`40K_dropdown_bg` 119×102 · border 23,20,23,20 · 色 (0.286,0.965,0.686)）。</summary>
        const float IfL = 540.13f, IfT = 496.00f, IfR = 1376.29f, IfB = 556.00f;
        const string ArtDropdown = "40K_dropdown_bg";
        static readonly Vector4 DropBorder = new Vector4(23f, 20f, 23f, 20f);
        static readonly Color GreenInk = new Color(0.286f, 0.965f, 0.686f, 1f);
        const float TaL = 550.13f, TaT = 503.00f, TaR = 1366.29f, TaB = 550.00f;    // Text Area
        /// <summary>输入框里的 `Text`：fs 40 · auto[18~40] · **Left/Middle** · **折行 3** · 白。
        /// 🔴 **2026-10-07 就地订正（铁律 5）**：这里原来写「折行 3（**= 不折行**）」—— **那个等号是本仓
        ///   明令不许的等价假设**（普查「没查清的①」原文：`3` 与 `0` 在我们这套排版下**等不等价没有判据**）。
        ///   有判据的只有一半：`NoWrap`/`PreserveWhitespaceNoWrap` 在「**折不折行**」这一件事上同档
        ///   （`TMP_Text.cs:4485` / `:4731` 并列），**空白保留**那一半不同（`:4461`）⇒ 照原版写 `3`。
        ///   判据 = `md "Player Profile Window" --depth 25 --md` 该行 `折行=3`；落地 = `SetWrappingMode(3)`。</summary>
        const float InPx = 40f, InAutoMin = 18f;
        /// <summary>原版 `Text` 的换行模式**原文**（⚠️ 是 **3**，不是 0 —— 见上面那条订正）。
        /// `public`：自检要拿它当期望值（`Editor/MainMenuScene.cs` 那一节），⛔ 别在自检里再写一个字面量 3。</summary>
        public const int InWrapMode = 3;
        /// <summary>`Placeholder`：fs 18 · auto[18~40] · Center/Middle · 色 (0.22,0.22,0.22,**0.5**)。
        /// ⚠️ 预制体里它和 `Text` 的文本**都是空的**（`Text` 里那个是零宽空格）⇒ 我们照原版**不写占位文案**。</summary>
        const float PhPx = 18f, PhAutoMin = 18f;
        static readonly Color PhColor = new Color(0.22f, 0.22f, 0.22f, 0.5f);
        /// <summary>`MessageText`：fs 40 · auto[4~40] · Center/Midline · 白。</summary>
        const float MsgL = 540.13f, MsgT = 426.87f, MsgR = 1376.28f, MsgB = 501.13f;
        const float MsgPx = 40f, MsgAutoMin = 4f;
        const string MsgText = "Choose your player name";
        /// <summary>`Change Name Button`（`PriceDisplayButton`）> `Generic UI Button`（`40K_button` · border 234,46,234,46）。</summary>
        const float CnbL = 822.82f, CnbT = 578.22f, CnbR = 1097.18f, CnbB = 645.78f;
        const float CnbTxL = 835.82f, CnbTxT = 587.84f, CnbTxR = 1084.18f, CnbTxB = 636.16f;
        const string ArtButton = "40K_button";
        static readonly Vector4 ButtonBorder = new Vector4(234f, 46f, 234f, 46f);
        static readonly Color GreenButton = new Color(0.369f, 0.894f, 0.587f, 1f);
        const float CnbPx = 50f, CnbAutoMin = 12f;
        /// <summary>`Button Text` = `'Free'`（**出厂态**）。⚠️ 原版是**两套参数**：`TimesNameChange >= 1` 时
        /// `Price Display` ON / `'Free'` OFF（§A.1）—— 我们没有改名次数（服务器）⇒ 走首次那条。</summary>
        const string FreeLabel = "Free";
        /// <summary>`Price Display`（**出厂 F**）：`251.44×51.47`，里面 icon（无图）+ text（`'300,00'` 是示例价）。
        /// 🔴 **2026-10-07（A77⑫④）`PdTxL/PdTxR` 就地重算**（`PdIcL/PdIcR` 不变）：
        /// 旧值 `938.55 / 1030.77` 是**旧工具**的读数 —— 那版 `rect_of` 的 `scale` 是死参、且 HLG 的
        /// 「前一个子件的 `m_LocalScale`」没进推进量。重算命令（现读，2026-10-07）：
        /// `python 工具/menu_dump.py bundle_menus_assets_all "Player Profile Window" --depth 25 --md`
        /// ⇒ `Price Display > text` = **943.70,586.77→1035.92,638.24**（`icon` 仍是 `887.08→938.55` ✓）。
        /// **为什么变了**：`Price Display` 是 `HorizontalLayoutGroup`，`scaleW=1` 而前一件 `icon` 的
        /// `m_LocalScale = 1.2`（dump 里那行标着 `×1.2 → 视觉 61.77×61.77`）⇒ uGUI 的推进量是
        /// `childSize × scaleFactor`（`51.47×1.2`），而**组内居中**的起始偏移又按**乘过缩放**的 requiredSpace 折半
        /// ⇒ 文字框净位移 = `51.47 × (1.2 − 1) ÷ 2` = **+5.15**（左沿 938.55 → 943.70、右沿同比 +5.15）。</summary>
        const float PdL = 833.20f, PdT = 586.77f, PdR = 1084.64f, PdB = 638.24f;
        const float PdIcL = 887.08f, PdIcR = 938.55f, PdTxL = 943.70f, PdTxR = 1035.92f;
        const float PdPx = 40f, PdAutoMin = 13.46f;
        /// <summary>`Generic Close Button Green`（75×75 圆底）+ `Icon`（`40k_bt_close` 175×174）。</summary>
        const float CbL = 1358.30f, CbT = 362.50f, CbR = 1433.30f, CbB = 437.50f;
        const float CbIcL = 1367.62f, CbIcT = 372.75f, CbIcR = 1423.98f, CbIcB = 427.25f;
        const string ArtRoundBtn = "UI_Button_Round_background", ArtCloseIcon = "40k_bt_close";

        /// <summary>改名长度上限。**我们挑的 24**（原版在 `TMP_InputField.m_CharacterLimit`，本地没取到，§C）。</summary>
        public const int MaxNameLen = 24;

        // ============================================================ 空态串（逐类见文件头 ⓐ/ⓑ/ⓒ）
        /// <summary>ⓐ 空态串：`'-'`（原版 `Player Level Text` 自己用的）。</summary>
        public const string Dash = "-";
        /// <summary>ⓐ 空态串：`'------'`（原版 `Current Rank` 的 `Individual rating value` 自己用的）。
        /// 其余几格原版写的是示例数字（`'4879'` / `'32'` / `'16'`）⇒ 我们**沿用同一个空态串**（我们的选择）。</summary>
        public const string Dash6 = "------";
        /// <summary>ⓑ 占位串（照抄预制体）。`Player Name` 有本地数据，不用它。</summary>
        public const string PlaceholderName = "Player Name", PlaceholderTitle = "Player Title",
                                   PlaceholderAlliance = "Alliance Name";
        /// <summary>ⓒ 段位名：预制体写的是 `'Division V'`（示例数据）⇒ **留空**。段位/评分全是服务器的（§C）。</summary>
        public const string DivisionEmpty = "";
        /// <summary>`PlayerId` 那一行（ⓒ：**Player ID 在服务器**，我们留空；标签照原版的 `PlayerProfile/PlayerIdDisplay` 格式）。</summary>
        public const string IdLine = "Player id: -";
        /// <summary>`Consecutive login days` 那一行（同上；原版串是 `'Consecutive login days: 312 days'`）。</summary>
        public const string LoginLine = "Consecutive login days: -";
        /// <summary>`Invite to alliance` 的文案（原版由 `HasBeenInvited` 在两句话里二选一，§A.1）。</summary>
        public const string InviteLabel = "Invite to Alliance";
        public const string LeaderboardLabel = "Leaderboard", LegendaryPointsLabel = "Legendary Points";
        /// <summary>`Player Level Text` 的值：照抄原版预制体自己的空态串 `'-'`（ⓐ）。</summary>
        public const string PlayerLevelText = Dash;

        // ============================================================ 建的
        Transform _nameWin;      // `ChooseNameWindow`（出厂关）
        Label _nameField;        // 输入框里的字
        Label _playerName;       // `Player Name`（改名后要刷它）
        ImageQuad _avatarArt;    // `Player Info` 的立绘
        Label _avatarNameLabel;  // （出厂 F 的那行名；留引用备查）

        /// <summary>改名窗开着没有（自检用）。</summary>
        public bool NameWindowOpen { get { return _nameWin != null && _nameWin.gameObject.activeSelf; } }

        protected override void Build()
        {
            BuildTopRow();
            BuildPlayerInfo();
            BuildRanking();
            BuildEvents();
            BuildNameWindow();
            RefreshIdentity();
        }

        /// <summary>每次切到本页时重读「我是谁」（原版 `OnOpen → Initialize`）：名字可能刚被改名窗改过、
        /// 头像可能刚在 `Avatar` 页换过 ⇒ 两处都要重读。</summary>
        public override void OnOpen() { RefreshIdentity(); }

        // ============================================================ 1) 顶部三件

        void BuildTopRow()
        {
            // `PlayerId`：点它 = 复制 Player ID（`GUIUtility.set_systemCopyBuffer(PlayfabId)` + 一条提示，§B.2）
            var pid = Node("PlayerId", new PxRect(PidL, PidT, PidR, PidB));
            Rect(pid, ArtCopyIcon, new PxRect(PidIcL, PidIcT, PidIcR, PidIcB), "Image", L_Art, null, true);
            // 🔴 **2026-10-07（A62 子表 A · A1）**：原版 `playerIdText` 是 **`折行=0`**
            //   （判据 = `md "Player Profile Window" --depth 25 --md`），而 `SetAutoFitBox` 内部**无条件开折行**
            //   ⇒ 显式关掉。⚠️ 关完会**重排**（`Label.SetWrapping` → `ForceRelayout`，A205）⇒ 左对齐按新宽度再算一次。
            var pidTx = new PxRect(PidTxL, PidT, PidTxR, PidB);
            var pidLb = Text(pid, IdLine, pidTx, Faded, "playerIdText",
                             PidPx, L_Text, autoFit: true, autoMinPx: PidAutoMin, alignLeft: true, wrap: false);
            if (pidLb != null) { pidLb.SetWrapping(false); MenuDraw.AlignLeft(pidLb, pidTx); }
            Hit(pid, "Hit", new PxRect(PidL, PidT, PidR, PidB), L_Hit, OnCopyPlayerId);

            // `Consecutive login days`（出厂 F · 查不到激活点）⇒ 建成后关（判据 §A.1）
            var cl = Node("Consecutive login days", new PxRect(ClL, ClT, ClR, ClB));
            var lb = Text(cl, LoginLine, new PxRect(ClL, ClT, ClR, ClB), Faded, "playerIdText",
                          ClPx, L_Text, autoFit: true, autoMinPx: ClAutoMin, alignLeft: false, wrap: true);
            if (lb != null) MenuDraw.AlignRight(lb, new PxRect(ClL, ClT, ClR, ClB));   // 原版 Right/Middle
            cl.gameObject.SetActive(false);

            // `Invite to alliance`（出厂 T，但运行期在我们这种情况是 **F**）⇒ 建成后关（判据 §A.1）
            var iv = Node("Invite to alliance", new PxRect(IvL, IvT, IvR, IvB));
            Nine(iv, ArtBtnBg, new PxRect(IvL, IvT, IvR, IvB), BtnBorder, "Image", L_Frame, BtnBgTint);
            // `Button Outline`：`fillCenter=0`（**只画四边**，中间透空）
            Nine(iv, ArtBtnOutline, new PxRect(IvOutL, IvOutT, IvOutR, IvOutB), BtnBorder,
                 "Button Outline", L_Frame + 1, BtnOutlineTint, fillCenter: false);
            Text(iv, InviteLabel, new PxRect(IvTxL, IvTxT, IvTxR, IvTxB), Color.white, "Text",
                 IvPx, L_Text2, autoFit: true, autoMinPx: IvAutoMin, alignLeft: false, wrap: true);
            iv.gameObject.SetActive(false);
        }

        // ============================================================ 2) `Player Info`

        void BuildPlayerInfo()
        {
            var info = Node("Player Info", new PxRect(PiL, PiT, PiR, PiB));

            // --- 头像（`Avatar Item Small`）---
            var av = Node(info, "Avatar Item Small", new PxRect(AvL, AvT, AvR, AvB));
            var ic = Node(av, "Image Container", new PxRect(AvIcL, AvIcT, AvIcR, AvIcB));
            // `Highlight` 挂在 `Image Container` 下（照原版树）；出厂 T
            Rect(ic, "Player_Avatar_selected", new PxRect(AvHlL, AvHlT, AvHlR, AvHlB), "Highlight", L_Bg2, null, true);
            _avatarArt = CosmeticRect(ic, CurrentAvatarArt(), new PxRect(AvImL, AvImT, AvImR, AvImB), "Image", L_ArtOverFrame);
            Rect(ic, "Player_Profile_Border", new PxRect(AvBdL, AvBdT, AvBdR, AvBdB), "Border", L_Frame, null, true);
            // 出厂 F 的那行名（`AvatarDisplay.avatarName` 只 set_text、从不 SetActive，§A.1）⇒ 建成后关
            // 🔴 **2026-10-07（A62 子表 A · A4 / 判据文件 §③「碰巧对」）**：原版 `Avatar Name` 是 **`折行=1`**，
            //   而我们原来**没显式声明**（靠 `SetAutoFitBox` 顺带打开 = 碰巧对）⇒ 补 `wrap: true` 把它钉死
            //   （哪天 `SetAutoFitBox` 不再有那个副作用，这里也不会静默回退成不折行）。
            _avatarNameLabel = Text(av, "", new PxRect(AvL, AvB, AvR, AvNmB), Color.white,
                                    "Avatar Name", 36f, L_Text2, autoFit: true, autoMinPx: 12f, alignLeft: false, wrap: true);
            if (_avatarNameLabel != null) _avatarNameLabel.gameObject.SetActive(false);
            // `Raycast Target` 是**透明命中区**（色 (1,1,1,0)）⇒ 我们用 Hit 表达同一件事
            Hit(info, "AvatarHit", new PxRect(AvHitL, AvHitT, AvHitR, AvHitB), L_Hit, OnAvatarClick);

            BuildInfoSectionWithAlliance(info);
            BuildInfoSectionWithoutAlliance(info);

            // --- `Player Level`（圆底 + 数字）---
            var lv = Node(info, "Player Level", new PxRect(PlL, PlT, PlR, PlB));
            Rect(lv, ArtRoundBtn, new PxRect(PlL, PlT, PlR, PlB), "Image", L_Art);
            Text(lv, PlayerLevelText, new PxRect(PltL, PltT, PltR, PltB), Color.white, "Player Level Text",
                 PltPx, L_Text2, autoFit: true, autoMinPx: PltAutoMin, alignLeft: false, wrap: true);
        }

        /// <summary>`Info Section with Alliance`（**出厂 F**）。原版 `ProfileTab.changeNameButton` 指的**就是**
        /// 这一份的 `Edit Name Button`（§A.1 那条容易踩的②）—— 我们照原版把它建在这里，但**不给它加语义**
        /// （那个字段全类没人读，判据 ⑤）。</summary>
        void BuildInfoSectionWithAlliance(Transform info)
        {
            var sec = Node(info, "Info Section with Alliance", new PxRect(IwaL, InsT, IwaR, InsB));
            var nth = Node(sec, "Name and Title Holder", new PxRect(NhL, NthT, NhR, NthB));
            // ⚠️ 这一份的 `Name and Title Holder` 是 **HorizontalLayoutGroup [unk]**（§A.2 末：主轴算不准）
            //    ⇒ 子件的 x **不照抄**（那张表里 `Player Name` 的宽是 0.00），只用 x 独立可信的这两件。
            var enb = Node(nth, "Edit Name Button", new PxRect(EnbL, EnbT, EnbR, EnbB));
            Nine(enb, ArtBtnBg, new PxRect(EnbL, EnbT, EnbR, EnbB), BtnBorder, "Image", L_Frame, BtnBgTint);
            Nine(enb, ArtBtnOutline, new PxRect(EnoOutL, EnbT, EnoOutR, EnbB), BtnBorder,
                 "Button Outline", L_Frame + 1, BtnOutlineTint, fillCenter: false);
            Rect(enb, ArtEditIcon, new PxRect(EnbL, EnbT, EnbR, EnbB), "Icon", L_Text, null, true);
            // 🔴 **2026-10-07（A62 子表 A · A6/A7）**：原版这两件都是 **`折行=0`**（判据 = `md "Player Profile Window" --depth 25 --md`
            //   · `Profile Tab > Info Section … > Player Name / Player Title`）⇒ `SetAutoFitBox` 之后显式关掉；
            //   关完重排（A205）⇒ 左对齐按新宽度再算一次。
            var iwaNameR = new PxRect(PnL, PnT, PnR, PnB);
            var iwaNameLb = Text(nth, PlaceholderName, iwaNameR, NameGold, "Player Name",
                                 PnPx, L_Text, autoFit: true, autoMinPx: PnAutoMin, alignLeft: true, wrap: false);
            if (iwaNameLb != null) { iwaNameLb.SetWrapping(false); MenuDraw.AlignLeft(iwaNameLb, iwaNameR); }
            var iwaTitleR = new PxRect(PtL, PtT, PtR, PtB);
            var iwaTitleLb = Text(nth, PlaceholderTitle, iwaTitleR, Color.white, "Player Title",
                                  PtPx, L_Text2, autoFit: true, autoMinPx: PtAutoMin, alignLeft: true, wrap: false);
            if (iwaTitleLb != null) { iwaTitleLb.SetWrapping(false); MenuDraw.AlignLeft(iwaTitleLb, iwaTitleR); }

            // `Alliance Info`（`ProfileAllianceDisplay`）—— 我们没有联盟 ⇒ 全部空态（ⓑ/ⓒ）
            var al = Node(sec, "Alliance Info", new PxRect(AlL, AlT, AlR, AlB));
            Text(al, PlaceholderAlliance, new PxRect(AlL, AlNmT, AlR, AlNmB), Color.white, "Alliance Name",
                 31.8f, L_Text, autoFit: true, autoMinPx: 23f, alignLeft: true, wrap: true);
            var rt = Node(al, "Alliance Rating Display", new PxRect(AlRtL, AlRtT, AlRtR, AlRtB));
            // `Secondary Icon`（出厂 F · 无 ratingIcon 时关）+ `Main Icon` + 数值
            var si = Rect(rt, ArtRankIcon, new PxRect(AlRtL, AlSicT, AlRtL + 44.4f, AlSicB), "Secondary Icon", L_Art, null, true);
            if (si != null) si.gameObject.SetActive(false);
            Rect(rt, ArtRankIcon, new PxRect(AlRtL, AlMicT, AlRtL + 49.31f, AlMicB), "Main Icon", L_Art, null, true);
            Text(rt, Dash6, new PxRect(AlRtL + 49.31f, AlRtT, AlRtR, AlRtB), Color.white, "Individual rating value",
                 AlRtPx, L_Text, autoFit: true, autoMinPx: AlRtAutoMin, alignLeft: true, wrap: true);
            sec.gameObject.SetActive(false);
        }

        /// <summary>`Info Section without Alliance`（**出厂 ON**）—— `Edit Name Button` 的命中接到改名窗上（判据 ③）。</summary>
        void BuildInfoSectionWithoutAlliance(Transform info)
        {
            var sec = Node(info, "Info Section without Alliance", new PxRect(InsL, InsT, InsR, InsB));
            var nth = Node(sec, "Name and Title Holder", new PxRect(NhL, NthT, NhR, NthB));
            var nh = Node(nth, "NameHolder", new PxRect(NhL, NthT, NhR, NthB));

            var enb = Node(nh, "Edit Name Button", new PxRect(EnbL, EnbT, EnbR, EnbB));
            Nine(enb, ArtBtnBg, new PxRect(EnbL, EnbT, EnbR, EnbB), BtnBorder, "Image", L_Frame, BtnBgTint);
            Nine(enb, ArtBtnOutline, new PxRect(EnoOutL, EnbT, EnoOutR, EnbB), BtnBorder,
                 "Button Outline", L_Frame + 1, BtnOutlineTint, fillCenter: false);
            Rect(enb, ArtEditIcon, new PxRect(EnbL, EnbT, EnbR, EnbB), "Icon", L_Text, null, true);
            Hit(nh, "EditNameHit", new PxRect(EnbL, EnbT, EnbR, EnbB), L_Hit, OpenNameWindow);

            // 🔴 **2026-10-07（A62 子表 A · A10/A11）**：原版 `Ranking Tab` 那份的 `Player Name` / `Player Title`
            //   也是 **`折行=0`**（同一条 dump 命令）⇒ 同上：显式关 + 重排后重做左对齐。
            var woNameR = new PxRect(PnL, PnT, PnR, PnB);
            _playerName = Text(nh, ProfileData.PlayerName, woNameR, NameGold, "Player Name",
                               PnPx, L_Text, autoFit: true, autoMinPx: PnAutoMin, alignLeft: true, wrap: false);
            if (_playerName != null) { _playerName.SetWrapping(false); MenuDraw.AlignLeft(_playerName, woNameR); }
            var woTitleR = new PxRect(PtL, PtT, PtR, PtB);
            var woTitleLb = Text(nth, PlaceholderTitle, woTitleR, Color.white, "Player Title",
                                 PtPx, L_Text2, autoFit: true, autoMinPx: PtAutoMin, alignLeft: true, wrap: false);
            if (woTitleLb != null) { woTitleLb.SetWrapping(false); MenuDraw.AlignLeft(woTitleLb, woTitleR); }
        }

        // ============================================================ 3) `Ranking`

        void BuildRanking()
        {
            var rk = Node("Ranking", new PxRect(RkL, RkT, RkR, RkB));

            // `Current Rank`：`MainRating` 出厂 **T**（`displaySeals=1`）⇒ 带四个封印格
            BuildRankCard(rk, "Current Rank", CrL, CrT, CrR, CrB, CrCtL, CrCtT, CrCtR, CrCtB,
                          CrTtL, CrTtT, CrTtR, CrTtB, "Current Rank", CrBgL, CrBgT, CrBgR, CrBgB,
                          CrDtL, CrDtR, DvPx,
                          CrFtL, CrFtT, CrFtR, CrFtB, CrMrL, CrMrT, CrMrR, CrMrB,
                          CrGlL, CrGlT, CrGlR, CrGlB, CrMsL, CrMsT, CrMsR, CrMsB,
                          ArtCardRedSimple, RedSimpleBorder, true);

            // `Highest Rank`：`MainRating` 出厂 **F**（`displaySeals=0`）⇒ 不建那四个格子
            BuildRankCard(rk, "Highest Rank", HrL, CrT, HrR, CrB, HrCtL, HrCtT, HrCtR, HrCtB,
                          HrTtL, HrTtT, HrTtR, HrTtB, "Highest Rank", HrBgL, HrBgT, HrBgR, HrBgB,
                          HrDtL, HrDtR, DvPxHighest,
                          HrFtL, HrFtT, HrFtR, HrFtB, HrMrL, HrMrT, HrMrR, HrMrB,
                          HrGlL, HrGlT, HrGlR, HrGlB, 0f, 0f, 0f, 0f,
                          ArtCardRed, RedBorder, false);

            BuildLegendaryCard(rk);

            Debug.Log("[Profile] `Ranking` 三块：原版**段位 / 评分 / 封印 / 荣誉**全是服务器的（本地一条都没有，§C）"
                    + "⇒ 整块空态；且按判据 ② 只走**非传奇支**（`Legendary Display Profile` 关）。");
        }

        /// <summary>一张 `RankingDisplay` 卡（`Current Rank` 与 `Highest Rank` 同构，只有几个数不同）。
        /// 🔴 两份的**互斥参数**（§A.1）：`Current Rank` 的 `MainRating` 出厂 **T**（`displaySeals=1`）、
        /// `Highest Rank` 的出厂 **F**（`displaySeals=0`）—— `Mission Milestones Progress` 只在 `MainRating` 下面。</summary>
        void BuildRankCard(Transform parent, string name, float l, float t, float r, float b,
                           float ctL, float ctT, float ctR, float ctB,
                           float ttL, float ttT, float ttR, float ttB, string title,
                           float bgL, float bgT, float bgR, float bgB, float dtL, float dtR, float dvPx,
                           float ftL, float ftT, float ftR, float ftB,
                           float mrL, float mrT, float mrR, float mrB,
                           float glL, float glT, float glR, float glB,
                           float msL, float msT, float msR, float msB,
                           string cardArt, Vector4 cardBorder, bool sealsOn)
        {
            var card = Node(parent, name, new PxRect(l, t, r, b));
            Nine(card, cardArt, new PxRect(l, t, r, b), cardBorder, "Generic Window Red Background Small", L_Bg);

            // `LeaderboardButton`（出厂 F · 全包无字段引用 —— §A.1）⇒ 建成后关。
            // ⚠️ 它的 rect 在表里是 **y 189.56..242.79**（在卡片**之上**、页面之外）—— 那是**模板位**
            //    （它在父的 VLG 里、而 VLG 的主轴算不准，§A.2 末）⇒ 我们按卡片顶部对齐摆一小块，**只求结构在**。
            var lbR = new PxRect(l + 67.15f, t + 20f, l + 358.10f, t + 73.23f);
            var lb = Node(card, "LeaderboardButton", lbR);
            Nine(lb, ArtMulligan, lbR, MulliganBorder, "Image", L_Frame);
            // 🔴 **2026-10-07（A62 子表 A · A12）**：原版 `Button Text`（`'Leaderboard'`）= **`折行=0`**
            //   （判据 = `md "Player Profile Window" --depth 25 --md`，该行 `折行=0 auto[10~36]`）⇒ 关掉。
            var lbTxt = Text(lb, LeaderboardLabel, new PxRect(lbR.x1 + 13.33f, lbR.y1 + 5.22f, lbR.x2 - 14.27f, lbR.y2 - 5.21f),
                             Color.white, "Button Text", 36f, L_Text2, autoFit: true, autoMinPx: 10f, alignLeft: false, wrap: false);
            if (lbTxt != null) lbTxt.SetWrapping(false);
            lb.gameObject.SetActive(false);

            var ct = Node(card, "Content", new PxRect(ctL, ctT, ctR, ctB));
            // 🔴 **A62 · A13**：`Title`（`'Current Rank'`）原版 `折行=0 auto[18~40]` ⇒ 关掉。
            var ctTitle = Text(ct, title, new PxRect(ttL, ttT, ttR, ttB), RankInk, "Title",
                               RkTitlePx, L_Title, autoFit: true, autoMinPx: RkTitleAutoMin, alignLeft: false, wrap: false);
            if (ctTitle != null) ctTitle.SetWrapping(false);

            // `RankTitleBG`（`40K_main_rank_display` · Simple · a=0.918）> `DivisionText`
            Rect(ct, ArtRankBg, new PxRect(bgL, bgT, bgR, bgB), "RankTitleBG", L_Bg2,
                 new Color(1f, 1f, 1f, RankBgA));
            // 🔴 **A62 · A14**：`DivisionText`（`'Division V'`）原版 `折行=0 auto[18~36]` ⇒ 关掉。
            var ctDiv = Text(ct, DivisionEmpty, new PxRect(dtL, bgT, dtR, bgB), RankInk, "DivisionText",
                             dvPx, L_Title, autoFit: true, autoMinPx: DvAutoMin, alignLeft: false, wrap: false);
            if (ctDiv != null) ctDiv.SetWrapping(false);

            // `Timer`（出厂 F · `RankingDisplay.timerDisplay` 只填内容、**从不 SetActive 它** —— §A.1）
            // 里面两件照预制体建（`Timer Icon` + `Timer` 文本），父关着 ⇒ 看不见。
            // 🔴 它整棵子树的坐标**两份卡逐位相同**（都相对 `Content` 的左上：图标 x −103.24 y −14.00，
            //    文本 x −64.40 y −25.45，高 45.90 —— 两份卡只差 Content 自己在哪）⇒ 按 `ctL/ctB` 推。
            var tm = Node(ct, "Timer", new PxRect(ctL, ctB, ctR, ctB));
            tm.gameObject.SetActive(false);
            var tmIc = new PxRect(ctL - 103.24f, ctB - 14.00f, ctL - 70.24f, ctB + 19.00f);
            Rect(tm, "WF_icon_clock", tmIc, "Timer Icon", L_Art);
            // 🔴 **A62 · A15**：`Timer`（`'Ends in: 23d 5h'`）原版 `折行=0 auto[18~32]` ⇒ 关掉
            //   （那棵子树出厂关着，但模式与别的件一样是**判据**，不是「看不见就能不管」）。
            var tmTx = Text(tm, "", new PxRect(ctL - 64.40f, ctB - 25.45f, ctL + 103.24f, ctB + 30.45f), Color.white,
                            "Timer", 32f, L_Text, autoFit: true, autoMinPx: 18f, alignLeft: false, wrap: false);
            if (tmTx != null) tmTx.SetWrapping(false);

            // `DivisionImage`：工具给的高是 **0.00**、里面 `RankImage` 的高是 **−8.00**
            // （§A.2 末点名「布局跑出来的垃圾值、别照抄」）⇒ 我们**只建结构不画图**：
            // 段位图（`Roman V` 那一族）我们**没有段位数据**，画出来就是编一个段位。
            var di = Node(ct, "DivisionImage", new PxRect(ctL - 9.19f, bgB + 10f, ctR + 9.18f, bgB + 10f));
            Node(di, "RankImage", new PxRect(dtL + 138.61f, bgB + 14f, dtL + 216.79f, bgB + 6f));

            var ft = Node(ct, "footer", new PxRect(ftL, ftT, ftR, ftB));
            // `Highest Faction Rating`（出厂 F · 无字段引用 —— §A.1）⇒ 关
            var hf = Node(ft, "Highest Faction Rating",
                          new PxRect(ftL - 177.72f, ftT + 160.10f, ftR - 232.28f, ftT + 199.90f));
            hf.gameObject.SetActive(false);
            // `MainRating`（逐卡 T/F）> `Mission Milestones Progress` / `Global Rating`
            var mr = Node(ft, "MainRating", new PxRect(mrL, mrT, mrR, mrB));
            if (!sealsOn) mr.gameObject.SetActive(false);
            else
            {
                var ms = Node(mr, "Mission Milestones Progress", new PxRect(msL, msT, msR, msB));
                var steps = Node(ms, "steps", new PxRect(msL, msT, msR, msB));
                for (int i = 0; i < 4; i++)
                {
                    float x = msL + SealStep * i;
                    // 原版这四格是 `Rank Skull` + **色 (1,1,1,0)**（透明，出厂本来就看不见）；
                    // 我们没有段位 ⇒ **只建节点不画图**（画了就等于编一个「你走到了第几档」）
                    Node(steps, i == 0 ? "RankedSealStep" : "RankedSealStep (" + (i + 1) + ")",
                         new PxRect(x, msT + 13.09f, x + SealW, msT + 13.09f + SealH));
                }
            }
            var gl = Node(ft, "Global Rating", new PxRect(glL, glT, glR, glB));
            // `Secondary Icon`（出厂 F）+ `Main Icon` + 数值
            var gs = Rect(gl, ArtRankIcon, new PxRect(glL, glB, glL, glB), "Secondary Icon", L_Art, null, true);
            if (gs != null) gs.gameObject.SetActive(false);
            Rect(gl, ArtRankIcon, new PxRect(glL, glT - 14.84f, glL + 60f, glT + 30.16f), "Main Icon", L_Art, null, true);
            Text(gl, Dash6, new PxRect(glL + 60f, glT, glR, glB), Color.white, "Individual rating value",
                 RvPx, L_Text, autoFit: true, autoMinPx: RvAutoMin, alignLeft: true, wrap: true);
        }

        /// <summary>`Legendary Display Profile`（传奇支）—— **出厂 T，但与 `Highest Rank` 互斥**（判据 ②）。
        /// 我们没有段位数据 ⇒ 走非传奇支、**整卡关掉**；结构照预制体建（含三档奖杯行），供以后接线。</summary>
        void BuildLegendaryCard(Transform parent)
        {
            var card = Node(parent, "Legendary Display Profile", new PxRect(HrL, CrT, HrR, CrB));
            Nine(card, ArtCardRed, new PxRect(HrL, CrT, HrR, CrB), RedBorder,
                 "Generic Window Red Background Small", L_Bg);

            var ct = Node(card, "Content", new PxRect(LgCtL, LgCtT, LgCtR, LgCtB));
            var di = Node(ct, "DivisionImage", new PxRect(LgCtL - 37.89f, LgDiT, LgCtR + 37.89f, LgDiB));
            // `07-Legend` 512×512 **ppu=50（不是 100）** ⇒ 显示尺寸 ×2（390.92 → 781.84）；色 a=0.306
            var tex = Art(ArtLegend);
            if (tex != null)
            {
                float half = 390.92f, cy = (LgDiT + LgDiB) * 0.5f;
                MenuDraw.Rect(di, tex, new PxRect(960f - half, cy - half, 960f + half, cy + half),
                              "Body", Q + L_Art, new Color(1f, 1f, 1f, LegendArtA), true, Clip);
            }
            var ri = Node(di, "RankImage", new PxRect(916.74f, LgDiT + 107.29f, 994.93f, LgDiT + 132.39f));
            ri.gameObject.SetActive(false);      // 出厂 F（那一份的角色图没填）
            // 🔴 **A62 · A17**：`legendary title`（`'Legendary Points'`）原版 `折行=0 auto[18~28]` ⇒ 关掉。
            var lgdTx = Text(ct, LegendaryPointsLabel, new PxRect(HrLgL, LgTtT, HrLgR, LgTtB), RankInk, "legendary title",
                             28f, L_Title, autoFit: true, autoMinPx: 18f, alignLeft: false, wrap: false);
            if (lgdTx != null) lgdTx.SetWrapping(false);

            var cn = Node(ct, "Legendary Counter", new PxRect(HrLcL, LgCnT, HrLcR, LgCnB));
            Rect(cn, ArtRankBg, new PxRect(HrLcL, LgCnT, HrLcR, LgCnB), "Image", L_Bg2);
            var rt = Node(cn, "Rating Text", new PxRect(LgRtL, LgRtT, LgRtR, LgRtB));
            var s2 = Rect(rt, ArtRankIcon, new PxRect(LgRtL, LgRtB, LgRtL, LgRtB), "Secondary Icon", L_Art, null, true);
            if (s2 != null) s2.gameObject.SetActive(false);
            Rect(rt, ArtRankIcon, new PxRect(LgRtL, LgRtT - 22.56f, LgRtL + 60f, LgRtT + 22.44f), "Main Icon", L_Art, null, true);
            Text(rt, Dash6, new PxRect(LgRtL + 60f, LgRtT, LgRtR, LgRtB), Color.white,
                 "Individual rating value", 32f, L_Text, autoFit: true, autoMinPx: 18f, alignLeft: true, wrap: true);

            // 三档奖杯（Gold / Silver / Bronze）。⚠️ `Victories number` / `Victories text` 这两个**名字**
            //    是原版预制体复制粘贴来的 —— 装的其实是「奖杯数」与 `Trophies` 文案（照原版名字建）。
            for (int i = 0; i < 3; i++)
            {
                float top = 783.26f + TpStep * i, bot = top + TpHT;
                var row = Node(ct, "Player Profile Ranked Trophies " + TrophyNames[i],
                               new PxRect(TpL, top, TpR, bot));
                Nine(row, TrophyBgs[i], new PxRect(824.71f, top + 0.46f, 1106.14f, bot + 0.46f), TrophyBorder,
                     "Background", L_Bg2);
                Rect(row, ArtTrophyIcon, new PxRect(767.37f, top - 3.02f, 881.22f, top + 110.82f), "Icon", L_Art, null, true);
                // 🆕 **2026-10-11（A317）**：这两处**原来没传 `autoFit` / `autoMinPx`**（靠缺省 `false`/`0f`），
                //   而 `ProfilePage.Text` 本批把它们变必填 ⇒ 现读原版补上：dump 里这两行
                //   （`Victories number` `'0'` / `Victories text` `'Trophies'`）**只有 `字号=36.0`、没有 `auto[…]`**
                //   ⇒ 原版 `m_enableAutoSizing = 0`（`工具/menu_dump.py:592` 只在它非 0 时才印 `auto[…]`）
                //   ⇒ `autoFit: false`（`autoMinPx` 在 `autoFit=false` 时是死值，照今天取 `0f`）。
                //   ⚠️ 与原来逐字等价（原来就是吃这两个缺省）；`对齐=Center/Midline|Capline` · `折行=1` 见同一行。
                Text(row, "0", new PxRect(861.90f, top + 15.53f, 1081.22f, top + 61.07f), Color.white,
                     "Victories number", 36f, L_Text, autoFit: false, autoMinPx: 0f, alignLeft: false, wrap: true);
                Text(row, "Trophies", new PxRect(861.90f, top + 49.60f, 1081.22f, top + 99.60f), Color.white,
                     "Victories text", 36f, L_Text2, autoFit: false, autoMinPx: 0f, alignLeft: false, wrap: true);
            }

            card.gameObject.SetActive(false);       // ← 非传奇支（判据 ②）
        }

        // ============================================================ 4) `Events`

        void BuildEvents()
        {
            var ev = Node("Events", new PxRect(EvL, EvT, EvR, EvB));
            // ⚠️ 节点名里的**两个空格**是原版真值（`Warlord  Mastery Container`），别顺手改成一个
            BuildEventContainer(ev, "Warlord  Mastery Container", EvT, ArtSubmenuBack, true, true);
            BuildEventContainer(ev, "Forge Profile Container", EvT + EvStep, ArtForgeBg, false, true);
            BuildEventContainer(ev, "Campaign Profile Container", EvT + EvStep * 2f, ArtSubmenuBack, true, false);
            Debug.Log("[Profile] `Events` 三格：原版装的是**服务端活动状态**（战将精通 / 锻造厂 / 战役）"
                    + "⇒ 我们**留空态**（ArmyName / Level 都不编数字）；三行标题照抄预制体字面值。");
        }

        /// <summary>一个事件容器（555×175）。`warlord` == true 是 `Warlord  Mastery Container` ——
        /// 它的 `Title` / `ArmyName` / `Level` 的 y 与另外两格**不同**（+13.07 vs +1.77）、
        /// `Badge` 是 440×440（另外两格 123.07×104.28）—— **逐格真值，别统一**。</summary>
        void BuildEventContainer(Transform parent, string name, float top, string art, bool nine, bool warlord)
        {
            float l = EvX1, r = EvX2, b = top + EvH;
            var c = Node(parent, name, new PxRect(l, top, r, b));
            if (nine) Nine(c, art, new PxRect(l, top, r, b), SubmenuBorder, "Player Profile Container Base", L_Bg);
            else Rect(c, art, new PxRect(l, top, r, b), "Player Profile Container Base", L_Bg, null, true);

            float tx = warlord ? 1195.12f : 1194.38f;
            float ty = warlord ? top + 13.07f : top + 1.77f;
            Text(c, warlord ? EvTitleWarlord : EvTitleCampaign,
                 new PxRect(tx, ty, tx + (warlord ? 280.40f : 483.05f), ty + 60f),
                 Color.white, "Title", EvTitlePx, L_Title, autoFit: true, autoMinPx: EvTitleAutoMin,
                 alignLeft: true, wrap: true);
            // `Badge`：预制体里**没有 sprite**（运行期由活动给图）⇒ 照 `SearchingOpponentWindow` 的先例**只建节点**
            Node(c, "Badge", warlord ? new PxRect(1397.12f, top - 233.32f, 1837.12f, top + 206.68f)
                                     : new PxRect(1198.59f, top + 53.02f, 1321.65f, top + 157.31f));
            // `ArmyName` / `Level`：**数据留空**（ⓒ）—— 只把结构摆出来
            float ax = warlord ? 1195.12f : 1330.87f;
            float ay = warlord ? top + 73.99f : top + 57.93f;
            Text(c, "", new PxRect(ax, ay, ax + 289.46f, top + 119.53f), ArmyOrange, "ArmyName",
                 EvArmyPx, L_Text, autoFit: true, autoMinPx: EvArmyAutoMin, alignLeft: true, wrap: true);
            Text(c, "", new PxRect(ax, top + 116.36f, ax + 312.27f, top + 148.36f), Color.white, "Level",
                 EvLvlPx, L_Text2, autoFit: true, autoMinPx: EvLvlAutoMin, alignLeft: true, wrap: true);
        }

        // ============================================================ 5) `ChooseNameWindow`

        void BuildNameWindow()
        {
            _nameWin = Node("ChooseNameWindow", new PxRect(CnwL, CnwT, CnwR, CnwB));

            // 压暗（`BackgroundCloseButton`：点它 = 关窗）。⚠️ 它的命中区在**中间那一档**（见队列档注释）
            Rect(_nameWin, null, new PxRect(DbL, DbT, DbR, DbB), "Dark Background", L_Bg, DarkBg);
            // 🔴 **2026-10-07（A77⑧a）：这一处 = 「压暗层命中档」那条规矩的【唯一例外】，⛔ 不许收口到
            //    `MenuDraw.ShadeHit`。** 理由（裁定原文 → `资料/待办判据_阶段二与联机.md` §（一）⑥③ 与
            //    `资料/待办判据_审查发现_1005.md` §⑬②）：
            //      · 那条规矩（档 = **本窗压暗层自己那一档**、且严格低于本窗任何内容命中区档）是给
            //        **整屏模态窗**定的 —— 那种窗打开时底下的窗被 `ToBackground()`，内容不该再被点到；
            //      · 而 `ChooseNameWindow` 在我们这里是**窗内浮层**：它是**本页的直接子节点**（不是
            //        `WindowsManager` 的一扇窗）⇒ 打开时**下层页面仍然 active**（`L_Hit = 7` 那些命中区还在）。
            //        按通例收口 = 把它的档压到 `L_Bg`(0) ⇒ **下层页面那些钮会把这扇窗的压暗层抢走**
            //        （症状：改名窗开着，点窗外却打在本页的钮上），**而改名窗里的钮(9)照样在它上面** ⇒
            //        「收口」在这里是**反效果**；
            //      · ⇒ **有意取 `L_NameBgHit = 8`**：夹在「下层内容(7)」与「浮层内容(9)」之间
            //        （原版的对应物是 `m_RaycastTarget` 的排序，不是某个档号 —— 这一格是**我们的档位分配**）。
            //    ⚠️ **别照原版 prefab 去找这个 8**：它是我们这套「按渲染队列分档」的产物（`CLAUDE.md` §三
            //    那条「分层要用渲染队列、不能用 z」）。判据/自检在 `Editor/ShellScene.cs` ⑤·l。
            Hit(_nameWin, "DarkBgHit", new PxRect(CnwL, CnwT, CnwR, CnwB), L_NameBgHit, CancelNameWindow);

            // 面板 `Generic Popup Background`（九宫格）> `Mask`（**不做真裁切**，见文件头）> `Background fill`（Tiled）
            Nine(_nameWin, "40k_popup", new PxRect(PpL, PpT, PpR, PpB), PopupBorder,
                 "Generic Popup Background", L_Bg2);
            var mk = Node(_nameWin, "Mask", new PxRect(MkL, MkT, MkR, MkB));
            var fill = Art(ArtPopupFill);
            if (fill != null)
                MenuDraw.Tiled(mk, fill, new PxRect(MkL, MkT, MkR, MkB), FillTilePx, Q + L_Art, "Background fill");

            // 输入框（`Choose Name Input Field` > `Text Area` > `Placeholder` / `Text`）
            var fld = Node(_nameWin, "Choose Name Input Field", new PxRect(IfL, IfT, IfR, IfB));
            Nine(fld, ArtDropdown, new PxRect(IfL, IfT, IfR, IfB), DropBorder, "Image", L_Frame, GreenInk);
            // `Text Area` 上挂 `RectMask2D`（裁输入的字）—— 我们没有掩码体系，只建节点（见文件头末尾）
            var ta = Node(fld, "Text Area", new PxRect(TaL, TaT, TaR, TaB));
            // 🔴 **A62 · A22**：`Placeholder` 原版 **`折行=0`**（判据 = `md "Player Profile Window" --depth 25 --md`：
            //   `Choose Name Input Field > Text Area > Placeholder` 那行 `'' 字号=18.0 auto[18.0~40.0] … 折行=0`）⇒ 关掉。
            //   ⚠️ 这一处 `fontPx == autoMinPx` ⇒ 我们**本来就没调** `SetAutoFitBox`（它只在 `fontPx > autoMinPx` 时才调）
            //   ⇒ 今天折行本来就是关的；这一行是**把判据钉在代码里**（别哪天改了那个守卫就静默变成折行）。
            var phTx = Text(ta, "", new PxRect(TaL, TaT, TaR, TaB), PhColor, "Placeholder",
                            PhPx, L_Title, autoFit: true, autoMinPx: PhAutoMin, alignLeft: false, wrap: false);
            if (phTx != null) phTx.SetWrapping(false);
            // 🔴 **A62 · A23 + A77①（第三档）**：原版 `Text` 是 **`折行=3`（`PreserveWhitespaceNoWrap`）**
            //   —— ⛔ **不能用 `SetWrapping(false)` 顶替**（那是把 `3` 静默降级成 `0`；`3` 与 `0` 「等不等价」
            //   至今没有判据，见 `Label.WrappingMode` 头）。判据 = 同一行 dump 的 `折行=3`。
            //   🆕 **2026-10-11（A317）**：`ProfilePage.Text` 的 `wrap` 已**去掉缺省、形参必填** ⇒ 这里显式传
            //   **`wrap: false`** —— 那是本口对「**不是 `1`**」的唯一表示（这一处**逐字保持今天那条路径**：
            //   先落 `0`、再由下面 `SetWrappingMode(InWrapMode)` 设成 `3`；传 `true` 会多一次
            //   `Normal → 3` 的重排，终态一样但路径不同）。
            _nameField = Text(ta, "", new PxRect(TaL, TaT, TaR, TaB), Color.white, "Text",
                              InPx, L_Text, autoFit: true, autoMinPx: InAutoMin, alignLeft: true, wrap: false);
            if (_nameField != null)
            {
                _nameField.SetWrappingMode(InWrapMode);                       // 3
                MenuDraw.AlignLeft(_nameField, new PxRect(TaL, TaT, TaR, TaB)); // 重排（A205）之后重做左对齐
            }
            Hit(fld, "InputHit", new PxRect(IfL, IfT, IfR, IfB), L_NameHit, BeginNameTyping);

            // 🔴 **A62 · A24 / 判据文件 §③「碰巧对」**：原版 `MessageText` 是 **`折行=1`**，原来没显式声明
            //   ⇒ 补 `wrap: true` 钉死（防「`SetAutoFitBox` 副作用哪天没了就静默回退」）。
            Text(_nameWin, MsgText, new PxRect(MsgL, MsgT, MsgR, MsgB), Color.white, "MessageText",
                 MsgPx, L_Title, autoFit: true, autoMinPx: MsgAutoMin, alignLeft: false, wrap: true);

            // `Change Name Button`（`PriceDisplayButton`）> `Generic UI Button` > `Button Text` / `Price Display`
            var btn = Node(_nameWin, "Change Name Button", new PxRect(CnbL, CnbT, CnbR, CnbB));
            var cnQ = Rect(btn, ArtButton, new PxRect(CnbL, CnbT, CnbR, CnbB), "Generic UI Button", L_Frame, GreenButton, true);
            // 🔴 **A62 · A25**：`Button Text`（`'Free'`）原版 `折行=0 auto[12~50]` ⇒ 关掉。
            var freeTx = Text(btn, FreeLabel, new PxRect(CnbTxL, CnbTxT, CnbTxR, CnbTxB), Color.white, "Button Text",
                              CnbPx, L_Text2, autoFit: true, autoMinPx: CnbAutoMin, alignLeft: false, wrap: false);
            if (freeTx != null) freeTx.SetWrapping(false);
            var pd = Node(btn, "Price Display", new PxRect(PdL, PdT, PdR, PdB));
            Rect(pd, null, new PxRect(PdIcL, PdT, PdIcR, PdB), "icon", L_Text2);
            // 🔴 **A62 · A26**：`text`（`'300,00'`）原版 `折行=0 auto[13.46~40]` ⇒ 关掉。
            var pdTx = Text(pd, "", new PxRect(PdTxL, PdT, PdTxR, PdB), Color.white, "text",
                            PdPx, L_Text2, autoFit: true, autoMinPx: PdAutoMin, alignLeft: false, wrap: false);
            if (pdTx != null) pdTx.SetWrapping(false);
            pd.gameObject.SetActive(false);    // 出厂 F（首次改名免费，判据见常量注释）
            // 🆕 A17：原版 `Profile Tab>ChooseNameWindow>Change Name Button>Generic UI Button` 是 SpriteSwap（普查 §块 5 第 6 行）
            Hit(btn, "ChangeNameHit", new PxRect(CnbL, CnbT, CnbR, CnbB), L_NameHit, CommitNameWindow, cnQ, ArtButton);

            // `Generic Close Button Green`（75×75 圆底 + `40k_bt_close`）
            var cb = Node(_nameWin, "Generic Close Button Green", new PxRect(CbL, CbT, CbR, CbB));
            // 🔴 换图落在**圆底那一层**（原版 `Generic Close Button Green` = `UI_Button_Round_background`(Image)
            //    + `Icon`(`40k_bt_close`)；`trans=2` 换的是它自己的 Image，HL = `40k_bt_close_hover`。实测见 `ChooseNameWindow`）
            var cbBaseQ = Rect(cb, ArtRoundBtn, new PxRect(CbL, CbT, CbR, CbB), "Image", L_Frame, null, true);
            Rect(cb, ArtCloseIcon, new PxRect(CbIcL, CbIcT, CbIcR, CbIcB), "Icon", L_Title);
            Hit(cb, "Hit", new PxRect(CbL, CbT, CbR, CbB), L_NameHit, CancelNameWindow, cbBaseQ, null, "40k_bt_close_hover");

            _nameWin.gameObject.SetActive(false);   // 出厂 F（`ProfileTab.Start:19` 显式关它）
        }

        // ============================================================ 交互
        //
        // 🔴 提交/取消**各只有一条路**（`PointerLayer.BeginText` 的回调）：回车与点钮走同一个 `EndText(true)`、
        //    ESC 与点关闭/背景走同一个 `EndText(false)` —— 两条路各写一份迟早不一致（照 `DeckRuntime.EndTextEdit`）。
        // ⚠️ 批处理里没有 `PointerLayer`、也敲不出键盘 ⇒ 自检走 `UiSetName` / `CommitNameWindow` 这两条。

        /// <summary>`Edit Name Button` ⇒ 开改名窗（原版那条四跳链的终点，判据 ③）。</summary>
        public void OpenNameWindow()
        {
            if (_nameWin == null) return;
            _nameWin.gameObject.SetActive(true);
            _nameWin.SetAsLastSibling();
            BeginNameTyping();
            Debug.Log("[Profile] 打开 `ChooseNameWindow`（内嵌的全屏改名窗）—— 输入后**回车**确认、**ESC** 取消");
        }

        /// <summary>把焦点交给输入框（原版 `ChangeNameWindow.Open` 会把当前名字填进 `nameInput`）。
        /// ⚠️ **已经在编辑中就不重开** —— 否则点一下输入框会把**改到一半的字丢掉**（`BeginText` 是用初值重来的）。</summary>
        public void BeginNameTyping()
        {
            if (!NameWindowOpen) { OpenNameWindow(); return; }
            var pl = PointerLayer.Instance;
            if (pl != null)
            {
                if (pl.TextEditing) return;
                pl.BeginText(ProfileData.PlayerName, MaxNameLen,
                             ApplyName,        // 回车 / 点 `Change Name Button`
                             CloseNameWindow,  // ESC / 点关闭钮 / 点压暗层
                             OnNameChanged);   // 每次改动
            }
            RefreshNameField();
        }

        void OnNameChanged(string s) { RefreshNameField(s); }

        /// <summary>把输入框那一行字刷出来（编辑中带一个下划线当光标）。</summary>
        public void RefreshNameField()
        {
            var pl = PointerLayer.Instance;
            RefreshNameField((pl != null && pl.TextEditing) ? pl.TextBuffer : ProfileData.PlayerName);
        }
        void RefreshNameField(string s)
        {
            if (_nameField == null) return;
            var pl = PointerLayer.Instance;
            bool editing = pl != null && pl.TextEditing;
            _nameField.SetText((s ?? "") + (editing ? "_" : ""));
        }

        /// <summary>点 `Change Name Button`：走**同一个提交函数**（`PointerLayer.EndText(true)`）。
        /// 批处理里没有 `PointerLayer` ⇒ 直接落（自检用的就是这一条）。</summary>
        public void CommitNameWindow()
        {
            var pl = PointerLayer.Instance;
            if (pl != null && pl.TextEditing) { pl.EndText(true); return; }
            ApplyName(_nameField != null ? _nameField.Text.TrimEnd('_') : ProfileData.PlayerName);
        }

        /// <summary>点关闭钮 / 点压暗层：取消（原版 `closeButton` + `BackgroundCloseButton`）。</summary>
        public void CancelNameWindow()
        {
            var pl = PointerLayer.Instance;
            if (pl != null && pl.TextEditing) { pl.EndText(false); return; }
            CloseNameWindow();
        }

        /// <summary>提交那一跳（`PointerLayer.BeginText` 的 onCommit）—— **全工程只有这一处写玩家名**。</summary>
        void ApplyName(string s)
        {
            string v = (s ?? "").Trim();
            if (v.Length == 0)
            {
                Debug.Log("[Profile] 名字不能是空的 ⇒ 不改（原版 `ChangeNameWindow` 也是这道校验）");
                CloseNameWindow();
                return;
            }
            ProfileData.PlayerName = v;      // 🔴 唯一写点（联机层的显示名读的是同一个源）
            if (_playerName != null) _playerName.SetText(v);
            CloseNameWindow();
            Debug.Log("[Profile] 玩家名改成「" + v + "」（⚠️ **只在本次会话里**：原版这一步会上传服务器 + 扣改名费）");
        }

        /// <summary>关掉改名窗（提交与取消都走这里）。</summary>
        public void CloseNameWindow()
        {
            var pl = PointerLayer.Instance;
            if (pl != null && pl.TextEditing) pl.EndText(false);
            if (_nameWin != null) _nameWin.gameObject.SetActive(false);
            RefreshNameField();
        }

        /// <summary>自检入口：**直接提交一个名字**（批处理里敲不出键盘 ⇒ 这是两个入口之一，另一个是 `CommitNameWindow`）。</summary>
        public void UiSetName(string s) { ApplyName(s); }

        void OnCopyPlayerId()
        {
            // 原版：`GUIUtility.set_systemCopyBuffer(PlayfabId)` + `UIMessageController.ShowMessage`（§B.2）
            Debug.Log("[Profile] 复制 Player ID：**没有可复制的** —— 原版拷的是 PlayfabId（服务器），本地没有");
        }

        void OnAvatarClick()
        {
            Debug.Log("[Profile] 点头像（原版 `PlayerInfoDisplay.onAvatarClick` → `GameWindowWithTabs.ChangeTab<T>()`，"
                    + "**切到哪个页签没解出**，§C 第 6 条 ⇒ 不猜，只出声）");
        }

        /// <summary>当前该显示哪张头像 —— 🔴 **2026-09-27 收口：读的是全工程唯一那一份**
        /// （`ProfileData.AvatarArt`）。原来这条要**绕到 `Avatar` 页那个实例**上取
        /// （`Win.Page(...) as AvatarTab`）—— 那扇页没建出来 / 档案窗没开过时就取不到，
        /// 而且主菜单**顶栏**那块头像是**另一处在读**，两处各读各的（`PlayerName` 收口那次同一个病）。
        /// ⚠️ 建的时候**必须把真名传进去**：`CosmeticRect(…, null, …)` 会走 `MenuDraw.Rect` 的
        ///    「`tex == null` ⇒ 不建」那条路返回 null，**那一层就根本不存在**（2026-09-27 自检当场抓到）。</summary>
        string CurrentAvatarArt()
        {
            return ProfileData.AvatarArt;
        }

        /// <summary>把「我是谁」刷一遍：玩家名 + 头像（原版 `Initialize` 每次 `OnOpen` 重跑一次）。</summary>
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
                    // 不许静默：立绘没取到就说出来
                    Debug.LogWarning("[Profile] `Player Info` 的立绘取不到（" + (art ?? "清单为空") + "）—— 那一层不画");
                }
            }
            RefreshNameField();
        }
    }
}
