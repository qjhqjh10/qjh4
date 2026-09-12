// BattleDriver.cs — 规则引擎 ←→ 表现层的**唯一连接点**
//
// 对应 `CardEffects` 在特效那边的角色：`CardPresentation` 不知道规则，`RuleEngine` 不知道画面，
// 两边都只跟这里打交道。
//
// 职责：
//   · 开一局（两个阵营的牌组）→ 把引擎状态同步成卡牌视图
//   · 玩家输入：拖手牌上场（**费用不够拖不上去**）/ 点自己的单位选中 / 点敌方单位攻击 / 结束回合
//   · 对手回合：跑 `SimpleAI`，每步之间留个延时，让玩家看得清
//   · HUD：回合归属、能量、结束回合按钮、胜负
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CardPresentation
{
    public class BattleDriver : MonoBehaviour
    {
        // ---- 场景引用（由 BattleScene 建好）----
        public Camera cam;
        public BoardLayout playerBoard;
        public BoardLayout enemyBoard;
        public HandLayout hand;
        public CardInteraction interaction;
        public Transform boardRoot;
        public BattleBackdrop backdrop;
        /// <summary>攻击方式选择器（原版 `Drag Attack Selector`）。没有就退化成无按钮（自检里能空跑）</summary>
        public AttackSelector selector;
        /// <summary>选目标反馈：准星 + 弧线（原版 `NoCanvas2D/Attack Target Reticle`）。没有就只靠卡面高亮</summary>
        public TargetReticle reticle;
        /// <summary>技能卡面板（原版 `ActiveSkillDesc`）。没有就不显示技能详情</summary>
        public SkillPanel skillPanel;

        // ---- 阵营配色（只用在**没有阵营卡框图**时的占位卡面上；有原版卡框就轮不到它）----
        public static readonly Color EmberColor = new Color(0.85f, 0.38f, 0.25f);
        public static readonly Color TideColor = new Color(0.28f, 0.55f, 0.85f);

        /// <summary>
        /// 先挑的两个**原版阵营**（2026-09-12 用户指定：13 个一次铺开风险太大，先做通两个）。
        /// `Start()` 不带参数就会用这两个开局。
        /// </summary>
        public const string DefaultFactionA = "Ultramarines";
        public const string DefaultFactionB = "Goff";

        /// <summary>
        /// 阵营 → 占位卡面底色。**只有卡框图缺了才用得上**（`CardArt.Frame(faction)` 取到图就整卡换原版三层）。
        /// 我们自己那两个阵营的色是**我们挑的**；原版这 13 个是按阵营主色取的近似值（**不是原版数值**，
        /// 原版是整张卡框图，没有「底色」这个字段）。
        /// </summary>
        public static Color FactionColor(string faction)
        {
            switch (faction)
            {
                case StarterCards.EmberFaction: return EmberColor;
                case StarterCards.TideFaction:  return TideColor;
                case "Ultramarines":    return new Color(0.20f, 0.38f, 0.78f);
                case "Goff":            return new Color(0.35f, 0.62f, 0.24f);
                case "SaimHann":        return new Color(0.85f, 0.75f, 0.30f);
                case "AstraMilitarum":  return new Color(0.45f, 0.50f, 0.33f);
                case "BlackLegion":     return new Color(0.30f, 0.30f, 0.34f);
                case "DarkAngels":      return new Color(0.22f, 0.48f, 0.30f);
                case "Genestealers":    return new Color(0.45f, 0.30f, 0.62f);
                case "Sautekh":         return new Color(0.20f, 0.65f, 0.60f);
                case "Sororitas":       return new Color(0.72f, 0.22f, 0.25f);
                case "Leviathan":       return new Color(0.58f, 0.42f, 0.62f);
                case "TauEmpire":       return new Color(0.30f, 0.62f, 0.75f);
                case "SpaceWolves":     return new Color(0.55f, 0.62f, 0.72f);
                case "EmperorsChildren":return new Color(0.72f, 0.35f, 0.62f);
                default:                return new Color(0.55f, 0.55f, 0.62f);
            }
        }

        // ---- 状态 ----
        public BattleContext Ctx { get; private set; }
        int _me = 0;
        /// <summary>我是几号玩家（0 基）。结算面板判胜负要用。</summary>
        public int MyIndex { get { return _me; } }
        string _myFaction = DefaultFactionA;
        string _foeFaction = DefaultFactionB;
        /// <summary>这一局的种子。重开时 +1（引擎只用 `System.Random(seed)`，对局可复现）。</summary>
        int _seed = 20260911;
        /// <summary>本局我方用的**存档卡组**（卡组编辑器里当前选中的那套）。null = 没编过 / 读不出来。
        /// **必须留着** —— `Restart()` 要照原样再来一局，不记的话「按 R 再来一局」就变成自动凑的牌了
        /// （和 `_myFaction` 一个道理：那是「这一局才有」之外的状态，跨局要显式带过去）。</summary>
        PlayerDeck _myDeckSrc, _foeDeckSrc;
        /// <summary>开局那句「本局用的是哪副牌 / 多少张没上场」。提示行空着时显示它（见 `SetHint`）。
        /// 空串 = 没什么要交代的。**这是我们加的** —— 原版没有这一行（原版全卡种都能上场）。</summary>
        string _deckNotice = "";
        /// <summary>HUD 是不是已经建过了（**只能建一次**，见 `BuildHud`）</summary>
        bool _hudBuilt;

        readonly Dictionary<int, CardView> _myUnits = new Dictionary<int, CardView>();
        readonly Dictionary<int, CardView> _foeUnits = new Dictionary<int, CardView>();
        readonly List<CardView> _handViews = new List<CardView>();

        Label _turnLabel, _energyLabel, _endTurnLabel, _resultLabel, _hintLabel;
        /// <summary>敌方能量数字（原版 `EnemyMana/ManaText`）</summary>
        Label _foeEnergyLabel;
        EndPanel _endPanel;
        /// <summary>设置面板（原版 `BattleSettingsPanel`）—— **投降按钮就在里面**</summary>
        SettingsPanel _settingsPanel;
        /// <summary>右上角那颗设置按钮（原版 `SettingsBtn`，x[1808.0,1871.9] y[9.2,73.1]）</summary>
        ImageQuad _settingsBtn;
        CardDisplayWindow _cardDisplay;
        /// <summary>卡牌放大展示窗（自检要读它的 Visible / ShownTitle）。</summary>
        public CardDisplayWindow CardDisplay { get { return _cardDisplay; } }
        /// <summary>结算面板（自检要读它的 Visible / ShownSkulls）。</summary>
        public EndPanel End { get { return _endPanel; } }
        /// <summary>这局里**敌方督军降到过的最低生命** —— 结算的骷髅数由它算（规则书:36）。
        /// 生命只会往下走（治疗会回，但「首次得到」不回退），所以取最小值就够，不用记历史。</summary>
        int _foeWarlordMinHp = int.MaxValue;
        Label _handLabel, _myText, _enemyText;
        Label _pileLabel, _foePileLabel;

        // 原版 UI 图（`Resources/Art/ui/`，没有就是 null —— 退回纯文字 HUD）
        ImageQuad _endTurnBg, _energyGem, _energyGemEmpty, _myPlate, _enemyPlate;
        ImageQuad _foeEnergyGem, _foeEnergyGemEmpty;      // 敌方那颗能量水晶（原来**根本没画**）
        ImageQuad _myEnergyPlate, _foeEnergyPlate;        // 水晶底下那块底板 `Card Frame Cost Icon`
        ImageQuad _myQuestJoin, _foeQuestJoin;            // 任务点连到水晶上的小接片
        ImageQuad _myPile, _foePile;
        ImageQuad _myDeckPlate, _foeDeckPlate, _myDeckLight, _foeDeckLight;

        // ---- 牌堆那一套的尺寸（px @1920×1080 → 世界单位，108 px/单位）----
        // 出处：运行时 dump `runtime_ui_dump_Battle_Arena_1.tsv` 里 `PlayerDeck` 的子树
        /// <summary>牌堆底板 `UI_Deck_Background`：`PlayerDeck` 自己的 230×230</summary>
        const float DeckPlatePx = 230f;
        /// <summary>卡背：`Cardback` 的 `sizeDelta` 2.1739 × 3.1364，父节点 scale 100 → 217×314 px</summary>
        const float DeckCardPx = 314f;
        /// <summary>回合灯：`YourTurnImage` 的 anchor 占底板的 9.9%×15% → 矩形 22.8×34.5，
        /// 但贴图 60×59 是 **KEEP_ASPECT** 缩进这个矩形 → 实绘 **23.4×23.4**。
        /// ⚠️ 2026-09-12 改：原来是 34.5（把矩形的高当成了图的高），比原版大 47%。</summary>
        const float DeckLightPx = 23.4f;
        /// <summary>回合灯相对牌堆中心的偏移（px）：anchor 0.8285 / 0.126 在 230×230 底板上折算</summary>
        const float DeckLightDxPx = 75.6f, DeckLightDyPx = -86f;
        static float Px(float px) { return px / 108f; }

        /// <summary>两边牌堆的锚点（归一化）。**判据只有这一份** —— 建、重贴、放灯都用它。
        /// ⚠️ 2026-09-12 改：原来是 (0.845, 0.235 / 0.790)（右侧中段）。原版实测 `PlayerDeck` 230×230
        ///    绝对 x[1615,1845] y[850,1080]（从上）—— **贴着屏幕右下角**，底边正好压在屏幕下沿；
        ///    敌方牌堆对称贴在右上角（`EnemyDeck` 200×200）。
        ///    出处：`战斗界面JSON权威表_0827.md` 的绝对坐标表（和 dump 里 `RightArea` 那一族的锚点一致）。
        ///    换算：x01 = 1730/1920，玩家 y01 = 1 − 965/1080（中心 115 px 从下）。</summary>
        const float MyDeckX01 = 0.90104f, MyDeckY01 = 0.10648f;
        const float FoeDeckX01 = 0.90104f, FoeDeckY01 = 0.90741f;
        /// <summary>敌方牌堆底板小一号（原版 `EnemyDeck` 200×200，我方 230×230）</summary>
        const float FoeDeckPlatePx = 200f;

        /// <summary>END TURN 按钮的中心（归一化）。**判据只有这一份** —— 建按钮、建文字、
        /// 以及换分辨率重贴，三处都读它。
        /// ⚠️ 踩过（2026-09-12）：建的时候改到右侧了，但换分辨率重贴那段**把旧坐标又写了一遍**，
        /// 结果按钮在右边、文字留在右下角（截图抓到的）。</summary>
        const float EndTurnX01 = 0.96263f, EndTurnY01 = 0.57819f;

        // ---- 右侧能量区（原版 `Energy And turn holder` 那一竖排）----
        // **判据只有这一份** —— 建、换分辨率重贴都读它。出处：`资料/战斗规格/战斗重建_0827/战斗界面JSON权威表_0827.md`
        // B 节「能量水晶区」的 chain_rect 绝对坐标（x[1826.3,1900.8] 这种）。
        // 换算：x01 = 中心x/1920，y01 = 1 − 中心y(从上)/1080。
        /// <summary>我方能量水晶 `PlayerMana`：x[1827.8,1903.9] y[517.1,594.1]</summary>
        const float MyEnergyX01 = 0.97180f, MyEnergyY01 = 0.48556f;
        /// <summary>敌方能量水晶 `EnemyMana`：x[1826.3,1900.8] y[249.8,327.4] —— **原来我们压根没画这一颗**</summary>
        const float FoeEnergyX01 = 0.97060f, FoeEnergyY01 = 0.73278f;
        /// <summary>能量底板 `Energy Player`（图 `Card Frame Cost Icon`）：实绘 94.6 × 91.3 px</summary>
        const float EnergyPlateH = 91.3f;
        const float MyEnergyPlateX01 = 0.97352f, MyEnergyPlateY01 = 0.48569f;
        const float FoeEnergyPlateX01 = 0.97232f, FoeEnergyPlateY01 = 0.73292f;
        /// <summary>任务点 `QuestPointsHolder`（97.7²）：我方水晶**下方** x[1817.0,1914.7] y[595.4,693.1]、
        /// 敌方水晶**上方** x[1816.1,1913.8] y[150.2,247.9]。
        /// ⚠️ 2026-09-12 更正：原来写的是 0.52019 / 0.69907 —— 那是把 `BackgroundJoin`（接片）
        /// 的子偏移当成了 holder 的偏移，两个图标都贴在**水晶内侧**。现在按上面的绝对坐标摆。</summary>
        const float QuestPx = 97.7f;
        const float MyQuestX01 = 0.97180f, MyQuestY01 = 0.40347f;
        const float FoeQuestX01 = 0.97133f, FoeQuestY01 = 0.81569f;
        /// <summary>任务点接片 `BackgroundJoin`（35.2×23.9），在水晶与任务点之间</summary>
        const float QuestJoinPx = 23.9f;
        const float MyQuestJoinY01 = 0.43807f, FoeQuestJoinY01 = 0.78200f;
        /// <summary>张数文字离牌堆中心多远（归一化高度）：165 px / 1080。出处见 `BuildHud` 牌堆那段</summary>
        const float DeckLabelDy01 = 165f / 1080f;

        int _selectedSlot = -1;
        /// <summary>定下来的打法（原版 `attackType`）。`None` = 还没选</summary>
        AttackKind _command = AttackKind.None;
        /// <summary>按住哪个单位还没松手（松手=单击开选择器，拖够距离=直接开）</summary>
        int _pressSlot = -1;
        Vector3 _pressWorld;
        float _aiTimer;
        bool _aiThinking;

        /// <summary>AI 每步之间的间隔（秒）—— 太快玩家看不清发生了什么</summary>
        public float aiStepDelay = 0.55f;

        // ==================================================================
        //  回合时钟（原版 `ClockManager` / `Countdown`）
        //  数值**全部有出处**，见每个字段的注释；机制也照反编译的方法体来（不是我们编的）。
        // ==================================================================
        /// <summary>每回合基准时长（秒）。出处：`DefaultScenario.json:19` `clockTimeLimit = 60`
        /// （`bundle_duplicateassetisolationso_assets_all/MonoBehaviour/`）。
        /// ⚠️ **原版按模式覆盖**（`ClockManager__GetTotalTime.c:24-36`）：matchType 80(EventAI)=240 s、
        ///    50(PracticeOffline)=600 s。我们这局是单机对 AI，严格说更接近后者 ——
        ///    但 600 s 等于没有压力，所以**默认取 DefaultScenario 的 60**；要改就改这一行。</summary>
        public float turnSeconds = 60f;
        /// <summary>「缩时」时长（原版 `clockTimeLimitReduced = 10`，同上 :20）。
        /// 触发条件是「上一回合**超时且整回合零动作**且**不是对 AI**」（`EndTurnClick.c:135-152`）——
        /// 我们这局是对 AI，按原版判定**永远不会进缩时**，所以只留字段、不写死逻辑（写注释不写死代码）。</summary>
        public float reducedTurnSeconds = 10f;
        /// <summary>总时长走完后再显示的倒计时秒数（原版 `clockCountdownSec = 15`，同上 :21）。
        /// 这一轮走完就**自动结束回合**（原版 `ClockManager__Update.c:110-141` → `EndTurnClick(true)`，无惩罚）。</summary>
        public float countdownSeconds = 15f;
        /// <summary>剩这么多秒开始催（原版 `timeToHurryUp = 35.0`）。原版是发一句语音
        /// （`DisplayHurryUpChatMessage`，每回合一次）；我们没接音频，**改成数字变色** ——
        /// ⚠️ 变色是**我们挑的表现**，不是原版的做法。</summary>
        public float hurryUpSeconds = 35f;

        /// <summary>本回合还剩多少秒（走表用）。`_clockInCountdown` = 已经进「超时后的 15 秒」那一段</summary>
        float _clockLeft;
        bool _clockInCountdown;
        /// <summary>玩家这个回合做了几个动作 —— 原版缩时判定要用（见 `reducedTurnSeconds`）</summary>
        int _actionsThisTurn;
        Label _clockLabel;

        /// <summary>把自己的手牌索引找出来（落点校验要用）</summary>
        int HandIndexOf(CardView v) { return _handViews.IndexOf(v); }

        /// <summary>
        /// 进 Play 模式自动开一局。
        ///
        /// ⚠️ 两件事必须在这里做：
        ///   1. **重新 `LayoutSpace.Apply(cam)`** —— `LayoutSpace.Cam` 是静态字段，
        ///      **静态状态不进 Play 模式**（编辑器里建场景时设过，运行时是 null）。
        ///      不重设的话所有归一化坐标会退回默认宽高比，超宽屏/4:3 上全部错位。
        ///   2. 卡牌是**运行时生成**的（不烘进场景，所以 Battle.unity 只有 79 KB），
        ///      不调 `Begin()` 打开就是一块空场。
        /// </summary>
        void Start()
        {
            if (cam != null) LayoutSpace.Apply(cam);
            // 背景：场景里存的是建好的 quad，但组件上的私有引用不进序列化，运行时得重绑一次
            if (backdrop != null) backdrop.Build();
            if (Ctx == null) BeginFromDeckLibrary();
        }

        /// <summary>
        /// **按 Play 时的开局**：读卡组编辑器里当前选中的那套 → 开一局。
        ///
        /// 抽成独立方法是为了**自检能走同一条路** —— 批处理下 `AddComponent` 不触发 `Start`，
        /// 不这样的话「卡组库 → 对局」这段连接就永远没被验过（而那正是这一段的意义）。
        /// </summary>
        public void BeginFromDeckLibrary()
        {
            string note;
            var saved = PickSavedDeck(out note);
            Begin(myDeck: saved, deckNote: note);
        }

        /// <summary>
        /// 卡组编辑器里**当前选中的那套**（`DeckLibrary.Current`，落在 `DeckStore` 那个本地文件里）。
        /// 返回 null = 没编过，走自动凑；<paramref name="note"/> 非空 = **存档读不出来**，
        /// 这句人话会被 <see cref="Begin"/> 说在提示行上（`deckNote` 参数）。
        ///
        /// ⚠️ 只读不写；也**不吞错** —— 存档坏了就明说，不能让玩家以为打的是自己编的那副。
        /// </summary>
        public static PlayerDeck PickSavedDeck(out string note)
        {
            var lib = DeckLibrary.Load();
            note = lib.LastError;      // 「还没编过」不算失败：那时 LastError 是 null，库也是空的
            return lib.Current;
        }

        // ==================================================================
        //  开局
        // ==================================================================

        /// <summary>
        /// 开局。
        /// </summary>
        /// <param name="myFaction">我方阵营。null = 不改（默认 <see cref="DefaultFactionA"/>）。
        /// ⚠️ 给了 <paramref name="myDeck"/> 时**以那个卡组的督军阵营为准** —— 督军决定阵营。</param>
        /// <param name="foeFaction">对手阵营。null = 用默认（<see cref="DefaultFactionB"/>），
        /// 但若那正好和我方撞了，会**换一个**（免得开局先打内战，这条是我们挑的）。</param>
        /// <param name="myDeck">我方卡组（卡组编辑器存的那套）。**null = 按卡池自动凑一副**。</param>
        /// <param name="foeDeck">对手卡组。null = 自动凑。</param>
        /// <param name="deckNote">卡组**读不出来**时的人话（`PickSavedDeck` 的 note）。null = 没这回事。</param>
        public void Begin(string myFaction = null, string foeFaction = null, int seed = 20260911,
                          PlayerDeck myDeck = null, PlayerDeck foeDeck = null, string deckNote = null)
        {
            if (myFaction != null) _myFaction = myFaction;
            if (foeFaction != null) _foeFaction = foeFaction;
            _seed = seed;
            _myDeckSrc = myDeck;           // 留着给 `Restart()`
            _foeDeckSrc = foeDeck;

            // ⚠️ **重开一局必须清这个** —— 上一局摆过的槽还占着的话，新一局往那些槽拖会被弹回来
            //（`CardInteraction._placed` 是跨局留着的，它只认识槽号，不认识这是第几局）。
            // 踩到它的地方：`BattleScene` 里第二次 `Begin()` 之后第一张牌就落不下去。
            if (interaction != null) interaction.ClearPlaced();

            // 卡池是**原版那 1131 张**（`cards_engine.json`）—— 不再是 `StarterCards` 那 26 张自设计的。
            // `StarterCards` 还留着：`RuleEngineTest` 里那批规则用例还在用它（那些卡是专门为了
            // 覆盖关键词/触发而设计的，原版卡替不了），而且它是「卡池可以换」这件事的活证明。
            var pool = CardDatabase.Load();
            if (pool.Count == 0)
                Debug.LogError("[Battle] 卡池是空的（`Resources/cards_engine.json` 没加载上）—— 这局没法打");

            // ---- 我方阵营：**玩家编的那副牌说了算** ----
            // 规则书:43「1 督军 + 1 防御卡 + 30 张阵营卡」，`DeckRules.Validate` 也是拿**督军的阵营**
            // 去比每一张卡（`SameFaction`）—— 所以「督军的阵营」就是这副牌的阵营，这里不另立判据。
            if (myDeck != null)
            {
                var wf = WarlordFaction(myDeck, pool);
                if (wf != null) _myFaction = wf;
                else Debug.LogWarning($"[Battle] 卡组「{myDeck.Name}」的督军 `{myDeck.WarlordId}`"
                                    + $" 在卡池里找不到 —— 阵营照旧用 {_myFaction}");
            }
            // 对手：默认 Goff；玩家自己选的就是 Goff 时让开，免得开局先打一场内战。
            // ⚠️ **这是我们挑的** —— 原版由匹配系统配对手，单机没有匹配对象。
            //    只在**没显式指定**对手（`foeFaction == null`）时生效，显式传了就以调用方为准。
            if (foeFaction == null && _myFaction == _foeFaction)
                _foeFaction = _foeFaction == DefaultFactionB ? DefaultFactionA : DefaultFactionB;

            string myNotice;
            var myCards = ResolveDeck(PoolFor(pool, _myFaction), _myFaction, myDeck, seed + 1, "我", out myNotice);
            var foeCards = ResolveDeck(PoolFor(pool, _foeFaction), _foeFaction, foeDeck, seed + 2, "对手", out _);
            // ⚠️ 2026-09-12：`StarterDeck` 现在**会混进能打的战术卡**（原来那开关没实现，自动凑的牌
            //    一张战术都没有，实战里永远看不到战术）。要退回「只有单位卡」就把 `ResolveDeck`
            //    里那一处传 `unitsOnly: true`。

            // 提示行只说**我方**那副 —— 对手那副是自动凑的，不用跟玩家交代
            _deckNotice = myNotice;
            if (string.IsNullOrEmpty(_deckNotice) && !string.IsNullOrEmpty(deckNote))
                _deckNotice = $"卡组存档读不出来（{Short(deckNote, 26)}）—— 本局自动凑了一副";

            // `cardPool: pool` —— `create` 造牌要从**整个卡池**按阵营 + 兵种筛候选
            //（`Create three Ultramarines Vehicles` 那 18 张不可能都在牌库里）。
            // 不传的话造牌会如实报「这一局没有卡池」然后什么都不做。
            Ctx = RuleCore.NewBattle(myCards, foeCards, seed, cardPool: pool);

            BuildHud();

            // 落点合法性**由这里说了算** —— 表现层只问这一个委托
            interaction.CanDropAtSlot = (slot, card) =>
            {
                if (Ctx == null || Ctx.IsOver) return false;
                if (Ctx.Active != _me) return false;                 // 对手回合不能出牌
                int idx = HandIndexOf(card);
                if (idx < 0) return false;
                return RuleCore.CanPlayCard(Ctx, _me, idx, slot) == RuleCodes.OK;
            };
            // 战术卡能落到**敌方半场**（`Deal 3 damage to an enemy` 打的就是敌方单位）——
            // 不接这个引用的话，敌方目标的战术卡拖过去一律弹回来
            interaction.foeBoard = enemyBoard;
            // ⚠️ 先 `-=` 再 `+=`：`Begin()` 会被调多次（重开一局），不清的话每开一局就多挂一份，
            //    落位回调会跑 N 遍（第二遍起 `HandIndexOf` 找不到牌、还会报错刷屏）。
            interaction.OnDeployed -= OnCardDeployed;
            interaction.OnDeployed += OnCardDeployed;
            // 轻点卡牌 → 开关展示窗（原版 `BasicCardUI.ToggleOpenCardDisplayOnTouch`）
            interaction.OnTapped -= OnCardTapped;
            interaction.OnTapped += OnCardTapped;

            RuleCore.BeginTurn(Ctx);       // 先手第 1 回合：能量 2、抽 1
            ResetClock();                  // 第 1 回合的表也得上（原版 `ClockManager.StartTimer`）
            RefreshAll();
            UpdateHud();
            SetHint("");                   // 提示行空着时显示开局那句「本局用的是哪副牌」
        }

        /// <summary>
        /// **投降**（原版 `BattleResult.Forfeit`）：立刻判对手胜，不看督军血量。
        /// 规则在 `RuleCore.Forfeit`（判过了不再改）。UI 入口将来挂在设置面板上，
        /// 现在**键盘 `F`** 也能投 —— 自检里走的是这个公开方法。
        /// </summary>
        /// <summary>
        /// 设置面板那一路的输入。返回 true = **这一帧的点击被它接管了**，回合逻辑别再处理。
        ///
        /// ⚠️ 只能在这里调 `ClickedThisFrame()` —— 它是 latch（按一次只算一次），
        ///    在 Update 里先调一次，回合那段就再也收不到点击了。所以：
        ///    · 面板**开着**：无条件接管（模态）
        ///    · 面板关着：**只有指针在设置按钮上**才接管，其余一律放行
        /// </summary>
        bool HandleSettings()
        {
            if (_settingsPanel != null && _settingsPanel.Visible)
            {
                if (ClickedThisFrame())
                {
                    var w = WorldPointer();
                    if (_settingsPanel.HitResign(w)) { _settingsPanel.Hide(); Forfeit(); return true; }
                    if (_settingsPanel.HitClose(w)) { _settingsPanel.Hide(); return true; }
                    // 点面板别处：什么都不做（但不穿透到棋盘）
                }
                return true;
            }
            if (_settingsBtn == null) return false;
            if (!_settingsBtn.Contains(WorldPointer())) return false;
            if (ClickedThisFrame()) _settingsPanel.Show();
            return true;
        }

        /// <summary>自检用：设置面板 / 设置按钮</summary>
        public SettingsPanel Settings { get { return _settingsPanel; } }
        /// <summary>自检用：模拟点右上角设置按钮。
        /// ⚠️ **不能要求指针在按钮上** —— 批处理下没有鼠标，`WorldPointer()` 是个死点，
        ///    真实输入那条路（`HandleSettings`）才需要判指针，自检这条只验「按下去会开」。</summary>
        public bool SimulateOpenSettings()
        {
            if (_settingsPanel == null) return false;
            _settingsPanel.Show();
            return _settingsPanel.Visible;
        }

        /// <summary>自检用：设置按钮本身是好的吗？—— 贴图对不对、它自己的命中矩形认不认自己</summary>
        public bool SettingsBtnReady
        {
            get
            {
                return _settingsBtn != null && _settingsBtn.Texture != null
                    && _settingsBtn.Texture.name == "UI_Settings_Icon"
                    && _settingsBtn.Contains(_settingsBtn.transform.position);
            }
        }

        public void Forfeit()
        {
            if (Ctx == null || Ctx.IsOver) return;
            RuleCore.Forfeit(Ctx, _me);
            RefreshAll();
            UpdateHud();
        }

        /// <summary>
        /// 重开一局（结算面板上那句「按 R 再来一局」就是它）。
        /// 阵营不变，**种子 +1** —— 同一副牌、不同的抽牌顺序，不然每次重开都一模一样。
        /// ⚠️ **卡组也要原样带过去**（`_myDeckSrc`）：不带的话重开一局就变成自动凑的牌了，
        ///    玩家会以为自己在打自己编的那副（2026-09-12 接卡组库时撞到的）。
        /// </summary>
        public void Restart()
        {
            if (_endPanel != null) _endPanel.Hide();     // 上一局的结算面板先收掉（HUD 复用，不清会叠着）
            Begin(_myFaction, _foeFaction, _seed + 1, _myDeckSrc, _foeDeckSrc);
        }

        /// <summary>
        /// 这个阵营的卡池在哪。
        /// 我们自己设计的两套（`Ember` / `Tide`）**不在原版卡池里** —— 它们由 `StarterCards` 造，
        /// 是专门为覆盖关键词/触发而设计的（`RuleEngineTest` 里那批用例靠它们）。
        /// 其余阵营一律走原版卡池。
        /// </summary>
        static List<CardDef> PoolFor(List<CardDef> pool, string faction)
        {
            if (faction == StarterCards.EmberFaction || faction == StarterCards.TideFaction)
                return StarterCards.Of(faction);
            return pool;
        }

        /// <summary>
        /// 一方用哪副牌：**给了合法卡组就用它，否则按卡池自动凑一副**（并**说清楚为什么**）。
        /// 不合法/凑不出来都退回自动凑 —— 宁可打一局「不是你要的那副」，也不能开不了局。
        /// </summary>
        /// <param name="notice">给**画面提示行**的一句人话：用的是哪副牌 / 为什么退了。
        /// 走自动凑时是空串（那是默认行为，不用交代）。调用方只显示己方那句。</param>
        static List<CardDef> ResolveDeck(List<CardDef> pool, string faction, PlayerDeck saved,
                                         int seed, string who, out string notice)
        {
            notice = "";
            if (saved != null)
            {
                var err = DeckRules.Validate(saved, id => CardDatabase.Find(pool, id));
                if (err == DeckError.None)
                {
                    var skipped = new List<string>();
                    var list = DeckBuilder.FromDeck(pool, saved, skipped);
                    if (skipped.Count > 0)
                        Debug.LogWarning($"[Battle] {who}的卡组「{saved.Name}」里有 {skipped.Count} 张"
                                       + "**引擎还不能结算、上不了场**的卡，已丢掉："
                                       + string.Join("、", Limit(skipped, 6).ToArray())
                                       + " —— 防御卡与「效果解析不了」的战术卡，这是**已知**的，不是 bug");
                    if (list.Count >= 2)
                    {
                        // 这句是要**玩家**看到的：这副牌没有全上场，别以为打的是自己编的那 30 张。
                        // 2026-09-12 起 `FromDeck` **会收战术卡**了（能完整解析的那些），
                        // 所以丢掉的不再是「所有战术卡」，而是「防御卡 + 解析不了的战术卡」—— 措辞跟着改，
                        // 不然会冤枉一批其实能打的牌。
                        notice = $"本局用你编的「{Short(saved.Name, 14)}」"
                               + (skipped.Count > 0
                                  ? $"·{skipped.Count} 张（防御卡 / 效果本版解析不了的战术卡）没上场" : "");
                        return list;
                    }
                    Debug.LogError($"[Battle] {who}的卡组「{saved.Name}」展开之后只剩 {list.Count} 张，打不了");
                    notice = $"你的卡组「{Short(saved.Name, 14)}」展开后只剩 {list.Count} 张能上场"
                           + " —— 本局退回自动凑的一副";
                }
                else
                {
                    Debug.LogError($"[Battle] {who}的卡组「{saved.Name}」不合法（{DeckRules.Describe(err)}）");
                    notice = $"你的卡组「{Short(saved.Name, 14)}」不合法（{DeckRules.Describe(err)}）"
                           + " —— 本局退回自动凑的一副";
                }
                Debug.LogWarning($"[Battle] {who}退回**按卡池自动凑**的一副（阵营 {faction}）");
            }

            // `unitsOnly: false` —— 自动凑的牌组也带**能打的战术卡**（约占 1/3）。
            // ⚠️ 2026-09-12 之前那个开关是**没实现的**，所以自动凑的牌一张战术都没有，
            //    实战里永远看不到战术卡（引擎自检全绿 ≠ 打起来会用到）。
            return DeckBuilder.StarterDeck(pool, faction, DeckBuilder.ClassicDeckSize,
                                          new System.Random(seed), unitsOnly: false);
        }

        /// <summary>
        /// 这副牌的**阵营 = 它督军的阵营**。查不到（没督军 / 督军不在池子里）返回 null，调用方别动阵营。
        /// 判据和 `DeckRules.Validate` 里那条 `SameFaction(卡.阵营, 督军.阵营)` 是同一条 ——
        /// 只是这里把它用在「开局用哪个阵营」上。
        /// </summary>
        static string WarlordFaction(PlayerDeck d, List<CardDef> pool)
        {
            if (d == null || string.IsNullOrEmpty(d.WarlordId)) return null;
            var w = CardDatabase.Find(pool, d.WarlordId);
            return (w != null && !string.IsNullOrEmpty(w.Faction)) ? w.Faction : null;
        }

        /// <summary>截断到 <paramref name="n"/> 个字（提示行**不换行**，太长会横着铺出屏幕）。
        /// 卡组名是玩家自己起的，长度不可控。</summary>
        static string Short(string s, int n)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= n ? s : s.Substring(0, n) + "…";
        }

        static List<string> Limit(List<string> src, int n)
        {
            var outList = new List<string>();
            for (int i = 0; i < src.Count && i < n; i++) outList.Add(src[i]);
            if (src.Count > n) outList.Add("…");
            return outList;
        }

        /// <summary>手牌被**轻点**了（按下→松开几乎没动）。原版这个动作就是开关卡牌展示窗。</summary>
        void OnCardTapped(CardView card)
        {
            if (_cardDisplay == null || card == null) return;
            _cardDisplay.Toggle(card.Data);
        }

        void OnCardDeployed(CardView card, int slot)
        {
            if (_cardDisplay != null) _cardDisplay.Hide();   // 这张牌已经上场了，展示窗别留着
            int idx = HandIndexOf(card);
            if (idx < 0)
            {
                Debug.LogError("[Battle] 落位回调找不到这张牌的手牌索引 —— 引擎和画面不同步了");
                return;
            }

            // 战术卡：**不落格位** —— 它打出去就没了（效果已经结算完），视图直接销毁。
            // 单位卡才走下面「从手牌变成场上单位」那条路。
            bool tactic = !card.Data.isUnit;
            int code = RuleCore.PlayCard(Ctx, _me, idx, slot);
            if (code != RuleCodes.OK)
            {
                Debug.LogError($"[Battle] 引擎拒绝了这次落位（{RuleCodes.Describe(code)}）—— "
                             + "校验委托和实际出牌用的不是同一份判据");
                return;
            }

            _handViews.Remove(card);
            if (tactic)
            {
                Kill(card.gameObject);          // 批处理下 Destroy 不生效，`Kill` 会走 DestroyImmediate
                RefreshAll();
                UpdateHud();
                AutoEndTurnIfStuck();
                return;
            }

            // 这张卡从手牌变成场上单位：视图也搬过去，别重建（重建会丢落位动画）
            card.SetData(ToCardData(_ctx_CurrentUnit(slot), _myFaction));
            _myUnits[slot] = card;
            card.transform.SetParent(boardRoot, true);

            // 登场特效**不在这儿播** —— 引擎在 `PlayCard` 里已经发了一条 Deploy 事件，
            // 下面这次 `RefreshAll()` 会把它翻译成特效（表现层只有那一个出口）。
            // 这样 AI 出的牌也带着卡名，两边走的是同一条路。
            RefreshAll();
            UpdateHud();
            AutoEndTurnIfStuck();
        }

        UnitState _ctx_CurrentUnit(int slot)
        {
            return Ctx.Players[_me].Board[slot];
        }

        // ==================================================================
        //  每帧
        // ==================================================================

        void Update()
        {
            if (Ctx == null) return;

            // 事件时间线：每帧推 —— 动作才有节奏，不是同一帧全点着
            AdvanceTimeline(Time.deltaTime);

            if (Ctx.IsOver)
            {
                UpdateHud();
                // 结算面板上写着「按 R 再来一局」—— 那句话原来**没有任何代码接**
                //（2026-09-12 发现的：面板承诺了一件事，什么都没发生）。补上。
                if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) Restart();
                return;
            }

            // 设置面板 / 设置按钮：**两个回合都能用**（原版随时能开）
            if (HandleSettings()) { UpdateHud(); return; }

            TickClock(Time.deltaTime);

            if (Ctx.Active == _me) DrivePlayerTurn();
            else DriveAiTurn();

            UpdateHud();
        }

        /// <summary>轮到玩家时把表拨回去（原版 `ClockManager.StartTimer` 的等价物）。</summary>
        void ResetClock()
        {
            _clockLeft = turnSeconds;
            _clockInCountdown = false;
            _actionsThisTurn = 0;
            UpdateClockLabel();
        }

        /// <summary>走表。只有**玩家的回合**走（原版时钟也只给行动方看）。</summary>
        void TickClock(float dt)
        {
            if (Ctx == null || Ctx.IsOver || Ctx.Active != _me) return;
            if (_clockLeft <= 0f) return;

            _clockLeft -= dt;
            if (_clockLeft <= 0f)
            {
                if (!_clockInCountdown)
                {
                    // 总时长走完 → 换成那 15 秒倒计时（原版 `ClockManager__Update.c:110-141`）
                    _clockInCountdown = true;
                    _clockLeft = countdownSeconds;
                }
                else
                {
                    // 倒计时也走完 → **自动结束回合**，没有额外惩罚（原版 `EndTurnClick(timeOutFlag=true)`）
                    _clockLeft = 0f;
                    UpdateClockLabel();
                    EndPlayerTurn();
                    return;
                }
            }
            UpdateClockLabel();
        }

        /// <summary>把秒数写到 END TURN 按钮里那行字上。
        /// ⚠️ 位置是**我们挑的**（原版 `ClockManager.clockText` 是 `Clock/TurnBtn` 子树里的一个 TMP，
        /// dump 里只列到 `TurnText` 和那个空节点，取不到它的 rect）；写法 m:ss / 倒计时直接写秒数。</summary>
        void UpdateClockLabel()
        {
            if (_clockLabel == null) return;
            int sec = Mathf.CeilToInt(Mathf.Max(0f, _clockLeft));
            _clockLabel.SetText(_clockInCountdown ? sec.ToString()
                                                  : $"{sec / 60}:{sec % 60:00}");
            _clockLabel.SetColor(_clockInCountdown ? new Color(1f, 0.35f, 0.30f)
                               : sec <= hurryUpSeconds ? new Color(1f, 0.72f, 0.30f)
                               : new Color(0.85f, 0.88f, 0.95f));
        }

        // ---- 玩家回合 ----

        void DrivePlayerTurn()
        {
            if (cam == null) return;

            // 正在拖手牌时不接「选单位/打人」的点击 —— 那一下是 `CardInteraction` 的
            //（两边都读同一个鼠标，不挡的话拖牌时会顺带选中底下的单位）
            if (interaction != null && interaction.IsDragging)
            {
                // 手牌拖拽优先 —— 两边读同一个鼠标，不挡的话拖牌时会顺带指挥底下的单位
                CancelCommand();
                return;
            }

            Vector3 world = WorldPointer();

            // ① **选择器开着**：只做两件事 —— 喂指针（压着谁就放大 1.3 倍 + 亮黄圈）、点一下定下来
            if (selector != null && selector.Visible)
            {
                selector.UpdatePointer(world);
                if (ClickedThisFrame())
                {
                    var hot = selector.Hovered;
                    if (hot != AttackKind.None) CommitCommand(hot);
                    else if (!selector.ContainsBar(world)) ClearSelection();   // 点槽外 = 取消
                }
                return;
            }

            // ② **按住自己的单位拖出去** → 弹出选择器
            //    阈值 0.085 屏高（≈92 px @1080p）来自原版 `accumulatedDragForMinDistance`
            if (_pressSlot >= 0)
            {
                if (!PointerDown())
                {
                    // 松手时还没拖够 → 当成一次**单击**，照样弹。
                    // 原版 mobile 走的是长按（`battle.gd` 的 `_board_hold_timeout`），
                    // 鼠标上单击更顺手，而且原版短按在那边是「开卡展窗」，我们还没有那个窗
                    int s = _pressSlot;
                    _pressSlot = -1;
                    OpenCommand(s);
                    return;
                }
                if ((world - _pressWorld).magnitude > AttackSelector.DragThresholdWorld)
                {
                    int s = _pressSlot;
                    _pressSlot = -1;
                    OpenCommand(s);
                }
                return;
            }

            // ③ 正在选目标 → **每帧**把准星挪到指针压着的那个合法目标上（原版「我现在指着谁」）
            //    放在 `ClickedThisFrame` 之前 —— 它是持续反馈，不是只在点击那一下更新
            if (_selectedSlot >= 0 && _command != AttackKind.None)
            {
                UpdateReticle(world);
                // 技能卡面板：指针按在面板上会铺蓝色那层（原版 `LightPressed`）
                if (skillPanel != null && skillPanel.Visible) skillPanel.SetPointer(world, PointerDown());
            }

            if (!ClickedThisFrame()) return;

            // ③ 结束回合按钮（有原版按钮底图就按图判，没有就按文字判）
            bool onEndTurn = _endTurnBg != null ? _endTurnBg.Contains(world)
                                                : (_endTurnLabel != null && _endTurnLabel.Contains(world));
            if (onEndTurn)
            {
                EndPlayerTurn();
                return;
            }

            // ④ 已经定好打法 → 这一下是选目标
            if (_selectedSlot >= 0 && _command != AttackKind.None)
            {
                int foeSlot = HitSlot(_foeUnits, world);
                if (foeSlot >= 0) { Resolve(_command, foeSlot); return; }
                ClearSelection();
                return;
            }

            // ⑤ 点自己的单位 → **先记下来**，松手（单击）或拖够距离再弹选择器
            int pick = HitSlot(_myUnits, world);
            if (pick >= 0) { _pressSlot = pick; _pressWorld = world; }
        }

        static bool PointerDown()
        {
            return Mouse.current != null && Mouse.current.leftButton.isPressed
                || Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed;
        }

        /// <summary>手牌开始拖了 → 把指挥状态收干净（别让它挂着等松手）</summary>
        void CancelCommand()
        {
            if (_selectedSlot >= 0 || _pressSlot >= 0 || (selector != null && selector.Visible))
                ClearSelection();
        }

        /// <summary>
        /// 弹出**攻击方式选择器**（原版的 `Drag Attack Selector`）。
        ///
        /// 给几项由引擎说了算：
        ///   · 近战 —— 近战攻击力 &gt; 0
        ///   · 远程 —— 远程攻击力 &gt; 0（**这才是原版的规则**：玩家自己选，不是我们按数值替他选。
        ///     以前那套「远程攻击力更高就自动用远程」是权宜之计，现在退回成**只给 AI 用**）
        ///   · 主动技能 —— 卡上有 `Ability:` 且 `CanStartAbility` 放行
        /// 一个都没有就不弹，直接说清楚为什么。
        /// </summary>
        void OpenCommand(int slot)
        {
            ClearSelection();

            var u = Ctx.Players[_me].Board[slot];
            if (u == null) return;

            if (u.Exhausted) { SetHint(CardText.Phrase("THIS UNIT ALREADY ACTED")); return; }
            if (u.IsStunned) { SetHint(CardText.Phrase("STUNNED")); return; }

            bool melee = RuleCore.FieldAttack(Ctx, _me, u, false) > 0;
            bool ranged = RuleCore.FieldAttack(Ctx, _me, u, true) > 0;
            bool skill = u.HasAbility && RuleCore.CanStartAbility(Ctx, _me, slot) == RuleCodes.OK;

            if (!melee && !ranged && !skill)
            {
                SetHint(CardText.Phrase("THIS UNIT CANNOT ACT"));
                return;
            }

            _selectedSlot = slot;
            _myUnits[slot].SetHighlight(CardHighlightState.Selected);

            var opts = new List<AttackSelector.Option>();
            if (melee) opts.Add(new AttackSelector.Option { Kind = AttackKind.Melee, Enabled = true });
            if (skill) opts.Add(new AttackSelector.Option
            {
                Kind = AttackKind.Ability, Enabled = true,
                // 角标是**数值**（原版 `ValueText` 就是个大数字）。完整文字写在中间那条提示行上 ——
                // 原版是弹 `ActiveSkillDesc` 技能卡（名字/费用/描述/可选目标数），我们还没做
                Badge = u.Ability.Amount.ToString(),
            });
            if (ranged) opts.Add(new AttackSelector.Option { Kind = AttackKind.Ranged, Enabled = true });

            if (selector != null)
                selector.Show(opts, CardText.Phrase("CHOOSE ACTION"),
                              // 技能效果写在条**上方** —— 中间那条提示行正好被按钮压住
                              skill ? CardText.Name(u.Name) + ": " + CardText.Effect(u.Ability) : null);
            SetHint("");
        }

        /// <summary>定下打法 → 收选择器 → 把合法目标点亮</summary>
        void CommitCommand(AttackKind kind)
        {
            _command = kind;
            if (selector != null) selector.Hide();

            // 只有「主动技能」才可能有技能卡面板（`u` 非空就意味着这次是放技能）
            var u = kind == AttackKind.Ability ? Ctx.Players[_me].Board[_selectedSlot] : null;

            // 不用选目标的技能（治疗 / 抽牌 / 直接打督军）：选完就放，没有「选谁」这一步，
            // 也就不弹面板（一闪而过等于没有）
            if (u != null && u.Ability != null && !EffectTargets.NeedsPick(u.Ability.Target))
            {
                Resolve(kind, -1);
                return;
            }

            // 点亮合法目标，顺便拿到个数 —— 面板和中间那行提示**共用这一个数**
            int n = HighlightTargets();
            if (u != null) ShowSkillPanel(u, n);
        }

        /// <summary>
        /// 弹技能卡面板（原版 `ActiveSkillDesc`）。内容全从**施放者那张卡的 `Ability`** 来 ——
        /// 面板不认识规则，判据也不在它那儿。
        /// </summary>
        void ShowSkillPanel(UnitState u, int targets)
        {
            if (skillPanel == null || u == null) return;
            // 卡名当技能名 —— 原版有独立的技能名（`NameText` = "Fire Arrow"），我们还没那个字段
            skillPanel.Show(CardText.Name(u.Name), u.Ability, targets);
        }

        /// <summary>把**合法的**目标标出来。返回个数（技能卡面板和提示行**共用这一个数**）</summary>
        int HighlightTargets()
        {
            int n = 0;
            for (int t = 0; t < BoardSpec.Size; t++)
            {
                CardView v;
                bool have = _foeUnits.TryGetValue(t, out v) && v != null;
                bool legal = LegalTargetCode(t) == RuleCodes.OK;

                // 不合法的一律**熄掉底光** —— 不然上个打法点亮的那些会留在卡面上
                if (have) v.SetTargetGem(legal ? GemForCommand() : TargetGem.None);
                if (!legal || !have) continue;

                v.SetHighlight(CardHighlightState.ValidTarget);
                n++;
            }

            string what = _command == AttackKind.Ability ? "ABILITY"
                        : (_command == AttackKind.Ranged ? "RANGED" : "MELEE");
            SetHint(CardText.Phrase(what) + " - " +
                    (n > 0 ? CardText.Phrase("PICK A TARGET") + " (" + n + ")"
                           : CardText.Phrase("NO LEGAL TARGET")));
            return n;
        }

        /// <summary>
        /// 当前打法点亮哪颗数值格的底光。**技能没有那一档** —— 原版 `Base Attack Counters` 下
        /// 只有近战/远程两个 `Highlight`（见 `CardView.SetTargetGem`）。技能靠准星和弧线的金色表达。
        /// </summary>
        TargetGem GemForCommand()
        {
            return _command == AttackKind.Ranged ? TargetGem.Ranged
                 : _command == AttackKind.Ability ? TargetGem.None
                 : TargetGem.Melee;
        }

        /// <summary>
        /// 敌方槽位 `t` 在当前打法下合不合法（返回引擎码）。
        /// **判据只有这一份** —— 点亮合法目标（`HighlightTargets`）和准星（`UpdateReticle`）都调它。
        /// 各写一遍的话迟早自相矛盾：卡亮着、准星却不认。
        /// </summary>
        int LegalTargetCode(int t)
        {
            if (Ctx == null || _selectedSlot < 0 || t < 0 || t >= BoardSpec.Size) return RuleCodes.ErrTarget;
            if (Ctx.Players[1 - _me].Board[t] == null) return RuleCodes.ErrTarget;
            return _command == AttackKind.Ability
                 ? RuleCore.CanUseAbility(Ctx, _me, _selectedSlot, t)
                 : RuleCore.IsValidTarget(Ctx, _me, _selectedSlot, 1 - _me, t,
                                          _command == AttackKind.Ranged);
        }

        /// <summary>
        /// 准星跟着指针走。指针不在**合法**目标上就收起来 ——
        /// 不显示「你正指着一个打不了的人」，那比不显示更误导。
        /// </summary>
        void UpdateReticle(Vector3 world)
        {
            if (reticle == null) return;

            CardView me, foe;
            int t = HitSlot(_foeUnits, world);
            if (t < 0 || LegalTargetCode(t) != RuleCodes.OK ||
                !_foeUnits.TryGetValue(t, out foe) || foe == null ||
                !_myUnits.TryGetValue(_selectedSlot, out me) || me == null)
            {
                reticle.Hide();
                return;
            }

            reticle.Show(me.transform.position, foe.transform.position, _command);
        }

        /// <summary>打出去（攻击或放技能）。`foeSlot` &lt; 0 = 不需要选目标的技能。返回引擎码。</summary>
        int Resolve(AttackKind kind, int foeSlot)
        {
            int slot = _selectedSlot;
            int code = kind == AttackKind.Ability
                     ? RuleCore.UseAbility(Ctx, _me, slot, foeSlot)
                     : RuleCore.DeclareAttack(Ctx, _me, slot, 1 - _me, foeSlot, kind == AttackKind.Ranged);

            if (code != RuleCodes.OK) Debug.Log($"[Battle] 这一手打不出去：{RuleCodes.Describe(code)}");

            // 技能**正在结算** → 面板铺白那层（原版 `ShowActingLight()`），
            // 紧接着 `ClearSelection` 会带着这层白淡出，不是「啪」地消失
            if (kind == AttackKind.Ability && skillPanel != null) skillPanel.SetActing();

            ClearSelection();
            RefreshAll();
            AutoEndTurnIfStuck();
            return code;
        }

        /// <summary>
        /// 写提示行。**传空 = 回到「休息态」那句**（`_deckNotice`：本局用的是哪副牌 / 为什么退了）。
        ///
        /// 为什么要有这个中转：开局那句必须**一直在**，不然玩家一悬停一选目标就被抹掉了 ——
        /// 而「你这副牌有 N 张没上场」正是要他看见的事（红线：不许静默失败）。
        /// 所以把它做成提示行的**默认文字**，游戏过程中的临时提示盖在它上面、用完自动落回来。
        ///
        /// ⚠️ **原版没有这一行**（原版全卡种都能上场，没什么要交代的）—— 这是我们加的。
        /// ⚠️ 唯一要**真的清空**的地方是结算（那时要收干净，不然会从结算面板底下透出来），
        ///    那里直接调 `_hintLabel.SetText("")`。
        /// </summary>
        void SetHint(string s)
        {
            if (_hintLabel == null) return;
            _hintLabel.SetText(string.IsNullOrEmpty(s) ? _deckNotice : s);
        }

        void ClearSelection()
        {
            if (_selectedSlot >= 0)
            {
                CardView v;
                if (_myUnits.TryGetValue(_selectedSlot, out v) && v != null)
                    v.SetHighlight(CardHighlightState.Normal);
            }
            _selectedSlot = -1;
            _command = AttackKind.None;
            _pressSlot = -1;
            if (selector != null) selector.Hide();
            if (reticle != null) reticle.Hide();
            if (skillPanel != null) skillPanel.Hide();
            foreach (var kv in _foeUnits) if (kv.Value != null)
            {
                kv.Value.SetHighlight(CardHighlightState.Normal);
                kv.Value.SetTargetGem(TargetGem.None);    // 底光也要熄
            }
            if (_hintLabel != null) SetHint("");          // 落回「休息态」那句（开局那副牌）
        }

        void EndPlayerTurn()
        {
            ClearSelection();
            RuleCore.EndTurn(Ctx);
            // ⚠️ 换边之后**必须再 BeginTurn** —— 它才是「给当前行动方发能量、抽牌、解疲劳」的那一步。
            //    少了这一步，对手整个回合都是 0 能量，一张牌都出不来（踩过：AI 场上永远只有督军）。
            RuleCore.BeginTurn(Ctx);
            _aiTimer = aiStepDelay;
            _aiThinking = false;
            RefreshAll();
        }

        /// <summary>没牌可出、也没技能可放、也没人能攻击了 → 别让玩家干等，自动结束回合</summary>
        void AutoEndTurnIfStuck()
        {
            if (Ctx.IsOver || Ctx.Active != _me) return;
            if (SimpleAI.NextCardToPlay(Ctx) >= 0) return;
            int aslot, atgt;
            if (SimpleAI.NextAbility(Ctx, out aslot, out atgt)) return;
            int a, tp, ts;
            bool ranged;
            if (SimpleAI.NextAttack(Ctx, out a, out tp, out ts, out ranged)) return;
            Debug.Log("[Battle] 没牌可出、没技能可放也没人能打 —— 自动结束回合");
            EndPlayerTurn();
        }

        // ---- 对手回合 ----

        void DriveAiTurn()
        {
            _aiTimer -= Time.deltaTime;
            if (_aiTimer > 0f) return;
            _aiTimer = aiStepDelay;

            if (!_aiThinking)
            {
                // 先出牌，出到没得出为止；然后一刀一刀打
                // ⚠️ 落点**由引擎给**（`NextPlay` 内部挨个格位问过 `CanPlayCard`）——
                //    写死 `FirstFreeSlot` 的话，战术卡永远落不到它该落的敌方单位上。
                int card, slot;
                if (SimpleAI.NextPlay(Ctx, out card, out slot))
                {
                    if (RuleCore.PlayCard(Ctx, Ctx.Active, card, slot) == RuleCodes.OK)
                    {
                        RefreshAll();       // 登场特效由引擎的 Deploy 事件带出来
                        return;
                    }
                }
                _aiThinking = true;      // 出牌阶段结束，转技能 + 攻击
                return;
            }

            // 技能排在攻击前面：放了技能这个单位就疲劳了，再想攻击也打不了
            int aslot, atgt;
            if (SimpleAI.NextAbility(Ctx, out aslot, out atgt))
            {
                if (RuleCore.UseAbility(Ctx, Ctx.Active, aslot, atgt) == RuleCodes.OK)
                {
                    RefreshAll();
                    return;
                }
            }

            int a2, tp2, ts2;
            bool ranged2;
            if (SimpleAI.NextAttack(Ctx, out a2, out tp2, out ts2, out ranged2))
            {
                if (RuleCore.DeclareAttack(Ctx, Ctx.Active, a2, tp2, ts2, ranged2) == RuleCodes.OK)
                {
                    // 攻击特效也走引擎事件（引擎发 Attack，表现层打在**目标**那一格）
                    RefreshAll();
                    return;
                }
            }

            // 打完了 → 交给玩家
            RuleCore.EndTurn(Ctx);
            _aiThinking = false;
            RuleCore.BeginTurn(Ctx);          // 玩家的新回合
            ResetClock();                     // 又轮到玩家 → 把表拨回去
            RefreshAll();
        }

        // ==================================================================
        //  视图同步
        // ==================================================================

        public void RefreshAll()
        {
            // ① 先把引擎**这一轮发生的事**翻译成特效。
            //    ⚠️ 必须赶在同步视图之前 —— 阵亡单位这一格马上就要空了，
            //       事件里带着格位号，趁现在把它变成世界坐标最省事。
            PlaySignals();

            SyncBoard(_me, _myUnits, true);
            SyncBoard(1 - _me, _foeUnits, false);
            SyncHand();

            UpdateHud();     // 批处理里没有 Update() 循环，HUD 得在这里刷，不然截图上是旧值
        }

        // ==================================================================
        //  特效：引擎事件流 → 特效
        //
        //  以前这里靠**对比同步前后两份战场快照**猜「谁挨打了、谁死了」。
        //  猜得出这两件事，但猜不出「谁发动了技能」「谁触发了效果」——
        //  那两类特效一直接不上，就是因为引擎里没有这两种事件（2026-09-12 补上）。
        //  现在引擎把发生的事全写进 `Ctx.Signals`，这边只做翻译。
        // ==================================================================

        readonly List<BattleEvent> _signalBuf = new List<BattleEvent>();

        /// <summary>自检用：最近一次 drain 里播放过的事件（不接特效也能断言「引擎说了什么」）</summary>
        public readonly List<BattleEvent> LastSignals = new List<BattleEvent>();

        void PlaySignals()
        {
            if (Ctx == null || Ctx.Signals.Count == 0) return;

            _signalBuf.Clear();
            Ctx.DrainSignals(_signalBuf);

            LastSignals.Clear();
            LastSignals.AddRange(_signalBuf);

            // **不立刻全播** —— 按 `EventTiming` 排一条时间线，让动作一段段来。
            // 以前是同一帧把整串一起点着，看着就是「一坨特效」，读不出「谁先谁后」。
            BattleEvent prev = null;
            float t = _clock;
            for (int i = 0; i < _signalBuf.Count; i++)
            {
                var e = _signalBuf[i];
                t += EventTiming.DelayBetween(prev, e);
                _timeline.Add(new PendingSignal { evt = e, at = t });
                t += EventTiming.DurationOf(e.Kind);
                prev = e;
            }
            AdvanceTimeline(0f);        // 延迟为 0 的那几条**这一帧**就播，不用等下一帧
        }

        struct PendingSignal { public BattleEvent evt; public float at; }
        readonly List<PendingSignal> _timeline = new List<PendingSignal>();
        float _clock;

        /// <summary>还没播的事件条数（自检用：推到 0 = 这一段动作演完了）</summary>
        public int TimelinePending { get { return _timeline.Count; } }

        /// <summary>
        /// 推进事件时间线。Play 模式由 `Update` 每帧推；**批处理没有帧循环**，
        /// 自检里由 `BattleScene.Step()` 显式推。
        /// </summary>
        public void AdvanceTimeline(float dt)
        {
            _clock += dt;

            // 结算面板的「开门」视频也吃这个 dt —— 它和事件时间线一样，
            // 真机靠 `Update`、批处理靠 `BattleScene.Step` 手动推（同一个泵，不另开一条路）
            if (_endPanel != null) _endPanel.Advance(dt);

            int guard = 0;
            while (_timeline.Count > 0 && _timeline[0].at <= _clock && guard++ < 256)
            {
                var p = _timeline[0];
                _timeline.RemoveAt(0);
                PlaySignal(p.evt);
            }
        }

        /// <summary>
        /// 丢掉还没播的事件。**只给「一口气跑完几百回合」那种脚本推进用** ——
        /// 那种路径下每回合的几十条事件会堆到一起，一次全点着既慢又看不出什么。
        /// </summary>
        public void DropSignals()
        {
            if (Ctx == null) return;
            Ctx.ClearSignals();
            LastSignals.Clear();
            _timeline.Clear();
        }

        /// <summary>一条引擎事件 → 一个特效。（事件种类和特效名的对应在 `VfxMap` 里）</summary>
        void PlaySignal(BattleEvent e)
        {
            string evt;
            switch (e.Kind)
            {
                case EvtKind.Deploy:  evt = VfxMap.Deploy; break;
                case EvtKind.Attack:  evt = e.Ranged ? VfxMap.AttackRanged : VfxMap.AttackMelee; break;
                case EvtKind.Hit:     evt = VfxMap.Hit; break;
                case EvtKind.Death:   evt = VfxMap.Death; break;
                case EvtKind.Ability: evt = VfxMap.Ability; break;
                case EvtKind.Trigger: evt = VfxMap.Trigger; break;
                default: return;
            }

            // Attack 打在**目标**那一格（原版也是弹着点，不是抬手那一下）；
            // 其余事件都发生在自己那一格
            int owner = e.Kind == EvtKind.Attack ? e.TargetPlayer : e.Player;
            int slot  = e.Kind == EvtKind.Attack ? e.TargetSlot   : e.Slot;
            if (slot < 0) return;                       // 不在场上（比如从牌库直接进弃牌堆）

            var layout = owner == _me ? playerBoard : enemyBoard;
            string faction = owner == _me ? _myFaction : _foeFaction;

            // 濒死的单位已经不在 `_myUnits/_foeUnits` 里了（视图也要等 SyncBoard 才清），
            // 但格位坐标只跟棋盘几何有关 —— 直接问 layout，不依赖视图
            CardEffects.FireEvent(evt, layout.SlotPosition(slot), faction, e.CardId);
        }

        /// <summary>
        /// 销毁视图。
        /// ⚠️ **编辑器模式下 `Object.Destroy` 不生效**（它要等下一帧，而编辑/批处理里没有帧循环），
        ///    必须用 `DestroyImmediate`。踩过：批处理自检里旧的手牌视图全留在画面上，
        ///    看起来像「手牌莫名其妙多出好几张」。
        /// </summary>
        static void Kill(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o);
            else DestroyImmediate(o);
        }

        void SyncBoard(int owner, Dictionary<int, CardView> views, bool mine)
        {
            var board = Ctx.Players[owner].Board;
            var layout = mine ? playerBoard : enemyBoard;
            // ⚠️ 原来这里是「是不是 Tide，不是就是 Ember」的二选一 —— 一接原版阵营就会
            //    全部掉进 Ember 那个色。改成查表（`FactionColor`）。
            var frame = FactionColor(owner == _me ? _myFaction : _foeFaction);

            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var u = board[s];
                CardView v;
                bool hasView = views.TryGetValue(s, out v) && v != null;

                if (u == null)
                {
                    if (hasView) { Kill(v.gameObject); views.Remove(s); }
                    continue;
                }

                if (!hasView)
                {
                    var data = ToCardData(u, owner == _me ? _myFaction : _foeFaction);
                    data.frame = u.IsWarlord ? new Color(0.95f, 0.82f, 0.35f) : frame;   // 督军描金
                    v = CardView.Create(boardRoot, data, $"{(mine ? "My" : "Foe")}Unit_{s}_{u.Name}");
                    views[s] = v;
                }
                else
                {
                    v.SetData(ToCardData(u, owner == _me ? _myFaction : _foeFaction));  // 掉血/疲劳要反映到卡面
                }

                v.SetPose(layout.SlotPosition(s), 0f, layout.placedScale * LayoutSpace.Scale);
                v.SetHighlight(CardHighlightState.Normal);
            }
        }

        /// <summary>
        /// 手牌同步：**引擎的手牌顺序是权威**，视图跟着走。
        /// 先按顺序把现有的视图和引擎手牌一一配对（同名的多个也逐个配），
        /// 配不上的（被打出/超上限弃掉的）销毁，缺的新建。
        /// </summary>
        void SyncHand()
        {
            var h = Ctx.Players[_me].Hand;

            var pool = new List<CardView>(_handViews);
            var ordered = new List<CardView>(h.Count);

            foreach (var card in h)
            {
                CardView match = null;
                for (int i = 0; i < pool.Count; i++)
                {
                    if (pool[i] != null && pool[i].Data.id == card.Name) { match = pool[i]; pool.RemoveAt(i); break; }
                }
                if (match == null) match = CardView.Create(transform, ToCardData(card, _myFaction), "Hand_" + card.Name);
                ordered.Add(match);
            }

            foreach (var dead in pool) if (dead != null) Kill(dead.gameObject);

            _handViews.Clear();
            _handViews.AddRange(ordered);
            interaction.SetCards(new List<CardView>(_handViews));
            RefreshHandPlayable();
        }

        /// <summary>付不起/不能打的牌置灰 —— **判据全部来自引擎**，这里不自己算费用</summary>
        void RefreshHandPlayable()
        {
            bool myTurn = Ctx.Active == _me && !Ctx.IsOver;
            var p = Ctx.Players[_me];
            bool hasSlot = p.HasFreeSlot();

            for (int i = 0; i < _handViews.Count && i < p.Hand.Count; i++)
            {
                if (_handViews[i] == null) continue;
                var card = p.Hand[i];
                bool playable = myTurn && card.IsUnit && card.Cost <= p.Energy && hasSlot;
                _handViews[i].SetHighlight(playable ? CardHighlightState.Normal
                                                    : CardHighlightState.Unplayable);
            }
        }

        CardData ToCardData(UnitState u, string faction)
        {
            return new CardData
            {
                // `id` 保持英文 —— 立绘文件名（`Art/cards/art_<卡名>.png`）认的是它
                id = u.Name,
                title = CardText.Name(u.Name, u.Card != null ? u.Card.NameZh : null),
                cost = -1,
                melee = u.Attack,
                ranged = u.RangedAttack,
                health = u.Health,
                armor = u.Armor,          // 场上是**当前**护甲（会被效果改），原版卡面显示的也是当前值
                keywords = u.Card != null ? FaceText(u.Card) : DescribeKeywords(u),
                isUnit = true,
                frame = FactionColor(faction),
                faction = faction,
                rarity = u.Card != null ? u.Card.Rarity : null,   // 卡框按稀有度分四档
                subtype = u.Card != null ? u.Card.Subtype : "",     // 卡面下方的兵种行（`RaceText`）
            };
        }

        /// <summary>⚠️ public static 是给 `CardFaceProbe`（单卡渲染量尺）用的：卡面数据必须**只有这一条路**。</summary>
        public static CardData ToCardData(CardDef c, string faction)
        {
            return new CardData
            {
                id = c.Name,                       // 同上：英文，给立绘用
                title = CardText.Name(c.Name, c.NameZh),
                cost = c.Cost,
                melee = c.Attack,
                ranged = c.RangedAttack,
                health = c.Health,
                armor = c.KwValue(KeywordTable.Armour),   // 手牌里显示的是卡面印的护甲
                keywords = FaceText(c),
                isUnit = c.IsUnit,
                frame = FactionColor(faction),
                faction = faction,
                rarity = c.Rarity,                 // 卡框按稀有度分四档
                subtype = c.Subtype,               // 卡面下方的兵种行（战术卡没有）
            };
        }

        /// <summary>
        /// 卡面那行小字。
        /// · **原版卡**（`FromOriginalPool`）：写**它自己的效果原文**
        ///   （`DescZh` 有就用中文，没有就英文 `Desc`）—— 原版卡面上印的就是这段字。
        /// · **我们自己设计的 26 张**：写**引擎结算得到的关键词**（`Desc` 对我们那 26 张是风味文字，不是效果）。
        /// 两边都**把引擎结算不了的部分打 `*` 附在后面**（红线：不许静默失败）。
        /// </summary>
        /// <summary>⚠️ public 是给 `CardFaceProbe`（单卡渲染量尺）用的 —— 卡面文案必须**只有这一条路**，
        /// 探针里再写一份迟早不一致。</summary>
        public static string FaceText(CardDef c)

        {
            if (c == null) return "";
            if (!c.FromOriginalPool) return DescribeKeywords(c);

            string body = string.IsNullOrEmpty(c.DescZh) ? c.Desc : c.DescZh;
            string notes = string.Join(" ", UnimplementedNotes(c).ToArray());
            if (string.IsNullOrEmpty(body)) return notes;
            return string.IsNullOrEmpty(notes) ? body : body + "  " + notes;
        }

        /// <summary>关键词 → 卡面上那行小字（只列**引擎真的会结算**的）。
        /// 文案走 `CardText`：拿得到中文字体就是中文，拿不到就是大写英文。</summary>
        static string DescribeKeywords(UnitState u)
        {
            var parts = new List<string>();
            if (u.Has(KeywordTable.Vanguard)) parts.Add(CardText.Keyword(KeywordTable.Vanguard));
            if (u.Has(KeywordTable.Flying)) parts.Add(CardText.Keyword(KeywordTable.Flying));
            if (u.Has(KeywordTable.Stealth)) parts.Add(CardText.Keyword(KeywordTable.Stealth));
            if (u.Has(KeywordTable.LongRange)) parts.Add(CardText.Keyword(KeywordTable.LongRange));
            if (u.Armor > 0) parts.Add(CardText.Keyword(KeywordTable.Armour) + " " + u.Armor);
            if (u.HasShield) parts.Add(CardText.Keyword(KeywordTable.Shield));
            parts.AddRange(EffectNotes(u.Card));
            parts.AddRange(UnimplementedNotes(u));
            return string.Join(" ", parts.ToArray());
        }

        static string DescribeKeywords(CardDef c)
        {
            var parts = new List<string>();
            if (c.Has(KeywordTable.Vanguard)) parts.Add(CardText.Keyword(KeywordTable.Vanguard));
            if (c.Has(KeywordTable.Flying)) parts.Add(CardText.Keyword(KeywordTable.Flying));
            if (c.Has(KeywordTable.Stealth)) parts.Add(CardText.Keyword(KeywordTable.Stealth));
            if (c.Has(KeywordTable.LongRange)) parts.Add(CardText.Keyword(KeywordTable.LongRange));
            if (c.Has(KeywordTable.Armour)) parts.Add(CardText.Keyword(KeywordTable.Armour) + " " + c.KwValue(KeywordTable.Armour));
            if (c.Has(KeywordTable.Shield)) parts.Add(CardText.Keyword(KeywordTable.Shield));
            parts.AddRange(EffectNotes(c));
            parts.AddRange(UnimplementedNotes(c));
            return string.Join(" ", parts.ToArray());
        }

        /// <summary>
        /// 触发类关键词 + 主动技能 → 卡面小字（`RALLY DMG1 FOE` / `ABILITY HEAL2 OWN`）。
        ///
        /// ⚠️ 带了关键词但**效果原文解析不出来**的，照样列出来、但打成 `*`
        ///    （`RALLY*`）。「关键词实现了」和「这张卡的效果能跑」是两件事 ——
        ///    不标的话玩家会以为它有作用，那比不显示更不诚实。
        /// </summary>
        static List<string> EffectNotes(CardDef c)
        {
            var list = new List<string>();
            if (c == null) return list;

            string[] triggers =
            {
                KeywordTable.Rally, KeywordTable.Strike, KeywordTable.Slay,
                KeywordTable.Backlash, KeywordTable.Penitence,
            };
            foreach (var kw in triggers)
            {
                if (!c.Has(kw)) continue;
                var spec = c.Effect(kw);
                list.Add(spec != null ? CardText.Keyword(kw) + " " + CardText.Effect(spec)
                                      : CardText.Keyword(kw) + "*");
            }

            if (c.Has(KeywordTable.Ability))
                list.Add(c.Ability != null
                         ? CardText.Keyword(KeywordTable.Ability) + " " + CardText.Effect(c.Ability)
                         : CardText.Keyword(KeywordTable.Ability) + "*");

            return list;
        }

        /// <summary>
        /// 卡上带了但**本版引擎不结算**的关键词，用 `*` 标出来。
        /// 不标的话玩家会以为它有作用 —— 这比直接不显示更诚实。
        /// </summary>
        static List<string> UnimplementedNotes(UnitState u)
        {
            var list = new List<string>();
            foreach (var kv in u.Card.Keywords)
                if (!KeywordTable.Implemented.Contains(kv.Key))
                    list.Add(CardText.Keyword(kv.Key) + "*");
            return list;
        }

        static List<string> UnimplementedNotes(CardDef c)
        {
            var list = new List<string>();
            foreach (var kv in c.Keywords)
                if (!KeywordTable.Implemented.Contains(kv.Key))
                    list.Add(CardText.Keyword(kv.Key) + "*");
            // 战术卡的**效果文字**能不能结算，和「关键词实现了没有」是两件事：
            // 关键词全实现了，效果照样可能解析不出来（原版卡面是英文自然语言）。
            // 解析不了的就在卡面打 `*` 说明白 —— 红线：不许静默失败。
            // 判据共用 `EffectText.IsFullyParsed`（和 `CanPlayTactic` / `DeckBuilder.TacticPlayable` 同一份）。
            if (c.Type == "tactic" && !EffectText.IsFullyParsed(c.Desc))
                list.Add("效果本版结算不了*");
            return list;
        }

        // ==================================================================
        //  HUD
        // ==================================================================

        void BuildHud()
        {
            // ⚠️ **只能建一次**：HUD 的结构不随对局变，变的只是字。
            //    重开一局（「按 R 再来一局」）会再走一遍 `Begin()` → `BuildHud()`，
            //    再建一份的话屏幕上会**叠两层 HUD**，而且**旧的那个结算面板成了孤儿**——
            //    `_endPanel` 已经指向新建的那个，旧面板再也没人 `Hide()`，就一直挂在画面上
            //    （2026-09-12 接「再来一局」时截图抓到的：新一局已经开打，上一局的
            //     「对局结束 / 三个骷髅 / 2/3 / 按 R 再来一局」还压在战场上）。
            if (_hudBuilt) return;
            _hudBuilt = true;

            var root = transform;
            CardArt.Load();

            _turnLabel = Hud(root, "", 0.5f, 0.965f, 4,
                             new Color(0.95f, 0.95f, 0.98f), new Vector2(0.5f, 1f), "TurnLabel");

            // ---- 名牌：原版左上是对手、左下是自己 ----
            // 位置出处：`d:/2/Warpforge_tools/scripts/arena_hud_layout.py` 从原版 RectTransform 算的
            //   EnemyInfo  (157,108) 260×75  → x01 0.082  y01(从下) 0.900
            //   PlayerInfo ( 32,977) 260×75  → x01 0.017  y01(从下) 0.095
            var dim = new Color(0.86f, 0.88f, 0.93f);
            // 名牌尺寸：原版 `NameBackground` 435.7×126.3，贴图 `UI_Player_Frame` 是 442×146（比例 3.027），
            // PreserveAspect 后实际绘 **382.3×126.3** → worldHeight = 126.3/108 = 1.1694。
            // ⚠️ 原来给的是 0.75（= 81 px 高），比原版**小 36%**。
            _enemyPlate = HudImage(root, "UI_Player_Frame", 0.082f, 0.900f,
                                   new Vector2(0f, 0.5f), 126.3f / 108f, "EnemyPlate");
            _enemyText = Hud(root, "", 0.098f, 0.900f, 3, dim, new Vector2(0f, 0.5f), "EnemyPlateText");
            _myPlate = HudImage(root, "UI_Player_Frame", 0.017f, 0.095f,
                                new Vector2(0f, 0.5f), 126.3f / 108f, "PlayerPlate");
            _myText = Hud(root, "", 0.033f, 0.095f, 3, dim, new Vector2(0f, 0.5f), "PlayerPlateText");

            // ---- 左下：能量宝石（原版 `40k_battle_energy_full/empty`）+ 数量 ----
            // ---- 能量 / 结束回合：**右侧一竖排**（原版 `RightArea/Right Anchor/Energy And turn holder`）----
            //
            // ⚠️ 2026-09-12 改：原来这两样都放在**左下角**（注释还写着「原版 Energy Player 也在左下角」——
            //    那句是没有出处的）。原版实测是一条**靠右的竖排**，从上到下：
            //      EnemyMana   97.7×97.7  绝对 x[1827.8,1903.9] y[249.8,327.4]（从上）
            //      Clock/TurnBtn 130.7×80.4  x[1782.9,1913.6] y[415.3,495.8]
            //      PlayerMana  97.7×97.7  绝对 x[1827.8,1903.9] y[517.1,594.1]
            //    出处：`资料/战斗规格/战斗重建_0827/战斗界面JSON权威表_0827.md:149-156`（绝对坐标表）
            //      + 运行时 dump `Energy And turn holder` 的 sizeDelta（本机重跑过，两者一致）。
            //    换算：x01 = 中心x/1920，y01 = 1 − 中心y(从上)/1080。
            // 大底板先铺（`HudImageZ` 让它比水晶远，压在水晶底下）
            //   原版 `Energy And turn holder` 自己的 rect：pos(5.6,−2.6) 尺寸 302.1×480.8、
            //   anchor(1,0.5) → 中心 x = 1920+5.6 = 1925.6（**右侧出血 156 px，原版就这样**）。
            //   ⚠️ 用 `HudDecorZ`（比别的图更远）—— 不然它会压住能量水晶，见那个常量的注释
            HudImageTex(root, CardArt.Ui("UI_Energy_Holder_big"), 1.00292f, 0.49759f,
                        new Vector2(0.5f, 0.5f), 480.8f / 108f, "EnergyHolderBig", HudDecorZ);
            //   任务点（`QuestPointsHolder` 97.7×97.7）：**我方在水晶下方、敌方在水晶上方**，
            //   接片 `UI_Quest_Points_Joint` 夹在水晶和任务点中间。
            //   ⚠️ 2026-09-12 更正：原来两个图标的位置用的是接片的偏移，都摆到了水晶**内侧**。
            HudImageTex(root, CardArt.Ui("UI_Quest_Points"), MyQuestX01, MyQuestY01,
                        new Vector2(0.5f, 0.5f), QuestPx / 108f, "PlayerQuestPoints", HudDecorZ + 0.05f);
            HudImageTex(root, CardArt.Ui("UI_Quest_Points"), FoeQuestX01, FoeQuestY01,
                        new Vector2(0.5f, 0.5f), QuestPx / 108f, "EnemyQuestPoints", HudDecorZ + 0.05f);
            _myQuestJoin = HudImageTex(root, CardArt.Ui("UI_Quest_Points_Joint"), MyQuestX01, MyQuestJoinY01,
                        new Vector2(0.5f, 0.5f), QuestJoinPx / 108f, "PlayerQuestJoin", HudDecorZ + 0.04f);
            _foeQuestJoin = HudImageTex(root, CardArt.Ui("UI_Quest_Points_Joint"), FoeQuestX01, FoeQuestJoinY01,
                        new Vector2(0.5f, 0.5f), QuestJoinPx / 108f, "EnemyQuestJoin", HudDecorZ + 0.04f);

            //   能量底板 `Card Frame Cost Icon`（原版 `Energy Player`，实绘 94.6×91.3）
            //   —— 两块水晶底下各垫一块。图在 `Resources/Art/ui_deck/`（和卡面费用格同一张）。
            _myEnergyPlate = HudImageTex(root, CardArt.DeckUi("Card_Frame_Cost_Icon"), MyEnergyPlateX01, MyEnergyPlateY01,
                        new Vector2(0.5f, 0.5f), EnergyPlateH / 108f, "PlayerEnergyPlate", HudDecorZ + 0.1f);
            _foeEnergyPlate = HudImageTex(root, CardArt.DeckUi("Card_Frame_Cost_Icon"), FoeEnergyPlateX01, FoeEnergyPlateY01,
                        new Vector2(0.5f, 0.5f), EnergyPlateH / 108f, "EnemyEnergyPlate", HudDecorZ + 0.1f);

            var gold = new Color(1f, 0.86f, 0.42f);
            // 我方水晶
            _energyGem = HudImage(root, "40k_battle_energy_full", MyEnergyX01, MyEnergyY01,
                                  new Vector2(0.5f, 0.5f), 0.72f, "EnergyGem");
            _energyGemEmpty = HudImage(root, "40k_battle_energy_empty", MyEnergyX01, MyEnergyY01,
                                       new Vector2(0.5f, 0.5f), 0.72f, "EnergyGemEmpty");
            // 数字压在宝石上（原版 `ManaText` 就框在 `Energy Player` 上，不是并排）
            _energyLabel = Hud(root, "", MyEnergyX01, MyEnergyY01, 4, gold, new Vector2(0.5f, 0.5f), "EnergyLabel");
            // 敌方水晶：原版有（`EnemyMana`，也带 `ManaText`），**我们原来一颗都没画** ——
            // 于是玩家看不到对手还剩多少能量，只能靠猜。
            _foeEnergyGem = HudImage(root, "40k_battle_energy_full", FoeEnergyX01, FoeEnergyY01,
                                     new Vector2(0.5f, 0.5f), 0.72f, "FoeEnergyGem");
            _foeEnergyGemEmpty = HudImage(root, "40k_battle_energy_empty", FoeEnergyX01, FoeEnergyY01,
                                          new Vector2(0.5f, 0.5f), 0.72f, "FoeEnergyGemEmpty");
            _foeEnergyLabel = Hud(root, "", FoeEnergyX01, FoeEnergyY01, 4, gold,
                                  new Vector2(0.5f, 0.5f), "FoeEnergyLabel");
            _handLabel = Hud(root, "", 0.017f, 0.158f, 3, dim, new Vector2(0f, 0f), "HandLabel");

            // ---- END TURN：原版 `Clock/TurnBtn` 130.7×80.4，**在右侧能量区中段**（不是右下角）----
            //     贴图 `UI_Button_End_Turn_Normal_wide` 是 182×112（比例 1.625），
            //     所以只给高度：80.4/108 = 0.74444 世界单位 → 宽自动 = 80.4×1.625 = 130.7 ✓
            //     ⚠️ 位置读 `EndTurnX01/Y01`（**判据只有那一份**，换分辨率重贴也读它）
            _endTurnBg = HudImage(root, "UI_Button_End_Turn_Normal_wide", EndTurnX01, EndTurnY01,
                                  new Vector2(0.5f, 0.5f), 80.4f / 108f, "EndTurnBg");
            // 文字压在按钮正中（原版 `TurnBtn/TurnText` 就是这个关系，两边读同一份坐标）
            _endTurnLabel = Hud(root, CardText.Phrase("END TURN"), EndTurnX01, EndTurnY01, 3,
                                new Color(1f, 1f, 1f), new Vector2(0.5f, 0.5f), "EndTurnButton");
            // 回合时钟：写在按钮**下半部分**（原版 `ClockManager.clockText` 也在 `Clock/TurnBtn` 子树里）。
            // 按钮 80.4 px 高 = 0.744 世界单位，往下让 0.021 ≈ 23 px，正好落在按钮下半。
            _clockLabel = Hud(root, "", EndTurnX01, EndTurnY01 + 0.021f, 2,
                              new Color(0.85f, 0.88f, 0.95f), new Vector2(0.5f, 0.5f), "TurnClock");

            // ---- 牌堆：照原版 `PlayerDeck` 那一套摆 ----
            //
            // ⚠️ **原版的牌堆不是「好几张叠起来」** —— 运行时 dump
            // （`资料/原版参照图/Unity参照管线_0825/data/runtime_ui_dump_Battle_Arena_1.tsv`）
            // 里 `PlayerDeck` 是 230×230，底下四样东西：
            //   `DeckAndEnergyImage`  图 `UI_Deck_Background`  —— 底板
            //   `YourTurnImage` / `NotYourTurnImage`
            //                         图 `40k_DeckHolder_light_green` / `_light_red` —— **回合灯**
            //                         （anchor 0.779–0.878 × 0.051–0.201，在底板右下角）
            //   `Player Deck Size Container`（图 `40K_display`）→ `Player Deck Size Tex` —— 张数
            //   `Cardback Container`（scale 100）→ `Cardback`(2.17×3.14×100 = **217×314 px**)
            //                                     + `Cardback Shadow SDF`(292×381 px)
            // 230 px = 2.13 世界单位、314 px = 2.91 世界单位（108 px/单位）。
            // 张数文字原来压在一个 `40K_display` 小板上，我们直接用文字（那张图没在用的集合里）。
            _myDeckPlate = HudImageTex(root, CardArt.Ui("UI_Deck_Background"), MyDeckX01, MyDeckY01,
                                       new Vector2(0.5f, 0.5f), Px(DeckPlatePx), "MyDeckPlate");
            _foeDeckPlate = HudImageTex(root, CardArt.Ui("UI_Deck_Background"), FoeDeckX01, FoeDeckY01,
                                        new Vector2(0.5f, 0.5f), Px(FoeDeckPlatePx), "FoeDeckPlate");
            // 底板再往后一点，别把卡背盖住（`HudImageTex` 已经把图放到文字后面了）
            if (_myDeckPlate != null) _myDeckPlate.transform.localPosition += new Vector3(0f, 0f, 0.02f);
            if (_foeDeckPlate != null) _foeDeckPlate.transform.localPosition += new Vector3(0f, 0f, 0.02f);

            _myPile = HudImageTex(root, CardArt.CardBack(_myFaction), MyDeckX01, MyDeckY01,
                                  new Vector2(0.5f, 0.5f), Px(DeckCardPx), "MyDeck");
            _foePile = HudImageTex(root, CardArt.CardBack(_foeFaction), FoeDeckX01, FoeDeckY01,
                                   new Vector2(0.5f, 0.5f), Px(DeckCardPx), "FoeDeck");

            // 回合灯：底板右下角（位置在 `PlaceDeckLights` 里统一摆 —— 换分辨率要重贴）
            _myDeckLight = HudImageTex(root, CardArt.Ui("40k_DeckHolder_light_green"), MyDeckX01, MyDeckY01,
                                       new Vector2(0.5f, 0.5f), Px(DeckLightPx), "MyDeckLight");
            _foeDeckLight = HudImageTex(root, CardArt.Ui("40k_DeckHolder_light_green"), FoeDeckX01, FoeDeckY01,
                                        new Vector2(0.5f, 0.5f), Px(DeckLightPx), "FoeDeckLight");
            PlaceDeckLights();

            // 张数写在底板上方 —— 原版 `Player Deck Size Container` 的 pivot 在底板**上沿**再上 165 px
            // （anchor y=1 + pos y=50 + 半个 rect 高）→ 0.235 + 165/1080 = 0.388；对手那边镜像。
            // ⚠️ 原来放在 0.150 / 0.875，卡背按原版尺寸放大后就被压住了
            _pileLabel = Hud(root, "", MyDeckX01, MyDeckY01 + DeckLabelDy01, 3, dim,
                             new Vector2(0.5f, 0.5f), "MyPileLabel");
            _foePileLabel = Hud(root, "", FoeDeckX01, FoeDeckY01 - DeckLabelDy01, 3, dim,
                                new Vector2(0.5f, 0.5f), "FoePileLabel");

            // 提示行放在**两行棋盘中间那条缝**里（玩家行上沿 0.483 / 对手行下沿 0.530）
            _hintLabel = Hud(root, "", 0.5f, 0.507f, 3,
                             new Color(0.75f, 0.78f, 0.85f), new Vector2(0.5f, 0.5f), "HintLabel");

            _resultLabel = Hud(root, "", 0.5f, 0.5f, 7,
                               new Color(1f, 0.9f, 0.4f), new Vector2(0.5f, 0.5f), "ResultLabel");

            // 设置按钮（原版 `SettingsBtn` 63.9²，图 `UI_Settings_Icon`）——
            // 权威表绝对坐标 x[1808.0,1871.9] y[9.2,73.1] → 中心 (1839.95, 41.15)。
            _settingsBtn = HudImage(root, "UI_Settings_Icon", 0.95831f, 0.96190f,
                                    new Vector2(0.5f, 0.5f), 63.87f / 108f, "SettingsBtn");
            // 设置面板（投降按钮在里面）
            _settingsPanel = SettingsPanel.Create(root, Forfeit);

            // 结算面板：原版 `EndBattlePanel`。它自己管显示/隐藏，平时是关着的。
            _endPanel = EndPanel.Create(root);
            // 卡牌放大展示窗：原版 `CardDisplayWindow`。轻点卡牌开关，平时关着。
            _cardDisplay = CardDisplayWindow.Create(root);
        }

        /// <summary>
        /// 把两盏回合灯摆到牌堆底板的右下角。
        /// ⚠️ **必须可重入** —— `ReanchorHud` 换分辨率时会把所有 HUD 图拉回各自的锚点，
        ///    那会把灯上的偏移抹掉，所以那边也要再调一次。这里是从零算出来的，调几次都一样。
        /// </summary>
        void PlaceDeckLights()
        {
            var off = new Vector3(Px(DeckLightDxPx), Px(DeckLightDyPx), 0f);
            float z = HudImageZ - 0.01f;          // 比卡背再靠前一点，别被压住
            if (_myDeckLight != null)
            {
                var p = LayoutSpace.ToWorld(MyDeckX01, MyDeckY01) + off;
                _myDeckLight.transform.localPosition = new Vector3(p.x, p.y, z);
            }
            if (_foeDeckLight != null)
            {
                var p = LayoutSpace.ToWorld(FoeDeckX01, FoeDeckY01) + off;
                _foeDeckLight.transform.localPosition = new Vector3(p.x, p.y, z);
            }
        }

        /// <summary>牌堆的回合灯。原版是**两张图**（绿/红），不是同一张染色 —— 换贴图而不是换 `_Color`</summary>
        void SetDeckLight(ImageQuad q, bool lit)
        {
            if (q == null) return;
            var tex = CardArt.Ui(lit ? "40k_DeckHolder_light_green" : "40k_DeckHolder_light_red");
            if (tex != null && q.Texture != tex) q.SetTexture(tex);
        }

        // ---- 自检用：牌堆那几张图现在的实际尺寸/贴图（**截图看不出「尺寸对不对」**）----
        /// <summary>牌堆底板的世界高度（应 ≈ 230/108 = 2.13）</summary>
        public float DeckPlateWorldH { get { return _myDeckPlate != null ? _myDeckPlate.WorldH : 0f; } }
        /// <summary>卡背的世界高度（应 ≈ 314/108 = 2.91）</summary>
        public float DeckCardWorldH { get { return _myPile != null ? _myPile.WorldH : 0f; } }
        /// <summary>我这边的回合灯现在是哪张图</summary>
        public string MyDeckLightTex
        {
            get { return (_myDeckLight != null && _myDeckLight.Texture != null) ? _myDeckLight.Texture.name : "<无>"; }
        }
        /// <summary>对手那边的回合灯现在是哪张图</summary>
        public string FoeDeckLightTex
        {
            get { return (_foeDeckLight != null && _foeDeckLight.Texture != null) ? _foeDeckLight.Texture.name : "<无>"; }
        }

        // ---- 自检用：右侧能量区那几件（**截图看不出「贴图对不对/谁上谁下」**）----
        /// <summary>敌方能量水晶现在用的贴图（`40k_battle_energy_full` / `_empty`）</summary>
        public string FoeEnergyGemTex
        {
            get { return (_foeEnergyGem != null && _foeEnergyGem.Texture != null) ? _foeEnergyGem.Texture.name : "<无>"; }
        }
        /// <summary>自检读：回合时钟那行字（`1:00`；进了倒计时那段是纯秒数）</summary>
        public string ClockText { get { return _clockLabel != null ? _clockLabel.Text : null; } }
        /// <summary>自检读：还剩多少秒</summary>
        public float ClockLeft { get { return _clockLeft; } }
        /// <summary>自检读：是不是已经进了「超时后的倒计时」那一段</summary>
        public bool ClockCountingDown { get { return _clockInCountdown; } }
        /// <summary>自检用：把表按秒推（批处理下没有真实帧循环）</summary>
        public void TickClockForTest(float dt) { TickClock(dt); }

        /// <summary>敌方能量数字（`2/2` 这种）—— 那颗水晶原来**根本没画**</summary>
        public string FoeEnergyText { get { return _foeEnergyLabel != null ? _foeEnergyLabel.Text : null; } }
        /// <summary>水晶底下那块底板用的图（应为 `Card_Frame_Cost_Icon`）</summary>
        public string MyEnergyPlateTex
        {
            get { return (_myEnergyPlate != null && _myEnergyPlate.Texture != null) ? _myEnergyPlate.Texture.name : "<无>"; }
        }
        public string FoeEnergyPlateTex
        {
            get { return (_foeEnergyPlate != null && _foeEnergyPlate.Texture != null) ? _foeEnergyPlate.Texture.name : "<无>"; }
        }
        /// <summary>水晶在屏幕上的归一化位置（x01 / y01，y 从**下**算）</summary>
        public Vector2 MyEnergyPos01 { get { return PosOf(_energyGem); } }
        public Vector2 FoeEnergyPos01 { get { return PosOf(_foeEnergyGem); } }
        /// <summary>任务点那两张图的归一化位置 —— 我方应在水晶**下方**、敌方在**上方**</summary>
        public Vector2 QuestIconPos(bool mine)
        {
            var go = transform.Find(mine ? "PlayerQuestPoints" : "EnemyQuestPoints");
            return go != null ? LayoutSpace.ToNormalized(go.localPosition) : new Vector2(-1f, -1f);
        }
        static Vector2 PosOf(Component c)
        {
            return c == null ? new Vector2(-1f, -1f) : LayoutSpace.ToNormalized(c.transform.localPosition);
        }

        // HUD 的每个字都是**世界空间**的一块 quad，位置在建立时算好就固定了 ——
        // 而 `LayoutSpace.VisibleWidth` 跟着宽高比走，所以切分辨率时必须重算，
        // 不然 4:3 建、16:9 拍的时候文字会整片偏到左边（批处理自检里踩过）。
        readonly List<Label> _hudLabels = new List<Label>();
        readonly List<Vector2> _hudSpots = new List<Vector2>();
        readonly List<ImageQuad> _hudImages = new List<ImageQuad>();
        readonly List<Vector2> _hudImageSpots = new List<Vector2>();
        float _hudWidth = -1f;

        Label Hud(Transform root, string text, float x01, float y01, int scale,
                  Color c, Vector2 anchor, string name)
        {
            var l = Label.Create(root, text, LayoutSpace.ToWorld(x01, y01), scale, c, anchor, name);
            _hudLabels.Add(l);
            _hudSpots.Add(new Vector2(x01, y01));
            return l;
        }

        /// <summary>HUD 上的一张图。**没有那张图就返回 null**（删掉美术目录也能跑）</summary>
        ImageQuad HudImage(Transform root, string artName, float x01, float y01,
                           Vector2 anchor, float worldHeight, string name)
            => HudImageTex(root, CardArt.Ui(artName), x01, y01, anchor, worldHeight, name);

        /// <summary>HUD 图统一往后放这么多（相机看 +Z，z 越大越远）—— 文字在 z=0，图在后面</summary>
        const float HudImageZ = 0.3f;

        /// <summary>**装饰性**底板再往后一层。⚠️ HUD 图原本全在同一个 z，而它们是同一个透明队列、
        /// 距离也一样 —— 谁压谁由渲染顺序决定，**不确定**。右侧能量区那张大底板
        /// （`UI_Energy_Holder_big`，一张几乎铺满那一片的金属板）就是这么把能量水晶压住的
        /// （2026-09-12 截图抓到：水晶只剩一块灰板）。要压在谁底下就给它更大的 z。</summary>
        const float HudDecorZ = 0.6f;

        ImageQuad HudImageTex(Transform root, Texture2D tex, float x01, float y01,
                              Vector2 anchor, float worldHeight, string name, float z = HudImageZ)
        {
            var q = ImageQuad.Create(root, tex, LayoutSpace.ToWorld(x01, y01),
                                     worldHeight, anchor, name);
            if (q != null)
            {
                // ⚠️ 往后放一点（相机看 +Z，z 越大越远）：HUD 文字在 z=0，
                //    同 z 的话谁压谁看渲染顺序，实测按钮底图会把文字盖住（踩过）
                q.transform.localPosition += new Vector3(0f, 0f, z);
                _hudImages.Add(q);
                _hudImageSpots.Add(new Vector2(x01, y01));
            }
            return q;
        }

        void ReanchorHud()
        {
            if (Mathf.Approximately(_hudWidth, LayoutSpace.VisibleWidth)) return;
            _hudWidth = LayoutSpace.VisibleWidth;
            for (int i = 0; i < _hudLabels.Count; i++)
                if (_hudLabels[i] != null)
                    _hudLabels[i].transform.localPosition =
                        LayoutSpace.ToWorld(_hudSpots[i].x, _hudSpots[i].y);
            for (int i = 0; i < _hudImages.Count; i++)
                if (_hudImages[i] != null)
                    _hudImages[i].SetAnchorPosition(_hudImageSpots[i].x, _hudImageSpots[i].y);

            // 牌堆的回合灯不是贴在锚点上的（要偏到牌堆底板的右下角），上面那一轮会把它拉回中心
            PlaceDeckLights();

            // 格位底片和槽带同理（它们也是用 VisibleWidth 算的）
            if (playerBoard != null) playerBoard.EnsureMarkers();
            if (enemyBoard != null) enemyBoard.EnsureMarkers();
            if (backdrop != null) backdrop.Refresh();
            // 攻击方式选择器同理 —— 它的按钮位置也按 VisibleWidth/Scale 算
            if (selector != null) selector.RefreshLayout();
            // 技能卡面板取的是**屏幕 30%×30% 的 anchor**，宽高比跟着屏幕走 —— 同理
            if (skillPanel != null) skillPanel.RefreshLayout();
        }

        void UpdateHud()
        {
            if (_turnLabel == null || Ctx == null) return;
            ReanchorHud();

            var me = Ctx.Players[_me];
            var foe = Ctx.Players[1 - _me];

            string who = Ctx.IsOver ? "GAME OVER"
                       : (Ctx.Active == _me ? "YOUR TURN" : "ENEMY TURN");
            _turnLabel.SetText(CardText.TurnLabel(Ctx.Turn) + "   " + CardText.Phrase(who));

            _energyLabel.SetText($"{me.Energy}/{me.MaxEnergy}");
            _handLabel.SetText(CardText.Phrase("HAND") + " " + me.Hand.Count);
            _myText.SetText(CardText.Faction(_myFaction) + "   " + CardText.Phrase("HP") + " " +
                            Mathf.Max(0, me.Warlord.Health));
            _enemyText.SetText(CardText.Faction(_foeFaction) + "   " + CardText.Phrase("HP") + " " +
                               Mathf.Max(0, foe.Warlord.Health));
            // 记「降到过的最低生命」（骷髅头判据用它）
            if (foe.Warlord.Health < _foeWarlordMinHp) _foeWarlordMinHp = foe.Warlord.Health;
            _pileLabel.SetText(CardText.Phrase("DECK") + " " + me.Deck.Count + "  " +
                               CardText.Phrase("DISC") + " " + me.Discard.Count);
            _foePileLabel.SetText(CardText.Phrase("DECK") + " " + foe.Deck.Count + "  " +
                                  CardText.Phrase("DISC") + " " + foe.Discard.Count);

            // 能量宝石：有能量亮、没能量灭（原版两张图）
            bool hasEnergy = me.Energy > 0 && Ctx.Active == _me && !Ctx.IsOver;
            if (_energyGem != null) _energyGem.gameObject.SetActive(hasEnergy);
            if (_energyGemEmpty != null) _energyGemEmpty.gameObject.SetActive(!hasEnergy);
            // 敌方那颗同理（判据同一份，只是主语换成对手）
            bool foeHasEnergy = foe.Energy > 0 && Ctx.Active != _me && !Ctx.IsOver;
            if (_foeEnergyGem != null) _foeEnergyGem.gameObject.SetActive(foeHasEnergy);
            if (_foeEnergyGemEmpty != null) _foeEnergyGemEmpty.gameObject.SetActive(!foeHasEnergy);
            if (_foeEnergyLabel != null) _foeEnergyLabel.SetText($"{foe.Energy}/{foe.MaxEnergy}");

            bool myTurn = Ctx.Active == _me && !Ctx.IsOver;
            _endTurnLabel.SetColor(myTurn ? new Color(1f, 0.85f, 0.35f) : new Color(0.35f, 0.35f, 0.40f));
            if (_endTurnBg != null)
                _endTurnBg.SetTint(myTurn ? Color.white : new Color(0.42f, 0.44f, 0.50f));

            // 牌堆的回合灯：轮到自己亮绿、否则红（原版两张图 `40k_DeckHolder_light_green/_red`）
            SetDeckLight(_myDeckLight, myTurn);
            SetDeckLight(_foeDeckLight, Ctx.Active != _me && !Ctx.IsOver);

            if (Ctx.IsOver)
            {
                string r = Ctx.Winner == 3 ? "DRAW" : (Ctx.Winner == _me + 1 ? "YOU WIN" : "YOU LOSE");
                // 结算面板接管这块文字（面板自己有标题）—— 留着的话中心会和面板标题撞成两处
                _resultLabel.SetText(_endPanel == null ? CardText.Phrase(r) : "");
                // 结算：这里要**真的清空**（不走 `SetHint`）—— 落回「开局那句牌组说明」的话，
                // 它会从结算面板底下透出来（提示行 z=3，结算面板盖在中间）
                if (_hintLabel != null) _hintLabel.SetText("");
                if (_endPanel != null && !_endPanel.Visible)
                    _endPanel.Show(Ctx.Winner, _me,
                                   _foeWarlordMinHp == int.MaxValue ? 30 : _foeWarlordMinHp, Ctx.Turn,
                                   Ctx.ForfeitedBy);
            }
            else
            {
                _resultLabel.SetText("");
                if (_endPanel != null && _endPanel.Visible) _endPanel.Hide();
            }
        }

        // ==================================================================
        //  输入
        // ==================================================================

        public void SetCamera(Camera c) { cam = c; }

        Vector3 WorldPointer()
        {
            Vector2 sp = PointerScreen();
            return LayoutSpace.ScreenToWorld(sp, cam);
        }

        static Vector2 PointerScreen()
        {
            if (Mouse.current != null) return Mouse.current.position.ReadValue();
            if (Touchscreen.current != null) return Touchscreen.current.primaryTouch.position.ReadValue();
            return Vector2.zero;
        }

        bool _clickLatch;

        bool ClickedThisFrame()
        {
            // 轮询按下（批处理里没有输入事件，轮询才验得了）；用 latch 防止按住触发多次
            bool down = Mouse.current != null && Mouse.current.leftButton.isPressed
                     || Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed;
            if (!down) { _clickLatch = false; return false; }
            if (_clickLatch) return false;
            _clickLatch = true;
            return true;
        }

        /// <summary>点到哪个槽位了（从最前面往后测，压住的也能选中）</summary>
        static int HitSlot(Dictionary<int, CardView> views, Vector3 world)
        {
            int best = -1;
            float bestZ = float.MaxValue;
            foreach (var kv in views)
            {
                var v = kv.Value;
                if (v == null || !v.Contains(world)) continue;
                float z = v.transform.position.z;
                if (z < bestZ) { bestZ = z; best = kv.Key; }
            }
            return best;
        }

        // ---- 给批处理自检用的显式接口（不进 play 模式也能走完整流程）----
        //
        // ⚠️ 这几个都走**和真实鼠标同一条路**（开选择器 → 选打法 → 点目标），
        //    不是绕过交互直接调引擎 —— 那样自检就验不到交互本身了。

        /// <summary>点自己的单位 → 弹出选择器。返回「真弹出来了」没有</summary>
        public bool SimulateOpenCommand(int slot) { OpenCommand(slot); return _selectedSlot == slot; }

        /// <summary>点选择器上的某个按钮</summary>
        public void SimulateCommand(AttackKind kind) { CommitCommand(kind); }

        /// <summary>点敌方目标 → 结算。返回引擎码</summary>
        public int SimulateResolve(int foeSlot) { return Resolve(_command, foeSlot); }

        /// <summary>选择器上有没有这一项（自检用来挑一个能点的）</summary>
        public bool HasCommand(AttackKind kind)
        {
            if (selector == null) return false;
            foreach (var o in selector.Options) if (o.Kind == kind && o.Enabled) return true;
            return false;
        }

        public bool SimulateDeselect() { ClearSelection(); return _selectedSlot < 0; }
        /// <summary>把指针挪到某个世界坐标（只喂给选择器，不做别的）</summary>
        /// <summary>自检用：把指针挪到某处。**和真实输入共用同一套逻辑**（选择器高亮 + 准星 + 面板）</summary>
        public void SimulatePointerAt(Vector3 world, bool down = false)
        {
            if (selector != null) selector.UpdatePointer(world);
            // 准星和面板跟着同一个指针走。批处理没有 `Update()`，这里补上 —— 但走的是**同一个**
            // `UpdateReticle` / `SetPointer`，不另写一份判据
            if (_selectedSlot >= 0 && _command != AttackKind.None)
            {
                UpdateReticle(world);
                if (skillPanel != null && skillPanel.Visible) skillPanel.SetPointer(world, down);
            }
        }

        /// <summary>自检用：准星现在开着吗（`reticle` 为空也算 false）</summary>
        public bool ReticleVisible { get { return reticle != null && reticle.Visible; } }
        public void SimulateEndTurn() { EndPlayerTurn(); }
        public int SelectedSlot { get { return _selectedSlot; } }
        public AttackKind Command { get { return _command; } }
        public AttackSelector Selector { get { return selector; } }
        public bool SelectorOpen { get { return selector != null && selector.Visible; } }
        public AttackKind HoveredCommand { get { return selector != null ? selector.Hovered : AttackKind.None; } }
        public string SelectorDescription { get { return selector != null ? selector.Describe() : "（没有选择器）"; } }
        public int HandCount { get { return _handViews.Count; } }
        /// <summary>我方/对手阵营（自检用）。</summary>
        public string MyFaction { get { return _myFaction; } }
        public string FoeFaction { get { return _foeFaction; } }
        /// <summary>本局我方用的**存档卡组**（null = 自动凑的）。</summary>
        public PlayerDeck MyDeckSource { get { return _myDeckSrc; } }
        /// <summary>开局那句「本局用的是哪副牌」的原文。空串 = 没什么要交代的。</summary>
        public string DeckNotice { get { return _deckNotice; } }
        /// <summary>提示行现在写着什么。⚠️ 它平时等于 <see cref="DeckNotice"/>（休息态），
        /// 悬停/选目标时会被临时提示盖住 —— 断言「玩家看得见那句」要挑对时机。</summary>
        public string HintText { get { return _hintLabel != null ? _hintLabel.Text : null; } }
        /// <summary>提示行这块字有多宽（世界单位，可见区宽 = `LayoutSpace.VisibleWidth`）。
        /// ⚠️ `Label` 是 **NoWrap** 的，太长不会折行、只会横着长到屏幕外去 ——
        /// 而卡组名是玩家自己起的、长度不可控，所以要有条断言挡着。</summary>
        public float HintWidth { get { return _hintLabel != null ? _hintLabel.WorldW : 0f; } }
        public IReadOnlyDictionary<int, CardView> MyUnits { get { return _myUnits; } }
        public IReadOnlyDictionary<int, CardView> FoeUnits { get { return _foeUnits; } }

        /// <summary>
        /// 自检用：放技能。走的是**和真实点击同一条路**：开选择器 → 选技能 → 点目标。
        /// 返回引擎的码。`targetSlot &lt; 0` = 只到「选目标」这一步就停（给截图留的）。
        /// </summary>
        public int SimulateUseAbility(int slot, int targetSlot = -1)
        {
            if (!SimulateOpenCommand(slot)) return RuleCodes.ErrNotUnit;

            var u = Ctx.Players[_me].Board[slot];
            if (u == null || !u.HasAbility) { ClearSelection(); return RuleCodes.ErrNoAbility; }
            if (!HasCommand(AttackKind.Ability)) { ClearSelection(); return RuleCodes.ErrNoAbility; }

            CommitCommand(AttackKind.Ability);
            if (_selectedSlot < 0) return RuleCodes.OK;   // 不用选目标的技能已经结算完了
            if (targetSlot < 0) return RuleCodes.OK;      // 停在「选目标」

            return Resolve(AttackKind.Ability, targetSlot);
        }

        /// <summary>自检用：取手牌第 idx 张的视图（拖拽用例要拿它的世界坐标）</summary>
        public CardView HandViewAt(int idx)
        {
            return (idx >= 0 && idx < _handViews.Count) ? _handViews[idx] : null;
        }

        /// <summary>自检用：把画面上的手牌名字列出来对账</summary>
        public string HandViewNames()
        {
            var names = new List<string>();
            foreach (var v in _handViews) names.Add(v == null ? "<null>" : $"{v.Data.id}({v.Data.cost})");
            return string.Join("/", names.ToArray());
        }

        /// <summary>自检用：把场上的单位名字列出来（按槽位）</summary>
        public string BoardViewNames(bool mine)
        {
            var views = mine ? _myUnits : _foeUnits;
            var names = new List<string>();
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                CardView v;
                if (views.TryGetValue(s, out v) && v != null) names.Add($"{s}:{v.Data.id}");
            }
            return string.Join(" ", names.ToArray());
        }

        /// <summary>自检用：把手牌第 idx 张直接打到 slot（跳过鼠标拖拽）</summary>
        public int SimulatePlay(int idx, int slot)
        {
            int code = RuleCore.PlayCard(Ctx, _me, idx, slot);
            if (code == RuleCodes.OK) RefreshAll();
            return code;
        }

        /// <summary>自检用：把对手那一步也走完（省得等延时）</summary>
        public void SimulateAiTurn()
        {
            SimpleAI.PlayTurn(Ctx);
            if (Ctx.IsOver) { RefreshAll(); return; }
            RuleCore.EndTurn(Ctx);
            RuleCore.BeginTurn(Ctx);
            RefreshAll();
        }
    }
}
