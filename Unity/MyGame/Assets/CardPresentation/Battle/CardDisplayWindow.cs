// CardDisplayWindow.cs — 卡牌放大展示窗（原版 `CardDisplayWindow`）· **战斗里的那处摆放**
//
// 原版出处：`资料/战斗规格/战斗重建_0827/子代理读报_front弹层_0827.md` 第一节 + 运行时 dump
//   `BattlePrefab/.../FrontCanvas/Safe area FrontCanvas/Card Display Window`（**activeSelf=False**，平时关着）
//   + 场景 JSON `bundle_scenes_scenes_battlearena1/`（2026-09-28 逐节点复读）。
//   · 遮罩 `Menu Dark Background`  4574.6×2572.4，`color = (0,0,0,0.773)`
//   · `Card Display`               752×868，屏幕正中（x[584,1336] y[106,974]）
//   · `Card Display/Cards/CardUI (0..4)` —— **5 个卡槽**（脚本字段 `cardUIs[5]`）
//   · `Card Display/LowerSection`  y 921.34~1058.34：`FlavourTextBG`(1320×178) → `LoreText`
//     (1250×93.47 · fs35 · 右对齐) + `Voice Over Button`(88.655² @1654.7→1743.4) + `Show Card Text`(同尺寸 @181.3→270)
//
// 🔴 **2026-09-28 两条订正**（判据全文 → `资料/阶段二_卡片详情窗_原版规格.md` §十）：
//   ① **卡体 743 px 是错的** —— 那是**另一棵树**的尺寸：多卡展示窗
//      `Generic Multi Card Display Combat/Viewport/Content/CardUI Reference` 的 `m_LocalScale = 223.14`
//      （`RectTransform_3421.json`）。**本窗** 5 个槽的 `m_LocalScale` **全是 250**
//      （`RectTransform_2874/2772/2908/3170/2663.json`）、`Cards`→`Card Display`→`Safe area FrontCanvas`
//      一路父级 scale 都是 1 ⇒ **本窗卡体 = 523.175×832.825 px**，**与菜单版同一个尺寸**。
//   ② **原版是同一个脚本、两处摆放**（正本 §〇·2）⇒ 两边共用 `CardFan`（扇形位姿/分层/着色）
//      与 `CardWinBox`（矩形），**判据只此一份**，不再各写一套。
//
// **怎么打开**（原版行为）：反编译 `BasicCardUI.ToggleOpenCardDisplayOnTouch` —— **轻点卡牌就开关**。
// 我们照这个做：手牌**按下→松开且几乎没移动**（`CardInteraction.TapThreshold`）就开关一次。
// **怎么关**：点遮罩空白处（原版 `BackgroundCloseButton` + `OnBackgroundClick`）/ 再轻点同一张牌。
using System.Collections.Generic;
using DG.Tweening;          // `OnComplete` 是它的扩展方法（换位收尾那一步）
using RuleEngine;
using UnityEngine;

namespace CardPresentation
{
    public class CardDisplayWindow : MonoBehaviour
    {
        /// <summary>整块窗放到 HUD **前面**（相机看 +Z，z 越小越近）。
        /// 比结算面板（−0.5）还靠前 —— 展示窗是「盖在所有东西上面」的那一层。</summary>
        const float ZMask = -2.0f, ZContent = -2.1f;
        /// <summary>卡格之间的 z 阶梯（**只是兜底**：真正决定前后的是渲染队列，见 `CardFan.QueueFor`）。</summary>
        const float ZSlotStep = 0.02f;
        /// <summary>遮罩 / 文字 / 按钮那一档队列（**低于卡格的 `CardFan.QCardBase`** ⇒ 卡永远画在它们之上）。</summary>
        const int QChrome = 3000;

        Transform _root, _cards, _lower, _bg;
        ImageQuad _mask, _voiceBtn, _eyeBtn;
        Label _lore, _hint;
        AudioSource _voiceAudio;

