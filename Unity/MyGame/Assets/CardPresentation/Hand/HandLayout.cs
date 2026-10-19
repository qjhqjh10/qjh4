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
//
// 🆕 **2026-10-17（B1 批）又收了四处**，逐条判据见 `资料/普查产出_1017/W_B1_手牌布局.md`：
//     · **小屏档的触发条件**（项 9）—— 原来按「可见宽 < 设计宽」猜，现在读 `SmallScreenUI.Enabled`
//       （= 原版 `GameStaticData.smallScreenUI` 静态 bool），见 `MaxLayoutWorld()`；
//     · **`GetClosestInHandSlot`**（项 11）—— 换成原版的「逐卡比 |Δx| 取最近」，分界从中点算，见该方法注释；
//     · **`useExtraSpaceOnSelectedCard` 的门闸方向**（项 8）—— 🔴 **原来反了**：原版是**装得下时归零**，
//       即**压缩态（我方 ≥8 张）才让位**；同时补上 `useExtraSpaceOnSelectedCard` 这个字段（敌方 = 0）；
//     · **`useZOrder`**（项 7）—— 判据补齐：原版靠 **嵌套 Canvas 的 `sortingOrder` + `SetSiblingIndex`**
//       （= `名单长度 − 序号 − 1`），`z` 只加 0.001；我们没 canvas ⇒ **这一格是「原版没有等价物」**，
//       保留真 z 步长，出处见 `zOrderStep` 那段。
//   ➖ **`m_inverted`（项 12）不实现**：全库 13 战场 × 4 份 = 52 份实例**全是 0**，见 `invertRotation` 下面那段注释。
//
// 🆕 **2026-10-19（`T01` · 施工图 `资料/普查产出_1019/W_工具线_T01施工图_1019b.md` §一·⑤）** ——
//   **把最后一处还在驱动运行时的 Godot 值（`DefaultBaselineY = 0.1204` = `1 − 950/1080`）换成原版口径**：
//     · `DefaultBaselineY` 0.1204 → **0.11**（原版 `HandAnchor` 1.1 × 108 = **118.8 px 距底** = 961.2 px 距顶）；
//     · `EnemyBaselineY` 0.9102 → **0.999704**（敌方 `HandAnchor` 距顶 **0.32 px**，链条见那个常量的 doc）；
//     · `SlotPosition` 的弧高**换回原版公式** `ArcOffsetFromBase`（基准 + 整条曲线 × maxHeight × hMod）——
//       旧的 `ArcHeightFull × Arc01` 是「先减掉曲线最小值再乘」，**只在满 12 张（hMod≈1）时等价**，
//       牌少时会把整把手牌越压越低（1 张低 31.5 px）；
//     · 新增 `fanCenterX`：原版 `GetPosition` 的 x 基准 = `this.transform.position.x`，玩家 `HandAnchor.x = 0.125×108`
//       = **13.5 px 偏右** ⇒ 扇心 973.5 px（我们原来整把**偏左 13.5 px**）；敌方 `HandAnchor.x = 0` ⇒ 0.5。
//   ⚠️ 观感变化是真实的（手牌整体下移 ~42.6 px、卡底出屏 44 px）—— **原版就是这样**（铁律 11/12：
//     照原版做，真机验收攒到复刻完成）。
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

        /// <summary>🔴 **我方手牌【布局基准】的 y（归一化，从**下**算）= 0.11** ——
        /// 即原版 `HandAnchor`：`Transform_1455.localPosition.y = 1.1` × `HandArea`
        /// （`Transform_1401`）`localScale = 108` = **118.8 px 距底 = 961.2 px 距顶**
        /// （16:9 下 1 canvas 单位 = 1 px；`CanvasScaler` 参考分辨率 1920×1080、`match=0`）。
        ///
        /// <para>🔴 **它是「基准」不是「卡心」**：原版 `GetPosition` 的输出 =
        /// `基准 + yAxisCurve(t) × m_maxHeight × hMod`（曲线 ≤0 ⇒ 只往下垂），见 <see cref="SlotPosition"/> /
        /// <see cref="ArcOffsetFromBase"/>。所以**中间那张卡心 ≈961.2 px、两端 ≈992.6 px**；
        /// 卡底在两端 = 992.6 + 131.3 = **1124.0 px**（出屏 44 px —— **原版就是这样**）。</para>
        ///
        /// <para>⚠️ **2026-10-19 就地订正（铁律 5 · `T01` §一·⑤）**：原值 **0.1204** = `1 − 950/1080`，
        /// 950 来自**我们自己的上一版 Godot 复刻** `d:/warpforge/scripts/battle.gd:407`（`HAND_Y = 950`）——
        /// **不是原版字段**（`CLAUDE.md` 铁律 2：只能当旁证）。「取 0.1204 让卡底正好压屏幕下沿」是**事后解释**
        /// （真要贴边该是 `131.35/1080 = 0.12162`，不是 0.1204）。旧值让**每张手牌比原版高约 42.6 px**
        /// （卡心 42.6；`B6` 记的「≈11 px」只比了「我们的两端 vs 原版的中间」，漏了弧高代数差 31.4 px）。</para>
        ///
        /// 🔴 **判据只此一处**：`BattleScene` 引用它（`HandBaselineY`），⛔ 别在别处再写一个数。</summary>
        public const float DefaultBaselineY = 1.1f * 108f / LayoutSpace.DesignPxH;   // 118.8/1080 = 0.11

        /// <summary>敌方手牌卡缩放 = 原版 `m_scale 0.54` ⇒ 卡宽 2.0927 × 0.54 × 108 ≈ **122 px**
        /// （我方是 165 px，同一套 2DCard 预制体，只是 `m_scale` 不同）。</summary>
        public const float EnemyCardScale = 0.54f;

        /// <summary>🔴 **敌方手牌【布局基准】的 y（归一化，从**下**算）= 0.999704** ——
        /// 原版敌方 `HandAnchor` 就挂在**屏幕顶边**（距顶 **0.32 px**）。
        ///
        /// <para>**链条（全部现读解包资源）**：
        /// `UpperAnchor`（`RectTransform_2762`：anchor/pivot 顶部、`sizeDelta (100,100)`
        /// ⇒ 框心距顶 **50 px**）→ `EnemyArea`（`RectTransform_2798`：anchor 中、pivot 顶部、
        /// `anchoredPosition (0,0)` ⇒ 顶边落在 50 px）→ `HandArea`（`Transform_1295`：localPosition 0、
        /// `localScale 108`）→ `HandAnchor`（`Transform_1257`：`y = 0.46 × 108 = 49.68 px`，朝上）
        /// ⇒ 距顶 = `50 − 49.68` = **0.32 px** ⇒ 归一化（从下算）`1 − 0.32/1080` = **0.999704**。</para>
        ///
        /// <para>⚠️ **2026-10-19 就地订正（铁律 5 · `T01` §一·⑤ 风险 9）**：原值 **0.9102** 是
        /// 「归一化弧高 + 卡顶贴屏幕上沿」的**旧推导** —— 和 `DefaultBaselineY 0.1204` 是**同一套
        /// Godot 时代的代数**，把弧高的常量部分吸收进了 baseline。新版 `SlotPosition` 换成
        /// 原版口径（基准 + 整条曲线 × maxHeight × hMod）之后，**它必须回到原版节点值**，否则同一把牌
        /// 会被算两次偏移。改完敌方手牌比原来**上移 ~97–136 px**（原版就是**只露下半张**，
        /// 两端还翘出屏幕上沿 ≈39 px —— `m_maxHeight = −0.78` 是负的）。</para></summary>
        public const float EnemyBaselineY = 1f - (50f - 0.46f * 108f) / LayoutSpace.DesignPxH;   // 1 − 0.32/1080

        [Tooltip("m_maxLayoutSize = 0.59（16:9 档）：首卡中心↔末卡中心的**上限**，"
               + "单位是「占可见宽度的比例」。超过它就把间距压下来")]
        public float maxLayoutSize = 0.59f;

        [Tooltip("m_maxLayoutSizeSmallScreen = 0.51：Small Screen UI 开关【开着】时用这个（与屏宽无关）")]
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

        // ➖ **`m_inverted`（原版 +0x7C）我们没实现 —— 2026-10-17 按「全库还有没有不是 0 的实例」查过：
        //    13 个战场 × 4 份（我方 / 敌方 / 选牌 / 换牌）= **52 份实例，`m_inverted` 全部 = 0**。**
        //    查法：`d:/2/新解包资源/assets_full/` 里按 `m_betweenElementsSpacing` 定位到那 52 个
        //    `bundle_scenes_scenes_battlearena*/MonoBehaviour/*.json`（= CardsHorizontalLayout 专有的字段名），
        //    再逐个读 `"m_inverted"`。⇒ **这个分支在成品数据里触发不了**，实现它只会多一条
        //    没有判据可对照的死路（铁律 11 允许的两条例外里的第 ① 条：原版数据里就没有）。
        //    ⚠️ 真要实现时判据是现成的：`GetPosition` 头一句
        //    `if (m_inverted) cardIndex = (totalCards - cardIndex) - 1;`
        //    （`d:/2/tools/decomp_full/CardsHorizontalLayout__GetPosition.c:55`）——
        //    只翻**序号**、不翻任何曲线。

        [Tooltip("m_scale = 0.73 × 2DCard 宽 2.0927 = 1.528 世界单位 = 165 px。"
               + "🔴 判据收在 DefaultCardScale 一处，别手写数字")]
        public float cardScale = DefaultCardScale;

        // ==================================================================
        //  版面（原版没这几个字段，是这套 2D 布局自己的）
        // ==================================================================

        [Tooltip("手牌【布局基准】的 y（归一化，从**下**算）—— 原版 HandAnchor = 118.8 px 距底"
               + " = 961.2 px 距顶（0.11）。⚠️ 它是**基准**不是卡心：卡心 = 基准 + 弧高（≤0，原版口径），"
               + "满 12 张时两端比基准还低 ~31.4 px")]
        public float baselineY = DefaultBaselineY;

        /// <summary>手牌扇形的**水平中心**（归一化 x）。
        ///
        /// <para>判据（`T01` §四·2，2026-10-19 现核）：原版 `CardsHorizontalLayout.GetPosition` 的 **x 基准**
        /// 取的是 `this.transform.position.x`（VA `0x1808438A0` 的指令流：
        /// `mov rcx, rbx; call Component.get_transform` → `call Transform.get_position`，
        /// 返回值的低 32 位直接进 x 那一格），而**玩家** `HandAnchor.localPosition.x = 0.125`
        /// × `HandArea` 108 = **13.5 px 偏右** ⇒ 扇心在 **973.5 px**，**不是**屏幕正中 960
        /// （我们原来整把手牌比原版**偏左 13.5 px**）。</para>
        ///
        /// <para>⚠️ **敌方**那份的 `HandAnchor.localPosition.x = 0`（`Transform_1257`）⇒
        /// `ConfigureForEnemy()` 里置回 **0.5**（敌方扇心就在正中）。
        /// `RotationAt` 的 `dx` 是「卡相对扇心」的量，所以它减的是**本字段**、⛔ 不是常数 0.5。</para></summary>
        [Tooltip("手牌扇形水平中心（归一化 x）—— 我方 0.50703125（原版 HandAnchor x = 13.5 px），敌方 0.5")]
        public float fanCenterX = DefaultFanCenterX;

        [Tooltip("悬停时抬起多少（**世界单位**，×108 = px）—— 原版 `VarsDevice.cardInHandShownYOffset`"
               + " = **30 px**（桌面；小屏 ≤7 吋取 `…YOffsetMin` = 20）。原来我们自造 0.06（=6.5 px），2026-09-25 换成实读值")]
        public float hoverLift = HoverLiftPx / 108f;

        [Tooltip("悬停那张的缩放 —— 原版 `VarsDevice.cardInHandShownScale` = **1.3**（移动端 1.7）")]
        public float hoverScale = 1.3f;

        [Tooltip("**选中一张牌时，两侧的牌各往外让多少**（世界单位）—— 原版 `VarsDevice.extraSpaceOnSelectedCard`"
               + " **桌面 = 平线 2.0** · 移动端 = (0,2.0)→(1,5.0) 斜坡。"
               + "🔴 **只有「装不下」（压缩态，我方 ≥8 张）才让** —— 装得下时原版把 extra 归零")]
        public float selectedCardExtra = SelectedExtraWorld;

        /// <summary>原版 `useExtraSpaceOnSelectedCard`（序列化 @+0x7E）：**我方 = 1、敌方 = 0、选牌/换牌 = 0**。
        /// 🔴 **这是「选中让位」的第一道门**，第二道门在 `PlayerHand.PositionCardInHand` 里
        /// （`if (isPlayer) selectedCardIndex = 传入值; else selectedCardIndex = -1;`
        ///  —— `PlayerHand__PositionCardInHand.c:28-30`，**只有玩家手牌才把 selectedCardIndex 传下去**）。
        /// 两道门是**两回事**：这一道是「这套布局用不用让位」，那一道是「这局这张牌算不算被选中」。
        /// ⚠️ 别把它和 `allowSelectCardLayoutOverFlow`（我们恒按 =1 处理）混起来 —— 那个管的是
        /// 「让位占的宽算不算进 `totalWidth`」，四份实例都 =1 ⇒ 不算。</summary>
        public bool useExtraSpaceOnSelectedCard = true;

        [Tooltip("悬停/拖拽的那张往前提多少 z")]
        public float frontZ = 0.5f;

        [Tooltip("原版 useZOrder=1：「Left card Z < right card Z」= **左边的压在上面**。"
               + "所以露出来的是每张牌的右半边 —— 卡面上的费用（右上）和生命（右下）正好在那儿，对得上")]
        // 🔴 **2026-10-12（`A1665`）就地订正：z 步长【不再是】卡与卡之间的分层手段** —— 那个活交给了
        //    `queueTop` 那一段渲染队列（见下面 `queueTop` 与 `Refresh` 里那三句）。
        //    ⇒ 「**必须大于一张卡内部各层的 z 跨度**」这条隐含契约**作废**：只要每张卡一段队列，
        //    前卡**任何**一层都压得住后卡**任何**一层，卡内 z 跨度多大都不影响卡间。
        //    本值保留 0.08（它现在只管**几何纵深**观感，且改了会动已有截图/断言），但**别再往里塞判据**。
        // 历史（留着是因为它证过「同一个症状报过两次」）：原来是 0.004、比卡内跨度还小，于是
        //   **后面那张卡的卡面文字会穿到前面那张卡上面**（2026-09-12 把步长提到 0.08 止血）；
        //   而卡内真实跨度是 **0.09**（`shadow +0.06` … 文字 `−0.03`，`0.06` 那条注释漏算了 `shadow` 层）
        //   ⇒ 0.08 其实**仍然小于跨度**，**用户 2026-10-12 又报了一次同一个症状**。
        //   ⇒ 这次上队列（根治），不再调步长。
        public float zOrderStep = 0.08f;

        /// <summary>🆕 2026-10-12（`A1665`）**手牌队列段的顶号** —— 第 `top` 靠前的那张拿 `queueTop − top`。
        ///
        /// <para>**为什么必须走队列**：卡面每一层原本都是 `Sprites/Default`（Transparent **3000**），
        /// 「谁盖谁」只剩「到相机的 3D 距离」在排 —— 而手牌的 z 步长小于卡内 z 跨度（见 `zOrderStep`）
        /// ⇒ 邻牌的文字横穿（2026-09-12、2026-10-12 同一症状报过两次）。队列是**唯一**能保证
        /// 「前卡整段 &gt; 后卡整段」的手段（原版用嵌套 Canvas 的 `sortingOrder`，见下面那段）。</para>
        ///
        /// 🔴 **为什么是「往下排」而不是「往上排」**（号段约束，别改方向）：
        ///   `CardView.QCard = 3000` 是**全仓的卡上限** —— `Board/BoardLayout.cs:111`（`MarkerQ = 3100`
        ///   「**必须大于卡的 3000**」）、`Core/CardWinBox.cs:83`（`QEffect = 3020` 画在卡之上）、
        ///   `Battle/CardDisplayWindow.cs:83`（遮罩 `QChrome = 3000`，靠 `z = −2` 压住手牌）。
        ///   ⇒ 手牌只能 ≤ 3000。顶号取 3000、序号往下减，**上面那三条假设全部继续成立**。
        /// </summary>
        [Tooltip("手牌队列段的顶号（最靠前那张拿它）。⚠️ 别改到 3000 以上 —— 见上面那段号段约束")]
        public int queueTop = CardView.QCard;

        /// <summary>手牌队列段的**下限**（纯防守）。战斗场景里 3000 以下是空的（只有 `BattleDriver` 那两块
        /// `QFiller = 2999` 的屏外黑边），再往下（≤2980）就跨到**外壳**那些窗的号段了
        /// （`Deck/DeckRuntime.cs` 的 `QAreaBg = 2980` 那一段 —— **不同场景、今天不共存**）。
        /// ⚠️ 10 张手牌 + 一个洞 = 11 槽 ⇒ 最低 3000−11 = **2989**，正常**碰不到**这个下限。
        /// 真碰到时**钳住 + `LogError`**（不静默失败）：钳住的那几张会退回按 z 排，
        /// 也就是**可能重新出现「邻牌文字横穿」**—— 那时要抬号段或减手牌上限。</summary>
        public const int QueueFloor = 2980;
        static bool _queueFloorWarned;
        static bool _queueTopBad;

        /// <summary>上一趟 `Refresh` 看到的那几张 —— **只为「被拖起来的那张」那一条**（见 `Refresh` 里那段）。</summary>
        readonly List<CardView> _lastSeen = new List<CardView>();
        readonly HashSet<CardView> _nowSet = new HashSet<CardView>();

        // 🔴 **2026-10-17 查证：原版那个 `useZOrder` 的加量【不适用】，而且现在有确凿判据了。**
        //
        //   ① 原版那个 `useZOrder` 的加量 = **0.001 世界单位 ≈ 0.015 px**
        //      （`CardsHorizontalLayout..cctor` 的 `Z_OFFSET_BY_POSITION_INDEX = 0x3a83126f`，即 0.001f；
        //       消费者 `CardsHorizontalLayout__GetPosition.c` 末尾
        //       `if (useZOrder) local_e0 = cardIndex * Z_OFFSET + basePos.z;`）。
        //      **它根本不是分层手段** —— 0.015 px 排不出前后，UGUI 也不按 z 排序。
        //
        //   ② **原版真正的分层机制 = 嵌套 Canvas 的 `sortingOrder` + 兄弟序**：
        //      `PlayerHand._MoveCardsInHandToPosition_d__76__MoveNext.c:55` 每张牌调
        //      `CardScript.SetSortingOrder(**名单长度 − 序号 − 1**)`
        //      → `CardScript__SetSortingOrder.c:5` → `BattleCardUI__SetSortingOrder.c:8,11`：
        //        `Canvas.set_sortingOrder(order)` **且** `Transform.SetSiblingIndex(order)`。
        //      ⇒ 序号越大 sortingOrder 越小 ⇒ **左边的压在上面** ✓ —— 与我们 `z = i * step`
        //        （z 越小越靠前、i 越大越靠后）**语义完全一致**，只是载体不同。
        //
        //   ③ 我们是**世界空间的 2D 四边面**（`ImageQuad`），**没有 canvas 排序** ⇒
        //      **不能照抄 0.001**（照抄 = 全部牌同一层，卡面文字会横穿邻牌）。
        //      ⚠️ **这一格是「原版没有等价物、我们用另一种手段顶上」**，照铁律 3 标注：**不是原版的做法**。
        //      ✅ **2026-10-12（`A1665`）已换成原版那种手段**：每张卡的**所有图层**共用一段 render queue
        //      （`CardView.SetQueue`，手牌那一段由 `queueTop` 给）—— 等价于原版「一张卡一个 `sortingOrder`」。
        //      ⇒ 序号越大 ⇒ 队列越小 ⇒ **左边的压在上面**，与上面 ② 的语义逐字一致，只是载体不同。
        //      （原来这里写着「那要动 `CardView` 的分层，**不在本批白名单里**」—— 本轮动了。）

        [Tooltip("重排走补间（拖拽让位才不跳）。批处理自检里关掉 —— 位置要当场精确")]
        public bool animateRelayout = false;

        // ------------------------------------------------------------------
        //  🔴 2026-10-11（`A1540`）：最近一趟 `Refresh` 给每张牌算出的**位姿**
        // ------------------------------------------------------------------
        // 为什么要有它：`Place` 在 `animateRelayout == true` 时**只开一条 0.2 s 补间、不当场落位**
        // （见下面 `Place` 那一支），而 `CardFeel.DealIn` 恰恰是**读当时的位置**当落点
        // （`Core/CardFeel.cs:1142` 的 `Vector3 to = tr.position;`）—— 同一帧里那个位置**还是父级原点**
        // （= 画布正中 960,540）⇒ 刚抽到的牌从牌堆飞进画布正中并停住，而 `MulliganPanel` 同帧按
        // `c.transform.position` 建的「换」钮**全都落到牌堆上**（实拍 `_tmp_view/ui_1019/17_battle.png`）。
        // ⇒ 调用方（`BattleDriver.SyncHand`）在读落点**之前**先用本表把新牌落实。
        // ⚠️ 每趟 `Refresh` **重填**（不留上一趟的）—— 它只保证「最近一次排布的结果」。
        // ⚠️ 世界坐标，与 `SlotPosition` / `CardTween.ToPose`（`DOMove`）同一套。
        struct HandPose { public Vector3 pos; public float angle; public float scale; }
        readonly Dictionary<CardView, HandPose> _lastPose = new Dictionary<CardView, HandPose>();

        /// <summary>最近一趟 `Refresh` 给这张牌算的位姿。没排过 ⇒ `false`。
        /// `A1540`：发牌入场要在读落点之前用它先把牌落实（见 `_lastPose` 的注释）。</summary>
        public bool TryLastPose(CardView c, out Vector3 pos, out float angle, out float scale)
        {
            HandPose p;
            if (c != null && _lastPose.TryGetValue(c, out p))
            {
                pos = p.pos; angle = p.angle; scale = p.scale;
                return true;
            }
            pos = Vector3.zero; angle = 0f; scale = 1f;
            return false;
        }

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
            useExtraSpaceOnSelectedCard = false;         // useExtraSpaceOnSelectedCard = 0（我方是 1）
            // m_verticalOffsetLookTo = +74.5（正值，我方是 −330）—— 用和 `lookToDistance` 同一个桥换算
            lookToDistance = 74.5f * OriginalPxPerUnit / 108f;      // = 10.23
            cardScale = EnemyCardScale;
            baselineY = EnemyBaselineY;                  // 1 − 0.32/1080 = 0.999704（敌方 HandAnchor 距顶 0.32 px）
            fanCenterX = 0.5f;                           // 敌方 HandAnchor.x = 0（我方 0.125 × 108 = 偏右 13.5 px）

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

        /// <summary>`GetClosestInHandSlot` 里「最近距离」的**初值** —— 原版 `.rdata 0x1834b3354` 实读 = **1e6**。
        /// 它远大于手牌的任何跨度 ⇒ 效果就是「0 号槽是默认值、任何真实候选都赢过它」——
        /// 照抄它是为了让「平局 / 全都很远」这两种边角与原版逐位一致（不是随手取个大数）。</summary>
        const float ClosestSlotInitialDistance = 1e6f;

        /// <summary>我这套：可见高 10 世界单位 = 1080 px</summary>
        const float PxPerNormY = 1f / 1080f;

    /// <summary>**1 原版世界单位 = 多少「我们的单位」** = 14.835/108 = 0.13736。
    /// 两个空间的桥：`1 设计单位（卡宽/间距那套）= 108 px`、`1 世界单位（弧高/lookTo 那套）= 14.835 px`。
    /// 🔴 两者差 7.28 倍**不是错**：原版 x 乘父级 `lossyScale`（`HandArea` 108 × Canvas 1/14.835 = **7.28**），
    /// y 不乘 —— 逐值推导见 `资料/手牌布局_原版算法与参数.md` §四。</summary>
    public const float OurUnitsPerWorldUnit = OriginalPxPerUnit / 108f;

    /// <summary>手牌扇形相对屏幕中心的**水平偏移**（设计 px）= 原版 `HandAnchor.localPosition.x 0.125`
    /// × `HandArea.localScale 108` = **13.5 px**（判据与 `fanCenterX` 同一处：`Transform_1455` / `Transform_1401`
    /// + `GetPosition` 的 VA 指令流）。⚠️ 归一化之后 `13.5/1920 = 0.00703125` —— 这个**归一化值在所有宽高比下通用**：
    /// 我们的世界单位恒 108 px、窄屏靠 `LayoutSpace.Scale` 整体缩，两者正好抵消。</summary>
    public const float FanCenterOffsetXPx = 0.125f * 108f;

    /// <summary>我方手牌扇形水平中心（归一化）= `0.5 + 13.5/1920` = **0.50703125**（判据见 `fanCenterX`）。</summary>
    public const float DefaultFanCenterX = 0.5f + FanCenterOffsetXPx / LayoutSpace.DesignPxW;

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

        /// <summary>弧线在 t（0=最左，1=最右）处抬起多少 —— 归一化到 0..1（两端 0、中间最高）。
        ///
        /// <para>⚠️ **2026-10-19（`T01`）：这个口径【不再用于定位】** —— 它「先把曲线最小值减掉再乘满弧」，
        /// 只在满 12 张（hMod≈1）时与原版等价，牌少时会把整把手牌压下去（见 <see cref="SlotPosition"/>）。
        /// 定位一律走 <see cref="ArcOffsetFromBase"/>。本方法只留作诊断/显示；
        /// ⛔ **别再拿它乘 <see cref="ArcHeightFull"/> 去算位置**。</para></summary>
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
        /// ⚠️ 它**同时**是「选中一张牌时两侧要不要让位」的开关 —— 原版在**装得下**那一支里把 `extra` 归零
        /// （`GetPosition.c`：`else { fVar10 = 0f; }`）⇒ **`IsCompressed == true` 才让位**。
        /// 我方手牌 n ≥ **8** 起为真（`0.6026 × 屏宽 ÷ 156.6 px`），所以「≥8 张时选牌才让位、≤7 张不让」。
        /// 🔴 判据与 `SpacingFor` **共用这一处**，别各写一份。</summary>
        public bool IsCompressed(int count)
        {
            if (count <= 1) return false;
            return count * betweenElementsSpacing * LayoutSpace.Scale > MaxLayoutWorld();
        }

        /// <summary>首卡中心↔末卡中心的上限（世界单位）= 原版 `cap`
        /// = `(baseSize + 宽高比修正) × 该深度处的可见宽度`
        /// （`GetPosition.c` 的 `fVar10 = (fVar19 + fVar10) * fVar16 * (dVar13 + dVar13) * fVar17`
        ///   —— `fVar16` = `cam.aspect`、`2·dist·tan(fov/2)` = 那个深度的可见宽度）。
        ///
        /// 🔴 **2026-10-17 更正触发条件**：`baseSize` 由 **`SmallScreenUI` 开关**决定，
        /// **与屏宽 / 窗口宽 / 可见宽【无关】** —— 原版就是一个静态 bool 的三目：
        /// `if (GameStaticData.smallScreenUI) fVar10 = *(this+0x30);  // m_maxLayoutSizeSmallScreen
        ///  else fVar10 = *(this+0x2c);                              // m_maxLayoutSize`
        /// （`d:/2/tools/decomp_full/CardsHorizontalLayout__GetPosition.c:83-88`）。
        /// 这里原来写的是 `LayoutSpace.VisibleWidth &lt; LayoutSpace.DesignWidth`（**拿屏宽猜**）——
        /// 那是「一个值 ≠ 全部情况」那一类错（铁律 5·c）：**4:3 但不勾小屏 UI** 时我们走错档，
        /// **超宽屏勾了小屏 UI** 时我们也走错档。
        /// 现在读全工程唯一那一份开关：`Shell/TransformScalerBySmallScreenUI.cs` 的
        /// <see cref="SmallScreenUI.Enabled"/>（原版 `GameStaticData.smallScreenUI` 的等价物，
        /// 出厂 = false，只有设置→图形页那颗开关会写它）。</summary>
        public float MaxLayoutWorld()
        {
            float aspect = LayoutSpace.Cam != null ? LayoutSpace.Cam.aspect : LayoutSpace.DesignAspect;
            float baseSize = SmallScreenUI.Enabled ? maxLayoutSizeSmallScreen : maxLayoutSize;
            return (baseSize + aspectRatioModifier.Evaluate(aspect)) * LayoutSpace.VisibleWidth;
        }

        /// <summary>第 slot 张（共 count 张）的归一化 x —— **已含扇形水平中心** <see cref="fanCenterX"/>
        /// （我方偏右 13.5 px、敌方居中；判据见那个字段）</summary>
        public float NxOfSlot(int slot, int count)
        {
            int n = Mathf.Max(1, count);
            return fanCenterX + (slot - (n - 1) * 0.5f) * SpacingFor(n);
        }

        /// <summary>第 index 张（共 count 张）在哪 —— 位置(世界) 由这里算，旋转由 RotationAt 算。
        ///
        /// <para>🔴 **2026-10-19（`T01`）：弧高换回原版口径** ——
        /// `ArcOffsetFromBase`（= `yAxisCurve(t) × m_maxHeight × hMod`，**不归一化**），
        /// 而不是旧的 `ArcHeightFull × Arc01(t) × hMod`。</para>
        ///
        /// <para>**为什么不能只换 baseline 常量**：`Arc01` 是「先减掉曲线最小值（−31.6 px）、再乘 hMod」，
        /// 而原版是**整条曲线一起乘 hMod** ⇒ 两者只在 **hMod ≈ 1（满 12 张）** 时等价。
        /// 用「常数 baseline」替代的话：1 张会低 **31.5 px**、5 张低 ≈21 px、8 张低 ≈11 px ——
        /// 也就是**牌越少，整把手牌被压得越低**（静默、只在观感上）。</para>
        ///
        /// <para>判据 = 原版 `CardsHorizontalLayout__GetPosition`（VA `0x1808438A0`）：
        /// 输出 y = `transform.position.y + curve(t) × maxHeight × hMod`
        /// （`CardsHorizontalLayout__GetPosition.c:235-236`），基准就是 <see cref="fanCenterX"/> 那一段
        /// 解出的 `this.transform.position`。</para></summary>
        public Vector3 SlotPosition(int index, int count)
        {
            int n = Mathf.Max(1, count);
            float t = n == 1 ? 0.5f : index / (float)(n - 1);
            float arc = ArcOffsetFromBase(t, n) / Mathf.Max(1e-6f, LayoutSpace.VisibleHeight);  // 世界 → 归一化
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
            // dx = 卡相对**扇形中心**的横向距离 —— 原版 `dir = cardPos − (transform.position + lookTo)`
            // ⇒ 减的是扇心（`fanCenterX`），⛔ 不是常数 0.5（我方扇心在 973.5 px）。
            float dx = (NxOfSlot(index, n) - fanCenterX) * LayoutSpace.VisibleWidth;   // 我们的单位
            float look = Mathf.Max(1f, lookToDistance * LayoutSpace.Scale);
            float arc = ArcOffsetFromBase(t, n);                                  // 原版口径（≤0 我 / ≥0 敌）
            float denom = Mathf.Max(1f, look + (invertRotation ? -arc : arc));
            float mod = rotationModifierByCount.Evaluate(CountFraction(n));
            float deg = -Mathf.Atan2(dx, denom) * Mathf.Rad2Deg * mod;
            return invertRotation ? -deg : deg;
        }

        /// <summary>原版口径的弧高偏移（**相对基准行**，我们的单位）——
        /// = `yAxisCurve(t) × m_maxHeight × 按张数修正 × (14.835/108) × Scale`，**不归一化**
        /// （原版就是拿原始曲线值直接当 y 偏移：`GetPosition.c:235-236`）。
        /// 我方 ≤ 0（中间=基准、两端下垂）；敌方 = 负 maxHeight × 负曲线 ⇒ ≥ 0（两端翘、中间凹）。
        ///
        /// <para>🔴 **2026-10-19（`T01`）：定位用的就是这一条**（<see cref="SlotPosition"/> / `RotationAt` 共用）。
        /// ⚠️ 与 <see cref="Arc01"/>（归一化到 0..1 再乘满弧）**不是一个口径** —— 那个只在满 12 张时等价，
        /// 留作诊断，⛔ 别再拿它算位置。</para></summary>
        float ArcOffsetFromBase(float t, int count)
        {
            float hMod = heightModifierByCount.Evaluate(CountFraction(count));
            return yAxisCurve.Evaluate(Mathf.Clamp01(t)) * maxHeight * hMod * OurUnitsPerWorldUnit
                 * LayoutSpace.Scale;
        }

        /// <summary>
        /// 拖拽中：指针位置对应**第几个空位**（0..cardsInHand 闭区间）。
        /// 原版 `GetClosestInHandSlot` —— 手牌边拖边让位就靠它。
        ///
        /// 🔴 **2026-10-17 换成原版算法**（此前是「按阈值累加」的近似）。
        /// 逐句读 `d:/2/tools/decomp_full/CardsHorizontalLayout__GetClosestInHandSlot.c:14-33`：
        /// <code>
        /// iVar5 = 0; fVar7 = DAT_1834b3354;              // 初值 1e6（.rdata 实读），默认 0 号
        /// for (i = 0; i &lt; total; i++) {
        ///     p = GetPosition(i, total, selectedCardIndex: -1);   // ← 第三参 -1 = 选中让位不参与
        ///     d = (float)((uint)(pointer.x - p.x) &amp; DAT_1834b2e60); // ← 掩码实读 = 0x7FFFFFFF ⇒ |Δx|
        ///     if (fVar7 &lt;= d) { 保留旧的 }                        // ← **严格小于才换 ⇒ 平局取小序号**
        /// }
        /// </code>
        /// ⇒ **逐卡比 |指针.x − 卡.x|，取最近的那个**。
        ///
        /// ⚠️ **两张牌之间只有一个分界，而且它在两张卡中心的【中点】上** ——
        /// 旧写法把分界放在**卡中心**上（指针一过第 s 张的中心就判 s+1），**差半个间距**：
        /// 指针停在「第 s 张中心往右一点点」时，旧写法判 s+1、原版判 s（见 `CardBaseDemo` 的判别式断言）。
        ///
        /// ⚠️ **`cardsInHand` = 「除被拖那张以外、看得见的张数」**（= `CardInteraction.VisibleCards().Count`），
        /// 候选槽位是 `0..cardsInHand` 共 **`cardsInHand + 1`** 个 —— 与 `Refresh` 里 `slots = n + 1` 同一套。
        /// 这也是原版的口径：它把**被拖的那张也留在名单里**（`PlayerHand._MoveCardsInHandToPosition_d__76__MoveNext.c:55`
        /// 的循环里 `PositionCardInHand(手牌, 牌, 新序号, 名单长度, …)`），所以那边 `total = 名单长度 = others + 1`，
        /// 候选数与返回值域 `0..total-1` 与我们完全对齐。
        /// </summary>
        public int GetClosestInHandSlot(Vector3 worldPos, int cardsInHand)
        {
            int others = Mathf.Max(0, cardsInHand);
            int slots = others + 1;
            int best = 0;
            float bestDist = ClosestSlotInitialDistance;      // 原版初值 1e6（远大于任何跨度 ⇒ 0 号是默认值）
            for (int s = 0; s < slots; s++)
            {
                float slotX = (NxOfSlot(s, slots) - 0.5f) * LayoutSpace.VisibleWidth;
                float d = Mathf.Abs(worldPos.x - slotX);
                if (d < bestDist) { best = s; bestDist = d; }  // 严格小于 ⇒ 平局取小序号（照原版）
            }
            return best;
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
            _lastPose.Clear();          // 只保证「最近一趟」的结果（见 `_lastPose` 的注释）
            if (cards == null) return;
            int n = cards.Count;
            if (n == 0) { _lastSeen.Clear(); return; }   // 空手牌也要清 —— 否则下一趟会拿旧名单判「谁被拿起来了」

            bool hole = insertIndex >= 0;
            int slots = hole ? n + 1 : n;

            // 出厂值就是 `CardView.QCard`，但**新加的公字段**在老场景的反序列化里可能落成 0
            // （0 会让整把手牌排到所有东西**之下** ⇒ 手牌看不见，而且「≤ 上限」那条断言照样绿）⇒ 不静默。
            if (queueTop <= 0)
            {
                if (!_queueTopBad)
                {
                    _queueTopBad = true;
                    Debug.LogError($"[HandLayout] `queueTop` = {queueTop}（<= 0）—— 序列化丢了？"
                                 + $"已按 {CardView.QCard} 处理（手牌每张一段队列，顶号必须是正数）。");
                }
                queueTop = CardView.QCard;
            }

            // 🆕 2026-10-12（`A1665`）**拖起来的那张恒拿顶号** —— 为什么必须有这一块：
            //   `CardInteraction` 拖拽时把被拖那张**从传给本布局的名单里摘掉**
            //   （`Hand/CardInteraction.cs:579` 的 `VisibleCards()` 排除 `_dragging`），
            //   而它把那张卡抬到最前靠的**只是 z**（`:232` 的 `−dragLiftZ = −0.6`）。
            //   队列是**上一次**给它的那个号 ⇒ 不补这一步，被拖的牌会被**它左边那几张压住**
            //   （原来是「全 3000 + z 抬 0.6」⇒ 它自然在最前；换了队列之后这个副作用就没了）。
            //   判据 = **「这一趟带洞（`hole`，`CardInteraction.Relayout` 只在拖拽时传洞）
            //           且上一趟见过、这一趟不在名单里」的那张**。
            //   ⚠️ 这是**必需的补位**、不是新口径：层次的正主仍然是「布局给每张一个号」。
            //   ⚠️ 别的「从手牌离开」的路径（打出去 / 弃掉 / 洗回）也会命中这一条 ⇒ 它们拿到 3000
            //      （= 手牌段顶号）—— 它们随即被搬走/丢掉，**影响有界**（场上那一路在 3D 下压根不在
            //      这台相机上；2D 下场上卡本来也是 3000）。⛔ 别为此把这一块改成「按卡名猜」。
            //   🔧 **哪天 `CardInteraction` 自己调 `SetQueue`**，把这一块删掉（它才是知道「谁被拿起来」的那一层）。
            if (hole)
            {
                _nowSet.Clear();
                for (int i = 0; i < n; i++) if (cards[i] != null) _nowSet.Add(cards[i]);
                for (int i = 0; i < _lastSeen.Count; i++)
                {
                    var prev = _lastSeen[i];
                    if (prev == null || _nowSet.Contains(prev)) continue;   // `prev == null` 也吃掉「已销毁」
                    prev.SetQueue(queueTop);
                }
            }
            _lastSeen.Clear();
            for (int i = 0; i < n; i++) if (cards[i] != null) _lastSeen.Add(cards[i]);

            // 被选中/展示的那一张**在槽位空间里**是第几号 —— 原版比的是同一个索引空间里的两个序号
            // （`GetPosition(cardIndex, total, selectedCardIndex)`），带着空位时 `slot` 会整体错开 1。
            int selSlot = (hole && hoveredIndex >= insertIndex) ? hoveredIndex + 1 : hoveredIndex;

            for (int i = 0; i < n; i++)
            {
                var c = cards[i];
                if (c == null) continue;
                int slot = (hole && i >= insertIndex) ? i + 1 : i;

                var pos = SlotPosition(slot, slots);
                float angle = RotationAt(slot, slots);
                float scale = cardScale * LayoutSpace.Scale;

                // ---- 原版 `useExtraSpaceOnSelectedCard`（我方 = 1，敌方 = 0）----
                // 选中一张 ⇒ **它左边的所有牌往左让、右边的所有牌往右让**，让位量 = VarsDevice 那条曲线
                // （桌面恒 2.0 世界单位 = 29.7 px）。
                //
                // 🔴 **2026-10-17 更正方向**：原版是「**装得下就把 extra 归零**」，不是「装不下就不让」——
                //   `GetPosition.c` 那一段逐句核过：
                //     `fVar17 = totalCards * spacing;`
                //     `if (cap < fVar17) { spacing = cap / totalCards; }   // 压缩间距，extra 留着`
                //     `else { fVar10 = 0.0; }                              // ← 装得下 ⇒ extra 归零`
                //   （`d:/2/tools/decomp_full/CardsHorizontalLayout__GetPosition.c` 的 :201-214 那一段；
                //     ⚠️ `allowSelectCardLayoutOverFlow` 四份实例都 = 1 ⇒ `totalWidth` **不含** `2*halfExtra`，
                //     所以压缩门闸就是 `N × 自然间距 > cap`。）
                //   ⇒ **我方 ≥8 张（压缩态）才让位、≤7 张不让**；这跟字段名也自洽：
                //     牌挨得紧时才需要「额外空间」，本来就排得开就不需要。
                //   ⚠️ 原来这里写的是 `!IsCompressed`（正好反了），`资料/手牌布局_原版算法与参数.md`
                //     §二 的注释也跟着反了 —— 那条注释与它自己的伪代码互相矛盾（伪代码的 else 分支才是对的）。
                if (useExtraSpaceOnSelectedCard && hoveredIndex >= 0 && selSlot < slots
                    && slot != selSlot && IsCompressed(slots))
                {
                    if (slot < selSlot) pos.x -= selectedCardExtra;
                    else pos.x += selectedCardExtra;
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

                // ---- 🆕 2026-10-12（`A1665`）：**卡与卡之间的层级** ----
                // 🔴 **这一句才是「谁盖谁」的判据**（上面那个 z 步长只管几何纵深，见 `zOrderStep` 的注释）。
                //    一张卡 = 一段队列 ⇒ 前卡**任何**一层都压得住后卡**任何**一层。
                //    `top` = 「在这把手牌里的靠前序」：**悬停那张恒最靠前(=0)**，其余按槽号
                //    （槽号小 = 靠左 = 靠前）—— 与上面 `z` 的方向**一致**（z 小 ⇔ 队号大）。
                //    判据/号段约束见 `queueTop` 那一段。⚠️ 非悬停用 `slot + 1`（不是 `slot`）：
                //    给悬停那张留 0 号，这样「悬停是唯一的顶号」在**有洞/无洞**两态都成立。
                int top = (i == hoveredIndex) ? 0 : (slot + 1);
                int q = queueTop - top;
                if (q < QueueFloor)     // 防守：不许静默失败（见 `QueueFloor`）
                {
                    if (!_queueFloorWarned)
                    {
                        _queueFloorWarned = true;
                        Debug.LogError($"[HandLayout] 手牌 {n} 张（{slots} 槽）把队列号压到 {q}，"
                                     + $"低于本段下限 {QueueFloor} ⇒ **钳到 {QueueFloor}**"
                                     + "（那几张退回按 z 排 ⇒ 可能重新出现「邻牌文字横穿」）。"
                                     + "要么抬 `queueTop` 的号段、要么减手牌上限。");
                    }
                    q = QueueFloor;
                }
                c.SetQueue(q);

                Place(c, pos, angle, scale);
            }
        }

        void Place(CardView c, Vector3 pos, float angle, float scale)
        {
            // 记下这一趟算出来的位姿（`A1540`：发牌入场读落点前要靠它先落实，见 `_lastPose` 的注释）
            _lastPose[c] = new HandPose { pos = pos, angle = angle, scale = scale };
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
