// CardInteraction.cs — 手牌交互的驱动：悬停 / 拖拽 / 落位 / 回弹
//
// 这是把前面几个模块串起来的那一层：
//   指针 → 命中哪张牌 → 悬停（抬起 + 邻牌让位）
//        → 按下 → 拖拽（跟着指针走、其他牌补位）
//        → 松手 → 落在格位上（DeploySequence）或弹回手牌（DOTween）
//
// 输入用的是**新输入系统**（本工程 `activeInputHandler: 1`，旧输入 API 会直接抛异常）。
// 鼠标和触摸都读，同一套逻辑 —— 后面上手机不用改这里。
//
// 批处理自检怎么验：把 `CardTween.Mode` 设成 Manual，然后手动喂指针位置 + `Advance(dt)`，
// 见 `CardBaseDemo`。所以这里**不要**写死任何跟 Time.deltaTime 有关的东西。
using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CardPresentation
{
    public class CardInteraction : MonoBehaviour
    {
        public HandLayout hand;
        public BoardLayout board;
        /// <summary>
        /// **敌方半场**。战术卡不落格位、而是**打到某个单位身上**，所以它要能落到对面那一半
        /// （`Deal 3 damage to an enemy` 打的就是敌方单位）。没接也能跑 —— 就是打不了敌方目标。
        /// ⚠️ 两个棋盘的**槽号是同一套**（0–8），光看槽号分不出哪边，所以落点判定必须同时返回棋盘。
        /// </summary>
        public BoardLayout foeBoard;
        public Camera cam;

        [Tooltip("拖拽时放大到多少 —— **乘在手牌缩放上**（手牌缩放随分辨率变，写死绝对值会在 4:3 上炸开）")]
        public float dragScale = 1.16f;

        [Tooltip("拖起来时往前挪多少 z（越小越靠近相机）")]
        public float dragLiftZ = 0.6f;

        [Tooltip("悬停/拖拽中的牌额外前移，避免被旁边的压住")]
        public float frontZ = 0.5f;

        public event Action<CardView, int> OnDeployed;     // 落位成功
        public event Action<CardView> OnReturned;          // 回弹到手牌
        /// <summary>**轻点**了一张手牌（按下→松开，几乎没移动）。
        /// 原版这个动作是 `BasicCardUI.ToggleOpenCardDisplayOnTouch` —— 开关卡牌展示窗。
        /// 和「拖出去又放回来」区分开：拖过就不算轻点。</summary>
        public event Action<CardView> OnTapped;

        /// <summary>算不算「轻点」的位移阈值（世界单位）。13 px @1080p —— 手抖一下不算拖</summary>
        public const float TapThreshold = 0.12f;

        /// <summary>
        /// 落点合法性 —— **由外部注入**（战斗驱动层接规则引擎的 `RuleCore.CanPlayCard`）。
        /// 返回 false 就回弹。默认全放行（不接引擎时，表现层自检照样能跑）。
        /// 拖拽过程中的高亮和松手时的判定**都走这一个委托**，不另写一份。
        /// </summary>
        public Func<int, CardView, bool> CanDropAtSlot = (slot, card) => true;

        readonly List<CardView> _cards = new List<CardView>();
        readonly Dictionary<int, CardView> _placed = new Dictionary<int, CardView>();
        Vector3 _pressWorld;      // 按下时指针在哪（判「轻点」用）
        float _travel;            // 这一拖走了多远

        int _hovered = -1;
        CardView _dragging;
        int _insertIndex = -1;        // 拖拽中：这张牌会插到第几个位置（手牌边拖边让位）
        Vector3 _grabOffset;          // 抓起时指针和卡中心的差，拖拽时保持它，手感才不跳

        public int HoveredIndex { get { return _hovered; } }
        public CardView Dragging { get { return _dragging; } }
        public bool IsDragging { get { return _dragging != null; } }
        public int InsertIndex { get { return _insertIndex; } }
        public IReadOnlyDictionary<int, CardView> Placed { get { return _placed; } }

        public void SetCards(List<CardView> cards)
        {
            _cards.Clear();
            _cards.AddRange(cards);
            Relayout();
        }

        /// <summary>
        /// 清掉「哪些槽已经摆了牌」的记录。**重开一局必须调** ——
        /// 不清的话上一局摆过的槽全都还占着，新一局往那些槽拖会被判成「这格有牌」而弹回手上，
        /// 表现为「打过的格子再也放不了牌」（2026-09-12 接原版卡牌时在自检里撞到的：
        /// 第二次 `Begin()` 之后拖第一张就落不下去，根因就是这里）。
        /// </summary>
        public void ClearPlaced()
        {
            foreach (var kv in _placed)
                if (kv.Value != null) kv.Value.SetHighlight(CardHighlightState.Normal);
            _placed.Clear();
            _dragging = null;
            _insertIndex = -1;
            _hovered = -1;
        }

        void Update()
        {
            if (cam == null || hand == null || board == null) return;
            if (_cards.Count == 0) return;

            Vector3 world = PointerWorld();

            if (_dragging == null) UpdateHover(world);
            else UpdateDrag(world, Time.deltaTime);

            // 松手/按下在 Update 里轮询（批处理下没有输入事件，轮询才验得了）
            if (PressedThisFrame())
            {
                if (_hovered >= 0 && _dragging == null) PickUp(_cards[_hovered], world);
            }
            else if (ReleasedThisFrame() && _dragging != null)
            {
                Release(world);
            }
        }

        // ---- 悬停 ----
        void UpdateHover(Vector3 world)
        {
            int hit = HitTest(world);
            if (hit == _hovered) return;
            _hovered = hit;
            Relayout();
        }

        // ---- 拖拽 ----
        void PickUp(CardView card, Vector3 world)
        {
            _dragging = card;
            _grabOffset = card.transform.position - world;
            _pressWorld = world;
            _travel = 0f;

            // 空位先落在它自己原来的位置 —— 之后跟手往哪拖，空位就跟到哪
            _insertIndex = Mathf.Clamp(_cards.IndexOf(card), 0, Mathf.Max(0, _cards.Count - 1));
            _hovered = -1;

            card.SetHighlight(CardHighlightState.Selected);
            CardTween.ToPose(card.transform,
                             card.transform.position + new Vector3(0f, 0f, -dragLiftZ),
                             card.transform.eulerAngles.z,
                             hand.cardScale * LayoutSpace.Scale * dragScale,
                             CardTween.PickUpDuration, Ease.OutQuad);

            Relayout();                    // 手牌把这张空出来，邻牌补位
            board.SetDragHighlight(true);  // 格位亮起来，告诉玩家「能往这儿放」
        }

        void UpdateDrag(Vector3 world, float dt)
        {
            // 拖拽跟手：直接跟，但带一点平滑（完全硬跟会显得很生硬）。
            // ⚠️ 平滑系数要**按 dt 算**，不能用固定的 0.45 —— 那样帧率一变手感就变，
            //    而且批处理自检里手动喂 dt 时推不到位（踩过）。
            var t = _dragging.transform;
            var target = world + _grabOffset + new Vector3(0f, 0f, -dragLiftZ);
            t.position = Vector3.Lerp(t.position, target, 1f - Mathf.Exp(-20f * dt));

            // 记下这一拖最远走到哪 —— 松手时用它区分「轻点」和「真拖」（只有前者开关展示窗）
            float d = Vector3.Distance(world, _pressWorld);
            if (d > _travel) _travel = d;

            // ---- 手牌插槽：边拖边让位（原版 GetClosestInHandSlot）----
            // 只有插槽号变了才重排 —— 每帧重排会把补间反复打断。
            // ⚠️ 只在**手牌这一带**（棋盘线以下）才跟着走：拖到战场上以后还按 x 重排的话，
            //    你在上面选位置、下面的手牌会自己滑来滑去，看着像 bug
            if (LayoutSpace.ToNormalized(t.position).y < board.lineY)
            {
                var visible = VisibleCards();
                int slot = hand.GetClosestInHandSlot(t.position, visible.Count);
                if (slot != _insertIndex)
                {
                    _insertIndex = slot;
                    Relayout();
                }
            }

            // 落点是否合法 → 实时反映在卡的着色上
            int boardSlot;
            BoardLayout which;
            bool ok = ResolveDrop(t.position, _dragging, out boardSlot, out which);
            _dragging.SetHighlight(ok ? CardHighlightState.ValidTarget : CardHighlightState.Selected);
        }

        /// <summary>这张手牌是战术卡吗（战术卡**不落格位**，它是打到某个单位上的）</summary>
        static bool IsTactic(CardView c)
        {
            return c != null && !c.Data.isUnit;
        }

        /// <summary>
        /// 指针现在指着的落点合不合法。
        ///
        /// **战术卡和单位卡的落点规则不一样**（2026-09-12 接战术卡时分的）：
        ///   · 单位卡：落在**自己半场的空格**上，而且这格本回合还没摆过（`_placed`）；
        ///   · 战术卡：落在**一个单位**上（自己或对面的都行，由卡面文本决定），**不占格位** ——
        ///     所以既不该要求「这格空着」，也不该因为「这格本回合摆过牌」就被拒。
        /// 合法性本身仍然只有**引擎一处**说了算（`CanDropAtSlot` → `RuleCore.CanPlayCard`）。
        /// </summary>
        bool ResolveDrop(Vector3 pos, CardView card, out int slot, out BoardLayout which)
        {
            slot = -1;
            which = board;
            if (card == null) return false;
            bool tactic = IsTactic(card);
            int s;

            // 战术卡先试**敌方半场**（打敌方目标的那一类）
            if (tactic && foeBoard != null && foeBoard.TryResolveSlot(pos, out s)
                && CanDropAtSlot(s, card))
            {
                slot = s; which = foeBoard; return true;
            }

            if (board == null || !board.TryResolveSlot(pos, out s)) return false;
            if (!CanDropAtSlot(s, card)) return false;
            if (!tactic && _placed.ContainsKey(s)) return false;     // 这一格本回合已经摆过牌了
            slot = s; which = board;
            return true;
        }

        void Release(Vector3 world)
        {
            var card = _dragging;
            _dragging = null;
            board.SetDragHighlight(false);
            bool tapped = _travel < TapThreshold;      // 几乎没动 = 轻点

            int slot;
            BoardLayout which;
            bool ok = ResolveDrop(card.transform.position, card, out slot, out which);

            if (ok)
            {
                _insertIndex = -1;
                bool tactic = IsTactic(card);
                // ⚠️ 战术卡**不占格位** —— 记进 `_placed` 的话，那个格位本回合就再也放不了牌了
                if (!tactic) _placed[slot] = card;
                // 从手牌里拿掉：之后的手牌重排不该再算它
                _cards.Remove(card);
                card.SetHighlight(CardHighlightState.Normal);
                var tw = DeploySequence.Play(card, which.SlotPosition(slot),
                                             which.placedScale * LayoutSpace.Scale,
                                             () => { if (OnDeployed != null) OnDeployed(card, slot); });
                tw.SetUpdate(CardTween.Mode);
                // 🔴 **出牌那个特效不在这里播**（2026-09-15 修）—— 驱动层收引擎的 `EvtKind.Play`
                //    事件时已经在**格位**上播了同一个 `VfxMap.PlayCard`，这里再播一次等于**打一张牌
                //    播两遍**（单位卡再叠一个 `Deploy`，一共三个）。
                //    留驱动层那一个，理由和别处一样：**事件只有引擎那一条**，表现层自己猜时机迟早会分叉。
                Debug.Log($"[CardPresentation] 落位：{card.name} → "
                        + (tactic ? "战术卡打向" : "槽")
                        + $" {slot}{(which == foeBoard ? "（敌方半场）" : "")}"
                        + (which.IsWarlord(slot) ? "（督军位）" : ""));
            }
            else
            {
                // 拖到哪就插到哪 —— 松手时把手牌顺序真的改成拖拽时让出来的那个顺序
                if (_insertIndex >= 0)
                {
                    int from = _cards.IndexOf(card);
                    if (from >= 0)
                    {
                        _cards.RemoveAt(from);
                        _cards.Insert(Mathf.Clamp(_insertIndex, 0, _cards.Count), card);
                    }
                    _insertIndex = -1;
                }

                // 回弹：回到扇形里的原位
                int idx = Mathf.Max(0, _cards.IndexOf(card));
                var home = hand.SlotPosition(idx, Mathf.Max(1, _cards.Count));
                var seq = DOTween.Sequence();
                seq.Join(card.transform.DOMove(home, CardTween.SnapBackDuration));
                seq.Join(card.transform.DOScale(Vector3.one * hand.cardScale * LayoutSpace.Scale,
                                                CardTween.SnapBackDuration));
                CardTween.Use(seq, Ease.OutQuad, card.transform);
                card.SetHighlight(CardHighlightState.Normal);
                if (OnReturned != null) OnReturned(card);
                Debug.Log($"[CardPresentation] 落点不合法，{card.name} 回弹到手牌第 {idx} 位");
            }

            // 轻点（按下→松开几乎没动）：**没落位**才算 —— 顺手拍了张牌上场不该弹展示窗。
            // 原版这个动作是 `BasicCardUI.ToggleOpenCardDisplayOnTouch`。
            if (tapped && !ok && OnTapped != null) OnTapped(card);

            _hovered = -1;
            Relayout();
        }

        /// <summary>手牌里当前**看得见**的那几张（拖拽中的那张不参与重排）</summary>
        List<CardView> VisibleCards()
        {
            var list = new List<CardView>();
            foreach (var c in _cards) if (c != null && c != _dragging) list.Add(c);
            return list;
        }

        /// <summary>
        /// 手牌重排。拖拽中的那张不参与（它自己跟手），
        /// 但它会在 `_insertIndex` 处**留一个空位**，其余牌按多一张的间距让开。
        /// </summary>
        public void Relayout()
        {
            var list = VisibleCards();
            hand.Refresh(list, HoveredIn(list), _dragging != null ? _insertIndex : -1);
        }

        int HoveredIn(List<CardView> visible)
        {
            if (_hovered < 0 || _hovered >= _cards.Count) return -1;
            var h = _cards[_hovered];
            if (h == null) return -1;
            int i = visible.IndexOf(h);
            return i;
        }

        /// <summary>命中哪张手牌。**从最前面往后测**，不然会选中被压住的那张。</summary>
        int HitTest(Vector3 world)
        {
            int best = -1;
            float bestZ = float.MaxValue;
            for (int i = 0; i < _cards.Count; i++)
            {
                var c = _cards[i];
                if (c == null || !c.Contains(world)) continue;
                float z = c.transform.position.z;
                if (z < bestZ) { bestZ = z; best = i; }     // z 越小越靠前
            }
            return best;
        }

        // ---- 输入（新输入系统）----
        static Vector2 PointerScreen()
        {
            if (Mouse.current != null) return Mouse.current.position.ReadValue();
            if (Touchscreen.current != null) return Touchscreen.current.primaryTouch.position.ReadValue();
            return Vector2.zero;
        }

        static bool PressedThisFrame()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
            return false;
        }

        static bool ReleasedThisFrame()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame) return true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame) return true;
            return false;
        }

        Vector3 PointerWorld()
        {
            return LayoutSpace.ScreenToWorld(PointerScreen(), cam);
        }

        // ---- 给批处理自检用的显式接口（不进 play 模式也能走完整流程）----
        public void SimulateHover(Vector3 world) { UpdateHover(world); }
        public void SimulatePress(Vector3 world) { if (_hovered >= 0 && _dragging == null) PickUp(_cards[_hovered], world); }
        public void SimulateDrag(Vector3 world, float dt) { if (_dragging != null) UpdateDrag(world, dt); }
        public void SimulateRelease(Vector3 world) { if (_dragging != null) Release(world); }
    }
}
