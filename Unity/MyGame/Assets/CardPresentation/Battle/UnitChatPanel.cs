// UnitChatPanel.cs — 原版**单位语音条**（`Unit Chat/PlayerChatDisplay` · `EnemyChatDisplay`）
//
// 规格出处（2026-09-17 复核过原始 JSON，**并更正了权威表的两处标错**）：
//   父链 `FrontCanvas/Safe area FrontCanvas/Unit Chat`(MB `UnitsVoiceLinesPanel`)
//     ├ PlayerChatDisplay  x[12.61,661.38] y[643.50,880.00]   648.77×236.50（贴屏幕左侧、**我方的在下**）
//     └ EnemyChatDisplay   x[12.61,661.38] y[173.50,410.00]   同尺寸（**敌方的在上**）
//   一个气泡里的四层（相对气泡左上角，px）：
//     · Background  铺满气泡         图 `40k_voicelines_radio`（766×280，`m_Type=0` 纯拉伸）
//     · 卡图        中心 (104.5,113.15) 尺寸 144.46×157.46
//                   —— 原版是 `Mask`(RectMask2D) + `CardImage` 176×176（**画得比遮罩大、被裁**），
//                      ⚠️ 我们**不裁**，直接画成遮罩那个尺寸（差别只在边缘，如实记着）
//     · wave        中心 (435.5,68.65) 尺寸 387.95×62.60 · 色 (0.6988,0.9811,0.9265)
//                   图 `40k_voicelines_radio_wave equalizer`（708×96，**非等比拉伸**）
//     · ChatText    区域 (188.5,93.7)–(631.6,175.4) · 原版 TMP **fontSize 33、纯白**
//
// 🔴 **原版那层「波形」是一个频谱等化器**（`AudioWaveEqualizer`：`spectrumModifier=15` /
//    `speedChange=10`，每帧用 `GetSpectrumData` 扰动材质）。⚠️ **2026-09-17 二次核查更正**：
//    那个 shader **在本地**（`d:/2/解包整理/11_着色器/shaders/Shader_2656665607827278157.json`），
//    但 **`m_Name` 是空的、也没有可读的字节码** ⇒ 只能读到它的**属性表**（7 个：
//    `_MainTex` / `_Noise` / `_XOffsets` / `_MovementRange` / `_WaveScaleRemap` /
//    `_Noise1ScaleAndSpeed` / `_Noise2ScaleAndSpeed`），**运算逻辑拿不到**。
//    ⚠️ 场景那个材质上还挂着一堆 `_WaveScale1/2/3` / `_Movement` / `_Resolution` / `_Parallax` ——
//    **那些是内置 Standard shader 的残留值、当前 shader 根本不读**（`CLAUDE.md` 那条「原版材质带残留」的坑）。
//    ⇒ **我们这里画的是静态图**：自写一个等化器等于自己发明（逻辑无法照抄）。这是**如实标注的差异**。
//
// 🔴 **查不到的（所以是我们挑的）**：原版**什么时机播哪一条**没有任何代码或数据（对账表 §三·〇 :127）。
//    后缀集合是文件名给的，**「事件 → 后缀」的对应是我们定的**（见 `VoiceLines.ForDeploy` 那一族）。
//    文本层：普通单位那条台词原文**就在文件名里**（`VO_…_<卡名> - <台词> (配音演员).ogg`），
//    我们把它显示出来；督军那种只有事件后缀的（`…_attack.ogg`）**没有文本可用** ⇒ 退而显示**卡名**（我们的选择）。
using UnityEngine;

namespace CardPresentation
{
    public class UnitChatPanel : MonoBehaviour
    {
        // ---- 原版实测（1920×1080，y 从上算）----
        const float RefW = 1920f, RefH = 1080f;
        const float BubbleW = 648.77f, BubbleH = 236.50f;
        const float PortraitW = 144.46f, PortraitH = 157.46f, PortraitCx = 104.5f, PortraitCy = 113.15f;
        const float WaveW = 387.95f, WaveH = 62.60f, WaveCx = 435.5f, WaveCy = 68.65f;
        const float TextLeft = 188.5f, TextCy = 134.55f;    // 文本区左沿 / 中线（相对气泡左上角）
        static readonly Color WaveTint = new Color(0.6988f, 0.9811f, 0.9265f);
        static readonly Color TextColor = Color.white;
        /// <summary>原版 `ChatText` 的 TMP 字号（em）。英文文案按**拉丁大写高度** ≈ 0.72 em 折算。</summary>
        const float OrigFontPx = 33f;

