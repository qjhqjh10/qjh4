// LiveOpsEventWindow.cs — 阶段二第 3 层第 6 件：**活动窗的共用基类**
//   （原版 `SkirmishEventWindow : LiveOpsEventWindow<IFastModeEvent>` / `RankedEventWindowV2 : LiveOpsEventWindow<IRankedEvent>`）
//
// ============================ 出处（唯一正本）============================
// `资料/阶段二_战斗入口_原版规格.md` **§一（窗口参数）+ §二 B/C（逐节点表）+ §三（开战链）+ §五（入口）**。
// 2026-09-24 建这两扇窗时**又从根节点逐条实读了一遍**（`工具/menu_rect.py` + `menu_dump.py` +
// `工具/_probe_deckinfo.py`），补上正本表里没有的：`Faction Icon Image` · `Warlod Image` · `Warlord Darkening` ·
// `Deck Buttons` 的四颗钮与各自图标 · `Header Background (1)` / `Header Back Button` · `To Battle Button` 的
// `ButtonIcons` · `Army Content` 的 GridLayoutGroup 参数 · 排位窗的 `Content`(VLG) 那一族。
//
// 🔴 **两扇窗【逐格相同】的部分全在这一个类里**（原版就是同一个泛型基类）——
//    CLAUDE.md §三：「两处写同一条规则 = 迟早不一致」。差别只有 4 处，各自重写：
//      ① 背景那一族（排位多一层 `General Red Background` + `Noise`，且 rect 不同）
//      ② 左列（遭遇战 = `Reward Display` 进度；排位 = `Ranked Division Info` 分段）
//      ③ `TrophyIcon` 的图（`WF_UI_Trophy_Gold` / `40k_ranking_icon_trophy_Plus`）
//      ④ 排位多一个 `ChangeRankedToggle`
//
// ============================ 开战链（§三，四窗共用）============================
// 原版：`To Battle Button` → `MatchMakerManager.StartMatch` →（等不到真人）→ `StartBotBattle`
//       → `SearchOpponentManager.StartBattle` → `GetBattleArena(army)` → `LoadScene(场景名)`
//       （`SearchOpponentManager__StartBattle.c:57/61` —— 全二进制**唯一**的 `LoadScene` 点）
// 🔴 **等待秒数原版是有的，2026-09-24 读到了**（正本 §六 原来记「没读」）：
//    · `SearchOpponentManager.GetTimeToWaitForOpponent`：**不能匹配真人**时返回一个常量 ——
//      用 `工具/read_literal.py` 读出来 = **12.0 秒**（`DAT_1834b3160`）；能匹配真人时走
//      `FeatureConfig.GetMatchmakingWaitingTime(gamesLost, …)`（**远端配置**，本地没有）。
//    · `WaitForHumanOpponent` 协程**每秒轮询一次**（`DAT_1834b2bb8 = 1.0`），减到 0 就去打 bot。
//    ⇒ 我们单机（没有服务器）**正好落在「不能匹配真人」那一支 = 等 12 秒**，然后就该进 bot 对局 ——
//      **这不是我们编的，是原版离线时自己的行为**（`Searching Oponent Popup` 就是这 12 秒里显示的那扇）。
//    ✅ **2026-09-25 更正**：`BattleArenaByArmySO` 那张表**在本地、已实读**（`ArenaBuilder.ArenaForArmy`，
//      判据 → `资料/普查产出_0920/场景光照与后处理_原版规格.md` §六）。
//      ✅ **而且「一局一战场」已经接上了**（2026-09-25）：打完按**督军阵营**切 `Battle_<场>.unity`
//      —— 方案 = **一场一份 Battle 场景**（不是运行时实例化），13 份已建并核验；
//      选场判据 = `ArenaByArmy.BattleSceneNameFor(阵营)` —— 🔴 **2026-09-30（§27）起恒为 `Battle`**：
//      13 份 `Battle_<场>.unity` 不再生成，「哪一场」由 `ArenaRuntimeLoader` 在**运行时**按
//      `SceneFor(阵营)` 从 `Resources/ArenaPrefabs/<场>.prefab` 实例化（判据只留一处）。
//      做法/依据/验收 → `资料/阶段二_战斗入口_原版规格.md` **§七**。
using System.Collections.Generic;
using UnityEngine;
using RuleEngine;
using CardPresentation.Net;      // 🆕 联机（N3：`Battle!` 走 P2P 还是走 12 秒 bot 链）

namespace CardPresentation
{
    /// <summary>活动窗（遭遇战 / 排位）的公共基类。**不要直接实例化**。</summary>
    public abstract partial class LiveOpsEventWindow : GameWindow
    {
        // ============================================================ 分层
        // 🔴 **一条明确的梯子，每层 +1** —— 同一个队列的两层「谁盖谁」是**不可控**的
        //    （透明队列按到相机的距离排；2026-09-23 踩过，2026-09-24 在这里又踩了一次：
        //     四颗圆钮的**图标与底图同队列** ⇒ 实拍里只有第一颗的图标透出来，另外三颗看着是空的）。
        // 高于所有「页」（最高 CampaignTab 3064）与练习窗（3100–3103），
        // 低于 `SearchingMatchPopup`（3130）与 `PromptPopup`（3140）。
        // ✅ **2026-10-11（A252）可见性收窄**：这一族原是 2026-10-04（A47 接线批）**整行**放宽成 `public` 的；
        //   本件把用不到的那些收回去，但**只能收成 `protected`、⛔ 不能收成 `const`** ——
        //   本窗有**两个子类**（`Shell/RankedEventWindow.cs` · `Shell/SkirmishEventWindow.cs`），
        //   它们在**自己的方法体里非限定**用这些常量（`QBg1/QBg2/QBg3/QArt/QArt1/QText` 等在 `Build*`
        //   那几段里逐个出现）⇒ 收 `const` 会当场 CS0122。⚠️ **按内容认，⛔ 别按行号认**（行号会漂）。
        //   量法（可复跑）：脚本扫全工程 **301** 个 `.cs` 的限定名 `LiveOpsEventWindow.<常量>`、**剔注释**、剔本文件。
        public const int QBg = 3104;      // 压暗整屏 —— ✅ 留 `public`：`Editor/MainMenuScene.cs` 引用（2 处）
        protected const int QBg1 = 3105;  // 红底
        protected const int QBg2 = 3106;  // 红底上的 `Noise`
        protected const int QBg3 = 3107;  // `Menu Vignette`
        protected const int QDeck = 3108; // 中栏第一层（阵营徽记）
        protected const int QDeck1 = 3109;// 中栏第二层（督军立绘）
        protected const int QDeck2 = 3110;// 中栏第三层（`Warlord Darkening` 压暗）
        protected const int QArt = 3111;  // 各处的按钮底 / 面板底 / 装饰
        protected const int QArt1 = 3112; // 压在 `QArt` 上的那一层（按钮里的图标、标题栏的尖角/返回钮）
        protected const int QArt2 = 3113; // 再上面一层（返回钮压在尖角上）
        protected const int QText = 3114;
        /// <summary>⚠️ **2026-10-04（A47 接线批）：这一档现在已经没有用户了**（保留常量只为不改 API）——
        /// `RankedEventWindow` / `SkirmishEventWindow` 的整屏背板命中区**不再用它**，改走
        /// `MenuDraw.ShadeHit`、档直接取**压暗层自己那一档** `QBg`(3104)（规矩：压暗层的命中区落在压暗层那一档，
        /// 且严格低于本窗内容命中区最低档 `QHit`）。⚠️ 原来这里把 `PracticeModePopup` 的 `QPrHit - 1`
        /// 当成正确写法写了进来 —— **那个写法是错的**（A25·补 已订正为 `QPr`；铁律 5：就地改掉误导记录）。
        /// <para>🔴 **2026-10-07（A77⑪）就此补回一条指针（那条「为什么」被上面这次改写删掉了）** ——
        /// 「整屏背板那一下**必须比其他命中区【低】**」的判据**只有一份**，在
        /// `Shell/MenuDraw.cs` 的 `ShadeHit` 那段注释里（`qShade >= qContentMin` 当场告警那条）：
        /// `PointerLayer` 的判据是「**队列大的先吃、同队列比 z**」，而 `ImageQuad` 的**世界 z 恒 0**
        /// ⇒ 同档时点 `Battle!` 会被判成「点背景」直接关窗（**2026-09-24 找茬子代理抓到的阻断项**；
        /// 实拍/日志证据：`Shell/BoosterInfoPopup.cs` 的 `QShadeHit` 注释 + `_tmp_view/shop.log:11896`）。
        /// ⛔ 别把这条判据在这里重写一遍（`CLAUDE.md` §三「两处写同一条规则 = 迟早不一致」）。</para></summary>
        protected const int QHitBackdrop = 3115;   // ⬆️ 2026-10-11（A252）：`public` → `protected`（只有子类用得到）
        public const int QHit = 3116;      // ✅ 留 `public`：`Editor/MainMenuScene.cs` 引用（4 处）

        // ---- 阵营格**内部**那四层（🆕 2026-10-07 A118②）------------------------------------------
        // 判据 = 原版 item prefab `Ranked Army Selector Container V2` 的**兄弟序**
        //   `Background`(0) → `On`(0 的子件) → `ProgressBar`(1) → `Army Icon`(2) → `Featured Icon`(3)
        //   ⇒ 画序 Background < On < ProgressBar < Army Icon < Featured Icon，四层**互相叠**。
        // 🔴 每层必须**不同档**（同档谁盖谁不可控 —— 透明物按到相机的 3D 距离排，见文件头那条梯子的注释）。
        //   上面那把梯子在 3111–3113 只有三档艺术层 ⇒ 第四层落 **3115**（`QHitBackdrop` 2026-10-04 起
        //   已无用户，见它自己的注释；本窗族的 3116 是命中档、**下一扇窗 `SearchingMatchPopup` 从 3130 起**）。
        //   ⚠️ `On` 那一层**与 `Background` 同档**是安全的：原版它出厂 `m_IsActive = false`，而且运行期
        //   **也从不显示** —— uGUI `Toggle.PlayEffect` 只对 `graphic` cross-fade alpha、**不 SetActive**
        //   （`Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/Toggle.cs:297-309` 实读），
        //   我们的选中态由 `WindowButton` 换 `Background` 的贴图承担（= 原版 `ToggleSprite` 那一层语义）。
        //   ⛔ 将来真要把它显示出来，**先给它一个独立档**（它必须夹在 `Background` 与 `ProgressBar` 之间）。
        // ✅ **2026-10-11（A252）**：同样只能收成 `protected`（两个子类在**自己的方法体里非限定**用这四个别名）。
        protected const int QArmyBack = QArt, QArmyBar = QArt1, QArmyIcon = QArt2, QArmyFeat = 3115;

        // ============================================================ 行为常量（原版实测）
        /// <summary>`SearchOpponentManager.GetTimeToWaitForOpponent` 的「不能匹配真人」分支
        /// （`DAT_1834b3160`，`工具/read_literal.py` 读出来 = **12**）。
        /// 🔴 **只留这一份** —— 真正的等待/倒计时在 `SearchingMatchPopup` 里（那扇窗才是「匹配」这件事的宿主），
        /// 这里转调，别再写一遍。</summary>
        public static float WaitForOpponentSeconds { get { return SearchingMatchPopup.WaitForOpponentSeconds; } }
        /// <summary>原版 `Numer Of Army Decks` 分子的上限档（出厂占位串 `1/20` —— 单机不限制，照印）。
        /// 🔴 **2026-10-24（E11）起这一格【不再用】本常量做分母**：原版那一格是
        /// `"{该套下标+1}/{该阵营该模式套数}"`（`RankedDeckSelector__ChangeDeck.c:110-119`），**从来没有 20**。
        /// 常量留着只为不改公开 API（⛔ 别把它重新塞回 `DeckCountText`）。</summary>
        public const int MaxDecksPerArmy = 20;

        // ============================================================ 几何：共用件
        public const float DeckSelL = 525.30f, DeckSelT = -0.10f, DeckSelR = 1394.70f, DeckSelB = 1080.10f;
        /// <summary>`Faction Icon Image` **522.63²**（原版 `sprite=0`、运行时喂）。
        /// ⚠️ 这四个是**原版 prefab 的真值**（判据），画的时候还要叠 <see cref="FacIconDropY"/>。</summary>
        public const float FacIconL = 711.01f, FacIconT = 133.50f, FacIconR = 1233.64f, FacIconB = 656.14f;
        /// <summary>🔴 **2026-10-24（E19 · 用户点名「中央那块整体往下，到中间」）：空态阵营大徽记整体下移 59.2 px。**
        /// <para>可量化的落点：原版徽记中心 y = (133.50 + 656.14) / 2 = **394.8**；用户参考图
        /// （`C:/Users/qjh36/Desktop/warpforge参考图/经典模式参考图.png`）同一位置是 `Warlod Image`
        /// `(450.93,−95.07)-(1549.07,1003.07)`，中心 y = **454.0** ⇒ 454.0 − 394.8 = **59.2**。
        /// 判据出处 = `资料/普查产出_1024b/S4_模式窗细节与徽章.md` §③。</para>
        /// <para>⚠️ **只作用于空态那一块**（`Faction Icon Image` 本来就是 `SetActive(!hasDeck)`）——
        /// 有卡组时画的是 `Warlod Image`，它的中心**本来就在 454.0**，⛔ 别跟着一起挪。</para>
        /// <para>⚠️ 再往下到 1080 正中（540）**没有判据**（参考图没有空态）⇒ 到 454.0 为止，别自己再往下推。
        /// ⛔ 也不要把 `FacIconT/B` 直接改掉：那四个是 prefab 原值，改掉之后「画在哪」与「原版是多少」
        /// 就再也分不开了（铁律 5·c：一个值 ≠ 全部情况）。</para></summary>
        public const float FacIconDropY = 59.2f;
        /// <summary>`Warlod Image` **1098.14²**（原版 `sprite=0`、运行时喂督军立绘）。</summary>
        public const float WlL = 450.93f, WlT = -95.07f, WlR = 1549.07f, WlB = 1003.07f;
        public const float WlDarkL = 529.97f, WlDarkT = 418.18f, WlDarkR = 1470.03f, WlDarkB = 975.82f;
        /// <summary>`Deck Buttons`（HLG spacing **0** · align **MiddleCenter**）· 每颗 **160.87×128**。</summary>
        public const float DbL = 640.84f, DbT = 816.03f, DbR = 1265.16f, DbB = 1033.97f;
        public const float DbBtnW = 160.87f, DbBtnH = 128f;
        public const float DnL = 694.70f, DnT = 702.14f, DnR = 1216.70f, DnB = 779.06f;
        public const float DwL = 694.70f, DwT = 779.06f, DwR = 1216.70f, DwB = 833.74f;
        public const float NoDeckL = 617.17f, NoDeckT = 684.99f, NoDeckR = 1302.83f, NoDeckB = 851.49f;
        public const float CreateBtnL = 789.00f, CreateBtnT = 865.74f, CreateBtnR = 1131.00f, CreateBtnB = 968.74f;
        public const float CreateTxL = 800.96f, CreateTxT = 875.79f, CreateTxR = 1118.70f, CreateTxB = 958.60f;
        public const float CntL = 949.19f, CntT = 625.22f, CntR = 1038.01f, CntB = 702.14f;
        public const float CntIcL = 866.58f, CntIcT = 630.17f, CntIcR = 933.60f, CntIcB = 697.20f;

        public const float ArmSelL = 1283.06f, ArmSelT = 128.05f, ArmSelR = 1910.38f, ArmSelB = 917.80f;
        public const float FacTitleL = 1335.72f, FacTitleT = 164.25f, FacTitleR = 1857.73f, FacTitleB = 218.94f;
        public const float ArmViewL = 1323.16f, ArmViewT = 218.94f, ArmViewR = 1870.28f, ArmViewB = 882.03f;
        /// <summary>`Army Content` = GridLayoutGroup：**cell 168×168** · spacing 0 · **pad(0,0,20,0)** ·
        /// `constraint=0/Flexible` ⇒ 列数按宽度算。`align=1`（UpperCenter）。</summary>
        public const float ArmyCell = 168f, ArmyPadT = 20f;

        /// <summary>🆕 **2026-10-07（A9 / A38①）：阵营条 `Viewport` 的 `RectMask2D.m_Softness` = `(0,52)`**
        /// —— **纵向渐变 52px、左右硬边**（x 管左右、y 管上下；0 = 硬边）。
        /// 🔴 **判据（逐处实读 `d:/4/_tmp_view/q1_rm2d.txt`，三族各一份、值逐字相同）**：
        ///   · `Ranked Army Selector/Army Selector/Viewport`（值 `:299` · 路径 `:300`）—— 独立母版
        ///   · `SkirmishModeEventWindow/Ranked Army Selector/Army Selector/Viewport`（值 `:55` · 路径 `:56`）
        ///   · `RankedEventWindowV2/Ranked Army Selector/Army Selector/Viewport`（值 `:269` · 路径 `:270`）
        ///   三条的 `m_Padding` **都是 `(0,0,0,0)`** ⇒ 本条**只有软边、没有 padding**
        ///   （别把锻造轨道那个 `(10,0,0,0)` 抄过来 —— 铁律 5·c：一个值 ≠ 全部情况）。
        /// 🔴 **为什么是「逐件传」而不是设一个 `ClipSoftness`**：本窗是
        ///   `LiveOpsEventWindow : GameWindow : MonoBehaviour`，**不是 `MenuWindowBase` 的子类**
        ///   ⇒ 那三兄弟（`Clip` / `ClipSoftness` / `ClipPad`）**当年一个都够不着**
        ///   （🔴 **2026-10-10 订正（A194）**：三兄弟**已上移到 `GameWindow`**（A78② 落地）⇒ **今天够得着了**；
        ///   ⚠️ **本窗仍走「逐件传」是 A78① 的裁定、不是回归** —— ⛔ 别改成设 `ClipSoftness`。
        ///   原文：「它们只长在 `MenuWindowBase` 上」）。
        ///   而 `MenuDraw.Rect/Nine` 本来就收 `clipSoftness` ⇒ 照本窗既有做法（`clip` 也是逐件传的，见
        ///   `RebuildArmyCells`）把软边一起传下去 —— 机制**只有 `MenuDraw.ApplySoftEdges` 那一份**，
        ///   本窗不新写第二份。先例 = `Shell/ChatPanel.cs` 的 `ChatTab.VpSoft = (0,22)`（A78①）。
        /// ⚠️ **命中区照旧【不吃】软边**（`MenuDraw.Hit` 故意不收它）：原版 `m_Softness` 只改渲染
        ///   （掩码在 shader 里逐像素削 alpha），射线那一面只看矩形 ⇒ 命中区跟着缩就是**行为偏离**。
        /// <para>🔴 **2026-10-13（A435 阶段 2 · 丁 / A745）**：裁切状态（框 + 软边）**已经搬到
        ///   `Army Selector/Viewport` 那颗 `ViewportClip` 节点上**（`BuildArmySelector` 里挂的，
        ///   `padding = (0,0,0,0)` · `softness = (0,52)` —— 与本常量逐字同值）。
        ///   ⇒ 本常量现在是**回落那一档**的软边：`RebuildArmyCells` 逐件传的 `clip` 已是 `null`，
        ///   而 `clipSoftness` **照旧传本常量**（形参非空 = 旧路赢；节点在时以节点的 `softness` 为准）。
        ///   先例 / 同一形状 = `Shell/ChatPanel.cs` 的 `ChatTab.VpSoft`（A435 丙）。⛔ 别把它删掉。</para>
        /// <para>⚠️ 上面那段「**为什么是逐件传而不是设一个 `ClipSoftness`**」是 **A78① 当时的判断**，
        ///   迁移后**只覆盖「`clipSoftness` 那一个形参」这一半**（`clip` 那半边已经交给节点）——
        ///   按铁律 5 保留原文并就地标注，⛔ 不删。</para></summary>
        public static readonly Vector2 VpSoft = new Vector2(0f, 52f);

        public const float HdrL = 0f, HdrT = 40.86f, HdrR = 550f, HdrB = 150.41f;
        /// <summary>`Header Background` 的 HLG：spacing **5.5** · pad **(155,61,0,0)** · align **MiddleLeft**。</summary>
        public const float HdrPadL = 155f, HdrPadR = 61f, HdrGap = 5.5f;
        public const float HdrBg1L = -462.10f, HdrBg1T = 40.87f, HdrBg1R = 87.90f, HdrBg1B = 156.23f;
        public const float HdrBackL = -24.40f, HdrBackT = 42.88f, HdrBackR = 143.48f, HdrBackB = 154.21f;

        public const float BattleL = 1376.76f, BattleT = 917.80f, BattleR = 1817.09f, BattleB = 1038.40f;
        public const float BattleTxL = 1392.21f, BattleTxT = 929.56f, BattleTxR = 1800.98f, BattleTxB = 1026.52f;
        /// <summary>`ButtonIcons`（HLG spacing **−28.31** · align **MiddleRight** · 🔴 **`reverse=1`**
        /// ⇒ 视觉左→右 = `TrophyIcon` → `ShieldIcon`，**树序的倒排**）· 图标 100²。</summary>
        public const float BIconsL = 1015.57f, BIconsT = 917.79f, BIconsR = 1457.66f, BIconsB = 1038.40f;
        public const float BIconsGap = -28.31f, BIconSide = 100f;

        public const float HelpL = 1804.47f, HelpT = 23.73f, HelpR = 1893.00f, HelpB = 112.27f;
        /// <summary>`Help Button` 的 `m_LocalScale` = **0.7357**（原版那个 rect 是**未缩放**的本地矩形 ⇒
        /// 真画出来只有 88.53 × 0.7357 = **65.13 px**，绕中心缩）。</summary>
        public const float HelpScale = 0.7356948f;

        public const string ArtShade = null;                       // 纯色层（Background fill 那种）
        public const string ArtPopupRed = "40k_general_popup_simple_red";
        public const string ArtWarlordDark = "Smooth_background_square";
        public const string ArtRoundBtn = "UI_Button_Round_background";
        public const string ArtMulligan = "UI_Button_Mulligan";
        public const string ArtDeckChange = "40k_UI_bt_deck_change";
        public const string ArtEye = "40k_bt_eye";
        public const string ArtBackArrow = "40k_UI_bt_back";
        /// <summary>🔴 **2026-10-18（A1053）**：四颗卡组钮里**唯一吃射线的**那颗 `Icon` 子件**自己的**
        /// `m_RaycastPadding`（原版实读 `(-20)⁴`；L,B,R,T · **负 = 外扩**）——
        /// ⚠️ 四颗钮**根上那颗 `Image`(`UI_Button_Round_background`) 实测 `m_RaycastTarget = 0`**
        /// ⇒ 可点区 = `Icon`（120.49×93.01）外扩 20 = **160.49 × 133.01**（⛔ 不是整颗钮 160.87×128）。
        /// 算式只走 `MenuDraw.PaddedRect`；口径 → `普查_全仓命中区与关闭键族.md` §〇-1。</summary>
        static readonly Vector4 IconPad = new Vector4(-20f, -20f, -20f, -20f);
        public const string ArtDeckCount = "40k_UI_icon_deck";
        public const string ArtHeaderBg = "WF_Campaign_Info_Background";
        public const string ArtHeaderBack = "UI_Button_Menu_Back";
        public const string ArtShield = "UI_icon_shield";
        public const string ArtHelp = "40K_generic_bt_info";
        // ⚠️ `WF_Campaign_Info_Background` 的**九宫 / 贴图尺寸**本窗**不再留第二份** —— 2026-10-17（A866）
        //    收口到 `WindowHeader.PlateBorder` / `PlateTexW` / `PlateTexH`（判据写在那三个常量上）。

        // ============================================================ 状态
        public readonly List<Transform> ArmyCells = new List<Transform>();
        public readonly List<string> MissingArt = new List<string>();
        /// <summary>右栏阵营格的**下标**（`Factions()` 里那一格）。
        /// 🔴 **2026-10-24（E11）**：「选中阵营」的**判源是字符串** <see cref="SelectedArmyFaction"/>
        /// （原版 `currentArmy` 是个具体阵营，**没有**「不限」这一档）；本字段只是它在 `_facs` 里的下标。
        /// −1 今天只剩一种含义：**该阵营不在 `Factions()` 里**（卡池残缺 ⇒ 一格都不亮、并出声）。
        /// ⛔ 别再把它写成开窗初值 = −1（那会让右栏一格都不亮，与原版不符）。</summary>
        public int ArmyIndex = -1;
        /// <summary>卡组栏当前指向的自有卡组下标；**−1 = 空态**（本窗模式 ∧ 本阵营一套都没有）。
        /// 🔴 **2026-10-24（E11）**：−1 是**合法状态**（原版 `ChangeDeck(null)`），⛔ 别当「没初始化」处理。</summary>
        public int DeckIndex;
        /// <summary>在「预组卡组」那一页选中的那副（空 = 用的是「我的卡组」）。
        /// ⚠️ **还不能拿去开战** —— 原因与出处见 `PrebuiltDecks.WarnNotPlayableYet`。</summary>
        public PrebuiltDecks.Deck PickedPrebuilt;

        /// <summary>`To Battle!` 真的切了场景没有（自检用 —— 批处理下不切）。</summary>
        public bool StartedBattle { get; private set; }
        /// <summary>最近一次开出来的那扇（自检用）。</summary>
        public static LiveOpsEventWindow LastOpened;

        protected SearchingMatchPopup _search;
        protected Label _txtDeckName, _txtDeckWarlord;
        protected Transform _deckHolder;
        protected MenuScroll _armyScroll;
        /// <summary>🆕 **2026-10-07（A9 / A38①，自检用）**：阵营条那个滚动区（`Viewport` 就在它身上 ——
        /// 软边的裁切边界与带宽都由它定）。照 `ForgeTab.TrackScroll` / `ArmyScroll` 那两条的形状开只读口。</summary>
        public MenuScroll ArmyScroll { get { return _armyScroll; } }
        protected List<string> _facs;

        // ============================================================ 子类要给的四处
        /// <summary>本局该载入哪份对战场景 —— **照原版按督军阵营查表**（`ArenaByArmy.SceneFor`）。
        /// 🆕 **2026-09-25**：战场几何是**建场时烘进场景**的 ⇒ **一局一个战场 = 一场一份 Battle 场景**
        /// （建法：`WF_ARENA=&lt;场&gt; BattleScene.BuildAndSaveScene`）。
        /// 该场那份**没建 / 没进 Build Settings** 时，`ArenaByArmy` 会**回落 `Battle` 并出声**（不许静默）。
        /// 子类若要钉死用某一个场景，覆写这个方法即可（原来是个无参的 `virtual string BattleScene`）。</summary>
        protected virtual string BattleSceneFor(string faction) { return ArenaByArmy.BattleSceneNameFor(faction); }
        /// <summary>本窗对应的**对局模式**（`0` 经典 · `13` 遭遇）—— 新建卡组时把它打进卡组、
        /// 开战时按它挑 `GameplayVariables`。
        /// 判据 → `资料/加时与冲突模式_原版规格.md` §2.7：**模式是卡组的固有属性**，
        /// 原版在 `SelectDecksTab.CreateDeck` 那一刻把「当前模式」打进新卡组。
        /// ⚠️ **排位窗用默认值（经典）** —— 原版排位那条链传的是一个运行期「当前模式」全局量
        /// （`RankedDeckSelector__OnSelectDeckButtonClick.c:32` 的实参是 `FUN_1800021f0(0, …)`、**不是字面量**），
        /// **本地判不出它一定是经典**；按我们现有的 30 张口径走，**如实记着**。</summary>
        protected virtual int DeckGameMode { get { return (int)GameMode.Classic; } }

