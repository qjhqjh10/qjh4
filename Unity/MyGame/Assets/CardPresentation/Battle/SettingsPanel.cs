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
// ⚠️ **投降按钮的 rect 取不到**（dump 里 `BattleSettingsPanel` 那一支只列到 Auto Zoom / 关闭钮 / Debug 那排，
//    没有 resignButton 节点）—— 位置和大小是**我们挑的**，用原版通用按钮图 `40K_button`。
// ⚠️ 三根音量滑块**没做**：我们没接音频（原版是 music / soundFX / voiceOver）。
using System;
using UnityEngine;

namespace CardPresentation
{
    public class SettingsPanel : MonoBehaviour
    {
        /// <summary>面板是不是开着。开着时**吃掉点击**，别让底下的棋盘也响应。</summary>
        public bool Visible { get; private set; }
        /// <summary>点「投降」时回调（驱动层接 `BattleDriver.Forfeit`）</summary>
        public Action OnResign;

        ImageQuad _bg, _close, _resignBtn, _shade;
        Label _title, _resignText, _closeText;
        ImageQuad _closeIcon;

        // 面板尺寸（原版 743.2×758.6 px，1080p 下 108 px/世界单位）
        const float PanelW = 743.2f, PanelH = 758.6f;
        const float ClosePx = 75f;
        /// <summary>投降按钮的**宽度**（原版 Debug 那排用的就是这个宽度 249 px；高度按图自身的比例来 ——
        /// `40K_button` 是 489×107 的横条，硬拉成 249×100 会横向压扁。
        /// ⚠️ 我们的 `ImageQuad` 是按贴图宽高比推宽度的，不能只喂两个尺寸。</summary>
        const float ResignW = 249f;

        static float U(float px) { return px / 108f; }

        /// <summary>一张**按给定宽高比**的纯白贴图（配 `SetTint` 画任意半透明色块）。
        /// 和 `EndPanel.SolidTex` 同一个做法 —— 纯色块不该去借原版图（那些是带花纹/半透明的）。</summary>
        static UnityEngine.Texture2D SolidTex(float aspect)
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

        public static SettingsPanel Create(Transform parent, Action onResign)
        {
            var go = new GameObject("SettingsPanel");
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<SettingsPanel>();
            p.OnResign = onResign;
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

            // ---- 投降：面板中下方（位置我们挑的，见文件头）----
            var resignTex = CardArt.Ui("40K_button");
            float resignH = resignTex != null ? ResignW * resignTex.height / resignTex.width : 100f;
            _resignBtn = ImageQuad.Create(transform, resignTex,
                                          new Vector3(0f, -U(120f), Z - 0.01f), U(resignH), new Vector2(0.5f, 0.5f), "settings_resign");
            _resignText = Label.Create(transform, "投降", new Vector3(0f, -U(120f), Z - 0.02f),
                                       4, new Color(1f, 0.86f, 0.55f), new Vector2(0.5f, 0.5f), "settings_resign_text");
        }

        public void Show() { Visible = true; SetActive(true); }
        public void Hide() { Visible = false; SetActive(false); }
        void SetActive(bool on)
        {
            foreach (var go in new[] { _shade, _bg, _close, _closeIcon, _resignBtn })
                if (go != null) go.gameObject.SetActive(on);
            foreach (var l in new[] { _title, _resignText }) if (l != null) l.gameObject.SetActive(on);
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

        /// <summary>点面板的空白处 —— 吃掉，但不做事（原版有 `Dark Shade` 挡住背后）</summary>
        public bool HitBlank(Vector3 world) { return Visible && Contains(world); }

        // ---- 自检用 ----
        public string ResignText { get { return _resignText != null ? _resignText.Text : null; } }
        /// <summary>投降按钮的世界坐标（自检照着它点 —— 走的是和真实点击同一条命中判定）</summary>
        public Vector3 ResignWorldPos { get { return _resignBtn != null ? _resignBtn.transform.position : Vector3.zero; } }
        public bool HasArt
        {
            get
            {
                return _bg != null && _bg.Texture != null
                    && _close != null && _close.Texture != null
                    && _resignBtn != null && _resignBtn.Texture != null;
            }
        }
    }
}