        /// <summary>HUD 图那一层的 z（和 `ReplayBar` / `BattleDriver.HudImageZ` 一致）。
        /// ⚠️ **同一 z 的透明 quad 谁压谁是不确定的**（`BattleDriver.HudDecorZ` 那段注释踩过）
        /// ⇒ 这里**按原版的兄弟序显式分层**：卡图 → 气泡底 → 波形 → 文字（z 越大越远）。</summary>
        const float Z = 0.25f;
        const float ZPortrait = Z + 0.03f;      // 原版 `Mask`(卡图) 在最底下，被气泡底那层半透屏盖着
        const float ZBg = Z;
        const float ZWave = Z - 0.01f;
        const float ZText = Z - 0.02f;

        class Bubble
        {
            public ImageQuad bg, portrait, wave;
            public Label text;
            public AudioSource audio;
            public float left;
            public bool Visible { get { return bg != null && bg.gameObject.activeSelf; } }
            public void SetActive(bool on)
            {
                if (bg != null) bg.gameObject.SetActive(on);
                if (portrait != null) portrait.gameObject.SetActive(on);
                if (wave != null) wave.gameObject.SetActive(on);
                if (text != null) text.gameObject.SetActive(on);
            }
        }

        Bubble _player, _enemy;
        float _left;                 // 还剩多少秒（两边的气泡共用一个倒计时 —— 原版是两条独立的，
                                     // 但我们同一时刻只会显示一条：对手说话时我没在说）
        Bubble _current;

        public bool Ready { get { return _player != null && _player.bg != null; } }
        /// <summary>自检用：现在亮着的是哪一侧（`-1` = 都没显示）</summary>
        public int ShownSide
        {
            get
            {
                if (_player != null && _player.Visible) return 0;
                if (_enemy != null && _enemy.Visible) return 1;
                return -1;
            }
        }
        /// <summary>自检用：条上写的字</summary>
        public string ShownText
        {
            get { return _current != null && _current.text != null ? _current.text.Text : "<无>"; }
        }
        /// <summary>自检用：这一条是谁的（卡 id / 事件 / 音频名）</summary>
        public string LastCardId { get; private set; }
        public string LastEvent { get; private set; }
        public string LastClipName { get; private set; }
        /// <summary>自检用：音频**取到了没有**（批处理里放不出声，但能断言「挂上去了」）</summary>
        public bool LastClipLoaded { get; private set; }
        /// <summary>自检用：还剩几秒</summary>
        public float TimeLeft { get { return _left; } }
        /// <summary>自检用：图都取到了没有（两张原版图任一缺失就报 false）</summary>
        public bool HasArt
        {
            get
            {
                return _player != null && _player.bg != null && _player.bg.Texture != null
                    && _player.wave != null && _player.wave.Texture != null
                    && _player.portrait != null;
            }
        }

        /// <summary>自检用：气泡中点的世界坐标（断言它落在原版矩形上）</summary>
        public Vector3 BubbleCenterWorld(int side)
        {
            var b = side == 0 ? _player : _enemy;
            return b != null && b.bg != null ? b.bg.transform.position : Vector3.zero;
        }
        /// <summary>自检用：气泡的世界尺寸（原版 648.77×236.50 px）</summary>
        public Vector2 BubbleWorldSize
        {
            get
            {
                if (_player == null || _player.bg == null) return Vector2.zero;
                return new Vector2(_player.bg.WorldW, _player.bg.WorldH);
            }
        }

        public static UnitChatPanel Create(Transform parent)
        {
            var go = new GameObject("UnitChatPanel");
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<UnitChatPanel>();
            p.Build();
            return p;
        }

