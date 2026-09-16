// SkillPanel.cs — 原版的「主动技能卡面板」（`ActiveSkillDesc`）
//
// 原版选完**主动技能**之后会弹一张卡，写清楚这个技能是什么、还能选几个目标。
// 我们以前把技能效果塞在选择器上方那行小字里（`AttackSelector` 的 `note`），现在按原版搬到这里。
//
// 出处：解包场景 `07_场景/battlearena1/`（**UI/3D 的东西不在 UI dump 里**）。
//
// ⚠️ **位置别照着 anchor 写。** 原版 RT 2911 的 anchor 是 x 0.6656–0.9658 / y 0–0.304，
//    看着在「右下」，但 `m_AnchoredPosition = (-606.11, 55.0)` 把它**拉回了水平正中**：
//      pivot 世界位 = (anchorMin + Δanchor×pivot) × 父尺寸 + anchoredPosition
//                   = (0.8156827×1920, 0.1520065×1080) + (-606.11, 55.0) = (960.0, 219.17)
//      尺寸        = (0.300212×1920, 0.304013×1080) + sizeDelta(-0.00037, -3.51) = 576.41 × 324.82
//    → 屏幕矩形 x **671.8–1248.2**、y **56.8–381.6**（1920×1080），即**底部居中**、不是右下。
//    交接文档第七节原来写的「右下 30%×30%」是错的（只看了一个字段）。
//
// 换算链（每一步都是 ×1.0，已逐级核过）：CanvasScaler `ScaleWithScreenSize` +
// `MatchWidthOrHeight=0` + 参考分辨率 1920×1080 → `scaleFactor = 屏宽/1920`；
// Canvas / Safe area FrontCanvas / FrontCanvas / BattleHud / BattlePrefab / ActiveSkillDesc
// 各级 `m_LocalScale` 全 1 → **1920 下 1 UGUI 单位 = 1 px**。
//
// 其它从原件量到的：
//   · `fadeTime = 0.2`，**纯代码插值、没有 Animator**（走 `CardTween` 的推进模式，
//     批处理自检里 `Step()` 才推得动）。`FadeOut` 用的是 `fadeTime × 一个解析不出的常数`
//   · 四个文本（原版默认值 / 字号）：`NameText`(`"Fire Arrow"`/42.35)、`CostText`(`"10"`/52.4)、
//     `DescText`(37.35)、`TargetsAvailableText`(`"0 available"`/56.45)。
//     `DescText` **左对齐**（`m_margin.x = 7.4727 px`），其余居中。
//     四个都开着自动缩放 `[4,60]` —— **序列化字号是编辑器烘的「上次结果」**，
//     运行时会按各自矩形重算。这里用「同款矩形 + 装不下就回缩」来逼近那个行为（见 `Fit`）
//   · `Lights` = **三层全铺的色片**（三个子节点 anchor 0,0–1,1 完全重叠），不是一排小灯
//   · `HideAbilityButton` = 一块**远大于面板的透明点击捕获区**（`m_Color.a = 0`、
//     `m_Type = Sliced`、`RaycastTarget = 1`），点它 → 面板收起。
//     我们这边等价的行为是「点面板外的任意地方 → `ClearSelection()` → 面板收起」
//
// ⚠️ **一处是我们挑的、不是原版的做法**：**底板**。
//    原版 `AbilityContainer` 上那个 Image **没有 sprite、颜色是纯白** ——
//    照搬会渲成一块白底压着白字（多半靠运行时别的机制压暗），所以这里用深色板。
using DG.Tweening;
using RuleEngine;
using UnityEngine;

namespace CardPresentation
{
    public class SkillPanel : MonoBehaviour
    {
        // ---- 面板在屏幕上的位置（归一化，y 从**底部**算，和 `LayoutSpace.ToWorld` 一致）----
        // 671.8 / 1248.2 ÷ 1920、56.8 / 381.6 ÷ 1080（算式见文件头）
        const float PanelX0 = 0.3499f, PanelX1 = 0.6501f;
        const float PanelY0 = 0.0526f, PanelY1 = 0.3533f;
        /// <summary>原版面板的像素尺寸（1920×1080 下）—— 子节点矩形是按它折算的</summary>
        const float PanelPxW = 576.41f, PanelPxH = 324.82f;

