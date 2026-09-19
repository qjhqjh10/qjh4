// CardDisplayWindow.cs — 卡牌放大展示窗（原版 `CardDisplayWindow` / `ShowCard`）
//
// 原版出处：`资料/战斗规格/战斗重建_0827/子代理读报_front弹层_0827.md` 第一节 + 运行时 dump
// `FrontCanvas/Safe area FrontCanvas/Card Display Window`（**activeSelf=False**，平时关着）。
//   · 遮罩 `Menu Dark Background`  4574.6×2572.4，`color = (0,0,0,0.7725)`
//   · `Card Display`               752×868，屏幕正中（x[584,1336] y[106,974]）
//   · 里面那张卡                    `CardUI` scale 223.14 → 卡本体约为 **467×743 px**
//   · `LowerSection`               pivot(0.5,0) pos(0,−518.34) → 窗口下缘那条，y≈921~1078
//   · 文字 `FlavourTextBG`（dump 里 `img=TXT`，是 TMP 不是图）字号 32
//
// **怎么打开**（原版行为）：反编译 `BasicCardUI.ToggleOpenCardDisplayOnTouch`
// —— **轻点卡牌就开关**。我们照这个做：手牌**按下→松开且几乎没移动**（`CardInteraction.TapThreshold`）
// 就开关一次，再点一次关掉。（原版对棋盘上的单位也是轻点开关，但我们的轻点已经被
// 「选中 → 选目标」占了 —— 见 `CardInteraction` 的注释，那部分是另一套交互，没动。）
using UnityEngine;

namespace CardPresentation
{
    public class CardDisplayWindow : MonoBehaviour
    {
        // ---- 版面（px @1920×1080，全部来自上面的 dump / 报告）----
        /// <summary>里面那张卡的高度（px）。`CardUI` 的 scale 223.14 × 卡本体 3.3313 ≈ 743</summary>
        public const float CardHeightPx = 743f;
        /// <summary>遮罩的不透明度。原版 `Menu Dark Background` 的 `color.a`</summary>
        public const float MaskAlpha = 0.7725f;
        const float BodyYPx = 1000f;          // 窗口下缘那条文字的中心（y[921,1078]）

        /// <summary>整块窗放到 HUD **前面**（相机看 +Z，z 越小越近）。
        /// 比结算面板（−0.5）还靠前 —— 展示窗是「盖在所有东西上面」的那一层。</summary>
        const float ZMask = -2.0f, ZContent = -2.1f;

        /// <summary>正文每行多少个字就断行。**这是我们挑的** —— 原版是 TMP 自动折行，
        /// 我们这块走 `Label`（没有自动折行），中文基本等宽，所以按字数硬断。</summary>
        const int CharsPerLine = 34;
        /// <summary>正文最多几行（超了截断加省略号 —— 宁可短也不能糊出屏幕）</summary>
        const int MaxLines = 4;

        // ---- 语音按钮（原版 `Voices Over Button` / `CardDisplayWindow.voiceOverButton`）----
        // 出处：`资料/战斗规格/战斗重建_0827/子代理读报_front弹层_0827.md` §一 那一行 ——
        //   `pos(734.33,0.0002) size(**88.655**,88.655)` → 绝对 `x[1650.0,1738.7] y[945.5,1034.2]`；
        //   贴图 `40k_UI_bt_voicelines`（128×128 → 88.66 = 0.693x）。onClick 静态为空（运行时挂）。
        // ⚠️ 原版还有一个 `voiceOverAudioSource`（我们这条也建一个 AudioSource，2D 播放）。
        /// <summary>语音按钮中心与边长（px @1920×1080）—— 上面那行权威表里的绝对值</summary>
        const float VoiceCx = 1694.35f, VoiceCy = 989.85f, VoiceD = 88.655f;

        Transform _root;
        ImageQuad _mask, _voiceBtn;
        CardView _card;
        Label _title, _body, _hint;
        AudioSource _voiceAudio;
        /// <summary>正在展示的那张卡（`CardData` 是 struct，不能拿 null 当「没有」⇒ 单独一个 bool）</summary>
        CardData _shown;
        bool _hasShown;