        /// <summary>这个模式的人话名字（提示行 / 弹窗文案用）。</summary>
        protected string DeckGameModeName
        {
            get
            {
                // 🔴 **2026-10-22（`W63` · 铁律 5 就地订正）：这两句改用【官方词条键】**
                //   `MainMenu/GameMode/{Skirmish,Classic}` —— 判据 = 官方本地化表逐字：
                //   `数据/游戏数据/官方本地化_en+zh-CN.json` 的 `terms["MainMenu/GameMode/Skirmish"]`
                //   = `["Skirmish","小冲突"]`、`…/Classic` = `["Classic","经典"]`；
                //   `Resources/official_loc_13lang.json` 同键 13 列同。
                //   ⚠️ **订正**：原来这两句走 `MenuDeck/GameMode/{Skirmish,Classic}`，注释写着「键名照原版」——
                //   **不成立**：那两条是**自拟键、非原版**（两张官方表 0 命中；条目在 `Core/Loc.cs:2071-2072`，
                //   值里还嵌着我们自己的卡组张数注解「（Skirmish · 12 张）」/「（Classic · 30 张）」，
                //   官方词条没有这层注解）⇒ 界面上印的模式名在此偏离原版。现在换官方键，
                //   值 = 官方的「小冲突 / 经典」（⚠️ `Core/Loc.cs` 那两条**没删**，本笔只改引用）。
                //   ⚠️ 别与 `StartMatch` 里那个 `modeStr`（`"Skirmish"`/`"Classic"`）混为一谈 —— 那两个是
                //   **网络协议串**（喂 `NetMatchmaking.TryStart`、两边对账用），⛔ 绝不能翻（`A1036`）。
                return DeckGameMode == (int)GameMode.Skirmish
                     ? Loc.T("MainMenu/GameMode/Skirmish") : Loc.T("MainMenu/GameMode/Classic");
            }
        }

        /// <summary>这副自有卡组**合不合本窗的模式**。
        /// 原版：卡组带 `GameMode` 字段，选卡组那一层按它筛（`DeckSelectionPopup.__TryOpen_b__10_0.c:10-17`）。</summary>
        bool DeckFitsMode(RuleEngine.PlayerDeck d) { return d != null && d.GameMode == DeckGameMode; }

        // ============================================================ 「选中阵营」与「本窗有效卡组」
        // 🔴 **2026-10-24（E11 · 用户点名「经典 / 小冲突模式窗与原版不符」）** —— 判据（第一权威 = 反编译
        //    `d:/2/tools/decomp_full/RankedDeckSelector*.c`，四支方法体逐步实读）：
        //    · **表**（原版字段 `+0xa8` `currentArmyCardDeckList`）= 自有卡组里
        //      「`deck.warlord.cardArmy == currentArmy` ∧ `deck.GameMode == 当前模式`」的那几套
        //      （`__c__DisplayClass29_0___ChangeArmy_b__0.c:12-22` 两个条件；`ChangeArmy.c` 用它建表）；
        //    · 表空 ⇒ `Enumerable.FirstOrDefault` = **null** ⇒ `ChangeDeck(this, null, army)`
        //      ⇒ 走**空态**那一支（`ChangeDeck.c:52-63` 的 `param_2 == 0`）；⛔ **绝不拿别的模式/别的阵营顶上**；
        //    · 空态 = `warlordImage(0x78) / warlordName(0x48) / deckName(0x50) / deckQuantity(0x40)`
        //      **全 `SetActive(false)`**；`factionIconImage(0x80)` + `noDeckText(0x58)` **`SetActive(true)`**，
        //      徽记喂 `ArmyUtilities.GetArmyIcon(currentArmy)`（`ChangeDeck.c:134-139`）；
        //    · 有卡组 = 上表反过来（那四件开、徽记与提示关），且张数文本 =
        //      `String.Format("{0}/{1}", 该套在表里的下标 + 1, 表.Count)`（`ChangeDeck.c:105-119`）；
        //    · 四颗圆钮按**表.Count** 关：`previous/next (0x20/0x28) = SetActive(1 < Count)`、
        //      `select/view (0x30/0x38) = SetActive(0 < Count)`（`ChangeArmy.c` 那四跳）；
        //    · `currentArmy` 的初值 = **当前卡组督军的阵营**、没有卡组 ⇒ **10 = Ultramarines**
        //      （`Initialize.c:15-22` → `ChangeArmy`）。
        //    ⚠️ 原版**没有**「不限阵营（`ArmyIndex = -1`）」这一档 —— 我们保留 −1 只当内部兜底
        //    （阵营不在 `Factions()` 里时），⛔ 别再把它当开窗初值用。

        /// <summary>本窗当前选中的阵营（原版 `RankedDeckSelector.currentArmy`，字段 `+0xa0`）。
        /// **字符串才是判源**；`ArmyIndex` 只是它在 `_facs` 里的下标（阵营不在卡池里时会是 −1，
        /// 但「选中阵营」这件事照样成立 —— 空态徽记与空态卡组数都按这个字符串算）。
        /// 空 ⇒ 回落 `Ultramarines`（= 原版 `Initialize` 的 `10`，见 `DeckRuntime.NoArmyFaction`）。</summary>
        string _armyFactionSel;

        /// <summary>本窗当前选中的阵营（只读口，自检/断言用）。⚠️ 原版没有「不限阵营」这一档。</summary>
        public string SelectedArmyFaction
        {
            get
            {
                if (!string.IsNullOrEmpty(_armyFactionSel)) return _armyFactionSel;
                if (ArmyIndex >= 0 && _facs != null && ArmyIndex < _facs.Count) return _facs[ArmyIndex];
                return DeckRuntime.NoArmyFaction;          // = "Ultramarines"（原版 Initialize 的 10）
            }
        }

        /// <summary>阵营格下标 → 阵营名；越界 / `_facs` 还没建 ⇒ null（调用方出声）。</summary>
        string FactionOfIndex(int i)
        {
            if (_facs == null || i < 0 || i >= _facs.Count) return null;
            return _facs[i];
        }

        /// <summary>阵营名 → 阵营格下标；`_facs` 里没有 ⇒ **−1**（⛔ 不是 0 —— 0 是 Ultramarines 那一格）。</summary>
        int IndexOfFaction(string faction)
        {
            if (_facs == null || _facs.Count == 0 || string.IsNullOrEmpty(faction)) return -1;
            for (int i = 0; i < _facs.Count; i++)
                if (DeckRules.SameFaction(_facs[i], faction)) return i;
            return -1;
        }

        /// <summary>第 <paramref name="i"/> 套自有卡组的阵营（督军决定；越界 / 没有督军 ⇒ 空串）。</summary>
        string FactionOfDeck(int i) { return CollectionData.DeckAt(i).Faction; }

        /// <summary>第 <paramref name="i"/> 套自有卡组在不在「选中阵营 ∧ 本窗模式」那张表里
        /// （= 原版 `ChangeArmy` 的 `Where`，**全类唯一一份**）。🔴 **不含合法性** —— 合法性是**下一跳**
        /// `OrderByDescending` 的**排序键**（⛔ 不是过滤器），见类尾 `DeckIsLegal`（含 2026-10-28 的更正与判据）。</summary>
        bool InArmyDeckList(int i, string faction)
        {
            var raw = CollectionData.Raw(i);
            return raw != null && DeckFitsMode(raw) && DeckRules.SameFaction(FactionOfDeck(i), faction);
        }

        /// <summary>`Open()` 那一下：**选中阵营 ∧ 本窗模式**那张表里的第一套（从 <paramref name="prefer"/> 起绕一圈）。
        /// 表空 ⇒ **−1 = 空态**。
        /// <para>判据 = 原版 `Initialize.c:15-22` → `ChangeArmy`：`currentArmy` 先由**当前卡组督军的阵营**定
        /// （**只有当前卡组为空**才回落到 `10` = Ultramarines），再在该阵营 ∧ 本模式的表里取 `FirstOrDefault`。
        /// ⚠️ 调用前 `_armyFactionSel` 必须先由 `Open()` 定好（本方法只做「表里第一套」这一件事）。</para>
        /// <para>🔴 **2026-10-24 更正（铁律 5）**：改前本方法只在**模式**上筛、末尾还有
        /// `return (prefer >= 0 &amp;&amp; prefer &lt; n) ? prefer : 0;`（「找不到就回落到第 0 套 / 当前套」）——
        /// 那就是「小冲突窗画出一套**经典**卡组」的病根。原版表空时 `FirstOrDefault` 是 null ⇒ 空态，
        /// ⛔ 没有这条回落。</para></summary>
        int FirstDeckInMode(int prefer) { return FirstDeckInArmy(SelectedArmyFaction, prefer); }

        /// <summary>`ChangeArmy` 那一下：该阵营那张表的**第一套**（从 <paramref name="prefer"/> 起绕一圈）。
        /// 表空 ⇒ **−1**（= 空态）。**排序照原版 `OrderByDescending(ValidateDeck)`**：合法的优先，全不合法才取
        /// 表里第一套（`prefer` 那一套若在表里且合法 ⇒ 就是它自己 = 原版「保持当前这副」那一支）。</summary>
        int FirstDeckInArmy(string faction, int prefer)
        {
            int n = CollectionData.DeckCount();
            if (n <= 0) return -1;
            int start = (prefer >= 0 && prefer < n) ? prefer : 0, first = -1;
            for (int k = 0; k < n; k++)
            {
                int i = (start + k) % n;
                if (!InArmyDeckList(i, faction)) continue;
                if (first < 0) first = i;                    // 稳定序里的第一套（全不合法时用它）
                if (DeckIsLegal(i)) return i;                // 合法优先 = `OrderByDescending` 之后的 `FirstOrDefault`
            }
            return first;
        }

        /// <summary>「选中阵营 ∧ 本窗模式」的卡组数（= 原版 `currentArmyCardDeckList.Count`，字段 `+0xa8`）。
        /// 🔴 改前这里（a）按 `ArmyIndex` 判、（b）**不按模式筛**、（c）`ArmyIndex = -1` 时回落到**全库总数**
        /// ⇒ 三处都不对（空态会算出「有 1 套」）。</summary>
        public int ArmyDeckCount()
        {
            string fac = SelectedArmyFaction;
            int n = 0;
            for (int i = 0; i < CollectionData.DeckCount(); i++) if (InArmyDeckList(i, fac)) n++;
            return n;
        }

        /// <summary>当前画出来的那套在「选中阵营 ∧ 本窗模式」表里的**0 基**序号；不在表里 ⇒ −1。
        /// ⚠️ 预组不在卡组库里 ⇒ 恒 −1（原版 `List.IndexOf` 同解）。</summary>
        int ShownDeckOrdinal()
        {
            if (ShownPrebuilt != null) return -1;
            string fac = SelectedArmyFaction;
            if (!InArmyDeckList(DeckIndex, fac)) return -1;
            int ord = 0;
            for (int i = 0; i < CollectionData.DeckCount(); i++)
            {
                if (i == DeckIndex) return ord;
                if (InArmyDeckList(i, fac)) ord++;
            }
            return -1;
        }

        /// <summary>本窗**这一栏该不该画「有卡组」那一套**（= 原版 `ChangeDeck` 的 `cVar1 = deck != null`）。
        /// = `PickedPrebuilt != null` **或** 「当前自有卡组既合本窗模式、又属于选中阵营」。
        /// <para>🔴 这是用户 2026-10-24 点名那三处（左边栏 / 中间大阵营图标 / 恒画灵族立绘）的**总闸**：
        /// `false` ⇒ 只画「选中阵营的徽记 + 空态提示 + `Create deck`」，⛔ 不画督军立绘 / 卡组名 / 督军名 / 张数。</para></summary>
        public bool HasEffectiveDeck
        {
            get
            {
                if (ShownPrebuilt != null) return true;
                return DeckIsCurrentInArmy();      // 🆕 B90-N3：**不带合法性**（原版这一支只判 `deck != null`）
            }
        }

        /// <summary>空态提示那一句 —— 原版 `ChangeDeck.c` 的两支（**两档，不是一句**）：
        /// <list type="number">
        /// <item>该阵营**一套都没有**（`count &lt; 1`）⇒ `I2.GetTranslation(DAT_1842c0818)`，**无占位符**
        ///   ⇒ 用官方键 `MainMenu/Ranked/NoWarlordMessage`（en `You don't have any warlord from this army.`
        ///   / zh `你没有这个军队的任何战将。`）；</item>
        /// <item>有 `N` 套 ⇒ `String.Format(GetTranslation(DAT_1842c0718), N, GetLocalizedArmyName(army))`
        ///   —— **两个实参、次序与类型恰好对上** ⇒ 官方键 `MainMenu/Ranked/NoDeckMessage`
        ///   （en `You have &lt;color=#E98A00FF&gt;{0} {1} Warlord(s)&lt;/color&gt;, build your deck for this game mode now!`）。</item>
        /// </list>
        /// <para>⚠️ **如实记**：反编译只给字符串指针（`DAT_…`），没读 `.rdata`；这两个键是 R1 按
        /// 「语义 + 实参个数」从**官方 13 语表**里挑的**唯一候选** ⇒ 属**推断**，不是直读。
        /// （判别力在第二支：官方表里带 `{0} {1}` 的 Ranked 空态句只有它。）</para></summary>
        string NoDeckMessage()
        {
            int n = ArmyDeckCount();
            if (n < 1) return Loc.T("MainMenu/Ranked/NoWarlordMessage");
            // ⚠️ `{1}`（阵营名）先填、`{0}`（数字）后填：数字里不会有花括号，阵营名更不会有。
            // 🔴 **2026-10-25（`P6` · 铁律 5）：`{1}` 用【短名】** —— 原版 `RankedDeckSelector.ChangeDeck`
            //   这一句是 `ArmyUtilities.GetLocalizedArmyName(currentArmy, useShortName: **true**)`
            //   （`cpp2il/RankedDeckSelector.cs:645`；实参逐个核过，原版 5 个调用点里只有这一处传 `true`）。
            //   改前这里写的是 `CardText.Faction(...)` = **全名** ⇒ 与原文不符。词条键 = `Armies/<Faction>_short`，
            //   官方表里只有 6 条（Goff / Leviathan / Sautekh / Sororitas / Genestealers / AstraMilitarum），
            //   其余阵营回落全名（`ArmyIcons.LocalizedName` 的短名支；与原版「取不到就回落」同）。
            //   ⚠️ 中文档下 **6 条里 4 条真的更短**（利维坦虫巢舰队→利维坦虫 · 索泰克王朝→索泰克 ·
            //   修女会→修女 · 基因窃取者教团→基因窃取者）；`Goff`（欧克蛮人）与 `AstraMilitarum`（星界军）
            //   两条的 `_short` 与全名**逐字相同** ⇒ 这两个阵营在本句里**无视觉差异**（英文列同理）。
            return Loc.T("MainMenu/Ranked/NoDeckMessage")
                      .Replace("{1}", ArmyIcons.LocalizedName(SelectedArmyFaction, true) ?? SelectedArmyFaction)
                      .Replace("{0}", n.ToString());
        }

        /// <summary>选中的那副牌**能不能用在本窗模式下**；不能时 <paramref name="why"/> 是人话。
        ///
        /// 原版同款判据：`SkirmishEventWindow.OnDeckSelected` → `RankedDeckSelector.HasValidDeckWithValidationMessage`
        /// → `DeckUtility.ValidateDeck(deck, out err, isSkirmish)`（出处 → `资料/加时与冲突模式_原版规格.md` §2.3）。
        /// ⚠️ **两条路都要判**：预组那副（`PickedPrebuilt`）与「我的卡组」那副 —— 预组页是**两种模式混在一页**列出来的
        /// （照原版 66 副），所以在遭遇窗里点一副经典预组是**点得到**的。</summary>
        public bool SelectedDeckFitsMode(out string why)
        {
            var pre = PickedPrebuilt;
            if (pre != null)
            {
                if (pre.gameMode == DeckGameMode) { why = null; return true; }
                // 整句走词条（键 `MenuDeck/Error/WrongGameMode`）。
                // ⚠️ 两个替换值**都来自 `Loc`**（词条正文里不会有花括号）⇒ 次序无所谓；这一处照「高位先填」。
                // 🔴 **2026-10-22（`W63`）**：`{0}` 用的 `MenuDeck/GameMode/{Skirmish,Classic}Tag` 是**自拟键、非原版**
                //   —— 官方两张本地化表里**没有** `*Tag*` 类键（`MainMenu/GameMode/*` 只有
                //   `Classic` / `Skirmish` / `Practice` 三条；`MenuDeck/*` 112 条里没有 `MenuDeck/GameMode/*`
                //   这一族 —— 唯一带 Mode 的 `MenuDeck/SelectDeckGameMode` 是「选择游戏模式」的标题，不是模式名）
                //   ⇒ 按「⛔ 不另造新键」**沿用** `Core/Loc.cs:2073-2074` 那两条
                // 🔶 自己设计（非原版）：模式名短形状「遭遇 · 12 张」/「经典 · 30 张」 · 官方两张本地化表里没有 `*Tag*` 类键（原版没有这一族键） · TODO(谁定的)｜原注：（值 = 「遭遇 · 12 张」/「经典 · 30 张」，属于我们自拟的短形状，不是原版词条）。
                why = Loc.T("MenuDeck/Error/WrongGameMode")
                          .Replace("{1}", DeckGameModeName)
                          .Replace("{0}", pre.gameMode == (int)GameMode.Skirmish
                                           ? Loc.T("MenuDeck/GameMode/SkirmishTag")
                                           : Loc.T("MenuDeck/GameMode/ClassicTag"));
                return false;
            }
            var raw = CollectionData.Raw(DeckIndex);
            if (raw == null)
            {
                // 整句走词条（键 `MenuDeck/Error/NoDeckForMode`）。
                why = Loc.T("MenuDeck/Error/NoDeckForMode").Replace("{0}", DeckGameModeName);
                return false;
            }
            if (DeckFitsMode(raw)) { why = null; return true; }
            // 整句走词条（键 `MenuDeck/Error/WrongGameModeDeck`）。
            // 🔴 **`{0}` 必须最后填**：`Replace` 会**再扫一遍已填入的内容**，而 `{0}` 是**玩家数据**（卡组名，
            //    用户随便起）—— 只有「最后填」才能保证名字里万一出现 `{1}`/`{2}` 也不会被当成占位符换掉。
            //    （`{2}`/`{1}` 的值来自 `Loc`，词条正文不含花括号。）
            // ⚠️ **2026-10-22（`W63`）**：`{1}` 的 `MenuDeck/GameMode/{Skirmish,Classic}Tag` 与上面
            //    `WrongGameMode` 那条同源 —— **自拟键、非原版**（官方两张表没有 `*Tag*` 类键）
            //    ⇒ 沿用 `Core/Loc.cs:2073-2074`，⛔ 不另造新键。
            why = Loc.T("MenuDeck/Error/WrongGameModeDeck")
                      .Replace("{2}", DeckGameModeName)
                      .Replace("{1}", raw.IsSkirmish ? Loc.T("MenuDeck/GameMode/SkirmishTag")
                                                     : Loc.T("MenuDeck/GameMode/ClassicTag"))
                      .Replace("{0}", raw.Name);
            return false;
        }

        /// <summary>`Battle!` 的图：遭遇战 `WF_UI_Trophy_Gold` · 排位 `40k_ranking_icon_trophy_Plus`。</summary>
        protected abstract string TrophyIconArt { get; }
        /// <summary>背景那一族 —— 两扇窗**rect 不同**（排位多一层 `General Red Background`，三个子层的值也不一样）。</summary>
        protected abstract void BuildBackdrop(Transform root);
        /// <summary>左列 —— 遭遇战是 `Reward Display`（进度），排位是 `Ranked Division Info`（分段）。</summary>
        protected abstract void BuildLeftColumn(Transform root);

        /// <summary>取图 + **记账**。
        /// 🔴 为什么要有它：`MenuDraw.Rect/Nine/Tiled` **取不到图就 `return null`，一声不吭** ——
        /// 整层静默消失（`MissingArt` 原来是**只 `Clear` 从不 `Add` 的死字段**，2026-09-24 找茬抓到）。
        /// 本窗所有取图都走这里；`MainMenuScene` 那条「图一张都不能少」的断言会自动带上它。</summary>
        protected Texture2D Tex(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var t = CardArt.MenuUi(name);   // ⚠️ **这里必须是 `CardArt.MenuUi`** —— 写成 `Tex` 就是无限递归
            if (t == null && !MissingArt.Contains(name)) MissingArt.Add(name);
            return t;
        }

        /// <summary>`Header/Game Mode Icon` 用哪张图（**本地只有 classic / skirmish 两张** ⇒ 给 null 时那边不画并出声）。</summary>
        protected virtual string GameModeIconArt { get { return null; } }

        /// <summary>四颗圆钮里图标那块的框：原版 `Icon` 子件 = 钮内居中 **120.49×93.01**。用整颗钮当框会把图标放大 1.376 倍。</summary>
        protected static PxRect IconBox(float x1, float y1, float x2, float y2)
        {
            const float iw = 120.49f, ih = 93.01f;
            float cx = (x1 + x2) * 0.5f, cy = (y1 + y2) * 0.5f;
            return new PxRect(cx - iw * 0.5f, cy - ih * 0.5f, cx + iw * 0.5f, cy + ih * 0.5f);
        }

        /// <summary>窗口参数（**逐窗实测**：都是 `type=1 Popup` · `placement=5 Canvas` · `closeOnESC=1` ·
        /// `extraScaleSmallScreen=1.0`）。</summary>
        protected static T Init<T>(T win, WindowsManager mgr) where T : LiveOpsEventWindow
        {
            win.type = WindowType.Popup;                     // 实证 type=1
            win.placement = WindowsPlacement.Canvas;         // 实证 windowsPlacement=5（**不是** 15）
            win.closeOnEsc = true;                           // 实证 closeOnESC=1
            win.extraScaleSmallScreen = 1f;                  // 实证 1.0
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            return win;
        }

        public override void Open()
        {
            LastOpened = this; LoadEventDeckSlots();   // 2026-10-28（B92_N9）：每活动卡组槽跨进程持久化（判据 → 类尾）
            // 🆕 **2026-10-24（E11 · P2 + P0-2）**：照原版 `RankedDeckSelector__Initialize.c:15-22` 那条链，**顺序不能反**：
            //   ① 选中阵营 = **当前卡组督军的阵营**；**只有当前卡组为空**才回落 `10 = Ultramarines`
            //      （`if (currentDeck == 0) army = 10; else army = currentDeck.Army;`）；
            //   ② 再由该阵营走 `ChangeArmy` ⇒ 在「**阵营 ∧ 本窗模式**」的表里取 `FirstOrDefault`。
            //   🔴 改前是「按模式吸附到第一套、再 `ArmyIndex = -1`」—— 于是小冲突窗会画出**经典**卡组那一套
            //      （本机只有一套 `ASH1` 经典 ⇒ 用户看到「恒是灵族」）。
            //   ⚠️ 吸附**只在同一阵营内**（原版的表本来就带阵营条件），⛔ 不再跨阵营吸附。
            int cur = EventDeckIndex();     // 🆕 B90-N3：原版这一格 = `currentEvent.GetDeck()`（每活动各记各的）
            string fac0 = cur >= 0 ? FactionOfDeck(cur) : null;
            _armyFactionSel = string.IsNullOrEmpty(fac0) ? DeckRuntime.NoArmyFaction : fac0;
            DeckIndex = FirstDeckInMode(cur);        // 表空 ⇒ **−1 = 空态**（⛔ 不回落到别的模式 / 别的阵营）
            ArmyIndex = -1;                  // 内部分下标；原版没有「不限」这一档，BuildArmySelector 会解析
            Build();
            // ⚠️ **每次开窗都要重建它** —— `Build()` 会把根下的子件全清掉（含上一次那个搜索弹窗）。
            // 🔴 **2026-10-22（W64）：`hostDrivesItself: false`** —— 本族宿主**可能被全屏窗整棵藏起来**
            //    （排位那条：`SearchingOpponentWindow` 是 `type=0 Fullscreen` ⇒ `OpenWindow` 的全屏支
            //     `HideAllWindows()` ⇒ `GameWindow.Hide()` ⇒ `SetActive(false)` ⇒ Unity 不再派 `Update`）
            //    ⇒ 推进交给常驻 `SearchOpponentManager`（判据 = 原版 `SearchingOpponentWindow` 没有 `Update`，
            //    倒计时在 `SearchOpponentManager._WaitForHumanOpponent` 的协程上）。
            _search = SearchingMatchPopup.Attach(transform, "Searching Oponent Popup (1)", false);
            _search.OnCancel = CancelSearch; ScheduleExpiryRefresh();   // 原版 OnRefresh 的本地触发点（事件到期）
        }

        /// <summary>关窗时**顺手把匹配停掉** —— 原版那个弹窗 `ESCPressed()` 就是 `Close()` **+ `CancelSearch()`**
        /// （`SearchingMatchWindowDemo__ESCPressed.c`）；不这么做，ESC 关掉宿主窗之后搜索还挂着
        /// （宿主 GO 关掉 `Update` 会停，但状态没清、窗口再开还能看到旧进度）。</summary>
        public override void Close()
        {
            if (_search != null) _search.Cancel(); ReleaseExpiryRefresh();   // 到期刷新那一班要跟着撤（同 `_search` 的口径）
            base.Close();
        }

        // ============================================================ 建整棵树
        protected void Build()
        {
            // 🔴 **2026-10-18（A867 · S1）**：**首句**就把本窗名下的旧滚动区撤掉 —— 下一句就把 `root` 的子件
            //   全清掉，而下面 `BuildArmySelector` 会登记一颗**新**的 `MenuScroll`（`Owner = gameObject`）。
            //   ⛔ **兜不住**，所以非补不可（`PointerLayer` 的两道自动清理**都判不出这里已死**）：
            //     · `PruneScrolls` 的 `s == null` —— `MenuScroll` 是**普通 C# 类**（`Shell/MenuScroll.cs`）
            //       ⇒ 节点被销毁**不会**让它变 null ⇒ **恒假**；
            //     · `s.Owner == null` —— `Owner` 指的是**活下来的窗根**（本方法只删**子件**，`transform` 本身还在，
            //       而且我们关窗是 `SetActive(false)`、**不销毁**）⇒ 也**恒假**（`Shell/PointerLayer.cs` 的 `SetActive(false)`）。
            //   ⇒ 少了这一句 = **每重建一次净涨 1 条**，而且旧条目**还能被滚轮命中**（`OnChanged` 指向已销毁的节点）。
            //   ✅ 形状照抄 `Shell/InboxWindow.cs`。⚠️ 本窗这个 `Build()` 会被**同一实例**再走一遍
            //   （`Open()` 调的：`TryOpen` 从 `Closed` 支 ⇒ `Open()`，`Shell/WindowsManager.cs`；
            //    生产侧到达它的是 `ShowPreviousWindow` ⑥ 那句 `prev.TryOpen()`，`CardPresentation/Shell/WindowsManager.cs`）。
            PointerLayer.UnregisterOwnedBy(gameObject);
            var root = transform;
            MissingArt.Clear();
            for (int i = root.childCount - 1; i >= 0; i--) RewardsWindow.DestroySafe(root.GetChild(i).gameObject);
            ArmyCells.Clear();

            BuildBackdrop(root);
            BuildDeckSelection(root);
            BuildArmySelector(root);
            BuildLeftColumn(root);
            BuildHeader(root);
            BuildToBattle(root);
            BuildHelp(root);
            BuildExtras(root);        // 子类独有的那几件（基类不知道它们）
        }

