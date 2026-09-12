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

        // ---- 阵营配色（只用在卡框上，卡面本身是黑的）----
        public static readonly Color EmberColor = new Color(0.85f, 0.38f, 0.25f);
        public static readonly Color TideColor = new Color(0.28f, 0.55f, 0.85f);

        // ---- 状态 ----
        public BattleContext Ctx { get; private set; }
        int _me = 0;
        string _myFaction = StarterCards.EmberFaction;
        string _foeFaction = StarterCards.TideFaction;

        readonly Dictionary<int, CardView> _myUnits = new Dictionary<int, CardView>();
        readonly Dictionary<int, CardView> _foeUnits = new Dictionary<int, CardView>();
        readonly List<CardView> _handViews = new List<CardView>();

        Label _turnLabel, _energyLabel, _endTurnLabel, _resultLabel, _hintLabel;
        Label _handLabel, _myText, _enemyText;
        Label _pileLabel, _foePileLabel;

        // 原版 UI 图（`Resources/Art/ui/`，没有就是 null —— 退回纯文字 HUD）
        ImageQuad _endTurnBg, _energyGem, _energyGemEmpty, _myPlate, _enemyPlate;
        ImageQuad _myPile, _foePile;
        ImageQuad _myDeckPlate, _foeDeckPlate, _myDeckLight, _foeDeckLight;

        // ---- 牌堆那一套的尺寸（px @1920×1080 → 世界单位，108 px/单位）----
        // 出处：运行时 dump `runtime_ui_dump_Battle_Arena_1.tsv` 里 `PlayerDeck` 的子树
        /// <summary>牌堆底板 `UI_Deck_Background`：`PlayerDeck` 自己的 230×230</summary>
        const float DeckPlatePx = 230f;
        /// <summary>卡背：`Cardback` 的 `sizeDelta` 2.1739 × 3.1364，父节点 scale 100 → 217×314 px</summary>
        const float DeckCardPx = 314f;
        /// <summary>回合灯：`YourTurnImage` 的 anchor 占底板的 10%×15% → 34.5 px</summary>
        const float DeckLightPx = 34.5f;
        /// <summary>回合灯相对牌堆中心的偏移（px）：anchor 0.8285 / 0.126 在 230×230 底板上折算</summary>
        const float DeckLightDxPx = 75.6f, DeckLightDyPx = -86f;
        static float Px(float px) { return px / 108f; }

        /// <summary>两边牌堆的锚点（归一化）。**判据只有这一份** —— 建、重贴、放灯都用它</summary>
        const float MyDeckX01 = 0.845f, MyDeckY01 = 0.235f;
        const float FoeDeckX01 = 0.845f, FoeDeckY01 = 0.790f;
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
            if (Ctx == null) Begin();
        }

        // ==================================================================
        //  开局
        // ==================================================================

        public void Begin(string myFaction = null, string foeFaction = null, int seed = 20260911)
        {
            if (myFaction != null) _myFaction = myFaction;
            if (foeFaction != null) _foeFaction = foeFaction;

            var myDeck = DeckBuilder.StarterDeck(StarterCards.Of(_myFaction), _myFaction,
                                                 DeckBuilder.ClassicDeckSize, new System.Random(seed + 1));
            var foeDeck = DeckBuilder.StarterDeck(StarterCards.Of(_foeFaction), _foeFaction,
                                                  DeckBuilder.ClassicDeckSize, new System.Random(seed + 2));

            Ctx = RuleCore.NewBattle(myDeck, foeDeck, seed);

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
            interaction.OnDeployed += OnCardDeployed;

            RuleCore.BeginTurn(Ctx);       // 先手第 1 回合：能量 2、抽 1
            RefreshAll();
            UpdateHud();
        }

        void OnCardDeployed(CardView card, int slot)
        {
            int idx = HandIndexOf(card);
            if (idx < 0)
            {
                Debug.LogError("[Battle] 落位回调找不到这张牌的手牌索引 —— 引擎和画面不同步了");
                return;
            }

            int code = RuleCore.PlayCard(Ctx, _me, idx, slot);
            if (code != RuleCodes.OK)
            {
                Debug.LogError($"[Battle] 引擎拒绝了这次落位（{RuleCodes.Describe(code)}）—— "
                             + "校验委托和实际出牌用的不是同一份判据");
                return;
            }

            // 这张卡从手牌变成场上单位：视图也搬过去，别重建（重建会丢落位动画）
            _handViews.Remove(card);
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

            if (Ctx.IsOver) { UpdateHud(); return; }

            if (Ctx.Active == _me) DrivePlayerTurn();
            else DriveAiTurn();

            UpdateHud();
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

            if (u.Exhausted) { _hintLabel.SetText(CardText.Phrase("THIS UNIT ALREADY ACTED")); return; }
            if (u.IsStunned) { _hintLabel.SetText(CardText.Phrase("STUNNED")); return; }

            bool melee = RuleCore.FieldAttack(Ctx, _me, u, false) > 0;
            bool ranged = RuleCore.FieldAttack(Ctx, _me, u, true) > 0;
            bool skill = u.HasAbility && RuleCore.CanStartAbility(Ctx, _me, slot) == RuleCodes.OK;

            if (!melee && !ranged && !skill)
            {
                _hintLabel.SetText(CardText.Phrase("THIS UNIT CANNOT ACT"));
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
            _hintLabel.SetText("");
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
            _hintLabel.SetText(CardText.Phrase(what) + " - " +
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
            if (_hintLabel != null) _hintLabel.SetText("");
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
                int card = SimpleAI.NextCardToPlay(Ctx);
                if (card >= 0)
                {
                    int slot = SimpleAI.FirstFreeSlot(Ctx.ActivePlayer);
                    if (slot >= 0 && RuleCore.PlayCard(Ctx, Ctx.Active, card, slot) == RuleCodes.OK)
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
            var frame = owner == _me
                      ? (_myFaction == StarterCards.TideFaction ? TideColor : EmberColor)
                      : (_foeFaction == StarterCards.TideFaction ? TideColor : EmberColor);

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
                title = CardText.Name(u.Name),
                cost = -1,
                melee = u.Attack,
                ranged = u.RangedAttack,
                health = u.Health,
                keywords = DescribeKeywords(u),
                isUnit = true,
                frame = new Color(0.55f, 0.55f, 0.62f),
                faction = faction,
            };
        }

        CardData ToCardData(CardDef c, string faction)
        {
            return new CardData
            {
                id = c.Name,                       // 同上：英文，给立绘用
                title = CardText.Name(c.Name),
                cost = c.Cost,
                melee = c.Attack,
                ranged = c.RangedAttack,
                health = c.Health,
                keywords = DescribeKeywords(c),
                isUnit = c.IsUnit,
                frame = new Color(0.55f, 0.55f, 0.62f),
                faction = faction,
            };
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
            return list;
        }

        // ==================================================================
        //  HUD
        // ==================================================================

        void BuildHud()
        {
            var root = transform;
            CardArt.Load();

            _turnLabel = Hud(root, "", 0.5f, 0.965f, 4,
                             new Color(0.95f, 0.95f, 0.98f), new Vector2(0.5f, 1f), "TurnLabel");

            // ---- 名牌：原版左上是对手、左下是自己 ----
            // 位置出处：`d:/2/Warpforge_tools/scripts/arena_hud_layout.py` 从原版 RectTransform 算的
            //   EnemyInfo  (157,108) 260×75  → x01 0.082  y01(从下) 0.900
            //   PlayerInfo ( 32,977) 260×75  → x01 0.017  y01(从下) 0.095
            var dim = new Color(0.86f, 0.88f, 0.93f);
            _enemyPlate = HudImage(root, "UI_Player_Frame", 0.082f, 0.900f,
                                   new Vector2(0f, 0.5f), 0.75f, "EnemyPlate");
            _enemyText = Hud(root, "", 0.098f, 0.900f, 3, dim, new Vector2(0f, 0.5f), "EnemyPlateText");
            _myPlate = HudImage(root, "UI_Player_Frame", 0.017f, 0.095f,
                                new Vector2(0f, 0.5f), 0.75f, "PlayerPlate");
            _myText = Hud(root, "", 0.033f, 0.095f, 3, dim, new Vector2(0f, 0.5f), "PlayerPlateText");

            // ---- 左下：能量宝石（原版 `40k_battle_energy_full/empty`）+ 数量 ----
            // 原版 `Energy Player` 也在左下角，就在名牌右边
            _energyGem = HudImage(root, "40k_battle_energy_full", 0.168f, 0.078f,
                                  new Vector2(0f, 0.5f), 0.72f, "EnergyGem");
            _energyGemEmpty = HudImage(root, "40k_battle_energy_empty", 0.168f, 0.078f,
                                       new Vector2(0f, 0.5f), 0.72f, "EnergyGemEmpty");
            var gold = new Color(1f, 0.86f, 0.42f);
            _energyLabel = Hud(root, "", 0.213f, 0.078f, 4, gold, new Vector2(0f, 0.5f), "EnergyLabel");
            _handLabel = Hud(root, "", 0.017f, 0.158f, 3, dim, new Vector2(0f, 0f), "HandLabel");

            // ---- 右下：原版 END TURN 按钮底图 + 文字压在中间 ----
            _endTurnBg = HudImage(root, "UI_Button_End_Turn_Normal_wide", 0.952f, 0.062f,
                                  new Vector2(1f, 0.5f), 1.15f, "EndTurnBg");
            _endTurnLabel = Hud(root, CardText.Phrase("END TURN"), 0.952f - EndTurnTextDx(), 0.062f, 3,
                                new Color(1f, 1f, 1f), new Vector2(0.5f, 0.5f), "EndTurnButton");

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
                                        new Vector2(0.5f, 0.5f), Px(DeckPlatePx), "FoeDeckPlate");
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
        }

        /// <summary>按钮文字要压在按钮中间：按钮锚在右边，文字得往左挪半个按钮宽</summary>
        float EndTurnTextDx()
        {
            if (_endTurnBg == null) return 0f;
            return _endTurnBg.WorldW * 0.5f / LayoutSpace.VisibleWidth;
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

        ImageQuad HudImageTex(Transform root, Texture2D tex, float x01, float y01,
                              Vector2 anchor, float worldHeight, string name)
        {
            var q = ImageQuad.Create(root, tex, LayoutSpace.ToWorld(x01, y01),
                                     worldHeight, anchor, name);
            if (q != null)
            {
                // ⚠️ 往后放一点（相机看 +Z，z 越大越远）：HUD 文字在 z=0，
                //    同 z 的话谁压谁看渲染顺序，实测按钮底图会把文字盖住（踩过）
                q.transform.localPosition += new Vector3(0f, 0f, HudImageZ);
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

            // END TURN 的文字是压在按钮中间的，按钮宽度随分辨率变 → 文字的偏移得跟着重算
            if (_endTurnLabel != null)
                _endTurnLabel.transform.localPosition =
                    LayoutSpace.ToWorld(0.952f - EndTurnTextDx(), 0.062f);

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
            _pileLabel.SetText(CardText.Phrase("DECK") + " " + me.Deck.Count + "  " +
                               CardText.Phrase("DISC") + " " + me.Discard.Count);
            _foePileLabel.SetText(CardText.Phrase("DECK") + " " + foe.Deck.Count + "  " +
                                  CardText.Phrase("DISC") + " " + foe.Discard.Count);

            // 能量宝石：有能量亮、没能量灭（原版两张图）
            bool hasEnergy = me.Energy > 0 && Ctx.Active == _me && !Ctx.IsOver;
            if (_energyGem != null) _energyGem.gameObject.SetActive(hasEnergy);
            if (_energyGemEmpty != null) _energyGemEmpty.gameObject.SetActive(!hasEnergy);

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
                _resultLabel.SetText(CardText.Phrase(r));
                if (_hintLabel != null) _hintLabel.SetText("");      // 结束时别留着「选目标」的提示
            }
            else _resultLabel.SetText("");
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