        /// <summary>每个**位姿槽**上的卡（下标 0 = 前台）。判据与真值 → `CardFan`。</summary>
        public readonly CardView[] SlotViews = new CardView[CardFan.Slots];
        readonly CardDef[] _slotDefs = new CardDef[CardFan.Slots];
        bool _swapping;

        /// <summary>正在展示的那张卡（`CardData` 是 struct，不能拿 null 当「没有」⇒ 单独一个 bool）</summary>
        CardData _shown;
        bool _hasShown;

        /// <summary>窗是不是开着（自检用）</summary>
        public bool Visible { get; private set; }
        /// <summary>位姿槽上有几张卡（= 1 主卡 + 相关卡数，最多 `CardFan.Slots`；自检用）</summary>
        public int SlotCount { get; private set; }
        /// <summary>**前台那张**（永远是位姿槽 0）—— 原版 `cardInFront`。</summary>
        public CardView Card { get { return SlotViews[0]; } }
        /// <summary>前台那张的卡表项（没拿到 `CardDef` 时是 null）。</summary>
        public CardDef FrontDef { get { return _slotDefs[0]; } }
        /// <summary>换位动画在播没有（= 原版那个 `swappingCards` 闸）。</summary>
        public bool IsSwapping { get { return _swapping; } }
        /// <summary>位姿槽 `i` 上的卡（越界返回 null）。</summary>
        public CardView SlotView(int i) { return (i >= 0 && i < CardFan.Slots) ? SlotViews[i] : null; }
        /// <summary>位姿槽 `i` 上那张卡的卡表项（越界返回 null）。</summary>
        public CardDef SlotDef(int i) { return (i >= 0 && i < CardFan.Slots) ? _slotDefs[i] : null; }

        /// <summary>窗口上那行字（自检用）</summary>
        public string ShownTitle { get; private set; }
        /// <summary>窗口下面那段正文（自检用）</summary>
        public string ShownBody { get; private set; }
        /// <summary>语音按钮上一次播的是哪个文件（**没播成是 null**；自检用）</summary>
        public string LastVoiceFile { get; private set; }
        /// <summary>效果文字条现在露着没有（原版 `Show Card Text` 那颗眼睛钮切它）。</summary>
        public bool LoreVisible { get; private set; }

        /// <summary>只有**前台那张**吃 tooltip（原版 `InitializeCardForDisplay` 的
        /// `Card2DController.ToggleTooltips(index == 0)`）—— 外面问这一条，不要自己数槽位。</summary>
        public static bool SlotHasTooltips(int i) { return i == 0; }

        public static CardDisplayWindow Create(Transform parent)
        {
            var go = new GameObject("Card Display Window");
            go.transform.SetParent(parent, false);
            var w = go.AddComponent<CardDisplayWindow>();
            w.Build();
            w.Hide();
            return w;
        }

