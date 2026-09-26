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
//      选场判据 = `ArenaByArmy.BattleSceneNameFor(阵营)`（缺席时**回落 `Battle` 并出声**）。
//      做法/依据/验收 → `资料/阶段二_战斗入口_原版规格.md` **§七**。
using System.Collections.Generic;
using UnityEngine;
using RuleEngine;

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
        public const int QBg = 3104;      // 压暗整屏
        public const int QBg1 = 3105;     // 红底
        public const int QBg2 = 3106;     // 红底上的 `Noise`
        public const int QBg3 = 3107;     // `Menu Vignette`
        public const int QDeck = 3108;    // 中栏第一层（阵营徽记）
        public const int QDeck1 = 3109;   // 中栏第二层（督军立绘）
        public const int QDeck2 = 3110;   // 中栏第三层（`Warlord Darkening` 压暗）
        public const int QArt = 3111;     // 各处的按钮底 / 面板底 / 装饰
        public const int QArt1 = 3112;    // 压在 `QArt` 上的那一层（按钮里的图标、标题栏的尖角/返回钮）
        public const int QArt2 = 3113;    // 再上面一层（返回钮压在尖角上）
        public const int QText = 3114;
        /// <summary>🔴 **整屏背板那一下必须比其他命中区【低】** —— `PointerLayer` 的判据是「队列大的先吃、
        /// 同队列比 z」，而所有 quad 的 z 恒 0 ⇒ 同队列时点 `Battle!` 可能被判成「点背景」直接关窗
        /// （2026-09-24 找茬子代理抓到的阻断项）。`PracticeModePopup` 里 `QPrHit - 1` 就是同一个写法。</summary>
        public const int QHitBackdrop = 3115;
        public const int QHit = 3116;

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

        public const float HdrL = 0f, HdrT = 40.86f, HdrR = 550f, HdrB = 150.41f;
        /// <summary>`Header Background` 的 HLG：spacing **5.5** · pad **(155,61,0,0)** · align **MiddleLeft**。</summary>
        public const float HdrPadL = 155f, HdrPadR = 61f, HdrGap = 5.5f;
        public const float HdrBg1L = -462.10f, HdrBg1T = 40.87f, HdrBg1R = 87.90f, HdrBg1B = 156.23f;
        public const float HdrBackL = -24.40f, HdrBackT = 42.88f, HdrBackR = 143.48f, HdrBackB = 154.21f;

        public const float BattleL = 1376.76f, BattleT = 917.80f, BattleR = 1817.09f, BattleB = 1038.40f;
        public const float BattleTxL = 1392.21f, BattleTxT = 929.56f, BattleTxR = 1800.98f, BattleTxB = 1026.52f;
        /// <summary>`ButtonIcons`（HLG spacing **−28.31** · align **MiddleRight**）· 图标 100²。</summary>
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
        public const string ArtDeckCount = "40k_UI_icon_deck";
        public const string ArtHeaderBg = "WF_Campaign_Info_Background";
        public const string ArtHeaderBack = "UI_Button_Menu_Back";
        public const string ArtShield = "UI_icon_shield";
        public const string ArtHelp = "40K_generic_bt_info";
        /// <summary>`WF_Campaign_Info_Background`：**740×167 · border (335,0,395,0)**（`Sprite/*.json` 实测）。</summary>
        public static readonly Vector4 HeaderBorder = new Vector4(335f, 0f, 395f, 0f);
        public const float HeaderTexW = 740f, HeaderTexH = 167f;

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
                return DeckGameMode == (int)GameMode.Skirmish
                     ? "遭遇战（Skirmish · 12 张）" : "经典（Classic · 30 张）";
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
                why = "这副预组是「" + (pre.gameMode == (int)GameMode.Skirmish ? "遭遇 · 12 张" : "经典 · 30 张")
                    + "」的，不能用在" + DeckGameModeName + "里 —— 换一副。";
                return false;
            }
            var raw = CollectionData.Raw(DeckIndex);
            if (raw == null)
            {
                why = "还没有可用的卡组 —— 先点 `Create deck` 建一副" + DeckGameModeName + "的。";
                return false;
            }
            if (DeckFitsMode(raw)) { why = null; return true; }
            why = "「" + raw.Name + "」是「"
                + (raw.IsSkirmish ? "遭遇 · 12 张" : "经典 · 30 张")
                + "」的卡组，不能用在" + DeckGameModeName + "里 —— 换一副，或点 `Create deck` 建一副新的"
                + "（照原版：**模式在建组那一刻定，之后改不了**）。";
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
                MenuDraw.Rect(b, Tex(ArtRoundBtn), new PxRect(x1, y1, x2, y2), "Bg", QArt, null, true);
                var icon = Tex(icons[i]);
                // 🔴 图标必须**高于**底图那一层（同队列谁盖谁不可控 —— 第一版就是这样，实拍里后三颗是空的）
                // ⚠️ **图标框不是整颗钮**：原版 `Icon` 子件是 **120.49×93.01**、在 160.87×128 的钮里居中
                //    （`preserveAspect` ⇒ 实画 94.08×93.01）。用整颗钮当框会把图标放大 **1.376 倍**
                //    —— 2026-09-24 找茬子代理按锚点比例算出来的（`menu_rect --depth 5` 那一行）。
                var q = MenuDraw.Rect(b, icon, IconBox(x1, y1, x2, y2), "Icon", QArt1, null, true);
                // ⚠️ 原版 `Next Deck Button` 的图标挂 `UIFlippable`（水平翻转成「→」）——
                //    我们的 `ImageQuad` 没有翻转开关 ⇒ **这一颗是「←」不是「→」**，如实记着（出声）。
                if (i == 3 && q != null)
                    Debug.Log("[Event] `Next Deck Button` 的箭头**没翻转**（原版走 `UIFlippable`，`ImageQuad` 没有这个开关）—— 出声");
                MenuDraw.Hit(b, "Hit", new PxRect(x1, y1, x2, y2), QHit, acts[i]);
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
            var ndt = MenuDraw.Text(none, new PxRect(NoDeckL, NoDeckT, NoDeckR, NoDeckB),
                                    "这套卡组还没有督军 —— 去卡组编辑里选一个再来。", Color.white,
                                    "No Deck Text", 45f, QText);
            // ⚠️ 我们这句话比原版长（24 字 × fs45 ≈ 1080px）⇒ **会溢出 685.65 的框** ⇒ 自己缩着放进去
            //    （原版那句带 `auto 18-45`，我们照它的区间自缩）——**这一条是我们挑的**
            if (ndt != null) ndt.SetAutoFitBox(LayoutSpace.Px(NoDeckR - NoDeckL), LayoutSpace.Px(NoDeckB - NoDeckT), 18f, 45f);
            //   ⚠️ 原版那句是 `Localize` 的西班牙语串（带 `{0} {1}` 占位符，运行时灌「有几个指挥官」）
            //      ⇒ **我们印自己的话**（本地无术语表、也没有那个计数）—— 这一条是**我们写的**，不是复刻。
            MenuDraw.Rect(none, Tex(ArtMulligan), new PxRect(CreateBtnL, CreateBtnT, CreateBtnR, CreateBtnB),
                          "Generic Simplified UI Button", QArt, null, true);
            // ⚠️ 文字挂在**那颗钮底下**（原版 `Button Text` 是钮的子件；直接挂容器会让 `FindChild(钮,"Button Text")` 找不到）
            var cbtn = MenuDraw.Node(none, "Generic Simplified UI Button",
                                     new PxRect(CreateBtnL, CreateBtnT, CreateBtnR, CreateBtnB));
            MenuDraw.Rect(cbtn, Tex(ArtMulligan), new PxRect(CreateBtnL, CreateBtnT, CreateBtnR, CreateBtnB),
                          "Bg", QArt, null, true);
            MenuDraw.Text(cbtn, new PxRect(CreateTxL, CreateTxT, CreateTxR, CreateTxB), "Create deck",
                          Color.white, "Button Text", 55f, QText);      // 原版 hAlign = Center ⇒ 不调 Align*
            MenuDraw.Hit(none, "CreateDeckHit", new PxRect(CreateBtnL, CreateBtnT, CreateBtnR, CreateBtnB), QHit,
                         CreateDeckInMode);
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
        /// ⚠️ `Army Content` 的格是**运行时实例化的**（出厂 0 子）—— 原版灌什么**没查**；
        ///    我们照练习窗那套用**阵营徽记**（13 个），**这一条是我们挑的**。</summary>
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
            _facs = CollectionWindow.CardsState.Factions();

            int cols = Mathf.Max(1, Mathf.FloorToInt((ArmViewR - ArmViewL) / ArmyCell));
            int rows = Mathf.CeilToInt(_facs.Count / (float)cols);
            float contentH = ArmyPadT + ArmyCell * rows;
            var view = new PxRect(ArmViewL, ArmViewT, ArmViewR, ArmViewB);
            _armyScroll = MenuScroll.TopAligned(view, contentH);
            _armyScroll.Owner = gameObject;
            _armyContent = MenuDraw.Node(vp, "Army Content",
                                         new PxRect(ArmViewL, ArmViewT, ArmViewR, ArmViewT + contentH));
            _armyScroll.OnChanged = () => RebuildArmyCells(_armyContent);
            PointerLayer.RegisterScroll(_armyScroll);
            RebuildArmyCells(_armyContent);
        }

        Transform _armyHolder, _armyContent;
        void RebuildArmyCells(Transform holder)
        {
            _armyHolder = holder;
            for (int i = holder.childCount - 1; i >= 0; i--) RewardsWindow.DestroySafe(holder.GetChild(i).gameObject);
            ArmyCells.Clear();
            var facs = _facs ?? new List<string>();
            int cols = Mathf.Max(1, Mathf.FloorToInt((ArmViewR - ArmViewL) / ArmyCell));
            var view = new PxRect(ArmViewL, ArmViewT, ArmViewR, ArmViewB);
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
                if (rr.y2 <= view.y1 + 0.5f || rr.y1 >= view.y2 - 0.5f) continue;   // 只跳**完全**在视口外的
                var cell = MenuDraw.Node(holder, "Army_" + i, rr);
                MenuDraw.Rect(cell, Tex(DeckRuntime.FactionIcon(facs[i])),
                              new PxRect(rr.x1 + 8f, rr.y1 + 8f, rr.x2 - 8f, rr.y2 - 8f), "Icon", QArt, null, true, view);
                if (ArmyIndex == i)
                    MenuDraw.Rect(cell, Tex("Highlight_Rounded_Square"),
                                  new PxRect(rr.x1 - 1.4f, rr.y1 - 1.4f, rr.x1 + 289.8f * 0.9f - 1.4f, rr.y1 + 427.3f * 0.9f - 1.4f),
                                  "Highlight", QArt1, new Color(1f, 0.773f, 0f, 1f));
                int idx = i;
                MenuDraw.Hit(cell, "Hit", rr, QHit, () => PickArmy(idx));
                ArmyCells.Add(cell);
            }
        }

        // ------------------------------------------------------------ 顶上那条标题栏
        /// <summary>`Game Mode Header With Back Button` + `Header Background`(HLG) + `Header Background (1)` + `Header Back Button`。
        /// ⚠️ `Game Mode Icon` 原版 `sprite=0`（由 `LiveopUIDrawer_GameModeIcon` 运行时按模式喂，
        ///    那两张图**本地没有**）⇒ **不画**，出声。</summary>
        void BuildHeader(Transform root)
        {
            var hdr = MenuDraw.Node(root, "Game Mode Header With Back Button",
                                    new PxRect(HdrL, HdrT, HdrR, HdrB));
            // `Header Background`：`ContentSizeFitter` 按内容撑开 ⇒ 它自己宽 0；HLG 从 **padLeft 155** 起排
            // 底板宽度 = **内容撑开的宽度**：原版 `Header Background` 挂 `ContentSizeFitterMinMax`
            //   （`widthMin 550 / widthMax 1250`），按内容 = padL 155 + 标题 369.36 + spacing 5.5 + 图标 100 + padR 61
            //   = **690.86**。原来只画到父容器右缘 550，少了 140（找茬子代理按字段算出来的）
            const float plateR = 690.86f;
            var bgT = MenuDraw.Node(hdr, "Header Background", new PxRect(HdrL, HdrBg1T, plateR, HdrBg1B));
            MenuDraw.Nine(bgT, Tex(ArtHeaderBg), new PxRect(HdrL, HdrBg1T, plateR, HdrBg1B),
                          HeaderBorder, HeaderTexW, HeaderTexH, QArt);
            float titleL = HdrL + HdrPadL;
            var title = MenuDraw.Text(bgT, new PxRect(titleL, HdrT + 16.36f, titleL + 369.36f, HdrT + 99.01f),
                                      "Game mode", Color.white, "Window Title", 67.55f, QText);
            // 🔴 **顺序不能反**：`AlignLeftOn` 是按**当时的 `WorldW`** 定位的，而 `SetAutoFitBox` 会**改字号 ⇒ 改宽**
            //    ⇒ 先对齐再自适应，左边缘会被推走（实测偏 40.75px）。已知的坑「对齐必须在 SetText 之后」的同一条。

            //   ⚠️ 原版这行 TMP `charSpacing = 5` —— 我们的 `Label` **没有字距开关** ⇒ 这一条没复刻（出声）
            Debug.Log("[Event] `Window Title` 的 `charSpacing = 5` **没复刻**（`Label` 没有字距接口）—— 出声");
            if (title != null)
            {
                title.SetAutoFitBox(LayoutSpace.Px(369.36f), LayoutSpace.Px(82.65f), 18f, 67.55f);
                MenuDraw.AlignLeft(title, new PxRect(titleL, HdrT + 16.36f, titleL + 369.36f, HdrT + 99.01f));
            }
            // `Game Mode Icon`（HLG 里紧跟标题：155 + 369.36 + spacing 5.5 ⇒ 左沿 **529.86**，竖中在 115.36 的板里）
            // 2026-09-24 订正：原来这里写「那两张图本地没有」—— **是错的**：
            //   `Resources/Art/ui_menu/40k_gamemode_icon_skirmish.png` 与 `…_classic.png` **早在工程里**
            //   （练习窗那个 `Toggle` 用的就是它们）。遭遇战窗接 skirmish 那张；
            //   排位那张**查不到**（本地只有 classic / skirmish）⇒ 子类给 null 时不画并出声。
            float iconL = titleL + 369.36f + HdrGap;
            float iconT = HdrBg1T + (HdrBg1B - HdrBg1T - 100f) * 0.5f;
            var gmTex = Tex(GameModeIconArt);
            if (gmTex != null)
                MenuDraw.Rect(bgT, gmTex, new PxRect(iconL, iconT, iconL + 100f, iconT + 100f),
                              "Game Mode Icon", QArt1, null, true);
            else
                Debug.Log("[Event] `Game Mode Icon` **没画** —— 这一窗的模式图标本地没有"
                          + "（原版 `sprite=0`、由 `LiveopUIDrawer_GameModeIcon` 运行时喂；"
                          + "本地只有 `40k_gamemode_icon_classic` / `_skirmish` 两张）");

            // `Header Background (1)`（往左延伸的尖角）与返回钮：**兄弟序在后 ⇒ 队列更高**
            MenuDraw.Nine(hdr, Tex(ArtHeaderBg),
                          new PxRect(HdrBg1L, HdrBg1T, HdrBg1R, HdrBg1B),
                          HeaderBorder, HeaderTexW, HeaderTexH, QArt1);
            MenuDraw.Rect(hdr, Tex(ArtHeaderBack),
                          new PxRect(HdrBackL, HdrBackT, HdrBackR, HdrBackB), "Header Back Button", QArt2, null, true);
            MenuDraw.Hit(hdr, "BackHit", new PxRect(HdrBackL, HdrBackT, HdrBackR, HdrBackB), QHit, () => Close());
        }

        // ------------------------------------------------------------ `Battle!`
        /// <summary>`To Battle Button`（`UI_Button_Mulligan` **preserveAspect**）+ 文案 + `ButtonIcons`（两颗 100² 图标、
        /// HLG spacing **−28.31** · align **MiddleRight**）。</summary>
        void BuildToBattle(Transform root)
        {
            var btn = MenuDraw.Node(root, "To Battle Button", new PxRect(BattleL, BattleT, BattleR, BattleB));
            MenuDraw.Rect(btn, Tex(ArtMulligan), new PxRect(BattleL, BattleT, BattleR, BattleB),
                          "Bg", QArt, null, true);
            // 原版 hAlign = **Center** ⇒ 不调 `Align*`（原来右对齐了）
            MenuDraw.Text(btn, new PxRect(BattleTxL, BattleTxT, BattleTxR, BattleTxB), "Battle!",
                          Color.white, "Button Text", 74.25f, QText);

            var icons = MenuDraw.Node(btn, "ButtonIcons", new PxRect(BIconsL, BIconsT, BIconsR, BIconsB));
            float cy = (BIconsT + BIconsB) * 0.5f;
            // HLG align = MiddleRight ⇒ 从右边往左排，间距 −28.31（**负数 = 两颗叠一点**）
            float x2 = BIconsR, x1 = x2 - BIconSide;
            // 🔴 这两颗图标压在 `UI_Button_Mulligan` 那张底图上 ⇒ 必须用 **QArt1**（同队列会谁盖谁不可控）
            MenuDraw.Rect(icons, Tex(TrophyIconArt),
                          new PxRect(x1, cy - BIconSide * 0.5f, x2, cy + BIconSide * 0.5f), "TrophyIcon", QArt1, null, true);
            x2 = x1 - BIconsGap; x1 = x2 - BIconSide;
            MenuDraw.Rect(icons, Tex(ArtShield),
                          new PxRect(x1, cy - BIconSide * 0.5f, x2, cy + BIconSide * 0.5f), "ShieldIcon", QArt1, null, true);

            MenuDraw.Hit(btn, "BattleHit", new PxRect(BattleL, BattleT, BattleR, BattleB), QHit, () => StartMatch());
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
            // 🆕 2026-09-26：**先过模式这一关**（原版 `DeckUtility.ValidateDeck(deck, out err, isSkirmish)`
            //   被 `SkirmishEventWindow.OnDeckSelected → HasValidDeckWithValidationMessage` 调）——
            //   ⚠️ 拦截**不是**静默：弹窗说清「哪一副、为什么不行、怎么办」。
            if (!SelectedDeckFitsMode(out string whyMode))
            {
                Debug.LogWarning("[Event] 开战被挡：模式不对 —— " + whyMode);
                if (Manager != null) Manager.ShowPopUp(whyMode, "知道了", null);
                return;
            }
            var d = CollectionData.DeckAt(DeckIndex);
            if (string.IsNullOrEmpty(d.WarlordId) && PickedPrebuilt == null)
            {
                Debug.LogWarning("[Event] 这套卡组**没有督军**，开不了局 —— 如实说，不静默。");
                if (Manager != null) Manager.ShowPopUp("这套卡组还没有选督军，开不了局。", "知道了", null);
                return;
            }
            _search.OnCancel = CancelSearch;
            _search.OnSearchDone = () => { OnSearchFinished(); StartBotBattle(); };
            _search.BeginSearch(ShowInlineSearchPopup);
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
        }

        /// <summary>打 bot 那一支：选定卡组 + 切该阵营那份对战场景
        /// （= 原版 `SearchOpponentManager.StartBattle → GetBattleArena → LoadScene` 的等价物）。</summary>
        public void StartBotBattle()
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
        /// （`CollectionData.PendingEditDeck`，静态字段跨场景）。</summary>
        public void CreateDeckInMode()
        {
            int mode = DeckGameMode;
            string name = CollectionData.CreateDeck(mode);
            int idx = CollectionData.IndexOf(name);
            CollectionData.Select(idx);            // 新卡组即选中（`DeckLibrary.Create` 本来就把它置成 current）
            CollectionData.PendingEditDeck = idx;
            Debug.Log("[Event] 新建卡组「" + name + "」· 模式 "
                      + (mode == (int)GameMode.Skirmish ? "遭遇 Skirmish（12 张）" : "经典 Classic（30 张）")
                      + "（照原版 `SelectDecksTab.CreateDeck`：**建组这一刻定模式**，之后没有改的路径）"
                      + " ⇒ 进卡组编辑（交接下标 " + idx + "）");
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
