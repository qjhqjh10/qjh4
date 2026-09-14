// CardChoicePanel.cs — 原版那套「横向选卡面板」的**共用壳**（选牌 / 选效果 / 换牌）
//
// **为什么有它**：原版这两个面板（`ChooseCardMenu` 与 `Mulligan`）是**两份克隆** ——
// 两棵节点树**逐节点同构**：`HideChooseButton` / `HideMulliganButton` 同图 `40k_UI_bt_eye`；
// 两个文字框的 pos/size/pivot **完全相同**（`7.0,-106.5` / `1344.0,79.4` / `0.5,1.0`）；
// 连每张牌下面那颗按钮用的都是**同一张图** `UI_Button_Mulligan`（只有文案不同：`Select` / `换`）。
// 出处：`资料/选牌Choose_数据与设计.md` §四之二（那张表逐条带证据）。
// ⇒ **我们合成一份**：`ChoosePanel` 与 `MulliganPanel` 都从这个类派生，**别写第三份克隆**。
//
// **数值出处**：
//   · 选牌 / 选效果：`资料/选牌与选效果面板_原版数值.md` §三 + 实况报告
//     `资料/原版参照图/Unity参照管线_0825/data/panel_0914b/报告.md` §③（**运行时真值**）。
//   · 换牌：`data/runtime_ui_dump_drive_0912.tsv:437-450`。
// ⚠️ **实况与静态字段冲突时以实况为准**（铁律 4）。
//
// **卡行布局**照原版**自己的算法**（探针直接问 `CardsHorizontalLayout.GetPosition(i,n)` 问出来的，
// 不是我们推算的）—— 见 `SpacingPx` 与报告 §③那张表。
//
// **哪些是「我们挑的」**（文件里每处都单独标了）：
//   · 压暗层（原版有 `Shade.SwitchShade`，**颜色/透明度没查到**）
//   · 卡行的**纵向位置**（原版那个 anchor 节点**只有 Transform、没有 RectTransform** ⇒ 无据可查）
//   · 卡上按钮的**纵向偏移**（原版那个 prefab 是运行时实例化的，静态 dump 里没有）
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>
    /// 「横向选卡面板」的共用壳。**只管画 + 命中 + 可见性**，选几张、选完做什么由派生类决定。
    /// </summary>
    public class CardChoicePanel : MonoBehaviour
    {
        /// <summary>挑几张：换牌是**多选**（可弃回任意张），选牌/选效果是**单选**。</summary>
        public enum PickMode { Multi, Single }

        /// <summary>
        /// 两个面板不同的那几处数值。**全部是 1920×1080 下的像素**、`y` **从上面数**。
        /// 只把两边**实测不同**的放进来；相同的（标题框、底条尺寸、圆钮尺寸、图名）写成常量。
        /// </summary>
        public struct Layout
        {
            public float TitleCx, TitleCy, TitleW, TitleH;
            /// <summary>标题字号（px）。⚠️ 实况：选牌面板 `ChooseText/Text` = **55**。</summary>
            public float TitlePx;

            public float BarCx, BarCy;                  // 底条中心（图 `40k_bt_underbutton` 577.5×63.84）
            public float PlayCx, PlayCy;                // 圆形播放钮中心（图 `40k_UI_bt_play` 80.47×79.64）
            public float EyeCx, EyeCy;                  // 眼睛中心（图 `40k_UI_bt_eye`）
            public float ConfirmCx, ConfirmCy;          // 确认文字中心

            /// <summary>卡行中心（**从上面数**）。⚠️ **这是我们挑的** —— 原版那个 anchor 节点
            /// **只有 Transform、没有 RectTransform**，卡的位置是运行时算的，**无据可查**。</summary>
            public float CardRowCy;

            public string CardBtnText;                  // 卡上那颗按钮的字（`换` / `Select`）
        }

        /// <summary>面板开着吗。开着时**吃掉点击**，别让底下的棋盘/手牌也响应。</summary>
        public bool Visible { get; private set; }

        /// <summary>点确认 → 回调「选中的下标」（升序；可能为空 = 没选）。</summary>
        public Action<List<int>> OnDone;

        public PickMode Mode { get; private set; }

        // ---- 共用常量（两个面板实测**相同**的那几处）----
        /// <summary>底条尺寸（`40k_bt_underbutton`）—— 原版 `BG` `sd=577.50×63.84`</summary>
        const float BarW = 577.5f, BarH = 63.84f;
        /// <summary>圆钮尺寸（`40k_UI_bt_play`）—— 原版 `ContinueButton` `sd=80.47×79.64`</summary>
        const float PlayH = 79.64f;
        /// <summary>眼睛尺寸 —— 实测两边**略有不同**（78.44² vs 79.6），取 `Layout` 不给就按这个</summary>
        const float EyePx = 78.44f;

        /// <summary>补间/自检用：面板的 z。和日志面板一样推到 −0.9 一带 ——
        /// 场上的粒子在 −0.5 那层会飘到面板上面（那个坑记在 `BattleLogPanel` 的注释里）</summary>
        protected const float Z = -0.9f;

        protected Layout Lo;

        string _titleText = "", _confirmText = "";

        ImageQuad _shade, _bar, _play, _eye;
        Label _title, _confirm;
        readonly List<ImageQuad> _cardBtns = new List<ImageQuad>();
        readonly List<Label> _cardTexts = new List<Label>();
        readonly List<CardView> _cards = new List<CardView>();
        readonly List<int> _picked = new List<int>();

        // ==================================================================
        //  摆位换算
        // ==================================================================

        /// <summary>
        /// 像素 → 世界单位。
        /// `LayoutSpace` 的约定是「可见高度恒为 `DesignHeight`(=10) 世界单位」，而 1080p 下
        /// 那 10 个单位就是 **1080 px** ⇒ **1 px = 10/1080 = 1/108 世界单位**。
        /// ⚠️ **2026-09-14 踩到**：第一版写成 `px / (DesignHeight * 108)` = `/1080`，
        ///    于是**所有尺寸小了 10 倍** —— 4 张候选卡的间距（470 px）算出来只有 0.44 世界单位，
        ///    而卡自己宽 3.55 ⇒ **全部叠在屏幕中央**。**截图才看出来的**（断言全绿）。
        /// </summary>
        protected static float U(float px) { return px * LayoutSpace.DesignHeight / 1080f; }

        /// <summary>(中心 x, 中心 y 从**上**数) → 世界坐标</summary>
        protected static Vector3 At(float cx, float cy)
        {
            return LayoutSpace.ToWorld(cx / 1920f, 1f - cy / 1080f);
        }

        /// <summary>原版 `CardsHorizontalLayout` 的**中心距**（px）。**问出来的**，不是推算的
        /// —— 出处：实况报告 §③（`GetPosition(i,n)` 直接调用，n=2 算得 502.1 与序列化字段吻合）。
        /// ⚠️ **n=4 就开始压**（原来文档写「>7 张才压」是错的，2026-09-14 晚更正）。
        /// ⚠️ 原版只测到 **n=7**；`n≥8` 按 7 的间距走 —— **这一格是外推，如实标着**。</summary>
        public static float SpacingPx(int n)
        {
            if (n <= 3) return 502.2f;
            if (n == 4) return 470.6f;
            if (n == 5) return 376.5f;
            if (n == 6) return 313.7f;
            return 268.9f;                      // n>=7（7 是实测，8+ 是外推）
        }

        /// <summary>面板里那张卡的缩放 —— 原版 `CardsHorizontalLayout.Scale = 183.41`
        /// 是「每**卡本地单位**多少像素」，而我们的世界是 1 单位 = 108 px
        /// ⇒ `183.41/108`。卡本体 `CardView` 的宽高正好等于原版 `CardUI` 的子节点 `2DCard`
        /// （`2.093×3.331`，实据见 `选牌与选效果面板_原版数值.md` §三末）。
        /// ⇒ 乘出来 = 383.8×611.0 px，与原版实测的 383.9×610.9 **对得上**。</summary>
        public const float CardScale = 183.41f / 108f;

        // ==================================================================
        //  建
        // ==================================================================

        /// <summary>建面板。**派生类的 `Create` 里调它**（`this` 就是宿主）。</summary>
        protected void Build(Layout lo, PickMode mode, string title, string confirm)
        {
            Lo = lo;
            Mode = mode;
            _titleText = title ?? "";
            _confirmText = confirm ?? "";
            var root = transform;

            // 压暗（**我们挑的**：原版只查到有个 `Shade.SwitchShade`，颜色/透明度没查到）
            _shade = ImageQuad.Create(root, CardArt.Solid(), At(960f, 540f), U(1200f),
                                      new Vector2(0.5f, 0.5f), "ChoiceShade");
            if (_shade != null)
            {
                _shade.SetAspect(1920f / 1080f);
                _shade.SetTint(new Color(0f, 0f, 0f, 0.55f));
                _shade.transform.localPosition += new Vector3(0f, 0f, Z);
            }

            // 标题（原版 `ChooseText` / `MulliganText`：`sd=1344×79.44`、`a=(.5,1)`、`ap=(7,-106.5)`）
            _title = Label.Create(root, _titleText, At(lo.TitleCx, lo.TitleCy),
                                  8, new Color(1f, 0.94f, 0.82f), new Vector2(0.5f, 0.5f), "ChoiceTitle");
            if (_title != null) _title.SetGlyphHeight(U(lo.TitlePx));   // 中文用汉字高度，不是大写高度

            // 确认：底条 + 圆形播放钮（两边同图同尺寸）
            _bar = ImageQuad.Create(root, CardArt.Ui("40k_bt_underbutton"), At(lo.BarCx, lo.BarCy),
                                    U(BarH), new Vector2(0.5f, 0.5f), "ChoiceBar");
            if (_bar != null) _bar.SetAspect(BarW / BarH);
            _play = ImageQuad.Create(root, CardArt.Ui("40k_UI_bt_play"), At(lo.PlayCx, lo.PlayCy),
                                     U(PlayH), new Vector2(0.5f, 0.5f), "ChoiceCircle");
            _confirm = Label.Create(root, _confirmText, At(lo.ConfirmCx, lo.ConfirmCy), 6,
                                    new Color(1f, 0.92f, 0.75f), new Vector2(0.5f, 0.5f), "ChoiceConfirmText");
            if (_confirm != null) _confirm.SetGlyphHeight(U(33.7f));    // ⚠️ 实况 fs=33.7（不是 §三 那个 45）

            // 眼睛：收起卡片上的按钮（原版 `onClick → ToogleChooseMenuVisibility`）
            _eye = ImageQuad.Create(root, CardArt.Ui("40k_UI_bt_eye"), At(lo.EyeCx, lo.EyeCy),
                                    U(EyePx), new Vector2(0.5f, 0.5f), "ChoiceEye");

            foreach (var q in new[] { _bar, _play, _eye })
                if (q != null) q.transform.localPosition += new Vector3(0f, 0f, Z);
            if (_title != null) _title.transform.localPosition += new Vector3(0f, 0f, Z - 0.05f);
            if (_confirm != null) _confirm.transform.localPosition += new Vector3(0f, 0f, Z - 0.05f);

            SetVisible(false);
        }

        /// <summary>改标题（`Open` 时按「正在结算的是哪张卡」传进来）。</summary>
        protected void SetTexts(string title, string confirm)
        {
            _titleText = title ?? "";
            if (!string.IsNullOrEmpty(confirm)) _confirmText = confirm;
            if (_title != null) _title.SetText(_titleText);
            if (_confirm != null) _confirm.SetText(_confirmText);
        }

        // ==================================================================
        //  开 / 关
        // ==================================================================

        public void Open(IReadOnlyList<CardView> cards, string title)
        {
            _picked.Clear();
            if (!string.IsNullOrEmpty(title)) SetTexts(title, _confirmText);
            LayOutCards(cards);
            SetVisible(true);
        }

        public void Close()
        {
            ClearCardDecor();
            SetVisible(false);
        }

        void SetVisible(bool v)
        {
            Visible = v;
            foreach (var q in new[] { _shade, _bar, _play, _eye })
                if (q != null) q.gameObject.SetActive(v);
            if (_title != null) _title.gameObject.SetActive(v);
            if (_confirm != null) _confirm.gameObject.SetActive(v);
            for (int i = 0; i < _cardBtns.Count; i++)
                if (_cardBtns[i] != null) _cardBtns[i].gameObject.SetActive(v);
            for (int i = 0; i < _cardTexts.Count; i++)
                if (_cardTexts[i] != null) _cardTexts[i].gameObject.SetActive(v);
        }

        /// <summary>
        /// 把候选牌摆成一行（**原版自己的间距表**），每张下面贴一颗按钮。
        /// ⚠️ **纵向位置是我们挑的**（原版那个 anchor 无 RectTransform，见文件头）。
        /// ⚠️ 按钮的**纵向偏移也是我们排的**（原版 prefab 运行时实例化、静态 dump 里没有）。
        /// </summary>
        void LayOutCards(IReadOnlyList<CardView> cards)
        {
            ClearCardDecor();
            _cards.Clear();
            if (cards == null) return;
            for (int i = 0; i < cards.Count; i++) if (cards[i] != null) _cards.Add(cards[i]);
            int n = _cards.Count;
            if (n == 0) return;

            float spacing = U(SpacingPx(n));
            float total = spacing * (n - 1);
            float left = At(960f, Lo.CardRowCy).x - total * 0.5f;
            float y = At(960f, Lo.CardRowCy).y;

            for (int i = 0; i < n; i++)
            {
                var c = _cards[i];
                c.transform.localScale = new Vector3(CardScale, CardScale, 1f);
                c.transform.localPosition = new Vector3(left + spacing * i, y, Z);
                c.SetHighlight(CardHighlightState.Normal);

                // ⚠️ 按钮位置：原版那个 prefab `CardChooseCardButtonFrame` 的 `Select` 钮在
                //    **卡中心下方 ≈323 px**（`选牌与选效果面板_原版数值.md` §三末，×183.41 实测）。
                //    2026-09-14 第一版写成「卡高 ×0.30」≈183 px ⇒ **压在卡面的文字上**（截图才发现）。
                var pos = new Vector3(left + spacing * i, y - U(323f), Z - 0.02f);
                var q = ImageQuad.Create(transform, CardArt.Ui("UI_Button_Mulligan"), pos,
                                         U(77f), new Vector2(0.5f, 0.5f), "ChoiceCardBtn_" + i);
                if (q != null) _cardBtns.Add(q);

                var t = Label.Create(transform, Lo.CardBtnText, pos, 5, new Color(1f, 0.9f, 0.7f),
                                     new Vector2(0.5f, 0.5f), "ChoiceCardBtnText_" + i);
                if (t != null)
                {
                    t.SetGlyphHeight(U(28f));
                    t.transform.localPosition += new Vector3(0f, 0f, Z - 0.05f);
                }
                _cardTexts.Add(t);
            }
        }

        void ClearCardDecor()
        {
            for (int i = 0; i < _cardBtns.Count; i++) if (_cardBtns[i] != null) Kill(_cardBtns[i].gameObject);
            for (int i = 0; i < _cardTexts.Count; i++) if (_cardTexts[i] != null) Kill(_cardTexts[i].gameObject);
            _cardBtns.Clear();
            _cardTexts.Clear();
            _cards.Clear();
        }

        protected static void Kill(GameObject o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }

        // ==================================================================
        //  选择
        // ==================================================================

        /// <summary>选中/取消一张牌。**单选**模式下选新的会顶掉旧的（原版选牌就是这个语义）。</summary>
        public void TogglePick(int i)
        {
            if (i < 0 || i >= _cards.Count) return;
            if (_picked.Contains(i)) _picked.Remove(i);
            else
            {
                if (Mode == PickMode.Single && _picked.Count > 0) _picked.RemoveAt(0);
                _picked.Add(i);
            }
            ApplyPicks();
        }

        /// <summary>被选中的下标（**升序**）。</summary>
        public List<int> Picked { get { var l = new List<int>(_picked); l.Sort(); return l; } }
        public bool IsPicked(int i) { return _picked.Contains(i); }
        public int CardCount { get { return _cards.Count; } }

        /// <summary>选中的样子：卡**置灰**（`Unplayable` 那一档）+ 按钮换成按下态那张图。
        /// 换牌那套就是靠这两处表达「这张要换掉」的。</summary>
        void ApplyPicks()
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                var c = _cards[i];
                if (c == null) continue;
                bool m = _picked.Contains(i);
                c.SetHighlight(m ? CardHighlightState.Unplayable : CardHighlightState.Normal);
                if (i < _cardBtns.Count && _cardBtns[i] != null)
                    _cardBtns[i].SetTexture(CardArt.Ui(m ? "UI_Button_Mulligan_Pressed" : "UI_Button_Mulligan"));
            }
        }

        // ==================================================================
        //  交互（和设置面板/日志面板同一条路：批处理下由驱动层喂指针）
        // ==================================================================

        /// <summary>指针在哪张牌的按钮上（-1 = 没点中）</summary>
        public int ButtonIndexAt(Vector3 world)
        {
            for (int i = 0; i < _cardBtns.Count; i++)
                if (_cardBtns[i] != null && _cardBtns[i].Contains(world)) return i;
            return -1;
        }

        /// <summary>指针在「确认」上吗（底条或圆钮都算，外加确认文字周围一圈）</summary>
        public bool HitDone(Vector3 world)
        {
            if (_bar != null && _bar.Contains(world)) return true;
            if (_play != null && _play.Contains(world)) return true;
            float r = U(380f) * 0.5f;                  // 确认块那块 372.86×43.62 的包围半径（我们取的）
            return (world - DoneWorldPos).sqrMagnitude < r * r;
        }

        public bool HitEye(Vector3 world) { return _eye != null && _eye.Contains(world); }
        public Vector3 DoneWorldPos { get { return At(Lo.ConfirmCx, Lo.ConfirmCy) + new Vector3(0f, 0f, Z); } }
        public Vector3 EyeWorldPos { get { return At(Lo.EyeCx, Lo.EyeCy) + new Vector3(0f, 0f, Z); } }
        public Vector3 CardWorldPos(int i)
        {
            return (i >= 0 && i < _cards.Count && _cards[i] != null) ? _cards[i].transform.position : Vector3.zero;
        }

        /// <summary>眼睛钮 = 把卡片上的按钮收起来看战场（原版 `ToogleChooseMenuVisibility`）。
        /// ⚠️ 原版到底是「按住」还是「开关」**没查到** → 我们做成开关（**我们挑的**）。</summary>
        public void ToggleEye()
        {
            bool show = !(_cardBtns.Count > 0 && _cardBtns[0] != null && _cardBtns[0].gameObject.activeSelf);
            for (int i = 0; i < _cardBtns.Count; i++)
                if (_cardBtns[i] != null) _cardBtns[i].gameObject.SetActive(show);
            for (int i = 0; i < _cardTexts.Count; i++)
                if (_cardTexts[i] != null) _cardTexts[i].gameObject.SetActive(show);
        }

        /// <summary>
        /// 处理一次点击。返回 true = 被面板吃掉了（别再传给棋盘/手牌）。
        /// ⚠️ 面板开着时**点哪儿都先经过这里**（原版 `Shade` 就是这个意思）。
        /// </summary>
        public virtual bool HandleClick(Vector3 world)
        {
            if (!Visible) return false;

            if (HitDone(world))
            {
                var picks = Picked;
                if (OnDone != null) OnDone(picks);
                return true;
            }
            if (HitEye(world)) { ToggleEye(); return true; }

            int hit = ButtonIndexAt(world);
            if (hit >= 0) { TogglePick(hit); return true; }

            // 点卡本身也能选（原版是卡片上那颗按钮；**点卡这条路是我们加的**）
            for (int i = 0; i < _cards.Count; i++)
                if (_cards[i] != null && _cards[i].Contains(world)) { TogglePick(i); return true; }

            return true;      // 面板开着 → 点其它地方也吃掉
        }

        // ==================================================================
        //  自检
        // ==================================================================

        public string TitleText { get { return _title != null ? _title.Text : "<无>"; } }
        public string ConfirmText { get { return _confirm != null ? _confirm.Text : "<无>"; } }
        public bool BarHasArt { get { return _bar != null && _bar.Texture != null; } }
        public bool PlayHasArt { get { return _play != null && _play.Texture != null; } }
        public bool EyeHasArt { get { return _eye != null && _eye.Texture != null; } }
        public string BarTex { get { return _bar != null && _bar.Texture != null ? _bar.Texture.name : "<无>"; } }
        public string EyeTex { get { return _eye != null && _eye.Texture != null ? _eye.Texture.name : "<无>"; } }
        public string CardBtnTex(int i)
        {
            if (i < 0 || i >= _cardBtns.Count || _cardBtns[i] == null) return "<无>";
            return _cardBtns[i].Texture != null ? _cardBtns[i].Texture.name : "<无>";
        }
        public int CardButtonCount { get { return _cardBtns.Count; } }

        /// <summary>第 i 张牌的中心 x（世界）—— 自检拿来验「间距照原版那张表」。</summary>
        public float CardWorldX(int i)
        {
            return (i >= 0 && i < _cards.Count && _cards[i] != null) ? _cards[i].transform.position.x : 0f;
        }

        public string Describe()
        {
            return $"选卡面板 开着={Visible} 模式={Mode} 卡 {_cards.Count} 张 选中 [{string.Join(",", Picked.ConvertAll(x => x.ToString()).ToArray())}]";
        }
    }
}