        /// <summary>面板所在的 z —— 越负越靠前。压在 HUD（z=0）前面、选择器（-2.0）后面</summary>
        const float ZPanel = -1.80f;
        /// <summary>面板内的 z 分层（相对面板）。色片要**盖在字上面** —— 原版就是这么叠的</summary>
        const float ZBg = 0.02f, ZText = 0f, ZLight = -0.02f;

        /// <summary>本工程 1 世界单位 = 108 px —— 和 `AttackSelector` / `TargetReticle` 同口径</summary>
        const float PxPerUnit = 108f;
        static float WorldOfPx(float px) { return px / PxPerUnit; }

        // ---- 各子节点的矩形（**面板内**归一化：x 从左 0 到右 1，y 从**顶部** 0 到底部 1）----
        // 出处：`07_场景/battlearena1/RectTransform/`，按「pivot 世界位 = (anchorMin + Δanchor×pivot)
        // × 父尺寸 + anchoredPosition」逐级折算到面板坐标。7 个节点的 pivot 全是 (0.5,0.5)、
        // localScale 全是 1。
        // ⚠️ 底板**不是铺满面板的** —— 它只占 84.4% × 68.1%，偏左上。
        static readonly Rect BgRect = new Rect(0.0687f, 0.2356f, 0.8439f, 0.6813f);
        static readonly Rect NameRect = new Rect(0.1004f, 0.2806f, 0.3295f, 0.2059f);
        static readonly Rect CostIconRect = new Rect(0.7409f, 0.0829f, 0.0837f, 0.3057f);
        static readonly Rect CostTextRect = new Rect(0.8385f, 0.0812f, 0.1155f, 0.3074f);
        static readonly Rect DescRect = new Rect(0.1009f, 0.5004f, 0.7140f, 0.3335f);
        static readonly Rect TargetsRect = new Rect(0.1580f, 0.0710f, 0.6865f, 0.1646f);
        /// <summary>原版 `Lights` 那三层色片铺的范围（anchor x 0.0308–0.9630 / y 0–0.8468，y 从底部 → 折成从顶部）</summary>
        static readonly Rect LightRect = new Rect(0.0308f, 0.1532f, 0.9322f, 0.8468f);

        /// <summary>原版 `DescText` 的 `m_margin.x = 7.4727 px` —— 左对齐的起笔位置</summary>
        const float DescMarginPx = 7.4727f;

        // 四个字号是 **em**（UGUI 的 fontSize 就是 em 大小），而我们的文案是**中文** ——
        // 一个汉字约占 1 em。所以按「汉字多高」定档（`Label.SetGlyphHeight`），
        // **不要**套 `SetCapHeight`（那是拉丁大写，只有约 0.72 em，会让字大 ~39%）。
        const float NamePx = 42.35f, CostPx = 52.4f, DescPx = 37.35f, TargetsPx = 56.45f;
        // 原版四个字的字体：Name/Cost/Desc 是 Asar-Regular SDF（PointSize 94 / CapLine 61），
        // Targets 是 Pragati-Regular SDF（95 / 60）。我们统一用中文那份 —— 反正要出汉字。

        /// <summary>原版 `fadeTime`</summary>
        public const float FadeTime = 0.2f;

        /// <summary>面板底色。原版那块 Image 是**纯白无 sprite**（见文件头）—— **这块深色板是我们挑的**</summary>
        static readonly Color BgColor = new Color(0.05f, 0.06f, 0.09f, 0.86f);
        static readonly Color NameColor = new Color(1.00f, 0.96f, 0.86f);
        static readonly Color CostColor = new Color(1.00f, 0.94f, 0.72f);
        static readonly Color DescColor = new Color(0.90f, 0.92f, 0.96f);
        static readonly Color TargetsColor = new Color(0.96f, 0.90f, 0.60f);