        /// <summary>子类在本窗独有的件（不覆写 `Open` —— 那个顺序很容易错）。
        /// 遭遇战：`Timer` · `Banned card in deck`；排位：`ChangeRankedToggle`。</summary>
        protected virtual void BuildExtras(Transform root) { }

        // ------------------------------------------------------------ 卡组那一栏的**显示判源**
        /// <summary>卡组那一栏（`Ranked Deck Selection`）**显示**的是哪一副 —— 🔴 **预组优先**。
        ///
        /// <para>🔴 **2026-10-21 新增（真缺陷）**：本窗原来**没有这一层** —— `BuildDeckSelection` /
        /// `BuildDeckTexts` 四处一律读 `CollectionData.DeckAt(DeckIndex)` / `CollectionData.Warlord(DeckIndex)`，
        /// 而**预组不在 `DeckLibrary` 里**（`DeckIndex` 永远指不到它）⇒ 在「选卡组」弹窗里挑一副预组之后，
        /// 这一栏画的还是**上一副自建卡组**（名字 / 督军 / 阵营徽记三样全错）＝ **面板显示与开战实际用的牌不是同一副**。
        /// 判据：`OpenDeckSelection` 的回调（预组支）只写了 `PickedPrebuilt` + `PrebuiltDecks` 通道，**没有重建这一栏**。</para>
        ///
        /// <para>⚠️ 与 `PracticeModePopup.CurrentDeck`（2026-10-21 收成的「唯一判源」）是**同一口径的两处实现**：
        /// 那边返回 `PlayerDeck`（供卡表 / 费用曲线用），本窗这一栏只要**名字 / 阵营 / 督军**三样
        /// ⇒ 这里是**显示用**的薄解析，**不是**第三个「这局用哪副牌」的判源 —— 开战那条路读的仍然是
        /// `PrebuiltDecks.PendingSource`（`StartMatch` 里那句），两者由回调 / `PickDeck` 成对写入。
        /// ⛔ 想合并成一份就得把它抬到共用件里去改 `PracticeModePopup` —— 本笔**不动那个文件**。</para></summary>
        PrebuiltDecks.Deck ShownPrebuilt { get { return PickedPrebuilt; } }

        /// <summary>这一栏的督军卡：预组 = 它自己的 `heroId` 那张；自建 = `DeckIndex` 那副的督军。取不到 ⇒ null。</summary>
        CardDef ShownWarlord
        {
            get
            {
                var pre = ShownPrebuilt;
                return pre != null ? CollectionData.Card(pre.heroId) : CollectionData.Warlord(DeckIndex);
            }
        }

        /// <summary>这一栏的卡组名：预组 = `DisplayName`（官方 zh-CN 优先，同 `PrebuiltDecks` 那条口径）；
        /// 自建 = 卡组库里那副的名字（没有 = 空串）。</summary>
        string ShownDeckName
        {
            get
            {
                var pre = ShownPrebuilt;
                return pre != null ? pre.DisplayName : CollectionData.DeckAt(DeckIndex).Name;
            }
        }

        /// <summary>这一栏的阵营：预组 = 它自己的 `faction`；自建 = 督军的阵营（没有督军 = 空串）。</summary>
        string ShownFaction
        {
            get
            {
                var pre = ShownPrebuilt;
                return pre != null ? pre.faction : CollectionData.DeckAt(DeckIndex).Faction;
            }
        }

        // ------------------------------------------------------------ 中间那一栏：卡组选择
        /// <summary>`Ranked Deck Selection`（**两扇窗逐格相同**）。
        /// 🔴 三层的先后**照原版的兄弟序**：`Faction Icon Image` → `Warlod Image`（里面再叠 `Warlord Darkening`）
        ///    → 文字/按钮。渲染队列按这个次序递增（同队列谁盖谁不确定 —— 2026-09-23 踩过）。</summary>
        void BuildDeckSelection(Transform root)
        {
            var col = MenuDraw.Node(root, "Ranked Deck Selection",
                                    new PxRect(DeckSelL, DeckSelT, DeckSelR, DeckSelB));

            // 🔴 **2026-10-21**：判源走 `ShownFaction` / `ShownWarlord`（预组优先，见那两个属性）——
            //   原来这两处一律读 `CollectionData.*(DeckIndex)`，选完预组时画的是**上一副自建卡组**。
            // 🔴 **2026-10-24（E11）**：整栏的总闸 = `HasEffectiveDeck`（= 原版 `ChangeDeck` 的 `deck != null`）。
            bool hasDeck = HasEffectiveDeck;

            // 阵营大徽记 —— 🔴 **原版只在空态显示**（`ChangeDeck.c:80-83`：`SetActive(deck == null)`），
            //   图 = `ArmyUtilities.GetArmyIcon(currentArmy)` ⇒ **选中阵营**那一张
            //   （⛔ 不是卡组的阵营、⛔ 不是 `DeckRuntime.FactionIcon(null)` 那个「卡组图标」兜底）。
            //   有卡组时节点**照建**（原版也是同一颗节点、只是关着；图仍是上一次写进去的那张 ⇒ 我们照旧喂卡组阵营）。
            string facIconArt = hasDeck
                ? DeckRuntime.FactionIcon(ShownFaction)
                : (ArmyIcons.IconName(SelectedArmyFaction) ?? DeckRuntime.FactionIcon(SelectedArmyFaction));
            var facQ = MenuDraw.Rect(col, Tex(facIconArt),
                                     new PxRect(FacIconL, FacIconT + FacIconDropY,
                                                FacIconR, FacIconB + FacIconDropY),
                                     "Faction Icon Image", QDeck, null, true);
            if (facQ != null) facQ.gameObject.SetActive(!hasDeck);

            // 督军立绘（原版 `sprite=0` ⇒ 运行时喂；**槽是 1098.14² 的正方形**，原版喂的也是方形整身立绘）
            // 🔴 **2026-10-22（M1 · 真机缺陷，用户原话「战将的立绘是残缺的，你是否使用正确的图片」）**：
            //   原来这里走 `CardArt.Portrait`（**卡面插图，竖构图 ~671×1024**）塞进 1098.14² 的方框，
            //   等比放到 1098 宽 ⇒ 高 ~1675，**上下各溢出 ~288px** ⇒ 人被截断、还把标题栏盖掉。
            //   正解 = `CardArt.DeckHero`：`UI_Deck_Warlord_<督军名>`（**2048² 方形整身立绘**、
            //   `bundle_uiwarlords_assets_all`）—— 与 `Shell/DeckInfoPopup.cs` 的 `Warlord Image` 同一条口径
            //   （记忆 `warpforge-deck-warlord-art`：**方形立绘只给 UI 立绘位**；卡组格才用 `CardArt.Portrait`）。
            // ⚠️ **2026-10-22（界面线 M3）**：这里原来按**纹理名前缀** `UI_Deck_Warlord_` 猜「是不是拿到了方形图」——
            //   那条判据在 `DA75` 那种资产名**没有** `_Warlord_` 前缀（`UI_Deck_Master Zacharial`）的档上会误判。
            //   ⇒ 改走 `CardArt.TryDeckHero`（**可判别**的两档：true = 真拿到方形立绘，false = 没有）。
            //   退回时**出声**（⛔ 不静默），并说明那一路是「我们挑的兜底」。
            // 🔴 **2026-10-24（E11）**：**空态不画督军** —— 判据 = `ChangeDeck.c` 的 `warlordImage(0x78)`
            //   `SetActive(deck != null)`。改前这里是「有督军就画」⇒ 小冲突窗会画出一套**经典**卡组的督军
            //   （本机存档只有那一套 `ASH1` ⇒ 用户看到的「恒是灵族」）。
            var wl = hasDeck ? ShownWarlord : null;
            var wlRoot = MenuDraw.Node(col, "Warlod Image", new PxRect(WlL, WlT, WlR, WlB));
            Texture2D wlTex = null;
            bool squareHero = wl != null && CardArt.TryDeckHero(wl.Id, out wlTex) && wlTex != null;
            if (wl != null && !squareHero)
            {
                // 兜底（**我们挑的**）：方形立绘没有 ⇒ 退回卡面插图，等比内接。
                wlTex = CardArt.Portrait(wl.Id);
                if (wlTex != null)
                    Debug.LogWarning("[Event] 督军「" + wl.Name + "」**没有方形整身立绘**"
                                     + "（`CardArt.TryDeckHero` = false）⇒ 退回卡面插图等比内接 —— "
                                     + "**这一档是我们挑的兜底**，不是原版做法（原版喂的是 2048² 方形立绘）");
            }
            if (wlTex != null)
            {
                // 原版那件 `m_PreserveAspect = 0`（直接拉满 1098.14²）。**方形图拉满 ≡ 等比**（2048²→1098²），
                // 两种写法同解 ⇒ 统一 `keepAspect = true`：方形档零差异，退回的竖构图档才不会拉变形。
                // （`squareHero` 只用于出声；两档的**画法**差异就由贴图自身的宽高比决定 = `keepAspect`。）
                MenuDraw.Rect(wlRoot, wlTex, new PxRect(WlL, WlT, WlR, WlB), "Warlord", QDeck1, null, true);
            }
            else
                Debug.Log("[Event] 督军立绘取不到（" + (wl != null ? wl.Name : "这套卡组没有督军") + "）—— 那一层不画，出声");
            //    `Warlord Darkening`：压暗一层（`Smooth background square` Sliced · col(0,0,0,0.816)）
            MenuDraw.Nine(wlRoot, Tex(ArtWarlordDark),
                          new PxRect(WlDarkL, WlDarkT, WlDarkR, WlDarkB), SmokeBorder, SmokeTexW, SmokeTexH,
                          QDeck2, new Color(0f, 0f, 0f, 0.816f), true, "Warlord Darkening");
            // 🔴 **2026-10-24（E11）**：整根立绘（含 `Warlord Darkening`）跟着 `hasDeck` 开关 ——
            //   原版 `warlordImage.SetActive(deck != null)`，`Warlord Darkening` 是它的子件 ⇒ 一起走。
            //   ⚠️ 先建后关：节点/图/矩形都留在树里（结构断言照旧认得到），只是**不上屏**（`activeSelf = false`）。
            wlRoot.gameObject.SetActive(hasDeck);

            BuildDeckButtons(col);
            BuildDeckTexts(col);
            BuildArmyDeckCount(col);
        }

        /// <summary>`Smooth background square`：**32×32 · border (12,2,12,12)**（`Sprite/*.json` 实测；
        /// `bundle_duplicateassetisolation_assets_all` 与 `sharedassets0` 两份同值）。
        /// 🔴 **2026-09-24 更正**：原来这里写 1×1 / border 全 0 还注释「从 `Sprite/*.json` 抄」——
        /// **那是我编的**（找茬子代理抓到的：四边不保、整张拉伸）。现已填真值。</summary>
        static readonly Vector4 SmokeBorder = new Vector4(12f, 2f, 12f, 12f);
        const float SmokeTexW = 32f, SmokeTexH = 32f;

        /// <summary>四颗圆钮：**上一套 / 换卡组 / 看卡组 / 下一套**（HLG spacing 0 · align MiddleCenter · 每颗 160.87×128）。
        /// 图标顺序照原版：`40k_UI_bt_back` · `40k_UI_bt_deck_change` · `40k_bt_eye` · `40k_UI_bt_back`(**水平翻转**)。
        /// <para>🔴 **2026-10-24（E11 · P1）**：**四颗不是恒画的** —— 原版 `RankedDeckSelector__ChangeArmy.c`
        /// 按「选中阵营 ∧ 本窗模式」的表长 `Count` 逐个关：
        /// `previousDeckButton(0x20)` / `nextDeckButton(0x28)` = `SetActive(1 &lt; Count)` ·
        /// `selectDeckButton(0x30)` / `viewDeckButton(0x38)` = `SetActive(0 &lt; Count)`
        /// ⇒ 0 套 = 四颗全关 · **1 套 = 只留「换卡组 / 看卡组」两颗**（参考图 1/1 那张正好两颗）·
        /// ≥2 套 = 四颗全开（老参考图 4/14 那张四颗）。
        /// 居中照原版 `HorizontalLayoutGroup`：**布局组跳过关着的子件** ⇒ 只按**开着的那几颗**从左到右排、
        /// 整排仍以栏心居中（`total = 160.87 × 可见颗数`）。</para></summary>
        void BuildDeckButtons(Transform col)
        {
            var bar = MenuDraw.Node(col, "Deck Buttons", new PxRect(DbL, DbT, DbR, DbB));
            float cx = (DbL + DbR) * 0.5f, cy = (DbT + DbB) * 0.5f;
            int armyCount = ArmyDeckCount();
            // 0 = 上一套 · 1 = 换卡组 · 2 = 看卡组 · 3 = 下一套（顺序 = 原版 HLG 的树序）
            bool[] shown = { armyCount > 1, armyCount > 0, armyCount > 0, armyCount > 1 };
            int visible = 0;
            for (int i = 0; i < 4; i++) if (shown[i]) visible++;
            float total = DbBtnW * visible;
            string[] icons = { ArtBackArrow, ArtDeckChange, ArtEye, ArtBackArrow };
            System.Action[] acts =
            {
                () => StepDeck(-1),
                () => ChangeDeckButtonClick(),
                () => ViewDeckButtonClick(),
                () => StepDeck(+1),
            };
            string[] names = { "Previous Deck Button", "Change Deck Button", "View Deck Button", "Next Deck Button" };
            int slot = 0;
            for (int i = 0; i < 4; i++)
            {
                // 关着的那几颗**照建**（结构/断言仍认得到，`PointerLayer` 明确跳过 `!activeInHierarchy` 的命中件
                // ⇒ 不会在旧位置偷吃点击），只是不占布局槽位。
                float x1 = cx - total * 0.5f + DbBtnW * slot, x2 = x1 + DbBtnW;
                float y1 = cy - DbBtnH * 0.5f, y2 = cy + DbBtnH * 0.5f;
                if (shown[i]) slot++;
                var b = MenuDraw.Node(bar, names[i], new PxRect(x1, y1, x2, y2));
                var dbBg = MenuDraw.Rect(b, Tex(ArtRoundBtn), new PxRect(x1, y1, x2, y2), "Bg", QArt, null, true);
                var icon = Tex(icons[i]);
                // 🔴 图标必须**高于**底图那一层（同队列谁盖谁不可控 —— 第一版就是这样，实拍里后三颗是空的）
                // ⚠️ **图标框不是整颗钮**：原版 `Icon` 子件是 **120.49×93.01**、在 160.87×128 的钮里居中
                //    （`preserveAspect` ⇒ 实画 94.08×93.01）。用整颗钮当框会把图标放大 **1.376 倍**
                //    —— 2026-09-24 找茬子代理按锚点比例算出来的（`menu_rect --depth 5` 那一行）。
                var iconQ = MenuDraw.Rect(b, icon, IconBox(x1, y1, x2, y2), "Icon", QArt1, null, true);
                // 🔴 原版 `Next Deck Button` 的图标挂 `UIFlippable`（水平翻转成「→」）。
                // 🆕 **2026-10-03 补上了**：原来这里写着「`ImageQuad` 没有翻转开关 ⇒ 出声」—— **那句是错的**：
                //    **`SetUvRect` 传一个负宽就是镜像**，工程里早有先例（`Shell/CollectionWindow.cs` 的 `BuildStyleArrow` 里那句 `SetUvRect(new Rect(1f, 0f, -1f, 1f))`
                //    的 `new Rect(1f, 0f, -1f, 1f)`、`Shell/MatchLogRow.cs` 里那句 `SetUvRect(new Rect(1f, 0f, -1f, 1f))` 同款翻左/右箭头）。
                if (i == 3 && iconQ != null) iconQ.SetUvRect(new Rect(1f, 0f, -1f, 1f));   // UIFlippable ⇒ 水平镜像
                // 🆕 A17：原版这四颗是 SpriteSwap，**高亮图逐颗不同**
                //（`40k_UI_bt_back_hover` / `40k_UI_bt_deck_change_hover` / `40k_bt_eye_hover` / 末颗又是 `40k_UI_bt_back_hover`）
                // ⇒ 按**图标那张图的名字**推（命名规律 `<常态图>_hover`）。
                // 🔴 **2026-10-18 更正（A1058 · 第六会话批 2 · 铁律 5）**：换图那一层 = **子件 `Icon`**
                //    （画的是 `icons[i]`，= 下面那颗 `iconQ`），⛔ **不是圆底盘 `dbBg`**。
                //    判据（原版 prefab 亲读）=
                //    `python -I d:/tmp/wf_hit/rcunion.py bundle_menus_assets_all "SkirmishModeEventWindow" --depth 8 --sub "Deck Buttons/Previous"`：
                //    根 `Previous Deck Button` 那颗 `EverguildButton` 的 **`m_TargetGraphic` = pid5832333277041741170`**；
                //    解该 pid（`python -I d:/tmp/wf_hit/tgt.py bundle_menus_assets_all 5832333277041741170`）⇒
                //    **所属 GO 名 = `Icon`**、贴图 = `40k_UI_bt_back`。
                //    ⚠️ **2026-10-18 就地订正**：本行上面的注释（与它引的「普查 §块 4 第 15 行」）
                //    说「换图落在 `Bg` 那一层」—— **那句是错的**（错因 = 只读 `m_Transition`、没读 `m_TargetGraphic`）。
                //    改前的症状：悬停把**整块圆底盘**换成**箭头的高亮图**（`UI_Button_Round_background` → `40k_UI_bt_back_hover`）。
                //    ⚠️ `art` 实参本来就是图标那张常态图的名字 ⇒ 不用动（上面那三行「按图标名字推」是对的）。
                // 🆕 **2026-10-18（A1053）**：**命中区 = 可射线件的并集** —— 本钮根上那颗 `Image` 实测
                //    `m_RaycastTarget = 0`（不吃射线），子树里**只有 `Icon` 一颗吃射线**
                //    ⇒ 可点区 = `Icon`（120.49×93.01）按自己的 `m_RaycastPadding (-20)⁴` 外扩
                //    = **160.49 × 133.01**（⛔ 不是整颗钮 160.87×128）。
                //    判据 = `python -I d:/tmp/wf_hit/rcpad.py bundle_menus_assets_all "SkirmishModeEventWindow"
                //    --depth 8 --substr "Deck Buttons/Previous"`（实读 `160.49 x 133.01`）。
                MenuDraw.Hit(b, "Hit", MenuDraw.PaddedRect(IconBox(x1, y1, x2, y2), IconPad),
                             QHit, acts[i], iconQ, icons[i], clickCue: WindowSounds.DefaultCloseCue);
                // 🔴 原版那四跳 `SetActive(…)` —— 放在**最后**（TMP/quad 都要在父节点还活着时建好）。
                b.gameObject.SetActive(shown[i]);
            }
            Debug.Log("[Event] `Deck Buttons`：选中阵营「" + SelectedArmyFaction + "」∧ 本窗模式有 "
                      + armyCount + " 套 ⇒ 显示 " + visible + " 颗"
                      + "（原版 `ChangeArmy`：上一套/下一套 `1 < Count`、换卡组/看卡组 `0 < Count`）");
        }

        void BuildDeckTexts(Transform col)
        {
            // 🔴 **2026-10-21**：判源走 `ShownDeckName` / `ShownWarlord`（预组优先）——
            //   原来这两处读 `CollectionData.*(DeckIndex)`，选完预组时名字/督军停在上一副自建卡组上。
            // 🔴 **2026-10-24（E11）**：判据换成 `HasEffectiveDeck`（= 原版 `deck != null`），⛔ **不是** `wl != null`。
            //   改前是「有督军就画」⇒ 小冲突窗会画出**经典**卡组那一套（本机只有一套 `ASH1` ⇒ 用户看到「恒是灵族」）。
            var wl = ShownWarlord;
            bool hasDeck = HasEffectiveDeck;
            // 🔴 **对齐照修好的 `menu_dump.py` 重导**（原来那份的 `hAlign` 映射错位一位：
            //    真枚举是位标志 `Left=1 · Center=2 · Right=4 · Flush=16` —— 见 `资料/已知的坑.md`）。
            //    这两行原版是 **Center** ⇒ 不调 `Align*`（`MenuDraw.Text` 默认就是居中）。
            // 🔴 **2026-10-09（A1126 · A2 档）**：这两颗原来**没传 autosize 实参** ⇒ 固定 62.85 / 44.65px。
            //   判据 = 原版 `menus ▸ SkirmishModeEventWindow ▸ Ranked Deck Selection/{Deck Name, Deck Warlord}`
            //   （逐颗现读 TMP 字段）：**两格 `m_enableAutoSizing = 1`** · `m_fontSizeMin = **18**` ·
            //   `m_fontSizeMax = **72**` · `m_fontSizeBase = **36**` · 折行 = 1。
            //   ⚠️ 本窗刻度 = 原版刻度（下面 `No Deck Text` 那处传的就是原版原值 `18/45/36`，"原文"逐字）
            //   ⇒ 四格照抄、**不做比例换算**。
            _txtDeckName = MenuDraw.Text(col, new PxRect(DnL, DnT, DnR, DnB),
                                         hasDeck ? ShownDeckName : "", Color.white,
                                         "Deck Name", 62.85f, QText, DnR - DnL, 18f, 72f, 36f, face: TmpFont.Face.LatinUi);
            _txtDeckWarlord = MenuDraw.Text(col, new PxRect(DwL, DwT, DwR, DwB),
                                            (hasDeck && wl != null) ? wl.Name : "", Color.white, "Deck Warlord", 44.65f, QText,
                                            DwR - DwL, 18f, 72f, 36f, face: TmpFont.Face.LatinUi);
            // 🔴 原版 `deckName(0x50)` / `warlordName(0x48)` 与那四件同一条 `SetActive(deck != null)`（`ChangeDeck.c:64-79`）
            //   ⇒ 空态两颗都关（节点照建，结构断言仍认得到）。
            if (_txtDeckName != null) _txtDeckName.gameObject.SetActive(hasDeck);
            if (_txtDeckWarlord != null) _txtDeckWarlord.gameObject.SetActive(hasDeck);

            // `No Deck Text`（+ `Create deck` 那颗钮）—— 原版 `NoDeckText` 那一族，**只在空态显示**
            var none = MenuDraw.Node(col, "No Deck", new PxRect(NoDeckL, NoDeckT, NoDeckR, NoDeckB));
            var ndt = MenuDraw.Text(none, new PxRect(NoDeckL, NoDeckT, NoDeckR, NoDeckB),
                                    NoDeckMessage(), Color.white,
                                    "No Deck Text", 45f, QText, face: TmpFont.Face.LatinUi);
            // ⚠️ 我们这句话比原版长（24 字 × fs45 ≈ 1080px）⇒ **会溢出 685.65 的框** ⇒ 自己缩着放进去
            //    （原版那句带 `auto 18-45`，我们照它的区间自缩）——**这一条是我们挑的**
            // 🔴 **2026-10-11（A305①）**：第 5 个实参 = 原版 `No Deck Text` 的 `m_fontSizeBase` **原文**。
            //    判据（本轮自己扫 `bundle_menus_assets_all`，按**文案**认节点）：原版那颗的 `m_text` 是
            //    西班牙语串 `'Tienes <color=#E98A00FF>{0} {1} Comandan…'`（正是下面那条注释说的占位串）——
            //    `m_fontSize 45` · `auto[18~45]` · **`m_fontSizeBase 36.0`**（3 颗同族全是 36.0，
            //    例 `MonoBehaviour_-1328164060620678543.json`）⇒ 区间 (18,45) 是原版的、base 也是。
            //    ⚠️ V7 §二·3 #28 那一行记的是**全库** `[18~45]` 那族（87 个）的 base 分布（36/31.38/14 三值并存），
            //    本条按**这一颗**的实读取 36。文案本身**是我们写的**（本地没有术语表/计数，见下），
            //    但「用哪个区间、base 是多少」照原版。
            if (ndt != null) ndt.SetAutoFitBox(LayoutSpace.Px(NoDeckR - NoDeckL), LayoutSpace.Px(NoDeckB - NoDeckT), 18f, 45f, 36f);
            // 🆕 **2026-10-24（E11）**：文案改走**官方词条**（两支，见 `NoDeckMessage()`）——
            //   ⚠️ 原来这里印的是我们自拟的「这套卡组还没有战将…」（键 `MenuDeck/HUD/NoWarlordText`），
            //   而原版那两句**在官方 13 语表里就有**（`bundle_menus_assets_all` 的 prefab 出厂串正是西语占位版）。
            //   ⛔ 别再改回自拟句：`MenuDeck/HUD/NoWarlordText` 那条键**没删**（别处/历史断言还认它），但本处不用了。
            // ⚠️ 文字挂在**那颗钮底下**（原版 `Button Text` 是钮的子件；直接挂容器会让 `FindChild(钮,"Button Text")` 找不到）
            // 🔴 **2026-10-24（E11 · P3）**：这里原来**多建了一颗** `Generic Simplified UI Button`（先 `MenuDraw.Rect`
            //   又在下面 `MenuDraw.Node` 同名建一遍）⇒ 同一位置两套底板 + 同名子件（`FindChild` 会歧义）。
            //   原版只有一份 ⇒ 删掉那句 `MenuDraw.Rect`，底板只留下面 `cbtnBg` 这一颗。
            var cbtn = MenuDraw.Node(none, "Generic Simplified UI Button",
                                     new PxRect(CreateBtnL, CreateBtnT, CreateBtnR, CreateBtnB));
            var cbtnBg = MenuDraw.Rect(cbtn, Tex(ArtMulligan), new PxRect(CreateBtnL, CreateBtnT, CreateBtnR, CreateBtnB),
                          "Bg", QArt, null, true);
            // 🔴 **2026-10-18（A891 的续）：这一颗的字走 `Loc.T`** —— 本批**先认树验 `Localize`**
            //   （铁律 2/10；做法 = `工具/menu_rect.py … "Ranked Deck Selection" --depth 4` 沿 `m_Children` 走，
            //    再按 pid 读 `MonoBehaviour/*.json`）。结论：
            //     · 本节点 = `{RankedEventWindowV2 | SkirmishModeEventWindow | Ranked Deck Selection} >
            //       Ranked Deck Selection > No Deck Text > Generic Simplified UI Button > Button Text`（3 颗同族）；
            //     · `Localize.mTerm` = **`MainMenu/Ranked/GoToCreateDeck`** —— ⛔ **不是** `MenuDeck/MenuButtons/CreateDeck`
            //       （那条的父链在 `Collection Menu Variant > … > Control Buttons/Create`，**另一棵树**）；
            //     · 那颗 TMP 的 `m_text` = **`Create deck`**（小写 d）—— 与我们原来写死的这 11 个字符**逐字符相同**
            //       ⇒ 这次换口**画面上一个像素都不变**（中文档才会变成「创建卡组」）。
            //   ⚠️ 别把**父节点** `No Deck Text` 扯进来：那一颗才是占位串（原版 prefab 出厂是西语
            //     `Tienes …{0} {1} Comandante(s)…`；🔴 **2026-10-24（E11）起我们改走官方词条**
            //     —— 两支见 `NoDeckMessage()`，⛔ 那句「它是我们写的」已过期）。
            //   ⛔ 节点名 `"Button Text"` 不动（`FindChild(钮,"Button Text")` 靠它）。
            MenuDraw.Text(cbtn, new PxRect(CreateTxL, CreateTxT, CreateTxR, CreateTxB),
                          Loc.T(ArmyDeckCount() < 1 ? "MainMenu/Ranked/GoToCreateDeck" : "MainMenu/Ranked/GoToBoosterStore"),
                          Color.white, "Button Text", 55f, QText, face: TmpFont.Face.LatinCardBody);      // 原版 hAlign = Center ⇒ 不调 Align*
            // 🆕 A17：原版 `Ranked Deck Selection>No Deck Text>Generic Simplified UI Button` 是 SpriteSwap（普查 §块 4 第 16 行）
            // 🆕 **2026-10-18（音效线缺口 `AUD` §三 B10）**：原版
            //   `RankedEventWindowV2/Ranked Deck Selection/No Deck Text/Generic Simplified UI Button`（`SkirmishModeEventWindow`
            //   同名节点同 cue）= **`ButtonClick Positive`** ⇒ 补 `clickCue`（改前不传 = 默认 `ButtonClick`）。
            MenuDraw.Hit(none, "CreateDeckHit", new PxRect(CreateBtnL, CreateBtnT, CreateBtnR, CreateBtnB), QHit,
                         OnNoDeckButtonClick, cbtnBg, ArtMulligan, clickCue: WindowSounds.PositiveCue);
            none.gameObject.SetActive(!hasDeck);
        }

