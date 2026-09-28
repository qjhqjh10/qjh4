// CardWinBox.cs — 卡片详情窗（原版 `CardDisplayWindow`）的**矩形/字号/颜色**（判据只此一份）
//
// 为什么单开一个文件：**原版是同一个脚本、两处摆放** —— 菜单版 `Shell/CardDetailPopup`
// 与战斗版 `Battle/CardDisplayWindow` 用的是**同一套绝对 px**，写两份迟早不一致。
//
// 出处（两份独立来源**逐值吻合**，2026-09-28 复核）：
//   ① 菜单版逐节点表 —— `资料/阶段二_卡片详情窗_原版规格.md` §一（从场景根一路走下来的绝对 px）
//   ② 战斗版运行时 dump —— `资料/原版参照图/Unity参照管线_0825/data/runtime_ui_dump_Battle_Arena_1.tsv:529-538`
//      （`Card Display Window/Card Display` = 752×868 · `LowerSection` pos(0,−518.3) 1320×137 ·
//        `FlavourTextBG` 1320×178 · `LoreText` 1250×93.5 · `Voice Over Button` pos(734.3,0) 88.7²）
// 坐标约定：**1920×1080 · 左上原点 · y 向下**。
using UnityEngine;

namespace CardPresentation
{
    public static class CardWinBox
    {
        /// <summary>`Card Display` —— 整块卡区 752×868，屏幕正中（x[584,1336] y[106,974]）。</summary>
        public const float CardL = 584f, CardT = 106f, CardR = 1336f, CardB = 974f;

        /// <summary>`Card Display/Cards` —— 纯容器（原版是个 100×100 的盒子，5 张卡都挂它下面）。</summary>
        public const float CardsCx = 960f, CardsCy = 540f, CardsS = 100f;

        /// <summary>`Card Display/LowerSection` —— 下缘那条（`pivot(0.5,0)` · 1320×137）。</summary>
        public const float LowerL = 300f, LowerT = 921.34f, LowerR = 1620f, LowerB = 1058.34f;
        /// <summary>`LowerSection/FlavourTextBG` —— 文字底板（1320×178 · 运行期由 `FlavourTextSO` 喂图）。</summary>
        public const float LoreBgT = 900.84f, LoreBgB = 1078.84f;
        /// <summary>`FlavourTextBG/LoreText` —— 1250×93.47 · **fs35**（auto 10–35）· **右对齐** · 白。</summary>
        public const float LoreL = 335f, LoreT = 943.11f, LoreR = 1585f, LoreB = 1036.58f;
        public const float LorePx = 35f;

        /// <summary>两个圆钮：88.655²（`40k_UI_bt_voicelines` / `40k_UI_bt_eye`）。</summary>
        public const float BtnS = 88.655f, BtnCy = 989.84f;
        /// <summary>`Voice Over Button` 中心 x（原版 pos(734.33,0.0002) 相对 LowerSection 中心）。</summary>
        public const float VoiceCx = 1694.33f;
        /// <summary>`Show Card Text`（眼睛钮）中心 x。</summary>
        public const float EyeCx = 225.67f;

        /// <summary>`Menu Dark Background` —— 压暗遮罩 4574.6×2572.36（两倍屏还多 = 铺满带余量）。</summary>
        public const float MaskL = -1327.3f, MaskT = -746.18f, MaskR = 3247.3f, MaskB = 1826.18f;
        /// <summary>遮罩色 —— 原版 `color = (0,0,0,0.773)`。</summary>
        public static readonly Color ShadeColor = new Color(0f, 0f, 0f, 0.773f);