        // ---- 原版 `Lights`：三层**全铺**色片，靠开关切换（不是一排小灯）----
        // 颜色原样照抄，alpha 0.392 也是原版的。
        // ⚠️ 交接文档之前把颜色写反了 —— 原件里 `LightActing` 是白、`LightAvailable` 才是黄绿。
        static readonly Color LightActing = new Color(1.000f, 1.000f, 1.000f, 0.392f);
        static readonly Color LightAvailable = new Color(0.925f, 1.000f, 0.000f, 0.392f);
        static readonly Color LightPressed = new Color(0.006f, 0.000f, 1.000f, 0.392f);

        /// <summary>
        /// 面板上铺哪一层色片。三态的触发条件是**从 IL2CPP dump 里查出来的**：
        ///   · **蓝 `LightPressed`** —— 指针按在面板上（`ClickDown` 点亮、`PointerExit`/`Up` 熄灭）**确定**
        ///   · **白 `LightActing`** —— 技能**正在结算**（`TryUsingActiveAbility` 成功后点亮）**确定**
        ///   · **黄绿 `LightAvailable`** —— 可用性指示（`DisableUsableLight()` 把它瞬间淡到 0）。
        ///     这一条是**从函数名推的**，原码里没有一行显式把它设成 1
        /// </summary>
        public enum Light { None, Available, Pressed, Acting }

        // ==================================================================

        ImageQuad _bg, _light, _manaIcon;
        Label _name, _cost, _desc, _targets;
        Texture2D _solid;
        bool _built;

        /// <summary>面板现在开着吗</summary>
        public bool Visible { get; private set; }
        /// <summary>正在显示哪个技能（自检断言用）。没开着是 null</summary>
        public string ShownName { get; private set; }
        /// <summary>现在显示的可选目标数（自检断言用）</summary>
        public int ShownTargets { get; private set; }
        /// <summary>现在铺的是哪层色片（自检断言用）</summary>
        public Light CurrentLight { get; private set; }
        /// <summary>现在淡到多少（0~1，自检断言用）</summary>
        public float Alpha { get; private set; }

        bool _pressed, _acting;
        Tween _fade;

        void Awake() { Build(); }

        public void Build()
        {
            if (_built) return;
            _built = true;
            CardArt.Load();

            _bg = ImageQuad.Create(transform, Solid(), Vector3.zero, 1f, new Vector2(0.5f, 0.5f), "AbilityContainer");
            _light = ImageQuad.Create(transform, Solid(), Vector3.zero, 1f, new Vector2(0.5f, 0.5f), "Lights");

            _name = MakeLabel("NameText", NamePx, NameColor);
            _cost = MakeLabel("CostText", CostPx, CostColor);
            // 原版 `DescText` 是**左对齐**（H=1）+ `m_margin.x = 7.4727` → 锚点在左边
            _desc = MakeLabel("DescText", DescPx, DescColor, new Vector2(0f, 0.5f));
            _targets = MakeLabel("TargetsAvailableText", TargetsPx, TargetsColor);

            // 原版那格的图标是 `manaIcon`（原件里 `costSprite` 是 null，运行时才赋）——
            // 用 HUD 那颗能量宝石那张图占位，方图居中放进它的矩形
            _manaIcon = ImageQuad.Create(transform, CardArt.Ui("UI_Energy_Eldar"), Vector3.zero, 0.2f,
                                         new Vector2(0.5f, 0.5f), "manaIcon");

            HideImmediate();
            RefreshLayout();
        }

        Label MakeLabel(string name, float px, Color c, Vector2? anchor = null)
        {
            var l = Label.Create(transform, "", Vector3.zero, 1, c,
                                 anchor ?? new Vector2(0.5f, 0.5f), name);
            l.SetGlyphHeight(WorldOfPx(px));
            return l;
        }

        /// <summary>1×1 纯色图。底板和色片都是纯色块 —— 原版那几个 sprite 本来就全是 null</summary>
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

        static Color WithAlpha(Color c, float a) { c.a *= a; return c; }

        float PanelW { get { return LayoutSpace.VisibleWidth * (PanelX1 - PanelX0); } }
        float PanelH { get { return LayoutSpace.VisibleHeight * (PanelY1 - PanelY0); } }