        void Build()
        {
            _root = transform;

            // 遮罩：**盖满整个可见区**（原版那张是 4574.6×2572.36，两倍屏还多，就是「铺满带余量」）。
            // ⚠️ 遮罩与提示行**建一次就留着**，开关只切 activeSelf —— 每次都重建的话，
            //    `Hide` 里把它们销毁了、`Show` 又不会重建，第二次打开就只剩一张光卡（踩过）。
            _mask = MenuDraw.Rect(_root, CardArt.Solid(),
                                  new PxRect(CardWinBox.MaskL, CardWinBox.MaskT, CardWinBox.MaskR, CardWinBox.MaskB),
                                  "Menu Dark Background", QChrome, CardWinBox.ShadeColor);
            SetZ(_mask, ZMask);

            _hint = Label.Create(_root, "再点一下关闭", EndPanel.Pos(960f, 1064f, ZContent), 2,
                                 new Color(0.7f, 0.7f, 0.75f), new Vector2(0.5f, 0.5f), "cdw_hint");
            if (_hint != null) _hint.SetRenderQueue(QChrome);

            // 结构照原版那棵树：`Card Display` → { `Cards`(5 个卡槽), `LowerSection` → `FlavourTextBG` → `LoreText` }
            var cardDisplay = MenuDraw.Node(_root, "Card Display",
                                            new PxRect(CardWinBox.CardL, CardWinBox.CardT, CardWinBox.CardR, CardWinBox.CardB));
            _cards = MenuDraw.Node(cardDisplay, "Cards",
                                   new PxRect(CardWinBox.CardsCx - CardWinBox.CardsS * 0.5f, CardWinBox.CardsCy - CardWinBox.CardsS * 0.5f,
                                              CardWinBox.CardsCx + CardWinBox.CardsS * 0.5f, CardWinBox.CardsCy + CardWinBox.CardsS * 0.5f));
            _lower = MenuDraw.Node(cardDisplay, "LowerSection",
                                   new PxRect(CardWinBox.LowerL, CardWinBox.LowerT, CardWinBox.LowerR, CardWinBox.LowerB));
            _bg = MenuDraw.Node(_lower, "FlavourTextBG",
                                new PxRect(CardWinBox.LowerL, CardWinBox.LoreBgT, CardWinBox.LowerR, CardWinBox.LoreBgB));

            // 两颗圆钮：**图在工程里才建**（`Resources/Art/ui/` 被删时退回「没有这颗钮」，
            // 而不是摆一块白方块 —— 那就是「静默失败」了）。**建不建看卡**在 `RefreshButtons` 里切。
            float vx = CardWinBox.VoiceCx - CardWinBox.BtnS * 0.5f, ex = CardWinBox.EyeCx - CardWinBox.BtnS * 0.5f;
            float by = CardWinBox.BtnCy - CardWinBox.BtnS * 0.5f;
            _voiceBtn = MenuDraw.Rect(_lower, CardArt.Ui("40k_UI_bt_voicelines"),
                                      new PxRect(vx, by, vx + CardWinBox.BtnS, by + CardWinBox.BtnS),
                                      "Voice Over Button", QChrome, null, true);
            _eyeBtn = MenuDraw.Rect(_lower, CardArt.Ui("40k_UI_bt_eye"),
                                    new PxRect(ex, by, ex + CardWinBox.BtnS, by + CardWinBox.BtnS),
                                    "Show Card Text", QChrome, null, true);
            if (CardArt.Ui("40k_UI_bt_voicelines") == null)
                Debug.Log("[展示窗] `40k_UI_bt_voicelines` 不在 `Resources/Art/ui/` ⇒ **语音钮不建**（不摆白方块）");

            _voiceAudio = gameObject.AddComponent<AudioSource>();
            _voiceAudio.playOnAwake = false;
            _voiceAudio.spatialBlend = 0f;          // 2D —— 原版这个 source 也挂在 UI 上
            _voiceAudio.volume = 0.85f;             // ⚠️ 这个系数是**我们自己定的**（原版逐 source 的音量没查到）
            // 🆕 2026-09-19：挂到 `Voices` 通道 ⇒ 受设置面板那根「语音」滑块控制
            _voiceAudio.outputAudioMixerGroup = WarpforgeAudio.VoicesGroup;

            LoreVisible = true;
            SetChrome(false);
        }

        static void SetZ(ImageQuad q, float z)
        {
            if (q == null) return;
            var p = q.transform.localPosition; q.transform.localPosition = new Vector3(p.x, p.y, z);
        }

