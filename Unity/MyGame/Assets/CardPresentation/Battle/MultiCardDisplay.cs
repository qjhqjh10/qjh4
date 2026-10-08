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
//   ② **卡片高度 743 px** —— 🔴 **2026-09-28 查实：这不是「我们挑的」，是原版值**
//      （原来写「原版 `CardUI Reference` 的 scale 在预制体里，本地没读到」—— 那个字段一直在，
//       只是当时没找到节点）。它 = `RectTransform_3421.json` 的 `m_LocalScale = **223.14**`
//      × 卡体 `3.3313` = **743.35 px**。⚠️ **它属于【多卡展示窗】这棵树**，
//      与「卡片详情窗」（`CardDisplayWindow`，卡体 **832.825 px**）**不是同一个尺寸**
//      —— 2026-09-28 订正，判据 → `资料/阶段二_卡片详情窗_原版规格.md` §十·4。
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
        /// <summary>标题与「继续」两个字的字号（px）。**两颗都是原版 TMP `m_fontSize = 38`**
        /// （2026-10-18 现读 `bundle_scenes_scenes_battlearena1`）：`…/Header Text` = 38
        /// （`m_fontSizeBase 31.9`、auto 18..38）；`…/BattleContinueButton/Text`（MB 3886）
        /// `m_fontSize = 38` · `m_fontSizeMax = 38` · `m_text = "Continue"` · `m_HorizontalAlignment = 4(Right)`。
        /// ⚠️ **这是 px（em 的像素值），⛔ 不是 `Label.Create` 的第 4 参** —— 那个是整数 scale 档，
        /// 见 <see cref="ChromeFontScale"/>。</summary>
        const float ChromeFontPx = 38f;
        /// <summary>`Label.Create` 第 4 参要的**整数 scale 档**（⛔ 不是字号；同目录 20 处调用全是 1~8）。
        /// 档位换算：点阵/TMP 每档 = **7 px 大写字高**（`Battle/Label.cs:207` `CapHeightWorldOfScale = 7f/100`），
        /// 而拉丁大写 ≈ **0.72 em**（`Battle/Label.cs:268`）⇒ 原版 fs 38 要的大写字高 = `38 × 0.72 ≈ 27.4 px`
        /// ⇒ `27.4 / 7 ≈ 3.9` ⇒ 最近整数档 = **4**（3 档 → 21 px，小 23%；5 档 → 35 px，大 28%）。
        /// 真正的字号**一律走 `Label.SetScriptHeight(text, px, pxPerUnit)`** —— 那是「按语种二选一」的
        /// **唯一一份**口径（汉字 `SetGlyphHeight(≈1em)` / 拉丁 `SetCapHeight(≈0.72em)`，`Battle/Label.cs:266`）；
        /// 兄弟件同一个形状：`Battle/MulliganPanel.cs:238` · `Battle/CardChoicePanel.cs:156`。
        /// ⚠️ 原来这两处把 **38 当 scale 传**（= 原版的 12.7 倍），实测症状 = 画面右下角两个巨大白字。</summary>
        const int ChromeFontScale = 4;
        /// <summary>Continue 条：`x[1322.7,1900.2] y[945.2,1009]`</summary>
        const float BarCx = 1611.45f, BarCy = 977.1f, BarW = 577.5f, BarH = 63.84f;
        /// <summary>🔴 **Continue 条那颗 `Image` 自己的 `m_RaycastPadding`**（UGUI 分量序 **L,B,R,T**）——
        /// **负 = 外扩**。判据（2026-10-18 · 第六会话现读）=
        /// `python -I d:/tmp/wf_hit/rcunion.py bundle_scenes_scenes_battlearena1 "BattleContinueButton" --depth 3`：
        /// `/BattleContinueButton/Button` 的 `Image`(`40k_bt_underbutton`) = `RT=1` ·
        /// `pad={'x':0.0,'y':-40.0,'z':0.0,'w':-40.0}` ⇒ **上下各外扩 40 px、横向一个像素都不动**。
        /// ⚠️ **不必再乘缩放**：`RectTransform_3348`（那颗钮自己）的 `m_LocalScale = (1,1,1)`，
        /// 且父链 `BattleContinueButton`(3058) → 根(3175) 的 scale 也都是 1（`Viewport`(3176) 的 0.7 在**卡片那一支**上）。
        /// ⛔ 外扩一律走 `MenuDraw.PaddedRect`（「正值缩小、负值扩大」的唯一一份算式），别在本地另写一遍。</summary>
        static readonly Vector4 ContinuePad = new Vector4(0f, -40f, 0f, -40f);
        /// <summary>圆钮：`x[1723.2,1803.7] y[937.3,1016.9]`（**纵向凸出横条**）。
        /// `CircleD` = 原版 rect 的**宽**（也是 `HitContinue` 认的那一份，口径未改）；
        /// `CircleH` = 它的**高** —— 原版那一格**不是正方**（80.47 × 79.64）。
        /// ⚠️ `ImageQuad.Create` 只吃**高**（宽 = 高 × 显示比例），所以画的时候喂的是 `CircleH`
        /// + `SetAspect(CircleD / CircleH)` —— 兄弟件 `CardChoicePanel.cs:71` 的 `PlayH = 79.64` 也是这个高
        /// （它们没补 `SetAspect`，因为 `40k_UI_bt_play` 是 128×128、补不补差 1%）。</summary>
        const float CircleCx = 1763.45f, CircleCy = 977.1f, CircleD = 80.47f, CircleH = 79.64f;
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
            // 🔴 **2026-10-11（A218）**：根节点是 `RectTransform` + 写 `sizeDelta`。
            //    判据 = 原版同名件 `Generic Multi Card Display Combat` 实读（`bundle_scenes_scenes_battlearena1`，
            //    2026-10-11 现读）：`RectTransform` · `anchor (0,0.5)-(1,0.5)`（**横向 stretch**）·
            //    `sizeDelta (0, **818.04**)` ⇒ 绝对矩形 **x 铺满 1920 · y 818.04**（与本文件头引的
            //    「2D 全树 `[0,671 0x818]` / `size(0,818.04)` → y[131,949]」逐位一致）。
            //    ⚠️ 原版靠横向 stretch 拿父宽 ⇒ 我们用「重合锚点 + 1920×818.04」表达同一个矩形（锚点不复刻）。
            //    改坏法：删掉 `SetPxSize` ⇒ `Editor/BattleScene.cs` §A218「多卡展示窗根 = 1920×818.04」红。
            //    ⚠️ 高度取本文件那对上下缘（`BandBottomPx − BandTopPx` = 818，原版精确值 818.04 ——
            //       那 0.04px 的取整在本文件全部几何里已经这样用了，⛔ 别在这儿另立一个数）。
            var go = new GameObject("MultiCardDisplay", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            MenuDraw.SetPxSize(go.transform, LayoutSpace.DesignPxW, BandBottomPx - BandTopPx);
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
            // 第 4 参是**整数 scale 档**（⛔ 不是原版那个 fs 38 —— 那个走 `SetScriptHeight`，见常量处的注释）。
            // 出厂这一下只是给个尺寸：标题文字是 `Show()` 里才定的（语种跟着标题走）⇒ 字号在那边再设一遍。
            _header = Label.Create(_root, "", Pos(960f, BandTopPx + HeaderH * 0.5f, ZContent),
                                   ChromeFontScale, Color.white, new Vector2(0.5f, 0.5f), "mcd_header");
            if (_header != null) _header.SetScriptHeight("", ChromeFontPx, EndPanel.PxPerUnit);

            // Continue：**贴图用原版那两张**（`40k_bt_underbutton` / `40k_UI_bt_play`），
            // 图不在工程里时退回纯色（颜色仍是原版那个绿 —— 不静默、也不假装）
            //
            // 🔴 **2026-10-18：这一块原来两处都错，本件修的就是它们**
            //  ① **量纲**：`ImageQuad.Create` 第 4 参是**世界高度**，原来喂的是 px 原值
            //     （`BarH` / `CircleD`）⇒ 实绘 = 原版 rect 的 **108 倍**（条 40,285 × 6,894 px，
            //      整屏被 `40k_bt_underbutton` 铺满 —— 实据 `d:/4/_tmp_view/battle/11b_多张展示窗.png`）。
            //     改用 `U(...)`（见本文件 `U` 的定义与那三条兄弟件出处）。
            //  ② **显示比例**：`ImageQuad` 默认按**贴图比例**定宽（`ImageQuad.cs:124` `_aspect = tex.w/tex.h`），
            //     而原版这两格都是 `m_PreserveAspect = 0` + `m_Type = 0(Simple)` ⇒ **拉满 rect**。
            //     判据 = **2026-10-18 现读** `bundle_scenes_scenes_battlearena1`：
            //       `Generic Multi Card Display Combat/BattleContinueButton/Button`（RT 3348）
            //         `m_SizeDelta = (577.50, **63.84**)` · 它的 Image（MB 4202）`m_PreserveAspect = 0` · `m_Type = 0`；
            //       `…/CircleButton`（RT 3384）`m_SizeDelta = (**80.47**, **79.64**)` · Image（MB 4285）同样 PA=0 / Type=0。
            //     贴图实际尺寸：`40k_bt_underbutton` = **485×83**（比例 5.8434，≠ 9.046）
            //       ⇒ 不补 `SetAspect` 只画出 **372.9 × 63.84**（**窄 204.6 px**）；
            //       `40k_UI_bt_play` = **128×128**（正方，≠ 1.0104）⇒ 差 1%（0.83 px），照旧补上。
            //     兄弟件同一条：`Battle/CardChoicePanel.cs:161` · `Battle/MulliganPanel.cs:265`
            //       （两句都是 `if (… != null) ….SetAspect(BarW / BarH);`）。
            var barTex = CardArt.Ui("40k_bt_underbutton");
            _bar = ImageQuad.Create(_root, barTex != null ? barTex : CardDisplayWindow.SolidTexFor(BarW / BarH),
                                    Pos(BarCx, BarCy, ZContent), U(BarH), new Vector2(0.5f, 0.5f), "mcd_bar");
            if (_bar != null) _bar.SetAspect(BarW / BarH);
            if (_bar != null && barTex == null) _bar.SetTint(BarColor);

            var circleTex = CardArt.Ui("40k_UI_bt_play");
            _circle = ImageQuad.Create(_root, circleTex != null ? circleTex : CardDisplayWindow.SolidTexFor(1f),
                                       Pos(CircleCx, CircleCy, ZContent), U(CircleH), new Vector2(0.5f, 0.5f), "mcd_circle");
            if (_circle != null) _circle.SetAspect(CircleD / CircleH);

            // 文字**右对齐**在条内右侧（原版 `H=4(Right)`）—— 我们这块 `Label` 只有居中
            // ⇒ 用「中心点右移」近似（**我们挑的**，见文件头 ④）
            // 🔴 **2026-10-18（第十二轮 · W6）**：那颗钮上的字走**原版词条** `Battle/Mulligan/ButtonDone`
            //    （原版 `Generic Multi Card Display Combat < BattleContinueButton < Text` 挂的就是它，
            //     TMP 原文 `Continue`）—— 原来写死的是中文 `"继续"`（与换牌面板那条是**同一条**词条）。
            string continueText = Loc.T(MulliganPanel.DoneTerm);
            _barText = Label.Create(_root, continueText, Pos(BarCx + BarW * 0.5f - 60f, BarCy, ZContent), ChromeFontScale,
                                    Color.white, new Vector2(0.5f, 0.5f), "mcd_continue");
            if (_barText != null) _barText.SetScriptHeight(continueText, ChromeFontPx, EndPanel.PxPerUnit);

            // ⚠️ **这行提示是我们自加的**：原版 `Generic Multi Card Display Combat` 下只有
            //    `Header Text`（TMP = `Header Text`、**无 `Localize`**）· `BattleContinueButton` ·
            //    `CardUI Reference` · `Viewport` ⇒ **原版这一格零词条**（铁律 11 例外①）。
            //    它里面那个「继续」是**夹在中文句子里**的 ⇒ 英文档下这行会露中文 ——
            //    **这是已知缺口**（没有原版键可接，要修得先自拟一条两条语言都编的键），已记进交件报告。
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

            // 标题的文字与字号**不在这里设** —— 挪到本方法末尾（`SetChrome(true)` 之后）
            // 🔴 理由：TMP **在对象没激活时量不出尺寸**（`ForceMeshUpdate()` 要在 `SetActive(true)`
            //    之后调，否则 `textBounds` 是垃圾 —— CLAUDE.md §三那条坑）；而本方法开头刚跑过
            //    `Hide()` ⇒ 到这里 chrome 整排是关着的。
            HeaderShown = header ?? "";

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

            // 标题的文字 + 字号（见本方法开头那条：必须在 chrome **点亮之后**做）
            if (_header != null)
            {
                _header.SetText(HeaderShown);
                // 🔴 字号要在**文字定下来之后**设：`SetScriptHeight` 按 `Loc.HasCjk(text)` 二选一
                //    （汉字 `SetGlyphHeight(≈1em)` / 拉丁 `SetCapHeight(≈0.72em)`）—— 标题是本地化串，
                //    语种得看这一串本身。与 `MulliganPanel.SetTurnText` 同一个形状。
                _header.SetScriptHeight(HeaderShown, ChromeFontPx, EndPanel.PxPerUnit);
            }
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

        /// <summary>「继续」那一块（横条）被点到了没有 —— **判据只此一处**。
        /// <para>🔴 **2026-10-18（`A1061`）按订正后的口径重算** —— 判据（第一权威 = 原版 prefab 实读）=
        /// `python -I d:/tmp/wf_hit/rcunion.py bundle_scenes_scenes_battlearena1 "BattleContinueButton" --depth 3`：
        /// 子树里**只有一颗**可射线件 = `/BattleContinueButton/Button` 的 `Image`（`40k_bt_underbutton`，
        /// 577.50 × 63.84，`m_RaycastTarget = 1`），它自己的 `m_RaycastPadding = (0,-40,0,-40)`
        /// （**负 = 外扩**）⇒ **上下各外扩 40**、横向不变；`Text`（TMP）与 `CircleButton`
        /// （`40k_UI_bt_play`）的 `m_RaycastTarget` **都是 0** ⇒ **它们一颗都不吃射线**
        /// （`CircleButton` 上那颗 `EverguildButtonStateFollower` 只跟随状态、**不是** `Button`，
        /// 它 `m_Transition=2` 换的也是**圆钮自己那张图**，与点击无关）。
        /// ⇒ 原版可点区 = **条矩形横向原样、纵向 ±40**。
        /// ⚠️ **2026-10-18 更正（铁律 5）**：本函数原来写
        /// `px &gt;= BarCx − BarW/2 − CircleD/2`（**向左多出圆钮半径 40.235 px**）且**纵向没有那 ±40 外扩**
        /// ⇒ **两处都不对**。错因 = 只见「圆钮 `RT=0`、条 `RT=1`」就**推定「圆钮凸出去的那半圈要算进来」**，
        /// **既没读条自己的 `m_RaycastPadding`、也没核圆钮往哪边凸** —— 原版与我们都把圆钮画在条的
        /// **内部右端**（圆钮 x[1723.2,1803.7] ⊂ 条 x[1322.7,1900.2]，只有**纵向**凸出 ~8 px，
        /// 而那 8 px 已被 ±40 的外扩整个盖住）。</para>
        /// <para>⚠️ **顺手现读（不是本函数的口径问题）**：生产路径 `BattleDriver.TickMultiCards`
        /// **不调本函数** —— 那一支按「多卡窗开着时**松手即关**」处理（原版对应的是整屏
        /// `Menu Dark Background` 上的 `BackgroundCloseButton`）⇒ 本函数今天**只有自检在用**
        /// （`Editor/BattleScene.cs` 的 `A964/E2④`）。留给将来「按条/圆钮判」那天照这里算。</para></summary>
        public bool HitContinue(Vector3 world)
        {
            float px = world.x * EndPanel.PxPerUnit + 960f;
            float py = 540f - world.y * EndPanel.PxPerUnit;
            // 条那颗 `Image` 的矩形（px，左上原点）→ 按它自己的 `m_RaycastPadding` 外扩（共用那一份算式）
            var hit = MenuDraw.PaddedRect(
                new PxRect(BarCx - BarW * 0.5f, BarCy - BarH * 0.5f, BarCx + BarW * 0.5f, BarCy + BarH * 0.5f),
                ContinuePad);
            return px >= hit.x1 && px <= hit.x2 && py >= hit.y1 && py <= hit.y2;
        }

        // ---- 🆕 2026-10-18（A964① ④）：「继续」那一块的**实绘几何**只读口（⛔ 只给自检）----
        //  补它的理由：上面 `HitContinue` 用的是**硬写的原版 px 矩形**（`BarCx/BarW` 那一串
        //  + 圆钮探出的那半圈），而**画出来的**是 `_bar` / `_circle` 两颗 quad
        //  （宽由**贴图比例**定，未必等于那个原版 rect）⇒ 没有实绘口就**无从比**「命中区 ⊇ 实绘」。
        //  ⚠️ **2026-10-18 更正（铁律 5）**：本行原写「⛔ 本件**不改** `HitContinue` 的口径
        //  （改命中＝改行为，要另开一件、另配断言）」—— 那一句**已被 `A1061` 兑现**：
        //  口径**改了**（按订正后的原版判据重算，见 `HitContinue` 的注释），
        //  配套断言 = `Editor/BattleScene.cs` 的 `A964/E2④` 那两条**口径判别式**（`A1061①②`）
        //  + 两条原有的（内侧中 / 外侧不中）。

        /// <summary>「继续」横条的**实绘**世界中心（= 那颗 quad 的中心）。</summary>
        public Vector3 ContinueBarWorldPos
        {
            get { return _bar != null ? _bar.transform.position : Vector3.zero; }
        }

        /// <summary>「继续」横条**实绘**的 px 尺寸（判据 = `ImageQuad.WorldW/WorldH × 108`）。</summary>
        public Vector2 ContinueBarDrawnPx
        {
            get { return _bar != null
                       ? new Vector2(_bar.WorldW, _bar.WorldH) * EndPanel.PxPerUnit
                       : Vector2.zero; }
        }

        /// <summary>圆钮的**实绘**世界中心。</summary>
        public Vector3 ContinueCircleWorldPos
        {
            get { return _circle != null ? _circle.transform.position : Vector3.zero; }
        }

        /// <summary>圆钮**实绘**的 px 尺寸。</summary>
        public Vector2 ContinueCircleDrawnPx
        {
            get { return _circle != null
                       ? new Vector2(_circle.WorldW, _circle.WorldH) * EndPanel.PxPerUnit
                       : Vector2.zero; }
        }

        /// <summary>点在我方牌堆那一块没有（**入口是我们挑的**，见文件头 ⑤）。</summary>
        public static bool HitDeckPile(Vector3 world)
        {
            return BattleDriver.HitMyDeckPile(world);
        }

        static Vector3 Pos(float px, float py, float z) { return EndPanel.Pos(px, py, z); }

        /// <summary>px → **世界单位**。
        /// <para>🔴 **2026-10-18：这一句就是本件缺的那一步** —— `ImageQuad.Create` 的第 4 参是
        /// **世界高度**（`Battle/ImageQuad.cs:110` 的 doc 逐字：「`worldHeight` 是**屏幕上的高度**（世界单位）」），
        /// 而本现场 **1 px = 1/108 世界单位**（`Battle/EndPanel.cs:40`
        /// `PxPerUnit = 1080f / LayoutSpace.DesignHeight`，`DesignHeight = 10`；同一份换算也写在
        /// `Core/LayoutSpace.cs:148`：「1 px = 1/108 世界单位」）。
        /// ⇒ 把 px 原值直接喂进那个形参 = 画出来 **108 倍**大（曾经把整屏铺满）。</para>
        /// <para>兄弟件逐字同一条（**照它们写的**）：`Battle/MulliganPanel.cs:194` `U(px) = px / 108` ·
        /// `Battle/CardChoicePanel.cs:102` `U(px) = px * LayoutSpace.DesignHeight / 1080f`（同一个 108）·
        /// `Battle/EndPanel.cs:55`（私有）`U(px) = px / PxPerUnit`。</para></summary>
        static float U(float px) { return px / EndPanel.PxPerUnit; }

        static void Kill(GameObject o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }
    }
}