        /// <summary>面板内归一化坐标（x 从左 0 到右 1，y 从**顶部** 0 到底部 1）→ 局部坐标</summary>
        Vector3 InPanel(float x01, float y01, float z)
        {
            return new Vector3((x01 - 0.5f) * PanelW, (0.5f - y01) * PanelH, z);
        }

        Vector3 CenterOf(Rect r, float z)
        {
            return InPanel(r.x + r.width * 0.5f, r.y + r.height * 0.5f, z);
        }

        /// <summary>
        /// 按当前分辨率摆一遍。面板是**屏幕的固定比例**（30% 宽 × 30% 高），宽高比跟着屏幕走 ——
        /// 只摆一次的话切分辨率就错位（HUD 那几个 Label 踩过一模一样的坑）。
        /// </summary>
        public void RefreshLayout()
        {
            if (!_built) return;

            var c = LayoutSpace.ToWorld((PanelX0 + PanelX1) * 0.5f, (PanelY0 + PanelY1) * 0.5f);
            transform.localPosition = new Vector3(c.x, c.y, ZPanel);

            PlaceQuad(_bg, BgRect, ZBg);
            PlaceQuad(_light, LightRect, ZLight);

            // 卡名 / 费用：居中放进各自的矩形
            Layout(_name, CenterOf(NameRect, ZText));
            Layout(_cost, CenterOf(CostTextRect, ZText));
            Layout(_targets, CenterOf(TargetsRect, ZText));

            // 说明：**左对齐**，起笔位置 = 矩形左边 + 原版那个 margin
            Layout(_desc, InPanel(DescRect.x + DescMarginPx / PanelPxW,
                                  DescRect.y + DescRect.height * 0.5f, ZText));

            // ⚠️ 原版四个字都开着 `m_enableAutoSizing`（min 4 / max 60），序列化字号是
            //    **编辑器烘进去的「上次结果」**。这里用「装不进矩形就整体回缩」逼近它 ——
            //    像 `TargetsAvailableText`（56.45 em）塞进 53.5 px 高的矩形，原版一定是缩过的。
            Fit(_name, W01(NameRect.width), H01(NameRect.height));
            Fit(_cost, W01(CostTextRect.width), H01(CostTextRect.height));
            Fit(_targets, W01(TargetsRect.width), H01(TargetsRect.height));
            Fit(_desc, W01(DescRect.width) - WorldOfPx(DescMarginPx), H01(DescRect.height));

            if (_manaIcon != null)
            {
                // 图标方图居中放进它的矩形（原版 `m_PreserveAspect = 1`）
                float side = Mathf.Min(W01(CostIconRect.width), H01(CostIconRect.height));
                _manaIcon.SetWorldHeight(side);
                _manaIcon.transform.localPosition = CenterOf(CostIconRect, ZText);
            }

            ApplyColors();
        }

        float W01(float x01) { return x01 * PanelW; }
        float H01(float y01) { return y01 * PanelH; }

        /// <summary>把一块纯色 quad 摆进面板内那个矩形。
        /// `ImageQuad` 是按「高 + 图的比例」定尺寸的（1×1 的图出来是正方形），
        /// 所以横向再拉一下才成矩形（原版那几块也都是非等比的 Image）</summary>
        void PlaceQuad(ImageQuad q, Rect r, float z)
        {
            if (q == null) return;
            q.SetWorldHeight(H01(r.height));
            q.transform.localPosition = CenterOf(r, z);
            q.transform.localScale = new Vector3(W01(r.width) / Mathf.Max(1e-4f, q.WorldW), 1f, 1f);
        }

        static void Layout(Label l, Vector3 p) { if (l != null) l.transform.localPosition = p; }

