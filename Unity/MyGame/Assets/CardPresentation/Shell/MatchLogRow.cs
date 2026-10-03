// MatchLogRow.cs — **对局历史那一行**（原版 `logPrefab` / 节点名 `Match Log`）的**唯一一份** builder
//
// ============================ 为什么要抽出来 ============================
// 🔴 原版 `BattleLogTab.logPrefab` 与 `BattleLogPopup.logPrefab` 是**同一个 prefab**
// （两处 MB 的 `logPrefab` 都指 `m_PathID 8607776031950241599`，判据 →
// `资料/普查产出_0927/对局历史_行模板与弹窗.md` §B·补列 ①）。
// ⇒ 两扇窗的行**必须是同一份几何/同一套判据**，所以这里**只写一份**，
// `BattleLogTab`（档案窗第 4 页）与 `BattleLogPopup`（独立弹窗）都调它。
// 出处：`资料/普查产出_0927/档案窗_BattleLog与页签按钮.md` §A·1/§A·4/§B（行模板那一张表）·
// `对局历史_行模板与弹窗.md` §A（prefab 根 `-5579061773596819649` 的 32 节点）。
//
// ⚠️ **2026-09-27 从 `BattleLogTab` 原样搬出来**（常量与算式一字未改），只多了一层「画图上下文」
//    （取图 / 队列档 / 裁切边界），好让两扇窗共用。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>行 builder 的**画图上下文**：两扇窗各给自己的一份。</summary>
    public class RowCtx
    {
        /// <summary>取图（取不到会记进宿主自己的 `MissingArt`）。</summary>
        public System.Func<string, Texture2D> Art;
        /// <summary>本行的**队列基档**（行内各层用 `Q + 偏移`）。</summary>
        public int Q;
        /// <summary>裁切边界（画布像素；`null` = 不裁）。</summary>
        public PxRect? Clip;
    }

    /// <summary>对局历史的一行。**唯一的一份**（见文件头）。</summary>
    public static class MatchLogRow
    {
        // ============================================================ 行本身
        /// <summary>行高 **203.20** · 行距 **25**（`Content` 的 `VerticalLayoutGroup` spacing 25 / UpperLeft）。
        /// ⚠️ 这两个数**两扇窗都一样**（`Battle Log Tab` 与 `Battle Log Popup` 的 `Content` 各一份，
        /// 但参数逐值相同）—— 弹窗那份的 `Matches` 只差 `ScrollRect.m_MovementType`（**Clamped** vs Elastic）。</summary>
        public const float RowH = 203.20f, RowGap = 25f;

        // ---- 行底板 ----
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
        /// <summary>`skullCounter`（`x N`）的位置。🔴 **2026-09-27 订正**：原来写 **-115 / -548**，
        /// 那是**场景里那个实例**的值；而行是 `Instantiate(logPrefab)` 出来的 ⇒ 该用 **prefab 根
        /// `-5579061773596819649`** 上那份 = **-104.9 / -536.7**（普查 §A·1 + §D1，直读原始 JSON）。
        /// 差 10.1px 看着小，但**行为差异是真的**（行来自 prefab、不来自场景）。</summary>
        static readonly Vector2[] SkullCntPos = { new Vector2(-104.9f, -75.677f), new Vector2(-536.7f, -75.677f) };
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

        // ---- 行内队列档（相对 `RowCtx.Q`；HUD/窗口那一档在别处，互不重叠）----
        const int L_Bg = 0, L_Art = 2, L_Text = 3, L_Text2 = 4, L_Btn = 5, L_BtnIcon = 6, L_Hit = 7;

        // ============================================================ 画图转发（只有 `MenuDraw` 那一份实现）

        static Transform Node(Transform p, string n, PxRect r) { return MenuDraw.Node(p, n, r); }

        static ImageQuad Rect(RowCtx c, Transform p, string art, PxRect r, string n, int off,
                              Color? tint = null, bool keepAspect = false)
        {
            var tex = art == null ? CardArt.Solid() : c.Art(art);
            return MenuDraw.Rect(p, tex, r, n, c.Q + off, tint, keepAspect, c.Clip);
        }

        /// <summary>行底九宫格。🆕 2026-10-03：**`c.Clip` 也传下去了**（此前这一处漏了 —— 滚动区里
        /// 日志行底一直画到视口外，因为 `MenuDraw.Nine` 那时根本没有 `clip` 参数）。
        /// 求交那一份 = `MenuDraw.ClipRect`（唯一一份）；整块在框外 ⇒ `Nine` 返回 null（连节点一起不建）。</summary>
        static GameObject Nine(RowCtx c, Transform p, string art, PxRect r, Vector4 b, string n, int off)
        {
            var tex = c.Art(art);
            return tex == null ? null : MenuDraw.Nine(p, tex, r, b, tex.width, tex.height, c.Q + off, null, true, n,
                                                      clip: c.Clip);
        }

        /// <summary>`ProfilePage.Text` **那条语义一字不改**：限宽换行只在 `wrap` 时给、自适应字号只在 `autoFit` 时给
        /// （**别用 `SetFontSize(px/108)`** —— 那会大 2.7 倍；底层 `MenuDraw.Text` 走的是 `SetGlyphHeight`）。</summary>
        static Label Text(RowCtx c, Transform p, string s, PxRect r, Color col, string n, float px, int off,
                          bool autoFit = false, float autoMinPx = 0f, bool alignLeft = false, bool wrap = false)
        {
            // 🔴 2026-10-03：这条守卫原来**只判横轴**、而且是**第二份「整块在框外」**（另一份在
            //   `MenuDraw.ClipRect` 里）⇒ 收口到唯一那一份，顺带把**纵轴**也覆盖上
            //   （压在视口上/下的那几行文字以前照样建出来，靠 `Label` 自己不裁 ⇒ 会画到视口外）。
            //   ⚠️ 它**只管「建不建」**，不真裁 —— 「`Text` 要不要按矩形裁掉一半」是另一条账（本件不做）。
            if (!MenuDraw.ClipRect(r, c.Clip, out _)) return null;
            var lb = MenuDraw.Text(p, r, s, col, n, px, c.Q + off);
            if (lb != null)
            {
                if (wrap) lb.SetWrapWidth(LayoutSpace.Px(r.W));
                if (autoFit && px > autoMinPx)
                    lb.SetAutoFitBox(LayoutSpace.Px(r.W), LayoutSpace.Px(r.H), autoMinPx, px);
                if (alignLeft) MenuDraw.AlignLeft(lb, r);
            }
            return lb;
        }

        /// <summary>透明命中区 + `WindowButton`。🆕 2026-10-03：`c.Clip` 也传下去 —— 判据 = 原版
        /// `RectMask2D` 的**射线那一面**（框外的点判不中任何东西）⇒ 滚出视口的行**点不到**、
        /// 压在视口边上的命中区**截到视口内**（`MenuDraw.Hit` 转调 `ClipRect`，唯一一份求交）。</summary>
        static Transform Hit(RowCtx c, Transform p, string n, PxRect r, int off, System.Action onClick,
                             ImageQuad target = null, string art = null, string hoverArt = null)
        { return MenuDraw.Hit(p, n, r, c.Q + off, onClick, target, art, hoverArt, clip: c.Clip); }

        // ============================================================ 一行

        /// <summary>一行 = 原版 `Instantiate(logPrefab)` 出来的那棵树（普查那张表逐件照抄）。</summary>
        public static Transform Build(RowCtx ctx, Transform parent, PxRect r, BattleLogData.Match m)
        {
            var row = Node(parent, "Match Log", r);
            Nine(ctx, row, ArtRowBg, r, RowBorder, "Background", L_Bg);

            // `Result`（`Victory` / `Defeat` / `Draw`）—— 文案走 `CardText.Phrase`（与结算面板同一个源）。
            // ⚠️ 原版还会按结果换**字体材质**（`victoryMaterial`/…）—— 那三个指针是**跨文件引用**
            //    （`m_FileID=5`），本地解不出（§D3）⇒ 我们用预制体自己的字色（白），**如实记着**。
            var resR = UguiRect.Child(r, ResultA, ResultA, ResultP, ResultPos, ResultSz);
            Text(ctx, row, BattleLogData.ResultText(m.Result), resR, Color.white, "Result", ResultPx, L_Text,
                 true, ResultAutoMin);

            var modeR = UguiRect.Child(r, ModeA, ModeA, ModeP, ModePos, ModeSz);
            // ⚠️ 原版 `Mode` 是 `Left/Top`，我们这套文字只能「居中/左/右」三档 ⇒ 实际是 Left/**Middle**
            //    （§D4 已如实记着，不是漏做）。
            Text(ctx, row, m.Mode ?? "", modeR, Color.white, "Mode", ModePx, L_Text2, true, ModeAutoMin, true, true);

            // 两个圆钮（**图标挂在钮里面**，照原版树：`ReplayButton > replayicon`）
            var repR = UguiRect.Child(r, BtnA, BtnA, UguiRect.P50c, new Vector2(ReplayDx, BtnDy), BtnSz);
            var rep = Node(row, "ReplayButton", repR);
            var repBg = Rect(ctx, rep, ArtBtn, repR, "Image", L_Btn);
            Rect(ctx, rep, ArtReplay, repR, "replayicon", L_BtnIcon);
            // 🆕 A17：原版 `Battle Log Tab>…>Match Log>{ReplayButton,PinButton}` 是 SpriteSwap（普查 §块 5 第 10 行）
            Hit(ctx, rep, "Hit", repR, L_Hit, () => OnReplay(m), repBg, ArtBtn);

            var pinR = UguiRect.Child(r, BtnA, BtnA, UguiRect.P50c, new Vector2(PinDx, BtnDy), BtnSz);
            var pin = Node(row, "PinButton", pinR);
            var pinBg = Rect(ctx, pin, ArtBtn, pinR, "Image", L_Btn);
            var pinIc = Rect(ctx, pin, ArtPin, pinR, "pinicon", L_BtnIcon);
            if (pinIc != null) pinIc.SetTint(m.Pinned ? BattleLogData.PinOn : BattleLogData.PinOff);
            Hit(ctx, pin, "Hit", pinR, L_Hit, () => OnPin(m, pinIc), pinBg, ArtBtn);

            BuildSide(ctx, row, r, m, 0);
            BuildSide(ctx, row, r, m, 1);

            // `Details`（一个透明容器）> `Sword`：中间那把剑
            var det = Node(row, "Details", r);
            var swordR = UguiRect.Child(r, SkullA, SkullA, UguiRect.P50c, SwordPos, SwordSz);
            Rect(ctx, det, ArtSword, swordR, "Sword", L_Art);
            return row;
        }

        /// <summary>半边（0 = `Player Info` 我方 · 1 = `Enemy Info` 敌方）。两份的**每一条数值都不同** —— 照 §A·1。</summary>
        static void BuildSide(RowCtx c, Transform row, PxRect r, BattleLogData.Match m, int side)
        {
            bool mine = side == 0;
            var sideR = UguiRect.Child(r, SideA[side], SideB[side], UguiRect.P50c, Vector2.zero, Vector2.zero);
            var box = Node(row, mine ? "Player Info" : "Enemy Info", sideR);

            // 对齐**逐格不同**（照 §A·1 的「文字」那一列）：我方那三行是 `Right`、我方的 `Score` 是 `Left`、
            // 敌方那三行是 `Left`、敌方的 `Score` 是 `Right` —— 别按「半边」一刀切。
            var heroR = UguiRect.Child(sideR, HeroA[side], HeroA[side], HeroP[side], HeroPos[side], HeroSz[side]);
            TextSide(c, box, mine ? m.OwnHeroName : m.EnemyHeroName, heroR, "Hero Name", HeroPx, L_Text,
                     HeroAutoMin, mine);

            var nameR = UguiRect.Child(sideR, HeroA[side], HeroA[side], HeroP[side], NamePos[side], NameSz[side]);
            TextSide(c, box, mine ? m.OwnName : m.EnemyName, nameR, "Player Name", NamePx, L_Text,
                     NameAutoMin, mine);

            var clanR = UguiRect.Child(sideR, HeroA[side], HeroA[side], HeroP[side], ClanPos[side], ClanSz[side]);
            TextSide(c, box, mine ? m.PlayerClan : m.EnemyClan, clanR, "Alliance Name", ClanPx, L_Text2,
                     ClanAutoMin, mine, true);

            var scoreR = UguiRect.Child(sideR, ScoreA[side], ScoreA[side], ScoreP[side], ScorePos[side], ScoreSz);
            // ⚠️ `Score` 那一格**与外层相反**：我方是 `Left/Middle`、敌方是 `Right/Middle`
            TextSide(c, box, mine ? m.OwnScore : m.EnemyScore, scoreR, "Score",
                     mine ? ScorePx : ScorePxEnemy, L_Text, ScoreAutoMin, !mine);

            var sciR = UguiRect.Child(sideR, ScoreA[side], ScoreA[side], ScoreP[side], ScIcPos[side], ScIcSz);
            Rect(c, box, ArtScoreIcon, sciR, "Score Icon", L_Art, null, true);

            var facR = UguiRect.Child(sideR, FacA, FacA, FacP, FacPos[side], FacSz);
            Rect(c, box, ArtFactionIcon, facR, "Score Icon (1)", L_Art, null, true);

            var skR = UguiRect.Child(sideR, SkullA, SkullA, SkullP, SkullPos[side], SkullSz);
            var skq = Rect(c, box, ArtSkull, skR, "Skulls", L_Art, null, true);
            // 🔴 **2026-09-27 订正**：原来写「`UIFlippable` 的两个值本地读不到 ⇒ 我们不翻」——
            //    值**读得到**（普查 §D2 直读 MB）：我方 `(H0,V0)` · **敌方 `(H1,V0)` ⇒ 敌方那半边水平镜像**。
            //    翻转走 `SetUvRect`（与本工程 `CollectionWindow.BuildStyleArrow` 那条同款，别另造）。
            //    ⚠️ 教训：`menu_dump` 的「字段: …」那一列印的是**字段名清单、不是值** ——
            //    凡照它写「读不到」的格子，先回原始 JSON 看一眼（`资料/已知的坑.md`）。
            if (skq != null && !mine) skq.SetUvRect(new Rect(1f, 0f, -1f, 1f));

            var cntR = UguiRect.Child(sideR, HeroA[side], HeroA[side], HeroP[side], SkullCntPos[side], SkullCntSz);
            Text(c, box, "x" + (mine ? m.OwnSkulls : m.EnemySkulls), cntR, Color.white, "skullCounter",
                 SkullCntPx, L_Text, true, SkullCntAutoMin);

            if (!mine)
            {
                // `UIGenericEventCatcher` + `NonDrawingGraphic`（**不可见**）⇒ 我们建一个透明命中区
                var catR = UguiRect.Child(sideR, CatcherA, CatcherA, UguiRect.P50c, CatcherPos, CatcherSz);
                Node(box, "GameObject", catR);
                Hit(c, box, "EnemyClickHit", catR, L_Hit, OnEnemyClicked);
            }
        }

        /// <summary>半边里的一段字。`right` = **右对齐**（原版我方那三行是 `Right/Middle`）。
        /// 右对齐走 `MenuDraw.AlignRight`（同一处实现，别另写一份）。</summary>
        static Label TextSide(RowCtx c, Transform parent, string s, PxRect r, string name, float px, int q,
                              float autoMin, bool right, bool wrap = false)
        {
            var lb = Text(c, parent, s, r, Color.white, name, px, q, true, autoMin, !right, wrap);
            if (lb != null && right) MenuDraw.AlignRight(lb, r);
            return lb;
        }

        // ============================================================ 三个动作（原版各自的落点，两扇窗共用）

        /// <summary>`ClickReplayButton` → 原版 `PlayerDataManager.RecoverMatchRecording(recordingIndex, …)`
        /// ⇒ 从**服务器**取回录制并重演。
        /// 🆕 **2026-09-27：我们改走本地录像**（用户当天拍板「做，我们需要录像」）——
        /// 原版那套在 PlayFab 上（判据 → `资料/普查产出_0927/回放_入口与数据链.md`），
        /// **这是加功能、不是复刻**。链路：读文件 → 挂到 `ReplayStore.PendingPlay` → 切战场 →
        /// 战场起来时 `BattleDriver.Start` 看到它就直接 `PlayReplay`。**没有录像就如实出声**。</summary>
        static void OnReplay(BattleLogData.Match m)
        {
            if (string.IsNullOrEmpty(m.ReplayFile))
            {
                Debug.Log("[BattleLog] 点回放：**这一局没有录像**（`Match.ReplayFile` 是空的）。"
                        + "⚠️ 录像只从 2026-09-27 起录 —— 之前打的局没有。**没有静默**，点了就说这一句。");
                return;
            }
            var rec = ReplayStore.Load(m.ReplayFile);
            if (rec == null) return;                 // `Load` 自己已经出过声（版本不认识 / 文件没了 / 解不出）
            string scene = rec.start != null && !string.IsNullOrEmpty(rec.start.arena)
                         ? rec.start.arena : "Battle";
            ReplayStore.PendingPlay = rec;
            Debug.Log($"[BattleLog] 放录像 `{m.ReplayFile}` ⇒ 切战场 `{scene}`"
                    + $"（{rec.actions.Count} 条动作 · {rec.savedAt}）");
            UnityEngine.SceneManagement.SceneManager.LoadScene(scene);
        }

        /// <summary>`ClickPinButton` → 原版先 `pinButton.interactable=false`，再 `PinMatchRecording(…, !pinned, …)`，
        /// 回来 `SetPinState` 把 `pinButton.image.color` 设成 **未钉 (1,1,1,1) / 已钉 (0,1,0,1)**（绿）。
        /// ⚠️ 原版那一步要**上传服务器**；我们只有本地那一份 ⇒ 只在本地翻（如实出声）。</summary>
        static void OnPin(BattleLogData.Match m, ImageQuad icon)
        {
            m.Pinned = !m.Pinned;
            if (icon != null) icon.SetTint(m.Pinned ? BattleLogData.PinOn : BattleLogData.PinOff);
            Debug.Log("[BattleLog] 这一局" + (m.Pinned ? "**已钉住**（钮变绿）" : "**取消钉住**")
                    + " —— ⚠️ 只在本地：原版 `PinMatchRecording` 是上传服务器 + 回放要不要留档由它管。");
        }

        /// <summary>点敌方那一块 → 原版 `BattleLogPlayerDisplay.OnPlayerClicked` 开他的档案
        /// （`ChatPlayerOptionsPanel.ShowProfile` 那一路）。我们本地对局的对手是 bot ⇒ **出声**。</summary>
        static void OnEnemyClicked()
        {
            Debug.Log("[BattleLog] 点敌方那一块（原版开**他的**玩家档案窗 `PlayerProfileMenu`）——"
                    + " 本地对局的对手是 bot、联机局对面也不在本地 ⇒ 开不了别人的档案。");
        }
    }
}