        /// <summary>`Numer Of Army Decks`「{当前序号}/{该阵营该模式套数}」+ `Deck Quantity Icon`。
        /// <para>🔴 **2026-10-24（E11 · P1）**：文本模型与显隐都按原版改：
        /// · 文本 = `String.Format("{0}/{1}", 该套在表里的下标 + 1, 表.Count)`（`ChangeDeck.c:105-119`）——
        ///   改前是 `{该阵营套数}/20`（**写死 20**、且**不按模式筛**）⇒ 两处都错；
        /// · 显隐 = 整颗跟着 `deck != null` 开关（`ChangeDeck.c:68-71` 的 `+0x40` `deckQuantity`）——
        ///   空态**连图标一起不显示**。</para></summary>
        void BuildArmyDeckCount(Transform col)
        {
            // 原版 `Deck Quantity Icon` 是 `Numer Of Army Decks` 的**子件**（实读 depth 3），别挂在同级
            var cnt = MenuDraw.Node(col, "Numer Of Army Decks", new PxRect(CntL, CntT, CntR, CntB));
            MenuDraw.Node(cnt, "Deck Quantity Icon", new PxRect(CntIcL, CntIcT, CntIcR, CntIcB));
            MenuDraw.Rect(cnt, Tex(ArtDeckCount), new PxRect(CntIcL, CntIcT, CntIcR, CntIcB),
                          "Icon", QArt, null, true);
            // 原版 hAlign = **Left** ⇒ 左对齐（不是居中）
            // 🔴 **2026-10-09（A1126 · A2 档）**：这一处原来**没传 autosize 实参** ⇒ 固定 60.5px。
            //   判据 = 原版 `menus ▸ SkirmishModeEventWindow ▸ …/Numer Of Army Decks`：**`m_enableAutoSizing = 1`** ·
            //   `m_fontSizeMin = **18**` · `m_fontSizeMax = **63.4**` · `m_fontSizeBase = **36**` · 折行 = 1
            //   （⚠️ 上限 63.4 与上面两颗的 72 **不同**，⛔ 别一刀切）。
            _txtDeckCount = MenuDraw.Text(cnt, new PxRect(CntL, CntT, CntR, CntB), DeckCountText(), Color.white,
                                          "Text", 60.5f, QText, CntR - CntL, 18f, 63.4f, 36f);
            // A1024③ 判不出（原版那一格没有字，我们自加卡组张数）⇒ 保持 `Face.Cjk`
            MenuDraw.AlignLeft(_txtDeckCount, new PxRect(CntL, CntT, CntR, CntB));
            // 🔴 显隐放在最后（`MenuDraw.Text` / `SetAutoFitBox` 要在父节点还活着时量尺寸）
            cnt.gameObject.SetActive(HasEffectiveDeck);
        }
        Label _txtDeckCount;

        /// <summary>`"{当前序号 + 1}/{该阵营该模式卡组数}"` —— 原版 `ChangeDeck.c:110-119` 的
        /// `String.Format(…, idx + 1, list.Count)`（`idx = List.IndexOf(deck)`）。
        /// ⚠️ 预组不在卡组库里 ⇒ `IndexOf` = −1 ⇒ 印 **`0/N`** —— **这是原版同解**
        /// （`idx + 1 = 0`），⛔ 别「为了好看」改成 1。
        /// ⚠️ `MaxDecksPerArmy = 20` **不再是这一格的分母**（原版这一格从来没有 20）——常量留着只为不改 API。</summary>
        string DeckCountText() { return (ShownDeckOrdinal() + 1) + "/" + ArmyDeckCount(); }

        // ------------------------------------------------------------ 右栏：阵营选择
        /// <summary>`Ranked Army Selector`：标题 + 可滚的阵营格（**cell 168×168 · 3 列 · padTop 20**）。
        /// ⚠️ `Army Content` 的格是**运行时实例化的**（出厂 0 子）。
        /// 🔴 **2026-10-07 就地订正（铁律 5，A118②）：原来这里写「原版灌什么**没查**」—— 查实了。**
        /// 原版 item prefab = **`Ranked Army Selector Container V2`**（168²，与本窗 `ArmyCell` 同尺寸）。
        /// 判据链（三层，逐条实读）：
        /// ① `d:/2/Warpforge_code/Scripts/Assembly-CSharp/ArmySelectorRanked.cs`（签名桩）有
        ///    `contentAnchor` / `armySelectorContainer` 两个字段；
        ///    `d:/2/tools/decomp_full/ArmySelectorRanked__Initialize.c:286`
        ///    = `Instantiate(*(this+0x30) /*item prefab*/, *(this+0x28) /*contentAnchor*/)`；
        /// ② 三处实例（`SkirmishModeEventWindow` / `RankedEventWindowV2` / 独立母版）的 `armySelectorContainer`
        ///    **都指同一个 pid `-2100390822107119323`**（`MonoBehaviour_-3073094293306978958 / _4144414309722819516 /
        ///    _4195433167936194480.json` 三份实读，值逐字相同）；
        /// ③ 该 pid 是 **`bundle_menus_assets_all/GameObject/Ranked Army Selector Container V2.json`** 根上那颗
        ///    `RankedArmySelectorContainer` 组件 ⇒ item prefab 就是它。
        /// ⇒ 全树与几何见 `RebuildArmyCells` 上面那段注释（锚点/pivot 实读 + 按 uGUI 算法算成「相对格左上角」）。
        /// ⚠️ 仍然**我们挑的**只有一条：`Army Icon` 喂的是 `DeckRuntime.FactionIcon`（原版是
        ///    `ArmyUtilities.GetArmyIcon(armyId)`，那张来源表本地没有 ⇒ 用同一批阵营徽记）。</summary>
        void BuildArmySelector(Transform root)
        {
            var holder = MenuDraw.Node(root, "Ranked Army Selector",
                                       new PxRect(ArmSelL, ArmSelT, ArmSelR, ArmSelB));
            // 原版 hAlign = **Center** ⇒ 不调 `Align*`
            // 🔴 **2026-10-22（M1 · 文案）**：原来写死英文 `"Factions"`，而原版这颗 TMP 挂 `Localize`
            //   （`mTerm = MainMenu/Factions`，`bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-2286559357550753872.json`
            //   现读；官方表 en `Factions` / zh `派系`）⇒ 改走词条。⛔ 别写死英文（那是未翻译的原文）。
            MenuDraw.Text(holder, new PxRect(FacTitleL, FacTitleT, FacTitleR, FacTitleB), Loc.T("MainMenu/Factions"),
                          Color.white, "Factions Title", 45.87f, QText, face: TmpFont.Face.LatinCardBody);

            // 层级照原版：`Army Selector`(ScrollRect) -> `Viewport`(RectMask2D) -> `Army Content`(GridLayoutGroup)
            // 原来把 `Army Selector` 这个**名字**安在普通 Transform 上、格子直接挂它 —— 名字错位（找茬抓到）
            var sel = MenuDraw.Node(holder, "Army Selector", new PxRect(ArmViewL, ArmViewT, ArmViewR, ArmViewB));
            var vp = MenuDraw.Node(sel, "Viewport", new PxRect(ArmViewL, ArmViewT, ArmViewR, ArmViewB));
            // 🔴 **2026-10-13（A435 阶段 2 · 丁 / A745）**：这颗 `Viewport` 就是**这颗视口的裁切状态载体**
            //    （= 原版 `Ranked Army Selector/Army Selector/Viewport` 身上那个 `RectMask2D`；三族实读值
            //    见 `VpSoft` 那段注释：`m_Padding = (0,0,0,0)` · `m_Softness = (0,52)`）。
            //    形状照 **`Shell/ChatPanel.cs` 的 `ChatTab/Viewport` 那一颗**（A435 阶段 2 · 丙的成品）——
            //    ⚠️ 本节点**本来就存在**（`Army Content` 就挂在它下面）⇒ 只 `AddComponent`、**零结构改动**
            //    （同 `AvatarTab` / `TitleTab` 走的迁移表 §二·A 注①【低风险】那条路）。
            //    ✅ 与迁移前「`RebuildArmyCells` 里逐件传 `view` + `VpSoft`」**逐位同值**：
            //    框 = 本节点自己的 rect（本来就是同一个 `PxRect(ArmView…)`）、两个参数逐字相同。
            //    ⛔ 别改回去逐件传 `view` —— 那样节点会被形参盖住（`ViewportClip.NodeShadowedByParam`），
            //    框一个像素都不生效、而且不出声（迁移表 §二 通则那一句）。
            var vc = vp.gameObject.AddComponent<ViewportClip>();
            vc.padding = Vector4.zero;                                     // 三条原版路径的 `m_Padding` 都是全 0
            vc.softness = new Vector2Int((int)VpSoft.x, (int)VpSoft.y);     // (0,52)：只渐变上下，见 `VpSoft`
            // 🔴 **2026-10-18（A840 · S1）**：补一句 `CaptureNow()` —— 把**刚写进这个节点的那个矩形**记成基准
            //   （= 框的中心那一帧，口径 → `ViewportClip` 文件头 §①）。⛔ 少了它，这颗视口**逐位回落到旧写法**
            //   （实时反推 + `LiveDerivations`）：本窗整棵树重建时节点或它的祖先被挪过，框与被比矩形就**不在同一帧**。
            //   ⚠️ **必须在这里调**（`AddComponent` 之后、`RebuildArmyCells` 之前）：`MenuDraw.ApplyPxRect` 写矩形时
            //   组件还不存在（那一句的穿透写在 `ApplyPxRect` 尾）—— 同 `ViewportClip.Hang` 里那句
            //   `vc.SetBaseRect(r)` 的位置理由（`CardPresentation/Shell/ViewportClip.cs`）。⛔ 别改成 `OnEnable` 里自动抓（文件头 `CardPresentation/Shell/ViewportClip.cs` 明令）。
            vc.CaptureNow();
            _facs = CollectionWindow.CardsState.Factions();

            // 🆕 **2026-10-24（E11 · P2）**：把「选中阵营」解析成右栏那一格的下标（`Open()` 已按当前卡组定好字符串）。
            //   原版 `RankedDeckSelector.currentArmy` 在 `Initialize` / `ChangeArmy` 之后一定是一个**具体阵营**
            //   ⇒ 右栏那一格**开窗就亮**（参考图左上是 UM 按下态）。改前这里是 `ArmyIndex = -1`（一格都不亮）。
            // ⚠️ 阵营不在 `Factions()` 里（卡池残缺）⇒ −1 并**出声**，⛔ 不回落成第 0 格（那会亮错阵营）。
            ArmyIndex = IndexOfFaction(SelectedArmyFaction);
            if (ArmyIndex < 0)
                Debug.LogWarning("[Event] 选中阵营「" + SelectedArmyFaction + "」**不在 `Factions()` 里** ⇒ 右栏一格都不亮"
                                 + "（如实出声；⛔ 不回落成别的阵营）");

            int cols = Mathf.Max(1, Mathf.FloorToInt((ArmViewR - ArmViewL) / ArmyCell));
            int rows = Mathf.CeilToInt(_facs.Count / (float)cols);
            float contentH = ArmyPadT + ArmyCell * rows;
            // ⚠️ 这里**不再留一个 `view` 局部量**当裁切框：裁切状态已经搬到上面 `BuildArmySelector` 建的那颗
            //    `Viewport` 节点上（`ViewportClip`）。下表里的 `view` 全部换成 `null` ⇒ `MenuDraw.Rect/Nine/Hit`
            //    沿父链解析到那一颗。🔴 **`MenuScroll` 那份 `Viewport` 是【另一条路】**（滚动区的矩形，
            //    不是裁切载体）—— 两者今天同值，但⛔ 别把滚动区当成裁切状态的来源。
            _armyScroll = MenuScroll.TopAligned(new PxRect(ArmViewL, ArmViewT, ArmViewR, ArmViewB), contentH, 1f);   // 原版 `Ranked Army Selector/Army Selector` = 1.0（D_N29）
            _armyScroll.Owner = gameObject;
            _armyContent = MenuDraw.Node(vp, "Army Content",
                                         new PxRect(ArmViewL, ArmViewT, ArmViewR, ArmViewT + contentH));
            _armyScroll.OnChanged = () => RebuildArmyCells(_armyContent);
            PointerLayer.RegisterScroll(_armyScroll);
            RebuildArmyCells(_armyContent);
        }

        Transform _armyHolder, _armyContent;

        // ------------------------------------------------------------ 阵营格 = 原版 item prefab 的全树（A118②）
        // 判据 = `bundle_menus_assets_all/GameObject/Ranked Army Selector Container V2.json` 那一棵树
        //   （判据链见 `BuildArmySelector` 的注释）。几何 = **锚点/pivot 逐字段实读 + 按 uGUI 算法
        //   算成「相对格左上角」**（本 prefab 没有 LayoutGroup，所以 `menu_dump` 给的就是实画位）：
        //     `Background` / `On`  0,0 → 168,168（铺满）· `Army Icon` 8.68,6.20 → 158.64,156.16（149.96²）
        //     `ProgressBar` = `Fill Area`  3.84,142.58 → 163.44,163.43
        //     `Separator`  26.18,142.58 → 30.18,163.43（4×20.852；`Fill Area` 中心偏左 55.46）
        //     `Featured Icon`  1.24,1.68 → 124.05,125.21（122.81×123.53）
        // ⚠️ `Fill` 那一层的**序列化** anchor 是 `(0,0)-(0,0) sz=(0,0.0005)` = **`Slider.UpdateVisuals` 跑之前
        //    的模板位**（跑完是 `(0,0)-(value,1)` ⇒ 高撑满 `Fill Area`）⇒ 本件照**运行期语义**摆：
        //    左沿 = `Fill Area` 左沿、高 = 整条、宽 = 比例 × 159.60。比例只此一处 = `ArmyProgress`。
        static readonly PxRect CellBg = new PxRect(0f, 0f, ArmyCell, ArmyCell);
        static readonly PxRect CellBar = new PxRect(3.84f, 142.58f, 163.44f, 163.43f);
        /// <summary>prefab 里那根 `Separator` 模板的矩形（**模板位**，不是第一条缝的位置 —— 见 `ArmyBarCells` 那段）。
        /// 🔴 **2026-10-24（E19）：现在只用它的【宽 4 px / 高 20.852】**（4×20.852 是原版真值，9 根都照它画），
        /// **落点按 10 格的边界算**（`barRect.x1 + s·pitch`）—— ⛔ 别把 26.18 当成第一条缝抄回去。</summary>
        static readonly PxRect CellSep = new PxRect(26.18f, 142.58f, 30.18f, 163.43f);
        static readonly PxRect CellIcon = new PxRect(8.68f, 6.20f, 158.64f, 156.16f);
        static readonly PxRect CellFeat = new PxRect(1.24f, 1.68f, 124.05f, 125.21f);
        /// <summary>把「相对格左上角」的偏移矩形搬到画布坐标。</summary>
        static PxRect InCell(PxRect cell, PxRect off)
        { return new PxRect(cell.x1 + off.x1, cell.y1 + off.y1, cell.x1 + off.x2, cell.y1 + off.y2); }

        // 图名（同一颗 `EverguildToggle` 的四态 + 进度条 + 特色角旗；sprite pid → 名字由 `menu_dump` 的索引解出）
        const string ArtArmyBack = "UI_Army_Selection_Back";                // `offSprite` = 未选中
        const string ArtArmyOn = "UI_Army_Selection_Back_Pressed";          // `onSprite` / `m_PressedSprite`
        const string ArtArmyHover = "UI_Army_Selection_Back_Hover";         // `m_HighlightedSprite`（悬停）
        const string ArtArmyProg = "UI_Army_Selection_Back_Progression";    // `Fill`（`Sliced` 九宫 20,0,20,0）
        const string ArtArmyFeat = "UI_Army_Selection_Featured";            // `Featured Icon`
        /// <summary>`ProgressBar` 的填充比例。🔴 **原版是数据驱动的，我们没有那份数据** ——
        /// `RankedArmySelectorContainer__Initialize.c`：`ProgressBar.SetProgress(progressBar, param_3, param_4, …)`
        /// 的实参来自 `RankedScoreBoostSave.GetWins(army)`（赛季存档，在服务器）⇒ 取 **0**
        /// （= 序列化里 `Fill` 那个 0 宽，也是「没有进度」唯一有判据的那个值）。**这一条是我们挑的**，
        /// 不是从原版读出来的运行期值；真接上数据时改这一个数。
        /// ⚠️ 同理 `ProgressBar` / `Featured Icon` **两件的显隐**也是数据驱动的（`SetActive(…, showProgress)` /
        /// `SetActive(…, army.RankedV3ArmyState &amp; 4 /*Featured*/)`，两个源都在赛季数据里）—— 我们**没有**那份数据，
        /// ⇒ 照 **prefab 出厂状态**画（`m_IsActive` 两件都是 true）。**这一条也是我们挑的**，见下面那次出声。
        /// <para>🔴 **2026-10-24（E19）：原版真值的两个口子（接数据时只改这里）** ——
        /// `current = RankedScoreBoostSave.GetWins(army)`（`Dictionary&lt;CardArmy,int&gt; matchCount`，
        /// `ArmySelectorRanked__Initialize.c`）· `max = boost 事件数据 +0x68`（`RankedArmySelectorContainer__Initialize.c`）。
        /// **两个都在服务器**（判据 → `资料/普查产出_1024b/S4_模式窗细节与徽章.md` §⑤）⇒ 本地只能给 0。</para></summary>
        const float ArmyProgress = 0f;
        /// <summary>🔴 **2026-10-24（E19）：`ProgressBar` 是【10 格】**（不是一根连续条）。
        /// 判据 = `RankedArmySelectorContainer__Initialize.c`：`SetProgress(current, max, …, useWholeNumbers:1)`
        /// —— **按整格取整**；`max &gt; 1` 时按 `max − 1` 实例化 `Separator`（4×20.852、均分）⇒ 10 格 = **9 根缝**。
        /// ⚠️ `max` 原版也来自数据；那个数本地没有 ⇒ 用 prefab 那套「10 格」的画法（`SetProgress` 的
        /// `useWholeNumbers` 那条口径下，格子数就是 `max`；10 是参考图能数出来的那个数：
        /// 「有条的格子一律 10/10 全亮」，格宽 15.96 / 缝 4.3 —— 两者相加正好 = 159.60 / 10）。</summary>
        public const int ArmyBarCells = 10;
        /// <summary>格与格之间的缝（参考图逐像素实测 **4.3**；prefab 那根 `Separator` 本体宽 **4**）。
        /// ⇒ 亮格宽 = 15.96 − 4.3 = 11.66，缝里正好摆那根 4 px 的黑条（`CellSep` 的宽度）。</summary>
        public const float ArmyBarGapPx = 4.3f;
        /// <summary>`ArmyProgress`/两件显隐那条「我们挑的」**只说一次**（同 `TipHovers` 那类一次性出声）。</summary>
        static bool _armyDataNoteShown;

        void RebuildArmyCells(Transform holder)
        {
            _armyHolder = holder;
            for (int i = holder.childCount - 1; i >= 0; i--) RewardsWindow.DestroySafe(holder.GetChild(i).gameObject);
            ArmyCells.Clear();
            var facs = _facs ?? new List<string>();
            int cols = Mathf.Max(1, Mathf.FloorToInt((ArmViewR - ArmViewL) / ArmyCell));
            // `GridLayoutGroup` 的 `align=1`(UpperCenter) ⇒ 3 列那 504 宽在一列里**居中**：
            //   左右各留 (547.12 - 3x168)/2 = **21.56**（找茬子代理按 `menu_dump` 的布局组字段算的）
            float padX = ((ArmViewR - ArmViewL) - cols * ArmyCell) * 0.5f;
            for (int i = 0; i < facs.Count; i++)
            {
                int c = i % cols, r = i / cols;
                float x1 = ArmViewL + padX + c * ArmyCell, y1 = ArmViewT + ArmyPadT + r * ArmyCell;
                var rr = _armyScroll.Shift(new PxRect(x1, y1, x1 + ArmyCell, y1 + ArmyCell));
                // 原来「只画完整落在视口里的」⇒ 半行那 139px 可见也整行不画；
                // 改用 **uv 裁**（= 原版 `RectMask2D` 的等效物，`MenuDraw.Rect` 的 clip 形参）
                // 🔴 **2026-10-07（A12①）**：这里原来是**内联的「整块在框外」**（只判纵轴、且自带 0.5px 余量）
                //    ⇒ 与 `MenuScroll.Intersects` / `MenuDraw.Visible` 并列成了**第二、第三份求交**。
                //    收口成唯一那一份（两轴都判）—— **本处的行为一字不变**：`view` 就是本条的 `Viewport`
                //    （同一个矩形，见 `BuildArmySelector` 的 `MenuDraw.Node(…, new PxRect(ArmView…))`），
                //    而列位是 `ArmViewL + padX + c*ArmyCell`、`cols = floor(宽/格)`、`padX ≥ 0`
                //    ⇒ **每一列的横轴必定落在视口内** ⇒ 那条新增的横轴判据在这里**永不触发**。
                //    （格内每一颗图/命中区在 **A745 迁移之前**把 `view` 当 `clip` 逐件传进去了 ⇒ 半行也只画可见的那截；
                //     A745 起改由下面那颗节点给 —— 见紧接着那一段。）
                // 🔴 **2026-10-13（A435 阶段 2 · 丁 / A745）**：改走 **`VisibleAbove`**（节点态那一版）——
                //    裁切状态已经搬到 `Army Selector/Viewport` 那颗 `ViewportClip` 上，下面逐件传的 `clip`
                //    全部换成 `null` ⇒ 沿用裸 `Visible` 的话这里**永远放行**（静默：整格滚出视口照建）。
                //    解析起点 = `holder`（= `Army Content`，正挂在那颗 `Viewport` 下 ⇒ 一次父链就命中）。
                if (!MenuDraw.VisibleAbove(holder, rr, null)) continue;   // 整块在视口外 ⇒ 不建（节点、图、点击区一起没有）
                // 🆕 **2026-10-07（A9 / A38①）：软边（原版 `m_Softness = (0,52)`）** ——
                //    判据/三族路径见 `VpSoft` 的注释。🔴 **A745 起 `clip` 逐件传 `null`**（裁切框那半边由
                //    节点给），而 `clipSoftness` **照旧传 `VpSoft`** —— 它现在是**回落那一档**的软边
                //    （形参非空 = 旧路赢）；节点在时由**节点自己的 `softness`** 说了算（两者逐字同值 `(0,52)`）。
                //    ⚠️ 下面那一条 `MenuDraw.Hit` **故意不传**软边（`Hit` 也不收这个形参）——
                //    原版 `m_Softness` 只改渲染，射线那一面只看矩形。
                bool sel = (ArmyIndex == i);
                var cell = MenuDraw.Node(holder, "Army_" + i, rr);          // 原版根：`Ranked Army Selector Container V2`

                // ① `Background`（铺满·`m_PreserveAspect = 0` ⇒ 拉伸）；**选中 = 换它的贴图**
                //    （原版 `EverguildToggle.spriteToChange` / `m_TargetGraphic` 指的就是这颗 Image）
                var bg = MenuDraw.Node(cell, "Background", rr);
                var bgQ = MenuDraw.Rect(bg, Tex(sel ? ArtArmyOn : ArtArmyBack), InCell(rr, CellBg), "Image",
                                        QArmyBack, null, false, null, VpSoft);
                // ② `On`（= `Toggle.graphic`，**出厂 act F**）—— 原版 uGUI `Toggle` 只 cross-fade 它的 alpha、
                //    **从不 SetActive**，而它序列化就是关的 ⇒ **原版它一个像素都不画**（死节点）。
                //    我们照原版那样建出来、也照原版关掉：节点结构（含 tag 语义）齐，画面上不多一层。
                var on = MenuDraw.Node(bg, "On", rr);
                MenuDraw.Rect(on, Tex(ArtArmyOn), InCell(rr, CellBg), "Image", QArmyBack, null, false, null, VpSoft);
                on.gameObject.SetActive(false);
                // ③ `ProgressBar`(Slider) > `Fill Area` > `Fill`（10 格）+ `Separator`（9 根）
                var bar = MenuDraw.Node(cell, "ProgressBar", InCell(rr, CellBar));
                var barArea = MenuDraw.Node(bar, "Fill Area", InCell(rr, CellBar));
                var barRect = InCell(rr, CellBar);
                // 🔴 **2026-10-24（E19 · 用户点名「能量条」）**：原来是「一根 `Fill`（宽 0）+ prefab 那 1 根缝」
                //   ⇒ 视觉上等于没有条。原版是 **10 格**、**9 根缝**（判据见 `ArmyBarCells` 的 doc）。
                //   画法 = ①`Fill` 这一层节点留下（原版树里就叫 `Fill`），底下摆 10 颗 `Cell`；
                //          ②亮几格由 `ArmyProgress` 决定（`useWholeNumbers:1` ⇒ 整格），**0 ⇒ 一格都不亮**；
                //          ③缝摆在这 10 格的**边界**上（每格 pitch 的整数倍处），所以亮格与缝**不重叠**
                //            ⇒ 它们同用 `QArmyBar` 也不会「谁盖谁不可控」。
                var fill = MenuDraw.Node(barArea, "Fill", barRect);
                float pitch = (barRect.x2 - barRect.x1) / ArmyBarCells;
                int litCells = Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(ArmyProgress) * ArmyBarCells),
                                           0, ArmyBarCells);
                for (int ci = 0; ci < ArmyBarCells; ci++)
                {
                    float cx1 = barRect.x1 + ci * pitch + ArmyBarGapPx * 0.5f;
                    float cx2 = barRect.x1 + (ci + 1) * pitch - ArmyBarGapPx * 0.5f;
                    var cellR = new PxRect(cx1, barRect.y1, cx2, barRect.y2);
                    var cellNode = MenuDraw.Node(fill, ci == 0 ? "Cell" : "Cell (" + ci + ")", cellR);
                    // 未点亮的格子**节点照在、就关掉**（原版 `Fill` 的宽度按格数走；0 格 ≠ 少 10 个节点）。
                    // 亮着的才建那片九宫 —— 本地 `ArmyProgress = 0` ⇒ 今天一颗 quad 都不建（省 480 个对象），
                    // 数据接上之后自动长出来。
                    if (ci < litCells)
                        MenuDraw.Nine(cellNode, Tex(ArtArmyProg), cellR,
                                      new Vector4(20f, 0f, 20f, 0f), 48f, 18f, QArmyBar, null, true, "Image",
                                      new Vector4(10f, 0f, 10f, 0f), null, VpSoft);   // `m_PixelsPerUnitMultiplier = 2` ⇒ 画出来 20÷2 = 10
                    else
                        cellNode.gameObject.SetActive(false);
                }
                // `Separator`：**无图**、纯黑 (0,0,0,1)、4×20.852 —— 原版运行期按 `max − 1` 实例化
                // （`RankedArmySelectorContainer__Initialize.c` 里那个 `param_4 - 1` 的循环）并**把模板那份的组件关掉**。
                // ⇒ 我们建 **9 根**（10 格 = 9 条边界），第 1 根仍叫 `Separator`（模板名），其余按 Unity 的 ` (n)` 递增名。
                // ⚠️ prefab 那根模板的落点 `CellSep`（26.18…30.18）是**模板位**，不是第一条边界
                //    （第一条边界 = 3.84 + 15.96 = 19.80）⇒ 这里**按边界算**，别照抄模板 x。
                float sepW = CellSep.x2 - CellSep.x1;
                for (int s = 1; s < ArmyBarCells; s++)
                {
                    float cx = barRect.x1 + s * pitch;
                    MenuDraw.Rect(barArea, CardArt.Solid(),
                                  new PxRect(cx - sepW * 0.5f, barRect.y1, cx + sepW * 0.5f, barRect.y2),
                                  s == 1 ? "Separator" : "Separator (" + (s - 1) + ")", QArmyBar,
                                  new Color(0f, 0f, 0f, 1f), false, null, VpSoft);
                }
                // ④ `Army Icon`（149.96² · `preserveAspect`；图由运行期喂 —— 我们喂阵营徽记，见 `BuildArmySelector`）
                MenuDraw.Rect(cell, Tex(DeckRuntime.FactionIcon(facs[i])), InCell(rr, CellIcon),
                              "Army Icon", QArmyIcon, null, true, null, VpSoft);
                // ⑤ `Featured Icon`（122.81×123.53 · `preserveAspect`）—— 显隐数据驱动（见 `ArmyProgress` 那段）
                // 🔴 **2026-10-22（M1 · 真机对照）**：原来**照 prefab 出厂态把每一格都画出来**（= 12 格里 12 个金色
                //   「+」角旗）。原版那件的显隐是 `SetActive(cell, (army.RankedV3ArmyState & 4 /*Featured*/) != 0)`
                //   （`RankedArmySelectorContainer__Initialize.c`）—— **赛季数据在服务器**，我们一条都没有；
                //   而参考图 `C:/Users/qjh36/Desktop/warpforge参考图/经典模式参考图.png` 的 12 格**一个角旗都没有**
                //   ⇒ 与「进度取 0」同一条口径：**没有数据 = 不显示**。节点照建（结构与 prefab 一致、断言仍认得到），
                //   只把它 **关掉**。⚠️ 这一条是我们按「无数据」推的，不是从原版运行期读到的值。
                var featQ = MenuDraw.Rect(cell, Tex(ArtArmyFeat), InCell(rr, CellFeat), "Featured Icon",
                                          QArmyFeat, null, true, null, VpSoft);
                if (featQ != null) featQ.gameObject.SetActive(false);

