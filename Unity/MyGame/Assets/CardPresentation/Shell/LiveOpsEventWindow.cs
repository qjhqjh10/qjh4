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
    public abstract class LiveOpsEventWindow : GameWindow
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
        /// <summary>`Numer Of Army Decks` 的分母（原版占位串 `1/20` —— 单机不限制，照印）。</summary>
        public const int MaxDecksPerArmy = 20;

        // ============================================================ 几何：共用件
        public const float DeckSelL = 525.30f, DeckSelT = -0.10f, DeckSelR = 1394.70f, DeckSelB = 1080.10f;
        /// <summary>`Faction Icon Image` **522.63²**（原版 `sprite=0`、运行时喂）。</summary>
        public const float FacIconL = 711.01f, FacIconT = 133.50f, FacIconR = 1233.64f, FacIconB = 656.14f;
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
        public int ArmyIndex = -1;              // -1 = 不限阵营
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
                // 🔴 **2026-10-18（波 1b）**：这两句走词条（键 `MenuDeck/GameMode/{Skirmish,Classic}`，
                //   键名照原版；ZH 列与改前写死串**逐字相同** ⇒ 中文档零变化）。
                //   ⚠️ 别与 `StartMatch` 里那个 `modeStr`（`"Skirmish"`/`"Classic"`）混为一谈 —— 那两个是
                //   **网络协议串**（喂 `NetMatchmaking.TryStart`、两边对账用），⛔ 绝不能翻（`A1036`）。
                return DeckGameMode == (int)GameMode.Skirmish
                     ? Loc.T("MenuDeck/GameMode/Skirmish") : Loc.T("MenuDeck/GameMode/Classic");
            }
        }

        /// <summary>这副自有卡组**合不合本窗的模式**。
        /// 原版：卡组带 `GameMode` 字段，选卡组那一层按它筛（`DeckSelectionPopup.__TryOpen_b__10_0.c:10-17`）。</summary>
        bool DeckFitsMode(RuleEngine.PlayerDeck d) { return d != null && d.GameMode == DeckGameMode; }

        /// <summary>本窗模式下**能用的那几套自有卡组的第一套** —— 从 <paramref name="prefer"/> 起绕一圈找。
        /// 一套都没有 ⇒ 保持原样（UI 那边会显示「这套卡组还没有督军 / Create deck」那一栏）。
        ///
        /// 🔴 为什么开窗要**吸附**一下：模式窗里**默认选中的那套**如果模式不对，
        /// 玩家一进来点 `Battle!` 就会被挡（下面 `StartMatch` 那条），体验像坏掉了。</summary>
        int FirstDeckInMode(int prefer)
        {
            int n = CollectionData.DeckCount();
            if (n <= 0) return 0;
            for (int k = 0; k < n; k++)
            {
                int i = ((prefer < 0 ? 0 : prefer) + k) % n;
                if (DeckFitsMode(CollectionData.Raw(i))) return i;
            }
            return (prefer >= 0 && prefer < n) ? prefer : 0;
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
            LastOpened = this;
            // 🆕 2026-09-26：**吸附到本窗模式下能用的第一套** —— 默认选中那套模式不对的话，
            //    玩家一进来点 `Battle!` 会被下面的校验挡住（原版是「列表里就只剩同模式的」）。
            DeckIndex = FirstDeckInMode(CollectionData.CurrentIndex());
            ArmyIndex = -1;
            Build();
            // ⚠️ **每次开窗都要重建它** —— `Build()` 会把根下的子件全清掉（含上一次那个搜索弹窗）。
            _search = SearchingMatchPopup.Attach(transform, "Searching Oponent Popup (1)");
            _search.OnCancel = CancelSearch;
        }

        /// <summary>关窗时**顺手把匹配停掉** —— 原版那个弹窗 `ESCPressed()` 就是 `Close()` **+ `CancelSearch()`**
        /// （`SearchingMatchWindowDemo__ESCPressed.c`）；不这么做，ESC 关掉宿主窗之后搜索还挂着
        /// （宿主 GO 关掉 `Update` 会停，但状态没清、窗口再开还能看到旧进度）。</summary>
        public override void Close()
        {
            if (_search != null) _search.Cancel();
            base.Close();
        }

        // ============================================================ 建整棵树
        protected void Build()
        {
            // 🔴 **2026-10-18（A867 · S1）**：**首句**就把本窗名下的旧滚动区撤掉 —— 下一句就把 `root` 的子件
            //   全清掉，而下面 `BuildArmySelector` 会登记一颗**新**的 `MenuScroll`（`Owner = gameObject`）。
            //   ⛔ **兜不住**，所以非补不可（`PointerLayer` 的两道自动清理**都判不出这里已死**）：
            //     · `PruneScrolls` 的 `s == null` —— `MenuScroll` 是**普通 C# 类**（`Shell/MenuScroll.cs:79`）
            //       ⇒ 节点被销毁**不会**让它变 null ⇒ **恒假**；
            //     · `s.Owner == null` —— `Owner` 指的是**活下来的窗根**（本方法只删**子件**，`transform` 本身还在，
            //       而且我们关窗是 `SetActive(false)`、**不销毁**）⇒ 也**恒假**（`Shell/PointerLayer.cs:223-231`）。
            //   ⇒ 少了这一句 = **每重建一次净涨 1 条**，而且旧条目**还能被滚轮命中**（`OnChanged` 指向已销毁的节点）。
            //   ✅ 形状照抄 `Shell/InboxWindow.cs:405`。⚠️ 本窗这个 `Build()` 会被**同一实例**再走一遍
            //   （`Open()` 调的：`TryOpen` 从 `Closed` 支 ⇒ `Open()`，`Shell/WindowsManager.cs:513-519`；
            //    生产侧到达它的是 `ShowPreviousWindow` ⑥ 那句 `prev.TryOpen()`，`:1147`）。
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

        // ------------------------------------------------------------ 中间那一栏：卡组选择
        /// <summary>`Ranked Deck Selection`（**两扇窗逐格相同**）。
        /// 🔴 三层的先后**照原版的兄弟序**：`Faction Icon Image` → `Warlod Image`（里面再叠 `Warlord Darkening`）
        ///    → 文字/按钮。渲染队列按这个次序递增（同队列谁盖谁不确定 —— 2026-09-23 踩过）。</summary>
        void BuildDeckSelection(Transform root)
        {
            var col = MenuDraw.Node(root, "Ranked Deck Selection",
                                    new PxRect(DeckSelL, DeckSelT, DeckSelR, DeckSelB));

            var d = CollectionData.DeckAt(DeckIndex);

            // 阵营大徽记（原版 sprite=0 ⇒ 运行时喂 = 该阵营的 `40k_DeckSelection_icon_Faction*`）
            MenuDraw.Rect(col, Tex(DeckRuntime.FactionIcon(d.Faction)),
                          new PxRect(FacIconL, FacIconT, FacIconR, FacIconB), "Faction Icon Image", QDeck, null, true);

            // 督军立绘（原版同样是运行时喂；我们用督军卡自己的立绘 —— 同 `DeckInfoPopup` 那条路）
            var wl = CollectionData.Warlord(DeckIndex);
            var wlRoot = MenuDraw.Node(col, "Warlod Image", new PxRect(WlL, WlT, WlR, WlB));
            var wlTex = wl != null ? CardArt.Portrait(wl.Id) : null;
            if (wlTex != null)
                // ⚠️ **原版那件 `m_PreserveAspect = 0`（直接拉满 1098.14²）**，而喂给它的是一张
                //    **2048² 的方形立绘**；我们手上只有**卡面插图**（671×1024 竖构图）⇒ 拉满会把人拉变形，
                //    这里 **`keepAspect`（等比放进框、居中）是我们挑的**，不是复刻。原版那张方形立绘本地没有。
                MenuDraw.Rect(wlRoot, wlTex, new PxRect(WlL, WlT, WlR, WlB), "Warlord", QDeck1, null, true);
            else
                Debug.Log("[Event] 督军立绘取不到（" + (wl != null ? wl.Name : "这套卡组没有督军") + "）—— 那一层不画，出声");
            //    `Warlord Darkening`：压暗一层（`Smooth background square` Sliced · col(0,0,0,0.816)）
            MenuDraw.Nine(wlRoot, Tex(ArtWarlordDark),
                          new PxRect(WlDarkL, WlDarkT, WlDarkR, WlDarkB), SmokeBorder, SmokeTexW, SmokeTexH,
                          QDeck2, new Color(0f, 0f, 0f, 0.816f), true, "Warlord Darkening");

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
        /// 图标顺序照原版：`40k_UI_bt_back` · `40k_UI_bt_deck_change` · `40k_bt_eye` · `40k_UI_bt_back`(**水平翻转**)。</summary>
        void BuildDeckButtons(Transform col)
        {
            var bar = MenuDraw.Node(col, "Deck Buttons", new PxRect(DbL, DbT, DbR, DbB));
            float cx = (DbL + DbR) * 0.5f, cy = (DbT + DbB) * 0.5f;
            float total = DbBtnW * 4f;
            string[] icons = { ArtBackArrow, ArtDeckChange, ArtEye, ArtBackArrow };
            System.Action[] acts =
            {
                () => StepDeck(-1),
                () => OpenDeckSelection(),
                () => OpenDeckInfo(),
                () => StepDeck(+1),
            };
            string[] names = { "Previous Deck Button", "Change Deck Button", "View Deck Button", "Next Deck Button" };
            for (int i = 0; i < 4; i++)
            {
                float x1 = cx - total * 0.5f + DbBtnW * i, x2 = x1 + DbBtnW;
                float y1 = cy - DbBtnH * 0.5f, y2 = cy + DbBtnH * 0.5f;
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
                             QHit, acts[i], iconQ, icons[i]);
            }
        }

        void BuildDeckTexts(Transform col)
        {
            var d = CollectionData.DeckAt(DeckIndex);
            var wl = CollectionData.Warlord(DeckIndex);
            bool hasDeck = wl != null;
            // 🔴 **对齐照修好的 `menu_dump.py` 重导**（原来那份的 `hAlign` 映射错位一位：
            //    真枚举是位标志 `Left=1 · Center=2 · Right=4 · Flush=16` —— 见 `资料/已知的坑.md`）。
            //    这两行原版是 **Center** ⇒ 不调 `Align*`（`MenuDraw.Text` 默认就是居中）。
            _txtDeckName = MenuDraw.Text(col, new PxRect(DnL, DnT, DnR, DnB), d.Name, Color.white,
                                         "Deck Name", 62.85f, QText);
            _txtDeckWarlord = MenuDraw.Text(col, new PxRect(DwL, DwT, DwR, DwB),
                                            wl != null ? wl.Name : "", Color.white, "Deck Warlord", 44.65f, QText);

            // `No Deck Text`（+ `Create deck` 那颗钮）—— **只有在没有督军时才显示**（原版 `NoDeckText` 那一族）
            var none = MenuDraw.Node(col, "No Deck", new PxRect(NoDeckL, NoDeckT, NoDeckR, NoDeckB));
            // 🔴 **2026-10-18（波 1b）**：这一句走词条（键 `MenuDeck/HUD/NoWarlordText`；ZH 列与改前写死串**逐字相同** ⇒ 中文档零变化）。
            //   ⚠️ 它是「整句」—— 别拿 `MenuDeck/Error/NoWarlord`（短键「还没有选战将」）替，那会**丢信息**（`A1034`）。
            var ndt = MenuDraw.Text(none, new PxRect(NoDeckL, NoDeckT, NoDeckR, NoDeckB),
                                    Loc.T("MenuDeck/HUD/NoWarlordText"), Color.white,
                                    "No Deck Text", 45f, QText);
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
            //   ⚠️ 原版那句是 `Localize` 的西班牙语串（带 `{0} {1}` 占位符，运行时灌「有几个指挥官」）
            //      ⇒ **我们印自己的话**（本地无术语表、也没有那个计数）—— 这一条是**我们写的**，不是复刻。
            MenuDraw.Rect(none, Tex(ArtMulligan), new PxRect(CreateBtnL, CreateBtnT, CreateBtnR, CreateBtnB),
                          "Generic Simplified UI Button", QArt, null, true);
            // ⚠️ 文字挂在**那颗钮底下**（原版 `Button Text` 是钮的子件；直接挂容器会让 `FindChild(钮,"Button Text")` 找不到）
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
            //   ⚠️ 别把**父节点** `No Deck Text` 扯进来：那一颗才是西语占位串（`Tienes …{0} {1} Comandante(s)…`，
            //     见上面 `No Deck Text` 那一段），而且**它是我们写的**、不是复刻。
            //   ⛔ 节点名 `"Button Text"` 不动（`FindChild(钮,"Button Text")` 靠它）。
            MenuDraw.Text(cbtn, new PxRect(CreateTxL, CreateTxT, CreateTxR, CreateTxB),
                          Loc.T("MainMenu/Ranked/GoToCreateDeck"),
                          Color.white, "Button Text", 55f, QText);      // 原版 hAlign = Center ⇒ 不调 Align*
            // 🆕 A17：原版 `Ranked Deck Selection>No Deck Text>Generic Simplified UI Button` 是 SpriteSwap（普查 §块 4 第 16 行）
            MenuDraw.Hit(none, "CreateDeckHit", new PxRect(CreateBtnL, CreateBtnT, CreateBtnR, CreateBtnB), QHit,
                         CreateDeckInMode, cbtnBg, ArtMulligan);
            none.gameObject.SetActive(!hasDeck);
        }

        /// <summary>`Numer Of Army Decks`「n/20」+ `Deck Quantity Icon`（**这一栏是原版真数据**：该阵营有多少套）。</summary>
        void BuildArmyDeckCount(Transform col)
        {
            // 原版 `Deck Quantity Icon` 是 `Numer Of Army Decks` 的**子件**（实读 depth 3），别挂在同级
            var cnt = MenuDraw.Node(col, "Numer Of Army Decks", new PxRect(CntL, CntT, CntR, CntB));
            MenuDraw.Node(cnt, "Deck Quantity Icon", new PxRect(CntIcL, CntIcT, CntIcR, CntIcB));
            MenuDraw.Rect(cnt, Tex(ArtDeckCount), new PxRect(CntIcL, CntIcT, CntIcR, CntIcB),
                          "Icon", QArt, null, true);
            // 原版 hAlign = **Left** ⇒ 左对齐（不是居中）
            _txtDeckCount = MenuDraw.Text(cnt, new PxRect(CntL, CntT, CntR, CntB), DeckCountText(), Color.white,
                                          "Text", 60.5f, QText);
            MenuDraw.AlignLeft(_txtDeckCount, new PxRect(CntL, CntT, CntR, CntB));
        }
        Label _txtDeckCount;
        string DeckCountText() { return ArmyDeckCount() + "/" + MaxDecksPerArmy; }

        int ArmyDeckCount()
        {
            if (ArmyIndex < 0 || _facs == null || ArmyIndex >= _facs.Count) return CollectionData.DeckCount();
            int n = 0;
            for (int i = 0; i < CollectionData.DeckCount(); i++)
                if (CollectionData.DeckAt(i).Faction == _facs[ArmyIndex]) n++;
            return n;
        }

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
            MenuDraw.Text(holder, new PxRect(FacTitleL, FacTitleT, FacTitleR, FacTitleB), "Factions",
                          Color.white, "Factions Title", 45.87f, QText);

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
            //   `vc.SetBaseRect(r)` 的位置理由（`:314-317`）。⛔ 别改成 `OnEnable` 里自动抓（文件头 `:72-73` 明令）。
            vc.CaptureNow();
            _facs = CollectionWindow.CardsState.Factions();

            int cols = Mathf.Max(1, Mathf.FloorToInt((ArmViewR - ArmViewL) / ArmyCell));
            int rows = Mathf.CeilToInt(_facs.Count / (float)cols);
            float contentH = ArmyPadT + ArmyCell * rows;
            // ⚠️ 这里**不再留一个 `view` 局部量**当裁切框：裁切状态已经搬到上面 `BuildArmySelector` 建的那颗
            //    `Viewport` 节点上（`ViewportClip`）。下表里的 `view` 全部换成 `null` ⇒ `MenuDraw.Rect/Nine/Hit`
            //    沿父链解析到那一颗。🔴 **`MenuScroll` 那份 `Viewport` 是【另一条路】**（滚动区的矩形，
            //    不是裁切载体）—— 两者今天同值，但⛔ 别把滚动区当成裁切状态的来源。
            _armyScroll = MenuScroll.TopAligned(new PxRect(ArmViewL, ArmViewT, ArmViewR, ArmViewB), contentH);
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
        /// 的实参来自 `RankedScoreBoostSave.GetWins(army, …)`（赛季存档，在服务器）⇒ 取 **0**
        /// （= 序列化里 `Fill` 那个 0 宽，也是「没有进度」唯一有判据的那个值）。**这一条是我们挑的**，
        /// 不是从原版读出来的运行期值；真接上数据时改这一个数。
        /// ⚠️ 同理 `ProgressBar` / `Featured Icon` **两件的显隐**也是数据驱动的（`SetActive(…, showProgress)` /
        /// `SetActive(…, army.RankedV3ArmyState &amp; 4 /*Featured*/)`，两个源都在赛季数据里）—— 我们**没有**那份数据，
        /// ⇒ 照 **prefab 出厂状态**画（`m_IsActive` 两件都是 true）。**这一条也是我们挑的**，见下面那次出声。</summary>
        const float ArmyProgress = 0f;
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
                // ③ `ProgressBar`(Slider) > `Fill Area` > `Fill` + `Separator`
                var bar = MenuDraw.Node(cell, "ProgressBar", InCell(rr, CellBar));
                var barArea = MenuDraw.Node(bar, "Fill Area", InCell(rr, CellBar));
                var barRect = InCell(rr, CellBar);
                float fillW = Mathf.Clamp01(ArmyProgress) * (barRect.x2 - barRect.x1);
                var fill = MenuDraw.Node(barArea, "Fill", new PxRect(barRect.x1, barRect.y1, barRect.x1 + fillW, barRect.y2));
                MenuDraw.Nine(fill, Tex(ArtArmyProg),
                              new PxRect(barRect.x1, barRect.y1, barRect.x1 + fillW, barRect.y2),
                              new Vector4(20f, 0f, 20f, 0f), 48f, 18f, QArmyBar, null, true, "Image",
                              new Vector4(10f, 0f, 10f, 0f), null, VpSoft);   // `m_PixelsPerUnitMultiplier = 2` ⇒ 画出来 20÷2 = 10
                // `Separator`：**无图**、纯黑 (0,0,0,1)、4×20.852 —— 原版运行期还会按 `maxWins` 再实例化 n−1 份
                // （`RankedArmySelectorContainer__Initialize.c` 里那个 `param_4 - 1` 的循环）并**把模板那份的组件关掉**；
                // 我们没有那个数 ⇒ 照 prefab 只画出厂这一份。
                MenuDraw.Rect(barArea, CardArt.Solid(), InCell(rr, CellSep), "Separator", QArmyBar,
                              new Color(0f, 0f, 0f, 1f), false, null, VpSoft);
                // ④ `Army Icon`（149.96² · `preserveAspect`；图由运行期喂 —— 我们喂阵营徽记，见 `BuildArmySelector`）
                MenuDraw.Rect(cell, Tex(DeckRuntime.FactionIcon(facs[i])), InCell(rr, CellIcon),
                              "Army Icon", QArmyIcon, null, true, null, VpSoft);
                // ⑤ `Featured Icon`（122.81×123.53 · `preserveAspect`）—— 显隐数据驱动（见 `ArmyProgress` 那段）
                MenuDraw.Rect(cell, Tex(ArtArmyFeat), InCell(rr, CellFeat), "Featured Icon", QArmyFeat, null, true, null, VpSoft);

                // 四态（悬停 / 按下 / 常态**逐颗显式给**）：本颗的悬停图**不是** `<常态图>_hover` ——
                // `WindowButton` 的后备规则会推成 `UI_Army_Selection_Back_hover`（**小写 h**），
                // 而原版那张叫 `UI_Army_Selection_Back_Hover`（**大写 H**，同 `HoverNames` 里那两条同因）
                // ⇒ 不显式给就是「取不到 + 悬停不换图 + `MissingSwapArt` 记一条」。
                // 🔴 **2026-10-13（A435 阶段 2 · 丁 / A745）**：`clip` 传 `null` —— 命中区要的【裸框 + pad】
                //    也由那颗 `Viewport` 节点给（`MenuDraw.Hit` 内部 `Resolve`）。⚠️ 软边**故意不传**
                //    （`Hit` 也不收这个形参）：原版 `m_Softness` 只改渲染，射线那一面只看矩形。
                int idx = i;
                MenuDraw.Hit(cell, "Hit", rr, QHit, () => PickArmy(idx), bgQ,
                             sel ? ArtArmyOn : ArtArmyBack, ArtArmyHover, ArtArmyOn, null);
                ArmyCells.Add(cell);
            }
            if (!_armyDataNoteShown)
            {
                _armyDataNoteShown = true;
                Debug.Log("[Event] 阵营格：`ProgressBar` / `Featured Icon` 已按原版 item prefab"
                          + "（`Ranked Army Selector Container V2`）建出来，但**它们的数值与显隐在原版是赛季数据驱动的**"
                          + "（`RankedArmySelectorContainer.Initialize` 的 `wins/maxWins` · `showProgress` ·"
                          + " `RankedV3ArmyState.Featured`，全在服务器）—— **我们没有那份数据** ⇒ 进度取 0、两件照 prefab"
                          + " 出厂状态显示。**这一条是我们挑的**，不是原版的运行期值。");
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
        /// · 底板**比父容器宽**（`ContentSizeFitter` 按内容撑开 = 690.86，见下）；
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
            // 底板宽度 = **内容撑开的宽度**：原版 `Header Background` 挂 `ContentSizeFitterMinMax`
            //   （`widthMin 550 / widthMax 1250`），按内容 = padL 155 + 标题 369.36 + spacing 5.5 + 图标 100 + padR 61
            //   = **690.86**。原来只画到父容器右缘 550，少了 140（找茬子代理按字段算出来的）
            const float plateR = 690.86f;
            float titleL = HdrL + HdrPadL;
            var parts = WindowHeader.WithBackButton(root, new WindowHeader.Spec
            {
                RootName = WindowHeader.RootNameGameMode,
                RootRect = new PxRect(HdrL, HdrT, HdrR, HdrB),
                PlateRect = new PxRect(HdrL, HdrBg1T, plateR, HdrBg1B),
                TitleRect = new PxRect(titleL, HdrT + 16.36f, titleL + 369.36f, HdrT + 99.01f),
                TitleText = "Game mode",       // prefab 出厂 `m_text`；运行时被 `WindowHeaderWithBackButton.Initialize` 换掉
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

            // `Game Mode Icon`（HLG 里紧跟标题：155 + 369.36 + spacing 5.5 ⇒ 左沿 **529.86**，竖中在 115.36 的板里）
            // 2026-09-24 订正：原来这里写「那两张图本地没有」—— **是错的**：
            //   `Resources/Art/ui_menu/40k_gamemode_icon_skirmish.png` 与 `…_classic.png` **早在工程里**
            //   （练习窗那个 `Toggle` 用的就是它们）。遭遇战窗接 skirmish 那张；
            //   排位那张**查不到**（本地只有 classic / skirmish）⇒ 子类给 null 时不画并出声。
            // 🔴 A866 收口后它建在 `parts.Plate`（= `Header Background`）底下 —— 与原版父链、与收口前的父件**同**
            //   （原来是这个方法里的局部 `bgT`）；树序也同：`Window Title` 先建、图标后建。
            float iconL = titleL + 369.36f + HdrGap;
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

        // ------------------------------------------------------------ `Battle!`
        /// <summary>`To Battle Button`（`UI_Button_Mulligan` **preserveAspect**）+ 文案 + `ButtonIcons`（两颗 100² 图标、
        /// HLG spacing **−28.31** · align **MiddleRight** · 🔴 **`reverse=1`** ⇒ 树序倒排，`TrophyIcon` 在左）。</summary>
        void BuildToBattle(Transform root)
        {
            var btn = MenuDraw.Node(root, "To Battle Button", new PxRect(BattleL, BattleT, BattleR, BattleB));
            var btBg = MenuDraw.Rect(btn, Tex(ArtMulligan), new PxRect(BattleL, BattleT, BattleR, BattleB),
                          "Bg", QArt, null, true);
            // 原版 hAlign = **Center** ⇒ 不调 `Align*`（原来右对齐了）
            MenuDraw.Text(btn, new PxRect(BattleTxL, BattleTxT, BattleTxR, BattleTxB), "Battle!",
                          Color.white, "Button Text", 74.25f, QText);

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
            MenuDraw.Rect(icons, Tex(TrophyIconArt),
                          new PxRect(x1, cy - BIconSide * 0.5f, x2, cy + BIconSide * 0.5f), "TrophyIcon", QArt1, null, true);
            x1 = x2 + BIconsGap; x2 = x1 + BIconSide;            // 1357.66 → 1457.66（**贴组右沿**）
            MenuDraw.Rect(icons, Tex(ArtShield),
                          new PxRect(x1, cy - BIconSide * 0.5f, x2, cy + BIconSide * 0.5f), "ShieldIcon", QArt1, null, true);

            // 🆕 A17：原版 `To Battle Button` 是 SpriteSwap（普查 §块 4 第 14 行）
            MenuDraw.Hit(btn, "BattleHit", new PxRect(BattleL, BattleT, BattleR, BattleB), QHit, () => StartMatch(),
                         btBg, ArtMulligan);
        }

        /// <summary>`Help Button`（原版 `40K_generic_bt_info`，`m_LocalScale 0.7357` ⇒ 真渲出来 65.13²）。</summary>
        void BuildHelp(Transform root)
        {
            float cx = (HelpL + HelpR) * 0.5f, cy = (HelpT + HelpB) * 0.5f;
            float half = (HelpR - HelpL) * HelpScale * 0.5f;
            var r = new PxRect(cx - half, cy - half, cx + half, cy + half);
            MenuDraw.Rect(root, Tex(ArtHelp), r, "Help Button", QArt, null, true);
            MenuDraw.Hit(root, "HelpHit", r, QHit,
                         () => NotBuilt("`Help Button`（原版弹一段活动说明；本地没有那份文案）"));
        }

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

        /// <summary>推进匹配（`Update` 与自检都走它 —— 批处理没有帧循环）。</summary>
        public void TickSearch(float dt) { if (_search != null) _search.Tick(dt); }

        void Update() { TickSearch(Time.deltaTime); }

        /// <summary>取消匹配后宿主窗口要复位的东西（窗口本身**留着**，玩家还站在这里）。</summary>
        public virtual void CancelSearch()
        {
            Debug.Log("[Event] 取消匹配（原版 `SearchingOpponentWindow__CancelMatchMatchmaking → MatchMakerManager.CancelSearch`）");
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
        /// （只多一个 vtable 槽：`:920` 那个委托里的 `StartBotBattle()` 仍落到同一个方法体）。</para>
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
            UnityEngine.SceneManagement.SceneManager.LoadScene(scene);
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
        {
            int mode = DeckGameMode;
            string name = CollectionData.CreateDeck(mode);
            // 空串 = **没写进存档**（A503 的口径；原因已由 `CollectionData` 自己 `LogWarning` 过一次）
            // ⇒ 这一支里 `IndexOf("")` 恒 −1。两个失败（**没建出来** / **建出来了没选中**）要分开说，
            //   不能都塞进 `Select` 的返回值 —— 那会把「压根没建」报成「选中失败」（W-Small3 §六·1 点名的抄法）。
            bool created = !string.IsNullOrEmpty(name);
            int idx = created ? CollectionData.IndexOf(name) : -1;
            if (created)
            {
                // 新卡组即选中（`DeckLibrary.Create` 本来就把它置成 current）；返回值照 A600 的口径**要读**
                if (!CollectionData.Select(idx))
                    Debug.LogWarning("[Event] 新建的卡组「" + name + "」**没选中**：" + CollectionData.LastSelectError
                                     + "（⚠️ 进编辑器后打开的**不是**这一副）");
                CollectionData.PendingEditDeck = idx;
            }
            else
            {
                // 「新建失败」这一支**行为是良性的**（W-E4 §六·4 静态核过、W-Small3 §六·1 复核：
                // `DeckLibrary.Create` 已经把 `_current` 置成刚建那套 ⇒ 不吃交接就落 `Library.Current` = **同一副**）
                // ⇒ **不改玩家可见流程**（照旧进编辑器、不拦；同 A609 的裁定），但**要出声**（红线：不许静默失败）。
                // ⚠️ `PendingEditDeck` 照旧**显式清成 −1**：⛔ 别省这一步 —— 留着上一次的旧下标会跳进**别的**卡组。
                CollectionData.PendingEditDeck = -1;
                Debug.LogWarning("[Event] 新建卡组**没写进存档**（内存里那套还在、进编辑器后打开的**就是它**；"
                                 + "重启就没了）⇒ 照旧进编辑器，但**不交接下标**"
                                 + "（`DeckRuntime` 落 `Library.Current` = 刚建那套，与交接同一副）");
            }
            Debug.Log("[Event] 新建卡组" + (created ? "「" + name + "」" : "（⚠️ 名字拿不到、见上一条警告）") + "· 模式 "
                      + (mode == (int)GameMode.Skirmish ? "遭遇 Skirmish（12 张）" : "经典 Classic（30 张）")
                      + "（照原版 `SelectDecksTab.CreateDeck`：**建组这一刻定模式**，之后没有改的路径）"
                      + " ⇒ 进卡组编辑（交接下标 " + idx + "）");
            // 🆕 2026-10-18（A855）：**这条来路的回程意图**（全仓仅有两处 `LoadScene("DeckEditor")`，
            //   这是**第二处**）—— 「来源由入口决定、不由离场方式决定」⇒ 写在**进编辑器**这一刻，
            //   ⛔ 别挪到 `DeckRuntime.BackToMenu` 去；⚠️ 写在批处理闸**之前**（批处理不切场景，
            //   自检只能靠这个意图观测）。⚠️ 回程**今天开不了这扇窗** —— 本地没有事件数据
            //   （理由逐条 → `MainMenuRuntime.Build` 里那一段）⇒ 只记来源，那一跳还开着。
            CollectionData.SetReturnIntent(DeckExitSource.LiveOpsEvent, WindowTabType.None);
            if (Application.isBatchMode) { Debug.Log("[Event] （批处理：不切场景，只交接）"); return; }
            UnityEngine.SceneManagement.SceneManager.LoadScene("DeckEditor");
        }

        // ============================================================ 交互
        void PickArmy(int i)
        {
            ArmyIndex = (ArmyIndex == i) ? -1 : i;
            // 选了阵营 ⇒ 跳到该阵营的第一套（没选 ⇒ 保持）
            // 🆕 2026-09-26：**外加「模式要对」** —— 否则会跳进一套不能用的牌（下了阵营筛选反而更糟）
            if (ArmyIndex >= 0 && _facs != null && ArmyIndex < _facs.Count)
                for (int k = 0; k < CollectionData.DeckCount(); k++)
                    if (CollectionData.DeckAt(k).Faction == _facs[ArmyIndex] && DeckFitsMode(CollectionData.Raw(k)))
                    { DeckIndex = k; break; }
            if (_armyHolder != null) RebuildArmyCells(_armyHolder);
            RefreshDeckColumn();
            Debug.Log("[Event] 阵营筛选：" + (ArmyIndex < 0 ? "不限" : _facs[ArmyIndex]));
        }

        /// <summary>上/下一套（在这一屏可见的卡组里循环）。
        /// 🆕 2026-09-26：**跳过模式不对的**（阵营筛选与模式筛选是**两条**，都要满足）。</summary>
        void StepDeck(int dir)
        {
            int n = CollectionData.DeckCount();
            if (n <= 0) return;
            int cur = DeckIndex;
            for (int k = 0; k < n; k++)
            {
                cur = (cur + dir + n) % n;
                bool armyOk = ArmyIndex < 0 || _facs == null || ArmyIndex >= _facs.Count
                              || CollectionData.DeckAt(cur).Faction == _facs[ArmyIndex];
                if (armyOk && DeckFitsMode(CollectionData.Raw(cur))) break;
            }
            DeckIndex = cur;
            RefreshDeckColumn();
            Debug.Log("[Event] 换卡组：" + CollectionData.DeckAt(DeckIndex).Name);
        }

        /// <summary>把「卡组那一栏」重建一遍（换阵营 / 换卡组之后）。
        /// 🔴 **重建而不是原地改**：`CardView` 那条教训（`SetData` 只更新已存在的层）—— 这里同理，
        /// 「督军立绘 / 阵营徽记 / 有没有督军」三样都可能整块变。</summary>
        void RefreshDeckColumn()
        {
            var col = transform.Find("Ranked Deck Selection");
            if (col == null) return;
            RewardsWindow.DestroySafe(col.gameObject);
            BuildDeckSelection(transform);
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
                    Debug.Log("[Event] 选中**预组卡组**「" + pick.PrebuiltDeck.DisplayName + "」("
                              + pick.PrebuiltDeck.deckId + ") ⇒ 本局就用它（防御卡 = 我们补的「"
                              + pick.PrebuiltDeck.defensiveNameZh + "」" + pick.PrebuiltDeck.defensiveId + "）");
                    return;
                }
                int idx = CollectionData.IndexOf(pick.Info.Name);
                if (idx >= 0) { DeckIndex = idx; PickedPrebuilt = null; PrebuiltDecks.ClearPendingBattleDeck(); RefreshDeckColumn(); }
            }, DeckGameMode);        // 🆕 2026-09-26：**「我的卡组」页按本窗模式筛**（原版 `DeckSelectionPopup` 的 context 筛选）
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
            var w = DeckInfoPopup.Create(Manager, DeckIndex);
            Manager.OpenWindow(w);
            LastDeckInfo = w;
            Debug.Log("[Event] 开 `Deck info Popup`");
            return w;
        }
        public static DeckInfoPopup LastDeckInfo;

        protected void NotBuilt(string what)
        {
            Debug.LogWarning("[Event] `" + what + "` 还没实现（**出声**，见 `Shell/LiveOpsEventWindow.cs` 文件头/各处注释）");
        }
    }
}
