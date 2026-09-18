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
//    ⚠️ 13 个战场各存一份副本，**只有 `battlearena2` 不同**（anchor(0,0)/ap(0,0)）。**本节代码还没照真值改**，
//    完整规格与出处见 `资料/普查产出_0918/第18行_UI三小条_规格.md` §①。
// ⚠️ 三根音量滑块**没做**：我们没接音频（原版是 music / soundFX / voiceOver）。
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

        ImageQuad _bg, _close, _resignBtn, _shade;
        Label _title, _resignText, _closeText;
        ImageQuad _closeIcon;
        ImageQuad _diffBtn;
        Label _diffLabel, _diffValue;

        // 面板尺寸（原版 743.2×758.6 px，1080p 下 108 px/世界单位）
        const float PanelW = 743.2f, PanelH = 758.6f;
        const float ClosePx = 75f;
        /// <summary>投降按钮的**宽度**（原版 Debug 那排用的就是这个宽度 249 px；高度按图自身的比例来 ——
        /// `40K_button` 是 489×107 的横条，硬拉成 249×100 会横向压扁。
        /// ⚠️ 我们的 `ImageQuad` 是按贴图宽高比推宽度的，不能只喂两个尺寸。</summary>
        const float ResignW = 249f;

        static float U(float px) { return px / 108f; }

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

            _title = Label.Create(transform, "设置", new Vector3(0f, U(PanelH * 0.5f) - U(70f), Z - 0.01f),
                                  4, new Color(0.95f, 0.93f, 0.88f), new Vector2(0.5f, 0.5f), "settings_title");

            // ---- 关闭钮：75×75，圆底 + 叉（原版 `Generic Close Button`）----
            float cx = U(PanelW * 0.5f) - U(ClosePx * 0.6f);
            float cy = U(PanelH * 0.5f) - U(ClosePx * 0.6f);
            _close = ImageQuad.Create(transform, CardArt.Ui("UI_Button_Round_background"),
                                      new Vector3(cx, cy, Z - 0.01f), U(ClosePx), new Vector2(0.5f, 0.5f), "settings_close");
            _closeIcon = ImageQuad.Create(transform, CardArt.Ui("40k_bt_close"),
                                          new Vector3(cx, cy, Z - 0.02f), U(ClosePx * 0.42f), new Vector2(0.5f, 0.5f), "settings_close_icon");

            // ---- 投降：面板左下角 —— ⚠️ **这行仍是旧值（位置是我们挑的）**，原版真值见文件头与
            //      `资料/普查产出_0918/第18行_UI三小条_规格.md` §①（中心 (−171.7,−310.5)px / 300×90 / 绿 / fs38）----
            var resignTex = CardArt.Ui("40K_button");
            float resignH = resignTex != null ? ResignW * resignTex.height / resignTex.width : 100f;
            _resignBtn = ImageQuad.Create(transform, resignTex,
                                          new Vector3(0f, -U(120f), Z - 0.01f), U(resignH), new Vector2(0.5f, 0.5f), "settings_resign");
            _resignText = Label.Create(transform, "投降", new Vector3(0f, -U(120f), Z - 0.02f),
                                       4, new Color(1f, 0.86f, 0.55f), new Vector2(0.5f, 0.5f), "settings_resign_text");

            // ---- 对手难度（🆕 2026-09-17）----
            // 用的是**现成的两样东西**：投降那颗钮同一张原版按钮图 `40K_button`（同宽 249 px，
            // 免得压扁），以及 `Label`。位置我们挑的 —— 原版**没有这个入口**
            // （那个旋钮在 `AIBotsConfig` 里、按排位/连败自动挑配置，玩家改不了；
            // 见 `资料/AI_原版反编译_0917.md` §三末）。所以这一行**是我们加的**，如实标着。
            float diffY = U(120f);
            _diffLabel = Label.Create(transform, "对手难度", new Vector3(-U(190f), diffY, Z - 0.02f),
                                      4, new Color(0.95f, 0.93f, 0.88f), new Vector2(0.5f, 0.5f), "settings_diff_label");
            _diffBtn = ImageQuad.Create(transform, resignTex,
                                        new Vector3(U(70f), diffY, Z - 0.01f), U(resignH), new Vector2(0.5f, 0.5f), "settings_diff_btn");
            _diffValue = Label.Create(transform, "", new Vector3(U(70f), diffY, Z - 0.02f),
                                      4, new Color(1f, 0.86f, 0.55f), new Vector2(0.5f, 0.5f), "settings_diff_value");
        }

        /// <summary>难度显示名（**中文是我们起的**，原版只有枚举名 `SuperEasy / Easy / Normal / Hard`）。</summary>
        public static string DifficultyName(AiDifficulty d)
        {
            switch (d)
            {
                case AiDifficulty.SuperEasy: return "很简单";
                case AiDifficulty.Easy: return "简单";
                case AiDifficulty.Hard: return "困难";
                default: return "普通";
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
            foreach (var go in new[] { _shade, _bg, _close, _closeIcon, _resignBtn, _diffBtn })
                if (go != null) go.gameObject.SetActive(on);
            foreach (var l in new[] { _title, _resignText, _diffLabel, _diffValue })
                if (l != null) l.gameObject.SetActive(on);
        }

        /// <summary>指针是不是落在面板上（开着的时候**吃掉**点击，别穿到棋盘）</summary>
        public bool Contains(Vector3 world)
        {
            return _bg != null && _bg.Contains(world);
        }

        /// <summary>这一下点在「投降」上吗</summary>
        public bool HitResign(Vector3 world)
        {
            return Visible && _resignBtn != null && _resignBtn.Contains(world);
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
                    && _resignBtn != null && _resignBtn.Texture != null
                    && _diffBtn != null && _diffBtn.Texture != null;
            }
        }
    }
}