        /// <summary>把一个节点推到指定的 z。
        /// 🔴 **`MenuDraw` 建出来的东西 z 一律是 0**（它只算 x/y），而本窗的遮罩在 `ZMask = −2.0`
        /// —— 相机看 +Z、**z 越小越靠前** ⇒ 不推的话**文字/底图会被遮罩压住变暗**
        /// （2026-09-28 写完当场抓到的，同 `CardDetailPopup` 那条「遮罩画到卡上面」是一族）。</summary>
        static void SetZ(Transform t, float z)
        {
            if (t == null) return;
            var p = t.localPosition; t.localPosition = new Vector3(p.x, p.y, z);
        }

        void SetChrome(bool on)
        {
            if (_mask != null) _mask.gameObject.SetActive(on);
            if (_hint != null) _hint.gameObject.SetActive(on);
            if (_voiceBtn != null) _voiceBtn.gameObject.SetActive(on);
            if (_eyeBtn != null) _eyeBtn.gameObject.SetActive(on);
        }

        // ============================================================ 命中（战斗侧不走 PointerLayer，按 px 判）

        /// <summary>点在**哪一格**上（−1 = 没点到任何卡）。**前台优先**：
        /// 前面的卡盖住后面的，所以从槽 0 往后找、先命中的就是它。
        /// 命中区比卡面**小一圈**：原版点击接收器是 `CardUI/2DCard/UI Collider`
        /// —— 拉伸锚 + `sd(-0.2,-0.44)` ⇒ **473.18×722.83**，而卡本体是 523.25×832.75 ⇒ 比例见 `CardFan.HitRatio`。</summary>
        public int HitSlot(Vector3 world)
        {
            if (!Visible) return -1;
            var px = LayoutSpace.ToPixel(world);
            for (int i = 0; i < SlotCount && i < CardFan.Slots; i++)
            {
                float hw = CardFan.Wpx(i) * CardFan.HitRatio * 0.5f;
                float hh = CardFan.Hpx(i) * CardFan.HitRatio * 0.5f;
                float dx = px.x - CardFan.Cx(i), dy = px.y - CardFan.Cy(i);
                if (Mathf.Abs(dx) <= hw && Mathf.Abs(dy) <= hh) return i;
            }
            return -1;
        }

        /// <summary>语音按钮被点到了没有（px 判定，和别处同一套换算）。</summary>
        public bool HitVoice(Vector3 world)
        {
            if (_voiceBtn == null || !_voiceBtn.gameObject.activeSelf) return false;
            return HitBtn(world, CardWinBox.VoiceCx);
        }

        /// <summary>「显示卡面文字」那颗眼睛钮被点到了没有。</summary>
        public bool HitEye(Vector3 world)
        {
            if (_eyeBtn == null || !_eyeBtn.gameObject.activeSelf) return false;
            return HitBtn(world, CardWinBox.EyeCx);
        }

        static bool HitBtn(Vector3 world, float cx)
        {
            var px = LayoutSpace.ToPixel(world);
            float h = CardWinBox.BtnS * 0.5f;
            return Mathf.Abs(px.x - cx) <= h && Mathf.Abs(px.y - CardWinBox.BtnCy) <= h;
        }

        /// <summary>
        /// 点语音按钮 → 播**正在展示的那张卡**的单位语音（原版 `voiceOverButton` + `voiceOverAudioSource`）。
        /// 判据（哪条语音）复用 `VoiceLines.TryPick` —— **只此一处**，不另写一张后缀表。
        /// ⚠️ 这张卡没有语音时**说出来**（红线：不许静默失败），返回 false。
        /// </summary>
        public bool PlayVoice()
        {
            LastVoiceFile = null;
            if (!_hasShown) return false;      // 窗里没有卡（`CardData` 是 struct，用它判「有没有」）
            // ⚠️ **`_shown.id` 是「名字」不是卡 id**（`ToCardData` 的注释：`id` 兼着显示名/配对的活）——
            //    语音表按**引擎卡 id** 索引 ⇒ 要用 `artId`（原版卡 = 卡 id；自造卡 = 卡名，自然查不到）。
            //    判据只此一份：`CardButtons.VoiceKey`。
            string key = CardButtons.VoiceKey(_shown);
            if (string.IsNullOrEmpty(key)) return false;
            if (!VoiceLines.Has(key))
            {
                Debug.Log($"[展示窗] 「{_shown.title}」没有单位语音（`VoiceLines.Has(\"{key}\")` 为假）—— 语音按钮这次没声音");
                return false;
            }
            string file, text;
            // `rng` 传 null = 取第一条（定死、可复现）；这个按钮是「听一下」，不需要随机
            // ⚠️ 用 `ForVoiceOver`（**随便哪条都行**）—— 借 `ForDeploy` 会挂空：有的卡只有 attack/death
            if (!VoiceLines.TryPick(key, VoiceLines.ForVoiceOver, null, out file, out text)) return false;
            var clip = VoiceLines.Clip(file);
            if (clip == null || _voiceAudio == null) return false;
            _voiceAudio.PlayOneShot(clip);
            LastVoiceFile = file;
            return true;
        }

