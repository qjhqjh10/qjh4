// ChatPopupPanel.cs — 原版 `ChatPopup`：点 `ChatButton` 弹出来的 **6 个预设台词钮**
//
// 规格出处（**逐条都是解包实测，不是照截图摆的**）：
//   `资料/语音线_原版规格与ASR管道.md` §1.7（机制/时长/颜色）与 §1.7.1（**逐节点绝对矩形**）。
//
// ── 原版链路 ────────────────────────────────────────────────────────────
//   `ChatButton`（GameObject 85，父链 `PlayerInfo → LeftArea`）上的 `EverguildButton.m_OnClick`
//    → `BattleManager.ClickChat` → `VoiceLinesController.ClickChat` → `VoiceLinesPopupSelector.Clicked`
//   `Clicked` = **开关**：开着就 `Hide()`，关着就 `SetActive(true)` + `RefreshChatLines()` + 淡入。
//   ⚠️ **原来任务表写「开面板的入口是 `EnemyInfo`」是错的** —— `EnemyInfo` 那侧 `m_Calls` 是空数组。
//
// ── 三条「跟直觉不一样」的原版事实（都在 §1.7 / §1.7.1 里有出处）────────────────
//   ① **没有水平滑动**。`Awake` 里 `outPos = originalPos − (1.5f × originalPos.x, 0)`，
//      而面板序列化 `x = 0` ⇒ 显示位与隐藏位**同为 (0, 479.81)**，那个 `DOAnchorPos` 是个**空动作**。
//      看得见的只有 `CanvasGroup.alpha` 0↔1。⇒ 我们**不做位移**。
//   ② **6 个钮的序列化位置全是 (0,0)**，真正的位置是 `Buttons` 容器上的 **`VerticalLayoutGroup`**
//      运行时排出来的（`spacing = 1.48` · `childAlignment = UpperCenter` · **不改尺寸**）。
//      ⇒ 只看序列化数据会把 6 个钮**全叠在左下角**。下表用的是**排完之后**的坐标。
//   ③ `CloseChatPopup` 那个全屏关闭区**确实隐形，但原因不是 alpha** ——
//      它挂的是 `NonDrawingGraphic`（UnityEngine.UI.Extensions，**不绘制、只吃射线**），
//      `m_Color.a` 其实是 1.0。我们工程里没有那个组件 ⇒ **用 1×1 全透明图 + raycastTarget** 等效。
//
// ── 6 个钮说什么 ────────────────────────────────────────────────────────
//   原版：`RefreshChatLines` 里**写死起点 5、按下标 +1** → `RawCardScript.GetLocalizedChatMsg(warlord, idx+5)`
//   （`SetupChatOptions.c:54,112` 与 `RefreshChatLines.c:71` 两处独立证实）。
//   ⇒ 下标 0…5 = 枚举 5…10 = `Greet / Threat / WellPlayed / Taunt / Sorry / Oops`，
//     对应我们 `VoiceLines.ForChatButton` = `greet / threat / wp / gen1 / gen2 / gen3`。
//   ⚠️ **钮上印什么字**：原版的字来自 **I2 本地化词条**，而那张表**在远端 CCD、本地没有**
//     （§1.6.1 已证）。原版**序列化下来的兜底文本 6 个钮全是 `Greetings`**（6 个 TMP 逐一确认）。
//     ⇒ 我们**用枚举自己的名字当标签**（`Greet / Threat / Well Played / Taunt / Sorry / Oops`）——
//       那是**原版自己的命名**，只是原版把它交给本地化表印。**这一条是我们挑的，明确标出来**：
//       本工程里没有任何一处能印出原版那 6 个真标签，因为那个来源不存在于客户端。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `ChatPopup` 的 6 个预设台词钮面板。**只负责显示与命中** ——
    /// 「说了什么」由 `BattleDriver` 决定（它才有 `RuleCore` 和语音表）。</summary>
    public class ChatPopupPanel : MonoBehaviour
    {
        // ==================================================================
        //  版面：**原版绝对 px，1920×1080，左上原点** —— 逐条抄自 §1.7.1
        //  ⚠️ 这几行**不要「顺手改整齐」**：它们是从 34 个 RectTransform 逐级下推算出来的，
        //     看着可疑的两个数（钮宽 603.6 > 容器 557.31 ⇒ 左右各溢出 23.15 · 6 条边不等长）
        //     **原版就是这样**。
        // ==================================================================
        static readonly Rect Panel = new Rect(0.00f, 362.45f, 815.04f, 475.47f);
        static readonly Rect Bg = new Rect(121.16f, 475.64f, 557.31f, 301.35f);   // `White Square` 染深绿
        static readonly Rect FrameTop = new Rect(130.75f, 445.23f, 517.69f, 33.92f);
        static readonly Rect FrameBottom = new Rect(131.49f, 769.06f, 517.68f, 36.54f);
        static readonly Rect FrameRight = new Rect(648.49f, 434.37f, 83.65f, 371.07f);
        static readonly Rect FrameLeft = new Rect(14.32f, 445.41f, 117.33f, 386.98f);

        /// <summary>第 0 个钮的绝对矩形；其余 5 个 **y 依次 +49.48**（= 48 高 + 1.48 间距）。</summary>
        static readonly Rect Button0 = new Rect(77.20f, 481.89f, 603.60f, 48.00f);
        const float ButtonPitch = 49.48f;

        // 钮内三块（偏移 = 绝对 − 钮原点，见 §1.7.1 那张表的最后三行）
        static readonly Rect BtnBg = new Rect(50.32f, 1.75f, 550.40f, 44.50f);    // `40k_voicelines_bt_R`
        static readonly Rect BtnIcon = new Rect(3.50f, 4.00f, 40.00f, 40.00f);    // `40k_voicelines_bt_L`
        static readonly Rect BtnText = new Rect(64.16f, 0.00f, 528.70f, 48.00f);

        /// <summary>原版底色 `White Square` 的 **`m_Color` 真值**（不是「大概深绿」）。
        /// ⚠️ 我先前记的 `(0, 0.07, 0, 1)` 是**凑整过的近似**；精确值是这三个数。</summary>
        static readonly Color BgColor = new Color(0.001554f, 0.066038f, 0.000000f, 1f);

        /// <summary>原版 `timeToOpen` / `timeToClose`，**两个都是 0.5 s**、缓动 `OutCubic`。</summary>
        public const float FadeTime = 0.5f;
        /// <summary>原版 `VoiceLinesController.CHAT_INTERACTABLE_COOLDOWN = 4f`。
        /// 冷却挂在 **`ChatButton`** 上（不是面板上）—— 由 `BattleDriver` 计时。</summary>
        public const float Cooldown = 4f;

        /// <summary>钮上的标签。⚠️ **这一行是我们挑的**，理由见文件头（原版标签来自远端 I18N，本地不存在）。</summary>
        public static readonly string[] ButtonLabels =
            { "Greet", "Threat", "Well Played", "Taunt", "Sorry", "Oops" };

        // ==================================================================

        public const int ButtonCount = 6;

        class Btn
        {
            public ImageQuad bg, icon;
            public Label text;
            public Rect rect;                 // 绝对矩形（每次 RefreshLayout 重算）
        }

        readonly List<Btn> _btns = new List<Btn>();
        ImageQuad _bg, _frameTop, _frameBottom, _frameLeft, _frameRight;
        ImageQuad _close;                     // 全屏关闭区（等效 `NonDrawingGraphic`）
        bool _built;

        float _alpha;
        /// <summary>面板**开着**没有 —— **不是**「alpha > 0」。</summary>
        bool _open;
        /// <summary>正在淡出（`Hide()` 之后那一小段）。原版这时 `interactable` 已经是 false。</summary>
        bool _fadingOut;

        /// <summary>面板开着没有。🔴 **自检看这个、模态判据也用它**。</summary>
        /// ⚠️ **2026-09-18 更正**：原来写的是 `_alpha > 0.001f` —— 那样 `Show()` 之后要等一次
        ///    `Advance` 才为真，于是「刚打开就点」全部落空（自检当场抓到 4 条红）。
        ///    原版 `Clicked` 是**立刻** `SetActive(true)` + `canvasGroup.interactable = true`，
        ///    淡入只是**外观**，不影响「开没开」—— 这里照它改。
        public bool Visible { get { return _open; } }
        /// <summary>现在收不收点击。原版 `Hide()` 一开头就把 `interactable` 置 0（**不等淡出完**）。</summary>
        public bool Interactable { get { return _open && !_fadingOut; } }
        /// <summary>当前整块面板的透明度（淡入淡出中会介于 0..1）。</summary>
        public float Alpha { get { return _alpha; } }
        /// <summary>建起来了没有（图都取到了）。</summary>
        public bool Ready { get { return _built && _bg != null; } }
        /// <summary>最近一次 `SetPointer` 命中的钮（-1 = 没命中）。自检用。</summary>
        public int LastHit { get; private set; }
        /// <summary>最近一次**点击**（按下后抬起）落在第几个钮上。-1 = 没点到钮。</summary>
        public int LastClicked { get; private set; }

        /// <summary>6 个钮的绝对矩形（自检拿它比原版坐标）。</summary>
        public Rect ButtonRect(int i)
        {
            return (i >= 0 && i < _btns.Count) ? _btns[i].rect : new Rect();
        }

        public static ChatPopupPanel Create(Transform parent)
        {
            var go = new GameObject("ChatPopup");
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<ChatPopupPanel>();
            p.Build();
            return p;
        }

        void Build()
        {
            if (_built) return;
            _built = true;
            CardArt.Load();

            // 全屏关闭区先建 ⇒ 渲染顺序在**最底下**（面板主体压着它）
            //   原版这个节点是 `ChatPopup` 的**子物体**、`EventTrigger` 绑 `PointerClick → Hide`
            //   ⚠️ 我们**不接 Unity 的射线**（本工程的点击一律是「自己算 `Contains(世界坐标)`」，
            //      `ImageQuad` 上根本没有 `raycastTarget` 这个口子）⇒ 这里只要一张**全透明的图**占位，
            //      命中由下面的 `SetPointer` 按同一套矩形判 —— 等效原版那个 `NonDrawingGraphic`。
            _close = ImageQuad.Create(transform, Solid(), Vector3.zero, 1f,
                                      new Vector2(0.5f, 0.5f), "CloseChatPopup");
            if (_close != null) _close.SetTint(new Color(1f, 1f, 1f, 0f));   // 只看不画

            _frameTop = Make("BGFrame_Top", "40k_UnitChat_Background_Top");
            _frameBottom = Make("BGFrame_Bottom", "40k_UnitChat_Background_Bottom");
            _frameLeft = Make("BGFrame_Left", "40k_UnitChat_Background_Left");
            _frameRight = Make("BGFrame_Right", "40k_UnitChat_Background_Right");
            _bg = Make("BGFrame_BG", "White_Square");
            if (_bg != null) _bg.SetTint(BgColor);

            for (int i = 0; i < ButtonCount; i++)
            {
                var b = new Btn();
                b.bg = Make("ChatButton" + (i + 1) + "_bg", "40k_voicelines_bt_R");
                b.icon = Make("ChatButton" + (i + 1) + "_icon", "40k_voicelines_bt_L");
                b.text = Label.Create(transform, ButtonLabels[i], Vector3.zero, 1, Color.white,
                                      new Vector2(0f, 0.5f), "ChatButton" + (i + 1) + "_Text");
                _btns.Add(b);
            }

            HideImmediate();
            RefreshLayout();
        }

        ImageQuad Make(string name, string art)
        {
            var q = ImageQuad.Create(transform, CardArt.Ui(art), Vector3.zero, 0.1f,
                                     new Vector2(0.5f, 0.5f), name);
            return q;
        }

        /// <summary>1×1 纯色图（关闭区用）。</summary>
        Texture2D Solid()
        {
            if (_solid == null)
            {
                _solid = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                _solid.SetPixel(0, 0, Color.white);
                _solid.Apply();
                _solid.name = "solid_white";
            }
            return _solid;
        }
        Texture2D _solid;

        // ==================================================================
        //  摆放：绝对 px → 世界坐标
        //  ⚠️ **每次刷新重算**：这套坐标是屏幕的固定比例，只摆一次的话换分辨率就错位
        //     （HUD 那几个 Label 踩过一模一样的坑，见 `SkillPanel.RefreshLayout` 的注释）。
        // ==================================================================

        const float RefW = 1920f, RefH = 1080f;

        /// <summary>绝对 px 矩形（左上原点）→ 世界坐标的中心点 + 世界高度。</summary>
        static void Place(Rect r, out Vector3 pos, out float worldH)
        {
            pos = LayoutSpace.ToWorld((r.x + r.width * 0.5f) / RefW,
                                      1f - (r.y + r.height * 0.5f) / RefH);
            worldH = r.height * (LayoutSpace.VisibleHeight / RefH);
        }

        static void Put(ImageQuad q, Rect r, float z)
        {
            if (q == null) return;
            Vector3 p; float h;
            Place(r, out p, out h);
            q.transform.localPosition = new Vector3(p.x, p.y, z);
            q.SetWorldHeight(h);
            q.SetAspect(r.width / Mathf.Max(r.height, 0.0001f));   // **非等比**（原版就不是）
        }

        /// <summary>把整块面板按当前分辨率重摆一遍。</summary>
        public void RefreshLayout()
        {
            Vector3 p; float h;
            Place(Panel, out p, out h);

            // 关闭区：原版 4055.34 × 2114.44，相对面板中心 (2.45, 41.66) —— 盖满屏幕还多出去一截
            Put(_close, new Rect(Panel.x + 2.45f - 4055.34f / 2f, Panel.y + 41.66f - 2114.44f / 2f,
                                 4055.34f, 2114.44f), 0.02f);

            Put(_bg, Bg, 0.04f);
            Put(_frameLeft, FrameLeft, 0.05f);
            Put(_frameRight, FrameRight, 0.05f);
            Put(_frameTop, FrameTop, 0.05f);
            Put(_frameBottom, FrameBottom, 0.05f);

            for (int i = 0; i < _btns.Count; i++)
            {
                var b = _btns[i];
                // 🔴 位置按**布局组排完之后**算（`Button0.y + i × 49.48`），**不是**序列化的 (0,0)
                var r = new Rect(Button0.x, Button0.y + i * ButtonPitch, Button0.width, Button0.height);
                b.rect = r;
                Put(b.bg, new Rect(r.x + BtnBg.x, r.y + BtnBg.y, BtnBg.width, BtnBg.height), 0.06f);
                Put(b.icon, new Rect(r.x + BtnIcon.x, r.y + BtnIcon.y, BtnIcon.width, BtnIcon.height), 0.07f);
                if (b.text != null)
                {
                    var tr = new Rect(r.x + BtnText.x, r.y + BtnText.y, BtnText.width, BtnText.height);
                    Place(tr, out p, out h);
                    b.text.transform.localPosition = new Vector3(p.x, p.y, 0.07f);
                    b.text.SetGlyphHeight(30f * (LayoutSpace.VisibleHeight / RefH));  // 原版 fs30
                }
            }
            SetAlpha(_alpha);
        }

        // ==================================================================
        //  开关 / 淡入淡出（原版 `Clicked` / `Show` / `Hide`）
        // ==================================================================

        /// <summary>原版 `VoiceLinesPopupSelector.Clicked`：**开着就关、关着就开**。</summary>
        public bool Toggle()
        {
            if (Visible) { Hide(); return false; }
            Show();
            return true;
        }

        public void Show()
        {
            _open = true;                     // **立刻**算开（原版 `SetActive(true)` + `interactable=true`）
            _fadingOut = false;
            gameObject.SetActive(true);
            RefreshLayout();
            _fading = true; _fadeTarget = 1f;
        }

        public void Hide()
        {
            _open = false;                    // **立刻**算关（原版一开头就把 `interactable` 置 0）
            _fadingOut = true;
            if (!gameObject.activeSelf) { _fading = false; _fadingOut = false; SetAlpha(0f); return; }
            _fading = true; _fadeTarget = 0f;
        }

        /// <summary>不播淡出直接收（**批处理里用** —— 那里没有帧循环，`Update` 不会跑）。</summary>
        public void HideImmediate()
        {
            _open = false;
            _fadingOut = false;
            _fading = false;
            gameObject.SetActive(false);
            SetAlpha(0f);
        }

        float _fadeTarget;
        bool _fading;

        void SetAlpha(float a)
        {
            _alpha = Mathf.Clamp01(a);
            SetAlphaOn(_bg); SetAlphaOn(_frameTop); SetAlphaOn(_frameBottom);
            SetAlphaOn(_frameLeft); SetAlphaOn(_frameRight);
            for (int i = 0; i < _btns.Count; i++) { SetAlphaOn(_btns[i].bg); SetAlphaOn(_btns[i].icon); }
        }

        void SetAlphaOn(ImageQuad q)
        {
            if (q == null) return;
            var c = q.Tint;
            // `White Square` 那块有**自己的颜色**，别把底色冲掉
            if (q == _bg) c = BgColor;
            c.a = (q == _bg ? BgColor.a : 1f) * _alpha;
            q.SetTint(c);
        }

        /// <summary>推进淡入淡出。**必须由 `BattleDriver.AdvanceTimeline` 调** ——
        /// 批处理没有帧循环，`Update` 不会跑（这条坑本项目踩过）。</summary>
        public void Advance(float dt)
        {
            if (!_fading) return;
            float next = Mathf.MoveTowards(_alpha, _fadeTarget, dt / FadeTime);
            SetAlpha(next);
            if (Mathf.Abs(next - _fadeTarget) < 0.0001f)
            {
                _fading = false;
                if (_fadeTarget <= 0f)
                {
                    _fadingOut = false;
                    gameObject.SetActive(false);   // 原版 `OnComplete` 里 `SetActive(false)`
                }
            }
        }

        // ==================================================================
        //  命中
        // ==================================================================

        /// <summary>世界坐标落在面板主体上吗（**不含**全屏关闭区）。</summary>
        public bool Contains(Vector3 world)
        {
            if (!Interactable) return false;
            for (int i = 0; i < _btns.Count; i++)
                if (Hit(_btns[i].rect, world)) return true;
            return false;
        }

        /// <summary>世界坐标落在第几个钮上（-1 = 没落在任何钮上）。</summary>
        public int ButtonAt(Vector3 world)
        {
            if (!Interactable) return -1;
            for (int i = 0; i < _btns.Count; i++)
                if (Hit(_btns[i].rect, world)) return i;
            return -1;
        }

        static bool Hit(Rect r, Vector3 world)
        {
            Vector3 p; float h;
            Place(r, out p, out h);
            float w = r.width * (LayoutSpace.VisibleHeight / RefH);
            return Mathf.Abs(world.x - p.x) <= w * 0.5f && Mathf.Abs(world.y - p.y) <= h * 0.5f;
        }

        /// <summary>
        /// 自检/交互统一入口：喂一次指针。**返回这一下点中的钮下标**（-1 = 没点到钮）。
        /// 语义对齐原版：**点在面板外 → 关面板**（原版是 `CloseChatPopup` 的全屏 `EventTrigger`）；
        /// **点在某个钮上 → 交给调用方说那句话**。
        /// </summary>
        public int SetPointer(Vector3 world, bool down)
        {
            LastHit = Interactable ? ButtonAt(world) : -1;
            if (!down) return -1;

            if (!Interactable) { LastClicked = -1; return -1; }

            if (LastHit >= 0) { LastClicked = LastHit; return LastHit; }

            // 面板外（或钮与钮之间的缝）⇒ 关。原版那条全屏关闭区就是干这个的。
            Hide();
            LastClicked = -1;
            return -1;
        }

        /// <summary>自检打印用。</summary>
        public string Describe()
        {
            // ⚠️ `?:` **不能直接写进字符串插值**（`:` 会被当成格式分隔符，CS8361）—— 先取出来
            string r0 = _btns.Count > 0 ? _btns[0].rect.ToString() : "-";
            return $"ChatPopup 可见={Visible} alpha={_alpha:F2} 钮={_btns.Count} 第1钮矩形=({r0})";
        }
    }
}