        void Build()
        {
            // 我方在下（y 643.5 起）、敌方在上（y 173.5 起）—— 和原版一致
            _player = MakeBubble("PlayerChatDisplay", 643.50f);
            _enemy = MakeBubble("EnemyChatDisplay", 173.50f);
            HideAll();
        }

        Bubble MakeBubble(string name, float topPx)
        {
            var b = new Bubble();
            b.left = 12.61f;

            var radio = CardArt.Ui("40k_voicelines_radio");
            var waveTex = CardArt.Ui("40k_voicelines_radio_wave_equalizer");
            if (radio == null || waveTex == null)
                Debug.LogWarning($"[UnitChat] 语音条图没取到（radio={radio != null} wave={waveTex != null}）"
                               + " —— 跑 `工具/sync_battle_ui_art.py` 同步");

            b.bg = Quad(b.left + BubbleW * 0.5f, topPx + BubbleH * 0.5f, radio, BubbleH, "chat_bg_" + name, ZBg);
            // 卡图先拿一张**占位**建起来（原版是运行时 `set` sprite）—— 说话时再 `SetTexture` 换成那张卡的立绘
            b.portrait = Quad(b.left + PortraitCx, topPx + PortraitCy, CardArt.Solid(), PortraitH,
                              "chat_card_" + name, ZPortrait);
            b.wave = Quad(b.left + WaveCx, topPx + WaveCy, waveTex, WaveH, "chat_wave_" + name, ZWave);
            if (b.wave != null) b.wave.SetTint(WaveTint);

            // 文本区原版是 anchor(0.29062,0.25823)-(0.97362,0.60365) 的**拉伸矩形**、
            // 对齐 `H=1(左) V=512(中)` ⇒ 我们按**左对齐**摆（轴心 x=0），y 取那条的中线。
            var t = Label.Create(transform, "", Spot(b.left + TextLeft, topPx + TextCy, ZText), 4, TextColor,
                                 new Vector2(0f, 0.5f), "chat_text_" + name);
            // 原版是 TMP `fontSize 33`（= 33 px 的 em）。**英文**按拉丁大写 ≈ 0.72 em；
            // 中文文案（退而显示卡名时）按汉字高度 ≈ 1 em —— 判据照 `SkillPanel.cs:78-80` 那条。
            if (t != null) t.SetCapHeight(OrigFontPx * 0.72f / 108f);
            b.text = t;

            b.audio = gameObject.AddComponent<AudioSource>();
            b.audio.playOnAwake = false;
            b.audio.spatialBlend = 0f;            // 2D（原版也挂在 UI 上）
            b.audio.volume = 0.85f;
            return b;
        }

        ImageQuad Quad(float xPx, float yPxTop, Texture tex, float hPx, string goName, float z)
        {
            if (tex == null) return null;
            var q = ImageQuad.Create(transform, tex, Spot(xPx, yPxTop, z), hPx / 108f,
                                     new Vector2(0.5f, 0.5f), goName);
            return q;
        }

        static Vector3 Spot(float xPx, float yPxTop, float z = Z)
        {
            var v = LayoutSpace.ToWorld(xPx / RefW, 1f - yPxTop / RefH);
            return new Vector3(v.x, v.y, z);
        }

        /// <summary>分辨率变了要重算（`BattleDriver.ReanchorHud` 里调）。</summary>
        public void RefreshLayout()
        {
            Place(_player, 643.50f, BubbleW, PortraitW, PortraitH);
            Place(_enemy, 173.50f, BubbleW, PortraitW, PortraitH);
        }

        void Place(Bubble b, float topPx, float bubbleW, float pw, float ph)
        {
            if (b == null) return;
            float left = 12.61f;
            if (b.bg != null)
            {
                var p = Spot(left + bubbleW * 0.5f, topPx + BubbleH * 0.5f, ZBg);
                b.bg.transform.localPosition = p;
                b.bg.SetAspect(BubbleW / BubbleH);          // 原版 `m_Type=0` 纯拉伸
            }
            if (b.portrait != null)
            {
                b.portrait.transform.localPosition = Spot(left + PortraitCx, topPx + PortraitCy, ZPortrait);
                b.portrait.SetAspect(pw / ph);
            }
            if (b.wave != null)
            {
                b.wave.transform.localPosition = Spot(left + WaveCx, topPx + WaveCy, ZWave);
                b.wave.SetAspect(WaveW / WaveH);            // 图是 708×96 ⇒ 这里就是原版那次非等比拉伸
            }
            if (b.text != null) b.text.transform.localPosition = Spot(left + TextLeft, topPx + TextCy, ZText);
        }