        /// <summary>眼睛钮：切开/切掉效果文字条（原版 `showCardTextButton` → `ToggleCardState`）。
        /// 切的是**整块 `FlavourTextBG`**（底板 + 那行字一起）—— 原版也是切那一整块。</summary>
        public void ToggleLore()
        {
            LoreVisible = !LoreVisible;
            if (_bg != null) _bg.gameObject.SetActive(LoreVisible);
            Debug.Log("[展示窗] 效果文字条 " + (LoreVisible ? "露出" : "收起") + "（原版 `Show Card Text`）");
        }

        // ============================================================ 开 / 关

        /// <summary>开/关一次。已开着就关掉（原版那个方法名就是 `Toggle...`）。</summary>
        public void Toggle(CardData d, CardDef def) { if (Visible) Hide(); else Show(d, def); }

        /// <summary>开窗（不带卡表项 —— 只显示主卡、没有相关卡，并**出声**）。</summary>
        public void Show(CardData d) { Show(d, null); }

        /// <summary>开窗。`def` 是这张卡的**引擎卡表项** —— **相关卡要它才算得出来**（没有就只画主卡）。</summary>
        public void Show(CardData d, CardDef def)
        {
            Hide();                       // 先把上一次那一叠拆干净（换阵营要换卡框贴图，原地改不换图）

            ShownTitle = d.title;
            ShownBody = d.keywords;
            _shown = d; _hasShown = true;   // 语音按钮要按它查单位语音
            LastVoiceFile = null;
            LoreVisible = true;             // 原版每次 `ShowCard` 都把 lore 复位

            BuildFan(d, def);
            RefreshLore();
            RefreshButtons();

            SetChrome(true);
            Visible = true;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            Visible = false;
            ShownTitle = null;
            ShownBody = null;
            _hasShown = false;
            LastVoiceFile = null;
            _swapping = false;
            SlotCount = 0;
            for (int i = 0; i < CardFan.Slots; i++) { SlotViews[i] = null; _slotDefs[i] = null; }
            if (_cards != null) MenuDraw.ClearChildren(_cards);
            if (_bg != null) MenuDraw.ClearChildren(_bg);
            _lore = null;
            SetChrome(false);
        }

        // ============================================================ 卡片那一叠（1 主卡 + 8 相关卡）

