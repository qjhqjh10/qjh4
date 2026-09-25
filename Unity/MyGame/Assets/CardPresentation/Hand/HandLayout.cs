// HandLayout.cs — 手牌扇形布局（原版 `CardsHorizontalLayout` 的 2D 移植）
//
// **数值不是拍的，是从原版序列化数据里搬的**，字段名和原版一一对应，方便回头对账。
//   类定义：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardsHorizontalLayout.cs`
//   权威实例：`d:/2/解包整理/07_场景/battlearena1/MonoBehaviour/MonoBehaviour_5271.json`
//
// ⚠️ 这个场景里有**多份** CardsHorizontalLayout 实例，别拿错：
//     · MB 5271（挂在 GO 1192）—— **我方手牌**。m_scale=0.73 → 卡正好 165×263 px；
//       m_verticalOffsetLookTo=-330（负的，和 Tooltip "Should be negative" 对得上）；
//       m_betweenElementsSpacing=1.45 → 0.95 卡宽（牌几乎挨着，不重叠）。
//     · MB 4053（挂在 GO 58）—— **敌方手牌**（宿主 `PlayerHand` MB 4350，isPlayer=0）。
//       `m_verticalOffsetLookTo=+74.5`（正值）、`m_invertRotation=1`、`m_maxHeight=-0.78`（**负的**）、
//       `m_scale=0.54`、`m_betweenElementsSpacing=0.6` —— 这几个加起来正是
//       「**敌方那排牌从屏幕上方垂下来、中间往下凹**」。另有一份 4052=选牌面板 · 4881=换牌面板。
//
// 🔴 **2026-09-17 更正**：这里原来写「4053 是**另一套预设**、`资料/对战排版_原版数值与改造方案.md`
//   引的是这一份、**它的单位自相矛盾，别用**」—— **那条是错的，而且方向反了**：
//   ① 4053 **不是「另一套预设」，是敌方手牌**（判据：宿主 PlayerHand 4350 的 `isPlayer: 0`，
//      父链 `UpperAnchor/EnemyArea/HandArea`）；这与 `CLAUDE.md` 铁律 4 的 2026-09-14 更正是同一条。
//   ② 它的单位**不自相矛盾**：`m_maxHeight = -0.78` 是负的，乘在同样「两端低中间高」的 `yAxisCurve` 上
//      ⇒ 弧高变负 ⇒ **中间往下凹**，与 `invertRotation=1`、`lookTo=+74.5` 完全自洽。
//   ⇒ 所以这一份**要照用**（敌方手牌就靠它），下面 `ConfigureForEnemy()` 把它的 7 个字段整套搬过来了。
//
// 单位换算（原版 3D → 我这套 2D 归一化坐标）：
//   原版 UI 相机 fov40 / 平面 100 → **14.835 px / 世界单位**；手牌所在的深度是 108 px/单位
//   （165 px 的卡 = 2DCard 宽 2.0927 × scale 0.73 × 108）。我这套是「可见高恒 10 单位」，
//   1080p 下正好也是 108 px/单位 —— **所以手牌这一块的像素数可以 1:1 搬过来**。
//   棋盘那一层深度不同（k=182.14），是另一套换算，见 `BoardLayout`。
//
// 🆕 **2026-09-25：原版 `GetPosition` / `GetRotation` 逐句解出**，正本 = `资料/手牌布局_原版算法与参数.md`。
//   这一版照着它收了三处（之前是自造或近似）：
//     · **选中一张牌时两侧让位** = 原版 `VarsDevice.extraSpaceOnSelectedCard`（桌面恒 2.0 世界单位 = 29.7 px），
//       且**装不下时不让**（`IsCompressed`）—— 取代原来自造的 `neighborShift`；
//     · **悬停抬起/放大** = 原版 `cardInHandShownYOffset` 30 px · `cardInHandShownScale` 1.3
//       （原来自造 0.06 世界单位 = 6.5 px、不放大）；
//     · **张角分母补上弧高项** —— 原版 `dir.y` 是「卡相对基准的 y 偏移」不是常数。
//   🔴 **同时更正一条旧结论**：原来记「间距按 108 px/单位、弧高按 14.835 ⇒ 差 7.28 倍是矛盾」——
//   **不是矛盾**：原版 x 乘父级 `lossyScale`（`HandArea` 108 × Canvas(1/14.835) = **7.28**）、y 不乘，
//   两个桥本来就该差 7.28 倍。父链实读：`HandAnchor→HandArea(×108)→PlayerArea→BottomAnchor→
//   Safe area BackCanvas→BackCanvas→Canvas(ScreenSpaceCamera, planeDistance 100, fov 40)`。
//   ⚠️ 仍**属于我们的**（不是原版的做法）：`zOrderStep`（原版 0.001 ≈ 0.015 px，它靠 canvas 排序；我们是世界空间 2D 面）。
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace CardPresentation
{
    public class HandLayout : MonoBehaviour
    {
        // ==================================================================
        //  原版字段（名字一一对应）
        // ==================================================================

        /// <summary>原版手牌卡宽度（px @1920×1080）= `2DCard 2.0927 × m_scale 0.73 × 108`。</summary>
        public const float OriginalCardWidthPx = 165f;

        /// <summary>手牌那张卡的缩放 = 原版卡宽 ÷ (`2DCard` 宽 × 我们 108 px/单位) = **0.730**。
        /// 🔴 **判据只此一处**：`BattleScene` 引用的就是它，**别再各写一份**。
        /// ⚠️ **2026-09-17 更正**：这里的默认值原来是 **1.054**，那是拿**旧的** `CardView.Width = 1.45`
        /// 算出来的（1.528 ÷ 1.45）—— 卡宽后来改成 2.0927（= 165 px 那个桥），这个默认值就成了**死值**。
        /// 与 `BoardLayout.placedScale` 是**同一个形状**：产品侧（Battle 场景）被 `BattleScene` 显式
        /// 赋了正确值 ⇒ 一直没露馅；**中招的仍是「取默认值」那条路**（`CardBaseDemo` 自建的场），
        /// 它手牌卡会画成 238 px 而不是 165 px。</summary>
        public const float DefaultCardScale = OriginalCardWidthPx / (CardView.Width * 108f);

        /// <summary>我方手牌中心行的 y（归一化，从**下**算）—— **0.1204**，
        /// 让卡底（= 中心 − 卡半高 131.35 px）正好压在屏幕下沿上（原版 `HandAnchor` 链 961/1080）。
        /// 🔴 **判据只此一处**：`BattleScene` 引用它。
        /// ⚠️ **2026-09-17 更正**：默认值原来是 **0.140** —— 这是**同一个形状的第三处**
        /// （前两处是 `BoardLayout.placedScale 0.876` 与本文的 `cardScale 1.054`）：
        /// 产品侧被 `BattleScene` 显式赋了正确值 ⇒ 一直没露馅，**只有取默认值的那条路会画错**
        /// （`CardBaseDemo` 的手牌会整体高 21.6 px）。三处一起收成「默认值必须由常量算出来」。</summary>
        public const float DefaultBaselineY = 0.1204f;

        /// <summary>敌方手牌卡缩放 = 原版 `m_scale 0.54` ⇒ 卡宽 2.0927 × 0.54 × 108 ≈ **122 px**
        /// （我方是 165 px，同一套 2DCard 预制体，只是 `m_scale` 不同）。</summary>
        public const float EnemyCardScale = 0.54f;

        /// <summary>🔴 敌方手牌中心行的 y（归一化，从**下**算）= **0.9102**。
        ///
        /// **怎么推的**：原版敌方 `HandAnchor` 落在**屏幕顶边**（距顶 0.32 px，ny ≈ 0.9997 ——
        /// 出处 `Transform_1257.json` 的 y=0.46 × `Transform_1295.json` 的 scale=108 = 49.68 px，
        /// 锚点链见 `资料/敌方手牌_原版规格.md` §三），而敌方卡区只有**顶部 ~97 px 厚**
        /// （`EnemyCardAreaSizeHelperData` 的 `sizeDelta (100, 96.7)` 挂顶边；我方对应件是 249.9，
        /// ≈ 整张卡高）。⇒ **卡顶贴屏幕上沿**：卡高 194.3 px（宽 122.05 × 高宽比 1.5918），
        /// 半高 97.15 px = 0.08995 ⇒ 中心 = 1 − 0.08995 = **0.9102**。
        ///
        /// ⚠️ **这一段是推导，不是原版字段**（原版那两个节点是纯 `Transform`、祖先 canvas 的
        /// `localScale` 序列化成 0 ⇒ **绝对像素位从序列化里读不出来**）。链条见上面；**已看截图核对**。</summary>
        public const float EnemyBaselineY = 0.9102f;

        [Tooltip("m_maxLayoutSize = 0.59（16:9 档）：首卡中心↔末卡中心的**上限**，"
               + "单位是「占可见宽度的比例」。超过它就把间距压下来")]
        public float maxLayoutSize = 0.59f;

        [Tooltip("m_maxLayoutSizeSmallScreen = 0.51：窄屏（可见宽 < 设计宽）时用这个")]
        public float maxLayoutSizeSmallScreen = 0.51f;

        [Tooltip("maxLayoutSizeAspectRatioModifier：(1.70,0)→(2.33,0.1013)，"
               + "宽屏额外给一点余量。16:9 时 ≈ +0.0126（→ 0.6025）")]
        public AnimationCurve aspectRatioModifier = new AnimationCurve(
            new Keyframe(1.70f, 0.000000f, 0.160748f, 0.160748f),
            new Keyframe(2.33f, 0.101271f, 0.160748f, 0.160748f));

        [Tooltip("m_betweenElementsSpacing = 1.45：相邻两张的**中心间距**（Tooltip 原文 "
               + "\"The spacing is between centers!\"），单位=世界单位（×108 px）。"
               + "手牌卡宽 1.528 → 间距 0.95 卡宽：牌是挨着排的，几乎不叠")]
        public float betweenElementsSpacing = 1.45f;

        [Tooltip("m_numberOfCardsForMaxHeight = 12：到这个张数，弧高/张角都拉满")]
        public int numberOfCardsForMaxHeight = 12;

        [Tooltip("m_yAxisCurve：弧线形状（原版 5 关键帧，**原始值**，单位=世界单位。"
               + "形状不是抛物线：头 8% 就升到 2/3 高，中段是平台，两端陡降）")]
        public AnimationCurve yAxisCurve = new AnimationCurve(
            new Keyframe(0.069202f, -3.019253f, 12.541409f, 12.541409f),
            new Keyframe(0.085211f, -1.886966f, 10.778070f, 10.778070f),
            new Keyframe(0.500285f, -0.000360f, 0.004107f, 0.004107f),
            new Keyframe(0.892280f, -1.744377f, -8.074480f, -8.074480f),
            new Keyframe(0.985224f, -3.042531f, -21.663622f, -21.663622f));

        [Tooltip("m_maxHeight = 0.70：弧高上限（乘在 yAxisCurve 上）")]
        public float maxHeight = 0.70f;

        [Tooltip("m_heightModifierBasedOnTotalCards：牌少的时候弧高按比例缩小"
               + "（x=0 是 1 张，x=1 是 numberOfCardsForMaxHeight 张）")]
        public AnimationCurve heightModifierByCount = new AnimationCurve(
            new Keyframe(0.000000f, 0.002358f, 0.464329f, 0.464329f),
            new Keyframe(0.207319f, 0.149721f, 0.943726f, 0.943726f),
            new Keyframe(0.397620f, 0.359491f, 1.210666f, 1.210666f),
            new Keyframe(0.598949f, 0.588759f, 1.169906f, 1.169906f),
            new Keyframe(1.000000f, 1.002358f, 0.977445f, 0.977445f));

        [Tooltip("m_rotationModifierBasedOnTotalCards：牌少的时候张角也按比例缩小。"
               + "原版是**按总张数**查表（不是按第几张）—— 按序号查会变成「左端不转右端转」的斜切")]
        public AnimationCurve rotationModifierByCount = new AnimationCurve(
            new Keyframe(0.000000f, 0.000000f, 3.468699f, 3.468699f),
            new Keyframe(0.263962f, 0.825967f, 1.723799f, 1.723799f),
            new Keyframe(0.496854f, 0.998978f, 0.072311f, 0.072311f),
            new Keyframe(1.000000f, 1.000000f, 0.026170f, 0.026170f));

        [Tooltip("m_verticalOffsetLookTo = -330：卡「看向」手牌下方 330 世界单位处的那个点 —— "
               + "**看向下面，卡的顶边就朝外撇**（这就是 Tooltip 说 Should be negative 的意思）。"
               + "这里换成我的世界单位（×14.835/108）")]
        public float lookToDistance = 45.33f;

        [Tooltip("m_invertRotation = 0：原版靠它把敌方那份翻过来（敌手的卡朝内撇）")]
        public bool invertRotation = false;

        [Tooltip("m_scale = 0.73 × 2DCard 宽 2.0927 = 1.528 世界单位 = 165 px。"
               + "🔴 判据收在 DefaultCardScale 一处，别手写数字")]
        public float cardScale = DefaultCardScale;

        // ==================================================================
        //  版面（原版没这几个字段，是这套 2D 布局自己的）
        // ==================================================================

        [Tooltip("手牌中心行的 y（归一化，从**下**算）—— 原版 HandAnchor 链 961/1080 ⇒ 距底 119 px"
               + "（0.110）；取 **0.1204** 让**卡底正好压在屏幕下沿上**（卡半高 131.35 px）。"
               + "两端那张卡在 baselineY（弧高在两端为 0）")]
        public float baselineY = DefaultBaselineY;

        [Tooltip("悬停时抬起多少（**世界单位**，×108 = px）—— 原版 `VarsDevice.cardInHandShownYOffset`"
               + " = **30 px**（桌面；小屏 ≤7 吋取 `…YOffsetMin` = 20）。原来我们自造 0.06（=6.5 px），2026-09-25 换成实读值")]
        public float hoverLift = HoverLiftPx / 108f;

        [Tooltip("悬停那张的缩放 —— 原版 `VarsDevice.cardInHandShownScale` = **1.3**（移动端 1.7）")]
        public float hoverScale = 1.3f;

        [Tooltip("**选中一张牌时，两侧的牌各往外让多少**（世界单位）—— 原版 `VarsDevice.extraSpaceOnSelectedCard`"
               + " **桌面 = 平线 2.0** · 移动端 = (0,2.0)→(1,5.0) 斜坡。⚠️ **装不下时不让**（牌多时压缩间距那一支会把 extra 归零）")]
        public float selectedCardExtra = SelectedExtraWorld;

        [Tooltip("悬停/拖拽的那张往前提多少 z")]
        public float frontZ = 0.5f;

        [Tooltip("原版 useZOrder=1：「Left card Z < right card Z」= **左边的压在上面**。"
               + "所以露出来的是每张牌的右半边 —— 卡面上的费用（右上）和生命（右下）正好在那儿，对得上")]
        // ⚠️ **必须大于「一张卡内部各层的 z 跨度」**（`CardView` 里 立绘 +0.03 … 文字 −0.03 = 0.06）。
        //    原来是 0.004，比跨度还小 —— 于是**后面那张卡的卡面文字会穿到前面那张卡上面**
        //    （文字层在自己的卡里最靠前，就跑到邻牌前面去了）。2026-09-12 把效果文字放大到原版字号后
        //    这个毛病变得很明显（截图里能看到「字横穿别人的卡」），所以把步长提到 0.08。
        public float zOrderStep = 0.08f;
        // 🔴 **2026-09-25 实读原版：`Z_OFFSET_BY_POSITION_INDEX` = 0.001**（`CardsHorizontalLayout..cctor`
        //    里那句 `= 0x3a83126f`，即 `.rdata` 的 0.001f；世界单位 ⇒ 只等于 0.015 px）。
        //    ⇒ **原版根本不是靠 z 分层**（0.015 px 排不出前后），它靠 Canvas 的兄弟序/排序层。
        //    我们这套是**世界空间的 2D 四边面**，没有 canvas 排序 ⇒ **用一个真 z 步长替代**。
        //    ⚠️ **这是我们挑的，不是原版的做法**（照铁律 3 标注）。
        //    约束不变：步长**必须大于「一张卡内部各层的 z 跨度」**（`CardView` 里 立绘 +0.03 … 文字 −0.03 = 0.06），
        //    否则后面那张卡的卡面文字会穿到前面那张上面（2026-09-12 实测）。

        [Tooltip("重排走补间（拖拽让位才不跳）。批处理自检里关掉 —— 位置要当场精确")]
        public bool animateRelayout = false;

        // ==================================================================
        //  敌方手牌那一份（原版 `CardsHorizontalLayout` MB **4053**）
        // ==================================================================

        /// <summary>
        /// 把自己配成**敌方手牌**。八个字段 + 三条曲线**整套从原版序列化值搬**
        /// （出处：`资料/敌方手牌_原版规格.md` §二 —— 那份也解释了「为什么它的 `maxHeight` 是负的」）。
        ///
        /// 🔴 这一份**以前被判成「另一套预设、单位自相矛盾、别用」**（见文件头 2026-09-17 更正）——
        /// 那条错误正是「敌方手牌整支没进我们清单」的源头。
        /// 实际它完全自洽：`maxHeight = -0.78`（**负的**）×「两端低中间高」的 `yAxisCurve`
        /// ⇒ 弧高变负 ⇒ **中间往下凹**，配 `invertRotation` + 正的 `lookToDistance`，
        /// 就是「敌方那排牌从屏幕**上方**垂下来」。
        /// </summary>
        public void ConfigureForEnemy()
        {
            // ---- 标量：MB 4053 的原版值 ----
            maxLayoutSize = 0.51f;                       // m_maxLayoutSize
            maxLayoutSizeSmallScreen = 0.51f;
            betweenElementsSpacing = 0.6f;               // m_betweenElementsSpacing
            numberOfCardsForMaxHeight = 10;              // m_numberOfCardsForMaxHeight
            maxHeight = -0.78f;                          // m_maxHeight（**负的**，见方法注释）
            invertRotation = true;                       // m_invertRotation = 1
            // m_verticalOffsetLookTo = +74.5（正值，我方是 −330）—— 用和 `lookToDistance` 同一个桥换算
            lookToDistance = 74.5f * OriginalPxPerUnit / 108f;      // = 10.23
            cardScale = EnemyCardScale;
            baselineY = EnemyBaselineY;

            // ---- 三条曲线：关键帧与切线都是原版序列化值，一个没改 ----
            // m_maxLayoutSizeAspectRatioModifier：4053 上是**一条 0 的平线**（我方有 (1.70,0)→(2.33,0.1013)）
            aspectRatioModifier = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 0f));

            yAxisCurve = new AnimationCurve(
                new Keyframe(-0.010000f, -3.391766f,  3.183986f,  3.183986f),
                new Keyframe( 0.132761f, -2.260551f,  5.579828f,  5.579828f),
                new Keyframe( 0.485413f,  0.010992f,  0.004107f,  0.004107f),
                new Keyframe( 0.884423f, -2.369609f, -8.206697f, -8.206697f),
                new Keyframe( 1.000000f, -3.385479f, -5.948666f, -5.948666f));

            heightModifierByCount = new AnimationCurve(
                new Keyframe(0.000000f, 0.002358f, 0.000000f, 0.000000f),
                new Keyframe(0.398679f, 0.181347f, 0.893307f, 0.893307f),
                new Keyframe(1.000000f, 1.002358f, 1.528499f, 1.528499f));

            rotationModifierByCount = new AnimationCurve(
                new Keyframe(0f, 0f, 1f, 1f),
                new Keyframe(1f, 1f, 1f, 1f));
        }

        // ==================================================================
        //  换算常量
        // ==================================================================

        /// <summary>原版 UI 相机：fov40 / 平面 100 → 14.835 px 每世界单位</summary>
        const float OriginalPxPerUnit = 14.835f;

        /// <summary>我这套：可见高 10 世界单位 = 1080 px</summary>
        const float PxPerNormY = 1f / 1080f;

    /// <summary>**1 原版世界单位 = 多少「我们的单位」** = 14.835/108 = 0.13736。
    /// 两个空间的桥：`1 设计单位（卡宽/间距那套）= 108 px`、`1 世界单位（弧高/lookTo 那套）= 14.835 px`。
    /// 🔴 两者差 7.28 倍**不是错**：原版 x 乘父级 `lossyScale`（`HandArea` 108 × Canvas 1/14.835 = **7.28**），
    /// y 不乘 —— 逐值推导见 `资料/手牌布局_原版算法与参数.md` §四。</summary>
    public const float OurUnitsPerWorldUnit = OriginalPxPerUnit / 108f;

    /// <summary>原版 `VarsDevice.cardInHandShownYOffset`（`VarsDesktop`）= **30 px**。
    /// 出处：`bundle_duplicateassetisolationso_assets_all/MonoBehaviour/VarsDesktop.json`；
    /// 消费者 `CardScript.ShowCardInHand`（`AlternateArtCard__get_CardImage` 那个符号是 Ghidra 认错，
    /// 真身是 `BattleManager.get_globalVars` → 读 `+0x18/0x1c/0x20` 三个字段）。
    /// 小屏（物理尺寸 ≤ 7 吋，`.rdata 0x1834b338c` 实读）取 `cardInHandShownYOffsetMin` = 20。</summary>
    public const float HoverLiftPx = 30f;

    /// <summary>原版 `VarsDevice.extraSpaceOnSelectedCard`（`VarsDesktop`）= **恒 2.0 世界单位**
    /// （移动端是 (0,2.0)→(1,5.0)，见 `VarsMobile.json` —— 又一个「一个值 ≠ 全部情况」）。
    /// ⇒ 换算成我们的单位 = `2.0 × 14.835/108` = **0.2747**（= **29.7 px**）。</summary>
    public const float SelectedExtraWorld = 2.0f * OurUnitsPerWorldUnit;


        // ==================================================================
        //  算
        // ==================================================================

        float _curveMin, _curveMax;
        AnimationCurve _rangeCurve;

        /// <summary>曲线的值域（原版曲线最小值在两端、最大值在中间）</summary>
        void CurveRange()
        {
            if (_rangeCurve == yAxisCurve) return;
            _rangeCurve = yAxisCurve;
            _curveMin = float.MaxValue; _curveMax = float.MinValue;
            for (int i = 0; i <= 32; i++)
            {
                float v = yAxisCurve.Evaluate(i / 32f);
                if (v < _curveMin) _curveMin = v;
                if (v > _curveMax) _curveMax = v;
            }
        }

        /// <summary>满手时的弧高（归一化高度）= |曲线最低点| × m_maxHeight × px/单位 ÷ 1080</summary>
        public float ArcHeightFull
        {
            get { CurveRange(); return -_curveMin * maxHeight * OriginalPxPerUnit * PxPerNormY; }
        }

        /// <summary>弧线在 t（0=最左，1=最右）处抬起多少 —— 归一化高度，两端 0、中间最高</summary>
        public float Arc01(float t)
        {
            CurveRange();
            float v = yAxisCurve.Evaluate(Mathf.Clamp01(t));
            return Mathf.Clamp01((v - _curveMin) / Mathf.Max(1e-6f, _curveMax - _curveMin));
        }

        /// <summary>「牌少的时候弧高也小」—— 原版 x = (张数-1)/(满张数-1)</summary>
        float CountFraction(int count)
        {
            int full = Mathf.Max(2, numberOfCardsForMaxHeight);
            return Mathf.Clamp01((count - 1) / (float)(full - 1));
        }

        /// <summary>相邻两张的中心间距（归一化宽度）—— 原版「超过 maxLayoutSize 才压缩」</summary>
        public float SpacingFor(int count)
        {
            if (count <= 1) return 0f;

            // 原版：n × spacing > maxLayout → spacing = maxLayout / n（**除 n 不是除 n-1**，
            // 这会让压缩后整把牌比上限略窄一点 —— 原版就是这样，照抄）
            float natural = betweenElementsSpacing * LayoutSpace.Scale;        // 世界
            if (IsCompressed(count)) natural = MaxLayoutWorld() / count;
            return natural / LayoutSpace.VisibleWidth;                          // 归一化
        }

        /// <summary>原版「装不下 ⇒ 压缩间距」那一支的判据：`count × 自然间距 > 上限`。
        /// ⚠️ 它**同时**是「选中一张牌时两侧要不要让位」的开关 —— 原版在压缩那一支里把 `extra` 归零。
        /// 我方手牌 n ≥ **8** 起为真（`0.6026 × 屏宽 ÷ 156.6 px`），所以「牌多时选牌不让位」。
        /// 🔴 判据与 `SpacingFor` **共用这一处**，别各写一份。</summary>
        public bool IsCompressed(int count)
        {
            if (count <= 1) return false;
            return count * betweenElementsSpacing * LayoutSpace.Scale > MaxLayoutWorld();
        }

        /// <summary>首卡中心↔末卡中心的上限（世界单位）= maxLayoutSize + 宽高比修正</summary>
        float MaxLayoutWorld()
        {
            float aspect = LayoutSpace.Cam != null ? LayoutSpace.Cam.aspect : LayoutSpace.DesignAspect;
            bool small = LayoutSpace.VisibleWidth < LayoutSpace.DesignWidth;
            float baseSize = small ? maxLayoutSizeSmallScreen : maxLayoutSize;
            return (baseSize + aspectRatioModifier.Evaluate(aspect)) * LayoutSpace.VisibleWidth;
        }

        /// <summary>第 slot 张（共 count 张）的归一化 x</summary>
        public float NxOfSlot(int slot, int count)
        {
            int n = Mathf.Max(1, count);
            return 0.5f + (slot - (n - 1) * 0.5f) * SpacingFor(n);
        }

        /// <summary>第 index 张（共 count 张）在哪 —— 位置(世界) 由这里算，旋转由 RotationAt 算</summary>
        public Vector3 SlotPosition(int index, int count)
        {
            int n = Mathf.Max(1, count);
            float t = n == 1 ? 0.5f : index / (float)(n - 1);
            float arc = ArcHeightFull * Arc01(t) * heightModifierByCount.Evaluate(CountFraction(n));
            // 弧高跟卡一样随窄屏缩放（都是「屏幕上的大小」，不是布局间距）
            arc *= LayoutSpace.Scale;
            return LayoutSpace.ToWorld(NxOfSlot(index, n), baselineY + arc);
        }

        /// <summary>
        /// 张角 —— 原版 `GetRotation(cardPosition, totalCards)`：**输入是位置不是序号**。
        /// 卡看向手牌下方 lookToDistance 处的一个点 → 顶边朝外撇（右半边的卡顺时针）。
        /// </summary>
        /// <summary>
        /// 张角 —— 原版 `GetRotation(cardPosition, totalCards)`。
        ///
        /// 🔴 **2026-09-25 更正**：原版那套三维构造（`LookRotation(dir, Vector3.back) × AngleAxis(90°, Vector3.right)`
        /// → `Lerp(from, q, rotMod)` → `invertRotation` 再乘 `AngleAxis(180°, Vector3.forward)`）
        /// **代数上恰好退化成「纯绕 Z 滚一个角」**（`dir.z ≡ 0`，因为 `lookTo` 与卡同 z）：
        ///   `LookRotation(dir, back) = R_z(−θ)·R_x(−90°)`（`tanθ = dir.x/dir.y`）
        ///   ⇒ 乘上 `R_x(+90°)` 后 `= R_z(−θ)`；再 Lerp 到 `R_z(−θ·mod)`；敌方再 ×`R_z(180°)`。
        /// ⇒ **所以二维 `atan2` 不是「近似」，是同一件事**（此前把它记成「二维近似」是错的）。
        /// **唯一漏掉的是分母里的弧高项**（原版 `dir.y` 是「卡相对基准的 y 偏移」，不是常数），本版补上。
        ///
        /// 分母：**我方** `|lookTo| + 弧高`（`lookTo = −330` ⇒ `dir.y = 弧高 + 330`）；
        /// **敌方** `|lookTo| − 弧高`（`lookTo = +74.5` ⇒ `180° − atan2(dx, 弧高−74.5) = atan2(dx, 74.5−弧高)`）。
        /// </summary>
        public float RotationAt(int index, int count)
        {
            int n = Mathf.Max(1, count);
            if (n <= 1) return 0f;

            float t = index / (float)(n - 1);
            float dx = (NxOfSlot(index, n) - 0.5f) * LayoutSpace.VisibleWidth;   // 我们的单位
            float look = Mathf.Max(1f, lookToDistance * LayoutSpace.Scale);
            float arc = ArcOffsetFromBase(t, n);                                  // 原版口径（≤0 我 / ≥0 敌）
            float denom = Mathf.Max(1f, look + (invertRotation ? -arc : arc));
            float mod = rotationModifierByCount.Evaluate(CountFraction(n));
            float deg = -Mathf.Atan2(dx, denom) * Mathf.Rad2Deg * mod;
            return invertRotation ? -deg : deg;
        }

        /// <summary>原版口径的弧高偏移（**相对基准行**，我们的单位）——
        /// = `yAxisCurve(t) × m_maxHeight × 按张数修正`，**不归一化**（原版就是拿原始值直接当 y 偏移）。
        /// 我方 ≤ 0（中间=基准、两端下垂）；敌方 = 负 maxHeight × 负曲线 ⇒ ≥ 0（两端翘、中间凹）。
        /// ⚠️ 与 `Arc01()`（归一化到 0..1 再乘满弧）**不是一个口径** —— 那个是画位置用的，
        /// 两者差一个常量（全靠 `baselineY` 吸收）。</summary>
        float ArcOffsetFromBase(float t, int count)
        {
            float hMod = heightModifierByCount.Evaluate(CountFraction(count));
            return yAxisCurve.Evaluate(Mathf.Clamp01(t)) * maxHeight * hMod * OurUnitsPerWorldUnit
                 * LayoutSpace.Scale;
        }

        /// <summary>
        /// 拖拽中：指针位置对应**第几个空位**（0..cardsInHand 闭区间）。
        /// 原版 `GetClosestInHandSlot` —— 手牌边拖边让位就靠它。
        /// </summary>
        public int GetClosestInHandSlot(Vector3 worldPos, int cardsInHand)
        {
            int n = Mathf.Max(1, cardsInHand);
            int idx = 0;
            for (int s = 0; s < n; s++)
                if (worldPos.x > LayoutSpace.ToWorld(NxOfSlot(s, n), baselineY).x) idx = s + 1;
            return idx;
        }

        // ==================================================================
        //  摆
        // ==================================================================

        /// <summary>
        /// 重排。
        /// hoveredIndex 传 -1 表示没人被悬停。
        /// insertIndex 传 >=0 表示「拖拽中，这里有个空位」—— 其余牌按**多一张**的间距排，
        /// 把那个位置空出来（边拖边让位）。
        /// </summary>
        public void Refresh(IReadOnlyList<CardView> cards, int hoveredIndex = -1, int insertIndex = -1)
        {
            if (cards == null) return;
            int n = cards.Count;
            if (n == 0) return;

            bool hole = insertIndex >= 0;
            int slots = hole ? n + 1 : n;

            for (int i = 0; i < n; i++)
            {
                var c = cards[i];
                if (c == null) continue;
                int slot = (hole && i >= insertIndex) ? i + 1 : i;

                var pos = SlotPosition(slot, slots);
                float angle = RotationAt(slot, slots);
                float scale = cardScale * LayoutSpace.Scale;

                // ---- 原版 `useExtraSpaceOnSelectedCard`（我方 = 1）----
                // 选中一张 ⇒ **它左边的所有牌往左让、右边的所有牌往右让**，让位量 = VarsDevice 那条曲线
                // （桌面恒 2.0 世界单位 = 29.7 px）。⚠️ **装不下时不让**（原版那一支会把 extra 归零）——
                // 这正是「牌少」与「牌多」的又一处不同：≤7 张会让，≥8 张不让。
                if (hoveredIndex >= 0 && slot != hoveredIndex && !IsCompressed(slots))
                {
                    if (slot < hoveredIndex) pos.x -= selectedCardExtra;
                    else if (hoveredIndex < slot) pos.x += selectedCardExtra;
                }

                // 悬停（= 原版 `ShowCardInHand` 的「亮出来」态）：抬起 `cardInHandShownYOffset` px + 放大 1.3
                if (i == hoveredIndex)
                {
                    pos.y += hoverLift;
                    scale *= hoverScale;
                }

                // 层序：相机在 -Z 看 +Z，**z 越小越靠前**。原版「左卡 z < 右卡 z」= 左边压上面。
                float z = i * zOrderStep - (i == hoveredIndex ? frontZ : 0f);
                pos.z += z;

                Place(c, pos, angle, scale);
            }
        }

        void Place(CardView c, Vector3 pos, float angle, float scale)
        {
            if (!animateRelayout) { c.SetPose(pos, angle, scale); return; }
            // 只有真的会动的地方才补间（拖拽让位每帧都可能重排，别把补间叠起来）
            c.transform.DOKill();
            CardTween.ToPose(c.transform, pos, angle, scale, CardTween.RelayoutDuration, Ease.OutQuad);
        }

        // ==================================================================
        //  自检/诊断
        // ==================================================================

        /// <summary>整把手牌的水平跨度（世界单位）—— 自检用</summary>
        public float SpanWorld(int count)
        {
            if (count <= 1) return 0f;
            return (count - 1) * SpacingFor(count) * LayoutSpace.VisibleWidth;
        }

        public string Describe(int count)
        {
            if (count <= 1) return "手牌 1 张：无间距/无弧";
            float cardW = CardView.Width * cardScale * LayoutSpace.Scale;
            float sp = SpacingFor(count) * LayoutSpace.VisibleWidth;
            return $"手牌 {count} 张：中心距 {sp:F3} 世界（{sp / cardW:F2} 卡宽）"
                 + $"  跨度 {SpanWorld(count):F2} / 上限 {MaxLayoutWorld():F2}"
                 + $"  满弧 {ArcHeightFull * 100f * LayoutSpace.Scale:F2} 归一化×100"
                 + $"  张角修正 {rotationModifierByCount.Evaluate(CountFraction(count)):F2}";
        }
    }
}