        /// <summary>
        /// 说一句。<paramref name="artKey"/> = 立绘键（`BattleDriver.ArtKey`，**判据只此一处**）；
        /// <paramref name="text"/> 为空时退回显示 <paramref name="fallbackName"/>（卡名）。
        /// <paramref name="clip"/> 为 null = 这张卡没语音（那就不该调用本方法）。
        /// </summary>
        public void Speak(int side, string cardId, string evt, string artKey, string fallbackName,
                          string text, AudioClip clip, string clipFile = null)
        {
            var b = side == 0 ? _player : _enemy;
            if (b == null) return;

            HideAll();                                  // 同时只显示一条（另一侧说话就把这条顶掉）
            _current = b;
            _left = Duration(clip);
            LastCardId = cardId; LastEvent = evt; LastClipName = clipFile;
            LastClipLoaded = clip != null;

            if (b.portrait != null)
            {
                var tex = CardArt.Portrait(artKey);
                if (tex == null) tex = CardArt.PortraitByName(fallbackName);
                if (tex != null)
                {
                    b.portrait.SetTexture(tex);
                    b.portrait.SetAspect(PortraitW / PortraitH);   // 换图会带进贴图比例，拽回原版矩形
                }
            }
            if (b.text != null)
            {
                string shown = string.IsNullOrEmpty(text) ? (fallbackName ?? "") : text;
                b.text.SetText(shown);
                // 中文（退回卡名那一支）按汉字高度定字号，英文按拉丁大写 —— 见 `MakeBubble` 的注释
                if (IsAscii(shown)) b.text.SetCapHeight(OrigFontPx * 0.72f / 108f);
                else b.text.SetGlyphHeight(OrigFontPx / 108f);
            }
            b.SetActive(true);

            // ⚠️ 用 `clip=` + `Play()` 而不是 `PlayOneShot` —— 后者的声音**停不掉**
            //    （`Stop()` 管不到 one-shot），而我们要在气泡收起来时把话掐掉
            if (clip != null && b.audio != null) { b.audio.clip = clip; b.audio.Play(); }
        }

        static bool IsAscii(string s)
        {
            if (string.IsNullOrEmpty(s)) return true;
            for (int i = 0; i < s.Length; i++) if (s[i] > 127) return false;
            return true;
        }

        /// <summary>
        /// 显示多久。⚠️ **原版查得到两个常量**：`MAX_AUDIO_DURATION = 2 s`（截断）、
        /// `CHAT_INTERACTABLE_COOLDOWN = 4 s`；但**没有**「气泡停留多久」那条。
        /// 我们取 `clamp(音频长度, 1.0, 4.0) + 0.4` —— **这是我们挑的**（按音频长度走最省心，
        /// 也不会出现「说完了气泡还挂着」或「话没说完就收了」）。
        /// </summary>
        static float Duration(AudioClip clip)
        {
            float len = clip != null ? clip.length : 1.5f;
            return Mathf.Clamp(len, 1.0f, 4.0f) + 0.4f;
        }

        public void HideAll()
        {
            if (_player != null) _player.SetActive(false);
            if (_enemy != null) _enemy.SetActive(false);
            _current = null;
            _left = 0f;
        }

        /// <summary>走表（`BattleDriver.AdvanceTimeline` 里每帧推，批处理靠 `Step` 推）。</summary>
        public void Advance(float dt)
        {
            if (_current == null) return;
            _left -= dt;
            if (_left > 0f) return;
            if (_current.audio != null && _current.audio.isPlaying) _current.audio.Stop();
            HideAll();
        }
    }
}
