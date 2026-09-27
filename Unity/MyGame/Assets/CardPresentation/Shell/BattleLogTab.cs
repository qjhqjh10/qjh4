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

        // ============================================================ 队列档（页内分层）
        const int L_Bg = 0;      // 行的底板
        const int L_Art = 2;     // 图标（骷髅 / 段位 / 阵营 / 剑）
        const int L_Text = 3;    // 主要文字（结果 / 名字 / 分数 / 次数）
        const int L_Text2 = 4;   // 次要文字（模式 / 联盟名）
        const int L_Btn = 5;     // 两个圆钮的底
        const int L_BtnIcon = 6; // 钮上的图标
        const int L_Hit = 7;     // 命中区

        // ============================================================ 真值（绝对画布像素）
        // `Matches`（`ScrollRect`）：a=(0,0)-(1,1) p=(.5,.5) pos=(0,6.80032) sz=(50,−101.441)
        // ⇒ 326.03,162.84 → 1771.97,904.48（**比页根宽 25、两侧各溢**，判据 ③）。
        // ⚠️ 它的 `m_Sprite` 是 **UGUI 内置 `Background`**、**`m_Color=(1,1,1,0)`（a=0 ⇒ 看不见）** ⇒ **不画**。
        static readonly Vector2 MtA = UguiRect.A00, MtB2 = UguiRect.A11;
        static readonly Vector2 MtPos = new Vector2(0f, 6.80032f), MtSz = new Vector2(50f, -101.441f);
        /// <summary>`Viewport`：`sz=(0,−17)` ⇒ 底边 **887.48**（比 `Matches` 少 17）。`Mask` + `showGraphic=0` ⇒ 只建节点。</summary>
        const float VpSzY = -17f;
        /// <summary>`Content`：`a=(0,1)-(1,1) p=(0,1) pos=(0,0) sz=(0,203.2)` · `VerticalLayoutGroup` **spacing 25 / align 0 (UpperLeft)**。</summary>
        const float RowH = 203.20f, RowGap = 25f;
        /// <summary>`Content` 的 `ContentSizeFitter`（`m_HorizontalFit`/`m_VerticalFit` **查不到**，§D4）——
        /// 我们按「行数 × 行高 + 间距」算内容高（= 原版 `VerticalFit=PreferredSize` 那条语义）。</summary>
        const string ArtRowBg = "UI_Deck_Information_submenu_Back";
        static readonly Vector4 RowBorder = new Vector4(18f, 18f, 18f, 18f);

        // ---- 行内（锚点/pos/sizeDelta 全照 §A·1 的表；用 `UguiRect.Child` 算，别手抄绝对坐标）----
        static readonly Vector2 ResultA = new Vector2(0.5f, 1f), ResultP = new Vector2(0.5f, 1f);
        static readonly Vector2 ResultPos = new Vector2(0f, -16f), ResultSz = new Vector2(250f, 80f);
        const float ResultPx = 65.35f, ResultAutoMin = 40f;
        static readonly Vector2 ModeA = UguiRect.A01, ModeP = UguiRect.P01;
        static readonly Vector2 ModePos = new Vector2(20f, -25f), ModeSz = new Vector2(210f, 76.6f);
        const float ModePx = 40f, ModeAutoMin = 20f;
        /// <summary>两个圆钮：`a=(1,.5) p=(.5,.5)`，`ReplayButton` pos=(−64,43.08)、`PinButton` pos=(−141.87,43.08)，65×65。</summary>
        static readonly Vector2 BtnA = new Vector2(1f, 0.5f), BtnSz = new Vector2(65f, 65f);
        const float ReplayDx = -64f, PinDx = -141.87f, BtnDy = 43.08f;
        const string ArtBtn = "40k_general_bt_yellow", ArtReplay = "40k_general_bt_yellow_replay",
                     ArtPin = "40k_general_bt_yellow_pin_replay";

        /// <summary>`Player Info`：`a=(0,0)-(0.5,1)`（左半边）· `Enemy Info`：`a=(0.5,0)-(1,1)`（右半边）。</summary>
        static readonly Vector2[] SideA = { UguiRect.A00, new Vector2(0.5f, 0f) };
        static readonly Vector2[] SideB = { new Vector2(0.5f, 1f), UguiRect.A11 };
        // 我方（0）与敌方（1）逐个数值 —— **两份不是镜像**（每一条都不同，见 §A·1 的表体）
        static readonly Vector2[] HeroA = { new Vector2(1f, 0.5f), new Vector2(0f, 0.5f) };
        static readonly Vector2[] HeroP = { new Vector2(1f, 0.5f), new Vector2(0f, 0.5f) };
        static readonly Vector2[] HeroPos = { new Vector2(-148f, 45f), new Vector2(148f, 45f) };
        static readonly Vector2[] HeroSz = { new Vector2(340f, 50f), new Vector2(360f, 50f) };
        const float HeroPx = 45f, HeroAutoMin = 18f;
        static readonly Vector2[] NamePos = { new Vector2(-150f, -40f), new Vector2(165f, -40f) };
        static readonly Vector2[] NameSz = { new Vector2(305.036f, 50f), new Vector2(276.45f, 50f) };
        const float NamePx = 38f, NameAutoMin = 12f;
        static readonly Vector2[] ClanPos = { new Vector2(-163f, -77f), new Vector2(165f, -77f) };
        static readonly Vector2[] ClanSz = { new Vector2(250f, 50f), new Vector2(338f, 50f) };
        const float ClanPx = 30f, ClanAutoMin = 10f;
        static readonly Vector2[] ScoreA = { new Vector2(0f, 0.5f), new Vector2(1f, 0.5f) };
        static readonly Vector2[] ScoreP = { new Vector2(0f, 0.5f), new Vector2(1f, 0.5f) };
        static readonly Vector2[] ScorePos = { new Vector2(128.51f, -40f), new Vector2(-142.1f, -40f) };
        static readonly Vector2 ScoreSz = new Vector2(116.45f, 50f);
        const float ScorePx = 46.25f, ScorePxEnemy = 35.85f, ScoreAutoMin = 12f;
        static readonly Vector2[] ScIcPos = { new Vector2(63.51f, -40f), new Vector2(-71.1f, -40f) };
        static readonly Vector2 ScIcSz = new Vector2(65f, 65f);
        const string ArtScoreIcon = "40k_UI_icon_ranked_Skirmish";
        /// <summary>`Score Icon (1)`（阵营图标）：🔴 **两份都是 `a=(0,.5) p=(0,.5)`** ——
        /// 敌方那一份与它自己的 `Score`/`Score Icon`（都是 `a=(1,.5)`）**不同**，别当成同一套。</summary>
        static readonly Vector2 FacA = new Vector2(0f, 0.5f), FacP = new Vector2(0f, 0.5f);
        static readonly Vector2[] FacPos = { new Vector2(9.68039f, -40f), new Vector2(651.87f, -40f) };
        static readonly Vector2 FacSz = new Vector2(53.8296f, 53.8295f);
        const string ArtFactionIcon = "40k_DeckSelection_icon_FactionSororitas";
        static readonly Vector2 SkullA = UguiRect.P50c, SkullP = UguiRect.P50c;
        static readonly Vector2[] SkullPos = { new Vector2(250f, -40f), new Vector2(-250f, -40f) };
        static readonly Vector2 SkullSz = new Vector2(100f, 100f);
        const string ArtSkull = "40K_missions_icon_Daily_skulls";
        static readonly Vector2[] SkullCntPos = { new Vector2(-115f, -75.677f), new Vector2(-548f, -75.677f) };
        static readonly Vector2 SkullCntSz = new Vector2(61.5863f, 28.6458f);
        const float SkullCntPx = 30.2f, SkullCntAutoMin = 18f;
        /// <summary>`Details/Sword`（`40k_main_bt_play` 124×109）—— 100×100 居中偏下。</summary>
        static readonly Vector2 SwordPos = new Vector2(0f, -40f), SwordSz = new Vector2(100f, 100f);
        const string ArtSword = "40k_main_bt_play";
        /// <summary>`Enemy Info/GameObject`（`UIGenericEventCatcher` + `NonDrawingGraphic`，**不可见**）
        /// —— 原版那块是「点敌方那块 ⇒ `BattleLogPlayerDisplay.OnPlayerClicked`（开他的档案）」。
        /// 我们照它建一个**透明命中区**（`Hit`），动作见 `OnEnemyClicked`。</summary>
        static readonly Vector2 CatcherA = UguiRect.P50c, CatcherPos = new Vector2(-13.225f, -36.25f);
        static readonly Vector2 CatcherSz = new Vector2(343.55f, 72.5f);

        MenuScroll _scroll;
        Transform _content;
        BattleLogData.Match[] _rows = new BattleLogData.Match[0];

        /// <summary>这一屏建出来的行数（自检用：条数多时只建看得见的）。</summary>
        public int BuiltRows { get { return _rows.Length; } }

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

            Clip = vp;
            BuildRows();
            Clip = null;
        }

        /// <summary>滚轮改了偏移 ⇒ 重画（**先清再建**，回调会重入 —— 同 `ForgeTab.BuildRewardCells` 那条）。
        /// 数据变了也调它（原版 `OnOpen` 就是「清空 + 逐条 instantiate」那两步）。</summary>
        public void RebuildRows()
        {
            if (_content == null) return;
            for (int i = _content.childCount - 1; i >= 0; i--) DestroyNow(_content.GetChild(i).gameObject);
            Clip = _scroll.Viewport;
            BuildRows();
            Clip = null;
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
            for (int i = 0; i < n; i++)
            {
                float y = top + i * (RowH + RowGap);
                var r = _scroll.Shift(new PxRect(_scroll.Viewport.x1, y, _scroll.Viewport.x2, y + RowH));
                if (!_scroll.Intersects(r)) continue;
                BuildRow(all[i], i, r);
                built.Add(all[i]);
            }
            _rows = built.ToArray();
        }

        /// <summary>一行 = 原版 `Instantiate(logPrefab)` 出来的那棵树（§B·2 那张表逐件照抄）。</summary>
        void BuildRow(BattleLogData.Match m, int index, PxRect r)
        {
            var row = Node(_content, "Match Log", r);
            Nine(row, ArtRowBg, r, RowBorder, "Background", L_Bg);

            // `Result`（`Victory` / `Defeat` / `Draw`）—— 文案走 `CardText.Phrase`（与结算面板同一个源）。
            // ⚠️ 原版还会按结果换**字体材质**（`victoryMaterial`/…）—— 那三个指针是**跨文件引用**
            //    （`m_FileID=5`），本地解不出（§D3）⇒ 我们用预制体自己的字色（白），**如实记着**。
            var resR = UguiRect.Child(r, ResultA, ResultA, ResultP, ResultPos, ResultSz);
            Text(row, BattleLogData.ResultText(m.Result), resR, Color.white, "Result", ResultPx, L_Text,
                 autoFit: true, autoMinPx: ResultAutoMin);

            var modeR = UguiRect.Child(r, ModeA, ModeA, ModeP, ModePos, ModeSz);
            Text(row, m.Mode ?? "", modeR, Color.white, "Mode", ModePx, L_Text2,
                 autoFit: true, autoMinPx: ModeAutoMin, alignLeft: true, wrap: true);

            // 两个圆钮（**图标挂在钮里面**，照原版树：`ReplayButton > replayicon`）
            var repR = UguiRect.Child(r, BtnA, BtnA, UguiRect.P50c, new Vector2(ReplayDx, BtnDy), BtnSz);
            var rep = Node(row, "ReplayButton", repR);
            Rect(rep, ArtBtn, repR, "Image", L_Btn);
            Rect(rep, ArtReplay, repR, "replayicon", L_BtnIcon);
            Hit(rep, "Hit", repR, L_Hit, () => OnReplay(m));

            var pinR = UguiRect.Child(r, BtnA, BtnA, UguiRect.P50c, new Vector2(PinDx, BtnDy), BtnSz);
            var pin = Node(row, "PinButton", pinR);
            Rect(pin, ArtBtn, pinR, "Image", L_Btn);
            var pinIc = Rect(pin, ArtPin, pinR, "pinicon", L_BtnIcon);
            if (pinIc != null) pinIc.SetTint(m.Pinned ? BattleLogData.PinOn : BattleLogData.PinOff);
            Hit(pin, "Hit", pinR, L_Hit, () => OnPin(m, pinIc));

            BuildSide(row, r, m, 0);
            BuildSide(row, r, m, 1);

            // `Details`（一个透明容器）> `Sword`：中间那把剑
            var det = Node(row, "Details", r);
            var swordR = UguiRect.Child(r, SkullA, SkullA, UguiRect.P50c, SwordPos, SwordSz);
            Rect(det, ArtSword, swordR, "Sword", L_Art);
        }

        /// <summary>半边（0 = `Player Info` 我方 · 1 = `Enemy Info` 敌方）。两份的**每一条数值都不同** —— 照 §A·1。</summary>
        void BuildSide(Transform row, PxRect r, BattleLogData.Match m, int side)
        {
            bool mine = side == 0;
            var sideR = UguiRect.Child(r, SideA[side], SideB[side], UguiRect.P50c, Vector2.zero, Vector2.zero);
            var box = Node(row, mine ? "Player Info" : "Enemy Info", sideR);

            // 对齐**逐格不同**（照 §A·1 的「文字」那一列）：我方那三行是 `Right`、我方的 `Score` 是 `Left`、
            // 敌方那三行是 `Left`、敌方的 `Score` 是 `Right` —— 别按「半边」一刀切。
            var heroR = UguiRect.Child(sideR, HeroA[side], HeroA[side], HeroP[side], HeroPos[side], HeroSz[side]);
            TextSide(box, mine ? m.OwnHeroName : m.EnemyHeroName, heroR, "Hero Name", HeroPx, L_Text,
                     HeroAutoMin, mine);

            var nameR = UguiRect.Child(sideR, HeroA[side], HeroA[side], HeroP[side], NamePos[side], NameSz[side]);
            TextSide(box, mine ? m.OwnName : m.EnemyName, nameR, "Player Name", NamePx, L_Text,
                     NameAutoMin, mine);

            var clanR = UguiRect.Child(sideR, HeroA[side], HeroA[side], HeroP[side], ClanPos[side], ClanSz[side]);
            TextSide(box, mine ? m.PlayerClan : m.EnemyClan, clanR, "Alliance Name", ClanPx, L_Text2,
                     ClanAutoMin, mine, true);

            var scoreR = UguiRect.Child(sideR, ScoreA[side], ScoreA[side], ScoreP[side], ScorePos[side], ScoreSz);
            // ⚠️ `Score` 那一格**与外层相反**：我方是 `Left/Middle`、敌方是 `Right/Middle`
            TextSide(box, mine ? m.OwnScore : m.EnemyScore, scoreR, "Score",
                     mine ? ScorePx : ScorePxEnemy, L_Text, ScoreAutoMin, !mine);

            var sciR = UguiRect.Child(sideR, ScoreA[side], ScoreA[side], ScoreP[side], ScIcPos[side], ScIcSz);
            Rect(box, ArtScoreIcon, sciR, "Score Icon", L_Art, null, true);

            var facR = UguiRect.Child(sideR, FacA, FacA, FacP, FacPos[side], FacSz);
            Rect(box, ArtFactionIcon, facR, "Score Icon (1)", L_Art, null, true);

            var skR = UguiRect.Child(sideR, SkullA, SkullA, SkullP, SkullPos[side], SkullSz);
            Rect(box, ArtSkull, skR, "Skulls", L_Art, null, true);
            // ⚠️ 两个 `Skulls` 上都有 `UIFlippable`（`m_Horizontal`/`m_Veritical`）—— **那两个值本地读不到**
            //    （§A·1 只印了字段名）⇒ 我们**不翻**，如实记着（翻了就是猜）。
            var cntR = UguiRect.Child(sideR, HeroA[side], HeroA[side], HeroP[side], SkullCntPos[side], SkullCntSz);
            Text(box, "x" + (mine ? m.OwnSkulls : m.EnemySkulls), cntR, Color.white, "skullCounter",
                 SkullCntPx, L_Text, autoFit: true, autoMinPx: SkullCntAutoMin);

            if (!mine)
            {
                // `UIGenericEventCatcher` + `NonDrawingGraphic`（**不可见**）⇒ 我们建一个透明命中区
                var catR = UguiRect.Child(sideR, CatcherA, CatcherA, UguiRect.P50c, CatcherPos, CatcherSz);
                Node(box, "GameObject", catR);
                Hit(box, "EnemyClickHit", catR, L_Hit, OnEnemyClicked);
            }
        }

        /// <summary>半边里的一段字。`right` = **右对齐**（原版我方那三行是 `Right/Middle`）。
        /// `ProfilePage.Text` 只有左对齐那一路 ⇒ 右对齐走 `MenuDraw.AlignRight`（同一处实现，别另写一份）。</summary>
        Label TextSide(Transform parent, string s, PxRect r, string name, float px, int q,
                       float autoMin, bool right, bool wrap = false)
        {
            var lb = Text(parent, s, r, Color.white, name, px, q, autoFit: true, autoMinPx: autoMin,
                          alignLeft: !right, wrap: wrap);
            if (lb != null && right) MenuDraw.AlignRight(lb, r);
            return lb;
        }

        // ============================================================ 三个动作（原版各自的落点见 §B·4）

        /// <summary>`ClickReplayButton` → 原版 `PlayerDataManager.RecoverMatchRecording(recordingIndex, …)`。
        /// 我们的**回放整条还没做**（`项目任务.md` §三 第 18 条 第 6 件）⇒ **出声**，不静默。</summary>
        void OnReplay(BattleLogData.Match m)
        {
            Debug.Log("[Profile] 点回放（原版 `RecoverMatchRecording(recordingIndex=" + m.RecordingIndex + ")`）"
                    + "—— **回放还没做**（§三 第 18 条 第 6 件：要先补正本那张表）。"
                    + "地基是现成的：`Net/NetBattle` 里那串权威动作流。");
        }

        /// <summary>`ClickPinButton` → 原版先 `pinButton.interactable=false`，再 `PinMatchRecording(…, !pinned, …)`，
        /// 回来 `SetPinState` 把 `pinButton.image.color` 设成 **未钉 (1,1,1,1) / 已钉 (0,1,0,1)**（绿）。
        /// ⚠️ 原版那一步要**上传服务器**；我们只有本地那一份 ⇒ 只在本地翻（如实出声）。</summary>
        void OnPin(BattleLogData.Match m, ImageQuad icon)
        {
            m.Pinned = !m.Pinned;
            if (icon != null) icon.SetTint(m.Pinned ? BattleLogData.PinOn : BattleLogData.PinOff);
            Debug.Log("[Profile] 这一局" + (m.Pinned ? "**已钉住**（钮变绿）" : "**取消钉住**")
                    + " —— ⚠️ 只在本地：原版 `PinMatchRecording` 是上传服务器 + 回放要不要留档由它管。");
        }

        /// <summary>点敌方那一块 → 原版 `BattleLogPlayerDisplay.OnPlayerClicked` 开他的档案
        /// （`ChatPlayerOptionsPanel.ShowProfile` 那一路，§A·1 的入口表第 1/2 条）。
        /// 我们本地对局的对手是 bot ⇒ **出声**。</summary>
        void OnEnemyClicked()
        {
            Debug.Log("[Profile] 点对手那一块（原版开他的玩家档案）—— 我们本地对局的对手是 bot，**没有档案可开**");
        }
    }
}
