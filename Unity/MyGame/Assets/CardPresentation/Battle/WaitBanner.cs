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
// ⚠️ **底板：2026-09-17 已改回原版**（原来写「我们没九宫格 ⇒ 自建实底」，见 `Build()` 里那段更正）——
//    现在用 `ImageQuad.CreateNineSlice`（`40k_popup`，Sliced）+ `CreateTiled`（`40k_popup_texture`，Tiled）。
//    两件原版事实是从场景 JSON 里读出来的，不是猜的：
//    `Generic Popup Background.m_Type = 1`（Sliced）、`Background fill.m_Type = 2`（Tiled）。
using UnityEngine;

namespace CardPresentation
{
    public class WaitBanner : MonoBehaviour
    {
        // ---- 原版字段（1080p 下 108 px = 1 世界单位，和 `SettingsPanel` 同一套换算）----
        const float BarDx = 7f, BarDy = -175.1f;     // `WaitText` 的锚 (0.5,1) 起算
        static readonly Color ShadeColor = new Color(0f, 0f, 0f, 0.6118f);   // 原版 `Dark Shade` 的色

        /// <summary>z —— 和 `SettingsPanel` 同一层（比结算面板靠前、比任何卡靠前）</summary>
        const float Z = -0.45f;

        static float U(float px) { return px / 108f; }

        ImageQuad _shade;
        GameObject _popup;          // 原版 `Generic Popup Background`（九宫格框 + 平铺填充）
        GameObject _fillRoot;       // 填充那层的父节点（单独挂着，自检好分开数）
        Label _text;

        /// <summary>原版 `Generic Popup Background` 的尺寸（**1323×90**）——
        /// ⚠️ 注意它**不是** 父节点 `WaitText` 的 1344×79.4：真正画出来的是这个子节点。</summary>
        const float PopupW = 1323f, PopupH = 90f;

        public bool Visible { get { return _popup != null && _popup.activeSelf; } }
        /// <summary>自检用：现在条上写的字</summary>
        public string ShownText { get { return _text != null ? _text.Text : "<无>"; } }
        /// <summary>自检用：提示条的**世界尺寸**（断言它等于原版 `Generic Popup Background` 的 1323×90 px）</summary>
        public float BarWorldW { get { return U(PopupW); } }
        public float BarWorldH { get { return U(PopupH); } }
        /// <summary>自检用：底板取到图了没有。
        /// 🔴 **必须数「框」自己的块，不能数 `_popup.transform.childCount`** ——
        ///    2026-09-17 第一版就是这么写的，结果**框没建起来（贴图取空了）它照样绿**，
        ///    因为填充那些块也挂在同一个父节点下（典型的「尺子自己会坏」）。</summary>
        public bool BarHasArt { get { return BarFramePieces == 9; } }
        /// <summary>自检用：九宫格框建了几块（原版 `Sliced` 应当是 **9**）</summary>
        public int BarFramePieces
        {
            get
            {
                if (_popup == null) return 0;
                int n = 0;
                foreach (Transform c in _popup.transform)
                    if (c.name.StartsWith("wait_popup_")) n++;
                return n;
            }
        }
        /// <summary>自检用：填充层建了几块（原版 `Tiled`：1323÷128 向上取整 × 90÷128 向上取整 = 11×1）</summary>
        public int BarFillPieces { get { return _fillRoot == null ? 0 : _fillRoot.transform.childCount; } }
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

            // ---- 提示条：位置与尺寸照原版（`runtime_ui_dump_drive_0912.tsv:350-355`）----
            //   `WaitText`(1344×79.4, 只是个容器)
            //     └ `Generic Popup Background` **1323×90** = Image `40k_popup`
            //         · `m_Type = 1`（**Sliced**）、`m_Border = (169,160,169,160)`、`m_Rect = 359×336`
            //         └ `Mask` + `Background fill` = Image `40k_popup_texture`
            //             · `m_Type = 2`（**Tiled**）、`m_Rect = 128×128`、`m_Color` 纯白
            //         └ `Text` 1269.5×70（原版 `m_fontSize = 0` ⇒ 字号取不到，见文件头）
            // 🔴 **2026-09-17 更正**：原来这里写「我们**没有九宫格**，`40k_popup` 硬拉到 1344×79.4 会横向糊成
            //    一条 ⇒ 改用自建实底」，**那是我们挑的**。现在 `ImageQuad.CreateNineSlice / CreateTiled`
            //    补上了这两个原版的 `Image.Type`，就照原版画。
            float cx = U(BarDx);
            float cy = LayoutSpace.DesignHeight * 0.5f + U(BarDy);
            // ⚠️ **两张图在工程的**不同**目录里**：`40k_popup` 在 `Art/ui_deck/`（`CardArt.DeckUi`），
            //    `40k_popup_texture` 在 `Art/ui/`（`CardArt.Ui`）—— 2026-09-17 第一次取错目录，
            //    九宫格静默没建（断言当时也没抓住，见 `BarFramePieces` 那段注释）。
            _popup = ImageQuad.CreateNineSlice(transform, CardArt.DeckUi("40k_popup"),
                                               new Vector4(169f, 160f, 169f, 160f), 359f, 336f,
                                               new Vector3(cx, cy, Z), U(PopupW), U(PopupH), "wait_popup");
            if (_popup == null)
                Debug.LogWarning("[WaitBanner] 提示条底板没建起来（`40k_popup` 没取到？）");
            // 填充在**框后面**（我们这个坐标系的 z 越负越靠前）—— 原版它是被 `Mask` 裁在框里的。
            // 单独挂一个子节点，好让自检能分开数「框几块 / 填充几块」。
            _fillRoot = new GameObject("wait_fillRoot");
            _fillRoot.transform.SetParent(_popup.transform, false);
            _fillRoot.transform.localPosition = new Vector3(0f, 0f, 0.01f);
            ImageQuad.CreateTiled(_fillRoot.transform, CardArt.Ui("40k_popup_texture"), 128f, 128f,
                                  Vector3.zero, U(PopupW), U(PopupH), "fill");

            _text = Label.Create(transform, CardText.Phrase("WAITING FOR OPPONENT"),
                                 new Vector3(cx, cy, Z - 0.01f), 4,
                                 new Color(0.95f, 0.93f, 0.88f), new Vector2(0.5f, 0.5f), "wait_text");
        }

        /// <summary>开 / 关。**重复调用同一个值不会重复设置**（`UpdateHud` 每帧都会调它）。</summary>
        public void SetVisible(bool on)
        {
            if (_popup == null) return;
            if (_popup.activeSelf == on) return;
            _popup.SetActive(on);
            if (_shade != null) _shade.gameObject.SetActive(on);
            if (_text != null) _text.gameObject.SetActive(on);
        }
    }
}
