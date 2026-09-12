// HandLayout.cs — 手牌扇形布局（原版 `CardsHorizontalLayout` 的 2D 移植）
//
// **数值不是拍的，是从原版序列化数据里搬的**，字段名和原版一一对应，方便回头对账。
//   类定义：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardsHorizontalLayout.cs`
//   权威实例：`d:/2/解包整理/07_场景/battlearena1/MonoBehaviour/MonoBehaviour_5271.json`
//
// ⚠️ 这个场景里有**两份** CardsHorizontalLayout 实例，别拿错：
//     · MB 5271（挂在 GO 1192）—— **权威**。m_scale=0.73 → 卡正好 165×263 px；
//       m_verticalOffsetLookTo=-330（负的，和 Tooltip "Should be negative" 对得上）；
//       m_betweenElementsSpacing=1.45 → 0.95 卡宽（牌几乎挨着，不重叠）。
//     · MB 4053（挂在 GO 58）—— 另一套预设（scale 0.54 / lookTo 74.5 正值 / spacing 0.6），
//       `资料/对战排版_原版数值与改造方案.md` 引的是这一份，**它的单位自相矛盾**，别用。
//       （`m_maxLayoutSizeSmallScreen` 在 4053 上是 0.51、在 5271 上是 0.51 小屏档 —— 说明
//        这两个数都是「占可见宽度的比例」，不是绝对距离。）
//
// 单位换算（原版 3D → 我这套 2D 归一化坐标）：
//   原版 UI 相机 fov40 / 平面 100 → **14.835 px / 世界单位**；手牌所在的深度是 108 px/单位
//   （165 px 的卡 = 2DCard 宽 2.0927 × scale 0.73 × 108）。我这套是「可见高恒 10 单位」，
//   1080p 下正好也是 108 px/单位 —— **所以手牌这一块的像素数可以 1:1 搬过来**。
//   棋盘那一层深度不同（k=182.14），是另一套换算，见 `BoardLayout`。
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

        [Tooltip("m_scale = 0.73 × 2DCard 宽 2.0927 = 1.528 世界单位 = 165 px；"
               + "除以 CardView.Width(1.45) 就是这里的缩放")]
        public float cardScale = 1.054f;

        // ==================================================================
        //  版面（原版没这几个字段，是这套 2D 布局自己的）
        // ==================================================================

        [Tooltip("手牌中心行的 y（归一化）—— 原版 HandAnchor 链 y≈961/1080、"
               + "Godot 线取 950（槽底 + 卡半高 + 9px 隙）。两端那张卡在 baselineY - 弧高")]
        public float baselineY = 0.140f;

        [Tooltip("悬停时抬起多少（归一化）—— 原版 useExtraSpaceOnSelectedCard=1 的简化")]
        public float hoverLift = 0.06f;

        [Tooltip("邻牌让位：相邻的牌往外挪多少（归一化）")]
        public float neighborShift = 0.018f;

        [Tooltip("悬停/拖拽的那张往前提多少 z")]
        public float frontZ = 0.5f;

        [Tooltip("原版 useZOrder=1：「Left card Z < right card Z」= **左边的压在上面**。"
               + "所以露出来的是每张牌的右半边 —— 卡面上的费用（右上）和生命（右下）正好在那儿，对得上")]
        // ⚠️ **必须大于「一张卡内部各层的 z 跨度」**（`CardView` 里 立绘 +0.03 … 文字 −0.03 = 0.06）。
        //    原来是 0.004，比跨度还小 —— 于是**后面那张卡的卡面文字会穿到前面那张卡上面**
        //    （文字层在自己的卡里最靠前，就跑到邻牌前面去了）。2026-09-12 把效果文字放大到原版字号后
        //    这个毛病变得很明显（截图里能看到「字横穿别人的卡」），所以把步长提到 0.08。
        public float zOrderStep = 0.08f;

        [Tooltip("重排走补间（拖拽让位才不跳）。批处理自检里关掉 —— 位置要当场精确")]
        public bool animateRelayout = false;

        // ==================================================================
        //  换算常量
        // ==================================================================

        /// <summary>原版 UI 相机：fov40 / 平面 100 → 14.835 px 每世界单位</summary>
        const float OriginalPxPerUnit = 14.835f;

        /// <summary>我这套：可见高 10 世界单位 = 1080 px</summary>
        const float PxPerNormY = 1f / 1080f;

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
            float cardW = CardView.Width * cardScale * LayoutSpace.Scale;      // 世界
            float natural = betweenElementsSpacing * LayoutSpace.Scale;        // 世界
            float cap = MaxLayoutWorld();
            if (count * natural > cap) natural = cap / count;
            return natural / LayoutSpace.VisibleWidth;                          // 归一化
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
        public float RotationAt(int index, int count)
        {
            int n = Mathf.Max(1, count);
            if (n <= 1) return 0f;

            float dx = (NxOfSlot(index, n) - 0.5f) * LayoutSpace.VisibleWidth;   // 世界
            float look = Mathf.Max(1f, lookToDistance * LayoutSpace.Scale);
            float mod = rotationModifierByCount.Evaluate(CountFraction(n));
            float deg = -Mathf.Atan2(dx, look) * Mathf.Rad2Deg * mod;
            return invertRotation ? -deg : deg;
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

                // 悬停：自己被抬起来，紧邻的往两边让一点
                if (hoveredIndex >= 0)
                {
                    if (i == hoveredIndex) pos.y += hoverLift;
                    else if (Mathf.Abs(i - hoveredIndex) == 1)
                        pos.x += (i < hoveredIndex ? -1f : 1f) * neighborShift;
                }

                // 层序：相机在 -Z 看 +Z，**z 越小越靠前**。原版「左卡 z < 右卡 z」= 左边压上面。
                float z = i * zOrderStep - (i == hoveredIndex ? frontZ : 0f);
                pos.z += z;

                Place(c, pos, angle, cardScale * LayoutSpace.Scale);
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