        // ============================================================ 效果清单 `EffectList`（卡右侧那一竖列）
        //
        // 「谁给我加的 buff」那块 —— 原版 `Card Display/EffectList`，**只有大卡展示窗有**，出厂关着，
        // 由 `BattleManager.DisplayCard → CardDisplayWindow.ShowBattleCard → DisplayCardEffects` 驱动
        // （`cardEffectsGroup` 的**唯一**写者，全反编译核对过）。
        //
        // 出处：`d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena1/`（13 个战场包数值**完全一致**）——
        //   `Transform/Transform_1433.json`（EffectList 原点）· `RectTransform/RectTransform_3043.json`（Elements）
        //   + 5 个槽 RT（2912/3114/2643/3335/3107）+ 各子节点 RT；逐值与换算链 → `资料/待办判据_战场与战斗视图.md` §8b。
        //   换算：`EffectList.localScale = 108.79123` ⇒ **子树 1 单位 = 108.79 px**。
        // ⚠️ **2026-09-28 订正三处误读**（原来记的 `EffectBg 161.8` / `AffectedBy「0.4×0.885」` / 「整块中心 (1628,545)」
        //    都不是能直接用进代码的值）：`EffectBg` 的 `m_LocalScale` 是 **(0.5, 0.4)**（两分量不同 ⇒ 实绘 129.4 高）；
        //    `AffectedBy` 的 **0.4 是 TMP `m_fontSize`、0.88542 是 `m_LocalScale`**（RT 真身 4.43×0.53）；
        //    (1628, 545) 是 **`Elements` 矩形的中心**，不是 `EffectList` 的原点。
        /// <summary>整块（`Elements`）左右边界 —— 卡右侧那一竖列，贴右屏边。</summary>
        public const float EffL = 1358.1f, EffR = 1898.6f;
        /// <summary>一行的宽高（5 个槽 `m_SizeDelta` 全同）。</summary>
        public const float EffRowW = 540.5f, EffRowH = 150.0f;
        /// <summary>行距 pitch（`VerticalLayoutGroup.spacing = −0.09` ⇒ 140.2，行间叠 9.8）。</summary>
        public const float EffPitch = 140.2f;
        /// <summary>行底板 `EffectBg` 的**实绘**尺寸（9.83×2.974 × scale **(0.5, 0.4)**）。</summary>
        public const float EffBgW = 534.7f, EffBgH = 129.4f;
        /// <summary>槽心 x（整块的水平中心）。</summary>
        public const float EffCx = 1628.4f;
        /// <summary>五个槽的中心 y（**距屏幕上边**，y 向下）—— 逐槽 `m_AnchoredPosition.y` 复算。</summary>
        public static readonly float[] EffRowCy = { 269.7f, 409.9f, 550.2f, 690.4f, 830.7f };
        /// <summary>槽数 —— 原版**硬编码 5 个**（`Elements` 下就是 5 个 `EffectElement`）。</summary>
        public const int EffSlots = 5;
        /// <summary>`EnchanterText`（**谁给的**）：框 467.8×38.1 · 中心在槽心**上 31.5** · 垂直对齐 Bottom。</summary>
        public const float EffWhoW = 467.8f, EffWhoH = 38.1f, EffWhoDy = -31.5f;
        /// <summary>`EffectText`（**给了什么**）：框 467.8×62.0 · 中心在槽心**下 17.4** · 垂直对齐 Middle。</summary>
        public const float EffWhatW = 467.8f, EffWhatH = 62.0f, EffWhatDy = 17.4f;
        /// <summary>两行文字：**fs32.6**（原版 `m_fontSize 0.3`，auto 10.9–32.6）· **左对齐** · 白。</summary>
        public const float EffTextPx = 32.6f;
        /// <summary>标题 `AffectedBy`：实绘 **426.7×51.1** · 中心 **(1607.8, 161.2)**（在 `Elements` **上方**，间隙 8.1）
        /// · **没有旋转**（`m_LocalRotation` 是单位四元数）· Left/Middle · 白 · **fs38.5**（auto 19.3–38.5）。</summary>
        public const float EffTitleCx = 1607.8f, EffTitleCy = 161.2f;
        public const float EffTitleW = 426.7f, EffTitleH = 51.1f, EffTitlePx = 38.5f;
        /// <summary>这一块的渲染队列 —— 原版它是 `Card Display` 的**最后一个子节点**（画在卡之上）；
        /// `CardFan.QCardBase = 3009` 那一叠最多用到 **3017**，遮罩/文字是 3000 ⇒ 取 **3020**（上面留空号）。</summary>
        public const int QEffect = 3020;
    }
}