        /// <summary>窗是不是开着（自检用）</summary>
        public bool Visible { get; private set; }
        /// <summary>正在展示哪张卡（自检用）。关着时是 null</summary>
        public CardView Card { get { return _card; } }
        /// <summary>窗口上那行字（自检用）</summary>
        public string ShownTitle { get; private set; }
        /// <summary>窗口下面那段正文（自检用）</summary>
        public string ShownBody { get; private set; }
        /// <summary>语音按钮上一次播的是哪个文件（**没播成是 null**；自检用）</summary>
        public string LastVoiceFile { get; private set; }

        public static CardDisplayWindow Create(Transform parent)
        {
            var go = new GameObject("CardDisplayWindow");
            go.transform.SetParent(parent, false);
            var w = go.AddComponent<CardDisplayWindow>();
            w.Build();
            w.Hide();
            return w;
        }

        void Build()
        {
            _root = transform;

            // 遮罩：**盖满整个可见区**（原版那张是 4574×2572，两倍屏还多，就是「铺满带余量」）。
            // ⚠️ 遮罩和提示行**建一次就留着**，开关只切 activeSelf —— 每次都重建的话，
            //    `Hide` 里把它们销毁了、`Show` 又不会重建，第二次打开就只剩一张光卡（踩过）。
            _mask = ImageQuad.Create(_root, SolidTexFor(LayoutSpace.VisibleWidth / LayoutSpace.DesignHeight),
                                     Vector3.zero, LayoutSpace.DesignHeight * 1.2f,
                                     new Vector2(0.5f, 0.5f), "mask");
            if (_mask != null)
            {
                _mask.transform.localPosition = new Vector3(0f, 0f, ZMask);
                _mask.SetTint(new Color(0f, 0f, 0f, MaskAlpha));
            }

            _hint = Label.Create(_root, "再点一下关闭", EndPanel.Pos(960f, 1046f, ZContent), 2,
                                 new Color(0.7f, 0.7f, 0.75f), new Vector2(0.5f, 0.5f), "cdw_hint");

            // 语音按钮（原版 `Voices Over Button`）：**图在工程里才建**（`Resources/Art/ui/` 被删时退回没有按钮，
            // 而不是摆一块白方块 —— 那就是「静默失败」了）
            var voiceTex = CardArt.Ui("40k_UI_bt_voicelines");
            if (voiceTex != null)
                _voiceBtn = ImageQuad.Create(_root, voiceTex, EndPanel.Pos(VoiceCx, VoiceCy, ZContent),
                                             VoiceD / 108f, new Vector2(0.5f, 0.5f), "cdw_voice");
            _voiceAudio = gameObject.AddComponent<AudioSource>();
            _voiceAudio.playOnAwake = false;
            _voiceAudio.spatialBlend = 0f;          // 2D —— 原版这个 source 也挂在 UI 上
            _voiceAudio.volume = 0.85f;             // ⚠️ 这个系数是**我们自己定的**（原版逐 source 的音量没查到）
            // 🆕 2026-09-19：挂到 `Voices` 通道 ⇒ 受设置面板那根「语音」滑块控制
            _voiceAudio.outputAudioMixerGroup = WarpforgeAudio.VoicesGroup;

            SetChrome(false);
        }

        void SetChrome(bool on)
        {
            if (_mask != null) _mask.gameObject.SetActive(on);
            if (_hint != null) _hint.gameObject.SetActive(on);
            if (_voiceBtn != null) _voiceBtn.gameObject.SetActive(on);
        }

        /// <summary>语音按钮被点到了没有（px 判定，和别处同一套换算）。</summary>
        public bool HitVoice(Vector3 world)
        {
            if (_voiceBtn == null) return false;
            float px = world.x * EndPanel.PxPerUnit + 960f;
            float py = 540f - world.y * EndPanel.PxPerUnit;
            float h = VoiceD * 0.5f;
            return Mathf.Abs(px - VoiceCx) <= h && Mathf.Abs(py - VoiceCy) <= h;
        }