        /// <summary>
        /// 装不进矩形就整体回缩 —— 逼原版那个 `enableAutoSizing`。
        /// 字号正比于汉字高度，宽度也正比，所以「缩多少比例」能直接作用回去。
        /// </summary>
        /// <summary>
        /// 装不进矩形就整体回缩 —— 逼原版那个 `enableAutoSizing`。
        /// 字号正比于汉字高度，宽度也正比，所以「缩多少比例」能直接作用回去。
        ///
        /// ⚠️ **空文本必须跳过**：TMP 在字符串为空时 `textBounds` 会返回 **`4.2949673E+09`**
        ///    （`uint.MaxValue`，它内部当「无穷大」用的哨兵）。拿它当真实宽度算比例，
        ///    字号会被缩掉 1e-10 倍 —— 表现就是「面板上一个字都看不见」。
        /// ⚠️ 再加一道保险：缩得比 5% 还狠说明量出来的数不对，**宁可不缩**也别缩成零。
        /// </summary>
        static void Fit(Label l, float maxW, float maxH)
        {
            if (l == null || string.IsNullOrEmpty(l.Text)) return;
            float w = l.WorldW, h = l.WorldH;
            if (!(w > 1e-4f) || !(h > 1e-4f) || !(maxW > 1e-4f) || !(maxH > 1e-4f)) return;
            float k = Mathf.Min(1f, Mathf.Min(maxW / w, maxH / h));
            if (k >= 1f || k < 0.05f) return;
            l.SetGlyphHeight(l.GlyphHeightWorld * k);
        }

        /// <summary>面板在世界坐标里占的那块矩形（指针在不在面板上用它判）</summary>
        public bool Contains(Vector3 world)
        {
            var l = transform.InverseTransformPoint(world);
            return Mathf.Abs(l.x) <= PanelW * 0.5f && Mathf.Abs(l.y) <= PanelH * 0.5f;
        }

        // ==================================================================
        //  显示
        // ==================================================================

        /// <summary>
        /// 弹出来。`cardName` 当技能名（原版有独立的技能名 `"Fire Arrow"`，我们还没有那个字段）、
        /// `ability` 是效果、`targets` 是**现在有几个合法目标** —— 由 `BattleDriver` 数好传进来，
        /// 面板不自己判规则。
        /// </summary>
        public void Show(string cardName, EffectSpec ability, int targets)
        {
            Build();
            ShownName = cardName;
            ShownTargets = targets;
            _acting = false;

            if (_name != null) _name.SetText(cardName);
            if (_desc != null) _desc.SetText(CardText.EffectSentence(ability));
            if (_targets != null) _targets.SetText(CardText.TargetsAvailable(targets));
            // 我们的技能**不吃能量、吃一次行动** —— 原版这一格是能量费。照实填 0，
            // 不把行动费伪装成能量费（以后技能真收能量了，这格已经接好）
            if (_cost != null) _cost.SetText("0");

            Visible = true;
            RefreshLayout();
            FadeTo(1f);
        }

        /// <summary>合法目标数变了就更新那一行和色片（判据在 `BattleDriver`，这儿只显示）</summary>
        public void SetTargetsAvailable(int n)
        {
            ShownTargets = n;
            if (_targets != null) _targets.SetText(CardText.TargetsAvailable(n));
            ApplyColors();
        }

        /// <summary>指针喂进来（`down` = 这一帧按着）—— 决定要不要铺蓝色那层</summary>
        public void SetPointer(Vector3 world, bool down)
        {
            bool p = Visible && down && Contains(world);
            if (p == _pressed) return;
            _pressed = p;
            ApplyColors();
        }

        /// <summary>技能开始结算 → 铺白那层（原版 `ShowActingLight()`）</summary>
        public void SetActing()
        {
            _acting = true;
            ApplyColors();
        }

