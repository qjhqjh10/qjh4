// ChoosePanel.cs — 原版「选牌 / 选效果」面板（`ChooseCardMenu`）
//
// **它和「换牌」是同一个组件吗**：不是同一个**组件**（`BattleManager` 里 `mulliganManager` 与
// `chooseCardManager` 是两个独立字段、运行时 dump 里也是并排两个节点），但**两棵节点树逐节点同构**
// —— 原版是**两份克隆**。⇒ 我们**合成一份壳**（`CardChoicePanel`），这里只放这个面板**自己**的数值。
// 出处：`资料/选牌Choose_数据与设计.md` §四之二 · `资料/选牌与选效果面板_原版数值.md`。
//
// ---- 数值出处（**实况**，铁律 4）----
// `资料/原版参照图/Unity参照管线_0825/data/panel_0914b/报告.md` §③（`p4_choose_tree.tsv` 的逐字段真值）
//   · 根 `ChooseCardMenu` 铺满全屏；`ChooseCardMenuAnchor` **childCount=0**（卡运行时生成）
//   · `HideChooseButton`  `ap=143.40,133.17  sd=78.44²  a=(0,0)`  sprite `40k_UI_bt_eye`
//        → 锚点是**左下** ⇒ 屏上中心 = (143.40, 1080−133.17 = **946.83**)
//   · `Continue Button`  `ap=−396.40,105.00  sd=548.02×75.90  a=(1,0)`（**底右**锚）
//        → 容器中心 = (1920−396.40, 105.00) = (**1523.60**, 从下 105.00)
//        ├ `BG`              `ap=65.20,−5.75  sd=577.50×63.84`  sprite `40k_bt_underbutton`
//        │      → (**1588.80**, 从**上** 980.75)
//        ├ `ContinueText`    `ap=−4.60,−5.26  sd=372.86×43.62`  TMP **`继续` fs=33.7** 白 左对齐
//        │      → (**1519.00**, 从**上** 980.26)
//        └ `ContinueButton`  `ap=240.31,−3.39  sd=80.47×79.64`  sprite `40k_UI_bt_play`
//               → (**1763.91**, 从**上** 978.39)
//   · `ChooseText`       `ap=7.00,−106.50  sd=1344×79.44  a=(.5,1)`  TMP **`选择一张牌` fs=55** 白 居中
//        → 屏上中心 = (**967**, 从**上** **106.5**)（和换牌面板那个文字框**完全相同**）
//   ⚠️ `ChooseText` 的 Image 组件是 **DISABLED** 的（原版那块没有底板）⇒ 我们也不画底板。
//
// ---- ⚠️ 哪些是「我们挑的」----
//   · **卡行的纵向位置**（`CardRowCy`）：原版那个 anchor 节点**只有 Transform、没有 RectTransform**，
//     卡的位置是运行时算的 ⇒ **无据可查**。这里取「标题下沿 ≈146」到「眼睛上沿 ≈907.6」这段空档的
//     中点 = **526**，正好让整行卡（611 px 高）落在里面。
//   · **卡上那颗 `Select` 按钮的纵向偏移**：原版 prefab `CardChooseCardButtonFrame` 是运行时实例化的，
//     静态 dump 里没有它；实测那个 prefab 的按钮在卡中心下方 **≈323 px**（§三末），
//     共享壳用的是「卡高 ×0.30」≈183 px —— **我们排的**，要更贴原版就去改 `CardChoicePanel.LayOutCards`。
//   · **压暗层**、**眼睛是开关还是按住**：见共享壳文件头的说明。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `ChooseCardMenu`：**选牌**与**选效果**共用（靠标题区分）。**单选**。</summary>
    public class ChoosePanel : CardChoicePanel
    {
        /// <summary>原版运行时实测的标题兜底文案（词条 key `Battle/ChooseCard/Instructions` 的中文）。
        /// 出处：实况报告 §③ / `选牌与选效果面板_原版数值.md` §四（2026-09-14 晚更正：**运行时是中文**）。</summary>
        public const string DefaultTitle = "选择一张牌";
        /// <summary>原版 `Battle/Mulligan/ButtonDone` 的中文 —— 选牌**复用了换牌那条**词条。</summary>
        public const string ConfirmLabel = "继续";

        /// <summary>原版每张牌下面那颗按钮上的字（`CardChooseCardButtonFrame` 里的 `Select`）。
        /// ⚠️ 中文词条查不到（I2 表本地没有）⇒ **留英文**，如实标着。</summary>
        public const string CardBtnWord = "Select";

        public static ChoosePanel Create(Transform parent)
        {
            var go = new GameObject("ChoosePanel");
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<ChoosePanel>();
            p.Build(new Layout
            {
                TitleCx = 967f, TitleCy = 106.5f, TitleW = 1344f, TitleH = 79.44f, TitlePx = 55f,
                BarCx = 1588.80f, BarCy = 980.75f,
                PlayCx = 1763.91f, PlayCy = 978.39f,
                EyeCx = 143.40f, EyeCy = 946.83f,
                ConfirmCx = 1519.00f, ConfirmCy = 980.26f,
                CardRowCy = 526f,                    // ⚠️ **我们挑的**，见文件头
                CardBtnText = CardBtnWord,
            }, PickMode.Single, DefaultTitle, ConfirmLabel);
            return p;
        }

        /// <summary>开面板。`title` 传 null 就用原版兜底文案 `选择一张牌`。
        /// ⚠️ 「选效果」复用同一个面板（原版靠 `isEnviromental` 分叉）——**我们靠标题区分**，
        ///    因为原版 `isEnviromental` 到底改了哪些视觉/文案**查不到**（那个方法体没有反编译产物）。</summary>
        public void Open(IReadOnlyList<CardView> cards, string title = null)
        {
            base.Open(cards, title ?? DefaultTitle);
        }
    }
}
