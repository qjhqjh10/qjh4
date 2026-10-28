// VarsDevice.cs — 原版 `VarsDevice`（`ScriptableObject`，两份资产）的**数据件**
//
// ==================================================================================================
//  判据（全部来自解包资源，不是猜的）
// ==================================================================================================
//  · 类型 / 字段名 / 字段序 = `Assembly-CSharp/VarsDevice.cs`（签名桩，字段完整）：
//      cardInHandShownScale · cardInHandShownYOffset · cardInHandShownYOffsetMin(`[SerializeField] private`) ·
//      extraSpaceOnSelectedCard(`AnimationCurve`) · warlordStartSizeFactor ·
//      tutorialTipHorizontalOffset · tutorialTipHorizontalOffsetForElements · moveCardYOffset · targetYOffset ·
//      `GetCardInHandYOffsetTouchInput()`
//  · **两份真值实例** = `d:/2/新解包资源/assets_full/bundle_duplicateassetisolationso_assets_all/MonoBehaviour/`
//      的 `VarsDesktop.json` / `VarsMobile.json`（`m_Script.m_PathID = -3478648252019303344` = `VarsDevice`）。
//      桌面 / 移动：`cardInHandShownScale` 1.3 / 1.7 · `cardInHandShownYOffset` 30 / 25 ·
//      `cardInHandShownYOffsetMin` 20 / 12 · `warlordStartSizeFactor` 1.3 / 1.5 ·
//      `tutorialTipHorizontalOffset` 20 / 18 · `…ForElements` 18 / 16 · `moveCardYOffset` 0.2 / 0.7 ·
//      `targetYOffset` 0.0 / 0.2 ·
//      `extraSpaceOnSelectedCard` **桌面 = 恒 2.0 的平曲线**（两键 (0,2)/(1,2)、四个 slope 全 0）/
//      移动 = (0,2)→(1,5)、两端 inSlope = outSlope = 3。
//  · 哪一份生效 = `Battle Arena 1.unity` 的 `BattleManager` 块 `desktopVariables` 指向 `VarsDesktop.asset`
//      （`mobileVariables` 指向 `VarsMobile.asset`，但**可读四源里零消费者**）⇒ 本工程只出桌面 = `Desktop`。
//  · `GetCardInHandYOffsetTouchInput()` 真体 = `decomp_full/VarsDevice__GetCardInHandYOffsetTouchInput.c`：
//      `f = ScreenUtilities.GetScreenPhysicalSize();`
//      `if (f <= <0x1834b338c>)  return this+0x1c;   // cardInHandShownYOffset`
//      `return this+0x20;                            // cardInHandShownYOffsetMin`
//      ⚠️ 那个字面量 = **7.0f**（`工具/read_literal.py` 按 **4 字节 float** 读 ✅；8 字节读法给 8.59e9 是垃圾）。
//      📌 `Warpforge_code/…/VarsDevice.cs` 里这个方法印的是 `return 0f` —— 那是**签名桩的空壳体**，⛔ 不是真值。
//
// ==================================================================================================
//  我们这边（如实标）
// ==================================================================================================
//  ⚠️ **形态不同**：原版是 `ScriptableObject` + 两份 `.asset`；我们是**纯 C# 类 + 两个静态实例**
//     （本工程不跑资产烘焙链，值直接来自上面两份 JSON）⇒ 语义等价，⛔ 别再建第三份 `Resources/*.json`。
//  ⚠️ **今天没有消费者切平台**：`Current` 恒 `Desktop`（原版靠 `desktopVariables` / `mobileVariables` 两字段选；
//     `mobileVariables` 在可读四源里 0 消费者）⇒ 未接切换，⛔ 别假装支持移动档。
//  📌 已散落实现（本轮**不动**，只在此登记）：`cardInHandShownScale` 1.3 → `Hand/HandLayout.cs:267` ·
//     `cardInHandShownYOffset` 30 → `HandLayout.cs:34` · `tutorialTipHorizontalOffset` 20 /
//     `…ForElements` 18 → `BattleDriver.cs:17033/17038` · `moveCardYOffset` 0.2 → `BattleDriver.cs:36013`。
//  🔴 **缺的那一项 = `warlordStartSizeFactor`（桌面 1.3）**：全仓 0 命中 ⇒ 整条没做。
//     接法（⛔ 别凭猜，按原版次序；判据 → 账 `战斗逻辑驱动:291` 的 `B92_B8` 那段）：
//       ① `GetUnitSizeInPlay(cardType, isPlayer)` 的等价物 → 写 `DefaultScale`；
//       ② `transform.localScale = defaultScale × VarsDevice.Current.warlordStartSizeFactor`；
//       ③ 再判 `waitMult != 1.0` 的 `localPosition.y -= 0.2` 那一跳。
//     唯一读点 = 原版 `CardScript.<HeroLandIntoField>d__318.MoveNext`（VA `0x180611B6F`，指令序已坐实：
//     `call DeviceVariables` → `movss xmm2,[rax+0x30]` → 逐分量 `mulss` → `set_localScale`）。
//     我们的等价方法 = `CardPresentation/Battle/BattleDriver.cs:34703 HeroLandInto`（**不属本代理**，其 doc 已明写
//     「缩放/alpha 两半按我们的形状不写」⇒ 缺的正是这一半）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `VarsDevice`（平台相关的观感参数字典）。字段名/序逐字照原版，值逐项来自
    /// `VarsDesktop.json` / `VarsMobile.json`（见文件头）。</summary>
    public class VarsDevice
    {
        public float cardInHandShownScale;
        public float cardInHandShownYOffset;

        /// <summary>原版是 `[SerializeField] private`（只被 <see cref="GetCardInHandYOffsetTouchInput"/> 读）——
        /// 我们保持 private，另给只读口 <see cref="CardInHandYOffsetMin"/> 供自检。</summary>
        [SerializeField] float cardInHandShownYOffsetMin;

        /// <summary>选中一张手牌时让开的距离（**世界单位**；X = 手牌方向、Y = 另一轴）。
        /// ⚠️ 同一份 SO 里 `cardInHandShownYOffset` 是 **UI px** 而这一项是**世界单位** —— 原版**混量纲**，照抄。</summary>
        public AnimationCurve extraSpaceOnSelectedCard;

        public float warlordStartSizeFactor;
        public float tutorialTipHorizontalOffset;
        public float tutorialTipHorizontalOffsetForElements;
        public float moveCardYOffset;
        public float targetYOffset;

        /// <summary>自检读口（原版字段是 private）。⛔ 生产代码别读它 —— 走
        /// <see cref="GetCardInHandYOffsetTouchInput"/>（那才是原版唯一读点）。</summary>
        public float CardInHandYOffsetMin { get { return cardInHandShownYOffsetMin; } }

        /// <summary>= 原版 `VarsDevice.GetCardInHandYOffsetTouchInput()`（判据见文件头）：
        /// 屏幕物理对角线 **≤ 7.0 英寸** ⇒ `cardInHandShownYOffset`；否则 ⇒ `cardInHandShownYOffsetMin`。
        /// <para>⚠️ 那个 7.0 是 `0x1834b338c` **4 字节 float** 读出来的；屏幕尺寸走
        /// <see cref="ScreenUtilities.GetScreenPhysicalSize"/>（**唯一实现**，⛔ 别在这里再写一遍 sqrt）。</para></summary>
        public float GetCardInHandYOffsetTouchInput()
        {
            const float kLargeScreenInches = 7.0f;      // 判据 = DAT_0x1834b338c（4B float）
            float physical = ScreenUtilities.GetScreenPhysicalSize();
            return physical <= kLargeScreenInches ? cardInHandShownYOffset : cardInHandShownYOffsetMin;
        }

        // ---- 两份真值实例（`VarsDesktop.json` / `VarsMobile.json` 现读） ----

        /// <summary>`VarsDesktop.asset`（`Battle Arena 1.unity` 的 `BattleManager.desktopVariables` 指向它）。</summary>
        public static readonly VarsDevice Desktop = new VarsDevice
        {
            cardInHandShownScale = 1.3f,
            cardInHandShownYOffset = 30f,
            cardInHandShownYOffsetMin = 20f,
            // 两键 (0,2)/(1,2)、四个 slope 全 0 ⇒ **恒 2.0 的平曲线**
            extraSpaceOnSelectedCard = new AnimationCurve(new Keyframe(0f, 2f, 0f, 0f),
                                                          new Keyframe(1f, 2f, 0f, 0f)),
            warlordStartSizeFactor = 1.3f,
            tutorialTipHorizontalOffset = 20f,
            tutorialTipHorizontalOffsetForElements = 18f,
            moveCardYOffset = 0.2f,
            targetYOffset = 0f,
        };

        /// <summary>`VarsMobile.asset`（可读四源里**零消费者**；留着是为了「值不丢」+ 将来接移动档时只改一处）。</summary>
        public static readonly VarsDevice Mobile = new VarsDevice
        {
            cardInHandShownScale = 1.7f,
            cardInHandShownYOffset = 25f,
            cardInHandShownYOffsetMin = 12f,
            // (0,2)→(1,5)，两端 inSlope = outSlope = 3（JSON m_Curve 现读）
            extraSpaceOnSelectedCard = new AnimationCurve(new Keyframe(0f, 2f, 3f, 3f),
                                                          new Keyframe(1f, 5f, 3f, 3f)),
            warlordStartSizeFactor = 1.5f,
            tutorialTipHorizontalOffset = 18f,
            tutorialTipHorizontalOffsetForElements = 16f,
            moveCardYOffset = 0.7f,
            targetYOffset = 0.2f,
        };

        /// <summary>本局生效的那一份。**恒 <see cref="Desktop"/>** —— 本工程只出桌面，
        /// 且原版 `mobileVariables` 在可读四源里 0 消费者（未接平台切换，如实记）。</summary>
        public static VarsDevice Current { get { return Desktop; } }
    }
}
