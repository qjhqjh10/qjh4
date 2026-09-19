// SettingsPanel.cs — 对局内的**设置面板**（原版 `BattleSettingsPanel` / `BattleSettingsWindow`）
//
// 为什么要有它：**投降按钮就在这里面** —— 原版 `BattleSettingsWindow.cs:9` 字段
// `resignButton` / `resignButtonText`（还有 closeButton 和三根音量滑块），
// 入口是右上角那个 `SettingsBtn`（`BattleSettingsWindow` 由 `SettingsBtn` 打开）。
// 之前只找到 `BattleResult.Forfeit` 这个枚举、找不到按钮，就是漏了「它在设置面板里」。
//
// 数值出处（`资料/战斗规格/战斗重建_0827/战斗界面JSON权威表_0827.md` + 运行时 dump）：
//   · 面板本体 743.2 × 758.6（`BattleSettingsPanel` 自己的 rect）
//   · 关闭钮 75 × 75（`Generic Close Button`，图 `UI_Button_Round_background` + 子图 `40k_bt_close`）
//   · 面板底 图 `40k_popup_texture`、边框 图 `40k_popup`
// ⚠️ **2026-09-18 更正：投降按钮的 rect 一直都拿得到，原来那句「取不到 ⇒ 位置是我们挑的」是错误否定。**
//    原来写「dump 里 `BattleSettingsPanel` 那一支只列到 Auto Zoom / 关闭钮 / Debug 那排，没有 resignButton 节点」，
//    实际是当年**漏了它挂在 `Bottom buttons` 子节点下**、也漏了运行时 dump 里本来就有这颗
//    （`runtime_ui_dump_Battle_Arena_1.tsv` 路径 `.../BattleSettingsPanel/Bottom buttons/Resign Button`）。
//    **原版真值**（`assets_full/bundle_scenes_scenes_battlearena1/RectTransform/RectTransform_3211.json` 亲读）：
//    面板内（原点=面板中心，y 向上）中心 **(−171.7, −310.5)** px、尺寸 **300×90** px（= 面板左下角，纵向 90.9% 处），
//    底图 `40K_button`（9-slice border 234,46,234,46）、染色 **(0.3686,0.8941,0.5874,1) 绿**、文字 `Resign` fs38。
//    ⚠️ 13 个战场各存一份副本，**只有 `battlearena2` 不同**（anchor(0,0)/ap(0,0)）—— 我们照多数那份做。
//    ✅ **2026-09-19 已照真值改完代码**：九宫格 `40K_button`（Sliced，border 234/46）+ 绿染 (0.3686,0.8941,0.5874,1)
//      + 白字 fs38。完整规格与出处见 `资料/普查产出_0918/第18行_UI三小条_规格.md` §①。
// ⚠️ 三根音量滑块：见 §②（原版写 `AudioMixer.SetFloat("Volume"+MixerType 枚举名)`）。
using System;
using UnityEngine;
using RuleEngine;

namespace CardPresentation
{
    public class SettingsPanel : MonoBehaviour
    {
        /// <summary>面板是不是开着。开着时**吃掉点击**，别让底下的棋盘也响应。</summary>
        public bool Visible { get; private set; }
        /// <summary>点「投降」时回调（驱动层接 `BattleDriver.Forfeit`）</summary>
        public Action OnResign;
        /// <summary>点「对手难度」时回调（驱动层换下一档，再用 <see cref="SetDifficulty"/> 回写文字）</summary>
        public Action OnCycleDifficulty;

        ImageQuad _bg, _close, _shade;
        Label _title, _resignText, _closeText;
        ImageQuad _closeIcon;
        ImageQuad _diffBtn;
        Label _diffLabel, _diffValue;
        /// <summary>投降钮的根节点。⚠️ **不是 `ImageQuad`** —— 原版底图 `40K_button` 是
        /// `Image.Type = Sliced`（border 234,46,234,46），要九宫格；单个 quad 拉不出那个形。</summary>
        GameObject _resignBtn;
        Texture _resignTex;

