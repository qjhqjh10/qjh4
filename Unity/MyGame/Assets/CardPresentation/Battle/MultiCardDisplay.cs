// MultiCardDisplay.cs — **一次摊开多张**的展示窗（原版 `UIMultiCardDisplay` / `BattleHud.DisplayMultiCardView`）
//
// 原版出处（**全是解包/反编译，不是照截图猜的**）：
//   · 2D 全树 `资料/说明书/01_战斗_对战/2D层_battlearena1全树.md:309`
//       `Generic Multi Card Display Combat (inactive) [0,671 0x818]`
//         ├ Header Text [-596,671 1192x63]
//         ├ Viewport [0,671 0x818] └ Content [959,671 -1918x818] └ CardUI Reference
//         └ BattleContinueButton [-857,1492 833x50]（Button 578x64 · Text 'Continue' · CircleButton 80x80）
//   · **逐字段权威表** `资料/战斗规格/战斗重建_0827/子代理读报_front弹层_0827.md` §二：
//       根 `anchor(0,0.5)-(1,0.5)` `size(0, **818.04**)` → 绝对 **y[131,949]**、横向铺满；
//       根上**自带 `ScrollRect`**：`m_Horizontal=1 m_Vertical=0 m_MovementType=1(Elastic)` ⇒ **横向滚动**；
//       `Header Text` `anchor(0.5,1) pivot(0.5,1) size(1192.37,63.204)` · 白 · **fs 38**（auto 18..38）· 居中；
//       `Menu Dark Background` 4574.6×2572.4 · `color(0,0,0,**0.7725**)` · 吃 raycast；
//       Continue 条 577.5×63.84（贴图 `40k_bt_underbutton`，按钮色 `(0.369,0.894,0.587)` 绿）+
//       圆钮 80.47×79.64（贴图 `40k_UI_bt_play`，**纵向凸出横条**：条 y945–1009、钮 y937–1017）；
//       `'Continue'` 文字 **右对齐**、fs 38、白。
//   · 反编译 `BattleHud__DisplayMultiCardView.c`：`UIMultiCardDisplay.Initialize(cards, header)`
//     + `WindowsManager.OpenWindow(...)`；调用点在 `BattleManager__ResolveAction.c`（带一个**本地化过的标题**）。
//
// 🔴 **我们挑的（解包里查不到 / 我们没那套设施，如实标着，别当成原版行为）**：
//   ① **卡与卡的间距 24 px** —— 解包里查不到（原版 `Content` 的宽由 `ContentSizeFitter` 运行时算，
//      静态表里是塌缩值 −1917.7）。
//   ② **卡片高度 743 px** —— 用**放大窗那张的尺寸**（`CardDisplayWindow.CardHeightPx` 同源）。
//      原版 `CardUI Reference` 的 scale 在预制体里，本地没读到。
//   ③ **滚动**：原版是 uGUI `ScrollRect` + `RectMask2D` 裁剪；我们这套 HUD 是**世界空间四边形**、
//      没有 uGUI 遮罩 ⇒ 卡放不下时**先把卡等比缩到装得下**（缩到 45% 仍装不下才让它溢出去）。
//   ④ **「继续」文字右对齐**：我们这块 `Label` 只有居中 ⇒ 用「中心点右移」近似。
//   ⑤ **入口**：原版从 `BattleManager.ResolveAction` 里调（环境卡 / 战绩卡组），我们没有那两个流程
//      ⇒ 接到**点我方牌堆**（摊开牌库）—— **入口是我们挑的**。
//
// ⚠️ 遮罩与标题**建一次就留着**，开关只切 `activeSelf`（照 `CardDisplayWindow` 的教训：
//    每次重建、`Hide` 又销毁的话，第二次打开只剩一张光卡）。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    public class MultiCardDisplay : MonoBehaviour
    {
        // ---- 版面（px @1920×1080，**逐条来自上面那张权威表**）----
        /// <summary>窗口带的上/下缘（绝对 px，左上原点）。原版 `y[131,949]`、高 818.04</summary>
        public const float BandTopPx = 131f, BandBottomPx = 949f;
        /// <summary>标题：宽 1192.37、高 63.204（锚在窗口带**顶边**，`anchor(0.5,1) pivot(0.5,1)`）</summary>
        const float HeaderW = 1192.37f, HeaderH = 63.204f;
        /// <summary>遮罩不透明度。原版 `Menu Dark Background` 的 `color.a`（和放大窗**同一个值**）</summary>
        public const float MaskAlpha = 0.7725f;
        /// <summary>标题字号。原版 fs 38（base 31.9，auto 18..38）</summary>
        const int HeaderFontSize = 38;
        /// <summary>Continue 条：`x[1322.7,1900.2] y[945.2,1009]`</summary>
        const float BarCx = 1611.45f, BarCy = 977.1f, BarW = 577.5f, BarH = 63.84f;
        /// <summary>圆钮：`x[1723.2,1803.7] y[937.3,1016.9]`（**纵向凸出横条**）</summary>
        const float CircleCx = 1763.45f, CircleCy = 977.1f, CircleD = 80.47f;
        /// <summary>Continue 的绿色（原版 Button `colors.normal`）</summary>
        static readonly Color BarColor = new Color(0.369f, 0.894f, 0.587f);

        /// <summary>卡片高度（px）。**我们挑的**（见文件头 ②）</summary>
        public const float CardHeightPx = 743f;
        /// <summary>卡间距（px）。**我们挑的**（见文件头 ①）</summary>
        public const float SpacingPx = 24f;
        /// <summary>两侧留白（px）—— 判断「装不装得下」时按它算可用宽度</summary>
        const float SideMarginPx = 60f;
        /// <summary>缩放下限：再小就不缩了（溢出去也比糊成一团好认）</summary>
        const float MinShrink = 0.45f;

        const float ZMask = -2.2f, ZContent = -2.3f;   // 比放大窗（−2.0/−2.1）还靠前一点

        Transform _root;
        ImageQuad _mask, _bar, _circle;
        Label _header, _barText, _hint;
        readonly List<CardView> _cards = new List<CardView>();

        /// <summary>窗是不是开着（自检用）</summary>
        public bool Visible { get; private set; }
        /// <summary>摊开了几张（自检用）</summary>
        public int CardCount { get { return _cards.Count; } }
        /// <summary>标题那行字（自检用）</summary>
        public string HeaderShown { get; private set; }
        /// <summary>这一排的实际宽度（px；自检用 —— 用来验「装得下 / 缩过」）</summary>
        public float ContentWidthPx { get; private set; }
        /// <summary>画这一排用的缩放（自检用；和「无缩」相比小就是缩过）</summary>
        public float UsedScale { get; private set; }
        /// <summary>窗口带里第一个卡片的中心（px；自检用）</summary>
        public float FirstCardXpx { get; private set; }
        /// <summary>卡片中心 y（px；自检用）</summary>
        public float CardCypx { get; private set; }

        public static MultiCardDisplay Create(Transform parent)
        {
            var go = new GameObject("MultiCardDisplay");
            go.transform.SetParent(parent, false);
            var w = go.AddComponent<MultiCardDisplay>();
            w.Build();
            w.Hide();
            return w;
        }

        void Build()
        {
            _root = transform;

            _mask = ImageQuad.Create(_root, CardDisplayWindow.SolidTexFor(LayoutSpace.VisibleWidth / LayoutSpace.DesignHeight),
                                     Vector3.zero, LayoutSpace.DesignHeight * 1.2f,
                                     new Vector2(0.5f, 0.5f), "mcd_mask");
            if (_mask != null)
            {
                _mask.transform.localPosition = new Vector3(0f, 0f, ZMask);
                _mask.SetTint(new Color(0f, 0f, 0f, MaskAlpha));
            }

            // 标题：锚在窗口带**顶边**（原版 `anchor(0.5,1) pivot(0.5,1)`）
            _header = Label.Create(_root, "", Pos(960f, BandTopPx + HeaderH * 0.5f, ZContent),
                                   HeaderFontSize, Color.white, new Vector2(0.5f, 0.5f), "mcd_header");

            // Continue：**贴图用原版那两张**（`40k_bt_underbutton` / `40k_UI_bt_play`），
            // 图不在工程里时退回纯色（颜色仍是原版那个绿 —— 不静默、也不假装）
            var barTex = CardArt.Ui("40k_bt_underbutton");
            _bar = ImageQuad.Create(_root, barTex != null ? barTex : CardDisplayWindow.SolidTexFor(BarW / BarH),
                                    Pos(BarCx, BarCy, ZContent), BarH, new Vector2(0.5f, 0.5f), "mcd_bar");
            if (_bar != null && barTex == null) _bar.SetTint(BarColor);

            var circleTex = CardArt.Ui("40k_UI_bt_play");
            _circle = ImageQuad.Create(_root, circleTex != null ? circleTex : CardDisplayWindow.SolidTexFor(1f),
                                       Pos(CircleCx, CircleCy, ZContent), CircleD, new Vector2(0.5f, 0.5f), "mcd_circle");

            // 文字**右对齐**在条内右侧（原版 `H=4(Right)`）—— 我们这块 `Label` 只有居中
            // ⇒ 用「中心点右移」近似（**我们挑的**，见文件头 ④）
            _barText = Label.Create(_root, "继续", Pos(BarCx + BarW * 0.5f - 60f, BarCy, ZContent), 38,
                                    Color.white, new Vector2(0.5f, 0.5f), "mcd_continue");

            _hint = Label.Create(_root, "点「继续」或再点一下牌堆关闭", Pos(960f, BandBottomPx + 34f, ZContent),
                                 2, new Color(0.7f, 0.7f, 0.75f), new Vector2(0.5f, 0.5f), "mcd_hint");

            SetChrome(false);
        }

        void SetChrome(bool on)
        {
            if (_mask != null) _mask.gameObject.SetActive(on);
            if (_bar != null) _bar.gameObject.SetActive(on);
            if (_circle != null) _circle.gameObject.SetActive(on);
            if (_barText != null) _barText.gameObject.SetActive(on);
            if (_hint != null) _hint.gameObject.SetActive(on);
            if (_header != null) _header.gameObject.SetActive(on);
        }

        /// <summary>开/关一次（和放大窗一个形状）。</summary>
        public void Toggle(IList<CardData> cards, string header)
        {
            if (Visible) Hide(); else Show(cards, header);
        }

        public void Show(IList<CardData> cards, string header)
        {
            Hide();
            ContentWidthPx = 0f;
            UsedScale = 0f;
            FirstCardXpx = 0f;
            CardCypx = 0f;
            if (cards == null || cards.Count == 0) { HeaderShown = null; return; }

            HeaderShown = header ?? "";
            if (_header != null) _header.SetText(HeaderShown);

            // 卡宽（px）：和放大窗同源 —— 卡本体按「高 743 px」反算缩放
            float baseScale = CardHeightPx / EndPanel.PxPerUnit / CardView.Height;
            float cardWpx = CardHeightPx * (CardView.Width / CardView.Height);
            float gapPx = SpacingPx;
            float scale = baseScale;

            float totalPx = cards.Count * cardWpx + (cards.Count - 1) * gapPx;
            float availPx = LayoutSpace.VisibleWidth - SideMarginPx * 2f;
            if (totalPx > availPx && cards.Count > 1)
            {
                // 装不下 ⇒ **等比缩**（原版这里是横向滚动，见文件头 ③）
                float k = Mathf.Max(MinShrink, availPx / totalPx);
                scale = baseScale * k;
                cardWpx *= k;
                gapPx *= k;
                totalPx = cards.Count * cardWpx + (cards.Count - 1) * gapPx;
            }
            UsedScale = scale;
            ContentWidthPx = totalPx;

            // 竖：标题下面那一条的中点；横：整排居中（原版是 ScrollRect，我们居中 —— 见文件头 ③）
            float cy = (BandTopPx + HeaderH + BandBottomPx) * 0.5f;
            float x0 = 960f - totalPx * 0.5f + cardWpx * 0.5f;
            CardCypx = cy;
            FirstCardXpx = x0;
            for (int i = 0; i < cards.Count; i++)
            {
                var v = CardView.Create(_root, cards[i], "mcd_card" + i);
                if (v == null) continue;
                v.SetPose(Pos(x0 + i * (cardWpx + gapPx), cy, ZContent), 0f, scale);
                v.SetHighlight(CardHighlightState.Normal);   // 展示窗里的卡不吃「可出牌」那套状态色
                _cards.Add(v);
            }

            SetChrome(true);
            Visible = true;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            Visible = false;
            HeaderShown = null;
            for (int i = 0; i < _cards.Count; i++)
                if (_cards[i] != null) Kill(_cards[i].gameObject);
            _cards.Clear();
            SetChrome(false);
        }

        /// <summary>「继续」那一块（横条 + 圆钮）被点到了没有 —— **判据只此一处**（窗口自己的关闭路径）。</summary>
        public bool HitContinue(Vector3 world)
        {
            float px = world.x * EndPanel.PxPerUnit + 960f;
            float py = 540f - world.y * EndPanel.PxPerUnit;
            return px >= BarCx - BarW * 0.5f - CircleD * 0.5f && px <= BarCx + BarW * 0.5f
                && py >= BarCy - BarH * 0.5f && py <= BarCy + BarH * 0.5f;
        }

        /// <summary>点在我方牌堆那一块没有（**入口是我们挑的**，见文件头 ⑤）。</summary>
        public static bool HitDeckPile(Vector3 world)
        {
            return BattleDriver.HitMyDeckPile(world);
        }

        static Vector3 Pos(float px, float py, float z) { return EndPanel.Pos(px, py, z); }

        static void Kill(GameObject o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }
    }
}