        /// <summary>
        /// 点语音按钮 → 播**正在展示的这张卡**的单位语音（原版 `voiceOverButton` + `voiceOverAudioSource`）。
        /// 判据（哪条语音）复用 `VoiceLines.TryPick` —— **只此一处**，不另写一张后缀表。
        /// ⚠️ 这张卡没有语音时**说出来**（红线：不许静默失败），返回 false。
        /// </summary>
        public bool PlayVoice()
        {
            LastVoiceFile = null;
            if (!_hasShown) return false;      // 窗里没有卡（`CardData` 是 struct，用它判「有没有」）
            // ⚠️ **`_shown.id` 是「名字」不是卡 id**（`ToCardData` 的注释：`id` 兼着显示名/配对的活）——
            //    语音表按**引擎卡 id** 索引 ⇒ 要用 `artId`（原版卡 = 卡 id；自造卡 = 卡名，自然查不到）。
            string key = !string.IsNullOrEmpty(_shown.artId) ? _shown.artId : _shown.id;
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

        /// <summary>开/关一次。已开着就关掉（原版那个方法名就是 `Toggle...`）。</summary>
        public void Toggle(CardData d) { if (Visible) Hide(); else Show(d); }

        public void Show(CardData d)
        {
            Hide();                       // 先把上一次那张拆干净（换阵营要换卡框贴图，原地改不换图）

            // 大字卡：和战场上用的是**同一个 `CardView`**，只是放大 ——
            // 这样展示窗里看到的就是这张牌本身（卡框/立绘/中文名/数值全一致），不是另画一套
            float scale = CardHeightPx / 108f / CardView.Height;
            _card = CardView.Create(_root, d, "cdw_card");
            if (_card != null)
            {
                _card.SetPose(EndPanel.Pos(960f, 540f, ZContent), 0f, scale);
                // 展示窗里的卡不吃「可出牌/不可出牌」那套状态色
                _card.SetHighlight(CardHighlightState.Normal);
            }

            ShownTitle = d.title;
            ShownBody = Wrap(d.keywords);
            _shown = d; _hasShown = true;   // 语音按钮要按它查单位语音
            LastVoiceFile = null;
            _title = Label.Create(_root, d.title, EndPanel.Pos(960f, 921f, ZContent), 3,
                                  new Color(1f, 0.94f, 0.85f), new Vector2(0.5f, 0.5f), "cdw_title");
            _body = Label.Create(_root, ShownBody, EndPanel.Pos(960f, BodyYPx, ZContent), 2,
                                 new Color(0.92f, 0.9f, 0.88f), new Vector2(0.5f, 0.5f), "cdw_body");
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
            if (_card != null) { Kill(_card.gameObject); _card = null; }
            if (_title != null) { Kill(_title.gameObject); _title = null; }
            if (_body != null) { Kill(_body.gameObject); _body = null; }
            SetChrome(false);
        }

        static void Kill(GameObject o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }

        /// <summary>按字数硬断行（中文基本等宽）。空串返回空串。</summary>
        static string Wrap(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new System.Text.StringBuilder(s.Length + 8);
            int line = 0, chars = 0;
            for (int i = 0; i < s.Length; i++)
            {
                if (line >= MaxLines) { sb.Append("…"); break; }
                char c = s[i];
                if (c == '\n') { sb.Append(c); line++; chars = 0; continue; }
                if (chars >= CharsPerLine) { sb.Append('\n'); line++; chars = 0; }
                sb.Append(c);
                chars++;
            }
            return sb.ToString();
        }

        /// <summary>遮罩用的纯色贴图（`ImageQuad` 得有一张图才肯建）。宽高比按传入的来。
        /// ⚠️ **`MultiCardDisplay` 也用这一个**（两张遮罩是同一个作用，别写第二份）。</summary>
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
    }
}
