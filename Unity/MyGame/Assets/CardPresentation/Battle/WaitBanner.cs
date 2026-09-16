// WaitBanner.cs — 「等待提示」（原版 `WaitText`）
//
// 原版出处：运行时 dump `资料/原版参照图/Unity参照管线_0825/data/runtime_ui_dump_drive_0912.tsv:350-355`
//   `BackCanvas/Safe area BackCanvas/WaitText`  anchor (0.5,1) · anchoredPosition **(7.0, −175.1)**
//                                              size **1344.0 × 79.4** · activeSelf=**False**
//     ├── `Dark Shade`              size **3963.5 × 3366**（**无图，纯色**，`color = (0,0,0,0.6118)`）
//     └── `Generic Popup Background` size **1323 × 90**  图 `40k_popup`
//           ├── `Mask`                                图 `40k_popup`（拉伸填满）
//           │     └── `Background fill`               图 `40k_popup_texture`（128×128）
//           └── `Text`  anchor(0,1) pos(661.5,−45) size **1269.5 × 70**
//
// 🔴 **这份文档有两处是我们挑的，都写在下面**（原版查不到，别当成复刻）：
//   ① **什么时机显示** —— 原版 `WaitText` 的触发点在反编译与场景 JSON 里**都查不到**。
//      我们接的是「**不是我的回合、且不在换牌/结算**」，也就是对手思考的那段时间。
//   ② **文字文案** —— dump 里 `Text` 节点是**空的**（运行时才赋），原版的本地化 key 没解出来。
//      我们用 `CardText.Phrase("WAITING FOR OPPONENT")`（这一条是**我们加的**词条）。
//
// ⚠️ **底板没有照原版用 `40k_popup` 九宫格** —— 我们**没有九宫格**，
//    而 `40k_popup` 原生 359×336 硬拉到 1344×79.4 会横向糊成一条。
//    `SettingsPanel` 当初撞的是同一堵墙（`Battle/SettingsPanel.cs:77-80`：那张图是半透明填充、
//    拉大后棋盘会透出来）。⇒ 这里**沿用同一套做法：自建实底**，标记成「我们的做法」。
using UnityEngine;

namespace CardPresentation
{
    public class WaitBanner : MonoBehaviour
    {
        // ---- 原版字段（1080p 下 108 px = 1 世界单位，和 `SettingsPanel` 同一套换算）----
        const float BarW = 1344f, BarH = 79.4f;
        const float BarDx = 7f, BarDy = -175.1f;     // 锚 (0.5,1) 起算
        static readonly Color ShadeColor = new Color(0f, 0f, 0f, 0.6118f);   // 原版 `Dark Shade` 的色

        /// <summary>z —— 和 `SettingsPanel` 同一层（比结算面板靠前、比任何卡靠前）</summary>
        const float Z = -0.45f;

        static float U(float px) { return px / 108f; }

        ImageQuad _shade, _bar;
        Label _text;

        public bool Visible { get { return _bar != null && _bar.gameObject.activeSelf; } }
        /// <summary>自检用：现在条上写的字</summary>
        public string ShownText { get { return _text != null ? _text.Text : "<无>"; } }
        /// <summary>自检用：提示条的**世界尺寸**（断言它等于原版 1344×79.4 px / 108）。
        /// ⚠️ 宽要**乘 `localScale.x`** —— 见 `Build()` 里那段校正，`ImageQuad.WorldW` 本身不含缩放。</summary>
        public float BarWorldW
        {
            get { return _bar != null ? _bar.WorldW * _bar.transform.localScale.x : 0f; }
        }
        public float BarWorldH { get { return _bar != null ? _bar.WorldH : 0f; } }
        /// <summary>自检用：压暗层的颜色（断言 α = 原版 0.6118）</summary>
        public Color ShadeTint { get { return _shade != null ? _shade.Tint : Color.clear; } }

        public static WaitBanner Create(Transform parent)
        {
            var go = new GameObject("WaitBanner");
            go.transform.SetParent(parent, false);
            var b = go.AddComponent<WaitBanner>();
            b.Build();
            b.SetVisible(false);
            return b;
        }

        void Build()
        {
            // ---- 整屏压暗（原版 `Dark Shade`：无图、纯色、α 0.6118）----
            _shade = ImageQuad.Create(transform,
                                      SettingsPanel.SolidTex(LayoutSpace.VisibleWidth / LayoutSpace.DesignHeight),
                                      new Vector3(0f, 0f, Z + 0.02f),
                                      LayoutSpace.DesignHeight * 1.05f,
                                      new Vector2(0.5f, 0.5f), "wait_shade");
            if (_shade != null) _shade.SetTint(ShadeColor);

            // ---- 提示条：位置与尺寸都照原版字段 ----
            // 锚 (0.5,1) + anchoredPosition (7,−175.1)，pivot 居中 ⇒ **中心**落在「屏幕顶下方 175.1 px」处
            float cx = U(BarDx);
            float cy = LayoutSpace.DesignHeight * 0.5f + U(BarDy);
            _bar = ImageQuad.Create(transform, SettingsPanel.SolidTex(BarW / BarH),
                                    new Vector3(cx, cy, Z),
                                    U(BarH), new Vector2(0.5f, 0.5f), "wait_bar");
            // ⚠️ 这是**我们的做法**（不是原版的 `40k_popup` 九宫格，理由见文件头）
            if (_bar != null) _bar.SetTint(new Color(0.13f, 0.13f, 0.16f, 0.98f));
            // 🔴 **宽度按原版 px 校正**：`ImageQuad` 的宽是**按贴图宽高比推的**，而 `SolidTex` 只有 8 px 高
            //    ⇒ 1344/79.4 = 16.93 被量化成 135/8 = 16.875，条宽少了 **0.038 世界单位**。
            //    断言卡的是「1344 ÷ 108」这个**确切值**，不是「差不多」——所以这里按比例掰回来。
            if (_bar != null && _bar.WorldW > 0f)
                _bar.transform.localScale = new Vector3(U(BarW) / _bar.WorldW, 1f, 1f);

            _text = Label.Create(transform, CardText.Phrase("WAITING FOR OPPONENT"),
                                 new Vector3(cx, cy, Z - 0.01f), 4,
                                 new Color(0.95f, 0.93f, 0.88f), new Vector2(0.5f, 0.5f), "wait_text");
        }

        /// <summary>开 / 关。**重复调用同一个值不会重复设置**（`UpdateHud` 每帧都会调它）。</summary>
        public void SetVisible(bool on)
        {
            if (_bar == null) return;
            if (_bar.gameObject.activeSelf == on) return;
            _bar.gameObject.SetActive(on);
            if (_shade != null) _shade.gameObject.SetActive(on);
            if (_text != null) _text.gameObject.SetActive(on);
        }
    }
}