        /// <summary>
        /// 三态归一到「现在铺哪一层」。**判据只有这一份** —— 别在调用处各自决定。
        /// 优先级照原版：结算 &gt; 按下 &gt; 可用。
        /// </summary>
        void ApplyColors()
        {
            CurrentLight = _acting ? Light.Acting
                         : _pressed ? Light.Pressed
                         : (ShownTargets > 0 ? Light.Available : Light.None);

            if (_light != null)
            {
                Color c;
                switch (CurrentLight)
                {
                    case Light.Acting: c = LightActing; break;
                    case Light.Pressed: c = LightPressed; break;
                    case Light.Available: c = LightAvailable; break;
                    default: c = new Color(0, 0, 0, 0); break;
                }
                _light.SetTint(WithAlpha(c, Alpha));
            }

            if (_bg != null) _bg.SetTint(WithAlpha(BgColor, Alpha));
            if (_name != null) _name.SetColor(WithAlpha(NameColor, Alpha));
            if (_cost != null) _cost.SetColor(WithAlpha(CostColor, Alpha));
            if (_desc != null) _desc.SetColor(WithAlpha(DescColor, Alpha));
            if (_targets != null) _targets.SetColor(WithAlpha(TargetsColor, Alpha));
            if (_manaIcon != null) _manaIcon.SetTint(WithAlpha(Color.white, Alpha));
        }

        /// <summary>收起（带淡出）</summary>
        public void Hide()
        {
            if (!Visible && Alpha <= 0f) { HideImmediate(); return; }
            Visible = false;
            ShownName = null;
            FadeTo(0f);
        }

        /// <summary>
        /// 收起（不带淡出）。
        /// ⚠️ 显隐**只靠 alpha，不碰 `SetActive`** —— 踩过：一开始用 `SetActive(false)` 收起来，
        ///    结果 `Show()` 里在**还没激活**的对象上调 `SetText`，而 TMP 在未激活的 GameObject 上
        ///    `ForceMeshUpdate()` **生成不出网格**，`textBounds` 全是 0，
        ///    面板上的字全成零尺寸、一个字都看不见。
        ///    面板就这么点东西，一直开着只花几个全透明 quad，不值得为它冒这个险。
        /// </summary>
        void HideImmediate()
        {
            Visible = false;
            ShownName = null;
            ShownTargets = 0;
            _pressed = _acting = false;
            CurrentLight = Light.None;
            if (_fade != null) { _fade.Kill(); _fade = null; }
            Alpha = 0f;
            ApplyColors();
        }

        /// <summary>淡到某个 alpha。**不切 `SetActive`** —— 理由见 `HideImmediate`</summary>
        void FadeTo(float target)
        {
            if (!_built) return;
            if (_fade != null) _fade.Kill();
            _fade = DOTween.To(() => Alpha, SetAlpha, target, FadeTime)
                           .SetUpdate(CardTween.Mode)
                           // 🔴 **绑生命周期**（2026-09-16 加）：面板被销毁时还在飞的淡入淡出
                           //    没人杀的话，DOTween 安全模式会每帧记一条
                           //    `Target or field is missing/null`（真包实测 22 条，编辑器看不见）。
                           //    全工程同一条规矩 —— 见 `CardTween.Use` 的注释。
                           .SetLink(gameObject);
        }

        void SetAlpha(float a)
        {
            Alpha = a;
            ApplyColors();
        }

        /// <summary>
        /// 自检用：面板各元素现在摆在哪、多大、有没有激活。
        /// **截图看不出「字是不是根本没生成」**，所以把数报出来。
        /// </summary>
        public string Describe()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("面板 世界中心 ").Append(transform.position.ToString("F2"))
              .Append("　尺寸 ").Append(PanelW.ToString("F2")).Append("×").Append(PanelH.ToString("F2"))
              .Append("　alpha ").Append(Alpha.ToString("F2"))
              .Append("　灯 ").Append(CurrentLight);
            sb.Append("　name[").Append(One(_name)).Append("]");
            sb.Append("　cost[").Append(One(_cost)).Append("]");
            sb.Append("　desc[").Append(One(_desc)).Append("]");
            sb.Append("　targets[").Append(One(_targets)).Append("]");
            return sb.ToString();
        }

        static string One(Label l)
        {
            if (l == null) return "无";
            return "「" + l.Text + "」pos=" + l.transform.position.ToString("F2")
                 + " 尺寸=" + l.WorldW.ToString("F2") + "×" + l.WorldH.ToString("F2")
                 + " [" + l.DumpSizes() + "]"
                 + " active=" + l.gameObject.activeInHierarchy + "/" + l.gameObject.activeSelf;
        }
    }
}