        // ---- 三根音量滑块（原版 `BattleSettingsWindow` 的 music/soundFX/voiceOver）----
        // 位置逐条来自解包（`资料/普查产出_0918/第18行_UI三小条_规格.md` §② + 2026-09-19 逐级解父链）：
        //   滑块中心（面板内，原点=面板中心，y 向上）：Music (−2.00, 120.07) · FX (−2.00, 3.44) · VoiceOver (−2.00, −113.18)
        //   标签：左边缘 x = −282.54（= 轨道左边缘），**矩形中心** y = 183.74 / 67.11 / −49.51，
        //         盒高 64.14 且 VAlign Bottom ⇒ **文字底边** = 盒中心 − 32.07 = 151.67 / 35.04 / −81.58
        //   三者共用祖父 `Volume Sliders`（在面板内 (−2.00,−19.89)，701.35×441.40），**没有 Media Tab**
        WfSlider _musicSlider, _fxSlider, _voiceSlider;
        Label _musicLabel, _fxLabel, _voiceLabel;
        // 原版标签是 `Music` / `Sound Effects` / `Voice-overs`（fs42 白，左对齐）。
        // 🔴 **2026-09-19 用户口径：这些地方先用英文**（原版本来就是英文；中文原版**查不到** ——
        //    客户端没有 I2 语言表），**之后再做一次彻底的完全翻译**。所以这里照抄原版英文。
        const float SliderCx = -2.00f;
        static readonly float[] SliderCy = { 120.07f, 3.44f, -113.18f };
        static readonly float[] LabelBottomY = { 151.67f, 35.04f, -81.58f };
        const float LabelLeftX = -282.54f;
        const float LabelFontPx = 42f;                       // 原版 fs42（autoSizing 10–42）
        static readonly string[] SliderNames = { "Music", "Sound Effects", "Voice-overs" };
        /// <summary>正在被拖动的那根（拖动期间不把指针当点击）。</summary>
        WfSlider _dragSlider;

        // 面板尺寸（原版 743.2×758.6 px，1080p 下 108 px/世界单位）
        const float PanelW = 743.2f, PanelH = 758.6f;
        const float ClosePx = 75f;

        // ---- 投降钮：**原版真值**（`资料/普查产出_0918/第18行_UI三小条_规格.md` §①）----
        // 面板内（原点=面板中心，y 向上）中心 (−171.7,−310.5) px、尺寸 300×90 px = 面板左下角。
        // 出处：`assets_full/bundle_scenes_scenes_battlearena1/RectTransform/RectTransform_3211.json`
        //       （`ap(171.7,−50)` `sd(300,90)` anchorMin=anchorMax`(0,1)` pivot`.5,.5`）
        const float ResignCxPx = -171.7f, ResignCyPx = -310.5f;
        const float ResignWPx = 300f, ResignHPx = 90f;
        /// <summary>底图 `40K_button` 的九宫格 border（贴图 px，x/y/z/w = 左/下/右/上）。
        /// 出处：同一张 Sprite 的 `m_Border`。⚠️ 左右各 234 而目标只有 300 宽 ⇒ 中间那段会被压没，
        /// 这是**原版 Sliced 在目标小于 border 时的退化行为**，不是我们挑的。</summary>
        const float ResignBorderL = 234f, ResignBorderB = 46f, ResignBorderR = 234f, ResignBorderT = 46f;
        /// <summary>原版文字字号 fs38（`MonoBehaviour_3799.json` 的 TMP `m_fontSize`）。</summary>
        const float ResignFontPx = 38f;
        /// <summary>原版按钮染色（`MonoBehaviour_4755.json` 那个 Image 的 `m_Color`）。</summary>
        static readonly Color ResignTint = new Color(0.3686f, 0.8941f, 0.5874f, 1f);

        static float U(float px) { return px / 108f; }

        /// <summary>把九宫格根节点下所有块染成同一个色（根节点自己没有 `ImageQuad` 组件）。</summary>
        static void TintAll(GameObject root, Color c)
        {
            if (root == null) return;
            foreach (var q in root.GetComponentsInChildren<ImageQuad>(true)) q.SetTint(c);
        }

