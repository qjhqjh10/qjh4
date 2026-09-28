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
    }
}