        /// <summary>把「1 主卡 + N 相关卡」按**原版那条 clip 的扇形**摆出来（判据与真值 → `CardFan`）。
        /// ⚠️ 位姿槽 0 = 前台（原版 `cardUIs[0]`：屏心偏上 (960,480) · scale 250 · 转角 0）。</summary>
        void BuildFan(CardData d, CardDef def)
        {
            var cards = new List<CardData>();
            var defs = new List<CardDef>();
            cards.Add(d); defs.Add(def);
            if (def != null)
            {
                // 相关卡：判据只此一份（`RelatedCards.Find`），与原版那 4 个 `relatedCard1..4` 字段的关系 → 那个文件头
                foreach (var r in RelatedCards.Find(def, CardFan.Slots - 1))
                {
                    cards.Add(BattleDriver.ToCardData(r, r.Faction));
                    defs.Add(r);
                }
            }
            else
            {
                Debug.LogWarning("[展示窗] 没拿到卡表项（`CardDef`）⇒ **只显示主卡、没有相关卡**"
                               + "（相关卡是按卡表算的；不静默）");
            }

            SlotCount = Mathf.Min(cards.Count, CardFan.Slots);
            _swapping = false;
            for (int i = 0; i < CardFan.Slots; i++) { SlotViews[i] = null; _slotDefs[i] = null; }

            for (int i = 0; i < SlotCount; i++)
            {
                var cd = cards[i];
                var v = CardView.Create(_cards, cd, CardFan.SlotName(i));
                if (v == null)
                {
                    Debug.LogWarning("[展示窗] 第 " + i + " 格（" + cd.title + "）的 `CardView` 建不出来 —— 那一格没画，出声");
                    continue;
                }
                v.gameObject.SetActive(true);
                v.SetPose(SlotLocal(i), CardFan.Rot(i), CardFan.Hpx(i) / (CardView.Height * 108f));
                v.SetData(cd);
                v.SetFace(CardFace.Full);
                v.SetHighlight(CardHighlightState.Normal);   // 展示窗里的卡不吃「可出牌/不可出牌」那套状态色
                CardFan.ApplySlotTint(v, i == 0);            // 前台白 / 相关卡压暗（原版 `cardInBackGroundColorTint`）
                SlotViews[i] = v; _slotDefs[i] = defs[i];
            }
            CardFan.ApplyQueues(SlotViews, SlotCount, CardFan.QCardBase);
            Debug.Log($"[展示窗] 卡片那一叠：**{SlotCount} 格**（1 主卡 + {Mathf.Max(0, SlotCount - 1)} 相关卡）"
                    + " —— 位姿：槽 0–4 照原版 clip `Card Display Open`，槽 5–8 是**我们外推的**（`CardFan` 文件头）");
        }

        /// <summary>第 `i` 格的**局部坐标**（相对 `_cards`；z 按槽位阶梯，只作兜底，真正排序看渲染队列）。
        /// ⚠️ 走 `MenuDraw.Local` **而不是**直接拿 `EndPanel.Pos` —— 那样只有在「窗根正好在世界原点」时才对。</summary>
        Vector3 SlotLocal(int i)
        {
            float w = CardFan.Wpx(i), h = CardFan.Hpx(i);
            var l = MenuDraw.Local(_cards, CardFan.Cx(i) - w * 0.5f, CardFan.Cy(i) - h * 0.5f,
                                            CardFan.Cx(i) + w * 0.5f, CardFan.Cy(i) + h * 0.5f);
            return new Vector3(l.x, l.y, ZContent - i * ZSlotStep);
        }

