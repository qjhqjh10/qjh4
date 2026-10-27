// ProfileTab.cs — 玩家档案窗第 1 页：`Profile Tab`（原版类名 `ProfileTab`）
//
// ============================ 出处（唯一正本） ============================
// `资料/普查产出_0927/档案窗_Profile页.md` —— §A 是**层 × 参数**表（工具逐行）·
// §A.1 `activeSelf` 的出现条件（**字段↔节点绑定，权威**）· §A.2 图参数 · §A.3 `ChooseNameWindow`
// · §B 入口/调用/监听 · §C 查不到的（**不猜**）。
// 表 = `python 工具/menu_dump.py bundle_menus_assets_all --rt -8094654694055052750 --depth 8 --md`
//
// ---- 🔴 A406（2026-10-12）：本页每一处 `Text(...)` 的 `m_fontSizeMax` / `m_fontSizeBase` 都是**逐站实读**的 ----
// 判据命令（本件亲跑，读数印在 `字号= … 基准= … auto[…]` 那三个字段里）：
//   `python 工具/menu_dump.py bundle_menus_assets_all "Player Profile Window" --depth 25 --md`
// ⚠️ **逐个节点读、不按页/按族统一**（铁律 5·c）：本页实测就有四处同族不同值 ——
//   两棵 `Player Info` 的 `Player Name`（`m_fontSize` 40.7 / 38.6）· 两张卡的 `DivisionText`（36 / 31.35）·
//   两张卡的 `Global Rating …Individual rating value`（40 / 36.3）· 三格 `Events` 的 `ArmyName` 的 `min`（14/13/23）。
//   而 `m_fontSizeMax` / `m_fontSizeBase` 两侧**恰好逐棵相同**的那几族，也**是读过才知道相同**的。
//
// ---- 🔴 这一页的五条判据（读绑定表/反编译定的，不是照字段猜的）----
// ① **出厂 ON 的是 `Info Section without Alliance`**（with-Alliance 那份 F）：`PlayerInfoDisplay.Initialize`
//    按「被看的人有没有联盟名」二选一（§A.1）。我们没有联盟（无服务器）⇒ 照**运行期那条主导路径**：
//    without-ON / with-OFF / `Invite to alliance` 也**藏**（它只在一条很窄的路上才保持 T，§A.1）。
// ② **`Legendary Display Profile` ↔ `Highest Rank` + `Legendarey Counter` 是互斥两支**
//    （`ProfileRankingSection.Initialize:103-111` 非传奇支 / `:146-154` 传奇支）——
//    **出厂两份都是 T ⇒ 出厂态本身不是任何一个运行态**。我们判不了「玩家是不是传奇」
//    （那一格在服务器）⇒ 走**非传奇支**。
//    🔴 **2026-10-24（`E10`）**：段位**图/名**不再留空 —— 按**演示段位号**画（见 `BuildRanking`）；
//    但「走哪一支」仍与非传奇支一致（传奇卡整棵关着），两件事别混起来。
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
//      🔴 **例外（2026-10-24 · `E10`）**：段位**名**与段位**大图**改走**演示段位号**
//      （`RankedDivisionInfo.DemoDivisionNumber` / `DivisionName` / `DivisionArtName`）——
//      理由与逐条出处写在 `BuildRanking` 与 `DrawDivisionArt` 的 doc 里（⛔ 别的格子不受影响）。
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
        /// ⇒ **立绘必须排在边框之后**。原来这里给的是 `L_Art`(2) &lt; `L_Frame`(3) ⇒ **立绘被压成黑块**。
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
        /// <summary>两张排名卡的标题词条（🆕 2026-10-24 E12 · P0-3）。官方两列 = `Current Rank`/`当前排名` ·
        /// `Highest Rank`/`最高排名`（参考图那两格就是中文）。⛔ 别再写死英文 —— 原来是字面量。
        /// ⚠️ 节点名（`Current Rank` / `Highest Rank`）**仍走英文**（原版 prefab 的节点名），只有**上屏文案**走词条。</summary>
        public const string RankTermCurrent = "PlayerProfile/CurrentRank", RankTermHighest = "PlayerProfile/HighestRank";
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
        // ---- `Content/DivisionImage`（段位大图的**槽**）：两张卡的真实矩形 ----------------------------
        // 🔴 **2026-10-24（`E10`）现读**（`资料/普查产出_0927/档案窗_Profile页.md` §A 第 61/92 行）：
        //   `Current Rank/Content/DivisionImage` = 366.58,457.65→757.50,457.65（390.92×**0.00**）
        //   `Highest Rank/Content/DivisionImage` = 760.37,448.49→1151.30,448.49（390.92×**0.00**）
        //   · 两张卡**宽度逐位相同（390.922）**，但**横向溢出 `Content` 的量不同**：
        //     Current 两侧各溢 9.16（ctL 375.74 / ctR 748.34）· Highest 两侧各溢 37.89（798.26 / 1113.41）
        //     ⇒ ⛔ 别再用「ctL−9.19 / ctR+9.18」那一组（那只对 Current 成立，Highest 会窄 57.4）。
        //   · **高度是 0.00** —— 那不是真值，是「布局组 + 图还没喂」的产物（§A.2 明写「别照抄」）：
        //     `Content` 是 `VerticalLayoutGroup`（`ctrlH=1`），`DivisionImage` 的 `LayoutElement` 没给
        //     `preferredHeight`，而 `Image` 自己的 `preferredHeight` 要等运行时 `set_sprite` 之后才有值，
        //     作者态量出来就是 0 ⇒ 宽度是字段真值、**高度得另找依据**（见 `DrawDivisionArt` 的 doc）。
        //   · 两个 y（457.65 / 448.49）**正好是各自 `RankTitleBG` 的下沿**（Current 还多 10 = 中间那颗
        //     `spacing` 槽）⇒ VLG 里 `DivisionImage` 槽的**上沿**就是它。
        static readonly PxRect CrDivR = new PxRect(366.58f, 457.65f, 757.50f, 457.65f);
        static readonly PxRect HrDivR = new PxRect(760.37f, 448.49f, 1151.30f, 448.49f);
        /// <summary>段位大图的**边长 = 卡宽 × 本系数**（512² 方图 + `preserveAspect` ⇒ 画出来是正方形）。
        /// <para>🔴 **这是「我们挑的」，不是 prefab 字段值** —— 三段判据照实写在这里：
        /// ① prefab 那个 `DivisionImage` **只有宽度是真值（390.922）、高度是 0.00**（布局伪值，见上）
        ///    ⇒ 「画多大」在原版本来就不是这个矩形说了算，而是运行时 `set_sprite` 之后
        ///    `Image.preferredHeight` 参与 VLG 重排的结果 ⇒ **字段里查不到**（铁律 2：查不到就说查不到）。
        /// ② 用户实拍 `账户-个人资料参考图.png` 反算（本轮亲量，脚本量的是**紫色圆盘**的 bbox，
        ///    再拿 `04-Admiral` 的同一块圆盘当尺子；屏幕→设计的比例由**两张卡自身的宽度**标定）：
        ///      · 左（`当前排名`）徽记视觉 ≈ **307** 设计 px ÷ 卡宽 425.26 = **0.722**
        ///      · 右（`最高排名`）徽记视觉 ≈ **247** 设计 px ÷ 卡宽 359.40 = **0.688**
        ///    ⇒ 两格**不是等大**（比值 0.806，远超声噪）⇒ 边长**跟卡宽成比例**，取 **0.72**。
        /// ③ 若照派单那句话「尺寸照 prefab 那两颗 `DivisionImage` 的 rect」取**槽宽 390.92** 当边长，
        ///    右卡那张会**比卡还宽 31.5px、两侧各溢 15.7**（卡 359.40，实拍里从未出现）⇒ 不取。
        ///    ⚠️ 真要切回字段档：把本系数改成 `1f`、并把下面 `slot.W *` 换成 `slot.W` 的语义
        ///    （`DrawDivisionArt` 里那两行已按「卡宽」写，切回去只改这一个常量 + 一处取宽）。</para></summary>
        const float DivArtSideRatio = 0.72f;
        /// <summary>建完记下**这次画的是哪张图**（自检读口；`null` = 一张都没画成）。</summary>
        public string DivisionArtUsed { get; private set; }
        /// <summary>本页画成功的段位大图**张数**（正常 = 2；自检读它，⛔ 别让断言去数子节点）。</summary>
        public int DivisionArtDrawn { get; private set; }
        /// <summary>同上（`Highest Rank/Timer` 的原始四值，建的时候按 `Content` 推）。</summary>
        const float HrTmT = 792.70f, HrTmB = 825.70f, HrTmTL = 781.25f, HrTmTB = 837.16f;
        const float HrFtL = 750.84f, HrFtT = 448.49f, HrFtR = 1160.84f, HrFtB = 589.39f;
        const float HrMrL = 762.65f, HrMrT = 497.39f, HrMrR = 1149.02f, HrMrB = 540.49f;
        const float HrGlL = 762.65f, HrGlT = 501.74f, HrGlR = 1149.02f, HrGlB = 536.14f;
        // ---- `Highest Rank` 卡里那两件「传奇点数」（`legendary title` + `Legendarey Counter`）----
        // 🔴 **2026-10-24（E12 · P0-2）**：这两件**原版在非传奇支也是 ON 的**
        //   （`ProfileRankingSection.Initialize:103-111`：非传奇支只关 `Legendary Display Profile`，
        //   `Highest Rank` 那棵整棵保持 T；参考图 `账户-个人资料参考图.png` 里「最高排名」卡下方就印着
        //   「传奇点数」+ 图标 + `0`）。我们原来**根本没建它们** —— 唯一的 Counter 在**隐藏的**
        //   `Legendary Display Profile` 里（`BuildLegendaryCard`）⇒ 那格永远看不见。
        //
        // 🔴 **坐标/字号判据 = `--no-layout` 那一份（作者态）**，并且**就地订正**了这里原来的四个数（铁律 5）：
        //   原来 `HrLgT/B = 589.39/619.39`、`HrLcTT/B = 601.25/637.54` 抄自 `menu_dump` **带布局**那一版，
        //   而那一版把 `Legendarey Counter` 的子件 `Rating Text` 算到了**父块上沿之外**
        //   （601.25 < 619.39 = 父块顶）—— 自相矛盾：该父块此刻 `activeInHierarchy = false`，
        //   工具自己在那两行标着 `⛔GRP-off`（「本表给的是激活之后的值」，即**算出来的伪值**）。
        //   `--no-layout` 那版自洽：counter `624.59..784.40`（332.80×159.81，与序列化 `m_SizeDelta` 同量级）、
        //   `Rating Text` **正在它正中**（中心 704.5 = 父块中心）、`legendary title` 紧贴其上方
        //   （590.33..624.59，VLG spacing 0）—— 与参考图「『传奇点数』下一行才是图标 + 0」逐形吻合。
        //   命令：`python 工具/menu_dump.py bundle_menus_assets_all "Player Profile Window" --depth 25 --md --no-sprite --no-layout`
        const float HrLgL = 818.74f, HrLgT = 590.33f, HrLgR = 1092.94f, HrLgB = 624.59f;   // legendary title（274.20×34.26）
        /// <summary>`Legendarey Counter`（⚠️ 原版拼写就是 `Legendarey`）：`789.43,624.59 → 1122.24,784.40`。
        /// ⚠️ 它那块底图的 `m_Sprite` = **`<未解出 3837025018791853890>`**（`menu_dump` 现读原文；
        /// 本地没有一张图对得上）⇒ **只建节点、不画底图**，并在日志里出声（⛔ 不许画白块 ——
        /// `Rect(..., null, ...)` 会落到 `CardArt.Solid()`，同 `RankedTab` 那两处白块的病）。</summary>
        const float HrLcL = 789.43f, HrLcT = 624.59f, HrLcR = 1122.24f, HrLcB = 784.40f;
        /// <summary>counter 里的 `Rating Text`：`790.50,686.35 → 1121.17,722.64`（330.67×36.29）。</summary>
        const float HrLcTL = 790.50f, HrLcTT = 686.35f, HrLcTR = 1121.17f, HrLcTB = 722.64f;
        /// <summary>`Main Icon`：60×45 @ `912.42,682.00 → 972.42,727.00` —— HLG `align 4`（MiddleCenter）
        /// 算出来的居中位（行宽 330.67、图标 60 + 值 26.83 = 86.83 居中 ⇒ 起点 912.42），
        /// ⛔ 不是「行左沿 + 60」（那是本工程别的几族的写法，这一族不是）。</summary>
        const float HrLcIcL = 912.42f, HrLcIcT = 682.00f, HrLcIcR = 972.42f, HrLcIcB = 727.00f;
        const float HrLcValL = 972.42f;     // 值左沿 = 图标右沿（HLG 推进出来）
        /// <summary>counter 的值：fs 32 · 基准 31.38 · auto[18~32] · **Left/Midline** · 折行 1 · 白。</summary>
        const float HrLcValPx = 32f, HrLcValAutoMin = 18f;
        /// <summary>值 = `0` —— 本地没有传奇点数（服务器数据）。⚠️ prefab 那颗 TMP 的 `m_text` 是**示例** `'32'`
        /// ⇒ ⛔ 不用它；`0` 与参考图那一格一致，也不是我们编的数字（空态就是 0）。</summary>
        public const string LegendaryPointsValue = "0";

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
        /// <summary>三格的标题 = **官方词条**（🆕 2026-10-24（E12 · P0-1）：把「版式 flag」与「标题」拆开）。
        /// <para>🔴 改前：`BuildEvents` 给 `Forge Profile Container` 传的是 `warlord:true`，而 `Text(...)`
        /// 又拿**同一个 flag** 选标题 ⇒ **Forge 那格印的是战将的标题**（实拍 `_tmp_view/menu/09_档案窗_Profile页.png`
        /// 里第 1、2 格同字「Highest Warlod Mastery」）。`warlord` 那个 flag 本来的职责只有**版式**
        /// （y 偏移 / 标题宽 / `Badge` 440 vs 123×104），标题是另一件事。</para>
        /// <para>判据（R2 §② + 原版词条）：`PlayerProfile/HighestWarlordMastery` = `Warlord Mastery` / `战将精通`
        /// （参考图第 1 格就是「战将精通」）· `/HighestForgeLevel` = `Highest Forge Level` / `最高锻造厂等级`
        /// （参考图第 2 格）· `/CurrentCampaign` = `Current Campaign` / `当前战役`（第 3 格）。
        /// ⚠️ 预制体里那三个字面值是 `'Highest Warlod Mastery'`（原版自己的拼写）与两格 `'Current campaign'`
        /// —— 那是**作者态占位**，运行期由服务端事件文案覆盖；本地拿不到那些文案 ⇒ 走上面三条官方词条。</para></summary>
        public const string EvTermWarlord = "PlayerProfile/HighestWarlordMastery";
        public const string EvTermForge = "PlayerProfile/HighestForgeLevel";
        public const string EvTermCampaign = "PlayerProfile/CurrentCampaign";
        /// <summary>`Events/*/Title` 的词条取不到时的英文兜底（= 官方两列的英文那一列，逐字）。</summary>
        const string EvTitleFallbackWarlord = "Warlord Mastery",
                     EvTitleFallbackForge = "Highest Forge Level",
                     EvTitleFallbackCampaign = "Current Campaign";
        /// <summary>`Title` 的 auto 下限**逐格不同**（A406 逐棵读过）：Warlord `auto[12~25]`，Forge / Campaign
        /// `auto[18~25]` —— 上限与 base（25 / 35）三棵相同，**只有下限不同** ⇒ 别一刀切。</summary>
        const float EvTitlePx = 25f, EvTitleAutoMin = 12f, EvTitleAutoMinOther = 18f;
        /// <summary>`ArmyName`：fs 35 · auto[14~35] · **Left/Bottom** · 色 (0.992,0.647,0.188)。
        /// ⚠️ **2026-10-24（E12）起下限由调用点逐格传**（14 / 13 / 23 —— 三棵各不相同），
        /// `EvArmyAutoMin` 保留只为「表里写的就是 14」可复查（战将那一格用的就是它）。</summary>
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
        /// <summary>`MessageText` 的**出厂原文**（原版 prefab 那颗 TMP 的 `m_text`）。
        /// 🔴 **2026-10-17（`A1756`）起上屏走词条** `MainMenu/Register/PleaseChooseName`
        /// （官方两列 = `Choose your player name` / `选择你的玩家名字`，与出厂串逐字相同）
        /// ⇒ ⛔ 别再把这个常量直接印到界面上。</summary>
        public const string MsgTextFactory = "Choose your player name";
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
        /// <summary>🔴 **2026-10-18（A1053）**：关窗钮那颗 `Icon` 子件**自己的** `m_RaycastPadding`
        /// （原版实读 `(-20)⁴`；L,B,R,T · **负 = 外扩**）⇒ 命中区 = 子件矩形（`CbIcL..CbIcB` = 56.36×54.50）
        /// 外扩 20 = **96.37 × 94.50**（⛔ 不是根矩形 `CbL..CbB` 的 75×75）。
        /// 算式只走 `MenuDraw.PaddedRect`；口径 → `普查_全仓命中区与关闭键族.md` §〇-1。</summary>
        static readonly Vector4 ClosePad = new Vector4(-20f, -20f, -20f, -20f);
        const string ArtRoundBtn = "UI_Button_Round_background", ArtCloseIcon = "40k_bt_close";

        /// <summary>改名长度上限 = **25**（原版 `ChangeNameWindow.ValidateInput`：`if (0x18 &lt; len) text =
        /// Substring(0, 0x19)` —— `0x19` = **25** ⇒ 实际最长 25 个字符）。
        /// 🔴 **2026-10-24（E12 · P1-8）就地订正（铁律 5）**：这里原写「**我们挑的 24**（原版在
        /// `TMP_InputField.m_CharacterLimit`，本地没取到）」—— **判据其实一直在**（就是上面那条
        /// `ChangeNameWindow__ValidateInput.c`），24 是我们早先的猜测。`E12` 起按反编译取 25。
        /// ⚠️ **`Net/NetProtocol.cs:324-328` 的名字上限还是 24**（那份不在本批白名单里）⇒ 25 字的名字
        /// 过网时会被截成 24 —— 已记进本批报告，等那份改成 25 才真正一致。</summary>
        public const int MaxNameLen = 25;
        /// <summary>改名最小长度 = **3**（同一条 `ValidateInput`：`if (len &lt; 3) interactable = false`）。</summary>
        public const int MinNameLen = 3;

        // ============================================================ 空态串（逐类见文件头 ⓐ/ⓑ/ⓒ）
        /// <summary>ⓐ 空态串：`'-'`（原版 `Player Level Text` 自己用的；⚠️ **2026-10-26 起我们这一格不再用它** —— 等级改由 `CardTierConfig` 算，见 `PlayerLevelText`）。</summary>
        public const string Dash = "-";
        /// <summary>ⓐ 空态串：`'------'`（原版 `Current Rank` 的 `Individual rating value` 自己用的）。
        /// 其余几格原版写的是示例数字（`'4879'` / `'32'` / `'16'`）⇒ 我们**沿用同一个空态串**（我们的选择）。</summary>
        public const string Dash6 = "------";
        /// <summary>ⓑ 占位串（照抄预制体）。`Player Name` 有本地数据，不用它。</summary>
        public const string PlaceholderName = "Player Name", PlaceholderTitle = "Player Title",
                                   PlaceholderAlliance = "Alliance Name";
        /// <summary>ⓒ **旧口径（已废弃，留着是为了让下一个会话看懂历史）**：段位名原来留空串
        /// （预制体写的是示例 `'Division V'`）。🔴 **2026-10-24（`E10`）已改**：段位名走官方词条
        /// `Ranked/Division/&lt;演示号&gt;`（见 `BuildRanking` 的 `_rankDivisionName` 与 `DivisionText` 那一格的注释）；
        /// 评分/封印/里程碑那些**仍然是服务器数据**，照样留空/不画。</summary>
        // （`DivisionEmpty` 常量随那条旧口径一起删掉了 —— 别再加回来：段位名现在有唯一来源。）
        /// <summary>`PlayerId` 那一行 —— 标签走官方词条 `PlayerProfile/PlayerIdDisplay`
        /// （`Player id: {0}` / `玩家ID：{0}`），值按既定口径取空态 `-`（Player ID 在服务器，§C）。
        /// 🆕 **2026-10-24（E12 · P0-3）**：原来是写死的英文常量 `"Player id: -"` ⇒ 中文档下仍印英文
        /// （用户点名）。⚠️ 从 `const` 改成 `static` 属性只影响读取点 —— `Editor/MainMenuScene.cs:6666`
        /// 那句 `ProfileTab.IdLine` 照样编译，而且它比的是**同一个动态值**（口径不变）。</summary>
        public static string IdLine { get { return string.Format(Loc.T("PlayerProfile/PlayerIdDisplay"), Dash); } }
        /// <summary>`Consecutive login days` 那一行（同上；原版串是 `'Consecutive login days: 312 days'`）。</summary>
        public const string LoginLine = "Consecutive login days: -";
        /// <summary>`Invite to alliance` 的文案（原版由 `HasBeenInvited` 在两句话里二选一，§A.1）。</summary>
        public const string InviteLabel = "Invite to Alliance";
        public const string LeaderboardLabel = "Leaderboard", LegendaryPointsLabel = "Legendary Points";
        /// <summary>「传奇点数」的词条键（🆕 2026-10-24 E12 · P0-3）：官方 `PlayerProfile/LegendaryRank` =
        /// `Legendary Points` / `传奇点数` —— 与 prefab 那颗 TMP 的 `m_text` 逐字相同，故判据成立。
        /// 两处用它：`Highest Rank` 卡里那一行（`BuildLegendaryPointsRow`）与隐藏的传奇卡
        /// （`BuildLegendaryCard`，结构照建、出厂关）。</summary>
        public const string LegendaryRankTerm = "PlayerProfile/LegendaryRank";
        /// <summary>奖杯行底下那行文案（`Victories text`，装的其实是 `Trophies`）的词条键。
        /// 🆕 2026-10-24（E12 · P0-3）：官方 `PlayerProfile/Trophies` = `Trophies` / `奖杯`；原来写死英文。</summary>
        public const string TrophiesTerm = "PlayerProfile/Trophies";
        /// <summary>`Player Level Text` 的值 —— ⚠️ 2026-10-26 订正（铁律 5）：原文「照抄原版预制体自己的空态串 `'-'`」**已过期**（原版 prefab 仍写 `-`，但值由服务器下发；用户当日裁定 (b)「算式照原版、4 个标量自拟」⇒ 改成我们自己算的等级）⇒ 转发 `CardTierConfig`，与顶栏**同一个口**。原文保留以免被误读成「原版也用 `-`」。</summary>
        public static string PlayerLevelText { get { return CardTierConfig.PlayerLevelText; } }

        /// <summary>`Events/*/Level` 那一行的词条 —— **逐格不同**（参考图 `账户-个人资料参考图.png`）：
        /// 战将格印 **胜利数**（`胜利 : 487`）、锻造厂与战役两格印 **等级**（`等级 : 32` / `等级 : 9`）。
        /// 值本地没有（服务器/活动数据）⇒ 按全页既定的空态口径取 `-`（同 `Player id: -`）。</summary>
        public const string EvLevelTermWarlord = "PlayerProfile/VictoriesDisplay";
        public const string EvLevelTermOther = "PlayerProfile/LevelDisplay";

        /// <summary>官方词条 or 英文兜底。⚠️ `Loc.T` 缺键时**返回键名本身**（不是空串）⇒ 直接上屏会印
        /// `PlayerProfile/…` 这种串。3 档顺序 = 手写 `Table` → 数据面 bulk → 官方全表，这里取不到才用兜底。
        /// 同族先例 = `PlayerProfileWindow.TabLabel`。</summary>
        protected static string TermOr(string key, string fallback)
        {
            string v = Loc.T(key);
            return string.IsNullOrEmpty(v) || v == key ? fallback : v;
        }

        // ============================================================ 建的
        Transform _nameWin;      // `ChooseNameWindow`（出厂关）
        Label _nameField;        // 输入框里的字
        Label _playerName;       // `Player Name`（改名后要刷它）
        Label _playerTitle;      // `Player Title`（without-Alliance 那一棵 = 出厂 ON 的那一棵）
        Label _playerTitleWa;    // `Player Title`（with-Alliance 那一棵，出厂 F —— 一并刷，切到那份时才不是旧值）
        ImageQuad _avatarArt;    // `Player Info` 的立绘
        Label _avatarNameLabel;  // （出厂 F 的那行名；留引用备查）
        /// <summary>两张排名卡 `RankTitleBG/DivisionText` 印的段位名（= 官方词条 `Ranked/Division/<演示号>`，
        /// 由 `BuildRanking` 算**一次**、两张卡共用 —— 同一件事不留第二份算法）。</summary>
        string _rankDivisionName;

        /// <summary>改名窗开着没有（自检用）。</summary>
        public bool NameWindowOpen { get { return _nameWin != null && _nameWin.gameObject.activeSelf; } }

        protected override void Build()
        {
            EnsureLocalDefaultTitle();      // 🆕 `U2` V1（见方法 doc）
            BuildTopRow();
            BuildPlayerInfo();
            BuildRanking();
            BuildEvents();
            BuildNameWindow();
            RefreshIdentity();
        }

        // ============================================================ 🆕 2026-10-24（`U2` V1）：头衔空态兜底
        //
        // 🔴 **现象**（`资料/普查产出_1024c/U2_账户窗多余按钮复查.md` §5·2 V1）：没装备称号时
        //    `Title` 页左栏**只剩一颗「选择」钮** —— 上方徽记 + 橙色称号名整片空。
        // 🔴 **原版为什么没有这个空态**：开局那条已装备称号是**服务器**下发的
        //    （`PlayerDataManager.get_PlayerTitle`），所以我们这边是「本地缺数据」造出来的假空态。
        // ⇒ **本地兜底**：没装备过 ⇒ 默认装备**清单第 1 条**（用已有数据、⛔ 不编占位文案）。
        //    `Title.Tab.OnOpen` 本来就用 `PreviewIndex = ProfileData.TitleIndex` ⇒ 数据一到位，左栏自然有东西。
        //
        // ⚠️ **落点说明**：本条最自然的位置是 `Shell/ProfileData.cs`（`_titleIndex` 初值）或
        //    `Shell/TitleTab.cs`（`OnOpen` 那一支）—— **这两个文件都不在本批白名单**（另有代理占着）。
        //    退而求其次放在**本窗第 1 页的 Build** 里：`PlayerProfileWindow.BuildPages` 按 `i` 升序
        //    `Setup()` 六页，本页是 i=0 ⇒ 一定**早于** `TitleTab.Build`（i=2）跑，那一页读到的就是兜底值。
        // ⚠️ ⛔ **不是「为了截图好看」**（`Editor/MainMenuScene.cs` 8144 那条注释担心的是那种）；
        //    这是**复刻原版的开局状态**：原版玩家一进游戏就有已装备称号。
        // ⚠️ 已有装备（`0 ≤ TitleIndex < Count`）⇒ **一个字节都不动**；清单为空 ⇒ **出声**（判据/数据不足）。

        /// <summary>没装备过（`TitleIndex &lt; 0`）且清单非空 ⇒ 默认装备**第 1 条**。
        /// <returns>`true` = 这次真的兜了底；`false` = 已经有装备 / 清单为空（什么都没改）。</returns>
        /// <para>⚠️ 写 `ProfileData.TitleIndex` 会走它的 setter（正常游戏里落 `profile.json`；批处理不写盘 ——
        /// 见 `ProfileData` 的「本地存档」那一节）。</para></summary>
        public static bool EnsureLocalDefaultTitle()
        {
            var list = ProfileData.Titles;
            if (list == null || list.Count == 0)
            {
                Debug.LogWarning("[Profile] 头衔清单为空 ⇒ **给不出**本地默认装备项（判据/数据不足："
                               + "`Resources/profile_cosmetics.json` 没读进来？）—— 头衔页会停在空态");
                return false;
            }
            int cur = ProfileData.TitleIndex;
            if (cur >= 0 && cur < list.Count) return false;          // 已经装备着 ⇒ 不碰
            ProfileData.TitleIndex = 0;
            Debug.Log("[Profile] 头衔空态兜底（`U2` V1）：没装备过 ⇒ 默认装备清单第 1 条「"
                    + list[0].Name + "」—— 原版开局必有已装备项（服务器下发），本地没有来源，"
                    + "这里用**已有数据**兜、⛔ 不编占位文案");
            return true;
        }

        /// <summary>每次切到本页时重读「我是谁」（原版 `OnOpen → Initialize`）：名字可能刚被改名窗改过、
        /// 头像可能刚在 `Avatar` 页换过、**称号可能刚在 `Title` 页装备过** ⇒ 三处都要重读。</summary>
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
            // 🔴 **A406（2026-10-12）逐站实读**：`Profile Tab/PlayerId/playerIdText` 原版
            //   `m_fontSizeMax = 32.0`（= 标称）· `m_fontSizeBase = 35.0`。判据 =
            //   `python 工具/menu_dump.py bundle_menus_assets_all "Player Profile Window" --depth 25 --md`
            //   那一行的 `字号=32.0 基准=35.0 auto[10.0~32.0]`。
            var pidLb = Text(pid, IdLine, pidTx, Faded, "playerIdText",
                             PidPx, L_Text, autoFit: true, autoMinPx: PidAutoMin, alignLeft: true, wrap: false,
                             autoMaxPx: 32f, basePx: 35f, face: TmpFont.Face.LatinCardBody);
            if (pidLb != null) { pidLb.SetWrapping(false); MenuDraw.AlignLeft(pidLb, pidTx); }
            Hit(pid, "Hit", new PxRect(PidL, PidT, PidR, PidB), L_Hit, OnCopyPlayerId);

            // `Consecutive login days`（出厂 F · 查不到激活点）⇒ 建成后关（判据 §A.1）
            var cl = Node("Consecutive login days", new PxRect(ClL, ClT, ClR, ClB));
            // 🔴 **A406**：`Profile Tab/Consecutive login days/playerIdText` 原版
            //   `m_fontSizeMax = 30.0`（= 标称）· `m_fontSizeBase = 35.0`（同一条 dump 的那一行）。
            var lb = Text(cl, LoginLine, new PxRect(ClL, ClT, ClR, ClB), Faded, "playerIdText",
                          ClPx, L_Text, autoFit: true, autoMinPx: ClAutoMin, alignLeft: false, wrap: true,
                          autoMaxPx: 30f, basePx: 35f, face: TmpFont.Face.LatinCardBody);
            if (lb != null) MenuDraw.AlignRight(lb, new PxRect(ClL, ClT, ClR, ClB));   // 原版 Right/Middle
            cl.gameObject.SetActive(false);

            // `Invite to alliance`（出厂 T，但运行期在我们这种情况是 **F**）⇒ 建成后关（判据 §A.1）
            var iv = Node("Invite to alliance", new PxRect(IvL, IvT, IvR, IvB));
            Nine(iv, ArtBtnBg, new PxRect(IvL, IvT, IvR, IvB), BtnBorder, "Image", L_Frame, BtnBgTint);
            // `Button Outline`：`fillCenter=0`（**只画四边**，中间透空）
            Nine(iv, ArtBtnOutline, new PxRect(IvOutL, IvOutT, IvOutR, IvOutB), BtnBorder,
                 "Button Outline", L_Frame + 1, BtnOutlineTint, fillCenter: false);
            // 🔴 **A406**：`Profile Tab/Invite to alliance/Text` 原版
            //   `auto[18.0~**45.0**] 基准=36.0` ⇒ **上限 45.0**（我们原来把 `fontPx 29.45` 当上限 = 真错）·
            //   **base 36.0**。同一条 dump。
            Text(iv, InviteLabel, new PxRect(IvTxL, IvTxT, IvTxR, IvTxB), Color.white, "Text",
                 IvPx, L_Text2, autoFit: true, autoMinPx: IvAutoMin, alignLeft: false, wrap: true,
                 autoMaxPx: 45f, basePx: 36f, face: TmpFont.Face.LatinCardBody);
            iv.gameObject.SetActive(false);
        }

        // ============================================================ 2) `Player Info`

        void BuildPlayerInfo()
        {
            var info = Node("Player Info", new PxRect(PiL, PiT, PiR, PiB));

            // --- 头像（`Avatar Item Small`）---
            // 🔴 **2026-10-24（E18）**：`Image Container` 是**布局框**，底下三层各自按原版 `m_LocalScale`
            //   **绕布局框中心放大**：头像 ×2.0 · 框 ×1.25 · 高亮 ×1.9（判据与「为什么」→
            //   `Shell/AvatarTab.cs` 的 `ScaleImage` 常量注释；同族先例 `Editor/BattleScene.cs:24532`
            //   对**同一颗** `Avatar Item Small` 也是 `Border 1.25 / Image 2.0`）。
            //   ⚠️ 放大后头像会**溢出** `Image Container` 与 `Avatar Item Small`（原版也溢出；父链无遮罩）。
            var av = Node(info, "Avatar Item Small", new PxRect(AvL, AvT, AvR, AvB));
            var ic = Node(av, "Image Container", new PxRect(AvIcL, AvIcT, AvIcR, AvIcB));
            // `Highlight` 挂在 `Image Container` 下（照原版树）；出厂 T
            Rect(ic, "Player_Avatar_selected",
                 AvatarTab.ScaleAboutCenter(new PxRect(AvHlL, AvHlT, AvHlR, AvHlB), AvatarTab.ScaleHighlight),
                 "Highlight", L_Bg2, null, true);
            _avatarArt = CosmeticRect(ic, CurrentAvatarArt(),
                                      AvatarTab.ScaleAboutCenter(new PxRect(AvImL, AvImT, AvImR, AvImB),
                                                                 AvatarTab.ScaleImage),
                                      "Image", L_ArtOverFrame);
            Rect(ic, AvatarTab.BorderArtName(),
                 AvatarTab.ScaleAboutCenter(new PxRect(AvBdL, AvBdT, AvBdR, AvBdB), AvatarTab.ScaleBorder),
                 "Border", L_Frame, null, true);
            // 出厂 F 的那行名（`AvatarDisplay.avatarName` 只 set_text、从不 SetActive，§A.1）⇒ 建成后关
            // 🔴 **2026-10-07（A62 子表 A · A4 / 判据文件 §③「碰巧对」）**：原版 `Avatar Name` 是 **`折行=1`**，
            //   而我们原来**没显式声明**（靠 `SetAutoFitBox` 顺带打开 = 碰巧对）⇒ 补 `wrap: true` 把它钉死
            //   （哪天 `SetAutoFitBox` 不再有那个副作用，这里也不会静默回退成不折行）。
            // 🔴 **A406**：`Player Info/Avatar Item Small/Avatar Name` 原版
            //   `auto[12.0~36.0] 基准=36.0` ⇒ 上限 **36.0**（= 标称）· base **36.0**。
            _avatarNameLabel = Text(av, "", new PxRect(AvL, AvB, AvR, AvNmB), Color.white,
                                    "Avatar Name", 36f, L_Text2, autoFit: true, autoMinPx: 12f, alignLeft: false, wrap: true,
                                    autoMaxPx: 36f, basePx: 36f, face: TmpFont.Face.LatinCardBody);
            if (_avatarNameLabel != null) _avatarNameLabel.gameObject.SetActive(false);
            // `Raycast Target` 是**透明命中区**（色 (1,1,1,0)）⇒ 我们用 Hit 表达同一件事
            // 🆕 2026-10-24（E12 · P0-4）：点它 = 切到 `Avatar` 页（原版 `ProfileTab.<Start>b__9_0`）
            Hit(info, "AvatarHit", new PxRect(AvHitL, AvHitT, AvHitR, AvHitB), L_Hit, GoToAvatarTab);

            BuildInfoSectionWithAlliance(info);
            BuildInfoSectionWithoutAlliance(info);

            // --- `Player Level`（圆底 + 数字）---
            var lv = Node(info, "Player Level", new PxRect(PlL, PlT, PlR, PlB));
            Rect(lv, ArtRoundBtn, new PxRect(PlL, PlT, PlR, PlB), "Image", L_Art);
            // 🔴 **A406**：`Player Info/Player Level/Player Level Text` 原版
            //   `字号=37.2 基准=**36.88999938964844** auto[18.0~37.2]` ⇒ 上限 37.2（= 标称）· base **36.89**。
            // 🆕 2026-10-26（批次 4 · F7 · 任务 ④）：留住这颗字 —— 升级后 `RefreshIdentity` 要重写它。
            _levelLabel = Text(lv, PlayerLevelText, new PxRect(PltL, PltT, PltR, PltB), Color.white, "Player Level Text",
                 PltPx, L_Text2, autoFit: true, autoMinPx: PltAutoMin, alignLeft: false, wrap: true,
                 autoMaxPx: 37.2f, basePx: 36.88999938964844f, face: TmpFont.Face.LatinCardBody);
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
            // 🔴 **A406**：`Info Section with Alliance/Name and Title Holder/Player Name` 原版
            //   `auto[23.0~**45.0**] 基准=35.0` ⇒ 上限 **45.0**（我们原来把 `fontPx 38.6` 当上限）· base 35.0。
            var iwaNameR = new PxRect(PnL, PnT, PnR, PnB);
            // 🔴 **A1024③（2026-10-17）换面**：`Player Profile Window` 里 `Player Name` / `Player Title` /
            //    `Alliance Name` / `ArmyName` / `DivisionText` / `Title`（Current|Highest Rank、Events 小节头）/
            //    `legendary title` 的 `m_fontAsset` = **`Asar-Regular SDF`** ⇒ `LatinUi`；其余（`playerIdText` /
            //    `Player Level Text` / `Individual rating value` / `Victories *` / `Level` / `Timer` / 按钮与输入框）
            //    全是 **Pragati** ⇒ `LatinCardBody`。判据 = `menu_dump … "Player Profile Window" --depth 30`
            //    逐颗现读（⚠️ 同名不同字：`Avatar Name` 在本页 `Avatar Item Small` 下是 **Pragati**，
            //    只有 Avatar/Title 页的**大图**那颗才是 Asar —— 逐站判过，⛔ 别按名统一）。
            var iwaNameLb = Text(nth, PlaceholderName, iwaNameR, NameGold, "Player Name",
                                 PnPx, L_Text, autoFit: true, autoMinPx: PnAutoMin, alignLeft: true, wrap: false,
                                 autoMaxPx: 45f, basePx: 35f, face: TmpFont.Face.LatinUi);
            if (iwaNameLb != null) { iwaNameLb.SetWrapping(false); MenuDraw.AlignLeft(iwaNameLb, iwaNameR); }
            // 🆕 **2026-10-16（A712 阶段 2）**：纵向那一半 —— 原版 `…/Player Name`（fs38.6）
            //   = `对齐=Left/**Midline**`（判据 = 本文件 `本文件` 那一行 + `python 工具/menu_dump.py
            //   bundle_menus_assets_all "Player Profile Window" --depth 12 --md` 实读）。
            MenuDraw.SetVAlign(iwaNameLb, Label.VAlign.Midline, iwaNameR);
            // 🔴 **A406**：同树 `Player Title` 原版 `auto[20.0~35.0] 基准=35.0` ⇒ 上限 35（= 标称）· base 35.0。
            var iwaTitleR = new PxRect(PtL, PtT, PtR, PtB);
            // 🆕 **2026-10-22（P1）**：这一格原来印的是 prefab 的**占位串 `'Player Title'`** ——
            //   参考图（`账户-个人资料参考图.png`）那一行是**已装备的称号**（`复仇的兄弟`，与称号页「正在使用」同一条）。
            //   判据 = `PlayerProfileUIController` 有 `titleText` 字段 + `SetProfileTitle`（空串 ⇒ 整块关）；
            //   数据源 = `ProfileData.TitleName`（全工程唯一一份；称号页装备的就是它）。
            var iwaTitleLb = Text(nth, ProfileData.TitleName, iwaTitleR, Color.white, "Player Title",
                                  PtPx, L_Text2, autoFit: true, autoMinPx: PtAutoMin, alignLeft: true, wrap: false,
                                  autoMaxPx: 35f, basePx: 35f, face: TmpFont.Face.LatinUi);
            if (iwaTitleLb != null) { iwaTitleLb.SetWrapping(false); MenuDraw.AlignLeft(iwaTitleLb, iwaTitleR); }
            _playerTitleWa = iwaTitleLb;

            // `Alliance Info`（`ProfileAllianceDisplay`）—— 我们没有联盟 ⇒ 全部空态（ⓑ/ⓒ）
            var al = Node(sec, "Alliance Info", new PxRect(AlL, AlT, AlR, AlB));
            // 🔴 **A406**：`…/Alliance Info/Alliance Name` 原版
            //   `字号=31.8 基准=36.0 auto[23.0~**37.400001525878906**]` ⇒ 上限 **37.4**（我们原来拿 31.8 当上限）
            //   · base **36.0**。（全库同族两颗同值 —— 本页这一颗 + `Ranking Tab` 那一颗。）
            Text(al, PlaceholderAlliance, new PxRect(AlL, AlNmT, AlR, AlNmB), Color.white, "Alliance Name",
                 31.8f, L_Text, autoFit: true, autoMinPx: 23f, alignLeft: true, wrap: true,
                 autoMaxPx: 37.400001525878906f, basePx: 36f, face: TmpFont.Face.LatinUi);
            var rt = Node(al, "Alliance Rating Display", new PxRect(AlRtL, AlRtT, AlRtR, AlRtB));
            // `Secondary Icon`（出厂 F · 无 ratingIcon 时关）+ `Main Icon` + 数值
            var si = Rect(rt, ArtRankIcon, new PxRect(AlRtL, AlSicT, AlRtL + 44.4f, AlSicB), "Secondary Icon", L_Art, null, true);
            if (si != null) si.gameObject.SetActive(false);
            Rect(rt, ArtRankIcon, new PxRect(AlRtL, AlMicT, AlRtL + 49.31f, AlMicB), "Main Icon", L_Art, null, true);
            // 🔴 **A406**：`…/Alliance Rating Display/Individual rating value` 原版
            //   `字号=37.4 基准=**24.0** auto[18.0~**40.0**]` ⇒ 上限 **40.0**（我们原来拿 37.4 当上限）·
            //   base **24.0**（⚠️ 这一族的 base 不是 36，别按族统一填）。
            Text(rt, Dash6, new PxRect(AlRtL + 49.31f, AlRtT, AlRtR, AlRtB), Color.white, "Individual rating value",
                 AlRtPx, L_Text, autoFit: true, autoMinPx: AlRtAutoMin, alignLeft: true, wrap: true,
                 autoMaxPx: 40f, basePx: 24f, face: TmpFont.Face.LatinCardBody);
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
            // 🔴 **A406**：`Info Section without Alliance/Name and Title Holder/NameHolder/Player Name` 原版
            //   `auto[23.0~**45.0**] 基准=35.0` ⇒ 上限 **45.0**（我们原来拿 38.6 当上限）· base 35.0。
            //   ⚠️ 与 with-Alliance 那一份**同为 45/35**（本页两棵同值）；但 `Ranking Tab` 那一棵也同值 —— 逐棵核过。
            var woNameR = new PxRect(PnL, PnT, PnR, PnB);
            _playerName = Text(nh, ProfileData.PlayerName, woNameR, NameGold, "Player Name",
                               PnPx, L_Text, autoFit: true, autoMinPx: PnAutoMin, alignLeft: true, wrap: false,
                               autoMaxPx: 45f, basePx: 35f, face: TmpFont.Face.LatinUi);
            if (_playerName != null) { _playerName.SetWrapping(false); MenuDraw.AlignLeft(_playerName, woNameR); }
            // 🆕 **2026-10-16（A712 阶段 2）**：同上（`without-Alliance` 那一棵的 `Player Name`，同一档）。
            MenuDraw.SetVAlign(_playerName, Label.VAlign.Midline, woNameR);
            // 🔴 **A406**：同树 `Player Title` 原版 `auto[20.0~35.0] 基准=35.0` ⇒ 上限 35 · base 35.0。
            var woTitleR = new PxRect(PtL, PtT, PtR, PtB);
            // 🆕 **2026-10-22（P1）**：同上面 with-Alliance 那一棵 —— 印**已装备的称号**（`ProfileData.TitleName`），
            //   不再印 prefab 占位串 `'Player Title'`（参考图那一行就是称号）。
            var woTitleLb = Text(nth, ProfileData.TitleName, woTitleR, Color.white, "Player Title",
                                 PtPx, L_Text2, autoFit: true, autoMinPx: PtAutoMin, alignLeft: true, wrap: false,
                                 autoMaxPx: 35f, basePx: 35f, face: TmpFont.Face.LatinUi);
            if (woTitleLb != null) { woTitleLb.SetWrapping(false); MenuDraw.AlignLeft(woTitleLb, woTitleR); }
            _playerTitle = woTitleLb;
        }

        // ============================================================ 3) `Ranking`

        void BuildRanking()
        {
            var rk = Node("Ranking", new PxRect(RkL, RkT, RkR, RkB));

            // 段位名（官方词条 `Ranked/Division/<演示号>`）+ 段位大图（`DivisionArtName(<演示号>)`）：
            // **两张卡共用同一个演示段位** —— 原版这两格是「当前段位」与「历史最高段位」，本地两条都没有；
            // 取同一个演示号是**我们挑的**（用户 2026-09-26 口径「数据和排名可以空着」，而
            // 2026-10-24 用户又点名要段位徽章 ⇒ 走演示值）。⛔ 别在这里另建第二张「号 → 图/名」表。
            _rankDivisionName = RankedDivisionInfo.DivisionName(RankedDivisionInfo.DemoDivisionNumber);
            DivisionArtDrawn = 0; DivisionArtUsed = null;   // 重进 `Build` 时计数从 0 起（自检读口不能累加）

            // `Current Rank`：`MainRating` 出厂 **T**（`displaySeals=1`）⇒ 带四个封印格
            BuildRankCard(rk, "Current Rank", CrL, CrT, CrR, CrB, CrCtL, CrCtT, CrCtR, CrCtB,
                          CrTtL, CrTtT, CrTtR, CrTtB, TermOr(RankTermCurrent, "Current Rank"),
                          CrBgL, CrBgT, CrBgR, CrBgB,
                          CrDtL, CrDtR, DvPx,
                          CrFtL, CrFtT, CrFtR, CrFtB, CrMrL, CrMrT, CrMrR, CrMrB,
                          CrGlL, CrGlT, CrGlR, CrGlB, CrMsL, CrMsT, CrMsR, CrMsB,
                          CrDivR, ArtCardRedSimple, RedSimpleBorder, true);

            // `Highest Rank`：`MainRating` 出厂 **F**（`displaySeals=0`）⇒ 不建那四个格子
            var hrContent = BuildRankCard(rk, "Highest Rank", HrL, CrT, HrR, CrB, HrCtL, HrCtT, HrCtR, HrCtB,
                          HrTtL, HrTtT, HrTtR, HrTtB, TermOr(RankTermHighest, "Highest Rank"),
                          HrBgL, HrBgT, HrBgR, HrBgB,
                          HrDtL, HrDtR, DvPxHighest,
                          HrFtL, HrFtT, HrFtR, HrFtB, HrMrL, HrMrT, HrMrR, HrMrB,
                          HrGlL, HrGlT, HrGlR, HrGlB, 0f, 0f, 0f, 0f,
                          HrDivR, ArtCardRed, RedBorder, false);

            // 🆕 2026-10-24（E12 · P0-2）：非传奇支里 `Highest Rank` **还带「传奇点数」那一行**
            //   （`legendary title` + `Legendarey Counter`）—— 我们原来没建（见常量区那一段判据）。
            BuildLegendaryPointsRow(hrContent);

            BuildLegendaryCard(rk);

            // 🔴 **2026-10-24（`E10`）就地订正（铁律 5）**：这句原来写「**段位** / 评分 / 封印 / 荣誉**全是**服务器的
            //   ⇒ 整块空态」—— **「段位」那一半已过期**：段位**图**与段位**名**现在按演示段位号画
            //   （`DivArtSideRatio` / `DrawDivisionArt` / `_rankDivisionName` 三处的 doc 里写着「哪些是原版值、
            //   哪些是我们挑的」）。评分 / 封印 / 里程碑 / 荣誉仍然一条数据都没有 ⇒ 仍旧空态。
            Debug.Log("[Profile] `Ranking`：段位**图 + 名**按演示段位号 "
                    + RankedDivisionInfo.DemoDivisionNumber + " 画（**号是我们挑的演示值**，真值在服务器）；"
                    + "评分 / 封印 / 里程碑 / 荣誉仍然空态；按判据 ② 只走**非传奇支**"
                    + "（`Legendary Display Profile` 关）。");
        }

        /// <summary>点 `Ranking/{Current,Highest} Rank/LeaderboardButton` ⇒ 开**经典榜**
        /// （`RankedClassicLeaderboardPopup Variant`）。
        /// <para>🔴 **判据如实标注**：原版这两颗钮**没有接线** —— `m_OnClick.m_Calls` 空，且全量反编译里
        /// `LeaderboardButton` 只有三处（`RankedEventWindow` / `RankedEventWindowV2` / `AlliancesEventScorePanel`，
        /// 全是**别处**的同名钮）；本页 `ProfileRankingSection.Initialize` 只 SetActive 那三个 display。
        /// ⇒ 动作**是我们按同名钮的用途挑的**（同 `Shell/RankedEventWindow.cs` 那颗 `LeaderboardButton` →
        /// `LeaderboardKindForMode`；本页没有模式上下文 ⇒ 取 **Classic**），⛔ 别写成「原版行为」。</para></summary>
        void OpenLeaderboard()
        {
            var mgr = Win != null ? Win.Manager : null;
            if (mgr == null)
            {
                Debug.LogWarning("[Profile] `LeaderboardButton`：拿不到 `WindowsManager` ⇒ 榜开不出来（不许静默）");
                return;
            }
            // 复用同一扇（同 `RankedEventWindow.OpenLeaderboard` 的口径：`OpenByRef` 命中就复用，⛔ 不叠窗）
            WindowsManager.OpenByRef(LeaderboardWindow.NameOf(LeaderboardKind.Classic),
                                     m => LeaderboardWindow.Create(m, LeaderboardKind.Classic));
            Debug.Log("[Profile] `LeaderboardButton` ⇒ 开 `" + LeaderboardWindow.NameOf(LeaderboardKind.Classic) + "`"
                    + "（⚠️ 动作是我们的判断：原版这两颗无接线，见本方法 doc）");
        }

        /// <summary>一张 `RankingDisplay` 卡（`Current Rank` 与 `Highest Rank` 同构，只有几个数不同）。
        /// 🔴 两份的**互斥参数**（§A.1）：`Current Rank` 的 `MainRating` 出厂 **T**（`displaySeals=1`）、
        /// `Highest Rank` 的出厂 **F**（`displaySeals=0`）—— `Mission Milestones Progress` 只在 `MainRating` 下面。
        /// <para>🆕 2026-10-24（E12）：**返回 `Content` 那棵**，好让 `Highest Rank` 再挂自己的
        /// 「传奇点数」两件（P0-2）。</para></summary>
        Transform BuildRankCard(Transform parent, string name, float l, float t, float r, float b,
                           float ctL, float ctT, float ctR, float ctB,
                           float ttL, float ttT, float ttR, float ttB, string title,
                           float bgL, float bgT, float bgR, float bgB, float dtL, float dtR, float dvPx,
                           float ftL, float ftT, float ftR, float ftB,
                           float mrL, float mrT, float mrR, float mrB,
                           float glL, float glT, float glR, float glB,
                           float msL, float msT, float msR, float msB,
                           PxRect divR,
                           string cardArt, Vector4 cardBorder, bool sealsOn)
        {
            var card = Node(parent, name, new PxRect(l, t, r, b));
            Nine(card, cardArt, new PxRect(l, t, r, b), cardBorder, "Generic Window Red Background Small", L_Bg);

            // `LeaderboardButton`（出厂 **F** · 全包反查无字段引用 —— `资料/普查产出_0927/档案窗_Profile页.md` §A.1 / §C·10）。
            // 🔴 **2026-10-18（音效缺口批）：rect 改成【原版逐值】**（原来按卡片顶部对齐摆一小块，「只求结构在」）。
            //   原版实读（`档案窗_Profile页.md` §A 表，两棵各一行）：
            //     · `Current Rank/LeaderboardButton` = **418.18,189.56 → 709.13,242.79**（290.95×53.23）
            //     · `Highest Rank/LeaderboardButton` = **811.95,189.56 → 1102.90,242.79**（同尺寸）
            //   ⇒ 两张卡**各自水平居中**（偏移 = (卡宽 − 290.95)/2：Current 67.155 · Highest 34.225）、
            //     **y 恒 189.56..242.79**（在卡片**之上**那一条空带里 —— 不是「模板位」，两棵同 y 就是作者态）。
            //   `Button Text` = 431.51,194.78→694.86,237.58（框内缩 13.33 / 5.22 / 14.27 / 5.21，两棵同值）。
            float lbCx = (l + r) * 0.5f;
            var lbR = new PxRect(lbCx - 290.9464f * 0.5f, 189.56f, lbCx + 290.9464f * 0.5f, 242.79f);
            var lb = Node(card, "LeaderboardButton", lbR);
            var lbBg = Nine(lb, ArtMulligan, lbR, MulliganBorder, "Image", L_Frame);
            // 🔴 **2026-10-07（A62 子表 A · A12）**：原版 `Button Text`（`'Leaderboard'`）= **`折行=0`**
            //   （判据 = `md "Player Profile Window" --depth 25 --md`，该行 `折行=0 auto[10~36]`）⇒ 关掉。
            // 🔴 **A406**：`Ranking/{Current,Highest} Rank/LeaderboardButton/Button Text` 原版
            //   `auto[10.0~36.0] 基准=12.0` ⇒ 上限 36（= 标称）· base **12.0**（两棵同值，逐棵读过）。
            var lbTxt = Text(lb, LeaderboardLabel, new PxRect(lbR.x1 + 13.33f, lbR.y1 + 5.22f, lbR.x2 - 14.27f, lbR.y2 - 5.21f),
                             Color.white, "Button Text", 36f, L_Text2, autoFit: true, autoMinPx: 10f, alignLeft: false, wrap: false,
                             autoMaxPx: 36f, basePx: 12f, face: TmpFont.Face.LatinCardBody);
            if (lbTxt != null) lbTxt.SetWrapping(false);
            // 🆕 **2026-10-18（音效线转来的界面缺口）**：补**命中区 + 点击音**。
            //   原版值（`bundle_menus_assets_all` MB 实读，两棵各一份、逐值相同）：
            //     `EverguildButton.m_onClickSound` = pid **8549838806721213354** = **`ButtonClick Positive`**
            //     · `useDefaultAudioClipIfNull = 1` · `m_Interactable = 1` · `m_OnClick.m_Calls` **空**
            //     （Current 那颗 MB `3930931442171673138` · Highest 那颗 MB `-2929059975558170062`）。
            //   ⚠️ **原版这两颗是 `m_IsActive = 0`**（prefab 实读；`档案窗_Profile页.md` §C·10 如实记着
            //     「**查不到激活点**」—— 搜过 `SetActive` / `set_enabled` / `Transform.Find` / `GetChild` /
            //     `GetComponentsInChildren` + 全包组件反向引用扫描，这些节点只被 `m_Children`/`m_Father` 引用）
            //     ⇒ **我们也不擅自激活**（激活 = 多出一颗原版运行时看不到的钮）。命中与音先接好：
            //     哪天真找到激活点，一颗 `SetActive(true)` 就能用。
            //   动作**判据缺失**（原版无接线）：按**同名钮**的用途接**经典榜**（同 `Shell/RankedEventWindow.cs`）。
            var lbQ = lbBg != null ? lbBg.GetComponentInChildren<ImageQuad>() : null;
            Hit(lb, "Hit", lbR, L_Hit, OpenLeaderboard, lbQ, ArtMulligan, clickCue: WindowSounds.PositiveCue);
            lb.gameObject.SetActive(false);

            var ct = Node(card, "Content", new PxRect(ctL, ctT, ctR, ctB));
            // 🔴 **A62 · A13**：`Title`（`'Current Rank'`）原版 `折行=0 auto[18~40]` ⇒ 关掉。
            // 🔴 **A406**：`Ranking/{Current,Highest} Rank/Content/Title` 原版
            //   `auto[18.0~40.0] 基准=23.0` ⇒ 上限 40（= 标称）· base **23.0**（`Current Rank`/`Highest Rank` 两棵同值）。
            var ctTitle = Text(ct, title, new PxRect(ttL, ttT, ttR, ttB), RankInk, "Title",
                               RkTitlePx, L_Title, autoFit: true, autoMinPx: RkTitleAutoMin, alignLeft: false, wrap: false,
                               autoMaxPx: 40f, basePx: 23f, face: TmpFont.Face.LatinUi);
            if (ctTitle != null) ctTitle.SetWrapping(false);

            // `RankTitleBG`（`40K_main_rank_display` · Simple · a=0.918）> `DivisionText`
            Rect(ct, ArtRankBg, new PxRect(bgL, bgT, bgR, bgB), "RankTitleBG", L_Bg2,
                 new Color(1f, 1f, 1f, RankBgA));
            // `DivisionText`：段位名。🔴 **2026-10-24（`E10`）就地订正（铁律 5）**：
            //   原来这一格恒印空串（`DivisionEmpty`），理由是「段位全是服务器数据、不编」。那条**已过期** ——
            //   同一批（`A1` + `E19`）已经把「号 → 图」与「号 → 官方词条」落进 `RankedDivisionInfo`，
            //   排位窗/排名页都按**演示段位号 7** 画了 ⇒ 本页再留空就会「两个窗口显示的段位不一样」。
            //   用户实拍 `账户-个人资料参考图.png` 这两格也印着段位名（那边是「金 II」= 段位名 + 名次数字）。
            //   ⚠️ **名次（罗马数字）仍不印**：名次号在服务器（`RankImage` 也仍关着，见下）。
            // 🔴 **A62 · A14**：`DivisionText`（`'Division V'`）原版 `折行=0 auto[18~36]` ⇒ 关掉。
            // 🔴 **A406**：`Ranking/{Current,Highest} Rank/Content/RankTitleBG/DivisionText` 原版
            //   `auto[18.0~36.0] 基准=23.0` ⇒ 上限 **36**（两棵同值）· base **23.0**。
            //   ⚠️ 两棵的 `m_fontSize` **不同**（`Current` 36.0 / `Highest` 31.35 —— 本文件 `DvPx`/`DvPxHighest`），
            //      但 **上限与 base 逐位相同** —— 逐棵读过才敢这么填（铁律 5·c）。
            var ctDiv = Text(ct, _rankDivisionName, new PxRect(dtL, bgT, dtR, bgB), RankInk, "DivisionText",
                             dvPx, L_Title, autoFit: true, autoMinPx: DvAutoMin, alignLeft: false, wrap: false,
                             autoMaxPx: 36f, basePx: 23f, face: TmpFont.Face.LatinUi);
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
            // 🔴 **A406**：`Ranking/{Current,Highest} Rank/Content/Timer/Timer` 原版
            //   `auto[18.0~32.0] 基准=**50.0**` ⇒ 上限 32（= 标称）· base **50.0**（两棵同值）。
            var tmTx = Text(tm, "", new PxRect(ctL - 64.40f, ctB - 25.45f, ctL + 103.24f, ctB + 30.45f), Color.white,
                            "Timer", 32f, L_Text, autoFit: true, autoMinPx: 18f, alignLeft: false, wrap: false,
                            autoMaxPx: 32f, basePx: 50f, face: TmpFont.Face.LatinCardBody);
            if (tmTx != null) tmTx.SetWrapping(false);

            // `DivisionImage`：**槽的矩形照 prefab 真值**（见 `CrDivR`/`HrDivR` 的 doc —— 宽度 390.92 是字段值、
            // 高度 0.00 是「布局组 + 图还没喂」的伪值，所以这里把槽建成**零高**、图由 `DrawDivisionArt` 画）。
            // 🔴 **2026-10-24（`E10`）**：原来这里写死 `ctL−9.19 / ctR+9.18`（只对 `Current Rank` 成立，
            //    `Highest Rank` 的真实溢出是两侧各 37.89 ⇒ 会窄 57.4）—— 已换成逐卡传入的 `divR`。
            var di = Node(ct, "DivisionImage", divR);
            // `RankImage`（罗马数字名次，128²）**仍关着**：名次号也在服务器，且「最高段位叠不叠名次」
            // 参考图判不了（S4 §缺数据）。矩形照 prefab（那一格本身是负高，原样保留、只是不出图）。
            // 🔴 **2026-10-24（第七十三会话 E12 · 账 `A1 B·3b` 同族）**：同文件下面 `legendary display` 那颗
            //   （`var ri = Node(di, "RankImage", …)` 后紧跟 `SetActive(false)`）、`RankedTab.cs:440-441`、
            //   `RankedDivisionInfo.cs` 的 `RankImage` **都关着**，只有这一颗漏了 ——
            //   「不出图」靠「没挂 `ImageQuad`」凑不住（`activeSelf` 仍是真的）。
            var riCur = Node(di, "RankImage", new PxRect(dtL + 138.61f, bgB + 14f, dtL + 216.79f, bgB + 6f));
            riCur.gameObject.SetActive(false);
            DrawDivisionArt(di, divR, r - l);

            var ft = Node(ct, "footer", new PxRect(ftL, ftT, ftR, ftB));
            // `Highest Faction Rating`（出厂 F · 无字段引用 —— §A.1）⇒ 关
            var hf = Node(ft, "Highest Faction Rating",
                          new PxRect(ftL - 177.72f, ftT + 160.10f, ftR - 232.28f, ftT + 199.90f));
            hf.gameObject.SetActive(false);
            // `MainRating`（逐卡 T/F）> `Mission Milestones Progress` / `Global Rating`
            // 🔴 **2026-10-24（U2 · 父链归真）**：`Global Rating` 原版挂在 **`MainRating` 下面**
            //   （`…/Content/footer/MainRating/Global Rating`，`menu_dump` 现读），**不是** `footer` 的直接子件。
            //   改前它建在 `footer` 上 ⇒ `Highest Rank` 那句 `MainRating.SetActive(false)`（出厂 F）**关不到它**
            //   ⇒ 实拍 `09_档案窗_Profile页.png` 里「最高排名」卡**左外侧**浮着一颗小图标 + 一条破折号
            //   （用户参考图那张卡只有「传奇点数 ⚙ 0」，没有这一行）—— 这就是那个残件的来源。
            //   ⇒ 现在整块连 `Global Rating` 一起受 `MainRating` 的开关管。
            var mr = Node(ft, "MainRating", new PxRect(mrL, mrT, mrR, mrB));
            if (sealsOn)
            {
                var ms = Node(mr, "Mission Milestones Progress", new PxRect(msL, msT, msR, msB));
                var steps = Node(ms, "steps", new PxRect(msL, msT, msR, msB));
                for (int i = 0; i < 4; i++)
                {
                    float x = msL + SealStep * i;
                    // 🔴 **2026-10-24（U2）就地订正（铁律 5）**：这里原来写「原版这四格是 `Rank Skull` +
                    //   **色 (1,1,1,0)（透明，出厂本来就看不见）**」—— **那条判据读错了对象**（铁律 5·c：
                    //   一个值 ≠ 全部情况）：
                    //   · `(1,1,1,0)` 是**根** `RankedSealStep` 自己那颗 `Image`（它**没有 sprite**、只是容器）；
                    //   · 真正上屏的是它的两个子件 —— `Empty` = sprite **`Rank Skull Empty`**
                    //     （pid -7214020762604782334）· `Fill` = sprite **`Rank Skull`**（pid -300467167438017286），
                    //     两颗的 `m_Color` **都是 (1,1,1,1)**（判据 = `menu_dump` 该两行的「贴图模式/颜色」列；
                    //     pid→名 = `_tmp_view/sprite_pids_ALL.json`）。
                    //   ⇒ 用户实拍 `账户-个人资料参考图.png` 里「当前排名」卡底部**确实有一排灰色封印**（未达成态），
                    //     所以「出厂看不见」是错的。
                    //   ⛔ **仍然没画**，但理由换成**几何**：我们摆 footer 用的是「段位大图还没喂」那一版布局快照
                    //     （prefab 里 `DivisionImage` 高 = 0.00）⇒ footer 停在卡片**中部**（457.7），
                    //     照 prefab 坐标画封印会**压在那张 306px 的段位大图上**。原版那是个 `VerticalLayoutGroup`
                    //     （`Content`，`ctrlH=1`）：大图一旦有真实高度（= 卡宽 × 0.72），footer 会被推到卡片**底部**
                    //     （≈763.7–829.1）——用户实拍那排封印就落在 775–826，与这条推算吻合。
                    //   ⇒ 修法 = 先把 footer 按 VLG 重算到卡底、再画 4 颗 `Rank Skull Empty`；
                    //     ⚠️ 用户实拍那一版是 **5 颗、横贯整卡**（与 prefab 的 4 颗不同 = 又一个版本差）⇒ 颗数以谁为准待裁定。
                    Node(steps, i == 0 ? "RankedSealStep" : "RankedSealStep (" + (i + 1) + ")",
                         new PxRect(x, msT + 13.09f, x + SealW, msT + 13.09f + SealH));
                }
            }
            var gl = Node(mr, "Global Rating", new PxRect(glL, glT, glR, glB));
            // `Secondary Icon`（出厂 F）+ `Main Icon` + 数值
            var gs = Rect(gl, ArtRankIcon, new PxRect(glL, glB, glL, glB), "Secondary Icon", L_Art, null, true);
            if (gs != null) gs.gameObject.SetActive(false);
            Rect(gl, ArtRankIcon, new PxRect(glL, glT - 14.84f, glL + 60f, glT + 30.16f), "Main Icon", L_Art, null, true);
            // 🔴 **A406**：`Ranking/{Current,Highest} Rank/Content/footer/MainRating/Global Rating/Individual rating value`
            //   原版 `auto[18.0~40.0] 基准=31.3799991607666` ⇒ 上限 40（= 标称）· base **31.38**
            //   （⚠️ 两颗的 `m_fontSize` 不同：`Current` 40.0 / `Highest` 36.3 —— 上限与 base 仍逐位相同）。
            Text(gl, Dash6, new PxRect(glL + 60f, glT, glR, glB), Color.white, "Individual rating value",
                 RvPx, L_Text, autoFit: true, autoMinPx: RvAutoMin, alignLeft: true, wrap: true,
                 autoMaxPx: 40f, basePx: 31.3799991607666f, face: TmpFont.Face.LatinCardBody);
            // 🔴 **2026-10-24（U2 · 父链归真）**：`MainRating` 出厂 **F** 的那一份（`Highest Rank`，`displaySeals=0`）
            //   ⇒ **整块关**（连 `Global Rating` 一起关）。这句**必须放在 `Global Rating` 建完之后** ——
            //   改前 `Global Rating` 建在 `footer` 上、不归 `MainRating` 管 ⇒ 关不掉
            //   （症状 = 实拍里「最高排名」卡左外侧那颗小图标 + 破折号）。判据 =
            //   `Ranking Display` 的 `displaySeals` 逐卡 T/F（`ProfileTab.cs` 的 `BuildRankCard(… sealsOn)` 实参）。
            if (!sealsOn) mr.gameObject.SetActive(false);
            return ct;
        }

        /// <summary>把**段位大图**画进 `DivisionImage` 那一格（两张排名卡各调一次）。
        /// <para>🔴 **2026-10-24（`E10`）**：改前这两格**只有结构、不画图**，注释理由是「段位图那一族我们
        /// 没有段位数据，画出来就是编一个段位」—— 那条**已过期**：`RankedDivisionInfo.DivisionArt`
        /// 已有两档实测钉死的映射（4/7，判据 → `资料/普查产出_1024c/A1_段位图案映射.md`），
        /// 排位窗与排名页都已按它画 ⇒ 本页只是**接上同一份判据**（⛔ 不在这里另建第二张表）。</para>
        /// <para>🔴 **几何**（三句话，判据都在 `CrDivR`/`HrDivR`/`DivArtSideRatio` 的 doc 里）：
        /// ① **边长 = 卡宽 × 0.72**（实拍反算，见 `DivArtSideRatio` —— 两张卡因此**不等大**，与实拍一致）；
        /// ② **横向中心 = 槽的中心**（Current 562.04 / Highest 955.84 —— ⛔ 不是屏幕中线 960，
        ///    传奇卡那一支写死 960 是因为那张卡本来就居中，两张排名卡**不是**）；
        /// ③ **上沿 = 槽的 y**（VLG 里那颗零高槽的 `y` 就是槽的上沿 ⇒ 图往下长；实拍量到的图顶
        ///    比槽高 ~10px，差在这一档的可接受范围）。</para>
        /// <para>⚠️ **压暗**：用户实拍里「最高排名」那张明显更暗，但**是恒压暗还是别的机制没查清**
        /// （`A1` §五·4 如实记着）⇒ 两张都按 **prefab `m_Color` 字段值 (1,1,1,1)、不压暗**
        /// —— 「按左图同参数」是**我们挑的**，⛔ 别自己编一个 alpha。传奇卡那支的 `a=0.306` 是
        /// **那颗节点自己的**字段值，⛔ 不要套到这里（同族不同站，`RankedDivisionInfo` 里那条
        /// 「四处的 a 各不相同、别统一」就是这一条）。</para></summary>
        void DrawDivisionArt(Transform di, PxRect slot, float cardW)
        {
            string artName = RankedDivisionInfo.DivisionArtName(RankedDivisionInfo.DemoDivisionNumber);
            if (string.IsNullOrEmpty(artName))
            {
                Debug.LogWarning("[Profile] 段位大图**没画** —— `RankedDivisionInfo.DivisionArtName("
                               + RankedDivisionInfo.DemoDivisionNumber + ")` 返回空（表长 "
                               + RankedDivisionInfo.DivisionArtCount + "）；⛔ 不拿别的段位顶替");
                return;
            }
            var tex = Art(artName);
            if (tex == null)
            {
                Debug.LogWarning("[Profile] 段位大图**没画** —— 图 `" + artName + "` 取不到"
                               + "（`Resources/Art/ui_menu/` 里没有？）；⛔ 不拿别的段位顶替");
                return;
            }
            float side = cardW * DivArtSideRatio, cx = slot.CX;
            var quadR = new PxRect(cx - side * 0.5f, slot.y1, cx + side * 0.5f, slot.y1 + side);
            // 队列 `Q + L_Bg2`(1)：压在卡片红底(0)之上、页内所有字(≥4)与 footer 的图标(2)之下
            // ⇒ 与原版兄弟序（`DivisionImage` 在 `footer` 之前）一致。
            var q = MenuDraw.Rect(di, tex, quadR, "Image", Q + L_Bg2, null, true);
            if (q == null)
            {
                Debug.LogWarning("[Profile] 段位大图**没画** —— `MenuDraw.Rect` 返回 null（图 `" + artName + "`）");
                return;
            }
            DivisionArtUsed = artName;
            DivisionArtDrawn++;
            Debug.Log("[Profile] 段位大图：`" + artName + "`（演示段位号 " + RankedDivisionInfo.DemoDivisionNumber
                    + "，**我们挑的演示值**）画在 (" + quadR.x1.ToString("F2") + "," + quadR.y1.ToString("F2")
                    + ")→(" + quadR.x2.ToString("F2") + "," + quadR.y2.ToString("F2") + ") 边长 "
                    + side.ToString("F2") + "；⚠️ 边长 = 卡宽×" + DivArtSideRatio.ToString("F2")
                    + "（实拍反算，**我们挑的**）、不压暗（见本方法 doc）");
        }

        /// <summary>`Highest Rank` 卡里的「传奇点数」那一行：`legendary title` + `Legendarey Counter`。
        /// 判据/坐标 → 本文件常量区 `HrLgT/Htt`、`HrLcT/Hcb` 那两段的长注释（含 `--no-layout` 的理由）。
        /// <para>🔴 原版拼写是 **`Legendarey`**（`a` 不是 `e`）—— 节点名照抄，别顺手「修正」，
        /// 否则形态与 `资料/普查产出_0927/档案窗_Profile页.md` §A 对不上（同 `Warlord  Mastery` 的双空格）。</para></summary>
        void BuildLegendaryPointsRow(Transform content)
        {
            if (content == null)
            {
                Debug.LogWarning("[Profile] `Highest Rank/Content` 没建出来 ⇒ 「传奇点数」那一行也建不了（不许静默）");
                return;
            }
            // 标题：官方词条 `PlayerProfile/LegendaryRank` = `Legendary Points` / `传奇点数`（参考图那格就是中文）。
            // ⚠️ 这条词条**不是**自拟：prefab 那颗 TMP 的 `m_text` 逐字就是 `'Legendary Points'`。
            var lgdR = new PxRect(HrLgL, HrLgT, HrLgR, HrLgB);
            var lgdLb = Text(content, TermOr(LegendaryRankTerm, LegendaryPointsLabel), lgdR, RankInk, "legendary title",
                             28f, L_Title, autoFit: true, autoMinPx: 18f, alignLeft: false, wrap: false,
                             autoMaxPx: 28f, basePx: 23f, face: TmpFont.Face.LatinUi);
            if (lgdLb != null) lgdLb.SetWrapping(false);

            var cn = Node(content, "Legendarey Counter", new PxRect(HrLcL, HrLcT, HrLcR, HrLcB));
            // ⛔ **不画那块 332.80×159.81 的底图**：原版它的 sprite `<未解出 3837025018791853890>`（本地无图）⇒
            //    只建节点（同 `Badge` 的处置），并出声。⛔ 别用 `Rect(cn, null, …)` —— 那会落到白图。
            Debug.Log("[Profile] `Highest Rank/Legendarey Counter` 的底图：原版 `m_Sprite` = "
                    + "`<未解出 3837025018791853890>`（本地没有对应图）⇒ 只建节点、不画底图");

            var rtR = new PxRect(HrLcTL, HrLcTT, HrLcTR, HrLcTB);
            var rt = Node(cn, "Rating Text", rtR);
            // `Secondary Icon`（出厂 F · 无 ratingIcon）+ `Main Icon` + 值
            var si = Rect(rt, ArtRankIcon, new PxRect(HrLcTL, HrLcTB, HrLcTL, HrLcTB), "Secondary Icon", L_Art, null, true);
            if (si != null) si.gameObject.SetActive(false);
            Rect(rt, ArtRankIcon, new PxRect(HrLcIcL, HrLcIcT, HrLcIcR, HrLcIcB), "Main Icon", L_Art, null, true);
            var valR = new PxRect(HrLcValL, HrLcTT, HrLcTR, HrLcTB);
            var val = Text(rt, LegendaryPointsValue, valR, Color.white, "Individual rating value",
                           HrLcValPx, L_Text, autoFit: true, autoMinPx: HrLcValAutoMin, alignLeft: true, wrap: true,
                           autoMaxPx: 32f, basePx: 31.3799991607666f, face: TmpFont.Face.LatinCardBody);
            // 原版这一格是 `Left/**Midline**`（dump 那一行），`Text(...)` 只表达左右 ⇒ 纵向另接一次。
            MenuDraw.SetVAlign(val, Label.VAlign.Midline, valR);
        }

        /// <summary>`Legendary Display Profile`（传奇支）—— **出厂 T，但与 `Highest Rank` 互斥**（判据 ②）。
        /// 我们没有段位数据 ⇒ 走非传奇支、**整卡关掉**；结构照预制体建（含三档奖杯行），供以后接线。</summary>
        void BuildLegendaryCard(Transform parent)
        {
            var card = Node(parent, "Legendary Display Profile", new PxRect(HrL, CrT, HrR, CrB));
            Nine(card, ArtCardRed, new PxRect(HrL, CrT, HrR, CrB), RedBorder,
                 "Generic Window Red Background Small", L_Bg);

            var ct = Node(card, "Content", new PxRect(LgCtL, LgCtT, LgCtR, LgCtB));
            // 🔴 **2026-10-24（`U2` V6）就地改挂点（铁律 5）**：
            //   改前 = `Node(ct, "DivisionImage", 槽)` + 自造子件 **`Body`** 上再挂那张 quad ——
            //   而原版**全树没有 `Body`** 这个节点：那张 `07-Legend` 的 `Image` **就长在 `DivisionImage`
            //   自己身上**（判据 → `资料/普查产出_0927/档案窗_Profile页.md` §A 第 119 行：
            //   `Legendary Display Profile/Content/DivisionImage` = `760.37,340.49 → 1151.30,671.45`
            //   （390.92×330.96）· 图 `07-Legend 512×512` · `Simple + preserveAspect` · 色 (1,1,1,**0.306**) · ppu=50）。
            //   ⇒ 现在**节点本身就是那颗 quad**（`MenuDraw.Rect` 的返回物直接当节点用 —— 同
            //   `PlayerProfileWindow.BuildCloseButton` 里那颗 `Generic Close Button Orange` 的先例：
            //   原版「图长在根节点自己身上」时，我们就把 quad 节点当那个节点）。
            // ⚠️ **尺寸也跟着归真**：`preserveAspect` 的语义是「**内接进 RectTransform**」，**与 ppu 无关**
            //   ⇒ 512² 方图内接进 390.92×330.96 = **330.96² 居中**（x 790.36..1121.32 · y 340.49..671.45）。
            //   改前画的是 **781.84²**（= `390.92 × 2`，理由是「ppu=50 ⇒ ×2」）—— 那个 ×2 只在
            //   `SetNativeSize` 那一档成立；`preserveAspect` 下**尺寸由框决定** ⇒ 那张图原来大了一倍多、
            //   还以屏心 **960** 居中（槽心其实是 **955.83**）。
            //   ⚠️ 本处**没有原版实拍可对**（传奇支整卡出厂关，见方法头）⇒ 判据 = uGUI
            //   `Image.PreserveSpriteAspectRatio` 的语义 + prefab 自己的 rect，**不是**我们挑的。
            var divR = new PxRect(LgCtL - 37.89f, LgDiT, LgCtR + 37.89f, LgDiB);
            var tex = Art(ArtLegend);
            Transform di;
            if (tex != null)
            {
                var diQ = MenuDraw.Rect(ct, tex, divR, "DivisionImage", Q + L_Art,
                                        new Color(1f, 1f, 1f, LegendArtA), true);
                di = diQ != null ? diQ.transform : Node(ct, "DivisionImage", divR);
            }
            else
            {
                Debug.LogWarning("[Profile] 传奇支的段位大图**没画** —— `" + ArtLegend + "` 取不到"
                               + "（只建 `DivisionImage` 节点；⛔ 不留白块）");
                di = Node(ct, "DivisionImage", divR);
            }
            var ri = Node(di, "RankImage", new PxRect(916.74f, LgDiT + 107.29f, 994.93f, LgDiT + 132.39f));
            ri.gameObject.SetActive(false);      // 出厂 F（那一份的角色图没填）
            // 🔴 **A62 · A17**：`legendary title`（`'Legendary Points'`）原版 `折行=0 auto[18~28]` ⇒ 关掉。
            // 🔴 **A406**：`Ranking/Legendary Display Profile/Content/legendary title` 原版
            //   `auto[18.0~28.0] 基准=23.0` ⇒ 上限 28（= 标称）· base **23.0**。
            var lgdTx = Text(ct, TermOr(LegendaryRankTerm, LegendaryPointsLabel),
                              new PxRect(HrLgL, LgTtT, HrLgR, LgTtB), RankInk, "legendary title",
                             28f, L_Title, autoFit: true, autoMinPx: 18f, alignLeft: false, wrap: false,
                             autoMaxPx: 28f, basePx: 23f, face: TmpFont.Face.LatinUi);
            if (lgdTx != null) lgdTx.SetWrapping(false);

            var cn = Node(ct, "Legendary Counter", new PxRect(HrLcL, LgCnT, HrLcR, LgCnB));
            Rect(cn, ArtRankBg, new PxRect(HrLcL, LgCnT, HrLcR, LgCnB), "Image", L_Bg2);
            var rt = Node(cn, "Rating Text", new PxRect(LgRtL, LgRtT, LgRtR, LgRtB));
            var s2 = Rect(rt, ArtRankIcon, new PxRect(LgRtL, LgRtB, LgRtL, LgRtB), "Secondary Icon", L_Art, null, true);
            if (s2 != null) s2.gameObject.SetActive(false);
            Rect(rt, ArtRankIcon, new PxRect(LgRtL, LgRtT - 22.56f, LgRtL + 60f, LgRtT + 22.44f), "Main Icon", L_Art, null, true);
            // 🔴 **A406**：`Ranking/Legendary Display Profile/Content/Legendary Counter/Rating Text/Individual rating value`
            //   原版 `auto[18.0~32.0] 基准=31.3799991607666` ⇒ 上限 32（= 标称）· base **31.38**
            //   （⚠️ 与同页 `Global Rating` 那颗**上限不同**：它 32、那颗 40 —— 同名不同站，别按名统一）。
            Text(rt, Dash6, new PxRect(LgRtL + 60f, LgRtT, LgRtR, LgRtB), Color.white,
                 "Individual rating value", 32f, L_Text, autoFit: true, autoMinPx: 18f, alignLeft: true, wrap: true,
                 autoMaxPx: 32f, basePx: 31.3799991607666f, face: TmpFont.Face.LatinCardBody);

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
                //   🔴 **A406（2026-10-12）：这一族【不填】`autoMaxPx`/`basePx`** —— 原版那两颗
                //   `m_enableAutoSizing = 0`（dump 那一行**没有** `auto[…]` 段），`m_fontSizeMax` 根本不参与、
                //   而 `m_fontSizeBase` 因为自适应关着**恒等于 `m_fontSize`（36.0）** ⇒ 这两格对本站**不适用**，
                //   填上去反而是编一个原版没有的语义（如实记，⛔ 不是「没读到」）。
                var vicNumR = new PxRect(861.90f, top + 15.53f, 1081.22f, top + 61.07f);
                var lbVicNum = Text(row, "0", vicNumR, Color.white,
                     "Victories number", 36f, L_Text, autoFit: false, autoMinPx: 0f, alignLeft: false, wrap: true,
                     face: TmpFont.Face.LatinCardBody);
                var vicTxtR = new PxRect(861.90f, top + 49.60f, 1081.22f, top + 99.60f);
                var lbVicTxt = Text(row, TermOr(TrophiesTerm, "Trophies"), vicTxtR, Color.white,
                     "Victories text", 36f, L_Text2, autoFit: false, autoMinPx: 0f, alignLeft: false, wrap: true,
                     face: TmpFont.Face.LatinCardBody);
                // 🆕 **2026-10-16（A712 阶段 2）**：上面 本文件 那句「`对齐=Center/Midline|Capline`」的**逐颗真值**
                //   （`menu_dump … "Player Profile Window" --depth 30 --md` 实读，三份实例逐值相同）：
                //   · `Victories number`（`'0'`）= `Center/**Midline**` · `Victories text`（`'Trophies'`）= `Center/**Capline**`
                //   ⇒ **同框两行、档不同**，⛔ 别一刀切。
                MenuDraw.SetVAlign(lbVicNum, Label.VAlign.Midline, vicNumR);
                MenuDraw.SetVAlign(lbVicTxt, Label.VAlign.Capline, vicTxtR);
            }

            card.gameObject.SetActive(false);       // ← 非传奇支（判据 ②）
        }

        // ============================================================ 4) `Events`

        void BuildEvents()
        {
            var ev = Node("Events", new PxRect(EvL, EvT, EvR, EvB));
            // ⚠️ 节点名里的**两个空格**是原版真值（`Warlord  Mastery Container`），别顺手改成一个
            //
            // 🔴 **`warlord` 这个实参只管【版式】**（`Title` 的 y / 宽 · `Badge` 440 vs 123×104 ·
            //    `ArmyName`/`Level` 的 x），**标题另给**（2026-10-24 E12 · P0-1）。
            // 🔴 **`Forge` 那一格原来传的是 `warlord:true` —— 那是错的**：原版 prefab 里它走的是
            //    「小 Badge」那套（`Title` 1194.38,509.48→1677.43,569.48 = 483.05 宽 · `Badge`
            //    1198.59,560.73→1321.65,665.02 = 123.07×104.28 · `ArmyName` x 1330.87）
            //    ⇒ 与 Campaign 同一套版式、只有底图（Simple `40K_profile_ForgeLevel_bg`）不同。
            //    判据 = `资料/普查产出_0927/档案窗_Profile页.md` §A 第 149-154 行（逐件实读）。
            //    改前它不仅标题错，`Title` 宽（280.40 而不是 483.05）与 `Badge`（440 而不是 123×104）
            //    也跟着错 —— 同一个 flag 承担了两件事的连带后果。
            BuildEventContainer(ev, "Warlord  Mastery Container", EvT, ArtSubmenuBack, true, true,
                                TermOr(EvTermWarlord, EvTitleFallbackWarlord), EvLevelTermWarlord, 14f);
            BuildEventContainer(ev, "Forge Profile Container", EvT + EvStep, ArtForgeBg, false, false,
                                TermOr(EvTermForge, EvTitleFallbackForge), EvLevelTermOther, 13f);
            // 🔴 **2026-10-24（U2）：「当前战役」格的底图不是通用红底。**
            //   判据链（三条独立来源互证）：
            //   ① prefab 那颗 `Campaign Profile Container/Player Profile Container Base` 是 `UI_Deck_Information_submenu_Back`
            //      （Sliced）= **作者态占位**，而 `CampaignProfileContainer.background` 是**运行期字段**
            //      （`ProfileContainerComponents` 的 `armyIcon/armyName/background/level/title` 五件全是运行期填的）；
            //   ② 用户实拍 `账户-个人资料参考图.png` 的「当前战役」格（太空野狼）与
            //      `Resources/Art/ui_menu/40K_profile_campaign_bg_SW.png` **是同一张图**（蓝行星 + 环 + 网格底纹）；
            //   ③ 该族 **13 张全在本地**，与锻造厂那颗 `40K_profile_ForgeLevel_bg` **同尺寸 631×194（3.253:1）**
            //      —— 同族兄弟（一个 forge、一个 campaign），而锻造厂那格我们早就按它画了。
            //   阵营 → 图 走**已有唯一那份表** `PurchasePremiumWindow.ArmyBackgroundArt(int)`（⛔ 不在这里另建第二张，
            //   铁律「两处写同一条规则 = 迟早不一致」）；string→army id 走 `ArmyIcons.Id`（那是 13 阵营的唯一真值表）。
            string campArmy = CampaignData.Selected;
            int campId = ArmyIcons.Id(campArmy);
            string campBg = campId > 0 ? PurchasePremiumWindow.ArmyBackgroundArt(campId) : null;
            if (string.IsNullOrEmpty(campBg))
                Debug.LogWarning("[Profile] 「当前战役」格的阵营底图查不到（army=" + campArmy + " · id=" + campId
                               + "）⇒ **退回通用红底**（⛔ 不静默留白、也不拿别的阵营的图顶替）");
            BuildEventContainer(ev, "Campaign Profile Container", EvT + EvStep * 2f,
                                string.IsNullOrEmpty(campBg) ? ArtSubmenuBack : campBg,
                                string.IsNullOrEmpty(campBg), false,
                                TermOr(EvTermCampaign, EvTitleFallbackCampaign), EvLevelTermOther, 23f);
            Debug.Log("[Profile] `Events` 三格：标题走官方词条（战将精通 / 最高锻造厂等级 / 当前战役）；"
                    + "格里的 ArmyName / 等级·胜利数**原版装的是服务端活动状态** ⇒ 我们留空态"
                    + "（`ArmyName` 空串、`Level` 行印「词条 + `-`」），⛔ 不编数字。");
        }

        /// <summary>一个事件容器（555×175）。`warlord` == true 是 `Warlord  Mastery Container` ——
        /// 它的 `Title` / `ArmyName` / `Level` 的 y 与另外两格**不同**（+13.07 vs +1.77）、
        /// `Badge` 是 440×440（另外两格 123.07×104.28）—— **逐格真值，别统一**。
        /// <para>⚠️ `warlord` **只描述版式**：标题由 `title` 另给、`Level` 行用哪条词条由
        /// `levelFormatKey` 另给（2026-10-24 E12：原来标题复用了这个 flag ⇒ Forge 那格印战将的标题）。</para></summary>
        void BuildEventContainer(Transform parent, string name, float top, string art, bool nine, bool warlord,
                                 string title, string levelFormatKey, float armyAutoMin)
        {
            float l = EvX1, r = EvX2, b = top + EvH;
            var c = Node(parent, name, new PxRect(l, top, r, b));
            if (nine) Nine(c, art, new PxRect(l, top, r, b), SubmenuBorder, "Player Profile Container Base", L_Bg);
            else Rect(c, art, new PxRect(l, top, r, b), "Player Profile Container Base", L_Bg, null, true);

            float tx = warlord ? 1195.12f : 1194.38f;
            float ty = warlord ? top + 13.07f : top + 1.77f;
            // 🔴 **A406**：`Events/{Warlord  Mastery,Forge Profile,Campaign Profile} Container/Title` 三棵
            //   **逐棵读过**：`auto[12.0~25.0]`（Warlord）/ `auto[18.0~25.0]`（Forge · Campaign）· **基准全是 35.0**
            //   ⇒ 上限 **25**（= 标称）· base **35.0**（三棵同 max/base，**min 不同** —— 随 `warlord` 走）。
            Text(c, title,
                 new PxRect(tx, ty, tx + (warlord ? 280.40f : 483.05f), ty + 60f),
                 Color.white, "Title", EvTitlePx, L_Title,
                 autoFit: true, autoMinPx: warlord ? EvTitleAutoMin : EvTitleAutoMinOther,
                 alignLeft: true, wrap: true, autoMaxPx: 25f, basePx: 35f, face: TmpFont.Face.LatinUi);
            // `Badge`：预制体里**没有 sprite**（运行期由活动给图）⇒ 照 `SearchingOpponentWindow` 的先例**只建节点**
            Node(c, "Badge", warlord ? new PxRect(1397.12f, top - 233.32f, 1837.12f, top + 206.68f)
                                     : new PxRect(1198.59f, top + 53.02f, 1321.65f, top + 157.31f));
            // `ArmyName` / `Level`：**数据留空**（ⓒ）—— 只把结构摆出来
            float ax = warlord ? 1195.12f : 1330.87f;
            float ay = warlord ? top + 73.99f : top + 57.93f;
            // 🔴 **A406**：`Events/*/ArmyName` 三棵 `auto[14.0|13.0|23.0~35.0] 基准=35.0` ⇒ 上限 35（= 标称）
            //   · base **35.0**（三棵同 max/base；**min 三棵各不相同**）。
            // 🔴 **2026-10-24（E12）**：`ArmyName` 的**框宽逐族不同**（战将 289.46 / 另两格 240.45，
            //   实读 `…:401.72→1484.58` 与 `…:565.64→1571.32`）—— 原来三格都用 289.46（另两格多 49.01）；
            //   **下限也逐格不同**（14 / 13 / 23）—— 原来三格都吃 `EvArmyAutoMin = 14` ⇒ 改成调用点显式传。
            Text(c, "", new PxRect(ax, ay, ax + (warlord ? 289.46f : 240.45f), top + 119.53f), ArmyOrange, "ArmyName",
                 EvArmyPx, L_Text, autoFit: true, autoMinPx: armyAutoMin, alignLeft: true, wrap: true,
                 autoMaxPx: 35f, basePx: 35f, face: TmpFont.Face.LatinUi);
            // 🔴 **A406**：`Events/*/Level` 三棵 `auto[23.0~27.0] 基准=35.0` ⇒ 上限 27（= 标称）· base **35.0**。
            // 🆕 2026-10-24（E12 · P0-3）：标签走官方词条（战将格 = 胜利数、另两格 = 等级），值取空态 `-`。
            Text(c, string.Format(TermOr(levelFormatKey, ""), Dash),
                 new PxRect(ax, top + 116.36f, ax + 312.27f, top + 148.36f), Color.white, "Level",
                 EvLvlPx, L_Text2, autoFit: true, autoMinPx: EvLvlAutoMin, alignLeft: true, wrap: true,
                 autoMaxPx: 27f, basePx: 35f, face: TmpFont.Face.LatinCardBody);
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
            //   🔴 **A406**：`ChooseNameWindow/Choose Name Input Field/Text Area/Placeholder` 原版
            //   `auto[18.0~40.0] 基准=14.0` ⇒ 上限 **40** · base **14.0**。
            //   ⚠️ **本站这两格今天不生效**（`fontPx 18 == autoMinPx 18` ⇒ `ProfilePage.Text` 的守卫
            //   `fontPx > autoMinPx` 为假、`SetAutoFitBox` 一次都不跑）—— 照传是为了「哪天守卫一变也不会静默回退」。
            var phTx = Text(ta, "", new PxRect(TaL, TaT, TaR, TaB), PhColor, "Placeholder",
                            PhPx, L_Title, autoFit: true, autoMinPx: PhAutoMin, alignLeft: false, wrap: false,
                            autoMaxPx: 40f, basePx: 14f, face: TmpFont.Face.LatinCardBody);
            if (phTx != null) phTx.SetWrapping(false);
            // 🔴 **A62 · A23 + A77①（第三档）**：原版 `Text` 是 **`折行=3`（`PreserveWhitespaceNoWrap`）**
            //   —— ⛔ **不能用 `SetWrapping(false)` 顶替**（那是把 `3` 静默降级成 `0`；`3` 与 `0` 「等不等价」
            //   至今没有判据，见 `Label.WrappingMode` 头）。判据 = 同一行 dump 的 `折行=3`。
            //   🆕 **2026-10-11（A317）**：`ProfilePage.Text` 的 `wrap` 已**去掉缺省、形参必填** ⇒ 这里显式传
            //   **`wrap: false`** —— 那是本口对「**不是 `1`**」的唯一表示（这一处**逐字保持今天那条路径**：
            //   先落 `0`、再由下面 `SetWrappingMode(InWrapMode)` 设成 `3`；传 `true` 会多一次
            //   `Normal → 3` 的重排，终态一样但路径不同）。
            //   🔴 **A406**：`…/Text Area/Text` 原版 `auto[18.0~40.0] 基准=14.0` ⇒ 上限 40（= 标称）· base **14.0**。
            _nameField = Text(ta, "", new PxRect(TaL, TaT, TaR, TaB), Color.white, "Text",
                              InPx, L_Text, autoFit: true, autoMinPx: InAutoMin, alignLeft: true, wrap: false,
                              autoMaxPx: 40f, basePx: 14f, face: TmpFont.Face.LatinCardBody);
            if (_nameField != null)
            {
                _nameField.SetWrappingMode(InWrapMode);                       // 3
                MenuDraw.AlignLeft(_nameField, new PxRect(TaL, TaT, TaR, TaB)); // 重排（A205）之后重做左对齐
            }
            Hit(fld, "InputHit", new PxRect(IfL, IfT, IfR, IfB), L_NameHit, BeginNameTyping);

            // 🔴 **A62 · A24 / 判据文件 §③「碰巧对」**：原版 `MessageText` 是 **`折行=1`**，原来没显式声明
            //   ⇒ 补 `wrap: true` 钉死（防「`SetAutoFitBox` 副作用哪天没了就静默回退」）。
            // 🔴 **A406**：`ChooseNameWindow/MessageText` 原版 `auto[4.0~40.0] 基准=36.0` ⇒ 上限 40（= 标称）· base **36.0**。
            var msgR = new PxRect(MsgL, MsgT, MsgR, MsgB);
            var lbMsg = Text(_nameWin, Loc.T("MainMenu/Register/PleaseChooseName"), msgR, Color.white, "MessageText",
                 MsgPx, L_Title, autoFit: true, autoMinPx: MsgAutoMin, alignLeft: false, wrap: true,
                 autoMaxPx: 40f, basePx: 36f, face: TmpFont.Face.LatinCardBody);
            // 🆕 **2026-10-16（A712 阶段 2）**：纵向那一半 —— 同 `:297` 那条（原版 `Center/**Midline**`，
            //   `menu_dump … "Player Profile Window" --depth 12 --md` 实读 `MessageText 字号=40.0 对齐=Center/Midline`）。
            MenuDraw.SetVAlign(lbMsg, Label.VAlign.Midline, msgR);

            // `Change Name Button`（`PriceDisplayButton`）> `Generic UI Button` > `Button Text` / `Price Display`
            var btn = Node(_nameWin, "Change Name Button", new PxRect(CnbL, CnbT, CnbR, CnbB));
            var cnQ = Rect(btn, ArtButton, new PxRect(CnbL, CnbT, CnbR, CnbB), "Generic UI Button", L_Frame, GreenButton, true);
            // 🔴 **A62 · A25**：`Button Text`（`'Free'`）原版 `折行=0 auto[12~50]` ⇒ 关掉。
            // 🔴 **A406**：`…/Change Name Button/Generic UI Button/Button Text` 原版
            //   `auto[12.0~50.0] 基准=12.0` ⇒ 上限 50（= 标称）· base **12.0**。
            var freeTx = Text(btn, FreeLabel, new PxRect(CnbTxL, CnbTxT, CnbTxR, CnbTxB), Color.white, "Button Text",
                              CnbPx, L_Text2, autoFit: true, autoMinPx: CnbAutoMin, alignLeft: false, wrap: false,
                              autoMaxPx: 50f, basePx: 12f, face: TmpFont.Face.LatinCardBody);
            if (freeTx != null) freeTx.SetWrapping(false);
            var pd = Node(btn, "Price Display", new PxRect(PdL, PdT, PdR, PdB));
            Rect(pd, null, new PxRect(PdIcL, PdT, PdIcR, PdB), "icon", L_Text2);
            // 🔴 **A62 · A26**：`text`（`'300,00'`）原版 `折行=0 auto[13.46~40]` ⇒ 关掉。
            //   🔴 **A406**：`…/Generic UI Button/Price Display/text` 原版
            //   `auto[13.46~40.0] 基准=**39.0**` ⇒ 上限 40（= 标称）· base **39.0**。
            var pdTx = Text(pd, "", new PxRect(PdTxL, PdT, PdTxR, PdB), Color.white, "text",
                            PdPx, L_Text2, autoFit: true, autoMinPx: PdAutoMin, alignLeft: false, wrap: false,
                            autoMaxPx: 40f, basePx: 39f, face: TmpFont.Face.LatinCardBody);
            if (pdTx != null) pdTx.SetWrapping(false);
            pd.gameObject.SetActive(false);    // 出厂 F（首次改名免费，判据见常量注释）
            // 🆕 A17：原版 `Profile Tab>ChooseNameWindow>Change Name Button>Generic UI Button` 是 SpriteSwap（普查 §块 5 第 6 行）
            Hit(btn, "ChangeNameHit", new PxRect(CnbL, CnbT, CnbR, CnbB), L_NameHit, CommitNameWindow, cnQ, ArtButton);

            // `Generic Close Button Green`（75×75 圆底 + `40k_bt_close`）
            // 🔴🔴 **2026-10-18（A1149 第一半）圆底盘【挂点】归真**（本件 = 第九会话 P7）：
            //   改前圆底盘画在一颗**自造子件 `Image`** 上、而 `Generic Close Button Green` 是颗**裸节点**
            //   （原来在这里先 `Node(...)` 建它、下面再 `Rect(cb, …, "Image", …)` 套一层）。
            //   原版那颗 `Image` **就长在根节点自己身上**（无独立子件名）。
            //   判据（逐字段直读）= `python -I d:/tmp/wf_b4probe/pa.py bundle_menus_assets_all
            //   "ChooseNameWindow" 8`：根 `Generic Close Button Green` **自己带 `Image`**
            //   （它下面唯一的子件是 `Icon`）：`UI_Button_Round_background` · `m_Type=0`(Simple) ·
            //   **`m_PreserveAspect=1`** · `m_RaycastTarget=0` · 矩形 **75.00×75.00**；
            //   贴图 `m_Rect` = **237×237 正方** ⇒ 实绘 **75×75**（框本身即正方）。
            //   ⚠️ `cb` 这个局部量**已挪到下面那颗 quad 之后**（`Rect` 取不到图才退回 `Node`）——
            //   中间那段全是注释、没有任何引用。
            // 🆕 **2026-10-18（A1058 · 第六会话批 2）**：换图那一层 = **子件 `Icon`**
            //   （画的是 `ArtCloseIcon` = `40k_bt_close`），⛔ **不是圆底盘 `cbBaseQ`**。
            //   判据（原版 prefab 亲读；原版那颗的父窗是 `ChooseNameWindow`，**独立于
            //   `Player Profile Window/…/Generic Close Button Green` 的第二个实例**、两者 `m_TargetGraphic` 同指一颗）=
            //   `python -I d:/tmp/wf_hit/rcunion.py bundle_menus_assets_all "ChooseNameWindow" --depth 6`：
            //   根 `Generic Close Button Green` 那颗 `EverguildButton` 的
            //   **`m_TargetGraphic` = pid-5517809430966076878**；解该 pid ⇒ **所属 GO 名 = `Icon`**、
            //   贴图 pid `6553861554683527146` → `40k_bt_close`（`d:/4/_tmp_view/sprite_pids_ALL.json`）；
            //   根自己那颗 `UI_Button_Round_background` 带 **`m_RaycastTarget=0`**。
            //   ⚠️ **2026-10-18 更正（铁律 5）**：原注释写「`trans=2` 换的是它自己的 Image」——
            //   **那句是错的**（错因 = 只读 `m_Transition`、没读 `m_TargetGraphic`）。
            //   ⚠️ 同族的 `Deck Info`（`Shell/DeckInfoPopup.cs`）与 `Battle Log` 早就绑对了「按钮脸」——
            //   本处是漏网（`A1058`）。
            var cbBaseQ = Rect(_nameWin, ArtRoundBtn, new PxRect(CbL, CbT, CbR, CbB),
                               "Generic Close Button Green", L_Frame, null, true);
            var cb = cbBaseQ != null ? cbBaseQ.transform
                                     : Node(_nameWin, "Generic Close Button Green", new PxRect(CbL, CbT, CbR, CbB));
            var cbIconQ = Rect(cb, ArtCloseIcon, new PxRect(CbIcL, CbIcT, CbIcR, CbIcB), "Icon", L_Title);
            // 🆕 **2026-10-18（A1053 · 第六会话批 2）**：**命中区**归真值 —— 原版根那颗
            //   `UI_Button_Round_background` 带 `m_RaycastTarget = 0`（不吃射线），吃射线的只有子件 `Icon`
            //   （56.36×54.50）按 `(-20)⁴` 外扩 ⇒ **96.37 × 94.50**；改前传根矩形（75×75）⇒ 每边小 10.7/9.8。
            //   判据 = `python -I d:/tmp/wf_hit/rcpad.py bundle_menus_assets_all "ChooseNameWindow" --depth 6
            //   --substr "Close Button Green" --ignore-active`（出厂 inact ⇒ 要加那个开关；实读 `96.37 x 94.50`）。
            Hit(cb, "Hit", MenuDraw.PaddedRect(new PxRect(CbIcL, CbIcT, CbIcR, CbIcB), ClosePad),
                L_NameHit, CancelNameWindow, cbIconQ, ArtCloseIcon, "40k_bt_close_hover",
                clickCue: WindowSounds.DefaultCloseCue);

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
            // 🆕 2026-10-24（E12 · P1-8）：**边打边去富文本** —— 原版 `ValidateInput` 每次变化都把
            //   清洗过的串 `set_text` 回输入框（用户看到的就是洗干净的那一行）。我们这里只改**显示**，
            //   `PointerLayer` 自己的缓冲区不动（提交时 `ApplyName` 再洗一次，两处同一份 `StripRichText`）。
            _nameField.SetText(StripRichText(s ?? "") + (editing ? "_" : ""));
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

        /// <summary>提交那一跳（`PointerLayer.BeginText` 的 onCommit）—— **全工程只有这一处写玩家名**。
        /// <para>🔴 **2026-10-24（E12 · P1-8）对齐原版 `ChangeNameWindow.ValidateInput` / `ChangeNameButtonClick`**：
        /// ① **去富文本标签**（原版 `TextMessageValidationTools.RemoveRichTextTags`）；
        /// ② **长度 3~25**（`len &lt; 3` ⇒ 钮不可点 · `len &gt; 24` ⇒ `Substring(0, 0x19)` = 截到 25）；
        /// ③ **同名禁改**（`interactable = … &amp;&amp; text != PlayerDataManager.playerName`）。</para>
        /// <para>⚠️ 原版这三条都落在**钮的 `interactable`** 上（灰钮 = 提交不了），我们没有那条灰钮链
        /// ⇒ 在**唯一的写点**这里做同一组判断，并且**每次都出声**（⛔ 不许静默吞掉一次改名）。
        /// ⚠️ `ValidateInput` 里还有一次 `string.Replace(x, y)`，那两个字面量（`DAT_1842c15b8` / `DAT_1842c11b8`）
        /// **本地没解出** ⇒ 那一跳**没做**（如实记，别当我们做了）。</para></summary>
        void ApplyName(string s)
        {
            string v = StripRichText(s ?? "").Trim();
            if (v.Length > MaxNameLen)
            {
                Debug.Log("[Profile] 名字超长（" + v.Length + " > " + MaxNameLen + "）⇒ 截到前 " + MaxNameLen
                        + " 个字符（原版 `ChangeNameWindow.ValidateInput`：`Substring(0, 0x19)`，0x19 = 25）");
                v = v.Substring(0, MaxNameLen);
            }
            if (v.Length < MinNameLen)
            {
                Debug.Log("[Profile] 名字至少要 " + MinNameLen + " 个字（现在是「" + v + "」）⇒ 不改"
                        + "（原版同一处：`len < 3` ⇒ `Change Name Button.interactable = false`）");
                CloseNameWindow();
                return;
            }
            if (v == ProfileData.PlayerName)
            {
                Debug.Log("[Profile] 与当前名字相同 ⇒ 不改（原版：`interactable` 还要求 `text != 当前名`）");
                CloseNameWindow();
                return;
            }
            ProfileData.PlayerName = v;      // 🔴 唯一写点（联机层的显示名读的是同一个源）
            if (_playerName != null) _playerName.SetText(v);
            CloseNameWindow();
            Debug.Log("[Profile] 玩家名改成「" + v + "」（🆕 2026-10-22 起**已落盘**："
                    + "`persistentDataPath/WarpforgeProfile/profile.json`；原版这一步是上传服务器 + 扣改名费）");
        }

        /// <summary>去掉 `&lt;…&gt;` 形状的富文本标签 —— 原版 `TextMessageValidationTools.RemoveRichTextTags`
        /// 的**超集**：原版是拿一张**静态标签表**逐条 `StringBuilder.Replace(tag, "")`，而那张表**本地没解出**
        /// （`TextMessageValidationTools__RemoveRichTextTags.c` 里是运行期静态数组）⇒ 我们按**形状**删。
        /// ⚠️ 两条保守做法：`&lt;` 后面带空白的**不删**（多半是正文里的数学小于号）；没有闭合 `&gt;` 的**不删**。</summary>
        public static string StripRichText(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            if (s.IndexOf('<') < 0) return s;
            var sb = new System.Text.StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char ch = s[i];
                if (ch != '<') { sb.Append(ch); continue; }
                int close = s.IndexOf('>', i + 1);
                if (close < 0) { sb.Append(ch); continue; }        // 没有闭合 ⇒ 当普通字符
                bool isTag = true;
                for (int k = i + 1; k < close; k++)
                    if (char.IsWhiteSpace(s[k])) { isTag = false; break; }
                if (isTag) i = close;                              // 整段丢掉（连 '>' 一起跳过）
                else sb.Append(ch);
            }
            return sb.ToString();
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

        /// <summary>点头像 ⇒ **切到 `Avatar` 页**（🆕 2026-10-24 E12 · P0-4 —— 原来这里只打一行日志）。
        /// <para>判据 = `decomp_full/ProfileTab__&lt;Start&gt;b__9_0.c`：`GameWindowWithTabs.ChangeTab&lt;object&gt;(this,
        /// DAT_1842d1918)`（前面还有一道 `*(ProfileTab+0xa0)+0x10 != 0` 的布尔闸 —— 那一格是哪个字段**还没解出**，
        /// 如实记着）。**目标页签本身是确定的**：`RankedTab__&lt;Initialize&gt;b__10_0.c` 用的是**同一个元数据常量
        /// `DAT_1842d1918`**，而那一处已经定成 `AvatarTab`（`资料/普查产出_0927/档案窗_Profile页.md` §C·1 第 4 条，
        /// 本项目 `RankedTab.OnAvatarClick` 就照它做的）。</para>
        /// <para>⚠️ 走宿主窗的 `tabButtons.Click(i)` 而不是直接 `ChangeTab` —— 否则左栏高亮会停在原来那一键
        /// （同 `MissionRerollPopup` 那条先例；`TabButtons.Click` 内部就是 `window.ChangeTab(CurrentType)`）。</para></summary>
        public void GoToAvatarTab()
        {
            if (Win == null || Win.tabButtons == null)
            {
                Debug.LogWarning("[Profile] 点头像要切 `Avatar` 页，但拿不到宿主窗的 `tabButtons` ⇒ 切不了（不许静默）");
                return;
            }
            for (int i = 0; i < PlayerProfileWindow.Tabs.Length; i++)
                if (PlayerProfileWindow.Tabs[i].Node == "Avatar Button")
                {
                    Win.tabButtons.Click(i);
                    Debug.Log("[Profile] 点 `Profile` 页的头像 ⇒ 切到 `Avatar` 页"
                            + "（原版 `ProfileTab.<Start>b__9_0` → `ChangeTab<AvatarTab>`）");
                    return;
                }
            Debug.LogWarning("[Profile] `PlayerProfileWindow.Tabs` 里找不到 `Avatar Button` ⇒ 切不了页（不许静默）");
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

        /// <summary>把「我是谁」刷一遍：玩家名 + **已装备的称号** + 头像（原版 `Initialize` 每次 `OnOpen` 重跑一次）。</summary>
        public void RefreshIdentity()
        {
            if (_playerName != null) _playerName.SetText(ProfileData.PlayerName);
            // 🆕 2026-10-22（P1）：称号页装备完再切回本页要看到新值（`ChangeTab` 会调 `OnOpen`）
            if (_playerTitle != null) _playerTitle.SetText(ProfileData.TitleName);
            if (_playerTitleWa != null) _playerTitleWa.SetText(ProfileData.TitleName);
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
            // 🆕 2026-10-26（批次 4 · F7 · 任务 ④）：等级那一格也跟着刷 —— 原版是
            //   `PlayerProfileUIController.OnCollectorLevelUpdate`（升级信号）驱动；我们没信号层 ⇒
            //   **每次开页（`OnOpen`）重读同一个口**，与顶栏 / 排位页一致。
            if (_levelLabel != null) _levelLabel.SetText(PlayerLevelText);
            RefreshNameField();
        }

        /// <summary>🆕 **2026-10-26（批次 4 · F7 · 任务 ④）**：本页 `Player Level Text` 那颗字。
        /// ⚠️ 声明在**类尾**（`CLAUDE.md` §三「新成员追加到类尾」）—— 本页既有成员的行号一个不动。</summary>
        Label _levelLabel;

        // ============================================================ 🆕 2026-10-27（C5 批 · 账 438）
        // 🔴 纪律：这一段**追加在类尾**（本页既有成员的行号一个不动）。

        /// <summary>= 原版 `ProfileAllianceDisplay.Initialize(ProfileAllianceInfo info)` 的**那一支** ——
        /// 🔴 **按真跑的那一份施工**：`ProfileAllianceDisplay.Initialize` 全库**零调用点**，真正在跑的是
        /// `ProfileNameTitleSection` 里那段内联同款（判据 `out_cpp2il_dev_ilspy/ProfileNameTitleSection.cs:126-146`）：
        /// **`AllianceName` 非空** ⇒ `AllianceRatingDisplay.Initialize(Rating, null, disableIfZero: false, …)`
        /// （**零值也显示、不隐藏**）；**空** ⇒ 只把那个 rating 显示件 `SetActive(false)`。
        /// <para>⚠️ 本地没有联盟 ⇒ 出厂走「空」那一支（`Alliance Name` 保持 prefab 占位串 `Alliance Name`）。
        /// 数据源 = `SocialData.AllianceName`（与账 93/95 **同一份**，⛔ 别各写一份）；
        /// 评级文本本地无源 ⇒ 保持 `------`（原版 `RateEmpty`）。</para></summary>
        public void SetAllianceInfo(string name, string ratingText)
        {
            Transform al = null;
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name == "Alliance Info") { al = t; break; }
            if (al == null)
            {
                Debug.LogWarning("[Profile] 找不到 `Alliance Info` 节点 ⇒ `SetAllianceInfo` 什么都没做（⛔ 不静默）");
                return;
            }
            var nm = al.Find("Alliance Name");
            var nmLb = nm != null ? nm.GetComponent<Label>() : null;
            if (nmLb != null) nmLb.SetText(string.IsNullOrEmpty(name) ? PlaceholderAlliance : name);

            var rt = al.Find("Alliance Rating Display");
            bool hasName = !string.IsNullOrEmpty(name);
            if (rt != null && rt.gameObject.activeSelf != hasName) rt.gameObject.SetActive(hasName);
            if (hasName && rt != null && !string.IsNullOrEmpty(ratingText))
            {
                var v = rt.Find("Individual rating value");
                var lb = v != null ? v.GetComponent<Label>() : null;
                if (lb != null) lb.SetText(ratingText);
            }
            Debug.Log("[Profile] `SetAllianceInfo(" + (name ?? "null") + ")` ⇒ "
                    + (hasName ? "联盟名 + 评级段**开**（原版内联版：非空才 `AllianceRatingDisplay.Initialize`）"
                               : "评级段**关**、联盟名回占位串（原版内联版：空 ⇒ `SetActive(false)`）"));
        }
    }
}