                // 四态（悬停 / 按下 / 常态**逐颗显式给**）：本颗的悬停图**不是** `<常态图>_hover` ——
                // `WindowButton` 的后备规则会推成 `UI_Army_Selection_Back_hover`（**小写 h**），
                // 而原版那张叫 `UI_Army_Selection_Back_Hover`（**大写 H**，同 `HoverNames` 里那两条同因）
                // ⇒ 不显式给就是「取不到 + 悬停不换图 + `MissingSwapArt` 记一条」。
                // 🔴 **2026-10-13（A435 阶段 2 · 丁 / A745）**：`clip` 传 `null` —— 命中区要的【裸框 + pad】
                //    也由那颗 `Viewport` 节点给（`MenuDraw.Hit` 内部 `Resolve`）。⚠️ 软边**故意不传**
                //    （`Hit` 也不收这个形参）：原版 `m_Softness` 只改渲染，射线那一面只看矩形。
                int idx = i;
                // 🆕 **2026-10-18（主对话裁定 · 按原版订正）**：原版 `Ranked Army Selector Container V2`
                //   是 `EverguildToggle` + **`allowClickWhenAlreadySelected = 0`** ⇒ 已选中再点 = 什么都不做
                //   （连按钮音一起跳过；我们原来「再点 = 取消筛选」的语义已删，见 `PickArmy`）。
                var armyHit = MenuDraw.Hit(cell, "Hit", rr, QHit, () => PickArmy(idx), bgQ,
                             sel ? ArtArmyOn : ArtArmyBack, ArtArmyHover, ArtArmyOn, null,
                             clickCue: WindowSounds.SelectAvatarCue);
                var armyWb = armyHit != null ? armyHit.GetComponent<WindowButton>() : null;
                if (armyWb != null)
                {
                    armyWb.allowClickWhenAlreadySelected = false;
                    armyWb.isOn = () => ArmyIndex == idx;
                }
                ArmyCells.Add(cell);
            }
            if (!_armyDataNoteShown)
            {
                _armyDataNoteShown = true;
                Debug.Log("[Event] 阵营格：`ProgressBar` / `Featured Icon` 已按原版 item prefab"
                          + "（`Ranked Army Selector Container V2`）建出来，但**它们的数值与显隐在原版是赛季数据驱动的**"
                          + "（`RankedArmySelectorContainer.Initialize` 的 `wins/maxWins` · `showProgress` ·"
                          + " `RankedV3ArmyState.Featured`，全在服务器）—— **我们没有那份数据** ⇒ 进度取 0（10 格全灭）、"
                          + "`Featured Icon` **关着**（2026-10-22 M1：参考图 12 格一个角旗都没有 ⇒ 无数据 = 不显示）。"
                          + "**这一条是我们挑的**，不是原版的运行期值（格的画法 = 10 格 + 9 根缝，照原版 `max−1` 那条口径）。");
            }
        }

        // ------------------------------------------------------------ 顶上那条标题栏
        /// <summary>`Game Mode Header With Back Button` + `Header Background`(HLG) + `Header Background (1)` + `Header Back Button`。
        /// ⚠️ `Game Mode Icon` 原版 `sprite=0`（由 `LiveopUIDrawer_GameModeIcon` 运行时按模式喂，
        ///    那两张图**本地没有**）⇒ **不画**，出声。</summary>
        /// <remarks>🔴 **2026-10-17（A866）：四层建法已收口到 `WindowHeader.WithBackButton`**
        /// （全工程 4 扇窗各抄一遍 ⇒ 收成一份）。本窗这一档的**差异**（逐格对照 →
        /// `资料/普查产出_1018/S5_A866窗头收口.md` §2）：
        /// · 根名 = `Game Mode Header With Back Button`（另三扇是缺省那名）；
        /// · 底板宽度 = **`ContentSizeFitterMinMax` 按内容撑开**（`widthMin 550 / widthMax 1250`）——
        ///   🔴 **2026-10-24（E19）就地订正**：这里原来写「= 690.86」，那是 `menu_dump` 按**序列化**的
        ///   标题框宽 369.36 算出来的（标题挂 `ContentSizeFitter h:PreferredSize`，运行期会缩到**字宽**）；
        ///   ⇒ 真值 = `clamp(155 + 标题实宽 + 5.5 + 100 + 61, 550, 1250)`，中文两档都夹在 **550** 上；
        /// · 标题走 `FitAfterSpacing`（A492 把两句对调过 —— 判据与「无牙口」那段**已搬进**
        ///   `WindowHeader.TitleFit` 的 `FitAfterSpacing`，⛔ 别在这儿再写第二份）；
        /// · **多一颗 `Game Mode Icon`**（本窗在 `parts.Plate` 底下补 —— 兄弟序与原来逐位一致：`Window Title` → 图标）；
        /// · 🔴 **2026-10-18（三扇标题垂直档漏网）**：标题**多一档 `TitleVAlign = Capline`**（原文这里没列这一条 ⇒
        ///   A866 时本窗走的是 `Label` 出厂的 `Middle`，是**与原版不符**的，见 `BuildHeader` 里那段判据）；
        /// · 返回钮 = `QuadOnHeader` + **有换图**（`BackSwapArt = ArtHeaderBack`，A17）。
        /// <para>🔴 **2026-10-18（A967 · 铁律 5 订正）**：原文这里写着「⚠️ 尖角那一颗**沿用** `MenuDraw.Nine`
        /// 的缺省名 `"Nine"`（另三扇都显式起名 `Header Background (1)`）—— 本笔**照旧保留**（收口不夹带行为改动），
        /// 差异已登记在 §5 / §7」。**那一笔是错的、已经删了**：原版同一个 prefab 里那颗尖角就叫
        /// `Header Background (1)`（判据 = `WindowHeader.WingName` 的 doc 那份实读）⇒ A866 当时「保留差异」
        /// 保留成了一处**与原版不符**。现在 `Spec` 上**不再写 `WingName`** ⇒ 落到缺省 `Header Background (1)`，
        /// 与另三扇齐平。⛔ 别再把这一行加回来。</para>
        /// <para>⚠️ 今天**没有断言**盯着本窗这颗尖角的节点名（全仓唯一读 `Header Background (1)` 的是
        /// `Editor/MainMenuScene.cs` 里读 `TutorialModePopup` 那颗的那一条）—— 断言侧缺口见本批报告。</para></remarks>
        void BuildHeader(Transform root)
        {
            // `Header Background`：`ContentSizeFitter` 按内容撑开 ⇒ 它自己宽 0；HLG 从 **padLeft 155** 起排
            // 原版 `Header Background` 挂 `ContentSizeFitterMinMax`（**`widthMin 550` / `widthMax 1250`**），
            //   按内容 = padL 155 + **标题实宽** + spacing 5.5 + 图标 100 + padR 61，再夹到 [550, 1250]。
            // 🔴 **2026-10-24（E19 · 用户点名「左上角圆标离模式名太远」）**：原来这里写死
            //   `title 369.36` ⇒ 底板 690.86、圆标左沿 529.86（实测参考图 **299**，右了 239.4 px）。
            //   原版 `Window Title` 挂 **`ContentSizeFitter m_HorizontalFit = 2`（按文本 MinSize 自适应）**
            //   ⇒ 标题盒宽 = **文本实宽**（「经典」≈ 138.5，不是 369.36）。
            //   判据 → `资料/普查产出_1024b/S4_模式窗细节与徽章.md` §②（`Header Background` 的 HLG 实读
            //   `m_Padding=(155,61,0,0)` / `m_Spacing=5.5` / `MiddleLeft`；参考图实测圆标图形左沿 299）。
            const float titleFitW = 369.36f;          // 原版那句 `SetAutoFitBox` 的框宽（⛔ 别拿它当文本宽）
            const float plateMinW = 550f, plateMaxW = 1250f;   // 原版 `ContentSizeFitterMinMax` 的上下限
            float titleL = HdrL + HdrPadL;
            // ⚠️ **先按 `widthMin` 建**：标题的实宽要**建完才量得到**（`Label.WorldW`），而底板宽又依赖它。
            //   量完再按需撑开（`ResizeHeaderPlate`）—— ⛔ 别为了这个把 `WindowHeader` 的建法抄第二份。
            var parts = WindowHeader.WithBackButton(root, new WindowHeader.Spec
            {
                RootName = WindowHeader.RootNameGameMode,
                RootRect = new PxRect(HdrL, HdrT, HdrR, HdrB),
                PlateRect = new PxRect(HdrL, HdrBg1T, plateMinW, HdrBg1B),
                TitleRect = new PxRect(titleL, HdrT + 16.36f, titleL + titleFitW, HdrT + 99.01f),
                // 🔴 **2026-10-22（`W63` §四）：标题按【当前模式的官方名】喂，不再写死出厂占位字**。
                //   改前这一格是 prefab 出厂 `m_text = "Game mode"`；原版运行时由
                //   `WindowHeaderWithBackButton.Initialize` 换掉，换上的就是这一局模式的标题
                //   （参考图 `C:/Users/qjh36/Desktop/warpforge参考图/经典模式参考图.png` 上是 **「经典」**）
                //   ⇒ 照抄出厂字 = 我们的标题永远停在占位值。
                //   模式来源 = **本类既有的 `DeckGameMode`**（子类覆写：`SkirmishEventWindow` → 遭遇 13，
                //   其余走基类默认经典 0）—— ⛔ 不新造状态位。
                //   词条键 = 官方 `MainMenu/GameMode/{Classic,Skirmish}`
                //   （`官方本地化_en+zh-CN.json` 现读 **Classic / 经典** 与 **Skirmish / 小冲突**）。
                //   判据 → `资料/普查产出_1022/W63_卡组双模式与卡组内容_调查.md` §四。
                TitleText = DeckGameMode == (int)GameMode.Skirmish
                          ? Loc.T("MainMenu/GameMode/Skirmish")
                          : Loc.T("MainMenu/GameMode/Classic"),
                TitleMode = WindowHeader.TitleFit.FitAfterSpacing,
                TitleFitW = 369.36f, TitleFitH = 82.65f,   // 照本窗原来的字面量（⛔ 别改成 `TitleRect.W/H`）
                // 🔴 **2026-10-18（三扇标题垂直档漏网 · 铁律 11）**：补这一档 —— 原版 `Window Title` 那颗 TMP 是
                //   **`m_VerticalAlignment = 8192`（`Capline`）**（判据：亲读 `bundle_menus_assets_all` 里
                //   **8 颗 `Window Title` 逐颗现读、逐值相同**，本窗那颗也在其中）；本窗此前没传 ⇒
                //   落到 `Label` 出厂的 `Middle`，是**真差异**。写法照 `DailyStreakPopup` 那一处。
                //   ⚠️ 共件里 `SetVAlign` **排在 `AlignLeft` 之后**（纵向/横向互不干涉），照旧。
                TitleVAlign = Label.VAlign.Capline,
                WingRect = new PxRect(HdrBg1L, HdrBg1T, HdrBg1R, HdrBg1B),
                // 🔴 **2026-10-18（A967）**：原来这里显式写着 `WingName = "Nine",`（= `MenuDraw.Nine` 的
                //   **缺省名**，A866 收口时「照旧保留」的那一笔）—— **已删**。
                //   判据 = 原版同一个 prefab 里那颗尖角就叫 **`Header Background (1)`**
                //   （`WindowHeader.WingName` 的 doc 里那份 `menu_dump` 实读；另三扇 + `Editor/MainMenuScene.cs`
                //   读的那个实例也都是这个名字）⇒ 删掉这一行即落到 `Spec.WingName` 的缺省
                //   （= `WindowHeader.WingName` = `"Header Background (1)"`，`MenuWindowBase.cs` 的 `Spec`）。
                //   ⚠️ 只动**节点名**：矩形 / 贴图 / 队列 / 兄弟序一个字节都没变（`MenuDraw.Nine` 的落位与
                //   `name` 无关）。
                BackRect = new PxRect(HdrBackL, HdrBackT, HdrBackR, HdrBackB),
                BackButtonStyle = WindowHeader.BackStyle.QuadOnHeader,
                BackKeepAspect = true,
                // A17：原版 `Game Mode Header With Back Button>Header Back Button` 是 SpriteSwap（普查 §块 4 第 17 行）
                BackSwapArt = ArtHeaderBack,
                PlateTex = Tex(ArtHeaderBg),
                BackTex = Tex(ArtHeaderBack),
                QPlate = QArt, QWing = QArt1, QTitle = QText, QBack = QArt2, QHit = QHit,
                OnBack = () => Close(),
            });

            // ---------------------------------------------------------- 标题实宽 → 圆标落点 + 底板宽
            // 🔴 **2026-10-24（E19）**：量的是**建出来的那颗 `Window Title`** 的 `Label.WorldW`
            //   （`Label` 的单位是 Unity 单位，1 单位 = 108 设计 px —— 与 `Shell/ItemDrawer.cs` /
            //   `Shell/MainMenuRuntime.cs` 那两处同一口径），⛔ 不读 `TitleFitW` 那个**框宽**。
            //   `WorldW` 是 `RefreshBounds` 量出来的 `textBounds` 尺寸；`WithBackButton` 里的
            //   `AlignLeftOn` 头一句就是 `RefreshBounds()` ⇒ 到这里已经是**含字距的实宽**。
            float titleW = titleFitW;
            var tlb = parts.Title;
            float measured = tlb != null ? tlb.WorldW * 108f : 0f;
            if (measured > 0.1f && measured <= titleFitW + 1f)
                titleW = measured;
            else
                Debug.LogWarning("[Event] `Window Title` 的**实宽量不到**（WorldW ≈ " + measured.ToString("F2")
                                 + " px）⇒ 本窗圆标与底板按自适应框宽 " + titleFitW.ToString("F2")
                                 + " 摆（⛔ 不是量出来的，只是让位置可复现）");
            // 底板 = padL 155 + 标题实宽 + 5.5 + 图标 100 + padR 61，夹在 `ContentSizeFitterMinMax` 的 [550,1250]
            float plateR = Mathf.Clamp(titleL + titleW + HdrGap + 100f + HdrPadR, plateMinW, plateMaxW);
            if (Mathf.Abs(plateR - plateMinW) > 0.01f) ResizeHeaderPlate(parts.Plate, plateR);

            // `Game Mode Icon`（HLG 里紧跟标题：**155 + 标题实宽 + spacing 5.5**，竖中在 115.36 的板里）
            // 🔴 **2026-10-24（E19）**：原文这里写死 `155 + 369.36 + 5.5 = 529.86` —— 那是**框宽**，不是字宽
            //   ⇒ 圆标比原版右了 **239.4 px**（S4 参考图实测图形左沿 **299**；按实宽算 = 155 + 138.5 + 5.5 ≈ **299**）。
            // 2026-09-24 订正：原来这里写「那两张图本地没有」—— **是错的**：
            //   `Resources/Art/ui_menu/40k_gamemode_icon_skirmish.png` 与 `…_classic.png` **早在工程里**
            //   （练习窗那个 `Toggle` 用的就是它们）。遭遇战窗接 skirmish 那张；
            //   排位那张**查不到**（本地只有 classic / skirmish）⇒ 子类给 null 时不画并出声。
            // 🔴 A866 收口后它建在 `parts.Plate`（= `Header Background`）底下 —— 与原版父链、与收口前的父件**同**
            //   （原来是这个方法里的局部 `bgT`）；树序也同：`Window Title` 先建、图标后建。
            float iconL = titleL + titleW + HdrGap;
            float iconT = HdrBg1T + (HdrBg1B - HdrBg1T - 100f) * 0.5f;
            var gmTex = Tex(GameModeIconArt);
            if (gmTex != null)
                MenuDraw.Rect(parts.Plate, gmTex, new PxRect(iconL, iconT, iconL + 100f, iconT + 100f),
                              "Game Mode Icon", QArt1, null, true);
            else
                Debug.Log("[Event] `Game Mode Icon` **没画** —— 这一窗的模式图标本地没有"
                          + "（原版 `sprite=0`、由 `LiveopUIDrawer_GameModeIcon` 运行时喂；"
                          + "本地只有 `40k_gamemode_icon_classic` / `_skirmish` 两张）");
        }

        /// <summary>把 `Header Background` 那一块按**新宽度**重摆（宽度 = 标题实宽算出来的那一档）。
        /// <para>🔴 为什么要有它：原版这一块挂 `ContentSizeFitterMinMax`（`widthMin 550 / widthMax 1250`），
        /// 内容宽 = padL 155 + 标题实宽 + 5.5 + 图标 100 + padR 61 —— 而**标题实宽要建完才量得到**
        /// （`Label.WorldW`），底板却必须先于标题建。⇒ 先按 `widthMin` 建，量到之后再撑开
        /// （只有内容宽 &gt; 550 才会走到这里：中文「经典」/「小冲突」都夹在下限上，英文 `Classic` 会超过）。
        /// 判据 → `资料/普查产出_1024b/S4_模式窗细节与徽章.md` §②。</para>
        /// <para>⛔ 只重建**那一片九宫**（`WithBackButton` 建的、`MenuDraw.Nine` 的缺省名 `"Nine"`）——
        /// 绝不动挂在底板底下的 `Window Title` 与 `Game Mode Icon`（它们是同一个父件的兄弟）。</para></summary>
        void ResizeHeaderPlate(Transform plate, float newR)
        {
            if (plate == null) return;
            // 🔴 **节点本身不动**（`Window Title` / `Game Mode Icon` 都挂在它底下 —— 挪节点会连带把它们挪走）。
            //   只把**那一片九宫**换成新宽的：`MenuDraw.Nine` 收的是**绝对矩形**、内部自己减父件位置
            //   ⇒ 图形落在新矩形上，兄弟件一个像素不动。
            for (int i = plate.childCount - 1; i >= 0; i--)
            {
                var c = plate.GetChild(i);
                if (c != null && c.name == "Nine" && c.GetComponentInChildren<Label>(true) == null)
                    RewardsWindow.DestroySafe(c.gameObject);
            }
            MenuDraw.Nine(plate, Tex(ArtHeaderBg), new PxRect(HdrL, HdrBg1T, newR, HdrBg1B),
                          WindowHeader.PlateBorder, WindowHeader.PlateTexW, WindowHeader.PlateTexH, QArt);
        }

        // ------------------------------------------------------------ `Battle!`
        /// <summary>`To Battle Button`（`UI_Button_Mulligan` **preserveAspect**）+ 文案 + `ButtonIcons`（两颗 100² 图标、
        /// HLG spacing **−28.31** · align **MiddleRight** · 🔴 **`reverse=1`** ⇒ 树序倒排，`TrophyIcon` 在左）。</summary>
        void BuildToBattle(Transform root)
        {
            var btn = MenuDraw.Node(root, "To Battle Button", new PxRect(BattleL, BattleT, BattleR, BattleB));
            var btBg = MenuDraw.Rect(btn, Tex(ArtMulligan), new PxRect(BattleL, BattleT, BattleR, BattleB),
                          "Bg", QArt, null, true);
            // 原版 hAlign = **Center** ⇒ 不调 `Align*`（原来右对齐了）
            // 🔴 **2026-10-17（`A1511(b)`）**：文案改走词条 —— 原版这颗 `Button Text` 挂 `Localize`，
            //   `mTerm = Demo/ToBattle`（en `Battle` · zh `战斗`；键与中文出处 → `Core/Loc.cs`）。
            //   原来写死的 `"Battle!"` 是 prefab 出厂占位串，⛔ 别按它保留英文。
            MenuDraw.Text(btn, new PxRect(BattleTxL, BattleTxT, BattleTxR, BattleTxB), Loc.T("Demo/ToBattle"),
                          Color.white, "Button Text", 74.25f, QText, face: TmpFont.Face.LatinCardBody);

            var icons = MenuDraw.Node(btn, "ButtonIcons", new PxRect(BIconsL, BIconsT, BIconsR, BIconsB));
            float cy = (BIconsT + BIconsB) * 0.5f;
            // 🔴 **2026-10-05 更正（倒排）**：原版 `ButtonIcons` 的 HLG 是 **`m_ReverseArrangement = 1`**
            //   ⇒ 树序 `[ShieldIcon, TrophyIcon]` 的**最后一个（`TrophyIcon`）落在最左**、`ShieldIcon` 在右。
            //   （**原文**：「HLG align = MiddleRight ⇒ 从右边往左排」—— **理由错了**：`align` 只管
            //    整排的**起点偏移**，**不决定子件顺序**；决定顺序的是 `m_ReverseArrangement`。
            //    照 `align` 推出来的「从右往左」正好把这一对画成了镜像。）
            //   判据 = uGUI `HorizontalOrVerticalLayoutGroup.cs:152-155`（`startIndex = reverse ? Count−1 : 0`
            //   / `increment = reverse ? −1 : 1`）· 原版跑后矩形
            //   （`python 工具/menu_dump.py bundle_menus_assets_all "SkirmishModeEventWindow" --depth 12 --md`，
            //   `RankedEventWindowV2` 逐值相同）：`TrophyIcon` **1285.97,928.10→1385.97,1028.10** ·
            //   `ShieldIcon` **1357.66,928.10→1457.66,1028.10**（右端那颗贴组右沿 1457.66 = `BIconsR`）。
            //   算式：组宽 442.091 − 排宽 171.69（= 100×2 + spacing(−28.31)）= **surplus 270.401**，
            //   `expandW=0` ⇒ 总 flexible = 0 ⇒ `pos = GetStartOffset(...)`（`align=5` ⇒ `alignmentOnAxis = 1`）
            //   ⇒ 整排右对齐 ⇒ 最左那颗左缘 = `BIconsL` 1015.57 + 270.401 = **1285.97**。
            float x1 = BIconsR - (BIconSide * 2f + BIconsGap);   // 1285.97
            float x2 = x1 + BIconSide;                           // 1385.97
            // 🔴 这两颗图标压在 `UI_Button_Mulligan` 那张底图上 ⇒ 必须用 **QArt1**（同队列会谁盖谁不可控）
            // 🔴🔴 **2026-10-24（E19 · 用户问「为什么有两个」）：原版这两颗【默认全灭】，按事件数据 `flags` 开关。**
            //   判据 = `d:/2/tools/decomp_full/RankedEventWindowV2__ArmySelectedOnSelectArmy.c:69-100`
            //     （与 `SkirmishEventWindow__SelectArmy.c:42-73` **逐句同构** —— 两扇窗都按同一套 flags 开关）：
            //     `TrophyIcon.SetActive(uVar4 & 1)` · `ShieldIcon.SetActive((uVar4 & 2) != 0)`，
            //     命中时再给**战斗钮底图**染色（`Selectable.get_image()` 的 color）：盾位优先取 `shieldedColor`，
            //     否则取 `boostedColor`（ranked 窗 = `param_1 + 0xe8` / `+0xf8`，skirmish 窗 = `+0xd0` / `+0xe0`）。
            //   `uVar4` = 事件接口那一路算出来的 flags（`(*(code *)*puVar5)(plVar7, army, 0, …)`）——
            //   **数据在服务器**，本地一条都没有 ⇒ 恒 0（= 两颗都灭，与参考图 `经典模式参考图.png` 逐像素一致：
            //   x1288..1398 / y888..1058 无亮块）。
            //   颜色值 = 两扇窗的 prefab 各现读一次、逐字相同（见 `BoostedColor` 那条 doc 的文件名）。
            //   ⛔ 改前是「无条件建 + 常亮 + 不染色」⇒ 用户看到多出两颗。
            _icTrophy = MenuDraw.Rect(icons, Tex(TrophyIconArt),
                          new PxRect(x1, cy - BIconSide * 0.5f, x2, cy + BIconSide * 0.5f), "TrophyIcon", QArt1, null, true);
            x1 = x2 + BIconsGap; x2 = x1 + BIconSide;            // 1357.66 → 1457.66（**贴组右沿**）
            _icShield = MenuDraw.Rect(icons, Tex(ArtShield),
                          new PxRect(x1, cy - BIconSide * 0.5f, x2, cy + BIconSide * 0.5f), "ShieldIcon", QArt1, null, true);
            _btBg = btBg;
            // 出厂态 = 生产侧当前能拿到的 flags（= 0，两颗都灭）。**节点照建、只关掉** —— 原版也是同一颗节点 `SetActive`。
            ApplyBattleBoostFlags(BattleBoostFlags);

            // 🆕 A17：原版 `To Battle Button` 是 SpriteSwap（普查 §块 4 第 14 行）
            MenuDraw.Hit(btn, "BattleHit", new PxRect(BattleL, BattleT, BattleR, BattleB), QHit, () => PlayButtonClicked(),
                         btBg, ArtMulligan, clickCue: WindowSounds.PositiveCue);
        }

        // ------------------------------------------------------------ 战斗钮那两颗徽标（`flags` 驱动）
        /// <summary>原版 `boostedColor` / `shieldedColor` —— **两扇窗的 prefab 各现读一次、值逐字相同**
        /// （`bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-7893144455456258128.json` = `RankedEventWindowV2` ·
        /// `…_-1807133450305712782.json` = `SkirmishModeEventWindow`）。命中时染的是**战斗钮那张底图**的 color。</summary>
        static readonly Color BoostedColor = new Color(1f, 0.92156863f, 0.015686275f, 1f);
        static readonly Color ShieldedColor = new Color(0f, 1f, 1f, 1f);

        ImageQuad _btBg, _icTrophy, _icShield;
        int _boostFlags;
        /// <summary>战斗钮那两颗徽标的 flags。**两扇窗在原版都按它开关**：
        /// `RankedEventWindowV2__ArmySelectedOnSelectArmy.c:69-100` 与 `SkirmishEventWindow__SelectArmy.c:42-73`
        /// **逐句同构**（`SetActive(·&amp;1)` / `SetActive((·&amp;2)!=0)` + 盾位优先的染色）。
        /// 🔴 **本地恒 0**：那份值由事件数据算出来（`IRankedEvent` 的接口调用，数据在服务器）——
        /// 我们一条都没有 ⇒ **两颗都灭**（原版在 flags=0 时就是这两句 `SetActive(false)`）。将来接数据只改这一处。</summary>
        protected virtual int BattleBoostFlags { get { return 0; } }
        /// <summary>现在生效的 flags（自检读它验「置 flags 之后真的开了」）。</summary>
        public int CurrentBattleBoostFlags { get { return _boostFlags; } }
        /// <summary>按 flags 开/关两颗徽标并给战斗钮底图染色 —— 原版那三句 `SetActive` / 两次 `set_color` 的等价物。
        /// `bit0` = `TrophyIcon` · `bit1` = `ShieldIcon`（盾位优先，两位都置时取 `shieldedColor`）。</summary>
        public void ApplyBattleBoostFlags(int flags)
        {
            _boostFlags = flags & 3;
            bool trophy = (_boostFlags & 1) != 0;
            bool shield = (_boostFlags & 2) != 0;
            if (_icTrophy != null) _icTrophy.gameObject.SetActive(trophy);
            if (_icShield != null) _icShield.gameObject.SetActive(shield);
            // 反编译那一段：`(flags >> 1 & 1) == 0` ⇒ 看 bit0 取 boosted；否则（bit1 置）取 shielded
            if (_btBg != null) _btBg.SetTint(shield ? ShieldedColor : trophy ? BoostedColor : Color.white);
        }

        /// <summary>`Help Button`（原版 `40K_generic_bt_info`，`m_LocalScale 0.7357` ⇒ 真渲出来 65.13²）。
        ///
        /// <para>🔴 **2026-10-17（本笔）接线** —— 原文是 `NotBuilt("…本地没有那份文案")`，**那条理由已过期**。
        /// 原版这一颗的开窗链**逐句实读**（`d:/2/tools/decomp_full/`）：
        /// `SkirmishEventWindow__HelpButtonClick.c` / `RankedEventWindowV2__HelpButtonClick.c` **逐行同构** =
        /// ① `LiveOpsAssetUtility.HasComponent(liveOps, 0x1e)`（`0x1e` = **30** = `EventComponentReferenceType.HelpPopup`）；
        /// ② `GetComponentReference(liveOps, 30)` → 一颗 `GameWindow`（就是某个 `BaseEventPopup` 变体 prefab）；
        /// ③ 虚表 `0x248` 那一跳（合法性判定）通过 ⇒ `WindowsManager.OpenWindow(组件, liveOps)`。
        /// ⇒ **「用哪一份变体」是事件数据里配的**（`EventComponents.HelpPopup`）。
        /// 🔴 **2026-10-19（`A1853②`）订正**：原文写「事件 JSON 在远端 CCD」—— **那一半已过期**：
        /// 13 份活动 SO 就在 `bundle_cosmeticsso_assets_all`，连 `EventComponents.HelpPopup` 的 GUID 都有
        /// （`2d7ec1a46a19245798bf28a5d1a030bb` ∈ `menus_assets_all.bundle`）。**仍在远端的只有「当前开着哪个事件」**。
        /// 我们这一侧：变体由 <see cref="HelpVariant"/> 给（缺省 = `Liveop Event Help Variant` —— 它三颗 tag 是
        /// `WelcomeTitle / Help / ForegroundImage`，正是「活动帮助」那一套）；文案键由
        /// <see cref="EventHelpTitleTerm"/> / <see cref="EventHelpDescriptionTerm"/> 给
        /// （**缺省问 `LiveOpsLocalCatalog`**；子类没声明 <see cref="LocalLiveOpsEventId"/> / 事件不在期 ⇒ null）
        /// ⇒ 弹窗照开、标题与正文**留空并出声**（= 原版 `GetLabel` 取不到标签时 `set_text(空)` 的画面结果）。
        /// ⛔ 别为了「别空着」拿 `Event_Description/Event/EventModeDesc` 之类的键顶进来（那会静默画错文案）。</para></summary>
        void BuildHelp(Transform root)
        {
            float cx = (HelpL + HelpR) * 0.5f, cy = (HelpT + HelpB) * 0.5f;
            float half = (HelpR - HelpL) * HelpScale * 0.5f;
            var r = new PxRect(cx - half, cy - half, cx + half, cy + half);
            var helpQ = MenuDraw.Rect(root, Tex(ArtHelp), r, "Help Button", QArt, null, true);
            var helpHit = MenuDraw.Hit(root, "HelpHit", r, QHit, () => HelpButtonClick());
            // 🆕 **2026-10-19（`N70` P1-7）**：原版这颗 `Help Button` 是 **`m_Transition = 1`(ColorTint)** ——
            //   可见件与命中区是两个节点（命中区那颗 quad 透明）⇒ 必须 `TintOn` 到可见件上，否则零反馈。
            //   色 = 全库默认（HL 0.9607843 / PR 0.7843137 / fade 0.1）⇒ `WindowButton` 的缺省值就是它，不覆盖。
            var helpWb = helpHit != null ? helpHit.GetComponent<WindowButton>() : null;
            if (helpWb != null && helpQ != null) helpWb.TintOn(helpQ);
        }

        /// <summary>本窗 `Help Button` 开哪一份事件弹窗变体。
        /// ⚠️ 原版的答案在 liveops 事件的 `HelpPopup` 组件引用里（`HasComponent(liveOps, 30)`，
        /// 判据见 `BuildHelp`）—— 事件数据在远端 CCD、本地没有 ⇒ 我们取「活动帮助」那一份
        /// （`Liveop Event Help Variant`）。子类要钉死就覆写它。</summary>
        protected virtual EventPopupVariant HelpVariant { get { return EventPopupVariant.LiveopEventHelp; } }

        /// <summary>帮助弹窗**标题**的官方词条键（= 事件 `EventLabel` 里 `WelcomeTitle`/`Title` 那一条的
        /// `RawValue`）。**缺省 = 问离线提供者**（`Shell/LiveOpsLocalCatalog.cs`）：
        /// 子类声明 <see cref="LocalLiveOpsEventId"/> 时按事件取 label 5/0；没声明 ⇒ null（本地查不到）⇒ 标题留空 + 出声。
        /// <para>🆕 **2026-10-19（`A1853②`）**：这是 12 个承载面里的 #1
        /// （`Event_Description/BattlePass/Title`，EC/SW 记分活动）—— 事件实例从本地目录来，⛔ 不猜。</para></summary>
        protected virtual string EventHelpTitleTerm { get { return LiveOpsLocalCatalog.HelpTitleTerm(LocalLiveOpsEventId); } }

        /// <summary>帮助弹窗**正文**的官方词条键（= 事件 `EventLabel` 里 `Help` 那一条的 `RawValue`）。
        /// 缺省同样问 `LiveOpsLocalCatalog`，但取的是 **label 12 `Help`**；没声明事件 id / 不在期 ⇒ null。
        /// 🆕 **2026-10-17（`A1853②`）**：`SkirmishEventWindow` 已覆写成记分活动那一对
        /// （`Event_Description/ExpansionPass/{Title,GeneralHelp}`）—— 见那边的注释与判据。</summary>
        protected virtual string EventHelpDescriptionTerm { get { return LiveOpsLocalCatalog.HelpDescriptionTerm(LocalLiveOpsEventId); } }

        /// <summary>🆕 **2026-10-19（`A1853②`）**：本窗对应的**本地活动实例 id**（13 份活动 SO 之一，
        /// 例 `ExpansionOrks1Scoring`）。缺省 null = 本窗**不挂**本地目录里的活动
        /// （遭遇战 / 排位这两种 eventId 都**不在**那 13 份 SO 里）。
        /// <para>子类声明它 ⇒ 上面两条词条自动从 `LiveOpsLocalCatalog` 取（label 5/0 与 12）；
        /// 只有**在期**事件才算（过期活动按用户边界不做）。</para></summary>
        protected virtual string LocalLiveOpsEventId { get { return null; } }

        /// <summary>🆕 2026-10-17（`A1853②`）：上面两条词条里的 `{0}` / `{1}` 实参（原版 `EventLabel.Args`）。
        /// 缺省 null = 不替换（词条里若真有 `{0}`，`BaseEventPopup` 会**出声**、
        /// ⛔ 不会静默把大括号印出去）。</summary>
        protected virtual string[] EventHelpTitleArgs { get { return null; } }
        /// <inheritdoc cref="EventHelpTitleArgs"/>
        protected virtual string[] EventHelpDescriptionArgs { get { return null; } }

        /// <summary>点 `Help` ⇒ 开事件说明弹窗（原版 `…HelpButtonClick` → `WindowsManager.OpenWindow`）。
        /// 返回开出来的那扇；**没有 `WindowsManager` 时返回 null 并出声**（红线：不静默失败）。</summary>
        public BaseEventPopup OpenHelp()
        {
            LastHelpPopup = null;
            if (Manager == null)
            {
                Debug.LogWarning("[Event] `Help Button`：**没有 `WindowsManager`**，开不了事件说明弹窗"
                                 + "（原版那一跳是 `WindowsManager.OpenWindow`；这里如实说、不假装打开）");
                return null;
            }
            var w = BaseEventPopup.Create(Manager, HelpVariant, EventHelpTitleTerm, EventHelpDescriptionTerm, name,
                                          EventHelpTitleArgs, EventHelpDescriptionArgs);
            Manager.OpenWindow(w);
            LastHelpPopup = w;
            Debug.Log("[Event] `Help Button` ⇒ 开出 `" + BaseEventPopup.RootNameOf(HelpVariant) + "`"
                      + "（原版 `HelpButtonClick`：`HasComponent(liveOps, 30 HelpPopup)` ⇒ `OpenWindow(组件, liveOps)`）");
            return w;
        }

        /// <summary>最近一次由 `Help` 开出来的那扇（自检用；同族先例 = <see cref="LastOpened"/>）。</summary>
        public static BaseEventPopup LastHelpPopup;

        // ============================================================ 开战：原版那条链
        /// <summary>点 `Battle!` ⇒ 照原版 `MatchMakerManager.StartMatch`：**先显示 `Searching Oponent Popup`，
        /// 等 `WaitForOpponentSeconds`（= 12s，原版离线时的值）**，等不到真人就去打 bot。</summary>
        public virtual void StartMatch()
        {
            NetTookOver = false;      // 🆕 先归零，下面只有联机真接管了才置真（见属性注释）
            // 🆕 2026-09-26：**先过模式这一关**（原版 `DeckUtility.ValidateDeck(deck, out err, isSkirmish)`
            //   被 `SkirmishEventWindow.OnDeckSelected → HasValidDeckWithValidationMessage` 调）——
            //   ⚠️ 拦截**不是**静默：弹窗说清「哪一副、为什么不行、怎么办」。
            if (!SelectedDeckFitsMode(out string whyMode))
            {
                Debug.LogWarning("[Event] 开战被挡：模式不对 —— " + whyMode);
                if (Manager != null) Manager.ShowPopUp(whyMode, Loc.T("MainMenu/General/OK"), null);
                return;
            }
            var d = CollectionData.DeckAt(DeckIndex);
            if (string.IsNullOrEmpty(d.WarlordId) && PickedPrebuilt == null)
            {
                Debug.LogWarning("[Event] 这套卡组**没有督军**，开不了局 —— 如实说，不静默。");
                // 弹窗正文走**整句**词条（键 `MenuDeck/Error/CantStartNoWarlord`；
                //   `A1034` 裁定：这里原来复用了短键 `MenuDeck/Error/NoWarlord`（「还没有选战将」）⇒ 正文从
                //   「这套卡组还没有选战将，开不了局。」缩成 5 个字 = 玩家可见内容缩水 ⇒ **改回整句**）；
                // 钮文案 `MainMenu/General/OK`（见 `Core/Loc.cs` 里那一条的留痕块，⚠️ 是 `OK` 不是 `Ok`）。
                if (Manager != null) Manager.ShowPopUp(Loc.T("MenuDeck/Error/CantStartNoWarlord"), Loc.T("MainMenu/General/OK"), null);
                return;
            }
            // 🆕 2026-09-26（N3）：**联机已连上 ⇒ 走 P2P**，不跑那 12 秒 bot 链
            //    （判据 → `资料/联机P2P_设计与交接.md` §六 N3）。没接管时照旧，单机行为一字不改。
            {
                var pre0 = PrebuiltDecks.PendingSource;
                var pd = pre0 != null ? PrebuiltDecks.ToPlayerDeck(pre0) : CollectionData.Raw(DeckIndex);
                string modeStr = DeckGameMode == 13 ? "Skirmish" : "Classic";
                if (NetMatchmaking.TryStart(pd, modeStr,
                                            pre0 != null ? pre0.faction : d.Faction, out string netWhy))
                {
                    NetTookOver = true;
                    Debug.Log($"[Event] 这一局走**联机**（{modeStr}，本机交了卡组「{d.Name}」）—— 不跑 12 秒 bot 链");
                    // 🆕 2026-10-03（A2）：**必须把「在等对面」显示出来** —— 那三扇是 `currentWindow`，
                    //    全屏那扇会把它们顶掉 ⇒ 基类默认走**窗内**那扇 `Searching Oponent Popup`。
                    OnNetSearchStarted();
                    return;
                }
                Debug.Log($"[Event] 联机没接管（{netWhy}）⇒ 照旧走「等 12 秒再打 bot」那条链");
                // 🔴 **红线**（`项目任务.md` §三 第 14 条 表 第 5 条）：**配了联机却没连上要说一声**，
                //    否则玩家填了 IP、忘了点【检查连接】，就会无声无息打个 bot 还以为是真人。
                //    ⚠️ 判定「该不该说」在那一处（单机玩家不打扰）—— 别在这儿再写一遍。
                NetMatchmaking.ExplainNotTakingOver(netWhy);
            }
            _search.OnCancel = CancelSearch;
            _search.OnSearchDone = () => { OnSearchFinished(); StartBotBattle(); };
            _search.BeginSearch(ShowInlineSearchPopup);
        }

        /// <summary>🆕 这一局**交给联机了**（`StartMatch` 里 `NetMatchmaking.TryStart` 返回真）。
        /// 子类（排位窗）据此在 P2P 那条路上也开「找对手」那扇全屏窗 —— 那条路**不跑 12 秒链**，
        /// 不开窗的话玩家在等对面的时候**屏幕上什么都看不到**（判据 → `资料/阶段二_多人界面_原版规格.md` §6·4）。</summary>
        protected bool NetTookOver { get; private set; }

        /// <summary>🆕 **2026-10-03（A2）**：联机接管了 ⇒ 把「在等对面」放到屏幕上。
        /// 基类默认 = **显示窗内那扇 `Searching Oponent Popup`**（练习/遭遇/找对手三扇是 `currentWindow`，
        /// 开全屏那扇会把它们顶掉）；排位窗覆写成开**全屏** `SearchingOpponentWindow`（它是弹窗，顶不掉）。
        /// ⚠️ 这条链 **不跑 12 秒 bot 倒计时** —— 等的是真人。</summary>
        protected virtual void OnNetSearchStarted()
        {
            if (_search == null) return;
            _search.OnCancel = CancelSearch;
            _search.BeginNetWait(ShowInlineSearchPopup);
        }

        /// <summary>匹配那一步**要不要显示窗口内的 `Searching Oponent Popup`**。
        /// 排位窗改成 `false` —— 它走**全屏** `SearchingOpponentWindow`（本地入口是我们定的），
        /// 两扇一起显示会叠在一起（实拍抓到过）。**倒计时仍然只有一份**（在 `SearchingMatchPopup` 里）。</summary>
        protected virtual bool ShowInlineSearchPopup { get { return true; } }

        /// <summary>匹配等满时先收掉子类多开的东西（排位那扇全屏搜索窗），再开战。</summary>
        protected virtual void OnSearchFinished() { }

        /// <summary>推进匹配。🔴 **2026-10-22（W64）起生产路径不经过它** —— 每帧由常驻
        /// <see cref="SearchOpponentManager"/> 直接推 `_search`（判据 = 原版 `SearchingOpponentWindow` 没有 `Update`，
        /// 等待在 `SearchOpponentManager` 的协程上）。**本方法只留给自检直调**（批处理没有帧循环）。
        /// ⛔ 别再把 `void Update() { TickSearch(Time.deltaTime); }` 加回来：这一族宿主会被全屏窗 `SetActive(false)`
        /// （真机上 12 秒就冻在那儿），而且会和常驻 ticker **一帧推两次**。</summary>
        public void TickSearch(float dt) { if (_search != null) _search.Tick(dt); }

        /// <summary>取消匹配后宿主窗口要复位的东西（窗口本身**留着**，玩家还站在这里）。</summary>
        public virtual void CancelSearch()
        {
            Debug.Log("[Event] 取消匹配（原版 `SearchingOpponentWindow__CancelMatchMatchmaking → MatchMakerManager.CancelSearch`）");
            // 🔴 **2026-10-22（W64）：取消 ⇒ 清状态 + 从常驻 ticker 注销**（= 原版 `CancelBattleSearch → StopCoroutine`）。
            //    原来这一句**没有**：单机那一支下面第一行就 `return` 了，`Searching`/`SecondsLeft` 原样留着 ——
            //    以前靠「关全屏窗必重建宿主 ⇒ 旧 popup 随销毁丢掉」侥幸没露，但那是**脆的**：
            //    只要宿主那一扇走 `Background` 支（不重建），常驻 ticker 会接着推 ⇒ **取消之后 12 秒又开一局**。
            //    ⚠️ 用 `AbortSearch()`（**不**回调 `OnCancel`）—— `Cancel()` 会经 `OnCancel` 再回到本方法，绕成一圈。
            if (_search != null) _search.AbortSearch();
            // 🆕 **2026-10-03（A1）：联机那一支要【真拆局】**。
            //    原来只关窗 ⇒ 配对已经成了，对面照样开局，`MsgStart` 一到还是会被拉进战场。
            //    ⚠️ **这条链是我们设计的、不是复刻**（原版那是服务端撤单）—— 判据 → `NetMatchmaking.Cancel`。
            if (!NetTookOver) return;
            if (NetMatchmaking.Cancel("对局发起方点了取消", out string why))
            {
                NetTookOver = false;
                // 整句走词条（键 `Settings/Online/MatchCancelled`）。
                NetRuntime.Notice(Loc.T("Settings/Online/MatchCancelled"));
            }
            else
            {
                // **不假装取消成功**（红线）：说清为什么、以及该怎么办。
                // 走词条（键 `Settings/Online/MatchCancelFailed`）。
                NetRuntime.Notice(Loc.T("Settings/Online/MatchCancelFailed").Replace("{0}", why));
            }
        }

        /// <summary>打 bot 那一支：选定卡组 + 切该阵营那份对战场景
        /// （= 原版 `SearchOpponentManager.StartBattle → GetBattleArena → LoadScene` 的等价物）。
        ///
        /// <para>🔴 **2026-10-15（A383 收尾 · 纯形状改动 · 零行为改动）**：本方法原来是 `public` **非虚** ——
        /// 于是两扇子类**没法在「真正切场景那一刻」插手**，只能在基类紧接着调本方法的那个钩子上设模式号
        /// （`_search.OnSearchDone = () => { OnSearchFinished(); StartBotBattle(); }`，
        /// 两扇子类的覆写在 `Shell/SkirmishEventWindow.cs` / `Shell/RankedEventWindow.cs` 里）
        /// —— 语义上够近，但**是间接的**（且每加一个子类都要记得补那句）。
        /// 改成 `public virtual` 后，子类可以覆写本方法、在 `base.StartBotBattle()` **之前**做
        /// 「切场景前的最后一件事」（例如写模式号）。⚠️ **今天没有任何子类覆写它** ⇒ 行为**逐位不变**
        /// （只多一个 vtable 槽：`本文件` 那个委托里的 `StartBotBattle()` 仍落到同一个方法体）。</para>
        ///
        /// <para>⛔ **为什么不走「基类加一个 `protected virtual GameMode PlayModeForWindow` 口」那条**：
        /// 今天**没有子类能覆写它**（两扇子类在 `Shell/SkirmishEventWindow.cs` 与
        /// `Shell/RankedEventWindow.cs` —— 那是另一笔账的文件范围）⇒ 加出来是个**死钩子**；
        /// 若改由基类自己调、拿一个默认档兜底，就会用 `Classic 0` **盖掉**遭遇窗已经设好的
        /// `Skirmish 13`（`OnSearchFinished` 里设的，基类紧接着就切场景）⇒ **真回归**。
        /// 判据 / 来历 → `资料/普查产出_1015/W4_A383模式号.md` §四·3 / §五·1。</para>
        ///
        /// <para>⚠️ **联机那一支不走这里**：`NetTookOver` 时基类不调本方法（模式号随开局包走，
        /// 见 `NetPendingBattle.PlayMode`）—— 覆写时别以为它覆盖了那条路。</para></summary>
        public virtual void StartBotBattle()
        {
            var info = CollectionData.DeckAt(DeckIndex);
            // 🔴 本局用哪副牌：挑过**预组**就走预组那条（`PrebuiltDecks` 的「本局用这副牌」通道），
            //    否则照旧用「我的卡组」—— 原因见 `PracticeModePopup.StartBotBattle` 那段注释。
            var pre = PrebuiltDecks.PendingSource;
            string faction = pre != null ? pre.faction : info.Faction;
            string deckName = pre != null ? pre.DisplayName : info.Name;
            if (pre == null) CollectionData.Select(DeckIndex);
            StartedBattle = true;
            var scene = BattleSceneFor(faction);
            Debug.Log("[Event] 开战：「" + deckName + "」"
                      + (pre != null ? "（**预组卡组** " + pre.deckId + "）" : "")
                      + "→ 切 `" + scene + ".unity`"
                      + "（原版 `StartMatch → StartBotBattle → StartBattle → LoadScene`，唯一 LoadScene 点；"
                      + " 照原版查表，督军阵营「" + faction + "」该去 `" + ArenaByArmy.OriginalNameFor(faction)
                      + "`（我们的键 `" + ArenaByArmy.SceneFor(faction) + "`）"
                      + (scene == "Battle"
                         ? " —— ⚠️ **该场那份场景还没建，这一局用的是兜底 `Battle`（战场 = " + ArenaByArmy.DefaultScene + "）**"
                         : "）"));
            if (Application.isBatchMode) { Debug.Log("[Event] （批处理：不切场景，只记账）"); return; }
            // 🔴 **2026-10-17（界面线 T16）**：同 `PracticeModePopup` —— 先走壳侧 `LoadingMenu` 黑场再切场景。
            ShellRuntime.LoadSceneWithFade(scene);
        }

        /// <summary>点 `Create deck`：**建一副「本窗模式」的卡组，然后进卡组编辑**。
        ///
        /// 照原版那条链：`SelectDecksTab.CreateDeck` = 造一个空 `CardDeck` → **把当前模式写进它**
        /// → **紧接着打开 `DeckEditingWindow`**（`SelectDecksTab__CreateDeck.c:17,20,24`）。
        /// 编辑器那边不认识「当前模式」，它从这副卡组上读（判据 → `资料/加时与冲突模式_原版规格.md` §2.7）
        /// ⇒ 所以**模式必须在建组这一刻带上**，晚一步就没机会了（建完没有改的路径）。
        ///
        /// 我们的编辑器是**独立场景**（`DeckEditor`），交接办法与「从收藏进编辑」同一条
        /// （`CollectionData.PendingEditDeck`，静态字段跨场景）。
        ///
        /// <para>🔴 **2026-10-14（A549② + A612）**：`CollectionData.CreateDeck` 自 A503 起**失败时返回空串**
        /// （内存里那套还在、只是没写进存档）⇒ 原来这里 `IndexOf("")` 会是 **−1**。
        /// 行为**本来是良性的**（见下面那一支的注释），但那两条账要求：**返回值一旦有人读，
        /// 就必须同时处理「新建失败」这一支** —— ⛔ 别只写 `if (!Select(idx))` 抄过去。
        /// A612 的**「要不要拦下来」**不在这里裁（那笔账在 A613 / A609 一族）。</para></summary>
        public void CreateDeckInMode()
            => CreateDeckInMode(WindowsManager.Instance != null, Application.isBatchMode);   // ⛔ 生产只看这一行

        /// <summary>🆕 **2026-10-13（`A1132` 第 3 步）**：与 `Shell/CollectionWindow.GoEdit(int, bool, bool)`
        /// **同一个形状、同一张表**（`CollectionWindow.RouteDeckEntry`）—— 判据、「生产只走单参那一版」、
        /// 以及「缝只能喂判定值、伪造不出一个壳」→ `GoEdit` 那一段，⛔ 别在这儿抄第二份。</summary>
        public void CreateDeckInMode(bool hasShell, bool batch)
        {
            int mode = DeckGameMode;
            string name = CollectionData.CreateDeck(mode);
            // 🔴 **2026-10-12（`A1697`）改向（铁律 5：原文留痕）**：这里原来还有一整段「下标交接」逻辑
            //   （`IndexOf(name)` → `Select(idx)` → `PendingEditDeck = idx`，外加「没选中 / 没写进存档」两条警告，
            //   出处见 `资料/普查产出_第二十三会话/R1_没选战将的卡组.md` §二）。
            //   **它随着原版那条链一起作废了**：`CollectionData.CreateDeck` 自 A1697 起**不入库**
            //   （照原版 `SelectDecksTab__CreateDeck.c:17,26,28`）⇒ 新卡组在库里**没有下标**、`IndexOf(name)` 恒 −1，
            //   照旧写下去会让编辑器**跳进别的卡组**（= 静默开错牌）。现在交接的是**那份对象本身**
            //   （`CollectionData.PendingNewDeck`，由 `CreateDeck` 写好）—— 本口**一个字段都不用再写**。
            bool created = !string.IsNullOrEmpty(name);
            CollectionData.PendingEditDeck = -1;   // ⛔ 显式清：留着上一次的旧下标会跳进**别的**卡组
            if (!created)
                Debug.LogWarning("[Event] 新建卡组**没造出来**（⛔ 库里一套都没多、编辑器也拿不到它）"
                                 + " —— 见上一条 `[CollectionData]` 警告，⛔ 别在这里另写一套原因");
            Debug.Log("[Event] 新建卡组" + (created ? "「" + name + "」" : "（⚠️ 名字拿不到、见上一条警告）") + "· 模式 "
                      + (mode == (int)GameMode.Skirmish ? "遭遇 Skirmish（12 张）" : "经典 Classic（30 张）")
                      + "（照原版 `SelectDecksTab.CreateDeck`：**建组这一刻定模式**，之后没有改的路径）"
                      + " ⇒ 进卡组编辑（交接的是那份**未入库**的新卡组：**Done 过了校验才入库**）");
            // 🆕 2026-10-18（A855）：**这条来路的回程意图**
            //   （🔴 **2026-10-12（`A1697`）就地订正（铁律 5）**：原文写「全仓仅有两处 `LoadScene("DeckEditor")`，
            //    这是**第二处**」—— **现在是三处**，第三处 = `Shell/CollectionWindow.GoNewDeck`；本处仍是第二处）—— 「来源由入口决定、不由离场方式决定」⇒ 写在**进编辑器**这一刻，
            //   ⛔ 别挪到 `DeckRuntime.BackToMenu` 去；⚠️ 写在批处理闸**之前**（批处理不切场景，
            //   自检只能靠这个意图观测）。⚠️ 回程**今天开不了这扇窗** —— 本地没有事件数据
            //   （理由逐条 → `MainMenuRuntime.Build` 里那一段）⇒ 只记来源，那一跳还开着。
            CollectionData.SetReturnIntent(DeckExitSource.LiveOpsEvent, WindowTabType.None);
            if (Application.isBatchMode) { Debug.Log("[Event] （批处理：不切场景，只交接）"); return; }
            // 🆕 **2026-10-13（`A1132` 第 3 步）**：**进编辑 = 开那扇窗**（与 `Shell/CollectionWindow.GoEdit`
            //   同一口径、同一张落点表 `CollectionWindow.RouteDeckEntry`）。
            //   ⚠️ 本口交接的也是 `PendingNewDeck`（**未入库**那一份，见上面 `CreateDeck(mode)`），
            //   同样会被窗里 `Build` 开局读掉 ⇒ 与那两处同理。
            //   ⚠️ 与另外两处**唯一的不同**：这里的上一扇是**事件窗自己**（收藏窗那条路上一扇是收藏窗）
            //   —— 它归第 5 步的遮让去管，⛔ 本步不动。
            //   ⛔ 生产路径只走上面那个单参重载（它读现场的 `WindowsManager.Instance` 与 `Application.isBatchMode`）；
            //   本重载是**自检缝**（`Editor/CollectionScene.cs` 只用它断那张表）。
            if (WindowsManager.Instance != null)
            {
                var deWin = DeckEditingWindow.OpenFrom(WindowsManager.Instance);
                Debug.Log("[Event] 新建卡组 ⇒ **开出卡组编辑窗**（" + (deWin != null ? deWin.name : "<没开出来>")
                          + "）—— 照原版 `SelectDecksTab.CreateDeck`：**叠一扇窗**，⛔ 不切场景");
                return;
            }
            Debug.Log("[Event] 没有壳（`WindowsManager.Instance == null`）⇒ 退回老路：切 `DeckEditor` 场景");
            UnityEngine.SceneManagement.SceneManager.LoadScene("DeckEditor");
        }

        // ============================================================ 交互
        /// <summary>点阵营格 = 原版 `RankedDeckSelector.ChangeArmy(army)`：**换阵营 ⇒ 换那一栏**。
        /// <para>🔴 **2026-10-24（E11 · P0）**：原版 `ChangeArmy.c` 的语义是
        /// ① 记下新阵营；② 用它重建「阵营 ∧ 模式」那张表；③ **当前那套若已属于该阵营就保持**，
        /// 否则取表里 `FirstOrDefault`（**表空 = null = 空态**）；④ 四颗圆钮按表长开关。
        /// 改前这里只在「找得到该阵营 + 模式对的卡组」时才改 `DeckIndex`，找不到就**什么都不动**
        /// ⇒ 点一个没有卡组的阵营 = **画面原地不动**（用户 2026-10-24 点名的那条）。</para></summary>
        void PickArmy(int i)
        {
            // 🔴 **2026-10-18（主对话裁定 · 按原版 `allowClickWhenAlreadySelected = 0` 订正）**：
            //   原版 `Ranked Army Selector Container V2` 已选中再点 = **什么都不做**（不播音、不取消筛选）。
            //   改前这里是 `ArmyIndex = (ArmyIndex == i) ? -1 : i`（「再点一次 = 取消筛选」）——
            //   那是**我们自设计的语义**，已删。同格再点现在由命中件的
            //   `allowClickWhenAlreadySelected = false` 守卫挡在派发之前（连声音一起，见 `BuildArmySelector`）。
            //   ⚠️ 原版 prefab 里**没有**单独的清除/All 入口（本轮 `menu_dump` 现读 `SkirmishModeEventWindow` 整棵树：
            //   `Army Selector` 下只有 `Factions Title` + `Army Content`（ToggleGroup + GridLayoutGroup），
            //   没有任何 Clear/Reset/All 节点）⇒ **不自己补一个入口**；「不限阵营」这一档也**不再**是开窗初值。
            string fac = FactionOfIndex(i);
            if (fac == null)
            {
                Debug.LogWarning("[Event] 点阵营格 " + i + " 时 `_facs` 还没建 / 越界 ⇒ 什么都不做（⛔ 不静默改状态）");
                return;
            }
            ArmyIndex = i;
            _armyFactionSel = fac;
            // 🔴 **2026-10-21（真缺陷）**：**改阵营筛选 = 从「我的卡组」里挑** ⇒ 先把预组那一档撤掉。
            //   病根：本窗这一栏的显示判源是**预组优先**（`ShownPrebuilt` / `ShownDeckName` / `ShownWarlord` /
            //   `ShownFaction` 四处），开战那条路也读 `PickedPrebuilt`（`SelectedDeckFitsMode` / `StartMatch`）
            //   ⇒ 留着它，「点阵营格」就**看起来点了没反应**（那一栏还画着预组、开战还用预组）。
            //   顺序照「我的卡组」那一支（`OpenDeckSelection` 的回调）：改下标 → 清预组 → 排空通道 → 刷新。
            PickedPrebuilt = null;
            PrebuiltDecks.ClearPendingBattleDeck();
            // 🔴 原版 `ChangeArmy` ③：当前那套**就在这个阵营的表里** ⇒ 保持（`CardDeck.get_Army(current) == army`
            //    那一支）；否则取该阵营表里的第一套；**表空 ⇒ `DeckIndex = -1` = 空态**。
            if (!InArmyDeckList(DeckIndex, fac)) DeckIndex = FirstDeckInArmy(fac, DeckIndex);
            if (_armyHolder != null) RebuildArmyCells(_armyHolder);
            RefreshDeckColumn();
            Debug.Log("[Event] 阵营筛选：" + fac + "（该阵营 ∧ 本窗模式 " + ArmyDeckCount() + " 套"
                      + (DeckIndex >= 0 ? "，选中第 " + (ShownDeckOrdinal() + 1) + " 套「" + ShownDeckName + "」"
                                        : " ⇒ **空态**（这套都没有 ⇒ 画徽记 + 提示 + Create deck）") + "）");
        }

        /// <summary>上/下一套 —— 只在「阵营 ∧ 本窗模式」那张表里循环（原版 `OnNextDeckButtonClick` 走
        /// `currentArmyCardDeckList` 的 `Next`）。表里 ≤1 套时什么都不做（原版那两颗钮本来就关着）。</summary>
        void StepDeck(int dir)
        {
            int total = CollectionData.DeckCount();
            if (total <= 0) return;
            // 🔴 **2026-10-24（E11）**：改前这里在**全库**里循环、只「跳过」不匹配的，且**找不到也照样落子**
            //   （`DeckIndex = cur` 最后落在某套别的模式/阵营的牌上 = 静默换错牌）。原版是「只在表里 Next」。
            if (ArmyDeckCount() <= 1)
            {
                Debug.Log("[Event] 换卡组：本阵营 ∧ 本窗模式只有 " + ArmyDeckCount() + " 套 ⇒ "
                          + "原版这两颗箭头是关着的，什么都不做");
                return;
            }
            string fac = SelectedArmyFaction;
            int cur = DeckIndex;
            for (int k = 0; k < total; k++)
            {
                cur = ((cur < 0 ? 0 : cur) + dir + total) % total;
                if (InArmyDeckList(cur, fac)) break;
            }
            if (!InArmyDeckList(cur, fac))
            {
                Debug.LogWarning("[Event] 换卡组：表里明明有 >1 套却绕了一圈没找到（" + total + " 套里阵营「" + fac
                                 + "」∧ 模式 " + DeckGameMode + "）⇒ 不动 `DeckIndex`（⛔ 不落到别的模式上）");
                return;
            }
            DeckIndex = cur;
            // 🔴 **2026-10-21（真缺陷）**：同 `PickArmy` —— 上/下一套这两颗箭头改的是「我的卡组」那一档，
            //   而这一栏的显示与开战都是**预组优先**（`ShownPrebuilt` / `PickedPrebuilt`）
            //   ⇒ 不撤预组的话这俩箭头**看起来点了没反应**（那一栏还是那副预组、开战还是它）。
            //   顺序照 `OpenDeckSelection` 的自建支：改下标 → 清预组 → 排空通道 → 刷新。
            PickedPrebuilt = null;
            PrebuiltDecks.ClearPendingBattleDeck();
            RefreshDeckColumn();
            Debug.Log("[Event] 换卡组：" + CollectionData.DeckAt(DeckIndex).Name
                      + "（表内第 " + (ShownDeckOrdinal() + 1) + "/" + ArmyDeckCount() + " 套）");
        }

        /// <summary>把「选中阵营」同步到**当前画出来的那套**上（= 原版 `ChangeDeck.c:51-63` 那句
        /// `currentArmy(+0xa0) = deck.warlord.cardArmy`：选卡组弹窗挑完，阵营跟着这副走）。
        /// 只给「挑完卡组」那两条路用 —— 点阵营格走的是 `PickArmy`（方向相反）。
        /// ⚠️ 同步后要重画阵营格：选中态的底图（`UI_Army_Selection_Back_Pressed`）挂在那一格上。</summary>
        void SyncArmyToShownDeck()
        {
            string fac = ShownPrebuilt != null ? ShownPrebuilt.faction : FactionOfDeck(DeckIndex);
            if (string.IsNullOrEmpty(fac)) return;
            if (DeckRules.SameFaction(fac, SelectedArmyFaction)) return;
            _armyFactionSel = fac;
            ArmyIndex = IndexOfFaction(fac);
            if (_armyHolder != null) RebuildArmyCells(_armyHolder);
            Debug.Log("[Event] 卡组那一栏换成了阵营「" + fac + "」的牌 ⇒ 选中阵营跟着走（右栏那一格按下）");
        }

        /// <summary>把「卡组那一栏」重建一遍（换阵营 / 换卡组之后）。
        /// 🔴 **重建而不是原地改**：`CardView` 那条教训（`SetData` 只更新已存在的层）—— 这里同理，
        /// 「督军立绘 / 阵营徽记 / 有没有有效卡组 / 四颗圆钮的显隐与居中 / 张数文本」都会整块变。</summary>
        void RefreshDeckColumn()
        {
            var col = transform.Find("Ranked Deck Selection");
            if (col == null) return;
            RewardsWindow.DestroySafe(col.gameObject);
            BuildDeckSelection(transform);
            OnDeckColumnChanged(); RememberEventDeck();   // 2026-10-26（X4）：这一栏重建完 ⇒ 通知子类 ｜ B90-N3：并把「本活动这一副」记进每活动槽（类尾）
        }

        /// <summary>开 `Deck Selection Popup with Tabs`（原版 `RankedDeckSelector` 那颗换卡组钮）。</summary>
        public DeckSelectionPopup OpenDeckSelection()
        {
            LastDeckSelection = null;
            if (Manager == null) { Debug.LogWarning("[Event] 没有 `WindowsManager`，开不了 `Deck Selection Popup`"); return null; }
            var w = DeckSelectionPopup.Create(Manager, pick =>
            {
                // 原版这条是 `RankedDeckSelector.OnSelectDeckButtonClick` → `ChangeDeck(deck)`
                // ⇒ **登记**（记 currentDeck + `currentEvent.SetDefaultDeck`），不分支。我们这边要分开：
                // 预组**不在** `DeckLibrary` 里，拿名字回查必然 −1 = **静默无事发生**（撞红线）。
                if (pick.Prebuilt)
                {
                    PickedPrebuilt = pick.PrebuiltDeck;
                    PrebuiltDecks.SetPendingBattleDeck(pick.PrebuiltDeck);
                    SyncArmyToShownDeck();      // 🆕 E11：挑的是别的阵营的牌 ⇒ 右栏那一格/圆钮/张数跟着走
                    // 🔴 **2026-10-21（真缺陷）**：这一支原来**少了这一步** —— 只写状态、不重建卡组那一栏
                    //   ⇒ 挑完预组之后 `Ranked Deck Selection` 还是上一副（自建那一支一直有这一跳，见下一行）。
                    //   ⚠️ `DeckIndex` **故意不动**（预组不在 `DeckLibrary` 里、没有下标可指）：
                    //   这一栏现在靠 `ShownPrebuilt`/`ShownWarlord`/`ShownDeckName`/`ShownFaction` 解析出**预组那一副**。
                    RefreshDeckColumn();
                    Debug.Log("[Event] 选中**预组卡组**「" + pick.PrebuiltDeck.DisplayName + "」("
                              + pick.PrebuiltDeck.deckId + ") ⇒ 本局就用它（防御卡 = 我们补的「"
                              + pick.PrebuiltDeck.defensiveNameZh + "」" + pick.PrebuiltDeck.defensiveId + "）");
                    return;
                }
                // 🔴 **2026-10-31（B104_P4）**：原来拿**卡组名**回查下标（`CollectionData.IndexOf(pick.Info.Name)`）
                //   ⇒ 同名卡组永远落到第一套（选错牌组、且不报错）。`DeckPick.OwnIndex` = 列表构造那一刻的库内下标
                //   （`DeckSelectionPopup.Shown()` 里就是 `i`），它才是身份。⛔ 别退回按名回查。
                int idx = pick.OwnIndex;
                if (idx >= 0 && idx < CollectionData.DeckCount())
                {
                    DeckIndex = idx;
                    PickedPrebuilt = null;
                    PrebuiltDecks.ClearPendingBattleDeck();
                    SyncArmyToShownDeck();  // 🆕 E11：同预组支
                    RefreshDeckColumn();
                }
                else Debug.LogWarning("[Event] 选卡组回调拿到的下标 " + idx + " 不在库里（" + CollectionData.DeckCount()
                                    + " 套）⇒ **不选**（出声，不静默无事发生）");
            }, DeckGameMode, info => DeckRules.SameFaction(info.Faction, SelectedArmyFaction), CurrentDeckModeForSelection());   // 🆕 2026-09-26 模式筛 + 🆕 B90-N3：第 4 个实参谓词 = 原版 `d.Army == currentArmy` ｜ 🆕 B92_N9：第 5 个实参 = 原版 `context.Deck.GameMode`（`_TryOpen_b__10_0` 的**二选一**闸，见 `DeckSelectionPopup.ModePasses`）
            Manager.OpenWindow(w);
            LastDeckSelection = w;
            Debug.Log("[Event] 开 `Deck Selection Popup with Tabs`");
            return w;
        }
        public static DeckSelectionPopup LastDeckSelection;

        /// <summary>看这套卡组（原版 `View Deck Button`）⇒ 开 `Deck info Popup`。</summary>
        public DeckInfoPopup OpenDeckInfo()
        {
            LastDeckInfo = null;
            if (Manager == null) { Debug.LogWarning("[Event] 没有 `WindowsManager`，开不了 `Deck info Popup`"); return null; }
            // 🆕 **2026-10-24（E11）**：空态没有「这套卡组」可看 ⇒ 如实出声、不开一扇空窗
            //   （原版那颗 `viewDeckButton` 在表空时是 `SetActive(false)`，点不到；这里是直调入口的兜底）。
            if (PickedPrebuilt == null && DeckIndex < 0)
            {
                Debug.LogWarning("[Event] 空态（没有本窗模式、本阵营的卡组）⇒ `View Deck Button` 无牌可看，不开窗");
                return null;
            }
            DeckInfoPopup w;
            var pre = PickedPrebuilt;
            if (pre != null)
            {
                // 🔴 **2026-10-21（真缺陷）**：预组**不在 `DeckLibrary` 里**（`DeckIndex` 恒指不到它）⇒
                //   原来这里恒传 `DeckIndex` ⇒ 选中预组之后点这颗眼睛，开出来的是**库里那一副**
                //   （名字 / 督军 / 卡表全不是面板上这副）。修法照 `Shell/PracticeModePopup.cs` 的
                //   `ShowDeckContent`（同一批真机缺陷的先例）：`Create(Manager, -1, …)` + `OverrideDeck` /
                //   `OverrideInfo` / `IsPlayerDeck = false` —— 那两个字段是 `DeckInfoPopup` 给
                //   「外面送进来的卡组」留的**唯一那个口**（`CurDeck` / `CurInfo` 只此一处判源）。
                //   ⚠️ **不**走 `CreateForPicked`：那一支强制 `SelectButtonProvided = true`（会多一颗
                //     `Select Deck`）；原版这颗 `View Deck Button` 传的 `selectButton = null`
                //     （判据 = `decomp_full/RankedDeckSelector__OnViewDeckButtonClick.c` 的
                //      `DeckInfoContext___ctor(ctx, currentDeck, 0, 0, 1, 0, 0)`）。
                //   ⚠️ **state 保持默认 `Edit(0)`**（= 原版上面那句的第二个实参 `0`）—— ⛔ 别照
                //     练习窗那扇抄成 `View(2)`：那是**另一个按钮**（`cardInDeckInfoButton`，原版传 2）。
                //   `IsPlayerDeck = false` = 原版 `InventoryManager.HasItem(预组)` 为假
                //   ⇒ `Practice Deck` / `Share` / `Delete` / `Duplicate` 按判据藏（预组没有库下标，点了也无从下手）。
                //   🔴 **2026-10-21（真缺陷 · 本笔）**：`Edit Deck` 这颗在这一档**也藏掉**了 ——
                //     原版 `state < 2` 会让它露着、点了把 `context.Deck` 交给编辑器；我们接不上
                //     （编辑器入口只吃库下标、预组没有下标）⇒ 判据/理由/回退条件全集中在一处：
                //     `DeckInfoPopup.EditDeckShown()`。⛔ 别在这里再写一遍、也别改 `DeckIndex` 那条通用逻辑。
                w = DeckInfoPopup.Create(Manager, -1);
                w.OverrideDeck = PrebuiltDecks.ToPlayerDeck(pre);
                w.OverrideInfo = DeckSelectionPopup.InfoOf(pre);
                w.IsPlayerDeck = false;
            }
            else
            {
                w = DeckInfoPopup.Create(Manager, DeckIndex);
            }
            Manager.OpenWindow(w);
            LastDeckInfo = w;
            Debug.Log("[Event] 开 `Deck info Popup`");
            return w;
        }
        public static DeckInfoPopup LastDeckInfo;

        // 🔴 **2026-10-17（`N5`）已删**：原来这里有个 `protected void NotBuilt(string what)`（只打一行
        //    `LogWarning`）。全仓 `grep "NotBuilt("` 现读 **0 个调用点**（唯一那个 `BuildHelp` 已改成真开窗，
        //    见本文件 `本文件` 那段）⇒ 死代码。⛔ 别为了「以后可能用」把它加回来 —— 新的「没实现」走
        //    `RuleEngine` 那套 `ErrUnimplemented` 或就地 `Debug.LogWarning`，不留一个没人调的空壳。

        /// <summary>「卡组那一栏重建完了」的**注入点**（2026-10-26 · `X4`；追加在类尾，⛔ 别插到成员中间）。
        /// <para>🔴 **为什么需要它**：原版换卡组走 `SkirmishEventWindow.OnDeckSelected`
        /// （`d:/2/tools/decomp_full/SkirmishEventWindow__OnDeckSelected.c`）—— 那里除了重画那一栏，
        /// **同一拍**还会 `battleButton.set_interactable(valid)` 并把 `bannedCardInDeckText.text`
        /// 写成 `DeckUtility.ToShortLocalizedString(err)`（= 遭遇战窗那颗「卡组不可用」提示）。
        /// 我们这边「重画那一栏」**只有 `RefreshDeckColumn` 一个出口**（4 个调用点：<see cref="PickArmy"/> /
        /// <see cref="StepDeck"/> / <see cref="OpenDeckSelection"/> 的预组支与自建支），而它是 `private`
        /// ⇒ 子类挂不上去。加这个**空实现**的虚钩子，由 `RefreshDeckColumn` 收尾调用。</para>
        /// <para>⛔ **默认必须什么都不做**：本族另一支 `RankedEventWindow` 根上**没有**「卡组不可用」这一行
        /// （反向断言 → `Editor/MainMenuScene.cs` 的「排位窗没有 `Banned card in deck`」）⇒
        /// 基类在这里干任何事，都是**平白给排位窗加行为**。</para>
        /// <para>⛔ 也别把 `RefreshDeckColumn` 改成 `protected virtual` —— 那会动到所有子类的可见性契约。</para></summary>
        protected virtual void OnDeckColumnChanged() { }

        // ============================================================ 🆕 2026-10-27（B88 · E11，追加在类尾）
        //   **原版 `LiveOpsEventWindow<T>.OnRefresh()` 的等价实现**
        //
        // 判据（逐句 → `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/LiveOpsEventWindow.cs` 的 `OnRefresh`）：
        //   `var handler = <本窗的 ILiveOpsHandler>;  var ev = handler.GetEvent(<事件 id>) as T;`
        //   `if (<ILiveOpsHandler 接口槽 4 的 bool>) Close(); else ToFocus();` —— 已坐实（判据见下）。
        //   ⚠️ 它是在 `InitializeWindow(ILiveOps)` 里挂到「事件刷新」回调上的（同一个方法末尾 `new Action(this, …)`）。
        //   ✅ 2026-10-28 W7 找到真判据件（原判词「泛型无产物」**已作废**）：`decomp_generic_trial/out/LiveOpsEventWindow_object___OnRefresh__1016a30.c`
        //      ＋ `cpp2il/out_cpp2il_dev_ilspy/LiveOpsEventWindow.cs:71-200`。两侧一致：闭窗/聚焦这一支**只在 `GetEvent` 成功之后**才走；
        //      **取不到在原版是 throw**（NRE / cast 失败）⇒「取不到 ⇒ Close() + 出声」是我们**有意的差异**（⛔ 别当成原版行为）。
        //
        // 🔶 本地等价（原版两半都不可离线复刻，理由逐条写在这里）：
        //   ① **「重取事件」的替代** = 查离线目录 `LiveOpsLocalCatalog.Find(EventId)`：查不到，或
        //      「不可选 / 不在期」⇒ 视同原版的「取不到」⇒ `Close()`。判据来源 = `LiveOpsLocalEvent.Selectable`
        //      （`enabled && uploadToLive`）+ `IsInPeriod`，两者都在 `Shell/LiveOpsLocalCatalog.cs` 里。
        //   ② **「刷进度」的替代** = 重建整棵树（`Build()`）。我们的窗没有「进度对象引用」这一层，
        //      重建是唯一能把面板刷到当前数据的路（`Build()` 本来就会先清空自己名下所有子件）。
        //   ③ **触发点** = `LiveOpsScheduler.Schedule(EventEndUtc, OnRefresh)`（原版由服务器推刷新；
        //      本地唯一能定的时刻就是**事件截止**）—— `Open()` 末尾挂、`Close()` 撤（与 `_search` 同口径）。
        //
        // ⚠️ **今天这条链是休眠的**：13 份活动 SO **全部过期** ⇒ 没有任何窗拿到事件实例 ⇒
        //    `EventId` 一直是 null ⇒ 既不排期、也不重建（`ScheduleExpiryRefresh` 直接返回）。
        //    ⛔ 这不是「写死 false」：宿主/子类一旦把 `EventId` 填上（或将来 SO 在期），它立刻按上面跑。

        /// <summary>本窗绑定的事件 id（原版 `LiveOpsEventWindow&lt;T&gt;.LiveOp.GetID()` 的等价物）。
        /// ⚠️ **由打开它的那一侧填**（`ParentEventWindow` 的页签 / 活动弹窗那一族）；
        /// 没填 = 本窗不是由事件实例开的（今天就全是这种）⇒ 到期刷新不排期。</summary>
        public string EventId;

        LiveOpsScheduledRefresh _expiryRefresh;

        /// <summary>原版 `protected virtual void OnRefresh()`。
        /// <list type="bullet">
        /// <item>取不到（`Find` 空 / 不可选 / 不在期）⇒ `Close()`（**并出声**，⛔ 不静默关窗）。</item>
        /// <item>取到 ⇒ 重建界面（= 原版「把进度刷进界面」的等价物），不在顶层则 `ToFocus()`。</item>
        /// </list></summary>
        public virtual void OnRefresh()
        {
            if (string.IsNullOrEmpty(EventId))
            {
                // 没有事件实例 ⇒ 原版这一刻**根本不会收到**这个回调（`InitializeWindow` 没挂上）。
                // ⇒ 我们也不做任何事（不关窗、不重建），行为与「回调没发生」一致。
                return;
            }
            var now = LiveOpsLocalCatalog.NowUtc;
            var ev = LiveOpsLocalCatalog.Find(EventId);
            bool usable = ev != null && ev.Selectable && ev.IsInPeriod(now);
            if (!usable)
            {
                Debug.Log("[Event] `OnRefresh`：事件 `" + EventId + "` 取不到"
                        + (ev == null ? "（离线目录里没有这个 id）" : "（不可选或已不在期）")
                        + " ⇒ 照原版关窗。");
                Close();
                return;
            }
            Build();                                   // ② 刷进度 = 重建（见上）
            var mgr = WindowsManager.Instance;
            if (mgr != null && mgr.TopWindow != this) ToFocus();   // 原版 `base.ToFocus()`
        }

        /// <summary>排一次「事件截止 ⇒ `OnRefresh`」（原版由服务器推刷新，本地唯一能定的时刻就是截止）。
        /// 没有 <see cref="EventId"/> 或目录里查不到 ⇒ 不排（返回）。</summary>
        void ScheduleExpiryRefresh()
        {
            ReleaseExpiryRefresh();
            if (string.IsNullOrEmpty(EventId)) return;
            var ev = LiveOpsLocalCatalog.Find(EventId);
            if (ev == null || !ev.Selectable) return;
            _expiryRefresh = LiveOpsScheduler.Schedule(ev.EndUtc, OnRefresh);
        }

        /// <summary>撤掉那一班（关窗时调；`LiveOpsScheduler.Release` 幂等）。</summary>
        void ReleaseExpiryRefresh()
        {
            if (_expiryRefresh == null) return;
            LiveOpsScheduler.Release(_expiryRefresh);
            _expiryRefresh = null;
        }

        // ============================================================ 🆕 2026-10-28（B90 · N3，追加在类尾）
        //   原版 `RankedDeckSelector` 的**空态那颗钮的两条来路 + 每活动卡组槽**。判据（第一权威 = 反汇编方法体）：
        //   · `RankedDeckSelector__OnCreateDeckButtonClick.c`：`OpenWindow(收藏窗, {event}, TypeReference(CollectionWindow))`
        //     ⇒ **开收藏窗**（⛔ 不是「直接建一副再进编辑器」—— 那条近路是本类的旧实现，钮已不再走它）
        //   · `RankedDeckSelector__OnGoToStoreButtonClick.c`：`OpenWindow(商店窗, {currentArmy(+0xa0)}, TypeReference(ShopWindow))`
        //     ⇒ 开商店（🔴 **原版喂的是 booster 页**；我们 `ShopWindow.Create(mgr)` 不收页参数 ⇒ 差一档，见差异列）
        //   · `RankedDeckSelector__ChangeDeck.c`：空态那颗钮的委托按 `count < 1` 二分（两处
        //     `UIGenericEventCatcher_SourceDelegate___ctor` 的 method-info 分别指向这两个实例方法）。
        //   ⛔ 这两个口**不新建视觉**：钮还是 `BuildDeckTexts` 里那一颗（`CreateDeckHit`），只换它的委托。

        /// <summary>空态那颗钮（`No Deck Text > Generic Simplified UI Button`）的点击 —— 原版 `ChangeDeck` 的二分支：
        /// `ArmyDeckCount() &lt; 1`（该阵营一套都没有）⇒ 建卡组（开收藏窗）；否则（有但都不合本模式 / 不合法）⇒ 去商店。</summary>
        public void OnNoDeckButtonClick()
        {
            if (ArmyDeckCount() < 1) OnCreateDeckButtonClick();
            else OnGoToStoreButtonClick();
        }

        /// <summary>原版 `RankedDeckSelector.OnCreateDeckButtonClick()` —— **开收藏窗**。
        /// 同族先例 = 本类 `OnGoToStoreButtonClick` 的 `OpenByRef`（`Shell/RankedDeckSelector.cs` 重复实现已于 2026-10-28 删除）。
        /// ⚠️ 旧的近路 `CreateDeckInMode()` **留着**（`Editor/MainMenuScene.cs:9527` 的断言直调它）——
        ///   **钮已经不再走它**：原版是「先开收藏窗，用户自己在里面建」，不是「窗口替你建好」。</summary>
        public void OnCreateDeckButtonClick()
        {
            if (Manager == null) { Debug.LogWarning("[Event] 没有 `WindowsManager` ⇒ 开不了收藏窗（原版 `OnCreateDeckButtonClick`）"); return; }
            WindowsManager.OpenByRef("CollectionWindow", m => CollectionWindow.Create(m));
            Debug.Log("[Event] 空态钮 ⇒ 建卡组：**开收藏窗**（原版 `RankedDeckSelector.OnCreateDeckButtonClick`）");
        }

        /// <summary>原版 `RankedDeckSelector.OnGoToStoreButtonClick()` —— 开商店的 **booster 页**。🆕 **2026-10-28（`B91_W5`）已对齐**：原版 = `OpenWindow(shopWindow, OpenData{army = currentArmy(+0xa0)}, …)` ＋ `GameWindowWithTabsOptions.startTab = TypeReference(BoosterShopTab)`（判据 → `判据/E7_…商店208条判据.md` §三）。
        /// 我们这一跳 = `ShopWindow.Create(m, OpenData.ForFaction(SelectedArmyFaction))` ⇒ 落 **第 5 页 Crates/Boosters**，
        /// 并把该阵营喂给 `BoosterShopTab.TryOpenContainerArmy` 的等价物（`Shell/ShopTabPage.TryOpenContainerArmy`）。
        /// ⚠️ 我们的 `OpenData` 只带 `army`（本地没有 options 资产）⇒「落哪一页」由 `army != Neutral` 推，如实标成我们的换算。</summary>
        public void OnGoToStoreButtonClick()
        {
            if (Manager == null) { Debug.LogWarning("[Event] 没有 `WindowsManager` ⇒ 开不了商店（原版 `OnGoToStoreButtonClick`）"); return; }
            WindowsManager.OpenByRef("ShopWindow", m => ShopWindow.Create(m, ShopWindow.OpenData.ForFaction(SelectedArmyFaction)));
            // 🆕 2026-10-28（B91_W5）：这两行原来是「开的是默认页 / Create 不收页参数」的 LogWarning —— **现在是假话**
            //   （`Create` 已收 `OpenData`，这一跳直接落 booster 页并把阵营喂进去）⇒ 改成说明；⛔ 不删行（本文件账按行号钉着）。
        }

        // ------------------------------------------------------------ 每活动卡组槽（原版 `IPlayEvent.GetDeck/SetDeck`）
        /// <summary>「活动 id → 该活动上次选的那副卡组」。原版这一格住在**活动存档**里，存的是**卡组身份串**
        /// （`PlayEventSaveGame.defaultDeckId`，`d:/2/Warpforge_code/Scripts/Assembly-CSharp/PlayEventSaveGame.cs` 现读；
        /// 接口 = `IPlayEvent.GetDeck()` / `SetDeck(deck)`，判据 = `RankedDeckSelector__Initialize.c` 的接口槽 **9**、
        /// `RankedDeckSelector__ChangeDeck.c` 里 `FUN_1800121a0(8, …)` 那一跳）。
        /// <para>🔴 **2026-10-31（B104_P4）订正（铁律 5）**：本段原文写「只存内存、跨进程不持久」——
        /// **已过期**（B92_N9 起写 `_ours_EventDeckSlot` 落盘）；同时把**身份从「卡组名」换成「卡组 id」**
        /// （原版存的就是 id；按名在两个同名卡组之间会选错）。旧档只有名字 ⇒ 读档时保留名字作**回落**。</para>
        /// <para>⚠️ 本仓的 `PlayerDeck` 原本没有 id 字段 ⇒ 2026-10-31 用 `partial` 补了 `PlayerDeck.DeckId`
        /// （`RuleEngine/Data/DeckLibrary.cs` 文件尾；旧档第一次用到时由 `EnsureDeckId` 补发并落盘）。</para></summary>
        struct EventDeckSlotRec { public string DeckId; public string DeckName; }
        static readonly Dictionary<string, EventDeckSlotRec> _eventDeckSlot = new Dictionary<string, EventDeckSlotRec>();

        /// <summary>`Open()` 用哪一套当「当前」：先查**本活动**记的那副（原版 `currentEvent.GetDeck()`）；
        /// 没记过 / 没有 `EventId` / 那副已经不在库里 ⇒ 回落全局当前（= 改前的行为，⛔ 不是新语义）。
        /// <para>查法：**先按 id**（原版 `defaultDeckId`）；只有「旧档那条记录里没有 id」时才按名回落（并出声）。</para></summary>
        int EventDeckIndex()
        {
            if (!string.IsNullOrEmpty(EventId))
            {
                EventDeckSlotRec rec;
                if (_eventDeckSlot.TryGetValue(EventId, out rec))
                {
                    int i = CollectionData.IndexOfDeckId(rec.DeckId);
                    if (i >= 0) return i;
                    if (string.IsNullOrEmpty(rec.DeckId) && !string.IsNullOrEmpty(rec.DeckName))
                    {
                        // 🔴 旧档（B92_N9 起的记录只存名字）⇒ 按名回落。⚠️ 同名卡组之间这一步有歧义（取第一套），
                        //    所以**出声**；下一次 `RememberEventDeck` 会把它升级成 id。
                        i = CollectionData.IndexOf(rec.DeckName);
                        if (i >= 0)
                        {
                            Debug.LogWarning("[Event] 活动 `" + EventId + "` 的卡组槽是**旧档**（只有名字「"
                                           + rec.DeckName + "」、没有 id）⇒ 按名回落，**同名卡组之间有歧义**；"
                                           + "本窗一重建就会升级成 id。");
                            return i;
                        }
                    }
                    Debug.Log("[Event] 活动 `" + EventId + "` 记的那副（id = `" + (rec.DeckId ?? "")
                              + "` · 名 =「" + (rec.DeckName ?? "") + "」）已经不在库里 ⇒ 回落全局当前");
                }
            }
            return CollectionData.CurrentIndex();
        }

        /// <summary>把「现在画着的这一副」记回本活动（= 原版 `currentEvent.SetDeck(deck)`）。
        /// 由 `RefreshDeckColumn` 收尾调（改卡组的 4 条路都经过它）。
        /// 预组档不记（原版 `SetDeck` 收的是 `CardDeck`，预组走我们另一条通道 `PickedPrebuilt`）；
        /// 空态（没有卡组对象）也不记 —— ⛔ 别把「空」写成一条记录（那会让下次开窗误判成「记过」）。
        /// 🔴 存的**主动是 id**（原版 `PlayEventSaveGame.defaultDeckId`）；名字跟着存一份只为诊断 / 旧档回落。</summary>
        void RememberEventDeck()
        {
            if (string.IsNullOrEmpty(EventId)) return;          // 没有事件实例 ⇒ 原版这一跳根本不存在
            if (PickedPrebuilt != null) return;
            var raw = CollectionData.Raw(DeckIndex);
            if (raw == null || string.IsNullOrEmpty(raw.Name)) return;
            string id = CollectionData.EnsureDeckId(DeckIndex);   // 旧档第一次用到时现发 + 落盘（失败由它自己出声）
            _eventDeckSlot[EventId] = new EventDeckSlotRec { DeckId = id, DeckName = raw.Name };
            SaveEventDeckSlots();                               // 2026-10-28（B92_N9）：写进 `_ours_` 存档（原版住在活动存档里）
        }

        /// <summary>自检口：读某活动记的那副**卡组名**（没记过 ⇒ null）。身份判据看 <see cref="EventDeckSlotIdFor"/>。</summary>
        public static string EventDeckSlotFor(string eventId)
        {
            EventDeckSlotRec rec;
            return !string.IsNullOrEmpty(eventId) && _eventDeckSlot.TryGetValue(eventId, out rec) ? rec.DeckName : null;
        }

        /// <summary>自检口：读某活动记的那副**卡组 id**（没记过 / 旧档没 id ⇒ null）。</summary>
        public static string EventDeckSlotIdFor(string eventId)
        {
            EventDeckSlotRec rec;
            return !string.IsNullOrEmpty(eventId) && _eventDeckSlot.TryGetValue(eventId, out rec) ? rec.DeckId : null;
        }

        /// <summary>自检口：清空每活动槽（⚠️ 只给断言用；⛔ 正式代码别调）。</summary>
        public static void ResetEventDeckSlotsForTest() { _eventDeckSlot.Clear(); }

        /// <summary>`HasEffectiveDeck` 用的那一句（原版 `ChangeDeck` 的 `deck != null`）：
        /// 「这一套自有卡组属于选中阵营 ∧ 本窗模式」——🔴 **不判合法性**（合法性只进 `FirstDeckInArmy` 的**排序**，
        /// 见类尾 `DeckIsLegal`），决定不了「当前这一套画不画」；
        /// 判据 = `RankedDeckSelector__ChangeDeck.c` 的 `cVar1 = CardDeck.op_Inequality(deck, null)`。</summary>
        bool DeckIsCurrentInArmy()
        {
            var raw = CollectionData.Raw(DeckIndex);
            return raw != null && DeckFitsMode(raw) && DeckRules.SameFaction(FactionOfDeck(DeckIndex), SelectedArmyFaction);
        }

        // ============================================================ 🆕 2026-10-28（B90 · F1，追加在类尾）
        //   **`ChangeArmy` 的合法性判定 —— 它只是 `OrderByDescending` 的【排序键】，⛔ 不是过滤器。**
        //
        //   原版那一句（逐句现读 = `d:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp/RankedDeckSelector.cs:195-207`）：
        //     `Where(GetItemInstances(true), 阵营 ∧ 模式)`            ← 只有这一道 Where
        //     → `OrderByDescending(<上式>, _ChangeArmy_b__29_1)`      ← 合法性在这里，**是排序键**
        //     → `FirstOrDefault`                                     ← 合法的不合法的都在表里，只是排后面
        //   谓词体 `:492-496` = `return DeckUtility.ValidateDeck(d, ref err, true);`；
        //   四钮显隐 `:247-253` = `next/prev SetActive(Count > 1)` · `view/select SetActive(Count > 0)`；
        //   「`i+1 / Count`」`对的是这张表的 `IndexOf` 与 `_size`（`:382-386`）⇒ **分母是表长**。
        //   ⇒ **不合法卡组必须留在表里**，只影响「先挑谁」（`FirstDeckInArmy`）。
        //
        //   ⚠️ **2026-10-28 更正（铁律 5）**：改前这个判定被写进 `InArmyDeckList` 当「第二道 Where」，
        //     于是「本阵营 ∧ 本模式」那张表被**过滤**掉了不合法卡组 ⇒ 表空 ⇒ 空态 / 四钮全关 / `0/0`
        //     （`MainMenuScene.Run` 一次红 38 条：遭遇战段 6 · 排位段 16 · `[E11]` 16）。
        //   ⚠️ **判据只认 `out_il2csharp`**：`decomp_full/RankedDeckSelector.__c___ChangeArmy_b__29_1.c`
        //     把谓词的返回值打成 `void`（返回值被吞）⇒ 只看那份 `.c` 确实会把它当成 `Where` 的谓词。
        //   ⛔ 别改成「合法才留」——那是把排序当过滤，与原版相反。
        bool DeckIsLegal(int i)
        {
            var raw = CollectionData.Raw(i);
            return raw != null && DeckRules.Validate(raw, CollectionData.Card, raw.IsSkirmish) == DeckError.None;
        }

        // ================================================================================================
        //  🆕 2026-10-28（B92_N9）· 每活动卡组槽的**跨进程持久化**（追加在类尾 —— 零行号漂移）
        //
        //  原版：这一格住在**活动存档**里 —— `RankedDeckSelector__Initialize.c` 读接口槽 **9**
        //        （`IPlayEvent.GetDeck()`）、`RankedDeckSelector__ChangeDeck.c` 的 `FUN_1800121a0(8, …)`
        //        （= `SetDeck(deck)`）⇒ 关掉游戏再进，**每个活动还记着自己那副**。
        //  我们（B90-N3）当时只做了内存字典 + 在账里写明「跨进程会丢」；本笔按 B91-L6 的判定补落盘：
        //        `SaveKeys.OursPrefix + "EventDeckSlot"` = `_ours_EventDeckSlot`
        //        （`SaveKeys.IsKnown` 对 `_ours_` 前缀直接放行 ⇒ **不用改键表**；原版没有这个键，
        //         它是 🔶 我们自定的 —— 原版那格在活动存档里，我们没有活动存档）。
        //  ⚠️ 形状：`JsonUtility` **不能序列化 Dictionary** ⇒ 落成「行数组」。
        //  ⚠️ 读不到 / 坏档 ⇒ 空表 + **出声**（`SaveCenter` 自己会说话）⇒ 退回「回落全局当前」的旧行为，⛔ 不静默造一条记录。
        // ================================================================================================
        /// <summary>我们自定的存档键：**活动 id → 该活动上次选的那副卡组**（🔴 2026-10-31 起**主动是卡组 id**，
        /// 与原版 `PlayEventSaveGame.defaultDeckId` 同形；`DeckName` 只是诊断 / 旧档回落）。</summary>
        public const string EventDeckSlotKey = SaveKeys.OursPrefix + "EventDeckSlot";

        /// <summary>落盘一行。（`JsonUtility` 要 public 字段 + `[System.Serializable]`）
        /// 🔴 **2026-10-31（B104_P4）新增 `DeckId`**：旧档没有这个键 ⇒ 反序列化成 `null` ⇒ 走「按名回落」那一支，不丢记录。</summary>
        [System.Serializable]
        public class EventDeckSlotRow { public string EventId; public string DeckId; public string DeckName; }

        /// <summary>`EventDeckSlotKey` 的 payload（一份 JSON 对象）。</summary>
        [System.Serializable]
        public class EventDeckSlotFile { public List<EventDeckSlotRow> Items = new List<EventDeckSlotRow>(); }

        static bool _eventDeckSlotLoaded;

        /// <summary>从 `_ours_EventDeckSlot` 读一次（`Open()` 那一拍调；**只读一次**）。</summary>
        static void LoadEventDeckSlots()
        {
            if (_eventDeckSlotLoaded) return;
            _eventDeckSlotLoaded = true;                 // 读失败也不重试（`SaveCenter` 已经出过声）
            var f = SaveCenter.Get<EventDeckSlotFile>(EventDeckSlotKey);
            if (f == null || f.Items == null) return;    // 第一次跑 / 档丢了 / 坏档 —— `SaveCenter` 各自出过声
            int n = 0;
            foreach (var row in f.Items)
            {
                // 旧档（B92_N9 起）只有 DeckName、没有 DeckId ⇒ **照样读进来**，解析时按名回落（见 `EventDeckIndex`）。
                if (row == null || string.IsNullOrEmpty(row.EventId)) continue;
                if (string.IsNullOrEmpty(row.DeckId) && string.IsNullOrEmpty(row.DeckName)) continue;
                _eventDeckSlot[row.EventId] = new EventDeckSlotRec { DeckId = row.DeckId, DeckName = row.DeckName };
                n++;
            }
            Debug.Log("[Event] 读到每活动卡组槽 " + n + " 条（`" + EventDeckSlotKey + "`）");
        }

        /// <summary>写回 `_ours_EventDeckSlot`（`RememberEventDeck` 每改一次就写一次 —— 一格一份 JSON，代价可忽略）。</summary>
        static void SaveEventDeckSlots()
        {
            var f = new EventDeckSlotFile();
            foreach (var kv in _eventDeckSlot)
                if (!string.IsNullOrEmpty(kv.Key)
                    && (!string.IsNullOrEmpty(kv.Value.DeckId) || !string.IsNullOrEmpty(kv.Value.DeckName)))
                    f.Items.Add(new EventDeckSlotRow
                    {
                        EventId = kv.Key,
                        DeckId = kv.Value.DeckId,
                        DeckName = kv.Value.DeckName,
                    });
            if (!SaveCenter.Set(EventDeckSlotKey, f))
                Debug.LogWarning("[Event] 每活动卡组槽**没落盘**（`" + EventDeckSlotKey + "`）⇒ 这一局里记的，重开游戏会丢");
        }

        /// <summary>自检口：把「已读」标记清掉（配合 `SaveCenter.OverrideRoot` 验跨进程那一段）。</summary>
        public static void ResetEventDeckSlotLoadForTest() { _eventDeckSlotLoaded = false; }

        // ================================================================================================
        //  🆕 2026-10-28（B92_N9）· `RankedDeckSelector.OnSelectDeckButtonClick` 的第 5 个实参
        //
        //  原版把整个 `DeckSelectionContext` 传给 `DeckSelectionPopup.TryOpen`，其中 `context.Deck`
        //  = **当前正在用的那一副**；`DeckSelectionPopup.__TryOpen_b__10_0` 的模式闸读它
        //  （`.c` 全文 → `DeckSelectionPopup.ModePasses` 那段），**有它就只看它的模式、不看窗口模式**。
        //  我们（B90-N3）当时只传了窗口模式 ⇒ 账 `战役.tsv:117` 的残差「没传 currentDeck」就是这一格。
        //  ⚠️ 预组那副也算「当前那一副」（原版两边都是 `CardDeck`）⇒ 用它自己的 `gameMode`。
        // ================================================================================================
        /// <summary>当前那一副卡组的模式（原版 `context.Deck.GameMode`）；取不到 ⇒ `null`（回落窗口模式）。</summary>
        int? CurrentDeckModeForSelection()
        {
            if (PickedPrebuilt != null) return PickedPrebuilt.gameMode;
            var raw = CollectionData.Raw(DeckIndex);
            return raw != null ? (int?)raw.GameMode : null;
        }

        // ================================================================================================
        //  🆕 2026-10-28（第九十三会话 · `B93_D1` · 判据 `判据/B93_Q2_日常190.tsv` 批次 `B93-4`）
        //  「原版同名入口」+ 战斗钮 `interactable` 那一族。**全部追加在类尾**（⛔ 不在文件中段加行：
        //  账按 `文件:行号` 引用，中段加一行就让下游整体漂移，见 `CLAUDE.md` §18·b）。
        // ================================================================================================

        /// <summary>点 `Battle!` —— = 原版 `SkirmishEventWindow.BattleButtonClick` / `.StartBattle` /
        /// `RankedEventWindowV2.BattleButtonClick` / `.StartBattle` / `RankedEventWindow.PlayButtonClicked`
        /// （五个方法体**逐句同构**：先 `GameStaticData.shouldUseDebugMatchmakingData = false`、再 `StartMatch`）。
        /// <para>⚠️ 我们**没有** `shouldUseDebugMatchmakingData` 这个调试开关（原版只喂 debug matchmaking 那一支，
        /// 本工程没有对应实现 ⇒ 无落点）⇒ 这里只剩 `StartMatch` 那一跳；⛔ 别为此造一个恒 false 的假静态。</para></summary>
        public virtual void PlayButtonClicked() { StartMatch(); }

        /// <summary>`Close Button` / `BackgroundCloseButton` / 窗头返回键 —— 原版两扇活动窗都有同名私有方法，
        /// 体 = `Close()`（`SkirmishEventWindow__CloseButtonClick.c` / `RankedEventWindowV2__CloseButtonClick.c`）。</summary>
        public virtual void CloseButtonClick() { Close(); }

        /// <summary>`Help Button` —— 原版 `HelpButtonClick`：`HasComponent(liveOps, HelpPopup)` ⇒
        /// `OpenWindow(GetComponentReference(HelpPopup), liveOps)`（两扇窗**逐行同构**）。
        /// 等价物 = <see cref="OpenHelp"/>（变体按 `HelpVariant`、文案键按 `EventHelpTitleTerm`/`…DescriptionTerm`）。</summary>
        public virtual void HelpButtonClick() { OpenHelp(); }

        /// <summary>`Change Deck Button` —— 原版 `RankedEventWindow.ChangeDeckButtonClick`
        /// （`DeckSelectionContext(currentDeck, DeckChanged, null, filter, gameMode)` ⇒ `OpenWindow&lt;DeckSelectionPopup&gt;`）
        /// 与 `RankedDeckSelector.OnSelectDeckButtonClick`（同一个弹窗）。
        /// 等价物 = <see cref="OpenDeckSelection"/>：挑完那一步会重建卡组栏并调 `OnDeckColumnChanged`
        /// （= 原版 `DeckChanged` 里 `deckInfo.SetContent(deck)` 的那一半；另一半 `SetDivision()` 的输入全在服务器）。</summary>
        public virtual void ChangeDeckButtonClick() { OpenDeckSelection(); }

        /// <summary>`View Deck Button` —— 原版 `RankedEventWindow.ViewDeckButtonClick`
        /// （`DeckInfoContext(deck, DeckInfoStates.Edit, null, checkOwnership, editButtonAction)` ⇒ `OpenWindow&lt;DeckInfoPopup&gt;`）
        /// 与 `RankedDeckSelector.OnViewDeckButtonClick`。等价物 = <see cref="OpenDeckInfo"/>。</summary>
        public virtual void ViewDeckButtonClick() { OpenDeckInfo(); }

        /// <summary>战斗钮那颗 `WindowButton`（懒取，`BuildToBattle` 建完就有）。
        /// 走**路径**而不是在 `BuildToBattle` 里存一份 —— 中段加行会让账的行号整体漂移（见上面那段标题）。</summary>
        WindowButton _battleWb;

        /// <summary>`To Battle Button/BattleHit` 那颗 `WindowButton`；没有 ⇒ `null`（没建 / 被视口裁掉）。</summary>
        public WindowButton BattleButtonWb
        {
            get
            {
                if (_battleWb == null)
                {
                    var h = transform.Find("To Battle Button/BattleHit");
                    if (h != null) _battleWb = h.GetComponent<WindowButton>();
                }
                return _battleWb;
            }
        }

        /// <summary>现在这颗 `Battle!` 可不可点（自检读口）。没有按钮 ⇒ `true`（不该发生，出声在 setter 里）。</summary>
        public bool BattleInteractable { get { var wb = BattleButtonWb; return wb == null || wb.Interactable; } }

        /// <summary>原版 `battleButton.interactable = …` 那一句的等价物。
        /// <para>判据（两处**逐句同构**）：
        /// ① `SkirmishEventWindow__OnDeckSelected.c` —— `battleButton.interactable = HasValidDeckWithValidationMessage().valid`；
        /// ② `RankedEventWindowV2__ArmySelectedOnSelectArmy.c` —— `= currentDeck != null &amp;&amp; ValidateDeck(...)`。
        /// 本批只接了 ① （`SkirmishEventWindow.RefreshBannedLine`）；② 未接，已记账。</para>
        /// <para>⚠️ 置假 ⇒ `WindowButton.Click()` 第一句直接返回（= 原版 `Selectable.OnPointerClick` 的
        /// `if (!IsActive() || !IsInteractable()) return;`）+ 底图变灰；见 `WindowButton.Interactable` 的 doc。</para></summary>
        public void SetBattleInteractable(bool on)
        {
            var wb = BattleButtonWb;
            if (wb == null)
            {
                Debug.LogWarning("[Event] `Battle!` 没有 `WindowButton` ⇒ `interactable` **没写进去**（⛔ 不静默）");
                return;
            }
            if (wb.Interactable == on) return;
            wb.Interactable = on;
            Debug.Log("[Event] `Battle!` interactable = " + on
                      + "（原版 `battleButton.interactable = <卡组存在 && 校卡通过>`）");
        }

        /// <summary>当前这一副卡组的校验 = 原版 `HasValidDeckWithValidationMessage()` /
        /// `DeckUtility.ValidateDeck(currentDeck, out error)` 的等价物。**全工程只此一份**
        /// （`SkirmishEventWindow.RefreshBannedLine` 与 `SetBattleInteractable` 的调用点都从这里取判据）。
        /// <para>返回 `false` = 「没有卡组」**或**「有卡组但校不过」—— 调用方要看 `deck`/`err` 区分
        /// （原版两处都是 `deck != null &amp;&amp; Validate(...)`：空态也置假）。</para></summary>
        protected bool TryValidateCurrentDeck(out RuleEngine.DeckError err, out RuleEngine.PlayerDeck deck)
        {
            deck = PickedPrebuilt != null ? PrebuiltDecks.ToPlayerDeck(PickedPrebuilt)
                 : HasEffectiveDeck ? CollectionData.Raw(DeckIndex) : null;
            err = RuleEngine.DeckError.None;
            if (deck == null) return false;
            // 张数规则从**这副牌自己的模式**读（原版 `ValidateDeck` 不带 isSkirmish 实参，读 `CardDeck.gameMode`）
            bool isSkirmish = deck.GameMode == (int)RuleEngine.GameMode.Skirmish;
            err = RuleEngine.DeckRules.Validate(deck, CollectionData.Card, isSkirmish);
            return err == RuleEngine.DeckError.None;
        }
    }
}