        /// <summary>点第 `idx` 格 ⇒ **和前台那张两两换位**（原版 `CardDisplayWindow.ChangeCardPosition`）。
        /// **三个闸**（任一中就整个 return，什么都不做）：① 上一次没播完（`swappingCards`）·
        /// ② 点的是前台自己 · ③ 有 `Animation` 在播（⚠️ **本工程不适用** —— 原版摆位那条 legacy clip
        /// 我们没有，我们按 `CardFan` 的常量直接摆；这一点是**如实说明的偏离**）。
        /// 收尾照原版 `CardSwapFinished`：开闸 · 换前台指针 · **重设 lore / 语音 / 眼睛钮** ·
        /// **换色补间**（去前台变白、让位的压暗到 `cardInBackGroundColorTint`）。</summary>
        public void SwapToFront(int idx)
        {
            if (_swapping)
            {
                Debug.Log("[展示窗] 换位被挡：上一次还没播完（原版闸① `swappingCards`）");
                return;
            }
            if (idx <= 0 || idx >= CardFan.Slots) return;                  // 闸②：点的是前台自己（位姿槽 0）
            var a = SlotViews[0]; var b = SlotViews[idx];
            if (a == null || b == null) return;
            _swapping = true;

            var pa = a.transform.position; var pb = b.transform.position;
            float ra = a.transform.eulerAngles.z, rb = b.transform.eulerAngles.z;
            float sa = a.transform.localScale.x, sb = b.transform.localScale.x;

            // 六条 tween 全用同一个时长（原版 `relatedCardSwapTime` = 0.25s）：
            // 被点那张 → 前台位；前台那张 → 被点卡的槽位。
            CardTween.ToPose(b.transform, pa, ra, sa, CardFan.SwapTime, DG.Tweening.Ease.OutQuad).OnComplete(() =>
            {
                // 记账：两个**位姿槽**上的卡对调（前台永远是位姿槽 0）
                var td = _slotDefs[0]; _slotDefs[0] = _slotDefs[idx]; _slotDefs[idx] = td;
                var tv = SlotViews[0]; SlotViews[0] = SlotViews[idx]; SlotViews[idx] = tv;
                // 🔴 **层级按新的槽位重排**（原版那一步是 `SetSiblingIndex` 两两互换 —— 目的就是
                //    「现在站在前台的那张画在最上面」；我们这套的前后由**渲染队列**决定，见 `CardFan`）。
                CardFan.ApplyQueues(SlotViews, SlotCount, CardFan.QCardBase);
                _swapping = false;
                // 收尾（原版 `CardSwapFinished`）：重设 lore / 语音 / 眼睛钮 —— 认的都是**新前台**那张
                if (SlotViews[0] != null)
                {
                    _shown = SlotViews[0].Data;
                    ShownTitle = _shown.title;
                    ShownBody = _shown.keywords;
                }
                RefreshLore();
                RefreshButtons();
                Debug.Log("[展示窗] 换位完成：前台现在是「" + (SlotViews[0] != null ? ShownTitle : "?")
                        + "」（收尾重设了 lore / 语音 —— 原版 `CardSwapFinished`）");
            });
            CardTween.ToPose(a.transform, pb, rb, sb, CardFan.SwapTime, DG.Tweening.Ease.OutQuad);
            // 着色跟着换：去前台那张 → 白，让出前台那张 → `cardInBackGroundColorTint`
            // （原版 `ChangeCardPosition.c:203-221` 那两条 tween，同一时长）。
            CardFan.TweenTint(b, CardFan.BackTint, CardFan.FrontTint, CardFan.SwapTime);
            CardFan.TweenTint(a, CardFan.FrontTint, CardFan.BackTint, CardFan.SwapTime);
            Debug.Log($"[展示窗] 换位：第 {idx} 格 ⇄ 前台（{CardFan.SwapTime}s = 原版 `relatedCardSwapTime`）");
        }

        // ============================================================ 下缘那条（文字 + 两颗钮）