        /// <summary>世界坐标是不是落在九宫格根节点的矩形里（它没有 `ImageQuad.Contains`，命中测试自己算）。
        /// 锚点固定 0.5,0.5（`CreateNineSlice` 就是这么摆的）。</summary>
        static bool RectContains(Transform root, Vector3 world, float w, float h)
        {
            if (root == null) return false;
            var l = root.InverseTransformPoint(world);
            return Mathf.Abs(l.x) <= w * 0.5f && Mathf.Abs(l.y) <= h * 0.5f;
        }

        /// <summary>一张**按给定宽高比**的纯白贴图（配 `SetTint` 画任意半透明色块）。
        /// 和 `EndPanel.SolidTex` 同一个做法 —— 纯色块不该去借原版图（那些是带花纹/半透明的）。
        /// 🆕 2026-09-17：开成 `internal` —— 同目录的 `WaitBanner`（原版 `WaitText`）要画同一套
        /// 「整屏压暗 + 实底条」，**别再抄第三份**（`EndPanel` 那份是历史遗留，下次谁碰它顺手并过来）。</summary>
        internal static UnityEngine.Texture2D SolidTex(float aspect)
        {
            int h = 8, w = Mathf.Max(1, Mathf.RoundToInt(8f * aspect));
            var t = new UnityEngine.Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color32[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            t.SetPixels32(px);
            t.Apply();
            t.name = $"Solid_{w}x{h}";
            return t;
        }

        public static SettingsPanel Create(Transform parent, Action onResign, Action onCycleDifficulty)
        {
            var go = new GameObject("SettingsPanel");
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<SettingsPanel>();
            p.OnResign = onResign;
            p.OnCycleDifficulty = onCycleDifficulty;
            p.Build();
            p.Hide();
            return p;
        }

        void Build()
        {
            // ---- 底板：原版是 `Mask`+`Background fill`（图 `40k_popup_texture`）----
            // z 都要**比棋盘/HUD 靠前**（面板盖住它们）：-0.45 比结算面板(−0.50)靠后、比任何卡靠前
            const float Z = -0.45f;
            // 挡在背后的整屏压暗 —— 原版 `BattleSettingsPanel` 里就有一个 `Dark Shade`（3963.5×2693）
            _shade = ImageQuad.Create(transform, SolidTex(LayoutSpace.VisibleWidth / LayoutSpace.DesignHeight),
                                      new Vector3(0f, 0f, Z + 0.02f),
                                      LayoutSpace.DesignHeight * 1.05f, new Vector2(0.5f, 0.5f), "settings_shade");
            if (_shade != null) _shade.SetTint(new Color(0f, 0f, 0f, 0.62f));

            // 面板底：原版是 `Mask` + `Background fill`（图 `40k_popup_texture`，128×128 的九宫格填充）。
            // ⚠️ **不能直接用那张图**（试过）：它是**半透明填充**，拉大到 743×758 之后板子几乎全透、
            //    棋盘从面板里透出来；而且我们没有九宫格，拉大边缘也糊。
            //    所以这里用**自建实底**（深色 + 不透明）+ 上面那层压暗 —— 标记成「我们的做法」。
            _bg = ImageQuad.Create(transform, SolidTex(PanelW / PanelH), new Vector3(0f, 0f, Z),
                                   U(PanelH), new Vector2(0.5f, 0.5f), "settings_bg");
            if (_bg != null) _bg.SetTint(new Color(0.13f, 0.13f, 0.16f, 0.98f));

            // 🔴 2026-09-19 用户口径：面板上这些**先用英文**（之后做彻底的完全翻译）。
            // ⚠️ 原版这个面板**有没有标题查不到**（规格文档 §① 里没记标题这一项）⇒ `Settings` 是**我们起的**。
            _title = Label.Create(transform, "Settings", new Vector3(0f, U(PanelH * 0.5f) - U(70f), Z - 0.01f),
                                  4, new Color(0.95f, 0.93f, 0.88f), new Vector2(0.5f, 0.5f), "settings_title");

            // ---- 关闭钮：75×75，圆底 + 叉（原版 `Generic Close Button`）----
            float cx = U(PanelW * 0.5f) - U(ClosePx * 0.6f);
            float cy = U(PanelH * 0.5f) - U(ClosePx * 0.6f);
            _close = ImageQuad.Create(transform, CardArt.Ui("UI_Button_Round_background"),
                                      new Vector3(cx, cy, Z - 0.01f), U(ClosePx), new Vector2(0.5f, 0.5f), "settings_close");
            _closeIcon = ImageQuad.Create(transform, CardArt.Ui("40k_bt_close"),
                                          new Vector3(cx, cy, Z - 0.02f), U(ClosePx * 0.42f), new Vector2(0.5f, 0.5f), "settings_close_icon");

            // ---- 投降：原版 `Resign`，面板左下角 ----
            // 🔴 **2026-09-19 照原版真值重做** —— 原来这里是 (0,−U(120)) / 宽 249 / 橙字，
            //    三样都是我们挑的（旧注释写的「rect 取不到」是**错误否定**，值一直都在，
            //    见文件头那段的更正与 `资料/普查产出_0918/第18行_UI三小条_规格.md` §①）。
            var resignTex = CardArt.Ui("40K_button");
            _resignTex = resignTex;
            float resignTexW = resignTex != null ? resignTex.width : 489f;
            float resignTexH = resignTex != null ? resignTex.height : 107f;
            _resignBtn = ImageQuad.CreateNineSlice(transform, resignTex,
                                                   new Vector4(ResignBorderL, ResignBorderB, ResignBorderR, ResignBorderT),
                                                   resignTexW, resignTexH,
                                                   new Vector3(U(ResignCxPx), U(ResignCyPx), Z - 0.01f),
                                                   U(ResignWPx), U(ResignHPx), "settings_resign");
            TintAll(_resignBtn, ResignTint);
            // 文字：原版 `Resign`（本地化 key `Battle/Settings/ResignButton`）。
            // 🔴 **2026-09-19 用户口径：先用英文**（原版就是英文；中文**查不到** —— 客户端没有 I2 语言表），
            //    之后再做彻底的完全翻译。字号 fs38 按**拉丁大写高度**定（见 `SliderLabel` 那条注释）。
            _resignText = Label.Create(transform, "Resign", new Vector3(U(ResignCxPx), U(ResignCyPx), Z - 0.02f),
                                       4, Color.white, new Vector2(0.5f, 0.5f), "settings_resign_text");
            if (_resignText != null) _resignText.SetCapHeight(U(ResignFontPx * 0.72f));

            // ---- 对手难度（🆕 2026-09-17）----
            // 用的是**现成的两样东西**：投降那颗钮同一张原版按钮图 `40K_button`（同宽 249 px，
            // 免得压扁），以及 `Label`。位置我们挑的 —— 原版**没有这个入口**
            // （那个旋钮在 `AIBotsConfig` 里、按排位/连败自动挑配置，玩家改不了；
            // 见 `资料/AI_原版反编译_0917.md` §三末）。所以这一行**是我们加的**，如实标着。
            // ⚠️ 它**不跟着投降钮改成九宫格** —— 那颗钮的形制是原版的，这一行是我们自己的，
            //    按贴图宽高比铺就够了（2026-09-19：投降钮改真值时这行原样保留）。
            float diffH = resignTex != null ? 249f * resignTex.height / resignTex.width : 100f;
            // ⚠️ **2026-09-19 从 y=+120 挪到 y=−220**：原来那格正好压在**音乐滑块**（y=120.07）上。
            //    原版没有这一行（那个旋钮在 `AIBotsConfig` 里、玩家改不了），所以**让位的是我们**。
            //    −220 落在原版 `ChatToggle`（−174.94，我们没做）与投降钮（−310.5）之间，不撞任何原版元素。
            float diffY = U(-220f);
            _diffLabel = Label.Create(transform, "AI Difficulty", new Vector3(-U(190f), diffY, Z - 0.02f),
                                      4, new Color(0.95f, 0.93f, 0.88f), new Vector2(0.5f, 0.5f), "settings_diff_label");
            _diffBtn = ImageQuad.Create(transform, resignTex,
                                        new Vector3(U(70f), diffY, Z - 0.01f), U(diffH), new Vector2(0.5f, 0.5f), "settings_diff_btn");
            _diffValue = Label.Create(transform, "", new Vector3(U(70f), diffY, Z - 0.02f),
                                      4, new Color(1f, 0.86f, 0.55f), new Vector2(0.5f, 0.5f), "settings_diff_value");

            // ---- 三根音量滑块（原版 `BattleSettingsWindow` 的 music / soundFX / voiceOver）----
            // 每一根都按「标签在上、滑块在下」；数值一路走到 `AudioMixer.SetFloat("Volume"+组名, dB)`
            // （见 `Core/WarpforgeAudio.cs`，那里面写清了原版的 dB 公式与「只存音乐」那条）。
            _musicSlider = WfSlider.Create(transform, "music", new Vector3(U(SliderCx), U(SliderCy[0]), Z - 0.01f),
                                           WarpforgeAudio.Music, WarpforgeAudio.SetMusic);
            _fxSlider = WfSlider.Create(transform, "fx", new Vector3(U(SliderCx), U(SliderCy[1]), Z - 0.01f),
                                        WarpforgeAudio.SoundFx, WarpforgeAudio.SetSoundFx);
            _voiceSlider = WfSlider.Create(transform, "voice", new Vector3(U(SliderCx), U(SliderCy[2]), Z - 0.01f),
                                           WarpforgeAudio.VoiceOver, WarpforgeAudio.SetVoiceOver);

            _musicLabel = SliderLabel(SliderNames[0], 0, Z);
            _fxLabel = SliderLabel(SliderNames[1], 1, Z);
            _voiceLabel = SliderLabel(SliderNames[2], 2, Z);
        }

        /// <summary>滑块标签：左对齐（锚点 (0,0) = **文字块的左下角**落在给定坐标上）。</summary>
        Label SliderLabel(string text, int i, float z)
        {
            var l = Label.Create(transform, text, new Vector3(U(LabelLeftX), U(LabelBottomY[i]), z - 0.01f),
                                 4, Color.white, new Vector2(0f, 0f), "settings_slider_label_" + i);
            // ⚠️ **英文用「拉丁大写高度」定字号**：fs42 是 TMP 的 font size，而拉丁大写只占约 0.72 em
            //    —— 套 `SetGlyphHeight`（按 1 em 算）会让字**大 39%**。判据同 `UnitChatPanel.cs:183`。
            //    （中文那条路才用 `SetGlyphHeight`；彻底翻译成中文时这里要跟着换。）
            if (l != null) l.SetCapHeight(U(LabelFontPx * 0.72f));
            return l;
        }

        // ==================================================================
        //  滑块交互 —— **真实输入与自检走同一条判定**
        // ==================================================================

        /// <summary>指针这一帧的状态喂进来。`down` = 按住。返回「**滑块接住了这一下**」
        /// （驱动层据此决定要不要再把它当点击转给按钮）。</summary>
        public bool PointerFrame(Vector3 world, bool down)
        {
            if (!Visible) { _dragSlider = null; return false; }
            if (!down) { _dragSlider = null; return false; }

            if (_dragSlider != null) { _dragSlider.SetFromPointer(world); return true; }

            foreach (var s in Sliders)
                if (s != null && s.Contains(world))
                {
                    _dragSlider = s;
                    s.SetFromPointer(world);       // **按下即定位**（原版 `m_TargetGraphic` 只认手柄，这条是我们挑的）
                    return true;
                }
            return false;
        }

        WfSlider[] Sliders { get { return new[] { _musicSlider, _fxSlider, _voiceSlider }; } }

        /// <summary>三根滑块（自检用）。</summary>
        public WfSlider SliderAt(int i) { return Sliders[i]; }
        public int SliderCount { get { return 3; } }

        /// <summary>三根滑块的标签文字（自检用 —— 空标签 = 玩家看到一根没有说明的条）。</summary>
        public string SliderLabelAt(int i)
        {
            var ls = new[] { _musicLabel, _fxLabel, _voiceLabel };
            return ls[i] != null ? ls[i].Text : null;
        }

        /// <summary>难度显示名。🔴 **2026-09-19 改回英文** —— 用的就是**原版的枚举名**
        /// `SuperEasy / Easy / Normal / Hard`（原版没有玩家可见的难度开关，所以没有「原版文案」可抄；
        /// 枚举名是原版的）。之前那套中文是我们起的。（彻底的完全翻译留到后面统一做。）</summary>
        public static string DifficultyName(AiDifficulty d)
        {
            switch (d)
            {
                case AiDifficulty.SuperEasy: return "SuperEasy";
                case AiDifficulty.Easy: return "Easy";
                case AiDifficulty.Hard: return "Hard";
                default: return "Normal";
            }
        }

        /// <summary>按下一档（很简单 → 简单 → 普通 → 困难 → 很简单）。</summary>
        public static AiDifficulty NextDifficulty(AiDifficulty d)
        {
            switch (d)
            {
                case AiDifficulty.SuperEasy: return AiDifficulty.Easy;
                case AiDifficulty.Easy: return AiDifficulty.Normal;
                case AiDifficulty.Normal: return AiDifficulty.Hard;
                default: return AiDifficulty.SuperEasy;
            }
        }

        /// <summary>把当前难度写到按钮上（驱动层改完 `aiDifficulty` 就调它）。</summary>
        public void SetDifficulty(AiDifficulty d)
        {
            if (_diffValue != null) _diffValue.SetText(DifficultyName(d));
        }

        public void Show() { Visible = true; SetActive(true); }
        public void Hide() { Visible = false; SetActive(false); }
        void SetActive(bool on)
        {
            foreach (var go in new[] { _shade, _bg, _close, _closeIcon, _diffBtn })
                if (go != null) go.gameObject.SetActive(on);
            if (_resignBtn != null) _resignBtn.SetActive(on);   // 九宫格根节点（不是 ImageQuad）
            foreach (var l in new[] { _title, _resignText, _diffLabel, _diffValue,
                                      _musicLabel, _fxLabel, _voiceLabel })
                if (l != null) l.gameObject.SetActive(on);
            // 🆕 三根音量滑块。⚠️ `WfSlider` **不是** `MonoBehaviour`，没有 `.gameObject` ——
            //    要它自己的 `SetVisible`（第一次写漏了会在这里编译不过）。
            foreach (var s in Sliders) if (s != null) s.SetVisible(on);
            if (!on) _dragSlider = null;
        }

        /// <summary>指针是不是落在面板上（开着的时候**吃掉**点击，别穿到棋盘）</summary>
        public bool Contains(Vector3 world)
        {
            return _bg != null && _bg.Contains(world);
        }

        /// <summary>这一下点在「投降」上吗</summary>
        public bool HitResign(Vector3 world)
        {
            if (!Visible || _resignBtn == null) return false;
            return RectContains(_resignBtn.transform, world, U(ResignWPx), U(ResignHPx));
        }

        /// <summary>这一下点在「关闭」上吗</summary>
        public bool HitClose(Vector3 world)
        {
            if (!Visible) return false;
            if (_close != null && _close.Contains(world)) return true;
            if (_closeIcon != null && _closeIcon.Contains(world)) return true;
            return false;
        }

        /// <summary>这一下点在「对手难度」上吗</summary>
        public bool HitDifficulty(Vector3 world)
        {
            return Visible && _diffBtn != null && _diffBtn.Contains(world);
        }

        /// <summary>点面板的空白处 —— 吃掉，但不做事（原版有 `Dark Shade` 挡住背后）</summary>
        public bool HitBlank(Vector3 world) { return Visible && Contains(world); }

        // ---- 自检用 ----
        public string ResignText { get { return _resignText != null ? _resignText.Text : null; } }
        /// <summary>投降按钮的世界坐标（自检照着它点 —— 走的是和真实点击同一条命中判定）</summary>
        public Vector3 ResignWorldPos { get { return _resignBtn != null ? _resignBtn.transform.position : Vector3.zero; } }
        /// <summary>难度按钮上写着的字（自检用）</summary>
        public string DifficultyText { get { return _diffValue != null ? _diffValue.Text : null; } }
        /// <summary>难度按钮的世界坐标（自检照着它点）</summary>
        public Vector3 DifficultyWorldPos { get { return _diffBtn != null ? _diffBtn.transform.position : Vector3.zero; } }
        public bool HasArt
        {
            get
            {
                return _bg != null && _bg.Texture != null
                    && _close != null && _close.Texture != null
                    && _resignBtn != null && _resignTex != null
                    && _diffBtn != null && _diffBtn.Texture != null;
            }
        }
    }
}