        /// <summary>效果文字条那行字。原版这里是 **Lore 背景故事**；我们没有 lore 文案
        /// （I2 词条在远端，本地拿不到 —— 正本 §十·2 已结案）⇒ 画**效果文字**，并**如实标注**。
        /// ⚠️ 换位收尾（原版 `CardSwapFinished` 的 `SetCardLore(新前台)`）会**重建**它 ⇒ 抽成独立方法。</summary>
        void RefreshLore()
        {
            if (_bg == null) return;
            MenuDraw.ClearChildren(_bg);
            _lore = null;
            // ⚠️ **先亮起来再建字** —— TMP 在**未激活**的对象里量不出尺寸（`textBounds` 是垃圾，
            //    实测能到 4.29e9 ⇒ 面板宽度顶到上限、一个字看不见）。最后再按开关收起/露出。
            _bg.gameObject.SetActive(true);

            // ① 底板：**按阵营**选图（原版 `FlavourTextSO.GetClanFlavorBackground`）—— 认的是**当前前台**那张。
            //    ⚠️ 原版 `FlavourTextBG` 的 `m_PreserveAspect = 0`（实读）⇒ **拉伸**铺满，别开 keepAspect。
            string fac = _hasShown ? _shown.faction : null;
            var tex = CardArt.FlavorBg(fac);
            if (tex != null)
                SetZ(MenuDraw.Rect(_bg, tex,
                                   new PxRect(CardWinBox.LowerL, CardWinBox.LoreBgT, CardWinBox.LowerR, CardWinBox.LoreBgB),
                                   "Image", QChrome, null, false), ZContent);
            else
                Debug.LogWarning($"[展示窗] 阵营「{fac}」没有风味底图（`CardArt.FlavorBg` 取不到）⇒ **只画字**（不静默）");

            // ② 那行字
            string body = ShownBody ?? "";
            if (string.IsNullOrEmpty(body)) { _bg.gameObject.SetActive(LoreVisible); return; }
            var r = new PxRect(CardWinBox.LoreL, CardWinBox.LoreT, CardWinBox.LoreR, CardWinBox.LoreB);
            // 原版 `LoreText`：**fs35**（auto 10–35）· 限宽换行 · **居中** · 白
            // 🔴 **2026-09-28：不是右对齐** —— `m_HorizontalAlignment = 2`（Center，位标志 Left=1/Center=2/Right=4），
            //    实拍也一致（正本 §十·5 第 4 条）。`MenuDraw.Text` 默认就是居中 ⇒ **不要** `AlignRight`。
            _lore = MenuDraw.Text(_bg, r, body, Color.white, "LoreText", CardWinBox.LorePx, QChrome, r.W, 10f);
            SetZ(_lore != null ? _lore.transform : null, ZContent);
            if (_lore == null)
                Debug.LogWarning("[展示窗] 效果文字条建不出来（`MenuDraw.Text` 返回 null）—— 不静默");
            _bg.gameObject.SetActive(LoreVisible);
        }

        /// <summary>遮罩用的纯色贴图（`ImageQuad` 得有一张图才肯建）。宽高比按传入的来。
        /// ⚠️ **`MultiCardDisplay` 也用这一个**（两张遮罩是同一个作用，别写第二份）。
        /// 🔴 2026-09-28 本文件重写时**一度把它弄丢**（`MultiCardDisplay` 三处调用立刻编译不过）——
        ///    别再删：它不是本窗私有的。</summary>
        static Texture2D _solid;
        static float _solidAspect;
        internal static Texture2D SolidTexFor(float aspect)
        {
            if (_solid != null && Mathf.Abs(_solidAspect - aspect) < 0.01f) return _solid;
            int h = 32;
            int w = Mathf.Max(4, Mathf.RoundToInt(h * aspect));
            _solid = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            _solid.SetPixels(px);
            _solid.Apply();
            _solidAspect = aspect;
            return _solid;
        }

        /// <summary>两颗钮**按卡决定建不建**（原版 `CardDisplayWindow.ShowCard` / `CardSwapFinished`）。
        /// 判据只此一份 → `CardButtons`（那里写了原版出处与我们的等价物）。</summary>
        void RefreshButtons()
        {
            bool voice = _hasShown && CardButtons.HasVoice(_shown);
            if (_voiceBtn != null)
            {
                _voiceBtn.gameObject.SetActive(voice);
                if (voice) SetZ(_voiceBtn, ZContent);
            }
            bool eye = _hasShown && CardButtons.HasTextButton(_shown);
            if (_eyeBtn != null)
            {
                _eyeBtn.gameObject.SetActive(eye);
                if (eye) SetZ(_eyeBtn, ZContent);
            }
        }
    }
}
